# Story 2.2: Conversation project (re)assignment — upstream capability *(enabler / PR-1 / AR-G1)*

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **Projects platform engineer**,
I want **`Hexalith.Conversations` to support setting, changing, and explicitly clearing a conversation's `ProjectId` after creation through an additive command and event**,
so that **the Projects write side can link, move, and unlink conversations without violating Conversations' ownership of the relationship**.

This is an upstream prerequisite story. The implementation belongs in the `Hexalith.Conversations` submodule and must remain a separately tested Conversations change. Do not implement Projects-side link/move/unlink behavior here; that is Story 2.3.

## Acceptance Criteria

1. **Additive public contract.** A new public command and past-tense public event exist in `Hexalith.Conversations.Contracts`, for example `ReassignConversationProjectCommand` and `ConversationProjectChanged`. They are additive, serialization-tolerant, use existing `ConversationCommandMetadata`, `ConversationEventMetadata`, `ConversationId`, and `ProjectId` identifiers, and introduce no `V2` command/event/schema type.
2. **Assignment, reassignment, and explicit clearing.** The command can set a previously unassigned conversation to a `ProjectId`, change from one `ProjectId` to another, and explicitly clear the assignment for conversation unlink. A missing command field must not accidentally mean "clear"; use either an explicit operation/action discriminator or a nullable target with validation that proves the caller intentionally requested unassignment. The event records at least previous project id, current project id, tenant, conversation id, actor, correlation, causation, event id, and timestamp.
3. **Aggregate/state behavior.** `ConversationAggregate.Handle(...)` emits exactly one `ConversationProjectChanged`-style event for a valid state change, updates `ConversationState.ProjectId` through `Apply(...)`, and leaves replay deterministic. Reassigning to the current target is idempotent/no-op or stable duplicate replay, but it must not emit duplicate successful events. Missing/not-created state, tenant/conversation mismatch, unsupported lifecycle, unsupported schema, missing metadata, and invalid target shape produce typed content-safe rejections.
4. **Tenant isolation and authorization.** The server/application boundary checks Conversations tenant access before aggregate load, idempotency lookup, command dispatch, projection read, or duplicate outcome disclosure. Missing, stale, unavailable, unauthorized, cross-tenant, or tenant-mismatched state fails closed and does not reveal whether the conversation exists or which project it currently belongs to.
5. **Idempotency and command availability.** The new command is added to `ConversationCommandType`, idempotency fingerprinting, command outcome mapping, API/client result handling, and command availability metadata where relevant. Equivalent duplicate requests return the same logical outcome; conflicting same-key requests return typed `idempotency_conflict` without mutation or disclosure.
6. **Read model reflects current owner.** Conversations projections and query results apply the new event so `ConversationDetailsV1`, `ConversationSummaryV1`, `ConversationSummaryProjectionV1`, and `ConversationListFilterV1(ProjectId: ...)` reflect the current assignment. After reassignment, the old project's filtered list no longer returns the conversation and the new project's filtered list does. After clearing, project-filtered reads do not return the conversation.
7. **Submodule and ADR discipline.** The ownership decision is recorded in a `Hexalith.Conversations/docs/adrs` ADR before or with the change. The implementation is self-contained in the Conversations submodule, with its own tests. Do not mix Projects source changes, story 2.3 work, or a root submodule-pointer update into the same change.
8. **No payload leakage.** Command/event/result/projection/log/test artifacts include only metadata and stable identifiers. They never include transcript text, prompts, message bodies, Party display/contact data, project setup bodies, file contents, memory payloads, raw upstream problem bodies, tokens, claims, EventStore stream names, storage positions, Dapr internals, or raw exception text.
9. **Validation evidence.** Tests cover contract serialization, aggregate Handle/Apply, state replay, idempotency duplicate/conflict paths, tenant-denial paths, API/client mapping, projection materialization/list filtering, payload/privacy scans, and boundary references. The Conversations solution or the narrowest affected test projects pass, with any pre-existing unrelated failures documented.

