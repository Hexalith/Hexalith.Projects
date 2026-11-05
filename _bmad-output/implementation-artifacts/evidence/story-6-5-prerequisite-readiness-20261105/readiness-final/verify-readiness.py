#!/usr/bin/env python3
"""Validate the final readiness packet and preservation boundaries without running feature work."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

output = Path(__file__).resolve().parent
root = output.parents[4]
packet = output.parent
spec = root / '_bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md'
allowed_refresh = {
    str(spec.relative_to(root)),
    '_bmad-output/implementation-artifacts/deferred-work.md',
    str((packet / 'README.md').relative_to(root)),
    str((packet / 'repair/README.md').relative_to(root)),
}
before = json.loads((output / 'preservation-before.json').read_text())
for row in before:
    path = root / row['path']
    if row['path'] not in allowed_refresh:
        assert hashlib.sha256(path.read_bytes()).hexdigest() == row['sha256'], row['path']
for old, copy in ((spec, 'spec-before-refresh.md'),
                  (packet / 'README.md', 'prerequisite-readme-before-refresh.md'),
                  (packet / 'repair/README.md', 'repair-readme-before-refresh.md')):
    retained = next(row for row in before if row['path'] == str(old.relative_to(root)))
    assert hashlib.sha256((output / copy).read_bytes()).hexdigest() == retained['sha256']

for document in (spec, output / 'README.md', output / 'contract-map.md',
                 packet / 'README.md', packet / 'repair/README.md'):
    for target in re.findall(r'\]\(([^)]+)\)', document.read_text()):
        target = target.split('#', 1)[0]
        if not target or '://' in target:
            continue
        assert (document.parent / target).exists(), (str(document), target)

assembly, = ET.parse(output / 'projects-auth-composition.xml').getroot().findall('assembly')
expected = {'total': '12', 'passed': '12', 'failed': '0', 'errors': '0', 'skipped': '0', 'not-run': '0'}
assert all(assembly.get(key) == value for key, value in expected.items())
tests = assembly.findall('.//test')
assert len(tests) == 12 and all(test.get('result') == 'Pass' for test in tests)
assert all('ProjectsUiAuthenticationCompositionTests.' in test.get('name', '') for test in tests)
commands = json.loads((output / 'commands.json').read_text())
assert all(row['exit_code'] == 0 for row in commands)
build = next(row for row in commands if row['name'] == 'projects-ui-tests-build')
build_text = (output / build['stdout']).read_text()
assert '0 Warning(s)' in build_text and '0 Error(s)' in build_text
assert '--configuration' in build['command'] and 'Debug' in build['command']
assert 'HEXALITH_SOURCE_MAPPING_HASH=18d3e8ef0de4e65bc49da4edec30d74464ede01f49c969156fe277ce0babd5aa' in build['command']

text = spec.read_text()
assert 'status: draft' in text and 'implementation_authorized: false' in text
assert 'readiness: ready-for-separate-implementation-request' in text
assert 'src/Hexalith.Projects.UI/Services/' not in text
assert 'one delivered coherent detail/context/setup result' not in text
assert '- [x]' not in text
ledger = (root / '_bmad-output/implementation-artifacts/deferred-work.md').read_text()
dw1 = ledger.split('### DW-1:', 1)[1].split('### DW-2:', 1)[0]
assert re.search(r'^status: done$', dw1, re.MULTILINE)
assert 'plus evidence/story-6-5-prerequisite-readiness-20261105/repair/owner-source.patch' in dw1
changed = subprocess.check_output(['git', 'diff', '--name-only'], cwd=root, text=True).splitlines()
allowed_tracked = allowed_refresh | {
    'references/Hexalith.FrontComposer',
    'tests/Hexalith.Projects.UI.Tests/Authentication/ProjectsUiAuthenticationCompositionTests.cs',
}
assert set(changed) <= allowed_tracked, changed
assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip() == 'b9a890e38044d23affe6b5d403d4b2a69b6edf34'
assert subprocess.check_output(['git', 'branch', '--show-current'], cwd=root, text=True).strip() == 'main'

result = dict(readiness='ready-for-separate-implementation-request',
              spec_status='draft; separate approval/implementation request required',
              story_implemented=False, sprint_status_unchanged=True, dw1='done for retained verified owner repair',
              consumer_build_exit_code=0, consumer_build_warnings=0, consumer_build_errors=0,
              native_tests={key: int(value) for key, value in expected.items()},
              preserved_original_fingerprints=len(before) - len(allowed_refresh),
              historical_readme_and_spec_snapshots_verified=True,
              source_manifest='source-inputs.json', native_xml='projects-auth-composition.xml', commands='commands.json')
(output / 'readiness-result.json').write_text(json.dumps(result, indent=2) + '\n')
print('PASS: native build and 12/12 focused authentication composition cases')
print('PASS: original repair receipts/patches/manifests, historical documentation snapshots and sprint bytes preserved')
print('PASS: current documentation links, DW-1 verified-repair scope and source-change boundaries')
print('PASS: readiness complete; feature implementation and dispatch remain outside this request')
