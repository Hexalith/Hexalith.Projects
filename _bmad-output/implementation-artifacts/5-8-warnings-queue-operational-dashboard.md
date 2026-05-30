---
story_id: 5.8
story_key: 5-8-warnings-queue-operational-dashboard
epic: "Epic 5: Operational Console & Audit (CLI / MCP / Web)"
created: 2026-05-30
source_story_status: backlog
baseline_commit: 079a2f9
---

# Story 5.8: Warnings Queue & Operational Dashboard

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an administrator,
I want a warnings/maintenance queue and a cross-project operational dashboard,
so that I can triage projects needing intervention and see overall health.

## Acceptance Criteria

1. Given projects or references in attention-needed states, when the Warnings Queue renders, then it lists tenant-scoped, metadata-only queue items for stale, conflict, invalid reference, unauthorized, unavailable, archived, ambiguous, and fail-closed conditions with project identity, lifecycle, reference kind/id where applicable, shared state, reason code, last observed timestamp, freshness evidence, and read-only safe next-action labels.
2. Given shared vocabulary requirements, when warning states and reason codes are mapped, then the implementation uses existing `ReferenceState`, `ProjectReasonCode`, `ProjectContextInclusionCheck`, `ProjectContextInclusionDiagnostic`, `ProjectVocabularyDescriptors`, and `ProjectDiagnosticRendering` semantics. Unknown enum or diagnostic member names must fail tests/build or render a safe explicit unknown/unavailable warning; they must never silently no-match, disappear, or use Web-only enum/severity tables.
3. Given the current Story 5.4 inventory limitation, when warning filtering and dashboard aggregation are implemented, then the developer either adds/reuses additive metadata-only summary fields on the existing list/operator diagnostic model or uses a bounded generated-client enrichment strategy over already authorized rows. Do not create a duplicate operator inventory endpoint, tenant-bypassing local cache, sibling fan-out from Web, or unsafe client-derived tenant scope.
4. Given the dashboard renders, then it shows tenant-scoped aggregated health/status tiles for total visible projects, active, archived, projects with warnings, stale references, conflicts, invalid references, unavailable/unauthorized references, ambiguous/multiple-candidate or fail-closed resolution evidence when available, and diagnostic freshness/data-unavailable counts. Tiles are non-color-only and drill into the queue/list filters without changing state.
5. Given the queue renders as an `ActionQueue`-style view, then it supports filters for warning state, reason code, reference type, lifecycle, and updated/last-checked timestamp where backed by safe metadata. Unsupported filters must remain visibly disabled with the existing disabled-filter explanation pattern rather than pretending to filter hidden data.
6. Given a queue row is selected, when the user follows a safe action, then read-only actions navigate to the existing detail tabs/workbenches (`/projects/{ProjectId}`, References, Resolution, Audit) or copy safe identifiers. Mutating actions remain unavailable and clearly labeled as Story 5.9 maintenance scope; this story must not archive, restore, relink, unlink, or reevaluate.
7. Given safe feedback and empty-state requirements, when no warnings exist, access is denied, data is unavailable, validation fails, or filters return no results, then the UI distinguishes "no warnings", "access denied", "data unavailable", "validation error", and "filter returned no results" using `ProjectEmptyState` / `ProjectConsoleFeedback`; it must never render a blank dashboard or empty table for denial/unavailable cases.
8. Given FrontComposer delivery constraints, when queue/dashboard descriptors are added, then the lowest sufficient FrontComposer gradient is attempted first with metadata-only `[Projection]` wrapper contracts, for example `ProjectWarningQueueItemProjection` and `ProjectOperationalDashboardProjection`. Hand-authored Blazor composition is allowed only where generated Level-1/2 output cannot express filtering, aggregation, or drill-in behavior; descriptor metadata must still exist for inspect/parity gates.
9. Given payload safety, when queue items, dashboard tiles, UI markup, logs, tests, docs, export handoffs, or future CLI/MCP descriptors are produced, then they contain no transcript text, prompt, raw setup text, file path/content, byte range, workspace id, memory payload, secret, token, raw ProblemDetails body, command/proposal body, idempotency key, candidate score/rank, rejected candidate id, tenant authority from client input, or sibling denial detail.
10. Given responsive and accessibility requirements, when the dashboard and queue render, then desktop supports dense tiles plus table/list triage, tablet stacks tiles and preserves filters, and mobile prioritizes project identity, tenant scope, lifecycle, warning count, top reason codes, and safe drill-in links. Tables/lists have semantic headings, explicit headers, visible focus, copyable IDs, no hover-only critical actions, no color-only status, and stable selectors.
11. Given Epic 5 story boundaries, when this story completes, then it does not implement Story 5.9 maintenance mutation flows, Story 5.10 broad MCP/CLI adapter wiring, or Story 5.11 final responsive/a11y hardening beyond focused queue/dashboard tests and descriptors. It may update `docs/parity-matrix.md` with exact field names and handoff notes for Story 5.10.
12. Given test automation requirements, when the story is implemented, then contract/source/bUnit/leakage tests cover descriptor metadata, warning extraction, enum exhaustiveness, aggregation counts, safe failure mapping, filter behavior, empty states, selector stability, no payload leakage, and safe drill-ins; Playwright fixme/page-object selectors are added or updated; the FrontComposer inspect gate, solution build, focused tests, and `git diff --check` are documented.

