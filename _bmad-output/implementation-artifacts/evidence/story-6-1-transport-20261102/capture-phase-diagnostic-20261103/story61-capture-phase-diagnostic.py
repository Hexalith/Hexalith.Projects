from pathlib import Path
import hashlib,importlib.util,json,sys
root=Path('/home/administrator/projects/hexalith/projects')
evidence=root/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/capture-phase-diagnostic-20261103'
source=root/'tools/qualification/run_supported_project_reads.py'
spec=importlib.util.spec_from_file_location('story61_capture_phase_launcher',source)
m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
original=m.run_test_process
invoked=False
def diagnose(command,*,env,timeout,cleanup):
 global invoked
 if invoked or not any('DuplicateDeliveryUsesActualPersistedOwnersAsync' in p for p in command):raise RuntimeError('Diagnostic refuses unexpected or repeated dispatch.')
 invoked=True
 assembly=Path(command[1]);before=hashlib.sha256(assembly.read_bytes()).hexdigest()
 receipt={'diagnosticOnly':True,'actualNativeCommand':command,'nativeScenarioUnchanged':True,'assemblySha256':before,'launcherSourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'normalGuardsAndCleanupUnchanged':True,'qualificationIntentionallyRefused':True}
 (evidence/'capture-phase-command.json').write_text(json.dumps(receipt,indent=2)+'\n')
 result=original(command,env=env,timeout=timeout,cleanup=cleanup)
 (evidence/'capture-phase.log').write_text(result.stdout)
 receipt.update(nativeExitCode=result.returncode,assemblyUnchanged=hashlib.sha256(assembly.read_bytes()).hexdigest()==before)
 (evidence/'capture-phase-command.json').write_text(json.dumps(receipt,indent=2)+'\n')
 raise RuntimeError('Diagnostic-only actual native duplicate scenario completed; qualification deliberately refused pending capture-phase investigation.')
m.run_test_process=diagnose
sys.argv=[str(source),'--scenario','duplicate','--timeout','900','--evidence',str(evidence)]
raise SystemExit(m.main())
