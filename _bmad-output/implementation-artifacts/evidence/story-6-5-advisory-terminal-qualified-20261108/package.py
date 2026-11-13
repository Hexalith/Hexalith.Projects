"""File-only staging of the accepted synthetic terminal recorder qualification."""
from pathlib import Path
import gzip
import hashlib
import json

PREP = Path('/tmp/story65-advisory-terminal-prep-20261108')
RESULTS = Path('/tmp/story65-advisory-terminal-qualification-20261108')
BUILD = Path('/tmp/story65-advisory-terminal-build-20261108')
AUDIT = Path('/tmp/story65-advisory-terminal-audit-20261108')
HISTORY = Path('/tmp/story65-advisory-terminal-prep-history-20261108/59e1534a')
OUT = Path('/tmp/story65-advisory-terminal-qualified-publication-prep-20261108')
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
load = lambda p: json.loads(Path(p).read_bytes())
rows = []


def save(name, data):
    path = OUT / name
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as stream:
        stream.write(data)
    return path


def encode(value):
    return (json.dumps(value, indent=2) + '\n').encode()


def copy(source, name, compressed=False):
    source = Path(source)
    assert source.is_file() and not source.is_symlink()
    data = source.read_bytes()
    target = save(name, gzip.compress(data, mtime=0) if compressed else data)
    rows.append({'originalPath': str(source), 'originalSha256': hashlib.sha256(data).hexdigest(),
                 'originalBytes': len(data), 'destination': str(target.relative_to(OUT)),
                 'storedSha256': sha(target), 'storedBytes': target.stat().st_size,
                 'transformation': 'gzip-mtime-zero; original bytes recoverable' if compressed else 'byte-exact'})


assert not OUT.exists()
assert sha(BUILD / 'qualified-handoff.json') == '145064ef35e86fb7581ff5eada0ba61f45b148363a4fee0e543abdfcd4b120af'
assert sha(AUDIT / 'independent-qualification-audit.json') == '5e1361211d0c4aa53ee34a5d8337d767d4778a9b313fcbe774cc3b9383cbe6d9'
assert sha(AUDIT / 'audit.py') == 'dd9f1d65e56624f9b10c1c99747a9642ae8a6dd843165450044eda49cb3a9c5f'
handoff = load(BUILD / 'qualified-handoff.json')
audit = load(AUDIT / 'independent-qualification-audit.json')
assert audit['passed'] and handoff['scratchSourceQualificationPassed']
assert handoff['businessComparisons'] == 196 and handoff['controlledProcesses'] == 23
assert handoff['scalarRows'] == 167315 and not handoff['applicationBuildReleased'] and not handoff['liveRunReleased']
for path, digest in handoff['bindingsSha256'].items():
    assert sha(path) == digest, path
prep = load(PREP / 'preparation-receipt.json')
assert len(prep['filesSha256']) == prep['preparedFiles'] == 44
OUT.mkdir(mode=0o755)

for name, digest in sorted(prep['filesSha256'].items()):
    assert sha(PREP / name) == digest, name
    copy(PREP / name, 'source/' + name)
copy(PREP / 'preparation-receipt.json', 'source/preparation-receipt.json')

allowed = {'business-parity.json', 'controller-receipt.json', 'projects.closed.json', 'projects.jsonl', 'qualification-summary.json'}
result_counts = {}
for source in sorted(RESULTS.rglob('*')):
    if not source.is_file():
        continue
    assert source.name in allowed, source.name
    result_counts[source.name] = result_counts.get(source.name, 0) + 1
    compressed = source.name == 'projects.jsonl'
    copy(source, 'qualification/' + str(source.relative_to(RESULTS)) + ('.gz' if compressed else ''), compressed)
assert result_counts == {'business-parity.json': 23, 'controller-receipt.json': 23, 'projects.closed.json': 21, 'projects.jsonl': 21, 'qualification-summary.json': 1}

for name in ['compile-release.json', 'disabled-preparation-receipt.json', 'receipt.json', 'runtime-facts.json', 'qualified-handoff.json', 'run_build.py']:
    copy(BUILD / name, 'build/' + name)
save('build/log-metadata.json', encode({'rawLogContentsArchived': False, 'logs': [
    {'originalPath': str(BUILD / name), 'sha256': sha(BUILD / name), 'bytes': (BUILD / name).stat().st_size}
    for name in ['restore.log', 'build.log']]}))
copy(AUDIT / 'audit.py', 'audit/audit.py')
copy(AUDIT / 'independent-qualification-audit.json', 'audit/independent-qualification-audit.json')
copy(HISTORY / 'preparation-receipt.json', 'history/59e1534a/preparation-receipt.json')
copy(HISTORY / 'run_differential.py', 'history/59e1534a/run_differential.py')
save('history/reader-guard-correction.json', encode({
    'scope': 'Unexecuted preparation correction, not a failed business/native qualification.',
    'originalHistoryDirectory': str(HISTORY),
    'originalPreparationSha256': sha(HISTORY / 'preparation-receipt.json'),
    'originalControllerSha256': sha(HISTORY / 'run_differential.py'),
    'currentControllerSha256': sha(PREP / 'run_differential.py'),
    'diffPath': 'source/protocol-reader.diff', 'diffSha256': sha(PREP / 'protocol-reader.diff'),
    'offlineOwnPipeSmokePath': 'source/protocol-reader-smoke.json',
    'offlineOwnPipeSmokeSha256': sha(PREP / 'protocol-reader-smoke.json'),
    'change': 'Bounded nonblocking per-child pending-line reader and unbuffered stdout; coalesced lines, partial-line deadlines/EOF, ASCII and child isolation checked.'}))