## Tasks / Subtasks

- [x] Define queue/dashboard descriptor contracts (AC: 1, 2, 4, 8, 9)
  - [x] Add metadata-only FrontComposer wrapper contracts under `src/Hexalith.Projects.Contracts/Ui/`, for example `ProjectWarningQueueItemProjection` and `ProjectOperationalDashboardProjection`, using `[Projection]`, `[BoundedContext("Projects")]`, display metadata, stable field names, column priorities, field groups, and explicit contract version constants.
  - [x] Queue item fields should include `projectId`, safe project name, `lifecycle`, warning `state`, `reasonCode`, `referenceKind`, `referenceId`, `ownerContext`, `lastObservedAt`, `freshnessTrustState`, `projectionWatermark`, `sourceSection`, and read-only `safeActionAvailabilityLabel`.
  - [x] Dashboard fields should include aggregate counts and freshness/data-availability evidence only. Do not include raw per-project payloads, raw resolution trace details, candidate score/rank, or hidden sibling denial details.
  - [x] Use existing shared enums and descriptor lookup. If a source string cannot map to a shared enum, surface it as a safe unavailable/unknown diagnostic row and add tests; do not add parallel enum members casually.

- [x] Build or extend a safe data source for warning extraction and aggregation (AC: 1, 3, 4, 5, 7)
  - [x] Prefer a focused UI source such as `IProjectWarningsDashboardSource` / `ProjectWarningsDashboardSource` under `src/Hexalith.Projects.UI/Diagnostics/`.
  - [x] Start from the existing generated `ListProjectsAsync(lifecycle, correlationId, eventually_consistent, ct)` inventory source for project identity/lifecycle/freshness. Reuse `ProjectInventorySource` patterns for safe 400/404/503/transport mapping.
  - [x] For warning details, use existing authorized metadata only: `GetProjectOperatorDiagnosticsAsync(projectId, auditLimit: 25, correlationId, eventually_consistent, ct)` and `ProjectReferenceHealthMapper.BuildRows(...)`, or justified additive metadata-only summary fields on the existing public model. Do not create a duplicate `/warnings`, `/dashboard`, or operator-inventory endpoint unless the existing model is proven insufficient and the new contract is additive, tenant-scoped, and metadata-only.
  - [x] Bound enrichment to the visible/loaded project set if using generated-client per-row diagnostics. Do not perform unbounded browser-side fan-out. Capture partial diagnostics failures as safe warning/data-unavailable counts while preserving rows that loaded successfully.
  - [x] Preserve query semantics: caller-generated correlation id, eventual consistency header, cancellation propagation, no `Idempotency-Key`, no mutation submission, and no 202 command lifecycle in this story.

