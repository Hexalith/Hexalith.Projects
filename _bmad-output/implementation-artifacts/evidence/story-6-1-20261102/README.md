# Story 6.1 verification attempt — 2026-11-02

Status: **failed; story remains in progress**. This directory contains native test XML, build logs, and the launcher's sanitized result report. The copied distributed report belongs to the final attempt after the authority and Folder liveness fixture corrections.

## Tested revisions

- `.`: `47a1ffd02cb3e1cde80bd7697ac1b46fdefda8cc`
- `references/Hexalith.EventStore`: `7c93a1234d14c66f5ee85ed48caaee1d30a671f3`
- `references/Hexalith.Tenants`: `964d64561b8fdd06e146ef7e304f25f51d276ace`
- `references/Hexalith.Folders`: `531bee88db685b703c7092ea5e2eaf683f15cede`
- `references/Hexalith.Conversations`: `c9e6fdebc7725df3462a1db991f5d69a4b1f22b9`
- `references/Hexalith.Builds`: `d2a430c988462a634674a69c562ea4afd1f06ef4`
- `references/Hexalith.Platform`: `c42c4a017ce72ad1c911c355bf16dc84afdf2008`

The working Projects source was modified from the recorded HEAD; the launcher bound its inputs with `sourceManifestSha256=a6d77160fc0084510bcfe03b2ad1b03fc8fad1b0c43909646d13e5976756be50`. Its full source manifest is retained in the original temporary run directory. These coordinates are local source proof, not published package qualification.

## Commands and results

The Builds `prepare-build` command generated `/tmp/story61-project-map-c0m61myd/result.json` from `module/hexalith.module-manifest.v2.json` in source mode. The commands below ran with its `buildPreparation.environment` plus `MSBUILDDISABLENODEREUSE=1`, `DOTNET_CLI_USE_MSBUILD_SERVER=0`, and `DOTNET_PROCESSOR_COUNT=2`.

- `dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj -c Debug -m:1 -p:NuGetAudit=false`: pass, 0 warnings and 0 errors (`server-only-build.log`).
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:BuildProjectReferences=false --no-restore`: pass, 0 warnings and 0 errors against freshly built Projects.Server and prebuilt sibling assemblies (`server-tests-fallback-build.log`).
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class '*SupportedProjectProjectionTests' -result-xml /tmp/story61-project-map-c0m61myd/projection-tests-final.xml`: 28 passed, 0 failed, 0 skipped (`projection-tests.xml`).
- Same assembly with `-class '*SupportedProjectReadTests' -result-xml /tmp/story61-project-map-c0m61myd/read-tests.xml`: 73 passed, 3 failed, 0 skipped (`read-tests.xml`). The three existing live-fixture cases require owned Dapr endpoint variables absent from this standalone run. Five targeted quarantine selectors passed 6/6 across the named XML files in this directory.
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false`: failed with 8 Conversations.Server compile errors for missing EventStore checkpoint/tenant snapshot types (`server-tests-source-build.log`). No sibling source was changed.
- `python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900 --evidence /tmp/story61-post-probes-20261102`: launcher controls 63/63; owned resources started healthy and cleaned up; Integration test assembly built with 0 warnings/errors (`distributed-build.log`). Its one distributed test failed during `BootstrapGlobalAdmin`: gateway HTTP 503, with `RouteCatalogUnavailableException` in EventStore. The native result is `distributed.xml`; the sanitized launcher report is `distributed-run.json`. No persisted matrix or Server regression XML was produced. Cleanup verified owned sidecars absent, private material removed, and preexisting resources preserved.

## Remaining acceptance

The current EventStore gateway requires an independently activated route catalog before command admission. The supported-read Development fixture does not enroll that catalog; doing so requires a separately scoped route-catalog composition decision. After that and the Conversations source compatibility repair, rerun the full authenticated persisted lane and verify every story matrix row, replay/restart, cursor, authority change, no-domain-write, and XML total before marking 6.1 done.
