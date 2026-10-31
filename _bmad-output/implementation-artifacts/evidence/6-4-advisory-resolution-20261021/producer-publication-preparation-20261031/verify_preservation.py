#!/usr/bin/env python3
"""Compare original workspace bytes and Git identities without refreshing indices."""
import argparse
import datetime
import gzip
import hashlib
import json
import os
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--output', default='preservation-final.json')
args = parser.parse_args()
evidence = Path(__file__).resolve().parent
root = evidence.parents[4]
baseline = json.loads(gzip.decompress((evidence / 'baseline.json.gz').read_bytes()))
allowed = {
    '.': {
        '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md',
        '_bmad-output/implementation-artifacts/sprint-status.yaml',
    }
}
env = dict(os.environ, GIT_OPTIONAL_LOCKS='0')

def git(repo, *argv):
    return subprocess.check_output(['git', *argv], cwd=repo, env=env).decode()

result = {'startedAt': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'repos': {}}
for key, original in baseline['repos'].items():
    repo = root / key
    index = Path(git(repo, 'rev-parse', '--git-path', 'index').strip())
    if not index.is_absolute():
        index = repo / index
    current = {
        'head': git(repo, 'rev-parse', 'HEAD').strip(),
        'branch': git(repo, 'branch', '--show-current').strip(),
        'refs': git(repo, 'show-ref'),
        'indexSha256': hashlib.sha256(index.read_bytes()).hexdigest(),
        'nestedModules': git(repo, 'submodule', 'status'),
    }
    changes = []
    for name, digest in original['files'].items():
        path = repo / name
        actual = hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
        if digest != actual:
            changes.append({'path': name, 'before': digest, 'after': actual,
                            'allowedTaskEdit': name in allowed.get(key, set())})
    result['repos'][key] = {
        'observedAt': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'current': current,
        'gitMatches': {k: current[k] == original[k] for k in current},
        'checkedFiles': len(original['files']),
        'changedOriginalFiles': changes,
    }
story = root / '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
result['originalStoryPrefixPreserved'] = story.read_bytes().startswith(
    (evidence / (story.name + '.before')).read_bytes())
result['allOriginalGitIdentitiesPreserved'] = all(
    all(r['gitMatches'].values()) for r in result['repos'].values())
result['allOtherOriginalFilesPreserved'] = all(
    all(c['allowedTaskEdit'] for c in r['changedOriginalFiles']) for r in result['repos'].values())
result['completedAt'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
(evidence / args.output).write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({k: v for k, v in result.items() if k != 'repos'}, indent=2))
for key, value in result['repos'].items():
    if not all(value['gitMatches'].values()) or value['changedOriginalFiles']:
        print(key, json.dumps(value))
raise SystemExit(0 if result['originalStoryPrefixPreserved'] and
                 result['allOriginalGitIdentitiesPreserved'] and
                 result['allOtherOriginalFilesPreserved'] else 1)
