---
title: 'Complete warning scan consistency across Web MCP and CLI'
type: 'bugfix'
created: '2026-09-05'
status: 'done'
review_loop_iteration: 0
followup_review_recommended: false
baseline_revision: '2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2'
baseline_commit: '2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2'
context:
  - '{project-root}/docs/parity-matrix.md'
  - '{project-root}/docs/projection-catalog.md'
warnings: [multiple-goals, oversized]
deferred:
  - summary: >-
      Concurrent Story 6.2 review, deferred-work ledger, and submodule updates were committed while the warning-scan bundle was running.
    evidence: |-
      The warning-scan run began from clean baseline 2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2. Commits f4509ef1d0bfe045b0733d8ef2906d3e32eb44eb and 152ca393bb7128d0ff1e5fe54531624af8534506 appeared on main and origin/main during implementation and review; they contain the unrelated Story 6.2 artifact, deferred-work ledger entry, and EventStore/Folders/Parties submodule pointer updates. The warning-scan implementation session did not author those edits, and separating already-published concurrent work requires its owner or orchestrator.
    location: >-
      Repository change set after baseline 2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2
    severity: medium
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

### 2026-09-05 — Review pass
- verdicts: 26 findings — high 0, medium 4, low 5, false 17, maybe-false 0
- findings:
  - `[low]` `[patch]` The warning-scan summary failure test asserted only that unsafe text was absent, so an empty explanation would pass — added a positive `ShouldNotBeNullOrWhiteSpace` assertion and reran the affected MCP tests successfully.
  - `[medium]` `[defer]` The reviewed baseline diff contains a deferred-work ledger edit despite the bundle's read-only constraint — the run started clean, and commit `f4509ef1d0bfe045b0733d8ef2906d3e32eb44eb` containing that edit appeared concurrently on `main` and `origin/main`; it is preserved for the orchestrator rather than rewritten here.
  - `[medium]` `[defer]` The deferred-work ledger changed within the reviewed baseline range — this is the same concurrently published repository mutation evidenced by commit `f4509ef1d0bfe045b0733d8ef2906d3e32eb44eb`, not an edit made by the warning-scan implementation session.
  - `[medium]` `[defer]` The baseline diff also includes an unrelated Story 6.2 artifact and EventStore/Folders/Parties submodule pointer updates — commits `f4509ef1d0bfe045b0733d8ef2906d3e32eb44eb` and `152ca393bb7128d0ff1e5fe54531624af8534506` appeared and were published concurrently, so their separation is deferred to their owner.
  - `[low]` `[reject]` The spec's `git diff --check` command alone cannot prove protected paths stayed unchanged — the proposed correction edits this build's spec and is therefore rejected by the review rules; baseline name/status and commit inspection supplied the missing evidence during this run.
  - `[false]` `[reject]` Separate warning-queue and summary resource reads can observe different live snapshots — they are intentionally independent resource requests with no cross-request snapshot or ETag contract, so normal freshness differences are not an internally inconsistent single operation.
  - `[false]` `[reject]` A non-positive MCP `Take` would be clamped to one item — the production FrontComposer MCP projection reader normalizes `Take` to a positive value before constructing the only public `ProjectionQuery` path.
  - `[false]` `[reject]` MCP warning queue ignores `Skip` — the public FrontComposer caller constructs this resource query with `Take` only and does not expose `Skip` for this dispatch path, so the claimed behavior is unreachable at the adapter surface.
  - `[false]` `[reject]` MCP warning queue ignores filters, search, and sorting — the public caller does not populate those query features for handwritten resource dispatch; fixed domain warning ordering is the documented contract.
  - `[low]` `[reject]` Handwritten MCP descriptors report integer JSON fields with `System.String` as their CLR type and make common fields optional — runtime JSON types are correct, while changing the shared pre-existing descriptor helper would alter every handwritten manifest/fingerprint and is disproportionate to this rarely consumed developer metadata defect.
  - `[false]` `[reject]` Adapter output can be corrupted by a diagnostic response for another project — the authorized server endpoint constructs the returned detail for the requested project id, so the production generated client cannot supply the mismatched identity posited by the finding.
  - `[false]` `[reject]` Blank or duplicate project ids can make the shared selector unstable — server list schema validates project identifiers and the read model is keyed uniquely by project id, excluding those values from the production inventory.
  - `[false]` `[reject]` CLI warning references are nondeterministic within a diagnostic — the server emits folder references first and file/memory references in ordinal id order, and the CLI preserves that deterministic sequence.
  - `[false]` `[reject]` Raw diagnostic reason codes can leak unsafe values through MCP or CLI — the operator-diagnostic schema constrains the value and the server emits the bounded domain reason vocabulary; unsafe exception text remains excluded.
  - `[false]` `[reject]` Undefined numeric enum values can become arbitrary numeric strings — the generated client consumes the server's constrained enum schema, and no production response path emits the malformed numeric enum assumed by the finding.
  - `[false]` `[reject]` Malformed successful diagnostic data can bypass per-project failure isolation — the authorized server and generated schema enforce the mapped response shape, so the hypothetical malformed success is not reachable through the production client.
  - `[low]` `[patch]` The parity matrix claimed CLI exposes the same warning fields as MCP/Web even though its established JSON shape differs — corrected the text to promise the CLI's existing safe fields and shared operational truth, then reran verification.
  - `[false]` `[reject]` The spec marked work complete without recording actual verification results — it was intentionally still `in-review`; this finalization records every executed command and result below.
  - `[false]` `[reject]` Queue and summary can disagree because each performs a scan — as independent resources they intentionally describe their own request-time snapshots, not a shared transaction across calls.
  - `[false]` `[reject]` Sorting the inventory can delay cancellation for an unbounded period — the server list contract caps the inventory at 200 items and cancellation is observed before and during network operations, so the claimed unbounded synchronous delay does not occur.
  - `[low]` `[reject]` The warning-scan descriptor repeats the existing integer/`System.String` metadata mismatch — this is the same shared helper defect already triaged above; its repository-wide manifest change is not justified for this negligible developer-only impact.
  - `[false]` `[reject]` Tests exercise implementation classes rather than the user-visible surfaces requested by the intent — `ProjectWarningsDashboardSource.LoadAsync`, MCP `QueryAsync<T>` plus descriptor registration, and CLI `RunAsync` JSON are the repository's actual adapter boundaries for these behaviors.
  - `[false]` `[reject]` Independent Web, MCP, and CLI fixtures fail to prove cross-surface consistency — each boundary asserts the same ordinal-first membership rule and all three call the single shared selector, which establishes the required parity without a coupled test harness.
  - `[false]` `[reject]` MCP over-25 selection and post-scan `Take` are not proven together — the affected test set separately locks fixed-window membership and a `Take=1` scan that still visits all selected projects and reports the full warning count; one combined test is not required by the intent.
  - `[false]` `[reject]` The always-emitted summary lacks coverage at the named MCP endpoint — descriptor registration and the production resource-reader dispatch are the public MCP projection mechanism, and tests exercise both the registered name and its one-row empty/failure behavior.
  - `[medium]` `[defer]` Unrelated ledger, Story 6.2, and submodule changes violate this bundle's narrow scope — initial sanity was clean and the two published concurrent commits identify the external root cause; the changes are preserved and grouped in frontmatter for owner/orchestrator follow-up.

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

