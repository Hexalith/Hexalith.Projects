from pathlib import Path
import datetime,hashlib,importlib.util,json,shutil,subprocess,sys,tempfile,xml.etree.ElementTree as ET

base=Path('/tmp/story61-isolated-20261103-r5tqvlm3');root=base/'projects'
canonical=Path('/home/administrator/projects/hexalith/projects')
destination=canonical/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/query-privacy-checkpoint-20261104'
destination.mkdir(mode=0o700)
sdk='references/Hexalith.EventStore/'
files=[sdk+'src/Hexalith.EventStore.ServiceDefaults/'+name for name in [
 'Extensions.cs','RequestLogPrivacyOptionsFactory.cs','Authentication/EventStoreHttpHostSecurityExtensions.cs',
 'ReadDiagnosticsPrivacy.cs','ReadDiagnosticsPrivacyExtensions.cs','ReadDiagnosticsPrivacyStartupFilter.cs','ReadDiagnosticsPrivacyScope.cs','ReadBoundaryDiagnostics.cs']]
files += [sdk+'tests/Hexalith.EventStore.Server.Tests/Telemetry/'+name for name in [
 'RequestLogPrivacyTests.cs','ReadDiagnosticsPrivacyTests.cs','ReadDiagnosticsPrivacyCollection.cs','ReadPrivacyLogExporter.cs','ReadPrivacyTraceExporter.cs']]
files += ['tests/Hexalith.Projects.Server.Tests/Queries/'+name for name in [
 'ProjectReadDenialObserver.cs','ProjectReadDenialLogger.cs','ProjectReadDenialLogProvider.cs','ProjectReadDenialTraceExporter.cs',
 'ProjectReadTelemetryCollection.cs','SupportedProjectGatewayHttpTests.cs','SupportedProjectGatewayHttpTests.DenialObservability.cs']]
