# Story 2.3: Link & Move Conversation (write-side)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to link an existing conversation to a Project, move it between Projects on explicit confirmation, and unlink it**,
so that **a project's conversation membership reflects the user's intent while preserving single-project membership**.

This is the Projects write-side orchestration story for FR-6, FR-7, and the conversation portion of FR-11. It depends on Story 2.2: the durable membership write authority is `Hexalith.Conversations` through `ReassignConversationProjectCommand` / `ConversationProjectChanged`. Projects must authorize intent, expose a Projects-shaped API/client contract, call Conversations through a Projects-owned ACL, and keep the response and diagnostics metadata-only.

## Acceptance Criteria

1. **Projects write surface.** The Projects API exposes command-async mutation contracts for link, move, and unlink conversation membership under the Project resource. Mutations require `Idempotency-Key`, carry `X-Correlation-Id` and `X-Hexalith-Task-Id`, return the existing `AcceptedCommand` envelope on accepted upstream dispatch, and map validation/idempotency/denial/unavailable outcomes to the established safe ProblemDetails responses.
2. **Link existing conversation.** Given an authorized active Project and an authorized existing Conversation with no different current Project assignment, when Chatbot links the Conversation to the Project, Projects dispatches `ReassignConversationProjectCommand` with `ConversationProjectAssignmentOperation.Assign` and the target Project id. The operation records only stable identifiers and metadata, never transcript content.
3. **Single-project membership.** Given the Conversation is already assigned to the same Project, an equivalent link request is handled as an idempotent replay or no-op success. Given it is assigned to a different Project and the caller did not submit explicit move confirmation, the request fails with a safe conflict/validation outcome and creates no second membership.
4. **Move with explicit confirmation.** Given a Conversation currently belongs to another Project, when a move is requested with explicit confirmation and an expected source Project id, Projects authorizes both the source and target Project scopes, then dispatches the upstream assign operation with `ExpectedCurrentProjectId` set to the source. The upstream change atomically replaces the current assignment; no Projects aggregate list is updated.
5. **Optimistic guard for concurrency.** Move and unlink use Story 2.2's `ExpectedCurrentProjectId` guard so a stale UI or concurrent reassignment cannot overwrite a newer assignment. A guard mismatch surfaces as a safe conflict or unavailable outcome without disclosing the hidden current Project.
6. **Unlink conversation.** Given an authorized unlink request for a Conversation assigned to the target Project, Projects dispatches `ReassignConversationProjectCommand` with `ConversationProjectAssignmentOperation.Clear` and `ExpectedCurrentProjectId` equal to the target Project. The underlying Conversation is not deleted, and subsequent Project Context/list reads no longer include it once Conversations projections catch up.
7. **Tenant isolation and authorization.** Projects enforces the existing layered Project authorization gate before any upstream call, and Conversations enforces its own tenant access before mutation. Missing/stale/unavailable/unauthorized Project or Conversation evidence fails closed. Client-controlled tenant, principal, or Project ids never become authority.
8. **Auditability without payload duplication.** The accepted upstream command and resulting `ConversationProjectChanged` event carry tenant, conversation id, previous/current Project ids, actor, correlation, causation/idempotency, and timestamp metadata sufficient for the later `ProjectAuditTimelineProjection` to audit link/move/unlink. Story 2.3 does not persist transcript content, setup bodies, or sibling payloads.
9. **OpenAPI and generated client stay current.** The Contract Spine includes the new mutation operations and closed request schemas. The generated client and generated idempotency helpers are regenerated from the spine, never hand-edited, and the OpenAPI fingerprint/contract-spine gates pass.
10. **No local conversation collection.** `ProjectAggregate`, `ProjectState`, `ProjectListProjection`, and `ProjectDetailProjection` do not store an unbounded `ConversationId` list. Pattern A remains authoritative: membership is read by querying Conversations by `ProjectId` through `IProjectConversationDirectory`.
11. **Tests.** Tier-1/Tier-2 tests cover link, same-project replay, link-to-other-project without confirmation, confirmed move, expected-current mismatch, unlink, route/body identity mismatch, tenant/project authorization denial, upstream Conversations denial/unavailable mapping, idempotency duplicate/conflict behavior, no-payload leakage, OpenAPI/client generation, and existing Story 2.1 conversation read behavior after reassignment/clear.

