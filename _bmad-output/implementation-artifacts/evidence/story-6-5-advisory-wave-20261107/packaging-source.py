from pathlib import Path
import collections,gzip,hashlib,json,math,os,re,shutil,stat,time
P=Path('/tmp/story65-advisory-wave-evidence-20261107')
ROOT=Path('/home/administrator/projects/hexalith/projects')
SRC=Path('/tmp/story65-advisory-wave-prep-20261107')
OPS=Path('/tmp/story65-advisory-wave-ops-prep-20261107')
LIVE=Path('/tmp/story65-advisory-wave-diagnostic-20261107')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
def sha_bytes(d):return hashlib.sha256(d).hexdigest()
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def save(n,v):
 p=P/n;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n')
def put(p,n):
 p=Path(p);assert p.is_file()and not p.is_symlink();q=P/n;q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,q);assert sha(p)==sha(q)
def strict(data):
 def pairs(items):
  out={}
  for k,v in items:
   assert k not in out;out[k]=v
  return out
 return json.loads(data,object_pairs_hook=pairs)
ALIASES={'Whole':'Whole','ComponentFile':'InitialScope','ComponentPolicy':'InitialFilterInputs','AclFile':'IndexRead','SettingsEnumerationSortDictionary':'DiscoveryParallel','AuthenticationSerialization':'FinalFilterInputs','ActorBoundary':'FinalCandidateParallel','ResultTail':'FinalScope'}
ZERO=['allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded']
FIELDS={'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency',*ZERO}
def validate_rows(raw,run=None):
 counts=collections.Counter();groups=collections.defaultdict(dict);rows=[]
 for line in raw.splitlines():
  d=strict(line);assert set(d)==FIELDS and d['role']=='projects' and d['stage']in ALIASES
  assert all(type(v)is int for k,v in d.items()if k not in ('role','stage'))
  assert all(d[k]==0 for k in ZERO)and d['elapsedTicks']>=0 and d['frequency']>0 and d['captureOrdinal']>0
  if run is not None:assert d['runOrdinal']==run
  assert d['stage']not in groups[d['captureOrdinal']];groups[d['captureOrdinal']][d['stage']]=d
  counts[d['stage']]+=1;rows.append(d)
 return rows,counts,groups

