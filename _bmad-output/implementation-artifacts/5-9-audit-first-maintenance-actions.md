---
story_id: 5.9
story_key: 5-9-audit-first-maintenance-actions
epic: "Epic 5: Operational Console & Audit (CLI / MCP / Web)"
created: 2026-05-30
source_story_status: backlog
baseline_commit: 5a678f6
---

# Story 5.9: Audit-First Maintenance Actions

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an authorized operator,
I want to restore, relink, unlink, and re-evaluate via maintenance actions that preview impact and produce audit evidence,
so that state-changing operations are explicit, scoped, confirmed, and auditable.

## Acceptance Criteria

1. Given an authorized operator opens `/projects/{ProjectId}` Actions or a Story 5.8 warning row action, when a maintenance action is selected, then a Maintenance Action Panel renders action name, server-derived tenant scope, target identifiers, current state, proposed state, warnings, dry-run/preview result, expected audit event, payload-exclusion guarantee, and confirmation control with stable selectors.
2. Given panel state requirements, when an action progresses, then it uses explicit panel states `Preview`, `DryRunRequired`, `DryRunPassed`, `DryRunBlocked`, `ConfirmationRequired`, `Executing`, `Succeeded`, and `Failed`; focus moves to new risk/result content and no state-changing control is enabled before required preview/dry-run and confirmation evidence exists.
3. Given command lifecycle requirements, when a mutating action submits, then Web uses the shared lifecycle `Idle -> Submitting -> Acknowledged(202) -> Syncing -> Confirmed/Rejected`, keeps the 202 accepted response separate from final projection/audit confirmation, and renders domain rejections as safe `ProblemDetailsPayload` outcomes rather than exceptions or raw problem bodies.
4. Given archive already exists from Epic 1, when archive is exposed in the panel, then it reuses `POST /api/v1/projects/{projectId}/archive`, requires `Idempotency-Key`, preserves `ArchiveProject` semantics, confirms through detail/operator diagnostic/audit reload, and does not create a second archive command or alternate lifecycle endpoint.
5. Given restore is requested for an archived project, when the existing aggregate/API lacks a restore command, then the implementation adds a first-class metadata-only `RestoreProject` command/event/rejection path through Contracts, aggregate `Handle/Apply`, projections, OpenAPI spine, generated client, server endpoint, command submitter, audit projection mapping, docs, and tests. Do not fake restore by setup update, direct projection write, direct Dapr state edit, or client-side lifecycle override.
6. Given relink is requested, when the target is the single Project Folder, a File Reference, or a Memory Reference, then the panel uses existing `SetProjectFolder`, `LinkFileReference`, and `LinkMemory` semantics with preflight ACL validation. Folder replacement requires explicit replacement confirmation; file relink must validate Folders metadata using transient workspace/path inputs only; memory relink must validate the Memories Case. No sibling payload, file content, memory content, path, or denial detail is persisted or rendered.
7. Given unlink is requested, when the target is a conversation, file reference, or memory reference, then the implementation uses existing unlink routes and semantics: conversation unlink delegates to Conversations via the assignment ACL, file/memory unlink removes only the Projects association, missing file/memory links are idempotent no-ops, and the underlying conversation, file, folder, memory Case, and MemoryUnit data are never deleted or mutated.
8. Given re-evaluate is requested, when the operator previews or confirms it, then re-evaluation is implemented as safe diagnostic recomputation/reload over existing `RefreshProjectContext`, resolution trace, and operator diagnostic query surfaces unless a justified additive metadata-only command contract is required. It must not persist resolution traces, candidate scores/ranks, rejected ids, or new source-of-truth state.
9. Given form validation and dry-run requirements, when inputs are malformed, unauthorized, unavailable, stale, conflict with current state, or would violate lifecycle rules, then field-specific safe errors are returned before any state change, no sibling ACL mutation runs after a Project-level denial, and the panel shows blockers with shared reason/state vocabulary.
10. Given audit evidence requirements, when an action succeeds, then a metadata-only audit row appears through the existing operator diagnostic audit timeline with action, actor/source, tenant/project identifiers, affected reference identifiers, timestamp, correlation ID, task ID, result/state delta, and audit event ID. Public audit rows must not expose idempotency keys, command bodies, raw problem bodies, proposal bodies, candidate score/rank, rejected candidate ids, sibling denial details, or payload fields.
11. Given tenant/lifecycle rules, when an action is not allowed for the caller, tenant, project lifecycle, reference state, or upstream ACL evidence, then the system fails closed with safe reason codes and no state change. Unauthorized, cross-tenant, hidden, or malformed identifiers remain indistinguishable at the boundary where the existing API pattern requires safe-denial 404.
12. Given FrontComposer and cross-surface parity constraints, when maintenance descriptors are added, then the same action names, panel states, lifecycle states, input field names, safe result fields, and rejection vocabulary are represented in metadata-only `[Command]` / projection wrapper contracts for Web now and MCP handoff later. Broad MCP/CLI adapter wiring remains Story 5.10 scope unless existing generation exposes descriptors without building a new adapter framework.
13. Given payload-safety and regression requirements, when the story is implemented, then no payload-bearing data appears in UI markup, command payload logs, audit rows, docs, tests, safe exports, or future MCP/CLI descriptors; `NoPayloadLeakage` coverage is extended over any new commands/events/DTOs/descriptors/rendered fixtures.
14. Given testing and quality gates, when implementation completes, then contract, aggregate, projection, server, generated-client, UI source, bUnit, leakage, and E2E selector scaffolding tests cover success/rejection/dry-run/confirmation/lifecycle/audit evidence for archive, restore, relink, unlink, and re-evaluate; build, focused tests, FrontComposer inspect gate, OpenAPI/generated-client fingerprint gates, and `git diff --check` are documented.

