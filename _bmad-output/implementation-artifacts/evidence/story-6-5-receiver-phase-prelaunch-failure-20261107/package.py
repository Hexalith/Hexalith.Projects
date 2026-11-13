from pathlib import Path
import collections,gzip,hashlib,json,re,xml.etree.ElementTree as ET
OUT=Path(__file__).parent
RUN=Path('/tmp/story65-receiver-phase-diagnostic-20261107')
ARCHIVE=Path('/tmp/story65-receiver-phase-retry-history-20261107')
OPS=ARCHIVE/'released-ops'
PROBE=Path('/tmp/story65-oidc-start-probe-20261107')
ROOT=Path('/home/administrator/projects/hexalith/projects')
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
load=lambda p:json.loads(Path(p).read_bytes())
provenance=[]
def save(name,x):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);data=(json.dumps(x,indent=2)+'\n').encode();assert not p.exists()or p.read_bytes()==data;p.write_bytes(data)if not p.exists()else None
def record(source,target,encoding,original,stored,fields=None):
 row={'originalPath':str(source),'originalSha256':hashlib.sha256(original).hexdigest(),'originalBytes':len(original),'packetPath':str(target.relative_to(OUT)),'packetSha256':hashlib.sha256(stored).hexdigest(),'packetBytes':len(stored),'encoding':encoding}
 if fields is not None:row['fixedAllowlistFields']=fields
 provenance.append(row)
def copy(source,name,gzip_data=False):
 source=Path(source);assert source.is_file()and not source.is_symlink();original=source.read_bytes();data=gzip.compress(original,mtime=0)if gzip_data else original
 target=OUT/name;target.parent.mkdir(parents=True,exist_ok=True);assert not target.exists()or target.read_bytes()==data;target.write_bytes(data)if not target.exists()else None;record(source,target,'gzip-mtime0'if gzip_data else'exact-copy',original,data)
def project(source,name,value,fields):
 source=Path(source);original=source.read_bytes();data=(json.dumps(value,indent=2)+'\n').encode();target=OUT/name;target.parent.mkdir(parents=True,exist_ok=True);assert not target.exists()or target.read_bytes()==data;target.write_bytes(data)if not target.exists()else None;record(source,target,'fixed-allowlist-projection',original,data,fields)
def scalar_fields(x,names):
 d={}
 for k in names:
  if k in x:
   v=x[k];assert v is None or type(v)in [bool,int,float]or(isinstance(v,str)and re.fullmatch('[0-9a-f]{64}',v));d[k]=v
 return d
