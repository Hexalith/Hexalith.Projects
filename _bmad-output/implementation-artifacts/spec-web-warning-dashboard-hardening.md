---
title: 'Harden Web warning dashboard cancellation and diagnostic drill-in'
type: 'bugfix'
created: '2026-08-28'
status: 'done'
baseline_revision: 'a6bb45a6fbcf8c771eb6cb15411aaf774814a1ce'
baseline_commit: 'a6bb45a6fbcf8c771eb6cb15411aaf774814a1ce'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md'
warnings: [multiple-goals]
deferred:
  - summary: >-
      The Web Home page never cancels warnings/dashboard loads, so the source's
      caller-cancellation rethrows are unreachable and loads can race.
    evidence: |-
      Home.LoadAsync always passes CancellationToken.None, has no
      CancellationTokenSource tied to disposal or to a newer lifecycle load, and
      has no OperationCanceledException handling. Up to 26 requests keep running
      after navigation, and out-of-order lifecycle reloads end in a last-writer-wins
      state. This predates the hardening story (review findings VG-03, BH-01, EC-09).
    location: >-
      src/Hexalith.Projects.UI/Components/Pages/Home.razor
    severity: medium
  - summary: >-
      The Web dashboard does not reveal that only the first 25 projects are
      diagnosed, and it windows the lifecycle-filtered list rather than the
      all-lifecycle list CLI/MCP use.
    evidence: |-
      ProjectWarningsDashboardSource shows the full TotalVisibleProjects next to
      warning counts bounded by ProjectWarningScanWindow, and has no counterpart
      to MCP ScannedProjectCount or the CLI's bounded-diagnostics note. With an
      Active or Archived filter, it diagnoses a different 25-project window than
      CLI/MCP. Introduced by the warning scan-consistency work (f4509ef), not this
      story (review findings BH-09, EC-05, EC-07).
    location: >-
      src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs
    severity: medium
  - summary: >-
      Dashboard tile drill-ins keep the reason, reference-kind and updated-window
      filters, so a tile can render fewer rows than its count.
    evidence: |-
      ApplyDashboardFilterAsync replaces only the state filter. Synthetic
      diagnostic rows have a null reason and an empty reference kind, so any
      reason/kind filter empties the diagnostic drill-in. The same composition
      applies to every warning tile and predates this story (review findings
      BH-04c, EC-02, EC-10).
    location: >-
      src/Hexalith.Projects.UI/Components/Pages/Home.razor
    severity: low
  - summary: >-
      CLI and MCP warning scans rethrow every OperationCanceledException,
      unlike the Web source's caller-token guard.
    evidence: |-
      ProjectsCliApplication.ScanWarningsAsync (catch at line 237) and
      ProjectsMcpResourceReader.ScanWarningsAsync (catch at line 297) rethrow
      unguarded. A transport timeout aborts the CLI/MCP scan, while the Web source
      records a diagnostic_query_failed synthetic row. This spec forbids CLI/MCP
      changes (review finding BH-11).
    location: >-
      src/Hexalith.Projects.Cli/ProjectsCliApplication.cs
    severity: low
  - summary: >-
      Scan-window code and tests can be tightened: single projection pass,
      ordinal-sensitive test IDs, and named limits.
    evidence: |-
      ProjectWarningsDashboardSource runs ToProjection twice for each in-window
      item. SourceDiagnosesOrdinalFirstWindowAndKeepsFullInventoryTotals uses IDs
      that sort the same under ordinal and culture comparison, and uses a bare 25
      for both ProjectWarningScanWindow.ProjectLimit and DiagnosticAuditLimit. This
      belongs to the scan-consistency code (review finding BH-10).
    location: >-
      tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs
    severity: low
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

### 2026-10-06 — Review pass

