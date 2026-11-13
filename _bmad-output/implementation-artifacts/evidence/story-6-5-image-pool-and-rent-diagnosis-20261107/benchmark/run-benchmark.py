from pathlib import Path
import datetime,hashlib,json,os,subprocess
out=Path('/tmp/story65-pool-capacity-probe-20261107');env=os.environ.copy();removed=[]
for key in ('DOTNET_PROCESSOR_COUNT','CustomBeforeMicrosoftCommonTargets','CustomBeforeDirectoryBuildProps','CustomAfterMicrosoftCommonProps','CustomBeforeDirectoryBuildTargets','CustomAfterDirectoryBuildTargets','AfterMicrosoftNETSdkTargets','HEXALITH_SOURCE_MAPPING_HASH','Configuration'):
 if key in env:removed.append(key);env.pop(key)
report={'diagnosticOnly':True,'commands':[],'removedEnvironmentKeys':removed,'processorOverrideExplicitlyAbsent':True,'noProductionSourceChanges':True,'inventoryCount':12,'sdkPath':str(out/'bin/Release/net10.0/Probe.dll')}
order=[(1,2),(2,4),(4,2),(1,4),(2,2),(4,4)]
for slots,workers in order:
 output=out/f'results-slots{slots}-workers{workers}.json';log=out/f'run-slots{slots}-workers{workers}.log'
 args=['dotnet',str(out/'bin/Release/net10.0/Probe.dll'),'/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/src/Hexalith.EventStore.Gateway/bin/Debug/net10.0/Hexalith.EventStore.Gateway.dll',str(slots),str(workers),str(output)]
 receipt={'argv':args,'cwd':str(out),'startedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'log':str(log),'output':str(output)}
 print('RUN',slots,workers,flush=True)
 with log.open('wb') as stream:done=subprocess.run(args,cwd=out,env=env,stdout=stream,stderr=subprocess.STDOUT,timeout=180)
 receipt.update(exitCode=done.returncode,logSha256=hashlib.sha256(log.read_bytes()).hexdigest(),finishedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
 if output.exists():receipt['outputSha256']=hashlib.sha256(output.read_bytes()).hexdigest()
 report['commands'].append(receipt);(out/'benchmark-command-receipt.json').write_text(json.dumps(report,indent=2)+'\n')
 print('DONE',slots,workers,done.returncode,flush=True)
 if done.returncode:raise SystemExit(done.returncode)
print('ALL_SIX_COMPLETE',flush=True)
