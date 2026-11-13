"""Metadata-only pinning, deliberately unavailable until Root supplies actual new owner/native proof."""
from pathlib import Path
import argparse, datetime, hashlib, json, os, re, shutil, stat

PREP = Path('/tmp/story65-image-memo-rw-final-compiled-prep-20261107')
ROOT = Path('/home/administrator/projects/hexalith/projects')
SDK = ROOT / 'references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll'


def sha(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK | os.O_CLOEXEC)
    try:
        assert stat.S_ISREG(os.fstat(fd).st_mode)
        with os.fdopen(fd, 'rb', closefd=False) as file:
            return hashlib.file_digest(file, 'sha256').hexdigest()
    finally:
        os.close(fd)


def proof(row):
    assert set(row) == {'path', 'sha256'} and Path(row['path']).is_absolute()
    assert re.fullmatch('[0-9a-f]{64}', row['sha256']) and sha(row['path']) == row['sha256']
    return json.loads(Path(row['path']).read_text())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--qualified-handoff', required=True)
    parser.add_argument('--handoff-sha256', required=True)
    args = parser.parse_args()
    handoff_path = Path(args.qualified_handoff)
    assert handoff_path.is_absolute() and re.fullmatch('[0-9a-f]{64}', args.handoff_sha256)
    assert sha(handoff_path) == args.handoff_sha256
    handoff = json.loads(handoff_path.read_text())
    assert handoff['schema'] == 1 and handoff['rootOwnerAndNativeQualificationPassed'] is True
    assert handoff['nativeAllPassed'] is True and type(handoff['nativeQualifiedUniquePasses']) is int and handoff['nativeQualifiedUniquePasses'] > 0
    assert handoff['compiledProductionMemoGate'] == 'System.Threading.ReaderWriterLockSlim'
    owner = proof(handoff['ownerReceipt'])
    native = proof(handoff['nativeAudit'])
    controls = proof(handoff['sourceControls'])
    assert isinstance(owner, dict) and isinstance(native, dict)
    source = controls['sourceSha256']
    assert type(source) is dict and len(source) == handoff['sourceCount'] and len(source) > 0
    inputs = {str(handoff_path): args.handoff_sha256}
    for key in ('ownerReceipt', 'nativeAudit', 'sourceControls'):
        inputs[handoff[key]['path']] = handoff[key]['sha256']
    for relative, expected in source.items():
        path = (ROOT / relative).resolve()
        assert path.is_relative_to(ROOT) and re.fullmatch('[0-9a-f]{64}', expected) and sha(path) == expected
        inputs[str(path)] = expected
    apps = handoff['app147Sha256']
    assert len(apps) == 147
    for relative, expected in apps.items():
        path = (ROOT / relative).resolve()
        assert path.is_relative_to(ROOT) and re.fullmatch('[0-9a-f]{64}', expected) and sha(path) == expected
        inputs[str(path)] = expected
    assert handoff['compiledSdk']['dllPath'] == str(SDK)
    assert handoff['compiledSdk']['pdbPath'] == str(SDK.with_suffix('.pdb'))
    for coordinate, field in [('dllPath', 'dllSha256'), ('pdbPath', 'pdbSha256')]:
        expected = handoff['compiledSdk'][field]
        assert re.fullmatch('[0-9a-f]{64}', expected) and sha(handoff['compiledSdk'][coordinate]) == expected
        inputs[handoff['compiledSdk'][coordinate]] = expected
    routing = SDK.parents[3] / 'Routing'
    for name in ('RouteCatalogImageDigestMemo.cs', 'RouteCatalogImageDigestScope.cs', 'RouteCatalogFreshStreamImageDigest.cs', 'RouteCatalogImageBuffers.cs'):
        current = routing / name
        copied = PREP / 'candidate' / name
        assert current.read_bytes() == copied.read_bytes().replace(b'Story65.ImageMemo.Candidate', b'Hexalith.EventStore.ServiceDefaults.Routing')
        assert str(current.relative_to(ROOT)) in source
    owner_bin = ROOT / 'references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0'
    assert sha(owner_bin / SDK.name) == handoff['compiledSdk']['dllSha256']
    assert sha(owner_bin / SDK.with_suffix('.pdb').name) == handoff['compiledSdk']['pdbSha256']
    for path in sorted(owner_bin.iterdir()):
        if path.is_file() and path.suffix in {'.dll', '.pdb', '.json'}:
            inputs[str(path)] = sha(path)
    dotnet = Path(shutil.which('dotnet')).resolve()
    inputs[str(dotnet)] = sha(dotnet)
    for framework in ('Microsoft.NETCore.App', 'Microsoft.AspNetCore.App'):
        for path in sorted((dotnet.parent / 'shared' / framework / '10.0.14').glob('*.dll')):
            inputs[str(path)] = sha(path)
    for name in ('Hexalith.EventStore.ServiceDefaults', 'Hexalith.EventStore.Contracts'):
        directory = ROOT / 'references/Hexalith.EventStore/src' / name / 'bin/Debug/net10.0'
        for suffix in ('.dll', '.pdb', '.deps.json'):
            path = directory / (name + suffix)
            inputs[str(path)] = sha(path)
    binding = {
        'sourcePinsReady': True, 'rootOwnerAndNativeQualificationPassed': True,
        'compiledProductionMemoGate': handoff['compiledProductionMemoGate'],
        'sdkDllPath': str(SDK), 'sdkDllSha256': handoff['compiledSdk']['dllSha256'],
        'sdkPdbSha256': handoff['compiledSdk']['pdbSha256'],
        'ownerReceiptSha256': handoff['ownerReceipt']['sha256'],
        'nativeAuditSha256': handoff['nativeAudit']['sha256'],
        'sourceControlsSha256': handoff['sourceControls']['sha256'],
        'sourceCount': len(source), 'sourceSha256': source,
        'nativeQualifiedUniquePasses': handoff['nativeQualifiedUniquePasses'],
        'currentInputsSha256': inputs,
        'qualificationBooleansSuppliedByRootAfterActualOwnerNativeAuditAndBoundByHashes': True,
        'sourceQualificationReady': False, 'executionReleased': False,
        'finalCompiledTransferNotYetExecuted': True,
    }
    dest = PREP / 'harness/final-binding.json'
    assert not dest.exists()
    dest.write_text(json.dumps(binding, indent=2) + '\n')
    print(json.dumps({'bindingPath': str(dest), 'bindingSha256': sha(dest),
        'compiledProductionSdkSha256': binding['sdkDllSha256'], 'sourceCount': len(source),
        'currentInputs': len(inputs), 'restoreBuildLoadRunExecuted': False, 'executionReleased': False}))


if __name__ == '__main__':
    main()
