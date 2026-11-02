# Story 6.1 follow-up verification — 2026-11-02

Status: **failed; Story 6.1 remains in progress**. This packet retains native xUnit XML, build logs, the launcher's sanitized report, source manifest, and cleanup result. The supported read source was not changed in this attempt; `tools/qualification/run_supported_project_reads.py` was changed to prepare the Platform-selected Projects Debug source mapping inside its isolated run and to remove that private mapping safely.

## Tested source

- Projects HEAD: `3f2931a4680180db177d315aab327087eee05fef`; the launcher change was uncommitted and is bound by `sourceManifestSha256` in `run.json`.
- EventStore: `7c93a1234d14c66f5ee85ed48caaee1d30a671f3`.
- Tenants: `964d64561b8fdd06e146ef7e304f25f51d276ace`.
- Folders: `531bee88db685b703c7092ea5e2eaf683f15cede`.
- Conversations: `c9e6fdebc7725df3462a1db991f5d69a4b1f22b9`.
- Builds: `d2a430c988462a634674a69c562ea4afd1f06ef4`.
- The complete root-declared revision inventory and source hash are in `run.json` and `source-manifest.json`.

## Commands and results

From the Projects root:

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900
```

The launcher prepared the Projects-root source mapping, built the Integration test assembly, started healthy owned EventStore, Tenants, Projects, Folders, worker, sidecar, and authority resources, then ran the distributed selector. Its launcher controls passed **63/63**, with 0 failures and 0 skips. The native `distributed.xml` has **1 total, 0 passed, 1 failed, 0 skipped**: `BootstrapGlobalAdmin` was refused with HTTP 503. EventStore runtime diagnostics recorded `RouteCatalogUnavailableException`. The authenticated persisted matrix did not execute; no Integration or Server regression XML was produced by the launcher. `run.json` reports `matrixCompleted=false`.

The launcher stopped its exact AppHost and removed every owned sidecar/container. It reports `preexistingResourcesPreserved=true` and `privateMaterialRemoved=true`. Earlier failed preparatory runs were also cleaned up; their results do not contribute to acceptance.

Additional local checks used the freshly prepared Projects-root Debug source mapping:

- `dotnet build src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj -c Debug -m:1 -p:NuGetAudit=false`: **pass**, 0 warnings/errors (`apphost-build.log`).
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false`: **fail**, 8 missing Conversations.Server/EventStore checkpoint and Tenant snapshot type errors (`server-tests-source-build.log`).
- Same Server.Tests build with `-p:BuildProjectReferences=false --no-restore`: **pass**, 0 warnings/errors against freshly built Projects.Server and prebuilt sibling assemblies (`server-tests-fallback-build.log`).
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class '*SupportedProjectProjectionTests' -xml <packet>/projection.xml`: **28 passed**, 0 failed/skipped.
- Same assembly with `-method '*Quarantin*' -result-xml <packet>/quarantine.xml`: **2 passed**, 0 failed/skipped; `-method '*AdministratorInventoryAuditsEveryReturnedProjectAndBindsCursorToInventory*' -result-xml <packet>/inventory.xml`: **1 passed**, 0 failed/skipped.
- Same assembly with `-class '*SupportedProjectReadTests' -result-xml <packet>/read.xml`: **73 passed, 3 failed, 0 skipped**. The three live gateway catalog cases require the launcher's owned Dapr endpoints, absent from this standalone run.

The retained legacy/supported HTTP comparator was run separately against the same Debug test assembly (SHA-256 `567df6104f09ccabbb9a0ca88d83caffc51438e52aabf306d1dcdc8738af96a2`):

```bash
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -method '*LegacyHttpListAndOpenMatchAuthorizedSupportedMetadataAcrossPages*' -result-xml _bmad-output/implementation-artifacts/evidence/story-6-1-20261102-followup/shadow.xml
```

Native [shadow.xml](shadow.xml) records **1 passed, 0 failed, 0 skipped**. This covers the local additive list/open comparison only; it does not substitute for the distributed authenticated matrix or Story 6.7's exhaustive migration equivalence.

`<packet>` above is `_bmad-output/implementation-artifacts/evidence/story-6-1-20261102-followup`.

## Acceptance still open

AC7 is blocked: no authenticated persisted list/open, no-domain-write, replay/duplicates, process restart, cursor, or authority-change matrix completed at this revision. Thus the live end-to-end parts of AC1–3 and AC5–6 are not accepted by these local checks. AC4's local comparator passed separately, while the launcher did not reach its regression phase; exhaustive cutover equivalence belongs to 6.7. The Story 6.1 scope explicitly excludes route-catalog enrollment and sibling source changes, so the EventStore route-catalog and Conversations source compatibility failures remain outside this attempt.
