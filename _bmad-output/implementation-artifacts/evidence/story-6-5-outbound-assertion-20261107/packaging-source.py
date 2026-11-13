from pathlib import Path
import collections,datetime,gzip,hashlib,importlib.util,json,math,os,re,shutil,stat,statistics
R=Path('/home/administrator/projects/hexalith/projects');B=Path('/tmp/story65-outbound-assertion-evidence-20261107');P=Path('/tmp/story65-outbound-assertion-evidence-complete-20261107');BASE=Path('/tmp/story65-outbound-assertion-ops-prep-20261107');O=BASE/'live';E=Path('/tmp/story65-outbound-assertion-diagnostic-20261107');Q=Path('/tmp/story65-outbound-assertion-qualification-20261107');D=Path('/tmp/story65-capture-counter-private-20261107')
A={'Whole':'WholeHandler','ComponentFile':'IssuerValueTask','ComponentPolicy':'BaseSendAsync'}
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
 assert sha(B/'staging-manifest.json')=='e58c7032f89d1d4717d38de19316e09a1f2e33624b1de5169e800a34a8d7ccf2';partial=strict((B/'staging-manifest.json').read_bytes());assert len(partial['files'])==236;assert not P.is_symlink();P.mkdir(mode=0o700,exist_ok=True)
 for name,info in partial['files'].items():assert sha(B/name)==info['sha256']and(B/name).stat().st_size==info['bytes'];put(B/name,Path('qualified-preparation')/name)
 put(B/'staging-manifest.json',Path('qualified-preparation/staging-manifest.json'));save('partial-generation-preservation.json',{'originalPartialPath':str(B),'originalStagingManifestSha256':sha(B/'staging-manifest.json'),'copiedVerifiedEntries':236,'copiedManifestUnchanged':True,'originalPartialNotModified':True,'pendingLabelsInPartialAreHistorical':True})
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
 raw=(E/'counter-archive/gateway.jsonl').read_bytes();compressed=gzip.compress(raw,mtime=0);dest=P/'actual-handler/gateway.jsonl.gz';dest.parent.mkdir(mode=0o700,parents=True,exist_ok=True);dest.write_bytes(compressed);assert gzip.decompress(compressed)==raw
 put(E/'counter-archive/gateway.closed.json',Path('actual-handler/gateway.closed.json'));save('actual-handler/raw-compression-receipt.json',{'sourceArchivedRawSha256':shab(raw),'rawBytes':len(raw),'gzipSha256':shab(compressed),'gzipBytes':len(compressed),'decompressionByteIdentityVerified':True,'rawDuplicateNotRetained':True})
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
 for file in Path('/tmp').glob('story65-outbound-assertion-parent-*.py'):put(file,Path('parent-scripts')/file.name)
 save('archive-validation-correction-history.json',{'parentReportedInitialFailureType':'KeyError','parentReportedMissingPreparationReceiptKey':'files','correctPreparationReceiptKey':'filesSha256','initialGuardFailureBeforeArchiveDirectoryOrPrivateFileAccess':True,'bothInitialAndCorrectedParentScriptsRetained':True,'noCounterCopyOrRemovalByFailedAttempt':True,'laterCorrectedAttemptFullyGuardedValidatedAndRemovedExact3':True,'noApplicationOrNativeRetestForArchiveFix':True})
 print(json.dumps({'packetStaged':str(P),'partialPreservedArtifacts':236,'actualCaptures':1502,'actualRows':3025}))
