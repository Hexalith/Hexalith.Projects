"""Run the existing bound-signing gateway fixture against an owned Dapr/Redis store."""
from run import *
import socket
import signal
import urllib.request

def port():
    with socket.socket() as listener:
        listener.bind(('127.0.0.1',0))
        return listener.getsockname()[1]

workspace=WORK/'projects'
qualified=json.loads((EVIDENCE/'qualified-builds-input.json').read_text())['identity']['embedded_catalog']
image=qualified['redisImage']+'@'+qualified['redisImageDigest']
name='story64-current-hostnet-'+str(os.getpid())
ports={key:port() for key in ['redis','http','grpc','internal','public']}
assert len(set(ports.values()))==len(ports)
components=WORK/'runtime-components-host-network';components.mkdir(exist_ok=False)
component=components/'statestore.yaml'
component.write_text('apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.redis\n  version: v1\n  metadata:\n  - name: redisHost\n    value: "127.0.0.1:'+str(ports['redis'])+'"\n  - name: redisPassword\n    value: ""\nscopes:\n- '+name+'\n')
(EVIDENCE/'hostnet-runtime-statestore.yaml').write_bytes(component.read_bytes())
container=None;sidecar=None
report={'scope':'Existing source-only bound-signing HTTP fixture; loopback service discovery, real Dapr/Redis persisted state; not Platform/runtime release acceptance','image':image,'ports':ports,'app_id':name,'component_sha256':digest(component),'started_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
run('hostnet-existing-containers',['docker','ps','--all','--no-trunc','--format','{{.ID}}'],workspace)
run('hostnet-dapr-version',['/home/administrator/.dapr/bin/daprd','--version'],workspace)
try:
    container=run('hostnet-runtime-redis-create',['docker','run','--pull=never','--detach','--rm','--name',name,'--network','host',image,'redis-server','--bind','127.0.0.1','--port',str(ports['redis']),'--protected-mode','yes','--save','','--appendonly','no'],workspace).strip()
    run('hostnet-runtime-redis-identity',['docker','inspect','--format','{{.Id}} {{.Image}} {{json .NetworkSettings.Ports}}',container],workspace)
    argv=['/home/administrator/.dapr/bin/daprd','--app-id',name,'--resources-path',str(components),'--dapr-http-port',str(ports['http']),'--dapr-grpc-port',str(ports['grpc']),'--dapr-internal-grpc-port',str(ports['internal']),'--dapr-public-port',str(ports['public']),'--dapr-listen-addresses','127.0.0.1','--dapr-public-listen-address','127.0.0.1','--enable-metrics=false','--scheduler-host-address=','--log-level','warn']
    out=(WORK/'private-hostnet-sidecar.stdout').open('wb');err=(WORK/'private-hostnet-sidecar.stderr').open('wb')
    sidecar=subprocess.Popen(argv,cwd=workspace,stdout=out,stderr=err,env={**os.environ,'GIT_OPTIONAL_LOCKS':'0'},start_new_session=True)
    report['sidecar']={'argv':argv,'cwd':str(workspace),'pid':sidecar.pid,'binary_sha256':digest(Path(argv[0]))}
    deadline=time.monotonic()+45
    while True:
        if sidecar.poll() is not None: raise RuntimeError('Dapr exited before readiness')
        try:
            with urllib.request.urlopen('http://127.0.0.1:'+str(ports['http'])+'/v1.0/healthz/outbound',timeout=1) as response:
                if response.status==204: break
        except Exception:
            if time.monotonic()>deadline: raise RuntimeError('Dapr readiness deadline exceeded')
            time.sleep(.2)
    report['outbound_ready']=True
    env={'STORY61_DAPR_HTTP_ENDPOINT':'http://127.0.0.1:'+str(ports['http']),'STORY61_DAPR_GRPC_ENDPOINT':'http://127.0.0.1:'+str(ports['grpc'])}
    command=['dotnet','tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll','-class','*ProjectAdvisoryQueryTests','-class','*DelegatedProjectAdvisoryConversationDirectoryTests','-class','*DelegatedProjectDirectoryRegistrationTests','-class','*ProjectQueryEnvelopePrincipalBindingTests','-class','*ProjectQueryTenantFilterTests','-class','*SupportedProjectReadHttpTests','-class','*SupportedProjectGatewayHttpTests','-result-xml',str(EVIDENCE/'hostnet-gateway-store-tests.xml'),'-failSkips']
    run('hostnet-gateway-store-tests',command,workspace,env,check=False)
finally:
    if sidecar is not None:
        sidecar.terminate()
        try:sidecar.wait(timeout=15)
        except subprocess.TimeoutExpired:sidecar.kill();sidecar.wait(timeout=5)
        report['sidecar_exit']=sidecar.returncode
        out.close();err.close()
    if container:
        run('hostnet-runtime-redis-stop',['docker','stop',container],workspace,check=False)
        output=run('hostnet-runtime-cleanup-check',['docker','ps','--all','--filter','id='+container,'--format','{{.ID}}'],workspace)
        report['container_absent']=not output.strip()
    report['finished_utc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
    (EVIDENCE/'hostnet-runtime-lane.json').write_text(json.dumps(report,indent=2)+'\n')