parent=load(RUN/'parent-post-restoration-audit.json')
assert sha(RUN/'parent-post-restoration-audit.json')=='da29a51d8fa384579a4e2cc25fd3edef35c75bbf8ef477c7787db4a85a27e76b'
assert parent['passed']is True and parent['privateBuilds']==13 and parent['privateNativeExecutions']==47 and parent['ordinaryMinimumBuilds']==14 and parent['measurementComplete']is False
assert parent['sdkSha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'and parent['sdkPdbSha256']=='ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
assert parent['managedLauncherExitCode']==1 and parent['noOwnedApplicationHostsStarted']is True
pnames=['passed','featureAcceptance','privateBuilds','privateNativeExecutions','ordinaryMinimumBuilds','ordinarySourceControlsUnchanged','sdkSha256','sdkPdbSha256','sdkCensusCopies','priorNative147Compared','ordinaryNative1048NotInflated','noOwnedApplicationHostsStarted','managedLauncherExitCode','supervisorExitDoesNotMeanManagedJourneyPassed','measurementComplete','allFullLiveManifestHashesUnchanged','frozenSpecAndSprintUnchanged','priorUnknownDependencyProvenanceLimitsRetained']
p=scalar_fields(parent,pnames);p['privateDirectory']=parent['privateDirectory'];assert set(p['privateDirectory'])=={'uid','device','inode','mode','symlink'}
assert p['privateDirectory']=={'uid':1000,'device':2096,'inode':8752986,'mode':'0700','symlink':False}
p['priorNative147CurrentHashDifferences']=parent['priorNative147CurrentHashDifferences'];assert len(p['priorNative147CurrentHashDifferences'])==17
for name in p['priorNative147CurrentHashDifferences']:assert isinstance(name,str)and not Path(name).is_absolute()and name.endswith('.dll')and '..'not in Path(name).parts
p['actualOrdinaryReceiptOriginalPath']=parent['actualOrdinaryReceipt'];p['emptyFixedDirectoryVerifiedInParentAudit']=True
p['actual17DeltasAreRecordedNotWhitelisted']=True;p['historical10UnknownDependencyProvenanceLimitRetained']=True
project(RUN/'parent-post-restoration-audit.json','restoration/parent-post-restoration-audit.projection.json',p,pnames+['privateDirectory','priorNative147CurrentHashDifferences','actualOrdinaryReceipt'])
# Never copy the private managed run, launcher output, identities, URLs, namespace, auth, env or CIDs.
run=load(RUN/'managed/run.json');controls=run['launcherControls'];assert all(type(controls[k])is int for k in ['total','passed','failed','skipped'])and(controls['total'],controls['passed'],controls['failed'],controls['skipped'])==(142,142,0,0)
cleanup=run['cleanup'];assert len(cleanup)==1 and all(x['removed']is True and x['absenceVerified']is True for x in cleanup)
value={'featureAcceptance':False,'managedJourneySucceeded':False,'oidcContainerStartFailedBeforeApplicationHosts':True,'appHostsStarted':0,'nativeJourneyEntries':0,'browserEntries':0,'counterWindowPublished':False,'acceptedEquivalence':run['acceptedEquivalence'],'productionReadiness':run['productionReadiness'],'matrixCompleted':run['matrixCompleted'],'launcherControls':{k:controls[k]for k in ['total','passed','failed','skipped']},'ownedCleanupResources':len(cleanup),'allOwnedCleanupRemovedAndAbsent':True,'preexistingResourcesPreserved':run['preexistingResourcesPreserved'],'privateMaterialRemoved':run['privateMaterialRemoved'],'commandCount':len(run['commands']),'rawRunCopied':False,'rawFailureTextCopied':False,'rawRuntimeUrlsAuthEnvOrContainerIdsCopied':False}
project(RUN/'managed/run.json','prelaunch/managed-run.fixed-projection.json',value,['acceptedEquivalence','productionReadiness','matrixCompleted','launcherControls.total/passed/failed/skipped','cleanup.removed/absenceVerified(count only)','preexistingResourcesPreserved','privateMaterialRemoved','commands(length only)'])
supervisor=load(RUN/'supervisor.json');assert supervisor['controlPublished']is False and supervisor['launcherExitCode']==1
s=scalar_fields(supervisor,['preparationOnly','noNewRuntimeEnvironmentOrConfigurationKeys','naturalCpuOverrideAbsent','noRuntimeSourceHook','eightSecondAdmissionAfterAtLeastThreeGenuineFailedMultiple','latestCompletedAttemptMaximum','noDelayToOriginalCleanup','noCutoffOrTargetSignals','diagnosticOnly','featureAcceptance','controlPublished','launcherExitCode','noTopologyCutoffOrSignals']);s['supervisorClosedZeroDoesNotEstablishJourneySuccess']=True
project(RUN/'supervisor.json','prelaunch/supervisor.fixed-projection.json',s,list(s))
for name in ['execution-entry-counts.json','closed-recorder-validation.json','supplemental-cleanup-qualification.json']:
 x=load(RUN/name)
 fields={'execution-entry-counts.json':['diagnosticOnly','featureAcceptance','nativeInvocationEntries','browserInvocationEntries'],'closed-recorder-validation.json':['diagnosticOnly','featureAcceptance','complete'],'supplemental-cleanup-qualification.json':['diagnosticOnly','nativeNoneAcceptedOnlyWithProvenNoNativeExecution','allEarlyIdentitiesAbsent','noOwnedHostsStarted','safeForOrdinaryRestoration']}[name]
 v=scalar_fields(x,fields)
 if name=='closed-recorder-validation.json':v['noWindowPublished']=True;v['noMetadataCaptureClaimed']=True
 project(RUN/name,'prelaunch/'+name.replace('.json','.fixed-projection.json'),v,fields)
audit=load(RUN/'independent-cleanup-source-audit.json');assert len(audit['identities'])==len(audit['containers'])==1 and all(x['recordedIdentityAbsent']for x in audit['identities'])and all(x['absenceVerified']for x in audit['containers'])
fields=['ordinaryAcceptanceOnly','profilerEnabled','matrixCompleted','singleAttempts','singleVerifiedPositives','multipleAttempts','multipleVerifiedPositives','elapsedIncludesSubsequentEvidenceReads','namespaceContainersAbsent','privateRuntimeDirectoryAbsent','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved','fullLiveManifestSourceCount','fullLiveManifestHashMismatches','allIndependentRecordedProcessAndContainerAbsences','actualBrowserArtifactsRequireIndependentXmlAndPersistencePrivacyReview','diagnosticOnly','featureAcceptance','privateReceiverPhaseOverlay','sdkOrdinary']
v=scalar_fields(audit,fields);v.update(recordedProcessIdentityCount=1,allRecordedProcessIdentitiesAbsent=True,ownedContainerCount=1,allRecordedOwnedContainersAbsent=True,nativeProcessCleanupUnavailableBecauseZeroJourneyNativeEntries=True)
project(RUN/'independent-cleanup-source-audit.json','cleanup/independent-cleanup-source.fixed-projection.json',v,fields+['identities.recordedIdentityAbsent(count only)','containers.absenceVerified(count only)'])
# Full source manifest contains only static tracked paths and digests, not file contents.
manifest=load(RUN/'managed/source-manifest.json');assert all(isinstance(k,str)and not Path(k).is_absolute()and re.fullmatch('[0-9a-f]{64}',v)for k,v in manifest.items())
copy(RUN/'managed/source-manifest.json','source/full-live-source-manifest.json.gz',True)
# Frozen first-run scripts/pins; never touch current retry files.
copy(ARCHIVE/'archive-manifest.json','first-run-ops/archive-manifest.json')
pins=load(OPS/'operational-pins.json');assert len(pins['files'])==44
copy(OPS/'operational-pins.json','first-run-ops/operational-pins.json')
for name,digest in pins['files'].items():
 f=OPS/name;assert sha(f)==digest
 if f.suffix in ['.py','.targets']:copy(f,Path('first-run-ops/source')/name)
for name in ['affected-inventory.json','source-qualification-binding.json','private-directory-proof.json','entry-metadata-contract.json','native-reference-case-set.json','native47-source-derivation.json','native47-plan.json','ordinary-handoff.json','ordinary-restore-plan.json','private-build-plan.json','pipeline-plan.json','operational-plan.json','reused-machinery-provenance.json']:
 copy(OPS/name,Path('first-run-ops/proofs')/name)
for area,count in [('operational-private-j_jwbuv6',13),('operational-ordinary-6c_besa8',14)]:
 directory=OPS/area;commands=load(directory/'commands.json');assert len(commands)==count and all(x['exitCode']==0 for x in commands)
 # These exact build arguments contain no runtime values or environment dump.
 for item in commands:
  assert set(item)=={'command','cwd','startedUtc','finishedUtc','exitCode','log','logSha256'}
  assert all('http://'not in str(a)and'https://'not in str(a)for a in item['command'])
 for f in directory.iterdir():
  if f.suffix=='.log':copy(f,Path('build-restoration')/area/f.name)
 for name in ['commands.json','receipt.json','attempt-closed.json','sdk-proof.json','after-census.json','after-native-artifacts.json','native147-marker-scope.json','independent-dependency-generations.json','six-entry-metadata.json']:
  copy(directory/name,Path('build-restoration')/area/name)
 mapping=load(directory/'mapping-receipt.json');fields=['allSevenEmittedValuesRetained','mappingLogSha256','noNewToolBuild','buildCpu2Only'];project(directory/'mapping-receipt.json',Path('build-restoration')/area/'mapping-receipt.fixed-projection.json',scalar_fields(mapping,fields),fields)
# Native test XML/arguments and scalar outcome. Do not copy build environment values.
native_dir=OPS/'native-private-receiver-na0gtjlz';native=load(native_dir/'receipt.json');assert native['passed']is True and native['caseCount']==47 and len(native['processes'])==4
processes=[];actual=0
for i,row in enumerate(native['processes'],1):
 assert row['passed']is True and row['exitCode']==row['ownedExitCode']==0 and row['processAndDescendantsAbsent']is True and row['remainingOwnedGroupIdentities']==[]
 xml=native_dir/f'selection-{i}.xml';data=xml.read_bytes();assert b'http://'not in data and b'https://'not in data
 tests=ET.fromstring(data).findall('.//test');assert all(t.attrib['result']=='Pass'for t in tests);actual+=len(tests);assert sha(xml)==row['xmlSha256']
 copy(xml,Path('native47')/xml.name)
 fields=['nativeStarted','processAndDescendantsAbsent','mappedPrivateSdk','passed','nativeRuntimeProcessorCountOverrideAbsent','nativeRuntimeCpuUnmodified','exitCode','xmlSha256','logSha256','caseCount','exactRenderedNameMultiplicityCounterAccepted','noFuzzyNormalization','failureBodyValuesPersistedInDerivedReceipt','rawXunitXmlAndLogsMayContainSyntheticSourceLiterals','ownedExitCode']
 assert row['buildOnlyProcessorCount']=='2'
 clean=scalar_fields(row,fields);clean.update(buildOnlyProcessorCount=2,command=row['command'],cwd=row['cwd'],actualParsedDisplayIdentityShaTuples=row['actualParsedDisplayIdentityShaTuples']);processes.append(clean)
assert actual==47
fields=['diagnosticOnly','featureAcceptance','passed','nativeStarted','nativeProcessAndDescendantsAbsent','counterDirectoryEmpty','caseCount','sourceDerivedExpectedCases','historicalAllSelectionRetentionClaimed','ordinaryUniqueNativeCountUnchanged','source56Match','allSelectedProcessesMappedPrivateSdk','exactRenderedNameMultiplicityRequired']
v=scalar_fields(native,fields);v['processes']=processes;v['ordinaryNative1048NotInflated']=True
project(native_dir/'receipt.json','native47/receipt.fixed-projection.json',v,fields+['processes.fixed-listed-scalars/command/cwd/display-hash-tuples'])
# Independent OIDC start-only reproduction, no tokens or readiness implied.
probe=load(PROBE/'receipt.json');assert sha(PROBE/'receipt.json')=='7c01a303fde3a0909a670c2b6a06190b28133fe146f98a5113b883a428f66a47'
assert probe['passed']is True and probe['startSucceeded']is True and probe['tokensRequested']is False and probe['oidcReadinessClaimed']is False and probe['applicationHostsStarted']==probe['nativeOrBrowserCasesStarted']==0
assert probe['rawCommandOutputOrIdentityValuesPersisted']is False and probe['ownedContainerCount']==1 and probe['ownedContainerCleanupVerified']is True
copy(PROBE/'receipt.json','oidc-start-only/receipt.json');copy(Path('/tmp/story65-oidc-start-probe-20261107.py'),'oidc-start-only/probe.py')
assert sha(Path('/tmp/story65-oidc-start-probe-20261107.py'))==probe['probeScriptSha256']
# Bind relevant already-qualified diagnostic source without duplicating the full prior qualification packet.
sourceprep=Path('/tmp/story65-receiver-phase-prep-20261107')
for name in ['TechnicalHttpRouteCatalogReceiver.private.cs','TechnicalHttpRouteCatalogReceiver.original.cs.txt','ReceiverPhaseBootstrap.cs','CaptureCounters.cs','source.patch','source-normalization.json','source-preparation-receipt.json']:
 copy(sourceprep/name,Path('probe-source')/name)
save('outcome.json',{'diagnosticOnly':True,'featureAcceptance':False,'privateBuildCommandsPassed':13,'privateNativeExecutionsPassed':47,'launcherControlsPassed':142,'managedLauncherExitCode':1,'applicationHostsStarted':0,'nativeJourneyEntries':0,'browserEntries':0,'counterWindowPublished':False,'counterCapturesClaimed':0,'managedJourneySucceeded':False,'supervisorExitZeroIsNotJourneySuccess':True,'ordinaryRestorationBuildsPassed':14,'ordinarySdkSha256':parent['sdkSha256'],'ordinarySdkPdbSha256':parent['sdkPdbSha256'],'ordinarySdkCensusCopies':137,'sourceControlsUnchanged':56,'fullLiveSourceManifestCount':len(manifest),'fullLiveSourceHashMismatches':0,'fixedPrivateDirectoryEmptyAndIdentityVerifiedByRoot':True,'priorNative147BaselineDifferences':17,'historical10DependencyProvenanceLimitRetained':True,'ordinaryNative1048NotInflated':True,'oidcStartOnlyReproductionPassed':True,'oidcReadinessOrTokenOrHostSuccessClaimed':False,'packetCreationStartsNoBuildHostProbeOrCleanup':True})
save('copy-provenance.json',provenance)
# Runtime URLs/CIDs/auth/env/errors are deliberately omitted, not redacted into misleading raw copies.
save('omitted-private-inputs.json',{'omissions':[{'originalPath':str(RUN/'managed/run.json'),'originalSha256':sha(RUN/'managed/run.json'),'handling':'fixed allowlist projection only; no raw copy'},{'originalPath':str(RUN/'launcher-console.log'),'originalSha256':sha(RUN/'launcher-console.log'),'handling':'hash only; no raw output'},{'originalPath':str(RUN/'managed-command.json'),'originalSha256':sha(RUN/'managed-command.json'),'handling':'hash only; managed command may carry private coordinates'},{'originalPath':str(RUN/'cleanup-observation-1.json'),'originalSha256':sha(RUN/'cleanup-observation-1.json'),'handling':'use equivalent final count/absence projection; no raw identities/CIDs'}],'noRawRuntimeUrlAuthEnvErrorOutputOrContainerIdCopies':True})
print(json.dumps({'copiedOrProjectedArtifacts':len(provenance),'nativeXmlPasses':actual,'sourceManifestPaths':len(manifest),'counterWindowPublished':False,'featureAcceptance':False}))
