#!/usr/bin/env python3
"""Read retained official observations and exact local Git objects; never adopt pins."""
import base64
import gzip
import hashlib
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import zipfile

p = Path(__file__).resolve().parent
root = p.parents[4]
builds = root / 'references/Hexalith.Builds'
platform = root / 'references/Hexalith.Platform'
expected = 'd1931b1e803144048d927fff16cb8119afde541e'
observations = []

def read(name):
    data = (p / name).read_bytes()
    return json.loads(gzip.decompress(data) if data[:2] == b'\x1f\x8b' else data)

def api(name):
    document = read(name)
    response = document.get('result', document.get('value', document))
    return json.loads(response['structuredContent']['content'])

def git(repo, *args):
    argv = ['git', '--no-optional-locks', *args]
    result = subprocess.run(argv, cwd=repo, capture_output=True, check=True)
    observations.append({'argv': argv, 'cwd': str(repo), 'exit_code': result.returncode,
                         'stdout': result.stdout.decode(), 'stderr': result.stderr.decode()})
    return result.stdout

release = next(x for x in api('github-builds-releases.json') if x['tag_name'] == 'v4.38.3')
assert not release['draft'] and not release['prerelease']
assert api('github-builds-4383-tag.json')['object']['sha'] == expected
assert git(builds, 'rev-parse', 'v4.38.3^{commit}').decode().strip() == expected
asset = next(x for x in release['assets'] if x['name'] == 'g4-tool-package-inventory.json')
inventory_bytes = (p / 'g4-tool-package-inventory.json').read_bytes()
assert asset['digest'] == 'sha256:' + hashlib.sha256(inventory_bytes).hexdigest()
assert asset['size'] == len(inventory_bytes)
inventory = read('g4-tool-package-inventory.json')
assert inventory['version'] == '4.38.3' and inventory['configuration'] == 'Release'
qualification = inventory['qualification']
assert qualification['sourceTree'] == {'clean': True, 'revision': expected}
assert qualification['releaseEligible'] and not qualification['ineligibilityReasons']
for field, mode in [('packageBuild', 'official'), ('sourceValidation', 'executed'), ('controls', 'executed')]:
    assert qualification[field]['result'] == 'passed' and qualification[field]['mode'] == mode
runs = api('github-builds-4383-runs.json')['workflow_runs']
for name, run_id in [('CI', 40669428503), ('Release', 40673566658)]:
    run = next(x for x in runs if x['id'] == run_id)
    assert run['name'] == name and run['head_sha'] == expected
    assert run['status'] == 'completed' and run['conclusion'] == 'success'
for name, run_id, required_names in [
    ('github-builds-4383-ci-jobs.json',40669428503, {'Python contract tests','CodeQL / analyze','Build and test','Dependency review'}),
    ('github-builds-4383-release-jobs.json',40673566658, {'Verify current main source','Release'})]:
    response = api(name)
    jobs = response['jobs']
    assert response['total_count'] == len(jobs) == len(required_names)
    assert {x['name'] for x in jobs} == required_names
    assert len({x['id'] for x in jobs}) == len(jobs)
    run = next(x for x in runs if x['id'] == run_id)
    assert all(x['run_id'] == run_id and x['head_sha'] == expected and x['run_attempt'] == run['run_attempt'] and x['status'] == 'completed' for x in jobs)
    assert jobs and all(x['conclusion'] in ['success', 'skipped'] for x in jobs)
    assert all(x['conclusion'] == 'success' for x in jobs if x['name'] != 'Dependency review')
