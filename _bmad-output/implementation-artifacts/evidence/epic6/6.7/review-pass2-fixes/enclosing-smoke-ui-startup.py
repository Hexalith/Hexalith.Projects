"""Run the built UI locally until startup, retain its one route-selection log, and reap the host."""
import argparse, json, os, queue, subprocess, threading, time
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--assembly',required=True);args=parser.parse_args()
out=Path(__file__).resolve().parent;log=out/'ui-startup.log';proof=out/'ui-startup-proof.json'
assert not log.exists() and not proof.exists()
env=os.environ.copy();env.update(ASPNETCORE_ENVIRONMENT='Development',Projects__Reads__Mode='Supported')
# No request is issued and no dependency is contacted; only local host startup is observed.
command=['dotnet',str(Path(args.assembly).resolve()),'--urls','http://127.0.0.1:0']
child=subprocess.Popen(command,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,env=env)
lines=[];events=queue.Queue()
def read():
 for line in child.stdout:events.put(line)
reader=threading.Thread(target=read);reader.start();started=False
try:
 deadline=time.monotonic()+30
 while time.monotonic()<deadline:
  try:line=events.get(timeout=.2);lines.append(line)
  except queue.Empty:
   if child.poll() is not None:break
   continue
  if 'Now listening on:' in line:started=True;break
finally:
 child.terminate()
 try:child.wait(timeout=15)
 except subprocess.TimeoutExpired:child.kill();child.wait()
 reader.join(timeout=5)
 while not events.empty():lines.append(events.get())
 log.write_text(''.join(lines))
 matches=[line for line in lines if 'Project read route Unavailable; refusal reason missing-evidence-configuration.' in line]
 result={'command':command,'environment':{'ASPNETCORE_ENVIRONMENT':'Development','Projects__Reads__Mode':'Supported'},'started':started,'matchingStartupLogs':len(matches),'reaped':child.poll() is not None,'deployment':False}
 proof.write_text(json.dumps(result,indent=2)+'\n')
 assert started and len(matches)==1 and result['reaped'],result
 print(json.dumps(result))
