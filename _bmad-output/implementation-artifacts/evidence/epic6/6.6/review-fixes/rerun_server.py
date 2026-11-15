"""Rebuild just the affected test project graph and rerun the focused Gateway class in the held job."""
import json,os,pathlib,subprocess,sys
root=pathlib.Path.cwd();out=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-fixes';env=os.environ.copy();env.update(json.loads((out/'source-preparation.json').read_text())['buildPreparation']['environment']);details=json.loads((out/'server-handoff.json').read_text());env.update(details['environment']);env['MSBUILDTERMINALLOGGER']='off'
commands=json.loads((out/'commands.json').read_text());suffix=sys.argv[1]
def run(name,args):
 with (out/(name+'.log')).open('w') as log: code=subprocess.run(args,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
 commands.append(dict(lane=name,cwd=str(root),command=args,exitCode=code));(out/'commands.json').write_text(json.dumps(commands,indent=2)+'\n');print(name,code,flush=True)
 if code:print((out/(name+'.log')).read_text()[-3500:],flush=True)
 return code
if run('server-rebuild-'+suffix,['dotnet','build','tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj','--configuration','Debug','-m:1','-p:NuGetAudit=false','--disable-build-servers','--artifacts-path',details['tmpdir']+'/build']):sys.exit(1)
lane='gateway-focused-'+suffix
sys.exit(run(lane,['dotnet',details['assembly'],'-class','Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests','-xml',str(out/(lane+'.xml')),'-trx',str(out/(lane+'.trx'))]))
