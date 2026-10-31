#!/usr/bin/env python3
"""Verify each proposed YAML gate structurally and with the actual vulnerable locked tree."""
import pathlib, json, subprocess, os, yaml, hashlib, datetime
root=pathlib.Path('/tmp/story64-producer-verification-1e388du9')
rows=[]
for relative in ['.github/workflows/build-release.yml','Github/create-release/action.yml']:
 document=yaml.safe_load((root/'npm-gate-correction'/relative).read_text())
 steps=document['jobs']['release']['steps'] if 'jobs' in document else document['runs']['steps']
 guards=[(i,s) for i,s in enumerate(steps) if s.get('name')=='Verify npm vulnerability audit']
 assert len(guards)==1
 index,gate=guards[0]
 assert gate.get('continue-on-error',False) is False
 assert gate['run']=='npm audit --audit-level=moderate'
 assert index > next(i for i,s in enumerate(steps) if s.get('run')=='npm ci --ignore-scripts')
 assert index > next(i for i,s in enumerate(steps) if s.get('run')=='npm audit signatures')
 if relative.startswith('Github/'):
  assert index < next(i for i,s in enumerate(steps) if s.get('run')=='npx --no-install --ignore-scripts semantic-release')
 env=os.environ.copy();env.update(HOME=str(root/'home'),PATH='/home/administrator/.nvm/versions/node/v24.21.0/bin:/usr/bin:/bin',NO_COLOR='1')
 argv=['bash','-e','-o','pipefail','-c',gate['run']+'\nprintf "STORY64_RELEASE_CONTINUED\n"']
 start=datetime.datetime.now(datetime.timezone.utc).isoformat()
 result=subprocess.run(argv,cwd=root/'npm-original',env=env,capture_output=True)
 assert result.returncode==1
 assert b'STORY64_RELEASE_CONTINUED' not in result.stdout
 name='workflow' if relative.startswith('.') else 'action'
 here=pathlib.Path(__file__).resolve().parent
 for stream in ['stdout','stderr']:(here/(name+'-audit-gate.'+stream+'.log')).write_bytes(getattr(result,stream))
 rows.append({'file':relative,'argv':argv,'cwd':str(root/'npm-original'),'startedUtc':start,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':result.returncode,'publicationContinuationReached':False,'run':gate['run'],'stdoutSha256':hashlib.sha256(result.stdout).hexdigest(),'stderrSha256':hashlib.sha256(result.stderr).hexdigest()})
print(json.dumps({'scope':'Proposed future-source gate only; unresolved vulnerabilities remain on disk and published source is unchanged','cases':rows},indent=2))
