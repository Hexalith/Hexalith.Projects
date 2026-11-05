# Story 6.1 final verification and review

Feature acceptance and all three review layers are complete. The build spec is **done**; story and sprint are **review**, as required by the Build workflow's terminal synchronization. Nothing was deferred. No staging, commit, push, branch, dependency, or checkout change was performed.

Final source is `fd22b04355f941d920ee3ecaa5d5b68bea047e7f01486df55cc33d2a30451e22` over **17,083 inputs**. [Final input manifest](final-checks/source-manifest.json), [all recorded repository heads](../completion/source-heads.json), [current owner patches and reverse checks](source-changes/patch-checks.json), and [independent runtime validation](../qualification-6/parent-runtime-validation.json) bind acceptance to the actual source. The original spec baseline remains `0047c961ccad19f1a7344095ecb70bcd8e4bb65b`. All thirteen dependency/workspace heads are unchanged from the implementation handoff.

The approved fixed platform maintenance route requires its exact human permission and explicit scope, GlobalAdministrator authority and attributable operator; it reconciles only the retained administrator technical checkpoint/work state. The shared SDK isolates authenticated native source reads from human submission transport. Review corrections qualify this native enrollment internally, preserve ordinary legacy readers and fallback behavior, refuse malformed proof keys, document the operator route, and test delayed SDK results after cancellation/expiry. Production authority, event history, grants and proof checks were not broadened.

## Verification

[Final check commands, exact environment and totals](final-checks/result.json), [fresh native command/environment/output digest](native-6.command.json), and [complete native output](native-6.txt) retain the executed commands. All final relevant Debug builds have zero warnings/errors. Test projects ran individually; no solution-level test command was used.

| Check | Result | Native evidence |
| --- | --- | --- |
| Fresh duplicate provider | 1/1 | [XML](../qualification-6/duplicate/distributed.xml), [state/replay receipt](../qualification-6/duplicate/projection-replay.json) |
| Fresh full-replay provider | 1/1 | [XML](../qualification-6/full-replay/distributed.xml), [original-source/rebuild/state receipt](../qualification-6/full-replay/projection-replay.json) |
| Fresh main provider, nine scenarios | 1/1 | [XML](../qualification-6/main/distributed.xml), [matrix](../qualification-6/main/matrix.json) |
| Full runtime Projects Integration | 222/222 | [XML](../qualification-6/main/integration.xml) |
| Full runtime Projects Server | 1,378/1,378 | [XML](../qualification-6/main/server.xml) |
| Full Folders, ordinary execution policy | 1,424/1,424 | [Command](final-checks/folders-default-full.command.json), [XML](final-checks/folders-default-full.xml) |
| Full SDK DomainService | 1,381 pass, one existing opt-in skip | [Command](final-checks/sdk-full.command.json), [XML](final-checks/sdk-full.xml) |
| Selected owner-source helper | 34/34 | [Command](final-checks/helper-full.command.json), [XML](final-checks/helper-full.xml) |
| Launcher controls | 122/122 | [Command](final-checks/launcher-controls.command.json), [output](final-checks/launcher-controls.txt) |
| Focused corrected SDK admission/composition | 59/59 | [Correction results](../review-fixes/results.json) |

All eleven distributed scenarios pass across three fresh independent providers at the same frozen source. All feature/native/Projects/Folders/helper totals have zero failures, errors, skips and not-run cases. The full SDK's sole skip is the pre-existing `ContentPersistenceQualificationTests.PersistedBytesSurviveSidecarProcessReplacementAndRejectCorruption`, which explicitly requires `EVENTSTORE_CONTENT_LIVE=1`; it was not relabeled as passing. Broader checks, native startup and each provider ran sequentially.

## Seven-criterion acceptance map

