from pathlib import Path
import collections,hashlib,json,os,stat
P=Path('/tmp/story65-invocation-transport-prep-20261107');Q=Path('/tmp/story65-invocation-transport-qualification-20261107');ROOT=Path('/home/administrator/projects/hexalith/projects');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
meta=json.loads((P/'preparation-receipt.json').read_text());assert sha(P/'preparation-receipt.json')=='8d967b31560ce95a630751a2d2b37bc6a9070296d19306ad11785c4b01ca0d36'
summary=json.loads((P/'results-initial/controller-summary.json').read_text());assert summary['allPassed'] and summary['completedProcesses']==summary['plannedProcesses']==16
rows=0;stages=collections.Counter();modes=collections.Counter();casecount=entrycount=canaries=excludedcases=excludedentries=0;receipts={};businesses={}
zero={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
for item in summary['receipts']:
 p=Path(item['path']);r=json.loads(p.read_text());assert r['passed']and r['processAndGroupAbsent']and r['directoryReleased']and r['allInputsUnchanged'];assert r['inputsBeforeSha256']==r['inputsAfterSha256'];assert r['remainingOwnedGroupIdentities']==[] and r['remainingPrivateKnownNames']==[] and r['unexpectedPrivateNameCount']==0
 assert r['exitCode']==0 and not r.get('scratchRecoveryTerminateAttempted',False);modes[r['mode']]+=1;receipts[str(p)]=sha(p)
 bpath=p.parent/'business-parity.json';b=json.loads(bpath.read_text());assert b['Cases']==b['Passed'] and b['ErrorType']=='none';businesses[str(bpath)]=sha(bpath)
 if r['mode'].startswith('canary-'):canaries+=1
 elif r['mode']=='excluded':excludedcases+=b['Cases'];excludedentries+=b['InvocationComparisons'];assert r['rows']is None and b['PrivateAdmittedCount']is None and not b['HelperStateInspected']
 else:casecount+=b['Cases'];entrycount+=b['InvocationComparisons']
 if r['rows']is not None:rows+=r['rows']
 for k,v in (r['stageCounts']or{}).items():stages[k]+=v
 f=p.parent/'gateway.jsonl'
 if f.exists():
  actual=0
  with f.open()as stream:
   for line in stream:
    d=json.loads(line);assert d['stage']in{'Whole','ComponentFile','ComponentPolicy'}and d['role']=='gateway'and all(type(d[k])is int and d[k]==0 for k in zero);actual+=1
  assert actual==r['rows']
assert (casecount,entrycount,canaries,excludedcases,excludedentries)==(80,84,3,20,21)
for rel,h in meta['filesSha256'].items():assert sha(P/rel)==h
for field in ['sourceSha256','app147Sha256','currentExternalInputsSha256']:
 for n,h in meta[field].items():p=Path(n);p=p if p.is_absolute()else ROOT/p;assert sha(p)==h
binaries=json.loads((Q/'facts-attempt-1/scratch-binaries-before.json').read_text());assert all(sha(Path(n))==h for n,h in binaries.items())
before=json.loads((Q/'build-attempt-1/inputs-before.json').read_text());assert all(sha(Path(n))==h for n,h in before.items())
fd=os.open('/tmp/story65-capture-counter-private-20261107',os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_NONBLOCK)
try:s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and not os.listdir(fd)
finally:os.close(fd)
out={'ownerAuditOnly':True,'allPassed':True,'passingBehavioralCells':casecount,'actualSendAsyncEntriesPerVariantAcrossFourModes':entrycount,'passingFreshCanaries':canaries,'eligibleControlledProcesses':15,'genuineExcludedProcesses':1,'controlledProcesses':16,'excludedBehavioralCases':excludedcases,'excludedActualSendAsyncEntriesPerVariant':excludedentries,'strictValidatedRows':rows,'strictStageCounts':dict(stages),'modeProcesses':dict(modes),'allCountAndAllocationColumnsZero':True,'allExactOwnedPidsAndGroupsAbsent':True,'fixedPinnedDirectoryEmpty':True,'all61Prep55Source147App408ExternalAndScratchBinariesUnchanged':True,'sourceSha256':meta['sourceSha256'],'controllerReceiptsSha256':receipts,'businessReceiptsSha256':businesses,'nativeTestsExecuted':0,'applicationBuilds':0,'hostsStarted':0,'featureAcceptance':False,'parentIndependentAuditPending':True}
(Q/'owner-scratch-audit.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({k:out[k]for k in ['allPassed','passingBehavioralCells','actualSendAsyncEntriesPerVariantAcrossFourModes','passingFreshCanaries','controlledProcesses','excludedBehavioralCases','strictValidatedRows','strictStageCounts','allExactOwnedPidsAndGroupsAbsent','fixedPinnedDirectoryEmpty']}))
