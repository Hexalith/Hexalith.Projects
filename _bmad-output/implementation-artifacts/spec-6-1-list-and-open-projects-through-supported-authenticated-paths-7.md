---
title: 'Story 6.1: Supported authenticated Project list and open'
type: 'feature'
created: '2026-10-13'
updated: '2026-10-15'
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

## Open Questions

1. Recover the reviewed October 14 P1R source/evidence bundle (provide its path), or prepare fresh prerequisite work against canonical EventStore main? The current checkout lacks both that bundle and 3.122.1 enrollment. Fresh work needs new review/qualification; publication and owner acceptance remain separate gates.

## Code Map

- `src/Hexalith.Projects.Contracts/Queries/AdmissionSnapshot.cs`: reuse AD-32 vocabulary.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs`: reuse authenticated binding; handlers belong here.
- `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs`: comparison pattern; list/open requires approved normalization.

## Tasks & Acceptance

**Execution:**

- [ ] `evidence/epic6/6.1-entry-gate.yaml`: validate the frozen prerequisite chain, contracts, normalization and independent `READY`.
- [ ] `src/Hexalith.Projects.Contracts/Queries/{ListProjectsQuery,GetProjectQuery,ProjectListPage,ProjectListRow,GetProjectResponse}.cs`: add one type per file using approved metadata.
- [ ] `src/Hexalith.Projects/Projections/{ProjectList/ProjectListProjection,ProjectDetail/ProjectDetailProjection}.cs`: reuse folds in platform-backed models with watermarks and explicit event classification.
- [ ] `src/Hexalith.Projects.Server/Queries/{ListProjectsQueryHandler,GetProjectQueryHandler}.cs`: implement binding, Folder filtering before paging, inspection audit, platform cursors, safe denial and freshness.
- [ ] `src/Hexalith.Projects.Server/{ProjectsServerModule,ProjectsServerServiceCollectionExtensions}.cs`: register both queries beside legacy routes.
- [ ] `src/Hexalith.Projects.Testing/Reads/ProjectReadShadowComparator.cs`: compare identical inputs, keys, watermarks, ordering and cursors.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/{ListProjectsQueryHandlerTests,GetProjectQueryHandlerTests}.cs` and `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTests.cs`: verify the frozen matrix and persisted replay/restart; emit actual G-4 reports.

**Acceptance Criteria:**

- Given any read, when effects are inspected, then only required metadata security/inspection audit may persist; no Project or sibling mutation occurs.
- Given the accepted fixture, when shadow and replay/restart checks run, then actual reports establish equivalence and every unexplained delta fails.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- 2026-10-15 root `42c8e7bc3a58d78e7c6ea4273711d697e81175e7`: `python3 tools/planning/validate_production_authority.py --story-id 6.1` passed, exit 0 (scheduling scope only).
- P0/P2/P3/P4 remain open, P1R unusable, architect signature pending, entry-gate absent, readiness `NOT_READY`.
- P1R source-only report references missing owner evidence `evidence/6-1-p1r-successor-enrollment-20261014/`. Current clean EventStore HEAD is `5b0df296abc86adf8d37a92fae02e26574d19ad5`; finite guards lack 3.122.1. Read-only `gh api` confirms canonical main remains `210b90da172cc7ca946fbb06cfd44390bffa707b` and latest GitHub release is v3.122.0. This does not establish package qualification or acceptance.
- After acceptance: Debug build; test projects separately; `dotnet tool run hexalith-module test --profile reads --filter Story=6.1`; retain actual reports.
- `git diff --check` must pass. Planning only.
