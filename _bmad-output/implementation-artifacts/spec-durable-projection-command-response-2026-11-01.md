---
title: 'Return committed command acceptance without awaiting durable projection replay'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
baseline_commit: 'fa978a283b29deb05a9c988ff54e3299de9606fc'
review_loop_iteration: 1
context: []
---

<frozen-after-approval reason="existing user authorization for all actual CI repairs and bypass of human validation">

## Intent

Fix the demonstrated ordinary command-response timeout by using the already durable projection activation and independently owned activation worker. Return the existing real command result after aggregate acceptance and existing response obligations; leave eventual projection delivery to that worker only when its actual ownership is positively established. This is one response-liveness correction, not a new projection engine.

## Boundaries & Constraints

Preserve write-ahead activation BEFORE aggregate routing, real actor results, status/archive/identity checks, protected admission/fencing/recovery, all auth/catalog/receipt/refusal rules, cancellation, lease ownership, and delivery/cleanup correctness. Preserve synchronous projection behavior when workers are explicitly disabled, DaprClient is absent, the durable outbox is absent, or the positive worker capability is absent. No new request-detached tasks, fake acceptance/proof, shared lease reuse, timeout/retry increases, gate removal, or bootstrap-only special case. Existing independently hosted worker acquires its own catalog authority through its normal production delivery path. No local live backend, Docker changes, nested initialization or Git operations by implementation agent. Parent owns Git, root pointers and hosted qualification.

## I/O & Edge-Case Matrix

| State | Expected behavior |
|---|---|
| Actual activation worker capability and durable outbox; actor accepts events | Ensure activation before routing, return the actual command result without invoking inline full replay; existing worker later owns delivery |
| Activation persistence fails/cancels | Routing never starts; original failure/cancellation survives |
| Capability absent, outbox absent, workers disabled or Dapr absent | Existing awaited projection fallback remains, including request lease lifetime |
| Rejected/no-event/protected replay command | Existing result/refusal, admission and recovery semantics remain |
| Worker delivery fails/restarts | Durable activation remains recoverable through existing defer/retry/conditional cleanup; no invented completion |

</frozen-after-approval>

## Code Map

- Owner: `references/Hexalith.EventStore`. Read its AGENTS, permitted enclosing LLM/Git/state baselines, build/config/test guidance. Never load a skill from any references directory. All nested checkouts remain absent; source references use sibling roots.
- `src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs`: mandatory outbox.EnsureAsync around385 before router; accepted EventCount>0 inline TriggerProjectionUpdateAsync around615 awaits full replay with None, then activity/stream trackers use original canceled token. Public DI constructor and internal friend constructor are existing composition seams; append compatible optional injection if needed. Keep diagnostics meaningful and unchanged on fallback.
- `src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs`: durable outbox registered164; WorkersEnabled defaulttrue gate268; actual ProjectionActivationWorker276 only when DaprClient exists, otherwise NoOpHostedService. Recovery comment explicitly preserves request projection when workers disabled. Register/use a small positive actual-worker capability without enumerating all IHostedService instances or introducing DI cycles; configuration alone is insufficient. Keep scope narrow and avoid broad public API expansion.
- `src/Hexalith.EventStore.Server/Projections/ProjectionActivationWorker.cs`: independently hosted existing outbox scanner; real source guard and conditional complete/defer; fallback UpdateProjectionAsync66. Do not rewrite this protocol.
- `src/Hexalith.EventStore.Server/Projections/DaprProjectionActivationOutbox.cs`: EnsureAsync advances the durable revision; CompleteAsync only clears its sampled revision. A worker can clear the BEFORE-route activation after observing the old source head while the new actor mutation is in flight. For worker-backed accepted EventCount>0 commands, re-arm the activation durably AFTER routing and BEFORE ACK; preserve the mandatory pre-route write-ahead as well. Do not ACK if this required post-commit write fails/cancels. The new revision prevents a previously sampled worker cleanup from erasing the new-head activation.
- `src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs`: Update/Deliver independently acquire catalog leases141–166. Do not optimize away validation or change routing/authority.
- Existing regressions: `tests/Hexalith.EventStore.Server.Tests/Pipeline/SubmitCommandHandlerTests.cs`, `SubmitCommandHandlerIdempotencyAdmissionTests.cs`, `SubmitCommandHandlerResultPayloadTests.cs`; `Configuration/EventStoreServerServiceCollectionExtensionsTests.cs` pins worker enable/disable/non-Dapr composition; `Projections/ProjectionActivationWorkerTests.cs` exercises real persisted source/outbox cleanup. New diagnostic tests under this project also pin original fallback lease/cancellation behavior.
- Update concise existing owning projection/command documentation only if necessary to explain the durable worker/fallback contract.

