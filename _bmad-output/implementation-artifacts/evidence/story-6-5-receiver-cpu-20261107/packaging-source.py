from pathlib import Path
import collections, datetime, gzip, hashlib, json, math, os, re, shutil, stat

ROOT = Path('/home/administrator/projects/hexalith/projects')
PREP = Path('/tmp/story65-receiver-cpu-prep-20261107')
QUAL = Path('/tmp/story65-receiver-cpu-qualification-20261107')
WATCH_QUAL = Path('/tmp/story65-receiver-cpu-watcher-qualification-20261107')
LIVE = Path('/tmp/story65-receiver-cpu-owner-live-20261107')
PROFILE = Path('/tmp/story65-receiver-cpu-owner-profile-20261107')
PACKET = Path('/tmp/story65-receiver-cpu-evidence-20261107')
ROLES = ('projects', 'tenants', 'folders')
EXPECTED = {'projects': (121031, 551, 240), 'tenants': (100357, 129, 45), 'folders': (119824, 1560, 454)}
COPIED = []
EXCLUDED = []
SAFE_SUFFIX = {'.cs', '.c', '.cpp', '.csproj', '.config', '.json', '.py', '.md', '.patch', '.diff', '.log', '.xml'}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read(path):
    path = Path(path)
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK)
    try:
        s = os.fstat(fd)
        assert stat.S_ISREG(s.st_mode)
        with os.fdopen(fd, 'rb', closefd=False) as stream:
            raw = stream.read()
        assert len(raw) == s.st_size
        return raw
    finally:
        os.close(fd)


def strict(raw):
    def pairs(items):
        result = {}
        for k, v in items:
            assert k not in result
            result[k] = v
        return result
    return json.loads(raw, object_pairs_hook=pairs)


def save(relative, value):
    dest = PACKET / relative
    dest.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    assert not dest.exists()
    dest.write_text(json.dumps(value, indent=2) + '\n')


def copy(source, relative):
    source = Path(source)
    raw = read(source)
    assert source.suffix.lower() in SAFE_SUFFIX
    dest = PACKET / relative
    dest.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    assert not dest.exists()
    dest.write_bytes(raw)
    assert digest(read(dest)) == digest(raw)
    COPIED.append({'source': str(source), 'packetPath': str(relative), 'bytes': len(raw), 'sha256': digest(raw)})


def tree(source, relative):
    for path in sorted(Path(source).rglob('*')):
        if path.is_dir():
            continue
        if any(part in {'bin', 'obj', '__pycache__'} for part in path.relative_to(source).parts):
            continue
        assert not path.is_symlink()
        assert path.suffix.lower() in SAFE_SUFFIX, path.name
        copy(path, Path(relative) / path.relative_to(source))


