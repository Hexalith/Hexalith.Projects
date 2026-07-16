# Story 1.8: Update Project Setup & Archive Project

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot or an authorized operator**,
I want **to update a Project's durable setup and to archive a Project**,
so that **conversation continuity stays current and finished projects remain auditable without being auto-selected as context** _(FR-3, FR-4, FR-19)_.

This story completes the Epic 1 workspace mutation surface after Stories 1.4, 1.6, and 1.7 established `CreateProject`, the layered fail-closed authorization chain, and Open/List read behavior. It adds the two remaining workspace commands: `UpdateProjectSetup` and `ArchiveProject`. These commands must flow through the same EventStore command pipeline as create, emit metadata-only success or rejection events, update the list/detail projections, and preserve the safe Open/List behavior already shipped by Story 1.7.

**Scope discipline:** this story owns `UpdateProjectSetup`, `ArchiveProject`, `ProjectSetupUpdated`, `ProjectArchived`, their HTTP/contract/client surfaces, aggregate/domain-processor dispatch, projection folds, validation, and tests. Do not implement reference link/unlink commands, ProjectContext assembly, `GetConversationStartSetup`, Project Resolution, audit timeline projection, restore/unarchive, FrontComposer UI screens, CLI/MCP command adapters, Aspire topology, production durable projection storage, or sibling ACL calls. Archived Projects remain readable/listable to authorized callers through the existing Open/List read surfaces, but they must not become active conversation context by default.

## Acceptance Criteria

1. **Authorized setup update emits durable, metadata-only setup event and updates projections (FR-3, FR-19, AR-3, AR-6, AR-8).**
   **Given** an authorized request for an existing Project
   **When** `UpdateProjectSetup` is submitted through the EventStore command pipeline
   **Then** `ProjectAggregate.Handle` emits exactly one `ProjectSetupUpdated` success event containing tenant/project identity, safe v1 Project Setup fields, actor/correlation/task/idempotency metadata, and `OccurredAt`
   **And** `ProjectStateApply`, `ProjectDetailProjection`, and `ProjectListProjection` fold the event deterministically: detail exposes the latest setup and `UpdatedAt`; list keeps metadata-only row data and updates `UpdatedAt`/sequence without inventing setup fields on list rows
   **And** the Open Project response can surface the updated setup additively without removing the existing `setupMetadata` compatibility field.

2. **Setup validation is field-specific, fail-closed, and bounded to conversation behavior/context policy (FR-19, NFR-2, NFR-6).**
   **Given** setup input containing project goals, user-facing instructions, preferred/excluded context sources, or conversation-start defaults
   **When** the command is validated
   **Then** only the v1 setup contract is accepted: bounded safe text for goals/instructions, supported reference/source kinds only, supported linked-source policy only, and no model-provider internals
   **And** raw secrets, raw tokens, unrestricted/local paths, transcript/file/memory payloads, raw prompts, unsupported reference types, unknown JSON members, malformed identifiers, or foreign-context payloads are rejected with `ProjectSetupUpdateRejected`
   **And** the rejection carries a shared `ReferenceState` reason plus the rejected field name only, never the rejected value.

3. **Authorized archive emits lifecycle event and existing reads reflect Archived semantics (FR-4, AR-8, NFR-3).**
   **Given** an authorized archive request for an existing Active Project
   **When** `ArchiveProject` is submitted through the EventStore command pipeline
   **Then** `ProjectArchived` is emitted; aggregate state and list/detail projections set lifecycle to `Archived`, update `UpdatedAt`/sequence from event data, and preserve project identity/setup/reference metadata for history
   **And** Story 1.7 Open Project returns metadata with `contextActivation.enabled = false` and `blockedReasonCode = "archived"`
   **And** Story 1.7 List Projects includes the Project when lifecycle is `archived` or `all` and excludes it when lifecycle is `active`.

4. **Invalid, unauthorized, duplicate, or stale mutation attempts fail closed without state corruption (AR-16, AR-19, NFR-1, NFR-3, NFR-7).**
   **Given** missing tenant context, reserved `system` tenant, stale/unavailable TenantAccessProjection, non-member principal, malformed claim-transform evidence, gateway denial, cross-tenant project, missing project, archived-project setup update, already-archived archive, invalid setup, duplicate delivery, or idempotency conflict
   **When** update/archive is attempted
   **Then** unauthorized/cross-tenant/missing-project cases remain externally indistinguishable at the HTTP edge through the existing safe-denial 404 shape
   **And** authorized domain invalid cases produce the appropriate rejection event (`ProjectSetupUpdateRejected` or `ProjectArchiveRejected`) with a shared reason code
   **And** the aggregate state is unchanged on rejection, same idempotency key plus same payload produces a logical replay/no-op, and same key plus different payload produces conflict.

5. **OpenAPI spine, generated client, idempotency helpers, and fingerprints stay authoritative (AR-15, AR-16).**
   **Given** `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` is the source of truth
   **When** update/archive operations are added
   **Then** the spine defines command-async mutation endpoints, metadata-only schemas, examples, safe-denial/validation/idempotency responses, required `Idempotency-Key`, correlation/task headers, and field-scoped idempotency equivalence for each mutation
   **And** `src/Hexalith.Projects.Client/Generated/*.g.cs` plus idempotency helpers are regenerated from the spine and never hand-edited
   **And** contract, client-generation, and fingerprint tests prove `UpdateProjectSetup` and `ArchiveProject` are available as generated client mutations while query operations still expose no idempotency helpers.

