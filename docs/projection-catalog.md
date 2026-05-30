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
  `ProjectFolderCreationPending`, `FileReferenceLinked`, `FileReferenceUnlinked`, `MemoryLinked`,
  `MemoryUnlinked`, and `ProjectResolutionConfirmed`. Setup, folder-set, pending-folder,
  reference-link/unlink, and resolution-confirmation events refresh `UpdatedAt`/sequence only;
  archive updates lifecycle to `Archived`. File/Memory reference events are also routed to the
  reference index projection; the list projection must still tolerate them because runtime rebuilds
  fold the shared tenant journal over every Project event.
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
  `MemoryUnlinked`, and `ProjectResolutionConfirmed`. `SetupMetadata` is the safe setup metadata reference carried by creation;
  `ProjectSetupUpdated` stores the latest bounded metadata-only setup preferences; `ProjectArchived`
  updates lifecycle to `Archived`; folder events record the single Project Folder reference (or pending
  intent when the Folders create capability is externally unavailable); file/memory link/unlink events
  maintain the bounded per-kind reference sets on the detail row alongside the disjoint reference index;
  `ProjectResolutionConfirmed` updates freshness/sequence only and stores no candidate scores, ranks,
  rejected ids, or trace payload.
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
  /`ProjectArchived`/`ProjectResolutionConfirmed` are observed but produce no reference-index rows.
  Unknown event types throw to keep the projection in sync with `ProjectStateApply`.
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
  Reference rows. Story 4.3 attachment resolution also uses the reverse-by-reference read model over
  this projection for `folder` and `file` inputs. The lanes never share a key prefix.

## `ProjectAuditTimelineProjection`

- **Type:** `Hexalith.Projects.Projections.ProjectAuditTimeline.ProjectAuditTimelineProjection`.
- **Owner:** Hexalith.Projects Workers host appends persisted Project events to the shared durable
  projection journal through `ProjectEventProjectionProcessor`; runtime Server reads audit rows through
  `DaprProjectAuditTimelineReadModel` / `IProjectAuditTimelineReadModel`.
- **Key:** audit rows are keyed by deterministic `AuditEventId`, derived from tenant, project, event
  type, projection/global-position sequence, idempotency key/fingerprint, operation type, and affected
  reference identity where applicable. It never uses `Guid.NewGuid()`, wall-clock generation time,
  random values, or Dapr state as the audit id source.
- **Source events:** every current Project success event: `ProjectCreated`, `ProjectSetupUpdated`,
  `ProjectArchived`, `ProjectFolderCreationPending`, `ProjectFolderSet`, `FileReferenceLinked`,
  `FileReferenceUnlinked`, `MemoryLinked`, `MemoryUnlinked`, and `ProjectResolutionConfirmed`.
  Unknown future `IProjectEvent` types throw during fold until they are explicitly mapped or
  intentionally documented.
- **Stored data:** metadata-only tenant id, project id, audit event id, operation type, event
  timestamp, actor principal id, correlation id, task id, idempotency key, optional affected reference
  kind/id, safe lifecycle/reference state deltas, optional reason code, confirmed conversation id, and
  safe source project id for resolution confirmation.
- **Tenant scoping:** envelope tenant and event tenant must match before any row is folded. Runtime
  reads are tenant-scoped and optionally project-scoped through the read-model seam. Caller-layer
  authorization/filtering composes above this seam in Stories 5.2 and 5.7.
- **Rebuild behavior:** `Rebuild(envelopes)` delegates to `Empty.Apply(envelopes)`, orders by
  `(Sequence, IdempotencyKey, IdempotencyFingerprint)`, deduplicates by deterministic audit id, and
  returns rows newest-first from `List(...)`. Runtime reads rebuild from the same
  `projects:projection-journal:{tenantId}` document used by list/detail/reference-index projections.
- **Runtime store:** Dapr `statestore`, key `projects:projection-journal:{tenantId}`. Audit reads use
  the same `EnsureReadable(...)` fail-closed path for missing journals, replay conflicts, malformed
  evidence, unsupported persisted event types, duplicate-message evidence, out-of-order messages, and
  EventStore global-position watermarks.
- **Freshness semantics:** row timestamps come from the event `OccurredAt`; rows are returned
  newest-first by EventStore global-position sequence (the authoritative total order, matching the
  list/detail/reference-index projections), with `OccurredAt` carried as the displayed timestamp. No
  response wall-clock is used to invent audit evidence.
- **Leakage boundary:** audit rows never store transcripts, file contents, raw prompts, memory
  payloads, unrestricted/local paths, raw tokens, full command bodies, candidate scores/ranks, rejected
  candidate ids, sibling denial details, or raw proposal bodies. `ProjectResolutionConfirmed` remains
  metadata-only, and `ProjectCreatedFromProposal` remains intentionally absent; proposal confirmation is
  visible only as the existing explicit command chain (`ProjectCreated` plus assignment/folder/file
  events and safe correlation/task/idempotency metadata).
