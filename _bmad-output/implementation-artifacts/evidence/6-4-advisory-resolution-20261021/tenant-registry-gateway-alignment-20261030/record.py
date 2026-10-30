"""Record exact local qualification commands; never archive credential values."""
import argparse,datetime,json,os,subprocess,time
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--name',required=True);p.add_argument('--cwd',required=True);p.add_argument('--env',action='append',default=[]);p.add_argument('--timeout',type=int,default=300);p.add_argument('argv',nargs=argparse.REMAINDER);a=p.parse_args()
e=Path(__file__).resolve().parent
argv=a.argv[1:] if a.argv[:1]==['--'] else a.argv
assert argv and '/' not in a.name
paths={k:e/(a.name+'.'+k) for k in ['stdout','stderr','command.json']}
assert not any(x.exists() for x in paths.values())
overrides=dict(x.split('=',1) for x in a.env);env=os.environ.copy();env.update(overrides)
record={'argv':argv,'cwd':a.cwd,'environment':overrides,'inheritedEnvironment':'Current process; no credentials archived.','timeoutSeconds':a.timeout,'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
start=time.monotonic()
with paths['stdout'].open('xb') as stdout,paths['stderr'].open('xb') as stderr:
 try: code=subprocess.run(argv,cwd=a.cwd,env=env,stdout=stdout,stderr=stderr,timeout=a.timeout).returncode
 except subprocess.TimeoutExpired: code=124;record['timedOut']=True
record.update(exitCode=code,finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),elapsedSeconds=round(time.monotonic()-start,3))
paths['command.json'].write_text(json.dumps(record,indent=2)+'\n')
print(json.dumps({'name':a.name,'exitCode':code,'seconds':record['elapsedSeconds']}))
raise SystemExit(code)
