"""Independent read-only evidence audit; writes only its new TEMP JSON report.
No application, assembly load, build, test, profile or benchmark is invoked.
"""
from pathlib import Path
from collections import Counter, defaultdict
import datetime, hashlib, json, math, os, re, stat, statistics

PREP = Path('/tmp/story65-image-memo-rw-prep-20261107')
RESULTS = Path('/tmp/story65-image-memo-rw-results-retry-20261107')
EXEC = Path('/tmp/story65-image-memo-rw-execution-20261107/attempt-2')
OLD = Path('/tmp/story65-image-memo-rw-prep-history-20261107/e69bc812')
ROOT = Path('/home/administrator/projects/hexalith/projects')
OUT = Path('/tmp/story65-image-memo-rw-independent-audit-20261107/independent-benchmark-audit.json')
PREP_HASH = 'ff43de5c3ecd8faa1583faa6f384f298fa397d08ed02d754fd84ec091eca24a2'
OLD_HASH = 'e69bc812c1e2b290dea54c8225a52d12211977b3c7a3805cf4708ca3b5addbbb'
SDK_HASH = '5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872'


def sha(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK)
    try:
        assert stat.S_ISREG(os.fstat(fd).st_mode), str(path)
        with os.fdopen(fd, 'rb', closefd=False) as stream:
            return hashlib.file_digest(stream, 'sha256').hexdigest()
    finally:
        os.close(fd)


def pairs(items):
    data = {}
    for key, value in items:
        assert key not in data, ('duplicate-json-key', key)
        data[key] = value
    return data


def read(path):
    return json.loads(Path(path).read_bytes(), object_pairs_hook=pairs,
                      parse_constant=lambda value: (_ for _ in ()).throw(AssertionError(value)))


def natural(value):
    return type(value) is int and value >= 0


def stats(values):
    values = sorted(values)
    assert values and all(math.isfinite(value) for value in values)
    return dict(Count=len(values), Min=values[0], Median=statistics.median(values),
                P95NearestRank=values[math.ceil(len(values) * .95) - 1], Max=values[-1])


def near(left, right):
    if isinstance(left, dict):
        assert isinstance(right, dict) and left.keys() == right.keys()
        for key in left: near(left[key], right[key])
    elif isinstance(left, list):
        assert isinstance(right, list) and len(left) == len(right)
        for a, b in zip(left, right): near(a, b)
    elif type(left) in (int, float) and type(right) in (int, float):
        assert math.isclose(left, right, rel_tol=1e-12, abs_tol=1e-9), (left, right)
    else:
        assert left == right, (left, right)


prep = read(PREP / 'preparation-receipt.json')
assert sha(PREP / 'preparation-receipt.json') == PREP_HASH
for field, base, count in [('filesSha256', PREP, 69), ('sourceSha256', ROOT, 55),
                          ('app147Sha256', ROOT, 147), ('currentExternalInputsSha256', None, 588)]:
    assert len(prep[field]) == count
    for name, digest in prep[field].items():
        path = Path(name) if base is None else base / name
        assert sha(path) == digest, (field, name)
assert prep['executionReleased'] is False and prep['sourceQualificationReady'] is False
before = read(EXEC / 'inputs-before.json'); after = read(EXEC / 'inputs-after.json')
assert before == after and len(before) == 658
for name, digest in before.items(): assert sha(name) == digest, name
old = read(OLD / 'preparation-receipt.json')
assert sha(OLD / 'preparation-receipt.json') == OLD_HASH and len(old['filesSha256']) == 59
for name, digest in old['filesSha256'].items(): assert sha(OLD / name) == digest
changed = sorted(name for name, digest in old['filesSha256'].items() if prep['filesSha256'][name] != digest)
assert changed == sorted(['DESIGN.md', 'benchmark-plan.json', 'harness/BenchmarkSummary.cs',
    'harness/DRIVER.md', 'harness/ImageInventory.cs', 'harness/Parity.cs', 'harness/Program.cs',
    'harness/commands.json', 'harness/freeze_preparation.py'])

