"""Artifact and enrollment checks affected by the Story 6.6 review fixes. Run under run-with-temp."""
import hashlib,json,os,pathlib,shutil,subprocess,sys,xml.etree.ElementTree as ET
root=pathlib.Path.cwd(); repo=root/'references/Hexalith.McpCli'; out=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-fixes'; tmp=pathlib.Path(os.environ['TMPDIR']);feed=tmp/'feed';feed.mkdir()
artifact=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/d9/candidate/Hexalith.Projects.Contracts.2.0.0-story66.2.nupkg';shutil.copy2(artifact,feed/artifact.name)
(tmp/'NuGet.Config').write_text(f'''<configuration><packageSources><clear/><add key="story66" value="{feed}"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="story66"><package pattern="Hexalith.Projects.Contracts"/></packageSource><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>''')
commands=[]
def run(name,args,cwd):
    env=os.environ.copy();env['MSBUILDTERMINALLOGGER']='off'
    with (out/(name+'.log')).open('w') as log: code=subprocess.run([str(a) for a in args],cwd=cwd,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
    commands.append(dict(lane=name,cwd=str(cwd),command=[str(a) for a in args],exitCode=code));(out/'artifact-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
    print(name,'exit',code,flush=True)
    if code:print((out/(name+'.log')).read_text()[-5500:],flush=True)
    return code
vectors=repo/'tools/conformance-vectors/v1/current/modules/Hexalith.Projects.Contracts';approval=json.loads((vectors/'approval.json').read_text())
for name,sha in approval['vectorFilesSha256'].items():assert hashlib.sha256((vectors/name).read_bytes()).hexdigest()==sha
plan=json.loads((repo/'tools/live-topology/plans-current/projects.list.json').read_text());assert plan['vectorSha256']==approval['vectorFilesSha256']['list.vector.json']
assert hashlib.sha256((repo/'tools/inventory-coverage/v1/projects-approved.json').read_bytes()).hexdigest()==approval['inventorySha256']
if run('projects-artifact',[sys.executable,repo/'tools/conformance-vectors/v1/verify_artifact.py','--artifact',feed/artifact.name,'--output',out/'projects-artifact','--inventory',repo/'tools/inventory-coverage/v1/projects-approved.json','--accepted-lint',repo/'tools/inventory-coverage/v1/projects-accepted-lint.json',*sorted(vectors.glob('*.vector.json'))],repo):sys.exit(1)
copy=tmp/'mcpcli';copy.mkdir()
for name in ['src','tests','tools','scripts','docs','module']:
    if (repo/name).exists():shutil.copytree(repo/name,copy/name,ignore=shutil.ignore_patterns('bin','obj','.git','__pycache__'))
for name in ['Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json','.editorconfig']:
    if (repo/name).exists():shutil.copy2(repo/name,copy/name)
(copy/'references').mkdir();(copy/'references/Hexalith.Builds').symlink_to(root/'references/Hexalith.Builds',target_is_directory=True)
# Source-based coverage fixtures locate repository documentation through the copied layout.
(copy/'_bmad-output').symlink_to(repo/'_bmad-output',target_is_directory=True)
project='tests/Hexalith.McpCli.Core.Tests/Hexalith.McpCli.Core.Tests.csproj'
if run('enrollment-build',['dotnet','build',project,'-c','Release','-m:1','-p:NuGetAudit=false','--disable-build-servers'],copy):sys.exit(1)
assembly=copy/'tests/Hexalith.McpCli.Core.Tests/bin/Release/net11.0/Hexalith.McpCli.Core.Tests.dll'
code=run('enrollment-focused',['dotnet',assembly,'-class','Hexalith.McpCli.Core.Tests.EnrolledModuleCoverageTests','-xml',out/'enrollment-focused.xml','-trx',out/'enrollment-focused.trx'],copy)
sys.exit(code)
