---
title: 'Repair CI/CD and publish the first Projects release'
type: 'bugfix'
created: '2026-09-19'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-ci-cd-2.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current `main` cannot obtain the green CI required by Release: the first gate has 25 BMAD Loop relay failures, and package-only restore is blocked because four pinned Conversations/Folders `1.0.0` dependencies are absent from NuGet.org. No Projects package, release configuration, or `production` environment exists.

**Approach:** Restore the relay's previously tested portable filename validation, make every prerequisite package available through its owning repository's guarded release path, prove Projects in Release/package-only mode, then push the reviewed fix, wait for exact-source CI, temporarily authorize publication, dispatch the manual release, and verify the immutable outputs before refreezing.

**Decision:** Execute the recommended end-to-end chain: repair and release Conversations, then Folders, then Projects. This authorizes the irreversible publication of their complete manifest inventories: 14 NuGet packages plus matching tags and GitHub releases across the three repositories.

## Boundaries & Constraints

**Always:** Work and commit from each owning repository; preserve user changes and gitlinks until the owning-repository commit is complete. Keep CI Release/package-only, retain tests and exact-source admission, validate Conventional Commits with pinned commitlint, publish only absent manifest inventories, configure `production` for `main`, enable publication only for each release window, and refreeze afterward.

**Never:** Initialize nested submodules; weaken tests, package gates, source freshness, package-only mode, or duplicate checks; publish outside guarded Release workflows; blind-retry partial publication; alter historical/signed evidence; or publish an occupied version.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Invalid relay component | Unsafe/oversized task ID or event name | Exit zero without creating an events directory or file | Silent no-op, preserving hook behavior |
| Valid first release | Exact green live `main`, absent `1.0.0` destinations, publication enabled | Manifest packages, symbols, `v1.0.0`, and GitHub release originate from the dispatched SHA | Stop before writes on any preflight mismatch |
| Frozen/stale source | Variable is not exact `true`, source moved, or exact CI proof is absent | No publication | Frozen run may skip; stale/invalid source fails |
| Partial publication | One immutable artifact exists before a later failure | Preserve logs and state; do not blind-rerun | Reconcile through a separately validated recovery path |

</frozen-after-approval>

## Code Map

- `.bmad-loop/bmad_loop_hook.py` -- installed hook relay; BMAD `6.12.0` update removed the tested filename-component guard added in `14dea93f`.
- `tests/tools/test_bmad_loop_hook.py` -- reproduces the live failure and defines accepted task/event grammar and boundary lengths; do not weaken.
- `.github/workflows/ci.yml` -- local policy, shared package-only CI, and generated-artifact gates.
- `Directory.Build.props`, `Directory.Packages.props`, `Hexalith.Projects.CI.slnx` -- enforce package-only CI; the four `1.0.0` pins currently resolve to NuGet 404.
- `.github/workflows/release.yml`, `release.config.cjs`, `tools/release-packages.json` -- exact-green-main manual release and authoritative five-package inventory.
- `references/Hexalith.Conversations/_bmad-output/implementation-artifacts/spec-publish-conversations-nuget-packages.md` -- existing four-package prerequisite release plan.
- `references/Hexalith.Folders/.github/workflows/{ci,release}.yml` and `references/Hexalith.Folders/_bmad-output/implementation-artifacts/spec-folders-ci-cd-reference-alignment.md` -- implemented five-package release path whose current CI remains red.

## Tasks & Acceptance

**Execution:**
- [ ] `.bmad-loop/bmad_loop_hook.py` -- restore bounded portable ASCII component validation before payload parsing or filesystem writes.
- [ ] Prerequisite owning repositories -- make exact-source CI green, validate first-release candidates, publish manifest inventories at `1.0.0`, verify official NuGet/GitHub state, and refreeze.
- [ ] Projects gates -- run workflow policy, package-only restore/build, manifest/consumer checks, and every configured test project individually; fix only newly exposed root-owned defects.
- [ ] Projects Git/GitHub state -- validate/commit/push current `main`, wait for exact-source CI, configure the main-only environment, unfreeze, and dispatch once.
- [ ] Release evidence -- monitor to terminal success, verify five Projects `1.0.0` NuGet packages plus symbols and matching GitHub tag/release/source SHA, then refreeze and record links/results.

**Acceptance Criteria:**
- Given every unsafe relay component in the regression matrix, when the hook runs, then it exits successfully without any filesystem publication.
- Given prerequisite `1.0.0` dependencies are public, when Projects CI runs at the pushed `main` SHA, then all blocking jobs pass in Release/package-only mode.
- Given exact-source CI success and empty destinations, when Release is dispatched, then exactly the five manifest Projects packages publish at `1.0.0` and the matching tag/release targets that SHA.
- Given terminal release completion, when official NuGet and GitHub APIs are queried, then all expected artifacts are observable and publication is frozen again.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The dependency releases are ordered prerequisites, not a source-mode fallback: Projects directly consumes the four absent packages, and its candidate consumer gate independently restores `Hexalith.Conversations.Contracts` from NuGet.org. The release workflow otherwise completes as a frozen no-op until the repository variable is explicitly enabled.

## Verification

**Commands:**
- `python3 -m unittest tests/tools/test_bmad_loop_hook.py -v` -- all relay cases pass.
- `python3 -m unittest tests/tools/test_release_package_tools.py -v && pwsh ./tests/tools/run-ci-workflow-gates.ps1` -- local CI/release policy gates pass.
- `CI=true dotnet restore Hexalith.Projects.CI.slnx -p:Configuration=Release -m:1 && CI=true dotnet build Hexalith.Projects.CI.slnx --no-restore --configuration Release -warnaserror -m:1` -- package-only graph succeeds.
- `dotnet test <each configured test project> --configuration Release --no-build` -- every blocking shard passes individually.
- `gh run watch <run-id> --exit-status` and NuGet flat-container/registration checks -- CI/Release succeed and exact artifacts are public.
