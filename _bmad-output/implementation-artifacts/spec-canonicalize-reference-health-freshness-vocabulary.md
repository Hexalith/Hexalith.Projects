---
title: 'Canonicalize reference-health freshness vocabulary'
type: 'bugfix'
created: '2026-07-31'
status: 'done'
baseline_commit: 'e5966df5b6d94155c50d0cde600ed24c26833533'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31-freshness-trust-state-vocabulary.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Reference-health rows expose producer-specific freshness synonyms (`trusted`, `fresh`, `current`, and others), so operator-visible meaning can depend on source and merge order instead of the canonical Evidence Freshness State contract.

**Approach:** Introduce one Contracts-owned four-state vocabulary and fail-closed normalizer, apply it only at reference-health and reference-warning boundaries, and render shared human labels while preserving lower-case machine codes and richer diagnostic fields.

## Boundaries & Constraints

**Always:** Emit exactly `current`, `stale`, `rebuilding`, or `unavailable`; match trimmed inputs case-insensitively; preserve inclusion, health, failed-check, reason, diagnostic, authorization, and redaction evidence; keep `FreshnessTrustState` serialized as a string; preserve all pre-existing dirty-tree changes.

**Ask First:** Any need to add aliases beyond the approved table, change producer contracts, widen CLI/MCP enrichment, alter persisted or generated artifacts, or normalize non-reference inventory/detail freshness.

**Never:** Edit OpenAPI/generated clients, producer enums, events, persisted schemas, routes, topology, or top-level project/diagnostic/export freshness; add adapter-local mapping tables; erase `Forbidden`, `Redacted`, or `MixedGeneration` detail.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Current evidence | `trusted`, `fresh`, or `current`, including mixed case/outer whitespace | Machine code `current`; Web label `Current` | N/A |
| Degraded evidence | `stale` or `mixedGeneration` | `stale`; existing diagnostic evidence retained | N/A |
| Rebuild | `rebuilding` | `rebuilding`; Web label `Rebuilding` | N/A |
| Unusable evidence | `unavailable`, `unknown`, `forbidden`, or `redacted` | `unavailable`; dedicated authorization/redaction fields retained | Fail closed |
| Invalid evidence | null, empty, whitespace, punctuation variant, or unrecognized input | `unavailable` | Never pass through arbitrary text |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Contracts/Models/EvidenceFreshnessState*.cs` -- new canonical enum and code/label normalizer.
- `src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs` -- normalize summary input and enforce a fail-closed row value.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs` -- normalize evaluation/conversation inputs before merge.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs` -- normalize reference and diagnostic-unavailable warning rows.
- `src/Hexalith.Projects.UI/Components/Pages/{ProjectDiagnostics,Home}.razor` -- render canonical reference/warning labels only.
- `src/Hexalith.Projects.Cli/{Hexalith.Projects.Cli.csproj,ProjectsCliApplication.cs}` -- reference Contracts and normalize validate/warning rows.
- `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs` -- normalize reference-health/warning resources.
- `docs/{projection-catalog,parity-matrix,event-catalog}.md` and `_bmad-output/planning-artifacts/ux-design-specification.md` -- document mapping, boundary, parity, and textual labels.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.Projects.Contracts/Models/EvidenceFreshnessState.cs`, `EvidenceFreshnessStateCode.cs`, and `src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs` -- add the canonical vocabulary/normalizer and make every row assignment/default fail closed.
- [x] `src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs` and `ProjectWarningsDashboardMapper.cs` -- normalize every approved Web reference/warning construction path without losing dedicated detail.
- [x] `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor` and `Home.razor` -- show exact shared title-case labels for reference/warning freshness, leaving inventory and headers unchanged.
- [x] `src/Hexalith.Projects.Cli/Hexalith.Projects.Cli.csproj`, `ProjectsCliApplication.cs`, and `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs` -- reuse Contracts normalization for reference-health and warning output only.
- [x] `tests/Hexalith.Projects.Contracts.Tests/Models/EvidenceFreshnessStateCodeTests.cs`, `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs`, `tests/Hexalith.Projects.UI.Tests/{Diagnostics/ProjectDetailSourceTests.cs,Diagnostics/ProjectWarningsDashboardSourceTests.cs,Diagnostics/ProjectSafeDiagnosticExportBuilderTests.cs,Components/ProjectDetailPageTests.cs,Components/ProjectInventoryPageTests.cs}`, `tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs`, and `tests/Hexalith.Projects.Mcp.Tests/{ProjectsMcpResourceReaderTests.cs,ProjectsMcpResourceReaderFailureTests.cs}` -- exhaustively cover mapping, merge/detail preservation, labels, export distinction, and cross-surface parity.
- [x] `docs/projection-catalog.md`, `docs/parity-matrix.md`, `docs/event-catalog.md`, and `_bmad-output/planning-artifacts/ux-design-specification.md` -- define canonical output versus producer-local/top-level freshness.
- [x] `_bmad-output/implementation-artifacts/5-5-reference-inventory-health-view.md` and `sprint-status.yaml` -- after all verification passes, close only the matching follow-up/action with dated evidence and preserve unrelated history/changes.

**Acceptance Criteria:**

- Given any approved or invalid source input, when a reference-health or reference-warning row is produced by Web, CLI, or MCP, then its machine value belongs to the canonical four-code set and matches the matrix.
- Given evaluation/conversation enrichment in any existing merge order, when rows merge, then producer synonyms cannot reappear and authorization/redaction/mixed-generation diagnostics remain intact.
- Given Web reference-health and warning rows, when rendered, then visible text is exactly `Current`, `Stale`, `Rebuilding`, or `Unavailable` without relying on color.
- Given safe export and out-of-scope project freshness, when serialized, then nested reference rows are canonical while top-level producer freshness, schemas, and generated artifacts remain unchanged.

