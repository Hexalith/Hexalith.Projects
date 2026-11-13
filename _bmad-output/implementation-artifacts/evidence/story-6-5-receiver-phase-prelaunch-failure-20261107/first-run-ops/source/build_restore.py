from pathlib import Path
import argparse,atexit,datetime,json,subprocess,tempfile
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--phase',choices=['private','ordinary'],required=True);parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
inventory=json.loads((PREP/'affected-inventory.json').read_text());projects=inventory['projects'];order=inventory['projectOrder']
plan={'preparationOnly':not args.execute,'phase':args.phase,'sourceChecks':'required before execution; pending qualification'if not args.execute else sources(),'privateDirectory':'fresh expected identity/empty guard only at execution'if not args.execute else private_directory(),'primaryBuilds':13,'ordinaryMinimumBuildsIncludingNative':14,'sevenMappingValuesPreserved':True,'newRuntimeEnvironmentKeys':False,'strategy':'FORCE SDK then DomainService/Server/Gateway/six entries/inactive Folders.EventStore/AppHost/Integration, refsfalse. Ordinary also native Server.Tests and remaining stale SDK union. Dependency generations independently scoped.','projects':projects}
if not args.execute:print(json.dumps(plan,indent=2));raise SystemExit()
operational_pins(args.pins_sha256)
work=Path(tempfile.mkdtemp(prefix='operational-'+args.phase+'-',dir=PREP));before=census();save(work/'before-census.json',before);save(work/'before-native-artifacts.json',native_artifacts());env=clean_environment();env.update(DOTNET_PROCESSOR_COUNT='2',HexalithBuildsRoot=str(ROOT/'references/Hexalith.Builds'));commands=[];completed=False
if args.phase=='private':
 save(PREP/'latest-private-attempt.json',{'attemptDirectory':str(work),'ordinaryRestoreRequiredEvenIfPrivateBuildFails':True})
 save(work/'ordinary-sdk-before-private.json',sdk_proof('baseline'));save(work/'independent-dependencies-before-private.json',independent_dependencies('baseline'))
def attempt_end():
 try:save(work/'after-census.json',census());save(work/'after-native-artifacts.json',native_artifacts());save(work/'attempt-closed.json',{'phase':args.phase,'completed':completed,'ordinaryRestoreRequired':args.phase=='private','finallyOwner':'reviewed orchestrator/parent must restore after EVERY partial private attempt once exact runtime/native absence is proven'})
 except Exception:pass
