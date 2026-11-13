from pathlib import Path
import os,json,hashlib,subprocess,time
P=Path('/tmp/story65-gateway-controller-prep-20261107');Q=Path('/tmp/story65-gateway-controller-qualification-20261107');R=Path('/home/administrator/projects/hexalith/projects');M=json.loads((P/'preparation-receipt.json').read_text());C=json.loads((P/'commands.json').read_text());H=lambda f:hashlib.sha256(Path(f).read_bytes()).hexdigest()
def env(build=False):
 e=os.environ.copy()
 for k in C['clearAllMappingHookVariablesForHarness']+['DOTNET_PROCESSOR_COUNT']:e.pop(k,None)
 for k in list(e):
  if k.startswith('Hexalith')and k.endswith('Root'):e.pop(k,None)
 e.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
 if build:e['DOTNET_PROCESSOR_COUNT']='2'
 return e

def snapshot(name):
 d={'sourceSha256':{p:H(R/p)for p in M['sourceSha256']},'app147Sha256':{p:H(R/p)for p in M['app147Sha256']}}
 assert d['sourceSha256']==M['sourceSha256']and d['app147Sha256']==M['app147Sha256']
 paths=json.loads((P/'variant-paths.json').read_text());d['variantSha256']={str(f):H(f)for key in ['oracleDll','privateDll']for f in [Path(paths[key]),Path(paths[key]).with_suffix('.pdb')]}
 (Q/(name+'.json')).write_text(json.dumps(d,indent=2)+'\n');return d

def run(label,argv,build=False):
 log=Q/(label+'.log');receipt=Q/(label+'.json');assert not log.exists()and not receipt.exists();started=time.monotonic()
 with log.open('wb')as f:ret=subprocess.run(argv,cwd=P,env=env(build),stdout=f,stderr=subprocess.STDOUT).returncode
 d={'argv':argv,'cwd':str(P),'exitCode':ret,'elapsedSeconds':time.monotonic()-started,'buildOnlyProcessorOverride':'2'if build else None,'clearedMappingHooks':C['clearAllMappingHookVariablesForHarness'],'setEnv':{'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0'},'logSha256':H(log)}
 receipt.write_text(json.dumps(d,indent=2)+'\n');print(label,ret,flush=True);return ret
if __name__=='__main__':
 assert H(P/'preparation-receipt.json')=='4aab8a4be30c05d67405c39601fbbbbb34b03c98319e8d3b2fd55fbdc88a3925'
 for name,wanted in M['filesSha256'].items():assert H(P/name)==wanted
 snapshot('before')
 if run('restore-1',C['scratchRestoreArgv'],True):raise SystemExit(1)
 raise SystemExit(run('build-1',C['scratchBuildArgv'],True))
