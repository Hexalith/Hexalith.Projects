from pathlib import Path
import hashlib,json,os,re,stat
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path('/tmp/story65-invocation-transport-ops-prep-20261107/live')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
SDK='Hexalith.EventStore.ServiceDefaults'
SDK_PRODUCER=ROOT/'references/Hexalith.EventStore/src'/SDK
ASSEMBLY=SDK
PRODUCER=ROOT/'references/Hexalith.EventStore/src'/ASSEMBLY
SOURCE_PREP=Path('/tmp/story65-invocation-transport-prep-20261107')
HELPER_SHA='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
PRIVATE_HANDLER_SHA='26c6a0312ba2cd47b086a7f3fbaf8bf77452bdfb34e8e189f33456a10f5b6918'
BOOTSTRAP_SHA='40eb2532822ec0b759a69ec159926d4f9a87ad5a059ef23944f585107b3485f3'
MAPPING={'CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'}
def sha(file):
 with Path(file).open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(path,value):
 path=Path(path);path.parent.mkdir(mode=0o700,parents=True,exist_ok=True);temporary=path.with_name(path.name+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');temporary.chmod(0o600);temporary.replace(path)
def operational_pins(expected):
 assert re.fullmatch(r'[0-9a-f]{64}',expected or'')and sha(PREP/'operational-pins.json')==expected
 pins=json.loads((PREP/'operational-pins.json').read_text());assert pins['sourceQualificationReady']is True
 binding=json.loads((PREP/'source-qualification-binding.json').read_text())
 for key in ['qualifiedReceipt','parentAudit','preparedReceipt']:assert sha(binding[key])==binding[key+'Sha256']
 q=json.loads(Path(binding['qualifiedReceipt']).read_text());assert q['featureAcceptance']is False and q['qualified']is True
 # Final actual aggregate field guards are bound by the separately reviewed qualification receipt.
 assert(q['passingBehavioralCells'],q['passingFreshCanaries'],q['controlledProcesses'])==(80,3,16)
 assert q['strictValidatedRows']==83662 and len(q['sourceSha256'])==55
 assert q['actualSendAsyncEntriesPerVariantAcrossFourModes']==84 and q['actualEntryCountsAreNotAdditionalCaseComparisons']is True
 assert(q['eligibleControlledProcesses'],q['genuineExcludedProcesses'],q['excludedBehavioralCases'],q['excludedActualSendAsyncEntriesPerVariant'])==(15,1,20,21)
 assert q['perProcessDistinctInputPaths']==675 and q['all408UnionExternalInputsCheckedPerProcess']is True
 assert q['allCountAndAllocationColumnsZero']is True and q['activeExactPerEntryMasksQualified']is True
 assert q['source55App147AndVariantsBeforeAfterIdentical']is True and q['all61PreparedFilesUnchangedDuringExecution']is True and q['shared359InputsUnchanged']is True
 assert q['allActualOwnedPidsAndGroupsAbsent']is True and q['fixedPinnedPrivateDirectoryEmpty']is True
 assert q['excludedEntryLivedFullWindowAndClosureOpportunity']is True and q['excludedArtifactAbsenceObservedWithoutHelperReflection']is True
 assert q['excludedDoesNotClaimNoCctorOrWorkerExecutionOrFiveRealHostsExecuted']is True
 assert q['coreSha256']==PRIVATE_HANDLER_SHA and q['helperSha256']==HELPER_SHA and q['bootstrapSha256']==BOOTSTRAP_SHA
 assert q['apiSha256']=='41cce04f1e2d6e97e840980ed03713e20d751ec5665b6349045999ceea410cf9'
 assert q['native33Executed']is False and q['applicationBuilds']==q['hostsStarted']==0
 assert q['preparationReceiptSha256']==binding['preparedReceiptSha256']
 assert all(sha(PREP/name)==digest for name,digest in pins['files'].items())
 assert all(sha(path)==digest for path,digest in pins['externalFiles'].items())
 return pins

def sources():
 assert sha(SOURCE_PREP/'CaptureCounters.cs')==HELPER_SHA
 projects=json.loads((PREP/'affected-inventory.json').read_text())['projectInputsSha256'];assert len(projects)==14 and all(sha(ROOT/rel)==digest for rel,digest in projects.items())
 metadata=json.loads((SOURCE_PREP/'preparation-receipt.json').read_text());assert len(metadata['sourceSha256'])==55
 for relative,digest in metadata['sourceSha256'].items():assert sha(ROOT/relative)==digest
 assert sha(PRODUCER/'Routing/RouteCatalogInvocationHandler.cs')=='a59c8093be4a2a112eb64b628cf243a343cb3ad836e3fff2f8dd0fe9450c4854'
 assert sha(SOURCE_PREP/'RouteCatalogInvocationHandler.private.cs')==PRIVATE_HANDLER_SHA
 assert sha(SOURCE_PREP/'InvocationTransportBootstrap.cs')==BOOTSTRAP_SHA
 return {'sourceCount':55,'source55Match':True,'privateInvocationHandlerSha256':PRIVATE_HANDLER_SHA,'helperSha256':HELPER_SHA,'entryGateBootstrapSha256':BOOTSTRAP_SHA,'noEntrySetterOrNewRuntimeConfiguration':True}
def private_directory(require_empty=False):
 pin=json.loads((PREP/'private-directory-proof.json').read_text());s=PRIVATE.lstat()
 assert stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)and stat.S_IMODE(s.st_mode)==0o700 and s.st_uid==pin['uid']and s.st_dev==pin['device']and s.st_ino==pin['inode']
 if require_empty:assert not any(PRIVATE.iterdir())
 return {'uid':s.st_uid,'device':s.st_dev,'inode':s.st_ino,'mode':'0700','symlink':False}
