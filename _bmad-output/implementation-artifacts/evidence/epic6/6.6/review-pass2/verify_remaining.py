"""Resume unchanged McpCli patch gates with package sources scoped to the sample fixture."""
from pathlib import Path
import hashlib,json,os,shutil,subprocess,sys
root=Path.cwd();repo=root/'references/Hexalith.McpCli';out=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2';tmp=Path(os.environ['TMPDIR']);feed=tmp/'feed';feed.mkdir();commands=[]
shutil.copy2(out/'candidate/Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg',feed/'Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg')
env=os.environ.copy();env.pop('RestoreSources',None);env['MSBUILDTERMINALLOGGER']='off';env['DOTNET_CLI_USE_MSBUILD_SERVER']='0';env['MSBUILDDISABLENODEREUSE']='1';env['UseSharedCompilation']='false'
def run(name,args,cwd=root,custom=None,required=True):
    actual=env.copy();actual.update(custom or {})
    with (out/(name+'.log')).open('w') as log:code=subprocess.run([str(a) for a in args],cwd=cwd,env=actual,stdout=log,stderr=subprocess.STDOUT).returncode
    commands.append(dict(lane=name,cwd=str(cwd),command=[str(a) for a in args],exitCode=code,packageSourceOverride='RestoreSources' in actual));(out/'remaining-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
    print(name,'exit',code,flush=True)
    if code:
        print((out/(name+'.log')).read_text()[-6000:],flush=True)
        if required:sys.exit(code)
patch=json.loads((out/'mcpcli-patch.json').read_text())
for row in patch['files']:assert hashlib.sha256((repo/row['path']).read_bytes()).hexdigest()==row['sha256'],row['path']
(out/'remaining-source-match.json').write_text(json.dumps(dict(result='pass',patchSha256=patch['sha256'],matchedPatchFiles=len(patch['files']),mcpcliHead=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo).decode().strip(),packageSources='RestoreSources is absent from ordinary suites; only sample commands inherit the unreleased candidate feed override.'),indent=2)+'\n')
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
run('topology-focused',['python3','-m','unittest','-v','test_ci.CiWorkflowTests','test_live_plans.LiveKindCoverageTests','test_current_cohort'],copy/'tools/live-topology',mcp_env)
run('topology-preflight',['python3','tools/live-topology/run-vectors.py','--output',out/'topology-preflight','--preflight-only'],copy,mcp_env)
(copy/'NuGet.Config').write_text(f'<configuration><packageSources><clear/><add key="story66" value="{feed}"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>')
run('sample-validation',['bash','tools/conformance-vectors/v1/run-sample-validation.sh','Release'],copy,sample_env)
run('sample-loopback',['bash','tools/conformance-vectors/v1/run-sample-loopback.sh'],copy,sample_env)
run('mcpcli-final-restore',['dotnet','restore','Hexalith.McpCli.slnx','-m:1'],copy,mcp_env)
run('mcpcli-final-build',['dotnet','build','Hexalith.McpCli.slnx','--no-restore','--configuration','Release','-warnaserror','-m:1','--disable-build-servers'],copy,mcp_env)
cli=copy/'src/Hexalith.McpCli/bin/Release/net11.0/Hexalith.McpCli.dll'
for name,args in [('modules',['modules','--strict']),('projects',['operations','projects','--strict'])]:run('strict-'+name,['dotnet',cli,*args],copy,mcp_env)
print('All second-pass patch lanes passed.',flush=True)
