---
title: 'Align Dapr package catalog on 1.18.4'
type: 'chore'
created: '2026-07-16'
status: 'done'
review_loop_iteration: 4
baseline_commit: '6f82b5eab5643d9bfe9e0cc4163a8e89cc9e80c7'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The shared central package catalog contains an unused `Dapr` entry at `1.17.9` beside the real Dapr .NET SDK package IDs at `1.18.4`. This creates apparent version skew even though current restored Projects and Memories graphs already resolve every Dapr SDK dependency to `1.18.4`.

**Approach:** Remove the invalid, unreferenced bare `Dapr` catalog row and retain `1.18.4` for every active Dapr SDK package declaration. Verify both MSBuild's evaluated central items and restored dependency graphs so the repository has no active Dapr SDK `1.17.9` dependency.

## Boundaries & Constraints

**Always:** Keep project `PackageReference` items versionless; manage Dapr SDK versions through `Directory.Packages.props`; preserve `Dapr.Client`, `Dapr.AspNetCore`, Actors, Generators, Workflow, and Memories AI packages at `1.18.4`; preserve existing unrelated work.

**Ask First:** Ask before changing Dapr CLI/runtime pins, adding or removing a real SDK package, changing `CommunityToolkit.Aspire.Hosting.Dapr`, rewriting historical evidence or generated caches, committing submodule changes, or updating parent submodule pointers.

