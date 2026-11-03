from pathlib import Path
import hashlib, importlib.util, json, sys
root = Path('/home/administrator/projects/hexalith/projects')
evidence = root / '_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/owner-boundary-diagnostic-20261103'
source = root / 'tools/qualification/run_supported_project_reads.py'
spec = importlib.util.spec_from_file_location('story61_owner_boundary_launcher', source)
launcher = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = launcher
spec.loader.exec_module(launcher)
original_native = launcher.run_test_process
invoked = False
def diagnose(command, *, env, timeout, cleanup):
 global invoked
 if invoked or '-method' not in command or not any('MainReadsUseActualPersistedOwnersAsync' in part for part in command):
  raise RuntimeError('Diagnostic refuses an unexpected or repeated native dispatch.')
 invoked = True
 assembly = root / 'tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll'
 before = hashlib.sha256(assembly.read_bytes()).hexdigest()
 actual = ['dotnet', str(assembly), '-method', '*RealBrokerAndBothRs256ReceiversAuthorizeListOpenAndContinuationWithoutDomainWrites', '-xml', str(evidence / 'owner-boundary.xml')]
 receipt = {'diagnosticOnly': True, 'expectedQualificationScenarioNotExecuted': command, 'actualNativeCommand': actual, 'assemblySha256': before, 'launcherSourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'normalGuardsAndCleanupUnchanged': True, 'qualificationIntentionallyRefused': True}
 (evidence / 'owner-boundary-command.json').write_text(json.dumps(receipt, indent=2) + '\n')
 result = original_native(actual, env=launcher.regression_environment(env, Path(env['SUPPORTED_PROJECT_READ_ENDPOINTS'])), timeout=timeout, cleanup=cleanup)
 (evidence / 'owner-boundary.log').write_text(result.stdout)
 receipt.update(nativeExitCode=result.returncode, assemblyUnchanged=hashlib.sha256(assembly.read_bytes()).hexdigest()==before)
 (evidence / 'owner-boundary-command.json').write_text(json.dumps(receipt, indent=2) + '\n')
 raise RuntimeError('Diagnostic-only native owner-boundary selection completed; qualification deliberately refused without executing its scenario or accepting a matrix.')
launcher.run_test_process = diagnose
sys.argv = [str(source), '--scenario', 'main', '--timeout', '900', '--evidence', str(evidence)]
raise SystemExit(launcher.main())
