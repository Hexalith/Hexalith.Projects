#!/usr/bin/env python3
"""Reviewed replay: byte-bound inputs, fresh outputs and explicit fail-closed checks under python -O."""
import argparse, datetime, hashlib, json, os, pathlib, subprocess, sys, shutil, shlex
import yaml
p=argparse.ArgumentParser()
p.add_argument('--candidate-root',required=True)
p.add_argument('--capsule',required=True)
p.add_argument('--output-dir',required=True)
p.add_argument('--node-executable',default='node')
p.add_argument('--npm-cli',default='npm')
a=p.parse_args()
candidate=pathlib.Path(a.candidate_root).resolve()
capsule=pathlib.Path(a.capsule).resolve()
out=pathlib.Path(a.output_dir).resolve()
def require(condition,message):
 if not condition:raise SystemExit(message)

expected_yaml={'.github/workflows/build-release.yml': '378275b2944d2aa6bc297229314083d751af63d47057b509599a3ed1d4ce9f1e', 'Github/create-release/action.yml': 'fd95be3c120f9594b25d69ac233240052a9da3de10ad4f56e8f07a6d5a371edb'}
expected_inputs={'package.json':'6b24b99692d3ea18a3554e6018a271b951bd5a4c83db5895e1a83a98218d61a9','package-lock.json':'1ff1ac5a67d88d1c16eb10ac4b861e989d5c9c629f73a461ccc151c1f7e87724'}
if out.exists():sys.exit('Refusing to overwrite existing replay output directory: '+str(out))
for relative,digest in expected_yaml.items():
 if hashlib.sha256((candidate/relative).read_bytes()).hexdigest()!=digest:sys.exit('Candidate YAML digest differs: '+relative)
for relative,digest in expected_inputs.items():
 if hashlib.sha256((capsule/relative).read_bytes()).hexdigest()!=digest:sys.exit('Selected dependency input digest differs: '+relative)
# Validate both candidates before creating output or starting a command.
gates=[]
for relative in expected_yaml:
 doc=yaml.safe_load((candidate/relative).read_text())
 steps=doc['jobs']['release']['steps'] if 'jobs' in doc else doc['runs']['steps']
 matched=[(i,s) for i,s in enumerate(steps) if s.get('name')=='Verify npm vulnerability audit']
 require(len(matched)==1,'Expected exactly one proposed audit gate')
 index,gate=matched[0]
 require(gate.get('continue-on-error',False) is False and 'if' not in gate,'Proposed audit gate must always execute and fail closed')
 require(gate['shell']=='bash' and gate['run']=='npm audit --audit-level=moderate','Unexpected proposed audit shell or command')
 require(index>next(i for i,s in enumerate(steps) if s.get('run')=='npm ci --ignore-scripts'),'Audit gate must follow locked installation')
 require(index>next(i for i,s in enumerate(steps) if s.get('run')=='npm audit signatures'),'Audit gate must follow signature verification')
 if relative.startswith('Github/'):
  require(index<next(i for i,s in enumerate(steps) if s.get('run')=='npx --no-install --ignore-scripts semantic-release'),'Audit gate must precede release lifecycle')
 gates.append((relative,gate['run']))
node_executable=shutil.which(a.node_executable)
npm_executable=shutil.which(a.npm_cli)
require(node_executable is not None,'Node executable is unavailable')
require(npm_executable is not None,'npm CLI is unavailable')
npm_cli=str(pathlib.Path(npm_executable).resolve())
node_version=subprocess.run([node_executable,'--version'],capture_output=True,text=True,check=True).stdout.strip()
npm_version=subprocess.run([node_executable,npm_cli,'--version'],capture_output=True,text=True,check=True).stdout.strip()
parts=node_version.lstrip('v').split('.')
require((int(parts[0])>24 or (int(parts[0])==24 and int(parts[1])>=10)),'Replay requires Node >=24.10')
require(npm_version.split('.')[0]=='11','Replay requires an explicitly supplied npm11 frontend')
toolchain={'nodeExecutable':node_executable,'nodeVersion':node_version,'npmCli':npm_cli,'npmVersion':npm_version}
out.mkdir(parents=True,exist_ok=False)
(out/'home').mkdir()
env=os.environ.copy();env.update(HOME=str(out/'home'),NO_COLOR='1')
rows=[]
for relative,command in gates:
 name='workflow' if relative.startswith('.') else 'action'
 argv=['bash','-e','-o','pipefail','-c',shlex.quote(node_executable)+' '+shlex.quote(npm_cli)+' '+command.removeprefix('npm ')+'\nprintf "STORY64_RELEASE_CONTINUED\n"']
 started=datetime.datetime.now(datetime.timezone.utc).isoformat()
 result=subprocess.run(argv,cwd=capsule,env=env,capture_output=True)
 row={'candidate':relative,'argv':argv,'cwd':str(capsule),'publicEnvironment':{k:env[k] for k in ['HOME','PATH','NO_COLOR']},'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':result.returncode,'publicationContinuationReached':b'STORY64_RELEASE_CONTINUED' in result.stdout}
 for stream in ['stdout','stderr']:
  data=getattr(result,stream);path=out/(name+'.'+stream+'.log')
  with path.open('xb') as f:f.write(data)
  row[stream]={'file':path.name,'sha256':hashlib.sha256(data).hexdigest(),'sizeBytes':len(data)}
 rows.append(row)
report={'scope':'Nonpublishing future-gate replay; dependency remediation remains unresolved','inputHashes':expected_inputs,'candidateHashes':expected_yaml,'toolchain':toolchain,'cases':rows,'passed':all(r['exitCode']==1 and not r['publicationContinuationReached'] for r in rows)}
with (out/'result.json').open('x') as f:json.dump(report,f,indent=2);f.write('\n')
print(json.dumps({'result':str(out/'result.json'),'passed':report['passed']},indent=2))
sys.exit(0 if report['passed'] else 1)
