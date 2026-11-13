import pathlib,json,hashlib,os,subprocess,datetime,time,sys
OUT=pathlib.Path(__file__).parent
PREP=pathlib.Path('/tmp/story65-receiver-phase-prep-20261107')
ROOT=pathlib.Path('/home/administrator/projects/hexalith/projects')
RECEIPT=PREP/'preparation-receipt.json'
EXPECTED='83bb2d0938e32482f1a459df945e7441904e84bcd64c605d9eb223e76f2fc623'
def sha(p):
 with pathlib.Path(p).open('rb') as f:
  h=hashlib.sha256()
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def write(name,x):(OUT/name).write_text(json.dumps(x,indent=2)+'\n')
def proc(pid):
 try:
  raw=pathlib.Path(f'/proc/{pid}/stat').read_text();v=raw[raw.rindex(')')+2:].split()
  return {'pid':pid,'state':v[0],'processGroup':int(v[2]),'startTicks':int(v[19])}
 except (FileNotFoundError,ProcessLookupError):return None
def group_members(group):
 result=[]
 for p in pathlib.Path('/proc').iterdir():
  if p.name.isdecimal():
   x=proc(int(p.name))
   if x and x['processGroup']==group:result.append(x)
 return result
assert sha(RECEIPT)==EXPECTED
r=json.loads(RECEIPT.read_text());cpath=PREP/'entry-registration-retry-commands.json'
assert sha(cpath)==r['commandSnapshotSha256']=='94a0768e4f112c09df1546a9529d5b79a960cffbeeaff4ee4231c02a7be521de'
c=json.loads(cpath.read_text())
paths={}
for field,base in [('filesSha256',PREP),('sourceSha256',ROOT),('app147Sha256',ROOT),('currentExternalInputsSha256',ROOT)]:
 for name,digest in r[field].items():
  p=pathlib.Path(name);p=p if p.is_absolute() else base/p
  key=str(p)
  if key in paths:assert paths[key]==digest
  paths[key]=digest
paths[str(RECEIPT)]=EXPECTED
write('expected-inputs.json',paths)
def snapshot():
 result={}
 for name,digest in paths.items():
  p=pathlib.Path(name)
  assert p.is_file() and not p.is_symlink(),f'input-file:{name}'
  actual=sha(p);assert actual==digest,f'input-hash:{name}'
  result[name]=actual
 return result
started=now();before=snapshot();write('before-inputs.json',before)
env=os.environ.copy();removed={}
for name in c['removeInheritedEnvironment']:
 removed[name]=name in env;env.pop(name,None)
env.update(c['explicitBuildEnvironment'])
write('environment.json',{'explicit':c['explicitBuildEnvironment'],'removedInheritedNamesPresent':removed,'cwd':c['cwd'],'applicationRuntimeInvoked':False})
commands=[('restore',a) for a in c['restoreArgv']]+[('build',a) for a in c['buildArgv']]
assert len(commands)==8
records=[];failure=None
for i,(kind,argv) in enumerate(commands,1):
 assert argv[1]==kind and argv[2] in c['projects']
 if kind=='build':assert '-p:BuildProjectReferences=false' in argv
 log=OUT/f'{i:02d}-{kind}.log';t0=now()
 with log.open('wb') as output:
  child=subprocess.Popen(argv,cwd=c['cwd'],env=env,stdout=output,stderr=subprocess.STDOUT,start_new_session=True)
  identity=proc(child.pid)
  write(f'{i:02d}-{kind}-running.json',{'argv':argv,'cwd':c['cwd'],'startUtc':t0,'process':identity})
  rc=child.wait()
 group=child.pid if identity is None else identity['processGroup']
 members=group_members(group)
 record={'ordinal':i,'kind':kind,'argv':argv,'cwd':c['cwd'],'startUtc':t0,'endUtc':now(),'process':identity,'exitCode':rc,'pidAbsent':proc(child.pid) is None,'remainingGroupMembers':members,'log':str(log),'logSha256':sha(log),'logBytes':log.stat().st_size}
 records.append(record);write(f'{i:02d}-{kind}-receipt.json',record)
 print(json.dumps({'ordinal':i,'kind':kind,'exitCode':rc,'pidAbsent':record['pidAbsent'],'groupAbsent':not members}),flush=True)
 if rc or members:
  failure=f'command-{i}';break
try:
 after=snapshot();write('after-inputs.json',after);unchanged=before==after
except BaseException as e:
 unchanged=False;failure=failure or 'input-check-failed';write('input-check-failure.json',{'type':type(e).__name__})
outputs={}
for role in ['projects','tenants','folders','excluded']:
 for area in ['bin','obj']:
  d=PREP/'entry'/role/area
  if d.exists():
   for p in sorted(d.rglob('*')):
    if p.is_file() and not p.is_symlink():outputs[str(p)]={'sha256':sha(p),'bytes':p.stat().st_size}
write('actual-entry-output-files.json',outputs)
summary={'startUtc':started,'endUtc':now(),'preparationReceipt':{'path':str(RECEIPT),'sha256':EXPECTED},'commandsSnapshotSha256':sha(cpath),'commandCount':len(records),'allEightCommandsClosedZero':len(records)==8 and all(x['exitCode']==0 and x['pidAbsent'] and not x['remainingGroupMembers'] for x in records),'currentInputCount':len(paths),'allInputHashesBeforeAfterUnchanged':unchanged,'commands':records,'actualEntryOutputFileCount':len(outputs),'assemblyLoadsOrFixtureNativeCounterWindowExecutionInvoked':False,'failure':failure}
write('receipt.json',summary)
print(json.dumps({k:v for k,v in summary.items() if k!='commands'}),flush=True)
sys.exit(0 if summary['allEightCommandsClosedZero'] and unchanged and not failure else 1)
