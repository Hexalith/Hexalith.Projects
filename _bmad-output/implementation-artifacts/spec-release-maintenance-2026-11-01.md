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
- [x] Parent: finalize the follow-up catalog/root gitlinks and push all recorded evidence (SDK and owner fixes already pushed).
- [x] Parent: observe exact-current-main CI, dispatch Release and verify GitHub release and all five public NuGet downloads/signatures.
- [ ] Parent: observe follow-up owner/final-sync CI and close remaining real failures.

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

### Follow-up source qualification repair

- The first full current G-6 run at Projects 29f71190ce0e2b536d253e310f4c0c5aae175eca passed the real PostgreSQL restart qualifier (1/1), deterministic support (33/33), fixture controls (33/33), nine project AppHost builds and McpCli. It failed only the Platform file host: its Debug configuration selected source locally, but the choice was not global and a foreign Tenants edge restored published EventStore contracts lacking current source APIs. The failed receipt remains retained under `.g6-current-evidence/maintenance-20261101-local`.
- A fresh isolated file-host build succeeds with the single explicit global property `-p:UseHexalithProjectReferences=true`. The runner now passes that property and records Platform as source mode, matching the other AppHosts and the qualification policy. All 26 existing current-runner/environment/cleanup tests pass. No production API, package pin, or automatic acceptance rule changes.
- Folders formatting CI failures were corrected in two test files using the formatter; scoped whitespace verification passes. Parties documentation now matches its module-tool 4.41.1 pin and the existing documentation contract passes.
- EventStore's previously staged Platform.IntegrationTests suite is now explicitly classified in its existing CI lane table; the existing exact failed classification test passes (1/1). Existing blocking test projects remain configured. Concurrent owner changes refined the source metadata target to the selected Tenants checkout and added dependency-mode regression coverage; these changes were preserved when synchronizing main.
- The user reaffirmed latest-main synchronization while forbidding nested initialization. All root-declared repositories were fetched; every nested checkout remained deinitialized. Tenants records the EventStore readiness-fix revision without initializing its child checkout. FrontComposer's latest-main dependency declarations and projection seals were reconciled with concurrent owner changes, retaining later-base governance checks.

### Follow-up review triage (2026-11-01)

- Blind1 — medium, patch: both parsers ignore a final empty path assignment and retain a stale eligible path; the implementation agent is correcting this with actual empty-override cases.
- Blind2 — low, rejected: exotic concatenated quoting/continuations are valid Git syntax but are absent from all declared workspace paths; unsupported forms fail closed. A general Git-config parser adds complexity for a layout developers do not ordinarily meet here.
- Blind3 — low, rejected: physical symlink substitution is possible but no workspace uses one; Git checkouts provide actual dependency directories. Physical-tree traversal guards add complexity for an unencountered layout, beyond the requested deinitialized-checkout fallback.
- Blind4 — maybe-false (unverified medium), defer: all actual direct checkouts are either complete Git checkouts or absent; a partially populated direct Contracts tree could disagree with coarse existing MSBuild root selection. A deliberately partial checkout and actual source build would settle whether a launch can occur with divergent binary selection.
- Blind5 — false: the Folders test catalog fallback mirrors its actual existing Directory.Packages.props sibling import. It inspects the same catalog used by that owner build; unrelated-catalog divergence is not established.
- Blind6 — medium, patch: cold-cache metadata restore is another independent MSBuild operation and currently consumes response-file properties; exclude automatic responses there and verify a cold-cache restore under a conflicting response file.
- Blind7 — medium, patch: the pending candidate digest omits fixture Hosting inputs now used by candidate tests; extend the owned-path and coverage lists and regenerate the pending inventory. No prior accepted evidence is rewritten.
- Blind8 — low, patch: a blank non-null scheme is accepted and fails later during fixture requests; reject it at registration with focused coverage.
- Blind9 — false: options assertions are supplemented by the existing real request fixtures (ContextReadAuthority, GoldenLifecycle, MixedSurfaceHandoff and routing), whose synthetic humans, credential failures and domain authorization controls were run across 853 integration cases; four remaining failures are precisely the old packaged Memories path resolver. Production HumanPolicy requirements and fail-closed fallback remain intact.
- Blind10 — low, patch: metadata consumer documentation still describes self/direct only; update those comments to reflect the declared-sibling rule.
- Edge1 — low, rejected: the EventStore internal resolver would accept traversal segments, but all actual metadata callers use literal module-relative project paths. No reachable untrusted/configured traversal source exists; this argument-validation expansion adds guards for unused input.
- Edge2 — low, rejected: the same internal constant-only caller rule applies to Memories; no actual caller supplies parent traversal.
- Edge3 — medium, patch: same EventStore empty-override defect as Blind1, retained as its own triage row.
- Edge4 — medium, patch: same Memories empty-override defect as Blind1, retained as its own triage row.
- Verification1 — medium, patch: no regression combines matching inherited selections and a conflicting automatic response file; add the exact writer case so the environment-observation subprocess is independently covered.
- Verification-other1 — medium, patch: the Contracts wrapper still expects eight Python tests while the expanded compatibility suite executes twelve; fix the stale count assertion and invoke the built wrapper test.

