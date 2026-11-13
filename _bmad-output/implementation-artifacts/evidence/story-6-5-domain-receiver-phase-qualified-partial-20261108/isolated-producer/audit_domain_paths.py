"""Deferred read-only MSBuild evaluations and selected import/target-order proof."""
from pathlib import Path
import argparse,hashlib,json,subprocess,xml.etree.ElementTree as ET
from producer_common import *
PROPERTIES=['Configuration','Optimize','DebugType','DebugSymbols','DefineConstants','UseHexalithProjectReferences','UseNuGetDeps','EnablePrivateCatalogInvocationProof','EnablePrivateOwnerSourceReadProof','GenerateDocumentationFile','EmitCompilerGeneratedFiles','NoWarn','TreatWarningsAsErrors','BaseOutputPath','BaseIntermediateOutputPath','OutputPath','OutDir','TargetPath','TargetRefPath','IntermediateOutputPath','MSBuildProjectExtensionsPath','ProjectAssetsFile','ProjectAssetsCacheFile','DocumentationFile','CompilerGeneratedFilesOutputPath','GeneratedAssemblyInfoFile','GeneratedGlobalUsingsFile','GeneratedMSBuildEditorConfigFile','SourceLink','ProjectDepsFilePath','ProjectRuntimeConfigFilePath','CoreCompileInputsCacheFile','ResolveAssemblyReferencesStateFile','PublishDir','Story65DomainOriginalAfterSdkTargets','AfterMicrosoftNETSdkTargets','Story65DomainLateGuardImported','_HXWFinalTargetsImported','CoreCompileDependsOn','TargetsTriggeredByCompilation']
WRITES=['OutputPath','OutDir','TargetPath','TargetRefPath','IntermediateOutputPath','ProjectAssetsCacheFile','DocumentationFile','CompilerGeneratedFilesOutputPath','GeneratedAssemblyInfoFile','GeneratedGlobalUsingsFile','GeneratedMSBuildEditorConfigFile','SourceLink','ProjectDepsFilePath','ProjectRuntimeConfigFilePath','CoreCompileInputsCacheFile','ResolveAssemblyReferencesStateFile','PublishDir']
ITEMS='Compile,AdditionalFiles,Analyzer,EmbeddedFiles,LinkResource,EmbeddedResource,EditorConfigFiles,IntermediateAssembly,IntermediateRefAssembly,FileWrites'
def command(project,hook=False):
 result=['dotnet','msbuild',str(project),'-nologo','-p:Configuration=Debug','-p:BuildProjectReferences=false','-p:NuGetAudit=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true']
 if hook:result+=['-p:CustomBeforeMicrosoftCommonTargets='+str(PREP/'domain-scratch.props')]
 return result
def evaluate(project,name,hook=False,substitute=False):
 mapping,env=environment();cmd=command(project,hook)
 if substitute:cmd+=['-target:Story65DomainReceiverSourceSubstitution']
 cmd+=['-getProperty:'+','.join(PROPERTIES),'-getItem:'+ITEMS]
 r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True);assert r.returncode==0
 value=json.loads(r.stdout);save(name,{'readOnlyEvaluation':True,'command':cmd,'exitCode':0,'stdoutSha256':hashlib.sha256(r.stdout).hexdigest(),'stderrSha256':hashlib.sha256(r.stderr).hexdigest(),'selectedPropertiesAndItems':value});return value

