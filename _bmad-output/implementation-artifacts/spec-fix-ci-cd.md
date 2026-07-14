---
title: 'Fix CI/CD without changing submodule pointers'
type: 'bugfix'
created: '2026-07-14'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'b89cb8f9f2ee7d1c4a2965cd9c317aae083b224d'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/analysis/hexalith-projects-codebase-audit-2026-07-14.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current CI fails with 221 build errors because Conversations resolves Commons through an intentionally deinitialized nested path, while Release fails because the FrontComposer gate inspects missing Debug output. The pipelines also omit Integration and E2E lanes, pin Dapr below the supported baseline, race CI with an independent release, use mutable third-party action refs, and can package external project dependencies at the Projects release version.

**Approach:** Repair the automation entirely in root-owned files: route builds to the root-declared Commons checkout, make artifact gates self-building in Release, include existing Integration and nightly E2E coverage, align Dapr, pin third-party actions, sequence release behind the exact tested main SHA, and fail packaging when dependency versions are corrupted.

## Boundaries & Constraints

**Always:** Snapshot root gitlink index entries and every root-submodule worktree SHA before edits; preserve both throughout. Keep CI Release-only, warnings-as-errors, individual test-project execution, generated-artifact gates, locked npm installs, least-privilege job permissions, and the documented Hexalith.Builds `@main` exception.

**Ask First:** Halt before changing any `references/*` source, gitlink, checkout SHA, package publication secret, public dependency contract, or remote workflow state; halt if a correct fix requires nested submodules or a wider package-only migration.

**Never:** Initialize nested submodules; use recursive submodule commands, `submodules: true`, mutable third-party action tags, unsafe npm lifecycle scripts, weakened tests/build flags, duplicate push-triggered releases, or publish packages whose external dependencies inherit the Projects version.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Clean CI checkout | Root-declared submodules only; nested Commons absent | Restore/build resolves root Commons and passes | Fail with actionable dependency-routing evidence |
| Artifact gates | No prior Debug output | Gates build/inspect Release deterministically | Fail on warnings or fingerprint drift |
| Event routing | PR, schedule, or main push | Test PR/schedule; release only tested main push SHA | Never cancel an active main release |
| E2E install | Dependency has recursive postinstall | Lifecycle scripts cannot execute; browsers install explicitly | Upload failure evidence without changing Git state |
| Package preparation | Projects version differs from external modules | Internal dependencies use release version; external dependencies retain their own versions | Block publication before NuGet push |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml` -- CI, generated gates, nightly E2E, and exact-SHA release orchestration.
- `.github/workflows/release.yml` -- current duplicate push-triggered release to retire.
- `Hexalith.Projects.CI.slnx` -- Release build graph; currently omits AppHost and Integration.Tests.
- `tests/tools/run-frontcomposer-inspect-gate.ps1` -- currently assumes Debug generated output.
- `tests/tools/run-openapi-fingerprint-gate.ps1` -- must run the compatibility owner in Release.
- `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` -- external project-reference version isolation for packaging.
- `release.config.cjs` -- package preparation and pre-publish dependency validation.
- `tests/e2e/` -- locked nightly Playwright lane and safe setup guidance.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/ci.yml`, `.github/workflows/release.yml` -- consolidate exact-SHA CI/release orchestration; pin third-party actions; use explicit nonrecursive root initialization, Dapr 1.18, root Commons routing, Integration, and scheduled E2E.
- [x] `Hexalith.Projects.CI.slnx` -- add AppHost and Integration.Tests so shared `--no-build` test execution has complete Release outputs.
- [x] `tests/tools/run-frontcomposer-inspect-gate.ps1`, `tests/tools/run-openapi-fingerprint-gate.ps1` -- make both gates self-contained, Release-configured, and warning/drift blocking.
- [x] `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj`, `release.config.cjs`, `tests/tools/` -- isolate external project versions and add a pre-publish package dependency gate.
- [x] `tests/e2e/README.md` -- document the lifecycle-script-safe locked install used by CI.
- [x] `tests/tools/run-ci-workflow-gates.ps1`, `tests/tools/run-package-dependency-gate.ps1` -- cover event routing, immutable refs, forbidden recursive patterns, package dependency versions, and every matrix row.

