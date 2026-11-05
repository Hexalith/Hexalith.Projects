# Story 6.1 review patches — 2026-11-05

All fourteen G1 patch requests are implemented. Story status remains **in-progress**: current distributed acceptance and package-mode confirmation are blocked. No dependency pins, sibling sources, public routes, staging, commits or pushes changed.

## Verification

| Lane | Result |
| --- | --- |
| Mapped Debug Integration/Server builds | 0 warnings, 0 errors |
| Local Integration (distributed methods excluded) | 196 passed, 0 failed/errors/skipped/not-run |
| Local Server (owned-Dapr selectors excluded) | 1,328 passed, 0 failed/errors/skipped/not-run |
| Focused projection / read / catalog-worker | 51 / 7 / 12 passed, no failures/errors/skips |
| SDK evidence controls | 83 passed, no failures/errors/skips |
| Launcher controls | 119 passed in each coordinator/group |
| Actual MSBuild umbrella/sibling layout probes | 2 passed |

Native results are `local-integration.xml`, `local-server.xml`, `projection.xml`, `reads-corrected.xml`, `catalog.xml` and `integration-evidence-controls.xml`. `local-integration-command.json`, `local-server-commands.json`, `integration-current-commands.json`, `focused-current-commands.json` and `commands.json` retain exact commands. The Server exclusions are the entire `SupportedProjectAuthorityHttpTests` class and methods `GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites`, `ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart` and `GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures`. These excluded live cases supply no accepted AC7 coverage.

The existing topology control replays the removed later bootstrap assignments through Aspire's actual callbacks: all **47 Tenants and 58 gateway environment entries** remain equal. `Program.cs` composes the validated profile before the infrastructure catalog; all formerly optional inputs are mandatory. This rules out a changed environment dictionary or assignment order from the cleanup. Native control output is retained in `local-integration.xml`.

`summary.json` records final source/assembly evidence, exact totals, source limits and fresh cleanup verification. `revisions.json` records full Projects/direct dependency commits. Current local source manifest is **2e4b2076406fe46f47b27cea844d71353e62a6eccadd81c01171267145a6e5d7** (17028 inputs). The latest distributed attempt used **32ac7ec2d471f0c68a0d871aa42541c35ae3e1121917b235e75d064d9592796e**; its only subsequent source change is the topology equivalence control.

## Current blockers

The fresh prescribed launcher with `--regressions --timeout 900` reached a healthy isolated topology and a passing Integration build, then its native duplicate method failed **0 passed / 1 failed / 0 skipped/errors** before any Project reads: `Real Tenants hosted bootstrap has no original submission receipt`. EventStore recorded `RouteCatalogUnavailableException`. No later provider group or full runtime regression ran. The immutable packet is `distributed-current/duplicate/` and the coordinator result is `distributed-current/run.json`. Owned AppHost, six sidecars, all four containers and private material were removed; unrelated resources were preserved. Fresh verification reconfirmed container/AppHost absence and external preservation. No blind rerun or sibling repair was performed.

The exact package command was:

```bash
CI=true MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Release -m:1 -p:NuGetAudit=false
```

It exited 1 with **3 NU1109 restore errors**, before compilation: Tenants.Client 7.0.0 requires EventStore.Client/Contracts >=4.0.4; Projects centrally selects 3.133.0. `build-package.log` preserves the exact output. No version override or weakened gate was used. Package evaluation confirms the fixed source-only worker test is excluded (`package-symbols.json`). Ordinary unmapped Debug builds require the Platform-selected Conversations mapping; mapped builds pass.

## Preserved failed attempts

The original `distributed/` attempt stopped at two outdated SDK constructor calls in Projects evidence tests. The minimal test-only reader-interface adaptation passes all 83 evidence controls. Its original placement cleanup failure and report remain unchanged; `distributed-recovery.json` separately verifies exact already-absent ownership, private-material removal and external preservation. Earlier missing-import and incorrect denial-audit test assertions remain in `build-mapped.log` and `reads.xml`; both were corrected before passing evidence. The native failing original files were not overwritten.