## Tasks / Subtasks

- [x] **Task 1 - Freeze ownership boundary before coding** (AC: 2, 4, 6, 8, 10)
  - [x] Read Story 2.2 completion notes and `Hexalith.Conversations/docs/adrs/0002-conversation-project-assignment-ownership.md`.
  - [x] Confirm the write authority stays in `Hexalith.Conversations`; do not add `ConversationId[]` or equivalent mutable membership to `ProjectAggregate` or Project projections.
  - [x] Define the Projects-side operation shape as application orchestration over the Conversations typed client. Only add Projects success events if a concrete project-side invariant is identified and documented before implementation.

- [x] **Task 2 - Add Projects contract/API spine for conversation mutations** (AC: 1, 3, 4, 5, 6, 9)
  - [x] Extend `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` with command-async operations for link, move, and unlink under `/api/v1/projects/{projectId}/conversations...`.
  - [x] Use closed, camelCase request schemas with `requestSchemaVersion: "v1"`, `conversationId`, explicit operation/confirmation fields, and optional `expectedCurrentProjectId` where needed.
  - [x] Declare idempotency-equivalence fields in ordinal order, for example conversation id, expected current Project id, move confirmation, operation, request schema version, and target Project id.
  - [x] Regenerate the Projects client from the Contract Spine (`dotnet build src/Hexalith.Projects.Client` or the local generator target). Do not hand-edit `Generated/*.g.cs`.
  - [x] Update contract-spine tests to assert the new operations are mutations with `AcceptedCommand`, required idempotency, safe-denial, and no query idempotency leakage.

- [x] **Task 3 - Add Projects conversation write ACL** (AC: 2, 4, 5, 6, 7, 8)
  - [x] Extend `IProjectConversationDirectory` or add a sibling `IProjectConversationAssignmentDirectory` in `src/Hexalith.Projects.Server/Conversations/` for `LinkAsync`, `MoveAsync`, and `UnlinkAsync`.
  - [x] Implement the adapter over `IConversationClient.ReassignConversationProjectAsync(...)` using `ReassignConversationProjectCommand`.
  - [x] Build `ConversationCommandMetadata` from authenticated Projects context only: authoritative tenant, stable actor Party id/principal-derived value, correlation id, causation id/task id, and idempotency key. Do not accept tenant/actor authority from request body, query string, or client-controlled headers.
  - [x] Use `ConversationProjectAssignment(Assign, new ProjectId(projectId.Value))` for link/move and `ConversationProjectAssignment(Clear)` for unlink.
  - [x] Pass `ExpectedCurrentProjectId` for confirmed moves and unlink. For simple link, use a guard only after reading the current assignment if the operation needs to distinguish unassigned/same/different assignment.
  - [x] Map upstream `Unauthorized`, `Forbidden`, and `NotFound` to Projects safe-denial/hidden outcomes; map upstream conflicts to idempotency/conflict outcomes; map 5xx/transport/serialization failures to retryable unavailable. Never bubble raw Conversations error text or ProblemDetails bodies.
  - [x] Keep `UnavailableProjectConversationDirectory` or the new write-directory fake fail-closed when the Conversations client is not registered.

- [x] **Task 4 - Authorize Projects-side intent before upstream mutation** (AC: 4, 7)
  - [x] Add explicit action tokens to `ProjectAuthorizationGate`, for example `projects:link_conversation`, `projects:move_conversation`, and `projects:unlink_conversation`, or document reuse of an existing action if the security model intentionally treats them as the same permission.
  - [x] Authorize the target Project detail for link and unlink using the authoritative tenant/principal context.
  - [x] For move, authorize both target Project and source/expected-current Project before calling Conversations. If the source Project is hidden, stale, archived where not allowed, or unavailable, fail closed without revealing whether the Conversation exists.
  - [x] Preserve the existing client-controlled tenant/principal mismatch checks (`X-Tenant-Id`, `tenantId`, `X-Principal-Id`, etc.) before mutation.

