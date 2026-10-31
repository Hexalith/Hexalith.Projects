import subprocess,json,datetime,sys
from pathlib import Path
e=Path(__file__).resolve().parent
name,cwd,*argv=sys.argv[1:]
t=datetime.datetime.now(datetime.timezone.utc).isoformat()
r=subprocess.run(argv,cwd=cwd,capture_output=True)
(e/(name+'.stdout')).write_bytes(r.stdout)
(e/(name+'.stderr')).write_bytes(r.stderr)
(e/(name+'.command.json')).write_text(json.dumps({'cwd':cwd,'argv':argv,'startedUtc':t,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exit':r.returncode},indent=2)+'\n')
print(name,r.returncode,len(r.stdout),len(r.stderr))
