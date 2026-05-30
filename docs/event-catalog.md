# Hexalith.Projects Event Catalog

Authoritative human-readable catalog of the domain events Hexalith.Projects produces (AR-6). Every new
event type MUST be added here with its purpose, fields, sensitivity class, and consumers. The
machine-usable payload-classification source of truth is
`src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs`; the FS-2 `NoPayloadLeakage` harness
asserts every serialized event against its `ForbiddenContent` denylist.

All Projects events are **metadata-only**: they never carry conversation transcript text, file
contents, memory bodies, raw prompts, secrets, raw tokens, full command bodies, or unrestricted/local
file paths. Schema evolution is additive and serialization-tolerant — never introduce a `V2` event
type; new fields must be optional and backward-compatibly deserializable (NFR-6, FS-5).

> **See also (Epic 3 Story 3.1):** the per-`(evidence-state × operation)` fail-closed decision
> matrix for `ProjectContext` assembly lives at
> [`docs/context-assembly-decision-matrix.md`](context-assembly-decision-matrix.md); the
> producer-of-last-resort tracker for shared-vocabulary outcomes is the
> [`## Shared vocabulary — producer of last resort`](#shared-vocabulary--producer-of-last-resort)
> section at the foot of this catalog.

## Success events

### `ProjectCreated`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectCreated` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records that a tenant-scoped Project workspace was durably created as `Active` (FR-1).
- **Emitted by:** `ProjectAggregate.Handle(CreateProject)` (Story 1.4).
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId` — managed tenant (envelope tenant).
  - `ProjectId` — opaque project identifier (ULID-shaped).
  - `Name` — project name (metadata only).
  - `Description` — optional safe, metadata-only description.
  - `SetupMetadata` — optional safe, reference-only setup-metadata reference (never a raw body/path/secret).
  - `Lifecycle` — shared `ProjectLifecycle` (always `Active` at creation).
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectListProjection`, `ProjectDetailProjection`.

### `ProjectSetupUpdated`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectSetupUpdated` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records a durable update to a Project's bounded, metadata-only setup preferences (Story 1.8).
- **Emitted by:** `ProjectAggregate.Handle(UpdateProjectSetup)`.
- **Sensitivity class:** metadata-only (`SetupPreference`).
- **Fields:**
  - `TenantId` — managed tenant (envelope tenant).
  - `ProjectId` — opaque project identifier.
  - `Setup` — bounded Projects-owned setup preferences: goals, user instructions, preferred/excluded source kinds, and conversation-start defaults. It never contains transcript text, file contents, memory bodies, raw prompts, tokens, secrets, command bodies, or paths.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectDetailProjection` stores the latest setup; `ProjectListProjection` updates freshness/sequence only.

### `ProjectArchived`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectArchived` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records that a Project moved to `Archived` lifecycle (Story 1.8).
- **Emitted by:** `ProjectAggregate.Handle(ArchiveProject)`.
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId` — managed tenant (envelope tenant).
  - `ProjectId` — opaque project identifier.
  - `Lifecycle` — shared `ProjectLifecycle` (`Archived`).
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectListProjection`, `ProjectDetailProjection`.

### `ProjectFolderCreationPending`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectFolderCreationPending` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records the degraded, retryable auto-create intent for a Project Folder when the Folders external create route is not mapped yet (Story 2.4).
- **Emitted by:** `ProjectAggregate.Handle(CreateProject)` after `ProjectCreated` in the accepted degraded path.
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId`, `ProjectId` — canonical Projects identity.
  - `DisplayNameIntent` — safe display-name intent derived from the Project name.
  - `ReasonCode` — stable metadata-only reason such as `folder_create_external_unavailable`.
  - `Retryable` — whether reconciliation may retry when Folders create becomes available.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, derived `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectDetailProjection`, `ProjectListProjection` freshness updates, `ProjectReferenceIndexProjection`.

### `ProjectFolderSet`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectFolderSet` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records the single authorized Project Folder reference after Projects server-side ACL validation through Hexalith.Folders.
- **Emitted by:** `ProjectAggregate.Handle(SetProjectFolder)` after the server has validated Folders lifecycle/effective-permissions evidence.
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId`, `ProjectId` — canonical Projects identity.
  - `FolderId` — Folders-owned sibling reference string.
  - `FolderMetadata` — safe display metadata only; never folder contents, paths, repository internals, tenant authority, or raw upstream ACL details.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectDetailProjection`, `ProjectListProjection` freshness updates, `ProjectReferenceIndexProjection`.

### `FileReferenceLinked`

- **Type:** `Hexalith.Projects.Contracts.Events.FileReferenceLinked` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records an optional File Reference link on an active Project after Projects server-side ACL validation of the file through the Hexalith.Folders metadata-only context route (FR-9, FR-11). File references are a bounded optional set; linking never clears, replaces, satisfies, or auto-creates the single Project Folder.
- **Emitted by:** `ProjectAggregate.Handle(LinkFileReference)` after the server validated Folders file metadata evidence (`GetFolderFileMetadata`, never file content bytes).
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId`, `ProjectId` — canonical Projects identity.
  - `FileReferenceId` — Projects-owned opaque, stable file-reference string.
  - `FolderId` — owning Folders-owned folder reference string.
  - `FileMetadata` — safe display metadata only; never file contents, byte ranges, raw/workspace paths, diffs, provider payloads, repository internals, tenant authority, or raw upstream ACL details.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectDetailProjection`, `ProjectReferenceIndexProjection` (`file`-kind rows).

### `FileReferenceUnlinked`

- **Type:** `Hexalith.Projects.Contracts.Events.FileReferenceUnlinked` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records removal of the Project-to-file association only (FR-9, FR-11). It never deletes, removes, archives, reads, mutates, or otherwise changes the underlying file in Hexalith.Folders, and never removes the single Project Folder reference.
- **Emitted by:** `ProjectAggregate.Handle(UnlinkFileReference)` when the targeted reference exists; unlinking a missing reference is a safe idempotent no-op that emits no event.
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId`, `ProjectId` — canonical Projects identity.
  - `FileReferenceId` — Projects-owned opaque file-reference string that was unlinked.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectDetailProjection`, `ProjectReferenceIndexProjection` (removes only the targeted `file`-kind row).

### `MemoryLinked`

- **Type:** `Hexalith.Projects.Contracts.Events.MemoryLinked` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records a Memory Reference link on an active Project after Projects server-side ACL validation of the Hexalith.Memories `Case` through the stable `MemoriesClient.GetCaseAsync` read route (FR-10, FR-11). Memory references are a bounded optional set; linking never clears, replaces, satisfies, or auto-creates the single Project Folder, and never touches file references.
- **Emitted by:** `ProjectAggregate.Handle(LinkMemory)` after the server validated Memories case evidence (`GetCaseAsync`, never MemoryUnit content / embeddings / search / traversal payloads).
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId`, `ProjectId` — canonical Projects identity.
  - `MemoryReferenceId` — opaque Memories case identifier (ULID-shaped sibling identifier).
  - `MemoryMetadata` — safe display metadata only; never `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, `SourceType`, `IngestedBy`, `Metadata`, `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`, `Classification`, raw `ErrorResponse.Message`/`Suggestion`, raw `MemoriesRemoteException.Message`, tokens, paths, or Memories-internal tenant identifier as payload.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectDetailProjection`, `ProjectReferenceIndexProjection` (`memory`-kind rows on a disjoint lane); Epic 3 Project Context assembly.

### `MemoryUnlinked`

- **Type:** `Hexalith.Projects.Contracts.Events.MemoryUnlinked` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records removal of the Project-to-memory association only (FR-10, FR-11). It never calls Hexalith.Memories, never deletes, archives, reads, or mutates the underlying `Case` or any `MemoryUnit`, and never removes the single Project Folder or any File Reference.
- **Emitted by:** `ProjectAggregate.Handle(UnlinkMemory)` when the targeted reference exists; unlinking a missing reference is a safe idempotent no-op that emits no event.
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId`, `ProjectId` — canonical Projects identity.
  - `MemoryReferenceId` — opaque Memories case identifier that was unlinked.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Consumers:** `ProjectDetailProjection`, `ProjectReferenceIndexProjection` (removes only the targeted `memory`-kind row); Epic 3 Project Context assembly.

### `ProjectResolutionConfirmed`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectResolutionConfirmed` (`IProjectEvent` → `IEventPayload`)
- **Purpose:** Records the user-confirmed Project choice from a `MultipleCandidates` resolution result after the Conversation assignment has been accepted through Hexalith.Conversations.
- **Emitted by:** `ProjectAggregate.Handle(ConfirmProjectResolution)` after the server validates confirmation evidence and assignment orchestration succeeds.
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId`, `ProjectId` — canonical confirmed target Project identity.
  - `ConversationId` — confirmed Conversation identifier.
  - `SourceProjectId` — optional expected current source Project identifier.
  - `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint` — envelope/idempotency metadata.
  - `OccurredAt` — wall-clock instant (pipeline `TimeProvider`).
- **Forbidden payload:** candidate ids, rejected candidates, scores, ranks, raw resolution results, transcripts, file contents, prompts, memory bodies, paths, tokens, and full request bodies.
- **Consumers:** `ProjectStateApply` records idempotency only; `ProjectListProjection` and `ProjectDetailProjection` update freshness/sequence only; `ProjectReferenceIndexProjection` intentionally ignores it.

## Rejection events

### `ProjectCreationRejected`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectCreationRejected` (`IRejectionEvent`)
- **Purpose:** Records a refused project-creation attempt (fail-closed on missing/unauthorized tenant,
  validation failure, duplicate create, or idempotency conflict) — a domain rejection, never an
  infrastructure exception (AR-3, FS-4).
- **Emitted by:** `ProjectAggregate.Handle(CreateProject)` rejection paths (Story 1.4), surfaced through
  the Server `/process` callback as a `DomainResult.Rejection`.
- **Sensitivity class:** metadata-only.
- **Fields:**
  - `TenantId` — managed tenant the rejected creation targeted.
  - `Reason` — canonical shared `ReferenceState` reason code (e.g. `Unauthorized`, `TenantMismatch`, `Conflict`, `InvalidReference`).
  - `RejectedField` — optional NAME of the offending field (never its value).
  - `CorrelationId` — optional correlation identifier.
  - `ProjectId` — optional project identifier (added additively in Story 1.4 for create-path correlation).
- **Consumers:** the Server denial mapper (RFC 9457 ProblemDetails + safe-denial 404); audit/log scopes (metadata only).

### `ProjectSetupUpdateRejected`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectSetupUpdateRejected` (`IRejectionEvent`)
- **Purpose:** Records a refused setup-update command (validation failure, missing project, archived project, authorization failure, or idempotency conflict).
- **Emitted by:** `ProjectAggregate.Handle(UpdateProjectSetup)` and `/process` fail-closed payload paths.
- **Sensitivity class:** metadata-only.
- **Fields:** `ProjectId`, `TenantId`, canonical `Reason`, optional `RejectedField` name, optional `CorrelationId`.
- **Consumers:** Server denial/problem mapping; audit/log scopes (metadata only).

### `ProjectArchiveRejected`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectArchiveRejected` (`IRejectionEvent`)
- **Purpose:** Records a refused archive command (validation failure, missing project, already archived project, authorization failure, or idempotency conflict).
- **Emitted by:** `ProjectAggregate.Handle(ArchiveProject)` and `/process` fail-closed payload paths.
- **Sensitivity class:** metadata-only.
- **Fields:** `ProjectId`, `TenantId`, canonical `Reason`, optional `RejectedField` name, optional `CorrelationId`.
- **Consumers:** Server denial/problem mapping; audit/log scopes (metadata only).

### `ProjectReferenceLinkRejected`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectReferenceLinkRejected` (`IRejectionEvent`)
- **Purpose:** Records a refused sibling reference link/set attempt, including Project Folder set (`folder`), optional File Reference link (`file`), and Memory Reference link (`memory`) rejection paths (validation failure, replacement not confirmed, conflicting/over-limit reference, missing/archived project, tenant mismatch, Folders/Memories ACL denial/archived/staleness/unavailability, authorization failure, or idempotency conflict).
- **Emitted by:** `ProjectAggregate.Handle(SetProjectFolder)`, `ProjectAggregate.Handle(LinkFileReference)`, and `ProjectAggregate.Handle(LinkMemory)` rejection paths, the server-side Folders/Memories ACL fail-closed mappings, and `/process` fail-closed payload paths.
- **Sensitivity class:** metadata-only.
- **Fields:** `ProjectId`, `TenantId`, `ReferenceKind` (`folder`, `file`, or `memory`), `ReferenceId` sibling identifier when safe (malformed identifiers are dropped to `unknown`, never echoed raw), canonical `Reason`, optional `RejectedField` name, optional `CorrelationId`.
- **Consumers:** Server denial/problem mapping; audit/log scopes (metadata only).

### `ProjectReferenceUnlinkRejected`

- **Type:** `Hexalith.Projects.Contracts.Events.ProjectReferenceUnlinkRejected` (`IRejectionEvent`)
- **Purpose:** Records a refused sibling reference unlink attempt, including optional File Reference unlink (`file`) and Memory Reference unlink (`memory`) rejection paths (validation failure, missing/archived project, tenant mismatch, authorization failure, or idempotency conflict). Unlinking a reference that is not present is a safe idempotent no-op, not a rejection.
- **Emitted by:** `ProjectAggregate.Handle(UnlinkFileReference)`, `ProjectAggregate.Handle(UnlinkMemory)` rejection paths and `/process` fail-closed payload paths.
- **Sensitivity class:** metadata-only.
- **Fields:** `ProjectId`, `TenantId`, `ReferenceKind` (`file` or `memory`), `ReferenceId` sibling identifier when safe (malformed dropped to `unknown`), canonical `Reason`, optional `RejectedField` name, optional `CorrelationId`.
- **Consumers:** Server denial/problem mapping; audit/log scopes (metadata only).

## Consumed external events

### Hexalith.Tenants events for `TenantAccessProjection`

- **Source:** `Hexalith.Tenants.Contracts.Events`.
- **Consumed by:** `Hexalith.Projects.Workers.Tenants.TenantEventHandlers.ProjectsTenantEventHandler`.
- **Purpose:** Maintain the local metadata-only `TenantAccessProjection` used by layered, fail-closed
  Projects authorization.
- **Events:** `TenantCreated`, `TenantUpdated`, `TenantEnabled`, `TenantDisabled`,
  `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and
  `TenantConfigurationRemoved`.
- **Sensitivity class:** consumed metadata only. Projects stores lifecycle, membership, project-scoped
  configuration keys, message fingerprints, and watermarks; it does not store raw Tenants payloads,
  secrets, tokens, or caller-controlled authority.
- **Ownership:** consumed only. Hexalith.Projects does not produce Tenants events.

## Shared vocabulary — producer of last resort

Per the Epic 2 retrospective action item *"Track unproduced shared-vocabulary outcomes deliberately"*,
this section enumerates every member of the shared-vocabulary enums consumed by the AR-9 Project
Context inclusion policy (Story 3.1) and names the producer that actually emits each value today.
Members marked `unproduced — taxonomy-only` are reserved for symmetry with the upstream surface but
are not yet emitted anywhere; the linked future story is where the producer lands.

### `ReferenceState` (`src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs`)

| Value              | Current producer                                                                         | Notes                                                                                  |
| ------------------ | ---------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `Pending`          | `ProjectFolderCreationPending` (Story 2.4 degraded folder path)                          | Surfaced by the inclusion policy as `ReferenceFreshness` exclusion.                    |
| `Included`         | Every Story 2.x ACL (Folders, Memories, Conversations) on success                        | Default success state for assembled references.                                        |
| `Excluded`         | Story 3.1 inclusion policy (conversation `Redacted` upstream signal)                     | Always carries `Diagnostic = "referenceRedacted"`.                                     |
| `Unauthorized`     | Story 2.x ACLs (Folders, Memories, Conversations) on per-reference auth failure          | Plus Memories `TenantMismatch` collapsed at the assembly boundary (Story 2.6 ADR).     |
| `Unavailable`      | Story 2.x ACLs (Folders, Memories, Conversations) when upstream is unreachable           | Fail-closed-clean exclusion.                                                           |
| `Stale`            | Story 2.1 Conversation read ACL (`MixedGeneration` / `Stale` trust signals); Story 3.4 refresh mappers for Folder/Memory ACL rechecks; Epic 4 resolution mappers preserve stale evidence as non-qualifying exclusions. | Fails closed for inclusion/resolution; never contributes a positive match. |
| `Archived`         | Story 2.x ACLs (Folders archived; Memories `Case.Status` = `Closed`/`Deleting`)          | Plus Story 1.8 archived project lifecycle.                                             |
| `Ambiguous`        | Resolution engine output as `ResolutionResult.MultipleCandidates`; not currently emitted as a `ReferenceState` row. | Reserved for future reference-health views that need to label ambiguous reference evidence directly. |
| `TenantMismatch`   | Story 1.6 `TenantAccessAuthorizer` + Story 2.6 Memories ADR (boundary collapse)          | **`unproduced` as a surfaced `ReferenceState`** — the assembly always collapses to `Unauthorized` with `Diagnostic = "tenantMismatch"`. Kept for symmetry with `TenantAccessOutcome.TenantMismatch`. |
| `Conflict`         | `ProjectAggregate` (`MemoryReferenceConflict` / `FileReferenceConflict` rejection paths) | Surfaced by the inclusion policy as `ReferenceLifecycle` exclusion.                    |
| `InvalidReference` | Story 2.x ACLs (malformed identifier) + Story 3.1 policy (non-allowlisted reference kind) | Diagnostic differs (`referenceInvalidIdentifier` vs `referenceKindNotAllowlisted`).    |

### `ProjectLifecycle` (`src/Hexalith.Projects.Contracts/Ui/ProjectLifecycle.cs`)

| Value      | Current producer                                                       | Notes                          |
| ---------- | ---------------------------------------------------------------------- | ------------------------------ |
| `Active`   | `ProjectAggregate.Handle(CreateProject)` (Story 1.4)                   | Default on success.            |
| `Archived` | `ProjectAggregate.Handle(ArchiveProject)` (Story 1.8)                  | Surfaced unchanged in context. |

### `ProjectReasonCode` (`src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs`)

| Value                     | Current producer                                                                    | Notes                                                                            |
| ------------------------- | ----------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `ConversationLinked`      | Story 3.1 inclusion policy (conversation included path); Story 4.2 conversation-resolution mapper. | Resolution weight: 50. |
| `ProjectFolderMatched`    | Story 3.1 inclusion policy (Project Folder included path); Story 4.3 attachment-resolution mapper. | Resolution weight: 45. |
| `FileReferenceMatched`    | Story 3.1 inclusion policy (file reference included path); Story 4.3 attachment-resolution mapper. | Resolution weight: 35. |
| `MemoryMatched`           | Story 3.1 inclusion policy (memory reference included path); Story 4.1 engine supports this signal for future memory-backed resolution evidence. | Resolution weight: 30. |
| `MetadataMatched`         | Story 4.2 conversation-resolution mapper (safe-label/project-name metadata equality). | Resolution weight: 20. |

### `ProjectContextInclusionCheck` (`src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionCheck.cs`, Story 3.1)

| Value                      | Current producer                                                              |
| -------------------------- | ----------------------------------------------------------------------------- |
| `TenantAuthority`          | Story 3.1 inclusion policy (assembly-level collapse).                         |
| `ProjectVisibility`        | Story 3.1 inclusion policy (assembly-level collapse).                         |
| `ProjectLifecycle`         | Story 3.1 inclusion policy (per-reference exclusion on archived project).     |
| `ReferenceAuthorization`   | Story 3.1 inclusion policy (per-reference auth failure).                      |
| `ReferenceLifecycle`       | Story 3.1 inclusion policy (per-reference archived/ambiguous/conflict).       |
| `ReferenceFreshness`       | Story 3.1 inclusion policy (per-reference stale/unavailable/pending/redacted). |
| `ReferenceKindAllowlist`   | Story 3.1 inclusion policy (non-allowlisted kind or malformed identifier).    |

### `ProjectContextAssemblyOutcome` (`src/Hexalith.Projects.Contracts/Ui/ProjectContextAssemblyOutcome.cs`, Story 3.1)

| Value                | Current producer                                                                    |
| -------------------- | ----------------------------------------------------------------------------------- |
| `Assembled`          | Story 3.1 inclusion policy (assembly succeeded; includes Archived-project case).    |
| `ProjectUnavailable` | Story 3.1 inclusion policy (safe-denial 404 contract; cross-tenant or null detail). |
| `Unauthorized`       | Story 3.1 inclusion policy (tenant authority failure / collapse).                   |

### `ProjectContextFreshness` (`src/Hexalith.Projects.Contracts/Ui/ProjectContextFreshness.cs`, Story 3.1)

| Value         | Current producer                                                                        | Notes                                                                |
| ------------- | --------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `Fresh`       | Story 3.1 inclusion policy (tenant projection `Fresh`).                                 |                                                                      |
| `Stale`       | Story 3.1 inclusion policy (tenant projection `Stale` on read-only operations).         | Materialized as a `Refresh` surface in Story 3.4.                    |
| `Unavailable` | Story 3.1 inclusion policy (tenant projection `Unavailable`).                           |                                                                      |
| `Unknown`     | Story 3.1 inclusion policy (tenant projection `Future` / `Unknown` / authority missing).| Default sentinel; never collapses to `Stale` on its own.             |

### `ProjectConversationTrustSignal` (`src/Hexalith.Projects.Contracts/Queries/ProjectConversationTrustSignal.cs`)

| Value             | Current producer                                                                |
| ----------------- | ------------------------------------------------------------------------------- |
| `Current`         | Story 2.1 Conversation Reference Read ACL on success.                           |
| `Stale`           | Story 2.1 ACL when upstream projection evidence is stale.                       |
| `Rebuilding`      | Story 2.1 ACL when upstream read model is rebuilding.                           |
| `Unavailable`     | Story 2.1 ACL when upstream is unreachable.                                     |
| `Forbidden`       | Story 2.1 ACL on auth failure.                                                  |
| `Redacted`        | Story 2.1 ACL when upstream metadata is policy-redacted.                        |
| `MixedGeneration` | Story 2.1 ACL when upstream list detects mixed projection generations.          |

### `TenantAccessOutcome` (`src/Hexalith.Projects/Authorization/TenantAccessOutcome.cs`)

| Value                       | Current producer                                                                 | Notes                                                                       |
| --------------------------- | -------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `Allowed`                   | Story 1.6 `TenantAccessAuthorizer` on success.                                   |                                                                             |
| `Denied`                    | Story 1.6 `TenantAccessAuthorizer` (explicit denial).                            |                                                                             |
| `StaleProjection`           | Story 1.6 `TenantAccessAuthorizer` (bounded-stale projection observed).          |                                                                             |
| `UnavailableProjection`     | Story 1.6 `TenantAccessAuthorizer` (projection rebuilding / absent).             |                                                                             |
| `UnknownTenant`             | Story 1.6 `TenantAccessAuthorizer` (no tenant projection row).                   |                                                                             |
| `DisabledTenant`            | Story 1.6 `TenantAccessAuthorizer` (tenant disabled).                            |                                                                             |
| `MalformedEvidence`         | Story 1.6 `TenantAccessAuthorizer` (claim/projection mismatch).                  |                                                                             |
| `TenantMismatch`            | Story 1.6 `TenantAccessAuthorizer` (cross-tenant claim).                         | Boundary-collapsed by Story 3.1 to `Unauthorized` with `tenantMismatch` diagnostic. |
| `MissingAuthoritativeTenant`| Story 1.6 `TenantAccessAuthorizer` (no authoritative tenant claim).              |                                                                             |
| `ReplayConflict`            | Story 1.6 `TenantAccessAuthorizer` (envelope-watermark replay conflict).         |                                                                             |

### `TenantProjectionFreshnessStatus` (`src/Hexalith.Projects/Authorization/TenantProjectionFreshnessStatus.cs`)

| Value         | Current producer                                                                | Notes                                                                  |
| ------------- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| `Unknown`     | Story 1.6 `TenantAccessAuthorizer` (no signal observed).                        |                                                                        |
| `Fresh`       | Story 1.6 `TenantAccessAuthorizer` (projection watermark recent).               |                                                                        |
| `Stale`       | Story 1.6 `TenantAccessAuthorizer` (projection watermark older than threshold). | Surfaced by Story 3.1 as `ProjectContextFreshness.Stale`.              |
| `Future`      | Story 1.6 `TenantAccessAuthorizer` (projection watermark ahead of clock).       | Story 3.1 maps to `ProjectContextFreshness.Unknown`.                   |
| `Unavailable` | Story 1.6 `TenantAccessAuthorizer` (no projection row observed).                | Surfaced by Story 3.1 as `ProjectContextFreshness.Unavailable`.        |

**Maintenance rule.** Every new shared-vocabulary value (existing enum addition or new policy enum)
MUST add a row here in the same PR. Stories 3.2–3.5 may add `current producer` annotations for
existing values; they may not remove rows.
