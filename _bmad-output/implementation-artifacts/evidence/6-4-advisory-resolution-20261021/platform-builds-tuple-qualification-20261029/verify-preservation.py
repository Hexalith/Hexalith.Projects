#!/usr/bin/env python3
"""Verify original worktree bytes and all original repository HEAD/index bindings."""
import hashlib
import json
from pathlib import Path
import subprocess

p = Path(__file__).resolve().parent
root = p.parents[4]
before = json.loads((p / 'preservation-before.json').read_bytes())
differences = []
for name, expected in before['files'].items():
    path = root / name
    observed = hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
    if observed != expected:
        differences.append({'file': name, 'expected': expected, 'observed': observed})
repositories = {}
for name, expected in before['repositories'].items():
    repo = root / name
    def git(*args):
        return subprocess.check_output(['git', '--no-optional-locks', *args], cwd=repo, text=True).strip()
    index = Path(git('rev-parse', '--git-path', 'index'))
    if not index.is_absolute():
        index = repo / index
    observed = {'head': git('rev-parse', 'HEAD'), 'branch': git('symbolic-ref', '-q', 'HEAD'),
                'index_sha256': hashlib.sha256(index.read_bytes()).hexdigest()}
    repositories[name] = observed
    for field in observed:
        if observed[field] != expected[field]:
            differences.append({'repository': name, 'field': field, 'expected': expected[field], 'observed': observed[field]})
result = {'original_file_count': len(before['files']), 'repository_count': len(repositories),
          'differences': differences, 'repositories': repositories,
          'result': 'preserved' if not differences else 'changed'}
(p / 'preservation-result.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({key: result[key] for key in ['original_file_count', 'repository_count', 'result', 'differences']}, indent=2))
raise SystemExit(bool(differences))