- [x] Implement the Warnings Queue UI (AC: 1, 2, 5, 6, 7, 10)
  - [x] Add an `ActionQueue`-style section on the inventory route, a dedicated route such as `/projects/warnings`, or an operational dashboard band on `/projects` consistent with existing navigation. Keep `/projects/{ProjectId}` detail tabs intact.
  - [x] Add stable selectors: `project-warnings-dashboard`, `project-dashboard-tile`, `project-warnings-queue`, `project-warning-row`, `project-warning-state`, `project-warning-reason`, `project-warning-reference`, `project-warning-freshness`, `project-warning-safe-action`, `project-warning-filter-state`, `project-warning-filter-reason`, `project-warning-filter-reference-type`, `project-warning-filter-lifecycle`, and `project-warning-empty`.
  - [x] Render warning states with `ProjectStatusBadge` / `ProjectVocabularyDescriptors` and visible text. State meaning must not depend on color.
  - [x] Safe row actions may navigate to existing detail tabs/workbenches or copy IDs. Disable or label maintenance actions as "Handled by Story 5.9"; do not wire mutation buttons.
  - [x] Empty and failure states must use existing `ProjectEmptyStateView` / `ProjectFeedbackView` patterns. Add `NoWarnings()` only if needed, with tests and parity docs.

- [x] Implement the operational dashboard/status overview (AC: 4, 5, 7, 10)
  - [x] Add dashboard/status overview tiles above or beside the queue using restrained operational styling, not a marketing dashboard or decorative card layout.
  - [x] Include counts for total visible projects, active, archived, warning projects, stale, conflict, invalid reference, unauthorized/unavailable, ambiguous/fail-closed where data exists, and diagnostic unavailable.
  - [x] Tile interactions should filter the queue or navigate to the existing filtered inventory/detail routes. They must not mutate state or trigger maintenance commands.
  - [x] Dashboard count labels must be screen-reader-readable and include text labels, not only icons or color.

- [x] Preserve and update documentation/parity handoffs (AC: 8, 9, 11)
  - [x] Update `docs/parity-matrix.md` with Story 5.8 field names, selectors, Web behavior, CLI/MCP handoff resource names, and explicit Story 5.10 scope boundary.
  - [x] Update `docs/projection-catalog.md` for any new UI descriptor/wrapper contracts, clearly distinguishing them from persisted runtime projections.
  - [x] Update `docs/payload-taxonomy.md` and `PayloadClassification.SafeFields` only if a genuinely new safe category is required. Prefer existing `OpaqueId`, `ReferenceKind`, `OwnerContext`, `Timestamp`, `LifecycleState`, `InclusionState`, `ResolutionState`, `ReasonCode`, `CorrelationId`, `AuditId`, `UiFeedbackCode`, and `UiProjectionDescriptor`.

- [x] Add focused tests and quality gates (AC: all)
  - [x] Add Contracts tests proving descriptor metadata, contract versions, field names, `ProjectionRole`/field grouping where available, and shared enum property types.
  - [x] Add source tests for warning extraction from `ProjectOperatorReferenceSummary` / reference-health rows, enum exhaustiveness, unknown-string behavior, partial diagnostic failures, query call shape, eventual consistency, cancellation, no idempotency, and 400/404/503/transport mapping.
  - [x] Add bUnit tests for dashboard tile counts, queue row rendering, filters, empty/failure states, safe drill-in links, disabled maintenance actions, non-color-only labels, and stable selectors.
  - [x] Extend `NoPayloadLeakageTests` for any new DTO/descriptor/serialized fixture/rendered markup.
  - [x] Update `tests/e2e/support/page-objects/project-detail.page.ts` or add a focused inventory/dashboard page object with Story 5.8 selectors; add `test.fixme` Playwright specs for queue/dashboard triage, filters, a11y/axe, and no payload leakage.
  - [x] Run:
    - [x] `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal`
    - [x] focused `dotnet test` for warning/dashboard contracts, UI source, bUnit rendering, `ProjectVocabulary`, and `NoPayloadLeakage`
    - [x] `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1`
    - [x] `git diff --check`

## Dev Notes

### Current State

