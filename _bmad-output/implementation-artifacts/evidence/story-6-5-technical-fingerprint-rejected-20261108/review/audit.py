#!/usr/bin/env python3
"""Read-only reconciliation of the completed technical-fingerprint experiment; executes no .NET code."""
import collections, datetime, hashlib, itertools, json, math, pathlib, re, statistics
P=pathlib.Path('/tmp/story65-technical-fingerprint-prep-20261108')
B=pathlib.Path('/tmp/story65-technical-fingerprint-build-di-corrected-20261108')
R=pathlib.Path('/tmp/story65-technical-fingerprint-results-di-corrected-20261108')
O=pathlib.Path('/tmp/story65-technical-fingerprint-audit-20261108')
W=pathlib.Path('/home/administrator/projects/hexalith/projects')
PREP='d1f1239af7879f498dc8765325ed1ffda10114286f68845340dc0e8a5976a8ad'
RELEASE='5221005e0d7be369d199fc383d7ca03dc18eb9643b1ffc3e6f030f07e9b4c178'
def require(ok, check):
    if not ok: raise AssertionError(check)
def sha(p):
    with pathlib.Path(p).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def pairs(xs):
    d={}
    for k,v in xs:
        require(k not in d,'duplicate-json-key');d[k]=v
    return d
def load(p):return json.loads(pathlib.Path(p).read_text(),object_pairs_hook=pairs)
def natural(v):return type(v) is int and v>=0
def numeric(v):return type(v) in (int,float) and math.isfinite(v) and v>=0
def close(a,b):return a is None and b is None or type(a) in (int,float) and type(b) in (int,float) and math.isclose(a,b,rel_tol=1e-11,abs_tol=1e-7)
def median(xs):return statistics.median(xs)
def p95(xs):return sorted(xs)[math.ceil(len(xs)*.95)-1]
require(sha(P/'preparation-receipt.json')==PREP,'prep-sha')
require(sha(B/'runtime-release.json')==RELEASE,'runtime-release-sha')
prep=load(P/'preparation-receipt.json');release=load(B/'runtime-release.json');runtime=load(B/'runtime-receipt.json');comp=load(B/'compile-release.json');built=load(B/'receipt.json')
require(release['preparationReceiptSha256']==PREP and release['executionReleased'] is True and release['productionEdits'] is False,'release-binding')
require(comp['preparationReceiptSha256']==PREP and built['releaseSha256']==sha(B/'compile-release.json') and built['passed'] is True,'compile-binding')
union={};map_counts={}
def bind(mapping,base=None):
    for n,h in mapping.items():
        p=pathlib.Path(n) if base is None else base/n
        require(p.is_absolute() and not p.is_symlink() and p.is_file(),'regular-absolute-input')
        if base is not None:require(p.resolve().is_relative_to(base.resolve()),'contained-prepared-input')
        k=str(p);require(k not in union or union[k]==h,'same-path-same-pin');union[k]=h
for key in ['filesSha256','inputsSha256','compiledInputsSha256','coordinatorInputsSha256']:
    map_counts[key]=len(release[key]);bind(release[key],P if key=='filesSha256' else None)
require(map_counts=={'filesSha256':59,'inputsSha256':211,'compiledInputsSha256':282,'coordinatorInputsSha256':4},'exact-runtime-map-counts')
require(len(union)==556,'runtime-distinct-union')
runtime_union_count=len(union)
bind(comp['pinnedInputsSha256'])
helpers={
 '/tmp/story65-gateway-current-cpu-prep-20261108/watcher/run_one.py':'b0d1edc30318a87914a2055d38706f04ef56d146182b0fd4b69bba1d1875947c',
 '/tmp/story65-gateway-current-cpu-prep-20261108/watcher/common.py':'29c8809f64bbf1b682e570942155891d910447e8ef3ecfe66e34d115d8f448f6'}
