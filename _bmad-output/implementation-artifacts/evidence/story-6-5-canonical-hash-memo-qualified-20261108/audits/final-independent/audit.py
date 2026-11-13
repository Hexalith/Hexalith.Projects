"""Read-only audit of closed final compiled transfer, its frozen inputs and current native handoff."""
from pathlib import Path
import collections,datetime,hashlib,json,math,statistics
BASE=Path('/tmp/story65-canonical-hash-memo-final-compiled-results-20261108');PREP=Path('/tmp/story65-canonical-hash-memo-final-compiled-prep-20261108');BUILD=Path('/tmp/story65-canonical-hash-memo-final-compiled-build-20261108');ROOT=Path('/home/administrator/projects/hexalith/projects');OUT=Path(__file__).parent
bound={}
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def pairs(xs):
 d={}
 for k,v in xs:assert k not in d;d[k]=v
 return d
def read(p):bound[str(p)]=sha(p);return json.loads(Path(p).read_bytes(),object_pairs_hook=pairs)
def near(a,b):return math.isclose(a,b,rel_tol=1e-11,abs_tol=1e-8)
m=read(PREP/'preparation-receipt.json');closed=read(BASE/'closed.json');parity=read(BASE/'parity.json');facts=read(BASE/'runtime-facts.json');datasets=read(BASE/'dataset-facts.json');summary=read(BASE/'summary.json');run=read(BUILD/'runtime-release/run-receipt.json');release=read(BUILD/'runtime-release/root-release.json');handoff=read(BUILD/'native-handoff.json')
assert bound[str(PREP/'preparation-receipt.json')]=='2c9eb795e0a7fca470673a3d5f8955abf5519e49c98c7b190ada6f7348d629f9'and bound[str(BUILD/'native-handoff.json')]=='0b56e41f35db16a65b34e386eaca08a1904c5d7d1e9abc0333601bfeb3bb2be2'
assert m['nativeHandoffReady']and m['executionReleased']and m['nativeHandoffSha256']==facts['NativeHandoffSha256']==bound[str(BUILD/'native-handoff.json')]
assert len(m['inputsSha256'])==1055 and len(m['compiledInputsSha256'])==63 and len(m['filesSha256'])==44
for group in ['inputsSha256','compiledInputsSha256']:
 for p,h in m[group].items():assert sha(p)==h