6. **No compiler settings weakened; boundaries preserved; gates green (project-context.md).**
   All touched projects keep `net10.0`, nullable, implicit usings, warnings-as-errors, central package management, and existing analyzer behavior. Domain aggregate/projection validation remains pure and infrastructure-free. Server owns HTTP/auth/safe ProblemDetails mapping. Client/CLI/MCP do not reference domain events or Dapr. No new package version is added unless unavoidable; any version goes through `Directory.Packages.props`. No sibling submodule pointer changes. No recursive submodule initialization/update.

## Tasks / Subtasks

- [x] **Task 1 - Define setup/archive contracts and metadata-only events** (AC: 1, 2, 3, 5, 6)
  - [x] Add `src/Hexalith.Projects.Contracts/Commands/UpdateProjectSetup.cs` and `ArchiveProject.cs` implementing `IProjectCommand`; names are imperative with no `Command` suffix.
  - [x] Add a concrete v1 setup model under `src/Hexalith.Projects.Contracts/Models/`: `ProjectSetup`, `ConversationStartDefaults`, and small closed enums for source kinds/policy if needed. Keep contracts low-dependency and serializer-tolerant. Suggested v1 fields: `Goals`, `UserInstructions`, `PreferredSourceKinds`, `ExcludedSourceKinds`, and `ConversationStartDefaults.LinkedSourcePolicy`.
  - [x] Keep setup model fields bounded and metadata-safe. Do not store transcript text, file contents, memory body, raw prompt, model/provider internals, unrestricted path, token, or secret.
  - [x] Add `ProjectSetupUpdated` and `ProjectArchived` success events implementing `IProjectEvent`. Include tenant/project identity, actor/correlation/task/idempotency metadata, fingerprint, and `OccurredAt`; do not include EventStore envelope fields or sibling payloads.
  - [x] Reuse existing `ProjectSetupUpdateRejected` and `ProjectArchiveRejected`; extend only if an additive field is strictly required. Keep reason as shared `ReferenceState`.
  - [x] Update `docs/event-catalog.md` for the new success events and the existing setup/archive rejection events. Document purpose, sensitivity, fields, and projection consumers.
  - [x] Update `docs/payload-taxonomy.md` and `PayloadClassification` only if the concrete setup model introduces new safe-field categories. Keep machine and human forms aligned.

- [x] **Task 2 - Extend pure aggregate validation, results, state, and idempotency** (AC: 1, 2, 3, 4, 6)
  - [x] Extend `ProjectCommandValidator` with overloads or command-specific validators for `UpdateProjectSetup` and `ArchiveProject`. Keep it pure: no Dapr, HTTP, gateway, clock lookup, sibling client, or filesystem access.
  - [x] Centralize setup validation so create/update do not drift. Existing `CreateProject.SetupMetadata` must continue to work; add the typed setup contract additively instead of breaking current create/read consumers.
  - [x] Reject unknown/unsupported setup members at the HTTP/process boundary using strict JSON member handling; preserve additive event deserialization in persisted events.
  - [x] Add operation-specific idempotency fingerprints. Include the operation name in every fingerprint, use deterministic field ordering, distinguish omitted vs null, and include every semantic setup field for `UpdateProjectSetup`. For `ArchiveProject`, fingerprint the operation plus request schema version/intent.
  - [x] Extend `ProjectResultCode` and `ProjectResult` so accepted results are not hard-coded to `Created` and rejection-event mapping is command-specific. `ProjectSetupUpdateRejected` and `ProjectArchiveRejected` must not accidentally surface as `ProjectCreationRejected`.
  - [x] Extend `ProjectAggregate.Handle` for `UpdateProjectSetup` and `ArchiveProject`. Required guards: command validation; existing project required; tenant/project identity must match state; archived Projects reject setup updates; already archived archive rejects unless it is the same idempotent replay; accepted paths emit exactly one success event.
  - [x] Extend `ProjectState` and `ProjectStateApply` for `ProjectSetupUpdated` and `ProjectArchived`. Apply must enforce canonical identity, dedupe same key/fingerprint before mutation, throw on unknown event types, and keep state unchanged on rejection.

- [x] **Task 3 - Update projections and read-model behavior** (AC: 1, 3, 4, 6)
  - [x] Extend `ProjectDetailProjection.Apply` for `ProjectSetupUpdated` and `ProjectArchived`: update setup/lifecycle, `UpdatedAt`, and sequence from event-carried data only.
  - [x] Extend `ProjectListProjection.Apply` for `ProjectSetupUpdated` and `ProjectArchived`: keep list rows metadata-only; update lifecycle for archive and update `UpdatedAt`/sequence for both events. Do not add setup text to list rows.
  - [x] Preserve deterministic ordering by `(Sequence, IdempotencyKey, IdempotencyFingerprint)`, envelope/event tenant agreement guard, canonical `ProjectIdentity` key derivation, and throw-on-unknown-event behavior.
  - [x] Extend `ProjectionRebuildConformance` coverage if item equality/fold behavior changes. Rebuild must remain exactly `Empty.Apply(envelopes)`; do not duplicate fold logic.
  - [x] Update `docs/projection-catalog.md` so `ProjectListProjection` and `ProjectDetailProjection` list `ProjectSetupUpdated`/`ProjectArchived` as source events with lifecycle/setup/freshness semantics.

