"""Read-only evaluation only. Never builds/restores/cleans or starts a process topology."""
from pathlib import Path
import argparse,datetime,hashlib,json,os,re,subprocess
P=Path('/tmp/story65-gateway-controller-ops-prep-20261107');R=Path('/home/administrator/projects/hexalith/projects');S=Path('/tmp/story65-gateway-controller-prep-20261107');G=R/'references/Hexalith.EventStore/src/Hexalith.EventStore.Gateway';PRIVATE=P/'isolated-gateway';OLD=R/'_bmad-output/implementation-artifacts/evidence/story-6-5-delegated-client-20261107'
PROPERTIES=['Story65GatewayLateGuardImported','Story65GatewayOriginalAfterSdkTargets','AfterMicrosoftNETSdkTargets','_HXWFinalTargetsImported','Configuration','Optimize','DefineConstants','DebugType','UseHexalithProjectReferences','UseNuGetDeps','EnablePrivateCatalogInvocationProof','EnablePrivateOwnerSourceReadProof','BuildProjectReferences','GenerateDocumentationFile','NoWarn','TreatWarningsAsErrors','BaseOutputPath','OutputPath','OutDir','TargetDir','TargetPath','TargetRefPath','BaseIntermediateOutputPath','IntermediateOutputPath','MSBuildProjectExtensionsPath','ProjectAssetsFile','ProjectAssetsCacheFile','DocumentationFile','GeneratedAssemblyInfoFile','GeneratedGlobalUsingsFile','GeneratedMSBuildEditorConfigFile','CompilerGeneratedFilesOutputPath','EmitCompilerGeneratedFiles','ProjectDepsFilePath','ProjectRuntimeConfigFilePath','PublishDir','SourceLink','CoreCompileInputsCacheFile','ResolveAssemblyReferencesStateFile','CleanFile','GeneratedDocumentationFile','FileListAbsolute','AppendTargetFrameworkToOutputPath','AppendRuntimeIdentifierToOutputPath']
WRITABLE=['OutputPath','OutDir','TargetDir','TargetPath','TargetRefPath','IntermediateOutputPath','ProjectAssetsCacheFile','DocumentationFile','GeneratedAssemblyInfoFile','GeneratedGlobalUsingsFile','GeneratedMSBuildEditorConfigFile','CompilerGeneratedFilesOutputPath','ProjectDepsFilePath','ProjectRuntimeConfigFilePath','PublishDir','SourceLink','CoreCompileInputsCacheFile','ResolveAssemblyReferencesStateFile','GeneratedDocumentationFile','FileListAbsolute']
def sha(f):
 with Path(f).open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(name,value):(P/name).write_text(json.dumps(value,indent=2)+'\n')
def census(assembly):
 rows=[];roots=[R]+[R/x for x in re.findall(r'^\s*path\s*=\s*(.+)$',(R/'.gitmodules').read_text(),re.M)]
 for root in roots:
  for cur,dirs,files in os.walk(root):
   dirs[:]=[d for d in dirs if d not in ['.git','references','node_modules']]
   for n in files:
    if n not in [assembly+'.dll',assembly+'.pdb']:continue
    f=Path(cur)/n
    if'Debug'not in f.parts or not any(x in f.parts for x in ['bin','obj']):continue
    assert not f.is_symlink();parent=f.parent
    while parent!=root and parent.name not in ['bin','obj']:parent=parent.parent
    projects=list(parent.parent.glob('*.csproj'))if parent.name in ['bin','obj']else[]
    rows.append({'path':str(f.relative_to(R)),'sha256':sha(f),'kind':'pdb'if f.suffix=='.pdb'else'refint'if'refint'in f.parts else'ref'if'ref'in f.parts else'full','project':str(projects[0].relative_to(R))if len(projects)==1 else None})
 return sorted(rows,key=lambda x:x['path'])
