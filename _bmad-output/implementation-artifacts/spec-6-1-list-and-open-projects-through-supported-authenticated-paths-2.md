---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-09-22'
status: 'draft'
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

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` and `Authorization/ProjectAuthorizationGate.cs` -- preserve legacy list/open DTOs, routes, and authorization order as shadow authority; domain handlers cannot depend on `HttpContext`.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` and `ProjectDetail/ProjectDetailProjection.cs` -- reuse deterministic folds in separate persisted envelopes/handlers.
- `src/Hexalith.Projects/Queries/` and `Projections/ConversationStart/ConversationStartSetupProjectionHandler.cs` -- reuse dual-principal authorization, safe-denial, and persisted-handler patterns.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` -- current authenticated `/query`; canonical mapping collides on `/process` and `/query`.
- `references/Hexalith.EventStore/src/` -- required APIs/fakes exist, but safe denial is not opted in and cursor v1 has no wall-clock expiry.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/` and `evidence/epic6/` -- verify accepted P1R/P0/P2/P3/P4 records, sign-off, and superseding `READY`; stop on any mismatch.
- [ ] `src/Hexalith.Projects.Contracts/Queries/` -- add list/open queries, pages/results, and canonical snapshot/component/recovery contracts without changing legacy API contracts or general `PageRequest` defaults.
- [ ] `src/Hexalith.Projects/Projections/ProjectList/` and `ProjectDetail/` -- add incremental persisted handlers/envelopes with sequence and maximum positive global-position protection while retaining pure folds.
- [ ] `src/Hexalith.Projects/Queries/Handlers/` and server composition -- add authorization-first handlers, scoped 50/200 paging, safe denial, and an approved non-colliding authenticated route.
- [ ] `src/Hexalith.Projects.Testing/Reads/` and existing tests -- cover shadow comparison, leakage/zero-write, cursor, replay/restart/fault, persisted state, and the authenticated adapter.

**Acceptance Criteria:**
- Given accepted prerequisites and an authorized caller, when list/open executes through the supported gateway and handler, then only allowlisted metadata and canonical snapshot evidence are returned from persisted read models.
- Given identical persisted fixtures, when legacy and supported reads are compared, then values, filtering, ordering, keys, approved watermarks, and page boundaries match except for the accepted finite normalization table.
- Given success, replay, restart, denial, or fault, when reads execute, then no domain/sibling/task/resolution state is mutated and actual G-4 evidence is retained.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

P2 remains unaccepted. Safe denial needs explicit route opt-in, cursor v1 has no wall-clock expiry, and the canonical two-line host conflicts with existing mapped routes. These need prerequisite/architecture disposition; Story 6.1 must not invent local substitutes.

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.1` -- expected: production-authority scope passes without changing the blocked gate state.
- `dotnet restore Hexalith.Projects.slnx && dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: clean warnings-as-errors build on SDK 10.0.401.
- Run each affected test project individually -- expected: focused and persisted tests pass.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` -- expected: approved G-4 runner emits actual TRX, JSON, and passing shadow-equivalence evidence.
- `git diff --check` -- expected: no whitespace errors.
