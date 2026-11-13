"""Isolated Gateway producer plan; execution requires separately root-released pins."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,subprocess,datetime,tempfile
P=Path('/tmp/story65-gateway-controller-ops-prep-20261107');R=Path('/home/administrator/projects/hexalith/projects')
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def save(n,v):(P/n).write_text(json.dumps(v,indent=2)+'\n')
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--execute',action='store_true');ap.add_argument('--pins-sha256');a=ap.parse_args()
 plan=json.loads((P/'scratch-producer-command-plan.json').read_text());interface=json.loads((P/'oracle-candidate-interface.json').read_text())
 if not a.execute:print(json.dumps({'preparationOnly':True,'executed':False,'copyOracleWithoutRebuild':interface['oracle'],'candidateBuildCommand':plan['command'],'noRebuildCleanRestoreGlobalOutputOverrideOrAssemblyLoad':True}));return
 pinsfile=P/'producer-pins.json';assert a.pins_sha256 and sha(pinsfile)==a.pins_sha256;pins=json.loads(pinsfile.read_text());assert pins['allowIsolatedScratchProducerBuild']is True,'Root has not released isolated producer build'
 for n,h in pins['files'].items():assert sha(P/n)==h
 for n,h in pins['externalFiles'].items():assert sha(n)==h
 spec=importlib.util.spec_from_file_location('audit_scratch',P/'audit_scratch_paths.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
 before=audit.snapshot();baseline=json.loads((P/'scratch-readonly-before.json').read_text());assert before==baseline
 oracle=Path(interface['oracle']['isolatedDll']).parent;private=P/'isolated-gateway'
 attempts=P/'producer-attempts';attempts.mkdir(mode=0o700,exist_ok=True);work=Path(tempfile.mkdtemp(prefix='attempt-',dir=attempts));save('latest-scratch-producer-attempt.json',{'attemptDirectory':str(work)})
 receipt={'isolatedScratchProducerOnly':True,'featureAcceptance':False,'applicationBuilds':0,'nativeTestsExecuted':0,'hostsStarted':0,'started':False,'completed':False,'rootModulesUnchanged':False,'attemptDirectory':str(work),'priorTopLevelFailureReceiptPreserved':True}
 try:
  # Only byte copies of the pinned ordinary baseline; no assembly loading or ordinary rebuild.
  if private.exists():
   preservation=json.loads((P/'failed-attempt-c090/preservation-receipt.json').read_text())
   assert sha(P/'scratch-producer-receipt.json')==preservation['producerReceiptSha256']and sha(P/'scratch-producer-build.log')==preservation['buildLogSha256']
   failed=json.loads((P/'scratch-producer-receipt.json').read_text());assert failed['completed']is False and failed['exitCode']==1 and failed['rootModulesUnchanged']is True
   actual={}
   for file in private.rglob('*'):
    assert not file.is_symlink()
    if file.is_file():actual[str(file.relative_to(private))]=sha(file)
   assert actual==preservation['partialGatewayFileSha256']
   archived=work/'archived-c090-partial';private.rename(archived);receipt['archivedExactFailedPartial']={'path':str(archived),'filesSha256':actual,'noDeletionOrClean':True}
  if not oracle.exists():oracle.mkdir(mode=0o700)
  assert not oracle.is_symlink()and set(f.name for f in oracle.iterdir())in[set(),{'Hexalith.EventStore.Gateway.dll','Hexalith.EventStore.Gateway.pdb'}]
  for src,dst,digest in [(interface['oracle']['sourceDll'],interface['oracle']['isolatedDll'],interface['oracle']['dllSha256']),(interface['oracle']['sourcePdb'],interface['oracle']['isolatedPdb'],interface['oracle']['pdbSha256'])]:
   assert sha(src)==digest;target=Path(dst)
   if not target.exists():shutil.copyfile(src,target);target.chmod(0o600)
   assert not target.is_symlink()and sha(target)==digest
  mapping=json.loads((P/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(n)==h for n,h in mapping['files'].items())
  env=os.environ.copy()
  for k in list(env):
   if k in mapping['environment']or k in ['CustomBeforeMicrosoftCommonTargets','DOTNET_PROCESSOR_COUNT']:env.pop(k)
  env.update(mapping['environment']);env.update(plan['buildOnlyEnvironment'])
  command=plan['command'];assert '-t:Build'in command and '--no-restore'in command and '-p:BuildProjectReferences=false'in command and not any(x in command for x in ['-t:Rebuild','-t:Clean'])
  receipt.update(started=True,command=command,startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),oracleDllSha256=interface['oracle']['dllSha256'],oraclePdbSha256=interface['oracle']['pdbSha256'])
  with(work/'build.log').open('wb')as stream:result=subprocess.run(command,cwd=R,env=env,stdout=stream,stderr=subprocess.STDOUT)
  receipt.update(exitCode=result.returncode,finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),buildLogSha256=sha(work/'build.log'));assert result.returncode==0
  candidate=Path(interface['candidate']['targetDll']);pdb=Path(interface['candidate']['targetPdb']);assert candidate.is_file()and pdb.is_file()
  data=candidate.read_bytes();assert b'Hexalith.PrivateCaptureCounter'in data and b'GatewayControllerBootstrap'in data
  intermediate=private/'obj/Debug/net10.0';inputs=intermediate/'story65-compiler-inputs.txt';writes=intermediate/'story65-writable-paths.txt';assert inputs.is_file()and writes.is_file()
  actual=[]
  for line in inputs.read_text(encoding='utf-8-sig').splitlines():
   kind,path=line.split('|',1);assert kind in ['Compile','AdditionalFile','Analyzer','Reference','Resource'];file=Path(path);assert file.is_file()and not file.is_symlink();actual.append({'kind':kind,'path':path,'sha256':sha(file)})
  for path in writes.read_text(encoding='utf-8-sig').splitlines():assert Path(path).is_relative_to(private)
  assert sum(x['path'].endswith('/QueriesController.private.cs')for x in actual)==1 and not any(x['path'].endswith('/Hexalith.EventStore/Controllers/QueriesController.cs')for x in actual)
  save(work/'actual-compiler-inputs.json',{'buildMetadataOnly':True,'inputs':actual,'manifestSha256':sha(inputs),'writableManifestSha256':sha(writes),'noRuntimeRequestConfigurationOrPayloadData':True})
  receipt.update(completed=True,candidateDllSha256=sha(candidate),candidatePdbSha256=sha(pdb),candidateReferenceSha256=sha(Path(interface['candidate']['referenceDll'])),sameIdentityAndDebugAttributesStillRequireALCQualification=True,lateCompilerPathGuardAndManifestsPresent=True)
 except Exception as failure:receipt['failureType']=type(failure).__name__
 finally:
  after=audit.snapshot();save(work/'after.json',after);receipt['rootModulesUnchanged']=after==before
  receipt['noOrdinaryGatewayModuleOrApp147Source52Domain98Writes']=after==before;save(work/'receipt.json',receipt)
 assert receipt['completed']and receipt['rootModulesUnchanged']
 print(json.dumps({'isolatedScratchProducerOnly':True,'completed':True,'rootModulesUnchanged':True,'assemblyLoads':0,'hostsStarted':0}))
if __name__=='__main__':main()
