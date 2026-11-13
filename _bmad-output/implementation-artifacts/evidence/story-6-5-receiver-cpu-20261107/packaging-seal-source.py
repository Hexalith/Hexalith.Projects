from pathlib import Path
import datetime, gzip, hashlib, importlib.util, json, os, re, stat

spec = importlib.util.spec_from_file_location('receiver_package', '/tmp/story65-receiver-cpu-package.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
P = m.PACKET
Q = m.WATCH_QUAL


def main():
    assert P.is_dir() and not (P / 'SHA256SUMS').exists()
    final = {
        'parent-final-live-audit.json': '221c04d843de8956e1285603209e0cf2fefd5ffc23d62eff6fdf2f2bc6e984f5',
        'parent-final-ordinary-binary-audit.json': '022a574c6046df1b54b6949af3710c0753d8764b8b8d0587706a9c15993f65a5',
        'independent-profile-audit.json': 'b574e06b2d36480a8f993ca3e24e135f5a26e9b6163be43f78b5ca4af2b8edc6',
        'independent-profile-audit-source.py': '7689558b91a540afa6ab8caed72a9b7548125bbe8543af3c695ba2e4909ecfe9',
    }
    for name, expected in final.items():
        assert m.digest(m.read(Q / name)) == expected
    for name in (*final, 'parent-final-ordinary-binary-audit.py'):
        dest = P / 'watcher-qualification' / name
        if dest.exists():
            assert m.read(dest) == m.read(Q / name)
        else:
            m.copy(Q / name, Path('watcher-qualification') / name)
    live = m.strict(m.read(Q / 'parent-final-live-audit.json'))
    assert live['allMatched'] is True and live['currentFullSourceManifestRehashed'] == 17340 and live['source55Rehashed'] == 55
    assert live['exactNumericIdentitiesAbsent'] == 29 and len(live['diagnosticAndSeedGroupsAbsent']) == 8
    assert all(live[k] is True for k in ('all5ExactContainersAbsentFromOriginalAndIndependentAudit',
        'originalAllCleanupPreservationFlagsTrue', 'roleDirs0700RegularAllowlistOnlyFiles0600',
        'rawNettraceEtlxNonallowlistedFilesAbsent', 'all3PrivateIdentityFilesAbsent',
        'all3CollectorsAndGroupsClosedBeforeSerialParsers', 'allDiagnosticGroupsAbsent',
        'postCollectionIdentityGenerationMapGuardPassed', 'frozenSprintIndexUnchanged', 'gitDiffCheckPass'))
    binary = m.strict(m.read(Q / 'parent-final-ordinary-binary-audit.json'))
    assert len(binary['actualManagedSourceGraphProjects']) == 42
    assert binary['actualSourceGraphProjectsBoundToUnchangedFullManifest'] is True
    assert binary['noSourceOverlayOrRestorationBuildPerformed'] is True
    assert binary['historicalBlanketByteEquivalenceClaimed'] is False
    assert binary['sdk']['sdkCensusCopies'] == 137 and binary['sdk']['allEightCurrentProducer'] is True
    assert binary['sdk']['producerSha256'] == '5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872'
    assert binary['sdk']['producerPdbSha256'] == '80a7174c81e2dc92d8abc4eea2bc371b672066d85c444f66e437dbe13e168af7'
    assert binary['native147']['count'] == 147 and binary['native147']['allPrivateMarkersAbsent'] is True
    assert len(binary['nativeArtifacts']) == 6 and all(not r['privateMarker'] for r in binary['nativeArtifacts'])
    dependencies = binary['dependencies']
    expected_scopes = {'Hexalith.EventStore.DomainService': (98, 0), 'Hexalith.EventStore.Server': (46, 20),
                       'Hexalith.EventStore.Gateway': (20, 14), 'Hexalith.Projects.Server': (10, 0)}
    for name, (old, current) in expected_scopes.items():
        assert dependencies[name]['unchangedBeforeRun'] == old and dependencies[name]['newCurrentProducer'] == current
    run = m.strict(m.read(m.LIVE / 'run.json'))
    assert all(re.fullmatch('[0-9a-f]{40}', x) for x in run['revisions'].values())
    m.save('full-revisions-and-qualification-boundaries.json', {
        'revisions': run['revisions'], 'source55ControlHashes': m.strict(m.read(m.PREP / 'watcher/source-controls.json')),
        'fullLiveManifestSourceInputs': 17340, 'fullLiveManifestHashMismatches': 0,
        'priorOrdinaryUniqueNativePasses': 1038, 'native1038RequalifiedByThisRun': False,
        'collectorOfflineChecks': 90, 'watcherMockedOfflineChecks': 24,
        'theseChecksAreNotAdditionalProductNativeTests': True, 'featureAcceptance': False})
    m.save('durable-qualification-references.json', {
        'existingOrdinaryNative1038': '_bmad-output/implementation-artifacts/evidence/story-6-5-parsed-template-ordinary-20261107',
        'priorOrdinarySdkAndSource55Restoration': '_bmad-output/implementation-artifacts/evidence/story-6-5-invocation-transport-20261107',
        'nativeSuitesAndSourceSnapshotsNotDuplicated': True, 'oldPacketsNotModified': True})
    m.save('final-custody-and-audit-bindings.json', {
        'parentFinalLiveAuditSha256': final['parent-final-live-audit.json'],
        'parentFinalOrdinaryBinaryAuditSha256': final['parent-final-ordinary-binary-audit.json'],
        'independentProfileAuditSha256': final['independent-profile-audit.json'],
        'independentProfileAuditSourceSha256': final['independent-profile-audit-source.py'],
        'independentProfileAuditSourceIsReviewableReconstructionNotRerun': True,
        'source55AndFull17340Preserved': True, 'currentManagedSourceGraphProjects': 42,
        'ordinaryGenerationAuditAdapterHistoryPreservedInParentReceipt': binary['auditAdapterHistory'],
        'noRestorativeBuildOrSourceChangeForGenerationAuditCorrections': True,
        'privateIdentityConfigEnvironmentGraphTraceEtlxOrBinaryContentsCopied': False,
        'rawAndDerivativeDeletionProofsRetainedInCoordinatorAndPerRoleSupervisorReceipts': True,
        'oldPrivateCaptureDirectoryWasNotTouchedByPacketAssembly': True})
    readme = '''# Receiver process CPU/runtime-GC evidence, 7 November 2026

Current acceptance failed: SINGLE had one verified positive on attempt 2 of 2; MULTIPLE had no positive in 30 attempts, and the authenticated browser journey did not run. The standard managed launcher exited 1. The diagnostic watcher and outer supervisor exited 0 after collecting and cleaning up. This packet records a diagnostic run; it does not establish feature acceptance or a performance improvement.

## Actual bounded collection

The unchanged ordinary command was `npm --prefix tests/e2e run test:live:managed`. It used the original managed launcher, normal source-mapping compilation and natural CPU availability, with `DOTNET_PROCESSOR_COUNT` unset. There were no source substitutions, private application assemblies, logger overrides, deadline or expectation changes, PATH shims, import replacements, topology cutoffs, or target signals.

One fresh owned AppHost/DCP graph identified the Projects, Tenants and Folders source runtimes. AppHost and CLI start identities and their existing evidence binding were checked before private configuration reads. Each exact role, source project, DCP wrapper, runtime start, native/dotnet entry form, entry/DomainService/SDK hashes and mapped assemblies passed before attachment and after collection. Successful early discovery/source checks were cached; final guards and the completed readiness decision were checked again before launch. The trigger was a genuine completed HTTP-200 MULTIPLE failure, no observed genuine positive, and at least three attempt windows remaining.

Three collectors attached in one diagnostic window. Attach start spread was 0.305304 ms. The nominal requested overlap was 7,957.077203 ms; this is a metadata estimate and does not identify exact sample, request or logical-root overlap. Collection finished for all roles before any analysis began. The three parsers then ran sequentially.

| Process role | Retained samples | Managed labels | External labels | Stack groups | Unresolved frames |
| --- | ---: | ---: | ---: | ---: | ---: |
| Projects | 121,031 | 551 | 120,480 | 240 | 7,171 |
| Tenants | 100,357 | 129 | 100,228 | 45 | 7,168 |
| Folders | 119,824 | 1,560 | 118,264 | 454 | 14,212 |

All 341,212 original sanitized rows reconcile exactly to their type and stack-group aggregates. Missing stacks, truncated stacks, group-cap overflows, row caps, conversion loss and reported source-event loss were zero. All three sessions ended by normal duration, stopped and drained, without hitting the file cap. Sanitized summaries and all six per-role metadata files are preserved byte for byte, except that the sample JSONLs are represented by deterministic gzip files with verified original hashes, lengths and decompression identity. No uncompressed duplicate is included.

## Interpretation limits

These are physical sampled execution-stack labels, not CPU-time measurements. `External` is not evidence that a thread was blocked, and sample fractions are not CPU utilization. Sampling has safepoint and suspension bias. The collector records all work in each process during the interval; it does not identify request kinds, route owners, logical roots, or downstream causes. There is no join to previous client/controller/handler probes, no end-to-end critical-path inference, no ordinary latency claim and no subtraction of collector overhead.

GC SuspendEE-to-RestartEE pairs are wall intervals and remain separate from GCStart-to-GCStop cycle intervals. Other runtime/sample-profiler suspensions are not all GC pauses. Projects had five overwritten suspend starts and five unmatched restarts; Folders had eleven of each; Tenants had neither. These pairing holes, unresolved frames and possible event coverage effects prevent a complete suspension/GC accounting. Recorded loss counters being zero does not resolve those limits. Native symbols were not requested.

The providers were `Microsoft-DotNETCore-SampleProfiler` (keywords `0x0`, level 4) and `Microsoft-Windows-DotNETRuntime` (GC keywords `0x1`, level 4), with stackwalk and rundown. There were no allocation ticks, additional application/network providers, dumps or forced collections. Each session requested eight seconds, a 64 MiB session buffer, a 256 MiB file cap and a 224 MiB stop threshold to reserve rundown headroom. Buffer size is not the file cap. Internal collector guards were four CPU seconds and 512 MiB observed RSS; external collection/parser limits and their actual usage are retained in the supervisor receipts. The three collectors used about 494–513 CPU milliseconds and 73–74 MiB observed peak RSS each; those are collector costs, not target CPU.

## Qualification and retained setup failure

The first collector build completed with five CA1416 warnings. Its offline smoke stopped after 23 checks with a dependency-resolution FileNotFound failure; the failed source generation, commands, logs, snapshots and proposed correction are retained. The reviewed correction added the Linux assembly contract and corrected the smoke's cached dependency resolver. The accepted retry builds had zero warnings/errors and passed 90 pure collector guards. The collector algorithm was unchanged. These tests did not attach to a target or establish live DCP ownership.

The final watcher passed 24 mocked offline guards with no real application, collector, parser, attachment or target signal. The frozen disabled execution manifest and the release receipt preserve the flag-only release, all 488 inputs and the qualified tool/runtime closure. Real ownership/maps checks are supplied by this run's receipts. No new product native qualification ran: the previously qualified 1,038 unique tests remain a historical boundary and are not increased by the 90/24 tool checks or this failed browser seed.

## Cleanup and ordinary generations

The original run and independent cleanup history are retained, including the initial observation before the final absence proof. Root's final audit rehashed all 17,340 live source inputs and all 55 controls with zero mismatches. It verified 29 exact numeric identities, eight diagnostic/watcher/seed groups and all five owned containers absent; original cleanup/preservation flags passed, and namespace/private runtime material was absent. Collector/parser groups closed before sequential parsing and final raw-file cleanup. The old private capture directory was independently proven to be the same empty directory and was not used by this probe.

Raw nettrace, ETLX, temporary derivatives and all three private identity contracts were hashed and deleted. Only the six fixed sanitized metadata files remained in each fresh 0700 role directory, as regular 0600 files. The packet contains those approved metadata bytes and hash/size/deletion receipts. It excludes private identity/configuration/environment contents, raw graph output, raw traces, ETLX, binaries, payloads and protected fixture objects. Readiness retains only closed scalar fields; excluded fixture artifacts are represented by hashes and sizes.

The ordinary binary audit passed without a restoration build or source edit. The SDK census had 137 entries, with all eight current SDK runtime copies coherent with DLL `5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872` and PDB `80a7174c81e2dc92d8abc4eea2bc371b672066d85c444f66e437dbe13e168af7`. DomainService's 98 entries retained their prior bytes. Server's 66 entries consisted of 46 exact prior entries and 20 current producer entries; Gateway's 34 comprised 20 prior and 14 current entries; the ten Projects.Server entries retained prior bytes. The 147 native DLL census and six native test artifacts had no private markers; six ordinary native-census DLLs changed during the normal managed build.

The audit initially reused an overly strict all-copies-current-generation assertion, then a prior 14-project scope that omitted four Server DLL/PDB copies in Testing/Tenants.Server. The final rule permits exact recorded prior bytes or exact current producer bytes in the explicit prior repair/current 42-project managed source graph; unknown bytes or projects still fail. The full source manifest binds those project inputs. This correction changed audit interpretation only; it did not rebuild or alter application sources, and it makes no blanket historical byte-equivalence claim.

## Packet map

- `preparation/`: exact collector, primary-source baseline and watcher code/plans/pins.
- `collector-qualification/`: both attempts, correction, 90-check receipt and parent audit.
- `watcher-qualification/`: 24-check qualification, release proof, final live/binary audits and independent profile audit. The saved independent audit source is a reviewable reconstruction, not an extra execution.
- `profiles/`: original sanitized summaries/receipts, three sample gzips and exact compression/schema verification.
- `managed/`: original approved run/command/supervisor/cleanup artifacts and scalar-only readiness.
- Root JSON files retain revision hashes, count boundaries, byte-copy provenance and final audit bindings. Prior 1,038-test qualification/source snapshots are referenced through already durable evidence instead of duplicated.

`SHA256SUMS` covers every packet artifact except itself. Publication/spec edits remain Root-owned and are not performed by this packaging step.
'''
    (P / 'README.md').write_text(readme)
    m.copy(__file__, 'packaging-seal-source.py')
    # Source/metadata bytes are never rewritten by packaging. Validate every copied original again.
    originals = m.strict(m.read(P / 'byte-copy-provenance.json'))
    for row in originals:
        assert m.digest(m.read(P / row['packetPath'])) == row['sha256'] == m.digest(m.read(row['source']))
    m.save('final-added-original-provenance.json', m.COPIED)
    blocked = []
    scanned = 0
    gzip_count = 0
    rows = 0
    for path in sorted(P.rglob('*')):
        if path.is_dir():
            continue
        assert not path.is_symlink()
        assert path.suffix.lower() not in {'.dll', '.pdb', '.exe', '.pyc', '.cache', '.nettrace', '.etlx'}
        raw = m.read(path)
        if path.suffix == '.gz':
            raw = gzip.decompress(raw)
            gzip_count += 1
            rows += len(raw.splitlines())
        scanned += 1
        if re.search(rb'eyJ[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}', raw):
            blocked.append(str(path.relative_to(P)))
        if re.search(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----\s+[A-Za-z0-9+/=]{24,}', raw):
            blocked.append(str(path.relative_to(P)))
    assert not blocked and gzip_count == 3 and rows == 341212
    m.save('privacy-and-artifact-scan.json', {'scannedArtifactsBeforeThisReceipt': scanned,
        'gzipFiles': gzip_count, 'decompressedSanitizedRows': rows,
        'jwtOrPrivateKeyPatternMatches': 0, 'symlinkArtifacts': 0,
        'binaryCacheRawTraceEtlxArtifacts': 0, 'rawPrivateIdentityConfigEnvironmentGraphOrControlContentsCopied': False,
        'patternScanIsAdditionalCheckNotProofOfAllPrivacyProperties': True,
        'primaryPrivacyProofIsClosedSchemaAllowlistSanitizerAndRawRemovalAudits': True})
    m.save('sealed-status.json', {'sealedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'sourceRunAndParentInputsClosed': True, 'finalLiveBinaryIndependentProfileAuditsBound': True,
        'all341212RowsStrictlyReconciledAndByteCompressed': True, 'published': False,
        'featureAcceptance': False, 'noAdditionalAppBuildTestHostProfilerSourceSpecOrGitOperations': True})
    files = [p for p in sorted(P.rglob('*')) if p.is_file()]
    sums = ''.join(m.digest(m.read(p)) + '  ' + str(p.relative_to(P)) + '\n' for p in files)
    (P / 'SHA256SUMS').write_text(sums)
    for line in sums.splitlines():
        h, name = line.split('  ', 1)
        assert m.digest(m.read(P / name)) == h
    for path in (*files, P / 'SHA256SUMS'):
        path.chmod(0o444)
    print(json.dumps({'packet': str(P), 'checksumEntries': len(files),
        'sha256sumsSha256': m.digest(m.read(P / 'SHA256SUMS')),
        'readmeSha256': m.digest(m.read(P / 'README.md')), 'gzipFiles': 3,
        'verifiedSampleRows': 341212, 'sealed': True, 'published': False}))


if __name__ == '__main__':
    main()