## Auto Run Result

### Summary

Web, MCP, and CLI now use the shared ordinal-first 25-project diagnostic window while retaining full-inventory totals. MCP warning output is limited only after the complete scan, reports the full warning count, exposes the always-emitted safe `projects.warningScanSummary` resource, and builds dashboard totals from one list snapshot.

### Files Changed

- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs` — materializes one complete inventory snapshot and scans only the shared project window.
- `src/Hexalith.Projects.Cli/ProjectsCliApplication.cs` — replaces input-order truncation with the shared deterministic selector.
- `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs` — decouples scan membership from output `Take`, adds summary dispatch, and reuses one dashboard snapshot.
- `src/Hexalith.Projects.Mcp/ProjectsMcpDescriptors.cs` — registers `projects.warningScanSummary`.
- `tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs` — covers over-25 non-ordinal inventory and full totals.
- `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs` — covers consistent selection and full totals for warnings and dashboard commands.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs` — covers full-scan/post-scan-`Take`, ordinal membership, summary, and one-snapshot behavior.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs` — covers safe empty-queue/unavailable summary output and cancellation.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpDescriptorTests.cs` — locks summary resource registration.
- `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpStory511ParityTests.cs` — locks descriptor/documentation parity.
- `docs/parity-matrix.md` — documents shared scan membership, full-versus-scanned totals, and adapter behavior.
- `docs/projection-catalog.md` — documents MCP queue, summary, and dashboard projection semantics.
- `_bmad-output/implementation-artifacts/spec-warning-scan-consistency-2.md` — records the plan, review triage, verification, and final result.

