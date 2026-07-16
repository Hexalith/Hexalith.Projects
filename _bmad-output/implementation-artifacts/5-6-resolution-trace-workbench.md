---
story_id: 5.6
story_key: 5-6-resolution-trace-workbench
epic: "Epic 5: Operational Console & Audit (CLI / MCP / Web)"
created: 2026-05-30
source_story_status: backlog
baseline_commit: 8e19197
---

# Story 5.6: Resolution Trace Workbench

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an administrator,
I want a resolution trace view that shows evaluated inputs, candidate projects, reason codes, inclusion/exclusion evidence, and the final outcome,
so that I can diagnose why resolution selected, rejected, or could not decide between candidates.

## Acceptance Criteria

1. Given a selected Project in `/projects/{ProjectId}`, when the Resolution tab renders, then the Story 5.4 placeholder is replaced by a read-only Resolution Trace Workbench that preserves `ProjectDiagnosticHeader`, the existing detail tabs, safe feedback rendering, keyboard navigation, and the `project-detail-section-resolution` selector.
2. Given safe resolution inputs, when an operator runs a trace, then the workbench supports the existing compute-on-demand query modes only: conversation id via `ResolveProjectFromConversationAsync` and folder/file attachment ids via `ResolveProjectFromAttachmentsAsync`, with an `includeArchived` toggle. It must not invent memory-backed resolution, proposal preview, confirmation, or persisted trace history.
3. Given a trace result, when the outcome renders, then it shows an input summary, result badge, observed timestamp, candidate list, reason-code badges, exclusion evidence, and an outcome panel for `Resolved`/`NoMatch`/`MultipleCandidates`/`Excluded`/`FailedClosed`. `Resolved` maps to `ResolutionResult.SingleCandidate`; `Excluded` and `FailedClosed` are derived from `ResolutionExclusion` evidence, not new wire enum members.
4. Given multiple or competing candidates, when the workbench renders, then it provides side-by-side candidate comparison with project id, optional safe display name, rank, score, and reason codes. Candidate `Score`/`Rank` may be displayed only in this computed trace context; they must not be persisted, copied into audit/reference-health rows, exported by Story 5.7, or stored as a new projection.
5. Given shared Projects vocabulary, when result, reason, and exclusion states render, then they use `ResolutionResult`, `ReferenceState`, `ProjectReasonCode`, `ProjectContextInclusionDiagnostic`, and `ProjectVocabularyDescriptors`; no Web-only status enum, confidence enum, severity table, or free-text diagnostic vocabulary is introduced.
6. Given the trace is a query surface, when the workbench calls generated clients, then it sends caller-generated correlation id, `X-Hexalith-Freshness: eventually_consistent`, no `Idempotency-Key`, and propagates cancellation. It maps 400 to safe validation feedback, 404 to fail-closed safe denial, 503 to retryable/data-unavailable feedback, and unexpected API/transport/deserialization failures to safe feedback without echoing exception text or raw ProblemDetails.
7. Given the metadata-only rule, when trace inputs, results, logs, tests, or markup are produced, then no tenant id on the wire, transcript text, prompt, file path, file content, byte range, workspace id, memory payload, secret, token, command body, proposal body, raw sibling denial detail, or raw problem body is fetched, logged, serialized, or shown.
8. Given FrontComposer delivery constraints, when the workbench is implemented, then the lowest sufficient FrontComposer gradient is attempted first: add metadata-only descriptor/wrapper contracts for trace rows/panels and attempt Level 2 composition before escalating to a Level 3 slot or Level 4 hand-rolled component. Any escalation is documented with contract version and rationale in docs/parity/projection notes.
9. Given screen-reader and responsive requirements, when the trace renders at desktop, tablet, and mobile breakpoints, then trace order and candidate comparisons are semantically readable with headings, labels, table/list semantics, visible focus, non-color-only badges, accessible full identifiers, and no overlap or truncation-only critical data.
10. Given Epic 5 boundaries, when this story completes, then it does not implement Story 5.7 audit timeline/export, Story 5.8 warnings dashboard, Story 5.9 maintenance mutations, or Story 5.10 MCP/CLI surfaces, except for documenting the trace field names, selectors, and parity handoff those stories must reuse.
11. Given test automation requirements, when the story is implemented, then bUnit/source/contract/leakage tests cover conversation trace, attachment trace, no-match, single-candidate, multiple-candidates, excluded/failed-closed evidence, safe failure mapping, selector stability, and payload leakage; Playwright fixme/page-object selectors are updated; the FrontComposer inspect gate and solution build are documented.

