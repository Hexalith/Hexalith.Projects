"""Probe the actual package-built host with disposable loopback Redis/Dapr; keep secrets/logs private."""
import base64,datetime,hashlib,hmac,json,os,secrets,signal,socket,subprocess,time,urllib.request,urllib.error
from pathlib import Path
ROOT=Path('/home/administrator/projects/hexalith/projects')
OWNER=ROOT/'references/Hexalith.Platform'
E=Path(__file__).resolve().parent
W=Path('/tmp/story64-tenant-registry-alignment-20261030')
W.mkdir(exist_ok=True)
commands=[];children=[];result={'kind':'local-Debug-host-probe','candidateAcceptance':False,'observations':[]}
now=lambda:datetime.datetime.now(datetime.timezone.utc).isoformat()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def save():
 (E/'local-host.json').write_text(json.dumps(result,indent=2)+'\n')
 (E/'runtime-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
def port():
 with socket.socket() as s:s.bind(('127.0.0.1',0));return s.getsockname()[1]
def launch(name,argv,cwd,overrides):
 env=os.environ.copy();env.update(overrides)
 log=(W/(name+'.private.log')).open('wb')
 p=subprocess.Popen(argv,cwd=cwd,env=env,stdout=log,stderr=log,start_new_session=True)
 children.append((name,p,log));commands.append({'name':name,'argv':argv,'cwd':str(cwd),'environment':{k:('<ephemeral secret; not retained>' if 'TOKEN' in k or 'SigningKey' in k else v) for k,v in overrides.items()},'startedUtc':now(),'pid':p.pid,'privateLog':str(W/(name+'.private.log'))});save();return p
def request(url,payload=None,token=None,timeout=5):
 data=None if payload is None else json.dumps(payload).encode()
 headers={'Content-Type':'application/json'}
 if token:headers['Authorization']='Bearer '+token
 req=urllib.request.Request(url,data=data,headers=headers)
 try:
  with urllib.request.urlopen(req,timeout=timeout) as response:return response.status,response.read()
 except urllib.error.HTTPError as error:return error.code,error.read()
def wait_http(url,process,seconds):
 end=time.monotonic()+seconds
 while time.monotonic()<end:
  if process.poll() is not None:raise RuntimeError('Owned process exited before HTTP readiness')
  try:return request(url,timeout=0.5)
  except OSError:time.sleep(0.1)
 raise RuntimeError('Owned process HTTP startup deadline expired')
def observe(name,method,path,expected,payload=None,token=None):
 started=now();code,body=request(base+path,payload,token,timeout=12)
 item={'name':name,'method':method,'path':path,'expectedStatus':expected,'actualStatus':code,'startedUtc':started,'finishedUtc':now(),'responseBytes':len(body),'responseSha256':hashlib.sha256(body).hexdigest()}
 result['observations'].append(item);save();assert code==expected,(name,code,expected)
 return body
key=secrets.token_urlsafe(48)
def jwt(extra=None,bad=False):
 enc=lambda value:base64.urlsafe_b64encode(json.dumps(value,separators=(',',':')).encode()).rstrip(b'=')
 t=int(time.time());claims={'iss':'local-story64-issuer','aud':'hexalith-eventstore','sub':'local-story64-actor','iat':t,'nbf':t-1,'exp':t+300};claims.update(extra or {})
 raw=enc({'alg':'HS256','typ':'JWT'})+b'.'+enc(claims)
 signature=hmac.new((key+'x' if bad else key).encode(),raw,hashlib.sha256).digest()
 return (raw+b'.'+base64.urlsafe_b64encode(signature).rstrip(b'=')).decode()
try:
 redis_port,http_port,grpc_port,internal_port,public_port,app_port=[port() for _ in range(6)]
 base='http://127.0.0.1:'+str(app_port);app_id='story64-tenant-registry-'+str(os.getpid())
 components=W/'components';components.mkdir(exist_ok=False)
 for name,kind in [('eventstore-statestore','state.redis'),('eventstore-pubsub','pubsub.redis'),('eventstore-configstore','configuration.redis')]:
  (components/(name+'.yaml')).write_text(f'apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: {name}\nspec:\n  type: {kind}\n  version: v1\n  metadata:\n  - name: redisHost\n    value: "127.0.0.1:{redis_port}"\n  - name: redisPassword\n    value: ""\nscopes:\n- {app_id}\n')
 redis=W/'redis-server';dapr=Path('/home/administrator/.dapr/bin/daprd')
 result['runtime']={str(p):{'sha256':sha(p),'version':subprocess.check_output([str(p),'--version'],text=True).strip()} for p in [redis,dapr]}
 result['host']={'source':str(OWNER/'src/Hexalith.Platform.TenantRegistry.Host'),'assembly':str(OWNER/'src/Hexalith.Platform.TenantRegistry.Host/bin/Debug/net10.0/Hexalith.EventStore.dll'),'assemblySha256':sha(OWNER/'src/Hexalith.Platform.TenantRegistry.Host/bin/Debug/net10.0/Hexalith.EventStore.dll')}
 r=launch('redis',[str(redis),'--bind','127.0.0.1','--port',str(redis_port),'--save','','--appendonly','no','--dir',str(W),'--protected-mode','yes'],W,{})
 for _ in range(50):
  ping=subprocess.run(['redis-cli','-h','127.0.0.1','-p',str(redis_port),'PING'],capture_output=True,text=True)
  if ping.returncode==0 and ping.stdout.strip()=='PONG':break
  time.sleep(.1)
 else:raise RuntimeError('Isolated Redis did not become ready')
 api_token=secrets.token_urlsafe(32);app_token=secrets.token_urlsafe(32)
 d=launch('daprd',[str(dapr),'--app-id',app_id,'--resources-path',str(components),'--dapr-http-port',str(http_port),'--dapr-grpc-port',str(grpc_port),'--dapr-internal-grpc-port',str(internal_port),'--dapr-public-port',str(public_port),'--dapr-listen-addresses','127.0.0.1','--dapr-public-listen-address','127.0.0.1','--dapr-internal-grpc-listen-address','127.0.0.1','--enable-metrics=false','--scheduler-host-address','','--log-level','warn'],W,{'DAPR_API_TOKEN':api_token,'APP_API_TOKEN':app_token})
 status,_=wait_http('http://127.0.0.1:'+str(http_port)+'/v1.0/healthz/outbound',d,20);result['daprOutboundHealthStatus']=status
 env={'ASPNETCORE_ENVIRONMENT':'Development','ASPNETCORE_URLS':base,'DAPR_HTTP_ENDPOINT':'http://127.0.0.1:'+str(http_port),'DAPR_GRPC_ENDPOINT':'http://127.0.0.1:'+str(grpc_port),'DAPR_HTTP_PORT':str(http_port),'DAPR_GRPC_PORT':str(grpc_port),'DAPR_API_TOKEN':api_token,'APP_API_TOKEN':app_token,'Authentication__JwtBearer__Issuer':'local-story64-issuer','Authentication__JwtBearer__Audience':'hexalith-eventstore','Authentication__JwtBearer__SigningKey':key,'Authentication__JwtBearer__AllowedAlgorithms__0':'HS256','Authentication__JwtBearer__RequireHttpsMetadata':'false','EventStore__OpenApi__Enabled':'false','EventStore__CommandStatus__StateStoreName':'eventstore-statestore','EventStore__Projections__CheckpointStateStoreName':'eventstore-statestore','EventStore__Projections__ReadModelStateStoreName':'eventstore-statestore','EventStore__Publisher__PubSubName':'eventstore-pubsub','EventStore__ProjectionChanges__PubSubName':'eventstore-pubsub','EventStore__RateLimiting__ConfigurationStoreName':'eventstore-configstore','Logging__LogLevel__Default':'Warning'}
 host=launch('tenant-registry-host',['dotnet',result['host']['assembly']],OWNER/'src/Hexalith.Platform.TenantRegistry.Host',env)
 wait_http(base+'/alive',host,65)
 observe('liveness','GET','/alive',200)
 observe('readiness-without-actor-placement','GET','/ready',503)
 observe('health-without-actor-placement','GET','/health',503)
 body=observe('dynamic-subscription','GET','/dapr/subscribe',200);result['subscriptionCount']=len(json.loads(body))
 payload={'tenant':'local-tenant','domain':'tenants','aggregateId':'local-aggregate','queryType':'GetTenant','payload':{}}
 observe('anonymous-query-denied','POST','/api/v1/queries',401,payload)
 observe('malformed-bearer-denied','POST','/api/v1/queries',401,payload,'not-a-jwt')
 observe('wrong-signature-denied','POST','/api/v1/queries',401,payload,jwt(bad=True))
 observe('wrong-audience-denied','POST','/api/v1/queries',401,payload,jwt({'aud':'other'}))
 observe('authenticated-nonmember-query-denied','POST','/api/v1/queries',403,payload,jwt())
 observe('authenticated-command-only-query-denied','POST','/api/v1/queries',403,payload,jwt({'eventstore:tenant':'local-tenant','eventstore:permission':'command:submit'}))
 result['status']='passed-local-controls';result['limitations']='No Tenants DomainService, actor placement/scheduler or native writer initialization. Readiness stays unavailable; no successful authorized owner query or complete runtime/tuple/release acceptance is claimed.'
except Exception as error:
 result['status']='blocked';result['blockerType']=type(error).__name__;result['blocker']=str(error);save();raise
finally:
 result['cleanup']=[]
 for name,p,log in reversed(children):
  if p.poll() is None:
   os.killpg(p.pid,signal.SIGTERM)
   try:p.wait(timeout=12)
   except subprocess.TimeoutExpired:os.killpg(p.pid,signal.SIGKILL);p.wait(timeout=5)
  log.close();result['cleanup'].append({'name':name,'pid':p.pid,'exitCode':p.returncode,'stopped':p.poll() is not None})
 result['finishedUtc']=now();save()
 print(json.dumps({'status':result['status'],'observations':len(result['observations']),'cleanup':result['cleanup']}))
