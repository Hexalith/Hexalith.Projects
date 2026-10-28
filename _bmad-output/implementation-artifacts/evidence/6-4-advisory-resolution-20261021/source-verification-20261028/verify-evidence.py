"""Read-only retained evidence checks, optionally including both live workspaces."""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

if not __debug__:
    raise SystemExit('Refuse optimized Python: integrity checks require assertions')
sys.dont_write_bytecode = True
parser = argparse.ArgumentParser()
parser.add_argument('--live', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parent

def read(name):
    return json.loads((root/name).read_text())

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

checksums = read('SHA256SUMS.json')
for name, expected in checksums.items():
    assert sha(root/name) == expected, 'Evidence changed: '+name

commands = read('commands.json')
assert len({r['label'] for r in commands}) == len(commands)
records = {r['label']:r for r in commands}
for record in commands:
    assert (root/record['stdout']).exists() and (root/record['stderr']).exists()
for name, expected in {
    'original-source-refusal':2, 'accepted-tool-signature':0,
    'isolated-source-preparation':0, 'contracts-build':0, 'contracts-tests':0,
    'server-build':0, 'server-tests':0, 'isolated-package-refusal':2,
    'projects-contracts-actual-mapping':1, 'projects-server-actual-mapping':1,
    'original-preservation-audit':0, 'final-original-preservation-audit':0,
    'final-contracts-build':0, 'final-contracts-tests':0,
    'final-server-build':0, 'final-server-tests':0,
    'reviewed-verification-harness':0, 'reviewed-source-capture':0,
    'reviewed-original-preservation-audit':0,
}.items():
    assert records[name]['exit_code'] == expected, name

prepared = read('isolated-source-preparation.stdout')
assert prepared['status'] == 'prepared'
assert {d['ruleId'] for d in prepared['diagnostics']} == {'HXW008','HXW009','HXW011'}
package = read('isolated-package-refusal.stdout')
assert package['status'] == 'failed'
assert {d['ruleId'] for d in package['diagnostics']} == {'HXW008','HXW009','HXW011'}
assert 'HXW004' in {d['ruleId'] for d in read('original-source-refusal.stdout')['diagnostics']}

mapping = read('generated-source-mapping/mapping.json')
environment = prepared['buildPreparation']['environment']
assert mapping['Mode'] == 'source' and mapping['ActiveModule'] == 'Hexalith.Conversations'
assert len(mapping['Entries']) == 12
assert 'Hexalith.Projects' not in {e['Identity'] for e in mapping['Entries']}
assert mapping['ContentHash'] == prepared['buildPreparation']['mappingHash'] == environment['HEXALITH_SOURCE_MAPPING_HASH']
assert environment['Configuration'] == 'Debug'
assert len(environment) == 7
for name in ['contracts-build','server-build','final-contracts-build','final-server-build','projects-contracts-actual-mapping','projects-server-actual-mapping']:
    assert records[name]['environment'] == {'GIT_OPTIONAL_LOCKS':'0',**environment}, name
for name in ['contracts-tests','server-tests','final-contracts-tests','final-server-tests']:
    assert records[name]['environment'] == {'GIT_OPTIONAL_LOCKS':'0'}
for name in ['contracts-build','server-build','final-contracts-build','final-server-build']:
    output = (root/records[name]['stdout']).read_text()
    assert 'Build succeeded.' in output and '0 Warning(s)' in output and '0 Error(s)' in output
for name in ['projects-contracts-actual-mapping','projects-server-actual-mapping']:
    assert 'error HXW006: Unmapped ProjectReference' in (root/records[name]['stdout']).read_text()

for name, expected in read('generated-input-hashes.json').items():
    assert sha(root/'generated-source-mapping'/name) == expected
catalog = read('generated-source-mapping/catalog-versions.json')
assert catalog['CatalogCommit'] == '51a71cb1cfe860d5fc135eb3b8521fd80c413810'
blob = (root/'platform-owned-exact-catalog.stdout').read_bytes()
emitted = (root/'generated-source-mapping/pinned-tree/Props/Directory.Packages.props').read_bytes()
assert blob[:3] == b'\xef\xbb\xbf' and blob[3:] == emitted
assert hashlib.sha256(emitted).hexdigest() == catalog['CatalogFileHash']
assert sha(root/'capture-v2-platform-owned-exact-catalog.stdout') == hashlib.sha256(blob).hexdigest()

result = read('test-results.json')
for label, total in [('contracts',10),('server',243)]:
    xml = ET.parse(root/('final-'+label+'-tests.xml')).getroot()
    tests = xml.findall('.//test')
    assert len(tests) == total == result[label]['total']
    assert len({t.attrib['id'] for t in tests}) == total
    assert all(t.attrib['result'] == 'Pass' for t in tests)
    assert sorted({t.attrib['type'] for t in tests}) == result[label]['classes']
    assembly = xml.find('assembly')
    assert all(assembly.attrib[k] == '0' for k in ['failed','errors','skipped','not-run'])

assert read('preservation-before.json') == read('preservation-after.json')
closure = read('executable-closure.json')
assert closure['before'] == closure['after']
assert len(closure['after']) > 2
for label in ['final-contracts-build','final-server-build','final-contracts-tests','final-server-tests']:
    assert 'PATH' in records[label]['effective_build_environment']
assert (root/'final-dotnet-info.stdout').read_text().strip()
live = False
if args.live:
    from run import snapshot
    assert snapshot() == read('preservation-before.json'), 'Original checkout changed'
    selection = read('selected-inputs.json')
    workspace = Path(selection['workspace'])
    def git(repo, *arguments):
        return subprocess.check_output(['git',*arguments],cwd=repo,env={**__import__('os').environ,'GIT_OPTIONAL_LOCKS':'0'})
    assert git(workspace,'rev-parse','HEAD').decode().strip() == selection['head']
    assert git(workspace,'ls-files','--stage','references/') == (root/'isolated-index.stdout').read_bytes()
    # Every root source file and all child sources must remain clean; only the documented
    # real isolated gitlink selections are permitted in the root index.
    expected_paths = {i['path']:i['selected'] for i in selection['inputs']}
    for row in git(workspace,'status','--porcelain=v1','-z','--untracked-files=all').split(b'\0'):
        if row:
            assert row[:3] == b'M  ' and row[3:].decode() in expected_paths, 'Unexpected isolated root change: '+str(row)
    for name, expected in read('selected-source-hashes.json').items():
        assert sha(workspace/name) == expected, 'Selected source changed: '+name
    for entry in selection['inputs']:
        repo = workspace/entry['path']
        assert git(repo,'rev-parse','HEAD').decode().strip() == entry['selected']
        assert git(repo,'rev-parse','--show-superproject-working-tree').decode().strip() == str(workspace)
        assert not git(repo,'status','--porcelain','--untracked-files=all'), 'Selected child working tree/index changed: '+str(repo)
        assert not list((repo/'references').glob('*/.git'))
    actual_mapping = Path(prepared['buildPreparation']['workspace'])/'source-mapping'
    for name, expected in read('generated-input-hashes.json').items():
        assert sha(actual_mapping/name) == expected, 'Actual generated input changed: '+name
    for label in ['contracts','server']:
        assert sha(Path(result[label]['assemblies'][0]['name'])) == result[label]['binary_sha256']
    actual_closure = {str(p.relative_to(workspace)):sha(p)
        for name in ['Contracts','Server']
        for p in sorted((workspace/f'references/Hexalith.Conversations/tests/Hexalith.Conversations.{name}.Tests/bin/Debug/net10.0').rglob('*')) if p.is_file()}
    assert actual_closure == closure['after'], 'Executable output inventory/content changed'
    live = True
print(json.dumps(dict(status='passed',tests=253,evidence_files=len(checksums),live_workspaces_reverified=live,story_acceptance=False)))