def profile(role):
    base = PROFILE / role
    assert {p.name for p in base.iterdir()} == {'attached.json', 'collection-receipt.json', 'identity-checks.json', 'supervisor-receipt.json', 'cpu-summary.json', 'cpu-samples.jsonl'}
    for name in ('attached.json', 'collection-receipt.json', 'identity-checks.json', 'supervisor-receipt.json', 'cpu-summary.json'):
        copy(base / name, Path('profiles') / role / name)
    summary = strict(read(base / 'cpu-summary.json'))
    raw = read(base / 'cpu-samples.jsonl')
    assert digest(raw) == summary['sanitizedRowsSha256']
    samples, managed, group_count = EXPECTED[role]
    assert summary['samples'] == summary['sanitizedRows'] == samples
    assert summary['sampleTypeCounts'] == {'External': samples - managed, 'Managed': managed}
    assert all(summary[k] == 0 for k in ('missingStack', 'truncatedStacks', 'uniqueGroupCapOverflowSamples', 'lostOnConvert', 'traceEventsLost'))
    assert summary['sanitizedRowsCapped'] is False
    assert summary['diagnosticOnly'] is True and summary['featureAcceptance'] is False
    assert summary['exactCpuTimeClaimed'] is False and summary['externalIsNotProofOfBlocked'] is True
    groups = summary['stackGroups']
    assert len(groups) == group_count
    ids = {}
    for item in groups:
        assert set(item) == {'Id', 'SampleType', 'Count', 'Frames'}
        assert type(item['Id']) is int and type(item['Count']) is int and item['Count'] > 0
        assert item['SampleType'] in ('Managed', 'External')
        assert isinstance(item['Frames'], list) and all(type(x) is str for x in item['Frames'])
        assert item['Id'] not in ids
        ids[item['Id']] = item
    counted = collections.Counter()
    types = collections.Counter()
    for line in raw.splitlines():
        item = strict(line)
        assert set(item) == {'timeUtc', 'relativeMilliseconds', 'threadId', 'sampleType', 'stackId'}
        assert type(item['timeUtc']) is str and datetime.datetime.fromisoformat(item['timeUtc'].replace('Z', '+00:00'))
        assert type(item['relativeMilliseconds']) in (int, float) and math.isfinite(item['relativeMilliseconds'])
        assert type(item['threadId']) is int and type(item['stackId']) is int
        assert item['sampleType'] in ('Managed', 'External') and ids[item['stackId']]['SampleType'] == item['sampleType']
        counted[item['stackId']] += 1
        types[item['sampleType']] += 1
    assert sum(counted.values()) == samples and dict(types) == summary['sampleTypeCounts']
    assert all(counted[k] == v['Count'] for k, v in ids.items())
    collection = strict(read(base / 'collection-receipt.json'))
    assert collection['incomplete'] is False and collection['capReached'] is False
    assert collection['stopCompleted'] is True and collection['drainCompleted'] is True
    assert collection['targetGcRequested'] is False and collection['onlyOwnedSessionStopped'] is True
    assert collection['rawSha256'] == summary['rawSha256']
    compressed = gzip.compress(raw, mtime=0)
    assert gzip.decompress(compressed) == raw
    dest = PACKET / 'profiles' / role / 'cpu-samples.jsonl.gz'
    dest.write_bytes(compressed)
    save(Path('profiles') / role / 'sample-compression-and-validation.json', {
        'originalSanitizedRowsSha256': digest(raw), 'originalBytes': len(raw), 'originalRows': samples,
        'gzipSha256': digest(compressed), 'gzipBytes': len(compressed), 'decompressionByteIdentityVerified': True,
        'duplicateFieldsRejected': True, 'strictFiveFieldRows': True, 'allRowsReconcileToTypeAndStackGroups': True,
        'originalUncompressedDuplicateNotRetained': True, 'sanitizedSummaryUnmodified': True,
        'rawTraceSha256FromSanitizedReceipts': summary['rawSha256'], 'rawTraceOrEtlxCopied': False})
    return {k: summary[k] for k in ('samples', 'sampleTypeCounts', 'sampledThreads', 'unresolvedFrames', 'missingStack',
                                  'truncatedStacks', 'uniqueGroupCapOverflowSamples', 'sanitizedRowsCapped', 'lostOnConvert',
                                  'traceEventsLost', 'unpairedSuspendStarts', 'overwrittenSuspendStarts', 'unpairedRestarts', 'unpairedGcStarts')} | {'stackGroups': group_count}


def readiness():
    source = LIVE / 'browser-advisory-seed-readiness.jsonl'
    raw = read(source)
    fields = ('multiple', 'attempt', 'elapsedMilliseconds', 'httpStatus', 'result', 'incomplete', 'candidateCount',
              'positiveCandidatesVerified', 'actualPublicGateway', 'nonBootstrapCredential',
              'fabricatedProjectionWrite', 'runtimeAuthoritySupplied')
    rows = []
    for line in raw.splitlines():
        original = strict(line)
        row = {k: original[k] for k in fields}
        assert all(type(row[k]) is int for k in ('attempt', 'httpStatus', 'candidateCount'))
        assert type(row['elapsedMilliseconds']) in (int, float) and math.isfinite(row['elapsedMilliseconds'])
        assert row['result'] in (None, 'Empty', 'SingleCandidate', 'MultipleCandidates')
        assert all(type(row[k]) is bool for k in fields if k not in ('attempt', 'httpStatus', 'candidateCount', 'elapsedMilliseconds', 'result'))
        assert row['httpStatus'] == 200 and row['actualPublicGateway'] is True and row['nonBootstrapCredential'] is True
        assert row['fabricatedProjectionWrite'] is False and row['runtimeAuthoritySupplied'] is False
        rows.append(row)
    single = [r for r in rows if not r['multiple']]
    multiple = [r for r in rows if r['multiple']]
    assert len(single) == 2 and len(multiple) == 30
    assert [r['attempt'] for r in single if r['positiveCandidatesVerified'] and not r['incomplete']] == [2]
    assert not any(r['positiveCandidatesVerified'] for r in multiple)
    dest = PACKET / 'managed/artifacts/advisory-readiness-scalars.jsonl'
    dest.write_text(''.join(json.dumps(r, separators=(',', ':')) + '\n' for r in rows))
    save('managed/scalar-readiness-provenance.json', {'originalSha256': digest(raw), 'originalBytes': len(raw),
        'rows': len(rows), 'retainedFields': fields, 'fixtureIndexDetailsOwnerStatesGenerationObjectsExcluded': True,
        'elapsedIncludesSubsequentEvidenceReadsNotExactApiLatency': True, 'noJoinToCpuSamples': True})


