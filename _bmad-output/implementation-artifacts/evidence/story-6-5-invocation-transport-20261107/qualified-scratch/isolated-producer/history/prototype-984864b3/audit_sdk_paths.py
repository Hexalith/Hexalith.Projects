"""Read-only SDK compiler property/item evaluation, without build/restore/generators."""
from pathlib import Path
import argparse,datetime,hashlib,json,os,re,subprocess
P=Path('/tmp/story65-invocation-transport-ops-prep-20261107');R=Path('/home/administrator/projects/hexalith/projects');S=Path('/tmp/story65-invocation-transport-prep-20261107');G=R/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults';PRIVATE=P/'isolated-sdk'
Q=Path('/tmp/story65-outbound-assertion-qualification-20261107/final-scratch-qualification.json');QSHA='5c97f64afab724a053dc846ed05dbdd3508ddd3f43c099149898c8991abad2d9';I=Path('/tmp/story65-outbound-assertion-ops-prep-20261107/live/affected-inventory.json')
PROPERTIES=['Story65SdkLateGuardImported','Story65SdkOriginalAfterSdkTargets','AfterMicrosoftNETSdkTargets','_HXWFinalTargetsImported','HexalithSourceMappingApplies','_HXWObservedConfiguration','_HXWPreLateSelectionCaptured','Configuration','Optimize','DefineConstants','DebugType','UseHexalithProjectReferences','UseNuGetDeps','EnablePrivateCatalogInvocationProof','EnablePrivateOwnerSourceReadProof','BuildProjectReferences','GenerateDocumentationFile','EmitCompilerGeneratedFiles','NoWarn','TreatWarningsAsErrors','BaseOutputPath','OutputPath','OutDir','TargetDir','TargetPath','TargetRefPath','BaseIntermediateOutputPath','IntermediateOutputPath','MSBuildProjectExtensionsPath','ProjectAssetsFile','ProjectAssetsCacheFile','DocumentationFile','GeneratedAssemblyInfoFile','GeneratedGlobalUsingsFile','GeneratedMSBuildEditorConfigFile','CompilerGeneratedFilesOutputPath','ProjectDepsFilePath','ProjectRuntimeConfigFilePath','PublishDir','SourceLink','CoreCompileInputsCacheFile','ResolveAssemblyReferencesStateFile','CleanFile','GeneratedDocumentationFile','FileListAbsolute','AppendTargetFrameworkToOutputPath','AppendRuntimeIdentifierToOutputPath']
WRITABLE=['OutputPath','OutDir','TargetDir','TargetPath','TargetRefPath','IntermediateOutputPath','ProjectAssetsCacheFile','DocumentationFile','GeneratedAssemblyInfoFile','GeneratedGlobalUsingsFile','GeneratedMSBuildEditorConfigFile','CompilerGeneratedFilesOutputPath','ProjectDepsFilePath','ProjectRuntimeConfigFilePath','PublishDir','SourceLink','CoreCompileInputsCacheFile','ResolveAssemblyReferencesStateFile','GeneratedDocumentationFile','FileListAbsolute']
def sha(f):
 with Path(f).open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(name,value):(P/name).write_text(json.dumps(value,indent=2)+'\n')
def marker(file):
 data=Path(file).read_bytes();return any(x in data or x.decode().encode('utf-16-le')in data for x in[b'Hexalith.PrivateCaptureCounter',b'CaptureCounters.cs',b'RouteCatalogInvocationHandler.private.cs',b'QueriesController.private.cs',b'DaprDomainQueryInvoker.private.cs'])
def census(assembly):
 rows=[];roots=[R]+[R/x for x in re.findall(r'^\s*path\s*=\s*(.+)$',(R/'.gitmodules').read_text(),re.M)]
 for root in roots:
  for cur,dirs,files in os.walk(root):
   dirs[:]=[d for d in dirs if d not in['.git','references','node_modules']]
   for name in files:
    if name not in[assembly+'.dll',assembly+'.pdb']:continue
    f=Path(cur)/name
    if'Debug'not in f.parts or not any(x in f.parts for x in['bin','obj']):continue
    assert not f.is_symlink();parent=f.parent
    while parent!=root and parent.name not in['bin','obj']:parent=parent.parent
    projects=list(parent.parent.glob('*.csproj'))if parent.name in['bin','obj']else[]
    rows.append({'path':str(f.relative_to(R)),'sha256':sha(f),'kind':'pdb'if f.suffix=='.pdb'else'refint'if'refint'in f.parts else'ref'if'ref'in f.parts else'full','project':str(projects[0].relative_to(R))if len(projects)==1 else None,'privateMarker':marker(f)})
 return sorted(rows,key=lambda x:x['path'])
