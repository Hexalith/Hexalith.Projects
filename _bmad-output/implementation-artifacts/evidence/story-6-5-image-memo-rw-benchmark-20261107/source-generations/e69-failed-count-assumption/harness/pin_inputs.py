"""Prepare metadata-only frozen inputs; this does not invoke dotnet or load an assembly."""
from pathlib import Path
import datetime, hashlib, json, os, shutil, stat

ROOT = Path('/home/administrator/projects/hexalith/projects')
PREP = Path('/tmp/story65-image-memo-rw-prep-20261107')
HARNESS = PREP / 'harness'


def sha(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK)
    try:
        assert stat.S_ISREG(os.fstat(fd).st_mode)
        with os.fdopen(fd, 'rb', closefd=False) as stream:
            return hashlib.file_digest(stream, 'sha256').hexdigest()
    finally:
        os.close(fd)


def save(name, data):
    path = HARNESS / name
    assert not path.exists()
    path.write_text(json.dumps(data, indent=2) + '\n')


def main():
    controls_path = Path('/tmp/story65-receiver-cpu-prep-20261107/watcher/source-controls.json')
    controls = json.loads(controls_path.read_text())['sourceSha256']
    assert len(controls) == 55 and all(sha(ROOT / key) == value for key, value in controls.items())
    old_baseline = json.loads(Path('/tmp/story65-invocation-transport-prep-20261107/source-app-baseline.json').read_text())
    apps = {name: sha(ROOT / name) for name in old_baseline['app147Sha256']}
    assert len(apps) == 147
    owner = ROOT / 'references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0'
    owner_inputs = {str(path): sha(path) for path in sorted(owner.iterdir())
                    if path.is_file() and path.suffix in {'.dll', '.pdb', '.json'}}
    dotnet = Path(shutil.which('dotnet')).resolve()
    shared_root = dotnet.parent / 'shared'
    framework = {str(path): sha(path) for name in ('Microsoft.NETCore.App', 'Microsoft.AspNetCore.App')
                 for path in sorted((shared_root / name / '10.0.14').glob('*.dll'))}
    assert framework
    ordinary = {}
    for name in ('Hexalith.EventStore.ServiceDefaults', 'Hexalith.EventStore.Contracts'):
        base = ROOT / 'references/Hexalith.EventStore/src' / name / 'bin/Debug/net10.0'
        for suffix in ('.dll', '.pdb', '.deps.json'):
            path = base / (name + suffix)
            assert path.is_file()
            ordinary[str(path)] = sha(path)
    sdk = str(ROOT / 'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll')
    assert ordinary[sdk] == '5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872'
    inputs = {str(ROOT / name): value for name, value in controls.items()}
    inputs.update({str(ROOT / name): value for name, value in apps.items()})
    inputs.update(owner_inputs)
    inputs.update(framework)
    inputs.update(ordinary)
    inputs[str(dotnet)] = sha(dotnet)
    for name in ('global.json', '.editorconfig', '.gitattributes'):
        inputs[str(ROOT / name)] = sha(ROOT / name)
    runtimeconfig = owner / 'Hexalith.Folders.Server.runtimeconfig.json'
    owner_gc = json.loads(runtimeconfig.read_text())['runtimeOptions']['configProperties']['System.GC.Server']
    assert owner_gc is True
    save('input-pins.json', {
        'filePreparationOnly': True, 'compiledOracleSdkSha256': ordinary[sdk],
        'sourceSha256': controls, 'currentApp147Sha256': apps,
        'currentExternalInputsSha256': inputs, 'ordinaryReferenceFilesSha256': ordinary,
        'ownerBinMetadataInputsSha256': owner_inputs, 'frameworkInputsSha256': framework,
        'ownerServerGc': owner_gc, 'builds': 0, 'assemblyLoads': 0, 'measurements': 0})
    save('provenance.json', {
        'preparedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'scope': 'single memo read/write synchronization candidate; no production changes',
        'apiSha256': sha(PREP / 'harness-api.json'),
        'normalizationSha256': sha(PREP / 'source-normalization.json'),
        'actualSdkOracleSha256': ordinary[sdk],
        'current55ProductionControlSourcesUnchanged': True, 'current147AppsMetadataPinnedOnly': True,
        'allInputHashingWasReadOnlyNoAssemblyLoads': True,
        'ownerRuntimeconfigSha256': sha(runtimeconfig), 'ownerServerGc': owner_gc,
        'historicalStreamChunkBuilderReadForInventoryShapeOnlyNoMetricsReused': True,
        'controlOracleOutputAgreementRequiredBeforeTiming': True,
        'candidateBenefitAndFullHostDeadlineGainClaimed': False,
        'builds': 0, 'loads': 0, 'benchmarks': 0, 'applicationHosts': 0})
    print(json.dumps({'inputPins': str(HARNESS / 'input-pins.json'), 'externalInputs': len(inputs),
                      'source55': len(controls), 'apps147': len(apps), 'ownerInputs': len(owner_inputs),
                      'frameworkInputs': len(framework), 'builds': 0, 'loads': 0, 'measurements': 0}))


if __name__ == '__main__':
    main()