### Published release and follow-up verification

- Projects CI 41330638275 passed for exact source 2405d82997519bdd4a6ed61cce8abef748f2b6bd, including hosted fresh G-6. Release 41333151082 then reran fresh qualification, completed OIDC login and published v1.2.2. The tag points directly to that source. All five public indexes include 1.2.2; all actual downloads validate exact manifest, source metadata and signatures, and all five isolated real-API consumers compile. Durable proof is in release-evidence/nuget-trusted-publishing-1.2.2-2405d82997519bdd4a6ed61cce8abef748f2b6bd. Publication is refrozen and REST confirms HEXALITH_RELEASE_PUBLISH_ENABLED=false.
- The fresh local full G-6 attempt passed every code/runtime check (real PostgreSQL 1/1, support 33/33, fixture controls 33/33, ten AppHost builds and McpCli). Its overall result remains failed because a concurrent external actor added a shared Redis container during the audit; all owned cleanup passed. This receipt is not relabeled successful. The isolated hosted CI and Release G-6 receipts are qualified and preserve shared-resource invariants.
- Review follow-up patches: catalog suite passed 99 scenarios including a real isolated rebuild; 90 non-rebuild cases passed after independent response-file isolation/cold metadata coverage was added. The final edited cold fixture passes independently, uses NuGet-normalized identity and constructs only a temporary local metadata archive; no cache archive retention is assumed. The matching inherited-selection/response-file writer path is covered. Package audit and central-authority validators pass 314/154/1 and 51 approved IDs respectively.
- Tenants final source build passes with zero warnings/errors; 21 bound-command and 120 hosted-bootstrap cases pass, plus its actual Release wrapper 1/1 and 12 Python compatibility cases. Timers use the existing TimeProvider seam; only a valid infinite transport timeout receives the finite 100-second fallback. All original caller cancellation, real authentication and authority readiness rules remain enforced.
- Folders standalone hosted CI now passes every repaired assertion/fixture, with only the four already documented missing-Memories-project cases remaining. The reusable main job did not initialize that root declaration; workflow-owned, standalone-only setup is being added. No automatic MSBuild initialization is restored. Current source-mode 14/14 topology cases pass with nested checkouts absent.
- Memories automatic release 41335320800 succeeded and published 3.0.15 with the sibling fix; the parent is waiting for public propagation before adopting it in the shared catalog. Projects 1.2.2 has already passed public verification and is being adopted in that catalog.

### Final release source requirement

The verified v1.2.2 release is an intermediate publication from2405d82997519bdd4a6ed61cce8abef748f2b6bd. Additional owner CI repairs and latest-main synchronization completed after that source was frozen. To honor the requested order, the parent will push the complete functional module snapshot, wait for exact-source CI, and create the final release (expectedv1.2.3) from that head. It will again verify all five public package downloads, signatures, provenance and consumers; the publication switch remains false until this exact-source dispatch is ready. Post-release evidence-only recording does not change the frozen functional source.

