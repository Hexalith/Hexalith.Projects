from pathlib import Path
import copy,fcntl,hashlib,importlib.util,json,os,stat,sys
root=Path('/tmp/story61-isolated-20261103-r5tqvlm3/projects')
p=Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-59')
private=Path('/tmp/supported-project-reads-private-2fmxpf4s')
source=root/'tools/qualification/run_supported_project_reads.py'
spec=importlib.util.spec_from_file_location('story61_exact_recovery',source)
m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
original=p/'main/run.json';before_hash=hashlib.sha256(original.read_bytes()).hexdigest()
r=json.loads(original.read_text());capture=json.loads((private/'infrastructure-observed.json').read_text());isolation=json.loads((private/'mtls-plane.json').read_text())['isolationId']
lock=Path('/tmp/hexalith-timesheets-local-runtime.lock');fd=os.open(lock,os.O_RDWR|os.O_NOFOLLOW|os.O_CLOEXEC|os.O_NONBLOCK)
def identity(v):return (v.st_dev,v.st_ino,v.st_uid,stat.S_IMODE(v.st_mode),v.st_nlink,v.st_size,v.st_mtime_ns)
observed=os.fstat(fd);assert stat.S_ISREG(observed.st_mode) and observed.st_uid==os.geteuid() and observed.st_nlink==1 and stat.S_IMODE(observed.st_mode)==0o600 and identity(observed)==identity(lock.lstat())
fcntl.flock(fd,fcntl.LOCK_EX|fcntl.LOCK_NB)
report={'kind':'separate-exact-recovery-after-failed-qualification','failedReportSha256':before_hash,'failedReportUnchanged':False,'qualificationAccepted':False,'cleanup':copy.deepcopy([x for x in r['cleanup'] if x['resource']!='dapr-placement-mtls']),'ownedSidecarsAbsent':False}
try:
 before=m.preexisting_resources()
 session=r['ownedAppHostSession']
 assert all(m.process_start(session[a])!=session[b] for a,b in [('appHostPid','appHostStartTicks'),('cliPid','cliStartTicks')])
 report['ownedAppHostProcessIdentitiesAbsent']=True
 assert not m.exact_apphost_sessions();report['exactAppHostSessionsAbsent']=True
 assert all(m.process_start(v[a])!=v[b] for v in r['runtimeModes'].values() for a,b in [('pid','startTicks'),('wrapperPid','wrapperStartTicks')]);report['allOwnedRuntimeIdentitiesAbsent']=True
 assert not m.owned_sidecar_census(isolation);report['ownedSidecarsAbsent']=True
 m.validate_infrastructure_capture(capture,isolation,stopped=True);report['originalStoppedInfrastructureCaptureValid']=True
 sentry=[c for c in capture['containers'] if c['name']=='dapr-placement-mtls'];assert len(sentry)==1
 m.cleanup_resources(None,sentry,isolation,lambda command,**kwargs:m.execute(command,**kwargs),report)
 assert all(c.get('removed') and c.get('absenceVerified') for c in report['cleanup'])
 m.cleanup_private_material(private,isolation,capture,True,report)
 assert report['privateMaterialRemoved'] and not report.get('recoveryMaterialRetained')
 after=m.preexisting_resources()
 report['preexistingResourcesPreserved']=before==after
 assert report['preexistingResourcesPreserved']
 report['failedReportUnchanged']=hashlib.sha256(original.read_bytes()).hexdigest()==before_hash
 assert report['failedReportUnchanged']
 report['recoveryCompleted']=True
finally:
 report['lockIdentityAndMetadataPreserved']=identity(observed)==identity(lock.lstat())
 os.close(fd);report['admissionReleased']=True
 (p/'exact-recovery.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='cleanup'},indent=2))