- Epic 5 delivers the FrontComposer-generated Metadata Control Plane console plus Web/MCP/CLI parity over one safe diagnostic model. Story 5.8 owns UX-DR11 `ActionQueue` warnings/maintenance queue and UX-DR12 `Dashboard`/`StatusOverview` cross-project health overview. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.8-Warnings-queue--operational-dashboard]
- Story 5.4 inventory renders `/` and `/projects` from `ListProjectsAsync(...)`. Current list rows expose project id/name/lifecycle/timestamps/freshness only; warning, reason-code, and reference-type filters are deliberately disabled with "Unavailable on list rows" / "Requires summary field". Story 5.8 must close that gap safely, not fake it. [Source: src/Hexalith.Projects.UI/Components/Pages/Home.razor] [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectInventorySource.cs] [Source: docs/parity-matrix.md#Story-5.4-Inventory--Detail-Contract]
- Story 5.3 already provides `ProjectDiagnosticHeader`, shared badge rendering, `ProjectConsoleFeedback`, and `ProjectEmptyStateView`. The header warning count is computed by `ProjectDiagnosticRendering.CountWarnings(ProjectOperatorDiagnostic)` and treats any reference not parsed as `ReferenceState.Included` as requiring attention. Reuse or tighten that semantic; do not invent a separate warning algorithm. [Source: src/Hexalith.Projects.UI/Components/Shared/ProjectDiagnosticHeader.razor] [Source: src/Hexalith.Projects.UI/Rendering/ProjectDiagnosticRendering.cs]
- Story 5.5 delivered the Reference Health Matrix through `ProjectReferenceHealthRowProjection` and `ProjectReferenceHealthMapper.BuildRows(...)`, merging existing operator reference summaries, context-evaluation diagnostics, and conversation ACL rows. This is the richest existing source for queue items. [Source: src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs] [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs]
- Story 5.7 delivered the Audit tab timeline and safe export over `GetProjectOperatorDiagnosticsAsync(...)`, with bounded audit limits and safe failure mapping. Story 5.8 can reuse the same generated operator diagnostic query, but must not export or mutate anything. [Source: _bmad-output/implementation-artifacts/5-7-audit-timeline-view-safe-diagnostic-export.md] [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectAuditTimelineSource.cs]

### Data Source and Aggregation Guidance

- Preferred implementation path: reuse existing generated query clients and add UI/parity descriptors first. If aggregate health requires list-level metadata not currently present, add only additive metadata-only summary fields to the existing public list/operator model and regenerate through the OpenAPI spine. Do not hand-edit generated `.g.cs`. [Source: _bmad-output/planning-artifacts/architecture.md#API--Communication-Patterns] [Source: docs/parity-matrix.md#Story-5.4-Inventory--Detail-Contract]
- Acceptable bounded fallback: use `ListProjectsAsync(...)` for the tenant-scoped visible project set, then call `GetProjectOperatorDiagnosticsAsync(...)` per visible row with cancellation and partial-failure handling. This is a UI source strategy, not a new source of truth. It must be bounded, testable, and safe under partial 404/503/transport failures.
- Do not make Web call sibling bounded contexts directly for dashboard metrics. Conversations/Folders/Memories evidence must arrive through existing Projects server/operator/context-query surfaces or approved additive Projects DTOs. [Source: _bmad-output/planning-artifacts/architecture.md#Component-boundaries]
- Do not derive tenant authority from URL, UI route, headers, query inputs, or local storage. Tenant scope labels shown in Web are display-only and server-derived. [Source: _bmad-output/planning-artifacts/architecture.md#Security-Architecture]

### Warning Semantics

- Attention-needed reference states include shared `ReferenceState` values other than `Included`, especially `Stale`, `Conflict`, `InvalidReference`, `Unauthorized`, `Unavailable`, `Archived`, `Ambiguous`, `TenantMismatch`, and `Excluded` when the diagnostic indicates operator action or safe investigation. Use shared descriptor labels and accessible names. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR5-Status--reason-code-pattern] [Source: src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs]
- Reason codes must come from `ProjectReasonCode` or approved closed diagnostic strings in `ProjectContextInclusionDiagnostic`. Unknown names should be explicit safe diagnostics and covered by tests; silent no-match is a regression. [Source: src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs] [Source: src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionDiagnostic.cs]
- The queue should prioritize actionable triage, not broad analytics. Sort by severity/state, last observed timestamp, project id, reference kind, and reference id deterministically. Avoid wall-clock urgency unless backed by safe observed/freshness metadata.
- Maintenance actions are read-only labels in this story. The safe action label can say "Inspect metadata", "Open references", "Open audit", "Run trace", "Copy ID", or "Maintenance handled by Story 5.9". It must not submit commands.

### FrontComposer / Fluent UI Guardrails

- Architecture maps warnings to `ProjectionRole.ActionQueue` and dashboard to `Dashboard`/`StatusOverview`; if current FrontComposer attributes do not expose these exact roles, use the closest supported descriptor metadata and document the gap. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- Reuse existing Blazor/Fluent/FrontComposer primitives and local CSS patterns from `Home.razor`, `ProjectDiagnostics.razor`, `ProjectAuditTimelineSection`, `ProjectResolutionTraceWorkbench`, `ProjectStatusBadge`, `ProjectFeedbackView`, and `ProjectEmptyStateView`. Do not introduce a bespoke UI framework, decorative dashboard visual language, nested card layouts, or custom component library.
- Local package pins on 2026-05-30 are authoritative: .NET SDK `10.0.300`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, bUnit `2.7.2`, xUnit v3 `3.2.2`, Dapr `1.17.9`, Aspire `13.3.5`, and Verify.XunitV3 `31.17.0`. Do not upgrade/downgrade or inline package versions. [Source: global.json] [Source: Directory.Packages.props] [Source: Hexalith.FrontComposer/Directory.Packages.props]
- Attempted Fluent UI Blazor MCP version/component lookup during story creation, but the MCP tool calls were cancelled by the environment. Treat the pinned package and existing local component usage as authoritative for implementation.

### UX Requirements

- UX-DR11 defines the Warnings / maintenance queue as `ActionQueue` with `WhenState` filters such as `Stale`, `Conflict`, and `InvalidReference`; it lists pending items needing intervention. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- UX-DR12 defines the dashboard/status overview as aggregated cross-project health/status tiles. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- UX-DR4 requires dense operational layouts: filter bars and command bars near affected data, warning panels near affected resources, and action panels separated from read-only diagnostics. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- UX-DR22/23 require distinct safe feedback and empty states: success/warning/error/fail-closed/loading, no warnings, denied, unavailable, and filter-empty must not collapse into blank tables. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Additional-Patterns]
- UX-DR26/27/28 require responsive behavior, WCAG 2.2 AA accessibility, and stable selectors. Critical metadata, warnings, reason codes, and action consequences remain visible at every viewport. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Responsive-Design--Accessibility]

### Previous Story Intelligence

- Story 5.7 safe export forbids candidate score/rank, raw setup text, idempotency keys, command/proposal bodies, and sibling denial detail from diagnostic handoff content. Story 5.8 queue/dashboard must keep the same boundary in rendered markup, docs, logs, and tests. [Source: _bmad-output/implementation-artifacts/5-7-audit-timeline-view-safe-diagnostic-export.md]
- Story 5.6 review fixed a `ReferenceRedacted` classification bug: policy/redacted exclusions belong to `Excluded`, not `FailedClosed`. Preserve that policy-vs-unverifiable boundary when deriving warning buckets. [Source: _bmad-output/implementation-artifacts/5-6-resolution-trace-workbench.md#Senior-Developer-Review-AI]
- Story 5.5 left freshness vocabulary and responsive horizontal-scroll ambiguity as follow-ups. Do not normalize mixed freshness values with a Web-only vocabulary; preserve source safe field names and render accessible full values for long identifiers/reason codes. [Source: _bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md#Senior-Developer-Review-AI]
- Story 5.4 review fixed transport/non-API exception handling so UI sources map failures to safe feedback instead of crashing the Blazor circuit. Any new queue/dashboard source must follow the same pattern. [Source: _bmad-output/implementation-artifacts/5-4-project-inventory-detail-views.md#Senior-Developer-Review-AI]
- Recent git history is Story 5.7 at `079a2f9`, Story 5.6 at `1df34c4`, Story 5.5 at `8e19197`, Story 5.4 at `cc7e96f`. Current working tree has an unrelated modified `_bmad-output/story-automator/orchestration-4-20260530-070036.md`; do not revert it. [Source: git log --oneline -5] [Source: git status --short]

### Project Structure Notes

- Inventory/dashboard page update likely starts in `src/Hexalith.Projects.UI/Components/Pages/Home.razor` and `Home.razor.css`.
- Detail drill-in preservation likely touches `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` only if new tab targets or links are needed.
- Optional reusable components: `src/Hexalith.Projects.UI/Components/Shared/ProjectWarningsQueue.razor`, `ProjectOperationalDashboard.razor`, and scoped `.razor.css` files.
- Source/model layer: `src/Hexalith.Projects.UI/Diagnostics/` for `IProjectWarningsDashboardSource`, load result, warning mapper, and aggregation helper.
- Descriptor contracts: `src/Hexalith.Projects.Contracts/Ui/`.
- Existing models to reuse: `src/Hexalith.Projects.Contracts/Models/ProjectOperatorDiagnostic.cs`, `ProjectOperatorReferenceSummary.cs`, `ProjectOperatorAuditTimelineItem.cs`, and UI descriptors under `Contracts/Ui/`.
- Existing rendering primitives: `src/Hexalith.Projects.UI/Rendering/ProjectDiagnosticRendering.cs`, `ProjectConsoleFeedback.cs`, `ProjectEmptyState.cs`, and `ProjectVocabularyRendering.cs`.
- Tests: UI component/source tests under `tests/Hexalith.Projects.UI.Tests/`, descriptor tests under `tests/Hexalith.Projects.Contracts.Tests/Ui/`, leakage tests under `tests/Hexalith.Projects.Tests/Leakage/`, and E2E selector/spec updates under `tests/e2e/`.
- Do not hand-edit generated `.g.cs` files. Do not read or modify BMAD folders inside submodules. Do not initialize nested submodules. Do not create submodule pointer churn.

### Hard Stops

- Stop before coding if implementation appears to require a duplicate public `/warnings`, `/dashboard`, `/operator-inventory`, or sibling-context fan-out endpoint only to reshape data already available through `ListProjects`, `GetProjectOperatorDiagnostics`, or additive metadata-only fields on existing contracts.
- Stop before coding if warning aggregation requires raw EventStore events, raw projection rows, idempotency keys, command bodies, proposal bodies, raw setup prompts, sibling payloads, file paths/content, memory payloads, transcripts, secrets, raw tokens, raw ProblemDetails bodies, candidate score/rank, rejected candidate ids, or sibling denial details.
- Stop before coding if a Web-only warning enum, reason-code enum, severity table, or feedback taxonomy appears necessary. Use existing shared vocabulary and descriptors.
- Stop before coding if queue safe actions become archive/restore/relink/unlink/reevaluate commands, dry-runs, persisted maintenance state, audit events, Dapr state entries, or SignalR source-of-truth. Story 5.9 owns maintenance mutations.
- Stop before coding if broad MCP/CLI command/resource frameworks enter scope. Story 5.10 owns broad parity adapter wiring; this story owns descriptors and exact handoff documentation.
- Stop before coding if generated FrontComposer files or generated client `.g.cs` files appear to need hand edits.
- Stop before coding if package upgrades/downgrades, analyzer suppressions, nullable disable, warning downgrade, central package management bypass, submodule pointer changes, nested submodule init, or BMAD reads inside submodules are required.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-5.8-Warnings-queue--operational-dashboard]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Custom-Components]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Additional-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Responsive-Design--Accessibility]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/implementation-artifacts/5-4-project-inventory-detail-views.md]
- [Source: _bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md]
- [Source: _bmad-output/implementation-artifacts/5-6-resolution-trace-workbench.md]
- [Source: _bmad-output/implementation-artifacts/5-7-audit-timeline-view-safe-diagnostic-export.md]
- [Source: docs/parity-matrix.md#Story-5.4-Inventory--Detail-Contract]
- [Source: docs/parity-matrix.md#Story-5.5-Reference-Health-Contract]
- [Source: docs/parity-matrix.md#Story-5.7-Audit-Timeline--Safe-Export-Contract]
- [Source: docs/projection-catalog.md#ProjectOperatorDiagnosticShellProjection]
- [Source: docs/projection-catalog.md#ProjectInventoryRowProjection]
- [Source: docs/projection-catalog.md#ProjectReferenceHealthRowProjection]
- [Source: docs/payload-taxonomy.md]
- [Source: src/Hexalith.Projects.UI/Components/Pages/Home.razor]
- [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectInventorySource.cs]
- [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectDetailSource.cs]
- [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs]
- [Source: src/Hexalith.Projects.UI/Rendering/ProjectDiagnosticRendering.cs]
- [Source: src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs]
- [Source: tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs]
- [Source: tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs]
- [Source: tests/e2e/support/page-objects/project-detail.page.ts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-30: `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal` passed with 0 warnings / 0 errors.
- 2026-05-30: VSTest `dotnet test` focused lanes were attempted but blocked by sandbox local socket permission (`System.Net.Sockets.SocketException (13): Permission denied`). Re-ran focused lanes with the xUnit v3 in-process runner instead.
- 2026-05-30: Focused xUnit in-process lanes passed: `ProjectVocabularyTests` 37/37, `ProjectWarningsDashboardSourceTests` + `ProjectInventoryPageTests` 11/11, `NoPayloadLeakageTests` 58/58.
- 2026-05-30: Broader xUnit in-process regression passed for Contracts 158/158, Client 52/52, Projects.Tests 577/577, UI 105/105, Integration 15/15. Server.Tests host-starting cases remain blocked by sandbox socket permission (220 Kestrel bind failures), matching the environment constraint rather than a story regression.
- 2026-05-30: `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` passed with no FrontComposer warnings.
- 2026-05-30: `git diff --check` passed.
- 2026-05-30: `npm --prefix tests/e2e run typecheck` was attempted but blocked because `tsc` is not installed in `tests/e2e/node_modules` in this workspace.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Create-story validation applied against checklist: story includes source-derived ACs, current file locations, existing generated-client/operator diagnostic constraints, warning-source strategy, FrontComposer/Fluent UI constraints, previous-story learnings, testing requirements, parity documentation handoff, and hard stops.
- Input discovery loaded: project context (`_bmad-output/project-context.md`), architecture (`_bmad-output/planning-artifacts/architecture.md`), epics (`_bmad-output/planning-artifacts/epics.md`), UX design (`_bmad-output/planning-artifacts/ux-design-specification.md`), previous story 5.7, relevant code/docs/tests, package pins, and recent git history.
- Fluent UI Blazor MCP version/component lookup was attempted for latest technical confirmation, but the tool calls were cancelled by the environment; local pinned package versions and existing component usage are treated as authoritative.
- Implemented Story 5.8 metadata-only FrontComposer descriptors for the warning queue and operational dashboard with shared `ReferenceState`, `ProjectReasonCode`, and `ProjectLifecycle` property types.
- Added `ProjectWarningsDashboardSource` over tenant-scoped `ListProjectsAsync(...)` rows and bounded per-visible-row `GetProjectOperatorDiagnosticsAsync(..., auditLimit: 25, eventually_consistent, ct)` enrichment. Partial diagnostic failures now produce explicit safe unavailable queue items and dashboard counts.
- Added `/projects` dashboard band and `/projects/warnings` route with queue filters for state, reason, reference type, lifecycle, and observed/updated timestamp metadata; safe actions are read-only drill-ins/labels and mutation actions remain labeled as Story 5.9 scope.
- Added `ProjectEmptyState.NoWarnings()`, parity/projection catalog handoffs for `projects.warningQueue` and `projects.operationalDashboard`, bUnit/source/contract/leakage coverage, and Playwright fixme selectors/specs for triage, filters, a11y/axe, and payload leakage.
- No backend endpoint, generated client, package, mutation flow, MCP/CLI adapter, payload taxonomy category, or submodule pointer change was introduced.

### File List

- `_bmad-output/implementation-artifacts/5-8-warnings-queue-operational-dashboard.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/parity-matrix.md`
- `docs/projection-catalog.md`
- `src/Hexalith.Projects.Contracts/Ui/ProjectOperationalDashboardProjection.cs`
- `src/Hexalith.Projects.Contracts/Ui/ProjectWarningQueueItemProjection.cs`
- `src/Hexalith.Projects.UI/Components/Pages/Home.razor`
- `src/Hexalith.Projects.UI/Components/Pages/Home.razor.css`
- `src/Hexalith.Projects.UI/Diagnostics/IProjectWarningsDashboardSource.cs`
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardLoadResult.cs`
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs`
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs`
- `src/Hexalith.Projects.UI/Program.cs`
- `src/Hexalith.Projects.UI/Rendering/ProjectEmptyState.cs`
- `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs`
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`
- `tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs`
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs`
- `tests/e2e/specs/projects-inventory-detail.spec.ts`
- `tests/e2e/specs/projects-warnings-dashboard.spec.ts`
- `tests/e2e/support/page-objects/project-detail.page.ts`

## Senior Developer Review (AI)

**Reviewer:** Jerome (automated adversarial review) on 2026-05-30
**Outcome:** Changes Requested → fixed automatically → Approved

### Verification of dev claims (all confirmed)

- `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal` → **0 warnings / 0 errors**.
- Focused lanes re-run via VSTest (sandbox disabled, serial): `ProjectWarningsDashboardSourceTests` + `ProjectInventoryPageTests` **13/13** (was 11; +2 added during review), `ProjectVocabularyTests` **37/37**, `NoPayloadLeakageTests` **58/58**.
- `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` → **PASSED, no warnings**. `git diff --check` → **clean**.
- File List cross-checked against `git status`: every changed source/test/doc file is listed. (The only uncommitted file outside the list is the BMAD `tests/test-summary.md` artifact, which is out of review scope.)
- All 12 acceptance criteria are implemented; no task marked `[x]` was found incomplete.

### Findings and fixes applied

- **[MEDIUM][AC4] Lifecycle dashboard tiles did nothing.** `Home.razor` `ApplyDashboardFilter` set `_selectedLifecycle` for the Active/Archived tiles but never reloaded, and lifecycle is a server-side query parameter with no client-side fallback filter — so those tiles only changed the dropdown's displayed value, not the data. Fixed: `ApplyDashboardFilterAsync` now re-queries via `LoadAsync()` when a lifecycle tile is clicked. [`src/Hexalith.Projects.UI/Components/Pages/Home.razor`]
- **[MEDIUM][AC4/AC5] Grouped tile counts disagreed with their drill-in.** "Denied/unavailable" counts `Unauthorized + Unavailable` but filtered to `Unavailable` only; "Ambiguous/fail-closed" counts `Ambiguous + TenantMismatch` but filtered to `Ambiguous` only — hiding counted rows with no way to reach them. Fixed: the warning-state filter is now a state **set** (`_warningStateFilter`); grouped tiles select their full group so the queue shows exactly what the tile counts. The single-select dropdown is cleared when a multi-state group is active. [`src/Hexalith.Projects.UI/Components/Pages/Home.razor`]
- **[LOW][AC2/AC8] Warning predicate surfaced an undeclared state.** `ProjectWarningsDashboardMapper.BuildQueueItems` treated every non-`Included` reference as a warning, including `Pending`, even though the `ProjectWarningQueueItemProjection` `WhenState` descriptor and AC1's enumerated conditions omit `Pending`. Fixed: the predicate now excludes both `Included` and `Pending`, so queue behavior matches the declared ActionQueue contract used by the inspect/parity gate. [`src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs`]
- **[LOW][AC12] Inventory test did not validate the real placeholder.** `ProjectInventoryPageTests` hard-coded `"Unavailable on list row"` while production uses `ProjectInventoryRowProjection.WarningSummaryUnavailable` (`"Not available on list row"`), so the assertion would not catch a regression in the placeholder. Fixed: the row helper and assertion now reference the constant, and two new bUnit tests lock in the grouped-tile and lifecycle-tile drill-in fixes. [`tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs`]

### Residual notes (non-blocking, no action required)

- The "Warning projects", "Total visible", and "Diagnostic unavailable" tiles remain intentionally broad drill-ins (project-distinct count / show-all / closest `Unavailable` bucket); these are reasonable navigations, not the misleading count-vs-rows contradictions that were fixed.

## Change Log

| Date | Change |
| --- | --- |
| 2026-05-30 | Created Story 5.8 context package for Warnings Queue & Operational Dashboard; status set to ready-for-dev. |
| 2026-05-30 | Implemented warnings queue and operational dashboard; added metadata-only descriptors, bounded UI source/enrichment, Web dashboard/queue/filter UI, docs, tests, and validation evidence; status set to review. |
| 2026-05-30 | Automated senior review: fixed lifecycle/grouped dashboard tile drill-ins, aligned warning predicate with the declared ActionQueue `WhenState`, corrected the inventory test placeholder, and added two bUnit tile tests. Re-verified build (0/0), focused tests (13/37/58), inspect gate, and `git diff --check`. Status set to done. |
