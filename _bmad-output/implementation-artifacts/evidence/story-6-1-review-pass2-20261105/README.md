# Story 6.1 second-pass review patches: focused verification (2026-11-05)

This packet verifies the five patches from the second-pass code review of `a6c415f..19a0548`. These are focused local Debug/source runs. The distributed authenticated lane (`run_supported_project_reads.py`) was **not** rerun against this source. The AC7 distributed receipts stay bound to manifest `fd22b04355f941d920ee3ecaa5d5b68bea047e7f01486df55cc33d2a30451e22`.

## Source

- Projects HEAD `19a0548d0e84584092d9ca50693cedfa8805cb5c`, plus the uncommitted patch in `source-changes.patch`, which covers four files:
  - `ProjectReadQueryExecutor.cs`
  - `SupportedProjectReadDistributedFixture.cs`
  - `SupportedProjectProjectionTests.ReviewPatch.cs`
  - `SupportedProjectReadTests.ReviewPatch.cs`
- The patch has SHA-256 `0e4be3808e37516bdafb8fd6903f6ade06627ba8b73145e9fbe8c020cd028a40`, computed over the working-tree `git diff` bytes when the packet was written. Git line-ending normalization can change those bytes after checkout, so to recheck it, regenerate the diff from the same files.
- The submodules are at the pins recorded in `19a0548`.
- SDK `dotnet` 10.0.403.

## Environment

Builds and test runs used the following environment:

- The Platform-selected source mapping from `dotnet run --project references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace <private> --output json`, with `HEXALITH_SOURCE_MAPPING_HASH=338ef4355d7aa2fea32ffa1fe1ee0dc62a5b53edb54a4a8b63fa699fd7219c56`.
- The launcher's ordinary flags: `UseHexalithProjectReferences=true`, `UseNuGetDeps=false`, `EnablePrivateCatalogInvocationProof=true` and `EnablePrivateOwnerSourceReadProof=true`.
- Root-declared `Hexalith*Root` paths under `references/`.
- `MSBUILDDISABLENODEREUSE=1` and `DOTNET_CLI_USE_MSBUILD_SERVER=0`. The Integration build and run also used `DOTNET_PROCESSOR_COUNT=2`.

## Commands and results

| Step | Command | Result |
| --- | --- | --- |
| Server build | `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 --disable-build-servers --artifacts-path <scratch>/artifacts` | exit 0, 0 warnings, 0 errors (local `server-build.log`) |
| Server local lane | `dotnet <scratch>/artifacts/bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll -class- '*SupportedProjectAuthorityHttpTests' -method- '*GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites' -method- '*ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart' -method- '*GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures' -xml server.xml` | **1,335/1,335** passed; 0 failed, 0 errors, 0 skipped, 0 not-run |
| Integration build | `dotnet build tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj -c Debug -m:1 -p:SupportedProjectReadDistributed=true --disable-build-servers --artifacts-path <scratch>/artifacts` | exit 0, 0 warnings, 0 errors (local `integration-build.log`) |
| Integration local lane | `dotnet <scratch>/artifacts/bin/Hexalith.Projects.Integration.Tests/debug/Hexalith.Projects.Integration.Tests.dll -class- '*SupportedProjectReadDistributedTests' -xml integration.xml` | **222/222** passed; 0 failed, 0 errors, 0 skipped, 0 not-run |

The local lanes exclude the same live selectors as the earlier parent verification, so they provide no AC7 coverage. The Server total is the previous local 1,331 plus four new cases: `missing-tenant`, `missing-project`, `missing-reason` and `SkewedEventTimestampDegradesOnlyItsRowAtReadTime`. The renamed theory `RejectionNoOpWithInvalidOrMissingContentCannotAdvanceSequence` passes all six cases, and the updated `ConfirmedFolderReplacementKeepsEnumerationCompleteAndReadsPartial` passes. The fixture change that makes the status poll fail fast on deterministic refusals compiles in the Integration build, but it runs only in the distributed lane.

Assembly SHA-256: Server.Tests `60c413bbe07e01edb68a01b3e56a20be273513893a22fdf24d2c1c71dd0742c3`, Integration.Tests `1f02822ea60c87cfa8fc11aff45ec5b1cc6b77fae7edb887ff888f8320875e8d`.

## Retained files (SHA-256)

- `server.xml` `dc84af8063c6c81027c2a7bb76a6528c6570014a41c819425c5bcd1267e283bc`
- `integration.xml` `6d777d4752c5fe5a37981c339efecb90e638ef2ea4cb72832883ec7b8d8b69c1`

The build logs exist only in the local checkout, because the repository ignores `*.log`:

- `server-build.log` `fd4d851975e1293d4ebdfe3f497ad54371cc4658c9da976db07dc5ad7eb8a86c`
- `integration-build.log` `ef536307454c4c5def9420386254bb90eb059a63065800e6f3259580416f9b01`

## Commit reconciliation of the final acceptance (no rerun)

Final acceptance was recorded against worktrees plus patches (`parent-review/source-changes/patch-checks.json` in `story-6-1-platform-maintenance-20261105`). Whitespace-insensitive `git patch-id --stable` comparisons of those patches with the commits that landed give the following results:

- **Projects:** `git diff fb6e1df 39aa170` over the patch's files equals `Projects.patch` (patch-id `f4b6a84a…`). No non-evidence file outside the patch changed.
- **Tenants:** `8e20524..cfb5d1a` equals `Tenants.patch` (patch-id `c817cd55…`), with no other changes.
- **Folders:** `ff915ec..e88c940` equals `Folders.patch` (patch-id `60528308…`), with no other changes.
- **EventStore:** over the patch's 30 files, `bbc165a..5df9640` equals `EventStore.patch` (patch-id `81c1c083…`). It also includes two changes that were never in the tested source:
  - `d9498bb15` changes `tests/Hexalith.EventStore.DomainService.Tests/DomainServiceTrustBoundaryTests.cs` and `tests/Hexalith.EventStore.Testing.Tests/CatalogInvocationTests.cs`.
  - `7b248e03b` and `0245c39c2` move EventStore's nested `references/Hexalith.Builds` and `references/Hexalith.Platform` pointers. The net change is in Platform only. Umbrella builds resolve the root-declared Builds and Platform instead.

So the Projects code and the sibling code that the acceptance used are now committed in `39aa170` and `19a0548`. The EventStore difference is limited to its own tests and to nested pointers that the umbrella does not use. As decided by Jerome on 2026-11-05, the two changed EventStore test classes were not rerun.