bind(helpers)
for p,h in union.items():require(sha(p)==h,'current-input-sha:'+p)
require(prep['filesSha256']==release['filesSha256'] and prep['inputsSha256']==release['inputsSha256'],'prep-release-map-equality')
require(len(prep['source65Sha256'])==65,'source65-count')
for p,h in prep['source65Sha256'].items():require(sha(W/p)==h and release['inputsSha256'][str(W/p)]==h,'source65-current')
for c in built['commands']+[runtime]:
    require(c['childSpawned'] and c['ownedChildExited'] and c['exitCode']==0 and c['guardReason'] is None and c['supervisorErrorType'] is None,'owned-command-pass')
    require(c['onlyDirectlyOwnedDiagnosticChildSignaled'] is False and c['pidfdPinsExactOwnedChild'] and c['unrecognizedStdoutStderrRetained'] is False,'owned-command-no-signals-or-raw-output')
require(runtime['manifestSha256']==RELEASE and runtime['postInputsMatch'] is True and runtime['wallSeconds']<180 and runtime['peakObservedRssBytes']<2*1024**3,'runtime-bounds-and-posthash')
identities=[{'pid':x['childPid'],'startTicks':str(x['childStartTicks']),'group':x['childGroup']}for x in built['commands']+[runtime]]
groups={x['group']for x in identities};pids={x['pid']for x in identities};survivors=[];unreadable=[]
for pd in pathlib.Path('/proc').iterdir():
    if not pd.name.isdecimal():continue
    try:
        t=(pd/'stat').read_text();f=t[t.rfind(')')+2:].split();pid=int(pd.name);group=int(f[2]);session=int(f[3])
        if pid in pids or group in groups or session in groups:survivors.append({'pid':pid,'state':f[0],'group':group,'session':session,'startTicks':f[19]})
    except FileNotFoundError:pass
    except PermissionError:unreadable.append(int(pd.name))
require(not survivors and not unreadable,'fresh-pid-group-session-absence-including-zombies')
closed=load(R/'closed.json');parity=load(R/'parity.json');corpus=load(R/'corpus-counts.json');facts=load(R/'runtime-facts.json');summary=load(R/'summary.json')
require(closed=={'Passed':True,'Batches':54,'Observations':2079,'DescriptorSchemaAndKeyValuesPersisted':False,'ProductionApproved':False,'HostDeadlineBenefitClaimed':False},'closed-exact')
require(parity=={'Checks':386,'Passed':386,'AllPassed':True},'parity386-diagnostic-only')
require(corpus['MarkedTechnicalEndpoints']==corpus['EligibleTechnicalEndpoints']==103 and corpus['RefusedTechnicalEndpoints']==0 and corpus['ConstrainedEligibleEndpoints']==3,'complete-corpus')
require(corpus['AssemblyCounts']=={'Hexalith.EventStore.Gateway':{'Marked':35,'Eligible':35,'Refused':0,'ConstrainedEligible':1},'Hexalith.EventStore.Admin.Server':{'Marked':68,'Eligible':68,'Refused':0,'ConstrainedEligible':2}},'per-assembly-corpus')
require(not corpus['OwnerQueryMarked'] and not corpus['OwnerQueryEligible'] and not corpus['RefusalLabels'] and corpus['ServiceFilterTypesResolved']==3,'unmarked-owner-query-not-promoted')
require(facts['Runtime']=='10.0.14' and facts['ProcessorCount']==14 and facts['IsServerGC'] and facts['RuntimeCpuOverrideAbsent'] and facts['Frequency']==10**9 and facts['ForcedGcCalls']==0,'runtime-debug-natural-facts')
assembly_names={'Harness':'Hexalith.EventStore.Server.Tests','ActualSdk':'Hexalith.EventStore.ServiceDefaults','ActualContracts':'Hexalith.EventStore.Contracts','ActualGateway':'Hexalith.EventStore.Gateway','ActualAdminServer':'Hexalith.EventStore.Admin.Server'}
for key,name in assembly_names.items():
    obj=facts[key];path=P/'bin/Debug/net10.0'/(name+'.dll')
    require(obj['FullName'].split(',')[0]==name and obj['Sha256']==sha(path) and 'DisableOptimizations' in obj['Debuggable'],'actual-typed-assembly-facts')
