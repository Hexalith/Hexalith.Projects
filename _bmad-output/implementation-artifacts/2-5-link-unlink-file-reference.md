---
baseline_commit: 21da98fd8fd8ebc7f10ae152f232fc0f0f52f2e8
---

# Story 2.5: Link/Unlink File Reference

## Status

done

## Story

As **Hexalith.Chatbot**,
I want **to link authorized file references to a Project and unlink them without changing the Project Folder**,
so that **specific files can be part of Project Context as optional references**.

This story adds the Projects-owned optional File Reference slice for FR-9 and the file portion of FR-11. Hexalith.Folders remains the lifecycle, file metadata, content, path-policy, and authorization owner. Hexalith.Projects stores only stable reference identifiers and bounded safe metadata needed for Project reads, reference index, later context assembly, and audit.

Story 2.4 already established the Project Folder reference pattern, the Folders ACL boundary, `ProjectReferenceIndexProjection`, safe rejection handling, and the degraded `ProjectFolderCreationPending` path. Story 2.5 must extend that pattern for optional file references. It must not replace or weaken the Project Folder rule: active Projects still require exactly one Project Folder, while file references are a bounded optional set.

## Acceptance Criteria

1. Projects exposes command-async mutations for linking and unlinking File References under an active Project. Mutations require `Idempotency-Key`, preserve `X-Correlation-Id` and `X-Hexalith-Task-Id`, validate route/body Project identity equality, use closed request schemas with `requestSchemaVersion`, and return the existing `AcceptedCommand` shape on accepted dispatch.
2. `LinkFileReference` emits a metadata-only `FileReferenceLinked` event after the server has validated the referenced file through the Folders ACL. The event records stable Project identity, stable File/Folder-owned reference identity, safe display metadata, actor/correlation/task/idempotency metadata, and no file contents, raw paths, unrestricted paths, provider payloads, diffs, secrets, tokens, or raw Folders authorization details.
3. File References are optional and do not replace the single Project Folder. Linking a file must not clear, replace, satisfy, or auto-create the Project Folder; it may only add/update a file-reference association for an already active Project whose Project Folder rule remains intact.
4. `UnlinkFileReference` emits a metadata-only `FileReferenceUnlinked` event that removes the Project-to-file association only. It never deletes, removes, archives, reads, mutates, or otherwise changes the underlying file in Hexalith.Folders.
5. Folders file-reference authorization, lifecycle/freshness, path-policy, and redaction evidence are delegated to a Projects server ACL, extending the Story 2.4 Folders boundary. Denied, missing, stale, redacted/excluded, archived, tenant-mismatched, malformed, or unavailable evidence fails closed with Projects-safe reason/state codes.
6. The development agent must HALT before coding if no trustworthy Folders file metadata authorization path exists. An acceptable path is a current typed-client/server route that verifies file metadata by Project Folder/workspace/path or stable file reference without returning content bytes. If only contract-only or unmapped routes exist, the dev agent must obtain architecture approval for an explicit degraded state before implementation; it must not silently accept unverified file links.
7. `ProjectState`, `ProjectDetailProjection`, Project read reference summaries, and `ProjectReferenceIndexProjection` are extended for file references using bounded metadata-only models. File-reference projection rows use reference kind `file`, shared `ReferenceState`, freshness metadata, and deterministic tenant/project/reference keys.
8. Idempotency is deterministic and field-scoped. Equivalent duplicate link/unlink requests replay or no-op safely; conflicting same-key requests reject. Link idempotency includes Project id, file reference id, safe file metadata fields, operation, and schema version. Unlink idempotency includes Project id, file reference id, operation/unlink intent, and schema version.
9. Public contracts evolve only through `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`; generated client and idempotency helper artifacts are regenerated through the existing workflow and never hand-edited.
10. `docs/event-catalog.md` is updated with `FileReferenceLinked`, `FileReferenceUnlinked`, and file-reference link/unlink rejection behavior before merge.
11. Focused tests cover aggregate/state handling, projection/index behavior, endpoint validation, Folders file ACL mapping, idempotency, OpenAPI/client generation, no-payload-leakage, and unlink-does-not-delete semantics.

## Tasks / Subtasks

- [x] Confirm the Folders file-reference authorization capability gate before coding. (AC: 5, 6)
  - [x] Inspect current Folders OpenAPI, generated client, and server endpoint mapping for metadata-only file validation routes such as `GetFolderFileMetadataAsync`, `ListFolderFilesAsync`, `SearchFolderFilesAsync`, or `GlobFolderFilesAsync`.
  - [x] Verify whether the chosen route is actually server-mapped in the current working tree; current evidence shows Folders server maps lifecycle/effective-permissions/archive, while context/file query routes may be contract/client-only.
  - [x] Record the selected path in the Dev Agent Record before implementation: verified available metadata route, approved in-topology route, or explicit degraded file-reference pending/unavailable behavior.
  - [x] HALT if no approved file-reference authorization path exists. (Gate PASSED — no HALT required.)