- [x] **Task 4 - Grow OpenAPI spine and regenerate client artifacts** (AC: 2, 5, 6)
  - [x] Add `PATCH /api/v1/projects/{projectId}/setup` with `operationId: UpdateProjectSetup`, required `Idempotency-Key`, optional `X-Correlation-Id`, optional `X-Hexalith-Task-Id`, command-async `202 AcceptedCommand`, validation/safe-denial/idempotency responses, and metadata-only examples.
  - [x] Add `POST /api/v1/projects/{projectId}/archive` with `operationId: ArchiveProject`, required `Idempotency-Key`, optional correlation/task headers, command-async `202 AcceptedCommand`, validation/safe-denial/idempotency responses, and metadata-only examples.
  - [x] Add closed schemas for `UpdateProjectSetupRequest`, `ArchiveProjectRequest`, `ProjectSetup`, `ConversationStartDefaults`, and any setup enums. Use camelCase. No client-controlled `tenantId`.
  - [x] Add optional `projectSetup` (or equivalent) to the `Project` response additively. Keep the existing `setupMetadata` field for compatibility unless all generated/client tests are deliberately migrated.
  - [x] Define `x-hexalith-idempotency-equivalence` for both mutations and keep field lists deterministic and ordinal-sorted. Queries must still not include `Idempotency-Key`.
  - [x] Regenerate `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` and `HexalithProjectsIdempotencyHelpers.g.cs` from the spine. Do not hand-edit generated files; generated `.g.cs` stays LF-only per the existing generation tests.

- [x] **Task 5 - Wire Server endpoints, command submitter, and domain processor dispatch** (AC: 1, 2, 3, 4, 5, 6)
  - [x] Extend `ProjectsServerModule` with fully qualified command type constants for `UpdateProjectSetup` and `ArchiveProject`.
  - [x] Extend `IProjectCommandSubmitter` and `EventStoreProjectCommandSubmitter` with `SubmitUpdateProjectSetupAsync` and `SubmitArchiveProjectAsync`. Submit only metadata-safe payloads; tenant authority remains the verified EventStore tenant/envelope, never the request body.
  - [x] Extend `ProjectAuthorizationGate` with action tokens and methods for setup update/archive. Use the existing ordered chain. Require strict mutation freshness and project-detail evidence to avoid writing blind, but let the aggregate own lifecycle-domain rejections so authorized invalid mutations produce rejection events.
  - [x] Map the new HTTP endpoints in `ProjectsDomainServiceEndpoints`. Validate route project id and idempotency key, authorize before body parsing, reject malformed/unknown body members with field-specific safe validation problems only after authorization, and return the same command-async `202` envelope on accepted/replay outcomes.
  - [x] Preserve Story 1.7 anti-oracle behavior: unauthorized protected project-specific requests must get safe denial before validation details that could reveal resource existence.
  - [x] Extend `ProjectsDomainProcessor` to dispatch by command type: deserialize the matching payload strictly, build the command from `CommandEnvelope` values, run the matching write-layer EventStore authorization action, call the matching aggregate `Handle`, and map success/replay/rejection to `DomainResult` without mixing success and rejection payloads.

- [x] **Task 6 - Add focused Tier-1 tests** (AC: 1, 2, 3, 4, 6)
  - [x] Aggregate tests: setup update emits `ProjectSetupUpdated`; archive emits `ProjectArchived`; `Handle` is pure; state unchanged on rejection; archived setup update rejects; already archived archive rejects or replays according to idempotency; same key/same payload replays; same key/different payload conflicts.
  - [x] Validator tests: setup field bounds; unsupported source kind; unknown provider/model internals; raw secret/token/path/prompt/transcript/file/memory payload markers; rejected field name only; no value echo.
  - [x] State apply tests: identity mismatch throws metadata-only error; setup update changes setup; archive changes lifecycle; duplicate event delivery does not reapply; unknown event throws.
  - [x] Projection tests: list/detail fold setup update/archive; archive filters appear in list; detail context activation remains blocked through existing endpoint behavior; rebuild equals incremental and out-of-order/duplicate delivery stays deterministic.
  - [x] Schema evolution tests: golden corpus for `ProjectSetupUpdated`, `ProjectArchived`, `ProjectSetupUpdateRejected`, and `ProjectArchiveRejected`; future additive fields do not break deserialization. Do not introduce `V2` event types.
  - [x] NoPayloadLeakage tests over new commands, setup model, success events, rejection events, OpenAPI examples, and representative response/problem bodies.

