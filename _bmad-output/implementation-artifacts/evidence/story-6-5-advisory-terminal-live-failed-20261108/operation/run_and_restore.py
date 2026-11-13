from pathlib import Path
import argparse,json,subprocess,sys,time
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
commands={phase:[sys.executable,str(PREP/'build_restore.py'),'--phase',phase,'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']for phase in['private','ordinary']}
commands['managed']=[sys.executable,str(PREP/'supervise_managed.py'),'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']
commands['native139']=[sys.executable,str(PREP/'native_qualification.py'),'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']
if not args.execute:print(json.dumps({'preparationOnly':True,'commands':commands,'finally':'Ordinary restore after EVERY private build attempt, including partial/nonzero builds; after managed launch only once exact cleanup proof passes. Failed restore retries create distinct attempt directories.'},indent=2));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True)
EVIDENCE=Path('/tmp/story65-advisory-terminal-diagnostic-20261108');status={'diagnosticOnly':True,'featureAcceptance':False,'privateAttempted':False,'native139Attempted':False,'managedAttempted':False,'ordinaryRestoreAttempted':False}
def qualify_cleanup():
 safe=True
 if status['native139Attempted']:
  pointer=PREP/'latest-native-attempt.json';safe=pointer.exists()
  if safe:
   work=Path(json.loads(pointer.read_text())['attemptDirectory']);safe=work.parent==PREP and(work/'receipt.json').exists()
   if safe:
    native139=json.loads((work/'receipt.json').read_text());safe=native139.get('nativeProcessAndDescendantsAbsent')is True
    if native139.get('nativeStarted')is True:
     identity=native139.get('ownedNativeIdentity')
     if isinstance(identity,dict)and type(identity.get('pid'))is int and type(identity.get('processGroup'))is int:safe=safe and not group_members(identity['processGroup'])
     else:safe=False;status['nativeOwnedIdentityMissingCleanupUnproven']=True
    status['nativeCounterDirectoryEmpty']=native139.get('counterDirectoryEmpty')is True
    if not status['nativeCounterDirectoryEmpty']:
     status['measurementComplete']=False;status['nativeDiagnosticMetadataIncomplete']=True;status['remainingCounterFilesPreservedWithoutDeletion']=True
 if status['managedAttempted']:
  audit=EVIDENCE/'independent-cleanup-source-audit.json'
  audit_command=[sys.executable,'/tmp/story65-canonical-hash-acceptance-prep-20261108/audit.py','--evidence',str(EVIDENCE/'managed'),'--output',str(audit)]
  observations=[];deadline=time.monotonic()+10;ordinal=0
  while True:
   ordinal+=1;observed=EVIDENCE/('cleanup-observation-'+str(ordinal)+'.json');command=[*audit_command[:-1],str(observed)]
   checked=subprocess.run(command,cwd=ROOT,capture_output=True)
   snapshot=json.loads(observed.read_text())if checked.returncode==0 and observed.exists()else{}
   early_path=EVIDENCE/'early-runtime-identities.json';early=json.loads(early_path.read_text()).get('identities',[])if early_path.exists()else[]
   def absent_now(pid,ticks):
    try:
     state=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return state[19]!=str(ticks)
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
   native_safe=isinstance(native,list)and all(item.get('descendantsAbsent')is True and type(item.get('groupId'))is int and not group_members(item['groupId'])for item in native)or native is None and never_native
   strict_absent=[]
   for item in value.get('identities',[]):
    try:fields=Path('/proc',str(item['pid']),'stat').read_text().rsplit(')',1)[1].split();gone=fields[19]!=str(item['startTicks'])
    except(FileNotFoundError,ProcessLookupError):gone=True
    strict_absent.append(gone)
   strict_all_recorded=bool(strict_absent)and all(strict_absent)
   early_path=EVIDENCE/'early-runtime-identities.json';early=[]
   if early_path.exists():
    early=json.loads(early_path.read_text())['identities'];assert len(early)==6 and len({item['role']for item in early})==6
   def absent(pid,ticks):
    try:
     state=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return state[19]!=str(ticks)
    except FileNotFoundError:return True
   early_absent=bool(early)and all(absent(item['pid'],item['startTicks'])and absent(item['wrapperPid'],item['wrapperStartTicks'])for item in early)
   no_hosts_started=not early and run.get('ownedAppHostSession')is None and not any(isinstance(command,list)and command[:2]==['aspire','start']for command in run.get('commands',[]))
   sidecars_safe=value.get('ownedSidecarsAbsent')is True or no_hosts_started and value.get('ownedSidecarsAbsent')is None
   safe=all(value.get(k)is True for k in['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent'])and sidecars_safe and value.get('fullLiveManifestHashMismatches')==0 and native_safe and strict_all_recorded and(early_absent or no_hosts_started)
   save(EVIDENCE/'supplemental-cleanup-qualification.json',{'diagnosticOnly':True,'nativeNoneAcceptedOnlyWithProvenNoNativeExecution':native is None and never_native,'nativeExecutionEntryCounts':entries,'earlyRuntimeIdentityCount':len(early)*2,'allEarlyIdentitiesAbsent':early_absent,'noOwnedHostsStarted':no_hosts_started,'safeForOrdinaryRestoration':safe,'allRecordedPidStartIdentitiesAbsentIncludingZombies':strict_all_recorded,'allNativeSeedGroupsFreshlyAbsentIncludingZombies':native_safe})
   value.update(diagnosticOnly=True,featureAcceptance=False,ordinaryAcceptanceOnly=False,privateProjectsAdvisoryTerminalOverlay=True,sdkOrdinary=True,profilerEnabled=False);save(audit,value)
 if status['managedAttempted']and safe:
  # Metadata is inspected only after all normal/native/early owned identities close.
  # A missing/incomplete recorder never delays cleanup or blocks ordinary restore.
  try:
   supervisor=json.loads((EVIDENCE/'supervisor.json').read_text());status['underlyingManagedLauncherExitCode']=supervisor.get('launcherExitCode')
   if supervisor.get('controlPublished')is True:
    from validate_closed_terminal import validate
    validation=validate(supervisor);status['measurementComplete']=True
   else:
    validation={'diagnosticOnly':True,'featureAcceptance':False,'complete':False,'reason':'control-publication-partial-possible'if supervisor.get('controlPublicationAttempted')else'no-admission-window-published','doesNotProveZeroWork':True};status['measurementComplete']=False
  except Exception as failure:
   validation={'diagnosticOnly':True,'featureAcceptance':False,'complete':False,'reason':'closed-recorder-validation-failed','failureType':type(failure).__name__};status['measurementComplete']=False
  try:save(EVIDENCE/'closed-recorder-validation.json',validation)
  except Exception as failure:status['metadataValidationWriteFailureType']=type(failure).__name__
 return safe
