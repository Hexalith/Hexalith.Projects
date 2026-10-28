"""Validate and prepare the real Projects declaration using the released public CLI."""
from run import *
import shutil

workspace = WORK/'projects'
owner = workspace/'references/Hexalith.Conversations'
manifest = str(workspace/'module/hexalith.module-manifest.v2.json')
assert Path(manifest).is_file()
run('projects-manifest-validation', ['dotnet','tool','run','hexalith-module','--','validate','--manifest',manifest,'--output','json'], owner)
run('projects-source-preparation', ['dotnet','tool','run','hexalith-module','--','prepare-build','--manifest',manifest,'--mode','source','--workspace',str(WORK/'source'),'--output','json'], owner)
prepared = json.loads((EVIDENCE/'projects-source-preparation.stdout').read_text())
assert prepared['status']=='prepared'
env=prepared['buildPreparation']['environment']
assert env['Configuration']=='Debug'
shutil.copytree(WORK/'source/source-mapping',EVIDENCE/'generated-source-mapping')
(EVIDENCE/'generated-input-hashes.json').write_text(json.dumps({str(p.relative_to(WORK/'source/source-mapping')):digest(p) for p in sorted((WORK/'source/source-mapping').rglob('*')) if p.is_file()},indent=2)+'\n')
args=['-c','Debug','-m:1','-p:NuGetAudit=false','-p:MinVerVersionOverride=1.0.0','-v:minimal']
for layer in ['Contracts','Server']:
 label=layer.lower()
 project=f'tests/Hexalith.Projects.{layer}.Tests/Hexalith.Projects.{layer}.Tests.csproj'
 run('projects-'+label+'-build',['dotnet','build',project,*args],workspace,env,check=False)
run('projects-package-preparation',['dotnet','tool','run','hexalith-module','--','prepare-build','--manifest',manifest,'--mode','package','--workspace',str(WORK/'package'),'--output','json'],owner,check=False)
