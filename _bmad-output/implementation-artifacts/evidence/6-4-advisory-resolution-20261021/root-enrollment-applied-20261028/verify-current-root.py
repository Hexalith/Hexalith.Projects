"""Record the bounded verification of the installed Projects declaration."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET

evidence = Path(__file__).resolve().parent
root = evidence.parents[4]
owner = root / 'references/Hexalith.Conversations'
manifest = root / 'module/hexalith.module-manifest.v2.json'
workspace = Path(tempfile.mkdtemp(prefix='story64-applied-root-'))
ledger = []


def git_state():
    return {
        'head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(),
        'index_sha256': hashlib.sha256(subprocess.check_output(['git', 'ls-files', '--stage'], cwd=root)).hexdigest(),
    }


def run(name, argv, cwd=root, explicit=None):
    explicit = explicit or {}
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    environment = os.environ.copy()
    environment.update(explicit)
    with (evidence / f'{name}.stdout').open('wb') as stdout, (evidence / f'{name}.stderr').open('wb') as stderr:
        result = subprocess.run(argv, cwd=cwd, env=environment, stdout=stdout, stderr=stderr)
    ledger.append({'name': name, 'argv': argv, 'cwd': str(cwd), 'environment': explicit,
                   'started': started, 'finished': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                   'exit_code': result.returncode})
    (evidence / 'commands.json').write_text(json.dumps(ledger, indent=2) + '\n')
    print(f'{name}: exit {result.returncode}', flush=True)
    return result.returncode


before = git_state()
assert before['head'] == '22c99ad1ae0aa88cd563f2d85346c9d680acc2ca'
base = ['dotnet', 'tool', 'run', 'hexalith-module', '--']
assert run('validate', base + ['validate', '--manifest', str(manifest), '--output', 'json'], owner) == 0
assert run('source-prepare', base + ['prepare-build', '--manifest', str(manifest), '--mode', 'source',
           '--workspace', str(workspace / 'source'), '--output', 'json'], owner) == 0
prepared = json.loads((evidence / 'source-prepare.stdout').read_text())
environment = prepared['buildPreparation']['environment']
assert len(environment) == 7
assert run('package-prepare', base + ['prepare-build', '--manifest', str(manifest), '--mode', 'package',
           '--workspace', str(workspace / 'package'), '--output', 'json'], owner) == 2
for short in ['Contracts', 'Server']:
    project = f'tests/Hexalith.Projects.{short}.Tests/Hexalith.Projects.{short}.Tests.csproj'
    assert run(f'{short.lower()}-build', ['dotnet', 'build', project, '-c', 'Debug', '-m:1',
               '-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0', '-v:minimal'], explicit=environment) == 0
for short, classes, expected in [
    ('Contracts', ['ProjectAdvisoryContractTests'], 0),
    ('Server', ['ProjectAdvisoryQueryTests', 'DelegatedProjectAdvisoryConversationDirectoryTests',
                'DelegatedProjectDirectoryRegistrationTests', 'ProjectQueryEnvelopePrincipalBindingTests',
                'ProjectQueryTenantFilterTests', 'SupportedProjectReadHttpTests'], 1),
]:
    assembly = f'tests/Hexalith.Projects.{short}.Tests/bin/Debug/net10.0/Hexalith.Projects.{short}.Tests.dll'
    argv = ['dotnet', assembly]
    for selected in classes:
        argv += ['-class', '*' + selected]
    argv += ['-result-xml', str(evidence / f'{short.lower()}-tests.xml'), '-failSkips']
    assert run(f'{short.lower()}-tests', argv, explicit=environment) == expected
results = {}
for short in ['contracts', 'server']:
    assembly = ET.parse(evidence / f'{short}-tests.xml').find('assembly')
    results[short] = {key: int(assembly.get(key)) for key in ['total', 'passed', 'failed', 'skipped', 'errors', 'not-run']}
    results[short]['nonpassing'] = [
        {'name': test.get('name'), 'message': test.findtext('failure/message', '')}
        for test in assembly.findall('.//test') if test.get('result') != 'Pass'
    ]
assert results['contracts']['passed'] == 8 and results['contracts']['failed'] == 0
assert results['server']['passed'] == 173 and results['server']['failed'] == 1
assert 'FAIL_SKIP' in results['server']['nonpassing'][0]['message']
after = git_state()
assert before == after
files = ['module/hexalith.module-manifest.v2.json', 'module/README.md', 'tools/release-packages.json']
summary = {'before': before, 'after': after, 'results': results,
           'files': {name: hashlib.sha256((root / name).read_bytes()).hexdigest() for name in files},
           'story_complete': False, 'source_mapping': environment}
(evidence / 'results.json').write_text(json.dumps(summary, indent=2) + '\n')
print(json.dumps(results), flush=True)