for p,h in m['filesSha256'].items():assert sha(PREP/p)==h
native=read(Path(handoff['independentAuditPath']));completed=read(Path(handoff['nativeCompletedReceiptPath']));source=read(Path(handoff['source65Path']))['sourceFilesSha256']
assert handoff['nativePasses']==native['nativeExecutions']==completed['nativeTotal']==1076 and not handoff['featureAcceptance']
assert bound[handoff['independentAuditPath']]==handoff['independentAuditSha256']=='857afbd14563603cf2b1f24f9785a5decf6687b0055b1c25034184eb499001c8'and bound[handoff['nativeCompletedReceiptPath']]==handoff['nativeCompletedReceiptSha256']and bound[handoff['source65Path']]==handoff['source65Sha256']
assert len(source)==65 and all(sha(ROOT/p)==h for p,h in source.items())
assert handoff['producerFilesSha256']==completed['producerFilesSha256']==native['finalProducer8DllPdbRefRefintSha256']and all(sha(p)==h for p,h in handoff['producerFilesSha256'].items())
assert all(sha(p)==h for p,h in native['native20TestOutputsUnchangedSha256'].items())and all(sha(p)==h for p,h in native['app147MarkerFreeCurrentSha256'].items())
for p,h in native['bindingsSha256'].items():assert sha(p)==h
assert closed=={'Passed':True,'Batches':216,'Observations':16380,'ProductionApproval':False,'HostDeadlineClaim':False}and parity=={'Checks':178,'Passed':178,'AllPassed':True,'NativeCases':0,'ProductionApproval':False}
assert facts['Runtime']=='10.0.14'and facts['ProcessorCount']==14 and facts['IsServerGC']and facts['Frequency']==1000000000 and facts['ForcedGcCalls']==facts['NewRuntimeEnvironmentValues']==0
assert facts['ActualAuthorityOwnerSelectionProved']and facts['OwnerInitializationAndDisposalOutsideCapture']and(facts['ProductionOriginalMemos'],facts['ProductionNewHashMemos'],facts['ControlByteMemos'],facts['CopiedPrototypeMemos'])==(3,1,1,2)
for k in ['Harness','ActualContracts','ActualSdk']:assert'DisableOptimizations'in facts[k]['Debuggable']
assert facts['ActualSdk']['Sha256']==m['sdkSha256']=='873dbb74ee259d91c99ea2804e5801134c993abb4ef7806e79f242e5d23faf96'and facts['ActualContracts']['Sha256']==m['contractsSha256']=='f1cf82fe57d5ae0037772d3ccea77766a141cd49ec66c2079c3c8b046c8089df'
assert run['exitCode']==0 and run['ownedChildExited']and run['pidfdPinsExactOwnedChild']and run['guardReason']is None and not run['onlyDirectlyOwnedDiagnosticChildSignaled']and run['childPid']==575330 and run['childGroup']==575329
assert release['manifestSha256']==bound[str(PREP/'preparation-receipt.json')]and release['sourceInputs']==1055 and release['compiledArtifacts']==63 and release['nativeHandoffSha256']==bound[str(BUILD/'native-handoff.json')]
rows=[json.loads(line,object_pairs_hook=pairs)for line in(BASE/'raw-batches.jsonl').read_text().splitlines()];bound[str(BASE/'raw-batches.jsonl')]=sha(BASE/'raw-batches.jsonl');assert len(rows)==216 and sum(r['Observations']for r in rows)==16380
keys=['Dataset','Workload','State','Variant','Readers'];fields=set(keys)|{'Round','Observations','WallTicks','CpuMilliseconds','AllocatedBytes','Gen0','Gen1','Gen2','Captures','OwnerInitializeTicks','OwnerDisposeTicks','Counters'}
orders=[['production','control','candidate'],['control','candidate','production'],['candidate','production','control']];expected=[(ds,work,state,variant,readers,round)for ds in ['public-catalog-fixture','compiled-sample-owner']for work in ['inventory','identity']for state in ['memo-cold','warm']for readers in [1,2,4]for round,order in enumerate(orders,1)for variant in order]
assert[(r['Dataset'],r['Workload'],r['State'],r['Variant'],r['Readers'],r['Round'])for r in rows]==expected
groups=collections.defaultdict(list)
for r in rows:
 assert set(r)==fields and all(type(r[k])is int and r[k]>=0 for k in ['Observations','WallTicks','AllocatedBytes','Gen0','Gen1','Gen2','OwnerInitializeTicks','OwnerDisposeTicks'])and type(r['CpuMilliseconds'])in[int,float]and math.isfinite(r['CpuMilliseconds'])and r['CpuMilliseconds']>=0
 n=64 if r['State']=='warm'else 1;assert r['Observations']==r['Readers']*n and len(r['Captures'])==r['Observations']and {(x['Worker'],x['Ordinal'])for x in r['Captures']}=={(worker,ordinal)for worker in range(r['Readers'])for ordinal in range(1,n+1)}
 assert all(set(x)=={'Worker','Ordinal','ElapsedTicks'}and type(x['ElapsedTicks'])is int and 0<=x['ElapsedTicks']<=r['WallTicks']for x in r['Captures'])
 c=r['Counters'];assert c['ByteMemoEntries']==c['ByteMemoRetained']==0
 if r['Variant']in ['production','candidate']:
  unique,charge=(64,97868)if r['Dataset']=='public-catalog-fixture'else(14,26168)
  assert c['HashEntries']==unique and c['RetainedCharge']==charge and c['Hits']+c['Misses']==unique*(r['Observations']+(1 if r['State']=='warm'else 0))
  if r['State']=='warm':assert c['Misses']==unique and c['Hits']==unique*r['Observations']
 if r['Variant']=='production':assert c['ActualAuthorityOwner']and c['ActualSelectorOptionObservationsPerEntry']==3 and c['OriginalOwnerMemos']==3 and c['AdditionalCanonicalHashMemo']==1 and c['ScopeDelegateBoundOutsideCapture']and c['NoPerCaptureReflection']
 groups[tuple(r[k]for k in keys)].append(r)
