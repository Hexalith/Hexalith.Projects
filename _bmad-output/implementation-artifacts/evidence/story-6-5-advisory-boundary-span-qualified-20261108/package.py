"""File-only staging of the accepted synthetic boundary-span qualification."""
from pathlib import Path
import gzip
import hashlib
import json

PREP = Path('/tmp/story65-advisory-boundary-span-prep-20261108')
RESULTS = Path('/tmp/story65-advisory-boundary-span-qualification-20261108')
BUILD = Path('/tmp/story65-advisory-boundary-span-build-20261108')
AUDIT = Path('/tmp/story65-advisory-boundary-span-audit-prep-20261108')
HISTORY = Path('/tmp/story65-advisory-boundary-span-history-20261108')
AUDIT_HISTORY = Path('/tmp/story65-advisory-boundary-span-audit-history-20261108/ebe3fb76')
OUT = Path('/tmp/story65-advisory-boundary-span-qualified-publication-prep-20261108')
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
assert sha(BUILD / 'qualified-handoff.json') == '2b0a08f800d415dd9a280b0b68feeaedecc5084b7eba740af4a84b96643c4ba9'
assert sha(AUDIT / 'independent-qualification-audit.json') == '5463d181d60f69547b3faf218da6e910a02e77b3b53f861d2e981fbd14f001ab'
assert sha(AUDIT / 'audit.py') == '52c39e5717f45fab3971ce58816115e3e33403c3c5ae754d7a0e203fba01f302'
handoff = load(BUILD / 'qualified-handoff.json')
audit = load(AUDIT / 'independent-qualification-audit.json')
assert audit['passed'] and handoff['scratchSourceQualificationPassed']
assert handoff['businessComparisons'] == 196 and handoff['controlledProcesses'] == 23
assert handoff['scalarRows'] == 167522 and handoff['stateProtocolCases'] == 14
assert not handoff['applicationBuildReleased'] and not handoff['liveRunReleased']
for path, digest in handoff['bindingsSha256'].items():
    assert sha(path) == digest, path
prep = load(PREP / 'preparation-receipt.json')
assert len(prep['filesSha256']) == prep['preparedFiles'] == 50
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

for name in ['compile-release-disabled.json', 'compile-release.json', 'disabled-preparation-receipt.json', 'receipt.json', 'runtime-facts.json', 'qualified-handoff.json', 'run_build.py']:
    copy(BUILD / name, 'build/' + name)
save('build/log-metadata.json', encode({'rawLogContentsArchived': False, 'logs': [
    {'originalPath': str(BUILD / name), 'sha256': sha(BUILD / name), 'bytes': (BUILD / name).stat().st_size}
    for name in ['restore.log', 'build.log']]}))
copy(AUDIT / 'audit.py', 'audit/audit.py')
copy(AUDIT / 'independent-qualification-audit.json', 'audit/independent-qualification-audit.json')
for generation, names in [('3f5d27db', ['preparation-receipt.json', 'AdvisoryTerminalCapture.cs', 'stage-map.json']),
                          ('e4320689', ['preparation-receipt.json', 'controller-plan.json'])]:
    for name in names:
        copy(HISTORY / generation / name, 'history/' + generation + '/' + name)
for name in ['audit.py', 'baseline-reference.json']:
    copy(AUDIT_HISTORY / name, 'history/ebe3fb76/' + name)
save('history/correction-index.json', encode({
    'allThreeRevisionsUnexecuted': True, 'notFailedMeasurementsOrAuditRuns': True,
    'onlyExecutedBoundaryAuditSha256': sha(AUDIT / 'audit.py'),
    'onlyExecutedBoundaryAuditReportSha256': sha(AUDIT / 'independent-qualification-audit.json'),
    'revisions': [
        {'revision': '3f5d27db', 'originalDirectory': str(HISTORY / '3f5d27db'),
         'receiptSha256': sha(HISTORY / '3f5d27db/preparation-receipt.json'),
         'correction': 'Selected stage7 changed from FinalScope to Index; FinalScope remains a stop marker and starts none.'},
        {'revision': 'e4320689', 'originalDirectory': str(HISTORY / 'e4320689'),
         'receiptSha256': sha(HISTORY / 'e4320689/preparation-receipt.json'),
         'correction': 'Planned protocolCases metadata corrected4 to14 (retained4 plus new10); no C# or runtime change.'},
        {'revision': 'ebe3fb76', 'originalDirectory': str(AUDIT_HISTORY),
         'auditSha256': sha(AUDIT_HISTORY / 'audit.py'),
         'correction': 'Unexecuted audit assumed two nonexistent Root receipt fields; corrected to direct disabled-copy hash and compiled receipt binding before the single52c PASS. Final audit also checks facts PGID/SID without inventing startTicks.'}]}))

copy(Path(__file__), 'package.py')
save('provenance.json', encode({'syntheticQualificationOnly': True, 'featureAcceptance': False,
    'copies': rows, 'notArchived': ['compiled binaries/binobj', 'raw logs/traces', 'control file contents',
    'RAM-only dependency/token/DTO/output/error-reference traces', 'caller canary/Activity values',
    'actual application/request/authentication/configuration payloads'],
    'packagingSourcePath': str(Path(__file__)), 'packagingSourceSha256': sha(__file__)}))
