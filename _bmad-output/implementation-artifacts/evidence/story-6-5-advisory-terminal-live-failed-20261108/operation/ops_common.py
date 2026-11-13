from pathlib import Path
import hashlib,json,os,re,stat
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-advisory-terminal-live-prep-20261108')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
SOURCE_PREP=Path('/tmp/story65-advisory-terminal-prep-20261108')
ASSEMBLY='Hexalith.Projects.Server';PRODUCER=ROOT/'src'/ASSEMBLY
SDK='Hexalith.EventStore.ServiceDefaults';SDK_PRODUCER=ROOT/'references/Hexalith.EventStore/src'/SDK
SDK_SHA='873dbb74ee259d91c99ea2804e5801134c993abb4ef7806e79f242e5d23faf96'
SDK_PDB_SHA='f4fae4fb336e03b5e0636a5781a817598b301f445a967d7b4eb309be29dd639e'
CONTRACTS='Hexalith.EventStore.Contracts'
CONTRACTS_SHA='f1cf82fe57d5ae0037772d3ccea77766a141cd49ec66c2079c3c8b046c8089df'
CONTRACTS_PDB_SHA='bbe424a27b1f242f2d8865864121ea4a99ab40ecee493084df462cf0efd49a39'
ORIGINAL_EXECUTOR_SHA='ac34b0c5c86c8a85ddf8591f73ddb02d32dc3a63db47a206b42688f2c696df8d'
PRIVATE_EXECUTOR_SHA='d36afcb5709e36eb0b8039133947b1fad93df56de0147e1b624842f0e77542fe'
MAPPING={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'}
PRIMARY=['service-defaults-producer','eventstore','tenants','projects','projects-workers','folders','folders-workers','folders-eventstore','apphost','integration']
MARKERS=[b'Hexalith.PrivateCaptureCounter',b'CaptureCounters.cs',b'ProjectAdvisoryQueryExecutor.private.cs',b'AdvisoryTerminalCapture',b'AdvisoryWaveBootstrap.cs',b'PrivateCaptureInstrumentation',b'CaptureCounterBegin',b'CaptureCounterScope',b'PrivateSourceCaptureTiming',b'TimingHexalith',b'PrivateCaptureScope']
def sha(file):
 path=Path(file);assert path.is_file()and not path.is_symlink()
 with path.open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(path,value):
 path=Path(path);path.parent.mkdir(mode=0o700,parents=True,exist_ok=True);temporary=path.with_name(path.name+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');temporary.chmod(0o600);temporary.replace(path)
def operational_pins(expected):
 assert re.fullmatch(r'[0-9a-f]{64}',expected or'')and sha(PREP/'operational-pins.json')==expected
 pins=json.loads((PREP/'operational-pins.json').read_text())
 assert pins['executionReleased']is True and pins['sourceQualificationReady']is True
 for name,digest in pins['files'].items():assert sha(PREP/name)==digest
 for name,digest in pins['externalFiles'].items():assert sha(name)==digest
 sources();return pins

def sources():
 binding=json.loads((PREP/'source-qualification-binding.json').read_text())
 assert sha(binding['qualificationPath'])==binding['qualificationSha256']
 q=json.loads(Path(binding['qualificationPath']).read_text())
 assert q['scratchSourceQualificationPassed']is True
 assert all(q[k]==v for k,v in binding['requiredCounts'].items())
 assert all(q[k]is True for k in ['all733FrozenInputsCurrent','source65AndApp147Current','allGroupsAbsentIncludingZombies','privateDirectorySameAndEmpty'])
 for path,digest in q['bindingsSha256'].items():assert sha(path)==digest # Closed receipt files, not their mutable historical workspace binary maps.
 source=json.loads(Path(binding['currentSourceControls']).read_text())['sourceFilesSha256'];assert len(source)==65
 for name,digest in source.items():
  p=ROOT/name;assert p.resolve().is_relative_to(ROOT)and sha(p)==digest
 prep=json.loads((SOURCE_PREP/'preparation-receipt.json').read_text())
 for name,digest in prep['filesSha256'].items():assert sha(SOURCE_PREP/name)==digest
 assert sha(PRODUCER/'Queries/ProjectAdvisoryQueryExecutor.cs')==ORIGINAL_EXECUTOR_SHA
 assert sha(SOURCE_PREP/'ProjectAdvisoryQueryExecutor.private.cs')==PRIVATE_EXECUTOR_SHA
 return {'source65Match':True,'qualifiedHandoffSha256':binding['qualificationSha256'],'helperSha256':q['helperSha256'],'privateExecutorSha256':q['privateSourceSha256'],'adapterSha256':q['adapterSha256']}

def private_directory(require_empty=False):
 s=PRIVATE.lstat();assert stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)
 assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)
 if require_empty:assert not any(PRIVATE.iterdir())
 return {'uid':s.st_uid,'device':s.st_dev,'inode':s.st_ino,'mode':'0700','symlink':False}
