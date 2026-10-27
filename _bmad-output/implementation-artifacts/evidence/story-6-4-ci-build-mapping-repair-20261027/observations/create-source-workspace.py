from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
original=Path('/home/administrator/projects/hexalith/projects')
evidence=Path('/tmp/story64-ci-repair-20261027')
workspace=evidence/'source-workspace'; workspace.mkdir(exist_ok=True)
commands=[]; snapshots=[]
def run(args,cwd):
 started=time.time(); result=subprocess.run(args,cwd=cwd,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
 commands.append(dict(command=args,cwd=str(cwd),exit=result.returncode,seconds=round(time.time()-started,3)))
 with (evidence/'source-workspace-creation.log').open('a') as log: log.write('$ '+repr(args)+'\n'+result.stdout+'\n')
 (evidence/'source-workspace-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
 if result.returncode: raise SystemExit(result.stdout)
 return result.stdout.rstrip("\n")
run(['git','init','-b','main'],workspace)
run(['git','config','user.name','CI Repair Fixture'],workspace)
run(['git','config','user.email','fixture@example.invalid'],workspace)
run(['git','config','submodule.recurse','false'],workspace)
paths=run(['git','config','--file',str(original/'.gitmodules'),'--get-regexp','^submodule\\..*\\.path$'],original)
for row in paths.splitlines():
 relative=row.split(' ',1)[1]; source=original/relative; destination=workspace/relative
 head=run(['git','rev-parse','HEAD'],source)
 if not destination.exists(): run(['git','-c','protocol.file.allow=always','submodule','add','--force',str(source),relative],workspace)
 run(['git','checkout','--detach',head],destination)
 changes=run(['git','status','--porcelain','--untracked-files=all'],source)
 copied=[]
 for line in changes.splitlines():
  # Current changes are modifications/new files only; refuse unknown forms.
  status=line[:2]; name=line[3:]
  if status not in (' M','??','M '): raise SystemExit('Unexpected source state '+line)
  path=source/name
  if not path.is_file(): raise SystemExit('Unexpected non-file '+name)
  target=destination/name; target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(path,target)
  copied.append(dict(path=name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 snapshots.append(dict(path=relative,head=head,copied_working_files=copied))
run(['git','add','.gitmodules'],workspace)
run(['git','commit','-m','test: record direct source fixture identities'],workspace)
(evidence/'source-workspace-snapshots.json').write_text(json.dumps(snapshots,indent=2)+'\n')
print(workspace)
