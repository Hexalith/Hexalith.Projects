#!/usr/bin/env python3
"""Exercise rejection paths using disposable copies of official verification inputs."""
import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import zipfile

BASE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('publication_verifier', BASE / 'verify-publication.py')
verifier = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verifier)
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
SOURCE = parser.parse_args().source
results = []


def rejected(name, operation, expected):
    try:
        operation()
    except (ValueError, KeyError) as exception:
        if expected not in str(exception):
            raise AssertionError(f'{name}: wrong failure {exception}') from exception
        results.append({'name': name, 'result': 'passed', 'rejection': str(exception)})
    else:
        raise AssertionError(name + ': invalid input accepted')


original = (BASE / 'downloads/hexalith.builds.evidence.cli.4.39.0.nupkg').read_bytes()
expected = json.loads((BASE / 'downloads/g4-tool-package-inventory.json').read_bytes())['packages'][0]['nupkg']
digest, size = verifier.unsigned_digest(original)
if digest != expected['sha256'].lower() or size != expected['sizeBytes']:
    raise AssertionError('Official valid signature envelope failed')
results.append({'name': 'valid official pre-signing envelope', 'result': 'passed'})
modified = bytearray(original)
modified[150] ^= 1
if verifier.unsigned_digest(modified)[0] == digest:
    raise AssertionError('Payload modification did not change inventory digest')
results.append({'name': 'compressed payload tamper changes digest', 'result': 'passed'})
rejected('trailing data', lambda: verifier.unsigned_digest(original + b'tamper'), 'trailing data')
with io.BytesIO() as buffer:
    with zipfile.ZipFile(buffer, 'w') as archive:
        archive.writestr('payload', b'x')
        archive.writestr('.signature.p7s', b'fixture-only', compress_type=zipfile.ZIP_DEFLATED)
    rejected('unsupported signature envelope', lambda: verifier.unsigned_digest(buffer.getvalue()), 'compression')


def run_tampered(name, mutation, expected_error):
    with tempfile.TemporaryDirectory(prefix='story64-publication-control-') as temporary:
        temporary = Path(temporary)
        for directory in ['observations', 'downloads']:
            target = temporary / directory
            target.mkdir()
            for original_path in (BASE / directory).iterdir():
                if original_path.is_file():
                    (target / original_path.name).symlink_to(original_path)

        def change(relative, operation):
            path = temporary / relative
            data = json.loads(path.read_text(encoding='utf-8-sig'))
            operation(data)
            path.unlink()
            path.write_text(json.dumps(data))

        def inventory_change(operation):
            change('downloads/g4-tool-package-inventory.json', operation)
            # Exercise the semantic guard even with a consistent asset checksum.
            digest = hashlib.sha256((temporary / 'downloads/g4-tool-package-inventory.json').read_bytes()).hexdigest()
            length = (temporary / 'downloads/g4-tool-package-inventory.json').stat().st_size
            def update_release(data):
                asset, = [a for a in data['data']['assets'] if a['name'] == 'g4-tool-package-inventory.json']
                asset['digest'] = 'sha256:' + digest
                asset['size'] = length
            change('observations/release.json', update_release)

        mutation(change, inventory_change)
        saved_base, saved_argv = verifier.BASE, sys.argv
        verifier.BASE = temporary
        sys.argv = ['verify-publication.py', '--source', SOURCE]
        try:
            rejected(name, verifier.main, expected_error)
        finally:
            verifier.BASE, sys.argv = saved_base, saved_argv


run_tampered('foreign tag source', lambda change, inv: change('observations/tag.json', lambda d: d['data']['object'].update(sha='0' * 40)), 'Tag binding')
run_tampered('foreign CI source', lambda change, inv: change('observations/ci-run.json', lambda d: d['data'].update(head_sha='0' * 40)), 'Push CI source')
run_tampered('skipped source validation', lambda change, inv: inv(lambda d: d['qualification']['sourceValidation'].update(mode='skipped')), 'sourceValidation did not pass')
run_tampered('ineligible inventory', lambda change, inv: inv(lambda d: d['qualification'].update(releaseEligible=False)), 'Inventory is ineligible')
run_tampered('foreign qualified source', lambda change, inv: inv(lambda d: d['qualification']['sourceTree'].update(revision='0' * 40)), 'Qualified source tree')
run_tampered('duplicate fixture path', lambda change, inv: inv(lambda d: d['qualification']['fixtures']['files'].append(d['qualification']['fixtures']['files'][0])), 'Duplicate fixture inventory path')
run_tampered('fixture byte tamper', lambda change, inv: inv(lambda d: d['qualification']['fixtures']['files'][0].update(sha256='0' * 64)), 'Fixture checkout bytes')
run_tampered('missing evidence coverage', lambda change, inv: inv(lambda d: d['qualificationEvidence'].pop()), 'Required evidence coverage')
print(json.dumps({'result': 'passed', 'controls': results, 'count': len(results), 'inputs': 'Official retained bytes; synthetic inputs exist only inside disposable TemporaryDirectory scopes'}, indent=2))
