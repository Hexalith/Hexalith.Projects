#!/usr/bin/env python3
"""Prepared synthetic-only controller. Refuses execution until Root separately pins compiled outputs."""
from pathlib import Path
import argparse,hashlib,json,os,selectors,stat,subprocess,time

PREP=Path(__file__).resolve().parent
ROOT=Path('/home/administrator/projects/hexalith/projects')
PRIVATE=Path('/tmp/story65-capture-counter-private-20261107')
DOTNET='/home/administrator/.dotnet/dotnet'
DLL=PREP/'differential/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll'
NAMES={'window.control','projects.jsonl','projects.closed.json'}
COUNTS=['yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded']
FIELDS={'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency','allocationMode','allocatedBytes',*COUNTS}
SLOW=['shared-deadline','index-uncooperative','filter-uncooperative','final-anchor-uncooperative','caller-uncooperative-child']
CANARIES=['canary-positive-control','canary-current','canary-suppressed']
REMOVE=['CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration','CustomBeforeMicrosoftCommonTargets','DOTNET_PROCESSOR_COUNT']

def sha(data):return hashlib.sha256(data).hexdigest()
def current_inputs(pins):
 result={}
 for mapping,base in [(pins['filesSha256'],PREP),(pins['inputsSha256'],ROOT),(pins['compiledInputsSha256'],PREP)]:
  for name,expected in mapping.items():
   p=Path(name);p=p if p.is_absolute() else base/p
   assert not p.is_symlink() and p.is_file() and all(not parent.is_symlink() for parent in p.parents)
   actual=sha(p.read_bytes());assert actual==expected
   result[str(p)]=actual
 return result

def check_dir(fd):
 s=os.fstat(fd)
 assert (s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)
 assert stat.S_ISDIR(s.st_mode) and os.geteuid()==1000

def identity(s):return(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode),s.st_size,s.st_mtime_ns)
def write_new(fd,name,data):
 out=os.open(name,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600,dir_fd=fd)
 try:
  s=os.fstat(out);assert stat.S_ISREG(s.st_mode) and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600
  with os.fdopen(out,'wb',closefd=False) as f:f.write(data);f.flush();os.fsync(f.fileno())
  return identity(os.fstat(out))
 finally:os.close(out)

def publish_control(fd,data):
 temporary='.terminal-control-publication';created=write_new(fd,temporary,data)
 try:
  raw,seen=read_owned(fd,temporary,4096);assert raw==data and seen==created and len(data)>0
  os.link(temporary,'window.control',src_dir_fd=fd,dst_dir_fd=fd,follow_symlinks=False)
  assert identity(os.stat('window.control',dir_fd=fd,follow_symlinks=False))==created
 finally:
  if identity(os.stat(temporary,dir_fd=fd,follow_symlinks=False))==created:os.unlink(temporary,dir_fd=fd)

def read_owned(fd,name,maximum):
 f=os.open(name,os.O_RDONLY|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC,dir_fd=fd)
 try:
  s=os.fstat(f);assert stat.S_ISREG(s.st_mode) and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600 and s.st_size<=maximum
  chunks=[];total=0
  while True:
   part=os.read(f,min(65536,maximum+1-total))
   if not part:break
   chunks.append(part);total+=len(part);assert total<=maximum
  assert identity(os.fstat(f))==identity(s)
  return b''.join(chunks),identity(s)
 finally:os.close(f)

def members(group):
 found=[]
 for p in Path('/proc').iterdir():
  if not p.name.isdigit():continue
  try:
   v=(p/'stat').read_text().rsplit(')',1)[1].split()
   if int(v[2])==group or int(v[3])==group:found.append({'pid':int(p.name),'startTicks':v[19]})
  except(FileNotFoundError,ProcessLookupError):pass
 return found

