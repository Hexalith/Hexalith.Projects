from pathlib import Path
import datetime,hashlib,json,os,subprocess,time
ROOT=Path('/home/administrator/projects/hexalith/projects');PREP=Path(__file__).parent
LIVE=Path('/tmp/story65-parsed-template-cpu-owner-live-20261107');WATCH=Path('/tmp/story65-parsed-template-cpu-owner-profile-20261107')
def sha(path):
 with path.open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(path,value):
 temp=Path(str(path)+'.tmp');temp.write_text(json.dumps(value,indent=2)+'\n');temp.chmod(0o600);temp.replace(path)
def ticks(pid):return Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split()[19]
os.umask(0o077);assert not LIVE.exists()and not WATCH.exists()
owner_receipt=Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json');native_audit=Path('/tmp/story65-parsed-template-native-20261107/binary-source-audit.json')
assert sha(owner_receipt)=='2a031e4d183152beb50b56428805fab3af7b08b787dbad3fc6240ca3dd6e1fb8'
assert sha(native_audit)=='696d669ab030d532653567ebdb7deac1481fe63dd68f27c79fda3e3a277d72e8'
owner_sources=json.loads(owner_receipt.read_text())['sourceSha256']
assert len(owner_sources)==48 and all(sha(ROOT/'references/Hexalith.EventStore'/relative)==digest for relative,digest in owner_sources.items())

env=os.environ.copy();removed=[]
for key in ['DOTNET_PROCESSOR_COUNT','CustomBeforeMicrosoftCommonTargets','CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration']:
 if key in env:removed.append(key)
 env.pop(key,None)
assignments={'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','E2E_SUPPORTED_READS_ONLY':'1','E2E_RESOURCE_TIMEOUT_SECONDS':'900','E2E_SUPPORTED_EVIDENCE':str(LIVE)};env.update(assignments)
command=['npm','--prefix','tests/e2e','run','test:live:managed'];started=datetime.datetime.now(datetime.timezone.utc).isoformat();log=Path(str(LIVE)+'-console.log')
with log.open('wb')as stream:managed=subprocess.Popen(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
receipt={'command':command,'cwd':str(ROOT),'environmentAssignments':assignments,'environmentRemovals':['DOTNET_PROCESSOR_COUNT'],'inheritedSourceMappingCleared':True,'inheritedKeysActuallyRemoved':removed,'startedAtUtc':started,'stdoutStderr':str(log),'privateDiagnosticOverride':False,'launcherPid':managed.pid,'launcherStartTicks':ticks(managed.pid),'ownerSupervisorPid':os.getpid(),'ownerSupervisorStartTicks':ticks(os.getpid()),'noTopologyCutoff':True};save(Path(str(LIVE)+'-command.json'),receipt)
watchcmd=['python3',str(PREP/'watch_live.py'),'--evidence',str(LIVE),'--not-before-utc',started,'--owner-receipt','/tmp/story65-owner-tests/parsed-template-command-receipt.json','--owner-receipt-sha256','2a031e4d183152beb50b56428805fab3af7b08b787dbad3fc6240ca3dd6e1fb8','--gateway-sha256','3217866af55943fcf1fa6483bd13f4735e21d515e2ed24421470f8e066ed42ab','--watch-output',str(WATCH),'--tool',str(Path('/tmp/story65-cpu-prep-20261107/bin/Debug/net10.0/CpuProbe.dll'))]
with Path(str(WATCH)+'-console.log').open('wb')as stream:watcher=subprocess.Popen(watchcmd,cwd=PREP,env=env,stdout=stream,stderr=subprocess.STDOUT)
status={'phase':'running','live':str(LIVE),'watch':str(WATCH),'launcherPid':managed.pid,'launcherStartTicks':ticks(managed.pid),'watcherPid':watcher.pid,'watcherStartTicks':ticks(watcher.pid),'watcherSha256':sha(PREP/'watch_live.py'),'toolSha256':sha(Path('/tmp/story65-cpu-prep-20261107/bin/Debug/net10.0/CpuProbe.dll')),'featureAcceptance':False,'noSourceOrPolicyChanges':True};save(Path(str(LIVE)+'-supervisor.json'),status);print(json.dumps(status),flush=True)
receipt['exitCode']=managed.wait();receipt['finishedAtUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat();receipt['logSha256']=sha(log);save(Path(str(LIVE)+'-command.json'),receipt)
try:status['watcherExitCode']=watcher.wait(timeout=65)
except subprocess.TimeoutExpired:status['watcherStillRunningAfterManagedCompletion']=True
status.update(phase='managed-complete',managedExitCode=receipt['exitCode'],finishedAtUtc=receipt['finishedAtUtc']);save(Path(str(LIVE)+'-supervisor.json'),status);print(json.dumps(status),flush=True)