- [x] **Task 5 - Wire endpoints and result mapping** (AC: 1, 3, 4, 5, 6, 7)
  - [x] Add endpoint handlers in `ProjectsDomainServiceEndpoints` using the same header parsing, canonical identifier checks, `AcceptedCommand`, `ValidationProblem`, `IdempotencyConflict`, `SafeDenial`, and `ReadModelUnavailable` helpers already used by create/setup/archive.
  - [x] Validate route/body identity equality for `projectId` and `conversationId` before dispatch.
  - [x] Reject missing or malformed `Idempotency-Key`; generate/fallback correlation/task ids only through the established canonical rules.
  - [x] For link-to-other-project without move confirmation, return a safe validation/conflict outcome. Do not dispatch an upstream assign that would silently move the Conversation.
  - [x] Ensure successful upstream acceptance uses the upstream accepted correlation when safe, otherwise the Projects correlation. The body remains the Projects `AcceptedCommand` shape.

- [x] **Task 6 - Preserve read-side Pattern A behavior** (AC: 6, 10, 11)
  - [x] Do not update `ProjectListProjection` or `ProjectDetailProjection` to store conversation membership.
  - [x] Use Story 2.1's `IProjectConversationDirectory.ListForProjectAsync(...)` for read verification after upstream assignment projection catch-up.
  - [x] Add or extend tests that prove the Projects read ACL sees the reassigned Conversation under the new Project, not the old Project, and sees no row after clear, using fakes where possible and Conversations projection tests where needed.

- [x] **Task 7 - Privacy, audit, and documentation hygiene** (AC: 8, 9, 11)
  - [x] Update `docs/event-catalog.md` only if a Projects event is actually added. If no Projects success event is added, document in story completion notes that `ConversationProjectChanged` is the durable audit source for membership changes until Story 5.1 projects it into the Project audit timeline.
  - [x] Extend `NoPayloadLeakage` coverage over new request DTOs, endpoint responses, safe problem extensions, and any new audit/diagnostic metadata.
  - [x] Update adopter-facing docs or README snippets only if required to explain the Projects write surface and explicit move/unlink semantics.
  - [x] Keep logs structured and metadata-only: tenant id, project id, conversation id, operation, reason code, correlation/task/idempotency ids, and upstream trust/outcome category only.

- [x] **Task 8 - Validation lane** (AC: 9, 11)
  - [x] Run targeted Projects contracts/client/server tests first.
  - [x] Run `tests/tools/run-contract-spine-gates.ps1 -NoRestore` after regeneration when restore has already happened, otherwise without `-NoRestore`.
  - [x] Run `dotnet test tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --no-restore`.
  - [x] Run `dotnet test tests/Hexalith.Projects.Client.Tests/Hexalith.Projects.Client.Tests.csproj --no-restore`.
  - [x] Run `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --no-restore`.
  - [x] Run the narrow Conversations test lane only if the implementation uncovers an upstream regression; do not modify Story 2.2 capability as part of this story unless it is strictly required and separately documented.
  - [x] Run `git diff --check`. CRLF conversion warnings are acceptable only if they match existing repository behavior and no content whitespace errors are reported.

## Dev Notes

### Current Implementation Facts

- Story 2.1 added the read-side ACL: `IProjectConversationDirectory.ListForProjectAsync(ProjectId, TenantId, CallerPrincipalId, PageRequest, ct)` in `src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs`. The adapter queries Conversations through `IConversationClient.ListConversationsAsync(...)` with `ConversationListFilterV1(ProjectId: ...)` and translates results to `ProjectConversationsPage`.
- Story 2.1 review fixed scope-poisoning: if upstream Conversations returns a row from another tenant or Project, the Projects page closes the entire result as empty/unavailable rather than returning partial data. Keep that fail-closed posture for write outcomes.
- Story 2.2 added upstream public contracts: `ReassignConversationProjectCommand`, `ConversationProjectAssignment`, `ConversationProjectAssignmentOperation.Assign`, `ConversationProjectAssignmentOperation.Clear`, and `ConversationProjectChanged`. It also added `IConversationClient.ReassignConversationProjectAsync(...)` and `POST /api/v1/conversations/{conversationId}/project`.
- Story 2.2 implemented projection behavior so `ConversationListFilterV1(ProjectId: ...)` reflects assign, reassign, and explicit clear after projection catch-up. Projects should rely on that owner behavior, not patch around it locally.
- Current Projects API has create/setup/archive command-async endpoint patterns in `ProjectsDomainServiceEndpoints`, `IProjectCommandSubmitter`, and `EventStoreProjectCommandSubmitter`. Conversation membership writes should reuse the safe HTTP response helpers but should not automatically go through the Projects EventStore aggregate unless a Project-owned invariant is being persisted.
- Current Projects OpenAPI spine does not yet include the `GET /api/v1/projects/{projectId}/conversations` endpoint added by Story 2.1, nor any conversation mutation operations. If the dev agent updates the conversation API group, prefer aligning the read endpoint in the spine at the same time to prevent further generated-client drift.