- [x] Add Projects contracts for file-reference link/unlink. (AC: 1, 8, 9)
  - [x] Add `LinkFileReference` and `UnlinkFileReference` command contracts under `src/Hexalith.Projects.Contracts/Commands`.
  - [x] Add safe metadata models for file references, for example `ProjectFileReference` and `ProjectFileReferenceMetadata`; keep them reference-only and bounded.
  - [x] Extend the OpenAPI spine with command-async routes `POST /api/v1/projects/{projectId}/files/{fileReferenceId}/link` and `DELETE /api/v1/projects/{projectId}/files/{fileReferenceId}`.
  - [x] Use closed camelCase request schemas with `requestSchemaVersion: "v1"`, explicit operation/unlink intent, route/body Project identity equality, and no tenant/principal/actor authority fields.
  - [x] Regenerate Projects generated client and idempotency helpers; do not hand-edit `.g.cs`.
- [x] Add Projects domain command/event/state behavior. (AC: 2, 3, 4, 8)
  - [x] Add `FileReferenceLinked` and `FileReferenceUnlinked` success events under `src/Hexalith.Projects.Contracts/Events`.
  - [x] Extend `ProjectAggregate.Handle(...)` (new `ProjectAggregate.References.cs` partial), `ProjectState`, `ProjectStateApply`, `ProjectResult`, and `ProjectResultCode` for file-reference link/unlink.
  - [x] Keep a bounded file-reference collection or map in `ProjectState`; reject malformed identifiers, duplicates with conflicting metadata, missing Projects, archived Projects, tenant mismatches, and unsafe metadata.
  - [x] Treat equivalent duplicate link/unlink as idempotent replay/no-op and conflicting same-key as idempotency conflict.
  - [x] Map link rejections to `ProjectReferenceLinkRejected` and unlink rejections to `ProjectReferenceUnlinkRejected` with reference kind `file` and safe reference ids only.
- [x] Extend the Folders ACL boundary for file references. (AC: 5, 6)
  - [x] Add a narrowly named sibling `IProjectFileReferenceDirectory` in `src/Hexalith.Projects.Server/Folders/` (with `FoldersProjectFileReferenceDirectory`, `ProjectFileReferenceValidationResult`, `UnavailableProjectFileReferenceDirectory`).
  - [x] Validate file references using Folders-owned evidence only (`GetFolderFileMetadata`, metadata-only). Projects does not infer access from request payloads, cached local metadata, or the Project Folder reference alone.
  - [x] Map Folders `401/403/404` to safe denial, `409` to archived, redacted/excluded/binary-disallowed metadata to fail-closed, stale freshness to stale, and `408`/`413`/`422`/5xx/transport/serialization failures to validation-failed or retryable unavailable.
  - [x] Keep generated Folders client usage transient/request-scoped as in Story 2.4 (`TryAddTransient`); no singleton bearer-token-dependent typed client.
  - [x] Add fail-closed unavailable implementation for hosts/tests without a Folders client.
- [x] Wire Projects endpoints and command submission. (AC: 1, 5, 8)
  - [x] Add authorization action tokens `projects:link_file_reference` and `projects:unlink_file_reference` to `ProjectAuthorizationGate`.
  - [x] Gate Project mutation intent before any Folders ACL call. Unauthorized, hidden, archived, stale, or unavailable Project evidence does not touch Folders.
  - [x] Add endpoint handlers in `ProjectsDomainServiceEndpoints` using existing mutation envelope parsing, safe problem mapping, route/body checks, and command-async 202 response helpers.
  - [x] Extend `IProjectCommandSubmitter`, `EventStoreProjectCommandSubmitter`, `ProjectsDomainProcessor`, and `ProjectsServerModule` command type constants.
  - [x] Ensure the `/process` payload remains metadata-only and rejects unknown fields.
- [x] Extend projections and reads. (AC: 3, 7)
  - [x] Extend `ProjectDetailItem` and `ProjectDetailProjection` to expose file-reference summaries alongside the Project Folder reference.
  - [x] Extend `ProjectsDomainServiceEndpoints.ToProjectReferenceSummaries(...)` and the OpenAPI `ProjectReferenceSummary.referenceKind` enum to include `file`.
  - [x] Extend `ProjectReferenceIndexProjection` to index file links (per-kind key prefix) and remove file rows on unlink without removing the Project Folder row.
  - [x] Preserve deterministic ordering by reference kind then reference id.
- [x] Update documentation and validation artifacts. (AC: 9, 10, 11)
  - [x] Update `docs/event-catalog.md` with file-reference events and rejection cases.
  - [x] Update payload/no-leakage fixtures or assertions for all new DTOs/events/problems.
  - [x] Update story completion notes with the selected Folders capability path and any degraded behavior approval.