command = read(EXEC / 'command-receipt.json')
release = read(EXEC / 'execution-release.json')
assert command['passed'] and command['preparationSha256'] == release['preparationSha256'] == PREP_HASH
assert len(command['steps']) == 3
process_identities = []
for index, step in enumerate(command['steps'], 1):
    assert step['exitCode'] == 0 and step['ownedDiagnosticGroupAbsent'] is True
    assert step['remainingOwnedDiagnosticGroup'] == [] and step['guardReason'] is None
    assert step['targetSignalsSent'] is False and step['sourceAndAppBuilds'] is False
    stem = {1: '01-restore', 2: '02-build', 3: '03-benchmark'}[index]
    for stream in ['stdout', 'stderr']:
        assert sha(EXEC / (stem + '.' + stream + '.log')) == step[stream + 'Sha256']
    assert not Path('/proc', str(step['pid'])).exists(), ('old-diagnostic-pid-present', step['pid'])
    assert step['command'] == release['commands'][index - 1]
    process_identities.append(dict(pid=step['pid'], startTicks=step['startTicks'],
        recordedGroupAbsent=True, freshPidAbsent=True, wallSeconds=step['wallSeconds']))
assert command['steps'][2]['runtimeNaturalProcessorsExpected'] == 14
build_log = (EXEC / '02-build.stdout.log').read_text()
assert re.search(r'\b0 Warning\(s\)', build_log) and re.search(r'\b0 Error\(s\)', build_log)
# Fresh numeric process-group census. The three recorded group IDs are each child PID.
groups = {step['pid']: [] for step in command['steps']}
for item in Path('/proc').iterdir():
    if not item.name.isdigit(): continue
    try:
        text = (item / 'stat').read_text(); parts = text[text.rfind(')') + 2:].split()
        pgrp = int(parts[2])
        if pgrp in groups: groups[pgrp].append(int(item.name))
    except (FileNotFoundError, PermissionError, ProcessLookupError):
        continue
assert all(not members for members in groups.values()), groups

inventory = read(RESULTS / 'inventory.json')
metadata = read(PREP / 'cohort-metadata/current-cohort-metadata.json')
assert metadata['ownerAssemblyRefRows'] == 65 and metadata['domainServiceAlreadyAnOwnerDirectReference']
assert metadata['currentDistinctMetadataIdentities'] == inventory['ImageCount'] == 66
files = inventory['Files']; assert len(files) == 66
assert [f['FullName'] for f in files] == sorted(f['FullName'] for f in files)
assert len({f['FullName'] for f in files}) == len({f['Path'] for f in files}) == 66
for f, m in zip(files, metadata['entries']):
    assert (f['Ordinal'], f['FullName'], f['Path'], f['Bytes'], f['Sha256']) == (
        m['ordinal'], m['fullName'], m['path'], m['bytes'], m['sha256'])
    assert prep['currentExternalInputsSha256'][f['Path']] == f['Sha256'] == sha(f['Path'])
    assert Path(f['Path']).stat().st_size == f['Bytes'] and 0 < f['Bytes'] <= 16 * 1024 * 1024
    assert f['ExpectedTechnicalIdentity'] == f"{f['FullName']}:{f['ModuleVersionId']}:{f['Sha256']}"
assert inventory['Bytes'] == sum(f['Bytes'] for f in files) == 27655992
charge = sum(f['Bytes'] + 512 for f in files)
assert inventory['ChargedBytes'] == charge == 27689784 and inventory['EntireCohortFitsOriginalBounds']
parity = read(RESULTS / 'parity.json'); result = read(RESULTS / 'result.json')
assert parity == dict(Checks=620, Passed=620, AllPassed=True)
assert result['Completed'] and result['AllPassed'] and result['RawSamples'] == 324
assert result['FeatureAcceptance'] is False and result['ProductSourcesEdited'] is False
facts = read(RESULTS / 'runtime-facts.json')
assert facts['Runtime'] == '10.0.14' and facts['ProcessorCount'] == 14
assert facts['ProcessorCountOverrideAbsent'] and facts['IsServerGC'] and facts['OriginalOwnerServerGc']
assert facts['ForcedGcCalls'] == 0
assert sha(facts['OriginalOwnerRuntimeConfigPath']) == facts['OriginalOwnerRuntimeConfigSha256']
paths = read(RESULTS / 'selected-variant-path-proofs.json')
assert [p['Name'] for p in paths] == ['oracle', 'control', 'candidate']
for p in paths:
    assert p['ScopeTypeProvenByActualObservation'] and p['DomainStreamAndTechnicalSelectedPathProvenByActualHitsAndOccupancy']
    assert p['DomainPathUsesSelectedFreshHelperNotDomainServiceMethod']
    assembly = p['Assembly']; assert sha(assembly['Location']) == assembly['Sha256']
    assert assembly['Configuration'] == 'Debug' and assembly['IsJITOptimizerDisabled']
    assert assembly['Sha256'] == (SDK_HASH if p['Name'] == 'oracle' else facts['Harness']['Sha256'])
