# Reviewer Gate (Validate intent) — Technology-currency lens

**Target:** `ARCHITECTURE-SPINE.md` (Hexalith.Projects, status final, updated 2026-11-24, SHA-256 `01b89a2f5e076a52b17f253417b7472ae85c0c7d26ae5a3c21c428e5868eb5fe`, 600 lines). Repository HEAD and `origin/main`: `31b35637`. Its gitlinks match `280b1eed`. Reviewed 2026-11-25 against repository pins, nuget.org (flat container and registration APIs), GitHub releases and tags, the .NET release metadata, the Dapr support table, and read-only `gh run view`. The local NuGet cache was not used.

**Verdict: PASS WITH FINDINGS.** The named versions match the repository pins and are current on nuget.org. The EventStore seam claim checks out. Two high findings need to be fixed before the spine is used for DW-63 acceptance or the AD-35/AD-17 writer gate: the "source mode only" consequence is understated, and Dapr runtime 1.19.1 predates a concurrency fix.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 2 |
| Medium | 3 |
| Low | 2 |

---

## High

### T-1 — "Source mode only" hides a CI build that fails by construction

- **Severity:** High
- **Anchor:** Implementation boundaries, Writers row (line 51); Stack, EventStore row (line 461); Consistency Conventions, Build and test (line 452); AD-25 package-mode parity (line 327)
- **Evidence:**
  - The claim itself holds. `git log -S IDomainQueryResponsePolicy` in references/Hexalith.EventStore returns only `208af26a5 2026-11-24 21:16:46 +0100 feat: constrain development grants and query HTTP mapping`. `git grep IDomainQueryResponsePolicy v5.5.2 -- src` finds nothing. `git ls-remote` shows remote `main` = `208af26a` and no tag after `v5.5.2`.
  - EventStore **5.5.2 is published**. Its registration shows `published 2026-11-24T16:04:40Z`, and it is the newest version on nuget.org. It does **not** carry the seam. The spine names only 5.5.1 (line 461).
  - `Directory.Build.props` sets `UseHexalithProjectReferences=false` when `CI=true`. Its `RejectUnsafeHexalithProjectReferenceMode` target fails CI builds that use sibling project references. So CI always builds in package mode against Builds-selected EventStore 5.5.1.
  - CI build-and-test fails at step 15 "Build" with the same error on every DW-63-era push: run 45477603548 (`0db83209`), run 45479334086 (`280b1eed`) and run 45488087994 (`31b35637`, HEAD). The error is `src/Hexalith.Projects.Server/Queries/ProjectsQueryResponsePolicy.cs(8,53): error CS0246: The type or namespace name 'IDomainQueryResponsePolicy' could not be found`. In each of these runs `ci / aspire-tests`, `ci / performance-tests`, `Projects generated-artifact gates` and `Scheduled managed AppHost E2E` are **skipped**.
- **Failure scenario:** An implementer of DW-63 or Story 7.1 reads "builds in source mode only" as a local inconvenience. In practice every push to `main` fails compilation in CI, and no CI test, generated-artifact or managed-journey evidence can exist for any revision. This conflicts with the line 452 convention ("Release plus package references in CI") and with AD-25/AD-30 evidence. Two plausible wrong fixes: bump the Builds catalog to the newest published EventStore (5.5.2), which still lacks the type, or enable project references in CI, which the build and Hexalith.CiCd forbid.
- **Proposed resolution:** In the Writers row and the Stack EventStore row, say: "Published EventStore up to 5.5.2 lacks `IDomainQueryResponsePolicy`, which first appears in untagged `208af26a`. While the Server uses it, CI package-mode build-and-test fails (CS0246) on every revision and all downstream CI test jobs are skipped. CI therefore supplies no DW-63 acceptance evidence. The exit requires all three steps: (1) EventStore publishes a release later than 5.5.2 that carries the type, (2) Builds selects it, and (3) Projects advances its Builds gitlink. Bumping to 5.5.2 is insufficient. Project references in CI remain forbidden." Name the EventStore release owner as the dependency.
- **Acceptance check:** The spine names 5.5.2 as not carrying the seam and states that CI build-and-test is red until the three-step exit. After the exit, a CI run on the new Builds gitlink passes the Build step with no CS0246.
- **Status:** CONFIRMED (EventStore git history and tags, nuget registration, CI job logs).

### T-2 — G-6 and CI use Dapr runtime 1.19.1, which predates the 1.19.2 ETag/first-write fix