### Architecture Guardrails

- Pattern A is still the default: conversation membership lives in `Hexalith.Conversations`; Projects derives membership by querying Conversations by `ProjectId`.
- Do not store an unbounded `ConversationId` collection in `ProjectAggregate`, `ProjectState`, `ProjectListProjection`, or `ProjectDetailProjection`.
- Do not call `IConversationClient` from aggregate, projection, CLI, MCP, or UI logic. All upstream access belongs in the Projects-owned server ACL.
- Keep public Projects contracts additive and serialization-tolerant. Do not introduce `V2` command/event/schema types.
- Edit the Contract Spine and run generation; never hand-edit generated `.g.cs`.
- Reuse the shared vocabulary (`ReferenceState`, `ProjectReasonCode`) for outcomes and diagnostics. Do not add parallel string enums for link/move/unlink state.
- Tenant authority comes from authenticated claims and EventStore claim-transform evidence only. Request bodies can contain target identifiers, but never tenant or actor authority.
- Events, DTOs, logs, audit metadata, and diagnostics are metadata-only. Forbidden: conversation transcript/message text, prompts, file contents, memory bodies, raw upstream problem bodies, tokens, claims, EventStore stream names, Dapr internals, raw exception text, local paths, and full command bodies.

### Suggested API Shape

Use the smallest shape that keeps link, move, and unlink unambiguous:

- `POST /api/v1/projects/{projectId}/conversations/{conversationId}/link`
  - Body: `requestSchemaVersion`, optional `expectedCurrentProjectId`.
  - Semantics: assign to `{projectId}` only when unassigned or already same, unless a guard explicitly proves the current assignment.
- `POST /api/v1/projects/{projectId}/conversations/{conversationId}/move`
  - Body: `requestSchemaVersion`, `sourceProjectId`, `confirmed: true`.
  - Semantics: authorize source and target; dispatch assign to target with `ExpectedCurrentProjectId = sourceProjectId`.
- `DELETE /api/v1/projects/{projectId}/conversations/{conversationId}`
  - Body or queryless command body: `requestSchemaVersion`, `unlinkIntent: "clear"`.
  - Semantics: dispatch clear with `ExpectedCurrentProjectId = projectId`.

If the team prefers one endpoint with an operation discriminator, keep the same guarantees: explicit operation, explicit move confirmation, explicit clear intent, expected-current guard for move/unlink, and idempotency fields sorted in the spine.

### Actor/Party Mapping Warning

`ReassignConversationProjectCommand` requires `ConversationCommandMetadata.ActorPartyId`. Projects currently exposes `IProjectTenantContextAccessor.PrincipalId`, not a dedicated Party id. Before coding, inspect the active authentication/claims convention. It is acceptable only if a stable authenticated Party id claim already exists or if the existing principal id is explicitly the Party id for this integration. Do not accept `actorPartyId` from the request body. If there is no stable server-derived Party id source, HALT for architecture direction rather than inventing a new trust boundary.

### Actor/Party Mapping Decision

