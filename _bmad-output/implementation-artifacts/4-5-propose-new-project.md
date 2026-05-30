---
baseline_commit: 935caf5
---

# Story 4.5: Propose New Project

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to propose creating a new Project from the current conversation, attachments, and setup metadata when no suitable Project exists**,
so that **a user with no matching project can start one without losing the current context** _(FR-15; realizes UJ-2, UJ-3; depends on Story 2.2)_.

## Acceptance Criteria

AC1 and AC2 are the authoritative epic ACs (BDD, source: `_bmad-output/planning-artifacts/epics.md#Story 4.5: Propose New Project`). AC3-AC11 are engineering acceptance criteria derived from the architecture, Stories 4.1-4.4, and the current code so completion is testable.

**AC1 - Proposal preview only after NoMatch.**
**Given** a `NoMatch` resolution result
**When** a proposal is generated
**Then** it includes a suggested Project name and initial setup metadata when available, and **no Project is created from inference until an authorized user action confirms creation**.

**AC2 - Confirmed proposal creates and links context.**
**Given** the user confirms the proposal
**When** the new Project is created
**Then** it is created via `CreateProject` (Epic 1) and links the initiating conversation through the Story 2.2/2.3 reassignment path and links the authorized attachments
**And** the flow fails closed if authorization for the conversation or attachments cannot be established.

**AC3 - Preview is read-style and persists nothing.**
**Given** proposal generation is inference over current evidence
**When** the preview endpoint runs
**Then** it reuses the Story 4.1 `ProjectResolutionEngine` and Story 4.2/4.3 evidence composition, writes no event/projection/state/trace, rejects `Idempotency-Key`, returns `200 ProjectCreationProposal` only for `NoMatch`, and returns a safe problem if an existing Project now qualifies.

**AC4 - Confirmation is explicit and command-async.**
**Given** new Project creation changes durable state
**When** the confirm endpoint is called
**Then** it requires `Idempotency-Key`, `confirmed: true`, `resolutionResult: "NoMatch"`, an explicit canonical `projectId`, an initiating `conversationId`, safe project metadata, and a bounded attachment set; it returns `202 AcceptedCommand` or a mapped RFC 9457/Hexalith problem.

**AC5 - Reuse existing commands and ACLs; do not add local ownership.**
**Given** Projects already has creation, folder, file, and conversation assignment flows
**When** the proposal is confirmed
**Then** the server orchestrates existing surfaces: `CreateProject`, `IProjectConversationAssignmentDirectory.ConfirmResolutionAssignmentAsync(..., expectedSourceProjectId: null)`, `SetProjectFolder`, and `LinkFileReference`; it does not add a `ProjectCreatedFromProposal` command/event, does not use `ProjectResolutionConfirmed`, and does not create local conversation membership.

**AC6 - Preflight authorization before writes.**
**Given** a proposal may include a conversation, a Project Folder, and File References
**When** confirmation runs
**Then** tenant/create authorization, conversation readability/assignability, folder authorization, and file metadata authorization are established before the first write is submitted; failure returns safe-denial `404`, `400 validation_error`, `409 idempotency_conflict`, or retryable `503 read_model_unavailable` as appropriate, without creating a partial Project when preflight evidence is unavailable.

**AC7 - Idempotent recovery across multiple boundaries.**
**Given** confirmation can touch EventStore, Conversations, Folders ACLs, and multiple Project commands
**When** the caller retries with the same root `Idempotency-Key`
**Then** `CreateProject` may replay, conversation assignment may observe already-target as accepted, and folder/file commands use deterministic child idempotency keys so the flow can resume without duplicate events; same root key with different body returns `409 idempotency_conflict`.

**AC8 - No payload leakage.**
**Given** proposals may be derived from conversation and attachment context
**When** responses, command payloads, events, logs, generated clients, and problem bodies are inspected
**Then** they contain only metadata: ids, safe names, setup metadata, reason/result codes, correlation/task/idempotency metadata, and timestamps. They never contain transcripts, message text, file contents, byte ranges, workspace paths, unrestricted paths, prompts, memory bodies, secrets, raw tokens, tenant-authority payloads, or full request bodies.

