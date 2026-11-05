from pathlib import Path
import json,os,subprocess,sys,tempfile

root=Path('/tmp/story61-isolated-20261103-r5tqvlm3/projects')
packet=Path('/tmp/story61-isolated-20261103-r5tqvlm3/query-privacy-build')
packet.mkdir(mode=0o700,exist_ok=True)
environment=os.environ.copy()
environment.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',DOTNET_PROCESSOR_COUNT='2',
 UseHexalithProjectReferences='true',UseNuGetDeps='false',EnablePrivateCatalogInvocationProof='true',
 HexalithBuildsRoot=str(root/'references/Hexalith.Builds'))
mapping=packet/'mapping.json'
if not mapping.exists():
 workspace=Path(tempfile.mkdtemp(prefix='story61-query-privacy-mapping-'));workspace.chmod(0o700)
 command=['dotnet','run','--project',str(root/'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj'),'--','prepare-build','--manifest','module/hexalith.module-manifest.v2.json','--mode','source','--workspace',str(workspace/'source-mapping'),'--output','json']
 result=subprocess.run(command,cwd=root,env=environment,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 (packet/'mapping.stderr').write_text(result.stderr)
 mapping.write_text(result.stdout)
 assert result.returncode==0, 'Mapping failed; inspect retained result.'
selected=json.loads(mapping.read_text())['buildPreparation']['environment']
assert set(selected)=={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'} and selected['Configuration']=='Debug'
environment.update(selected)
for name in ('EventStore','Tenants','Commons','Folders','Conversations','FrontComposer','Memories','Parties'):
 environment['Hexalith'+name+'Root']=str(root/'references'/('Hexalith.'+name))
environment['HexalithTenantsBasePath']=str(root/'references/Hexalith.Tenants/src')
command=sys.argv[1:]
print('Executing mapped source Debug command:',json.dumps(command),flush=True)
result=subprocess.run(command,cwd=root/'references/Hexalith.EventStore',env=environment)
raise SystemExit(result.returncode)