## Tasks & Acceptance

- [x] Add minimal positive actual-worker ownership composition and gate only inline projection replay; preserve backward-compatible constructor composition and synchronous fallbacks.
- [x] For deferred accepted event-bearing commands, durably re-arm after actor success before returning, retaining the pre-route Ensure. Test the concrete worker-clears-old-head-activation-during-routing race and stale sampled cleanup refusal after re-arm, with actual outbox persistence and later worker delivery.
- [x] Preserve a durable shard revision high-water mark across activation removal/recreation; seed legacy ledgers from live revisions and allocate checked revisions so stale sampled cleanup cannot erase a re-armed activation (ABA).
- [x] Add meaningful production-seam tests for the matrix, including a held full replay proving worker-backed response returns before replay, exact real result, durable activation ordering/failure, worker-disabled/non-Dapr fallback, and actual durable worker delivery/recovery. Use existing production outbox/backend harnesses where useful; no live resources.
- [x] Run only relevant owning builds/tests; report exact commands/results and changed files. Parent runs broader focused regressions and three-layer review, then actual hosted qualification.

Given a worker-backed accepted command, when full projection replay would exceed the unchanged Dapr5second attempt, then replay is not executed within that HTTP request and its durable activation remains owned by the existing worker. Given missing ownership, when projection is held, then the original awaited request and lease remain pending. Given a failed activation write, when handling a command, then no actor route occurs. Given the normal worker later runs, then existing persisted projection and conditional activation cleanup checks pass independently of the completed HTTP scope.

## Implementation Notes

Implementation inspection found an ABA cleanup race: the existing outbox recreates a removed activation at revision1, allowing another worker holding the old revision1 to erase the post-commit activation. Retain a monotonic high-water revision in the existing bounded shard ledgers, including empty ledgers; legacy live revisions seed the counter. This supports the required re-arm boundary without changing frozen legacy-delivery comparisons.

Definitive hosted evidence: Tenants source run41353744654 at7c7fe7f47d7970607e37ca5dfb741a3950d10753 with EventStorefa978a283b29deb05a9c988ff54e3299de9606fc. Full raw log `/tmp/projects-release-maintenance/tenants-7c7-source-runtime-failed.log`; bounded parsed1190 records `/tmp/projects-release-maintenance/tenants-7c7-stage-records.json`. Warm ActorRpc421–613ms and RouterRelease7–12ms complete. Three Submit attempts wait ProjectionDelivery11787/11114/13612ms (operation token None), then ActivityTracking fails on canceled original request. Caller30second timeout21:56:17.119; no202 exists. This establishes the functional response boundary, unlike earlier speculation. Root latest-main refresh found all12 clean modules equal freshly fetched main and no nested checkouts. There are no missing intent decisions; parent remote/release operations already explicitly authorized.

## Review Triage Log

