# Hexalith.Projects Projection Catalog

Authoritative human-readable catalog of Projects read-model projections (AR-8). Projection entries
describe ownership, source events, tenant scoping, rebuild behavior, freshness semantics, and leakage
boundaries.

## `TenantAccessProjection`

- **Type:** `Hexalith.Projects.Projections.TenantAccess.ProjectTenantAccessProjection`.
- **Owner:** Hexalith.Projects Workers host writes the projection through
  `ProjectsTenantEventHandler`; runtime Server/Workers use
  `DaprProjectTenantAccessProjectionStore` through `IProjectTenantAccessProjectionStore`.
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
- **Runtime store:** Dapr `statestore`, key `projects:tenant-access:{tenantId}`, with optimistic
  concurrency and detached snapshots. In-memory storage remains only for tests and explicit
  pre-runtime fakes.
- **Freshness semantics:** mutations require the strict `MutationFreshnessBudget`; diagnostic reads may
  use the bounded-stale `DiagnosticStalenessBudget`. Stale or unavailable evidence fails closed.
- **Query authorization:** `TenantAccessAuthorizer` gates membership before project ACL and EventStore
  validator layers. Query-side filters strip records whose tenant differs from the authoritative
  tenant.

## `ProjectListProjection`

- **Type:** `Hexalith.Projects.Projections.ProjectList.ProjectListProjection`.
- **Owner:** Hexalith.Projects Workers host folds persisted Project events through
  `ProjectEventProjectionProcessor`; runtime Server reads it through `DaprProjectListReadModel`.
- **Key:** canonical Project identity `{tenant}:projects:{projectId}` derived by `ProjectIdentity`.
- **Source events:** `ProjectCreated`, `ProjectSetupUpdated`, `ProjectArchived`, `ProjectFolderSet`,
  and `ProjectFolderCreationPending`. Setup, folder-set, and pending-folder events refresh
  `UpdatedAt`/sequence only; archive updates lifecycle to `Archived`. Reference link/unlink events
  (`FileReferenceLinked`/`FileReferenceUnlinked`/`MemoryLinked`/`MemoryUnlinked`) are routed to the
  reference index projection, not the list row.
- **Tenant scoping:** envelope tenant and event tenant must match before the row is folded. Query reads
  filter rows by the authenticated authoritative tenant before response construction.
- **Stored data:** metadata-only project id, display name, lifecycle state, sequence watermark, created
  timestamp, and updated timestamp. No Project Context, transcript, file contents, memory payload,
  prompt, token, secret, path, or sibling denial detail is stored.
- **Rebuild behavior:** `Rebuild(envelopes)` delegates exactly to `Empty.Apply(envelopes)` so rebuild
  and incremental projection share the same deterministic fold.
- **Runtime store:** Dapr `statestore`, key `projects:projection-journal:{tenantId}`, stores a
  metadata-bound event journal plus message-id/fingerprint evidence. The journal watermark is the
  EventStore global position so multiple project aggregate streams can share one tenant journal
  without treating each aggregate's sequence `1` event as out of order. Runtime reads rebuild through
  `ProjectListProjection.Rebuild(...)`; in-memory read models remain only for tests and explicit
  pre-runtime fakes.
- **Freshness semantics:** list responses derive `observedAt` and projection freshness/trust metadata
  from the projected row timestamps/sequences; no wall-clock guessing is used.
- **Leakage boundary:** list rows never include client-controlled `tenantId` in the external response.

## `ProjectDetailProjection`

- **Type:** `Hexalith.Projects.Projections.ProjectDetail.ProjectDetailProjection`.
- **Owner:** Hexalith.Projects Workers host folds persisted Project events through
  `ProjectEventProjectionProcessor`; runtime Server reads it through `DaprProjectDetailReadModel`.
- **Key:** canonical Project identity `{tenant}:projects:{projectId}` derived by `ProjectIdentity`.
- **Source events:** `ProjectCreated`, `ProjectSetupUpdated`, `ProjectArchived`, `ProjectFolderSet`,
  `ProjectFolderCreationPending`, `FileReferenceLinked`, `FileReferenceUnlinked`, `MemoryLinked`,
  and `MemoryUnlinked`. `SetupMetadata` is the safe setup metadata reference carried by creation;
  `ProjectSetupUpdated` stores the latest bounded metadata-only setup preferences; `ProjectArchived`
  updates lifecycle to `Archived`; folder events record the single Project Folder reference (or pending
  intent when the Folders create capability is externally unavailable); file/memory link/unlink events
  maintain the bounded per-kind reference sets on the detail row alongside the disjoint reference index.
