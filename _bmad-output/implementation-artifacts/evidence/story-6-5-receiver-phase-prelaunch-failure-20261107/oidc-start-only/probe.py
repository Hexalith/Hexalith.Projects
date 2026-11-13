from pathlib import Path
import datetime,hashlib,importlib.util,json,os,re,secrets,shutil,sys,tempfile
sys.dont_write_bytecode=True
R=Path('/home/administrator/projects/hexalith/projects');OUT=Path('/tmp/story65-oidc-start-probe-20261107');assert not OUT.exists();OUT.mkdir(mode=0o700);os.umask(0o077)
sys.path.insert(0,str(R/'tools/qualification'));s=importlib.util.spec_from_file_location('owned_oidc_start_probe',R/'tools/qualification/run_supported_project_reads.py');m=importlib.util.module_from_spec(s);sys.modules[s.name]=m;s.loader.exec_module(m)
from supported_browser_reads import prepare_oidc
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
source_hashes={str(p):sha(p)for p in [R/'tools/qualification/run_supported_project_reads.py',R/'tools/qualification/supported_browser_reads.py']}
before=set(m.container_listing());image=json.loads(m.execute(['docker','image','inspect','quay.io/keycloak/keycloak:26.6']));assert len(image)==1;image_id=image[0]['Id']
private=Path(tempfile.mkdtemp(prefix='supported-project-reads-private-'));private.chmod(0o700);owner_stat=private.stat();assert owner_stat.st_uid==os.geteuid()==1000
isolation='reads-'+secrets.token_hex(12);owned=[];report={'cleanup':[],'result':'failed'};facts={'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'oidcStartOnly':True,'applicationHostsStarted':0,'nativeOrBrowserCasesStarted':0,'privateSdkBuildsStarted':0,'tokensRequested':False,'oidcReadinessClaimed':False,'sourceSha256':source_hashes,'imageAlreadyCached':True,'startAttempted':False,'startSucceeded':False,'errorCategory':None,'rawCommandOutputOrIdentityValuesPersisted':False,'probeScriptSha256':sha(__file__)}
class StartObserved(Exception):pass
def classify(text):
 v=text[:16384].lower()
 for category,needles in [('bind-source-missing',['bind source path does not exist','no such file or directory']),('bind-path-shape',['not a directory']),('port-unavailable',['port is already allocated','address already in use']),('permission-denied',['permission denied']),('storage-capacity',['no space left on device']),('daemon-connectivity',['cannot connect to the docker daemon','is the docker daemon running']),('oci-runtime-failure',['oci runtime create failed','failed to create task for container'])]:
  if any(x in v for x in needles):return category
 return 'unclassified-docker-start-error'
def run(command,**kw):
 if command[:2]==['docker','start']:
  assert len(command)==3 and len(owned)==1 and owned[0]['id']==command[2]and re.fullmatch('[0-9a-f]{64}',command[2])and command[2]not in before
  observed=json.loads(m.execute(['docker','inspect',command[2]]));assert len(observed)==1 and observed[0]['Id']==command[2]and observed[0]['Image']==image_id and observed[0]['Config']['Labels'].get('hexalith.supported-reads')==isolation
  facts['startAttempted']=True
  try:m.execute(command,timeout=45)
  except RuntimeError as error:
   facts['errorCategory']=classify(getattr(error,'command_output',''));raise
  facts['startSucceeded']=True;raise StartObserved()
 return m.execute(command,**kw)
try:
 m.execute(['openssl','req','-x509','-newkey','rsa:2048','-nodes','-days','1','-subj','/CN=localhost','-addext','subjectAltName=DNS:localhost,IP:127.0.0.1','-addext','keyUsage=digitalSignature,keyCertSign','-keyout',str(private/'certificate.key'),'-out',str(private/'certificate.pem')])
 for p in private.iterdir():p.chmod(0o600)
 prepare_oidc(R,private,isolation,owned,run,m.free_port,secrets.token_hex(32))
 raise AssertionError('StartSentinelWasNotReached')
except StartObserved:report['result']='passed'
except BaseException as error:facts['exceptionType']=type(error).__name__
finally:
 m.cleanup_resources(None,owned,isolation,m.execute,report)
 after=set(m.container_listing());owned_ids={v['id']for v in owned if v.get('id')};cleanup_ok=all(x.get('removed')is True and x.get('absenceVerified')is True for x in report['cleanup'])and not(owned_ids&after)
 facts.update(ownedContainerCount=len(owned_ids),ownedContainerCleanupVerified=cleanup_ok,preexistingContainerIdentitiesPreserved=before<=after,allContainerIdentitySetsRestored=before==after,sourceFilesUnchanged=all(sha(p)==h for p,h in source_hashes.items()),finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
 if cleanup_ok:
  now=private.lstat();assert now.st_ino==owner_stat.st_ino and now.st_dev==owner_stat.st_dev and now.st_uid==1000 and private.parent==Path('/tmp')and private.name.startswith('supported-project-reads-private-')
  assert all(not p.is_symlink()and p.stat().st_uid==1000 for p in private.rglob('*'));shutil.rmtree(private);facts['ownedPrivateMaterialRemoved']=not private.exists()
 else:facts['ownedPrivateMaterialRemoved']=False
 facts['passed']=facts['startSucceeded']and cleanup_ok and facts['allContainerIdentitySetsRestored']and facts['sourceFilesUnchanged']and facts['ownedPrivateMaterialRemoved']
 (OUT/'receipt.json').write_text(json.dumps(facts,indent=2)+'\n');print(json.dumps({k:v for k,v in facts.items()if k not in ['sourceSha256','probeScriptSha256']}))
