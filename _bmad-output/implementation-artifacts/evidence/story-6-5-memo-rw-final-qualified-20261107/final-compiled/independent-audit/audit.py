"""Read-only final compiled image-memo evidence audit. Only a new TEMP report is written.
No app, build, native test, benchmark, profile, target assembly or host is invoked.
"""
from pathlib import Path
from collections import Counter, defaultdict
import datetime, hashlib, json, math, os, re, stat, statistics

PREP = Path('/tmp/story65-image-memo-rw-final-compiled-prep-20261107')
RESULTS = Path('/tmp/story65-image-memo-rw-final-compiled-results-20261107')
EXEC = Path('/tmp/story65-image-memo-rw-final-compiled-execution-20261107/attempt-1')
OUT = Path('/tmp/story65-image-memo-rw-final-compiled-independent-audit-20261107/independent-final-transfer-audit.json')
INITIAL = Path('/tmp/story65-image-memo-rw-prep-20261107')
SUPERVISOR = Path('/tmp/story65-image-memo-rw-final-compiled-run-20261107.py')
PREP_HASH = '7f7d51907f9142d02b00fbc46cf394f820c89812f95234c36d1d74edc0e07464'
BIND_HASH = '9c2f3ebfac36e1fe6be3e1ba591124b1204763cac851ca51eab633369a9c7d12'
SDK_HASH = '6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
PDB_HASH = 'ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
SUPERVISOR_HASH = '4059f0dc1e44c1bb8ba989b365f75fab758689b94bd6c11e7f60a49ab52c0910'


def sha(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK | os.O_CLOEXEC)
    try:
        assert stat.S_ISREG(os.fstat(fd).st_mode)
        with os.fdopen(fd, 'rb', closefd=False) as f: return hashlib.file_digest(f, 'sha256').hexdigest()
    finally: os.close(fd)


def pairs(items):
    d = {}
    for k, v in items:
        assert k not in d, ('duplicate-json-key', k)
        d[k] = v
    return d


def parse(text):
    return json.loads(text, object_pairs_hook=pairs,
        parse_constant=lambda v: (_ for _ in ()).throw(AssertionError(v)))


def read(path): return parse(Path(path).read_bytes())
def natural(v): return type(v) is int and v >= 0


def stats(values):
    values = sorted(values)
    assert values and all(math.isfinite(v) for v in values)
    return dict(Count=len(values), Min=values[0], Median=statistics.median(values),
        P95NearestRank=values[math.ceil(len(values)*.95)-1], Max=values[-1])


def near(a, b):
    if isinstance(a, dict):
        assert isinstance(b, dict) and a.keys() == b.keys()
        for k in a: near(a[k], b[k])
    elif isinstance(a, list):
        assert isinstance(b, list) and len(a) == len(b)
        for x, y in zip(a, b): near(x, y)
    elif type(a) in (float, int) and type(b) in (float, int):
        assert math.isclose(a, b, rel_tol=1e-12, abs_tol=1e-9), (a, b)
    else: assert a == b, (a, b)


assert sha(PREP/'preparation-receipt.json') == PREP_HASH and sha(SUPERVISOR) == SUPERVISOR_HASH
prep = read(PREP/'preparation-receipt.json')
for field, base, count in [('filesSha256',PREP,62),('currentExternalInputsSha256',None,590)]:
    assert len(prep[field]) == count
    for name, h in prep[field].items(): assert sha(base/name if base else name) == h, name
prototype = read(PREP/'prototype-receipt.json')
assert len(prototype['filesSha256']) == 59
assert all(prep['filesSha256'][n] == h for n, h in prototype['filesSha256'].items())
assert prep['executionReleased'] is False and prep['sourceQualificationReady'] is False
binding = read(PREP/'harness/final-binding.json')
assert sha(PREP/'harness/final-binding.json') == prep['bindingSha256'] == BIND_HASH
assert binding['currentInputsSha256'] == prep['currentExternalInputsSha256']
assert binding['nativeQualifiedUniquePasses'] == prep['nativeQualifiedUniquePasses'] == 1048
assert binding['sourceCount'] == prep['sourceControlCount'] == 56
assert binding['sdkDllSha256'] == prep['actualCompiledOracleSdkSha256'] == SDK_HASH
assert binding['sdkPdbSha256'] == prep['sdkPdbSha256'] == PDB_HASH
assert binding['compiledProductionMemoGate'] == 'System.Threading.ReaderWriterLockSlim'
expected_inputs = dict(prep['currentExternalInputsSha256'])
expected_inputs.update({str(PREP/n):h for n,h in prep['filesSha256'].items()})
expected_inputs[str(PREP/'preparation-receipt.json')] = PREP_HASH
before = read(EXEC/'inputs-before.json'); after = read(EXEC/'inputs-after.json')
assert expected_inputs == before == after and len(before) == 653
for name, h in before.items(): assert sha(name) == h
for name, h in prep['filesSha256'].items(): assert sha(EXEC/'prepared-source-snapshot'/name) == h
for folder in ['candidate','control','original']:
    for file in (PREP/folder).iterdir(): assert file.read_bytes() == (INITIAL/folder/file.name).read_bytes()
