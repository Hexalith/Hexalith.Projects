---
title: '7.1-P2: Qualify Durable Task admission and Folder provisioning'
type: 'chore'
created: '2026-11-25'
status: 'in-progress'
baseline_commit: 'fdc8ca69ebe0a656f88d55f9e32c8de71b1262f6'
route: 'dispatch'
review_loop_iteration: 1
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem** — Story 7.1 needs authoritative task admission and Folder provisioning before writer qualification. Story 8.1 needs committed task status and receipt recovery for its acceptance. The inspected source revisions lack accepted owner records for this package.

**Approach** — Qualify the relevant G-1 and G-2 inputs in isolated persisted fixtures. Record the owner contracts, exact revisions, verification commands, receipts, exercised rollback, and decisions in one Projects prerequisite pin.

**Decision (2026-11-25)** — Prepare the EventStore, Platform and Folders owner proposals. Route the G-1 qualification design to Platform outside its current MVP. Owner implementation requires repository-local scope approval.

## Boundaries & Constraints

**Always** preserve repository ownership under AD-6. EventStore and Platform own generic task execution and its qualification. Folders owns creation, authorization, provision receipts and any compensating command. Require separately approved repository-local scope for owner changes. Scope this package to prerequisite evidence. Story 7.1 owns AD-35 claim, fencing and writer qualification. Story 8.1 owns reconciliation acceptance with 7.15.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Expected result | Failure handling |
| --- | --- | --- | --- |
| Admission | Equivalent scoped retry on either replica, including after a lost reply | Reauthorize the original actor and return the same task and bindings. When confirmation applies, consume it atomically with first admission. | Changed request or invalid evidence on a new admission yields no second task or effect. |
| Recovery | Crash or lease expiry after a checkpoint or owner commit | A second replica resumes from committed status and receipts. The stale worker is fenced. Resolve unknown owner outcomes by authoritative lookup before retry. | Dependency unavailability prevents advancement. An unresolved partial effect reaches `NeedsAttention`. |
| Provisioning | Expected owner version, original actor acting directly or through authorized delegation, deterministic task-step key, and no supplied Folder | One create-only Folder. The original actor is sole manager and the Folder has no Tenant-default read. The owner returns current scoped read/manage evidence and a durable receipt. | Stale version, revoked authority, or a name collision cannot bind another Folder. An existing Folder remains untouched. |
| Compensation | Folder exists after an uncommitted activation | Retain the Folder and receipt and enter `NeedsAttention`. An explicit compensating action, where approved, uses an idempotent Folders command. | Owner unavailability prevents further mutation and authority fallback. |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md` under **Story 7.1 prerequisite work packages** and `sprint-change-proposal-2026-11-24-readiness-blockers.md` §5.1 define the work and approval route. `sprint-status.yaml` tracks `7.1-P2` under `action_items`.
- Projects source inventory `90315d6bdad589ea9072c508bda0bd3b7e1f2a43` pins the following owner source. These coordinates identify inspected source.
  - EventStore `ae1143bdc831ca6f2cf61726648a3efe946d68a2` has `docs/guides/checkpointed-processes.md` and `src/Hexalith.EventStore.DomainService/CheckpointedProcessRunner.cs`. Its process checkpoints and effect receipts omit task admission and atomic confirmation consumption.
  - Platform `f7635b592b0499a1c0888b91d1e3be4b0a2d7f78` excludes G-1 from MVP in `_bmad-output/specs/spec-platform/SPEC.md`. Projects Architecture Spine reserves provider, package, lease and scheduler decisions to G-1 design approval.
  - Folders `d56e868fd38c1c84f447180b8bfbe525235220f7` has `src/Hexalith.Folders.Contracts/Commands/CreateFolder.cs` and `FoldersDomainServiceEndpoints.CreateFolderAsync`. Generic creation lacks the expected version and sole-manager access mode. `FolderOwnerReadQueryHandler` supplies delegated metadata read authority. Folders workspace task status covers workspace operations. Provisioning still needs its own authoritative receipt, read/manage proof and compensation disposition.
- `evidence/epic7/7.1-P1-creation-contract-pin.json` shows how to record revisions, commands, hashes and dated decisions. Its HTTP acceptance covers the creation contract and submitter call.

## Authenticated lookup slice (2026-11-26)

The user approved `DurableTaskExecution` for the G-1 authenticated host and requested this slice stop after the committed-step lookup is verified. The broader package below stays open.

- [x] Update [the EventStore G-1 decision](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md) with the approval, numbered step model, lookup contract and remaining G-1 work.
- [x] In EventStore `src/Hexalith.EventStore.DomainService/DurableTasks/`, expose an opt-in, versioned POST owned by the G-1 host using the existing EventStore workload assertion and Dapr channel authentication. Use a dedicated workload operation and exact signed Tenant and target domain. Require a host-registered grant keyed by workload, admission operation, step number and target domain. Bind the concrete target to the committed step by checking it against host-owned target derivation, using the committed admission and canonical step. Never select a grant or actor from a caller's unverified identity. Keep Folder-specific identity derivation in the registering host/owner.
- [x] Use `DurableTaskAdmission.ResolveStepAsync` to return only the relevant committed Tenant, original actor, admission operation, request digest, task, canonical step key and receipt/state. Require the presented normalized digest to match the admission and withhold the binding if the scope, target or digest does not match. Check the presented positive fence against the current persisted fence after scoping. Stale fence refusal must disclose no binding or current fence. A committed receipt is recoverable under the current fence without authorizing execution again. A pending step only authorizes execution where the committed state accepts it. Ensure the numbered lookup does not reinterpret a legacy named `DurableTaskRecovery` step. Folders must independently re-evaluate the original actor's current authority and deduplicate the effect by task-step key.
- [x] In EventStore `tests/Hexalith.EventStore.DomainService.Tests/DurableTasks/`, exercise the real opt-in HTTP route with signed assertions against the existing Platform `probe_state_store.py --native-execution` phases. Given a persisted pending step and a scoped caller, the route returns the original admission and accepts only the current fence. Given a wrong workload, Tenant, target, operation, step or digest, it discloses no binding. Given takeover after restart, the stale fence is refused, the replacement fence recovers the same receipt with execution disabled once checkpointed, and the owner row shows no duplicate effect. Keep the existing local crash-recovery assertion patch byte-for-byte. Retain the native XML and exact source overlay and command receipts.
- [x] Record the exact revisions, commands, results and receipt hashes in this spec. End this slice with the spec in progress and 7.1-P2 open for runtime qualification, Folder provisioning, rollback evidence and owner acceptance. Preserve the package matrix below as outstanding acceptance criteria.

## G-2 Folder provisioning slice (2026-11-26)

The [Folders owner decision](../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md) governs this isolated qualification. The Folders v1 workload command derives the Folder identity from the Tenant and canonical numbered task step. Its first write requires version zero. The command verifies the [committed EventStore lookup](../../references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DurableTasks/DurableTaskStepLookupExtensions.cs), rechecks current Folders authority, and appends the creation, receipt, and two explicit grants in one actor commit. Legacy creation retains Organization permission inheritance. The Folder owner lookup returns current access evidence with the committed receipt. The worker retains the Folder and receipt when activation needs attention.

## G-2 follow-up on the two requalification findings (2026-11-26)

The Folders owner decision and pushed-revision requalification established the two source findings. The original isolated deployment evidence is under `~/.local/state/hexalith-projects/7-1-p2-g2-pushed-G7oDoWbT`.

- [x] In Folders `tests/Hexalith.Folders.Server.Tests/FoldersHumanCommandCatalogTests.cs`, verify the installed `folders-provision-folder` payload schema with its internal ingress and continue verifying the existing public command schemas with public ingress.
- [x] In shared EventStore gateway composition, return `DomainServiceSafeDenialException` as the Folders host's metadata-only problem for each closed status. Copy the `title`, `code`, `category`, `retryable` and `details.visibility` values of [`FoldersDomainServiceSafeDenialExceptionHandler`](../../references/Hexalith.Folders/src/Hexalith.Folders.EventStore/FoldersDomainServiceSafeDenialExceptionHandler.cs) and omit `detail`. Reuse the existing `ProblemTypeUris` types: 401 `AuthenticationRequired`, 403 `Forbidden`, 404 `NotFound` and 503 `ServiceUnavailable`. Make the handler `public` like its siblings in `src/Hexalith.EventStore/ErrorHandling/`, and log each denial at Warning as a `SecurityEvent` with the correlation ID and status through a source-generated `LoggerMessage`.
- [x] Document each response as an additional example of its existing `ErrorReferenceEndpoints.ErrorModels` entry, and add the four scenarios to `ErrorReferenceHandlerParityTests`. As with `bad-request` and `not-found`, the primary `authentication-required` example comes from outside the exception handlers, from the JWT challenge in `ConfigureJwtBearerOptions`. Keep the signed regression through `POST /api/v1/commands/workload` and assert its media type without a null-conditional.
- Verify from `references/Hexalith.EventStore` with `run-with-temp bash -c 'export NUGET_PACKAGES="$TMPDIR/nuget" CI=true; dotnet build tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj -c Debug -m:1 --artifacts-path "$PWD/artifacts/7-1-p2-g2-followup" && dotnet "$PWD/artifacts/7-1-p2-g2-followup/bin/Hexalith.EventStore.Server.Tests/debug/Hexalith.EventStore.Server.Tests.dll" -class "*DaprInternalAuthenticationHandlerTests" -class "*ErrorReferenceHandlerParityTests" -class "*ErrorReferenceEndpointTests" -class "Hexalith.EventStore.Server.Tests.ErrorHandling.*" -failSkips'`. The ignored in-repository artifacts path lets the tests locate the solution and source files. The build must have no warnings or errors, and every selected test must pass with zero skips.
- This re-derivation covers only the two unchecked tasks above. Leave the Folders test change, the evidence bundles and the package tasks below untouched, and make no commits. The correction candidate requalification below covers the pre-review EventStore candidate `5bc9e7a9`, and it needs a rerun before the EventStore change lands.

The [correction candidate requalification](#g-2-correction-candidate-requalification-2026-11-26) records the Release package, runtime, enrollment and native results.

## Tasks & Acceptance

**Execution**

- [x] [`7-1-p2-owner-scope-proposals.md`](7-1-p2-owner-scope-proposals.md) — prepare the G-1 and G-2 owner requests, persisted proof cases and rollback criteria for review.
- [ ] `evidence/epic7/7.1-P2-capability-pin.json` — collect immutable owner revisions and records, contract versions, retained native receipt hashes, commands and exercised rollback evidence. Verify the matrix against real persisted task and Folder state across restart, competing replicas and lost replies.
- [ ] `evidence/epic7/7.1-P2-capability-pin.json` — record dated EventStore, Platform and Folders approvals and Solution Architect and Test Architect acceptance against the same coordinates.
- [x] `tools/planning/validate_production_authority.py` and `tests/tools/test_production_authority_guard.py` — require the accepted pin when the tracker marks P2 done. Cover missing or mismatched evidence and preserve the Story 7.1 gate.
- [ ] `sprint-status.yaml` — link the accepted pin after candidate and active index validation. Route it to the existing 7.1 and 8.1 gates.

**Acceptance Criteria**

- Given immutable owner records, when an isolated checkout runs the recorded commands, then persisted results satisfy the matrix and the exercised rollback recovers without duplicate effects or lost committed history.
- Given a missing capability, persisted receipt, rollback result or approval, when the package is evaluated, then 7.1-P2 remains open.
- Given approvals and matching evidence, when the pin is recorded, then both consumers can resolve the accepted contract versions, evidence and rollback from that pin.

## Implementation Notes

- The [Projects guard](../../tools/planning/validate_production_authority.py) requires `evidence/epic7/7.1-P2-capability-pin.json` when the P2 action is `done`. The pin schema `hexalith.projects.7-1-p2-capability-pin.v1` identifies the three owner revisions and capability records, both contract versions, persisted admission/recovery/provisioning/compensation results, and an exercised rollback target and results for each contract. Verification commands and SHA-256 digests accompany the retained receipts. Artifact paths are relative to this implementation-artifacts directory.
- The pin references five dated, hash-verified approval records. Each UTF-8 JSON record declares `role`, `decision` (`accepted`), `approver`, `timestampUtc`, and `qualificationSha256`. Compute the digest over the pin's `qualification` object serialized as ASCII JSON with sorted keys, no spaces around separators, and no final newline.
- The owner scope decisions dated 2026-11-25 are recorded locally. [EventStore G-1](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md) · [Platform G-1](../../references/Hexalith.Platform/docs/implementation/g1-durable-task-scope-decision.md) · [Folders G-2](../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md)
- EventStore implemented the scoped admission and atomic confirmation candidate. Platform exercised the conditional state provider with two Dapr 1.19.2 sidecars and a Redis restart. EventStore passed four focused and three live admission cases with zero skips. The Folders investigation identified the native create, restrictive ACL and receipt lookup seams. Delegated provisioning needs verified original-actor evidence bound to the admitted task step.
- P2 remains open. The full execution and provisioning matrix, exercised owner rollback, immutable capability records and five acceptance decisions are pending. The [owner proposals](7-1-p2-owner-scope-proposals.md) describe the required qualification runs.
- The [Epic 7 entry verification](evidence/epic7/entry-blockers-2026-11-25.md#71-p2--durable-task-and-folder-provisioning) records the current gitlinks, focused results and the capability evidence each owner must supply. The package remains open pending qualification and acceptance.

- The G-1 execution candidate is committed at EventStore `be511806116666bb0a6d6b374f461b23449b2a4f` and Platform `7b4cc72203cf4dd4ab5f31e33b9fc5f981e30dc5`, which Projects `602816fc5f3bb206beafcbf5a61eed0a43b75416` pins. EventStore persists the lease fence, checkpoint, pending owner step and receipt identifiers on the admission row. It resolves the stable task-step key through the owner before invoking a step and fences automatic execution when the owner reports an unresolved partial effect. Current authority gates scoped progress reads. Platform runs the two execution phases as separate native test processes and restarts Redis and both Dapr sidecars between them.
- The reviewed G-1 lifecycle candidate is committed at EventStore `388b0ad679d7f9404e10669dd1238381157259c9`. The admission row records the eight-state lifecycle, and a persisted state outside it fails closed. Owner failures record `WaitingForDependency`. A terminal outcome requires the current lease and no pending step, and it stays immutable. Cancellation fences the worker and keeps committed receipts. It is refused once an irreversible step may have reached its owner, and while another step is in flight unless the task is in `NeedsAttention`. Status and cancellation by task ID reauthorize the original scope, and a worker can acquire its lease from a scheduled task ID. [EventStore lifecycle candidate](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md#execution-and-lifecycle-candidate)
- The next G-1 slice is committed at EventStore `ea3ca6ec2457e88765ac90060952039fdf4d6402` over `8e205908765dac0358c92f65058ea05b231287a0`. Projects pins its merge with EventStore main, `a0026dc8caaa4aac50fd010cf831f2376aba15f9`. An owner can report an authoritative refusal, which ends the task as `Rejected` with no pending step. Each attempt at a pending step is charged against the runtime's budget when it starts, including attempts that outlive their lease or are abandoned. A spent budget moves the task to `NeedsAttention`, so it can be cancelled before its irreversible checkpoint. `ResolveStepAsync` implements the committed step lookup selected for delegated steps. It returns the admission, fence, status and receipt of a started step and accepts execution only under the current fence. `IDurableTaskEffect.ExecuteAsync` now returns the same outcome and receipt pair as the lookup. Race tests cover cancellation against step start, lease acquisition and completion. [EventStore lifecycle candidate](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md#execution-and-lifecycle-candidate)
- G-1 still needs authenticated admission, confirmation, task status and cancellation routes, a scheduler with durable discovery, reconciliation from `NeedsAttention`, admission and index retention, an owner-selected operating envelope with the failure budget value, and exercised rollback. G-2 can reuse the Folder stream's absent-stream commit, ACL projection and current owner-read seams. On 2026-11-25 EventStore, Platform and Folders selected the committed admission lookup to bind the original actor for delegated steps. Folders kept display names non-unique, so a collision is an existing Folder at the derived identity or a changed request under the same step key. Its rollback after restricted provisioning is forward-only. [EventStore](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md#original-actor-binding-2026-11-25) · [Platform](../../references/Hexalith.Platform/docs/implementation/g1-durable-task-scope-decision.md#original-actor-binding-2026-11-25) · [Folders](../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md#decisions-2026-11-25) The approved scope specifies retaining the Folder and receipt for unresolved activation.
- The package requires immutable owner capability records, the full persisted matrix and rollback, and all five hash-bound acceptance decisions before linking a pin.

- EventStore main also carries a parallel one-task crash recovery slice, `3475b097857b71d4cc25d53f0dce810e7a7fc928`, with `DurableTaskRecovery` and a prepared step on the same admission row. Its live test `WorkerCrashResumesCommittedCheckpointWithOneOwnerEffect` already fails at EventStore `9d24e05d8` before the merge, because it compares `ImmutableArray` receipts by reference. The merged tree builds with zero warnings, all 87 non-live Durable Task tests pass, and the five other admission live tests and both execution phases pass. The decision record lists the choice of one step-execution path as still open.

- The live crash rehearsal now passes with an EventStore test assertion change. `DurableTaskRecovery` prepares a named step with a hashed key. `ResolveStepAsync` parses the numbered keys used by `DurableTaskExecution` and reads progress from the shared task row. The 2026-11-26 lookup slice selects the numbered step model and exposes the authenticated lookup for G-2 to verify the admitted actor and request. The owner entry point needs a create-only commit that retains the restricted ACL and receipt with the Folder history. [EventStore crash test](../../references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/DurableTasks/DurableTaskAdmissionLiveTests.Recovery.cs) · [Folders scope decision](../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md)

## Spec Change Log

- **2026-11-26, review loop 1.** Trigger: BHC-1, BHC-2 and VGC-2 (high). The shared safe-denial handler failed `ErrorReferenceHandlerParityTests` and documented none of its four responses. BHC-3 (medium): the gateway stopped logging the denial. Amended the second G-2 follow-up task into two tasks. They require the existing `ProblemTypeUris` types (owner decision on 2026-11-26), a public handler with a `SecurityEvent` log, documented examples, and parity scenarios for all four statuses (VGC-1). They also require a media-type assertion without a null-conditional (ECC-5), the verification command and the re-derivation scope. Known-bad state avoided: a gateway handler outside EventStore's error-reference contract that fails EventStore CI and leaves owner refusals unlogged. KEEP: register the handler directly before `GlobalExceptionHandler`. Keep the closed status mapping and the Folders titles, codes, categories, retryable values and metadata-only details. Keep the signed workload regression and its `SignedWorkloadCommandRequest` helper. The Folders test change and candidate `20015f06` were not reverted, because no surviving finding touches them.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
|---|---|---|
| Receipt bytes can be arbitrary before owner review | false for approval bypass | The digest binds the actual bytes, revisions and verification commands to all five decisions. Those owners and architects verify the native results for their selected harnesses before signing. |
| The guard suite did not isolate a missing Platform rollback revision | medium | The new regression supplies matching synthetic approval hashes and omits only Platform from the Durable Task rollback target. The guard refuses that candidate. |

### EventStore lifecycle slice review (2026-11-25)

Design gaps whose fix needs new contract surface went to the G-1 Still open list, because the EventStore owner scope governs that design.

| ID | Finding | Verdict | Evidence and disposition |
|---|---|---|---|
| BHP-1 | The previous candidate binary would lease NeedsAttention and terminal rows | low | Only test fixtures hold these rows, and no deployed reader exists. Recorded in Still open for the rollback selection. |
| BHP-2 | Rows written by the previous candidate have no migration path | low | Same root as BHP-1. Still open. |
| BHP-3 | The persisted status is an integer with implicit values | medium | Patched to string persistence with an explicit `Unknown` default. The cancellation outcome is never persisted or left unset; reject that part. |
| BHP-4 | An owner denial of the pending step cannot reach Rejected or Failed | medium | `IDurableTaskEffect` cannot report a definitive rejection, so the fix adds contract surface. Still open. |
| BHP-5 | A cancel from NeedsAttention leaves the partial effect unreconciled | medium | The row keeps the pending step and receipts for a reconciler. Patched the `Cancelled` doc. |
| BHP-6 | A waiting task with a pending step cannot be cancelled | medium | The retry budget must first move it to NeedsAttention. Still open. |
| BHP-7 | Owner contract violations repeat on every lease | medium | Patched to move the task to NeedsAttention before throwing, with `MismatchedOwnerReceiptRequiresAttention`. |
| BHP-8 | `IsConsistent` accepts off-graph rows, and only `Unknown` is tested | medium | Patched the rules; `OffGraphPersistedStateFailsClosed` seeds 25 rows. |
| BHP-9 | A corrupt indexed row resolves as unknown before authorization | low | Patched so a corrupt row fails closed. The existence signal applies only to corrupt rows; reject that part. |
| BHP-10 | Authorization cannot distinguish status, cancellation and admission | medium | Needs an authorizer contract change for the still-open authenticated host. Still open. |
| BHP-11 | The task-ID index has no tests | medium | Patched with the orphan-reservation case (VGP-2). Other index faults are covered by the fail-closed theory. |
| BHP-12 | No cancellation race tests | medium | Part of the cancellation qualification still ahead. Still open. |
| BHP-13 | The lifecycle verification block cannot run as written | low | Patched; it ran as written from the umbrella checkout. |
| BHP-14 | The architecture doc is stale and incomplete | low | Patched. |
| BHP-15 | Status cannot tell a live lease from an abandoned one | low | No consumer before the scheduler. Reject. |
| BHP-16 | A failed WaitingForDependency write is swallowed | low | The owner failure still reaches the worker; logging adds a dependency. Reject. |
| ECP-1 | Authority revoked between authorization and the cancellation write | low | Cancellation only stops work, inside one conditional write. Reject. |
| ECP-2 | A corrupt indexed row resolves as unknown | low | Same as BHP-9. Patched. |
| ECP-3 | A corrupt scope surfaces as `ArgumentException` | low | Both fail loudly. Reject. |
| ECP-4 | A blank task ID throws instead of returning no task | false | A blank value is invalid input, and `ThrowIfNullOrWhiteSpace` is the repository standard. Reject. |
| ECP-5 | Owner contract violations leave the task Running | medium | Same as BHP-7. Patched. |
| ECP-6 | The swallowed WaitingForDependency write | low | Same as BHP-16. Reject. |
| ECP-7 | A non-retryable owner denial cannot reach a terminal state | medium | Same as BHP-4. Still open. |
| ECP-8 | A persistent outage makes the task uncancellable | medium | Same as BHP-6. Still open. |
| ECP-9 | A reversible task cancelled from NeedsAttention keeps an unresolved effect | medium | Same as BHP-5. Doc patched. |
| ECP-10 | `IsConsistent` gaps | medium | Same as BHP-8. Patched. |
| ECP-11 | The legacy row format | low | Same as BHP-1. Still open. |
| VGP-1 | Cancelling an unleased task is untested | medium | Pre-verified. Added `UnleasedTaskCancelsAtCheckpointZero`. |
| VGP-2 | The orphan reservation is untested | medium | Pre-verified. `VerifyConfirmationCommitAsync` now checks the leftover ID, including in the live probe. |
| VGP-3 | Only `Unknown` is tested for fail-closed reads | medium | Pre-verified. Same as BHP-8. Patched. |
| VGP-O1 | Old rows do not read back | low | Same as BHP-1. Still open. |

### EventStore step binding and failure budget slice review (2026-11-25)

| ID | Finding | Verdict | Evidence and disposition |
|---|---|---|---|
| BHQ-1 | The failure budget misses an owner call that outlives its lease and a worker crash mid-call | medium | `RecordDependencyDelayAsync` writes through `UpdateOwnedAsync`, which throws once the lease expired, and its catch swallows that. A new lease resumes `Running` with the step pending, so the task can retry forever and stay uncancellable. Patched. |
| BHQ-2 | The previous candidate drops the failure count when it rewrites a row | medium | The stores use default web JSON options, which ignore unknown members. Rollback compatibility is still open; patched that item. |
| BHQ-3 | A refusal is not durable for the step key | medium | A late execution from an earlier lease could commit under a terminal `Rejected` task. Patched the `IDurableTaskEffect` contract. |
| BHQ-4 | The verifying test owner answers a stale binding with `Rejected` | low | A stale worker cannot write `Rejected` because its lease check throws, but owners copying the pattern would treat a stale call as a domain refusal. Patched the owner and the contract text. |
| BHQ-5 | `AcceptsExecution` ignores lease expiry | low | Acceptance is fence-based. The owner effect is idempotent per step key and the next lease's lookup recovers it. Docs patched; no code change. |
| BHQ-6 | A refused step resolves to null without documentation | low | Patched the `ResolveStepAsync` returns text and the decision record. |
| BHQ-7 | The step lookup has no caller scoping, and the class remarks are stale | medium | Owner scoping belongs to the authenticated route, now named in Still open. The class remarks are patched. |
| BHQ-8 | `NeedsAttention` does not record its cause | medium | Reconciliation needs it, and a persisted cause is new contract surface. Recorded in Still open. |
| BHQ-9 | `IsConsistent` misses the failure-count invariant | medium | Rows with failures and no pending step load outside `Rejected`. Patched with off-graph rows. |
| BHQ-10 | Budget paths are untested | medium | Patched the constructor guard (VGQ-2), a budget of 1, an `ExecuteAsync` failure and caller cancellation. |
| BHQ-11 | The API break needs a breaking-change footer | false | The Durable Task API was added in EventStore `be5118061`, which no release tag contains, so it has no published consumer. Reject. |
| BHQ-12 | `ResolveStepAsync` reads the admission row twice | low | The extra read is on a route that does not exist yet, and the fix refactors the resolver. Reject. |
| BHQ-13 | Reusing the lookup enum lets invalid execute outcomes compile | low | The default branch fences them at run time; a dedicated type adds surface. Reject. |
| BHQ-14 | Race tests run only in memory, but Still open dropped the item | low | Patched Still open to keep live-store races. |
| ECQ-1 | A failure after lease expiry is uncounted | medium | Same as BHQ-1. Patched. |
| ECQ-2 | A stale step failure is charged to the next step | false | A lease runs one step at a time (`RunStepAsync` remarks), and another lease's write fails the fence check. Reject. |
| ECQ-3 | `AcceptsExecution` accepts an expired lease | low | Same as BHQ-5. Docs patched. |
| ECQ-4 | `IsConsistent` accepts failures without a pending step | medium | Same as BHQ-9. Patched. |
| ECQ-5 | A previous-candidate writer resets the count | medium | Same as BHQ-2. Docs patched. |
| ECQ-6 | After rollback, refused irreversible rows fail closed and counts are lost | low | Already in the Still open rollback item; BHQ-2 adds the count. Reject. |
| VGQ-1 | An older committed step is never resolved | medium | Pre-verified. Patched in the live probe test. |
| VGQ-2 | The budget constructor guard is untested | medium | Pre-verified. Patched. |
| VGQ-3 | The narrowed irreversible rule is tested only with `Running` | medium | Pre-verified. Patched with `Succeeded` and `Failed` rows. |
| VGQ-4 | No test reads a row written before the failure count existed | medium | Pre-verified. Rollback and row compatibility are still open. Deferred to the ledger. |
| VGQ-O1 | Failures after lease expiry are uncounted | medium | Same as BHQ-1. Patched. |
| VGQ-O2 | `AcceptsExecution` ignores expiry | low | Same as BHQ-5. Docs patched. |

### G-2 slice review (2026-11-26)

| Finding | Verdict | Resolution |
| --- | --- | --- |
| Native reads lack proof at an uncreated identity | High | An authenticated zero-window check must establish missing-stream before the owner reports HTTP 204. A persisted task step and real actor metadata exercise this path. |
| Worker may disclose after a revocation or takeover during a native prefix read | High | The service rechecks membership and the committed fence after the final source observation. The fixture revokes membership during that observation. |
| Non-executable pending step can appear definitively rejected | Medium | The owner withholds execution and returns an empty 404 for this binding. |
| Receipt and access mode leak through reflected legacy projection fields | High | The projection excludes both fields. The legacy projection test asserts their absence. |
| Direct content and lease mutations bypass the freeze | High | The server gates direct writes and keeps recovery reads available. Forward recovery also fences previous servers and permission-projecting workers. |
| AppHost, gateway and worker inventories expected 18 command declarations | Medium | A separate internal provisioning entry joins the 18 public Folders commands. Legacy canonical data still covers its original set. |
| Gateway canonicalization did not cover provisioning casing, fence changes or changed intent | Medium | The wire parser and adapter use exact camelCase. Registry coverage exercises equivalent intent across fences and changed immutable input. |
| Physical Dapr target was used as the gateway JWT audience | High | Stream presence uses the SDK's logical gateway audience. The fixture checks both the target and the signed audience. |
| Native qualification originally accepted proofless read pages | High | The actor supplies native metadata and event envelopes. The SDK reader validates the actual native prefix using EventStore's codec. Withholding proof produces an empty 503. |
| Fixture originally checked membership revocation only | High | A second Folder records an explicit manage revocation and keeps its receipt. Both command retry and lookup withhold it, and the task reaches `NeedsAttention`. |
| A recomputed digest for different intent receives 404 | False | The G-1 binding rejects an uncommitted digest. With the committed digest and edited owner fields, Folders returns the conflict decision. The fixture exercises both. |
| An Organization read failure blocks projected use of restricted grants | Medium | The candidate preserves the existing fail-closed projection dependency policy. The owner receipt reads committed Folder evidence and rechecks membership. |
| Native source APIs are newer than the published package and ordinary tests would skip the fixture | Medium | The source build opts into the Docker qualification fixture. The regular package-mode test project compiles against its released cohort. |
| Reusing a result directory can accept an earlier passing receipt | Medium | The probe requires a new directory and exactly one passing native XML test per phase. |
| Persisted fixture bypasses deployed gateway command and stream transport | Medium | The real signed Folders callback is tested separately. The isolated fixture uses an attributed callback adapter and an actor-backed stream receiver adapter. Running the fully enrolled deployment is a package acceptance item. |

### G-2 correction candidate review (2026-11-26)

The review covered the uncommitted EventStore gateway handler and the Folders schema test. On 2026-11-26 the owner chose to reuse EventStore's existing problem types. The bad_spec loopback reverted the EventStore candidate edits and kept the Folders test change.

| ID | Finding | Verdict | Evidence and disposition |
| --- | --- | --- | --- |
| BHC-1 | The new handler breaks `ErrorReferenceHandlerParityTests`, and it is `internal` while its siblings are `public` | high | Confirmed: the parity test failed on candidate `5bc9e7a9` with the handler missing from its scenarios. bad_spec with BHC-2 and VGC-2, because the follow-up task omitted the error-reference contract. |
| BHC-2 | The four responses are undocumented and their type URIs are inline | high | `ErrorReferenceEndpoints.ErrorModels` has no entry, and `ErrorReferenceEndpointTests` pairs each documented slug with a `ProblemTypeUris` constant. bad_spec; the owner chose the existing `ProblemTypeUris` types. |
| BHC-3 | The denial is no longer logged at the gateway | medium | `GlobalExceptionHandler` logged it before the change. The new handler has no logger, while sibling denial handlers log a `SecurityEvent`. bad_spec. |
| BHC-4 | The body has no `correlationId` or `instance` | low | The caller already holds the identifiers it submitted, and adding fields diverges from the Folders host body. Reject. |
| BHC-5 | `retryable: true` contradicts the terminal idempotency record | medium | Same as ECC-3. Deferred. The missing `Retry-After` is ECC-1. |
| BHC-6 | Every 503 is labelled `read_model_unavailable` through a catch-all arm | false | The exception admits only 401, 403, 404 and 503, so the default arm handles only 503. It carries only the closed status by design, and the labels match the Folders host theory. Reject. |
| BHC-7 | The regression mocks the router and skips the invoker gate | medium | The regression covers the required path from router result through `SubmitCommandHandler` to the shared handler. The unchanged invoker gate in `DaprDomainServiceInvoker.cs` has no EventStore test. Deferred. |
| BHC-8 | Only the 503 branch is tested | medium | Same as VGC-1. Patch carried into the amended task. |
| BHC-9 | Two diverging handler copies | medium | Folders `Program.cs` registers its copy before `AddEventStore()`, so it runs in that host. Retiring it needs an EventStore release with the shared handler. Deferred. |
| BHC-10 | An owner 401 reaches an authenticated caller as 401 | low | Same as ECC-2. Reject. |
| BHC-11 | The Folders schema test passes if the provisioning route disappears | false | `EveryExistingCommandRetainsItsSupportedHumanEligibility` pins all 19 commands, including `ProvisionFolderCommandType` and its internal ingress. Reject. |
| ECC-1 | The retryable 503 has no `Retry-After` | low | Owner freezes are rare, and the header would advertise a same-MessageId retry that ECC-3 defeats. Reject. |
| ECC-2 | A 401 has no `WWW-Authenticate` | low | The mapping is the Folders host contract, and an owner 401 after gateway authentication is rare. Reject. |
| ECC-3 | A same-MessageId retry after the retryable 503 replays the denial | medium | `AggregateActor.cs:1540-1546` records the denial through `CompleteTerminalAsync`. That actor behavior predates this change. Deferred to the EventStore owner. |
| ECC-4 | An aborted write makes the handler throw | low | It occurs only after the caller disconnects, and the middleware logs and rethrows. Reject. |
| ECC-5 | `ContentType?.MediaType.ShouldBe` skips the assertion when null | low | Confirmed in the new regression. Patch carried into the amended task. |
| ECC-6 | The denial stub leaks to later fixture tests | low | `Reset()` clears only received calls, but each later router test sets its own stub or asserts no calls. Reject. |
| VGC-1 | The 401, 403 and 404 mappings are untested | medium | Pre-verified gap. Patch carried into the amended task. |
| VGC-2 | The change breaks `ErrorReferenceHandlerParityTests` | high | Same as BHC-1. The recorded run covered only `DaprInternalAuthenticationHandlerTests`. bad_spec. |

### G-2 correction candidate review, loop 1 (2026-11-26)

The re-derived EventStore change had no bad_spec or intent_gap finding. The three patches below were applied by the implementation session.

| ID | Finding | Verdict | Evidence and disposition |
| --- | --- | --- | --- |
| EC2-1 | A same-MessageId retry after the retryable 503 replays the denial | medium | carried: ECC-3. Already deferred. |
| EC2-2 | Owner categories collapse into `read_model_unavailable` | false | carried: BHC-6. The titles match the Folders host. Reject. |
| EC2-3 | A 401 has no `WWW-Authenticate` | low | carried: ECC-2. Reject. |
| EC2-4 | The retryable 503 has no `Retry-After` | low | carried: ECC-1. Reject. |
| EC2-5 | An idempotent replay logs another denial security event | low | Each replay serves the recorded denial to a caller, as sibling denial handlers log each refusal. Distinguishing replays needs a new flag. Reject. |
| EC2-6 | The `authentication-required` page gives JWT guidance for an owner 401 | low | Confirmed in the served description and steps. Patched with BH2-4 and BH2-5. |
| EC2-7 | A rollback that resubmits the same provisioning step through the gateway never re-executes | maybe-false | Same root cause as ECC-3, which is deferred. It would be medium if the worker resubmits with the original MessageId. The qualified recovery used the committed lookup instead. |
| BH2-1 | Extension keys are literals instead of `GatewayProblemDetailsExtensions` constants | low | Sibling handlers and `EventStoreGatewayClient` use the constants. Patched. |
| BH2-2 | The response has no `clientAction` | low | The Folders host omits it as well, and choosing values is a contract decision tied to the ECC-3 deferral. Reject. |
| BH2-3 | The security log omits tenant, domain, command type and MessageId | low | The exception carries only the closed status, and the log uses the request correlation ID like `GlobalExceptionHandler`. Adding fields needs an exception change. Reject. |
| BH2-4 | The served pages describe other causes than the new examples | low | Confirmed for the four entries. Patched with appended steps that keep each primary corrective action. |
| BH2-5 | The markdown problem pages omit the owner denial, and `service-unavailable.md` promises `Retry-After` | low | Confirmed. Patched. |
| BH2-6 | `ProblemTypeUriComplianceTests` does not list the new handler | false | The parity test pins each exact body, including its `hexalith.io` type and title, so a noncompliant URI or term fails there. Reject. |
| BH2-7 | The signed regression checks only some identifiers for leaks | false | The handler builds the whole body from constants and the closed status, so no request identifier can enter it. The parity test pins the exact body. Reject. |
| BH2-8 | The handler summary claims the Folders host body although the type URIs differ | low | Patched the summary. The spec and the BHC-9 entry are not edited by this review. |
| VG2-1 | The 503 is retryable although the actor records a terminal rejection | medium | carried: ECC-3. Already deferred; EventStore also tracks the question in its own deferred work. |

## Verification

- `python3 tools/planning/validate_production_authority.py --validate-index` must pass for the active index and the isolated candidate using the four project-context path overrides.
- `run-with-temp python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider` must pass before recording a tracker change.
- Active index and isolated candidates passed `--validate-index`. The isolated command supplied `--sprint-status`, `--deferred-work`, `--p0-artifact`, and `--workspace-root`. The synthetic done candidate was rejected with exit 1 after its retained recovery receipt changed.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider` passed 58 tests and 442 subtests.
- The baseline command `aspire start --non-interactive --isolated --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` ran under `run-with-temp` and exited 2 during build. `references/Hexalith.Conversations/Directory.Build.props(9,5)` requires the Platform tool-selected source mapping. Runtime qualification still requires the actual owner capabilities and fixtures.
- The isolated G-1 candidate build used the pinned owner base revisions with the working-tree changes copied into disposable checkouts. From EventStore under `run-with-temp`, build `tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj` with `dotnet build --configuration Debug --artifacts-path "$TMPDIR/artifacts" -p:UseHexalithProjectReferences=true -m:1`. Run the resulting assembly with `dotnet "$assembly" -class "*DurableTaskAdmissionTests" -class "*DurableTaskExecutionTests" -class "*CheckpointedProcessRunnerTests" -failSkips`. Build and 41 tests passed with no warnings, errors or skips.
- Run `python3 ../Hexalith.Platform/eng/durable-task-qualification/probe_state_store.py --native-execution "$assembly" --evidence-directory "$evidence"` in the same job. Three admission tests and both execution phases passed with zero skips. Two Dapr 1.19.2 sidecars shared a Redis append-only store. The test owner committed its second receipt before losing the response. After restart, a replacement worker kept the original task and first receipt, fenced the old worker, and recovered the second receipt from the owner without invoking a duplicate effect. The fixture also checked that owner unavailability prevented advancement. Source and receipt digests and native XML are in `~/.local/state/hexalith-projects/7-1-p2-g1-8uTCXOSM/`. The fixture uses test authority and a test owner.
- The active and isolated Projects indices passed. A separate isolated tracker candidate marked P2 done and linked the missing pin. It was rejected with exit 1. The guard suite passed 59 tests and 442 subtests. The additional test rejects a recovery receipt symlink with the approved bytes and hash.
- The resumed active index passed. Isolated candidates passed for the open tracker and the synthetic complete pin, and failed when the pin was missing or the retained recovery receipt changed. The focused guard suite passed 60 tests and 442 subtests, including the incomplete Platform rollback target. The full owner qualification and five acceptance decisions are pending.
- The committed G-1 inputs were checked out from the Projects `602816fc5f3bb206beafcbf5a61eed0a43b75416` gitlinks into a disposable `run-with-temp` workspace with an isolated NuGet cache. The build had zero warnings and errors, the three focused classes passed 41 tests, and the probe passed three admission tests and both execution phases with zero skips. Receipts are in `~/.local/state/hexalith-projects/7-1-p2-g1-base-Dcl6kI/`.
- After the review fixes, the lifecycle candidate overlaid its 14 EventStore source, test and decision-record files on the same checkouts. The overlay diff SHA-256 is `afddb3fed0a20c8abba1f45e8a26fa17c7716ecd3c70b6d1ccd25cb40e029c70`. The build had zero warnings and errors. `DurableTaskAdmissionTests`, `DurableTaskExecutionTests` and `DurableTaskLifecycleTests` passed 4, 4 and 31 tests. The probe passed 4 live admission and lifecycle tests and both execution phases with zero skips. Receipts and native XML digests are in `~/.local/state/hexalith-projects/7-1-p2-g1-lifecycle-BUQuqa/`. The fixture uses test authority and a test owner.
- Before the review fixes, the full DomainService.Tests assembly had the same 11 `DomainModuleAuthoringGuardrailTests` failures at the committed base and with the first overlay. Those tests need a layout this workspace does not provide. The first overlay caused no new failures. Both XML receipts are in `~/.local/state/hexalith-projects/7-1-p2-g1-lifecycle-kcCc7f/`.
- The active index passed `--validate-index` with 7.1-P2 open, and the focused guard suite passed 60 tests and 442 subtests.
- At EventStore `388b0ad679d7f9404e10669dd1238381157259c9` (the reviewed slice over merge `69dae5d355e1f0824e6e82328aa8f6c2ef027edb`), the decision record's lifecycle verification block ran as written from the umbrella checkout with an isolated NuGet cache. The build had zero warnings and errors. The four focused classes passed 73 tests: 4, 4 and 31 Durable Task tests and 34 `CheckpointedProcessRunnerTests`. The probe passed 4 live admission tests and both execution phases with zero skips. The full DomainService.Tests assembly ran 1,489 tests with the same 11 `DomainModuleAuthoringGuardrailTests` failures, which need EventStore's nested module layout, and 7 skips.
- With that slice applied to EventStore `8e205908765dac0358c92f65058ea05b231287a0` in the umbrella checkout and an isolated NuGet cache, the Debug source-mode build had zero warnings and errors. The five focused classes passed 70 tests: 4 admission, 4 execution, 44 lifecycle, 13 step-binding and 5 cancellation-race tests. The probe passed 5 live admission tests, including the new step-binding case, and both execution phases with zero skips. The full DomainService.Tests assembly ran 1,521 tests with the same 11 `DomainModuleAuthoringGuardrailTests` failures and 8 skips; the eighth skip is the new live case outside the fixture. Native XML is in `~/.local/state/hexalith-projects/7-1-p2-g1-binding-htx5pzy8/`.
- After the review fixes, the six focused classes passed 83 tests, adding 7 failure-budget tests and 6 off-graph rows. The probe again passed 5 live admission tests and both execution phases with zero skips. Native XML is in `~/.local/state/hexalith-projects/7-1-p2-g1-budget-ny0k27ag/`. The full assembly was not rerun.
- In two detached `run-with-temp` checkouts of Projects `928f66f552c041efa306a445587a6ae9a9fb5486`, Platform's `probe_state_store.py --native-execution` exited 1 on the clean EventStore `a0026dc8caaa4aac50fd010cf831f2376aba15f9` test build at the final receipt comparison, then passed with the assertion patch applied. The patched build had zero warnings and errors. All 87 focused Durable Task tests, 6 live admission and crash cases, and both execution phases around a Redis and Dapr restart passed with zero skips. The fixture used a test owner and test authority. Its persisted rows show one owner effect, a byte-identical task row and ETag after a stale attempt, and a matching receipt recovered through lookup. The test patch SHA-256 is `4def9a247bf42cb5da0b7a3d688b2e37502f309fe1890db0d40254ed4fd566ed` and the persisted recovery receipt SHA-256 is `e55ae8d61ca6a6ae9dbc53e62f85d29a98a85b247eeb792c4915388d83cdba20`. Native XML, the patch and its retained receipt are in `~/.local/state/hexalith-projects/7-1-p2-g1-crash-fix-aJNdrmHb/`. The active index and the isolated open tracker passed. The isolated tracker marked done with no pin was rejected with exit 1. The guard suite passed 60 tests and 442 subtests.


