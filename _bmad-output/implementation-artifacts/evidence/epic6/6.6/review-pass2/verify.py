"""Story 6.6 second-pass patch verification. Invoke under run-with-temp."""
from pathlib import Path
import hashlib,json,os,shutil,socket,subprocess,sys,time,urllib.request
active=Path.cwd();out=active/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2';tmp=Path(os.environ['TMPDIR']);commands=[]
env=os.environ.copy();env['MSBUILDTERMINALLOGGER']='off';env['DOTNET_CLI_USE_MSBUILD_SERVER']='0';env['MSBUILDDISABLENODEREUSE']='1';env['UseSharedCompilation']='false'
def run(name,args,cwd=None,custom=None,required=True):
    cwd=cwd or root
    actual=env.copy();actual.update(custom or {})
    with (out/(name+'.log')).open('w') as log:
        code=subprocess.run([str(a) for a in args],cwd=cwd,env=actual,stdout=log,stderr=subprocess.STDOUT).returncode
    commands.append(dict(lane=name,cwd=str(cwd),command=[str(a) for a in args],exitCode=code));(out/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')
    print(name,'exit',code,flush=True)
    if code:
        print((out/(name+'.log')).read_text()[-6500:],flush=True)
        if required:sys.exit(code)
    return code
# Snapshot current source bytes and local Git metadata. Only private clone indices change.
def git_at(path,*args):return subprocess.check_output(['git','-C',str(path),*args])
def snapshot(source,destination):
    subprocess.run(['git','clone','--local','--shared','--no-checkout','--no-recurse-submodules',str(source),str(destination)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    subprocess.run(['git','-C',str(destination),'read-tree','HEAD'],check=True)
    entries=git_at(source,'ls-files','--stage','-z').split(b'\0')
    for entry in entries:
        if not entry:continue
        meta,relative=entry.split(b'\t',1);name=relative.decode()
        if meta.startswith(b'160000') or name.startswith(('_bmad-output/implementation-artifacts/evidence/','_bmad-output/implementation-artifacts/qualification-evidence/','evidence/')):continue
        original=source/name;target=destination/name
        if not original.exists():continue
        target.parent.mkdir(parents=True,exist_ok=True)
        if original.is_symlink():target.symlink_to(os.readlink(original))
        else:shutil.copy2(original,target)
    for relative in git_at(source,'ls-files','--others','--exclude-standard','-z').split(b'\0'):
        if not relative:continue
        name=relative.decode()
        if name.startswith(('_bmad-output/implementation-artifacts/evidence/','evidence/')):continue
        original=source/name
        if original.is_file():
            target=destination/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(original,target)
    return git_at(destination,'rev-parse','HEAD').decode().strip()
root=tmp/'projects';revisions={'projects':snapshot(active,root)}
for row in git_at(active,'config','-f','.gitmodules','--get-regexp','path').decode().splitlines():
    relative=row.split(None,1)[1];target=root/relative;target.parent.mkdir(parents=True,exist_ok=True)
    revisions[relative]=snapshot(active/relative,target)
    subprocess.run(['git','-C',str(root),'update-index','--cacheinfo','160000,'+revisions[relative]+','+relative],check=True)
repo=root/'references/Hexalith.McpCli'
# Fixture source tests read retained historical packets without writing to them.
for relative in ['evidence','_bmad-output/implementation-artifacts/evidence','_bmad-output/implementation-artifacts/qualification-evidence']:
    for source,destination in [(active,root),(active/'references/Hexalith.McpCli',repo)]:
        if (source/relative).is_dir() and not (destination/relative).exists():
            (destination/relative).parent.mkdir(parents=True,exist_ok=True);(destination/relative).symlink_to(source/relative,target_is_directory=True)
source_files={}
for base in [root,*(root/relative for relative in revisions if relative!='projects')]:
    for folder in ['src','tests','tools','module']:
        if (base/folder).exists():
            for path in (base/folder).rglob('*'):
                if path.is_file() and not any(part in ['bin','obj','__pycache__'] for part in path.relative_to(base).parts):source_files[str(path.relative_to(root))]=hashlib.sha256(path.read_bytes()).hexdigest()
(out/'snapshot-source-manifest.json').write_text(json.dumps(dict(revisions=revisions,files=source_files,activeIndexChanged=False,notes='Private clone indices align snapshot source HEADs for prepare-build; no active index or Gitlink was changed.'),indent=2)+'\n')
print('Isolated source snapshot captured.',flush=True)
feed=tmp/'feed';feed.mkdir()
run('candidate-pack',['dotnet','pack','src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj','--configuration','Release','-m:1','--disable-build-servers','-p:HexalithProjectsPackageVersion=2.0.0-story66.3','-p:UseHexalithProjectReferences=false','--artifacts-path',tmp/'package-build','--output',feed])
artifact=feed/'Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg';(out/'candidate').mkdir(exist_ok=True);shutil.copy2(artifact,out/'candidate'/artifact.name)
(out/'candidate.json').write_text(json.dumps(dict(path=str((out/'candidate'/artifact.name).relative_to(active)),version='2.0.0-story66.3',sha256=hashlib.sha256(artifact.read_bytes()).hexdigest()),indent=2)+'\n')
prepare=['dotnet',active/'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net11.0/Hexalith.Builds.Module.Cli.dll','prepare-build','--manifest','module/hexalith.module-manifest.v2.json','--mode','source','--workspace',tmp/'prepared','--output','json']
run('source-preparation',prepare)
preparation=json.loads((out/'source-preparation.log').read_text());(out/'source-preparation.json').write_text(json.dumps(preparation,indent=2)+'\n');source_env=preparation['buildPreparation']['environment']
for project in ['Hexalith.Projects.Contracts.Tests','Hexalith.Projects.Server.Tests']:
    run(project+'-build',['dotnet','build',f'tests/{project}/{project}.csproj','--configuration','Debug','-m:1','--disable-build-servers','--artifacts-path',tmp/'build'],custom=source_env)
for entry in root.iterdir():
    target=tmp/entry.name
    if not target.exists():target.symlink_to(entry,target_is_directory=entry.is_dir())
run('Contracts',['dotnet',tmp/'build/bin/Hexalith.Projects.Contracts.Tests/debug/Hexalith.Projects.Contracts.Tests.dll','-xml',out/'Contracts.xml','-trx',out/'Contracts.trx'],custom=source_env)
resources=tmp/'dapr-resources';resources.mkdir();(resources/'statestore.yaml').write_text('apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.in-memory\n  version: v1\n  metadata: []\n')
ports=[]
for _ in range(4):
    with socket.socket() as s:s.bind(('127.0.0.1',0));ports.append(s.getsockname()[1])
args=[str(Path.home()/'.dapr/bin/daprd'),'--app-id','story66-review2-'+str(os.getpid()),'--resources-path',str(resources),'--dapr-http-port',str(ports[0]),'--dapr-grpc-port',str(ports[1]),'--metrics-port',str(ports[2]),'--dapr-internal-grpc-port',str(ports[3]),'--log-level','warn']
with (out/'daprd.log').open('w') as log:
    child=subprocess.Popen(args,stdout=log,stderr=subprocess.STDOUT)
    try:
        endpoint='http://127.0.0.1:'+str(ports[0])
        for _ in range(100):
            try:urllib.request.urlopen(endpoint+'/v1.0/healthz/outbound',timeout=1).close();break
            except Exception:time.sleep(.2)
        else:raise RuntimeError('Dapr failed readiness')
        source_env.update(STORY61_DAPR_HTTP_ENDPOINT=endpoint,STORY61_DAPR_GRPC_ENDPOINT='http://127.0.0.1:'+str(ports[1]))
        run('Server',['dotnet',tmp/'build/bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll','-xml',out/'Server.xml','-trx',out/'Server.trx'],custom=source_env)
    finally:
        child.terminate()
        try:child.wait(timeout=15)
        except subprocess.TimeoutExpired:child.kill();child.wait()
        print('Owned daprd reaped.',flush=True)
copy=tmp/'mcpcli';subprocess.run(['git','clone','--local','--shared','--no-checkout','--no-recurse-submodules',str(repo),str(copy)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
for name in ['src','tests','tools','scripts','docs','module','.github']:
    if (repo/name).exists():shutil.copytree(repo/name,copy/name,ignore=shutil.ignore_patterns('bin','obj','.git','__pycache__'))
for p in repo.iterdir():
    if p.is_file() and p.name not in ['.git','NuGet.Config','nuget.config']:shutil.copy2(p,copy/p.name)
(copy/'references').mkdir();(copy/'references/Hexalith.Builds').symlink_to(root/'references/Hexalith.Builds',target_is_directory=True)
(copy/'_bmad-output').symlink_to(repo/'_bmad-output',target_is_directory=True)
(copy/'evidence').symlink_to(repo/'evidence',target_is_directory=True)
config=f'''<configuration><packageSources><clear/><add key="story66" value="{feed}"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="story66"><package pattern="Hexalith.Projects.Contracts"/></packageSource><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>'''
(copy/'NuGet.Config').write_text(config)
mcp_env={'NUGET_PACKAGES':str(tmp/'packages')}
sample_env={**mcp_env,'RestoreSources':str(feed)+';https://api.nuget.org/v3/index.json'}
run('mcpcli-restore',['dotnet','restore','Hexalith.McpCli.slnx','-m:1'],copy,mcp_env)
run('mcpcli-build',['dotnet','build','Hexalith.McpCli.slnx','--no-restore','--configuration','Release','-warnaserror','-m:1','--disable-build-servers'],copy,mcp_env)
for name in ['Abstractions','Core','Cli','Mcp']:
    project=f'Hexalith.McpCli.{name}.Tests'
    run('McpCli-'+name,['dotnet',copy/f'tests/{project}/bin/Release/net11.0/{project}.dll','-xml',out/f'McpCli-{name}.xml','-trx',out/f'McpCli-{name}.trx'],copy,mcp_env)
run('McpCli-Manifest',['dotnet','test','--project','tests/Hexalith.McpCli.Manifest.Tests','--no-build','--configuration','Release'],copy,mcp_env)
run('enrolled',['python3','tools/conformance-vectors/v1/check_enrolled.py','--local','--output',out/'enrolled'],copy,mcp_env)
run('topology-python',['python3','-m','unittest','discover','-v','-s','tools/live-topology','-p','test_*.py'],copy,mcp_env)
run('topology-preflight',['python3','tools/live-topology/run-vectors.py','--output',out/'topology-preflight','--preflight-only'],copy,mcp_env)
(copy/'NuGet.Config').write_text(f'<configuration><packageSources><clear/><add key="story66" value="{feed}"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>')
run('sample-validation',['bash','tools/conformance-vectors/v1/run-sample-validation.sh','Release'],copy,sample_env)
run('sample-loopback',['bash','tools/conformance-vectors/v1/run-sample-loopback.sh'],copy,sample_env)
run('mcpcli-final-restore',['dotnet','restore','Hexalith.McpCli.slnx','-m:1'],copy,mcp_env)
run('mcpcli-final-build',['dotnet','build','Hexalith.McpCli.slnx','--no-restore','--configuration','Release','-warnaserror','-m:1','--disable-build-servers'],copy,mcp_env)
cli=copy/'src/Hexalith.McpCli/bin/Release/net11.0/Hexalith.McpCli.dll'
for name,args in [('modules',['modules','--strict']),('projects',['operations','projects','--strict'])]:run('strict-'+name,['dotnet',cli,*args],copy,mcp_env)
print('All second-pass patch lanes passed.',flush=True)
