---
title: 'Align warning diagnostic scans across Web MCP and CLI'
type: 'bugfix'
created: '2026-08-28'
status: ready-for-dev
baseline_revision: 393bd990047d9d80160e1aabdcc290c4c67f91ef
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/parity-matrix.md'
  - '{project-root}/docs/projection-catalog.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Web, MCP, and CLI inspect different warning-diagnostic project sets, MCP query `Take` can shrink its scan, and the MCP dashboard can combine inventory and warning counters from separate snapshots. MCP also loses `DiagnosticUnavailable` when no healthy warning row is emitted.

**Approach:** Select one shared, deterministic 25-project diagnostic window while retaining full visible-inventory totals. Decouple MCP emitted-row limits from scanning, build each MCP dashboard from one inventory snapshot, and add an always-emitted `projects.warningScanSummary` resource carrying scanned cardinality and diagnostic-unavailable count.

## Boundaries & Constraints

**Always:** Order the diagnostic window by ordinal project id and cap it at 25; keep the project-window limit distinct from the per-diagnostic audit limit of 25; derive lifecycle/inventory totals from every visible row and warning/unavailable totals from the scanned window; preserve cancellation, server-derived tenant scope, metadata-only output, safe failure mapping, warning-row ordering, and existing Web synthetic-unavailable and MCP warning-row shapes.

**Block If:** A deterministic project identifier is unavailable, the new summary cannot use `projects.warningScanSummary` without conflicting with an existing protocol contract, or implementing the change requires modifying generated client artifacts or public REST/domain contracts.

