---
title: 'Update EventStore package version to 3.110.0'
type: 'feature'
created: '2026-09-30'
status: 'done'
route: 'dispatch'
baseline_commit: 'a0a54820e6b5b5dee317fe51a53291294d27b91e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Builds catalog pins EventStore at `3.109.0`. nuget.org lists `3.110.0` as the latest stable for all 13 `Hexalith.EventStore.*` packages. `tools/release-packages.json` still expects `Hexalith.EventStore.Contracts` `3.106.0`, so consumer validation fails before tests run.

**Approach:** Advance the shared pin and every live coordinate that must equal it to `3.110.0`, point Projects at that Builds commit, and align the consumer manifest with the packed dependency.

**Decision:** Pin only. Leave `qualification-evidence/g-6-runtime-toolchain-20260929/packet.json` unchanged. `g6-candidate` and `g6-acceptance` stay failed until a later qualification. Those jobs do not block `ci`.

## Boundaries & Constraints

**Always:** Use listed stable `3.110.0` from nuget.org. Keep the 13 EventStore rows on `$(HexalithEventStoreVersion)`. Change the pin in `references/Hexalith.Builds`, then set the Projects Builds gitlink and every full-SHA `Hexalith.Builds` reference in `.github/workflows/ci.yml` to that commit. Set the four `Hexalith.EventStore.Contracts` versions in `tools/release-packages.json` to `3.110.0`. Refresh the `hexalith-eventstore` audit family with `Tools/audit-central-package-versions.ps1`. Update fixtures and tests that must equal the live pin.

