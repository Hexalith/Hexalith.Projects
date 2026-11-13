from pathlib import Path
import hashlib,json,os,re,stat
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-gateway-controller-ops-prep-20261107/live')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
SDK='Hexalith.EventStore.ServiceDefaults'
SDK_PRODUCER=ROOT/'references/Hexalith.EventStore/src'/SDK
ASSEMBLY='Hexalith.EventStore.Gateway'
PRODUCER=ROOT/'references/Hexalith.EventStore/src'/ASSEMBLY
SOURCE_PREP=Path('/tmp/story65-gateway-controller-prep-20261107')
RECEIPT=Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json')
OWNER_SHA='2a031e4d183152beb50b56428805fab3af7b08b787dbad3fc6240ca3dd6e1fb8'
HELPER_SHA='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
ORIGINAL_CAPTURE_SHA='5e85827847578384a7dcca9fe448b97f21cdf4a201263c2d9ec9d0f3eda6be4d'
ORIGINAL_EXECUTOR_SHA='ac34b0c5c86c8a85ddf8591f73ddb02d32dc3a63db47a206b42688f2c696df8d'
PRIVATE_CONTROLLER_SHA='058ba9cfc909cc510f6c382a6b8406a0f04df50b10699db20a0b642cc21eced9'
ORIGINAL_CONTROLLER_SHA='0a7bf260e145136d409a575ab2bc300e179c3350a749039f1589a90e2aaa7597'
ORIGINAL_CLIENT_SHA='c969d284411288c245c5833b15abb73dd93309c2671b08a03de18d694ee33a2c'
BOOTSTRAP_SHA='05bbb6de2e160f5ee87546b2980137434e388c74494f4a111c88a59ec6f85a42'
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
 binding=json.loads((PREP/'source-qualification-binding.json').read_text())
 for path,digest in[(binding['qualifiedReceipt'],binding['qualifiedReceiptSha256']),(binding['parentAudit'],binding['parentAuditSha256']),(binding['preparedReceipt'],binding['preparedReceiptSha256'])]:assert sha(path)==digest
 q=json.loads(Path(binding['qualifiedReceipt']).read_text());assert q['featureAcceptance']is False and q['allPassed']is True
 assert(q['passingBehavioralCells'],q['passingFreshCanaries'],q['controlledProcesses'],q['strictValidatedRows'])==(60,3,19,111653)and len(q['sourceSha256'])==52
 assert q['source52App147AndVariantsBeforeAfterIdentical']is True and q['shared129InputsUnchanged']is True
 assert q['native40Executed']is False and q['hostsStarted']==0
 assert q['preparationReceiptSha256']==binding['preparedReceiptSha256']
 assert all(sha(PREP/name)==digest for name,digest in pins['files'].items())
 assert all(sha(path)==digest for path,digest in pins.get('externalFiles',{}).items())
 return pins