def main():
    assert not PACKET.exists()
    PACKET.mkdir(mode=0o700)
    for source in (PREP / 'collector', PREP / 'collector-smoke', PREP / 'qualified-baseline', PREP / 'watcher'):
        tree(source, Path('preparation') / source.name)
    for path in sorted(PREP.iterdir()):
        if path.is_file():
            copy(path, Path('preparation') / path.name)
    tree(QUAL, 'collector-qualification')
    tree(WATCH_QUAL, 'watcher-qualification')
    q = strict(read(QUAL / 'collector-qualification-receipt.json'))
    assert q['qualified'] is True and q['guardSmokeChecks'] == 90
    assert digest(read(QUAL / 'collector-qualification-receipt.json')) == '5ea081f9954b4a10fe2ac9dba0d77c06e3d87eb304f2d35c1ca2fb5c06387e97'
    assert digest(read(WATCH_QUAL / 'command-receipt.json')) == '59ec79ca1b66ebfad3c0ce7a8008ba7f61a88bb08056b53a2fb5b0be3be3afcc'
    for name in ('coordinator-receipt.json', 'watcher-receipt.json'):
        copy(PROFILE / name, Path('profiles') / name)
    for role in ROLES:
        for kind in ('collect', 'analyze'):
            copy(PROFILE / f'{role}.{kind}-supervisor.json', Path('profiles') / f'{role}.{kind}-supervisor.json')
    counts = {role: profile(role) for role in ROLES}
    coordinator = strict(read(PROFILE / 'coordinator-receipt.json'))
    assert coordinator['incomplete'] is False and coordinator['postCollectionGuardPassed'] is True
    assert coordinator['allOwnedDiagnosticGroupsAbsent'] is True and coordinator['allCollectorsAndGroupsClosedBeforeAnyParser'] is True
    assert coordinator['actualAttachedCount'] == 3
    for suffix in ('-command.json', '-supervisor.json', '-console.log', '-independent-cleanup-source-audit.json',
                   '-cleanup-observation-1.json', '-cleanup-observation-history.json'):
        path = Path(str(LIVE) + suffix)
        copy(path, Path('managed') / path.name)
    selected = {'run.json', 'source-manifest.json', 'browser-seed.xml', 'browser-seed.log',
                'launcher-controls.log', 'seed-progress.log', 'writer-protocol-cutover.json', 'bootstrap-command-status.json'}
    for path in sorted(LIVE.iterdir()):
        if path.is_file() and (path.name in selected or re.fullmatch(r'build-[0-9]+\.log', path.name)):
            copy(path, Path('managed/artifacts') / path.name)
        elif path.is_file():
            raw = read(path)
            EXCLUDED.append({'originalFileName': path.name, 'bytes': len(raw), 'sha256': digest(raw),
                             'contentsRetained': False, 'reason': 'not in closed managed artifact allowlist'})
    readiness()
    supervisor = strict(read(Path(str(LIVE) + '-supervisor.json')))
    assert supervisor['managedExitCode'] == 1 and supervisor['watcherExitCode'] == 0
    save('actual-result-and-sample-counts.json', {'diagnosticOnly': True, 'featureAcceptance': False,
        'wrapperExitCode': 0, 'underlyingManagedLauncherExitCode': 1, 'watcherExitCode': 0,
        'singleAttempts': 2, 'singleVerifiedPositiveAttempts': [2], 'multipleAttempts': 30,
        'multipleVerifiedPositiveAttempts': [], 'browserCases': 0, 'actualSampleCounts': counts,
        'actualAttachedCount': 3, 'attachStartSpreadMilliseconds': coordinator['attachStartSpreadMilliseconds'],
        'nominalRequestedWindowOverlapMilliseconds': coordinator['nominalRequestedWindowOverlapMilliseconds'],
        'overlapIsMetadataEstimateNotExactSampleOrRequestCoverage': True,
        'ordinaryUniqueNativeQualificationStill1038': True, 'noNativeSuiteRequalificationByThisDiagnostic': True,
        'noCpuTimeRequestRoutePreviousSpanJoinOrCausalClaim': True})
    save('excluded-live-artifact-hashes.json', EXCLUDED)
    copy(__file__, 'packaging-source.py')
    save('byte-copy-provenance.json', COPIED)
    save('staging-status.json', {'stagedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'stage': 'profiles and qualification staged; parent scoped binary proof and final seal pending',
        'sealed': False, 'published': False, 'repoSpecSourceGitOrQualifiedCodeModified': False,
        'noAppBuildHostProfilingOrSignalOperations': True})
    print(json.dumps({'stagingPath': str(PACKET), 'copiedOriginalFiles': len(COPIED),
                      'profileRows': sum(v['samples'] for v in counts.values()), 'gzipFiles': 3, 'sealed': False}))


if __name__ == '__main__':
    main()
