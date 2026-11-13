"""Read-only closed scalar evidence audit; no target operations or raw trace reads."""
from pathlib import Path
import collections,datetime,hashlib,json,math,os,re,stat,statistics,xml.etree.ElementTree as ET
ROOT=Path('/home/administrator/projects/hexalith/projects');BASE=Path('/tmp/story65-gateway-current-cpu-owner-profile-20261108');ROLE=BASE/'gateway';LIVE=Path('/tmp/story65-gateway-current-cpu-owner-live-20261108');PREP=Path('/tmp/story65-gateway-current-cpu-prep-20261108');OUT=Path(__file__).parent
bound={}
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def load(p):
 p=Path(p);bound[str(p)]=sha(p)
 def pairs(items):
  d={}
  for k,v in items:assert k not in d;d[k]=v
  return d
 return json.loads(p.read_bytes(),object_pairs_hook=pairs)
def close(a,b):return math.isclose(a,b,rel_tol=1e-12,abs_tol=1e-8)
s=load(ROLE/'cpu-summary.json');w=load(BASE/'watcher-receipt.json');co=load(BASE/'coordinator-receipt.json');attached=load(ROLE/'attached.json');collection=load(ROLE/'collection-receipt.json');ids=load(ROLE/'identity-checks.json');sup=load(Path(str(LIVE)+'-supervisor.json'));cleanup=load(Path(str(LIVE)+'-independent-cleanup-source-audit.json'));interpret=load(Path('/tmp/story65-gateway-current-cpu-evidence-20261108/cpu-interpretation.json'));pins=load(PREP/'watcher/execution-pins.json');source=load(PREP/'watcher/source-controls.json');apps=load(PREP/'watcher/current-app147-prelaunch.json')
assert bound[str(PREP/'watcher/execution-pins.json')]=='7cdb317a07f5f72540f7558c39019d36e4ef4bf507a4db29dc729019862e8405'
assert w['phase']=='profile-finished'and w['profilerAttached']and not w['profileIncomplete']and w['privateIdentityFilesAbsent']
assert not co['incomplete']and co['allOwnedDiagnosticGroupsAbsent']and co['allCollectorsAndGroupsClosedBeforeAnyParser']and co['postCollectionGuardPassed']
assert not co['targetSignalsSent']and co['actualAttachedCount']==1 and co['singleFixedGatewayTarget']and co['collectionWindowSecondsPerSession']==8
assert attached['Pid']==collection['Pid']==ids['Pid']==w['targetIdentities']['gateway']['pid']and str(attached['StartTicks'])==w['targetIdentities']['gateway']['startTicks']
assert attached['provider']=='Microsoft-DotNETCore-SampleProfiler'and attached['keywords']=='0x0'and attached['level']==4 and attached['gcProvider']=='Microsoft-Windows-DotNETRuntime'and attached['gcKeywords']=='0x1'and attached['gcLevel']==4
assert attached['maxSeconds']==8 and attached['bufferMiB']==64 and attached['fileCapBytes']==256*1024*1024 and not attached['allocationTicksRequested']and attached['requestStackwalk']and attached['requestRundown']
assert collection['reason']=='duration'and not collection['incomplete']and not collection['capReached']and collection['stopCompleted']and collection['drainCompleted']and not collection['targetGcRequested']and collection['onlyOwnedSessionStopped']
assert collection['rawSha256']==s['rawSha256']and collection['writtenBytes']==collection['observedBytes']<attached['fileCapBytes']
assert ids['role']=='gateway'and ids['argvStructuralWhitelistPassed']and not ids['argvPrintedOrRetained']
checks={x['Label']:x for x in ids['checks']};assert len(checks)==len(ids['checks'])and all(x['Matches']for x in checks.values())
assert checks['source-audit']['Sha256']==sha(PREP/'watcher/source-controls.json')
for n,(rel,h)in enumerate(sorted(source['sourceSha256'].items())):assert checks[f'source-{n:03d}']['Sha256']==h and sha(ROOT/rel)==h
assert len(source['sourceSha256'])==59
for x in attached['immediateChecks']:assert x==checks[x['Label']]
for label,key in [('eventstore-entry-assembly','entryDll'),('gateway-assembly','gatewayDll'),('domain-service-assembly','domainServiceDll'),('sdk-assembly','sdkDll')]:
 role=load(PREP/'collector-guard-map.json')['roles'][0];assert sha(role[key])==checks[label]['Sha256']
