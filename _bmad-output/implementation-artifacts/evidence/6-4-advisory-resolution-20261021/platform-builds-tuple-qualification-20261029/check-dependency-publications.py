#!/usr/bin/env python3
"""Observe official NuGet dependency metadata without restoring or changing pins."""
import datetime
import gzip
import hashlib
import json
from pathlib import Path
import urllib.request
from concurrent.futures import ThreadPoolExecutor

p = Path(__file__).resolve().parent
packages = [('Hexalith.Tenants.' + suffix, '5.11.1') for suffix in ['Aspire', 'Client', 'Contracts', 'Server', 'Testing']]
packages += [('Hexalith.EventStore.' + suffix, '3.128.4') for suffix in ['Aspire', 'ServiceDefaults']]
packages += [('Hexalith.Conversations.' + suffix, '1.3.1') for suffix in ['Contracts', 'Client']]
packages += [('Hexalith.FrontComposer.' + suffix, '4.7.1') for suffix in ['Contracts', 'Shell']]

def fetch(url, path):
    assert not path.exists()
    request = urllib.request.Request(url, headers={'User-Agent': 'Hexalith-story64-read-only-qualification', 'Cache-Control': 'no-cache'})
    with urllib.request.urlopen(request, timeout=60) as response:
        data = response.read()
        path.write_bytes(data)
        observation = {'url': url, 'status': response.status, 'observed_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                       'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)}
    return json.loads(gzip.decompress(data) if data[:2] == b'\x1f\x8b' else data), observation

def check(item):
    name, version = item
    directory = p / 'dependency-publications' / (name.lower() + '.' + version)
    directory.mkdir(parents=True)
    registration, first = fetch('https://api.nuget.org/v3/registration5-gz-semver2/' + name.lower() + '/' + version + '.json', directory / 'registration.raw')
    catalog, second = fetch(registration['catalogEntry'], directory / 'catalog.raw')
    assert registration['listed'] and catalog['id'].lower() == name.lower() and catalog['version'] == version
    result = {'id': name, 'version': version, 'source': catalog.get('repository'), 'published': catalog['published'],
              'dependencyGroups': catalog.get('dependencyGroups', []), 'observations': [first, second]}
    (directory / 'metadata.json').write_text(json.dumps(result, indent=2) + '\n')
    return result

with ThreadPoolExecutor(max_workers=6) as executor:
    result = list(executor.map(check, packages))
(p / 'dependency-publications.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps([{'id': x['id'], 'version': x['version'], 'source': x['source']} for x in result], indent=2))
