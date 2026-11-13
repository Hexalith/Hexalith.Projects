from pathlib import Path
import json,hashlib,stat,sys
import psutil
sys.path.insert(0,'/tmp/story65-outbound-assertion-qualification-20261107')
from qualify import P,Q,R,M,snapshot
H=lambda f:hashlib.sha256(Path(f).read_bytes()).hexdigest()
assert H(P/'preparation-receipt.json')=='6814b1a4a1e0658c809d9d8108ef6ff48292783f2605ac2b5dc84381f21c1f30'
for rel,wanted in M['filesSha256'].items():assert H(P/rel)==wanted
before=json.loads((Q/'before.json').read_text());after=snapshot('after');assert after==before
shared=json.loads((P/'shared-input-pins.json').read_text());assert len(shared['inputsSha256'])==152 and all(H(name)==wanted for name,wanted in shared['inputsSha256'].items())
root=P/'results-qualified-initial';summary=json.loads((root/'controller-summary.json').read_text());assert summary['allPassed'] and summary['completedProcesses']==15
controllers=[];cases=[];canaries=[];rows=0
allowed=['Whole','ComponentFile','ComponentPolicy']
for file in sorted(root.glob('*/*/controller-receipt.json')):
 d=json.loads(file.read_text());assert d['passed'] and d['exitCode']==0 and d['allInputsUnchanged'] and d['processAndGroupAbsent'] and d['directoryReleased']
 assert d['remainingOwnedGroupIdentities']==[] and d['remainingPrivateKnownNames']==[] and d['unexpectedPrivateNameCount']==0 and set(d['stageCounts'])<=set(allowed)
 for name,info in d['artifacts'].items():
  if info['contentsArchived']:assert H(file.parent/name)==info['sha256']
 business=json.loads((file.parent/'business-parity.json').read_text());assert business['Cases']==business['Passed'] and business['ErrorType']=='none'
 if d['mode'].startswith('canary-'):canaries.append({'mode':d['mode'],**business})
 else:cases.append({'mode':d['mode'],'group':d['group'],'case':d['case'],**business})
 rows+=d['rows'];controllers.append({'receipt':str(file),'sha256':H(file),'argv':d['command'],'cwd':d['cwd'],'exitCode':0,'runtimeProcessorOverride':d['runtimeProcessorOverride'],'identity':d['identity'],'groupAbsent':True,'privateDirectoryReleased':True,'closed':d.get('closed'),'rows':d['rows'],'stageCounts':d['stageCounts']})
assert len(controllers)==15 and len(canaries)==3 and sum(d['Cases'] for d in cases)==100 and sum(d['Passed'] for d in cases)==100 and rows==83688
modes={mode:{'cases':sum(d['Cases'] for d in cases if d['mode']==mode),'passed':sum(d['Passed'] for d in cases if d['mode']==mode),'actualCaptures':sum(d['Captures'] for d in cases if d['mode']==mode)} for mode in ['disabled','active','capped','failing-sink']}
assert all(d['cases']==d['passed']==25 for d in modes.values());assert modes['disabled']['actualCaptures']==0 and all(modes[m]['actualCaptures']==25 for m in ['active','capped','failing-sink'])
facts=json.loads((Q/'runtime-facts.log').read_text());props=json.loads((Q/'compilation-properties.log').read_text())['Properties'];assert props['Optimize']=='false' and props['Configuration']=='Debug' and facts['runtimeVersion']=='10.0.14' and facts['processorCount']==14 and facts['processorOverride'] is None
for key in ['oracle','candidate','scratch']:assert facts[key]['configuration']=='Debug' and facts[key]['jitOptimizerDisabled'] is True
assert facts['oracle']['debuggingFlags']==facts['candidate']['debuggingFlags']==facts['scratch']['debuggingFlags'] and facts['samePublicIdentity'] and facts['differentServerInstances']
assert H(P/'DomainServiceWorkloadAssertionHandler.private.cs')=='bf9c43a96b935f9d570fa7de46f81b4b577d68e700e287929bf8eb110c788e91' and H(P/'CaptureCounters.cs')=='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498' and H(P/'GatewayControllerBootstrap.cs')=='05bbb6de2e160f5ee87546b2980137434e388c74494f4a111c88a59ec6f85a42'
private=Path('/tmp/story65-capture-counter-private-20261107');s=private.lstat();assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700);assert stat.S_ISDIR(s.st_mode) and not any(private.iterdir())
active=[];targets={str(P/'AssertionProof.csproj'),str(P/'bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll'),str(P/'freshcontroller.py')}
for process in psutil.process_iter(['pid','cmdline']):
 try:
  if any(arg in targets for arg in process.info['cmdline'] or []):active.append(process.info['pid'])
 except(psutil.NoSuchProcess,psutil.AccessDenied):pass
