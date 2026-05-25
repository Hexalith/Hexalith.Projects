# Hexalith.Projects Projection Catalog

Authoritative human-readable catalog of Projects read-model projections (AR-8). Projection entries
describe ownership, source events, tenant scoping, rebuild behavior, freshness semantics, and leakage
boundaries.

## `TenantAccessProjection`

- **Type:** `Hexalith.Projects.Projections.TenantAccess.ProjectTenantAccessProjection`.
- **Owner:** Hexalith.Projects Workers host writes the projection through
  `ProjectsTenantEventHandler`; Server reads it through `IProjectTenantAccessProjectionStore`.
- **Key:** authoritative tenant id (`{tenantId}`), surfaced as `ProjectionWatermark = {tenant}:{sequence}`.
- **Source events:** consumed Tenants lifecycle, membership, and configuration events:
  `TenantCreated`, `TenantUpdated`, `TenantEnabled`, `TenantDisabled`, `UserAddedToTenant`,
  `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and
  `TenantConfigurationRemoved`.
- **Tenant scoping:** only the authoritative claim-derived tenant is read. Client-controlled tenant
  values are comparison evidence only; payload, header, and query values never become authority.
- **Stored data:** metadata-only lifecycle, membership role, project-scoped configuration keys,
  processed message evidence, replay-conflict/malformed flags, and watermarks. No raw payload,
  token, secret, transcript, file content, or path is stored.
- **Rebuild behavior:** replay Tenants events in sequence. A tenant is not usable until its projection
  is present, enabled, conflict-free, non-malformed, and fresh enough for the operation.
- **Freshness semantics:** mutations require the strict `MutationFreshnessBudget`; diagnostic reads may
  use the bounded-stale `DiagnosticStalenessBudget`. Stale or unavailable evidence fails closed.
- **Query authorization:** `TenantAccessAuthorizer` gates membership before project ACL and EventStore
  validator layers. Query-side filters strip records whose tenant differs from the authoritative
  tenant.

## `ProjectListProjection`

- **Type:** `Hexalith.Projects.Projections.ProjectList.ProjectListProjection`.
- **Owner:** Hexalith.Projects Workers host will fold persisted Project events; Server reads it through
  `IProjectListReadModel`. The current Story 1.7 in-memory implementation is
  `InMemoryProjectListReadModel`.
- **Key:** canonical Project identity `{tenant}:projects:{projectId}` derived by `ProjectIdentity`.
- **Source events:** `ProjectCreated`, `ProjectSetupUpdated`, and `ProjectArchived`. Setup updates refresh
  `UpdatedAt`/sequence only; archive updates lifecycle to `Archived`.
- **Tenant scoping:** envelope tenant and event tenant must match before the row is folded. Query reads
  filter rows by the authenticated authoritative tenant before response construction.
- **Stored data:** metadata-only project id, display name, lifecycle state, sequence watermark, created
  timestamp, and updated timestamp. No Project Context, transcript, file contents, memory payload,
  prompt, token, secret, path, or sibling denial detail is stored.
- **Rebuild behavior:** `Rebuild(envelopes)` delegates exactly to `Empty.Apply(envelopes)` so rebuild
  and incremental projection share the same deterministic fold.
- **Freshness semantics:** list responses derive `observedAt` and projection freshness/trust metadata
  from the projected row timestamps/sequences; no wall-clock guessing is used.
- **Leakage boundary:** list rows never include client-controlled `tenantId` in the external response.

## `ProjectDetailProjection`

- **Type:** `Hexalith.Projects.Projections.ProjectDetail.ProjectDetailProjection`.
- **Owner:** Hexalith.Projects Workers host will fold persisted Project events; Server reads it through
  `IProjectDetailReadModel`. The current Story 1.7 in-memory implementation is
  `InMemoryProjectDetailReadModel`.
- **Key:** canonical Project identity `{tenant}:projects:{projectId}` derived by `ProjectIdentity`.
- **Source events:** `ProjectCreated`, `ProjectSetupUpdated`, and `ProjectArchived`. `SetupMetadata` is the
  safe setup metadata reference carried by creation; `ProjectSetupUpdated` stores the latest bounded
  metadata-only setup preferences; `ProjectArchived` updates lifecycle to `Archived`.
- **Tenant scoping:** envelope tenant and event tenant must match before detail is folded. Open Project
  reads filter the detail by authoritative tenant before response construction.
- **Stored data:** metadata-only project id, name, description, setup metadata reference, bounded setup
  preferences, lifecycle state, created/updated timestamps, and sequence watermark.
- **Rebuild behavior:** `Rebuild(envelopes)` delegates exactly to `Empty.Apply(envelopes)` and keeps the
  projection pure, deterministic, tenant-guarded, and throw-on-unknown-event.
- **Freshness semantics:** Open Project responses derive freshness/trust metadata from projection state
  (`UpdatedAt` and `Sequence`), not from the response wall clock.
- **Leakage boundary:** Open Project returns metadata/setup/reference summaries only and blocks context
  activation explicitly when lifecycle or availability prevents active use.
