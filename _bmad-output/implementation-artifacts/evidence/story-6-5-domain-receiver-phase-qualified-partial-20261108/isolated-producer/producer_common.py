"""Readonly source/carrier metadata helpers. No build or assembly load on import."""
from pathlib import Path
import hashlib,json,os,re,stat
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-domain-receiver-phase-producer-prep-20261108')
SOURCE=Path('/tmp/story65-domain-receiver-phase-prep-20261108')
ASSEMBLY='Hexalith.EventStore.DomainService'
PROJECT=ROOT/'references/Hexalith.EventStore/src'/ASSEMBLY
SDK_PROJECT=ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults'
SDK_SHA='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
SDK_PDB_SHA='ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
def sha(file):
 fd=os.open(file,os.O_RDONLY|os.O_NOFOLLOW)
 with os.fdopen(fd,'rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(name,value):
 path=PREP/name;path.parent.mkdir(mode=0o700,parents=True,exist_ok=True)
 with path.open('x')as stream:json.dump(value,stream,indent=2);stream.write('\n')
def marker(file):
 data=Path(file).read_bytes()
 return any(s in data or s.decode().encode('utf-16-le')in data for s in[b'Hexalith.PrivateCaptureCounter',b'CaptureCounters.cs',b'DomainRouteCatalogFilter.private.cs',b'DomainRouteCatalogGuard.private.cs',b'DomainRouteCatalogResult.private.cs',b'ReceiverPhaseBootstrap'])
def verify_core():
 binding=json.loads((PREP/'source-core-binding.json').read_text());assert binding['sourcePinsReady']is True and binding['sourceQualificationReady']is False
 assert sha(binding['sourceCoreReview'])==binding['sourceCoreReviewSha256'];proof=json.loads(Path(binding['sourceCoreReview']).read_text());assert proof['sourceStrategyReviewedByRoot']is True and proof['independentReaderAck']is True and proof['allThreeOriginalsByteExactAfterReverse']is True
 assert len(binding['sourceSha256'])==59
 checked={str(Path(binding['sourceCoreReview'])):binding['sourceCoreReviewSha256']}
 for relative,digest in binding['sourceSha256'].items():
  rel=Path(relative);assert not rel.is_absolute()and'..'not in rel.parts;file=ROOT/rel;assert file.resolve().is_relative_to(ROOT)and sha(file)==digest;checked[str(file)]=digest
 assert sha(binding['normalizationFile'])==binding['normalizationSha256'];normal=json.loads(Path(binding['normalizationFile']).read_text());assert normal['allThreeOriginalsByteExactAfterReversingDocumentedEdits']is True;checked[binding['normalizationFile']]=binding['normalizationSha256']
 for name,digest in binding['corePrivateSha256'].items():
  file=SOURCE/(name+'.private.cs');assert sha(file)==digest==normal['changes'][name]['privateSha256'];checked[str(file)]=digest
 for name,key in [('CaptureCounters.cs','helperSha256'),('ReceiverPhaseBootstrap.cs','bootstrapSha256')]:assert sha(SOURCE/name)==binding[key];checked[str(SOURCE/name)]=binding[key]
 return checked

def census(assembly):
 rows=[];roots=[ROOT]+[ROOT/name for name in re.findall(r'^\s*path\s*=\s*(.+)$',(ROOT/'.gitmodules').read_text(),re.M)]
 for root in roots:
  for current,dirs,files in os.walk(root):
   dirs[:]=[d for d in dirs if d not in['.git','references','node_modules']]
   for name in files:
    if name not in[assembly+'.dll',assembly+'.pdb']:continue
    file=Path(current)/name
    if'Debug'not in file.parts or not any(p in['bin','obj']for p in file.relative_to(root).parts):continue
    assert not file.is_symlink();parent=file.parent
    while parent!=root and parent.name not in['bin','obj']:parent=parent.parent
    projects=list(parent.parent.glob('*.csproj'))if parent.name in['bin','obj']else[]
    rows.append({'path':str(file.relative_to(ROOT)),'sha256':sha(file),'kind':'pdb'if file.suffix=='.pdb'else'refint'if'refint'in file.parts else'ref'if'ref'in file.parts else'full','project':str(projects[0].relative_to(ROOT))if len(projects)==1 else None,'privateMarker':marker(file)})
 return sorted(rows,key=lambda r:r['path'])
def module_files(project):
 rows={}
 for folder in['bin','obj']:
  for file in(project/folder).rglob('*'):
   if not file.is_file():continue
   assert not file.is_symlink();s=file.stat();rows[str(file.relative_to(ROOT))]={'sha256':sha(file),'bytes':s.st_size,'inode':s.st_ino,'mtimeNs':s.st_mtime_ns,'ctimeNs':s.st_ctime_ns}
 return rows

def snapshot():
 verify_core();binding=json.loads((PREP/'source-core-binding.json').read_text())
 sdk=SDK_PROJECT/'bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll';assert sha(sdk)==SDK_SHA and sha(sdk.with_suffix('.pdb'))==SDK_PDB_SHA
 coords=json.loads((PREP/'sdk-coordinate-inputs.json').read_text());sdk8=[]
 for relative in coords['sdk8']:
  file=ROOT/relative;assert sha(file)==SDK_SHA and sha(file.with_suffix('.pdb'))==SDK_PDB_SHA;sdk8.append({'path':relative,'sha256':sha(file),'pdbSha256':sha(file.with_suffix('.pdb'))})
 assert len(sdk8)==8
 audit=json.loads(Path(coords['native147Audit']['path']).read_text());assert sha(coords['native147Audit']['path'])==coords['native147Audit']['sha256'];app={row['path']:sha(ROOT/row['path'])for row in audit['files']};assert len(app)==147 and all(not marker(ROOT/name)for name in app)
 source={str(f.relative_to(ROOT)):sha(f)for f in PROJECT.rglob('*.cs')if not any(x in['bin','obj']for x in f.relative_to(PROJECT).parts)}
 result={'source59Sha256':binding['sourceSha256'],'domainCSharpSourceSha256':source,'app147CurrentSha256':app,'domainBinObjMetadata':module_files(PROJECT),'sdkBinObjMetadata':module_files(SDK_PROJECT),'sdk8':sdk8}
 for alias,assembly in [('domain','Hexalith.EventStore.DomainService'),('sdk','Hexalith.EventStore.ServiceDefaults'),('server','Hexalith.EventStore.Server'),('gateway','Hexalith.EventStore.Gateway'),('rootServer','Hexalith.Projects.Server')]:
  result[alias+'Census']=census(assembly);assert all(not row['privateMarker']for row in result[alias+'Census'])
 assert len(result['sdkCensus'])==137
 return result

def environment():
 mapping=json.loads((PREP/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(path)==digest for path,digest in mapping['files'].items())
 env=os.environ.copy()
 for name in list(env):
  if name in mapping['environment']or name in['CustomBeforeMicrosoftCommonTargets','DOTNET_PROCESSOR_COUNT']:env.pop(name)
 env.update(mapping['environment']);env.update(HexalithBuildsRoot=str(ROOT/'references/Hexalith.Builds'),DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
 return mapping,env
