# Image memo read/write synchronization benchmark, 7 November 2026

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

The workload is the **66-image source-derived Folders.Server owner + DomainService + owner direct-reference cohort**, in original ordinal FullName order. It is not claimed equal to the complete runtime ExecutableOwners union, which can include discovery projection, aggregate, processor and handler owners. No application host startup, domain discovery, authority or network operation ran. The harness read the owner runtimeconfig to match GC mode; production configuration capture was not part of the workload. DomainService methods are not used to compute the selected images: each typed adapter opens the current file and calls its own selected FreshStream helper/scope. Technical66 is a separate PE/MVID stress cohort, not a claim that a real host makes 66 technical calls.

The physical cohort has 27,655,992 image bytes and 27,689,784 charged bytes; all 66 are eligible and fit the original bounds. The original bounds remain 128 entries, 16 MiB per image, 64 MiB charged retention and 512 bytes per entry. Every batch's actual retained occupancy and exact hit counts reconcile. The complete current files, full SHA results and loaded MVID identities agree before timing and remain unchanged afterward.

The oracle is actual unchanged ordinary SDK DLL `5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872`. The control copies original bodies with namespace changes; the candidate changes only memo synchronization. Warm lookup and complete byte comparison use shared read protection. Misses leave read protection and repeat all original closed/length/lookup/full-equality/count/budget checks under exclusive write protection before copying/hashing/publication. Dispose uses write protection and does not dispose the lock, preserving late closed fallback. Scope locking, full fresh stream/64 KiB/EOF behavior, pools, PE/MVID behavior and original order remain unchanged. No authority, output, grant, configuration, lease, replay or fence cache is introduced.

Typed namespace/assembly proofs plus a real small-fit SDK image verify that selected Domain/FreshStream and Technical calls actually increase that scope's hits and populate that memo. A correct label bound accidentally to the wrong SDK scope would fail. Before timing, all three variants match independent complete SHA/MVID expectations across the full cohort.

The harness and oracle prove matched unoptimized Debug. Runtime facts passed .NET 10.0.14, fourteen processors with no DOTNET_PROCESSOR_COUNT override, and ServerGC matching the actual Folders runtimeconfig. Build parallelism alone used the two-CPU setting. Each observation enters/disposes a fresh selected scope inside its dedicated reader; 1/2/4 readers share one memo per variant/sample. Only batch start is aligned; there are no per-image barriers. All six variant permutations balance position. Each fresh memo records memo-cold, warmup and warm batches. Memo-cold does not mean cold filesystem, JIT, pool or process. Parity/discovery/expected-SHA setup and JSON output are outside timing; scope creation/disposal, comparisons and fixed bookkeeping are included identically.

## Initial count-assumption failure and correction

Attempt 1 restored and built successfully with zero warnings/errors, then stopped before parity/timing at the closed `inventory-count` invariant. Runtime/Debug/GC/CPU facts had passed. The assumed 67 was a preparation error that conflated two inventories: current PE metadata has 65 owner AssemblyRefs, with DomainService already among them, so owner + Domain + refs deduplicates to **66**. Cached dotnet-inspect made 66 metadata-only queries; no application assembly was executed by that diagnosis. Every identity matched exactly one pinned physical file. Historical evidence also distinguished a 66-image subset from a 67-image runtime census with an additional image; this benchmark does not add that extra image or claim runtime completeness.

Both original source generations and receipts are retained: e69's 59 files and corrected ff43's 69 files. The exact nine-file difference updates count guards, labels/docs/output fields and a fresh retry destination. Selection, order, algorithms, byte bounds, parity logic and timing are unchanged apart from the exact expected count. No image was trimmed to satisfy a bound. Attempt 1's failure/runtime facts, exact commands/logs/input snapshots, preserved compiled-generation hashes, current metadata proof and historical provenance remain intact. The successful retry did not overwrite the failed output directory.

## Evidence and pending validation

- `source-generations/`: both immutable source/plans/pins/receipt generations, original/control/candidate bodies and source normalization review.
- `execution/`: both exact command releases, logs, before/after inputs, preservation context and supervisor sources. Compiled files are recorded by size/hash rather than copied.
- `metadata-count-diagnosis/`: current exact identity/physical-image metadata and cached-tool commands, with historical provenance.
- `failed-entry-results/`: the initial closed failure and passed runtime facts.
- `actual-results/`: original runtime/type-path proofs, parity/oracle agreement, inventory, all 819,024 original raw sample bytes and unchanged summary/result JSON.
- `independent-audit/`: executed read-only audit/source. It reproduces all 54 summary groups, exact nested raw/schema/count/hit reconciliation, all input/identity facts and process/group absence.

Root's later decision is recorded separately from original benchmark outputs, whose no-production-benefit-claim flags remain unchanged. Production implementation, native qualification and ordinary managed/browser acceptance are still required; there is no host deadline or request-root attribution and no join to earlier runtime profiles. Product sources, application outputs, configuration and Git/spec files were not changed by this benchmark or packet assembly. No application hosts, new profiles or native suite ran.

`SHA256SUMS` covers every packet artifact except itself. Root verified the original 323-artifact candidate, 311 original copies and all duplicate-checked raw rows before publication. Original candidate README/checksums and parent packet audit remain under `publication-provenance/`. Publication wording and explicit cohort/configuration-reading clarifications are the only changes to original candidate artifacts.