- [x] Validate the story implementation. (AC: 11)
  - [x] Run focused `Hexalith.Projects.Tests` aggregate/projection/no-leakage tests. (179/179)
  - [x] Run focused `Hexalith.Projects.Server.Tests` endpoint/Folders ACL tests. (158/158)
  - [x] Run `Hexalith.Projects.Contracts.Tests` OpenAPI contract-spine tests. (126/126)
  - [x] Run `Hexalith.Projects.Client.Tests` generation/idempotency tests. (29/29)
  - [x] Run `dotnet test Hexalith.Projects.slnx` when scoped tests are clean. (506/506; Integration 14/14)
  - [x] Run `git diff --check`; clean for story-touched files (no whitespace errors; hand-written sources normalized to the repository-normal LF used by HEAD).

## Dev Notes

### Current Implementation Facts

- Story 2.4 implemented the Project Folder reference flow and committed it. The current root commit is `436fc23 feat(story-2.4): add project folder reference flow`.
- Current Projects folder-reference code includes `SetProjectFolder`, `ProjectFolderSet`, `ProjectFolderCreationPending`, `ProjectFolderMetadata`, `ProjectFolderReference`, `IProjectFolderDirectory`, `FoldersProjectFolderDirectory`, `ProjectFolderValidationResult`, and `UnavailableProjectFolderDirectory`.
- `ProjectAggregate` is authored as `partial` and already has Story 2.4 reference handling in `ProjectAggregate.cs`. Later reference work may split into `ProjectAggregate.References.cs` only if that matches local style and reduces churn.
- `ProjectState` currently stores one `ProjectFolderReference? ProjectFolder`; it does not yet store File References.
- `ProjectStateApply`, `ProjectDetailProjection`, `ProjectListProjection`, and `ProjectReferenceIndexProjection` currently handle `ProjectFolderSet` and `ProjectFolderCreationPending`. Unknown Project event types throw in state/projections; any new file events must be added everywhere before tests will pass.
- `ProjectReferenceIndexProjection` currently uses a folder-only prefix (`:references:folder:`) and removes all rows with that prefix when replacing the folder. File references need their own kind/key behavior so folder replacement cannot remove file rows and file unlink cannot remove the folder row.
- `ProjectsDomainServiceEndpoints.ToProjectReferenceSummaries(...)` currently returns only the Project Folder reference. The OpenAPI `ProjectReferenceSummary.referenceKind` enum currently includes `conversation`, `folder`, and `memory`, but not `file`; Story 2.5 must add `file`.
- `ProjectReferenceLinkRejected` and `ProjectReferenceUnlinkRejected` already exist and are intended for conversation, folder, file, and memory reference failures. `ProjectResult.ToRejectionEvent()` currently maps only `SetProjectFolder` to link rejection kind `folder`; file commands must extend this safely and use unlink rejection for unlink.
- Folders generated client exposes metadata-only file query methods including `ListFolderFilesAsync`, `GetFolderFileMetadataAsync`, `SearchFolderFilesAsync`, `GlobFolderFilesAsync`, and `ReadFileRangeAsync`. Do not use `ReadFileRangeAsync` in this story because it can return content bytes.
- Current Folders server endpoint mapping evidence shows external routes for lifecycle status, effective permissions, archive, `/process`, and `/project`; context/file query routes may be contract/client-only in this checkout. Treat this as a hard capability gate before implementation.

### Required Folders/File Authorization Decision

Development must not silently link a file based only on a caller-provided file id, path, or metadata.

Before code changes, choose one path and record it:

- **Available metadata path:** use a verified Folders typed-client/server route that returns metadata-only file evidence after tenant access, folder ACL, path policy, sensitivity/redaction, and freshness checks. The Projects ACL may then translate accepted metadata into safe Projects file-reference metadata.
- **Approved in-topology path:** use an explicitly approved internal/Dapr route to equivalent Folders file metadata evidence. The approval must state that no file content bytes cross into Projects.
- **Approved degraded path:** add a named pending/unavailable file-reference state, for example `FileReferenceLinkPending`, only if product/architecture approves accepting the link request before Folders file metadata evidence is available. The pending event must contain only safe identifiers, display intent, retry/correlation/task/idempotency metadata, and a stable reason such as `file_reference_authorization_unavailable`. It must not be treated as included Project Context until Folders evidence confirms it.

If none of these is approved, HALT.

### Guardrails

