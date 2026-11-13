from pathlib import Path
import json,hashlib,stat,sys
import psutil
sys.path.insert(0,'/tmp/story65-gateway-controller-qualification-20261107')
from qualify import P,Q,R,M,snapshot
H=lambda f:hashlib.sha256(Path(f).read_bytes()).hexdigest()
assert H(P/'preparation-receipt.json')=='4aab8a4be30c05d67405c39601fbbbbb34b03c98319e8d3b2fd55fbdc88a3925'
for rel,wanted in M['filesSha256'].items():assert H(P/rel)==wanted
before=json.loads((Q/'before.json').read_text());after=snapshot('after');assert after==before
shared=json.loads((P/'shared-input-pins.json').read_text());assert all(H(name)==wanted for name,wanted in shared['inputsSha256'].items())
root=P/'results-qualified-initial';summary=json.loads((root/'controller-summary.json').read_text());assert summary['allPassed']and summary['completedProcesses']==19
controllers=[];cases=[];canaries=[];rows=0
for file in sorted(root.glob('*/*/controller-receipt.json')):
 d=json.loads(file.read_text());assert d['passed']and d['exitCode']==0 and d['allInputsUnchanged']and d['processAndGroupAbsent']and d['directoryReleased']
 assert d['remainingOwnedGroupIdentities']==[]and d['remainingPrivateKnownNames']==[]and d['unexpectedPrivateNameCount']==0
 for name,info in d['artifacts'].items():
  if info['contentsArchived']:assert H(file.parent/name)==info['sha256']
 b=json.loads((file.parent/'business-parity.json').read_text());assert b['Cases']==b['Passed']and b['ErrorType']=='none'
 if d['mode'].startswith('canary-'):canaries.append({'mode':d['mode'],**b})
 else:cases.append({'mode':d['mode'],'group':d['group'],'case':d['case'],**b})
 rows+=d['rows'];controllers.append({'receipt':str(file),'sha256':H(file),'argv':d['command'],'cwd':d['cwd'],'exitCode':0,'runtimeProcessorOverride':d['runtimeProcessorOverride'],'identity':d['identity'],'groupAbsent':True,'privateDirectoryReleased':True,'closed':d.get('closed'),'rows':d['rows'],'stageCounts':d['stageCounts']})
assert len(controllers)==19 and len(canaries)==3 and sum(d['Cases']for d in cases)==60 and sum(d['Passed']for d in cases)==60 and rows==111653
modes={m:{'cases':sum(d['Cases']for d in cases if d['mode']==m),'passed':sum(d['Passed']for d in cases if d['mode']==m),'actualCaptures':sum(d['Captures']for d in cases if d['mode']==m)}for m in ['disabled','active','capped','failing-sink']}
assert all(d['cases']==d['passed']==15 for d in modes.values());assert modes['disabled']['actualCaptures']==0 and all(modes[m]['actualCaptures']==15 for m in ['active','capped','failing-sink'])
facts=json.loads((Q/'runtime-facts.log').read_text());props=json.loads((Q/'compilation-properties.log').read_text())['Properties'];assert props['Optimize']=='false'and props['Configuration']=='Debug'and facts['runtimeVersion']=='10.0.14'and facts['processorCount']==14 and facts['processorOverride']is None
for key in ['oracle','candidate','scratch']:assert facts[key]['configuration']=='Debug'and facts[key]['jitOptimizerDisabled']is True
assert facts['oracle']['debuggingFlags']==facts['candidate']['debuggingFlags']==facts['scratch']['debuggingFlags']and facts['samePublicIdentity']and facts['differentGatewayInstances']
proof=json.loads((P/'source-proof.json').read_text());assert H(P/'QueriesController.private.cs')=='058ba9cfc909cc510f6c382a6b8406a0f04df50b10699db20a0b642cc21eced9'and H(P/'CaptureCounters.cs')=='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'and H(P/'GatewayControllerBootstrap.cs')=='05bbb6de2e160f5ee87546b2980137434e388c74494f4a111c88a59ec6f85a42'
private=Path('/tmp/story65-capture-counter-private-20261107');s=private.lstat();assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700);assert stat.S_ISDIR(s.st_mode)and not any(private.iterdir())
active=[];targets={str(P/'GatewayProof.csproj'),str(P/'bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll'),str(P/'freshcontroller.py')}
for process in psutil.process_iter(['pid','cmdline']):
 try:
  if any(arg in targets for arg in process.info['cmdline']or[]):active.append(process.info['pid'])
 except(psutil.NoSuchProcess,psutil.AccessDenied):pass
assert active==[]
commands=[]
for file in sorted(Q.glob('*.json')):
 try:d=json.loads(file.read_text())
 except:continue
 if 'argv'in d:commands.append({'receipt':str(file),'sha256':H(file),**d})
scratch={str(f.relative_to(P)):H(f)for f in (P/'bin/Debug/net10.0').glob('Hexalith.EventStore.Server.Tests.*')if f.suffix in {'.dll','.pdb','.json'}}
parent=Q/'parent-scratch-audit.json';assert parent.exists()
producer=Path('/tmp/story65-gateway-controller-ops-prep-20261107/producer-attempts/attempt-lphthq92/receipt.json');assert producer.exists()
receipt={'scratchQualificationOnly':True,'featureAcceptance':False,'preparationReceiptSha256':H(P/'preparation-receipt.json'),'passingBehavioralCells':60,'passingFreshCanaries':3,'controlledProcesses':19,'allPassed':True,'strictValidatedRows':rows,'allRowsZeroAllocationAndCountColumns':True,'stage7Overlaps456':True,'residualIsExactRelease':False,'noMvcSerializationSocketOrPairedRootAttribution':True,'modes':modes,'canaries':canaries,'runtimeFacts':facts,'compilationProperties':props,'sameIdentityDistinctGatewayAlcsQualified':True,'runtimeCriticalDependencyReferenceChecksPassed':True,'allNonGatewayDependenciesSharedByLoader':True,'defaultGatewayFallbackForbidden':True,'privateHelperOnlyInCandidateGateway':True,'sourceSha256':after['sourceSha256'],'app147Sha256':after['app147Sha256'],'variantDllPdbSha256':after['variantSha256'],'source52App147AndVariantsBeforeAfterIdentical':True,'shared129InputsUnchanged':True,'preparedFilesSha256':M['filesSha256'],'scratchBinarySha256':scratch,'commands':commands,'controlledProcessReceipts':controllers,'parentIndependentAudit':{'path':str(parent),'sha256':H(parent)},'producerReceipt':{'path':str(producer),'sha256':H(producer)},'producerHistoryOwner':'/tmp/story65-gateway-controller-ops-prep-20261107 (failed pre-emit CS2021 preserved independently)','prepHistoryPreserved':['history/a473b47a/','history/d524a72f/'],'knownWarning':'ASP0019 transparent IDictionary.Add delegation kept to preserve Add semantics','coreSource058Helper112Bootstrap05bbbUnchanged':True,'privateDirectoryPinnedEmpty':True,'ownedScratchProcessesAbsent':True,'applicationBuildsByThisAgent':0,'native40Executed':False,'hostsStarted':0,'pending':['Root final review','Separately authorized actual40 native and consumer audit','Separately authorized managed timing diagnostic and ordinary restoration']}
file=Q/'final-scratch-qualification.json';file.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'receipt':str(file),'sha256':H(file),'cells':60,'canaries':3,'processes':19,'rows':rows,'directoryEmpty':True,'ownedProcesses':active}))
