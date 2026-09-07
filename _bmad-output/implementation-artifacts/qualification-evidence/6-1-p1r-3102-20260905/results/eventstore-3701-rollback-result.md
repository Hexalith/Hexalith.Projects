# EventStore Rollback Coordinate Qualification — `v3.70.1`

Lane: **rollback-coordinate** (Phase 5, reciprocal rollback matrix, EventStore side only).
Repository: `Hexalith.EventStore`
Worktree: `/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-eventstore-3701` (isolated, detached, created from `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`)

## 1. Coordinate values

| Field | Value |
| --- | --- |
| `rollback_eventstore_revision` (requested) | `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` |
| `rollback_eventstore_source_revision` (actual, used for both source-mode and package-source-mode lanes) | `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` |
| `rollback_eventstore_source_describe` | `v3.70.1` (clean, no `-dirty`, no trailing `-N-g...`) |
| `rollback_eventstore_package_tag` | `v3.70.1` |
| `rollback_eventstore_package_source_revision` | `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` (tag `v3.70.1` resolves to the same commit as the source revision — this rollback coordinate is source/package-identical) |
| `source_package_equivalent` | `true` (tag and source commit are literally the same revision) |

### Worktree state — before first command / after last command

Captured immediately after `git worktree add --detach` (before any restore/build/test) and again after the final test run.

| Check | Before (first) | After (last) |
| --- | --- | --- |
| `git status --porcelain=v1` | *(empty — clean)* | *(empty — clean)* |
| `git rev-parse HEAD` | `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` | `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` |
| `git describe --tags --always --dirty` | `v3.70.1` | `v3.70.1` |

Note: root submodules declared in this repository's own `.gitmodules` (`Hexalith.AI.Tools`, `Hexalith.Builds`, `Hexalith.Commons`, `Hexalith.FrontComposer`, `Hexalith.Memories`, `Hexalith.PolymorphicSerializations`, `Hexalith.Tenants`) were initialized non-recursively (`git submodule update --init`, no nested submodules touched) after the worktree was created — required because `Directory.Packages.props` imports `references/Hexalith.Builds/Props/Directory.Packages.props`, and without it every project fails NuGet central-package-management restore (`NU1010`). This is a root-declared dependency of the workspace being qualified, not a nested submodule, so it is within the CLAUDE.md submodule policy. This did not modify any tracked file and `git status --porcelain=v1` remained empty throughout (submodules are gitlinks, not working-tree changes, when checked out at their pinned commit).

### SDK pin note

`global.json` at this revision pins `"version": "10.0.302"`, `"rollForward": "latestPatch"`. The host only had .NET SDK `10.0.400` installed (a different feature band; `latestPatch` roll-forward does not bridge `10.0.3xx` → `10.0.4xx`, and forcing it via `DOTNET_ROLL_FORWARD=LatestMajor` would weaken the exact pin, which the qualification contract forbids). To satisfy the pin exactly rather than weaken it, SDK `10.0.302` was installed via the official `dotnet-install.sh` script into an isolated, qualification-scoped directory (`scratchpad/sdk-install/dotnet-10.0.302`, outside `/home/administrator/.dotnet`), and `DOTNET_ROOT`/`PATH` were set to that directory for every restore/build/test command in both lanes. This is an environment-only addition (a repository-required pin being satisfied exactly, not overridden) and did not touch the worktree or the shared installation.

## 2. Command ledger

All commands run from `cwd = /tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-eventstore-3701` unless noted. `repository = Hexalith.EventStore`, `revision = f13f9925fdca53efa2ab8c90d396ab106f91bb9c` for all rows. Logs under `.../scratchpad/logs/eventstore-3701/<id>.log`.

