from record import ROOT,EVIDENCE,WORK,run,write,digest,sources
from pathlib import Path
import json,shutil
SOURCE=Path(json.loads((EVIDENCE/'workspace.json').read_text())['source'])
inputs=sources();changed={}
for relative,expected in inputs.items():
    if relative.startswith('_bmad-output/'):continue
    target=SOURCE/relative
    if not target.is_file() or digest(target)!=expected:
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/relative,target)
        changed[relative]=expected
write('scratch-source-overlay.json',changed)
write('isolated-workspace.json',{'root':str(SOURCE),'reused_command_owned_source':True,'nested_initialization':False})
cli=SOURCE/'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net10.0/Hexalith.Builds.Module.Cli.dll'
write('producer-executable-hashes.json',{p.name:digest(p) for p in cli.parent.iterdir() if p.is_file() and p.suffix in ('.dll','.pdb','.json')})
for owner,folder in [('projects','.'),('conversations','references/Hexalith.Conversations')]:
    cwd=SOURCE/folder;manifest=cwd/'module/hexalith.module-manifest.v2.json'
    code=run(owner+'-source-prepare',['dotnet',str(cli),'prepare-build','--manifest',str(manifest),'--mode','source','--workspace',str(WORK/(owner+'-mapping')),'--output','json'],cwd=cwd)
    if code:raise SystemExit(code)
    result=json.loads((EVIDENCE/(owner+'-source-prepare.stdout.log')).read_text());assert result['status']=='prepared'
    write(owner+'-environment.json',result['buildPreparation']['environment'])
    shutil.copytree(WORK/(owner+'-mapping'),EVIDENCE/(owner+'-generated-source-mapping'))
write('generated-inputs.json',{owner:{str(p.relative_to(WORK/(owner+'-mapping'))):digest(p) for p in (WORK/(owner+'-mapping')).rglob('*') if p.is_file()} for owner in ['projects','conversations']})
print('Fresh owner environments prepared unchanged.',flush=True)