## Tasks / Subtasks

- [x] Define the resolution trace view model and FrontComposer descriptor contract (AC: 3, 4, 5, 8, 10)
  - [x] Add metadata-only descriptor/wrapper contracts under `src/Hexalith.Projects.Contracts/Ui/`, for example `ProjectResolutionTraceProjection`, `ProjectResolutionTraceCandidateProjection`, and `ProjectResolutionTraceExclusionProjection`, using `[Projection]`, `[BoundedContext("Projects")]`, display metadata, stable field names, and a documented contract version. Keep contracts safe and additive.
  - [x] Model trace fields from existing DTOs only: input mode, presented ids, includeArchived, observedAt, `ResolutionResult`, candidate `ProjectId`, optional `DisplayName`, `Rank`, `Score`, `ReasonCodes`, exclusion `ProjectId`, optional `DisplayName`, `ReferenceState`, optional `ProjectReasonCode`, and optional closed `ProjectContextInclusionDiagnostic`.
  - [x] Do not add `TenantId`, raw correlation/task ids to the rendered trace body, sibling payload fields, proposal fields, command fields, or new persisted trace identifiers.
  - [x] Keep `ResolutionResult.NoMatch`, `SingleCandidate`, and `MultipleCandidates` as the only wire outcomes. If a UI helper needs visual labels `Resolved`, `Excluded`, or `FailedClosed`, derive them inside UI mapping from the result/exclusion evidence and do not expose them as a new public enum.

- [x] Implement a focused generated-client backed trace source (AC: 2, 3, 6, 7)
  - [x] Add `IProjectResolutionTraceSource`, `ProjectResolutionTraceSource`, `ProjectResolutionTraceRequest`, and `ProjectResolutionTraceLoadResult` under `src/Hexalith.Projects.UI/Diagnostics/` rather than adding trace calls to `ProjectDetailSource` page load. The trace must run only when the operator submits safe trace input.
  - [x] For conversation mode, call `ResolveProjectFromConversationAsync(conversationId, includeArchived, correlationId, ReadConsistencyClass.Eventually_consistent, ct)`.
  - [x] For attachment mode, call `ResolveProjectFromAttachmentsAsync(folderIds, fileIds, includeArchived, correlationId, ReadConsistencyClass.Eventually_consistent, ct)` with deterministic input ordering and duplicate removal.
  - [x] Validate inputs client-side for empty/mixed unsafe forms before calling the client, but preserve server authority: malformed or unauthorized inputs still rely on server safe-denial behavior. Validation text must identify the field only and never echo unsafe content.
  - [x] Map generated-client API exceptions exactly like other UI sources: 400 `validation_error`, 404 `safe_denial`, 503 `data_unavailable`, other API/transport/deserialization failures `resolution_trace_query_failed`. Never show raw exception messages or response bodies.
  - [x] Preserve query semantics: no idempotency header, eventual consistency, cancellation propagation, no mutation submission, no 202 command lifecycle.

- [x] Replace the Resolution tab placeholder with the workbench UI (AC: 1, 3, 4, 5, 8, 9)
  - [x] Update `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` to render a real workbench in the existing `ResolutionSection` while keeping `project-detail-section-resolution`.
  - [x] Prefer a small reusable component under `src/Hexalith.Projects.UI/Components/Shared/` if the trace UI becomes large; keep the page readable and preserve existing metadata/setup/reference/audit/actions sections.
  - [x] Add stable selectors: `project-resolution-trace-workbench`, `project-resolution-trace-mode`, `project-resolution-trace-conversation-id`, `project-resolution-trace-folder-id`, `project-resolution-trace-file-id`, `project-resolution-trace-include-archived`, `project-resolution-trace-run`, `project-resolution-trace-outcome`, `project-resolution-trace-input-summary`, `project-resolution-trace-candidate`, `project-resolution-trace-candidate-comparison`, `project-resolution-trace-reason`, `project-resolution-trace-exclusion`, and `project-resolution-trace-feedback`.
  - [x] Render `ResolutionResult` and `ProjectReasonCode` through `ProjectStatusBadge` / `ProjectVocabularyDescriptors`; render `ReferenceState` exclusions through the shared reference-state descriptors. Do not duplicate display labels/severities.
  - [x] Candidate comparison should use semantic tables/lists with scoped headers or labelled groups. Long project ids must wrap or expose the full value accessibly; do not truncate without a full accessible value.
  - [x] Empty/initial state must say no trace has been run yet. Denied/unavailable/validation states must render as feedback, not as an empty trace.

