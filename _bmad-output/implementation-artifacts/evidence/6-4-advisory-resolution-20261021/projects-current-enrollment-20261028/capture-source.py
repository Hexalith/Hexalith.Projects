"""Capture actual tracked C# and build inputs for the isolated current-source lane."""
from run import *

workspace = WORK / 'projects'
selected = json.loads((EVIDENCE / 'selected-inputs.json').read_text())
repos = ['.'] + [r['path'] for r in selected['inputs']]
result = {}
for relative in repos:
    repo = workspace / relative
    names = git(repo, 'ls-files', '-z').decode().split('\0')
    paths = [n for n in names if n and Path(n).suffix.lower() in {'.cs', '.csproj', '.props', '.targets', '.slnx'} and not n.startswith(('_bmad-output/', 'docs/'))]
    paths += [n for n in ['global.json', 'NuGet.Config', 'nuget.config', '.editorconfig', '.gitattributes', '.gitmodules', '.config/dotnet-tools.json', 'hexalith-platform.json'] if (repo / n).is_file()]
    result[relative] = {'head': git(repo, 'rev-parse', 'HEAD').decode().strip(), 'status': git(repo, 'status', '--porcelain=v1', '--untracked-files=all').decode(), 'tracked_build_hashes': {n: digest(repo / n) for n in sorted(set(paths))}}
for name in ['module/hexalith.module-manifest.v2.json', 'module/README.md', 'tools/release-packages.json']:
    result['.']['tracked_build_hashes'][name] = digest(workspace / name)
name = 'source-before.json' if not (EVIDENCE / 'source-before.json').exists() else 'source-after.json'
(EVIDENCE / name).write_text(json.dumps(result, indent=2) + '\n')
print(name, sum(len(r['tracked_build_hashes']) for r in result.values()), 'source/build inputs')
if name == 'source-after.json':
    assert result == json.loads((EVIDENCE / 'source-before.json').read_text()), 'Isolated source inputs or Git state changed'