atexit.register(attempt_end)
def execute(command,label):
 started=datetime.datetime.now(datetime.timezone.utc).isoformat();log=work/(label+'.log')
 with log.open('wb')as stream:result=subprocess.run(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 commands.append({'command':command,'cwd':str(ROOT),'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':result.returncode,'log':log.name,'logSha256':sha(log)});save(work/'commands.json',commands)
 if result.returncode:raise RuntimeError('Explicit source build failed; exact log retained')
mapping=json.loads((PREP/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(n)==h for n,h in mapping['files'].items());assert set(mapping['environment'])==MAPPING;env.update(mapping['environment'])
save(work/'mapping-receipt.json',{'allSevenEmittedValuesRetained':True,'emittedEnvironment':mapping['environment'],'mappingLogSha256':mapping['mappingLogSha256'],'allEmittedFileHashes':mapping['files'],'noNewToolBuild':True,'buildCpu2Only':True})
hook=['-p:CustomBeforeMicrosoftCommonTargets='+str(PREP/'sdk-live.targets')]if args.phase=='private'else[]
def rebuild(relative,label):
 command=['dotnet','build',str(ROOT/relative),'-c','Debug','-t:Rebuild','-m:1','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-p:BuildProjectReferences=false',*hook]
 if relative==projects['integration']:command+=['-p:SupportedProjectReadDistributed=true']
 execute(command,label)
for index,alias in enumerate(order,1):rebuild(projects[alias],str(index)+'-'+alias+'-force-refsfalse')
properties=['Configuration','Optimize','DefineConstants','DebugType','UseHexalithProjectReferences','UseNuGetDeps','EnablePrivateCatalogInvocationProof','EnablePrivateOwnerSourceReadProof'];expected=json.loads((PREP/'expected-managed-profile.json').read_text())['compilationProfile'];actual={}
for relative in projects.values():
 command=['dotnet','msbuild',str(ROOT/relative),'-nologo','-p:Configuration=Debug','-p:SupportedProjectReadDistributed=true','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-getProperty:'+','.join(properties)]
 result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True);assert result.returncode==0;value=json.loads(result.stdout)['Properties'];actual[relative]=value;assert value==expected[relative]
save(work/'evaluated-eight-property-profile.json',{'sameExistingManagedFlags':True,'all13PrimaryExpectedProfilesMatch':True,'profile':actual})
metadata=[]
for alias,enabled,disabled in [('eventstore','CatalogInvocationServiceCollectionExtensions','Private catalog invocation requires an explicitly selected Debug proof build.'),('tenants','OwnerSourceReadServiceCollectionExtensions','Private owner source proof requires the explicitly selected Debug/source host build.')]:
 project=ROOT/projects[alias];binary=project.parent/'bin/Debug/net10.0'/(project.stem+'.dll');data=binary.read_bytes();present=enabled.encode()in data;absent=disabled.encode('utf-16-le')not in data;assert present and absent;metadata.append({'role':alias,'entryAssemblySha256':sha(binary),'enabledMetadataReferencePresent':present,'disabledGuardLiteralAbsent':absent})
save(work/'compiled-proof-profile.json',{'diagnosticOnly':True,'sameExistingManagedProofFlags':True,'entries':metadata})
if args.phase=='ordinary':
 rebuild(inventory['nativeProject'],'14-native-eventstore-server-tests-ordinary-refsfalse')
 private_work=Path(json.loads((PREP/'latest-private-attempt.json').read_text())['attemptDirectory']);assert private_work.parent==PREP
 private_before=json.loads((private_work/'before-census.json').read_text());private_after=json.loads((private_work/'after-census.json').read_text())if(private_work/'after-census.json').exists()else[];initial={r['path']:r['sha256']for r in private_before}
 after_primary=census();save(work/'after-primary-census.json',after_primary)
 affected={r['path']for r in private_after+before+after_primary if r['privateMarker']or r['sha256']!=initial.get(r['path'])};affected.update(r['path']for r in inventory['knownSdkCopies']);save(work/'affected-paths-union.json',sorted(affected))
 expected_hashes={k:sha(ordinary_expected(k))for k in['full','pdb','ref','refint']};assert all(not marker(ordinary_expected(k))for k in expected_hashes)
 actual_rows={r['path']:r for r in census()};coordinates={r['path']:r for r in private_before+private_after+before+after_primary}
 stale=[coordinates[path]for path in affected if path not in actual_rows or actual_rows[path]['privateMarker']or actual_rows[path]['sha256']!=expected_hashes[actual_rows[path]['kind']]];assert all(r['project']for r in stale)
 for index,project in enumerate(sorted({r['project']for r in stale})):rebuild(project,'remaining-sdk-carrier-'+str(index))
after=census();save(work/'after-census.json',after);save(work/'after-native-artifacts.json',native_artifacts());sources()
if args.phase=='ordinary':
 affected.update(r['path']for r in after if r['privateMarker']or r['sha256']!=initial.get(r['path']));save(work/'affected-paths-union.json',sorted(affected))
 assert not any(r['privateMarker']for r in after)and affected<={r['path']for r in after}
 for row in after:
  if row['path']in affected:assert row['sha256']==expected_hashes[row['kind']]
  elif row['path']in initial:assert row['sha256']==initial[row['path']],'Unrelated SDK generation changed'
 for row in native_artifacts():
  file=ROOT/row['path'];kind='pdb'if file.suffix=='.pdb'else'refint'if'refint'in file.parts else'ref'if'ref'in file.parts else'full';native=(ROOT/inventory['nativeProject']).parent;expected=native/('obj/Debug/net10.0/'+kind+'/Hexalith.EventStore.Server.Tests.dll'if kind in['ref','refint']else'bin/Debug/net10.0/Hexalith.EventStore.Server.Tests'+('.pdb'if kind=='pdb'else'.dll'));assert not row['privateMarker']and sha(file)==sha(expected)
 assert all(sha(ordinary_expected(k))==digest for k,digest in expected_hashes.items())
sdk=sdk_proof('private-primary'if args.phase=='private'else'ordinary');save(work/'sdk-proof.json',sdk);save(work/'independent-dependency-generations.json',independent_dependencies('rebuilt-primary'if args.phase=='private'else'rebuilt-with-native'));save(work/'native147-marker-scope.json',native147_proof('private-primary'if args.phase=='private'else'ordinary'));save(work/'six-entry-metadata.json',entry_metadata())
save(work/'receipt.json',{'diagnosticOnly':True,'featureAcceptance':False,'phase':args.phase,'completed':True,'source56Match':True,'primaryBuilds':13,'ordinaryNativeArtifactBuildRequired':True,'privateProducerAssembly':ASSEMBLY,'producerSha256':sdk['producerSha256'],'producerPdbSha256':sdk['producerPdbSha256'],'censusCopies':len(after),'allPrivateMarkersAbsent':not any(r['privateMarker']for r in after),'allKnownSdkCopiesScopedCoherent':True,'noHistoricalOrdinaryProducerByteEqualityClaimed':True,'privateCliHookOnly':args.phase=='private','sevenMappingValuesRetained':True});completed=True
print(json.dumps({'phase':args.phase,'completed':True,'censusCopies':len(after)}))
