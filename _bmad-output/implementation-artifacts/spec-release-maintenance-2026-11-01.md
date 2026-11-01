---
title: 'Synchronize and publish current Projects packages'
type: 'chore'
created: '2026-11-01'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '7a1652a926b9f478b65f0f961c09e81a000a6954'
context: []
---

<frozen-after-approval reason="user explicitly authorized updates, CI repair, commits, push, and release without human validation">

## Intent

Prepare the current Hexalith.Projects workspace for a verified release: preserve and push existing work, synchronize root-declared dependencies, use the latest stable .NET SDK and compatible package versions, repair demonstrated CI failures, publish the five Projects packages, and verify them on NuGet.org. The user authorizes the complete operation and asks to bypass human validation; do not introduce another approval checkpoint.

## Boundaries & Constraints

Always preserve existing source, documentation, and evidence changes. Work in each owning repository, stay on main, validate Conventional Commit messages before committing, and publish submodule commits before parent pointers. Latest stable SDK is 10.0.403 according to Microsoft release metadata on this date; .NET 11 is prerelease. Preserve documented package compatibility holds that require a newer target framework or incompatible API migration. Release remains dependent on real passing automated checks and package verification.

Never initialize nested submodules, reset or clean user work, fabricate acceptance evidence, disable tests, weaken authorization, or replace functional checks with unconditional success. Human release approvals may be bypassed within the user's granted access; automated validation remains meaningful. Upstream technical changes remain in their owning root-declared repositories. Remote synchronization and publication are performed by the parent session after local implementation.

</frozen-after-approval>

## Code Map