- [x] **Task 7 - Add Tier-2 server and contract/client tests** (AC: 2, 3, 4, 5, 6)
  - [x] Contract tests: new paths/operations exist; schemas are closed/camelCase; mutation idempotency required; query idempotency still absent; safe-denial/validation/idempotency responses present; examples contain no forbidden payloads, secrets, token-shaped values, or local paths.
  - [x] Client tests: generated client exposes `UpdateProjectSetupAsync` and `ArchiveProjectAsync`; generated mutation request types expose idempotency helpers; query response types still do not.
  - [x] Server endpoint tests: authorized update/archive return `202`; invalid setup returns metadata-only validation after authorization; missing idempotency key returns `400`; idempotency conflict maps to `409`; gateway denial maps to safe-denial `404`; unavailable gateway/read model maps to retryable metadata-only problem.
  - [x] Authorization negative tests: missing tenant, `system` tenant, unknown tenant, disabled tenant, non-member, stale mutation evidence, malformed claim transform, Dapr policy denial, cross-tenant project, and missing project never reach the submitter and do not leak existence.
  - [x] Read-after-projection tests: after `ProjectSetupUpdated`, Open Project returns updated setup; after `ProjectArchived`, Open Project has `lifecycleState = archived` and activation blocked; List filters `active`, `archived`, and all work.
  - [x] Extend `ProjectTenantIsolationConformance` surfaces for setup update and archive routes so cross-tenant existing-project and missing-project responses remain externally indistinguishable.

- [x] **Task 8 - Run required verification and record results** (AC: 5, 6)
  - [x] `dotnet build Hexalith.Projects.slnx`
  - [x] `tests/tools/run-filtered-tests.ps1`
  - [x] `tests/tools/run-contract-spine-gates.ps1`
  - [x] `tests/tools/run-openapi-fingerprint-gate.ps1`
  - [x] `tests/tools/run-frontcomposer-inspect-gate.ps1` (expected skip-clean unless FrontComposer `[Projection]`/`[Command]` contracts are intentionally added)
  - [x] `git diff --check`
  - [x] Confirm `git status --short` shows no sibling submodule pointer changes and no unrelated file churn.

## Dev Notes

### Current On-Disk State to Build From

- `ProjectAggregate` currently handles only `CreateProject`. Its result path is create-specific: `ProjectResult.IsAccepted` means `ProjectResultCode.Created`, `ProjectResult.Accepted` takes `CreateProject`, and `ToRejectionEvent()` always produces `ProjectCreationRejected`. Story 1.8 must generalize this without breaking the existing create behavior.
- `ProjectCommandValidator` currently validates `CreateProject` only and treats `SetupMetadata` as a safe optional string. Story 1.8 should reuse the existing forbidden-metadata checks but introduce a typed v1 setup validator rather than accepting arbitrary JSON or raw setup blobs.
- `ProjectState` currently stores `SetupMetadata`, lifecycle, and idempotency fingerprints from `ProjectCreated` only. `ProjectStateApply` throws on unknown events. Add `ProjectSetupUpdated` and `ProjectArchived` to both state and projection folds at the same time.
- `ProjectSetupUpdateRejected` and `ProjectArchiveRejected` already exist in Contracts as metadata-only rejection records. The matching success events and commands are absent and belong to this story.
- `ProjectLifecycle` already contains only `Active` and `Archived`; do not add a third lifecycle state for this story.
- `ProjectListProjection` and `ProjectDetailProjection` currently fold only `ProjectCreated`, update `UpdatedAt == CreatedAt`, and throw on unknown event types. New lifecycle/setup events must update these folds or Story 1.7 reads will lie after archive/update.
- `ProjectsDomainServiceEndpoints` maps `POST /api/v1/projects`, `GET /api/v1/projects`, and `GET /api/v1/projects/{projectId}`. It already has safe ProblemDetails helpers, query idempotency rejection, freshness validation after authorization, and archived-context activation blocking. Reuse these helpers; do not mint a second error shape.
- `ProjectAuthorizationGate` currently has `projects:create`, `projects:read`, and `projects:list`. Add update/archive action tokens and keep the ordered chain intact.
- `IProjectCommandSubmitter` and `EventStoreProjectCommandSubmitter` currently submit only create. Widen the abstraction rather than adding ad hoc gateway calls in endpoint methods.
- `ProjectsDomainProcessor` currently accepts only `CreateProjectCommandType`. If command dispatch is not widened here, the HTTP/client surface may return `202` while the domain service rejects the command at `/process`.
- The OpenAPI spine explicitly says `UpdateProjectSetup` and `ArchiveProject` are deferred. This story removes that deferral and requires regeneration plus fingerprint updates.

### Existing Behavior That Must Be Preserved

- Tenant authority comes from authenticated claims plus EventStore claim-transform evidence only. Payload, headers, query strings, or setup fields are comparison inputs at most; they never become authority.
- Unauthorized and nonexistent protected resources remain externally indistinguishable. Do not parse setup bodies or return field-specific validation before authorization on project-specific mutation routes.
- EventStore remains the sole write authority. Read models/projections confirm state after events; endpoints must not mutate projections directly as authority.
- Domain logic stays pure. Aggregate `Handle`, state `Apply`, validators, and projection folds must not call Dapr, HTTP, filesystem, sibling clients, logging, or TimeProvider directly.
- All events, logs, ProblemDetails, OpenAPI examples, generated artifacts, and tests stay metadata-only. No transcript, file content, memory body, raw prompt, secret, token, full command body, unrestricted/local path, sibling denial detail, or absolute machine path.
- Existing `CreateProject`, Open Project, and List Projects behavior from Stories 1.4 and 1.7 must remain green, including query idempotency rejection and archived read activation blocking.
- Generated client artifacts are regenerated from the spine, not manually patched. Hand-written `.cs` remains CRLF/UTF-8/final newline; generated `.g.cs` remains LF-only per existing tests.
- No package upgrades are required. Do not bump Fluent UI, Dapr, Aspire, Roslyn, Fluxor, xUnit, NSwag, or the SDK as part of this story.

