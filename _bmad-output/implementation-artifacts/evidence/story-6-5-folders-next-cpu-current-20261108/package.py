"""File-only evidence staging: exact sources, explicit projections, deterministic gzip."""
from pathlib import Path
import gzip, hashlib, json, shutil

ROOT = Path('/home/administrator/projects/hexalith/projects')
PREP = Path('/tmp/story65-folders-next-cpu-prep-20261108')
PROFILE = Path('/tmp/story65-folders-next-cpu-owner-profile-20261108')
LIVE = Path('/tmp/story65-folders-next-cpu-owner-live-20261108')
AUDIT = Path('/tmp/story65-folders-next-cpu-audit-20261108')
OUT = Path('/tmp/story65-folders-next-cpu-publication-prep-20261108')
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
load = lambda p: json.loads(Path(p).read_bytes())
rows = []

def save(name, data):
    p = OUT / name
    p.parent.mkdir(parents=True, exist_ok=True)
    with p.open('xb') as f:
        f.write(data)
    return p

def record(source, destination, transformation, original):
    rows.append({'originalPath': str(source), 'originalSha256': hashlib.sha256(original).hexdigest(),
                 'originalBytes': len(original), 'destination': str(destination.relative_to(OUT)),
                 'storedSha256': sha(destination), 'storedBytes': destination.stat().st_size,
                 'transformation': transformation})

def copy(source, name, compressed=False):
    source = Path(source)
    assert source.is_file() and not source.is_symlink()
    data = source.read_bytes()
    p = save(name, gzip.compress(data, mtime=0) if compressed else data)
    record(source, p, 'gzip-mtime-zero; original bytes recoverable' if compressed else 'byte-exact', data)

def project(source, name, value):
    source = Path(source)
    p = save(name, (json.dumps(value, indent=2) + '\n').encode())
    record(source, p, 'explicit fixed-field projection; omitted fields not archived', source.read_bytes())

def pick(d, keys):
    return {k: d[k] for k in keys}

assert not OUT.exists()
audit = load(AUDIT / 'independent-profile-audit.json')
assert sha(AUDIT / 'independent-profile-audit.json') == '874f1ab891cc9e4d8ba009ab665993be7360d6187fd6bec1cf0db3dabd95a223'
assert audit['passed'] and audit['strictRows'] == 112727 and audit['stackGroupCount'] == 483
interpret = Path('/tmp/story65-folders-next-cpu-evidence-20261108/cpu-interpretation.json')
assert sha(interpret) == '87d6aba9776f11754eba303340ffd3b55dc72c23143d467231b0460d9351f96e'
pins = load(PREP / 'watcher/execution-pins.json')
assert pins['toolSha256'] == '2a55813a7c8cc8408a4a3a68a4297a778d1e8c416d3a93172e892ae95bf373c5'
assert len(pins['inputsSha256']) == 265
OUT.mkdir(mode=0o755)

for name in ['analyze_cpu.py', 'common.py', 'coordinate.py', 'own_live.py', 'ownership.py', 'run_one.py', 'watch_live.py']:
    copy(PREP / 'watcher' / name, 'source/watcher/' + name)
copy(PREP / 'watcher/source-controls.json', 'inputs/source-controls59.json')
copy(PREP / 'watcher/current-app147-prelaunch.json', 'inputs/app147-prelaunch.json')
copy(PREP / 'root-release/release.json', 'inputs/root-release.json')
plan = load(PREP / 'PLAN.json')
project(PREP / 'PLAN.json', 'inputs/plan-projection.json', {k: v for k, v in plan.items() if k != 'ordinaryManagedCommand'})
guard = load(PREP / 'collector-guard-map.json')
project(PREP / 'collector-guard-map.json', 'inputs/guard-map-projection.json', {k: v for k, v in guard.items() if k != 'cli'})
project(PREP / 'watcher/execution-pins.json', 'inputs/execution-pins-projection.json', {k: v for k, v in pins.items() if k != 'ordinaryManagedCommand'})
project(PREP / 'commands.json', 'inputs/command-plan-projection.json', pick(load(PREP / 'commands.json'), ['executionReleased', 'cwd', 'releaseRequiresRootReviewedFrozenPins', 'postClosureInterpretationPrerequisite']))

