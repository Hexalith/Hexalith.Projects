# EventStore owner handoff: platform-scoped workload source reads

Story 6.1's administrator source proof is blocked by a documented capability gap in the existing EventStore SDK. This is separate from Projects' public-fixture alignment and denial-observer repairs. No sibling implementation, package pin, transport workaround or validation relaxation is included in this handoff.

## Inspected source

EventStore commit: `2dca77cc2f22a9b85e4fcc3f8e18cbcaa532b135`, obtained directly with `git -C references/Hexalith.EventStore rev-parse HEAD`.

- `src/Hexalith.EventStore.Client/Gateway/IEventStoreGatewayClient.cs`, `ReadStreamAsync` remarks, explicitly state that the workload client does not support platform-scope reads: it sends no scope header and refuses a page whose Tenant differs from the request.
- `EventStoreGatewayClient.ReadWorkloadStreamAsync` requests a proof over the original request and calls `SendWorkloadAsync`, which sets the assertion/proof headers but never sets `X-Hexalith-Scope`. It accepts a response only when `result.Tenant == request.Tenant`, alongside exact Domain/AggregateId checks.
- `src/Hexalith.EventStore/Controllers/StreamsController.cs`, `ReadStreamCoreAsync`, requires one exact `X-Hexalith-Scope` for a platform workload source read and rejects a mixed `X-Hexalith-Tenant`. It then maps the authorized scope to retained storage Tenant `system`.
- `src/Hexalith.EventStore.Contracts/Identity/LegacyTenantStorageMap.cs` defines only `platform:tenants` + `tenants` and `platform:global-administrators` + `global-administrators` as these source-scope mappings. Arbitrary platform or customer identities must never inherit that mapping.

Consequently, a platform request through the current SDK lacks a required admission header. Even if a caller injected that header outside the SDK, the returned historical `system` page would fail the client's exact-Tenant comparison. A Projects-owned custom client or response remapping would evade the approved shared SDK boundary. The human stream endpoint's Tenants-only platform admission is not an alternative for global-administrator source proof.

The compiled API was inspected successfully without rebuilding or changing dependencies:

```bash
dnx dotnet-inspect -y -- member Hexalith.EventStore.Client.Gateway.EventStoreGatewayClient --library references/Hexalith.EventStore/src/Hexalith.EventStore.Client/bin/Debug/net10.0/Hexalith.EventStore.Client.dll -m ReadWorkloadStreamAsync -v:d -n 65
```

It exposes `ReadWorkloadStreamAsync(StreamReadRequest, CancellationToken)`; the behavioral gap above is established from the owning source and its explicit interface documentation, not inferred from the signature.

## Proposed owner work

1. In `EventStoreGatewayClient`, carry the explicit platform source scope on the existing workload transport for the two owner-approved mappings. Preserve the original request used by the bound proof and workload assertion. Retain the existing ordinary-Tenant behavior and reject conflicting or unsupported scope/domain combinations.
2. Validate the returned storage identity against the exact approved mapping while retaining strict Domain/AggregateId identity checks. Do not broadly accept `system` pages, alter historical event identities, or remove proof validation.
3. Extend `tests/Hexalith.EventStore.Client.Tests/Gateway/EventStoreGatewayWorkloadClientTests.cs` and relevant stream/controller controls with valid platform mappings, missing/mismatched/mixed scope headers, wrong storage/Domain/AggregateId, unchanged ordinary Tenant validation, and exact original proof/assertion binding.
4. Update the interface documentation only after the capability is implemented and verified. Record the actual tested owner commit and controls. Projects can then rerun its existing strict original-receipt, causation/correlation, source-history, persisted no-write and full distributed acceptance lane.

No SDK test was rerun by the parent while the implementation agent was building; source inspection and compiled API inspection are not runtime acceptance. Current Projects acceptance must remain in progress until its actual required evidence passes. Package-mode NU1109 is a separate dependency-selection blocker already retained in the prior packet.
