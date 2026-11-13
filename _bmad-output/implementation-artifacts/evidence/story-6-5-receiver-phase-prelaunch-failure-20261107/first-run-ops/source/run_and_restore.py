from pathlib import Path
import argparse,json,subprocess,sys,time
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
commands={phase:[sys.executable,str(PREP/'build_restore.py'),'--phase',phase,'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']for phase in['private','ordinary']}
commands['managed']=[sys.executable,str(PREP/'supervise_managed.py'),'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']
commands['nativeReceiver']=[sys.executable,str(PREP/'native_qualification.py'),'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']
if not args.execute:print(json.dumps({'preparationOnly':True,'commands':commands,'finally':'Ordinary restore after EVERY private build attempt, including partial/nonzero builds; after managed launch only once exact cleanup proof passes. Failed restore retries create distinct attempt directories.'},indent=2));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True)
EVIDENCE=Path('/tmp/story65-receiver-phase-diagnostic-20261107');status={'diagnosticOnly':True,'featureAcceptance':False,'privateAttempted':False,'nativeReceiverAttempted':False,'managedAttempted':False,'ordinaryRestoreAttempted':False}
try:
 status['privateAttempted']=True;status['privateExitCode']=subprocess.run(commands['private'],cwd=ROOT).returncode
 if status['privateExitCode']==0:
  status['nativeReceiverAttempted']=True;status['nativeReceiverExitCode']=subprocess.run(commands['nativeReceiver'],cwd=ROOT).returncode
  if status['nativeReceiverExitCode']==0:
   work=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory']);assert work.parent==PREP
   qualified=json.loads((work/'receipt.json').read_text());assert qualified['passed']is True and qualified['nativeProcessAndDescendantsAbsent']is True and qualified['counterDirectoryEmpty']is True
   status['managedAttempted']=True;status['managedExitCode']=subprocess.run(commands['managed'],cwd=ROOT).returncode
  else:status['managedSkippedReason']='private-receiver-native-qualification-failed'
