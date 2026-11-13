from pathlib import Path
import datetime,hashlib,json,os,shutil,subprocess,sys
PREP=Path('/tmp/story65-invocation-transport-prep-20261107');ROOT=Path('/home/administrator/projects/hexalith/projects');QUAL=Path('/tmp/story65-invocation-transport-qualification-20261107')
PIN='8d967b31560ce95a630751a2d2b37bc6a9070296d19306ad11785c4b01ca0d36'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
now=lambda:datetime.datetime.now(datetime.timezone.utc).isoformat()
def save(p,v):p.write_text(json.dumps(v,indent=2)+'\n')
def snapshot():
 x=json.loads((PREP/'preparation-receipt.json').read_text());assert sha(PREP/'preparation-receipt.json')==PIN
 result={}
 for rel,d in x['filesSha256'].items():p=PREP/rel;assert sha(p)==d;result[str(p)]=d
 for k in ['sourceSha256','app147Sha256','currentExternalInputsSha256']:
  for name,d in x[k].items():
   p=Path(name);p=p if p.is_absolute()else ROOT/p;assert p.is_file()and not p.is_symlink()and sha(p)==d;result[str(p)]=d
 test=ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll';result[str(test)]=sha(test)
 return result
plan=json.loads((PREP/'commands.json').read_text());attempt=QUAL/'build-attempt-1';attempt.mkdir(exist_ok=False);before=snapshot();save(attempt/'inputs-before.json',before)
producer=Path('/tmp/story65-invocation-transport-ops-prep-20261107/producer-attempts/attempt-_ktcrvhx/receipt.json');assert sha(producer)=='516fcd90fc5ea263eab9335c7d737f26d48bdb0dac1f36e97842bf4c8c6272b0'
for p,d in [(Path('/tmp/story65-invocation-transport-ops-prep-20261107/isolated-sdk/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll'),'579126362fc7b763a8f979cf3613b93942bc699ea3dbab1a6aec0408104fd04c'),(Path('/tmp/story65-invocation-transport-ops-prep-20261107/isolated-sdk/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.pdb'),'44e75fbf5dafd03e70f3096b9f9b3fc4ad724609d12c877d0987da0ef85d6730')]:assert sha(p)==d
records=[]
for index,cmd in enumerate(plan['commands'][:6],1):
 env=os.environ.copy();removed=plan['buildEnvironmentRemove']+[k for k in env if k.startswith('Hexalith')and k.endswith('Root')]
 for key in removed:env.pop(key,None)
 env.update(plan['buildEnvironmentOnly'])
 r={'kind':cmd['kind'],'command':cmd['argv'],'cwd':cmd['cwd'],'startedUtc':now(),'environmentRemoved':sorted(set(removed)),'explicitEnvironment':plan['buildEnvironmentOnly'],'effectiveSourceMappingHookValues':{k:env.get(k)for k in plan['buildEnvironmentRemove']},'started':True,'completed':False}
 stem=f'{index:02d}-{cmd["kind"]}';save(attempt/(stem+'.json'),r)
 out=attempt/(stem+'.stdout.log');err=attempt/(stem+'.stderr.log')
 with out.open('wb')as stdout,err.open('wb')as stderr:completed=subprocess.run(cmd['argv'],cwd=cmd['cwd'],env=env,stdout=stdout,stderr=stderr)
 r.update(exitCode=completed.returncode,finishedUtc=now(),completed=True,stdoutSha256=sha(out),stderrSha256=sha(err),stdoutBytes=out.stat().st_size,stderrBytes=err.stat().st_size)
 save(attempt/(stem+'.json'),r);records.append(r);save(attempt/'build-commands.json',records)
 after=snapshot();save(attempt/'inputs-after.json',after);assert after==before
 print(json.dumps({'step':index,'kind':cmd['kind'],'exitCode':completed.returncode,'ordinaryInputsUnchanged':True}),flush=True)
 if completed.returncode:
  shutil.copytree(PREP,attempt/'failed-generation');save(attempt/'failed-generation-sha256.json',{str(p.relative_to(attempt/'failed-generation')):sha(p)for p in (attempt/'failed-generation').rglob('*')if p.is_file()});save(attempt/'status.json',{'passed':False,'stoppedAt':index,'coreOrHarnessFilesChanged':False,'ordinaryInputsUnchanged':True,'factsExecuted':False,'matrixExecuted':False});sys.exit(completed.returncode)
save(attempt/'status.json',{'passed':True,'commands':6,'ordinaryInputsUnchanged':True,'factsExecuted':False,'matrixExecuted':False})
