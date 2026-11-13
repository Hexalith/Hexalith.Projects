from pathlib import Path
import collections,datetime,gzip,hashlib,importlib.util,json,math,os,re,shutil,stat,statistics
R=Path('/home/administrator/projects/hexalith/projects');B=Path('/tmp/story65-dapr-invoker-evidence-20261107');P=Path('/tmp/story65-dapr-invoker-evidence-complete-20261107');BASE=Path('/tmp/story65-dapr-invoker-ops-prep-20261107');O=BASE/'live-xml-reporter-retry';OLD=BASE/'live';H=BASE/'history/executed-d9eae03d9621474d';E=Path('/tmp/story65-dapr-invoker-diagnostic-retry-20261107');Q=Path('/tmp/story65-dapr-invoker-qualification-20261107');D=Path('/tmp/story65-capture-counter-private-20261107')
A={'Whole':'WholeInvoker','ComponentFile':'CatalogAcquire','ComponentPolicy':'ResolveAndRegistration','AclFile':'DaprRequestAndAuthorityBinding','SettingsEnumerationSortDictionary':'CreateClientAndSendDefaultBuffering','AuthenticationSerialization':'JsonBody','ActorBoundary':'OverlappingHttpOwnership','ResultTail':'OverlappingLeaseOwnership'}
Z={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'};F=Z|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
def sha(f):
 with Path(f).open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def shab(b):return hashlib.sha256(b).hexdigest()
def strict(raw):
 def pairs(items):
  d={}
  for k,v in items:assert k not in d;d[k]=v
  return d
 return json.loads(raw,object_pairs_hook=pairs)
def save(n,d):
 p=P/n;p.parent.mkdir(mode=0o700,parents=True,exist_ok=True);p.write_text(json.dumps(d,indent=2)+'\n')
def put(f,n):
 f=Path(f);assert f.is_file()and not f.is_symlink()and f.suffix.lower()not in['.dll','.pdb','.cache','.nettrace','.etlx'];p=P/n;p.parent.mkdir(mode=0o700,parents=True,exist_ok=True)
 if p.exists():assert sha(f)==sha(p)
 else:shutil.copyfile(f,p)
 assert sha(f)==sha(p)
def metadata_tree(source,target,suffixes):
 for f in sorted(source.rglob('*')):
  assert not f.is_symlink()
  if f.is_file():assert f.suffix in suffixes,(str(f),f.suffix);put(f,target/f.relative_to(source))
def stage():
 assert sha(B/'staging-manifest.json')=='5486b6a5ef2d7aaa33c28314be19ba2b56b62d35f34d67cb6afbcf3cb70e4ba4';partial=strict((B/'staging-manifest.json').read_bytes());assert len(partial['files'])==307;assert not P.is_symlink();P.mkdir(mode=0o700,exist_ok=True)
 for name,info in partial['files'].items():assert sha(B/name)==info['sha256']and(B/name).stat().st_size==info['bytes'];put(B/name,Path('qualified-preparation')/name)
 put(B/'staging-manifest.json',Path('qualified-preparation/staging-manifest.json'));save('partial-generation-preservation.json',{'originalPartialPath':str(B),'originalStagingManifestSha256':sha(B/'staging-manifest.json'),'copiedVerifiedEntries':307,'copiedManifestUnchanged':True,'originalPartialNotModified':True,'pendingLabelsInPartialAreHistorical':True})
 metadata_tree(H,Path('initial-executed-generation'),{'.json','.log','.xml','.py','.targets','.md','.cs','.csproj'})
 for f in sorted(O.rglob('*')):
  if not f.is_file()or '__pycache__'in f.parts:continue
  assert not f.is_symlink()
  if any(parent.name.startswith(('operational-private-','operational-ordinary-','native-private-'))for parent in f.parents if parent!=O):continue
  assert f.suffix in['.py','.json','.targets','.md','.diff','.cs','.csproj'];put(f,Path('retry/preparation')/f.relative_to(O))
 for prefix in ['operational-private-','native-private-','operational-ordinary-']:
  folders=list(O.glob(prefix+'*'));assert len(folders)==1
  metadata_tree(folders[0],Path('retry/execution')/folders[0].name,{'.json','.log','.xml'})
 for f in E.iterdir():
  if f.is_file():assert f.suffix in['.json','.log'];put(f,Path('managed')/f.name)
 for name in ['gateway.jsonl','gateway.closed.json']:put(E/'counter'/name,Path('actual-invoker')/name)
 selected={'run.json','source-manifest.json','browser-seed.xml','browser-seed.log','launcher-controls.log','seed-progress.log','writer-protocol-cutover.json','bootstrap-command-status.json','bootstrap-delivery.json'}
 for f in(E/'managed').iterdir():
  if f.is_file()and(f.name in selected or re.fullmatch(r'build-[0-9]+\.log',f.name)):put(f,Path('managed/artifacts')/f.name)
 raw=(E/'managed/browser-advisory-seed-readiness.jsonl').read_bytes();rows=[strict(line)for line in raw.splitlines()];fields=['multiple','attempt','elapsedMilliseconds','httpStatus','result','incomplete','candidateCount','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied'];safe=[]
 for row in rows:
  d={k:row[k]for k in fields};assert type(d['attempt'])is int and type(d['httpStatus'])is int and type(d['candidateCount'])is int and type(d['elapsedMilliseconds'])in[int,float]and math.isfinite(d['elapsedMilliseconds']);assert d['result']in[None,'Empty','SingleCandidate','MultipleCandidates'];assert all(type(d[k])is bool for k in ['multiple','incomplete','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied']);safe.append(d)
 f=P/'managed/artifacts/advisory-readiness-scalars.jsonl';f.write_text(''.join(json.dumps(d,separators=(',',':'))+'\n'for d in safe));save('managed/scalar-readiness-provenance.json',{'originalSha256':shab(raw),'originalBytes':len(raw),'rows':len(safe),'retainedFields':fields,'fixtureIndexDetailsOwnerStatesGenerationObjectsExcluded':True,'noJoinToInvokerOrdinals':True})
 run=strict((E/'managed/run.json').read_bytes());assert all(re.fullmatch('[0-9a-f]{40}',v)for v in run['revisions'].values())
 save('actual-managed-result.json',{'diagnosticOnly':True,'featureAcceptance':False,'managedResult':run['result'],'originalFailure':run['failure'],'singleAttempts':sum(not x['multiple']for x in safe),'singleVerifiedPositiveAttempts':[x['attempt']for x in safe if not x['multiple']and x['positiveCandidatesVerified']],'multipleAttempts':sum(x['multiple']for x in safe),'multipleVerifiedPositiveAttempts':[x['attempt']for x in safe if x['multiple']and x['positiveCandidatesVerified']],'browserCases':0,'authenticatedBrowserPersistencePrivacyAccessibilityAcceptance':'not exercised; native seed failed','privateBrowserSuccessWouldStillBeDiagnostic':True,'trueAcceptanceRequiresLaterOrdinaryRun':True,'canonicalRevisions':run['revisions']})
 aggregate();verify_outputs()
 print(json.dumps({'packetStaged':str(P),'partialPreserved':307,'actualCaptures':25,'actualRows':186}))
def aggregate():
 data=(P/'actual-invoker/gateway.jsonl').read_bytes();closed=strict((P/'actual-invoker/gateway.closed.json').read_bytes());assert all(type(v)is int for k,v in closed.items()if k!='role')and closed['role']=='gateway';assert closed['admitted']==closed['disposedOrUnwound']==25 and closed['writtenRows']==186 and closed['writtenBytes']==len(data)==60858
 assert closed['countersFinal']==1 and all(closed[k]==0 for k in ['activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease']);assert closed['windowEndTicks']-closed['windowStartTicks']==8*closed['frequency']
 groups=collections.defaultdict(dict);values=collections.defaultdict(list);counts=collections.Counter()
 for line in data.splitlines():
  row=strict(line);assert set(row)==F and row['role']=='gateway'and row['stage']in A and all(type(row[k])is int for k in F-{'role','stage'})and all(row[k]==0 for k in Z)and row['runOrdinal']==closed['runOrdinal']and row['frequency']==closed['frequency']and row['elapsedTicks']>=0 and row['captureOrdinal']>0
  label=A[row['stage']];assert label not in groups[row['captureOrdinal']];ms=row['elapsedTicks']*1000/row['frequency'];groups[row['captureOrdinal']][label]=ms;values[label].append(ms);counts[row['stage']]+=1
 assert counts=={'Whole':25,'ComponentFile':25,'ComponentPolicy':23,'AclFile':23,'SettingsEnumerationSortDictionary':23,'AuthenticationSerialization':21,'ActorBoundary':23,'ResultTail':23}and len(groups)==25
 stage_sets=collections.Counter(tuple(k for k in A.values()if k in group)for group in groups.values());assert sorted(stage_sets.values())==[2,2,21]
 def stats(a):
  s=sorted(a);return{'count':len(s),'medianMs':statistics.median(s),'minMs':min(s),'p95NearestRankMs':s[math.ceil(.95*len(s))-1],'maxMs':max(s)}
 paired=[{'opaqueLocalOrdinal':ordinal,'phasesMilliseconds':group,'allEightStages':len(group)==8}for ordinal,group in sorted(groups.items())]
 shares={key:{name.replace('Ms','Percent'):value for name,value in stats([100*group[key]/group['WholeInvoker']for group in groups.values()if key in group]).items()}for key in A.values()if key!='WholeInvoker'}
 summary={'diagnosticOnly':True,'featureAcceptance':False,'strictDuplicateFree14FieldRows':True,'allZeroCountAllocationColumns':sorted(Z),'captures':25,'rows':186,'closedReceiptFinalNoDropsIoCapsOrActiveWork':True,'aliases':A,'millisecondsByStage':{key:stats(a)for key,a in values.items()},'stageSetCounts':[{'stages':list(keys),'captures':count}for keys,count in stage_sets.items()],'withinSameLocalCapturePercentOfWhole':shares,'pairedLocalCaptures':paired,'eligiblePopulationAllActualInvokerEntriesIncludingOuterAdvisoryAndNullGuard':True,'stage6Overlaps345':True,'stage7Overlaps23456':True,'noExactReleaseResidualOrOverlappingSum':True,'noMissingStageOutcomeOrCancellationInference':True,'noControllerClientPriorRunRootRequestOrDownstreamCorrelation':True,'sendIncludesDefaultResponseContentReadBuffering':True,'noNetworkReceiverServerActorCpuOrOrdinaryLatencyBenefitClaim':True,'noOverheadSubtraction':True}
 save('actual-invoker/schema-and-aggregation.json',summary)
 parent=strict((E/'parent-counter-summary.json').read_bytes());assert parent['captures']==25 and parent['rows']==186 and parent['originalRawSha256']==shab(data)
 for label,vals in parent['millisecondsByStage'].items():assert abs(vals['median']-statistics.median(values[label]))<1e-9
 assert(E/'parent-counter-summary.json').read_bytes()==(E/'parent-counter-summary-archived.json').read_bytes()
 cleanup=strict((E/'parent-counter-archive-cleanup.json').read_bytes());assert cleanup['artifacts']['gateway.jsonl']['sha256']==shab(data)and cleanup['artifacts']['gateway.closed.json']['sha256']==sha(P/'actual-invoker/gateway.closed.json')and cleanup['controlContentsArchived']is False
 save('actual-invoker/archive-and-parent-summary-validation.json',{'rawSha256':shab(data),'rawBytes':len(data),'allParentStageMediansAgree':True,'parentSummaryBeforeAndAfterArchiveByteIdentical':True,'archivedHashesMatchRootGuardedCleanupProof':True,'controlContentsNotCopied':True,'allStageSetAndPrimitiveSchemaChecksPass':True})
def verify_outputs():
 spec=importlib.util.spec_from_file_location('live_output_audit',O/'ops_common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 source=m.sources();sdk=m.ordinary_sdk_proof();domain=m.ordinary_domain_proof();server=m.ordinary_server_proof();native147=m.native147_marker_absence();native_sdk=m.native_sdk_proof();actual=m.census()
 private=Path(strict((O/'latest-private-attempt.json').read_bytes())['attemptDirectory']);native=Path(strict((O/'latest-native-attempt.json').read_bytes())['attemptDirectory']);ordinary=list(O.glob('operational-ordinary-*'));assert len(ordinary)==1;ordinary=ordinary[0]
 affected=set(strict((ordinary/'affected-paths-union.json').read_bytes()));initial={row['path']:row for row in strict((private/'before-census.json').read_bytes())};kinds={kind:sha(m.ordinary_expected(kind))for kind in ['full','pdb','ref','refint']};assert len(actual)==34 and len(affected)==20 and all(not row['privateMarker']for row in actual)
 for row in actual:assert row['sha256']==(kinds[row['kind']]if row['path']in affected else initial[row['path']]['sha256'])
 commands=strict((ordinary/'commands.json').read_bytes());assert len(commands)==11 and all(row['exitCode']==0 for row in commands)
 native_rows=m.native_artifacts();assert len(native_rows)==6 and all(not row['privateMarker']for row in native_rows)
 native_project=R/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests'
 for row in native_rows:
  file=R/row['path'];kind='pdb'if file.suffix=='.pdb'else'refint'if'refint'in file.parts else'ref'if'ref'in file.parts else'full'
  expected=native_project/('obj/Debug/net10.0/'+kind+'/Hexalith.EventStore.Server.Tests.dll'if kind in['ref','refint']else'bin/Debug/net10.0/Hexalith.EventStore.Server.Tests'+('.pdb'if kind=='pdb'else'.dll'))
  assert sha(file)==sha(expected)
 gate=strict((native/'receipt.json').read_bytes());assert gate['passed']and gate['caseCount']==15 and len(gate['processes'])==3 and all(row['passed']and row['mappedPrivateGateway']and row['processAndDescendantsAbsent']for row in gate['processes']);assert[case['caseCount']for case in gate['processes']]==[5,8,2]
 def absent(pid,ticks):
  try:state=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return state[0]=='Z'or state[19]!=str(ticks)
  except FileNotFoundError:return True
 generic=strict((E/'independent-cleanup-source-audit.json').read_bytes());assert len(generic['identities'])==21 and all(absent(row['pid'],row['startTicks'])for row in generic['identities'])
 oldnative=Path(strict((OLD/'latest-native-attempt.json').read_bytes())['attemptDirectory']);first=strict((oldnative/'receipt.json').read_bytes());assert first['passed']is False and len(first['processes'])==1 and first['processes'][0]['exitCode']==0 and first['processes'][0]['mappedPrivateGateway']is True
 assert all(absent(row['pid'],row['startTicks'])for row in first['processes']+gate['processes'])
 q=strict((Q/'final-scratch-qualification.json').read_bytes());assert len(q['sourceSha256'])==53 and all(sha(R/path)==digest for path,digest in q['sourceSha256'].items())
 manifest=strict((E/'managed/source-manifest.json').read_bytes());assert len(manifest)==17318 and all(sha(R/path)==digest for path,digest in manifest.items())
 save('source-preservation.json',{'all53ProductionAndControlSourcesMatchAfterOrdinaryRestoration':True,'source53Sha256':q['sourceSha256'],'fullLiveManifestCount':len(manifest),'fullLiveManifestHashMismatches':0,'privateSourceE7c9Helper112Bootstrap05bbbUnchanged':True,'noProductSourceGitBuildHostSignalChangesByPackaging':True})
 save('independent-restoration-source-audit.json',{'diagnosticOnly':True,'featureAcceptance':False,'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source53':source,'gateway34MarkerClean':True,'affected20FullPdbRefRefintCoherent':True,'unrelated14ExactPreRetryBaseline':True,'ordinaryNewGatewayGenerationSha256':kinds['full'],'ordinaryKindsSha256':kinds,'notClaimedByteEquivalentToPriorF629Or2a67Generations':True,'sdk8':sdk,'additionalNativeSdk':native_sdk,'domain98':domain,'server10Scoped':server,'native147':native147,'nativeTestArtifactsCount':len(native_rows),'allSixNativeTestArtifactsCoherentAndMarkerClean':True,'all11OrdinaryBuildsExitedZero':True,'all21RecordedRuntimeKernelIdentitiesAbsentNow':True,'all4InitialAndRetryNativeKernelIdentitiesAbsentNow':True,'parentContainerGroupNamespacePrivatePreservationProofRetained':'managed/parent-counter-archive-cleanup.json','noBuildTestsAssemblyLoadHostSignalsOrProductSourceWrites':True})
 save('qualification-count-boundaries.json',{'ordinaryUniqueNativePassesUnchanged':1038,'owner412PlusProjects626':True,'scratchBehavioralCells':120,'scratchFreshCanaries':3,'scratchProcesses':19,'scratchStrictRows':111752,'initialNativeFirstGroup5PassButExactNameGateRejected':True,'initialNative15NotQualified':True,'correctedActualNative15DiagnosticPassed':True,'actualThreeProcessCaseCounts':[5,8,2],'native15SourceDerivedNotClaimedHistoricalRetainedOrSubsetOf1038':True,'no15AdditionToOrdinaryUniqueCount':True,'featureAcceptance':False})
 fd=os.open(D,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and os.listdir(fd)==[]
 finally:os.close(fd)
 save('independent-counter-directory-recheck.json',{'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'device':2096,'inode':8752986,'uid':1000,'mode':'0700','samePinnedIdentity':True,'empty':True,'noCounterOrControlFilesTouchedByPackaging':True})
if __name__=='__main__':stage()

def finish():
 put('/tmp/story65-dapr-invoker-complete-package.py',Path('packaging-source.py'))
 forbidden=[];matches=[];jwt=rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}'
 for file in P.rglob('*'):
  if not file.is_file():continue
  assert not file.is_symlink()
  if file.suffix.lower()in['.dll','.pdb','.pfx','.pem','.key','.der','.nettrace','.etlx','.cache']or file.name in['window.control','fixture.json','endpoints.json','apphost-session.json']:forbidden.append(str(file.relative_to(P)))
  data=gzip.decompress(file.read_bytes())if file.suffix=='.gz'else file.read_bytes()
  if re.search(jwt,data)or(bytes([45])*5+b'BEGIN PRIVATE KEY'+bytes([45])*5)in data or(bytes([45])*5+b'BEGIN RSA PRIVATE KEY'+bytes([45])*5)in data:matches.append(str(file.relative_to(P)))
 assert not forbidden and not matches
 save('privacy-and-exclusions.json',{'featureAcceptance':False,'strictActualPrimitiveRowsAndAllZeroCountAllocation':True,'all25Captures186RowsFinalNoDropsIoOrCaps':True,'partial307ManifestPreservedByteForByte':True,'oldExecuted83AndOriginalPinsUnchanged':True,'initialXmlGateFailureRetained':True,'exactTwoLayerNativeNameFixNotFuzzy':True,'syntheticFixtureLiteralScopeInNativeXmlAndLogsExplicit':True,'parentGuardedArchivedOnlyExact2CounterFilesRemovedExact3':True,'numericControlContentsNotCopied':True,'fixedSamePrivateDirectoryEmpty':True,'noApplicationBinariesCachesPrivateRuntimeConfigurationEnvironmentCredentialsKeysTrustProofActualWireRawGraphsTraces':True,'readinessFixtureObjectsExcluded':True,'allInvokerPopulationNoControllerClientRootPriorRunJoins':True,'stage6And7OverlapsNotDoubleCounted':True,'noMissingStageOutcomeCancellationExactReleaseNetworkServerCpuOrOrdinaryBenefitClaim':True,'noNativeUniqueCountInflation':True,'noProtectedPatternMatchesIncludingDecompressedGzip':True,'matchingValuesPrinted':False})
 files=sorted(file for file in P.rglob('*')if file.is_file()and file.name!='SHA256SUMS');(P/'SHA256SUMS').write_text(''.join(sha(file)+'  '+str(file.relative_to(P))+'\n'for file in files))
 for line in(P/'SHA256SUMS').read_text().splitlines():digest,name=line.split('  ',1);assert sha(P/name)==digest
 assert sha(B/'staging-manifest.json')=='5486b6a5ef2d7aaa33c28314be19ba2b56b62d35f34d67cb6afbcf3cb70e4ba4'
 assert sha(OLD/'operational-pins.json')=='d9eae03d9621474d8a8dbcb33be1ff4d8f91ef73bd411f1bee4d697ce075c94d'and sha(O/'operational-pins.json')=='a239ecc7f4612750d6b39abd7871ef859aa4c30ca99ef33434b0301258c7a680'
 print(json.dumps({'packet':str(P),'entries':len(files),'bytes':sum(file.stat().st_size for file in files),'SHA256SUMSSha256':sha(P/'SHA256SUMS'),'featureAcceptance':False,'protectedPatternViolations':0,'originalPartialAndBothExecutedPinsUnchanged':True}))