**AC9 - Contract spine and generated client lockstep.**
**Given** the OpenAPI Contract Spine is the source of truth
**When** proposal preview/confirm operations and proposal DTOs are added
**Then** `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` defines the operations, schemas, examples, idempotency equivalence fields, and canonical error categories; `src/Hexalith.Projects.Client/Generated/*.g.cs` and idempotency helpers are regenerated and never hand-edited; the generated-artifacts fingerprint gate passes.

**AC10 - Tests cover query, mutation, recovery, leakage, and generated artifacts.**
**Given** this story adds a new read-style preview and a composite mutation
**When** validation runs
**Then** Tier-1 pure proposal/mapper tests, Tier-2 endpoint/orchestration tests, contract/client tests, no-payload leakage tests, idempotency/retry tests, and the applicable `docs/checklists/mutation-and-query-negative-tests.md` rows pass under SDK `10.0.300`.

**AC11 - Epic 4 closes cleanly.**
**Given** Story 4.5 is the last Epic 4 implementation story
**When** it reaches review
**Then** the Dev Agent Record lists any remaining Epic 4 caveats, confirms no submodule pointer changes, and leaves `epic-4-retrospective` as the next optional workflow item.

## Tasks / Subtasks

- [x] **Task 1 - Proposal preview contract and DTOs (AC1, AC3, AC8, AC9).**
  - [x] Add a metadata-only `ProjectCreationProposal` model under `src/Hexalith.Projects.Contracts/Models/`. Recommended fields: `resolutionResult`, `suggestedName`, optional `description`, optional `setupMetadata`, `conversationId`, `folderId`, `fileReferenceIds`, `observedAt`, `freshness`, and `warnings`. Do **not** include tenant id, transcript snippets, paths, file contents, scores/ranks, or raw resolution traces.
  - [x] Add a preview operation to the OpenAPI spine. Recommended route: `POST /api/v1/projects/resolution/new-project-proposal` with a closed request body because proposal inputs include multiple optional fields. Treat it as a read-style preview: no `Idempotency-Key`, no `202`, no EventStore submit.
  - [x] Add `ProjectCreationProposalRequest` with closed, metadata-only fields: `requestSchemaVersion: "v1"`, `conversationId`, optional `folderId`, optional bounded file references, optional caller-provided safe `suggestedName`, optional `description`, and optional `setupMetadata`.
  - [x] Define max counts up front: at most one `folderId` for the v1 Project Folder, at most 32 file references, no duplicate ids, Ordinal ordering for deterministic validation and fingerprinting.
  - [x] Add safe examples only. Example strings must be synthetic and must pass the existing forbidden payload/path/token checks.

- [x] **Task 2 - NoMatch proof for preview (AC1, AC3, AC6, AC8).**
  - [x] Reuse the Story 4.1 `ProjectResolutionEngine`; do not duplicate scoring, ranks, thresholds, archived filtering, or reason-code decisions.
  - [x] Compose conversation evidence by reusing the Story 4.2 path: `IProjectConversationResolutionDirectory.ReadConversationMetadataAsync(...)`, `IProjectListReadModel.ListAsync(...)`, `ProjectQueryTenantFilter.FilterList(...)`, and `ConversationResolutionEvidenceMapper.Map(...)`.
  - [x] Compose attachment evidence by reusing the Story 4.3 path: `IProjectReferenceIndexReadModel.ListByReferenceAsync(...)`, `ProjectReferenceIndexReadModelMapper`, and `AttachmentResolutionEvidenceMapper.Map(...)`.
  - [x] Combine evidence into one `ProjectResolutionContext` with server-derived `AuthoritativeTenantId == RequestedTenantId`, `IncludeArchived == false` by default, `Now = timeProvider.GetUtcNow()`, and metadata-only `PresentedInputIds`.
  - [x] Return a proposal only if the combined engine result is `NoMatch`. If a `SingleCandidate` or `MultipleCandidates` now qualifies, return a safe conflict/validation response that tells the client to resolve/confirm an existing Project rather than create a new one. Do not leak hidden candidates.
  - [x] Proposal name derivation order: caller-provided safe suggested name, then safe conversation display label, then safe folder/file display label if available, then a deterministic fallback such as `New project`. Every free-form value must pass the existing `ProjectCommandValidator` safety rules before it appears in the response.

