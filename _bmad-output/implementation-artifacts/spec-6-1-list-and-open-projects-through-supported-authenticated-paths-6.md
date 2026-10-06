---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-10-02'
status: 'draft'
route: 'dispatch'
updated: '2026-10-06'
baseline_commit: 'cbcf54fa4d7a8c17bbfc3f9fb555ac0a85c179c0'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Authenticated callers lack supported DomainService list/open reads with authorization-filtered Project truth and the shared admission snapshot.

**Approach:** After the entry gate, add list/open contracts, watermarked models, authenticated handlers, and shadow comparison beside legacy REST. Queries make no Project domain writes.

**Decision:** Use the full Epic 6.1 scope: Chatbot Folder-read filtering, inspection-gated names with durable FR-21 audit, and shadow equivalence.

## Boundaries & Constraints

**Always:** Require P0, usable current P1R, P2, P3, same-baseline architect sign-off, P4 clean-checkout acceptance, complete approved spec, and independent `READY` before runtime work. P4 pins Folder authority, inspection/audit, adapter, and finite normalization contracts. Bind Tenant, original actor, workload, delegation, audience, and action from the authenticated envelope. Authorize before protected validation; reuse platform store/cursor/denial seams; one C# type per file.

**Never:** While `blocked` or `NOT_READY`, do not edit runtime/tests. Do not switch `/api/v1/projects`, hand-roll persistence/cursors, rewrite events, expose unaudited names/protected detail, select candidates, mutate Project state, move pins, or trust caller Tenant headers.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Gate open | Missing prerequisite | No runtime/test/pin edit | Remain blocked |
| Authorized | Current evidence | AD-32 snapshot; default 50/cap 200 list and stable cursor; role-specific visibility | Safe `400` after authorization for structural errors |
| Denied/absent | Denied, cross-Tenant, nonexistent | No protected data | Indistinguishable `404` |
| Degraded | Stale/rebuilding/unknown event | Honest `Partial`/`Unavailable`; no write | `Unavailable` blocks context |
| Shadow | Same authorized query on both paths | Approved canonical result matches | Unexplained delta fails |

**Prerequisite decision (2026-10-03):** Start separate P1R compatibility, replay, and rollback verification for accepted EventStore 3.110.0 / Builds 4.29.1. Story 6.1 remains pending its entry gate.

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — Story blocked; P0/P2/P3/P4 open, P1R unusable, readiness `NOT_READY`; architect signature and `evidence/epic6/6.1-entry-gate.yaml` absent.
- `qualification-evidence/g-6-checkout-refresh-20261003/` beside this spec — passing G-6 at `0f03582b3457a6d9212d60e2f9146a6043af5f7e`; 48 checksums verified. Historical checkout proof cannot qualify published P1R packages or current HEAD.
- `src/Hexalith.Projects/Projections/{ProjectList,ProjectDetail}/` — reuse pure folds and detail Seed. List lacks Folder identity/status. Server's existing setup projection persists aggregate sequence, not global watermark.
- `src/Hexalith.Projects.Server/Queries/` — reuse `ProjectQueryEnvelopePrincipalBinding`, `ProjectContextQueryExecutor` and `GetProjectContextQueryHandler`; preserve legacy endpoints and reuse `AdmissionSnapshot`.
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFolderDirectory.cs` — eventual/task-scoped; actor bearer forwarding unproved. Inspection permission, durable read audit, and list/open shadow normalization lack accepted contracts.

## Tasks & Acceptance

**Execution:**

- [x] `sprint-status.yaml` beside this spec — verify the entry conditions; missing gates prohibit runtime work.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add `ListProjectsQuery`, `GetProjectQuery`, `ProjectListPage`, `ProjectListRow`, and `ProjectOpenResult`; reuse snapshot vocabulary and separate 50/200 paging.
- [ ] `src/Hexalith.Projects.Server/Projections/{ProjectList,ProjectDetail}/` — add persisted handlers/envelopes over existing folds, retaining authoritative global position separately from aggregate sequence; use platform store/write policy.
- [ ] `src/Hexalith.Projects.Server/Queries/` — add list/open handlers and `ProjectReadAuditService`; register in `ProjectsServerModule.cs` and `ProjectsServerServiceCollectionExtensions.cs` using accepted P4 authority/audit seams and platform cursor.
- [ ] `src/Hexalith.Projects.Testing/Reads/` — add `ProjectListShadowComparator` and `ProjectOpenShadowComparator` with only P4's finite normalization rules.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/` and `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTests.cs` — cover the matrix, Folder filtering before pagination, inspection audit, persisted replay/restart, denial/leakage, scoped cursors and zero Project writes.

**Acceptance Criteria:**

- Given accepted gates and current authorized evidence, when list/open executes, then the full frozen scope and AD-32 fields apply; pre-activation tasks and source payloads are absent, and Archived blocks context use.
- Given a Chatbot Project User, when list/open runs, then current Folder-read authorization filters Projects before pagination; rows, counts and scoped cursors reveal only that permitted set, and Tenant-role authority never widens Chatbot visibility.
- Given a Tenant-role caller without descriptive-metadata inspection authorization, when list/open runs, then results contain only Safe Metadata and no Project name. With inspection authorization, authorized descriptive fields require one durable FR-21 event per request recording the inspected Project-set counts and field class without descriptive payload.
- Given a denied, cross-Tenant or nonexistent target, when list/open runs, then the accepted indistinguishable safe-404 contract reveals no protected existence or metadata.
- Given shadow comparison, when an unapproved output/key/watermark/cursor/order/Tenant delta occurs, then qualification fails.

## Implementation Notes

## Spec Change Log

- 2026-10-06: Jerome approved the P1R course correction. Restored explicit Folder-before-pagination, inspection-gated names with durable FR-21 audit, and indistinguishable denial criteria in the non-frozen section. The frozen intent/entry gate, Story status, runtime/tests and pins remain unchanged. These cases become executable only after all existing entry prerequisites pass.

## Review Triage Log

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.1` — exit 0, authority scope passes.
- `python3 tools/planning/validate_production_authority.py --validate-index` — exit 0; does not establish execution readiness.
- After gate acceptance: Debug solution build and individual Server/Integration test projects, then actual persisted evidence through the accepted P0 runner. The proposed `hexalith-module --profile reads` consumer lane is currently absent.
