# Next owner decision: authenticated platform delivery reconciliation

The SDK/source-helper repair reaches the next existing fixture operation and receives HTTP 403 before any Project seed or read. The user approved this bounded maintenance proposal on 2026-11-05. The active spec now records the exact route, separate namespace and permission; implementation follows that approved extension.

## Confirmed boundary

`SupportedProjectReadDistributedFixture.ActivateFreshDeliveryWriterProtocolAsync` calls:

```text
POST /api/v1/admin/projections/system/global-administrators/delivery-reconciliation/global-administrators/global-administrators
```

The real public credential has no reserved storage Tenant grant. In the EventStore owner repository, `src/Hexalith.EventStore/Authorization/AdminTenantAuthorizationFilter.cs` rejects the route's reserved `system` Tenant. `src/Hexalith.EventStore/Controllers/AdminProjectionRebuildController.cs`, `ReconcileDeliveryFromEventStoreAsync`, subsequently calls `CreateScope`, which independently requires a canonical public Tenant. A scope header alone cannot authorize this route: the existing filter rejects mixing a platform header with a Tenant route argument. `AdminPlatformScopeCatalog` currently declares global list reads, not this maintenance operation.

The source-only helper must remain a read. Public storage grants, direct checkpoint rewriting, calling the reconciler around its authorization boundary, and skipping the cutover assertions are unacceptable substitutes.

## Proposed bounded owner work

1. Define an explicit platform maintenance admission for the exact global-administrator bootstrap checkpoint, with an attributable authenticated global administrator and a narrow maintenance permission. The namespace and permission are an explicit contract decision; the existing read-only `admin:read` declaration must not silently gain write authority.
2. Add an owner-supported maintenance entry point without a public `system` Tenant argument. Admit only the declared platform scope and exact global-administrator domain, aggregate and projection. Reject mixed Tenant/scope headers, missing/duplicate permissions, non-administrators and alternate targets. Reuse the existing reconciliation service after authorization; retain original source-proof, preserved-sequence, quiescence, native backup, audit and no-domain-event behavior.
3. Keep ordinary Tenant maintenance behavior and all existing storage/history identities unchanged. Relevant owner files are `AdminProjectionRebuildController.cs`, its scope/source helpers, `AdminTenantAuthorizationFilter.cs`, and an explicit maintenance admission declaration under `Hexalith.EventStore.Contracts/Routing`. Avoid widening the existing read-only catalog or applying a global reserved-Tenant exception.
4. Extend `tests/Hexalith.EventStore.Server.Tests/Controllers/AdminProjectionRebuildControllerTests.cs` and `tests/Hexalith.EventStore.Server.Tests/Authorization/AdminTenantAuthorizationFilterTests.cs` for positive platform admission, exact permission/target binding, reserved/mixed/foreign refusal, unchanged ordinary Tenant admission and actual preserved persisted checkpoint/source state.
5. In Projects, update only the fixture's maintenance request and finite Administrator permission inventory, with evidence controls. Retain its original authenticated bootstrap receipt/source checks, backup verification, reconciliation result, writer-protocol activation and persisted no-write/read assertions. Then rerun a fresh three-provider qualification with regressions and exact owned cleanup.

This changes production maintenance authorization and introduces a supported platform maintenance entry point. The user explicitly approved this sole exception to the earlier new-public-endpoint prohibition; all other restrictions remain effective. Dependency pins, production deployment, Git mutations and other stories remain outside the proposal.