All five adopted Memories3.0.15 packages are now public and their nuspec source commits match e8956eb5c1bd49297f86702f9606eb9b013bc2f1. Folders Release/package IntegrationTests pass853/853 with no skips using those actual packages and no nested checkouts. The central catalog records Projects1.2.2 and Memories3.0.15; the fresh audit marks all ten package entries listed/latest and validates all314 entries.

### Full catalog runtime-loading regression

Builds CI41337279205 exposed a cross-phase test-host conflict after the new cold fixture loaded SDK NuGet.Versioning7.9 into the PowerShell host; later rebuild checks load Tooling's own NuGet version. The fixture now normalizes identity in a separate PowerShell process, preserving SDK semantics without changing the Tooling loader. The full existing catalog suite passes101 scenarios, including all successful generated snapshots loading with offline NuGet semantics and an actual isolated incremental rebuild. The corresponding production writer and catalog selections are unchanged.

### Complete functional snapshot

All twelve root-declared modules match their fetched main tips and have clean worktrees; every nested checkout is deinitialized. The exact canonical source identities and stable SDK pins are recorded in maintenance-main-snapshot-2026-11-01.json. Folders includes the subsequent owner-authored0eabc23856fde13dd76cab5d7ac2cb45739df8c5 fixture/npm refresh; its predecessor11aea2162ce42b7ce1e110e4dde4cae94aa6f2db passed complete hosted CI. EventStore7776e64fdb5b9b86d0d180a3a8f47c1f11379466 passed main CI41336998470. Tenants526936ef9d9b1566b0832c70e38e3c0b39ffbee1 adds the independently reviewed closed rejection diagnostics,175 passing focused cases, and current Builds/EventStore declarations; its new source-profile run41340025527 remains pending. No assertion, auth rule, receipt requirement or producer grant was weakened. The root final CI/release will use this committed source snapshot; later unrelated owner movement is not part of its frozen source.

### Latest-main continuation and request-body repair

A fresh all-root fetch observed newer EventStore1965ac06af00552636c6d9d77b0cb77996220e05, FrontComposer7fd0d09f36e38dfeb044bf54402cf360fe7a11b6 and Buildsaeb4872c993ca81627f5648f9aeb4d793cb593ec. Clean checkouts were fast-forwarded, preserving upstream work. The catalog now consumes the verified EventStore3.131.0 cohort; Projects exact dependency expectations advance accordingly. The package gate passes with all five isolated real-API consumers. The new catalog exposed stale Builds synthetic fixture/schema pins; only the documented contract corpus and its exact matched hash bindings are being refreshed, with unchanged negative expectations and historical qualification records.

The real Tenants source invocation failed because Dapr retried an unknown-length streamed JSON request after its5-second timeout with an empty consumed body. An isolated actual Dapr1.18.7 probe reproduced400 for streamed content and202 for identical known-length bytes, with unchanged timeout/retry policy and no shared backends. Tenants48ba1fcb74779e8dc9dd4851d55e1055762eebab buffers the existing signed JSON before sending. Final full Debug/source build is clean;24 bound-command and205 hosted-bootstrap tests pass. Hosted source-profile qualification remains required before final release; see the dedicated Tenants spec and credential-free replay proof.

Builds4d2ef11ea7f36b955ed701fef5af1f26a6414942 is pushed with the106-file mechanical fixture/schema refresh. Synthetic acceptance consistency reports zero changes;7 preparation,191 evidence and full publisher contract checks pass. The full Module suite ran2733 cases:2727 passed,3 existing platform/qualification skips and3 failures. The stale schema constant was corrected and its focused check passes1/1; the other two fixture failures pass the isolated7-case rerun. No failed full receipt is relabeled successful; the fresh hosted Builds run remains required.

The final dependency cut records all12 clean fetched-main checkouts in maintenance-main-snapshot-2026-11-01.json. Tenants331c528219e921e990402a6134fed9bfc50f0441 includes the demonstrated Dapr replay correction and fully verified3.131.0 default/package/release qualification. EventStore1965 exact main CI is green. Builds4d2 fixture repair and FrontComposer07089dcb latest tooling refresh are preserved. All nested checkouts remain deinitialized. Final root exact-source CI, new Tenants source profile and final publication remain pending.