### Review Findings

- Patched: 2 low findings — required a nonblank safe summary explanation and corrected an overbroad CLI field-parity statement.
- Deferred: 1 medium root-cause entry covering 4 findings — unrelated ledger, Story 6.2, and submodule changes were committed and published concurrently during this run.
- Rejected: protected-path verification command gap, because its fix would edit this build's spec and direct commit inspection supplied the evidence.
- Rejected: cross-request queue/summary snapshot divergence (reported twice), because separate resources intentionally observe independent request-time state.
- Rejected: non-positive `Take`, `Skip`, filters/search/sort, because the public FrontComposer resource path normalizes or does not expose those inputs.
- Rejected: integer descriptor CLR metadata mismatch (reported twice), because it is a low-impact pre-existing shared-helper issue whose fix would churn all handwritten manifests.
- Rejected: mismatched diagnostic identity, blank/duplicate project ids, nondeterministic CLI reference order, arbitrary reason codes, numeric enum values, and malformed successful payloads, because server authorization/schema/read-model invariants exclude each hypothesized production input.
- Rejected: missing verification results, because results belong to and are recorded by this finalization phase.
- Rejected: unbounded selector cancellation delay, because inventory is contract-capped at 200 and network cancellation remains propagated.
- Rejected: insufficient public-surface, shared-fixture, combined-scenario, and named-endpoint coverage, because the tested UI source, MCP descriptor/reader, and CLI JSON are the actual adapter boundaries and collectively exercise each acceptance condition through the shared selector.
- Follow-up review recommendation: `false`; patched counts are high 0, medium 0, low 2, so the workflow threshold is not met.

### Verification Performed

- `dotnet restore Hexalith.Projects.slnx` — succeeded; all projects were up to date.
- UI test project build plus `ProjectWarningsDashboardSourceTests` — build succeeded with zero warnings/errors; 13/13 tests passed.
- MCP test project build plus the four affected reader/failure/descriptor/parity classes — build succeeded with zero warnings/errors; 23/23 tests passed.
- CLI test project build plus `ProjectsCliApplicationTests` — build succeeded with zero warnings/errors; 14/14 tests passed.
- `dotnet build Hexalith.Projects.slnx --no-restore` — succeeded with zero warnings and zero errors.
- `git diff --check` and `git diff --cached --check` — succeeded.
- Matrix audit — over-25 membership/full totals, post-scan `Take`/full count, empty queue with safe unavailable summary, and one-snapshot dashboard all have executable coverage.

### Residual Risks

The repository history since the baseline includes unrelated changes that another process committed and pushed during this run, including the deferred-work ledger. They were not authored or reverted by this implementation. The pre-existing handwritten MCP descriptor helper also retains its low-impact CLR type-name inconsistency while emitting the correct JSON types.
