---
title: 'Align warning diagnostic scans across Web MCP and CLI'
type: 'bugfix'
created: '2026-08-28'
status: done
baseline_commit: 311aa85c8e81c7c0b19d5c0004ddf36ceafd5651
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

**Always:** Order the diagnostic window by ordinal project id and cap it at 25; keep the project-window limit distinct from the per-diagnostic audit limit of 25; for every completed scan, set `ScannedProjectCount` to the number of projects selected for attempted diagnosis, including projects whose diagnostics are unavailable, and treat `DiagnosticUnavailable` as a subset of that count; derive lifecycle/inventory totals from every visible row and warning/unavailable totals from the scanned window; preserve cancellation, server-derived tenant scope, metadata-only output, safe failure mapping, warning-row ordering, and existing Web synthetic-unavailable and MCP warning-row shapes.

**Block If:** A deterministic project identifier is unavailable, the new summary cannot use `projects.warningScanSummary` without conflicting with an existing protocol contract, or implementing the change requires modifying generated client artifacts or public REST/domain contracts.

**Never:** Edit the deferred-work ledger, bundle intent, `.bmad-loop` decision evidence, or generated `_bmad/render/**` snapshots; let an implementation handoff edit this workflow-owned spec, add baseline metadata, or mark its tasks complete; expose raw exceptions, ProblemDetails, payloads, or client-derived tenant authority; change warning semantics, maintenance behavior, or unrelated UI markup/styles; let MCP query `Take` affect which projects are diagnosed.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Inventory exceeds window | More than 25 visible projects in non-ordinal input order | All surfaces diagnose the ordinal-first 25 only; full inventory/lifecycle totals still cover every visible project | Projects after the window are not queried and do not affect scan-derived counts |
| Output is smaller than scan | MCP warning query requests fewer rows than the scan produces | MCP diagnoses the fixed 25-project window, sorts all warning rows, then applies query `Take` only to emitted rows | Per-project diagnostic failures still contribute to the full scan summary |
| No healthy warning row | A completed 25-project scan emits no warning rows and one diagnostic is unavailable | Warning queue remains unchanged/empty and `projects.warningScanSummary` still emits one item with `ScannedProjectCount = 25` and `DiagnosticUnavailable = 1` | Unsafe failure detail remains excluded |
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

- [x] `src/Hexalith.Projects.Client/Diagnostics/ProjectWarningScanWindow.cs` -- implement the shared deterministic selector and named scan limit.
- [x] `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs` and `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs` -- consume the shared selector while preserving full inventory totals and existing safe surface contracts.
- [x] `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs`, `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScan.cs`, `src/Hexalith.Projects.Mcp/ProjectsMcpWarningScanSummaryItem.cs`, and `src/Hexalith.Projects.Mcp/ProjectsMcpDescriptors.cs` -- add the always-emitted summary and restructure scanning around a supplied inventory snapshot with emitted-row limiting after the scan.
- [x] `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpDescriptorTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpStory511ParityTests.cs`, and `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs` -- cover all I/O matrix cases, exact diagnostic call membership/counts, cancellation/safe output continuity, and full-versus-scanned totals.
- [x] `docs/parity-matrix.md` and `docs/projection-catalog.md` -- make the new public MCP resource and cross-surface cardinality rules discoverable and keep descriptor-documentation parity green.

**Acceptance Criteria:**

- Given the same visible inventory larger than 25, when Web, MCP, and CLI warning/dashboard surfaces run, then each diagnoses exactly the same ordinal-first 25 project ids while every reported visible/lifecycle inventory total reflects the full inventory.
- Given an MCP warning query with `Take` below 25, when it is dispatched, then all projects in the fixed scan window are diagnosed before only the requested number of ordered warning rows is emitted.
- Given a completed 25-project scan with one unavailable diagnostic and no emitted healthy warning row, when MCP warning resources are queried, then the queue remains empty and `projects.warningScanSummary` returns exactly one safe item with `ScannedProjectCount = 25` and `DiagnosticUnavailable = 1`.
- Given an MCP dashboard request, when visible inventory could change between reads, then the client receives exactly one list request and every dashboard counter is derived from that snapshot and its fixed warning scan.
- Given cancellation or an unsafe diagnostic failure, when any affected surface runs, then cancellation still propagates and successful partial output excludes raw failure or payload detail.

