from pathlib import Path
import hashlib,json,os,re,stat
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-advisory-wave-ops-prep-20261107')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
SDK='Hexalith.EventStore.ServiceDefaults'
SDK_PRODUCER=ROOT/'references/Hexalith.EventStore/src'/SDK
ASSEMBLY='Hexalith.Projects.Server'
PRODUCER=ROOT/'src'/ASSEMBLY
SOURCE_PREP=Path('/tmp/story65-advisory-wave-prep-20261107')
RECEIPT=Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json')
OWNER_SHA='2a031e4d183152beb50b56428805fab3af7b08b787dbad3fc6240ca3dd6e1fb8'
HELPER_SHA='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
ORIGINAL_CAPTURE_SHA='5e85827847578384a7dcca9fe448b97f21cdf4a201263c2d9ec9d0f3eda6be4d'
ORIGINAL_EXECUTOR_SHA='ac34b0c5c86c8a85ddf8591f73ddb02d32dc3a63db47a206b42688f2c696df8d'
PRIVATE_EXECUTOR_SHA='b2b672c688355e504f1de87b870b1e0226f609a23fecbdb2dfbe5a162c43eede'
BOOTSTRAP_SHA='8f5836d1ff4dabb4c5fe819ae762dfff0748b5d792f984ae1adf45af28f45375'
ORDINARY_SDK_SHA='5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872'
MAPPING={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'}
def sha(file):
 with Path(file).open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(path,value):
 path=Path(path);path.parent.mkdir(mode=0o700,parents=True,exist_ok=True);temporary=path.with_name(path.name+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');temporary.chmod(0o600);temporary.replace(path)
def operational_pins(expected):
 assert re.fullmatch(r'[0-9a-f]{64}',expected or'')and sha(PREP/'operational-pins.json')==expected
 pins=json.loads((PREP/'operational-pins.json').read_text())
 assert pins.get('sourceQualificationReady')is True,'Source/harness qualification is not yet released for execution'
 assert all(sha(PREP/name)==digest for name,digest in pins['files'].items())
 assert all(sha(path)==digest for path,digest in pins.get('externalFiles',{}).items())
 return pins
def sources():
 assert sha(RECEIPT)==OWNER_SHA and sha(SOURCE_PREP/'CaptureCounters.cs')==HELPER_SHA
 owner=json.loads(RECEIPT.read_text());assert len(owner['sourceSha256'])==48
 for relative,digest in owner['sourceSha256'].items():assert sha(ROOT/'references/Hexalith.EventStore'/relative)==digest
 assert sha(SDK_PRODUCER/'Routing/DevelopmentRouteCatalogInputs.cs')==ORIGINAL_CAPTURE_SHA
 assert sha(PRODUCER/'Queries/ProjectAdvisoryQueryExecutor.cs')==ORIGINAL_EXECUTOR_SHA
 assert sha(SOURCE_PREP/'ProjectAdvisoryQueryExecutor.private.cs')==PRIVATE_EXECUTOR_SHA
 assert sha(SOURCE_PREP/'AdvisoryWaveBootstrap.cs')==BOOTSTRAP_SHA
 return {'ownerSource48Match':True,'extraCaptureSourceMatch':True,'ordinaryExecutorExtraMatch':True,'sourceCount':50,'helperSha256':HELPER_SHA,'privateExecutorSha256':PRIVATE_EXECUTOR_SHA,'firstEntryBootstrapSha256':BOOTSTRAP_SHA}
def private_directory(require_empty=False):
 pin=json.loads((PREP/'private-directory-proof.json').read_text());s=PRIVATE.lstat()
 assert stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)and stat.S_IMODE(s.st_mode)==0o700 and s.st_uid==pin['uid']and s.st_dev==pin['device']and s.st_ino==pin['inode']
 if require_empty:assert not any(PRIVATE.iterdir())
 return {'uid':s.st_uid,'device':s.st_dev,'inode':s.st_ino,'mode':'0700','symlink':False}
def marker(file):
 data=Path(file).read_bytes()
 return any(value in data or value.decode().encode('utf-16-le')in data for value in[b'Hexalith.PrivateCaptureCounter',b'CaptureCounters.cs',b'ProjectAdvisoryQueryExecutor.private.cs',b'AdvisoryWaveBootstrap.cs'])
def permitted_roots():
 return [ROOT]+[ROOT/match for match in re.findall(r'^\s*path\s*=\s*(.+)$',(ROOT/'.gitmodules').read_text(),re.M)]
def census():
 result=[]
 for root in permitted_roots():
  for current,dirs,files in os.walk(root):
   dirs[:]=[d for d in dirs if d not in['.git','references','node_modules']]
   for name in files:
    if name not in[ASSEMBLY+'.dll',ASSEMBLY+'.pdb']:continue
    f=Path(current)/name
    if f.is_symlink():raise RuntimeError('SDK carrier symlink refused')
    if not any(part in['bin','obj']for part in f.relative_to(root).parts)or'Debug'not in f.parts:continue
    kind='pdb'if f.suffix=='.pdb'else'refint'if'refint'in f.parts else'ref'if'ref'in f.parts else'full'
    parent=f.parent
    while parent!=root and parent.name not in['bin','obj']:parent=parent.parent
    projectdir=parent.parent if parent.name in['bin','obj']else None
    projects=list(projectdir.glob('*.csproj'))if projectdir else[]
    result.append({'path':str(f.relative_to(ROOT)),'sha256':sha(f),'bytes':f.stat().st_size,'kind':kind,'privateMarker':marker(f),'project':str(projects[0].relative_to(ROOT))if len(projects)==1 else None})
 return result
def ordinary_expected(kind):
 return PRODUCER/('obj/Debug/net10.0/'+kind+'/'+ASSEMBLY+'.dll'if kind in['ref','refint']else'bin/Debug/net10.0/'+ASSEMBLY+('.pdb'if kind=='pdb'else'.dll'))
def ordinary_sdk_proof():
 producer=SDK_PRODUCER/'bin/Debug/net10.0'/(SDK+'.dll');pdb=producer.with_suffix('.pdb');assert sha(producer)==ORDINARY_SDK_SHA and not marker(producer)and not marker(pdb)
 result=[]
 for row in json.loads((PREP/'affected-inventory.json').read_text())['knownServiceDefaultsCopies']:
  copy=ROOT/row['path'];symbols=copy.with_suffix('.pdb');assert sha(copy)==sha(producer)and sha(symbols)==sha(pdb)and not marker(copy)and not marker(symbols)
  result.append({'path':row['path'],'sha256':sha(copy),'pdbSha256':sha(symbols),'ordinaryNoPrivateMarker':True})
 assert len(result)==8
 return {'producerSha256':sha(producer),'producerPdbSha256':sha(pdb),'allEightOrdinarySdkCopiesCoherent':True,'copies':result}
def clean_environment():
 env=os.environ.copy()
 for key in list(env):
  if key in MAPPING or key=='CustomBeforeMicrosoftCommonTargets'or key=='DOTNET_PROCESSOR_COUNT':env.pop(key)
 env.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
 return env