def ready_line(proc,deadline):
 # Pending bytes belong to this one child. Always consume a complete pending line
 # before selecting: a prior os.read may already have received the next message.
 if not hasattr(proc,'_terminal_pending'):proc._terminal_pending=bytearray()
 pending=proc._terminal_pending
 fd=proc.stdout.fileno();os.set_blocking(fd,False)
 selector=selectors.DefaultSelector();selector.register(fd,selectors.EVENT_READ)
 try:
  while True:
   end=pending.find(b'\n')
   if end>=0:
    if end+1>4096:raise RuntimeError('ClosedProtocolBoundExceeded')
    line=bytes(pending[:end+1]);del pending[:end+1]
    return line.decode('ascii').strip()
   if len(pending)>=4096:raise RuntimeError('ClosedProtocolBoundExceeded')
   remaining=deadline-time.monotonic()
   if remaining<=0:raise TimeoutError('ChildProtocolTimeout')
   if not selector.select(min(.1,remaining)):continue
   try:chunk=os.read(fd,4096-len(pending))
   except BlockingIOError:continue
   if not chunk:
    raise RuntimeError('ChildProtocolPartialLineAtEof' if pending else 'ChildExitedBeforeProtocol')
   pending.extend(chunk)
 finally:selector.close()

def wait_closed(fd):
 end=time.monotonic()+8
 while time.monotonic()<end:
  try:
   raw,seen=read_owned(fd,'projects.closed.json',4096);return json.loads(raw),raw,seen
  except(FileNotFoundError,json.JSONDecodeError):time.sleep(.01)
 raise TimeoutError('NoCompleteClosedReceipt')

def validate_rows(data,closed,ordinal,frequency,mode,business):
 groups={};rows=0;maximum=[11,11,7,2,1,1]
 for line in data.splitlines():
  pairs=json.loads(line,object_pairs_hook=lambda pairs:pairs)
  assert isinstance(pairs,list) and len(pairs)==len(dict(pairs))
  row=dict(pairs);assert set(row)==FIELDS
  assert row['role']=='projects' and type(row['stage']) is str
  assert all(type(row[k]) is int for k in FIELDS-{'role','stage'})
  assert row['runOrdinal']==ordinal and row['captureOrdinal']>0 and row['frequency']==frequency and row['elapsedTicks']>=0
  assert row['allocationMode']==row['allocatedBytes']==0
  assert row['stage'] in ('Whole','SettingsEnumerationSortDictionary')
  values=[row[k] for k in COUNTS]
  assert all(0<=n<=bound for n,bound in zip(values,maximum))
  if row['stage']=='Whole':assert values==[0]*6
  else:assert (values[1]==0)==(values[3]==0)
  group=groups.setdefault(row['captureOrdinal'],{});assert row['stage'] not in group;group[row['stage']]=row
  rows+=1
 assert rows==closed['writtenRows'] and len(data)==closed['writtenBytes']
 if mode not in ('capped','failing-sink'):
  assert set(groups)==set(range(1,closed['admitted']+1))
  for group in groups.values():
   assert set(group)=={'Whole','SettingsEnumerationSortDictionary'}
   assert group['SettingsEnumerationSortDictionary']['elapsedTicks']<=group['Whole']['elapsedTicks']
  if 'Terminals' in business:
   expected=[r['Values'] for r in business['Terminals']]
   actual=[[groups[n]['SettingsEnumerationSortDictionary'][k] for k in COUNTS] for n in sorted(groups)]
   assert actual==expected
  if 'TerminalValues' in business:
   actual=[[groups[n]['SettingsEnumerationSortDictionary'][k] for k in COUNTS] for n in sorted(groups)]
   assert actual==business['TerminalValues']
 return rows

