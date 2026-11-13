from pathlib import Path
import importlib.util,json,re,sys
from ops_common import *
from oidc_failure_category import created_owned_oidc, owned_start_failure

# External metadata wrapper only. No application configuration, logger or source edits.
source=ROOT/'tools/qualification/run_supported_project_reads.py';sys.path.insert(0,str(source.parent))
assert sys.argv[1]=='--operational-pins-sha256';operational_pins(sys.argv[2]);del sys.argv[1:3]
spec=importlib.util.spec_from_file_location('capture_counter_owned_launcher',source)
module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=module;spec.loader.exec_module(module)
evidence=Path(sys.argv[sys.argv.index('--evidence')+1]);outer=evidence.parent
original_observe=module.observe_runtime_modes;original_execute=module.execute;original_test=module.run_test_process
entries={'diagnosticOnly':True,'featureAcceptance':False,'nativeInvocationEntries':0,'browserInvocationEntries':0}
save(outer/'execution-entry-counts.json',entries)
owned_oidc=None
def execute(command,*args,**kwargs):
 global owned_oidc
 exact=['aspire','start','--apphost',str(module.APPHOST),'--isolated','--format','Json','--non-interactive']
 if command==exact:
  save(outer/'aspire-nobuild-substitution.json',{'diagnosticOnly':True,'featureAcceptance':False,'exactOriginalCommandMatched':True,'originalCommand':command,'actualCommand':[*command,'--no-build'],'onlyAddedArgument':'--no-build','prebuiltSourceGraphRequired':True,'noRuntimeEnvironmentChange':True})
  return original_execute([*command,'--no-build'],*args,**kwargs)
 try:
  output=original_execute(command,*args,**kwargs)
 except RuntimeError as failure:
  # Observational metadata only. Never mask/replace/retry the original failure.
  try:
   closed=owned_start_failure(failure,command,kwargs.get('env'),owned_oidc)
   if closed is not None:save(outer/'oidc-start-failure-category.json',closed)
  except Exception:pass
  raise
 try:
  created=created_owned_oidc(command,kwargs.get('env'),output)
  if created is not None:owned_oidc=created
 except Exception:pass
 return output
def run_test_process(command,*args,**kwargs):
 if command and command[0]=='dotnet'and len(command)>1 and Path(command[1]).name=='Hexalith.Projects.Integration.Tests.dll':entries['nativeInvocationEntries']+=1
 elif command[:2]==['npx','playwright']:entries['browserInvocationEntries']+=1
 else:raise RuntimeError('Private execution-entry classification refused')
 save(outer/'execution-entry-counts.json',entries)
 return original_test(command,*args,**kwargs)
