from pathlib import Path
import datetime,hashlib,json,os,stat,subprocess,time
P=Path('/tmp/story65-capture-counter-prep-20261107');Q=P/'differential';D=Path('/tmp/story65-capture-counter-private-20261107');OUT=P/'results';OUT.mkdir(mode=0o700,exist_ok=True)
proof=json.loads((P/'private-directory-proof.json').read_text());before=json.loads((P/'before-differential-preservation.json').read_text())
def sha(f):return hashlib.sha256(f.read_bytes()).hexdigest()
def verify_directory():
 s=D.lstat();assert stat.S_ISDIR(s.st_mode)and not stat.S_ISLNK(s.st_mode)and stat.S_IMODE(s.st_mode)==0o700 and s.st_uid==proof['uid']and s.st_dev==proof['device']and s.st_ino==proof['inode']
def cleanup(label):
 verify_directory();removed=[]
 allowed={'window.control'}|{role+suffix for role in ['gateway','tenants','projects','projects-workers','folders','folders-workers']for suffix in['.jsonl','.closed.json']}
 for f in D.iterdir():
  assert f.name in allowed
  s=f.lstat();record={'name':f.name,'mode':oct(stat.S_IMODE(s.st_mode)),'symlink':stat.S_ISLNK(s.st_mode),'fifo':stat.S_ISFIFO(s.st_mode)}
  if stat.S_ISREG(s.st_mode):
   record.update(bytes=s.st_size,sha256=sha(f))
   if f.suffix in['.jsonl','.json']:
    target=OUT/label/f.name;target.parent.mkdir(mode=0o700,exist_ok=True);target.write_bytes(f.read_bytes())
  else:assert stat.S_ISLNK(s.st_mode)or stat.S_ISFIFO(s.st_mode)
  f.unlink();record['absenceVerified']=not f.exists()and not f.is_symlink();removed.append(record)
 assert not any(D.iterdir());return removed
env=os.environ.copy()
for k in list(env):
 if k.startswith('CustomBefore')or k.startswith('CustomAfter')or k in['AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration','DOTNET_PROCESSOR_COUNT']:env.pop(k)
work=[('matrix',x)for x in['disabled','active','expired','capped','failing']]+[('stress',x)for x in['fifo','symlink','future','malformed','long-control','wrong-frequency','long-window','wrong-mode','role','race','file-cap','fault','midwindow-close','hard-drain']]
receipts=[];cleanup('initial')
for kind,mode in work:
 label=kind+'-'+mode;verify_directory();cmd=['dotnet',str(Q/'bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll'),kind,mode];started=datetime.datetime.now(datetime.timezone.utc).isoformat();clock=time.monotonic()
 with subprocess.Popen(cmd,cwd=Q,env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE)as child:
  pid=child.pid;start_ticks=Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split()[19]
  stdout,stderr=child.communicate(timeout=45);exitcode=child.returncode
 try:value=json.loads(stdout)
 except (ValueError,UnicodeDecodeError):value={'kind':'unparseable-private-driver-output'}
 assert not stderr, 'unexpected scratch stderr omitted'
 target=OUT/(label+'.json');target.write_text(json.dumps(value,indent=2)+'\n')
 absent=not Path(f'/proc/{pid}').exists()
 receipt=dict(label=label,command=cmd,cwd=str(Q),startedUtc=started,finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),wallSeconds=time.monotonic()-clock,pid=pid,startTicks=start_ticks,processAbsent=absent,exitCode=exitcode,stdoutSchemaClosed=True,stderrEmpty=True,resultSha256=sha(target),cleanup=cleanup(label))
 receipts.append(receipt);(OUT/'command-receipts.json').write_text(json.dumps(receipts,indent=2)+'\n')
 print(json.dumps(dict(label=label,exitCode=exitcode,wallSeconds=round(receipt['wallSeconds'],3),passFacts={k:v for k,v in value.items()if k in['allPairsEqual','allExpectedOutcomes','counterPartitionEqual','pass','failureType']},processAbsent=absent)),flush=True)
source_bad=[k for k,v in before['sources'].items()if sha(Path(k))!=v];binary_bad=[k for k,v in before['binaries'].items()if sha(Path(k))!=v]
values=[json.loads((OUT/(x['label']+'.json')).read_text())for x in receipts]
summary=dict(scratchOnly=True,featureAcceptance=False,groups=len(receipts),matrixRows=sum(x.get('cases',0)for x in values),supplementalMatrixRows=sum(x.get('supplementalCases',0)for x in values),corePairedExecutions=sum(row['pairs']for x in values for row in x.get('results',[])if not row.get('supplemental')),supplementalPairedExecutions=sum(row['pairs']for x in values for row in x.get('results',[])if row.get('supplemental')),allExitZero=all(x['exitCode']==0 for x in receipts),allOwnedProcessesAbsent=all(x['processAbsent']for x in receipts),allOwnedOutputRemoved=not any(D.iterdir()),source49Mismatches=len(source_bad),applicationBinary147Mismatches=len(binary_bad),helperSha256=sha(P/'CaptureCounters.cs'),driverSha256=sha(Q/'Program.cs'),buildReceiptSha256=sha(P/'differential-build-receipt.json'))
(OUT/'summary.json').write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps(summary),flush=True)
