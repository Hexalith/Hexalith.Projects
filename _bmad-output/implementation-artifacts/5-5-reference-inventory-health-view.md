---
story_id: 5.5
story_key: 5-5-reference-inventory-health-view
epic: "Epic 5: Operational Console & Audit (CLI / MCP / Web)"
created: 2026-05-30
source_story_status: backlog
baseline_commit: 6f18018
---

# Story 5.5: Reference Inventory & Health View

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an administrator,
I want a reference health matrix showing each linked conversation, folder, file, and memory with its inclusion/health state and reason code,
so that I can diagnose why a reference is stale, unauthorized, unavailable, or invalid.

## Acceptance Criteria

1. Given a selected Project in `/projects/{ProjectId}`, when the Reference Health Matrix renders, then it shows one metadata-only row per linked conversation, Project Folder, File Reference, and Memory reference with explicit grid headers for reference type, reference ID, bounded-context owner, inclusion state, health state, reason code, last-checked timestamp, freshness/trust evidence, and available safe actions.
2. Given shared Projects vocabulary, when inclusion/health/reason values render, then they use `ReferenceState`, `ProjectReasonCode`, `ProjectContextInclusionCheck`, `ProjectContextInclusionDiagnostic`, and `ProjectVocabularyDescriptors` rather than Web-only enums, free-text reason tables, or duplicated severity mappings.
3. Given metadata-only and tenant-isolation rules, when reference health is loaded or rendered, then no sibling payload, folder path, file content, memory payload, transcript, raw prompt, token, command body, proposal body, candidate score/rank, rejected id, raw ProblemDetails body, or sibling denial detail is fetched, logged, serialized, or shown.
4. Given conversation references are owned by `Hexalith.Conversations` and are not stored in `ProjectDetailProjection`, when conversation rows are needed, then the implementation reuses the existing Pattern-A `IProjectConversationDirectory.ListForProjectAsync` / context evaluation path or an additive Projects-owned ACL adapter; it does not persist local conversation membership or add a conversation lane to `ProjectReferenceIndexProjection` unless a later architecture decision explicitly approves it.
5. Given folder/file/memory summaries already flow through `GetProjectAsync` and `GetProjectOperatorDiagnosticsAsync`, when reference health is implemented, then it reuses `ProjectOperatorReferenceSummary`, generated `ProjectReferenceSummary`, `ProjectContextEvaluation`, and existing query semantics where sufficient; any contract/OpenAPI addition is additive, metadata-only, serialization-tolerant, and covered by OpenAPI/client regeneration and leakage tests.
6. Given absent, denied, stale, unavailable, archived, ambiguous, conflict, invalid, and filter-empty reference cases, when the matrix renders, then it distinguishes each state with visible text, accessible names, safe reason codes, and non-color-only badges; denial/unavailable states never collapse into a blank table.
7. Given Story 5.4 delivered the detail inspector with a placeholder References section, when this story completes, then that section becomes the full Reference Health Matrix and preserves the existing `ProjectDiagnosticHeader`, detail tabs/sections, route, query failure mapping, and keyboard navigation.
8. Given Epic 5 boundaries, when 5.5 completes, then it does not implement the Resolution Trace Workbench, audit timeline export, warnings dashboard, maintenance mutations, or MCP/CLI surfaces owned by Stories 5.6-5.10, except for documenting the parity field names those later surfaces must reuse.
9. Given responsive and accessibility requirements, when the matrix renders at desktop, tablet, and mobile breakpoints, then critical identifiers, owner context, states, reason codes, freshness, and safe actions remain visible or accessibly expandable; long identifiers wrap or expose full values without layout overlap.
10. Given test automation requirements, when the story is implemented, then bUnit/source/contract/leakage tests cover all row kinds and failure states, Playwright fixme/page-object selectors are updated, the FrontComposer inspect gate passes, and build/test verification is documented.

## Tasks / Subtasks

