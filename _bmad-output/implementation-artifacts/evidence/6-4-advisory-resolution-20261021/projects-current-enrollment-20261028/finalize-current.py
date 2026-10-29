"""Retain the exact two-file candidate patch and the current narrow results."""
from run import *
import xml.etree.ElementTree as ET
import shutil

workspace = WORK / 'projects'
parts = []
for label, name in [('declaration', 'module/hexalith.module-manifest.v2.json'), ('documentation', 'module/README.md')]:
    run('patch-' + label, ['git', 'diff', '--no-index', '--', '/dev/null', name], workspace, check=False)
    parts.append((EVIDENCE / ('patch-' + label + '.stdout')).read_bytes())
patch = EVIDENCE / 'projects-current-enrollment.UNAPPLIED.patch'
patch.write_bytes(b''.join(parts))
run('original-two-file-patch-apply-check', ['git', 'apply', '--check', str(patch)], ROOT)
for name in ['module/hexalith.module-manifest.v2.json', 'module/README.md', 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj', 'tools/release-packages.json']:
    target = EVIDENCE / 'candidate-source' / name
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(workspace / name, target)
(EVIDENCE / 'candidate-hashes.json').write_text(json.dumps({name: digest(workspace / name) for name in ['module/hexalith.module-manifest.v2.json', 'module/README.md', 'src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj', 'tools/release-packages.json']}, indent=2) + '\n')
results = []
for label in ['contracts', 'server']:
    xml = EVIDENCE / ('current-' + label + '-tests.xml')
    assembly = ET.parse(xml).find('assembly')
    results.append({'xml': xml.name, 'assembly': dict(assembly.attrib), 'cases': [dict(t.attrib) for t in assembly.findall('.//test')]})
(EVIDENCE / 'test-results.json').write_text(json.dumps(results, indent=2) + '\n')
assert sum(int(r['assembly']['passed']) for r in results) == 181
assert sum(int(r['assembly']['failed']) for r in results) == 1
before = json.loads((EVIDENCE / 'executables-before.json').read_text())
after = {}
for layer in before:
    out = workspace / f'tests/Hexalith.Projects.{layer}.Tests/bin/Debug/net10.0'
    after[layer] = {str(p.relative_to(out)): digest(p) for p in sorted(out.rglob('*')) if p.is_file()}
(EVIDENCE / 'executables-after.json').write_text(json.dumps(after, indent=2) + '\n')
assert before == after, 'Compiled executable inputs changed during the tests'
run('runtime-final-containers', ['docker', 'ps', '--all', '--no-trunc', '--format', '{{.ID}}'], workspace)
initial = set((EVIDENCE / 'runtime-existing-containers.stdout').read_text().splitlines())
final = set((EVIDENCE / 'runtime-final-containers.stdout').read_text().splitlines())
assert initial == final, 'Container namespace changed during the bounded fixture attempts'
blocker = {'current_narrow_results': {'passed': 181, 'failed': 1, 'skipped': 0, 'errors': 0, 'not_run': 0}, 'failed_case': 'Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadHttpTests.ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart', 'failure': 'FAIL_SKIP: Requires the isolated Story 6.1 Dapr persistence lane.', 'initial_redis_fixture': {'command': 'current-runtime-runtime-redis-create', 'exit_code': 125, 'cause': 'Docker loopback port publishing /forwards/expose returned HTTP 500'}, 'local_redis_server_probe_exit': 127, 'host_network_fallback': {'redis_create_exit': 0, 'daprd_exit': 1, 'stage': 'before outbound readiness', 'component': 'statestore (state.redis/v1)', 'cause': 'Dapr could not connect to fixture Redis at 127.0.0.1:40621: connection refused', 'private_runtime_logs_archived': False, 'sidecar_exited': True, 'container_absent': True}, 'container_namespace_preserved': True, 'needed_prerequisite': 'A fixture Redis endpoint reachable from the local Dapr process; Docker published ports currently fail and Docker host-network loopback is not reachable from that process.', 'source_fix_needed': False, 'story_complete': False}
(EVIDENCE / 'runtime-prerequisite-blocker.json').write_text(json.dumps(blocker, indent=2) + '\n')
print('Two-file patch prepared; 181 unique passes and one unchanged fixture-prerequisite failure retained.')