### Suggested v1 Project Setup Shape

The architecture called out concrete `ProjectSetup` schema as a design gap. Resolve it here with a deliberately small, additive, metadata-safe model:

```text
ProjectSetup
  Goals: IReadOnlyList<string>                 # bounded safe text, optional, no payload/secret/path/raw prompt
  UserInstructions: IReadOnlyList<string>      # user-facing conversation guidance, not provider/system prompt internals
  PreferredSourceKinds: IReadOnlyList<ProjectContextSourceKind>
  ExcludedSourceKinds: IReadOnlyList<ProjectContextSourceKind>
  ConversationStartDefaults: ConversationStartDefaults?

ProjectContextSourceKind
  Conversation
  ProjectFolder
  FileReference
  Memory

ConversationStartDefaults
  LinkedSourcePolicy: LinkedSourcePolicy

LinkedSourcePolicy
  None
  ProjectsOwnedMetadataOnly
  AuthorizedReferences
```

The exact C# member names may vary if the OpenAPI/client generator requires a better wire shape, but the semantics must stay: goals/instructions/preferences/defaults only, no model/provider internals, no sibling payloads, and no unsupported reference kinds. If the developer chooses a different shape, document the reason in the Dev Agent Record and keep AC 2 fully testable.

### Previous Story Intelligence

- Story 1.7 added Open/List queries and hardened the anti-oracle ordering: protected `GetProject` authorization runs before freshness/idempotency validation feedback. Preserve that ordering for update/archive routes.
- Story 1.7 review fixed list authorization proof and generated-client whitespace. Add direct gate tests for new `projects:update_setup` and `projects:archive` permissions, and run `git diff --check` after regeneration.
- Story 1.6 added the layered authorization chain, `TenantAccessProjection`, `ProjectAuthorizationGate`, `ProjectQueryTenantFilter`, redacted safe-denial envelope, and negative-path matrix. New mutation routes must use the same chain and must extend cross-tenant conformance rather than creating one-off tests.
- Story 1.6 review fixed safe-denial leakage and claim-transform normalization. Do not surface internal authz codes or claim values through update/archive ProblemDetails.
- Story 1.5 added `ProjectionRebuildConformance` and the "single fold" rule: `Rebuild(...)` delegates to `Empty.Apply(...)`. New events must be added to the single fold, not to a separate rebuild path.
- Story 1.4 established the command-async 202 pattern, `/process` domain processor, safe ProblemDetails helpers, `NoPayloadLeakageAssertions`, schema-evolution corpus, and create-state/projection vertical slice. Story 1.8 extends that slice; it does not replace it.

### Git Intelligence

- Latest commit `89de718 feat(story-1.7): open and list projects` changed the OpenAPI spine, generated client, server endpoints, auth gate, list read model, list/detail projections, projection catalog, and read tests. Follow those locations for update/archive rather than adding parallel structures.
- Previous commit `4e99f22 feat(story-1.6): tenant access and layered authorization` added deny-by-default authz defaults and the Tenants event projection. New mutation permissions should default closed until explicitly allowed by tests/host wiring.
- The sprint-status history records that Story 1.7 gates were green: build, filtered lane, full solution tests, fingerprint gate, frontcomposer skip-clean, and `git diff --check`. Treat those as the minimum verification target.

### Library / Framework Requirements

- Use repository pins: .NET SDK `10.0.302`, target `net10.0` except low-dependency Contracts where already configured, Dapr `1.17.9`, NSwag.MSBuild `14.7.1`, Newtonsoft.Json `13.0.4` for generated client, YamlDotNet `17.1.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`.
- Central Package Management is active. New `PackageReference` entries must not have inline `Version`; add/adjust `<PackageVersion />` only in `Directory.Packages.props` if unavoidable. Microsoft Learn documents this as the CPM model and NU1008 failure mode.
- Use `System.Text.Json` for hand-written server/domain JSON. `JsonUnmappedMemberHandling.Disallow` is available in modern .NET and throws on unmapped object properties; use it for request DTO strictness where the current endpoint pattern already does.
- Do not use Newtonsoft.Json in domain/server hand-written code. The generated NSwag client already uses Newtonsoft.Json because `nswag.json` pins that generator setting.

### Expected File / Structure Changes