- [x] Define the reference health row contract and source strategy (AC: 1, 2, 4, 5, 8)
  - [x] Add a thin metadata-only FrontComposer descriptor/wrapper under `src/Hexalith.Projects.Contracts/Ui/`, for example `ProjectReferenceHealthProjection` or `ProjectReferenceHealthRowProjection`, using `[Projection]`, `[ProjectionRole(DetailRecord)]`, `[BoundedContext("Projects")]`, `[ColumnPriority]`, `[RelativeTime]`, and `[ProjectionFieldGroup]` only where the generator already supports the view.
  - [x] Model the row fields as safe metadata only: stable row id, `ProjectId`, reference kind (`conversation`, `folder`, `file`, `memory`), owner context (`Conversations`, `Folders`, `Memories`), reference id, optional safe display label, `ReferenceState`/inclusion state, health state if distinct, `ProjectReasonCode?`, `ProjectContextInclusionCheck?`, diagnostic code, observed/last-checked timestamp, freshness trust state/watermark, and a safe action availability label.
  - [x] Prefer existing contract DTOs over new backend shape: `ProjectOperatorReferenceSummary` for folder/file/memory summaries and `ProjectContextEvaluation` from the Story 3.3 explain path for per-reference inclusion diagnostics. Add new DTO properties only when the existing DTOs cannot express a required AC safely.
  - [x] For conversation rows, reuse `IProjectConversationDirectory.ListForProjectAsync(...)` and `ProjectContextConversationEvidenceMapper`/`ProjectContextEvaluation` semantics. Do not store Conversations membership locally and do not add Conversations client references outside `src/Hexalith.Projects.Server/Conversations/`.
  - [x] If an endpoint must be added or extended, make it a query with `X-Hexalith-Freshness: eventually_consistent`, no `Idempotency-Key`, safe 404/400/503 behavior, and OpenAPI/client regeneration. Consider extending operator diagnostics with conversation/evaluation metadata before creating a separate route.

- [x] Implement the Web Reference Health Matrix in the existing detail inspector (AC: 1, 6, 7, 9)
  - [x] Replace the Story 5.4 placeholder references table in `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` with a matrix component or section that keeps `project-detail-section-references` and adds stable selectors such as `project-reference-health-matrix`, `project-reference-health-row`, `project-reference-kind`, `project-reference-owner`, `project-reference-state`, `project-reference-reason`, `project-reference-last-checked`, and `project-reference-safe-actions`.
  - [x] Keep `ProjectDiagnosticHeader` above the inspector and preserve the `metadata`, `setup`, `references`, `resolution`, `audit`, and `actions` tabs. The References tab is the only tab becoming feature-complete in this story.
  - [x] Render statuses through `ProjectStatusBadge` / `ProjectVocabularyRendering.DescribeReferenceState(...)` and descriptors from `ProjectVocabularyDescriptors`; do not add a second severity table or string switch for reference health.
  - [x] Use semantic table/grid markup with a caption, scoped column headers, keyboard-reachable rows/actions, screen-reader-readable status text, and visible focus. Avoid hover-only controls.
  - [x] Keep the layout dense and operational. Desktop can use multi-column rows; tablet/mobile should reduce columns, stack secondary metadata, and keep full identifiers accessible without truncation-only behavior.
  - [x] Safe actions in this story are affordance labels or disabled/read-only entry points only, such as "inspect", "copy id", or "maintenance handled by Story 5.9". Do not implement unlink/relink/reevaluate/archive mutations here.

