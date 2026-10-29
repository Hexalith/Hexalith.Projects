"""Read-only verification of this bounded consumer-pin checkpoint."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

if not __debug__:
    raise RuntimeError('Optimized Python cannot verify evidence assertions')
p = Path(__file__).resolve().parent
root = p.parents[4]
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
read = lambda name: json.loads((p / name).read_text())
seal = p / 'SHA256SUMS.json'
if seal.exists():
    sealed = read('SHA256SUMS.json')
    actual = {str(f.relative_to(p)) for f in p.rglob('*') if f.is_file() and f != seal}
    assert actual == set(sealed)
    for name, entry in sealed.items():
        assert sha(p / name) == entry['sha256'] and (p / name).stat().st_size == entry['bytes'], name
source = read('source-audit.json')
for entry in source['source_files']:
    assert sha(root / entry['path']) == entry['sha256'], entry['path']
for repo, entry in source['repositories'].items():
    assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root / repo, text=True).strip() == entry['head'], repo
    index = subprocess.check_output(['git', 'ls-files', '--stage'], cwd=root / repo)
    assert hashlib.sha256(index).hexdigest() == entry['index_sha256'], repo
spec = source['original_spec']
text = (root / spec['path']).read_bytes()
assert hashlib.sha256(text[:spec['prefix_bytes']]).hexdigest() == spec['prefix_sha256']
frozen = re.search(rb'<frozen-after-approval.*?</frozen-after-approval>', text, re.S).group()
assert hashlib.sha256(frozen).hexdigest() == spec['frozen_sha256']
before = read('release-packages.before.json')
changes = 0
for package in before['packages']:
    deps = package['dependencies']
    if 'Hexalith.Conversations.Contracts' in deps:
        assert deps['Hexalith.Conversations.Contracts'] == '1.3.0'
        deps['Hexalith.Conversations.Contracts'] = '1.3.1'
        changes += 1
assert changes == 4 and before == json.loads((root / 'tools/release-packages.json').read_text())
assembly = ET.parse(p / 'owner-focused.xml').find('assembly')
assert assembly.get('passed') == '243'
assert all(assembly.get(k) == '0' for k in ['failed', 'errors', 'skipped', 'not-run'])
assert len({t.get('name') for t in assembly.findall('.//test')}) == 243
commands = read('commands.json')['commands']
expected = {'package-prepare': 2, 'projects-current-server-build': 1, 'projects-current-contracts-build': 1, 'mapped-current-projects-server-build': 1}
assert len(commands) == 14 and len({r['name'] for r in commands}) == 14
for entry in commands:
    assert entry['exit_code'] == expected.get(entry['name'], 0), entry['name']
    for ext in ['stdout', 'stderr']:
        assert (p / 'logs' / (entry['name'] + '.' + ext)).is_file()
assert {d['ruleId'] for d in json.loads((p / 'logs/package-prepare.stdout').read_text())['diagnostics']} == {'HXW011', 'HXW009'}
assert 'HXW006' in (p / 'logs/mapped-current-projects-server-build.stdout').read_text()
assert not (root / 'module/hexalith.module-manifest.v2.json').exists()
print(json.dumps({'checkpoint': 'pass', 'current_passing_tests': 299, 'source_hashes': len(source['source_files']), 'pin_changes': changes, 'story_complete': False, 'seal_checked': seal.exists()}))