assert len(groups)==len(summary)==72;ms=lambda t:1000*t/facts['Frequency'];indexed={}
for s in summary:
 key=tuple(s['Key'][k]for k in keys);rs=groups[key];caps=[ms(x['ElapsedTicks'])for r in rs for x in r['Captures']]
 e={'Batches':len(rs),'Observations':sum(r['Observations']for r in rs),'MedianAmortizedWallMilliseconds':statistics.median(ms(r['WallTicks'])/r['Observations']for r in rs),'MedianCpuMillisecondsPerObservation':statistics.median(r['CpuMilliseconds']/r['Observations']for r in rs)if all(r['CpuMilliseconds']>=30 for r in rs)else None,'CpuBatchesBelow30Milliseconds':sum(r['CpuMilliseconds']<30 for r in rs),'MedianOwnerInitializeMilliseconds':statistics.median(ms(r['OwnerInitializeTicks'])for r in rs),'MedianOwnerDisposeMilliseconds':statistics.median(ms(r['OwnerDisposeTicks'])for r in rs),'MedianAllocatedBytesPerObservation':statistics.median(r['AllocatedBytes']/r['Observations']for r in rs),'ObservationMedianMilliseconds':statistics.median(caps),'ObservationP95Milliseconds':sorted(caps)[math.ceil(.95*len(caps))-1]}
 assert all(s[k]is None if v is None else near(s[k],v)for k,v in e.items());indexed[key]=s
comparisons=[]
for ds in ['public-catalog-fixture','compiled-sample-owner']:
 for work in ['inventory','identity']:
  for state in ['memo-cold','warm']:
   for readers in [1,2,4]:
    prod=indexed[(ds,work,state,'production',readers)];control=indexed[(ds,work,state,'control',readers)];prototype=indexed[(ds,work,state,'candidate',readers)];rounds=[]
    for round in [1,2,3]:
     p=next(r for r in groups[(ds,work,state,'production',readers)]if r['Round']==round);c=next(r for r in groups[(ds,work,state,'control',readers)]if r['Round']==round);q=next(r for r in groups[(ds,work,state,'candidate',readers)]if r['Round']==round)
     rounds.append({'round':round,'productionVsControlWall':p['WallTicks']/c['WallTicks'],'productionVsPrototypeWall':p['WallTicks']/q['WallTicks']})
    comparisons.append({'dataset':ds,'workload':work,'state':state,'readers':readers,'productionAmortizedWallMs':prod['MedianAmortizedWallMilliseconds'],'controlAmortizedWallMs':control['MedianAmortizedWallMilliseconds'],'prototypeAmortizedWallMs':prototype['MedianAmortizedWallMilliseconds'],'productionVsControlWallRatio':prod['MedianAmortizedWallMilliseconds']/control['MedianAmortizedWallMilliseconds'],'productionVsPrototypeWallRatio':prod['MedianAmortizedWallMilliseconds']/prototype['MedianAmortizedWallMilliseconds'],'productionVsControlAllocationRatio':prod['MedianAllocatedBytesPerObservation']/control['MedianAllocatedBytesPerObservation'],'productionVsPrototypeAllocationRatio':prod['MedianAllocatedBytesPerObservation']/prototype['MedianAllocatedBytesPerObservation'],'productionVsControlP95Ratio':prod['ObservationP95Milliseconds']/control['ObservationP95Milliseconds'],'productionVsPrototypeP95Ratio':prod['ObservationP95Milliseconds']/prototype['ObservationP95Milliseconds'],'productionOwnerInitializeMs':prod['MedianOwnerInitializeMilliseconds'],'productionOwnerDisposeMs':prod['MedianOwnerDisposeMilliseconds'],'controlOwnerInitializeMs':control['MedianOwnerInitializeMilliseconds'],'prototypeOwnerInitializeMs':prototype['MedianOwnerInitializeMilliseconds'],'usableCpuPerObservationMs':{v:s['MedianCpuMillisecondsPerObservation']for v,s in [('production',prod),('control',control),('prototype',prototype)]},'perRound':rounds})
remaining=[];perms=0
for p in Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:
  f=(p/'stat').read_text().rsplit(')',1)[1].split()
  if int(p.name)==run['childPid']and f[19]==str(run['childStartTicks'])or int(f[2])==run['childGroup']or int(f[3])==run['childGroup']:remaining.append(int(p.name))
 except(FileNotFoundError,ProcessLookupError):pass
 except PermissionError:perms+=1