```text
src/Hexalith.Projects.Contracts/Commands/UpdateProjectSetup.cs          # NEW
src/Hexalith.Projects.Contracts/Commands/ArchiveProject.cs              # NEW
src/Hexalith.Projects.Contracts/Events/ProjectSetupUpdated.cs           # NEW
src/Hexalith.Projects.Contracts/Events/ProjectArchived.cs               # NEW
src/Hexalith.Projects.Contracts/Events/ProjectSetupUpdateRejected.cs    # MODIFY only if additive metadata is required
src/Hexalith.Projects.Contracts/Events/ProjectArchiveRejected.cs        # MODIFY only if additive metadata is required
src/Hexalith.Projects.Contracts/Models/ProjectSetup*.cs                 # NEW - concrete v1 setup shape
src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml       # MODIFY - update/archive mutations + schemas
src/Hexalith.Projects.Client/Generated/*.g.cs                           # REGENERATE ONLY
src/Hexalith.Projects.Client/Idempotency/*                              # MODIFY only generator-owned helper inputs/outputs
src/Hexalith.Projects/Aggregates/Project/*.cs                           # MODIFY - validator/result/state/handle/apply
src/Hexalith.Projects/Projections/ProjectList/*.cs                      # MODIFY - fold setup/archive events
src/Hexalith.Projects/Projections/ProjectDetail/*.cs                    # MODIFY - fold setup/archive events
src/Hexalith.Projects.Server/ProjectsServerModule.cs                    # MODIFY - command type constants
src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs                # MODIFY - submit update/archive
src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs       # MODIFY - gateway payloads
src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs   # MODIFY - action tokens/methods
src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs          # MODIFY - endpoints
src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs                 # MODIFY - command dispatch
docs/event-catalog.md                                                   # MODIFY
docs/projection-catalog.md                                              # MODIFY
docs/payload-taxonomy.md                                                # MODIFY only if setup safe categories are added
tests/Hexalith.Projects.Contracts.Tests/*                               # MODIFY/ADD contract/event/openapi tests
tests/Hexalith.Projects.Client.Tests/*                                  # MODIFY generated client/idempotency tests
tests/Hexalith.Projects.Tests/*                                         # MODIFY/ADD aggregate/projection/schema/leakage tests
tests/Hexalith.Projects.Server.Tests/*                                  # MODIFY/ADD endpoint/authz tests
```

### Testing Requirements