The prior HALT is resolved by the 2026-05-26 architecture decision: Projects derives the Conversations `ActorPartyId` server-side through `IActorPartyResolver`, using authenticated `tenantId` + `principalId` only. The first implementation uses `DeterministicActorPartyResolver`, a documented non-reversible integration mapping (`projects-actor-<sha256-prefix>`) encapsulated behind the resolver so a future Parties-backed lookup can replace it without changing endpoint payloads. Public request schemas and endpoint bodies do not accept `tenantId`, `principalId`, or `actorPartyId`, and endpoint tests prove a supplied `actorPartyId` is rejected before dispatch.

### Files To Read Before Editing

- `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md`
- `_bmad-output/implementation-artifacts/2-2-conversation-project-reassignment-upstream-capability.md`
- `Hexalith.Conversations/docs/adrs/0002-conversation-project-assignment-ownership.md`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ReassignConversationProjectCommand.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ConversationProjectAssignment.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ConversationProjectAssignmentOperation.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/ConversationClient.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/UnavailableProjectConversationDirectory.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`
- `src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs`
- `tests/Hexalith.Projects.Server.Tests/Conversations/*`
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs`

### Likely Files To Add / Update

- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` (generated only)
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` (generated only)
- `src/Hexalith.Projects.Server/Conversations/IProjectConversationAssignmentDirectory.cs` or extension of `IProjectConversationDirectory`
- `src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationAssignmentDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/UnavailableProjectConversationAssignmentDirectory.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` (file path is `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` in the repo)
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Conversations/*Assignment*Tests.cs`
- `tests/Hexalith.Projects.Server.Tests/*Conversation*EndpointTests.cs`
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`

### Testing Requirements

- Prefer Tier-1/Tier-2 tests with fakes. Do not require Dapr, Aspire, containers, browser, or live Conversations service for primary story coverage.
- Endpoint tests must prove no upstream call is made when Project authorization fails, route/body ids mismatch, idempotency is missing, move confirmation is absent, or expected source Project is hidden.
- ACL tests must prove exact upstream command shape for assign, move with expected source, and clear with expected current Project.
- Mapping tests must cover upstream success, idempotent replay, 400 validation, 401/403/404 hidden denial, 409 conflict, 5xx unavailable, thrown `HttpRequestException`, and cancellation passthrough.
- Contract/client tests must prove generated methods exist, query endpoints do not accept idempotency keys, mutations expose `ComputeIdempotencyHash()`, and helper hashes match the spine field list.
- Privacy tests must scan new request/response/result/problem/audit metadata for forbidden payload terms through `NoPayloadLeakageAssertions`.

### Previous Story Intelligence

- Story 2.1 chose A1 and added `IConversationClient.ListConversationsAsync(...)`. Keep using the typed client rather than adding a raw HTTP path in Projects.
- Story 2.1's safe read endpoint defaults to `http://conversations` only when a runtime host has not already registered `IConversationClient`; keep the same DI override behavior.
- Story 2.1 review fixed the `IProjectConversationDirectory` registration to be transient because typed `HttpClient` clients must not be captured by a singleton. Any new assignment directory over `IConversationClient` should also be transient.
- Story 2.2 review fixed malformed body identity validation and tenant-mismatched loaded-state regression coverage. Story 2.3 must repeat the route/body identity and tenant-mismatch negative tests at the Projects boundary.
- Story 2.2 confirmed explicit clear/unassignment is supported. Do not encode unlink by omitting the target Project id; use `ConversationProjectAssignmentOperation.Clear`.
- The full `Hexalith.Conversations.slnx` still has unrelated Admin.Web/Conformance analyzer debt. Do not treat that as a Story 2.3 blocker if all story-scoped Projects and affected Conversations lanes pass.

### Out Of Scope

- Pattern B local `ProjectConversationsView` projection.
- Persisting conversation membership in Projects aggregate or projections.
- Full `ProjectContext` assembly policy, allowlist matrix, and explanation surfaces (Epic 3).
- Project resolution, ambiguous confirmation, and new Project proposal flows (Epic 4), except that their later link path should be able to call this story's API.
- Folder, file, and memory link/unlink implementation.
- Web/MCP/CLI operational console surfaces and full audit timeline projection (Epic 5).
- Keycloak/OIDC real E2E; synthetic auth and local fakes are enough for this story unless an existing test lane already covers more.
- Recursive submodule initialization or nested submodule updates.

### Developer HALT Conditions

- No stable server-derived Party id or approved principal-to-Party mapping exists for `ConversationCommandMetadata.ActorPartyId`.
- Product/architecture wants Projects to persist a local conversation membership list, which conflicts with Pattern A and the Story 2.2 ADR.
- Move cannot authorize both source and target Project scopes before dispatching the upstream command.
- Conversations `ExpectedCurrentProjectId` guard cannot be used for move/unlink, leaving a stale UI able to overwrite a newer assignment.
- Implementing the story requires a breaking Conversations contract, a `V2` command/event, or changes to generated client files by hand.
- The work would require initializing or updating nested submodules recursively.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.3: Link & Move Conversation (write-side)`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-6: Link Conversation`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-7: Move Conversation Between Projects`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-11: Unlink Context Reference`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Decision Priority Analysis`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`]
- [Source: `_bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md#Pattern A - Query by back-reference (recommended default)`]
- [Source: `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md`]
- [Source: `_bmad-output/implementation-artifacts/2-2-conversation-project-reassignment-upstream-capability.md`]
- [Source: `Hexalith.Conversations/docs/adrs/0002-conversation-project-assignment-ownership.md`]
- [Source: `Hexalith.Conversations/README.md#Project assignment changes are owned by Conversations`]
- [Source: `docs/payload-taxonomy.md`]
- [Source: `docs/event-catalog.md`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-26: Resolved `bmad-dev-story` workflow customization and loaded root, Conversations, and Parties project context.
- 2026-05-26: Marked Story 2.3 and sprint status as `in-progress` per dev-story workflow.
- 2026-05-26: Read Story 2.2 completion notes and `Hexalith.Conversations/docs/adrs/0002-conversation-project-assignment-ownership.md`; confirmed Conversations owns conversation-to-project assignment and Projects must not persist mutable conversation membership.
- 2026-05-26: Inspected Projects authenticated context and claims transformation (`IProjectTenantContextAccessor`, `HttpContextProjectTenantContextAccessor`, `ProjectsClaimsTransformation`) before coding the reassignment call.
- 2026-05-26: HALT: no stable server-derived Party id claim or approved principal-to-Party mapping was found for `ConversationCommandMetadata.ActorPartyId`.
- 2026-05-26: Resumed after architecture decision resolving HALT; added server-owned `IActorPartyResolver` and documented deterministic integration mapping from authenticated tenant/principal to Conversations `PartyId`.
- 2026-05-26: Added Project conversation link/move/unlink OpenAPI operations, closed request schemas, regenerated NSwag client and idempotency helpers, and extended contract/client generation tests.
- 2026-05-26: Added `IProjectConversationAssignmentDirectory` over `IConversationClient.ReassignConversationProjectAsync(...)`, fail-closed unavailable directory, explicit authorization gate actions, and endpoint handlers/result mapping.
- 2026-05-26: Added tests for server-derived actor party metadata, rejection of request-supplied `actorPartyId`, explicit source/target move authorization, unlink validation, upstream outcome mapping, Pattern A read behavior after assign/move/clear, and metadata-only conversation reference leakage coverage.
- 2026-05-26: Validation lane passed; story moved to `review`.
- 2026-05-26: QA automation pass added focused endpoint and write-ACL coverage for explicit move confirmation, unlink dispatch, expected-current conflict mapping, reassignment failure mapping, and actor Party resolver fail-closed behavior; story and sprint status remained `review`.
- 2026-05-26: Senior review auto-fixed upstream accepted idempotency-key verification and added negative endpoint coverage for authority-header mismatch, missing idempotency, and route/body identity mismatch; story and sprint status moved to `done`.

### Completion Notes List

- Task 1 complete: ownership boundary remains Pattern A. Projects-side link/move/unlink should be orchestration over `IConversationClient.ReassignConversationProjectAsync(...)`; no Project aggregate event or local membership list is justified before the hard gate is resolved.
- HALT condition triggered before Task 2/code changes: Projects currently exposes authenticated tenant and principal context only. `HttpContextProjectTenantContextAccessor.PrincipalId` comes from `ClaimTypes.NameIdentifier`/`sub`, and the active claims transformation normalizes tenant and permission evidence, not a stable Party id. No local artifact explicitly approves treating that principal id as a Conversations `PartyId`.
- Required architecture decision: provide a server-derived actor Party id source, or approve and document a principal-to-Party mapping, before Projects can build `ConversationCommandMetadata.ActorPartyId` for reassignment commands.
- HALT resolved by architecture direction: `IActorPartyResolver` maps authenticated `tenantId` + `principalId` to a stable Conversations `PartyId`; the conservative first implementation is `DeterministicActorPartyResolver`, isolated behind the interface and documented as the integration mapping.
- Actor authority remains server-derived command metadata only. OpenAPI request schemas contain no actor/tenant/principal authority fields, endpoint JSON parsing rejects extra `actorPartyId`, and tests prove the assignment directory uses resolver output rather than request body data.
- Implemented Projects write-side link, move, and unlink as orchestration over Conversations reassignment. Link reads the current assignment to reject different-project links without confirmation, move authorizes source and target and sends the expected-current source guard, and unlink sends `Clear` with expected-current target guard.
- No Projects aggregate event or local conversation membership collection was added. `ConversationProjectChanged` remains the durable audit source for membership changes until Story 5.1 projects it into the Project audit timeline.
- The Contract Spine now includes `GET /api/v1/projects/{projectId}/conversations` and link/move/unlink mutations. Generated client and idempotency helpers were regenerated from the spine; only mechanical whitespace cleanup was applied to generated client output so `git diff --check` passes.
- Senior review auto-fix kept the accepted upstream result fail-closed when the upstream idempotency key differs from the Projects request key, and expanded endpoint regression coverage for client-controlled tenant/principal spoofing plus link/move request identity and idempotency validation.

### File List

- `_bmad-output/implementation-artifacts/2-3-link-move-conversation-write-side.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs`
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs`
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`
- `src/Hexalith.Projects.Server/Conversations/IActorPartyResolver.cs`
- `src/Hexalith.Projects.Server/Conversations/DeterministicActorPartyResolver.cs`
- `src/Hexalith.Projects.Server/Conversations/IProjectConversationAssignmentDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationAssignmentDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/ProjectConversationAssignmentResult.cs`
- `src/Hexalith.Projects.Server/Conversations/ProjectConversationCommandMetadata.cs`
- `src/Hexalith.Projects.Server/Conversations/UnavailableProjectConversationAssignmentDirectory.cs`
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Conversations/ConversationsProjectConversationDirectoryTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Conversations/ProjectConversationAssignmentDirectoryTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs`
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`

## Change Log

- 2026-05-26: Story context created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-26: Started non-tmux dev-story workflow, completed ownership-boundary Task 1, and halted before coding because the ActorPartyId trust-boundary gate lacks an approved server-derived Party id source.
- 2026-05-26: Implemented Projects link/move/unlink conversation write-side orchestration over Conversations reassignment, regenerated OpenAPI client artifacts, updated tests/story notes, and moved story to review.
- 2026-05-26: Added focused BMAD QA automation coverage for Story 2.3 and kept story status in review.
- 2026-05-26: Senior review auto-fixed accepted upstream idempotency-key verification, added endpoint negative-path coverage, verified story lanes, and moved story to done.

## Validation Notes

- Target story matched explicit user request and sprint key: `2-3-link-move-conversation-write-side`.
- Source analysis covered the create-story skill, checklist, sprint status, Epic 2 story text, PRD FR-6/FR-7/FR-11, architecture Pattern A/data/process/API rules, technical Conversations reference research, root and Conversations project context, Stories 2.1 and 2.2 completion/review notes, Epic 1 retrospective carry-forward items, recent git history, current Projects command/API/authorization/Contract Spine patterns, and current Conversations reassignment command/client/API contracts.
- Latest external technical lookup was not needed for story creation because this story uses pinned local project versions and existing local APIs; no new external library/API is introduced.
- Validation result: ready-for-dev. The story contains concrete acceptance criteria, scoped tasks, current-code facts, previous-story intelligence, architecture guardrails, likely file locations, testing requirements, out-of-scope boundaries, and developer HALT conditions.
- Implementation validation:
  - `dotnet build src\Hexalith.Projects.Client\Hexalith.Projects.Client.csproj --no-restore` passed.
  - `pwsh -NoProfile -ExecutionPolicy Bypass -File tests\tools\run-contract-spine-gates.ps1 -NoRestore` passed: OpenAPI filtered lane 16/16, Client generation lane 24/24. The same script fails under Windows PowerShell 5.1 because it uses PowerShell 7 `Join-Path` argument behavior; rerun under `pwsh` passed.
  - `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore` passed: 120/120.
  - `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore` passed: 24/24.
  - `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore` passed: 101/101.
  - `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore` passed: 129/129.
  - `git diff --check` passed with CRLF conversion warnings only.
- QA automation validation:
  - `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectConversationAssignment"` passed: 24/24.
  - `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore` passed: 111/111.
  - `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApiContractSpineTests"` passed: 16/16.
  - `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ClientGenerationTests"` passed: 24/24.
  - `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~NoPayloadLeakageTests"` passed: 14/14.
  - `git diff --check` passed with existing CRLF conversion warnings only.
- Senior review validation:
  - `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectConversationAssignment"` passed: 30/30.
  - `pwsh -NoProfile -ExecutionPolicy Bypass -File tests\tools\run-contract-spine-gates.ps1 -NoRestore` passed: OpenAPI filtered lane 16/16, Client generation lane 24/24.
  - `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore` passed: 120/120.
  - `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore` passed: 24/24.
  - `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore` passed: 117/117.
  - `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~NoPayloadLeakageTests"` passed: 14/14.
  - `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore` passed: 129/129.
  - `git diff --check` passed with existing CRLF conversion warnings only.

## Senior Developer Review (AI)

Reviewer: Codex (GPT-5) on 2026-05-26

Outcome: Approved after auto-fix. Story status set to `done`; sprint status synced. No CRITICAL/HIGH/MEDIUM/LOW issues remain.

### Findings After Auto-Fix

- CRITICAL: 0 remaining.
- HIGH: 0 remaining.
- MEDIUM: 0 remaining. Fixed 1 MEDIUM: accepted upstream Conversations reassignment results now fail closed if the accepted idempotency key differs from the Projects request metadata.
- LOW: 0 remaining. Fixed 1 LOW test gap: endpoint regression coverage now proves client-controlled tenant/principal authority mismatches fail closed before the write ACL, missing `Idempotency-Key` is rejected before dispatch, and link/move route/body identity mismatches do not call the write ACL.

### Review Evidence

- Acceptance criteria cross-check: AC1-AC11 verified against the OpenAPI spine, generated client/idempotency helpers, Projects endpoint orchestration, Projects write/read ACLs, authorization gate, Conversations reassignment command shape, Pattern A read behavior, and no-payload leakage tests.
- Task audit: all `[x]` tasks have implementation evidence in the story File List; no Projects aggregate event or mutable conversation membership collection was added.
- Security check: tenant, principal, and actor Party attribution remain server-derived; request bodies are closed and reject `actorPartyId`; client-controlled tenant/principal headers fail closed; upstream denial/unavailable/problem detail bodies are mapped to safe Projects outcomes.
- Git/story check: unrelated working-tree changes under `.agents/`, `.codex/`, `.gitignore`, `Hexalith.EventStore`, `Hexalith.Tenants`, and story-automator outputs were observed and left untouched.
- MCP documentation check: Microsoft Learn ASP.NET Core Minimal APIs / ProblemDetails documentation was reviewed while validating the manual `IResult` safe ProblemDetails shape.

### Verification

- `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectConversationAssignment"` passed: 30/30.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File tests\tools\run-contract-spine-gates.ps1 -NoRestore` passed: OpenAPI filtered lane 16/16, Client generation lane 24/24.
- `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore` passed: 120/120.
- `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore` passed: 24/24.
- `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore` passed: 117/117.
- `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~NoPayloadLeakageTests"` passed: 14/14.
- `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore` passed: 129/129.
- `git diff --check` passed with existing CRLF conversion warnings only.
