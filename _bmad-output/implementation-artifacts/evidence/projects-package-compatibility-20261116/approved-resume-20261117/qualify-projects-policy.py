from pathlib import Path
import hashlib,json,os,shutil,subprocess,datetime
OUT=Path(__file__).resolve().parent;WORK=Path(os.environ['TMPDIR'])/'projects';ORIGINAL=Path('/home/administrator/projects/hexalith/projects')
def run(*args,cwd=WORK):
 v=subprocess.run(args,cwd=cwd,capture_output=True,timeout=180)
 if v.returncode:raise RuntimeError(v.stderr.decode(errors='replace'))
 return v.stdout
run('git','clone','--shared','--no-checkout',str(ORIGINAL),str(WORK),cwd=WORK.parent)
run('git','remote','set-url','origin','https://github.com/Hexalith/Hexalith.Projects.git');run('git','fetch','--no-tags','--no-recurse-submodules','origin','main');base=run('git','rev-parse','origin/main').decode().strip();assert base=='8b53d6b917957762949d39c21bc5de0919811413','Fresh source changed; requalify exact new inputs'
archive='/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/isolation-20261103/final-source-20261103/raw-changes/'
run('git','sparse-checkout','init','--no-cone');run('git','sparse-checkout','set','--no-cone','/src/','/tests/','/.github/workflows/','/docs/runbooks/','/.gitattributes','/.gitignore','/.editorconfig','/Directory.Build.props','/Directory.Packages.props','/Hexalith.Projects.CI.slnx','/release.config.cjs','/tools/release-packages.json',archive+'src/Hexalith.Projects.Workers/Hexalith.Projects.Workers.csproj',archive+'tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj');run('git','-c','submodule.recurse=false','checkout','--quiet','--detach',base)
paths=['tests/tools/run-ci-workflow-gates.ps1','tests/tools/test_run_ci_workflow_gates.py']
for p in paths:shutil.copyfile(OUT/'projects-policy-source'/p,WORK/p)
with (OUT/'projects-policy-qualification.log').open('wb') as log:
 v=subprocess.run(['python3','-B','-m','unittest','tests/tools/test_run_ci_workflow_gates.py','-v'],cwd=WORK,stdout=log,stderr=subprocess.STDOUT,timeout=180)
assert v.returncode==0,'Policy suite failed; retained full log'
with (OUT/'projects-policy-gate.log').open('wb') as log:
 v=subprocess.run(['pwsh','-NoLogo','-NoProfile','-NonInteractive','-File','tests/tools/run-ci-workflow-gates.ps1'],cwd=WORK,stdout=log,stderr=subprocess.STDOUT,timeout=60)
assert v.returncode==0,'Real policy gate failed; retained full log'
run('git','add','--',*paths);changed=run('git','diff','--cached','--name-only').decode().splitlines();assert set(changed)==set(paths)
patch=run('git','diff','--cached','--binary','--full-index');(OUT/'projects-policy.patch').write_bytes(patch)
proof={'base':base,'base_tree':run('git','rev-parse','HEAD^{tree}').decode().strip(),'result_tree':run('git','write-tree','--missing-ok').decode().strip(),'paths':paths,'patch_sha256':hashlib.sha256(patch).hexdigest(),'suite_exit':0,'gate_exit':0,'normal_full_gate_checks_preserved':True,'unrelated_pins_and_gitlinks_preserved':True,'recorded_at':datetime.datetime.now(datetime.timezone.utc).isoformat()}
(OUT/'projects-policy-qualification.json').write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps(proof))
