"""Prepare a new isolated candidate from exact current permitted owner identities."""
from run import *
import shutil

workspace = WORK / 'projects'
assert not workspace.exists()
before = json.loads((EVIDENCE / 'preservation-before.json').read_text())
paths = [row.split(' ', 1)[1] for row in git(ROOT, 'config', '--file', '.gitmodules', '--get-regexp', r'^submodule\..*\.path$').decode().splitlines()]
selected = []
for relative in ['.'] + paths:
    source = ROOT / relative
    label = 'root' if relative == '.' else source.name.lower()
    run(label + '-original-status', ['git', 'status', '--short', '--branch'], source)
    run(label + '-original-remotes', ['git', 'remote', '-v'], source)
    run(label + '-original-history', ['git', 'log', '-3', '--format=fuller'], source)
    assert git(source, 'rev-parse', 'HEAD').decode().strip() == before[relative]['head']
run('clone-current-root', ['git', 'clone', '--shared', '--no-checkout', '--no-recurse-submodules', str(ROOT), str(workspace)], WORK)
run('detach-current-root', ['git', '-c', 'submodule.recurse=false', 'checkout', '--detach', before['.']['head']], workspace)
run('isolated-root-no-recursion', ['git', 'config', 'submodule.recurse', 'false'], workspace)
for relative in paths:
    source = ROOT / relative
    target = workspace / relative
    label = source.name.lower()
    revision = before[relative]['head']
    assert git(ROOT, 'ls-files', '--stage', '--', relative).decode().startswith('160000 ' + revision + ' 0\t')
    run(label + '-current-clone', ['git', 'clone', '--shared', '--no-checkout', '--no-recurse-submodules', str(source), str(target)], workspace)
    run(label + '-current-detach', ['git', '-c', 'submodule.recurse=false', 'checkout', '--detach', revision], target)
    run(label + '-absorb-direct', ['git', 'submodule', 'absorbgitdirs', '--', relative], workspace)
    assert git(target, 'rev-parse', '--show-superproject-working-tree').decode().strip() == str(workspace)
    assert not list((target / 'references').glob('*/.git'))
    assert not git(target, 'status', '--porcelain', '--untracked-files=all')
    selected.append({'path': relative, 'selected': revision, 'source': str(source), 'reason': 'Exact current permitted owner HEAD, matching original root stage-zero gitlink'})
assert not git(workspace, 'diff', '--cached', '--name-only')
previous = ROOT / '_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/projects-root-enrollment-20261028'
seal = json.loads((previous / 'SHA256SUMS.json').read_text())
provenance = []
for name in ['module/hexalith.module-manifest.v2.json', 'module/README.md']:
    original = previous / 'reviewed-candidate-source' / name
    assert digest(original) == seal['reviewed-candidate-source/' + name]
    target = workspace / name
    target.parent.mkdir(exist_ok=True)
    shutil.copy2(original, target)
    provenance.append({'path': name, 'prior_receipt': str(original.relative_to(ROOT)), 'prior_sha256': digest(original)})
readme = workspace / 'module/README.md'
b = readme.read_bytes()
old = b'6cdf9bbdc0824e59529c9a99c16b7ec02e03e21b'
assert b.count(old) == 1
readme.write_bytes(b.replace(old, before['.']['head'].encode()))
# Include the already approved local consumer metadata without copying it from the old candidate.
shutil.copy2(ROOT / 'tools/release-packages.json', workspace / 'tools/release-packages.json')
assert (workspace / 'tools/release-packages.json').read_bytes() == (ROOT / 'tools/release-packages.json').read_bytes()
assert (workspace / 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj').read_bytes().replace(b'\r\n', b'\n') == (ROOT / 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj').read_bytes().replace(b'\r\n', b'\n')
(EVIDENCE / 'selected-inputs.json').write_text(json.dumps({'workspace': str(workspace), 'head': before['.']['head'], 'inputs': selected, 'declaration_provenance': provenance, 'local_root_overlay': ['tools/release-packages.json'], 'original_index_mutations': False, 'isolated_gitlink_changes': False, 'qualification': 'Isolated current-source verification only; no package/runtime/Story acceptance'}, indent=2) + '\n')
run('current-isolated-status', ['git', 'status', '--short', '--branch'], workspace)
print(workspace)
