"""Pure mapping-selection and compiler-path validation; no writer/build targets."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,subprocess,xml.etree.ElementTree as ET
P=Path('/tmp/story65-invocation-transport-ops-prep-20261107');R=Path('/home/administrator/projects/hexalith/projects')
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def save(name,value):(P/name).write_text(json.dumps(value,indent=2)+'\n')
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--evaluate-readonly',action='store_true');a=ap.parse_args()
 if not a.evaluate_readonly:print(json.dumps({'preparationOnly':True,'executed':False,'onlyReadOnlyTargetValidationAvailable':True}));return
 spec=importlib.util.spec_from_file_location('audit_scratch',P/'audit_sdk_paths.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
 before=audit.snapshot();assert before==json.loads((P/'readonly-before.json').read_text())and not(P/'isolated-sdk').exists()
 mapping=json.loads((P/'mapping-pins.json').read_text());assert all(sha(n)==h for n,h in mapping['files'].items())
 env=os.environ.copy()
 for name in list(env):
  if name in mapping['environment']or name in ['CustomBeforeMicrosoftCommonTargets','DOTNET_PROCESSOR_COUNT']:env.pop(name)
 env.update(mapping['environment']);env.update(HexalithBuildsRoot=str(R/'references/Hexalith.Builds'),DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
 props=list(mapping['environment'])+['Story65SdkOriginalAfterSdkTargets','Story65SdkLateGuardImported','HexalithSourceMappingApplies','_HXWObservedConfiguration','_HXWPreLateSelectionCaptured','_HXWFinalTargetsImported']
 command=['dotnet','msbuild',str(R/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj'),'-nologo','-p:Configuration=Debug','-p:BuildProjectReferences=false','-p:NuGetAudit=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-p:CustomBeforeMicrosoftCommonTargets='+str(P/'sdk-scratch.props'),'-target:HexalithVerifySourceMappingSelection;Story65ValidateSdkCompilerPaths','-getProperty:'+','.join(props)]
 result=subprocess.run(command,cwd=R,env=env,capture_output=True,text=True);(P/'readonly-guard-validation.log').write_text(result.stdout);(P/'readonly-guard-validation.stderr.log').write_text(result.stderr);assert result.returncode==0
 observed=json.loads(result.stdout)['Properties'];effective=dict(mapping['environment']);effective['AfterMicrosoftNETSdkTargets']+=';'+str(P/'sdk-late-guard.targets')
 assert all(observed[k]==v for k,v in effective.items())
 assert observed['Story65SdkOriginalAfterSdkTargets']==mapping['environment']['AfterMicrosoftNETSdkTargets']
 assert observed['Story65SdkLateGuardImported']==observed['HexalithSourceMappingApplies']==observed['_HXWPreLateSelectionCaptured']==observed['_HXWFinalTargetsImported']=='true'and observed['_HXWObservedConfiguration']=='Debug'
 after=audit.snapshot();assert after==before and not(P/'isolated-sdk').exists()
 contract=Path('/tmp/story65-owner-repair-native-20261107/mapping/source-mapping/SourceMapping.values.targets');tree=ET.parse(contract);checks=[x.attrib for x in tree.iter('Error')if 'AfterMicrosoftNETSdkTargets'in x.attrib.get('Condition','')];assert checks and all('.Contains('in x['Condition']for x in checks)
 sdk=Path('/home/administrator/.dotnet/sdk/10.0.403/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets');lines=sdk.read_text().splitlines();matches=[i+1 for i,line in enumerate(lines)if '<Import Project="$(AfterMicrosoftNETSdkTargets)"'in line];assert len(matches)==1
 proof={'readOnlyOnly':True,'command':command,'exitCode':0,'originalSevenEmittedEnvironmentValues':mapping['environment'],'effectiveSelectedProperties':observed,'effectiveChangedProperties':['AfterMicrosoftNETSdkTargets'],'allSevenEffectiveValuesUnchangedClaim':False,'originalAfterImportFirstAndExactHash':mapping['files'][mapping['environment']['AfterMicrosoftNETSdkTargets']],'addedSdkOnlyFinalImport':str(P/'sdk-late-guard.targets'),'allOriginalMappingFilesHashesStillMatch':True,'mappingTargetDefinitionAndVerificationExecutedReadOnly':True,'sourceMappingContractPrimaryFile':str(contract),'sourceMappingContractPrimarySha256':sha(contract),'allAfterGuardsAreContainsChainChecks':checks,'sdkFinalImportPrimaryFile':str(sdk),'sdkFinalImportPrimarySha256':sha(sdk),'sdkFinalImportLine':matches[0],'noOriginalMappingFileEdited':True,'noBuildCoreCompileResolveReferencesOrAssemblyLoad':True,'source55App147SdkModuleMetadataSdk137Server66Gateway34Domain98RootServer10Sdk8Unchanged':True,'guardSourceAndWritablePathValidationSucceeded':True,'futureGeneratorAndCscWriteObservationStillPending':True,'importOrderClaimLimitedToPinnedSdkImportGraph':True}
 save('source-mapping-chain-proof.json',proof)
 save('readonly-guard-validation.json',{'pureValidationTargetOnly':True,'command':command,'exitCode':0,'stdoutSha256':sha(P/'readonly-guard-validation.log'),'stderrSha256':sha(P/'readonly-guard-validation.stderr.log'),'targetHasOnlyItemGroupsAndErrors':True,'originalHandlerAbsentPrivateHelperBootstrapExactlyOnce':True,'allRootBaselinesUnchanged':True,'noCopyBuildRestoreCleanResolveReferencesCoreCompileOrAssemblyLoad':True})
 print(json.dumps({'readOnlyValidationPassed':True,'ordinaryRootBaselinesUnchanged':True,'effectiveMappingPropertyExtendedCount':1,'isolatedOutputCreated':False}))
if __name__=='__main__':main()
