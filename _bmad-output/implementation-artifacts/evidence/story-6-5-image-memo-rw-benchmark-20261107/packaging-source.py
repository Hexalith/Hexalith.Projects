from pathlib import Path
import collections, datetime, hashlib, json, math, os, re, shutil, stat

PREP = Path('/tmp/story65-image-memo-rw-prep-20261107')
OLD = Path('/tmp/story65-image-memo-rw-prep-history-20261107/e69bc812')
EXEC = Path('/tmp/story65-image-memo-rw-execution-20261107')
FAILED = EXEC / 'attempt-1/failed-preparation-and-compiled-generation'
RESULTS = Path('/tmp/story65-image-memo-rw-results-retry-20261107')
DIAG = Path('/tmp/story65-image-memo-rw-inventory-diagnosis-20261107')
PACKET = Path('/tmp/story65-image-memo-rw-evidence-20261107')
COPIED = []
SUFFIXES = {'.cs', '.csproj', '.md', '.json', '.jsonl', '.py', '.diff', '.txt', '.config', '.patch', '.log'}


def read(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK | os.O_CLOEXEC)
    try:
        assert stat.S_ISREG(os.fstat(fd).st_mode)
        with os.fdopen(fd, 'rb', closefd=False) as file:
            return file.read()
    finally:
        os.close(fd)


def sha(path):
    return hashlib.sha256(read(path)).hexdigest()


def strict(raw):
    def pairs(items):
        result = {}
        for key, value in items:
            assert key not in result
            result[key] = value
        return result
    return json.loads(raw, object_pairs_hook=pairs)


def save(relative, value):
    path = PACKET / relative
    path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    assert not path.exists()
    path.write_text(json.dumps(value, indent=2) + '\n')


def copy(source, relative):
    source = Path(source)
    assert source.suffix.lower() in SUFFIXES
    raw = read(source)
    dest = PACKET / relative
    dest.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    assert not dest.exists()
    dest.write_bytes(raw)
    assert sha(dest) == sha(source)
    COPIED.append({'source': str(source), 'packetPath': str(relative), 'bytes': len(raw), 'sha256': sha(source)})


def tree(source, relative, skip=()):
    for path in sorted(source.rglob('*')):
        parts = path.relative_to(source).parts
        if path.is_dir() or any(part in {'bin', 'obj', '__pycache__', *skip} for part in parts):
            continue
        assert not path.is_symlink()
        copy(path, Path(relative) / path.relative_to(source))


def generation(source, name, expected, count):
    receipt = strict(read(source / 'preparation-receipt.json'))
    assert sha(source / 'preparation-receipt.json') == expected and len(receipt['filesSha256']) == count
    for relative, digest in receipt['filesSha256'].items():
        assert sha(source / relative) == digest
        copy(source / relative, Path('source-generations') / name / relative)
    copy(source / 'preparation-receipt.json', Path('source-generations') / name / 'preparation-receipt.json')
    return receipt


def validate_samples():
    raw = read(RESULTS / 'benchmark-samples.jsonl')
    fields = {'Variant', 'Path', 'Phase', 'Round', 'Order', 'Readers', 'IterationsPerReader', 'CompleteObservations',
        'ImagesPerObservation', 'StopwatchFrequency', 'ElapsedTicks', 'ProcessCpuTicks', 'ProcessAllocatedBytes',
        'GcCollectionDeltas', 'MemoCountBefore', 'MemoCountAfter', 'ChargedBytesBefore', 'ChargedBytesAfter',
        'EntireCohortFitsOriginalBounds', 'ReportedMemoHits', 'FreshResultsWithoutReportedReuse',
        'NonHitsDistinguishedFromRejections', 'Workers'}
    worker_fields = {'Worker', 'ElapsedTicks', 'ThreadAllocatedBytes', 'FailureType', 'Observations'}
    observation_fields = {'Worker', 'Iteration', 'ElapsedTicks', 'ThreadAllocatedBytes', 'ReportedMemoHits', 'FreshResultsWithoutReportedReuse'}
    rows = [strict(line) for line in raw.splitlines()]
    assert len(rows) == 324
    unique = set()
    observations = 0
    calls = 0
    for row in rows:
        assert set(row) == fields
        assert row['Variant'] in ('oracle', 'control', 'candidate') and row['Path'] in ('domain', 'technical')
        assert row['Phase'] in ('memo-cold', 'warmup', 'warm')
        assert row['Readers'] in (1, 2, 4) and row['ImagesPerObservation'] == 66
        assert row['IterationsPerReader'] == {'memo-cold': 1, 'warmup': 3, 'warm': 12}[row['Phase']]
        assert row['CompleteObservations'] == row['Readers'] * row['IterationsPerReader']
        assert row['EntireCohortFitsOriginalBounds'] is True and row['NonHitsDistinguishedFromRejections'] is True
        assert row['MemoCountAfter'] == 66 and row['ChargedBytesAfter'] == 27689784
        assert row['MemoCountBefore'] == (0 if row['Phase'] == 'memo-cold' else 66)
        assert row['ChargedBytesBefore'] == (0 if row['Phase'] == 'memo-cold' else 27689784)
        integer_keys = fields - {'Variant', 'Path', 'Phase', 'Workers', 'GcCollectionDeltas',
                                'EntireCohortFitsOriginalBounds', 'NonHitsDistinguishedFromRejections'}
        assert all(type(row[key]) is int and row[key] >= 0 for key in integer_keys)
        assert len(row['GcCollectionDeltas']) == 3 and all(type(n) is int and n >= 0 for n in row['GcCollectionDeltas'])
        key = (row['Variant'], row['Path'], row['Phase'], row['Round'], row['Readers'])
        assert key not in unique
        unique.add(key)
        assert len(row['Workers']) == row['Readers']
        hits = 0
        for index, worker in enumerate(row['Workers']):
            assert set(worker) == worker_fields and worker['Worker'] == index and worker['FailureType'] is None
            assert len(worker['Observations']) == row['IterationsPerReader']
            assert type(worker['ElapsedTicks']) is int and worker['ElapsedTicks'] >= 0
            assert type(worker['ThreadAllocatedBytes']) is int and worker['ThreadAllocatedBytes'] >= 0
            for iteration, observation in enumerate(worker['Observations']):
                assert set(observation) == observation_fields
                assert all(type(value) is int and value >= 0 for value in observation.values())
                assert observation['Worker'] == index and observation['Iteration'] == iteration
                assert observation['ReportedMemoHits'] + observation['FreshResultsWithoutReportedReuse'] == 66
                hits += observation['ReportedMemoHits']
                observations += 1
                calls += 66
        expected_hits = (row['CompleteObservations'] - (1 if row['Phase'] == 'memo-cold' else 0)) * 66
        assert hits == row['ReportedMemoHits'] == expected_hits
        assert hits + row['FreshResultsWithoutReportedReuse'] == row['CompleteObservations'] * 66
    assert observations == 4032 and calls == 266112
    return {'strictDuplicateFreeNestedJson': True, 'rawSha256': sha(RESULTS / 'benchmark-samples.jsonl'),
        'rawBytes': len(raw), 'batchRows': len(rows), 'completeObservations': observations,
        'selectedImageCalls': calls, 'imageCohort': 66, 'allSuccessfulWorkerAndExactHitOccupancyChecksPassed': True,
        'candidateBenefitOrProductionApprovalClaimed': False}