agreement = read(RESULTS / 'oracle-control-candidate-agreement.json')
assert len(agreement) == 12
for row in agreement:
    expected_hits = 0 if row['Path'] == 'domain' and row['Repeat'] == 0 else 66
    assert row['CompleteIdentityOutputsAgreeWithIndependentExpectedValues']
    assert row['Images'] == row['MemoCount'] == 66 and row['MemoChargedBytes'] == charge
    assert row['ReportedMemoHits'] == expected_hits and row['FreshResultsWithoutReportedReuse'] == 66 - expected_hits

row_keys = set('Variant Path Phase Round Order Readers IterationsPerReader CompleteObservations ImagesPerObservation StopwatchFrequency ElapsedTicks ProcessCpuTicks ProcessAllocatedBytes GcCollectionDeltas MemoCountBefore MemoCountAfter ChargedBytesBefore ChargedBytesAfter EntireCohortFitsOriginalBounds ReportedMemoHits FreshResultsWithoutReportedReuse NonHitsDistinguishedFromRejections Workers'.split())
worker_keys = set('Worker ElapsedTicks ThreadAllocatedBytes FailureType Observations'.split())
observation_keys = set('Worker Iteration ElapsedTicks ThreadAllocatedBytes ReportedMemoHits FreshResultsWithoutReportedReuse'.split())
orders = [('oracle','control','candidate'),('candidate','control','oracle'),('control','candidate','oracle'),
          ('oracle','candidate','control'),('candidate','oracle','control'),('control','oracle','candidate')]
rows = [json.loads(line, object_pairs_hook=pairs) for line in (RESULTS / 'benchmark-samples.jsonl').read_text().splitlines()]
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
round_comparisons = []
for path in ['domain','technical']:
    for phase in ['memo-cold','warmup','warm']:
        for n in [1,2,4]:
            index = {(r['Variant'],r['Round']):metrics(r) for r in rows if r['Path']==path and r['Phase']==phase and r['Readers']==n}
            for baseline in ['oracle','control']:
                per_round = [dict(round=i, baseline=index[baseline,i], candidate=index['candidate',i],
                    candidatePercentChange={k:100*(index['candidate',i][k]/index[baseline,i][k]-1) for k in index[baseline,i]}) for i in range(6)]
                round_comparisons.append(dict(path=path, phase=phase, readers=n, baseline=baseline,
                    balancedSixRounds=per_round,
                    pairedPercentChange={k:dict(stats(v['candidatePercentChange'][k] for v in per_round),
                        candidateLowerRounds=sum(v['candidatePercentChange'][k]<0 for v in per_round)) for k in index[baseline,0]}))

bindings = {str(p):sha(p) for p in sorted(RESULTS.iterdir()) if p.is_file()}
for p in [PREP/'preparation-receipt.json', PREP/'cohort-count-fix.diff', PREP/'cohort-correction.json',
          PREP/'source-normalization.json', PREP/'cohort-metadata/current-cohort-metadata.json',
          EXEC/'command-receipt.json', EXEC/'execution-release.json', EXEC/'inputs-before.json', EXEC/'inputs-after.json']:
    bindings[str(p)] = sha(p)
