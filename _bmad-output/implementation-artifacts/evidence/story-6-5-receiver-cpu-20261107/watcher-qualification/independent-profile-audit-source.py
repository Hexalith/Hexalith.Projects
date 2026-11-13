# Saved reconstruction of the two actual inline read-only audit cells.
# Their checks passed before this source was saved; this saved file was not run.
# No application, parser, profiler, diagnostic attach or topology operation occurs here.
import pathlib,json,hashlib,collections,re,math,stat,os
b=pathlib.Path('/tmp/story65-receiver-cpu-owner-profile-20261107')
root=pathlib.Path('/home/administrator/projects/hexalith/projects')
roles=['projects','tenants','folders']
def read(p):
 def pairs(items):
  d={}
  for k,v in items:
   assert k not in d
   d[k]=v
  return d
 return json.loads(p.read_bytes(),object_pairs_hook=pairs)
c=read(b/'coordinator-receipt.json');w=read(b/'watcher-receipt.json')
assert c['incomplete']is False and c['postCollectionGuardPassed']and c['allOwnedDiagnosticGroupsAbsent']and c['allCollectorsAndGroupsClosedBeforeAnyParser']
assert w['anyCollectorLaunched']and w['profilerAttached']and not w['profileIncomplete']and w['privateIdentityFilesAbsent']
removed={x['role']:x for x in w['privateIdentityRemoval']}
allowed={'identity-checks.json','attached.json','collection-receipt.json','cpu-samples.jsonl','cpu-summary.json','supervisor-receipt.json'}
namepat=re.compile(r'^[A-Za-z_<>][A-Za-z0-9_.+`<>, \[\]:&*()!~=?|\-]*$')
badext=re.compile(r'\b0x[0-9a-fA-F]+\b|\.(?:cs|dll|exe|so|pdb)(?:\b|:)',re.I)
def goodframe(f):
 return f in ['[unresolved]','[unique-key-cap]']or(isinstance(f,str)and 0<len(f)<=512 and not any(ord(x)<32 or ord(x)==127 for x in f)and not any(x in f for x in ['/',chr(92),'"',"'"])and not badext.search(f)and namepat.fullmatch(f)and'.'in f)
