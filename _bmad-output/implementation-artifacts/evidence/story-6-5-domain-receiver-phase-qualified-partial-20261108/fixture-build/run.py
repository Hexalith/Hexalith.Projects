from pathlib import Path
import datetime,hashlib,json,os,signal,subprocess,sys,time
OUT=Path(__file__).parent
PREP=Path('/tmp/story65-domain-receiver-phase-prep-20261108')
ROOT=Path('/home/administrator/projects/hexalith/projects')
PREP_SHA='d91e15a7ce3cff1e9ee2b30fb024923b2d9d281f9c8b48c1882bd4958e4f6374'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def save(name,x):(OUT/name).write_text(json.dumps(x,indent=2)+'\n')
def identity(pid):
 try:
  v=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return {'pid':pid,'state':v[0],'processGroup':int(v[2]),'startTicks':int(v[19])}
 except(FileNotFoundError,ProcessLookupError):return None
def group_members(group):
 members=[]
 for p in Path('/proc').iterdir():
  if p.name.isdecimal():
   x=identity(int(p.name))
   if x and x['processGroup']==group:members.append(x)
 return members
receipt_path=PREP/'preparation-receipt.json';assert sha(receipt_path)==PREP_SHA
prepared=json.loads(receipt_path.read_bytes());paths={str(PREP/name):h for name,h in prepared['filesSha256'].items()};paths.update(prepared['currentExternalInputsSha256']);paths[str(receipt_path)]=PREP_SHA
assert len(paths)==1570
for field in ['sourceSha256','app147Sha256']:
 for name,h in prepared[field].items():assert paths[str(ROOT/name)]==h
def snapshot():
 actual={}
 for name,h in paths.items():
  p=Path(name);assert p.is_file()and not p.is_symlink();actual[name]=sha(p);assert actual[name]==h
 return actual
