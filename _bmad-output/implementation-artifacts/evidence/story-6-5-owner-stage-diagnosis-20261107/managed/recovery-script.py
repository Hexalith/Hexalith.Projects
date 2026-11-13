from pathlib import Path
import json,sys,subprocess,hashlib,time,os,shutil
sys.path.insert(0,'/home/administrator/projects/hexalith/projects/tools/qualification')
import run_supported_project_reads as lane
runpath=Path('/tmp/story65-owner-stage-diagnostic-corrected-20261107/run.json');before=runpath.read_bytes();run=json.loads(before)
private=Path('/tmp/supported-project-reads-private-k6_24d0g');configuration=private/'fixture.json'
assert private.is_dir() and not private.is_symlink() and private.stat().st_uid==os.getuid()
assert hashlib.sha256(configuration.read_bytes()).hexdigest()==run['ownedAppHostSession']['configurationSha256']
isolation=json.loads(configuration.read_text())['Projects']['SupportedReads']['IsolationId'];capture=json.loads((private/'infrastructure-observed.json').read_text());identities=[]
for role in ['appHost','cli']:
 pid=run['ownedAppHostSession'][role+'Pid'];ticks=run['ownedAppHostSession'][role+'StartTicks'];identities.append({'role':role,'pid':pid,'recordedStartTicks':ticks,'absent':lane.process_start(pid)!=ticks})
for name,facts in run['runtimeModes'].items():
 for pk,tk,suffix in [('pid','startTicks',''),('wrapperPid','wrapperStartTicks','-wrapper')]:
  if facts.get(pk) and facts.get(tk):identities.append({'role':name+suffix,'pid':facts[pk],'recordedStartTicks':facts[tk],'absent':lane.process_start(facts[pk])!=facts[tk]})
for facts in run['discovery']['ownedSidecars']:
 identities.append({'role':facts['appId']+'-sidecar','pid':facts['pid'],'recordedStartTicks':facts['startTicks'],'absent':lane.process_start(facts['pid'])!=facts['startTicks']})
assert all(x['absent'] for x in identities)
assert lane.find_owned_apphost(configuration,isolation) is None and not lane.owned_sidecar_census(isolation)
rows=[json.loads(line) for line in subprocess.check_output(['docker','ps','--all','--no-trunc','--format','{{json .}}'],text=True).splitlines()]
assert not subprocess.check_output(['docker','ps','--all','--no-trunc','--filter','label=hexalith.supported-reads='+isolation,'--format','{{.ID}}'],text=True).strip()
ids={x['id'] for x in run['mtlsInfrastructure']}|{x['containerId'] for x in run['cleanup'] if x.get('containerId')};assert not ids & {x['ID'] for x in rows}
placement=next(x['id'] for x in run['mtlsInfrastructure'] if x['name']=='dapr-placement-mtls');check=subprocess.run(['docker','inspect',placement],text=True,capture_output=True);assert check.returncode==1 and 'no such object' in check.stderr.lower()
matched=[];old=[];cutoff=Path('/tmp/story65-owner-stage-diagnostic-corrected-20261107/launcher-controls.log').stat().st_mtime-60;boot=time.time()-time.monotonic();hz=os.sysconf('SC_CLK_TCK')
for d in Path('/proc').iterdir():
 if not d.name.isdecimal():continue
 try:
  if d.stat().st_uid!=os.getuid():continue
  try:raw=(d/'environ').read_bytes()
  except PermissionError:
   ticks=int((d/'stat').read_text().rsplit(')',1)[1].split()[19]);assert boot+ticks/hz<cutoff;old.append(int(d.name));continue
  env=dict(x.split(b'=',1) for x in raw.split(b'\0') if b'=' in x)
  if env.get(b'NAMESPACE')==isolation.encode() or env.get(b'Projects__SupportedReads__ConfigurationFile')==str(configuration).encode():matched.append(int(d.name))
 except FileNotFoundError:pass
assert not matched
lane.validate_infrastructure_capture(capture,isolation,stopped=True)
for current,dirs,_ in os.walk(private,followlinks=False):
 for name in dirs:
  path=Path(current,name)
  if not path.is_symlink():path.chmod(0o700)
shutil.rmtree(private)
receipt={'diagnosticOnly':True,'featureAcceptance':False,'originalRunResult':run['result'],'originalPlacementRemovalObservationFailurePreserved':True,'originalRunJsonSha256':hashlib.sha256(before).hexdigest(),'recordedProcessIdentities':identities,'allRecordedProcessIdentitiesAbsent':True,'ownedAppHostAbsent':True,'ownedSidecarsAbsent':True,'exactCapturedContainerIdsAbsent':sorted(ids),'placementInspectExitCode':check.returncode,'placementNoSuchObjectVerified':True,'structuredNamespaceContainerListingEmpty':True,'sameOwnerNamespaceProcessCount':0,'unreadableSameOwnerProcessesProvenPreexistingByKernelStart':old,'retainedInfrastructureIdentityChecksPassed':True,'onlyRemovalTarget':str(private),'privateMaterialRemoved':not private.exists(),'originalRunJsonUnchanged':runpath.read_bytes()==before}
Path('/tmp/story65-owner-stage-diagnostic-corrected-20261107/post-placement-cleanup-recovery.json').write_text(json.dumps(receipt,indent=2)+'\n')
print({'recoveryCompleted':receipt['privateMaterialRemoved'],'recordedProcessIdentitiesAbsent':len(identities),'containerIdsAbsent':len(ids),'originalRunJsonUnchanged':receipt['originalRunJsonUnchanged']})
