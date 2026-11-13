"""Read-only post-managed census. No builds, restoration, assembly loads or topology actions."""
from pathlib import Path
import datetime, hashlib, json, runpy

ROOT = Path('/home/administrator/projects/hexalith/projects')
LIVE = Path('/tmp/story65-memo-rw-acceptance-live-20261107')
HELPER = Path('/tmp/story65-invocation-transport-ops-prep-20261107/live/ops_common.py')
PRIOR = Path('/tmp/story65-receiver-cpu-watcher-qualification-20261107/parent-final-ordinary-binary-audit.json')
NATIVE = Path('/tmp/story65-memo-rw-native-20261107/binary-source-audit.json')
CONTROLS = Path('/tmp/story65-memo-rw-native-20261107/source-controls.json')
OUTPUT = Path(str(LIVE) + '-ordinary-binary-source-audit.json')


def main():
    assert not OUTPUT.exists()
    ns = runpy.run_path(str(HELPER))
    sha, marker, census = ns['sha'], ns['marker'], ns['census']
    assert sha(PRIOR) == '022a574c6046df1b54b6949af3710c0753d8764b8b8d0587706a9c15993f65a5'
    prior = json.loads(PRIOR.read_text())
    native = json.loads(NATIVE.read_text())
    qualified = json.loads(NATIVE.with_name('parent-native-qualification.json').read_text())
    assert sha(NATIVE) == qualified['binaryAuditSha256']
    assert sha(NATIVE.with_name('parent-native-qualification.json')) == '93ab46016105006e2572f47960c6a9a7c2203f0db622a764e5c0525028fe6f37'
    native_before = {row['path']: row['sha256'] for row in native['files']}
    assert len(native_before) == 147
    report = json.loads((LIVE / 'run.json').read_text())
    manifest = json.loads((LIVE / 'source-manifest.json').read_text())
    inventory = json.loads((HELPER.parent / 'affected-inventory.json').read_text())
    owned = set(inventory['projects'].values()) | {inventory['nativeProject']}
    actual_graph = set(report['compilationProfile'])
    for host in report['liveHostSourceReferences'].values():
        assert host['allSourceProjects'] is True
        for source in host['sourceProjects'].values():
            path = Path(source)
            assert path.is_absolute() and path.resolve().is_relative_to(ROOT)
            relative = str(path.relative_to(ROOT))
            assert relative in manifest and sha(path) == manifest[relative]
            actual_graph.add(relative)
    owned |= actual_graph
    dependencies = {}
    for name, proof in prior['dependencies'].items():
        before = {row['path']: row['sha256'] for row in proof['copies']}
        before.update({path: h for path, h in native_before.items() if Path(path).name == name + '.dll'})
        current = {kind: sha(ns['module_expected'](name, kind)) for kind in ('full', 'pdb', 'ref', 'refint')}
        rows = census(name)
        for row in rows:
            assert not row['privateMarker'], row['path']
            if row['path'] in before and row['sha256'] == before[row['path']]:
                row['coherenceScope'] = 'exact-recorded-prior-ordinary-generation'
            else:
                assert row['project'] in owned and row['sha256'] == current[row['kind']], row['path']
                row['coherenceScope'] = 'current-producer-in-qualified-or-managed-source-graph'
        assert set(before) <= {row['path'] for row in rows}
        dependencies[name] = {'currentProducer': current, 'copies': rows,
            'exactPrior': sum(row['coherenceScope'].startswith('exact') for row in rows),
            'currentScoped': sum(row['coherenceScope'].startswith('current') for row in rows),
            'blanketHistoricalOrAllCopiesCurrentEqualityClaimed': False}
    before_sdk = {row['path']: row['sha256'] for row in prior['sdk']['copies']}
    before_sdk.update({path: h for path, h in native_before.items() if Path(path).name == 'Hexalith.EventStore.ServiceDefaults.dll'})
    current_sdk = {kind: sha(ns['ordinary_expected'](kind)) for kind in ('full', 'pdb', 'ref', 'refint')}
    assert current_sdk['full'] == '6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
    assert current_sdk['pdb'] == 'ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
    sdk_rows = census()
    for row in sdk_rows:
        assert not row['privateMarker'], row['path']
        if row['path'] in before_sdk and row['sha256'] == before_sdk[row['path']]:
            row['coherenceScope'] = 'exact-recorded-prior-ordinary-generation'
        else:
            assert row['project'] in owned and row['sha256'] == current_sdk[row['kind']], row['path']
            row['coherenceScope'] = 'current-sdk-in-qualified-or-managed-source-graph'
    assert set(before_sdk) <= {row['path'] for row in sdk_rows}
    sdk8 = []
    for entry in inventory['knownServiceDefaultsCopies']:
        file = ROOT / entry['path']
        assert sha(file) == current_sdk['full'] and sha(file.with_suffix('.pdb')) == current_sdk['pdb']
        sdk8.append({'path': entry['path'], 'dllSha256': sha(file), 'pdbSha256': sha(file.with_suffix('.pdb'))})
    assert len(sdk8) == 8
    native_rows = []
    for relative, before in native_before.items():
        file = ROOT / relative
        assert not marker(file)
        native_rows.append({'path': relative, 'sha256': sha(file), 'privateMarkersAbsent': True, 'changedSinceQualifiedNative': sha(file) != before})
    artifacts = ns['native_artifacts']()
    assert len(artifacts) == 6 and all(not row['privateMarker'] for row in artifacts)
    controls = json.loads(CONTROLS.read_text())['sourceSha256']
    assert len(controls) == 56 and all(sha(ROOT / relative) == expected for relative, expected in controls.items())
    assert all(sha(ROOT / relative) == expected for relative, expected in manifest.items())
    result = {'auditedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'auditScriptSha256': sha(__file__), 'helperSha256': sha(HELPER),
        'priorOrdinaryAuditSha256': sha(PRIOR), 'qualifiedNativeBinaryAuditSha256': sha(NATIVE),
        'sourceControls56AllMatched': True, 'fullManifestInputs': len(manifest), 'fullManifestMismatches': 0,
        'actualManagedSourceGraphProjects': sorted(actual_graph), 'allGraphProjectsBoundToManifest': True,
        'sdk137': sdk_rows, 'sdkCurrentProducer': current_sdk, 'sdk8CurrentProducerDllPdb': sdk8,
        'runtimeSdkCopyProofScope': 'source-output copies used by the original six-role source-backed runtime profile; no fresh post-exit maps claim',
        'dependencyGenerations': dependencies, 'native147': native_rows, 'nativeSixArtifacts': artifacts,
        'exactPriorOrCurrentScopedRule': True, 'allCopiesSameGenerationClaimed': False,
        'noBuildRestoreAssemblyLoadOrTopologyMutation': True, 'featureAcceptanceClaimed': False}
    OUTPUT.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'output': str(OUTPUT), 'auditSha256': sha(OUTPUT), 'sdkCensus': len(sdk_rows),
        'sdk8DllPdbCurrent': True, 'native147Clean': True, 'source56Match': True,
        'sourceManifest': len(manifest), 'sourceGraph': len(actual_graph),
        'dependencyScopes': {name: {k: row[k] for k in ('exactPrior', 'currentScoped')} for name, row in dependencies.items()}}))


if __name__ == '__main__':
    main()