Builds fresh hosted CI41345961190 completed successfully for exact4d2ef11e, including the full .NET, publisher and catalog jobs. Tenants current main CI41347795557 and Source-reference integration41347795093 are running for331c5282.

### Final owner repair source snapshot

All12 root modules were freshly fetched without recursive updates and match clean main/origin-main checkouts. EventStorea82555477689e3e021c3c74bcab0ccfefd4ff307 repairs the demonstrated inline-projection response boundary with default-off positively owned durable worker delivery, full authority/method preservation, bounded worker progress and persistent ABA fencing; it also fixes the exact Redis SAVE busy race without changing historic qualification outcomes. Tenants434fefb0f8140afc78d33747d398051e388fdf41 pins that pushed source without initializing its nested checkout, opts in only after existing fresh backend checks, and fixes disposed-provider access in five retained command flows. The independently arrived bootstrap transport commit5a21dd2c is preserved.

Three-layer combined review is fully triaged and accepted patches verified. Local parent server571/571, staged security246/246, backup-chain250/250 and UI745/745 cases pass without skips; final source AppHost build has zero warnings/errors. The credential-free local summary is release-evidence/final-owner-local-verification-2026-11-01.json. All nested checkouts remain absent. New exact owner main/Integration/source CI is running; the root snapshot is pushed now to overlap its fresh automated qualification. Final release remains pending all actual owner failures and exact root CI. Intermediate1.2.2 and green root2e do not qualify this new snapshot.

### Native authority endpoint correction and latest-main adoption

Run41361188323 proves the new deferred response path accepts bootstrap and completes its actual1event global-administrator projection, but its subsequent native source reads are refused19times because Tenants rewrites localhost to127.0.0.1. This mismatch violates the correct exact v2 runtime-coordinate guard before HTTP invocation. No native proof or timeout is relaxed. Credential-free attribution is release-evidence/tenants-native-endpoint-attribution-2026-11-01.json.

A concurrent owner push already corrected this boundary in114badd2 and added production registration/actual v2 controls;6b034a2d includes the service-surface claim correction. The parent preserved/adopted both via clean fast-forward. Fresh fetch again confirms all12 root modules equal clean remote main tips and every nested checkout absent. EventStorea825 mainCI41360994832 is successful; its published/live-sidecar Integration41360994325 remains running. Tenants434 main build, both package profiles, HTTP and complete blocking authenticated live/browser routes passed; its remaining Aspire job was canceled by the new upstream push. Final qualification now tracks Tenants6b034a2d main41362685408 and actualsource41362685140. Rootcfb is an intermediate snapshot and will not be released. The new root pin receives fresh exact-source CI before final publication.

### Owning gateway invocation budget

Actual Tenants6b source run41362685140 now passes owned bootstrap/current native authority, then CreateTenant receives500. Exact traces prove its unchanged5second SDK caller budget cancels the authoritative final-head read after two successful native HTTP200 reads. The separate2second metadata observation is not yet reached. The owning AppHost now supplies a bounded30second default while preserving explicit operator values; all shared SDK defaults, native proof/final checks, reader/test budgets and Dapr policies remain unchanged. Tenants020e40e68dca2d489d5341e0911e70baeb7d35c2 is pushed with this two-file correction.

Three-layer review is individually triaged; a preexisting standalone deployment-policy alignment is recorded in the ledger. Source build0warnings/errors, three actual offline publish-model default/override cases and five existing configured/caller cancellation controls pass without skips. Bounded attribution and local verification are recorded alongside the endpoint repair proof. New Tenants main41364760985/source41364760838 must qualify. EventStorea825 complete main CI and all auxiliary workflows are green; Integration41360994325 live-sidecar is now successful, published matrix remains running. Rootbdf is superseded; its ordinary build/test/generated/evidence checks passed and obsoleteG6 cancellation was requested so this current snapshot can qualify. The final release still waits for exact-source CI and the actual owner source/published matrix results. All12 refreshed main tips are clean and every nested checkout remains absent.

### API gateway attempt and final owner source cut

