---
title: 'Fix all Hexalith.Projects test failures'
type: 'bugfix'
created: '2026-07-14'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '63033124c7a57d659842ba75abb9747d3be69cb9'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The complete Hexalith.Projects-owned test surface has not been proven green in the current workspace, and any reproducible failures may represent product, test, generated-artifact, or tooling regressions.

**Approach:** Establish a clean baseline for every root-owned .NET test project, generated-artifact gate, and Playwright check; diagnose each failure at its owning boundary, implement the smallest correct fix, and rerun focused plus broad gates until all executable tests pass.

## Boundaries & Constraints

**Always:** Preserve all pre-existing dirty-worktree changes; run the eight .NET test projects individually; distinguish product defects from environment blockers; keep warnings-as-errors, nullable checks, tenant isolation, redaction, generated-source ownership, and existing test strength intact; use the repository's pinned toolchain and root-declared submodules as checked out.

**Ask First:** Halt before changing any `references/*` submodule source, intentionally versioned snapshot/public-API/OpenAPI baseline, package version, or user-authored change whose ownership cannot be separated from a fix.

**Never:** Run recursive submodule initialization; revert, reset, clean, or overwrite existing work; edit generated `.g.cs` output directly; weaken assertions or build settings; convert documented `test.fixme`/skipped scenarios into passing no-ops; treat submodule-owned suites or archived E2E backups as Hexalith.Projects test targets.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Green lane | Root-owned test/gate executes with prerequisites | Lane passes without source changes | Record command and result |
| Reproducible failure | Test fails consistently in a focused rerun | Owning production/test/tooling defect is fixed and regression remains covered | Preserve diagnostics; rerun focused then broad lane |
| Environment blocker | Restore, browser, container, or service prerequisite is unavailable | Exhaust fallback validation and report exact blocked command separately | Do not alter product behavior to hide the blocker |
| Dirty-file overlap | Required fix overlaps an existing change | Existing intent is preserved and only separable edits proceed | Halt for approval if separation is unsafe |

</frozen-after-approval>

## Code Map

- `Hexalith.Projects.slnx` -- exhaustive root build graph, including all eight owned test projects.
- `Hexalith.Projects.CI.slnx` -- seven-project CI build graph and Release parity baseline.
- `tests/Hexalith.Projects.*.Tests/` -- Contracts, Client, domain, Server, UI, MCP, CLI, and Integration test owners.
- `src/Hexalith.Projects.*/` -- production ownership boundaries for fixes discovered by tests.
- `tests/tools/run-frontcomposer-inspect-gate.ps1` -- generated FrontComposer staleness gate.
- `tests/tools/run-openapi-fingerprint-gate.ps1` -- OpenAPI/generated-client compatibility gate.
- `tests/e2e/` -- Node 24+ TypeScript and Playwright workspace; documented `test.fixme` scenarios remain intentional.

## Tasks & Acceptance

**Execution:**
- [ ] `Hexalith.Projects.slnx` -- restore and build the exhaustive graph, recording compiler or restore failures before test triage.
- [ ] `tests/Hexalith.Projects.*.Tests/*.csproj` -- run all eight projects individually, reproduce every failure narrowly, and fix the defect in its owning `src/` or test-support path.
- [ ] `tests/tools/run-frontcomposer-inspect-gate.ps1` and `tests/tools/run-openapi-fingerprint-gate.ps1` -- run both artifact gates and correct source/input drift without hand-editing generated output.
- [ ] `tests/e2e/` -- install locked dependencies, type-check, install/use an available browser, and run the full active Playwright suite.
- [ ] `_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md` -- record implementation evidence and final verification state through the workflow.

**Acceptance Criteria:**
- Given the pinned toolchain and current authorized dirty worktree, when all eight root-owned .NET test projects run individually, then every executable test passes and intentional skips are reported without being weakened.
- Given the repository contract and FrontComposer inputs, when both generated-artifact gates run, then both exit successfully with no unapproved baseline mutation.
- Given locked E2E dependencies and an available browser, when TypeScript checking and the active Playwright suite run, then both pass and documented `test.fixme` cases remain explicitly skipped.
- Given any environment-blocked broad gate, when fallback validation is exhausted, then the exact blocker and all successful focused evidence are reported separately.

## Spec Change Log

## Design Notes

Submodule suites are excluded because each `references/*` directory is a separately versioned repository with its own test policy. Their sources participate only as Hexalith.Projects dependencies; a root failure that proves a submodule change is necessary triggers the Ask First boundary.

## Verification

**Commands:**
- `dotnet restore Hexalith.Projects.slnx && dotnet build Hexalith.Projects.slnx --no-restore --configuration Release -warnaserror` -- expected: exhaustive graph builds cleanly.
- `dotnet test <each-owned-test-csproj> --no-build --configuration Release` -- expected: all eight projects pass individually.
- `pwsh ./tests/tools/run-frontcomposer-inspect-gate.ps1 && pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` -- expected: both artifact gates pass.
- `npm --prefix tests/e2e ci && npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e test` -- expected: type-check and all active Playwright tests pass.