for name in ['BatchRunner.cs','ObservationRunner.cs','BatchMeasurement.cs','ObservationMeasurement.cs','WorkerMeasurement.cs','InventoryAgreement.cs']:
    assert (PREP/'harness'/name).read_bytes() == (INITIAL/'harness'/name).read_bytes()

command = read(EXEC/'command-receipt.json'); release = read(EXEC/'execution-release.json')
assert command['passed'] and command['preparationSha256'] == release['preparationSha256'] == PREP_HASH
resolved = read(PREP/'resolved-commands.json')['plannedCommands']
assert len(command['steps']) == 3 and release['commands'] == resolved
identities = []
for i, step in enumerate(command['steps']):
    assert step['command'] == resolved[i] and step['exitCode'] == 0
    assert step['guardReason'] is None and step['ownedDiagnosticGroupAbsent'] and step['remainingOwnedDiagnosticGroup'] == []
    assert step['sourceAndAppBuilds'] is False and step['targetSignalsSent'] is False
    assert not step.get('ownedStandaloneDiagnosticChildSignaled', False)
    stem = ['01-restore','02-build','03-benchmark'][i]
    for stream in ['stdout','stderr']: assert sha(EXEC/(stem+'.'+stream+'.log')) == step[stream+'Sha256']
    assert not Path('/proc',str(step['pid'])).exists()
    identities.append(dict(pid=step['pid'],startTicks=step['startTicks'],recordedGroupAbsent=True,
        freshPidAbsent=True,wallSeconds=step['wallSeconds'],peakObservedRssBytes=step['peakObservedRssBytes']))
assert command['steps'][0]['buildProcessorOverride'] == command['steps'][1]['buildProcessorOverride'] == 2
assert command['steps'][2]['runtimeNaturalProcessorsExpected'] == 14
assert command['steps'][2]['buildProcessorOverride'] is None
build_log = (EXEC/'02-build.stdout.log').read_text()
assert re.search(r'\b0 Warning\(s\)',build_log) and re.search(r'\b0 Error\(s\)',build_log)
groups = {s['pid']:[] for s in command['steps']}
for p in Path('/proc').iterdir():
    if not p.name.isdigit(): continue
    try:
        t = (p/'stat').read_text(); f = t[t.rfind(')')+2:].split(); g = int(f[2])
        if g in groups: groups[g].append(int(p.name))
    except (FileNotFoundError,PermissionError,ProcessLookupError): pass
assert all(not members for members in groups.values())
assert not Path('/proc',str(release['supervisorPid'])).exists()

inventory = read(RESULTS/'inventory.json'); files = inventory['Files']
assert inventory['ImageCount'] == len(files) == 66
assert [f['FullName'] for f in files] == sorted(f['FullName'] for f in files)
old_metadata = read(INITIAL/'cohort-metadata/current-cohort-metadata.json')
assert [(f['FullName'],f['Path']) for f in files] == [(f['fullName'],f['path']) for f in old_metadata['entries']]
changed_inputs = []
for ordinal, (f, old_f) in enumerate(zip(files,old_metadata['entries'])):
    assert f['Ordinal'] == ordinal and prep['currentExternalInputsSha256'][f['Path']] == f['Sha256'] == sha(f['Path'])
    assert Path(f['Path']).stat().st_size == f['Bytes'] and 0 < f['Bytes'] <= 16*1024*1024
    assert f['ExpectedTechnicalIdentity'] == f"{f['FullName']}:{f['ModuleVersionId']}:{f['Sha256']}"
    if f['Sha256'] != old_f['sha256']: changed_inputs.append(dict(fullName=f['FullName'],oldSha256=old_f['sha256'],currentSha256=f['Sha256']))
