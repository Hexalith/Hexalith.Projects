from pathlib import Path
import datetime, hashlib, importlib.util, json, shutil, subprocess

location = json.loads(Path('/tmp/story61-isolation-location.json').read_text())
root = Path(location['workspace'])
base = Path(location['packet'])
packet = base / 'final-source-20261103'
packet.mkdir(exist_ok=False)
(packet / 'patches').mkdir()
raw_root = packet / 'raw-changes'
raw_root.mkdir()
original = json.loads((base / 'isolation.json').read_text())
paths = ['src', 'tests', 'Props', 'tools/qualification', '*.props', '*.targets', '*.slnx', 'global.json', 'nuget.config', 'AGENTS.md', '.editorconfig', '.gitattributes']

def run(args, cwd):
    return subprocess.check_output(args, cwd=cwd, stderr=subprocess.PIPE)

def digest(value):
    return hashlib.sha256(value).hexdigest()

repositories = []
provenance = json.loads((base / 'input-provenance.json').read_text())
for repository, expected in sorted(original['recordedRevisions'].items()):
    owner = root / repository
    actual = run(['git', 'rev-parse', 'HEAD'], owner).decode().strip()
    assert actual == expected
    detached = subprocess.run(['git', 'symbolic-ref', '-q', 'HEAD'], cwd=owner, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode != 0
    assert detached
    patch = run(['git', 'diff', '--binary', 'HEAD', '--', *paths], owner)
    tracked = [s for s in run(['git', 'diff', '--name-only', '-z', 'HEAD', '--', *paths], owner).decode().split('\0') if s]
    untracked = sorted(set(s for s in run(['git', 'ls-files', '--others', '--exclude-standard', '-z', '--', *paths], owner).decode().split('\0') if s))
    for relative in untracked:
        result = subprocess.run(['git', 'diff', '--no-index', '--binary', '--', '/dev/null', relative], cwd=owner, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        assert result.returncode == 1, (repository, relative)
        patch += result.stdout
    name = 'Projects' if repository == '.' else Path(repository).name
    destination = packet / 'patches' / (name + '.patch')
    destination.write_bytes(patch)
    files = []
    for relative in sorted(set(tracked + untracked)):
        source = owner / relative
        complete = str(Path(repository) / relative)
        if not source.is_file():
            files.append({'path': relative, 'deleted': True})
            continue
        data = source.read_bytes()
        target = raw_root / complete
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        record = {'path': relative, 'untracked': relative in untracked, 'sha256': digest(data), 'size': len(data), 'rawCopy': str(target.relative_to(packet)), 'crlfCount': data.count(b'\r\n'), 'lfCount': data.count(b'\n')}
        files.append(record)
        provenance[complete] = {'kind': 'final-isolated-task-source', 'baseRevision': expected, 'sha256': digest(data), 'rawCopy': str(target.relative_to(packet)), 'originalProvenance': provenance.get(complete)}
    repositories.append({'repository': repository, 'revision': actual, 'detached': detached, 'patch': str(destination.relative_to(packet)), 'patchSha256': digest(patch), 'trackedChangedCount': len(tracked), 'untrackedSourceCount': len(untracked), 'sourceFiles': files})

spec = importlib.util.spec_from_file_location('launcher', root / 'tools/qualification/run_supported_project_reads.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
manifest = module.source_manifest()
assert digest(json.dumps(manifest, sort_keys=True).encode()) == 'b54edcdc7c47dc7eedf968b037b6e5c3b686607bb740d1d9e927c26090ada492'
(packet / 'source-manifest.json').write_text(json.dumps(manifest, sort_keys=True, indent=2) + '\n')
(packet / 'input-provenance.json').write_text(json.dumps(provenance, sort_keys=True, indent=2) + '\n')
summary = {'createdUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'isolatedRoot': str(root),
           'sourceManifestSha256': digest(json.dumps(manifest, sort_keys=True).encode()), 'buildInputCount': len(manifest),
           'repositories': repositories, 'sharedSourceCopiedBack': False, 'trackingOrIndexChangesPerformed': False,
           'untrackedTaskSourceIncluded': True, 'rawChangedSourceIncluded': True,
           'patchScope': paths, 'qualificationAccepted': False, 'live60Started': False,
           'pending': ['User-coordinated quiet runtime interval', 'Fresh same-source three-group aggregate and cleanup', 'Parent final review']}
(packet / 'manifest.json').write_text(json.dumps(summary, indent=2) + '\n')
shutil.copyfile(__file__, packet / 'prepare.py')
print(json.dumps({'packet': str(packet), 'sourceManifestSha256': summary['sourceManifestSha256'], 'repositories': len(repositories),
                  'trackedChanged': sum(r['trackedChangedCount'] for r in repositories), 'untrackedSourceFiles': sum(r['untrackedSourceCount'] for r in repositories)}, indent=2))