## Tasks / Subtasks

- [x] Define maintenance descriptors and shared UI state models (AC: 1, 2, 3, 12, 13)
  - [x] Add metadata-only FrontComposer contracts under `src/Hexalith.Projects.Contracts/Ui/`, for example `ProjectMaintenanceActionProjection`, `ProjectMaintenanceActionResultProjection`, or command descriptors if the current FrontComposer attributes support them.
  - [x] Model fields: `action`, `panelState`, `commandLifecycleState`, `projectId`, server-derived tenant scope label, `referenceKind`, `referenceId`, current state, proposed state, warnings, dry-run status, expected audit operation, confirmation requirement, safe feedback code, correlation/task/audit IDs, and contract version.
  - [x] Use existing shared enums (`ProjectLifecycle`, `ReferenceState`, `ProjectReasonCode`, `ProjectContextInclusionDiagnostic`) and descriptor labels; do not add Web-only action/lifecycle/reason enums unless they are shared contracts with tests and parity docs.
  - [x] Add stable selectors: `maintenance-action-panel`, `maintenance-action-select`, `maintenance-action-state`, `maintenance-action-current-state`, `maintenance-action-proposed-state`, `maintenance-action-warning`, `maintenance-action-dry-run`, `maintenance-action-confirm`, `maintenance-action-submit`, `maintenance-action-feedback`, `maintenance-action-audit-event`, and per-action selectors such as `maintenance-action-archive`, `maintenance-action-restore`, `maintenance-action-relink`, `maintenance-action-unlink`, `maintenance-action-reevaluate`.

- [x] Implement or expose safe preview/dry-run behavior (AC: 1, 2, 8, 9, 11)
  - [x] Prefer UI/server preview over new persisted state: load current detail via `GetProjectAsync`, bounded diagnostics via `GetProjectOperatorDiagnosticsAsync`, reference health rows, and applicable ACL validation evidence.
  - [x] For archive, preview current lifecycle `Active -> Archived` and expected `ProjectArchived` audit operation.
  - [x] For restore, preview `Archived -> Active` only after adding a real restore command path; until the command exists, the panel must show `DryRunBlocked` with safe reason `unsupported_operation`, not fake success.
  - [x] For relink, validate the target type and show replacement/link impact: folder replacement, file link/relink, or memory link/relink. File workspace/path inputs are transient validation inputs only and must not be stored, exported, or logged.
  - [x] For unlink, show that the Project association is removed only; underlying sibling resources are untouched.
  - [x] For re-evaluate, run/reload `RefreshProjectContext` and/or existing resolution trace/operator diagnostic queries; present recomputed diagnostics as preview evidence unless an additive command contract is explicitly justified.

- [x] Add the restore write path if needed (AC: 5, 10, 11, 13, 14)
  - [x] Add `RestoreProject` command, `ProjectRestored` event, and `ProjectRestoreRejected` event in `src/Hexalith.Projects.Contracts/Commands/` and `Events/`.
  - [x] Add validation/fingerprint support to `ProjectCommandValidator`, result codes to `ProjectResultCode`, pure aggregate handler in `ProjectAggregate`, and state transition in `ProjectStateApply`.
  - [x] Update `ProjectListProjection`, `ProjectDetailProjection`, `ProjectReferenceIndexProjection` no-op/tolerance as needed, `ProjectAuditTimelineProjection`, event catalog, projection catalog, and schema-evolution/no-payload tests.
  - [x] Add `IProjectCommandSubmitter.SubmitRestoreProjectAsync`, `EventStoreProjectCommandSubmitter` payload, `ProjectsServerModule` command type, server endpoint, OpenAPI spine operation such as `POST /api/v1/projects/{projectId}/restore`, generated client regeneration, and fingerprint/compatibility evidence.
  - [x] Restore must not relink references, include archived projects in automatic resolution, or silently undo unrelated archive-era changes.