def aggregate():
 data=gzip.decompress((P/'actual-handler/gateway.jsonl.gz').read_bytes());closed=strict((P/'actual-handler/gateway.closed.json').read_bytes());assert all(type(v)is int for k,v in closed.items()if k!='role')and closed['role']=='gateway';assert closed['admitted']==closed['disposedOrUnwound']==1502 and closed['writtenRows']==3025 and closed['writtenBytes']==len(data)==976695
 assert closed['countersFinal']==1 and all(closed[k]==0 for k in ['activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease']);assert closed['windowEndTicks']-closed['windowStartTicks']==8*closed['frequency']
 groups=collections.defaultdict(dict);values=collections.defaultdict(list);counts=collections.Counter()
 for line in data.splitlines():
  row=strict(line);assert set(row)==F and row['role']=='gateway'and row['stage']in A and all(type(row[k])is int for k in F-{'role','stage'})and all(row[k]==0 for k in Z)and row['runOrdinal']==closed['runOrdinal']and row['frequency']==closed['frequency']and row['elapsedTicks']>=0 and row['captureOrdinal']>0
  stage=A[row['stage']];assert stage not in groups[row['captureOrdinal']];ms=row['elapsedTicks']*1000/row['frequency'];groups[row['captureOrdinal']][stage]=ms;values[stage].append(ms);counts[row['stage']]+=1
 assert len(groups)==1502 and counts=={'Whole':1502,'ComponentFile':21,'ComponentPolicy':1502}
 def stats(a):
  v=sorted(a);return{'count':len(v),'medianMs':statistics.median(v),'minMs':min(v),'p95NearestRankMs':v[math.ceil(.95*len(v))-1],'maxMs':max(v)}
 conditioned=[]
 for present,expected in[(False,1481),(True,21)]:
  selected=[group for group in groups.values()if('IssuerValueTask'in group)==present];assert len(selected)==expected;conditioned.append({'issuerExpressionStagePresent':present,'captures':len(selected),'millisecondsByStage':{stage:stats([group[stage]for group in selected])for stage in A.values()if stage in selected[0]},'noRequestKindResultOrTokenPresenceInference':True})
 parent=strict((E/'parent-counter-summary.json').read_bytes());assert parent['captures']==1502 and parent['rows']==3025 and parent['originalRawSha256']==shab(data)
 for stage,summary in parent['millisecondsByStage'].items():assert abs(summary['median']-statistics.median(values[stage]))<1e-9
 other=strict((E/'parent-counter-conditioned-summary.json').read_bytes());assert other['rawSha256']==shab(data)
 for mine,theirs in zip(conditioned,other['groupsByNumericStagePresenceOnly'],strict=True):
  assert mine['issuerExpressionStagePresent']==theirs['issuerExpressionStagePresent']and mine['captures']==theirs['captures']
  for serialized,summary in theirs['millisecondsByStage'].items():assert abs(mine['millisecondsByStage'][A[serialized]]['medianMs']-summary['median'])<1e-9
 cleanup=strict((E/'parent-counter-archive-cleanup.json').read_bytes());assert cleanup['artifacts']['gateway.jsonl']['sha256']==shab(data)and cleanup['artifacts']['gateway.closed.json']['sha256']==sha(P/'actual-handler/gateway.closed.json')and cleanup['controlContentsNotArchived']is True
 save('actual-handler/schema-and-aggregation.json',{'diagnosticOnly':True,'featureAcceptance':False,'strictDuplicateFree14FieldRows':True,'allZeroCountAllocationColumns':sorted(Z),'captures':1502,'rows':3025,'finalNoDropsIoCapsOrActiveWork':True,'aliases':A,'globalMillisecondsByStage':{stage:stats(a)for stage,a in values.items()},'conditionedOnlyOnNumericStagePresence':conditioned,'pairedLocalCaptures':[{'opaqueLocalOrdinal':ordinal,'phasesMilliseconds':group}for ordinal,group in sorted(groups.items())],'parentAllAndConditionedStageMediansAgree':True,'allFactoryRequestsExceptIssuerBroadPopulationIncludesNonCanonical':True,'fixedRoleLimitedToSelectedSixRuntimeComposition':True,'wholeAndBaseExcludeOuterHttpClientBuffering':True,'noPriorInvokerControllerClientRootRequestOrOutcomeJoin':True,'noMissingStageOutcomeCancellationOrTokenInference':True,'noNetworkReceiverCpuOrOrdinaryBenefitClaim':True,'noOverheadSubtraction':True})
