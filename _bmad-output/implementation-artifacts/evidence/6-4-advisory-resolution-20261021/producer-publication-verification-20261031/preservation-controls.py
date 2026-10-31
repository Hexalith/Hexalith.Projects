#!/usr/bin/env python3
"""Exercise preservation gaps only in command-owned disposable Git repositories."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

sys.dont_write_bytecode = True
BASE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('preservation', BASE / 'audit-preservation-reviewed.py')
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
source = Path(sys.argv[1])
env = {**os.environ, 'GIT_OPTIONAL_LOCKS': '0'}
results = []

with tempfile.TemporaryDirectory(prefix='story64-preservation-control-') as temporary:
    repo = Path(temporary) / 'repo'
    subprocess.run(['git', 'clone', '--shared', '--no-checkout', '--no-hardlinks', str(source), str(repo)], env=env, check=True, capture_output=True)
    def git(*args):
        return subprocess.check_output(['git', '-C', str(repo), *args], env=env).decode().strip()
    selected = '2186b9a4bc507a116a457c590c80945c2e232cbd'
    git('checkout', '--detach', selected)
    git('branch', 'same-source', selected)
    def old_state():
        index = Path(git('rev-parse', '--path-format=absolute', '--git-path', 'index'))
        return git('rev-parse', 'HEAD'), git('show-ref'), hashlib.sha256(index.read_bytes()).hexdigest()
    before = old_state()
    symbolic_before = audit.symbolic_state(repo)
    # Only HEAD's attachment changes; commit, named refs and index remain equal.
    git('symbolic-ref', 'HEAD', 'refs/heads/same-source')
    after = old_state()
    symbolic_after = audit.symbolic_state(repo)
    if before != after or symbolic_before == symbolic_after:
        raise SystemExit('Symbolic HEAD control did not isolate the old audit gap')
    results.append({'name': 'same-commit attachment change detected', 'result': 'passed', 'oldComparedStateEqual': True, 'symbolicBefore': symbolic_before, 'symbolicAfter': symbolic_after})

    original = {'src/old.cs'}
    unexpected = original | {'src/unexpected.cs'}
    if audit.unauthorized_new_paths(original, unexpected) != ['src/unexpected.cs']:
        raise SystemExit('New source file escaped preservation check')
    results.append({'name': 'new unrelated source path detected', 'result': 'passed'})
    packet = audit.BASE.relative_to(audit.ROOT).as_posix()
    if audit.unauthorized_new_paths(original, original | {packet + '/new-receipt.json', audit.SCOPED_SPEC}):
        raise SystemExit('Authorized evidence/spec path refused')
    results.append({'name': 'only exact evidence and scoped spec additions allowed', 'result': 'passed'})
    if audit.status_entries('?? eng/unexpected.json', 'references/Hexalith.Platform') == audit.status_entries(''):
        raise SystemExit('New submodule status escaped preservation check')
    results.append({'name': 'unrelated submodule status change detected', 'result': 'passed'})
    if audit.status_entries('?? ' + packet + '/new-receipt.json'):
        raise SystemExit('Authorized evidence status refused')
    results.append({'name': 'authorized evidence status allowed', 'result': 'passed'})
print(json.dumps({'result': 'passed', 'count': len(results), 'controls': results, 'realRepositoriesMutated': False}, indent=2))
