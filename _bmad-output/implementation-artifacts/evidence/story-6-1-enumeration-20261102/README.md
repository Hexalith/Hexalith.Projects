# Story 6.1 enumeration correction — 2026-11-02

Status: **focused correction verified; feature acceptance incomplete**. Story and spec remain in progress. Successful list pages now describe current authorized enumeration independently of each row's readiness. Row state, freshness, omissions and recovery guidance remain intact. Existing early unavailable/denial paths are unchanged. The human event catalog now documents the three existing ChatBot authority events.

## Tested source and limits

Projects HEAD: `98efe700b60cacfe0b3cc3cb0c134e0902419b4c`, plus this uncommitted response/test/documentation correction. The baseline [run.json](run.json) records all root-declared dependency revisions and its original source-manifest digest. That baseline ran before this correction; it does not qualify the changed source. The focused tests compile the changed Projects.Server and Server.Tests against existing sibling build outputs through the documented fallback, rather than qualifying the complete current dependency source graph.

The focused XML contains **112 total: 111 passed, 0 failed, 1 skipped, 0 errors**. Its seven new cases are included in the 111 passes. A final run of those seven cases after CRLF normalization reports **7 passed, 0 failed, 0 skipped**. The skipped test is the existing `ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart`, which requires the isolated Dapr lane. The three `GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites` theory cases were explicitly excluded from this standalone selection. These exclusions are missing distributed coverage, not passes.

- [focused.xml](focused.xml): read controls 80/80; projection/catalog controls 28/28; HTTP 3 passed and 1 skipped, including the passing retained legacy/supported HTTP comparator.
- [enumeration.xml](enumeration.xml): mixed Complete/Partial/Unavailable rows at page sizes 1, 2 and 4; four authorized empty-enumeration scenarios. Tests compare row/open snapshots, cursor behavior, stored values, ETags, write count and absence of inspection audits for Safe Metadata reads.
- [server-fallback-build.log](server-fallback-build.log), [tests-fallback-build.log](tests-fallback-build.log), [tests-final-build.log](tests-final-build.log): fresh Debug compilation of changed Projects code/tests, each with 0 warnings/errors; sibling projects are not rebuilt.

## Exact commands and results

Working directory: `/home/administrator/projects/hexalith/projects`. Each command below used the environment assignments `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2`.

The baseline command exited **1** before AppHost startup or owned resource allocation:

```bash
python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900
```

The launcher's required controls passed **63/63** ([launcher-controls.log](launcher-controls.log)). `run.json` records `matrixCompleted=false`, `preexistingResourcesPreserved=true`, `privateMaterialRemoved=true`, and a source-preparation command failure. No distributed test or native distributed XML was produced.

A direct retry retained the underlying source-preparation diagnostic; it exited **2**:

```bash
dotnet run --project references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace /tmp/story61-resume-source-mapping-20261102 --output json
```

[prepare-build.json](prepare-build.json) reports `HXW009 / PrerequisiteUnavailable`: pinned Builds file `Props/Directory.Packages.props` at `4456dfb7869a307d009038c6b8523a9b6c0a732e` cannot be read from the root-declared Builds repository. A read-only `git -C references/Hexalith.Builds cat-file -t 4456dfb7869a307d009038c6b8523a9b6c0a732e` also failed: `fatal: git cat-file: could not get object info`.

Normal source builds each exited **1**:

```bash
dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj -c Debug -m:1 -p:NuGetAudit=false --no-restore
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:HexalithBuildsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Builds --no-restore
```

The [Server source log](server-source-build.log) records two `MSB4019` errors for Conversations' missing nested Builds import. The [source test build log](tests-source-build.log) records 259 errors, including missing Tenants `HexalithIdentifier`/contract attributes and Conversations' required Platform-selected source mapping. Nested submodules were not initialized, dependency versions were not changed, and no build gate was weakened.

The focused fallback compiled the changed Server and tests; each command exited **0**:

```bash
dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj -c Debug -m:1 -p:NuGetAudit=false -p:HexalithBuildsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Builds -p:BuildProjectReferences=false --no-restore
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:HexalithBuildsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Builds -p:BuildProjectReferences=false --no-restore
```

The test fallback build was repeated after CRLF normalization, with the same command and exit **0**. The following assembly invocations each exited **0**:

```bash
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class '*SupportedProjectReadTests' -class '*SupportedProjectProjectionTests' -class '*SupportedProjectReadHttpTests' -method- '*GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites' -result-xml /tmp/story61-enumeration-focused.xml
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -method '*CompleteEnumerationPreservesMixedRowEvidenceAcrossContinuations' -method '*CurrentAuthorizedEmptyEnumerationIsComplete' -result-xml /tmp/story61-enumeration-new.xml
```

The XML files are copied here unchanged. Both production-authority checks and whitespace validation exited **0**:

```bash
python3 tools/planning/validate_production_authority.py --story-id 6.1
python3 tools/planning/validate_production_authority.py --validate-index
git diff --check
```

## Remaining acceptance

AC7 and the current authenticated persisted portions of AC1–3/5–6 remain unaccepted. The present run stops at the missing pinned Builds source object. The earlier [catalog prototype diagnostic](../story-6-1-20261102-catalog-prototype/README.md) separately records route-catalog enrollment and Dapr mTLS caller-identity failures; this run did not reach or resolve them. Complete current-source verification and the real authenticated persisted matrix remain required. No sibling changes, enrollment, public-route cutover, commit, publication, or story completion is claimed.
