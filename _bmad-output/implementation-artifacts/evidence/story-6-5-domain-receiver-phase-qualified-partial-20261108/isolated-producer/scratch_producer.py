"""Separately gated isolated Domain producer; no application/host/native execution."""
from pathlib import Path
import argparse,datetime,json,shutil,subprocess,tempfile
from producer_common import *
from read_compiler_manifest import read

def main():
 a=argparse.ArgumentParser();a.add_argument('--execute',action='store_true');a.add_argument('--pins-sha256');args=a.parse_args()
 plan=json.loads((PREP/'producer-command-plan.json').read_text());interface=json.loads((PREP/'oracle-candidate-interface.json').read_text())
 if not args.execute:print(json.dumps({'preparationOnly':True,'isolatedProducerCommand':plan['command'],'oracleCopyOnlyAfterRelease':interface['oracle'],'noBuildCopyLoadOrHostExecuted':True}));return
 assert args.pins_sha256 and sha(PREP/'producer-pins.json')==args.pins_sha256;pins=json.loads((PREP/'producer-pins.json').read_text())
 assert pins['sourcePinsReady']is True and pins['sourceQualificationReady']is False and pins['allowIsolatedScratchProducerBuild']is True and pins['readonlyEvaluationReady']is True and pins['allowApplicationBuildNativeOrHosts']is False
 assert all(sha(PREP/name)==digest for name,digest in pins['files'].items())and all(sha(path)==digest for path,digest in pins['externalFiles'].items())
 core_before=verify_core();before=snapshot();assert before==json.loads((PREP/'readonly-before.json').read_text())
 private=PREP/'isolated-domain';oracle=PREP/'isolated-oracle';assert not private.exists()and not oracle.exists(),'Fresh isolated outputs required; retain any failure before a reviewed retry'
 attempts=PREP/'producer-attempts';attempts.mkdir(mode=0o700,exist_ok=True);work=Path(tempfile.mkdtemp(prefix='attempt-',dir=attempts));save('latest-producer-attempt.json',{'attemptDirectory':str(work)})
 receipt={'isolatedProducerOnly':True,'completed':False,'rootModulesUnchanged':False,'coreInputsUnchanged':False,'featureAcceptance':False,'applicationBuilds':0,'assemblyLoads':0,'nativeTests':0,'hostsStarted':0,'command':plan['command']}
 try:
  oracle.mkdir(mode=0o700)
  for src,dst,digest in [(interface['oracle']['sourceDll'],interface['oracle']['isolatedDll'],interface['oracle']['dllSha256']),(interface['oracle']['sourcePdb'],interface['oracle']['isolatedPdb'],interface['oracle']['pdbSha256'])]:
   assert sha(src)==digest;target=Path(dst);assert target.parent==oracle and not target.exists();shutil.copyfile(src,target);target.chmod(0o600);assert sha(target)==digest
  mapping,env=environment();command=plan['command'];assert'-t:Build'in command and'--no-restore'in command and'-p:BuildProjectReferences=false'in command
  receipt['startedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
  with(work/'build.log').open('xb')as stream:r=subprocess.run(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
  receipt.update(exitCode=r.returncode,buildLogSha256=sha(work/'build.log'));assert r.returncode==0
  actual=read(private/'obj/Debug/net10.0');compiles=[row['path']for row in actual['observedInputs']if row['kind']=='Compile'];binding=json.loads((PREP/'source-core-binding.json').read_text())
  expected=[str(SOURCE/(name+'.private.cs'))for name in binding['corePrivateSha256']]+[str(SOURCE/'CaptureCounters.cs'),str(SOURCE/'ReceiverPhaseBootstrap.cs')]
  assert all(compiles.count(name)==1 for name in expected)and all(str(PROJECT/(name+'.cs'))not in compiles for name in binding['corePrivateSha256'])
  assert all(Path(path).is_relative_to(private)for path in actual['observedWritePaths'])
  assert actual['preCoreCompileFlags']['Configuration']==['Debug']and actual['preCoreCompileFlags']['Optimize']==['false']and actual['preCoreCompileFlags']['PrivateDomainCore']==['true']
  save(work/'observed-compiler-inputs.json',actual)
  candidate=Path(interface['candidate']['targetDll']);assert marker(candidate)
  receipt.update(completed=True,candidateDllSha256=sha(candidate),candidatePdbSha256=sha(interface['candidate']['targetPdb']),candidateRefSha256=sha(interface['candidate']['referenceDll']),candidateRefintSha256=sha(interface['candidate']['intermediateRefDll']),sourceCorePrivateSha256=binding['corePrivateSha256'],compilerObservationProofSha256=sha(work/'observed-compiler-inputs.json'),fixtureQualificationStillPending=True,byteEquivalenceNotClaimed=True)
 except Exception as failure:receipt['failureType']=type(failure).__name__
 finally:
  try:after=snapshot();save(work/'after.json',after);receipt['rootModulesUnchanged']=after==before
  except Exception as failure:receipt['afterAuditFailureType']=type(failure).__name__
  try:receipt['coreInputsUnchanged']=verify_core()==core_before
  except Exception as failure:receipt['coreAfterAuditFailureType']=type(failure).__name__
  save(work/'receipt.json',receipt)
 assert receipt['completed']and receipt['rootModulesUnchanged']and receipt['coreInputsUnchanged']
 print(json.dumps({'isolatedDomainProducerPassed':True,'rootUnchanged':True,'sdkAlwaysOrdinary6947':True,'applicationOrHostLoads':0}))
if __name__=='__main__':main()
