"""Closed external guards; no launcher replacement, raw runtime identity output or target control."""
from pathlib import Path
import hashlib,importlib.util,json,math,os,re,stat,subprocess,sys,time
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path(__file__).resolve().parent
CORE=Path('/tmp/story65-dapr-retry-metrics-prep-20261108')
ORDINARY=Path('/tmp/story65-canonical-hash-acceptance-prep-20261108')
LAUNCHER=ROOT/'tools/qualification/run_supported_project_reads.py'
LIVE=Path('/tmp/story65-dapr-retry-metrics-owner-live-20261108')
OUTPUT=Path('/tmp/story65-dapr-retry-metrics-owner-observation-20261108')
MAPPING={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'}
COMMAND=['npm','--prefix','tests/e2e','run','test:live:managed']
RUNTIME_PROJECTS={
 'eventstore':'references/Hexalith.EventStore/src/Hexalith.EventStore/Hexalith.EventStore.csproj',
 'tenants':'references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj',
 'projects':'src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj',
 'projects-workers':'src/Hexalith.Projects.Workers/Hexalith.Projects.Workers.csproj',
 'folders':'references/Hexalith.Folders/src/Hexalith.Folders.Server/Hexalith.Folders.Server.csproj',
 'folders-workers':'references/Hexalith.Folders/src/Hexalith.Folders.Workers/Hexalith.Folders.Workers.csproj'}
class Guard(Exception):pass

def require(value,check):
 if not value:raise Guard(check)
def sha(path):
 fd=os.open(path,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC)
 with os.fdopen(fd,'rb')as stream:
  require(stat.S_ISREG(os.fstat(stream.fileno()).st_mode),'input-not-regular')
  return hashlib.file_digest(stream,'sha256').hexdigest()
def read(path):return json.loads(Path(path).read_bytes())
def write(path,value):
 path=Path(path);temporary=path.with_suffix(path.suffix+'.tmp')
 with temporary.open('x')as stream:json.dump(value,stream,indent=2,allow_nan=False);stream.write('\n')
 temporary.chmod(0o600);temporary.replace(path)
def load(path,name):
 spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);sys.modules[name]=module;spec.loader.exec_module(module);return module

def process(pid):
 try:
  fields=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()
  return {'state':fields[0],'parent':int(fields[1]),'group':int(fields[2]),'session':int(fields[3]),'start':fields[19]}
 except(FileNotFoundError,ProcessLookupError):return None
def same(pid,ticks):
 row=process(pid);return row is not None and row['state']!='Z'and row['start']==str(ticks)
def members(group):
 result=[]
 for path in Path('/proc').iterdir():
  if not path.name.isdigit():continue
  row=process(int(path.name))
  if row is not None and(row['group']==group or row['session']==group):result.append((int(path.name),row['start']))
 return result # Includes zombies; no signals, raw argv, environment or map output.
def mapped(pid,path):
 try:return any(len(v:=line.split(None,5))==6 and v[5].strip()==str(path)for line in Path('/proc',str(pid),'maps').read_text().splitlines())
 except(FileNotFoundError,ProcessLookupError):return False

def verify_preparation(path,digest,*,released=False):
 require(path is not None and type(digest)is str and re.fullmatch('[0-9a-f]{64}',digest),'root-release-required')
 require(sha(path)==digest,'root-release-hash-required');data=read(path)
 require(data.get('executionReleased')is released,'root-release-state')
 for name,expected in data['inputsSha256'].items():require(sha(name)==expected,'integration-static-input-changed')
 return data

def controls():return load(ORDINARY/'supervise.py','ordinary_readonly_controls')
def ordinary_preflight():
 c=controls();b=read(PREP/'ordinary-binding.json')
 for key in ['finalNativeReceipt','parentNativeAudit','compiledTransferAudit']:c.evidence(b[key])
 return {'sources':c.sources(b),'protected':c.protected(b),'generations':c.generations(b,True)}
def ordinary_current():
 c=controls();b=read(PREP/'ordinary-binding.json')
 return {'sources':c.sources(b),'protected':c.protected(b),'generations':c.generations(b)}
def full_source(evidence):
 path=evidence/'source-manifest.json';manifest=read(path);require(isinstance(manifest,dict)and bool(manifest),'source-manifest-unavailable')
 for name,digest in manifest.items():
  rel=Path(name);require(not rel.is_absolute()and'..'not in rel.parts and(ROOT/rel).resolve().is_relative_to(ROOT),'source-manifest-coordinate')
  require(type(digest)is str and re.fullmatch('[0-9a-f]{64}',digest)and sha(ROOT/rel)==digest,'live-source-manifest-changed')
 return (sha(path),len(manifest))
def observations(evidence):
 try:data=(evidence/'browser-advisory-seed-readiness.jsonl').read_bytes()
 except FileNotFoundError:return []
 require(len(data)<=4*1024*1024,'readiness-cap');rows=[]
 for line in data.split(b'\n')[:-1]:
  try:d=json.loads(line)
  except ValueError:continue
  if isinstance(d,dict)and d.get('multiple')is True and d.get('actualPublicGateway')is True and d.get('fabricatedProjectionWrite')is False and d.get('nonBootstrapCredential')is True and d.get('runtimeAuthoritySupplied')is False and type(d.get('attempt'))is int and 1<=d['attempt']<=30 and type(d.get('httpStatus'))is int and d['httpStatus']==200 and type(d.get('incomplete'))is bool and type(d.get('candidateCount'))is int and d['candidateCount']>=0 and type(d.get('positiveCandidatesVerified'))is bool and type(d.get('elapsedMilliseconds'))in[int,float]and math.isfinite(d['elapsedMilliseconds'])and d['elapsedMilliseconds']>=0 and d.get('result')in[None,'Empty','SingleCandidate','MultipleCandidates']:rows.append(d)
 return rows

def decision(rows):
 if any(r['result']=='MultipleCandidates'and not r['incomplete']and r['candidateCount']>=2 and r['positiveCandidatesVerified']for r in rows):return'positive-skip'
 if not rows:return'wait'
 if max(r['attempt']for r in rows)>24:return'late-skip'
 failed={r['attempt']for r in rows if r['result']in[None,'Empty']and r['incomplete']and r['candidateCount']==0 and not r['positiveCandidatesVerified']}
 return'failed-window'if len(failed)>=3 else'wait'
