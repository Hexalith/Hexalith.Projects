#!/usr/bin/env python3
"""PREPARATION ONLY until Root releases exact source/runtime pins. No builds or app lifecycle.
Future scratch controller uses fresh genuine Default entries; only its own exact child may be signaled.
"""
from pathlib import Path
import argparse, collections, hashlib, json, os, re, resource, selectors, shutil, signal, stat, subprocess, time
import xml.etree.ElementTree as ET

PREP=Path('/tmp/story65-domain-receiver-phase-prep-20261108')
ROOT=Path('/home/administrator/projects/hexalith/projects')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
DOTNET='/home/administrator/.dotnet/dotnet'
API_SHA='6a82c8705726aece454651a8e21d846f6abb2d68c02fc1a7ff7eed10335515c0'
GROUPS={
 'returns':['custom-no-lease-nonresult','custom-owned-nonresult','concrete-delayed-result','terminal-refusal-owned-result'],
 'faults':['concrete-result-fault','concrete-result-cancel','concrete-release-override','result-release-cancel-override'],
 'guard-next':['filter-next-fault','filter-next-cancel','guard-acquire-fault','guard-validation-fault'],
 'nested':['nested-two-results-success','nested-inner-result-fault'],
 'double-success':['filter-double-dispose-retry-success'],
 'double-fault':['filter-double-dispose-second-fault'],
}
EXPECTED={'custom-no-lease-nonresult': {'name': 'custom-no-lease-nonresult', 'entries': 1, 'authorityAcquireCalls': 0, 'nextCalls': 1, 'releaseCallbacks': 0, 'injectedFaults': 0, 'maskPerCapture': 147, 'errorType': None}, 'custom-owned-nonresult': {'name': 'custom-owned-nonresult', 'entries': 1, 'authorityAcquireCalls': 0, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 0, 'maskPerCapture': 179, 'errorType': None}, 'concrete-delayed-result': {'name': 'concrete-delayed-result', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 0, 'maskPerCapture': 255, 'errorType': None}, 'concrete-result-fault': {'name': 'concrete-result-fault', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 1, 'maskPerCapture': 255, 'errorType': 'System.InvalidOperationException'}, 'concrete-result-cancel': {'name': 'concrete-result-cancel', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 1, 'maskPerCapture': 255, 'errorType': 'System.OperationCanceledException'}, 'concrete-release-override': {'name': 'concrete-release-override', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 2, 'maskPerCapture': 255, 'errorType': 'System.IO.IOException'}, 'filter-next-fault': {'name': 'filter-next-fault', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 1, 'maskPerCapture': 191, 'errorType': 'System.InvalidOperationException'}, 'filter-next-cancel': {'name': 'filter-next-cancel', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 1, 'maskPerCapture': 191, 'errorType': 'System.OperationCanceledException'}, 'guard-acquire-fault': {'name': 'guard-acquire-fault', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 0, 'releaseCallbacks': 0, 'injectedFaults': 1, 'maskPerCapture': 135, 'errorType': None}, 'guard-validation-fault': {'name': 'guard-validation-fault', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 0, 'releaseCallbacks': 1, 'injectedFaults': 0, 'maskPerCapture': 175, 'errorType': None}, 'terminal-refusal-owned-result': {'name': 'terminal-refusal-owned-result', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 0, 'releaseCallbacks': 1, 'injectedFaults': 0, 'maskPerCapture': 255, 'errorType': None}, 'filter-double-dispose-retry-success': {'name': 'filter-double-dispose-retry-success', 'entries': 1, 'authorityAcquireCalls': 0, 'nextCalls': 1, 'releaseCallbacks': 2, 'injectedFaults': 1, 'maskPerCapture': 179, 'errorType': 'Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogUnavailableException'}, 'filter-double-dispose-second-fault': {'name': 'filter-double-dispose-second-fault', 'entries': 1, 'authorityAcquireCalls': 0, 'nextCalls': 1, 'releaseCallbacks': 2, 'injectedFaults': 2, 'maskPerCapture': 179, 'errorType': 'System.IO.IOException'}, 'nested-two-results-success': {'name': 'nested-two-results-success', 'entries': 2, 'authorityAcquireCalls': 2, 'nextCalls': 2, 'releaseCallbacks': 2, 'injectedFaults': 0, 'maskPerCapture': 255, 'errorType': None}, 'nested-inner-result-fault': {'name': 'nested-inner-result-fault', 'entries': 2, 'authorityAcquireCalls': 2, 'nextCalls': 2, 'releaseCallbacks': 2, 'injectedFaults': 1, 'maskPerCapture': 255, 'errorType': 'System.InvalidOperationException'}, 'result-release-cancel-override': {'name': 'result-release-cancel-override', 'entries': 1, 'authorityAcquireCalls': 1, 'nextCalls': 1, 'releaseCallbacks': 1, 'injectedFaults': 2, 'maskPerCapture': 255, 'errorType': 'System.OperationCanceledException'}}
STAGES=['Whole','ComponentFile','ComponentPolicy','AclFile','SettingsEnumerationSortDictionary','AuthenticationSerialization','ActorBoundary','ResultTail']
ZERO={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
FIELDS=ZERO|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
CLOSED_FIELDS={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
MODES=['disabled','active','pre-capped','failing-sink']
NATIVE={'DevelopmentDomainRouteCatalogTests':14,'DomainSidecarCatalogLeaseTests':12,'CatalogReceiverScopeTests':41,'CatalogReceiverStateBoundaryTests':16,'CatalogReceiverVersionTests':7,'TechnicalHttpDomainPeerTests':12,'HeldMetadataDispatchTests.RealDomainFilterAndResultComposeWithSdkMetadataClient':1}
SEMANTIC_FIELDS={'Case','Passed','FilterEntries','AuthorityAcquireCalls','NextCalls','ReleaseCallbacks','ErrorType','Faults','TokensMatched','LifetimeAndHandoffMatched','CallerRestored','ExpectedStageMask','DoubleDisposeOrder','ReleaseBoundaryRestored','ExactContextReachedNext','FirstDependencyProbeObserved','EntrySuppressionObserved'}
RESULT_FIELDS=SEMANTIC_FIELDS|{'MinimumLastDisposeTicks','MaximumLastDisposeTicks','FirstDisposeHoldTicks'}


def load(raw):
 def pairs(items):
  d={}
  for k,v in items:
   if k in d: raise ValueError('DuplicateClosedField')
   d[k]=v
  return d
 return json.loads(raw,object_pairs_hook=pairs)
def digest(data):return hashlib.sha256(data).hexdigest()
def sha(path):
 fd=os.open(path,os.O_RDONLY|os.O_NOFOLLOW|os.O_CLOEXEC|os.O_NONBLOCK)
 try:
  assert stat.S_ISREG(os.fstat(fd).st_mode)
  with os.fdopen(fd,'rb',closefd=False)as f:return hashlib.file_digest(f,'sha256').hexdigest()
 finally:os.close(fd)
def save(path,value):
 path=Path(path);assert not path.exists();path.parent.mkdir(mode=0o700,parents=True,exist_ok=True)
 fd=os.open(path,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600)
 with os.fdopen(fd,'w')as f:json.dump(value,f,indent=2);f.write('\n')
def identity(pid):
 try:
  f=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()
  return {'pid':pid,'startTicks':f[19],'group':int(f[2]),'zombie':f[0]=='Z'}
 except FileNotFoundError:return None
def group_members(group):
 found=[]
 for p in Path('/proc').iterdir():
  if not p.name.isdigit():continue
  try:
   d=identity(int(p.name))
   if d and d['group']==group:found.append(d)
  except(PermissionError,ProcessLookupError):pass
 return found

def check_private(fd):
 s=os.fstat(fd)
 assert stat.S_ISDIR(s.st_mode)and(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and os.geteuid()==1000

def write_private(fd,name,data):
 f=os.open(name,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600,dir_fd=fd)
 try:
  s=os.fstat(f);assert stat.S_ISREG(s.st_mode)and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600
  with os.fdopen(f,'wb',closefd=False)as stream:stream.write(data);stream.flush()
 finally:os.close(f)
def publish_control(fd,data):
 # Helper112 rejects visible zero-size control files. Publish only complete validated bytes.
 check_private(fd);assert 'window.control'not in os.listdir(fd)
 assert re.fullmatch(rb'[0-9]+\n[0-9]+\n[0-9]+\n[0-9]+\n',data)and 0<len(data)<=128
 temporary='.window.control.prepared';assert temporary not in os.listdir(fd)
 token=None
 try:
  write_private(fd,temporary,data)
  observed,token=read_private(fd,temporary,128);assert observed==data
  # linkat-style publication is atomic and fails EEXIST; it never overwrites another control.
  os.link(temporary,'window.control',src_dir_fd=fd,dst_dir_fd=fd,follow_symlinks=False)
  actual=os.stat('window.control',dir_fd=fd,follow_symlinks=False)
  assert(actual.st_dev,actual.st_ino,actual.st_size)==token
 finally:
  if token is not None:
   actual=os.stat(temporary,dir_fd=fd,follow_symlinks=False)
   assert(actual.st_dev,actual.st_ino,actual.st_size)==token
   os.unlink(temporary,dir_fd=fd)
 check_private(fd)
 assert os.stat('window.control',dir_fd=fd,follow_symlinks=False).st_nlink==1

def read_private(fd,name,maximum):
 f=os.open(name,os.O_RDONLY|os.O_NOFOLLOW|os.O_NONBLOCK|os.O_CLOEXEC,dir_fd=fd)
 try:
  s=os.fstat(f);assert stat.S_ISREG(s.st_mode)and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600 and s.st_size<=maximum
  chunks=[];size=0
  while True:
   b=os.read(f,min(65536,maximum+1-size))
   if not b:break
   chunks.append(b);size+=len(b);assert size<=maximum
  return b''.join(chunks),(s.st_dev,s.st_ino,s.st_size)
 finally:os.close(f)

def preparation_inputs(expected):
 p=PREP/'preparation-receipt.json';assert sha(p)==expected
 d=load(p.read_bytes());inputs={str(p):expected}
 source=d.get('sourceSha256',d.get('sourceControlsSha256'));assert isinstance(source,dict)and len(source)==59
 for name,h in d['filesSha256'].items():
  f=PREP/name;assert f.resolve().is_relative_to(PREP)and sha(f)==h;inputs[str(f)]=h
 for field in ['currentExternalInputsSha256','sourceSha256','sourceControlsSha256','app147Sha256']:
  for name,h in d.get(field,{}).items():
   f=Path(name);f=f if f.is_absolute()else ROOT/f
   assert (f.resolve().is_relative_to(ROOT) if field!='currentExternalInputsSha256' or not Path(name).is_absolute() else True)
   assert sha(f)==h;inputs[str(f)]=h
 assert sha(PREP/'fixture-api.json')==API_SHA
 return inputs

def stage_runtime(manifest,path,result):
 assert manifest['schema']==1 and manifest['fixtureApiSha256']==API_SHA
 assert manifest['frameworkRoots']==['/home/administrator/.dotnet/shared/Microsoft.NETCore.App/10.0.14','/home/administrator/.dotnet/shared/Microsoft.AspNetCore.App/10.0.14']
 assert set(manifest['entries'])=={'projects','tenants','folders','excluded'}and set(manifest['domainService'])=={'ordinary','private'}
 assert manifest['domainService']['private']['dll']['sha256']!=manifest['domainService']['ordinary']['dll']['sha256']
 assert all(manifest['domainService']['ordinary'][kind]['path']!=manifest['domainService']['private'][kind]['path']for kind in ['dll','pdb'])
 inputs={str(path):sha(path)}
 common=manifest['commonFiles'];assert all(Path(n).name==n for n in common)
 assert 'Hexalith.EventStore.DomainService.dll'not in common and 'Hexalith.EventStore.DomainService.pdb'not in common
 assert common['Hexalith.EventStore.ServiceDefaults.dll']['sha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
 assert common['Hexalith.EventStore.ServiceDefaults.pdb']['sha256']=='ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
 assert all(n in common for n in ['Story65.DomainReceiverPhase.Driver.dll','Hexalith.EventStore.Server.LiveSidecar.Tests.dll','Hexalith.EventStore.Server.Tests.dll','Hexalith.EventStore.Server.Tests.deps.json','Hexalith.EventStore.Server.Tests.runtimeconfig.json'])
 staged={}
 for role,entry in manifest['entries'].items():
  staged[role]={}
  assert entry['assemblyName']=={'projects':'Hexalith.Projects.Server','tenants':'Hexalith.Tenants','folders':'Hexalith.Folders.Server','excluded':'Story65.DomainReceiverPhase.Excluded'}[role]
  assert len(entry['files'])==4 and set(entry['files'])=={entry['assemblyName']+s for s in ['.dll','.pdb','.deps.json','.runtimeconfig.json']}
  variant_maps={}
  for variant,domain in manifest['domainService'].items():
   directory=result/'runtime'/role/variant;directory.mkdir(mode=0o700,parents=True,exist_ok=False)
   rows=dict(common)
   for n,row in entry['files'].items():assert n not in rows;rows[n]=row
   for n,row in manifest['resources'].items():assert n not in rows;rows[n]=row
   assert set(manifest['resources'])=={'Hexalith.EventStore.slnx','deploy/dapr/eventstore-routing-catalog.json'}
   rows['Hexalith.EventStore.DomainService.dll']=domain['dll'];rows['Hexalith.EventStore.DomainService.pdb']=domain['pdb']
   hashes={}
   for name,row in rows.items():
    relative=Path(name);assert not relative.is_absolute()and'..'not in relative.parts and set(row)=={'path','sha256'}
    source=Path(row['path']);assert source.is_absolute()and sha(source)==row['sha256'];inputs[str(source)]=row['sha256']
    target=directory/relative;target.parent.mkdir(mode=0o700,parents=True,exist_ok=True);assert not target.exists()
    shutil.copyfile(source,target);target.chmod(0o600);assert sha(target)==row['sha256'];hashes[name]=row['sha256']
   binding={'schema':1,'variant':variant,'role':role,'entryAssemblyName':entry['assemblyName'],'domainServiceSha256':domain['dll']['sha256'],'sdkSha256':common['Hexalith.EventStore.ServiceDefaults.dll']['sha256'],
    'filesSha256':hashes,'frameworkRoots':manifest['frameworkRoots']}
   file=directory/'runtime-binding.json';save(file,binding)
   staged[role][variant]={'directory':directory,'entry':directory/(entry['assemblyName']+'.dll'),'binding':file,'bindingSha256':sha(file),'files':hashes}
   variant_maps[variant]=hashes
  assert {n for n in variant_maps['ordinary']if variant_maps['ordinary'][n]!=variant_maps['private'][n]}=={'Hexalith.EventStore.DomainService.dll','Hexalith.EventStore.DomainService.pdb'}
 return staged,inputs

class Lines:
 def __init__(self,p,deadline):
  self.proc=p;self.deadline=deadline;self.buf=b'';self.selector=selectors.DefaultSelector();self.selector.register(p.stdout,selectors.EVENT_READ)
 def next(self):
  while time.monotonic()<self.deadline:
   if b'\n'in self.buf:
    line,self.buf=self.buf.split(b'\n',1);assert len(line)<=512;return line.decode('ascii').strip()
   try:
    fields=dict(x.split(':',1)for x in Path('/proc',str(self.proc.pid),'status').read_text().splitlines()if':'in x)
    assert int(fields.get('VmRSS','0 kB').split()[0])*1024<=1024**3
   except FileNotFoundError:pass
   if self.selector.select(.1):
    b=os.read(self.proc.stdout.fileno(),1024)
    if not b:raise RuntimeError('OwnedChildClosedBeforeProtocol')
    self.buf+=b;assert len(self.buf)<=4096
  raise TimeoutError('OwnedChildWallBound')
 def close(self):self.selector.close()

def validate_result(row):
 assert set(row)==RESULT_FIELDS and row['Passed']is True
 assert row['Case']in sum(GROUPS.values(),[])
 expected=EXPECTED[row['Case']]
 assert(row['FilterEntries'],row['AuthorityAcquireCalls'],row['NextCalls'],row['ReleaseCallbacks'],row['Faults'],row['ExpectedStageMask'],row['ErrorType'])==(expected['entries'],expected['authorityAcquireCalls'],expected['nextCalls'],expected['releaseCallbacks'],expected['injectedFaults'],expected['maskPerCapture'],expected['errorType'])
 for k in ['FilterEntries','AuthorityAcquireCalls','NextCalls','ReleaseCallbacks','Faults','ExpectedStageMask','MinimumLastDisposeTicks','MaximumLastDisposeTicks','FirstDisposeHoldTicks']:assert type(row[k])is int and row[k]>=0
 for k in SEMANTIC_FIELDS-{'Case','ErrorType','FilterEntries','AuthorityAcquireCalls','NextCalls','ReleaseCallbacks','Faults','ExpectedStageMask'}:assert type(row[k])is bool
 assert row['TokensMatched']and row['LifetimeAndHandoffMatched']and row['CallerRestored']and row['ReleaseBoundaryRestored']and row['ExactContextReachedNext']and 0<row['ExpectedStageMask']<256 and row['ExpectedStageMask']&1
 if row['Case'].startswith('filter-double-dispose-'):
  assert row['ReleaseCallbacks']==2 and row['DoubleDisposeOrder']and 0<row['MinimumLastDisposeTicks']<=row['MaximumLastDisposeTicks']<row['FirstDisposeHoldTicks']

def archive_rows(fd,directory,role,run,frequency,start,mode,business,recording):
 check_private(fd);names=set(os.listdir(fd));allowed={'window.control',role+'.jsonl',role+'.closed.json'}
 if not recording:
  if mode=='excluded':
   assert names=={'window.control'}
   raw,token=read_private(fd,'window.control',256);assert list(map(int,raw.splitlines()))==[run,frequency,start,start+8*frequency]
   artifact={'window.control':{'sha256':digest(raw),'bytes':len(raw),'contentsArchived':False}}
   s=os.stat('window.control',dir_fd=fd,follow_symlinks=False);assert(s.st_dev,s.st_ino,s.st_size)==token;os.unlink('window.control',dir_fd=fd)
   return artifact,{},None
  assert not names;return {},{},None
 assert names==allowed
 raw,closed_token=read_private(fd,role+'.closed.json',4096);closed=load(raw)
 assert set(closed)==CLOSED_FIELDS and closed['role']==role and all(type(v)is int and v>=0 for k,v in closed.items()if k!='role')
 assert closed['diagnosticOnly']==1 and closed['featureAcceptance']==0 and closed['runOrdinal']==run
 assert closed['admitted']==closed['disposedOrUnwound']and closed['activeAtDrainEnd']==0 and closed['countersFinal']==1
 assert all(closed[k]==0 for k in ['incompleteHardDrain','queuedAtDrainEnd','lateDisposedOrDropCountersMayIncrease'])
 assert(closed['maximumRows'],closed['maximumBytes'],closed['queueCapacity'])==(65536,8388608,4096)
 assert closed['frequency']==frequency and closed['windowStartTicks']==start and closed['windowEndTicks']==start+8*frequency
 raws={};tokens={}
 for n in names:raws[n],tokens[n]=read_private(fd,n,8388608 if n.endswith('.jsonl')else 4096)
 assert re.fullmatch(rb'[0-9]+\n[0-9]+\n[0-9]+\n[0-9]+\n',raws['window.control'])
 assert list(map(int,raws['window.control'].splitlines()))==[run,frequency,start,start+8*frequency]
 by_capture=collections.defaultdict(dict);counts=collections.Counter()
 for line in raws[role+'.jsonl'].splitlines():
  r=load(line);assert set(r)==FIELDS and r['role']==role and r['stage']in STAGES
  assert all(type(r[k])is int and r[k]>=0 for k in FIELDS-{'role','stage'})and all(r[k]==0 for k in ZERO)
  assert r['runOrdinal']==run and r['captureOrdinal']>0 and r['frequency']==frequency and r['elapsedTicks']>=0
  assert r['stage']not in by_capture[r['captureOrdinal']];by_capture[r['captureOrdinal']][r['stage']]=r['elapsedTicks'];counts[r['stage']]+=1
 assert sum(counts.values())==closed['writtenRows']and len(raws[role+'.jsonl'])==closed['writtenBytes']
 if mode=='pre-capped':assert closed['capRowsDropped']>0 and closed['ioFailures']==0
 elif mode=='failing-sink':assert closed['ioFailures']>0 and closed['writtenRows']==0
 else:
  assert all(closed[k]==0 for k in ['capRowsDropped','ioFailures','queuedRowsDropped','closedRowsDropped'])
  assert len(by_capture)==closed['admitted']and set(by_capture)==set(range(1,closed['admitted']+1))
  for stages in by_capture.values():
   assert 'Whole'in stages and all(elapsed<=stages['Whole']for elapsed in stages.values())
 if mode=='active':
  for observed in business['Cases']:
   r=observed['Result'];expected={STAGES[i]for i in range(8)if r['ExpectedStageMask']&(1<<i)}
   assert len(observed['CaptureOrdinals'])==r['FilterEntries']
   for ordinal in observed['CaptureOrdinals']:
    actual=by_capture[ordinal];assert set(actual)==expected
    if observed['SyntheticDurationBracketOnly']:
     last=actual[STAGES[5]];assert r['MinimumLastDisposeTicks']<=last<=r['MaximumLastDisposeTicks']<r['FirstDisposeHoldTicks']
 if mode in ['canary-current','canary-suppressed']:
  assert set(by_capture[business['CaptureOrdinal']])=={STAGES[i]for i in range(8)if business['ExpectedStages']&(1<<i)}
 artifacts={}
 for n,b in raws.items():
  artifacts[n]={'sha256':digest(b),'bytes':len(b),'contentsArchived':n!='window.control'}
  if n!='window.control':
   f=directory/n;f.write_bytes(b);f.chmod(0o600);assert sha(f)==digest(b)
 # Only fixed validated files, after exact child/group closure and hash-verified archive.
 for n in sorted(names):
  s=os.stat(n,dir_fd=fd,follow_symlinks=False);assert(s.st_dev,s.st_ino,s.st_size)==tokens[n];os.unlink(n,dir_fd=fd)
 assert not os.listdir(fd);return artifacts,dict(counts),closed

def run_one(stage,mode,group,work,run,reference):
 work.mkdir(mode=0o700,parents=True,exist_ok=False)
 private=mode!='native';recording=stage['variant']=='private'and mode not in ['disabled','canary-positive-control','excluded','native']
 role=stage['role'];output_role='folders'if role=='excluded'else role
 result=work/'business.json';cmd=[DOTNET,str(stage['entry']),mode,group,str(stage['binding']),stage['bindingSha256'],str(result)]
 if mode=='native':cmd.append(str(work/'native.xml'))
 receipt={'started':False,'passed':False,'mode':mode,'group':group,'variant':stage['variant'],'role':role,'signalsSent':False,'featureAcceptance':False,'rawBusinessValuesPersisted':False}
 proc=reader=None;pidfd=fd=None;frequency=start=0;began=time.monotonic()
 try:
  fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC);check_private(fd);assert not os.listdir(fd)
  if recording and mode=='failing-sink':write_private(fd,output_role+'.jsonl',b'')
  env=os.environ.copy()
  for k in list(env):
   if k in ['DOTNET_PROCESSOR_COUNT','DOTNET_ADDITIONAL_DEPS','DOTNET_SHARED_STORE','CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','CustomBeforeMicrosoftCommonTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration']:env.pop(k)
  env.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
  if mode=='native':assert env.get('XUNIT_PRINT_MAX_STRING_LENGTH')is None
  assert len(list(Path('/proc/self/task').iterdir()))==1
  def limits():resource.setrlimit(resource.RLIMIT_CPU,(45,46))
  proc=subprocess.Popen(cmd,cwd=stage['directory'],env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,start_new_session=True,preexec_fn=limits)
  receipt.update(started=True,command=cmd,pid=proc.pid,startTicks=None,groupId=proc.pid,processGroupAbsent=False)
  save(work/'started.json',receipt)
  pidfd=os.pidfd_open(proc.pid);actual=identity(proc.pid);assert actual and actual['group']==proc.pid;receipt['startTicks']=actual['startTicks']
  reader=Lines(proc,began+(120 if mode=='native'else 60))
  first=reader.next()
  if mode=='native':assert first=='NATIVE_DONE'
  else:
   parts=first.split(':');assert len(parts)==3 and parts[0]=='READY';frequency,start=map(int,parts[1:]);assert frequency>0 and start>0
   if recording or mode=='excluded':publish_control(fd,f'{run}\n{frequency}\n{start}\n{start+8*frequency}\n'.encode())
   proc.stdin.write(b'GO\n');proc.stdin.flush();assert reader.next()=='CASES_DONE'
   if recording:
    until=time.monotonic()+8
    while time.monotonic()<until:
     try:
      raw,_=read_private(fd,output_role+'.closed.json',4096);closed_ack=load(raw)
      assert set(closed_ack)==CLOSED_FIELDS and closed_ack['countersFinal']==1 and closed_ack['activeAtDrainEnd']==0
      break
     except(FileNotFoundError,json.JSONDecodeError):time.sleep(.01)
    else:raise TimeoutError('NoCompleteClosedReceiptBeforeExit')
   proc.stdin.write(b'EXIT\n');proc.stdin.flush()
  code=proc.wait(timeout=10);assert code==0
  until=time.monotonic()+10
  while group_members(proc.pid)and time.monotonic()<until:time.sleep(.05)
  assert not group_members(proc.pid)
  receipt.update(exitCode=code,processGroupAbsent=True)
  business=load(result.read_bytes());assert business['Passed']is True
  if mode=='native':
   assert business['ExpectedCases']==NATIVE[group]and business['NativeAssertionsUnchanged']is True
   tree=ET.parse(work/'native.xml');tests=tree.getroot().findall('.//test');assert len(tests)==NATIVE[group]and all(t.attrib['result']=='Pass'for t in tests)
   actual_names=collections.Counter((digest(t.attrib['name'].encode()),t.attrib['type'],t.attrib['method'])for t in tests)
   selection=next(r for r in reference['selections']if r['filter']=='Hexalith.EventStore.Server.Tests.Routing.'+group)
   assert selection['expectedExecutions']==NATIVE[group]
   expected_names=collections.Counter({(r['parsedXmlNameSha256'],r['type'],r['method']):r['multiplicity']for r in selection['expectedDisplayMultiset']})
   assemblies=tree.getroot().findall('assembly')
   assert actual_names==expected_names and not tree.getroot().findall('.//errors/error')and len(assemblies)==1
   assert all(int(assemblies[0].get(k,'0'))==0 for k in ['errors','failed','skipped','not-run'])
   receipt['nativeCases']=len(tests);receipt['nativeXmlSha256']=sha(work/'native.xml')
  elif mode in MODES:
   assert business['Mode']==mode and business['Group']==group and len(business['Cases'])==len(GROUPS[group])
   for observed,name in zip(business['Cases'],GROUPS[group]):
    assert observed['Result']['Case']==name;validate_result(observed['Result'])
    assert set(observed)=={'Result','CaptureOrdinals','AdmittedDelta','ActualStageRowsRequired','HandoffCounterObservationRequired','HandoffChecks','SyntheticDurationBracketOnly'}
    assert observed['AdmittedDelta']==(observed['Result']['FilterEntries']if recording else 0)
    if recording:
     assert len(observed['CaptureOrdinals'])==observed['Result']['FilterEntries']
     assert observed['CaptureOrdinals']==list(range(observed['CaptureOrdinals'][0],observed['CaptureOrdinals'][0]+observed['Result']['FilterEntries']))
    else:assert observed['CaptureOrdinals']==[]
    deferred=observed['Result']['Case']in['concrete-delayed-result','terminal-refusal-owned-result','concrete-result-fault','concrete-result-cancel','concrete-release-override','result-release-cancel-override','nested-two-results-success','nested-inner-result-fault']
    assert observed['HandoffCounterObservationRequired']==(recording and deferred)and observed['HandoffChecks']==(1 if recording and deferred else 0)
    assert observed['ActualStageRowsRequired']==(recording and mode=='active')
  elif mode=='excluded':assert business['HelperInspected']is False and business['Admitted']is None and business['LivedTicks']>=12*business['Frequency']
  else:assert business['CallerPreserved']is True and business['CanaryValuesPersisted']is False
  artifacts,stage_counts,closed=archive_rows(fd,work,output_role,run,frequency,start,mode,business,recording)
  receipt.update(passed=True,artifacts=artifacts,stageCounts=stage_counts,closed=closed,businessSha256=sha(result),wallSeconds=time.monotonic()-began,directoryReleased=True)
  return business,receipt
 finally:
  if reader is not None:reader.close()
  if proc is not None:
   if proc.poll()is None:
    # Pidfd addresses only this exact owned process; never an app/topology PID or broad group.
    if pidfd is None:pidfd=os.pidfd_open(proc.pid)
    signal.pidfd_send_signal(pidfd,signal.SIGTERM);receipt['signalsSent']=True
    try:proc.wait(timeout=3)
    except subprocess.TimeoutExpired:signal.pidfd_send_signal(pidfd,signal.SIGKILL);proc.wait(timeout=5)
   receipt['remainingGroup']=group_members(proc.pid);receipt['processGroupAbsent']=not receipt['remainingGroup']
   receipt['exitCode']=proc.returncode
  if pidfd is not None:os.close(pidfd)
  if fd is not None:os.close(fd)
  save(work/'controller-receipt.json',receipt)

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--runtime-manifest',required=True);parser.add_argument('--runtime-manifest-sha256',required=True);parser.add_argument('--preparation-sha256',required=True);parser.add_argument('--native-reference',required=True);parser.add_argument('--native-reference-sha256',required=True);parser.add_argument('--result-root',required=True)
 args=parser.parse_args();os.umask(0o077)
 path=Path(args.runtime_manifest);assert path.is_absolute()and sha(path)==args.runtime_manifest_sha256
 reference_path=Path(args.native_reference);assert sha(reference_path)==args.native_reference_sha256;reference=load(reference_path.read_bytes())
 assert reference['expectedTotal']==sum(NATIVE.values())==103 and reference['distinctRenderedIdentities']==103
 assert {r['filter'].removeprefix('Hexalith.EventStore.Server.Tests.Routing.'):r['expectedExecutions']for r in reference['selections']}==NATIVE
 result=Path(args.result_root);assert result.is_absolute()and result.is_relative_to('/tmp')and result.name.startswith('story65-domain-receiver-phase-')and not result.exists();result.mkdir(mode=0o700,parents=True)
 before=preparation_inputs(args.preparation_sha256);manifest=load(path.read_bytes());assert manifest['sourcePreparationSha256']==args.preparation_sha256
 staged,extra=stage_runtime(manifest,path,result);before.update(extra)
 for role,variants in staged.items():
  for variant,stage in variants.items():stage['role']=role;stage['variant']=variant
 save(result/'inputs-before.json',before);records=[];ordinal=22001;summary={'passed':False,'focusedPairs':0,'nativePassesPerVariant':{},'additionalRolePositiveCases':8,'firstBusinessCanaries':3,'excludedCases':1,'noNativeCountInflation':True,'rawRequestOrTraceValuesPersisted':False}
 def one(role,variant,mode,group):
  nonlocal ordinal
  ordinal+=1;work=result/'processes'/f'{ordinal}-{role}-{variant}-{mode}-{group}'
  row={'receipt':str(work/'controller-receipt.json'),'passed':False};records.append(row)
  try:
   b,r=run_one(staged[role][variant],mode,group,work,ordinal,reference);return b
  finally:
   if (work/'controller-receipt.json').exists():
    row.update(sha256=sha(work/'controller-receipt.json'),passed=load((work/'controller-receipt.json').read_bytes())['passed'])
 try:
  # Fresh first-business canaries run before any other helper activity in their own process.
  for mode in ['canary-positive-control','canary-current','canary-suppressed']:one('folders','private',mode,'concrete-delayed-result')
  for role in ['projects','tenants']:one(role,'private','active','returns')
  one('excluded','private','excluded','concrete-delayed-result')
  for variant in ['ordinary','private']:
   for selection in NATIVE:one('folders',variant,'native',selection)
   summary['nativePassesPerVariant'][variant]=103
  for mode in MODES:
   for group in GROUPS:
    original=one('folders','ordinary',mode,group);instrumented=one('folders','private',mode,group)
    for a,b in zip(original['Cases'],instrumented['Cases']):
     assert {k:a['Result'][k]for k in SEMANTIC_FIELDS}=={k:b['Result'][k]for k in SEMANTIC_FIELDS}
     summary['focusedPairs']+=1
  assert summary['focusedPairs']==64 and len(records)==68
  summary.update(passed=True,processes=len(records))
 finally:
  after=preparation_inputs(args.preparation_sha256);after.update({p:sha(p)for p in extra});assert after==before
  save(result/'inputs-after.json',after);summary['sourceAndRuntimeInputsUnchanged']=True
  summary['receipts']=records;save(result/'qualification-summary.json',summary)
 print(json.dumps({'passed':True,'focusedPairs':64,'nativePerVariant':103,'processes':len(records)}))

if __name__=='__main__':main()
