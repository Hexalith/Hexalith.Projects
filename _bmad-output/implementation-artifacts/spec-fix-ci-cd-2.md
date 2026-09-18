---
title: 'Implement Projects CI/CD with the shared Hexalith release method'
type: 'bugfix'
created: '2026-09-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-ci-cd.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projects has never published its five declared NuGet packages. Its latest push CI is red because a removed BMAD skill path is still asserted and a Folders client contract changed from a free-form string to an enum; its release/package implementation also predates the manifest-driven, package-consumer-validated method now used by Tenants, EventStore, and FrontComposer.

**Approach:** Repair the current failures, adopt the siblings' common shared-CI/manual exact-green-source release spine, make the package manifest authoritative for pack/validation/publication, and add post-release verification that proves the exact GitHub release and all declared NuGet versions exist. Verify current public state, but do not perform a remote release.

## Boundaries & Constraints

**Always:** Keep Release warnings-as-errors, individual test projects, generated-artifact gates, root-only nonrecursive submodule initialization, immutable publication workflow identity, protected `production`, least practical permissions, Conventional Commit versioning, exact package count/IDs/version/dependency checks, isolated package-consumer restore/build, and failure on duplicate NuGet publication. Preserve the five-package inventory unless source evidence proves an entry is invalid.

**Never:** Modify `references/*` content or gitlinks; initialize nested submodules; weaken tests, dependency metadata, or package-consumer checks; use `--skip-duplicate`; add push-triggered publication; expose secrets; rewrite Git history; commit, push, dispatch Release, or publish packages. Do not hide the currently unpublished `Hexalith.Conversations.Contracts` dependency—report it as an upstream publication blocker if it remains absent.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| CI source build | Root-declared sibling sources at recorded gitlinks | Release build and eight explicit test projects run through shared CI | Compile/test drift blocks CI with the owning file identified |
| Package candidate | Five manifest projects and one semantic version | Exactly five packages have matching IDs/version, correct internal versions, preserved external versions, and package-only consumer validation | Missing/unpublished dependencies block before NuGet push |
| Release dispatch | Current `main` SHA with successful push CI | One non-cancellable protected release uses the identical pinned Builds SHA/input | Stale/red/non-main source stops before credentials |
| Publication verification | Semantic release version and exact source SHA | Matching GitHub release assets and all five NuGet versions are observed | Missing/mismatched package or source fails the workflow |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml` -- shared `domain-ci.yml` caller plus Projects-only gates; add consumer validation without copying sibling-specific governance lanes.
- `.github/workflows/release.yml` -- retain operator dispatch/exact-green-source preflight; update the immutable Builds binding and add exact publication verification.
- `tools/release-packages.json` -- sole five-package publication inventory and independent expected-count boundary.
- `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, `scripts/validate-consumer-package-references.py` -- adapt the Tenants/EventStore/FrontComposer manifest, metadata, dependency, and isolated-consumer pattern.
- `release.config.cjs` -- delegate preparation/validation to the scripts and publish without duplicate suppression; keep GitHub package assets.
- `tests/tools/run-package-dependency-gate.ps1`, `tests/tools/run-ci-workflow-gates.ps1` -- converge existing Projects invariants on the authoritative manifest/scripts and verify release routing/publication checks.
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs`, `tests/Hexalith.Projects.Server.Tests/ProjectFileReferenceDirectoryTests.cs` -- use the generated metadata-only Folders enum and lock its serialized request.
- `_bmad/custom/*.toml`, `tests/tools/test_production_authority_guard.py` -- move Projects-owned persistent context checks to supported team overrides after removal of `bmad-create-story`.
- `Directory.Build.props`, packable `src/*/*.csproj` -- preserve local source debugging while making package-candidate dependency behavior explicit; do not invent dependencies for unpublished siblings.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs`, `tests/Hexalith.Projects.Server.Tests/ProjectFileReferenceDirectoryTests.cs` -- repair the Folders enum drift and lock the metadata-only request.
- [ ] `_bmad/custom/*.toml`, `tests/tools/test_production_authority_guard.py` -- move the obsolete skill-path assertion to supported workflow customization overrides.
- [ ] `scripts/*.py`, `tools/release-packages.json`, `release.config.cjs`, `tests/tools/run-package-dependency-gate.ps1` -- add manifest-driven pack, package/dependency validation, and isolated consumer validation; remove duplicated package lists.
- [ ] `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `tests/tools/run-ci-workflow-gates.ps1` -- align callers with the immutable exact-source release method, no duplicate suppression, and post-publication verification.
- [ ] `Hexalith.Projects.CI.slnx`, eight `tests/*.Tests/*.csproj`, generated-artifact gates -- run the full Release validation and record any upstream-only blocker precisely.

**Acceptance Criteria:**
- Given the recorded root gitlinks, when Release CI runs, then all locally owned build, workflow, and test failures are fixed without nested submodules or reference edits.
- Given a simulated version, when package validation runs, then inventory, nuspec dependencies, count, version, and isolated consumer behavior are proven before publication.
- Given a release dispatch, when the source is not current green `main` or publication is incomplete, then the workflow fails before or immediately after the publication boundary with actionable evidence.
- Given NuGet.org and GitHub public state, when verification completes, then each of the five Projects IDs is reported with an exact version/source result; absence is reported, never inferred as success.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The shared method is the intersection of the three reference repositories: reusable CI, manual exact-source release, a package manifest with an independent count, deterministic pack/validate/consumer scripts, and public post-release proof. EventStore-only OQ8/container lanes, Tenants release-floor recovery, and FrontComposer pact/UI governance stay out of scope.

## Verification

**Commands:**
- `actionlint .github/workflows/*.yml && pwsh tests/tools/run-ci-workflow-gates.ps1` -- expected: workflow syntax and policy pass.
- `dotnet restore Hexalith.Projects.CI.slnx && dotnet build Hexalith.Projects.CI.slnx --no-restore -c Release -warnaserror -m:1` -- expected: zero warnings/errors.
- `for project in Contracts Client '' Server UI Mcp Cli Integration; do dotnet test "tests/Hexalith.Projects${project:+.$project}.Tests/Hexalith.Projects${project:+.$project}.Tests.csproj" --no-build -c Release || exit; done` -- expected: all eight projects pass individually.
- `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py ./nupkgs && python3 scripts/validate-consumer-package-references.py ./nupkgs` -- expected: package-only validation passes, or identifies an exact unpublished upstream package.
- `curl`/`gh` read-only checks against NuGet.org and `Hexalith/Hexalith.Projects` -- expected: exact public package/release state is reported.