audit = dict(auditUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), passed=True,
    diagnosticOnly=True, productionRepairOrHostDeadlineGainClaimed=False,
    evidenceSha256=bindings, auditSourceSha256=sha(__file__),
    frozenInputs=dict(preparationSha256=PREP_HASH, prepared69Current=True, source55Current=True,
        app147Current=True, external588Current=True, all658BeforeEqualsAfterEqualsCurrent=True,
        originalE69ReceiptAnd59FilesPreserved=True, actualChangedPreparedFiles=changed),
    execution=dict(allThreeCommandsExitZero=True, buildWarnings=0, buildErrors=0,
        actualProcessIdentities=process_identities, freshOwnedGroupMembers=groups,
        signalsSent=False, noApplicationBuildHostProfileOrNativeTests=True),
    cohort=dict(images=66, ownerAssemblyReferences=65, domainAlreadyInDirectReferences=True,
        all66ActualRowsEqualMetadataSelectionAndPinnedPhysicalBytes=True,
        bytes=27655992, chargedBytes=charge, originalBoundsFit=True,
        sourceDerivedOwnerDomainDirectReferenceCohortOnly=True,
        completeRuntimeExecutableOwnersDiscoveryUnionClaimed=False),
    qualification=dict(parityChecks=620, parityPassed=620, selectedVariantProofs=3,
        completeIdentityAgreementRows=12, matchedDebugDisableOptimizations=True,
        naturalProcessorCount=14, actualOwnerServerGC=True, forcedGcCalls=0,
        actualCompiledSdkOracleSha256=SDK_HASH, harnessSha256=facts['Harness']['Sha256']),
    rawReconciliation=dict(strictDuplicateKeyRejection=True, strictNestedSchema=True,
        batches=324, observations=observations, imageCalls=observations*66,
        memoHits=hits, freshNonReusedResults=nonhits, all54SummaryGroupsReproduced=True,
        allBatchMemoChargeAndAggregateHitAssertionsPass=True,
        allObservationWorkerIterationAndDurationAllocationFieldsValid=True,
        allSixVariantPermutationsEachPathReaderCount=True, eachVariantTwiceInEachPosition=True),
    recomputedSummary=recomputed, pairedRoundComparisons=round_comparisons,
    interpretation=[
        'Warm concurrent whole-cohort batch wall improves against both oracle and copied control in all six balanced rounds at both 2 and 4 readers for Domain and Technical.',
        'Warm Domain per-observation median and per-batch p95 improve against both baselines in all six 2- and 4-reader rounds. Technical 2-reader p95 is mixed; do not claim a uniform tail gain.',
        'Warm Domain 1-reader batch wall is higher versus oracle in five of six rounds (paired median +4.52%); copied control difference is smaller and mixed. No general single-reader speedup.',
        'Cold results are mixed. Technical cold4 process CPU is higher versus copied control in all six rounds (paired +10.43% to +34.35%); do not suppress cold regressions.',
        'Warm 4-reader Domain allocation reduction is observed in all rounds, while most other allocation differences are small. No mechanism or general memory benefit is established.',
        'Control/oracle output identity agrees exactly. Their warm batch timing medians differ by at most about 5.4%; cold and CPU round dispersion can be larger, so performance equivalence is not asserted.',
        'GC occurs naturally and differs between sequential batches; no forced collection or fresh-process-per-sample isolation. Process CPU and allocation include whole-process activity and release/join bookkeeping.',
        'Six balanced rounds come from one process/session; round pairs are sequential matched observations, not independent randomized repetitions or statistical confidence intervals.',
        'Memo cold means new memo only. Filesystem, JIT and private pools are warm; no cold-start filesystem claim.',
        'Batch wall is throughput/cohort completion, not per-request latency. Observation latency is separately retained; no per-image synchronization is added.',
        'Domain adapter uses selected Fresh helper and scope, not the original DomainService entry method. Discovery/config/authority/lease/network and distributed deadline are outside this benchmark.',
        'RWL candidate changes lock mechanics only in TEMP copied source. No application source, cache observations, limits, fresh file/EOF/PE/MVID reads, production behavior or Story6.5 acceptance is changed.'
    ])
assert not OUT.exists()
OUT.write_text(json.dumps(audit, indent=2, allow_nan=False) + '\n')
print(json.dumps(dict(passed=True, auditPath=str(OUT), auditSha256=sha(OUT), auditSourceSha256=sha(__file__),
    batches=324, observations=observations, imageCalls=observations*66, memoHits=hits, freshNonReusedResults=nonhits), indent=2))
