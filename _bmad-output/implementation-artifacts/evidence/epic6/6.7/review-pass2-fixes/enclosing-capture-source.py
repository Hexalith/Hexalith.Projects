"""Bind final source bytes to the private verification copy without modifying either Git checkout."""
import argparse, hashlib, json, subprocess
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--copied-root',required=True);parser.add_argument('--output',default='source-manifest.json');args=parser.parse_args()
root=Path.cwd();copied=Path(args.copied_root);out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def git(repo,*args):return subprocess.check_output(['git',*args],cwd=repo,text=True).strip()
base=['.editorconfig','.gitattributes','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json','.gitmodules','NuGet.Config','NuGet.config']
repos=['.']+[str(p.relative_to(root)) for p in (root/'references').iterdir() if (p/'.git').exists()]
manifest={'qualification':'Debug source/project-reference verification only; no deployed or package-mode acceptance.',
          'baselineCommit':'c6541a305a420078b16dceaba47f36468022850b','sourceCopyParity':True,
          'omittedHistoricalDocumentationDirectories':['artifacts','validation','bin','obj','node_modules','nupkgs'],'repositories':[]}
count=0
for rel in sorted(repos):
 original=root/rel;private=copied/rel
 tracked=git(original,'ls-files','--','src','tests','docs','module/hexalith.module-manifest.v2.json').splitlines()
 if rel=='.':tracked+=git(original,'ls-files','--others','--exclude-standard','--','src','tests','docs').splitlines()
 names=set(tracked)|{n for n in base if (original/n).is_file()}
 entries=[]
 for name in sorted(names):
  if name.startswith("docs/") and any(part in manifest["omittedHistoricalDocumentationDirectories"] for part in Path(name).parts):continue
  p=original/name;q=private/name
  if not p.is_file():
   assert not q.exists(),str(q)
   continue
  if p.is_symlink():continue
  assert q.is_file() and p.read_bytes()==q.read_bytes(),f'Private source differs: {rel}/{name}'
  entries.append({'path':name,'sha256':sha(p),'bytes':p.stat().st_size});count+=1
 deleted=git(original,'diff','--name-only','--diff-filter=D','--','src','tests').splitlines()
 manifest['repositories'].append({'path':rel,'revision':git(original,'rev-parse','HEAD'),'files':entries,'deletedFiles':deleted})
spec=root/'_bmad-output/implementation-artifacts/spec-6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback.md'
s=spec.read_text();frozen=s.split('<frozen-after-approval',1)[1].split('</frozen-after-approval>',1)[0]
manifest['approvedFrozenIntentSha256']=hashlib.sha256(frozen.encode()).hexdigest()
manifest['context']=[{'path':str(p.relative_to(root)),'sha256':sha(p)} for p in [root/'_bmad-output/implementation-artifacts/epic-6-context.md',root/'_bmad-output/planning-artifacts/story-6-7-approved-read-cutover-amendments-2026-11-16.md']]
target=out/args.output;assert not target.exists();target.write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'sourceFilesCompared':count,'repositories':len(manifest['repositories']),'manifestSha256':sha(target),'sourceCopyParity':True}))