readme = """# Advisory boundary spans: scratch qualification

Status: synthetic source/recorder qualification passed. The accepted handoff records 196 business comparisons (49 per disabled/active/capped/failing-sink mode), 3 fresh first-business flow canaries and 14 protocol cases (4 retained terminal cases plus 10 new boundary-span cases) across 23 controlled processes. These add zero native cases and release no application build or live run. Feature acceptance remains false.

The packet retains 50 prepared source/plan files, runtime receipt b7662b65, disabled source receipt 37dbcd22, build/release/facts metadata, the independent audit 52c39e57/report 5463d181 and Root handoff 2b0a08f8. The audit checked 766 current pins: 50 prepared, 381 external and 335 compiled-closure bindings, including source 65/current app147 and the compiled oracle. Only hashes/manifests of compiled files are retained, never DLL/PDB/binobj contents. Oracle and scratch were Debug with JIT optimization disabled on .NET 10.0.14/natural 14 and no runtime CPU override. Current output hashes do not claim equivalence to every historical 733 input generation or old native outputs.

Helper 112/bootstrap 8f58/private executor d36 remain byte-exact. Only the adapter adds six fixed boundary intervals. Stage 1 ComponentFile starts InitialScope; 2 ComponentPolicy starts InitialInputs; 7 ResultTail starts Index; 3 AclFile starts Discovery; 5 AuthenticationSerialization starts FinalInputs; 6 ActorBoundary starts FinalCandidates. Every valid existing marker ends the preceding interval, including unselected EnvelopeValidation, FinalScope, Conversation and Response markers. First catch notification/close/dispose also stop it. Repeated selected markers disable later spans instead of overwriting. At most eight rows can be emitted per admitted capture, including Whole 0 and unchanged terminal encoding 4.

Intervals run between recorder timestamps, include intervening original work and diagnostic bookkeeping, and are not await-only durations or proof a candidate callback began or quiesced. Unselected-marker gaps remain unattributed. Whole minus selected intervals does not identify any one omitted activity. Whole includes admitted capture bookkeeping after its timestamp and the original method lifetime; stage 4 measures terminal encoding only. The six count slots remain bounded terminal enum/boolean encodings, not business counts; allocation columns are zero. No request/identity/expiry joins, previous-probe ordinal joins, payload outcome, omission cause, dependency cause or deadline cause are inferred.

The audit reconciled 167,522 strict 14-field rows: Whole 167,249; stage 1=47, 2=41, 3=28, 4=59, 5=32, 6=32, 7=34. Most rows are stress prefill, not additional business cases. It checked 129 closed RAM business terminal snapshots, 59 zero-drop capture groups, exact original-branch stage masks and RAM-to-row duration equality, uniqueness and selected-span-plus-encoding sum no greater than Whole. Ten focused adapter cases cover held intervals/omitted gaps, second failure while retaining the first failure, idempotent close, repetition, invalid enums, cancellation and late events. Their exact scheduler holds remained RAM-only; the independent audit binds their reviewed source and passed receipts rather than reconstructing those schedules.

Capped cases have deliberate queue/cap drops; caps can lose any part of a capture. Six failing-sink cases recorded 264 I/O failures and wrote zero rows. Passing business/RAM parity under loss does not prove every terminal/span persisted. The 23-field closed receipts reconcile writes, drops and I/O. Missing/zero/dropped spans are unobserved. At qualification audit time all 23 PID/PGID/SID identities, build groups and facts group/session were freshly absent including zombies, and the pinned private directory was the same and empty. Facts-process start ticks were not recorded or invented. The audit neither read nor modified numeric control contents; its hash was reconstructed from closed numeric fields.

Three unexecuted correction histories are preserved separately: 3f5d27db selected FinalScope before the reviewed Index substitution; e4320689 had only a planned protocol count 4 corrected to 14; ebe3fb76 was an unexecuted audit template with two incorrect Root receipt-field assumptions, corrected before the sole 52c PASS. These are not failed measurements or failed audit runs. No additional qualification execution occurred during packaging.

All 89 numeric result artifacts are retained; 21 scalar JSONL files use deterministic gzip with exact original SHA/byte length in provenance. Source and closed receipts contain no RAM-only dependency/token/DTO/output/error-reference traces or actual canary/Activity values. Binaries, raw logs/traces, control contents, runtime URLs/authentication/configuration payloads and private identities are excluded. Build log contents are omitted but their original hashes/lengths remain. Root owns review and publication; this temporary package starts no test, build, application, profiler or host.
"""
save('README.md', readme.replace('\n', '\r\n').encode())
files = {str(p.relative_to(OUT)): sha(p) for p in sorted(OUT.rglob('*')) if p.is_file()}
save('manifest.json', encode({'syntheticQualificationOnly': True, 'featureAcceptance': False,
    'fileCountExcludingManifestAndSums': len(files), 'filesSha256': files, 'preparedInputs': 50,
    'businessComparisons': 196, 'freshFlowCanaries': 3, 'protocolCases': 14, 'retainedTerminalProtocolCases': 4, 'newBoundarySpanCases': 10, 'controlledProcesses': 23,
    'scalarRows': 167522, 'nativeAcceptanceCasesAdded': 0, 'applicationBuildReleased': False,
    'liveRunReleased': False, 'resultArtifactCounts': result_counts}))
files = {str(p.relative_to(OUT)): sha(p) for p in sorted(OUT.rglob('*')) if p.is_file()}
save('SHA256SUMS', ''.join(f'{digest}  {name}\n' for name, digest in files.items()).encode())
print(json.dumps({'destination': str(OUT), 'files': len(files) + 1, 'manifestSha256': sha(OUT / 'manifest.json'),
                  'sumsSha256': sha(OUT / 'SHA256SUMS'), 'readmeSha256': sha(OUT / 'README.md')}))
