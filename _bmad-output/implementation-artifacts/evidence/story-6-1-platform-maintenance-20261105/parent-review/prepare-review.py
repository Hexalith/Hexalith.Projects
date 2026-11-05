from pathlib import Path
import subprocess,tempfile,json,hashlib,datetime
root=Path('/home/administrator/projects/hexalith/projects');baseline='0047c961ccad19f1a7344095ecb70bcd8e4bb65b';dest=Path(tempfile.mkstemp(prefix='story61-platform-review-',suffix='.diff')[1]); records=[]
def run(args,cwd):
 r=subprocess.run(args,cwd=cwd,stdout=subprocess.PIPE,stderr=subprocess.PIPE);assert r.returncode in (0,1),(args,r.returncode,r.stderr[:300]);return r.stdout
repos=[('.',baseline),('references/Hexalith.EventStore','HEAD'),('references/Hexalith.Tenants','HEAD'),('references/Hexalith.Folders','HEAD')]
chunks=[]; evidence=[]
for rel,base in repos:
 repo=root/rel;index=run(['git','rev-parse','--git-path','index'],repo).decode().strip();ip=Path(index);ip=ip if ip.is_absolute() else repo/ip; ih=hashlib.sha256(ip.read_bytes()).hexdigest(); prefix='' if rel=='.' else rel+'/'
 args=['git','diff','--no-ext-diff','--binary','--submodule=short','--src-prefix=a/'+prefix,'--dst-prefix=b/'+prefix,base]
 chunks.append(run(args,repo)); untracked=[s for s in run(['git','ls-files','--others','--exclude-standard','-z'],repo).decode().split('\0') if s]
 for path in sorted(untracked):
  data=run(['git','diff','--no-ext-diff','--no-index','--binary','--','/dev/null',path],repo)
  if prefix:data=data.replace(('a/'+path).encode(),('a/'+prefix+path).encode()).replace(('b/'+path).encode(),('b/'+prefix+path).encode())
  (evidence if path.startswith('_bmad-output/') else chunks).append(data)
 records.append({'repository':rel,'base':base,'head':run(['git','rev-parse','HEAD'],repo).decode().strip(),'untrackedFiles':len(untracked),'indexSha256':ih})
 assert hashlib.sha256(ip.read_bytes()).hexdigest()==ih
with dest.open('wb') as f:
 for part in chunks+evidence:f.write(part)
meta={'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'path':str(dest),'bytes':dest.stat().st_size,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'baselineCommit':baseline,'repositories':records,'untrackedIncluded':True,'indexMutations':False,'externalAdvancedRevisionsRepresentedByRootGitlinks':True}
dest.with_suffix('.json').write_text(json.dumps(meta,indent=2)+'\n');Path('/tmp/story61-platform-review-path.txt').write_text(str(dest)+'\n');print(json.dumps(meta))
