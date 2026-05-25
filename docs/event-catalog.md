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
