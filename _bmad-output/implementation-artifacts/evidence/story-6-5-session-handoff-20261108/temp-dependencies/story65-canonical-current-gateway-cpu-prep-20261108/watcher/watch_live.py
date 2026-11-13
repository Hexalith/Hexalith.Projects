"""Independent conditional observer. Exact ordinary launcher is untouched; no app lifecycle control."""
from pathlib import Path
import argparse,datetime,os,time
from common import *
from ownership import session,describe,target,identity
from coordinate import run

def main(args):
 os.umask(0o077);args.output.mkdir(mode=0o700,exist_ok=False);receipt={'diagnosticOnly':True,'featureAcceptance':False,'phase':'waiting-new-owned-session','watcherPid':os.getpid(),'watcherStartTicks':process(os.getpid())['startTicks'],'watcherGroup':os.getpgrp(),'targetSignalsSent':False,'endOnlyRunJsonNotRequired':True,'anyCollectorLaunched':False,'profilerAttached':False};write(args.output/'watcher-receipt.json',receipt)
 graph=None;owned=None;prepared=None;deadline=time.monotonic()+1000;next_graph=0;identity_paths={}
 try:
  while time.monotonic()<deadline:
   choice=decision(observations(args.evidence))
   if choice in['positive-skip-all','insufficient-windows-skip-all']:receipt['phase']=choice;break
   if not same(args.launcher_pid,args.launcher_start):receipt['phase']='ordinary-launcher-exited-no-attach';break
   if prepared is None:
    selected=session(args.not_before_epoch,args.launcher_pid,args.launcher_start,args.evidence)
    if selected:
     path,owned=selected
     if graph is None and time.monotonic()>=next_graph:
      receipt['graphAttempts']=receipt.get('graphAttempts',0)+1;graph=describe(owned,args.evidence);next_graph=time.monotonic()+2
      if graph:receipt['closedGraphProof']=graph;receipt['ownershipPathSha256']=hashlib.sha256(str(path).encode()).hexdigest()
     if graph:
      current={}
      try:
       for role in roles():
        value=target(role,owned,graph)
        if value is None or not all(mapped(value['pid'],Path(role[k]))for k in['entryDll','gatewayDll','sdkDll','domainServiceDll','contractsDll']):break
        current[role['role']]=value
      except Guard as failure:
       if str(failure)=='owned-wrapper-start-changed':graph=None;continue
       raise
      if len(current)==1:
       source_proof=sources(args.evidence);proofs={};contracts={}
       for role in roles():contracts[role['role']],proofs[role['role']]=identity(role,owned,graph,current[role['role']],args.evidence,source_proof)
       prepared={'values':current,'contracts':contracts,'proofs':proofs};receipt.update(phase='ready-waiting-failed-multiple',identityReadyUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),targetIdentities=current,currentOrdinaryGenerationProofs=proofs,onlyFixedGatewayRole=True,fullSourceSnapshotCachedOnceUntilTrigger=True)
       write(args.output/'watcher-receipt.json',receipt)
   else:
    # Quiet waiting: only finite PID/start checks and completed readiness rows. No repeated file hashing.
    if not all(same(v['pid'],v['startTicks'])and same(v['wrapperPid'],v['wrapperStartTicks'])for v in prepared['values'].values()):raise Guard('prepared-owned-runtime-changed')
    if choice=='failed-attach':
     final_contracts={};final_values={};source_proof=sources(args.evidence)
     for role in roles():
      value=target(role,owned,graph)
      if value is None or value!=prepared['values'][role['role']]:raise Guard('final-owned-runtime-changed')
      final_contracts[role['role']],_=identity(role,owned,graph,value,args.evidence,source_proof);final_values[role['role']]=value
     final_choice=decision(observations(args.evidence))
     if final_choice!='failed-attach':receipt['phase']='final-'+final_choice;break
     for role,contract in final_contracts.items():
      file=args.output/(role+'-private-identity.json');write(file,contract);identity_paths[role]=file
     receipt['anyCollectorLaunched']=True;receipt['phase']='collecting-one-gateway-target';receipt['decisionBeforeFirstLaunch']='three-distinct-strict-genuine-failed-multiple-no-positive-attempt-at-most24';write(args.output/'watcher-receipt.json',receipt)
     def postcheck():
      result={};source_proof=sources(args.evidence)
      for role in roles():
       value=target(role,owned,graph)
       if value!=final_values[role['role']]:raise Guard('post-collection-runtime-changed')
       _,result[role['role']]=identity(role,owned,graph,value,args.evidence,source_proof)
      return result
     outcome=run(args.tool,identity_paths,args.output,postcheck)
     receipt['profilerAttached']=any((args.output/role/'attached.json').is_file()for role in identity_paths);receipt['phase']='profile-finished';receipt['profileIncomplete']=outcome['incomplete'];receipt['lateIndividualGuardRefusalIsIncomplete']=True;break
   report=read_json(args.evidence/'run.json')
   if isinstance(report,dict)and report.get('result')in['failed','passed']:receipt['phase']='ordinary-run-ended-before-attach';break
   write(args.output/'watcher-receipt.json',receipt);time.sleep(.2)
  else:receipt['phase']='watcher-timeout-no-attach'
 except Exception as failure:receipt.update(phase='diagnostic-guard-refused',errorType=type(failure).__name__,fixedReason=str(failure)if type(failure)is Guard else'watcher-error')
 finally:
  removed=[]
  for role,file in identity_paths.items():
   if file.is_file()and not file.is_symlink():removed.append({'role':role,'sha256':sha(file),'bytes':file.stat().st_size});file.unlink()
  receipt['privateIdentityRemoval']=removed;receipt['privateIdentityFilesAbsent']=all(not f.exists()for f in identity_paths.values());write(args.output/'watcher-receipt.json',receipt)
 return receipt

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--evidence',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--tool',type=Path,required=True);p.add_argument('--not-before-utc',required=True);p.add_argument('--launcher-pid',type=int,required=True);p.add_argument('--launcher-start',required=True);a=p.parse_args();a.not_before_epoch=datetime.datetime.fromisoformat(a.not_before_utc.replace('Z','+00:00')).timestamp();main(a)
