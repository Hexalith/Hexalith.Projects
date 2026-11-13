"""File-only staging of fixed metadata projections and sanitized CPU artifacts."""
from pathlib import Path
import gzip
import hashlib
import json
import xml.etree.ElementTree as ET

PREP = Path('/tmp/story65-canonical-current-gateway-cpu-prep-20261108')
PROFILE = Path('/tmp/story65-canonical-current-gateway-cpu-owner-profile-20261108')
LIVE = Path('/tmp/story65-canonical-current-gateway-cpu-owner-live-20261108')
AUDIT = Path('/tmp/story65-canonical-current-gateway-cpu-audit-20261108')
OUT = Path('/tmp/story65-canonical-current-gateway-cpu-root-publication-prep-20261108')
INTERPRET = Path('/tmp/story65-canonical-current-gateway-cpu-evidence-20261108/cpu-interpretation.json')
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
load = lambda p: json.loads(Path(p).read_bytes())
rows = []


def save(name, data):
    path = OUT / name
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as stream:
        stream.write(data)
    return path


def encode(value):
    return (json.dumps(value, indent=2) + '\n').encode()


def record(source, destination, transformation, original):
    rows.append({'originalPath': str(source), 'originalSha256': hashlib.sha256(original).hexdigest(),
                 'originalBytes': len(original), 'destination': str(destination.relative_to(OUT)),
                 'storedSha256': sha(destination), 'storedBytes': destination.stat().st_size,
                 'transformation': transformation})


def copy(source, name, compressed=False):
    source = Path(source)
    assert source.is_file() and not source.is_symlink()
    data = source.read_bytes()
    path = save(name, gzip.compress(data, mtime=0) if compressed else data)
    record(source, path, 'gzip-mtime-zero; original bytes recoverable' if compressed else 'byte-exact', data)


def project(source, name, value):
    source = Path(source)
    path = save(name, encode(value))
    record(source, path, 'explicit fixed-field projection; omitted fields not archived', source.read_bytes())


def pick(value, keys):
    return {key: value[key] for key in keys}


assert not OUT.exists()
assert sha(AUDIT / 'independent-profile-audit.json') == 'ec28bf63007a318803aa750a013fea0d88804a1b4c6525ae67fcf1d24b854089'
assert sha(AUDIT / 'audit.py') == 'e537dd78a06271909222fbb6cc5712bdc9288a58f5986095c7085a8696a860ff'
audit = load(AUDIT / 'independent-profile-audit.json')
assert audit['passed'] and audit['strictRows'] == 128231 and audit['stackGroupCount'] == 1211
assert audit['allDiagnosticPgidOrSidFreshlyAbsentIncludingZombies']
pins = load(PREP / 'watcher/execution-pins.json')
assert len(pins['inputsSha256']) == 279
assert pins['toolSha256'] == '7bbaef3570c82e5797d633c2f24e5333b21f0098ffbf7ac6bac448e70911ef9b'
OUT.mkdir(mode=0o755)

for name in ['analyze_cpu.py', 'common.py', 'coordinate.py', 'own_live.py', 'ownership.py', 'run_one.py', 'watch_live.py']:
    copy(PREP / 'watcher' / name, 'source/watcher/' + name)
copy(PREP / 'adaptation.diff', 'source/adaptation.diff')
copy(PREP / 'members-guard-smoke.json', 'source/members-guard-smoke.json')
copy(PREP / 'watcher/source-controls.json', 'inputs/source-controls65.json')
copy(PREP / 'watcher/current-app147-prelaunch.json', 'inputs/app147-prelaunch.json')
release = load(PREP / 'root-release/release.json')
project(PREP / 'root-release/release.json', 'inputs/root-release-projection.json', {k: v for k, v in release.items() if k != 'ordinaryCommand'})
plan = load(PREP / 'PLAN.json')
project(PREP / 'PLAN.json', 'inputs/plan-projection.json', {k: v for k, v in plan.items() if k != 'ordinaryManagedCommand'})
project(PREP / 'collector-guard-map.json', 'inputs/guard-map-projection.json', load(PREP / 'collector-guard-map.json'))
project(PREP / 'watcher/execution-pins.json', 'inputs/execution-pins-projection.json', {k: v for k, v in pins.items() if k != 'ordinaryManagedCommand'})
project(PREP / 'commands.json', 'inputs/command-plan-projection.json', pick(load(PREP / 'commands.json'), ['executionReleased', 'cwd', 'releaseRequiresRootReviewedFrozenPins', 'postClosureInterpretationPrerequisite', 'noBuildOrNewTooling']))

