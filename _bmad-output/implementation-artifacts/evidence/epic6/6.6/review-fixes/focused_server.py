"""Focused Story 6.6 review verification; run under run-with-temp. Hold assembly for parent verification."""
import json, os, pathlib, socket, subprocess, sys, time, urllib.request
root=pathlib.Path.cwd(); out=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-fixes'; tmp=pathlib.Path(os.environ['TMPDIR'])
commands=[]
def run(name,args,env=None):
    with (out/(name+'.log')).open('w') as log:
        code=subprocess.run(args,cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
    commands.append(dict(lane=name,cwd=str(root),command=args,exitCode=code)); (out/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')
    print(name,'exit',code,flush=True)
    if code: print((out/(name+'.log')).read_text()[-5000:],flush=True)
    return code
prepare=['dotnet',str(root/'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net11.0/Hexalith.Builds.Module.Cli.dll'),'prepare-build','--manifest','module/hexalith.module-manifest.v2.json','--mode','source','--workspace',str(tmp/'prepared'),'--output','json']
if run('source-preparation',prepare): sys.exit(1)
preparation=json.loads((out/'source-preparation.log').read_text()); (out/'source-preparation.json').write_text(json.dumps(preparation,indent=2)+'\n')
env=os.environ.copy();env.update(preparation['buildPreparation']['environment']);env['MSBUILDTERMINALLOGGER']='off'
build=['dotnet','build','tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj','--configuration','Debug','-m:1','-p:NuGetAudit=false','--disable-build-servers','--artifacts-path',str(tmp/'build')]
if run('server-build',build,env): sys.exit(1)
for entry in root.iterdir():
    target=tmp/entry.name
    if not target.exists():target.symlink_to(entry, target_is_directory=entry.is_dir())
resources=tmp/'dapr-resources';resources.mkdir(); (resources/'statestore.yaml').write_text('apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.in-memory\n  version: v1\n  metadata: []\n')
ports=[]
for _ in range(4):
    with socket.socket() as s:s.bind(('127.0.0.1',0));ports.append(s.getsockname()[1])
args=[str(pathlib.Path.home()/'.dapr/bin/daprd'),'--app-id','story66-review-'+str(os.getpid()),'--resources-path',str(resources),'--dapr-http-port',str(ports[0]),'--dapr-grpc-port',str(ports[1]),'--metrics-port',str(ports[2]),'--dapr-internal-grpc-port',str(ports[3]),'--log-level','warn']
with (out/'daprd.log').open('w') as log:
    child=subprocess.Popen(args,stdout=log,stderr=subprocess.STDOUT)
    try:
        endpoint='http://127.0.0.1:'+str(ports[0])
        for attempt in range(100):
            try:
                urllib.request.urlopen(endpoint+'/v1.0/healthz/outbound',timeout=1).close();break
            except Exception:time.sleep(.2)
        else:raise RuntimeError('Dapr failed readiness')
        env['STORY61_DAPR_HTTP_ENDPOINT']=endpoint;env['STORY61_DAPR_GRPC_ENDPOINT']='http://127.0.0.1:'+str(ports[1])
        assembly=tmp/'build/bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll'
        details=dict(tmpdir=str(tmp),assembly=str(assembly),environment={k:env[k] for k in ('STORY61_DAPR_HTTP_ENDPOINT','STORY61_DAPR_GRPC_ENDPOINT')},daprdCommand=args)
        (out/'server-handoff.json').write_text(json.dumps(details,indent=2)+'\n')
        code=run('gateway-focused',['dotnet',str(assembly),'-class','Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests','-xml',str(out/'gateway-focused.xml'),'-trx',str(out/'gateway-focused.trx')],env)
        print('HELD_FOR_PARENT',json.dumps(details),flush=True)
        # The parent consumes this prepared assembly before sending a newline for cleanup.
        sys.stdin.readline()
    finally:
        child.terminate()
        try:child.wait(timeout=15)
        except subprocess.TimeoutExpired:child.kill();child.wait()
        print('Dapr reaped; disposable workspace can be removed.',flush=True)
sys.exit(code)
