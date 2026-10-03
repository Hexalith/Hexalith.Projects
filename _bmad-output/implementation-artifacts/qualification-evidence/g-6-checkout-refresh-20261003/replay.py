"""Verify the archived artifact and replay G-6 validation without runtime tests."""

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import tempfile
import zipfile
from datetime import datetime, timezone
from pathlib import Path


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-workspace', type=Path, required=True)
    parser.add_argument('--receipt-directory', type=Path, required=True)
    options = parser.parse_args()
    source = options.source_workspace.resolve()
    output = options.receipt_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    evidence = Path(__file__).resolve().parent
    archived = evidence / 'ci-37118950623-1'
    result = json.loads((archived / 'result.json').read_bytes())
    artifacts = json.loads((evidence / 'ci-37118950623-artifacts.json').read_bytes())
    artifact = next(item for item in artifacts['artifacts'] if item['id'] == 11273215715)
    archive_path = evidence / 'ci-37118950623-artifact.zip'
    assert 'sha256:' + sha256(archive_path) == artifact['digest']
    canonical = json.dumps({key: value for key, value in result.items() if key != 'artifactSha256'},
                           sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()
    assert hashlib.sha256(canonical).hexdigest() == result['artifactSha256']
    with zipfile.ZipFile(archive_path) as archive:
        for item in archive.infolist():
            if not item.is_dir():
                path = Path(item.filename)
                assert not path.is_absolute() and '..' not in path.parts
                assert archive.read(item.filename) == (archived / path).read_bytes()
    environment = dict(os.environ, PYTHONDONTWRITEBYTECODE='1')
    receipt = {'startedAtUtc': datetime.now(timezone.utc).isoformat(), 'preparationCommands': [],
               'graphInputs': [], 'uploadedZipDigestVerified': True, 'zipMembersByteIdentical': True,
               'canonicalResultDigestVerified': True}
    with tempfile.TemporaryDirectory(prefix='hexalith-g6-replay-', dir='/var/tmp') as temporary:
        workspace = Path(temporary) / 'workspace'
        bindings = [('.', result['audit']['source']['rootSha'])]
        bindings.extend((item['path'], item['sha']) for item in result['audit']['source']['gitlinks'])
        for relative, revision in bindings:
            original = source if relative == '.' else source / relative
            target = workspace if relative == '.' else workspace / relative
            commands = [['git', 'clone', '--shared', '--no-checkout', str(original), str(target)],
                        ['git', '-C', str(target), '-c', 'core.autocrlf=false', 'checkout', '--detach', revision]]
            for command in commands:
                subprocess.run(command, env=environment, check=True, capture_output=True)
                receipt['preparationCommands'].append(command)
        policy_path = workspace / 'references/Hexalith.Builds/Tools/g6-current-policy.json'
        policy = json.loads(policy_path.read_bytes())
        for relative in policy['resolvedProjects']:
            assets = Path(relative).parent / 'obj/project.assets.json'
            target = workspace / assets
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source / assets, target)
            receipt['graphInputs'].append({'path': assets.as_posix(), 'sha256': sha256(target)})
        run_directory = workspace / Path(result['qualification']['receipts']['captureDirectory']).parent
        shutil.copytree(archived, run_directory)
        validator = workspace / 'references/Hexalith.Builds/Tools/g6_current.py'
        audit_command = ['python3', str(validator), 'audit', '--workspace', str(workspace),
                         '--policy', str(policy_path), '--out', str(output / 'audit.json')]
        subprocess.run(audit_command, cwd=workspace, env=environment, check=True, capture_output=True)
        command = ['python3', str(validator), 'validate', '--workspace', str(workspace),
                   '--policy', str(policy_path), '--evidence', str(run_directory / 'result.json')]
        completed = subprocess.run(command, cwd=workspace, env=environment, capture_output=True)
        (output / 'validate.log').write_bytes(completed.stdout + completed.stderr)
        receipt.update(command=command, auditCommand=audit_command, workingDirectory=str(workspace),
                       validatorSha256=sha256(validator), validatorRevision=dict(bindings)['references/Hexalith.Builds'],
                       exitCode=completed.returncode)
    receipt['temporaryCheckoutRemoved'] = not workspace.exists()
    receipt['completedAtUtc'] = datetime.now(timezone.utc).isoformat()
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print((output / 'validate.log').read_text())
    return receipt['exitCode']


if __name__ == '__main__':
    raise SystemExit(main())