def snapshot():
 assert sha(Q)==QSHA;q=json.loads(Q.read_text());source=dict(q['sourceSha256']);source['references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/RouteCatalogInvocationHandler.cs']='a59c8093be4a2a112eb64b628cf243a343cb3ad836e3fff2f8dd0fe9450c4854';assert len(source)==55 and all(sha(R/name)==digest for name,digest in source.items())
 app={name:sha(R/name)for name in q['app147Sha256']};assert len(app)==147 and all(not marker(R/name)for name in app)
 module={}
 for folder in['bin','obj']:
  for file in(G/folder).rglob('*'):
   if not file.is_file():continue
   assert not file.is_symlink();s=file.stat();module[str(file.relative_to(R))]={'sha256':sha(file),'bytes':s.st_size,'inode':s.st_ino,'mtimeNs':s.st_mtime_ns,'ctimeNs':s.st_ctime_ns}
 inventory=json.loads(I.read_text());sdk=[];producer=R/'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll';assert sha(producer)=='5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872'
 for row in inventory['knownServiceDefaultsCopies']:
  f=R/row['path'];pdb=f.with_suffix('.pdb');assert sha(f)==sha(producer)and sha(pdb)==sha(producer.with_suffix('.pdb'))and not marker(f)and not marker(pdb);sdk.append({'path':row['path'],'sha256':sha(f),'pdbSha256':sha(pdb)})
 assert len(sdk)==8
 return{'source55Sha256':source,'app147Sha256':app,'ordinarySdkModuleBinObjFiles':module,'sdkCensus':census('Hexalith.EventStore.ServiceDefaults'),'serverCensus':census('Hexalith.EventStore.Server'),'gatewayCensus':census('Hexalith.EventStore.Gateway'),'domainCensus':census('Hexalith.EventStore.DomainService'),'rootProjectsServerCensus':census('Hexalith.Projects.Server'),'sdk8':sdk,'sdkProducerAssemblySha256':sha(G/'bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll'),'sdkProducerPdbSha256':sha(G/'bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.pdb')}
