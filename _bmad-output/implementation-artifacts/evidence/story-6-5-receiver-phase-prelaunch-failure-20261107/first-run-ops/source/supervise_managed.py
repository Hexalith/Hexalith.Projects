from pathlib import Path
import argparse,datetime,json,math,os,re,secrets,stat,subprocess,sys,time
from ops_common import *

# Byte-exact publication helpers from the qualified receiver controller.
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

parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
EVIDENCE=Path('/tmp/story65-receiver-phase-diagnostic-20261107')
command=[sys.executable,str(PREP/'launch_managed.py'),'--operational-pins-sha256',args.pins_sha256 or'REVIEWED_PINS_REQUIRED','--browser','--timeout','900','--evidence',str(EVIDENCE/'managed')]
plan={'preparationOnly':not args.execute,'command':command,'cwd':str(ROOT),'noNewRuntimeEnvironmentOrConfigurationKeys':True,'naturalCpuOverrideAbsent':True,'noRuntimeSourceHook':True,'fixedPrivateControlPath':str(PRIVATE/'window.control'),'eightSecondAdmissionAfterAtLeastThreeGenuineFailedMultiple':True,'latestCompletedAttemptMaximum':24,'noDelayToOriginalCleanup':True,'noCutoffOrTargetSignals':True,'diagnosticOnly':True,'featureAcceptance':False}
def valid_multi(row):
 required={'multiple','attempt','httpStatus','incomplete','candidateCount','positiveCandidatesVerified','elapsedMilliseconds','result','actualPublicGateway','fabricatedProjectionWrite','nonBootstrapCredential','runtimeAuthoritySupplied'}
 return isinstance(row,dict)and required<=set(row)and row['multiple']is True and type(row['attempt'])is int and 1<=row['attempt']<=30 and type(row['httpStatus'])is int and row['httpStatus']==200 and type(row['incomplete'])is bool and type(row['candidateCount'])is int and row['candidateCount']>=0 and type(row['positiveCandidatesVerified'])is bool and type(row['elapsedMilliseconds'])in[int,float]and math.isfinite(row['elapsedMilliseconds'])and row['elapsedMilliseconds']>=0 and row['result']in[None,'Empty','SingleCandidate','MultipleCandidates']and row['actualPublicGateway']is True and row['fabricatedProjectionWrite']is False and row['nonBootstrapCredential']is True and row['runtimeAuthoritySupplied']is False
def positive(row):
 return valid_multi(row)and row['result']=='MultipleCandidates'and row['incomplete']is False and row['candidateCount']>=2 and row['positiveCandidatesVerified']is True
def failed(row):
 return valid_multi(row)and row['result']is None and row['incomplete']is True and row['candidateCount']==0 and row['positiveCandidatesVerified']is False