| Criterion | Final evidence |
| --- | --- |
| AC1 supported authorized list/open | The [main matrix](../qualification-6/main/matrix.json) verifies real authenticated gateway → Projects → Folders reads, Tenant/actor authority, current Folder filtering before paging and supported continuation. Full Server tests cover ordering, page limits, query validation, AD-32 snapshots and singleton open. |
| AC2 indistinguishable safe denial | Current Server XML embeds the passing real-owner and SDK denial observations: identical safe status/body/headers/log category, no exported service-chain span and the approved 50 ms minimum. [Current telemetry audit](../qualification-6/parent-telemetry-final-validation.json) and [console audit](../qualification-6/parent-console-validation.json) verify the original fifteen traces/windows with positive collection controls. All three providers retain actual distributed timing samples. |
| AC3 honest response state | Main native persisted metadata/outage/authority cases and full Server regressions verify Partial/Unavailable, component freshness, blocked context use, archived behavior and fail-closed authority. No protected snapshot substitutes for missing authorization. |
| AC4 additive contracts and retained routing | Full Server/Integration regressions cover historical/additive event classification, serialization, replay, retained legacy comparison and unsupported/corrupt history refusal. Public routing is retained. Exhaustive owner/gateway migration equivalence and reversible cutover remain Story 6.7 obligations. |
| AC5 response completeness and safe fields | Full Server tests verify metadata allowlists, safe defaults, descriptive inspection permission and durable audit, typed setup/reference summaries, quarantine/Archived rules, excluded source/sibling content and audit-storage failure refusal. Actual main native list/open/inspection passes. |
| AC6 read-only/additive compatibility | Each native matrix verifies persisted domain bytes/ETags and watermarks. The main receipt confirms unchanged domain state through reads and restart; replay/duplicate receipts distinguish authorized projection work from reads. Maintenance controls separately preserve original source/sequence and limit writes to technical checkpoint/work records. Security/inspection audit remains metadata-only and required. |
| AC7 persisted authenticated proof | [Fresh aggregate](../qualification-6/run.json), [all eleven cases](../qualification-6/matrix.json) and current XML prove replay/duplicates, actual Projects restart, continuation after restart, a cursor valid immediately before revocation then rejected within lifetime, cross-Tenant/absent denials and current authority. Exact commands, heads, manifests, source patches and cleanup are retained. |

AC2 accepts the previously approved 50 ms gateway floor, aggregate metric operation-count inference and bounded observed-window proof. Slower/fault variation remains; no constant-time, load-wide or universal infrastructure-fault equality is claimed. The collector's original `privacyEqualityAccepted=false` is preserved: coverage collection does not make the acceptance decision. Parent acceptance uses the combined current evidence above.

## Privacy and cleanup

[Exact audit commands and script/result hashes](final-audit-commands.json) retain executable runtime and privacy checks plus exact private cleanup. The stopped current dashboard uniquely matches all six original native OTEL service/instance pairs. All fifteen protected traces have zero exported spans, linked logs or metric exemplars; fourteen positive protected POST metric dimensions have zero exemplars. Positive controls include **60,131 spans**, **39,074 logs**, **29,548 exemplars**, native exports for all six peers, and **1,373 ordinary routed server spans**. The audit retains sanitized binding rows and per-resource positive log/exemplar controls. Database metadata remains unchanged.

All six raw console files independently match original UID/mode/device/inode/hash and are silent throughout each of the fifteen recorded windows. Only those exact files and their original private directory were removed; raw console bytes were never copied into this packet. Independent runtime verification confirms all twelve captured containers, eighteen original sidecars and original AppHosts absent. Original provider receipts also verify private material removal and pre-existing resource preservation. [Review mapping cleanup](review-mapping-cleanup.json) verifies exact original custody, no symlinks or matching processes, then removes only that mapping.

[Retained-output mapping](../retained-output-manifest.json) now contains **67** byte-identical, checkout-visible copies and preserves every prior entry. Packet-local raw-artifact attributes retain native bytes. Earlier source bundles, failed builds/runs, placement recovery, Task36 failures and grant corrections remain separate historical evidence.

## Review disposition and remaining obligations

Blind Hunter, Edge Case Hunter and Verification Gap all ran at the same model capability; none was skipped. Every finding has its own verdict in the [build spec](../../../spec-6-1-list-and-open-projects-through-supported-authenticated-paths-13.md). Six correction groups are resolved: native enrollment compatibility, malformed keys, operator guidance, ordinary-policy Folders verification, positive telemetry-channel evidence and post-dispatch cancellation/expiry controls. The repeated private-custody finding retains its prior rejection; no issue was deferred and frozen human intent did not change.

The historical Release/package NU1109 conflict remains separate and unrerun: Tenants.Client 7.0.0 requires EventStore Client/Contracts >=4.0.4 while Projects pins 3.133.0. The approved Epic 6 E1–E7 entry contract accepts the recorded local source proof for this feature. No published-package, production-enrollment, global READY, Story 6.7 equivalence or Epic 8 release acceptance is inferred. The approved prohibition on Git mutations takes precedence over Build's default local-commit instruction; no commit was created.