- Blind1 — high, bad_spec: accepted unknown-outcome reconciliation returns before the new post-acceptance activation path. If re-arm failed after old-head cleanup, reconciliation can acknowledge without durable delivery. Require the same independent durable activation before returning a reconciled accepted event-bearing result.
- Blind2 — medium, bad_spec: old writers can drop LastRevision from the shared v1 ledger; the documented limitation does not prevent unqualified default adoption. Keep worker-backed responses opt-in, enabled only in the explicitly qualified fresh same-source cohort, and retain existing inline defaults elsewhere. This is a behavior feature flag, not fabricated writer authority; mixed-writer/rollback qualification is not claimed.
- Blind3 — high, bad_spec: the real worker source-guard shortcut does not execute a separate legacy output. Its complete committed method plan must be delivered before activation can be cleared; add actual mixed-method persisted end-state and refusal coverage.
- Blind4 — high, bad_spec: per-call transport leases do not capture one generation across the worker's multi-call source/model/state/cleanup attempt. Acquire and retain an independent complete worker lease; test held work and generation transitions.
- Blind5 — medium, patch subsumed by re-derivation: a DI-created capability can survive missing/unstarted/stopped hosted registration. Eligibility must reflect actual hosted lifetime; test these states and preserve synchronous fallback.
- Blind6 — medium, patch subsumed by re-derivation: appending an optional public constructor argument removes the original CLR signature. Restore its exact forwarding signature and verify a previously compiled caller can bind it, while keeping DI selection unambiguous.
- Blind7 — medium, bad_spec: one indefinite call can block the sequential activation worker and all newly deferred work. Use bounded independently owned work slots under the hosted lifetime so a held aggregate cannot prevent another aggregate or future scans progressing; no arbitrary timeout increase or abandoned lease/task.
- Blind8 — medium, patch subsumed by re-derivation: silent scan/delivery/defer failures now conceal failures of the acknowledged path. Add closed structured failure and recovery signals with repeated-scan failure coverage; a separate health API is not required to resolve the missing observable signal.
- Edge1 — high, bad_spec: independently reported the mixed named/legacy omission; preserve this separate finding and require all committed outputs before cleanup.
- Edge2 — medium, bad_spec: cancellation of post-commit re-arm after old-head cleanup can remove the delivery path where the former None-token inline replay would continue. Use independently owned post-commit activation cleanup, preserve caller cancellation for the response, and require durable restoration on reconciliation. Never invent an ACK on write failure.
- Edge3 — false as a code defect: its claim-only mismatch is the historical matrix's imprecise activation wording. Tests explicitly prove pre-route failure routes zero times and post-commit failure routes once and refuses acknowledgement; the revised implementation tasks distinguish both boundaries. No code change can make a post-commit write precede its already completed actor call.
- Verification-gap reviewer — no gaps in the reviewed candidate's declared tests; the production-path assumptions above require additional coverage during re-derivation.

## Verification

Use existing Debug/source Server.Tests build with global EventStore producer identity3.131.0, serial MSBuild and no nested initialization. Execute built xUnit classes directly when appropriate so unrelated shared outputs are not rebuilt concurrently. Retain exact totals and zero-skip evidence. No local topology startup.

## Local implementation verification

Final serial Debug/source Server.Tests build passed with zero warnings/errors. The seven-class focused run passed98 tests with zero failures/errors/skips/not-run, including production-DI ownership/fallbacks and actual persisted full-replay cleanup/re-arm/restart cases. Parent independent verification and hosted qualification remain pending. The same-cohort counter is backward-readable, but old writers can discard it; mixed writers and rollback are explicitly unqualified in owning lifecycle documentation.

## Spec Change Log

Review loop1 corrects the plan's assumption that the existing activation worker already preserves the complete committed method plan and one authority lifetime. The initial candidate passes98 focused and524 parent regressions, but those fixtures do not cover the omitted mixed-output route or whole-attempt authority. Re-derive before pushing. KEEP the positive exact-worker alias, mandatory pre-route write, independently restored post-acceptance work, all conditional stale-cleanup/ABA fencing, actual persisted single-event/full-replay recovery tests, original returned results, original fallback lease/cancellation behavior, protected recovery and all automatic CI assertions. Preserve the separate parent synthetic metadata sentinel correction. Add bounded hosted ownership/progress, compatibility and explicit current-cohort opt-in; do not silently default unknown old deployments into this behavior.

## Re-derived implementation requirements (loop1)

The parent already invoked and rendered bmad-build; this handoff is its step03 implementation. Do not invoke/render/orchestrate that skill again. Implement directly from this spec after owner guidance.

The parent reverted only the first candidate's owned code/test/doc changes; the separate diagnostic metadata sentinel correction remains. Positive source/test material is archived at `/tmp/durable-projection-loop1-preserved/` for KEEP constraints, and the reviewed patch is `/tmp/durable-projection-response-review.diff`. Read actual baseline source before re-deriving; the archived candidate's worker assumptions are known-bad.

