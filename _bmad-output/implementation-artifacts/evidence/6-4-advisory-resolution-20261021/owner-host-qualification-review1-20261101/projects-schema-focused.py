"""Focused Projects query cases using command-owned Redis/Dapr; no full suites."""
from record import ROOT,EVIDENCE,WORK,BASE,run,write,digest
from pathlib import Path
import datetime,json,os,secrets,shutil,signal,socket,subprocess,time,urllib.request
import xml.etree.ElementTree as ET

SOURCE=Path(json.loads((EVIDENCE/'workspace.json').read_text())['source'])
projects=json.loads((EVIDENCE/'projects-environment.json').read_text())
conversations=json.loads((EVIDENCE/'conversations-environment.json').read_text())
RESULTS={}
def suite(name,assembly,selectors,explicit):
    name='routing-schema-'+name
    argv=['dotnet',str(assembly)]
    for selector in selectors:argv.extend(['-class',selector])
    argv.extend(['-parallel','none','-result-xml',str(EVIDENCE/(name+'.xml')),'-failSkips'])
    def closure():return {str(p.relative_to(assembly.parent)):digest(p) for p in assembly.parent.iterdir() if p.is_file() and p.suffix in ('.dll','.json','.pdb')}
    before=closure();write(name+'-executables-before.json',before)
    code=run(name,argv,cwd=assembly.parents[4],explicit=explicit,private=True,timeout=900)
    after=closure();write(name+'-executables-after.json',after)
    summary={'exit_code':code,'executable_closure_unchanged':before==after,'xml_present':(EVIDENCE/(name+'.xml')).is_file()}
    if summary['xml_present']:
        tree=ET.parse(EVIDENCE/(name+'.xml'));a=tree.find('assembly')
        summary.update({key:int(a.get(key,0)) for key in ['total','passed','failed','skipped','errors','not-run']})
        summary['nonpassing']=[{'name':t.get('name'),'result':t.get('result'),'message':t.findtext('failure/message','')} for t in tree.findall('.//test') if t.get('result')!='Pass']
    write(name+'-result.json',summary);RESULTS[name]=summary
    print(name+': '+json.dumps(summary),flush=True)
    return code==0 and summary.get('total',0)>0 and summary.get('passed')==summary.get('total') and summary.get('errors')==0 and before==after

def assembly(owner,name):return SOURCE/owner/'tests'/name/'bin/Debug/net10.0'/(name+'.dll')
contracts=assembly('.','Hexalith.Projects.Contracts.Tests');core=assembly('.','Hexalith.Projects.Tests');server=assembly('.','Hexalith.Projects.Server.Tests')
conversation=assembly('references/Hexalith.Conversations','Hexalith.Conversations.Server.Tests')
checks=[]

def port():
    with socket.socket() as s:s.bind(('127.0.0.1',0));return s.getsockname()[1]
def process_start(pid):
    try:return Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()[19]
    except FileNotFoundError:return None
children=[];cleanup=[];ports=[]
name='story64-owner-routing-'+secrets.token_hex(6)
def launch(label,argv):
    label='routing-schema-'+label
    handles=[(WORK/(label+'.stdout.log')).open('xb'),(WORK/(label+'.stderr.log')).open('xb')]
    env={**os.environ,**BASE};env.pop('CI',None)
    start=datetime.datetime.now(datetime.timezone.utc).isoformat()
    process=subprocess.Popen(argv,cwd=WORK,env=env,stdout=handles[0],stderr=handles[1],start_new_session=True)
    started=process_start(process.pid);children.append((label,process,started,handles))
    write(label+'-start.json',{'argv':argv,'cwd':str(WORK),'explicit_environment':BASE,'removed_environment':['CI'],'started_utc':start,'pid':process.pid,'kernel_start_time':started,'private_output':True,'executable_sha256':digest(argv[0])})
    return process