- **Consumer guidance:** Stories 5.2 and 5.7 can render timestamp, actor/source metadata, operation,
  previous-to-new state where available, affected reference, correlation id, task id, audit event id,
  safe reason/state codes, and safe resolution identifiers through
  `GET /api/v1/projects/{projectId}/operator-diagnostics`. Story 5.2 deliberately omits the
  stored idempotency key from the public operator diagnostic DTO and bounds the audit window with
  `auditLimit` (default 25, max 100). Later Web/MCP/CLI adapters must reuse this DTO shape rather
  than composing raw projection rows directly.

## `ProjectOperatorDiagnosticShellProjection`

- **Type:** `Hexalith.Projects.Contracts.Ui.ProjectOperatorDiagnosticShellProjection`.
- **Owner:** Story 5.3 FrontComposer shell/navigation seed in Contracts. It is a generated UI/MCP/CLI
  descriptor input, not a persisted runtime projection and not the Story 5.4 inventory/detail view.
- **Source data:** thin metadata-only wrapper over
  `Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic` from Story 5.2. It reuses the shared
  `ProjectLifecycle` vocabulary and computes only shell-level warning count from reference states.
- **Tenant scoping:** inherited from the server-side operator diagnostic query; the seed never accepts
  or derives tenant authority from client input.
- **Stored data:** none. The generated FrontComposer artifacts are build output under
  `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`.
- **Freshness semantics:** carries `LastUpdated` from the diagnostic DTO and `FreshnessTrustState` from
  `ProjectOperatorFreshnessMetadata`. It does not invent wall-clock freshness.
- **Leakage boundary:** project id/name, lifecycle, warning count, last-updated timestamp, mode label,
  and freshness trust state only. No transcript, file content, memory payload, prompt, token, path,
  proposal body, command body, candidate score/rank, rejected candidate id, or sibling denial detail.
- **Consumer guidance:** Stories 5.4-5.11 consume the shell seed and shared rendering primitives while
  owning their specific view/action contracts.

## `ProjectInventoryRowProjection`

- **Type:** `Hexalith.Projects.Contracts.Ui.ProjectInventoryRowProjection`.
- **Owner:** Story 5.4 FrontComposer inventory descriptor/wrapper in Contracts. It is not a persisted
  runtime projection.
- **Source data:** thin metadata-only wrapper over the existing `ListProjects` response shape:
  project id, name, lifecycle, created/updated timestamps, and freshness. No backend field was added
  for warning/reason/reference filters in Story 5.4.
- **Tenant scoping:** inherited from `ProjectListProjection` and `ProjectQueryTenantFilter`. The UI
  displays a server-derived tenant scope label only; the wrapper has no tenant authority field and
  external list rows still do not carry `tenantId`.
- **Stored data:** none. It exists to carry Level-1 FrontComposer metadata for the Web inventory.
- **Freshness semantics:** carries per-row/list freshness trust state, stale flag, and projection
  watermark from the generated list DTO. It does not invent wall-clock freshness.
- **Leakage boundary:** project id/name, lifecycle, warning unavailability text, timestamps, tenant
  scope display label, and freshness evidence only. No transcript, file content, memory payload,
  prompt, token, path, proposal body, command body, candidate score/rank, rejected candidate id, or
  sibling denial detail.

## `ProjectDetailInspectorProjection`

- **Type:** `Hexalith.Projects.Contracts.Ui.ProjectDetailInspectorProjection`.
- **Owner:** Story 5.4 FrontComposer DetailRecord descriptor/wrapper in Contracts. It is not a
  persisted runtime projection.
- **Source data:** thin metadata-only wrapper over `ProjectOperatorDiagnostic`, itself composed from
  the generated `GetProject` detail DTO plus bounded operator diagnostic evidence when available.
- **Tenant scoping:** inherited from the generated query clients and server query authorization. Client
  route values select a project id only and never become tenant authority.
- **Stored data:** none. It exists to describe the read-only inspector field groups: context,
  references, audit, and freshness.
- **Freshness semantics:** carries detail freshness trust state from `ProjectOperatorFreshnessMetadata`
  and preserves generated-client eventual-consistency semantics.
- **Leakage boundary:** project id/name, lifecycle, context activation state, bounded reference/audit
  counts, update timestamp, and freshness trust state only. The rendered detail page may show bounded
  setup preferences and safe reference/audit summaries from approved DTOs, but never transcript, file
  content, memory payload, raw prompt, unrestricted path, token, proposal body, command body,
  candidate score/rank, rejected candidate id, or sibling denial detail.

## `ProjectReferenceHealthRowProjection`

- **Type:** `Hexalith.Projects.Contracts.Ui.ProjectReferenceHealthRowProjection`.
- **Owner:** Story 5.5 FrontComposer DetailRecord descriptor/wrapper in Contracts. It is not a
  persisted runtime projection and does not add a conversation lane to
  `ProjectReferenceIndexProjection`.
