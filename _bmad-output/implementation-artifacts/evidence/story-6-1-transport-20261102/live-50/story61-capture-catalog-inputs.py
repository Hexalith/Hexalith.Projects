from pathlib import Path
import base64,hashlib,json,os,stat,sys,time
launcher=int(sys.argv[1]);evidence=Path(sys.argv[2]);destination=Path(sys.argv[3]);private=None;identity=None
assert destination.stat().st_uid==os.geteuid() and stat.S_IMODE(destination.stat().st_mode)==0o700

def read(path,limit=4194304):
 if any(x.is_symlink() for x in [path,*path.parents]):raise RuntimeError("Link refused")
 fd=os.open(path,os.O_RDONLY|os.O_NOFOLLOW)
 try:
  a=os.fstat(fd)
  if not stat.S_ISREG(a.st_mode) or a.st_uid!=os.geteuid() or a.st_nlink!=1 or a.st_mode&0o077 or not 0<a.st_size<=limit:raise RuntimeError("Private file ownership refused")
  data=os.read(fd,limit+1);b=os.fstat(fd)
  if (a.st_dev,a.st_ino,a.st_size,a.st_mtime_ns)!=(b.st_dev,b.st_ino,b.st_size,b.st_mtime_ns) or len(data)!=a.st_size:raise RuntimeError("Private input changed during read")
  return data
 finally:os.close(fd)

for _ in range(15000):
 try:
  if private is None:
   for child in Path('/proc',str(launcher),'task',str(launcher),'children').read_text().split():
    try:env=dict(x.split(b'=',1) for x in Path('/proc',child,'environ').read_bytes().split(b'\0') if b'=' in x)
    except OSError:continue
    if env.get(b'SUPPORTED_PROJECT_READ_EVIDENCE')!=str(evidence).encode() or b'SUPPORTED_PROJECT_READ_CONFIG' not in env:continue
    candidate=Path(env[b'SUPPORTED_PROJECT_READ_CONFIG'].decode()).parent;info=candidate.lstat()
    if not stat.S_ISDIR(info.st_mode) or info.st_uid!=os.geteuid() or stat.S_IMODE(info.st_mode)!=0o700:raise RuntimeError("Owner directory refused")
    private=candidate;identity=(info.st_dev,info.st_ino);break
  if private is not None and (private/'catalog-plane.json').exists():
   info=private.lstat()
   if (info.st_dev,info.st_ino)!=identity:raise RuntimeError("Owner directory identity changed")
   config=json.loads(read(private/'fixture.json'));isolation=config['Projects']['SupportedReads']['IsolationId'];receipt=json.loads(read(private/'catalog-plane.json'));directory=Path(receipt['directory'])
   if receipt['isolationId']!=isolation or Path(receipt['custodyDirectory'])!=private/'catalog-custody' or directory.parent!=Path('/tmp') or not directory.name.startswith('hexalith-route-catalog-'):raise RuntimeError("Canonical receipt differs")
   owned=directory.lstat()
   if not stat.S_ISDIR(owned.st_mode) or owned.st_uid!=os.geteuid() or stat.S_IMODE(owned.st_mode)!=0o700:raise RuntimeError("Canonical directory refused")
   names=['catalog.json','activation.json','eventstore.observation.json','folders.observation.json'];inputs={name:read(directory/name) for name in names}
   if hashlib.sha256(inputs['catalog.json']).hexdigest()!=receipt['rootDigest']:raise RuntimeError("Canonical root differs")
   activation=json.loads(inputs['activation.json']);fields={k.lower():v for k,v in activation.items()}
   if fields['rootdigest']!=receipt['rootDigest'] or base64.b64decode(fields['canonicalcatalog'])!=inputs['catalog.json']:raise RuntimeError("Activation bytes differ")
   for name,data in inputs.items():
    json.loads(data);target=destination/('input-'+name);fd=os.open(target,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
    try:os.write(fd,data)
    finally:os.close(fd)
   facts={'canonicalRootVerified':True,'activationBytesVerified':True,'receiptIsolationVerified':True,'files':{name:hashlib.sha256(data).hexdigest() for name,data in inputs.items()}}
   (destination/'input-capture-facts.json').write_text(json.dumps(facts,indent=2)+'\n');(destination/'input-capture-facts.json').chmod(0o600)
   print('Captured four exact owner-private canonical observation inputs.');break
 except (FileNotFoundError,json.JSONDecodeError):pass
 if (evidence/'run.json').exists():print('Run ended before complete canonical capture; no replacement input accepted.');break
 time.sleep(0.2)
