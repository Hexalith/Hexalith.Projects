import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = Path('/home/administrator/projects/hexalith/projects')
OWNER = ROOT / 'references/Hexalith.EventStore'
OUT = Path('/tmp/story65-memo-rw-owner-native-20261107')
PRIOR = Path('/tmp/story65-owner-tests/parsed-template-command-receipt.json')
MAPPING = Path('/tmp/story65-owner-repair-native-20261107/mapping.log')
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
now = lambda: datetime.datetime.now(datetime.timezone.utc).isoformat()

def save(name, value):
    p = OUT / name
    assert not p.exists(), str(p)
    p.write_text(json.dumps(value, indent=2) + '\n')

prior = json.loads(PRIOR.read_bytes())
assert sha(PRIOR) == '2a031e4d183152beb50b56428805fab3af7b08b787dbad3fc6240ca3dd6e1fb8'
assert sha(MAPPING) == 'ecd8d6b4819e5b1b9c9ca26a840925ed51aa2e77d19bd91c47580d7dc5a57977'
mapping = json.loads(MAPPING.read_bytes())['buildPreparation']['environment']
assert len(mapping) == 7
sources = dict(prior['sourceSha256'])
sources['tests/Hexalith.EventStore.Server.Tests/Routing/ImageDigestMemoConcurrencyTests.cs'] = sha(OWNER / 'tests/Hexalith.EventStore.Server.Tests/Routing/ImageDigestMemoConcurrencyTests.cs')
sources = {name: sha(OWNER / name) for name in sources}
save('source49-before.json', sources)
removed = ['CustomBeforeMicrosoftCommonTargets', *mapping]
environment = dict(os.environ)
for name in removed:
    environment.pop(name, None)
environment.update(mapping)
common = {'MSBUILDDISABLENODEREUSE': '1', 'DOTNET_CLI_USE_MSBUILD_SERVER': '0', 'HexalithBuildsRoot': str(ROOT / 'references/Hexalith.Builds')}
environment.update(common)
assert environment.get('DOTNET_PROCESSOR_COUNT') is None
build_environment = dict(environment, DOTNET_PROCESSOR_COUNT='2')
records = []

def run(name, argv, env, xml=None):
    log = OUT / (name + '.log')
    record = {'argv': argv, 'cwd': str(OWNER), 'sourceMappingEnvironment': mapping,
              'explicitEnvironment': {**common, **({'DOTNET_PROCESSOR_COUNT': '2'} if env is build_environment else {})},
              'runtimeProcessorOverride': env.get('DOTNET_PROCESSOR_COUNT'), 'removedEnvironmentBeforeApplyingMappings': removed,
              'mappingSource': str(MAPPING), 'mappingSha256': sha(MAPPING), 'startedAtUtc': now(), 'log': str(log)}
    print(json.dumps({'stage': name, 'state': 'starting', 'at': record['startedAtUtc']}), flush=True)
    with log.open('xb') as stream:
        process = subprocess.Popen(argv, cwd=OWNER, env=env, stdout=stream, stderr=subprocess.STDOUT, start_new_session=True)
        record['pid'] = process.pid
        record['processGroup'] = os.getpgid(process.pid)
        record['startTicks'] = Path(f'/proc/{process.pid}/stat').read_text().split(') ', 1)[1].split()[19]
        record['exitCode'] = process.wait()
    record['finishedAtUtc'] = now()
    record['logSha256'] = sha(log)
    try:
        os.killpg(record['processGroup'], 0)
        record['processGroupAbsent'] = False
    except ProcessLookupError:
        record['processGroupAbsent'] = True
    record['pidAbsent'] = not Path(f'/proc/{record["pid"]}').exists()
    if xml and Path(xml).is_file():
        tree = ET.parse(xml)
        record['xml'] = str(xml)
        record['xmlSha256'] = sha(xml)
        keys = ['total', 'passed', 'failed', 'errors', 'skipped', 'not-run']
        record['totals'] = {key: sum(int(row.get(key, '0')) for row in tree.findall('.//assembly')) for key in keys}
        record['testCases'] = [dict(row.attrib) for row in tree.findall('.//test')]
    records.append(record)
    save(name + '-command.json', record)
    print(json.dumps({'stage': name, 'state': 'finished', 'exitCode': record['exitCode'], 'totals': record.get('totals'),
                      'pidAbsent': record['pidAbsent'], 'processGroupAbsent': record['processGroupAbsent']}), flush=True)
    okay = record['exitCode'] == 0 and record['pidAbsent'] and record['processGroupAbsent']
    if xml:
        totals = record.get('totals', {})
        okay &= bool(totals) and totals.get('total') == totals.get('passed') and all(totals.get(k, -1) == 0 for k in ['failed', 'errors', 'skipped', 'not-run'])
    if not okay:
        save('incomplete-execution-receipt.json', {'records': records, 'stoppedOnFirstFailure': True, 'sourceSha256': sources})
        sys.exit(1)
    return record

run('01-build', prior['build']['argv'], build_environment)
test_dll = prior['nativeInvocations'][1]['argv'][1]
run('02-new-focused', ['dotnet', test_dll, '-class', 'Hexalith.EventStore.Server.Tests.Routing.ImageDigestMemoConcurrencyTests', '-xml', str(OUT / 'new-focused.xml')], environment, OUT / 'new-focused.xml')
regression = list(prior['nativeInvocations'][1]['argv'])
regression[regression.index('-xml') + 1] = str(OUT / 'regression412.xml')
run('03-regression412', regression, environment, OUT / 'regression412.xml')
current = {name: sha(OWNER / name) for name in sources}
assert current == sources
save('source49-after.json', current)
save('completed-execution-receipt.json', {'records': records, 'sourceSha256': sources, 'sourceCount': len(sources),
     'sourceBeforeAfterIdentical': True, 'allCommandsCompleted': True, 'priorReceiptSha256': sha(PRIOR)})
