from record import ROOT,EVIDENCE,WORK,run,write,digest,sources
from pathlib import Path
import sys,json,shutil
name,attempt=sys.argv[1:];SOURCE=Path(json.loads((EVIDENCE/'workspace.json').read_text())['source'])
owner,project={'conversations':('references/Hexalith.Conversations','Hexalith.Conversations.Server.Tests'),'folders':('references/Hexalith.Folders','Hexalith.Folders.Server.Tests'),'projects-contracts':('.','Hexalith.Projects.Contracts.Tests'),'projects-core':('.','Hexalith.Projects.Tests'),'projects-server':('.','Hexalith.Projects.Server.Tests')}[name]
inputs=sources();changed={}
for relative,expected in inputs.items():
    if relative.startswith('_bmad-output/'):continue
    target=SOURCE/relative
    if not target.is_file() or digest(target)!=expected:
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/relative,target);changed[relative]=expected
write(name+'-'+attempt+'-source-overlay.json',changed)
env={'Configuration':'Debug'} if name=='folders' else json.loads((EVIDENCE/(('conversations' if name=='conversations' else 'projects')+'-environment.json')).read_text())
code=run(name+'-'+attempt+'-build',['dotnet','build',str(SOURCE/owner/'tests'/project/(project+'.csproj')),'-c','Debug','-m:1','-v:minimal',*(['-p:UseHexalithProjectReferences=true','-p:UseNuGetDeps=false'] if name=='folders' and attempt not in ('initial','ordinary') else [])],cwd=SOURCE/owner,explicit=env,timeout=900)
raise SystemExit(code)
