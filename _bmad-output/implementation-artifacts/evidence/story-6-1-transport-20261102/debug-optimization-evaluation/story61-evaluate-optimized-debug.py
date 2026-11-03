import pathlib,json,subprocess,hashlib,os
root=pathlib.Path('/home/administrator/projects/hexalith/projects'); work=pathlib.Path('/tmp/story61-opt-debug-profile');work.mkdir(exist_ok=True)
projects=['src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj','src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj','references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj','references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj','references/Hexalith.EventStore/src/Hexalith.EventStore.Testing/Hexalith.EventStore.Testing.csproj','references/Hexalith.Folders/src/Hexalith.Folders.Server/Hexalith.Folders.Server.csproj','references/Hexalith.Builds/src/hosts/Hexalith.Builds.Module.EventStoreHost/Hexalith.Builds.Module.EventStoreHost.csproj']
rows=[]
for optimize in ['false','true']:
 for project in projects:
  command=['python3','/tmp/build-story61-helper.py','dotnet','msbuild',project,'-p:Configuration=Debug','-p:Optimize='+optimize,'-getProperty:Configuration,Optimize,DefineConstants,DebugType,DebugSymbols,UseHexalithProjectReferences,HexalithEventStoreFromSource,EnablePrivateCatalogInvocationProof,MSBuildVersion,NETCoreSdkVersion']
  done=subprocess.run(command,cwd=root,text=True,capture_output=True);(work/(pathlib.Path(project).stem+'-'+optimize+'-properties.log')).write_text(done.stdout+done.stderr)
  if done.returncode: raise RuntimeError('Property evaluation failed')
  props=json.loads(done.stdout)['Properties'];assert props['Configuration']=='Debug' and props['Optimize']==optimize and 'DEBUG' in props['DefineConstants'].split(';') and props['DebugType']=='portable' and props['DebugSymbols']=='true'
  rows.append({'command':command,'exitCode':done.returncode,'properties':props})
sourcepaths=['Directory.Build.props','src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj','references/Hexalith.EventStore/Directory.Build.props','references/Hexalith.Folders/Directory.Build.props','references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Workspace/SourceMappingMaterializer.cs']
sdk=pathlib.Path('/usr/share/dotnet/sdk/10.0.403'); sdkpaths=list(sdk.glob('Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.CSharp.targets'))+list(sdk.glob('Microsoft.Common.CurrentVersion.targets'))+list(sdk.glob('Roslyn/Microsoft.CSharp.Core.targets'))+list(sdk.glob('Microsoft.Common.CurrentVersion.targets'))
sources={str(p if p.is_absolute() else root/p):hashlib.sha256((p if p.is_absolute() else root/p).read_bytes()).hexdigest() for p in [pathlib.Path(x) for x in sourcepaths]+sdkpaths}
mapping=json.loads(pathlib.Path('/tmp/story61-prerequisite-dev-mapping-20261102.json').read_text())['buildPreparation']['environment'];assert len(mapping)==7
(work/'evaluation.json').write_text(json.dumps({'selection':'evaluation-only, no live optimized profile selected','mappingEnvironment':mapping,'helperSha256':hashlib.sha256(pathlib.Path('/tmp/build-story61-helper.py').read_bytes()).hexdigest(),'evaluations':rows,'sourceInputs':sources},indent=2)+'\n')
print('All14 property evaluations preserve Debug, DEBUG, portable symbols, source mapping; optimized profile not selected.')
