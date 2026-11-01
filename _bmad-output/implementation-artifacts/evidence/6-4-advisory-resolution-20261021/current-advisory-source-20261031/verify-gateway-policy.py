"""Run existing advisory cases with an exclusively owned state store."""
from record import ROOT, EVIDENCE, WORK, BASE, run, write, digest
from pathlib import Path
import json, os, re, secrets, signal, socket, subprocess, time, urllib.request
import xml.etree.ElementTree as ET

environment=json.loads((EVIDENCE/'projects-environment.json').read_text())
RESULTS={}

def suite(name, assembly, selectors, explicit):
    name='policy-'+name
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


server=ROOT/'tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll'
children=[]; checks=[]; cleanup=[]
name='story64-local-'+secrets.token_hex(6)
def launch(label,argv):
    handles=[(WORK/('policy-'+label+'.stdout.log')).open('xb'),(WORK/('policy-'+label+'.stderr.log')).open('xb')]
    process=subprocess.Popen(argv,cwd=WORK,env={**os.environ,**BASE},stdout=handles[0],stderr=handles[1],start_new_session=True)
    started=process_start(process.pid)
    children.append((label,process,started,handles))
    write('policy-'+label+'-start.json',{'argv':argv,'cwd':str(WORK),'pid':process.pid,'kernel_start_time':started,'private_output':True})
    return process
try:
    redis_port,http,grpc,internal,public=[port() for _ in range(5)]
    redis=launch('redis',[str(WORK/'redis-server'),'--bind','127.0.0.1','--port',str(redis_port),'--save','','--appendonly','no','--dir',str(WORK),'--protected-mode','yes'])
    for attempt in range(100):
        if redis.poll() is not None:raise RuntimeError('Owned Redis exited')
        ready=subprocess.run(['redis-cli','-h','127.0.0.1','-p',str(redis_port),'PING'],capture_output=True,text=True)
        if ready.returncode==0 and ready.stdout.strip()=='PONG':break
        time.sleep(.1)
    else:raise RuntimeError('Owned Redis readiness deadline')
    components=WORK/'policy-components';components.mkdir()
    component='apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.redis\n  version: v1\n  metadata:\n  - name: redisHost\n    value: "127.0.0.1:'+str(redis_port)+'"\n  - name: redisPassword\n    value: ""\nscopes:\n- '+name+'\n'
    (components/'statestore.yaml').write_text(component);(EVIDENCE/'policy-statestore.yaml').write_text(component)
    dapr=launch('dapr',['/home/administrator/.dapr/bin/daprd','--app-id',name,'--resources-path',str(components),'--dapr-http-port',str(http),'--dapr-grpc-port',str(grpc),'--dapr-internal-grpc-port',str(internal),'--dapr-public-port',str(public),'--dapr-listen-addresses','127.0.0.1','--dapr-public-listen-address','127.0.0.1','--dapr-internal-grpc-listen-address','127.0.0.1','--enable-metrics=false','--scheduler-host-address','','--log-level','warn'])
    for attempt in range(200):
        if dapr.poll() is not None:raise RuntimeError('Owned Dapr exited')
        try:
            with urllib.request.urlopen('http://127.0.0.1:'+str(http)+'/v1.0/healthz/outbound',timeout=.5) as response:
                if response.status==204:break
        except OSError:time.sleep(.1)
    else:raise RuntimeError('Owned Dapr readiness deadline')
    write('policy-readiness.json',{'redis_ping':'PONG','dapr_outbound_status':204,'app_id':name,'redis_port':redis_port,'http_port':http,'grpc_port':grpc})
    runtime_env={**environment,'STORY61_DAPR_HTTP_ENDPOINT':'http://127.0.0.1:'+str(http),'STORY61_DAPR_GRPC_ENDPOINT':'http://127.0.0.1:'+str(grpc)}
    gateway_ok=suite('gateway-owner-store',server,['*SupportedProjectGatewayHttpTests'],runtime_env);checks.append(gateway_ok)

finally:
    for label,process,started,handles in reversed(children):
        forced=False
        if process.poll() is None:os.killpg(process.pid,signal.SIGTERM)
        try:process.wait(timeout=15)
        except subprocess.TimeoutExpired:forced=True;os.killpg(process.pid,signal.SIGKILL);process.wait(timeout=5)
        for handle in handles:handle.close()
        cleanup.append({'name':label,'pid':process.pid,'kernel_start_time':started,'exit_code':process.returncode,'forced':forced,'exact_process_absent':process_start(process.pid)!=started})
    write('policy-cleanup.json',cleanup)
    write('policy-test-summary.json',RESULTS)
if not all(checks):raise SystemExit(1)
