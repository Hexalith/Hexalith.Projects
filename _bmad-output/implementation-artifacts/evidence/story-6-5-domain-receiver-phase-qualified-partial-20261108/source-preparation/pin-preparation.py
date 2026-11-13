from pathlib import Path
import datetime,hashlib,json,xml.etree.ElementTree as ET
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP=Path(__file__).parent
PRODUCER=Path('/tmp/story65-domain-receiver-phase-producer-prep-20261108')
TRANSFER=Path('/tmp/story65-domain-receiver-phase-ops-prep-20261108/producer-transfer-interface.json')
SHA=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
LOAD=lambda p:json.loads(Path(p).read_bytes())
CORE={'DomainRouteCatalogFilter':'121ae30a04c76268f6fee5fa725730188579d7ec741ad3e293df27b1e5bb9a06','DomainRouteCatalogGuard':'50ba9c6b9200c6463aded2bb13f64af2ce5b775b94ce6520aded74e3d186f953','DomainRouteCatalogResult':'304eba32f8ec5ef2bc8832425a8996a2ebd721b2d58ec9b6bf777d39420f8ca7'}
assert SHA(PREP/'fixture-api.json')=='6a82c8705726aece454651a8e21d846f6abb2d68c02fc1a7ff7eed10335515c0'
assert SHA(PREP/'CaptureCounters.cs')=='112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'
assert SHA(PREP/'ReceiverPhaseBootstrap.cs')=='ff22ad6362bf4f7dea2ffaa782066dc719f1fcbf6e9205e50ee3df9df30df495'
for name,h in CORE.items():assert SHA(PREP/(name+'.private.cs'))==h
reader=LOAD(PREP/'driver/owned-source-index.json');assert len(reader['filesSha256'])==44
for name,h in reader['filesSha256'].items():assert SHA(PREP/name)==h
companion=LOAD(PREP/'companion-source-index.json')
for name,h in companion['filesSha256'].items():assert SHA(PREP/name)==h
normal=LOAD(PREP/'source-normalization.json')
for name,row in normal['changes'].items():
 original=(PREP/(name+'.original.cs.txt')).read_bytes();text=(PREP/(name+'.private.cs')).read_text()
 for operation in reversed(row['operations']):
  assert text.count(operation['new'])==1;text=text.replace(operation['new'],operation['old'],1)
 assert text.encode()==original and SHA(row['originalPath'])==row['originalSha256']
source=companion['sourceControls59Sha256'];assert len(source)==59
for name,h in source.items():assert SHA(ROOT/name)==h
# Use the established exact app147 path set with FRESH current bytes, never inherited historical hashes.
prior=LOAD('/tmp/story65-receiver-phase-prep-20261107/preparation-receipt.json')
apps={name:SHA(ROOT/name)for name in prior['app147Sha256']};assert len(apps)==147
external={}
def pin(path,expected=None):
 p=Path(path);assert p.is_absolute()and p.is_file()and not p.is_symlink();actual=SHA(p)
 if expected is not None:assert actual==expected,str(p)
 if str(p)in external:assert external[str(p)]==actual
 external[str(p)]=actual
for name,h in source.items():pin(ROOT/name,h)
for name,h in apps.items():pin(ROOT/name,h)
for project in [PREP/'companion/DomainReceiverFixture.csproj',PREP/'driver/ReceiverDriver.csproj']+[PREP/'entry'/r/'Entry.csproj'for r in ['projects','tenants','folders','excluded']]:
 tree=ET.parse(project);assert not tree.findall('.//PackageReference')
 project_refs=tree.findall('.//ProjectReference')
 if project.parent.parent==PREP/'entry':
  assert len(project_refs)==1 and (project.parent/project_refs[0].attrib['Include']).resolve()==(PREP/'driver/ReceiverDriver.csproj').resolve()
 else:assert not project_refs
 for item in tree.findall('.//Reference'):
  pin(item.findtext('HintPath'));assert item.findtext('Private')=='false'
# Pin only known managed fixture assets; no appsettings, identity, endpoint, auth or arbitrary JSON files.
testbin=ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0'
fixture_assets={}
for f in sorted(testbin.iterdir()):
 if f.is_file()and(f.suffix in ['.dll','.pdb']or f.name.endswith(('.deps.json','.runtimeconfig.json'))):
  pin(f);fixture_assets[f.name]={'path':str(f),'sha256':SHA(f)}
