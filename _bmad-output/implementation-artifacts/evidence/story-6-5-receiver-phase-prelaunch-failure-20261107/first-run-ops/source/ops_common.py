from pathlib import Path
import hashlib,json,os,re,stat
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-receiver-phase-ops-prep-20261107')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
SDK='Hexalith.EventStore.ServiceDefaults'
SDK_PRODUCER=ROOT/'references/Hexalith.EventStore/src'/SDK
ASSEMBLY=SDK
PRODUCER=ROOT/'references/Hexalith.EventStore/src'/ASSEMBLY
SOURCE_PREP=Path('/tmp/story65-receiver-phase-prep-20261107')
HELPER_SHA='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
PRIVATE_HANDLER_SHA='f426ca5a31a53b26d92ed218151d1d2aa1eab4dc64b6a2f0cd35da142c0eaed1'
BOOTSTRAP_SHA='ff22ad6362bf4f7dea2ffaa782066dc719f1fcbf6e9205e50ee3df9df30df495'
MAPPING={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'}
def sha(file):
 with Path(file).open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(path,value):
 path=Path(path);path.parent.mkdir(mode=0o700,parents=True,exist_ok=True);temporary=path.with_name(path.name+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');temporary.chmod(0o600);temporary.replace(path)
def operational_pins(expected):
 assert re.fullmatch(r'[0-9a-f]{64}',expected or'')and sha(PREP/'operational-pins.json')==expected
 pins=json.loads((PREP/'operational-pins.json').read_text());assert pins['sourceQualificationReady']is True and pins['executionReleased']is True
 binding=json.loads((PREP/'source-qualification-binding.json').read_text());assert binding['sourceQualificationReady']is True and binding['nativeSelectionReady']is True
 for key in ['qualifiedReceipt','parentAudit','preparedReceipt','qualifiedProducerReceipt','qualifiedProducerParentAudit','runtimeManifest']:
  assert binding[key]and re.fullmatch(r'[0-9a-f]{64}',binding[key+'Sha256'])and sha(binding[key])==binding[key+'Sha256']
 q=json.loads(Path(binding['qualifiedReceipt']).read_text());assert q['qualified']is True and q['featureAcceptance']is False
 # This nonempty exact map is populated ONLY from the new actual reviewed receiver aggregate.
 expected_fields=binding['expectedAggregateFields'];assert expected_fields
 assert all(q[key]==value for key,value in expected_fields.items())
 assert q['coreSha256']==PRIVATE_HANDLER_SHA and q['helperSha256']==HELPER_SHA and q['bootstrapSha256']==BOOTSTRAP_SHA
 assert len(q['sourceSha256'])==56
 producer=json.loads(Path(binding['qualifiedProducerReceipt']).read_text());assert binding['expectedProducerFields']
 assert all(producer[key]==value for key,value in binding['expectedProducerFields'].items())
 inventory=json.loads((PREP/'affected-inventory.json').read_text());assert inventory['baselineReady']is True
 assert inventory['ordinarySdkSha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
 assert inventory['ordinarySdkPdbSha256']=='ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
 reference=json.loads((PREP/'native-reference-case-set.json').read_text());assert reference['metadataPreparationComplete']is True and reference['expectedTotal']==47
 assert sha(PREP/'native-reference-case-set.json')==binding['immutableNativeReferenceSha256']
 assert all(sha(PREP/name)==digest for name,digest in pins['files'].items())
 assert all(sha(path)==digest for path,digest in pins['externalFiles'].items())
 return pins

def sources():
 assert sha(SOURCE_PREP/'CaptureCounters.cs')==HELPER_SHA
 inventory=json.loads((PREP/'affected-inventory.json').read_text());assert inventory['baselineReady']is True
 projects=inventory['projectInputsSha256'];assert len(projects)==14 and all(sha(ROOT/rel)==digest for rel,digest in projects.items())
 metadata=json.loads((SOURCE_PREP/'preparation-receipt.json').read_text());assert len(metadata['sourceSha256'])==56
 for relative,digest in metadata['sourceSha256'].items():assert sha(ROOT/relative)==digest
 assert sha(PRODUCER/'Routing/TechnicalHttpRouteCatalogReceiver.cs')=='71a9dc98cedde0a3f1651f3ec06b9812416b91e0a62595914a2fb7014ccf8c94'
 assert sha(SOURCE_PREP/'TechnicalHttpRouteCatalogReceiver.private.cs')==PRIVATE_HANDLER_SHA
 assert sha(SOURCE_PREP/'ReceiverPhaseBootstrap.cs')==BOOTSTRAP_SHA
 normalized=json.loads((SOURCE_PREP/'source-normalization.json').read_text())
 assert normalized['normalizationByteExact']is True and normalized['stage5RepeatedIntervalsAreLastWinsNotSum']is True
 return {'sourceCount':56,'source56Match':True,'privateReceiverSha256':PRIVATE_HANDLER_SHA,'helperSha256':HELPER_SHA,'entryGateBootstrapSha256':BOOTSTRAP_SHA,'stage5LastIntervalNotTotal':True,'noEntrySetterOrNewRuntimeConfiguration':True}

def private_directory(require_empty=False):
 pin=json.loads((PREP/'private-directory-proof.json').read_text());s=PRIVATE.lstat()
 assert stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)and stat.S_IMODE(s.st_mode)==0o700 and s.st_uid==pin['uid']and s.st_dev==pin['device']and s.st_ino==pin['inode']
 if require_empty:assert not any(PRIVATE.iterdir())
 return {'uid':s.st_uid,'device':s.st_dev,'inode':s.st_ino,'mode':'0700','symlink':False}
def marker(file):
 data=Path(file).read_bytes()
 return any(value in data or value.decode().encode('utf-16-le')in data for value in[b'Hexalith.PrivateCaptureCounter',b'CaptureCounters.cs',b'QueriesController.private.cs',b'DaprDomainQueryInvoker.private.cs',b'DomainServiceWorkloadAssertionHandler.private.cs',b'ProjectAdvisoryQueryExecutor.private.cs',b'GatewayControllerBootstrap.cs',b'InvocationTransportBootstrap',b'RouteCatalogInvocationHandler.private.cs',b'ReceiverPhaseBootstrap',b'TechnicalHttpRouteCatalogReceiver.private.cs'])
def permitted_roots():
 return [ROOT]+[ROOT/match for match in re.findall(r'^\s*path\s*=\s*(.+)$',(ROOT/'.gitmodules').read_text(),re.M)]
def census(assembly=ASSEMBLY):
 result=[]
 for root in permitted_roots():
  for current,dirs,files in os.walk(root):
   dirs[:]=[d for d in dirs if d not in['.git','references','node_modules']]
   for name in files:
    if name not in[assembly+'.dll',assembly+'.pdb']:continue
    f=Path(current)/name
    if f.is_symlink():raise RuntimeError('EventStore carrier symlink refused')
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


def module_expected(name,kind):
 producer=ROOT/('src/'+name if name=='Hexalith.Projects.Server'else'references/Hexalith.EventStore/src/'+name)
 return producer/('obj/Debug/net10.0/'+kind+'/'+name+'.dll'if kind in['ref','refint']else'bin/Debug/net10.0/'+name+('.pdb'if kind=='pdb'else'.dll'))

def sdk_proof(mode='private-with-native'):
 assert mode in['baseline','private-primary','private-with-native','ordinary']
 inventory=json.loads((PREP/'affected-inventory.json').read_text());baseline={r['path']:r for r in inventory['allSdkBaseline']};known={r['path']for r in inventory['knownSdkCopies']};actual=census();current={k:sha(ordinary_expected(k))for k in['full','pdb','ref','refint']};private=mode.startswith('private');copies=[];owned=set(inventory['projects'].values())|{inventory['nativeProject']}
 assert all(marker(ordinary_expected(k))==private for k in current)
 for row in actual:
  native=row['project']==inventory['nativeProject'];owned_path=row['project']in owned and '/legacy-sdk-'not in row['path'];active=owned_path and mode!='baseline'and(mode!='private-primary'or not native)
  assert row['path']in baseline or active,'New SDK carrier is outside exact owned project scope'
  required=current[row['kind']]if active else baseline[row['path']]['sha256']
  assert row['sha256']==required and row['privateMarker']==(private and active)
  copies.append({**row,'coherenceScope':'current-private-sdk'if private and active else'current-ordinary-sdk'if active else'untouched-recorded-baseline'})
 assert set(baseline)<={x['path']for x in actual}
 for entry in inventory['knownServiceDefaultsCopies']:
  path=entry['path'];native=path.startswith('references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/');active=mode!='baseline'and(mode!='private-primary'or not native)
  file=ROOT/path;assert sha(file)==(current['full']if active else entry['sha256'])and sha(file.with_suffix('.pdb'))==(current['pdb']if active else entry['pdbSha256'])
 return{'mode':mode,'producerSha256':current['full'],'producerPdbSha256':current['pdb'],'sdkCensusCopies':len(actual),'knownSdk8ScopedCoherent':True,'allEightCurrentProducer':mode in['private-with-native','ordinary'],'noHistoricalProducerByteEqualityClaimed':True,'copies':copies}

def dependency_proof(name,mode='rebuilt-with-native'):
 assert mode in['baseline','rebuilt-primary','rebuilt-with-native']
 key={'Hexalith.EventStore.DomainService':'ordinaryDomain98Baseline','Hexalith.EventStore.Server':'allServerBaseline','Hexalith.EventStore.Gateway':'allGatewayBaseline','Hexalith.Projects.Server':'knownOrdinaryRootServerCopies'}[name]
 inventory=json.loads((PREP/'affected-inventory.json').read_text());baseline={r['path']:r for r in inventory[key]};owned=set(inventory['projects'].values())|{inventory['nativeProject']};current={k:sha(module_expected(name,k))for k in['full','pdb','ref','refint']};actual=census(name);rows=[]
 assert all(not marker(module_expected(name,k))for k in current)
 for row in actual:
  native=row['project']==inventory['nativeProject'];active=row['project']in owned and mode!='baseline'and(mode!='rebuilt-primary'or not native)
  assert row['path']in baseline or active,'New dependency carrier is outside exact owned project scope'
  required=current[row['kind']]if active else baseline[row['path']]['sha256'];assert row['sha256']==required and not row['privateMarker']
  rows.append({**row,'coherenceScope':'current-rebuilt-ordinary-source'if active else'untouched-baseline'})
 assert set(baseline)<={row['path']for row in actual}
 return{'assembly':name,'mode':mode,'currentProducerSha256':current['full'],'currentProducerPdbSha256':current['pdb'],'sourceUnchangedNoPrivateMarker':True,'allBaselinePathsPresent':True,'notAllCopiesSameGenerationOrHistoricalByteEqualityClaimed':True,'copies':rows}

def independent_dependencies(mode='rebuilt-with-native'):
 return{n:dependency_proof(n,mode)for n in['Hexalith.EventStore.DomainService','Hexalith.EventStore.Server','Hexalith.EventStore.Gateway','Hexalith.Projects.Server']}

def native147_proof(mode="ordinary"):
 private=mode.startswith("private")
 inventory=json.loads((PREP/'affected-inventory.json').read_text());paths=inventory['knownNative147AssemblyPaths'];known={r['path']for r in inventory['knownSdkCopies']};assert len(paths)==147
 for path in paths:
  file=ROOT/path;assert file.is_file()
  native=path.startswith('references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/')
  assert marker(file)==(private and path in known and file.name==SDK+'.dll'and(mode!='private-primary'or not native))
 return{'count':147,'privateMarkersRestrictedToKnownSdkCarriers':private,'allPrivateMarkersAbsent':not private,'noHistoricalByteEqualityClaimed':True}

def entry_metadata():
 import subprocess
 proof=json.loads((PREP/'entry-metadata-contract.json').read_text());reader=proof['metadataReader'];assert reader['qualified']is True and sha(reader['path'])==reader['sha256'];rows=[]
 for source in proof['entries']:
  file=Path(source['entryDllPath']);before=sha(file)
  result=subprocess.run([reader['path'],'library',str(file),'--json'],capture_output=True,text=True);assert result.returncode==0
  actual=json.loads(result.stdout)['assembly_info']['assembly_name'];assert actual==source['metadataAssemblySimpleName']and sha(file)==before
  expected={'Hexalith.Projects.Server':'projects','Hexalith.Tenants':'tenants','Hexalith.Folders.Server':'folders'}.get(actual)
  rows.append({'role':source['role'],'entryDllPath':str(file),'entryDllSha256':before,'metadataAssemblySimpleName':actual,'eligibleUnderFreshLiteralNameGate':expected==source['role'],'selectedFixedCounterRole':expected})
 assert len(rows)==6 and {r['role']for r in rows if r['eligibleUnderFreshLiteralNameGate']}=={'projects','tenants','folders'}
 return{'metadataOnlyNoApplicationAssemblyLoad':True,'readerSha256':reader['sha256'],'onlyThreeReceiverEntriesMatch':True,'gatewayAndTwoWorkersExcluded':True,'excludedRuntimeHelperStatesNotInspected':True,'entries':rows}

def same_start(pid,ticks):
 try:
  fields=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return fields[0]!='Z'and fields[19]==str(ticks)
 except(FileNotFoundError,ProcessLookupError):return False