allowed = {'identity-checks.json', 'attached.json', 'collection-receipt.json', 'cpu-samples.jsonl', 'cpu-summary.json', 'supervisor-receipt.json'}
assert {p.name for p in (PROFILE / 'folders').iterdir()} == allowed
for name in sorted(allowed):
    copy(PROFILE / 'folders' / name, 'profile/folders/' + name + ('.gz' if name == 'cpu-samples.jsonl' else ''), name == 'cpu-samples.jsonl')
copy(interpret, 'analysis/cpu-interpretation.json')
for name in ['audit.py', 'independent-profile-audit.json', 'manifest-membership-correction.json', 'audit-before-manifest-membership-correction.py']:
    copy(AUDIT / name, 'analysis/' + name)

co = load(PROFILE / 'coordinator-receipt.json')
co_view = pick(co, ['diagnosticOnly', 'featureAcceptance', 'collectionWindowSecondsPerSession', 'singleFixedFoldersTarget', 'noRequestOrLogicalRootJoin', 'incomplete', 'targetSignalsSent', 'allCollectorsAndGroupsClosedBeforeAnyParser', 'postCollectionGuardPassed', 'allOwnedDiagnosticGroupsAbsent', 'actualAttachedCount', 'attachStartSpreadMilliseconds', 'nominalRequestedWindowOverlapMilliseconds', 'overlapIsMetadataEstimateNotExactSampleOrRequestCoverage'])
co_view['stages'] = {k: [pick(x, ['role', 'processGroupAbsent', 'exitCode', 'remainingDiagnosticGroupMembers']) for x in co[k]] for k in ['collectorProcesses', 'parserProcesses']}
co_view['rawRemoval'] = pick(co['privateRemoval']['folders'], ['removed', 'allNonAllowlistedPrivateFilesAbsent', 'allSubdirectoriesRemoved', 'symlinkTargetsFollowedOrHashed', 'ownerMode'])
project(PROFILE / 'coordinator-receipt.json', 'closed/coordinator-projection.json', co_view)
w = load(PROFILE / 'watcher-receipt.json')
w_view = pick(w, ['diagnosticOnly', 'featureAcceptance', 'phase', 'targetSignalsSent', 'endOnlyRunJsonNotRequired', 'anyCollectorLaunched', 'profilerAttached', 'graphAttempts', 'onlyFixedFoldersRole', 'fullSourceSnapshotCachedOnceUntilTrigger', 'profileIncomplete', 'lateIndividualGuardRefusalIsIncomplete', 'privateIdentityFilesAbsent'])
w_view['generationProof'] = pick(w['currentOrdinaryGenerationProofs']['folders'], ['sourceCount', 'source59Match', 'fullManifestCount', 'fullManifestSha256', 'fullManifestAllMatch', 'role', 'noHistoricalNativeByteEquivalenceClaimed'])
project(PROFILE / 'watcher-receipt.json', 'closed/watcher-projection.json', w_view)
sup_path = Path(str(LIVE) + '-supervisor.json')
sup = load(sup_path)
project(sup_path, 'closed/managed-supervisor-projection.json', pick(sup, ['preparationOnly', 'noPathShimImportReplacementSourceHookNoBuildOrLoggerOverride', 'runtimeCpuOverrideAbsent', 'oneFixedTarget', 'noTargetSignalsOrCutoff', 'phase', 'targetSignalsSent', 'managedExitCode', 'managedIdentityAbsent', 'watcherExitCode', 'watcherGroupRemaining', 'watcherGroupAbsent', 'cleanup', 'source59After']))
cleanup_path = Path(str(LIVE) + '-independent-cleanup-source-audit.json')
cleanup = load(cleanup_path)
cleanup_view = pick(cleanup, ['ordinaryAcceptanceOnly', 'profilerEnabled', 'runResult', 'matrixCompleted', 'singleAttempts', 'singleVerifiedPositives', 'multipleAttempts', 'multipleVerifiedPositives', 'namespaceContainersAbsent', 'privateRuntimeDirectoryAbsent', 'allOriginalCleanupAbsenceVerified', 'preexistingResourcesPreserved', 'ownedSidecarsAbsent', 'privateMaterialRemoved', 'fullLiveManifestSourceCount', 'fullLiveManifestHashMismatches', 'allIndependentRecordedProcessAndContainerAbsences', 'actualBrowserArtifactsRequireIndependentXmlAndPersistencePrivacyReview', 'profilingAttempted', 'actualAttachMetadataObserved', 'diagnosticOnly', 'featureAcceptance', 'profilingDoesNotChangeOriginalRunResult'])
cleanup_view.update({'recordedIdentityCount': len(cleanup['identities']), 'recordedIdentitiesAbsent': all(x['recordedIdentityAbsent'] for x in cleanup['identities']), 'containerCount': len(cleanup['containers']), 'containersAbsent': all(x['absenceVerified'] for x in cleanup['containers']), 'nativeGroupsAbsent': all(x['descendantsAbsent'] for x in cleanup['nativeTestProcessCleanup'])})
project(cleanup_path, 'closed/independent-cleanup-projection.json', cleanup_view)