## Tasks / Subtasks

- [x] **Task 1 — Freeze scope and ADR** (AC: 1, 2, 7)
  - [x] Create a Conversations ADR from `Hexalith.Conversations/docs/adrs/0000-template.md` covering Pattern A ownership, why Conversations owns the conversation->project link, why Projects must not maintain a separate mutable membership list, and whether explicit clearing/unassignment is included in this story.
  - [x] Confirm the implementation is done in `Hexalith.Conversations` only. Do not edit `src/Hexalith.Projects.*`, do not implement Story 2.3 link/move/unlink commands, and do not update the root submodule pointer in the same code change.
  - [x] Before editing, read the existing command/event/state/idempotency/API/projection files listed in Dev Notes; do not rely on earlier story artifacts alone because Story 2.1 already changed the Conversations client.

- [x] **Task 2 — Add public command/event vocabulary** (AC: 1, 2, 5, 8)
  - [x] Add the public command contract under `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands`. It should carry `ConversationCommandMetadata`, `ConversationId`, an explicit target project assignment shape, optional expected-current project guard if needed for move safety, and optional bounded caller metadata if local command patterns require it.
  - [x] Add the public event contract under `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events` with `ConversationEventMetadata`, `PreviousProjectId`, and `CurrentProjectId`. Keep `ProjectId` values opaque and stable; do not hydrate Project display data into the event.
  - [x] Extend `ConversationCommandType` and `ConversationEventType` known-type dictionaries and JSON converter coverage without changing existing canonical string values.
  - [x] Add contract serialization and compatibility tests proving additive JSON shape, canonical command/event type parsing, null/clear semantics, and absence of forbidden payload terms.

- [x] **Task 3 — Implement pure domain handling and replay** (AC: 2, 3, 8)
  - [x] Add a domain command record under `src/Hexalith.Conversations/Commands` and a domain event record under `src/Hexalith.Conversations/Events` matching existing `CreateConversation` / `ConversationCreatedDomainEvent` style.
  - [x] Add validation under `src/Hexalith.Conversations/Validation`, including schema envelope validation, required conversation identity, explicit target/clear shape, event identity, business timestamp, state existence, tenant/conversation consistency, lifecycle policy, and identity-substitution guards.
  - [x] Extend `ConversationAggregate.Handle(...)` to return `DomainResult.Success`, `DomainResult.Rejection`, or `DomainResult.NoOp` consistently. Keep authorization, tenant access, HTTP, and projection freshness out of aggregate logic.
  - [x] Extend `ConversationState.Apply(...)` for the public and domain event so replay updates `ProjectId` deterministically and rejection events remain no-op.
  - [x] Add aggregate/state replay tests for assign, reassign, clear, same-target duplicate/no-op, not-created rejection, tenant mismatch, conversation mismatch, invalid target, unsupported lifecycle, and payload/privacy safety.

- [x] **Task 4 — Wire server boundary, API, and typed client** (AC: 4, 5, 7, 9)
  - [x] Add or extend a Conversations command handler under `src/Hexalith.Conversations.Server/CommandHandlers` following the tenant-first pattern in `AddParticipantCommandHandler`: cheap public schema check first, trusted tenant requirement, tenant access guard, semantic validation after authorization, state load after authorization, aggregate tenant cross-check, then aggregate dispatch.
  - [x] Wire idempotency after tenant access and before mutation. Extend `ConversationCommandFingerprint`, `ConversationIdempotencyOutcome` mapping, and related tests so the canonical fingerprint includes tenant, conversation scope, command type, idempotency key, schema version, actor, target project/clear operation, and any expected-current guard.
  - [x] Extend `ConversationCommandApi` with a guarded opt-in route, for example `POST /api/v1/conversations/{conversationId}/project`, validating route/body identity equality and returning typed safe results.
  - [x] Extend `IConversationClient` and `ConversationClient` with a thin `ReassignConversationProjectAsync(...)` method using the same typed-result pattern as create/append/list. Do not expose raw `HttpResponseMessage`, raw route internals, EventStore status, or projection topology.
  - [x] Add deterministic fake-transport client tests and server API tests for happy path, route/body mismatch, tenant mismatch, unauthenticated/missing claims, typed denial, idempotency conflict, and unknown outcome mapping.

