from pathlib import Path
import collections, datetime, hashlib, json, math, os, re, stat, xml.etree.ElementTree as ET

ROOT = Path('/home/administrator/projects/hexalith/projects')
LIVE = Path('/tmp/story65-memo-rw-acceptance-live-20261107')
PACKET = Path('/tmp/story65-memo-rw-ordinary-evidence-20261107')
PREP = Path('/tmp/story65-memo-rw-acceptance-prep-20261107')
COPIED = []


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
        out = {}
        for key, value in items:
            assert key not in out
            out[key] = value
        return out
    return json.loads(raw, object_pairs_hook=pairs)


def save(relative, value):
    dest = PACKET / relative
    dest.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    assert not dest.exists()
    dest.write_text(json.dumps(value, indent=2) + '\n')


def copy(path, relative):
    path = Path(path)
    raw = read(path)
    assert path.suffix.lower() in {'.json', '.py', '.xml'}
    dest = PACKET / relative
    dest.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    assert not dest.exists()
    dest.write_bytes(raw)
    COPIED.append({'source': str(path), 'packetPath': str(relative), 'sha256': sha(path), 'bytes': len(raw)})


def main():
    assert not PACKET.exists()
    PACKET.mkdir(mode=0o700)
    for name in ('supervise.py', 'audit.py', 'commands.json', 'preparation-receipt.json'):
        copy(PREP / name, Path('preparation') / name)
    for suffix in ('-command.json', '-preflight-source.json', '-postflight-source.json', '-independent-cleanup-source-audit.json',
                   '-ordinary-binary-source-audit-with-provenance-limits.json'):
        file = Path(str(LIVE) + suffix)
        copy(file, Path('ordinary-run') / file.name)
    for name in ('run.json', 'source-manifest.json', 'browser-seed.xml'):
        copy(LIVE / name, Path('ordinary-run/artifacts') / name)
    for source in ('/tmp/story65-memo-rw-acceptance-binary-guard-failure-20261107.json',
                   '/tmp/story65-memo-rw-acceptance-post-binary-audit-20261107.py',
                   '/tmp/story65-memo-rw-acceptance-post-binary-audit-before-provenance-limit-20261107.py',
                   '/tmp/story65-memo-rw-acceptance-post-binary-audit-with-provenance-limits-20261107.py'):
        copy(source, Path('audit-history') / Path(source).name)
    for name in ('parent-native-qualification.json', 'source-controls.json', 'parent-guards.json'):
        copy(Path('/tmp/story65-memo-rw-native-20261107') / name, Path('qualification-reference') / name)
    copy('/tmp/story65-memo-rw-acceptance-release-20261107.json', 'preparation/root-execution-release.json')
    fields = ('multiple', 'attempt', 'elapsedMilliseconds', 'httpStatus', 'result', 'incomplete', 'candidateCount',
              'positiveCandidatesVerified', 'actualPublicGateway', 'nonBootstrapCredential',
              'fabricatedProjectionWrite', 'runtimeAuthoritySupplied')
    original = read(LIVE / 'browser-advisory-seed-readiness.jsonl')
    rows = []
    for line in original.splitlines():
        parsed = strict(line)
        row = {key: parsed[key] for key in fields}
        assert all(type(row[key]) is int for key in ('attempt', 'httpStatus', 'candidateCount'))
        assert type(row['elapsedMilliseconds']) in (int, float) and math.isfinite(row['elapsedMilliseconds'])
        assert row['result'] in (None, 'Empty', 'SingleCandidate', 'MultipleCandidates')
        assert all(type(row[key]) is bool for key in fields if key not in ('attempt', 'httpStatus', 'candidateCount', 'elapsedMilliseconds', 'result'))
        assert row['actualPublicGateway'] and row['nonBootstrapCredential'] and not row['fabricatedProjectionWrite'] and not row['runtimeAuthoritySupplied']
        rows.append(row)
    single = [row for row in rows if not row['multiple']]
    multi = [row for row in rows if row['multiple']]
    assert len(single) == 1 and single[0]['positiveCandidatesVerified'] is True
    assert len(multi) == 30 and all(row['httpStatus'] == 200 and row['result'] is None and row['incomplete'] is True
                                  and row['candidateCount'] == 0 and row['positiveCandidatesVerified'] is False for row in multi)
    dest = PACKET / 'ordinary-run/artifacts/advisory-readiness-scalars.jsonl'
    dest.write_text(''.join(json.dumps(row, separators=(',', ':')) + '\n' for row in rows))
    save('ordinary-run/readiness-provenance.json', {'originalSha256': hashlib.sha256(original).hexdigest(),
        'originalBytes': len(original), 'rows': len(rows), 'retainedClosedFields': fields,
        'protectedFixtureObjectsAndCorrelationsExcluded': True,
        'outerElapsedIncludesLaterEvidenceReadsAndDoesNotLocalizeDeadlineStage': True})
    run = strict(read(LIVE / 'run.json'))
    command = strict(read(Path(str(LIVE) + '-command.json')))
    cleanup = strict(read(Path(str(LIVE) + '-independent-cleanup-source-audit.json')))
    assert command['exitCode'] == 1 and run['result'] == 'failed'
    assert cleanup['allIndependentRecordedProcessAndContainerAbsences'] is True
    assert cleanup['fullLiveManifestSourceCount'] == 17341 and cleanup['fullLiveManifestHashMismatches'] == 0
    assert all(cleanup[key] is True for key in ('namespaceContainersAbsent', 'privateRuntimeDirectoryAbsent',
        'allOriginalCleanupAbsenceVerified', 'preexistingResourcesPreserved', 'ownedSidecarsAbsent', 'privateMaterialRemoved'))
    group_ids = [row['groupId'] for row in run['testProcessCleanup']]
    group_members = {str(group): [] for group in group_ids}
    unreadable = 0
    for item in Path('/proc').iterdir():
        if not item.name.isdecimal():
            continue
        try:
            fields_stat = (item / 'stat').read_text().rsplit(')', 1)[1].split()
            group = int(fields_stat[2])
            if group in group_ids and fields_stat[0] != 'Z':
                group_members[str(group)].append({'pid': int(item.name), 'startTicks': fields_stat[19]})
        except (FileNotFoundError, ProcessLookupError):
            pass
        except PermissionError:
            unreadable += 1
    assert all(not members for members in group_members.values())
    save('independent-native-group-absence.json', {'checkedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'recordedGroups': group_members, 'allGroupsAbsentFreshProcStatCensus': True,
        'zombieProcessesTreatedAsAbsent': True, 'unreadableProcStatEntries': unreadable,
        'argvEnvironmentAndPayloadNotRead': True, 'noSignalsSent': True})
    binary = strict(read(Path(str(LIVE) + '-ordinary-binary-source-audit-with-provenance-limits.json')))
    issues = binary['unresolvedGenerationProvenance']
    sdk_issues = [row for row in issues if row['assembly'] == 'Hexalith.EventStore.ServiceDefaults']
    assert len(sdk_issues) == 14 and all(row['actualSha256'] == row['currentProducerSha256'] for row in sdk_issues)
    other_issues = [row for row in issues if row['assembly'] != 'Hexalith.EventStore.ServiceDefaults']
    assert len(other_issues) == 10
    seed = ET.parse(LIVE / 'browser-seed.xml').getroot()
    cases = seed.findall('.//test')
    assert len(cases) == 1 and cases[0].get('result') == 'Fail'
    save('actual-result-and-proof-limits.json', {'ordinaryRunOnly': True, 'profilerEnabled': False,
        'wrapperExitCode': 0, 'underlyingManagedExitCode': 1, 'result': 'failed',
        'singleAttempts': 1, 'singleVerifiedPositives': 1, 'multipleAttempts': 30, 'multipleVerifiedPositives': 0,
        'allMultipleRowsHttp200NullIncompleteZeroCandidatesUnverified': True,
        'browserSeedCases': 1, 'browserSeedPassed': 0, 'browserSeedFailed': 1,
        'browserXmlPresent': False, 'browserPrivacyTelemetryPresent': False, 'browserProofPresent': False,
        'currentOidcBrowserPersistencePrivacyA11yJourneyNotExercised': True,
        'historicalManualAccessibilityNotRequalifiedOrInvalidatedByThisFailedSeed': True,
        'identitiesAbsent': 21, 'ownedContainersAbsent': 5, 'originalCleanupReceipts': 6,
        'fullManifestInputs': 17341, 'fullManifestMismatches': 0, 'sourceControlsMatched': 56,
        'sdk137MarkerClean': True, 'sdk8CurrentDllPdb': True, 'native147MarkerClean': True,
        'qualifiedProductionSdkSha256': binary['sdkCurrentProducer']['full'],
        'qualifiedProductionPdbSha256': binary['sdkCurrentProducer']['pdb'],
        'actualManagedSourceGraphProjects': len(binary['actualManagedSourceGraphProjects']),
        'additional14SdkRowsByteEqualCurrentProducerWithoutBuildScopeAttribution': True,
        'unresolvedServerGatewayGenerationPaths': other_issues,
        'fullDependencyGenerationCoherenceNotClaimed': True,
        'noBinaryWhitelistCorrectiveBuildRetryProbeOrDeadlineCauseInference': True,
        'priorNativeUnique1048AndSeparateTransferChecksNotInflated': True,
        'revisions': run['revisions']})
    excluded = []
    for file in sorted(LIVE.iterdir()):
        if file.is_file() and file.name not in {'run.json', 'source-manifest.json', 'browser-seed.xml'}:
            excluded.append({'fileName': file.name, 'sha256': sha(file), 'bytes': file.stat().st_size,
                             'contentsCopied': False, 'closedReadinessScalarsRetainedInstead': file.name == 'browser-advisory-seed-readiness.jsonl'})
    console = Path(str(LIVE) + '-console.log')
    excluded.append({'fileName': console.name, 'sha256': sha(console), 'bytes': console.stat().st_size, 'contentsCopied': False})
    save('excluded-original-artifacts-hashes.json', excluded)
    copy(__file__, 'packaging-source.py')
    save('byte-copy-provenance.json', COPIED)
    print(json.dumps({'packetStaged': str(PACKET), 'copiedOriginalFiles': len(COPIED), 'readinessRows': 31,
        'failedOrdinaryRunPreserved': True, 'sealed': False, 'sourceOrAppOperations': False}))


if __name__ == '__main__':
    main()