assert len({f['FullName'] for f in files}) == len({f['Path'] for f in files}) == 66
assert inventory['Bytes'] == sum(f['Bytes'] for f in files) == 27656504
charge = sum(f['Bytes']+512 for f in files)
assert inventory['ChargedBytes'] == charge == 27690296 and inventory['EntireCohortFitsOriginalBounds']
sdk_image = [f for f in files if f['FullName'].startswith('Hexalith.EventStore.ServiceDefaults,')]
assert len(sdk_image) == 1 and sdk_image[0]['Sha256'] == SDK_HASH
parity = read(RESULTS/'parity.json'); result = read(RESULTS/'result.json')
assert parity == dict(Checks=621,Passed=621,AllPassed=True)
assert result['Completed'] and result['AllPassed'] and result['RawSamples'] == 324 and result['FeatureAcceptance'] is False
facts = read(RESULTS/'runtime-facts.json')
assert facts['Runtime'] == '10.0.14' and facts['ProcessorCount'] == 14 and facts['ProcessorCountOverrideAbsent']
assert facts['IsServerGC'] and facts['OriginalOwnerServerGc'] and facts['ForcedGcCalls'] == 0
assert sha(facts['OriginalOwnerRuntimeConfigPath']) == facts['OriginalOwnerRuntimeConfigSha256']
paths = read(RESULTS/'selected-variant-path-proofs.json')
assert [p['Name'] for p in paths] == ['production','monitor-control','rw-copy']
for p in paths:
    assert p['ScopeTypeProvenByActualObservation'] and p['DomainStreamAndTechnicalSelectedPathProvenByActualHitsAndOccupancy']
    assert p['DomainPathUsesSelectedFreshHelperNotDomainServiceMethod']
    assembly = p['Assembly']; assert sha(assembly['Location']) == assembly['Sha256']
    assert assembly['Configuration'] == 'Debug' and assembly['IsJITOptimizerDisabled']
    assert assembly['Sha256'] == (SDK_HASH if p['Name']=='production' else facts['Harness']['Sha256'])
assert sha(PREP/'harness/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.pdb') == PDB_HASH
actual_binding = read(RESULTS/'native-qualified-production-binding.json')
assert actual_binding['BindingSha256'] == BIND_HASH and actual_binding['ActualCompiledProductionSdkSha256'] == SDK_HASH
assert actual_binding['SourceCount'] == 56 and actual_binding['NativeQualifiedUniquePasses'] == 1048
assert actual_binding['CurrentPinnedInputs'] == 590
agreement = read(RESULTS/'production-monitor-control-rw-copy-agreement.json')
assert len(agreement) == 12 and {r['Variant'] for r in agreement} == {'production','monitor-control','rw-copy'}
for r in agreement:
    hits = 0 if r['Path']=='domain' and r['Repeat']==0 else 66
    assert r['CompleteIdentityOutputsAgreeWithIndependentExpectedValues']
    assert r['Images'] == r['MemoCount'] == 66 and r['MemoChargedBytes'] == charge
    assert r['ReportedMemoHits'] == hits and r['FreshResultsWithoutReportedReuse'] == 66-hits

row_keys = set('Variant Path Phase Round Order Readers IterationsPerReader CompleteObservations ImagesPerObservation StopwatchFrequency ElapsedTicks ProcessCpuTicks ProcessAllocatedBytes GcCollectionDeltas MemoCountBefore MemoCountAfter ChargedBytesBefore ChargedBytesAfter EntireCohortFitsOriginalBounds ReportedMemoHits FreshResultsWithoutReportedReuse NonHitsDistinguishedFromRejections Workers'.split())
worker_keys = set('Worker ElapsedTicks ThreadAllocatedBytes FailureType Observations'.split())
observation_keys = set('Worker Iteration ElapsedTicks ThreadAllocatedBytes ReportedMemoHits FreshResultsWithoutReportedReuse'.split())
orders = [('production','monitor-control','rw-copy'),('rw-copy','monitor-control','production'),('monitor-control','rw-copy','production'),
          ('production','rw-copy','monitor-control'),('rw-copy','production','monitor-control'),('monitor-control','production','rw-copy')]
