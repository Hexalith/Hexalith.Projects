from pathlib import Path
import datetime,fcntl,hashlib,json,os,re,secrets,stat,statistics,subprocess,tempfile,textwrap,time,concurrent.futures
root=Path('/home/administrator/projects/hexalith/projects');output=root/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/scenario-provider-control-profile';output.mkdir(exist_ok=True)
source=root/'tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs';raw=Path('/tmp/story61-owned-diagnostics-live48-full-replay-lk0pfa11/freshness-and-audit-proof-state.jsonl');rows=json.loads(raw.read_text().splitlines()[-1])['rows'];assert len(rows)==4 and all(r['exists'] for r in rows)
script=textwrap.dedent(source.read_text().split('private const string RawStateBatchScript = """',1)[1].split('""";',1)[0]).strip()
owner='reads-observer-profile-'+secrets.token_hex(12);keys=['supported-read-observer-control||'+owner+'||'+str(n) for n in range(108)];values={k:(bytes.fromhex(rows[n%4]['dataHex']),rows[n%4]['version']) for n,k in enumerate(keys[:104])};values.update({keys[104+n]:(data,'9223372036854775807' if n==3 else str(n+1)) for n,data in enumerate((bytes(range(256)),b'', '\x00\r\n\"\\é雪'.encode(),b'{\"data\":\"escaped\\nvalue\"}\n'))})
image='sha256:c35b83ce044bb6d148c484d36e059ad28e02d5714ba6731fb55b6421e2ed0ccf';private=Path(tempfile.mkdtemp(prefix='story61-observer-profile-private-'));os.chmod(private,0o700);lock=Path('/tmp/hexalith-timesheets-local-runtime.lock');fd=None;cid=None;report={'purpose':'Read-only observer transport model; retained real Tenant projection bytes copied to finite operational controls, no AppHost/Dapr/live authority or qualification evidence','sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'retainedInputPath':str(raw),'retainedInputSha256':hashlib.sha256(raw.read_bytes()).hexdigest(),'inputByteVersionFingerprints':[{'dataSha256':hashlib.sha256(b).hexdigest(),'version':v} for b,v in values.values()],'parityControlKeyCount':108,'realDomainKeyCountFromActualGroup':104,'modelData':'104 repeated recorded Tenant projection values + exact four binary vectors; not actual 104 domain payloads','ordinaryProfileUnchanged':True,'trials':[],'commands':[]}
def execute(command,input=None):
 report['commands'].append(command if not input else command+['<private-binary-stdin>']);r=subprocess.run(command,input=input,capture_output=True,check=True,timeout=15);return r.stdout
def lockidentity(s):return {'device':s.st_dev,'inode':s.st_ino,'uid':s.st_uid,'mode':stat.S_IMODE(s.st_mode),'links':s.st_nlink,'size':s.st_size,'modifiedNs':s.st_mtime_ns}
def verify():
 actual=json.loads(execute(['docker','inspect','--format','{"id":{{json .Id}},"owner":{{json (index .Config.Labels "hexalith.supported-reads")}}}',cid]));assert actual=={'id':cid,'owner':owner}
def redis(*args):verify();return execute(['docker','exec',cid,'redis-cli','-e','--raw',*args])
def old():
 def one(k):
  assert redis('EXISTS',k).strip()==b'1';data=redis('-D','','HGET',k,'data');version=redis('HGET',k,'version').decode().rstrip('\n');return k,(data,version)
 with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:return dict(pool.map(one,keys))
def batch():
 observations=json.loads(redis('EVAL',script,str(len(keys)),*keys));assert len(observations)==len(keys);return {r['key']:(bytes.fromhex(r['dataHex']),r['version']) for r in observations if r['exists']}
def resp(parts):return b'*'+str(len(parts)).encode()+b'\r\n'+b''.join(b'$'+str(len(x)).encode()+b'\r\n'+x+b'\r\n' for x in parts)
try:
 fd=os.open(lock,os.O_RDWR|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC);s=os.fstat(fd);identity=lockidentity(s);assert stat.S_ISREG(s.st_mode) and s.st_uid==os.geteuid() and s.st_nlink==1 and not s.st_mode&0o077 and identity==lockidentity(lock.lstat());fcntl.flock(fd,fcntl.LOCK_EX|fcntl.LOCK_NB);report['admission']={'acquired':True,'identity':identity,'contentsReadOrWritten':False,'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
 execute(['docker','image','inspect',image]);cid=execute(['docker','run','--detach','--network','none','--label','hexalith.supported-reads='+owner,'--cidfile',str(private/'container.cid'),image]).decode().strip();assert re.fullmatch('[0-9a-f]{64}',cid);assert (private/'container.cid').read_text().strip()==cid
 payload=b''.join(resp([b'HSET',k.encode(),b'data',b,b'version',v.encode()]) for k,(b,v) in values.items());verify();execute(['docker','exec','-i',cid,'redis-cli','--pipe'],payload)
 for iteration in range(8):
  modes=('individual','batch') if iteration%2==0 else ('batch','individual')
  trial={'iteration':iteration,'warmup':iteration<2}
  for mode in modes:
   start=time.perf_counter();observed=old() if mode=='individual' else batch();elapsed=time.perf_counter()-start;assert observed==values;trial[mode+'Seconds']=elapsed
  report['trials'].append(trial)
 trials=report['trials'][2:];before=statistics.median(x['individualSeconds'] for x in trials);after=statistics.median(x['batchSeconds'] for x in trials);report['summary']={'individualMedianSeconds':before,'batchMedianSeconds':after,'medianSavingPerSnapshotSeconds':before-after,'modeledCompleteControlSavingSeconds':before-after,'parityVerifiedEveryTrial':True,'individualDockerProcessCount':648,'batchDockerProcessCount':2,'exactCapEightIndividualReads':True,'note':'Control remains mandatory; candidate moves it after protected case reads rather than removing its cost'}
finally:
 if cid is not None:
  verify();execute(['docker','rm','-f','-v',cid]);assert cid not in execute(['docker','ps','--all','--no-trunc','--format','{{.ID}}']).decode().splitlines();report['ownedContainerAbsent']=True;report['ownedContainerIdSha256']=hashlib.sha256(cid.encode()).hexdigest()
 if fd is not None:
  report['admission']['metadataPreserved']=identity==lockidentity(lock.lstat());os.close(fd);report['admission']['released']=True
 if (private/'container.cid').exists():(private/'container.cid').unlink()
 private.rmdir();report['privateMaterialRemoved']=True
 (output/'result.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report['summary']));print('Exact owned benchmark container absent; lock released/metadata preserved.')
