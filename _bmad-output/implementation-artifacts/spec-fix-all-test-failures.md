---
title: 'Fix all Hexalith.Projects test failures'
type: 'bugfix'
created: '2026-07-14'
status: 'done'
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
- [x] `Hexalith.Projects.slnx` -- restore and build the exhaustive graph, recording compiler or restore failures before test triage.
- [x] `tests/Hexalith.Projects.*.Tests/*.csproj` -- run all eight projects individually, reproduce every failure narrowly, and fix the defect in its owning `src/` or test-support path.
- [x] `tests/tools/run-frontcomposer-inspect-gate.ps1` and `tests/tools/run-openapi-fingerprint-gate.ps1` -- run both artifact gates and correct source/input drift without hand-editing generated output.
- [x] `tests/e2e/` -- install locked dependencies, type-check, install/use an available browser, and run the full active Playwright suite.
- [x] `_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md` -- record implementation evidence and final verification state through the workflow.

**Acceptance Criteria:**
- Given the pinned toolchain and current authorized dirty worktree, when all eight root-owned .NET test projects run individually, then every executable test passes and intentional skips are reported without being weakened.
- Given the repository contract and FrontComposer inputs, when both generated-artifact gates run, then both exit successfully with no unapproved baseline mutation.
- Given locked E2E dependencies and an available browser, when TypeScript checking and the active Playwright suite run, then both pass and documented `test.fixme` cases remain explicitly skipped.
- Given any environment-blocked broad gate, when fallback validation is exhausted, then the exact blocker and all successful focused evidence are reported separately.

## Spec Change Log

- 2026-07-14: Executed the complete root-owned test surface; migrated Projects MCP callers/tests to canonical `ProjectionQuery` criteria, restored the UI shell-options namespace import, aligned the Memories route assertion with the authoritative v1 route, and recorded dependency-routing fallback evidence, E2E install-script guardrail recovery, and final gitlink verification.
- 2026-07-14: Addressed adversarial-review verification feedback with focused inventory and warning-queue success-path tests for non-default canonical `ProjectionQuery.Take`; the warning queue now applies that bound to its final returned rows, and all broad gates were rerun.

## Design Notes

Submodule suites are excluded because each `references/*` directory is a separately versioned repository with its own test policy. Their sources participate only as Hexalith.Projects dependencies; a root failure that proves a submodule change is necessary triggers the Ask First boundary.

## Verification

