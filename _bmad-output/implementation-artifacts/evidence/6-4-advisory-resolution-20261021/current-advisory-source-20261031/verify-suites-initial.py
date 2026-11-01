"""Run existing advisory cases with an exclusively owned state store."""
from record import ROOT, EVIDENCE, WORK, BASE, run, write, digest
from pathlib import Path
import json, os, re, secrets, signal, socket, subprocess, time, urllib.request
import xml.etree.ElementTree as ET

environment=json.loads((EVIDENCE/'projects-environment.json').read_text())
RESULTS={}

def suite(name, assembly, selectors, explicit):
    argv=['dotnet',str(assembly)]
    for selector in selectors: argv.extend(['-class',selector])
    argv.extend(['-result-xml',str(EVIDENCE/(name+'.xml')),'-failSkips'])
    closure={str(f.relative_to(assembly.parent)):digest(f) for f in assembly.parent.iterdir() if f.is_file() and f.suffix in ('.dll','.json','.pdb')}
    write(name+'-executables-before.json',closure)
    code=run(name,argv,explicit=explicit,private=True,timeout=300)
    after={str(f.relative_to(assembly.parent)):digest(f) for f in assembly.parent.iterdir() if f.is_file() and f.suffix in ('.dll','.json','.pdb')}
    write(name+'-executables-after.json',after)
    path=EVIDENCE/(name+'.xml')
    summary={'exit_code':code,'executable_closure_unchanged':closure==after,'xml_present':path.is_file()}
    if path.is_file():
        tree=ET.parse(path); a=tree.find('assembly')
        summary.update({key:int(a.get(key,0)) for key in ('total','passed','failed','skipped','errors','not-run')})
        summary['nonpassing']=[{'name':t.get('name'),'result':t.get('result'),'message':t.findtext('failure/message','')} for t in tree.findall('.//test') if t.get('result')!='Pass']
    write(name+'-result.json',summary); RESULTS[name]=summary
    print(name+': '+json.dumps(summary),flush=True)
    return code==0 and summary.get('total',0)>0 and summary.get('passed')==summary.get('total') and summary.get('errors')==0 and closure==after

def checked(name,argv,**kwargs):
    code=run(name,argv,**kwargs)
    if code: raise RuntimeError(name+' failed: '+str(code))
    return (EVIDENCE/(name+'.stdout.log')).read_text().strip()

def port():
    with socket.socket() as sock: sock.bind(('127.0.0.1',0)); return sock.getsockname()[1]

def process_start(pid):
    try:return Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()[19]
    except FileNotFoundError:return None

