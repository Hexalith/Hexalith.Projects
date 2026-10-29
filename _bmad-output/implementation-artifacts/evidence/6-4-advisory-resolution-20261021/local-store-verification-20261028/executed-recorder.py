"""Run existing Story 6.4 source suites against command-owned local Redis/Dapr."""
from pathlib import Path
import datetime
import hashlib
import json
import os
import shutil
import signal
import socket
import subprocess
import time
import urllib.request
import xml.etree.ElementTree as ET

ROOT = Path('/home/administrator/projects/hexalith/projects')
WORK = Path(__file__).resolve().parent
EVIDENCE = ROOT / '_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/local-store-verification-20261028'
EVIDENCE.mkdir(exist_ok=True)
COMMANDS = []
RESULTS = {}
CHILDREN = []


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def write(name, value):
    (EVIDENCE / name).write_text(json.dumps(value, indent=2) + '\n')


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def state():
    result = {}
    for repo in [ROOT] + sorted((ROOT / 'references').glob('Hexalith.*')):
        if not (repo / '.git').exists():
            continue
        result[str(repo.relative_to(ROOT))] = {
            'head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repo, text=True).strip(),
            'index_sha256': hashlib.sha256(subprocess.check_output(['git', 'ls-files', '--stage'], cwd=repo)).hexdigest(),
        }
    return result


def sources():
    result = {}
    for repo in [ROOT] + sorted((ROOT / 'references').glob('Hexalith.*')):
        if not (repo / '.git').exists():
            continue
        names = subprocess.check_output(['git', 'ls-files', '-z'], cwd=repo).decode().split('\0')
        for name in names:
            path = repo / name
            if name and path.is_file() and (name.startswith(('src/', 'tests/', 'test/', 'tools/', 'docs/', 'module/')) or '/' not in name) and '/bin/' not in name and '/obj/' not in name:
                result[str(path.relative_to(ROOT))] = sha(path)
    for path in (ROOT / 'module').glob('*'):
        if path.is_file():
            result[str(path.relative_to(ROOT))] = sha(path)
    return result


def run(name, argv, cwd=ROOT, explicit=None, private=False):
    environment = os.environ.copy()
    environment.update(explicit or {})
    start = now()
    target = WORK if private else EVIDENCE
    with (target / f'{name}.stdout').open('wb') as stdout, (target / f'{name}.stderr').open('wb') as stderr:
        result = subprocess.run(argv, cwd=cwd, env=environment, stdout=stdout, stderr=stderr)
    COMMANDS.append({'name': name, 'argv': [str(a) for a in argv], 'cwd': str(cwd), 'environment': explicit or {},
                     'started': start, 'finished': now(), 'exit_code': result.returncode, 'private_output': private})
    write('commands.json', COMMANDS)
    print(f'{name}: exit {result.returncode}', flush=True)
    return result.returncode


def port():
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        return sock.getsockname()[1]


def launch(name, argv):
    stdout = (WORK / f'private-{name}.stdout').open('wb')
    stderr = (WORK / f'private-{name}.stderr').open('wb')
    process = subprocess.Popen(argv, cwd=WORK, stdout=stdout, stderr=stderr, start_new_session=True)
    CHILDREN.append((name, process, stdout, stderr))
    COMMANDS.append({'name': name, 'argv': [str(a) for a in argv], 'cwd': str(WORK), 'environment': {},
                     'started': now(), 'pid': process.pid, 'private_output': True})
    write('commands.json', COMMANDS)
    return process


def suite(name, assembly, environment, selectors=None):
    argv = ['dotnet', str(assembly)]
    for kind, value in selectors or []:
        argv += [kind, value]
    argv += ['-result-xml', str(EVIDENCE / f'{name}.xml'), '-failSkips']
    code = run(name, argv, explicit=environment, private=True)
    tree = ET.parse(EVIDENCE / f'{name}.xml')
    assembly_node = tree.find('assembly')
    summary = {key: int(assembly_node.get(key)) for key in ['total', 'passed', 'failed', 'skipped', 'errors', 'not-run']}
    summary['nonpassing'] = [{'name': test.get('name'), 'result': test.get('result'), 'message': test.findtext('failure/message', '')}
                             for test in tree.findall('.//test') if test.get('result') != 'Pass']
    RESULTS[name] = summary
    write('test-results.json', RESULTS)
    # Retain the test summary, not the private application/runtime stdout.
    (EVIDENCE / f'{name}.summary.txt').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps({name: summary}), flush=True)
    assert code == 0 and summary['failed'] == summary['skipped'] == summary['errors'] == summary['not-run'] == 0