- **Tier-1 pure:** command validators, aggregate handle/apply, state identity guard, idempotency replay/conflict, list/detail projection folds, rebuild/replay conformance, schema-evolution golden corpus, and `NoPayloadLeakage`.
- **Tier-2 server:** update/archive routes, authorization ordering, safe-denial, idempotency key handling, strict body validation after authorization, gateway outcome mapping, Open/List projection-observed behavior after events.
- **Contract/client:** OpenAPI path/schema/headers/responses/idempotency-equivalence tests, generated client shape tests, generated helper fingerprint/currentness tests.
- **Security negatives:** missing tenant, `system` tenant, unknown tenant, disabled tenant, non-member, stale projection for mutation, malformed claim transform, Dapr deny-by-default evidence, EventStore validator denial, cross-tenant project, missing project, archived setup update, already archived archive.
- **Payload-safety negatives:** raw secret, raw token, local/unrestricted path, transcript text marker, file-content marker, memory-body marker, raw-prompt marker, unsupported reference kind, model/provider internal fields, full command body echo.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.8: Update Project Setup & Archive Project] - story statement and BDD acceptance criteria.
- [Source: _bmad-output/planning-artifacts/epics.md#Functional Requirements] - FR-3 Update Project Setup, FR-4 Archive Project, FR-19 Validate Project Setup.
- [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements] - AR-3 write authority, AR-4 identity, AR-5 commands, AR-6 events, AR-8 projections, AR-15 OpenAPI spine, AR-16 command-async/errors, AR-19 authorization, AR-23 testing.
- [Source: _bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions] - EventStore-only writes, metadata-only `ProjectSetupUpdated`/`ProjectArchived`, setup validation, additive schema evolution.
- [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns] - command naming, event naming, rejection events, idempotency required on mutations, queries reject idempotency.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-3: Update Project Setup] - setup contents and v1 boundary.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-4: Archive Project] - archive lifecycle/discoverability behavior.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md#FR-19: Validate Project Setup] - validation rejects secrets/paths/payloads and returns field-specific safe errors.
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Feedback Patterns] - safe validation/error feedback and maintenance-action consequence visibility.
- [Source: _bmad-output/implementation-artifacts/1-7-open-list-projects.md] - previous read surface behavior, files touched, and review fixes to preserve.
- [Source: _bmad-output/implementation-artifacts/1-6-tenant-access-layered-fail-closed-authorization.md] - layered authz, safe denial, query-side filtering, and negative-path matrix.
- [Source: _bmad-output/implementation-artifacts/1-5-projection-rebuild-replay-idempotency.md] - rebuild/replay and idempotency conformance rules.
- [Source: _bmad-output/implementation-artifacts/1-4-create-project-end-to-end-tracer-bullet.md] - command-async create slice, `NoPayloadLeakage`, schema-evolution corpus, safe-denial helpers.
- [Source: _bmad-output/project-context.md] - pinned stack, Dapr-only infra, central package management, tenant isolation at every layer, no recursive submodules.
- [Source: src/Hexalith.Projects/Aggregates/Project] - current aggregate, validator, result, state, and apply code to extend from create-only to update/archive.
- [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml] - current OpenAPI spine with update/archive deferral.
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs] - current safe ProblemDetails, create/get/list route patterns, freshness/idempotency behavior.
- [Source: src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs] - current create-only `/process` dispatcher to extend.
- [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs] - ordered authz gate to extend with update/archive permissions.
- [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs] and [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs] - current deterministic projection folds.
- [Source: docs/payload-taxonomy.md] and [Source: src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs] - metadata-only allow/deny source of truth.
- [Source: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/missing-members] - `System.Text.Json` unmapped member handling can throw on unknown JSON properties.
- [Source: https://learn.microsoft.com/nuget/consume-packages/central-package-management] - central package versions belong in `Directory.Packages.props`, not inline project references.

## Dev Agent Record

### Agent Model Used

### Debug Log References

- `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore --filter FullyQualifiedName~OpenApi` - initial red for missing setup/archive paths and schemas; final pass 13/13.
- `dotnet build src\Hexalith.Projects.Client\Hexalith.Projects.Client.csproj --no-restore` - regenerated OpenAPI client/helper artifacts; 0 warnings, 0 errors.
- `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore --filter FullyQualifiedName~ClientGenerationTests` - pass 19/19 after fixing nested enum collection canonicalization.
- `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~SchemaEvolutionGoldenCorpusTests|FullyQualifiedName~NoPayloadLeakageTests|FullyQualifiedName~ProjectSetupArchiveAggregateTests|FullyQualifiedName~ProjectProjectionTests"` - pass 37/37.
- `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApi|FullyQualifiedName~PayloadClassificationTests|FullyQualifiedName~RejectionEventTaxonomyTests"` - pass 63/63.
- `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectsDomainProcessorTests|FullyQualifiedName~CreateProjectEndpointTests.PatchProjectSetup|FullyQualifiedName~CreateProjectEndpointTests.PostProjectArchive|FullyQualifiedName~CreateProjectEndpointTests.GetAndList_AfterSetupUpdateAndArchive|FullyQualifiedName~ProjectAuthorizationGateTests"` - pass 21/21.
- `dotnet build Hexalith.Projects.slnx` - pass, 0 warnings, 0 errors.
- `tests\tools\run-filtered-tests.ps1` - pass: Contracts 115/115, Client 19/19, Projects 115/115, Server 63/63.
- `tests\tools\run-contract-spine-gates.ps1` - pass: OpenAPI 13/13 and ClientGeneration 19/19.
- `tests\tools\run-openapi-fingerprint-gate.ps1` - pass; generated artifacts match the Contract Spine fingerprint.
- `tests\tools\run-frontcomposer-inspect-gate.ps1` - skipped clean; no FrontComposer contracts present.
- `git diff --check` - pass after trimming NSwag trailing whitespace; only Git CRLF conversion warnings remain.
- `git status --short` - no sibling submodule pointer changes observed; `.codex/` and existing orchestration-state edits preserved.
- `dotnet msbuild src\Hexalith.Projects.Client\Hexalith.Projects.Client.csproj /t:GenerateHexalithProjectsIdempotencyHelpers /p:Configuration=Debug` - pass; regenerated setup idempotency helper after review fix.
- `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore --filter FullyQualifiedName~ClientGenerationTests` - pass 20/20.
- `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectCommandValidatorTests|FullyQualifiedName~ProjectionRebuildDeterminismTests"` - pass 32/32.
- `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter FullyQualifiedName~ProjectsDomainProcessorTests` - pass 8/8 after schema-version rejection fix.
- `dotnet build Hexalith.Projects.slnx` - pass, 0 warnings, 0 errors.
- `tests\tools\run-filtered-tests.ps1` - pass: Contracts 115/115, Client 20/20, Projects 128/128, Server 64/64.
- `tests\tools\run-contract-spine-gates.ps1` - pass: OpenAPI 13/13 and ClientGeneration 20/20.
- `tests\tools\run-openapi-fingerprint-gate.ps1` - pass; generated artifacts match the Contract Spine fingerprint.
- `tests\tools\run-frontcomposer-inspect-gate.ps1` - skipped clean; no FrontComposer contracts present.
- `git diff --check` - pass; only Git CRLF conversion warnings reported.
- `git status --short` - no sibling submodule pointer changes observed; `.codex/` preserved.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added the typed v1 `ProjectSetup` contract, `UpdateProjectSetup`/`ArchiveProject` commands, and `ProjectSetupUpdated`/`ProjectArchived` success events.
- Generalized aggregate result/state/apply logic for setup update and archive, including command-specific rejections, operation-scoped idempotency fingerprints, replay/conflict handling, archived lifecycle guards, and metadata-only validation.
- Folded setup/archive events through list/detail projections and in-memory read models; Open Project now returns additive `projectSetup` while retaining `setupMetadata`.
- Added strict HTTP/process payload handling, update/archive authorization actions, EventStore submitter methods, OpenAPI operations/schemas/examples, regenerated client methods, and generated idempotency helpers.
- Extended payload taxonomy/docs, event/projection catalogs, leakage tests, schema-evolution golden corpus, OpenAPI/client tests, aggregate/projection tests, domain-processor tests, authorization tests, and endpoint tests.
- Review cycle 1 fixed setup-update idempotency parity for omitted conversation defaults and safe quoted/punctuated text, enforced `requestSchemaVersion` in the update process payload, and completed the validator/projection conformance coverage promised by Task 6/Task 3.

### File List

- _bmad-output/implementation-artifacts/1-8-update-project-setup-archive-project.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- docs/event-catalog.md
- docs/payload-taxonomy.md
- docs/projection-catalog.md
- src/Hexalith.Projects.Contracts/Commands/ArchiveProject.cs
- src/Hexalith.Projects.Contracts/Commands/UpdateProjectSetup.cs
- src/Hexalith.Projects.Contracts/Events/ProjectArchived.cs
- src/Hexalith.Projects.Contracts/Events/ProjectSetupUpdated.cs
- src/Hexalith.Projects.Contracts/Models/ConversationStartDefaults.cs
- src/Hexalith.Projects.Contracts/Models/LinkedSourcePolicy.cs
- src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs
- src/Hexalith.Projects.Contracts/Models/ProjectContextSourceKind.cs
- src/Hexalith.Projects.Contracts/Models/ProjectSetup.cs
- src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml
- src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs
- src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs
- src/Hexalith.Projects.Client/Generation/Program.cs
- src/Hexalith.Projects.Client/Idempotency/HexalithIdempotencyHasher.cs
- src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs
- src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidationResult.cs
- src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs
- src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs
- src/Hexalith.Projects/Aggregates/Project/ProjectResultCode.cs
- src/Hexalith.Projects/Aggregates/Project/ProjectState.cs
- src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs
- src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs
- src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs
- src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs
- src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs
- src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs
- src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs
- src/Hexalith.Projects.Server/InMemoryProjectDetailReadModel.cs
- src/Hexalith.Projects.Server/InMemoryProjectListReadModel.cs
- src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs
- src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs
- src/Hexalith.Projects.Server/ProjectsServerModule.cs
- tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs
- tests/Hexalith.Projects.Contracts.Tests/Models/PayloadClassificationTests.cs
- tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs
- tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs
- tests/Hexalith.Projects.Server.Tests/ProjectAuthorizationGateTests.cs
- tests/Hexalith.Projects.Server.Tests/ProjectQueryTenantFilterTests.cs
- tests/Hexalith.Projects.Server.Tests/ProjectsDomainProcessorTests.cs
- tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateHandleTests.cs
- tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectCommandValidatorTests.cs
- tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectSetupArchiveAggregateTests.cs
- tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs
- tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs
- tests/Hexalith.Projects.Tests/Replay/ProjectionRebuildDeterminismTests.cs
- tests/Hexalith.Projects.Tests/SchemaEvolution/Golden/ProjectArchiveRejected.v1.json
- tests/Hexalith.Projects.Tests/SchemaEvolution/Golden/ProjectArchived.v1.json
- tests/Hexalith.Projects.Tests/SchemaEvolution/Golden/ProjectSetupUpdateRejected.v1.json
- tests/Hexalith.Projects.Tests/SchemaEvolution/Golden/ProjectSetupUpdated.v1.json
- tests/Hexalith.Projects.Tests/SchemaEvolution/SchemaEvolutionGoldenCorpusTests.cs

### Senior Developer Review (AI)

**Reviewer:** Codex on 2026-05-25

**Outcome:** Approved after auto-fixes. No CRITICAL, HIGH, or MEDIUM issues remain.

**Findings fixed:**

- [HIGH] Setup-update idempotency parity drift: the generated client helper treated omitted `conversationStartDefaults` as `present=true;value=null`, while the aggregate fingerprint treated it as omitted. The aggregate also hand-built setup text-list JSON, drifting for safe quotes/equals/semicolons. Fixed generator nested-presence logic, regenerated helpers, and switched aggregate text-list canonicalization to JSON serialization with regression coverage.
- [HIGH] Update setup `/process` payload did not carry or enforce `requestSchemaVersion` even though the Contract Spine includes it in idempotency equivalence. Fixed the EventStore submitter payload, process deserialization/validation, and field-specific rejection coverage.
- [CRITICAL] Task 6 validator coverage was marked complete but did not cover the promised setup bounds, unsupported enum values, model/provider internals, payload markers, and no-value-echo cases. Added focused validator coverage and tightened model/provider internal markers.
- [CRITICAL] Task 3/6 projection rebuild conformance was marked complete while the reusable conformance stream still exercised only `ProjectCreated`. Expanded the stream to include `ProjectSetupUpdated` and `ProjectArchived`.

**Validation:** build, filtered tests, contract spine gate, OpenAPI fingerprint gate, FrontComposer inspect gate, and `git diff --check` all passed in review cycle 1.

### Change Log

- 2026-05-25: Implemented Story 1.8 update setup/archive command slice through contracts, domain, projections, server, OpenAPI/client generation, docs, and tests; moved status to review after required gates passed.
- 2026-05-25: Story-automator review cycle 1 auto-fixed setup idempotency parity, update process schema enforcement, validator coverage, and projection conformance coverage; moved status to done after gates passed.