- `global.json`: stable SDK pin is already 10.0.403. Folders root-declared repository still pins 10.0.402 and needs the mechanical SDK update handled by the parent.
- `Directory.Packages.props`: imports `references/Hexalith.Builds/Props/Directory.Packages.props`; no inline NuGet versions. Latest synchronized Builds includes current Commons 2.30.10 and EventStore 3.130.2. Parent coordinates further catalog maintenance and will notify the implementer when final package selection is ready; Conversations is expected to advance to 1.4.0.
- `tools/release-packages.json`: exact five-package inventory and dependency expectation; existing user change raises EventStore.Contracts 3.129.0 to 3.130.1. Synchronizing Builds changes additional graph versions: derive actual package dependency closure and update only demonstrated mismatches.
- `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, `scripts/validate-consumer-package-references.py`: existing authoritative packing, metadata checks and actual consumer compilations. Reuse these; do not add a second manifest.
- `.github/workflows/ci.yml`: workflow policy, package-mode reusable CI, generated artifacts and current G-6 runtime qualification. Existing run 41315045286 fails package manifest validation, then reports missing test counts because tests never ran after that failure.
- `.github/workflows/release.yml`, `release.config.cjs`, `docs/runbooks/nuget-trusted-publishing.md`: dispatch-only release requires exact-current-main successful CI and fresh live G-6, OIDC NuGet login and semantic-release. Production currently has no required-reviewer rules and publish variable is true. Do not delete automatic gates.
- `tools/qualification/run_g6_current.py`, `tests/tools/run_g6_ci_gate.py`, `references/Hexalith.Builds/Tools/g6-current-policy-2026-10-20.json`: current source qualification; execute real checks and repair implementation defects at the owner boundary.
- `_bmad-output/implementation-artifacts/spec-dw-76-restore-green-projects-main-ci.md`, `deferred-work.md`: preserve preexisting local CI investigation. EventStore DW-545 records version/restore output collision in the live PostgreSQL qualifier; newer owner source may already repair parts of it.

## Tasks & Acceptance

**Execution:**
- [x] Inspect synchronized Projects package graph and repair exact dependency manifest expectations.
- [x] Audit root npm and E2E dependencies against current registries; update supported packages and locks only when newer releases exist.
- [x] Run package-only Release restore/build, existing package tools, isolated consumer checks, relevant workflow tests and individual .NET suites. Diagnose and repair actual Projects regressions.
- [x] Record exact passing checks and any remaining upstream or environment blocker without altering frozen historical evidence.
- [ ] Parent: update Folders SDK, maintain shared package catalog, publish submodule commits and record root gitlinks; push the validated root commit.
- [ ] Parent: observe exact-current-main CI, repair real failures, dispatch Release and verify GitHub release and all five NuGet package downloads/signatures.

**Acceptance Criteria:**
- Given current registry metadata, when dependencies are audited, then SDK and compatible package selections are current and any compatibility hold has a concrete reason.
- Given synchronized main source, when automated checks execute, then Release build, required individual test suites, exact package graph and consumer gates succeed.
- Given passing main CI, when release is dispatched, then its automated runtime and publication checks succeed without a human approval pause.
- Given the resulting version, when queried publicly, then all five declared packages can be downloaded from NuGet.org and correspond to the GitHub release.

## Implementation Notes

- Parent repaired the EventStore legacy workflow caller contract, updated local tool versions, and patched DW-545: promoted Tenants references retain the source EventStore identity; immutable previous-peer binaries use a normalized NuGet root separator. Evaluated source reference metadata and separator-free NuGet-root paths passed. Full source builds and fresh G-6 remain in progress.

- Parent captured existing changes and synchronized clean root-declared repositories using fast-forward merges. EventStore's existing ledger change was committed with commitlint and rebased onto latest origin/main. No nested submodules were initialized.

- Local package graph repair uses the five actual packed nuspecs, with no added or removed dependency IDs. The Contracts, core, Client and Testing manifests now require EventStore.Contracts 3.130.2, Conversations.Contracts 1.4.0, and Commons.Serialization/UniqueIds 2.30.10. The preexisting EventStore manifest repair is preserved and advanced to the synchronized catalog.
- Registry audit on 2026-11-01 confirmed all root release-tool direct selections and lock entries already match npm latest tags. `npm ci --ignore-scripts` succeeds; no root manifest/lock update was necessary. E2E updates axe-core/playwright to 4.14.0, faker to 10.6.0, Playwright to 1.64.1, playwright-utils to 4.6.0, Node types to 26.6.6 and dotenv to 18.0.9, with a regenerated lock. `npm audit fix --ignore-scripts` also applies supported transitive fixes without forcing major upgrades.
- TypeScript remains 6.0.3. Registry metadata for latest 7.0.2 exports the stable package root as `./lib/version.cjs` and exposes compiler APIs only under unstable subpaths, while `specs/browser-configuration.spec.ts` uses the stable `transpileModule`, `ModuleKind` and `ScriptTarget` API. Upgrading would require an incompatible fixture/API migration; this is the concrete compatibility hold.
- npm vulnerability audit is not clean: the root latest direct graph still reports 37 moderate and one high finding, including npm 11.21.1's bundled undici 6.29.0. The supported audit-fix command leaves that bundled dependency unchanged; no forced downgrade or override was introduced. E2E supported fixes remove its high finding, leaving one low and ten moderate transitive findings through playwright-utils/exceljs/deepmerge. Registry-signature verification succeeds for all 498 root and 118 E2E packages. These are retained upstream dependency risks, not claimed fixes.
- Source and package tools needed no functional changes. No historical qualification evidence was modified. Remote exact-source CI, live G-6 and release/public-package verification remain parent-owned and pending.

## Spec Change Log

## Review Triage Log

- Blind 1 — false: the central audit records feed observations rather than compatibility acceptance by design; retained pending consumer acceptance is honest. Projects actual five packed consumers passed against the selected graph; published owner releases and current G-6 remain independent evidence.
- Blind 2 — medium, patched by verification: both affected source builds passed sequentially, then Reflection.AssemblyName verified Contracts, host and Server actual DLL versions are all 3.130.2.0. The retained G-6 gate repeats this build sequence and requires real child startup; no duplicate implementation-mirroring test was added.
- Blind 3 — false: the new target is conditioned on an empty HexalithSourceMappingHash, matching the existing source-only guard. Active producer mappings bypass it; existing mapped Conversations builds remain part of fresh G-6.
- Blind 4 — false: patched describes the implemented correction, not completed qualification; the spec remains in-review with parent runtime/release tasks unchecked. Both builds are now recorded; live qualification remains explicitly pending.
- Blind 5 — low, patched: appended supersession notes to historical DW-76 spec and ledger; preserved frozen intent and original failed evidence.
- Blind 6 — low, patched: the supersession note labels the old manifest-only claim historical and requires fresh qualification for the current runtime gitlink/source changes.
- Blind 7 — maybe-false, unverified medium, deferred: no broken live UI behavior was demonstrated; the updated axe/browser application/accessibility lane is explicitly unverified. A real managed AppHost/accessibility run would settle compatibility; harness success is not substituted for it.
- Blind 8 — low, patched by recorded verification: parsed the legacy reusable contract and validated all caller input/secret keys, retaining 19 isolated suites and AppHost integration; startup-failure cause is removed without dropping coverage.
- Blind 9 — low, patched by recorded verification: both owner tool restores and harmless help invocations succeeded with the updated pins.
- Blind 10 — low, patched: recorded exact high advisory identifiers and locked dependency chain; Projects DW-79 names its owner and concrete upstream refresh/verification closure conditions.
- Edge-case hunter — no findings.
- Verification-gap reviewer — no findings.

## Verification

- `CI=true dotnet build Hexalith.Projects.CI.slnx --configuration Release -m:1 -warnaserror`.
- `python3 -m unittest tests/tools/test_release_package_tools.py tests/tools/test_release_workflow.py -v`.
- `CI=true pwsh tests/tools/run-package-dependency-gate.ps1 -PackageDirectory /tmp/projects-release-maintenance/packages`.
- Run individual configured test projects or baseline built-assembly fallback; preserve exact results.
- GitHub exact-source CI and Release workflows; public NuGet flat-container package indexes, downloads and signatures.

### Local implementation verification (2026-11-01)

All .NET commands use `CI=true NUGET_PACKAGES=/tmp/projects-release-maintenance/nuget`; temporary logs/results are under `/tmp/projects-release-maintenance*`.

- `dotnet build Hexalith.Projects.CI.slnx --configuration Release -m:1 -warnaserror`: passed after final catalog selection, zero warnings/errors.
- `pwsh tests/tools/run-package-dependency-gate.ps1 -PackageDirectory /tmp/projects-release-maintenance/packages`: passed restore/build, all five candidate packs at 91.92.93-ci.1, exact manifest metadata/provenance/dependency validation, and five isolated real-API consumer restore/compilations. Initial pack correctly failed the stale dependency versions before the manifest repair.
- `python3 -m unittest tests/tools/test_release_package_tools.py tests/tools/test_release_workflow.py -v`: 34 passed after manifest repair.
- Each configured test assembly was run individually and serially with `dotnet tests/<project>/bin/Release/net10.0/<project>.dll -noColor -parallelMode none -result-xml /tmp/projects-release-maintenance/<suite>.xml`. Results: Contracts 230 passed; Client 125; core 684; Server 1,262 passed/one skipped; UI 174; Mcp 35; Cli 22; Integration 85. Total: 2,617 passed, zero failed, one existing skip. The skipped `SupportedProjectReadHttpTests.ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart` explicitly requires the isolated Story 6.1 Dapr persistence lane; this general suite run does not claim that live case.
- `CI=true pwsh tests/tools/run-ci-workflow-gates.ps1`: passed all five workflow policy checks.
- `pwsh tests/tools/run-frontcomposer-inspect-gate.ps1`: passed real inspection of 12 annotated inputs with no warnings.
- `pwsh tests/tools/run-openapi-fingerprint-gate.ps1`: passed Release build and 101 focused generation/provenance/drift tests.
- Root `npm ci --ignore-scripts` and `npm audit signatures`: passed, 498 package signatures verified. E2E `npm install --save-dev --ignore-scripts` for the six versions above, `npm audit signatures` and `npm run typecheck`: passed, 118 package signatures verified. `npm audit fix --ignore-scripts` in both workspaces exits 1 because unresolved advisories remain; it applies supported E2E transitive fixes and makes no root lock change. Final `npm audit --json` also exits 1 with the counts recorded above. Final E2E `npm outdated --json` exits 1 and lists only the documented TypeScript major-version hold.
- From `tests/e2e`, `npx playwright test specs/browser-configuration.spec.ts specs/live-fixture-identities.spec.ts specs/live-fixtures-lifecycle.spec.ts specs/framework-smoke.spec.ts --project=chromium --workers=1`: 24 passed; three existing AppHost-only lifecycle cases skipped because this was the no-AppHost harness lane. This does not claim live application acceptance.
- `git diff --check`: passed.

### Parent verification progress

- SDK update: Folders `dotnet --version` selects 10.0.403; committed and pushed. Other live repository root SDK pins are already 10.0.403.
- Builds `validate-central-package-versions.ps1` passes all 314 entries; `test-authoritative-package-catalog.ps1` passes 51 approved IDs/four family versions. Revision-bound incremental freshness audit refreshes Conversations/Memories/Parties and validates all 314 packages/154 families. `validate-package-version-exceptions.ps1 -InventoryPath Tools/package-version-exceptions.json -CatalogPath Props/Directory.Packages.props` passes 16 entries. An optional full-umbrella WorkspaceRoot run reports 47 preexisting fixture/unavailable-owner inventory errors; the actual CI command above passes.
- EventStore workflow YAML was parsed and caller `with`/`secrets` keys checked against the actual selected legacy reusable contract: passed, all 19 isolated test projects and blocking AppHost integration retained. Staged v2-only profiles/secrets/preparation were removed without dropping test coverage.
- EventStore and Parties `dotnet tool restore` and harmless tool `--help` invocations pass after the pinned updates. EventStore admin CLI is 3.130.2; both module-tool pins are 4.41.1.
- DW-545 serial source builds: `CI=true NUGET_PACKAGES=/tmp/projects-release-maintenance/nuget dotnet build <LiveSidecar then Server.Tests csproj> --configuration Debug --no-incremental -m:1 -p:UseHexalithProjectReferences=true --disable-build-servers` both pass with zero warnings/errors. After both builds, actual Contracts, Sample host and Server DLL assembly identities all equal 3.130.2.0. The same sequence and real child startup are exercised by the retained current G-6 runner; fresh qualification is still pending.
- FrontComposer source projection was recomputed from its committed selected Platform/Builds identities using the existing canonical helpers; no helper bytes or inventory-only identities changed. Its 30 ConsumerProjectionTests pass. The broader 169-test governance run has 14 preexisting local missing-nested-Builds errors; nested initialization was not performed. Actual local diff evaluation is similarly blocked by unavailable nested Git objects. The hosted exact-object acquisition/diff lane remains required; no prior failed transition is relabeled successful.

### Review clarifications (2026-11-01)

- Existing `/tmp/projects-release-maintenance-root-npm-audit-final.json` identifies the remaining high-risk leaf as `semantic-release 25.0.9 -> @semantic-release/npm 13.2.0 -> npm 11.21.1 -> node-gyp 12.4.0 -> bundled undici 6.29.0`. Its high advisories are [GHSA-xjj6-f33q-929c](https://github.com/advisories/GHSA-xjj6-f33q-929c) (excessive content-encoding DoS), [GHSA-2m5v-pch6-74hx](https://github.com/advisories/GHSA-2m5v-pch6-74hx) (TLS validation bypass) and [GHSA-qrxm-fqhp-jwvm](https://github.com/advisories/GHSA-qrxm-fqhp-jwvm) (TLS renegotiation DoS). The same leaf also carries moderate [GHSA-4qv8-2p9j-2685](https://github.com/advisories/GHSA-4qv8-2p9j-2685) and [GHSA-9fm3-97px-rjf6](https://github.com/advisories/GHSA-9fm3-97px-rjf6). Existing lock/audit output distinguishes this leaf from the unaffected undici 6.29.1 and 7.30.1 copies. Projects release-tooling follow-up [DW-79](deferred-work.md#dw-79-remove-the-projects-release-toolings-bundled-undici-advisory-exposure) owns a compatible upstream refresh; its removal condition requires a clean result for these high IDs on this chain, signature verification and existing release-tooling checks. No dependency overrides were forced.
- The 24 E2E harness passes establish fixture/tool wiring only. The accessibility smoke case checks a synthetic accessible document, not the running Projects UI. Current live application compatibility and real-application accessibility compatibility after the dependency updates remain unverified; the three AppHost-only cases were skipped and are not passes. Fresh G-6 and the live application/accessibility lanes must supply their own results; these harness results cannot stand in for them.
