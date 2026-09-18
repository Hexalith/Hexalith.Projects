---
title: 'Repair package-only Projects CI and package-candidate validation'
type: 'bugfix'
created: '2026-09-18'
status: 'done'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '1b154fde25a33b7af6dfcac75b26f1694d9bd3fb'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-ci-cd.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projects CI has root-owned BMAD, Folders, .NET 10 test, and mixed sibling-reference failures. Package-only Release restore also lacks four published Conversations/Folders packages.

**Approach:** Fix root-owned failures, make sibling libraries package-only in Release/CI, pin shared CI, and add manifest-driven package plus real-consumer validation. Until upstream publication, exact `NU1101` evidence remains the expected blocker; release hardening is deferred.

## Boundaries & Constraints

**Always:** Use packages for sibling libraries in Release/CI and source only in Debug; AppHost executables remain Aspire resources. Keep warnings-as-errors, individual tests, root-only submodules, Builds SHA `cb91511794c8898b738d85dc6c751f82b832cbc9`, the five-package manifest, exact metadata, and real-API consumers.

**Never:** Modify `references/*`, gitlinks, `.github/workflows/release.yml`, or `release.config.cjs`; initialize nested submodules; create source-mode CI fallback or placeholder packages; mark `NU1101` as success; weaken checks; commit, push, dispatch Release, or publish packages.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Blocked package-only CI | Sibling package unpublished | Policy/fixture tests pass; restore reports every missing ID | Never use sibling source |
| Unblocked package-only CI | All pinned packages exist | Release build, consumer/generated gates, and eight tests pass | Drift identifies its owner |
| Package candidate | Five manifest entries and one version/source SHA | Exact IDs/version/internal ranges, complete dependencies, provenance, compile assets, and five real-API consumers | Unsafe, malformed, stale, empty, or unrestorable candidates fail |
| Folders lookup | Metadata request or hidden direct target | Request serializes `metadata_only`/`NFC`; hidden targets use the canonical indistinguishable 404 and map to denied | No 200-empty/redacted hidden-target fixture |

</frozen-after-approval>

## Code Map