- [x] **Task 3 - Confirm request contract and child idempotency design (AC2, AC4, AC7, AC9).**
  - [x] Add `ConfirmNewProjectProposalRequest` to the OpenAPI spine. Required fields: `requestSchemaVersion`, `operation: "confirmNewProjectProposal"`, `confirmed: true`, `resolutionResult: "NoMatch"`, `projectId`, `conversationId`, and project metadata with `displayName`. Optional fields: `description`, `setupMetadata`, `folderId`, `folderMetadata`, and file reference items.
  - [x] Require the caller to supply `projectId` on confirm. Do **not** server-generate a random Project id inside the composite confirm endpoint; retries after partial failure must target the same aggregate id.
  - [x] Define deterministic child idempotency keys derived from the root `Idempotency-Key`, for example `{root}:create`, `{root}:conversation`, `{root}:folder`, `{root}:file:{fileReferenceId}`. Use the derived keys for Project commands submitted after create so they do not collide with the create command's aggregate idempotency ledger.
  - [x] Define `x-hexalith-idempotency-equivalence` for confirm in deterministic order. Include confirmation intent, project id, conversation id, display name, description presence/value, setup metadata presence/value, folder id, sorted file reference ids, and request schema version. Do not include raw path/workspace fields in any persisted Project event fingerprint.
  - [x] Add an idempotency conflict guard at the endpoint/fake-submitter test layer: same root key + different confirm body returns `409` before or during command submission, matching the Story 4.4 same-key/different-body test.

- [x] **Task 4 - Preflight all authorization before writes (AC2, AC6, AC8).**
  - [x] Read/validate the mutation envelope first: missing/malformed `Idempotency-Key` returns `400 validation_error` with field `idempotency_key`.
  - [x] Validate closed JSON and route/body identities before any sibling write. Malformed/canonical-id failures on mutations return `400 validation_error` with a field name, per the canonical checklist.
  - [x] Authorize project creation with `ProjectAuthorizationGate.AuthorizeCreateAsync(...)`. Tenant and principal come only from `IProjectTenantContextAccessor`; reject client-controlled tenant/principal echoes through the existing gate.
  - [x] Preflight the initiating conversation through the existing Conversations ACL before `CreateProject` is submitted. Use `IProjectConversationResolutionDirectory` or a small shared read helper over the same `GetConversationAsync` semantics; do not call Conversations directly from the endpoint except through the ACL.
  - [x] Preflight a supplied `folderId` with `IProjectFolderDirectory.ValidateSetProjectFolderAsync(new ProjectId(projectId), folderId, correlationId, ct)`.
  - [x] Preflight supplied files with `IProjectFileReferenceDirectory.ValidateLinkFileReferenceAsync(...)` using the same request shape as `LinkFileReference`. `workspaceId` and `filePath` may be accepted only as ACL input and must never be persisted, logged, returned, or copied into events.
  - [x] If any preflight result is denied, stale, unavailable, tenant-mismatched, archived, conflicted, or invalid, fail closed before creating the Project.

- [x] **Task 5 - Composite confirm orchestration (AC2, AC4, AC5, AC7).**
  - [x] Add `ConfirmNewProjectProposalAsync` as a new partial endpoint under `src/Hexalith.Projects.Server/Queries/` to follow the existing endpoint organization. It is a mutation even if the folder is named `Queries`, matching `ConfirmProjectResolutionEndpoint.cs`.
  - [x] Register the route in `ProjectsDomainServiceEndpoints.MapProjectsDomainServiceEndpoints` near the resolution endpoints. Recommended route: `POST /api/v1/projects/proposals/confirm` with `.WithName("ConfirmNewProjectProposal")`.
  - [x] Submit `CreateProject` through `IProjectCommandSubmitter.SubmitCreateProjectAsync(...)` with the confirmed project id, safe display name, description, setup metadata, server-derived principal/tenant, and child create idempotency key. Treat Accepted and IdempotentReplay as success.
  - [x] Link the initiating conversation through `IProjectConversationAssignmentDirectory.ConfirmResolutionAssignmentAsync(targetProjectId, conversationId, expectedSourceProjectId: null, ...)`. Already-target is accepted; unassigned links; unexpected third Project is conflict; denied/unavailable maps safely.
  - [x] If `folderId` is present, submit `SetProjectFolder` with `ReplacementConfirmed = false` and the derived folder key. A new Project has no existing folder, so replacement must not be needed.
  - [x] For each file item, submit `LinkFileReference` with the derived file key after folder preflight succeeds. Link only files whose ACL preflight accepted. Preserve deterministic ordering by file reference id.
  - [x] If a later step fails after earlier writes accepted, return the mapped problem and rely on same-root-key retry to resume. Do not emit a fake success before all intended steps are accepted/replayed.
  - [x] Do not add `ProjectCreatedFromProposal`, do not emit `ProjectResolutionConfirmed`, and do not add local conversation membership state to `ProjectState`.