- **Severity:** High
- **Anchor:** Stack, Dapr row (line 464); AD-15 (line 233); AD-17 single-writer fencing (line 245); AD-35 claim race/stale-release proof (lines 428–432); G-6 (line 558)
- **Evidence:**
  - The spine says: "The November 23 source-mode G-6 policy and CI select runtime `1.19.1` under an approved support-table exception. Record the runtime actually tested." (line 464).
  - Repository pins: `tools/qualification/g6-current-policy-2026-11-23-toolkit-preview.json` has `"daprRuntime": "1.19.1"`. `.github/workflows/ci.yml:123-124` and `:377-378` set `dapr-runtime-version` and `runtime-version` to `'1.19.1'`. The Builds catalog sets `HexalithDaprRuntimeVersion` to `1.19.2`.
  - The dapr/dapr v1.19.2 release (published 2026-11-19) lists "State stores did not enforce ETags and first-write consistently". It names Redis, PostgreSQL (v1 and v2) and in-memory among the affected stores. Its notes say: "On Redis, a transaction with a failed ETag check still applied its other operations… a concurrent update could be lost without an error". After the upgrade, requests that were accepted by mistake "fail with an ETag mismatch: HTTP status `409`, or gRPC code `Aborted`". The same release fixes CVE-2026-116586, CVE-2026-116587 (go-jose) and CVE-2026-111497 (brotli).
  - EventStore depends on ETag concurrency. For example, `src/Hexalith.EventStore.Admin.Server/Services/DaprConsistencyCommandService.cs:196` calls `TrySaveStateAsync(…, etag, …)`, and the EventStore source has more than 1,200 ETag/first-write references. The G-6 qualifier starts `redis:7.4` and a real PostgreSQL two-sidecar restart (`tools/qualification/run_g6_current.py:223,242`).
- **Failure scenario:** AD-35 race, lost-reply and stale-release evidence, and AD-17/AD-15 fencing and serialization evidence, are produced on 1.19.1. On that runtime a failed ETag check can still apply other operations in a Redis transaction. Evidence can pass or fail for reasons that change on 1.19.2. A production platform on the catalog's 1.19.2 then returns `409/Aborted` where qualification saw success. Alternatively, a deployment that matches the qualified 1.19.1 can silently lose a concurrent update, which breaks the "at most one Active holder" guarantee. The support-table exception covers SDK and runtime pairing, not this defect.
- **Proposed resolution:** Change the Dapr row to: "Runtime 1.19.2 is the current patch and fixes ETag/first-write enforcement on Redis, PostgreSQL and in-memory, plus three CVEs. G-6 and CI currently run 1.19.1. Evidence for AD-15 serialization, AD-17 fencing and the AD-35 claim must run on runtime ≥ 1.19.2, unless the owner records an explicit risk acceptance on each affected evidence row. 1.19.1 results do not qualify those invariants." Route the G-6 policy and ci.yml runtime bump to the policy owner.
- **Acceptance check:** The spine names 1.19.2's ETag fix and binds AD-15/AD-17/AD-35 evidence to runtime ≥ 1.19.2. The next G-6 policy tuple, or the recorded acceptance, reflects that.
- **Status:** CONFIRMED (GitHub release notes, repository pins, EventStore source).

---

## Medium

### T-3 — CI and G-6 outcomes in the spine were already stale at finalization

- **Severity:** Medium
- **Anchor:** Implementation boundaries, Release row (line 52); Stack G-6 paragraph (line 469); Stack intro revision anchor (line 456)
- **Evidence:**
  - The spine says: "The last completed G-6 qualification passed on `26fed89d`, an ancestor of `280b1eed`" (line 469). Line 52 lists the `5f1d955b` and `0db83209` failures but says nothing about `280b1eed`'s own CI.
  - Run 45479334086 (`280b1eed`) completed at 2026-11-24T20:58:39Z. Its G-6 job 138759280105 logged `G6-CURRENT-QUALIFIED: approved tuple, material inputs, critical proof, cleanup and source verified` and P1R replay passed, but build-and-test failed (T-1). The memlog's final update (`updated: 2026-11-24T22:03`, i.e. 21:03Z) came later, so this result was available when the spine was finalized.
  - Run 45488087994 (`31b35637` = HEAD = `origin/main`, same gitlinks as `280b1eed`): G-6 job 138780744341 and P1R passed, build-and-test failed with CS0246.
  - "Last fully green CI run is for `26fed89d`" is still true.
