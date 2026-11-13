#!/usr/bin/env python3
"""Copy closed, payload-free Gateway diagnostic qualification evidence only."""
from collections import Counter
import difflib
import gzip
import hashlib
import json
import os
from pathlib import Path
import stat

WORKSPACE = Path('/home/administrator/projects/hexalith/projects')
PREP = Path('/tmp/story65-dapr-invoker-prep-20261107')
QUAL = Path('/tmp/story65-dapr-invoker-qualification-20261107')
OPS = Path('/tmp/story65-dapr-invoker-ops-prep-20261107')
DEST = Path('/tmp/story65-dapr-invoker-evidence-20261107')
PREP_SHA = '76b7a97319def3cfca5f58d35eff35e3e7ec51b2bf8c46941f9b8fcc91737938'
FINAL_SHA = 'ffed4376b3ea7b8b7afb5248519d444dad3bf57801a9d333debd565ac944f306'
ALLOWED = {'.cs', '.csproj', '.props', '.targets', '.proj', '.json', '.md', '.py',
           '.log', '.patch', '.diff', '.txt', '.xml', '.editorconfig', '.config'}
ROW_FIELDS = {'role', 'runOrdinal', 'captureOrdinal', 'stage', 'elapsedTicks', 'frequency',
              'allocationMode', 'allocatedBytes', 'yielded', 'nullValue',
              'authenticationAccepted', 'nonAuthenticationDiscarded',
              'sensitiveSegmentExcluded', 'authenticationOtherExcluded'}
STAGES = {'Whole', 'ComponentFile', 'ComponentPolicy', 'AclFile',
          'SettingsEnumerationSortDictionary', 'AuthenticationSerialization',
          'ActorBoundary', 'ResultTail'}
ZERO = {'allocationMode', 'allocatedBytes', 'yielded', 'nullValue',
        'authenticationAccepted', 'nonAuthenticationDiscarded',
        'sensitiveSegmentExcluded', 'authenticationOtherExcluded'}
CLOSED_FIELDS = {'diagnosticOnly', 'featureAcceptance', 'role', 'runOrdinal', 'admitted',
                 'disposedOrUnwound', 'activeAtDrainEnd', 'queuedRowsDropped',
                 'closedRowsDropped', 'capRowsDropped', 'ioFailures', 'writtenRows',
                 'writtenBytes', 'queuedAtDrainEnd', 'incompleteHardDrain',
                 'countersFinal', 'lateDisposedOrDropCountersMayIncrease', 'maximumRows',
                 'maximumBytes', 'queueCapacity', 'windowStartTicks', 'windowEndTicks',
                 'frequency'}

def sha(data):
    return hashlib.sha256(data).hexdigest()

def strict_pairs(pairs):
    value = {}
    for key, item in pairs:
        assert key not in value, 'duplicate JSON property'
        value[key] = item
    return value

def decode(data):
    return json.loads(data, object_pairs_hook=strict_pairs)

