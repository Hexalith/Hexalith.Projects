from pathlib import Path
import argparse,json,subprocess,sys
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
commands={phase:[sys.executable,str(PREP/'build_restore.py'),'--phase',phase,'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']for phase in['private','ordinary']}
commands['managed']=[sys.executable,str(PREP/'supervise_managed.py'),'--execute','--pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED']
if not args.execute:print(json.dumps({'preparationOnly':True,'commands':commands,'finally':'Ordinary restore after EVERY private build attempt, including partial/nonzero builds; after managed launch only once exact cleanup proof passes. Failed restore retries create distinct attempt directories.'},indent=2));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True)
EVIDENCE=Path('/tmp/story65-capture-counter-diagnostic-20261107');status={'diagnosticOnly':True,'featureAcceptance':False,'privateAttempted':False,'managedAttempted':False,'ordinaryRestoreAttempted':False}
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
   value=json.loads(audit.read_text());safe=all(value.get(k)is True for k in['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent'])and value.get('fullLiveManifestHashMismatches')==0 and isinstance(value.get('nativeTestProcessCleanup'),list)and all(item.get('descendantsAbsent')is True for item in value['nativeTestProcessCleanup'])
   value.update(diagnosticOnly=True,featureAcceptance=False,ordinaryAcceptanceOnly=False,privateSdkOverlay=True,profilerEnabled=False);save(audit,value)
 if status['privateAttempted']and safe:
  status['ordinaryRestoreAttempted']=True;status['ordinaryExitCode']=subprocess.run(commands['ordinary'],cwd=ROOT).returncode
 else:status['ordinaryRestoreDeferredReason']='owned-runtime-absence-unproven'
 save(PREP/'latest-operational-result.json',status)
print(json.dumps(status))
