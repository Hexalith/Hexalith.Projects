"""Isolated receiver-phase SDK producer. Root must separately release its pinned build flag."""
from pathlib import Path
import argparse,datetime,hashlib,importlib.util,json,os,shutil,subprocess,tempfile
P=Path('/tmp/story65-receiver-phase-producer-prep-20261107');R=Path('/home/administrator/projects/hexalith/projects')
def sha(path):
 with Path(path).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def save(path,value):
 (P/path).write_text(json.dumps(value,indent=2)+'\n')
def verify_bound_source_core():
 binding=json.loads((P/'source-core-binding.json').read_text());prepfile=Path(binding['sourceCoreReceipt'])
 assert binding['sourcePinsReady']is True and binding['sourceQualificationReady']is False and binding['producerCoreBindingNotFullFixtureFreeze']is True
 assert not prepfile.is_symlink()and sha(prepfile)==binding['sourceCoreReceiptSha256']
 metadata=json.loads(prepfile.read_text());source=prepfile.parent;assert source==Path('/tmp/story65-receiver-phase-prep-20261107')
 assert len(metadata['filesSha256'])==binding['sourceCoreReceiptPreparedFiles']==17
 assert len(metadata['sourceControlsSha256'])==binding['sourceControls']==56
 assert metadata['privateSourceSha256']==binding['coreSha256']and metadata['helperSha256']==binding['helperSha256']and metadata['bootstrapSha256']==binding['bootstrapSha256']
 assert binding['expectedCoreReceiptFields']and all(metadata[key]==value for key,value in binding['expectedCoreReceiptFields'].items())
 checked={str(prepfile):sha(prepfile)}
 for relative,digest in binding['consumedCoreFilesSha256'].items():
  rel=Path(relative);assert not rel.is_absolute()and'..'not in rel.parts;file=source/rel
  assert file.resolve().is_relative_to(source)and file.is_file()and not file.is_symlink()and sha(file)==digest==metadata['filesSha256'][relative];checked[str(file)]=digest
 for relative,digest in metadata['sourceControlsSha256'].items():
  rel=Path(relative);assert not rel.is_absolute()and'..'not in rel.parts;file=R/rel
  assert file.resolve().is_relative_to(R)and file.is_file()and not file.is_symlink()and sha(file)==digest;checked[str(file)]=digest
 normalized=json.loads((source/'source-normalization.json').read_text());assert normalized['normalizationByteExact']is True and normalized['stage5RepeatedIntervalsAreLastWinsNotSum']is True
 assert sha(source/'TechnicalHttpRouteCatalogReceiver.original.cs.txt')==binding['originalSha256']
 handoff=json.loads((P/'ordinary-handoff.json').read_text())
 for key in ['ownerReceipt','nativeAudit','sourceControls','nativeBinaryCoordinateAudit']:
  row=handoff[key];file=Path(row['path']);assert file.is_file()and not file.is_symlink()and sha(file)==row['sha256'];checked[str(file)]=row['sha256']
 assert json.loads(Path(handoff['sourceControls']['path']).read_text())['sourceSha256']==metadata['sourceControlsSha256']
 return checked
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--execute',action='store_true');ap.add_argument('--pins-sha256');a=ap.parse_args()
 plan=json.loads((P/'scratch-producer-command-plan.json').read_text());interface=json.loads((P/'oracle-candidate-interface.json').read_text())
 if not a.execute:
  print(json.dumps({'preparationOnly':True,'executed':False,'copyOracleWithoutRebuild':interface['oracle'],'candidateBuildCommand':plan['command'],'noRebuildCleanRestoreGlobalOutputOverrideOrAssemblyLoad':True}));return
 pinfile=P/'producer-pins.json';assert a.pins_sha256 and sha(pinfile)==a.pins_sha256;pins=json.loads(pinfile.read_text())
 assert pins['sourcePinsReady']is True and pins['allowIsolatedScratchProducerBuild']is True,'Source pins and isolated producer build are not released'
 assert pins['allowApplicationBuildNativeOrHosts']is False and pins['sourceQualificationReady']is False and pins['readonlyEvaluationReady']is True
 for name,digest in pins['files'].items():assert sha(P/name)==digest
 for name,digest in pins['externalFiles'].items():assert sha(name)==digest
 prepared_before=verify_bound_source_core()
 spec=importlib.util.spec_from_file_location('audit_scratch',P/'audit_sdk_paths.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
 before=audit.snapshot();assert before==json.loads((P/'readonly-before.json').read_text())
 private=P/'isolated-sdk';oracle=P/'isolated-oracle';assert not private.exists()and not oracle.exists(),'Fresh output directories required; preserve any failed attempt before a separately reviewed retry'
 attempts=P/'producer-attempts';attempts.mkdir(mode=0o700,exist_ok=True);work=Path(tempfile.mkdtemp(prefix='attempt-',dir=attempts));save('latest-scratch-producer-attempt.json',{'attemptDirectory':str(work)})
 receipt={'isolatedScratchProducerOnly':True,'featureAcceptance':False,'applicationBuilds':0,'nativeTestsExecuted':0,'hostsStarted':0,'assemblyLoads':0,'started':False,'completed':False,'rootModulesUnchanged':False,'attemptDirectory':str(work),'sourceCoreInputsUnchanged':False,'sourceCorePreparationReceiptSha256':json.loads((P/'source-core-binding.json').read_text())['sourceCoreReceiptSha256'],'sourceCoreInputCount':len(prepared_before)}
 try:
  oracle.mkdir(mode=0o700)
  for source,target,digest in [(interface['oracle']['sourceDll'],interface['oracle']['isolatedDll'],interface['oracle']['dllSha256']),(interface['oracle']['sourcePdb'],interface['oracle']['isolatedPdb'],interface['oracle']['pdbSha256'])]:
   assert sha(source)==digest and not Path(source).is_symlink();dst=Path(target);assert dst.parent==oracle and not dst.exists();shutil.copyfile(source,dst);dst.chmod(0o600);assert sha(dst)==digest
  mapping=json.loads((P/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(n)==h for n,h in mapping['files'].items())
  env=os.environ.copy()
  for name in list(env):
   if name in mapping['environment']or name in ['CustomBeforeMicrosoftCommonTargets','DOTNET_PROCESSOR_COUNT']:env.pop(name)
  env.update(mapping['environment']);env.update(plan['buildOnlyEnvironment'])
  command=plan['command'];assert '-t:Build'in command and '--no-restore'in command and '-p:BuildProjectReferences=false'in command and not any(x in command for x in ['-t:Rebuild','-t:Clean'])
  receipt.update(started=True,command=command,startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),oracleDllSha256=interface['oracle']['dllSha256'],oraclePdbSha256=interface['oracle']['pdbSha256'])
  with(work/'build.log').open('xb')as stream:result=subprocess.run(command,cwd=R,env=env,stdout=stream,stderr=subprocess.STDOUT)
  receipt.update(exitCode=result.returncode,finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),buildLogSha256=sha(work/'build.log'));assert result.returncode==0
  candidate=Path(interface['candidate']['targetDll']);pdb=Path(interface['candidate']['targetPdb']);assert candidate.is_file()and pdb.is_file()
  data=candidate.read_bytes();assert b'Hexalith.PrivateCaptureCounter'in data and b'ReceiverPhaseBootstrap'in data
  intermediate=private/'obj/Debug/net10.0';inputs=intermediate/'story65-compiler-inputs.txt';writes=intermediate/'story65-writable-paths.txt';assert inputs.is_file()and writes.is_file()
  actual=[]
  for line in inputs.read_text(encoding='utf-8-sig').splitlines():
   kind,path=line.split('|',1);assert kind in ['Compile','AdditionalFile','Analyzer','Reference','Resource'];file=Path(path);assert file.is_file()and not file.is_symlink();actual.append({'kind':kind,'path':path,'sha256':sha(file)})
  for path in writes.read_text(encoding='utf-8-sig').splitlines():assert Path(path).is_relative_to(private)
  paths=[x['path']for x in actual if x['kind']=='Compile'];source=Path('/tmp/story65-receiver-phase-prep-20261107')
  assert all(paths.count(str(source/name))==1 for name in ['TechnicalHttpRouteCatalogReceiver.private.cs','CaptureCounters.cs','ReceiverPhaseBootstrap.cs'])
  assert str(R/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/TechnicalHttpRouteCatalogReceiver.cs')not in paths
  assert paths.count(str(R/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/RouteCatalogInvocationHandler.cs'))==1
  save(work/'actual-compiler-inputs.json',{'buildMetadataOnly':True,'inputs':actual,'manifestSha256':sha(inputs),'writableManifestSha256':sha(writes),'noRuntimeRequestConfigurationOrPayloadData':True})
  receipt.update(completed=True,candidateDllSha256=sha(candidate),candidatePdbSha256=sha(pdb),candidateReferenceSha256=sha(interface['candidate']['referenceDll']),candidateIntermediateReferenceSha256=sha(interface['candidate']['intermediateRefDll']),sameIdentityAndDebugAttributesStillRequireFixtureQualification=True,lateCompilerPathGuardAndManifestsPresent=True)
 except Exception as failure:receipt['failureType']=type(failure).__name__
 finally:
  try:
   after=audit.snapshot();save(work/'after.json',after);receipt['rootModulesUnchanged']=after==before
  except Exception as failure:receipt['afterAuditFailureType']=type(failure).__name__
  try:receipt['sourceCoreInputsUnchanged']=verify_bound_source_core()==prepared_before
  except Exception as failure:receipt['sourceCoreAfterAuditFailureType']=type(failure).__name__
  receipt['noOrdinarySdkModuleOrApp147Source56ObservedSdkServerGatewayDomainCensusWrites']=receipt['rootModulesUnchanged'];save(work/'receipt.json',receipt)
 assert receipt['completed']and receipt['rootModulesUnchanged']and receipt['sourceCoreInputsUnchanged']
 print(json.dumps({'isolatedScratchProducerOnly':True,'completed':True,'rootModulesUnchanged':True,'assemblyLoads':0,'hostsStarted':0}))
if __name__=='__main__':main()
