from pathlib import Path
import datetime,hashlib,importlib.util,json,sys
root=Path('/home/administrator/projects/hexalith/projects');private=Path('/tmp/supported-project-reads-private-21lsiqi5');packet=root/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-48/full-replay'
spec=importlib.util.spec_from_file_location('story61_launcher_recovery',root/'tools/qualification/run_supported_project_reads.py');module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=module;spec.loader.exec_module(module)
original_bytes=(packet/'run.json').read_bytes();original=json.loads(original_bytes);owned=json.loads((private/'apphost-session.json').read_bytes());isolation=owned['_isolation'];config=private/'fixture.json';capture=json.loads((private/'infrastructure-observed.json').read_bytes())
assert original['qualificationGroup']=='full-replay' and original['result']=='failed' and original['recoveryDirectory']==str(private)
assert module.exact_apphost_sessions()==[], 'An exact AppHost session remains'
assert module.owned_apphost_gone(owned), 'An original owned process remains'
assert hashlib.sha256(config.read_bytes()).hexdigest()==owned['configurationSha256']
census_code="""from pathlib import Path
import sys,json,os
needle=sys.argv[1].encode();matches=[]
for p in Path('/proc').iterdir():
 if not p.name.isdigit() or int(p.name)==os.getpid():continue
 try:data=(p/'environ').read_bytes()
 except FileNotFoundError:continue
 if needle in data:matches.append(int(p.name))
print(json.dumps(matches))
"""
assert not json.loads(module.execute(['sudo','-n','python3','-c',census_code,str(private)])), 'Exact private configuration process remains'
assert not module.owned_sidecar_census(isolation), 'Namespace sidecars remain'
expected={r['id'] for r in original['mtlsInfrastructure']}|{r['containerId'] for r in original['cleanup'] if r['resource'].endswith('-redishost')}
assert len(expected)==4 and {r['id'] for r in capture['containers']}=={r['id'] for r in original['mtlsInfrastructure']}
assert not expected.intersection(module.container_listing()), 'Captured immutable owned container remains'
assert not module.execute(['docker','ps','--all','--no-trunc','--filter','label=hexalith.supported-reads='+isolation,'--format','{{.ID}}']).strip(), 'An owner-labeled container remains'
module.validate_infrastructure_capture(capture,isolation,stopped=True)
report={'result':'owned-recovery-cleanup','observedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'cleanup':[{'resource':'exact-apphost','absenceVerified':True}]+[{'resource':'immutable-owned-container','containerId':x,'absenceVerified':True} for x in sorted(expected)],'ownedSidecarsAbsent':True,'exactSessionAbsent':True,'originalProcessIdentitiesAbsent':True,'configurationProcessCensusEmpty':True,'ownedContainerLabelCensusEmpty':True,'allFourCapturedImmutableOwnedContainersAbsent':True,'originalCanonicalCaptureAndReceiptsRevalidated':True,'originalFailedReportSha256':hashlib.sha256(original_bytes).hexdigest(),'originalFailedReportPreserved':True,'noAdditionalContainerRemovalInvoked':True,'noUnrelatedResourceAction':True}
module.cleanup_private_material(private,isolation,capture,True,report)
assert report.get('privateMaterialRemoved') and not report.get('recoveryMaterialRetained'),report
assert (packet/'run.json').read_bytes()==original_bytes
(packet/'recovery-cleanup.json').write_text(json.dumps(report,indent=2)+'\n')
print('Fresh exact session/process/config/namespace/container absence and captured receipt proof pass; private material removed; original failure preserved.')