- [x] **Task 6 - OpenAPI/client generation (AC4, AC9).**
  - [x] Add both proposal operations, request/response schemas, examples, error categories, freshness/correlation metadata, and idempotency metadata to `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`.
  - [x] Regenerate `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` and `HexalithProjectsIdempotencyHelpers.g.cs` through the established generation path or the documented NSwag direct fallback used in Stories 4.3/4.4. Never hand-edit generated files.
  - [x] Extend `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` for both operations, closed schemas, examples, canonical error categories, and idempotency equivalence.
  - [x] Extend `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` for the typed preview method, typed confirm method, confirm idempotency helper, no idempotency helper for preview, and LF/NUL-free generated output.

- [x] **Task 7 - Tests and validation (AC1-AC11).**
  - [x] Tier-1: pure proposal-name/setup derivation tests. Cover caller suggestion, conversation label fallback, attachment label fallback, deterministic fallback, unsafe metadata rejection, duplicate/too-many attachments, and no path/token/transcript leakage.
  - [x] Tier-1: `ProjectCommandValidator` coverage for any new create/setup metadata fields touched by this story. If no event schema changes are made, assert no new event type is required.
  - [x] Tier-2 preview endpoint tests: happy `NoMatch`, `SingleCandidate` conflict, `MultipleCandidates` conflict, unauthorized caller safe-denial, conversation unavailable/denied, reference-index unavailable, `Idempotency-Key` rejected after authorization, freshness validation if the endpoint carries freshness, and response header threading.
  - [x] Tier-2 confirm endpoint tests: accepted full flow, create replay resumes assignment/reference links, assignment already-target recovery, unexpected existing conversation project conflict, folder denied before create, file denied before create, child command unavailable after create then retry succeeds, same root key/different body conflict, missing idempotency key, malformed ids, route/body mismatch where applicable, and no payload leakage in ProblemDetails.
  - [x] Contract/client tests: spine operation ids, schemas, examples, idempotency helper fields, generated-client method shape, and generated-artifacts fingerprint.
  - [x] Leakage tests: proposal DTO, confirm request DTO, ProjectCreated/ProjectCreationRejected existing events under proposal inputs, SetProjectFolder/LinkFileReference events created from proposal inputs, and ProblemDetails bodies contain no transcript/file/path/prompt/memory/token/full-body data.
  - [x] Negative-test checklist in Dev Agent Record: preview applies rows 4, 5, 6, 8 and row 1 for malformed ids; confirm applies rows 1, 2, 3, 6, 7, 8. Mark non-applicable rows explicitly.
  - [x] Validation commands: `/home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx -warnaserror`; `/home/administrator/.dotnet/dotnet test Hexalith.Projects.slnx --no-build`; `git diff --check`. If sandbox restrictions block VSTest sockets, run the executable xUnit lanes and record the limitation exactly.

## Dev Notes

### The one thing to get right

Story 4.5 is the only Epic 4 path that creates a Project after resolution inference, so it must be stricter than a normal create. Preview is inference and persists nothing. Confirm is explicit user action and must prove the current evidence is still `NoMatch` before any durable write. The implementation should reuse existing command and ACL surfaces instead of inventing a proposal aggregate or local conversation ownership.

