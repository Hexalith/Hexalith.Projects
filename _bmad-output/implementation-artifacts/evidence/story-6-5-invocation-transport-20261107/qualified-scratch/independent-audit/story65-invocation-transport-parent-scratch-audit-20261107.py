from pathlib import Path
import collections,hashlib,json,stat,os
B=Path('/tmp/story65-invocation-transport-prep-20261107');Q=Path('/tmp/story65-invocation-transport-qualification-20261107');R=Path('/home/administrator/projects/hexalith/projects')
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
prep=json.loads((B/'preparation-receipt.json').read_text());assert sha(B/'preparation-receipt.json')=='8d967b31560ce95a630751a2d2b37bc6a9070296d19306ad11785c4b01ca0d36'
summary=json.loads((B/'results-initial/controller-summary.json').read_text());assert summary['allPassed'] and summary['completedProcesses']==summary['plannedProcesses']==16
zero={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'};fields=zero|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'};stages={'Whole','ComponentFile','ComponentPolicy'}
def pairs(xs):
 d={}
 for k,v in xs:assert k not in d;d[k]=v
 return d
inputs={};processes=[];rows=0;stagecounts=collections.Counter();behavior=canaries=excluded=0;entrycount=0;modecounts=collections.Counter();mapcounts=[]
for item in summary['receipts']:
 p=Path(item['path']);r=json.loads(p.read_text());assert r['passed'] and r['processAndGroupAbsent'] and r['directoryReleased'] and r['exitCode']==0 and r['inputsBeforeSha256']==r['inputsAfterSha256'];mapcounts.append(len(r['inputsBeforeSha256']));mode=r['mode'];modecounts[mode]+=1
 for n,h in r['inputsBeforeSha256'].items():assert n not in inputs or inputs[n]==h;inputs[n]=h
 processes.append(r['identity']);biz=json.loads((p.parent/'business-parity.json').read_text());assert biz['ErrorType']=='none' and biz['Cases']==biz['Passed']
 if mode.startswith('canary-'):
  canaries+=1;assert biz['CallerCanaryUnchanged'] and biz['CallerActivityUnchanged'] and biz['CallerFlowUnchanged'];assert biz['DrainCanaryObserved']==(mode=='canary-positive-control')
 elif mode=='excluded':
  excluded+=biz['Passed'];assert biz['Passed']==20 and biz['InvocationComparisons']==21 and biz['PrivateAdmittedCount']is None and not biz['HelperStateInspected'];assert biz['LivedTicks']>=12*biz['Frequency']
 else:behavior+=biz['Passed'];entrycount+=biz['InvocationComparisons']
 raw=p.parent/'gateway.jsonl'
 if not raw.exists():continue
 data=raw.read_bytes();c=json.loads((p.parent/'gateway.closed.json').read_text());assert c['countersFinal']==1 and c['admitted']==c['disposedOrUnwound'];assert all(c[k]==0 for k in ['activeAtDrainEnd','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease']);assert c['writtenBytes']==len(data)
 seen=set();bycap=collections.defaultdict(set);count=0
 for line in data.splitlines():
  d=json.loads(line,object_pairs_hook=pairs);assert set(d)==fields and d['role']=='gateway' and d['stage']in stages;assert all(type(d[k])is int for k in fields-{'role','stage'}) and all(d[k]==0 for k in zero);assert d['runOrdinal']==c['runOrdinal'] and d['frequency']==c['frequency'] and d['captureOrdinal']>0 and d['elapsedTicks']>=0
  pair=(d['captureOrdinal'],d['stage']);assert pair not in seen;seen.add(pair);bycap[d['captureOrdinal']].add(d['stage']);count+=1;stagecounts[d['stage']]+=1
 assert count==c['writtenRows'];rows+=count
 if mode=='capped':assert c['capRowsDropped']>0 and c['ioFailures']==0
 elif mode=='failing-sink':assert c['ioFailures']>0 and count==0
 else:assert all(c[k]==0 for k in ['capRowsDropped','ioFailures','queuedRowsDropped','closedRowsDropped'])
 if mode=='active':
  for e in biz['StageExpectations']:
   for i,mask in enumerate(e['ExpectedStages']):assert bycap[e['FirstOrdinal']+i]==set(mask)
 if mode in ['canary-current','canary-suppressed']:assert bycap[biz['SecondBusinessCaptureOrdinal']]==stages
assert (behavior,canaries,excluded,entrycount,rows)==(80,3,20,84,83662)
assert dict(stagecounts)=={'Whole':83635,'ComponentFile':13,'ComponentPolicy':14}
for n,h in inputs.items():assert Path(n).is_file() and not Path(n).is_symlink() and sha(n)==h
livegroups=set()
for p in Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:
  s=(p/'stat').read_text().rsplit(')',1)[1].split()
  if s[0]!='Z':livegroups.add(int(s[2]))
 except (FileNotFoundError,PermissionError,ProcessLookupError):pass
for p in processes:
 assert p['processGroup']not in livegroups
 try:s=Path('/proc',str(p['pid']),'stat').read_text().rsplit(')',1)[1].split();assert s[0]=='Z' or s[19]!=p['startTicks']
 except FileNotFoundError:pass
private=Path('/tmp/story65-capture-counter-private-20261107');s=private.lstat();assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and (s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700) and not list(private.iterdir())
proof={'parentIndependentAuditOnly':True,'featureAcceptance':False,'allPassed':True,'passingBehavioralCells':behavior,'actualSendEntriesPerVariantAcrossFourModes':entrycount,'passingFreshCanaries':canaries,'excludedBehavioralCases':excluded,'excludedActualSendEntriesPerVariant':21,'controlledProcesses':len(processes),'allActualPidsAndGroupsIndependentlyAbsent':True,'strictValidatedRows':rows,'strictStageCounts':dict(stagecounts),'allCountAndAllocationColumnsZero':True,'perProcessInputMapCounts':sorted(set(mapcounts)),'distinctCurrentInputPathsRehashed':len(inputs),'allInputsStillMatch':True,'fixedPinnedDirectoryEmpty':True,'activeExactPerEntryStageMasksPass':True,'modeProcessCounts':dict(modecounts),'preparationReceiptSha256':sha(B/'preparation-receipt.json')}
(Q/'parent-scratch-audit.json').write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps(proof))