contracts=ROOT/'tests/Hexalith.Projects.Contracts.Tests/bin/Debug/net10.0/Hexalith.Projects.Contracts.Tests.dll'
core=ROOT/'tests/Hexalith.Projects.Tests/bin/Debug/net10.0/Hexalith.Projects.Tests.dll'
server=ROOT/'tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll'
checks=[]
checks.append(suite('contracts-advisory',contracts,['*ProjectAdvisoryContractTests'],environment))
checks.append(suite('engine-confirmation',core,['*Resolution*Tests'],environment))
checks.append(suite('server-advisory',server,['*ProjectAdvisoryQueryTests','*DelegatedProjectAdvisoryConversationDirectoryTests','*SupportedProjectReadHttpTests','*ProjectsServerModuleTests','*ResolveProjectFromAttachmentsTests','*ResolveProjectFromConversationTests','*ConfirmAmbiguousProjectTests'],environment))
if not all(checks):raise SystemExit('Focused tests failed; runtime not started')
image=json.loads(checked('redis-image',['docker','image','inspect','redis:7.4-alpine']))[0]
image_id=image['Id']
before=checked('containers-before',['docker','ps','--all','--no-trunc','--format','{{.ID}}']).splitlines()
write('runtime-inputs.json',{'redis_image_id':image_id,'redis_repo_digests':image.get('RepoDigests',[]),'daprd_path':'/home/administrator/.dapr/bin/daprd','daprd_sha256':digest('/home/administrator/.dapr/bin/daprd')})
name='story64-advisory-'+secrets.token_hex(6)
container=None; dapr=None; streams=[]; cleanup=[]
try:
    container=checked('redis-start',['docker','run','--detach','--name',name,'--label','hexalith.story64-verification='+name,'--read-only','--tmpfs','/data:rw,nosuid,nodev,noexec,size=64m','-p','127.0.0.1:0:6379',image_id,'redis-server','--save','','--appendonly','no','--protected-mode','yes'])
    if not re.fullmatch('[0-9a-f]{64}',container):raise RuntimeError('No immutable owned container ID')
    inspection=json.loads(checked('redis-start-inspect',['docker','inspect',container]))[0]
    if inspection['Config']['Labels'].get('hexalith.story64-verification')!=name:raise RuntimeError('Owner label differs')
    bindings=inspection['NetworkSettings']['Ports']['6379/tcp']
    if len(bindings)!=1 or bindings[0]['HostIp']!='127.0.0.1':raise RuntimeError('Redis binding not exclusively loopback')
    redis_port=int(bindings[0]['HostPort'])
    for attempt in range(100):
        ready=subprocess.run(['redis-cli','-h','127.0.0.1','-p',str(redis_port),'PING'],capture_output=True,text=True)
        if ready.returncode==0 and ready.stdout.strip()=='PONG':break
        time.sleep(.1)
    else:raise RuntimeError('Owned Redis readiness deadline')
    http,grpc,internal,public=[port() for _ in range(4)]
    components=WORK/'components'; components.mkdir()
    component='apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.redis\n  version: v1\n  metadata:\n  - name: redisHost\n    value: "127.0.0.1:'+str(redis_port)+'"\n  - name: redisPassword\n    value: ""\nscopes:\n- '+name+'\n'
    (components/'statestore.yaml').write_text(component)
    (EVIDENCE/'statestore.yaml').write_text(component)
    argv=['/home/administrator/.dapr/bin/daprd','--app-id',name,'--resources-path',str(components),'--dapr-http-port',str(http),'--dapr-grpc-port',str(grpc),'--dapr-internal-grpc-port',str(internal),'--dapr-public-port',str(public),'--dapr-listen-addresses','127.0.0.1','--dapr-public-listen-address','127.0.0.1','--dapr-internal-grpc-listen-address','127.0.0.1','--enable-metrics=false','--scheduler-host-address','','--log-level','warn']
    streams=[(WORK/'private-dapr.stdout.log').open('xb'),(WORK/'private-dapr.stderr.log').open('xb')]
    dapr=subprocess.Popen(argv,cwd=WORK,env={**os.environ,**BASE},stdout=streams[0],stderr=streams[1],start_new_session=True)
    started=process_start(dapr.pid)
    write('dapr-start.json',{'argv':argv,'cwd':str(WORK),'pid':dapr.pid,'process_start':started,'private_logs':True})
    for attempt in range(200):
        if dapr.poll() is not None:raise RuntimeError('Owned Dapr exited before readiness')
        try:
            with urllib.request.urlopen('http://127.0.0.1:'+str(http)+'/v1.0/healthz/outbound',timeout=.5) as response:
                if response.status==204:break
        except OSError:time.sleep(.1)
    else:raise RuntimeError('Owned Dapr readiness deadline')
    write('runtime-readiness.json',{'redis_ping':'PONG','dapr_outbound_http':204,'redis_port':redis_port,'http_port':http,'grpc_port':grpc,'app_id':name})
    runtime_env={**environment,'STORY61_DAPR_HTTP_ENDPOINT':'http://127.0.0.1:'+str(http),'STORY61_DAPR_GRPC_ENDPOINT':'http://127.0.0.1:'+str(grpc)}
    gateway_ok=suite('gateway-owner-store',server,['*SupportedProjectGatewayHttpTests'],runtime_env)
    checks.append(gateway_ok)
    if gateway_ok:checks.append(suite('projects-server-full',server,[],runtime_env))
finally:
    if dapr is not None:
        if dapr.poll() is None:os.killpg(dapr.pid,signal.SIGTERM)
        forced=False
        try:dapr.wait(timeout=15)
        except subprocess.TimeoutExpired:forced=True;os.killpg(dapr.pid,signal.SIGKILL);dapr.wait(timeout=5)
        cleanup.append({'kind':'daprd','pid':dapr.pid,'start':started,'exit_code':dapr.returncode,'forced':forced,'exact_process_absent':process_start(dapr.pid)!=started})
    for stream in streams:stream.close()
    if container is not None and re.fullmatch('[0-9a-f]{64}',container):
        code=run('redis-remove',['docker','rm','--force',container])
        cleanup.append({'kind':'redis','container_id':container,'remove_exit_code':code})
    after=checked('containers-after',['docker','ps','--all','--no-trunc','--format','{{.ID}}']).splitlines()
    write('runtime-cleanup.json',{'owned_resources':cleanup,'owned_container_absent':container not in after,'all_preexisting_container_ids_preserved':set(before)<=set(after),'preexisting_container_ids':before,'current_container_ids':after})
    write('focused-summary.json',RESULTS)
if not all(checks):raise SystemExit('Gateway fixture failed')
if not (set(before)<=set(after) and container not in after and all(item.get('exact_process_absent',True) for item in cleanup)):raise SystemExit('Runtime preservation/cleanup failed')
