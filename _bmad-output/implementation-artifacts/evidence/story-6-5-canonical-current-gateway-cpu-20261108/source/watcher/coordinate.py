"""One fixed Gateway collector; it closes before its parser. No target lifecycle actions."""
from pathlib import Path
import subprocess,sys,time
from common import *
from run_one import remove_private_derivatives

def members(group):
 out=[]
 for p in Path('/proc').iterdir():
  if not p.name.isdigit():continue
  try:
   fields=Path('/proc',p.name,'stat').read_text().rsplit(')',1)[1].split()
   # A zombie or descendant in a new process group still prevents session closure proof.
   if int(fields[2])==group or int(fields[3])==group:out.append({'pid':int(p.name),'startTicks':fields[19]})
  except(FileNotFoundError,ProcessLookupError):continue
 return out

def wait_owned(child,record):
 code=child.wait();until=time.monotonic()+10
 while members(child.pid)and time.monotonic()<until:time.sleep(.05)
 record.update(exitCode=code,remainingDiagnosticGroupMembers=members(child.pid),processGroupAbsent=not members(child.pid));return record['processGroupAbsent']

def run(tool,identities,output,postcheck):
 receipt={'diagnosticOnly':True,'featureAcceptance':False,'collectorProcesses':[],'parserProcesses':[],'collectionRequestedAtMonotonicNs':time.monotonic_ns(),'collectionWindowSecondsPerSession':8,'singleFixedGatewayTarget':True,'noRequestOrLogicalRootJoin':True,'incomplete':True,'targetSignalsSent':False};children=[];all_closed=False
 if set(identities)!={'gateway'}:raise Guard('only-fixed-gateway-target-allowed')
 try:
  # Coordinator has no threads and no preexec. Each child is a fresh supervisor with exactly ONE preexec child.
  for role,identity in identities.items():
   command=[sys.executable,str(OPS/'run_one.py'),'--stage','collect','--tool',str(tool),'--identity',str(identity),'--output',str(output/role)]
   child=subprocess.Popen(command,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,start_new_session=True);item={'role':role,'pid':child.pid,'startTicks':None,'processGroup':child.pid,'launchedAtMonotonicNs':time.monotonic_ns(),'processGroupAbsent':False};receipt['collectorProcesses'].append(item);children.append((child,item));value=process(child.pid);item['startTicks']=value['startTicks']if value else None
  write(output/'coordinator-receipt.json',receipt)
  all_closed=all([wait_owned(child,item)for child,item in children])
  if not all_closed:raise Guard('collector-group-absence-unproven')
  receipt['allCollectorsAndGroupsClosedBeforeAnyParser']=True;receipt['allCollectorsClosedAtMonotonicNs']=time.monotonic_ns()
  try:receipt['postCollectionTargetIdentityGenerationAndMaps']=postcheck()
  except Exception as failure:receipt['postCollectionGuardFailureType']=type(failure).__name__;receipt['postCollectionGuardPassed']=False
  else:receipt['postCollectionGuardPassed']=True
  # Parsing is strictly serial, with no collector still active. Partial captures stay explicitly incomplete.
  for role in identities:
   directory=output/role
   if not(directory/'collection-receipt.json').is_file()or not(directory/'execution.nettrace').is_file():continue
   command=[sys.executable,str(OPS/'run_one.py'),'--stage','analyze','--tool',str(tool),'--output',str(directory)]
   child=subprocess.Popen(command,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,start_new_session=True);item={'role':role,'pid':child.pid,'startTicks':None,'processGroup':child.pid,'launchedAtMonotonicNs':time.monotonic_ns(),'processGroupAbsent':False};receipt['parserProcesses'].append(item);children.append((child,item));write(output/'coordinator-receipt.json',receipt);value=process(child.pid);item['startTicks']=value['startTicks']if value else None
   if not wait_owned(child,item):raise Guard('parser-group-absence-unproven')
  receipt['incomplete']=len(receipt['parserProcesses'])!=1 or not receipt.get('postCollectionGuardPassed')or any(x['exitCode']!=0 for x in receipt['collectorProcesses']+receipt['parserProcesses'])
 except Exception as failure:receipt['fixedFailure']='diagnostic-coordinator-incomplete';receipt['errorType']=type(failure).__name__
 finally:
  # Never abandon diagnostic children. They own their bounded CPU/RSS/wall guards; never signal application targets.
  all_closed=all([wait_owned(child,item)for child,item in children]);receipt['allOwnedDiagnosticGroupsAbsent']=all_closed
  windows=[]
  for role in identities:
   attached=read_json(output/role/'attached.json');closed=read_json(output/role/'collection-receipt.json')
   if isinstance(attached,dict)and isinstance(closed,dict):
    windows.append({'role':role,'pid':attached.get('Pid'),'startTicks':attached.get('StartTicks'),'attachedTicks':attached.get('attachedTicks'),'beginTicks':closed.get('beginTicks'),'finalCloseTicks':closed.get('endTicks'),'frequency':closed.get('frequency'),'incomplete':closed.get('incomplete'),'reason':closed.get('reason'),'stopCompleted':closed.get('stopCompleted'),'drainCompleted':closed.get('drainCompleted')})
  receipt['actualAttachWindows']=windows;receipt['actualAttachedCount']=len(windows)
  if len(windows)==1 and len({x['frequency']for x in windows})==1 and all(type(x['attachedTicks'])is int and type(x['beginTicks'])is int and type(x['finalCloseTicks'])is int and type(x['frequency'])is int and x['frequency']>0 for x in windows):
   frequency=windows[0]['frequency'];receipt['attachStartSpreadMilliseconds']=1000*(max(x['attachedTicks']for x in windows)-min(x['attachedTicks']for x in windows))/frequency
   receipt['nominalRequestedWindowOverlapMilliseconds']=max(0,1000*(min(x['beginTicks']+8*frequency for x in windows)-max(x['attachedTicks']for x in windows))/frequency)
   receipt['overlapIsMetadataEstimateNotExactSampleOrRequestCoverage']=True
   for x in windows:x['attachedToFinalCloseMilliseconds']=1000*(x['finalCloseTicks']-x['attachedTicks'])/frequency
  receipt['privateRemoval']={}
  if all_closed:
   for role in identities:receipt['privateRemoval'][role]=remove_private_derivatives(output/role)
  else:receipt['privateRemovalDeferredReason']='owned-diagnostic-group-absence-unproven'
  if not all_closed or any(not x['allNonAllowlistedPrivateFilesAbsent']for x in receipt['privateRemoval'].values()):receipt['incomplete']=True
  write(output/'coordinator-receipt.json',receipt)
 return receipt
