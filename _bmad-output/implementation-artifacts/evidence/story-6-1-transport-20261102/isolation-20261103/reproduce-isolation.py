from pathlib import Path
import collections, hashlib, importlib.util, json, os, shutil, subprocess, tempfile

shared = Path('/home/administrator/projects/hexalith/projects')
history = shared / '_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102'
packet = history / 'isolation-20261103'
packet.mkdir(mode=0o755, exist_ok=True)
expected = json.loads((history / 'live-56/source-manifest.json').read_text())
revisions = json.loads((history / 'live-56/duplicate/run.json').read_text())['revisions']
private = Path(tempfile.mkdtemp(prefix='story61-isolated-20261103-'))
os.chmod(private, 0o700)
workspace = private / 'projects'
cache = private / 'exact-inputs'
cache.mkdir(mode=0o700)

def run(args, cwd=shared):
    return subprocess.check_output(args, cwd=cwd, stderr=subprocess.PIPE)

def digest(data):
    return hashlib.sha256(data).hexdigest()

before = {}
for repository, revision in revisions.items():
    owner = shared / repository
    guidance = owner / 'AGENTS.md'
    if guidance.exists():
        content = guidance.read_text()
        if not content.startswith('# AI Assistant Instructions'):
            raise RuntimeError(f'Unknown owner guidance must be read: {guidance}')
    before[repository] = {name: run(command, owner).decode() for name, command in {
        'status': ['git', 'status', '--short', '--branch'],
        'remotes': ['git', 'remote', '-v'],
        'history': ['git', 'log', '-8', '--format=%H %s'],
        'head': ['git', 'rev-parse', 'HEAD'],
        'indexSha256Input': ['git', 'ls-files', '--stage'],
    }.items()}
    before[repository]['indexSha256'] = digest(before[repository].pop('indexSha256Input').encode())
    run(['git', 'cat-file', '-e', revision + '^{commit}'], owner)

provenance = {}
for relative, wanted in expected.items():
    source = shared / relative
    owner = Path(*source.relative_to(shared).parts[:2]) if relative.startswith('references/') else Path('.')
    local = '/'.join(Path(relative).parts[2:]) if relative.startswith('references/') else relative
    data = source.read_bytes() if source.is_file() else None
    origin = {'kind': 'shared-current-exact', 'path': str(source)}
    if data is None or digest(data) != wanted:
        data = None
        candidates = [('recorded', revisions[str(owner)]), ('HEAD', 'HEAD'), ('index', ':'), ('ours', ':2:'), ('theirs', ':3:'), ('base', ':1:')]
        if local == 'src/Hexalith.EventStore.Contracts/Routing/RouteCatalogCodec.cs':
            candidates.append(('retained-local-commit', '2c13c030753bcf1687c167af7912ae3878016604'))
        for label, coordinate in candidates:
            specifier = coordinate + local if coordinate.startswith(':') else coordinate + ':' + local
            try:
                raw = run(['git', 'show', specifier], shared / owner)
            except subprocess.CalledProcessError:
                continue
            for ending, candidate in [('raw', raw), ('lf', raw.replace(b'\r\n', b'\n')), ('crlf', raw.replace(b'\r\n', b'\n').replace(b'\n', b'\r\n'))]:
                if digest(candidate) == wanted:
                    data = candidate
                    origin = {'kind': 'local-git-object', 'repository': str(owner), 'specifier': specifier, 'materialization': ending, 'stage': label}
                    break
            if data is not None:
                break
    if data is None:
        raise RuntimeError(f'Intended source cannot be reproduced exactly: {relative}')
    target = cache / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
    provenance[relative] = {**origin, 'sha256': wanted}

commands = []
for repository, revision in sorted(revisions.items(), key=lambda pair: (pair[0] != '.', pair[0])):
    owner = shared / repository
    target = workspace if repository == '.' else workspace / repository
    target.parent.mkdir(parents=True, exist_ok=True)
    command = ['git', '-c', 'submodule.recurse=false', 'worktree', 'add', '--detach', str(target), revision]
    run(command, owner)
    commands.append({'cwd': str(owner), 'argv': command})

for relative in expected:
    target = workspace / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(cache / relative, target)

spec = importlib.util.spec_from_file_location('isolated_supported_reads', workspace / 'tools/qualification/run_supported_project_reads.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
actual = module.source_manifest()
extras = sorted(set(actual) - set(expected))
for relative in extras:
    (workspace / relative).unlink()
actual = module.source_manifest()
if actual != expected:
    raise RuntimeError('Complete isolated relative build-input manifest differs')

patches = []
for repository, revision in revisions.items():
    owner = workspace / repository
    if run(['git', 'rev-parse', 'HEAD'], owner).decode().strip() != revision:
        raise RuntimeError(f'Detached revision differs: {repository}')
    if subprocess.run(['git', 'symbolic-ref', '-q', 'HEAD'], cwd=owner, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode == 0:
        raise RuntimeError(f'Isolated repository is not detached: {repository}')
    diff = run(['git', 'diff', '--binary'], owner)
    path = private / ('root.patch' if repository == '.' else Path(repository).name + '.patch')
    path.write_bytes(diff)
    os.chmod(path, 0o600)
    untracked = run(['git', 'ls-files', '--others', '--exclude-standard'], owner).decode().splitlines()
    patches.append({'repository': repository, 'base': revision, 'patch': str(path), 'sha256': digest(diff), 'untracked': untracked})

(packet / 'source-manifest.json').write_text(json.dumps(actual, indent=2, sort_keys=True) + '\n')
(packet / 'input-provenance.json').write_text(json.dumps(provenance, indent=2, sort_keys=True) + '\n')
(packet / 'shared-repositories-before.json').write_text(json.dumps(before, indent=2) + '\n')
result = {'sharedRoot': str(shared), 'isolatedRoot': str(workspace), 'privateRoot': str(private), 'recordedRevisions': revisions,
          'referenceManifest': str(history / 'live-56/source-manifest.json'),
          'sourceManifestSha256': digest(json.dumps(actual, sort_keys=True).encode()), 'inputCount': len(actual),
          'inputProvenanceKinds': dict(collections.Counter(v['kind'] for v in provenance.values())),
          'deletedIsolatedExtraInputs': extras, 'worktreeCommands': commands, 'patches': patches,
          'nestedSubmodulesInitialized': False, 'sharedSourceIndexOrHeadMutation': False,
          'sourcePreparationOrQualificationPassed': False}
(packet / 'isolation.json').write_text(json.dumps(result, indent=2) + '\n')
Path('/tmp/story61-isolation-location.json').write_text(json.dumps({'packet': str(packet), 'workspace': str(workspace), 'private': str(private)}) + '\n')
os.chmod('/tmp/story61-isolation-location.json', 0o600)
print(json.dumps({'workspace': str(workspace), 'packet': str(packet), 'inputCount': len(actual), 'manifestSha256': result['sourceManifestSha256'], 'reproduction': 'exact'}, indent=2))
