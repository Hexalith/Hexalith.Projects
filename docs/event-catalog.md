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
- **Purpose:** Records a refused sibling reference link/set attempt, including Project Folder set rejection paths (validation failure, replacement not confirmed, missing/archived project, authorization failure, or idempotency conflict).
- **Emitted by:** `ProjectAggregate.Handle(SetProjectFolder)` rejection paths and `/process` fail-closed payload paths.
- **Sensitivity class:** metadata-only.
- **Fields:** `ProjectId`, `TenantId`, `ReferenceKind` (for Story 2.4, `folder`), `ReferenceId` sibling identifier when safe, canonical `Reason`, optional `RejectedField` name, optional `CorrelationId`.
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
