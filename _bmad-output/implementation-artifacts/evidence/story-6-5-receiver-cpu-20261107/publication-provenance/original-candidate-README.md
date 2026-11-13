# Receiver process CPU/runtime-GC evidence, 7 November 2026

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
