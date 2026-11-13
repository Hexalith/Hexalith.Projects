from pathlib import Path
import datetime, importlib.util, json, re

spec = importlib.util.spec_from_file_location('ordinary_package', '/tmp/story65-memo-rw-ordinary-package.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
P = m.PACKET


def main():
    assert not (P / 'SHA256SUMS').exists()
    readme = '''# Ordinary RWL acceptance failure, 7 November 2026

The unchanged ordinary managed run failed. SINGLE passed on attempt 1; all 30 MULTIPLE attempts returned HTTP 200 with null result, incomplete=true, zero candidates and no verified positive. The native browser seed failed its one case without errors/skips. No browser XML, privacy telemetry or browser proof was produced, so the current OIDC/browser/persistence/privacy/accessibility journey did not run. The supervisor exited 0 after waiting; the actual managed launcher exited 1. Wrapper success is not feature acceptance.

The exact command was `npm --prefix tests/e2e run test:live:managed`, using the normal launcher/source mapping, natural CPU availability and ordinary source builds. There were no profiles, watchers, source overlays, no-build overrides, altered checks/deadlines, cutoffs, target signals or retries. The reused supervisor/audit helpers changed only their default evidence paths. All seven mapping values remained under the original launcher's normal emission/validation.

The 31 readiness rows are retained as a strict closed scalar projection. Protected fixture objects and correlations are excluded; original bytes/hash/length are recorded. MULTIPLE's external fixture elapsed median was 2,412.99025 ms, including subsequent evidence reads. This is not exact API latency and does not identify an expiry stage. The rows do not prove an old advisory-wave cause or any particular downstream bottleneck.

The reviewed independent audit passed all 21 recorded kernel identities and five owned containers absent, all six original cleanup receipts, namespace/private-runtime absence, original resource preservation, sidecar absence and private-material removal. A fresh proc-stat census independently found the recorded native seed process group absent; no argv/environment was read and no signal was sent. All 17,341 live source inputs and all 56 current controls match, with the actual 42-project managed source graph bound to that manifest. Source pre/post receipts retain the current 49 owner source hashes. No source/spec/Git mutation was performed by this monitoring/packaging work. The native qualification's frozen/sprint/index baseline is preserved; it is labelled as that prior baseline rather than fabricated as a fresh post-run guard.

The narrowed binary proof verifies all 137 SDK census entries and native 147 DLLs have no private diagnostic markers. All eight known runtime/test SDK DLL/PDB copies match qualified production SDK `6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542` / PDB `ffa22cd52ff313ce95ee647d13f753c475b588137d08c0bf676c19b66e92a4b6`. The six native test artifacts are clean. Fourteen SDK rows outside the recorded build scope are also byte-equal to the current producer, while their build provenance is not attributed to this managed graph. Runtime proof here concerns source-output copies; there is no fresh post-exit memory-map claim.

Full dependency generation coherence is **not** asserted. The initial read-only generation guard stopped at Integration.Tests' Server PDB. No exact pre-live hash was found in the 34 checked frozen native/final-transfer metadata artifacts. The final limited report retains ten unresolved Server/Gateway paths (three DLLs and seven PDBs), alongside exact-prior/current scoped matches. It does not whitelist unknown generations, invent historical equality, or rebuild anything. Both audit sources and the original guard failure are preserved. Marker-clean and SDK-copy facts remain independently established; a complete older-generation provenance claim does not follow from them.

The prior 1,048 native cases and separate 621 compiled-transfer assertions are qualification context, not new counts from this failed seed. Historical manual accessibility evidence is neither requalified nor invalidated by a browser journey that was not reached. Current browser/persistence/privacy proof remains pending. No product repair, full host deadline gain or Story 6.5 acceptance is concluded.

`preparation/` contains exact helper code/commands/pins/release; `ordinary-run/` contains original approved command/source/run/XML metadata and scalar readiness. `audit-history/` retains the failed guard and limited audit. `qualification-reference/` binds the current native/source controls and prior guard baseline. Root JSON files retain group absence, exact outcome/provenance limits and excluded-artifact hashes. Raw runtime/console logs, fixture objects, private configuration/identity/graph material, credentials, traces and binaries are excluded. Publication/spec updates remain Root-owned.
'''
    (P / 'README.md').write_text(readme)
    m.copy(__file__, 'packaging-seal-source.py')
    m.save('final-added-original-provenance.json', m.COPIED)
    for row in m.strict(m.read(P / 'byte-copy-provenance.json')):
        assert m.sha(P / row['packetPath']) == row['sha256'] == m.sha(row['source'])
    files = [path for path in P.rglob('*') if path.is_file()]
    for path in files:
        assert not path.is_symlink() and path.suffix.lower() not in {'.dll', '.pdb', '.exe', '.pyc', '.cache', '.nettrace', '.etlx', '.log'}
        raw = m.read(path)
        assert not re.search(rb'eyJ[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}', raw)
        assert not re.search(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----\s+[A-Za-z0-9+/=]{24,}', raw)
    m.save('privacy-and-artifact-scan.json', {'checkedArtifactsBeforeThisReceipt': len(files),
        'rawRuntimeConsoleLogsPrivateObjectsConfigsGraphsIdentitiesCredentialsTracesOrBinariesCopied': False,
        'jwtOrPrivateKeyPatternMatches': 0, 'symlinkArtifacts': 0,
        'patternScanAdditionalToClosedSchemaAndOriginalSanitizationNotCompletePrivacyProof': True})
    m.save('sealed-status.json', {'sealedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'published': False, 'ordinaryAcceptance': False, 'fullDependencyGenerationCoherenceClaimed': False,
        'noNewSourceBuildHostProfileRetrySignalSpecOrGitOperations': True,
        'parentMustIndependentlyReviewBeforePublication': True})
    files = [path for path in sorted(P.rglob('*')) if path.is_file()]
    text = ''.join(m.sha(path) + '  ' + str(path.relative_to(P)) + '\n' for path in files)
    (P / 'SHA256SUMS').write_text(text)
    for line in text.splitlines():
        digest, name = line.split('  ', 1)
        assert m.sha(P / name) == digest
    for path in (*files, P / 'SHA256SUMS'):
        path.chmod(0o444)
    print(json.dumps({'packet': str(P), 'entries': len(files), 'sha256sumsSha256': m.sha(P / 'SHA256SUMS'),
        'readmeSha256': m.sha(P / 'README.md'), 'ordinaryAcceptance': False, 'published': False}))


if __name__ == '__main__':
    main()