- Make worker-backed responses an explicit opt-in, disabled by default, for a qualified current writer cohort. Enable it automatically ONLY in Tenants' existing fresh same-source owned catalog profile (`references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/TenantsDevelopmentRoutingCatalog.cs` guarded source-profile Gateway environment configuration). This configuration switch enables behavior; it is not evidence that an arbitrary store has no old writers. Document the real fresh-cohort prerequisite and retain inline behavior when absent. Use already loaded allowed Aspire guidance for that existing resource setting; no local live startup or nested initialization. Do not add a fake writer marker or weaken actual host/source/catalog proofs. The parent validates the combined EventStore/Tenants diff.
- Actual hosted worker lifetime, its precise outbox, and the opt-in must be established before skipping inline replay. A constructed capability alone is insufficient. Unstarted, stopped, missing or disabled ownership retains original awaited fallback.
- Preserve the exact original public SubmitCommandHandler CLR constructor signature (including its optional source API) with unambiguous DI composition. Add a meaningful binary-signature/previous-caller binding regression.
- After a real accepted event-bearing result, independently restore activation even if the caller cancellation already fired, using the existing independent projection/cleanup semantics and bounded existing storage protocol. Preserve caller cancellation for response/advisory work and do not ACK on failed required durable restoration. Apply the same required durable restoration to successful unknown-outcome reconciliation BEFORE admission completion/ACK. Test old-head activation removal + cancellation/failure + later reconciliation with actual persistent recovery.
- Capture one independent catalog lease for the complete worker attempt, including guarded source/model/state delivery and cleanup. All supported legacy and named outputs in the captured committed plan must be delivered; no guard shortcut may erase or indefinitely defer an omitted legacy output. Reuse existing plan/delivery logic, source guard and exact proof/refusal contracts; do not replace them with checkpoint-only success. Test actual mixed-method end states and refusal, held worker lease/generation transition, and release only after known completion.
- One held aggregate must not block unrelated aggregates or future scans. Use bounded work slots owned and drained by the hosted worker; retain actual task/lease lifetimes until known completion rather than abandoning RPCs or introducing arbitrary timeout increases. Test two distinct aggregates and a later scan while one delivery is held. Preserve existing stopped-host cancellation/failure recovery. There must be no unobserved request-detached workflow.
- Emit closed structured scan/delivery/defer failure and recovery signals, with repeated scan failure visibility tests. No payload/credential/arbitrary exception message logging and no new health API is necessary.
- KEEP the counter/ABA safeguards and all tested positive behaviors, adapt the existing production-DI and persisted single-event/full-replay tests to actual hosted ownership and explicit opt-in, and run only these affected classes/builds. Do not stage/commit/push or start any local backend/topology.

## Final local candidate and parallel reviewed repairs

The re-derived EventStore candidate passes117 focused cases with zero failures/errors/skips and serial source build zero warnings/errors. It includes complete mixed-output delivery/authority, lifecycle and exact-outbox opt-in, protected restoration/removal fallback, bounded slots, actual-RPC draining without clearing ambiguity, closed logs and original constructor signature. Parent Tenants fresh source-profile setting uses EventStore__Projections__WorkerBackedResponsesEnabled=true only inside the existing TENANTS_SOURCE_CATALOG_PROFILE guard; owning source AppHost build passes0warnings/errors and2profile guards/13refusals pass.

This same combined review also contains two separately scoped repairs: spec-tenant-flow-disposal-2026-11-01.md caches the original tracker for five real Razor flow disposals (745affected tests including25new,0skip), and spec-owned-redis-save-2026-11-01.md accepts only exact busy→OK SAVE acknowledgement under one original3min budget while preserving the complete owned backup/restore chain (250no-backend cases,0skip). Their source tasks/bounds are in those neighboring specs; neither changes browser waits, historical expected outcomes, provider profiles or real acknowledgement requirements. Parent sentinel metadata rename retains existing privacy assertions and the unchanged secret scanner. All current code is included in /tmp/final-owner-repairs-review.diff.

Parent acceptance audit restored StopAsync caller-deadline behavior while the actual ExecuteTask continues owning pending RPCs/slots/leases. Final serial source build passes0warnings/errors;8 focused lifetime/drain cases pass after the117-case candidate run. Full owner diff includes this correction. All three scoped repairs are locally complete; hosted results remain pending.

## Final combined review triage (loop1 candidate)