## Spec Change Log

- 2026-09-05: Clarified that `ScannedProjectCount` counts every project selected for attempted diagnosis, including diagnostics recorded as unavailable.
- 2026-10-06: Verified the existing implementation, completed regression coverage and documentation clarification, and recorded independent review plus final Debug/source validation. The parent workflow captured the baseline and marked tasks complete; the intent contract and protected evidence are unchanged.

## Review Triage Log

### 2026-10-06 — Independent review

All three workflow layers completed. Edge Case Hunter returned no findings. The complete baseline diff includes pre-existing P1R work; the initial file-hash snapshot establishes its ownership and preservation.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | low | reject, outside intent | The scheduling guard does not check the optional remediation usability/execution fields, as its validation functions confirm. This is pre-existing P1R guard work, outside the warning-diagnostic consistency intent; required scheduling states and acceptance coordinates remain separately enforced. |
| BH-02 | false | reject | The EventStore checkout/gitlink difference existed at invocation. The P1R documents explicitly disclaim qualification of later source, so historical proof does not implicitly qualify that transition. This run changes no gitlink. |
| BH-03 | low | reject, outside intent | The dated P1R `g6_failures` narrative still uses “Current” for an earlier failed run while the separate qualification gate records later evidence. Clarifying this pre-existing planning narrative is outside warning-scan intent; the snapshotted sprint and context remain unchanged. |
| BH-04 | low | reject, outside intent | The existing P1R handoff supplies captured revision/hash evidence without a complete isolated historical-workspace recipe. Historical packet reproduction is outside this warning-diagnostic task and its files are preserved. |
| BH-05 | low | reject, outside intent | The pre-existing remediation owner-decision clause could state more clearly how rollback coordinates relate to the permitted forward-recovery alternative. That P1R recovery decision is outside warning-scan intent and remains with its existing handoff. |
| BH-06 | false | reject | Actual validation is retained in command outputs and audited by the parent: all 55 focused cases executed with no failures/skips and the broad build passed. Recording the final results in this spec belongs to workflow finalization; review fixes that edit the workflow-owned spec are prohibited. |
| BH-07 | medium | patch | Replacing the former two-project summary fixture with only a 25-project scan removed below-limit cardinality evidence. A hardcoded summary count of 25 would pass the remaining assertions; restore smaller and empty inventories. |
| BH-08 | low | patch | The summary fixture varies neither the unavailable count nor total diagnosis failure. Exercise all selected diagnostics failing and verify the complete unavailable count and safe one-row summary. |
| BH-09 | low | patch | The warning-query fixture emits already ordered reference rows, so removing the warning-row ordering would still pass. Supply reverse reference input and mixed kinds and assert ordered emitted rows. |
| BH-10 | low | patch | Only `Take=1` is exercised against the expanded warning window. Add `Take=40` against 48 matching rows to detect accidental reuse of the 25-project limit as an emitted-row cap. |
| BH-11 | low | patch | The per-surface numeric fixtures do not distinguish ordinal from cultural ordering. Project identifiers are case-sensitive non-whitespace strings; add a focused shared-selector regression whose valid boundary ids sort differently under those comparers. |
| VG-01 | medium | reject, outside intent | The pre-verified dependency-adoption gap is real: Projects' existing `/project` callback calls `ProjectProjectionHandler.Project` directly, bypassing EventStore's versioned-input refusal. That callback and the updated dependency predate this invocation. The warning-scan intent excludes REST/domain changes and leaves this issue with the existing dependency/P1R work; its read-only ledger is not edited. |

Patch entries are BH-07 through BH-11. No intent-gap or bad-spec entry requires re-derivation. Every pre-existing snapshotted file remains byte-identical; no deferred-work entry is appended under this intent's explicit ledger prohibition.

All five patches are complete and verified: the summary theory covers 0/0/0, 2/2/1, 30/25/1, and 30/25/25 visible/scanned/unavailable counts; the warning query asserts ordered mixed-kind/reference rows for `Take=1` and `Take=40`; a new Client selector test proves ordinal selection under `en-US` at the 25-project boundary and restores ambient culture. Parent final validation passes all 60 focused cases.

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

### Completed validation — 2026-10-06

The required behavior was already implemented by the current checkout. This run strengthens acceptance coverage and clarifies selected-project cardinality; it introduces no warning-scan behavior, REST/domain contract, generated-client, maintenance, or UI markup changes. All five tasks and every acceptance criterion are verified.