allowed = {'identity-checks.json', 'attached.json', 'collection-receipt.json', 'cpu-samples.jsonl', 'cpu-summary.json', 'supervisor-receipt.json'}
assert {p.name for p in (PROFILE / 'gateway').iterdir()} == allowed
for name in sorted(allowed):
    copy(PROFILE / 'gateway' / name, 'profile/gateway/' + name + ('.gz' if name == 'cpu-samples.jsonl' else ''), name == 'cpu-samples.jsonl')
copy(INTERPRET, 'analysis/cpu-interpretation.json')
copy(AUDIT / 'audit.py', 'analysis/audit.py')
copy(AUDIT / 'independent-profile-audit.json', 'analysis/independent-profile-audit.json')

co = load(PROFILE / 'coordinator-receipt.json')
co_view = pick(co, ['diagnosticOnly', 'featureAcceptance', 'collectionWindowSecondsPerSession', 'singleFixedGatewayTarget', 'noRequestOrLogicalRootJoin', 'incomplete', 'targetSignalsSent', 'allCollectorsAndGroupsClosedBeforeAnyParser', 'postCollectionGuardPassed', 'allOwnedDiagnosticGroupsAbsent', 'actualAttachedCount', 'attachStartSpreadMilliseconds', 'nominalRequestedWindowOverlapMilliseconds', 'overlapIsMetadataEstimateNotExactSampleOrRequestCoverage'])
co_view['stages'] = {k: [pick(x, ['role', 'processGroupAbsent', 'exitCode', 'remainingDiagnosticGroupMembers']) for x in co[k]] for k in ['collectorProcesses', 'parserProcesses']}
co_view['rawRemoval'] = pick(co['privateRemoval']['gateway'], ['removed', 'allNonAllowlistedPrivateFilesAbsent', 'allSubdirectoriesRemoved', 'symlinkTargetsFollowedOrHashed', 'ownerMode'])
project(PROFILE / 'coordinator-receipt.json', 'closed/coordinator-projection.json', co_view)
watcher = load(PROFILE / 'watcher-receipt.json')
w_view = pick(watcher, ['diagnosticOnly', 'featureAcceptance', 'phase', 'targetSignalsSent', 'endOnlyRunJsonNotRequired', 'anyCollectorLaunched', 'profilerAttached', 'graphAttempts', 'onlyFixedGatewayRole', 'fullSourceSnapshotCachedOnceUntilTrigger', 'profileIncomplete', 'lateIndividualGuardRefusalIsIncomplete', 'privateIdentityFilesAbsent'])
w_view['generationProof'] = pick(watcher['currentOrdinaryGenerationProofs']['gateway'], ['sourceCount', 'source65Match', 'fullManifestCount', 'fullManifestSha256', 'fullManifestAllMatch', 'currentProducer8AndActiveSdkContractsPairsVerified', 'app147MarkerClean', 'additionalMarkerCheckedCount', 'currentOrdinaryCensusSha256', 'role', 'files', 'currentOrdinaryModuleGenerations', 'noHistoricalNativeByteEquivalenceClaimed'])
project(PROFILE / 'watcher-receipt.json', 'closed/watcher-projection.json', w_view)
supervisor_path = Path(str(LIVE) + '-supervisor.json')
project(supervisor_path, 'closed/managed-supervisor-projection.json', pick(load(supervisor_path), ['preparationOnly', 'noPathShimImportReplacementSourceHookNoBuildOrLoggerOverride', 'runtimeCpuOverrideAbsent', 'oneFixedTarget', 'noTargetSignalsOrCutoff', 'phase', 'targetSignalsSent', 'managedExitCode', 'managedIdentityAbsent', 'watcherExitCode', 'watcherGroupRemaining', 'watcherGroupAbsent', 'cleanup', 'source65After', 'originalSevenMappingsObserved', 'originalDebugSourceAndTwoProofFlagsObserved']))
cleanup_path = Path(str(LIVE) + '-independent-cleanup-source-audit.json')
cleanup = load(cleanup_path)
cleanup_view = pick(cleanup, ['ordinaryAcceptanceOnly', 'profilerEnabled', 'runResult', 'matrixCompleted', 'singleAttempts', 'singleVerifiedPositives', 'multipleAttempts', 'multipleVerifiedPositives', 'namespaceContainersAbsent', 'privateRuntimeDirectoryAbsent', 'allOriginalCleanupAbsenceVerified', 'preexistingResourcesPreserved', 'ownedSidecarsAbsent', 'privateMaterialRemoved', 'fullLiveManifestSourceCount', 'fullLiveManifestHashMismatches', 'allIndependentRecordedProcessAndContainerAbsences', 'actualBrowserArtifactsRequireIndependentXmlAndPersistencePrivacyReview', 'profilingAttempted', 'actualAttachMetadataObserved', 'diagnosticOnly', 'featureAcceptance', 'profilingDoesNotChangeOriginalRunResult'])
cleanup_view.update({'recordedIdentityCount': len(cleanup['identities']), 'recordedIdentitiesAbsent': all(x['recordedIdentityAbsent'] for x in cleanup['identities']), 'containerCount': len(cleanup['containers']), 'containersAbsent': all(x['absenceVerified'] for x in cleanup['containers']), 'nativeGroupsAbsent': all(x['descendantsAbsent'] for x in cleanup['nativeTestProcessCleanup'])})
project(cleanup_path, 'closed/independent-cleanup-projection.json', cleanup_view)
run_path = LIVE / 'run.json'
run = load(run_path)
assert run['result'] == 'failed'
project(run_path, 'closed/run-result-projection.json', {**pick(run, ['acceptedEquivalence', 'productionReadiness', 'matrixCompleted', 'freshArtifacts', 'result', 'ownedSidecarsObservedBeforeStop', 'ownedSidecarsAbsent', 'preexistingResourcesPreserved', 'privateMaterialRemoved']), 'launcherControls': pick(run['launcherControls'], ['total', 'passed', 'failed', 'skipped']), 'nativeSeedGroupsAbsent': all(x['descendantsAbsent'] for x in run['testProcessCleanup']), 'browserAcceptanceAssertionsObserved': audit['outcome']['browserAcceptanceAssertionsObserved']})
xml_path = LIVE / 'browser-seed.xml'
assemblies = ET.parse(xml_path).getroot().findall('.//assembly')
assert len(assemblies) == 1
xml_counts = {k: int(assemblies[0].attrib[k]) for k in ['errors', 'failed', 'not-run', 'passed', 'skipped', 'total']}
assert xml_counts == {'errors': 0, 'failed': 1, 'not-run': 0, 'passed': 0, 'skipped': 0, 'total': 1}
project(xml_path, 'closed/native-seed-result-projection.json', {'sourceKind': 'xunit-v3-xml', 'assemblyCount': 1, 'counts': xml_counts, 'browserAcceptanceAssertionsObserved': 0, 'newNativeQualificationClaimed': False, 'namesPathsMessagesOutputAndIdentifiersArchived': False})

