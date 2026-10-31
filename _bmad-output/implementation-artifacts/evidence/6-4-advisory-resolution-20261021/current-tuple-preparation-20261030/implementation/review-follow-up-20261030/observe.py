"""Retain exact read-only or local validation commands without replacing prior receipts."""
import datetime,json,subprocess,sys
from pathlib import Path
e=Path(__file__).resolve().parent
name,cwd,*argv=sys.argv[1:]
if (e/(name+'.command.json')).exists():raise ValueError('Receipt already exists: '+name)
t=datetime.datetime.now(datetime.timezone.utc).isoformat()
with (e/(name+'.stdout')).open('wb') as out,(e/(name+'.stderr')).open('wb') as err:
 try:
  r=subprocess.run(argv,cwd=cwd,stdout=out,stderr=err,timeout=1200);code=r.returncode
 except subprocess.TimeoutExpired:code=124
(e/(name+'.command.json')).write_text(json.dumps({'cwd':cwd,'argv':argv,'startedUtc':t,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exit':code},indent=2)+'\n')
print(name,code)