### Authenticated lookup verification (2026-11-26)

The opt-in EventStore route `POST /internal/durable-tasks/v1/steps/lookup` requires a signed workload assertion, the Dapr application-channel token and a host grant for the committed operation, numbered step and target domain. It checks the asserted Tenant, the request digest and the derived target against the committed admission before checking the persisted fence. It returns the original actor from the admission. Valid-workload scope mismatches and stale fences return an empty HTTP 404. [EventStore decision](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md#authenticated-committed-step-lookup-2026-11-26)

| Source input | Verified revision |
| --- | --- |
| Projects | `4d740a889787bb095b16303ed749ec7f165e5683` |
| EventStore gitlink at that Projects revision | `a0026dc8caaa4aac50fd010cf831f2376aba15f9` |
| EventStore compiled source | `521bc1b75f84a24fd5e12f0029221f1e34e2c4de` plus the retained lookup patch |
| Platform | `3e83dfb961f4da617cc41f0f3a3bb453717c9d45` |
| Builds | `a6abb7908ec38dcb3fbd99d69e7ed29ae25794d2` |
| Commons | `0a1d70723b2d112690aa06e6e6a207459cfa2e0f` |
| Tenants | `d34f353abb59213ac021e81da6885b78ee3231fd` |

EventStore `521bc1b75f84a24fd5e12f0029221f1e34e2c4de` contains the previously local crash assertion fix. Its diff from the Projects gitlink for the crash test has the original SHA-256 `4def9a247bf42cb5da0b7a3d688b2e37502f309fe1890db0d40254ed4fd566ed`. The lookup patch leaves that test file byte-identical. Its source SHA-256 is `aa929c75be5142940ad9a70e2e5b70e05dccbf8fb5103dca33eaa705afc74a96`.

The fixture script SHA-256 is `33f8607bb9b7005760ae588883c80c649c80a393473fbe413ab89c0b0e789ce7`. The .NET SDK was `11.0.100`. The two Dapr 1.19.2 sidecars used `daprio/daprd@sha256:ed471547b67db50c831d498b8ee861822fb037d9d594c38ac299546b425f575a` and shared append-only Redis using `redis@sha256:7f7d30b7fdd09467bac58198a1f166dd69789d348d4fe859cccb20e0cf287fbe`. The native runner image was `mcr.microsoft.com/dotnet/sdk@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea`. The provider probe used `python@sha256:4766d8b510c428e595d74b9cc5bbb2fae8e26316fffb4adc89908d79aacd58a2`.

From the Projects root, the retained replay script cloned the exact coordinates into `$TMPDIR`, applied the EventStore patch, built the test project and ran the focused and persisted lanes. All commands below exited 0. The retained directory is local to the verification workspace. Use a copy of the script and patch in a fresh evidence directory to preserve the original receipts during a replay.

```bash
evidence="$HOME/.local/state/hexalith-projects/7-1-p2-g1-auth-5rgAIUbf"
run-with-temp bash "$evidence/replay.sh" "$PWD" "$evidence"
python3 "$evidence/verify-receipts.py" "$evidence"
run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index
run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider
```

The replay ran these checks from the detached EventStore checkout.

```bash
project=tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj
dotnet build "$project" --configuration Debug --artifacts-path "$TMPDIR/artifacts" -p:UseHexalithProjectReferences=true -m:1
assembly="$TMPDIR/artifacts/bin/Hexalith.EventStore.DomainService.Tests/debug/Hexalith.EventStore.DomainService.Tests.dll"
dotnet "$assembly" -class "*DurableTaskAdmissionTests" -class "*DurableTaskExecutionTests" \
    -class "*DurableTaskLifecycleTests" -class "*DurableTaskStepBindingTests" \
    -class "*DurableTaskCancellationRaceTests" -class "*DurableTaskFailureBudgetTests" \
    -class "*DurableTaskRecoveryTests" -failSkips -result-xml "$evidence/focused.xml"
python3 ../Hexalith.Platform/eng/durable-task-qualification/probe_state_store.py --native-execution "$assembly" --evidence-directory "$evidence"
```

The Debug build had zero warnings and errors. The focused lane passed 87 tests. The persisted lane passed six admission and crash cases and both restart phases, with zero skips. The real endpoint used ASP.NET TestServer with fixture-signed workload assertions and the application-channel token. Its task and owner reads used the persisted Dapr/Redis store. The Projects index returned `[6, 7, 8]` with 7.1-P2 open, and its guard suite passed 60 tests and 442 subtests.

| Persisted lookup | Verified result |
| --- | --- |
| Committed binding | Tenant `tenant`, actor `original-actor`, operation `sample.provision@1`, request SHA-256 `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`, task `01M8AH0AXBMS3SAX5FZHFPG761`, step `01M8AH0AXBMS3SAX5FZHFPG761/step/2`. |
| Before restart | Fence `1` returned HTTP 200 and accepted the pending step. Wrong workload scope, Tenant, target, operation, step and digest returned empty HTTP 404 responses. The named recovery step remained inaccessible to the numbered route both before and after its receipt was committed. |
| After takeover, step still pending | Fence `1` returned an empty HTTP 404. Fence `2` returned HTTP 200 with the same committed binding and accepted execution. |
| After receipt recovery | Fence `2` returned `receipt-2` with execution disabled. The owner row and ETag `1` matched across both replicas and across restart. The replacement worker recovered the committed owner reply by lookup and executed no duplicate effect. |

| Retained artifact | SHA-256 |
| --- | --- |
| `eventstore-lookup.patch` | `b75fe1681d0b515ba53c027ee095306a97d7c0af9acadcec4e0a3896560c198d` |
| `replay.sh` | `9009ca6f07a52a8dc3677918c93b8431c1b4b56c71521aa4a7cda1b6b7976e8e` |
| `verify-receipts.py` | `126a395338e39431cd6dc70ec345f5bb26cde9671d265d836b8b1a25053f3b7b` |
| `authenticated-lookup-receipt.json` | `1143fef1e2dc167f39a1e5fbc7d6156b6cc42874aeb218e50401bdfd531e007b` |
| `execution-before-restart.xml` | `1e0a773b40972b0e172e4a0b2dde48640f3cfe5dcf88104d206039ce0c6fa3a3` |
| `execution-after-restart.xml` | `548ce1259e2df98aea2cc6c91452af42d7ac9fac9cfce8d0e2cc8681cc40da70` |
| `admission.xml` | `6c2316bd6cc0b8d72a0f0fb3323626705664c11b6161d4ff6c869a41a5221202` |
| `focused.xml` | `52101da96fc80b046636e8d9ec6af2b17cef8105b1010cd969fc763069176e9e` |
| `recovery-receipt.json` | `f4b3f19eae0c594d01804f62723f46b07c59b9b199eb47cf03a5c843bd6b3d43` |
| `probe.log` | `88d78bad4840ce395365723a10b62c5a3c839970dbf1cad082dc43cada6794e7` |

The replay bundle also retains `build.log`, `revisions.txt`, the Projects guard logs and `sha256.txt`. The lookup slice is verified. 7.1-P2 stays in progress and its tracker remains open for runtime qualification, Folder provisioning, exercised rollback and owner acceptance.

### Authenticated lookup through Dapr service invocation (2026-11-26)

The isolated G-1 lookup host uses the EventStore decision's numbered `DurableTaskExecution` lookup, the shared HTTP security boundary and a single synthetic owner domain. The Platform runner places the lookup host, issuer, Redis and both Dapr sidecars on a private Docker network with no published ports. The owner sidecar `sample-owner` calls `POST /v1.0/invoke/g1-tasks/method/internal/durable-tasks/v1/steps/lookup`. The `g1-tasks` sidecar supplies its configured `APP_API_TOKEN` to the lookup application. The client obtains RS256 assertions from the separate HTTPS EventStore workload-binding issuer at `https://g1-issuer:8443/workload-authority`, using client credentials and exact resource claims. The client and receiver trust the fixture CA and independently validate the issuer's signing key. [G-1 lookup host](../../references/Hexalith.EventStore/tests/Hexalith.EventStore.DurableTaskLookupHost/Program.cs) · [Platform runner](../../references/Hexalith.Platform/eng/durable-task-qualification/probe_state_store.py)

The fixture used a synthetic owner and a 30-second lease on a controlled UTC clock. Both sidecars shared a component-name Redis keyspace with append-only persistence and synchronous fsync. Dapr peer mTLS was disabled inside the private network.

| Source input | Verified revision |
| --- | --- |
| Projects | `4d740a889787bb095b16303ed749ec7f165e5683` |
| Projects EventStore gitlink | `a0026dc8caaa4aac50fd010cf831f2376aba15f9` |
| EventStore | `521bc1b75f84a24fd5e12f0029221f1e34e2c4de` |
| Builds | `a6abb7908ec38dcb3fbd99d69e7ed29ae25794d2` |
| Commons | `0a1d70723b2d112690aa06e6e6a207459cfa2e0f` |
| Tenants | `d34f353abb59213ac021e81da6885b78ee3231fd` |
| Platform | `3e83dfb961f4da617cc41f0f3a3bb453717c9d45` |

The replay applied the existing `eventstore-lookup.patch` byte-for-byte, then the retained EventStore runtime overlay and the Platform runtime overlay. The crash-recovery assertion source matched SHA-256 `aa929c75be5142940ad9a70e2e5b70e05dccbf8fb5103dca33eaa705afc74a96`. Its diff from the Projects-pinned EventStore gitlink still hashes to `4def9a247bf42cb5da0b7a3d688b2e37502f309fe1890db0d40254ed4fd566ed`.

| Runtime | Version and image |
| --- | --- |
| Dapr sidecars | `1.19.2`, `daprio/daprd@sha256:ed471547b67db50c831d498b8ee861822fb037d9d594c38ac299546b425f575a` |
| Redis | `7.4.12`, `redis@sha256:7f7d30b7fdd09467bac58198a1f166dd69789d348d4fe859cccb20e0cf287fbe` |
| .NET build and native host | SDK `11.0.100`, .NET and ASP.NET Core `11.0.0`, `mcr.microsoft.com/dotnet/sdk@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea` |
| Provider probe container | Python 3.12.13, `python@sha256:4766d8b510c428e595d74b9cc5bbb2fae8e26316fffb4adc89908d79aacd58a2` |

Host tools were Docker client and server `29.8.2`, Python `3.14.4` and OpenSSL `3.5.5`. The effective Platform runner SHA-256 was `3fd2bb7a131ee5754262fe0c4cf6b2a7125305687832f4e8b96dbbaa1169e76b`.

The commands ran from the Projects root. For a repeat run, set `results` to a fresh directory to retain these receipts.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g1-runtime-bIZ3ZPFD"
results="$bundle/verified"
run-with-temp bash "$bundle/replay-runtime.sh" "$PWD" "$bundle" "$results"
python3 "$bundle/verify-runtime-receipts.py" "$results"
docker run --rm --pull never --network none python@sha256:4766d8b510c428e595d74b9cc5bbb2fae8e26316fffb4adc89908d79aacd58a2 python --version
run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index
run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider
```

The replay applied the overlays in detached local checkouts and ran the following commands from EventStore. The runner evidence retains each sidecar's container identifier, Redis start timestamps, HTTPS discovery and the results of the named Dapr health invocation before and after restart.

```bash
for name in Hexalith.EventStore.DurableTaskLookupHost Hexalith.EventStore.DomainService.Tests; do
    dotnet build "tests/$name/$name.csproj" --configuration Debug --artifacts-path "$TMPDIR/artifacts" \
        -p:UseHexalithProjectReferences=true -m:1
done
tests=Hexalith.EventStore.DomainService.Tests
host=Hexalith.EventStore.DurableTaskLookupHost
assembly="$TMPDIR/artifacts/bin/$tests/debug/$tests.dll"
lookup_host="$TMPDIR/artifacts/bin/$host/debug/$host.dll"
dotnet "$assembly" -class "*DurableTaskAdmissionTests" -class "*DurableTaskExecutionTests" \
    -class "*DurableTaskLifecycleTests" -class "*DurableTaskStepBindingTests" \
    -class "*DurableTaskCancellationRaceTests" -class "*DurableTaskFailureBudgetTests" \
    -class "*DurableTaskRecoveryTests" -failSkips -result-xml "$results/focused.xml"
python3 ../Hexalith.Platform/eng/durable-task-qualification/probe_state_store.py \
    --authenticated-lookup "$assembly" --lookup-host "$lookup_host" --evidence-directory "$results"
```

Both builds reported zero warnings and errors. The replay passed 87 focused tests, two native execution phases and six native admission and crash-recovery tests, with zero failures or skips. The Projects authority index passed with `[6, 7, 8]`, and its guard suite passed 60 tests and 442 subtests.

| Runtime check | Verified result |
| --- | --- |
| Authorized owner before the effect | HTTP 200 returned Tenant `tenant`, original actor `original-actor`, operation `sample.provision@1`, request SHA-256 `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`, task `01M8AJJBDA94DA9GXDSN2K0RZX` and step `01M8AJJBDA94DA9GXDSN2K0RZX/step/2`. Fence 1 accepted execution with a pending step and no task-side receipt. |
| Scope and credential refusals | A signed workload that disagreed with Dapr's attributed caller and a workload outside the receiver's allow-list each returned empty 401. Wrong Tenant, signed domain, target domain and concrete target ID each returned empty 404. Missing app-channel token and missing assertion each returned empty 401. The wrong assertion operation returned empty 403. The runtime also refused wrong admission operation, step and digest and the legacy named recovery binding. |
| Redis restart and takeover | Redis restarted on the same retained store and both sidecar container identities changed. The replacement worker acquired fence 2. While the step was still pending, fence 1 returned empty 404 and fence 2 returned HTTP 200 with the same binding and execution permitted. |
| Receipt recovery | The replacement worker resolved `receipt-2` through the owner lookup and made zero owner execution calls. Both sidecars read the same owner row, with the same value and ETag `1` before and after restart. The lookup then returned that receipt under fence 2 with execution disabled. SHA-256 of the UTF-8 receipt ID is `774ad7ab1a3d41b114b5f4a34e2d8fc19c2ee8d83dfc133f1d80068ed205597f`. |

| Retained artifact | SHA-256 |
| --- | --- |
| `eventstore-lookup.patch` | `b75fe1681d0b515ba53c027ee095306a97d7c0af9acadcec4e0a3896560c198d` |
| `eventstore-runtime.patch` | `23cbc3ff130600be1329441e67b5deb4210818de30e96ef9adf9852abadec77b` |
| `platform-runtime.patch` | `6df5220f4f4755711c71a52673e87b58ede51ac18b7c20c5043818e6eacc08ec` |
| `replay-runtime.sh` | `d0a83906f40e9b60efda1286e5b27d18769e5debd2638334c6b925874dcadba2` |
| `verify-runtime-receipts.py` | `9188416f33ebd5063098b3ddeb487cc3cbe32874c8738aaf5bc2f674ee4ecbb9` |
| `verified/runtime-lookup-receipt.json` | `93842e6a4ea69932a323f08889939524b53c7dc41f6748d86f8715369b0edaeb` |
| `verified/runtime-topology.json` | `fa1f6f724a644c1735460ae30c98c2f454b2e9d95c4a28a0fbe368543f36df51` |
| `verified/crash-recovery-receipt.json` | `6fecd1dd32751b90c90a1bbb957f9be81a18504926d740523713a7a1ff5dcddd` |
| `verified/execution-before-restart.xml` | `803e87e9d0d0c8035b0eb0c4ae542ffb647cd2b3ca9248e4a179096e79159de5` |
| `verified/execution-after-restart.xml` | `a2d4fb069c301fdd8bf8bcb8966d6241a059ac4dc98e3c65ca860f93f3488d27` |
| `verified/admission.xml` | `5efac0a2a5f60539128b4dfda9b7ead88c1181127b7e5a27c72cde8ec4a756f5` |
| `verified/focused.xml` | `f7fcd56000eba01b62ba81517ade6088404f25e653b25fed660accf9e156f2c5` |
| `verified/lookup-host.log` | `501da059648a95c7fc1988acdf44de8ad7c6582a976df671bb1ad6a26717310e` |
| `verified/probe.log` | `1b7fdf81e8e208485ecf8895bba8d3f889db0db8b81ef0e6bb08215cf0f8a834` |

The replay bundle retains the build logs, process logs, tool versions and full hash manifest. The authenticated lookup runtime slice is verified. 7.1-P2 stays in progress and its tracker stays open for the remaining G-1 runtime work, Folder provisioning, exercised rollback and owner acceptance.

### G-2 persisted qualification and forward recovery (2026-11-26)

The candidate uses the internal `Hexalith.Folders.Commands.ProvisionFolder` command, Folders `POST /internal/folders/v1/provisioning/lookup` and EventStore `POST /internal/durable-tasks/v1/steps/lookup`. The Folders receipt and inheritance marker are additive fields on the v1 `FolderCreated` event. The same actor commit stores two explicit ACL events. The source candidate is the Folders base below plus `folders-candidate.patch` with SHA-256 `fc52b1fcdb9b8f78ae445adeefd8c19e2ebc5e4d6c7896ade6cbbd263b577782`. The replay script SHA-256 is `b85f2ee1c949b97f768d5f28f26d12fadda97cb8095a44e704155ea5b00129d8`.

| Checkout | Exact revision |
| --- | --- |
| Projects | `fa85cf96b1d5804334dac772281e98cab2f7e139` |
| AI.Tools | `a71569a6262467a70c5135709cd10af3b953af9a` |
| Builds | `a6abb7908ec38dcb3fbd99d69e7ed29ae25794d2` |
| Commons | `0a1d70723b2d112690aa06e6e6a207459cfa2e0f` |
| EventStore | `a11d584a5bc9fe680c9b6d7908befb574d98e620` |
| Folders | `4f0ccfd7b116770df699a700858a8c5e29a4e72c` |
| FrontComposer | `4908b7ad53bbfa4b4ad31b153d5a76a5341b18a0` |
| McpCli | `ec657cbce665baf5ebe81283162dea406584dc3a` |
| Memories | `1f68406be7a61064044963139941c9ba2c117947` |
| Platform | `80a60039b46ea6bdccd9eeac5091551bc59643fa` |
| Tenants | `d34f353abb59213ac021e81da6885b78ee3231fd` |

| Runtime | Pinned image |
| --- | --- |
| daprd | `daprio/daprd@sha256:ed471547b67db50c831d498b8ee861822fb037d9d594c38ac299546b425f575a` |
| control | `daprio/dapr@sha256:16ca01236a97825e30b19ef9c89c7c9006fabdd9036a6d7e8947662371418851` |
| redis | `redis@sha256:7f7d30b7fdd09467bac58198a1f166dd69789d348d4fe859cccb20e0cf287fbe` |
| dotnet | `mcr.microsoft.com/dotnet/sdk@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea` |

The source build used .NET SDK `11.0.100`. The fixture used Dapr and its control plane `1.19.2`, Redis `7.4.12` with AOF and synchronous fsync, and .NET `11.0.0`. The network was private, with fixture-signed workload assertions and no published ports. Folders called the real authenticated G-1 endpoint through Dapr. The test composed the EventStore aggregate actor over the retained store and exercised the real Folders receipt endpoint. The fixture supplied callback attribution and adapted native actor reads to the stream receiver's wire shape. It used the SDK reader and EventStore's native proof codec to verify the committed prefix. The focused lane separately exercised the real signed Folders `/process` endpoint and the activated command admission registry.

The recorded command ran from the Projects root. For a repeat run, set the final argument to a new results directory. The script clones the exact root and owner inputs under the private job directory, checks the patch hash, applies the patch and retains logs, native XML and JSON receipts.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-native-lzDWrr15"
run-with-temp bash "$bundle/replay.sh" "$PWD" "$bundle" "$bundle/verified"
(cd "$bundle/verified" && sha256sum --check sha256.txt)
```

The replay ran the following source and package build and test commands from the detached Folders root.

```bash
project=Hexalith.Folders.Server.Tests
artifacts="$PWD/.artifacts/g2-qualification"
dotnet build "tests/$project/$project.csproj" -c Debug -m:1 --artifacts-path "$artifacts" \
    -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:EnableG2ProvisioningQualification=true
assembly="$artifacts/bin/$project/debug/$project.dll"
dotnet "$assembly" -class '*FolderEventReplayTests' -class '*FolderOwnerReadQueryHandlerTests' \
    -class '*FolderDomainProcessorRejectionTests' -class '*ProductionProcessWireEventTests' \
    -class '*FoldersHumanCommandCatalogTests' -class '*FolderServerCatalogAdapterSourceTests' \
    -class '*FolderProvisioningContractTests' -failSkips -result-xml "$results/focused.xml"
dotnet "$assembly" -method '*FrozenFolderRejectsDirectWritesAndKeepsNativeReadsAvailable' \
    -failSkips -result-xml "$results/freeze.xml"
python3 eng/g2-folder-provisioning/probe.py "$assembly" "$results/native"
catalog=Hexalith.Folders.EventStore.Tests
dotnet build "tests/$catalog/$catalog.csproj" -c Debug -m:1 --artifacts-path "$artifacts" \
    -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false
dotnet "$artifacts/bin/$catalog/debug/$catalog.dll" -class '*FoldersDeclaredAdmissionTests' \
    -class '*FoldersIdempotencyIntentAdapterCatalogTests' -class '*DeclaredFoldersIntentCompatibilityTests' \
    -failSkips -result-xml "$results/catalog.xml"
dotnet build "tests/$project/$project.csproj" -c Release -m:1 --artifacts-path "$artifacts-package" -p:UseNuGetDeps=true
```

The builds finished with zero warnings and errors. The Server and EventStore catalog lanes passed 104 and 43 tests. A focused test verified the direct-write freeze. Each native phase passed its test with zero skips. From the detached Projects root, `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index` passed, and `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider` passed 60 tests and 442 subtests. The Release build checks compatibility with the published EventStore `5.6.0` cohort. The live G-1 fixture compiles against the committed source APIs.

| Persisted case | Verified result |
| --- | --- |
| First create and equivalent retry | Authorized lookup returned empty 204 for the missing stream. One creation batch reached version 3 with the original actor's explicit `read_metadata` and `manage_folder_access`. The retry returned `receipt_available` and lookup returned the same receipt. A different step created a distinct Folder with the same display name. |
| Changed intent and stale precondition | Both produced `conflict` under the committed binding. A replacement digest received empty 404 from the binding check. The committed Folder stayed at version 3. |
| Occupied identity | The owner reported `conflict` and lookup returned empty 409. The generic incumbent remained at version 1 with inherited read grants. |
| Scope and revocation | Cross-Tenant use and revoked membership returned 404. Revocation during the final native observation and removal of the explicit Folder manage grant also withheld the receipt. The latter effect retained its creation receipt and the task entered `NeedsAttention`. |
| Dependencies | The unavailable G-1 target and missing native proof both yielded empty 503 responses. The dependency failure created no Folder stream. |
| Lost response | Task `01M8BQQMM5ZRY8AHXZ1KBD1TXX` stayed `WaitingForDependency` with its step pending and no task-side receipt after Folder `fld-p-8d6527e7ba1b35bac880bca69c0c172d46bbf044` committed receipt `prov-8d6527e7ba1b35bac880bca69c0c172d46bbf044`. |
| Forward recovery | The original application and sidecar were removed before the replacement started. Redis restarted on its original retained store. The replacement ran the same source with `Folders:Provisioning:Enabled=false` and `Folders:Provisioning:FreezeMutations=true`. Fence 1 returned empty 404. At fence 2, the task recorded the same owner receipt with zero owner execution calls. A frozen mutation returned 503. |
| Retained state | Folder version 3, receipt, native prefix proof and effective ACL matched across restart. The Folder history SHA-256 was `d1e71ba4ec85830a5524f1b3016c03d41cdece5885814b50ef56ebb6beaaf68d` and its native prefix digest was `f7a2b6b5d50160d3dc204ae948463d246b4f59237f8a3dbf8a48e912ba8e1b52`. The incumbent history SHA-256 was `fd7655ed6c0c8ef75b9574e3aa2274118ea5dec098730f2d3a9fb6622b4a7b97`. |

| Retained artifact under `verified/` | SHA-256 |
| --- | --- |
| `focused.xml` | `558fc17de541f2481d5594b1267faa658a843dc9d4f2284af322e50c5f70165d` |
| `freeze.xml` | `134cd5f817771fcf5c9f964510e24380809c4a78a9cc9548ea130c406b9ae6d8` |
| `catalog.xml` | `453843cec6dc2cf61a06e8054cc5fe9a5b1f83ab36c9a003a5aac65db1887c2e` |
| `native/after-restart.json` | `be298a8a9d9e352b9ba29f1c23b97ae9b70e1df468126536a8b7e204f01be7c9` |
| `native/before-restart.json` | `829819660f44be8bcb282dc9acd58057433e59b48f0823c8f5519cece6c794a8` |
| `native/topology.json` | `7c8d32b2709ee83d1bb579f9152bb3ddfe213894eca573e18680c66d6e89f4e2` |
| `native/after-restart.xml` | `48b0ba60cdc6430b5cdf4cf1d4247da45436b88c7c3009a1a7c0b3f692e6ac15` |
| `native/before-restart.xml` | `3c43fbd36efca5a98446a8b239ed4ac528421dfb2f0e6448769d584d6dc9db35` |

The AppHost catalog cross-check ran `SelectedOwnerCatalogBindsFinalAppIdsAndKeepsAllIntentPolicies`. With the Folders base's Tenants gitlink `54985ab11ade150d5fa908be179cf2a5a407353d`, the candidate passed. The clean Folders base and candidate each failed the same external catalog assertion with Projects' Tenants pin `d34f353abb59213ac021e81da6885b78ee3231fd`, which supplies 14 non-Folders adapters against the expected 13. Commands, revisions and XML are in `~/.local/state/hexalith-projects/7-1-p2-g2-baseline-WEC2Pv7w/` under `replay-implementation-apphost.sh`, `apphost-clean-pinned/` and `apphost-folder-owner/`.

**Remaining owner decisions.** EventStore and Platform still own the G-1 runtime and package choices recorded above. Folders and Platform must approve the released provisioning artifact and deployed workload enrollment for submission, G-1 lookup, and bound stream reads. The deployed composition must run the same receipt and recovery cases with those approved policies. Projects must bind its final activation operation and normalized request digest to the v1 Folder step. The owner disposition retains the Folder and receipt for unresolved activation. The five role acceptances and the combined capability pin remain pending. **7.1-P2 remains open and Story 7.1 remains blocked.**

### G-2 requalification on the pushed revision (2026-11-26)

Projects `685eb6c3476c472573f9e223e37e3fd61859a25c` pins Folders `b9ac1212aa2e65fe10ed50379bb4a04b3cebac77`, EventStore `a11d584a5bc9fe680c9b6d7908befb574d98e620`, Platform `80a60039b46ea6bdccd9eeac5091551bc59643fa` and Builds `a6abb7908ec38dcb3fbd99d69e7ed29ae25794d2`. Projects and Folders matched their respective `origin/main` tracking refs. The persisted rerun cloned those gitlinks into a private workspace and ran the committed Folders fixture without the earlier candidate patch. The runtime was .NET SDK `11.0.100` and .NET `11.0.0`, Dapr sidecars and control plane `1.19.2`, and Redis `7.4.12` with AOF and synchronous fsync, using the image digests above.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-pushed-G7oDoWbT"
run-with-temp bash "$bundle/replay-pushed.sh" "$PWD" "$bundle/persisted-repeat"
(cd "$bundle/persisted-final" && sha256sum --check sha256.txt)
```

| Clean-source lane | Result |
| --- | --- |
| Server focused | 105 passed, 1 failed, 0 skipped. `ContractsExportTheActualInstalledCommandPayloadSchemas` rebuilt `folders-provision-folder` with public ingress. The exported operation has internal ingress. The expected digest was `20e216dc27e6e8a6df864fb134354353ba5d9c2c485693645b38d27345ba195b` and the export digest was `5343af6a8df2fb25d79ee2a61a44d16effda57c7c4ba1606f7872a1f230512f2`. |
| Catalog and mutation freeze | All 43 catalog cases and the focused freeze test passed with zero skips. |
| Persisted qualification | Both native phases passed with zero skips. The cases covered equal retry, changed intent and digest, stale version, occupied identity, duplicate display name, scope and revocation, missing native proof, and unavailable lookup. |
| Forward recovery | The fixture removed the original app and sidecar, restarted Redis with its original store, and started the same revision with provisioning disabled and mutations frozen. Fence 1 returned 404. Fence 2 recovered `prov-ee7350acd6251a15b47de2cb7f305a3e4f254ae4` with zero owner execution calls. Frozen mutation returned 503. The Folder stayed at version 3 with two explicit grants to `original-actor`. Its history hash `21676117d4e7875c9a735f690ebbc2f3f3923fc497c49233be3170450e420ddf`, native prefix digest `ff13ba9e92622b516f52cc510092624c581bd250eaa5ec6a7b576bcaccb865b0`, and incumbent history hash `9d1daeb4fa83b0bc4c7c796c0aebd263317f28f975d680fc1d89e5be6072db4f` matched across restart. |
| Planning guard | `--validate-index` passed. The focused suite passed 60 tests and 442 subtests. |

The native before/after receipts under `persisted-final/native` have SHA-256 `f4d950b325941dc0bb951cb41c5d6534db38dede1dd5e65b702f20d0a3cd1620` and `3ccc09c38b8771273a37c68db961df39df4b77d237195769f0a4c0e65f64f4a5`. The hash manifest covers the native XML, topology receipt, focused failure and planning guard output.

#### Isolated deployment using the packaged extension

The isolated deployment restored `Hexalith.Folders.EventStoreExtensions` and `Hexalith.Folders.Contracts` at local Release candidate version `2.2.0-g2.b9ac1212`. Both NuGet manifests identify Folders `b9ac1212aa2e65fe10ed50379bb4a04b3cebac77`. The extension registers as `folders`, API major 1. Its archive SHA-256 is `b2c776a072666fbd5bc933ee8aa57c3ac2000f4fb864682ca43045ec3dfb3638`. The Contracts archive SHA-256 is `51eafdb057fedd18868846a1dd456240b27b8d4f8d65c55e79bf2dcba02c1378`. The loaded extension assembly and the assembly inside the archive both hash to `0f281ff33ff84aa9e32c4d2c52af38c93d596d9ea3f414a9dc2d02c89f9fd239`.

The package build used released EventStore SDK `5.6.0` and McpCli.Abstractions `3.0.0`. The isolated host used the pinned EventStore source at `a11d584a5bc9fe680c9b6d7908befb574d98e620` for the gateway and G-1 lookup. Its source projects reported version `5.6.0` in the dependency manifest. The host ran .NET and ASP.NET Core `11.0.0`, Dapr .NET SDK `1.19.1`, and the same Dapr `1.19.2` and Redis `7.4.12` images as the source lane. The harness ran the gateway and Folders Server web hosts in a disposable runner with three Dapr sidecars, fixture-signed assertions, separate application-channel tokens for the two receivers, and no published ports. Dapr mTLS was disabled for this fixture. A fixture catalog and seeded Tenant and Organization authority supplied the test enrollment. It exercised the actual gateway admission registry, Dapr domain-service invoker, signed Folders callback, owner receipt endpoint, G-1 step lookup, and native stream controller. The catalog root was `3be80240ab45101ae4b1a2a63aff4829bcc0582fcc3b14ea9949b176698cb456` in both phases.

| Caller → receiver | Route and required bindings |
| --- | --- |
| `projects-fixture` → `eventstore` | `POST /api/v1/commands/workload`, assertion `eventstore:command-submit`, Tenant `tenant-a`, domain `folders`, and internal discriminator `Hexalith.Folders.Commands.ProvisionFolder`. |
| `eventstore` → `folders` | `POST /process`, assertion `domain-service:process`, committed catalog binding and gateway-stamped `projects-fixture` origin. |
| `folders` → `eventstore` | `POST /internal/durable-tasks/v1/steps/lookup`, assertion `eventstore:durable-task-step-lookup`, Tenant and domain binding, admission operation `fixture.activate-project@1`, numbered step 1, derived target and current fence. |
| `folders` → `eventstore` | `POST /api/v1/streams/read/workload`, assertion `eventstore:stream-read`, enrolled Folders public key and RS256 proof bound to the exact stream and read window. |
| `projects-fixture` → `folders` | `POST /internal/folders/v1/provisioning/lookup`, assertion `folders:provisioning-lookup` bound to the Tenant and domain. |

Both deployment xUnit phases passed with zero skips. Missing assertion and wrong Tenant submissions returned 401 and 403. A mismatched stream proof returned 403. The bound zero-window absence check returned 404 with `missing-stream`, and the owner returned 204. The authorized command returned 202 and the signed callback returned 200. An equal request reused the gateway command identity. A changed request with that key returned 409 before any second callback. A fresh gateway attempt for the same task step returned `receipt_available` without adding events. The bound range read returned 200 with three events and the actor's native prefix proof. Stale owner version and changed intent returned 409 at receipt lookup. Changed digest and revoked membership returned 404.

The harness simulated a lost owner reply after the native commit. Task `01M8BSEVC7BHB2WRNN2YM044HR` waited for its dependency with no task-side receipt. The replacement runner and all three replacement sidecars used the same Redis store after restart, with provisioning disabled and mutations frozen. Fence 1 returned 404. Fence 2 recovered `prov-04a18d87526242413f682db042a560a865b86b16` for Folder `fld-p-04a18d87526242413f682db042a560a865b86b16`, recording one task receipt and zero recovery execution calls. Both explicit grants remained with `original-actor` and the incumbent retained its inherited read grant. The version-3 Folder history hash `deb6543f33380273ffc1c4101267dc18af65ccb2c6cfbd23a268248b62b69744`, native prefix digest `0d9a5e17575783c0c351c5667d1f44fffedb1916d89bfcc1afac70c4c7cd5d56` and incumbent history hash `1c5eaf96ea6388e2f74650603867d247c62ad7b4637cd160d12fccec8fa53f6e` matched across restart.

**Transport finding on the pushed tuple.** The signed Folders callback refused a post-recovery mutation with 503 and the persisted histories stayed identical. The shared gateway's global handler mapped `DomainServiceSafeDenialException` to a generic 500. The standalone Folders EventStore host registers an explicit handler for that exception. See the correction candidate below for the shared gateway mapping. Package acceptance still needs the owner-approved deployment and enrollment records. [Standalone exception handler](../../references/Hexalith.Folders/src/Hexalith.Folders.EventStore/FoldersDomainServiceSafeDenialExceptionHandler.cs)

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-pushed-G7oDoWbT"
run-with-temp bash "$bundle/replay-deployment.sh" "$PWD" "$bundle" "$bundle/deployment-repeat"
python3 "$bundle/verify-results.py" "$bundle"
(cd "$bundle" && sha256sum --check sha256.txt)
```

| Deployment artifact under `deployment/native` | SHA-256 |
| --- | --- |
| `before-restart.json` | `cafb774986465e0d8507accd13abcc6d2c193cd0af6c1ae48d7c784d3c340e17` |
| `after-restart.json` | `2c67a66c2d5b7aac1a36277f3aa283342361fa85ecae2f4073bb91ed4e25f157` |
| `before-restart-wire.json` | `8303cc657fba525a597ff9ff7b08f60fc9e35fc043a028b067208112c6c03461` |
| `after-restart-wire.json` | `d17c41da50448766d8a82a10293be04cba8178f046fe1372288f1598dfa9938b` |
| `topology.json` | `2a36fdc7c6f679355785adc9cefa017f28ce15b9512bffab6113d370db40b6b5` |

The verified bundle manifest has SHA-256 `c0df0f7ae8dfd75d195e0c8625cdda9c72469b439be2936ca22c9d582b3dd788`. The package archive, runtime dependencies and assembly hashes, replay harness, native XML and process logs are retained in that bundle. The combined capability pin and the five package acceptance records are pending. **7.1-P2 remains open and Story 7.1 remains blocked.**

### G-2 correction candidate requalification (2026-11-26)

Both findings reproduced on the pushed baseline and passed on the correction candidate. The [Folders schema test](../../references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FoldersHumanCommandCatalogTests.cs) rebuilds `folders-provision-folder` with internal ingress and checks the other commands with public ingress. The [shared gateway handler](../../references/Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/DomainServiceSafeDenialExceptionHandler.cs) converts the closed trusted denial status into the standalone Folders host's metadata-only problem format. The [signed workload regression](../../references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Authentication/DaprInternalAuthenticationHandlerTests.cs) checks this composition through `POST /api/v1/commands/workload`.

| Input | Exact revision |
| --- | --- |
| Projects base gitlinks | `685eb6c3476c472573f9e223e37e3fd61859a25c` |
| Folders candidate | `20015f0666daaa3c6354024ab06ad4f601da0d03` |
| EventStore candidate | `5bc9e7a901bfdc1b36d2340e7182ce0872ff30e9` |
| Platform | `80a60039b46ea6bdccd9eeac5091551bc59643fa` |
| Builds | `a6abb7908ec38dcb3fbd99d69e7ed29ae25794d2` |

The detached Folders and EventStore candidate commits are retained as local Git bundles against the original pinned parents. The full source tuple, patches, and commitlint output are in `source/` of the evidence bundle.

Both `Hexalith.Folders.EventStoreExtensions` and `Hexalith.Folders.Contracts` were built in Release for `net11.0` at `2.2.0-g2.20015f06`. Their NuGet manifests name the Folders candidate commit and the dependency chain uses released EventStore Contracts `5.6.0` and McpCli.Abstractions `3.0.0`. The archives have SHA-256 `c0961182cfb993e0e911a2c1837156e75cc1d1c5adf67ec15967f9ea5fcda8bc` and `73c09e1ae8865998fe8554cf0d1c2fdf5932601580f5cd3e50a195081c0198cc`, respectively. The installed extension registered as `folders`, API major 1, and its DLL matched the archive at SHA-256 `06ac81e682250726e70cc89143349a43ccd6db8e0a53c552c6df609fcb542a8c`. The installed Contracts DLL matched its archive as well.

The deployment built the EventStore candidate and Folders Server from source in Debug and loaded both Release packages from the isolated NuGet feed. Runtime coordinates were .NET SDK `11.0.100`, .NET and ASP.NET Core `11.0.0`, EventStore assemblies version `5.6.0`, Dapr .NET SDK `1.19.1`, Dapr sidecars and control plane `1.19.2`, Redis `7.4.12` with AOF and synchronous fsync, and Docker `29.8.2`. Both lanes used the image digests recorded above. The exact assembly dependencies and hashes are in `deployment/runtime-deps.json` and `deployment/runtime-assemblies.sha256`.

The isolated deployment used fixture-signed assertions, separate gateway and Folders application-channel tokens, and three Dapr app IDs with no published ports. The fixture ran with Dapr mTLS disabled and seeded Tenant and Organization authority. Its catalog root was `3be80240ab45101ae4b1a2a63aff4829bcc0582fcc3b14ea9949b176698cb456` in both phases. The workload bindings used Tenant `tenant-a` and domain `folders`.

| Caller → receiver | Enrolled route and assertion |
| --- | --- |
| `projects-fixture` → `eventstore` | `POST /api/v1/commands/workload` with `eventstore:command-submit` and internal command `Hexalith.Folders.Commands.ProvisionFolder`. |
| `eventstore` → `folders` | `POST /process` with `domain-service:process`, the committed catalog binding and the gateway-stamped `projects-fixture` origin. |
| `folders` → `eventstore` | `POST /internal/durable-tasks/v1/steps/lookup` with `eventstore:durable-task-step-lookup`. The host grant binds `fixture.activate-project@1`, numbered step 1, the derived target and the current fence. |
| `folders` → `eventstore` | `POST /api/v1/streams/read/workload` with `eventstore:stream-read` and an enrolled Folders public key. The RS256 `X-Hexalith-Bound-Stream-Read-Proof` binds the exact stream and read window. |
| `projects-fixture` → `folders` | `POST /internal/folders/v1/provisioning/lookup` with `folders:provisioning-lookup`. |

All 106 focused Folders Server tests, the focused freeze test, 43 Folders catalog tests and 53 EventStore authentication tests passed with zero skips. The Folders Server Release package-mode build passed. The Projects planning index and guard passed with 60 tests and 442 subtests. Both persisted fixture phases and both isolated packaged deployment phases passed with zero skips.

The packaged worker lost the response after the Folder committed. Before restart the task was `WaitingForDependency` with zero task-side receipts. The harness restarted Redis on the same store and replaced the runner and all three sidecars. With provisioning disabled and mutations frozen, fence 1 returned 404 and fence 2 recovered the original receipt `prov-88f1559fea26250f54067010404384fbc5be9314` for `fld-p-88f1559fea26250f54067010404384fbc5be9314` on task `01M8BTJ8JY8PTX9FXE0F4A7X9E`. The replacement recorded one receipt and executed the owner effect zero times. When the worker submitted a mutation during the freeze, the signed Folders callback and `POST /api/v1/commands/workload` both returned 503. The gateway emitted `application/problem+json` with `code=read_model_unavailable`, `retryable=true` and `details.visibility=metadata_only`. The source fixture recovered `prov-a77ed1440e3646416174cb6714204f31cd73209e` with zero owner execution calls and verified its direct mutation freeze at 503.

Each lane matched the original and recovered receipt, the version-3 Folder history, the incumbent history, and the native stream proof across restart. The Folder grants were `original-actor:manage_folder_access` and `original-actor:read_metadata`. The deployment verified exact equality of all incumbent grants, including inherited `tenant-reader:read_metadata`.

| Matched across restart | Persisted fixture | Packaged deployment |
| --- | --- | --- |
| Folder history SHA-256 | `0e5cd75db8011823112d2c6ba4f41f39cafd4257f9d2c949169087928aa3565d` | `8acf34ee91383028247880732b337e1f805b5ade98502ae65af546e3cda7659d` |
| Incumbent history SHA-256 | `23322cedfe4160cb713fd4df0b466844bf4729ea1b8946998db050a5449e7ed1` | `745689b2768f1b3f0db777de0fdc08eb94759ea132afcf7289fd9cd0878c5dc3` |
| Native prefix digest | `8c7afb6c0217e88de15e10efa00d97137290319909503b503299aba0197ce16f` | `7c5b05814e7e858e2be631a12505d10c9953c6fc852aec079eb703d882b55c29` |

The evidence bundle is `~/.local/state/hexalith-projects/7-1-p2-g2-candidate-8ga5_g7t/`. Its `README.md` records the Release pack and replay commands. The result verifier passed and the 86-file SHA-256 manifest verified at digest `b6684a9dae6cd469f951840503b7a1f099b6f3e76e848bc2fd50959e1aeace15`.

| Retained artifact | SHA-256 |
| --- | --- |
| `gateway/focused.xml` | `0354c9ccf1b7522a2e4bb048212bfc606cb81b72ff0c5c496fbd389e4ad1a5b7` |
| `persisted/focused.xml` | `37ae196df121783fa35990350b8ea6915fb0f68be7a69976515a655db037e545` |
| `persisted/native/before-restart.json` | `b2d5dc96c8e72e0dccfbbe07b5866ccacf074a46c7f6c174c00d07e09efccb52` |
| `persisted/native/before-restart.xml` | `597d3a366516752c2c0161aa36483fe4e3e77c11c07f1239ddc2422364da6178` |
| `persisted/native/after-restart.json` | `a0e12924f6bbba80f3c0b98651296966efef113c13b3d1dc8e7efdb347e88aea` |
| `persisted/native/after-restart.xml` | `80dea9377b8909cd8b79a17f21f130b30efef6a778e1489cc25332b1ed687e4b` |
| `deployment/native/before-restart.json` | `4ba15386efde998a9a7ad20e994342a918332c72d756211ed2e306bff0c9e78a` |
| `deployment/native/before-restart.xml` | `ecb3eb3f96e839902c47d4e3c4c45ec1b86d6a6b0b99579a2fcbd4c9f6ee3431` |
| `deployment/native/after-restart.json` | `d1687f78a3a9dd0a2b7c85221d47eeda70ac2fd9d14f8d66727424783a4e55ea` |
| `deployment/native/after-restart.xml` | `33f3d7ec0b328e3efdbdb41df9795eaf69c47e89d05c1a0cc4781174ffb347fb` |
| `deployment/native/before-restart-wire.json` | `7d95076f75286f0906448b93e25f27753b89914935fd245c03234a1ac4d4ce3f` |
| `deployment/native/after-restart-wire.json` | `f72671e3e78b9482cb6dcd0c8f0d636d24b4683891c02bd6cf31d21817bb5ad1` |
| `persisted/native/topology.json` | `df1aef42a64a11d709abf1c96877a1ad059c94335abc0baf8edb299a98399126` |
| `deployment/native/topology.json` | `16a11ed8566f1e70237ad04f8524a039f1b4dd3c3b12cff267e0032c38fb624e` |

The released provisioning artifact, deployed workload enrollment, combined capability pin and five acceptance records are pending. **7.1-P2 remains open and Story 7.1 remains blocked pending package acceptance.**

### G-2 requalification on the landed corrections (2026-11-26)

Projects `ae081d712fda9426f4b81da2734c631a2a8a14e2` now pins the landed EventStore and Folders corrections. The isolated replay selected those commits and EventStore's Builds, Commons and Tenants inputs from local Git objects, with the other inputs from the documented Projects base. The full tuple is in `source/revisions.txt`.

| Input | Replay revision |
| --- | --- |
| Projects | `685eb6c3476c472573f9e223e37e3fd61859a25c` |
| EventStore | `df6a96ab2a5bae24fe5951eee5429bda223cc675` |
| Folders | `c00bad4d364dc25800cf9dc41220c1eba512fed9` |
| Builds | `deccbc3bcf4ed687ecf4d556ebe356fe57148777` |
| Commons | `59c22162e6b54a39864dbc5f0ba76056045a411f` |
| Tenants | `067ac3fec62b0a607bb512bd75cb1ca0fd780ae7` |
| Platform | `80a60039b46ea6bdccd9eeac5091551bc59643fa` |

The replay ran from the Projects root. For another run, copy the scripts, `source/` and `harness/` to a fresh evidence directory as described in `README.md`.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-reviewed-4VVV9gV0/landed"
run-with-temp bash "$bundle/replay-package.sh" "$PWD" "$bundle"
run-with-temp bash "$bundle/replay-gateway.sh" "$PWD" "$bundle/gateway"
run-with-temp bash "$bundle/replay-pushed.sh" "$PWD" "$bundle/persisted"
run-with-temp bash "$bundle/replay-deployment.sh" "$PWD" "$bundle/deployment"
run-with-temp bash "$bundle/replay-index.sh" "$PWD"
python3 "$bundle/verify-results.py" "$bundle"
(cd "$bundle" && sha256sum --check sha256.txt)
```

The EventStore Debug build passed with zero warnings and errors. All 162 focused EventStore tests, 106 focused Folders Server tests, one direct mutation freeze test, 43 Folders catalog tests and four native restart phases passed with zero skips. The Folders Release package-mode build passed. The active and isolated authority indices returned `[6, 7, 8]`, and the guard passed 60 tests and 442 subtests. The two Release packages at `2.2.0-g2.c00bad4d` name the landed Folders revision in their NuGet manifests, and the deployed extension and Contracts assemblies match the package archives.

Each native lane recovered its original Folder receipt and preserved the version-3 Folder history, native proof, ACL and incumbent Folder history across the Redis restart. Fence 1 was refused with 404 and fence 2 recovered the receipt with zero new owner executions. In the packaged lane, the frozen owner callback and signed workload submission both returned 503. The gateway body used the canonical `service-unavailable` problem type, with `read_model_unavailable`, `metadata_only` and no `detail`. The fixtures used the prior pinned .NET, Dapr and Redis images on private networks with fixture-signed workload assertions.

| Retained artifact | SHA-256 |
| --- | --- |
| `gateway/focused.xml` | `ff235a6b760e87e6757b7a026b46a0383bea82aa5c7c12b73edad97d511d5b5e` |
| `persisted/native/before-restart.json` | `807ffc454d416de72d239a78aa603b40d662477a80873e488de9f7666b3ae5d9` |
| `persisted/native/after-restart.json` | `3fee2c1adc07db92144499bd389bef77b2c7dfcc1e660cac5de2e49fd4a6ab13` |
| `deployment/native/before-restart.json` | `cb20423d281d9d583a0355d2bca929ec50c450448a7e843f45a6fc97dc8d3d23` |
| `deployment/native/after-restart.json` | `7b2d52ad740961ef2a3a04cfb68a7b1416182d88bf5cb00b20505ae2d9521645` |

The 86-file manifest verified at SHA-256 `623f69e759ec76475b72c1916a07bef58b4cf76c4f572d5f7dc9f86449c0c801`. It includes the package archives, full source tuple, replay commands, native XML, topology, runtime assembly hashes and wire receipts.

**7.1-P2 remains open.** Package acceptance still needs the owner-selected G-1 runtime and exercised retained-store rollback, the released G-2 artifact and approved workload enrollment, the Projects activation operation and digest binding, and all five hash-bound owner and architect acceptances.

### G-1 fixture replay on the landed EventStore revision (2026-11-26)

The authenticated Dapr lookup and persisted admission and recovery replay used the same source tuple as the landed G-2 run above, with EventStore at `df6a96ab2a5bae24fe5951eee5429bda223cc675` and Platform at `80a60039b46ea6bdccd9eeac5091551bc59643fa`. The disposable checkouts used committed sources. The crash-recovery assertion source matched its previously recorded SHA-256 byte for byte.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g1-landed-EXuH8kJO"
run-with-temp bash "$bundle/replay.sh" "$PWD" "$bundle" "$bundle/native"
python3 "$bundle/verify-runtime-receipts.py" "$bundle/native"
(cd "$bundle" && sha256sum --check sha256.txt)
```

Use a fresh evidence directory for a repeat run. The two Debug builds each reported zero warnings and errors. All 87 focused tests, six native admission and crash tests, and both execution phases passed with zero skips. After Redis and both Dapr sidecars restarted, the replacement fence recovered the same receipt through the signed lookup and the stale fence returned 404. The persisted crash receipt records one committed owner effect, only an owner lookup during recovery, and identical task row bytes and ETag before and after the stale attempt.

| Retained artifact | SHA-256 |
| --- | --- |
| `native/focused.xml` | `7d4df803442b2ae3254b9083ba7b26c9fa2457d6fa635cb17054a40623b21b1c` |
| `native/admission.xml` | `9713486f62dce76edb26ef264d1e5d63645e109853094591c0820db0c9c23bf2` |
| `native/execution-before-restart.xml` | `bdc94441d678a7346ea53abb6f7e6880abef1c61c19d8ad6edf6a9c64706714d` |
| `native/execution-after-restart.xml` | `61707e7b9ce771c617b276d2f06d8437da48b5a3c819d829d46de327d170a49d` |
| `native/runtime-lookup-receipt.json` | `f54feff8eb9070e848a2cefda407c239d7a905b4e8c1f6390b2c55403fa5b173` |
| `native/crash-recovery-receipt.json` | `c8f3f49943a66986841f9f8f21f41e148de99098435c59f19c301b1eb159aedc` |
| `native/runtime-topology.json` | `be81868cb5a14a685d5abcf600f8f6f0ed8e4a6e9ea675fc1b83271a94896a55` |
| `sha256.txt` | `12a986573218dacedb34e0da2f80371cd6c9f420fe8a03910f0c2b68ef9e6018` |

The 26-file manifest covers the exact source tuple, replay scripts, build logs, native XML and receipts. The owner-selected G-1 runtime and retained-store rollback remain required for package acceptance.

### Joint EventStore and Platform G-1 runtime decision

**Decision requested (2026-11-26)** — Approve the following inputs together for the isolated runtime and retained-store qualification. EventStore owns the task contract and persisted transitions. Platform owns the topology, worker operation and recovery rehearsal.

**Approvals carried forward.** On 2026-11-25 the EventStore and Platform owners authorized their respective implementation and isolated qualification scopes. Both selected committed admission lookup for original-actor binding. On 2026-11-26 the user selected numbered `DurableTaskExecution` steps. Keep the accepted signed `POST /internal/durable-tasks/v1/steps/lookup`, host-owned owner grants, current owner-side actor reauthorization, and step-key deduplication. The records distinguish these approvals from runtime selection and capability acceptance. [EventStore G-1 decision](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md) · [Platform G-1 decision](../../references/Hexalith.Platform/docs/implementation/g1-durable-task-scope-decision.md)

| Choice | Proposed input |
| --- | --- |
| Runtime artifact | Use the landed replay source tuple and source-mode Debug build of the EventStore DomainService runtime at `df6a96ab2a5bae24fe5951eee5429bda223cc675`. Extend the owner-local `Hexalith.EventStore.DurableTaskLookupHost` and Platform runner at `80a60039b46ea6bdccd9eeac5091551bc59643fa` with the qualification changes below. Before starting the exercise, retain the exact owner source diffs or commits and hash the complete host output executed by both replicas and the recovery process. The landed lookup-only assembly is the control. |
| Provider and topology | Use two independently restartable task hosts and Dapr sidecars on a private network, with one retained `g1-task-state` Dapr `state.redis/v1` keyspace using `keyPrefix: name`. Select single-primary Redis 7.4.12 with AOF, `appendfsync always`, `noeviction` and a retained volume. Use strong reads and first-write/ETag compare-and-swap. The selected provider envelope covers a single primary with durable storage in one failure domain. |
| Runtime versions | Reuse the landed digest pins for .NET SDK 11.0.100, .NET and ASP.NET Core 11.0.0, Dapr 1.19.2, Redis 7.4.12 Alpine and the Python probe. See the [runtime image pins](#authenticated-lookup-through-dapr-service-invocation-2026-11-26). Record the effective images and configuration hashes before and after recovery. |
| Authenticated routes | Under `/internal/durable-tasks/v1`, add `POST /confirmation-artifacts/issue`, `POST /admissions`, `GET /tasks/{taskId}` and `POST /tasks/{taskId}/cancel`. Apply EventStore host security. Issue only for a verified interactive Preview with current authority and session/surface binding. Require separate admission, status and cancellation grants. For delegated calls, require the Dapr channel and per-operation signed workload assertions with exact Tenant, domain and original-actor binding. Reauthorize the actor and registered operation against current authority. Preserve the 15-minute opaque artifact and consume it only in the admission CAS. An admitted retry returns the original location after reauthorization. Status denial and absence return the same empty 404. Cancellation uses its own current authorization and the persisted irreversible boundary, with 409 and safe status on conflict. The existing owner step lookup keeps its own scheme and operation. |
| Durable scheduling | Use a Platform worker on each replica. Before the admission CAS, EventStore must confirm durable registration of the reserved task ID in a versioned, fixed set of 16 same-store directory shards and retain the canonical fixture intent under its exact scope and digest. A registration or intent write that cannot be verified blocks admission. Poll the known shards every second and resolve each candidate through the existing task-ID index and committed admission. Ignore uncommitted or losing reservations. The same-store directory allows rediscovery after a lost admission response or complete host restart without passing a task ID to the replacement. Lease CAS chooses one executor. |
| Clock and retry bounds | Use UTC from a common reference, at most 250 ms pairwise skew, and a monotonic timeout for owner calls. Suppress claims and effect dispatch when clock health cannot establish the bound. Use a 15-second nonrenewing lease and wait through persisted expiry plus 250 ms before takeover. Start owner work only if the lease has at least the 10-second step deadline plus the skew margin left. Persist a budget of three charged attempts total per pending step and a 30-second automatic retry window starting at the first attempt. Charge timeouts as dependency failures and check authoritative owner status before any reissue. Exhaustion ends automatic execution in `NeedsAttention`. Under an operator recovery fence, a matching owner receipt can be recovered when an automatic reissue is barred. Check recovery to a committed outcome or truthful `NeedsAttention` within five minutes. |
| Retention | Retain the admission and task-ID index, intent, discovery entries, checkpoint, consumed-artifact binding and owner receipts throughout the rehearsal with no TTL or eviction. Keep nonterminal bindings for their full lifetime. For terminal tasks, keep the idempotency record and referenced outcome for at least 30 days from completion or the longer result obligation. Keep durable audit metadata at least 365 days or the longer applicable event-history obligation. Preserve the original expiry on unused confirmations. [Projects architecture, AD-5, AD-26–28](../planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md) |
| Retained-store target | Use fenced forward recovery with a fresh instance of the same hashed candidate in recovery mode, attached to the original Redis volume and keyspace. The earlier lifecycle revision `388b0ad679d7f9404e10669dd1238381157259c9` predates the persisted attempt charge, so a downgrade can lose it on rewrite. Keep legacy named `DurableTaskRecovery` rows unchanged and verify their refusal by the numbered lookup. |

After approval, qualification will use task-only and confirmation-required admissions and a synthetic owner. Close confirmation issuance, new admission and cancellation on both replicas and drain requests. Interrupt worker A after the owner commits with the response withheld. Stop its host and sidecar and withdraw its access to the fixture store and owner. Restart on the original store with admissions closed, discover the task from persisted state and let worker B acquire a higher fence before resuming the step. Probe the old lease against the task store and the signed owner lookup after takeover. Verify the original task ID, scope and digest, the preexisting checkpoint and receipt prefix, recovery of the withheld receipt by lookup, and exactly one persisted owner effect. Verify the stale attempt leaves task and owner bytes and ETags unchanged, then recheck the committed terminal outcome on both replicas before reopening admissions. Exercise dependency outage and retry exhaustion, clock edges, and cancellation on either side of the irreversible boundary. Retain commands, artifact identities, native XML, persisted row and receipt digests, and the two dated owner decisions.

**Joint input decision (2026-11-26)** — The requesting user approved the full proposal above on behalf of both the EventStore Owner and Platform Owner in this session. This records approval of the inputs and the isolated owner-local changes. The qualification result and combined capability acceptance have separate evidence.

#### Approved G-1 execution slice

Execute this section against the landed fixture baseline. The slice ends when the G-1 native results and joint decision are recorded here. The package-level tasks for G-2 release and enrollment, Projects activation binding, capability pin and combined acceptance stay open.

1. In disposable copies of the exact source tuple, extend EventStore `tests/Hexalith.EventStore.DurableTaskLookupHost/` and the owner-local `src/Hexalith.EventStore.DomainService/DurableTasks/` seams needed by the approved routes, durable registration and timed execution. Keep the existing task-state contract and authenticated numbered lookup. Use a synthetic authority and owner with an atomic persisted effect and receipt. Preserve the committed native crash assertion source byte for byte.
2. Extend Platform `eng/durable-task-qualification/` to run the approved provider and topology. Provide a replay command to build the candidate and run both replicas, then stop admissions, fence worker A and recover using the same artifact and retained volume. Inject a lost owner response and verify durable discovery without handing the replacement a task ID. Keep native readback and authenticated wire receipts for all decisive comparisons.
3. Use `run-with-temp` for disposable sources, builds and store fixture files. Keep the exact candidate patches, clean-checkout replay script, binary and topology hashes, native results and manifest outside the scratch directory in a dedicated local evidence bundle. Record the commands, key results and owner decision in this spec. The selected source and fixture changes belong to their owner paths. No staging, commit, push or tracker change is needed for this slice.

**Status.** The approved G-1 runtime and retained-store rehearsal passed. The [joint input decision](#joint-eventstore-and-platform-g-1-runtime-decision) remains the 2026-11-26 approval from the requesting user on behalf of the EventStore Owner and Platform Owner.

### G-1 retained-store qualification (2026-11-26)

The clean replay used the [landed source tuple](#g-2-requalification-on-the-landed-corrections-2026-11-26). EventStore `df6a96ab2a5bae24fe5951eee5429bda223cc675` supplied the landed lookup-only control and the base for the EventStore candidate. Platform `80a60039b46ea6bdccd9eeac5091551bc59643fa` supplied the base runner. The retained EventStore overlay adds the authenticated admission, Preview confirmation, status and cancellation routes and the opt-in execution timing and receipt-only fencing. The Platform overlay supplies the directory worker and Docker rehearsal. The runtime used the [pinned images](#authenticated-lookup-through-dapr-service-invocation-2026-11-26), including .NET SDK `11.0.100`, .NET and ASP.NET Core `11.0.0`, Dapr `1.19.2`, Redis `7.4.12` and probe Python `3.12.13`. The replicas ran the same hashed candidate against a single retained primary with AOF, `appendfsync always`, `noeviction` and one `g1-task-state` keyspace.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g1-approved-vySfbsep"
run-with-temp bash "$bundle/replay.sh" "$PWD" "$bundle" "$bundle/verified-final"
python3 "$bundle/verify-rehearsal.py" "$bundle/verified-final"
(cd "$bundle" && sha256sum --check sha256.txt)
```

Run from the Projects root and use a fresh results directory for a repeat. All three Debug builds completed with zero warnings and errors. There were 90 focused xUnit tests, six native admission and crash tests, two native control restart tests and five retained-runtime scenario groups, with zero failures or skips. The committed crash assertion source retained SHA-256 `aa929c75be5142940ad9a70e2e5b70e05dccbf8fb5103dca33eaa705afc74a96`. The active Projects authority index returned `[6, 7, 8]`, and the guard passed 60 tests and 442 subtests.

| Case | Verified result |
| --- | --- |
| Admission and scheduling | Equal signed requests on both replicas returned the original task and location. The Preview artifact retained its 15-minute expiry and was consumed with first admission. Wrong-session evidence, changed intent and revoked authority could not create another task. A lost registration acknowledgement left an indexed reservation and intent which both workers ignored. Status denial and absence returned empty 404. |
| Clock, retries and cancellation | Unhealthy clocks suppressed claims or effect dispatch. A nonrenewing 15-second lease required the 250 ms takeover margin and enough time for the 10-second owner deadline. Dependency outage stopped after three charges, and the timed dependency stopped at the 30-second retry window. A partial effect and revoked access to a committed receipt remained in `NeedsAttention`. An operator fence recovered that receipt by lookup after authority returned. Cancellation before the irreversible boundary succeeded. Cancellation after it returned 409 with committed progress. |
| Fenced forward recovery | Both original hosts and task sidecars were withdrawn with mutations closed. Redis restarted on the original volume. The same candidate discovered task `01M8C2RV3F9SDZHKWGCD2FDE07` through the fixed 16-shard directory and committed its terminal status in 17.36 seconds after teardown began. Fence 2 recovered `receipt-abd96f178cdb0770fd4447f76d00ff108c5ed645e08367d9c2769ed5cbb9d765` with execution disabled. The old fence 1 returned empty 404, and stale completion and the stale store CAS returned 409. |
| Retained state | The task ID, original scope, request digest and committed first receipt survived takeover. The checkpoint advanced from 1 to 2 when the replacement appended the recovered receipt. The second owner row stayed at ETag `1` with one effect, and its SHA-256 remained `f63ed8103bde3113f2811510252b0f60afe9acd0d1151cbfab819ed9a9b7a702`. The terminal task and owner bytes and ETags matched before and after the stale probes. Both replicas read the same terminal result before reopening. The named recovery row stayed byte-identical and the numbered lookup refused it before and after restart. Seven admission, index, intent, directory, confirmation and owner keys reported no TTL. |

| Retained artifact | SHA-256 |
| --- | --- |
| `source/eventstore.patch` | `85825f71a757e914e11c7f2efbe08e44441faebd7f1f84973756fe13ac399c4b` |
| `source/platform.patch` | `84e61b0ea9c95f543deaf0ed7393bd5223ac0b7b685c19ff3090c38fd0e2581a` |
| `source/revisions.txt` | `52e55837763348a394696230d4a929a5b476110d49b68eea54033863c5c94712` |
| `replay.sh` | `cb142afcd232e7a86771ea163ba50e18d2426d21bed4421ca3904d906205df72` |
| `verified-final/control-binaries.sha256` | `b8a13e35dc2032307606e66a7b46c021f2d35d93383c4637666a2343e19b4ac5` |
| `verified-final/candidate-binaries.sha256` | `601f0d0a50cc40d1c7aca9240c49cadca222cd5b2dab2c37fa2ec2819ccd277a` |
| `verified-final/focused.xml` | `b1b2486cf0df8e8dab3dcb9749f96164e3d9d8e03cf56008ccc6b68de8d379bc` |
| `verified-final/control/admission.xml` | `5dca740baf3d70edd8d35038c127bdb6f8e4d4ec072acd75e481768e7bd21b8a` |
| `verified-final/control/execution-before-restart.xml` | `755c6c80885e297732472da866eb8f14d6f4a4196062ebb4f305279f02aae50a` |
| `verified-final/control/execution-after-restart.xml` | `1766f72803d1774dd121c4e2396dd5ab9a51fb156dd924bc4d374adf95ee228d` |
| `verified-final/runtime/retained-runtime.xml` | `1fbd2ad53d865d8a0e475580e9393ad4ee2a46acb95886f2a2879ee6fc3bdc9f` |
| `verified-final/runtime/native-state.json` | `297e54ee60b5f47f0af930f3f14e4d2e831eb7c2ab018dd4478dbb0ae53da842` |
| `verified-final/runtime/wire.json` | `3429e5da20cdf8782d91be8044c4ad1fa5820dde5d0ad23b09aa872fbc9e8dcc` |
| `verified-final/runtime/topology.json` | `0725cfa91dfcc3852eb1ad6d69882e74b8051aff25c8de1712cf505bc61803f5` |
| `verified-final/runtime/configuration.json` | `4cca5dd8a776465ec020428e1e3e0ada1185048a4401795881ed47a1866b80c9` |
| `sha256.txt` | `2f013dfe4c8f44527881dba78a10699a48f3f2456bcf58f4ffaeb28c71685f3f` |

**7.1-P2 remains open.** The G-2 release and workload enrollment, Projects activation operation and digest binding, combined capability pin, and five role acceptances are still required.

### Joint G-2 release and workload enrollment decision (2026-11-26)

**Owner decision (2026-11-26).** The requesting user approved the exact Folders release and isolated enrollment below on behalf of the Folders, Platform/realm and EventStore owners. The authorization requires successful exact-source CI and verification of the repository-signed archives before use. Folders owns the release, create and receipt contract. Platform and the realm owner own the workload enrollment and deployment. EventStore owns the G-1 host grant and gateway boundary. The approval below is for those inputs. The five role acceptances for 7.1-P2 follow the Projects activation binding and combined verification.

**Approvals carried forward.** The Folders owner approved implementation and isolated qualification on 2026-11-25, including the committed G-1 original-actor lookup, non-unique display names, create-only semantics, and forward recovery on retained state. The EventStore and Platform owners approved numbered execution, the runtime inputs and retained-store rehearsal recorded above. Both the landed G-2 baseline and the approved G-1 bundle passed their retained evidence verifiers and SHA-256 manifests. Keep version zero, the owner-derived target, current actor checks, the sole-manager ACL without inherited Tenant read, and `NeedsAttention` with the Folder and receipt retained. [Folders owner decision](../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md) · [Landed G-2 replay](#g-2-requalification-on-the-landed-corrections-2026-11-26) · [G-1 qualification](#g-1-retained-store-qualification-2026-11-26)

**Proposed release.** Publish the ordinary Folders `v2.2.0` release from `8c5452c09970420e48deb67918317d6c0dda9f13`. Its parent is the replayed Folders `c00bad4d364dc25800cf9dc41220c1eba512fed9`. The four changed files update the PD10 conformance inventory, generator and tests. The runtime, package inventory, release workflow and root dependency revisions match the landed baseline. The original commit failed exact-source CI because its PD10 inventory omitted provisioning compile inputs. The release workflow requires the approved SHA to be current main with a successful push CI and its protected production reviewer approval. Semantic-release computes `2.2.0` from the provisioning feature after `v2.1.0`. NuGet's six package version indexes were checked and `2.2.0` was absent. If the SHA, tag or package set changes, return to the owners with the new coordinates. [Failed baseline CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45826937192) · [Exact-source CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45836266998) · [Folders release procedure](../../references/Hexalith.Folders/docs/operations/release-packages.md)

| Artifact set | Exact coordinates or SHA-256 |
| --- | --- |
| Landed Contracts baseline | `Hexalith.Folders.Contracts/2.2.0-g2.c00bad4d`, `0b31746e415d4dc0805d7f302d922d9049dcbc7f730d88f808d733053ee735d3` |
| Landed Extensions baseline | `Hexalith.Folders.EventStoreExtensions/2.2.0-g2.c00bad4d`, `46786efe8ff55f5152cef312d4839b514e08d7250409347675ca74a834dd9eaa` |
| Proposed NuGet.org release | `Hexalith.Folders.EventStoreExtensions`, `Hexalith.Folders.Contracts`, `Hexalith.Folders`, `Hexalith.Folders.Client`, `Hexalith.Folders.Aspire` and `Hexalith.Folders.Testing`, all `2.2.0`. GitHub release `v2.2.0` carries the matching six package and six symbol archives. |
| Release verification before enrollment | Prove the release run and tag resolve to the approved SHA, confirm each package ID, version and repository commit, verify NuGet.org repository signatures, match package payloads against GitHub release assets, and record archive and payload hashes. Restore the bindings from those hashed archives. The prepublication build and the NuGet.org repository-signed archives have distinct byte identities. |

**Proposed enrollment.** Use the released packages in an owner-enrolled isolated G-2 qualification deployment. Select the replay Tenant `tenant-a`, admission `fixture.activate-project@1`, private Dapr app IDs `projects-fixture`, `eventstore` and `folders`, and the numbered G-1 lookup from EventStore `df6a96ab2a5bae24fe5951eee5429bda223cc675` with the approved retained-store envelope. Use the Platform `80a60039b46ea6bdccd9eeac5091551bc59643fa` workload-binding provider in a dedicated disposable Keycloak 26.6.4 realm, pinned to image index digest `sha256:0aae0de7fca85525f727d3354df17896092de8bb26ae4c12d89c77e5df8cbce4` and Linux amd64 digest `sha256:22c8cf9e79af88d320499c93994bb2c319ca255092d8ae7c7a45dd46d68192e2`. The Platform provider JAR built with 23 passing tests and has SHA-256 `6e6dcc45ffa350b7f015bf6a51c639d7b603ae115d11d9bd760546bf6836f582`. Use distinct confidential client credentials, TLS, the realm's pinned RS256 signer, and a private Dapr network with channel tokens and exact route ACLs. The signer, grant and catalog readbacks must be bound to the run. Any persistent/shared enrollment requires the owner-nominated target and an independently approved protected manifest. [Platform enrollment contract](../../references/Hexalith.Platform/docs/security/workload-binding-enrollment.md)

| Authenticated caller → receiver | Audience and permitted operation | Binding enforced at the receiver |
| --- | --- | --- |
| `projects-fixture` → EventStore gateway | `eventstore`, `eventstore:command-submit`, `POST /api/v1/commands/workload` | Signed `tenant-a` and `folders`. The effective gateway policy limits this caller to the internal `Hexalith.Folders.Commands.ProvisionFolder` command, its owner-derived aggregate, and expected version 0. Verify other command types are refused. |
| `eventstore` → Folders owner | `folders`, `domain-service:process`, `POST /process` | Existing authenticated callback identity, committed catalog, exact command envelope, and gateway-verified `projects-fixture` origin. The callback uses its own credential. |
| `projects-fixture` → Folders owner | `folders`, `folders:provisioning-lookup`, `POST /internal/folders/v1/provisioning/lookup` | Signed `tenant-a` and `folders`. The owner derives the target from the same committed canonical step and verifies current original-actor authority before disclosing the receipt. |
| `folders` → G-1 host | `eventstore`, `eventstore:durable-task-step-lookup`, `POST /internal/durable-tasks/v1/steps/lookup` | Signed `tenant-a` and `folders`. Register the host grant `(folders, fixture.activate-project@1, 1, folders)` with `FolderProvisioningContract.DeriveFolderId(committed.TenantId, canonicalStepKey)`. G-1 matches `<taskId>/step/1`, the committed operation, digest and Tenant, the derived target, and the positive current fence. The original actor comes from the committed admission. |
| `folders` → EventStore gateway | `eventstore`, `eventstore:stream-read`, `POST /api/v1/streams/read/workload` | Signed `tenant-a` and `folders` plus the separate enrolled Folders RSA bound-stream proof over the exact stream and read window. Require native proof for the receipt and an explicit `missing-stream` decision before first creation. |

Set `Folders:Provisioning:WorkerWorkload=projects-fixture` and `Folders:Provisioning:DurableTaskAppId=eventstore` with the exact Folders and G-1 receiver audiences and callers above. Hash the issuer/receiver and committed route-catalog evidence, including both lookup routes and the native stream-read grant. The target is the `fld-p-` identity derived from the Tenant and canonical step. The v1 request digest is lowercase SHA-256 over UTF-8 `hexalith.folders.provision-folder.v1\n` followed by the trimmed display name. The fixture operation and digest are qualification inputs. Projects will supply its activation operation and digest binding in the next slice.

**Released-package replay after approval.** Extend the landed packaged replay to restore the verified `EventStoreExtensions`, `Contracts` and Folders domain packages and prove the loaded assemblies match. Folders Server is a source-built host at the release revision, and the EventStore runtime and G-1 inputs remain independently pinned. Use the enrolled issuer and receiver policies for the signed operations. Assert current original-actor Tenant and Organization create/read/manage authority before create and current Tenant and explicit Folder read/manage authority before receipt disclosure. Include authority revocation, wrong workload, audience, Tenant, target, admission step or digest, and stale-fence refusals. Withhold the first response after commit, replace the application and sidecars, and restart Redis on its retained store. In the compatible forward-recovery configuration, freeze mutations, prove the old fence yields empty 404, recover the same Folder ID and receipt under the new fence, and verify one create event, two explicit grants for the original actor, zero repeat executions and unchanged Folder history and native proof. Retain the exact commands, source and runtime coordinates, package and enrollment hashes, signed-policy observations, and before/after native XML and JSON hashes.

The approved deployment target is the private qualification environment and its single fixture Tenant. The shared production identity configuration is governed separately.

**7.1-P2 remains open.** The Projects activation binding and combined acceptance follow this release and enrollment slice.

#### Release-source amendment (2026-11-26)

After the owner approved `8c5452c09970420e48deb67918317d6c0dda9f13`, Folders main advanced to its direct child `a13852ad170744ed4806a5acc881524d6f9b102c` and the branch concurrency policy cancelled the approved source's CI. The release workflow requires the current green main. The replacement source changes the Story 12.2 operational status read model and shifts Folders' development EventStore gitlink from `943be126d81fc82761c27d856c3c8d85cbeb2c99` to `8a3536ba56733eeba61b8e831547ca4ab113a0a1`. It leaves the G-2 provisioning handlers, Contracts, Extensions, and release/package-mode inputs intact. The independently approved EventStore replay runtime stays at `df6a96ab2a5bae24fe5951eee5429bda223cc675`. The requesting user approved this amendment on 2026-11-26. The approved release is the same six NuGet packages as `v2.2.0` from `a13852ad170744ed4806a5acc881524d6f9b102c` after its exact-source CI passes. The recorded isolated enrollment and archive verification rule carry forward. [Cancelled CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45836266998) · [Replacement revision](https://github.com/Hexalith/Hexalith.Folders/commit/a13852ad170744ed4806a5acc881524d6f9b102c) · [Replacement CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45841087928)

The superseded `8c5452c0` release dry run from the Projects root ran the exact manifest gate and built four independent package consumers. The Release build passed with zero warnings and errors. The result at `dry-run-result.json` has SHA-256 `3174e71dae54c837351a51d4e1f00b78217ab25bcee19aa6d8e9dadecb1d7ed4`. Its six unpublished packages and symbol archives are recorded in `package-sha256.txt`, SHA-256 `6a5a6895855fe98d2f621bc4364058800f337d87768d914d053071ec69fe55d4`.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-release-preflight-K4IQMXIk"
run-with-temp bash "$bundle/dry-run.sh" ../folders "$bundle"
(cd "$bundle/packages" && sha256sum --check ../package-sha256.txt)
```

**Amended source CI finding.** On the exact `a13852ad` checkout, running `python3 scripts/generate-pd10-v2-conformance-set.py --repository-root "$PWD" --output "$TMPDIR/regenerated.yaml"` succeeded, then `diff -u` against the committed PD10 conformance manifest exited 1. The Story 12.2 code and test changed after their manifest hashes were recorded. The generated refresh changes only the two entries and their aggregate digest. Regeneration of the refreshed candidate matched byte for byte. The blocked-source probe is `a13852ad/pd10-generator-diff.log` in the release bundle. The proposed single-file manifest patch at `a13852ad/pd10-manifest-refresh.patch` has SHA-256 `22960e6b94ece87b04a842810eb6e78999ad33cf9d609f22fc5aac9ea40bd6b5`. The resulting manifest SHA-256 is `6a34fe126f7347eb5b5d1556be556ca5c4582f7dcf60a2924bcc683567c2e2e5`. The requesting user approved landing this single-file refresh and publishing its exact green commit on 2026-11-26. The refresh was committed and pushed as `d3ae49af6a56334a5a3506a56821db45bfaae421`, with parent `a13852ad170744ed4806a5acc881524d6f9b102c`. Regeneration matched byte for byte and commitlint reported zero errors or warnings. That full SHA is the approved Folders `v2.2.0` release source. [Manifest refresh](https://github.com/Hexalith/Hexalith.Folders/commit/d3ae49af6a56334a5a3506a56821db45bfaae421) · [Release-source CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45842112112)

**Documentation-only selection rule.** GitHub cancelled CI for `d3ae49af` after Folders main moved to its child `20025f09cea1c8623c9738607bef6457e6cfa34c`. The only changes were `_bmad-output/implementation-artifacts/deferred-work.md`, `evidence/story-12-2-sm4-transport-20261126.json` and `spec-12-2-durable-projections-and-task-completion-pipeline.md` under that directory. The files are outside the release inputs and PD10 candidate inventory. The current tip regenerated its committed manifest byte for byte. On 2026-11-26 the requesting user approved this exact tip or a successor limited to the same three files. The selected SHA must be recorded and its own push CI must pass before release. The current selection is `20025f09cea1c8623c9738607bef6457e6cfa34c`. [Cancelled refresh CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45842112112) · [Selected release-source CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45843329303)

**Current release selection.** Folders main advanced to `2791d7a82825238da15a1bfb77c544079a7f7d09`. This commit changes only Folders' development `references/Hexalith.EventStore` gitlink, to `4e143993a91e446b116a838f87cf0bcafba70215`. The release builds against centrally pinned NuGet packages and the replay keeps its separately approved EventStore source `df6a96ab2a5bae24fe5951eee5429bda223cc675`. The manifest regenerated byte for byte at the new tip. On 2026-11-26 the requesting user approved adding that gitlink to the release-source selection rule. The selected `v2.2.0` source is `2791d7a82825238da15a1bfb77c544079a7f7d09` and its exact push CI must pass. [Cancelled documentation-only CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45843329303) · [Selected release-source CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45844094422)

#### Approved G-2 enrollment implementation

Prepare the isolated replay harness in `~/.local/state/hexalith-projects/7-1-p2-g2-release-preflight-K4IQMXIk/replay/`. Use the landed bundle at `~/.local/state/hexalith-projects/7-1-p2-g2-reviewed-4VVV9gV0/landed/` as its baseline and the exact approved release source identified above for Folders Server. The release coordinator handles GitHub and NuGet publication and supplies verified archives, provenance and hashes in the adjacent `published/` directory. This implementation slice is the replay harness. Leave the surrounding spec and owner repositories as read-only inputs. Use the workspace's `run-with-temp` wrapper for all disposable checkouts, build inputs, OIDC private material and test outputs. Keep reusable launcher code, sanitized native results and the evidence manifest in the replay directory.

- Adapt the landed `replay-deployment.sh`, `harness/` and `verify-results.py` to accept the signed archive directory and manifest for the six stable packages and record the three loaded Folder package DLLs, the exact EventStore and Folders runtime revisions, and the topology and installed route-catalog hashes. A prepublication package smoke test can use a separate results directory with the dry-run artifacts in the adjacent `packages/` directory. Distinguish those observations from the released-package run.
- Use the pinned and tested Platform provider JAR from the adjacent directory. Install the exact five per-call enrollments in a disposable owner realm, use the SDK workload issuer, verify issuer/TLS, signer and grant metadata, and configure the respective gateway, G-1 and Folders authentication schemes. Trust and credentials are private to this job. Constrain the worker to provisioning and its receipt lookup. Publish sanitized enrollment readback and package/runtime SHA-256 checks alongside the native evidence. Keep synthetic Tenant, actor and Organization data owned by the fixture.
- Exercise the full before/after replay matrix against the retained store and preserve the evidence file names and schema or supply an updated verifier. Verify revoked original-actor authority for create and lookup, explicit ACL read/manage, empty 404 for stale fences, matched Folder ID and receipt across restart, and one create event. Record exact commands and key evidence hashes in the handoff. Run with the final published artifacts when available. Release and enrollment approval do not close 7.1-P2 or include the Projects activation binding and combined acceptance.

#### Prepublication signed enrollment rehearsal

The `a13852ad` Release dry run passed with six package/symbol pairs and four independent consumers. The report and archive manifest in `a13852ad/` have SHA-256 `1927b00f84af6d41bb833e6b18dd33d5041f441c9f0c3332fec2e7369a9f44d4` and `2610454c14d185467127dda6e2dd09db88bee5ae536741ddc86fcad44d3b4e54`. The isolated replay used those local archives and loaded the exact Folders, Contracts and EventStoreExtensions package assemblies. The two native tests and independent verifier passed with zero skips. The verified realm issued all five grants over TLS and Dapr enforced the route ACLs with peer mTLS.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-release-preflight-K4IQMXIk"
run-with-temp bash "$bundle/replay/replay-deployment.sh" "$PWD" ../folders smoke \
  "$bundle/a13852ad/packages" "$bundle/replay/smoke-verified" a13852ad170744ed4806a5acc881524d6f9b102c
python3 -B "$bundle/replay/verify-results.py" "$bundle/replay/smoke-verified"
(cd "$bundle/replay/smoke-verified" && sha256sum --check sha256.txt)
```

Use a fresh result directory when replaying. The actor lost authority during both signed creation and receipt lookup controls. The signed successful lookup reported current version 3, `read_metadata` and `manage_folder_access` for the admitted actor and a current Tenant revision. Revoked requests returned 404. The committed Folder `fld-p-7b3176b6813b79cbc2492f833dd40c28429afde5` and receipt `prov-7b3176b6813b79cbc2492f833dd40c28429afde5` were recovered across the Redis restart under fence 2. Fence 1 returned empty 404 at both lookup routes. The same history held one create event and two explicit grants for the original actor. The replacement executed the effect zero times. The native stream proof and incumbent history matched across restart, and cleanup observed zero owned containers, networks or volumes.

| Retained prepublication result | SHA-256 |
| --- | --- |
| `enrollment.json` | `4c44da618f734163602fb8ba637cb3c97baf244a176bc4f64e115f47e276eb17` |
| `native/before-restart.xml` | `843457709588392acf13d075f10427a6c8b9d1356a03843efca864071b200efc` |
| `native/after-restart.xml` | `d97882b0873fa2c72c2f9aa344dc9f6c27d9c6112aee50c7dac33be6a8eaa62a` |
| `native/before-restart.json` | `202029858e8d8b0da0c2f13fc660d5d499be1a2c78174fa2a2957ba0b22242a4` |
| `native/after-restart.json` | `3b1f0ed1fadc14e2a2c8c017eefae1954a661cf248b5407ab789a2557f8256cb` |
| `native/topology.json` | `92e8b08b6eb6d8b10c14f7a808ba657f0b6794405cc816d51d6421ac797ac0c4` |
| `sha256.txt` | `d2f77c0d9cad85743d95daf9d4dd1e5bbc5f6b468994855a953bb3ab01293b75` |

#### Protected Folders publication

The exact `2791d7a82825238da15a1bfb77c544079a7f7d09` push CI and contract-spine checks passed. The coordinator rechecked the live main, exact source CI and release-run inventory, then dispatched `gh workflow run release.yml --ref main -R Hexalith/Hexalith.Folders`. The Release run recorded the approved source SHA. [Push CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45844094422) · [Contract spine](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45844093834) · [Release run](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45848307610)

The protected production review approved deployment `8089266765`. The Release workflow succeeded, recorded six NuGet.org package uploads and six symbol uploads, and created GitHub release `v2.2.0`. All 12 GitHub assets matched their API SHA-256 digests. The GitHub archive manifest at `2791d7a8/github-release-archives.sha256` has SHA-256 `6e20e2f8ae883e7d48f2353b2f14be1c0783097b4369abd3fb7eae9f37ceaa04`. [Folders v2.2.0](https://github.com/Hexalith/Hexalith.Folders/releases/tag/v2.2.0)

NuGet.org served all six repository-signed archives. The package verifier resolved tag `v2.2.0` and every NuGet repository manifest to `2791d7a82825238da15a1bfb77c544079a7f7d09`. The six `dotnet nuget verify --all` checks passed, and the signed archives' repository payloads matched their corresponding GitHub release assets. The 18-archive manifest `published/published-archives.sha256` has SHA-256 `86c56309fcaaa0e165299afa594a1842ac8107bb83a69ec759b6e2fddd3ba2f0`. The entry-level comparison `published/published-package-payloads.json` has SHA-256 `40d7999d91b4829943e2c829b09be96d2bf9a0c78a37f8b72eff98eb2ecb6997` and the signature result `published/nuget-signatures.log` has SHA-256 `bc429fdbde216479361854f31630fee5b26e63c344b7aa1f57a680af5870f155`.

| NuGet.org package | Version | Repository-signed archive SHA-256 |
| --- | --- | --- |
| `Hexalith.Folders` | `2.2.0` | `5888de77ad30433788817e6f39f45a7aaa02929db3f2c0acdfa360f2f21408fb` |
| `Hexalith.Folders.Aspire` | `2.2.0` | `631a14e2b9e905f61a49ae4ba3e1f2c18cde3f27e4f053cefe735adb1c2dd48d` |
| `Hexalith.Folders.Client` | `2.2.0` | `012e3da5fa9f2099ecaed0ef222750f2512710ae65325604bd1e66c6e5fdfaf3` |
| `Hexalith.Folders.Contracts` | `2.2.0` | `15163c8ad222591fff337a726ac62555af7aba1506e032b37ff7a762cd4c622f` |
| `Hexalith.Folders.EventStoreExtensions` | `2.2.0` | `28d60b95a73bb069d485dfa6b14531707e5b5a49fc4c0ce45db726eb4848c554` |
| `Hexalith.Folders.Testing` | `2.2.0` | `5dcb8746cf113aa440718209b2ced2b90ec9ae148b9fd9cc2cf6f625e47f5ea3` |

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-release-preflight-K4IQMXIk"
run-with-temp bash "$bundle/2791d7a8/verify-publication.sh" 45848307610 "$bundle/published"
(cd "$bundle/published" && sha256sum --check published-archives.sha256)
```

#### Released-package enrollment and retained recovery (2026-11-26)

The approved isolated deployment enrolled the three callers and five grants in the table above. The installed revision-1 grant IDs were `g2-worker-provision`, `g2-worker-lookup`, `g2-folders-step`, `g2-folders-stream`, `g2-gateway-process`. The SDK obtained and verified RS256 assertions over TLS against signer SPKI SHA-256 `1a09f5a365923b604194f61007fe731e40860dc271b1be9b9ca1bc92ce0d5148`. The independent Folder bound-stream key had SPKI SHA-256 `425e922e37f801a1cf67fb74f6b9f649d166668e4a17ced4dc9637eae142ffe9`. Both phases independently read back the same grants, signer and catalog root `3be80240ab45101ae4b1a2a63aff4829bcc0582fcc3b14ea9949b176698cb456`. The exact route ACLs, issuer and receiver policy checks refused workload, audience, Tenant, operation, step, target and digest mismatches. Dapr enforced peer mTLS and distinct receiver channel tokens.

The host built cleanly from Folders `2791d7a82825238da15a1bfb77c544079a7f7d09`. The retained runtime dependencies identify the independently pinned EventStore source and the three NuGet-supplied Folders assemblies. The loaded DLLs matched these entries in the repository-signed `2.2.0` archives.

| Loaded assembly | SHA-256 |
| --- | --- |
| `Hexalith.Folders.dll` | `8b6ae8536da0b16727799fce97296a404ba1751aff48881860e15df0d23b5f25` |
| `Hexalith.Folders.Contracts.dll` | `7d21be2349df4c4359635048eca25e7a061812a7ab16be8b7b9fc7e2392e5049` |
| `Hexalith.Folders.EventStoreExtensions.dll` | `9988b3810469cec8a62ca1c16087ea29cdeab852376f790700fbd7d86ee06100` |

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-g2-release-preflight-K4IQMXIk"
run-with-temp bash "$bundle/replay/replay-deployment.sh" "$PWD" ../folders released \
  "$bundle/published" "$bundle/replay/released" 2791d7a82825238da15a1bfb77c544079a7f7d09
python3 -B "$bundle/replay/verify-results.py" "$bundle/replay/released"
(cd "$bundle/replay/released" && sha256sum --check sha256.txt)
```

The two native xUnit phases passed with zero skips. The signed revoked-actor create returned 404 and the Folder stream remained absent. Receipt lookup with revoked authority returned empty 404. Authorized receipt lookup reported current owner version 3 and verified the original actor's current `read_metadata` and `manage_folder_access` authority and Tenant revision. The ACL contained exactly those two explicit grants for the original actor.

The first worker lost its reply after the Folder committed. Task `01M8C8A8C6ZKFD35ETAF8KE087` retained its pending step with zero task receipts. After Redis restarted on its original AOF volume and the apps and sidecars were replaced, fence 1 returned empty 404 from both G-1 and Folders. Fence 2 recovered the same Folder `fld-p-77ca1d12efb95b7acf5f31b5801e6549ee9cb285` and receipt `prov-77ca1d12efb95b7acf5f31b5801e6549ee9cb285` and appended the receipt once. It executed provisioning zero times. The frozen mutation received 503. The Folder history retained one `FolderCreated` and two `FolderAccessGranted` events. Its SHA-256 stayed `e5a97bb9e26337b5740bdc5e246119e6c3d69c03812df8e8811f79057751dc61` and the native stream digest stayed `cd6eb3006483bf01c239e264430d5e8e502391ee372d69e9ab010d90b6204883`. The incumbent history and effective access also matched. Cleanup verified that the disposable authority and runtime resources were removed.

| Retained released-package result | SHA-256 |
| --- | --- |
| `enrollment.json` | `753db42b0760b5dc16c39de5e8b4faf744df45637c504e2d2ab0824a786681c3` |
| `runtime-assemblies.sha256` | `86bd8749b28ab8565423515c274694c8db4128d6595bbf3f48a570c25230bc1b` |
| `native/before-restart.xml` | `2fd6588e156ed41a02fea8bf3514c41159820808b68056b7f686ec964dad2234` |
| `native/after-restart.xml` | `23a16cea87554f46c3f770772ddbfa5bc786a985fd00445bc519a989c945564b` |
| `native/before-restart.json` | `381e8dbc4c27bf3c516d75bca31acccb62a1720f645664ceabd08863982aba10` |
| `native/after-restart.json` | `c52ef5c461f62815050cd2ec609cde4164df65aab130719044253c87b6df970b` |
| `native/before-restart-wire.json` | `de187992261dc46f535828de290c2f51531ee88e36c8bec7ea09e2a7fe0fab99` |
| `native/after-restart-wire.json` | `6613e71b850c206337b0811461af26682f0d50c900856288aa1505bfc02d7c77` |
| `native/topology.json` | `535c0e65726837a8606191027d9584e9fdfe69cd785b93be4451c65921cba9f0` |
| `native/cleanup.json` | `b9cc6cbe2a29e1919d229db4ac9052d5683cc482ecc0442a422296a97d75d4cb` |
| `sha256.txt` | `33f8444f565365a1c5bd90ff1d137dd4a5285139f41a20d46703b3b158b466c3` |

The independent verifier passed and all 41 manifest entries verified. The approved G-2 release and isolated workload enrollment are complete. **7.1-P2 remains open** for the Projects activation operation and digest binding, the combined capability pin, and the five role acceptances.

### Projects activation binding qualification gap (2026-11-26)

The released G-2 replay enrolled `fixture.activate-project@1` and admitted the Folder display-name digest as the task digest. At the selected EventStore revision, the signed lookup accepts and returns the committed admission digest. Folders 2.2.0 independently requires the digest of the trimmed display name and verifies that the lookup returned the same digest. For fixture name `Shared name`, that digest is `da828e812bde3ae43486b58431c7666d864307f7c3bb36b7a4324fd59887a16b`. The existing Projects create fingerprint computes `sha256:5b1c9810d2a8110bef8b026a11abbf92bae24927879b793d20052d2fcde99738` from the display name and request schema version. The recorded different-digest control returned an empty 404 from G-1. [Committed admission lookup](../../references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DurableTasks/DurableTaskStepLookupExtensions.cs#L74) · [Folders v1 digest](../../references/Hexalith.Folders/src/Hexalith.Folders.Server/Provisioning/FolderProvisioningContract.cs#L29) · [Folders lookup response checks](../../references/Hexalith.Folders/src/Hexalith.Folders.Server/Provisioning/DurableTaskStepLookupClient.cs#L54) · [Projects create fingerprint](../../src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs#L467)

Projects still needs an approved activation operation and canonical admission intent. FR-1 requires same-name Folder creation. Projects accepts trimmed names up to 160 characters, while Folders v1 accepts up to 128. The owners need to decide how the Folder-first path handles names of 129 to 160 characters before admission enables provisioning. The existing Projects aggregate fingerprint covers the display name and schema version, and current tests deliberately treat description and setup metadata changes as equivalent there. AD-5 gives Durable Task admission authority over the activation request's retry equivalence. [FR-1](../planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#L216) · [Projects validation](../../src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs#L120) · [Folders provisioning input checks](../../references/Hexalith.Folders/src/Hexalith.Folders.Server/Provisioning/FolderProvisioningService.cs#L317) · [Projects command equivalence tests](../../tests/Hexalith.Projects.Tests/Replay/CommandDeliveryIdempotencyTests.cs#L121) · [AD-5](../planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#ad-5--adopted-confirmation-required-admission-uses-bound-artifacts-and-scoped-idempotency)

**Proposed for owner review.** Use `projects.activate-project@1` and commit the full normalized Projects activation intent digest for admission and retries. Give the isolated activation grant a trusted resolver that reads the immutable canonical intent under the admitted scope, verifies it against the committed admission digest, and derives the v1 Folder digest for numbered step 1. Have the signed lookup compare and return that step digest for this grant. Preserve the existing lookup grants and the Folders v1 wire format. Projects owns the activation intent and display-name rule, EventStore owns the lookup grant contract, and Platform and Folders own the isolated enrollment and owner verification. Qualification should show that material changes to the activation intent conflict on the same key, that changed Folder input or unavailable canonical intent prevents provisioning, and that the same receipt is recovered after restart and takeover with one Folder create. Verify that the Folder and receipt are retained in `NeedsAttention` when activation cannot commit.

**Proposed name-limit decision.** Extend the Folders-owned provisioning path to accept the same 160-character display-name limit as Projects. Qualify the 128-, 129-, 160- and 161-character boundaries against an isolated successor package. Folders owner approval is required before preparing the candidate. Publication requires its own approval.

The retained G-1 rehearsal and released G-2 enrollment verifiers passed, and their SHA-256 manifests and the published archive manifest matched. The active production-authority index returned `[6, 7, 8]`, and the focused guard suite passed 60 tests and 442 subtests. These checks revalidated the existing receipts. Combined persisted qualification of the Projects activation binding and five acceptances tied to its qualification digest are still pending. The capability pin remains pending and 7.1-P2 remains open.

#### Approved Projects activation rehearsal (2026-11-26)

The requesting user approved both isolated owner scopes in this session. The EventStore and Platform scope covers the opt-in lookup resolver and the replay for `projects.activate-project@1`. The Folders scope covers an isolated successor package for same-name provisioning through 160 characters. Keep the v1 Folder wire shape and apply the wider limit only to owner provisioning. The approved work uses disposable copies through `run-with-temp`. Keep the prior G-1, G-2 and publication evidence intact. Retain new source patches, reusable replay code, exact coordinates and sanitized evidence outside the disposable workspace. Publication and the five final role acceptances are separate decisions.

- [x] Extend the EventStore numbered lookup grant in `src/Hexalith.EventStore.DomainService/DurableTasks/` with an opt-in host resolver. Existing grants continue to require and return the committed admission digest. The Projects grant derives its Folder digest from immutable canonical activation intent after verifying the full digest and admitted scope. Missing or mismatched intent, the wrong digest, and a stale fence yield empty 404. Keep the signed workload, Tenant, operation, step and owner-derived target checks.
- [x] In the Folders owner candidate, extend both the provisioning ingress and aggregate validation paths to preserve the full Project display name through 160 UTF-16 code units. Verify the 128-, 129-, 160- and 161-character boundaries and unchanged generic Folder creation limits. Build the isolated package cohort from the approved release base plus the retained owner patch, with distinct candidate coordinates. Verify package payloads and loaded assemblies in the signed replay.
- [x] In the reusable isolated replay built from the approved Platform and G-2 harnesses, admit validated Projects activation intent under `projects.activate-project@1`. Canonical intent binds schema/classification, display name and material creation metadata, and excludes transport correlation and generated task identity. Use the committed full intent digest for admission equivalence and the independently derived Folder v1 digest for provisioning. Verify equivalent retry, a same-name material change conflict, corrupted or unavailable intent, and mismatched digest refusal. Run signed creation and bound receipt recovery against real persisted state across replica replacement and Redis restart. Show stale-fence refusal, a single owner create, identical committed Folder history across takeover, and retention of the Folder and receipt when activation ends in `NeedsAttention`. Keep Projects writer, AD-35, and shared enrollment outside this slice.
- [x] Retain the replay commands, hashes, test results, scope/intent binding evidence and rollback receipts. Use the existing production-authority guard to verify 7.1-P2 stays open pending released successor coordinates and five matching role acceptances.


#### Isolated Projects activation result (2026-11-26)

The rehearsal used Projects `8faccc8d87f60abd47b1eed0b45b80418b6a0455`, EventStore `df6a96ab2a5bae24fe5951eee5429bda223cc675`, Platform `80a60039b46ea6bdccd9eeac5091551bc59643fa`, and Folders `2791d7a82825238da15a1bfb77c544079a7f7d09`. The replay applied the previously approved G-1 DurableTasks source overlay and the new EventStore lookup patch. The Folders package build used its selected Builds revision `bf8a28b4bbcec619e8d9764d335f786f2bafb69e` and selected Folders base plus the provisioning-only patch. The six unpublished package and symbol pairs used version `2.2.1-activation.p7f23cd4154fe`. The signed replay verified the package inventory and loaded the candidate Folders, Contracts and EventStoreExtensions DLLs byte-for-byte from the staged archives.

Run from the Projects root. Choose fresh output directories for a repeat.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-activation-urvLdvsP"
run-with-temp bash "$bundle/pack-folders.sh" ../folders "$bundle/archives"
run-with-temp bash "$bundle/replay-deployment.sh" "$PWD" ../folders "$bundle/archives/packages" "$bundle/third-run"
python3 -B "$bundle/verify-results.py" "$bundle/third-run"
(cd "$bundle/third-run" && sha256sum --check sha256.txt)
(cd "$bundle/archives/packages" && sha256sum --check ../package-sha256.txt)
run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 tools/planning/validate_production_authority.py --validate-index
run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider
```

The combined Debug build reported zero warnings and errors. Both native phases passed with zero skips. The independent verifier checked the five signed enrollments over TLS, the Dapr peer mTLS and exact route ACLs, and the before/after evidence on the retained Redis volume. The active authority index returned `[6, 7, 8]`. The guard passed 60 tests and 442 subtests. The prior G-1, released G-2 and publication SHA-256 manifests still matched.

| Persisted case | Verified result |
| --- | --- |
| Admission | The approved Projects validator and metadata classifier accepted the fixture under `projects.activate-project@1`. Equivalent intent with new transport identifiers returned task `01M8CE1AH1J5CW3WEP7P5D2QD5`. Same-name changes to description, setup metadata or classification conflicted. The committed full digest was `84b9ce42cfc6fef5d2883d40b0421b6a92120316bba6f4a0e6d5b18997a82e70`. The owner grant compared and returned the separately derived Folder v1 digest `33d529e2f8d8d6ad50fe259bd2f27f5a89df742c40b4213628a2265001249d0d`. The legacy grant returned its committed admission digest. |
| Intent and authority controls | Missing, unavailable, altered and wrongly scoped intent returned empty 404 from G-1. Signed receipt lookup refused unavailable, altered and missing intent, and the control Folder stream stayed absent. A wrong or full-admission digest sent as the owner-step digest returned empty 404. The signed revoked-actor create and receipt lookup refused disclosure or mutation. |
| Name limits | Signed provisioning committed the complete 128-, 129- and 160-code-unit names. Projects admission and the Folders provisioning aggregate rejected 161, and signed owner lookup refused that length. An 80-surrogate-pair boundary confirmed the use of UTF-16 code units. The generic aggregate still admitted 128 and refused 129, 160 and 161. |
| Fenced recovery | The first worker lost the response after commit. Following replica replacement and Redis restart, directory discovery recovered the same task with its exact scope and intent ETag `1`. Both signed lookup routes refused fence 1 with empty 404. Fence 2 appended receipt `prov-2c1bf2a524552a9f7d10d8b7ead4b4ec59ec78a4` for Folder `fld-p-2c1bf2a524552a9f7d10d8b7ead4b4ec59ec78a4` with zero repeat owner executions. A frozen mutation returned 503. |
| Unresolved activation | A synthetic unresolved activation step moved the real persisted task to `NeedsAttention` with the recovered receipt. The retained Folder history had one create and exactly the two original-actor read/manage grants. Its SHA-256 `8fd1875f91c21de90f948863ff5dc517883838aa54ee1b9cd169e3767088bf20`, native proof digest `ecc832ed0f0d2b9927df42613e9c8712a41679239422d3fc18af216d8f917547` and incumbent history all matched before and after restart. |

| Retained input or result | SHA-256 |
| --- | --- |
| `source/g1-approved.patch` | `85825f71a757e914e11c7f2efbe08e44441faebd7f1f84973756fe13ac399c4b` |
| `source/eventstore.patch` | `152bd80fd284cd17b0445ecc54554c3d9edfc53ded09d3806ceb26efe7da2d13` |
| `source/folders.patch` | `7f23cd4154fe41482dde26e710f332454ffe979d1cddf57b75564aca5f6ed2b6` |
| `archives/package-sha256.txt` | `313b094704c76f1e227ab1ff27844e699a1a5302f490c75c600b9f7f3904f9ff` |
| `third-run/harness.sha256` | `a485ece180d1518d529a16c914fb3c1c6f62c6866573f9cacded0a790ca32cc4` |
| `third-run/package-inputs.json` | `0e3e6abcf25c37e80f92bf6be49de9ca90dc7eeac36fdd97ced9bb4eb3170a15` |
| `third-run/runtime-assemblies.sha256` | `cdb7d5fc6f917dbf901cbd7e12c98c365052813f7a1360cd484c4abc41ca44b2` |
| `third-run/enrollment.json` | `851d5ceb22f3cc5a259e8b2f918f701fe5f561e2ea9cfce10fab7916e341f3b7` |
| `third-run/native/before-restart.xml` | `275f08af1a38b8298d8bf5891852baedcce638f814008fa71716e575ffdab5d1` |
| `third-run/native/after-restart.xml` | `da475b969410166d8cc0d841ec9291e7b6345c59b0f2196cfa393e4aed9466a1` |
| `third-run/native/before-restart.json` | `10f99d64ddbec439b880b19c8ecfeb9ffe99891f547ad2da0d32e675cc48245a` |
| `third-run/native/after-restart.json` | `ff8d7fea684d49993b8f94484ea4ca3e722e1ff8f550c0ef2f4f9fd4f8d42e43` |
| `third-run/native/before-restart-wire.json` | `0e5af9b374eb54c4f3de658f90aca4ec45e2fa95ea4fc65de2c49ebcf0dc57d1` |
| `third-run/native/after-restart-wire.json` | `90c9206a612619babf9c4e7d5df3ce038355c55cde12727bc29192e305140150` |
| `third-run/native/topology.json` | `fa31e0bf50ea5b2761e95e118a20e1ba4f913af1d81df3d18acd01fbd8664e9c` |
| `third-run/sha256.txt` | `6f725b06443852da64ec3589fc0ea10c8ee11e9e7fb24ea81ec4c7b5f9f963ac` |

The 39-entry replay manifest verified. **7.1-P2 stays open** pending released successor coordinates, combined capability pin and five matching role acceptances. The activation outcome in this isolated run used a fixture owner for the Project commit step. Story 7.1 still owns the real Projects writer and AD-35 qualification.

#### Folders successor PD10 preflight (2026-11-26)

On Folders `2791d7a82825238da15a1bfb77c544079a7f7d09`, the base manifest regenerated byte for byte. After applying the retained provisioning patch (`7f23cd4154fe41482dde26e710f332454ffe979d1cddf57b75564aca5f6ed2b6`), the manifest comparison exited 1. Regeneration updated the four patched source entries and their aggregate digest. The refreshed manifest regenerated byte for byte in the disposable checkout. The Folders Contracts test project built in Release package mode with zero warnings and errors, and all 21 `Pd10ConformanceSetTests` passed with zero skips.

The verified run retained the SHA-256 manifest, replay launcher and xUnit receipt in `~/.local/state/hexalith-projects/7-1-p2-folders-successor-pd10-CmYFMSVn-repeat-GdSs0nOW/`. Run from the Projects root to reproduce the result.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-folders-successor-pd10-CmYFMSVn"
activation="$HOME/.local/state/hexalith-projects/7-1-p2-activation-urvLdvsP"
results=$(mktemp -d "${bundle}-repeat-XXXXXXXX")
run-with-temp bash "$bundle/replay-pd10.sh" ../folders "$activation/source/folders.patch" "$results"
(cd "$results" && sha256sum --check sha256.txt)
```

| Retained output | SHA-256 |
| --- | --- |
| `pd10-manifest-refresh.patch` | `68d8143540e36b55cddc05f9b776a32cd2945eb8adbdd2e97264a04624605d17` |
| `folders-successor.patch` | `03df021847030ba9cd8b6fd1b4a098a71002e6f7c70c514a4b5e60ef05824c20` |
| Refreshed PD10 manifest | `c2fbfa660218a13d7683a183bb6ab8bcbdaa8a615d8767b9617a04416fa5e95e` |
| `pd10.xml` | `f91bc111e6c6c6b891f390bdf97ad670f02b99c58bc4a228a507332e20e52f77` |
| `sha256.txt` | `2f4d7a38f7877ae25271b3bc658a382951eaa95d34b040fe067aa809a9467c37` |

The retained G-1, released G-2, publication, and isolated activation evidence verifiers and SHA-256 checks passed. The active authority index returned `[6, 7, 8]`, and the guard passed 60 tests and 442 subtests. The refreshed Folders candidate remains local. **7.1-P2 remains open** pending successor release approval, qualification on the released coordinates and five acceptances bound to the combined capability pin.

#### Folders 2.2.1 publication (2026-11-26)

The requesting user approved landing the verified provisioning patch and PD10 refresh and publishing the exact green Folders revision through the protected workflow. Folders main accepted `bf60ba51e7e665cb99db353652c5a5b6112b9b5c`, the direct child of the rehearsed base. Its complete diff matched the verified successor patch. Commitlint returned zero errors and warnings, and the PD10 manifest regenerated byte for byte. [Source](https://github.com/Hexalith/Hexalith.Folders/commit/bf60ba51e7e665cb99db353652c5a5b6112b9b5c) · [Push CI](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45875802404) · [Contract spine](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45875801805) · [Commitlint](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45875802413) · [CodeQL](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45875802386)

Release verified the exact green main source before protected deployment `8093400186` received approval. The workflow reported six successful package uploads and six successful symbol uploads to NuGet.org. GitHub tagged the exact release commit and supplied 12 assets whose bytes matched their API SHA-256 digests. [Protected Release](https://github.com/Hexalith/Hexalith.Folders/actions/runs/45877882978) · [Folders v2.2.1](https://github.com/Hexalith/Hexalith.Folders/releases/tag/v2.2.1)

#### Qualification on published Folders 2.2.1 (2026-11-26)

All six NuGet.org archives passed `dotnet nuget verify --all` with NuGet.org repository signatures. Their payloads matched the corresponding GitHub assets. The combined replay built the Folders Server host from `bf60ba51e7e665cb99db353652c5a5b6112b9b5c` and loaded the exact published Folders, Contracts and EventStoreExtensions assemblies. Projects `8faccc8d87f60abd47b1eed0b45b80418b6a0455`, EventStore `df6a96ab2a5bae24fe5951eee5429bda223cc675` with the two retained activation and G-1 overlays, and Platform `80a60039b46ea6bdccd9eeac5091551bc59643fa` supplied the isolated runtime.

The build completed with zero warnings and errors. Both native phases passed with zero skips. The independent verifier checked five signed grants over TLS, Dapr peer mTLS and exact route ACLs, and the persisted restart controls. Equivalent activation intent returned task `01M8CKDR85Y0VXFGHSYTCNYSD5` and changed material intent conflicted. Signed provisioning preserved all 128, 129 and 160 UTF-16 code units and refused 161. After the lost reply, Redis restarted on its original AOF volume and the worker, apps and sidecars were replaced. Mutation freeze returned 503. Both lookup routes refused fence 1 and fence 2 recovered receipt `prov-ed6e3b0af20b264d905f1c8e523db5d638308f50` with zero repeat owner executions. The task ended in `NeedsAttention` with the same Folder, one create event, two original-actor grants and unchanged native proof and Folder history SHA-256 `113f8988f3ae685075a6cdaef06a655004616e20636f7c7db46b82283a7f71f6`. The Project commit step used the fixture owner.

Reproduce from the Projects root with fresh result paths.

```bash
bundle="$HOME/.local/state/hexalith-projects/7-1-p2-folders-successor-publication-Mv9itxKM"
run-with-temp bash "$bundle/verify-publication.sh" 45877882978 "$bundle/published-repeat"
run-with-temp bash "$bundle/replay/replay-deployment.sh" "$PWD" ../folders "$bundle/published-repeat" "$bundle/activation-repeat"
python3 -B "$bundle/replay/verify-results.py" "$bundle/activation-repeat"
(cd "$bundle/activation-repeat" && sha256sum --check sha256.txt)
```

| Retained output | SHA-256 |
| --- | --- |
| `published/published-archives.sha256` | `6cb4347ca97ed287d7ac90cf57499b3c136c6996b5fddfa6a925ac7126615017` |
| `published/nuget-signatures.log` | `3a6f20ed1967acd39f3fc12f7c333719f50e9c5048dce11ad447ba49bcfbc975` |
| `published-activation/runtime-assemblies.sha256` | `d5c9d23dd07e75bbd1ed305b4a5a01f96ad319b9561d5739fae7d64c0f334c10` |
| `published-activation/native/before-restart.xml` | `0605e199ce9c3abbd6ddbc52b4a7c80148b66dabd5eaa2eaf6d987df31f67c4a` |
| `published-activation/native/after-restart.xml` | `ca740f0b06e2667f0cc03f46fcec4a4affeb48088837ad464db5c5b827903d3a` |
| `published-activation/sha256.txt` | `5e41b1a9a795c81145e8384747de400629b8bd13ae8411f07d8118ad134c6040` |

The 42-entry activation manifest and the retained G-1 verifier passed. The authority guard validated the qualification fields in `qualification-review/qualification.json` and refused the candidate without the five role decisions. The review digest is `24e379218233c378ea466c58f094bf1777903be3021889f2a38c5b4a1be87059`. **7.1-P2 remains open** pending five named acceptances of this digest before recording and linking the capability pin.
