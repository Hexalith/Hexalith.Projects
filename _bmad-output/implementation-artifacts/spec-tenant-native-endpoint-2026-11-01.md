---
title: 'Preserve the enrolled Dapr endpoint for native tenant authority reads'
type: 'fix'
created: '2026-11-01'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '434fefb0f8140afc78d33747d398051e388fdf41'
context: []
---

<frozen-after-approval reason="user authorized actual CI repairs, pushes and release without human validation">

## Intent

Repair the demonstrated Tenants source-profile bootstrap authority failure by preserving the configured Dapr endpoint at its owner registration. Actual run41361188323 atTenants434fefb0 and EventStorea825 accepts bootstrap and completes its global-administrator projection, but19 native reads fail at the v2 egress endpoint guard before reaching the server.

## Boundaries & Constraints

Keep the shared exact endpoint guard, all native stream proof/authority validation, workload assertions, bound proofs, cancellation and timeout budgets unchanged. Preserve the existing fallback when DAPR_HTTP_ENDPOINT is absent. Do not initialize nested submodules or start a local topology/backend. Do not modify unrelated work or weaken source-profile/receipt/persisted-state assertions. Parent owns all commits/pushes and remote qualification. Do not invoke or render bmad-build again; this is direct step03 of the existing parent workflow.

</frozen-after-approval>

## Code Map

- Tenants src/Hexalith.Tenants/Program.cs68-74 reads DAPR_HTTP_ENDPOINT then unconditionally rewrites localhost to127.0.0.1 before registering the typed TenantOwnerSourceGateway HTTP client.
- EventStore src/Hexalith.EventStore.ServiceDefaults/Routing/RouteCatalogInvocationHandler.cs157-159 and216-222 rejects requests with a scheme/host/port different from the committed authority coordinates. Actual diagnostic The v2 invocation uses a different Dapr runtime endpoint occurs19 times, matching19 native attempts with38 HTTP start events and no response-header/end events. This guard is correct and remains unchanged.
- Tenants tests/Hexalith.Tenants.IntegrationTests/TenantOwnerSourceGatewayTests.cs ProductionRegistration currently expects rewritten127 and its source-only catalog fixture omits TechnicalHttpReceivers, so it bypasses the failing v2 endpoint branch. It already exercises the production registration, complete bound request, signing, per-send Dapr token and neighboring scope refusals.
- TenantOwnerCatalogTestAuthority is the existing source-mode catalog fixture. Prefer a local helper/separate focused fixture for actual v2 snapshot coordinates and technical receiver enrollment; do not broaden unrelated legacy fixture semantics.
- Parent RCA raw log /tmp/projects-release-maintenance/tenants-434f-source-failed.log. The failed credential-free artifact contains only fresh backend provenance; it does not claim functional acceptance. Bootstrap accepted23:27:20.211 and global-administrator projection Event100301 completed with1event.19 exact endpoint refusals precede bootstrap-authority-timeout.

## Tasks & Acceptance

- [ ] Preserve the configured Dapr URI in production typed-client registration; retain the absent-endpoint fallback.
- [ ] Correct the existing production wire test expectation and add meaningful source-only actual v2 guard coverage for matching localhost coordinates and different host/port rejection before transport. Prove real production registration, existing signing/tokens/body/proofs and guard execution; a guard-removal mutation must not pass the refusal cases.
- [ ] Run only affected owner tests and the narrowest required serial Debug/source build with SDK producer3.131.0, no nested initialization. Report exact command/count/logs and changed files; then freeze. Parent conducts combined review and actual hosted qualification.

Given a configured localhost Dapr endpoint equal to the enrolled v2 authority coordinates, native authority reads reach the real guarded transport without rewriting its host. Given a request with neighboring host/port coordinates, the unchanged shared guard refuses before transport. All existing native proof, stream binding, resource scope and cancellation behavior remains enforced.

## Upstream adoption

Before implementation edits, a fresh fetch found114badd2f4c7b796f9441996bc7d8b99abf4c2c6 already fixes this exact owner boundary, with actual v2 endpoint controls and explicit current/floor/source workflow gates. Subsequent6b034a2d3b70533024343e721c47ffdfd1830a10 preserves the required service-surface claim in its noncanonical native-authority regression. The owner checkout was fast-forwarded cleanly to6b034a2d; no duplicate implementation was authored. The source registration uses SDK RouteCatalogStateCoordinates.Capture to agree with effective configured runtime coordinates, while package mode preserves its configured endpoint/fallback. The shared guard is unchanged. Independent local focused verification and actual hosted source/main qualification are pending.