| id | started_utc | ended_utc | command | environment_overrides | exit | result | log (sha256) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 00-worktree-add | 2026-09-05T13:01:09Z | 2026-09-05T13:01:10Z | `git -C /home/administrator/projects/hexalith/projects/references/Hexalith.EventStore worktree add --detach /tmp/.../scratchpad/wt-eventstore-3701 f13f9925fdca53efa2ab8c90d396ab106f91bb9c` | none | 0 | PASS | `73de20c8a69e05ea35f623220ebe6c7bd10f993feca8871a8564ac651cde1a07` |
| 00b-submodule-init | 2026-09-05T13:02:xx Z (immediately after worktree add) | same run | `git submodule update --init` | cwd = worktree root | 0 | PASS | `06f298bcb4cc5791219e348e80abfa30c0dbef928cf0ac11d133c509c5275e8b` |
| sdk-install | (before s1-restore) | (before s1-restore) | `bash scratchpad/sdk-install/dotnet-install.sh --version 10.0.302 --install-dir scratchpad/sdk-install/dotnet-10.0.302` | none (isolated install dir) | 0 | PASS | `5f01bbc82a609dfb17a922151acf9b75356ca161d759da33739c1bfce5100c8b` |
| s1-restore | 2026-09-05T13:03:51Z | 2026-09-05T13:04:36Z | `dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=true --force --no-cache --disable-parallel --verbosity minimal` | `DOTNET_ROOT=.../sdk-install/dotnet-10.0.302`; `PATH` prefixed with same; `NUGET_PACKAGES=.../wt-eventstore-3701-source/nuget`; `DOTNET_CLI_HOME=.../wt-eventstore-3701-source/dotnet-home`; `TMPDIR=.../wt-eventstore-3701-source/tmp` | 0 | PASS | `571a1c763741585f60d55fc3455030b2d0683382194ab6bfaa91a33239c50264` |
| s2-build | 2026-09-05T13:04:42Z | 2026-09-05T13:05:50Z | `dotnet build Hexalith.EventStore.slnx --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | same as s1-restore | 0 | PASS (0 warnings, 0 errors) | `bc4fe9d5aeba780d2272ace7b3b15d029d8b12a6a76f15f64204613f363eb561` |
| s3-test-contracts | 2026-09-05T13:05:57Z | 2026-09-05T13:06:09Z | `dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | same as s1-restore | 0 | PASS (708/708) | `f8eeeee17b27a9c514c5142f87dfdc6266ad4baf216bd1e5e48d61bd8e3572c0` |
| s4-test-client | 2026-09-05T13:06:09Z | 2026-09-05T13:06:17Z | `dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | same as s1-restore | 0 | PASS (673/673) | `eee622910ab21934e6c5df7a9cfdae2b8ddbfa437cc7188aeeeb147c78838808` |
| s5-test-domainservice | 2026-09-05T13:06:17Z | 2026-09-05T13:06:29Z | `dotnet test tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | same as s1-restore | 0 | PASS (143/143) | `84a7e160fef2a7f2a1f04043bb8bf07c1505aa6d4e5f3660fab13bd02d7998d5` |
| s6-test-server | 2026-09-05T13:06:29Z | 2026-09-05T13:10:50Z | `dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | same as s1-restore | 0 | PASS (2626/2651, 25 skipped, 0 failed) | `a51cfa26e1692b534433ddbaf1bccc6f9fdee8ed34e9f48d45af67f44c7268a1` |
| p1-restore | 2026-09-05T13:11:00Z | 2026-09-05T13:11:28Z | `dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=false --force --no-cache --disable-parallel --verbosity minimal` | `DOTNET_ROOT=.../sdk-install/dotnet-10.0.302`; `PATH` prefixed with same; `NUGET_PACKAGES=.../wt-eventstore-3701-package/nuget`; `DOTNET_CLI_HOME=.../wt-eventstore-3701-package/dotnet-home`; `TMPDIR=.../wt-eventstore-3701-package/tmp` | 0 | PASS | `5b7c301ae45664ad671115c2a84db7f10de2c0dea27870536da86a0fa5cb01ee` |
| p2-build | 2026-09-05T13:11:34Z | 2026-09-05T13:11:58Z | `dotnet build Hexalith.EventStore.slnx --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | same as p1-restore | 0 | PASS (0 warnings, 0 errors) | `e6486e187570ea0f8bc627da7f8e1ae62e0750dd4f46cc00820e1e099b44669c` |
| p3-test-contracts | 2026-09-05T13:12:06Z | 2026-09-05T13:12:11Z | `dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | same as p1-restore | 0 | PASS (708/708) | `174dcdd1f94e6c5ee91e3083115c96631c6a4d0922c2175987e682df4f98c9f1` |
| p4-test-client | 2026-09-05T13:12:11Z | 2026-09-05T13:12:16Z | `dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | same as p1-restore | 0 | PASS (673/673) | `10ea9aeeb5953cf1a4d0db8703853952f0642004f5d6dd2d0517442c97e903b7` |
| p5-test-domainservice | 2026-09-05T13:12:16Z | 2026-09-05T13:12:21Z | `dotnet test tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | same as p1-restore | 0 | PASS (143/143) | `63c48a1d506e99f56a03dc2ef171ba1e438e9855213ff8dfdeec8151920dfc51` |
| p6-test-server | 2026-09-05T13:12:21Z | 2026-09-05T13:16:35Z | `dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | same as p1-restore | 0 | PASS (2626/2651, 25 skipped, 0 failed) | `5c3453034059fb51256152012ddf533261a20075ef2dd0aead91557ed8c80b87` |