- [x] **Task 5 — Update projection/read model behavior** (AC: 6, 8, 9)
  - [x] Update `ConversationProjectionAccumulator` and `ConversationProjectionMaterializer` so the new event sets/replaces/clears `ProjectId`, is deduped by event id, rejects tenant/conversation mismatches, and preserves gap/out-of-order/unsupported-version trust behavior.
  - [x] Confirm `ConversationSummaryProjectionV1`, `ConversationDetailProjectionV1`, `ConversationSummaryV1`, and `ConversationDetailsV1` continue to expose the current `ProjectId` only as a stable reference. Add fields only if required by additive compatibility; do not rename existing fields.
  - [x] Add projection tests proving list-by-project moves from old project to new project, explicit clear removes the row from project-filtered lists, duplicate delivery is idempotent, out-of-order delivery downgrades trust instead of lying, and mixed-generation behavior stays fail-closed.
  - [x] Re-run or extend query tests around `ConversationListFilterV1.ProjectId` so Story 2.1's Projects ACL can rely on the current assignment after projection catch-up.

- [x] **Task 6 — Documentation, validation, and release hygiene** (AC: 7, 8, 9)
  - [x] Update ADR index and minimal adopter-facing docs only where needed to document the new command/event capability and route/client method.
  - [x] Add/update tests under `tests/Hexalith.Conversations.Contracts.Tests`, `tests/Hexalith.Conversations.Tests`, `tests/Hexalith.Conversations.Server.Tests`, and `tests/Hexalith.Conversations.Client.Tests` following existing xUnit v3 + Shouldly style.
  - [x] Run the narrowest affected tests first, then `dotnet test Hexalith.Conversations/Hexalith.Conversations.slnx --no-restore` when local dependencies allow. If the full submodule solution still fails for unrelated missing sibling dependency paths, record that evidence and run the affected project tests explicitly.
  - [x] Run `git -C Hexalith.Conversations diff --check`. Confirm no root-level Projects source changes or root submodule-pointer updates are included in this story's code change.

## Senior Developer Review (AI)

Reviewer: Codex (GPT-5) on 2026-05-26

Outcome: Approved after auto-fix. Story status set to `done`; sprint status synced. No CRITICAL issues remain.

### Findings After Auto-Fix

- CRITICAL: 0 remaining.
- HIGH: 0 remaining. Fixed 1 HIGH: the project reassignment API now rejects a malformed body with a missing `ConversationId` as a typed 400 validation error before invoking the handler.
- MEDIUM: 0 remaining. Fixed 2 MEDIUM: added tenant-mismatched loaded-state regression coverage for the reassignment handler, and completed adopter-facing documentation for the command/event/client route plus explicit assign/clear semantics.
- LOW: 0 remaining.

### Review Evidence

- Acceptance criteria cross-check: AC1-AC9 verified against the Conversations contracts, aggregate/state/replay behavior, tenant-first server boundary, API/client path, projection/query materialization, ADR/docs, and privacy-oriented tests.
- Task audit: all `[x]` tasks have implementation evidence in the listed Conversations source/tests; no Projects story 2.3+ source implementation was added.
- Security check: tenant access remains before protected state/idempotency work, route/body identity mismatches and missing body identity fail closed with typed safe errors, and tenant-mismatched loaded state produces no mutation event.
- Git/story check: unrelated working-tree changes under `.agents/`, `.codex/`, `.gitignore`, story-automator artifacts, and root test-summary state were observed and left untouched as outside story 2.2 review scope.
- MCP documentation check: Microsoft Learn System.Text.Json converter/polymorphism documentation was reviewed while validating the closed-vocabulary JSON converter shape.