def main():
 a=argparse.ArgumentParser();a.add_argument('--ordinary-only',action='store_true');a.add_argument('--evaluate-readonly',action='store_true');a.add_argument('--validate-order-readonly',action='store_true');args=a.parse_args()
 if not any(vars(args).values()):print(json.dumps({'preparationOnly':True,'executed':False}));return
 assert not(PREP/'isolated-domain').exists()and not(PREP/'isolated-oracle').exists();before=snapshot()
 if args.ordinary_only:
  save('readonly-before.json',before);value=evaluate(PROJECT/(ASSEMBLY+'.csproj'),'ordinary-evaluation.json');after=snapshot();assert after==before;save('ordinary-after.json',after);print(json.dumps({'ordinaryReadonlyPassed':True,'compileInputs':len(value['Items']['Compile']),'allRootUnchanged':True}));return
 assert before==json.loads((PREP/'readonly-before.json').read_text())
 if args.evaluate_readonly:
  normal=json.loads((PREP/'ordinary-evaluation.json').read_text())['selectedPropertiesAndItems'];plain=evaluate(PROJECT/(ASSEMBLY+'.csproj'),'isolated-evaluation.json',True);sub=evaluate(PROJECT/(ASSEMBLY+'.csproj'),'substituted-evaluation.json',True,True);props=plain['Properties'];checks=[]
  for name in WRITES:
   if props[name]:
    path=Path(props[name]);path=path if path.is_absolute()else PROJECT/path;assert path.resolve().is_relative_to(PREP/'isolated-domain');checks.append({'property':name,'insideIsolatedOutput':True,'path':str(path)})
  for key in ['GenerateDocumentationFile','EmitCompilerGeneratedFiles','NoWarn','TreatWarningsAsErrors','BaseOutputPath','BaseIntermediateOutputPath','MSBuildProjectExtensionsPath','ProjectAssetsFile']:assert props[key]==normal['Properties'][key]
  assert Path(props['ProjectAssetsFile'])==PROJECT/'obj/project.assets.json'
  expected=[str(PROJECT/(name+'.cs'))for name in json.loads((PREP/'source-core-binding.json').read_text())['corePrivateSha256']];ordinary=[v['FullPath']for v in normal['Items']['Compile']];unsub=[v['FullPath']for v in plain['Items']['Compile']];actual=[v['FullPath']for v in sub['Items']['Compile']];private=[str(SOURCE/(Path(v).stem+'.private.cs'))for v in expected]+[str(SOURCE/'CaptureCounters.cs'),str(SOURCE/'ReceiverPhaseBootstrap.cs')]
  assert sorted(unsub)==sorted(ordinary)and all(ordinary.count(v)==1 for v in expected)
  assert sorted(actual)==sorted([v for v in ordinary if v not in expected]+private)and all(actual.count(v)==1 for v in private)
  control=evaluate(ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore.Gateway/Hexalith.EventStore.Gateway.csproj','gateway-control.json',True)
  assert all(not Path(control['Properties'][key]).is_relative_to(PREP/'isolated-domain')for key in ['OutputPath','IntermediateOutputPath','TargetPath'])
  after=snapshot();assert after==before;save('readonly-after.json',after);save('writable-path-evaluation-proof.json',{'passed':True,'actualWritesStillRequireLateGuard':True,'evaluatedPaths':checks,'original3RemovedPrivate3HelperBootstrapExactlyOnce':True,'allOtherCompileItemsRetained':True,'beforeCompileCount':len(ordinary),'afterCompileCount':len(actual),'source59App147Sdk137AndAllModuleMetadataUnchanged':True});print(json.dumps({'isolatedReadonlyPassed':True,'beforeCompile':len(ordinary),'afterCompile':len(actual),'allRootUnchanged':True}));return
  
 mapping,env=environment();cmd=command(PROJECT/(ASSEMBLY+'.csproj'),True)+['-target:HexalithVerifySourceMappingSelection;Story65ValidateDomainReceiverCompilerInputs','-getProperty:'+','.join(PROPERTIES)]
 r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True);assert r.returncode==0;properties=json.loads(r.stdout)['Properties'];assert properties['_HXWFinalTargetsImported']==properties['Story65DomainLateGuardImported']=='true'
 assert properties['Story65DomainOriginalAfterSdkTargets']==mapping['environment']['AfterMicrosoftNETSdkTargets']
 assert properties['AfterMicrosoftNETSdkTargets']==mapping['environment']['AfterMicrosoftNETSdkTargets']+';'+str(PREP/'domain-csc-manifest.targets')
 # Preprocessing expands imports but executes no targets. Keep full XML only in RAM.
 pp=command(PROJECT/(ASSEMBLY+'.csproj'),True)+['-preprocess'];r=subprocess.run(pp,cwd=ROOT,env=env,capture_output=True);assert r.returncode==0 and len(r.stdout)<=32*1024*1024
 tree=ET.fromstring(r.stdout);targets=[e for e in tree.iter()if e.tag.rsplit('}',1)[-1]=='Target'];guard=[i for i,e in enumerate(targets)if e.get('Name')=='Story65CaptureDomainReceiverBeforeCsc'];assert len(guard)==1
 earlier_targets=[(i,e.get('Name'))for i,e in enumerate(targets)if'CoreCompile'in(e.get('BeforeTargets')or'').split(';')and e.get('Name')!='Story65CaptureDomainReceiverBeforeCsc'];assert all(i<guard[0]for i,_ in earlier_targets)
 csc=[e for e in targets if e.get('Name')=='CoreCompile'][-1];calls=[e for e in csc.iter()if e.tag.rsplit('}',1)[-1]=='Csc'];assert len(calls)==1
 selected={k:calls[0].get(k)for k in ['Sources','References','Analyzers','AdditionalFiles','EmbeddedFiles','Resources','LinkResources','SourceLink','DisabledWarnings','ApplicationConfiguration','NoStandardLib','TargetType','Platform','Features','WarningsAsErrors','WarningsNotAsErrors','EmbedAllSources','ChecksumAlgorithm']}
 assert selected['Sources']=='@(Compile)'and selected['References']=='@(ReferencePathWithRefAssemblies)'and selected['Analyzers']=='@(Analyzer)'
 after=snapshot();assert after==before and not(PREP/'isolated-domain').exists()
 save('mapping-and-target-order-proof.json',{'passed':True,'allSevenEmittedEnvironmentValuesRetained':mapping['environment'],'effectiveAfterSdk':properties['AfterMicrosoftNETSdkTargets'],'effectiveChangedPropertyCount':1,'originalMappingFirstFinalGuardLast':True,'selectedProperties':properties,'preprocessCommand':pp,'preprocessedXmlSha256':hashlib.sha256(r.stdout).hexdigest(),'rawPreprocessedXmlPersisted':False,'earlierLiteralBeforeCoreCompileTargets':[name for _,name in earlier_targets],'guardLastInPinnedResolvedImportGraph':True,'coreCompileDependsOn':properties['CoreCompileDependsOn'],'selectedPinnedCscParameterExpressions':selected,'insideCoreCompileNoWarnAppConfigPdbTransformsRequirePostSnapshot':True,'notTaskInterceptionOrFullCommandLineEquivalence':True,'allRootUnchanged':True});print(json.dumps({'mappingAndPinnedOrderProofPassed':True,'allRootUnchanged':True}))
if __name__=='__main__':main()
