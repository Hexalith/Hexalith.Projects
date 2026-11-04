from pathlib import Path
import datetime,hashlib,importlib.util,json,os,sys,tempfile
root=Path('/tmp/story61-isolated-20261103-r5tqvlm3/projects')
packet=Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/startup-diagnostic-20261104')
evidence=packet/'runtime'
source=root/'tools/qualification/run_supported_project_reads.py'
spec=importlib.util.spec_from_file_location('story61_startup_only_launcher',source)
launcher=importlib.util.module_from_spec(spec);sys.modules[spec.name]=launcher;spec.loader.exec_module(launcher)
expected='a2117a9c2f5e99f0776675507a661e914263637f1f76d0341b6f3564c971718e'
manifest=launcher.source_manifest()
assert hashlib.sha256(json.dumps(manifest,sort_keys=True).encode()).hexdigest()==expected
private=Path(tempfile.mkdtemp(prefix='story61-native-startup-private-'));os.chmod(private,0o700)
facts={'diagnosticOnly':True,'qualificationIntentionallyRefused':True,'sourceManifestSha256':expected,'privateRawDirectory':str(private),'nativeLogs':{},'discoveryInvoked':False,'discoveryPassed':False,'featureNativeTestInvoked':False,'continuousWatchersStarted':False}
original_execute=launcher.execute;original_verify=launcher.verify_discovery
native_resources={name+'-dapr-cli' for name in launcher.READ_APPS}
def save_private(name,output):
 path=private/name
 descriptor=os.open(path,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
 with os.fdopen(descriptor,'wb') as stream:stream.write(output.encode())
 return {'bytes':len(output.encode()),'sha256':hashlib.sha256(output.encode()).hexdigest()}
def execute(command,**kwargs):
 output=original_execute(command,**kwargs)
 if len(command)>2 and command[:2]==['aspire','logs'] and command[2] in native_resources:
  observation=save_private(command[2]+'.json',output)
  observation.update(queryIncludedHidden='--include-hidden' in command,facts=launcher.safe_log_facts(output))
  facts['nativeLogs'][command[2]]=observation
 return output
def verify(configuration,registry,isolation):
 facts['discoveryInvoked']=True
 graph_raw=execute(['aspire','describe','--apphost',str(launcher.APPHOST),'--format','Json','--include-hidden','--non-interactive'],timeout=30)
 facts['privateHiddenGraph']=save_private('hidden-resource-graph.json',graph_raw)
 graph=json.loads(graph_raw)
 facts['nativeResourceMetadata']=[{key:value for key,value in resource.items() if key in ('displayName','name','isHidden','hidden','resourceType','state','healthStatus')} for resource in graph.get('resources',[]) if resource.get('displayName') in native_resources]
 result=original_verify(configuration,registry,isolation)
 facts['discoveryPassed']=True
 facts['actualDiscovery']=result
 raise RuntimeError('Startup-only diagnostic intentionally refuses qualification after actual strict discovery; no feature native test selected.')
launcher.execute=execute;launcher.verify_discovery=verify
sys.argv=[str(source),'--scenario','duplicate','--timeout','900','--evidence',str(evidence)]
try:
 code=launcher.main()
finally:
 facts['completedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
 facts['sourceUnchanged']=launcher.source_manifest()==manifest
 (packet/'diagnostic-facts.json').write_text(json.dumps(facts,indent=2)+'\n')
print('Startup-only diagnostic finished; qualification remains refused.')
raise SystemExit(code)