- Blind1 — medium, patch: supported batch1 can repeatedly return only a held active ID; request bounded additional rows for active IDs and test batch1/default progress.
- Blind2 — medium, defer: lost actor RPC response after old-head activation cleanup already skips inline delivery at baselinefa and returns no acknowledgement. This candidate preserves unknown-outcome recovery and now repairs activation on successful reconciliation; independent recovery of never-retried unacknowledged commits is a preexisting protocol gap, not a new accepted-response path.
- Blind3 — medium, patch: the general source-profile flag also supports retained composition and alone must not automatically opt into a fresh-writer feature. Add a separate exact opt-in produced only after the existing fresh-backend preparation checks, preserving generic source-profile behavior and all proof/refusal controls.
- Blind4 — medium, patch: unconditional handler registrations replace preexisting custom consumer handlers. Preserve custom descriptors and upgrade only the default concrete MediatR handler registration to the owning factory, covering either registration order.
- Blind5 — low, patch: generic unproven logs discard finite production source-guard reason codes. Retain an explicit closed allowlist/fallback and test unknown arbitrary values are not logged.
- Blind6 — low, patch: completion after a failed defer atAttempt0 removes the remembered failure without a recovery signal. Emit recovery when actual completion supersedes that failure.
- Blind7 — low, patch: a stale-revision conditional defer no-op can currently emit recovery. Verify persisted revision/attempt/due state before claiming defer persistence; preserve newer activation and avoid changing processing for logging failures.
- Blind8 — low, patch: add a forced same-shard ETag conflict between two real outbox instances, proving counter retry and neighboring activation preservation across removal/recreation; existing sequential/failed-save checks do not cover this interleaving.
- Blind9 — medium, patch: independent duplicate of the pre-verified eight-slot saturation gap; retain this separate row and add bounded nine-activation coverage.
- Blind10 — medium, defer: cancellation callback failure before unsubscribe/detach is the unchanged baseline Dispose ordering, separate from the demonstrated disposed-provider lookup defect. The five flows now eliminate their DI access; guarding arbitrary throwing authorization callbacks is a preexisting cleanup-hardening task and does not change the verified provider-disposal correction.
- Edge1 — medium, patch: independent same batch-truncation starvation; preserve as separate row, addressed with bounded active-ID allowance and small-batch regression.
- Edge2 — false as a code defect, carried: same claim-only pre/post-activation wording as initial Edge3. Pre-route write failure prevents routing; required post-commit write failure refuses acknowledgement after the already completed actor call. Tests explicitly distinguish both and no code can undo that ordering.
- Verification1 — medium, patch (pre-verified): the eight-slot upper bound needs saturation coverage with a ninth durable activation, capacity release and continued owned shutdown.
- Verification2 — medium, patch (pre-verified): the real command entry resolves IRequestHandler through MediatR; positive deferral must exercise that registration, not only the concrete handler.
- Verification-other1 — medium, patch: independent same supported-batch starvation; keep this separate finding row, covered by the scheduler correction.

## Final review patch verification and publication

All accepted final combined findings are patched: bounded active-ID scan allowance (including batch1/int.MaxValue), actual eight-slot/ninth-work saturation and owned shutdown, custom-preserving handler composition in both assembly-scan orders, positive real IMediator dispatch, finite refusal reasons, actual completion recovery, persisted conditional defer readback, and forced same-shard ETag retry. All15 findings remain individually recorded above; two preexisting gaps are deferred in the ledger. Parent read the final patches. Four affected classes pass104/104; the independent15-class regression run passes571/571 with zero skips. The staged tracked-content security suite passes246/246 after replacing a synthetic credential-shaped negative fixture with an explicit inert redacted placeholder; scanner rules remain unchanged. Serial source builds pass with zero warnings/errors.

EventStore owner commit a82555477689e3e021c3c74bcab0ccfefd4ff307 is pushed to main with commitlint and hooks active. Hosted main/Integration qualification is pending. Tenants enables the default-off behavior only with a separate exact fresh-preparer opt-in inside the source-profile guard; generic retained source profiles remain off. The existing fresh-backend proof/refusal tests pass2 tests including13 refused preparations. A newly fetched upstream Tenants commit5a21dd2c4f0a96cd3cfafe418c61e1e197c99c42 independently changes bootstrap transport and its EventStore invocation budget to30s; it is preserved. The old5a21 source run41358425596 reached bootstrap-authority-timeout against EventStorefa; it does not qualify the new pinned source.

## Exact hosted qualification closure

EventStorea82555477689e3e021c3c74bcab0ccfefd4ff307 completes all seven exact-source workflows successfully, including mainCI41360994832 and both actual published compatibility/live-sidecar Integration41360994325 jobs. Tenants3a32b8d74f954832dd8426f5b0151205a6be4e47 actual source41372568646 passes the complete cohort/bootstrap/command/persistence prefix, six REST and conditional reads, actual Admin UI proxy/owner read, refused descendant, persisted Redis tenant and zero outstanding leases. Its successful artifact is preserved in release-evidence/tenant-a825-owner-source-qualification-2026-11-02.json. Earlier failed evidence is retained; a later successful run does not erase the separately demonstrated premature10s native-query/AdminApi race, tracked in its own transport-budget spec. This closes this scoped functional repair only; final maintenance release remains pending its final source cut/publication. The standalone deployment-policy budget follow-up is resolved by preserved upstreamca93 and its tests.