before=snapshot();save('inputs-before.json',before)
env=os.environ.copy();removed={}
for name in prepared['removeInheritedEnvironment']:removed[name]=name in env;env.pop(name,None)
env.update(prepared['explicitBuildEnvironment']);assert env['DOTNET_PROCESSOR_COUNT']=='2'
save('environment.json',{'explicitBuildEnvironment':prepared['explicitBuildEnvironment'],'removedInheritedNamesPresent':removed,'cwd':prepared['cwd'],'runtimeInvoked':False})
commands=[('restore',argv)for argv in prepared['exactStandaloneRestoreCommands']]+[('build',argv)for argv in prepared['exactStandaloneBuildCommands']]
assert len(commands)==12
records=[];failure=None;started=now();child=None;pidfd=None
for index,(kind,argv)in enumerate(commands,1):
 record={'ordinal':index,'kind':kind,'command':argv,'cwd':prepared['cwd'],'startedUtc':now(),'started':False,'exitCode':None,'pid':None,'processGroup':None,'startTicks':None,'signalsSent':False}
 records.append(record);log=OUT/f'{index:02d}-{kind}.log';child=None;pidfd=None
 try:
  assert argv[1]==kind and argv[2].startswith(str(PREP))
  if kind=='build':assert '-p:BuildProjectReferences=false'in argv and '-p:UseSharedCompilation=false'in argv and '-nodeReuse:false'in argv
  with log.open('wb')as stream:
   child=subprocess.Popen(argv,cwd=prepared['cwd'],env=env,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
   # The owning Popen object and PID/group are registered before any fallible /proc inspection.
   record.update(started=True,pid=child.pid,processGroup=child.pid)
   save(f'{index:02d}-{kind}-owned.json',record)
   pidfd=os.pidfd_open(child.pid)
   actual=identity(child.pid);assert actual and actual['processGroup']==child.pid
   record['startTicks']=actual['startTicks'];save(f'{index:02d}-{kind}-owned.json',record)
   record['exitCode']=child.wait()
  record['finishedUtc']=now();record['pidAbsent']=identity(child.pid)is None
  record['remainingGroup']=group_members(child.pid);record['processGroupAbsent']=not record['remainingGroup']
  record['log']=str(log);record['logSha256']=sha(log);record['logBytes']=log.stat().st_size
  text=log.read_text()
  if kind=='build':
   record['zeroWarnings']='0 Warning(s)'in text;record['zeroErrors']='0 Error(s)'in text
  save(f'{index:02d}-{kind}-receipt.json',record)
  print(json.dumps({'ordinal':index,'kind':kind,'exitCode':record['exitCode'],'pidAbsent':record['pidAbsent'],'groupAbsent':record['processGroupAbsent'],'zeroWarnings':record.get('zeroWarnings'),'zeroErrors':record.get('zeroErrors')}),flush=True)
  if record['exitCode']!=0 or not record['pidAbsent']or not record['processGroupAbsent']or(kind=='build'and not(record['zeroWarnings']and record['zeroErrors'])):failure=f'command-{index}';break
 except BaseException as error:
  failure=f'command-{index}-setup-or-verification';record['closedErrorType']=type(error).__name__
  if child is not None and child.poll()is None:
   if pidfd is None:pidfd=os.pidfd_open(child.pid)
   signal.pidfd_send_signal(pidfd,signal.SIGTERM);record['signalsSent']=True
   try:child.wait(timeout=3)
   except subprocess.TimeoutExpired:signal.pidfd_send_signal(pidfd,signal.SIGKILL);child.wait(timeout=5)
  record['exitCode']=None if child is None else child.returncode;record['finishedUtc']=now()
  if child is not None:record['remainingGroup']=group_members(child.pid);record['processGroupAbsent']=not record['remainingGroup']
  if log.exists():record['logSha256']=sha(log);record['logBytes']=log.stat().st_size
  save(f'{index:02d}-{kind}-receipt.json',record);print(json.dumps({'ordinal':index,'stopped':True,'closedErrorType':type(error).__name__}),flush=True);break
 finally:
  if pidfd is not None:os.close(pidfd)
try:after=snapshot();unchanged=after==before;save('inputs-after.json',after)
except BaseException as error:unchanged=False;failure=failure or'input-after-mismatch';save('input-after-failure.json',{'closedErrorType':type(error).__name__})
outputs={}
for base in [PREP/'companion',PREP/'driver']+[PREP/'entry'/role for role in ['projects','tenants','folders','excluded']]:
 for kind in ['bin','obj']:
  directory=base/kind
  if directory.exists():
   for file in sorted(directory.rglob('*')):
    if file.is_file()and not file.is_symlink():outputs[str(file)]={'sha256':sha(file),'bytes':file.stat().st_size}
save('compiled-output-manifest.json',outputs)
result={'preparationReceipt':{'path':str(receipt_path),'sha256':PREP_SHA},'startedUtc':started,'finishedUtc':now(),'commandCount':len(records),'all12CommandsClosedZero':len(records)==12 and all(r['exitCode']==0 and r.get('pidAbsent')and r.get('processGroupAbsent')and not r['signalsSent']for r in records),'allSixDebugBuildsZeroWarningsAndErrors':len(records)==12 and all(r.get('zeroWarnings')and r.get('zeroErrors')for r in records if r['kind']=='build'),'all1570CurrentInputsUnchanged':unchanged,'commands':records,'compiledOutputFileCount':len(outputs),'postBuildRuntimeManifestReviewStillRequired':True,'fixtureSdkDomainAssemblyLoadsInvoked':False,'nativeKestrelCounterWindowsAppHostsInvoked':False,'failure':failure}
save('receipt.json',result);print(json.dumps({k:v for k,v in result.items()if k!='commands'}),flush=True)
sys.exit(0 if result['all12CommandsClosedZero']and result['allSixDebugBuildsZeroWarningsAndErrors']and unchanged and not failure else 1)
