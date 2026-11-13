"""Ordinary managed lane only. Final native/transfer binding and Root release required."""
from pathlib import Path
import argparse, datetime, hashlib, json, os, re, stat, subprocess, sys, time
ROOT = Path('/home/administrator/projects/hexalith/projects')
PREP = Path(__file__).parent
LIVE = Path('/tmp/story65-canonical-hash-acceptance-live-20261108')
MAPPING = {'CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets', 'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH', 'Configuration'}
MARKERS = ['Story65Timing', 'Story65PrivateDiagnostic', 'Hexalith.PrivateOwnerTiming', 'PrivateFocusedTiming', 'TimingHexalith', 'Hexalith.PrivateCaptureCounter', 'InvocationTransportBootstrap', 'GatewayControllerBootstrap', 'ReceiverPhaseBootstrap', 'CaptureCounters.cs']

def require(value, check):
    if not value: raise RuntimeError(check)

def sha(path):
    fd = os.open(path, os.O_RDONLY | os.O_NONBLOCK | os.O_NOFOLLOW | os.O_CLOEXEC)
    with os.fdopen(fd, 'rb') as stream:
        require(stat.S_ISREG(os.fstat(stream.fileno()).st_mode), 'regular-owned-input')
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def read(path): return json.loads(Path(path).read_bytes())

def write(path, value):
    path = Path(path); temporary = Path(str(path) + '.tmp')
    temporary.write_text(json.dumps(value, indent=2) + '\n'); temporary.chmod(0o600); temporary.replace(path)

def start(pid):
    try:
        fields = Path(f'/proc/{pid}/stat').read_text().rsplit(')', 1)[1].split()
        return None if fields[0] == 'Z' else fields[19]
    except (FileNotFoundError, ProcessLookupError): return None

def sources(binding):
    path = Path(binding['sourceControls']['path']); require(sha(path) == binding['sourceControls']['sha256'], 'source65-receipt')
    rows = read(path)['sourceFilesSha256']; require(len(rows) == 65, 'source65-count')
    for relative, digest in rows.items():
        p = Path(relative)
        require(not p.is_absolute() and '..' not in p.parts and (ROOT / p).resolve().is_relative_to(ROOT), 'source-coordinate')
        require(sha(ROOT / p) == digest, 'source65-mismatch')
    return {'sourceCount': 65, 'allMatch': True, 'receiptSha256': sha(path)}

def protected(binding):
    baseline = binding['protectedBaseline']; require(sha(baseline['path']) == baseline['sha256'], 'protected-baseline')
    expected = read(baseline['path'])
    spec = ROOT / '_bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md'
    frozen = re.search(rb'<frozen-after-approval.*?</frozen-after-approval>', spec.read_bytes(), re.S)
    require(frozen is not None and hashlib.sha256(frozen.group()).hexdigest() == expected['frozen_sha256'], 'frozen-approval-block')
    require(sha(ROOT / '_bmad-output/implementation-artifacts/sprint-status.yaml') == expected['sprint_sha256'], 'sprint-preservation')
    index = hashlib.sha256(subprocess.check_output(['git', 'diff', '--cached', '--binary'], cwd=ROOT)).hexdigest()
    require(index == expected['index_sha256'], 'index-preservation')
    return {'frozenBlockMatch': True, 'sprintMatch': True, 'indexMatch': True}

def evidence(row):
    require(isinstance(row, dict) and row.get('path') and row.get('sha256'), 'final-evidence-not-bound')
    require(sha(row['path']) == row['sha256'], 'final-evidence-hash')
    data = read(row['path']); required = row.get('requiredFields')
    require(isinstance(required, dict) and bool(required), 'explicit-final-evidence-predicates')
    require(all(data.get(k) == v for k, v in required.items()), 'final-evidence-not-qualified')
    return data

