# Canonical hash memo: native and compiled component qualification

This packet qualifies the narrow production implementation and its measured component benefit. Ordinary managed readiness, browser, persisted-data, privacy and accessibility acceptance remain pending for this change. It makes no complete-repair, host-deadline or request-latency claim.

The change preserves every original validator, normalization and single `SerializeToElement` call. Only the subsequent pure canonical writer and SHA result may be reused, keyed by the complete freshly owned serialized JSON text with ordinal equality. A miss uses that same captured immutable element. The separate authority-owned memo retains at most512 entries within4MiB of charged UTF-16 keys/digests plus fixed entry allowances; the charge is not measured CLR heap size. The existing8MiB/three-entry validated-byte memo is unchanged. Existing scope APIs and option observation order remain, the two original host-enabled entry sites borrow the new memo, and it closes before the unchanged original disposal sequence. No raw `ObservedRouteCatalogHost.Hash` extension or authority/configuration/result cache was added.

The packet contains all nine production/test source files, exact before/after snapshots, the production diff and independent source review. The23 new native cases cover full keys, schema/scalar/descriptor mutation, error precedence, one serializer snapshot, bounds, scopes/owners/closure, concurrency and both actual authority entry sites.

| Final native execution | Passes |
| --- | ---: |
| EventStore owner: retained422 +23 new canonical cases +5 unchanged byte-memo cases |450|
| Projects Contracts |211|
| Projects Server |324|
| Projects UI |46|
| Projects Integration |45|
| Total |1076|

The exact five final XML files contain1076 Pass executions with no test failure, skip or assembly error. The separate earlier23+5 smoke executions are not added to that total. Benchmark parity checks are also not native cases. Source65, commands, current producer/copy generations, separate test outputs and the independent native audit are preserved. Earlier10 cached-dependency provenance limits and17 historical generation differences remain explicit; current outputs are not claimed byte-identical to historical1048 binaries. Binaries and native stdout are excluded; original artifact hashes and commands remain in the receipts.

The first scratch compilation failed on36 namespace/alias shadow errors before any benchmark run. Root preserved the failed source/receipt and corrected only standalone `Control`/`Candidate` aliases to `ControlCodec`/`CandidateCodec`; the subsequent build and run passed. This packet retains those source generations, the closed failed-command receipt and a bounded compiler-diagnostic projection with the original log hash. It does not copy raw stdout.

| Whole-operation benchmark | Parity checks | Raw batches | Timed observations |
| --- | ---: | ---: | ---: |
| Actual previous compiled baseline / exact copied control / prototype |178|216|16380|
| Actual new compiled Contracts+SDK / unchanged old control / unchanged prototype |178|216|16380|

Both runs use the same two bounded datasets: the unchanged public catalog fixture (30 declarations,17 admission policies and their descriptor schemas), and actual compiled Sample owner declarations/operation schemas (14 documents). These are component datasets, not a claim to reproduce complete live host inventories. Each observation enters and closes a fresh scope and includes fresh outer capture, original nested cloning, validation, normalization and serialization. Inventory and Identity are measured separately. Warm batches use64 observations per reader and cold batches one per reader, with1/2/4 readers, one shared owner per variant, and three cyclic variant orders. Cold means a new memo owner; it does not mean cold runtime, JIT or files, and simultaneous first readers can reuse another reader's newly published entry.

For the final actual compiled production path, all12 warm group median wall ratios versus the unchanged old control are0.407–0.574; allocation ratios are0.354–0.512 and observation P95 wall ratios0.319–0.637. Per-round wall wins are35/36, not every round: compiled-Sample Identity with one reader in round2 was1.448 times the control and3.179 times the copied prototype. Production/prototype warm median wall ratios range0.858–1.307, so production is not claimed uniformly faster than the prototype.

Cold wall ratios versus control are mixed (0.752–1.418), with roughly8–9% extra single-reader cold allocation. Only six of twelve final warm cells had all variants above the process-CPU granularity guard; no CPU ratio is claimed for sub30ms batches. The initial scratch comparison also retains its individual slower round and cold costs in the independent audit.

The final production variant uses the actual authority-selected scope through a typed delegate bound once before timing. Actual scope creation retains its three option observations; this is a source/native-test fact, not a dynamic benchmark counter. Reflection does not occur per observation. The owner constructor and disposal are outside the observation clock and separately recorded in final raw samples. Constructor timing includes benchmark reflection/delegate binding and its scope probe, and therefore is not production DI-startup cost. The actual owner has three original memos plus the new memo, whereas the control owns one byte memo and the copied prototype owns two memos. That ownership difference is disclosed rather than normalized away.

Both executions used .NET10.0.14, natural14 processors, Server GC and unoptimized Debug, with no forced GC or new runtime environment setting. The final typed-owner proof verifies actual participation by the new compiled Contracts and SDK. Exact producer hashes are recorded in `native/final/completed-receipt.json` and `final/build/native-handoff.json` (Contracts `f1cf82fe…`, SDK `873dbb74…`). Independent audits reconcile all raw schemas, counters, ordering, summary groups, input/artifact hashes and process-group absence. The final audit binds1055 current input paths,63 compiled artifacts and44 prepared files; these are binding-map counts, not an additive unique-file claim.

`raw-batches.jsonl.gz` files preserve each original byte stream. `packet/qualification-summary.json` records original/decompressed hashes, byte counts and cardinalities. The two archives contain432 strict batch rows and32760 nested timed observations combined. No keys, runtime configuration, private identity, graph, payload, raw trace, ETLX, binary or native stdout is included. The rejected stream-comparison experiment is separate already-published evidence and is not duplicated here.

`artifact-manifest.json` identifies every included artifact and source hash; `SHA256SUMS` covers all packet files except itself. The assembler only writes a fresh TEMP directory. Root reviews and publishes this packet separately; acceptance evidence is deliberately outside this packet.
