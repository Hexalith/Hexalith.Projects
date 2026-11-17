from pathlib import Path
import os,subprocess,json,io,sys,tarfile,hashlib,time
OUT=Path(__file__).resolve().parent;SCRATCH=Path(os.environ['TMPDIR']);WORK=SCRATCH/'cicd';OWNER=Path('/home/administrator/projects/hexalith/cicd');POLICY='a1e0c310b1f1a50d78e6132890fae6bcba769f6d'
env=os.environ.copy();env.pop('GH_TOKEN',None);env.pop('GITHUB_TOKEN',None);env['GH_CONFIG_DIR']='/home/administrator/.config/gh-cicd-agent'
def run(*args,cwd=WORK):
 v=subprocess.run(args,cwd=cwd,env=env,capture_output=True,timeout=240)
 if v.returncode:raise RuntimeError(v.stderr.decode(errors='replace'))
 return v.stdout
run('git','clone','--shared','--no-checkout',str(OWNER),str(WORK),cwd=SCRATCH);run('git','remote','set-url','origin','https://github.com/Hexalith/Hexalith.CiCd.git');run('git','fetch','--no-tags','--no-recurse-submodules','origin','main');base=run('git','rev-parse','origin/main').decode().strip();run('git','config','core.autocrlf','false');run('git','sparse-checkout','init','--no-cone');run('git','sparse-checkout','set','--no-cone','/tools/','/templates/','/setup/','/docs/','/.github/workflows/','/AGENTS.md','/.gitmodules');run('git','-c','submodule.recurse=false','checkout','--quiet','--detach',base)
raw=run('git','archive',POLICY,'Github/guard','Github/lib',cwd=OWNER/'references/Hexalith.Builds');trusted=SCRATCH/'trusted-builds';trusted.mkdir()
with tarfile.open(fileobj=io.BytesIO(raw)) as a:a.extractall(trusted,filter='data')
sys.path.insert(0,str(trusted/'Github/lib'))
from guard_tools import Tools
t=SCRATCH/'guard-tools';t.mkdir();tools=Tools(t)
messages=['build(cicd): dispatch approved contracts prerequisites\n','build(cicd): prepare protected EventStore proposal\n','build(cicd): qualify Projects prerequisite policy\n','build(cicd): deliver approved contracts prerequisite projects-prerequisite-policy\n','build(cicd): propose approved Contracts prerequisite publisher\n']
for i,m in enumerate(messages):tools.lint_message(m,'approved fixed package owner action');(OUT/f'owner-continuation-message-{i}.txt').write_text(m)
state={'owner_base':base,'work':str(WORK),'validator_source':POLICY,'validator_archive_sha256':hashlib.sha256(raw).hexdigest(),'commitlint_exit':0,'messages':messages};(OUT/'active-owner-source-workspace.json').write_text(json.dumps(state,indent=2)+'\n');print(json.dumps(state),flush=True)
deadline=time.monotonic()+7200
while not (OUT/'active-owner-source-workspace-finished').exists():
 if time.monotonic()>deadline:raise TimeoutError('Active private workspace lifetime exceeded')
 time.sleep(1)
print('Active private source workspace released',flush=True)