**Commands:**
- `dotnet restore Hexalith.Projects.slnx && dotnet build Hexalith.Projects.slnx --no-restore --configuration Release -warnaserror` -- **BLOCKED**. Restore exited successfully after reporting absent nested `Hexalith.Conversations/references/Hexalith.Commons` projects. The Release build exited 1 with 221 errors because `Hexalith.Conversations.Contracts` could not resolve `Hexalith.Commons.Serialization` (and restore also reported the absent `Hexalith.Commons.Http` project). Initializing nested submodules or editing the owning submodule is outside the approved boundary.
- `dotnet restore Hexalith.Projects.slnx -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons && dotnet build Hexalith.Projects.slnx --no-restore --configuration Release -warnaserror -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons` -- **PASSED fallback**, exhaustive restore and Release build completed with 0 warnings and 0 errors using the checked-out root-declared Commons submodule. The first fallback build exposed the obsolete MCP criteria access and missing UI options import; a second exposed obsolete test constructors; the guarded build passed after the root-owned fixes and was rerun successfully against the final externally advanced Folders/Memories worktree SHAs.
- `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --no-build --configuration Release` -- **PASSED**, 164 passed, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj --no-build --configuration Release` -- **PASSED**, 53 passed, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --no-build --configuration Release` -- **PASSED**, 584 passed, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --no-build --configuration Release` -- **PASSED**, 502 passed, 0 failed, 0 skipped. The rebuilt lane initially found one stale `/api/tenants/...` assertion; the test now follows the authoritative Memories `/api/v1/tenants/...` route.
- `dotnet test tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --no-build --configuration Release` -- **PASSED**, 140 passed, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Projects.Mcp.Tests/Hexalith.Projects.Mcp.Tests.csproj --no-build --configuration Release` -- **PASSED**, 23 passed, 0 failed, 0 skipped. Focused inventory and warning-queue `QueryAsync` tests prove that a non-default canonical `ProjectionQuery.Take` bounds returned results; the warning-queue test covers multiple warning rows per project.
- `dotnet test tests/Hexalith.Projects.Cli.Tests/Hexalith.Projects.Cli.Tests.csproj --no-build --configuration Release` -- **PASSED**, 13 passed, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --no-build --configuration Release` -- **PASSED**, 17 passed, 0 failed, 0 skipped. The final post-review eight-project rerun against the current Folders/Memories worktrees totaled 1,496 passed, 0 failed, 0 skipped.
- `pwsh ./tests/tools/run-frontcomposer-inspect-gate.ps1` -- **PASSED**. The real gate inspected 12 annotated inputs and 22 generated files with 0 warnings and 0 errors.
- `pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` -- **BLOCKED**. Its filtered Debug `dotnet test` rebuild reached the same absent nested `Hexalith.Commons.Serialization` dependency and exited 1; the script's generic nonzero-exit message reported fingerprint drift even though the build failed before the filtered assertions ran.
- `./tests/Hexalith.Projects.Client.Tests/bin/Release/net10.0/Hexalith.Projects.Client.Tests -class Hexalith.Projects.Client.Tests.ClientGenerationTests` -- **PASSED fallback**, 38 passed, 0 failed, 0 skipped. This directly ran the xUnit v3 fingerprint/provenance owner and proved the checked-in OpenAPI spine, generated client, and helper fingerprints are current without modifying generated files or baselines.
- `HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons pwsh ./tests/tools/run-openapi-fingerprint-gate.ps1` -- **PASSED fallback**, the real Debug fingerprint/compatibility gate rebuilt and ran 38 tests with 0 failures or skips; generated artifacts match the Contract Spine fingerprint.
- `npm --prefix tests/e2e ci` -- **INTERRUPTED for safety**. The `@seontechnologies/playwright-utils@4.4.0` postinstall script ran `git submodule update --init --recursive --force` after Git resolved the umbrella repository from `node_modules`, accidentally initializing nested submodules. The process was interrupted; all accidentally initialized nested worktrees were deinitialized as required, and four detached root-submodule HEADs were reattached to their existing `main` refs only after confirming that `HEAD == refs/heads/main`.
- `CI=1 npm --prefix tests/e2e ci` -- **PASSED safe fallback**, 252 locked packages installed. The inspected package script skips its recursive Git command when `CI` is non-empty. npm reported 3 moderate transitive audit findings; no package or lockfile was changed.
- `npm --prefix tests/e2e run typecheck` -- **PASSED** with `tsc --noEmit`.
- `npm --prefix tests/e2e test` -- **PASSED**, 18 active Chromium tests passed and 61 documented `test.fixme` scenarios remained explicitly skipped.

**Repository preservation:**

- Intended root-owned changes are limited to `src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs`, `src/Hexalith.Projects.UI/Program.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs`, `tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs`, `tests/Hexalith.Projects.Server.Tests/ProjectMemoryDirectoryTests.cs`, and this Verification record. No generated file, snapshot, baseline, package/lockfile, package version, or submodule source was edited by this session.
- A concurrent external workspace update advanced root `HEAD`/`origin/main` from the recorded baseline `63033124c7a57d659842ba75abb9747d3be69cb9` to `4fc7fa51a1705161675c715fc603b6286a3e4c01` (`feat: BMAD 6.10.1`) during verification, committing the pre-existing dirty BMAD changes and updating root gitlink index entries. This session did not stage, commit, push, or move any root gitlink worktree SHA and did not revert the external update.
- Final nested-submodule audit returned `initialized_nested=0`; every root-declared submodule is attached to `main`.
- Eight root-declared submodule worktree SHAs remained identical from pre-edit through final audit:
  - `references/Hexalith.AI.Tools`: `d632475f81196e744b55e435d5e91d82a296d7eb`
  - `references/Hexalith.EventStore`: `52897b1fa3678e4053772989c0bb0088d82429a6`
  - `references/Hexalith.Tenants`: `3b029781bfdc2aedc0c3006824e05494570641d8`
  - `references/Hexalith.FrontComposer`: `6188288a0ccdf3394389019b732d630f25726925`
  - `references/Hexalith.Conversations`: `432d4dd7bf01d5295b32e68e13c5a28ffb10da09`
  - `references/Hexalith.Parties`: `a35b1515b81d00dd9c58de5988a30f3c620d3d60`
  - `references/Hexalith.Commons`: `b03469b13408530bb757d3d02279c2d772ee4848`
  - `references/Hexalith.Builds`: `7b02a7a2ba4cf1f27cba7d5b79cc855a9165f402`
- Two clean `main` worktrees were fast-forwarded by concurrent external `pull --tags origin main` commands after the pre-edit snapshot; per instruction they were not restored or moved:
  - `references/Hexalith.Folders`: root index/pre-edit worktree `4ed58e12e98196b7f660239612f33c372c77fa47`; final worktree `3af63e3336fe26261b6c2b0852b8ca80e45c0054`; reflog time `2026-07-14 10:37:51 +0200`.
  - `references/Hexalith.Memories`: root index/pre-edit worktree `d40b93a8257bbd302550011abc1329f2778374ba`; final worktree `e574c313ae067b72ac7f69dd3061099c1a425466`; reflog time `2026-07-14 10:38:32 +0200`.
- Consequently the final root diff reports unstaged gitlink worktree differences for Folders and Memories even though the root index pointers were not changed or staged by this session. The final guarded build and all eight .NET test lanes passed after both external fast-forwards.

## Suggested Review Order

**Canonical query compatibility**

- Read pagination from FrontComposer's composed criteria at the MCP boundary.
  [`ProjectsMcpResourceReader.cs:30`](../../src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs#L30)

- Bound final warning rows after per-project diagnostics expand the result set.
  [`ProjectsMcpResourceReader.cs:295`](../../src/Hexalith.Projects.Mcp/ProjectsMcpResourceReader.cs#L295)

**Dependency contract alignment**

- Import the relocated shell options without changing UI startup composition.
  [`Program.cs:3`](../../src/Hexalith.Projects.UI/Program.cs#L3)

- Match the generated Memories client's authoritative versioned case route.
  [`ProjectMemoryDirectoryTests.cs:49`](../../tests/Hexalith.Projects.Server.Tests/ProjectMemoryDirectoryTests.cs#L49)

**Regression coverage**

- Prove non-default inventory limits through canonical `QueryRequest` composition.
  [`ProjectsMcpResourceReaderTests.cs:60`](../../tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs#L60)

- Prove warning limits apply after multiple rows fan out per project.
  [`ProjectsMcpResourceReaderTests.cs:92`](../../tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderTests.cs#L92)

- Keep failure-path requests on the current FrontComposer factory contract.
  [`ProjectsMcpResourceReaderFailureTests.cs:47`](../../tests/Hexalith.Projects.Mcp.Tests/ProjectsMcpResourceReaderFailureTests.cs#L47)

**Verification and follow-up**

- Review full gate results, environment fallbacks, and submodule preservation evidence.
  [`spec-fix-all-test-failures.md:74`](spec-fix-all-test-failures.md#L74)

- Track unrelated concurrent-update findings without expanding this fix's scope.
  [`deferred-work.md:1`](deferred-work.md#L1)