def observe(graph,owned,*,apps=None):
 closed=[]
 for role in(module.RUNTIME_PROJECTS if apps is None else apps):
  candidates=[value for value in graph.get('resources',[])if value.get('displayName')==role]
  properties=candidates[0].get('properties',{})if len(candidates)==1 else{};pid=properties.get('executable.pid')
  closed.append({'role':role,'uniqueResource':len(candidates)==1,'sourceProjectPathMatches':properties.get('project.path')==str(ROOT/module.RUNTIME_PROJECTS[role]),'executablePidInteger':type(pid)is int,'processStarted':type(pid)is int and module.process_start(pid)is not None})
 save(outer/'pre-observation-closed-source-facts.json',{'diagnosticOnly':True,'featureAcceptance':False,'roles':closed})
 result=original_observe(graph,owned,apps=apps)
 if set(result)!=set(module.READ_APPS):return result
 save(outer/'early-runtime-identities.json',{'diagnosticOnly':True,'featureAcceptance':False,'originalOwnedRuntimeObservationSucceeded':True,'identities':[{'role':role,'pid':value['pid'],'startTicks':value['startTicks'],'wrapperPid':value['wrapperPid'],'wrapperStartTicks':value['wrapperStartTicks']}for role,value in result.items()]})
 sources();private_directory()
 manifest=json.loads((evidence/'source-manifest.json').read_text());assert all(sha(ROOT/relative)==digest for relative,digest in manifest.items())
 domain=domain_proof('private-with-native');sdk=sdk_proof('private-with-native');dependencies=independent_dependencies();entry_proof=entry_metadata();installation=installed_source_path_proof()
 producer=PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');producer_pdb=producer.with_suffix('.pdb');assert marker(producer)and marker(producer_pdb)
 private_work=Path(json.loads((PREP/'latest-private-attempt.json').read_text())['attemptDirectory']);assert private_work.parent==PREP
 compiled=json.loads((private_work/'actual-domain-producer-compiled-proof.json').read_text());assert compiled['producerSha256']==sha(producer)and compiled['producerPdbSha256']==sha(producer_pdb)and compiled['threeExactCoreSourcesObserved']is True and sha(private_work/'actual-domain-compiler-inputs.json')==compiled['observedInputsSha256']
 copies=[];entry_by_role={row['role']:row for row in entry_proof['entries']}
 for role,value in result.items():
  project=ROOT/module.RUNTIME_PROJECTS[role];directory=project.parent/'bin/Debug/net10.0';sdk_copy=directory/(SDK+'.dll');sdk_pdb=sdk_copy.with_suffix('.pdb');eligible=entry_by_role[role]['eligibleUnderFreshLiteralNameGate']
  assert same_start(value['pid'],value['startTicks'])and same_start(value['wrapperPid'],value['wrapperStartTicks'])and entry_by_role[role]['entryDllSha256']==value['assemblySha256']
  assert sha(sdk_copy)==SDK_SHA and sha(sdk_pdb)==SDK_PDB_SHA and not marker(sdk_copy)and not marker(sdk_pdb)
  maps={line.split(maxsplit=5)[-1]for line in Path('/proc',str(value['pid']),'maps').read_text().splitlines()if len(line.split(maxsplit=5))==6};assert str(sdk_copy)in maps
  record={'role':role,'pid':value['pid'],'startTicks':value['startTicks'],'wrapperPid':value['wrapperPid'],'wrapperStartTicks':value['wrapperStartTicks'],'entryAssemblySha256':value['assemblySha256'],'entryAssemblySimpleName':entry_by_role[role]['metadataAssemblySimpleName'],'eligibleUnderFreshLiteralNameGate':eligible,'sdkCopyPath':str(sdk_copy),'sdkSha256':sha(sdk_copy),'sdkPdbSha256':sha(sdk_pdb),'ordinarySdkMappedOwnedRuntime':True,'privateDomainMapRequired':eligible}
  if eligible:
   assert role in{'projects','tenants','folders'}
   copy=directory/(ASSEMBLY+'.dll');pdb=copy.with_suffix('.pdb');assert marker(copy)and marker(pdb)and sha(copy)==sha(producer)and sha(pdb)==sha(producer_pdb)and str(copy)in maps
   record.update(domainCopyPath=str(copy),domainSha256=sha(copy),pdbSha256=sha(pdb),privateDomainMarkerPresent=True,domainMappedOwnedRuntime=True)
  else:record.update(excludedRuntimeHelperStateInspected=False,privateDomainMappingClaimed=False)
  assert same_start(value['pid'],value['startTicks'])and same_start(value['wrapperPid'],value['wrapperStartTicks'])
  copies.append(record)
 assert len(copies)==6 and {row['role']for row in copies if row['privateDomainMapRequired']}=={'projects','tenants','folders'}
 save(outer/'runtime-private-domain-proof.json',{'diagnosticOnly':True,'featureAcceptance':False,'source59Match':True,'fullManifestCount':len(manifest),'fullManifestSha256':sha(evidence/'source-manifest.json'),'fullManifestAllMatch':True,'sourceMappingValuesUnchangedByWrapper':True,'noNewRuntimeEnvironmentKeys':True,'allSixOrdinarySdkCopiesMappedCoherent':True,'exactThreeEligiblePrivateDomainCopiesMappedCoherent':True,'privateDomainMapRoleCount':3,'allActualDomainCarrierCopiesCensusedSeparately':True,'domainProof':domain,'sdkAlwaysOrdinaryProof':sdk,'actualPrivateCompilerProofSha256':sha(private_work/'actual-domain-producer-compiled-proof.json'),'actualPrivateCompilerInputsSha256':compiled['observedInputsSha256'],'installedSourcePathProof':installation,'independentOrdinarySourceDependencyGenerations':dependencies,'privateProducerSha256':sha(producer),'privateProducerPdbSha256':sha(producer_pdb),'onlyThreeReceiverPhysicalEntryMetadataMatches':True,'actualThreeExcludedRuntimeHelperStatesNotInspected':True,'freshEntryMetadataProof':entry_proof,'ownerSessionIdentities':{key:owned[key]for key in ['appHostPid','appHostStartTicks','cliPid','cliStartTicks']},'copies':copies})
 return result
module.execute=execute;module.run_test_process=run_test_process;module.observe_runtime_modes=observe
sys.exit(module.main())
