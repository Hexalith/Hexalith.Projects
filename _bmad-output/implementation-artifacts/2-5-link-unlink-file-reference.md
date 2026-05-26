# Story 2.5: Link/Unlink File Reference

## Status

ready-for-dev

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

- [ ] Confirm the Folders file-reference authorization capability gate before coding. (AC: 5, 6)
  - [ ] Inspect current Folders OpenAPI, generated client, and server endpoint mapping for metadata-only file validation routes such as `GetFolderFileMetadataAsync`, `ListFolderFilesAsync`, `SearchFolderFilesAsync`, or `GlobFolderFilesAsync`.
  - [ ] Verify whether the chosen route is actually server-mapped in the current working tree; current evidence shows Folders server maps lifecycle/effective-permissions/archive, while context/file query routes may be contract/client-only.
  - [ ] Record the selected path in the Dev Agent Record before implementation: verified available metadata route, approved in-topology route, or explicit degraded file-reference pending/unavailable behavior.
  - [ ] HALT if no approved file-reference authorization path exists.
- [ ] Add Projects contracts for file-reference link/unlink. (AC: 1, 8, 9)
  - [ ] Add `LinkFileReference` and `UnlinkFileReference` command contracts under `src/Hexalith.Projects.Contracts/Commands`.
  - [ ] Add safe metadata models for file references, for example `ProjectFileReference` and `ProjectFileReferenceMetadata`; keep them reference-only and bounded.
  - [ ] Extend the OpenAPI spine with command-async routes, likely `POST /api/v1/projects/{projectId}/files/{fileReferenceId}/link` and `DELETE /api/v1/projects/{projectId}/files/{fileReferenceId}` or the local equivalent.
  - [ ] Use closed camelCase request schemas with `requestSchemaVersion: "v1"`, explicit operation/unlink intent, route/body Project identity equality, and no tenant/principal/actor authority fields.
  - [ ] Regenerate Projects generated client and idempotency helpers; do not hand-edit `.g.cs`.
- [ ] Add Projects domain command/event/state behavior. (AC: 2, 3, 4, 8)
  - [ ] Add `FileReferenceLinked` and `FileReferenceUnlinked` success events under `src/Hexalith.Projects.Contracts/Events`.
  - [ ] Extend `ProjectAggregate.Handle(...)`, `ProjectState`, `ProjectStateApply`, `ProjectResult`, and `ProjectResultCode` for file-reference link/unlink.
  - [ ] Keep a bounded file-reference collection or map in `ProjectState`; reject malformed identifiers, duplicates with conflicting metadata, missing Projects, archived Projects, tenant mismatches, and unsafe metadata.
  - [ ] Treat equivalent duplicate link/unlink as idempotent replay/no-op and conflicting same-key as idempotency conflict.
  - [ ] Map link rejections to `ProjectReferenceLinkRejected` and unlink rejections to `ProjectReferenceUnlinkRejected` with reference kind `file` and safe reference ids only.
- [ ] Extend the Folders ACL boundary for file references. (AC: 5, 6)
  - [ ] Extend `IProjectFolderDirectory` or add a narrowly named sibling such as `IProjectFileReferenceDirectory` in `src/Hexalith.Projects.Server/Folders/`.
  - [ ] Validate file references using Folders-owned evidence only. Projects must not infer access from request payloads, cached local metadata, or the Project Folder reference alone.
  - [ ] Map Folders `401/403/404` to safe denial, `409` or archived/inactive evidence to archived/conflict as appropriate, stale projection/freshness to retryable unavailable or stale, redacted/excluded metadata to fail-closed, and 5xx/transport/serialization failures to retryable unavailable.
  - [ ] Keep generated Folders client usage transient/request-scoped as in Story 2.4; do not singleton a bearer-token-dependent typed client.
  - [ ] Add fail-closed unavailable implementation for hosts/tests without a Folders client.
- [ ] Wire Projects endpoints and command submission. (AC: 1, 5, 8)
  - [ ] Add authorization action tokens such as `projects:link_file_reference` and `projects:unlink_file_reference` to `ProjectAuthorizationGate`.
  - [ ] Gate Project mutation intent before any Folders ACL call. Unauthorized, hidden, archived, stale, or unavailable Project evidence must not touch Folders.
  - [ ] Add endpoint handlers in `ProjectsDomainServiceEndpoints` using existing mutation envelope parsing, safe problem mapping, route/body checks, and command-async 202 response helpers.
  - [ ] Extend `IProjectCommandSubmitter`, `EventStoreProjectCommandSubmitter`, `ProjectsDomainProcessor`, and `ProjectsServerModule` command type constants.
  - [ ] Ensure the `/process` payload remains metadata-only and rejects unknown fields.