def environment():
 m=json.loads((P/'mapping-pins.json').read_text());assert sha(m['mappingLog'])==m['mappingLogSha256']and all(sha(n)==h for n,h in m['files'].items());env=os.environ.copy()
 for k in list(env):
  if k in m['environment']or k in['CustomBeforeMicrosoftCommonTargets','DOTNET_PROCESSOR_COUNT']:env.pop(k)
 env.update(m['environment']);env.update(HexalithBuildsRoot=str(R/'references/Hexalith.Builds'),DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0');return m,env
def evaluate(project,label,hook=False,substitute=False):
 m,env=environment();command=['dotnet','msbuild',str(project),'-nologo','-p:Configuration=Debug','-p:BuildProjectReferences=false','-p:NuGetAudit=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true']
 if hook:command+=['-p:CustomBeforeMicrosoftCommonTargets='+str(P/'sdk-scratch.props')]
 if substitute:command+=['-target:Story65InvocationTransportSourceSubstitution']
 command+=['-getProperty:'+','.join(PROPERTIES),'-getItem:Compile,AdditionalFiles,Analyzer,EmbeddedResource,IntermediateAssembly,IntermediateRefAssembly,FileWrites']
 result=subprocess.run(command,cwd=R,env=env,capture_output=True,text=True);(P/(label+'.log')).write_text(result.stdout);(P/(label+'.stderr.log')).write_text(result.stderr)
 save(label+'-command.json',{'readOnlyPropertyAndItemEvaluationOnly':True,'noBuildRestoreCleanResolveReferencesCoreCompileOrWriteGeneratingTargets':True,'itemGroupOnlySubstitutionTargetRequested':substitute,'command':command,'exitCode':result.returncode,'stdoutSha256':sha(P/(label+'.log')),'stderrSha256':sha(P/(label+'.stderr.log'))});assert result.returncode==0;return json.loads(result.stdout)
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--ordinary-only',action='store_true');parser.add_argument('--evaluate-readonly',action='store_true');a=parser.parse_args()
 if not(a.ordinary_only or a.evaluate_readonly):print(json.dumps({'preparationOnly':True,'executed':False}));return
 assert not PRIVATE.exists()and not(P/'isolated-oracle').exists()
 if a.ordinary_only:
  assert not(P/'readonly-before.json').exists();before=snapshot();save('readonly-before.json',before);d=evaluate(G/'Hexalith.EventStore.ServiceDefaults.csproj','ordinary-sdk');after=snapshot();assert before==after;save('ordinary-after.json',after)
  p=d['Properties'];save('ordinary-feature-profile.json',{'readOnlyEvaluationOnly':True,'properties':{k:p[k]for k in['GenerateDocumentationFile','EmitCompilerGeneratedFiles','DocumentationFile','CompilerGeneratedFilesOutputPath','NoWarn','TreatWarningsAsErrors','BaseOutputPath','BaseIntermediateOutputPath','ProjectAssetsFile','MSBuildProjectExtensionsPath','Configuration','Optimize','DebugType','DefineConstants','UseHexalithProjectReferences','UseNuGetDeps']},'compilerItems':len(d['Items']['Compile']),'allSource55App147SdkBinObjServerGatewayDomainRootServerSdkUnchanged':True});print(json.dumps({'ordinaryPropertiesEvaluated':True,'GenerateDocumentationFile':p['GenerateDocumentationFile'],'EmitCompilerGeneratedFiles':p['EmitCompilerGeneratedFiles'],'DocumentationFile':p['DocumentationFile'],'CompilerGeneratedFilesOutputPath':p['CompilerGeneratedFilesOutputPath'],'compileItems':len(d['Items']['Compile']),'sdkCarrierRows':len(before['sdkCensus']),'serverCarrierRows':len(before['serverCensus']),'allRootUnchanged':True}));return
 before=snapshot();assert before==json.loads((P/'readonly-before.json').read_text());normal=json.loads((P/'ordinary-sdk.log').read_text());data=evaluate(G/'Hexalith.EventStore.ServiceDefaults.csproj','private-sdk',True);props=data['Properties'];checks=[]
 for name in WRITABLE:
  value=props[name]
  if value:
   path=Path(value)if Path(value).is_absolute()else G/value;assert path.is_relative_to(PRIVATE),(name,value);checks.append({'property':name,'path':str(path),'insideIsolatedSdk':True})
 assert Path(props['MSBuildProjectExtensionsPath'])==G/'obj'and Path(props['ProjectAssetsFile'])==G/'obj/project.assets.json'
 for name in['GenerateDocumentationFile','EmitCompilerGeneratedFiles','NoWarn','TreatWarningsAsErrors','BaseOutputPath','BaseIntermediateOutputPath']:assert props[name]==normal['Properties'][name]
 m,_=environment();assert props['Story65SdkOriginalAfterSdkTargets']==m['environment']['AfterMicrosoftNETSdkTargets']and props['AfterMicrosoftNETSdkTargets']==m['environment']['AfterMicrosoftNETSdkTargets']+';'+str(P/'sdk-late-guard.targets')and props['_HXWFinalTargetsImported']==props['Story65SdkLateGuardImported']=='true'
 original=str(G/'Routing/RouteCatalogInvocationHandler.cs');originalItems=[x['FullPath']for x in data['Items']['Compile']];ordinaryItems=[x['FullPath']for x in normal['Items']['Compile']];assert sorted(originalItems)==sorted(ordinaryItems)and originalItems.count(original)==1
 sub=evaluate(G/'Hexalith.EventStore.ServiceDefaults.csproj','substituted-sdk',True,True);actual=[x['FullPath']for x in sub['Items']['Compile']];private=[str(S/name)for name in['RouteCatalogInvocationHandler.private.cs','CaptureCounters.cs','InvocationTransportBootstrap.cs']];assert original not in actual and all(actual.count(name)==1 for name in private)and sorted(actual)==sorted([name for name in ordinaryItems if name!=original]+private)
 control=evaluate(R/'references/Hexalith.EventStore/src/Hexalith.EventStore.Gateway/Hexalith.EventStore.Gateway.csproj','gateway-control',True)
 for name in['OutputPath','IntermediateOutputPath','TargetPath']:assert not Path(control['Properties'][name]).is_relative_to(PRIVATE)
 after=snapshot();assert before==after and not PRIVATE.exists();save('readonly-after.json',after);save('scratch-writable-path-audit.json',{'preparationOnly':True,'readOnlyEvaluationPassed':True,'noBuildRestoreCleanResolveReferencesCoreCompileOrAssemblyLoad':True,'source55App147SdkWholeBinObjMetadataAllCarrierCensusesAndSdk8Unchanged':True,'sdkCarrierRows':len(before['sdkCensus']),'serverCarrierRows':len(before['serverCensus']),'gatewayCarrierRows':len(before['gatewayCensus']),'domainRows':len(before['domainCensus']),'rootProjectsServerRows':len(before['rootProjectsServerCensus']),'normalCompilerItems':len(ordinaryItems),'postSubstitutionItems':len(actual),'exactOwnSdkCompileOriginal1Private1Helper1Bootstrap1':True,'allOtherCompileInputsRetained':True,'originalPublicIdentityAndOtherSdkSourceInputsPreserved':True,'evaluatedWritablePaths':checks,'restoredAssetsAndExtensionsReadOnly':True,'optionalFeatureBooleansNoWarnAndWarningsPolicyUnchanged':True,'allSevenEmittedMappingEnvironmentValuesRetained':m['environment'],'onlyOneEffectivePropertyExtended':'AfterMicrosoftNETSdkTargets','effectiveAfterSdk':props['AfterMicrosoftNETSdkTargets'],'originalAfterImportFirstGuardLast':True,'nonSdkGatewayControlUnmodified':True,'evaluationDoesNotProveFutureResolvedGeneratorOrCscWrites':True});print(json.dumps({'sdkReadonlyEvaluationPassed':True,'compileBefore':len(ordinaryItems),'compileAfter':len(actual),'allRootBaselinesUnchanged':True}))
if __name__=='__main__':main()