rows = [parse(line) for line in (RESULTS / 'benchmark-samples.jsonl').read_text().splitlines()]
assert len(rows) == 324
expected_order = [(path,n,round_,order,phase) for path in ['domain','technical'] for n in [1,2,4]
    for round_ in range(6) for order in range(3) for phase in ['memo-cold','warmup','warm']]
assert [(r['Path'],r['Readers'],r['Round'],r['Order'],r['Phase']) for r in rows] == expected_order
observations = hits = nonhits = 0
for r in rows:
    assert set(r) == row_keys and r['Variant'] == orders[r['Round']][r['Order']]
    n = r['Readers']; it = {'memo-cold':1,'warmup':3,'warm':12}[r['Phase']]
    assert r['IterationsPerReader'] == it and r['CompleteObservations'] == n * it
    assert r['ImagesPerObservation'] == 66 and r['StopwatchFrequency'] == 1000000000
    for k in ['ElapsedTicks','ProcessCpuTicks','ProcessAllocatedBytes']: assert natural(r[k])
    assert len(r['GcCollectionDeltas']) == 3 and all(natural(v) for v in r['GcCollectionDeltas'])
    assert r['EntireCohortFitsOriginalBounds'] and r['NonHitsDistinguishedFromRejections']
    cold = r['Phase'] == 'memo-cold'
    assert r['MemoCountBefore'] == (0 if cold else 66) and r['ChargedBytesBefore'] == (0 if cold else charge)
    assert r['MemoCountAfter'] == 66 and r['ChargedBytesAfter'] == charge
    assert len(r['Workers']) == n
    row_hits = 0
    for w, worker in enumerate(r['Workers']):
        assert set(worker) == worker_keys and worker['Worker'] == w and worker['FailureType'] is None
        assert natural(worker['ElapsedTicks']) and natural(worker['ThreadAllocatedBytes'])
        assert worker['ElapsedTicks'] <= r['ElapsedTicks'] and len(worker['Observations']) == it
        assert sum(o['ElapsedTicks'] for o in worker['Observations']) <= worker['ElapsedTicks']
        for i, observation in enumerate(worker['Observations']):
            assert set(observation) == observation_keys
            assert observation['Worker'] == w and observation['Iteration'] == i
            assert all(natural(observation[k]) for k in ['ElapsedTicks','ThreadAllocatedBytes','ReportedMemoHits','FreshResultsWithoutReportedReuse'])
            assert observation['ReportedMemoHits'] + observation['FreshResultsWithoutReportedReuse'] == 66
            if not cold: assert observation['ReportedMemoHits'] == 66
            row_hits += observation['ReportedMemoHits']
    assert row_hits == r['ReportedMemoHits'] == (n * it - int(cold)) * 66
    assert r['FreshResultsWithoutReportedReuse'] == n * it * 66 - row_hits
    observations += n * it; hits += row_hits; nonhits += r['FreshResultsWithoutReportedReuse']
assert observations == 4032 and observations * 66 == 266112


def metrics(r):
    obs = [o for w in r['Workers'] for o in w['Observations']]
    latencies = [o['ElapsedTicks'] * 1000 / r['StopwatchFrequency'] for o in obs]
    return dict(batchWallMilliseconds=r['ElapsedTicks'] * 1000 / r['StopwatchFrequency'],
        processCpuMilliseconds=r['ProcessCpuTicks'] / 10000,
        processAllocatedBytesPerObservation=r['ProcessAllocatedBytes'] / r['CompleteObservations'],
        observationMedianMilliseconds=statistics.median(latencies),
        observationP95Milliseconds=stats(latencies)['P95NearestRank'])


grouped = defaultdict(list)
for r in rows: grouped[(r['Variant'],r['Path'],r['Readers'],r['Phase'])].append(r)
recomputed = []
for (variant,path,n,phase), rs in grouped.items():
    recomputed.append(dict(Variant=variant, Path=path, Readers=n, Phase=phase, Samples=len(rs),
        BatchWallMilliseconds=stats(metrics(r)['batchWallMilliseconds'] for r in rs),
        ProcessCpuMilliseconds=stats(metrics(r)['processCpuMilliseconds'] for r in rs),
        ProcessAllocatedBytesPerObservation=stats(metrics(r)['processAllocatedBytesPerObservation'] for r in rs),
        ObservationWallMilliseconds=stats(o['ElapsedTicks']*1000/r['StopwatchFrequency'] for r in rs for w in r['Workers'] for o in w['Observations']),
        GcCollectionsByGeneration=[sum(r['GcCollectionDeltas'][g] for r in rs) for g in range(3)],
        TotalReportedMemoHits=sum(r['ReportedMemoHits'] for r in rs),
        TotalFreshResultsWithoutReportedReuse=sum(r['FreshResultsWithoutReportedReuse'] for r in rs),
        NonHitsDistinguishedFromRejections=all(r['NonHitsDistinguishedFromRejections'] for r in rs)))