No command stalled; none required cancel/retry beyond the two deliberate, recorded environment corrections above (submodule init after the initial NU1010 restore failure at commands not otherwise numbered, and the isolated SDK install after the initial exit-155 SDK-not-found failure). Those two initial failing attempts are not separate ledger rows because they failed before any qualification command body executed (immediate SDK-resolution / project-graph errors, not a qualification result) — see Notes below for full transparency.

### Notes on the two pre-ledger failures (transparency, not scored)

1. First `s1-restore` attempt (no `DOTNET_ROOT` override): exit `155`, `.NET SDK 10.0.302 not found`. Not a scored row — corrected by installing the exact pinned SDK version in isolation (see `sdk-install` row) rather than weakening the pin.
2. Second `s1-restore` attempt (SDK present, submodules uninitialized): exit `1`, dozens of `NU1010` errors because `references/Hexalith.Builds/Props/Directory.Packages.props` did not exist. Not a scored row — corrected by initializing this repository's own root-declared submodules (`00b-submodule-init`), which is required workspace setup, not nested-submodule work.

## 3. 14-package manifest at `v3.70.1`

`git show v3.70.1:tools/release-packages.json` — SHA-256: `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`

| # | Package ID | Project path |
| --- | --- | --- |
| 1 | Hexalith.EventStore.Contracts | src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj |
| 2 | Hexalith.EventStore.Client | src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj |
| 3 | Hexalith.EventStore.Server | src/Hexalith.EventStore.Server/Hexalith.EventStore.Server.csproj |
| 4 | Hexalith.EventStore.SignalR | src/Hexalith.EventStore.SignalR/Hexalith.EventStore.SignalR.csproj |
| 5 | Hexalith.EventStore.Testing | src/Hexalith.EventStore.Testing/Hexalith.EventStore.Testing.csproj |
| 6 | Hexalith.EventStore.Testing.Integration | src/Hexalith.EventStore.Testing.Integration/Hexalith.EventStore.Testing.Integration.csproj |
| 7 | Hexalith.EventStore.Aspire | src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj |
| 8 | Hexalith.EventStore.ServiceDefaults | src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj |
| 9 | Hexalith.EventStore.DomainService | src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj |
| 10 | Hexalith.EventStore.RestApi.Generators | src/Hexalith.EventStore.RestApi.Generators/Hexalith.EventStore.RestApi.Generators.csproj |
| 11 | Hexalith.EventStore.Gateway | src/Hexalith.EventStore.Gateway/Hexalith.EventStore.Gateway.csproj |
| 12 | Hexalith.EventStore.Admin.Abstractions | src/Hexalith.EventStore.Admin.Abstractions/Hexalith.EventStore.Admin.Abstractions.csproj |
| 13 | Hexalith.EventStore.Admin.Cli | src/Hexalith.EventStore.Admin.Cli/Hexalith.EventStore.Admin.Cli.csproj |
| 14 | Hexalith.EventStore.Admin.Server | src/Hexalith.EventStore.Admin.Server/Hexalith.EventStore.Admin.Server.csproj |

Per the task scope, no separate 14-package remote-restore consumer proof was executed for this rollback revision (that proof is reserved for the selected `v3.102.0` tuple, run by the parallel selected-coordinate lane). This manifest is recorded for reference only, matching the required-coordinate-record fields for `rollback_eventstore_revision`.

## 4. Summary / disposition

**Every row in both the source-mode lane (restore, Debug build, and all four test projects) and the package-source lane (restore, Release build, and all four test projects) executed cleanly from the same isolated, detached worktree at `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` (tag `v3.70.1`) and produced `PASS`, with zero build warnings/errors and zero test failures across 1,524 unique test cases run twice (3,048 total test executions: 708+673+143+2651 per lane) — the worktree's `git status`, `HEAD`, and `describe` were identical and clean before the first command and after the last.** Two environment prerequisites had to be satisfied before any qualification command could run — installing the exact `global.json`-pinned .NET SDK `10.0.302` in an isolated directory (rather than weakening the pin to use the host's `10.0.400`), and initializing this repository's own root-declared submodules (not nested ones) so NuGet central-package-management could resolve `Directory.Packages.props` — both are recorded above as environment-only corrections, not weakenings of any pin, warning, test, or audit behavior. No command stalled, no `INCONCLUSIVE` or `FAIL` rows were produced, and no cancellation or exit-`143` occurred. This constitutes independently executable EventStore-side rollback evidence for `v3.70.1` per the Phase 5 exit gate ("Rollback is independently executable"), for the EventStore lanes only — the reciprocal Builds-side rollback ledger (catalog/audit/runner/schema/fixtures/evidence at `rollback_builds_revision`) and the candidate-vs-rollback `HXM016` negative-control cross-checks are out of this lane's scope and remain open per the contract's reciprocal rollback matrix.
