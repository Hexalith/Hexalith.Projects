from pathlib import Path
import json,hashlib,stat,sys,os
co=json.loads(Path(sys.argv[1]).read_text());pid=co['launcherPid'];candidates=[]
for child in Path(f'/proc/{pid}/task/{pid}/children').read_text().split():
 env=dict(x.split(b'=',1) for x in Path('/proc',child,'environ').read_bytes().split(b'\0') if b'=' in x)
 if env.get(b'SUPPORTED_PROJECT_READ_EVIDENCE')==co['evidence'].encode() and b'SUPPORTED_PROJECT_READ_CONFIG'in env:candidates.append(Path(env[b'SUPPORTED_PROJECT_READ_CONFIG'].decode()).parent)
assert len(candidates)==1
p=candidates[0];assert p.stat().st_uid==os.getuid() and stat.S_IMODE(p.stat().st_mode)==0o700
namespace=json.loads((p/'fixture.json').read_text())['Projects']['SupportedReads']['IsolationId'];rows=[]
for entry in Path('/proc').iterdir():
 if not entry.name.isdigit():continue
 try:
  env=dict(x.split(b'=',1) for x in (entry/'environ').read_bytes().split(b'\0') if b'=' in x)
  if env.get(b'NAMESPACE')!=namespace.encode():continue
  app=env.get(b'EventStore__DomainService__AppId',b'').decode();flag=env.get(b'EventStore__RouteCatalog__ReuseHeldMetadataDispatch',b'').decode()
  if app not in ['eventstore','tenants','projects','projects-workers','folders','folders-workers']:continue
  executable=(entry/'exe').readlink()
  if not executable.name.startswith('Hexalith.'):continue
  assert env[b'XDG_DATA_HOME']==str(p/('data-'+app)).encode()
  rows.append({'app':app,'reuseHeldMetadataDispatch':flag,'environment':env.get(b'ASPNETCORE_ENVIRONMENT',b'').decode(),'binarySha256':hashlib.sha256(executable.read_bytes()).hexdigest(),'assemblySha256':hashlib.sha256(Path(str(executable)+'.dll').read_bytes()).hexdigest()})
 except (FileNotFoundError,PermissionError,ProcessLookupError):pass
rows.sort(key=lambda x:x['app']);assert len(rows)==6 and len({x['app'] for x in rows})==6 and all(x['reuseHeldMetadataDispatch']==('true' if x['app']=='folders' else 'false') and x['environment']=='Development' for x in rows)
(Path(co['evidence'])/'runtime-metadata-selection.json').write_text(json.dumps({'exactOwnedNamespaceAndDataDirectoryMatched':True,'observedNativeExecutableEnvironment':rows},indent=2)+'\n');print('Actual finite runtime environment selection verified for six owned peers.')