summary = read(RESULTS / 'benchmark-summary.json')
near(recomputed, summary['Results'])
comparisons = []
for path in ['domain','technical']:
    for phase in ['memo-cold','warmup','warm']:
        for n in [1,2,4]:
            index = {(r['Variant'],r['Round']):metrics(r) for r in rows if r['Path']==path and r['Phase']==phase and r['Readers']==n}
            # Reuse strict metric calculations, but compare actual production with each independent copied variant.
            for baseline in ['monitor-control','rw-copy']:
                changes = {k:[100*(index['production',i][k]/index[baseline,i][k]-1) for i in range(6)] for k in index[baseline,0]}
                comparisons.append(dict(path=path,phase=phase,readers=n,baseline=baseline,
                    productionMedian={k:statistics.median(index['production',i][k] for i in range(6)) for k in changes},
                    baselineMedian={k:statistics.median(index[baseline,i][k] for i in range(6)) for k in changes},
                    pairedPercentChange={k:dict(stats(values),productionLowerRounds=sum(v<0 for v in values),sixRoundValues=values) for k,values in changes.items()}))
bindings = {str(p):sha(p) for p in sorted(RESULTS.iterdir()) if p.is_file()}
for p in [PREP/'preparation-receipt.json',PREP/'harness/final-binding.json',PREP/'prototype-receipt.json',
    PREP/'resolved-commands.json',EXEC/'command-receipt.json',EXEC/'execution-release.json',
    EXEC/'inputs-before.json',EXEC/'inputs-after.json',SUPERVISOR]: bindings[str(p)] = sha(p)
