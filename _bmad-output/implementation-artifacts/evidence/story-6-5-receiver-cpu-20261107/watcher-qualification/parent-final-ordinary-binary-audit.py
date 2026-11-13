from pathlib import Path
import datetime
import hashlib
import json
import runpy

ROOT = Path('/home/administrator/projects/hexalith/projects')
LIVE = Path('/tmp/story65-receiver-cpu-owner-live-20261107')
OUT = Path(__file__).parent
HELPER = Path('/tmp/story65-invocation-transport-ops-prep-20261107/live/ops_common.py')
PRIOR = HELPER.parent / 'parent-ordinary-restoration-audit.json'
ns = runpy.run_path(str(HELPER))
prior = json.loads(PRIOR.read_text())
inventory = json.loads((HELPER.parent / 'affected-inventory.json').read_text())
report = json.loads((LIVE / 'run.json').read_text())
manifest = json.loads((LIVE / 'source-manifest.json').read_text())
sha = ns['sha']
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
    before = {row['path']: row for row in proof['copies']}
    current = {kind: sha(ns['module_expected'](name, kind)) for kind in ('full', 'pdb', 'ref', 'refint')}
    rows = ns['census'](name)
    for row in rows:
        assert not row['privateMarker'], row['path']
        if row['path'] in before and row['sha256'] == before[row['path']]['sha256']:
            row['coherenceScope'] = 'unchanged-before-run-ordinary-generation'
        else:
            assert row['project'] in owned and row['sha256'] == current[row['kind']], row['path']
            row['coherenceScope'] = 'current-producer-in-actual-source-build-graph'
    assert set(before) <= {row['path'] for row in rows}
    dependencies[name] = {
        'currentProducer': current,
        'copies': rows,
        'unchangedBeforeRun': sum(row['coherenceScope'].startswith('unchanged') for row in rows),
        'newCurrentProducer': sum(row['coherenceScope'].startswith('current') for row in rows),
        'historicalBlanketGenerationEqualityClaimed': False,
    }
sdk = ns['sdk_proof']('ordinary')
assert sdk['producerSha256'] == '5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872'
assert sdk['producerPdbSha256'] == '80a7174c81e2dc92d8abc4eea2bc371b672066d85c444f66e437dbe13e168af7'
native = ns['native147_proof']('ordinary')
artifacts = ns['native_artifacts']()
assert len(artifacts) == 6 and all(not row['privateMarker'] for row in artifacts)
qual = json.loads(Path('/tmp/story65-receiver-cpu-qualification-20261107/collector-qualification-receipt.json').read_text())
changed = sum(sha(ROOT / path) != digest for path, digest in qual['app147Sha256'].items())
result = {
    'auditedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'auditScriptSha256': sha(__file__),
    'helperSha256': sha(HELPER),
    'priorOrdinaryRestorationAuditSha256': sha(PRIOR),
    'actualManagedSourceGraphProjects': sorted(actual_graph),
    'actualSourceGraphProjectsBoundToUnchangedFullManifest': True,
    'noSourceOverlayOrRestorationBuildPerformed': True,
    'ordinaryManagedBuildMayEmitFreshScopedGenerations': True,
    'historicalBlanketByteEquivalenceClaimed': False,
    'auditAdapterHistory': [
        'Initial reuse of forced-restoration all-owned-copies-current check failed after ordinary subset source builds; no repair build ran.',
        'Exact prior-generation-or-current-producer check initially used only the prior 14-project repair plan; four Server DLL/PDB copies in EventStore.Testing and Tenants.Server required the actual ordinary managed source graph.',
        'Final scope uses exact unchanged prior bytes, or exact current producer bytes in the explicit prior repair or current managed source graph; unknown hashes or projects still fail.',
    ],
    'native147ChangedSincePrelaunch': changed,
    'sdk': sdk,
    'dependencies': dependencies,
    'native147': native,
    'nativeArtifacts': artifacts,
}
target = OUT / 'parent-final-ordinary-binary-audit.json'
target.write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({
    'auditSha256': sha(target),
    'sdkCopies': sdk['sdkCensusCopies'],
    'allSdk8Current': sdk['allEightCurrentProducer'],
    'sourceGraphProjects': len(actual_graph),
    'dependencyScopes': {name: {key: value[key] for key in ('unchangedBeforeRun', 'newCurrentProducer')} for name, value in dependencies.items()},
    'native147Clean': native['allPrivateMarkersAbsent'],
    'nativeArtifacts': len(artifacts),
    'native147ChangedOrdinarySincePrelaunch': changed,
}))
