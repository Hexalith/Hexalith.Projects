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

**Problem:** Authenticated callers cannot list or open Projects through a supported DomainService read. Legacy `GET /api/v1/projects` is the only list/open path and does not return the shared snapshot.

**Approach:** After the Story 6.1 prerequisite chain is accepted, add additive list/open query contracts, watermarked projections, and `POST /query` handlers beside the unchanged legacy route. Queries return Safe Metadata plus the shared snapshot and write no Project domain state.

**Decision:** `asOf` is the server timestamp of the authorization and evidence computation.

## Boundaries & Constraints

**Always:** Confirm the entry gate before any runtime edit. Stop with no runtime edits while `6-1-list-and-open-projects-through-supported-authenticated-paths` is `blocked` or readiness is not exactly `READY`. Derive Tenant, actor, and workload identity from the authenticated envelope. Reauthorize before protected validation. Filter with the existing list/read gate before paging. Reuse platform read-model, cursor, and safe-denial seams. Keep one public C# type per file.

**Never:** Do not change submodule pins, switch public routing, duplicate `/api/v1/projects`, hand-roll persistence or cursors, rewrite events, expose payloads or denial detail, add UI/CLI/MCP work, select a resolution candidate, or mutate Project domain state. Do not claim a module-runner or evidence lane while G-6 or 6.1-P0 is open. Do not add shadow comparison, quarantined-folderless inspection, Chatbot Folder-read visibility, or inspection-gated Project names.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized list | Current evidence; valid dual principal; lifecycle and page inputs | Stable rows the existing list gate already allows; default 50 / max 200; caller-, filter-, and watermark-bound cursor; per-row snapshot; no Project name | Invalid structural input returns metadata-only `400` after disclosure-safe authorization |
| Authorized open | Visible well-formed Project ID | Lifecycle, component evidence, and recovery actions; no name, setup body, title, path, or preview body; no resolution result | Required evidence is Project, Folder, Setup, and current authorization. Stale or incomplete evidence is `Partial` or `Unavailable` |
| Denied or absent | Denied, cross-Tenant, or nonexistent target | No protected snapshot and no unauthorized list row | Observably identical safe `404` |
| Replay or fault | Duplicate, restart, rebuild, store fault, or unknown relevant event | Incremental and rebuild paths converge, or the read reports unavailable; the query writes nothing | Unknown or corrupt relevant events fail closed |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — story key is `blocked`. Accepted P1R is EventStore `3.106.0`; `references/Hexalith.Builds/Props/Directory.Packages.props` pins `HexalithEventStoreVersion` `3.110.0`. EventStore gitlink `6dededdecd62dd6dc6d1f15810108d860ec70c8f`. Do not move that submodule.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — keep `ListProjectsAsync` and `GetProjectAsync`. Failed queries already return HTTP 404.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — reuse `AuthorizeListAsync`, `AuthorizeReadAsync`, and `AuthorizeSupportedReadAsync`. Filter before paging.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` — reuse `TryBind`. Copy the `TryBind` then `AuthorizeSupportedReadAsync` order from `ProjectContextQueryExecutor`.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` and `ProjectDetail/ProjectDetailProjection.cs` — reuse the pure folds. Copy persistence from `ConversationStartSetupProjectionHandler` (`IReadModelStore`, `ReadModelWritePolicy`). Leave `DaprProjectProjectionStore` unchanged.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` and `ProjectsServerModule.cs` — register on existing `POST /query`. Add `*.Queries.*.v1` constants. Leave `PageRequest` at 1–100, default 25. Reuse `AdmissionSnapshot`, `AdmissionResponseState`, and `ProjectContextAdmission.SafeDenial`.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` — confirm current-baseline P1R, P0, P2, P3, architect sign-off, P4, and independent `READY` — stop with no runtime edits while any gate is open.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add list/open query, 50/200 page, and Safe Metadata result types, one public type per file — leave `PageRequest` unchanged.
- [ ] `src/Hexalith.Projects.Server/Projections/ProjectList/` and `ProjectDetail/` — add incremental persisted handlers over the pure folds.
- [ ] `src/Hexalith.Projects.Server/Queries/`, `ProjectsServerModule.cs`, and `ProjectsServerServiceCollectionExtensions.cs` — register authorization-first handlers on the existing `POST /query`.
- [ ] `tests/Hexalith.Projects.Server.Tests/Queries/` — test the I/O matrix and zero query writes.

**Acceptance Criteria:**
- Given any open item in the Story 6.1 prerequisite chain or a readiness result other than exactly `READY`, when this story starts, then no runtime source, test, package pin, or submodule changes.
- Given a completed list or open after the gate, when the call finishes, then no resolution candidate is selected and no Project domain state is written.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Role-specific disclosure is deferred. This slice returns Safe Metadata for every authorized caller and uses the existing list/read gate for which rows and targets are visible. `ProjectListItem.Name` stays in the fold and stays out of the query response.

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.1` — expected: authority scope passes and the blocked gate stays unchanged.
- After the gate task allows runtime edits: `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj` with the Story 6.1 filter — expected: pass. Do not run `hexalith-module test` until G-6 and 6.1-P0 accept that lane.