try:
 status['privateAttempted']=True;status['privateExitCode']=subprocess.run(commands['private'],cwd=ROOT).returncode
 if status['privateExitCode']==0:
  status['native139Attempted']=True;status['native139ExitCode']=subprocess.run(commands['native139'],cwd=ROOT).returncode
  if status['native139ExitCode']==0:
   work=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory']);assert work.parent==PREP
   qualified=json.loads((work/'receipt.json').read_text());assert qualified['passed']is True and qualified['nativeProcessAndDescendantsAbsent']is True and qualified['counterDirectoryEmpty']is True
   status['managedAttempted']=True;status['managedExitCode']=subprocess.run(commands['managed'],cwd=ROOT).returncode
  else:status['managedSkippedReason']='private-advisory-native139-qualification-failed'
except Exception as failure:
 status['pipelineFailureType']=type(failure).__name__
finally:
 safe=False
 try:safe=qualify_cleanup()
 except Exception as failure:
  status['cleanupQualificationFailureType']=type(failure).__name__;status['cleanupQualificationUnproven']=True
 if status['privateAttempted']and safe:
  status['ordinaryRestoreAttempted']=True
  try:status['ordinaryExitCode']=subprocess.run(commands['ordinary'],cwd=ROOT).returncode
  except Exception as failure:status['ordinaryRestoreFailureType']=type(failure).__name__
 else:status['ordinaryRestoreDeferredReason']='owned-runtime-absence-or-cleanup-qualification-unproven'
 try:save(PREP/'latest-operational-result.json',status)
 except Exception as failure:status['latestStatusWriteFailureType']=type(failure).__name__
print(json.dumps(status))