- [x] Reuse existing mutation endpoints for archive/relink/unlink (AC: 4, 6, 7, 9, 10, 11)
  - [x] Archive uses existing `ArchiveProjectAsync` and `SubmitArchiveProjectAsync`; do not duplicate lifecycle mutation paths.
  - [x] Folder relink uses existing `SetProjectFolderAsync`; preserve `ReplacementConfirmed` validation and Folders ACL preflight.
  - [x] File relink uses existing `LinkFileReferenceAsync` for link/replacement intent and `UnlinkFileReferenceAsync` only for explicit remove; do not read or persist file contents or paths.
  - [x] Memory relink uses existing `LinkMemoryAsync` and `UnlinkMemoryAsync`; validate Memories Case evidence and never touch MemoryUnit content.
  - [x] Conversation unlink uses existing `UnlinkProjectConversationAsync`; relink/move conversation remains Conversations-owned assignment ACL behavior, not local Projects membership.
  - [x] Ensure Project authorization gates run before body parsing or sibling ACL calls where current endpoint patterns require it.

- [x] Build the Web Maintenance Action Panel (AC: 1, 2, 3, 4, 6, 7, 8, 9, 10)
  - [x] Replace the current Actions tab placeholder in `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` with a panel or extract `ProjectMaintenanceActionPanel.razor` under `Components/Shared/`.
  - [x] Wire Story 5.8 "Handled by Story 5.9" affordances to open the project detail Actions section or preserve safe navigation if direct tab routing is deferred.
  - [x] Use existing `ProjectDiagnosticHeader`, `ProjectFeedbackView`, `ProjectEmptyStateView`, `ProjectStatusBadge`, `ProjectVocabularyDescriptors`, and operational CSS patterns.
  - [x] Distinguish loading states for metadata retrieval, ACL validation, dry-run, command submission, syncing, and audit confirmation.
  - [x] After 202, poll/reload bounded detail/operator diagnostics until the expected state/audit evidence is observed or a safe timeout/unavailable feedback state is reached. Do not assume read-after-write.
  - [x] Use buttons with clear action labels, visible focus, non-color-only warnings, and confirmation text; state-changing actions must not be casual primary actions before confirmation.

- [x] Preserve and update documentation/parity handoffs (AC: 10, 12, 13)
  - [x] Update `docs/parity-matrix.md` with maintenance action field names, selectors, Web behavior, command lifecycle, safe result schema, and Story 5.10 MCP/CLI handoff names.
  - [x] Update `docs/event-catalog.md` and `docs/projection-catalog.md` for any restore command/event/projection mapping or new UI descriptors.
  - [x] Update `docs/payload-taxonomy.md` and `PayloadClassification.SafeFields` only if a genuinely new safe category is required. Prefer existing `OpaqueId`, `ReferenceKind`, `Timestamp`, `LifecycleState`, `InclusionState`, `ResolutionState`, `ReasonCode`, `CorrelationId`, `CausationId`, `AuditId`, `UiFeedbackCode`, and `UiProjectionDescriptor`.
  - [x] Document that idempotency keys remain internal command evidence and public audit/operator/UI output must not expose them.

- [x] Add focused tests and quality gates (AC: all)
  - [x] Contracts tests for descriptor metadata, command schema versions, field names, enum/property types, no forbidden public fields, and OpenAPI compatibility/fingerprint updates.
  - [x] Aggregate/projection tests for restore success/rejection/idempotency, archive reuse, folder/file/memory link/unlink/relink invariants, audit timeline mapping, projection rebuild determinism, and no unknown event no-op.
  - [x] Server tests for mutation envelope validation, authorization-before-body/sibling-ACL behavior, 400/404/409/503 mappings, idempotency conflict/replay, field-specific validation, no raw ProblemDetails echo, and generated-client call shape.
  - [x] UI source/bUnit tests for panel states, dry-run blockers, confirmation gating, command lifecycle transitions, 202 syncing, final audit evidence rendering, safe feedback, no hover-only critical actions, stable selectors, and grouped warning-row handoff.
  - [x] Extend `NoPayloadLeakageTests` over new commands/events/rejections/descriptors/result DTOs/rendered markup and any safe maintenance export/handoff fixture.
  - [x] Add/update Playwright `test.fixme` specs and page objects for maintenance panel keyboard flow, axe/a11y, dry-run blocked/success, confirmation, audit evidence, no payload leakage, and mobile/tablet visibility.
  - [x] Run and record:
    - [x] `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal`
    - [x] focused `dotnet test` or in-process xUnit lanes for Contracts, Projects, Server, UI, Client, and leakage tests touched by this story
    - [x] OpenAPI/generated-client fingerprint/compatibility gates if the spine changes
    - [x] `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1`
    - [x] `git diff --check`