- Keep aggregate, state apply, and projections pure. No Folders, Dapr, HTTP, logging, filesystem, provider, or content calls from domain core.
- Store sibling file identifiers as plain strings per `docs/adr/identifier-boundary.md`. Do not mint a Projects-owned `FileId` value object.
- Do not store raw paths or unrestricted filesystem paths in Projects events/projections. If a safe display name or path metadata is needed, derive it only from Folders-authorized metadata and keep it bounded. Prefer opaque file reference id plus safe display metadata over path-like fields.
- Never store or echo file contents, byte ranges, diffs, provider payloads, raw path lists, repository internals, local paths, tokens, secrets, raw exception text, or raw upstream ProblemDetails bodies.
- Linking a File Reference must not read file contents. It verifies and records a reference only.
- Unlinking a File Reference must not call Folders file removal APIs and must not delete the underlying file.
- Keep the single Project Folder rule from Story 2.4 intact. File references supplement Project Context; they do not satisfy or replace the Project Folder requirement.
- Reuse shared `ReferenceState` values and existing safe ProblemDetails helpers; do not add parallel string enums for file link state.
- Keep public contracts additive and serialization-tolerant. No `V2` commands/events/schemas.
- Do not initialize or update nested submodules recursively.

### Suggested API Shape

The exact routes may follow local endpoint conventions, but the intended behavior is:

```http
POST /api/v1/projects/{projectId}/files/{fileReferenceId}/link
Idempotency-Key: <required>
X-Correlation-Id: <propagated>
X-Hexalith-Task-Id: <propagated>
```

Request body:

```json
{
  "requestSchemaVersion": "v1",
  "operation": "link",
  "projectId": "proj_...",
  "fileReferenceId": "file_...",
  "fileMetadata": {
    "displayName": "contract.pdf"
  }
}
```

```http
DELETE /api/v1/projects/{projectId}/files/{fileReferenceId}
Idempotency-Key: <required>
X-Correlation-Id: <propagated>
X-Hexalith-Task-Id: <propagated>
```

Request body:

```json
{
  "requestSchemaVersion": "v1",
  "operation": "unlink",
  "unlinkIntent": "removeReference",
  "projectId": "proj_...",
  "fileReferenceId": "file_..."
}
```

If Folders file metadata requires a folder/workspace/path tuple rather than a stable file id, the public Projects request may include the minimum safe identifier set needed by the Folders ACL, but only after proving those fields are not raw local paths or content-bearing payloads. Client-supplied metadata is comparison/display intent only; Folders evidence is authority.

### Files To Read Before Editing

- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md`
- `_bmad-output/implementation-artifacts/2-3-link-move-conversation-write-side.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md`
- `_bmad-output/planning-artifacts/research/technical-hexalith-folders-integration-research-2026-05-24.md`
- `docs/adr/identifier-boundary.md`
- `docs/event-catalog.md`
- `docs/payload-taxonomy.md`
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Contracts/Events/ProjectReferenceLinkRejected.cs`
- `src/Hexalith.Projects.Contracts/Events/ProjectReferenceUnlinkRejected.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectFolderReference.cs`
- `src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectResultCode.cs`
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs`
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs`
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs`
- `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs`
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFolderDirectory.cs`
- `src/Hexalith.Projects.Server/Folders/ProjectFolderValidationResult.cs`
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs`
- `src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs`
- `src/Hexalith.Projects.Server/ProjectsServerModule.cs`
- `Hexalith.Folders/src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml`
- `Hexalith.Folders/src/Hexalith.Folders.Client/Generated/HexalithFoldersClient.g.cs`
- `Hexalith.Folders/src/Hexalith.Folders.Server/FoldersDomainServiceEndpoints.cs`

### Testing Requirements

- Aggregate tests cover link initial file reference, duplicate equivalent link, conflicting metadata/idempotency, multiple bounded file references, unlink existing reference, unlink missing reference, archived Project rejection, tenant mismatch, and malformed unsafe file identifiers.
- State/replay tests prove `FileReferenceLinked` and `FileReferenceUnlinked` replay deterministically, duplicate delivery is idempotent, unknown events still fail loudly, and unlink removes only the targeted file reference.
- Projection tests cover `ProjectDetailProjection` file summaries and `ProjectReferenceIndexProjection` file add/remove behavior without deleting folder references.
- Endpoint tests cover required idempotency, route/body project and file identity mismatch, missing/unknown fields rejected by closed JSON binding, archived/unauthorized Project denial before Folders ACL call, Folders denied/stale/unavailable mapping, and 202 command-async accepted responses.
- Folders ACL tests cover accepted metadata evidence, missing file, denied file, redacted/excluded file, stale freshness, tenant mismatch, archived/inactive folder or file evidence, unavailable route/client, thrown `HexalithFoldersApiException`, thrown `HttpRequestException`, and cancellation passthrough.
- OpenAPI/client tests prove link/unlink operations exist, generated methods exist, mutation operations require idempotency, query endpoints reject idempotency, and generated helper hashes match the spine field list.
- NoPayloadLeakage tests scan new command/event/request/response/problem/projection metadata for forbidden terms: file contents, contentBytes, raw path lists, local paths, diffs, provider payload, repository internals, secrets, tokens, claims, raw upstream problem details, Dapr internals, and EventStore internals.
- Negative tests must prove unlink never invokes Folders remove-file APIs and link never invokes Folders read-range/content APIs.

### Previous Story Intelligence

- Story 2.1 established the ACL posture: sibling context access belongs in `Projects.Server`, and suspicious upstream tenant/project evidence must close the whole result rather than return partial poisoned data.
- Story 2.3 established Projects write-side mutation patterns: command-async surface, route/body identity validation, idempotency checks, safe problem mapping, authorization before upstream work, and metadata-only diagnostics.
- Story 2.4 established the Folders ACL and reference-index pattern to extend. Reuse its typed-client registration behavior, `ProjectAuthorizationGate` style, `ProjectCommandSubmitter` path, generated-client workflow, and no-payload-leakage coverage.
- Story 2.4 review fixed unsafe malformed reference-id echo in `ProjectReferenceLinkRejected`. File-reference rejection logic must preserve that fix: malformed file ids become `unknown` or are omitted; they are never echoed raw.
- Story 2.4 selected a degraded Project Folder creation path because external Folders `CreateFolder` was not mapped. Story 2.5 must perform the same explicit capability gate for Folders file metadata authorization before coding.
- Recent commit history shows the preferred pattern is additive, story-scoped changes with focused lanes and no nested recursive submodule initialization: `436fc23` (folder reference flow), `ad2b1e9` (conversation write-side), `3522c99` (conversation reassignment), `c61bb2e` (conversation read ACL).

### Out Of Scope

- Reading, returning, indexing, summarizing, embedding, or storing file contents.
- Calling Folders `ReadFileRangeAsync` or any content/byte-range endpoint from Projects for this story.
- Adding or exposing Folders file add/change/remove behavior.
- Implementing full Project Context assembly, context selection allowlist, explanation surfaces, or resolution from attachments (Epic 3/4).
- Replacing, removing, or weakening the Project Folder behavior from Story 2.4.
- Memory links, memory model decision spike, MCP/CLI/Web operational console surfaces, audit timeline projection, or FrontComposer views.
- Updating Folders external REST mappings unless a separate explicit task is created for Hexalith.Folders.
- Any nested recursive submodule initialization/update or unrelated submodule pointer changes.

### Developer HALT Conditions

- HALT if no current Folders file metadata authorization path is verified and no explicit degraded behavior is approved.
- HALT if the implementation would accept a link based only on caller-supplied file id, file path, or metadata without Folders authorization/freshness evidence.
- HALT if the implementation requires storing file contents, byte ranges, raw file paths, unrestricted paths, diffs, provider payloads, repository internals, raw Folders problem bodies, secrets, or tokens in Projects.
- HALT if product or architecture asks file references to replace the single Project Folder rule for active Projects.
- HALT if unlinking a File Reference would delete or mutate the underlying file in Hexalith.Folders.
- HALT if the only available Folders API for validation returns content bytes rather than metadata-only evidence.
- HALT if Folders redaction/exclusion evidence cannot be represented without leaking path/content details.
- HALT if generated `.g.cs` files would need hand edits.
- HALT if the change requires breaking public contracts, introducing `V2` command/event types, or bypassing the OpenAPI spine.
- HALT if completing the story requires nested recursive submodule initialization or update.

## References

- `_bmad-output/planning-artifacts/epics.md` - Story 2.5 ACs, FR-9/FR-11 file-reference scope, Epic 2 context.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` - FR-9 and FR-11 consequences; Project Context and non-goals.
- `_bmad-output/planning-artifacts/architecture.md` - AR-5/AR-6 commands/events, AR-8 reference index, AR-11 Folders ACL, AR-18 shared vocabulary.
- `_bmad-output/planning-artifacts/research/technical-hexalith-folders-integration-research-2026-05-24.md` - Folders client, safe-denial, metadata-only, and currently mapped external route evidence.
- `_bmad-output/planning-artifacts/research/domain-eventstore-persistence-for-hexalith-projects-module-data-research-2026-05-24.md` - candidate file reference commands/events and EventStore metadata-only constraints.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` - Folders ACL, ProjectReferenceIndexProjection, degraded-path precedent, and Story 2.4 review fixes.
- `_bmad-output/implementation-artifacts/2-3-link-move-conversation-write-side.md` - command-async mutation, idempotency, route/body validation, safe ProblemDetails, and no local ownership precedent.
- `docs/adr/identifier-boundary.md` - sibling file references are plain strings, not Projects-owned VOs.
- `docs/event-catalog.md` - authoritative event catalog to update.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` - Projects OpenAPI spine and generated-client source of truth.
- `Hexalith.Folders/src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml` - Folders metadata-only file query contracts and content-bearing route distinction.
- `Hexalith.Folders/src/Hexalith.Folders.Client/Generated/HexalithFoldersClient.g.cs` - generated Folders client method evidence.
- `Hexalith.Folders/src/Hexalith.Folders.Server/FoldersDomainServiceEndpoints.cs` - current server-mapped Folders route evidence.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (create-story); Claude Opus 4.7 (dev-story implementation, 2026-05-27)