- [x] Preserve compute-on-demand and persistence boundaries (AC: 2, 4, 7, 10)
  - [x] Do not add a `ProjectResolutionTraceProjection`, trace store, audit event, event payload, command, Dapr state key, or SignalR source-of-truth for traces. The workbench reads existing query endpoints and discards the computed result when the UI state is cleared.
  - [x] Do not add or modify resolution engine scoring rules. The workbench displays the engine output; it must not recompute candidate scores, ranks, thresholds, or outcome decisions.
  - [x] Do not create a new backend "operator trace" endpoint unless an existing query cannot satisfy an AC safely. If a public contract change becomes unavoidable, it must be additive, metadata-only, OpenAPI/client regenerated, fingerprint-tested, and leakage-tested.
  - [x] Do not add memory-backed resolution unless a separate architecture decision and endpoint already exist. `MemoryMatched` remains a shared reason code the engine can represent, not a license to invent a new query in this story.

- [x] Update parity, projection, and payload documentation (AC: 8, 10)
  - [x] Update `docs/parity-matrix.md` with Story 5.6 field names, selectors, supported modes, result/exclusion vocabulary, and MCP/CLI handoff names for Story 5.10.
  - [x] Update `docs/projection-catalog.md` if new FrontComposer descriptor/wrapper contracts are added. Clearly distinguish UI descriptors from persisted projections.
  - [x] Update `docs/payload-taxonomy.md` and `PayloadClassification.SafeFields` only if a new safe UI evidence category is introduced. Candidate score/rank are safe only as transient computed trace metadata, not as audit/reference/export payload.
  - [x] If a Level 3/4 FrontComposer escalation is used, document the rationale and why Level 2 was insufficient.

- [x] Add focused tests and quality gates (AC: all)
  - [x] Add Contracts tests proving the new descriptor/wrapper contracts have bounded-context metadata, stable display/field metadata, and no tenant/payload fields.
  - [x] Add source tests for `ProjectResolutionTraceSource`: conversation mode call shape, attachment mode call shape, deterministic/deduped inputs, eventual consistency, no idempotency, safe 400/404/503/transport mapping, and cancellation propagation.
  - [x] Add mapper tests for outcome derivation: `SingleCandidate -> Resolved`, `NoMatch -> NoMatch`, `MultipleCandidates -> MultipleCandidates`, archived/policy exclusions -> `Excluded`, unverifiable authorization/freshness/conflict/invalid/unavailable diagnostics -> `FailedClosed`.
  - [x] Add bUnit tests for the Resolution tab: initial state, safe validation, conversation trace, attachment trace, no-match, single candidate, multiple candidates, exclusion evidence, non-color-only badges, semantic candidate comparison, long identifiers, and payload-deny assertions against rendered markup.
  - [x] Extend `NoPayloadLeakageTests` for any new DTO/projection/trace load result/serialized evidence/markup fixture.
  - [x] Update `tests/e2e/support/page-objects/project-detail.page.ts` and add or extend Playwright fixme specs for trace selectors, keyboard path, axe accessibility, and no payload leakage. Existing `tests/e2e/specs/projects-resolution.spec.ts` already covers API-level resolution fixme scaffolding; do not duplicate API-only assertions unless needed for the Web journey.
  - [x] Run:
    - [x] `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal`
    - [x] focused `dotnet test` for `ProjectVocabulary`, `ProjectsUI`, `NoPayloadLeakage`, `ClientGeneration` if generated clients change, and any new trace source/contract tests
    - [x] `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1`
    - [x] `git diff --check`

## Dev Notes

### Current State

