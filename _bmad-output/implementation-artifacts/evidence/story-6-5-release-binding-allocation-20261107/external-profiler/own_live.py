from pathlib import Path
import datetime,hashlib,json,os,subprocess,time
ROOT=Path('/home/administrator/projects/hexalith/projects');PREP=Path(__file__).parent
LIVE=Path('/tmp/story65-release-binding-alloc-owner-live-20261107');WATCH=Path('/tmp/story65-release-binding-alloc-owner-profile-20261107')
def sha(path):
 with path.open('rb')as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def save(path,value):
 temp=Path(str(path)+'.tmp');temp.write_text(json.dumps(value,indent=2)+'\n');temp.chmod(0o600);temp.replace(path)
def ticks(pid):return Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split()[19]
os.umask(0o077);assert not LIVE.exists()and not WATCH.exists()
env=os.environ.copy();removed=[]
for key in ['DOTNET_PROCESSOR_COUNT','CustomBeforeMicrosoftCommonTargets','CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration']:
 if key in env:removed.append(key)
 env.pop(key,None)
assignments={'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','E2E_SUPPORTED_READS_ONLY':'1','E2E_RESOURCE_TIMEOUT_SECONDS':'900','E2E_SUPPORTED_EVIDENCE':str(LIVE)};env.update(assignments)
command=['npm','--prefix','tests/e2e','run','test:live:managed'];started=datetime.datetime.now(datetime.timezone.utc).isoformat();log=Path(str(LIVE)+'-console.log')
with log.open('wb')as stream:managed=subprocess.Popen(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
receipt={'command':command,'cwd':str(ROOT),'environmentAssignments':assignments,'environmentRemovals':['DOTNET_PROCESSOR_COUNT'],'inheritedSourceMappingCleared':True,'inheritedKeysActuallyRemoved':removed,'startedAtUtc':started,'stdoutStderr':str(log),'privateDiagnosticOverride':False,'launcherPid':managed.pid,'launcherStartTicks':ticks(managed.pid),'ownerSupervisorPid':os.getpid(),'ownerSupervisorStartTicks':ticks(os.getpid()),'noTopologyCutoff':True};save(Path(str(LIVE)+'-command.json'),receipt)
watchcmd=['python3',str(PREP/'watch_live.py'),'--evidence',str(LIVE),'--not-before-utc',started,'--owner-receipt','/tmp/story65-owner-tests/release-binding-command-receipt.json','--owner-receipt-sha256','100bc0a86c44bc663715a0401e3f6a65dbc8cae631a1210f5790da6a521c3327','--gateway-sha256','3217866af55943fcf1fa6483bd13f4735e21d515e2ed24421470f8e066ed42ab','--watch-output',str(WATCH),'--tool',str(PREP/'bin/Debug/net10.0/AllocationProbe.dll')]
with Path(str(WATCH)+'-console.log').open('wb')as stream:watcher=subprocess.Popen(watchcmd,cwd=PREP,env=env,stdout=stream,stderr=subprocess.STDOUT)
status={'phase':'running','live':str(LIVE),'watch':str(WATCH),'launcherPid':managed.pid,'launcherStartTicks':ticks(managed.pid),'watcherPid':watcher.pid,'watcherStartTicks':ticks(watcher.pid),'watcherSha256':sha(PREP/'watch_live.py'),'toolSha256':sha(PREP/'bin/Debug/net10.0/AllocationProbe.dll'),'featureAcceptance':False,'noSourceOrPolicyChanges':True};save(Path(str(LIVE)+'-supervisor.json'),status);print(json.dumps(status),flush=True)
receipt['exitCode']=managed.wait();receipt['finishedAtUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat();receipt['logSha256']=sha(log);save(Path(str(LIVE)+'-command.json'),receipt)
try:status['watcherExitCode']=watcher.wait(timeout=65)
except subprocess.TimeoutExpired:status['watcherStillRunningAfterManagedCompletion']=True
status.update(phase='managed-complete',managedExitCode=receipt['exitCode'],finishedAtUtc=receipt['finishedAtUtc']);save(Path(str(LIVE)+'-supervisor.json'),status);print(json.dumps(status),flush=True)