def generations(binding, prelaunch=False):
    expected = binding['producerFilesSha256']; require(isinstance(expected, dict) and len(expected) == 8, 'final-sdk-contracts-eight-artifacts-required')
    for path, digest in expected.items(): require(sha(path) == digest, 'final-producer-generation-mismatch')
    copies = []
    for directory in binding['applicationBinaryDirectories']:
        for name in ['Hexalith.EventStore.Contracts', 'Hexalith.EventStore.ServiceDefaults']:
            for suffix in ['dll', 'pdb']:
                path = Path(directory) / (name + '.' + suffix)
                producer = ROOT / 'references/Hexalith.EventStore/src' / name / 'bin/Debug/net10.0' / path.name
                require(str(producer) in expected and sha(path) == expected[str(producer)], 'current-eight-runtime-native-sdk-contracts-copy')
                copies.append({'path': str(path), 'sha256': sha(path)})
    binaries = {}
    additional = {}
    directories = {str(Path(d)) for d in binding['applicationBinaryDirectories']}
    for coordinate in binding['currentApp147Sha256']:
        path = Path(coordinate)
        require(str(path.parent) in directories and path.name.startswith('Hexalith.') and path.suffix == '.dll', 'bound-app147-coordinate')
        data = path.read_bytes()
        require(not any(m.encode() in data or m.encode('utf-16-le') in data for m in MARKERS), 'private-diagnostic-marker')
        binaries[str(path)] = hashlib.sha256(data).hexdigest()
    for directory in directories:
        for path in sorted(Path(directory).glob('Hexalith.*.dll')):
            if str(path) in binaries:
                continue
            data = path.read_bytes()
            require(not any(m.encode() in data or m.encode('utf-16-le') in data for m in MARKERS), 'additional-private-diagnostic-marker')
            additional[str(path)] = hashlib.sha256(data).hexdigest()
    require(len(binaries) == 147, 'current-app147-pathset')
    require(set(binaries) == set(binding['currentApp147Sha256']), 'current-app147-pathset-changed')
    if prelaunch: require(binaries == binding['currentApp147Sha256'], 'bound-prelaunch-generation')
    return {'producerFilesSha256': expected, 'currentApp147Sha256': binaries, 'additionalHexalithAssemblyFilesSha256': additional, 'additionalAssembliesNotPartOfApp147Claim': True, 'currentSdkContractsCopies': copies, 'historical1048ByteEqualityClaimed': False}

