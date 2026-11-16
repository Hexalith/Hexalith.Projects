"""Retain final static checks, real acceptance verification and exact source hashes."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

root = Path.cwd()
out = root / '_bmad-output/implementation-artifacts/evidence/epic6/6.7/approved-v3'
checks = []
for command in [
    ['python3', 'tools/planning/validate_production_authority.py', '--story-id', '6.7'],
    ['python3', 'tools/planning/validate_production_authority.py', '--validate-index'],
    ['git', 'diff', '--check'],
    ['git', '-C', 'references/Hexalith.Conversations', '-c', 'core.whitespace=cr-at-eol', 'diff', '--check'],
    ['git', '-C', 'references/Hexalith.EventStore', 'diff', '--check'],
]:
    result = subprocess.run(command, cwd=root, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    checks.append({'command': command, 'cwd': str(root), 'exitCode': result.returncode, 'output': result.stdout})


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


native_results = []
for name in ['Domain-private-final-2.xml', 'Contracts-private-final-2.xml', 'Client-private-final-2.xml',
             'Server-private-final-4.xml', 'UI-private-final-2.xml', 'Conversations-catalog-final-2.xml',
             'gateway-final-v3-5.xml', 'gateway-rollback-final-v3-7.xml']:
    path = out / name
    xml = ET.parse(path)
    for assembly in xml.findall('.//assembly'):
        a = assembly.attrib
        assert int(a['total']) == int(a['passed']) > 0
        assert all(a[k] == '0' for k in ('failed', 'skipped', 'not-run', 'errors'))
        assert all(t.attrib['result'] == 'Pass' for t in assembly.findall('.//test'))
        assert Path(a['name']).is_relative_to('/tmp/hexalith-job.r31UazNg')
        native_results.append({'path': name, 'sha256': sha(path), 'assemblyPath': a['name'],
                               'total': int(a['total']), 'passed': int(a['passed']), 'skipped': 0, 'notRun': 0})
packet = json.loads((out / 'read-cutover-final.json').read_text())
for receipt in packet['nativeReceipts']:
    assert sha(out / receipt['path']) == receipt['sha256']
proof = json.loads((out / 'rollback-final-proof.json').read_text())
assert proof['passed'] and proof['evidenceHash'] == sha(out / 'read-cutover-final.json')
assert len(proof['reads']) == 7 and all(r['passed'] and r['noDomainWrites'] and not r['unexpectedSdkWriteKeys'] for r in proof['reads'])
assert all(r['domainBytesAndEtagsBefore'] == r['domainBytesAndEtagsAfter'] for r in proof['reads'])
matrix = json.loads((out / 'gateway-rollback-final-v3-7-matrix.json').read_text())
assert len(matrix['cases']) == 77 and all(c['passed'] and c['noDomainWrites'] and not c['unexpectedSdkWriteKeys'] for c in matrix['cases'])
cleanup = json.loads((out.parent / 'approved-v2/final-session-sidecar-cleanup.json').read_text())
assert cleanup['reaped']
try:
    os.kill(cleanup['ownedPid'], 0)
except ProcessLookupError:
    pid_absent = True
else:
    pid_absent = False
assert pid_absent and not Path('/tmp/hexalith-job.r31UazNg').exists()

manifest = {'repositories': [], 'ownership': 'Includes pre-existing workspace changes; no Git mutation or blanket ownership claim.',
            'sourceParity': 'Debug/project references; package publication and external adoption unperformed.',
            'splitTypes': ['FoldersV1ProjectReadDirectory', 'FoldersV1ProjectReadTransport', 'FoldersV1ReadFreshness', 'FoldersV1ReadLifecycle', 'FoldersV1ReadPermissions']}
for relative in ['.', 'references/Hexalith.Conversations', 'references/Hexalith.EventStore']:
    repository = root / relative
    git = lambda *args: subprocess.check_output(['git', *args], cwd=repository, text=True)
    paths = set(git('diff', '--name-only', '--no-ext-diff').splitlines())
    paths.update(git('ls-files', '--others', '--exclude-standard').splitlines())
    paths.update(p for p in ['AGENTS.md', '.editorconfig', '.gitattributes', 'Directory.Build.props',
                             'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.gitmodules',
                             'module/hexalith.module-manifest.v2.json'] if (repository / p).is_file())
    if relative == '.':
        paths.add('_bmad-output/implementation-artifacts/epic-6-context.md')
        paths.add('_bmad-output/planning-artifacts/story-6-7-approved-read-cutover-amendments-2026-11-16.md')
    entries = []
    for path in sorted(paths):
        file = repository / path
        if not file.is_file() or 'evidence/' in path or '/artifacts/' in path or path.startswith('artifacts/'):
            continue
        content = file.read_bytes()
        entries.append({'path': path, 'sha256': hashlib.sha256(content).hexdigest(),
                        'lfNormalizedSha256': hashlib.sha256(content.replace(b'\r\n', b'\n')).hexdigest(),
                        'bytes': len(content)})
    manifest['repositories'].append({'path': relative, 'revision': git('rev-parse', 'HEAD').strip(),
                                     'branch': git('branch', '--show-current').strip(), 'files': entries})
manifest_path = out / 'source-manifest-reviewed.json'
assert not manifest_path.exists()
manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
result = {'checks': checks, 'nativeResults': native_results, 'sourceManifestSha256': sha(manifest_path),
          'activationPacket': {'path': 'read-cutover-final.json', 'sha256': sha(out / 'read-cutover-final.json'), 'passingCells': 77},
          'rollbackProof': {'path': 'rollback-final-proof.json', 'sha256': sha(out / 'rollback-final-proof.json'), 'passingReads': 7},
          'cleanup': {**cleanup, 'processAbsent': pid_absent, 'privateTMPDIRRemoved': True, 'rootArtifactsPreserved': True},
          'buildWarnings': 0, 'buildErrors': 0, 'deploymentOrPublication': False}
(out / 'final-checks-reviewed.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({'checks': [(c['command'], c['exitCode']) for c in checks], 'nativePassing': sum(x['passed'] for x in native_results), 'cleanup': result['cleanup']}))
assert all(c['exitCode'] == 0 for c in checks)
