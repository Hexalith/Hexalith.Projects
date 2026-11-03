from pathlib import Path
import json,os,stat,subprocess,time,hashlib,sys,datetime
co=json.loads(Path(sys.argv[1]).read_text());pid=co['launcherPid'];e=Path(co['evidence']);out=Path(co['privateDiagnosticDirectory']);assert stat.S_IMODE(out.stat().st_mode)==0o700 and out.stat().st_uid==os.geteuid()
script="""local rows={} local h='0123456789ABCDEF' for _,k in ipairs(KEYS) do local ex=redis.call('EXISTS',k)==1 local d=ex and redis.call('HGET',k,'data') or false local v=ex and redis.call('HGET',k,'version') or false local x=false if d then local b={} for i=1,#d do local n=string.byte(d,i) b[i]=string.sub(h,math.floor(n/16)+1,math.floor(n/16)+1)..string.sub(h,n%16+1,n%16+1) end x=table.concat(b) end rows[#rows+1]={key=k,exists=ex,dataHex=x,version=v} end return cjson.encode(rows)"""
copies=0
for attempt in range(600):
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
  cfg=json.loads(config.read_text())['Projects']['SupportedReads'];endpoints=json.loads((private/'endpoints.json').read_text());isolation=cfg['IsolationId'];tenant=cfg['Tenant'];foreign=cfg['ForeignTenant'];assert tenant.startswith(isolation) and tenant!=foreign
  cid=endpoints['redis-container-id'];assert len(cid)==64 and all(c in '0123456789abcdef' for c in cid)
  check=subprocess.run(['docker','inspect','--format','{"id":{{json .Id}},"owner":{{json (index .Config.Labels "hexalith.supported-reads")}}}',cid],capture_output=True,timeout=3,check=True);assert json.loads(check.stdout)=={'id':cid,'owner':isolation}
  keys=[prefix+t for t in [tenant,foreign] for prefix in ['projects:tenant-access:','projection:folders:tenant-access:']]
  read=subprocess.run(['docker','exec',cid,'redis-cli','-e','--raw','EVAL',script,str(len(keys)),*keys],capture_output=True,timeout=3,check=True);assert len(read.stdout)<=262144;rows=json.loads(read.stdout);assert len(rows)==len(keys) and [r['key'] for r in rows]==keys
  acl=subprocess.run(['docker','exec',cid,'redis-cli','-e','--raw','ACL','GETUSER','default'],capture_output=True,timeout=3,check=True);assert len(acl.stdout)<=65536;acl_value={'nativeRawReply':acl.stdout.decode(errors='replace')}
  audit_key='projects:read-audit:v1:'+hashlib.sha256((isolation+'diagnostic-only-no-write').encode()).hexdigest().upper()
  dry=None
  packet={'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exactLauncherPid':pid,'immutableOwnedRedisId':cid,'isolationId':isolation,'rows':rows,'actualDefaultUserAcl':acl_value,'actualReadOnlyAuditDryRun':{'notSupportedByRedis6':True,'notUsedAsPermissionProof':True,'keySha256':hashlib.sha256(audit_key.encode()).hexdigest()},'domainOrAuditWriteInvoked':False}
  data=(json.dumps(packet)+'\n').encode();path=out/'freshness-and-audit-proof-state.jsonl'
  assert not path.exists() or path.stat().st_size+len(data)<=16*1024*1024
  fd=os.open(path,os.O_WRONLY|os.O_APPEND|os.O_CREAT|os.O_NOFOLLOW,0o600)
  try:os.write(fd,data)
  finally:os.close(fd)
  copies+=1
 time.sleep(3)
print('Exact-owned readonly Tenant/ACL observations retained privately:',copies)
