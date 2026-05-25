# ADR: Identifier boundary — one `ProjectId` VO; sibling references as `string`

- Status: Accepted
- Date: 2026-05-25
- Story: 1.2 (Shared vocabulary, identifiers & payload taxonomy)
- Requirement: AR-7

## Context

AR-7 states: "reuse the owning context's Contracts identifier types (e.g. `ConversationId`,
Folders `FolderId`/`FileId`, Memories ids) rather than minting parallel value objects (VOs)."

Taken literally, this could be read as "import or re-create `ConversationId` / `FolderId` / `FileId`
/ `MemoryId` VOs inside Projects." A verification pass over the sibling Contracts/OpenAPI surfaces
shows that reading is **not** achievable as written, because those VOs do not exist as Contracts types:

- `Hexalith.Folders` exposes folder/file identifiers as plain `string`/ULID. `FolderId` appears only
  as a `string` schema in the Folders OpenAPI YAML — there is no `FolderId` value object in the
  `Hexalith.Folders.Contracts` package.
- `Hexalith.Conversations` exposes conversation identifiers as plain `string`/ULID. `ConversationId`
  appears in tests, not as a Contracts value object.
- `Hexalith.Memories` likewise exposes ids as plain `string`/ULID.

## Decision

Projects mints exactly **one** identifier value object:

- **`ProjectId`** — `sealed record` under `src/Hexalith.Projects.Contracts/Identifiers/`, with eager
  boundary validation and a custom `System.Text.Json` converter that serializes the opaque string
  value.

All **sibling references** (conversation, folder, file, memory) are held as plain **`string` (ULID)**
reference identifiers inside reference descriptors, **reusing each owning context's own representation**.
Projects does **not** invent `ConversationId` / `FolderId` / `FileId` / `MemoryId` value objects.

This is the correct application of AR-7's intent (avoid parallel/duplicate identifier types): inventing
Projects-local sibling VOs would itself be the ID-drift AR-7 warns against, since it would create a
second, divergent representation of an identifier the sibling already owns as a string.

## Identifier shape rule (EventStore retro R2-A7)

`ProjectId` and all aggregate/reference identifiers are **ULID-shaped strings, not GUIDs**. Validation
accepts any non-whitespace string (consistent with `Hexalith.EventStore.Contracts` `AggregateIdentity`
aggregate-id rules) and must **never** use `Guid.TryParse`. ULID and GUID share a 36-char shape only by
coincidence.

## Consequences

- One canonical place (`ProjectId`) defines Projects' own identity; everything downstream derives keys
  from `{tenant}:projects:{projectId}` via the identity-derivation helper.
- Reviewers expecting imported sibling VOs should note this intentional variance (flagged in the
  Story 1.2 Dev Agent Record).
- If a sibling later publishes a real Contracts identifier VO, Projects can adopt it at the reference
  boundary without changing `ProjectId`.
