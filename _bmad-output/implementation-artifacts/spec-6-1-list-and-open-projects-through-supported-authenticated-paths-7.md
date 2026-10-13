---
title: 'Story 6.1: Supported authenticated Project list and open'
type: 'feature'
created: '2026-10-13'
status: 'draft'
route: 'dispatch'
baseline_commit: '4b72d845f5d0e731913d20a259d6e406dbe5ff80'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
implementation_gate: 'blocked'
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

**Decision (2026-10-13):** The user selected separate P1R prerequisite work. Story 6.1 remains blocked.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Contracts/Queries/AdmissionSnapshot.cs` — reuse approved snapshot semantics.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` — binding pattern; P4 approves authorization and placement.

## Tasks & Acceptance

**Execution:**

- [ ] `evidence/epic6/6.1-entry-gate.yaml` — verify accepted P0/current usable P1R/P2/P3, architect sign-off, P4, rollback, adapter/inspection contracts, finite normalization and independent `READY`; never fabricate approvals.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add `ListProjectsQuery.cs`, `GetProjectQuery.cs`, `ProjectListPage.cs`, `ProjectListRow.cs`, `GetProjectResponse.cs`; reuse the shared snapshot and exact metadata allowlist.
- [ ] `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`, `ProjectDetail/ProjectDetailProjection.cs` — reuse incremental folds in platform-backed envelopes; retain authoritative watermarks and explicit event classification.
- [ ] `src/Hexalith.Projects.Server/Queries/ListProjectsQueryHandler.cs`, `GetProjectQueryHandler.cs` — implement approved binding, Folder filtering before paging, inspection audit, platform cursors, safe denial and honest freshness.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerModule.cs`, `ProjectsServerServiceCollectionExtensions.cs` — register and advertise both queries beside legacy routes.
- [ ] `src/Hexalith.Projects.Testing/Reads/ProjectReadShadowComparator.cs` — compare identical inputs, keys, watermarks, ordering and cursors; reject unexplained deltas.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/ListProjectsQueryHandlerTests.cs`, `GetProjectQueryHandlerTests.cs`, `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTests.cs` — verify the matrix, isolation, leakage, cursor security and persisted replay/restart; emit G-4 reports.

**Acceptance Criteria:**

- Given current authorized evidence, when list/open runs, then only allowlisted AD-32 metadata returns with stable 50/200 paging, role-specific visibility and Archived context exclusion.
- Given denied/cross-Tenant/absent identity, when open runs, then safe `404` is observationally indistinguishable; authorized invalid filters/pages/cursors receive metadata-only `400`.
- Given degraded required evidence, when an authorized read runs, then `Unavailable` blocks context; `Partial` requires current Project/Folder/Setup/authorization and declares all optional omissions.
- Given any query, when effects are inspected, then only required metadata security/inspection audit may persist; no Project write, task, repair, sibling mutation or candidate occurs.
- Given the accepted persisted fixture, when shadow and replay/restart checks run, then all unexplained differences fail and actual results establish equivalence.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.1` passed, exit 0: scheduling scope only.
- 2026-10-13: P0/P2/P3/P4 open, P1R unusable, architect signature pending, entry-gate absent, independent `NOT_READY`. P3 implementation is in review; owner acceptance blocked.
- After acceptance: Debug build; Server/Integration tests separately; approved `dotnet tool run hexalith-module test --profile reads --filter Story=6.1`; retain actual TRX/JSON/shadow reports.
- `git diff --check` must pass.
