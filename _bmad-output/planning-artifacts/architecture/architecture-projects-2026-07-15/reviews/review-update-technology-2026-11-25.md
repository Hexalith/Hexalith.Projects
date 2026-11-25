# Reviewer Gate (Finalize after Update): technology-currency lens, 2026-11-25

**Target:** `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (updated 2026-11-25), compared with the previous revision and memlog lines 110 to the end.
**Lens:** every committed technology decision is checked against the web, the existing project or the current pins, not asserted from memory.

**Verdict: PASS WITH FINDINGS.** Every version in the Stack table is current. Each one matches the committed Builds catalog and nuget.org or GitHub releases. Today's claims about EventStore, Dapr 1.19.2 and the Aspire 17.0.0 deprecation all check out. The weak points are in how the SDK works in practice. Discovery is per assembly, and a keyed `IAsyncDomainProcessor` outranks a keyed `IDomainProcessor`, so the single-writer rule has a hole. A second aggregate is a new EventStore domain that has to be declared in the route catalog. And no lane can run the Dapr 1.19.2 evidence rule today.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 1 |
| Medium | 2 |
| Low | 5 |

How each item was checked:
- **Project pins:** `global.json`, `Directory.Packages.props`, `Directory.Build.props` and `.config/dotnet-tools.json`.
- **Builds catalogs:** read with `git show` at `bf8a28b4` (the committed gitlink, tag v6.8.1), at `763fc0e9` (the Platform-pinned catalog, v6.7.0) and at `v6.8.2`.
- **nuget.org:** the flat-container index for each package. For `Hexalith.EventStore.DomainService` 5.5.1 and 5.5.2, `Aspire.AppHost.Sdk`, `Aspire.Hosting` and `Aspire.Hosting.Dotnet` 17.0.0, and `Hexalith.McpCli.Abstractions` 3.0.0, the package itself was downloaded and its contents scanned.
- **Releases:** GitHub releases for dapr/dapr, dapr/cli, dapr/dotnet-sdk and Hexalith/Hexalith.EventStore, plus the .NET 11 release metadata.
- **Source:** EventStore at the committed gitlink `208af26a`, Projects `src/`, `tools/qualification/` and CI workflows, and the log of the latest CI job.
- **Not used:** `~/.nuget/packages`.

---

## High

### T-H1: SDK discovery cannot keep the current processor as the only `projects` writer once `Hexalith.Projects` is scanned

- **Severity:** High. **Status:** CONFIRMED. The mechanism is in the code, and the spine itself requires the placement that triggers it.
- **Spine anchors:**
  - AD-17 line 249: "The SDK writer gate requires one convention-discovered `projects` aggregate deriving from `EventStoreAggregate<TState>`".
  - AD-17 line 251: "Until then deployed environments keep the current processor … A host build registers exactly one keyed processor per Projects aggregate type".
  - AD-24 line 316: "`Hexalith.Projects` … holds every `IDomainQueryHandler`, projection and read-model adapter (new ones from Story 7.1, existing Server residents when a story touches them, all before the writer cutover)".
  - Structural Seed lines 474 and 486: `Hexalith.Projects` holds the "Project and Folder-claim aggregates", and the target host is `AddEventStoreDomainService(typeof(ProjectAggregate).Assembly)`.
- **Evidence:**
  - `references/Hexalith.EventStore@208af26a:src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs:807-818`: aggregates (`AddEventStore(domainAssemblies)`, line 812), query handlers (`AddDomainQueryHandlers(builder.Services, domainAssemblies)`, line 815) and projection handlers (line 818) are all discovered from the same assembly list.
  - `src/Hexalith.EventStore.Client/Registration/EventStoreServiceCollectionExtensions.cs:272-291`: each discovered aggregate is registered keyed by its domain name, both as `IDomainProcessor` and as `IAsyncDomainProcessor`.
  - `src/Hexalith.EventStore.DomainService/DomainServiceRequestRouter.cs:177-178` looks up `GetKeyedService<IAsyncDomainProcessor>(request.Command.Domain)` first. The keyed `IDomainProcessor` (line 220) is used only when no async processor exists. Registration order therefore does not matter.
  - Projects `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs:43`: the class implements `IDomainProcessor, IAsyncAggregateReplay, …`, with no `IAsyncDomainProcessor`. `ProjectsServerServiceCollectionExtensions.cs:174` registers it with `AddKeyedTransient<IDomainProcessor>(ProjectsServerModule.DomainName, …)`. Line 62 calls `builder.AddEventStoreDomainService(typeof(ProjectsDomainProcessor).Assembly)`.
- **Failure scenario:**
  1. A story moves a deployed query handler into `Hexalith.Projects` before the cutover, as AD-24 requires.
  2. To keep that handler discoverable, the deployed host has to pass `Hexalith.Projects` to `AddEventStoreDomainService`.
  3. Discovery then also registers the SDK `projects` aggregate and the claim aggregate.
  4. The router sends every `projects` command to the SDK aggregate. The writer switches in production with no fence, no drain, no snapshot invalidation and no AD-17 record.
  5. Admin replay can also fail on two keyed `IAggregateReplay` registrations for the same domain (router lines 349-354, "ambiguous replay ownership").
- **Proposed fix:**
  - In the AD-17 **Writer cutover** paragraph, after "A host build registers exactly one keyed processor per Projects aggregate type, …", add: "SDK discovery is per assembly, and a keyed `IAsyncDomainProcessor` outranks a keyed `IDomainProcessor`. Until the writer cutover, no assembly that a deployed host passes to `AddEventStoreDomainService` contains an `EventStoreAggregate<TState>` for a Projects domain, and a host-build test asserts which processor resolves for each Projects domain."
  - Add a Deferred item: "Where the SDK `projects` and claim aggregates live before the writer cutover while AD-24 moves deployed handlers into `Hexalith.Projects` (owner: Projects Server; EventStore if an SDK processor-selection option is needed)."

## Medium

### T-M1: The AD-35 claim aggregate is a new EventStore domain that must be declared for routing; the spine says only "same DomainService"

- **Severity:** Medium. **Status:** CONFIRMED.
- **Spine anchor:** AD-35 line 423: "The claim is a second Projects aggregate type in the same DomainService, one stream per `(TenantId, FolderId)`, so per-aggregate serialization makes acquisition atomic". The Structural Seed module manifest row (line 479) is also affected.
- **Evidence that the claim itself holds:**
  - `AssemblyScanner.cs:94-106` discovers every concrete `EventStoreAggregate<>` and rejects duplicate domain names.
  - The keyed registration is per domain (`EventStoreServiceCollectionExtensions.cs:286-291`).
  - A comment in `DomainServiceResolver.cs:107-112` names "sample apps that host multiple domains under one DAPR app-id".
  - `AggregateIdentity.cs:55` defines `ActorId => $"{TenantId}:{Domain}:{AggregateId}"`, so serialization is per (Tenant, claim domain, FolderId).
  - `CommandStatusRecord.CommittedEventSequence` has been present since published v5.5.1, so the generation (the stream version) can be observed.
- **Evidence of the gap:**
  - `references/Hexalith.EventStore@208af26a:src/Hexalith.EventStore.Server/DomainServices/DomainServiceResolver.cs:75-86`: the catalog-enforced resolver accepts only a route whose declaration has `declaration.Domain == domain && declaration.Kind == "command"`. Otherwise it throws `RouteCatalogUnavailableException("A domain-only lookup cannot select an undeclared or ambiguous committed target.")`.
  - Lines 200-213 of the same file: without a catalog, the resolver falls back to `AppId: domain`.
  - `NamingConventionEngine` derives a kebab-case domain from the type name by stripping suffixes, unless `[EventStoreDomain]` is set. It also derives a per-domain topic, `{tenantId}.{domain}.events`.
  - Projects `src/Hexalith.Projects.Server/ProjectsRuntimeCatalogDeclarations.cs:94-114`: every command, query and projection declaration, including the per-command idempotency intent `"hexalith:projects:" + id + ":intent-v1"`, is bound to `ProjectsServerModule.DomainName`, which is `"projects"` (`ProjectsServerModule.cs:21`).
- **Failure scenario:**
  1. A `ProjectFolderClaimAggregate` resolves to the domain `project-folder-claim`.
  2. In catalog mode, the acquire and release commands fail as undeclared targets. In convention mode, the server invokes a Dapr app called `project-folder-claim`, which does not exist.
  3. Without an idempotency intent declaration, "An equivalent retry on any replica recovers the original receipt by task/step" has no admission record behind it.
  4. The Story 7.1 claim-race proof fails late, in topology.
- **Proposed fix:** append to the AD-35 Rule: "Its EventStore domain name is fixed by attribute, and its command, query and replay routes and idempotency intents are declared in the single Projects route-catalog authority and the module manifest against the `projects` app ID."

### T-M2: No lane can run the Dapr 1.19.2 evidence rule

- **Severity:** Medium. **Status:** CONFIRMED.
- **Spine anchor:** Stack Dapr row, line 460: "AD-15 serialization, AD-17 fencing and AD-35 claim evidence needs runtime `1.19.2` or later, or an owner risk acceptance per affected row." Gate G-6 (line 553) is also affected.
- **Evidence:**
  - `tools/qualification/run_g6_current.py:186-192` accepts only the Dapr pairs `("1.19.1", "1.19.1")` and `("1.18.0", "1.18.2")`. Anything else raises "No explicit G6 installation route for the selected Dapr pair".
  - Lines 212-215 of the same script map runtime image digests only for `1.18.2` and `1.19.1`.
  - `.github/workflows/ci.yml:123-124` and `:377-378` pin `runtime-version: '1.19.1'`.
  - Builds `bf8a28b4:Props/dapr-toolchain-sha256.json` says "The 1.19.2 runtime archives were verified on 2026-11-20", and the catalog sets `HexalithDaprRuntimeVersion` to `1.19.2`.
  - GitHub releases: dapr/dapr `v1.19.2` (2026-11-19) is the latest runtime, and dapr/cli `v1.19.1` is the latest CLI. On nuget, `Dapr.Client` 1.19.1 is the latest.
- **Failure scenario:** the Story 7.1 claim-race proof and the AD-17 fencing evidence can only be produced on 1.19.1. Either every affected row falls back to an owner risk acceptance, so the exception becomes the normal path, or the rows stay open and block the first writer while nobody owns the missing lane.
- **Proposed fix:** in the Dapr row, replace the sentence with: "AD-15 serialization, AD-17 fencing and AD-35 claim evidence needs runtime `1.19.2` or later from a lane that runs it (the G-6 qualifier and CI admit a CLI `1.19.1` / runtime `1.19.2` pair, for which Builds already carries verified digests), or an owner risk acceptance per affected row."

## Low

### T-L1: The FrontComposer row hides that 6.1.0 is outside G-6, and that the CLI bump is already available

- **Severity:** Low. **Status:** CONFIRMED.
- **Spine anchors:** line 457, "FrontComposer CLI `6.0.1` output is not evidence for `6.1.0` descriptors until the tool is bumped", and line 465, the generic catalog-delta rule. The distill dropped the previous sentence "so G-6 evidence does not cover FrontComposer `6.1.0`".
- **Evidence:**
  - `.config/dotnet-tools.json` pins `"hexalith.frontcomposer.cli": { "version": "6.0.1" }`.
  - nuget `hexalith.frontcomposer.cli` lists 6.1.0 as published.
  - `ci.yml:161-164` runs the "FrontComposer inspect gate", which goes through `tests/tools/run-frontcomposer-inspect-gate.ps1:28-34` and calls `dotnet tool run frontcomposer -- inspect …` on every build.
  - The G-6 catalog `763fc0e9` sets `HexalithFrontComposerVersion` to `6.0.1`. The committed catalog `bf8a28b4` sets `6.1.0`.
- **Failure scenario:** a green CI inspect gate, or a green G-6 run, gets read as evidence for the `6.1.0` descriptors that the row lists.
- **Proposed fix (row text):** "FrontComposer `6.1.0` is outside the G-6 catalog (`6.0.1`). The CI inspect gate runs the tool-manifest CLI, so its output is evidence for `6.1.0` descriptors only once `.config/dotnet-tools.json` pins the published `hexalith.frontcomposer.cli` `6.1.0`."

### T-L2: The Aspire 17 deprecation note is correct but imprecise about the replacement

- **Severity:** Low. **Status:** CONFIRMED.
- **Spine anchor:** line 459: "It deprecates `Aspire.AppHost.Sdk` and `AddProject` (ASPIRE012, CS0618), which the AppHost suppresses; new AppHost code uses `Microsoft.NET.Sdk` with path-based registration."
- **Evidence:**
  - `Aspire.AppHost.Sdk` 17.0.0, `Sdk/Sdk.targets`, ASPIRE012: "uses the Aspire.AppHost.Sdk, which is obsolete but remains available for staged migration. Switch the project's Sdk attribute to Microsoft.NET.Sdk, add explicit PackageReference items for Aspire.Hosting.AppHost and Aspire.Hosting.Dotnet, and migrate C# resource ProjectReference items to AddDotnetProject calls."
  - `Aspire.Hosting` 17.0.0 obsolete message: "Use AddDotnetProject(name, path) from the Aspire.Hosting.Dotnet package instead. There is no generic AddDotnetProject<TProject>; pass the project path instead of the TProject type."
  - In Projects, `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj:1,8,9` uses `Aspire.AppHost.Sdk/17.0.0`, `NoWarn ASPIRE012` and `SuppressAspireAppHostSdkObsoleteWarning`. `tests/Hexalith.Projects.PackageFixtures.AppHost` is on the same SDK.
  - `Directory.Build.props:65` lists CS0618 only in `WarningsNotAsErrors`, so the warning is still reported, not suppressed.
  - `src/Hexalith.Projects.AppHost/Program.cs:31` calls `AddDotnetProject(..., new Projects.Hexalith_EventStore().ProjectPath)`. The path comes from generated `Projects.*` metadata, which most likely depends on the obsolete SDK's ProjectReference handling (PLAUSIBLE).
- **Failure scenario:** "path-based registration" is taken to be satisfied by `new Projects.X().ProjectPath`, and moving to `Microsoft.NET.Sdk` breaks those registrations later.
- **Proposed fix (row text):** "It deprecates `Aspire.AppHost.Sdk` (ASPIRE012, which the AppHosts suppress) and `AddProject` (CS0618, kept as a warning); new AppHost code uses `Microsoft.NET.Sdk` with explicit `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet` references and `AddDotnetProject(name, path)` with a literal project path."

### T-L3: The Dapr 1.19.2 evidence rule leaves out the release notes' caveat about state-store topology

- **Severity:** Low. **Status:** PLAUSIBLE. The production state-store topology is owned by the platform and was not visible in the repository.
- **Spine anchors:** line 460 (Dapr row) and AD-28 line 342 (the platform owns "Dapr component/provider binding").
- **Evidence:**
  - dapr/dapr v1.19.2 release notes, "State stores did not enforce ETags and first-write consistently": "On Redis Cluster, a transaction is atomic only when all of its keys share a hash tag … Otherwise the component runs one script for each hash slot, and an ETag mismatch in one slot can leave the writes of other slots applied."
  - In the same notes, the PostgreSQL change is limited to "A delete with an ETag no longer matches an expired row."
  - The G-6 qualifier uses a single-node `redis:7.4` (`run_g6_current.py:223`) and PostgreSQL.
- **Failure scenario:** AD-15 and AD-35 evidence passes on single-node Redis or PostgreSQL, while production binds Redis Cluster, where actor-state transactions that span slots are not atomic.
- **Proposed fix:** append to the Dapr row: "Record the state-store component and topology tested; single-node results do not qualify a Redis Cluster binding."

### T-L4: The .NET SDK pin will break at the first servicing release, and no owner is named

- **Severity:** Low. **Status:** PLAUSIBLE. The timing follows the usual monthly .NET servicing schedule.
- **Spine anchor:** line 456: "G-6 requires exactly `11.0.100`, so a newer patch picked up by `latestPatch` fails G-6 until the policy changes."
- **Evidence:**
  - The .NET 11 release metadata (`builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json`) gives `latest-release` 11.0.0 (2026-11-10) and `latest-sdk` 11.0.100. The pin is current today.
  - `run_g6_current.py:204-206` fails when the policy's `dotnetSdk` value does not appear in the `dotnet --version` output.
  - Both the build-and-test and G-6 jobs use `setup-dotnet` with `global-json-file` (`ci.yml:157-159` and `:193-195`).
- **Failure scenario:** the first .NET 11 servicing SDK reaches the runners, and G-6 then fails on every revision until someone notices.
- **Proposed fix:** append to the row: "The G-6 policy owner revises the SDK pin with each .NET servicing release."

### T-L5: The Module.Cli pin's correction target is ambiguous

- **Severity:** Low. **Status:** CONFIRMED.
- **Spine anchor:** line 506: "Module.Cli and Evidence.Cli match the pinned Builds release or its selected published version … and the Module.Cli `6.4.2` pin is corrected or explained."
- **Evidence:**
  - `Directory.Packages.props:6` sets `<HexalithBuildsModuleCliVersion>6.4.2</HexalithBuildsModuleCliVersion>`.
  - The pin is used only through `PackageDownload Include="Hexalith.Builds.Module.Cli" Version="[$(HexalithBuildsModuleCliVersion)]"` with a HintPath to `Hexalith.Builds.Tooling.dll`, in `tests/Hexalith.Projects.Integration.Tests/*.csproj:34-36` and `tests/Hexalith.Projects.PackageFixtures.AppHost/*.csproj:33-35`.
  - The committed Builds gitlink `bf8a28b4` is exactly tag `v6.8.1`.
  - nuget lists `Hexalith.Builds.Module.Cli` and `Hexalith.Builds.Evidence.Cli` at both 6.8.1 and 6.8.2 (the latest).
  - The memlog M25 note "(6.8.2 published)" invites a bump ahead of the gitlink.
- **Failure scenario:** the pin is moved to 6.8.2. That is newer than the release the gitlink pins, so it still breaks the rule, while the two test projects keep loading Tooling from a version other than the one CI builds.
- **Proposed fix:** "…and the Module.Cli `6.4.2` package pin (Tooling for two test projects) moves to the version of the Builds release the gitlink pins, or its reason is recorded."

---

## What held up

| Spine claim | Verified against | Result |
| --- | --- | --- |
| .NET SDK `11.0.100` / `latestPatch` / `net11.0` / `LangVersion=latest` | `global.json`; `Directory.Build.props`; .NET 11 release metadata (GA 2026-11-10, latest SDK 11.0.100) | Current |
| EventStore `5.5.1`; `IDomainQueryResponsePolicy` absent through `5.5.2` and first added in untagged `208af26a`; CI fails with CS0246 | Both catalogs set `HexalithEventStoreVersion` to 5.5.1. On nuget, 5.5.2 is the latest version and its GitHub release is dated 2026-11-24. The 5.5.1 and 5.5.2 `DomainService.dll` files contain 0 occurrences of the type. `git log -S` finds it first in `208af26a` (`describe` gives v5.5.2-11), and no tag contains that commit, which is also the committed gitlink. The latest CI job (run 45576025453, `5e42ac65`) fails with `ProjectsQueryResponsePolicy.cs(8,53): error CS0246`. The Builds consumer validator rejects "overrides authoritative version property", so the exit path really does run through a Builds selection. | Confirmed |
| FrontComposer `6.1.0` | Committed catalog; nuget (latest 6.1.0) | Current (see T-L1) |
| McpCli.Abstractions `3.0.0` | Latest on nuget; the 3.0.0 DLL contains `HexalithQueryAttribute` and `HexalithCommandAttribute` (AD-29 decoration) | Current |
| Aspire SDK/CLI `17.0.0`; Toolkit Dapr `17.0.0-preview.1.261120-0454` | nuget: `Aspire.AppHost.Sdk`, `Aspire.Hosting` and `Aspire.Cli` are at 17.0.0 (after 13.6.5). The Toolkit preview is the newest build, with no stable 17.x. The G-6 policy records `approved-prerelease-exception`. | Current; deprecation confirmed (T-L2) |
| Dapr CLI and .NET packages `1.19.1`; catalog runtime `1.19.2`; G-6 and CI on 1.19.1 under exception; `Dapr.Workflow` unselected | GitHub: CLI v1.19.1 and runtime v1.19.2 are the latest. nuget: `Dapr.Client`, `Dapr.AspNetCore` and `Dapr.Workflow` are at 1.19.1. The catalog sets `HexalithDaprRuntimeVersion` 1.19.2 and `HexalithDaprCliVersion` 1.19.1. The policy records `daprSupportTable: approved-explicit-exception` and `Dapr.Workflow: catalog-only-unselected`. | Current |
| The memlog says 1.19.2 fixes ETag and first-write handling on Redis, PostgreSQL and in-memory stores, plus three CVEs | v1.19.2 release notes list Redis, PostgreSQL and in-memory among the affected stores. The CVEs are CVE-2026-116587 and CVE-2026-116586 (go-jose) and CVE-2026-111497 (brotli). | Confirmed |
| Fluent UI `5.1.0`, Fluxor `6.11.0`, ByteAether.Ulid `1.4.2`, NSwag.MSBuild `14.8.0`, xunit.v3 `4.0.3`, Shouldly `4.3.0`, NSubstitute `6.2.0` | Each equals both catalogs and the latest stable version on nuget (Shouldly 5.0.0 is still in preview) | Current |
| G-6: Platform `f7635b59` pins Builds `763fc0e9` (v6.7.0); source-mode tuple; proof reuse disabled; PostgreSQL two-sidecar | `git ls-tree` of the Projects and Platform gitlinks; `git describe`; policy JSON (`reuseEnabled: false`, `sourceModeOnly: true`, approval reference); `prepare-g6-catalog/action.yml` | Confirmed |
| G-6 catalog-delta rule | A diff of the `763fc0e9` and `bf8a28b4` catalogs shows FrontComposer 6.0.1→6.1.0, Memories 4.1.0→4.1.1 and Microsoft.Extensions.Http.Resilience, ServiceDiscovery and TimeProvider.Testing 11.0.0→11.0.1, plus test and other packages. Projects consumes FrontComposer, Memories, Http.Resilience and ServiceDiscovery. | The rule is correct and needed |
| Runner contract: `hexalith-module prepare-build --manifest … --mode source --workspace … --output json`; `hexalith-evidence validate`; CI builds Module.Cli from the gitlink; `hexalith-evidence` not yet in the tool manifest | `ModuleCommandApplication.cs:137-142` at v6.8.1 (the options exist, and `--manifest`, `--workspace` and `--mode` are required). `EvidenceCommandApplication.cs:55` validates `hexalith.readiness-evidence.v1` YAML. `ci.yml:236-241` builds Module.Cli from the gitlink. `dotnet-tools.json` lists only the FrontComposer CLI. Because the gitlink is an exact release tag, "released CLI" and "built from the gitlink" are the same thing today. | Confirmed |
| AD-35: a second aggregate type can be hosted by the same DomainService, with atomic per-stream acquisition and the generation equal to the stream version | Multiple aggregates are discovered and registered keyed per domain. The actor ID is `{TenantId}:{Domain}:{AggregateId}`. `CommittedEventSequence` has been published since 5.5.1. The two-line host overload `AddEventStoreDomainService(params Assembly[])` compiles against published 5.5.x; CS0246 is the only CI error. | Holds, subject to T-H1 and T-M1 |
| The Stack table matches the committed catalog (`bf8a28b4`) | Every row was compared; `v6.8.2` changes none of these values | Confirmed |
