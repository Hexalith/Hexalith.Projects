from pathlib import Path
import datetime,fcntl,json,os,signal,stat,subprocess,sys
root=Path('/home/administrator/projects/hexalith/projects');evidence=root/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-19';lock=Path('/tmp/hexalith-timesheets-local-runtime.lock')
command=['python3','tools/qualification/run_supported_project_reads.py','--regressions','--timeout','900','--evidence',str(evidence)]
receipt={'protocol':'existing-cooperative-same-user-nonblocking-advisory-lock','lockPath':str(lock),'command':command,'acquired':False,'released':False,'contentsReadOrWritten':False,'lifecycleOfOtherTasksInvoked':False}
def stamp():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def identity(info):return {'device':info.st_dev,'inode':info.st_ino,'uid':info.st_uid,'mode':stat.S_IMODE(info.st_mode),'links':info.st_nlink,'size':info.st_size,'modifiedNs':info.st_mtime_ns}
fd=None;child=None;result=1
try:
 fd=os.open(lock,os.O_RDWR|os.O_NONBLOCK|os.O_NOFOLLOW|os.O_CLOEXEC)
 observed=os.fstat(fd);named=lock.lstat()
 if not stat.S_ISREG(observed.st_mode) or observed.st_uid!=os.geteuid() or observed.st_nlink!=1 or observed.st_mode&0o077 or identity(observed)!=identity(named):raise RuntimeError('Cooperative admission identity is unsafe; no resources started')
 fcntl.flock(fd,fcntl.LOCK_EX|fcntl.LOCK_NB)
 if identity(lock.lstat())!=identity(observed):raise RuntimeError('Cooperative admission identity changed; no resources started')
 receipt.update(acquired=True,acquiredUtc=stamp(),identity=identity(observed))
 for name in ('MSBUILDDISABLENODEREUSE','DOTNET_CLI_USE_MSBUILD_SERVER','DOTNET_PROCESSOR_COUNT'):
  assert os.environ.get(name)=={'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','DOTNET_PROCESSOR_COUNT':'2'}[name]
 def forward(signum,_frame):
  if child is not None and child.poll() is None:child.send_signal(signum)
 signal.signal(signal.SIGTERM,forward);signal.signal(signal.SIGINT,forward)
 with open('/tmp/story61-live19-launcher.log','w') as output:
  os.chmod(output.name,0o600)
  child=subprocess.Popen(command,cwd=root,stdout=output,stderr=subprocess.STDOUT)
  receipt['launcherPid']=child.pid
  Path('/tmp/story61-live19-admission-pending.json').write_text(json.dumps(receipt,indent=2)+'\n');os.chmod('/tmp/story61-live19-admission-pending.json',0o600)
  result=child.wait()
 receipt['childExitCode']=result
 receipt['lockIdentityAndMetadataPreserved']=identity(lock.lstat())==identity(observed)
 if not receipt['lockIdentityAndMetadataPreserved']:result=1
except BlockingIOError:
 receipt['refusal']='Cooperative admission busy; no resources started';result=75
except BaseException as failure:
 receipt['failureType']=type(failure).__name__;raise
finally:
 if fd is not None:os.close(fd)
 receipt['released']=bool(receipt['acquired']);receipt['releasedUtc']=stamp()
 target=evidence/'runtime-admission.json' if evidence.is_dir() else Path('/tmp/story61-live19-admission.json')
 target.write_text(json.dumps(receipt,indent=2)+'\n')
 if target.parent==Path('/tmp'):os.chmod(target,0o600)
print('Cooperative runtime admission released; child exit code:',result) if child is not None else print('Cooperative runtime admission refused; no resources started.')
raise SystemExit(result)