**Acceptance Criteria:**
- Given a clean root-only checkout, when CI restores, builds, runs all eight .NET test projects and artifact gates, then every lane passes without nested initialization.
- Given a scheduled run, when E2E executes, then locked dependencies/typecheck/active Playwright tests pass and intentional skips remain explicit.
- Given a main push, when all required jobs pass, then one non-cancellable release handles that exact SHA with write permissions scoped to the release job.
- Given a simulated release version, when packages are prepared, then all five packages restore and external dependency versions are not rewritten to the Projects version.
- Given pre/post repository snapshots, when implementation completes, then no gitlink, submodule worktree SHA, nested initialization state, or `references/*` content changed.

## Spec Change Log

- 2026-07-14: Implemented all root-owned workflow, solution, artifact-gate, package-gate, and E2E documentation changes. Verification stopped fail-closed at the approved Ask First boundary because `Hexalith.Conversations.Contracts` is not published to NuGet.org, so the five prepared packages cannot yet restore as package consumers.

## Design Notes

CI remains explicit source mode because Conversations/Folders do not yet provide the complete package/version availability needed for package-only CI. The package dependency gate prevents unsafe publication until that larger migration is separately authorized. Hexalith.Builds refs remain `@main` per its internal-action policy; every third-party action uses a reviewed full SHA.

NuGet's `_GetProjectReferenceVersions` target re-evaluates project references from `project.assets.json` and does not honor `ProjectReference.GlobalPropertiesToRemove` when querying package versions. Release therefore passes `HexalithProjectsPackageVersion`, which the root `Directory.Build.props` translates to `Version`/`PackageVersion` only for Projects-owned projects. Internal Projects dependencies share the release version; sibling modules retain their own package versions.

## Verification

**Commands:**
- `actionlint .github/workflows/*.yml` -- expected: valid workflow syntax and reusable inputs.
- `CI=true HexalithCommonsRoot="$PWD/references/Hexalith.Commons" dotnet restore Hexalith.Projects.CI.slnx && dotnet build Hexalith.Projects.CI.slnx --no-restore -c Release -warnaserror` -- expected: clean root-only build.
- `dotnet test <each of eight test projects> --no-build -c Release` -- expected: every project passes individually.
- `HexalithCommonsRoot="$PWD/references/Hexalith.Commons" pwsh tests/tools/run-frontcomposer-inspect-gate.ps1 && pwsh tests/tools/run-openapi-fingerprint-gate.ps1` -- expected: both real gates pass.
- `CI=1 npm --prefix tests/e2e ci --ignore-scripts && npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e test -- --workers=1` -- expected: active E2E passes without lifecycle scripts.
- `pwsh tests/tools/run-package-dependency-gate.ps1` -- expected: simulated packages have correct internal/external dependency versions and restore.
- `rg -n 'uses:.*@(main|master|v[0-9]+)|recursive|submodules:\s*(true|recursive)' .github/workflows` -- expected: only approved Hexalith.Builds `@main`; no recursive checkout.
- Gitlink/worktree/nested audit -- expected: pre/post values identical and zero initialized nested submodules.

**Evidence (2026-07-14):**

- `actionlint .github/workflows/*.yml` -- passed with no diagnostics.
- `pwsh tests/tools/run-ci-workflow-gates.ps1` -- passed; validated one workflow file, exact-SHA release routing, immutable third-party actions, Dapr 1.18, all eight individual test projects, safe scheduled E2E, Release artifact gates, and no recursive submodule patterns.
- `pwsh tests/tools/run-package-dependency-gate.ps1` -- root-Common-routed Release restore/build passed with 0 warnings and 0 errors; five packages were created at simulated version `91.92.93-ci.1`; internal Projects dependencies used the simulated version and external dependencies retained their own versions. Consumer restore then failed closed with `NU1101` because `Hexalith.Conversations.Contracts` is not available in the five-package output or on NuGet.org. Correct closure requires the separately authorized wider package publication/migration; the gate was not weakened.
- Full individual tests, artifact gates, and safe E2E were not rerun after the package restore boundary because the approved spec requires stopping at Ask First.
- `git diff --check` -- passed.
- Preservation audit -- all ten root gitlink index entries and all root-submodule worktree SHAs/branches match the pre-edit snapshot; all ten submodule worktrees are internally clean; initialized nested submodule count remains zero.
