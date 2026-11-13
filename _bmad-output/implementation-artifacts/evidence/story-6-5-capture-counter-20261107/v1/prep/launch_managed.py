from pathlib import Path
import importlib.util,json,re,sys
from ops_common import *

# External metadata wrapper only. No application configuration, logger or source edits.
source=ROOT/'tools/qualification/run_supported_project_reads.py';sys.path.insert(0,str(source.parent))
assert sys.argv[1]=='--operational-pins-sha256';operational_pins(sys.argv[2]);del sys.argv[1:3]
spec=importlib.util.spec_from_file_location('capture_counter_owned_launcher',source)
module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=module;spec.loader.exec_module(module)
evidence=Path(sys.argv[sys.argv.index('--evidence')+1]);outer=evidence.parent
original_observe=module.observe_runtime_modes
def observe(graph,owned,*,apps=None):
 result=original_observe(graph,owned,apps=apps)
 if set(result)!=set(module.READ_APPS):return result
 sources();private_directory()
 manifest=json.loads((evidence/'source-manifest.json').read_text());assert all(sha(ROOT/relative)==digest for relative,digest in manifest.items())
 producer=PRODUCER/'bin/Debug/net10.0'/(SDK+'.dll');producer_pdb=producer.with_suffix('.pdb');assert marker(producer)
 copies=[]
 for role,value in result.items():
  project=ROOT/module.RUNTIME_PROJECTS[role];copy=project.parent/'bin/Debug/net10.0'/(SDK+'.dll');pdb=copy.with_suffix('.pdb')
  assert module.process_start(value['pid'])==value['startTicks']and marker(copy)and sha(copy)==sha(producer)
  maps=Path('/proc',str(value['pid']),'maps').read_text();assert any(line.split(maxsplit=5)[-1]==str(copy)for line in maps.splitlines()if len(line.split(maxsplit=5))==6)
  assert pdb.exists()and sha(pdb)==sha(producer_pdb)
  copies.append({'role':role,'pid':value['pid'],'startTicks':value['startTicks'],'wrapperPid':value['wrapperPid'],'wrapperStartTicks':value['wrapperStartTicks'],'entryAssemblySha256':value['assemblySha256'],'sdkSha256':sha(copy),'pdbSha256':sha(pdb),'privateMarkerPresent':True,'sdkMappedOwnedRuntime':True})
 save(outer/'runtime-private-sdk-proof.json',{'diagnosticOnly':True,'featureAcceptance':False,'ownerSource48Match':True,'extraCaptureSourceMatch':True,'fullManifestCount':len(manifest),'fullManifestSha256':sha(evidence/'source-manifest.json'),'fullManifestAllMatch':True,'sourceMappingValuesUnchangedByWrapper':True,'noNewRuntimeEnvironmentKeys':True,'producerSha256':sha(producer),'producerPdbSha256':sha(producer_pdb),'allSixRuntimeCopiesMatch':True,'copies':copies})
 return result
module.observe_runtime_modes=observe
sys.exit(module.main())