- [x] Update data loading and mapping without leaking payloads (AC: 2, 3, 4, 5, 6)
  - [x] Extend `ProjectDetailSource` and `ProjectGeneratedContractMapper` or add a focused `ProjectReferenceHealthSource` under `src/Hexalith.Projects.UI/Diagnostics/` to merge base detail/operator diagnostics and, if needed, context explanation/evaluation rows.
  - [x] Preserve generated-client query behavior: call read methods with caller-generated correlation id, `ReadConsistencyClass.Eventually_consistent`, no idempotency header, and cancellation token propagation.
  - [x] Map 404 to fail-closed feedback, 503 to retryable/data-unavailable feedback, 400 to safe validation feedback, and unexpected transport/deserialization exceptions to safe feedback without echoing exception text.
  - [x] For folder/file/memory, preserve ordering from current code where possible: folder first when present, file references ordered by id, memory references ordered by id. For the final matrix, sort deterministically by `(referenceKind, referenceId)` or document any UX-driven grouping; never rely on culture-sensitive ordering.
  - [x] For conversations, map `ProjectConversationTrustSignal` / context evaluations to the shared `ReferenceState` meanings already documented in `docs/context-assembly-decision-matrix.md`.
  - [x] If context explanation is used, ensure tenant id remains absent from the wire and that `ProjectContextEvaluation.Diagnostic` stays in the closed `ProjectContextInclusionDiagnostic` vocabulary.

- [x] Keep backend and contract changes additive and bounded (AC: 3, 4, 5, 8)
  - [x] If `ProjectOperatorDiagnostic` is extended, add only metadata-only reference health/evaluation fields and update `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`, generated client files, client generation tests, and compatibility/fingerprint tests.
  - [x] Do not change `ProjectReferenceIndexProjection` into a conversation membership store. It currently indexes folder/file/memory lanes; conversation membership is derived through the Conversations ACL.
  - [x] Do not widen `ProjectListProjection`; Story 5.5 is project-detail scoped and must not turn inventory rows into reference health payloads.
  - [x] Update `docs/projection-catalog.md` for any new FrontComposer descriptor/wrapper or public DTO field. Distinguish persisted projections from UI descriptors.
  - [x] Update `docs/parity-matrix.md` with Story 5.5 field names, row kinds, selectors, state/reason vocabulary, and MCP/CLI handoff names for Story 5.10.
  - [x] Update `docs/payload-taxonomy.md` and `PayloadClassification.SafeFields` only if a new safe UI evidence category is introduced.

- [x] Add focused tests and quality gates (AC: all)
  - [x] Add Contracts tests proving the new descriptor/wrapper has `ProjectionRole.DetailRecord`, bounded context metadata, stable columns/groups, and uses shared enum descriptor coverage.
  - [x] Add source tests for data loading/mapping: generated clients called with eventual consistency and no idempotency, safe failure mapping, diagnostic/evaluation fallback behavior, deterministic row ordering, and conversation trust-signal mapping.
  - [x] Add bUnit tests for all row kinds (`conversation`, `folder`, `file`, `memory`), empty/no references state, unauthorized/stale/unavailable/archived/conflict/invalid states, non-color-only badges, diagnostic codes, long identifiers, keyboard action affordances, and responsive-safe markup.
  - [x] Extend `NoPayloadLeakageTests` for any new DTO, UI projection descriptor, serialized evidence artifact, row model, feedback model, or rendered markup evidence.
  - [x] Update E2E fixme scaffolding and `tests/e2e/support/page-objects/project-detail.page.ts` selectors for the Reference Health Matrix. Keep full browser execution optional if local socket/AppHost permissions block it.
  - [x] Run:
    - [x] `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal`
    - [x] focused `dotnet test` for `ProjectVocabulary`, `ProjectsUI`, `NoPayloadLeakage`, `ClientGeneration`, `GetProject`, `ProjectContextExplanation`, `RefreshProjectContext`, and any new reference-health tests
    - [x] `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1`
    - [x] `git diff --check`

## Dev Notes

### Current State

