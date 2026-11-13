from pathlib import Path
import subprocess,os,time,json,hashlib,signal,sys
ROOT=Path('/home/administrator/projects/hexalith/projects');OVERLAY=Path('/tmp/story65-fresh-stream-overlay-20261107');EVIDENCE=Path('/tmp/story65-fresh-stream-diagnostic-20261107');EVIDENCE.mkdir(mode=0o700)
inputs=json.loads((OVERLAY/'source-inputs.json').read_text());assert all(hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==v['sourceSha256'] for path,v in inputs.items())
env=os.environ.copy();env.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',DOTNET_PROCESSOR_COUNT='2',CustomBeforeMicrosoftCommonTargets=str(OVERLAY/'focused.targets'))
env['FOCUSED_DOCKER_FAILURE_RECEIPT']=str(EVIDENCE/'docker-start-failure.json')
command=[sys.executable,str(Path('/tmp/story65-fresh-stream-prep-20261107/launch_with_docker_failure_receipt.py')),'--browser','--timeout','900','--evidence',str(EVIDENCE/'managed')]
output=(EVIDENCE/'launcher-console.log').open('w');process=subprocess.Popen(command,cwd=ROOT,env=env,stdout=output,stderr=subprocess.STDOUT)
receipt={'diagnosticOnly':True,'featureAcceptance':False,'command':command,'launcherPid':process.pid,'sourceInputsCheckedBeforeLaunch':True,'privateOverlay':str(OVERLAY),'samplingLimit':3,'cutoffRequested':False}
def save(phase):receipt['phase']=phase;(EVIDENCE/'supervisor.json').write_text(json.dumps(receipt,indent=2)+'\n')
def root_counts():
 begin=end=0
 for f in OVERLAY.glob('Hexalith.Projects.Server-*.jsonl'):
  for line in f.read_text().splitlines():
   try:r=json.loads(line)
   except ValueError:continue
   if r.get('stage')=='advisory.root':
    begin+=r.get('state')=='begin';end+=r.get('state')=='end'
 return begin,end
save('startup-build')
try:
 deadline=time.monotonic()+900;complete=None;stable=None;last_size=None
 while process.poll() is None and time.monotonic()<deadline:
  starts,ends=root_counts()
  if starts:receipt['rootStarts']=starts;receipt['rootEnds']=ends;save('warm-advisory-sampling')
  if (OVERLAY/'sample-complete').exists() or (EVIDENCE/'managed/browser-seed.xml').exists():
   if complete is None:complete=time.monotonic();save('draining-opened-spans')
   sizes=sum(f.stat().st_size for f in OVERLAY.glob('*.jsonl'))
   if sizes!=last_size:last_size=sizes;stable=time.monotonic()
   if (stable is not None and time.monotonic()-stable>=2) or time.monotonic()-complete>=20:
    receipt['rootStarts'],receipt['rootEnds']=root_counts();receipt['cutoffRequested']=True;receipt['drainSeconds']=round(time.monotonic()-complete,3);save('intentional-diagnostic-cutoff');os.kill(process.pid,signal.SIGTERM);break
  time.sleep(.25)
 if process.poll() is None and not receipt['cutoffRequested']:
  receipt['cutoffRequested']=True;receipt['reason']='diagnostic-supervisor-bound';os.kill(process.pid,signal.SIGTERM)
except BaseException as failure:
 receipt['supervisorFailureType']=type(failure).__name__
 if process.poll() is None:os.kill(process.pid,signal.SIGTERM)
finally:
 try:receipt['launcherExitCode']=process.wait(timeout=180)
 except subprocess.TimeoutExpired:receipt['cleanupStillRunning']=True
 output.close();save('finished');print(json.dumps(receipt),flush=True)
