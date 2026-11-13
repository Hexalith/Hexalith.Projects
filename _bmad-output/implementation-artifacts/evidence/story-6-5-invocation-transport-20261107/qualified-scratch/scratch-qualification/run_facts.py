from pathlib import Path
import datetime,hashlib,json,os,shutil,subprocess,sys
PREP=Path('/tmp/story65-invocation-transport-prep-20261107');ROOT=Path('/home/administrator/projects/hexalith/projects');QUAL=Path('/tmp/story65-invocation-transport-qualification-20261107');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();now=lambda:datetime.datetime.now(datetime.timezone.utc).isoformat()
def save(p,v):p.write_text(json.dumps(v,indent=2)+'\n')
def snapshot():
 x=json.loads((PREP/'preparation-receipt.json').read_text());assert sha(PREP/'preparation-receipt.json')=='8d967b31560ce95a630751a2d2b37bc6a9070296d19306ad11785c4b01ca0d36';d={}
 for n,h in x['filesSha256'].items():p=PREP/n;assert sha(p)==h;d[str(p)]=h
 for k in ['sourceSha256','app147Sha256','currentExternalInputsSha256']:
  for n,h in x[k].items():p=Path(n);p=p if p.is_absolute()else ROOT/p;assert sha(p)==h;d[str(p)]=h
 return d
plan=json.loads((PREP/'commands.json').read_text());attempt=QUAL/'facts-attempt-1';attempt.mkdir(exist_ok=False);before=snapshot();save(attempt/'inputs-before.json',before)
binaries={str(p):sha(p) for directory in [PREP/'fixture/bin',PREP/'entry/eligible/bin',PREP/'entry/excluded/bin']for p in directory.rglob('*')if p.is_file()};save(attempt/'scratch-binaries-before.json',binaries)
allfacts=[]
for index,cmd in enumerate(plan['commands'][6:8],1):
 env=os.environ.copy();removed=plan['runtimeEnvironmentRemove']+[k for k in env if k.startswith('Hexalith')and k.endswith('Root')]
 for k in removed:env.pop(k,None)
 record={'command':cmd['argv'],'cwd':cmd['cwd'],'startedUtc':now(),'environmentRemoved':sorted(set(removed)),'runtimeProcessorOverride':env.get('DOTNET_PROCESSOR_COUNT'),'effectiveSourceMappingHookValues':{k:env.get(k)for k in plan['buildEnvironmentRemove']},'started':True,'completed':False};stem=f'{index:02d}-facts';save(attempt/(stem+'.json'),record);out=attempt/(stem+'.stdout.log');err=attempt/(stem+'.stderr.log')
 with out.open('wb')as stdout,err.open('wb')as stderr:r=subprocess.run(cmd['argv'],cwd=cmd['cwd'],env=env,stdout=stdout,stderr=stderr)
 record.update(exitCode=r.returncode,finishedUtc=now(),completed=True,stdoutSha256=sha(out),stderrSha256=sha(err));save(attempt/(stem+'.json'),record)
 try:
  assert r.returncode==0
  facts=json.loads(out.read_text());assert facts['runtimeVersion']=='10.0.14' and facts['processorCount']==14 and facts['processorOverride']is None
  assert facts['physicalEntry']==('Hexalith.EventStore' if index==1 else 'Story65.InvocationTransport.Excluded')
  assert all(facts[k]is True for k in ['samePublicIdentity','distinctSdkInstances','distinctPluginInstances','allOtherDependenciesDefaultShared','defaultSdkAbsent'])
  for k in ['oracle','candidate','pluginOracle','pluginCandidate','scratch']:
   assert facts[k]['configuration']=='Debug' and facts[k]['jitOptimizerDisabled']is True and 'DisableOptimizations'in facts[k]['debuggingFlags']
  assert facts['oracle']['assembly']==facts['candidate']['assembly']=='Hexalith.EventStore.ServiceDefaults'
  assert facts['pluginOracle']['assembly']==facts['pluginCandidate']['assembly']=='Hexalith.EventStore.Server.Tests'
  assert facts['scratch']['assembly']==facts['physicalEntry']
  assert snapshot()==before
  assert {n:sha(Path(n))for n in binaries}==binaries
  record['metadataQualified']=True;save(attempt/(stem+'.json'),record);allfacts.append(facts)
  print(json.dumps({'entry':facts['physicalEntry'],'exitCode':0,'runtime':facts['runtimeVersion'],'naturalProcessorCount':facts['processorCount'],'buildingSdk':facts['buildingSdk'],'sameIdentityDebugMetadataQualified':True,'allInputsUnchanged':True}),flush=True)
 except Exception as error:
  record['metadataQualified']=False;record['qualificationFailureType']=type(error).__name__;save(attempt/(stem+'.json'),record);shutil.copytree(PREP,attempt/'failed-generation');save(attempt/'status.json',{'passed':False,'stoppedAt':index,'matrixExecuted':False});sys.exit(1)
save(attempt/'inputs-after.json',snapshot());save(attempt/'facts-qualified.json',allfacts);save(attempt/'status.json',{'passed':True,'factsProcesses':2,'ordinaryAndScratchInputsUnchanged':True,'matrixExecuted':False})