allids=[]
for role in roles:
 d=b/role;ds=d.lstat()
 assert stat.S_ISDIR(ds.st_mode)and stat.S_IMODE(ds.st_mode)==0o700 and ds.st_uid==os.getuid()and set(x.name for x in d.iterdir())==allowed
 assert(ds.st_dev,ds.st_ino)==(c['privateRemoval'][role]['rootDevice'],c['privateRemoval'][role]['rootInode'])
 for p in d.iterdir():
  s=p.lstat();assert stat.S_ISREG(s.st_mode)and stat.S_IMODE(s.st_mode)==0o600 and s.st_uid==os.getuid()
 identity=read(d/'identity-checks.json');attach=read(d/'attached.json');closed=read(d/'collection-receipt.json');summary=read(d/'cpu-summary.json')
 assert identity['role']==role and identity['argvStructuralWhitelistPassed']and identity['argvPrintedOrRetained']is False and identity['inputManifestSha256']==removed[role]['sha256']
 assert all(x['Matches']is True for x in identity['checks']+attach['immediateChecks'])and identity['checks']==attach['immediateChecks']
 assert(attach['Pid'],str(attach['StartTicks']))==(identity['Pid'],str(identity['StartTicks']))==(w['targetIdentities'][role]['pid'],str(w['targetIdentities'][role]['startTicks']))
 assert attach['provider']=='Microsoft-DotNETCore-SampleProfiler'and attach['keywords']=='0x0'and attach['level']==4 and attach['gcProvider']=='Microsoft-Windows-DotNETRuntime'and attach['gcKeywords']=='0x1'and attach['gcLevel']==4
 assert attach['requestStackwalk']and attach['requestRundown']and attach['allocationTicksRequested']is False and attach['bufferMiB']==64 and attach['fileCapBytes']==256*1024*1024 and attach['maxSeconds']==8
 assert closed['reason']=='duration'and closed['failureType']=='none'and closed['incomplete']is False and closed['capReached']is False and closed['stopCompleted']and closed['drainCompleted']and closed['targetGcRequested']is False and closed['onlyOwnedSessionStopped']
 assert 0<closed['writtenBytes']==closed['observedBytes']<attach['stopThresholdBytes']<attach['fileCapBytes']and closed['collectorCpuMilliseconds']<4000 and closed['collectorPeakObservedRssBytes']<512*1024*1024
 rawrem=next(x for x in c['privateRemoval'][role]['removed']if x.get('knownName')=='execution.nettrace')
 assert summary['rawSha256']==closed['rawSha256']==rawrem['sha256']and rawrem['bytes']==closed['writtenBytes']
 assert all(x['absenceVerified']for x in c['privateRemoval'][role]['removed'])and c['privateRemoval'][role]['allNonAllowlistedPrivateFilesAbsent']and c['privateRemoval'][role]['allSubdirectoriesRemoved']
 assert hashlib.sha256((d/'cpu-samples.jsonl').read_bytes()).hexdigest()==summary['sanitizedRowsSha256']
 groups=summary['stackGroups'];byid={g['Id']:g for g in groups};assert len(byid)==len(groups)
 for g in groups:assert set(g)=={'Id','SampleType','Count','Frames'}and type(g['Id'])is int and g['Id']>0 and type(g['Count'])is int and g['Count']>0 and g['SampleType']in ['Managed','External','Error','Unknown']and len(g['Frames'])<=192 and all(goodframe(x)for x in g['Frames'])
 perstack=collections.Counter();perclass=collections.Counter();threads=set();rows=0
 for line in(d/'cpu-samples.jsonl').read_bytes().splitlines():
  v=json.loads(line)
  assert set(v)=={'timeUtc','relativeMilliseconds','threadId','sampleType','stackId'}and type(v['threadId'])is int and v['threadId']>0 and type(v['stackId'])is int and v['stackId']in byid and v['sampleType']==byid[v['stackId']]['SampleType']and type(v['relativeMilliseconds'])in[int,float]and math.isfinite(v['relativeMilliseconds'])and v['relativeMilliseconds']>=0
  assert re.fullmatch(r'\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?Z',v['timeUtc'])
  perstack[v['stackId']]+=1;perclass[v['sampleType']]+=1;threads.add(v['threadId']);rows+=1
 assert perstack==collections.Counter({g['Id']:g['Count']for g in groups})and dict(perclass)==summary['sampleTypeCounts']
 assert rows==summary['samples']==summary['sanitizedRows']==sum(g['Count']for g in groups)and len(threads)==summary['sampledThreads']and summary['sameThreadIntervalCount']==rows-len(threads)
 assert all(summary[k]==0 for k in ['missingStack','truncatedStacks','uniqueGroupCapOverflowSamples','lostOnConvert','traceEventsLost'])and summary['sanitizedRowsCapped']is False
 assert sum(g['Count']*g['Frames'].count('[unresolved]')for g in groups)==summary['unresolvedFrames']
 assert summary['exactCpuTimeClaimed']is False and summary['externalIsNotProofOfBlocked']and summary['suspendAndGcCycleIntervalsAreWallTimeNotCpu']and summary['gcReasonPairingDoesNotMakeAllRuntimeSuspensionsGc']and all(summary[k]is False for k in ['symbolLookupRequested','heapDumpRequested','forcedGcRequested'])
 for stage in ['collect','analyze']:
  sup=read(b/(role+'.'+stage+'-supervisor.json'));child=sup['child']
  assert sup['incomplete']is False and child['exitCode']==0 and child['ownedChildExited']and child['guardReason']is None and child['supervisorErrorType']is None and child['onlyDirectlyOwnedDiagnosticChildSignaled']is False and child['unrecognizedStdoutStderrRetained']is False
  allids.append((child['childPid'],child['childStartTicks'],child['childGroup']))
for x in c['collectorProcesses']+c['parserProcesses']:
 assert x['exitCode']==0 and x['processGroupAbsent']and x['remainingDiagnosticGroupMembers']==[]
 allids.append((x['pid'],x['startTicks'],x['processGroup']))
