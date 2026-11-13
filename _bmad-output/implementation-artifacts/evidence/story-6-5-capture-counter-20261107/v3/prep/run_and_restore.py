from pathlib import Path
import argparse,json,subprocess,sys
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
commands={phase:[sys.executable,str(PREP/'build_restore.py'),'--phase',phase,'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']for phase in['private','ordinary']}
commands['managed']=[sys.executable,str(PREP/'supervise_managed.py'),'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']
if not args.execute:print(json.dumps({'preparationOnly':True,'commands':commands,'finally':'Ordinary restore after EVERY private build attempt, including partial/nonzero builds; after managed launch only once exact cleanup proof passes. Failed restore retries create distinct attempt directories.'},indent=2));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True)
EVIDENCE=Path('/tmp/story65-capture-counter-proof-diagnostic-20261107');status={'diagnosticOnly':True,'featureAcceptance':False,'privateAttempted':False,'managedAttempted':False,'ordinaryRestoreAttempted':False}
try:
 status['privateAttempted']=True;status['privateExitCode']=subprocess.run(commands['private'],cwd=ROOT).returncode
 if status['privateExitCode']==0:
  status['managedAttempted']=True;status['managedExitCode']=subprocess.run(commands['managed'],cwd=ROOT).returncode
finally:
 safe=True
 if status['managedAttempted']:
  audit=EVIDENCE/'independent-cleanup-source-audit.json'
  audit_command=[sys.executable,'/tmp/story65-receiver-acceptance-prep-20261107/audit.py','--evidence',str(EVIDENCE/'managed'),'--output',str(audit)]
  checked=subprocess.run(audit_command,cwd=ROOT,capture_output=True)
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
    try:return Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()[19]!=str(ticks)
    except FileNotFoundError:return True
   early_absent=bool(early)and all(absent(item['pid'],item['startTicks'])and absent(item['wrapperPid'],item['wrapperStartTicks'])for item in early)
   no_hosts_started=not early and run.get('ownedAppHostSession')is None and not any(isinstance(command,list)and command[:2]==['aspire','start']for command in run.get('commands',[]))
   sidecars_safe=value.get('ownedSidecarsAbsent')is True or no_hosts_started and value.get('ownedSidecarsAbsent')is None
   safe=all(value.get(k)is True for k in['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent'])and sidecars_safe and value.get('fullLiveManifestHashMismatches')==0 and native_safe and(early_absent or no_hosts_started)
   save(EVIDENCE/'supplemental-cleanup-qualification.json',{'diagnosticOnly':True,'nativeNoneAcceptedOnlyWithProvenNoNativeExecution':native is None and never_native,'nativeExecutionEntryCounts':entries,'earlyRuntimeIdentityCount':len(early)*2,'allEarlyIdentitiesAbsent':early_absent,'noOwnedHostsStarted':no_hosts_started,'safeForOrdinaryRestoration':safe})
   value.update(diagnosticOnly=True,featureAcceptance=False,ordinaryAcceptanceOnly=False,privateSdkOverlay=True,profilerEnabled=False);save(audit,value)
 if status['privateAttempted']and safe:
  status['ordinaryRestoreAttempted']=True;status['ordinaryExitCode']=subprocess.run(commands['ordinary'],cwd=ROOT).returncode
 else:status['ordinaryRestoreDeferredReason']='owned-runtime-absence-unproven'
 save(PREP/'latest-operational-result.json',status)
print(json.dumps(status))