def sources():
 assert sha(RECEIPT)==OWNER_SHA and sha(SOURCE_PREP/'CaptureCounters.cs')==HELPER_SHA
 owner=json.loads(RECEIPT.read_text());assert len(owner['sourceSha256'])==48
 for relative,digest in owner['sourceSha256'].items():assert sha(ROOT/'references/Hexalith.EventStore'/relative)==digest
 assert sha(SDK_PRODUCER/'Routing/DevelopmentRouteCatalogInputs.cs')==ORIGINAL_CAPTURE_SHA
 assert sha(ROOT/'src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs')==ORIGINAL_EXECUTOR_SHA
 assert sha(ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DelegatedOwnerQueryClient.cs')==ORIGINAL_CLIENT_SHA
 assert sha(ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/QueriesController.cs')==ORIGINAL_CONTROLLER_SHA
 assert sha(SOURCE_PREP/'QueriesController.private.cs')==PRIVATE_CONTROLLER_SHA
 assert sha(SOURCE_PREP/'GatewayControllerBootstrap.cs')==BOOTSTRAP_SHA
 return {'ownerSource48Match':True,'extraCaptureSourceMatch':True,'ordinaryExecutorExtraMatch':True,'ordinaryDelegatedClientExtraMatch':True,'sourceCount':52,'ordinaryControllerExtraMatch':True,'helperSha256':HELPER_SHA,'privateControllerSha256':PRIVATE_CONTROLLER_SHA,'firstEntryBootstrapSha256':BOOTSTRAP_SHA}
def private_directory(require_empty=False):
 pin=json.loads((PREP/'private-directory-proof.json').read_text());s=PRIVATE.lstat()
 assert stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)and stat.S_IMODE(s.st_mode)==0o700 and s.st_uid==pin['uid']and s.st_dev==pin['device']and s.st_ino==pin['inode']
 if require_empty:assert not any(PRIVATE.iterdir())
 return {'uid':s.st_uid,'device':s.st_dev,'inode':s.st_ino,'mode':'0700','symlink':False}
def marker(file):
 data=Path(file).read_bytes()
 return any(value in data or value.decode().encode('utf-16-le')in data for value in[b'Hexalith.PrivateCaptureCounter',b'CaptureCounters.cs',b'QueriesController.private.cs',b'ProjectAdvisoryQueryExecutor.private.cs',b'GatewayControllerBootstrap.cs'])
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
    if f.is_symlink():raise RuntimeError('Gateway carrier symlink refused')
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

def native_artifacts():
 project=ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests'
 rows=[]
 for folder in ['bin/Debug/net10.0','obj/Debug/net10.0']:
  base=project/folder
  if not base.exists():continue
  for file in base.rglob('*'):
   if file.is_file()and file.name in ['Hexalith.EventStore.Server.Tests.dll','Hexalith.EventStore.Server.Tests.pdb']:
    assert not file.is_symlink();rows.append({'path':str(file.relative_to(ROOT)),'sha256':sha(file),'privateMarker':marker(file)})
 return rows

def native_sdk_proof():
 result=ordinary_sdk_proof()
 copy=ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0'/(SDK+'.dll')
 producer=SDK_PRODUCER/'bin/Debug/net10.0'/(SDK+'.dll')
 assert sha(copy)==sha(producer)and sha(copy.with_suffix('.pdb'))==sha(producer.with_suffix('.pdb'))and not marker(copy)and not marker(copy.with_suffix('.pdb'))
 return {'knownEight':result,'nativeCopySha256':sha(copy),'nativeCopyPdbSha256':sha(copy.with_suffix('.pdb')),'nativeCopyOrdinary':True}

def ordinary_server_proof(mode='rebuilt-source'):
 assert mode in ['baseline','rebuilt-source']
 source=ROOT/'src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs';assert sha(source)==ORIGINAL_EXECUTOR_SHA
 server=ROOT/'src/Hexalith.Projects.Server';name='Hexalith.Projects.Server'
 expected={kind:server/('obj/Debug/net10.0/'+kind+'/'+name+'.dll'if kind in ['ref','refint']else'bin/Debug/net10.0/'+name+('.pdb'if kind=='pdb'else'.dll'))for kind in ['full','pdb','ref','refint']}
 expected_hash={kind:sha(file)for kind,file in expected.items()};rows=[]
 for entry in json.loads((PREP/'affected-inventory.json').read_text())['knownOrdinaryRootServerCopies']:
  copy=ROOT/entry['path'];assert copy.exists()and not marker(copy)
  untouched=entry['project']=='tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj'
  required=entry['sha256']if mode=='baseline'or untouched else expected_hash[entry['kind']]
  assert sha(copy)==required
  rows.append({'path':entry['path'],'kind':entry['kind'],'sha256':sha(copy),'baselineSha256':entry['sha256'],'privateMarkerAbsent':True,'coherenceScope':'untouched-baseline-test-copy'if untouched else'baseline'if mode=='baseline'else'current-rebuilt-source-generation'})
 assert len(rows)==10 and all(not marker(file)for file in expected.values())
 return {'mode':mode,'ordinaryServerSourceSha256':ORIGINAL_EXECUTOR_SHA,'priorBaselineProducerSha256':'39bb2ca3dfb51e1f62eb6eae79c0ded11eff6a4556dfa112ec58f131bcf30c73','priorBaselinePdbSha256':'83690e9480051111f852141ca10335bc8fdf030da393e8c621501759145f98be','currentProducerSha256':expected_hash['full'],'currentProducerPdbSha256':expected_hash['pdb'],'allTenPrivateMarkersAbsent':True,'rebuiltKnownOutputsCoherentWithCurrentProducer':True,'untouchedRootServerTestsRetainPriorHashes':True,'allTenNaturallyMatchPriorBaseline':all(row['sha256']==row['baselineSha256']for row in rows),'allTenSameGenerationClaimed':False,'copies':rows}

def native147_marker_absence():
 paths=json.loads((PREP/'affected-inventory.json').read_text())['knownNative147AssemblyPaths'];assert len(paths)==147
 for path in paths:assert (ROOT/path).is_file()and not marker(ROOT/path)
 return {'knownNative147AssemblyCopiesPrivateMarkersAbsent':True,'count':len(paths),'byteEqualityWithHistoricalNativeGenerationNotClaimed':True}

def ordinary_domain_proof():
 rows=json.loads((PREP/'affected-inventory.json').read_text())['ordinaryDomain98Baseline']
 assert len(rows)==98
 for row in rows:assert sha(ROOT/row['path'])==row['sha256']and not marker(ROOT/row['path'])
 return {'all98DomainCopiesRetainRecordedOrdinaryHashes':True,'count':98,'notClaimedAll98SameGeneration':True,'activeOrdinaryProducerSha256':'1c8dfbeec54a63ad59921f22aaf2826f89f2b2dcb2f2db54d4c4a403f1fd0178'}
