from record import ROOT,EVIDENCE,WORK,run,write,sources,digest
from pathlib import Path
import sys,json,shutil
owner,attempt=sys.argv[1:];source=Path(json.loads((EVIDENCE/'workspace.json').read_text())['source'])
inputs=sources();extra=['references/Hexalith.Folders/deploy/observability/production/observability.yaml','references/Hexalith.Folders/README.md']
for name in extra:inputs[name]=digest(ROOT/name)
changed={}
for relative,h in inputs.items():
 if relative.startswith('_bmad-output/'):continue
 target=source/relative
 if not target.is_file() or digest(target)!=h:
  target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/relative,target);changed[relative]=h
write(owner+'-'+attempt+'-source-overlay.json',changed)
folder,project={'folders':('references/Hexalith.Folders','Hexalith.Folders.Server.Tests'),'folders-contracts':('references/Hexalith.Folders','Hexalith.Folders.Contracts.Tests'),'projects':('.','Hexalith.Projects.Server.Tests')}[owner]
env={'Configuration':'Debug'} if owner.startswith('folders') else json.loads((EVIDENCE/'projects-environment.json').read_text())
args=['dotnet','build',str(source/folder/'tests'/project/(project+'.csproj')),'-c','Debug','-m:1','-v:minimal']
if owner.startswith('folders'):args+=['-p:UseHexalithProjectReferences=true','-p:UseNuGetDeps=false']
raise SystemExit(run(owner+'-'+attempt+'-build',args,cwd=source/folder,explicit=env,timeout=900))