require(facts['ActualSdk']['Sha256']==release['sdkSha256'] and facts['ActualContracts']['Sha256']==release['contractsSha256'],'current-sdk-contracts-selection')
raw=[json.loads(line,object_pairs_hook=pairs)for line in (R/'raw-batches.jsonl').read_text().splitlines()]
orders=[['actual','control','candidate'],['control','candidate','actual'],['candidate','actual','control']]
expected=[(v,s,n,i+1)for s in ['memo-cold','warm']for n in [1,2,4]for i,order in enumerate(orders)for v in order]
require([(r['Variant'],r['State'],r['Readers'],r['Round'])for r in raw]==expected,'exact54-balanced-matrix-order')
batch_keys={'Variant','State','Readers','Round','Observations','WallTicks','CpuMilliseconds','AllocatedBytes','Gen0','Gen1','Gen2','OwnerInitializeTicks','OwnerInitializeAllocatedBytes','OwnerDisposeTicks','OwnerDisposeAllocatedBytes','CountersBefore','CountersAfter','Samples'}
counter_keys={'Entries','ChargedBytes','Hits','Stores','ConstraintFallbacks','UnavailableScopeFallbacks','UnsupportedKeyFallbacks'}
sample_keys={'Reader','Ordinal','ElapsedTicks','EndpointCount','ImageHits'}
by=collections.defaultdict(list);observations=0;image_hits=collections.Counter();counter_totals=collections.Counter();cost_anomalies=[]
for r in raw:
    require(set(r)==batch_keys,'closed-batch-schema')
    for k in batch_keys-{'Variant','State','Samples','CountersBefore','CountersAfter','AllocatedBytes'}:require(numeric(r[k]) and (k=='CpuMilliseconds' or natural(r[k])),'batch-numeric-nonnegative')
    require(type(r['AllocatedBytes']) is int,'allocation-integer')
    if r['AllocatedBytes']<0:cost_anomalies.append({'variant':r['Variant'],'state':r['State'],'readers':r['Readers'],'round':r['Round'],'field':'AllocatedBytes','value':r['AllocatedBytes']})
    n=r['Readers'];iterations=32 if r['State']=='warm' else 1;count=n*iterations
    require(r['Observations']==count==len(r['Samples']),'nested-observation-count')
    require([(s['Reader'],s['Ordinal'])for s in r['Samples']]==list(itertools.product(range(n),range(1,iterations+1))),'reader-ordinal-complete')
    for s in r['Samples']:
        require(set(s)==sample_keys and all(natural(v)for v in s.values()) and s['EndpointCount']==103 and s['ElapsedTicks']<=r['WallTicks'],'closed-complete-observation-schema-wallbound')
        image_hits[s['ImageHits']]+=1
    a,b=r['CountersBefore'],r['CountersAfter']
    require(set(a)==set(b)==counter_keys and all(natural(v)for v in a.values()) and all(natural(v)for v in b.values()),'counter-schema')
    require(all(b[k]>=a[k]for k in counter_keys),'monotonic-counters')
    require(b['Entries']<=256 and b['ChargedBytes']<=2097152,'shared-owner-bounds')
    if r['Variant']!='candidate':require(not any(a.values()) and not any(b.values()),'actual-control-no-fingerprint-memo')
    else:
        require(b['Entries']==b['Stores']==145 and b['ChargedBytes']==689608 and b['UnavailableScopeFallbacks']==b['UnsupportedKeyFallbacks']==0,'candidate-unique-entry-charge-and-eligibility')
        if r['State']=='warm':
            require(a=={'Entries':145,'ChargedBytes':689608,'Hits':58,'Stores':145,'ConstraintFallbacks':3,'UnavailableScopeFallbacks':0,'UnsupportedKeyFallbacks':0},'one-warmup-outside-clock')
            require(b['Hits']-a['Hits']==203*count and b['ConstraintFallbacks']-a['ConstraintFallbacks']==3*count,'all-warm-eligible-hits-and-fresh-constraints')
        else:
            require(not any(a.values()) and b['ConstraintFallbacks']==3*count and 58<=b['Hits']<=203*count-145,'cold-shared-owner-misses')
        for k in counter_keys:counter_totals[k]+=b[k]-a[k]
    by[(r['Variant'],r['State'],n)].append(r);observations+=count
