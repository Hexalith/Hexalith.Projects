import collections
import hashlib
import json
import os
import pathlib
import stat

PREP = pathlib.Path('/tmp/story65-outbound-assertion-prep-20261107')
RESULTS = PREP / 'results-qualified-initial'
ROOT = pathlib.Path('/home/administrator/projects/hexalith/projects')
PRIVATE = pathlib.Path('/tmp/story65-capture-counter-private-20261107')

def sha(p):
    return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()

def unique(items):
    result = {}
    for key, value in items:
        assert key not in result
        result[key] = value
    return result

def read(p):
    return json.loads(pathlib.Path(p).read_bytes(), object_pairs_hook=unique)

assert sha(PREP / 'preparation-receipt.json') == '6814b1a4a1e0658c809d9d8108ef6ff48292783f2605ac2b5dc84381f21c1f30'
pins = read(PREP / 'preparation-receipt.json')
for section, base in [('filesSha256', PREP), ('sourceSha256', ROOT), ('app147Sha256', ROOT)]:
    for name, digest in pins[section].items():
        assert sha(base / name) == digest, name
shared = read(PREP / 'shared-input-pins.json')['inputsSha256']
for name, digest in shared.items():
    assert sha(name) == digest, name

summary = read(RESULTS / 'controller-summary.json')
plan = read(PREP / 'controller-plan.json')['plannedProcesses']
assert summary['allPassed'] and summary['scratchQualificationOnly'] and not summary['featureAcceptance']
assert summary['completedProcesses'] == summary['plannedProcesses'] == len(plan) == 15
zero = {'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
fields = zero | {'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
stages = {'Whole','ComponentFile','ComponentPolicy'}
cells = collections.Counter()
rows = 0
processes = []
all_inputs = {}
for index, (planned, entry) in enumerate(zip(plan, summary['receipts'])):
    assert (entry['mode'], entry['group'], entry['case']) == (planned['mode'], planned['group'], planned['case'])
    path = pathlib.Path(entry['path'])
    assert path == RESULTS / planned['mode'] / (planned['case'] or planned['group']) / 'controller-receipt.json'
    receipt = read(path)
    assert receipt['passed'] and receipt['started'] and receipt['allInputsUnchanged']
    assert receipt['processAndGroupAbsent'] and receipt['directoryReleased'] and receipt['remainingOwnedGroupIdentities'] == []
    assert receipt['exitCode'] == 0 and receipt['errorType'] == 'none'
    assert receipt['runtimeProcessorOverride'] is None and receipt['nativeCasesExecuted'] == receipt['hostsStarted'] == 0
    assert not receipt['rawDependencyTracePersisted'] and not receipt['controlContentsPersisted']
    assert receipt['inputsBeforeSha256'] == receipt['inputsAfterSha256']
    for name, digest in receipt['inputsAfterSha256'].items():
        if name in all_inputs:
            assert all_inputs[name] == digest
        all_inputs[name] = digest
    business_path = path.parent / 'business-parity.json'
    assert sha(business_path) == receipt['businessReceiptSha256']
    business = read(business_path)
    assert business['Cases'] == business['Passed'] == planned['expectedCatalogCases']
    assert business['ErrorType'] == 'none'
    if planned['group'] == 'canary':
        positive = planned['mode'] == 'canary-positive-control'
        assert business['DrainCanaryObserved'] == business['PositiveControl'] == positive
        assert business['AlreadySuppressedBranch'] == (planned['mode'] == 'canary-suppressed')
        assert all(business[k] for k in ['CallerCanaryUnchanged','CallerActivityUnchanged','CallerFlowUnchanged','VariantMethodIdentityQualified'])
        assert not business['ActualCanaryOrActivityValuePersisted']
        assert business['WorkerProgressAndClosedReceipt'] == (not positive)
    else:
        cells[planned['mode']] += business['Cases']
        assert business['Failures'] == [] and business['VariantIdentitiesQualified'] and business['SharedDependenciesQualified']
        assert not business['TraceValuesPersisted'] and business['AllocationMode'] == 'None' and business['CountCalls'] == 0
    observed_stages = collections.Counter()
    observed_pairs = set()
    local_rows = 0
    for name, artifact in receipt['artifacts'].items():
        if name == 'window.control':
            assert not artifact['contentsArchived'] and not (path.parent / name).exists()
            continue
        artifact_path = path.parent / name
        assert artifact['contentsArchived'] and artifact_path.stat().st_size == artifact['bytes']
        assert sha(artifact_path) == artifact['sha256']
    if 'closed' in receipt:
        closed = read(path.parent / 'gateway.closed.json')
        assert closed == receipt['closed']
        assert closed['admitted'] == closed['disposedOrUnwound']
        assert all(closed[k] == 0 for k in ['activeAtDrainEnd','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease'])
        assert closed['countersFinal'] == closed['diagnosticOnly'] == 1 and closed['featureAcceptance'] == 0
        assert (closed['maximumRows'],closed['maximumBytes'],closed['queueCapacity']) == (65536,8388608,4096)
        assert closed['windowEndTicks'] - closed['windowStartTicks'] == closed['frequency'] * 8
        raw_path = path.parent / 'gateway.jsonl'
        for line in raw_path.read_bytes().splitlines():
            row = json.loads(line, object_pairs_hook=unique)
            assert set(row) == fields and row['role'] == 'gateway' and row['stage'] in stages
            assert all(type(row[k]) is int for k in fields - {'role','stage'})
            assert all(row[k] == 0 for k in zero)
            assert row['runOrdinal'] == closed['runOrdinal'] and row['captureOrdinal'] > 0
            assert row['frequency'] == closed['frequency'] and row['elapsedTicks'] >= 0
            pair = (row['captureOrdinal'], row['stage'])
            assert pair not in observed_pairs
            observed_pairs.add(pair)
            observed_stages[row['stage']] += 1
            local_rows += 1
        assert local_rows == closed['writtenRows'] == receipt['rows']
        assert raw_path.stat().st_size == closed['writtenBytes']
        assert dict(observed_stages) == receipt['stageCounts']
        if planned['mode'] == 'capped':
            assert closed['capRowsDropped'] > 0 and closed['ioFailures'] == 0
        elif planned['mode'] == 'failing-sink':
            assert closed['ioFailures'] > 0 and closed['writtenRows'] == 0
        else:
            assert all(closed[k] == 0 for k in ['queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures'])
        rows += local_rows
    else:
        assert receipt['rows'] == 0 and receipt['artifacts'] == {}
    identity = receipt['identity']
    processes.append(identity)
    proc = pathlib.Path('/proc') / str(identity['pid']) / 'stat'
    if proc.exists():
        values = proc.read_text().rsplit(')', 1)[1].split()
        assert values[19] != identity['startTicks'] or values[0] == 'Z'
    for proc in pathlib.Path('/proc').glob('[0-9]*/stat'):
        try:
            values = proc.read_text().rsplit(')', 1)[1].split()
            assert int(values[2]) != identity['processGroup'] or values[0] == 'Z'
        except (FileNotFoundError, ProcessLookupError, PermissionError):
            pass
assert cells == dict(disabled=25, active=25, capped=25, **{'failing-sink':25})
assert len({p['pid'] for p in processes}) == 15
for name, digest in all_inputs.items():
    assert sha(name) == digest, name
s = PRIVATE.lstat()
assert stat.S_ISDIR(s.st_mode) and (s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode)) == (2096,8752986,1000,0o700)
assert not list(PRIVATE.iterdir())
output = dict(scratchQualificationOnly=True, featureAcceptance=False, allPassed=True, cells=dict(cells), canaries=3, processesAbsent=15, independentlyParsedRows=rows, currentInputPins=len(all_inputs), prepFiles=len(pins['filesSha256']), sourceControls=len(pins['sourceSha256']), applicationDlls=len(pins['app147Sha256']), sharedInputs=len(shared), samePrivateDirectoryEmpty=True, noApplicationBuildsOrNativeTestsOrHosts=True)
target = pathlib.Path('/tmp/story65-outbound-assertion-qualification-20261107/parent-scratch-audit.json')
target.write_text(json.dumps(output,indent=2)+'\n')
print(json.dumps(output,indent=2))