- Story 5.4 created the `/projects/{ProjectId}` detail inspector with tabs for metadata, setup, references, resolution, audit, and actions. Story 5.5 replaced the References placeholder; the Resolution tab still contains only "Resolution trace workbench is handled by Story 5.6" plus warning count text. [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- `ProjectDiagnosticHeader`, `ProjectStatusBadge`, `ProjectConsoleFeedback`, `ProjectEmptyState`, and shared badge descriptors already exist. Reuse them; do not build a separate visual language for the trace workbench. [Source: src/Hexalith.Projects.UI/Components/Shared/ProjectDiagnosticHeader.razor] [Source: src/Hexalith.Projects.UI/Rendering/ProjectVocabularyRendering.cs] [Source: src/Hexalith.Projects.Contracts/Ui/ProjectVocabularyDescriptors.cs]
- `ProjectDetailSource` currently loads base detail, operator diagnostics, context explanation, and conversation rows for the reference-health matrix. Do not make it call resolution trace queries on page load; trace execution should be explicit because resolution can enumerate candidates and should not run just to open a detail page. [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectDetailSource.cs]
- `ProjectDetailLoadResult` currently carries `ReferenceHealthRows` only. Add a separate trace source/result instead of overloading the detail load result unless there is a clear local pattern requiring page-level state. [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectDetailLoadResult.cs]
- E2E page objects already expose `resolutionSection` but no trace-specific selectors. Add the trace selectors listed above. [Source: tests/e2e/support/page-objects/project-detail.page.ts]

### Existing Resolution Query Contracts

- The generated client already exposes `ResolveProjectFromConversationAsync(string conversationId, bool? includeArchived, string x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken)` and `ResolveProjectFromAttachmentsAsync(IEnumerable<string> folderId, IEnumerable<string> fileId, bool? includeArchived, string x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken)`. Prefer these over any new backend surface. [Source: src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs]
- Both query endpoints are synchronous compute-on-demand reads. They write no events, projections, state, or trace history; only the later `ConfirmProjectResolution` mutation persists a selected ambiguous result. [Source: src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs] [Source: src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs]
- `ProjectResolution` is the approved metadata-only wire model. It intentionally carries no `TenantId` field and contains `Result`, `Candidates`, `Excluded`, and `ObservedAt`. [Source: src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs]
- Candidate fields are safe computed metadata: `ProjectId`, optional `DisplayName`, distinct `ReasonCodes`, one-based `Rank`, and numeric `Score`. Exclusion fields are `ProjectId`, optional `DisplayName`, `ReferenceState`, optional `ReasonCode`, and optional closed diagnostic string. [Source: src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs]
- Query APIs reject `Idempotency-Key`, require/accept only eventually-consistent freshness, thread correlation/task metadata server-side, and return safe 400/404/503 outcomes. Preserve this behavior in the UI source. [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml]

### Resolution Semantics and Trace Mapping

- `docs/resolution-scoring-heuristic.md` is the single source of truth for scoring and trace mapping. The UI must display, not recompute, these rules. [Source: docs/resolution-scoring-heuristic.md]
- Weights are engine-owned: `ConversationLinked=50`, `ProjectFolderMatched=45`, `FileReferenceMatched=35`, `MemoryMatched=30`, `MetadataMatched=20`; each reason code contributes at most once. Do not duplicate these in UI except as explanatory text in tests/docs if necessary. [Source: docs/resolution-scoring-heuristic.md#Per-Reason-Code-Weights]
- Candidate qualification is `Score >= 20`; confidence bands are documentation only, not a wire enum. Do not add `ResolutionConfidence` or similar. [Source: docs/resolution-scoring-heuristic.md#Confidence-Bands]
- Outcome decision is by qualifying candidate count: 0 `NoMatch`, 1 `SingleCandidate`, 2+ `MultipleCandidates`, with deterministic order by score descending then `ProjectId` ordinal ascending. [Source: docs/resolution-scoring-heuristic.md#Single-vs-Multiple-Threshold]
- Trace visual states map as: `Resolved` from `ResolutionResult.SingleCandidate`; `NoMatch` from `ResolutionResult.NoMatch`; `MultipleCandidates` from `ResolutionResult.MultipleCandidates`; `Excluded` from `ResolutionExclusion` rows for archived/policy exclusions; `FailedClosed` from exclusion diagnostics for unverifiable tenant, authorization, freshness, conflict, invalid, pending, stale, unavailable, or ambiguous evidence. [Source: docs/resolution-scoring-heuristic.md#Trace-Mapping]
- Exclusion diagnostics must remain members of `ProjectContextInclusionDiagnostic`; free-form upstream error text is forbidden. [Source: src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionDiagnostic.cs]

### FrontComposer / Fluent UI Guardrails

- Architecture calls resolution trace "the lone Level-3/4 customization candidate", but the workflow still requires attempting the lowest sufficient gradient. Start with descriptor/wrapper contracts and a Level 2 composition attempt; escalate only when the side-by-side comparison/workbench cannot be expressed safely. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- Story 5.4 already recorded a deliberate Level-4 escalation for the broader inventory/detail inspector. Reusing that existing hand-rolled detail page is acceptable, but the trace workbench still needs descriptor metadata so FrontComposer inspect/parity gates have a stable contract. [Source: _bmad-output/implementation-artifacts/5-4-project-inventory-detail-views.md]
- Local package pins on 2026-05-30 are authoritative: .NET SDK `10.0.300`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, bUnit `2.7.2`, xUnit v3 `3.2.2`, Dapr `1.17.9`, Aspire `13.3.5`. Do not upgrade/downgrade or inline package versions. [Source: global.json] [Source: Directory.Packages.props] [Source: Hexalith.FrontComposer/Directory.Packages.props]

### UX Requirements

- UX-DR9 defines the resolution trace view: evaluated inputs, candidate projects, reason-code badges, inclusion/exclusion evidence, final outcome, and side-by-side candidate comparison. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- UX-DR15 defines the custom Resolution Trace component anatomy: input summary, candidate list, reason-code badges, inclusion/exclusion evidence, outcome panel, and safe next actions. In this story safe next actions are read-only links/labels only; maintenance actions are Story 5.9. [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- Journey 1 starts from a safe conversation, folder, file, memory, or resolution-case identifier and loads resolution trace metadata. Current code supports conversation and folder/file attachments only; render memory/resolution-case fields only as disabled/deferred if design requires visibility. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Journey-1-Diagnose-Project-Resolution]
- Responsive strategy favors desktop side-by-side comparisons while tablet/mobile should preserve critical metadata, warnings, reason codes, and action consequences without overlap. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Responsive-Design--Accessibility]
- Accessibility requires keyboard access, visible focus, semantic headings/landmarks, status text labels, sufficient contrast, screen-reader-readable tables/timelines/comparisons, and no hover-only critical actions. [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Responsive-Design--Accessibility]

### Previous Story Intelligence

- Story 5.5 added `ProjectReferenceHealthRowProjection`, `ProjectReferenceHealthMapper`, and reference-health enrichment using existing generated clients without backend/OpenAPI changes. Follow the same pattern: prefer UI mapping over backend expansion when existing query DTOs are sufficient. [Source: _bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md]
- Story 5.5 proved the detail page can add dense, metadata-only operational subviews while preserving header/tabs/route/failure mapping. The 5.6 workbench should replace only the Resolution tab placeholder and leave the completed Reference Health Matrix intact. [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- Story 5.5 review left a low follow-up that `FreshnessTrustState` mixes vocabularies in reference health. Do not spread that ambiguity into trace. For trace, display the resolution result `ObservedAt` and query freshness feedback clearly; avoid inventing a new freshness vocabulary. [Source: _bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md#Senior-Developer-Review-AI]
- Story 5.5 source tests use NSubstitute in UI tests; Projects core/server resolution tests prefer deterministic builders/in-memory fakes. Match the target test project's local convention. [Source: tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectDetailSourceTests.cs] [Source: _bmad-output/implementation-artifacts/4-3-resolve-project-from-attachments.md]
- Story 4.1 delivered the pure resolution engine and proved no persistence/I/O/wall-clock dependency. Story 5.6 must not reopen that boundary or add trace persistence. [Source: _bmad-output/implementation-artifacts/4-1-resolution-engine-compute-on-demand.md]
- Stories 4.2 and 4.3 delivered the query endpoints and generated clients. Use them as-is; do not reimplement candidate enumeration, ACL reads, scoring, or endpoint semantics in the Web layer. [Source: _bmad-output/implementation-artifacts/4-2-resolve-project-from-conversation.md] [Source: _bmad-output/implementation-artifacts/4-3-resolve-project-from-attachments.md]

### Project Structure Notes

- UI page update: `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` and `.razor.css`.
- Optional reusable UI component: `src/Hexalith.Projects.UI/Components/Shared/`.
- Trace source/result/mapping: `src/Hexalith.Projects.UI/Diagnostics/`.
- Descriptor/wrapper contracts: `src/Hexalith.Projects.Contracts/Ui/`.
- Do not touch `src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs`, scoring rules, server endpoint behavior, generated `.g.cs` files, or OpenAPI unless an existing generated client cannot satisfy an AC safely.
- Tests: UI component/source tests under `tests/Hexalith.Projects.UI.Tests/`, contract descriptor tests under `tests/Hexalith.Projects.Contracts.Tests/Ui/`, leakage tests under `tests/Hexalith.Projects.Tests/Leakage/`, and E2E selectors/specs under `tests/e2e/`.
- Do not read or modify BMAD folders inside submodules. Do not initialize nested submodules. Do not create submodule pointer churn.

### Hard Stops

- Stop before coding if the implementation appears to require storing resolution traces, adding a trace projection, emitting audit events for trace reads, or persisting candidate scores/ranks/rejected ids.
- Stop before coding if a new backend/OpenAPI route is being added only to reshape data already available through `ResolveProjectFromConversation` or `ResolveProjectFromAttachments`.
- Stop before coding if memory-backed resolution, proposal preview, project creation, ambiguous confirmation, archive/restore/relink/unlink/reevaluate, audit export, MCP, or CLI behavior enters scope.
- Stop before coding if a new shared-vocabulary enum member, confidence enum, Web-only result enum, duplicate severity table, or free-text diagnostic mapping appears necessary.
- Stop before coding if generated FrontComposer files or generated client `.g.cs` files appear to need hand edits.
- Stop before coding if sibling payloads, file paths/content, memory payloads, transcripts, prompts, secrets, raw tokens, raw ProblemDetails bodies, command bodies, or proposal bodies are needed to make the UI useful.
- Stop before coding if package upgrades/downgrades, analyzer suppressions, nullable disable, warning downgrade, central package management bypass, submodule pointer changes, nested submodule init, or BMAD reads inside submodules are required.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-5.6-Resolution-Trace-Workbench]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Journey-1-Diagnose-Project-Resolution]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Resolution-Trace]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture-operational-surfaces]
- [Source: docs/resolution-scoring-heuristic.md]
- [Source: docs/parity-matrix.md#Story-5.5-Reference-Health-Contract]
- [Source: docs/payload-taxonomy.md]
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs]
- [Source: src/Hexalith.Projects.Contracts/Ui/ResolutionResult.cs]
- [Source: src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs]
- [Source: src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs]
- [Source: src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionDiagnostic.cs]
- [Source: src/Hexalith.Projects.Contracts/Ui/ProjectVocabularyDescriptors.cs]
- [Source: src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs]
- [Source: src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs]
- [Source: src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs]
- [Source: src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor]
- [Source: src/Hexalith.Projects.UI/Diagnostics/ProjectDetailSource.cs]
- [Source: tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs]
- [Source: tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectDetailSourceTests.cs]
- [Source: tests/e2e/specs/projects-resolution.spec.ts]
- [Source: tests/e2e/support/page-objects/project-detail.page.ts]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-30: `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false -m:1 -v:minimal` passed with 0 warnings / 0 errors.
- 2026-05-30: VSTest `dotnet test` focused lanes were blocked by sandbox local socket permission (`SocketException: Permission denied`) before execution; reran with xUnit v3 in-process runner.
- 2026-05-30: xUnit in-process `Hexalith.Projects.Contracts.Tests` focused classes passed 59/59.
- 2026-05-30: xUnit in-process `Hexalith.Projects.UI.Tests` focused classes passed 22/22 after QA gap coverage additions.
- 2026-05-30: xUnit in-process `Hexalith.Projects.Tests` `NoPayloadLeakageTests` passed 55/55.
- 2026-05-30: `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1` passed; 7 annotated contract inputs detected, real FrontComposer inspect gate reported no warnings.
- 2026-05-30: `git diff --check` passed.
- 2026-05-30: `npm run typecheck` under `tests/e2e` could not run because `tsc` is not installed in the workspace (`node_modules` absent); Playwright fixme files were not browser-executed.
- 2026-05-30: QA generate E2E workflow added bUnit/source gap tests for NoMatch, Excluded, FailedClosed, long accessible identifiers, empty attachment validation, and transport/deserialization failure mapping.
- 2026-05-30 (review): VSTest `dotnet test` lane re-run with the sandbox local-socket constraint lifted (in-process subset counts from earlier are superseded by these full-project runs): `Hexalith.Projects.Contracts.Tests` 152/152, `Hexalith.Projects.UI.Tests` 74/74, `Hexalith.Projects.Tests` (incl. `NoPayloadLeakage`) 574/574 — all green. This resolves the earlier VSTest-blocked note and the 59-vs-24 focused-count discrepancy in the test-summary; the authoritative figures are the full-project counts above.
- 2026-05-30 (review): After review auto-fixes, `Hexalith.Projects.UI.Tests` is 81/81 (added a redacted→Excluded mapper regression test and a 6-delimiter attachment-split theory). Solution rebuild `-warnaserror` remained 0 warnings / 0 errors.
- 2026-05-30 (review): Playwright specs in `tests/e2e/specs/projects-resolution-trace.spec.ts` remain `test.fixme` scaffolding by deliberate fixture convention; this satisfies AC11's "Playwright fixme/page-object selectors are updated" requirement. Browser execution stays deferred until the E2E `node_modules`/AppHost fixture is provisioned (tracked in sprint status), not as part of this story.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Create-story validation applied against checklist: story includes source-derived ACs, current file locations, existing generated-client query contracts, compute-on-demand/persist-nothing guardrails, FrontComposer/Fluent UI constraints, previous-story learnings, testing requirements, parity documentation handoff, and hard stops.
- Implemented the read-only Resolution Trace Workbench in the existing Project detail Resolution tab while preserving the diagnostic header, tab structure, `project-detail-section-resolution`, safe feedback rendering, and compute-on-demand-only query behavior.
- Added metadata-only transient trace descriptor contracts, generated-client backed trace source, deterministic attachment input normalization, safe failure mapping, cancellation propagation, derived visual outcome labels, responsive/semantic candidate comparison, exclusion evidence rendering, and stable E2E selectors.
- Preserved Epic 5 boundaries: no backend/OpenAPI/generated-client changes, no trace persistence, no audit/export/history surface, no resolution scoring changes, no memory-backed resolution, no proposal/confirmation/maintenance/MCP/CLI behavior.
- Updated parity/projection/payload documentation and safe-field taxonomy for transient trace metadata; candidate score/rank remain limited to the computed trace workbench context.
- Added contract/source/mapper/bUnit/leakage tests and Playwright fixme/page-object selector scaffolding for Story 5.6.
- QA gap pass added missing direct bUnit/source coverage for NoMatch, Excluded, FailedClosed, long accessible identifiers, empty attachment validation, and generic transport/deserialization safe feedback.

### File List

- _bmad-output/implementation-artifacts/5-6-resolution-trace-workbench.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- docs/parity-matrix.md
- docs/payload-taxonomy.md
- docs/projection-catalog.md
- src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs
- src/Hexalith.Projects.Contracts/Ui/ProjectResolutionTraceCandidateProjection.cs
- src/Hexalith.Projects.Contracts/Ui/ProjectResolutionTraceExclusionProjection.cs
- src/Hexalith.Projects.Contracts/Ui/ProjectResolutionTraceProjection.cs
- src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor
- src/Hexalith.Projects.UI/Components/Shared/ProjectResolutionTraceWorkbench.razor
- src/Hexalith.Projects.UI/Components/Shared/ProjectResolutionTraceWorkbench.razor.css
- src/Hexalith.Projects.UI/Diagnostics/IProjectResolutionTraceSource.cs
- src/Hexalith.Projects.UI/Diagnostics/ProjectResolutionTraceLoadResult.cs
- src/Hexalith.Projects.UI/Diagnostics/ProjectResolutionTraceMapper.cs
- src/Hexalith.Projects.UI/Diagnostics/ProjectResolutionTraceRequest.cs
- src/Hexalith.Projects.UI/Diagnostics/ProjectResolutionTraceSource.cs
- src/Hexalith.Projects.UI/Program.cs
- tests/Hexalith.Projects.Contracts.Tests/Models/PayloadClassificationTests.cs
- tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectResolutionTraceProjectionTests.cs
- tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs
- tests/Hexalith.Projects.UI.Tests/Components/ProjectDetailPageTests.cs
- tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectResolutionTraceSourceTests.cs
- tests/e2e/specs/projects-accessibility.spec.ts
- tests/e2e/specs/projects-inventory-detail.spec.ts
- tests/e2e/specs/projects-resolution-trace.spec.ts
- tests/e2e/support/page-objects/project-detail.page.ts

### Change Log

- 2026-05-30: Implemented Story 5.6 Resolution Trace Workbench with transient descriptor contracts, UI trace source, workbench rendering, docs, tests, and quality gates; status moved to review.
- 2026-05-30: story-automator-review (adversarial multi-agent, 9 dimensions, per-finding verification). 1 CRITICAL + 3 HIGH + 2 MEDIUM + 3 LOW confirmed (2 findings rejected by verification). CRITICAL auto-fixed in code; HIGH/MEDIUM doc-accuracy findings resolved by re-running the real VSTest gate and correcting the records; test-coverage gaps auto-fixed. 0 CRITICAL remain → status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-05-30
**Outcome:** Approve (after auto-fixes). 0 CRITICAL remaining.

### Scope

Adversarial review of all 24 source/doc/test files in the File List against the 11 ACs, the task checklist, and the story's hard stops, plus independent re-execution of the build and test gates. `_bmad-output/` files were excluded from the code review per workflow policy. File List cross-checked against `git status` — complete and accurate for all source code (the only extra git changes are `_bmad-output/` tracking files, which are out of review scope).

### Gates re-verified (this review actually ran them)

- `dotnet build Hexalith.Projects.slnx -warnaserror /p:UseSharedCompilation=false /p:NuGetAudit=false`: **0 warnings / 0 errors**.
- `dotnet test` (VSTest, sandbox socket constraint lifted): Contracts **152/152**, UI **74/74** → **81/81** after fixes, Tests incl. leakage **574/574**.
- `git diff --check`: clean.

### Findings and resolution

1. **[CRITICAL — FIXED] Mapper mis-classified policy-redacted exclusions as FailedClosed.** `ProjectResolutionTraceMapper.IsFailedClosed` listed `ProjectContextInclusionDiagnostic.ReferenceRedacted`. Per `docs/resolution-scoring-heuristic.md#Trace-Mapping`, redacted is a *policy* exclusion → `Excluded`, not unverifiable → `FailedClosed`. A `(ReferenceState.Excluded, referenceRedacted)` exclusion would have rendered the wrong outcome panel (AC3/AC4/AC5). Removed `ReferenceRedacted` from `IsFailedClosed` and documented the policy-vs-unverifiable boundary inline. Added regression test `MapperDerivesExcludedForPolicyRedactedExclusion`.
2. **[HIGH — RESOLVED] `dotnet test` task `[x]` but VSTest was sandbox-blocked.** The original dev run fell back to an in-process runner. This review re-ran the real VSTest lane (constraint lifted) and it passed; Debug Log updated with the authoritative full-project counts.
3. **[HIGH — RESOLVED] Test-count discrepancy (59/59 vs 24/24).** Reconciled to the verified full-project VSTest counts; the focused in-process subset numbers are explicitly superseded in the Debug Log.
4. **[HIGH — RESOLVED] Playwright specs `[x]` but remain `test.fixme`.** Confirmed this is the intended scaffolding deliverable and satisfies AC11; documented that browser execution is deferred to E2E fixture provisioning (sprint-status tracked).
5. **[MEDIUM — FIXED] Candidate-comparison semantic table not asserted.** The component renders correct `<caption>/<thead scope=col>/<tbody>/<th scope=row>` semantics (AC9) but the bUnit test only checked text. Added explicit structural assertions.
6. **[MEDIUM — RESOLVED] `[x]` gate marks lacked workaround qualification.** Debug Log now distinguishes specified-form vs workaround and records the verified re-run.
7. **[LOW — FIXED] Non-color-only badges not asserted.** Added assertions that each reason badge carries visible text (AC9).
8. **[LOW — FIXED] Delimiter splitting only implicitly tested.** Added a 6-case theory (comma/semicolon/newline/CRLF/tab/space) over the attachment input normalizer.
9. **[LOW — ACK] E2E `.fixme` specs not browser-executed.** Infrastructure limitation (`node_modules` absent); selectors/page-object are correctly defined; deferred as above.

**Rejected by verification (not defects):** an alleged missing text-span on the exclusion `ReferenceState` badge (`ProjectStatusBadge` already renders a mandatory `Label`, so it is never color-only) and an alleged missing negative idempotency assertion (NSubstitute's exact-signature match already proves no `Idempotency-Key` parameter exists).

### Confirmed strengths (verified, no action)

- No tenant id, transcript, prompt, file path/content, byte range, workspace id, memory payload, secret, token, command/proposal body, or raw ProblemDetails crosses the wire, logs, or rendered markup (AC7) — leakage suite 574/574, including the trace load result.
- No new persisted projection/store/audit event/command/Dapr-state/SignalR for traces; no scoring/rank/threshold recomputation; no new shared-vocabulary/confidence/Web-only enum; no generated `.g.cs` or OpenAPI edits; no package churn (hard stops honored).
- Trace source uses correct client call shapes, caller-generated correlation id, `Eventually_consistent` freshness, no `Idempotency-Key`, cancellation propagation, and safe 400/404/503/other mapping with no raw exception text.