compiled = {str(p):sha(p) for p in sorted((PREP/'harness/bin/Debug/net10.0').iterdir()) if p.is_file() and p.suffix in {'.dll','.pdb','.json'}}
audit = dict(auditUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),passed=True,diagnosticOnly=True,
    actualCompiledProductionTransferMeasured=True,liveCriticalPathOrHostDeadlineGainClaimed=False,
    auditSourceSha256=sha(__file__),evidenceSha256=bindings,compiledOutputSha256=compiled,
    frozenInputs=dict(preparationSha256=PREP_HASH, bindingSha256=BIND_HASH,
        all62Prepared590ExternalCurrent=True,all653ExactFrozenUnionBeforeEqualsAfterEqualsCurrent=True,
        all62ExecutedSourceSnapshotHashesMatch=True,prototype59Unchanged=True,
        oldCopiedHelpersAndTimingMeasurementBodiesByteExact=True,sourceControlCount=56),
    execution=dict(supervisorSha256=SUPERVISOR_HASH,allThreeCommandsExitZero=True,buildWarnings=0,buildErrors=0,
        diagnosticProcesses=identities,freshProcessGroups=groups,supervisorFreshPidAbsent=True,
        noSignalsSent=True,noApplicationBuildNativeTestHostProfileOrSourceEdit=True),
    sourceAndNativeBinding=dict(ownerReceiptSha256=binding['ownerReceiptSha256'],nativeAuditSha256=binding['nativeAuditSha256'],
        sourceControlsSha256=binding['sourceControlsSha256'],nativeAlreadyQualifiedUniquePasses=1048,
        sourceControls=56,compiledSdkSha256=SDK_HASH,compiledSdkPdbSha256=PDB_HASH,
        actualSdkDefaultContextMatchedTypedPath=True,productionRuntimeRwlGateCheckPassed=True,
        copiedMonitorAndRwVariantPathsProven=True,
        scratchParityChecksNotAddedToNativePasses=True),
    cohort=dict(images=66,bytes=27656504,chargedBytes=charge,allImagesEligibleAndBoundsFit=True,
        sameOrdinalIdentityAndPhysicalPathSelectionAsInitialCorrected66=True,
        everyPhysicalFileCurrentShaBoundIn590Inputs=True,imagesWhoseBytesChangedSinceInitialExperiment=changed_inputs,
        sourceDerivedOwnerDomainDirectReferencesOnly=True,completeRuntimeDiscoveryUnionClaimed=False),
    behavioralQualification=dict(parityChecks=621,parityPassed=621,selectedVariantProofs=3,completeIdentityAgreementRows=12,
        nativePassInflation=0,naturalProcessors=14,runtime='10.0.14',matchedDebugDisableOptimizations=True,
        ownerMatchedServerGc=True,forcedGc=0,actualHarnessSha256=facts['Harness']['Sha256']),
    rawReconciliation=dict(strictDuplicateKeysAndNestedSchema=True,batches=324,observations=observations,
        imageCalls=observations*66,memoHits=hits,freshNonReusedResults=nonhits,
        independentlyReproducedSummaryGroups=len(recomputed),allChargesAndHitCountsExact=True,
        sixBalancedVariantPermutationsEachPathReaderCount=True,allWorkerIterationsAndTimingsValid=True),
    timingComparisons=comparisons,
    gcCollections=[dict(variant=v,path=path,readers=n,phase=phase,
        summedSixBatches=[sum(r['GcCollectionDeltas'][g] for r in rs) for g in range(3)])
        for (v,path,n,phase),rs in grouped.items()],
    interpretation=[
        'Compiled production retains warm concurrent net whole-cohort benefit versus copied Monitor: Domain and Technical batch wall, process CPU, observation median and per-batch p95 are lower in every one of six balanced 2- and 4-reader rounds.',
        'Domain warm median batch wall is 60.586 vs86.574ms at2readers and68.545 vs114.520ms at4readers; process CPU121.083 vs148.768ms and264.047 vs282.451ms. These are separate distributions; median paired changes differ from ratios of medians.',
        'Production and RW-copy outputs agree exactly, but timing equality is not established. Domain warm4 production p95 is higher in all6 rounds, paired +1.67% to+27.71%; Domain2/4 production batch wall is higher in4of6 rounds. Technical warm production/RW-copy batch wall ranges overlap closely.',
        'Single-reader warm Domain batch wall median is51.582 vs50.486ms Monitor (+2.17% ratio), with3of6 paired wins and mixed tails/CPU. No general single-reader speedup.',
        'Cold results are mixed; Technical cold1 median wall21.716 vs16.890ms Monitor (+28.6% ratio), and cold4 median wall26.977 vs22.802ms and CPU48.997 vs39.596ms. Production is slower than Monitor in5of6 cold1 wall rounds and4of6 cold4 wall rounds. These observed costs remain reported.',
        'Warm Domain4 process allocated bytes/observation is lower versus Monitor in all6 rounds (paired median-16.46%). Other allocation changes are small; no general memory mechanism is established.',
        'Natural GC differs across sequential batches, particularly cold Technical; no causal attribution or removal of outliers. No forced collections, cold filesystem, fresh-process-per-sample isolation, confidence interval or guaranteed scheduling fairness is claimed.',
        'Six balanced rounds come from one process/session; paired comparisons are sequential measurements, not independent randomized repetitions. Process CPU/allocation include background/GC and batch release/join bookkeeping.',
        'All three variants consume the same current66 files, including the new SDK512 bytes larger than the initial compiled SDK. Cross-generation wall medians are not treated as identical-input repetitions.',
        'This is complete selected66-image computation with preserved full fresh file/EOF/PE/MVID reads and bounded scope/memo behavior. Domain adapter calls the selected helper, not the original DomainService entry method. Configuration, authority/discovery, leases, service transport and the2second distributed deadline remain outside the measured workload.',
        '621 scratch behavioral checks validate the final transfer and are never added to the already-qualified1048 native cases. No live acceptance or whole-host critical-path gain follows from this benchmark alone.'
    ])
assert not OUT.exists()
OUT.write_text(json.dumps(audit,indent=2,allow_nan=False)+'\n')
print(json.dumps(dict(passed=True,auditPath=str(OUT),auditSha256=sha(OUT),auditSourceSha256=sha(__file__),
    inputPaths=653,batches=324,observations=observations,imageCalls=observations*66,hits=hits,nonReused=nonhits,
    summaryGroups=len(recomputed),compiledSdkSha256=SDK_HASH),indent=2))