def marker(file):
 data=Path(file).read_bytes()
 return any(value in data or value.decode().encode('utf-16-le')in data for value in[b'Hexalith.PrivateCaptureCounter',b'CaptureCounters.cs',b'QueriesController.private.cs',b'DaprDomainQueryInvoker.private.cs',b'DomainServiceWorkloadAssertionHandler.private.cs',b'ProjectAdvisoryQueryExecutor.private.cs',b'GatewayControllerBootstrap.cs',b'InvocationTransportBootstrap',b'RouteCatalogInvocationHandler.private.cs'])
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
 proof=json.loads((SOURCE_PREP/'story65-invocation-entry-selection-proof-20261107.json').read_text());reader=proof['metadataReader'];assert sha(reader['path'])==reader['sha256'];rows=[]
 for source in proof['entries']:
  file=Path(source['entryDllPath']);before=sha(file)
  result=subprocess.run([reader['path'],'library',str(file),'--json'],capture_output=True,text=True);assert result.returncode==0
  actual=json.loads(result.stdout)['assembly_info']['assembly_name'];assert actual==source['metadataAssemblySimpleName']and sha(file)==before
  rows.append({'role':source['role'],'entryDllPath':str(file),'entryDllSha256':before,'metadataAssemblySimpleName':actual,'eligibleUnderFreshLiteralNameGate':actual=='Hexalith.EventStore'})
 assert len(rows)==6 and [r['role']for r in rows if r['eligibleUnderFreshLiteralNameGate']]==['eventstore']
 return{'metadataOnlyNoApplicationAssemblyLoad':True,'readerSha256':reader['sha256'],'onlyEventStoreEntryMatches':True,'otherFiveSourceSelectedEntriesDoNotMatch':True,'notFiveActualRuntimeHelperStateInspections':True,'entries':rows}

def same_start(pid,ticks):
 try:
  fields=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return fields[0]!='Z'and fields[19]==str(ticks)
 except(FileNotFoundError,ProcessLookupError):return False