require(observations==2079 and len(by)==18 and len(summary)==18,'raw-total')
require(cost_anomalies==[{'variant':'candidate','state':'warm','readers':1,'round':2,'field':'AllocatedBytes','value':-186168048}],'exact-observed-allocation-anomaly')
recomputed={}
for key,rs in by.items():
    obs=[s['ElapsedTicks']/1e6 for r in rs for s in r['Samples']]
    recomputed[key]={
      'Batches':len(rs),'Observations':sum(r['Observations']for r in rs),
      'MedianAmortizedWallMilliseconds':median(r['WallTicks']/1e6/r['Observations']for r in rs),
      'ObservationMedianMilliseconds':median(obs),'ObservationP95Milliseconds':p95(obs),
      'MedianAllocatedBytesPerObservation':median(r['AllocatedBytes']/r['Observations']for r in rs),
      'MedianCpuMillisecondsPerObservation':median(r['CpuMilliseconds']/r['Observations']for r in rs)if all(r['CpuMilliseconds']>=30 for r in rs)else None,
      'CpuBatchesBelow30Milliseconds':sum(r['CpuMilliseconds']<30 for r in rs),
      'MedianOwnerInitializeMilliseconds':median(r['OwnerInitializeTicks']/1e6 for r in rs),
      'MedianOwnerDisposeMilliseconds':median(r['OwnerDisposeTicks']/1e6 for r in rs)}
seen=set()
for s in summary:
    key=(s['Key']['Variant'],s['Key']['State'],s['Key']['Readers']);require(key not in seen,'unique-summary-group');seen.add(key)
    require(set(s)=={'Key',*recomputed[key]},'closed-summary-schema')
    require(all(close(s[k],v)for k,v in recomputed[key].items()),'summary-independently-recomputed')
comparison=[]
metrics=['MedianAmortizedWallMilliseconds','ObservationMedianMilliseconds','ObservationP95Milliseconds','MedianAllocatedBytesPerObservation','MedianCpuMillisecondsPerObservation']
for state in ['memo-cold','warm']:
 for n in [1,2,4]:
    c=recomputed[('candidate',state,n)];row={'state':state,'readers':n,'candidatePerObservation':{k:c[k]for k in metrics},'ratios':{},'roundRatios':{}}
    for other in ['actual','control']:
        o=recomputed[(other,state,n)]
        row['ratios'][other]={k:c[k]/o[k]if c[k]is not None and o[k]is not None else None for k in metrics}
        cr=by[('candidate',state,n)];orr=by[(other,state,n)]
        row['roundRatios'][other]=[{'round':a['Round'],'wall':a['WallTicks']/b['WallTicks'],'allocation':a['AllocatedBytes']/b['AllocatedBytes'] if a['AllocatedBytes']>=0 and b['AllocatedBytes']>=0 else None,'cpu':a['CpuMilliseconds']/b['CpuMilliseconds']}for a,b in zip(cr,orr)]
    row['allocationValidity']={'candidate':sum(r['AllocatedBytes']>=0 for r in by[('candidate',state,n)]),'actual':sum(r['AllocatedBytes']>=0 for r in by[('actual',state,n)]),'control':sum(r['AllocatedBytes']>=0 for r in by[('control',state,n)])}
    row['validPairedAllocationRatioBounds']={v:{'count':len(a:=[r['allocation'] for r in row['roundRatios'][v] if r['allocation'] is not None]),'minimum':min(a),'maximum':max(a)}for v in ['actual','control']}
    row['allocationSummaryContainsInvalidInput']=state=='warm' and n==1
    row['controlOverActual']={k:recomputed[('control',state,n)][k]/recomputed[('actual',state,n)][k]if recomputed[('control',state,n)][k]is not None and recomputed[('actual',state,n)][k]is not None else None for k in metrics}
    row['batchCpuBelow30']={v:recomputed[(v,state,n)]['CpuBatchesBelow30Milliseconds']for v in ['actual','control','candidate']}
    row['gcCollectionTotals']={v:{k:sum(r[k]for r in by[(v,state,n)])for k in ['Gen0','Gen1','Gen2']}for v in ['actual','control','candidate']}
    comparison.append(row)
