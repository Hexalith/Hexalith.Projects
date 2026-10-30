# Exact-candidate qualification plan

Status: proposed, not executed. Selected Builds release: 4.38.3 / `d1931b1e803144048d927fff16cb8119afde541e`. This plan grants no publication, deployment or consumer adoption authority.

## 1. Apply and record the owner correction under separate authority

Authorize applying `platform-tuple-correction.UNAPPLIED.patch` and `platform-direct-apphost-alignment.UNAPPLIED.patch` in Platform. Include the existing four analyzer corrections unchanged. Resolve the additional owner CI artifact-binding prerequisite below before selecting the final candidate. Record the candidate only with explicit staging/commit authorization and normal commit validation. Capture its real full `git rev-parse HEAD`; never predict a SHA. Any later content change requires a new candidate and evidence.

The dependency changes are exactly the Builds gitlink, tool/floor 4.38.3 and the three file-host package directives in the patches. No Builds catalog/source changes are proposed. Catalog-controlled Tenants remains 5.11.1, Conversations 1.3.1, EventStore 3.128.4 and FrontComposer 4.7.1. Do not substitute current sibling e909ffd… or either historical gitlink patch.

## 2. Establish the qualifying owner environment

Use a standalone Platform checkout or an approved owner CI workspace whose top-level `.gitmodules` directly declares its dependencies. Supply the real exact Builds release checkout and tag objects there; leave the current umbrella's nested submodules uninitialized. All dependency HEADs and recorded gitlinks must match. A missing tag must be solved with legitimate owner release context, not synthetic tags or temporary index changes. Fetch/install operations belong to the separately approved environment, not this preparation pass.

Identify the trusted CI workflow/repository and required integration suite before dispatch. No Platform workflow is tracked and no current-SHA run is observed, so a concrete existing owner pipeline cannot be assumed. The owner must provide or implement that entry point before claiming candidate acceptance. Builds producer CI does not qualify Platform.

Bind exact Platform candidate SHA, Builds d1931b1…, CLI 4.38.3 plus archive/Tooling hashes, embedded runtime catalog and evaluated pinned package catalog (including conditional entries). Retain restored package locks/assets and expected/loaded artifact content identities. Verify real SDK (Platform pin 10.0.402 with latestPatch roll-forward), Aspire CLI/SDK/Hosting 13.6.4, Toolkit 13.6.2-beta.959, Dapr CLI 1.18.2/runtime 1.18.7 and required Docker/isolated Dapr environment. No runtime prerequisites were installed or qualified here.

## 3. Isolated owner checks, then fresh CI package integration

Run existing owner tests individually by project. For local diagnostics only, use Debug/project references in the exact approved source environment:

```bash
dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj -c Debug -m:1
dotnet tests/Hexalith.Platform.Composition.Tests/bin/Debug/net10.0/Hexalith.Platform.Composition.Tests.dll -method Hexalith.Platform.Composition.Tests.DomainEnrollmentKitTests.AcceptedToolPinEqualsSupportedRangeFloorAsync
```

Require the owner-pin test to compare the recorded Builds gitlink with genuine `v4.38.3^{commit}` successfully. Rerun the four analyzer classes with the exact command in COMMANDS.md against newly built exact-source outputs. Local checks remain diagnostics even if green. Existing environment-specific version overrides in the old analyzer build receipt are not candidate/release identities.

For CI, restore/build the `.slnx` in Release with package mode and no foreign Hexalith project fallbacks. The following commands alone are **insufficient** to achieve that: `src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj` still adds a Builds.Tooling ProjectReference whenever `BuildsToolingAssemblyPath` is empty, and `tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj` unconditionally references the sibling Builds.Module.EventStoreHost project with alias `buildsseed`. Bind the supported `BuildsToolingAssemblyPath` to the authenticated 4.38.3 archive's exact Tooling DLL and verify its complete dependency closure. The owner must also implement or identify an approved Release artifact/package binding for the unconditional host reference; no supported override exists in that test project today. This is a separate build-configuration prerequisite outside the two tuple patches. Merely setting `UseHexalithProjectReferences=false` does not clear it. Include any approved configuration correction in the recorded candidate, then inspect the evaluated/restored CI graph to reject foreign source fallback.

Run every required test project separately; preserve counts, failures and skips. Example build commands **only after** those bindings and the real candidate/environment are established:

```bash
dotnet restore Hexalith.Platform.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false
dotnet build Hexalith.Platform.slnx -c Release --no-restore -m:1 -p:UseHexalithProjectReferences=false
```