The Tenants020/ca93 actual source profile now completes and persists CreateTenant, but its first REST query is interrupted by the inherited API10second attempt before the independent native10s+2s authority work can finish. The final result was not captured in those failed runs, so bounded attribution explicitly preserves that uncertainty. Tenants 3a603db76f894c4ffacbae16683eb04f179d5f99 scopes the supported standard pipeline to its actual gateway client with30second attempt/unchanged30second total, required60second sampling and original resilience/discovery/bearer/Dapr ordering. The existing first-list assertion now includes only its safe closed result classification. No proof, caller/test deadline or successful state/metadata/lease assertion is relaxed.

All three independent review layers are fully triaged and patches verified. Five actual factory/transport cases pass in source and both current3.131.0/floor3.129.0 package profiles; source readiness6/6 pass. Builds have0warnings/errors. Four preexisting current-package readiness mock cases fail the SDK primary-transport guard and are reported separately, never relabeled as green. Upstreamca93 standalone deployment budget alignment is preserved and its mainCI41366387993 is all green. EventStorea825 all seven exact-source workflows, including full published compatibility and live-sidecar integration, are successful. Root695 exact CI/G6 is successful but intermediate. This current pin receives fresh rootCI and Tenants actual source qualification before final release. All12root main tips remain current and every nested checkout absent.

Actual source41370746958 at3a603db7 passes all functional assertions through persisted Redis state, proving the API correction. It fails the unchanged final zero-leases wait atline92 after120seconds. Logs prove a separate Default domain-query10second attempt during bootstrap and AdminApi10second attempt during SSR canceled real enrolled remote calls before later retry success. Existing Retain semantics correctly preserve ambiguity; no fence may be cleared to qualify. Final lease contents are not captured, so these are proven ambiguous transports, not an exhaustive lease enumeration. New spec-native-query-admin-transport-budget-2026-11-02.md scopes a named domain-query client and actual AdminApi policy correction, preserving30second totals/caller/native/proof/zero-lease gates. Root5c is superseded for final publication even if its ordinary CI passes. Publication remains frozen and no release was dispatched.

Upstream Tenants3a32b8d74f954832dd8426f5b0151205a6be4e47 arrived during this repair and is preserved via clean fast-forward. It replaces caller credentials before gateway retries and adds corresponding real-factory/header cases, without changing Contracts or the scoped EventStore transport files. Earlier3a60 mainCI passed five enabled jobs (HTTP/current/floor/live/build+unit), then its generic Aspire lane was canceled by this push. New3a32 main41372569103/source41372568646 is running against the still-old a825 pin; the final source cut must include the reviewed EventStore client policy fix and receive fresh qualification.

Root5c exactCI41370819648 completes all five enabled jobs successfully, including freshG6. New upstreamTenants3a32 actualsource41372568646 againstEventa825 completes the entire profile with6REST/conditional/Admin/refusal/persisted/zero-leases proof. Both exact results are preserved as bounded metadata/artifact evidence. This closes the prior worker/backup/flow/invocation repairs, while the already demonstrated intermittent10second native-query/AdminApi cancellation race is repaired separately before the final source cut. No retained fence, test deadline or automated qualification is waived.

### Final native-query/Admin source snapshot

EventStore0ace1545c433bf234f54cef3a57ac77d62b4175f preserves latest mergedbb1e93c57 and scopes standard30/30/60 only to a new actual domain-query named client and AdminApi. Original per-authority/Dapr-target partitioning, resilience/discovery/credential order, real catalog guard and retained ambiguity remain; Admin outer30s is explicitly preserved after standard registration. Three independent review layers are individually triaged; meaningful circuit mutation and fallback deadline checks close accepted gaps. Latest source builds0warnings/errors, parent172query/guard/privacy+15Admin and final inert-fixture6+staged-content1 pass without skips. Tenants2d5bd163 preserves latest3a32 credentials fix and pins0ace without initializing nested sources. New actualowner/source/rootCI must qualify this cut before final release.

Public EventStore3.132.0 arrived during final work atbb1 via its independent manualRelease41373402892. The already qualified consumer SDK cohort remains3.131.0/current and3.129.0/floor; Tenants explicitly refuses unknown stable cohorts until their archive/runtime qualification is added. This maintenance cut keeps that compatibility boundary rather than silently admitting a newly published unqualified cohort. All latest-main source changes are preserved.