warm=[x for x in comparison if x['state']=='warm']
warm_wins={v:sum(r['wall']<1 for x in warm for r in x['roundRatios'][v])for v in ['actual','control']}
report={
 'auditCompleted':True,'reconciliationPassed':True,'allRawCostMeasurementsValid':False,'costAnomalies':cost_anomalies,'costValidity':{'validBatchAllocationCells':53,'totalBatchAllocationCells':54,'allOtherBatchNumericCostsNonnegative':True,'affectedPublishedSummaryMetric':'candidate/warm/1/MedianAllocatedBytesPerObservation and ratios using it; published numerical recomputation passes, allocation validity fails','warmAllocationRatiosUsingOnlyValidPairedRoundsAcrossAllReaders':{v:{'count':len(a:=[r['allocation']for x in warm for r in x['roundRatios'][v]if r['allocation']is not None]),'minimum':min(a),'maximum':max(a)}for v in ['actual','control']}},'auditKind':'read-only independent completed-experiment reconciliation','observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'decision':'Reject production: inconsistent whole-pass wall changes and higher valid two-/four-reader warm allocation.',
 'bindings':{'sourcePreparationSha256':PREP,'runtimeReleaseSha256':RELEASE,'runtimeReceiptSha256':sha(B/'runtime-receipt.json'),'compileReceiptSha256':sha(B/'receipt.json'),'auditScriptSha256':sha(__file__),'resultFilesSha256':{p.name:sha(p)for p in sorted(R.iterdir())if p.is_file()},'diConstructorOnlyCorrectionSha256':sha(P/'di-constructor-fix.diff')},
 'currentInputs':{'runtimeMapCounts':map_counts,'runtimeDistinctPaths':runtime_union_count,'compilePinnedPaths':len(comp['pinnedInputsSha256']),'auditedDistinctPathsIncludingCompileOnlyWrapperAndQualifiedSupervisorHelpers':len(union),'source65Current':True,'allCurrentHashesMatch':True,'sdkSha256':release['sdkSha256'],'contractsSha256':release['contractsSha256']},
 'closure':{'commandsExitZero':3,'runtimeWallSeconds':runtime['wallSeconds'],'runtimePeakObservedRssBytes':runtime['peakObservedRssBytes'],'bounds':{'wallSeconds':180,'cpuSoftSeconds':90,'cpuHardSeconds':91,'rssBytes':2147483648},'guardReason':None,'signalsSent':False,'identities':identities,'freshPidGroupAndSessionScanIncludesZombies':True,'matchingLiveOrZombieProcesses':0,'unreadableProcStat':0},
 'coverage':{'diagnosticParityChecks':386,'nativeTestsExecutedByThisExperiment':0,'markedAndEligibleTechnicalEndpoints':103,'gateway':35,'admin':68,'constrainedFallbackEndpoints':3,'unmarkedOwnerQueryExcluded':True,'actualServiceFilterTypesResolved':3,'batches':54,'wholeObservationPasses':2079,'perPassDescribeCalls':103,'describeCallsInMeasuredObservations':2079*103,'summaryGroups':18,'states':['memo-cold','warm'],'readers':[1,2,4],'balancedCyclicRounds':3,'warmIterationsPerReader':32,'observationsPerRound':{'memo-cold':7,'warm':224},'rawSchemaAndNestedCountsExact':True,'allPublishedSummaryValuesIndependentlyRecomputedIncludingInvalidAllocationInput':True,'imageHitsPerObservationFrequency':dict(image_hits)},
 'memo':{'sharedEntryLimit':256,'sharedChargedByteLimit':2097152,'entryChargeIncludesDigestAllowance':512,'actualCandidateEntriesPerOwner':145,'actualCandidateChargedBytesPerOwner':689608,'chargedBytesAreNotMeasuredHeapBytes':True,'warmupOutsideClockPassesPerWarmOwner':1,'eligibleMemoLookupsPerFullPass':203,'warmMeasuredHitDeltaPerPass':203,'constraintFallbackDeltaPerPass':3,'unavailableScopeFallbackDelta':0,'unsupportedKeyFallbackDelta':0,'candidateMeasuredCounterDeltas':dict(counter_totals),'coldConcurrentDuplicateMissesPermitted':True,'scopeAndCompleteIncludedInEveryObservationBySource':True},
 'comparison':comparison,'warmPairedRoundWallWinsOutOf9':warm_wins,
 'ownerLifecycle':{'initializeAndDisposeOutsideObservationAndBatchWall':True,'reportedSeparatelyByPublishedSummary':True,'noOwnerLifecycleSavingClaim':True,'allOwnerAllocationFieldsNonnegative':True,'ownerAllocationBytesByVariant':{v:{k:{'minimum':min(r[k]for r in raw if r['Variant']==v),'maximum':max(r[k]for r in raw if r['Variant']==v)}for k in ['OwnerInitializeAllocatedBytes','OwnerDisposeAllocatedBytes']}for v in ['actual','control','candidate']}},
 'limits':[
  'One candidate warm single-reader round reports a negative allocated-byte delta (-186168048); its cause is not established and that allocation sample is unusable. The published medians were recomputed exactly but retain this invalid input; valid paired rounds are reported separately, not substituted into published summaries.',
  'One bounded scratch process with three balanced cyclic rounds, Debug disable-optimization, .NET 10.0.14, natural 14 processors and Server GC; no independent repeated process series or significance claim.',
  'Whole fresh marked technical MVC observation plus Complete over this installed 103-endpoint Gateway/Admin corpus; no full receiver, authority operation, unmarked owner query, network or two-second host-deadline claim.',
  'No explicit Start/Run/network call by reviewed source construction; lifecycle/network inactivity was not independently instrumented.',
  'Memo-cold means fresh owner on a previously exercised process/JIT/runtime; warm includes one full unmeasured owner-warming pass. Shared-owner concurrent cold duplicate computations are allowed.',
  'Candidate adds key construction and a fingerprint scope; actual/control retain the existing image memo. All source image/PE/MVID/schema/policy observations and Complete remain in each measured pass.',
  'Allocation uses process-wide precise total allocated bytes across the batch and includes task/setup effects; GC totals and process CPU are batch costs, not isolated method or reader costs. CPU summary is null whenever any group batch is below 30 ms.',
  'Owner initialization and disposal are outside the measured observation/batch wall and reported separately; no disposal-in-clock or whole lifecycle benefit claim.',
  'The 256-entry/2 MiB charged policy includes UTF16 key bytes plus 512 fixed allowance per entry; this is not a CLR heap bound measurement. Successful pure digest entries can remain after later Complete failure; every later pass still performs Complete.',
  '386 checks are diagnostic parity coverage, not added native qualification. Original compile-closure/import and DI setup failures remain preserved separately; only this final DI-corrected attempt passed.',
  'Current hash equality is scoped to the explicitly bound source/runtime/compiled input maps, not an assertion of all historical assembly generations or host acceptance.'
 ]}
(O/'independent-benchmark-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'auditCompleted':True,'allRawCostMeasurementsValid':False,'report':str(O/'independent-benchmark-audit.json'),'sha256':sha(O/'independent-benchmark-audit.json'),'auditedDistinctInputs':len(union),'warmCandidateActualWall':[x['ratios']['actual']['MedianAmortizedWallMilliseconds']for x in warm],'warmCandidateActualAllocation':[x['ratios']['actual']['MedianAllocatedBytesPerObservation']for x in warm],'warmRoundWallWins':warm_wins}))
