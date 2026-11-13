from pathlib import Path
import argparse,datetime,json,math,os,secrets,subprocess,sys,time
from ops_common import *
from writer_fd_proof import owned_projects_writer
p=argparse.ArgumentParser();p.add_argument('--execute',action='store_true');p.add_argument('--pins-sha256');args=p.parse_args()
EVIDENCE=Path('/tmp/story65-advisory-boundary-span-diagnostic-20261108')
command=[sys.executable,str(PREP/'launch_managed.py'),'--operational-pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED','--browser','--timeout','900','--evidence',str(EVIDENCE/'managed')]
def rows():
 path=EVIDENCE/'managed/browser-advisory-seed-readiness.jsonl';out=[]
 if path.exists():
  for line in path.read_bytes().split(b'\n')[:-1]:
   try:v=json.loads(line)
   except ValueError:continue
   if valid(v):out.append(v)
 return out

def valid(v):
 return isinstance(v,dict)and 'result'in v and v.get('multiple')is True and type(v.get('attempt'))is int and 1<=v['attempt']<=30 and type(v.get('httpStatus'))is int and v['httpStatus']==200 and type(v.get('incomplete'))is bool and type(v.get('candidateCount'))is int and v['candidateCount']>=0 and type(v.get('positiveCandidatesVerified'))is bool and type(v.get('elapsedMilliseconds'))in[int,float]and math.isfinite(v['elapsedMilliseconds'])and v['elapsedMilliseconds']>=0 and v['result']in[None,'Empty','SingleCandidate','MultipleCandidates']and v.get('actualPublicGateway')is True and v.get('fabricatedProjectionWrite')is False and v.get('nonBootstrapCredential')is True and v.get('runtimeAuthoritySupplied')is False

def positive(v):return v['result']=='MultipleCandidates'and v['incomplete']is False and v['candidateCount']>=2 and v['positiveCandidatesVerified']is True
def failed(v):return v['result']in[None,'Empty']and v['incomplete']is True and v['candidateCount']==0 and v['positiveCandidatesVerified']is False

def ready(values):return values and not any(positive(v)for v in values)and len({v['attempt']for v in values if failed(v)})>=3 and max(v['attempt']for v in values)<=24

def publish(fd,data):
 name='.advisory-terminal-control';f=os.open(name,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW|os.O_CLOEXEC,0o600,dir_fd=fd);identity=None
 try:
  with os.fdopen(f,'wb',closefd=False)as stream:stream.write(data);stream.flush();os.fsync(f)
  s=os.fstat(f);identity=(s.st_dev,s.st_ino);assert stat.S_ISREG(s.st_mode)and s.st_uid==1000 and stat.S_IMODE(s.st_mode)==0o600 and 0<s.st_size<=128
  receipt['plannedControlIdentity']={'device':s.st_dev,'inode':s.st_ino,'bytes':s.st_size};save(EVIDENCE/'supervisor.json',receipt)
  os.link(name,'window.control',src_dir_fd=fd,dst_dir_fd=fd,follow_symlinks=False)
  seen=os.stat('window.control',dir_fd=fd,follow_symlinks=False);assert(seen.st_dev,seen.st_ino)==identity
  return {'device':s.st_dev,'inode':s.st_ino,'bytes':s.st_size}
 finally:
  os.close(f)
  if identity is not None:
   s=os.stat(name,dir_fd=fd,follow_symlinks=False)
   if(s.st_dev,s.st_ino)==identity:os.unlink(name,dir_fd=fd)

if not args.execute:
 print(json.dumps({'preparationOnly':True,'command':command,'admission':'one8s after3genuinefailedMULTI/latest<=24/noPositive/finalfreshguards','normalManagedCleanupUnchanged':True}));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True);ordinary_sdk_proof()