def compiled_hashes():
    result = []
    for label, base in [('failed-attempt-1', FAILED), ('successful-attempt-2', PREP)]:
        for path in sorted((base / 'harness').rglob('*')):
            if path.is_file() and any(part in {'bin', 'obj'} for part in path.relative_to(base / 'harness').parts):
                result.append({'generation': label, 'relativePath': str(path.relative_to(base)),
                               'bytes': path.stat().st_size, 'sha256': sha(path), 'contentsCopied': False})
    return result


def main():
    assert not PACKET.exists()
    PACKET.mkdir(mode=0o700)
    old = generation(OLD, 'e69-failed-count-assumption', 'e69bc812c1e2b290dea54c8225a52d12211977b3c7a3805cf4708ca3b5addbbb', 59)
    new = generation(PREP, 'ff43-current-66-cohort', 'ff43de5c3ecd8faa1583faa6f384f298fa397d08ed02d754fd84ec091eca24a2', 69)
    for name in ('attempt-1', 'attempt-2'):
        tree(EXEC / name, Path('execution') / name,
             skip={'prepared-source-snapshot', 'failed-preparation-and-compiled-generation'})
    tree(DIAG, 'metadata-count-diagnosis')
    tree(Path('/tmp/story65-image-memo-rw-results-20261107'), 'failed-entry-results')
    tree(RESULTS, 'actual-results')
    validation = validate_samples()
    save('raw-sample-validation.json', validation)
    parity = strict(read(RESULTS / 'parity.json'))
    assert parity == {'Checks': 620, 'Passed': 620, 'AllPassed': True}
    result = strict(read(RESULTS / 'result.json'))
    assert result['Completed'] is True and result['AllPassed'] is True and result['FeatureAcceptance'] is False
    for attempt, receipt in [('attempt-1', old), ('attempt-2', new)]:
        snapshot = EXEC / attempt / 'prepared-source-snapshot'
        assert all(sha(snapshot / name) == digest for name, digest in receipt['filesSha256'].items())
    after = strict(read(EXEC / 'attempt-2/inputs-after.json'))
    before = strict(read(EXEC / 'attempt-2/inputs-before.json'))
    assert after == before
    save('compiled-generations-hashes-only.json', compiled_hashes())
    save('snapshot-and-count-boundaries.json', {
        'bothExecutedPreparedSourceSnapshotsMatchSavedGenerations': True,
        'duplicateSourceSnapshotsNotRepeated': True,
        'old59AndNew69SavedWithExactOriginalReceipts': True,
        'all658SuccessfulAttemptInputsIdenticalBeforeAfter': True,
        'parityChecks': 620, 'checksAreNotAdditionalNativeCases': True,
        'ordinaryUniqueNativeQualificationUnchanged': 1038,
        'standaloneComputationBenchmarkNotHostCaptureDeadlineOrFeatureAcceptance': True,
        'metadataCountCorrectionPreservesFullSelectedCohortAndOriginalBounds': True,
        'independentFairnessAuditAndRootDecisionStillPending': True})
    copy(__file__, 'packaging-source.py')
    save('byte-copy-provenance.json', COPIED)
    save('staging-status.json', {'stagedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'sealed': False, 'published': False, 'waitForIndependentFairnessAuditAndRootDecision': True,
        'noFurtherAppBuildLoadBenchmarkHostSourceSpecOrGitOperations': True})
    print(json.dumps({'staging': str(PACKET), 'copiedOriginalFiles': len(COPIED),
        'validatedRawBatches': 324, 'observations': 4032, 'selectedImageCalls': 266112,
        'parityChecks': 620, 'sealed': False}))


if __name__ == '__main__':
    main()
