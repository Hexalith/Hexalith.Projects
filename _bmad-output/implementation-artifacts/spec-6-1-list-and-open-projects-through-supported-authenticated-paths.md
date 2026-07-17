---
title: 'Story 6.1: List and open Projects through supported authenticated paths'
type: 'feature'
created: '2026-07-17T11:38:50+02:00'
status: 'blocked'
blocked_by: ['6.1-P0', '6.1-P1', '6.1-P2', '6.1-P3', '6.1-P4']
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** Tenant Operators and delegated Chatbot callers need authorization-filtered Project list and open reads through the supported EventStore DomainService path. The current Projects server exposes a legacy Dapr-backed REST path and cannot yet prove the required dual-principal identity, non-leaking safe denial, persisted-read watermark parity, or runner-backed evidence on the supported path.

**Approach:** After the owner-approved 6.1-P4 gate record accepts the exact P0-P3 revisions, evidence, normalization, and rollback pins, add additive query contracts, EventStore-backed incremental read models, and `IDomainQueryHandler` list/open handlers while retaining the legacy path as the comparison authority. Prove deterministic, metadata-only, zero-write behavior through the authenticated gateway and persisted boundary before any later cutover.

## Boundaries & Constraints

**Always:** Derive Tenant, original actor, workload, delegation, scopes, and audience from immutable authenticated context; reauthorize before protected validation; filter unauthorized rows before paging; use `IReadModelStore`, `ReadModelWritePolicy`, `IQueryCursorCodec`, and `QueryCursorScope`; preserve existing event history and legacy routes; keep one public C# type per file; assert persisted end-state and metadata-only responses.

**Block If:** Stop before runtime edits unless 6.1-P0 through 6.1-P3 have owner-approved repository-local revisions and evidence, 6.1-P4 accepts their exact source/package/API pins and finite normalization/rollback record, and this spec subsequently passes ready-for-development. Story 6.1 cannot create, waive, or self-approve the G-4 runner, version baseline, dual-principal query envelope, safe-denial behavior, global-position watermark, or production identity contract. Stop if implementation would require a sibling-submodule edit or public-route switch not separately authorized.

