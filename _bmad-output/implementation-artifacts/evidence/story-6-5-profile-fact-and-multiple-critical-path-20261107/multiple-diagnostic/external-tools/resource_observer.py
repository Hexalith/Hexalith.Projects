from pathlib import Path
import json,os,time,re
OVERLAY=Path('/tmp/story65-multiple-overlay-20261107');OUT=Path('/tmp/story65-multiple-diagnostic-20261107');clock=os.sysconf('SC_CLK_TCK');page=os.sysconf('SC_PAGE_SIZE');seen={};count=0

def capture(pid):
 global count
 p=Path('/proc')/str(pid);s=(p/'stat').read_text();fields=s[s.rfind(')')+2:].split();start=fields[19]
 if pid in seen and seen[pid]!=start:return None
 first=pid not in seen;seen[pid]=start;affinity=sorted(os.sched_getaffinity(pid));row={'pid':pid,'startTicks':start,'monotonicNs':time.monotonic_ns(),'kernelCpuTicks':int(fields[11])+int(fields[12]),'kernelCpuTickFrequency':clock,'threads':int(fields[17]),'rssBytes':int(fields[21])*page,'affinityCpuIds':affinity,'affinityCpuCount':len(affinity)}
 if first:
  value=None
  for raw in (p/'environ').read_bytes().split(b'\0'):
   if raw.startswith(b'DOTNET_PROCESSOR_COUNT='):value=raw.split(b'=',1)[1].decode('ascii',errors='replace');break
  row['processorOverride']='absent' if value is None else value if re.fullmatch('[0-9]{1,6}',value) else 'non-numeric'
  limits=[];stats=None
  try:
   root=Path('/sys/fs/cgroup');line=next(x for x in (p/'cgroup').read_text().splitlines() if x.startswith('0::'));current=(root/line[3:].lstrip('/')).resolve();assert current.is_relative_to(root)
   for group in [current,*current.parents]:
    if not group.is_relative_to(root):continue
    limit=group/'cpu.max'
    if limit.exists():
     quota,period=limit.read_text().split();limits.append({'quota':None if quota=='max' else int(quota),'period':int(period)})
    if stats is None and (group/'cpu.stat').exists():stats={k:int(v) for k,v in (x.split() for x in (group/'cpu.stat').read_text().splitlines()) if k in ['nr_periods','nr_throttled','throttled_usec']}
   finite=[x['quota']/x['period'] for x in limits if x['quota'] is not None];row['effectiveQuotaCpuCount']=min(finite) if finite else None;row['quotaUnconstrained']=not finite;row['cpuLimits']=limits;row['cgroupCpuCounters']=stats;row['cgroupCounterScope']='May include other processes; not attributed solely to this PID.'
  except (OSError,AssertionError,StopIteration,ValueError):row['cgroupFactsAvailable']=False
 return row

with (OUT/'process-resource-observations.jsonl').open('w') as output:
 deadline=time.monotonic()+900
 while time.monotonic()<deadline:
  for f in OVERLAY.glob('Hexalith.EventStore.ServiceDefaults-*.jsonl'):
   pid=int(re.search(r'-(\d+)\.jsonl$',f.name)[1])
   try:
    row=capture(pid)
    if row is not None:output.write(json.dumps(row)+'\n');count+=1
   except (OSError,ValueError,ProcessLookupError):pass
  output.flush()
  status=OUT/'supervisor.json'
  if status.exists() and json.loads(status.read_text()).get('phase')=='finished':break
  time.sleep(.1)
(OUT/'resource-observer-receipt.json').write_text(json.dumps({'ownedObserverPid':os.getpid(),'samples':count,'processIdentities':seen,'environmentReadScope':'Only owned PIDs discovered from this unique private timing directory; only DOTNET_PROCESSOR_COUNT emitted.','processStateReadOnly':True,'noAffinityQuotaOrEnvironmentMutation':True,'cgroupCountersMayIncludeOtherProcesses':True},indent=2)+'\n')
print('resource samples',count,flush=True)
