from pathlib import Path
import json,collections,statistics,hashlib
BASE=Path('/tmp/story65-parsed-template-cpu-owner-profile-20261107/profile');OUT=Path('/tmp/story65-parsed-template-cpu-evidence-20261107');r=json.loads((BASE/'cpu-summary.json').read_text());groups=r['stackGroups']
def ranked(counter,denominator):return [{'frame':key,'samples':value,'fractionOfClassSamples':value/denominator if denominator else 0}for key,value in counter.most_common()]
analysis={}
for category in ['Managed','External','Error','Unknown']:
 current=[g for g in groups if g['SampleType']==category];count=sum(g['Count']for g in current);leaf=collections.Counter();first=collections.Counter();inclusive=collections.Counter()
 for g in current:
  frames=g['Frames'];leaf[frames[0]if frames else'[missing]']+=g['Count'];first[next((frame for frame in frames if frame.startswith('Hexalith.')),'[no retained Hexalith frame]')]+=g['Count']
  for frame in set(frame for frame in frames if frame.startswith('Hexalith.')):inclusive[frame]+=g['Count']
 analysis[category]={'classSamples':count,'leafPartition':ranked(leaf,count),'firstHexalithPartition':ranked(first,count),'inclusiveHexalithFrames':ranked(inclusive,count),'inclusiveIsOverlappingNotAdditive':True}
wait_tokens=['System.Threading.LowLevelLifoSemaphore.Wait','System.Threading.WaitHandle.Wait','System.Threading.Monitor.Wait','System.Threading.ManualResetEventSlim.Wait','System.Threading.Thread.Sleep']
external=collections.Counter();external_total=r['sampleTypeCounts'].get('External',0)
for g in groups:
 if g['SampleType']!='External':continue
 frames=g['Frames']
 if any(any(token in frame for token in wait_tokens)for frame in frames):category='recognized-managed-wait-api-visible'
 elif any('System.Security.Cryptography' in frame or'Interop+Crypto' in frame for frame in frames):category='crypto-api-visible-native-work-or-transition'
 elif any('System.IO.'in frame or'Interop+Sys.Read'in frame or'FileStream'in frame for frame in frames):category='file-or-os-read-api-visible-native-work-or-wait'
 elif any('System.Net.'in frame for frame in frames):category='network-api-visible-native-work-or-wait'
 else:category='other-external-unclassified'
 external[category]+=g['Count']
def union(intervals):
 intervals=sorted((a,b)for a,b in intervals if b>=a)
 if not intervals:return 0
 total=0;start,end=intervals[0]
 for a,b in intervals[1:]:
  if a<=end:end=max(end,b)
  else:total+=end-start;start,end=a,b
 return total+end-start
susp={}
for reason in sorted({x['reason']for x in r['suspendRestartPairs']}):
 rows=[x for x in r['suspendRestartPairs']if x['reason']==reason];durations=[x['durationMilliseconds']for x in rows];susp[reason]={'pairedCount':len(rows),'sumWallMilliseconds':sum(durations),'unionWallMilliseconds':union((x['startMilliseconds'],x['endMilliseconds'])for x in rows),'medianWallMilliseconds':statistics.median(durations),'maximumWallMilliseconds':max(durations)}
cycles=r['gcCyclePairs'];cycle_durations=[x['wallDurationMilliseconds']for x in cycles]
result={'diagnosticOnly':True,'featureAcceptance':False,'noExactCpuTimeClaim':True,'sourceSummarySha256':hashlib.sha256((BASE/'cpu-summary.json').read_bytes()).hexdigest(),'totalSamples':r['samples'],'sampleTypeCounts':r['sampleTypeCounts'],'managedSampleFraction':r['managedSampleFraction'],'externalSampleFraction':r['externalSampleFraction'],'classAggregation':analysis,'externalHeuristicPartitions':[{'category':key,'samples':value,'fractionOfExternalSamples':value/external_total}for key,value in external.most_common()],'externalClassificationIsNotBlockedTimeOrNativeCpuAccounting':True,'suspendRestartByReason':susp,'gcStartStopCollectionCycles':{'pairedCount':len(cycles),'sumWallMilliseconds':sum(cycle_durations),'unionWallMilliseconds':union((x['startMilliseconds'],x['endMilliseconds'])for x in cycles),'medianWallMilliseconds':statistics.median(cycle_durations),'maximumWallMilliseconds':max(cycle_durations),'collectionTypeNotRetained':'Cycles can include background/concurrent collections; these are not stop-the-world pause or CPU durations.'},'limits':['Managed means cooperative GC mode at sampling; runtime source explicitly treats that as an approximation to managed execution.','External means preemptive mode, and can include waiting, native computation and transitions. No external fraction is called blocked fraction or exact native CPU.','Only runtime-known managed threads with nonempty walked stacks contribute; native-only threads need not be represented.','Sampling suspends managed execution itself, skips sampling during other suspensions and has nominal1ms sleep plus work; sample counts are not exact CPU milliseconds.','Managed Thread.PollGC leaf samples can reflect safepoint/sampling bias. Current counts are retained in the leaf partition; leaf and first-Hexalith/inclusive views remain separate.','SuspendOther includes sampler-induced suspensions and is distinct from GC-related reasons.','Suspend start overwrites and unpaired restart counts are retained in the source summary. Paired intervals are observed wall intervals, not complete precise GC pause accounting.','GCStart→GCStop collection-cycle wall intervals may include background/concurrent collection; they are separate from SuspendEE→RestartEE suspension wall intervals.','Inclusive frame sample totals overlap and must not be summed as a disjoint CPU partition.','Frames marked unresolved remain fixed labels; no source/module paths, IPs, object addresses or payload are emitted.']}
(OUT/'cpu-interpretation.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'samples':r['samples'],'externalPartitions':result['externalHeuristicPartitions'],'suspensions':susp,'gcCycles':result['gcStartStopCollectionCycles']},indent=2))
