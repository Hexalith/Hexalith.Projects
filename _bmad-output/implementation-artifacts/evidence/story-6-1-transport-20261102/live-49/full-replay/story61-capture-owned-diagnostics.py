from pathlib import Path
import json,os,stat,sys,time
launcher=int(sys.argv[1]);evidence=Path(sys.argv[2]);destination=Path(sys.argv[3])
assert destination.is_dir() and stat.S_IMODE(destination.stat().st_mode)==0o700
private=None;identity=None
resources=['supported-read-authority','eventstore','tenants','projects','projects-workers','folders','folders-workers','tenants-dapr-cli','eventstore-dapr-cli','projects-dapr-cli','projects-workers-dapr-cli','folders-dapr-cli','folders-workers-dapr-cli']
for _ in range(18000):
 if private is None:
  try:children=Path('/proc',str(launcher),'task',str(launcher),'children').read_text().split()
  except OSError:children=[]
  for child in children:
   try:env=dict(x.split(b'=',1) for x in Path('/proc',child,'environ').read_bytes().split(b'\0') if b'=' in x)
   except OSError:continue
   if env.get(b'SUPPORTED_PROJECT_READ_EVIDENCE')==str(evidence).encode() and b'SUPPORTED_PROJECT_READ_CONFIG' in env:
    candidate=Path(env[b'SUPPORTED_PROJECT_READ_CONFIG'].decode()).parent
    info=candidate.lstat()
    if not stat.S_ISDIR(info.st_mode) or info.st_uid!=os.geteuid() or stat.S_IMODE(info.st_mode)!=0o700:raise RuntimeError('Private ownership changed')
    private=candidate;identity=(info.st_dev,info.st_ino);break
 if private is not None:
  try:
   info=private.lstat()
   if (info.st_dev,info.st_ino)!=identity:raise RuntimeError('Private identity changed')
   for resource in resources:
    path=private/(resource+'.log')
    try:
     descriptor=os.open(path,os.O_RDONLY|os.O_NOFOLLOW)
     try:
      observed=os.fstat(descriptor)
      if not stat.S_ISREG(observed.st_mode) or observed.st_uid!=os.geteuid() or observed.st_nlink!=1 or not 0<observed.st_size<=8*1024*1024:continue
      data=os.read(descriptor,8*1024*1024+1)
     finally:os.close(descriptor)
     parsed=json.loads(data)
     if not parsed.get('logs'):continue
     target=destination/(resource+'.json')
     out=os.open(target,os.O_WRONLY|os.O_CREAT|os.O_TRUNC|os.O_NOFOLLOW,0o600)
     try:os.write(out,data)
     finally:os.close(out)
    except (FileNotFoundError,json.JSONDecodeError):continue
  except FileNotFoundError:pass
 if (evidence/'run.json').exists():break
 time.sleep(0.1)
print('Captured owner-private native diagnostics:',len(list(destination.glob('*.json'))))