nw=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory']);assert nw.parent==PREP
native=json.loads((nw/'receipt.json').read_text());assert native['passed']is True and native['caseCount']==139 and native['nativeProcessAndDescendantsAbsent']is True and native['counterDirectoryEmpty']is True
assert sha(ordinary_expected('full'))==native['privateServerBefore']['producerSha256']and sha(ordinary_expected('pdb'))==native['privateServerBefore']['producerPdbSha256']
EVIDENCE.mkdir(mode=0o700);receipt={'diagnosticOnly':True,'featureAcceptance':False,'controlPublished':False,'source65Match':True,'noTopologyCutoffOrSignals':True,'physicalWriterProof':{'observed':False,'reason':'not-observed'},'command':command,'cwd':str(ROOT)}
process=None;directory=None;proof=None
try:
 with(EVIDENCE/'launcher-console.log').open('wb')as stream:
  process=subprocess.Popen(command,cwd=ROOT,env=clean_environment(),stdout=stream,stderr=subprocess.STDOUT)
  receipt['launcherPid']=process.pid;save(EVIDENCE/'supervisor.json',receipt)
  try:
   receipt['launcherStartTicks']=Path('/proc',str(process.pid),'stat').read_text().rsplit(')',1)[1].split()[19]
   save(EVIDENCE/'managed-command.json',{'command':command,'cwd':str(ROOT),'launcherPid':process.pid,'launcherStartTicks':receipt['launcherStartTicks'],'diagnosticOnly':True,'featureAcceptance':False})
   while process.poll()is None:
    observed=rows()
    if not receipt['controlPublished']and not receipt.get('coverageSkip'):
     if any(positive(v)for v in observed):receipt['coverageSkip']='genuine-multiple-positive-observed'
     elif observed and max(v['attempt']for v in observed)>24:receipt['coverageSkip']='late-admission-headroom-refused'
     elif ready(observed)and(EVIDENCE/'runtime-private-projects-proof.json').exists():
      try:
       sources();private_directory(require_empty=True);ordinary_sdk_proof();independent_dependencies();app_marker_proof(True)
       manifest=json.loads((EVIDENCE/'managed/source-manifest.json').read_text());assert all(sha(ROOT/name)==h for name,h in manifest.items())
       proof=json.loads((EVIDENCE/'runtime-private-projects-proof.json').read_text());verify_runtime(proof)
       directory=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC);s=os.fstat(directory)
       assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)and os.listdir(directory)==[]
       # Expensive guards precede this fresh final completed-readiness check.
       latest=rows()
       if process.poll()is not None or not ready(latest):
        receipt['coverageSkip']='readiness-changed-before-window';os.close(directory);directory=None
       else:
        start=time.monotonic_ns();run=secrets.randbelow(2**62)+1;end=start+8_000_000_000
        receipt.update(controlPublicationAttempted=True,runOrdinal=run,admissionStartTicks=start,admissionEndTicks=end,frequency=1000000000);save(EVIDENCE/'supervisor.json',receipt)
        identity=publish(directory,f'{run}\n1000000000\n{start}\n{end}\n'.encode('ascii'))
        receipt.update(controlPublished=True,runOrdinal=run,admissionStartTicks=start,admissionEndTicks=end,frequency=1000000000,failedMultipleBeforeAdmission=len({v['attempt']for v in latest if failed(v)}),controlIdentity=identity)
      except Exception as ex:
       receipt.update(coverageSkip='private-admission-guard-refused',diagnosticFailureType=type(ex).__name__)
       if receipt.get('controlPublicationAttempted'):receipt['controlPublicationPartialPossible']=True
    if receipt['controlPublished']and not receipt['physicalWriterProof']['observed']and directory is not None and not receipt.get('writerObservationClosed'):
     try:
      projects=next(v for v in proof['copies']if v['role']=='projects')
      assert all(same_start(proof['ownerSessionIdentities'][k+'Pid'],proof['ownerSessionIdentities'][k+'StartTicks'])for k in ['appHost','cli'])
      assert same_start(projects['wrapperPid'],projects['wrapperStartTicks'])and sha(ordinary_expected('full'))==proof['privateProducerSha256']
      maps={line.split(maxsplit=5)[-1]for line in Path('/proc',str(projects['pid']),'maps').read_text().splitlines()if len(line.split(maxsplit=5))==6};assert str(ordinary_expected('full'))in maps
      receipt['physicalWriterProof']=owned_projects_writer(projects,directory)
     except Exception as ex:receipt.update(writerObservationClosed=True,physicalWriterProof={'observed':False,'reason':'owned-writer-observation-unavailable','errorType':type(ex).__name__})
    save(EVIDENCE/'supervisor.json',receipt);time.sleep(.1)
  except Exception as ex:receipt.update(coverageSkip='private-controller-fault',diagnosticFailureType=type(ex).__name__)
  finally:receipt['launcherExitCode']=process.wait() # Never abandon/signal/hold original managed cleanup.
except Exception as ex:receipt.update(supervisorFailureType=type(ex).__name__)
finally:
 if process is not None and process.poll()is None:receipt['launcherExitCode']=process.wait()
 if directory is not None:os.close(directory)
 receipt.update(phase='managed-complete',finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),physicalWriterProofCoversOnlyObservedPoint=True)
 save(EVIDENCE/'supervisor.json',receipt)
print(json.dumps({'diagnosticOnly':True,'launcherExitCode':receipt.get('launcherExitCode'),'controlPublished':receipt['controlPublished'],'physicalWriterObserved':receipt['physicalWriterProof']['observed']}))
raise SystemExit(0 if process is not None else 1)
