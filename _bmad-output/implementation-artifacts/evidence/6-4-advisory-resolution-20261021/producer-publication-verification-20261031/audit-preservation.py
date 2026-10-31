#!/usr/bin/env python3
"""Verify original repository authority and bytes after this evidence-only task."""
import argparse
import datetime
import gzip
import hashlib
import json
import os
from pathlib import Path
import subprocess

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[4]
CANONICAL = '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
SPRINT = '_bmad-output/implementation-artifacts/sprint-status.yaml'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise SystemExit('Refusing to overwrite preservation report')
    before = json.loads(gzip.decompress((BASE / 'preservation-before.json.gz').read_bytes()))
    problems = []
    repos = []
    env = {**os.environ, 'GIT_OPTIONAL_LOCKS': '0'}
    for repository in before['repositories']:
        path = repository['path']
        def git(*argv):
            return subprocess.check_output(['git', '-C', path, *argv], env=env).decode().strip()
        index = Path(git('rev-parse', '--path-format=absolute', '--git-path', 'index'))
        state = {'path': path, 'head': git('rev-parse', 'HEAD'), 'refs': git('show-ref'), 'indexSha256': hashlib.sha256(index.read_bytes()).hexdigest(), 'configSha256': hashlib.sha256(git('config', '--local', '--list').encode()).hexdigest()}
        for key in ['head', 'refs', 'indexSha256', 'configSha256']:
            if state[key] != repository[key]:
                problems.append(path + ': changed ' + key)
        repos.append(state)
    for relative, expected in before['files'].items():
        if relative in [CANONICAL, SPRINT]:
            continue
        path = ROOT / relative
        if not path.is_file() or path.stat().st_size != expected['size']:
            problems.append(relative + ': missing or size changed')
        elif hashlib.file_digest(path.open('rb'), 'sha256').hexdigest() != expected['sha256']:
            problems.append(relative + ': bytes changed')
    canonical_before = gzip.decompress((BASE / 'canonical-prefix.md.gz').read_bytes())
    canonical_after = (ROOT / CANONICAL).read_bytes()
    if not canonical_after.startswith(canonical_before):
        problems.append('Canonical original prefix was changed')
    if b"status: 'in-progress'" not in canonical_after.split(b'---', 2)[1]:
        problems.append('Canonical story is no longer in-progress')
    old_sprint = gzip.decompress((BASE / 'sprint-status-before.yaml.gz').read_bytes())
    new_sprint = (ROOT / SPRINT).read_bytes()
    old_lines, new_lines = old_sprint.splitlines(), new_sprint.splitlines()
    if len(old_lines) != len(new_lines):
        problems.append('Sprint line count changed')
    else:
        for old, new in zip(old_lines, new_lines):
            if old != new and (not old.lstrip().startswith(b'6-4-resolve-projects-with-transient-current-explanations: in-progress #') or old.split(b'#', 1)[0] != new.split(b'#', 1)[0]):
                problems.append('Sprint change exceeds the Story 6.4 comment')
    report = {'schema': 'story64.producer-verification-preservation.v1', 'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'result': 'passed' if not problems else 'failed', 'originalRepositories': len(repos), 'originalFiles': len(before['files']), 'originalNonTaskBytesUnchanged': not problems, 'canonicalOriginalPrefixPreserved': canonical_after.startswith(canonical_before), 'canonicalStoryStatus': 'in-progress', 'allowedOriginalEdits': [CANONICAL + ' append only', SPRINT + ' Story 6.4 comment only'], 'repositories': repos, 'problems': problems}
    with args.output.open('x') as output:
        output.write(json.dumps(report, indent=2) + '\n')
    print(json.dumps({key: value for key, value in report.items() if key != 'repositories'}))
    raise SystemExit(0 if not problems else 1)


if __name__ == '__main__':
    main()