- [ ] Extend projections and reads. (AC: 3, 7)
  - [ ] Extend `ProjectDetailItem` and `ProjectDetailProjection` to expose file-reference summaries alongside the Project Folder reference.
  - [ ] Extend `ProjectsDomainServiceEndpoints.ToProjectReferenceSummaries(...)` and the OpenAPI `ProjectReferenceSummary.referenceKind` enum to include `file`.
  - [ ] Extend `ProjectReferenceIndexProjection` to index file links and remove file rows on unlink without removing the Project Folder row.
  - [ ] Preserve deterministic ordering by reference kind then reference id.
- [ ] Update documentation and validation artifacts. (AC: 9, 10, 11)
  - [ ] Update `docs/event-catalog.md` with file-reference events and rejection cases.
  - [ ] Update payload/no-leakage fixtures or assertions for all new DTOs/events/problems.
  - [ ] Update story completion notes with the selected Folders capability path and any degraded behavior approval.
- [ ] Validate the story implementation. (AC: 11)
  - [ ] Run focused `Hexalith.Projects.Tests` aggregate/projection/no-leakage tests.
  - [ ] Run focused `Hexalith.Projects.Server.Tests` endpoint/Folders ACL tests.
  - [ ] Run `Hexalith.Projects.Contracts.Tests` OpenAPI contract-spine tests.
  - [ ] Run `Hexalith.Projects.Client.Tests` generation/idempotency tests.
  - [ ] Run `dotnet test Hexalith.Projects.slnx --no-restore` when scoped tests are clean.
  - [ ] Run `git diff --check`; CRLF conversion warnings are acceptable only if they are repository-normal and no whitespace errors are reported.

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

GPT-5 Codex

### Debug Log References

- Create-story workflow only; no implementation commands were run for this story.
- 2026-05-26: Resolved `bmad-create-story` workflow customization and loaded project config, sprint status, planning artifacts, root/Folders project context, Story 2.4 and Story 2.3 artifacts, recent git history, current Projects folder-reference code, and Folders file metadata contract/client/server evidence.
- 2026-05-26: Confirmed target sprint key `2-5-link-unlink-file-reference` was `backlog` before creating this artifact.
- 2026-05-26: Created this story artifact and prepared sprint-status update to `ready-for-dev`.

### Completion Notes List

- Story context created only. No source implementation, tests, generation, submodule initialization, submodule update, or commit was performed.
- The story carries a hard Folders file-reference authorization gate. Development must confirm an available metadata-only Folders file validation route or get explicit approval for degraded pending/unavailable behavior before coding.
- The story explicitly forbids Projects file-content storage and forbids using content-bearing Folders `ReadFileRangeAsync` for link validation.
- The story extends the Story 2.4 reference-index/Folders ACL pattern while preserving the Project Folder as the required active Project boundary.

### File List

- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-05-26 | 1.0 | Created Story 2.5 artifact and set sprint status to ready-for-dev. | GPT-5 Codex |

## Validation Notes

- Target story matched explicit user request and sprint key: `2-5-link-unlink-file-reference`.
- Source analysis covered BMAD create-story workflow/checklist/template, sprint status, Epic 2 Story 2.5, PRD FR-9/FR-11, architecture commands/events/projection/ACL/vocabulary rules, root and Folders project context, Story 2.4 and Story 2.3 completion/review notes, recent git history, current Projects reference code, current Projects OpenAPI reference summary state, Folders generated file metadata client methods, Folders OpenAPI metadata/content route distinction, and current Folders server route mapping evidence.
- Latest external technical lookup was not needed for story creation because this story uses pinned local project versions and local OpenAPI/generated-client/server evidence; no new external library or public API is introduced.
- Checklist validation performed manually against the create-story quality checklist: the story includes concrete ACs, scoped tasks, current-code facts, previous-story intelligence, architecture guardrails, likely file locations, test requirements, out-of-scope boundaries, and developer HALT conditions.
- Validation result: ready-for-dev.
