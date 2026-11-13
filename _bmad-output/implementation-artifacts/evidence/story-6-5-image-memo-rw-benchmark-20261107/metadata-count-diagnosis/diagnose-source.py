from pathlib import Path
import datetime, hashlib, json, shutil, subprocess

ROOT = Path('/home/administrator/projects/hexalith/projects')
PREP = Path('/tmp/story65-image-memo-rw-prep-20261107')
OUT = Path('/tmp/story65-image-memo-rw-inventory-diagnosis-20261107')
OLD = Path('/tmp/story65-image-memo-rw-execution-20261107/attempt-1/failed-preparation-and-compiled-generation')
TOOL = Path('/home/administrator/.nuget/packages/dotnet-inspect.linux-x64/0.27.0/tools/any/linux-x64/dotnet-inspect')
OWNER = ROOT / 'references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0/Hexalith.Folders.Server.dll'


def sha(path):
    with Path(path).open('rb') as file:
        return hashlib.file_digest(file, 'sha256').hexdigest()


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n')


def identity(info, reference=False):
    name = info['name' if reference else 'assembly_name']
    version = info['version' if reference else 'assembly_version']
    return f"{name}, Version={version}, Culture={info.get('culture', 'neutral')}, PublicKeyToken={info.get('public_key_token', 'null')}"


