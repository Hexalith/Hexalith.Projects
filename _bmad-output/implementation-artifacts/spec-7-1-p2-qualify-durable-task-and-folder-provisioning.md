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

## Spec Change Log

## Review Triage Log

## Verification

- `python3 tools/planning/validate_production_authority.py --validate-index` must pass for the active index and the isolated candidate using the four project-context path overrides.
- `run-with-temp python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider` must pass before recording a tracker change.
- Active index and isolated candidates passed `--validate-index`. The isolated command supplied `--sprint-status`, `--deferred-work`, `--p0-artifact`, and `--workspace-root`. The synthetic done candidate was rejected with exit 1 after its retained recovery receipt changed.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m pytest tests/tools/test_production_authority_guard.py -q -p no:cacheprovider` passed 58 tests and 442 subtests.
- The baseline command `aspire start --non-interactive --isolated --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` ran under `run-with-temp` and exited 2 during build. `references/Hexalith.Conversations/Directory.Build.props(9,5)` requires the Platform tool-selected source mapping. Runtime qualification still requires the actual owner capabilities and fixtures.