def marker(file):
 data=Path(file).read_bytes();return any(x in data or x.decode().encode('utf-16-le')in data for x in MARKERS)
def permitted_roots():return[ROOT]+[ROOT/x for x in re.findall(r'^\s*path\s*=\s*(.+)$',(ROOT/'.gitmodules').read_text(),re.M)]
def census(assembly=ASSEMBLY):
 result=[]
 for root in permitted_roots():
  for current,dirs,files in os.walk(root):
   dirs[:]=[d for d in dirs if d not in['.git','references','node_modules']]
   for name in files:
    if name not in[assembly+'.dll',assembly+'.pdb']:continue
    f=Path(current)/name
    if not any(x in['bin','obj']for x in f.relative_to(root).parts)or'Debug'not in f.parts:continue
    assert not f.is_symlink()
    kind='pdb'if f.suffix=='.pdb'else'refint'if'refint'in f.parts else'ref'if'ref'in f.parts else'full'
    parent=f.parent
    while parent!=root and parent.name not in['bin','obj']:parent=parent.parent
    projects=list(parent.parent.glob('*.csproj'))if parent.name in['bin','obj']else[]
    result.append({'path':str(f.relative_to(ROOT)),'sha256':sha(f),'kind':kind,'privateMarker':marker(f),'project':str(projects[0].relative_to(ROOT))if len(projects)==1 else None})
 return result

def ordinary_expected(kind):return expected_at(PRODUCER,ASSEMBLY,kind)
def expected_at(producer,assembly,kind):return producer/('obj/Debug/net10.0/'+kind+'/'+assembly+'.dll'if kind in['ref','refint']else'bin/Debug/net10.0/'+assembly+('.pdb'if kind=='pdb'else'.dll'))
def ordinary_sdk_proof():
 inv=json.loads((PREP/'affected-inventory.json').read_text());producer=SDK_PRODUCER/'bin/Debug/net10.0'/(SDK+'.dll');pdb=producer.with_suffix('.pdb')
 assert sha(producer)==SDK_SHA and sha(pdb)==SDK_PDB_SHA and not marker(producer)and not marker(pdb)
 for path,digest in inv['core8Sha256'].items():assert sha(path)==digest and not marker(path)
 copies=[]
 for row in inv['knownServiceDefaultsCopies']:
  copy=ROOT/row['path'];symbols=copy.with_suffix('.pdb');contracts=copy.with_name(CONTRACTS+'.dll')
  assert sha(copy)==SDK_SHA and sha(symbols)==SDK_PDB_SHA and not marker(copy)and not marker(symbols)
  assert sha(contracts)==CONTRACTS_SHA and sha(contracts.with_suffix('.pdb'))==CONTRACTS_PDB_SHA
  copies.append({'path':row['path'],'sha256':sha(copy),'pdbSha256':sha(symbols),'contractsSha256':sha(contracts),'contractsPdbSha256':sha(contracts.with_suffix('.pdb'))})
 sdkrows=census(SDK);old={r['path']:r for r in inv['sdk137']};owned=set(inv['projects'].values())
 for row in sdkrows:
  assert not row['privateMarker']
  if row['path']in old and row['sha256']==old[row['path']]['sha256']:continue
  assert row['project']in owned and row['sha256']==sha(expected_at(SDK_PRODUCER,SDK,row['kind']))
 assert set(old)<={r['path']for r in sdkrows}and len(copies)==8
 return {'producerSha256':SDK_SHA,'producerPdbSha256':SDK_PDB_SHA,'allEightOrdinarySdkAndContractsCopiesCoherent':True,'allSdkCensusMarkerFree':True,'sdkCensusCount':len(sdkrows),'copies':copies}