- **Tenant scoping:** envelope tenant and event tenant must match before detail is folded. Open Project
  reads filter the detail by authoritative tenant before response construction.
- **Stored data:** metadata-only project id, name, description, setup metadata reference, bounded setup
  preferences, lifecycle state, created/updated timestamps, and sequence watermark.
- **Rebuild behavior:** `Rebuild(envelopes)` delegates exactly to `Empty.Apply(envelopes)` and keeps the
  projection pure, deterministic, tenant-guarded, and throw-on-unknown-event.
- **Runtime store:** Dapr `statestore`, key `projects:projection-journal:{tenantId}`, shared with the
  list projection journal. The stored projection sequence/watermark is EventStore global position, not
  per-aggregate sequence. Runtime reads rebuild through `ProjectDetailProjection.Rebuild(...)` and fail
  closed when the journal is absent, malformed, or in replay conflict.
- **Freshness semantics:** Open Project responses derive freshness/trust metadata from projection state
  (`UpdatedAt` and `Sequence`), not from the response wall clock.
- **Leakage boundary:** Open Project returns metadata/setup/reference summaries only and blocks context
  activation explicitly when lifecycle or availability prevents active use.

## `ProjectReferenceIndexProjection`

- **Type:** `Hexalith.Projects.Projections.ProjectReferenceIndex.ProjectReferenceIndexProjection`.
- **Owner:** Hexalith.Projects Workers host folds persisted Project events through
  `ProjectEventProjectionProcessor`; runtime callers consume it as a tenant-scoped read model.
- **Key:** per-reference key `{tenant}:projects:{projectId}:references:{kind}:{referenceId}` where
  `{kind}` is one of `folder`, `file`, or `memory`. The disjoint per-kind prefix is load-bearing:
  replacing the single Project Folder only ever touches `folder`-kind rows; linking/unlinking a File
  Reference or Memory Reference only ever touches a single row of its own kind. File unlink can
  never remove the Project Folder row and folder replacement can never remove file or memory rows.
- **Source events:** `ProjectFolderSet`, `ProjectFolderCreationPending`, `FileReferenceLinked`,
  `FileReferenceUnlinked`, `MemoryLinked`, and `MemoryUnlinked`. `ProjectCreated`/`ProjectSetupUpdated`
  /`ProjectArchived` are observed but produce no reference-index rows. Unknown event types throw to
  keep the projection in sync with `ProjectStateApply`.
- **Tenant scoping:** envelope tenant and event tenant must match before any row is folded. Query
  reads filter by the authoritative tenant before response construction.
- **Stored data:** metadata-only tenant id, project id, reference kind, reference id, inclusion state
  (`Included` for set/link, `Pending` for pending folder), safe display name, optional reason code
  (carried on `Pending` rows from `ProjectFolderCreationPending.ReasonCode`), occurred-at timestamp,
  and the envelope sequence. No file contents, memory payload, transcript, prompt, secret, token,
  unrestricted path, embedding vector, or sibling denial detail is stored.
- **Rebuild behavior:** `Apply(envelopes)` orders by `(Sequence, IdempotencyKey, IdempotencyFingerprint)`
  and folds deterministically. `Empty.Apply(envelopes)` is the rebuild path. Pending folder rows
  are only written when no `Included` Project Folder is already present so a degraded create cannot
  shadow an established folder.
- **Runtime store:** the reference index uses the shared `projects:projection-journal:{tenantId}`
  durable journal (EventStore global position watermark) — there is no separate per-projection store
  in production. In-memory storage remains only for tests and explicit pre-runtime fakes.
- **Freshness semantics:** consumers derive freshness/trust metadata from the projected
  `OccurredAt`/`Sequence` carried per row, not from the response wall clock. Pending folder rows
  surface their reason code so the view can show "folder creation queued/flagged" rather than treating
  the absent folder as available.
- **Leakage boundary:** reference rows expose only the safe identifier and the safe display name from
  the corresponding folder/file/memory metadata; no upstream content, path, or denial detail is
  retained. The `NoPayloadLeakage` harness asserts this per reference kind.
- **Consumer guidance:** Epic 3 context assembly reads this projection lane-aware: a single Project
  Folder row (Included or Pending), zero-to-many File Reference rows, and zero-to-many Memory
  Reference rows. The lanes never share a key prefix.