The owner must specify its required integration suite and launch declarations; no unimplemented runner command is invented here. Exercise generated module-tool composition and the aligned direct file-host entry separately if both are accepted entry points. Require real EventStore/Tenants and other enabled services, authenticated boundaries, readiness/startup tasks, expected/loaded content matching, required nonempty test coverage, shutdown and isolated cleanup evidence. A metadata-only Composition test pass cannot replace these gates.

Follow Platform candidate-and-attachment-policy-v1.md and architecture AD-4/AD-5. PR decisions bind the committed merge result plus exact head/base and authenticated gate request. Only the latest attempt can qualify that pending decision; use disposable GitHub-hosted package-mode CI, Release artifacts from the exact candidate, no evidence reuse or invented request IDs. A release requires its own selected exact committed revision, new authenticated decision and fresh run, even if source bytes match the PR candidate. If these producers/bindings are unavailable, record that blocker and refuse acceptance.

## 4. Publication remains a separate authorization

Only after owner integration acceptance, obtain explicit publication authority. Verify resulting official Platform tag/peeled full source, its exact Builds gitlink and range, any produced package/source identities and the matching fresh CI decision. A tag alone is not runtime/package qualification. Do not create a placeholder version, fabricate package artifacts or dispatch release during preparation.

## 5. Adopt and qualify consumers under separate authority

Authorize Projects' committed Platform gitlink to the newly verified official Platform identity and its Builds gitlink/checkout to exact d1931b1…. Use an exact 4.38.3 tool invocation; any participating manifest such as Conversations' currently prepared 4.38.2 must be separately updated before it can satisfy the new floor. Reconcile other actual checkout/gitlink dependencies without nested initialization. Review CI/release execution-source pins separately from catalog/tool selection; the existing workflow SHAs grant no tuple acceptance and are not automatically replaced by this proposal.

From adopted clean Projects, restore the approved exact CLI and run public preparation into fresh absolute scratch output:

```bash
hexalith-module prepare-build --manifest module/hexalith.module-manifest.v2.json --mode package --workspace "$QUALIFICATION_SCRATCH" --output json
```

Require exit 0, no HXW009/HXW011, exact committed Platform/Builds/catalog identities and the emitted environment unchanged. No handwritten mapping, toolVersionOverride, release-tag shim or project fallback is permitted. Retain generated `catalog-versions.json` and prove all Tenants selections are 5.11.1; verify runtime subset separately. Restore/build consumer Release artifacts using that environment in CI; inspect actual assets for package-only references and exact selected versions.

Refresh `tools/release-packages.json` only with explicit authorization and the real per-package dependency closures. The current 46 common-ID differences are a starting discrepancy list, not a complete replacement manifest. Keep the five Projects packages on one actual approved candidate version/source, using existing packaging and validation commands with a new empty output directory:

```bash
python3 scripts/pack-release-packages.py "$CANDIDATE_PACKAGES" "$PROJECTS_CANDIDATE_VERSION"
python3 scripts/validate-nuget-packages.py "$CANDIDATE_PACKAGES" --expected-version "$PROJECTS_CANDIDATE_VERSION" --expected-source-sha "$PROJECTS_CANDIDATE_SHA"
python3 scripts/validate-consumer-package-references.py "$CANDIDATE_PACKAGES"
```

These shell variables must be populated from the recorded/adopted candidate and fresh output paths; they are unresolved future inputs, not values to guess. Packaging is local artifact creation and does not authorize publication. The first script removes existing archives from its output directory, so use a new directory. Preserve all prior archives.

The existing `validate-consumer-package-references.py` deletes each consumer's temporary assets/output on exit. Its console pass cannot supply the retained graphs required here. Before final acceptance, authorize an evidence-retention extension to that script (or an approved equivalent consumer probe) that copies each exact per-package `project.assets.json`, restore/build logs, probe source/inputs and package archive hashes to a fresh durable evidence directory **before** temporary cleanup. The extension is a pending prerequisite, not an existing `--evidence` option. Retain sanitized records only. Also explicitly resolve the observed FrontComposer.Shell → FrontComposer.Contracts.UI >=4.7.1 edge: that dependency is absent from the catalog's exact version map and cannot be accepted from the 195 known-range checks alone.

Require exactly Contracts, core Projects, Client, Testing and ServiceDefaults `.nupkg` plus their matching `.snupkg`; bind source SHA/version, NuSpec dependencies, target frameworks, compile assets, portable PDB/document checksums and SourceLink. Existing NuGet validation primarily checks `.nupkg`; add or identify explicit symbol verification before claiming all-symbol acceptance. Run consumer probes with candidate packages and official published dependencies only, inspect restored libraries for no project fallback, execute appropriate API/store/gateway acceptance against the adopted tuple and retain end-state/cleanup evidence. Earlier source/store/full-suite results are not reusable acceptance for these changed inputs. Story 6.4 remains in progress until its remaining delivery gates actually pass.
