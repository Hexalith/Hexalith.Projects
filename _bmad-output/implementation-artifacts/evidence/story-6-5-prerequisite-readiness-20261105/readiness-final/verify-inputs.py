#!/usr/bin/env python3
"""Record source references and verify the retained owner repair without mutating it."""
import datetime
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

output = Path(__file__).resolve().parent
root = output.parents[4]
repair = output.parent / 'repair'
owner = root / 'references/Hexalith.FrontComposer'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def git(path, *args):
    return subprocess.check_output(['git', '-C', str(path), *args], text=True).strip()

retained = json.loads((repair / 'source-inputs.json').read_text())
assert git(owner, 'rev-parse', 'HEAD') == retained['frontcomposer_base']
assert digest(repair / retained['patch']) == retained['patch_sha256']
for row in retained['files']:
    assert digest(owner / row['path']) == row['sha256'], row['path']
for path, expected in retained['binaries'].items():
    assert digest(owner / path) == expected, path

preparation = json.loads((repair / 'prepare-projects-source-mapping.txt').read_text())
environment = preparation['buildPreparation']['environment']
mapping_dir = Path(environment['AfterMicrosoftNETSdkTargets']).parent
mapping = json.loads((mapping_dir / 'mapping.json').read_text())
assert mapping['ActiveModule'] == 'Hexalith.Projects'
assert mapping['Mode'] == 'source'
assert mapping['ContentHash'] == environment['HEXALITH_SOURCE_MAPPING_HASH']
saved_mapping = output / 'source-mapping'
saved_mapping.mkdir(exist_ok=True)
for path in mapping_dir.iterdir():
    if path.is_file():
        target = saved_mapping / path.name
        if target.exists():
            assert digest(target) == digest(path), str(path)
        else:
            shutil.copyfile(path, target)

source_files = set()
for tree in ('src/Hexalith.Projects.UI', 'tests/Hexalith.Projects.UI.Tests',
             'src/Hexalith.Projects.Contracts', 'src/Hexalith.Projects.Client',
             'src/Hexalith.Projects.Testing'):
    for rel in git(root, 'ls-files', tree).splitlines():
        source_files.add(root / rel)
for rel in ('AGENTS.md', '.editorconfig', '.gitattributes', 'Directory.Build.props',
            'Directory.Packages.props', 'global.json', 'nuget.config',
            'module/hexalith.module-manifest.v2.json',
            'src/Hexalith.Projects.AppHost/Program.cs',
            'src/Hexalith.Projects.Server/ProjectsServerModule.cs',
            'references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Gateway/IEventStoreGatewayClient.cs',
            'references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/EventStoreServiceCollectionExtensions.cs',
            'references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/SubmitQueryRequest.cs'):
    source_files.add(root / rel)
source_files.update(owner / row['path'] for row in retained['files'])
sources = [dict(path=str(path.relative_to(root)), sha256=digest(path))
           for path in sorted(source_files)]
assemblies = {}
for directory in (root / 'tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0',
                  owner / 'tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0'):
    for path in sorted(directory.glob('Hexalith.*.dll')):
        assemblies[str(path.relative_to(root))] = digest(path)
assert assemblies['tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll'] == retained['binaries']['tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll']
repositories = []
for entry in mapping['Entries']:
    path = Path(entry['Path'])
    repositories.append(dict(identity=entry['Identity'], path=str(path),
                             head=git(path, 'rev-parse', 'HEAD'),
                             branch=git(path, 'branch', '--show-current'),
                             status=git(path, 'status', '--short')))
manifest = dict(recorded_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                scope='Debug/source consumer verification against retained uncommitted FrontComposer repair',
                repaired_owner_commit=None, owner_source_manifest='../repair/source-inputs.json',
                owner_files_verified=len(retained['files']), owner_test_binaries_unchanged=True,
                repositories=repositories, files=sources, binaries=assemblies,
                mapping_environment=environment,
                mapping_files=[dict(path=path.name, sha256=digest(path))
                               for path in sorted(saved_mapping.iterdir())])
destination = output / 'source-inputs.json'
if destination.exists():
    prior = json.loads(destination.read_text())
    for field in ('files', 'binaries', 'mapping_environment', 'mapping_files'):
        assert prior[field] == manifest[field], 'Verification input drift: ' + field
else:
    destination.write_text(json.dumps(manifest, indent=2) + '\n')

print('PASS: all 17 owner source files, owner patch and prior tested owner assemblies are unchanged')
print('PASS: Projects source mapping', mapping['ContentHash'])
print('PASS:', len(sources), 'source/configuration fingerprints;', len(assemblies), 'assembly fingerprints')
print('Projects HEAD:', git(root, 'rev-parse', 'HEAD'))
print('FrontComposer base:', git(owner, 'rev-parse', 'HEAD'), '+ retained uncommitted repair')
