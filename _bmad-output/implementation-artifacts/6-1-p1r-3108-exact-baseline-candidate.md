# 6.1-P1R current exact-baseline candidate — owner decision packet

Recorded 2026-09-26. **Status: pending.** The 2026-09-25 owner direction chose EventStore `3.108.1` for revalidation. This packet prepares the new decision; it records no new P1R, G-6, P0, or Story 6.1 approval.

## Exact coordinates

| Coordinate | Candidate / observation |
| --- | --- |
| EventStore release packages | Version `3.108.1`, tag `v3.108.1`, release-source commit `b15ad59abca82d5980ef92a510c2379e05f4d46f` |
| EventStore release manifest | `references/Hexalith.EventStore/tools/release-packages.json`, 14 package IDs, SHA-256 `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae` at the tag; unchanged in the current checkout |
| EventStore current source checkout | `8ac62359c1ffdd520488ecd03bd9689c217e3788`; 28 commits after the tag. This is **not** the release-package source coordinate. |
| Builds current source | `2326f983bad14d5398ee55bf2bdf6c86b63c39ea` (`main` and `origin/main` at inspection) |
| Builds tool packages | The retained `0.0.0-stage5.8` qualification is local and earlier than this candidate revision; `published_consumer_pin` is absent. No published Builds tool package version is selected by this packet. |
| Builds catalog / runner / schema | Catalog `HexalithEventStoreVersion=3.108.1`; runner `SupportedPlatformPins.EventStoreVersion=3.108.1`; module-manifest schema `platform.eventStoreVersion const=3.108.1`; package qualification helper expects `3.108.1`. |

The historical accepted P1R tuple remains EventStore `3.106.0` / `v3.106.0` / `76051c70cbf868c40edc00ca0344fa5bd8879b69` plus Builds `ad52f350a2f0bc47849179ae17b4594dafff5363`. Its fixed JSON record, guard constants, and sprint index retain that tuple. The rollback remains EventStore `3.70.1` / `v3.70.1` / `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` plus Builds `7af20f8bafbfe561df6f7705913a0800603090b5` until an owner changes it.

The Architecture Spine Stack table still binds EventStore **`3.70.1`** at `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md:378`. The Solution Architect must explicitly approve conformance and rebinding to the exact `v3.108.1` tag commit plus Builds revision above; this packet does not silently replace that architecture binding.

## Verification boundary

- Historical guard: `python3 tools/planning/validate_production_authority.py --validate-index` exited `0`: `PASS: production-authority index is [6, 7, 8]`. This proves internal consistency of the **3.106.0** acceptance only.
- Builds: `pwsh -NoProfile -File Tools/test-authoritative-package-catalog.ps1` exited `0` (50 identities, 3 shared versions). The current Debug Module test assembly passed 214/214, including 43/43 manifest-validation tests, after a successful targeted restore. The direct assembly runner was used because project-level `dotnet test` discovered zero tests under Microsoft.Testing.Platform; `TMPDIR=/var/tmp` avoided an unrelated `/tmp/.git` fixture-root collision.
- EventStore: `git rev-parse 'v3.108.1^{commit}'`, `git rev-parse HEAD`, and `git diff --quiet v3.108.1 HEAD -- tools/release-packages.json` established the tag, later checkout, and unchanged manifest. The manifest hash proves only source inventory. Published `3.108.1` package provenance, hashes, and consumption evidence for all 14 manifest IDs have not been supplied here and remain required Test Architect inputs.
- G-6: `python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/packet.json` exited `1`: `G6-EVIDENCE-INVALID: Central package pin drift: CommunityToolkit.Aspire.Hosting.Dapr`. The retained baseline pins `13.5.1-beta.757`; the current Builds catalog pins `13.5.1-beta.767`.

The historical G-6 `source-state.json` is also stale. A normalized-LF SHA-256 comparison with the current workspace found **29 changed file bindings**: Projects `0/8`, Builds `24/36`, Conversations `0/3`, EventStore `1/6`, Folders `0/2`, FrontComposer `3/4`, Memories `0/5`, Parties `0/3`, Tenants `1/2`. The changed paths are:

- Builds: `.github/workflows/domain-ci.yml`, `.github/workflows/domain-release.yml`, `README.md`, `schemas/hexalith.module-manifest.v1.json`, `src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`, `test/Hexalith.Builds.Module.Tests/ManifestValidationTests.cs`, `test/Hexalith.Builds.Module.Tests/ModuleCommandApplicationTests.cs`, `test/Hexalith.Builds.Module.Tests/ModuleRunEvidenceSerializationTests.cs`, `test/fixtures/module/negative/absolute-path.json`, `duplicate-id.json`, `duplicate-json-key.json`, `invalid-profile.json`, `malformed-dependency.json`, `missing-descriptor.json`, `missing-required-value.json`, `path-escape.json`, `placeholder.json`, `secret-bearing.json`, `unknown-field.json`, `unknown-schema.json`, `unsupported-profile-class.json`, `test/fixtures/module/positive/hexalith.module-manifest.v1.json`, `.github/workflows/ci.yml`, and `Props/Directory.Packages.props`. The abbreviated negative fixture names all share the preceding `test/fixtures/module/negative/` directory.
- EventStore: `tools/validate-oq8-platform-evidence.py`.
- FrontComposer: `.github/workflows/quality.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`, and `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`.
- Tenants: `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`.

The packet's repository revision bindings have also changed in Projects, Builds, Conversations, EventStore, Folders, FrontComposer, and Tenants. Its retained approval applies only to its recorded source state. G-6 must be executed and accepted again for the current baseline; its historical packet must not be resealed.

## Decisions still required

1. **EventStore Owner:** accept or reject the `v3.108.1` tagged release source and package family for P1R, and disposition the later checkout's compatibility changes separately.
2. **Builds Owner:** accept or reject Builds `2326f983bad14d5398ee55bf2bdf6c86b63c39ea` with that EventStore release package coordinate; confirm the current catalog, runner, and schema form the selected tuple.
3. **Solution Architect:** assess conformance of the exact EventStore tag and Builds revision against the Architecture Spine's current `3.70.1` Stack binding and rollback plan; explicitly approve or reject rebinding that Stack entry to the `3.108.1` tuple, then record a named decision.
4. **Test Architect:** obtain published-package provenance, content hashes, and independent restore/consumption evidence for all 14 EventStore `3.108.1` release-manifest IDs, tied to the tag commit; assess source, runner, rollback, and qualification evidence for the exact tuple, then record a named decision. The manifest hash and earlier Stage 5 local evidence do not replace this decision.
5. **G-6 Builds, Platform, and FrontComposer/Web owners:** resolve the changed toolchain pins and 29 source bindings, decide whether the Dapr control-plane swap is acceptable for re-execution, review a fresh packet, and accept or reject it.
6. **Projects P1R record authority:** after those decisions, update the fixed acceptance record, guard constants, qualification contract, companion `_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/SPEC.md`, Architecture Spine binding, and sprint index as one coordinated exact-tuple transition. No such transition is authorized by this revalidation packet.

P0 Stage 6 remains unready pending a green current Builds CI, accepted exact P1R tuple, and refreshed owner-accepted G-6. P2, P3, P4, implementation readiness (`NOT_READY`), and Story 6.1 (`blocked`) retain their existing states. Story 6.1 implementation has not started.
