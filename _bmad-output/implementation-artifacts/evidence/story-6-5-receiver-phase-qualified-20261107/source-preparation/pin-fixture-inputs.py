from pathlib import Path
import datetime
import hashlib
import json

ROOT = Path('/home/administrator/projects/hexalith/projects')
OWNER = ROOT / 'references/Hexalith.EventStore'
PREP = Path(__file__).parent
TEST_BIN = OWNER / 'tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0'
PRODUCER = Path('/tmp/story65-receiver-phase-producer-prep-20261107')
sha = lambda path: hashlib.sha256(Path(path).read_bytes()).hexdigest()
inputs = {}

def pin(path):
    path = Path(path)
    assert path.is_file() and not path.is_symlink(), str(path)
    digest = sha(path)
    previous = inputs.setdefault(str(path), digest)
    assert previous == digest
    return {'path': str(path), 'sha256': digest}

controls = json.loads(Path('/tmp/story65-memo-rw-native-20261107/source-controls.json').read_bytes())['sourceSha256']
assert len(controls) == 56
for name, expected in controls.items():
    path = Path(name) if Path(name).is_absolute() else ROOT / name
    assert pin(path)['sha256'] == expected

common = {}
reserved = {'Hexalith.EventStore.ServiceDefaults.dll', 'Hexalith.EventStore.ServiceDefaults.pdb',
            'Hexalith.EventStore.Server.LiveSidecar.Tests.dll', 'Hexalith.EventStore.Server.LiveSidecar.Tests.pdb',
            'Story65.ReceiverPhase.Driver.dll', 'Story65.ReceiverPhase.Driver.pdb'}
for path in sorted(TEST_BIN.iterdir()):
    if not path.is_file(): continue
    if path.suffix in {'.dll', '.pdb'} or path.name.endswith(('.deps.json', '.runtimeconfig.json')) or path.name == 'xunit.runner.json':
        row = pin(path)
        if path.name not in reserved: common[path.name] = row
assert not any((TEST_BIN / name).exists() for name in reserved if name.startswith(('Story65.', 'Hexalith.EventStore.Server.LiveSidecar.Tests')))
assert all(name in common for name in ['Hexalith.EventStore.Server.Tests.dll', 'Hexalith.EventStore.Server.Tests.pdb',
    'Hexalith.EventStore.Server.Tests.deps.json', 'Hexalith.EventStore.Server.Tests.runtimeconfig.json'])

interface = json.loads((PRODUCER / 'oracle-candidate-interface.json').read_bytes())
attempt = Path(json.loads((PRODUCER / 'latest-scratch-producer-attempt.json').read_bytes())['attemptDirectory'])
receipt_path = attempt / 'receipt.json'
receipt = json.loads(receipt_path.read_bytes())
assert pin(receipt_path)['sha256'] == 'd7d77a65c2ff3a8d3d767eae1eecfabb847bc597504106008e1e7f4b15f0c4fd'
assert receipt['exitCode'] == 0 and receipt['rootModulesUnchanged'] and receipt['sourceCoreInputsUnchanged']
sdk = {
    'ordinary': {'dll': pin(interface['oracle']['sourceDll']), 'pdb': pin(interface['oracle']['sourcePdb'])},
    'private': {'dll': pin(interface['candidate']['targetDll']), 'pdb': pin(interface['candidate']['targetPdb'])},
}
assert sdk['ordinary']['dll']['sha256'] == '6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
assert sdk['ordinary']['pdb']['sha256'] == 'ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6'
assert sdk['private']['dll']['sha256'] == receipt['candidateDllSha256'] == '32afeb4a3b6f507cdd3ed464e0e23177169607a6ec442949b6caa75d0e64c63e'
assert sdk['private']['pdb']['sha256'] == receipt['candidatePdbSha256'] == '2b59b42b5d47ca6ce08079dc4ea9ae4c691e747a710822b1386aea5c1c31cca7'
resources = {name: pin(OWNER / name) for name in ['Hexalith.EventStore.slnx', 'deploy/dapr/eventstore-routing-catalog.json']}

