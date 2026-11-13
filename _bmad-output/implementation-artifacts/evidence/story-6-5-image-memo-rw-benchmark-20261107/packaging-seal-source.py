from pathlib import Path
import datetime, hashlib, importlib.util, json, re

spec = importlib.util.spec_from_file_location('memo_package', '/tmp/story65-image-memo-rw-package.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
P = m.PACKET


def main():
    assert P.exists() and not (P / 'SHA256SUMS').exists()
    independent = P / 'independent-audit/independent-benchmark-audit.json'
    assert m.sha(independent) == '7ef97a37b865fcf666329bddda32fdd60b8cd4afffad73bee45bf6cb77f5b1b1'
    assert m.sha(P / 'independent-audit/audit.py') == '3cae1ff7af4ce6525d507b6b2ac7554e6a4027c7df3eb0cb16f380325eab4a75'
    audit = m.strict(m.read(independent))
    assert audit['passed'] is True
    for name in ('story65-image-memo-rw-run-20261107.py', 'story65-image-memo-rw-run-retry-20261107.py'):
        m.copy(Path('/tmp') / name, Path('execution/supervisor-sources') / name)
    decision = {
        'recordedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'source': 'Root explicit post-audit decision in this task',
        'decision': 'supported for narrow implementation and native plus ordinary live validation; pending',
        'warmConcurrentComponentBenefitQualifiedAndRelevant': True,
        'productionQualificationSupported': True,
        'productionImplementationPerformedByThisPacket': False,
        'completedRepairOrHostDeadlineBenefitClaimed': False,
        'storyFeatureAcceptanceClaimed': False,
        'support': 'Warm Domain 2/4-reader wall wins in all six balanced rounds against both actual SDK oracle and copied control, with process CPU benefit; 620 safety checks and full input preservation passed.',
        'retainedLimits': ['single-reader Domain paired wall median +4.52% vs oracle; worse in five of six rounds',
            'Technical memo-cold four-reader process CPU +10.43% to +34.35% vs control across all six rounds',
            'Technical two-reader p95 mixed; no uniform tail claim',
            'one process/session, six sequential balanced rounds, warm filesystem/JIT/pools',
            '66 source-derived direct-reference cohort; not full host/catalog capture or distributed deadline'],
        'nextRequiredWork': 'Root-owned narrow production implementation qualification, native checks and ordinary managed/browser acceptance',
        'ordinaryNativeUniqueQualificationRemains': 1038,
        'parity620AreChecksNotAdditionalNativeCases': True,
    }
    m.save('root-decision-and-pending-validation.json', decision)
    readme = '''# Image memo read/write synchronization benchmark, 7 November 2026

Root's decision is **supported for narrow implementation and native plus ordinary live validation; pending**. The measured warm concurrent computation benefit is qualified and relevant. This packet does not record a production implementation, completed repair, whole-host deadline improvement or feature acceptance.

The successful standalone retry passed 620 behavioral/bounds/lifecycle checks and completed all 324 raw batches, 4,032 observations and 266,112 selected image calls. All 658 distinct frozen inputs matched before, after and at independent audit. The three restore/build/run processes and groups were absent afterward; all steps exited 0, and the build had zero warnings/errors. The 620 are checks, not extra product native cases. The prior ordinary 1,038-test qualification is unchanged and was not rerun here.

## Measured component result and tradeoffs

Warm two- and four-reader whole-cohort batch wall improved against both the actual compiled SDK oracle and namespace-copied unchanged control in all six balanced rounds, for both Domain and Technical paths. Warm Domain observation median and per-batch p95 improved against both baselines in all six two-/four-reader rounds, with a process CPU benefit.

The table shows original summary medians for the warm Domain cohort. Observation wall is a measured complete observation; process CPU is whole batch CPU, with twelve observations per reader. Batch wall divided by total observations would be amortized wall/throughput, not per-request latency.

| Readers | Variant | Observation wall median (ms) | Batch process CPU median (ms) |
| ---: | --- | ---: | ---: |
| 2 | compiled SDK oracle | 6.691 | 146.480 |
| 2 | copied control | 7.048 | 151.710 |
| 2 | candidate | 5.039 | 122.162 |
| 4 | compiled SDK oracle | 9.322 | 274.148 |
| 4 | copied control | 9.341 | 275.011 |
| 4 | candidate | 5.252 | 252.908 |

Known costs remain. Warm one-reader Domain batch wall was worse versus the oracle in five of six rounds, with paired median +4.52%; the control comparison was smaller and mixed. Technical memo-cold four-reader process CPU was higher than control in every round, paired +10.43% to +34.35%. Technical two-reader p95 was mixed, so there is no uniform tail improvement. Warm four-reader Domain allocation reduction was observed, but no mechanism or general memory benefit is established. These findings and all raw samples remain in the packet.

Control/oracle output identities agree exactly. Warm timing medians differ by up to about 5.4%, and cold/CPU dispersion can be larger; performance equivalence is not asserted. The six rounds are sequential observations within one process/session, not independent randomized repetitions or confidence intervals. GC occurred naturally and differed between batches; there was no forced GC or fresh process per sample. Process CPU/allocation include whole-process activity and release/join bookkeeping.

## Exact workload and selected implementation proof

The workload is the **66-image source-derived Folders.Server owner + DomainService + owner direct-reference cohort**, in original ordinal FullName order. It is not claimed equal to the complete runtime ExecutableOwners union, which can include discovery projection, aggregate, processor and handler owners. No host/discovery/configuration/authority/network operation ran. DomainService methods are not used to compute the selected images: each typed adapter opens the current file and calls its own selected FreshStream helper/scope. Technical66 is a separate PE/MVID stress cohort, not a claim that a real host makes 66 technical calls.

The physical cohort has 27,655,992 image bytes and 27,689,784 charged bytes; all 66 are eligible and fit the original bounds. The original bounds remain 128 entries, 16 MiB per image, 64 MiB charged retention and 512 bytes per entry. Every batch's actual retained occupancy and exact hit counts reconcile. The complete current files, full SHA results and loaded MVID identities agree before timing and remain unchanged afterward.

The oracle is actual unchanged ordinary SDK DLL `5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872`. The control copies original bodies with namespace changes; the candidate changes only memo synchronization. Warm lookup and complete byte comparison use shared read protection. Misses leave read protection and repeat all original closed/length/lookup/full-equality/count/budget checks under exclusive write protection before copying/hashing/publication. Dispose uses write protection and does not dispose the lock, preserving late closed fallback. Scope locking, full fresh stream/64 KiB/EOF behavior, pools, PE/MVID behavior and original order remain unchanged. No authority, output, grant, configuration, lease, replay or fence cache is introduced.

Typed namespace/assembly proofs plus a real small-fit SDK image verify that selected Domain/FreshStream and Technical calls actually increase that scope's hits and populate that memo. A correct label bound accidentally to the wrong SDK scope would fail. Before timing, all three variants match independent complete SHA/MVID expectations across the full cohort.

The harness and oracle prove matched unoptimized Debug. Runtime facts passed .NET 10.0.14, fourteen processors with no DOTNET_PROCESSOR_COUNT override, and ServerGC matching the actual Folders runtimeconfig. Build parallelism alone used the two-CPU setting. Each observation enters/disposes a fresh selected scope inside its dedicated reader; 1/2/4 readers share one memo per variant/sample. Only batch start is aligned; there are no per-image barriers. All six variant permutations balance position. Each fresh memo records memo-cold, warmup and warm batches. Memo-cold does not mean cold filesystem, JIT, pool or process. Parity/discovery/expected-SHA setup and JSON output are outside timing; scope creation/disposal, comparisons and fixed bookkeeping are included identically.

## Initial count-assumption failure and correction

Attempt 1 restored and built successfully with zero warnings/errors, then stopped before parity/timing at the closed `inventory-count` invariant. Runtime/Debug/GC/CPU facts had passed. The assumed 67 was stale for this source-derived subset: current PE metadata has 65 owner AssemblyRefs, with DomainService already among them, so owner + Domain + refs deduplicates to **66**. Cached dotnet-inspect made 66 metadata-only queries; no application assembly was executed by that diagnosis. Every identity matched exactly one pinned physical file. Historical evidence also distinguished a 66-image subset from a 67-image runtime census with an additional image; this benchmark does not add that extra image or claim runtime completeness.

Both original source generations and receipts are retained: e69's 59 files and corrected ff43's 69 files. The exact nine-file difference updates count guards, labels/docs/output fields and a fresh retry destination. Selection, order, algorithms, byte bounds, parity logic and timing are unchanged apart from the exact expected count. No image was trimmed to satisfy a bound. Attempt 1's failure/runtime facts, exact commands/logs/input snapshots, preserved compiled-generation hashes, current metadata proof and historical provenance remain intact. The successful retry did not overwrite the failed output directory.

## Evidence and pending validation

- `source-generations/`: both immutable source/plans/pins/receipt generations, original/control/candidate bodies and source normalization review.
- `execution/`: both exact command releases, logs, before/after inputs, preservation context and supervisor sources. Compiled files are recorded by size/hash rather than copied.
- `metadata-count-diagnosis/`: current exact identity/physical-image metadata and cached-tool commands, with historical provenance.
- `failed-entry-results/`: the initial closed failure and passed runtime facts.
- `actual-results/`: original runtime/type-path proofs, parity/oracle agreement, inventory, all 819,024 original raw sample bytes and unchanged summary/result JSON.
- `independent-audit/`: executed read-only audit/source. It reproduces all 54 summary groups, exact nested raw/schema/count/hit reconciliation, all input/identity facts and process/group absence.

Root's later decision is recorded separately from original benchmark outputs, whose no-production-benefit-claim flags remain unchanged. Production implementation, native qualification and ordinary managed/browser acceptance are still required; there is no host deadline or request-root attribution and no join to earlier runtime profiles. Product sources, application outputs, configuration and Git/spec files were not changed by this benchmark or packet assembly. No application hosts, new profiles or native suite ran.

`SHA256SUMS` covers every packet artifact except itself. This is a temporary sealed candidate; publication and spec changes remain Root-owned.
'''
    (P / 'README.md').write_text(readme)
    m.copy(__file__, 'packaging-seal-source.py')
    m.save('final-added-original-provenance.json', m.COPIED)
    original = m.strict(m.read(P / 'byte-copy-provenance.json'))
    for item in original:
        assert m.sha(P / item['packetPath']) == item['sha256'] == m.sha(item['source'])
    assert m.sha(P / 'actual-results/benchmark-samples.jsonl') == m.sha(m.RESULTS / 'benchmark-samples.jsonl')
    bad = []
    count = 0
    for path in P.rglob('*'):
        if path.is_dir():
            continue
        assert not path.is_symlink() and path.suffix.lower() not in {'.dll', '.pdb', '.exe', '.cache', '.pyc', '.nettrace', '.etlx'}
        raw = m.read(path)
        count += 1
        if re.search(rb'eyJ[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}', raw):
            bad.append(path.name)
        if re.search(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----\s+[A-Za-z0-9+/=]{24,}', raw):
            bad.append(path.name)
    assert not bad
    m.save('privacy-and-artifact-scan.json', {'scannedBeforeThisReceipt': count,
        'jwtOrPrivateKeyPatternMatches': 0, 'symlinks': 0, 'compiledBinaryCacheTraceEtlxContentsCopied': False,
        'onlySourcePublicExecutableMetadataClosedChecksAndNumericMeasurementsRetained': True,
        'patternScanIsAdditionalCheckNotCompletePrivacyProof': True})
    m.save('sealed-status.json', {'sealedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'independentAuditBound': True, 'rootDecisionBound': True, 'published': False,
        'productionImplementationOrNativeLiveQualificationCompleted': False,
        'rootMustReviewBeforePublicationOrNewProductWork': True,
        'noPackagingAppBuildBenchmarkRerunHostSourceSpecOrGitOperations': True})
    files = [path for path in sorted(P.rglob('*')) if path.is_file()]
    sums = ''.join(m.sha(path) + '  ' + str(path.relative_to(P)) + '\n' for path in files)
    (P / 'SHA256SUMS').write_text(sums)
    for line in sums.splitlines():
        digest, name = line.split('  ', 1)
        assert m.sha(P / name) == digest
    for path in (*files, P / 'SHA256SUMS'):
        path.chmod(0o444)
    print(json.dumps({'packet': str(P), 'checksumEntries': len(files),
        'sha256sumsSha256': m.sha(P / 'SHA256SUMS'), 'readmeSha256': m.sha(P / 'README.md'),
        'rawBatches': 324, 'observations': 4032, 'imageCalls': 266112, 'checks': 620, 'sealed': True, 'published': False}))


if __name__ == '__main__':
    main()
