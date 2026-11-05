from pathlib import Path
import datetime, hashlib, importlib.util, json, sqlite3, statistics, subprocess, sys
import xml.etree.ElementTree as ET

root = Path('/home/administrator/projects/hexalith/projects')
run = Path(sys.argv[1]).resolve()
spec = importlib.util.spec_from_file_location('story61_parent_validation_launcher', root / 'tools/qualification/run_supported_project_reads.py')
launcher = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = launcher
spec.loader.exec_module(launcher)
digest = lambda value: hashlib.sha256(value).hexdigest()
manifest = json.loads((run / 'source-manifest.json').read_text())
source = digest(json.dumps(manifest, sort_keys=True).encode())
assert launcher.source_manifest() == manifest
groups = [launcher.read_qualified_scenario(run / name, name, manifest, name == 'main') for name in launcher.SCENARIO_GROUPS]
aggregate = launcher.require_aggregate_scenarios(groups, True)
container_ids = [value for group in groups for value in group['containerIds']]
inspection = subprocess.run(['docker', 'inspect', '--format', '{{.Id}}', *container_ids], capture_output=True, text=True)
assert inspection.returncode != 0 and not inspection.stdout.strip(), 'A known owned container remains'
listing = subprocess.run(['docker', 'ps', '--all', '--no-trunc', '--format', '{{.ID}}'], capture_output=True, text=True, check=True)
listed_ids = {line.strip() for line in listing.stdout.splitlines() if line.strip()}
assert all(len(value) == 64 and all(char in '0123456789abcdef' for char in value) for value in listed_ids), 'A complete immutable-ID container listing is required'
assert listed_ids.isdisjoint(container_ids), 'A known owned container remains in the fresh successful listing'
native = {}
for name in ('integration', 'server'):
    assemblies = ET.parse(run / 'main' / (name + '.xml')).getroot().findall('assembly')
    native[name] = launcher.require_complete_native_results(assemblies)

matrix = {
    'Authorized list': ['ListDefaultsToFiftyAndPreservesConversationPolicy', 'ChatbotFiltersBeforePagingAndHasNoGaps', 'CursorRejectsTamperFilterIdentitySizeAndExpiredReplay', 'AuthorizedInvalidPagingIsSafe400Category'],
    'Authorized open': ['CurrentFolderEvidenceControlsEnumerationAndOpen', 'NoncurrentPersistedProjectIsUnavailable', 'SafeMetadataAndInspectedMetadataHaveDurableAuditOnlyEffects', 'AuditFailurePreventsDescriptiveDisclosure', 'PendingHistoricalFolderIdIsNotAnAcceptedBindingForOrdinaryReads', 'AdministratorInventoryAuditsEveryReturnedProjectAndBindsCursorToInventory', 'TenantCanInspectFolderlessHistoryWithoutChangingProjectState'],
    'Denied or absent': ['GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures', 'SupportedHandlerDenialsHaveSameHeadersSdkLogsAndHttpTelemetryCategories', 'AuthorityMismatchWinsOverInvalidPayload'],
    'Rebuild/fault': ['CompleteCatalogClassifiesHarmlessEventsAsDeterministicNoOps', 'InvalidDuplicateDeliveryFailsRatherThanBeingSkipped', 'IncrementalReplayDuplicateAndRestartPreserveExactGlobalPositions', 'MalformedKnownDuplicateCannotPreserveCurrent', 'MalformedKnownNoOpCannotAdvanceCurrent', 'OlderValidDuplicatePreservesLaterFolderObservation'],
}
tests = list(ET.parse(run / 'main/server.xml').getroot().iter('test'))
matrix_results = {}
for row, methods in matrix.items():
    coverage = {}
    for method in methods:
        matches = [test for test in tests if test.get('method') == method]
        assert matches and all(test.get('result') == 'Pass' for test in matches), method
        coverage[method] = len(matches)
    matrix_results[row] = coverage
for name in launcher.SCENARIO_GROUPS:
    report = json.loads((run / name / 'run.json').read_text())
    for peer in report['discoveryAfterReads']['ownedSidecars']:
        assert launcher.process_start(peer['pid']) != peer['startTicks'], 'A known owned sidecar remains'
    session = report['ownedAppHostSession']
    assert launcher.process_start(session['appHostPid']) != session['appHostStartTicks'], 'A known owned AppHost remains'

result={'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceManifestSha256':source,'currentSourceInputsUnchanged':True,'aggregateStrictlyRevalidated':True,'aggregate':aggregate,'native':native,'matrixCoverage':matrix_results,'allTwelveOwnedContainersAbsent':True,'allOriginalSidecarsAndAppHostsAbsent':True}
(run/'parent-runtime-validation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'sourceManifestSha256':source,'unchangedInputs':len(manifest),'aggregatePassed':True,'native':native,'allCleanupVerified':True}))
