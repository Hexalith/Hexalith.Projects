from pathlib import Path
import datetime,hashlib,importlib.util,json,os,subprocess,sys,xml.etree.ElementTree as ET
root=Path('/tmp/story61-isolated-20261103-r5tqvlm3/projects')
packet=Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/real-owner-diagnostic-20261104')
evidence=packet/'runtime';source=root/'tools/qualification/run_supported_project_reads.py'
spec=importlib.util.spec_from_file_location('story61_real_owner_diagnostic_launcher',source);launcher=importlib.util.module_from_spec(spec);sys.modules[spec.name]=launcher;spec.loader.exec_module(launcher)
checkpoint=packet.parent/'query-observer-metadata-checkpoint-20261104/commands.json';expected=json.loads(checkpoint.read_text())['sourceManifestSha256'];manifest=launcher.source_manifest();assert hashlib.sha256(json.dumps(manifest,sort_keys=True).encode()).hexdigest()==expected
facts={'diagnosticOnly':True,'qualificationIntentionallyRefused':True,'sourceManifestSha256':expected,'sourceInputCount':len(manifest),'featureNativeMatrixInvoked':False,'realOwnerNativeMethodInvoked':False,'continuousWatchersStarted':False,'commands':[],'nativeConsoleCaptured':False}
original_runner=launcher.run_test_process;original_verify=launcher.verify_discovery;original_execute=launcher.execute
method='Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests.GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures'
observed={}
def verify(configuration,registry,isolation):
 result=original_verify(configuration,registry,isolation)
 observed.update(configuration=configuration,registry=registry,isolation=isolation,discovery=result)
 facts['strictDiscovery']=result
 return result

def run_target(command,*,env,timeout,cleanup):
 assert not facts['realOwnerNativeMethodInvoked']
 assert command[0]=='dotnet' and command[2:4]==['-method',launcher.SCENARIO_TEST_TYPE+'.'+launcher.SCENARIO_METHODS['main']]
 assert observed and timeout==900
 facts['originalNativeSelectionIntentionallyNotInvoked']=command
 artifacts=Path(command[1]).parents[3];server_project=root/'tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj'
 build=['dotnet','build',str(server_project),'-c','Debug','-m:1','--artifacts-path',str(artifacts)]
 facts['commands'].append(build);build_output=original_execute(build,env=env)
 path=packet/'server-build.log';path.write_text(build_output);path.chmod(0o600)
 assembly=next((artifacts/'bin/Hexalith.Projects.Server.Tests').glob('debug*/Hexalith.Projects.Server.Tests.dll'))
 private=observed['configuration'].parent
 regression_env=launcher.regression_environment(env,private/'endpoints.json')
 selected=['dotnet',str(assembly),'-method',method,'-xml',str(evidence/'server.xml')]
 facts['commands'].append(selected);facts['realOwnerNativeMethodInvoked']=True
 facts['serverAssemblySha256']=hashlib.sha256(assembly.read_bytes()).hexdigest()
 result=original_runner(selected,env=regression_env,timeout=timeout,cleanup=cleanup)
 path=evidence/'server.log';path.write_text(result.stdout);path.chmod(0o600)
 if (evidence/'server.xml').exists():(evidence/'server.xml').chmod(0o600)
 assemblies=ET.parse(evidence/'server.xml').getroot().findall('assembly')
 tests=list(ET.parse(evidence/'server.xml').getroot().iter('test'))
 facts['nativeResult']=[{key:item.get(key) for key in ('total','passed','failed','errors','skipped','not-run','time')} for item in assemblies]
 if result.returncode or len(tests)!=1 or tests[0].get('name')!=method or tests[0].get('result')!='Pass':
  raise RuntimeError('Targeted real-owner native diagnostic did not pass; qualification remains refused.')
 launcher.require_complete_native_results(assemblies)
 facts['requestWindows']=launcher.real_owner_request_windows(evidence/'server.xml')
 fixture=json.loads((private/'fixture.json').read_text())['Projects']['SupportedReads']
 owned=launcher.find_owned_apphost(private/'fixture.json',fixture['IsolationId'])
 if owned is None or not launcher.owns_current_apphost(owned):raise RuntimeError('Diagnostic AppHost ownership changed before console capture.')
 current=original_verify(observed['configuration'],observed['registry'],observed['isolation'])
 capture_report={'sourceManifestSha256':expected,'ownedAppHostSession':{key:value for key,value in owned.items() if not key.startswith('_')},'discoveryAfterReads':current,'commands':[]}
 launcher.capture_native_console(owned,observed['configuration'],observed['registry'],env,evidence,capture_report)
 facts['nativeConsoleCaptured']=True;facts['nativeConsoleEvidence']=capture_report['nativeConsoleEvidence'];facts['commands'].extend(capture_report['commands'])
 facts['testAssemblyUnchanged']=hashlib.sha256(assembly.read_bytes()).hexdigest()==facts['serverAssemblySha256']
 if not facts['testAssemblyUnchanged']:raise RuntimeError('Diagnostic native assembly changed during verification.')
 raise RuntimeError('Targeted real-owner diagnostic intentionally refuses qualification after actual native method and console snapshot; no distributed matrix selected.')
launcher.verify_discovery=verify;launcher.run_test_process=run_target
sys.argv=[str(source),'--scenario','main','--timeout','900','--evidence',str(evidence)]
try:
 code=launcher.main()
finally:
 facts['completedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat();facts['sourceUnchanged']=launcher.source_manifest()==manifest
 (packet/'diagnostic-facts.json').write_text(json.dumps(facts,indent=2)+'\n')
print('Targeted real-owner diagnostic finished; qualification remains refused.')
raise SystemExit(code)
