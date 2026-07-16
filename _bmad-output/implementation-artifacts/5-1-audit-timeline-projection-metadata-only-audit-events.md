---
baseline_commit: b60eff7638a6002ce3467b998d858d07e32f62a1
---

# Story 5.1: Audit Timeline Projection & Metadata-Only Audit Events

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an authorized operator / support agent,
I want every Project lifecycle and context-reference change recorded as a metadata-only audit event in a queryable timeline,
so that I can reconstruct what happened to a Project without accessing any payload data.

## Acceptance Criteria

1. Given any Project operation, when it occurs, then `ProjectAuditTimelineProjection` records a metadata-only audit row including tenant, Project identity, operation type, timestamp, actor identity where available, affected reference identifiers, correlation/task/idempotency metadata, and a deterministic audit event id. Covered operations: creation, setup update, archival, Project Folder change, File Reference link/unlink, Memory link/unlink, resolution confirmation, and a new Project created from a proposal through the explicit Story 4.5 command chain.
2. Given Story 4.5 proposal confirmation, when a Project is created from a proposal, then audit derives from the resulting `ProjectCreated` plus assignment/folder/file command events and composite metadata; do not add or expect a `ProjectCreatedFromProposal` event, proposal aggregate, persisted proposal trace, candidate scores, ranks, rejected candidate ids, or raw proposal body.
3. Given the metadata-only rule, when audit rows are folded, serialized, logged, or stored, then they contain no transcript payloads, file contents, raw prompts, secrets, memory payloads, unrestricted/local paths, raw tokens, full command bodies, candidate scores/ranks, or sibling denial details; extend the existing `NoPayloadLeakage` harness over the new audit row DTO/model.
4. Given tenant scope, when the audit timeline is queried through the read-model seam, then results are tenant-scoped, project-scoped when requested, ordered newest-first or explicitly documented, and authorization-filterable by the caller layer. Cross-tenant rows must never be returned.
5. Given the shared durable projection journal, when the runtime store rebuilds read models, then the audit projection is rebuilt from the same persisted Project event journal and inherits replay-conflict, malformed-payload, duplicate-message, out-of-order, global-position watermark, and missing-journal fail-closed behavior already used by list/detail/reference-index projections.
6. Given projection consistency rules, when a new success `IProjectEvent` is added in the future, then `ProjectAuditTimelineProjection` fails loudly until it is explicitly mapped or documented as intentionally ignored; it must not silently drop unknown Project event types.
7. Given query-surface handoff to Stories 5.2 and 5.7, when this story completes, then the read-model contracts and projection rows are sufficient for operator read/API/UI/MCP/CLI stories to render timestamp, actor/source surface, operation, previous-to-new state where available, affected references, correlation id, task id, audit event id, and safe reason/state codes without refolding EventStore payloads in those later surfaces.

## Tasks / Subtasks

