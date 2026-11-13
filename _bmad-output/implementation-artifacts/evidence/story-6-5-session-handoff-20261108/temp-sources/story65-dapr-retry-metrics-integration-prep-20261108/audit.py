"""Accepted ordinary cleanup checks, projected to fixed scalar metadata; no cleanup mutation."""
from common import *
import statistics


def observe_cleanup(evidence):
 run=read(evidence/'run.json');identities=[]
 owned=run.get('ownedAppHostSession',{})
 for name in ['appHost','cli']:
  if name+'Pid'in owned:identities.append((owned[name+'Pid'],owned[name+'StartTicks']))
 for row in run.get('runtimeModes',{}).values():
  identities.extend([(row['pid'],row['startTicks']),(row['wrapperPid'],row['wrapperStartTicks'])])
 for row in run.get('discovery',{}).get('ownedSidecars',[]):identities.append((row['pid'],row['startTicks']))
 command=read(Path(str(evidence)+'-command.json'));identities.append((command['launcherPid'],command['launcherStartTicks']))
 # A matching zombie is still present; no successful cleanup claim merely because it cannot execute.
 absent=[]
 for pid,ticks in identities:
  current=process(pid);absent.append(current is None or current['start']!=str(ticks))
 containers=[]
 for row in run.get('cleanup',[]):
  if 'containerId'not in row:continue
  probe=subprocess.run(['docker','inspect',row['containerId']],capture_output=True,timeout=10)
  containers.append(probe.returncode!=0 and b'no such object'in probe.stderr.lower())
 private=next((Path(arg).parent for cmd in run.get('commands',[])for arg in cmd if isinstance(arg,str)and arg.startswith('/tmp/supported-project-reads-private-')and arg.endswith('/source-mapping')),None)
 oidc=next((r['resource']for r in run.get('cleanup',[])if isinstance(r.get('resource'),str)and r['resource'].endswith('-oidc')),None);namespace_absent=None
 if oidc:
  probe=subprocess.run(['docker','ps','--all','--filter','label=hexalith.supported-reads='+oidc[:-5],'--format','{{.ID}}'],capture_output=True,timeout=10)
  namespace_absent=probe.returncode==0 and not probe.stdout.strip()
 manifest=read(evidence/'source-manifest.json');mismatch=0
 for rel,digest in manifest.items():
  path=Path(rel);require(not path.is_absolute()and'..'not in path.parts and(ROOT/path).resolve().is_relative_to(ROOT),'cleanup-source-coordinate');mismatch+=sha(ROOT/path)!=digest
 native=run.get('testProcessCleanup');native_started=any(isinstance(c,list)and len(c)>1 and Path(c[1]).name=='Hexalith.Projects.Integration.Tests.dll'for c in run.get('commands',[]))or run.get('testAssemblySha256')is not None
 if native is None:native_safe=not native_started;native_count=0
 else:
  require(isinstance(native,list),'native-cleanup-schema');native_count=len(native)
  native_safe=all(r.get('descendantsAbsent')is True and type(r.get('groupId'))is int and not members(r['groupId'])for r in native)
 rows=observations(evidence);single_rows=[]
 path=evidence/'browser-advisory-seed-readiness.jsonl'
 if path.is_file():
  for line in path.read_bytes().split(b'\n')[:-1]:
   try:r=json.loads(line)
   except ValueError:continue
   if isinstance(r,dict)and r.get('multiple')is False:single_rows.append(r)
 checks={
  'allIndependentRecordedProcessAndContainerAbsences':bool(identities)and all(absent)and all(containers),
  'allOriginalCleanupAbsenceVerified':all(r.get('absenceVerified')is True for r in run.get('cleanup',[])),
  'preexistingResourcesPreserved':run.get('preexistingResourcesPreserved')is True,
  'ownedSidecarsAbsent':run.get('ownedSidecarsAbsent')is True,
  'privateMaterialRemoved':run.get('privateMaterialRemoved')is True,
  'namespaceContainersAbsent':namespace_absent is True,
  'privateRuntimeDirectoryAbsent':private is not None and not private.exists(),
  'nativeGroupsAbsentOrNeverStarted':native_safe,
  'fullLiveManifestUnchanged':mismatch==0}
 return {'passed':all(checks.values()),'checks':checks,'recordedIdentityCount':len(identities),'recordedIdentitiesAbsentIncludingZombies':sum(absent),'recordedContainerCount':len(containers),'recordedContainersAbsent':sum(containers),'nativeGroupCount':native_count,'fullLiveManifestSourceCount':len(manifest),'fullLiveManifestHashMismatches':mismatch,'runResult':run.get('result')if run.get('result')in ['passed','failed']else'unknown','singleAttempts':len(single_rows),'singleVerifiedPositives':sum(r.get('positiveCandidatesVerified')is True for r in single_rows),'multipleAttempts':len(rows),'multipleVerifiedPositives':sum(r.get('positiveCandidatesVerified')is True for r in rows),'matrixCompleted':run.get('matrixCompleted')is True,'noRawProcessContainerNamespaceOrGraphIdentifiersPersisted':True,'signalsSent':False,'featureAcceptance':False}