assert all(n in fixture_assets for n in ['Hexalith.EventStore.Server.Tests.dll','Hexalith.EventStore.Server.Tests.deps.json','Hexalith.EventStore.Server.Tests.runtimeconfig.json'])
for f in [ROOT/'references/Hexalith.EventStore/Hexalith.EventStore.slnx',ROOT/'references/Hexalith.EventStore/deploy/dapr/eventstore-routing-catalog.json']:pin(f)
for fact in LOAD(PREP/'driver/live-path-source-map.json')['sourceFacts']:
 f=Path(fact['path']);pin(f if f.is_absolute()else ROOT/f,fact['sha256'])
for name,h in LOAD(PREP/'installation-witness.json')['sourcePathsSha256'].items():pin(ROOT/name,h)
for selected in LOAD(PREP/'native103-source-derivation.json')['selectedSources']:
 f=Path(selected['source']);pin(f if f.is_absolute()else ROOT/f,selected['sourceSha256'])
for name in ['RouteCatalogTestFixture.cs','CatalogReceiverScopeTests.cs']:
 pin(ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Routing'/name)
# Framework reference and runtime closure use only the current installed .NET10.0.14 packs/libraries.
framework_inputs=0
for kind in ['Microsoft.NETCore.App.Ref','Microsoft.AspNetCore.App.Ref']:
 base=Path('/home/administrator/.dotnet/packs')/kind/'10.0.14';assert base.is_dir()
 for f in sorted(base.rglob('*')):
  if f.is_file()and f.suffix in ['.dll','.targets','.props','.json']:pin(f);framework_inputs+=1
for kind in ['Microsoft.NETCore.App','Microsoft.AspNetCore.App']:
 base=Path('/home/administrator/.dotnet/shared')/kind/'10.0.14';assert base.is_dir()
 for f in sorted(base.iterdir()):
  if f.is_file()and(f.suffix=='.dll'or f.name.endswith(('.deps.json','.runtimeconfig.json'))):pin(f);framework_inputs+=1
pin('/home/administrator/.dotnet/dotnet')
# Producer source/input generation is immutable and separately reviewed; no live-ops WIP gets pinned.
producer_pins=LOAD(PRODUCER/'producer-pins.json');assert len(producer_pins['files'])==25 and len(producer_pins['externalFiles'])==90
pin(PRODUCER/'producer-pins.json')
for name,h in producer_pins['files'].items():pin(PRODUCER/name,h)
for name,h in producer_pins['externalFiles'].items():
 f=Path(name);pin(f if f.is_absolute()else ROOT/f,h)
assert SHA(TRANSFER)=='e4ef57bb2b53fdc1d7a2921eab2d8eeae7eebbbc7c428f6f7a950e3ec034000f';pin(TRANSFER)
transfer=LOAD(TRANSFER);assert transfer['reviewedCoreSha256']==CORE
for path_key,hash_key in [('producerReceipt','producerReceiptSha256'),('parentProducerAudit','parentProducerAuditSha256'),('observedCompilerInputs','observedCompilerInputsSha256')]:pin(transfer[path_key],transfer[hash_key])
receipt=LOAD(transfer['producerReceipt']);parent=LOAD(transfer['parentProducerAudit']);assert receipt['completed']is True and receipt['exitCode']==0 and parent['passed']is True
observed=LOAD(transfer['observedCompilerInputs']);assert len(observed['observedInputs'])==636
for row in observed['observedInputs']:pin(row['path'],row['sha256'])
for field in ['ordinaryDomainVariant','privateDomainVariant']:
 for suffix in ['dll','pdb']:pin(transfer[field][suffix],transfer[field][suffix+'Sha256'])
# OrdinarySDK is the SAME current pair in both variants; only DomainService differs.
sdk=ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll'
pin(sdk,transfer['ordinarySdkDllSha256']);pin(sdk.with_suffix('.pdb'),transfer['ordinarySdkPdbSha256'])
commands=LOAD(PREP/'driver-commands.json');assert len(commands['futureRestoreCommands'])==len(commands['futureBuildCommands'])==6
files={str(f.relative_to(PREP)):SHA(f)for f in sorted(PREP.rglob('*'))if f.is_file()and not any(t in f.relative_to(PREP).parts for t in ['bin','obj','__pycache__'])and f.name!='preparation-receipt.json'}
canonical={str(PREP/name):h for name,h in files.items()};canonical.update(external)
for name,h in source.items():assert canonical[str(ROOT/name)]==h
value={'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'filePreparationOnly':True,'sourceQualificationReady':False,'fixtureBuildsStarted':0,'fixtureAssemblyLoads':0,'nativeExecutions':0,'appHostsOrCounterWindowsStarted':0,'filesSha256':files,'sourceSha256':source,'sourceCount':59,'app147Sha256':apps,'currentExternalInputsSha256':external,'canonicalCurrentInputCountBeforeAddingThisReceipt':len(canonical),'fixtureApiSha256':SHA(PREP/'fixture-api.json'),'coreSha256':CORE,'helperSha256':SHA(PREP/'CaptureCounters.cs'),'bootstrapSha256':SHA(PREP/'ReceiverPhaseBootstrap.cs'),'readerOwnedSourceIndexSha256':SHA(PREP/'driver/owned-source-index.json'),'readerOwnedFileCount':44,'companionSourceIndexSha256':SHA(PREP/'companion-source-index.json'),'companionFileCount':len(companion['filesSha256']),'sourceNormalizationAllThreeByteExact':True,'namespaceEnumTypeQualificationCorrectedBeforeAnyCSharpBuild':True,'ordinarySdkIdenticalBothVariants':True,'onlyDomainServiceDllPdbVariantDifferenceRequired':True,'producerTransfer':{'path':str(TRANSFER),'sha256':SHA(TRANSFER)},'producerReceipt':{'path':transfer['producerReceipt'],'sha256':transfer['producerReceiptSha256']},'parentProducerAudit':{'path':transfer['parentProducerAudit'],'sha256':transfer['parentProducerAuditSha256']},'observedCompilerInputCount':636,'ordinaryDomainVariant':transfer['ordinaryDomainVariant'],'privateDomainVariant':transfer['privateDomainVariant'],'ordinarySdkDllSha256':transfer['ordinarySdkDllSha256'],'ordinarySdkPdbSha256':transfer['ordinarySdkPdbSha256'],'rootReviewedCoreReceipt':{'path':str(PREP/'parent-core-review-v2.json'),'sha256':SHA(PREP/'parent-core-review-v2.json')},'installationWitness':{'path':str(PREP/'installation-witness.json'),'sha256':SHA(PREP/'installation-witness.json'),'staticSourceAndTypedSyntheticOnly':True,'notFreshLiveEndpointFactoryInspection':True},'proposedFocusedPairs':64,'proposedNativeCasesPerVariant':103,'proposedControlledProcesses':commands['plannedTotalProcesses'],'actualFixtureQualificationClaimed':False,'ordinaryNative1048NotInflated':True,'fixtureAssetFilesPinned':len(fixture_assets),'fixtureAssetsSha256':fixture_assets,'frameworkReferenceRuntimeInputsPinned':framework_inputs,'exactStandaloneRestoreCommands':commands['futureRestoreCommands'],'exactStandaloneBuildCommands':commands['futureBuildCommands'],'commandSnapshotSha256':SHA(PREP/'driver-commands.json'),'explicitBuildEnvironment':commands['explicitBuildEnvironment'],'removeInheritedEnvironment':commands['removeInheritedEnvironment'],'cwd':str(PREP),'runtimeProcessorOverride':None,'postBuildRuntimeManifestAndCompiledFixtureHashesPending':True,'postBuildRootReviewRequiredBeforeAssemblyLoadNativeKestrelOrWindow':True,'mainLiveOpsWipExcluded':True,'historical10DependencyProvenanceLimitRetained':True,'priorRestoration17Native147DifferencesNotRetrospectivelyWhitelisted':True,'allDependencyGenerationsHistoricalSourceCoherentClaimed':False,'correctionHistoryStoredOutsideRuntimeInputUnion':'/tmp/story65-domain-receiver-phase-prep-history-20261108','corrections':['inline non-result release retains original scope until failure unwinds; retry occurs after scope','concrete-result-fault uses actual Results.Json response-stream write failure','Stage and AllocationMode are namespace enums; Interval alone is nested'],'noProductionSourceOrSpecOrDependencyOrGitChanges':True}
out=PREP/'preparation-receipt.json';assert not out.exists();out.write_text(json.dumps(value,indent=2)+'\n')
print(json.dumps({'receipt':str(out),'sha256':SHA(out),'preparedFiles':len(files),'externalInputs':len(external),'sourceControls':59,'appCoordinates':147,'canonicalUnionBeforeReceipt':len(canonical),'canonicalUnionIncludingReceipt':len(canonical)+1,'fixtureAssets':len(fixture_assets),'frameworkInputs':framework_inputs,'buildsLoadsRuns':0}))