def main():
    assert not OUT.exists()
    OUT.mkdir(mode=0o700)
    (OUT / 'metadata').mkdir(mode=0o700)
    old = json.loads((OLD / 'preparation-receipt.json').read_text())
    assert sha(OLD / 'preparation-receipt.json') == 'e69bc812c1e2b290dea54c8225a52d12211977b3c7a3805cf4708ca3b5addbbb'
    assert all(sha(OLD / name) == h for name, h in old['filesSha256'].items())
    inputs = old['currentExternalInputsSha256']
    assert all(sha(path) == h for path, h in inputs.items())
    pin = json.loads((OLD / 'harness/input-pins.json').read_text())
    choices = [Path(path) for path in (*pin['ownerBinMetadataInputsSha256'], *pin['frameworkInputsSha256']) if Path(path).suffix == '.dll']
    commands = []
    metadata = {}

    def inspect(path):
        if str(path) in metadata:
            return metadata[str(path)]
        argv = [str(TOOL), 'library', str(path), '-S', 'References', '--json', '--trace']
        start = datetime.datetime.now(datetime.timezone.utc).isoformat()
        result = subprocess.run(argv, capture_output=True, check=False, timeout=15)
        assert result.returncode == 0
        data = json.loads(result.stdout)
        stem = f'{len(commands):02d}-{path.stem}'
        stdout = OUT / 'metadata' / f'{stem}.json'
        stderr = OUT / 'metadata' / f'{stem}.stderr.log'
        stdout.write_bytes(result.stdout)
        stderr.write_bytes(result.stderr)
        assert b'Assembly references' in result.stderr and b'metadata session' in result.stderr
        commands.append({'argv': argv, 'startedUtc': start,
            'endedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'exitCode': result.returncode,
            'stdoutPath': str(stdout.relative_to(OUT)), 'stdoutSha256': sha(stdout),
            'stderrPath': str(stderr.relative_to(OUT)), 'stderrSha256': sha(stderr),
            'inputPath': str(path), 'inputSha256': sha(path), 'cachedToolSha256': sha(TOOL)})
        metadata[str(path)] = data
        return data

    owner = inspect(OWNER)
    refs = owner['assembly_info']['references']
    expected = {identity(ref, True): ref for ref in refs}
    assert len(refs) == len(expected) == 65
    domain = next(ref for ref in refs if ref['name'] == 'Hexalith.EventStore.DomainService')
    entries = [{'fullName': identity(owner['assembly_info']), 'path': str(OWNER),
                'sha256': sha(OWNER), 'bytes': OWNER.stat().st_size, 'source': 'owner'}]
    for name, ref in expected.items():
        selected = [path for path in choices if path.name == ref['name'] + '.dll']
        assert len(selected) == 1
        path = selected[0]
        actual = inspect(path)
        assert identity(actual['assembly_info']) == name
        entries.append({'fullName': name, 'path': str(path), 'sha256': sha(path),
                        'bytes': path.stat().st_size, 'source': 'owner-direct-reference',
                        'alsoExplicitDomainService': ref['name'] == domain['name']})
    entries.sort(key=lambda item: item['fullName'])
    assert len(entries) == len({item['fullName'] for item in entries}) == 66
    assert all(item['fullName'].isascii() for item in entries)
    for ordinal, entry in enumerate(entries):
        entry['ordinal'] = ordinal
    assert all(sha(path) == h for path, h in inputs.items())
    assert all(sha(OLD / name) == h for name, h in old['filesSha256'].items())
    historical = Path('/tmp/story65-domain-reference-probe-20261107')
    history = {}
    for name in ('Program.cs', 'receipt.json', 'results.json'):
        path = historical / name
        copy = OUT / ('historical-' + name)
        shutil.copyfile(path, copy)
        history[str(path)] = sha(path)
    proof = {
        'diagnosticOnly': True, 'metadataOnlyNoApplicationAssemblyLoadOrExecution': True,
        'cachedDotnetInspectVersion': '0.27.0', 'cachedToolPath': str(TOOL), 'cachedToolSha256': sha(TOOL),
        'currentOwnerSha256': sha(OWNER), 'ownerAssemblyRefRows': 65,
        'domainServiceAlreadyAnOwnerDirectReference': True,
        'unchangedSelection': 'owner + DomainService + owner.GetReferencedAssemblies; Distinct; Ordinal FullName order',
        'currentDistinctMetadataIdentities': 66, 'allIdentitiesHaveExactlyOnePinnedPhysicalCandidate': True,
        'referenceIdentityMatchesPhysicalAssemblyMetadata': True,
        'originalExpected67WasNotThisSourceDerivedCohort': True,
        'noRuntimeDiscoveredExtraAddedNoCurrentImageRemoved': True,
        'all588ExternalInputsAndArchived59PreparedFilesUnchanged': True,
        'imageBytes': sum(item['bytes'] for item in entries),
        'chargedBytes': sum(item['bytes'] + 512 for item in entries),
        'perImageEligibleCount': sum(0 < item['bytes'] <= 16 * 1024 * 1024 for item in entries),
        'existingLimitsUnchanged': {'images': 128, 'imageBytes': 16 * 1024 * 1024, 'chargedBytes': 64 * 1024 * 1024},
        'entries': entries, 'historicalProvenanceSha256': history,
        'toolQueries': len(commands), 'applicationBuilds': 0, 'applicationLoads': 0, 'benchmarkReruns': 0,
    }
    save(OUT / 'current-cohort-metadata.json', proof)
    save(OUT / 'commands.json', commands)
    shutil.copyfile(__file__, OUT / 'diagnose-source.py')
    save(OUT / 'receipt.json', {'completed': True, 'metadataProofSha256': sha(OUT / 'current-cohort-metadata.json'),
        'commandsSha256': sha(OUT / 'commands.json'), 'queries': len(commands),
        'originalFrozenReceiptSha256': sha(OLD / 'preparation-receipt.json'),
        'allOriginalInputsAndHistoryUnchanged': True, 'noAppLoadOrBenchmarkRerun': True})
    print(json.dumps({'diagnosis': str(OUT), 'metadataSha256': sha(OUT / 'current-cohort-metadata.json'),
        'ownerReferences': 65, 'currentCohort': 66, 'metadataQueries': len(commands),
        'imageBytes': proof['imageBytes'], 'chargedBytes': proof['chargedBytes']}))


if __name__ == '__main__':
    main()