**Never:** Do not move the EventStore gitlink (`01498ac721db7c44f18fcf9591ffbbf30ba245e2`, nine commits after tag `v3.110.0`). Do not recapture or edit the G-6 packet, Architecture Spine, or P1R records. Do not change stale pins `3.70.1` and `3.88.0`, or the `v3.109.0` fetch in the P1R replay. Do not hand-edit `package-version-audit.json`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Live pins | Catalog, runner, schema, G-4 helper | All read `3.110.0` | Catalog test fails on mismatch |
| Packed consumer | Projects nupkgs | Nuspec and manifest both say `Hexalith.EventStore.Contracts` `3.110.0` | Validator reports `wrong_versions` |
| Local Debug | EventStore checkout present | Compiles against the current source checkout | N/A |
| Release / CI | Package mode | Restores published `3.110.0` | Restore fails if an id lacks `3.110.0` |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props` — `HexalithEventStoreVersion` is `3.109.0`. Projects only imports this catalog.
- `SupportedPlatformPins.cs` `EventStoreVersion`, `schemas/hexalith.module-manifest.v1.json` const, and `Tools/G4PackageQualification.functions.ps1` `$ExpectedEventStoreVersion` — must equal the catalog. `ValidatePlatformPin` and `SupportedPlatformPinsCatalogTests` enforce that.
- Builds tests at `3.109.0` in `test/Hexalith.Builds.Module.Tests/*.cs`, `test/fixtures/module/**`, `Tools/test-g4-tool-package-contracts.ps1`, and `Tools/test-publish-g4-tool-packages.ps1` — live pin. Leave `tampered-platform-pin.json` (`3.70.1`) and `superseded-platform-pin.json` (`3.88.0`). Touch `test/fixtures/evidence/**` only if a test requires the live pin.
- `Tools/audit-central-package-versions.ps1` — refresh `Tools/package-version-audit.json` for `hexalith-eventstore`. `selectedVersion` must equal the catalog.
- `tools/release-packages.json` — four `Hexalith.EventStore.Contracts` rows at `3.106.0`. `scripts/_nuget.py` compares the packed nuspec to this file.
- `Directory.Build.props` — Debug uses the EventStore checkout; CI and non-Debug use packages.
- `.github/workflows/ci.yml` — full-SHA Builds refs `ac58d02cd69f4c74cc71fc05430983d51b58fd84` must equal the gitlink. Leave `@main` refs.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- set `HexalithEventStoreVersion` to `3.110.0` -- one property feeds every EventStore row
- [x] `SupportedPlatformPins.cs`, `schemas/hexalith.module-manifest.v1.json`, `Tools/G4PackageQualification.functions.ps1` -- set the live pin to `3.110.0` -- runner, schema, and helper must match the catalog
- [x] Builds test fixtures and the two G-4 test scripts -- update live-pin assertions -- leave stale pins and frozen evidence
- [x] `references/Hexalith.Builds/Tools/package-version-audit.json` -- refresh family `hexalith-eventstore` -- `selectedVersion` must match the catalog
- [x] `tools/release-packages.json` -- set each `Hexalith.EventStore.Contracts` dependency to `3.110.0` -- packed nuspec must match
- [x] Projects Builds gitlink and `.github/workflows/ci.yml` -- point both at the Builds commit that contains the pin -- CI requires those SHAs to match
- [x] `qualification-evidence/g-6-runtime-toolchain-20260929/packet.json` -- leave it unchanged -- pin-only decision

**Acceptance Criteria:**
- Given the catalog and live pins, when they are evaluated together, then each EventStore coordinate that must match the catalog is `3.110.0`.
- Given a package-mode restore, when Projects resolves `Hexalith.EventStore.*`, then each of the 13 packages is `3.110.0`.
- Given packed Projects packages, when `scripts/validate-nuget-packages.py` runs, then `Hexalith.EventStore.Contracts` is `3.110.0` in the nuspec and the manifest.
- Given the EventStore checkout, when the pin change is done, then its gitlink is still `01498ac721db7c44f18fcf9591ffbbf30ba245e2`.

## Implementation Notes

- Builds tip `caad9bae6da30f667f82f6005758f2916fa766a8` contains the pin (`32aa7dd21773a179089764f6bc43d38ff6464f34`), the audit (`aa734324fdcda74e68ac4cf47db97fec103d5793`), and the G-4 helper test. Local only; not pushed.
- Incremental audit refresh failed before any write: prior `xunit` consumer evidence has a duplicate consumer that does not match `representativeConsumers`. A complete `audit-central-package-versions.ps1` run was used instead. The only `selectedVersion` changes are the 13 EventStore rows, `3.109.0` to `3.110.0`. `validate-package-version-audit.ps1` passed for 300 packages.
- Projects restore in package mode resolves 9 EventStore ids, all `3.110.0`. The other four catalog ids are pinned and not referenced by Projects.
- Packed Contracts, Projects, Client, and Testing nuspecs depend on `Hexalith.EventStore.Contracts` `3.110.0`. `scripts/validate-nuget-packages.py` still exits 1 because ServiceDefaults packs OpenTelemetry `1.19.1` while the manifest says `1.19.0`. That mismatch predates this pin.
- EventStore gitlink remains `01498ac721db7c44f18fcf9591ffbbf30ba245e2`. The G-6 packet was not edited.
- `CatalogDefaultsMatchSupportedPlatformPins` and `PlatformPinSchemaAndRuntimeValidationRemainInParity` passed. `tests/tools/run-ci-workflow-gates.ps1` passed.

## Spec Change Log

## Review Triage Log

- blind: audit `latestStable` moved for non-EventStore packages — `low`, rejected. `selectedVersion` changed only for the 13 EventStore rows. A complete refresh records current NuGet latest observations; that is the generator's job.
- blind: historical audit rows were rewritten — `false`. Sampled history for Authentication.Google, Identity, and MinVer still starts with the prior tuples. The refresh appended observations; `validate-package-version-audit.ps1` passed.
- blind: incremental `hexalith-eventstore` refresh did not run and the xunit consumer duplicate remains — `medium`, deferred. The new audit still has 6 xunit consumer rows and 5 unique paths. That duplicate predates this pin and blocks the next incremental refresh.
- blind: `pin-mismatch.json` still says EventStore `3.109.0` and Dapr `1.18.0` — `false`. `pin-mismatch.expected.json` requires only `HXE202`. `G4P0AcceptanceValidatorTests` passed, 39/39.
- blind: `test/fixtures/evidence/negative/evidence/` still embeds the old manifest hash — `maybe-false`, rejected. If true, the harm is test isolation only. Those snapshots were left alone because no test required the live pin.
- blind: `CatalogDefaultsMatchSupportedPlatformPins` uses `ShouldContain` and does not read the schema — `low`, rejected. Schema parity is `PlatformPinSchemaAndRuntimeValidationRemainInParity`, which passed. The helper assertion matches the default-parameter line tied to `SupportedPlatformPins.EventStoreVersion`.
- blind: CI `uses:` SHAs are unpublished — `false`. They equal local Builds `caad9bae6da30f667f82f6005758f2916fa766a8`. GitHub cannot fetch that commit until it is pushed; this step does not push.
- blind: package-mode restore does not show all 13 EventStore ids — `false`. All 13 catalog rows use `$(HexalithEventStoreVersion)` `3.110.0`. Projects references 9 of them, and the restore resolved those 9 at `3.110.0`.
- blind: `validate-nuget-packages.py` exits 1 and the Code Map is stale — `medium` for OpenTelemetry, deferred; the Code Map note is rejected because its fix is an edit to this spec. ServiceDefaults packs OpenTelemetry `1.19.1` while the manifest says `1.19.0`. The four EventStore.Contracts nuspecs are `3.110.0`. Implementation notes record that result.
- blind: three module tests still pass Dapr `1.18.0` — `false`. Those `PlatformPins` literals already used `1.18.0` before this diff. The diff replaced only the EventStore component. They do not validate against the schema const.
- edge-case-hunter: no findings.
- verification-gap: no findings.

## Verification

**Commands:**
- `dotnet test references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --filter CatalogDefaultsMatchSupportedPlatformPins` -- expected: pass with `3.110.0`
- `dotnet restore Hexalith.Projects.CI.slnx -p:UseHexalithProjectReferences=false` -- expected: EventStore packages resolve to `3.110.0`
- `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py ./nupkgs` -- expected: `Hexalith.EventStore.Contracts` `3.110.0` matches
