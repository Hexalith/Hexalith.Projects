from pathlib import Path
import importlib.util,json,re,sys
from ops_common import *

# External metadata wrapper only. No application configuration, logger or source edits.
source=ROOT/'tools/qualification/run_supported_project_reads.py';sys.path.insert(0,str(source.parent))
assert sys.argv[1]=='--operational-pins-sha256';operational_pins(sys.argv[2]);del sys.argv[1:3]
spec=importlib.util.spec_from_file_location('capture_counter_owned_launcher',source)
module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=module;spec.loader.exec_module(module)
evidence=Path(sys.argv[sys.argv.index('--evidence')+1]);outer=evidence.parent
original_observe=module.observe_runtime_modes;original_execute=module.execute;original_test=module.run_test_process
entries={'diagnosticOnly':True,'featureAcceptance':False,'nativeInvocationEntries':0,'browserInvocationEntries':0}
save(outer/'execution-entry-counts.json',entries)
def execute(command,*args,**kwargs):
 exact=['aspire','start','--apphost',str(module.APPHOST),'--isolated','--format','Json','--non-interactive']
 if command==exact:
  save(outer/'aspire-nobuild-substitution.json',{'diagnosticOnly':True,'featureAcceptance':False,'exactOriginalCommandMatched':True,'originalCommand':command,'actualCommand':[*command,'--no-build'],'onlyAddedArgument':'--no-build','prebuiltSourceGraphRequired':True,'noRuntimeEnvironmentChange':True})
  return original_execute([*command,'--no-build'],*args,**kwargs)
 return original_execute(command,*args,**kwargs)
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
 sdk_proof=ordinary_sdk_proof();sdk_producer=SDK_PRODUCER/'bin/Debug/net10.0'/(SDK+'.dll');sdk_pdb=sdk_producer.with_suffix('.pdb')
 producer=PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');producer_pdb=producer.with_suffix('.pdb');assert marker(producer)and marker(producer_pdb)
 copies=[]
 for role,value in result.items():
  project=ROOT/module.RUNTIME_PROJECTS[role];copy=project.parent/'bin/Debug/net10.0'/(SDK+'.dll');pdb=copy.with_suffix('.pdb')
  assert module.process_start(value['pid'])==value['startTicks']and not marker(copy)and sha(copy)==sha(sdk_producer)
  maps=Path('/proc',str(value['pid']),'maps').read_text();assert any(line.split(maxsplit=5)[-1]==str(copy)for line in maps.splitlines()if len(line.split(maxsplit=5))==6)
  assert pdb.exists()and sha(pdb)==sha(sdk_pdb)
  copies.append({'role':role,'pid':value['pid'],'startTicks':value['startTicks'],'wrapperPid':value['wrapperPid'],'wrapperStartTicks':value['wrapperStartTicks'],'entryAssemblySha256':value['assemblySha256'],'sdkSha256':sha(copy),'pdbSha256':sha(pdb),'ordinarySdkNoPrivateMarker':True,'sdkMappedOwnedRuntime':True})
 projects=result['projects'];maps=Path('/proc',str(projects['pid']),'maps').read_text()
 assert any(line.split(maxsplit=5)[-1]==str(producer)for line in maps.splitlines()if len(line.split(maxsplit=5))==6)
 assert projects['assemblySha256']==sha(producer)and module.process_start(projects['pid'])==projects['startTicks']
 save(outer/'runtime-private-projects-proof.json',{'diagnosticOnly':True,'featureAcceptance':False,'source50Match':True,'fullManifestCount':len(manifest),'fullManifestSha256':sha(evidence/'source-manifest.json'),'fullManifestAllMatch':True,'sourceMappingValuesUnchangedByWrapper':True,'noNewRuntimeEnvironmentKeys':True,'allSixOrdinarySdkCopiesMatch':True,'ordinarySdkProof':sdk_proof,'projectsPrivateAssemblyVerified':True,'projectsPid':projects['pid'],'projectsStartTicks':projects['startTicks'],'privateProducerSha256':sha(producer),'privateProducerPdbSha256':sha(producer_pdb),'privateProjectsAssemblyMapped':True,'copies':copies})
 return result
module.execute=execute;module.run_test_process=run_test_process;module.observe_runtime_modes=observe
sys.exit(module.main())
