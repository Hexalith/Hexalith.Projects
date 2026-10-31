#!/usr/bin/env python3
"""Focused controls for the three new replay helpers only. No fixture Git writes."""
from pathlib import Path
import datetime
import hashlib
import json
import os
import shutil
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
REPO = Path('/tmp/story64-producer-replay-reviewed-2186b9a4')
ORIGINAL_MODULES = Path('/tmp/story64-builds-producer-2186b9a4/node_modules')
NODE = '/home/administrator/.nvm/versions/node/v24.21.0/bin/node'
MANIFEST = ROOT / 'implementation-evidence-manifest.json'
PIN = hashlib.sha256(MANIFEST.read_bytes()).hexdigest()
ROWS = json.loads(MANIFEST.read_bytes())['files']
INPUTS = HERE / 'inputs'
OUTPUTS = HERE / 'outputs'
INPUTS.mkdir(); OUTPUTS.mkdir()
results = []

def sha(data):
    return hashlib.sha256(data).hexdigest()


def preserve():
    failures = []
    for row in ROWS:
        data = (ROOT / row['file']).read_bytes()
        if sha(data) != row['sha256'] or len(data) != row['sizeBytes']:
            failures.append(row['file'])
    assert not failures, failures
    assert sha(MANIFEST.read_bytes()) == PIN


def replace(path, data):
    # Copy-on-write replacement ensures a negative control never edits a hardlinked original.
    temp = path.with_name(path.name + '.control-write')
    temp.write_bytes(data)
    temp.replace(path)


def update(path, change):
    value = json.loads(path.read_bytes()); change(value)
    replace(path, (json.dumps(value, indent=2) + '\n').encode())


def snapshot(name):
    target = INPUTS / name; target.mkdir()
    for row in ROWS:
        source = ROOT / row['file']; dest = target / row['file']
        dest.parent.mkdir(parents=True, exist_ok=True)
        try:
            os.link(source, dest)
        except OSError:
            shutil.copyfile(source, dest)
    for name in ['approval-package.json', 'source-inputs.json', 'implementation-evidence-manifest.json']:
        shutil.copyfile(ROOT / name, target / name)
    return target


