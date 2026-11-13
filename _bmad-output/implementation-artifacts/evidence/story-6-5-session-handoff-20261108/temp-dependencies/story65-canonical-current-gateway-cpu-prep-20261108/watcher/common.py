"""External metadata guards. No target lifecycle, application configuration or logging changes."""
from pathlib import Path
import hashlib,json,math,os,re,stat,time,importlib.util
ROOT=Path('/home/administrator/projects/hexalith/projects');PREP=Path('/tmp/story65-canonical-current-gateway-cpu-prep-20261108');OPS=PREP/'watcher'
APPHOST=ROOT/'src/Hexalith.Projects.AppHost/bin/Debug/net10.0/Hexalith.Projects.AppHost'
MAPPING={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'}
FLAGS={'EventStore__RouteCatalog__ScopedDecodeReuse':'true','EventStore__RouteCatalog__SerializeLocalLeaseRelease':'false','EventStore__RouteCatalog__RetainValidatedCatalogBytes':'true'}
MARKERS=['Hexalith.PrivateCaptureCounter','TimingHexalith','PrivateFocusedTiming','Story65Timing']
class Guard(Exception):pass

def sha(path):
 fd=os.open(path,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC)
 with os.fdopen(fd,'rb')as stream:
  if not stat.S_ISREG(os.fstat(stream.fileno()).st_mode):raise Guard('input-not-regular')
  return hashlib.file_digest(stream,'sha256').hexdigest()
def read_json(path):
 try:return json.loads(Path(path).read_bytes())
 except(FileNotFoundError,ValueError):return None

def write(path,value):
 path=Path(path);path.parent.mkdir(mode=0o700,parents=True,exist_ok=True);temp=path.with_suffix(path.suffix+'.tmp');temp.write_text(json.dumps(value,indent=2)+'\n');temp.chmod(0o600);temp.replace(path)
def process(pid):
 try:
  fields=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return None if fields[0]=='Z'else{'pid':pid,'startTicks':fields[19],'parent':int(fields[1]),'group':int(fields[2])}
 except(FileNotFoundError,ProcessLookupError):return None

def same(pid,ticks):
 value=process(pid);return value is not None and value['startTicks']==str(ticks)
def mapped(pid,path):
 try:return any(len(v:=line.split(None,5))==6 and v[5].strip()==str(path)for line in Path('/proc',str(pid),'maps').read_text().splitlines())
 except(FileNotFoundError,ProcessLookupError):return False

def known_env(pid,expected,absent=()):
 # Call only after exact fresh AppHost/DCP/source executable/ancestry classification. Never retain raw entries.
 raw=Path('/proc',str(pid),'environ').read_bytes();wanted=set(expected)|set(absent);found={}
 for value in raw.split(b'\0'):
  if b'='not in value:continue
  key,data=value.split(b'=',1)
  if key.decode(errors='replace')in wanted:
   name=key.decode();
   if name in found:raise Guard('duplicate-known-environment-key')
   found[name]=data.decode()
 del raw
 if any(found.get(k)!=v for k,v in expected.items())or any(k in found for k in absent):raise Guard('owned-known-environment-mismatch')
 return True

def roles():
 value=read_json(PREP/'collector-guard-map.json');result=value['roles'];assert {v['role']for v in result}=={'gateway'};return result

def marker_free(file):
 data=Path(file).read_bytes()
 if any(x.encode()in data or x.encode('utf-16-le')in data for x in MARKERS):raise Guard('private-timing-marker-present')
def sources(evidence=None):
 snapshot=read_json(OPS/'source-controls.json')
 if type(snapshot)is not dict or type(snapshot.get('sourceSha256'))is not dict or len(snapshot['sourceSha256'])!=65:raise Guard('source65-schema-invalid')
 for name,digest in snapshot['sourceSha256'].items():
  if type(name)is not str or type(digest)is not str or re.fullmatch('[0-9a-f]{64}',digest)is None:raise Guard('source65-schema-invalid')
  path=Path(name)
  if path.is_absolute()or'..'in path.parts or not(ROOT/path).resolve().is_relative_to(ROOT):raise Guard('source65-coordinate-invalid')
  if sha(ROOT/path)!=digest:raise Guard('source65-mismatch')
 if evidence is None:return {'sourceCount':65,'source65Match':True}
 path=Path(evidence)/'source-manifest.json';manifest=read_json(path)
 if not isinstance(manifest,dict)or not manifest:raise Guard('live-source-manifest-unavailable')
 for rel,digest in manifest.items():
  name=Path(rel)
  if name.is_absolute()or'..'in name.parts or not(ROOT/name).resolve().is_relative_to(ROOT):raise Guard('live-source-coordinate-invalid')
  if type(digest)is not str or re.fullmatch('[0-9a-f]{64}',digest)is None:raise Guard('live-source-schema-invalid')
  if sha(ROOT/name)!=digest:raise Guard('live-source-manifest-mismatch')
 ordinary=ordinary_controls().generations(ordinary_binding())
 return{'sourceCount':65,'source65Match':True,'fullManifestCount':len(manifest),'fullManifestSha256':sha(path),'fullManifestAllMatch':True,'currentProducer8AndActiveSdkContractsPairsVerified':True,'app147MarkerClean':len(ordinary['currentApp147Sha256'])==147,'additionalMarkerCheckedCount':len(ordinary['additionalHexalithAssemblyFilesSha256']),'currentOrdinaryCensusSha256':hashlib.sha256(json.dumps(ordinary,sort_keys=True).encode()).hexdigest()}

def generation(role,pid,ticks,evidence,source_proof=None):
 proof=dict(source_proof)if source_proof is not None else sources(evidence);files=[];base=Path(role['entryDll']).parent
 for label,name in [('eventstore-entry-assembly',Path(role['entryDll']).name),('gateway-assembly','Hexalith.EventStore.Gateway.dll'),('sdk-assembly','Hexalith.EventStore.ServiceDefaults.dll'),('domain-service-assembly','Hexalith.EventStore.DomainService.dll'),('contracts-assembly','Hexalith.EventStore.Contracts.dll')]:
  copy=base/name;marker_free(copy);digest=sha(copy)
  if label=='sdk-assembly'and(digest!='873dbb74ee259d91c99ea2804e5801134c993abb4ef7806e79f242e5d23faf96'or sha(copy.with_suffix('.pdb'))!='f4fae4fb336e03b5e0636a5781a817598b301f445a967d7b4eb309be29dd639e'):raise Guard('ordinary-sdk-baseline-mismatch')
  if label=='contracts-assembly'and(digest!='f1cf82fe57d5ae0037772d3ccea77766a141cd49ec66c2079c3c8b046c8089df'or sha(copy.with_suffix('.pdb'))!='bbe424a27b1f242f2d8865864121ea4a99ab40ecee493084df462cf0efd49a39'):raise Guard('ordinary-contracts-baseline-mismatch')
  if label!='eventstore-entry-assembly':
   producer=ROOT/'references/Hexalith.EventStore/src'/name.removesuffix('.dll')/'bin/Debug/net10.0'/name
   marker_free(producer)
   if sha(producer)!=digest or sha(producer.with_suffix('.pdb'))!=sha(copy.with_suffix('.pdb')):raise Guard('producer-runtime-copy-mismatch')
  if not mapped(pid,copy):raise Guard('owned-required-module-not-yet-mapped')
  files.append({'Label':label,'Path':str(copy),'Sha256':digest})
 if not same(pid,ticks):raise Guard('owned-target-start-changed')
 # Other ordinary modules retain separate current producer generations; no blanket older-byte equality.
 modules={}
 for name in ['Hexalith.EventStore.Contracts','Hexalith.EventStore.ServiceDefaults','Hexalith.EventStore.DomainService','Hexalith.EventStore.Server','Hexalith.EventStore.Gateway']:
  producer=ROOT/'references/Hexalith.EventStore/src'/name/'bin/Debug/net10.0'/(name+'.dll');marker_free(producer);marker_free(producer.with_suffix('.pdb'));modules[name]={'producerSha256':sha(producer),'producerPdbSha256':sha(producer.with_suffix('.pdb'))}
 proof.update(role=role['role'],pid=pid,startTicks=ticks,files=[{'label':v['Label'],'sha256':v['Sha256']}for v in files],currentOrdinaryModuleGenerations=modules,noHistoricalNativeByteEquivalenceClaimed=True)
 return files,proof

def observations(evidence):
 try:data=(Path(evidence)/'browser-advisory-seed-readiness.jsonl').read_bytes()
 except FileNotFoundError:return []
 rows=[]
 for line in data.split(b'\n')[:-1]:
  try:d=json.loads(line)
  except ValueError:continue
  if isinstance(d,dict)and set(['result','multiple','attempt','httpStatus','incomplete','candidateCount','positiveCandidatesVerified','elapsedMilliseconds','actualPublicGateway','fabricatedProjectionWrite','nonBootstrapCredential','runtimeAuthoritySupplied'])<=set(d)and d.get('multiple')is True and d['actualPublicGateway']is True and d['fabricatedProjectionWrite']is False and d['nonBootstrapCredential']is True and d['runtimeAuthoritySupplied']is False and type(d.get('attempt'))is int and 1<=d['attempt']<=30 and type(d.get('httpStatus'))is int and d['httpStatus']==200 and type(d.get('incomplete'))is bool and type(d.get('candidateCount'))is int and d['candidateCount']>=0 and type(d.get('positiveCandidatesVerified'))is bool and type(d.get('elapsedMilliseconds'))in[int,float]and math.isfinite(d['elapsedMilliseconds'])and d['elapsedMilliseconds']>=0 and d.get('result')in[None,'Empty','SingleCandidate','MultipleCandidates']:rows.append(d)
 return rows

def decision(rows):
 if any(d['result']=='MultipleCandidates'and not d['incomplete']and d['candidateCount']>=2 and d['positiveCandidatesVerified']for d in rows):return'positive-skip-all'
 if not rows:return'wait'
 if max(d['attempt']for d in rows)>24:return'insufficient-windows-skip-all'
 failed={d['attempt']for d in rows if d['result']in[None,'Empty']and d['incomplete']and d['candidateCount']==0 and not d['positiveCandidatesVerified']and d['attempt']<=24}
 return'failed-attach'if len(failed)>=3 else'wait'

# Reuse the already reviewed ordinary acceptance controls as read-only functions only.
# This is not the managed launcher; its main/launch functions are never invoked.
_ORDINARY_CONTROLS=None
def ordinary_controls():
 global _ORDINARY_CONTROLS
 if _ORDINARY_CONTROLS is None:
  path=Path('/tmp/story65-canonical-hash-acceptance-prep-20261108/supervise.py')
  spec=importlib.util.spec_from_file_location('canonical_ordinary_readonly_controls',path)
  module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);_ORDINARY_CONTROLS=module
 return _ORDINARY_CONTROLS

def ordinary_binding():
 value=read_json(OPS/'ordinary-binding.json')
 if not isinstance(value,dict):raise Guard('ordinary-current-binding-unavailable')
 return value

def ordinary_preflight():
 controls=ordinary_controls();binding=ordinary_binding()
 return {'sources':controls.sources(binding),'protected':controls.protected(binding),'generations':controls.generations(binding,True)}

def ordinary_after():
 controls=ordinary_controls();binding=ordinary_binding()
 return {'sources':controls.sources(binding),'protected':controls.protected(binding),'generations':controls.generations(binding)}
