---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-10-02'
status: 'ready-for-dev'
route: 'dispatch'
updated: '2026-10-10'
baseline_commit: 'fc565a9771d719f3d705d5d6ec121d11242317a6'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
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

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — blocked; P1R unusable; P0/P2/P3/P4 open; `NOT_READY`.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextQueryHandler.cs`, `ProjectQueryEnvelopePrincipalBinding.cs`, `ProjectContextQueryExecutor.cs` — query/auth/store patterns.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — safe authorization; `ProjectsDomainServiceEndpoints.cs` preserves legacy routing. `ProjectsServerModule.cs` and `ProjectsServerServiceCollectionExtensions.cs` register queries.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` — verify gate; leave runtime/tests/pins untouched.
- [ ] `evidence/epic6/6.1-entry-gate.yaml` — verify accepted P0/P1R/P2/P3/P4, architect sign-off, rollback, approved spec, independent `READY`.
- [ ] `src/Hexalith.Projects.Contracts/Queries/ListProjectsQuery.cs`, `GetProjectQuery.cs`, `ProjectListPage.cs` — add metadata-only contracts, list-only 50/200 paging; reuse `AdmissionSnapshot`.
- [ ] `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`, `ProjectDetail/ProjectDetailProjection.cs` — reuse folds in platform-backed incremental models; persist watermarks and classify events.
- [ ] `src/Hexalith.Projects.Server/Queries/ListProjectsQueryHandler.cs`, `GetProjectQueryHandler.cs` — add envelope-bound reads with Folder filtering before paging, inspection audit, scoped cursors, freshness, and safe denial.
- [ ] `src/Hexalith.Projects.Server/ProjectsServerModule.cs`, `ProjectsServerServiceCollectionExtensions.cs` — register handlers beside legacy routes.
- [ ] `src/Hexalith.Projects.Testing/Reads/ProjectReadShadowComparator.cs` — reject differences outside P4 normalization.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/ListProjectsQueryHandlerTests.cs`, `GetProjectQueryHandlerTests.cs`, `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTests.cs` — cover denial, replay/restart, leakage, cursors, zero writes, G-4 evidence.

**Acceptance Criteria:**

- Given current authorization and evidence, when list/open runs, then only visible AD-32 Safe Metadata returns with 50/200 paging, inspection audit, and Archived context exclusion.
- Given denied, cross-Tenant, malformed, or absent identity, when open runs, then safe `404` is indistinguishable; authorized invalid filter/page/cursor gets metadata-only `400`.
- Given stale, incomplete, rebuilding, or unknown-event evidence, when read runs, then honest `Partial`/`Unavailable` blocks unusable context.
- Given any read, when effects are inspected, then no Project write, task, repair, sibling mutation, or candidate exists; only required metadata-only audit may persist.

## Implementation Notes

## Spec Change Log

- 2026-10-06: Jerome approved the P1R course correction. Restored explicit Folder-before-pagination, inspection-gated names with durable FR-21 audit, and indistinguishable denial criteria in the non-frozen section. The frozen intent/entry gate, Story status, runtime/tests and pins remain unchanged. These cases become executable only after all existing entry prerequisites pass.

## Review Triage Log

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.1` — scheduling scope only.
- After gate acceptance: `dotnet build Hexalith.Projects.slnx --configuration Debug`; run Server and Integration test projects separately; `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` — pass with actual `.trx`, JSON, shadow report.
- `git diff --check` — pass.
