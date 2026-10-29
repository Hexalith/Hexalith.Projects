"""Use the public producer unchanged for the current isolated candidate."""
from run import *
import shutil

workspace = WORK / 'projects'
owner = workspace / 'references/Hexalith.Conversations'
manifest = str(workspace / 'module/hexalith.module-manifest.v2.json')
run('current-producer-version', ['dotnet', 'tool', 'run', 'hexalith-module', '--version'], owner)
run('current-declaration-validation', ['dotnet', 'tool', 'run', 'hexalith-module', '--', 'validate', '--manifest', manifest, '--output', 'json'], owner)
run('current-source-preparation', ['dotnet', 'tool', 'run', 'hexalith-module', '--', 'prepare-build', '--manifest', manifest, '--mode', 'source', '--workspace', str(WORK / 'prepared-source'), '--output', 'json'], owner)
prepared = json.loads((EVIDENCE / 'current-source-preparation.stdout').read_text())
assert prepared['status'] == 'prepared'
env = prepared['buildPreparation']['environment']
assert env['Configuration'] == 'Debug' and len(env) == 7
shutil.copytree(WORK / 'prepared-source/source-mapping', EVIDENCE / 'generated-source-mapping')
(EVIDENCE / 'generated-input-hashes.json').write_text(json.dumps({str(p.relative_to(WORK / 'prepared-source/source-mapping')): digest(p) for p in sorted((WORK / 'prepared-source/source-mapping').rglob('*')) if p.is_file()}, indent=2) + '\n')
for layer in ['Contracts', 'Server']:
    project = f'tests/Hexalith.Projects.{layer}.Tests/Hexalith.Projects.{layer}.Tests.csproj'
    run('current-' + layer.lower() + '-build', ['dotnet', 'build', project, '-c', 'Debug', '-m:1', '-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0', '-v:minimal'], workspace, env)
