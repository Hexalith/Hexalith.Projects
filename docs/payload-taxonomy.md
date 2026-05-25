# Payload classification taxonomy / allowlist (FS-1, NFR-2)

- Status: Authoritative
- Story: 1.2 (Shared vocabulary, identifiers & payload taxonomy)
- Requirements: FS-1, NFR-2
- Machine form: `src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs`

## Source of truth

> **FS-2 `NoPayloadLeakage` harness (Story 1.4) is built against this allowlist.**

This document and its machine-usable companion (`PayloadClassification`) are the **single source of
truth** for what may and may not cross the Projects boundary. Every `NoPayloadLeakage` test, the
ProjectContext assembly policy (Epic 3), rejection events, and shared models derive their safe-field
set from here. When the two forms disagree, that is a bug — keep them in sync.

The principle is **metadata-only everywhere** (architecture.md#Anti-patterns, NFR-2): events, logs,
DTOs, and audit records carry safe identifiers and states, never sibling-owned content. Projects holds
*references* to conversations, folders, files, and memories — it never copies their contents.

## Safe / reference-only fields (ALLOWED on the wire)

These categories MAY appear in events, projections, DTOs, logs, and audit records.

| Category (machine code) | Description |
| --- | --- |
| `OpaqueId` | Opaque identifiers — `ProjectId`, sibling reference ULIDs, message/aggregate IDs. |
| `ETag` | Optimistic-concurrency ETag. |
| `Version` | Aggregate / projection version number. |
| `TenantId` | Managed tenant identifier. |
| `ReferenceKind` | Reference kind / owner-context discriminator (conversation, folder, file, memory). |
| `OwnerContext` | The owning bounded-context name for a reference. |
| `Timestamp` | Event / audit timestamps (UTC). |
| `LifecycleState` | `ProjectLifecycle` value (e.g. `Active`, `Archived`). |
| `InclusionState` | `ReferenceState` value (e.g. `Included`, `Stale`, `Unauthorized`). |
| `ResolutionState` | `ResolutionResult` value (e.g. `NoMatch`, `SingleCandidate`). |
| `ReasonCode` | `ProjectReasonCode` / `ReferenceState` code (the canonical, name-based signal). |
| `CorrelationId` | Correlation identifier. |
| `CausationId` | Causation identifier. |
| `AuditId` | Audit-record identifier. |

Notes:

- A rejected **field NAME** (e.g. `"Title"`) is safe; the rejected field **VALUE** is never echoed.
- Reason codes are emitted by **name** (name-based JSON), never as integer ordinals.

## Forbidden sibling-owned content (NEVER on the wire)

These categories MUST NEVER appear in any Projects event, log, DTO, or audit record. They are owned by
sibling contexts and are only ever *referenced* by opaque ID.

| Category (machine code) | Why forbidden |
| --- | --- |
| `ConversationTranscriptText` | Conversation transcript / message text is conversation-owned content. |
| `FileContents` | File contents are folder/file-context-owned content. |
| `MemoryBody` | Memory bodies are memory-context-owned content. |
| `RawPrompt` | Raw prompts may contain user/secret content. |
| `Secret` | Secrets must never be persisted or logged. |
| `RawToken` | Raw access/refresh tokens must never be persisted or logged. |
| `FullCommandBody` | Full command bodies may contain setup payloads / sensitive values. |
| `UnrestrictedFilePath` | Unrestricted file paths can leak structure / sensitive content. |
| `LocalFilePath` | Local filesystem paths can leak host/structure details. |
| `SensitiveFolderName` | Folder names where they carry sensitive content. |

## How tests use this

The FS-2 `NoPayloadLeakage` harness (Story 1.4) loads `PayloadClassification.SafeFields` /
`PayloadClassification.ForbiddenContent` and asserts that serialized events/DTOs/logs expose only safe
categories and never any forbidden category. Rejection events in
`src/Hexalith.Projects.Contracts/Events/` are documented as metadata-only and reference this taxonomy by
the path constant `PayloadClassification.TaxonomyDocumentPath`.