- `Directory.Build.props`, `Directory.Packages.props`, sibling-consuming project files -- Debug-source/Release-package switching; preserve Projects/AppHost references.
- `.github/workflows/ci.yml` -- pinned Builds, MTP, 30-minute timeout, shared consumer gate, independent fixtures first.
- `tools/release-packages.json`, three `scripts/*package*.py` files -- authoritative inventory, exact metadata/provenance/assets, five API probes.
- `tests/tools/test_release_package_tools.py`, `tests/tools/run-*-gate.ps1` -- negative fixtures, package-only policy, SourceTools invariants.
- `global.json`, `tests/tools/run-openapi-fingerprint-gate.ps1` -- .NET 10 MTP command shape.
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs`, its Server tests -- enum request and canonical 404 coverage.
- `_bmad/custom/*.toml`, `tests/tools/test_production_authority_guard.py` -- supported persistent-context overrides.

## Tasks & Acceptance

**Execution:**
- [x] `Directory.Build.props`, `Directory.Packages.props`, `src/**/*.csproj`, `tests/**/*.csproj` -- enforce Debug-source/Release-package sibling libraries and exact four-package pins.
- [x] `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs`, `tests/Hexalith.Projects.Server.Tests/ProjectFileReferenceDirectoryTests.cs`, `_bmad/custom/*.toml`, `tests/tools/test_production_authority_guard.py`, `global.json`, `tests/tools/run-openapi-fingerprint-gate.ps1` -- repair canonical wire, override, and MTP failures.
- [x] `tools/release-packages.json`, `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, `scripts/validate-consumer-package-references.py` -- enforce inventory, dependencies, provenance, safe archives, compile assets, and real API probes.
- [x] `.github/workflows/ci.yml`, `tests/tools/run-ci-workflow-gates.ps1`, `tests/tools/run-package-dependency-gate.ps1` -- pin Builds, run shared package-only validation, retain SourceTools checks, and run fixtures before restore.
- [x] `tests/tools/test_release_package_tools.py` -- exercise every package-candidate matrix failure and the external-version-equals-candidate case.

**Acceptance Criteria:**
- Given current public upstream state, when CI runs, then package-only restore fails with the exact missing Conversations/Folders IDs while root-owned workflow and release-tool fixture tests still report independently.
- Given those upstream packages later exist, when the unchanged CI runs, then Release build, generated gates, five isolated consumers, and all eight test projects pass without sibling-library source references.
- Given a simulated candidate, when validation runs, then exact metadata/dependencies/provenance/compile assets and all five public API probes are proven without a public Projects package source.

## Implementation Notes

## Spec Change Log

- 2026-09-18: Human resolved BH-3 in favor of mandatory package-only CI. Replanned source/package references, upstream-blocked evidence, release no-op routing, real API probes, complete dependency/provenance validation, and executable negative fixtures. Avoid the reviewed CI source fallback, unconditional publication verifier, empty consumer probes, partial dependency checks, and marker-only tests. KEEP the Folders enum fix, supported BMAD overrides, MTP/OpenAPI correction, strict duplicate publication, manifest inventory, immutable Builds pin, and exact public-state reporting.
- 2026-09-18: Human split manual release/public-verification hardening into deferred work. Current scope ends at package-only shared CI and validated candidates; do not edit release workflow/config. KEEP the manifest inventory, real API probes, complete dependency/provenance checks, package-only policy, local contract fixes, and independent fixture tests.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| VG-1 | medium | bad_spec | The generated consumer compiles only its own empty probe and checks assets identity; the reviewer demonstrated that removing `Hexalith.Projects.ServiceDefaults.dll` still passes, so a package without its compile API can reach publication. Package-specific API probes and a stripped-package regression test are non-trivial requirements missing from the plan. |
| VG-2 | medium | bad_spec | No executable negative fixture covers unsafe archive entries or malformed package metadata; valid-candidate runs and filename-presence policy checks cannot detect removal of the fail-closed predicates. The matrix requires this verification, but the plan did not define a fixture-test surface. |
| VG-3 | medium | bad_spec | The post-publication job is tested only through source-text markers; deleting a fail-closing `exit 1` leaves policy tests green. A stubbed GitHub/NuGet verifier test requires extracting the inline workflow logic, which the plan did not specify. |
| BH-1 | high | patch | With no release tags, the first semantic-release version is expected to be `1.0.0`, which legitimately equals the evaluated `Hexalith.Conversations.Contracts` version. The generic external-version contamination check rejects that valid pair before the later source-evidenced check can accept it. |
| BH-2 | false | reject | The pack command intentionally stamps NuGet package metadata after a Release build; package and assembly versions are not required to be identical, and no changed generated output was shown to consume the release version. |
| BH-3 | high | intent_gap | `Directory.Build.props` enables source references whenever `CI=true`, satisfying the frozen “CI source build” matrix row but contradicting the mandatory baseline rule that CI/CD use NuGet package references only. Package mode currently blocks on the unpublished Conversations package, while source mode violates the baseline and fails against the recorded Commons API; human intent is required. |
| BH-4 | medium | bad_spec | The shared workflow runs consumer validation before all unit and integration shards, so the known NU1101 publication blocker prevents the eight test projects from reporting. The frozen requirements demand both test evidence and fail-closed package validation but do not settle their ordering under an unpublished dependency. |
| BH-5 | low | reject | Sequential NuGet publication can leave partial public state after a later network/server failure, but duplicate failure is an explicit frozen requirement and exact verification exposes the partial result. Atomic multi-package publication is unavailable, and recovery design is disproportionate for this uncommon failure. |
| BH-6 | medium | bad_spec | The pinned legacy release workflow succeeds when publication is frozen and can succeed when semantic-release has no candidate, but the unconditional downstream verifier then requires a release at the dispatch SHA. The plan did not provide a publish/no-op output or another reliable condition for post-publication verification. |
| BH-7 | false | reject | The stated requirement is to prove current public state for the exact source SHA; accepting an already-existing release that still resolves to that SHA satisfies that requirement and is not inherently a false result. |
| BH-8 | false | reject | The frozen acceptance asks for the exact GitHub release and declared NuGet versions to exist, not byte-for-byte equivalence between GitHub and NuGet assets. Source identity is established by the release tag resolving to the dispatched SHA. |
| BH-9 | low | reject | Name-only GitHub asset verification would miss a concurrent substitution or corrupt zero-size upload, but semantic-release supplies the assets from the same job and such external mutation is uncommon. Adding digest/provenance machinery exceeds a direct correction. |
| BH-10 | medium | bad_spec | Treating `[version,)` as a matching internal version permits a transitive restore to select a newer Projects package, while the consumer check does not assert every internal resolved version. Exact internal-set behavior needs a dependency-range policy the plan did not settle. |
| BH-11 | false | reject | `EXPECTED_INTERNAL_DEPENDENCIES` is a fail-closed dependency contract checked against the manifest, not a second pack/publication inventory; only the manifest selects projects and package IDs for packing, validation count, and publication. |
| BH-12 | medium | bad_spec | The validator proves four required Contracts dependencies but allows other unexpected external dependencies in any package. That does not satisfy the approved exact dependency-check claim, and defining the complete allowed graph is non-trivial planning work. |
| BH-13 | false | reject | Intended candidates use a fresh, unpublished exact version in a clean temporary package cache, so NuGet.org cannot satisfy the direct package reference; a duplicate public version is separately required to fail publication. No reachable fresh-release path was shown to consume public Projects bytes instead. |
| BH-14 | medium | bad_spec | This independently confirms VG-1: the generated probe uses no package symbol, so an empty package can pass. Five stable public API probes and their regression fixtures are missing from the implementation plan. |
| BH-15 | false | reject | The packer cleans its output, immediately packs manifest-selected projects from the checked-out source, and the validator checks package identity and repository URL. A renamed or stale unrelated archive cannot enter the normal caller path without another invariant already failing. |
| BH-16 | false | reject | All in-scope callers provide semantic-release or fixed valid versions, and `dotnet pack` rejects invalid NuGet versions loudly. The permissive precheck does not create a silent success path in the approved workflow. |
| BH-17 | medium | patch | Replacing the PowerShell gate removed the only focused assertion that `Hexalith.FrontComposer.SourceTools` remains package-mode-only, private, and analyzer-enabled. Nuspec leakage validation does not detect disabling analyzer assets. |
| BH-18 | false | reject | The fifth task requires attempting full Release validation and recording an upstream-only blocker precisely; the observed section does so and explicitly says the remaining shards could not pass that boundary. Its checked state records completion of the task, not a claim that the blocked commands passed. |
| BH-19 | low | patch | The Folders OpenAPI contract says missing, excluded, restricted, and unauthorized direct targets use the canonical 404 safe denial. The renamed redacted/excluded tests instead duplicate the 200-empty missing fixture, so their wire-behavior labels are inaccurate even though a generic 404 test exists. |
| BH-20 | medium | bad_spec | The release-critical Python tools have no focused fixture suite; current policy checks only invocation text and happy-path commands. This overlaps the verified malformed-package, API-probe, and publication-verifier gaps and requires a planned test surface. |
| EC-1 | medium | patch | Moving package validation into the shared build-and-test job leaves the combined build, pack, validation, and eight test shards under the shared default 15-minute timeout, whereas the removed package lane alone had 30 minutes. Setting the existing `build-timeout-minutes` input to 30 is a direct correction. |
| EC-2 | medium | patch | Under `set -e`, failure to resolve any unrelated release tag aborts the verifier before it can inspect later releases. Skipping an unresolvable unrelated tag while still requiring one exact SHA match is a direct fail-closed correction. |
| EC-3 | low | reject | Multiple published tags on the same semantic-release source commit are uncommon; selecting the newest API result still checks an exact-source public release. Requiring uniqueness adds complexity without a demonstrated everyday failure. |
| EC-4 | low | reject | An administrator could delete assets during the short NuGet propagation loop, but concurrent external mutation is unlikely and re-fetching/reconciling changing release state adds non-trivial complexity. |
| EC-5 | low | reject | This duplicates BH-5: partial sequential publication is real but rare, duplicate failure is explicitly required, and no simple atomic fix exists. |
| EC-6 | low | reject | The recorded shared catalog does not declare `Hexalith.Conversations.Contracts`; a hypothetical future duplicate would fail loudly during restore. Making a pre-import fallback conditional on future imported items is disproportionate for this currently unreachable state. |
| EC-7 | low | patch | Calling `.Trim()` on the command result before checking `$LASTEXITCODE` can throw on a missing/invalid gitlink and bypass the gate's actionable failure. Capturing output first and trimming only after the exit/shape check is a direct correction. |
| EC-8 | low | reject | The packer deletes only NuGet archives in its explicitly supplied output directory, and all approved callers use the tool-owned `./nupkgs` directory. Accidental use against an unrelated archive directory is unlikely and an ownership-marker protocol is disproportionate. |
| EC-9 | false | reject | This duplicates BH-16: invalid prerelease text is not supplied by any approved caller and downstream NuGet tooling fails loudly rather than publishing it. |
| EC-10 | low | patch | Case-insensitive duplicate dependency elements with the same version are silently collapsed, leaving ambiguous nuspec metadata accepted. Rejecting any repeated normalized dependency ID is a direct one-condition fix. |
| EC-11 | high | patch | This independently confirms BH-1: source-evidenced external dependencies must be exempt from the generic coincidental-version rejection and then validated against their own evaluated versions. |
| EC-12 | false | reject | This duplicates BH-13: on the fresh-version path NuGet.org has no matching Projects version, the cache is isolated, and duplicate publication is independently fail-closed. |
| EC-13 | medium | bad_spec | This independently confirms BH-12: unexpected external dependencies are not compared with a complete allowed graph, despite the spec's exact-dependency claim. |
| EC-14 | medium | patch | This independently confirms BH-17: the analyzer/private-assets invariant was removed and is not replaced by nuspec-only checks. |
| EC-15 | low | patch | The success line reports only aggregate count/version/SHA, while the acceptance criterion asks each package ID to be reported with its exact version/source result. Emitting one verified line per manifest ID is a direct correction. |
| R2-VG-1 | medium | patch | The fixtures can delete the consumer `dotnet build` call without failing because the only `run_consumer` test exits during restore. Add success-path command assertions and build-failure propagation coverage. |
| R2-VG-2 | medium | patch | The semantic-release `-SkipPack` wrapper path and its normalization flag have no executable orchestration test. A hermetic command-sequence/failure-propagation fixture is a bounded correction. |
| R2-EC-1 | medium | patch | `CI=true` still accepts an explicit `UseHexalithProjectReferences=true`, as direct MSBuild evaluation returned `true`. Add a build target that rejects source mode in CI or any non-Debug configuration. |
| R2-EC-2 | false | reject | Approved CI and release callers build the exact checked-out source immediately before `--no-build` packing, and the packer owns/cleans the candidate directory. The hypothesized substituted binary is not reachable through an approved caller. |
| R2-EC-3 | medium | patch | Archive safety validates paths and sizes but permits active payload roots such as `tools/`, `buildTransitive/`, or `runtimes/`. Reject those roots and cover the rejection hermetically. |
| R2-EC-4 | medium | patch | Dependency elements accept attributes beyond `id`, `version`, and `exclude`, so restore semantics can differ while the exact-graph check passes. Reject all unexpected attributes and add a fixture. |
| R2-EC-5 | low | patch | An explicitly supplied empty expected SHA silently falls back to `HEAD`. Distinguishing `None` from an empty string is a direct correction and preserves the intended omitted-argument behavior. |
| R2-EC-6 | false | reject | carried from EC-9/BH-16: approved callers supply normal semantic-release or fixed CI versions, and `dotnet pack` rejects invalid NuGet versions before candidate validation. |
| R2-EC-7 | low | reject | Normalization can leave an earlier archive rewritten when a later archive is malformed, but every caller stops before validation/publication and rerunning is idempotent. Transactional multi-archive replacement is disproportionate. |
| R2-EC-8 | medium | patch | The non-`SkipPack` package gate regressed from a self-contained restore/build/pack gate to calling a `--no-build` packer, so clean-checkout use fails or consumes stale outputs. Restore the Release restore/build precondition in that mode. |
| R2-EC-9 | low | reject | Current sibling references all use the `$(Hexalith*Root)` convention, and no relative-path bypass exists in the tree. General path-resolution logic for a hypothetical future convention is disproportionate. |
| R2-EC-10 | medium | patch | The claimed negative matrix lacks active-payload, dependency-attribute, empty-pin, successful-build, and wrapper-orchestration fixtures. The surviving production patches require matching executable coverage. |
| R2-BH-1 | false | reject | The gitlink differences are committed changes between the preserved baseline and current `HEAD`; `git status`, submodule status, and the protected-path diff prove this working tree did not modify `references/*`. |
| R2-BH-2 | medium | defer | The `**/project-context.md` glob matches eight sibling contexts in addition to the Projects context and can inject contradictory planning facts. Its fix changes agent-context configuration, so it must be deferred by review policy. |
| R2-BH-3 | medium | patch | This independently confirms R2-EC-1: text-based defaults do not enforce the frozen CI/Release package-only invariant against an explicit global property. |
| R2-BH-4 | medium | patch | `global.json` selects MTP while `run-contract-spine-gates.ps1` still uses VSTest `--filter`; the local gate no longer proves its two intended classes ran. Convert it to direct xUnit v3 assembly selectors with nonzero-count checks. |
| R2-BH-5 | medium | patch | The xUnit v3 runner exits zero for a missing `-class` selection (`Total: 0` was reproduced), so the OpenAPI gate can pass vacuously. Require a nonzero executed-test summary. |
| R2-BH-6 | false | reject | carried from BH-13: candidate versions are fresh and exact, each consumer has an isolated global package folder, and NuGet source-mapping specificity selects `Hexalith.Projects*` over the public `*` fallback. |
| R2-BH-7 | false | reject | Both pinned reusable CI/release workflows restore and build the exact checkout immediately before packing; after R2-EC-8 the local wrapper does too. No approved pack entry point stamps unrelated stale output. |
| R2-BH-8 | false | reject | carried from BH-16/EC-9: invalid prerelease text is absent from approved callers and is rejected loudly by downstream NuGet tooling rather than published. |
| R2-BH-9 | medium | patch | This independently confirms R2-EC-3: valid-path active payloads can pass the archive validator despite the safe-candidate claim. |
| R2-BH-10 | medium | patch | This independently confirms R2-EC-4: unrecognized dependency attributes bypass the exact dependency contract. |
| R2-BH-11 | medium | patch | The probe fixture accepts a package-name string literal and does not prove the build command remains. Lock each probe to its intended public symbol and cover consumer build success/failure. |
| R2-BH-12 | low | reject | The standalone metadata validator accepts an `MZ`-prefixed fixture, but every publish path subsequently compiles the public API consumer and rejects a corrupt assembly. Full PE/CLI parsing is disproportionate duplicate validation. |

## Verification

**Commands:**
- `actionlint .github/workflows/*.yml && pwsh tests/tools/run-ci-workflow-gates.ps1 && python3 -m unittest tests.tools.test_production_authority_guard tests.tools.test_release_package_tools` -- expected: root-owned policy and negative fixtures pass independently of NuGet restore.
- OpenAPI gate and focused Debug Folders tests -- expected: intended MTP tests and canonical request/404 behavior pass.
- `dotnet restore Hexalith.Projects.CI.slnx -p:UseHexalithProjectReferences=false` -- expected now: fail only with exact unpublished upstream IDs; expected after upstream publication: succeed.
- Release build plus eight tests -- expected after upstream publication: zero warnings/errors.
- Simulated pack/validate/consumer scripts -- expected after upstream publication: five exact package-only candidates pass; malformed/dependency/probe regressions fail in fixtures.
