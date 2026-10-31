#!/usr/bin/env python3
"""Focused preservation, launch-failure and child-failure recorder controls."""
import datetime
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile

helper = pathlib.Path(__file__).with_name('record-command-reviewed.py').resolve()
root = pathlib.Path(tempfile.mkdtemp(prefix='story64-recorder-controls-'))
rows = []


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def run(name, cwd=None, argv=None):
    prefix = root / name
    command = [sys.executable, '-O', str(helper), '--output', str(prefix), '--cwd',
        str(cwd or root), '--', *(argv or [sys.executable, '-c', 'print("ok")'])]
    result = subprocess.run(command, capture_output=True)
    rows.append({'case': name, 'argv': command, 'exitCode': result.returncode,
        'stdout': result.stdout.decode(), 'stderr': result.stderr.decode()})
    return result, prefix


for suffix in ['.stdout.log', '.stderr.log', '.command.json']:
    name = 'orphan-' + suffix.split('.')[1]
    protected = (root / name).with_suffix(suffix)
    protected.write_bytes(b'original orphan bytes\n')
    before = set(root.iterdir())
    result, prefix = run(name)
    require(result.returncode != 0 and protected.read_bytes() == b'original orphan bytes\n', name)
    require(set(root.iterdir()) == before, 'Refusal created another component')
for name, kind in [('symlink', 'symlink'), ('directory', 'directory')]:
    protected = (root / name).with_suffix('.stdout.log')
    protected.symlink_to(root / 'missing-target') if kind == 'symlink' else protected.mkdir()
    before = set(root.iterdir())
    result, prefix = run(name)
    require(result.returncode != 0 and set(root.iterdir()) == before, name)
for name, cwd, argv in [
    ('missing-executable', root, [str(root / 'does-not-exist')]),
    ('invalid-cwd', root / 'missing-directory', [sys.executable, '-c', 'print("unreachable")']),
    ('nonzero-child', root, [sys.executable, '-c', 'import sys; print("out"); print("err",file=sys.stderr); sys.exit(23)'])]:
    result, prefix = run(name, cwd, argv)
    receipt = json.loads(prefix.with_suffix('.command.json').read_text())
    require(result.returncode == receipt['exitCode'] != 0, name + ' exit propagation')
    for stream in ['stdout', 'stderr']:
        data = prefix.with_suffix('.' + stream + '.log').read_bytes()
        require(hashlib.sha256(data).hexdigest() == receipt[stream]['sha256'], name + ' digest')
        require(len(data) == receipt[stream]['sizeBytes'], name + ' size')
    started = datetime.datetime.fromisoformat(receipt['startedUtc'])
    finished = datetime.datetime.fromisoformat(receipt['finishedUtc'])
    require(finished >= started, name + ' timestamps')
    if name == 'nonzero-child':
        require(receipt['childExitCode'] == 23 and receipt['status'] == 'child-failed', name)
        require(prefix.with_suffix('.stdout.log').read_bytes() == b'out\n', name + ' stdout')
    else:
        require(receipt['status'] == 'launch-failed' and receipt['childExitCode'] is None, name)
        require(receipt['launchError']['type'] == 'FileNotFoundError', name + ' launch type')
print(json.dumps({'result': 'passed', 'controls': rows, 'scratch': str(root),
    'helperSha256': hashlib.sha256(helper.read_bytes()).hexdigest()}, indent=2))