### Debug Log References

- Create-story workflow only; no implementation commands were run for this story.
- 2026-05-26: Resolved `bmad-create-story` workflow customization and loaded project config, sprint status, planning artifacts, root/Folders project context, Story 2.4 and Story 2.3 artifacts, recent git history, current Projects folder-reference code, and Folders file metadata contract/client/server evidence.
- 2026-05-26: Confirmed target sprint key `2-5-link-unlink-file-reference` was `backlog` before creating this artifact.
- 2026-05-26: Created this story artifact and prepared sprint-status update to `ready-for-dev`.

#### Capability gate decision (2026-05-27, Task 1 / AC 5,6) — PASSED, no HALT

Selected path: **Available metadata path** via the Folders metadata-only route `GetFolderFileMetadata`.

Evidence (verified directly in the current working tree, not just contract/client):
- `Hexalith.Folders/src/Hexalith.Folders.Server/FoldersDomainServiceEndpoints.cs` maps the file-context query routes as real external HTTP endpoints: `POST .../context/metadata` (GetFolderFileMetadata, line 673), `GET .../context/tree` (ListFolderFiles, 648), `POST .../context/search` (721), `POST .../context/glob` (773). These are NOT contract/client-only.
- `GetFolderFileMetadata` returns `FileMetadataResult` → `FileMetadataItem` (`path`, `kind`, `byteLength`, `sensitivity`, `redaction`) with **no content bytes**, enforced in documented order: `tenant_access → folder_acl → path_policy → sensitivity_classification → c4_bounds → query_execution`.
- The only content-bearing route is `POST .../context/range-read` (ReadFileRange → `FileRangeReadResult.contentBytes`). It is explicitly **NOT used** by this story.
- Generated client method `GetFolderFileMetadataAsync(folderId, workspaceId, correlationId, taskId, freshness, FileMetadataRequest)` exists in `HexalithFoldersClient.g.cs`.

Consequences for the Projects design:
- Folders addresses a file by `(folderId, workspaceId, workspace-relative path)`, not a single opaque file id. Per the story's explicit allowance ("the public Projects request may include the minimum safe identifier set needed by the Folders ACL"), the Projects link request carries `folderId`, `workspaceId`, and a bounded workspace-relative `filePath` solely so the server ACL can call `GetFolderFileMetadata`. `workspaceId`/`filePath` are transient (endpoint→ACL only) and are NOT stored in any command/event/projection/state.
- Projects stores only: opaque `fileReferenceId`, owning `folderId`, safe `displayName`, shared `ReferenceState`, optional reason code, and an event-carried `observedAt` — honouring "prefer opaque file reference id plus safe display metadata over path-like fields" and "do not store raw paths".

#### Environment notes
- The repo SDK (`global.json` 10.0.302) lives at `/home/administrator/.dotnet`; `/usr/bin/dotnet` resolves to 10.0.108 and fails `rollForward: latestPatch`. All builds use `DOTNET_ROOT=/home/administrator/.dotnet` + that `dotnet`.
- NSwag client regeneration works and is byte-deterministic. The `GenerateHexalithProjectsIdempotencyHelpers` MSBuild step passes a Windows-style `...Client\nswag.json` path that fails on Linux; the helper generator is therefore run manually with forward-slash paths (no `.g.cs` hand-edits).

### Completion Notes List

- (Create-story) Story context created only — superseded by the implementation below.
- **Capability gate (Task 1) PASSED, no HALT.** Selected the **Available metadata path** via the Folders server-mapped, metadata-only `GetFolderFileMetadata` route (`POST .../context/metadata`). No degraded pending/unavailable behavior was needed because a real metadata route exists. The content-bearing `ReadFileRange` route is never used. Full evidence and the file-addressing design decision are recorded in the Debug Log above.
- **Optional file-reference slice implemented end-to-end:** `LinkFileReference`/`UnlinkFileReference` command-async mutations → metadata-only `FileReferenceLinked`/`FileReferenceUnlinked` events → pure `ProjectAggregate` handlers (bounded set, idempotent replay/no-op, conflict, archived/tenant/malformed rejections) → `ProjectState`/`ProjectStateApply` → `ProjectDetailProjection` + `ProjectReferenceIndexProjection` (per-kind keys so folder and file lanes are disjoint) → server endpoints + Folders file ACL + command submitter/processor → OpenAPI spine + regenerated client/idempotency helpers → `docs/event-catalog.md`.
- **Project Folder rule preserved:** linking/unlinking a file never clears, replaces, satisfies, or auto-creates the single Project Folder, and folder replacement never removes file rows (proven by projection tests).
- **Unlink never touches Folders:** the unlink endpoint takes no Folders dependency and makes no Folders call; link never calls the content/range-read route (proven by the recording-handler ACL test). No file contents, byte ranges, raw/workspace paths, provider payloads, tokens, or raw upstream problem bodies are stored or echoed (NoPayloadLeakage tests extended).
- **Idempotency parity:** server-side fingerprints (`ComputeLinkFileReferenceFingerprint`/`ComputeUnlinkFileReferenceFingerprint`) match the regenerated client helper field lists and canonical line shape (cross-checked by `ClientGenerationTests`).
- **Validation:** build 0W/0E; full solution `dotnet test Hexalith.Projects.slnx` = 506/506 (Tests 179, Server 158, Contracts 126, Client 29, Integration 14); `git diff --check` clean for story-touched files. NSwag client regen is deterministic; the idempotency-helper MSBuild step was run manually with forward-slash paths to work around a pre-existing Windows backslash path in the build target on Linux (no `.g.cs` hand-edits). No submodule pointer changes; no nested recursive submodule init; no commit.

