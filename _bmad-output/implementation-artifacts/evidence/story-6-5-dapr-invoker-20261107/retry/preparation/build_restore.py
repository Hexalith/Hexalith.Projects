from pathlib import Path
import argparse,atexit,datetime,json,subprocess,tempfile
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--phase',choices=['private','ordinary'],required=True);parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
inventory=json.loads((PREP/'affected-inventory.json').read_text());projects=inventory['projects']
plan={'preparationOnly':not args.execute,'phase':args.phase,'sourceChecks':sources(),'privateDirectory':private_directory(),'sevenMappingValuesPreserved':True,'newRuntimeEnvironmentKeys':False,'strategy':'FORCE ten primary projects in Gateway/six-entry/Folders.EventStore/AppHost/Integration order with BuildProjectReferences=false; ordinary phase refreshes only remaining incoherent affected/known Gateway DLL/PDB/ref/refint carriers','projects':projects}
if not args.execute:print(json.dumps(plan,indent=2));raise SystemExit()
operational_pins(args.pins_sha256)
work=Path(tempfile.mkdtemp(prefix='operational-'+args.phase+'-',dir=PREP));before=census();save(work/'before-census.json',before);save(work/'before-native-artifacts.json',native_artifacts());env=clean_environment();env['DOTNET_PROCESSOR_COUNT']='2';env['HexalithBuildsRoot']=str(ROOT/'references/Hexalith.Builds');commands=[];completed=False
if args.phase=='private':
 save(PREP/'latest-private-attempt.json',{'attemptDirectory':str(work),'ordinaryRestoreRequiredEvenIfPrivateBuildFails':True})
 save(work/'ordinary-root-server-before-private.json',ordinary_server_proof('baseline'))
def attempt_end():
 try:save(work/'after-census.json',census());save(work/'after-native-artifacts.json',native_artifacts());save(work/'attempt-closed.json',{'phase':args.phase,'completed':completed,'ordinaryRestoreRequired':args.phase=='private','finallyOwner':'parent-or-reviewed-run-and-restore caller must invoke ordinary phase after every private attempt, including nonzero exit before managed launch'})
 except Exception:pass