assert checks['mapped-gateway-assembly']['Sha256']==checks['gateway-assembly']['Sha256']
assert set(checks)=={'runtime-executable','eventstore-entry-assembly','gateway-assembly','sdk-assembly','domain-service-assembly','owner-config','source-audit','mapped-gateway-assembly'}|{f'source-{n:03d}'for n in range(59)}
pre=w['currentOrdinaryGenerationProofs']['gateway'];post=co['postCollectionTargetIdentityGenerationAndMaps']['gateway'];assert pre['files']==post['files']and pre['pid']==post['pid']==attached['Pid']and str(pre['startTicks'])==str(attached['StartTicks'])
assert pre['fullManifestAllMatch']and post['fullManifestAllMatch']and pre['fullManifestSha256']==post['fullManifestSha256']==sha(LIVE/'source-manifest.json')
assert {x['label']:x['sha256']for x in pre['files']}=={k:checks[k]['Sha256']for k in ['eventstore-entry-assembly','gateway-assembly','sdk-assembly','domain-service-assembly']}
assert checks['sdk-assembly']['Sha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
assert sha(Path(role['sdkDll']).with_suffix('.pdb'))=='ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
for key in ['gatewayDll','sdkDll','domainServiceDll']:
 p=Path(role[key]);producer=ROOT/'references/Hexalith.EventStore/src'/p.stem/'bin/Debug/net10.0'/p.name
 assert sha(producer)==sha(p)and sha(producer.with_suffix('.pdb'))==sha(p.with_suffix('.pdb'))
assert sha(pins['toolPath'])==pins['toolSha256']=='7bbaef3570c82e5797d633c2f24e5333b21f0098ffbf7ac6bac448e70911ef9b'
app_changes=[p for p,h in apps['currentFilesSha256'].items()if sha(p)!=h]
for p in apps['currentFilesSha256']:
 data=Path(p).read_bytes();assert all(m.encode()not in data and m.encode('utf-16-le')not in data for m in ['Hexalith.PrivateCaptureCounter','TimingHexalith','PrivateFocusedTiming','Story65Timing'])
input_changes=[p for p,h in pins['inputsSha256'].items()if sha(p)!=h];assert set(input_changes)<=set(apps['currentFilesSha256'])
assert not s['exactCpuTimeClaimed']and s['externalIsNotProofOfBlocked']and not s['sanitizedRowsCapped']and not s['missingStack']and not s['truncatedStacks']and not s['uniqueGroupCapOverflowSamples']and not s['lostOnConvert']and not s['traceEventsLost']
groups={g['Id']:g for g in s['stackGroups']};assert len(groups)==len(s['stackGroups'])and set(groups)==set(range(1,len(groups)+1))
validname=re.compile(r'^[A-Za-z_<>][A-Za-z0-9_.+`<>, \[\]:&*()!~=?|\-]*$');badname=re.compile(r'\b0x[0-9a-fA-F]+\b|\.(?:cs|dll|exe|so|pdb)(?:\b|:)',re.I)
for g in groups.values():
 assert set(g)=={'Id','SampleType','Count','Frames'}and type(g['Count'])is int and g['Count']>0 and g['SampleType']in ['Managed','External','Error','Unknown']and 0<len(g['Frames'])<=192
 for f in g['Frames']:assert f=='[unresolved]'or(len(f)<=512 and validname.fullmatch(f)and'.'in f and not badname.search(f)and not any(ord(c)<32 or c in '/\\\"\''for c in f))
def unique_row_pairs(items):
 assert len(items)==len({x[0]for x in items});return dict(items)
count=collections.Counter();types=collections.Counter();last={};intervals=[];rowcount=0
with (ROLE/'cpu-samples.jsonl').open()as f:
 for line in f:
  row=json.loads(line,object_pairs_hook=unique_row_pairs);assert set(row)=={'timeUtc','relativeMilliseconds','threadId','sampleType','stackId'}
  assert type(row['threadId'])is int and row['threadId']>0 and type(row['stackId'])is int and row['stackId']in groups and row['sampleType']==groups[row['stackId']]['SampleType']
  t=row['relativeMilliseconds'];assert type(t)in[int,float]and math.isfinite(t)and t>=0 and re.fullmatch(r'\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{1,7}Z',row['timeUtc'])
  if row['threadId']in last:intervals.append(t-last[row['threadId']])
  last[row['threadId']]=t;count[row['stackId']]+=1;types[row['sampleType']]+=1;rowcount+=1
bound[str(ROLE/'cpu-samples.jsonl')]=sha(ROLE/'cpu-samples.jsonl');assert bound[str(ROLE/'cpu-samples.jsonl')]==s['sanitizedRowsSha256']
assert rowcount==s['samples']==s['sanitizedRows']==126709 and dict(types)==s['sampleTypeCounts']and all(count[k]==g['Count']for k,g in groups.items())
assert len(last)==s['sampledThreads']and len(intervals)==s['sameThreadIntervalCount']and close(sum(intervals)/len(intervals),s['sameThreadIntervalMeanMilliseconds'])and close(min(intervals),s['sameThreadIntervalMinMilliseconds'])and close(max(intervals),s['sameThreadIntervalMaxMilliseconds'])
assert sum(g['Count']*g['Frames'].count('[unresolved]')for g in groups.values())==s['unresolvedFrames']
assert close(types['Managed']/rowcount,s['managedSampleFraction'])and close(types['External']/rowcount,s['externalSampleFraction'])
for category in ['Managed','External','Error','Unknown']:
 gs=[g for g in groups.values()if g['SampleType']==category];n=sum(g['Count']for g in gs);leaf=collections.Counter();first=collections.Counter();inc=collections.Counter()
 for g in gs:
  leaf[g['Frames'][0]]+=g['Count'];first[next((x for x in g['Frames']if x.startswith('Hexalith.')),'[no retained Hexalith frame]')]+=g['Count']
  for x in set(x for x in g['Frames']if x.startswith('Hexalith.')):inc[x]+=g['Count']
 for key,c in [('leafPartition',leaf),('firstHexalithPartition',first),('inclusiveHexalithFrames',inc)]:
  rows=interpret['classAggregation'][category][key];assert {x['frame']:x['samples']for x in rows}==dict(c)and all(close(x['fractionOfClassSamples'],x['samples']/n if n else 0)for x in rows)
 assert interpret['classAggregation'][category]['classSamples']==n
assert interpret['sourceSummarySha256']==sha(ROLE/'cpu-summary.json')and interpret['totalSamples']==rowcount and interpret['sampleTypeCounts']==dict(types)
external=collections.Counter();wait_tokens=['System.Threading.LowLevelLifoSemaphore.Wait','System.Threading.WaitHandle.Wait','System.Threading.Monitor.Wait','System.Threading.ManualResetEventSlim.Wait','System.Threading.Thread.Sleep']
for g in groups.values():
 if g['SampleType']!='External':continue
 fs=g['Frames']
 if any(any(token in f for token in wait_tokens)for f in fs):kind='recognized-managed-wait-api-visible'
 elif any('System.Security.Cryptography'in f or'Interop+Crypto'in f for f in fs):kind='crypto-api-visible-native-work-or-transition'
 elif any('System.IO.'in f or'Interop+Sys.Read'in f or'FileStream'in f for f in fs):kind='file-or-os-read-api-visible-native-work-or-wait'
 elif any('System.Net.'in f for f in fs):kind='network-api-visible-native-work-or-wait'
 else:kind='other-external-unclassified'
 external[kind]+=g['Count']
assert {x['category']:x['samples']for x in interpret['externalHeuristicPartitions']}==dict(external)and all(close(x['fractionOfExternalSamples'],x['samples']/types['External'])for x in interpret['externalHeuristicPartitions'])
def union(rows):
 total=0;start=end=None
 for a,b in sorted(rows):
  if start is None:start,end=a,b
  elif a<=end:end=max(end,b)
  else:total+=end-start;start,end=a,b
 return total+(end-start if start is not None else 0)
for reason,values in interpret['suspendRestartByReason'].items():
 rows=[x for x in s['suspendRestartPairs']if x['reason']==reason];ds=[x['durationMilliseconds']for x in rows]
 assert len(rows)==values['pairedCount']and close(sum(ds),values['sumWallMilliseconds'])and close(statistics.median(ds),values['medianWallMilliseconds'])and close(max(ds),values['maximumWallMilliseconds'])and close(union((x['startMilliseconds'],x['endMilliseconds'])for x in rows),values['unionWallMilliseconds'])
cycles=s['gcCyclePairs'];ds=[x['wallDurationMilliseconds']for x in cycles];ci=interpret['gcStartStopCollectionCycles'];assert len(cycles)==ci['pairedCount']and close(sum(ds),ci['sumWallMilliseconds'])and close(statistics.median(ds),ci['medianWallMilliseconds'])and close(max(ds),ci['maximumWallMilliseconds'])and close(union((x['startMilliseconds'],x['endMilliseconds'])for x in cycles),ci['unionWallMilliseconds'])
allowed={'identity-checks.json','attached.json','collection-receipt.json','cpu-samples.jsonl','cpu-summary.json','supervisor-receipt.json'};assert {p.name for p in ROLE.iterdir()}==allowed
assert stat.S_IMODE(ROLE.stat().st_mode)==0o700 and all(p.is_file()and not p.is_symlink()and stat.S_IMODE(p.stat().st_mode)==0o600 for p in ROLE.iterdir())
removed=co['privateRemoval']['gateway'];assert removed['allNonAllowlistedPrivateFilesAbsent']and removed['allSubdirectoriesRemoved']and all(x['absenceVerified']for x in removed['removed'])
assert {(x['knownName'],x['sha256'])for x in removed['removed']}=={('execution.nettrace',s['rawSha256']),('execution.etlx','a0841318b99537e1568a7c6759e559c96830f82e5162c998e8bdc107435cf641')}
assert not any(BASE.glob('*private-identity*'))
diag=[(w['watcherPid'],w['watcherStartTicks'])];groups_to_check={w['watcherGroup']};stage_receipts=[]
for stage in ['collect','analyze']:
 x=load(BASE/f'gateway.{stage}-supervisor.json');stage_receipts.append(x);assert not x['incomplete']and x['toolSha256']==pins['toolSha256']and x['child']['exitCode']==0 and x['child']['ownedChildExited']and x['child']['guardReason']is None and not x['child']['onlyDirectlyOwnedDiagnosticChildSignaled']
 diag.extend([(x['supervisorPid'],x['supervisorStartTicks']),(x['child']['childPid'],x['child']['childStartTicks'])]);groups_to_check.add(x['supervisorGroup'])
assert co['allCollectorsClosedAtMonotonicNs']<co['parserProcesses'][0]['launchedAtMonotonicNs']
all_ids=[(x['pid'],x['startTicks'])for x in cleanup['identities']]+diag;groups_to_check.update(x['groupId']for x in cleanup['nativeTestProcessCleanup']);observed={};permission_errors=0
for p in Path('/proc').iterdir():
 if p.name.isdigit():
  try:
   f=(p/'stat').read_text().rsplit(')',1)[1].split()
   if f[0]!='Z':observed[int(p.name)]=(f[19],int(f[2]))
  except(FileNotFoundError,ProcessLookupError):pass
  except PermissionError:permission_errors+=1
assert all(observed.get(pid,(None,None))[0]!=str(ticks)for pid,ticks in all_ids)and all(v[1]not in groups_to_check for v in observed.values())and permission_errors==0
assert sup['managedExitCode']==1 and sup['watcherExitCode']==0 and sup['cleanup']['passed']and cleanup['fullLiveManifestHashMismatches']==0
for key in ['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent']:assert cleanup[key]
manifest=load(LIVE/'source-manifest.json');nowdiff=[{'path':rel,'closedExpectedSha256':h,'currentSha256':sha(ROOT/rel)}for rel,h in manifest.items()if sha(ROOT/rel)!=h]
assert len(manifest)==17355 and not nowdiff
spec=ROOT/'_bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md';assert str(spec.relative_to(ROOT))not in manifest
ready=[]
for line in (LIVE/'browser-advisory-seed-readiness.jsonl').read_bytes().split(b'\n')[:-1]:
 d=json.loads(line);assert type(d['multiple'])is bool;ready.append(d)
positive=lambda x:x.get('positiveCandidatesVerified')is True
single=[x for x in ready if not x['multiple']];multi=[x for x in ready if x['multiple']]
assert(len(single),sum(map(positive,single)),len(multi),sum(map(positive,multi)))==(1,1,30,0)
first3=multi[:3];assert len({x['attempt']for x in first3})==3 and all(x['httpStatus']==200 and x['actualPublicGateway']and not x['fabricatedProjectionWrite']and x['nonBootstrapCredential']and not x['runtimeAuthoritySupplied']and x['incomplete']and x['candidateCount']==0 and not positive(x)for x in first3)
xmlbytes=(LIVE/'browser-seed.xml').read_bytes();assert b'<!DOCTYPE'not in xmlbytes and b'<!ENTITY'not in xmlbytes;tests=list(ET.fromstring(xmlbytes).iter('test'));assert len(tests)==1 and tests[0].get('type')=='Hexalith.Projects.Integration.Tests.SupportedProjectBrowserSeedTests'and tests[0].get('result')=='Fail'
bound[str(LIVE/'browser-seed.xml')]=sha(LIVE/'browser-seed.xml');bound[str(LIVE/'browser-advisory-seed-readiness.jsonl')]=sha(LIVE/'browser-advisory-seed-readiness.jsonl')
managed_first=interpret['classAggregation']['Managed']['firstHexalithPartition'];fresh=next(x for x in managed_first if 'RouteCatalogFreshStreamImageDigest.Compute('in x['frame']);domain=sum(g['Count']for g in groups.values()if g['SampleType']=='Managed'and any('DevelopmentDomainRouteCatalog.ReadExecutableDigest('in x for x in g['Frames']));crypto=[g for g in groups.values()if g['SampleType']=='External'and any('System.Security.Cryptography'in x or'Interop+Crypto'in x for x in g['Frames'])]
result={'passed':True,'diagnosticOnly':True,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'auditSourceSha256':sha(__file__),'evidenceSha256':bound,'strictRows':rowcount,'strictRowFieldCount':5,'stackGroupCount':len(groups),'groupCountAndTypeAndThreadIntervalAndSanitizerAllReconciled':True,'sampleTypeCounts':dict(types),'sampledThreads':len(last),'unresolvedFrameSampleOccurrences':s['unresolvedFrames'],'lostEvents':0,'missingOrTruncatedStacks':0,'rowOrGroupCapsReached':False,'suspensionPairs':len(s['suspendRestartPairs']),'suspensionOverwriteAndUnpairedRestarts':{'overwritten':s['overwrittenSuspendStarts'],'unpairedRestarts':s['unpairedRestarts'],'unpairedStarts':s['unpairedSuspendStarts']},'gcCyclePairs':len(cycles),'unpairedGcStarts':s['unpairedGcStarts'],'allInterpretationClassPartitionsAndGcWallAggregatesRecomputed':True,'freshStreamFirstManaged':fresh,'domainReadDigestInclusiveManagedSamples':domain,'externalCryptoApiVisibleSamples':sum(g['Count']for g in crypto),'externalDecodePkcs8Samples':sum(g['Count']for g in groups.values()if g['SampleType']=='External'and any('DecodePkcs8'in x for x in g['Frames'])),'externalRsaSignHashSamples':sum(g['Count']for g in groups.values()if g['SampleType']=='External'and any('System.Security.Cryptography.RSAOpenSsl.SignHash('in x for x in g['Frames'])),'source59CurrentAllMatch':True,'all267InputsCurrentExceptPermittedFreshApp147Generations':True,'freshApp147GenerationDifferences':app_changes,'all147CurrentMarkerFree':True,'mappedIdentityLabelsAndCurrentProducerCopyDllPdbMatched':True,'watcherAllFourMappedPathsAndPrePostGenerationProofsMatch':True,'collectorOnlyGatewayMappedHashIndependentlyEqualsPinned':True,'oldCollectorLimitations':['SafeCommand accepts broader native/dotnet marker positioning than later receiver collector; external watcher proves exact source/DCP owned entry and immutable PID/start/path/hash. No newer positional guard is claimed.','Collector verifies only Gateway map and emits its fresh mapped hash as Matches=true without equality enforcement. This audit independently checked actual emitted hash equals pinned Gateway; watcher independently requires entry/Gateway/SDK/Domain maps with current producer-copy DLL/PDB before and after.','Old collector has no SourceProject identity field. Actual graph resource eventstore, collector Resource eventstorehost and Rolegateway are distinct fixed bindings; source ownership is established by the external watcher.'],'ordinarySdkSha256':checks['sdk-assembly']['Sha256'],'domainSha256':checks['domain-service-assembly']['Sha256'],'normalDurationBounded8SecondCollection':True,'rawAndEtlxHashChainsMatchAndAllNonallowlistedFilesAbsent':True,'privateIdentityAbsent':True,'privateOwnerPermissions':{'directory':'0700','files':'0600'},'all5DiagnosticIdentitiesAnd3GroupsFreshlyAbsent':True,'all21ManagedIdentitiesAndNativeSeedGroupFreshlyAbsent':True,'freshProcPermissionErrors':permission_errors,'fiveContainersAbsentFromClosedIndependentCleanupAudit':True,'cleanupClosedFullManifestZeroMismatches':True,'closedFullManifestCount':len(manifest),'currentFullManifestPostclosureDocumentationDelta':nowdiff,'specExcludedFromLiveManifest':True,'authorizedPostclosureSpecSha256':sha(spec),'postclosureSpecChangeNotRetroactivelyWhitelisted':True,'outcome':{'managedExitCode':1,'watcherExitCode':0,'singleAttempts':len(single),'singleVerifiedPositives':sum(map(positive,single)),'multipleAttempts':len(multi),'multipleVerifiedPositives':sum(map(positive,multi)),'onlySeedXmlTestFailed':True,'browserAcceptanceAssertionsObserved':0,'matrixCompleted':False},'limits':['All Gateway-process samples, not query- or request-attributed; no joins to earlier phase ordinals, roots, services, outcomes or expiry.','Sample counts and Managed/External classifications are not exact CPU durations or blocked time. Safepoint/sampler perturbation and unresolved frames remain.','Inclusive frame counts overlap. GC collection-cycle wall intervals include concurrent/background work; suspension overwrites/unpaired restarts remain explicit.','Closed and current full manifests both match17355 paths. The spec is excluded from that manifest; its authorized postclosure publication is recorded separately.','Prior native1048 was not requalified or inflated by this diagnostic run.']}
(OUT/'independent-profile-audit.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'passed':True,'report':str(OUT/'independent-profile-audit.json'),'sha256':sha(OUT/'independent-profile-audit.json'),'samples':rowcount,'groups':len(groups),'managed':types['Managed'],'external':types['External'],'freshManaged':fresh['samples'],'domainInclusive':domain,'cryptoExternal':result['externalCryptoApiVisibleSamples'],'inputGenerationChanges':len(app_changes),'currentPostclosureDocDeltas':len(nowdiff)},indent=2))