- [x] Add metadata-only audit projection contracts and row model (AC: 1, 3, 7)
  - [x] Create `src/Hexalith.Projects/Projections/ProjectAuditTimeline/ProjectAuditTimelineProjection.cs`.
  - [x] Create `src/Hexalith.Projects/Projections/ProjectAuditTimeline/ProjectAuditTimelineItem.cs`.
  - [x] Use existing `ProjectProjectionEnvelope` as the projection input; do not introduce a parallel envelope type unless it removes a real dependency problem.
  - [x] Include safe fields only: `TenantId`, `ProjectId`, `AuditEventId`, `OperationType`, `OccurredAt`, `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, affected reference kind/id, safe lifecycle/reference state deltas, and optional safe reason code.
  - [x] Derive `AuditEventId` deterministically from tenant/project/event type/sequence/idempotency fingerprint or an equivalent stable metadata-only tuple. Do not use `Guid.NewGuid()`, wall-clock outside `OccurredAt`, random values, or Dapr state as the id source.

- [x] Map all current Projects success events into audit rows (AC: 1, 2, 6)
  - [x] Map `ProjectCreated` as a project lifecycle/create operation.
  - [x] Map `ProjectSetupUpdated` as a setup metadata update without copying setup body text into the row.
  - [x] Map `ProjectArchived` as lifecycle transition `Active`/unknown-to-`Archived` using event-safe data only.
  - [x] Map `ProjectFolderSet` and `ProjectFolderCreationPending` as folder-reference operations.
  - [x] Map `FileReferenceLinked` / `FileReferenceUnlinked` as file-reference operations.
  - [x] Map `MemoryLinked` / `MemoryUnlinked` as memory-reference operations.
  - [x] Map `ProjectResolutionConfirmed` as resolution-confirmation metadata only; store the confirmed conversation id/source project id if safe, but never candidate lists, scores, ranks, or rejected ids.
  - [x] Do not map rejection events into this projection unless the implementation deliberately expands the story and adds tests/docs; the epics AC says audit derives from EventStore envelope metadata + Project events, and current `IProjectEvent` covers success events only.

- [x] Extend durable projection store seams (AC: 4, 5)
  - [x] Add an audit-list method to `src/Hexalith.Projects.Infrastructure/IProjectProjectionStore.cs`, for example `ListAuditTimelineAsync(string tenantId, string? projectId, int? limit, CancellationToken)`.
  - [x] Implement it in `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs` by reading the existing `projects:projection-journal:{tenantId}` document and rebuilding `ProjectAuditTimelineProjection`.
  - [x] Preserve current `EnsureReadable(...)` fail-closed behavior for missing journals, replay conflicts, and malformed evidence.
  - [x] Add `IProjectAuditTimelineReadModel` plus in-memory and Dapr-backed implementations under `src/Hexalith.Projects.Server/`, following `IProjectListReadModel`, `DaprProjectListReadModel`, and `InMemoryProjectListReadModel` patterns.
  - [x] Register the in-memory read model in `AddProjectsServer(...)` and replace it with the Dapr-backed read model in `AddProjectsServerRuntimeInfrastructure(...)`.
  - [x] Do not add a public HTTP/OpenAPI query unless required to prove AC 4; Story 5.2 owns operator read access and Story 5.7 owns the Audit Timeline view/export. If a minimal internal endpoint is added, apply query checklist rows 1/4/5/6/8 and regenerate OpenAPI/client artifacts.

- [x] Preserve proposal semantics from Story 4.5 (AC: 2)
  - [x] Treat "new Project created from proposal" as the observable command chain already emitted by Story 4.5: `CreateProject`, optional conversation assignment, optional `SetProjectFolder`, optional `LinkFileReference`.
  - [x] Do not introduce `ProjectCreatedFromProposal`, a proposal aggregate, a persisted proposal store, persisted proposal preview, or audit fields containing raw proposal inputs.
  - [x] If composite proposal metadata is needed, derive only safe correlation/task/idempotency metadata already present on the command events.

- [x] Extend metadata-only leakage proof (AC: 3)
  - [x] Add `ProjectAuditTimelineItem_SerializesMetadataOnly` to `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` or a sibling leakage fixture.
  - [x] Assert audit serialization does not contain forbidden terms from `PayloadClassification.ForbiddenContent`.
  - [x] Add explicit negative assertions for `candidate`, `score`, `rank`, `transcript`, `prompt`, `content`, `token`, `path`, and `secret` where relevant.
  - [x] Keep `docs/payload-taxonomy.md` and `src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs` in sync only if a genuinely new safe category is required. Prefer existing safe categories: `AuditId`, `Timestamp`, `OpaqueId`, `ReferenceKind`, `LifecycleState`, `InclusionState`, `ReasonCode`, `CorrelationId`, `CausationId`.

- [x] Add pure Tier-1 projection tests (AC: 1, 4, 5, 6)
  - [x] Create `tests/Hexalith.Projects.Tests/Projections/ProjectAuditTimelineProjectionTests.cs`.
  - [x] Cover one row per mapped event type and verify operation type, affected reference ids, actor/correlation/task/idempotency metadata, timestamp, and audit id stability.
  - [x] Cover deterministic ordering, tenant guard skip on envelope/event tenant mismatch, project filter, newest-first or documented ordering, duplicate deterministic rebuild behavior, and throw-on-unknown event.
  - [x] Cover Project 4.5 proposal chain by folding a representative `ProjectCreated` plus folder/file events and proving no proposal-specific synthetic event or raw proposal payload appears.

- [x] Extend durable store/integration tests (AC: 4, 5)
  - [x] Extend `tests/Hexalith.Projects.Integration.Tests/DaprProjectionStoreTests.cs` to prove `DaprProjectProjectionStore` can append current Project events and rebuild audit rows from the shared journal.
  - [x] Reuse the fake state store pattern already in that file; no live Dapr sidecar is required.
  - [x] Verify duplicate append returns `Duplicate` without duplicate audit rows, same-message/different-payload marks replay conflict, and missing/malformed journal fails closed before returning rows.

- [x] Update docs/catalogs (AC: 1, 3, 5, 7)
  - [x] Add a `ProjectAuditTimelineProjection` section to `docs/projection-catalog.md` with owner, key, source events, stored data, tenant scoping, rebuild behavior, runtime store, freshness semantics, leakage boundary, and consumer guidance for Stories 5.2 and 5.7.
  - [x] Update `docs/event-catalog.md` consumers so all success events list `ProjectAuditTimelineProjection` where applicable.
  - [x] Mention that `ProjectResolutionConfirmed` remains metadata-only and `ProjectCreatedFromProposal` remains intentionally absent.

- [x] Run focused verification (AC: all)
  - [x] `dotnet test Hexalith.Projects.slnx --filter "FullyQualifiedName~ProjectAuditTimeline|FullyQualifiedName~NoPayloadLeakage|FullyQualifiedName~DaprProjectionStore"`
  - [x] `dotnet build Hexalith.Projects.slnx -warnaserror`
  - [x] `git diff --check`
  - [x] If OpenAPI/client files are changed unexpectedly, stop and justify why this backend projection story needs public contract churn.

## Dev Notes

### Current State

- The existing projection path is a shared durable tenant journal. `ProjectEventProjectionProcessor` delegates every EventStore `EventEnvelope` to `IProjectProjectionStore.AppendAsync(...)`, and `DaprProjectProjectionStore` stores a per-tenant journal at `projects:projection-journal:{tenantId}` before rebuilding list/detail/reference-index projections from it. [Source: src/Hexalith.Projects.Infrastructure/ProjectEventProjectionProcessor.cs] [Source: src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs] [Source: docs/projection-catalog.md#ProjectListProjection]
- The shared projection input is `ProjectProjectionEnvelope(TenantId, Sequence, IProjectEvent Event)`. Existing projections use deterministic ordering by `(Sequence, IdempotencyKey, IdempotencyFingerprint)`, guard envelope/event tenant agreement, and throw on unknown event types. Reuse this pattern. [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectProjectionEnvelope.cs] [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs] [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs]
- Current projections are pure domain/read-model folds in `src/Hexalith.Projects/Projections/*`; Server read-model interfaces/adapters sit in `src/Hexalith.Projects.Server/`; Dapr runtime store implementation sits in `src/Hexalith.Projects.Infrastructure/`. Keep this boundary. [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- `ProjectReferenceIndexProjection` already models reference lanes (`folder`, `file`, `memory`) and must not be replaced or duplicated for reference health. The audit projection records sequence/history; the reference index remains the current reverse lookup/current-state model. [Source: src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs] [Source: docs/projection-catalog.md#ProjectReferenceIndexProjection]

### Event Coverage

Success events currently implementing `IProjectEvent` are the audit source set: `ProjectCreated`, `ProjectSetupUpdated`, `ProjectArchived`, `ProjectFolderCreationPending`, `ProjectFolderSet`, `FileReferenceLinked`, `FileReferenceUnlinked`, `MemoryLinked`, `MemoryUnlinked`, and `ProjectResolutionConfirmed`. Rejection events implement `IRejectionEvent` and are not part of the current success-event projection contract. [Source: src/Hexalith.Projects.Contracts/Events/IProjectEvent.cs] [Source: docs/event-catalog.md#Success-events]

`ProjectResolutionConfirmed` is deliberately narrow. It stores confirmed target project, conversation id, optional source project id, and metadata; it forbids candidates, rejected candidates, scores, ranks, raw resolution results, transcripts, file contents, prompts, memory bodies, paths, tokens, and full request bodies. The audit projection must preserve that boundary. [Source: docs/event-catalog.md#ProjectResolutionConfirmed]

Story 4.5 intentionally emits no `ProjectCreatedFromProposal` event. A proposal confirmation is visible as explicit `CreateProject` plus conversation assignment and optional folder/file commands. Do not create a synthetic event or persisted proposal trace for audit convenience. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.1-Audit-timeline-projection--metadata-only-audit-events] [Source: src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs]

### Metadata-Only Rules

- Audit rows may contain safe metadata categories only: opaque ids, tenant id, reference kind, timestamps, lifecycle/inclusion states, reason codes, correlation/causation/audit ids. [Source: docs/payload-taxonomy.md#Safe--reference-only-fields-ALLOWED-on-the-wire]
- Never store or serialize sibling-owned content: conversation transcripts, file contents, memory bodies, raw prompts, secrets, raw tokens, full command bodies, unrestricted/local file paths, or sensitive folder names. [Source: docs/payload-taxonomy.md#Forbidden-sibling-owned-content-NEVER-on-the-wire]
- The machine source of truth is `PayloadClassification`; prefer extending tests over adding new safe categories. [Source: src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs]
- The existing `NoPayloadLeakageAssertions` harness is the correct proof mechanism. Add coverage for the new audit item rather than creating a separate leakage framework. [Source: tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs] [Source: src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs]

### Query And Authorization Scope

This story should create a queryable projection/read-model seam, not a broad operator surface. Story 5.2 owns operator read access; Story 5.7 owns the web Audit Timeline and Safe Diagnostic Export. If this story adds a public HTTP endpoint anyway, it must follow the canonical query behavior: reject `Idempotency-Key` after authorization, reject non-`eventually_consistent` freshness after authorization, safe-deny cross-tenant/malformed identifiers with 404, and return 503 for stale/unavailable projection evidence. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.2-Operator-read-access] [Source: _bmad-output/planning-artifacts/epics.md#Story-5.7-Audit-timeline-view--Safe-Diagnostic-Export] [Source: docs/checklists/mutation-and-query-negative-tests.md#Canonical-Rows] [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]

### Previous Story Intelligence

No previous Epic 5 story exists. Carry forward these relevant completed-story constraints:

- Story 4.5 proposal flow: no proposal aggregate, no persisted proposal store, no `ProjectCreatedFromProposal`, no persisted trace/proposal body; confirmation uses explicit command chain with deterministic child idempotency keys. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]
- Story 4.4 confirmation flow: `ProjectResolutionConfirmed` is metadata-only and persists only the confirmed choice; rejected candidates and candidate scoring data are not persisted. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]
- Story 1.5 projection rule: rebuild must delegate to the same pure fold as incremental application; avoid duplicate fold logic. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]

### Latest Technical Context

- Keep the repo-pinned .NET SDK and package posture. The workspace uses .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Dapr `1.17.7`, and Fluent UI Blazor `5.0.0-rc.2-26098.1`; do not bump packages for this story. [Source: _bmad-output/project-context.md#Technology-Stack--Versions]
- External check on 2026-05-30: Microsoft documents .NET 10 as an LTS release, and Dapr docs list runtime `1.17.7` as the current supported release. That confirms the existing pinned posture; it does not authorize package churn. [Source: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview] [Source: https://docs.dapr.io/operations/support/support-release-policy/]

### Project Structure Notes

- New pure projection files belong under `src/Hexalith.Projects/Projections/ProjectAuditTimeline/`.
- New runtime read-model interfaces/adapters belong under `src/Hexalith.Projects.Server/`.
- New Dapr journal logic belongs in `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs` and `IProjectProjectionStore.cs`.
- Pure projection tests belong under `tests/Hexalith.Projects.Tests/Projections/`.
- Dapr fake-store tests belong in `tests/Hexalith.Projects.Integration.Tests/DaprProjectionStoreTests.cs`.
- Do not touch generated `.g.cs` files unless a deliberate public OpenAPI contract is added and regenerated.
- Do not initialize nested submodules or read BMAD folders inside submodules.

### Hard Stops

- Stop before coding if implementation appears to require storing transcript/file/memory/prompt/secret/path/token payloads in audit.
- Stop before coding if audit requires `ProjectCreatedFromProposal`, a proposal aggregate, persisted proposal preview, or persisted resolution candidate traces.
- Stop before coding if a new shared enum is required only to label audit operations; prefer a local metadata-only operation-name value or clearly justified contract addition.
- Stop before coding if public HTTP/OpenAPI/client churn appears necessary for AC 4; confirm it is not a Story 5.2 responsibility.
- Stop before coding if a submodule pointer change or package version bump appears in the diff.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-5.1-Audit-timeline-projection--metadata-only-audit-events]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Design-Principle-One-Operational-Model-Three-Surfaces]
- [Source: docs/event-catalog.md]
- [Source: docs/projection-catalog.md]
- [Source: docs/payload-taxonomy.md]
- [Source: docs/checklists/mutation-and-query-negative-tests.md]
- [Source: src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs]
- [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs]
- [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs]
- [Source: src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs]
- [Source: tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs]
- [Source: tests/Hexalith.Projects.Integration.Tests/DaprProjectionStoreTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Create-story validation applied against checklist: story includes source-derived ACs, concrete file locations, architecture guardrails, metadata-only leakage tests, prior-story constraints, and hard stops for likely implementation mistakes.
- dev-story finalization run by Claude Opus 4.8 (1M): all 9 task groups verified against the codebase, build run with `-warnaserror` (0 warnings/0 errors), and the full Projects regression suite run green (1243 tests: Tests 567, Server 473, Contracts 137, Client 51, Integration 15).
- AC1/AC2/AC3/AC6/AC7 satisfied by `ProjectAuditTimelineProjection` mapping all ten current `IProjectEvent` success events to metadata-only rows, throwing on unknown event types, deriving a deterministic SHA-256 `AuditEventId` (no `Guid.NewGuid()`/wall-clock/random/Dapr-state source), and emitting no `ProjectCreatedFromProposal` synthetic event — the 4.5 proposal chain is audited only through `ProjectCreated` + folder/file events.
- AC4/AC5 satisfied by the `IProjectAuditTimelineReadModel` seam (in-memory + Dapr-backed) and `IProjectProjectionStore.ListAuditTimelineAsync`, which rebuilds the audit projection from the shared `projects:projection-journal:{tenantId}` document and reuses the existing `EnsureReadable(...)` fail-closed path (missing journal, replay conflict, malformed/duplicate/out-of-order evidence, global-position watermark).
- AC3 leakage proof extended: `ProjectAuditTimelineItem_SerializesMetadataOnly` runs the `NoPayloadLeakage` harness over the new row DTO with explicit negative assertions for candidate/score/rank/transcript/prompt/content/token/path/secret.
- Defect fixed during finalization: `ProjectAuditTimelineProjection.List(...)` ordered newest-first by `OccurredAt` first, which reordered same-instant events against journal order and failed `List_IsTenantScopedProjectFilterableNewestFirstAndLimitBounded`. Switched to sequence-primary (EventStore global-position) newest-first ordering to match the codebase's deterministic `(Sequence, …)` convention and AC5 (inherit list/detail/reference-index ordering); updated the `docs/projection-catalog.md` freshness line to match. All focused and regression tests pass after the fix.
- Verification: `git diff --check` clean; no `.g.cs`/OpenAPI/client churn; no package-version or submodule-pointer changes.

### File List

**New**

- `src/Hexalith.Projects/Projections/ProjectAuditTimeline/ProjectAuditTimelineItem.cs`
- `src/Hexalith.Projects/Projections/ProjectAuditTimeline/ProjectAuditTimelineProjection.cs`
- `src/Hexalith.Projects.Server/IProjectAuditTimelineReadModel.cs`
- `src/Hexalith.Projects.Server/InMemoryProjectAuditTimelineReadModel.cs`
- `src/Hexalith.Projects.Server/DaprProjectAuditTimelineReadModel.cs`
- `tests/Hexalith.Projects.Tests/Projections/ProjectAuditTimelineProjectionTests.cs`
- `tests/Hexalith.Projects.Server.Tests/InMemoryProjectAuditTimelineReadModelTests.cs`

**Modified**

- `src/Hexalith.Projects.Infrastructure/IProjectProjectionStore.cs`
- `src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `tests/Hexalith.Projects.Integration.Tests/DaprProjectionStoreTests.cs`
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ServiceDefaultsEndpointTests.cs`
- `docs/projection-catalog.md`
- `docs/event-catalog.md`

## Senior Developer Review (AI)

**Reviewer:** Jerome — adversarial story-automator review on 2026-05-30 (Claude Opus 4.8 1M).
**Outcome:** Approved. Status review → done (0 Critical, 0 High issues).

### Scope verified

- All 7 Acceptance Criteria validated against actual implementation, not just story claims:
  - **AC1/AC6** — `ProjectAuditTimelineProjection.Apply` maps all ten current `IProjectEvent` success
    events (verified each event record signature: `ProjectArchived.Lifecycle`, `ProjectFolderCreationPending.ReasonCode`,
    `ProjectResolutionConfirmed.ConversationId`/`SourceProjectId`, `ReferenceState.Pending/Included/Excluded`)
    and throws `InvalidOperationException` on unknown types.
  - **AC1 (deterministic id)** — `AuditEventId` is SHA-256 over tenant/project/event-type/sequence/idempotency-key/
    fingerprint/operation/reference tuple; no `Guid.NewGuid()`, wall-clock, random, or Dapr-state source.
  - **AC2** — proposal chain audited only via `ProjectCreated` + folder/file events; no `ProjectCreatedFromProposal`,
    proposal aggregate, or persisted trace. Confirmed by `ProposalConfirmationChain_*` test.
  - **AC3** — `NoPayloadLeakage` harness extended over the row DTO with explicit negative assertions
    (candidate/score/rank/transcript/prompt/content/token/path/secret).
  - **AC4** — tenant guard (`envelope.TenantId == event.TenantId`) + `List(...)` tenant/project filter; cross-tenant
    rows never returned (verified by `TenantGuard_*` and `ListAsync_DropsDispatchTenantMismatch*`).
  - **AC5** — `ListAuditTimelineAsync` rebuilds from the shared `projects:projection-journal:{tenantId}` document via
    the same `EnsureReadable(...)` fail-closed path; integration tests cover replay-conflict, malformed, and
    missing-journal throw paths.
  - **AC7** — row exposes timestamp/actor/operation/state-delta/reference/correlation/task/audit-id for Stories 5.2/5.7.
- Build `dotnet build Hexalith.Projects.slnx -warnaserror` → 0 warnings / 0 errors.
- Focused suite (`ProjectAuditTimeline|NoPayloadLeakage|DaprProjectionStore`) and `ServiceDefaultsEndpoint` → all green
  (Integration 8, Server 7, Tests 56). `git diff --check` clean; no `.g.cs`/OpenAPI/client/package/submodule churn.

### Findings and fixes applied

- **[Medium][Fixed] Incomplete File List.** `tests/Hexalith.Projects.Server.Tests/InMemoryProjectAuditTimelineReadModelTests.cs`
  (new) and `tests/Hexalith.Projects.Server.Tests/ServiceDefaultsEndpointTests.cs` (modified) were changed in git but
  absent from the Dev Agent Record → File List. Both are legitimate additions (read-model unit tests + a Dapr-swap
  registration assertion); File List updated to document them.
- **[Low][Fixed] Redundant actor re-switch.** `Item(...)` re-derived `ActorPrincipalId` through a second 10-arm switch
  (with an unreachable `_ => string.Empty`) after the main `Apply` switch had already matched the typed event.
  Simplified to pass `ActorPrincipalId` directly from each main-switch arm and removed the helper; behavior-preserving
  (rebuild + full focused suite re-run green).
- **[Info] In-memory read model** uses the same monotonic `++_sequence` convention as `InMemoryProjectListReadModel` /
  `InMemoryProjectReferenceIndexReadModel`; durable redelivery dedup correctly lives at the Dapr journal layer. Not a defect.

## Change Log

| Date | Change |
| --- | --- |
| 2026-05-30 | Implemented metadata-only `ProjectAuditTimelineProjection` + `ProjectAuditTimelineItem`, mapping all ten current Project success events to deterministic audit rows. |
| 2026-05-30 | Added `IProjectAuditTimelineReadModel` (in-memory + Dapr) and `IProjectProjectionStore.ListAuditTimelineAsync`, rebuilding audit rows from the shared durable projection journal with existing fail-closed semantics; registered in `AddProjectsServer`/`AddProjectsServerRuntimeInfrastructure`. |
| 2026-05-30 | Extended `NoPayloadLeakage` proof over the audit row DTO and added pure Tier-1 projection tests + durable fake-store integration tests; updated `projection-catalog.md` and `event-catalog.md`. |
| 2026-05-30 | dev-story finalization (Claude Opus 4.8): fixed `List(...)` to sequence-primary newest-first ordering, aligned the projection-catalog freshness note, and verified build + 1243-test regression suite green. Status → review. |
| 2026-05-30 | Adversarial story-automator review (Claude Opus 4.8): documented two undocumented test files in File List; simplified redundant `ActorPrincipalId` re-switch in `ProjectAuditTimelineProjection`; re-verified build + focused suite green. Status → done. |
