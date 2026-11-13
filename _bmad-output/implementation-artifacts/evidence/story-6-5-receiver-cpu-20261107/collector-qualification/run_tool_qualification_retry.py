from pathlib import Path
import datetime,hashlib,json,os,shutil,subprocess,sys
P=Path('/tmp/story65-receiver-cpu-prep-20261107');Q=Path('/tmp/story65-receiver-cpu-qualification-20261107');R=Path('/home/administrator/projects/hexalith/projects');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();now=lambda:datetime.datetime.now(datetime.timezone.utc).isoformat();pin='6a182b564129669f01fcba97a3e6b943bf34cad6a7871ca74a98564185e25495'
def save(p,v):p.write_text(json.dumps(v,indent=2)+'\n')
def snapshot():
 f=P/'collector-preparation-receipt.json';assert sha(f)==pin;x=json.loads(f.read_text());d={}
 for n,h in x['filesSha256'].items():p=P/n;assert sha(p)==h;d[str(p)]=h
 for k in ['sourceSha256','app147Sha256','inputsSha256']:
  for n,h in x[k].items():p=Path(n);p=p if p.is_absolute()else R/p;assert sha(p)==h;d[str(p)]=h
 return d
attempt=Q/'attempt-2';attempt.mkdir(exist_ok=False);before=snapshot();save(attempt/'inputs-before.json',before);plan=json.loads((P/'collector-smoke-plan.json').read_text());steps=[]
for index,cmd in enumerate(plan['commands'],1):
 env=os.environ.copy();remove=cmd['environmentRemove']+[k for k in env if k.startswith('Hexalith')and k.endswith('Root')]
 for k in remove:env.pop(k,None)
 env.update(cmd.get('buildEnvironment',{}))
 stem=f'{index:02d}-{cmd["kind"]}';record={'command':cmd['argv'],'cwd':cmd['cwd'],'startedUtc':now(),'environmentRemoved':sorted(set(remove)),'explicitEnvironment':cmd.get('buildEnvironment',{}),'runtimeProcessorOverride':env.get('DOTNET_PROCESSOR_COUNT'),'started':True,'completed':False,'targetAttachCalls':0,'applicationHostsStarted':0};save(attempt/(stem+'.json'),record)
 out=attempt/(stem+'.stdout.log');err=attempt/(stem+'.stderr.log')
 with out.open('wb')as stdout,err.open('wb')as stderr:r=subprocess.run(cmd['argv'],cwd=cmd['cwd'],env=env,stdout=stdout,stderr=stderr)
 record.update(exitCode=r.returncode,completed=True,finishedUtc=now(),stdoutSha256=sha(out),stderrSha256=sha(err));steps.append(record);save(attempt/(stem+'.json'),record);save(attempt/'commands.json',steps)
 after=snapshot();save(attempt/'inputs-after.json',after);assert before==after
 print(json.dumps({'step':index,'kind':cmd['kind'],'exitCode':r.returncode,'allPinnedInputsUnchanged':True}),flush=True)
 if r.returncode:
  for name in ['collector','collector-smoke','qualified-baseline']:shutil.copytree(P/name,attempt/'failed-generation'/name)
  for f in P.glob('collector-*'):
   if f.is_file():shutil.copyfile(f,attempt/'failed-generation'/f.name)
  save(attempt/'status.json',{'passed':False,'stoppedAt':index,'noAutomaticFixOrRetry':True,'targetAttachCalls':0,'applicationHostsStarted':0});sys.exit(r.returncode)
 if index==1:
  old=json.loads(Path('/tmp/story65-cpu-prep-20261107/obj/project.assets.json').read_text());new=json.loads((P/'collector/obj/project.assets.json').read_text());assert set(old['libraries'])==set(new['libraries']);save(attempt/'cached-dependency-closure.json',{'samePackageNamesAndVersionsAsOldQualifiedTool':True,'libraries':sorted(new['libraries'])})
 if cmd['kind']=='smoke':
  smoke=json.loads(out.read_text());assert smoke['allPassed']is True and smoke['targetAttachCalls']==smoke['applicationProcessesStarted']==0 and smoke['traceParserExecuted']is False;save(attempt/'smoke-qualified.json',smoke)
save(attempt/'status.json',{'passed':True,'commands':5,'allPinnedInputsUnchanged':True,'targetAttachCalls':0,'applicationHostsStarted':0,'traceIngestionExecuted':False})