Do not confuse this story with Story 4.4. `ProjectResolutionConfirmed` records a selected existing Project from `MultipleCandidates`; it is not the event for creating a brand-new Project from `NoMatch`. For 4.5, the durable Project-side events should be the existing `ProjectCreated`, optional `ProjectFolderSet`, and optional `FileReferenceLinked` events. The Conversations assignment remains owned by `Hexalith.Conversations` through `IProjectConversationAssignmentDirectory`.

### Current state to modify

- `CreateProject` already exists and is accepted through `POST /api/v1/projects`. It creates `ProjectCreated` plus `ProjectFolderCreationPending` when no folder is supplied. Its required input is the Project name; `Description` and `SetupMetadata` are optional metadata-only strings.
- `ProjectCommandValidator.Validate(CreateProject)` currently fingerprints only the canonical display name for create equivalence. If confirm uses child create keys, same-key/different-body conflict must be enforced at the composite confirm layer as well as by the aggregate where possible.
- `IProjectCommandSubmitter` already exposes `SubmitCreateProjectAsync`, `SubmitSetProjectFolderAsync`, and `SubmitLinkFileReferenceAsync`. Reuse those methods; do not add a new EventStore bypass.
- `IProjectConversationAssignmentDirectory.ConfirmResolutionAssignmentAsync(...)` already implements the idempotent two-boundary recovery semantics Story 4.4 needed: already-target accepted, unassigned/no-source links, unexpected current Project conflicts.
- `IProjectFolderDirectory.ValidateSetProjectFolderAsync(...)` and `IProjectFileReferenceDirectory.ValidateLinkFileReferenceAsync(...)` are the existing preflight ACLs for folder/file links. Use them before creating the Project so the story does not leave a partial Project when attachment authorization cannot be established.
- `ResolveProjectFromConversationEndpoint.cs` and `ResolveProjectFromAttachmentsEndpoint.cs` are endpoint templates for evidence composition, query conventions, freshness headers, safe-denial, and engine delegation.
- `ProjectResolutionEngine` and `docs/resolution-scoring-heuristic.md` are frozen single sources of truth. Do not add scoring logic to the proposal endpoint.

### Recommended wire shapes

Preview request:

```json
{
  "requestSchemaVersion": "v1",
  "conversationId": "conversation_01HZY7Z6N7J4Q2X8Y9V0A1B2C4",
  "folderId": "folder_01HZY7Z6N7J4Q2X8Y9V0A1B2C7",
  "fileReferenceIds": ["file_01HZY7Z6N7J4Q2X8Y9V0A1B2D1"],
  "suggestedName": "synthetic-project-alpha",
  "description": "synthetic metadata description",
  "setupMetadata": "synthetic-setup-reference"
}
```

Preview response:

```json
{
  "resolutionResult": "NoMatch",
  "suggestedName": "synthetic-project-alpha",
  "description": "synthetic metadata description",
  "setupMetadata": "synthetic-setup-reference",
  "conversationId": "conversation_01HZY7Z6N7J4Q2X8Y9V0A1B2C4",
  "folderId": "folder_01HZY7Z6N7J4Q2X8Y9V0A1B2C7",
  "fileReferenceIds": ["file_01HZY7Z6N7J4Q2X8Y9V0A1B2D1"],
  "observedAt": "2026-05-30T12:00:00Z",
  "freshness": "Fresh",
  "warnings": []
}
```

Confirm request:

```json
{
  "requestSchemaVersion": "v1",
  "operation": "confirmNewProjectProposal",
  "resolutionResult": "NoMatch",
  "confirmed": true,
  "projectId": "project_01HZY7Z6N7J4Q2X8Y9V0A1B2C3",
  "conversationId": "conversation_01HZY7Z6N7J4Q2X8Y9V0A1B2C4",
  "projectMetadata": {
    "displayName": "synthetic-project-alpha",
    "metadataClass": "tenant_sensitive"
  },
  "description": "synthetic metadata description",
  "setupMetadata": "synthetic-setup-reference",
  "folder": {
    "folderId": "folder_01HZY7Z6N7J4Q2X8Y9V0A1B2C7",
    "folderMetadata": {
      "displayName": "synthetic-project-alpha"
    }
  },
  "fileReferences": [
    {
      "fileReferenceId": "file_01HZY7Z6N7J4Q2X8Y9V0A1B2D1",
      "folderId": "folder_01HZY7Z6N7J4Q2X8Y9V0A1B2C7",
      "workspaceId": "workspace_01HZY7Z6N7J4Q2X8Y9V0A1B2D2",
      "filePath": "docs/synthetic-note.md",
      "fileMetadata": {
        "displayName": "synthetic-note.md"
      }
    }
  ]
}
```