assert not remaining and not perms
warm=[c for c in comparisons if c['state']=='warm'];cold=[c for c in comparisons if c['state']=='memo-cold'];rounds=[r for c in warm for r in c['perRound']]
report={'passed':True,'diagnosticOnly':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'auditSourceSha256':sha(__file__),'bindingsSha256':bound,'frozen1055InputsAnd63CompiledAnd44PreparedCurrentAllMatch':True,'nativeHandoff1076Current':True,'source65CurrentAllMatch':True,'native20TestOutputsAndApp147StillUnchanged':True,'parityChecks':178,'nativeCasesAddedByBenchmark':0,'batches':216,'observations':16380,'summaryGroupsRecomputed':72,'allRawSchemaOrderCardinalityNestedCountersAndSummaryAggregatesReconciled':True,'runtime10_0_14Natural14ServerGcDebugNoOptimization':True,'actualTypedAuthorityOwnerScopeAndHashParticipationProved':True,'selectorReadCount3IsSourceAndNativeTestFactNotDynamicBenchmarkCounter':True,'allProductionBatchesHashOwner64Or14EntriesWithExactWarmHits':True,'ownerSetupReflectionAndDelegateBindingOutsideObservationClock':True,'perObservationScopeFactoryOptionReadsAndScopeDisposalInsideClock':True,'actualOwner3PriorPlus1NewMemoVersusControl1AndPrototype2':True,'warmComparisons':warm,'coldComparisons':cold,'warmMedianWallWinsVsControl':sum(c['productionVsControlWallRatio']<1 for c in warm),'warmPerRoundWallWinsVsControl':sum(r['productionVsControlWall']<1 for r in rounds),'warmRoundComparisons':len(rounds),'warmVsControlWallRatioRange':[min(c['productionVsControlWallRatio']for c in warm),max(c['productionVsControlWallRatio']for c in warm)],'warmVsControlAllocationRatioRange':[min(c['productionVsControlAllocationRatio']for c in warm),max(c['productionVsControlAllocationRatio']for c in warm)],'warmVsControlP95RatioRange':[min(c['productionVsControlP95Ratio']for c in warm),max(c['productionVsControlP95Ratio']for c in warm)],'warmVsPrototypeWallRatioRange':[min(c['productionVsPrototypeWallRatio']for c in warm),max(c['productionVsPrototypeWallRatio']for c in warm)],'coldVsControlWallRatioRange':[min(c['productionVsControlWallRatio']for c in cold),max(c['productionVsControlWallRatio']for c in cold)],'freshOwnedPidStartPgidSidAbsentIncludingZombieCheck':True,'freshProcPermissionErrors':perms,'limits':['Two datasets remain public fixture30declarations+17policies andcompiledSample7declarations+7operations, not complete installed-host inventories. No authority IO/network/fresh config-file work is benchmarked.','64warm operations per worker, one untimed warmup; cold is newly shared memo owner, not runtime/JIT/filesystem cold. Concurrent cold readers can reuse another publication.','Single process,3cyclic rounds each position once, not6permutations. Only3batches per cell; all round ratios retained. No confidence-interval or host-deadline inference.','Batch wall/observations is amortized throughput; nested median/p95 is observation latency. Process CPU normalized only when all3batches reach30ms. Allocations include task/record scaffolding and natural GC.','Real production owner construction/reflection/delegate binding and disposal are measured separately and excluded from observation; initialization owns3prior memos+1new versus copiedcontrol1/prototype2. Per-operation delegate/real options selector and fresh scope disposal remain in-clock.','Typed scope and memo proof plus actualnewContracts/SDK hashes prevent old-SDK fallback, but this scratch result is not ordinary MULTI/browser acceptance. Native1076 remains separate from178diagnostic checks.','Native source compiler and generation10/17 limits in handoff/audit are preserved; no full historical binary/source coherence claim is added.']}
p=OUT/'independent-final-compiled-audit.json';p.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'path':str(p),'sha256':sha(p),'warmWallRatioRange':report['warmVsControlWallRatioRange'],'warmAllocRatioRange':report['warmVsControlAllocationRatioRange'],'warmP95RatioRange':report['warmVsControlP95RatioRange'],'warmPrototypeRatioRange':report['warmVsPrototypeWallRatioRange'],'warmRoundWins':report['warmPerRoundWallWinsVsControl'],'warmRoundComparisons':len(rounds),'coldWallRatioRange':report['coldVsControlWallRatioRange']},indent=2))
