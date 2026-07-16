# Technology Currentness and Reality Review

- **Lens:** `verify tech current/reality checked`
- **Reviewed:** `ARCHITECTURE-SPINE.md` draft dated 2026-07-16
- **Review date:** 2026-07-16
- **Verdict:** **CHANGES REQUIRED** — the architecture's EventStore APIs, two-line host, Aspire ownership boundary, and future-runner claims are grounded in current code/package reality. Finalization should still resolve two release-facing version gaps and clarify that several Stack rows are observed catalog pins rather than selected target dependencies.

## Findings

### High — The Dapr runtime/SDK pair is tested locally but is not the pair in Dapr's published compatibility matrix

**Evidence**

- The root CI and release workflows pin the Dapr runtime to `1.18.0` (`.github/workflows/ci.yml`, lines 44 and 182).
- The centralized catalog pins `Dapr.Client`, `Dapr.AspNetCore`, and `Dapr.Workflow` to `1.18.4` (`references/Hexalith.Builds/Props/Directory.Packages.props`, lines 131-136).
- The spine's G-6 currently treats live restart/two-instance proof of `runtime 1.18.0 + SDK 1.18.4` as sufficient.
- Dapr's current support page identifies `runtime 1.18.0 + .NET SDK 1.18.1` as the packaged/tested 1.18 release tuple and says combinations outside its listed packaged releases are not supported. The same page separately states newer SDKs support the latest runtime and N-2, which leaves the patch-level `1.18.4/1.18.0` interpretation ambiguous rather than affirmatively supported: [Dapr supported runtime and SDK releases](https://docs.dapr.io/operations/support/support-release-policy/).
- `Dapr.Client 1.18.4` is a real current package and supports `net10.0`; existence/framework compatibility are not the issue: [Dapr.Client on NuGet](https://www.nuget.org/packages/Dapr.Client).

**Why it matters**

Durable admission, state, workflow, restart, and two-instance behavior are release-critical. A passing project fixture proves observed behavior but does not itself establish vendor supportability for the deployed combination.

**Action**

Amend G-6/AD-30 so release evidence must do one of the following: (a) pin a runtime/SDK tuple explicitly listed as supported by Dapr, or (b) carry an authoritative Dapr compatibility statement/approved support exception for `1.18.4 + 1.18.0` in addition to the live fixtures. Keep the existing restart/two-instance tests either way.

### High — FrontComposer 4.0.0 is no longer the current checked-out/published patch

**Evidence**

- The root catalog still resolves `HexalithFrontComposerVersion=4.0.0` (`references/Hexalith.Builds/Props/Directory.Packages.props`, line 8), matching the spine's Stack row.
- The checked-out FrontComposer changelog starts at `4.0.1` dated 2026-07-16 (`references/Hexalith.FrontComposer/CHANGELOG.md`). NuGet also resolves `Hexalith.FrontComposer.Contracts` latest stable to `4.0.1`.
- `dotnet-inspect diff Hexalith.FrontComposer.Contracts@4.0.0..4.0.1` found one additive public API change (`FcDiagnosticIds`) and no breaking public API change.
- The 4.0.1 changelog describes localization and identifier alignment for accessibility/governance compliance, directly adjacent to NFR-9 and the spine's fail-closed evidence posture.

**Why it matters**

The Stack introduction says it was verified against the checkout, but the package row reflects the root catalog rather than the current checked-out sibling release. That is a real package-mode/source-mode divergence on the day of finalization.

**Action**

Do not silently rewrite a sibling dependency. In the spine, mark `4.0.0` as the **root catalog/package-mode pin** and make G-3/G-6 require reconciliation to the approved FrontComposer revision/package before implementation. If repository-local approval chooses 4.0.1, update the Builds/root package pin and record the exact revision/evidence outside this architecture review.

### Medium — The Stack table mixes target architecture choices with migration-only or currently unused catalog entries

**Evidence**

- No Projects project references the package named `Dapr`; the catalog-only `Dapr=1.17.9` row is correctly called hygiene in G-6.
- No Projects project references `Dapr.Workflow`; the spine simultaneously defers the exact Durable Task platform package/scheduler. Listing `Dapr.Workflow 1.18.4` without an ownership/status qualifier can therefore be misread as selecting the deferred engine.
- `CommunityToolkit.Aspire.Hosting.Dapr` is used by the current Projects-owned AppHost/Aspire projects, but AD-24/AD-25 retire those projects after the platform runner exists. Its exact preview package is a brownfield migration baseline, not necessarily the platform runner's eventual implementation.
- `Fluxor.Blazor.Web` and Fluent UI are currently referenced from `Projects.Contracts`, a dependency that AD-16 explicitly removes. They remain relevant to the platform presentation lane but not to target `Projects.Contracts`.

**Action**

Either split Stack into **target bindings** and **observed brownfield/catalog baseline**, or add an Ownership/Status column such as `Projects target`, `platform-owned gate`, `migration-only`, or `catalog-only`. Explicitly state that the Dapr Workflow implementation remains deferred under G-1 and that current Projects AppHost integration packages do not constrain the platform runner.

### Medium — Three prerelease/stale pins need explicit compatibility disposition rather than an implied current-stable status

**Evidence**

- Fluent UI `5.0.0-rc.4-26180.1` exists, targets `net10.0`, and is the newest v5 prerelease; current stable is `4.14.3`. The spine already handles this appropriately in G-6: [Fluent UI Blazor package versions](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components).
- `CommunityToolkit.Aspire.Hosting.Dapr 13.4.0-preview.1.260602-0230` exists and targets `net10.0`, but is a preview integration. The platform extraction gate should own its compatibility disposition.
- The catalog pins `NSubstitute 6.0.0-rc.1`, while current stable is `6.0.0`; `dotnet-inspect diff` reports no public API changes between them.
- The catalog pins `Fluxor.Blazor.Web 6.9.0`, while current stable is `6.10.0`; `dotnet-inspect diff` reports no public API changes between them.

**Action**

Keep the Fluent RC warning already present. Add the CommunityToolkit preview to the platform compatibility gate. For NSubstitute and Fluxor, either update through `Hexalith.Builds` under repository-local approval or label the Stack rows as observed pins with an intentional-hold/revisit condition. These are package-catalog actions, not reasons to change domain architecture.

### Medium — API evidence must use the published target package or a clean rebuild, not the pre-existing local Release DLLs

**Evidence**

- The pre-existing local EventStore Release DLLs report assembly version `3.63.0` and predate later source changes.
- The checked-out EventStore changelog and central pin are `3.67.3`.
- Inspection of the published `Hexalith.EventStore.DomainService@3.67.3`, `Client@3.67.3`, `Aspire@3.67.3`, and `Testing.Integration@3.67.3` packages confirms all APIs used by the spine.

**Action**

Make G-1/G-4 evidence commands restore the pinned package or clean-build the checked-out revision before inspecting/running it. A pre-existing `bin/Release` assembly must not count as version evidence. The spine's clean-checkout language is already close; the evidence matrix should make the command explicit.

### Low — The SDK row is a baseline pin, while the effective SDK is the latest patch

`global.json` pins `10.0.300` with `rollForward: latestPatch`, and this environment actually resolves `dotnet --version` to `10.0.301`. That is valid and C# 14 remains the language version. For precision, describe the Stack row as `10.0.300 feature-band baseline; effective latest patch (10.0.301 during review)`. Microsoft's current .NET 10 page lists SDK 10.0.301 as current and .NET 10 as active LTS: [.NET downloads](https://dotnet.microsoft.com/en-us/download), while the 10.0.300 download page confirms C# 14 support: [.NET 10.0 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

## Assertions verified without a finding

| Spine assertion | Verification |
| --- | --- |
| `Hexalith.EventStore.DomainService` package line `3.67.3` | Current root pin, current checked-out changelog, and current published package all resolve to 3.67.3. |
| Explicit-assembly two-line host | `AddEventStoreDomainService(params Assembly[])` and `UseEventStoreDomainService(WebApplication)` exist in published 3.67.3 and in checked-out source; the overload documentation explicitly describes separate host/domain assemblies. |
| Domain query seam | `IDomainQueryHandler` exists in 3.67.3 with `Domain`, `QueryType`, and async execution. |
| Incremental vs full-replay projections | `IAsyncDomainProjectionHandler` is the named async persistence seam; `IDomainProjectionHandler` remains the stateless full-replay compatibility seam exactly as AD-14 states. |
| Read-model seams | `IReadModelStore`, `IReadModelBatchStore`, `ReadModelWritePolicy`, and freshness types exist in `Hexalith.EventStore.Client@3.67.3`. |
| Cursor seam | `IQueryCursorCodec` and `QueryCursorScope` exist. The codec uses ASP.NET Core Data Protection and binds query type plus caller-built scope; Tenant/actor/filter fields must be included by the Projects query implementation as AD-14 requires. |
| Aggregate seam | `EventStoreAggregate<TState>` and `DomainResult` exist in current 3.67.3 packages. |
| Aspire domain-module composition | `AddEventStoreDomainModule(...)` exists in `Hexalith.EventStore.Aspire@3.67.3`. Aspire 13.4.6 is the current stable NuGet package, the installed CLI is 13.4.6, and 13.4 officially supports first-class C# AppHosts: [What's new in Aspire 13.4](https://aspire.dev/whats-new/aspire-13-4/). |
| EventStore integration test support | `DaprDomainServiceTestFixtureBase` and `AspireTopologyFixtureBase<TAppHost>` exist in `Hexalith.EventStore.Testing.Integration@3.67.3`. |
| Durable Task / Confirmation Artifact gap | Searches across current EventStore DomainService, Client, and Server 3.67.3 packages find no `*Durable*` or `*Confirmation*` public type. G-1 correctly treats this as absent rather than implemented. |
| Future module runner/evidence tool | `.config/dotnet-tools.json` and `module/hexalith-projects.module.json` are absent; no `hexalith-module` or `hexalith-evidence` implementation exists outside planning artifacts. The spine correctly labels the commands as target contracts, not current capabilities. |
| Brownfield technical layers | Current `Projects.Server`, Infrastructure, Workers, AppHost, Aspire, ServiceDefaults, UI, MCP, and CLI runtime projects still exist. AD-24/AD-25 preserve them until platform parity, so the target does not strand repository-local run/test capability. |
| Testing packages | `xunit.v3 3.2.2`, `Shouldly 4.3.0`, and the pinned NSubstitute prerelease all exist and are framework-compatible; only the NSubstitute currentness disposition above remains. |
| Other exact pins | `ByteAether.Ulid 1.3.8` and `NSwag.MSBuild 14.7.1` are current stable packages. Aspire 13.4.6 and Dapr .NET 1.18.4 are current packages. |

## Reproduction commands used

```text
dotnet --version
dotnet --info
aspire --version
dapr --version
dotnet msbuild src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj -nologo \
  -getProperty:TargetFramework -getProperty:LangVersion \
  -getProperty:HexalithEventStoreVersion -getProperty:HexalithFrontComposerVersion

dnx dotnet-inspect -y -- package Hexalith.EventStore.DomainService@3.67.3 -v:q
dnx dotnet-inspect -y -- member EventStoreDomainServiceExtensions \
  --package Hexalith.EventStore.DomainService@3.67.3 --oneline
dnx dotnet-inspect -y -- type --package Hexalith.EventStore.DomainService@3.67.3 \
  -t 'I*Domain*Handler' --shape --oneline
dnx dotnet-inspect -y -- type --package Hexalith.EventStore.Client@3.67.3 \
  -t 'IReadModel*' --shape --oneline
dnx dotnet-inspect -y -- type --package Hexalith.EventStore.Client@3.67.3 \
  -t '*QueryCursor*' --shape --oneline
dnx dotnet-inspect -y -- type --package Hexalith.EventStore.Testing.Integration@3.67.3 \
  -t '*FixtureBase*' --shape --oneline
dnx dotnet-inspect -y -- member HexalithEventStoreDomainModuleExtensions \
  --package Hexalith.EventStore.Aspire@3.67.3 --oneline
dnx dotnet-inspect -y -- diff \
  --package Hexalith.FrontComposer.Contracts@4.0.0..4.0.1 --oneline
dnx dotnet-inspect -y -- diff --package Fluxor.Blazor.Web@6.9.0..6.10.0 --oneline
dnx dotnet-inspect -y -- diff --package NSubstitute@6.0.0-rc.1..6.0.0 --oneline
```

No spine or implementation file was changed by this reviewer.

## Recheck

**Disposition: PASS (2026-07-16).** All six findings are resolved at architecture-spine level:

- G-6 now blocks release until the Dapr runtime/SDK tuple is support-matrix-listed or has a platform-owner-approved exception, while retaining restart/two-instance proof.
- Stack and G-3 distinguish the root FrontComposer package-mode pin `4.0.0` from checked-out/published `4.0.1` and require an explicit parity disposition; local `CHANGELOG.md` and NuGet both report 4.0.1, whose 4.0.0→4.0.1 public API diff is additive only.
- Stack now separates target/compatibility bindings from migration/catalog-only rows: CommunityToolkit and Fluxor are migration inputs, Dapr.Workflow is unselected pending G-1, and `Dapr=1.17.9` is catalog hygiene.
- G-6 explicitly gates Fluent UI RC4, CommunityToolkit preview, and NSubstitute RC, and requires Fluxor 6.9 upgrade or an intentional Builds-governed hold.
- EventStore evidence is pinned to published 3.67.3 packages or a clean 3.67.3 build; reinspection confirmed the host extensions, read-model seams, and integration fixtures, while G-1 rejects stale local binaries.
- The SDK row now states the `10.0.300` feature-band policy with `latestPatch` and the reviewed effective SDK `10.0.301`.

No remaining technology/current-reality blocker was found; the unresolved external capabilities remain correctly represented as fail-closed entry gates rather than implemented features.
