from pathlib import Path
import json,os,stat,subprocess,time,hashlib,sys,datetime
co=json.loads(Path(sys.argv[1]).read_text());pid=co['launcherPid'];e=Path(co['evidence']);out=Path(co['privateDiagnosticDirectory']);assert stat.S_IMODE(out.stat().st_mode)==0o700 and out.stat().st_uid==os.geteuid()
script="""local rows={} local h='0123456789ABCDEF' for _,k in ipairs(KEYS) do local ex=redis.call('EXISTS',k)==1 local d=ex and redis.call('HGET',k,'data') or false local v=ex and redis.call('HGET',k,'version') or false local x=false if d then local b={} for i=1,#d do local n=string.byte(d,i) b[i]=string.sub(h,math.floor(n/16)+1,math.floor(n/16)+1)..string.sub(h,n%16+1,n%16+1) end x=table.concat(b) end rows[#rows+1]={key=k,exists=ex,dataHex=x,version=v} end return cjson.encode(rows)"""
last=None;copies=0
for attempt in range(900):
 if (e/'run.json').exists():break
 children=Path('/proc',str(pid),'task',str(pid),'children')
 if not children.exists():break
 private=None
 for child in children.read_text().split():
  try:env=dict(x.split(b'=',1) for x in Path('/proc',child,'environ').read_bytes().split(b'\0') if b'=' in x)
  except OSError:continue
  if env.get(b'SUPPORTED_PROJECT_READ_EVIDENCE')==str(e).encode() and b'SUPPORTED_PROJECT_READ_CONFIG' in env:
   config=Path(env[b'SUPPORTED_PROJECT_READ_CONFIG'].decode());private=config.parent;break
 if private is not None and (private/'endpoints.json').exists():
  st=private.lstat();assert stat.S_ISDIR(st.st_mode) and st.st_uid==os.geteuid() and stat.S_IMODE(st.st_mode)==0o700 and not private.is_symlink()
  for f in [config,private/'endpoints.json']:
   s=f.lstat();assert stat.S_ISREG(s.st_mode) and s.st_uid==os.geteuid() and s.st_nlink==1 and not f.is_symlink()
  cfg=json.loads(config.read_text())['Projects']['SupportedReads'];endpoints=json.loads((private/'endpoints.json').read_text());isolation=cfg['IsolationId'];tenant=cfg['Tenant'];assert tenant.startswith(isolation) and tenant!=cfg['ForeignTenant']
  cid=endpoints['redis-container-id'];assert len(cid)==64 and all(c in '0123456789abcdef' for c in cid)
  check=subprocess.run(['docker','inspect','--format','{"id":{{json .Id}},"owner":{{json (index .Config.Labels "hexalith.supported-reads")}}}',cid],capture_output=True,timeout=3,check=True);owner=json.loads(check.stdout);assert owner=={'id':cid,'owner':isolation}
  scopes=[(tenant,'folders',tenant,'operational-status'),(tenant,'folders','folder-project-a','operational-status'),(tenant,'folders','folder-project-b','operational-status')]+[(tenant,'projects',a,p) for a in ['project-a','project-b'] for p in ['project-reads','conversation-start-setup']]+[('system','global-administrators','global-administrators','global-administrators')]
  inventory=subprocess.run(['docker','exec',cid,'redis-cli','-e','--raw','EVAL',"return cjson.encode(redis.call('KEYS','*'))",'0'],capture_output=True,timeout=3,check=True)
  actual=json.loads(inventory.stdout);assert isinstance(actual,list) and len(actual)<=4096
  keys=[k for k in actual if k.startswith(('projection-activation:','projection-checkpoints','projection-delivery-','projection-rebuild','eventstore||ProjectionLifecycleActor||','shared-epoch:'))]+['projection-delivery-writer-protocol']
  for scope_tenant,domain,aggregate,projection in scopes:
   actor=scope_tenant+':'+domain+':'+aggregate+':'+projection;keys+=['projection-checkpoints:'+actor,'eventstore||ProjectionLifecycleActor||'+actor+'||projection-lifecycle-epoch','eventstore||ProjectionLifecycleActor||'+actor+'||projection-lifecycle']
  keys=list(dict.fromkeys(keys));assert len(keys)<=256
  read=subprocess.run(['docker','exec',cid,'redis-cli','-e','--raw','EVAL',script,str(len(keys)),*keys],capture_output=True,timeout=3,check=True);assert len(read.stdout)<=2097152;rows=json.loads(read.stdout);assert len(rows)==len(keys) and [r['key'] for r in rows]==keys
  digest=hashlib.sha256(read.stdout).hexdigest()
  if digest!=last:
   packet=json.dumps({'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exactLauncherPid':pid,'immutableOwnedRedisId':cid,'isolationId':isolation,'rows':rows},indent=2).encode();path=out/'shared-epoch-proof-state.json';tmp=out/'shared-epoch-proof-state.pending';fd=os.open(tmp,os.O_WRONLY|os.O_CREAT|os.O_TRUNC|os.O_NOFOLLOW,0o600)
   try:os.write(fd,packet)
   finally:os.close(fd)
   os.replace(tmp,path);copies+=1;last=digest
 time.sleep(1)
print('Exact-owned read-only shared epoch snapshots retained privately:',copies)