feed = read('nuget-4383-catalog.json')
payload = (p / 'hexalith.builds.module.cli.4.38.3.nupkg').read_bytes()
assert feed['repository']['commit'] == expected and feed['version'] == '4.38.3'
assert feed['packageHashAlgorithm'] == 'SHA512'
assert base64.b64encode(hashlib.sha512(payload).digest()).decode() == feed['packageHash']
assert feed['packageSize'] == len(payload)
assert read('archive-signature.command.json')['exit_code'] == 0
with zipfile.ZipFile(p / 'hexalith.builds.module.cli.4.38.3.nupkg') as package:
    doc = ET.fromstring(package.read('Hexalith.Builds.Module.Cli.nuspec'))
    ns = {'n': doc.tag.split('}')[0].strip('{')}
    meta = doc.find('n:metadata', ns)
    assert meta.find('n:version', ns).text == '4.38.3'
    assert meta.find('n:repository', ns).attrib['commit'] == expected
    assert '.signature.p7s' in package.namelist()
    tooling_hash = hashlib.sha256(package.read('tools/net10.0/any/Hexalith.Builds.Tooling.dll')).hexdigest()
    extracted_hashes = {}
    for entry in package.infolist():
        if entry.is_dir():
            continue
        data = package.read(entry)
        extracted = p / 'extracted-tool' / entry.filename
        assert extracted.read_bytes() == data, entry.filename
        extracted_hashes[entry.filename] = hashlib.sha256(data).hexdigest()
    assert {str(x.relative_to(p/'extracted-tool')) for x in (p/'extracted-tool').rglob('*') if x.is_file()} == set(extracted_hashes)
(p / 'extracted-archive-file-hashes.json').write_text(json.dumps(extracted_hashes, indent=2)+'\n')
assert (p/'actual-4383-version.stdout').read_text().strip() == '4.38.3+' + expected
for name, binary in [('actual-4383-version','Hexalith.Builds.Module.Cli.dll'),('actual-4383-embedded-catalog','Hexalith.Builds.Tooling.dll')]:
    command = read(name+'.command.json')
    assert command['exit_code'] == 0 and str(p/'extracted-tool/tools/net10.0/any'/binary) in command['argv']
remote_source = read('github-4383-source-props.json')['result']['structuredContent']
source_blob = git(builds, 'rev-parse', expected + ':Props/Directory.Packages.props').decode().strip()
assert source_blob == remote_source['sha']
identities = {
    'platform_recorded': git(platform, 'rev-parse', 'HEAD:references/Hexalith.Builds').decode().strip(),
    'prepared_tool': git(builds, 'rev-parse', 'v4.38.2^{commit}').decode().strip(),
    'selected_published': expected,
    'actual_sibling': git(builds, 'rev-parse', 'HEAD').decode().strip(),
}
catalogs = {}
for label, revision in identities.items():
    data = git(builds, 'show', revision + ':Props/Directory.Packages.props')
    (p / (label + '.Directory.Packages.props.txt')).write_bytes(data)
    elements = [e for e in ET.fromstring(data).iter() if e.tag.startswith('Hexalith')]
    catalogs[label] = {'revision': revision, 'git_blob_sha256': hashlib.sha256(data).hexdigest(),
        'properties': {e.tag: e.text for e in elements if not e.get('Condition') or e.get('Condition') == "'$(" + e.tag + ")' == ''"},
        'property_declarations': [{'name':e.tag, 'condition':e.get('Condition'), 'value':e.text} for e in elements]}
assert catalogs['selected_published']['git_blob_sha256'] == catalogs['actual_sibling']['git_blob_sha256']
assert catalogs['selected_published']['properties']['HexalithTenantsVersion'] == '5.11.1'
assert (p/'exact-source/Props/Directory.Packages.props').read_bytes() == (p/'selected_published.Directory.Packages.props.txt').read_bytes()
for label, project in [('candidate-evaluated-catalog','__HXW_DefaultCatalog__'), ('candidate-conditional-folders','Hexalith.Folders.Aspire'), ('candidate-conditional-contracts','Hexalith.EventStore.Contracts')]:
    wrapper = p/'exact-source'/(project+'.proj')
    assert wrapper.read_text() == '<Project><Import Project="Props/Directory.Packages.props" /></Project>\n'
    command = read(label+'.command.json')
    assert command['exit_code'] == 0 and command['argv'][:3] == ['dotnet','msbuild',str(wrapper)]
    assert '-getItem:PackageVersion' in command['argv'] and '-p:Configuration=Debug' in command['argv']
    assert read(label+'.stdout')['Items']['PackageVersion']