before = state()
source_before = sources()
write('git-before.json', before)
write('source-hashes.json', source_before)
spec = ROOT / '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
shutil.copy2(spec, WORK / 'spec-before.md')
try:
    owner = ROOT / 'references/Hexalith.Conversations'
    base = ['dotnet', 'tool', 'run', 'hexalith-module', '--', 'prepare-build']
    assert run('projects-source-prepare', base + ['--manifest', str(ROOT / 'module/hexalith.module-manifest.v2.json'), '--mode', 'source', '--workspace', str(WORK / 'projects-source'), '--output', 'json'], cwd=owner) == 0
    environment = json.loads((EVIDENCE / 'projects-source-prepare.stdout').read_text())['buildPreparation']['environment']
    for short in ['Contracts', 'Server', '']:
        project = 'Hexalith.Projects' + ('.' + short if short else '') + '.Tests'
        assert run(project + '-build', ['dotnet', 'build', f'tests/{project}/{project}.csproj', '-c', 'Debug', '-m:1', '-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0', '-v:minimal'], explicit=environment) == 0
    redis_port, http_port, grpc_port, internal_port, public_port = [port() for _ in range(5)]
    components = WORK / 'components'
    components.mkdir()
    app_id = 'story64-local-' + str(os.getpid())
    component = f'''apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: "127.0.0.1:{redis_port}"
  - name: redisPassword
    value: ""
scopes:
- {app_id}
'''
    (components / 'statestore.yaml').write_text(component)
    (EVIDENCE / 'statestore.yaml').write_text(component)
    redis = WORK / 'redis-extracted/usr/bin/redis-server'
    write('runtime-provenance.json', {'redis_package': 'redis-server=5:8.0.5-1ubuntu0.1',
           'package_sha256': sha(next(WORK.glob('*.deb'))), 'redis_binary': str(redis),
           'redis_binary_sha256': sha(redis), 'installed_tools_binary_sha256': sha('/usr/bin/redis-check-rdb'),
           'redis_version': subprocess.check_output([str(redis), '--version'], text=True).strip(),
           'daprd_sha256': sha('/home/administrator/.dapr/bin/daprd'),
           'daprd_version': subprocess.check_output(['/home/administrator/.dapr/bin/daprd', '--version'], text=True).strip(),
           'system_packages_changed': False})
    launch('redis', [str(redis), '--bind', '127.0.0.1', '--port', str(redis_port), '--save', '', '--appendonly', 'no', '--dir', str(WORK), '--protected-mode', 'yes'])
    for _ in range(100):
        ready = subprocess.run(['redis-cli', '-h', '127.0.0.1', '-p', str(redis_port), 'PING'], capture_output=True, text=True)
        if ready.returncode == 0 and ready.stdout.strip() == 'PONG':
            break
        time.sleep(0.1)
    else:
        raise RuntimeError('command-owned Redis did not become ready')
    dapr = launch('daprd', ['/home/administrator/.dapr/bin/daprd', '--app-id', app_id, '--resources-path', str(components),
        '--dapr-http-port', str(http_port), '--dapr-grpc-port', str(grpc_port), '--dapr-internal-grpc-port', str(internal_port),
        '--dapr-public-port', str(public_port), '--dapr-listen-addresses', '127.0.0.1', '--dapr-public-listen-address', '127.0.0.1',
        '--dapr-internal-grpc-listen-address', '127.0.0.1', '--enable-metrics=false', '--scheduler-host-address', '', '--log-level', 'warn'])
    for _ in range(200):
        assert dapr.poll() is None, 'Dapr exited before readiness'
        try:
            with urllib.request.urlopen(f'http://127.0.0.1:{http_port}/v1.0/healthz/outbound', timeout=0.5) as response:
                if response.status == 204:
                    break
        except OSError:
            time.sleep(0.1)
    else:
        raise RuntimeError('command-owned Dapr did not become ready')
    runtime_environment = {**environment, 'STORY61_DAPR_HTTP_ENDPOINT': f'http://127.0.0.1:{http_port}', 'STORY61_DAPR_GRPC_ENDPOINT': f'http://127.0.0.1:{grpc_port}'}
    server = ROOT / 'tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll'
    suite('gateway-persistence-focused', server, runtime_environment, [('-class', '*SupportedProjectGatewayHttpTests'), ('-method', '*SupportedProjectReadHttpTests.ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart')])
    suite('projects-server-full', server, runtime_environment)
    suite('projects-contracts-full', ROOT / 'tests/Hexalith.Projects.Contracts.Tests/bin/Debug/net10.0/Hexalith.Projects.Contracts.Tests.dll', environment)
    suite('projects-engine-confirmation', ROOT / 'tests/Hexalith.Projects.Tests/bin/Debug/net10.0/Hexalith.Projects.Tests.dll', environment)
    assert run('conversations-source-prepare', base + ['--manifest', str(owner / 'module/hexalith.module-manifest.v2.json'), '--mode', 'source', '--workspace', str(WORK / 'owner-source'), '--output', 'json'], cwd=owner) == 0
    owner_environment = json.loads((EVIDENCE / 'conversations-source-prepare.stdout').read_text())['buildPreparation']['environment']
    assert run('conversations-server-build', ['dotnet', 'build', 'tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj', '-c', 'Debug', '-m:1', '-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0', '-v:minimal'], cwd=owner, explicit=owner_environment) == 0
    suite('conversations-server-full', owner / 'tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0/Hexalith.Conversations.Server.Tests.dll', owner_environment)
finally:
    cleanup = []
    for name, process, stdout, stderr in reversed(CHILDREN):
        if process.poll() is None:
            os.killpg(process.pid, signal.SIGTERM)
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGKILL)
            process.wait(timeout=5)
        stdout.close()
        stderr.close()
        cleanup.append({'name': name, 'pid': process.pid, 'exit_code': process.returncode, 'stopped': process.poll() is not None})
    write('cleanup.json', cleanup)
    after = state()
    source_after = sources()
    write('git-after.json', after)
    write('preservation.json', {'git_heads_and_indices_unchanged': before == after,
           'source_bytes_unchanged': source_before == source_after, 'source_count': len(source_before),
           'spec_bytes_unchanged_during_execution': sha(spec) == sha(WORK / 'spec-before.md')})
    assert before == after and source_before == source_after