def independent_dependencies():
 inv=json.loads((PREP/'affected-inventory.json').read_text());owned=set(inv['projects'].values());result={}
 for assembly,group in inv['independentModules'].items():
  old={r['path']:r for r in group['rows']};rows=census(assembly);producer=ROOT/group['producer'];exact=current=0
  for row in rows:
   assert not row['privateMarker']
   if row['path']in old and row['sha256']==old[row['path']]['sha256']:exact+=1
   else:
    assert row['project']in owned and row['sha256']==sha(expected_at(producer,assembly,row['kind']));current+=1
  assert set(old)<={r['path']for r in rows}
  result[assembly]={'censusCount':len(rows),'exactBaseline':exact,'currentOrdinaryProducer':current,'noHistorical10Or17GenerationWaiver':True}
 return result

def app_marker_proof(private):
 inv=json.loads((PREP/'affected-inventory.json').read_text());checked=[]
 for path in list(inv['app147Sha256'])+list(inv['additional2Sha256']):
  p=ROOT/path if not Path(path).is_absolute()else Path(path)
  is_private=marker(p)
  assert not is_private or private and p.name==ASSEMBLY+'.dll'
  if is_private:assert sha(p)==sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll'))
  checked.append(str(p))
 return {'known147':len(inv['app147Sha256']),'additionalMarkerChecked':len(inv['additional2Sha256']),'checked':len(checked),'privateProjectsOnly':private}

def same_start(pid,ticks):
 try:
  f=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return f[0]!='Z'and f[19]==str(ticks)
 except(FileNotFoundError,ProcessLookupError):return False

def group_members(group):
 out=[]
 for p in Path('/proc').iterdir():
  if not p.name.isdigit():continue
  try:
   f=(p/'stat').read_text().rsplit(')',1)[1].split()
   if int(f[2])==group or int(f[3])==group:out.append({'pid':int(p.name),'startTicks':f[19]})
  except(FileNotFoundError,ProcessLookupError):pass
 return out

def clean_environment():
 env=os.environ.copy()
 for key in list(env):
  if key in MAPPING or key=='CustomBeforeMicrosoftCommonTargets'or key=='DOTNET_PROCESSOR_COUNT':env.pop(key)
 env.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0');return env

def verify_runtime(proof):
 for k in ['appHost','cli']:
  assert same_start(proof['ownerSessionIdentities'][k+'Pid'],proof['ownerSessionIdentities'][k+'StartTicks'])
 inv=json.loads((PREP/'affected-inventory.json').read_text());seen=[]
 for row in proof['copies']:
  role=row['role'];project=ROOT/inv['projects'][role];entry=project.parent/'bin/Debug/net10.0'/(project.stem+'.dll')
  assert same_start(row['pid'],row['startTicks'])and same_start(row['wrapperPid'],row['wrapperStartTicks'])
  # Recheck fresh ancestry and the same finite executable shape as the original observer.
  pending=row['pid'];visited=set()
  while pending!=row['wrapperPid']:
   assert pending>1 and pending not in visited and len(visited)<32;visited.add(pending)
   pending=int(Path('/proc',str(pending),'stat').read_text().rsplit(')',1)[1].split()[1])
  exe=Path('/proc',str(row['pid']),'exe').readlink();arguments=Path('/proc',str(row['pid']),'cmdline').read_bytes().split(b'\0')
  assert exe==entry.with_suffix('')or exe==Path('/home/administrator/.dotnet/dotnet')and str(entry).encode()in arguments
  maps={line.split(maxsplit=5)[-1]for line in Path('/proc',str(row['pid']),'maps').read_text().splitlines()if len(line.split(maxsplit=5))==6}
  assert str(entry)in maps and sha(entry)==row['entryAssemblySha256']
  for name,dll,pdb in [(SDK,SDK_SHA,SDK_PDB_SHA),(CONTRACTS,CONTRACTS_SHA,CONTRACTS_PDB_SHA)]:
   f=entry.with_name(name+'.dll');assert str(f)in maps and sha(f)==dll and sha(f.with_suffix('.pdb'))==pdb and not marker(f)
  if role=='projects':
   assert str(ordinary_expected('full'))in maps and sha(entry)==proof['privateProducerSha256']and sha(entry.with_suffix('.pdb'))==proof['privateProducerPdbSha256']and marker(entry)
  assert same_start(row['pid'],row['startTicks'])and same_start(row['wrapperPid'],row['wrapperStartTicks'])
  seen.append(role)
 assert len(seen)==6 and set(seen)=={'eventstore','tenants','projects','projects-workers','folders','folders-workers'}
 for k in ['appHost','cli']:assert same_start(proof['ownerSessionIdentities'][k+'Pid'],proof['ownerSessionIdentities'][k+'StartTicks'])
 return True
