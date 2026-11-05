from pathlib import Path
import subprocess,json,hashlib,datetime
root=Path('/home/administrator/projects/hexalith/projects');packet=root/'_bmad-output/implementation-artifacts/evidence/story-6-1-platform-maintenance-20261105';dest=packet/'parent-review/source-changes';dest.mkdir(exist_ok=True);rows=[]
for name,repo in [('Projects',root),('EventStore',root/'references/Hexalith.EventStore'),('Tenants',root/'references/Hexalith.Tenants'),('Folders',root/'references/Hexalith.Folders')]:
 paths=['src','tests','tools','Props','docs','*.props','*.targets','*.slnx','global.json','nuget.config','AGENTS.md','.editorconfig','.gitattributes']
 patch=subprocess.check_output(['git','diff','--binary','HEAD','--',*paths],cwd=repo)
 untracked=[s for s in subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z','--',*paths],cwd=repo).decode().split('\0') if s]
 for path in sorted(untracked):
  r=subprocess.run(['git','diff','--no-index','--binary','--','/dev/null',path],cwd=repo,stdout=subprocess.PIPE,stderr=subprocess.PIPE);assert r.returncode==1;patch+=r.stdout
 f=dest/(name+'.patch');f.write_bytes(patch);r=subprocess.run(['git','apply','--reverse','--check',str(f)],cwd=repo,capture_output=True,text=True);assert r.returncode==0,(name,r.stderr)
 check=subprocess.run(['git','diff','--check'],cwd=repo,capture_output=True,text=True);assert check.returncode==0,(name,check.stdout,check.stderr)
 cleanIndex=subprocess.run(['git','diff','--cached','--quiet','HEAD'],cwd=repo);assert cleanIndex.returncode==0
 rows.append({'owner':name,'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo).decode().strip(),'patch':str(f.relative_to(packet)),'sha256':hashlib.sha256(patch).hexdigest(),'reverseApplyCheckExitCode':r.returncode,'diffCheckExitCode':check.returncode,'indexMatchesHEAD':True,'untrackedSource':untracked})
(dest/'patch-checks.json').write_text(json.dumps({'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'repositories':rows},indent=2)+'\n');print(json.dumps({'ownerPatches':len(rows),'newSourceFiles':sum(len(row['untrackedSource']) for row in rows),'allReverseChecksPassed':True,'allIndexesMatchHEAD':True}))
