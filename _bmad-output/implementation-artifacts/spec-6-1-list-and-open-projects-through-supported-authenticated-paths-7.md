---
title: 'Story 6.1: Supported authenticated Project list and open'
type: 'feature'
created: '2026-10-13'
updated: '2026-10-16'
status: 'draft'
route: 'dispatch'
baseline_commit: '2b7edae27175fa5921de5768d9580e16d615db34'
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

## Open Questions

1. Start separate P1R tooling hardening in EventStore (nine deferred gaps; no landing/publication), or supply accepted prerequisite packets?

## Code Map

- `src/Hexalith.Projects.Contracts/Queries/AdmissionSnapshot.cs` and `PageRequest.cs`: AD-32 exists; paging is 25/100; list/open requires 50/200.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` and `GetProjectContextQueryHandler.cs`: reuse binding/store patterns; server actions remain separate. Preserve legacy routes.
- `src/Hexalith.Projects/Projections/{ProjectList,ProjectDetail}/`: reuse pure folds. Aggregate sequence is not the global watermark; platform list index is missing.
- `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs`: comparator pattern; P4 must approve finite list/open normalization.

## Tasks & Acceptance

**Execution:**

- [ ] `evidence/epic6/6.1-entry-gate.yaml`: verify the frozen chain; P4 must pin Folder, inspection/audit, adapter, list index/global watermark, cursor and normalization contracts.
- [ ] `src/Hexalith.Projects.Contracts/Queries/{ListProjectsQuery,GetProjectQuery,ProjectListPage,ProjectListRow,GetProjectResponse}.cs`: add approved metadata contracts.
- [ ] `src/Hexalith.Projects/Projections/{ProjectList,ProjectDetail}/`: add supported persistence around existing folds; reject unknown relevant events.
- [ ] `src/Hexalith.Projects.Server/Queries/{ListProjectsQueryHandler,GetProjectQueryHandler}.cs`: implement binding, Folder filtering before paging, inspection audit, scoped platform cursors and safe denial. Register in `ProjectsServerModule.cs` and `ProjectsServerServiceCollectionExtensions.cs`.
- [ ] `src/Hexalith.Projects.Testing/Reads/ProjectReadShadowComparator.cs`: compare identical authorized inputs, keys, watermarks, ordering and cursor boundaries.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/{ListProjectsQueryHandlerTests,GetProjectQueryHandlerTests}.cs` and `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTests.cs`: cover the frozen matrix, serialization, persisted replay/restart/faults and real G-4 reports.

**Acceptance Criteria:**

- Given authorized enumeration, when listing, then Chatbot Folder filtering precedes pagination; Tenant-role responses omit names unless inspection-authorized, with one durable FR-21 audit per inspected request recording counts and field class.
- Given any read, when effects are inspected, then only required metadata security/inspection audit may persist; no Project or sibling mutation occurs.
- Given shadow/replay/restart checks, when executed on the accepted fixture, then persisted evidence proves equivalence and every unexplained delta fails.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- 2026-10-16: `python3 tools/planning/validate_production_authority.py --story-id 6.1` passed (exit 0, scheduling only). P0/P2/P3/P4 open; P1R unusable; signature/entry packet absent; `NOT_READY`.
- Recovery complete: `spec-6-1-p1r-5.md`, EventStore `evidence/6-1-p1r-recovery-20261015/README.md`. Nine tooling gaps remain in `deferred-work.md`; packages remain unqualified.
- After acceptance: Debug build and individual test projects; `dotnet tool run hexalith-module test --profile reads --filter Story=6.1`. Tool manifest/profile and real reports are still missing.
- Planning checks: frozen bytes and sprint/acceptance unchanged; `git diff --check`.