evaluated = read('candidate-evaluated-catalog.stdout')
packages = {x['Identity']: x['Version'] for x in evaluated['Items']['PackageVersion']}
for name in ['Aspire', 'Client', 'Contracts', 'Server', 'Testing']:
    assert packages['Hexalith.Tenants.' + name] == '5.11.1'
assert packages['Hexalith.EventStore.Aspire'] == '3.128.4'
assert packages['Hexalith.Conversations.Contracts'] == '1.3.1'
assert packages['Hexalith.FrontComposer.Shell'] == '4.7.1'
embedded = read('actual-4383-embedded-catalog.stdout')
assert embedded['eventStoreVersion'] == packages['Hexalith.EventStore.Aspire']
assert embedded['frontComposerVersion'] == packages['Hexalith.FrontComposer.Shell']
assert embedded['aspireAppHostSdkVersion'] == evaluated['Properties']['HexalithAspireAppHostSdkVersion']
assert embedded['daprRuntimeVersion'] == evaluated['Properties']['HexalithDaprRuntimeVersion']
assert embedded['daprCliVersion'] == evaluated['Properties']['HexalithDaprCliVersion']
assert embedded['aspireHostingDaprVersion'] == packages['CommunityToolkit.Aspire.Hosting.Dapr']
assert embedded['daprSdkVersion'] == packages['Dapr.Client']
assert embedded['aspireHostingVersion'] == packages['Aspire.Hosting']
for field, property_name in [('redisImage','HexalithRedisImage'), ('redisImageTag','HexalithRedisImageTag'), ('redisImageDigest','HexalithRedisImageDigest')]:
    assert embedded[field] == catalogs['selected_published']['properties'][property_name]
assert 'tenant' not in str(embedded).lower()
assert api('github-platform-releases.json') == []
assert api('github-platform-current-runs.json')['total_count'] == 0
assert api('github-4382-tag.json')['object']['sha'] == identities['prepared_tool']
dependencies = read('dependency-publications.json')
expected_packages = {('Hexalith.Tenants.'+suffix,'5.11.1') for suffix in ['Aspire','Client','Contracts','Server','Testing']}
expected_packages |= {('Hexalith.EventStore.'+suffix,'3.128.4') for suffix in ['Aspire','ServiceDefaults']}
expected_packages |= {('Hexalith.Conversations.'+suffix,'1.3.1') for suffix in ['Contracts','Client']}
expected_packages |= {('Hexalith.FrontComposer.'+suffix,'4.7.1') for suffix in ['Contracts','Shell']}
assert len(dependencies) == len(expected_packages) == 11
assert {(x['id'],x['version']) for x in dependencies} == expected_packages
for item in dependencies:
    directory = p/'dependency-publications'/(item['id'].lower()+'.'+item['version'])
    registration = read(str(directory.relative_to(p)/'registration.raw'))
    catalog = read(str(directory.relative_to(p)/'catalog.raw'))
    assert catalog['id'] == item['id'] and catalog['version'] == item['version']
    assert registration['listed'] and registration['catalogEntry'] == item['observations'][1]['url']
    assert item['source'] == catalog['repository'] and item['dependencyGroups'] == catalog['dependencyGroups']
    assert registration['packageContent'] == 'https://api.nuget.org/v3-flatcontainer/'+item['id'].lower()+'/'+item['version']+'/'+item['id'].lower()+'.'+item['version']+'.nupkg'
    assert item['observations'][0]['url'] == 'https://api.nuget.org/v3/registration5-gz-semver2/'+item['id'].lower()+'/'+item['version']+'.json'
    for filename, observation in zip(['registration.raw','catalog.raw'], item['observations'], strict=True):
        data = (directory/filename).read_bytes()
        assert observation['status'] == 200 and observation['bytes'] == len(data) and observation['sha256'] == hashlib.sha256(data).hexdigest()