if not args.execute:print(json.dumps(plan,indent=2));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True)
native_work=Path(json.loads((PREP/'latest-native-attempt.json').read_text())['attemptDirectory']);assert native_work.parent==PREP
native_gate=json.loads((native_work/'receipt.json').read_text());assert native_gate['passed']is True and native_gate['caseCount']==json.loads((PREP/'native-reference-case-set.json').read_text())['expectedTotal'] and native_gate['nativeProcessAndDescendantsAbsent']is True and native_gate['counterDirectoryEmpty']is True
assert sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll'))==native_gate['privateSdkBefore']['producerSha256']and sha(PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.pdb'))==native_gate['privateSdkBefore']['producerPdbSha256']
sdk_proof('private-with-native');independent_dependencies();EVIDENCE.mkdir(mode=0o700);env=clean_environment()
assert 'CustomBeforeMicrosoftCommonTargets'not in env and not any('CAPTURE_COUNTER'in key for key in env)
receipt={**plan,'preparationOnly':False,'launcherPid':None,'launcherStartTicks':None,'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'controlPublished':False}
process=None
with(EVIDENCE/'launcher-console.log').open('wb')as stream:
 try:
  process=subprocess.Popen(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
  # Register the direct child before every fallible identity/read/write operation.
  receipt['launcherPid']=process.pid
  started=Path('/proc',str(process.pid),'stat').read_text().rsplit(')',1)[1].split()[19];receipt['launcherStartTicks']=started
  save(EVIDENCE/'managed-command.json',{'command':command,'cwd':str(ROOT),'launcherPid':process.pid,'launcherStartTicks':started,'diagnosticOnly':True,'featureAcceptance':False,'noCutoffOrSignals':True})
  save(EVIDENCE/'supervisor.json',receipt);print(json.dumps({'phase':'managed-started','launcherPid':process.pid,'evidence':str(EVIDENCE)}),flush=True)
  while process.poll()is None:
   rows=[];path=EVIDENCE/'managed/browser-advisory-seed-readiness.jsonl'
   if path.exists():
    # An unfinished final line is never interpreted as a row or positive.
    for line in path.read_bytes().split(b'\n')[:-1]:
     try:rows.append(json.loads(line))
     except ValueError:continue
   multi=[row for row in rows if valid_multi(row)]
   if not receipt['controlPublished']and multi:
    has_positive=any(positive(row)for row in multi)
    if has_positive:receipt['coverageSkip']='genuine-multiple-positive-observed';save(EVIDENCE/'supervisor.json',receipt)
    elif len({row['attempt']for row in multi if failed(row)})>=3 and max(row['attempt']for row in multi)<=24 and not receipt.get('coverageSkip')and(EVIDENCE/'runtime-private-sdk-proof.json').exists():
     try:
      sources();private_directory(require_empty=True)
      proof=json.loads((EVIDENCE/'runtime-private-sdk-proof.json').read_text());assert proof['allSixPrivateSdkCopiesCoherent']and proof['onlyThreeReceiverPhysicalEntryMetadataMatches']
      manifest_file=EVIDENCE/'managed/source-manifest.json';assert sha(manifest_file)==proof['fullManifestSha256']
      manifest=json.loads(manifest_file.read_text());assert len(manifest)==proof['fullManifestCount']and all(sha(ROOT/relative)==digest for relative,digest in manifest.items())
      producer=PRODUCER/'bin/Debug/net10.0'/(SDK+'.dll');assert sha(producer)==proof['privateProducerSha256']and sha(producer.with_suffix('.pdb'))==proof['privateProducerPdbSha256']and marker(producer)
      for prefix in ['appHost','cli']:
       identity=proof['ownerSessionIdentities'];assert same_start(identity[prefix+'Pid'],identity[prefix+'StartTicks'])
      for value in proof['copies']:
       assert same_start(value['pid'],value['startTicks'])and same_start(value['wrapperPid'],value['wrapperStartTicks']);copy=Path(value['sdkCopyPath']);assert sha(copy)==proof['privateProducerSha256']==value['sdkSha256']and marker(copy)
       assert sha(copy.with_suffix('.pdb'))==proof['privateProducerPdbSha256']==value['pdbSha256']
       maps=Path('/proc',str(value['pid']),'maps').read_text();assert any(line.split(maxsplit=5)[-1]==str(copy)for line in maps.splitlines()if len(line.split(maxsplit=5))==6)
       assert same_start(value['pid'],value['startTicks'])
      sdk_proof('private-with-native');independent_dependencies()
      fresh=[]
      for line in path.read_bytes().split(b'\n')[:-1]:
       try:
        row=json.loads(line)
        if valid_multi(row):fresh.append(row)
       except ValueError:pass
      if any(positive(row)for row in fresh):
       receipt['coverageSkip']='genuine-multiple-positive-observed-at-final-guard';save(EVIDENCE/'supervisor.json',receipt);continue
      if not fresh or len({row['attempt']for row in fresh if failed(row)})<3 or max(row['attempt']for row in fresh)>24:
       receipt['coverageSkip']='final-completed-multiple-window-guard-refused';save(EVIDENCE/'supervisor.json',receipt);continue
      assert all(same_start(value['pid'],value['startTicks'])and same_start(value['wrapperPid'],value['wrapperStartTicks'])for value in proof['copies'])
      assert all(same_start(proof['ownerSessionIdentities'][prefix+'Pid'],proof['ownerSessionIdentities'][prefix+'StartTicks'])for prefix in ['appHost','cli'])
      assert process.poll()is None and same_start(process.pid,started);private_directory(require_empty=True)
      start=time.monotonic_ns();end=start+8_000_000_000
      # Generated opaque ordinal has no inherited request/trace/identity linkage.
      run=secrets.randbelow(2**62)+1
      directory=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
      try:publish_control(directory,f'{run}\n1000000000\n{start}\n{end}\n'.encode())
      finally:os.close(directory)
      receipt.update(controlPublished=True,admissionStartTicks=start,admissionEndTicks=end,frequency=1000000000,runOrdinal=run,failedMultipleBeforeAdmission=len({row['attempt']for row in fresh if failed(row)}),identityReadyBeforeAdmission=True)
      save(EVIDENCE/'supervisor.json',receipt);print(json.dumps({'phase':'private-eight-second-admission','failedMultiple':receipt['failedMultipleBeforeAdmission']}),flush=True)
     except Exception as failure:
      receipt['coverageSkip']='private-admission-guard-refused';receipt['diagnosticFailureType']=type(failure).__name__;save(EVIDENCE/'supervisor.json',receipt)
    elif max(row['attempt']for row in multi)>24:receipt['coverageSkip']='conservative-latest-attempt-headroom-refused';save(EVIDENCE/'supervisor.json',receipt)
   time.sleep(.1)
 except Exception as failure:
  receipt['coverageSkip']='private-controller-failed';receipt['diagnosticFailureType']=type(failure).__name__
 finally:
  # Every post-spawn failure waits for normal managed cleanup. No cutoff/signals.
  if process is not None:receipt['launcherExitCode']=process.wait()
  receipt.update(finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),phase='managed-complete',noTopologyCutoffOrSignals=True)
  save(EVIDENCE/'supervisor.json',receipt)
 print(json.dumps({'phase':'managed-complete','launcherExitCode':receipt.get('launcherExitCode')}),flush=True)
