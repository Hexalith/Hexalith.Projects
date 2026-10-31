#!/usr/bin/env python3
"""Record exact nonpublishing argv, cwd, allowlisted public environment and raw streams."""
import argparse, datetime, hashlib, json, os, pathlib, subprocess, sys, time
p=argparse.ArgumentParser()
p.add_argument('--output',required=True)
p.add_argument('--cwd',required=True)
p.add_argument('--env',action='append',default=[])
p.add_argument('argv',nargs=argparse.REMAINDER)
a=p.parse_args()
argv=a.argv[1:] if a.argv[:1]==['--'] else a.argv
out=pathlib.Path(a.output);out.parent.mkdir(parents=True,exist_ok=True)
if out.with_suffix('.command.json').exists():raise SystemExit('Refusing to overwrite command receipt')
env=os.environ.copy()
for v in a.env:
 k,val=v.split('=',1);env[k]=val
public_names=['HOME','PATH','DOTNET_ROOT','DOTNET_CLI_HOME','DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_NOLOGO','NUGET_PACKAGES','NUGET_XMLDOC_MODE','CI','GITHUB_ACTIONS','GITHUB_REF','GITHUB_SHA','REPOSITORY','DISPATCH_REF','DISPATCH_SHA','NO_COLOR','FORCE_COLOR','NODE_OPTIONS','NODE_PATH']
public={k:env[k] for k in public_names if k in env}
start=datetime.datetime.now(datetime.timezone.utc).isoformat();clock=time.monotonic()
with out.with_suffix('.stdout.log').open('wb') as stdout,out.with_suffix('.stderr.log').open('wb') as stderr:
 proc=subprocess.run(argv,cwd=a.cwd,env=env,stdout=stdout,stderr=stderr,check=False)
receipt={'argv':argv,'cwd':a.cwd,'environment':public,'explicitEnvironment':dict(v.split('=',1) for v in a.env),'startedUtc':start,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'elapsedSeconds':round(time.monotonic()-clock,3),'exitCode':proc.returncode}
for stream in ['stdout','stderr']:
 path=out.with_suffix('.'+stream+'.log');b=path.read_bytes();receipt[stream]={'file':path.name,'sha256':hashlib.sha256(b).hexdigest(),'sizeBytes':len(b)}
out.with_suffix('.command.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps({'receipt':str(out.with_suffix('.command.json')),'exitCode':proc.returncode,'elapsedSeconds':receipt['elapsedSeconds']}),flush=True)
sys.exit(proc.returncode)
