"""Read-only reconciliation of closed scratch benchmark, without target or source mutations."""
from pathlib import Path
import collections,hashlib,json,math,statistics,datetime
BASE=Path('/tmp/story65-canonical-hash-memo-results-20261108');PREP=Path('/tmp/story65-canonical-hash-memo-prep-20261108');BUILD=Path('/tmp/story65-canonical-hash-memo-build-20261108');OUT=Path(__file__).parent
bindings={}
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def pairs(items):
 d={}
 for k,v in items:assert k not in d;d[k]=v
 return d
def read(p):
 bindings[str(p)]=sha(p);return json.loads(Path(p).read_bytes(),object_pairs_hook=pairs)
def near(a,b):return math.isclose(a,b,rel_tol=1e-11,abs_tol=1e-8)
closed=read(BASE/'closed.json');parity=read(BASE/'parity.json');facts=read(BASE/'runtime-facts.json');datasets=read(BASE/'dataset-facts.json');summary=read(BASE/'summary.json');decision=read(BASE/'root-implementation-decision.json');manifest=read(PREP/'preparation-receipt.json');run=read(BUILD/'runtime-release/run-receipt.json');release=read(BUILD/'runtime-release/root-release.json');compiled=read(BUILD/'alias-correction/receipt.json');initial=read(BUILD/'receipt.json')
assert bindings[str(BASE/'root-implementation-decision.json')]=='85bc53f58505a1e35e92f6d27f7a33533be362002e85885ab3e4101e950b6f42'and bindings[str(PREP/'preparation-receipt.json')]=='3bdb543d2131c0803544104eee76ab1ff76f36f93b3953387e1f2551f42fc6dc'
assert closed=={'Passed':True,'Batches':216,'Observations':16380,'ProductionApproval':False,'HostDeadlineClaim':False}and parity=={'Checks':178,'Passed':178,'AllPassed':True,'NativeCases':0,'ProductionApproval':False}
assert facts['Runtime']=='10.0.14'and facts['ProcessorCount']==14 and facts['IsServerGC']and facts['Frequency']==1000000000 and not facts['ForcedGcCalls']and not facts['NewRuntimeEnvironmentValues']
for k in ['Harness','ActualContracts','ActualSdk']:assert 'DisableOptimizations'in facts[k]['Debuggable']
assert facts['ActualContracts']['Sha256']=='067c49c6711c24cc14267e708cb968ad113da07ab5633419f3115a47362308c5'and facts['ActualSdk']['Sha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
assert run['exitCode']==0 and run['ownedChildExited']and run['pidfdPinsExactOwnedChild']and run['guardReason']is None and not run['onlyDirectlyOwnedDiagnosticChildSignaled']and run['childPid']==302755 and run['childGroup']==302754
assert release['allInputHashesMatch']and release['sourceInputs']==977 and release['compiledArtifacts']==63 and release['manifestSha256']==bindings[str(PREP/'preparation-receipt.json')]
assert compiled['exitCode']==0 and compiled['all930InputsAnd45PreparedUnchanged']
for p,h in compiled['compiledArtifactsSha256'].items():assert sha(p)==h
for rel,h in manifest['filesSha256'].items():assert sha(PREP/rel)==h
input_deltas=[{'path':p,'atRunExpectedSha256':h,'currentSha256':sha(p)}for p,h in manifest['inputsSha256'].items()if sha(p)!=h]
# Production source is now being changed under a separate Root authorization; historical source equality is not demanded.
assert decision['rootAll977And63InputsPostrunObservation'].startswith('All matched in Root immediately-after-run')
rows=[]
with (BASE/'raw-batches.jsonl').open()as f:
 for line in f:rows.append(json.loads(line,object_pairs_hook=pairs))
bindings[str(BASE/'raw-batches.jsonl')]=sha(BASE/'raw-batches.jsonl');assert len(rows)==216 and sum(r['Observations']for r in rows)==16380
fields={'Dataset','Workload','State','Variant','Readers','Round','Observations','WallTicks','CpuMilliseconds','AllocatedBytes','Gen0','Gen1','Gen2','Captures','OwnerDisposeTicks','Counters'}
expected=[];orders=[['production','control','candidate'],['control','candidate','production'],['candidate','production','control']]
for ds in ['public-catalog-fixture','compiled-sample-owner']:
 for work in ['inventory','identity']:
  for state in ['memo-cold','warm']:
   for readers in [1,2,4]:
    for round,order in enumerate(orders,1):
     for variant in order:expected.append((ds,work,state,variant,readers,round))
assert[(r['Dataset'],r['Workload'],r['State'],r['Variant'],r['Readers'],r['Round'])for r in rows]==expected
keys=['Dataset','Workload','State','Variant','Readers'];grouped=collections.defaultdict(list)
for r in rows:
 assert set(r)==fields and all(type(r[k])is int and r[k]>=0 for k in ['Observations','WallTicks','AllocatedBytes','Gen0','Gen1','Gen2','OwnerDisposeTicks'])and type(r['CpuMilliseconds'])in[int,float]and math.isfinite(r['CpuMilliseconds'])and r['CpuMilliseconds']>=0
 n=64 if r['State']=='warm'else 1;assert r['Observations']==r['Readers']*n and len(r['Captures'])==r['Observations']
 assert {(x['Worker'],x['Ordinal'])for x in r['Captures']}=={(worker,ordinal)for worker in range(r['Readers'])for ordinal in range(1,n+1)}
 assert all(set(x)=={'Worker','Ordinal','ElapsedTicks'}and type(x['ElapsedTicks'])is int and 0<=x['ElapsedTicks']<=r['WallTicks']for x in r['Captures'])
 c=r['Counters'];assert c['ByteMemoEntries']==c['ByteMemoRetained']==0
 if r['Variant']=='candidate':
  unique,charge=(64,97868)if r['Dataset']=='public-catalog-fixture'else(14,26168)
  assert c['HashEntries']==unique and c['RetainedCharge']==charge and c['Hits']+c['Misses']==unique*(r['Observations']+(1 if r['State']=='warm'else 0))
  if r['State']=='warm':assert c['Misses']==unique and c['Hits']==unique*r['Observations']
 grouped[tuple(r[k]for k in keys)].append(r)
assert len(grouped)==72 and len(summary)==72
ms=lambda ticks:1000*ticks/facts['Frequency']
for s in summary:
 key=tuple(s['Key'][k]for k in keys);rs=grouped[key];caps=[ms(x['ElapsedTicks'])for r in rs for x in r['Captures']];cpu_ok=all(r['CpuMilliseconds']>=30 for r in rs)
 expect={'Batches':len(rs),'Observations':sum(r['Observations']for r in rs),'MedianAmortizedWallMilliseconds':statistics.median(ms(r['WallTicks'])/r['Observations']for r in rs),'MedianCpuMillisecondsPerObservation':statistics.median(r['CpuMilliseconds']/r['Observations']for r in rs)if cpu_ok else None,'CpuBatchesBelow30Milliseconds':sum(r['CpuMilliseconds']<30 for r in rs),'MedianOwnerDisposeMilliseconds':statistics.median(ms(r['OwnerDisposeTicks'])for r in rs),'MedianAllocatedBytesPerObservation':statistics.median(r['AllocatedBytes']/r['Observations']for r in rs),'ObservationMedianMilliseconds':statistics.median(caps),'ObservationP95Milliseconds':sorted(caps)[math.ceil(.95*len(caps))-1]}
 assert all(s[k]is None if v is None else near(s[k],v)for k,v in expect.items())
summaries={tuple(s['Key'][k]for k in keys):s for s in summary};comparisons=[]
for ds in ['public-catalog-fixture','compiled-sample-owner']:
 for work in ['inventory','identity']:
  for state in ['memo-cold','warm']:
   for readers in [1,2,4]:
    cand=summaries[(ds,work,state,'candidate',readers)];actual=summaries[(ds,work,state,'production',readers)];control=summaries[(ds,work,state,'control',readers)]
    ratios=[]
    for round in [1,2,3]:
     c=next(r for r in grouped[(ds,work,state,'candidate',readers)]if r['Round']==round);a=next(r for r in grouped[(ds,work,state,'production',readers)]if r['Round']==round);o=next(r for r in grouped[(ds,work,state,'control',readers)]if r['Round']==round)
     ratios.append({'round':round,'vsActualBatchWallRatio':c['WallTicks']/a['WallTicks'],'vsControlBatchWallRatio':c['WallTicks']/o['WallTicks']})
    comparisons.append({'dataset':ds,'workload':work,'state':state,'readers':readers,'candidateAmortizedWallMs':cand['MedianAmortizedWallMilliseconds'],'actualAmortizedWallMs':actual['MedianAmortizedWallMilliseconds'],'controlAmortizedWallMs':control['MedianAmortizedWallMilliseconds'],'candidateVsActualWallRatio':cand['MedianAmortizedWallMilliseconds']/actual['MedianAmortizedWallMilliseconds'],'candidateVsControlWallRatio':cand['MedianAmortizedWallMilliseconds']/control['MedianAmortizedWallMilliseconds'],'candidateVsActualAllocationRatio':cand['MedianAllocatedBytesPerObservation']/actual['MedianAllocatedBytesPerObservation'],'candidateVsControlAllocationRatio':cand['MedianAllocatedBytesPerObservation']/control['MedianAllocatedBytesPerObservation'],'candidateVsActualP95Ratio':cand['ObservationP95Milliseconds']/actual['ObservationP95Milliseconds'],'candidateVsControlP95Ratio':cand['ObservationP95Milliseconds']/control['ObservationP95Milliseconds'],'perRoundWallRatios':ratios,'candidateOwnerDisposeMedianMs':cand['MedianOwnerDisposeMilliseconds'],'candidateCpuPerObservationMs':cand['MedianCpuMillisecondsPerObservation'],'actualCpuPerObservationMs':actual['MedianCpuMillisecondsPerObservation'],'controlCpuPerObservationMs':control['MedianCpuMillisecondsPerObservation']})
for x in decision['warmComparisons']:
 c=next(v for v in comparisons if v['state']=='warm'and(v['dataset'],v['workload'],v['readers'])==(x['dataset'],x['workload'],x['readers']))
 for a,b in [('candidateVsActualWallRatio','candidateVsActualWallRatio'),('candidateVsCopiedControlWallRatio','candidateVsControlWallRatio'),('candidateVsActualAllocatedRatio','candidateVsActualAllocationRatio'),('candidateVsActualP95Ratio','candidateVsActualP95Ratio')]:assert near(x[a],c[b])
remaining=[];permissions=0
for p in Path('/proc').iterdir():
 if p.name.isdigit():
  try:
   f=(p/'stat').read_text().rsplit(')',1)[1].split()
   if f[0]!='Z'and(int(p.name)==run['childPid']and f[19]==str(run['childStartTicks'])or int(f[2])==run['childGroup']):remaining.append(int(p.name))
  except(FileNotFoundError,ProcessLookupError):pass
  except PermissionError:permissions+=1
assert not remaining and not permissions
warm=[c for c in comparisons if c['state']=='warm'];cold=[c for c in comparisons if c['state']=='memo-cold'];rounds=[r for c in warm for r in c['perRoundWallRatios']]
report={'passed':True,'diagnosticOnly':True,'featureAcceptance':False,'productionBenefitNotYetQualified':True,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'auditSourceSha256':sha(__file__),'bindingsSha256':bindings,'parityChecks':178,'nativeTestCasesAdded':0,'batches':216,'observations':16380,'summaryGroupsRecomputed':72,'fullRawSchemaCardinalityOrderAndCountersReconciled':True,'threeCyclicOrdersEachPositionOnceNotSixPermutations':True,'warmComparisons':warm,'coldComparisons':cold,'warmPerRoundWallWinsVsActual':sum(r['vsActualBatchWallRatio']<1 for r in rounds),'warmPerRoundWallWinsVsControl':sum(r['vsControlBatchWallRatio']<1 for r in rounds),'warmRoundComparisons':len(rounds),'candidateWarmWallRatioVsActualRange':[min(c['candidateVsActualWallRatio']for c in warm),max(c['candidateVsActualWallRatio']for c in warm)],'candidateWarmWallRatioVsControlRange':[min(c['candidateVsControlWallRatio']for c in warm),max(c['candidateVsControlWallRatio']for c in warm)],'candidateWarmAllocationVsActualRange':[min(c['candidateVsActualAllocationRatio']for c in warm),max(c['candidateVsActualAllocationRatio']for c in warm)],'compiled63CurrentAllMatch':True,'preparedSourcesCurrentAllMatch':True,'rootRecorded977Plus63PostrunUnchangedBeforeProductionDelegation':True,'laterCurrentOriginalInputDeltas':input_deltas,'laterApprovedProductionEditsNotTreatedAsBenchmarkCorruption':True,'runtime10_0_14Natural14ServerGcDebugNoOptimization':True,'freshOwnedChildAndGroupAbsent':True,'freshProcPermissionErrors':permissions,'limits':['Two public/compiled datasets:30 declarations+17policies and7declarations+7operations; neither is a complete installed live host inventory.','64warm observations per worker with one untimed warmup, fresh scope and fresh outer input/Inventory snapshots every timed observation. Actual authority file/config/network observations were not benchmarked.','Single process,3cyclic orders each position once; only3batches per cell. Round ranges retained and no confidence-interval claim.','Memo-cold means one newly shared owner; concurrent cold readers can reuse a prior publication. Runtime/JIT/filesystem cold not claimed. Owner construction is outside measurement; fresh scope disposal is timed, owner disposal measured separately.','Global allocation delta includes task/record scaffolding and natural GC. CPU summary exists only if ALL3cell batches reached30ms; omitted cells retain raw CPU only.','Namespace-only copied control differs measurably from actual compiled production in some cells; candidate comparisons are shown against both.','Parity178 is scratch correctness evidence, not native qualification or final compiled transfer. Root implementation decision is subject to native tests and a final compiled benchmark. No host/deadline benefit or earlier-profile/request attribution is inferred.']}
(OUT/'independent-benchmark-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'path':str(OUT/'independent-benchmark-audit.json'),'sha256':sha(OUT/'independent-benchmark-audit.json'),'warmActualRange':report['candidateWarmWallRatioVsActualRange'],'warmControlRange':report['candidateWarmWallRatioVsControlRange'],'roundWinsActual':report['warmPerRoundWallWinsVsActual'],'roundWinsControl':report['warmPerRoundWallWinsVsControl'],'rounds':len(rounds),'laterInputDeltas':len(input_deltas)},indent=2))