tenant_commit = api('github-tenants-5111-tag.json')['object']['sha']
assert tenant_commit == 'db83309146816602033320270f10b1cea28d3100'
assert all(x['source']['commit'] == tenant_commit for x in dependencies if x['id'].startswith('Hexalith.Tenants.'))
range_command = ['pwsh','-NoLogo','-NoProfile','-File',str(p/'check-dependency-ranges.ps1'),'-Receipt',str(p),'-CheckOnly']
assert read('tuple-identity.json')['evaluated_package_versions'] == packages
recheck = subprocess.run(range_command, cwd=root, capture_output=True, text=True, check=True)
observations.append({'argv':range_command,'cwd':str(root),'exit_code':recheck.returncode,'stdout':recheck.stdout,'stderr':recheck.stderr})
range_report = json.loads(recheck.stdout)
assert range_report == read('dependency-range-final.json')
assert range_report['totalConstraints'] == 196 and range_report['checkedConstraints'] == 195
assert range_report['failedConstraints'] == 0 and range_report['unresolvedConstraints'] == 1
assert len(range_report['results']) == 195 and all(x['passed'] for x in range_report['results'])
assert range_report['unresolved'][0]['dependency'] == 'Hexalith.FrontComposer.Contracts.UI'
tags = {}
for version, filename in [('1.0.0', 'github-platform-tag-100.json'), ('1.0.1', 'github-platform-tag-101.json')]:
    revision = api(filename)['object']['sha']
    assert git(platform, 'rev-parse', 'v' + version + '^{commit}').decode().strip() == revision
    pin = git(platform, 'rev-parse', revision + ':references/Hexalith.Builds').decode().strip()
    source = ET.fromstring(git(builds, 'show', pin + ':Props/Directory.Packages.props'))
    tags[version] = {'commit': revision, 'builds_pin': pin, 'properties': {
        e.tag:e.text for e in source.iter() if e.tag in ['HexalithConversationsVersion', 'HexalithTenantsVersion', 'HexalithEventStoreVersion']}}
report = {'decision': 'select 4.38.3 for an unapplied Platform correction; no accepted current Platform tuple',
    'version': '4.38.3', 'source': expected, 'source_blob': source_blob,
    'signed_archive': {'sha256': hashlib.sha256(payload).hexdigest(), 'sha512': feed['packageHash'], 'size': len(payload), 'nuget_verify_exit': 0},
    'inventory_sha256': hashlib.sha256(inventory_bytes).hexdigest(),
    'unsigned_inventory_package': next(x for x in inventory['packages'] if x['id'] == 'Hexalith.Builds.Module.Cli'),
    'tooling_assembly_sha256': tooling_hash, 'embedded_runtime_catalog': embedded,
    'source_catalogs': catalogs, 'evaluated_package_versions': packages, 'platform_tags': tags,
    'dependency_publications': [{'id':x['id'],'version':x['version'],'source_commit':x['source']['commit']} for x in dependencies],
    'current_platform': git(platform, 'rev-parse', 'HEAD').decode().strip(),
    'actual_sibling_delta': git(builds, 'diff', '--stat', expected, 'HEAD').decode(),
    'local_git_commands': observations,
    'limitations': ['Unsigned producer ZIP hash differs from repository-signed NuGet ZIP; no unsigned ZIP reconstruction claimed.',
        'Embedded runtime catalog has no Tenants field; evaluated exact source catalog proves its declared selection.',
        'Producer qualification and local source diagnostics do not establish Platform integration, runtime or consumer acceptance.',
        'Official release metadata is an observation, not an immutable release; revalidate identities before adoption.']}
(p / 'tuple-identity.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({'version': report['version'], 'source': expected, 'signed_sha256': report['signed_archive']['sha256'], 'inventory_sha256': report['inventory_sha256'], 'decision': report['decision']}, indent=2))