The `filePath` and `workspaceId` fields are tolerated only as Folders ACL input because the existing file link ACL requires them. They must not appear in Project events, proposal responses, logs, problem details, or persisted idempotency fingerprints.

### Multi-boundary sequencing

Use a deterministic order so retries are easy to reason about:

1. Read and validate mutation envelope.
2. Parse and validate closed request.
3. Authorize create and preflight every sibling resource.
4. Re-run combined resolution and require `NoMatch`.
5. Submit `CreateProject` with derived key `{root}:create`.
6. Assign the conversation with `ConfirmResolutionAssignmentAsync(... expectedSourceProjectId: null ...)`.
7. Submit `SetProjectFolder` with derived key `{root}:folder` if a folder is present.
8. Submit each `LinkFileReference` with derived key `{root}:file:{fileReferenceId}` in Ordinal id order.
9. Return accepted only after every intended step is accepted or an idempotent replay.

There is no distributed transaction across EventStore and Conversations. The story therefore needs deterministic retry semantics, not pretend atomicity. If step 6, 7, or 8 fails after create accepted, the same root key must be able to resume without creating a second Project or duplicating reference events.

### Scope boundaries and non-goals

- Do not create a persisted proposal store or resolution trace. Persisted trace history is deferred by architecture.
- Do not add a `ProjectCreatedFromProposal` event or a new proposal aggregate unless implementation proves existing events cannot satisfy the story. If a durable proposal-origin marker becomes unavoidable, make it an additive optional metadata-only field on `ProjectCreated`, update the event catalog, and add schema-evolution/leakage tests. Treat that as a design escalation, not the default.
- Do not add a new shared-vocabulary enum member. Use existing `ResolutionResult.NoMatch`, `ProjectReasonCode` values, and `ReferenceState` values.
- Do not call sibling services from domain core. All Conversations/Folders access stays in `Projects.Server` ACLs.
- Do not add MCP/CLI/Web surfaces in this story. Epic 5 owns operator console, MCP, and CLI parity. This story only needs REST contract/client support for Chatbot integration.
- Do not initialize nested submodules or read submodule BMAD folders.

### Project Structure Notes

- New preview/confirm endpoint partials: `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs` or `ConfirmNewProjectProposalEndpoint.cs` as `public static partial class ProjectsDomainServiceEndpoints`.
- Route registration: `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`, near the existing resolution endpoints and `ConfirmProjectResolution`.
- New contract models: `src/Hexalith.Projects.Contracts/Models/ProjectCreationProposal.cs` and any nested request/response wire records if not generated exclusively from OpenAPI.
- OpenAPI source of truth: `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`.
- Generated client/idempotency output: `src/Hexalith.Projects.Client/Generated/*.g.cs`; never hand-edit.
- Pure helpers, if needed: `src/Hexalith.Projects/Resolution/` for proposal-name derivation or combined evidence composition that stays infrastructure-free. Host composition that touches ACLs/read models belongs in `Server`.
- Tests: `tests/Hexalith.Projects.Tests/Resolution/` or a proposal-specific Tier-1 folder for pure derivation; `tests/Hexalith.Projects.Server.Tests/Queries/` or the existing endpoint test area for preview/confirm; `tests/Hexalith.Projects.Contracts.Tests/OpenApi/`; `tests/Hexalith.Projects.Client.Tests/`.

### Previous Story Intelligence