- **Failure scenario:** An implementer or the AD-30 matrix cites G-6 on an ancestor (`26fed89d`) and misses the G-6 pass on the exact revision. Or they assume `280b1eed`'s own CI is unknown, when it ran and failed at compile. A dated run ledger in the spine goes stale on every push.
- **Proposed resolution:** Replace line 469's sentence with: "G-6 qualified on `280b1eed` (job 138759280105) and `31b35637` (job 138780744341). Build-and-test failed on both (T-1). The last fully green CI run remains `26fed89d`." Better still, keep only the rule ("cite G-6 on the exact candidate SHA; a red build-and-test blocks qualification") and move the per-run ledger to the memlog or tracker. A run ledger is rationale, not an invariant.
- **Acceptance check:** No spine sentence names an older G-6 pass when a later one on the anchored revision exists. Or the spine contains no run IDs beyond the evidence links, and the tracker holds them.
- **Status:** CONFIRMED (`gh run view` jobs and logs).

### T-4 — The Builds CLI pin is stale and does not match what CI and G-6 run

- **Severity:** Medium
- **Anchor:** Repository-owned runner contract (line 511); AD-30 `hexalith-evidence validate` (line 361)
- **Evidence:**
  - The spine says: "Use the released Builds CLI for source preparation. Projects selects Module.Cli `6.4.2`." (line 511). `Directory.Packages.props:6` sets `HexalithBuildsModuleCliVersion` to `6.4.2`. That value is consumed as `PackageDownload` plus a `Hexalith.Builds.Tooling.dll` HintPath in `tests/Hexalith.Projects.Integration.Tests/…csproj:34-36` and `tests/Hexalith.Projects.PackageFixtures.AppHost/…csproj:33-35`.
  - nuget.org has `hexalith.builds.module.cli` versions 6.4.3 through **6.8.2**. 6.4.2 is 11 releases behind the latest.
  - The Builds gitlink is `bf8a28b4` = `v6.7.0-7`. CI and G-6 do not use 6.4.2: they **build Module.Cli from that source** (`.github/workflows/ci.yml:236-241`, `tools/qualification/run_g6_current.py:35,64`).
  - AD-30 makes Builds own `hexalith-evidence validate`. The command exists (`Hexalith.Builds.Evidence.Cli/EvidenceCommandApplication.cs:55`, which validates `hexalith.readiness-evidence.v1`), and the package `Hexalith.Builds.Evidence.Cli` is published up to 6.8.2. Projects pins no version of it: `.config/dotnet-tools.json` lists only `hexalith.frontcomposer.cli`. The memlog line 106 confirms "governed evidence validation still unavailable from the Projects tool manifest".
- **Failure scenario:** A developer runs `prepare-build` with the "selected" 6.4.2 while CI maps with v6.7.0+7 source, and gets different mapping or schema behavior. Package-mode fixtures compile against Tooling 6.4.2 while the source lanes use 6.7.x. AD-30 rows are validated, or not, with whatever evidence CLI version someone installs.
- **Proposed resolution:** State a single Builds tooling version rule: "Module.Cli and Evidence.Cli versions match the pinned Builds release (v6.7.0 at `bf8a28b4`) or the published version that Builds selects. CI builds Module.Cli from the Builds gitlink. `hexalith-evidence` is pinned in `.config/dotnet-tools.json` before any AD-30 row can be recorded as validated." Either correct the 6.4.2 selection or record why it is intentionally older.
- **Acceptance check:** The spine names one Builds CLI version family consistent with the Builds gitlink. `.config/dotnet-tools.json` and `HexalithBuildsModuleCliVersion` agree with it, and `hexalith-evidence` has a pinned version.
- **Status:** CONFIRMED (repository files, nuget flat container, Builds source).

### T-5 — The G-6 catalog gap is wider than FrontComposer

- **Severity:** Medium
- **Anchor:** Stack G-6 paragraph (line 469); AD-24 shipping packages (line 321)
- **Evidence:**
  - The spine says: "Platform `f7635b59` pins Builds `763fc0e9` (v6.7.0), which selects FrontComposer `6.0.1`, so G-6 evidence does not cover FrontComposer `6.1.0`." The pin chain is confirmed: `git ls-tree f7635b59` shows Builds `763fc0e9`, and `git describe` shows `v6.7.0`.
  - `diff` of `Props/Directory.Packages.props` between `763fc0e9` and `bf8a28b4` also shows these changes:
    - `HexalithMemoriesVersion` 4.1.0 → 4.1.1. The Server uses `Hexalith.Memories.Client.Rest` and `.Contracts` packages in package mode (`Hexalith.Projects.Server.csproj:44-45`).
    - `Microsoft.Extensions.Http.Resilience` and `Microsoft.Extensions.ServiceDiscovery` 11.0.0 → 11.0.1. `tools/release-packages.json` declares both at `11.0.1` for the shipping `Hexalith.Projects.ServiceDefaults` package.
    - `Microsoft.Extensions.TimeProvider.Testing`, `Verify`/`Verify.XunitV3` 33.6.0 → 33.7.2, `Microsoft.Playwright` 1.65.0 → 1.65.1, among others.
