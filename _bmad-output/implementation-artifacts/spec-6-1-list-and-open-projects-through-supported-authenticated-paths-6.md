---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-10-02'
status: 'draft'
route: 'dispatch'
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

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `blocked`/`NOT_READY`; P0/P2/P3/P4/G-6 open, current P1R unusable. `_bmad-output/planning-artifacts/epics.md` governs scope.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`, `Authorization/ProjectAuthorizationGate.cs`, `Queries/ProjectQueryEnvelopePrincipalBinding.cs` — preserve legacy reads; reuse dual-principal gate.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`, `ProjectDetail/ProjectDetailProjection.cs`, `src/Hexalith.Projects.Contracts/Queries/AdmissionSnapshot.cs` — reuse folds and AD-32; platform owns store/cursor/denial.
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFolderDirectory.cs` — eventual/task-scoped, actor bearer unproved; list lacks Folder ID. Audit sink and G-5 inspection permission absent. `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs` is a pattern only.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml`, `evidence/epic6/6.1-entry-gate.yaml` — verify exact P0/P1R/P2/P3/P4, G-2/G-5/G-6, Folder/audit/normalization, architect and `READY` gates; stop while absent.
- [ ] `src/Hexalith.Projects.Contracts/Queries/{ListProjectsQuery,GetProjectQuery,ProjectListPage,ProjectListRow,ProjectOpenResult}.cs` — add one type per file; reuse `AdmissionSnapshot`.
- [ ] `src/Hexalith.Projects.Server/Projections/{ProjectList/ProjectListProjectionHandler,ProjectDetail/ProjectDetailProjectionHandler}.cs` — persist incremental folds and authoritative watermarks.
- [ ] `src/Hexalith.Projects.Server/Queries/{ListProjectsQueryHandler,GetProjectQueryHandler}.cs`, `ProjectsServerModule.cs`, `ProjectsServerServiceCollectionExtensions.cs` — register authorization-first handlers with platform cursor, safe denial, and freshness.
- [ ] `src/Hexalith.Projects.Server/Folders/FoldersProjectFolderDirectory.cs`, `Authorization/ProjectAuthorizationGate.cs`, `Queries/ProjectReadAuditService.cs` — apply current Folder authority before paging; gate names on distinct inspection permission and one durable metadata audit, failing closed.
- [ ] `src/Hexalith.Projects.Testing/Reads/{ProjectListShadowComparator,ProjectOpenShadowComparator}.cs` — compare both paths using only P4-approved normalization; fail unexplained deltas.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/{ListProjectsQueryHandlerTests,GetProjectQueryHandlerTests}.cs`, `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTests.cs` — verify matrix, persisted replay/restart, audit, leakage, and zero Project writes.

**Acceptance Criteria:**
- Given accepted gates and an authenticated caller, when list/open runs, then Tenant-scoped AD-32 snapshots, default 50/cap 200 paging, and no Project write result.
- Given an authorized result, when returned, then list rows include lifecycle, version, Folder availability, and per-row state; open includes typed Setup and Projects-owned reference summaries. Pre-activation tasks and source payloads are absent; Archived blocks context use.
- Given a Chatbot Project User, when list/open runs, then only current Folder-readable Projects appear after filtering before pagination; non-current Folder evidence is `Unavailable`.
- Given a Tenant-role caller, when inspection permission is absent/present, then names are hidden/shown with one durable FR-21 audit of inspected-set counts and field class; unavailable audit fails closed.
- Given a denied/absent target, when open runs, then status/body/logs/telemetry reveal no existence distinction.
- Given matching legacy/supported queries, when shadow comparison runs, then unapproved output/key/watermark/cursor/order/Tenant deltas fail.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Current Folder calls are eventual/task-scoped without proved actor bearer forwarding; inspection permission and durable audit sink are absent; legacy list lacks cursor/AD-32. These are external gates. FR-21 audit is the only durable read side effect.

## Verification

**Commands:** After gate acceptance, build Projects `.slnx`, run focused Server/Integration tests and `dotnet tool run hexalith-module test --profile reads --filter Story=6.1`; retain actual `evidence/epic6/6.1-authorized-reads.{trx,json}` and `6.1-shadow-read-equivalence.json`. Run `git diff --check`.
