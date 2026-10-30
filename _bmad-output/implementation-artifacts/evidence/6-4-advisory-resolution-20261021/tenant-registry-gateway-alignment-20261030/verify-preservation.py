"""Check original bytes and Git identities without changing any index/ref."""
from pathlib import Path
import datetime,hashlib,json,subprocess
ROOT=Path('/home/administrator/projects/hexalith/projects');E=Path(__file__).resolve().parent
before=json.loads(Path('/tmp/story64-tenant-registry-alignment-20261030/before.json').read_bytes())
sha=lambda data:hashlib.sha256(data).hexdigest()
allowed={'references/Hexalith.Platform/src/Hexalith.Platform.TenantRegistry.Host/Directory.Packages.props','_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md','_bmad-output/implementation-artifacts/sprint-status.yaml'}
differences=[]
for name,digest in before['files'].items():
 if name in allowed:continue
 path=ROOT/name
 if not path.is_file() or sha(path.read_bytes())!=digest:differences.append(name)
identities=[]
for name,state in before['repositories'].items():
 repo=ROOT/name
 def git(*argv):return subprocess.check_output(['git','-C',str(repo),*argv])
 index=Path(git('rev-parse','--path-format=absolute','--git-path','index').decode().strip())
 actual={'head':git('rev-parse','HEAD').decode().strip(),'indexSha256':sha(index.read_bytes()),'refs':git('show-ref').decode()}
 identities.append({'repository':name,**{key:actual[key]==state[key] for key in actual}})
nested=[str(p.relative_to(ROOT)) for name in before['repositories'] if name!='.' for p in (ROOT/name/'references').glob('*/.git')]
spec=ROOT/'_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md';original=(E/(spec.name+'.before')).read_bytes();current=spec.read_bytes()
tracking=ROOT/'_bmad-output/implementation-artifacts/sprint-status.yaml';old=(E/(tracking.name+'.before')).read_bytes().splitlines(keepends=True);new=tracking.read_bytes().splitlines(keepends=True)
changed=[i for i,(a,b) in enumerate(zip(old,new)) if a!=b]
owner=ROOT/'references/Hexalith.Platform/src/Hexalith.Platform.TenantRegistry.Host/Directory.Packages.props'
newfiles=[]
for name in before['repositories']:
 repo=ROOT/name
 for raw in subprocess.check_output(['git','-C',str(repo),'ls-files','-z','--others','--exclude-standard']).split(b'\0'):
  if not raw:continue
  path=repo/raw.decode();relative=str(path.relative_to(ROOT))
  if path.is_file() and relative not in before['files'] and not path.is_relative_to(E):newfiles.append(relative)
result={'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'originalFilesChecked':len(before['files'])-len(allowed),'unexpectedOriginalFileDifferences':differences,'unexpectedNewFiles':newfiles,'repositoryIdentities':identities,'nestedGitEntriesUnchanged':nested==before['nestedGitEntries'],'completeOriginalSpecPrefixPreserved':current.startswith(original),'frozenBlockPreserved':current[current.index(b'<frozen-after-approval'):current.index(b'</frozen-after-approval>')+len(b'</frozen-after-approval>')]==original[original.index(b'<frozen-after-approval'):original.index(b'</frozen-after-approval>')+len(b'</frozen-after-approval>')],'onlyStory64TrackingCommentChanged':len(old)==len(new) and len(changed)==1 and new[changed[0]].startswith(b'  6-4-resolve-projects-with-transient-current-explanations: in-progress #'),'approvedOwnerFileSha256':sha(owner.read_bytes()),'approvedOwnerFileMatchesProposal':sha(owner.read_bytes())=='af07af77a7ca12ad9120686d89e878e3701754e34ac4c40d3cb931507a152c23'}
(E/'preservation.json').write_text(json.dumps(result,indent=2)+'\n')
assert not differences and not newfiles and all(all(item[key] for key in ['head','indexSha256','refs']) for item in identities)
assert all(result[key] for key in ['nestedGitEntriesUnchanged','completeOriginalSpecPrefixPreserved','frozenBlockPreserved','onlyStory64TrackingCommentChanged','approvedOwnerFileMatchesProposal'])
print(json.dumps({k:v for k,v in result.items() if k!='repositoryIdentities'},indent=2))