## Dev Notes

### Current State

- Epic 5 owns the operational console and audit-first maintenance actions. Story 5.9 introduces the first state-changing maintenance surface after Stories 5.3-5.8 deliberately stayed read-only or export-only. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.9-Audit-first-maintenance-actions]
- Existing Web detail route `/projects/{ProjectId}` already has sections `metadata`, `setup`, `references`, `resolution`, `audit`, and `actions`, but the Actions section currently renders only "Read-only inspector. Maintenance mutations are handled by Story 5.9." [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- Story 5.8 warnings queue intentionally labels mutating actions as "Handled by Story 5.9" and links to read-only detail workbenches. This story is the handoff point for those labels, not a reason to mutate directly from the queue without preview/confirmation. [Source: _bmad-output/implementation-artifacts/5-8-warnings-queue-operational-dashboard.md]
- Existing public mutation routes already cover archive, conversation link/move/unlink, project folder set/replace, file link/unlink, memory link/unlink, setup update, and resolution confirmation. Reuse these for archive/relink/unlink where semantics match. [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml] [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- Restore is not present in current commands/events/OpenAPI/aggregate state handling. If "restore" is implemented as a real state change, it requires a proper command/event/projection/audit path. [Source: src/Hexalith.Projects.Contracts/Commands/ArchiveProject.cs] [Source: src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs] [Source: docs/event-catalog.md#ProjectArchived]
- `RefreshProjectContext` is a read-only GET that rechecks current Project context evidence. File-reference recheck by opaque `(folderId, fileReferenceId)` remains deferred because the stable Folders route is unavailable without transient workspace/path inputs. Re-evaluate must preserve that boundary. [Source: src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs]

### Existing Mutation and Query Contracts

- Mutations require `Idempotency-Key`, use safe canonical `X-Correlation-Id` / `X-Hexalith-Task-Id`, return command-async `202 AcceptedCommand`, map idempotency conflicts to `409`, validation to `400`, retryable unavailable evidence to `503`, and safe denial to `404`. [Source: _bmad-output/planning-artifacts/architecture.md#API--Communication-Patterns] [Source: src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs]
- `ArchiveProjectAsync` authorizes, validates `ArchiveProjectHttpRequest` (`requestSchemaVersion = v1`, `archiveIntent = archive`), submits `ArchiveProject`, and confirms later via projections/audit. Preserve this pattern. [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- `SetProjectFolderAsync` requires active project authorization, `operation = set`, `folderId`, `folderMetadata`, and `replacementConfirmed` for replacing a different existing folder, then validates with `IProjectFolderDirectory` before submitting `SetProjectFolder`. [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- `LinkFileReferenceAsync` validates transient `workspaceId` and workspace-relative `filePath` for Folders ACL only, then submits a metadata-only `LinkFileReference` command. Those path/workspace values must not enter Project state, events, audit public rows, UI exports, or logs. [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs] [Source: src/Hexalith.Projects.Contracts/Commands/LinkFileReference.cs]
- `UnlinkFileReferenceAsync` and `UnlinkMemoryAsync` make no sibling delete/mutation calls. They remove only Projects associations and missing references are safe idempotent no-ops at the aggregate level. [Source: src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.References.cs] [Source: src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.Memories.cs]
- `ConfirmProjectResolutionAsync` is a useful command lifecycle model: it validates explicit confirmation, checks target/source authorization, performs the Conversations assignment ACL, submits the Projects command, and returns mutation result safely. Reuse the lifecycle pattern, but do not conflate reevaluate with resolution confirmation. [Source: src/Hexalith.Projects.Server/Queries/ConfirmProjectResolutionEndpoint.cs]
- Public operator audit rows come through `GetProjectOperatorDiagnosticsAsync(projectId, auditLimit, correlationId, eventually_consistent, ct)`. `ProjectOperatorDiagnostic` intentionally omits idempotency keys even though the underlying persisted audit projection stores them internally. [Source: src/Hexalith.Projects.Contracts/Models/ProjectOperatorDiagnostic.cs] [Source: docs/projection-catalog.md#ProjectAuditTimelineProjection]

### Maintenance Action Semantics

- Archive: current state `Active`, proposed state `Archived`, expected operation `project.archived`, no sibling mutations, references remain auditable, archived projects are excluded from automatic resolution unless explicitly included. [Source: docs/event-catalog.md#ProjectArchived] [Source: docs/resolution-scoring-heuristic.md]
- Restore: current state `Archived`, proposed state `Active`, expected operation should be a new explicit restore audit operation if implemented. Restoration must not silently relink references, include unsafe context, or bypass tenant/access checks. [Source: _bmad-output/planning-artifacts/epics.md#Story-5.9-Audit-first-maintenance-actions]
- Folder relink: replaces the single Project Folder via `SetProjectFolder`; existing folder replacement requires explicit confirmation and Folders ACL evidence. [Source: src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs]
- File relink: validates Folders metadata and links/relinks the opaque file reference. Unlink removes only the Project association and never touches the underlying file. [Source: src/Hexalith.Projects.Contracts/Commands/LinkFileReference.cs] [Source: src/Hexalith.Projects.Contracts/Commands/UnlinkFileReference.cs]
- Memory relink: validates Memories Case evidence and links/relinks the opaque memory Case reference. Unlink removes only the Project association and never touches the underlying Case or MemoryUnits. [Source: src/Hexalith.Projects.Contracts/Commands/LinkMemory.cs] [Source: src/Hexalith.Projects.Contracts/Commands/UnlinkMemory.cs]
- Conversation unlink/relink/move is Conversations-owned through the assignment ACL. Projects must not create local conversation membership state. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns] [Source: src/Hexalith.Projects.Server/Conversations/IProjectConversationAssignmentDirectory.cs]
- Re-evaluate should be diagnostic recomputation unless a product decision explicitly makes it a persisted maintenance command. It may call existing read-only refresh/trace/diagnostic queries and show blockers or updated evidence; it must not create a persisted trace or audit event unless an actual state-changing command occurs. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]

### UX Requirements

- UX-DR17 defines the Maintenance Action Panel: action name, tenant scope, target identifiers, current state, proposed state, warnings, dry-run result, expected audit event, confirmation control, and states `Preview`, `DryRunRequired`, `DryRunPassed`, `DryRunBlocked`, `ConfirmationRequired`, `Executing`, `Succeeded`, `Failed`. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- UX-DR21 requires the five-state command lifecycle `Idle -> Submitting -> Acknowledged (202) -> Syncing -> Confirmed/Rejected`; components must tolerate the 202-to-projection eventual consistency window. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- UX-DR24/25 require field-specific safe validation before state change, explicit confirmation showing tenant scope/target/current/proposed/warnings/expected audit event/dry-run result, and success feedback with metadata-only audit evidence. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Form-Patterns] [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Confirmation-Pattern]
- UX-DR26/27 require responsive and accessible maintenance flows. Mobile may support urgent inspection and lightweight maintenance only when the full confirmation content remains visible; complex maintenance should remain desktop/CLI-friendly. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Responsive-Design--Accessibility]
- Use restrained operational styling and existing Fluent/FrontComposer-compatible primitives. Do not create a decorative dashboard, landing page, custom UI framework, or nested card-heavy layout. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Component-Implementation-Strategy]

### FrontComposer / Fluent UI Guardrails

- Architecture maps operational surfaces from shared `[Projection]`/`[Command]` contracts to Web/MCP/CLI. The exact mutating MCP/CLI adapters belong to Story 5.10, but this story should create the metadata contracts/handoff fields that make parity possible. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- Reuse `ProjectDiagnosticHeader`, `ProjectStatusBadge`, `ProjectVocabularyDescriptors`, `ProjectConsoleFeedback`, `ProjectEmptyStateView`, `ProjectAuditTimelineSection`, and Story 5.8 dashboard/queue styles where applicable. [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor] [Source: src/Hexalith.Projects.UI/Components/Pages/Home.razor]
- Local package pins on 2026-05-30 are authoritative: .NET SDK `10.0.302`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, bUnit `2.7.2`, xUnit v3 `3.2.2`, Dapr `1.17.9`, Aspire `13.3.5`, NSwag `14.7.1`, and Verify.XunitV3 where already used. Do not upgrade/downgrade or inline package versions. [Source: global.json] [Source: Directory.Packages.props]
- Attempted Fluent UI Blazor MCP version check during story creation, but the tool call was cancelled by the environment. Treat pinned package versions and existing local component usage as authoritative.

### Previous Story Intelligence

- Story 5.8 implemented metadata-only warning queue/dashboard descriptors and safe row actions. It intentionally left mutating actions disabled/labeled for this story and added grouped tile fixes in review. Preserve count/filter semantics and wire actions through the panel rather than adding mutation buttons to queue rows directly. [Source: _bmad-output/implementation-artifacts/5-8-warnings-queue-operational-dashboard.md#Senior-Developer-Review-AI]
- Story 5.7 safe export forbids idempotency keys, command/proposal bodies, raw setup text, candidate score/rank, rejected candidate ids, raw ProblemDetails bodies, and sibling denial detail. Maintenance result/audit/export handoffs must keep the same boundary. [Source: _bmad-output/implementation-artifacts/5-7-audit-timeline-view-safe-diagnostic-export.md]
- Story 5.6 fixed the `ReferenceRedacted` classification boundary: policy/redacted exclusions are `Excluded`, not `FailedClosed`. Do not turn policy exclusions into failed maintenance state unless trust evidence is genuinely unverifiable. [Source: _bmad-output/implementation-artifacts/5-6-resolution-trace-workbench.md#Senior-Developer-Review-AI]
- Story 5.5 established reference-health rows and safe actions as read-only; this story may turn selected safe actions into maintenance flows only after preview/confirmation and existing ACL semantics are preserved. [Source: _bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md]
- Story 5.4 review fixed UI source transport/non-API exception handling so Blazor does not crash or echo raw messages. Any maintenance source/client wrapper must map API/transport/deserialization failures to safe feedback. [Source: _bmad-output/implementation-artifacts/5-4-project-inventory-detail-views.md#Senior-Developer-Review-AI]
- Recent git history: Story 5.8 at `5a678f6`, Story 5.7 at `079a2f9`, Story 5.6 at `1df34c4`, Story 5.5 at `8e19197`, Story 5.4 at `6f18018`. Current root working tree has an unrelated modified `_bmad-output/story-automator/orchestration-4-20260530-070036.md`; do not revert it. [Source: git log --oneline -5] [Source: git status --short]

### Project Structure Notes

- Primary Web update: `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` and `.razor.css`; optional component under `src/Hexalith.Projects.UI/Components/Shared/ProjectMaintenanceActionPanel.razor`.
- UI source/client wrapper likely belongs under `src/Hexalith.Projects.UI/Diagnostics/`, alongside `ProjectDetailSource`, `ProjectAuditTimelineSource`, `ProjectWarningsDashboardSource`, and safe failure mappers.
- Maintenance descriptor contracts belong under `src/Hexalith.Projects.Contracts/Ui/`.
- Restore command path, if added, touches `src/Hexalith.Projects.Contracts/Commands/`, `Events/`, `openapi/hexalith.projects.v1.yaml`, `src/Hexalith.Projects.Client/Generated/` via regeneration only, `src/Hexalith.Projects/Aggregates/Project/`, projections, `src/Hexalith.Projects.Server/`, docs, and tests.
- Existing mutation endpoints and command submitter are in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`, `EventStoreProjectCommandSubmitter.cs`, and `IProjectCommandSubmitter.cs`.
- Tests: descriptor tests under `tests/Hexalith.Projects.Contracts.Tests/Ui/`, aggregate/projection tests under `tests/Hexalith.Projects.Tests/`, server endpoint tests under `tests/Hexalith.Projects.Server.Tests/`, UI/bUnit/source tests under `tests/Hexalith.Projects.UI.Tests/`, client/fingerprint tests under `tests/Hexalith.Projects.Client.Tests/` if generated client changes, and E2E selectors/specs under `tests/e2e/`.
- Do not hand-edit generated `.g.cs` files. Do not read or modify BMAD folders inside submodules. Do not initialize nested submodules. Do not create submodule pointer churn.

### Hard Stops

- Stop before coding if restore is implemented by mutating read models, direct Dapr state, Web-only lifecycle override, setup update side effect, or an archive endpoint flag instead of a first-class command/event/projection/audit path.
- Stop before coding if relink/unlink requires storing or rendering workspace IDs, folder paths, file paths/content, memory payloads, transcripts, prompts, raw setup text, raw sibling denial details, raw ProblemDetails bodies, idempotency keys, command bodies, proposal bodies, candidate scores/ranks, rejected candidate IDs, secrets, or tokens.
- Stop before coding if a Project-level authorization denial allows body validation details or sibling ACL calls to run first.
- Stop before coding if re-evaluate becomes persisted trace/history/scoring state without an explicit additive contract and audit decision.
- Stop before coding if a parallel Web-only maintenance lifecycle enum, severity table, error taxonomy, or reason-code vocabulary appears necessary.
- Stop before coding if broad Story 5.10 MCP/CLI adapter frameworks, package upgrades/downgrades, analyzer suppressions, nullable disable, warnings downgrade, generated-file hand edits, submodule pointer changes, or nested submodule initialization are required.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-5.9-Audit-first-maintenance-actions]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Journey-3-Perform-Safe-Maintenance-Action]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Maintenance-Action-Panel]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Form-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Confirmation-Pattern]
- [Source: _bmad-output/planning-artifacts/architecture.md#API--Communication-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- [Source: _bmad-output/implementation-artifacts/5-4-project-inventory-detail-views.md]
- [Source: _bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md]
- [Source: _bmad-output/implementation-artifacts/5-6-resolution-trace-workbench.md]
- [Source: _bmad-output/implementation-artifacts/5-7-audit-timeline-view-safe-diagnostic-export.md]
- [Source: _bmad-output/implementation-artifacts/5-8-warnings-queue-operational-dashboard.md]
- [Source: docs/parity-matrix.md#Story-5.8-Warnings-Queue--Operational-Dashboard-Contract]
- [Source: docs/event-catalog.md#ProjectArchived]
- [Source: docs/projection-catalog.md#ProjectAuditTimelineProjection]
- [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml]
- [Source: src/Hexalith.Projects.Contracts/Commands/ArchiveProject.cs]
- [Source: src/Hexalith.Projects.Contracts/Commands/SetProjectFolder.cs]
- [Source: src/Hexalith.Projects.Contracts/Commands/LinkFileReference.cs]
- [Source: src/Hexalith.Projects.Contracts/Commands/UnlinkFileReference.cs]
- [Source: src/Hexalith.Projects.Contracts/Commands/LinkMemory.cs]
- [Source: src/Hexalith.Projects.Contracts/Commands/UnlinkMemory.cs]
- [Source: src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs]
- [Source: src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.References.cs]
- [Source: src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.Memories.cs]
- [Source: src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs]
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs]
- [Source: src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs]
- [Source: src/Hexalith.Projects.Server/Queries/ConfirmProjectResolutionEndpoint.cs]
- [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- [Source: src/Hexalith.Projects.UI/Components/Pages/Home.razor]
- [Source: tests/e2e/support/page-objects/project-detail.page.ts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-30: `dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj --no-restore /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 /nodeReuse:false -v:minimal` passed after OpenAPI/client regeneration.
- 2026-05-30: `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --no-restore /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 /nodeReuse:false -v:minimal` passed.
- 2026-05-30: `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 /nodeReuse:false -v:minimal` passed.
- 2026-05-30: xUnit v3 in-process focused lanes passed: Contracts `ProjectVocabularyTests` + `RejectionEventTaxonomyTests` 76/76; Projects aggregate/projection/leakage focused classes 91/91; Client `ClientGenerationTests` 37/37; UI `ProjectDetailPageTests` + `ProjectMaintenanceActionSourceTests` 20/20.
- 2026-05-30: `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` passed with 12 annotated contract files, 22 generated files, and 0 warnings.
- 2026-05-30: `git diff --check` passed.
- 2026-05-30: `dotnet test` via VSTest was attempted but aborted before test execution with `System.Net.Sockets.SocketException (13): Permission denied`; equivalent focused xUnit v3 in-process lanes passed where the tests do not require Kestrel binding.
- 2026-05-30: Focused server endpoint execution `PostProjectRestore_Authorized_Returns202AndSubmitsRestore` was attempted with the xUnit v3 in-process runner but Kestrel socket binding was blocked by the sandbox (`SocketException: Permission denied`); the Server.Tests project build passed.
- 2026-05-30: Required solution build `dotnet build Hexalith.Projects.slnx -warnaserror --no-restore /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 /nodeReuse:false -v:minimal` compiled product/test projects but failed on generator projects with `NU1900` because the sandbox cannot reach `api.nuget.org` for vulnerability data and warnings are errors.
- 2026-05-30: `pwsh tests/tools/run-openapi-fingerprint-gate.ps1` was attempted; the gate script is blocked by MSBuild named-pipe/socket permissions in this sandbox. The equivalent `ClientGenerationTests` in-process lane passed after regenerating and hashing the checked-in client/helper artifacts.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Create-story validation applied against checklist: story includes source-derived ACs, existing mutation/query reuse, restore gap handling, dry-run/confirmation/lifecycle requirements, audit evidence boundaries, FrontComposer/Fluent UI constraints, previous-story learnings, testing requirements, parity documentation handoff, and hard stops.
- Input discovery loaded: project context (`_bmad-output/project-context.md`), sprint status, architecture, epics, UX design specification/directions, previous stories 5.4-5.8, current code/docs/tests, package pins, and recent git history.
- Fluent UI Blazor MCP version check was attempted during story creation, but the environment cancelled the tool call; local pinned package versions and existing component usage are treated as authoritative.
- Implemented a first-class metadata-only `RestoreProject` command/event/rejection path through Contracts, aggregate validation/handle/apply, projections, server endpoint/domain processor/submitter, OpenAPI, generated client, idempotency helpers, audit mapping, docs, and tests.
- Added shared FrontComposer maintenance descriptors and a Web Actions tab panel with stable selectors, explicit panel states, dry-run/confirmation gating, safe feedback, expected audit evidence, and payload-exclusion guarantee.
- Reused existing archive, folder/file/memory link, file/memory/conversation unlink, and `RefreshProjectContext` generated-client surfaces from the UI source; file relink requires transient validation inputs and is blocked in the visible panel when those inputs are absent.
- Extended docs, parity handoff, payload taxonomy, leakage coverage, aggregate/projection/client/server/UI tests, and E2E selector scaffolding for archive/restore/relink/unlink/reevaluate maintenance flows.
- No package version changes, recursive/nested submodule updates, broad MCP/CLI adapter work, direct Dapr/read-model restore shortcut, raw payload rendering, or idempotency-key exposure were introduced.

### File List

- `_bmad-output/implementation-artifacts/5-9-audit-first-maintenance-actions.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/event-catalog.md`
- `docs/parity-matrix.md`
- `docs/payload-taxonomy.md`
- `docs/projection-catalog.md`
- `src/Hexalith.Projects.Contracts/Commands/RestoreProject.cs`
- `src/Hexalith.Projects.Contracts/Events/ProjectRestored.cs`
- `src/Hexalith.Projects.Contracts/Events/ProjectRestoreRejected.cs`
- `src/Hexalith.Projects.Contracts/Ui/ProjectMaintenanceActionProjection.cs`
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidationResult.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectResultCode.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs`
- `src/Hexalith.Projects/Projections/ProjectAuditTimeline/ProjectAuditTimelineProjection.cs`
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs`
- `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs`
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs`
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`
- `src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs`
- `src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/ProjectsServerModule.cs`
- `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor`
- `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor.css`
- `src/Hexalith.Projects.UI/Components/Shared/ProjectMaintenanceActionPanel.razor`
- `src/Hexalith.Projects.UI/Diagnostics/IProjectMaintenanceActionSource.cs`
- `src/Hexalith.Projects.UI/Program.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.Projects.Contracts.Tests/Events/RejectionEventTaxonomyTests.cs`
- `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs`
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`
- `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectSetupArchiveAggregateTests.cs`
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`
- `tests/Hexalith.Projects.Tests/Projections/ProjectAuditTimelineProjectionTests.cs`
- `tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs`
- `tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs`
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectMaintenanceActionSourceTests.cs`
- `tests/e2e/specs/projects-maintenance.spec.ts`
- `tests/e2e/support/page-objects/project-detail.page.ts`

## Change Log

| Date | Change |
| --- | --- |
| 2026-05-30 | Created Story 5.9 context package for Audit-First Maintenance Actions; status set to ready-for-dev. |
| 2026-05-30 | Implemented audit-first maintenance actions, restore command path, Web panel, docs, tests, and validation evidence; status set to review. |
| 2026-05-30 | Senior Developer Review auto-fixed maintenance panel relink/confirmation gating, selector parity docs, E2E fixture lifecycle drift, and focus handling; status set to done. |

## Senior Developer Review (AI)

Reviewer: Codex on 2026-05-30

Outcome: Approved after auto-fixes. No CRITICAL issues remain.

Findings fixed:

- [HIGH] The Web panel could dry-run and submit `relink` without an explicit replacement target, reusing the first reference row as the mutation target. Fixed by adding explicit reference-kind/reference-id controls, blocking missing/same/current targets, blocking conversation relink as Conversations-owned, and preserving the file relink transient-validation blocker.
- [HIGH] The confirmation checkbox was available before dry-run evidence existed. Fixed by disabling confirmation until `DryRunPassed`, preserving `DryRunPassed -> ConfirmationRequired -> Executing` before submit can enable.
- [MEDIUM] New reference target selectors and lifecycle semantics were not represented in parity/E2E handoff docs. Fixed `docs/parity-matrix.md` and `tests/e2e/support/page-objects/project-detail.page.ts`.
- [MEDIUM] The no-app Playwright selector fixture encoded stale behavior (`ConfirmationRequired` immediately after dry-run and a synthetic re-evaluate audit event). Fixed fixture and expectations to match the real component: `DryRunPassed` first and `none (read-only recompute)` for re-evaluate audit.
- [LOW] Risk/result focus movement was implicit via live regions only. Added best-effort `FocusAsync` to dry-run and feedback regions after state/result changes.

Validation:

- `dotnet build Hexalith.Projects.slnx --no-restore -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 /nodeReuse:false -v:minimal` passed.
- `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 /nodeReuse:false -v:minimal` passed.
- xUnit v3 in-process focused UI lane passed: `ProjectDetailPageTests` + `ProjectMaintenanceActionSourceTests`, 27/27.
- `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` passed.
- `git diff --check` passed.
- `dotnet test` via VSTest remains blocked by sandbox socket permissions (`SocketException (13): Permission denied`) before test execution.
- `npm --prefix tests/e2e run typecheck` was not run because `tests/e2e/node_modules` is not installed in this workspace.