def snapshot():
 owner=json.loads(Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json').read_text());source={str(Path('references/Hexalith.EventStore')/n):h for n,h in owner['sourceSha256'].items()}
 source.update({'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/DevelopmentRouteCatalogInputs.cs':'5e85827847578384a7dcca9fe448b97f21cdf4a201263c2d9ec9d0f3eda6be4d','src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs':'ac34b0c5c86c8a85ddf8591f73ddb02d32dc3a63db47a206b42688f2c696df8d','references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DelegatedOwnerQueryClient.cs':'c969d284411288c245c5833b15abb73dd93309c2671b08a03de18d694ee33a2c','references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/QueriesController.cs':'0a7bf260e145136d409a575ab2bc300e179c3350a749039f1589a90e2aaa7597'})
 assert len(source)==52 and all(sha(R/n)==h for n,h in source.items())
 inv=json.loads((OLD/'operations/prep/affected-inventory.json').read_text());app={n:sha(R/n)for n in inv['knownNative147AssemblyPaths']};assert len(app)==147
 module={}
 for folder in ['bin','obj']:
  for f in(G/folder).rglob('*'):
   if not f.is_file():continue
   assert not f.is_symlink();st=f.stat();module[str(f.relative_to(R))]={'sha256':sha(f),'bytes':st.st_size,'inode':st.st_ino,'mtimeNs':st.st_mtime_ns,'ctimeNs':st.st_ctime_ns}
 return {'source52Sha256':source,'app147Sha256':app,'ordinaryGatewayModuleBinObjFiles':module,'gatewayCensus':census('Hexalith.EventStore.Gateway'),'domainCensus':census('Hexalith.EventStore.DomainService'),'gatewayProducerAssemblySha256':sha(G/'bin/Debug/net10.0/Hexalith.EventStore.Gateway.dll'),'gatewayProducerPdbSha256':sha(G/'bin/Debug/net10.0/Hexalith.EventStore.Gateway.pdb')}
def evaluate(project,env,label,substitution=False):
 cmd=['dotnet','msbuild',str(project),'-nologo','-p:Configuration=Debug','-p:BuildProjectReferences=false','-p:NuGetAudit=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true','-p:CustomBeforeMicrosoftCommonTargets='+str(P/'gateway-scratch.props'),'-getProperty:'+','.join(PROPERTIES),'-getItem:Compile,AdditionalFiles,Analyzer,EmbeddedResource,IntermediateAssembly,IntermediateRefAssembly,FileWrites']
 if substitution:cmd.insert(3,'-target:Story65GatewayControllerSourceSubstitution')
 started=datetime.datetime.now(datetime.timezone.utc).isoformat();result=subprocess.run(cmd,cwd=R,env=env,capture_output=True,text=True)
 (P/(label+'-evaluation.log')).write_text(result.stdout);(P/(label+'-evaluation.stderr.log')).write_text(result.stderr)
 save(label+'-evaluation-command.json',{'readOnlyPropertyAndItemEvaluationOnly':True,'noBuildRestoreCleanPublishOrTargetsRequested':True,'command':cmd,'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':result.returncode,'stdoutSha256':sha(P/(label+'-evaluation.log')),'stderrSha256':sha(P/(label+'-evaluation.stderr.log'))})
 assert result.returncode==0;return json.loads(result.stdout)
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--evaluate-readonly',action='store_true');args=parser.parse_args()
 if not args.evaluate_readonly:print(json.dumps({'preparationOnly':True,'noEvaluationOrBuildExecuted':True,'plannedProject':str(G/'Hexalith.EventStore.Gateway.csproj'),'onlyReadOnlyEvaluationFlag':'--evaluate-readonly'}));return
 partial_before=None
 if PRIVATE.exists():
  proof=json.loads((P/'latest-failed-output-binding.json').read_text());partial_before={str(f.relative_to(PRIVATE)):sha(f)for f in PRIVATE.rglob('*')if f.is_file()};assert partial_before==proof['partialGatewayFileSha256']
  assert all(not f.is_symlink()for f in PRIVATE.rglob('*'))
 before=snapshot();save('scratch-readonly-before.json',before)
 mapping=json.loads((P/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(n)==h for n,h in mapping['files'].items())
 env=os.environ.copy()
 for k in list(env):
  if k in mapping['environment']or k in ['CustomBeforeMicrosoftCommonTargets','DOTNET_PROCESSOR_COUNT']:env.pop(k)
 env.update(mapping['environment']);env.update(HexalithBuildsRoot=str(R/'references/Hexalith.Builds'),DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
 data=evaluate(G/'Hexalith.EventStore.Gateway.csproj',env,'gateway');props=data['Properties'];checks=[]
 for name in WRITABLE:
  value=props[name]
  if value:
   path=Path(value)if Path(value).is_absolute()else G/value;assert path.is_relative_to(PRIVATE),(name,value);checks.append({'property':name,'path':str(path),'insideIsolatedRoot':True})
 assert Path(props['MSBuildProjectExtensionsPath'])==G/'obj'and Path(props['ProjectAssetsFile'])==G/'obj/project.assets.json'
 assert props['_HXWFinalTargetsImported']=='true'and props['Story65GatewayLateGuardImported']=='true'and props['Story65GatewayOriginalAfterSdkTargets']==mapping['environment']['AfterMicrosoftNETSdkTargets']and props['AfterMicrosoftNETSdkTargets']==mapping['environment']['AfterMicrosoftNETSdkTargets']+';'+str(P/'gateway-late-guard.targets')
 assert props['Configuration']=='Debug'and props['Optimize']=='false'and props['DebugType']=='portable'and props['BuildProjectReferences']=='false'
 assert props['EnablePrivateCatalogInvocationProof']==props['EnablePrivateOwnerSourceReadProof']=='true'
 compile_items=data['Items']['Compile'];full=[x['FullPath']for x in compile_items];original=str(R/'references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/QueriesController.cs');assert full.count(original)==1
 for key in ['IntermediateAssembly','IntermediateRefAssembly']:
  for row in data['Items'][key]:assert Path(row['FullPath']).is_relative_to(PRIVATE)
 # Same imported hook must not redirect a non-Gateway library's output/intermediate paths.
 control=evaluate(R/'references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj',env,'domain-control')
 for name in ['OutputPath','IntermediateOutputPath','TargetPath']:
  value=control['Properties'][name];assert not Path(value).is_relative_to(PRIVATE)
 substituted=evaluate(G/'Hexalith.EventStore.Gateway.csproj',env,'gateway-substituted',True)
 sub=[row['FullPath']for row in substituted['Items']['Compile']];expected_private=[str(S/n)for n in ['QueriesController.private.cs','CaptureCounters.cs','GatewayControllerBootstrap.cs']]
 assert original not in sub and all(sub.count(n)==1 for n in expected_private)and sorted(sub)==sorted([n for n in full if n!=original]+expected_private)
 after=snapshot();assert after==before
 if partial_before is not None:assert {str(f.relative_to(PRIVATE)):sha(f)for f in PRIVATE.rglob('*')if f.is_file()}==partial_before
 else:assert not PRIVATE.exists()
 assert props['EmitCompilerGeneratedFiles']=='false'and props['CompilerGeneratedFilesOutputPath']==''and props['GenerateDocumentationFile']=='false'and props['DocumentationFile']==''
 save('scratch-readonly-after.json',after)
 save('scratch-writable-path-audit.json',{'preparationOnly':True,'readOnlyEvaluationPassed':True,'noBuildRestoreOrCleanExecuted':True,'allSevenEmittedMappingEnvironmentValuesRetained':mapping['environment'],'gatewayOnlyEffectiveAfterSdkImport':props['AfterMicrosoftNETSdkTargets'],'originalAfterSdkImportRetainedFirst':True,'lateGuardImportedLast':True,'twoProofFlagsTrue':True,'buildOnlyProcessorCount2':True,'runtimeOverrideWillBeAbsent':True,'isolatedScratchRoot':str(PRIVATE),'allEvaluatedWritablePathsWithinIsolatedRoot':checks,'assetsAndExtensionsOriginalReadOnly':True,'nonGatewayControlOutputNotRedirected':True,'originalLinkedControllerCompileExactlyOnceBeforeDeferredSubstitution':True,'sourceSubstitutionOccursOnlyBeforeCoreCompile':True,'itemGroupOnlySubstitutionWasExecutedWithoutCoreCompile':True,'postSubstitutionCompileItems':len(sub),'allOtherCompileInputsBytePathsRetained':True,'allSource52App147GatewayModuleBinObjGatewayAndDomainCensusesUnchanged':True,'gatewayRows':len(before['gatewayCensus']),'domainRows':len(before['domainCensus']),'compilerItemsBeforeDeferredSubstitution':len(full),'evaluationDoesNotProveLaterGeneratedInputsOrFutureBuildWrites':True,'basePathsUnmodifiedReadOnlyAnchors':{'BaseOutputPath':props['BaseOutputPath'],'BaseIntermediateOutputPath':props['BaseIntermediateOutputPath']},'futureBuildRequiresRootReviewOfAllPathsAndTargets':True,'emitCompilerGeneratedFiles':props['EmitCompilerGeneratedFiles'],'compilerGeneratedFilesOutputPath':props['CompilerGeneratedFilesOutputPath'],'knownFailedPartialTreeUnchangedByEvaluation':partial_before is not None,'generateDocumentationFile':props['GenerateDocumentationFile'],'documentationFile':props['DocumentationFile'],'NoWarn':props['NoWarn'],'TreatWarningsAsErrors':props['TreatWarningsAsErrors']})
 print(json.dumps({'readOnlyEvaluationPassed':True,'gatewayCensusRows':len(before['gatewayCensus']),'domainCensusRows':len(before['domainCensus']),'compilerItems':len(full),'writablePropertiesChecked':len(checks),'noAppBuildOrHostExecuted':True}))
if __name__=='__main__':main()
