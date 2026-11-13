from pathlib import Path
import subprocess,os,time,json,hashlib,signal,sys
root=Path('/home/administrator/projects/hexalith/projects');evidence=Path('/tmp/story65-actual-activation-eligibility-20261107');evidence.mkdir(mode=0o700)
sys.path.insert(0,str(root/'tools/qualification'));import run_supported_project_reads as lane
before=set(Path('/tmp').glob('supported-project-reads-private-*'))
env=os.environ.copy();env.pop('CustomBeforeMicrosoftCommonTargets',None)
env.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',DOTNET_PROCESSOR_COUNT='2')
command=[sys.executable,str(root/'tools/qualification/run_supported_project_reads.py'),'--browser','--timeout','900','--evidence',str(evidence/'managed')]
log=(evidence/'launcher-console.log').open('w');process=subprocess.Popen(command,cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT)
receipt={'diagnosticOnly':True,'featureAcceptance':False,'command':command,'launcherPid':process.pid,'ordinaryBinaries':True,'broadTimingOverlay':False,'activationCopyPersisted':False,'probeCompleted':False,'intentionalPreNativeCutoff':False}
status=evidence/'supervisor.json'
def save(phase):receipt['phase']=phase;status.write_text(json.dumps(receipt,indent=2)+'\n')
save('managed-startup')
try:
 deadline=time.monotonic()+750;selected=None
 while process.poll() is None and time.monotonic()<deadline:
  for private in set(Path('/tmp').glob('supported-project-reads-private-*'))-before:
   plane=private/'catalog-plane.json';config=private/'fixture.json'
   if not plane.exists() or not config.exists():continue
   if private.is_symlink() or private.stat().st_uid!=os.getuid() or private.stat().st_mode&0o077:continue
   data=json.loads(plane.read_text());owned=lane.find_owned_apphost(config,data['isolationId'])
   if owned is None:continue
   activation=Path(data['directory'])/'activation.json'
   if not activation.is_file() or activation.is_symlink():continue
   if activation.stat().st_uid!=os.getuid() or activation.parent.stat().st_uid!=os.getuid() or activation.parent.stat().st_mode&0o077:raise RuntimeError('Activation private custody check failed.')
   if not (private/'infrastructure-observed.json').exists():continue
   selected=(private,activation,owned);break
  if selected:break
  time.sleep(.5)
 if selected is None:raise RuntimeError('No live owned actual activation was available before the diagnostic bound.')
 private,activation,owned=selected
 receipt['ownedSession']={k:v for k,v in owned.items() if not k.startswith('_')}
 receipt['privateDirectoryPathSha256']=hashlib.sha256(str(private).encode()).hexdigest()
 save('probing-actual-activation')
 binary=root/'src/Hexalith.Projects.Server/bin/Debug/net10.0'
 probe=['dotnet','/tmp/story65-actual-activation-probe-20261107/bin/Debug/net10.0/Probe.dll',str(activation),str(binary),str(evidence/'actual-activation-facts.json')]
 result=subprocess.run(probe,cwd=root,env={k:v for k,v in env.items() if not k.startswith(('Projects__','Authentication__','NAMESPACE'))},text=True,capture_output=True,timeout=120)
 receipt['probeExitCode']=result.returncode;receipt['probeCompleted']=(evidence/'actual-activation-facts.json').exists()
 receipt['probeStdoutEmpty']=not result.stdout;receipt['probeStderrEmpty']=not result.stderr
 if not receipt['probeCompleted']:raise RuntimeError('Probe did not retain its closed receipt.')
 receipt['intentionalPreNativeCutoff']=True;save('intentional-pre-native-cutoff')
 if process.poll() is None:os.kill(process.pid,signal.SIGTERM)
except BaseException as failure:
 receipt['supervisorFailureType']=type(failure).__name__;save('diagnostic-failed')
 if process.poll() is None:os.kill(process.pid,signal.SIGTERM)
finally:
 try:receipt['launcherExitCode']=process.wait(timeout=180)
 except subprocess.TimeoutExpired:receipt['launcherCleanupStillRunning']=True
 log.close()
 receipt['nativeXmlCollected']=len(list((evidence/'managed').glob('*.xml')))
 receipt['browserCasesCollected']=0
 save('finished')
 print(json.dumps({k:receipt.get(k) for k in ['phase','probeCompleted','probeExitCode','intentionalPreNativeCutoff','launcherExitCode','nativeXmlCollected','supervisorFailureType']}),flush=True)