def verify_outputs():
 spec=importlib.util.spec_from_file_location('live_output_audit',O/'ops_common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 source=m.sources();sdk=m.ordinary_sdk_proof();domain=m.ordinary_domain_proof();server=m.ordinary_projects_server_proof();gateway=m.ordinary_gateway_proof();native147=m.native147_marker_absence();native_sdk=m.native_sdk_proof();actual=m.census()
 private=Path(strict((O/'latest-private-attempt.json').read_bytes())['attemptDirectory']);native=Path(strict((O/'latest-native-attempt.json').read_bytes())['attemptDirectory']);ordinary=list(O.glob('operational-ordinary-*'));assert len(ordinary)==1;ordinary=ordinary[0]
 affected=set(strict((ordinary/'affected-paths-union.json').read_bytes()));initial={row['path']:row for row in strict((private/'before-census.json').read_bytes())};kinds={kind:sha(m.ordinary_expected(kind))for kind in ['full','pdb','ref','refint']};assert len(actual)==66 and len(affected)==22 and all(not row['privateMarker']for row in actual)
 for row in actual:assert row['sha256']==(kinds[row['kind']]if row['path']in affected else initial[row['path']]['sha256'])
 commands=strict((ordinary/'commands.json').read_bytes());assert len(commands)==12 and all(row['exitCode']==0 for row in commands)
 native_rows=m.native_artifacts();assert len(native_rows)==6 and all(not row['privateMarker']for row in native_rows)
 native_project=R/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests'
 for row in native_rows:
  file=R/row['path'];kind='pdb'if file.suffix=='.pdb'else'refint'if'refint'in file.parts else'ref'if'ref'in file.parts else'full'
  expected=native_project/('obj/Debug/net10.0/'+kind+'/Hexalith.EventStore.Server.Tests.dll'if kind in['ref','refint']else'bin/Debug/net10.0/Hexalith.EventStore.Server.Tests'+('.pdb'if kind=='pdb'else'.dll'))
  assert sha(file)==sha(expected)
 gate=strict((native/'receipt.json').read_bytes());assert gate['passed']and gate['caseCount']==29 and len(gate['processes'])==2 and all(row['passed']and row['mappedPrivateServer']and row['processAndDescendantsAbsent']for row in gate['processes']);assert[case['caseCount']for case in gate['processes']]==[28,1]
 def absent(pid,ticks):
  try:state=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return state[0]=='Z'or state[19]!=str(ticks)
  except FileNotFoundError:return True
 generic=strict((E/'independent-cleanup-source-audit.json').read_bytes());assert len(generic['identities'])==21 and all(absent(row['pid'],row['startTicks'])for row in generic['identities'])
 assert all(absent(row['pid'],row['startTicks'])for row in gate['processes'])
 q=strict((Q/'final-scratch-qualification.json').read_bytes());assert len(q['sourceSha256'])==54 and all(sha(R/path)==digest for path,digest in q['sourceSha256'].items())
 manifest=strict((E/'managed/source-manifest.json').read_bytes());assert len(manifest)==17326 and all(sha(R/path)==digest for path,digest in manifest.items())
 save('source-preservation.json',{'all54ProductionAndControlSourcesMatchAfterOrdinaryRestoration':True,'source54Sha256':q['sourceSha256'],'fullLiveManifestCount':len(manifest),'fullLiveManifestHashMismatches':0,'privateSourceBf9Helper112Bootstrap05bbbUnchanged':True,'noProductSourceGitBuildHostSignalChangesByPackaging':True})
 save('independent-restoration-source-audit.json',{'diagnosticOnly':True,'featureAcceptance':False,'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source54':source,'server66MarkerClean':True,'affected22FullPdbRefRefintCoherent':True,'unrelated44ExactBaseline':True,'ordinaryNewServerGenerationSha256':kinds['full'],'ordinaryKindsSha256':kinds,'notClaimedByteEquivalentToPriorServerA594Generation':True,'gateway34Scoped':gateway,'sdk8':sdk,'additionalNativeSdk':native_sdk,'domain98':domain,'server10Scoped':server,'native147':native147,'nativeTestArtifactsCount':len(native_rows),'allSixNativeTestArtifactsCoherentAndMarkerClean':True,'all12OrdinaryBuildsExitedZero':True,'all21RecordedRuntimeKernelIdentitiesAbsentNow':True,'all2NativeKernelIdentitiesAbsentNow':True,'parentContainerGroupNamespacePrivatePreservationProofRetained':'managed/parent-counter-archive-cleanup.json','noBuildTestsAssemblyLoadHostSignalsOrProductSourceWrites':True})
 save('qualification-count-boundaries.json',{'ordinaryUniqueNativePassesUnchanged':1038,'owner412PlusProjects626':True,'scratchBehavioralCells':100,'scratchFreshCanaries':3,'scratchProcesses':15,'scratchStrictRows':83688,'actualNative29DiagnosticPassed':True,'actualProcessCaseCounts':[28,1],'strictRenderedIdentityMultiset27WithOneMultiplicity3':True,'noUniquePerSourceInputXmlClaimForTriple':True,'historical28OnlyCorroborationNotHistorical29':True,'native29NotClaimedSubsetOrExtraUniqueOrdinary1038':True,'featureAcceptance':False})
 fd=os.open(D,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and os.listdir(fd)==[]
 finally:os.close(fd)
 save('independent-counter-directory-recheck.json',{'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'device':2096,'inode':8752986,'uid':1000,'mode':'0700','samePinnedIdentity':True,'empty':True,'noCounterOrControlFilesTouchedByPackaging':True})
if __name__=='__main__':stage()


def finish():
 put('/tmp/story65-outbound-assertion-complete-package.py',Path('packaging-source.py'))
 forbidden=[];matches=[];jwt=rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}'
 for f in P.rglob('*'):
  if not f.is_file():continue
  assert not f.is_symlink()
  if f.suffix.lower()in['.dll','.pdb','.pfx','.pem','.key','.der','.nettrace','.etlx','.cache']or f.name in['window.control','fixture.json','endpoints.json','apphost-session.json']:forbidden.append(str(f.relative_to(P)))
  data=gzip.decompress(f.read_bytes())if f.suffix=='.gz'else f.read_bytes()
  if re.search(jwt,data)or(bytes([45])*5+b'BEGIN PRIVATE KEY'+bytes([45])*5)in data or(bytes([45])*5+b'BEGIN RSA PRIVATE KEY'+bytes([45])*5)in data:matches.append(str(f.relative_to(P)))
 assert not forbidden and not matches
 save('privacy-and-exclusions.json',{'featureAcceptance':False,'strictThreeStage14FieldActualRows':3025,'actualCaptures':1502,'zeroCountAllocationColumnsAndFinalNoDropsIoCaps':True,'originalPartial236PlusManifestPreserved':True,'syntheticGzipFiles':11,'actualGzipFiles':1,'totalRawRowsPreserved':86713,'exactRawDecompressionHashVerified':True,'native29StrictMultisetNotOrdinaryCountInflation':True,'staticTripleNoPerSourceInputXmlClaim':True,'archiveInitialReceiptKeyFailureAndOneKeyCorrectionPreserved':True,'controlContentsNotCopied':True,'fixedPinnedDirectoryEmpty':True,'stagePresenceGroupsNoKindResultTokenInference':True,'wholeAndBaseExcludeOuterBufferNoPriorProbeJoins':True,'noNetworkReceiverCpuOrOrdinaryBenefitClaim':True,'noProtectedPatternsIncludingDecompressedGzip':True,'noBinariesCachesPrivateRuntimeConfigEnvCredentialsKeysTrustProofWireRawGraphTrace':True,'readinessFixtureObjectsExcluded':True,'matchingValuesPrinted':False})
 files=sorted(f for f in P.rglob('*')if f.is_file()and f.name!='SHA256SUMS');(P/'SHA256SUMS').write_text(''.join(sha(f)+'  '+str(f.relative_to(P))+'\n'for f in files))
 for line in(P/'SHA256SUMS').read_text().splitlines():h,n=line.split('  ',1);assert sha(P/n)==h
 assert sha(B/'staging-manifest.json')=='e58c7032f89d1d4717d38de19316e09a1f2e33624b1de5169e800a34a8d7ccf2'and sha(O/'operational-pins.json')=='fddf36531bd99c5438457073e693ceaa9a133cec961717e5782555a175d2fd8a'
 print(json.dumps({'packet':str(P),'entries':len(files),'bytes':sum(f.stat().st_size for f in files),'SHA256SUMSSha256':sha(P/'SHA256SUMS'),'allOriginalPinsAndPartialUnchanged':True,'protectedPatternViolations':0,'featureAcceptance':False}))
