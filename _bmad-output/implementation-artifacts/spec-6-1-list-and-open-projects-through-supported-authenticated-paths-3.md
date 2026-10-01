---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-09-30'
status: 'done'
disposition: 'superseded'
superseded_by: 'spec-6-1-list-and-open-projects-through-supported-authenticated-paths-5.md'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenant-role operators and Chatbot Project Users cannot list or open Projects through a supported, authorization-filtered DomainService read. Legacy `GET /api/v1/projects` is the only list/open path and does not return the shared snapshot.

**Approach:** After the Story 6.1 prerequisite chain is accepted, add additive list/open query contracts, watermarked projections, and `POST /query` handlers beside the unchanged legacy route. Queries return the shared snapshot and write no Project domain state.

**Decision:** An authorized Tenant-role inspection returns the Project name and writes no FR-21 event. Story 8.1 records that inspection.

## Boundaries & Constraints

**Always:** Confirm the entry gate before any runtime edit. Derive Tenant, actor, and workload identity from the authenticated envelope. Reauthorize before protected validation. Filter before paging. Reuse platform read-model, cursor, and safe-denial seams. Keep one public C# type per file. `asOf` is the server timestamp of the authorization and evidence computation.

**Never:** Do not change submodule pins, switch public routing, duplicate `/api/v1/projects`, hand-roll persistence or cursors, rewrite events, expose payloads or denial detail, add UI/CLI/MCP work, select a resolution candidate, or mutate Project domain state. Do not claim a module-runner or evidence lane while G-6 or 6.1-P0 is open. Shadow equivalence and quarantined-folderless operator inspection are deferred.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized list | Current evidence; valid dual principal; lifecycle and page inputs | Stable filtered rows; default 50 / max 200; caller-, filter-, and watermark-bound cursor; per-row snapshot | Invalid structural input returns metadata-only `400` after disclosure-safe authorization |
| Authorized open | Visible well-formed Project ID | Metadata, lifecycle, setup and reference summaries, component evidence; no resolution result | Required evidence is Project, Folder, Setup, and current authorization. Stale or incomplete evidence is `Partial` or `Unavailable` |
| Chatbot visibility | Project User whose Folder read is current | Exactly those Projects; no Tenant-role-only or quarantined folderless row | A non-current Folder row is `Unavailable`; pre-activation work is absent |
| Tenant-role metadata | Inspection authorization absent or present | Safe Metadata by default; Project name only when inspection is authorized; no FR-21 event | Name, setup bodies, titles, paths, and preview bodies stay out of Safe Metadata |
| Denied or absent | Denied, cross-Tenant, or nonexistent target | No protected snapshot and no unauthorized list row | Observably identical safe `404` |
| Replay or fault | Duplicate, restart, rebuild, store fault, or unknown relevant event | Incremental and rebuild paths converge, or the read reports unavailable; the query writes nothing | Unknown or corrupt relevant events fail closed |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `6-1-list-and-open-projects-through-supported-authenticated-paths` is `blocked`. Accepted P1R is EventStore `3.106.0`; `references/Hexalith.Builds/Props/Directory.Packages.props` pins `3.110.0`. P0, P2, P3, P4, architect sign-off, and `current_result: NOT_READY` are open. Do not move `references/Hexalith.EventStore/`.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — keep `ListProjectsAsync` and `GetProjectAsync`. Reuse `ProjectAuthorizationGate.AuthorizeListAsync`, `AuthorizeReadAsync`, and `AuthorizeSupportedReadAsync` before paging, and `ProjectQueryEnvelopePrincipalBinding.TryBind`. Workload identity must not widen authority. Failed queries already return HTTP 404.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`, `ProjectDetail/ProjectDetailProjection.cs`, `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs` — reuse the pure folds. Copy `ConversationStartSetupProjectionHandler` (`IReadModelStore`, `ReadModelWritePolicy`). Server list/detail handlers are absent. Do not rewrite events.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`, `ProjectsServerModule.cs` — register in `AddProjectsServer` and `AddProjectsServerRuntimeInfrastructure`. `POST /query` exists. Add `*.Queries.*.v1` constants.
- `src/Hexalith.Projects.Contracts/Queries/PageRequest.cs` — leave 1–100, default 25. Add a 50/200 page type. List/open types are absent. Reuse `AdmissionSnapshot`, `AdmissionResponseState`, and `ProjectContextAdmission.SafeDenial`. List/open `asOf` is the PRD computation timestamp.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` — confirm current-baseline P1R, P0, P2, P3, architect sign-off, P4, and independent `READY` — stop with no runtime edits while any gate is open.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add list/open query, 50/200 page, and AD-32 result types, one public type per file — leave `PageRequest` unchanged.
- [ ] `src/Hexalith.Projects.Server/Projections/ProjectList/` and `ProjectDetail/` — add incremental persisted handlers over the pure folds.
- [ ] `src/Hexalith.Projects.Server/Queries/`, `ProjectsServerModule.cs`, and `ProjectsServerServiceCollectionExtensions.cs` — register authorization-first handlers on the existing `POST /query`.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/` — test the I/O matrix and zero query writes.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.1` — expected: authority scope passes and the blocked gate stays unchanged.
- After the gate task allows runtime edits: `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --filter Story=6.1` — expected: pass. Do not run `hexalith-module test` until G-6 and 6.1-P0 accept that lane.
