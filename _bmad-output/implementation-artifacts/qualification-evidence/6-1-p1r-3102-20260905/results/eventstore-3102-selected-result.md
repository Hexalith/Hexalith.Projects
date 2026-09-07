# Hexalith.EventStore — Selected Coordinate Qualification Result (v3.102.0)

Lane: 6.1-P1R revalidation, EventStore Owner acceptance lane.
Repository under test: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore` (checkout untouched; all work in an isolated worktree).

## 1. Coordinate values

| Field | Value |
| --- | --- |
| `eventstore_source_revision` | `4ae9cee1e9abe050402fd1405a9abd54892ba13f` |
| `eventstore_source_describe` | `v3.102.0` |
| `eventstore_package_version` | `3.102.0` |
| `eventstore_package_tag` | `v3.102.0` |
| `eventstore_package_source_revision` | `4ae9cee1e9abe050402fd1405a9abd54892ba13f` (same commit as source revision) |
| `source_package_equivalent` | `true` — both revisions are identical (single coordinate used for both source-mode and package-mode lanes; diff is trivially empty by construction, not separately proven) |
| `eventstore_release_manifest_sha256` | `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae` |

Worktree: `/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/wt-eventstore-3102`, created via `git worktree add --detach` at the pinned commit.

### Worktree clean-state proof

| Check | Before | After |
| --- | --- | --- |
| `git status --porcelain=v1` | (empty — clean) | (empty — clean) |
| `git rev-parse HEAD` | `4ae9cee1e9abe050402fd1405a9abd54892ba13f` | `4ae9cee1e9abe050402fd1405a9abd54892ba13f` |
| `git describe --tags --always --dirty` | `v3.102.0` | `v3.102.0` |

Logs: `logs/eventstore-3102/01-worktree-pre-check.log` (sha256 `69efa2d741076f85e533c25d4116d2ddd6505b46318baf98695c9cd7c3435c1d`), `logs/eventstore-3102/99-worktree-post-check.log` (sha256 `4e332b4be574c55bf861a42bb5a424251f9477a918150673b5a071bb46820a26`).

## 2. Blocking root cause (applies to both build lanes)

`Directory.Packages.props` at the repository root imports central package version pins from
`references/Hexalith.Builds/Props/Directory.Packages.props`:

```xml
<Hexalith4BuildPackageProps>$(MSBuildThisFileDirectory)../../references/Hexalith.Builds/Props/Directory.Packages.props</Hexalith4BuildPackageProps>
...
<Import Project="$(Hexalith4BuildPackageProps)" Condition="'$(HexalithVersionsLoaded)' != 'true'" />
```

`Hexalith.Builds` is declared in this repository's own `.gitmodules` as a submodule
(`references/Hexalith.Builds`, pinned commit `e0e069468b29ce3fe85082b7bf0eb0d1952ce77c` at this
coordinate) but is **not initialized** in the isolated worktree. My task boundaries explicitly
state "Never init nested submodules," and CLAUDE.md's umbrella-workspace rule restricts
submodule initialization to dependencies declared by the *top-level* workspace `.gitmodules`
(the outer `hexalith/projects` superproject) — `Hexalith.Builds` is declared by EventStore's own
(nested, from the superproject's perspective) `.gitmodules`, so it was not initialized.

Effect: every `PackageVersion` needed by Central Package Management is unresolved, so
`dotnet restore` fails with `NU1010` for every project, and `dotnet build`/`dotnet test --no-restore`
subsequently fail with `MSB4019` (missing import) because no restore output exists. This is **not**
a stall, sandbox, or environment-cache issue — it reproduces deterministically and immediately
(each command completes in under a few seconds) in both `UseHexalithProjectReferences=true` and
`UseHexalithProjectReferences=false` modes, since both modes load the same
`Directory.Packages.props`.

**This is a blocking issue for the orchestrator/EventStore Owner to resolve**: either explicitly
authorize initializing the `Hexalith.Builds` submodule (non-recursively, pinned to the commit this
EventStore coordinate declares) as an in-scope step for source/package-mode qualification, or
accept that the source-mode and package-source lanes cannot execute from a fully isolated
single-repository worktree under the current boundary, and record those rows `FAIL`.

## 3. Command ledger

All commands were expanded exactly as shown; all logs are under
`logs/eventstore-3102/` in this scratchpad; `environment_overrides` are listed once per lane
(identical for every command in that lane).

### Worktree setup

| id | repository | revision | cwd | started_utc | ended_utc | command | exit | result | log | log_sha256 | notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 00-worktree-add | Hexalith.EventStore | 4ae9cee1e9abe050402fd1405a9abd54892ba13f | /home/administrator/projects/hexalith/projects/references/Hexalith.EventStore | 2026-09-05T13:00:56.324199080Z | 2026-09-05T13:00:57.403995274Z | `git -C /home/administrator/projects/hexalith/projects/references/Hexalith.EventStore worktree add --detach /tmp/.../scratchpad/wt-eventstore-3102 4ae9cee1e9abe050402fd1405a9abd54892ba13f` | 0 | PASS | logs/eventstore-3102/00-worktree-add.log | b053082b394a447f40137d06edafe3a0ba4fde270265da09294e541900320be6 | Isolated detached worktree created cleanly |
| 01-worktree-pre-check | Hexalith.EventStore | 4ae9cee1e9abe050402fd1405a9abd54892ba13f | wt-eventstore-3102 | 2026-09-05T13:00:57Z (immediately following 00) | same | `git status --porcelain=v1`; `git rev-parse HEAD`; `git describe --tags --always --dirty` | 0 | PASS | logs/eventstore-3102/01-worktree-pre-check.log | 69efa2d741076f85e533c25d4116d2ddd6505b46318baf98695c9cd7c3435c1d | Clean, HEAD matches, describe = v3.102.0 |
| 99-worktree-post-check | Hexalith.EventStore | 4ae9cee1e9abe050402fd1405a9abd54892ba13f | wt-eventstore-3102 | 2026-09-05T13:05Z (approx, after all lanes) | same | `git status --porcelain=v1`; `git rev-parse HEAD`; `git describe --tags --always --dirty` | 0 | PASS | logs/eventstore-3102/99-worktree-post-check.log | 4e332b4be574c55bf861a42bb5a424251f9477a918150673b5a071bb46820a26 | Still clean, HEAD unchanged, describe unchanged — no drift caused by execution |

### Source-mode lane (`UseHexalithProjectReferences=true`, Debug)

Environment overrides for every row in this lane:
`NUGET_PACKAGES=/tmp/.../scratchpad/wt-eventstore-3102-source/nuget`,
`DOTNET_CLI_HOME=/tmp/.../scratchpad/wt-eventstore-3102-source/dotnet-home`,
`TMPDIR=/tmp/.../scratchpad/wt-eventstore-3102-source/tmp`.
cwd for every row: `wt-eventstore-3102`.

| id | started_utc | ended_utc | command | exit | result | log | log_sha256 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| src-01-restore | 2026-09-05T13:01:11.587009193Z | 2026-09-05T13:01:14.556862096Z | `dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=true --force --no-cache --disable-parallel --verbosity minimal` | 1 | FAIL | logs/eventstore-3102/src-01-restore.log | 8bac9109a2583d3b4cfef57c9637815e8e3f168b6114e21672dc3fb97979459a |
| src-02-build | 2026-09-05T13:02:14.666297695Z | 2026-09-05T13:02:15.651338675Z | `dotnet build Hexalith.EventStore.slnx --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | 1 | FAIL | logs/eventstore-3102/src-02-build.log | 3ead2411de5ed056be61452946ecc5a873b2b3126c0bae018d2a0a84c8244bbe |
| src-03-test-contracts | 2026-09-05T13:02:15.655067849Z | 2026-09-05T13:02:16.186892608Z | `dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | 1 | FAIL | logs/eventstore-3102/src-03-test-contracts.log | 52cda9e6da69191ff2e3a814370e07197faa92bdff8b1cd37e8fcddfa1f0932a |
| src-04-test-client | 2026-09-05T13:02:16.190288441Z | 2026-09-05T13:02:16.632748375Z | `dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | 1 | FAIL | logs/eventstore-3102/src-04-test-client.log | 729105adc113cae9175e009a7e046455e97474c07d2f4a593eee2d883ea7b283 |
| src-05-test-domainservice | 2026-09-05T13:02:16.636618625Z | 2026-09-05T13:02:17.127782559Z | `dotnet test tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | 1 | FAIL | logs/eventstore-3102/src-05-test-domainservice.log | c381949d6fdffdcff5321b46eccfe821c14778dc377a5d12b109d4289879ac47 |
| src-06-test-server | 2026-09-05T13:02:17.132110615Z | 2026-09-05T13:02:17.629336299Z | `dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1` | 1 | FAIL | logs/eventstore-3102/src-06-test-server.log | 146ec6374812677f49191e5f9e726398ea498d1440030f52ce627cfc7006aec0 |

Notes for all six rows: root cause is the missing `Hexalith.Builds` submodule import described
in section 2 above (`NU1010` on restore; `MSB4019` on build/test since restore produced no
output). No artifacts produced. All commands ran to completion in well under a second to a few
seconds each — no stall, no cancellation, no retry needed.

### Package-source and remote-package lane (`UseHexalithProjectReferences=false`, Release)

Environment overrides for every restore/build/test row in this sub-lane:
`NUGET_PACKAGES=/tmp/.../scratchpad/wt-eventstore-3102-package/nuget`,
`DOTNET_CLI_HOME=/tmp/.../scratchpad/wt-eventstore-3102-package/dotnet-home`,
`TMPDIR=/tmp/.../scratchpad/wt-eventstore-3102-package/tmp`.
cwd for every row: `wt-eventstore-3102`.

| id | started_utc | ended_utc | command | exit | result | log | log_sha256 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| pkg-01-restore | 2026-09-05T13:02:32.457063262Z | 2026-09-05T13:02:35.609998440Z | `dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=false --force --no-cache --disable-parallel --verbosity minimal` | 1 | FAIL | logs/eventstore-3102/pkg-01-restore.log | a160bae2803777aa7e78f9fa8014f66d88775faeaa08281d3cb24894f5237191 |
| pkg-02-build | 2026-09-05T13:02:35.614068454Z | 2026-09-05T13:02:36.323002653Z | `dotnet build Hexalith.EventStore.slnx --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | 1 | FAIL | logs/eventstore-3102/pkg-02-build.log | f581133a56a126d2f08cd42a1ce92650252f4cc18d20581ae5095bf2984fd5ca |
| pkg-03-test-contracts | 2026-09-05T13:02:36.326145122Z | 2026-09-05T13:02:36.778429376Z | `dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | 1 | FAIL | logs/eventstore-3102/pkg-03-test-contracts.log | 52cda9e6da69191ff2e3a814370e07197faa92bdff8b1cd37e8fcddfa1f0932a |
| pkg-04-test-client | 2026-09-05T13:02:36.783386811Z | 2026-09-05T13:02:37.240544462Z | `dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | 1 | FAIL | logs/eventstore-3102/pkg-04-test-client.log | 729105adc113cae9175e009a7e046455e97474c07d2f4a593eee2d883ea7b283 |
| pkg-05-test-domainservice | 2026-09-05T13:02:37.244039042Z | 2026-09-05T13:02:38.114154736Z | `dotnet test tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | 1 | FAIL | logs/eventstore-3102/pkg-05-test-domainservice.log | c381949d6fdffdcff5321b46eccfe821c14778dc377a5d12b109d4289879ac47 |
| pkg-06-test-server | 2026-09-05T13:02:38.120571434Z | 2026-09-05T13:02:38.778904137Z | `dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1` | 1 | FAIL | logs/eventstore-3102/pkg-06-test-server.log | 146ec6374812677f49191e5f9e726398ea498d1440030f52ce627cfc7006aec0 |

Notes: identical root cause as the source-mode lane (section 2). `pkg-03` through `pkg-06` logs
are byte-identical to their `src-` counterparts (same `MSB4019` failure independent of
configuration/mode, confirmed by matching SHA-256 hashes above) because in both modes the
missing `Directory.Packages.props` import blocks restore before any mode-specific difference
can matter.

#### Remote-package restore proof (independent of the repository's own Central Package Management)

Disposable consumer project: `/tmp/.../scratchpad/remote-consumer-3102/project/` (created via
`dotnet new console`, then edited to add `PackageReference` items and disable CPM locally).
Retained project file sha256: `a987c36f3b6fab944584db1cc4939f382e71837d2b18feead05a3ff0044a930c`
(final 13-package version; the very first attempt including `Hexalith.EventStore.Admin.Cli` as a
plain `PackageReference` hashed `b3beeb110f3f0ec7edad3c3f54c8969b341cc2fa4c74aa6444a91a07b553e05f`
and is superseded — see `remote-01-restore` below).
NuGet.Config (official `nuget.org` V3 source only, `<clear/>` on sources and fallback folders)
sha256: `e802a002d13da99816e4bc2d280322c073b69f429c582ab70331bda63542b2b5`.

Environment overrides for both rows below: `DOTNET_CLI_HOME=/tmp/.../scratchpad/remote-consumer-3102/nuget-home`, `TMPDIR=/tmp/.../scratchpad/remote-consumer-3102/tmp`.

| id | cwd | started_utc | ended_utc | command | exit | result | log | log_sha256 | notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| remote-01-restore | remote-consumer-3102/project | 2026-09-05T13:03:19.259101129Z | 2026-09-05T13:03:33.677136558Z | `dotnet restore /tmp/.../remote-consumer-3102/project/RemoteConsumer.csproj --configfile /tmp/.../remote-consumer-3102/project/NuGet.Config --force --no-cache --disable-parallel --verbosity minimal -p:RestorePackagesPath=/tmp/.../remote-consumer-3102/packages-cache` (project referenced all 14 package IDs as plain `PackageReference`) | 1 | FAIL | logs/eventstore-3102/remote-01-restore.log | b7b29e9a2563fd94118225df28580b31d1c48a7e6896ce8599c8f1948e12b0d7 | `NU1212 Invalid project-package combination for Hexalith.EventStore.Admin.Cli 3.102.0. DotnetToolReference project style can only contain references of the DotnetTool type` — this package is published as a .NET tool, not a library, so it cannot be consumed via a plain `PackageReference`. Genuine finding, not a stall/cache issue. |
| remote-02-restore-13pkgs | remote-consumer-3102/project | 2026-09-05T13:03:55.469364268Z | 2026-09-05T13:04:11.763119952Z | `dotnet restore /tmp/.../remote-consumer-3102/project/RemoteConsumer.csproj --configfile /tmp/.../remote-consumer-3102/project/NuGet.Config --force --no-cache --disable-parallel --verbosity minimal -p:RestorePackagesPath=/tmp/.../remote-consumer-3102/packages-cache` (project references the other 13 package IDs) | 0 | PASS | logs/eventstore-3102/remote-02-restore-13pkgs.log | 245a32dc8f1c1cdec25e90d601d1805b4bffbf768e444b10b571171fbffbdc0d | All 13 requested `Hexalith.EventStore.*` packages plus their full transitive graph resolved into the brand-new, previously-empty `RestorePackagesPath` from `nuget.org` only. Confirmed by directory listing of the fresh cache (13 `hexalith.eventstore.*` folders present). |
| remote-03-tool-install-admin-cli | remote-consumer-3102 | 2026-09-05T13:04:22.106584588Z | 2026-09-05T13:04:24.508061541Z | `dotnet tool install Hexalith.EventStore.Admin.Cli --tool-path /tmp/.../remote-consumer-3102/tool-install-dir --version 3.102.0 --configfile /tmp/.../remote-consumer-3102/project/NuGet.Config --verbosity minimal` | 0 | PASS | logs/eventstore-3102/remote-03-tool-install-admin-cli.log | d130edc010c2cf07f4dd28b3c824529ce0fc8ed8644f59a577f15e34ed904b73 | Correct consumption path for a `DotnetTool`-typed package (proven by remote-01's `NU1212`); resolves from the same nuget.org-only `NuGet.Config`, into a fresh tool-path directory. `eventstore-admin` binary installed at declared version 3.102.0. |

Combined, `remote-02` + `remote-03` prove all 14 release-manifest package IDs resolve remotely at
exactly `3.102.0` from the official `nuget.org` source only, with no local feeds, no fallback
folders, no floating versions, and a brand-new empty cache/tool-path each time. `remote-01`'s
`NU1212` failure is retained as an honest first result per the "retain every first result" rule,
superseded by the corrected two-step consumption proof in `remote-02`/`remote-03`.

No command in this task stalled (all completed in seconds); no retries, cancellations, or
`INCONCLUSIVE` results were needed anywhere.

## 4. 14-package manifest

`git show v3.102.0:tools/release-packages.json | sha256sum` → `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`
(retained copy: `logs/eventstore-3102/release-packages-3.102.0.json`, same hash).

14 package IDs:

1. `Hexalith.EventStore.Contracts`
2. `Hexalith.EventStore.Client`
3. `Hexalith.EventStore.Server`
4. `Hexalith.EventStore.SignalR`
5. `Hexalith.EventStore.Testing`
6. `Hexalith.EventStore.Testing.Integration`
7. `Hexalith.EventStore.Aspire`
8. `Hexalith.EventStore.ServiceDefaults`
9. `Hexalith.EventStore.DomainService`
10. `Hexalith.EventStore.RestApi.Generators`
11. `Hexalith.EventStore.Gateway`
12. `Hexalith.EventStore.Admin.Abstractions`
13. `Hexalith.EventStore.Admin.Cli` (published as a .NET tool package — consumed via `dotnet tool install`, not `PackageReference`)
14. `Hexalith.EventStore.Admin.Server`

Note: this manifest hash is byte-identical to the historical `v3.89.0` manifest hash recorded in
the qualification contract's dated observations (same 14 IDs, unchanged since that release) —
consistent with, not contradicting, the contract's caution that a package listing alone is not
consumption proof; the remote restore rows above supply that separate proof.

## 5. Overall disposition

**Not every row passed.** The worktree setup and pre/post clean-state checks passed cleanly
(HEAD, describe, and status all confirmed immutable and clean before and after). The independent
remote-package restore proof also fully passed for all 14 package IDs (13 via `dotnet restore`
into a brand-new cache from `nuget.org` only, 1 via `dotnet tool install` since it is packaged as
a .NET tool rather than a library) — genuine remote consumption at exactly `3.102.0` is
demonstrated. **However, both in-repository build lanes are blocked**: all twelve rows across the
source-mode lane (`src-01` through `src-06`) and the package-source lane (`pkg-01` through
`pkg-06`) FAILED, because `Directory.Packages.props` cannot resolve its central package version
pins without the `Hexalith.Builds` submodule, and initializing that submodule (declared only in
EventStore's own `.gitmodules`, not the outer superproject's) is forbidden by this task's explicit
boundary ("Never init nested submodules") and by CLAUDE.md's umbrella-workspace submodule rule.
This is a real, deterministic, immediate failure (`NU1010`/`MSB4019`, not a stall or cache
artifact) and is not something a cache retry or longer timeout would fix. **Blocking issue for the
orchestrator / EventStore Owner**: decide whether initializing `references/Hexalith.Builds`
(pinned non-recursively to the commit this EventStore coordinate declares) is in-scope for
source/package-mode qualification: if yes, this lane should be re-run with that submodule
initialized; if no, the source-mode and package-source build/test lanes should be recorded as
structurally un-runnable from a fully isolated EventStore-only worktree under the current
boundary, and that constraint itself becomes part of the finite record.
