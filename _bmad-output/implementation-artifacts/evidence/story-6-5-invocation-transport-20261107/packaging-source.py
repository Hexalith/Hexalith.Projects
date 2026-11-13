from pathlib import Path
import collections,datetime,gzip,hashlib,importlib.util,json,math,os,re,shutil,stat,statistics
R=Path('/home/administrator/projects/hexalith/projects');B=Path('/tmp/story65-invocation-transport-evidence-20261107');P=Path('/tmp/story65-invocation-transport-evidence-complete-20261107');O=Path('/tmp/story65-invocation-transport-ops-prep-20261107/live');E=Path('/tmp/story65-invocation-transport-diagnostic-20261107');Q=Path('/tmp/story65-invocation-transport-qualification-20261107')
A={'Whole':'WholeAfterAdmission','ComponentFile':'Transport','ComponentPolicy':'OwnedLeaseDispose'};Z={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'};F=Z|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
def sha(f):
 with Path(f).open('rb')as s:return hashlib.file_digest(s,'sha256').hexdigest()
def shab(b):return hashlib.sha256(b).hexdigest()
def strict(b):
 def pairs(rows):
  d={}
  for k,v in rows:assert k not in d;d[k]=v
  return d
 return json.loads(b,object_pairs_hook=pairs)
def save(n,v):
 f=P/n;f.parent.mkdir(mode=0o700,parents=True,exist_ok=True);f.write_text(json.dumps(v,indent=2)+'\n')
def put(source,name):
 source=Path(source);assert source.is_file()and not source.is_symlink()and source.suffix.lower()not in['.dll','.pdb','.exe','.pyc','.cache','.nettrace','.etlx'];f=P/name;f.parent.mkdir(mode=0o700,parents=True,exist_ok=True)
 if f.exists():assert sha(f)==sha(source)
 else:shutil.copyfile(source,f)
 assert sha(f)==sha(source)
def absent(pid,ticks):
 try:a=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return a[0]=='Z'or a[19]!=str(ticks)
 except FileNotFoundError:return True
def main():
 assert not P.exists();P.mkdir(mode=0o700)
 assert sha(B/'staging-manifest.json')=='b7b99f4456e3724e8c3a3e2efe8caed061080fa6f210070306f6c776e1223892'
 assert sha(B/'SHA256SUMS')=='97ed23c42bda2179a41942f162574fafdca00de7fbfde398e03aa98385af4915'
 initial=[];synthetic_rows=0;gzs=0
 for line in(B/'SHA256SUMS').read_text().splitlines():
  h,n=line.split('  ',1);assert sha(B/n)==h;put(B/n,Path('qualified-scratch')/n);initial.append(n)
  if n.endswith('.jsonl.gz'):
   data=gzip.decompress((B/n).read_bytes());gzs+=1;synthetic_rows+=len(data.splitlines())
   for line in data.splitlines():
    row=strict(line);assert set(row)==F and row['role']=='gateway'and row['stage']in A and all(type(row[k])is int for k in F-{'role','stage'})and all(row[k]==0 for k in Z)
 assert len(initial)==483 and gzs==11 and synthetic_rows==83662
 put(B/'SHA256SUMS',Path('qualified-scratch/original-SHA256SUMS'))
 save('partial-generation-preservation.json',{'originalPartialPath':str(B),'originalStagingManifestSha256':sha(B/'staging-manifest.json'),'originalChecksumSha256':sha(B/'SHA256SUMS'),'copiedVerifiedChecksumEntries':len(initial),'originalPartialNotModified':True,'pendingLabelsInPartialAreHistorical':True,'syntheticGzipFiles':gzs,'syntheticStrictRows':synthetic_rows})
 for f in sorted(O.rglob('*')):
  if not f.is_file()or'__pycache__'in f.parts:continue
  assert not f.is_symlink()and f.suffix in['.py','.json','.targets','.md','.diff','.log','.xml'];put(f,Path('live-operations')/f.relative_to(O))
 for f in E.iterdir():
  if f.is_file():assert f.suffix in['.json','.log'];put(f,Path('managed')/f.name)
 selected={'run.json','source-manifest.json','browser-seed.xml','browser-seed.log','launcher-controls.log','seed-progress.log','writer-protocol-cutover.json','bootstrap-command-status.json','bootstrap-delivery.json'}
 for f in(E/'managed').iterdir():
  if f.is_file()and(f.name in selected or re.fullmatch(r'build-[0-9]+\.log',f.name)):put(f,Path('managed/artifacts')/f.name)
 fields=['multiple','attempt','elapsedMilliseconds','httpStatus','result','incomplete','candidateCount','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied'];raw=(E/'managed/browser-advisory-seed-readiness.jsonl').read_bytes();rows=[]
 for line in raw.splitlines():
  source=strict(line);d={k:source[k]for k in fields};assert all(type(d[k])is int for k in['attempt','httpStatus','candidateCount'])and type(d['elapsedMilliseconds'])in[int,float]and math.isfinite(d['elapsedMilliseconds']);assert d['result']in[None,'Empty','SingleCandidate','MultipleCandidates'];assert all(type(d[k])is bool for k in fields if k not in['attempt','httpStatus','candidateCount','elapsedMilliseconds','result']);rows.append(d)
 (P/'managed/artifacts/advisory-readiness-scalars.jsonl').write_text(''.join(json.dumps(d,separators=(',',':'))+'\n'for d in rows));save('managed/scalar-readiness-provenance.json',{'originalSha256':shab(raw),'originalBytes':len(raw),'rows':len(rows),'retainedFields':fields,'fixtureIndexDetailsOwnerStatesGenerationObjectsExcluded':True,'noJoinToSdkOrdinals':True})
 run=strict((E/'managed/run.json').read_bytes());status=strict((O/'latest-operational-result.json').read_bytes());assert all(re.fullmatch(r'[0-9a-f]{40}',v)for v in run['revisions'].values());assert status['privateExitCode']==status['native33ExitCode']==status['managedExitCode']==status['ordinaryExitCode']==0
 assert sum(not d['multiple']for d in rows)==2 and sum(d['multiple']for d in rows)==30
 single=[d['attempt']for d in rows if not d['multiple']and d['positiveCandidatesVerified']and not d['incomplete']];multi=[d['attempt']for d in rows if d['multiple']and d['positiveCandidatesVerified']and not d['incomplete']];assert single==[2]and multi==[]
 save('actual-managed-result.json',{'diagnosticOnly':True,'featureAcceptance':False,'managedResult':run['result'],'pipelineExecutionExitCode':0,'underlyingManagedLauncherExitCode':1,'supervisorExitCode':0,'singleAttempts':2,'singleVerifiedPositiveAttempts':single,'multipleAttempts':30,'multipleVerifiedPositiveAttempts':multi,'browserCases':0,'authenticatedBrowserPersistencePrivacyAccessibilityAcceptance':'not exercised; native seed failed','privateBrowserSuccessWouldStillBeDiagnostic':True,'trueAcceptanceRequiresLaterOrdinaryRun':True,'canonicalRevisions':run['revisions']})
 raw=(E/'counter-archive/gateway.jsonl').read_bytes();assert shab(raw)=='145c3161584582aa44b8bd571d9a997d540a20b928f0e2de41f78181c86b944a';compressed=gzip.compress(raw,mtime=0);dest=P/'actual-sdk/gateway.jsonl.gz';dest.parent.mkdir(mode=0o700,parents=True);dest.write_bytes(compressed);assert gzip.decompress(compressed)==raw
 put(E/'counter-archive/gateway.closed.json',Path('actual-sdk/gateway.closed.json'));save('actual-sdk/raw-compression-receipt.json',{'originalRawSha256':shab(raw),'originalRawBytes':len(raw),'gzipSha256':shab(compressed),'gzipBytes':len(compressed),'decompressionByteIdentityVerified':True,'rawDuplicateNotRetained':True})
 aggregate(raw);verify_outputs()
 for f in Path('/tmp').glob('story65-invocation-transport-parent-*.py'):put(f,Path('parent-scripts')/f.name)
 for f in Path('/tmp/story65-invocation-transport-native-plan-20261107').iterdir():
  if f.is_file():put(f,Path('native-carrier-metadata-plan')/f.name)
 save('archive-validation-correction-history.json',{'initialFailureType':'KeyError','initialMissingStatusKey':'native29ExitCode','correctStatusKey':'native33ExitCode','failureBeforeAnyPrivateFileAccess':True,'beforeAndCorrectedParentScriptsRetained':True,'noCounterCopyOrRemovalByFailedAttempt':True,'correctedAttemptGuardedValidatedAndRemovedExact3':True,'noApplicationOrNativeRetestForArchiveFix':True})
 print(json.dumps({'packetStaged':str(P),'actualRows':1584,'actualCaptures':1542,'allGzipRows':85246}))
def aggregate(raw):
 closed=strict((P/'actual-sdk/gateway.closed.json').read_bytes());assert all(type(v)is int for k,v in closed.items()if k!='role')and closed['role']=='gateway';assert closed['admitted']==closed['disposedOrUnwound']==1542 and closed['writtenRows']==1584 and closed['writtenBytes']==len(raw)==502360
 assert closed['countersFinal']==1 and all(closed[k]==0 for k in['activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease']);assert closed['windowEndTicks']-closed['windowStartTicks']==8*closed['frequency']
 groups=collections.defaultdict(dict);values=collections.defaultdict(list);counts=collections.Counter()
 for line in raw.splitlines():
  d=strict(line);assert set(d)==F and d['role']=='gateway'and d['stage']in A and all(type(d[k])is int for k in F-{'role','stage'})and all(d[k]==0 for k in Z)and d['runOrdinal']==closed['runOrdinal']and d['frequency']==closed['frequency']and d['captureOrdinal']>0 and d['elapsedTicks']>=0
  k=A[d['stage']];assert k not in groups[d['captureOrdinal']];v=1000*d['elapsedTicks']/d['frequency'];groups[d['captureOrdinal']][k]=v;values[k].append(v);counts[d['stage']]+=1
 assert len(groups)==1542 and counts=={'Whole':1542,'ComponentFile':21,'ComponentPolicy':21}
 def stats(a):
  a=sorted(a);return{'count':len(a),'medianMs':statistics.median(a),'minMs':min(a),'p95NearestRankMs':a[math.ceil(.95*len(a))-1],'maxMs':max(a)}
 conditioned=[]
 for mask,expected in[(['WholeAfterAdmission'],1521),(['OwnedLeaseDispose','Transport','WholeAfterAdmission'],21)]:
  selected=[d for d in groups.values()if sorted(d)==sorted(mask)];assert len(selected)==expected;conditioned.append({'numericStagePresence':mask,'captures':len(selected),'millisecondsByStage':{k:stats([d[k]for d in selected])for k in mask},'noRequestKindResultCancellationOrOwnershipOutcomeInference':True})
 parent=strict((E/'parent-counter-summary.json').read_bytes());assert parent['originalRawSha256']==shab(raw)and parent['captures']==1542 and parent['rows']==1584
 # Parent serializes semantic stage labels; match by numeric helper alias where available.
 save('actual-sdk/schema-and-aggregation.json',{'diagnosticOnly':True,'featureAcceptance':False,'strictDuplicateFree14FieldRows':True,'allCountAllocationColumnsZero':True,'captures':1542,'rows':1584,'finalNoDropsIoCapsActiveOrQueuedWork':True,'aliases':A,'globalMillisecondsByStage':{k:stats(v)for k,v in values.items()},'conditionedOnlyOnNumericStagePresence':conditioned,'sameLocalCapturePhases':[{'opaqueLocalOrdinal':o,'milliseconds':d}for o,d in sorted(groups.items())],'populationEveryEntryIntoEligibleSdkHandlerIncludingOrdinaryControlOwnerStateAndPotentialIssuerClients':True,'noIssuerClientExclusionInherited':True,'wholeExcludesEligibilityAndAdmission':True,'transportOnlyExactLocalSendLeasedAwaitExcludesOuterBuffering':True,'ownedDisposeDoesNotProveCallbackOrFenceRemoval':True,'possibleRecursiveWholeCapturesOverlap':True,'residualIsUnattributed':True,'noPriorProbePopulationRequestRootOrOutcomeJoin':True,'noNetworkReceiverCpuOrdinaryBenefitClaimOrOverheadSubtraction':True})
 archive=strict((E/'parent-counter-archive-cleanup.json').read_bytes());assert archive['rows']==1584 and archive['privateDirectoryEmpty']is True and archive['controlContentsNotArchived']is True and archive['artifacts']['gateway.jsonl']['sha256']==shab(raw)
def verify_outputs():
 spec=importlib.util.spec_from_file_location('sdk_live_packet_audit',O/'ops_common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);source=m.sources();sdk=m.sdk_proof('ordinary');deps=m.independent_dependencies();n=m.native147_proof();actual=m.census()
 private=Path(strict((O/'latest-private-attempt.json').read_bytes())['attemptDirectory']);native=Path(strict((O/'latest-native-attempt.json').read_bytes())['attemptDirectory']);ordinary=list(O.glob('operational-ordinary-*'));assert len(ordinary)==1;ordinary=ordinary[0];affected=set(strict((ordinary/'affected-paths-union.json').read_bytes()));initial={d['path']:d for d in strict((private/'before-census.json').read_bytes())};kinds={k:sha(m.ordinary_expected(k))for k in['full','pdb','ref','refint']};assert len(actual)==137 and len(affected)==32
 for row in actual:assert not row['privateMarker']and row['sha256']==(kinds[row['kind']]if row['path']in affected else initial[row['path']]['sha256'])
 commands=strict((ordinary/'commands.json').read_bytes());assert len(commands)==14 and all(d['exitCode']==0 for d in commands)
 gate=strict((native/'receipt.json').read_bytes());assert gate['passed']is True and gate['caseCount']==33 and len(gate['processes'])==7 and all(d['passed']and d['mappedPrivateSdk']and d['processAndDescendantsAbsent']for d in gate['processes']);assert[d['caseCount']for d in gate['processes']]==[7,1,1,1,2,12,9]
 native_rows=m.native_artifacts();assert len(native_rows)==6 and all(not d['privateMarker']for d in native_rows)
 generic=strict((E/'independent-cleanup-source-audit.json').read_bytes());assert len(generic['identities'])==21 and all(absent(d['pid'],d['startTicks'])for d in generic['identities']);assert all(absent(d['pid'],d['startTicks'])for d in gate['processes'])
 q=strict((Q/'final-scratch-qualification.json').read_bytes());assert len(q['sourceSha256'])==55 and all(sha(R/name)==h for name,h in q['sourceSha256'].items());manifest=strict((E/'managed/source-manifest.json').read_bytes());assert len(manifest)==17330 and all(sha(R/name)==h for name,h in manifest.items())
 save('source-preservation.json',{'all55SourcesMatchAfterOrdinaryRestoration':True,'source55Sha256':q['sourceSha256'],'fullManifestCount':17330,'fullManifestHashMismatches':0,'privateSource26c6Helper112Bootstrap40ebUnchanged':True,'all14BuildProjectHashesMatch':True,'noProductSourceGitBuildHostSignalChangesByPackaging':True})
 save('independent-restoration-source-audit.json',{'diagnosticOnly':True,'featureAcceptance':False,'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source55':source,'sdk137MarkerClean':True,'affected32SdkFullPdbRefRefintCoherent':True,'unrelated105ExactBaselineIncludingLegacySdk':True,'currentOrdinarySdkKindsSha256':kinds,'sdk8ScopedCoherent':sdk,'independentDependencyGenerations':deps,'native147':n,'allSixNativeArtifactsMarkerClean':True,'all14OrdinaryBuildsExitZero':True,'all21RecordedRuntimeKernelIdentitiesAbsentNow':True,'allSevenNativeKernelIdentitiesAbsentNow':True,'parentGroupContainerNamespacePrivatePreservationProofRetained':True,'noBuildTestsAssemblyLoadHostSignalsOrProductSourceWrites':True})
 save('qualification-count-boundaries.json',{'ordinaryUniqueNativePassesUnchanged':1038,'scratchEligibleCaseComparisons':80,'scratchActualSendAsyncEntriesPerVariant':84,'scratchFreshCanaries':3,'scratchEligibleProcesses':15,'scratchGenuineExcludedProcesses':1,'scratchExcludedCaseComparisons':20,'scratchExcludedActualEntriesPerVariant':21,'scratchAllControlledProcesses':16,'scratchStrictRows':83662,'actualNative33DiagnosticPassed':True,'nativeProcessCaseCounts':[7,1,1,1,2,12,9],'strict33DistinctParsedXmlIdentitiesWithMultisetEnforced':True,'historicalCorroborationOnly26NotAll33':True,'sourceOnlyPlannedOutgoingCases7NowActuallyExecuted':True,'native33NotClaimedSubsetOrExtraUniqueOrdinary1038':True,'featureAcceptance':False})
 d=Path('/tmp/story65-capture-counter-private-20261107');fd=os.open(d,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and not os.listdir(fd)
 finally:os.close(fd)
 save('independent-counter-directory-recheck.json',{'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'device':2096,'inode':8752986,'uid':1000,'mode':'0700','samePinnedIdentity':True,'empty':True,'counterAndControlFilesNotTouchedByPackaging':True})
if __name__=='__main__':main()