**Never:** Edit the deferred-work ledger, bundle intent, `.bmad-loop` decision evidence, or generated `_bmad/render/**` snapshots; let an implementation handoff edit this workflow-owned spec, add baseline metadata, or mark its tasks complete; expose raw exceptions, ProblemDetails, payloads, or client-derived tenant authority; change warning semantics, maintenance behavior, or unrelated UI markup/styles; let MCP query `Take` affect which projects are diagnosed.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Inventory exceeds window | More than 25 visible projects in non-ordinal input order | All surfaces diagnose the ordinal-first 25 only; full inventory/lifecycle totals still cover every visible project | Projects after the window are not queried and do not affect scan-derived counts |
| Output is smaller than scan | MCP warning query requests fewer rows than the scan produces | MCP diagnoses the fixed 25-project window, sorts all warning rows, then applies query `Take` only to emitted rows | Per-project diagnostic failures still contribute to the full scan summary |
| No healthy warning row | Scanned diagnostics emit no warning rows and one diagnostic is unavailable | Warning queue remains unchanged/empty and `projects.warningScanSummary` still emits one item with scanned count and `DiagnosticUnavailable = 1` | Unsafe failure detail remains excluded |
| Dashboard snapshot changes | A client could return different inventories on consecutive reads | One MCP dashboard request reads inventory once and derives full totals and scanned warning counters from that exact snapshot | Cancellation propagates; base-list failures retain safe MCP mapping |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.Client/Diagnostics/ProjectWarningScanWindow.cs` -- new shared selector over generated `ProjectListItem`; expose the named 25-project limit and ordinal-id selection without editing generated files.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs:21-108` -- replace the all-project enrichment loop with the shared window while continuing to pass the full inventory to `BuildDashboard` and `FromRows`.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs:68-99` -- reuse unchanged: full inventory totals already separate from queue/unavailable totals.
- `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs:32-53,215-300` -- add summary dispatch; load visible inventory once per resource request; scan a supplied snapshot; keep untruncated scan rows/counts internally; apply query `Take` only when emitting warning rows.
- `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScan.cs` -- new internal one-type scan result carrying all ordered warnings, scanned project count, and unavailable count.
- `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScanSummaryItem.cs` -- new public one-type DTO for the always-emitted summary plus standard tenant, explanation, and payload-exclusion fields.
- `src/Hexalith.Projects.Mcp/ProjectsMcpDescriptors.cs:23-65` -- register `projects.warningScanSummary` beside the warning queue/dashboard resources.
- `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs:187-275` -- use the shared window; retain existing full inventory totals and top-level unavailable output.
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs:31-162` -- add over-25 deterministic-window/full-total evidence.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs:120-157` and `ProjectsMcpResourceReaderFailureTests.cs:109-190` -- prove scan/Take decoupling, summary visibility with no rows, >25 behavior, and one-snapshot dashboard construction.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpDescriptorTests.cs` and `ProjectsMcpStory511ParityTests.cs` -- lock resource registration and summary contract/documentation coverage.
- `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs:191-273` -- add over-25 deterministic scan and full-total JSON evidence.
- `docs/parity-matrix.md:87-104,124,143-144` and `docs/projection-catalog.md:360-430` -- document the fixed scan window, full-versus-scanned totals, summary resource, one-snapshot dashboard, and output-limit semantics.
- `.bmad-loop/runs/20260828-074849-6ef9/bundles/warning-scan-consistency/intent.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, and `_bmad/render/**` -- read-only workflow evidence; never include in implementation changes.

## Tasks & Acceptance

**Execution:**

- `src/Hexalith.Projects.Client/Diagnostics/ProjectWarningScanWindow.cs` -- implement the shared deterministic selector and named scan limit.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs` and `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs` -- consume the shared selector while preserving full inventory totals and existing safe surface contracts.
- `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs`, `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScan.cs`, `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScanSummaryItem.cs`, and `src/Hexalith.Projects.Mcp/ProjectsMcpDescriptors.cs` -- add the always-emitted summary and restructure scanning around a supplied inventory snapshot with emitted-row limiting after the scan.
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpDescriptorTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpStory511ParityTests.cs`, and `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs` -- cover all I/O matrix cases, exact diagnostic call membership/counts, cancellation/safe output continuity, and full-versus-scanned totals.
- `docs/parity-matrix.md` and `docs/projection-catalog.md` -- make the new public MCP resource and cross-surface cardinality rules discoverable and keep descriptor-documentation parity green.

**Acceptance Criteria:**

- Given the same visible inventory larger than 25, when Web, MCP, and CLI warning/dashboard surfaces run, then each diagnoses exactly the same ordinal-first 25 project ids while every reported visible/lifecycle inventory total reflects the full inventory.
- Given an MCP warning query with `Take` below 25, when it is dispatched, then all projects in the fixed scan window are diagnosed before only the requested number of ordered warning rows is emitted.
- Given a scanned diagnostic failure with no emitted healthy warning row, when MCP warning resources are queried, then the queue remains empty and `projects.warningScanSummary` returns exactly one safe item with the scanned cardinality and unavailable count.
- Given an MCP dashboard request, when visible inventory could change between reads, then the client receives exactly one list request and every dashboard counter is derived from that snapshot and its fixed warning scan.
- Given cancellation or an unsafe diagnostic failure, when any affected surface runs, then cancellation still propagates and successful partial output excludes raw failure or payload detail.

## Spec Change Log

## Review Triage Log

## Design Notes

The shared selector belongs in `Hexalith.Projects.Client` because all three adapters already consume generated `ProjectListItem` instances there. Sorting by `ProjectId` with `StringComparer.Ordinal` before `Take(25)` makes the contract explicit even when tests or alternate clients do not preserve the server projection's existing ordinal ordering. MCP keeps the summary separate from warning rows so zero-row queues remain observable without changing the established warning DTO.

## Verification

**Commands:**

- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds without dependency changes.
- `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore` followed by the built xUnit v3 assembly with `-class Hexalith.Projects.UI.Tests.Diagnostics.ProjectWarningsDashboardSourceTests` -- expected: all focused Web tests pass.
- `dotnet build tests/Hexalith.Projects.Mcp.Tests/Hexalith.Projects.Mcp.Tests.csproj --no-restore` followed by the built xUnit v3 assembly with `-class Hexalith.Projects.Mcp.Tests.ProjectsMcpResourceReaderTests -class Hexalith.Projects.Mcp.Tests.ProjectsMcpResourceReaderFailureTests -class Hexalith.Projects.Mcp.Tests.ProjectsMcpDescriptorTests -class Hexalith.Projects.Mcp.Tests.ProjectsMcpStory511ParityTests` -- expected: all focused MCP tests pass.
- `dotnet build tests/Hexalith.Projects.Cli.Tests/Hexalith.Projects.Cli.Tests.csproj --no-restore` followed by the built xUnit v3 assembly with `-class Hexalith.Projects.Cli.Tests.ProjectsCliApplicationTests` -- expected: all focused CLI tests pass.
- `dotnet build Hexalith.Projects.slnx --no-restore` -- expected: zero warnings and errors.
- `git diff --check` -- expected: no whitespace or conflict-marker errors; ledger, bundle intent, decision evidence, and generated workflow snapshots remain unchanged.

