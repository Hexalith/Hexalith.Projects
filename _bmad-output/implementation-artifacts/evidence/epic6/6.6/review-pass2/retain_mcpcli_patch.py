"""Retain the complete McpCli working tree without staging the active repository."""
import hashlib,json,subprocess
from pathlib import Path
root=Path.cwd();repo=root/'references/Hexalith.McpCli';out=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2';base='23072f04f423eea3f30456fe5af13d2d382624fe'
patch=subprocess.check_output(['git','diff','--binary','--full-index',base,'--'],cwd=repo);untracked=[n.decode() for n in subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z'],cwd=repo).split(b'\0') if n]
for name in untracked:
    r=subprocess.run(['git','diff','--no-index','--binary','--full-index','--','/dev/null',name],cwd=repo,capture_output=True);assert r.returncode==1;patch+=r.stdout
path=out/'mcpcli-working-tree-over-23072f04.patch';path.write_bytes(patch);digest=hashlib.sha256(patch).hexdigest();(out/(path.name+'.sha256')).write_text(digest+'  '+path.name+'\n')
files=[]
for name in subprocess.check_output(['git','diff','--name-only',base,'--'],cwd=repo).decode().splitlines()+untracked:
    p=repo/name
    if p.is_file():files.append(dict(path=name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
(out/'mcpcli-patch.json').write_text(json.dumps(dict(baseline=base,observedHead=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo).decode().strip(),patch=path.name,sha256=digest,bytes=len(patch),untracked=untracked,files=files),indent=2)+'\n')
p=out/'snapshot-source-manifest.json';snapshot=json.loads(p.read_text());changes=[]
for row in files:
    name='references/Hexalith.McpCli/'+row['path'];before=snapshot['files'].get(name)
    if before!=row['sha256']:changes.append(dict(path=name,beforeSha256=before,afterSha256=row['sha256']))
    snapshot['files'][name]=row['sha256']
snapshot['postNativeVerificationRefresh']={'scope':'Added complete McpCli patch/config hashes and final generic Python test-oracle fixes; production C# and Contracts .3 bytes remain unchanged.','files':changes};snapshot['files']=dict(sorted(snapshot['files'].items()));p.write_text(json.dumps(snapshot,indent=2)+'\n');print(json.dumps({'patchSha256':digest,'files':len(files),'bytes':len(patch)},indent=2))
