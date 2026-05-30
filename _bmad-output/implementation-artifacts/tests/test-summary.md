# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for Story 5.1: the story intentionally adds an internal audit read-model seam only; public operator/API access belongs to Stories 5.2 and 5.7.

### E2E Tests
- [x] Not applicable for Story 5.1: no audit UI or public audit route exists yet. Existing Playwright audit specs remain `test.fixme` until the Story 5.7 surface exists.

### Server/Projection Tests
- [x] `tests/Hexalith.Projects.Server.Tests/InMemoryProjectAuditTimelineReadModelTests.cs` - Added server-facing audit read-model coverage for tenant/project filtering, limit ordering, dispatch-tenant mismatch dropping, and metadata-only folder audit rows.
- [x] `tests/Hexalith.Projects.Server.Tests/ServiceDefaultsEndpointTests.cs` - Added runtime DI coverage proving the audit read-model seam is replaced by the Dapr-backed implementation.
- [x] Existing `tests/Hexalith.Projects.Tests/Projections/ProjectAuditTimelineProjectionTests.cs` - Re-run for all mapped audit event types, deterministic IDs, tenant/project filtering, proposal-chain semantics, and unknown-event failure.
- [x] Existing `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` - Re-run for `ProjectAuditTimelineItem_SerializesMetadataOnly`.
- [x] Existing `tests/Hexalith.Projects.Integration.Tests/DaprProjectionStoreTests.cs` - Re-run for durable journal rebuild, duplicate handling, replay conflict, malformed evidence, missing journal, and tenant/project scoping.

## Coverage
- Audit projection event mapping: all current Project success events covered.
- Read-model seam: in-memory server seam plus Dapr runtime replacement covered.
- Tenant isolation: authoritative tenant filtering, project filtering, cross-tenant event mismatch, and durable tenant journal scoping covered.
- Metadata-only boundary: audit row serialization and folder metadata payload exclusion covered.
- Public API/UI: intentionally not generated for this story because no public audit route or audit UI is part of Story 5.1.

## Validation
- [x] `dotnet build Hexalith.Projects.slnx -m:1 /nr:false -warnaserror`
- [x] `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class "Hexalith.Projects.Server.Tests.InMemoryProjectAuditTimelineReadModelTests" -class "Hexalith.Projects.Server.Tests.ServiceDefaultsEndpointTests" -parallel none -noColor`
- [x] `dotnet tests/Hexalith.Projects.Tests/bin/Debug/net10.0/Hexalith.Projects.Tests.dll -class "Hexalith.Projects.Tests.Projections.ProjectAuditTimelineProjectionTests" -parallel none -noColor`
- [x] `dotnet tests/Hexalith.Projects.Tests/bin/Debug/net10.0/Hexalith.Projects.Tests.dll -method "Hexalith.Projects.Tests.Leakage.NoPayloadLeakageTests.ProjectAuditTimelineItem_SerializesMetadataOnly" -parallel none -noColor`
- [x] `dotnet tests/Hexalith.Projects.Integration.Tests/bin/Debug/net10.0/Hexalith.Projects.Integration.Tests.dll -class "Hexalith.Projects.Integration.Tests.DaprProjectionStoreTests" -parallel none -noColor`
- [x] `git diff --check`

## Checklist Notes
- API tests generated: not applicable; no public API surface for Story 5.1.
- E2E tests generated: not applicable; no UI route for Story 5.1.
- Standard framework APIs: yes, xUnit v3 and Shouldly patterns.
- Happy path coverage: yes, projection fold, in-memory read model, and durable journal rebuild.
- Critical error coverage: yes, dispatch-tenant mismatch, replay conflict, malformed evidence, missing journal, and unknown event types.
- Semantic UI locators: not applicable.
- No hardcoded waits/sleeps: yes.
- Independent tests: yes.
