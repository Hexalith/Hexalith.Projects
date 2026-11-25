---
title: '7.1-P2: Qualify Durable Task admission and Folder provisioning'
type: 'chore'
created: '2026-11-25'
status: 'in-progress'
baseline_commit: 'fdc8ca69ebe0a656f88d55f9e32c8de71b1262f6'
route: 'dispatch'
review_loop_iteration: 0
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
- G-1 still needs authenticated task routing, including the route for the committed step lookup, a scheduler with durable discovery, reconciliation from `NeedsAttention`, admission and index retention, an owner-selected operating envelope with the failure budget value, and exercised rollback. G-2 can reuse the Folder stream's absent-stream commit, ACL projection and current owner-read seams. On 2026-11-25 EventStore, Platform and Folders selected the committed admission lookup to bind the original actor for delegated steps. Folders kept display names non-unique, so a collision is an existing Folder at the derived identity or a changed request under the same step key. Its rollback after restricted provisioning is forward-only. [EventStore](../../references/Hexalith.EventStore/docs/architecture/g1-durable-task-scope-decision.md#original-actor-binding-2026-11-25) · [Platform](../../references/Hexalith.Platform/docs/implementation/g1-durable-task-scope-decision.md#original-actor-binding-2026-11-25) · [Folders](../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md#decisions-2026-11-25) The approved scope specifies retaining the Folder and receipt for unresolved activation.
- The package requires immutable owner capability records, the full persisted matrix and rollback, and all five hash-bound acceptance decisions before linking a pin.

- EventStore main also carries a parallel one-task crash recovery slice, `3475b097857b71d4cc25d53f0dce810e7a7fc928`, with `DurableTaskRecovery` and a prepared step on the same admission row. Its live test `WorkerCrashResumesCommittedCheckpointWithOneOwnerEffect` already fails at EventStore `9d24e05d8` before the merge, because it compares `ImmutableArray` receipts by reference. The merged tree builds with zero warnings, all 87 non-live Durable Task tests pass, and the five other admission live tests and both execution phases pass. The decision record lists the choice of one step-execution path as still open.

- The live crash rehearsal now passes with an EventStore test assertion change. `DurableTaskRecovery` prepares a named step with a hashed key. `ResolveStepAsync` parses the numbered keys used by `DurableTaskExecution` and reads progress from the shared task row. G-2 needs a selected step model and authenticated lookup to derive the stable Folder identity and verify the admitted actor and request. The owner entry point needs a create-only commit that retains the restricted ACL and receipt with the Folder history. [EventStore crash test](../../references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/DurableTasks/DurableTaskAdmissionLiveTests.Recovery.cs) · [Folders scope decision](../../references/Hexalith.Folders/docs/governance/g2-folder-provisioning-scope-decision.md)

## Spec Change Log

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
