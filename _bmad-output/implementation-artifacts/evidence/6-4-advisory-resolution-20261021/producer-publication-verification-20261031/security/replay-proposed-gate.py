#!/usr/bin/env python3
"""Replay proposed gates with byte-bound inputs and a new output directory; never overwrite captures."""
import argparse, datetime, hashlib, json, os, pathlib, subprocess, sys
import yaml
p=argparse.ArgumentParser()
p.add_argument('--candidate-root',required=True)
p.add_argument('--capsule',required=True)
p.add_argument('--output-dir',required=True)
a=p.parse_args()
candidate=pathlib.Path(a.candidate_root).resolve()
capsule=pathlib.Path(a.capsule).resolve()
out=pathlib.Path(a.output_dir).resolve()
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
 assert len(matched)==1
 index,gate=matched[0]
 assert gate.get('continue-on-error',False) is False and 'if' not in gate
 assert gate['shell']=='bash' and gate['run']=='npm audit --audit-level=moderate'
 assert index>next(i for i,s in enumerate(steps) if s.get('run')=='npm ci --ignore-scripts')
 assert index>next(i for i,s in enumerate(steps) if s.get('run')=='npm audit signatures')
 if relative.startswith('Github/'):
  assert index<next(i for i,s in enumerate(steps) if s.get('run')=='npx --no-install --ignore-scripts semantic-release')
 gates.append((relative,gate['run']))
out.mkdir(parents=True,exist_ok=False)
(out/'home').mkdir()
env=os.environ.copy();env.update(HOME=str(out/'home'),PATH='/home/administrator/.nvm/versions/node/v24.21.0/bin:/usr/bin:/bin',NO_COLOR='1')
rows=[]
for relative,command in gates:
 name='workflow' if relative.startswith('.') else 'action'
 argv=['bash','-e','-o','pipefail','-c',command+'\nprintf "STORY64_RELEASE_CONTINUED\n"']
 started=datetime.datetime.now(datetime.timezone.utc).isoformat()
 result=subprocess.run(argv,cwd=capsule,env=env,capture_output=True)
 row={'candidate':relative,'argv':argv,'cwd':str(capsule),'publicEnvironment':{k:env[k] for k in ['HOME','PATH','NO_COLOR']},'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':result.returncode,'publicationContinuationReached':b'STORY64_RELEASE_CONTINUED' in result.stdout}
 for stream in ['stdout','stderr']:
  data=getattr(result,stream);path=out/(name+'.'+stream+'.log')
  with path.open('xb') as f:f.write(data)
  row[stream]={'file':path.name,'sha256':hashlib.sha256(data).hexdigest(),'sizeBytes':len(data)}
 rows.append(row)
report={'scope':'Nonpublishing future-gate replay; dependency remediation remains unresolved','inputHashes':expected_inputs,'candidateHashes':expected_yaml,'cases':rows,'passed':all(r['exitCode']==1 and not r['publicationContinuationReached'] for r in rows)}
with (out/'result.json').open('x') as f:json.dump(report,f,indent=2);f.write('\n')
print(json.dumps({'result':str(out/'result.json'),'passed':report['passed']},indent=2))
sys.exit(0 if report['passed'] else 1)