def read_regular(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    try:
        before = os.fstat(fd)
        assert stat.S_ISREG(before.st_mode), 'nonregular input'
        chunks = []
        while chunk := os.read(fd, 1024 * 1024):
            chunks.append(chunk)
        after = os.fstat(fd)
        assert (before.st_dev, before.st_ino, before.st_size, before.st_mtime_ns) == (
            after.st_dev, after.st_ino, after.st_size, after.st_mtime_ns), 'input changed'
        data = b''.join(chunks)
        assert len(data) == before.st_size
        return data
    finally:
        os.close(fd)

assert not DEST.exists(), 'fresh staging directory required'
prep_data = read_regular(PREP / 'preparation-receipt.json')
final_data = read_regular(QUAL / 'final-scratch-qualification.json')
assert sha(prep_data) == PREP_SHA and sha(final_data) == FINAL_SHA
prep = decode(prep_data)
final = decode(final_data)
assert final['allPassed'] and not final['featureAcceptance']
assert (final['passingBehavioralCells'], final['passingFreshCanaries'], final['controlledProcesses'],
        final['strictValidatedRows']) == (120, 3, 19, 111752)

# Verify pinned sources and metadata before writing the separate staging tree.
for relative, expected in prep['filesSha256'].items():
    assert sha(read_regular(PREP / relative)) == expected
for field in ('sourceSha256', 'app147Sha256', 'nativeFixtureSourceSha256'):
    for relative, expected in prep[field].items():
        assert sha(read_regular(WORKSPACE / relative)) == expected

DEST.mkdir(mode=0o700)
provenance = []
skipped = []
compressed = []

def write(relative, data):
    target = DEST / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open('xb') as stream:
        stream.write(data)
    target.chmod(0o444)
    return target

def copy(source, relative):
    data = read_regular(source)
    data.decode('utf-8')  # No binary/cache files enter this text-only packet.
    write(relative, data)
    provenance.append({'source': str(source), 'destination': relative.as_posix(),
                       'sha256': sha(data), 'bytes': len(data)})

def tree(base, prefix, excluded_roots):
    for path in sorted(base.rglob('*')):
        relative = path.relative_to(base)
        if any(part == '__pycache__' for part in relative.parts):
            continue
        if relative.parts[0] in excluded_roots:
            continue
        assert not path.is_symlink(), 'symlink in closed input tree'
        if not path.is_file():
            continue
        if path.suffix.lower() not in ALLOWED:
            skipped.append({'source': str(path), 'reason': 'non-text extension excluded'})
            continue
        copy(path, Path(prefix) / relative)

tree(PREP, 'source-preparation', {'bin', 'obj', 'results-qualified-initial'})
tree(QUAL, 'scratch-qualification', set())
tree(OPS, 'producer-history', {'live', 'isolated-gateway', 'isolated-oracle'})

for field, prefix in (('sourceSha256', 'source-controls'),
                      ('nativeFixtureSourceSha256', 'native-test-source-only')):
    for relative, expected in prep[field].items():
        source = WORKSPACE / relative
        assert sha(read_regular(source)) == expected
        copy(source, Path(prefix) / relative)

# Result JSONL is synthetic closed-schema numeric timing only. Preserve exact
# original byte identity through deterministic gzip, then independently parse.
all_rows = 0
all_stages = Counter()
results = PREP / 'results-qualified-initial'
for path in sorted(results.rglob('*')):
    assert not path.is_symlink()
    if not path.is_file():
        continue
    relative = path.relative_to(results)
    if path.name != 'gateway.jsonl':
        assert path.suffix == '.json', 'unexpected result artifact'
        copy(path, Path('synthetic-results') / relative)
        continue
    raw = read_regular(path)
    closed = decode(read_regular(path.with_name('gateway.closed.json')))
    assert set(closed) == CLOSED_FIELDS
    assert closed['role'] == 'gateway'
    for key in CLOSED_FIELDS - {'role'}:
        assert type(closed[key]) is int and closed[key] >= 0
    assert closed['diagnosticOnly'] == 1 and closed['featureAcceptance'] == 0
    assert closed['countersFinal'] == 1 and closed['incompleteHardDrain'] == 0
    assert closed['writtenBytes'] == len(raw)
    counts = Counter()
    rows = 0
    for line in raw.splitlines():
        row = decode(line)
        assert set(row) == ROW_FIELDS
        assert row['role'] == 'gateway' and row['stage'] in STAGES
        for key in ROW_FIELDS - {'role', 'stage'}:
            assert type(row[key]) is int and row[key] >= 0
        assert row['runOrdinal'] == closed['runOrdinal']
        assert row['captureOrdinal'] > 0 and row['frequency'] == closed['frequency'] > 0
        assert all(row[key] == 0 for key in ZERO)
        rows += 1
        counts[row['stage']] += 1
    assert rows == closed['writtenRows']
    zipped = gzip.compress(raw, compresslevel=9, mtime=0)
    destination = Path('synthetic-results') / relative.with_suffix('.jsonl.gz')
    write(destination, zipped)
    assert gzip.decompress(read_regular(DEST / destination)) == raw
    compressed.append({'source': str(path), 'destination': destination.as_posix(),
                       'originalSha256': sha(raw), 'originalBytes': len(raw),
                       'gzipSha256': sha(zipped), 'gzipBytes': len(zipped),
                       'strictRows': rows, 'stageCounts': dict(sorted(counts.items())),
                       'allCountAndAllocationColumnsZero': True})
    all_rows += rows
    all_stages.update(counts)
assert all_rows == final['strictValidatedRows'] == 111752

# Preserve the root-approved one-line scratch-only namespace fix and original failure.
old = read_regular(PREP/'history/4c0ab85b/driver/SubjectFixture.cs').decode()
new = read_regular(PREP/'driver/SubjectFixture.cs').decode()
added = 'using Hexalith.EventStore.Contracts.Routing;\n'
assert old.replace('using Hexalith.EventStore.Contracts.Queries;\n',
                   'using Hexalith.EventStore.Contracts.Queries;\n'+added) == new
assert sha(read_regular(PREP/'history/4c0ab85b/preparation-receipt.json')) == '4c0ab85b0d04d136768876d0244b19e04296f151c2602468033f81a8300cb173'
assert decode(read_regular(QUAL/'build-1.json'))['exitCode'] == 1
assert decode(read_regular(QUAL/'build-2.json'))['exitCode'] == 0
copy(Path(__file__), Path('packaging/stage_evidence.py'))
write(Path('STAGING.md'), b'''This is partial DaprDomainQueryInvoker diagnostic evidence staging only.

Scratch qualification passed120 comparison cells and3 fresh first-business
flow canaries across19 controlled processes. Independently validated111,752
synthetic numeric timing rows are preserved as deterministic gzip with original
SHA-256 and byte lengths. This is not feature acceptance or a latency claim.

Included: exact insertion-only original/private source and normalization proof,
unchanged helper112/bootstrap05bbb, all harness/API/source controls, exact
commands/logs/receipts, parent audit, same-identity producer inputs/path checks,
the original failed scratch build and approved one-line namespace-import fix.
The original4c0a preparation and failed build1 are preserved in history; build2
passed0warnings/0errors. The isolated producer succeeded on its first attempt.

Binary identities remain as hashes, but DLL/PDB/cache binaries are excluded.
Raw numeric control contents, payloads, proof bindings, logger arguments,
context/canary values, private runtime data and ongoing ops/live preparation
are excluded. Original absolute paths identify actual executed inputs;
provenance.json maps copied text and exact compressed data to staged paths.
Original preparation/ops/source/application files were not modified.

Native15, actual managed/live measurement, cleanup/ordinary restoration and
repository publication remain pending for the main agent. The native15 set
includes two loopback WebApplication cases, which were not run by scratch.
No builds, assembly loads, tests or hosts ran during packaging.

HTTP scope6 overlaps3/4/5; lease scope7 overlaps2/3/4/5/6. Do not sum them as
disjoint stages or call Whole residual exact disposal. Send includes default
ResponseContentRead buffering and internal cleanup, without network/server
attribution. No local ordinal is joined to prior root/controller/client data.
''')
def json_write(relative, value):
    write(Path(relative), (json.dumps(value, indent=2, sort_keys=True) + '\n').encode())

json_write('provenance.json', {'textCopies': provenance, 'gzipCopies': compressed,
                             'excludedNonTextFiles': skipped,
                             'intentionallyExcludedTrees': [str(PREP/'bin'), str(PREP/'obj'),
                                 str(OPS/'live'), str(OPS/'isolated-gateway'), str(OPS/'isolated-oracle')],
                             'sourcePrepAndOpsUnmodified': True})
json_write('strict-synthetic-schema-audit.json', {
    'rows': all_rows, 'gzipFiles': len(compressed), 'rowFields': sorted(ROW_FIELDS),
    'closedFields': sorted(CLOSED_FIELDS), 'stageCounts': dict(sorted(all_stages.items())),
    'exactIntegerTypesRejectBool': True, 'duplicateJsonPropertiesRejected': True,
    'allAllocationAndCountColumnsZero': True, 'allCompressedBytesRoundTripExact': True,
    'everyWrittenRowAndByteCountMatchesClosedReceipt': True})
json_write('failure-fix-lineage.json', {
    'originalPreparationReceiptSha256': '4c0ab85b0d04d136768876d0244b19e04296f151c2602468033f81a8300cb173',
    'originalPreparationHistory': 'source-preparation/history/4c0ab85b/',
    'failedBuildReceipt': 'scratch-qualification/build-1.json',
    'failedBuildLog': 'scratch-qualification/build-1.log',
    'failedBuildExitCode': 1, 'failedBuildErrorCount': 9,
    'failedBuildErrorCodes': ['CS0103', 'CS0246'],
    'exactApprovedPatch': 'source-preparation/approved-namespace-import-fix.patch',
    'onlyAddedCodeLine': 'using Hexalith.EventStore.Contracts.Routing;',
    'successfulBuildReceipt': 'scratch-qualification/build-2.json',
    'successfulBuildWarnings': 0, 'successfulBuildErrors': 0,
    'successfulProducerReceipt': 'producer-history/producer-attempts/attempt-7mixknj3/receipt.json',
    'ordinaryOutputsUnchanged': True})
json_write('pending-sections.json', {
    'scratchQualificationPassed': True, 'featureAcceptance': False,
    'native15': 'pending main-agent authorization/execution and evidence (two loopback WebApplication cases)',
    'managedLive': 'pending main-agent authorization/execution and evidence',
    'ordinaryRestoration': 'pending main-agent execution and evidence',
    'repositoryEvidencePublication': 'pending main-agent review/publication',
    'noApplicationBuildsTestsHostsOrAssemblyLoadsDuringStaging': True})

# Recheck all source/prep inputs to prove the packaging operation was read-only.
for relative, expected in prep['filesSha256'].items():
    assert sha(read_regular(PREP / relative)) == expected
for field in ('sourceSha256', 'app147Sha256', 'nativeFixtureSourceSha256'):
    for relative, expected in prep[field].items():
        assert sha(read_regular(WORKSPACE / relative)) == expected
for item in provenance:
    assert sha(read_regular(Path(item['source']))) == item['sha256']

manifest = {}
for path in sorted(DEST.rglob('*')):
    if path.is_file():
        data = read_regular(path)
        manifest[path.relative_to(DEST).as_posix()] = {'sha256': sha(data), 'bytes': len(data)}
json_write('staging-manifest.json', {
    'partialEvidenceStagingOnly': True, 'featureAcceptance': False,
    'preparationReceiptSha256': PREP_SHA, 'scratchQualificationReceiptSha256': FINAL_SHA,
    'passingBehavioralCells': 120, 'passingFreshCanaries': 3, 'controlledProcesses': 19,
    'strictSyntheticRows': all_rows, 'sourceControls': len(prep['sourceSha256']),
    'ordinaryAppDllHashControls': len(prep['app147Sha256']),
    'noBinariesOrRawControlsOrPayloads': True, 'files': manifest,
    'pending': ['native15', 'managed/live', 'ordinary restoration', 'repository publication']})
data = read_regular(DEST / 'staging-manifest.json')
print(json.dumps({'path': str(DEST), 'filesInManifest': len(manifest),
                  'manifestSha256': sha(data), 'strictRows': all_rows,
                  'gzipFiles': len(compressed), 'sourceAndOrdinaryOutputsUnchanged': True}))