frozen=json.loads((canonical/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-63/source-manifest.json').read_text())
spec=importlib.util.spec_from_file_location('story61_privacy_checkpoint',root/'tools/qualification/run_supported_project_reads.py')
m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
current=m.source_manifest()
changed={name for name in frozen.keys()|current.keys() if frozen.get(name)!=current.get(name)}
assert changed==set(files),(changed-set(files),set(files)-changed)
rows=[]
before=destination/'before';after=destination/'after'
for relative in files:
 p=root/relative;raw=p.read_bytes();old=None
 if relative in frozen:
  backup=base/'query-privacy-before'/relative.removeprefix(sdk) if relative.startswith(sdk) else base/'query-privacy-before/root'/relative
  if backup.exists():old=backup.read_bytes()
  else:
   assert relative==sdk+'tests/Hexalith.EventStore.Server.Tests/Telemetry/RequestLogPrivacyTests.cs'
   old=subprocess.run(['git','show','HEAD:'+relative.removeprefix(sdk)],cwd=root/'references/Hexalith.EventStore',check=True,stdout=subprocess.PIPE).stdout
  assert hashlib.sha256(old).hexdigest()==frozen[relative],relative
  q=before/relative;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(old)
 q=after/relative;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(raw)
 rows.append({'path':relative,'beforeSha256':hashlib.sha256(old).hexdigest() if old is not None else None,'afterSha256':hashlib.sha256(raw).hexdigest()})
diff=subprocess.run(['git','diff','--no-index','--binary','--no-ext-diff','--no-renames','before','after'],cwd=destination,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
assert diff.returncode==1 and not diff.stderr,diff.stderr
(destination/'selected.patch').write_bytes(diff.stdout)
verify=Path(tempfile.mkdtemp(prefix='story61-query-privacy-patch-verify-'))
try:
 if before.exists():shutil.copytree(before,verify,dirs_exist_ok=True)
 application=subprocess.run(['git','apply','-p2',str(destination/'selected.patch')],cwd=verify,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 assert application.returncode==0,application.stderr
 assert all((verify/row['path']).read_bytes()==(root/row['path']).read_bytes() for row in rows)
finally:shutil.rmtree(verify)
artifacts=['query-privacy-initial-build.log','query-privacy-tests-build.log','query-privacy-tests-build-2.log','query-privacy-tests-build-3.log',
 'query-privacy-tests-build-4.log','query-privacy-tests-build-5.log','query-privacy-tests-1.log','query-privacy-tests-1.xml',
 'query-privacy-tests-2.log','query-privacy-tests-2.xml','query-privacy-tests-3.log','query-privacy-tests-3.xml',
 'query-privacy-root-build-1.log','query-privacy-root-build-2.log','query-privacy-root-build-3.log','query-privacy-root-build-final.log',
 'query-privacy-root-tests-1.log','query-privacy-root-tests-1.xml','query-privacy-root-tests-2.log','query-privacy-root-tests-2.xml',
 'query-privacy-root-tests-final.log','query-privacy-root-tests-final.xml','query-privacy-host-security.log','query-privacy-host-security.xml']
for name in artifacts:shutil.copyfile(base/name,destination/name)
shutil.copyfile(base/'story61-query-privacy-build.py',destination/'build.py')
shutil.copyfile(base/'query-privacy-build/mapping.json',destination/'mapping.json')
native=[]
for name,expected in [('query-privacy-tests-3.xml',26),('query-privacy-root-tests-final.xml',5),('query-privacy-host-security.xml',46)]:
 assemblies=ET.parse(destination/name).getroot().findall('assembly')
 assert len(assemblies)==1
 a=assemblies[0];numbers={k:int(a.attrib[k]) for k in ['total','passed','failed','errors','skipped','not-run']}
 assert numbers['total']==numbers['passed']==expected and all(numbers[k]==0 for k in ['failed','errors','skipped','not-run'])
 native.append({'artifact':name,**numbers,'seconds':a.attrib['time']})
checks=[]
for repo in [root,root/'references/Hexalith.EventStore']:
 result=subprocess.run(['git','diff','--check'],cwd=repo,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 assert result.returncode==0
 checks.append({'repository':str(repo.relative_to(root)),'command':['git','diff','--check'],'exitCode':result.returncode,'output':result.stdout+result.stderr})
manifest_sha=lambda v:hashlib.sha256(json.dumps(v,sort_keys=True).encode()).hexdigest()
(destination/'source-manifest.json').write_text(json.dumps(current,sort_keys=True,indent=2)+'\n')
mapping=json.loads((destination/'mapping.json').read_text())['buildPreparation']['environment']
report={'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'workspace':str(root),
 'baselineSourceManifestSha256':manifest_sha(frozen),'sourceManifestSha256':manifest_sha(current),'inputCount':len(current),
 'changedFiles':rows,'onlySelectedInputsChanged':True,'selectedPatchSha256':hashlib.sha256(diff.stdout).hexdigest(),
 'patchReproducesAllSelectedBytes':True,'sourceMappingEnvironment':mapping,'nativeResults':native,
 'commands':[
  ['python3',str(base/'story61-query-privacy-build.py'),'dotnet','build','tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj','-c','Debug','-m:1'],
  ['dotnet','tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll','-class','*ReadDiagnosticsPrivacyTests','-class','*RequestLogPrivacyTests','-xml',str(base/'query-privacy-tests-3.xml')],
  ['python3',str(base/'story61-query-privacy-build.py'),'dotnet','build',str(root/'tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj'),'-c','Debug','-m:1'],
  ['python3',str(base/'story61-query-privacy-build.py'),'dotnet','build',str(root/'tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj'),'-c','Debug','-m:1','--no-restore','-p:BuildProjectReferences=false'],
  ['dotnet','tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll','-class','*SupportedProjectGatewayHttpTests','-method-','*GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures','-xml',str(base/'query-privacy-root-tests-final.xml')],
  ['dotnet','tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll','-class','*HttpHostSecurityTests','-class','*WorkloadGatewayControllerTests','-class','*DomainServiceWorkloadAssertionHandlerTests','-xml',str(base/'query-privacy-host-security.xml')]],
 'assemblySha256':{name:hashlib.sha256((root/path).read_bytes()).hexdigest() for name,path in {
  'SdkServerTests':sdk+'tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll',
  'ServiceDefaults':sdk+'src/Hexalith.EventStore.ServiceDefaults/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll',
  'ProjectsServerTests':'tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll'}.items()},
 'checks':checks,'sharedSourceChanged':False,'distributedRuntimeStarted':False,
 'localInProcessHttpFixtureHostsStartedAndDisposed':True,'privacyAcceptance':False,'timingToleranceAccepted':False,
 'remaining':['Augmented real-owner/foreign-only identity test with12 interleaved timing samples has not run at this source.',
  'Final native Dapr exporter/log/exemplar proof at this source is pending.',
  'Numeric timing tolerance and measurement acceptance remain unspecified.',
  'Current-source full regression/qualification and parent workflow review remain pending.'],
 'limits':['Pre-context Kestrel Connections/Transport sub-Warning chatter is omitted globally; warnings/errors and ordinary business diagnostics remain.',
  'Known protected request logs/traces are omitted; unsupported pre-context warning/error faults have not been exhaustively proven equivalent.',
  'Coarse aggregate metric operation counts remain and count-based traffic inference is accepted by the user.',
  'Original passing live-63 remains its own earlier-source functional evidence; no providers were reused.']}
(destination/'commands.json').write_text(json.dumps(report,indent=2)+'\n')
shutil.copyfile(Path(__file__),destination/'create-checkpoint.py')
assert m.source_manifest()==current
print(json.dumps({'packet':str(destination),'sourceManifestSha256':report['sourceManifestSha256'],'inputCount':len(current),'changedFiles':len(rows),'nativeResults':native,'patchVerified':True}))