finally:
 safe=True
 if status['nativeReceiverAttempted']:
  pointer=PREP/'latest-native-attempt.json';safe=pointer.exists()
  if safe:
   work=Path(json.loads(pointer.read_text())['attemptDirectory']);safe=work.parent==PREP and(work/'receipt.json').exists()
   if safe:
    nativeReceiver=json.loads((work/'receipt.json').read_text());safe=nativeReceiver.get('nativeProcessAndDescendantsAbsent')is True
    status['nativeCounterDirectoryEmpty']=nativeReceiver.get('counterDirectoryEmpty')is True
    if not status['nativeCounterDirectoryEmpty']:
     status['measurementComplete']=False;status['nativeDiagnosticMetadataIncomplete']=True;status['remainingCounterFilesPreservedWithoutDeletion']=True
 if status['managedAttempted']:
  audit=EVIDENCE/'independent-cleanup-source-audit.json'
  audit_command=[sys.executable,'/tmp/story65-receiver-acceptance-prep-20261107/audit.py','--evidence',str(EVIDENCE/'managed'),'--output',str(audit)]
  observations=[];deadline=time.monotonic()+10;ordinal=0
  while True:
   ordinal+=1;observed=EVIDENCE/('cleanup-observation-'+str(ordinal)+'.json');command=[*audit_command[:-1],str(observed)]
   checked=subprocess.run(command,cwd=ROOT,capture_output=True)
   snapshot=json.loads(observed.read_text())if checked.returncode==0 and observed.exists()else{}
   early_path=EVIDENCE/'early-runtime-identities.json';early=json.loads(early_path.read_text()).get('identities',[])if early_path.exists()else[]
   def absent_now(pid,ticks):
    try:
     state=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return state[0]=='Z'or state[19]!=str(ticks)
    except FileNotFoundError:return True
   early_ready=bool(early)and len(early)==6 and all(absent_now(item['pid'],item['startTicks'])and absent_now(item['wrapperPid'],item['wrapperStartTicks'])for item in early)
   observations.append({'ordinal':ordinal,'exitCode':checked.returncode,'path':str(observed),'recordedProcessAndContainerAbsences':snapshot.get('allIndependentRecordedProcessAndContainerAbsences')is True,'allEarlyIdentitiesAbsent':early_ready})
   if checked.returncode==0 and snapshot.get('allIndependentRecordedProcessAndContainerAbsences')is True and(early_ready or not early):
    save(audit,snapshot);break
   if time.monotonic()>=deadline:
    if snapshot:save(audit,snapshot)
    break
   time.sleep(min(.25,max(0,deadline-time.monotonic())))
  save(EVIDENCE/'cleanup-observation-history.json',{'diagnosticOnly':True,'signalsSent':False,'maximumReadOnlyPollingSeconds':10,'observations':observations})
  safe=checked.returncode==0 and audit.exists()
  if safe:
   value=json.loads(audit.read_text());run=json.loads((EVIDENCE/'managed/run.json').read_text())
   entries=json.loads((EVIDENCE/'execution-entry-counts.json').read_text());native=value.get('nativeTestProcessCleanup')
   never_native=entries['nativeInvocationEntries']==0 and entries['browserInvocationEntries']==0 and run.get('testAssemblySha256')is None and run.get('nativeTestCommand')is None and run.get('browserSeedCommand')is None and not any(isinstance(command,list)and len(command)>1 and command[0]=='dotnet'and Path(command[1]).name=='Hexalith.Projects.Integration.Tests.dll'for command in run.get('commands',[]))
   native_safe=isinstance(native,list)and all(item.get('descendantsAbsent')is True for item in native)or native is None and never_native
   early_path=EVIDENCE/'early-runtime-identities.json';early=[]
   if early_path.exists():
    early=json.loads(early_path.read_text())['identities'];assert len(early)==6 and len({item['role']for item in early})==6
   def absent(pid,ticks):
    try:
     state=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return state[0]=='Z'or state[19]!=str(ticks)
    except FileNotFoundError:return True
   early_absent=bool(early)and all(absent(item['pid'],item['startTicks'])and absent(item['wrapperPid'],item['wrapperStartTicks'])for item in early)
   no_hosts_started=not early and run.get('ownedAppHostSession')is None and not any(isinstance(command,list)and command[:2]==['aspire','start']for command in run.get('commands',[]))
   sidecars_safe=value.get('ownedSidecarsAbsent')is True or no_hosts_started and value.get('ownedSidecarsAbsent')is None
   safe=all(value.get(k)is True for k in['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent'])and sidecars_safe and value.get('fullLiveManifestHashMismatches')==0 and native_safe and(early_absent or no_hosts_started)
   save(EVIDENCE/'supplemental-cleanup-qualification.json',{'diagnosticOnly':True,'nativeNoneAcceptedOnlyWithProvenNoNativeExecution':native is None and never_native,'nativeExecutionEntryCounts':entries,'earlyRuntimeIdentityCount':len(early)*2,'allEarlyIdentitiesAbsent':early_absent,'noOwnedHostsStarted':no_hosts_started,'safeForOrdinaryRestoration':safe})
   value.update(diagnosticOnly=True,featureAcceptance=False,ordinaryAcceptanceOnly=False,privateReceiverPhaseOverlay=True,sdkOrdinary=False,profilerEnabled=False);save(audit,value)
 if status['managedAttempted']and safe:
  # Metadata is inspected only after all normal/native/early owned identities close.
  # A missing/incomplete recorder never delays cleanup or blocks ordinary restore.
  try:
   supervisor=json.loads((EVIDENCE/'supervisor.json').read_text())
   if supervisor.get('controlPublished')is True:
    from validate_closed_recorders import validate
    validation=validate(supervisor);status['measurementComplete']=True
   else:
    validation={'diagnosticOnly':True,'featureAcceptance':False,'complete':False,'reason':'no-admission-window-published'};status['measurementComplete']=False
  except Exception as failure:
   validation={'diagnosticOnly':True,'featureAcceptance':False,'complete':False,'reason':'closed-recorder-validation-failed','failureType':type(failure).__name__};status['measurementComplete']=False
  try:save(EVIDENCE/'closed-recorder-validation.json',validation)
  except Exception as failure:status['metadataValidationWriteFailureType']=type(failure).__name__
 if status['privateAttempted']and safe:
  status['ordinaryRestoreAttempted']=True;status['ordinaryExitCode']=subprocess.run(commands['ordinary'],cwd=ROOT).returncode
 else:status['ordinaryRestoreDeferredReason']='owned-runtime-absence-unproven'
 save(PREP/'latest-operational-result.json',status)
print(json.dumps(status))