try:
    # Debian's installed multicall binary dispatches the server by its owned filename.
    shutil.copy2('/usr/bin/redis-check-rdb',WORK/'redis-server')
    write('routing-schema-runtime-provenance.json',{'redis_installed_source':'/usr/bin/redis-check-rdb','redis_source_sha256':digest('/usr/bin/redis-check-rdb'),'redis_owned_copy_sha256':digest(WORK/'redis-server'),'daprd':'/home/administrator/.dapr/bin/daprd','daprd_sha256':digest('/home/administrator/.dapr/bin/daprd'),'system_packages_changed':False})
    run('routing-schema-redis-version',[str(WORK/'redis-server'),'--version'],cwd=WORK)
    redis_port,http,grpc,internal,public=[port() for _ in range(5)];ports=[redis_port,http,grpc,internal,public]
    redis=launch('redis',[str(WORK/'redis-server'),'--bind','127.0.0.1','--port',str(redis_port),'--save','','--appendonly','no','--dir',str(WORK),'--protected-mode','yes'])
    for attempt in range(100):
        if redis.poll() is not None:raise RuntimeError('Owned Redis exited')
        ready=subprocess.run(['redis-cli','-h','127.0.0.1','-p',str(redis_port),'PING'],capture_output=True,text=True)
        if ready.returncode==0 and ready.stdout.strip()=='PONG':break
        time.sleep(.1)
    else:raise RuntimeError('Owned Redis readiness deadline')
    components=WORK/'routing-schema-components';components.mkdir()
    component='apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.redis\n  version: v1\n  metadata:\n  - name: redisHost\n    value: "127.0.0.1:'+str(redis_port)+'"\n  - name: redisPassword\n    value: ""\nscopes:\n- '+name+'\n'
    (components/'statestore.yaml').write_text(component);(EVIDENCE/'routing-schema-statestore.yaml').write_text(component)
    dapr=launch('dapr',['/home/administrator/.dapr/bin/daprd','--app-id',name,'--resources-path',str(components),'--dapr-http-port',str(http),'--dapr-grpc-port',str(grpc),'--dapr-internal-grpc-port',str(internal),'--dapr-public-port',str(public),'--dapr-listen-addresses','127.0.0.1','--dapr-public-listen-address','127.0.0.1','--dapr-internal-grpc-listen-address','127.0.0.1','--enable-metrics=false','--scheduler-host-address','','--log-level','warn'])
    for attempt in range(200):
        if dapr.poll() is not None:raise RuntimeError('Owned Dapr exited')
        try:
            with urllib.request.urlopen('http://127.0.0.1:'+str(http)+'/v1.0/healthz/outbound',timeout=.5) as response:
                if response.status==204:break
        except OSError:time.sleep(.1)
    else:raise RuntimeError('Owned Dapr readiness deadline')
    write('routing-schema-readiness.json',{'redis_ping':'PONG','dapr_outbound_status':204,'app_id':name,'redis_port':redis_port,'http_port':http,'grpc_port':grpc})
    runtime={**projects,'STORY61_DAPR_HTTP_ENDPOINT':'http://127.0.0.1:'+str(http),'STORY61_DAPR_GRPC_ENDPOINT':'http://127.0.0.1:'+str(grpc)}
    checks.append(suite('projects-query-focused',server,[
        'Hexalith.Projects.Server.Tests.Queries.GatewayFixtureRouteCatalogTests',
        'Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests',
        'Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadTests',
        'Hexalith.Projects.Server.Tests.Queries.SupportedProjectAuthorityHttpTests',
    ],runtime))
finally:
    for label,process,started,handles in reversed(children):
        forced=False
        if process.poll() is None:os.killpg(process.pid,signal.SIGTERM)
        try:process.wait(timeout=15)
        except subprocess.TimeoutExpired:forced=True;os.killpg(process.pid,signal.SIGKILL);process.wait(timeout=5)
        for h in handles:h.close()
        cleanup.append({'name':label,'pid':process.pid,'kernel_start_time':started,'exit_code':process.returncode,'forced':forced,'exact_process_absent':process_start(process.pid)!=started,'finished_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()})
    portchecks=[]
    for p in ports:
        with socket.socket() as s:s.settimeout(.2);portchecks.append({'port':p,'listener_absent':s.connect_ex(('127.0.0.1',p))!=0})
    write('routing-schema-cleanup.json',{'owned_processes':cleanup,'ports':portchecks,'all_owned_processes_absent':all(x['exact_process_absent'] for x in cleanup),'all_owned_listeners_absent':all(x['listener_absent'] for x in portchecks),'shared_resources_stopped':False})
    write('routing-schema-test-summary.json',RESULTS)
if not all(checks):raise SystemExit(1)