def one(mode,group,case,ordinal,result_root,pins):
 before=current_inputs(pins)
 out=result_root/mode/(case or group);out.mkdir(parents=True,exist_ok=False)
 fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 proc=None;error='none';artifacts={};closed=None;start=None;code=None;rows=0
 try:
  check_dir(fd);assert os.listdir(fd)==[]
  if mode=='failing-sink':write_new(fd,'projects.jsonl',b'')
  command=[DOTNET,str(DLL),mode,group,str(out/'business-parity.json')]+([case] if case else [])
  env=os.environ.copy()
  for k in REMOVE:env.pop(k,None)
  for k in list(env):
   if k.startswith('Hexalith') and k.endswith('Root'):env.pop(k,None)
  proc=subprocess.Popen(command,cwd=PREP/'differential',env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,start_new_session=True,bufsize=0)
  fields=Path('/proc',str(proc.pid),'stat').read_text().rsplit(')',1)[1].split();start=fields[19]
  assert int(fields[2])==proc.pid and int(fields[3])==proc.pid
  line=ready_line(proc,time.monotonic()+20).split(':');assert len(line)==3 and line[0]=='READY'
  frequency,started=map(int,line[1:]);assert frequency>0 and started>0
  recording=mode not in ('disabled','canary-positive-control')
  if recording:publish_control(fd,f'{ordinal}\n{frequency}\n{started}\n{started+8*frequency}\n'.encode('ascii'))
  terminal=ready_line(proc,time.monotonic()+60)
  if recording:
   assert terminal=='CASES_DONE';closed,raw,_=wait_closed(fd)
   assert closed['role']=='projects' and closed['runOrdinal']==ordinal
   assert all(type(v) is int for k,v in closed.items() if k!='role')
   assert closed['admitted']==closed['disposedOrUnwound'] and closed['activeAtDrainEnd']==0
   assert closed['countersFinal']==1 and closed['incompleteHardDrain']==0 and closed['queuedAtDrainEnd']==0
   assert closed['writtenRows']<=65536 and closed['writtenBytes']<=8*1024*1024
   if mode=='capped':assert closed['capRowsDropped']>0 and closed['ioFailures']==0
   elif mode=='failing-sink':assert closed['ioFailures']>0 and closed['writtenRows']==0
   else:assert all(closed[k]==0 for k in ['capRowsDropped','ioFailures','queuedRowsDropped','closedRowsDropped'])
   proc.stdin.write(b'EXIT\n');proc.stdin.flush();terminal=ready_line(proc,time.monotonic()+10)
  status=json.loads(terminal);assert set(status)=={'cases','passed','errorType'}
  assert status['cases']==status['passed'] and status['errorType']=='none'
  code=proc.wait(timeout=10);assert code==0 and members(proc.pid)==[]
  check_dir(fd);business=json.loads((out/'business-parity.json').read_text());assert business['Cases']==business['Passed']
  owned={}
  for name in sorted(os.listdir(fd)):
   assert name in NAMES
   data,seen=read_owned(fd,name,8*1024*1024 if name.endswith('.jsonl') else 4096)
   artifacts[name]={'sha256':sha(data),'bytes':len(data)};owned[name]=seen
   if name!='window.control':(out/name).write_bytes(data)
   if name=='projects.jsonl':rows=validate_rows(data,closed,ordinal,frequency,mode,business)
  if closed:assert rows==closed['writtenRows']
  assert current_inputs(pins)==before
  for name,seen in owned.items():
   _,now=read_owned(fd,name,8*1024*1024);assert now==seen;os.unlink(name,dir_fd=fd)
  check_dir(fd);assert os.listdir(fd)==[]
 except Exception as ex:
  error=type(ex).__name__
  raise
 finally:
  # Only this directly owned synthetic child may be stopped after a controller fault.
  # Preserve fixed files on any failure, even if its known child was reaped.
  if proc is not None and proc.poll() is None:
   proc.terminate()
   try:proc.wait(timeout=10)
   except subprocess.TimeoutExpired:proc.kill();proc.wait(timeout=10)
  record={'mode':mode,'group':group,'case':case,'pid':proc.pid if proc else None,'startTicks':start,'exitCode':code,
   'ownedGroupMembersAfter':members(proc.pid) if proc else [],'errorType':error,'artifacts':artifacts,'rows':rows,'closed':closed,
   'runtimeProcessorOverride':None,'rawDependencyTracePersisted':False,'inputPaths':len(before)}
  (out/'controller-receipt.json').write_text(json.dumps(record,indent=2)+'\n');os.close(fd)
 return record

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');parser.add_argument('--result-root',type=Path)
 args=parser.parse_args();pin=PREP/'preparation-receipt.json';raw=pin.read_bytes();pins=json.loads(raw)
 assert args.execute and args.pins_sha256==sha(raw) and pins['executionReleased'] is True
 assert pins['compiledInputsSha256'] and args.result_root is not None
 args.result_root.mkdir(parents=True,exist_ok=False)
 plan=[('disabled','all',None)]+[(c,'canary',None) for c in CANARIES]
 for mode in ['active','capped','failing-sink']:plan += [(mode,'fast',None)]+[(mode,'slow',case) for case in SLOW]
 plan += [('state-protocol','protocol',None)]
 records=[]
 for n,(mode,group,case) in enumerate(plan,1):records.append(one(mode,group,case,1000+n,args.result_root,pins))
 assert len(records)==23
 (args.result_root/'qualification-summary.json').write_text(json.dumps({'controlledProcesses':23,'allClosedZero':True,'records':records,'featureAcceptance':False},indent=2)+'\n')
if __name__=='__main__':main()