assert max(x['launchedAtMonotonicNs']for x in c['collectorProcesses'])<c['allCollectorsClosedAtMonotonicNs']<min(x['launchedAtMonotonicNs']for x in c['parserProcesses'])
for previous,current in zip(c['parserProcesses'],c['parserProcesses'][1:]):
 prior=read(b/(previous['role']+'.analyze-supervisor.json'))['child']
 assert previous['launchedAtMonotonicNs']+int(prior['wallSeconds']*1e9)<current['launchedAtMonotonicNs']
live=[]
for p in pathlib.Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:
  x=(p/'stat').read_text().rsplit(')',1)[1].split()
  if x[0]!='Z':live.append((int(p.name),x[19],int(x[2])))
 except(FileNotFoundError,ProcessLookupError,PermissionError):pass
for pid,start,group in allids:assert not any(a==pid and str(t)==str(start)for a,t,g in live)and not any(g==group for a,t,g in live)
assert len(allids)==12
# The second executed inline cell checked these file/receipt bindings.
guard=read(pathlib.Path('/tmp/story65-receiver-cpu-prep-20261107/collector-guard-map.json'));rolemap={x['role']:x for x in guard['roles']}
manifestpath=pathlib.Path('/tmp/story65-receiver-cpu-owner-live-20261107/source-manifest.json');manifest=read(manifestpath)
assert len(manifest)==17340 and all(hashlib.sha256((root/n).read_bytes()).hexdigest()==h for n,h in manifest.items())
fullhash=hashlib.sha256(manifestpath.read_bytes()).hexdigest()
for role in roles:
 value=w['targetIdentities'][role];early=w['currentOrdinaryGenerationProofs'][role];post=c['postCollectionTargetIdentityGenerationAndMaps'][role]
 for proof in [early,post]:assert proof['source55Match']and proof['sourceCount']==55 and proof['fullManifestCount']==17340 and proof['fullManifestSha256']==fullhash and proof['fullManifestAllMatch']and proof['role']==role and proof['pid']==value['pid']and str(proof['startTicks'])==str(value['startTicks'])
 assert early['files']==post['files']and early['currentOrdinaryModuleGenerations']==post['currentOrdinaryModuleGenerations']
 checks={x['Label']:x for x in read(b/role/'identity-checks.json')['checks']}
 for label,key in [('target-entry-assembly','entryDll'),('sdk-assembly','sdkDll'),('domain-service-assembly','domainServiceDll')]:
  h=hashlib.sha256(pathlib.Path(rolemap[role][key]).read_bytes()).hexdigest()
  assert checks[label]['Matches']and checks['mapped-'+label]['Matches']and checks[label]['Sha256']==checks['mapped-'+label]['Sha256']==h
  assert next(x['sha256']for x in post['files']if x['label']==label)==h
 assert checks['sdk-assembly']['Sha256']=='5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872'
 assert hashlib.sha256(pathlib.Path(rolemap[role]['sdkDll']).with_suffix('.pdb').read_bytes()).hexdigest()=='80a7174c81e2dc92d8abc4eea2bc371b672066d85c444f66e437dbe13e168af7'
 # In the actual inline cell all three recorded targets were already absent. Their maps were therefore not opened again.
for role in roles:assert not(b/(role+'-private-identity.json')).exists()and not(b/(role+'-private-identity.json')).is_symlink()
known={'watcher-receipt.json','coordinator-receipt.json'}|{role+'.'+stage+'-supervisor.json'for role in roles for stage in ['collect','analyze']}|set(roles)
assert set(x.name for x in b.iterdir())==known
windows=c['actualAttachWindows'];frequency=windows[0]['frequency']
spread=1000*(max(x['attachedTicks']for x in windows)-min(x['attachedTicks']for x in windows))/frequency
overlap=max(0,1000*(min(x['beginTicks']+8*frequency for x in windows)-max(x['attachedTicks']for x in windows))/frequency)
assert math.isclose(spread,c['attachStartSpreadMilliseconds'])and math.isclose(overlap,c['nominalRequestedWindowOverlapMilliseconds'])
