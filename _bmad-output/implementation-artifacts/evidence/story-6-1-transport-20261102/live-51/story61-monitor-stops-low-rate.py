from pathlib import Path
import os,json,time,sys,datetime
launcher=int(sys.argv[1]);evidence=Path(sys.argv[2]);destination=Path(sys.argv[3]);seen=set()
apphost=b'/home/administrator/projects/hexalith/projects/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'
while not (evidence/'run.json').exists():
 for p in Path('/proc').iterdir():
  if not p.name.isdigit() or p.name in seen:continue
  try:args=(p/'cmdline').read_bytes().split(b'\0')
  except OSError:continue
  if b'stop' not in args or apphost not in args or not args[0].split(b'/')[-1].startswith(b'aspire'):continue
  seen.add(p.name);chain=[];pid=int(p.name)
  for depth in range(10):
   try:
    entry=Path('/proc',str(pid));raw=(entry/'cmdline').read_bytes();status=(entry/'stat').read_text();fields=status[status.rfind(')')+2:].split();parent=int(fields[1])
    chain.append({'pid':pid,'parentPid':parent,'startTicks':fields[19],'arguments':[a.decode(errors='replace')[:4096] for a in raw.split(b'\0')[:6] if a]})
   except OSError:break
   if parent<=1:break
   pid=parent
  data=json.dumps({'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'expectedLauncherPid':launcher,'chain':chain}).encode()
  output=os.open(destination/('stop-'+p.name+'.json'),os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
  try:os.write(output,data)
  finally:os.close(output)
 time.sleep(1)