atexit.register(attempt_end)
def execute(command,label):
 started=datetime.datetime.now(datetime.timezone.utc).isoformat();log=work/(label+'.log')
 with log.open('wb')as stream:r=subprocess.run(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 commands.append({'command':command,'cwd':str(ROOT),'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':r.returncode,'log':log.name,'logSha256':sha(log)})
 save(work/'commands.json',commands)
 if r.returncode:raise RuntimeError('Bounded explicit build failed; retained exact build log')
mapping=json.loads((PREP/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(path)==digest for path,digest in mapping['files'].items())
selected=mapping['environment'];assert set(selected)==MAPPING and selected['Configuration']=='Debug';env.update(selected)
save(work/'mapping-receipt.json',{'reusedExactPreparedMapping':True,'mappingLogSha256':mapping['mappingLogSha256'],'allEmittedFileHashes':mapping['files'],'emittedEnvironment':selected,'allSevenRetained':True,'noNewToolBuild':True,'buildCpu2Only':True})
hook=['-p:CustomBeforeMicrosoftCommonTargets='+str(PREP/'gateway-live.targets')]if args.phase=='private'else[]
def rebuild(relative,label,refs=True):
 command=['dotnet','build',str(ROOT/relative),'-c','Debug','-t:Rebuild','-m:1','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true',*hook]
 if relative==projects['integration']:command+=['-p:SupportedProjectReadDistributed=true']
 if not refs:command+=['-p:BuildProjectReferences=false']
 execute(command,label)
for index,alias in enumerate(['gateway-producer','eventstore','tenants','projects','projects-workers','folders','folders-workers','apphost','integration']):
 if alias=='apphost':rebuild(inventory['foldersEventStoreProject'],'folders-eventstore-source-consumer-force-refsfalse',False)
 rebuild(projects[alias],str(index+1)+'-'+alias+'-force-no-dependency-build',False)
# Evaluate the same eight properties, with the same selected flags, as the standard managed launcher.
expected=json.loads((PREP/'expected-managed-profile.json').read_text())['compilationProfile'];actual={}
properties=['Configuration','Optimize','DefineConstants','DebugType','UseHexalithProjectReferences','UseNuGetDeps','EnablePrivateCatalogInvocationProof','EnablePrivateOwnerSourceReadProof']
for relative in projects.values():
 command=['dotnet','msbuild',str(ROOT/relative),'-nologo','-p:Configuration=Debug','-p:SupportedProjectReadDistributed=true','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-getProperty:'+','.join(properties)]
 result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True);assert result.returncode==0
 value=json.loads(result.stdout)['Properties'];actual[relative]=value;assert value==expected[relative]
save(work/'evaluated-eight-property-profile.json',{'commandsUseSameEightPropertiesAsManaged':True,'matchesRecordedStandardManagedProfile':True,'profile':actual})
metadata=[]
for alias,enabled,disabled in[
 ('eventstore','CatalogInvocationServiceCollectionExtensions','Private catalog invocation requires an explicitly selected Debug proof build.'),
 ('tenants','OwnerSourceReadServiceCollectionExtensions','Private owner source proof requires the explicitly selected Debug/source host build.')]:
 project=ROOT/projects[alias];binary=project.parent/'bin/Debug/net10.0'/(project.stem+'.dll');data=binary.read_bytes()
 selected=enabled.encode()in data;refusal=disabled.encode('utf-16-le')in data
 metadata.append({'role':alias,'entryAssemblySha256':sha(binary),'enabledMetadataReferencePresent':selected,'disabledGuardLiteralAbsent':not refusal});assert selected and not refusal
save(work/'compiled-proof-profile.json',{'diagnosticOnly':True,'sameExistingManagedProofFlags':True,'entries':metadata})
if args.phase=='ordinary':
 # Always refresh the actual native carrier as well, including DLL/PDB/ref/refint test output and copied private dependency.
 rebuild(inventory['nativeProject'],'native-eventstore-server-tests-ordinary-refsfalse',False)
 # Only outputs carrying THIS captured private helper are owned restoration work.
 # Keep the complete producer/ref/refint/PDB generation coherent, then refresh stale carriers.
 private_work=Path(json.loads((PREP/'latest-private-attempt.json').read_text())['attemptDirectory']);assert private_work.parent==PREP
 private_before=json.loads((private_work/'before-census.json').read_text());private_after=json.loads((private_work/'after-census.json').read_text())if(private_work/'after-census.json').exists()else[];initial={row['path']:row['sha256']for row in private_before}
 affected={row['path']for row in private_after+before if row['privateMarker']or row['sha256']!=initial.get(row['path'])}
 # Every known primary/native Gateway producer/test/integration carrier must match the ordinary Gateway producer,
 # including EventStore.Server.Tests even if its earlier ordinary copy never carried a private marker.
 for row in inventory['knownGatewayCopies']:affected.add(row['path'])
 save(work/'affected-paths-union.json',sorted(affected))
 expected_hashes={kind:sha(ordinary_expected(kind))for kind in['full','pdb','ref','refint']};assert all(not marker(ordinary_expected(kind))for kind in expected_hashes)
 remaining={row['path']:row for row in census()};coordinates={row['path']:row for row in private_before+private_after+before}
 stale=[coordinates[path]for path in affected if path not in remaining or remaining[path]['privateMarker']or remaining[path]['sha256']!=expected_hashes[remaining[path]['kind']]]
 assert all(row['project']for row in stale);todo=sorted({row['project']for row in stale})
 for index,project in enumerate(todo):rebuild(project,'remaining-private-carrier-'+str(index),False)
after=census();save(work/'after-census.json',after);save(work/'after-native-artifacts.json',native_artifacts());sources()
if args.phase=='private':
 producer=PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');assert marker(producer)and marker(producer.with_suffix('.pdb'))
 for alias in['eventstore','tenants','projects','apphost','integration','folders-eventstore']:
  copied=(ROOT/(inventory['foldersEventStoreProject']if alias=='folders-eventstore'else projects[alias])).parent/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');assert copied.exists()and marker(copied)and sha(copied)==sha(producer)and sha(copied.with_suffix('.pdb'))==sha(producer.with_suffix('.pdb'))
else:
 assert not any(row['privateMarker']for row in after)
 assert affected<={row['path']for row in after}
 for row in after:
  if row['path']in affected:assert row['sha256']==expected_hashes[row['kind']]
  elif row['path']in initial:assert row['sha256']==initial[row['path']],'Unrelated preexisting generation changed'
 native_sdk_proof();save(work/'known-native147-marker-absence.json',native147_marker_absence())
 assert not any(row['privateMarker']for row in native_artifacts())
 native_project=(ROOT/inventory['nativeProject']).parent
 native_rows=native_artifacts();assert native_rows
 for row in native_rows:
  file=ROOT/row['path'];kind='pdb'if file.suffix=='.pdb'else'refint'if'refint'in file.parts else'ref'if'ref'in file.parts else'full'
  expected=native_project/('obj/Debug/net10.0/'+kind+'/Hexalith.EventStore.Server.Tests.dll'if kind in['ref','refint']else'bin/Debug/net10.0/Hexalith.EventStore.Server.Tests'+('.pdb'if kind=='pdb'else'.dll'))
  assert sha(file)==sha(expected)
 assert all(sha(ordinary_expected(kind))==digest for kind,digest in expected_hashes.items())
ordinary_domain_proof();sdk_proof=ordinary_sdk_proof();save(work/'ordinary-sdk-proof.json',sdk_proof);save(work/'ordinary-root-server-proof.json',ordinary_server_proof());save(work/'ordinary-domain98-proof.json',ordinary_domain_proof())
save(work/'receipt.json',{'diagnosticOnly':True,'featureAcceptance':False,'phase':args.phase,'completed':True,'source53Match':True,'privateProducerAssembly':ASSEMBLY,'producerSha256':sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll')),'producerPdbSha256':sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.pdb')),'censusCopies':len(after),'allPrivateMarkersAbsent':not any(row['privateMarker']for row in after),'ordinarySdkEightCopiesCoherent':True,'ordinarySdkProducerSha256':sdk_proof['producerSha256'],'privateCliHookOnly':args.phase=='private','sevenMappingValuesRetained':True})
completed=True
print(json.dumps({'phase':args.phase,'completed':True,'censusCopies':len(after)}))