def invoke(name, argv, expected, message=None):
    start = datetime.datetime.now(datetime.timezone.utc).isoformat()
    proc = subprocess.run(argv, cwd=REPO, capture_output=True, env={key: value for key, value in os.environ.items() if key not in ['NODE_OPTIONS', 'NODE_PATH']})
    (OUTPUTS / (name + '.stdout.log')).write_bytes(proc.stdout)
    (OUTPUTS / (name + '.stderr.log')).write_bytes(proc.stderr)
    record = {'name': name, 'argv': argv, 'cwd': str(REPO), 'startedUtc': start, 'finishedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'exitCode': proc.returncode, 'expectedExit': expected, 'expectedDiagnostic': message, 'stdoutSha256': sha(proc.stdout), 'stderrSha256': sha(proc.stderr)}
    results.append(record)
    assert (proc.returncode == 0) == (expected == 0), record
    if message:
        assert message in proc.stderr.decode(errors='replace'), record
    print(name, 'passed', flush=True)


def analyze(name, reference=None, expected=0, message=None, output=None):
    reference = reference or snapshot(name)
    output = output or OUTPUTS / (name + '-result')
    invoke(name, [NODE, str(ROOT / 'analyzer/analyze-release-reviewed.mjs'), str(REPO), str(reference), str(output), PIN], expected, message)
    return output


def check(name, reference=None, pin=PIN, expected=0, message=None, extra=None):
    reference = reference or snapshot(name)
    argv = [sys.executable, str(ROOT / 'qualification/verify-receipts-reviewed.py'), str(reference), '--manifest-sha256', pin]
    if extra:
        argv += extra
    invoke(name, argv, expected, message)


def reanchor(reference, relative):
    # Synthetic negative-control trust anchor: isolate a downstream relation rather than
    # failing only the preceding manifest-file check. Never written to real evidence.
    data = (reference / relative).read_bytes()
    def change(manifest):
        row = next(row for row in manifest['files'] if row['file'] == relative)
        row.update(sha256=sha(data), sizeBytes=len(data))
    update(reference / 'implementation-evidence-manifest.json', change)
    return sha((reference / 'implementation-evidence-manifest.json').read_bytes())


preserve()
assert subprocess.check_output(['git', '-C', str(REPO), 'rev-parse', 'HEAD'], text=True).strip() == '2186b9a4bc507a116a457c590c80945c2e232cbd'
assert not subprocess.check_output(['git', '-C', str(REPO), 'status', '--porcelain=v1'], text=True).strip()
shutil.copytree(ORIGINAL_MODULES, REPO / 'node_modules', symlinks=True)
positive = analyze('analysis-valid')
result_bytes = (positive / 'analysis-result-reviewed.json').read_bytes()
assert json.loads(result_bytes)['proposedVersion'] == '4.39.0'
analyze('analysis-refuses-existing-result', expected=1, message='Output directory already exists', output=positive)
assert (positive / 'analysis-result-reviewed.json').read_bytes() == result_bytes
for name, relative, message in [
    ('analysis-rejects-working-package', 'package.json', 'Working package.json mismatch'),
    ('analysis-rejects-working-lock', 'package-lock.json', 'Working package-lock.json mismatch'),
    ('analysis-rejects-module-hash', 'node_modules/semantic-release/lib/get-next-version.js', 'Installed module semantic-release/lib/get-next-version.js mismatch'),
]:
    path = REPO / relative; old = path.read_bytes()
    try:
        path.write_bytes(old + b'\n ')
        analyze(name, expected=1, message=message)
    finally:
        path.write_bytes(old)
path = REPO / 'node_modules/semantic-release/package.json'; old = path.read_bytes()
try:
    update(path, lambda value: value.update(version='99.0.0'))
    analyze('analysis-rejects-module-version', expected=1, message='Installed semantic-release version mismatch')
finally:
    path.write_bytes(old)
reference = snapshot('analysis-bad-approved-digest')
update(reference / 'approval-package.json', lambda value: value['versionProposal'].update(analysisSha256='0' * 64))
analyze('analysis-bad-approved-digest', reference, expected=1, message='Approval analysis digest mismatch')
reference = snapshot('analysis-bad-verified-inputs')
replace(reference / 'analyzer/verified-inputs.json', (reference / 'analyzer/verified-inputs.json').read_bytes() + b' ')
analyze('analysis-bad-verified-inputs', reference, expected=1, message='Retained analyzer/verified-inputs.json digest mismatch')
check('checker-valid')
reference = snapshot('checker-bad-approved-digest')
update(reference / 'approval-package.json', lambda value: value['versionProposal'].update(analysisSha256='0' * 64))
check('checker-bad-approved-digest', reference, expected=1, message='approval analysis digest mismatch')
check('checker-bad-manifest-digest', pin='0' * 64, expected=1, message='trusted implementation manifest digest mismatch')
reference = snapshot('checker-bad-manifest-file')
replace(reference / 'analyzer/analyze-release.mjs', b'tampered historical program')
check('checker-bad-manifest-file', reference, expected=1, message='implementation manifest file mismatch')
reference = snapshot('checker-bad-verified-inputs-binding')
update(reference / 'analyzer/analysis-result.json', lambda value: value.update(verifiedInputsSha256='0' * 64))
analysis_digest = sha((reference / 'analyzer/analysis-result.json').read_bytes())
update(reference / 'approval-package.json', lambda value: value['versionProposal'].update(analysisSha256=analysis_digest))
pin = reanchor(reference, 'analyzer/analysis-result.json')
check('checker-bad-verified-inputs-binding', reference, pin, expected=1, message='verified inputs digest mismatch')
reference = snapshot('checker-bad-stream-binding')
update(reference / 'analyzer/analysis.command.json', lambda value: value['stdout'].update(sha256='0' * 64))
pin = reanchor(reference, 'analyzer/analysis.command.json')
check('checker-bad-stream-binding', reference, pin, expected=1, message='command stream mismatch')
reference = snapshot('checker-bad-security-claim')
update(reference / 'approval-package.json', lambda value: value['securityObservation']['vulnerableDependencyEntries'].update(total=0))
check('checker-bad-security-claim', reference, expected=1, message='security finding counts mismatch')
existing = OUTPUTS / 'checker-existing-result.json'; existing.write_text('preserve existing result\n')
check('checker-refuses-existing-result', expected=1, message='result already exists', extra=['--output', str(existing)])
assert existing.read_text() == 'preserve existing result\n'
for name, exits, expected in [('orchestrator-all-pass', [0, 0], 0), ('orchestrator-earlier-failure', [7, 0], 1), ('orchestrator-late-failure', [0, 9], 1)]:
    plan = OUTPUTS / (name + '.plan.json')
    plan.write_text(json.dumps([{'name': f'double-{index}', 'argv': [sys.executable, '-c', f'import sys; sys.exit({code})'], 'cwd': str(HERE)} for index, code in enumerate(exits)]))
    result = OUTPUTS / (name + '.result.json')
    invoke(name, [sys.executable, str(ROOT / 'qualification/run-controls-reviewed.py'), str(plan), str(result)], expected)
    assert json.loads(result.read_bytes())['failed'] is (expected != 0)
name = 'orchestrator-start-failure'; plan = OUTPUTS / (name + '.plan.json')
plan.write_text(json.dumps([{'name': 'missing-process-double', 'argv': ['/absent/story64-process-double'], 'cwd': str(HERE)}]))
invoke(name, [sys.executable, str(ROOT / 'qualification/run-controls-reviewed.py'), str(plan), str(OUTPUTS / (name + '.result.json'))], 1)
preserve()
assert not subprocess.check_output(['git', '-C', str(REPO), 'status', '--porcelain=v1'], text=True).strip()
(HERE / 'focused-controls.json').write_text(json.dumps({'testedHelpers': ['analyzer/analyze-release-reviewed.mjs', 'qualification/run-controls-reviewed.py', 'qualification/verify-receipts-reviewed.py'], 'tests': results, 'passed': len(results), 'original116FilesPreserved': True, 'originalImplementationManifestSha256': PIN, 'cleanSelectedSource': str(REPO), 'selectedRevision': '2186b9a4bc507a116a457c590c80945c2e232cbd', 'fixtureCommitsTagsOrInitialization': False, 'fullQualifierExecuted': False, 'note': 'Only the explicit fresh detached checkout was created. Orchestrator controls use process doubles; real historical control list was not rerun. Synthetic tamper inputs are isolated copies and are never acceptance evidence.'}, indent=2) + '\n')
print('Focused controls passed:', len(results))
