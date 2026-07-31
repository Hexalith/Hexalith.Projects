---
title: 'Harden partial-failure diagnosticUnavailable parity coverage'
type: 'bugfix'
created: '2026-07-31'
status: 'done'
baseline_commit: '729798ab09cbff27223e06c019a1532865713da1'
review_loop_iteration: 0
context:
  - '{project-root}/docs/parity-matrix.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Partial per-project diagnostic failures are handled by Web, MCP, and CLI, but the regression evidence is uneven: Web source behavior is not proven at the rendered output boundary, and MCP mixed-success behavior is not exercised through resource dispatch. This leaves the `diagnosticUnavailable` parity contract vulnerable to silent regression even though CLI JSON coverage is already strong.

**Approach:** Harden focused tests around one canonical mixed-success fixture—one healthy warning and one unavailable diagnostic—so every warning/dashboard surface visibly preserves healthy facts, reports one unavailable diagnostic, and excludes raw failure detail. Document the surface-specific shapes and close the tracked sprint action only after all three suites pass.

## Boundaries & Constraints

**Always:** Keep enrichment bounded to the visible project set and audit limit 25; preserve healthy warning rows and dashboard facts when one diagnostic fails; expose `diagnosticUnavailable` through the existing Web projection/tile, MCP DTOs, and CLI top-level JSON; retain server-derived tenant scope, canonical freshness vocabulary, cancellation behavior, and payload exclusions.

**Ask First:** Any need to change a public DTO/schema, resource or command name, the meaning of `ProjectsWithWarnings`, production failure mapping, or the approved difference between Web synthetic unavailable rows and MCP/CLI aggregate-count output.

