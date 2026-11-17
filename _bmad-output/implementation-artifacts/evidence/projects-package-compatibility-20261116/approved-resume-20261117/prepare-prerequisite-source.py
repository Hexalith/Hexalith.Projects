"""Hold an isolated fresh CiCd workspace for reviewed prerequisite sources."""
from pathlib import Path
import hashlib,io,json,os,shutil,subprocess,sys,tarfile,time
OUT=Path(__file__).resolve().parent
ORIGINAL=Path('/home/administrator/projects/hexalith/cicd')
SCRATCH=Path(os.environ['TMPDIR']);WORK=SCRATCH/'cicd';SOURCE='a1e0c310b1f1a50d78e6132890fae6bcba769f6d'
env=os.environ.copy();env.pop('GH_TOKEN',None);env.pop('GITHUB_TOKEN',None);env['GH_CONFIG_DIR']='/home/administrator/.config/gh-cicd-agent'
def run(*args,cwd=WORK):
 v=subprocess.run(args,cwd=cwd,env=env,capture_output=True,timeout=240)
 if v.returncode:raise RuntimeError(v.stderr.decode(errors='replace'))
 return v.stdout
run('git','clone','--shared','--no-checkout',str(ORIGINAL),str(WORK),cwd=SCRATCH)
run('git','remote','set-url','origin','https://github.com/Hexalith/Hexalith.CiCd.git')
run('git','fetch','--no-tags','--no-recurse-submodules','origin','main')
base=run('git','rev-parse','origin/main').decode().strip()
run('git','config','core.autocrlf','false')
run('git','sparse-checkout','init','--no-cone')
run('git','sparse-checkout','set','--no-cone','tools','templates','setup','docs','.github/workflows','/AGENTS.md','/.gitmodules')
run('git','-c','submodule.recurse=false','checkout','--quiet','--detach',base)
paths=[]
for src in (OUT/'publisher-source').rglob('*'):
 if src.is_file():
  rel=src.relative_to(OUT/'publisher-source');dst=WORK/rel
  if dst.exists():raise FileExistsError('Refuse existing owner source '+str(rel))
  dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dst);paths.append(str(rel))
raw=run('git','archive',SOURCE,'Github/guard','Github/lib',cwd=ORIGINAL/'references/Hexalith.Builds');trusted=SCRATCH/'trusted-builds';trusted.mkdir()
with tarfile.open(fileobj=io.BytesIO(raw)) as a:a.extractall(trusted,filter='data')
sys.path.insert(0,str(trusted/'Github/lib'))
from guard_tools import Tools
toolsdir=SCRATCH/'guard-tools';toolsdir.mkdir();tools=Tools(toolsdir)
messages=['build(cicd): prepare approved contracts prerequisites\n','build(cicd): deliver approved contracts prerequisites\n']
messages+=['build(cicd): deliver approved contracts prerequisite '+p+'\n' for p in ['eventstore-publisher','projects-publisher','projects-pins']]
for i,m in enumerate(messages):
 tools.lint_message(m,'fixed Contracts prerequisite');(OUT/('prerequisite-message-'+str(i)+'.txt')).write_text(m)
binding={'owner_base':base,'work':str(WORK),'new_paths':sorted(paths),'validator_source':SOURCE,'validator_release':'v6.1.0','validator_archive_sha256':hashlib.sha256(raw).hexdigest(),'commitlint_exit':0,'messages':messages}
(OUT/'prerequisite-source-workspace.json').write_text(json.dumps(binding,indent=2)+'\n');print(json.dumps(binding),flush=True)
deadline=time.monotonic()+3600
while not (OUT/'prerequisite-source-workspace-finished').exists():
 if time.monotonic()>deadline:raise TimeoutError('Private source workspace lifetime exceeded')
 time.sleep(1)
print('Private source workspace released',flush=True)