**Never:** Do not replace the stale row with a nonexistent `Dapr` `1.18.4` package; do not add inline versions or `VersionOverride`; do not initialize nested submodules; do not modify unrelated architecture artifacts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Shared SDK catalog | Evaluated `PackageVersion` items whose IDs are active Dapr SDK packages | Every version is exactly `1.18.4` | Fail verification on any other version |
| Legacy bare entry | Unreferenced exact package ID `Dapr` at `1.17.9` | Entry is absent and cannot suggest a second SDK line | Do not rename or bump the invalid entry |
| Operational Dapr | CI/runtime/CLI pins such as `1.18.0` | Remains unchanged because NuGet CPM does not manage it | Report it separately from SDK package resolution |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props` -- canonical imported package catalog; owns the stale `Dapr` row and the active `Dapr.*` 1.18.4 pins.
- `references/Hexalith.Builds/Tools/validate-dapr-package-versions.ps1` -- executable evaluated-catalog invariant with bounded process and stream-drain handling, controlled diagnostics, and a test-only evaluator shim seam; rejects case-insensitive duplicate or bare Dapr IDs, a missing required SDK ID, and any shared Dapr SDK version other than 1.18.4.
- `references/Hexalith.Builds/Tools/test-dapr-package-version-validator.ps1` -- fixture and evaluator-shim regression suite for imports, evaluated metadata, identity uniqueness, every required ID, process failures, stream separation, timeouts, workflow structure, and all catalog-invariant rejection paths.
- `references/Hexalith.Builds/.github/workflows/build-release.yml` -- release gate that uses the repository-owned .NET initializer's single version source before canonical validation and validator regression tests, without constraining the pre-existing release action's overall runtime.
- `Directory.Packages.props` -- root CPM entry point importing the Builds catalog with transitive pinning enabled.
- `references/Hexalith.Memories/Directory.Packages.props` -- local central additions for `Dapr.AI` and `Dapr.AI.Microsoft.Extensions`, already at 1.18.4.
- `src/Hexalith.Projects.Infrastructure/Hexalith.Projects.Infrastructure.csproj` -- representative versionless direct references to `Dapr.Client` and `Dapr.AspNetCore`.
- `.github/workflows/ci.yml` -- separate Dapr CLI/runtime input, intentionally outside this package-only correction.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- delete the unused `<PackageVersion Include="Dapr" Version="1.17.9" />` row while leaving all real Dapr SDK pins at `1.18.4` -- remove the false second version from the canonical catalog without inventing a package.
- [x] `references/Hexalith.Builds/Tools/validate-dapr-package-versions.ps1` -- evaluate an optional catalog path with `dotnet msbuild -getItem:PackageVersion`; expose a test-only evaluator-script seam while keeping real `dotnet` as the production default; capture stdout and stderr separately without pipe deadlock; bound both process exit and redirected-stream drain; kill the process tree on timeout and never wait unbounded afterward; validate nonblank string identity/version schema; retain the useful tail of actionable failure output; compare NuGet IDs case-insensitively; reject a bare `Dapr`, duplicate case-insensitive `Dapr.*` identities, any missing required shared SDK ID, and every evaluated `Dapr.*` version other than `1.18.4` -- enforce the actual evaluated catalog safely and predictably.
- [x] `references/Hexalith.Builds/Tools/test-dapr-package-version-validator.ps1` -- add a dependency-free, deadlock-safe, time-bounded fixture/evaluator-shim suite covering the canonical catalog; imported items; a mixed-case required SDK identity; aligned and mismatched extra family items; case-insensitive duplicate SDK identities; property-expanded versions; inactive conditions; a case-variant bare ID; no SDK family; an `Update`-driven required-ID mismatch; one missing-ID assertion for each required SDK ID; benign stderr with valid stdout JSON; evaluator timeout; nonzero evaluator exit; empty stdout; malformed JSON; invalid JSON schema; and the release workflow's initializer/validator/tests/release ordering -- protect identity semantics, process/error behavior, and every catalog-invariant branch from regression.
- [x] `references/Hexalith.Builds/.github/workflows/build-release.yml` -- initialize .NET through the existing `Github/initialize-dotnet` composite action's default version source without repeating the version or adding a broad job timeout, then execute canonical validation plus the fixture suite after checkout and before release creation -- prevent environmental drift and validator regressions without constraining the existing release action.

**Acceptance Criteria:**
- Given a representative Projects project imports the shared central catalog, when MSBuild evaluates `PackageVersion`, then no exact `Dapr` item exists and every active shared `Dapr.*` SDK item is `1.18.4`.
- Given the validator receives a catalog path, when bounded MSBuild evaluation and stream drain complete, then only stdout is parsed as JSON, schema and process failures are controlled and actionable, package IDs use NuGet-compatible case-insensitive identity and uniqueness, all six required shared SDK IDs exist at `1.18.4`, and every additional evaluated `Dapr.*` item must also be `1.18.4`.
- Given the committed fixture/evaluator-shim suite, when it runs, then it proves imported items, property expansion, inactive conditions, mixed-case required identities, and aligned extra family items evaluate correctly; proves case-variant bare IDs, duplicates, an absent family, each individually missing required ID, required-ID skew through `Update`, and extra-family skew fail specifically; proves benign stderr is not parsed as JSON; proves timeout/nonzero/empty/malformed/schema failures are controlled; and proves workflow structure keeps initialization and both gates before release.
- Given the Builds release workflow, when a release starts, then the repository-owned initializer supplies its single default SDK version and canonical catalog validation plus the fixture suite run before release creation so either failure blocks release creation, without imposing a new whole-job timeout.
- Given Projects and Memories assets are restored, when direct and transitive package lists are inspected, then all resolved Dapr SDK packages are `1.18.4` and none resolve to `1.17.9`.
- Given the scoped correction, when the complete root and Builds diffs are reviewed, then only the catalog, validator, validator tests, release-gate invocation, and spec changed; Dapr CLI/runtime pins, Aspire integration versions, project references, historical evidence, generated outputs, and unrelated user changes are untouched.

## Spec Change Log

- 2026-07-16, review loop 1: The verification reviewers found that observational MSBuild/package-list commands and normal restore/build could not fail if the unused bare `Dapr` row returned. Added a persistent catalog validator, release-gate invocation, negative validator acceptance cases, explicit Memories restore, executable asset assertions, and submodule-aware diff checks. This avoids a known-bad state where the false second version passes every build. KEEP the minimal deletion, all real SDK pins at `1.18.4`, versionless project references, successful package-graph evidence, and unchanged CLI/runtime and Aspire integration versions.
- 2026-07-16, review loop 2: Review found that raw, case-sensitive XML inspection neither matched evaluated MSBuild items nor protected the complete required SDK set, and that the validator's rejection branches had no committed regression tests. Amended the validator task to consume MSBuild's evaluated `PackageVersion` output with case-insensitive NuGet identity, require all six shared SDK IDs, added a dependency-free fixture suite and release-gate execution, and made verification commands reproducible. This avoids the known-bad state where case variants, `Update`, imports, properties, inactive conditions, missing required IDs, or a regressed rejection branch escape the gate. KEEP the minimal bare-row deletion, the deliberate `1.18.4` invariant, all real SDK declarations, versionless project references, unchanged Memories AI pins, successful restored-graph evidence, and unchanged CLI/runtime and Aspire integration versions.
- 2026-07-16, review loop 3: Review found unsafe mixed-stream process capture, unbounded execution, incomplete identity/required-set/extra-family fixtures, mutable runner SDK selection, and verification commands whose pipeline or multi-file exit status could hide a failure. Amended the validator and fixture tasks for separate asynchronous stream capture, timeouts, controlled schema/process diagnostics, imports, mixed-case SDK identity, every required-ID omission, and an extra-family mismatch; amended the workflow for pinned .NET initialization and a bounded job; split asset assertions and removed the ambiguous exact-count pipeline. This avoids the known-bad state where benign stderr corrupts JSON, an evaluator hangs release, one required ID stops being required, an extra Dapr SDK diverges, or the last successful file masks an earlier graph failure. KEEP the MSBuild-evaluated validator, case-insensitive NuGet identity, complete required set, persistent fixture gate, minimal bare-row deletion, deliberate `1.18.4` invariant, versionless project references, unchanged Memories AI and operational Dapr pins, and successful restored-graph evidence.
- 2026-07-16, review loop 4: Review found that case-insensitive duplicate SDK identities passed, redirected streams could outlive the process timeout, process/schema paths and stream separation lacked executable coverage, a duplicated SDK input and broad job timeout could regress release maintenance, and asset assertions ignored an exact `Dapr` key. Amended the validator for uniqueness, bounded stream drain and a test-only evaluator seam; expanded the suite for aligned extras, duplicates, mixed streams, timeout and process/schema failures plus workflow structure; made the composite initializer the single SDK-version source; removed the whole-job timeout requirement; and strengthened asset assertions against exact `Dapr`. This avoids the known-bad state where duplicate central items pass, a descendant-held pipe hangs release, process diagnostics silently regress, SDK pins drift, release creation is cut short, or an exact bare library escapes graph checks. KEEP the safe evaluated-catalog architecture, all 15 prior scenarios, separate streams, all-family and required-set enforcement, persistent pre-release gate, minimal bare-row deletion, deliberate `1.18.4` invariant, versionless project references, unchanged Memories AI and operational Dapr pins, and successful restored-graph evidence.

## Design Notes

NuGet central package management is keyed by distinct, case-insensitive package IDs. It does not treat the `Dapr` ID and the `Dapr.Client` or `Dapr.AspNetCore` IDs as one version family, and it does not validate an unused catalog item during restore. The stale bare `Dapr` row therefore appeared in MSBuild's catalog but never entered `project.assets.json`; deleting it is safer than changing it to a version for a package that does not exist. Dapr CLI and runtime releases are configured outside NuGet and remain a separately tested compatibility tuple.

## Verification

**Commands:**
- `pwsh -NoProfile -File references/Hexalith.Builds/Tools/validate-dapr-package-versions.ps1` and `pwsh -NoProfile -File references/Hexalith.Builds/Tools/test-dapr-package-version-validator.ps1` -- expected: the canonical evaluated catalog and every committed positive/negative fixture pass within their configured timeouts.
- `actionlint references/Hexalith.Builds/.github/workflows/build-release.yml` plus the fixture suite's workflow-structure assertion -- expected: the workflow uses the repository-owned initializer without a duplicate SDK input or new whole-job timeout and invokes canonical validation and validator tests after checkout and before release creation.
- `dotnet restore Hexalith.Projects.CI.slnx -p:HexalithCommonsRoot="$PWD/references/Hexalith.Commons" -p:HexalithPolymorphicSerializationsVersion=1.16.3` and `dotnet restore references/Hexalith.Memories/src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -p:HexalithPolymorphicSerializationsVersion=1.16.3` -- expected: restores succeed without Dapr downgrade/conflict diagnostics; record the pre-existing invalid `v1.16.3` default separately.
- `dotnet build Hexalith.Projects.CI.slnx --configuration Release --no-restore -warnaserror -m:1 -p:BuildInParallel=false -p:RestoreBuildInParallel=false -p:HexalithCommonsRoot="$PWD/references/Hexalith.Commons" -p:HexalithPolymorphicSerializationsVersion=1.16.3` -- expected: serialized fallback build succeeds with zero warnings and errors.
- Run `jq -e '[.libraries | keys[] | select(((split("/")[0] | ascii_downcase) == "dapr") or (split("/")[0] | ascii_downcase | startswith("dapr.")))] as $dapr | (($dapr | length) > 0 and all($dapr[]; ((split("/")[0] | ascii_downcase) != "dapr") and endswith("/1.18.4")))'` separately against `src/Hexalith.Projects.Infrastructure/obj/project.assets.json` and `references/Hexalith.Memories/src/Hexalith.Memories.Server/obj/project.assets.json` -- expected: each command independently rejects exact `Dapr` and proves every direct/transitive `Dapr.*` SDK library in that restored graph is `1.18.4`.
- `git diff --check`; `git -C references/Hexalith.Builds diff --check`; and, for each untracked script, capture `git -C references/Hexalith.Builds -c core.whitespace=cr-at-eol diff --no-index --check -- /dev/null <script>` and assert its expected diff exit code is `1` with empty output; then perform baseline-aware changed-path inspection -- expected: no whitespace errors and no out-of-scope file changes.

## Suggested Review Order

**Central package invariant**

- The shared catalog now exposes only real Dapr SDK IDs at 1.18.4.
  [`Directory.Packages.props:130`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L130)

- One explicit target and six required IDs define the enforced central invariant.
  [`validate-dapr-package-versions.ps1:19`](../../references/Hexalith.Builds/Tools/validate-dapr-package-versions.ps1#L19)

- Case-insensitive checks reject the bare ID, duplicates, omissions, and version skew.
  [`validate-dapr-package-versions.ps1:429`](../../references/Hexalith.Builds/Tools/validate-dapr-package-versions.ps1#L429)

**Evaluated and bounded validation**

- MSBuild evaluation honors imports, properties, conditions, and `Update` metadata.
  [`validate-dapr-package-versions.ps1:237`](../../references/Hexalith.Builds/Tools/validate-dapr-package-versions.ps1#L237)

- Bounded cleanup separates process, stream, and diagnostic failure behavior.
  [`validate-dapr-package-versions.ps1:291`](../../references/Hexalith.Builds/Tools/validate-dapr-package-versions.ps1#L291)

**Release gate**

- Initialization and both gates run before any release side effect.
  [`build-release.yml:26`](../../references/Hexalith.Builds/.github/workflows/build-release.yml#L26)

**Regression coverage**

- Catalog fixtures cover positive identity, evaluation, and version-boundary behavior.
  [`test-dapr-package-version-validator.ps1:444`](../../references/Hexalith.Builds/Tools/test-dapr-package-version-validator.ps1#L444)

- Process fixtures prove stream separation, bounded cleanup, and useful failure diagnostics.
  [`test-dapr-package-version-validator.ps1:565`](../../references/Hexalith.Builds/Tools/test-dapr-package-version-validator.ps1#L565)

- The suite guards release-step ordering and the initializer's single version source.
  [`test-dapr-package-version-validator.ps1:703`](../../references/Hexalith.Builds/Tools/test-dapr-package-version-validator.ps1#L703)