## Spec Change Log

- 2026-07-31: Implemented the approved bounded correction and closed the matching Story 5.5 follow-up and sprint action after all verification commands passed.

## Design Notes

Canonical parity means identical semantics for overlapping reference rows, not identical row sets: Web may enrich conversations/context while CLI/MCP continue using bounded diagnostic summaries. Safe export copies already-normalized reference rows and must not own another mapping table.

## Verification

**Commands:**

- `dotnet restore Hexalith.Projects.slnx` -- expected: restore succeeds.
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --no-restore` -- expected: all pass.
- `dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-restore` -- expected: all pass.
- `dotnet test tests/Hexalith.Projects.Cli.Tests/Hexalith.Projects.Cli.Tests.csproj --no-restore` -- expected: all pass.
- `dotnet test tests/Hexalith.Projects.Mcp.Tests/Hexalith.Projects.Mcp.Tests.csproj --no-restore` -- expected: all pass.
- `dotnet build Hexalith.Projects.slnx --no-restore` -- expected: zero errors and warnings.
- `git diff --check` -- expected: no whitespace errors.
- `while IFS= read -r -d '' file; do git -c core.whitespace=cr-at-eol diff --no-index --check -- /dev/null "$file"; status=$?; [ "$status" -le 1 ] || exit "$status"; done < <(git ls-files --others --exclude-standard -z)` -- expected: every untracked file is audited as a new-file diff with no whitespace-error output while honoring required CRLF endings.

**Results (2026-07-31):**

- `dotnet restore Hexalith.Projects.slnx` -- passed.
- Contracts tests -- 188 passed, 0 failed, 0 skipped.
- UI tests -- 151 passed, 0 failed, 0 skipped.
- CLI tests -- 16 passed, 0 failed, 0 skipped.
- MCP tests -- 26 passed, 0 failed, 0 skipped.
- Solution build -- succeeded with 0 warnings and 0 errors.
- Tracked `git diff --check` -- passed.
- Untracked `/dev/null` new-file diff audit -- passed for every untracked file with no whitespace-error output.

## Suggested Review Order

**Canonical boundary**

- Start with the single fail-closed mapping and shared presentation labels.
  [`EvidenceFreshnessStateCode.cs:11`](../../src/Hexalith.Projects.Contracts/Models/EvidenceFreshnessStateCode.cs#L11)

- Enforce canonical values for every reference-health row assignment and default.
  [`ProjectReferenceHealthRowProjection.cs:103`](../../src/Hexalith.Projects.Contracts/Ui/ProjectReferenceHealthRowProjection.cs#L103)

**Source and adapter adoption**

- Normalize context and conversation enrichment before source-precedence merging.
  [`ProjectReferenceHealthMapper.cs:63`](../../src/Hexalith.Projects.UI/Diagnostics/ProjectReferenceHealthMapper.cs#L63)

- Normalize both reference-derived and diagnostic-unavailable warning rows.
  [`ProjectWarningsDashboardMapper.cs:36`](../../src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs#L36)

- Reuse Contracts for CLI validation and warning machine output only.
  [`ProjectsCliApplication.cs:165`](../../src/Hexalith.Projects.Cli/ProjectsCliApplication.cs#L165)

- Reuse the same boundary for MCP reference-health and warning resources.
  [`ProjectsMcpResourceReader.cs:154`](../../src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs#L154)

**Presentation**

- Render canonical textual labels in the project reference matrix.
  [`ProjectDiagnostics.razor:149`](../../src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor#L149)

- Render warning freshness labels without changing inventory freshness.
  [`Home.razor:177`](../../src/Hexalith.Projects.UI/Components/Pages/Home.razor#L177)

**Contract and parity documentation**

- Define complete cross-surface machine-code mapping and scope exclusions.
  [`parity-matrix.md:58`](../../docs/parity-matrix.md#L58)

- Bind exact visible labels and non-color-only communication.
  [`ux-design-specification.md:698`](../planning-artifacts/ux-design-specification.md#L698)

- Close only the approved historical follow-up and sprint action.
  [`5-5-reference-inventory-health-view.md:250`](5-5-reference-inventory-health-view.md#L250)

**Verification and supporting types**

- Exhaustively prove aliases, invalid inputs, labels, and fail-closed enum defaults.
  [`EvidenceFreshnessStateCodeTests.cs:15`](../../tests/Hexalith.Projects.Contracts.Tests/Models/EvidenceFreshnessStateCodeTests.cs#L15)

- Preserve conversation diagnostics across every trust signal and context state.
  [`ProjectDetailSourceTests.cs:108`](../../tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectDetailSourceTests.cs#L108)

- Prove CLI and MCP emit canonical values at their observable boundaries.
  [`ProjectsCliApplicationTests.cs:70`](../../tests/Hexalith.Projects.Cli.Tests/ProjectsCliApplicationTests.cs#L70)

- Keep numeric zero undefined in the canonical four-member enum.
  [`EvidenceFreshnessState.cs:11`](../../src/Hexalith.Projects.Contracts/Models/EvidenceFreshnessState.cs#L11)

- Record unrelated tracker inconsistencies without expanding this correction.
  [`deferred-work.md:65`](deferred-work.md#L65)