### Verification

- `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` passed: 587/587.
- `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj --no-restore` passed: 25/25.
- `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-restore` passed: 173/173.
- `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-restore` passed: 515/515.
- `dotnet test tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj --no-restore` passed: 8/8.
- `git diff --check` passed with Git CRLF conversion warnings only.
- `dotnet test Hexalith.Conversations.slnx --no-restore` failed on unrelated pre-existing Admin.Web CA2007 and Conformance CA1822 analyzer issues; story-scoped assemblies in that run still passed.

## Dev Notes

### Story Scope Boundary

This story is a prerequisite for Projects Story 2.3 and Epic 4 FR-15. It closes AR-G1 by adding the upstream Conversations capability only. It must not implement Projects-side `LinkConversation`, `MoveConversation`, `UnlinkConversation`, `ProjectReferenceIndexProjection`, audit timeline entries, resolution confirmation, UI/MCP/CLI surfaces, or FrontComposer descriptors. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.2: Conversation project (re)assignment — upstream capability`; `_bmad-output/planning-artifacts/architecture.md#Gap Analysis Results`]

The current architecture deliberately keeps conversation membership owned by Conversations. Projects reads by Pattern A through `IProjectConversationDirectory` and must not store an unbounded `ConversationId[]` in `ProjectAggregate`. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md#Pattern A — Query by back-reference (recommended default)`]

### Current Code Facts Verified