def cleanup_audit():
    observations = []; latest = None; deadline = time.monotonic() + 10
    while True:
        path = Path(str(LIVE) + '-cleanup-observation-' + str(len(observations) + 1) + '.json')
        result = subprocess.run([sys.executable, str(PREP / 'audit.py'), '--evidence', str(LIVE), '--output', str(path)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        current = read(path) if result.returncode == 0 and path.exists() else None
        observations.append({'path': str(path), 'exitCode': result.returncode, 'allRecordedAbsent': isinstance(current, dict) and current.get('allIndependentRecordedProcessAndContainerAbsences') is True})
        if current is not None: latest = current
        if latest and latest.get('allIndependentRecordedProcessAndContainerAbsences') is True: break
        if time.monotonic() >= deadline: break
        time.sleep(.25)
    write(Path(str(LIVE) + '-cleanup-observation-history.json'), {'signalsSent': False, 'maximumReadOnlyPollingSeconds': 10, 'observations': observations})
    if latest is None: return {'available': False, 'passed': False}
    write(Path(str(LIVE) + '-independent-cleanup-source-audit.json'), latest)
    run = read(LIVE / 'run.json'); native = latest.get('nativeTestProcessCleanup')
    native_safe = isinstance(native, list) and all(v.get('descendantsAbsent') is True for v in native)
    if native is None: native_safe = run.get('testAssemblySha256') is None and not any(isinstance(c, list) and len(c) > 1 and Path(c[1]).name == 'Hexalith.Projects.Integration.Tests.dll' for c in run.get('commands', []))
    keys = ['allIndependentRecordedProcessAndContainerAbsences', 'allOriginalCleanupAbsenceVerified', 'preexistingResourcesPreserved', 'ownedSidecarsAbsent', 'privateMaterialRemoved', 'namespaceContainersAbsent', 'privateRuntimeDirectoryAbsent']
    return {'available': True, 'passed': all(latest.get(k) is True for k in keys) and latest.get('fullLiveManifestHashMismatches') == 0 and native_safe, 'nativeGroupsAbsentOrNeverStarted': native_safe, 'fullManifestMismatches': latest.get('fullLiveManifestHashMismatches'), 'runResult': latest.get('runResult')}

def main():
    parser = argparse.ArgumentParser(); parser.add_argument('--execute', action='store_true'); parser.add_argument('--release', type=Path); parser.add_argument('--release-sha256'); args = parser.parse_args()
    command = ['npm', '--prefix', 'tests/e2e', 'run', 'test:live:managed']
    if not args.execute:
        print(json.dumps({'execution': False, 'command': command, 'evidence': str(LIVE), 'profiler': False, 'watcher': False, 'overlay': False, 'finalNativeAndCompiledTransferBindingRequired': True})); return
    os.umask(0o077)
    require(args.release is not None and args.release_sha256 and sha(args.release) == args.release_sha256, 'root-release-required')
    binding = read(args.release); require(binding['executionReleased'] is True and binding['nativeAndCompiledTransferGenerationsAgree'] is True, 'release-not-ready')
    require(not LIVE.exists() and not list(LIVE.parent.glob(LIVE.name + '-*')), 'fresh-live-coordinates')
    for path, digest in binding['preparedAndStaticInputsSha256'].items(): require(sha(path) == digest, 'frozen-preparation-or-original-launcher')
    native = evidence(binding['finalNativeReceipt']); evidence(binding['parentNativeAudit']); evidence(binding['compiledTransferAudit'])
    require(native['nativeTotal'] == 1076 and native['ownerCases'] == 450 and native['projectsCases'] == 626 and native['source65BeforeAfterIdentical'] is True and native['producerFilesSha256'] == binding['producerFilesSha256'], 'native1076-generation-binding')
    before = {'sources': sources(binding), 'protected': protected(binding), 'generations': generations(binding, True)}
    write(Path(str(LIVE) + '-preflight.json'), before)
    env = os.environ.copy()
    for key in MAPPING | {'DOTNET_PROCESSOR_COUNT', 'CustomBeforeMicrosoftCommonTargets'}: env.pop(key, None)
    assignments = {'MSBUILDDISABLENODEREUSE': '1', 'DOTNET_CLI_USE_MSBUILD_SERVER': '0', 'E2E_SUPPORTED_READS_ONLY': '1', 'E2E_RESOURCE_TIMEOUT_SECONDS': '900', 'E2E_SUPPORTED_EVIDENCE': str(LIVE)}
    env.update(assignments)
    log = Path(str(LIVE) + '-console.log'); child = None; status = {'ordinaryAcceptanceOnly': True, 'profilerEnabled': False, 'watcherEnabled': False, 'privateDiagnosticOverride': False, 'cutoffEnabled': False, 'targetSignalsSent': False, 'featureAcceptance': None, 'phase': 'starting', 'releaseSha256': args.release_sha256}
    try:
        with log.open('xb') as stream: child = subprocess.Popen(command, cwd=ROOT, env=env, stdout=stream, stderr=subprocess.STDOUT)
        status.update(launcherPid=child.pid, launcherStartTicks=None); write(Path(str(LIVE) + '-supervisor.json'), status)
        status['launcherStartTicks'] = start(child.pid)
        write(Path(str(LIVE) + '-command.json'), {'command': command, 'cwd': str(ROOT), 'launcherPid': child.pid, 'launcherStartTicks': status['launcherStartTicks'], 'startedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'environmentAssignments': assignments, 'sourceMappingHooksClearedBeforeOriginalLauncherReemitsSeven': True, 'runtimeCpuOverrideAbsent': True, 'noLauncherReplacementNoBuildCutoffOrTargetSignals': True})
        require(status['launcherStartTicks'] is not None, 'managed-start-identity')
        status['phase'] = 'ordinary-managed-running'; write(Path(str(LIVE) + '-supervisor.json'), status)
        status['managedExitCode'] = child.wait()
    finally:
        if child is not None:
            status['managedExitCode'] = child.wait(); status['launcherIdentityAbsent'] = start(child.pid) != status.get('launcherStartTicks')
            status['consoleLogSha256'] = sha(log)
            try: status['cleanup'] = cleanup_audit()
            except Exception as error: status['cleanupAuditErrorType'] = type(error).__name__
            try:
                status['source65After'] = sources(binding); status['protectedAfter'] = protected(binding)
                after = generations(binding)
                after['permittedFreshStandardBuildGenerationDifferences'] = [p for p, h in before['generations']['currentApp147Sha256'].items() if after['currentApp147Sha256'][p] != h]
                write(Path(str(LIVE) + '-postflight-generations.json'), after)
            except Exception as error: status['postflightErrorType'] = type(error).__name__
            try:
                run = read(LIVE / 'run.json')
                status['originalSevenMappingsObserved'] = set(run.get('sourceMappingEnvironment', {})) == MAPPING
                profile = run.get('compilationProfile', {})
                status['originalDebugSourceAndTwoProofFlagsObserved'] = bool(profile) and all(v.get('Configuration') == 'Debug' and v.get('Optimize') == 'false' and v.get('UseHexalithProjectReferences') == 'true' and v.get('UseNuGetDeps') == 'false' and v.get('EnablePrivateCatalogInvocationProof') == 'true' and v.get('EnablePrivateOwnerSourceReadProof') == 'true' for v in profile.values())
            except Exception as error: status['originalProfileObservationErrorType'] = type(error).__name__
        status['phase'] = 'ordinary-managed-complete'; write(Path(str(LIVE) + '-supervisor.json'), status)
    print(json.dumps({'phase': status['phase'], 'managedExitCode': status.get('managedExitCode'), 'cleanupPassed': status.get('cleanup', {}).get('passed', False), 'featureAcceptanceRequiresIndependentBrowserAndPrivacyReview': True}))

if __name__ == '__main__': main()
