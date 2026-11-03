from pathlib import Path
import importlib.util,json,os,hashlib,sys,re,tempfile,datetime
root=Path('/home/administrator/projects/hexalith/projects');private=Path('/tmp/supported-project-reads-private-_bf8elpo');packet=root/'_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-44';diagnostics=Path('/tmp/story61-owned-diagnostics-live44-iuyhozi8')
spec=importlib.util.spec_from_file_location('story61_launcher_recovery',root/'tools/qualification/run_supported_project_reads.py');module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=module;spec.loader.exec_module(module)
original=json.loads((packet/'run.json').read_bytes());owned=json.loads((private/'apphost-session.json').read_bytes());isolation=owned['_isolation'];config=private/'fixture.json'
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
graph=json.loads((diagnostics/'aspire-describe-startup.json').read_bytes());resources={}
for name in module.MTLS_RESOURCES:
 rows=[r for r in graph['resources'] if r.get('displayName')==name];assert len(rows)==1
 resources[name]=rows[0]
expected={r['properties']['container.id'] for r in resources.values()}|{r['containerId'] for r in original['cleanup'] if 'containerId'in r}
assert len(expected)==4 and all(re.fullmatch('[0-9a-f]{64}',x) for x in expected)
assert not expected.intersection(module.container_listing()), 'Captured immutable owned container remains'
assert not module.execute(['docker','ps','--all','--no-trunc','--filter','label=hexalith.supported-reads='+isolation,'--format','{{.ID}}']).strip(), 'An owner-labeled container remains'
# Recover current canonical ownership only after all independent absence checks.
# The old active healthy-infrastructure capture never existed and is not fabricated.
module.private_identity(private,directory=True);identities={}
for name in ['mtls-plane.json','catalog-plane.json']:
 p=private/name;identities[str(p)]=module.private_identity(p)
mtls=json.loads((private/'mtls-plane.json').read_bytes());catalog=json.loads((private/'catalog-plane.json').read_bytes())
assert mtls['isolationId']==catalog['isolationId']==isolation
run=Path(mtls['runDirectory']);certificate=Path(mtls['certificateDirectory']);directory=Path(catalog['directory']);custody=Path(catalog['custodyDirectory'])
assert run.parent==Path(tempfile.gettempdir())/'hexalith-dapr' and re.fullmatch('[0-9a-f]{32}',run.name)
assert certificate==private/'mtls'/('dapr-credentials-'+hashlib.sha256(str(run).encode()).hexdigest().upper()[:32])
assert directory.parent==Path(tempfile.gettempdir()) and directory.name.startswith('hexalith-route-catalog-') and not directory.exists() and not directory.is_symlink()
assert custody==private/'catalog-custody' and re.fullmatch('[0-9a-f]{64}',catalog['rootDigest'])
for path in [run,certificate,custody]:identities[str(path)]=module.private_identity(path,directory=True)
for name in ['ca.crt','issuer.crt','issuer.key']:
 path=certificate/name;identities[str(path)]=module.private_identity(path,private_parent=certificate)
for name in ['owner.lock','owner.json']:
 path=custody/name;identities[str(path)]=module.private_identity(path)
owner=json.loads((custody/'owner.json').read_bytes());assert owner['CustodyDirectory']==str(custody)
capture={'identities':identities,'runDirectory':str(run),'certificateDirectory':str(certificate),'catalogDirectory':str(directory),'custodyDirectory':str(custody),'rootDigest':catalog['rootDigest'],'containers':[],'resources':{}}
module.validate_infrastructure_capture(capture,isolation,stopped=True)
report={'result':'owned-recovery-cleanup','observedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'cleanup':[{'resource':'exact-apphost','absenceVerified':True}],'ownedSidecarsAbsent':True,'exactSessionAbsent':True,'originalProcessIdentitiesAbsent':True,'configurationProcessCensusEmpty':True,'ownedContainerLabelCensusEmpty':True,'allFourCapturedImmutableOwnedContainersAbsent':True,'freshCanonicalReceiptIdentityProof':True,'catalogDirectorySdkDisposedAndAbsent':True,'activeHealthyInfrastructureCaptureNeverClaimed':True,'originalInfrastructureUnprovenAndExternalFalseReportPreserved':True,'noUnrelatedResourceAction':True,'currentReceiptSha256':{Path(p).name:v['sha256'] for p,v in identities.items() if Path(p).name in ['mtls-plane.json','catalog-plane.json']},'ownedContainerIdentitySha256':sorted(hashlib.sha256(x.encode()).hexdigest() for x in expected)}
module.cleanup_private_material(private,isolation,capture,True,report)
assert report.get('privateMaterialRemoved') and not report.get('recoveryMaterialRetained'),report
(packet/'recovery-cleanup.json').write_text(json.dumps(report,indent=2)+'\n')
print('Owned recovery verified; exact absence and receipt-bound private removal pass. Original startup/external failure preserved.')
