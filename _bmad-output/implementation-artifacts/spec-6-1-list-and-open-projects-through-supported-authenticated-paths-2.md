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

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` and `Authorization/ProjectAuthorizationGate.cs` -- preserve legacy routes, safe denial, and filtering; handlers cannot use `HttpContext`.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`, `ProjectDetail/ProjectDetailProjection.cs`, and `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs` -- reuse pure folds; retain journal/replay as shadow input.
- `src/Hexalith.Projects.Server/Projections/ConversationStartSetup/ConversationStartSetupProjectionHandler.cs` and `Queries/ProjectContextQueryExecutor.cs` -- reuse incremental persisted reads and authorization.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` and `ProjectsServerServiceCollectionExtensions.cs` -- reuse dual-principal binding; resolve custom `/query` against the approved SDK host.
- `src/Hexalith.Projects.Contracts/Queries/` and `src/Hexalith.Projects.Testing/Reads/` -- reuse AD-32 and `PageRequest`; keep comparator code in Testing.
- `references/Hexalith.EventStore/` and `references/Hexalith.Builds/` -- consume exact accepted P2/P0 capabilities and pins.

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

2026-09-26: The user approved this spec and asked to continue. Implementation stopped at the entry gate before recording a baseline commit or changing status to `in-progress`: `sprint-status.yaml` still marks Story 6.1 `blocked`, P0/P2/P3/P4 remain open, and the latest independent readiness result is `NOT_READY`. The P4 entry-gate record is absent. Resume implementation only after the complete prerequisite chain is accepted on the exact current baseline.

## Spec Change Log

## Review Triage Log

## Design Notes

P1R accepted EventStore 3.106.0; current Builds work uses 3.108.1 and calls for revalidation. P0, P2, P3, architect sign-off, P4, and independent `READY` remain open. The P4 entry gate is absent; the tracker is `blocked` and latest readiness is `NOT_READY`.

The frozen P1R retarget applied to a prior run. Safe denial, cursor expiry, and host routing await accepted P2/P3/P4 contracts. Legacy reads permit stale Tenant evidence and use detail update time for `asOf`; Story 6.1 requires current authorization evidence and computation-time `asOf`.

## Verification

**Commands:**
- `python3 tools/planning/validate_production_authority.py --story-id 6.1` -- expected: production-authority scope passes without changing the blocked gate state.
- `dotnet restore Hexalith.Projects.slnx && dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: clean warnings-as-errors build on the SDK pinned by `global.json` (currently 10.0.401).
- Run each affected test project individually -- expected: focused and persisted tests pass.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` -- expected: approved G-4 runner emits actual TRX, JSON, and passing shadow-equivalence evidence.
- `git diff --check` -- expected: no whitespace errors.
