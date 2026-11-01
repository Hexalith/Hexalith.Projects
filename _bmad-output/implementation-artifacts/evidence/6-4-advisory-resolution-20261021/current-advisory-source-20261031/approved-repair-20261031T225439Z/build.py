"""Individual fresh Debug builds under each owner's supported source selection."""
from record import ROOT,EVIDENCE,WORK,run,write,digest
from pathlib import Path
import json,shutil
isolated=Path(json.loads((EVIDENCE/'isolated-workspace.json').read_text())['root'])
projects=json.loads((EVIDENCE/'projects-environment.json').read_text())
conversations=json.loads((EVIDENCE/'conversations-environment.json').read_text())
results=[]
for owner,names,env in [('.', ['Hexalith.Projects.Contracts.Tests','Hexalith.Projects.Server.Tests','Hexalith.Projects.Tests'],projects),('references/Hexalith.Conversations',['Hexalith.Conversations.Server.Tests'],conversations),('references/Hexalith.Folders',['Hexalith.Folders.Server.Tests'],{'Configuration':'Debug'})]:
    cwd=isolated/owner
    for name in names:
        path=cwd/'tests'/name/(name+'.csproj')
        code=run(name+'-build',['dotnet','build',str(path),'-c','Debug','-m:1','-v:minimal'],cwd=cwd,explicit=env,timeout=900)
        results.append({'project':name,'exit_code':code,'mapping_owner':owner,'standalone_source_fallback':owner.endswith('Folders')})
        write(name+'-build-result.json',results[-1])
# Generated imports remain unchanged; retain exact current producer output for later audit.
generated={}
for owner in ['projects','conversations']:
    path=WORK/(owner+'-mapping')
    mapping={str(p.relative_to(path)):digest(p) for p in path.rglob('*') if p.is_file()}
    generated[owner]=mapping
    shutil.copytree(path,EVIDENCE/(owner+'-generated-source-mapping'))
write('generated-inputs.json',generated)
write('build-results.json',results)
if any(r['exit_code'] for r in results):raise SystemExit(1)