qualification = Path('/tmp/story65-cpu-prep-20261107/compile-receipt.json')
q = load(qualification)
project(qualification, 'references/original-collector-compile-projection.json', {**pick(q, ['temporaryDebugToolOnly', 'profilerAttached', 'repositorySourceOrBinaryBuild']), 'steps': [pick(x, ['exitCode', 'wallSeconds', 'log', 'logSha256']) for x in q['steps']], 'toolPath': pins['toolPath'], 'toolSha256': pins['toolSha256'], 'programSha256': pins['originalCollectorProgramSha256'], 'historicalCompileOnlyNoNewBuild': True})
copy('/tmp/story65-cpu-prep-20261107/provider-parser-smoke.json', 'references/original-provider-parser-smoke.json')
apps = load(PREP / 'watcher/current-app147-prelaunch.json')['currentFilesSha256']
deltas = [{'path': p, 'prelaunchSha256': apps[p], 'postclosureSha256': sha(p)} for p in audit['freshApp147GenerationDifferences']]
assert len(deltas) == 7
save('inputs/app147-generation-deltas.json', encode({'scope': 'Seven fresh ordinary standard-build generations; no historical native-generation byte-equality claim.', 'count': len(deltas), 'differences': deltas}))
copy(Path(__file__), 'package.py')
save('provenance.json', encode({'diagnosticOnly': True, 'featureAcceptance': False, 'copiesAndProjections': rows, 'notArchived': ['raw run/readiness/seed graphs', 'private identity input', 'argv/environment/configuration contents', 'proc maps', 'URLs/authentication/request/response payloads', 'native stdout and raw logs', 'binaries/binobj', 'nettrace/ETLX contents'], 'packagingSourcePath': str(Path(__file__)), 'packagingSourceSha256': sha(__file__)}))
readme = """# Current canonical-memo Gateway CPU diagnostic

Status: diagnostic evidence only. The unchanged managed command exited 1: SINGLE 1/1, MULTI 30/0, browser acceptance assertions 0. Its one native seed test failed; the 142 launcher controls passed. The previously qualified native 1,076 executions were not rerun or increased by this diagnostic. Feature acceptance remains false.

One bounded 8-second Gateway-process sample completed: 128,231 sanitized rows, 1,211 stack groups and 21 sampled threads; 6,261 Managed and 121,970 External samples. No conversion/event loss, retained-row/group cap, missing stack or truncation was reported. There are 14,236 unresolved-frame sample occurrences, 54 suspension-start overwrites and 54 unpaired restarts. These are sampling classifications, not exact CPU fractions, durations or blocked-time accounting. Inclusive counts overlap; safepoint bias, sampler overhead, omitted native-only threads and concurrent/background GC wall intervals limit interpretation. No old CPU-count subtraction, request/outcome/expiry join, or join to earlier phase captures is made.

The current ordinary SDK is 873dbb74; Contracts is f1cf82fe. Source 65, the closed/current 17,361-path live manifest, core 8 and 32 active SDK/Contracts copy files passed the independent audit. Seven app147 DLL generations legitimately changed during the standard managed build; those prelaunch/current hashes are retained explicitly. App147 plus two additional files were marker checked. This establishes the recorded current generation, not historical all-generation equivalence. The spec is outside live-manifest membership.

The unchanged legacy collector accepts broader command-marker positions, lacks a SourceProject field, directly checks only the Gateway map and emits a fresh mapped hash without enforcing equality. The external watcher binds the exact EventStore source/DCP identity and all five mapped modules before/after collection; the independent audit reconciles the collector's emitted Gateway hash. Graph resource eventstore, collector resource eventstorehost and role gateway remain distinct fixed literals. The external coordinator's new PGID-or-SID membership scan includes zombies; its offline five-check smoke and source adaptation are included. This does not strengthen the collector's own guards by implication.

All owned diagnostic groups, managed identities and the seed group were freshly absent including zombies. Five filtered-owned containers were absent; original cleanup, preexisting-resource preservation and private-runtime removal passed. Raw nettrace/ETLX and private identity input were hash-recorded and removed. Only the six reviewed sanitized profile files are retained here; JSONL is deterministic gzip with its original SHA/byte length in provenance. Runtime run/readiness graphs, URLs/authentication/configuration, private identity inputs, raw argv/maps/logs/native stdout, payloads and binaries are excluded. Run/XML/supervisor/cleanup artifacts use fixed projections. The package copies source and metadata only; it starts no process, profiler, build, test or host.

The independent audit and post-closure interpretation are byte-exact. Provenance records every copied/projected original path, digest and byte count. SHA256SUMS binds this diagnostic archive; publication does not change production code or accept the feature.
"""
save('README.md', readme.replace('\n', '\r\n').encode())
files = {str(p.relative_to(OUT)): sha(p) for p in sorted(OUT.rglob('*')) if p.is_file()}
save('manifest.json', encode({'diagnosticOnly': True, 'featureAcceptance': False, 'fileCountExcludingManifestAndSums': len(files), 'filesSha256': files, 'rawRowOriginalSha256': sha(PROFILE / 'gateway/cpu-samples.jsonl'), 'rawRowOriginalBytes': (PROFILE / 'gateway/cpu-samples.jsonl').stat().st_size, 'source65': True, 'liveManifestPaths': 17361, 'strictRows': 128231, 'stackGroups': 1211}))
files = {str(p.relative_to(OUT)): sha(p) for p in sorted(OUT.rglob('*')) if p.is_file()}
save('SHA256SUMS', ''.join(f'{digest}  {name}\n' for name, digest in files.items()).encode())
print(json.dumps({'destination': str(OUT), 'files': len(files) + 1, 'manifestSha256': sha(OUT / 'manifest.json'), 'sumsSha256': sha(OUT / 'SHA256SUMS'), 'readmeSha256': sha(OUT / 'README.md')}))
