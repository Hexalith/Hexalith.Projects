from pathlib import Path
import json,hashlib,os,subprocess,importlib.util
ROOT=Path('/home/administrator/projects/hexalith/projects')
BASE=Path('/tmp/story65-invocation-transport-diagnostic-20261107')
OPS=Path('/tmp/story65-invocation-transport-ops-prep-20261107/live')
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def absent(pid,start):
 try:
  x=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return x[0]=='Z' or x[19]!=str(start)
 except FileNotFoundError:return True
def members(group):
 found=[]
 for p in Path('/proc').iterdir():
  if not p.name.isdigit():continue
  try:
   x=(p/'stat').read_text().rsplit(')',1)[1].split()
   if x[0]!='Z' and int(x[2])==group:found.append(int(p.name))
  except (FileNotFoundError,ProcessLookupError,PermissionError):pass
 return found
status=json.loads((OPS/'latest-operational-result.json').read_text());assert all(status[k]==0 for k in ['privateExitCode','native33ExitCode','managedExitCode','ordinaryExitCode'])
audit=json.loads((BASE/'independent-cleanup-source-audit.json').read_text());assert all(audit[k] is True for k in ['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','preexistingResourcesPreserved','privateMaterialRemoved','namespaceContainersAbsent','privateRuntimeDirectoryAbsent','ownedSidecarsAbsent']);assert audit['fullLiveManifestHashMismatches']==0
assert len(audit['identities'])==21 and all(absent(x['pid'],x['startTicks']) for x in audit['identities'])
assert len(audit['containers'])==5
for x in audit['containers']:
 r=subprocess.run(['docker','inspect','--format','{{.Id}}',x['containerId']],capture_output=True);assert r.returncode!=0 and b'no such' in r.stderr.lower() and not r.stdout.strip()
early=json.loads((BASE/'early-runtime-identities.json').read_text())['identities'];assert len(early)==6 and all(absent(x['pid'],x['startTicks']) and absent(x['wrapperPid'],x['wrapperStartTicks']) for x in early)
native=Path(json.loads((OPS/'latest-native-attempt.json').read_text())['attemptDirectory']);nr=json.loads((native/'receipt.json').read_text());assert nr['passed'] and nr['caseCount']==33 and len(nr['processes'])==7
for x in nr['processes']:assert absent(x['pid'],x['startTicks']) and not members(x['processGroup'])
for x in audit['nativeTestProcessCleanup']:assert x['descendantsAbsent'] and not members(x['groupId'])
supervisor=json.loads((BASE/'supervisor.json').read_text());assert supervisor['phase']=='managed-complete' and supervisor['launcherExitCode']==1 and supervisor['controlPublished'] and absent(supervisor['launcherPid'],supervisor['launcherStartTicks'])
manifest=json.loads((BASE/'managed/source-manifest.json').read_text());assert len(manifest)==audit['fullLiveManifestSourceCount'] and all(sha(ROOT/p)==s for p,s in manifest.items())
modulepath=Path('/tmp/story65-invocation-transport-prep-20261107/freshcontroller.py');expected=json.loads((Path('/tmp/story65-invocation-transport-prep-20261107')/'preparation-receipt.json').read_text());assert sha(modulepath)==expected['filesSha256']['freshcontroller.py']
spec=importlib.util.spec_from_file_location('outbound_assertion_archive',modulepath);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
out=BASE/'counter-archive';out.mkdir(mode=0o700)
fd=os.open(m.PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW)
try:artifacts,rows,stages,empty=m.archive_validated(fd,out,supervisor['runOrdinal'],supervisor['frequency'],supervisor['admissionStartTicks'],True,'live','live',{})
finally:os.close(fd)
receipt={'diagnosticOnly':True,'featureAcceptance':False,'rootPipelineExitCode':0,'underlyingManagedLauncherExitCode':1,'all21RecordedIdentitiesIndependentlyAbsent':True,'all5ExactContainerIdsIndependentlyAbsent':True,'all12EarlyIdentitiesAbsent':True,'native33SevenGroupsAndManagedSeedGroupAbsent':True,'managedLauncherIdentityAbsent':True,'fullManifestCount':len(manifest),'fullManifestMismatches':0,'onlyThreeFixedOwnedFilesArchivedAndRemovedAfterGuards':True,'controlContentsNotArchived':True,'rows':rows,'stages':stages,'privateDirectoryEmpty':empty,'artifacts':artifacts,'controllerSha256':sha(modulepath)}
(BASE/'parent-counter-archive-cleanup.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({k:receipt[k] for k in ['rows','stages','privateDirectoryEmpty','fullManifestCount','fullManifestMismatches']}))