- **Failure scenario:** Release qualification treats G-6 as covering everything except FrontComposer 6.1.0. The shipping ServiceDefaults package then goes out with `Microsoft.Extensions.*` 11.0.1 dependencies that no G-6 run built against.
- **Proposed resolution:** Change the sentence to: "G-6 evidence covers the Builds `763fc0e9` catalog only. Any version that differs in the committed `bf8a28b4` catalog is unqualified by G-6. Today that includes FrontComposer 6.1.0, Memories 4.1.1 and Microsoft.Extensions.Http.Resilience/ServiceDiscovery 11.0.1."
- **Acceptance check:** The spine states the coverage rule as "catalog delta", not a single package. A full-release G-6 rerun uses a catalog equal to the committed one, or lists the deltas it accepts.
- **Status:** CONFIRMED (git diff of both catalogs, release-packages.json).

---

## Low

### T-6 — The "Aspire SDK" is deprecated in the selected Aspire 17.0.0

- **Severity:** Low
- **Anchor:** Stack, Aspire row (line 463); AD-25 runner and AppHost retirement (line 327)
- **Evidence:** The microsoft/aspire v17.0.0 release notes (2026-11-18) say: "`AddProject`, `AddCSharpApp`, and `Aspire.AppHost.Sdk` remain available but are deprecated in favor of path-based .NET project registration and standard `Microsoft.NET.Sdk` AppHosts. C# AppHosts also require the Aspire CLI bundle." In the repository, `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` uses `Sdk="Aspire.AppHost.Sdk/17.0.0"` with `NoWarn ASPIRE012`. `Directory.Build.props` keeps `ASPIRE012` and `CS0618` out of warnings-as-errors. The Builds catalog comment (lines 8–10) says: "Aspire 17 deprecates Aspire.AppHost.Sdk (ASPIRE012) and AddProject (CS0618)". The spine records none of this.
- **Failure scenario:** New AppHost or fixture work, such as AD-25 runner consumers or package-fixture AppHosts, copies the deprecated SDK pattern. The warnings stay suppressed, and a later Aspire major release that removes the SDK breaks those lanes without warning.
- **Proposed resolution:** Add to the Aspire row: "`Aspire.AppHost.Sdk` and `AddProject` are deprecated in 17.0.0 (ASPIRE012/CS0618). Only existing Projects AppHosts keep them, pending AD-25 retirement. New AppHost code uses `Microsoft.NET.Sdk` with path-based `AddDotnetProject` and the Aspire CLI bundle."
- **Acceptance check:** The Stack names the deprecation and the rule for new AppHost code.
- **Status:** CONFIRMED (release notes, repository files).

### T-7 — The FrontComposer CLI tool lags the FrontComposer packages

- **Severity:** Low
- **Anchor:** Repository-owned runner contract (line 511); Stack, FrontComposer (line 461)
- **Evidence:** `.config/dotnet-tools.json` pins `hexalith.frontcomposer.cli` at `6.0.1`. The catalog selects FrontComposer packages at `6.1.0`. `hexalith.frontcomposer.cli 6.1.0` was published 2026-11-23T02:56Z. The spine says the manifest "pins FrontComposer CLI `6.0.1` only" but does not explain the gap.
- **Failure scenario:** `frontcomposer inspect` or `migrate` 6.0.1 checks descriptors built against FrontComposer 6.1.0 SourceTools, and its output is taken as evidence for 6.1.0 descriptors. That the output could actually be wrong is plausible, not shown.
- **Proposed resolution:** Either move the tool to 6.1.0, or state that 6.0.1 is kept on purpose and that its output is not evidence for 6.1.0 descriptors.
- **Acceptance check:** The tool version matches `HexalithFrontComposerVersion`, or the spine states the exception.
- **Status:** CONFIRMED for the version gap. PLAUSIBLE for the behavioral impact.

---

## Checked and held up

