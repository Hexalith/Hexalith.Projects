---
title: 'Harden Web warning dashboard cancellation and diagnostic drill-in'
type: 'bugfix'
created: '2026-08-28'
status: 'in-progress'
baseline_revision: 'a6bb45a6fbcf8c771eb6cb15411aaf774814a1ce'
baseline_commit: 'a6bb45a6fbcf8c771eb6cb15411aaf774814a1ce'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md'
warnings: [multiple-goals]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The Web warnings/dashboard source converts caller-requested cancellation into safe failure output, and the diagnostic-unavailable tile selects every unavailable row even though its count represents only synthetic per-project diagnostic failures. Both behaviors make the observable dashboard state disagree with the underlying request or count.

**Approach:** Rethrow caller cancellation at both generated-client request boundaries before existing safe failure mapping. Define an explicit predicate for the existing synthetic diagnostic-unavailable row marker and use it for that tile's drill-in while preserving the general unavailable-state filter for ordinary unavailable references.

## Boundaries & Constraints

**Always:** Preserve bounded enrichment, server-derived tenant scope, safe reason codes, payload exclusions, existing dashboard counts, ordinary unavailable-reference behavior, and the established `OperationCanceledException` propagation pattern guarded by the caller token.

**Block If:** Correctness requires changing a public DTO/schema, generated client, endpoint, shared state/reason vocabulary, diagnostic count semantics, or the safe non-cancellation failure mappings.

**Never:** Edit the deferred-work ledger; expose exception/ProblemDetails payloads; add a Web-only warning state; broaden fan-out; change MCP/CLI behavior; add packages, endpoints, mutation flows, CSS, or generated-code edits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Inventory cancellation | `ListProjectsAsync` throws `OperationCanceledException` after the supplied token is cancelled | `LoadAsync` propagates cancellation and returns no feedback result | Rethrow before API/general safe mapping |
| Diagnostic cancellation | A per-project diagnostic request throws `OperationCanceledException` after the supplied token is cancelled | `LoadAsync` propagates cancellation and creates no synthetic unavailable row | Rethrow before per-project API/general safe mapping |
| Diagnostic tile | One synthetic diagnostic-failure row and one ordinary `ReferenceState.Unavailable` row are present | The tile whose count is one renders exactly the synthetic row | Use the explicit synthetic-row predicate |
| General unavailable filter | The warning-state dropdown selects `Unavailable` for the same rows | Both unavailable rows remain selectable under the general state filter | Do not conflate state filtering with diagnostic-failure filtering |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs:24` -- `LoadAsync` has separate inventory-list and bounded per-project diagnostic try/catch boundaries; both general handlers currently swallow caller cancellation. Reuse the guarded cancellation catch in `ProjectResolutionTraceSource.LoadTraceAsync`.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs:36` -- `DiagnosticUnavailableItem` already emits a distinct `operator-diagnostics:<safe-reason>` source marker; expose one null-safe predicate here rather than adding a public projection field.
- `src/Hexalith.Projects.UI/Components/Pages/Home.razor:380` -- `ApplyQueueFilters` composes queue filters; `ApplyDashboardFilterAsync` currently maps `DiagnosticUnavailable` to all `ReferenceState.Unavailable` rows. Track/reset diagnostic-only mode separately from the general state set.
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs:29` -- focused NSubstitute/Shouldly source and mapper coverage, including existing mixed diagnostic failure fixtures.
- `tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs:132` -- bUnit filter/drill-in coverage and helpers for ordinary and synthetic unavailable rows.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- read-only orchestrator evidence; do not modify it.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs` -- add caller-token cancellation rethrows before safe mapping at both request boundaries.
- [x] `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs` -- add the explicit synthetic diagnostic-unavailable predicate over the canonical source marker.
- [x] `src/Hexalith.Projects.UI/Components/Pages/Home.razor` -- make the diagnostic tile use that predicate and reset diagnostic-only mode when a normal dashboard/state filter takes over.
- [x] `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs` -- prove inventory and diagnostic cancellation propagation plus synthetic/ordinary predicate discrimination.
- [x] `tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs` -- prove the diagnostic tile selects exactly its counted rows and the general unavailable filter still includes ordinary unavailable rows.

**Acceptance Criteria:**

- Given caller cancellation during either Web warnings/dashboard request boundary, when `LoadAsync` observes `OperationCanceledException` with the supplied token cancelled, then the exception propagates without feedback or a synthetic row.
- Given synthetic diagnostic-unavailable and ordinary unavailable-reference rows together, when the diagnostic-unavailable tile is activated, then the rendered queue contains exactly the rows represented by its count.
- Given the same mixed rows, when the general unavailable state filter is selected, then ordinary unavailable references remain visible and filtering semantics are unchanged.
- Given non-cancellation API or transport failures, when the source handles them, then the existing safe feedback/row mappings and payload exclusions remain intact.

## Spec Change Log

## Review Triage Log

## Design Notes

The existing `SourceSection` shape already distinguishes synthetic rows (`operator-diagnostics:<reason>`) from reference-derived rows (`operator-diagnostics.references...`). Centralizing that invariant in the mapper avoids a schema revision and prevents component code from duplicating string-shape knowledge.

## Verification

**Commands:**

- `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false` -- expected: zero warnings and errors.
- `dotnet tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.Projects.UI.Tests.dll -class Hexalith.Projects.UI.Tests.Diagnostics.ProjectWarningsDashboardSourceTests -class Hexalith.Projects.UI.Tests.Components.ProjectInventoryPageTests` -- expected: all focused source/component tests pass with zero failures.
- `git diff --check` -- expected: no whitespace errors.