- scope: implementation commit `8b60ab6fcd5d9fd371dd711717f71b1362c720f7` plus later edits to the five Code Map files, diffed against `baseline_commit`; the scan-window lines in that diff come from `f4509ef` (warning scan-consistency spec), not this story.
- verdicts: 30 findings — high 0, medium 14, low 10, false 6, maybe-false 0
- findings:
  - `[medium]` `[patch]` VG-01: no test leaves diagnostic-only mode by clicking another warning tile — pre-verified gap; added `DiagnosticDashboardTileYieldsToOtherWarningTiles`.
  - `[medium]` `[patch]` VG-02: the `IsCancellationRequested` guards are untested; an uncancelled-token timeout would still pass if the guards were dropped — pre-verified gap; added inventory and diagnostic timeout tests with `CancellationToken.None`, and the mutation check fails both.
  - `[medium]` `[defer]` VG-03: `Home.LoadAsync` passes `CancellationToken.None`, so the new rethrows are unreachable from the Web page — verified at `Home.razor` `LoadAsync`; this predates the story and the intent targets the source boundaries only.
  - `[medium]` `[patch]` VG-04: the diagnostic tile left the dropdown on "Unavailable" while the queue showed a subset, and re-selecting the same option fires no browser change — verified; the dropdown is now cleared in diagnostic-only mode, as it already is for grouped tiles, and the bUnit test asserts this.
  - `[medium]` `[defer]` BH-01: Home has no disposal or newer-load cancellation and no cancellation handling, so lifecycle reloads can race — verified; predates the story; grouped with VG-03.
  - `[medium]` `[patch]` BH-02: cancellation mocks throw whatever token they receive, and the uncancelled-token path is unproven — verified; mocks now throw only from the received token, and the timeout tests were added. Grouped with VG-02.
  - `[medium]` `[patch]` BH-03: the diagnostic-only qualifier is hidden behind an "Unavailable" dropdown value — same defect as VG-04.
  - `[low]` `[patch]` BH-04a: Active/Archived tiles cleared the diagnostic flag but kept `[Unavailable]`, silently widening the queue after reload — verified in `ApplyDashboardFilterAsync`; the flag is now set only after the lifecycle early return, and `DiagnosticDashboardTileSurvivesLifecycleTileReload` covers it.
  - `[false]` `[reject]` BH-04b: the lifecycle dropdown does not clear the diagnostic flag — the dropdown keeps every warning-state filter, so keeping the qualifier keeps the queue matching the reloaded diagnostic count.
  - `[low]` `[defer]` BH-04c: reason, reference-kind and updated-window filters still apply after the diagnostic tile, so synthetic rows (null reason, empty kind) can drop below the tile count — verified, but every warning tile composes with these filters this way, so it predates the story.
  - `[false]` `[reject]` BH-05: the predicate is fragile and throws NRE on a null `SourceSection` — `SourceSection` is a non-nullable `string` defaulting to `"operator-diagnostics"`, and Web rows come only from the mapper, which always sets it. The intent's design notes choose the marker and Block If forbids a DTO change. The hard-coded page fixture pins the contract and fails loudly if it changes.
  - `[low]` `[patch]` BH-06: the predicate test omits the unknown-state reference collision and the case of the marker with a non-Unavailable state — verified (code correct, gap real); added both assertions.
  - `[false]` `[reject]` BH-07a: the page test hard-codes the tile count apart from the rows — the page only displays the count; count/row parity comes from the source (`diagnosticUnavailableCount++` beside each synthetic row) and is asserted in `SourcePreservesLoadedRowsWhenOneDiagnosticEnrichmentFails`.
  - `[medium]` `[patch]` BH-07b: no test shows another tile clearing diagnostic-only mode — same gap as VG-01.
  - `[false]` `[reject]` BH-08: the diff mixes in scan-window count semantics, a Block If change — those lines come from `f4509ef` under the scan-consistency spec; this story's commit `8b60ab6` does not contain them.
  - `[medium]` `[defer]` BH-09: the Web page never reveals that only 25 projects are diagnosed, and it windows the lifecycle-filtered list where CLI/MCP use `Lifecycle.All` — verified; introduced by the scan-consistency work, not this story.
  - `[low]` `[defer]` BH-10: scan-window tightening — `ToProjection` runs twice per in-window item; the ordinal test IDs cannot distinguish ordinal from culture ordering; a bare `25` conflates `ProjectLimit` and `DiagnosticAuditLimit` — verified; this belongs to the scan-consistency code.
  - `[low]` `[defer]` BH-11: Web rethrows only caller cancellation, while CLI/MCP rethrow every `OperationCanceledException` (`ProjectsCliApplication.cs:237`, `ProjectsMcpResourceReader.cs:297`), so a transport timeout aborts CLI/MCP but becomes a synthetic Web row — verified; the intent mandates the guarded pattern and forbids CLI/MCP changes.
  - `[medium]` `[patch]` EC-01: the dropdown says Unavailable while a hidden subset filter is active — same defect as VG-04.
  - `[low]` `[defer]` EC-02: the tile does not reset the reason/kind/updated filters — same pre-existing composition as BH-04c.
  - `[low]` `[patch]` EC-03: Active/Archived tiles widen the diagnostic drill-in — same defect as BH-04a.
  - `[false]` `[reject]` EC-04: NRE on a null `SourceSection` — refuted as for BH-05.
  - `[medium]` `[defer]` EC-05: scan truncation is not surfaced in Web — same as BH-09.
  - `[low]` `[reject]` EC-06: blank `ProjectId` values sort first and use up window slots — project IDs are server-assigned, so this is unlikely in practice; the fix adds a guard to scan-consistency code outside this story.
  - `[medium]` `[defer]` EC-07: the lifecycle-filtered window differs from CLI/MCP — same as BH-09.
  - `[low]` `[reject]` EC-08: with the token cancelled, a non-OCE callee exception maps to feedback or a synthetic row — real only in a narrow race, unreachable while Home passes `None`; the fix adds guards beyond the intent's established caller-token pattern.
  - `[medium]` `[defer]` EC-09: page disposal does not cancel in-flight loads — same as VG-03.
  - `[low]` `[defer]` EC-10: the AC "exactly the rows" fails under reason/kind/updated filters — same pre-existing composition as BH-04c; with default filters, the AC holds and is tested.
  - `[medium]` `[patch]` EC-11: the AC "general Unavailable selectable" fails in a real browser — same defect as VG-04.
  - `[false]` `[reject]` EC-12: AC4 is broken because only the first 25 projects are diagnosed — AC4 covers failure mappings for diagnosed projects; the window comes from the scan-consistency spec (tracked as BH-09).