- .NET: `global.json` sets `11.0.100`, `latestPatch`, `allowPrerelease: false`. The .NET 11.0 release metadata shows latest SDK `11.0.100` (GA 2026-11-10, STS, EOL 2028-11-14). CI setup-dotnet resolved `11.0.100`. The spine correctly flags the risk that `latestPatch` moves CI off the exact G-6 SDK.
- `net11.0` and `LangVersion=latest` are set in `Directory.Build.props`, as the spine says.
- `IDomainQueryResponsePolicy` first appears in untagged EventStore `208af26a` (`v5.5.2-11`, current remote `main`) and is absent at `v5.5.2`, as the spine claims.
- Builds gitlink `bf8a28b4` (`v6.7.0-7`) is the same at `280b1eed` and HEAD. The committed catalog matches every Stack value: EventStore 5.5.1, FrontComposer 6.1.0, McpCli 3.0.0, Aspire 17.0.0, Toolkit Dapr `17.0.0-preview.1.261120-0454`, Dapr CLI 1.19.1 with runtime 1.19.2, Dapr .NET 1.19.1, Fluent UI 5.1.0, Fluxor 6.11.0, ByteAether.Ulid 1.4.2, NSwag.MSBuild 14.8.0, xunit.v3 4.0.3, Shouldly 4.3.0, NSubstitute 6.2.0.
- Each of those third-party versions is the latest stable on nuget.org. The only newer versions are previews: Shouldly 5.0.0-preview.2, and nothing stable for the Toolkit Dapr package.
- CommunityToolkit.Aspire v17.0.0 (2026-11-20) shipped stable 17.0.0 for other integrations but only `17.0.0-preview.1.261120-0454` for Dapr hosting, so the approved prerelease exception is a real need.
- Aspire 17.0.0 (2026-11-18) is the current release on GitHub and nuget.org, including `aspire.cli` 17.0.0.
- In the Dapr support table (v1.19 docs), the runtime 1.19.1 row lists .NET SDK 1.19.0, not 1.19.1, so "approved support-table exception" is accurate. Dapr CLI 1.19.1 and .NET SDK 1.19.1 are the current releases.
- The G-6 policy tuple matches the spine: SDK 11.0.100, Aspire 17.0.0, Toolkit preview, Dapr 1.19.1 ×3, Fluent UI 5.1.0, NSubstitute 6.2.0, Fluxor 6.11.0, `sourceModeOnly: true`, `Dapr.Workflow: catalog-only-unselected`. No `src/` or `tests/` project references Dapr.Workflow.
- Platform `f7635b59` pins Builds `763fc0e9` (`v6.7.0`), which selects FrontComposer 6.0.1, as stated.
- McpCli: the published tool versions are exactly 2.0.1 and 3.0.0. 3.0.0 was published 2026-11-15T02:41Z. The Projects enrollment text first appears in `61db47b7` (2026-11-15 22:54 +0100), after `v3.0.0`, so "3.0.0 predates enrollment" holds. The README at `6993abf` (an ancestor of gitlink `c07d09cc`) states enrollment against public Projects.Contracts 2.0.0 (published 2026-11-19) and the four reads. McpCli.Abstractions 3.0.0 is the latest.
- CI history checks out: run 45417404832 (`26fed89d`) was all green, including P1R and G-6 job 138530164186. Run 45430692159 (`5f1d955b`) failed at "Initialize root-declared submodules" before the Build step ran. Run 45477603548 (`0db83209`) failed build-and-test.
- AD-16 dependency-light Contracts holds: `Hexalith.EventStore.Contracts 5.5.1` depends only on Commons.UniqueIds and ByteAether.Ulid, and `Hexalith.McpCli.Abstractions 3.0.0` has no dependencies.
- The Structural Seed type names resolve. `ProjectAggregate` exists in `src/Hexalith.Projects`. EventStore `208af26a` has `AddEventStoreDomainService(this WebApplicationBuilder, params Assembly[])`. Today the Server passes `typeof(ProjectsDomainProcessor).Assembly` (the Server assembly) and registers `ProjectsQueryResponsePolicy`, as the seed describes.
- Inline package versions appear only in the historical P1R consumer fixture (`tests/tools/fixtures/6-1-p1r-3110`, EventStore 3.110.0), which matches the "historical qualification" scope.
- Outside the spine (no action for this gate): the `Directory.Build.props` comment that justifies `NU5104` refers to a "prerelease Fluent UI dependency". Fluent UI 5.1.0 has been stable since 2026-11-16.
