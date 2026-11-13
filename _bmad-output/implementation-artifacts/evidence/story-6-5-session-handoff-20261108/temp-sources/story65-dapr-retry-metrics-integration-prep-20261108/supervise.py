"""Exact ordinary npm child plus one external observer; no launcher hooks or application control."""
from common import *
from audit import observe_cleanup
import argparse,datetime


def main(args):
 if not args.execute:
  print(json.dumps({'executionReleased':False,'ordinaryManagedCommand':COMMAND,'evidence':str(LIVE),'observation':str(OUTPUT),'profiler':False,'overlay':False,'sourceLauncherHooks':False}));return
 os.umask(0o077);pins=verify_preparation(args.release,args.release_sha256,released=True)
 require(pins.get('core41GuardsQualified')is True,'core-offline-qualification-required')
 c=controls();c.evidence(pins['coreGuardQualification'])
 before=ordinary_preflight();require(not LIVE.exists()and not OUTPUT.exists()and not list(LIVE.parent.glob(LIVE.name+'-*')),'fresh-operational-coordinates')
 write(Path(str(LIVE)+'-preflight.json'),{'source65Match':before['sources']['allMatch'],'app147Current':len(before['generations']['currentApp147Sha256'])==147,'additionalAssemblyCount':len(before['generations']['additionalHexalithAssemblyFilesSha256']),'coreSdkContractsProducerAndCopiesMatch':True,'protectedPreservationGuardsPassed':True})
 env=os.environ.copy()
 for key in MAPPING|{'DOTNET_PROCESSOR_COUNT','CustomBeforeMicrosoftCommonTargets'}:env.pop(key,None)
 env.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',E2E_SUPPORTED_READS_ONLY='1',E2E_RESOURCE_TIMEOUT_SECONDS='900',E2E_SUPPORTED_EVIDENCE=str(LIVE))
 started=datetime.datetime.now(datetime.timezone.utc).isoformat();managed=None;watcher=None;managed_ticks=None;watcher_ticks=None
 status={'phase':'starting','diagnosticOnly':True,'featureAcceptance':False,'targetSignalsSent':False,'launcherMethodHooks':False,'profilerAttached':False,'rawConsoleLogsRetained':False,'noRuntimeConfigurationOrPolicyOverrides':True,'command':COMMAND}
 try:
  # Register the directly owned Popen BEFORE any fallible process stat or receipt operation.
  managed=subprocess.Popen(COMMAND,cwd=ROOT,env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  status.update(managedPid=managed.pid,managedStartTicks=None);write(Path(str(LIVE)+'-supervisor.json'),status)
  current=process(managed.pid);managed_ticks=current['start']if current else None;status['managedStartTicks']=managed_ticks
  write(Path(str(LIVE)+'-command.json'),{'command':COMMAND,'cwd':str(ROOT),'launcherPid':managed.pid,'launcherStartTicks':managed_ticks,'startedAtUtc':started,'standardEnvironmentAssignments':{k:env[k]for k in ['E2E_SUPPORTED_READS_ONLY','E2E_RESOURCE_TIMEOUT_SECONDS','E2E_SUPPORTED_EVIDENCE']},'runtimeCpuOverrideAbsent':True,'originalLauncherReemitsAllSevenMappings':True,'noLauncherShimNoBuildOverlayLoggerPolicyDeadlineOrTopologyChange':True})
  if managed_ticks is not None:
   try:
    command=[sys.executable,str(PREP/'watch.py'),'--release',str(args.release),'--release-sha256',args.release_sha256,'--evidence',str(LIVE),'--output',str(OUTPUT),'--not-before-utc',started,'--launcher-pid',str(managed.pid),'--launcher-start',managed_ticks]
    watcher=subprocess.Popen(command,cwd=PREP,env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,start_new_session=True)
    status.update(watcherPid=watcher.pid,watcherStartTicks=None,watcherGroup=watcher.pid);write(Path(str(LIVE)+'-supervisor.json'),status)
    current=process(watcher.pid);watcher_ticks=current['start']if current else None;status['watcherStartTicks']=watcher_ticks
   except Exception:status['observerStartupCategory']='observer-spawn-or-identity-refused'
  else:status['observerStartupCategory']='managed-start-unobservable'
  status['phase']='ordinary-managed-running';write(Path(str(LIVE)+'-supervisor.json'),status);status['managedExitCode']=managed.wait()
 finally:
  # Observer errors NEVER stop, signal, extend or abandon the ordinary managed child's original flow.
  if managed is not None:
   status['managedExitCode']=managed.wait();current=process(managed.pid);status['managedIdentityAbsentIncludingZombies']=current is None or current['start']!=str(managed_ticks)
  if watcher is not None:
   status['watcherExitCode']=watcher.wait();until=time.monotonic()+10
   while members(watcher.pid)and time.monotonic()<until:time.sleep(.05)
   status['watcherGroupAndSessionAbsentIncludingZombies']=not members(watcher.pid)
  latest=None;observations=[];deadline=time.monotonic()+10
  while managed is not None:
   try:latest=observe_cleanup(LIVE);observations.append(latest['passed'])
   except Exception:observations.append(False)
   if latest is not None and latest['passed']or time.monotonic()>=deadline:break
   time.sleep(.25)
  if latest is not None:write(Path(str(LIVE)+'-independent-cleanup-source-audit.json'),latest);status['cleanupPassed']=latest['passed']
  else:status['cleanupPassed']=False
  status['cleanupObservationCount']=len(observations);status['cleanupSignalsSent']=False
  try:
   after=ordinary_current();status.update(source65AfterMatch=after['sources']['allMatch'],protectedAfterMatch=all(after['protected'].values()),coreSdkContractsProducerAndCopiesAfterMatch=True,app149AfterCount=len(after['generations']['currentApp147Sha256'])+len(after['generations']['additionalHexalithAssemblyFilesSha256']),freshStandardBuildApp147GenerationDeltaCount=sum(h!=after['generations']['currentApp147Sha256'][p]for p,h in before['generations']['currentApp147Sha256'].items()),historical10And17GenerationLimitsErased=False)
   run=read(LIVE/'run.json');profile=run.get('compilationProfile',{});status['originalSevenMappingsObserved']=set(run.get('sourceMappingEnvironment',{}))==MAPPING;status['originalDebugSourceAndTwoProofFlagsObserved']=bool(profile)and all(v.get('Configuration')=='Debug'and v.get('Optimize')=='false'and v.get('UseHexalithProjectReferences')=='true'and v.get('UseNuGetDeps')=='false'and v.get('EnablePrivateCatalogInvocationProof')=='true'and v.get('EnablePrivateOwnerSourceReadProof')=='true'for v in profile.values())
   status['finalSourceGenerationProfileAuditPassed']=status['source65AfterMatch']and status['protectedAfterMatch']and status['coreSdkContractsProducerAndCopiesAfterMatch']and status['app149AfterCount']==149 and status['originalSevenMappingsObserved']and status['originalDebugSourceAndTwoProofFlagsObserved']
  except Exception:status['finalSourceGenerationProfileAuditPassed']=False
  status['phase']='ordinary-and-external-observer-closed';write(Path(str(LIVE)+'-supervisor.json'),status)
 print(json.dumps({'phase':status['phase'],'managedExitCode':status.get('managedExitCode'),'cleanupPassed':status.get('cleanupPassed',False),'featureAcceptance':False}))

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--execute',action='store_true');p.add_argument('--release',type=Path);p.add_argument('--release-sha256');main(p.parse_args())
