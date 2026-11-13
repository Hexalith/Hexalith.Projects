from pathlib import Path
import collections,datetime,gzip,hashlib,json,math,os,re,shutil,stat,statistics
P=Path('/tmp/story65-delegated-client-evidence-20261107');R=Path('/home/administrator/projects/hexalith/projects')
S=Path('/tmp/story65-delegated-client-prep-20261107');Q=Path('/tmp/story65-delegated-client-qualification-20261107');O=Path('/tmp/story65-delegated-client-ops-prep-20261107');L=Path('/tmp/story65-delegated-client-diagnostic-20261107');D=Path('/tmp/story65-capture-counter-private-20261107')
A={'Whole':'WholePublicInvoke','ComponentFile':'IssuerAcquisition','ComponentPolicy':'HttpRequestAndProof','AclFile':'SendAndBuffer','SettingsEnumerationSortDictionary':'JsonBody'}
Z={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'};F=Z|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
def shab(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def strict(b):
 def pair(items):
  d={}
  for k,v in items:assert k not in d;d[k]=v
  return d
 return json.loads(b,object_pairs_hook=pair)
def save(n,v):
 p=P/n;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n')
def put(f,n):
 f=Path(f);assert f.is_file()and not f.is_symlink();p=P/n;p.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(f,p);assert sha(f)==sha(p)
def rows_checked(raw,ordinal=None):
 groups=collections.defaultdict(dict);counts=collections.Counter();rows=[]
 for line in raw.splitlines():
  d=strict(line);assert set(d)==F and d['role']=='projects'and d['stage']in A
  assert all(type(d[k])is int for k in F-{'role','stage'})and all(d[k]==0 for k in Z)and d['frequency']>0 and d['captureOrdinal']>0 and d['elapsedTicks']>=0
  if ordinal is not None:assert d['runOrdinal']==ordinal
  assert d['stage']not in groups[d['captureOrdinal']];groups[d['captureOrdinal']][d['stage']]=d;counts[d['stage']]+=1;rows.append(d)
 return rows,counts,groups

def actual_copy_remove():
 P.mkdir(mode=0o700,exist_ok=False);fd=os.open(D,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);expected=(2096,8752986,1000,0o700);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==expected
  names={'projects.jsonl','projects.closed.json','window.control'};assert set(os.listdir(fd))==names;raws={};ids={}
  for name in sorted(names):
   f=os.open(name,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=fd)
   try:
    t=os.fstat(f);assert stat.S_ISREG(t.st_mode)and t.st_uid==1000 and stat.S_IMODE(t.st_mode)==0o600 and t.st_size<=({'projects.jsonl':8388608,'projects.closed.json':4096,'window.control':128}[name]);chunks=[]
    while True:
     data=os.read(f,65536)
     if not data:break
     chunks.append(data)
    raws[name]=b''.join(chunks);ids[name]=(t.st_dev,t.st_ino,t.st_size)
   finally:os.close(f)
  c=strict(raws['projects.closed.json']);fields={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
  assert set(c)==fields and c['role']=='projects'and all(type(v)is int for k,v in c.items()if k!='role')and c['diagnosticOnly']==1 and c['featureAcceptance']==0
  assert c['admitted']==c['disposedOrUnwound']==24 and c['writtenRows']==114 and c['writtenBytes']==len(raws['projects.jsonl'])and c['countersFinal']==1
  assert all(c[k]==0 for k in ['activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease'])
  assert(c['maximumRows'],c['maximumBytes'],c['queueCapacity'])==(65536,8388608,4096)and c['windowEndTicks']-c['windowStartTicks']==8*c['frequency']
  rows,counts,groups=rows_checked(raws['projects.jsonl'],c['runOrdinal']);assert len(rows)==114 and len(groups)==24 and counts=={'Whole':24,'ComponentFile':24,'ComponentPolicy':24,'AclFile':24,'SettingsEnumerationSortDictionary':18}
  patterns=collections.Counter(tuple(sorted(v))for v in groups.values());assert sorted(patterns.values())==[6,18]
  assert re.fullmatch(rb'[0-9]+\n[0-9]+\n[0-9]+\n[0-9]+\n',raws['window.control'])and list(map(int,raws['window.control'].splitlines()))==[c['runOrdinal'],c['frequency'],c['windowStartTicks'],c['windowEndTicks']]
  for n in ['projects.jsonl','projects.closed.json']:
   f=P/'actual-client'/n;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(raws[n]);assert sha(f)==shab(raws[n])
  save('actual-client/schema-and-copy-validation.json',{'diagnosticOnly':True,'featureAcceptance':False,'strictDuplicateFree14FieldRows':True,'strictClosedReceipt':True,'localCaptures':24,'rows':114,'stageCounts':dict(counts),'aliases':A,'zeroCountAndAllocationColumns':sorted(Z),'allCountAllocationZero':True,'allCountersFinal':True,'allDropsIoPendingAndActiveZero':True,'plainFileSha256':{n:shab(raws[n])for n in ['projects.jsonl','projects.closed.json']},'noInheritedRequestOrCorrelationFields':True})
  removed=[]
  for n in ['projects.jsonl','projects.closed.json','window.control']:
   t=os.stat(n,dir_fd=fd,follow_symlinks=False);assert(t.st_dev,t.st_ino,t.st_size)==ids[n];os.unlink(n,dir_fd=fd);removed.append({'name':n,'sha256':shab(raws[n]),'bytes':len(raws[n]),'copied':n!='window.control'})
  assert os.listdir(fd)==[];s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==expected
  save('private-counter-removal.json',{'afterParentIndependentCleanupAndRestorationRelease':True,'noSymlinkFollow':True,'directoryDevice':s.st_dev,'directoryInode':s.st_ino,'directoryUid':s.st_uid,'directoryMode':'0700','removedOnlyExactOwnedFiles':removed,'controlContentsNeverCopied':True,'strictSchemaAndHashCopiesBeforeRemoval':True,'sameDirectoryIdentityAfterRemoval':True,'directoryEmpty':True,'noSourceBuildHostOrSignalChanges':True})
 finally:os.close(fd)

if __name__=='__main__':
 actual_copy_remove();print(json.dumps({'packet':str(P),'actualCaptures':24,'actualRows':114,'privateDirectorySameAndEmpty':True}))

def stage():
 assert(P/'actual-client/projects.jsonl').is_file();compressed=[]
 def retain(file,relative):
  file=Path(file)
  if file.name=='window.control':return
  if file.suffix=='.jsonl':
   raw=file.read_bytes();rows,counts,groups=rows_checked(raw);out=P/(str(relative)+'.gz');out.parent.mkdir(parents=True,exist_ok=True)
   with out.open('wb')as stream:
    with gzip.GzipFile(filename='',fileobj=stream,mode='wb',mtime=0)as zipped:zipped.write(raw)
   with gzip.open(out,'rb')as zipped:assert shab(zipped.read())==shab(raw)
   compressed.append({'source':str(file),'retained':str(out.relative_to(P)),'rawSha256':shab(raw),'rawBytes':len(raw),'rows':len(rows),'stageCounts':dict(counts),'gzipSha256':sha(out),'gzipBytes':out.stat().st_size,'decompressionHashVerified':True})
  else:
   assert file.suffix in ['.py','.cs','.csproj','.Config','.json','.xml','.log','.md','.patch','.targets','.txt','.diff','.props','.proj'];put(file,relative)
 # Exact current prepared sources and complete failed scratch histories; never retain bin/obj/trace or numeric controls.
 for file in S.iterdir():
  if file.is_file()and file.suffix in ['.py','.cs','.csproj','.Config','.json','.xml','.md','.patch']:retain(file,Path('source-and-scratch/prep')/file.name)
 for folder in ['driver','results-qualified-final','results-qualified-initial','results-slow-diagnostic-1']:
  for file in(S/folder).rglob('*'):
   if file.is_file()and '__pycache__'not in file.parts:retain(file,Path('source-and-scratch')/folder/file.relative_to(S/folder))
 for file in Q.rglob('*'):
  if file.is_file()and not any(n in file.parts for n in ['bin','obj','__pycache__']):retain(file,Path('source-and-scratch/qualification')/file.relative_to(Q))
 save('source-and-scratch/synthetic-compression.json',{'bytePreservingGzip':True,'allRowsStrictClosed5StageZeroCountAllocation':True,'controlFilesNeverCopied':True,'files':compressed,'allRetainedSyntheticRows':sum(x['rows']for x in compressed),'qualifiedFinalSyntheticRows':sum(x['rows']for x in compressed if '/results-qualified-final/'in x['source'])})
 assert sum(x['rows']for x in compressed if '/results-qualified-final/'in x['source'])==166855
 pins=strict((O/'operational-pins.json').read_bytes());assert sha(O/'operational-pins.json')=='2b11c80446d5a495ad491610582cc66a209ab7af2dbb89cb6261c5df939cbc7d'
 for file in O.iterdir():
  if file.is_file()and file.suffix in ['.py','.json','.md','.log','.targets','.diff']:retain(file,Path('operations/prep')/file.name)
 for folder in ['operational-private-s2dl2i1m','native-private-24-hvktzhb7','operational-ordinary-w7n5n2dj','frozen-candidate-0270b99d906c','external-sources']:
  for file in(O/folder).rglob('*'):
   if file.is_file():retain(file,Path('operations')/folder/file.relative_to(O/folder))
 for file in L.iterdir():
  if file.is_file():retain(file,Path('managed')/file.name)
 for file in(L/'managed').iterdir():
  if file.is_file()and file.name in ['run.json','source-manifest.json','browser-seed.xml','browser-seed.log','launcher-controls.log','seed-progress.log','writer-protocol-cutover.json','bootstrap-command-status.json','bootstrap-delivery.json']or file.is_file()and re.fullmatch(r'build-[0-9]+\.log',file.name):retain(file,Path('managed/artifacts')/file.name)
 raw=(L/'managed/browser-advisory-seed-readiness.jsonl').read_bytes();ready=[strict(line)for line in raw.splitlines()];fields=['multiple','attempt','elapsedMilliseconds','httpStatus','result','incomplete','candidateCount','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied'];safe=[]
 for item in ready:
  row={k:item[k]for k in fields};assert type(row['attempt'])is int and type(row['candidateCount'])is int and type(row['httpStatus'])is int and type(row['elapsedMilliseconds'])in [float,int]and math.isfinite(row['elapsedMilliseconds'])
  assert all(type(row[k])is bool for k in ['multiple','incomplete','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied'])and row['result']in [None,'Empty','SingleCandidate','MultipleCandidates'];safe.append(row)
 (P/'managed/artifacts/advisory-readiness-scalars.jsonl').write_text(''.join(json.dumps(x,separators=(',',':'))+'\n'for x in safe))
 save('managed/scalar-readiness-provenance.json',{'originalSha256':shab(raw),'originalBytes':len(raw),'rows':len(safe),'retainedOnlyFixedFields':fields,'fixtureIndexDetailsOwnerStateAndGenerationObjectsNotCopied':True,'noTimingToReadinessRootJoin':True})
 run=strict((L/'managed/run.json').read_bytes());assert all(re.fullmatch(r'[0-9a-f]{40}',rev)for rev in run['revisions'].values())
 save('actual-managed-result.json',{'diagnosticOnly':True,'featureAcceptance':False,'standardManagedAllowedToFinishWithoutSignalsOrCutoff':True,'managedResult':run['result'],'originalFailure':run['failure'],'singleAttempts':sum(not x['multiple']for x in safe),'singleVerifiedPositiveAttempts':[x['attempt']for x in safe if not x['multiple']and x['positiveCandidatesVerified']],'multipleAttempts':sum(x['multiple']for x in safe),'multipleVerifiedPositiveAttempts':[x['attempt']for x in safe if x['multiple']and x['positiveCandidatesVerified']],'browserCases':0,'authenticatedBrowserPersistencePrivacyAccessibilityAcceptance':'not exercised; native seed failed','canonicalRevisions':run['revisions']})
 # Exact source-mapping provenance and reviewed external launcher/audit; no hidden CLI build.
 mapping=strict((O/'mapping-pins.json').read_bytes());put(mapping['mappingLog'],Path('mapping-provenance/mapping.log'))
 for i,(name,h)in enumerate(mapping['files'].items()):assert sha(name)==h;put(name,Path('mapping-provenance/files')/str(i)/Path(name).name)
 qual=strict((Q/'final-scratch-qualification.json').read_bytes());assert sha(Q/'final-scratch-qualification.json')=='bd180e87b5eff362d9eb199766a42a1e162888b8ba8f8efc1e3a14628243e21e';assert qual['passingCaseCells']==208 and qual['invocationComparisons']==224 and qual['passingFreshCanaries']==3 and qual['controlledProcesses']==27
 assert all(sha(R/n)==h for n,h in qual['sourceSha256'].items())and len(qual['sourceSha256'])==51
 assert all(sha(S/n)==h for n,h in qual['preparedFilesSha256'].items())
 save('source-preservation.json',{'source51Sha256':qual['sourceSha256'],'all51MatchedAfterOrdinaryRestorationAndPackaging':True,'actualQualifiedPreparedSourceFilesMatched':True,'noProductSourceGitBuildHostOrSignalsByPackaging':True})
 historical=[]
 for name in ['story-6-5-parsed-template-ordinary-20261107','story-6-5-parsed-template-cpu-20261107','story-6-5-capture-counter-20261107','story-6-5-advisory-wave-20261107']:
  path=R/'_bmad-output/implementation-artifacts/evidence'/name;historical.append({'packet':str(path.relative_to(R)),'SHA256SUMSSha256':sha(path/'SHA256SUMS')})
 save('qualification-and-historical-references.json',{'ordinaryUniqueNativePassesUnchanged':1038,'ownerNativePasses':412,'projectsNativePasses':626,'priorFullNativeAndCounterPacketsNotDuplicated':True,'durablePackets':historical,'scratchCaseCells':208,'scratchInvocationComparisons':224,'freshCanaries':3,'controlledScratchProcesses':27,'historicalNative24Retained':True,'native24IsNotClaimedSubsetOf1038':True,'native24NotAddedToOrdinaryUniqueCount':True,'scratchAndPrivateNativeAreDiagnosticQualificationOnly':True,'featureAcceptance':False,'canonicalRevisions':run['revisions']})
 # Local pairing only; no request, older-wave or downstream correlation.
 rows,counts,groups=rows_checked((P/'actual-client/projects.jsonl').read_bytes());stages={};paired=[]
 def stats(v):
  a=sorted(v);return {'n':len(a),'medianMs':statistics.median(a),'p95NearestRankMs':a[math.ceil(.95*len(a))-1],'minMs':min(a),'maxMs':max(a)}
 for stage in counts:stages[A[stage]]=stats([d['elapsedTicks']*1000/d['frequency']for d in rows if d['stage']==stage])
 for ordinal,g in sorted(groups.items()):
  phases={A[k]:v['elapsedTicks']*1000/v['frequency']for k,v in g.items()};paired.append({'opaqueLocalOrdinal':ordinal,'phasesMs':phases,'wholeMinusRecordedPhasesMs':phases['WholePublicInvoke']-sum(v for k,v in phases.items()if k!='WholePublicInvoke'),'jsonPhasePresent':len(g)==5})
 complete=[x for x in paired if x['jsonPhasePresent']];share=[x['phasesMs']['SendAndBuffer']/x['phasesMs']['WholePublicInvoke']for x in complete]
 save('actual-client/aggregation.json',{'diagnosticOnly':True,'featureAcceptance':False,'aliases':A,'stageWallMilliseconds':stages,'pairedLocalCaptures':paired,'all24WholeMinusRecordedSumMs':stats([x['wholeMinusRecordedPhasesMs']for x in paired]),'complete18WholeMs':stats([x['phasesMs']['WholePublicInvoke']for x in complete]),'complete18PairedSendShare':{'n':18,'median':statistics.median(share),'min':min(share),'max':max(share)},'completeFivePhaseCaptures':18,'fourPhaseWithoutJsonCaptures':6,'allCountsAllocationZero':True,'wholeStartsAfterOriginalGuardsAndWorkloadKeyReads':True,'sendIncludesDefaultResponseContentReadBufferingAndRemoteInterval':True,'noNetworkVersusServerAttribution':True,'missingJsonIsNotAuthorizationOutcomeOrCancellationEvidence':True,'noOrdinaryLatencyOrBenefitClaim':True,'noOverheadSubtraction':True,'noRequestRootEarlierWaveOrDownstreamJoins':True,'disposedOrUnwoundNotSuccessfulResult':True,'callerWaitCompletionNotChildQuiescence':True})
 print(json.dumps({'staged':str(P),'compressedSyntheticFiles':len(compressed),'allSyntheticRows':sum(x['rows']for x in compressed),'qualifiedRows':166855,'source51Match':True,'managedMultiAttempts':sum(x['multiple']for x in safe)}))

def finish():
 import datetime
 fd=os.open(D,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and os.listdir(fd)==[]
 finally:os.close(fd)
 save('independent-private-directory-recheck.json',{'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'directoryDevice':2096,'directoryInode':8752986,'directoryUid':1000,'directoryMode':'0700','samePinnedIdentity':True,'empty':True,'noSymlinkFollow':True})
 (P/'README.md').write_text('''This is a staging packet for a bounded private diagnostic of DelegatedOwnerQueryClient in EventStore.DomainService. Story6.5 acceptance remains failed. Ordinary unique native qualification stays412 owner+626 Projects=1,038. The real historical native24 retained here is a separately executed diagnostic qualification; it is neither claimed as a subset of1,038 nor added to that ordinary unique count. The existing parsed-template ordinary/CPU, capture-counter and advisory-wave packets are referenced by checksums, not replaced or relabelled.

Only DomainService compilation substituted DelegatedOwnerQueryClient.cs with private96b86cb2880e7d1cd5937c9c47dfa792a93029ee1f145b2c8d88b29f2fe426ae plus exact helper112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498 and bootstrap8f5836d1ff4dabb4c5fe819ae762dfff0748b5d792f984ae1adf45af28f45375. Root Projects.Server and ServiceDefaults stayed ordinary. The private code selects only the already-read validated projects literal; no additional configuration read or observed value enters the recorder. The no-inline bootstrap suppresses flow only around helper Begin and restores it before original application work. No inherited Activity/HttpContext/request identity, public logger, profiler, runtime recorder key or privacy-floor change was used. All original guards, fresh authority/configuration/token observations, ValueTask bounded/unbounded branch, default SendAsync buffering, JSON await, disposal order, cancellation and deadlines remain unchanged.

Actual scratch qualification binds finalprep b63548acaf07a804c8b610be4d7d1067b7b904b98acd1946c2e4bc464b04e406 and finalreceipt bd180e87b5eff362d9eb199766a42a1e162888b8ba8f8efc1e3a14628243e21e. All208 catalog cells and224 invocation comparisons passed across four modes, with two/four concurrent cohorts counted separately from case labels. Three serial fresh first-business canaries passed before the behavioral modes, including an unsafe Thread.Start positive detector. All27 owned processes/groups were absent and source51/app147/original native TestsDLL/107 oracle references stayed unchanged. The actual Debuggable attributes/profile and .NET10.0.14 natural14 CPU runtime facts are retained. Normal source values, keys, proof/header/payload and dependency traces were compared only in memory. All166,855 qualified synthetic rows have the strict primitive five-stage schema and zero allocation/count columns. Gzip files retain original/compressed hashes, row counts and verified decompression;20 additional retained historical row records may duplicate failed snapshot archives and are not additional qualification samples. No numeric window file is copied.

Initial build1 failed for a missing namespace; subsequent using fix, aggregate cooperative ChildAfterReturn failure, split diagnostic failure and their exact source snapshots/commands/receipts remain. Final qualification narrows instantaneous ChildAfterReturn equality to explicit uncooperative-issuer cells, while every source Completion/GetResult still joins exactly once and all other comparison assertions remain. The proposed scheduler-tail explanation is source-based inference, not established by a two-oracle control. That scope limit is preserved rather than turning the earlier failure into a pass. The transparent IDictionary.Add ASP0019 warning is also retained. The live source/helper/bootstrap were not changed by these scratch fixture corrections.

Root executed final operational pins2b11c80446d5a495ad491610582cc66a209ab7af2dbb89cb6261c5df939cbc7d after independent review. Private nine-project builds use the existing seven source mappings, two proof flags, Debug and BuildProjectReferences=false; build-only two-core resource control is removed at runtime. Exact Aspire start adds documented --no-build after prebuilds. Actual DomainService.Tests QueryAuthorityCorrectionTests retained its exact historical24-case set and passed without errors/failures/skips. The owned native process mandatorily mapped private DomainService test-copy/producer, then exited with its group absent and the pinned recorder directory empty before managed startup. This includes actual15 OwnerDeadlines cases and signed HTTP/owner-status tests, not merely mocked source comparisons.

The owned Projects runtime mapped private DomainService generation faa28372d4230200d565f102787096569a25124cc7666b168959c68af704c4e3 and coherent PDB. All six source-profile checks and strict ordinary SDK8 stayed independent of that private assembly. Source51 and all17,211 full live manifest inputs matched. The eight-second recording window followed a genuine failed MULTIPLE, with no positive seen and sufficient attempts remaining. There was no cutoff or application signal. Captures use generated opaque run/local invocation ordinals only, with no request/root/older-wave/downstream joins.

The actual closed receipt has24 admitted/disposed-or-unwound captures,114 rows, final counters and zero active/pending/queue/closed/cap drops or I/O failures. All count/allocation columns are0. Serialized helper labels are aliases: Whole=WholePublicInvoke; ComponentFile=IssuerAcquisition; ComponentPolicy=HttpRequestAndProof; AclFile=SendAndBuffer; SettingsEnumerationSortDictionary=JsonBody. Eighteen captures contain allfive phases; six lack JSON. No authorization/result/status/cancellation inference is made from missing JSON or a disposed scope.

All24 median wall durations in milliseconds are Whole599.521772, issuer4.136507, request/proof0.668870 and send/buffer594.265773. JSON's18 observations have median0.058104. Whole spans123.884333–656.740837ms; send/buffer119.150906–651.765891ms. The18 complete whole scopes have median609.813001ms and range559.499058–656.740837ms. Within those same local captures, the median send/whole fraction is99.183509%; the paired complete-capture whole-minus-sum median is0.059300ms. All24 residuals have median0.056895/min0.023595/max0.969083ms. Exact per-ordinal and stage min/median/nearest-rankP95/max values are in actual-client/aggregation.json.

Whole begins after the original front guards and workload/key reads. The send scope includes HttpClient creation, default ResponseContentRead response buffering, transit, remote processing and internal failure cleanup; it does not separate network, Gateway, authority, receiver or actor time. JSON stops before mapping; whole includes original response/request and public CTS disposal/final timeout checks. Whole-minus-recorded-phases is unattributed preparation/mapping/check/cleanup and instrumentation, not exact disposal. These are instrumented wall samples with possible background-recording perturbation, not ordinary latency/benefit or exact CPU evidence. No overhead was subtracted. Caller wait completion does not prove downstream quiescence. No production repair or policy/deadline change follows from this packet.

The full managed lane completed normally with SINGLE positive at attempt3, MULTIPLE30/30 failed and no browser cases; native seed failed. Authenticated OIDC browser persistence/privacy/accessibility acceptance remains unexercised. Readiness is retained only as fixed scalar fields, excluding protected fixture index/detail/owner-state/generation objects. Its original file hash/size are preserved; it is not joined to timing ordinals. Full source revisions are canonical and dirty/current source hashes are explicit.

Cleanup retains all four initial/delayed observations and root's independent final correction/receipt; the generic audit's derived ordinary label is corrected to diagnostic-only without discarding earlier observations. All21 recorded process identities/five containers, early12 identities, native24 group and integration-native descendants were absent. Namespace/private material and preservation flags passed;17,211 full source inputs had0 mismatches. Ordinary restoration rebuilt nine main projects plus the native test carrier: all10 builds exited0. All98 DomainService DLL/PDB/ref/refint copies are private-marker clean;22 affected outputs are coherent with ordinary producer1c8dfbeec54a63ad59921f22aaf2826f89f2b2dcb2f2db54d4c4a403f1fd0178 and76 unrelated generations are byte-unchanged. SDK8 and the additional native SDK copy are ordinary5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872. Scoped Server10 proof distinguishes current rebuilt generation from untouched Server.Tests copies; all naturally match prior39bb/PDB8369 here without requiring byte equivalence from source identity alone. All147 native assembly copies are free of private markers. Current source51 hashes still match.

After root's independent cleanup/restoration release, actual projects.jsonl and projects.closed.json were strict-schema validated and hash-copied plain, then only those two files and numeric window.control were removed through the exact pinned no-follow directory descriptor. Control hash/size are retained, never its contents. UID1000/device2096/inode8752986/mode0700 and emptiness were independently rechecked. Build/source-mapping configuration files are provenance, not private runtime configuration. No application binaries, private configs/env, credentials/keys/TrustProof/proofs, observed runtime objects, wire payloads, raw resource graphs or runtime traces are included. The packet awaits parent audit before any repository copy/spec edit; historical packets and repository sources were not changed by packaging.
''')
 put('/tmp/story65-delegated-client-package.py',Path('packaging-source.py'))
 bad=[];badnames=[];jwt=rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}'
 for file in P.rglob('*'):
  if not file.is_file():continue
  assert not file.is_symlink()
  if file.suffix.lower()in ['.dll','.pdb','.pfx','.pem','.key','.der','.nettrace','.etlx']or file.name in ['window.control','fixture.json','endpoints.json','apphost-session.json']:badnames.append(str(file.relative_to(P)))
  data=gzip.decompress(file.read_bytes())if file.suffix=='.gz'else file.read_bytes()
  if re.search(jwt,data)or(bytes([45])*5+b'BEGIN PRIVATE KEY'+bytes([45])*5)in data or(bytes([45])*5+b'BEGIN RSA PRIVATE KEY'+bytes([45])*5)in data:bad.append(str(file.relative_to(P)))
 assert not bad and not badnames,(bad,badnames)
 save('privacy-and-exclusions.json',{'actualRowsAndReceiptStrictClosedSchemaBeforeCopy':True,'allActualCountAllocationColumnsZero':True,'allRetainedSyntheticRowsSchemaValidatedAndGzipHashVerified':True,'noInheritedRequestRootTraceSubjectRouteFieldsInCounterRows':True,'scalarReadinessOnlyFixtureObjectsExcluded':True,'noControlContentsPrivateRuntimeConfigurationEnvironmentCredentialsKeysTrustProofWireOrRawGraphsCopied':True,'noApplicationBinaryRuntimeTraceOrDerivatives':True,'noJwtOrPrivateKeyPatternsIncludingGzipRows':True,'backgroundExecutionContextBoundaryQualifiedWithFreshCanaries':True,'fixtureRaceQualificationScopeAndInferenceLimitExplicit':True,'native24NoSubsetOrOrdinaryUniqueCountInflation':True,'noNetworkServerAttributionOrOrdinaryLatencyBenefitClaim':True,'privateDirectorySameIdentityEmpty':True,'matchedProtectedValuesPrinted':False,'featureAcceptance':False})
 files=sorted(x for x in P.rglob('*')if x.is_file()and x.name!='SHA256SUMS');(P/'SHA256SUMS').write_text(''.join(sha(x)+'  '+str(x.relative_to(P))+'\n'for x in files))
 for line in(P/'SHA256SUMS').read_text().splitlines():h,n=line.split('  ',1);assert sha(P/n)==h
 print(json.dumps({'packet':str(P),'entries':len(files),'bytes':sum(x.stat().st_size for x in files),'SHA256SUMSSha256':sha(P/'SHA256SUMS'),'privacyPatternViolations':0,'directorySameAndEmpty':True,'featureAcceptance':False}))