Local verification uses Debug and explicit source references. `dotnet restore Hexalith.Projects.slnx` passes. A matching `dotnet restore Hexalith.Projects.slnx -p:UseHexalithProjectReferences=true` is needed before source-mode builds; mixing the default restore with source builds reproduced CS1704 duplicate `Hexalith.Commons.UniqueIds` references. Initial default standalone builds failed with missing sibling types, and standalone Aspire builds failed with CS1501 at `FoldersProjectFolderDirectory.cs:66`. These reference-mode failures are preserved in the command logs and resolved by the explicit source mode; no compiler gate or dependency configuration was changed.

Each focused build finishes with zero warnings and errors. Exact successful build and final assembly execution commands:

```text
dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true
dotnet build tests/Hexalith.Projects.Mcp.Tests/Hexalith.Projects.Mcp.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true
dotnet build tests/Hexalith.Projects.Cli.Tests/Hexalith.Projects.Cli.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true
dotnet build tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true
dotnet tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.Projects.UI.Tests.dll -class Hexalith.Projects.UI.Tests.Diagnostics.ProjectWarningsDashboardSourceTests
dotnet tests/Hexalith.Projects.Mcp.Tests/bin/Debug/net10.0/Hexalith.Projects.Mcp.Tests.dll -class Hexalith.Projects.Mcp.Tests.ProjectsMcpResourceReaderTests -class Hexalith.Projects.Mcp.Tests.ProjectsMcpResourceReaderFailureTests -class Hexalith.Projects.Mcp.Tests.ProjectsMcpDescriptorTests -class Hexalith.Projects.Mcp.Tests.ProjectsMcpStory511ParityTests
dotnet tests/Hexalith.Projects.Cli.Tests/bin/Debug/net10.0/Hexalith.Projects.Cli.Tests.dll -class Hexalith.Projects.Cli.Tests.ProjectsCliApplicationTests
dotnet tests/Hexalith.Projects.Client.Tests/bin/Debug/net10.0/Hexalith.Projects.Client.Tests.dll -class Hexalith.Projects.Client.Tests.Diagnostics.ProjectWarningScanWindowTests
```

| Lane | Passed | Failed / skipped / not run |
| --- | --- | --- |
| Web source and mapper | 13 | 0 / 0 / 0 |
| MCP reader, failures, descriptors, parity | 28 | 0 / 0 / 0 |
| CLI application | 18 | 0 / 0 / 0 |
| Shared ordinal selector | 1 | 0 / 0 / 0 |
| Total | 60 | 0 / 0 / 0 |

The first solution build passed during implementation. Parent revalidation after source restore reproduced 153 missing Tenants contract errors with bare `dotnet build Hexalith.Projects.slnx --no-restore`. Explicit source mode builds successfully; a parallel copy retry produced MSB3026 once. The final `dotnet build Hexalith.Projects.slnx --no-restore -p:UseHexalithProjectReferences=true -m:1` passes with zero warnings/errors. All four focused assemblies reran afterward with the results above.

`UseHexalithProjectReferences=true aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --isolated --non-interactive --format Json` succeeds. `aspire describe` reports all 12 resources Running/Healthy; `aspire wait projects --status healthy --timeout 30 --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive` and the corresponding `projects-ui` wait pass. Owned startup is stopped through `aspire stop --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive`; final description confirms no AppHost remains running.

Matrix audit: over-25 membership/full totals is exercised by all three adapters; complete scan before output limiting is exercised by both Take cases; empty queue/one unavailable uses the 25-project theory case; changing consecutive inventories exercises one-list dashboard construction. Cancellation and sanitized partial failure cases pass across all surfaces. The final whitespace check passes, the intent contract is unchanged, all 14 initially snapshotted files retain their bytes, and no generated or protected workflow evidence is edited.

Command outputs and current individual review-fix case receipts are retained under `/tmp/hexalith-warning-scan-20261006-implementation`; parent final commands/results and preservation/matrix audits are under `/tmp/warning-scan-consistency-l0_ltixp`. No release/package qualification is inferred. The existing P1R scheduling/planning observations and versioned projection-admission gap remain outside this warning-scan intent with their existing owner work; no new deferred ledger entry is added.
