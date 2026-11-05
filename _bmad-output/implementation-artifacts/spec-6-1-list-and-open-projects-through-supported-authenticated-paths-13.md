---
title: 'Story 6.1: Unblock authenticated bootstrap source proof'
type: 'bugfix'
created: '2026-11-05'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
baseline_commit: '0047c961ccad19f1a7344095ecb70bcd8e4bb65b'
context:
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
  - '_bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Finish Story 6.1's fresh authenticated verification. Its bootstrap source helper always throws because the workload SDK lacks explicit platform-scope stream support. Earlier fixture/observer repairs are implemented and reviewed.

Proposed continuation: repair shared EventStore support, provide a finite owner-hosted Development test helper, then complete Projects' strict persisted bootstrap proof and existing acceptance lane. Previous scope restrictions remain effective pending the decision below.

## Boundaries & Constraints

**Always:** Preserve original-request assertion/proof binding, exact approved storage mappings, receipt/causation/correlation/event checks, privacy, authority, no-domain-write assertions and owned cleanup. Keep credentials inside Tenants. Keep story/sprint in progress until required evidence passes.

**Never:** Add public endpoints, generic relays, Projects-owned transport or impersonated owner assertions; grant public `system` access; rewrite history; weaken checks; change pins, branches or submodule checkouts; stage, commit or push. Production enrollment and other stories remain outside scope.

## I/O & Edge-Case Matrix

| Input/state | Expected behavior |
| --- | --- |
| Approved platform scope/domain | Explicit scope header; accept only mapped storage Tenant and exact Domain/AggregateId. |
| Unsupported pairing or conflicting headers/response identity | Fail closed without changing proof binding. |
| Ordinary Tenant request | Existing transport and exact Tenant validation. |
| Explicit Debug/Development helper selection | Exact administrator stream through SDK and real owner credentials. |
| Wrong helper target/mode/identity, expiration or oversized result | Refuse; propagate failures, never synthesize successful proof. |

</frozen-after-approval>

## Open Questions

1. Expand the previous Projects-only authorization? **Repair owners here (recommended):** authorize EventStore Client/Testing changes, Tenants test-only enrollment and Projects wiring below. **Keep Projects-only:** retain the handoff and leave acceptance blocked until the owners supply the capability. No sibling edits have occurred.

## Code Map

Paths below are relative to their named owning repository: EventStore at `references/Hexalith.EventStore`, Tenants at `references/Hexalith.Tenants`, Projects at this workspace root.

- EventStore `src/Hexalith.EventStore.Client/Gateway/EventStoreGatewayClient.cs`, `ReadWorkloadStreamAsync`: missing scope header/mapped response validation. Reuse `LegacyTenantStorageMap.IsPlatformSourceRead` and `TenantCanonicalizer.ReservedStorageTenant`; update adjacent `IEventStoreGatewayClient.cs` documentation after verification. Existing `StreamsController` already enforces platform admission and original-request proof.
- EventStore `src/Hexalith.EventStore.Testing/CatalogInvocation/`: reuse finite private IPC, deadlines and failure patterns. Existing worker's gateway issuer cannot impersonate Tenants. New `src/Hexalith.EventStore.Testing/OwnerSourceRead/` helper must run in the owner process; one type per file.
- Tenants `src/Hexalith.Tenants/Program.cs` and adjacent `Hexalith.Tenants.csproj`: explicitly selected Debug/source enrollment using actual issuer, bound key and catalog-fenced route. Preserve production owner-source behavior.
- Projects `src/Hexalith.Projects.AppHost/SupportedProjectReadDevelopmentInfrastructure.cs`, adjacent `SupportedProjectReadFixtureProfile.cs`, and `tools/qualification/run_supported_project_reads.py`: helper selection, private directory/build custody and cleanup.
- Projects `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs`, `ReadHostedBootstrapSourceAsync`: replace unconditional guard; retain subsequent exact event/receipt validation.

## Tasks & Acceptance

- [ ] EventStore Client paths above: implement exact platform mapping. Extend `tests/Hexalith.EventStore.Client.Tests/Gateway/EventStoreGatewayWorkloadClientTests.cs` with both mappings, wrong identities, original proof/assertion binding and ordinary-Tenant controls.
- [ ] EventStore Testing path above: add separate owner-source helper restricted to `platform:global-administrators` / `global-administrators` / `global-administrators`. Add `tests/Hexalith.EventStore.Testing.Tests/OwnerSourceReadTests.cs` for admission, SDK dispatch, bounds and failure propagation.
- [ ] Tenants host paths above: opt-in enrollment only; extend `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantOwnerSourceRuntimeTests.cs` for real issuer/catalog composition and default refusal.
- [ ] Projects paths above: wire shared helper; extend `tests/tools/test_supported_project_reads_launcher.py` and Integration `SupportedProjectReadTopologyTests.cs` / `SupportedProjectReadEvidenceTests.cs` for selection, proof and cleanup.
- [ ] Projects `_bmad-output/implementation-artifacts/evidence/`: retain commands, full tested commits/source changes, XML, totals and cleanup; append factual results to story/sprint tracking.

Given valid bootstrap, when its actual source is read, then exactly one persisted administrator event matches the original command, correlation and actor without remapping event history.

Given current source, when distributed qualification and regressions finish, then all seven story criteria have explicit evidence or an exact remaining blocker; historical receipts never substitute for changed-source acceptance.

## Implementation Notes

The gap was confirmed at EventStore `2dca77cc2f22a9b85e4fcc3f8e18cbcaa532b135` and rechecked after external advancement to `bbc165a5cd3a1f21822b4b53daa7fd895883f8a5`. Tenants is `8e20524574901d71ad5f4876804c64c3c6c87b88`. Multiple submodule checkouts advanced externally during planning; preserve them and recheck source/status before implementation. Retained local results are Integration 208/208 and Server 1,331/1,331, excluding live selectors; not rerun here. Package-mode NU1109 remains a separate unresolved conflict. No irreversible production changes; new callable helper is confined to Testing and explicitly selected local hosts.

## Spec Change Log

## Review Triage Log

## Verification

Inspect owned runtime before code changes through the existing isolated lifecycle. Build relevant individual owner projects in Debug/source mode, then run built xUnit assemblies with `-class` selectors for workload/stream client, controller, helper and owner-runtime controls. Require nonempty passes and clean builds.

Run `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py`, then fresh `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900`. Require persisted scenarios, relevant full local assemblies and owned cleanup. Retain failed attempts separately; run `git diff --check` per changed repository.