assert active==[]
commands=[]
for file in sorted(Q.glob('*.json')):
 try:d=json.loads(file.read_text())
 except:continue
 if 'argv' in d:commands.append({'receipt':str(file),'sha256':H(file),**d})
scratch={str(f.relative_to(P)):H(f) for f in (P/'bin/Debug/net10.0').glob('Hexalith.EventStore.Server.Tests.*') if f.suffix in {'.dll','.pdb','.json'}}
parent=Q/'parent-scratch-audit.json';assert parent.exists()
producer=Path('/tmp/story65-outbound-assertion-ops-prep-20261107/producer-attempts/attempt-kj2q5zzx/receipt.json');assert H(producer)=='01a5afec339c367b6108285dbccb06cb065f6b32b5600d54e0b109ef3584f568'
producer_audit=producer.parent/'parent-producer-audit.json';assert producer_audit.exists()
static_audit=Path('/tmp/story65-outbound-assertion-prep-static-audit-20261107.json');assert static_audit.exists()
assert '0 Warning(s)' in (Q/'build-1.log').read_text() and '0 Error(s)' in (Q/'build-1.log').read_text()
receipt={'scratchQualificationOnly':True,'featureAcceptance':False,'preparationReceiptSha256':H(P/'preparation-receipt.json'),'passingBehavioralCells':100,'passingFreshCanaries':3,'controlledProcesses':15,'allPassed':True,'strictValidatedRows':rows,'allRowsZeroAllocationAndCountColumns':True,'strictAllowedStages':allowed,'wholeBeforeOriginalNullGuard':True,'fixedGatewayRoleOnlySelectedRootSixRuntimeComposition':True,'populationIncludesNonCanonicalFactoryRequests':True,'issuerClientExcludedByUnchangedBuilderFilter':True,'wholeAndBaseExcludeOuterHttpClientResponseContentReadBuffering':True,'missingStagesDoNotEstablishOutcomeOrResult':True,'noRootClientControllerRequestJoin':True,'modes':modes,'canaries':canaries,'runtimeFacts':facts,'compilationProperties':props,'sameIdentityDistinctServerAlcsQualified':True,'runtimeCriticalDependencyReferenceChecksPassed':True,'allNonServerDependenciesSharedByLoader':True,'defaultServerFallbackForbidden':True,'privateHelperOnlyInCandidateServer':True,'sourceSha256':after['sourceSha256'],'app147Sha256':after['app147Sha256'],'variantDllPdbSha256':after['variantSha256'],'source54App147AndVariantsBeforeAfterIdentical':True,'shared152InputsUnchanged':True,'preparedFilesSha256':M['filesSha256'],'scratchBinarySha256':scratch,'commands':commands,'controlledProcessReceipts':controllers,'parentIndependentAudit':{'path':str(parent),'sha256':H(parent)},'producerReceipt':{'path':str(producer),'sha256':H(producer)},'parentProducerAudit':{'path':str(producer_audit),'sha256':H(producer_audit)},'producerHistoryOwner':'/tmp/story65-outbound-assertion-ops-prep-20261107','preparationStaticAudit':{'path':str(static_audit),'sha256':H(static_audit),'initialCensusHashAssumptionPreserved':True,'noSourceFixRequired':True},'scratchBuildAttempts':1,'failedScratchBuilds':0,'finalBuildWarnings':0,'finalBuildErrors':0,'coreSourceBf9Helper112Bootstrap05bbbUnchanged':True,'publicPartialIdentityAndGeneratedLoggerPreserved':True,'privateDirectoryPinnedEmpty':True,'ownedScratchProcessesAbsent':True,'applicationBuildsByThisAgent':0,'native29Executed':False,'hostsStarted':0,'nativeDisplayIdentityPolicyPlanned':'strict exact rendered-name multiset full28+joint1, static collisionmultiplicity3, no ordinary1038inflation','pending':['Root final review','Separately authorized actual29 native multiset and consumer audit','Separately authorized managed timing diagnostic and ordinary restoration']}
file=Q/'final-scratch-qualification.json';assert not file.exists();file.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'receipt':str(file),'sha256':H(file),'cells':100,'canaries':3,'processes':15,'rows':rows,'directoryEmpty':True,'ownedProcesses':active}))
