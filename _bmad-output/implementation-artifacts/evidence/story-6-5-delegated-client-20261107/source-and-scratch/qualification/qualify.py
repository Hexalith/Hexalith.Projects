from pathlib import Path
import json,hashlib,os,subprocess,time
P=Path('/tmp/story65-delegated-client-prep-20261107');Q=Path('/tmp/story65-delegated-client-qualification-20261107');R=Path('/home/administrator/projects/hexalith/projects')
C=json.loads((P/'commands.json').read_text());M=json.loads((P/'preparation-receipt.json').read_text())
def env(build=False):
 e=os.environ.copy()
 for key in C['clearMappingHookEnv']+['DOTNET_PROCESSOR_COUNT']:e.pop(key,None)
 for key in list(e):
  if key.startswith('Hexalith') and key.endswith('Root'):e.pop(key,None)
 e.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
 if build:e['DOTNET_PROCESSOR_COUNT']='2'
 return e

def snapshot(name):
 data={'sourceSha256':{p:hashlib.sha256((R/p).read_bytes()).hexdigest() for p in M['sourceSha256']},'app147Sha256':{p:hashlib.sha256((R/p).read_bytes()).hexdigest() for p in M['app147Sha256']},'originalDomainServiceTestsDllSha256':hashlib.sha256((R/'references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/bin/Debug/net10.0/Hexalith.EventStore.DomainService.Tests.dll').read_bytes()).hexdigest()}
 assert data['sourceSha256']==M['sourceSha256'] and data['app147Sha256']==M['app147Sha256'] and data['originalDomainServiceTestsDllSha256']==M['originalDomainServiceTestsDllSha256']
 (Q/(name+'.json')).write_text(json.dumps(data,indent=2)+'\n');return data

def run(label,argv,build=False):
 log=Q/(label+'.log');receipt=Q/(label+'.json');assert not log.exists() and not receipt.exists()
 t=time.monotonic()
 with log.open('wb') as output:r=subprocess.run(argv,cwd=P,env=env(build),stdout=output,stderr=subprocess.STDOUT)
 d={'argv':argv,'cwd':str(P),'exitCode':r.returncode,'elapsedSeconds':time.monotonic()-t,'buildOnlyProcessorOverride':'2' if build else None,'clearedMappingHooks':C['clearMappingHookEnv'],'setEnvironment':{'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0'},'logSha256':hashlib.sha256(log.read_bytes()).hexdigest()}
 receipt.write_text(json.dumps(d,indent=2)+'\n');print(label,r.returncode,flush=True);return r.returncode

if __name__=='__main__':
 snapshot('before')
 assert hashlib.sha256((P/'preparation-receipt.json').read_bytes()).hexdigest()=='f4a30ab92e2f7fa29c0c64893a9af4337d142aef3085e25a35412d53148330b5'
 for p,wanted in M['filesSha256'].items():assert hashlib.sha256((P/p).read_bytes()).hexdigest()==wanted
 if run('restore-1',C['restoreArgv'],True):raise SystemExit(1)
 raise SystemExit(run('build-1',C['buildArgv'],True))
