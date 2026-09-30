---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-09-30'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenant Operators and delegated Chatbot callers still lack supported, authorization-filtered Project list/open reads with canonical snapshot state, scoped paging, persisted watermark evidence, and indistinguishable safe denial.

**Approach:** After the complete Story 6.1 prerequisite chain is accepted, add additive EventStore-backed list/detail projections and query handlers beside the unchanged legacy route, then prove metadata-only, zero-write shadow equivalence through the authenticated persisted boundary.

**Decision:** On 2026-09-22, the user retargeted this workflow run to prerequisite 6.1-P1R. This Story 6.1 draft remains blocked and receives no runtime edits in the retargeted run.

**Decision:** On 2026-09-30, an authorized Tenant-role inspection returns the Project name and writes no FR-21 event. Story 8.1 records that inspection.

## Boundaries & Constraints

**Always:** Keep Story 6.1 blocked until P1R, P0, P2, P3, Solution Architect sign-off, P4, spec readiness, and an independent `READY` are recorded. Derive Tenant and both principals from authenticated context; reauthorize before protected validation; filter before paging; use platform read-model, cursor, and safe-denial seams. Preserve additive serialization and one public C# type per file.

**Never:** Do not self-approve gates, change submodule pins, switch public routing, duplicate `/api/v1/projects`, hand-roll persistence/cursors, rewrite events, expose payloads or denial detail, add UI/CLI/MCP work, mutate state, or fabricate evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized list | Current evidence; valid dual principal; lifecycle/page inputs | Stable filtered rows; default 50/max 200; caller/filter/watermark-bound cursor; per-row snapshot | Invalid structural input returns metadata-only `400` after disclosure-safe authorization |
| Authorized open | Visible well-formed Project ID | Metadata, lifecycle, setup/reference summaries, component evidence; no resolution result | Stale/incomplete evidence is honest `Partial` or `Unavailable` |
| Denied or absent | Denied, cross-Tenant, or nonexistent target | No protected snapshot or unauthorized list row | Observably identical safe `404` |
| Replay/fault | Duplicate, restart, rebuild, store fault, unknown relevant event | Incremental and rebuild paths converge or report unavailable; no query writes | Unknown/corrupt relevant events fail closed |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — keep `GET /api/v1/projects` as the shadow oracle.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — reuse list/read authorization. Filter before paging.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`, `ProjectDetail/ProjectDetailProjection.cs`, and `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs` — reuse pure folds; the journal stays the comparison input.
- `src/Hexalith.Projects.Server/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs` — copy this persisted-read pattern.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — register on existing `POST /query` (~216). No second public list route.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` — reuse dual-principal binding and safe denial.
- `src/Hexalith.Projects.Contracts/Queries/PageRequest.cs` — leave 1–100, default 25. Add a 50/200 list page type. List/open query types are absent.
- `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs` — reuse in Testing. Consume accepted `references/Hexalith.EventStore/` and `references/Hexalith.Builds/` pins only.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` — confirm current-baseline P1R, P0, P2, P3, architect sign-off, P4, and independent `READY` — stop with no runtime edits while any gate is open.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` — add list/open query, 50/200 page, and AD-32 result types, one public type per file — leave `PageRequest` and legacy REST shapes unchanged.
- [ ] `src/Hexalith.Projects.Server/Projections/ProjectList/` and `ProjectDetail/` — add incremental persisted handlers over the pure folds — supported reads need a rebuildable watermarked store.
- [ ] `src/Hexalith.Projects.Server/Queries/`, `ProjectsServerModule.cs`, and `ProjectsServerServiceCollectionExtensions.cs` — add and register authorization-first list/open handlers on `POST /query` — legacy GET stays the comparison route.
- [ ] `src/Hexalith.Projects.Testing/Reads/` — test the I/O matrix, Chatbot-versus-Tenant-role visibility, shadow equivalence, and zero writes.

**Acceptance Criteria:**
- Given a Chatbot Project User, when list/open runs, then the result is exactly the Projects whose Folder that actor can currently read, with no Tenant-role-only or quarantined folderless rows.
- Given a current authorized enumeration, when the list returns, then response state is `Complete`, each row has its own state, a non-current Folder row is `Unavailable`, and pre-activation work is absent.
- Given a Tenant-role caller, when list/open runs, then Safe Metadata is the default and a Project name appears only with inspection authorization.
- Given the same persisted fixture, when legacy and supported reads are compared, then values, order, keys, watermarks, cursors, and Tenant isolation match except for the accepted normalization table.
- Given any list/open outcome, when the call completes, then no resolution candidate is selected and no domain or audit state is written.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

On 2026-09-30 Story 6.1 is `blocked`. Accepted P1R is EventStore 3.106.0; the pin is 3.110.0. P0, P2, P3, P4, architect sign-off, and independent `READY` are open. `asOf` is computation time. Tenant-role open of a quarantined folderless Project returns Safe Metadata. The FR-21 inspection event belongs to Story 8.1.

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.1` — expected: authority scope passes and the blocked gate stays unchanged.
- `dotnet build Hexalith.Projects.slnx --configuration Debug` — expected: warnings-as-errors build on the `global.json` SDK.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` — expected: `evidence/epic6/6.1-authorized-reads.{trx,json}` and a passing shadow report.
