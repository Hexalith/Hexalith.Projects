"""Read-only audit of the current Story 6.4 verification receipt."""

import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET


EVIDENCE = Path(__file__).resolve().parent
ROOT = EVIDENCE.parents[4]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(name):
    return json.loads((EVIDENCE / name).read_text())


def check(condition, message):
    if not condition:
        raise RuntimeError(message)


def main():
    inventory = read('SHA256SUMS.json')
    for name, expected in inventory.items():
        path = EVIDENCE / name
        check(path.is_file(), 'Missing retained artifact: ' + name)
        check(path.stat().st_size == expected['bytes'] and digest(path) == expected['sha256'],
              'Retained artifact changed: ' + name)

    expected_suites = {
        'projects-server-full.xml': 1306,
        'projects-contracts-full.xml': 230,
        'projects-engine-confirmation.xml': 684,
        'conversations-server-full.xml': 1372,
    }
    cases = {}
    for name, expected_count in expected_suites.items():
        tree = ET.parse(EVIDENCE / name)
        assembly = tree.find('assembly')
        check(assembly is not None, 'Missing assembly: ' + name)
        counts = {key: int(assembly.get(key, '-1'))
                  for key in ['total', 'passed', 'failed', 'skipped', 'errors', 'not-run']}
        tests = tree.findall('.//test')
        check(counts['total'] == counts['passed'] == len(tests) == expected_count,
              'Unexpected executed case count: ' + name)
        check(all(counts[key] == 0 for key in ['failed', 'skipped', 'errors', 'not-run']),
              'Nonpassing full suite: ' + name)
        check(len({test.get('id') for test in tests}) == len(tests), 'Duplicate case IDs: ' + name)
        check(all(test.get('result') == 'Pass' for test in tests), 'Nonpassing case: ' + name)
        check(not tree.findall('.//output') and not tree.findall('.//system-out')
              and not tree.findall('.//system-err'), 'Unexpected captured runtime output: ' + name)
        cases[name] = {test.get('id'): test for test in tests}

    matrix = read('parent-audit/matrix-audit.json')
    expected_rows = {'Advisory', 'Protected', 'Order/archive', 'Bounded', 'Degraded', 'Revoked', 'Effects'}
    check(set(matrix['spec_rows']) == expected_rows and not matrix['missing_historical_references'],
          'Incomplete matrix audit')
    for row, references in matrix['spec_rows'].items():
        check(bool(references), 'Uncovered matrix row: ' + row)
        for reference in references:
            test = cases[reference['xml']].get(reference['id'])
            check(test is not None and test.get('name') == reference['name']
                  and test.get('result') == 'Pass', 'Unexecuted matrix reference: ' + row)

    server_names = {test.get('name') for test in cases['projects-server-full.xml'].values()}
    for required in [
        'Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests.'
        'GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures',
        'Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadHttpTests.'
        'ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart',
    ]:
        check(required in server_names, 'Missing actual gateway/persistence execution: ' + required)

    cleanup = read('cleanup.json')
    check({entry['name'] for entry in cleanup} == {'redis', 'daprd'}, 'Incomplete owned-process cleanup')
    check(all(entry['stopped'] and entry['exit_code'] == 0 for entry in cleanup), 'Owned process cleanup failed')

    for name, expected in read('source-hashes.json').items():
        path = ROOT / name
        check(path.is_file() and digest(path) == expected, 'Current source changed: ' + name)
    for name, expected in read('parent-audit/baseline.json')['files'].items():
        path = ROOT / name
        check(path.is_file() and digest(path) == expected, 'Original root bytes changed: ' + name)
    for relative, expected in read('git-before.json').items():
        repo = ROOT / relative
        head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repo, text=True).strip()
        index = hashlib.sha256(subprocess.check_output(['git', 'ls-files', '--stage'], cwd=repo)).hexdigest()
        check(head == expected['head'] and index == expected['index_sha256'],
              'Original Git identity changed: ' + relative)

    prefix = read('original-spec-prefix.json')
    spec = (ROOT / prefix['path']).read_bytes()
    check(hashlib.sha256(spec[:prefix['bytes']]).hexdigest() == prefix['sha256'],
          'Original specification prefix changed')
    start = spec.index(b'<frozen-after-approval')
    end = spec.index(b'</frozen-after-approval>') + len(b'</frozen-after-approval>')
    check(hashlib.sha256(spec[start:end]).hexdigest() == prefix['frozen_sha256'],
          'Frozen specification changed')
    check(b"status: 'in-progress'" in spec[:spec.index(b'---', 4)], 'Story prematurely completed')

    assets = read('parent-audit/consumer-project.assets.json')
    libraries = assets['libraries']
    for package in ['Hexalith.Conversations.Client/1.3.1', 'Hexalith.Conversations.Contracts/1.3.1']:
        check(libraries[package]['type'] == 'package', 'Incorrect consumer package selection: ' + package)
    check(not any(value['type'] == 'project' and not name.startswith('Hexalith.Projects')
                  for name, value in libraries.items()), 'Unexpected external source reference in consumer assets')
    package = read('parent-audit/public-package-prepare-final.stdout')
    check(package['status'] == 'failed' and {item['ruleId'] for item in package['diagnostics']}
          == {'HXW011', 'HXW009'}, 'Package qualification refusal was lost')

    print('PASS: 3,592 full-suite cases, seven matrix rows, current source/Git preservation, '
          'owned cleanup and package-consumer evidence; public qualification remains incomplete.')


if __name__ == '__main__':
    main()