- Story 4.4 is the direct predecessor and is `done`. It added the trust-bearing `ConfirmProjectResolution` mutation and proved the two-boundary pattern: assignment through Conversations first, Projects EventStore command after assignment acceptance, and same-key retry can recover after partial success. Reuse its `ConfirmResolutionAssignmentAsync` helper and its same-key/different-body endpoint test pattern.
- Story 4.4 review added missing leakage and serialization tests after implementation. Do not repeat that gap: add serialization round-trip and serialized-leakage tests for any new DTO or event-shape change in the first pass.
- Story 4.3 review found a real projection regression where a projection threw on newer event types. If this story changes `ProjectCreated`, `ProjectFolderSet`, `FileReferenceLinked`, or projection update behavior, update every projection/rebuild path intentionally and add regression tests.
- Story 4.3 selected the reverse reference-index read model and added direct tests for it. Use that read model for NoMatch proof from attachments; do not fall back to unbounded per-project fan-out.
- Story 4.2 review caught inaccurate docs around fail-closed behavior. Keep endpoint XML docs and OpenAPI descriptions aligned with actual behavior: unauthorized caller/unverifiable tenant/malformed mutation input maps safely; authorized NoMatch is a valid business result and feeds proposal.
- Stories 4.1-4.3 freeze the engine/scoring wire model. Proposal code must call the engine; it must not create a second scoring heuristic.

### Git Intelligence

- `935caf5 feat(story-4.4): Confirm Ambiguous Project` - confirms the latest mutation/orchestration pattern, generated client/idempotency helpers, projection tolerance for new events, and full test lane at 1195/1195.
- `1f7d619 feat(story-4.3): Resolve Project From Attachments` - adds the reverse reference-index read model and attachment evidence mapper used by proposal NoMatch proof.
- `08aa616 feat(story-4.2): Resolve project from conversation` - adds conversation resolution evidence composition and the read endpoint conventions to reuse.
- `517cc2b feat(story-4.1): Resolution engine (compute-on-demand)` - owns scoring, ranking, fail-closed candidate filtering, and the no-persistence rule.

### Latest Technical Information

- No external package or framework research is required for this story. The project pins SDK `10.0.300`, .NET `net10.0`, Dapr/Aspire versions, xUnit generation, Fluent UI RC, and NSwag generation locally in project files and `_bmad-output/project-context.md`.
- Do not upgrade packages, change central package versions, or alter generated-client tooling as part of Story 4.5.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.5: Propose New Project`] - authoritative user story and ACs.
- [Source: `_bmad-output/planning-artifacts/epics.md#Project Resolution`] - FR-15 and Epic 4 correctness-over-automation posture.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`] - EventStore write authority, Pattern A Conversations ownership, compute-on-demand resolution, OpenAPI spine.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`] - idempotency, safe-denial, no silent attach/create, metadata-only logging, negative-test checklist.
- [Source: `_bmad-output/project-context.md`] - pinned stack, Dapr-only infrastructure, tenant isolation, generated-artifact and testing rules.
- [Source: `docs/resolution-scoring-heuristic.md`] - engine owns scoring/outcome and persists nothing.
- [Source: `docs/checklists/mutation-and-query-negative-tests.md`] - canonical query/mutation negative-test rows.
- [Source: `docs/payload-taxonomy.md`] - metadata-only allowlist and forbidden content taxonomy.
- [Source: `src/Hexalith.Projects.Contracts/Commands/CreateProject.cs`] - existing create command shape.
- [Source: `src/Hexalith.Projects.Contracts/Events/ProjectCreated.cs`] - existing metadata-only create event.
- [Source: `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs`] - create, setup, archive, and folder command handler patterns.
- [Source: `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs`] - safety validation and idempotency fingerprint rules.
- [Source: `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`] - route mapping, mutation envelope, safe problem helpers, create endpoint, and existing resolution routes.
- [Source: `src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs`] - conversation evidence and query conventions.
- [Source: `src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs`] - attachment evidence and reverse reference-index query conventions.
- [Source: `src/Hexalith.Projects.Server/Queries/ConfirmProjectResolutionEndpoint.cs`] - mutation envelope, explicit confirmation, and assignment-before-command pattern.
- [Source: `src/Hexalith.Projects.Server/Conversations/IProjectConversationAssignmentDirectory.cs`] - idempotent confirmation assignment method to reuse.
- [Source: `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs`] - existing folder preflight ACL.
- [Source: `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs`] - existing file preflight ACL and path-use boundary.
- [Source: `src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs`] - existing command submitter methods to orchestrate.
- [Source: `_bmad-output/implementation-artifacts/4-4-confirm-ambiguous-project.md#Senior Developer Review (AI)`] - latest mutation review lessons and validation counts.
- [Source: `_bmad-output/implementation-artifacts/4-3-resolve-project-from-attachments.md#Senior Developer Review (AI)`] - reverse read-model and projection regression lessons.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex via `bmad-dev-story`.