- **Source data:** metadata-only merge of existing `ProjectOperatorReferenceSummary` rows,
  `ProjectContextExplanation.Evaluations`, and `ListProjectConversations` ACL rows. Folder/file/memory
  rows continue to come from Project detail/operator diagnostics; conversation rows are derived from
  the Conversations-owned ACL/context-evaluation path.
- **Tenant scoping:** inherited from generated query clients and server authorization. The row carries
  `projectId` and opaque sibling reference ids only; it has no `tenantId` authority field.
- **Stored data:** none. It exists to describe the read-only reference health matrix field groups:
  diagnostics, freshness, and safe actions.
- **Freshness semantics:** carries last-checked timestamps from reference freshness or context
  evaluation observations, plus trust state/watermark where the existing DTO exposes them. It does not
  invent wall-clock freshness.
- **Leakage boundary:** reference kind, reference id, bounded-context owner, safe display label,
  shared inclusion/health state, optional shared reason code, optional inclusion check, optional
  closed diagnostic code, freshness evidence, and read-only safe action labels only. No transcript,
  file content, memory payload, raw prompt, unrestricted path, token, proposal body, command body,
  candidate score/rank, rejected candidate id, or sibling denial detail.

## `ConversationStartSetupProjection`

- **Type:** `Hexalith.Projects.Projections.ConversationStartSetup.ConversationStartSetupProjector`.
- **Design class:** **server-side projection over `ProjectContext.Setup`** — not an event-stream
  projection. The projector is a pure `public static class` whose single
  `Project(ProjectContext)` method delegates to
  `Hexalith.Projects.Contracts.Models.ConversationStartSetup.FromContext(...)` (Story 3.5).
- **Owner:** invoked synchronously by
  `Hexalith.Projects.Server.Queries.GetConversationStartSetupEndpoint`. No Workers subscription
  wiring, no `IProjectProjectionStore<>` registration, no durable store. The source-of-truth is the
  same `ProjectContext` produced by `ProjectContextInclusionPolicy` (Story 3.1).
- **Key:** none — the projector is stateless. The endpoint is keyed by canonical Project identity
  `{tenant}:projects:{projectId}` via the policy, like every other Epic 3 query surface.
- **Source events:** none directly. The projector consumes the assembled `ProjectContext.Setup`,
  which is itself derived from `ProjectDetailItem` (and therefore from
  `ProjectCreated`/`ProjectSetupUpdated`/`ProjectArchived` events via `ProjectDetailProjection`). A
  separate event-stream projection over the same events would duplicate state — the Story 3.5
  Design Decision explicitly avoids this.
- **Tenant scoping:** inherited from the policy invocation — the same outer collapses that produce
  safe-denial 404 on `ProjectContextAssemblyOutcome.Unauthorized` / `ProjectUnavailable` apply.
  Story 3.5's handler additionally collapses those outcomes to HTTP 404 defensively (stricter than
  Stories 3.2 / 3.3 / 3.4).
- **Stored data:** none. The projection output is a `ConversationStartSetup` wire DTO that contains
  only `ProjectId` / `Lifecycle` / `Goals` / `UserInstructions` / `PreferredSourceKinds` /
  `ExcludedSourceKinds` / `LinkedSourcePolicy` / `ObservedAt` / `Freshness`. No `TenantId` (not
  declared on the record — cleaner than the `[JsonIgnore]`-on-existing-field pattern Story 3.2
  used for `ProjectContext.TenantId`), no audit metadata, no per-reference inventory, no
  diagnostics field.
- **Rebuild behavior:** N/A — the projector is pure and has no stored state. Re-running the policy
  with the same inputs is the "rebuild" mechanism.
- **Runtime store:** N/A. The wire body is computed per request from `ProjectContext.Setup`.
- **Freshness semantics:** inherits `ObservedAt` and `Freshness` from the policy's outer outcome.
  Empty references-evidence is passed to the policy explicitly (no sibling-ACL calls on the
  conversation-start fast path — by handler-signature construction, asserted by the
  `GetConversationStartSetup_DoesNotCallSiblingAcls` Tier-2 regression guard).
- **Leakage boundary:** asserted by three Story-3.5-specific wire-shape invariants
  (`BodyDoesNotContainTenantId`, `BodyDoesNotContainAuditMetadata`,
  `BodyDoesNotContainReferenceInventory`) plus the `ConversationStartSetup_SerializesMetadataOnly`
  Contracts-tier `NoPayloadLeakage` extension. The reference-inventory check uses
  `JsonDocument.TryGetProperty(...)` rather than substring match to avoid a false-positive on
  `excludedSourceKinds` containing the substring `excluded`.
- **Consumer guidance:** Hexalith.Chatbot calls `GET
  /api/v1/projects/{projectId}/setup/conversation-start` (FR-20) before the first response of a
  conversation. The response is the bounded subset needed to start or resume — never the full
  `ProjectContext` (consumers needing the full context call Story 3.2 `GetProjectContext`); never
  the per-reference inventory (consumers needing inventory and diagnostics call Story 3.3
  `ExplainContextSelection`); never the on-the-fly refresh (consumers needing current sibling
  state call Story 3.4 `RefreshProjectContext`).