def copy_and_remove_live():
 P.mkdir(mode=0o700,exist_ok=False)
 fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);expected=(2096,8752986,1000,0o700);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==expected
  names={'projects.jsonl','projects.closed.json','window.control'};assert set(os.listdir(fd))==names
  data={};identities={}
  for n in sorted(names):
   f=os.open(n,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=fd)
   try:
    t=os.fstat(f);assert stat.S_ISREG(t.st_mode)and t.st_uid==1000 and stat.S_IMODE(t.st_mode)==0o600
    assert t.st_size<=({'projects.jsonl':8*1024*1024,'projects.closed.json':4096,'window.control':128}[n])
    chunks=[]
    while True:
     b=os.read(f,65536)
     if not b:break
     chunks.append(b)
    data[n]=b''.join(chunks);identities[n]=(t.st_dev,t.st_ino,t.st_size)
   finally:os.close(f)
  c=strict(data['projects.closed.json']);assert type(c)is dict and c['role']=='projects'and all(type(v)is int for k,v in c.items()if k!='role')
  required={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
  assert set(c)==required and c['diagnosticOnly']==1 and c['featureAcceptance']==0
  assert c['admitted']==c['disposedOrUnwound']==3 and c['writtenRows']==21 and c['writtenBytes']==len(data['projects.jsonl'])
  assert c['countersFinal']==1 and all(c[k]==0 for k in ['activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease'])
  assert(c['maximumRows'],c['maximumBytes'],c['queueCapacity'])==(65536,8388608,4096)
  assert c['windowEndTicks']-c['windowStartTicks']==8*c['frequency']
  r,counts,groups=validate_rows(data['projects.jsonl'],c['runOrdinal']);assert len(r)==21 and len(groups)==3
  assert set(counts)==set(ALIASES)-{'ResultTail'}and set(counts.values())=={3}
  assert re.fullmatch(rb'[0-9]+\n[0-9]+\n[0-9]+\n[0-9]+\n',data['window.control'])
  control=list(map(int,data['window.control'].splitlines()));assert control==[c['runOrdinal'],c['frequency'],c['windowStartTicks'],c['windowEndTicks']]
  for n in ['projects.jsonl','projects.closed.json']:
   q=P/'actual-wave'/n;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data[n]);assert sha(q)==sha_bytes(data[n])
  save('actual-wave/schema-and-copy-validation.json',{'diagnosticOnly':True,'featureAcceptance':False,'strictDuplicateFreeClosedSchema':True,'rows':len(r),'localCaptures':len(groups),'serializedStageCounts':dict(counts),'aliases':ALIASES,'zeroCountAndAllocationColumns':ZERO,'allCountsAndAllocationZero':True,'receiptFinal':True,'allDropIoAndPendingZero':True,'plainCopiedSha256':{n:sha_bytes(data[n])for n in ['projects.jsonl','projects.closed.json']},'noInheritedRequestContextFields':True})
  removed=[]
  for n in ['projects.jsonl','projects.closed.json','window.control']:
   t=os.stat(n,dir_fd=fd,follow_symlinks=False);assert(t.st_dev,t.st_ino,t.st_size)==identities[n]
   os.unlink(n,dir_fd=fd);removed.append({'name':n,'sha256':sha_bytes(data[n]),'size':len(data[n]),'copied':n!='window.control'})
  assert os.listdir(fd)==[]
  t=os.fstat(fd);assert(t.st_dev,t.st_ino,t.st_uid,stat.S_IMODE(t.st_mode))==expected
  save('private-counter-removal.json',{'diagnosticOnly':True,'featureAcceptance':False,'afterParentIndependentRestorationRelease':True,'directoryDevice':t.st_dev,'directoryInode':t.st_ino,'directoryUid':t.st_uid,'directoryMode':'0700','noSymlinkFollow':True,'removedOnlyExactOwnedFiles':removed,'controlContentsNotRetained':True,'counterCopiesVerifiedBeforeRemoval':True,'sameDirectoryIdentityAfterRemoval':True,'directoryEmpty':True,'noSignalsOrBuildOrProductSourceChanges':True})
  return r,c,groups
 finally:os.close(fd)

if __name__=='__main__':
 rows,closed,groups=copy_and_remove_live()
 print(json.dumps({'packet':str(P),'liveRows':len(rows),'localCaptures':len(groups),'closedFinal':closed['countersFinal'],'privateDirectoryEmpty':True}))

