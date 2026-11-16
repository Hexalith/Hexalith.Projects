"""Reproduce the complete retained McpCli patch in a disposable baseline clone."""
from pathlib import Path
import hashlib,json,os,subprocess
root=Path.cwd();out=root/'_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2';repo=root/'references/Hexalith.McpCli';meta=json.loads((out/'mcpcli-patch.json').read_text());tmp=Path(os.environ['TMPDIR'])/'mcpcli';base=meta['baseline'];patch=out/meta['patch'];commands=[]
def run(args):
    r=subprocess.run(args,cwd=tmp if tmp.exists() else root,capture_output=True);commands.append(dict(command=args,exitCode=r.returncode,stdout=r.stdout.decode(),stderr=r.stderr.decode()));assert r.returncode==0,r.stderr.decode()
run(['git','clone','--shared','--no-checkout','--no-recurse-submodules',str(repo),str(tmp)]);run(['git','checkout','--detach',base]);run(['git','apply','--check','--index',str(patch)]);run(['git','apply','--index',str(patch)])
for row in meta['files']:assert hashlib.sha256((tmp/row['path']).read_bytes()).hexdigest()==row['sha256'],row['path']
(out/'patch-reproduction.json').write_text(json.dumps(dict(result='pass',baseline=base,patchSha256=meta['sha256'],verifiedFiles=len(meta['files']),commands=commands,scope='Private clone/index only; real .gitattributes checkout conversion retained; no active Git state was changed.'),indent=2)+'\n');print('Patch reproduced',len(meta['files']),'final files byte for byte.')
