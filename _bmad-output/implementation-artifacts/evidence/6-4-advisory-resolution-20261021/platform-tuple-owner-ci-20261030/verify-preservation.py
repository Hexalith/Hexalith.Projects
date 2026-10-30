#!/usr/bin/env python3
"""Verify original source/evidence, repository HEADs and indexes without edits."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path('/home/administrator/projects/hexalith/projects')
OUT = Path(__file__).resolve().parent
before = json.loads((OUT / 'before.json').read_text())
differences = []
checked = 0
for relative, expected in before['files'].items():
    path = ROOT / relative
    actual = hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
    checked += 1
    if actual != expected:
        differences.append({'path': relative, 'expected': expected, 'actual': actual})
repositories = {}
for relative, state in before['repositories'].items():
    repo = ROOT / relative
    run = lambda *args: subprocess.check_output(['git', *args], cwd=repo, text=True).strip()
    head, index = run('rev-parse', 'HEAD'), run('write-tree')
    if head != state['head'] or index != state['index']:
        differences.append({'repository': relative, 'head': head, 'index': index})
    for name, expected in state['files'].items():
        path = repo / name
        actual = hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
        checked += 1
        if actual != expected:
            differences.append({'repository': relative, 'path': name, 'expected': expected, 'actual': actual})
    repositories[relative] = {'head': head, 'index': index, 'originalFilesChecked': len(state['files'])}
owner = ROOT / 'references/Hexalith.Platform'
initialized = [str(p.relative_to(owner)) for p in (owner / 'references').glob('*/.git')]
story = (ROOT / '_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md').read_text()
spec = (ROOT / '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md').read_text()
sprint = (ROOT / '_bmad-output/implementation-artifacts/sprint-status.yaml').read_text()
story_in_progress = ('status: in-progress' in story and "status: 'in-progress'" in spec
                     and '6-4-resolve-projects-with-transient-current-explanations: in-progress' in sprint)
result = {'originalFileChecks': checked, 'historicalReceiptFiles': len(before['files']),
          'repositories': repositories, 'differences': differences,
          'umbrellaOwnerInitializedChildren': initialized, 'storyInProgress': story_in_progress}
(OUT / 'preservation-result.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({'originalFileChecks': checked, 'differences': len(differences),
                  'initializedChildren': initialized, 'storyInProgress': story_in_progress}, indent=2))
assert not differences and not initialized and story_in_progress
