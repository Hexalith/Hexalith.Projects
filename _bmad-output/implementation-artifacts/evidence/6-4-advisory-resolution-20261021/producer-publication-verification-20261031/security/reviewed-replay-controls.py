#!/usr/bin/env python3
"""Exercise reviewed replay under -O without modifying any retained capture/input."""
import pathlib, subprocess, tempfile, json, os, shutil, hashlib
here=pathlib.Path(__file__).resolve().parent
helper=here/'replay-proposed-gate-reviewed.py'
root=pathlib.Path(tempfile.mkdtemp(prefix='story64-reviewed-gate-controls-'))
original=pathlib.Path('/tmp/story64-producer-verification-1e388du9')
candidate=root/'candidate';capsule=root/'capsule';capsule.mkdir()
for relative in ['.github/workflows/build-release.yml','Github/create-release/action.yml']:
 dest=candidate/relative;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(original/'npm-gate-correction'/relative,dest)
for relative in ['package.json','package-lock.json']:shutil.copyfile(original/'npm-original'/relative,capsule/relative)
rows=[]
def run(name,output,expected_exit,extra_env=None,fragment=None):
 argv=['python3','-O',str(helper),'--candidate-root',str(candidate),'--capsule',str(capsule),'--output-dir',str(output)]
 env=os.environ.copy();env.update(extra_env or {})
 result=subprocess.run(argv,capture_output=True,env=env)
 if result.returncode!=expected_exit:raise SystemExit(name+' unexpected exit '+str(result.returncode)+' '+result.stderr.decode())
 if fragment and fragment not in result.stderr.decode():raise SystemExit(name+' missing expected refusal')
 row={'case':name,'argv':argv,'exitCode':result.returncode,'stdout':result.stdout.decode(),'stderr':result.stderr.decode()}
 if (output/'result.json').exists():
  report=json.loads((output/'result.json').read_text());row['replayPassed']=report['passed'];row['childExits']=[x['exitCode'] for x in report['cases']]
 rows.append(row)
 return row
valid=root/'valid-output';row=run('optimized-positive',valid,0)
if row.get('replayPassed') is not True:raise SystemExit('Positive replay did not accept blocking audit exits')
streams={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in valid.glob('*.log')}
run('optimized-overwrite-refusal',valid,1,fragment='Refusing to overwrite')
for path,digest in streams.items():
 if hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()!=digest:raise SystemExit('Existing replay output changed')
for name,path,fragment in [('yaml',candidate/'.github/workflows/build-release.yml','Candidate YAML digest differs'),('lock',capsule/'package-lock.json','Selected dependency input digest differs')]:
 old=path.read_bytes();path.write_bytes(old+b'\n')
 output=root/(name+'-rejected')
 run('optimized-'+name+'-tamper',output,1,fragment=fragment);path.write_bytes(old)
 if output.exists():raise SystemExit('Rejected input created an output directory')
for code in [0,42]:
 output=root/('child-'+str(code))
 row=run('optimized-unexpected-child-exit-'+str(code),output,1,{'BASH_FUNC_npm%%':'() { return '+str(code)+'; }'})
 if row.get('replayPassed') is not False or row.get('childExits')!=[code,code]:raise SystemExit('Child failure/success was incorrectly accepted')
report={'result':'passed','scope':'Six fresh-output and negative controls under python -O; process doubles are helper controls only','scratch':str(root),'helperSha256':hashlib.sha256(helper.read_bytes()).hexdigest(),'controls':rows}
print(json.dumps(report,indent=2))