def stage_remaining():
 assert(P/'actual-wave/projects.jsonl').is_file()
 compressed=[]
 # Reviewed source and differential driver; no compiled binaries or provider traces.
 for p in SRC.iterdir():
  if p.is_file()and p.suffix in ['.cs','.py','.json','.md','.patch']:put(p,Path('source-and-qualification/prep')/p.name)
 for directory in ['differential','qualification','history']:
  for p in(SRC/directory).rglob('*'):
   if not p.is_file()or any(x in p.parts for x in ['bin','obj','__pycache__']):continue
   assert p.suffix in ['.cs','.csproj','.json','.py','.md','.log','.Config','.patch']
   put(p,Path('source-and-qualification')/directory/p.relative_to(SRC/directory))
 for directory in ['differential-results','differential-results-qualified']:
  for p in(SRC/directory).rglob('*'):
   if not p.is_file()or p.name=='window.control':continue
   rel=Path('source-and-qualification')/directory/p.relative_to(SRC/directory)
   if p.name=='projects.jsonl':
    raw=p.read_bytes();rows,counts,groups=validate_rows(raw)
    q=P/(str(rel)+'.gz');q.parent.mkdir(parents=True,exist_ok=True)
    with q.open('wb')as f:
     with gzip.GzipFile(filename='',fileobj=f,mode='wb',mtime=0)as z:z.write(raw)
    with gzip.open(q,'rb')as z:assert sha_bytes(z.read())==sha_bytes(raw)
    compressed.append({'originalRelativePath':str(Path(directory)/p.relative_to(SRC/directory)),'retainedPath':str(q.relative_to(P)),'rawSha256':sha_bytes(raw),'rawBytes':len(raw),'rows':len(rows),'serializedStageCounts':dict(counts),'gzipSha256':sha(q),'gzipBytes':q.stat().st_size,'decompressedBytesHashVerified':True})
   else:
    assert p.suffix=='.json';put(p,rel)
 save('source-and-qualification/synthetic-raw-compression.json',{'bytePreservingGzip':True,'allRowsStrictClosedSchemaZeroCountAllocation':True,'files':compressed,'allSyntheticRows':sum(x['rows']for x in compressed),'qualifiedRows':sum(x['rows']for x in compressed if x['originalRelativePath'].startswith('differential-results-qualified/')),'syntheticControlFilesNotCopied':True})
 pins=strict((OPS/'operational-pins.json').read_bytes());assert sha(OPS/'operational-pins.json')=='b0395281c7a4ca7a98657e4f92e9dae60c87fa372898ca2324b8e95bedeb4419'
 for p in OPS.iterdir():
  if p.is_file()and p.suffix in ['.py','.targets','.json','.txt']:put(p,Path('operations/prep')/p.name)
 for directory in ['operational-private-s6zoyj_7','native-private-139-75txjnq7','operational-ordinary-d8qoqnu5','frozen-candidate-835069056b5e']:
  for p in(OPS/directory).rglob('*'):
   if p.is_file():
    assert p.suffix in ['.json','.log','.xml','.py','.targets','.txt','.cs','.patch']
    put(p,Path('operations')/directory/p.relative_to(OPS/directory))
 for p in LIVE.iterdir():
  if p.is_file():
   assert p.suffix in ['.json','.log'];put(p,Path('managed')/p.name)
 # Keep original closed run/manifest, command and test evidence. Detailed fixture object rows stay excluded.
 for n in ['run.json','source-manifest.json','browser-seed.xml','browser-seed.log','build-38.log','launcher-controls.log','seed-progress.log','writer-protocol-cutover.json','bootstrap-command-status.json','bootstrap-delivery.json']:
  put(LIVE/'managed'/n,Path('managed/artifacts')/n)
 raw=(LIVE/'managed/browser-advisory-seed-readiness.jsonl').read_bytes();readiness=[strict(x)for x in raw.splitlines()]
 allowed=['multiple','attempt','elapsedMilliseconds','httpStatus','result','incomplete','candidateCount','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied']
 sanitized=[]
 for row in readiness:
  out={k:row[k]for k in allowed};assert type(out['multiple'])is bool and type(out['attempt'])is int
  assert all(type(out[k])is bool for k in ['incomplete','positiveCandidatesVerified','actualPublicGateway','nonBootstrapCredential','fabricatedProjectionWrite','runtimeAuthoritySupplied'])
  assert out['result']in [None,'SingleCandidate','MultipleCandidates','Empty','NoCandidates']
  sanitized.append(out)
 p=P/'managed/artifacts/advisory-readiness-closed-summary.jsonl';p.write_text(''.join(json.dumps(x,separators=(',',':'))+'\n'for x in sanitized))
 save('managed/readiness-sanitization.json',{'originalSha256':sha_bytes(raw),'originalBytes':len(raw),'rows':len(sanitized),'retainedFields':allowed,'excludedFixtureDetailIndexOwnerStateAndGenerationObjects':True,'noTimingRowToReadinessJoinPerformed':True})
 # Shared implementation reused exactly; build mapping provenance, not new CLI generation.
 for external in [ROOT/'tools/qualification/run_supported_project_reads.py',Path('/tmp/story65-receiver-acceptance-prep-20261107/audit.py')]:put(external,Path('external-sources')/external.name)
 mapping=strict((OPS/'mapping-pins.json').read_bytes());put(mapping['mappingLog'],Path('mapping-provenance/mapping.log'))
 for i,(name,digest)in enumerate(mapping['files'].items()):assert sha(name)==digest;put(name,Path('mapping-provenance/files')/str(i)/Path(name).name)
 qualification=strict((SRC/'qualification/final-scratch-qualification.json').read_bytes());assert sha(SRC/'qualification/final-scratch-qualification.json')=='b1120707f751dc3ce6e8ec08f97de9d59567e5706782fd6e2ce2965e6e61525a'
 assert qualification['uniquePassingBehavioralCases']==188 and qualification['passingFreshCanaryCases']==3 and qualification['faultCasesTriggered']==100
 for rel,digest in qualification['preparedSourceSha256'].items():assert sha(SRC/rel)==digest
 assert len(qualification['sourceSha256'])==50 and all(sha(ROOT/rel)==digest for rel,digest in qualification['sourceSha256'].items())
 save('source-preservation.json',{'source50Sha256':qualification['sourceSha256'],'source50MatchedAfterOrdinaryRestorationAndPackaging':True,'ordinaryExecutorSourceUnchanged':True,'noProductSourceGitBuildOrHostChangesByPackaging':True,'qualifiedPreparedSourceHashesStillExact':True,'canonicalOwnerRevision':'3d4ba67db99f2f0bcd64b7d353609b555870cc9b'})
 run=strict((LIVE/'managed/run.json').read_bytes());assert all(re.fullmatch(r'[0-9a-f]{40}',v)for v in run['revisions'].values())
 native=strict((OPS/'native-private-139-75txjnq7/receipt.json').read_bytes());assert native['passed']and native['caseCount']==139 and native['exactHistoricalCaseSetRetained']and native['nativeProcessAndDescendantsAbsent']
 refs=[]
 for n in ['story-6-5-parsed-template-ordinary-20261107','story-6-5-parsed-template-cpu-20261107','story-6-5-capture-counter-20261107']:
  p=ROOT/'_bmad-output/implementation-artifacts/evidence'/n;refs.append({'packet':str(p.relative_to(ROOT)),'SHA256SUMSSha256':sha(p/'SHA256SUMS')})
 save('qualification-and-historical-references.json',{'currentNativeUniquePassingCases':1038,'ownerNativeCases':412,'projectsNativeCases':626,'fullPriorNativePacketDuplicatedHere':False,'durablePackets':refs,'currentScratchBehavioralCases':188,'freshCanaryCases':3,'faultInjections':100,'controlledScratchProcesses':22,'actualPrivateNative139Cases':139,'native139IsExistingSubsetNotAdditionalUniqueCases':True,'privateNativeAndScratchAreDiagnosticQualificationNotFeatureAcceptance':True,'canonicalRevisions':run['revisions'],'featureAcceptance':False})
 save('actual-managed-result.json',{'diagnosticOnly':True,'featureAcceptance':False,'standardManagedLaneAllowedToFinishWithoutSignalsOrCutoff':True,'managedResult':run['result'],'originalFailure':run['failure'],'singleAttempts':sum(not x['multiple']for x in sanitized),'singleVerifiedPositiveAttempts':[x['attempt']for x in sanitized if not x['multiple']and x['positiveCandidatesVerified']],'multipleAttempts':sum(x['multiple']for x in sanitized),'multipleVerifiedPositiveAttempts':[x['attempt']for x in sanitized if x['multiple']and x['positiveCandidatesVerified']],'browserCases':0,'authenticatedBrowserPersistencePrivacyAccessibilityAcceptance':'not exercised; native seed failed','instrumentedMethodNotFeatureAcceptance':True,'buildGenerationEqualityNotInferredFromSourceEquality':True,'canonicalRevisions':run['revisions']})
 live_raw=(P/'actual-wave/projects.jsonl').read_bytes();closed=strict((P/'actual-wave/projects.closed.json').read_bytes());rows,counts,groups=validate_rows(live_raw,closed['runOrdinal'])
 stages={}
 for stage in counts:
  vals=sorted(x['elapsedTicks']*1000/x['frequency']for x in rows if x['stage']==stage);stages[ALIASES[stage]]={'n':len(vals),'medianMs':vals[len(vals)//2],'p95NearestRankMs':vals[math.ceil(.95*len(vals))-1],'maxMs':max(vals)}
 paired=[]
 for ordinal,group in sorted(groups.items()):
  phases={ALIASES[k]:v['elapsedTicks']*1000/v['frequency']for k,v in group.items()};paired.append({'opaqueLocalCaptureOrdinal':ordinal,'phaseMilliseconds':phases,'wholeMinusSumOfRecordedPhaseMilliseconds':phases['Whole']-sum(v for k,v in phases.items()if k!='Whole'),'finalScopeRecorded':False})
 save('actual-wave/aggregation.json',{'diagnosticOnly':True,'featureAcceptance':False,'localCaptures':3,'rows':21,'aliases':ALIASES,'stages':stages,'pairedLocalMethodPhasesOnly':paired,'allCountAllocationColumnsZero':True,'noRequestOrDownstreamCorrelation':True,'instrumentedWallOnly':True,'noOverheadSubtraction':True,'wholeDisposedOrUnwoundDoesNotMeanSuccess':True,'awaitedCallerCompletionDoesNotProveChildQuiescence':True,'noInputCountOrOwnerResultAttribution':True,'missingFinalScopeOnlyMeansNoRecordedEntry':True})
 print(json.dumps({'staged':str(P),'syntheticCompressedFiles':len(compressed),'syntheticRows':sum(x['rows']for x in compressed),'qualifiedRows':sum(x['rows']for x in compressed if x['originalRelativePath'].startswith('differential-results-qualified/')),'source50Match':True,'managedMultipleAttempts':sum(x['multiple']for x in sanitized)}))

def finalize():
 import datetime
 assert P.is_dir()
 removal=strict((P/'private-counter-removal.json').read_bytes())
 fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and os.listdir(fd)==[]
 finally:os.close(fd)
 save('independent-private-directory-recheck.json',{'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'directoryDevice':2096,'directoryInode':8752986,'directoryUid':1000,'mode':'0700','samePinnedIdentity':True,'directoryEmpty':True,'noSymlinkFollow':True,'controlOrCounterFilesRemaining':0})
 (P/'README.md').write_text('''This packet stages a bounded private diagnostic of the current ProjectAdvisoryQueryExecutor method. Story 6.5 acceptance remains failed. Existing ordinary native qualification is 412 owner plus 626 Projects = 1,038 unique passes; the durable parsed-template ordinary/CPU and capture-counter packets are referenced with checksums rather than duplicated. The actual private native139 run here retains an existing subset, not 139 additional unique tests or feature acceptance.

Only Projects.Server compilation substituted the original executor with a reviewed private copy plus the exact helper and a separate bootstrap. Production sources stayed unchanged. The source hashes are private executor b2b672c688355e504f1de87b870b1e0226f609a23fecbdb2dfbe5a162c43eede, bootstrap 8f5836d1ff4dabb4c5fe819ae762dfff0748b5d792f984ae1adf45af28f45375, and helper 112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498. All original Tasks, awaits, cancellation, catches, authority observations/order and the two-second deadline remain. All six source runtimes retain the standard existing proof profile, seven build mappings, natural CPU setting and privacy guards. The exact Aspire start command uses documented --no-build after nine prebuilds; no runtime configuration/environment recorder key was added. No profiler, public logger sink, request identifier or inherited Activity linkage was used.

Preparation identified that Thread.Start can carry the first caller's ExecutionContext into helper initialization. The earlier direct-Begin candidate was never application-executed and remains an explicitly unqualified historical source/ops snapshot. The separate synchronous, no-inline bootstrap suppresses flow only around the helper Begin call, restoring it before original application work. Helper112 is unchanged. Three fresh-process canaries qualify the actual first-business-call path: the deliberately unsafe positive control detects synthetic flow, while normal and already-suppressed bootstrap callers do not expose the synthetic canary to the drain and retain caller state/flow across 22 fake awaits each. These tests do not establish that protected values leaked in any earlier diagnostic; neither absence of value fields nor older timing receipts alone proved the earlier background context free of ambient state. The prior DevelopmentRouteCatalogInputs packet did execute helper112 without this new bootstrap; its schema excluded protected values, but background ExecutionContext isolation was not established. No value leakage is inferred. Earlier packets remain immutable.

Scratch qualification records 188 passing behavioral cases (47 each disabled/active/capped/failing sink), three passing fresh canaries, 100 triggered fault cases, and 22 owned controller processes exiting zero. Output/error/read-order/cancellation/late-child parity comparisons stayed in memory. Initial two compiler setup failures and the uncooperative-mock/controller race failure are retained separately from corrected final qualification. All 167,480 qualified synthetic rows plus 247 historical initial rows pass the closed primitive schema and zero count/allocation columns. Their 27 raw JSONL files are retained as byte-preserving gzip, with original/compressed hashes, row counts and decompression verification. They are synthetic harness records, separate from the 21 actual live records. No synthetic window.control is copied.

The private application pipeline then rebuilt the nine existing carriers with BuildProjectReferences=false, Debug, the existing two proof flags and exact mapping profile. Actual Server.Tests ran the canonical 139 ProjectAdvisoryQueryTests cases: the exact historical case set passed without failures/errors/skips, and a mandatory sample verified that its owned process mapped the private Server copy/producer with the recorded hash. Native PID/group exit and an empty recorder directory were established before managed startup. All eight SDK copies stayed ordinary at 5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872. The actual owned Projects host then mapped private Server DLL/PDB, independently of SDK coherence.

Admission opened for eight monotonic seconds after a genuine failed MULTIPLE readiness observation, with no positive observed and sufficient attempts remaining. The actual closed receipt has three admitted/disposed-or-unwound captures and 21 rows, final counters and zero active/pending/queue/closed/cap drops or I/O failures. Every count/allocation field is zero. Old helper stage spellings are aliases only: ComponentFile=InitialScope, ComponentPolicy=InitialFilterInputs, AclFile=IndexRead, SettingsEnumerationSortDictionary=DiscoveryParallel, AuthenticationSerialization=FinalFilterInputs, ActorBoundary=FinalCandidateParallel, ResultTail=FinalScope. No configuration enumeration is being measured in this packet.

The three local whole-method wall durations are 2004.595, 2001.263 and 2000.060 ms. InitialFilterInputs takes 605.972/617.003/634.854 ms; DiscoveryParallel 631.280/595.731/609.488 ms; FinalFilterInputs 597.566/628.810/617.396 ms. FinalCandidateParallel is entered for 165.420/156.422/135.043 ms before unwinding. InitialScope is 1.552–1.983 ms and IndexRead 1.095–1.158 ms. No FinalScope row exists. The current local executions therefore advance into the fourth substantial wave after three roughly 600 ms waves. This differs from older measurements and localizes where to investigate next; it does not identify downstream owner cost, input cardinality, a specific request or authorization result. Exact per-capture values and nearest-rank P95/max are in actual-wave/aggregation.json.

These are instrumented method-local wall durations. Opaque run/capture ordinals pair local phases only; no root/request/trace/subject/route/child-call join was collected or reconstructed. A disposed/unwound scope is not a successful operation. Completion of a caller's WaitAsync does not prove downstream work quiesced. Missing FinalScope means no recorded phase entry. Instrumentation and background writing can perturb execution; no overhead was subtracted, no exact CPU or ordinary latency benefit is claimed, and no production repair follows from these coarse spans alone. Detailed fixture index/detail/owner-state/generation objects are excluded; a fixed scalar readiness projection and original hash preserve the failed30 result without correlating it to timing captures.

The managed lane ran to normal completion: SINGLE passed attempt2, MULTIPLE failed all30 attempts, the native seed failed and no browser case ran. Authenticated OIDC browser persistence/privacy/accessibility acceptance remains unexercised. All original deadlines and expectations remain. Eight retained read-only cleanup observations show the initial delay truthfully: early12 identities were absent by observation7, and all21 recorded process identities/five containers by observation8. No additional signal or cutoff was sent. Namespace/private material, original preservation flags and native descendants all passed; all17,200 full live manifest source inputs matched.

Ordinary nine-project restoration and root's independent audit completed. All ten Server DLL/PDB/ref/refint outputs are coherent with ordinary Server39bb2ca3dfb51e1f62eb6eae79c0ded11eff6a4556dfa112ec58f131bcf30c73 / PDB83690e9480051111f852141ca10335bc8fdf030da393e8c621501759145f98be. All147 runtime assembly copies are free of private markers, and SDK8/PDB copies remain coherent with ordinary5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872. Source50 hashes match. Build generations and diagnostic qualification are recorded separately; source equality does not imply byte equivalence with a different generation.

After root released restoration, the actual projects.jsonl and projects.closed.json were strictly validated, hash-copied plain, and only those two files plus numeric window.control were removed through the pinned no-follow directory descriptor. The control's hash/size are retained, never its contents. UID1000/device2096/inode8752986/mode0700 and empty-directory identity were independently rechecked. No private configuration, key/token/TrustProof, observed object, wire payload, raw resource graph, inherited correlation record, application binary or runtime trace is included. This temp packet awaits parent audit before repository evidence copy or spec updates; no repository file was changed by packaging.
''')
 put('/tmp/story65-advisory-wave-package.py',Path('packaging-source.py'))
 violations=[];badnames=[];files=[]
 jwt=rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}'
 for f in P.rglob('*'):
  if not f.is_file():continue
  assert not f.is_symlink();files.append(f)
  if f.suffix.lower()in ['.dll','.pdb','.pfx','.pem','.key','.der','.nettrace','.etlx']or f.name in ['window.control','fixture.json','endpoints.json','apphost-session.json']:badnames.append(str(f.relative_to(P)))
  data=gzip.decompress(f.read_bytes())if f.suffix=='.gz'else f.read_bytes()
  if re.search(jwt,data)or(bytes([45])*5+b'BEGIN PRIVATE KEY'+bytes([45])*5)in data or(bytes([45])*5+b'BEGIN RSA PRIVATE KEY'+bytes([45])*5)in data:violations.append(str(f.relative_to(P)))
 assert not violations and not badnames
 save('privacy-and-exclusions.json',{'onlyClosedActualCounterMetadataRetained':True,'actualRowsStrictDuplicateFreeSchema':True,'allActualCountAllocationFieldsZero':True,'noInheritedActivityHttpContextRequestSubjectOrRouteFieldsInCounterRows':True,'firstHelperThreadStartFlowBoundaryCanariesQualified':True,'earlierDirectBeginSourceUnqualifiedAndLimitationExplicit':True,'noInferenceThatEarlierProtectedValuesLeaked':True,'detailedFixtureObservedObjectsExcluded':True,'noPrivateConfigurationCredentialsTrustProofRuntimeGraphOrControlContentsCopied':True,'noApplicationBinariesRuntimeTraceOrDerivativeFiles':True,'noJwtOrPrivateKeyPatternsIncludingDecompressedSyntheticRows':True,'privateDirectorySameIdentityEmptyAfterExactRemoval':True,'matchedValuesPrinted':False,'featureAcceptance':False})
 files=sorted(f for f in P.rglob('*')if f.is_file()and f.name!='SHA256SUMS')
 (P/'SHA256SUMS').write_text(''.join(sha(f)+'  '+str(f.relative_to(P))+'\n'for f in files))
 for line in(P/'SHA256SUMS').read_text().splitlines():digest,name=line.split('  ',1);assert sha(P/name)==digest
 print(json.dumps({'packet':str(P),'entries':len(files),'totalBytes':sum(f.stat().st_size for f in files),'SHA256SUMSSha256':sha(P/'SHA256SUMS'),'privacyPatterns':0,'directoryEmpty':True,'featureAcceptance':False}))