- Story 5.4 implemented the Web inventory/detail baseline without backend/API expansion. The detail route `/projects/{ProjectId}` already renders `ProjectDiagnosticHeader`, tabs, and a placeholder `References` table from `_result.Detail.References`. [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- The current References section shows `Kind`, `Identifier`, `State`, `Reason`, and `Freshness` only. It lacks bounded-context owner, health/evaluation diagnostics, last-checked semantics beyond freshness text, conversation rows, and safe action affordances required by UX-DR8/UX-DR14. [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- `ProjectDetailSource` already loads base detail via `GetProjectAsync(projectId, correlationId, ReadConsistencyClass.Eventually_consistent, ct)` and non-blocking bounded diagnostics via `GetProjectOperatorDiagnosticsAsync(projectId, auditLimit: 25, ..., eventually_consistent, ct)`. It maps 404/503/400/API/transport failures to safe `ProjectConsoleFeedback`. Preserve that pattern. [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectDetailSource.cs]
- `ProjectGeneratedContractMapper.Merge(...)` replaces base detail references with operator-diagnostic references when diagnostics are available and carries audit/freshness from diagnostics. Any reference-health model should account for this merge behavior. [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectGeneratedContractMapper.cs]
- `ProjectOperatorReferenceSummary` currently contains `ReferenceKind`, `ReferenceState`, `ReferenceId`, `DisplayName`, `ReasonCode`, and `Freshness`. It is metadata-only and safe but does not include owner-context, failed-check, diagnostic, or action affordance fields. [Source: src/Hexalith.Projects.Contracts/Models/ProjectOperatorReferenceSummary.cs]
- Server operator diagnostics currently derive references from `ProjectDetailItem`: one folder row when present, file rows ordered by `FileReferenceId`, and memory rows ordered by `MemoryReferenceId`. Conversation rows are not included there. [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs#ToOperatorReferenceSummaries]
- The generated client already reserves `ProjectReferenceSummaryReferenceKind.Conversation`, but current detail/operator diagnostics do not produce conversation rows through `ProjectDetailProjection`. Treat that as a gap to bridge through the existing conversation ACL/context-evaluation path, not as permission to persist conversation membership locally. [Source: src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs#ProjectReferenceSummaryReferenceKind]

### Epic and UX Requirements

- Epic 5 delivers the Metadata Control Plane console and parity MCP/CLI surfaces over one diagnostic model; administrators must inspect reference health, resolution traces, and metadata-only audit history without payload exposure. [Source: _bmad-output/planning-artifacts/epics.md#Epic-5-Operational-Console--Audit-CLI--MCP--Web]
- Story 5.5 ACs require each row to show reference type, reference ID, bounded-context owner, inclusion state, health state, reason code, last-checked timestamp, and available safe actions with explicit headers and non-color-only status. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.5-Reference-inventory--health-view]
- UX-DR8 defines Reference inventory & health view as a `DetailRecord` sub-grid over linked Conversations, Project Folder, File References, and Memories with inclusion/health states, reason code, last-checked timestamp, and safe actions. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- UX-DR14 defines the Reference Health Matrix custom component fields and requires explicit grid headers and status that is not color-only. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- The UX spec's Journey 2 says the project inspection flow loads reference inventory metadata, maps valid/unauthorized/stale/unavailable/invalid outcomes into the health matrix, and only then opens safe maintenance preview if needed. Maintenance preview/execution remains Story 5.9. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Journey-2-Inspect-Project-Reference-Health]

### Reference Semantics and Source of Truth

- The context-assembly decision matrix is the single source for how evidence states map to surfaced `ReferenceState`, failed checks, diagnostics, and outer outcomes. 5.5 should use those states as display semantics, especially for conversation rows. [Source: docs/context-assembly-decision-matrix.md]
- Conversation trust signals map as: `Current` -> `Included + ConversationLinked`, `Stale`/`MixedGeneration` -> `Stale + ReferenceFreshness + referenceStale`, `Rebuilding`/`Unavailable` -> `Unavailable + ReferenceFreshness + referenceUnavailable`, `Forbidden` -> `Unauthorized + ReferenceAuthorization + referenceUnauthorized`, and `Redacted` -> `Excluded + ReferenceFreshness + referenceRedacted`. [Source: docs/context-assembly-decision-matrix.md#Conversation-rows-Story-2.1]
- Folder rows include the single Project Folder invariant and map pending/archived/unavailable/tenant-mismatch through shared `ReferenceState` values. There is exactly one Project Folder reference, never a list. [Source: docs/context-assembly-decision-matrix.md#Project-Folder-rows-Story-2.4]
- File rows map included/archived/stale/unavailable/unauthorized/invalidReference through shared states and diagnostics. [Source: docs/context-assembly-decision-matrix.md#File-reference-rows-Story-2.5]
- Memory rows map included/archived/unauthorized/unavailable/invalidReference/tenantMismatch through shared states and diagnostics. [Source: docs/context-assembly-decision-matrix.md#Memories-specific-rows-Story-2.6-ADR-Epic-3-allowlist-treatment]

### Conversation Boundary Guardrails

- Conversation membership is Pattern A: query Conversations through a Projects-owned ACL. Projects must not store local conversation membership. [Source: _bmad-output/implementation-artifacts/4-2-resolve-project-from-conversation.md#Authorized-conversation-read--Pattern-A-ACL-only]
- `IProjectConversationDirectory.ListForProjectAsync(ProjectId, TenantId, CallerPrincipalId, PageRequest, ct)` is the existing project-keyed conversation reference reader. It returns `ProjectConversationsPage` with metadata-only `ProjectConversationItem` rows and an aggregate trust signal. [Source: src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs] [Source: src/Hexalith.Projects.Contracts/Queries/ProjectConversationsPage.cs]
- `ConversationsProjectConversationDirectory` is the only allowed real adapter over `IConversationClient.ListConversationsAsync`; it maps unauthorized/not-found to `Forbidden`, other failures/exceptions to `Unavailable`, and never leaks upstream response bodies. [Source: src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationDirectory.cs]
- `ProjectConversationTranslator` re-checks every returned row against requested tenant and project scope and collapses the page to empty/unavailable if any row escapes scope. Preserve that fail-closed behavior. [Source: src/Hexalith.Projects.Server/Conversations/ProjectConversationTranslator.cs]
- `ProjectContextConversationEvidenceMapper` converts conversation pages into context evidence using an injected `now` timestamp, with no DI, HTTP, Dapr, wall-clock read, or payload access. Reuse or mirror this mapping rather than inventing a UI-only conversation interpretation. [Source: src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs]

### FrontComposer / Fluent UI Guardrails

- Architecture maps reference inventory to `ProjectionRole.DetailRecord`; resolution trace is the only explicitly known Level-3/4 candidate. Start at FrontComposer Level 1/DetailRecord or a narrow sub-grid component inside the existing detail route. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- Story 5.4 recorded a deliberate Level-4 escalation for the overall inventory/detail inspector because the exact shell/header/tabs/disabled filters/stable selectors were not expressible by generated DataGrid/DetailRecord output. 5.5 may extend that existing hand-rolled inspector, but keep descriptor/wrapper metadata so downstream parity and FrontComposer inspection remain covered. [Source: _bmad-output/implementation-artifacts/5-4-project-inventory-detail-views.md#Senior-Developer-Review-AI]
- Existing UI services are registered in `src/Hexalith.Projects.UI/Program.cs`: `AddFluentUIComponents`, `AddProjectsClient`, `IProjectInventorySource`, `IProjectOperatorDiagnosticSource`, `IProjectDetailSource`, `AddHexalithFrontComposerQuickstart`, and `AddHexalithDomain<ProjectsFrontComposerDomain>`. Do not create a bespoke UI framework or bypass generated client sources. [Source: src/Hexalith.Projects.UI/Program.cs]
- Local package pins on 2026-05-30 are authoritative: .NET SDK `10.0.302`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, bUnit `2.7.2`, xUnit v3 `3.2.2`, Dapr `1.17.9`, Aspire `13.3.5`. Do not upgrade/downgrade or inline package versions. [Source: global.json] [Source: Directory.Packages.props] [Source: Hexalith.FrontComposer/Directory.Packages.props]
- External package check on 2026-05-30: NuGet lists `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` as a prerelease version published 2026-05-19 while the main stable page is `4.14.2`; this confirms the repo's pinned prerelease posture, not permission for package churn. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]

### Project Structure Notes

- UI page changes belong in `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` and `.razor.css`, or in a small shared component under `src/Hexalith.Projects.UI/Components/Shared/` if the matrix becomes reusable.
- UI data-source/mapping changes belong under `src/Hexalith.Projects.UI/Diagnostics/`.
- FrontComposer descriptor/wrapper contracts belong under `src/Hexalith.Projects.Contracts/Ui/`.
- Server conversation ACL changes, if unavoidable, belong under `src/Hexalith.Projects.Server/Conversations/`; server query composition belongs in `ProjectsDomainServiceEndpoints` partials or existing query endpoint files, following Story 3.3/3.4 patterns.
- Tests belong in existing projects: UI component/source tests under `tests/Hexalith.Projects.UI.Tests/`, contract descriptor tests under `tests/Hexalith.Projects.Contracts.Tests/Ui/`, server query/ACL tests under `tests/Hexalith.Projects.Server.Tests/`, leakage tests under `tests/Hexalith.Projects.Tests/Leakage/`, and E2E fixme/page objects under `tests/e2e/`.
- Do not read or modify BMAD folders inside submodules. Do not initialize nested submodules. Do not create submodule pointer churn.

### Previous Story Intelligence

- Story 5.4's review fixed transport/non-API exception handling in all UI sources and added tests proving safe fallback. New 5.5 sources must include the same terminal safe exception mapping. [Source: _bmad-output/implementation-artifacts/5-4-project-inventory-detail-views.md#Senior-Developer-Review-AI]
- Story 5.4 left full reference health as a future-story boundary and explicitly documented that the current detail inspector references are summaries only. Replace that boundary with the 5.5 implementation and update docs so later stories do not think the placeholder remains. [Source: docs/parity-matrix.md#Story-5.4-Inventory--Detail-Contract]
- Story 5.3 delivered `ProjectDiagnosticHeader`, shared badges, empty states, feedback, warning counting, and `ProjectVocabularyDescriptors`; reuse them. [Source: src/Hexalith.Projects.Contracts/Ui/ProjectVocabularyDescriptors.cs] [Source: src/Hexalith.Projects.UI/Rendering/ProjectVocabularyRendering.cs]
- Recent git history shows 5.4 committed at `cc7e96f` and the current root HEAD at `6f18018`. The root working tree has an unrelated modified story-automator orchestration file; do not revert or rewrite it. [Source: git log --oneline -5] [Source: git status --short]

### Hard Stops

- Stop before coding if implementation requires storing conversation membership in Projects or adding a conversation lane to `ProjectReferenceIndexProjection` without an explicit architecture decision.
- Stop before coding if the matrix requires sibling payloads, folder paths, file contents, memory bodies, transcripts, raw prompts, tokens, command/proposal bodies, candidate scores/ranks, rejected ids, or raw sibling denial details.
- Stop before coding if a new Web-only reference status enum, reason-code enum, severity table, or adapter-local vocabulary appears necessary.
- Stop before coding if generated FrontComposer files or generated client `.g.cs` files appear to need hand edits.
- Stop before coding if the story starts implementing Story 5.6 resolution traces, Story 5.7 audit export/timeline, Story 5.8 warnings dashboard, Story 5.9 maintenance mutations, or Story 5.10 MCP/CLI surfaces.
- Stop before coding if package upgrades/downgrades, analyzer suppressions, nullable disable, warning downgrade, central package management bypass, submodule pointer changes, nested submodule init, or BMAD reads inside submodules are required.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-5.5-Reference-inventory--health-view]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Journey-2-Inspect-Project-Reference-Health]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- [Source: docs/context-assembly-decision-matrix.md]
- [Source: docs/parity-matrix.md#Story-5.4-Inventory--Detail-Contract]
- [Source: docs/projection-catalog.md#ProjectReferenceIndexProjection]
- [Source: docs/payload-taxonomy.md]
- [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectDetailSource.cs]
- [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectGeneratedContractMapper.cs]
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectOperatorReferenceSummary.cs]
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- [Source: src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs]
- [Source: src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationDirectory.cs]
- [Source: src/Hexalith.Projects.Server/Conversations/ProjectConversationTranslator.cs]
- [Source: src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs]
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectContextEvaluation.cs]
- [Source: tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs]
- [Source: tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextExplanationTests.cs]
- [Source: tests/e2e/support/page-objects/project-detail.page.ts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-30: Implemented `ProjectReferenceHealthRowProjection` as the metadata-only FrontComposer DetailRecord descriptor/wrapper; first attempt as a record compiled but did not emit FrontComposer generated output, so it was aligned with existing partial-class projection descriptors.
- 2026-05-30: `dotnet test` via VSTest compiled affected projects but aborted on sandbox local-socket permissions; xUnit v3 in-process runner was used for focused Projects UI, Contracts, Client, and leakage lanes.
- 2026-05-30: Server context endpoint focused lane was attempted with xUnit v3 in-process but all selected tests require Kestrel socket binding and failed before assertions with `System.Net.Sockets.SocketException (13): Permission denied`; no server code was changed for this story.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Create-story validation applied against checklist: story includes source-derived ACs, current file locations, source DTO/endpoint constraints, conversation-boundary guidance, FrontComposer/Fluent UI guardrails, test requirements, previous-story learnings, and hard stops.
- Added metadata-only `ProjectReferenceHealthRowProjection` with shared Projects vocabulary fields, FrontComposer DetailRecord metadata, freshness/diagnostic/action field groups, and leakage coverage.
- Replaced the Story 5.4 References placeholder with the Reference Health Matrix in the existing `/projects/{ProjectId}` inspector while preserving `ProjectDiagnosticHeader`, tabs, route, safe feedback rendering, and read-only Story 5.9 action boundaries.
- Extended `ProjectDetailSource` with non-blocking generated-client calls to `GetProjectContextExplanationAsync` and `ListProjectConversationsAsync`, using eventual consistency, caller-generated correlation id, cancellation propagation, and safe 400/404/503/transport feedback mapping.
- Added `ProjectReferenceHealthMapper` to merge base/operator reference summaries, context evaluations, and conversation ACL rows without backend/OpenAPI changes, local conversation membership, `ProjectReferenceIndexProjection` changes, or payload exposure.
- Updated projection/parity documentation and E2E page-object selectors for Story 5.5 MCP/CLI handoff field names.
- Verification: solution build passed with warn-as-error; FrontComposer inspect gate passed; focused in-process Contracts/UI/Client/leakage tests passed; `git diff --check` clean. Server context endpoint focused tests were attempted but blocked by local socket permissions before assertions.

### File List

- _bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- docs/parity-matrix.md
- docs/projection-catalog.md
- src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs
- src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor
- src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor.css
- src/Hexalith.Projects.UI/Diagnostics/ProjectDetailLoadResult.cs
- src/Hexalith.Projects.UI/Diagnostics/ProjectDetailSource.cs
- src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs
- tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs
- tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs
- tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs
- tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectDetailSourceTests.cs
- tests/e2e/support/page-objects/project-detail.page.ts
- tests/e2e/specs/projects-reference-health.spec.ts
- tests/e2e/specs/projects-accessibility.spec.ts
- tests/e2e/support/helpers/projects-api-client.ts

### Change Log

- 2026-05-30: Created Story 5.5 context package for Reference Inventory & Health View; status set to ready-for-dev.
- 2026-05-30: Implemented Reference Health Matrix, metadata-only row descriptor, generated-client enrichment/mapping, tests, selectors, and parity/projection documentation; status set to review.
- 2026-05-30: Senior Developer Review (AI) completed — verified build/tests/inspect-gate, fixed File List completeness (added the three E2E artifacts), recorded Low-severity follow-ups; 0 Critical remaining, status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-05-30
**Outcome:** Approve (0 Critical / 0 High). Story status advanced to `done`.

### Verification performed

- `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- Focused tests (in-process, sandbox-disabled, `-m:1`): Contracts `ProjectVocabularyTests` (35), UI `ProjectDetailPageTests`+`ProjectDetailSourceTests` (13), `NoPayloadLeakage` incl. `ProjectReferenceHealthRowProjection_SerializesMetadataOnly` (55) → **all passed**.
- `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` → **PASSED, no warnings**; `ProjectReferenceHealthRowProjection` emits ProjectionRazor + Fluxor + Registration generated output.
- Confirmed `ListProjectConversationsAsync` / `GetProjectContextExplanationAsync` and the conversation/context DTOs already exist in `HexalithProjectsClient.g.cs`; no generated-client/OpenAPI edits were required (additive, hard-stop respected).
- `git diff --check` clean (verified during dev run).

### Acceptance Criteria

- AC1–AC8, AC10: **Implemented and verified.** Matrix renders all ten metadata-only columns; shared vocabulary (`ReferenceState`/`ProjectReasonCode`/`ProjectContextInclusionCheck`/`ProjectContextInclusionDiagnostic`/`ProjectVocabularyDescriptors`) is reused; conversation rows derive from the Conversations ACL (no local membership, no `ProjectReferenceIndexProjection` lane); detail header/tabs/route/failure mapping preserved; later-story boundaries (5.6/5.9) shown as read-only affordances; tests cover all row kinds + failure states; inspect gate green.
- AC9: **Satisfied** (identifiers remain accessible — `overflow-x:auto` + `overflow-wrap:anywhere`, no truncation-only), with a Low note below.

### Findings

- **[Medium][Fixed] File List incomplete.** Three changed E2E artifacts were absent from the Dev Agent Record → File List: `tests/e2e/specs/projects-reference-health.spec.ts` (new — primary AC10 spec), `tests/e2e/support/helpers/projects-api-client.ts` (new helper used by that spec), and `tests/e2e/specs/projects-accessibility.spec.ts` (modified to navigate the references tab). Added during this review.
- **[Low][Follow-up] `FreshnessTrustState` mixes three vocabularies in one column.** Folder/file/memory rows surface `ProjectionTrustState` (e.g. `trusted`), evaluation-merged rows surface `ProjectContextFreshness` (e.g. `fresh`), conversation rows surface the lowercased `ProjectConversationTrustSignal` (e.g. `current`/`stale`). All values are safe metadata; this is operator-clarity only. Not auto-fixed — canonicalizing is a design decision.
- **[Low][Follow-up] Responsive matrix uses horizontal scroll, not column reduction.** The dense matrix keeps all ten columns at every breakpoint (`min-width` 68/56/48rem behind `overflow-x:auto`) rather than reducing/stacking secondary metadata as the task hint suggested. AC9 is still met. Not auto-fixed — legitimate dense-ops pattern; layout change is a UX decision.
- **[Low][Follow-up] `cursor: null!` in `ProjectDetailSource.ListProjectConversationsAsync` call.** Null-forgiving on a non-nullable generated `string cursor` parameter. Functionally correct (omits the cursor); cosmetic smell only.

### Review Follow-ups (AI)

- [ ] [AI-Review][Low] Canonicalize the `FreshnessTrustState` column vocabulary across reference/evaluation/conversation sources [src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs]
- [ ] [AI-Review][Low] Decide whether tablet/mobile should reduce/stack columns instead of horizontal scroll [src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor.css]
- [ ] [AI-Review][Low] Replace `cursor: null!` with an explicit no-cursor value once the generated signature allows it [src/Hexalith.Projects.UI/Diagnostics/ProjectDetailSource.cs:126]
