#!/usr/bin/env python3
"""Scratch-only fresh-process controller. No build or application lifecycle operation."""
from pathlib import Path
import argparse, collections, hashlib, json, os, re, selectors, stat, subprocess, time

PREP=Path('/tmp/story65-delegated-client-prep-20261107')
ROOT=Path('/home/administrator/projects/hexalith/projects')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
DLL=PREP/'bin/Debug/net10.0/Hexalith.EventStore.DomainService.Tests.dll'
DOTNET='/home/administrator/.dotnet/dotnet'
NAMES={'window.control','projects.jsonl','projects.closed.json'}
ZERO={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'}
FIELDS=ZERO|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'}
STAGES={'Whole','ComponentFile','ComponentPolicy','AclFile','SettingsEnumerationSortDictionary'}
CLOSED_FIELDS={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
MODES=['disabled','active','capped','failing-sink']
CANARIES=['canary-positive-control','canary-current','canary-suppressed']


def sha(data):return hashlib.sha256(data).hexdigest()
def load(raw):
 def pairs(items):
  d={}
  for k,v in items:
   if k in d:raise ValueError('DuplicateClosedField')
   d[k]=v
  return d
 return json.loads(raw,object_pairs_hook=pairs)
def save(path,value):
 path=Path(path);path.parent.mkdir(mode=0o700,parents=True,exist_ok=True)
 temporary=path.with_suffix(path.suffix+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');temporary.chmod(0o600);temporary.replace(path)
def check_dir(fd):
 s=os.fstat(fd)
 assert stat.S_ISDIR(s.st_mode)and(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and os.geteuid()==1000

def write_new(fd,name,data):
 out=os.open(name,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600,dir_fd=fd)
 try:
  s=os.fstat(out);assert stat.S_ISREG(s.st_mode)and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600
  with os.fdopen(out,'wb',closefd=False)as stream:stream.write(data);stream.flush()
 finally:os.close(out)
def read_owned(fd,name,maximum):
 f=os.open(name,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=fd)
 try:
  s=os.fstat(f);assert stat.S_ISREG(s.st_mode)and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600 and s.st_size<=maximum
  pieces=[];total=0
  while True:
   block=os.read(f,min(65536,maximum+1-total))
   if not block:break
   pieces.append(block);total+=len(block);assert total<=maximum
  return b''.join(pieces),(s.st_dev,s.st_ino,s.st_size)
 finally:os.close(f)
def identity(pid):
 try:
  x=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split()
  return {'pid':pid,'startTicks':x[19],'group':int(x[2]),'zombie':x[0]=='Z'}
 except FileNotFoundError:return None

def group_members(group):
 found=[]
 for p in Path('/proc').iterdir():
  if not p.name.isdigit():continue
  try:
   d=identity(int(p.name))
   if d and d['group']==group and not d['zombie']:found.append({'pid':d['pid'],'startTicks':d['startTicks']})
  except(PermissionError,ProcessLookupError):pass
 return found

def absent(pid,start):
 current=identity(pid);return current is None or current['zombie']or start is not None and current['startTicks']!=start

class Lines:
 def __init__(self,proc):self.proc=proc;self.buffer=b'';self.selector=selectors.DefaultSelector();self.selector.register(proc.stdout,selectors.EVENT_READ)
 def next(self,seconds):
  deadline=time.monotonic()+seconds
  while time.monotonic()<deadline:
   if b'\n'in self.buffer:
    line,self.buffer=self.buffer.split(b'\n',1);assert len(line)<=512;return line.decode('ascii').strip()
   if not self.selector.select(min(.1,max(0,deadline-time.monotonic()))):continue
   chunk=os.read(self.proc.stdout.fileno(),1024)
   if not chunk:raise RuntimeError('ChildExitedBeforeProtocol')
   self.buffer+=chunk;assert len(self.buffer)<=2048
  raise TimeoutError('ChildProtocolTimeout')
 def close(self):self.selector.close()

def closed_receipt(fd,ordinal,frequency,start):
 until=time.monotonic()+8
 while time.monotonic()<until:
  try:
   raw,_=read_owned(fd,'projects.closed.json',4096);d=load(raw);break
  except(FileNotFoundError,json.JSONDecodeError):time.sleep(.01)
 else:raise TimeoutError('NoCompleteClosedReceipt')
 assert set(d)==CLOSED_FIELDS and d['role']=='projects'and all(type(v)is int for k,v in d.items()if k!='role')
 assert d['diagnosticOnly']==1 and d['featureAcceptance']==0 and d['runOrdinal']==ordinal
 assert d['admitted']==d['disposedOrUnwound']and d['activeAtDrainEnd']==0
 assert d['countersFinal']==1 and all(d[k]==0 for k in ['incompleteHardDrain','queuedAtDrainEnd','lateDisposedOrDropCountersMayIncrease'])
 assert(d['maximumRows'],d['maximumBytes'],d['queueCapacity'])==(65536,8388608,4096)
 assert 0<=d['writtenRows']<=65536 and 0<=d['writtenBytes']<=8388608
 assert d['frequency']==frequency and d['windowStartTicks']==start and d['windowEndTicks']==start+8*frequency
 return d

def archive_validated(fd,result,ordinal,frequency,start,recording,mode,group):
 check_dir(fd);names=set(os.listdir(fd));assert names<=NAMES
 if not recording:assert not names;return {},0,{},True
 assert names==NAMES
 closed=closed_receipt(fd,ordinal,frequency,start);raws={};stats={}
 for n in sorted(names):raws[n],stats[n]=read_owned(fd,n,8388608 if n.endswith('.jsonl')else 4096)
 assert re.fullmatch(rb'[0-9]+\n[0-9]+\n[0-9]+\n[0-9]+\n',raws['window.control'])
 assert list(map(int,raws['window.control'].splitlines()))==[ordinal,frequency,start,start+8*frequency]
 count=0;stages=collections.Counter();seen=set()
 for line in raws['projects.jsonl'].splitlines():
  d=load(line);assert set(d)==FIELDS and d['role']=='projects'and d['stage']in STAGES
  assert all(type(d[k])is int for k in FIELDS-{'role','stage'})and all(d[k]==0 for k in ZERO)
  assert d['runOrdinal']==ordinal and d['captureOrdinal']>0 and d['frequency']==frequency and d['elapsedTicks']>=0
  pair=(d['captureOrdinal'],d['stage']);assert pair not in seen;seen.add(pair);count+=1;stages[d['stage']]+=1
 assert count==closed['writtenRows']and len(raws['projects.jsonl'])==closed['writtenBytes']
 if mode=='capped':assert closed['capRowsDropped']>0 and closed['ioFailures']==0
 elif mode=='failing-sink':assert closed['ioFailures']>0 and closed['writtenRows']==0
 else:assert all(closed[k]==0 for k in ['capRowsDropped','ioFailures','queuedRowsDropped','closedRowsDropped'])
 if mode in ['canary-current','canary-suppressed']or mode=='active'and group=='fast':assert set(stages)==STAGES
 artifacts={}
 for n,data in raws.items():
  artifacts[n]={'sha256':sha(data),'bytes':len(data),'contentsArchived':n!='window.control'}
  if n!='window.control':
   file=result/n;file.write_bytes(data);file.chmod(0o600);assert sha(file.read_bytes())==sha(data)
 # Delete only controller/helper-created fixed synthetic files after verified exit/group absence and receipt/row validation.
 for n in sorted(names):
  s=os.stat(n,dir_fd=fd,follow_symlinks=False);assert(s.st_dev,s.st_ino,s.st_size)==stats[n];os.unlink(n,dir_fd=fd)
 check_dir(fd);assert os.listdir(fd)==[]
 return artifacts,count,dict(stages),True

def validate_business(d,mode,group,expected):
 common={'Cases','Passed','ErrorType'}
 if mode in CANARIES:
  fields=common|{'PositiveControl','AlreadySuppressedBranch','DrainCanaryObserved','FakeAwaitChecks','CallerCanaryUnchanged','CallerActivityUnchanged','CallerFlowUnchanged','WorkerProgressAndClosedReceipt','ActualCanaryActivityOrHeaderValuePersisted'}
  assert type(d['Cases'])is int and type(d['Passed'])is int
  assert set(d)==fields and d['Cases']==d['Passed']==1 and d['ErrorType']=='none'
  assert all(type(d[k])is bool for k in fields-common-{'FakeAwaitChecks'})and type(d['FakeAwaitChecks'])is int
  positive=mode=='canary-positive-control';assert d['PositiveControl']==positive and d['AlreadySuppressedBranch']==(mode=='canary-suppressed')and d['DrainCanaryObserved']==positive and d['WorkerProgressAndClosedReceipt']==(not positive)
  assert d['CallerCanaryUnchanged']and d['CallerActivityUnchanged']and d['CallerFlowUnchanged']and not d['ActualCanaryActivityOrHeaderValuePersisted']
  assert d['FakeAwaitChecks']>0 if not positive else d['FakeAwaitChecks']==0
 else:
  fields=common|{'Mode','Group','Failures','InvocationComparisons','EligibleAdmittedCases','Captures','FaultCases','FaultCallbacks','TraceValuesPersisted','ProofHeaderKeyPayloadPersisted','AllocationMode','CountCalls'}
  assert set(d)==fields and d['Mode']==mode and d['Group']==group and d['ErrorType']=='none'and d['Failures']==[]
  assert all(type(d[k])is int and d[k]>=0 for k in ['Cases','Passed','InvocationComparisons','EligibleAdmittedCases','Captures','FaultCases','FaultCallbacks','CountCalls'])
  assert d['Cases']==d['Passed']==expected and d['InvocationComparisons']==(51 if group=='fast'else 1)
  assert type(d['TraceValuesPersisted'])is bool and type(d['ProofHeaderKeyPayloadPersisted'])is bool and not d['TraceValuesPersisted']and not d['ProofHeaderKeyPayloadPersisted']
  assert d['AllocationMode']=='None'and d['CountCalls']==0
  if mode=='disabled':assert d['Captures']==d['EligibleAdmittedCases']==0


def input_snapshot():
 fixture=load((PREP/'oracle-fixture-pins.json').read_bytes())['inputsSha256'];inputs={}
 for n,digest in fixture.items():assert sha(Path(n).read_bytes())==digest;inputs[n]=digest
 source=load((PREP/'source-proof.json').read_bytes())
 for n,key in [('DelegatedOwnerQueryClient.original.cs','originalSha256'),('DelegatedOwnerQueryClient.private.cs','privateSha256'),('CaptureCounters.cs','helperSha256'),('AdvisoryWaveBootstrap.cs','bootstrapSha256')]:assert sha((PREP/n).read_bytes())==source[key]
 owner=load(Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json').read_bytes())['sourceSha256'];assert len(owner)==48
 for n,digest in owner.items():f=ROOT/'references/Hexalith.EventStore'/n;assert sha(f.read_bytes())==digest;inputs[str(f)]=digest
 for rel,digest in [('references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Routing/DevelopmentRouteCatalogInputs.cs','5e85827847578384a7dcca9fe448b97f21cdf4a201263c2d9ec9d0f3eda6be4d'),('src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs','ac34b0c5c86c8a85ddf8591f73ddb02d32dc3a63db47a206b42688f2c696df8d'),('references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DelegatedOwnerQueryClient.cs',source['originalSha256'])]:assert sha((ROOT/rel).read_bytes())==digest;inputs[str(ROOT/rel)]=digest
 for folder in [PREP,PREP/'driver']:
  for file in folder.iterdir():
   if file.is_file()and file.suffix in ['.cs','.csproj','.py','.json','.Config']:inputs[str(file)]=sha(file.read_bytes())
 for suffix in ['.dll','.pdb','.deps.json','.runtimeconfig.json']:
  file=DLL.parent/('Hexalith.EventStore.DomainService.Tests'+suffix);assert file.is_file()and not file.is_symlink();inputs[str(file)]=sha(file.read_bytes())
 metadata=load((PREP/'preparation-receipt.json').read_bytes());assert len(metadata['app147Sha256'])==147
 for rel,digest in metadata['app147Sha256'].items():
  file=ROOT/rel;data=file.read_bytes();assert sha(data)==digest
  assert not any(marker in data or marker.decode().encode('utf-16-le')in data for marker in [b'Hexalith.PrivateCaptureCounter',b'TimingHexalith',b'PrivateFocusedTiming',b'Story65Timing'])
  inputs[str(file)]=sha(data)
 return inputs

def one(mode,group,case,ordinal,root,expected):
 result=root/mode/(case or group);result.mkdir(mode=0o700,parents=True,exist_ok=False)
 record={'noApplicationOrTopologySignals':True,'scratchRecoverySignalsOnlyIfExactSameStartOwnedChild':True,'scratchQualificationOnly':True,'featureAcceptance':False,'mode':mode,'group':group,'case':case,'expectedCatalogCases':expected,'nativeCasesExecuted':0,'hostsStarted':0,'started':False,'processAndGroupAbsent':True,'directoryReleased':False,'errorType':'none','rawDependencyTracePersisted':False,'controlContentsPersisted':False}
 fd=None;proc=None;reader=None;start_ticks=None;passed=False
 try:
  fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC);check_dir(fd);assert os.listdir(fd)==[]
  before=input_snapshot();record['inputsBeforeSha256']=before
  if mode=='failing-sink':write_new(fd,'projects.jsonl',b'')
  command=[DOTNET,str(DLL),mode,group,str(result/'business-parity.json')]+([case]if case else[])
  env=os.environ.copy()
  for key in list(env):
   if key in ['DOTNET_PROCESSOR_COUNT','CustomBeforeMicrosoftCommonTargets','CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration']or key.startswith('Hexalith')and key.endswith('Root'):env.pop(key)
  proc=subprocess.Popen(command,cwd=PREP,env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,start_new_session=True)
  record.update(started=True,processAndGroupAbsent=False,command=command,cwd=str(PREP),runtimeProcessorOverride=None,identity={'pid':proc.pid,'startTicks':None,'processGroup':proc.pid})
  save(result/'controller-receipt.json',record)
  actual=identity(proc.pid);assert actual is not None and actual['group']==proc.pid;start_ticks=actual['startTicks'];record['identity']['startTicks']=start_ticks;save(result/'controller-receipt.json',record)
  reader=Lines(proc);parts=reader.next(20).split(':');assert len(parts)==3 and parts[0]=='READY';frequency,start=map(int,parts[1:]);assert frequency>0 and start>0
  recording=mode not in ['disabled','canary-positive-control']
  if recording:write_new(fd,'window.control',f'{ordinal}\n{frequency}\n{start}\n{start+8*frequency}\n'.encode('ascii'))
  terminal=reader.next(70)
  if recording:
   assert terminal=='CASES_DONE';record['closed']=closed_receipt(fd,ordinal,frequency,start);proc.stdin.write(b'EXIT\n');proc.stdin.flush();terminal=reader.next(10)
  status=load(terminal);assert set(status)=={'cases','passed','errorType'}and type(status['cases'])is int and type(status['passed'])is int and status['errorType']=='none'
  assert status['cases']==status['passed']==expected;record['terminal']=status
  record['exitCode']=proc.wait(timeout=10);assert record['exitCode']==0
  deadline=time.monotonic()+10
  while group_members(proc.pid)and time.monotonic()<deadline:time.sleep(.05)
  record['processAndGroupAbsent']=absent(proc.pid,start_ticks)and not group_members(proc.pid);assert record['processAndGroupAbsent']
  after=input_snapshot();assert after==before;record['allInputsUnchanged']=True;record['inputsAfterSha256']=after
  business=result/'business-parity.json';assert business.is_file()and not business.is_symlink()and business.stat().st_size<=32768
  validate_business(load(business.read_bytes()),mode,group,expected);record['businessReceiptSha256']=sha(business.read_bytes())
  record['artifacts'],record['rows'],record['stageCounts'],record['directoryReleased']=archive_validated(fd,result,ordinal,frequency,start,recording,mode,group)
  passed=True
 except Exception as failure:
  record['errorType']=type(failure).__name__
 finally:
  if reader:reader.close()
  if proc is not None:
   # Scratch child only: never an application target or topology. Exact same-start guard precedes a bounded recovery terminate.
   if proc.poll()is None:
    record['scratchRecoveryTerminateAttempted']=False
    try:
     if start_ticks is not None and not absent(proc.pid,start_ticks):proc.terminate();record['scratchRecoveryTerminateAttempted']=True
     proc.wait(timeout=10)
    except Exception as failure:record['cleanupErrorType']=type(failure).__name__
   deadline=time.monotonic()+10
   while group_members(proc.pid)and time.monotonic()<deadline:time.sleep(.05)
   record['remainingOwnedGroupIdentities']=group_members(proc.pid);record['processAndGroupAbsent']=proc.poll()is not None and absent(proc.pid,start_ticks)and not record['remainingOwnedGroupIdentities']
  if fd is not None:
   try:check_dir(fd);names=set(os.listdir(fd));record['remainingPrivateKnownNames']=sorted(names&NAMES);record['unexpectedPrivateNameCount']=len(names-NAMES);record['directoryReleased']=record['directoryReleased']and not names
   except Exception as failure:record['directoryCheckErrorType']=type(failure).__name__;record['directoryReleased']=False
   os.close(fd)
  record['passed']=passed and record['processAndGroupAbsent']and record['directoryReleased'];save(result/'controller-receipt.json',record)
 if not record['passed']:raise RuntimeError('ClosedScratchAttemptFailed')
 return record

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');parser.add_argument('--mode',choices=['all']+MODES+CANARIES,default='all');parser.add_argument('--results',default='results-initial');args=parser.parse_args()
 plan=load((PREP/'controller-plan.json').read_bytes());entries=plan['plannedProcesses']
 if args.mode!='all':entries=[row for row in entries if row['mode']==args.mode]
 if not args.execute:print(json.dumps({'preparationOnly':True,'executed':False,'plannedProcesses':entries},indent=2));return
 assert re.fullmatch(r'[0-9a-f]{64}',args.pins_sha256 or'')
 preparation=PREP/'preparation-receipt.json';assert sha(preparation.read_bytes())==args.pins_sha256
 fixed=load(preparation.read_bytes());assert type(fixed['filesSha256'])is dict and len(fixed['sourceSha256'])==51 and len(fixed['app147Sha256'])==147
 for relative,digest in fixed['filesSha256'].items():
  name=Path(relative);assert not name.is_absolute()and'..'not in name.parts;file=PREP/name;assert file.is_file()and not file.is_symlink()and sha(file.read_bytes())==digest
 for relative,digest in fixed['sourceSha256'].items():assert sha((ROOT/relative).read_bytes())==digest
 assert re.fullmatch(r'results-[a-z0-9-]{1,64}',args.results);root=PREP/args.results;root.mkdir(mode=0o700,exist_ok=False)
 receipts=[];summary={'scratchQualificationOnly':True,'featureAcceptance':False,'allPassed':False,'plannedProcesses':len(entries),'completedProcesses':0}
 try:
  # Main sequence keeps all three fresh first-business canaries before any behavioral mode.
  for i,row in enumerate(entries,10101):
   receipt=one(row['mode'],row['group'],row['case'],i,root,row['expectedCatalogCases']);receipts.append({'path':str(root/row['mode']/(row['case']or row['group'])/'controller-receipt.json'),'passed':receipt['passed'],'mode':row['mode'],'group':row['group'],'case':row['case'],'catalogCases':row['expectedCatalogCases']})
   summary['completedProcesses']=len(receipts)
  summary['allPassed']=True
 finally:summary['receipts']=receipts;save(root/'controller-summary.json',summary)
 print(json.dumps({'scratchQualificationOnly':True,'allPassed':True,'completedProcesses':len(receipts)}))

if __name__=='__main__':main()