**Never:** Add endpoints, generated-client edits, adapter-local lifecycle/freshness vocabulary, new UI/CSS, unbounded fan-out, raw ProblemDetails/exception output, client-derived tenant authority, or unrelated Story 8 behavior.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Mixed diagnostic result | Two visible projects; one diagnostic returns a healthy warning and one returns 503 | Healthy warning remains; warning and dashboard outputs expose `diagnosticUnavailable = 1`; visible/dashboard totals remain intact | Failed diagnostic is represented only by existing safe count/row semantics |
| Rendered Web result | Healthy warning row plus safe diagnostic-unavailable row | Both rows render; dashboard exposes `Diagnostic unavailable: 1`; safe labels remain accessible | No raw error text reaches markup |
| Safe adapter output | MCP resource dispatch and CLI command serialization use the mixed fixture | Existing tenant, payload-exclusion, warning/reason/freshness fields remain; raw problem body is absent | Surface remains successful because useful partial data exists |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs` and `ProjectWarningsDashboardMapper.cs` -- existing Web per-project failure isolation and safe-row/dashboard aggregation.
- `src/Hexalith.Projects.UI/Components/Pages/Home.razor` -- observable warning queue and `Diagnostic unavailable` dashboard tile.
- `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs` and `ProjectsMcpModels.cs` -- existing resource dispatch, mixed-success scan, and parity fields.
- `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs` -- existing shared warning scan and JSON warning/dashboard envelopes.
- `tests/Hexalith.Projects.{UI,Mcp,Cli}.Tests/**` -- focused observable-boundary regression suites.

## Tasks & Acceptance

**Execution:**

- [x] `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs` -- strengthen the mixed-success source test with exact healthy/unavailable identities, preserved totals, safe reason evidence, and leakage assertions.
- [x] `tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs` -- add bUnit coverage proving the healthy row, safe unavailable row, and `Diagnostic unavailable: 1` tile render together with accessible labels.
- [x] `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs` -- exercise warning and dashboard mixed-success behavior through `QueryAsync<T>` and assert runtime values plus raw-body exclusion.
- [x] `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs` -- retain the canonical mixed-success regression as explicit warning/dashboard JSON parity evidence and strengthen only missing observable assertions.
- [x] `docs/parity-matrix.md` -- state the canonical mixed-success regression and existing Web/MCP/CLI output shapes without redefining their contracts.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- after green verification, mark only the matching action done and record dated results with this spec as evidence.

**Acceptance Criteria:**

- Given one healthy warning and one unavailable diagnostic, when Web, MCP, and CLI warning/dashboard outputs are exercised, then useful facts remain visible and each surface reports exactly one diagnostic unavailable without exposing failure payloads.
- Given the established contracts, when parity tests inspect observable output, then Web proves rendered accessible labels, MCP proves actual resource dispatch, and CLI proves serialized JSON for both commands.
- Given any failed required suite, when tracker closure is considered, then the action remains in progress and the exact blocker is recorded rather than represented as passing.

## Spec Change Log

## Design Notes

Parity means the same safe operational truth, not identical envelopes: Web uses a synthetic unavailable queue row plus a dashboard count, MCP carries the aggregate count on emitted warning resource rows and its dashboard item, and CLI uses a top-level count in both JSON objects. This task freezes those existing shapes; changing no-row semantics or warning-project counting requires a separate approved decision.

## Verification

**Commands:**

- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds.
- `dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore` -- expected: all tests pass.
- `dotnet test tests/Hexalith.Projects.Mcp.Tests/Hexalith.Projects.Mcp.Tests.csproj --no-restore` -- expected: all tests pass.
- `dotnet test tests/Hexalith.Projects.Cli.Tests/Hexalith.Projects.Cli.Tests.csproj --no-restore` -- expected: all tests pass.
- `dotnet tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.Projects.UI.Tests.dll -method Hexalith.Projects.UI.Tests.Diagnostics.ProjectWarningsDashboardSourceTests.SourcePreservesLoadedRowsWhenOneDiagnosticEnrichmentFails -method Hexalith.Projects.UI.Tests.Components.ProjectInventoryPageTests.InventoryRendersHealthyAndUnavailableDiagnosticsWithAccessibleDashboardCounts` -- expected: the named Web matrix passes 2/2.
- `dotnet tests/Hexalith.Projects.Mcp.Tests/bin/Debug/net10.0/Hexalith.Projects.Mcp.Tests.dll -method Hexalith.Projects.Mcp.Tests.ProjectsMcpResourceReaderFailureTests.Query_Warnings_And_Dashboard_Count_Unavailable_Diagnostics_And_Preserve_Healthy_Warnings` -- expected: the named MCP matrix passes 1/1.
- `dotnet tests/Hexalith.Projects.Cli.Tests/bin/Debug/net10.0/Hexalith.Projects.Cli.Tests.dll -method Hexalith.Projects.Cli.Tests.ProjectsCliApplicationTests.Warnings_And_Dashboard_Expose_Story511_Parity_Fields_And_Partial_Failure_Count` -- expected: the named CLI matrix passes 1/1.
- `dotnet build Hexalith.Projects.slnx --no-restore` -- expected: zero warnings and errors.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-07-31):**

- Restore passed; UI passed 152/152 with 0 skipped; MCP passed 26/26 with 0 skipped; CLI passed 16/16 with 0 skipped.
- Named matrix audit passed: the two Web source/rendering tests passed 2/2, MCP `QueryAsync<T>` mixed-success dispatch passed 1/1, and CLI warning/dashboard JSON parity passed 1/1.
- Solution build passed with 0 warnings and 0 errors; `git diff --check` passed.
- Aspire baseline limitation: the AppHost built and the dashboard launch was reported, but the detached process exited before resource-readiness inspection; `aspire describe` subsequently reported no running AppHost. No runtime or AppHost code changed in this coverage-only task.

## Suggested Review Order

**Contract and intent**

- Define the canonical partial-failure truth while preserving intentional surface-specific envelopes.
  [`parity-matrix.md:93`](../../docs/parity-matrix.md#L93)

**Web observable boundary**

- Preserve exact healthy and safe-unavailable projections after one diagnostic returns 503.
  [`ProjectWarningsDashboardSourceTests.cs:93`](../../tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs#L93)

- Render both rows with accessible dashboard counts and safe operator labels.
  [`ProjectInventoryPageTests.cs:60`](../../tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs#L60)

**Adapter outputs**

- Exercise MCP query dispatch while retaining safe metadata and excluding failure details.
  [`ProjectsMcpResourceReaderFailureTests.cs:110`](../../tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs#L110)

- Lock CLI warning and dashboard JSON identities, counts, metadata, and exclusions.
  [`ProjectsCliApplicationTests.cs:192`](../../tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs#L192)

**Evidence and follow-up**

- Reproduce the complete suites and named matrix audit from recorded commands.
  [`spec-partial-failure-diagnosticunavailable-parity-coverage.md:72`](spec-partial-failure-diagnosticunavailable-parity-coverage.md#L72)

- Close only the matching sprint action with dated green evidence.
  [`sprint-status.yaml:209`](sprint-status.yaml#L209)

- Keep review-found production questions outside this coverage-only change.
  [`deferred-work.md:70`](deferred-work.md#L70)
