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
PREP = Path('/tmp/story65-outbound-assertion-prep-20261107')
QUAL = Path('/tmp/story65-outbound-assertion-qualification-20261107')
OPS = Path('/tmp/story65-outbound-assertion-ops-prep-20261107')
DEST = Path('/tmp/story65-outbound-assertion-evidence-20261107')
PREP_SHA = '6814b1a4a1e0658c809d9d8108ef6ff48292783f2605ac2b5dc84381f21c1f30'
FINAL_SHA = '5c97f64afab724a053dc846ed05dbdd3508ddd3f43c099149898c8991abad2d9'
ALLOWED = {'.cs', '.csproj', '.props', '.targets', '.proj', '.json', '.md', '.py',
           '.log', '.patch', '.diff', '.txt', '.xml', '.editorconfig', '.config'}
ROW_FIELDS = {'role', 'runOrdinal', 'captureOrdinal', 'stage', 'elapsedTicks', 'frequency',
              'allocationMode', 'allocatedBytes', 'yielded', 'nullValue',
              'authenticationAccepted', 'nonAuthenticationDiscarded',
              'sensitiveSegmentExcluded', 'authenticationOtherExcluded'}
STAGES = {'Whole', 'ComponentFile', 'ComponentPolicy'}
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
        final['strictValidatedRows']) == (100, 3, 15, 83688)

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
tree(OPS, 'producer-history', {'live', 'isolated-server', 'isolated-oracle'})

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
assert all_rows == final['strictValidatedRows'] == 83688

# Preserve the initial metadata-only census/hash assumption without changing inputs.
copy(Path('/tmp/story65-outbound-assertion-prep-static-audit-20261107.json'),
     Path('scratch-qualification/preparation-static-audit.json'))
assert decode(read_regular(QUAL/'build-1.json'))['exitCode'] == 0
copy(Path(__file__), Path('packaging/stage_evidence.py'))
write(Path('STAGING.md'), b'''This is partial outbound-assertion diagnostic source/scratch evidence only.
+
+The selected Server-owned DomainServiceWorkloadAssertionHandler.SendAsync
+keeps its public sealed partial identity and generated logger. Seven inserted
+lines add only Whole, original nullable-issuer ValueTask, and original base-send
+intervals. Helper112/bootstrap05bbb and all application statements are unchanged.
+
+Scratch passed100 comparison cells plus3 fresh first-business flow canaries in
+15 processes.83,688 strict numeric timing rows permit only Whole/ComponentFile/
+ComponentPolicy and zero count/allocation columns. They are preserved as gzip
+with exact original byte lengths and SHA-256. This is not feature acceptance,
+current live timing, per-root attribution, or ordinary1038 qualification.
+
+Included: exact source/normalization,50 prepared harness/design files,54 source
+controls,4 native fixture sources, role/API proof, producer inputs/path checks,
+all exact scratch commands/logs/receipts/facts, parent audits, and final aggregate.
+The isolated producer and scratch restore/build succeeded first attempt with
+zero warnings/errors. An initial preparation-only mistake interpreting the
+98-file DomainService census as a digest prefix is preserved separately; it
+required no source/harness fix and caused no build/runtime failure.
+
+DLL/PDB/cache binaries, raw numeric control contents, runtime payloads/assertions,
+header/binding/logger/context/canary values and ongoing ops/live prep are excluded.
+Original paths in receipts identify executed inputs. provenance.json maps copied
+text and compressed synthetic timing to staged paths. Binary hashes remain in
+receipts. All copied original preparation/source/ops/application inputs stayed
+unchanged, and no builds, assembly loads, tests or hosts ran during packaging.
+
+Fixed Gateway role applies only to the pinned root six-runtime composition.
+The existing builder filter excludes the issuer client; other factory requests,
+including noncanonical targets, are eligible. Whole and base stop before outer
+HttpClient ResponseContentRead buffering. Missing stages establish no outcome.
+No ordinal is joined to prior invoker/controller/client/root data, and residual
+durations are not assigned to an exact setup, network, server or disposal phase.
+
+Native29, managed/live measurement, cleanup/ordinary restoration and repository
+publication remain pending for main. Native29 uses exact rendered-name MULTISET:
+full28 partial-class executions plus joint1, with a static display-name collision
+of multiplicity3. The28 isolated suite strips RouteCatalogInvocationHandler;
+the joint1 retains the actual catalog/resilience/assertion graph. No per-input
+XML identity is claimed for that triple, and no runner/config change was made.
+''')
def json_write(relative, value):
    write(Path(relative), (json.dumps(value, indent=2, sort_keys=True) + '\n').encode())

json_write('provenance.json', {'textCopies': provenance, 'gzipCopies': compressed,
                             'excludedNonTextFiles': skipped,
                             'intentionallyExcludedTrees': [str(PREP/'bin'), str(PREP/'obj'),
                                 str(OPS/'live'), str(OPS/'isolated-server'), str(OPS/'isolated-oracle')],
                             'sourcePrepAndOpsUnmodified': True})
json_write('strict-synthetic-schema-audit.json', {
    'rows': all_rows, 'gzipFiles': len(compressed), 'rowFields': sorted(ROW_FIELDS),
    'closedFields': sorted(CLOSED_FIELDS), 'stageCounts': dict(sorted(all_stages.items())),
    'exactIntegerTypesRejectBool': True, 'duplicateJsonPropertiesRejected': True,
    'allAllocationAndCountColumnsZero': True, 'allCompressedBytesRoundTripExact': True,
    'everyWrittenRowAndByteCountMatchesClosedReceipt': True})
json_write('qualification-lineage.json', {
    'sourceAndHarnessFixesDuringQualification': 0,
    'failedScratchBuilds': 0,
    'successfulBuildReceipt': 'scratch-qualification/build-1.json',
    'successfulBuildWarnings': 0, 'successfulBuildErrors': 0,
    'preparationStaticAudit': 'scratch-qualification/preparation-static-audit.json',
    'staticAuditLimit': 'initial census/hash assumption preserved; no source/harness fix required',
    'successfulProducerReceipt': 'producer-history/producer-attempts/attempt-kj2q5zzx/receipt.json',
    'ordinaryOutputsUnchanged': True})
json_write('pending-sections.json', {
    'scratchQualificationPassed': True, 'featureAcceptance': False,
    'native29': 'pending main-agent authorization/execution and strict rendered-name multiset evidence (static collisionmultiplicity3)',
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
    'passingBehavioralCells': 100, 'passingFreshCanaries': 3, 'controlledProcesses': 15,
    'strictSyntheticRows': all_rows, 'sourceControls': len(prep['sourceSha256']),
    'ordinaryAppDllHashControls': len(prep['app147Sha256']),
    'noBinariesOrRawControlsOrPayloads': True, 'files': manifest,
    'pending': ['native29', 'managed/live', 'ordinary restoration', 'repository publication']})
data = read_regular(DEST / 'staging-manifest.json')
print(json.dumps({'path': str(DEST), 'filesInManifest': len(manifest),
                  'manifestSha256': sha(data), 'strictRows': all_rows,
                  'gzipFiles': len(compressed), 'sourceAndOrdinaryOutputsUnchanged': True}))
