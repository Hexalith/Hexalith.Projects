#!/usr/bin/env python3
"""Verify retained official publication bytes without publishing or changing Git state."""
import argparse
import datetime
import gzip
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

BASE = Path(__file__).resolve().parent
SOURCE = '2186b9a4bc507a116a457c590c80945c2e232cbd'
VERSION = '4.39.0'
TREE = '18d735e56eeb17332e2d2b57a6b116097f42fdec'
REPOSITORY = 'https://github.com/Hexalith/Hexalith.Builds'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    data = (BASE / path).read_bytes()
    if data.startswith(b'\x1f\x8b'):
        data = gzip.decompress(data)
    return json.loads(data.decode('utf-8-sig'))


def unsigned_digest(data):
    """Hash the exact pre-signing ZIP envelope in memory; emit no unsigned archive.

    This deliberately supports only these observed non-ZIP64 packages: one final
    stored signature local record, one final signature central record, no comment,
    no multi-disk archive. Unexpected layouts fail closed. Existing compressed
    payload records and central records are preserved byte for byte, never packed.
    """
    import io
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        infos = archive.infolist()
        require(len({i.filename for i in infos}) == len(infos), 'Duplicate ZIP entries')
        signature = archive.getinfo('.signature.p7s')
        require(infos[-1] == signature, 'Signature is not the last entry')
        require(signature.compress_type == zipfile.ZIP_STORED, 'Unexpected signature compression')
        offsets = [i.header_offset for i in infos]
        require(max(offsets) == signature.header_offset, 'Signature local record is not final')
        position = archive.start_dir
        records = []
        while data[position:position + 4] == b'PK\x01\x02':
            fields = struct.unpack_from('<4s6H3I5H2I', data, position)
            length = 46 + fields[10] + fields[11] + fields[12]
            name = data[position + 46:position + 46 + fields[10]].decode('utf-8')
            require(fields[13] == 0, 'Multi-disk entry')
            require(fields[8] != 0xffffffff and fields[9] != 0xffffffff and fields[16] != 0xffffffff, 'ZIP64 entry')
            records.append((name, data[position:position + length]))
            position += length
        require(records[-1][0] == '.signature.p7s', 'Signature central record is not final')
        require(len(records) == len(infos), 'Central directory count mismatch')
        end = list(struct.unpack_from('<4s4H2IH', data, position))
        require(end[0] == b'PK\x05\x06' and end[1:3] == [0, 0], 'Unexpected end record')
        require(end[3] == end[4] == len(infos), 'End record entry count mismatch')
        require(end[5] == position - archive.start_dir and end[6] == archive.start_dir, 'Central directory binding mismatch')
        require(end[7] == 0 and position + 22 == len(data), 'ZIP comment or trailing data')
        central = b''.join(record for name, record in records if name != '.signature.p7s')
        end[3] -= 1
        end[4] -= 1
        end[5] = len(central)
        end[6] = signature.header_offset
        digest = hashlib.sha256()
        digest.update(data[:signature.header_offset])
        digest.update(central)
        digest.update(struct.pack('<4s4H2IH', *end))
        return digest.hexdigest(), signature.header_offset + len(central) + 22


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, required=True, help='Repository containing the selected Git object')
    parser.add_argument('--output', type=Path, help='New report path; existing outputs are refused')
    args = parser.parse_args()
    require(not args.output or not args.output.exists(), 'Refusing to overwrite report')
    env = {**os.environ, 'GIT_OPTIONAL_LOCKS': '0'}

    def git(*argv):
        return subprocess.check_output(['git', '--no-replace-objects', '-C', str(args.source), *argv], env=env)

    require(git('rev-parse', SOURCE + '^{commit}').decode().strip() == SOURCE, 'Source object mismatch')
    require(git('rev-parse', SOURCE + '^{tree}').decode().strip() == TREE, 'Source tree mismatch')
    run = load('observations/release-run.json')['data']
    require(run['id'] == 41172847732 and run['head_sha'] == SOURCE and run['run_attempt'] == 1, 'Release run binding mismatch')
    require(run['status'] == 'completed' and run['conclusion'] == 'success', 'Release did not succeed')
    require(run['path'] == '.github/workflows/build-release.yml', 'Release workflow mismatch')
    official_source = load('observations/selected-commit.json')['data']
    require(official_source['sha'] == SOURCE and official_source['commit']['tree']['sha'] == TREE, 'Official source/tree mismatch')
    ci = load('observations/ci-run.json')['data']
    require(ci['id'] == 41171553609 and ci['head_sha'] == SOURCE and ci['event'] == 'push' and ci['path'] == '.github/workflows/ci.yml', 'Push CI source/workflow mismatch')
    require(ci['status'] == 'completed' and ci['conclusion'] == 'success', 'Exact-source push CI failed')
    jobs = load('observations/release-jobs.json')['data']['jobs']
    require(len(jobs) == 2 and all(j['head_sha'] == SOURCE and j['conclusion'] == 'success' for j in jobs), 'Release jobs/source mismatch')
    require(load('observations/tag.json')['data']['object'] == {'sha': SOURCE, 'type': 'commit', 'url': 'https://api.github.com/repos/Hexalith/Hexalith.Builds/git/commits/' + SOURCE}, 'Tag binding mismatch')
    release = load('observations/release.json')['data']
    require(release['tag_name'] == 'v' + VERSION and not release['draft'] and not release['prerelease'], 'Release identity mismatch')
    asset, = [a for a in release['assets'] if a['name'] == 'g4-tool-package-inventory.json']
    inventory_bytes = (BASE / 'downloads/g4-tool-package-inventory.json').read_bytes()
    require(asset['digest'] == 'sha256:' + sha(inventory_bytes) and asset['size'] == len(inventory_bytes), 'Official inventory asset digest mismatch')
    inventory = json.loads(inventory_bytes)
    require(inventory['schema'] == 'hexalith.g4-tool-package-inventory.v1' and inventory['version'] == VERSION and inventory['configuration'] == 'Release', 'Inventory schema/version mismatch')
    qualification = inventory['qualification']
    for key, mode in [('packageBuild', 'official'), ('sourceValidation', 'executed'), ('controls', 'executed')]:
        require(qualification[key] == {'mode': mode, 'result': 'passed'}, key + ' did not pass')
    require(qualification['sourceTree'] == {'clean': True, 'revision': SOURCE}, 'Qualified source tree mismatch')
    require(qualification['releaseEligible'] is True and qualification['ineligibilityReasons'] == [], 'Inventory is ineligible')
    fixture = qualification['fixtures']
    require(fixture['mode'] == 'repository-tracked' and fixture['root'] == 'test/fixtures', 'External fixture provenance')
    tracked = set(git('ls-tree', '-r', '--name-only', SOURCE, '--', 'test/fixtures').decode().splitlines())
    # Reproduce the selected qualifier's Get-ChildItem -File -Recurse default:
    # hidden files/directories are omitted. Record that coverage limitation;
    # do not silently promote its manifest to an all-tracked-files proof.
    enumerated = {p for p in tracked if not any(part.startswith('.') for part in Path(p).parts)}
    listed = {'test/fixtures/' + f['file'] for f in fixture['files']}
    require(len(listed) == len(fixture['files']), 'Duplicate fixture inventory path')
    require(enumerated == listed, 'Existing qualifier fixture enumeration mismatch')
    hidden_omissions = sorted(tracked - listed)
    git('diff', '--quiet', SOURCE, '--', '.gitattributes', '**/.gitattributes')
    material = []
    checkout_conversions = []
    for row in fixture['files']:
        content = git('show', SOURCE + ':test/fixtures/' + row['file'])
        if sha(content) != row['sha256'] or len(content) != row['sizeBytes']:
            filtered = git('cat-file', '--filters', SOURCE + ':test/fixtures/' + row['file'])
            require(sha(filtered) == row['sha256'] and len(filtered) == row['sizeBytes'], 'Fixture checkout bytes mismatch: ' + row['file'])
            checkout_conversions.append({'file': row['file'], 'gitBytesSha256': sha(content), 'checkoutSha256': sha(filtered)})
        material.append(f"{row['file']}|{row['sha256']}|{row['sizeBytes']}")
    require(sha('\n'.join(material).encode()) == fixture['sha256'], 'Fixture manifest digest mismatch')
    required = {'qualification-evidence/' + n for n in ['source-release-passed.json', 'packaged-down-output.json', 'packaged-down-evidence.json', 'packaged-test-output.json', 'packaged-test-evidence.json', 'packaged-unavailable-output.json', 'packaged-unavailable-evidence.json', 'packaged-readiness-output.json', 'packaged-seed-subscriptions.json', 'qualification.log']}
    for row in fixture['files']:
        name = row['file']
        if name.startswith('module/negative/') and name.endswith('.json') and not name.endswith('.expected.json'):
            required.add('qualification-evidence/module-negative-' + Path(name).stem + '-output.json')
        elif name.startswith('evidence/negative/') and name.endswith(('.yaml', '.yml')):
            required.add('qualification-evidence/evidence-negative-' + Path(name).stem + '-output.json')
    evidence_names = [row['file'] for row in inventory['qualificationEvidence']]
    require(len(evidence_names) == len(set(evidence_names)) and required <= set(evidence_names), 'Required evidence coverage mismatch')
    for row in inventory['qualificationEvidence']:
        require(Path(row['file']).parent.as_posix() == 'qualification-evidence' and re.fullmatch(r'[0-9a-fA-F]{64}', row['sha256']) and row['sizeBytes'] > 0, 'Malformed evidence identity')
    source_catalog = git('show', SOURCE + ':Props/Directory.Packages.props')
    require(source_catalog == (BASE / 'downloads/Directory.Packages.props').read_bytes(), 'Official catalog/source byte mismatch')
    require(git('rev-parse', SOURCE + ':Props/Directory.Packages.props').decode().strip() == '37148298dfd3a4ae35d89dd5f4677e06e89149ad', 'Catalog blob mismatch')
    evaluated = (BASE / 'downloads/evaluated-catalog.json').read_bytes()
    require(load('observations/catalog-evaluation.command.json')['exitCode'] == 0, 'Catalog evaluation failed')
    ids = {'Hexalith.Builds.Module.Cli', 'Hexalith.Builds.Evidence.Cli'}
    require({r['id'] for r in inventory['packages']} == ids and len(inventory['packages']) == 2, 'Package allowlist mismatch')
    packages = []
    tooling_hashes = set()
    for row in inventory['packages']:
        package_id = row['id']
        require(row['version'] == VERSION, 'Inventory package version mismatch')
        require(VERSION in load('downloads/' + package_id.lower() + '.index.json')['versions'], 'Selected version absent from official feed')
        registration = load('downloads/' + package_id.lower() + '.registration.json')
        require(registration['listed'] is True and registration['packageContent'] == 'https://api.nuget.org/v3-flatcontainer/' + package_id.lower() + '/' + VERSION + '/' + package_id.lower() + '.' + VERSION + '.nupkg', 'NuGet registration identity mismatch')
        for role in ['nupkg', 'snupkg']:
            require(row[role]['file'] == package_id + '.' + VERSION + '.' + role, 'Inventory artifact filename mismatch')
        signed_path = BASE / 'downloads' / (package_id.lower() + '.' + VERSION + '.nupkg')
        symbols_path = signed_path.with_suffix('.snupkg')
        signed, symbols = signed_path.read_bytes(), symbols_path.read_bytes()
        unsigned_sha, unsigned_size = unsigned_digest(signed)
        require(unsigned_sha == row['nupkg']['sha256'].lower() and unsigned_size == row['nupkg']['sizeBytes'], 'Exact pre-signing inventory identity mismatch')
        require(sha(symbols) == row['snupkg']['sha256'].lower() and len(symbols) == row['snupkg']['sizeBytes'], 'Symbols identity mismatch')
        signature = load('observations/' + package_id.lower() + '.signature.command.json')
        require(signature['exitCode'] == 0, 'Signature verification failed')
        signature_log = (BASE / 'observations' / (package_id.lower() + '.signature.stdout.log')).read_text()
        require('Signature type: Repository' in signature_log and 'Owners: Hexalith' in signature_log and 'Service index: https://api.nuget.org/v3/index.json' in signature_log, 'Signature repository identity mismatch')
        require('Successfully verified package' in signature_log, 'Missing successful signature result')
        require(f"Verified remote '{package_id}' '{VERSION}' SHA-256 {sha(signed).upper()}" in (BASE / 'observations/release-job.log').read_text(), 'Current signed hash differs from official publication log')
        symbol_entries = []
        tooling_entries = []
        with zipfile.ZipFile(signed_path) as archive, zipfile.ZipFile(symbols_path) as sym:
            for current, role in [(archive, 'DotnetTool'), (sym, 'SymbolsPackage')]:
                names = current.namelist()
                require(len(names) == len(set(names)), 'Duplicate archive entries')
                nuspec, = [n for n in names if n.endswith('.nuspec')]
                metadata = ET.fromstring(current.read(nuspec)).find('{*}metadata')
                require(metadata.find('{*}id').text == package_id and metadata.find('{*}version').text == VERSION, 'Nuspec identity mismatch')
                require([t.attrib['name'] for t in metadata.findall('{*}packageTypes/{*}packageType')] == [role], 'Artifact role mismatch')
                require(metadata.find('{*}repository').attrib == {'type': 'git', 'url': REPOSITORY, 'branch': 'refs/heads/main', 'commit': SOURCE}, 'Nuspec repository mismatch')
            pdbs = [n for n in sym.namelist() if n.endswith('.pdb')]
            require(bool(pdbs), 'No symbol PDBs')
            for name in pdbs:
                payload = sym.read(name)
                require(name in archive.namelist() and archive.read(name) == payload, 'Symbol/package PDB mismatch')
                symbol_entries.append({'path': name, 'sha256': sha(payload), 'size': len(payload)})
            for name in archive.namelist():
                if not name.endswith('/Hexalith.Builds.Tooling.dll'):
                    continue
                payload = archive.read(name)
                tooling_hashes.add(sha(payload))
                with tempfile.TemporaryDirectory(prefix='story64-publication-inspection-') as temporary:
                    temporary = Path(temporary)
                    dll = temporary / 'Hexalith.Builds.Tooling.dll'
                    dll.write_bytes(payload)
                    command = ['pwsh', '-NoProfile', '-File', str(BASE / 'inspect-tooling.ps1'), '-Path', str(dll), '-OutputDirectory', str(temporary / 'result')]
                    child = subprocess.run(command, capture_output=True)
                    require(child.returncode == 0, 'Tooling inspection failed: ' + child.stderr.decode(errors='replace'))
                    metadata = json.loads((temporary / 'result/tooling.json').read_text(encoding='utf-8-sig'))
                    require(metadata['identity'] == 'Hexalith.Builds.Tooling, Version=4.39.0.0, Culture=neutral, PublicKeyToken=null', 'Tooling assembly identity mismatch')
                    require(metadata['informationalVersion'] == VERSION + '+' + SOURCE, 'Tooling source identity mismatch')
                    require((temporary / 'result/embedded-catalog.json').read_bytes() == evaluated, 'Embedded/evaluated catalog mismatch')
                    tooling_entries.append({'path': name, 'sha256': sha(payload), 'identity': metadata['identity'], 'informationalVersion': metadata['informationalVersion'], 'embeddedCatalogSha256': metadata['embeddedCatalogSha256']})
        require(len(tooling_entries) == (2 if package_id.endswith('Module.Cli') else 1), 'Missing or extra Tooling copies')
        packages.append({'id': package_id, 'version': VERSION, 'signedSha256': sha(signed), 'signedSize': len(signed), 'qualifiedUnsignedSha256': unsigned_sha, 'qualifiedUnsignedSize': unsigned_size, 'symbolsSha256': sha(symbols), 'symbolsSize': len(symbols), 'symbolPdbs': symbol_entries, 'tooling': tooling_entries, 'signature': 'verified NuGet.org repository / owner Hexalith'})
    require(len(tooling_hashes) == 1, 'Tooling copies differ')
    report = {'schema': 'story64.official-publication-verification.v1', 'verifiedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'result': 'passed', 'storyStatus': 'in-progress', 'candidateAcceptance': False, 'source': SOURCE, 'tree': TREE, 'version': VERSION, 'releaseRun': run['html_url'], 'release': release['html_url'], 'tag': 'v' + VERSION, 'inventoryUrl': asset['browser_download_url'], 'inventorySha256': sha(inventory_bytes), 'releaseEligible': True, 'fixturesVerified': len(fixture['files']), 'trackedFixtureCount': len(tracked), 'trackedFixtureOmissions': hidden_omissions, 'fixtureCoverageLimit': 'Selected qualifier enumerates without -Force; hidden paths are outside its manifest. Every listed fixture matches selected Git bytes.', 'qualificationEvidenceEntries': len(evidence_names), 'requiredEvidenceEntriesCovered': len(required), 'qualificationEvidenceLimit': 'Original 55 control-output files are represented by official inventory digests; their raw bytes were not attached to the release. Hosted log and independent local qualifier provide separate execution evidence.', 'catalogBlob': '37148298dfd3a4ae35d89dd5f4677e06e89149ad', 'catalogSha256': sha(source_catalog), 'embeddedCatalogSha256': sha(evaluated), 'toolingSha256': next(iter(tooling_hashes)), 'packages': packages}
    report['fixtureCheckoutConversions'] = checkout_conversions
    report['ci'] = {'runId': ci['id'], 'attempt': ci['run_attempt'], 'event': ci['event'], 'source': ci['head_sha'], 'conclusion': ci['conclusion'], 'url': ci['html_url']}
    report['fixtureCoverageLimit'] = 'Selected qualifier enumerates without -Force; hidden paths are outside its manifest. Every listed fixture matches selected Git checkout bytes, including tracked .gitattributes conversions recorded separately.'
    result = json.dumps(report, indent=2) + '\n'
    if args.output:
        with args.output.open('x') as file:
            file.write(result)
    print(result)


if __name__ == '__main__':
    main()
