from pathlib import Path
import datetime,hashlib,json,os,shutil,subprocess,sys
PREP=Path('/tmp/story65-invocation-transport-prep-20261107');QUAL=Path('/tmp/story65-invocation-transport-qualification-20261107');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();now=lambda:datetime.datetime.now(datetime.timezone.utc).isoformat()
def save(p,v):p.write_text(json.dumps(v,indent=2)+'\n')
assert json.loads((QUAL/'facts-attempt-1/status.json').read_text())['passed']is True
plan=json.loads((PREP/'commands.json').read_text());cmd=plan['commands'][8];argv=[a.replace('<final-preparation-receipt-sha256>','8d967b31560ce95a630751a2d2b37bc6a9070296d19306ad11785c4b01ca0d36')for a in cmd['argv']]
env=os.environ.copy();removed=plan['runtimeEnvironmentRemove']+[k for k in env if k.startswith('Hexalith')and k.endswith('Root')]
for key in removed:env.pop(key,None)
attempt=QUAL/'matrix-attempt-1';attempt.mkdir(exist_ok=False)
record={'command':argv,'cwd':cmd['cwd'],'startedUtc':now(),'environmentRemoved':sorted(set(removed)),'runtimeProcessorOverride':env.get('DOTNET_PROCESSOR_COUNT'),'started':True,'completed':False,'applicationBuilds':0,'nativeTests':0,'hostsStarted':0,'stopOnFirstFailure':True}
save(attempt/'command.json',record);out=attempt/'stdout.log';err=attempt/'stderr.log'
with out.open('wb')as stdout,err.open('wb')as stderr:r=subprocess.run(argv,cwd=cmd['cwd'],env=env,stdout=stdout,stderr=stderr)
record.update(exitCode=r.returncode,finishedUtc=now(),completed=True,stdoutSha256=sha(out),stderrSha256=sha(err));save(attempt/'command.json',record)
if r.returncode:
 shutil.copytree(PREP,attempt/'failed-generation');save(attempt/'failed-generation-sha256.json',{str(p.relative_to(attempt/'failed-generation')):sha(p)for p in (attempt/'failed-generation').rglob('*')if p.is_file()});record['fullFailedGenerationPreserved']=True;save(attempt/'command.json',record)
print(json.dumps({'controllerExitCode':r.returncode,'completed':True,'failedGenerationPreserved':r.returncode!=0}),flush=True);sys.exit(r.returncode)
