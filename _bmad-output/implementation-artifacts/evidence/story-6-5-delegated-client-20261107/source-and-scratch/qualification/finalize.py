from pathlib import Path
import json,hashlib,stat,sys
import psutil
sys.path.insert(0,'/tmp/story65-delegated-client-qualification-20261107')
from qualify import P,Q,R,snapshot,M
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
after=snapshot('after');before=json.loads((Q/'before.json').read_text());assert after==before
assert H(P/'preparation-receipt.json')=='b63548acaf07a804c8b610be4d7d1067b7b904b98acd1946c2e4bc464b04e406'
for rel,wanted in M['filesSha256'].items():assert H(P/rel)==wanted
refs=json.loads((P/'oracle-fixture-pins.json').read_text())['inputsSha256'];assert all(H(name)==wanted for name,wanted in refs.items())
summary=json.loads((P/'results-qualified-final/controller-summary.json').read_text());assert summary['allPassed'] and summary['completedProcesses']==27
controllers=[];cells=[];canaries=[]
for f in sorted((P/'results-qualified-final').glob('*/*/controller-receipt.json')):
 d=json.loads(f.read_text());assert d['passed'] and d['exitCode']==0 and d['allInputsUnchanged'] and d['processAndGroupAbsent'] and d['directoryReleased'];assert not d['remainingOwnedGroupIdentities'] and not d['remainingPrivateKnownNames'] and d['unexpectedPrivateNameCount']==0
 for name,info in d['artifacts'].items():
  if info['contentsArchived']:assert H(f.parent/name)==info['sha256']
 business=json.loads((f.parent/'business-parity.json').read_text());assert business['Cases']==business['Passed'] and business['ErrorType']=='none'
 if d['mode'].startswith('canary-'):canaries.append({'mode':d['mode'],**business})
 else:cells.append({'mode':d['mode'],'group':d['group'],'case':d['case'],**business})
 controllers.append({'receipt':str(f),'sha256':H(f),'argv':d['command'],'cwd':d['cwd'],'exitCode':d['exitCode'],'runtimeProcessorOverride':d['runtimeProcessorOverride'],'identity':d['identity'],'groupAbsent':True,'directoryReleased':True,'closed':d.get('closed'),'rows':d['rows'],'stageCounts':d['stageCounts']})
assert len(controllers)==27 and len(canaries)==3 and sum(d['Cases'] for d in cells)==208 and sum(d['Passed'] for d in cells)==208 and sum(d['InvocationComparisons'] for d in cells)==224
modes={mode:{'caseCells':sum(d['Cases'] for d in cells if d['mode']==mode),'passedCells':sum(d['Passed'] for d in cells if d['mode']==mode),'invocationComparisons':sum(d['InvocationComparisons'] for d in cells if d['mode']==mode),'eligibleAdmittedCells':sum(d['EligibleAdmittedCases'] for d in cells if d['mode']==mode),'actualCaptures':sum(d['Captures'] for d in cells if d['mode']==mode),'faultCallbacks':sum(d['FaultCallbacks'] for d in cells if d['mode']==mode)} for mode in ['disabled','active','capped','failing-sink']}
assert all(d['caseCells']==52 and d['passedCells']==52 and d['invocationComparisons']==56 for d in modes.values());assert modes['disabled']['actualCaptures']==0 and all(modes[m]['actualCaptures']==43 for m in ['active','capped','failing-sink'])
assert H(P/'DelegatedOwnerQueryClient.private.cs')=='96b86cb2880e7d1cd5937c9c47dfa792a93029ee1f145b2c8d88b29f2fe426ae';assert H(P/'CaptureCounters.cs')=='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498';assert H(P/'AdvisoryWaveBootstrap.cs')=='8f5836d1ff4dabb4c5fe819ae762dfff0748b5d792f984ae1adf45af28f45375'
private=Path('/tmp/story65-capture-counter-private-20261107');s=private.lstat();assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700);assert stat.S_ISDIR(s.st_mode)and not any(private.iterdir())
active=[];targets={str(P/'bin/Debug/net10.0/Hexalith.EventStore.DomainService.Tests.dll'),str(P/'Differential.csproj'),str(P/'freshcontroller.py'),str(P/'diagnostic_child.py')}
for proc in psutil.process_iter(['pid','cmdline']):
 try:
  if any(arg in targets for arg in proc.info['cmdline'] or []):active.append(proc.info['pid'])
 except (psutil.NoSuchProcess,psutil.AccessDenied):pass
assert active==[]
commands=[]
for file in sorted(Q.glob('*.json')):
 try:d=json.loads(file.read_text())
 except:continue
 if 'argv' in d:commands.append({'receipt':str(file),'sha256':H(file),**d})
facts=json.loads((Q/'runtime-facts-4.log').read_text());properties=json.loads((Q/'compilation-properties.log').read_text())['Properties'];assert properties['Optimize']=='false' and facts['processorCount']==14 and facts['processorOverride']is None
for n in ['oracle','scratch']:assert facts[n]['configuration']=='Debug'and facts[n]['jitOptimizerDisabled']is True
assert facts['oracle']['debuggingFlags']==facts['scratch']['debuggingFlags']
binaries={str(file.relative_to(P)):H(file) for file in (P/'bin/Debug/net10.0').glob('Hexalith.EventStore.DomainService.Tests.*') if file.suffix in {'.dll','.pdb','.json'}};binaries['bin/Debug/net10.0/Hexalith.EventStore.DomainService.dll']=H(P/'bin/Debug/net10.0/Hexalith.EventStore.DomainService.dll')
receipt={'scratchQualificationOnly':True,'featureAcceptance':False,'preparationReceiptSha256':H(P/'preparation-receipt.json'),'passingCaseCells':208,'invocationComparisons':224,'passingFreshCanaries':3,'controlledProcesses':27,'allPassed':True,'modes':modes,'canaries':canaries,'allRowsClosedSchemaZeroAllocationAndCountColumns':True,'sourceSha256':after['sourceSha256'],'app147Sha256':after['app147Sha256'],'originalDomainServiceTestsDllSha256':after['originalDomainServiceTestsDllSha256'],'source51App147OriginalTestsBeforeAfterIdentical':True,'all107OracleFixtureReferencesUnchanged':True,'preparedFilesSha256':M['filesSha256'],'scratchBinarySha256':binaries,'compilationProperties':properties,'runtimeFacts':facts,'commands':commands,'controlledProcessReceipts':controllers,'initialFailuresPreserved':{'missingNamespaceBuild':'history/f4a30ab9/','aggregateCooperativeChildSnapshotFailure':'history/64a839df/','splitConfirmedChildSnapshotFailure':'history/e41dbc7d/'},'qualifiedFixtureAdjustment':'Instantaneous ChildAfterReturn equality applies only to explicit issuer-uncooperative cells. Every source Completion and GetResult remains joined exactly once for all cells; all other comparisons remain. Source-based scheduler-tail explanation was not proved with a two-oracle control.','knownWarning':'ASP0019 on transparent IDictionary.Add delegation retained to preserve Add semantics','livePrivate96Helper112Bootstrap8fUnchanged':True,'privateDirectoryPinnedEmpty':True,'ownedScratchProcessesAbsent':True,'applicationBuilds':0,'native24Executed':False,'hostsStarted':0,'pending':['Root review','Separately authorized application overlay and original24 native class gate','Separately authorized managed measurement and ordinary restoration']}
f=Q/'final-scratch-qualification.json';f.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'receipt':str(f),'sha256':H(f),'cases':208,'pairs':224,'canaries':3,'processes':27,'modes':modes,'ownedProcesses':active,'directoryEmpty':True}))
