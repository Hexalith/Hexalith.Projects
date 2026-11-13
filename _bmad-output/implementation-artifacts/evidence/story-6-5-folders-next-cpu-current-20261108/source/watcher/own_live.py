"""Exact ordinary npm managed launcher and independent observer; no source/launcher replacement."""
from pathlib import Path
import argparse,datetime,subprocess,sys
from common import *
from coordinate import members

def cleanup_audit(live,profile):
 audit=Path('/tmp/story65-receiver-acceptance-prep-20261107/audit.py');observations=[];deadline=time.monotonic()+10;latest=None
 while True:
  path=Path(str(live)+'-cleanup-observation-'+str(len(observations)+1)+'.json');result=subprocess.run([sys.executable,str(audit),'--evidence',str(live),'--output',str(path)],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);data=read_json(path)if result.returncode==0 else None
  observations.append({'path':str(path),'exitCode':result.returncode,'allRecordedAbsent':isinstance(data,dict)and data.get('allIndependentRecordedProcessAndContainerAbsences')is True})
  if isinstance(data,dict):latest=data
  if latest and latest.get('allIndependentRecordedProcessAndContainerAbsences')is True:break
  if time.monotonic()>=deadline:break
  time.sleep(.25)
 write(Path(str(live)+'-cleanup-observation-history.json'),{'signalsSent':False,'maximumReadOnlyPollingSeconds':10,'observations':observations})
 if latest is None:return {'cleanupAuditAvailable':False,'passed':False}
 watcher=read_json(profile/'watcher-receipt.json')or{};attached=watcher.get('profilerAttached')is True;attempted=watcher.get('anyCollectorLaunched')is True
 latest.update(ordinaryAcceptanceOnly=not attempted,profilerEnabled=attached,profilingAttempted=attempted,actualAttachMetadataObserved=attached,diagnosticOnly=attempted,featureAcceptance=False if attempted else None,profilingDoesNotChangeOriginalRunResult=True)
 write(Path(str(live)+'-independent-cleanup-source-audit.json'),latest)
 native=latest.get('nativeTestProcessCleanup');native_safe=isinstance(native,list)and all(v.get('descendantsAbsent')is True for v in native)
 run=read_json(live/'run.json')or{}
 if native is None:native_safe=run.get('testAssemblySha256')is None and not any(isinstance(c,list)and len(c)>1 and Path(c[1]).name=='Hexalith.Projects.Integration.Tests.dll'for c in run.get('commands',[]))
 keys=['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent']
 passed=all(latest.get(k)is True for k in keys)and latest.get('fullLiveManifestHashMismatches')==0 and native_safe
 return {'cleanupAuditAvailable':True,'passed':passed,'nativeGroupsAbsentOrNeverStarted':native_safe,'fullManifestMismatches':latest.get('fullLiveManifestHashMismatches'),'profileEnabled':attached,'runResult':latest.get('runResult')}

def main(args):
 os.umask(0o077);live=Path('/tmp/story65-folders-next-cpu-owner-live-20261108');profile=Path('/tmp/story65-folders-next-cpu-owner-profile-20261108');plan={'preparationOnly':not args.execute,'ordinaryManagedCommand':['npm','--prefix','tests/e2e','run','test:live:managed'],'live':str(live),'profile':str(profile),'noPathShimImportReplacementSourceHookNoBuildOrLoggerOverride':True,'runtimeCpuOverrideAbsent':True,'oneFixedTarget':['folders'],'noTargetSignalsOrCutoff':True}
 if not args.execute:print(json.dumps(plan,indent=2));return
 pins=read_json(OPS/'execution-pins.json')
 if not args.pins_sha256 or sha(OPS/'execution-pins.json')!=args.pins_sha256 or pins['executionReleased']is not True:raise Guard('execution-not-released')
 for path,digest in pins['inputsSha256'].items():
  if sha(path)!=digest:raise Guard('frozen-input-mismatch')
 sources();assert not live.exists()and not profile.exists();env=os.environ.copy()
 for key in MAPPING|{'DOTNET_PROCESSOR_COUNT','CustomBeforeMicrosoftCommonTargets'}:env.pop(key,None)
 env.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',E2E_SUPPORTED_READS_ONLY='1',E2E_RESOURCE_TIMEOUT_SECONDS='900',E2E_SUPPORTED_EVIDENCE=str(live))
 started=datetime.datetime.now(datetime.timezone.utc).isoformat();log=Path(str(live)+'-console.log');command=plan['ordinaryManagedCommand'];status={**plan,'preparationOnly':False,'phase':'starting','targetSignalsSent':False};managed=None;watcher=None
 try:
  with log.open('wb')as stream:managed=subprocess.Popen(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
  status.update(managedPid=managed.pid,managedStartTicks=None);write(Path(str(live)+'-supervisor.json'),status);identity=process(managed.pid);status['managedStartTicks']=identity['startTicks']if identity else None
  write(Path(str(live)+'-command.json'),{'command':command,'cwd':str(ROOT),'launcherPid':managed.pid,'launcherStartTicks':status['managedStartTicks'],'startedAtUtc':started,'standardEnvironmentAssignments':{k:env[k]for k in['E2E_SUPPORTED_READS_ONLY','E2E_RESOURCE_TIMEOUT_SECONDS','E2E_SUPPORTED_EVIDENCE']},'cpuOverrideRemoved':True,'sourceMappingHooksClearedOnlyBeforeStandardLauncherReemitsNormally':True,'noSourceOverrideOrTopologyCutoff':True})
  try:
   if status['managedStartTicks']is None:raise Guard('managed-start-identity-unavailable')
   watch=[sys.executable,str(OPS/'watch_live.py'),'--evidence',str(live),'--output',str(profile),'--tool',pins['toolPath'],'--not-before-utc',started,'--launcher-pid',str(managed.pid),'--launcher-start',str(status['managedStartTicks'])]
   watcher=subprocess.Popen(watch,cwd=OPS,env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,start_new_session=True);status.update(watcherPid=watcher.pid,watcherStartTicks=None,watcherGroup=watcher.pid);write(Path(str(live)+'-supervisor.json'),status);wi=process(watcher.pid);status['watcherStartTicks']=wi['startTicks']if wi else None
  except Exception as failure:status.update(diagnosticSkippedReason='observer-spawn-or-identity-failed',diagnosticErrorType=type(failure).__name__)
  status['phase']='ordinary-managed-running';write(Path(str(live)+'-supervisor.json'),status);status['managedExitCode']=managed.wait()
 finally:
  # Diagnostic startup or parser failures never abandon or signal the unchanged managed topology.
  if managed is not None:
   status['managedExitCode']=managed.wait();status['managedIdentityAbsent']=not same(managed.pid,status.get('managedStartTicks'))
  if watcher is not None:
   status['watcherExitCode']=watcher.wait();until=time.monotonic()+10
   while members(watcher.pid)and time.monotonic()<until:time.sleep(.05)
   status['watcherGroupRemaining']=members(watcher.pid);status['watcherGroupAbsent']=not status['watcherGroupRemaining']
  try:status['cleanup']=cleanup_audit(live,profile);status['source59After']=sources()
  except Exception as failure:status['finalAuditErrorType']=type(failure).__name__
  status['phase']='ordinary-managed-and-observer-complete';write(Path(str(live)+'-supervisor.json'),status)
 print(json.dumps({'phase':status['phase'],'managedExitCode':status.get('managedExitCode'),'cleanupPassed':status.get('cleanup',{}).get('passed',False)}))

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--execute',action='store_true');p.add_argument('--pins-sha256');main(p.parse_args())
