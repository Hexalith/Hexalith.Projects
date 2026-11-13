"""Freeze only metadata/source hashes; never invoke a compiler, loader, benchmark or host."""
from pathlib import Path
import datetime, hashlib, json, os, stat

PREP = Path('/tmp/story65-image-memo-rw-prep-20261107')
ROOT = Path('/home/administrator/projects/hexalith/projects')


def sha(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK)
    try:
        assert stat.S_ISREG(os.fstat(fd).st_mode)
        with os.fdopen(fd, 'rb', closefd=False) as source:
            return hashlib.file_digest(source, 'sha256').hexdigest()
    finally:
        os.close(fd)


def main():
    receipt = PREP / 'preparation-receipt.json'
    assert not receipt.exists()
    pins = json.loads((PREP / 'harness/input-pins.json').read_text())
    assert len(pins['sourceSha256']) == 55 and len(pins['currentApp147Sha256']) == 147
    assert all(sha(ROOT / name) == h for name, h in pins['sourceSha256'].items())
    assert all(sha(ROOT / name) == h for name, h in pins['currentApp147Sha256'].items())
    external = dict(pins['currentExternalInputsSha256'])
    review = PREP / 'root-source-review.json'
    assert sha(review) == '3899a80e070fcfc3916e664df80f9fc756283742a2b2998b24554667b4095305'
    assert all(sha(Path(name)) == h for name, h in external.items())
    files = {}
    for path in sorted(PREP.rglob('*')):
        if path.is_dir() or any(part in {'bin', 'obj', '__pycache__', 'history'} for part in path.relative_to(PREP).parts):
            continue
        assert not path.is_symlink() and path.suffix.lower() not in {'.dll', '.pdb', '.exe', '.pyc'}
        files[str(path.relative_to(PREP))] = sha(path)
    data = {
        'createdUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'filePreparationOnly': True, 'sourcePinsReady': True, 'sourceQualificationReady': False,
        'executionReleased': False, 'allowStandaloneRestoreBuildLoadBenchmark': False,
        'applicationBuildsNativeTestsProfilesHostsOrProductionEditsAuthorized': False,
        'filesSha256': files, 'sourceSha256': pins['sourceSha256'],
        'app147Sha256': pins['currentApp147Sha256'], 'currentExternalInputsSha256': external,
        'rootExactSourceReviewSha256': sha(review),
        'fixedApiSha256': sha(PREP / 'harness-api.json'),
        'actualCompiledOracleSdkSha256': pins['compiledOracleSdkSha256'],
        'ownerBinInputFiles': len(pins['ownerBinMetadataInputsSha256']),
        'frameworkInputFiles': len(pins['frameworkInputsSha256']),
        'allCurrentInputsRehashedBeforeFreeze': True,
        'wholeRootAndReaderFinalManifestReviewStillRequired': True,
        'cohort': '67 source-derived Folders owner, DomainService and owner direct-reference images in original Ordinal FullName order',
        'completeDiscoveredExecutableOwnersUnionClaimed': False,
        'domainAndTechnicalPathsKeepOriginalReadsAndBounds': True,
        'oracleControlCandidateCompleteIdentityAgreementRequiredBeforeTiming': True,
        'parityChecksAndBoundsLifecycleStillUnexecuted': True,
        'readers': [1, 2, 4], 'balancedVariantPermutations': 6,
        'plannedRawBatches': 324, 'plannedObservations': 4032,
        'serverGcMustMatchOwnerRuntimeconfig': True,
        'runtimeCpuOverride': None, 'expectedRuntimeProcessors': 14,
        'configuration': 'Debug', 'optimize': False,
        'builds': 0, 'assemblyLoads': 0, 'runs': 0, 'applicationHosts': 0,
        'candidateBenefitOrFullHostDeadlineAcceptanceClaimed': False,
    }
    receipt.write_text(json.dumps(data, indent=2) + '\n')
    print(json.dumps({'receipt': str(receipt), 'sha256': sha(receipt), 'preparedFiles': len(files),
        'source55': len(data['sourceSha256']), 'app147': len(data['app147Sha256']),
        'externalInputs': len(external), 'executionReleased': False,
        'restoreBuildLoadBenchmarkOrHostsExecuted': False}))


if __name__ == '__main__':
    main()
