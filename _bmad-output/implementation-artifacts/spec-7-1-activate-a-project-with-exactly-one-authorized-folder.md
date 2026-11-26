---
title: '7.1: Activate a Project with exactly one authorized Folder'
type: 'feature'
created: '2026-11-26'
status: 'draft'
baseline_commit: '343a78fa17c4fa778fa6daf66b034cb52f131c60'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

An authorized creation request reserves a hidden ProjectId and activates through a Durable Task. The Project becomes visible after an authorized Folder is exclusively claimed, its binding is committed, and the read model confirms activation.

## Boundaries & Constraints

Qualify creation on the SDK writer in the isolated host and two-replica lanes. Story 7.17 owns the deployed writer change. Use current same-Tenant original-actor authority, forward recovery and Safe Metadata audit. Preserve retained v1 behavior and replay of historical `ProjectFolderCreationPending`. Positive supplied-Folder integration requires Story 7.16 Selection Evidence. The [canonical story](../planning-artifacts/epics.md#story-71-activate-a-project-with-exactly-one-authorized-folder) defines the complete acceptance matrix.

## I/O & Edge-Case Matrix

| Case | Expected result |
| --- | --- |
| Authorized creation with no Folder | Create one Folder using the expected owner version and deterministic task-step key. The original actor is sole manager with no Tenant-default read. Persist the owner receipt before advancing. |
| Supplied Folder | Reject a Service/Workflow Caller. Return `RequestPreview` for a Project User without valid Selection Evidence. Foreign, reserved or claimed Folders fail closed. |
| Invalid creation metadata | Preserve the P1 `400 ValidationFailure` contract without echoing input, admitting a task or submitting a domain command. |
| Retry, lost reply or dependency failure | Equivalent retries recover the admitted task and step receipts. Changed requests conflict. Resolve unknown effects through authoritative status. Keep unresolved post-provisioning work `NeedsAttention` with the Folder and receipt retained. |

</frozen-after-approval>

## Open Questions

1. Which SDK host candidate will Projects and EventStore accept for DW-63? This draft also requires the Epic 7 readiness gate.

## Code Map

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` and `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` provide authorization before parsing and P1 validation.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` registers the keyed retained processor. `src/Hexalith.Projects.Contracts/Routing/ProjectsRuntimeCatalogDeclarations.cs` supplies SDK routing declarations.
- `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs` emits the historical creation event pair. `ProjectStateApply.cs` and the list and detail projections fold it. The historical corpus is in `tests/Hexalith.Projects.Tests/SchemaEvolution/`.
- `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs` provides persisted owner and read fixture seams. The creation workflow consumes the accepted P2 task and Folder interfaces.

## Tasks & Acceptance

**Execution**

- [ ] `tests/Hexalith.Projects.SdkWriter/` and `Hexalith.Projects.slnx` — add a test-only SDK Project and claim aggregate composition. Declare `Tenant/project-folder-claims/FolderId` in the fixture catalog and enforce one writer per domain.
- [ ] `src/Hexalith.Projects.Contracts/Events/ProjectCreated.cs` and `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs` — emit one bound `ProjectCreated` on the SDK writer, accept absent bindings on historical replay, and fold the creation-abandoned fence without exposing reserved identities. Update the retained and supported folds. Apply AD-24 to any touched Server projection adapters.
- [ ] `tests/Hexalith.Projects.SdkWriter/` — connect P1 admission and P2 provisioning, verify current authority before the versioned Project commit, recover the exact owner and claim receipts by task and step, and establish the abandonment fence before releasing an exact-generation reservation.
- [ ] `tests/Hexalith.Projects.Integration.Tests/` and `_bmad-output/implementation-artifacts/evidence/epic7/7.1-folder-first-writer-capability.json` — extend the persisted host and two-replica fixtures. Record the accepted candidate, authorization, replay, fencing and recovery evidence for Story 7.16.

**Acceptance Criteria**

- Given a valid admission, when a worker resumes at any provisioning, commit or projection boundary, then event and read evidence shows a single bound Project/Folder pair and visibility only after confirmed activation.
- Given a race for one Folder, when the winner loses its reply and another replica resumes, then claim and Project histories show one Active holder. A stale generation cannot alter a newer holder, and an abandoned Project cannot activate after its fence.
- Given incumbent or conflicting legacy histories, when the fixture reconstructs claims, then it preserves incumbents, closes uncertain keys and quarantines conflicting Projects without context. Historical pending-Folder events remain replayable.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- The readiness rerun records `NOT_READY`. The sprint tracker records Story 7.1 `blocked` and both prerequisites `done`. The [7.1-P2 capability pin](evidence/epic7/7.1-P2-capability-pin.json) records the accepted Durable Task and Folder inputs. DW-63 awaits owner acceptance. [Readiness rerun](../planning-artifacts/implementation-readiness-report-2026-11-25.md) · [Sprint tracker](sprint-status.yaml) · [DW-63 ledger](deferred-work.md)
- At the baseline's committed gitlinks, the isolated G-1 fixture passed 87 focused tests, two Dapr/Redis restart phases and six admission and crash-recovery tests. It recovered the committed receipt under the replacement fence with one persisted synthetic owner effect. The authority index and its 60 guard tests passed. The P2 pin records the later runtime and Folder qualification and five owner and architect decisions.
- After entry acceptance, use the governed `dotnet tool run hexalith-module test --profile durable --filter Story=7.1` lane with Dapr 1.19.2 or later for the claim-race proof, or the recorded owner risk acceptance. Pin the G-4 runner before running this lane.
