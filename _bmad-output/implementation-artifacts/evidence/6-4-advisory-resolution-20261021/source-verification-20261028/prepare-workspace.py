"""Clone real source identities; align only the disposable root's gitlinks."""
from run import *

assert not WORK.exists(), 'Refuse to reuse a workspace'
WORK.mkdir()
workspace = WORK/'projects'
owners = json.loads((EVIDENCE/'owner-identities.json').read_text())
baseline = json.loads((EVIDENCE/'preservation-before.json').read_text())
for owner in owners.values():
    path = Path(owner['path'])
    assert git(path, 'rev-parse', 'HEAD').decode().strip() == owner['head']
    assert baseline[str(path.relative_to(ROOT))]['head'] == owner['head']

run('clone-root', ['git', 'clone', '--shared', '--no-checkout', '--no-recurse-submodules', str(ROOT), str(workspace)], WORK)
run('detach-root', ['git', '-c', 'submodule.recurse=false', 'checkout', '--detach', owners['projects']['head']], workspace)
run('isolated-root-no-recursion', ['git', 'config', 'submodule.recurse', 'false'], workspace)
paths = git(ROOT, 'config', '--file', '.gitmodules', '--get-regexp', r'^submodule\..*\.path$').decode().splitlines()
selected = []
for row in paths:
    relative = row.split(' ', 1)[1]
    source = ROOT/relative
    target = workspace/relative
    label = source.name.lower()
    owner_head = owners[label]['head']
    revision = '16ff70a2321ad576faaac3fe53088c2f1bdacae3' if source.name == 'Hexalith.Builds' else owner_head
    run(label+'-selected-object', ['git', 'cat-file', '-t', revision], source)
    run(label+'-selected-commit', ['git', 'show', '--no-patch', '--format=fuller', revision], source)
    run(label+'-clone', ['git', 'clone', '--shared', '--no-checkout', '--no-recurse-submodules', str(source), str(target)], workspace)
    run(label+'-detach', ['git', '-c', 'submodule.recurse=false', 'checkout', '--detach', revision], target)
    run(label+'-absorb-direct', ['git', 'submodule', 'absorbgitdirs', '--', relative], workspace)
    run(label+'-isolated-gitlink', ['git', 'update-index', '--cacheinfo', '160000,'+revision+','+relative], workspace)
    parent = run(label+'-isolated-parent', ['git', 'rev-parse', '--show-superproject-working-tree'], target).strip()
    assert parent == str(workspace)
    assert git(target, 'rev-parse', 'HEAD').decode().strip() == revision
    assert not list((target/'references').glob('*/.git')), 'Nested checkout initialized'
    assert not git(target, 'status', '--porcelain', '--untracked-files=all'), 'Selected committed source is dirty'
    selected.append(dict(path=relative, original_head=owner_head, selected=revision,
                         reason='Already qualified CLI/catalog v4.38.1; original catalog changes excluded' if revision != owner_head else 'Verified actual owner HEAD, committed source only'))

run('isolated-head', ['git', 'rev-parse', 'HEAD'], workspace)
run('isolated-index', ['git', 'ls-files', '--stage', 'references/'], workspace)
run('isolated-status', ['git', 'status', '--short', '--branch'], workspace)
run('isolated-source-diff', ['git', 'diff', '--cached', '--submodule=short'], workspace)
(EVIDENCE/'selected-inputs.json').write_text(json.dumps(dict(workspace=str(workspace), head=owners['projects']['head'], inputs=selected, original_dirty_files_copied=False, qualification='Source verification only; no package/runtime/Story acceptance'), indent=2)+'\n')
print(workspace)