### Debug Log References

- Read `.agents/skills/bmad-dev-story/SKILL.md` and `.agents/skills/bmad-dev-story/checklist.md` before implementation.
- Generated client artifacts with:
  - `/home/administrator/.dotnet/dotnet msbuild src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj /t:GenerateHexalithProjectsClient /v:minimal`
  - `/home/administrator/.dotnet/dotnet msbuild src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj /t:GenerateHexalithProjectsIdempotencyHelpers /v:minimal`
- Validation:
  - `/home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx -warnaserror -m:1 -nr:false` passed, 0 warnings / 0 errors.
  - `/home/administrator/.dotnet/dotnet test Hexalith.Projects.slnx --no-build -m:1 -nr:false` blocked by sandbox TCP listener creation: `System.Net.Sockets.SocketException (13): Permission denied` from VSTest `SocketServer.Start`.
  - Executable xUnit fallback lanes passed: `Hexalith.Projects.Tests` 559/559, `Hexalith.Projects.Contracts.Tests` 137/137, `Hexalith.Projects.Client.Tests` 51/51, `Hexalith.Projects.Integration.Tests` 14/14, and Story 4.5 direct server endpoint class `ProposeNewProjectEndpointTests` 19/19.
  - Full `Hexalith.Projects.Server.Tests` executable lane remains sandbox-blocked because existing tests start Kestrel and socket creation is denied; Story 4.5 endpoint tests use direct handler invocation and pass.
  - `git diff --check` passed.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-30.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added metadata-only proposal preview and explicit confirm endpoints, both routed through the OpenAPI spine and generated client.
- Preview reuses the existing resolution engine plus Story 4.2 conversation and Story 4.3 attachment evidence, rejects `Idempotency-Key`, fails closed on degraded conversation evidence, and returns a proposal only for `NoMatch`.
- Confirm preflights create authorization, conversation readability, folder ACL, and file ACL before the first Project write; then orchestrates `CreateProject`, `ConfirmResolutionAssignmentAsync`, `SetProjectFolder`, and `LinkFileReference` with deterministic child idempotency keys.
- Added a root confirm idempotency ledger so same root key with a different body returns `409` before duplicate submission.
- No new Project event, proposal aggregate, local conversation membership state, package upgrade, nested submodule initialization, or submodule BMAD read was introduced. No submodule pointer change was introduced by this story; `Hexalith.Folders` was already modified in the working tree before implementation and was left untouched.
- Negative-test checklist: preview covers row 1 malformed ids, row 4 unauthorized/missing authority, row 5 read-model/reference-index unavailable, row 6 freshness validation, and row 8 idempotency-key rejection; rows 2/3/7 are mutation-only for preview. Confirm covers row 1 malformed ids/body, row 2 missing/malformed `Idempotency-Key`, row 3 same-key/different-body conflict, row 6 command/reference unavailable retry behavior, row 7 folder/file preflight denial before create, and row 8 payload-leakage assertions; route/body mismatch is non-applicable because the confirm route has no identity path parameter.

### File List

- `_bmad-output/implementation-artifacts/4-5-propose-new-project.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Projects.Contracts/Models/ProjectCreationProposal.cs`
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs`
- `src/Hexalith.Projects/Resolution/ProjectCreationProposalBuilder.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `src/Hexalith.Projects.Server/Proposals/IProjectProposalConfirmationIdempotencyLedger.cs`
- `src/Hexalith.Projects.Server/Proposals/InMemoryProjectProposalConfirmationIdempotencyLedger.cs`
- `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectCreationProposalBuilderTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs`
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs`

## Change Log

- 2026-05-30: Implemented Story 4.5 proposal preview and confirm workflow, regenerated client artifacts, added pure/contract/client/server coverage, and moved story to review.