qualification = Path('/tmp/story65-receiver-cpu-qualification-20261107/collector-qualification-receipt.json')
q = load(qualification)
assert sha(qualification) == pins['collectorQualificationSha256']
project(qualification, 'references/qualified-collector-projection.json', pick(q, ['qualificationScope', 'qualified', 'featureAcceptance', 'collectorProgramSha256', 'collectorProjectSha256', 'smokeProgramSha256', 'collectorDllPath', 'collectorDllSha256', 'guardSmokeChecks', 'fixedAllowedRoles', 'finiteGuardAndMetadataNameSmokePassed', 'fullCollectAndAnalyzeSourceReuseQualified', 'metadataNameSourceExact', 'cachedDependencyNamesAndVersionsUnchanged', 'dependencyUpdates', 'oldToolSourceAndBinaryUnchanged', 'acceptedRetryCollectorBuildWarnings', 'acceptedRetryCollectorBuildErrors', 'acceptedRetrySmokeBuildWarnings', 'acceptedRetrySmokeBuildErrors', 'targetAttachCalls', 'applicationProcessesStarted', 'traceParserIngestionExecuted', 'native1038Requalified', 'actualLiveProcOrDcpOwnershipEstablishedBySmoke']))
apps = load(PREP / 'watcher/current-app147-prelaunch.json')['currentFilesSha256']
deltas = [{'path': p, 'prelaunchSha256': apps[p], 'postclosureSha256': sha(p)} for p in audit['freshApp147GenerationDifferences']]
assert len(deltas) == 6
save('inputs/app147-generation-deltas.json', (json.dumps({'scope': 'Six permitted fresh standard-build generations; no historical1048 byte-equality claim.', 'differences': deltas}, indent=2) + '\n').encode())
domain = Path('/tmp/story65-domain-receiver-phase-publication-prep-20261108/SHA256SUMS')
assert sha(domain) == 'b64754ef79154a1cf15d2818c32e0647db8edfaa197730de8c3969420c1c616c'
save('references/prior-domain-packet.json', (json.dumps({'manifestPath': str(domain), 'manifestSha256': sha(domain), 'publishedPacketFilesIncludingManifest': 717, 'assetsRecopied': False, 'noSampleOrRequestJoin': True}, indent=2) + '\n').encode())
save('provenance.json', (json.dumps({'diagnosticOnly': True, 'featureAcceptance': False, 'copiesAndProjections': rows, 'notArchived': ['raw run and seed output', 'private identity input', 'argv/environment/configuration contents', 'proc maps', 'URLs/authentication/request/response payloads', 'native stdout', 'binaries/binobj', 'nettrace/ETLX contents'], 'packagingSourcePath': str(Path(__file__)), 'packagingSourceSha256': sha(__file__)}, indent=2) + '\n').encode())
copy(Path(__file__), 'package.py')
print(json.dumps({'stagedFilesBeforeReadmePublisherAndManifest': len(list(OUT.rglob('*'))) - sum(p.is_dir() for p in OUT.rglob('*')), 'destination': str(OUT), 'byteExactCopiesOrExplicitProjections': len(rows)}))