- routing: no intent_gap or bad_spec entries, so no loopback; `review_loop_iteration` stays 0.
- patches applied directly (this run had no step-03 subagent to re-engage): `Home.razor` (flag set after the lifecycle early return; dropdown cleared in diagnostic-only mode), `ProjectInventoryPageTests.cs` (+2 tests, dropdown assertion), `ProjectWarningsDashboardSourceTests.cs` (token-driven cancellation mocks, +2 timeout tests, predicate collision assertions).
- verification after patches: Debug build 0 warnings/0 errors; focused classes 25/25 passed; `git diff --check` clean. The mutation check (reverting each production fix) failed 4 of the new or updated tests, as expected.
- deferrals are recorded in frontmatter `deferred:` because this spec forbids editing the deferred-work ledger.

## Design Notes

The existing `SourceSection` shape already distinguishes synthetic rows (`operator-diagnostics:<reason>`) from reference-derived rows (`operator-diagnostics.references...`). Centralizing that invariant in the mapper avoids a schema revision and prevents component code from duplicating string-shape knowledge.

## Verification

**Commands:**

- `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false` -- expected: zero warnings and errors.
- `dotnet tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.Projects.UI.Tests.dll -class Hexalith.Projects.UI.Tests.Diagnostics.ProjectWarningsDashboardSourceTests -class Hexalith.Projects.UI.Tests.Components.ProjectInventoryPageTests` -- expected: all focused source/component tests pass with zero failures.
- `git diff --check` -- expected: no whitespace errors.
