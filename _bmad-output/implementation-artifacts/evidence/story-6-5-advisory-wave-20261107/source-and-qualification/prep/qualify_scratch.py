from pathlib import Path
import os,json,hashlib,subprocess,time
P=Path('/tmp/story65-advisory-wave-prep-20261107');R=Path('/home/administrator/projects/hexalith/projects');O=R/'references/Hexalith.EventStore'
OUT=P/'qualification';OUT.mkdir(exist_ok=True)
REMOVE=list(json.loads(Path('/tmp/story65-capture-counter-proof-prep-20261107/mapping-pins.json').read_text())['environment'])+['CustomBeforeMicrosoftCommonTargets']

def env(build=False):
 e=os.environ.copy()
 for k in REMOVE+['DOTNET_PROCESSOR_COUNT']:e.pop(k,None)
 for k in list(e):
  if k.startswith('Hexalith') and k.endswith('Root'):e.pop(k,None)
 e.update(MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
 if build:e['DOTNET_PROCESSOR_COUNT']='2'
 return e

def snapshot(name):
 paths=[O/rel for rel in json.loads(Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json').read_text())['sourceSha256']]
 paths += [R/'src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs',O/'src/Hexalith.EventStore.ServiceDefaults/Routing/DevelopmentRouteCatalogInputs.cs']
 files=json.loads(Path('/tmp/story65-parsed-template-native-20261107/binary-source-audit.json').read_text())['files']
 d={'sourceSha256':{str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths},'application147Sha256':{f['path']:hashlib.sha256((R/f['path']).read_bytes()).hexdigest() for f in files},'originalServerTestsDllSha256':hashlib.sha256((R/'tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll').read_bytes()).hexdigest()}
 assert len(d['sourceSha256'])==50 and len(d['application147Sha256'])==147
 (OUT/(name+'.json')).write_text(json.dumps(d,indent=2)+'\n');return d

def run(label,args,build=False):
 log=OUT/(label+'.log');receipt=OUT/(label+'.json');assert not log.exists() and not receipt.exists()
 start=time.time()
 with log.open('wb') as f:ret=subprocess.run(args,cwd=P/'differential',env=env(build),stdout=f,stderr=subprocess.STDOUT).returncode
 result={'argv':args,'cwd':str(P/'differential'),'exitCode':ret,'elapsedSeconds':time.time()-start,'buildOnlyProcessorOverride':'2' if build else None,'clearedMappingHookVariables':REMOVE,'envSet':{'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0'},'logSha256':hashlib.sha256(log.read_bytes()).hexdigest()}
 receipt.write_text(json.dumps(result,indent=2)+'\n');print(label,ret,flush=True)
 return ret

if __name__=='__main__':
 import sys
 if sys.argv[1]=='snapshot':snapshot(sys.argv[2])
 elif sys.argv[1]=='build':
  d='/home/administrator/.dotnet/dotnet'
  if run('restore-1',[d,'restore',str(P/'differential/Differential.csproj'),'--configfile',str(P/'differential/NuGet.Config'),'--ignore-failed-sources','--disable-build-servers'],True):raise SystemExit(1)
  raise SystemExit(run('build-1',[d,'build',str(P/'differential/Differential.csproj'),'-c','Debug','--no-restore','-m:1','--disable-build-servers','-p:NuGetAudit=false'],True))