**Never:** Do not hand-roll Dapr persistence, cursors, query routing, authentication context, or projection actors; do not extend the legacy journal as the supported store; do not expose payload-bearing sibling content, resolution candidates, or denial detail; do not write domain state, append events, create tasks, rewrite history, remove legacy routing, or fabricate evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized list | Current read models; valid Tenant, actor, workload, delegation and scope; optional lifecycle/page cursor | Visible Active and Archived rows in stable order; default 50, cap 200; opaque caller/filter-bound cursor; AD-32 state and recovery evidence | Structurally invalid paging/filter/cursor returns metadata-only `400` after authorization where validation could leak |
| Authorized open | Well-formed visible Project identity | Metadata, lifecycle, typed setup summary, safe reference summary, component freshness and recovery evidence; no resolution result | Stale/incomplete evidence returns honest `Partial` or `Unavailable`; Archived remains inspectable but unusable for context |
| Denied or absent | Denied, cross-Tenant, or nonexistent well-formed Project | No protected snapshot and no unauthorized list row | Same safe `404` status, headers, body shape, timing category, logs, and telemetry |
| Rebuild/fault | Duplicate delivery, restart, rebuild, unavailable store, or unknown relevant event | Replay and incremental paths converge, or state is honestly unavailable; no writes from queries | Unknown/corrupt relevant events fail projection rather than being silently skipped |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` -- legacy list/open behavior and safe-denial comparison authority.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` -- current authorization order to preserve, but not reusable directly from domain handlers.
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` -- deterministic list fold to reuse.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs` -- deterministic detail fold to reuse.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs` -- supported query-handler seam.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs` -- required persisted read-model seam.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs` -- required cursor scope seam.

## Tasks & Acceptance

**External prerequisites — not Story 6.1 execution tasks:**
- `6.1-P0` -- Builds/platform supplies the supported G-4 persisted runner and evidence tool.
- `6.1-P1` -- EventStore, Builds, and Architecture owners approve one source/package/architecture/runner baseline and finite normalization record.
- `6.1-P2` -- EventStore/platform supplies the complete dual-principal query context, indistinguishable safe denial, and authoritative global-position watermark.
- `6.1-P3` -- Identity/security supplies the mandatory fail-closed production identity/authentication contract and supported fixtures.
- `6.1-P4` -- Projects planning owners accept the exact P0-P3 revisions, evidence, normalization, rollback pins, and readiness rerun.

**Execution — only after 6.1-P4 acceptance and a passing readiness rerun:**
- `src/Hexalith.Projects.Contracts/Queries/` -- add one-file-per-type `ListProjectsQuery`, `GetProjectQuery`, page/row/open results, and shared AD-32 snapshot/component/recovery vocabulary with additive serialization coverage.
- `src/Hexalith.Projects/Projections/ProjectList/` and `src/Hexalith.Projects/Projections/ProjectDetail/` -- adapt the existing pure folds to named incremental/rebuild handlers persisted only through EventStore read-model seams.
- `src/Hexalith.Projects/Queries/Handlers/ListProjectsQueryHandler.cs` and `GetProjectQueryHandler.cs` -- implement authorization-first, Tenant-filtered reads, honest state mapping, stable pagination, scoped cursors, and indistinguishable denial.
- `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs`, `src/Hexalith.Projects/Hexalith.Projects.csproj`, and `src/Hexalith.Projects.Server/Program.cs` -- register gate-approved supported dependencies and the two-line DomainService host beside intact legacy mappings; make production identity configuration fail closed.
- `src/Hexalith.Projects.Testing/Reads/` and existing Projects test projects -- add persisted fixtures, a finite-normalization shadow comparator, serialization/leakage tests, authorization matrices, replay/restart/fault tests, cursor boundary tests, and zero-write assertions.

**Acceptance Criteria:**
- Given the approved gate and an authenticated authorized caller, when list/open runs through the production gateway and `IDomainQueryHandler`, then results meet the matrix, contain only the metadata allowlist, and use only supported read-model/cursor seams.
- Given denied, cross-Tenant, and nonexistent targets, when production adapters execute the reads, then observable responses and metadata-only telemetry are indistinguishable safe `404`s and lists contain no unauthorized row.
- Given legacy and supported paths over the deterministic persisted fixture, when shadow comparison runs, then authorized output, keys, approved watermarks, ordering, cursor boundaries, and Tenant isolation are equal except for the exact hashed normalization table.
- Given success, denial, rebuild, restart, retry, or fault, when the reads execute, then they append no event and mutate no Project, sibling, task, audit-maintenance, or resolution state; platform-owned metadata-only denial audit remains permitted.
- Given required evidence is stale, rebuilding, missing, or unavailable, when an already-authorized read executes, then it reports honest component-level `Partial` or `Unavailable` state and never fabricates completeness.

## Spec Change Log

- 2026-07-17: Approved Sprint Change Proposal externalized platform enablement as 6.1-P0 through 6.1-P4 and retained `blocked` until P4 acceptance plus a passing readiness rerun.

## Review Triage Log

## Design Notes

The legacy route remains authoritative through Story 6.7. Story 6.1 adds a shadowable supported read path only; a passing comparator is evidence for later cutover, not permission to switch routes.

The approved 2026-07-17 course correction is planning and routing authority only. It does not approve
any P0-P3 revision, version, capability, evidence artifact, owner acceptance, or target date.

Current checkout evidence triggers the implementation block: EventStore source is `v3.69.0-12-g20be7872` while architecture pins `3.67.3` and Builds pins packages to `1.72.3`; `QueryEnvelope` omits original-actor/workload/delegation/scopes/audience fields; gateway authorization can emit distinguishable `403`; `ProjectionEventDto` omits the legacy global-position watermark; production JWT registration is conditional; and the readiness matrix records the G-4 tool as `not-available`. No owner-approved entry-gate or normalization artifact resolves these conflicts.

## Verification

**Commands:**
- `dotnet restore Hexalith.Projects.slnx && dotnet build Hexalith.Projects.slnx --configuration Debug` -- expected: clean restore/build with warnings as errors.
- Run each affected test project individually -- expected: all focused contract, projection, handler, server, and persisted integration tests pass.
- `dotnet tool run hexalith-module test --profile reads --filter Story=6.1` -- expected: approved G-4 runner produces actual `evidence/epic6/6.1-authorized-reads.trx`, JSON summary, and passing shadow-equivalence report.

## Auto Run Result

Status: blocked
Blocking condition: spec failed ready-for-development standard

Failing criterion: **Sufficient**. Known dependency and implementation gaps remain unresolved: incompatible/unapproved EventStore source, architecture, and central package versions; incomplete dual-principal query identity; distinguishable gateway `403` behavior that violates safe denial; absent supported global-position watermark; conditional production authentication; missing owner-approved entry-gate and normalization records; and unavailable G-4 persisted runner/evidence tooling. These gaps cannot be repaired within Story 6.1 without separately authorized platform, Builds, or sibling-repository work.