### File List

New:
- `src/Hexalith.Projects.Contracts/Commands/LinkFileReference.cs`
- `src/Hexalith.Projects.Contracts/Commands/UnlinkFileReference.cs`
- `src/Hexalith.Projects.Contracts/Events/FileReferenceLinked.cs`
- `src/Hexalith.Projects.Contracts/Events/FileReferenceUnlinked.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectFileReference.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectFileReferenceMetadata.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.References.cs`
- `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs`
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs`
- `src/Hexalith.Projects.Server/Folders/ProjectFileReferenceValidationResult.cs`
- `src/Hexalith.Projects.Server/Folders/UnavailableProjectFileReferenceDirectory.cs`
- `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateFileReferenceTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ProjectFileReferenceDirectoryTests.cs`

Modified:
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` (regenerated)
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` (regenerated)
- `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectResultCode.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidationResult.cs`
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs`
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs`
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs`
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs`
- `src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs`
- `src/Hexalith.Projects.Server/ProjectsServerModule.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `docs/event-catalog.md`
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`
- `tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs`
- `tests/Hexalith.Projects.Tests/Projections/ProjectReferenceIndexProjectionTests.cs`
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ProjectAuthorizationGateTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ProjectQueryTenantFilterTests.cs`
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs`
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-05-26 | 1.0 | Created Story 2.5 artifact and set sprint status to ready-for-dev. | GPT-5 Codex |
| 2026-05-27 | 1.1 | Implemented optional File Reference link/unlink end-to-end (contracts, OpenAPI + regenerated client/helpers, aggregate/state/projections, Folders metadata-only file ACL, endpoints, docs, tests). Capability gate PASSED (Available metadata path). Full solution tests 506/506; status set to review. | Claude Opus 4.7 |
| 2026-05-28 | 1.2 | Adversarial senior review (story-automator-review). All 11 ACs verified implemented; build 0W/0E; full solution 511/511 (Tests 179, Server 163, Contracts 126, Client 29, Integration 14). 0 CRITICAL/HIGH/MEDIUM findings; 3 LOW observations recorded (not auto-fixed — see review notes). Status set to done. | Claude Opus 4.7 (review) |

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-05-28 · **Mode:** story-automator-review (adversarial, auto-fix) · **Outcome:** Approve → done

### Method

Read every file in the Dev Agent Record File List (13 new + modified sources, tests, OpenAPI, generated client/helpers, event catalog). Built `Hexalith.Projects.slnx` (0 warnings / 0 errors). Ran the full solution test suite: **511/511 passing, 0 failed** (Tests 179, Server 163, Contracts 126, Client 29, Integration 14 — note the Server lane grew from the 158 recorded at dev time to 163, and the total exceeds the 506 the dev notes claimed; the delta is additional passing tests, nothing was removed or skipped). Verified cross-surface idempotency-fingerprint parity down to the generated enum `EnumMember` wire values (`v1`/`link`/`unlink`/`removeReference`) against the server `ProjectCommandValidator` fingerprint lines. Audited every `[x]` task and every Acceptance Criterion against the actual implementation, and checked `git status` for new source files not declared in the File List (none — the 13 untracked `.cs` files exactly match the declared New list).

### Acceptance Criteria — all verified IMPLEMENTED

- **AC1–AC4 (command-async link/unlink, metadata-only events, optional non-replacing refs, unlink-never-touches-Folders):** Endpoints require `Idempotency-Key`, preserve correlation/task, enforce route↔body identity equality, use closed (`UnmappedMemberHandling.Disallow`) schemas, return `AcceptedCommand` 202. The unlink endpoint takes **no** `IProjectFileReferenceDirectory` dependency, so it structurally cannot call Folders (proven by `DeleteFile_Authorized_Returns202AndSubmitsUnlinkWithoutFoldersCall`). Link never calls the content-bearing `range-read` route (proven by `ValidateLink_AuthorizedNotRedactedFile_Accepts` asserting `RequestPaths` excludes `/context/range-read`). Folder lane untouched by file ops (`LinkFileReference_DoesNotTouchProjectFolder`, disjoint per-kind index keys).
- **AC5–AC6 (Folders ACL fail-closed + capability gate):** `FoldersProjectFileReferenceDirectory` maps 401/403/404→Denied, 409→Archived, 408/503/5xx→Unavailable, 400/413/422→ValidationFailed, redacted/excluded/non-`File` kind→fail-closed, stale freshness→Stale; `UnavailableProjectFileReferenceDirectory` fails closed when no client is registered. Gate recorded PASSED (Available metadata path via `GetFolderFileMetadata`).
- **AC7 (state/projections/reads):** `ProjectState.FileReferences` (bounded, `MaxFileReferences=100`), `ProjectStateApply`, `ProjectDetailProjection`, and `ProjectReferenceIndexProjection` all extended; deterministic ordering (kind then id); throw-on-unknown-event preserved.
- **AC8 (field-scoped idempotency):** Link fingerprint = {file_metadata.display_name, file_reference_id, folder_id, operation, project_id, request_schema_version}; unlink = {file_reference_id, operation, project_id, request_schema_version, unlink_intent}. Server fingerprint, OpenAPI `x-hexalith-idempotency-equivalence`, and generated helper field lists all agree.
- **AC9–AC11 (contract spine, docs, tests):** OpenAPI is the single source; client + idempotency helpers regenerated, not hand-edited; `docs/event-catalog.md` documents both events and the link/unlink rejection paths; endpoint/ACL/aggregate/projection/leakage/generation tests are comprehensive.

### Findings

No CRITICAL, HIGH, or MEDIUM findings. Three LOW observations, intentionally **not** auto-fixed (each would trade a green build / stable contract for marginal or ambiguous benefit — surfaced here for the team instead):

- **[LOW] `WorkspaceRelativePath` contract pattern is stricter than the server validator.** The OpenAPI pattern restricts to `[A-Za-z0-9._/-]`, while the server `IsWorkspaceRelativePath` accepts any non-control, non-backslash, non-leading-slash, non-`..`, non-empty-segment path (e.g. spaces, Unicode). Not a security gap (the value is transient, never stored, and Folders path policy is authoritative), but a contract/impl divergence. Deliberately left as-is: tightening the server could reject legitimate workspace filenames containing spaces/Unicode, and relaxing the contract weakens published guidance — the correct direction depends on what Folders workspaces actually permit, which is a team decision, not a safe unilateral edit.
- **[LOW] `ProjectFileReferenceValidationOutcome.TenantMismatch` is never produced** by `FoldersProjectFileReferenceDirectory` (Folders surfaces tenant mismatch as 403/404 → Denied, by design, to avoid existence disclosure). It is part of the documented safe-outcome taxonomy and is handled by the endpoint's `_ → SafeDenial` default. Left for taxonomy symmetry with the folder ACL; harmless.
- **[LOW, pre-existing / out of scope] Client vs server fingerprint `Escape` divergence for `U+2028`/`U+2029`.** The shared client hasher escapes line/paragraph separators while the server validator's `Escape` (which uses `char.IsControl`, false for `Zl`/`Zp`) leaves them literal, and `IsSafeMetadata` does not reject them. A display name containing `U+2028` would therefore hash differently on the two surfaces. This is a property of the pre-existing Story 1.3 hasher/validator shared by every command (CreateProject, SetProjectFolder, …), not introduced by Story 2.5, and server-side replay detection is internally consistent regardless. Flagged for a future hardening pass on the shared canonicaliser.

### Conclusion

The slice is correct, fail-closed, metadata-only, contract-clean, and well-tested. 0 CRITICAL → status advanced to **done**; no code changes applied (no fix-worthy defect).

## Validation Notes

- Target story matched explicit user request and sprint key: `2-5-link-unlink-file-reference`.
- Source analysis covered BMAD create-story workflow/checklist/template, sprint status, Epic 2 Story 2.5, PRD FR-9/FR-11, architecture commands/events/projection/ACL/vocabulary rules, root and Folders project context, Story 2.4 and Story 2.3 completion/review notes, recent git history, current Projects reference code, current Projects OpenAPI reference summary state, Folders generated file metadata client methods, Folders OpenAPI metadata/content route distinction, and current Folders server route mapping evidence.
- Latest external technical lookup was not needed for story creation because this story uses pinned local project versions and local OpenAPI/generated-client/server evidence; no new external library or public API is introduced.
- Checklist validation performed manually against the create-story quality checklist: the story includes concrete ACs, scoped tasks, current-code facts, previous-story intelligence, architecture guardrails, likely file locations, test requirements, out-of-scope boundaries, and developer HALT conditions.
- Validation result: ready-for-dev.
