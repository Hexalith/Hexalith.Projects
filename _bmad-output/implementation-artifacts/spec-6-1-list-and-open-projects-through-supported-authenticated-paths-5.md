---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-10-01'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/sprint-status.yaml'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Authenticated callers cannot list or open Projects through a supported DomainService read. Legacy `GET /api/v1/projects` is the only list/open path and does not return the shared snapshot.

**Approach:** After the Story 6.1 prerequisite chain is accepted, add list/open query contracts, watermarked projections, and `POST /query` handlers beside the unchanged legacy route. Queries return Safe Metadata plus the shared snapshot and write no Project domain state.

**Decision:** This story is the shared-snapshot slice. Every authorized caller receives Safe Metadata. Visibility stays on the existing list/read gate. `ProjectListItem.Name` stays in the fold and stays out of the query response. Folder-read filtering, inspection-gated names, and shadow comparison stay out of this story.

## Boundaries & Constraints

**Always:** Confirm the entry gate before any runtime edit. Stop with no runtime edits while `6-1-list-and-open-projects-through-supported-authenticated-paths` is `blocked` or readiness is not exactly `READY`. Derive Tenant, actor, and workload identity from the authenticated envelope. Reauthorize before protected validation. Filter with the existing list/read gate before paging. Reuse platform read-model, cursor, and safe-denial seams. Keep one public C# type per file. Reuse `AdmissionSnapshot` as defined, including `asOf` as the persisted read-model observation instant.

**Never:** Do not change submodule pins, switch public routing, duplicate `/api/v1/projects`, hand-roll persistence or cursors, rewrite events, expose payloads or denial detail, add UI/CLI/MCP work, select a resolution candidate, or mutate Project domain state. Do not claim a module-runner or evidence lane while G-6 or 6.1-P0 is open. Do not add shadow comparison, Chatbot Folder-read visibility, or inspection-gated Project names.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Gate open | Story key `blocked`, or any of P0, current P1R, P2, P3, architect sign-off, P4, or independent `READY` missing | No runtime source, test, package pin, or submodule change | Stop on the gate task |
| Authorized list or open | Current evidence; valid dual principal; lifecycle and page inputs the existing gate already allows | Safe Metadata plus the shared snapshot (`responseState`, `asOf`, disclosable `projectVersion`, metadata-only `components`, `recoveryActions`); default page 50 / max 200; caller-, filter-, and watermark-bound cursor; no Project name, setup body, title, path, or preview body; no resolution candidate | Invalid structural input returns metadata-only `400` after disclosure-safe authorization |
| Denied or absent | Denied, cross-Tenant, or nonexistent target | No protected snapshot and no unauthorized list row | Observably identical safe `404` |
| Replay or fault | Duplicate, restart, rebuild, store fault, or unknown relevant event | Incremental and rebuild paths converge, or the read reports unavailable; the query writes nothing | Unknown or corrupt relevant events fail closed |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — story key `6-1-list-and-open-projects-through-supported-authenticated-paths` is `blocked`. P1 is done on EventStore 3.70.1. Accepted P1R covers only the historical 3.106.0 tuple; `current_candidate_usable` is false. P0, P2, P3, and P4 are open. `references/Hexalith.Builds/Props/Directory.Packages.props` pins `HexalithEventStoreVersion` at `3.110.0`. Do not move that pin or the EventStore gitlink.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — keep `MapGet` `/api/v1/projects` (`ListProjectsAsync`, ~1771) and `/api/v1/projects/{projectId}` (`GetProjectAsync`, ~660).
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — reuse `AuthorizeListAsync` (~137), `AuthorizeReadAsync`, and `AuthorizeSupportedReadAsync` (~113). The gate decides allow/deny and Tenant. It does not filter by Folder read or shape Safe Metadata.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` — reuse `TryBind`. Copy parse, then bind, then `AuthorizeSupportedReadAsync` from `ProjectContextQueryExecutor` and `GetProjectContextQueryHandler`.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` and `ProjectDetail/ProjectDetailProjection.cs` — reuse the pure folds. `ProjectListItem.Name` stays in the fold.
- `src/Hexalith.Projects.Server/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs` — copy `IReadModelStore` plus `ReadModelWritePolicy.UpdateAsync`. Leave `DaprProjectProjectionStore` unchanged.
- `src/Hexalith.Projects.Contracts/Queries/AdmissionSnapshot.cs` and `ProjectContextAdmission.SafeDenial` — reuse. Leave `PageRequest` at 1–100, default 25. Add a separate 50/200 page type.
- `src/Hexalith.Projects.Server/ProjectsServerModule.cs` and `ProjectsServerServiceCollectionExtensions.cs` — add `Hexalith.Projects.Queries.ListProjects.v1` and `GetProject.v1`, and register them beside the existing `IDomainQueryHandler` singletons (~127). `QueryCursorScope` is not used in Projects `src/`; reuse the platform cursor rather than a local codec.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` — confirm current-baseline P1R, P0, P2, P3, architect sign-off, P4, and independent `READY` — stop with no runtime edits while any gate is open.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add list/open query, 50/200 page, and Safe Metadata result types, one public type per file — leave `PageRequest` unchanged and omit Project name.
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

Supported context reads are the pattern: `GetProjectContextQueryHandler` returns `QueryResult.Failure("safe-denial")` or a serialized admission payload. List and open follow that order and add paging. Projection persistence follows `ConversationStartSetupProjectionHandler` (`StoreName`, `IAsyncDomainProjectionHandler`, `ReadModelWritePolicy`). Legacy REST and the Dapr journal stay in place until Story 6.7.

Folder-read filtering, inspection-gated names, and shadow comparison stay out. The list/read gate has no Folder-read or inspection seam, the inspection permission is unaccepted G-5, and an FR-21 audit write belongs to Story 8.1. `ProjectListItem.Name` remains in the fold so a later story can disclose it without rewriting the projection.

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.1` — expected: authority scope passes and the blocked gate stays unchanged.
- After the gate task allows runtime edits: `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj` — expected: the Story 6.1 cases pass. Do not run `hexalith-module test` until G-6 and 6.1-P0 accept that lane.
