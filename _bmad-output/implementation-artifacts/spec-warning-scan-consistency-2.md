---
title: 'Complete warning scan consistency across Web MCP and CLI'
type: 'bugfix'
created: '2026-09-05'
status: 'in-progress'
review_loop_iteration: 0
followup_review_recommended: false
baseline_revision: '2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2'
baseline_commit: '2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2'
context:
  - '{project-root}/docs/parity-matrix.md'
  - '{project-root}/docs/projection-catalog.md'
warnings: [multiple-goals, oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Web, MCP, and CLI diagnose different project sets, MCP query `Take` can shrink the diagnostic scan, and the MCP dashboard combines counters from two inventory reads. MCP also makes diagnostic failures unobservable when no healthy warning row is emitted.

**Approach:** Use one deterministic ordinal-first 25-project diagnostic window across all three surfaces while retaining full visible-inventory totals. Apply MCP `Take` only after the full warning scan, expose an always-emitted `projects.warningScanSummary` resource, and build each MCP dashboard from one inventory snapshot.

## Boundaries & Constraints

**Always:** Select the diagnostic window by ordinal `ProjectId` and cap it at 25; keep that project-window limit distinct from the per-diagnostic audit limit of 25; count every selected project in `ScannedProjectCount`, including unavailable diagnostics; derive inventory/lifecycle totals from every visible row and warning/unavailable totals from the selected window; retain server-derived tenant scope, cancellation, safe partial-failure mapping, metadata-only output, warning ordering, and existing Web synthetic-unavailable and MCP warning-row shapes. For MCP warning queries, emit at most `Take` ordered rows while reporting the full matching warning count in `TotalCount`.

**Never:** Edit the deferred-work ledger, bundle intent, `.bmad-loop` decision evidence, generated client artifacts, or generated `_bmad/render/**` snapshots; expose raw exceptions, ProblemDetails, payloads, or client-derived tenant authority; change REST/domain contracts, warning semantics, maintenance behavior, or UI markup/styles; let MCP `Take` affect diagnostic membership.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Inventory exceeds window | More than 25 visible projects in non-ordinal input order | Web, MCP, and CLI diagnose exactly the ordinal-first 25; visible/lifecycle totals cover the full inventory | Tail projects are never diagnosed and do not affect scan-derived counts |
| Output limit below scan | MCP warning query `Take` is below the number of warning rows | Diagnose the fixed window, sort all warnings, report the full matching `TotalCount`, then limit emitted `Items` | Unavailable diagnostics still contribute to the full scan result |
| No healthy warning row | A completed scan emits no warnings and one diagnostic is unavailable | Warning queue stays empty; `projects.warningScanSummary` emits one safe item with selected cardinality and unavailable count | No unsafe failure detail appears |
| Dashboard inventory changes between reads | The client would return different inventories on consecutive list requests | One dashboard request performs one list request; all totals come from that snapshot and its diagnostic window | Cancellation propagates; list failures retain safe MCP mapping |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Client/Diagnostics/ProjectWarningScanWindow.cs:13-30` -- existing unused shared selector; `ProjectLimit = 25`, ordinal `ProjectId` ordering, then `Take`.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs:24-108` -- materialize the one list snapshot, retain all projected inventory rows, and diagnose only the shared selected window; preserve the separate audit limit and existing failure isolation.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs:68-99` -- reuse unchanged: it already derives inventory totals from all projects and warning counts from scan rows/unavailable count.
- `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs:187-275,546-552` -- replace input-order `Take(25)` with the shared selector; preserve full inventory totals, JSON shapes, safe failures, cancellation, and the separate diagnostic audit limit.
- `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs:32-53,215-300` -- add summary dispatch; scan a supplied inventory snapshot; retain untruncated warnings/counts internally; apply `Take` only to emitted queue items; use one list read for dashboard totals and scanning.
- `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScan.cs:14-17` -- existing unused internal scan result for ordered warnings, scanned cardinality, and unavailable count.
- `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScanSummaryItem.cs:16-21` -- existing unused safe public summary DTO; always return one item from its resource.
- `src/Hexalith.Projects.Mcp/ProjectsMcpDescriptors.cs:23-66` -- register `projects.warningScanSummary` beside warning queue and dashboard; no repository-wide name conflict exists.
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs:31-362` -- existing small-inventory, partial-failure, cancellation, and safe-list coverage; add reverse-ordered over-25 membership and full-total evidence.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs:120-157` and `ProjectsMcpResourceReaderFailureTests.cs:109-190` -- extend dispatch coverage for scan/`Take` decoupling, full `TotalCount`, empty-queue summary, over-25 selection, and one-snapshot dashboard behavior.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpDescriptorTests.cs:19-30`, `ProjectsMcpStory511ParityTests.cs:22-45`, and `ProjectsMcpNoPayloadLeakageTests.cs:49-76` -- lock registration, documentation/common fields, and payload exclusion for the new DTO.
- `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs:191-291` -- add non-ordinal over-25 diagnostic-call membership plus full inventory/lifecycle versus scan-derived JSON counts for both commands.
- `docs/parity-matrix.md:87-104,124,143-144` and `docs/projection-catalog.md:362-430` -- document fixed-window cardinality, full-versus-scanned totals, summary availability, one-snapshot dashboard construction, and emitted-row-only `Take` semantics.
- `.bmad-loop/runs/20260905-175129-b379/bundles/warning-scan-consistency/intent.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, and `_bmad/render/**` -- read-only workflow evidence; exclude from implementation changes.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs` and `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs` -- consume `ProjectWarningScanWindow.Select` while retaining complete inventory totals and existing safe contracts.
- [x] `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs`, `src/Hexalith.Projects.Mcp/ProjectsMcpDescriptors.cs`, `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScan.cs`, and `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScanSummaryItem.cs` -- integrate the shared window, one-snapshot scan, post-scan output limit, full warning count, and always-emitted summary.
- [x] `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpDescriptorTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpStory511ParityTests.cs`, and `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs` -- cover every matrix scenario, exact scan membership/order, observable counts, cancellation continuity, and safe output.
- [x] `docs/parity-matrix.md` and `docs/projection-catalog.md` -- publish the approved cross-surface scan and MCP resource contracts and keep descriptor/documentation parity green.

**Acceptance Criteria:**

- Given identical visible inventories larger than 25 in arbitrary order, when Web, MCP, and CLI warning/dashboard surfaces execute, then all diagnose the same ordinal-first 25 ids while full visible/lifecycle totals include every visible project.
- Given MCP queue `Take` below the number of warnings, when dispatch completes, then `Items` are limited only after the complete fixed-window scan and `TotalCount` reports all matching ordered warning rows.
- Given a completed scan with no warning rows and unavailable diagnostics, when MCP resources are queried, then the queue stays empty and the summary still returns exactly one safe item carrying selected-project and unavailable counts.
- Given one MCP dashboard request, when the list client is observed, then it is called exactly once and all dashboard counters are derived from that snapshot.
- Given cancellation or an unsafe upstream diagnostic failure, when an affected surface runs, then cancellation propagates and any successful partial output excludes failure payload detail.

## Spec Change Log

## Review Triage Log

## Design Notes

The selector remains in `Hexalith.Projects.Client` because all three adapters already consume generated `ProjectListItem` values there. MCP keeps summary state separate from warning rows so an empty queue remains backward-compatible while scan cardinality and failures stay observable. `QueryResult.TotalCount` represents all matching warning rows; only `Items` is bounded by `Take`.

## Verification

**Commands:**

- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds without dependency changes.
- `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore` and run the built xUnit v3 assembly with `-class Hexalith.Projects.UI.Tests.Diagnostics.ProjectWarningsDashboardSourceTests` -- expected: focused Web tests pass.
- `dotnet build tests/Hexalith.Projects.Mcp.Tests/Hexalith.Projects.Mcp.Tests.csproj --no-restore` and run the built xUnit v3 assembly with the four affected MCP test classes -- expected: focused MCP tests pass.
- `dotnet build tests/Hexalith.Projects.Cli.Tests/Hexalith.Projects.Cli.Tests.csproj --no-restore` and run the built xUnit v3 assembly with `-class Hexalith.Projects.Cli.Tests.ProjectsCliApplicationTests` -- expected: focused CLI tests pass.
- `dotnet build Hexalith.Projects.slnx --no-restore` -- expected: zero warnings and errors.
- `git diff --check` -- expected: no whitespace or conflict-marker errors; ledger, bundle intent, decision evidence, and generated workflow snapshots are unchanged.