copy(Path(__file__), 'package.py')
save('provenance.json', encode({'syntheticQualificationOnly': True, 'featureAcceptance': False,
    'copies': rows, 'notArchived': ['compiled binaries/binobj', 'raw logs/traces', 'control file contents',
    'RAM-only dependency/token/DTO/output/error-reference traces', 'caller canary/Activity values',
    'actual application/request/authentication/configuration payloads'],
    'packagingSourcePath': str(Path(__file__)), 'packagingSourceSha256': sha(__file__)}))
readme = """# Advisory terminal recorder: scratch qualification

Status: synthetic source/recorder qualification passed. The accepted handoff records 196 business comparisons (49 per disabled/active/capped/failing-sink mode), 3 fresh first-business flow canaries and 4 protocol cases across 23 controlled processes. These add zero native acceptance cases and release no application build or live run. Feature acceptance remains false.

The packet preserves all 44 prepared inputs, their current receipt, the build/release/facts metadata, the independent audit, and all numeric business/controller/closed receipts. The compiled oracle and scratch assemblies were Debug with JIT optimization disabled on .NET 10.0.14, natural processor count 14 and no runtime processor override. The handoff/audit bind 733 current inputs, source 65 and app147. No DLL/PDB/binobj or log contents are copied; original build-log digests and byte counts are retained.

There are 167,315 strict 14-field scalar rows: Whole 167,266 and SettingsEnumerationSortDictionary 49. Most rows are stress prefill, not additional business cases. The second stage is terminal encoding time, not await-wave duration. Allocation mode is None; the six count slots carry bounded control-flow enum/boolean encodings, not measured business counts. Whole includes admitted capture bookkeeping and the original method lifetime. Neither field establishes serialized outcome, omission cause, timer causality, dependency cause or child quiescence. No ambient/request/root/old-probe joins are claimed.

The independent audit checked 129 closed RAM business terminal snapshots across the recorded modes; active persisted rows agree with their RAM snapshots. The 43 active business pairs and other zero-drop protocol/canary captures are audited separately from stress traffic. Capped cases have deliberate queue/cap drops; caps can lose either member of a pair. Six failing-sink cases recorded 86 I/O failures and wrote zero rows. Their successful business/RAM parity does not prove persistence of every business terminal. Closed accounting/finality and all process closures were independently checked, including zombies; the pinned private directory was the same and empty. The audit did not read or modify the numeric control; controller receipts retain its hash/byte metadata, never its contents.

The unsafe flow positive control observed its synthetic leak. Normal and pre-suppressed first-business canaries did not expose the caller value to the drain and preserved caller context/Activity/flow, with 22 fake-await checks each. Actual canary/Activity values and business/dependency/token/DTO traces remained in RAM. The differential-only class has the explicitly reviewed rename/terminal property assignment; the live private class has neither. Normalization and patch evidence are included.

The prior 59e1534a preparation was unexecuted (compile/execution release false). Its receipt/controller and the current reader diff plus eight-check offline own-pipe smoke preserve the bounded protocol-reader correction history. This is not a failed native or application run. JSONL files are deterministic gzip; provenance preserves their exact original SHA-256 and byte length. No qualification, application or cleanup operation is performed by this package script. Root owns review and any publication.
"""
save('README.md', readme.replace('\n', '\r\n').encode())
files = {str(p.relative_to(OUT)): sha(p) for p in sorted(OUT.rglob('*')) if p.is_file()}
save('manifest.json', encode({'syntheticQualificationOnly': True, 'featureAcceptance': False,
    'fileCountExcludingManifestAndSums': len(files), 'filesSha256': files, 'preparedInputs': 44,
    'businessComparisons': 196, 'freshFlowCanaries': 3, 'protocolCases': 4, 'controlledProcesses': 23,
    'scalarRows': 167315, 'nativeAcceptanceCasesAdded': 0, 'applicationBuildReleased': False,
    'liveRunReleased': False, 'resultArtifactCounts': result_counts}))
files = {str(p.relative_to(OUT)): sha(p) for p in sorted(OUT.rglob('*')) if p.is_file()}
save('SHA256SUMS', ''.join(f'{digest}  {name}\n' for name, digest in files.items()).encode())
print(json.dumps({'destination': str(OUT), 'files': len(files) + 1, 'manifestSha256': sha(OUT / 'manifest.json'),
                  'sumsSha256': sha(OUT / 'SHA256SUMS'), 'readmeSha256': sha(OUT / 'README.md')}))