- `ConversationCreated` and `ConversationCreatedDomainEvent` already carry optional `ProjectId`; `ConversationState.ProjectId` is set during creation replay. There is currently no post-creation project assignment event. [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationCreated.cs`; `Hexalith.Conversations/src/Hexalith.Conversations/Events/ConversationCreatedDomainEvent.cs`; `Hexalith.Conversations/src/Hexalith.Conversations/State/ConversationState.cs`]
- `ConversationMetadataUpdated` updates label, business reference, and attributes only; it must not be stretched into project reassignment. Use a separate past-tense event. [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationMetadataUpdated.cs`; `Hexalith.Conversations/src/Hexalith.Conversations/State/ConversationState.cs`]
- `ConversationCommandApi` currently maps create and append routes only. A new command route must be added explicitly and guarded by authorization. [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`]
- `IConversationClient` and `ConversationClient` already include the Story 2.1 list method. Add the new mutation method to the same thin typed-client surface; do not build a separate raw HTTP adopter path. [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs`; `Hexalith.Conversations/src/Hexalith.Conversations.Client/ConversationClient.cs`; `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md#Completion Notes List`]
- `ConversationProjectionMaterializer` currently projects `ProjectId` from `ConversationCreated` using null-coalescing assignment. A new reassignment event must override or clear that value; otherwise Story 2.1's `ListConversationsAsync(ProjectId: ...)` path will keep serving stale membership. [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`; `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Queries/ConversationListFilterV1.cs`]
- `ConversationProjectionAccumulator` tracks processed event ids and ignores tenant/conversation mismatches. Extend that exact defense-in-depth behavior for the new event. [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionAccumulator.cs`]

### Previous Story Intelligence

Story 2.1 is complete and chose A1: it added `IConversationClient.ListConversationsAsync(...)` and a Projects ACL that filters Conversations by `ProjectId`. That means Story 2.2 must update Conversations write/projection behavior so the read filter remains authoritative after assignment changes; Projects should not patch around stale upstream state. [Source: `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md#Completion Notes List`]

Story 2.1 review fixed scope-poisoned upstream rows so a bad upstream tenant/project row closes the entire Projects page empty/unavailable rather than returning partial data. Carry that posture into 2.2: a suspicious reassignment/projection mismatch must fail closed or downgrade trust, not return a mixed old/new membership result. [Source: `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md#Senior Developer Review (AI)`]

Epic 1 retrospective explicitly calls out that Story 2.2 must remain separately tested and committed in `Hexalith.Conversations`; do not combine it with Projects changes or root submodule-pointer churn. [Source: `_bmad-output/implementation-artifacts/epic-1-retro-2026-05-25.md#Epic 2 Preparation Tasks`]

### Architecture Compliance

- Use the existing Hexalith Conversations stack: .NET 10, nullable enabled, warnings-as-errors, central package management, xUnit v3, Shouldly, NSubstitute, and module-local tests. Do not bump SDK, Dapr, Aspire, Fluent UI, Roslyn, or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Hexalith.Conversations/_bmad-output/project-context.md#Technology Stack & Versions`]
- Keep public contract changes additive. Do not rename or remove `ConversationCreated.ProjectId`, `ConversationSummaryProjectionV1.ProjectId`, `ConversationSummaryV1.ProjectId`, or existing command/event canonical values. [Source: `Hexalith.Conversations/_bmad-output/project-context.md#Critical Implementation Rules`; `_bmad-output/planning-artifacts/architecture.md#Schema evolution`]
- Domain handlers stay pure. No Tenants calls, HTTP context, Dapr, EventStore status lookup, authorization decisions, project hydration, or logging inside `ConversationAggregate` or `ConversationState`. [Source: `Hexalith.Conversations/_bmad-output/project-context.md#Framework-Specific Rules`]
- Server/application boundaries own tenant access, idempotency orchestration, API routing, typed errors, and safe logging. Use `ConversationTenantAccessGuard` and existing command-handler patterns rather than inventing a second authorization pipeline. [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs`; `Hexalith.Conversations/src/Hexalith.Conversations.Server/TenantAccess/IConversationTenantAccessService.cs`]
- Published events and projections are metadata-only. A project assignment event may carry stable project identifiers but must not hydrate project names, setup, conversation transcripts, message text, or upstream payloads. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#5. Non-Goals (Explicit)`; `Hexalith.Conversations/_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### Likely Files To Read / Update

Read these before editing:

- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/UpdateConversationMetadataCommand.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationCreated.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationMetadataUpdated.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Results/ConversationCommandType.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationEventType.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/State/ConversationState.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Idempotency/ConversationCommandFingerprint.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/ConversationClient.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionAccumulator.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`

Likely new/update files:

- `Hexalith.Conversations/docs/adrs/0002-conversation-project-assignment-ownership.md` or next available ADR number.
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ReassignConversationProjectCommand.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationProjectChanged.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Commands/ReassignConversationProject.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Events/ConversationProjectChangedDomainEvent.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Validation/ReassignConversationProjectValidation.cs` and boundary wrapper if following local pattern.
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/ReassignConversationProjectCommandHandler.cs`
- Existing client/API/projection/idempotency files named above.
- Tests under `Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests`, `Hexalith.Conversations/tests/Hexalith.Conversations.Tests`, `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests`, and `Hexalith.Conversations/tests/Hexalith.Conversations.Client.Tests`.

### Testing Requirements

Minimum Tier-1/Tier-2 evidence:

- Contract tests for command/event JSON, known-type parsing, null/clear shape, additive compatibility, safe public vocabulary, and forbidden payload terms.
- Aggregate tests for assign, reassign, clear, same-target/no-op, invalid target, unsupported lifecycle, not-created state, tenant mismatch, conversation mismatch, and deterministic replay.
- Idempotency tests for equivalent duplicate, conflicting duplicate, missing key, same key under different tenant, same key against different conversation, unknown outcome, and no second success event.
- Server boundary tests proving tenant access runs before state load, idempotency lookup, conflict disclosure, or projection access; denied paths must not call protected delegates.
- API/client tests using fake HTTP handlers and opt-in endpoint tests; avoid live Dapr/Aspire/network/browser dependencies.
- Projection/query tests proving current `ProjectId` is visible in detail/summary/list after event replay; old-project/new-project/clear filters behave correctly; duplicate and out-of-order delivery are deterministic or safely downgraded.
- Privacy scans over serialized command/event/result/projection/error/log/test artifacts for transcript, prompt, provider payload, Party personal data, EventStore, Dapr, raw route, local path, token, and claim leakage.

### Out Of Scope

- Any `Hexalith.Projects` source implementation.
- Story 2.3 link/move/unlink Projects commands, events, projections, or APIs.
- Root submodule-pointer update mixed with the upstream Conversations change.
- Pattern B Projects-side local conversation projection.
- Project Resolution, proposal flows, Project Context assembly, audit timeline, CLI/MCP/Web/FrontComposer work.
- Real OIDC/Keycloak E2E; fast synthetic auth/test doubles are sufficient unless a later story requires runtime topology.

### Developer HALT Conditions

Stop and ask for architecture/product direction before coding if any of these occur:

- The team rejects explicit clear/unassignment in this upstream story, because Story 2.3's conversation unlink AC then needs to be revised before implementation.
- Tenant access cannot be enforced before state load/idempotency lookup for the new command without changing shared Conversations authorization architecture.
- The implementation cannot be kept inside a separately tested `Hexalith.Conversations` submodule change.
- The change requires a breaking contract/version change, a `V2` event/command, or a rewrite of existing conversation creation/list contracts.

## References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.2: Conversation project (re)assignment — upstream capability *(enabler / PR-1 / AR-G1)*`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Upstream Prerequisite & Sequencing Stories (cross-module gaps)`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Gap Analysis Results`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-6: Link Conversation`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-7: Move Conversation Between Projects`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-11: Unlink Context Reference`]
- [Source: `_bmad-output/planning-artifacts/research/technical-hexalith-projects-referencing-conversations-research-2026-05-24.md#Pattern A — Query by back-reference (recommended default)`]
- [Source: `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md#Senior Developer Review (AI)`]
- [Source: `_bmad-output/implementation-artifacts/epic-1-retro-2026-05-25.md#Epic 2 Preparation Tasks`]
- [Source: `_bmad-output/project-context.md`]
- [Source: `Hexalith.Conversations/_bmad-output/project-context.md`]
- [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationCreated.cs`]
- [Source: `Hexalith.Conversations/src/Hexalith.Conversations/State/ConversationState.cs`]
- [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`]
- [Source: `Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-26: Resolved `bmad-dev-story` workflow customization; loaded root and module project contexts.
- 2026-05-26: Read story-required Conversations command/event/state/idempotency/API/projection files before implementation.
- 2026-05-26: Added red/green contract coverage for additive project assignment command/event vocabulary and explicit clear semantics.
- 2026-05-26: Completed domain, server, client, idempotency, projection, query, publication, and scaffold-reference validation passes.

### Completion Notes List

- Task 1 complete: added ADR 0002 documenting Conversations ownership of the conversation-to-project assignment, Pattern A read-by-back-reference, Projects non-ownership of mutable membership, and explicit clear/unassignment semantics.
- Added additive public `ReassignConversationProjectCommand`, explicit `ConversationProjectAssignment` operation shape, and `ConversationProjectChanged` event without introducing V2 contracts or changing existing canonical command/event values.
- Implemented pure aggregate/state handling for assign, reassign, clear, same-target no-op, typed safe rejections, deterministic replay, and idempotency fingerprints keyed by assignment meaning.
- Wired the tenant-first server handler, guarded `/api/v1/conversations/{conversationId}/project` route, typed client method, publication mapping, and safe outcome handling without exposing raw transport, EventStore, Dapr, claim, or payload internals.
- Updated projections/query materialization so current `ProjectId` is replaced or cleared by the new event and project-filtered lists reflect move/unlink behavior after catch-up.
- Adjusted Conversations local project-reference detection to prefer root-level sibling modules and updated scaffold smoke validation without initializing nested submodules.
- Validation completed for the affected Conversations test projects. Full `Hexalith.Conversations.slnx` was also attempted; story-related projects passed, and remaining solution failure is unrelated Admin.Web/Conformance analyzer debt outside the story's touched surface.

### File List

- `_bmad-output/implementation-artifacts/2-2-conversation-project-reassignment-upstream-capability.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `Hexalith.Conversations/Directory.Build.props`
- `Hexalith.Conversations/README.md`
- `Hexalith.Conversations/docs/adrs/0002-conversation-project-assignment-ownership.md`
- `Hexalith.Conversations/docs/adrs/index.md`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ConversationProjectAssignment.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ConversationProjectAssignmentOperation.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ReassignConversationProjectCommand.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationEventType.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/ConversationProjectChanged.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/README.md`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Results/ConversationCommandType.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/ConversationClient.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/ReassignConversationProjectCommandHandler.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/SetConversationRetentionPolicyCommandHandler.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionAccumulator.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionSnapshot.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Publication/ConversationPublicationMetadata.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Commands/ReassignConversationProject.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Events/ConversationProjectChangedDomainEvent.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Hexalith.Conversations.csproj`
- `Hexalith.Conversations/src/Hexalith.Conversations/Idempotency/ConversationCommandFingerprint.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/State/ConversationState.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Validation/ReassignConversationProjectBoundary.cs`
- `Hexalith.Conversations/src/Hexalith.Conversations/Validation/ReassignConversationProjectValidation.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/ContractMetadataTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/ConversationProjectAssignmentContractTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideWorkflowExampleTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationOnboardingDiagnosticsServiceTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionAccumulatorTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Publication/CallerMetadataPublicationTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Publication/PublicationSamples.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/ReassignConversationProjectCommandHandlerTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateProjectAssignmentTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Tests/Idempotency/ConversationCommandFingerprintTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Tests/Idempotency/ConversationIdempotencyStoreTest.cs`
- `Hexalith.Conversations/tests/Hexalith.Conversations.Tests/Replay/ConversationReplayVerifierTest.cs`

## Change Log

- 2026-05-26: Story context created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-26: Started dev-story implementation and added Conversations ADR 0002 for project assignment ownership.
- 2026-05-26: Added additive project reassignment command/event contracts, domain validation and replay, server/API/client wiring, idempotency, projection/query updates, and focused test coverage.
- 2026-05-26: Updated Conversations build references to prefer root-level sibling modules and aligned scaffold smoke validation with active conditional references.
- 2026-05-26: Completed validation and moved story 2.2 to review.
- 2026-05-26: Review cycle 1 auto-fixed API missing-body-identity validation, reassignment tenant-mismatch regression coverage, and adopter-facing documentation; story moved to done.

## Validation Notes

- Target story matched explicit user request and sprint key: `2-2-conversation-project-reassignment-upstream-capability`.
- Source analysis covered the create-story skill, checklist, sprint status, Epic 2 story text, PRD FR-6/FR-7/FR-11, architecture AR-G1/Pattern A/gap analysis, technical research, root and Conversations project context, story 2.1 completion/review notes, Epic 1 retrospective carry-forward actions, recent git history, and current Conversations command/event/state/client/API/projection patterns.
- Latest external technical lookup was not needed for story creation because this story uses pinned local project versions and existing local APIs; no new external library/API is introduced.
- Validation result: ready-for-dev. The story contains concrete acceptance criteria, scoped tasks, current-code facts, previous-story intelligence, architecture guardrails, file-location guidance, test requirements, explicit out-of-scope boundaries, and developer HALT conditions.
