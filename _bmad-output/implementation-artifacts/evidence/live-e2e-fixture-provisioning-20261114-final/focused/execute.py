"""Reproduce final source checks with all disposable outputs beneath the supplied TMPDIR."""
import ast
import gzip
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
ROOT = Path('/home/administrator/projects/hexalith/projects')
sys.path.insert(0, str(ROOT / 'tools/qualification'))
import run_supported_project_reads as support
from supported_browser_reads import sanitize_output

EVIDENCE = Path(__file__).parent
REPORT = {'commands': [], 'testProcessCleanup': [], 'result': 'failed'}
PRIVATE = Path(tempfile.mkdtemp(prefix='projects-fixtures-final-'))
ENV = support.sanitized_environment(os.environ)
ENV.update(UseHexalithProjectReferences='true', UseNuGetDeps='false', UseArtifactsOutput='true',
    ArtifactsPath=str(PRIVATE / 'artifacts'), E2E_LIVE_APPHOST='0', E2E_SUPPORTED_READS_ONLY='0',
    PROJECTS_ISOLATED_LIVE_FIXTURES='0', PYTHONDONTWRITEBYTECODE='1', MSBUILDDISABLENODEREUSE='1',
    DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1')
for key in ('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets',
    'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH', 'Configuration',
    'SUPPORTED_PROJECT_READ_CONFIG', 'SUPPORTED_PROJECT_READ_ENDPOINTS', 'SUPPORTED_PROJECT_READ_EVIDENCE'):
    ENV.pop(key, None)
for name in ('EventStore', 'Tenants', 'Folders', 'Commons', 'Conversations', 'Memories', 'FrontComposer',
    'Parties', 'McpCli', 'Builds', 'Platform'):
    ENV['Hexalith' + name + 'Root'] = str(ROOT / 'references' / ('Hexalith.' + name))
ENV['HexalithTenantsBasePath'] = str(ROOT / 'references/Hexalith.Tenants/src')

def run(name, command, *, allow_failure=False, timeout=600):
    print('[final-checks] ' + name, flush=True)
    record = {'name': name, 'argv': command}
    REPORT['commands'].append(record)
    try:
        result = support.run_test_process(command, env=ENV, timeout=timeout, cleanup=REPORT['testProcessCleanup'])
    except subprocess.TimeoutExpired as error:
        raw = error.stdout or ''
        if isinstance(raw, bytes):
            raw = raw.decode(errors='replace')
        (EVIDENCE / (name + '.log')).write_text(sanitize_output(raw, []))
        record['timeout'] = True
        raise
    (EVIDENCE / (name + '.log')).write_text(sanitize_output(result.stdout, []))
    record['exitCode'] = result.returncode
    if result.returncode and not allow_failure:
        raise RuntimeError(name + ' failed with exit ' + str(result.returncode))
    return result

def xunit(name):
    value = ET.parse(EVIDENCE / (name + '.xml')).getroot().find('assembly')
    result = {key: int(value.get(key, '0')) for key in ('total', 'passed', 'failed', 'skipped', 'errors')}
    REPORT[name] = result
    if result['failed'] or result['skipped'] or result['errors'] or not result['passed']:
        raise RuntimeError(name + ' native result did not pass without skips')

try:
    initial = support.source_manifest()
    raw = json.dumps(initial, sort_keys=True).encode()
    with gzip.open(EVIDENCE / 'source-manifest.json.gz', 'wb') as output:
        output.write(raw)
    REPORT['sourceManifestSha256'] = hashlib.sha256(raw).hexdigest()
    tooling = PRIVATE / 'tool-artifacts'
    run('tool-build', ['dotnet', 'build', str(ROOT / 'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj'),
        '-c', 'Debug', '-m:1', '-p:UseSharedCompilation=false', '--artifacts-path', str(tooling)])
    cli = list((tooling / 'bin/Hexalith.Builds.Module.Cli').glob('debug*/Hexalith.Builds.Module.Cli.dll'))
    if len(cli) != 1:
        raise RuntimeError('Fresh source tooling assembly is missing or ambiguous')
    preparation = json.loads(run('source-preparation', ['dotnet', str(cli[0]), 'prepare-build', '--manifest',
        'module/hexalith.module-manifest.v2.json', '--mode', 'source', '--workspace', str(PRIVATE / 'mapping'), '--output', 'json']).stdout)
    if preparation.get('status') != 'prepared':
        raise RuntimeError('Supported source preparation did not complete')
    selected = preparation['buildPreparation']['environment']
    support.require_mapping_environment(selected)
    ENV.update(selected)
    REPORT['sourceMappingHash'] = selected['HEXALITH_SOURCE_MAPPING_HASH']
    common = ['-c', 'Debug', '-m:1', '-p:UseSharedCompilation=false', '-p:SupportedProjectReadDistributed=false',
        '--artifacts-path', str(PRIVATE / 'artifacts')]
    broad = run('solution-build', ['dotnet', 'build', 'Hexalith.Projects.slnx', *common], allow_failure=True, timeout=900)
    REPORT['solutionBuildPassed'] = broad.returncode == 0
    run('integration-build', ['dotnet', 'build', str(support.TEST_PROJECT), *common])
    run('integration', ['dotnet', str(PRIVATE / 'artifacts/bin/Hexalith.Projects.Integration.Tests/debug/Hexalith.Projects.Integration.Tests.dll'),
        '-xml', str(EVIDENCE / 'integration.xml')])
    xunit('integration')
    run('server-build', ['dotnet', 'build', 'tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj', *common])
    run('catalog-guards', ['dotnet', str(PRIVATE / 'artifacts/bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll'),
        '-class', '*ProjectsRuntimeCatalogTests', '-class', '*ProjectsWorkerCatalogTests', '-xml', str(EVIDENCE / 'catalog-guards.xml')])
    xunit('catalog-guards')
    run('python-controls', ['python3', '-m', 'unittest', 'discover', '-s', 'tests/tools', '-p', 'test_*project*launcher.py'])
    run('typecheck', ['npm', '--prefix', 'tests/e2e', 'run', 'typecheck'])
    ENV['PLAYWRIGHT_JUNIT_OUTPUT_FILE'] = str(EVIDENCE / 'typescript-controls.xml')
    run('typescript-controls', ['npm', '--prefix', 'tests/e2e', 'test', '--', '--workers=2', '--project=chromium',
        'specs/tenant-access-readiness.spec.ts', 'specs/live-fixture-identities.spec.ts', 'specs/live-fixtures-lifecycle.spec.ts', 'specs/api-transport-budget.spec.ts',
        'specs/project-fixture-cleanup.spec.ts', 'specs/browser-traffic-observer.spec.ts',
        '--grep', 'helper contracts|identity factory|no app required|archive cleanup diagnostics contract|browser traffic header observer',
        '--reporter=list,junit', '--output', str(PRIVATE / 'controls')])
    ts = ET.parse(EVIDENCE / 'typescript-controls.xml').getroot()
    REPORT['typescript-controls'] = {key: int(ts.get(key, '0')) for key in ('tests', 'failures', 'errors', 'skipped')}
    if not REPORT['typescript-controls']['tests'] or any(REPORT['typescript-controls'][key] for key in ('failures', 'errors', 'skipped')):
        raise RuntimeError('TypeScript controls did not pass without skips')
    run('shell-syntax', ['bash', '-n', 'tests/e2e/run-live-apphost.sh'])
    python_paths = ['tools/qualification/run_live_project_fixtures.py', 'tools/qualification/supported_browser_reads.py',
        'tests/tools/test_live_project_fixtures_launcher.py', 'tests/tools/test_supported_project_reads_launcher.py']
    for relative in python_paths:
        ast.parse((ROOT / relative).read_text(), filename=relative)
    REPORT['pythonSyntaxPaths'] = python_paths
    run('diff-check', ['git', 'diff', '--check'])
    REPORT['sourceInputsStable'] = support.source_manifest() == initial
    if not REPORT['sourceInputsStable']:
        raise RuntimeError('Source inputs changed during final verification')
    REPORT['result'] = 'passed'
except BaseException as failure:
    REPORT['failure'] = str(failure) if isinstance(failure, RuntimeError) else type(failure).__name__
finally:
    shutil.rmtree(PRIVATE)
    REPORT['privateMaterialRemoved'] = not PRIVATE.exists()
    (EVIDENCE / 'summary.json').write_text(json.dumps(REPORT, indent=2) + '\n')
print('[final-checks] ' + REPORT['result'] + '; evidence: ' + str(EVIDENCE), flush=True)
raise SystemExit(0 if REPORT['result'] == 'passed' else 1)