handoff_path = Path('/tmp/story65-memo-rw-native-20261107/final-transfer-qualified-handoff.json')
native = json.loads(handoff_path.read_bytes())
current_app147 = {}
for name in native['app147Sha256']:
    path = Path(name) if Path(name).is_absolute() else ROOT / name
    current_app147[name] = pin(path)['sha256']
assert len(current_app147) == 147
limits = Path('/tmp/story65-memo-rw-acceptance-live-20261107-ordinary-binary-source-audit-with-provenance-limits.json')
pin(limits)

for path in [Path('/tmp/story65-receiver-phase-ops-prep-20261107/native-reference-case-set.json'),
             Path('/tmp/story65-receiver-phase-ops-prep-20261107/native47-source-derivation.json'),
             Path('/tmp/story65-memo-rw-owner-native-20261107/command-source-freeze-receipt.json'),
             Path('/tmp/story65-memo-rw-owner-native-20261107/parent-owner-audit.json'),
             handoff_path, PRODUCER / 'oracle-candidate-interface.json', PRODUCER / 'latest-scratch-producer-attempt.json',
             Path('/home/administrator/.nuget/packages/dotnet-inspect.linux-x64/0.27.0/tools/any/linux-x64/dotnet-inspect'),
             Path('/home/administrator/.nuget/packages/xunit.v3.runner.inproc.console/4.0.2/lib/net8.0/xunit.v3.runner.inproc.console.xml')]:
    pin(path)
compiler = json.loads((attempt / 'actual-compiler-inputs.json').read_bytes())
pin(attempt / 'actual-compiler-inputs.json')
for row in compiler['inputs']:
    assert pin(row['path'])['sha256'] == row['sha256']
for sub in ['Microsoft.NETCore.App', 'Microsoft.AspNetCore.App']:
    for path in sorted((Path('/home/administrator/.dotnet/shared') / sub / '10.0.14').glob('*.dll')): pin(path)
for path in sorted((OWNER / 'tests/Hexalith.EventStore.Server.Tests/Routing').glob('*.cs')):
    if path.name in ['RouteCatalogTestFixture.cs', 'ReceiverImagePrecheckFixture.cs', 'TechnicalHttpCatalogFixture.cs',
                     'TechnicalHttpInstalledMetadataTests.cs', 'TechnicalHttpNativeRpcTests.cs',
                     'ReceiverImagePrecheckTests.cs', 'NativeDispatchDiagnosticsTests.cs']: pin(path)

plan = {'filePreparationOnly': True, 'currentExternalInputsSha256': inputs, 'sourceSha256': controls,
        'app147Sha256': current_app147, 'commonFilesBeforeNewDriverCompanionBuild': common, 'resources': resources, 'sdk': sdk,
        'postBuildRuntimeManifestStillRequired': True, 'noSdkFallbackToWorkspaceOrOtherVariant': True,
        'topLevelTestRuntimeFilesPinned': len(common), 'noAppsettingsOrCredentialFilesCopied': True,
        'fixtureSlnxMarkerNotUsedToBuildAnyApplication': True, 'rootClosedOrdinaryProvenanceLimitAudit': pin(limits),
        'allDependencyGenerationsCurrentSourceCoherentClaimed': False, 'oldVsCurrentCrossmoduleLimitsNotWhitelistedOrRebuilt': True,
        'isolatedSdkProducerReceipt': pin(receipt_path), 'applicationAssemblyLoadsOrFixtureRuns': 0}
(PREP / 'input-pins.json').write_text(json.dumps(plan, indent=2) + '\n')
print(json.dumps({'externalInputs': len(inputs), 'sourceControls': len(controls), 'app147': len(current_app147),
                  'baseCommonFiles': len(common), 'sdkPrivateSha256': sdk['private']['dll']['sha256'],
                  'inputPinsSha256': sha(PREP / 'input-pins.json'), 'runtimeBuildAndExecutionReleased': False}, indent=2))
