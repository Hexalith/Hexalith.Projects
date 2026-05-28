# Story 1.4: Create Project (end-to-end tracer bullet)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to create a Project with tenant context and a Project name and have it durably recorded as `Active`**,
so that **a conversation can be anchored to a real, tenant-scoped workspace record** _(realizes UJ-2, UJ-3; FR-1, FR-19)_.

This is **Implementation Sequence step 3** (architecture §Decision Impact Analysis): the **first vertical slice through the whole stack** — the tracer bullet that proves the persist-then-publish event flow end-to-end. It builds on Story 1.1 (scaffold/CI), Story 1.2 (`ProjectId` + shared vocabulary + rejection taxonomy + payload classification + `ProjectIdentity` derivation), and Story 1.3 (OpenAPI spine + generated client + idempotency hasher + fingerprint gate). It fills the empty `Contracts/Commands/`, core `Aggregates/Project/`, and `Projections/` folders, **grows** the spine's seed `CreateProject`/`GetProjectLifecycleStatus` operations into the real Create-Project + Get-Project contract, wires the Server `/api/v1/projects` command-async endpoint + `/process` aggregate callback through the EventStore command pipeline, and lands the **FS-2 `NoPayloadLeakage` harness** and the **FS-5 schema-evolution golden corpus** against the first real success/rejection events.

**Scope discipline — this story is `CreateProject` ONLY.** Defer to their own stories: `GetProject`/full detail read + `ListProjects` (1.7); `UpdateProjectSetup`/`ArchiveProject` (1.8); `TenantAccessProjection` + layered fail-closed authorization (1.6 — this story uses a **minimal tenant-context guard** sufficient to prove fail-closed-on-missing-tenant, not the full claim-transform/projection chain); Aspire/Dapr/Workers topology (1.9); projection-rebuild determinism + duplicate-command/duplicate-projection-delivery idempotency proofs (1.5 — this story keeps `Handle`/`Apply` idempotency-tolerant by construction but does **not** own the rebuild/dedup test suite); auto-folder-create (deferred to PR-3/Story 2.4). A `GetProject` confirmation read in scope here is the **minimal lifecycle/detail read** needed to confirm the 202'd create landed — not the full FR-2 Open Project surface.

## Acceptance Criteria

1. **Happy-path create → `ProjectCreated` → projections reflect it (FR-1, AR-3, AR-4).**
   **Given** a valid tenant context and a Project name (the only required user input)
   **When** `CreateProject` is submitted through the EventStore command pipeline
   **Then** `ProjectAggregate.Handle` is **pure** and emits `ProjectCreated` (a past-tense, metadata-only success event), persist-then-publish (events persisted before publish — domain returns payloads only, EventStore owns envelope metadata), `ProjectState.Apply`/`StateApply` sets lifecycle `ProjectLifecycle.Active` mutating only in-memory state, and `ProjectListProjection` + `ProjectDetailProjection` reflect the new project after projection update
   **And** the create endpoint returns **`202 AcceptedCommand`** (command-async; no read-after-write assumption) confirmable via a minimal `GetProject` lifecycle/detail read
   **And** no conversation transcript, file content, or memory payload is duplicated into the Project (metadata-only; `ProjectCreated` carries only `ProjectId`, tenant, name, optional description/setup-metadata refs, lifecycle, actor/correlation/idempotency metadata, `OccurredAt`).

2. **Fail-closed on missing/unauthorized tenant → `ProjectCreationRejected` (NFR-1, NFR-3, AR-3, FS-4).**
   **Given** missing or unauthorized tenant context
   **When** `CreateProject` is submitted
   **Then** the command **fails closed** and `ProjectCreationRejected` (the Story 1.2 `IRejectionEvent`) is emitted with a shared `ReferenceState` reason code (e.g. `Unauthorized`/`TenantMismatch`) — **not an exception path** (domain rejection ≠ infrastructure failure)
   **And** one `DomainResult`/aggregate result **never mixes success and rejection payloads** (a result is either Accepted-with-`ProjectCreated` or Rejected-with-`ProjectCreationRejected`, never both)
   **And** the HTTP surface maps the denial to **safe-denial `404`** (unauthorized and nonexistent externally indistinguishable, per AR-16/Story 1.3) using the spine's `SafeAuthorizationDenial` response — never a generic 500, never echoing whether a tenant/project exists.

3. **Setup validation rejects unsafe content without echoing values (FR-19, NFR-2).**
   **Given** `CreateProject` carries optional setup metadata
   **When** the command is validated (boundary validation in the aggregate / a pure `ProjectCommandValidator`)
   **Then** raw secrets, unrestricted/local file paths, unsupported reference types, and foreign-context payloads are rejected with a **structured error that names the rejected field without echoing its value** (`ProjectCreationRejected.RejectedField` carries the field NAME only; the RFC 9457 `ValidationFailure` response `details` is metadata-only)
   **And** the only required input is the Project name; default lifecycle is `Active`
   **And** when **no Project Folder is supplied the create succeeds without one** (auto-folder is deferred to PR-3/Story 2.4 — do NOT call the Folders client from the aggregate; that is a named anti-pattern).

4. **Metadata-only + schema tolerance: `NoPayloadLeakage` harness (FS-2) + golden corpus (FS-5).**
   **Given** the metadata-only invariant (FS-2) and schema tolerance (FS-5, NFR-6)
   **When** `ProjectCreated` / `ProjectCreationRejected` are serialized (event, log scope, and any DTO)
   **Then** the reusable **`NoPayloadLeakage` harness** asserts no forbidden field (per Story 1.2 `Models/PayloadClassification.ForbiddenContent`) appears in the serialized event, log, or DTO — and the harness is authored as a **reusable Tier-1 guard** (extended every later epic), not a one-off test
   **And** a **frozen serialized golden sample** of `ProjectCreated` and `ProjectCreationRejected` is added to the **schema-evolution corpus** (`tests/.../SchemaEvolution/` golden JSON files) and **round-trips in CI** (deserializes from the frozen bytes via the production converters, proving backward-compatible/additive deserialization — no `V2` event types).

5. **Spine grown to the real Create/Get contract; client regenerated; gates green (AR-15, AR-16, AR-17, FS-7).**
   **Given** the spine-as-source-of-truth rule and the seed `CreateProject`/`GetProjectLifecycleStatus` operations Story 1.3 left
   **When** the Create-Project + minimal Get-Project contract is finalized
   **Then** `Contracts/openapi/hexalith.projects.v1.yaml` carries the real `CreateProject` mutation (`202 AcceptedCommand`; `400 ValidationFailure`; `401/403/404 SafeAuthorizationDenial`; `409 IdempotencyConflict`; `Idempotency-Key` required + `x-hexalith-idempotency-equivalence` field list; camelCase; ISO-8601 `DateTimeOffset`; synthetic payload-free examples) and the minimal `GetProject` query (carries `X-Hexalith-Freshness`; `Idempotency-Key` NOT a parameter; `401/403/404 SafeAuthorizationDenial`)
   **And** the NSwag client + idempotency helpers are **regenerated from the spine** (`Client/Generated/*.g.cs`, never hand-edited) and the `HexalithProjectsGeneratedArtifacts` provenance updates
   **And** the OpenAPI fingerprint gate (`run-openapi-fingerprint-gate.ps1`) passes (no drift), and the FrontComposer inspect gate stays correct (skip-clean if no `[Projection]`/`[Command]` annotations are added; real `frontcomposer inspect --fail-on-warning` if they are).

6. **Tenant isolation + identity derivation proven on the trivial event set (NFR-1, AR-4, FS-3, FS-8).**
   **Given** canonical identity `{tenant}:projects:{projectId}` (Story 1.2 `ProjectIdentity`)
   **When** a project is created and its keys/topics/scopes are derived and its projections keyed
   **Then** the aggregate stream identity, state keys, projection keys, and pub/sub topic are derived **only** from `ProjectIdentity` (never from payload/header/query) — and a `ProjectCreated` for tenant A never lands in tenant B's `ProjectListProjection`/`ProjectDetailProjection` bucket (the `Apply` path rejects/skips a foreign-tenant event, mirroring `FolderStateApply`/`FolderListProjection`)
   **And** an explicit **cross-tenant isolation negative test** (FS-8, the SM-3 acceptance test seeded here) asserts "Project A in tenant A never appears in a tenant B query" on this trivial event set.

7. **No compiler setting weakened; boundaries preserved; build + filtered lane + gates green (NFR all, project-context.md).**
   All touched projects keep `net10.0` (`Contracts` netstandard2.0-safe), `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` — no `NoWarn`/`#pragma`/`SuppressMessage`/nullable-disable to force green. Boundary direction holds: `Contracts` low-dependency (commands/events/queries + reuse of Story 1.2 vocabulary); domain core `Hexalith.Projects` is **pure** (aggregate/projection/validator — no Dapr/network/ACL/HTTP); `Server` owns the host wiring + EventStore command-pipeline + endpoint + ProblemDetails mapping; `Client`/`Cli`/`Mcp` never reference domain event types or Dapr. `dotnet build Hexalith.Projects.slnx` + the filtered Tier-1/Contracts/Client + the new Server (Tier-2) lane + both gate scripts pass green with zero warnings. Any new package version is added to root `Directory.Packages.props` reusing the exact sibling-pinned version, never inline.

## Tasks / Subtasks

- [x] **Task 1 — `CreateProject` command + `ProjectCreated` success event (Contracts)** (AC: 1, 3, 7)
  - [x] Create `src/Hexalith.Projects.Contracts/Commands/CreateProject.cs`: a `sealed record` imperative command (no `Command` suffix) carrying `TenantId`, `ProjectId`, `Name` (required), optional `Description`, optional safe setup-metadata fields, and the cross-cutting envelope fields the Folders `CreateFolder` carries (`ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`). Mirror `Hexalith.Folders/.../Aggregates/Folder/CreateFolder.cs` shape and the `IFolderCommand`/`CommandType` convention; define a Projects `IProjectCommand` marker (Contracts or domain-core — see Project Structure Notes). Use `ProjectId` (Story 1.2 VO), NOT a raw string, for the project identity field; tenant is a `string` (envelope tenant).
  - [x] Create `src/Hexalith.Projects.Contracts/Events/ProjectCreated.cs`: a `sealed record` past-tense success event (no `Event` suffix) implementing the EventStore event-payload interface used by the rejection events' base (`Hexalith.EventStore.Contracts.Events.IEventPayload`; define a `IProjectEvent` marker if mirroring Folders' `IFolderEvent`). Carry **metadata-only** fields: `ProjectId`, `TenantId`, `Name`, optional `Description`, `ProjectLifecycle Lifecycle` (= `Active`), actor/correlation/task/idempotency metadata, `DateTimeOffset OccurredAt`. **No setup body, no folder path, no secrets/tokens.** Add XML doc marking sensitivity class metadata-only + an **event-catalog entry** (purpose, fields, sensitivity class, consumers) per AR-6 (`docs/event-catalog.md` — create if absent).
  - [x] Reuse the existing `Events/ProjectCreationRejected.cs` (Story 1.2) for the rejection path — do NOT re-define it. Confirm its `ReferenceState Reason` + `RejectedField` (name-only) shape is sufficient; if a `ProjectId` field is needed on the rejection for correlation, add it additively (no `V2`).

- [x] **Task 2 — `ProjectAggregate` pure `Handle` + `ProjectState`/`StateApply` (domain core)** (AC: 1, 2, 3, 6, 7)
  - [x] Create `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs` as a **static** class with a pure `Handle(ProjectState state, CreateProject command, DateTimeOffset occurredAt)` returning a `ProjectResult` (mirror `FolderAggregate.Handle`): validate (Task 3); fail-closed on missing/unauthorized tenant → `Rejected(..., ReferenceState.Unauthorized/TenantMismatch)`; idempotency-tolerant (if same idempotency key already recorded → replay/conflict result, not a second event); duplicate-create guard (`state.IsCreated` → reject); else emit `[ProjectCreated]` → `Accepted`. Add the deterministic-timestamp test overload `Handle(state, command)` → `Handle(state, command, DateTimeOffset.MinValue)` as Folders does. **Keep it pure: no Dapr/network/ACL/Folders-client/HTTP** (calling a sibling client from the aggregate is a named anti-pattern).
  - [x] Create `ProjectState.cs` (sealed record, `Empty` factory, `IsCreated` flag, lifecycle, recorded identity, an `IdempotencyFingerprints` map for replay dedup) and `ProjectStateApply.cs` (pure `Apply(state, event, expectedIdentity)`): apply `ProjectCreated` → `IsCreated=true`, `Lifecycle=Active`, records identity + idempotency; **enforce the expected canonical identity** (a foreign-tenant/foreign-project event throws/`TenantMismatch`, mirroring `FolderStateApply`); dedupe identical idempotent replays; **throw on unknown event types** (do not silently no-op — mirrors Folders). Derive the expected stream identity from `ProjectIdentity` (Story 1.2), never from a raw string.
  - [x] Create `ProjectResult.cs` (sealed record: reason/result code, safe echoed identity fields via a `SafePassthrough`-style guard so a rejection cannot echo unsafe input, and the emitted events list) + a `ProjectResultCode`/reuse of `ReferenceState` for rejection reasons. Use **partial classes per concern** (`ProjectAggregate.cs` now; leave room for `ProjectAggregate.References.cs`/`.Resolution.cs` in Epics 2/4) so later epics don't churn this file (epics.md Epic-1 decomposition guidance).

- [x] **Task 3 — Pure setup/command validator (FR-19)** (AC: 2, 3, 6, 7)
  - [x] Create `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` (pure, Tier-1, mirror `FolderCommandValidator`): require a non-empty/non-whitespace `Name`; require non-empty tenant (missing tenant → fail-closed reason); validate `ProjectId` shape (non-whitespace; do NOT `Guid.TryParse` — ULID per R2-A7); reject raw secrets, unrestricted/local file paths, unsupported reference types, and foreign-context payloads in optional setup metadata, returning the **rejected field NAME only** (cross-check forbidden categories against `Models/PayloadClassification.ForbiddenContent`); compute the idempotency fingerprint consistent with the spine's `x-hexalith-idempotency-equivalence` list (reuse the Story 1.3 `HexalithIdempotencyHasher` contract semantics — the canonical fingerprint, not a new scheme). Return a `ProjectCommandValidationResult` (accepted + canonicalized fields + fingerprint, or rejected + reason code + field name).
  - [x] Negative cases are **ACs on this story, not a trailing story** (FS-4): missing tenant, blank name, secret/path/foreign-payload in setup, duplicate create, idempotent replay vs idempotency conflict — each asserted in Tier-1 tests (Task 6).

- [x] **Task 4 — `ProjectListProjection` + `ProjectDetailProjection` (read models)** (AC: 1, 6, 7)
  - [x] Create `src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs` + `ProjectListItem.cs` + a `ProjectProjectionEnvelope.cs` (sequence + tenant + event), mirroring `Hexalith.Folders/.../Projections/FolderList/`: private ctor over a `FrozenDictionary`, `Empty`, pure `Apply(IEnumerable<envelope>)` with deterministic ordering (sequence, then idempotency key, then fingerprint), **tenant-guard** (envelope tenant must equal event tenant; foreign event skipped — FS-8/NFR-1), `ProjectCreated` → adds a `ProjectListItem` (tenant, projectId, name, lifecycle, sequence, timestamps — metadata-only), **throw on unknown event types** (keep in sync with `ProjectStateApply`). Key by the canonical `{tenant}:projects:{projectId}` shape (derive via `ProjectIdentity` or the same literal Folders uses).
  - [x] Create `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs` + `ProjectDetailItem.cs`: same pattern, the per-project detail record the minimal `GetProject` read returns (tenant, projectId, name, optional description, lifecycle, created/updated timestamps, freshness/sequence watermark — metadata-only). Both projections are tenant-scoped and rebuildable-by-construction (deterministic `Apply`); the **rebuild/replay determinism + duplicate-delivery idempotency proof suite is Story 1.5** — here just keep `Apply` deterministic and idempotent-tolerant.

- [x] **Task 5 — Grow the spine + Server command-async endpoint + `/process` pipeline wiring** (AC: 1, 2, 5, 7)
  - [x] In `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`: promote the seed `CreateProject` POST from a placeholder to the **real** Create-Project operation (request schema = the safe `CreateProject` fields; `202 AcceptedCommand`; `400 ValidationFailure`; `401/403/404 SafeAuthorizationDenial`; `409 IdempotencyConflict`; `Idempotency-Key` required + `x-hexalith-idempotency-equivalence` listing the canonical equivalence fields; `x-hexalith-correlation`), and add/promote a minimal **`GetProject`** query (`/api/v1/projects/{projectId}`, `X-Hexalith-Freshness` response header + freshness body, `401/403/404 SafeAuthorizationDenial`, no `Idempotency-Key` param). Keep examples synthetic/payload-free (cross-check `PayloadClassification.ForbiddenContent`); camelCase; ISO-8601. Regenerate the client + helpers (the MSBuild `BeforeCompile` targets do this) — **never hand-edit `.g.cs`**; the provenance hashes update.
  - [x] Wire the Server: a `/api/v1/projects` POST endpoint that maps the request → `CreateProject`, submits through the **EventStore command pipeline** (mirror the Folders `Server` domain-request-handler / `*DomainProcessor` / `/process` aggregate-callback pattern — `Hexalith.Folders/src/Hexalith.Folders.Server/`), returns **`202 AcceptedCommand`** on accept, and maps rejection/denial to RFC 9457 ProblemDetails with **safe-denial 404** for unauthorized/nonexistent. Tenant authority comes from authenticated claims / EventStore claim-transform **only** — never from payload/header/query (this story uses a minimal guard sufficient to prove fail-closed; the full `TenantAccessProjection`/claim-transform chain is Story 1.6). Add the minimal `GetProject` read endpoint over `ProjectDetailProjection`.
  - [x] Add the EventStore command/aggregate/projection package references the Server (and domain core where pure) needs, via the `$(Hexalith*Root)` root-detection `ProjectReference` pattern (Stories 1.1/1.2). **Domain core stays pure** — only the Server takes Dapr/EventStore-runtime deps. Confirm Dapr remains the only infra abstraction (no direct Redis/Postgres/Cosmos/broker).

- [x] **Task 6 — Tests: Tier-1 aggregate/projection/validator + `NoPayloadLeakage` harness + schema-evolution corpus + cross-tenant isolation + Server Tier-2 + spine/gates** (AC: 1, 2, 3, 4, 5, 6, 7)
  - [x] **Tier-1 (`tests/Hexalith.Projects.Tests`, xUnit v3 + Shouldly):** `ProjectAggregate.Handle` happy path emits exactly one `ProjectCreated` with `Active`; missing/unauthorized tenant → `ProjectCreationRejected` (reason from shared `ReferenceState`, no exception, result never mixes success+rejection); blank name / secret / local-path / foreign-payload setup → rejected with field NAME only (no value echo); duplicate create rejected; idempotent replay vs idempotency conflict. `ProjectStateApply` applies `ProjectCreated` → `Active`, rejects foreign-tenant event, dedupes replay, throws on unknown event. `ProjectListProjection`/`ProjectDetailProjection` apply `ProjectCreated`, tenant-guard a foreign event, deterministic ordering, throw on unknown event. **Cross-tenant isolation negative test (FS-8/SM-3):** a `ProjectCreated` in tenant A never appears in a tenant-B list/detail query. Use EventStore/Testing + Tenants/Testing fakes/builders before inventing doubles. All pure (no Dapr/Aspire/network/containers/browser).
  - [x] **`NoPayloadLeakage` harness (FS-2):** author a reusable Tier-1 guard (`src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs`) that serializes any event/DTO and asserts no `PayloadClassification.ForbiddenContent` category appears; apply it to `ProjectCreated` + `ProjectCreationRejected`. Design it for reuse by later epics (don't inline a one-off).
  - [x] **Schema-evolution golden corpus (FS-5):** freeze a serialized JSON sample of `ProjectCreated` + `ProjectCreationRejected` under `tests/.../SchemaEvolution/` and a test that deserializes the frozen bytes via the production `System.Text.Json` converters and round-trips (asserts additive/tolerant deserialization; no `V2`). Normalize line endings before comparison.
  - [x] **Server Tier-2 (`tests/Hexalith.Projects.Server.Tests`):** the POST endpoint returns `202 AcceptedCommand` on a valid create; maps a fail-closed denial to **404** (not 500, not 200); the minimal `GetProject` read returns the projected detail + freshness after the projection updates. Use Dapr-slim / EventStore in-memory test infrastructure where a real boundary is needed (Tier-2) — keep Tier-1 pure.
  - [x] **Spine/gates:** extend the `Contracts.Tests` `OpenApi` class to assert the real `CreateProject`/`GetProject` shapes (202, safe-denial 404, idempotency required-on-mutation/rejected-on-query, freshness on GET, examples payload-free); confirm `ClientGenerationTests` still pass (regenerated `.g.cs` reproducible, LF, no path/host/token leakage, provenance current). Run `dotnet build Hexalith.Projects.slnx` (0 warnings) + `tests/tools/run-filtered-tests.ps1` + `run-openapi-fingerprint-gate.ps1` + `run-frontcomposer-inspect-gate.ps1` — all green. Add the new Server.Tests (and any SchemaEvolution/leakage assets) to the filtered lane if appropriate.

## Dev Notes

### What Stories 1.1 + 1.2 + 1.3 left you (verified on-disk reality — fill, don't recreate)
- **Empty landing folders** (verified present, `.gitkeep`-or-empty): `src/Hexalith.Projects.Contracts/Commands/` (← `CreateProject`), `src/Hexalith.Projects/Aggregates/Project/` (← aggregate/state/result/validator), `src/Hexalith.Projects/Projections/` (← list/detail projections). The core `Hexalith.Projects` project exists and compiles; the `Server` project is a **minimal ASP.NET Core skeleton** (`src/Hexalith.Projects.Server/Program.cs` has only a `/health` MapGet + `ProjectsServerModule.cs` marker) — extend it, do not rewrite.
- **Story 1.2 gives you (reuse, never re-mint):** `Contracts/Identifiers/ProjectId.cs` (`sealed record`, opaque validated string, `[JsonConverter]`, no `Guid.TryParse`); `Contracts/Identifiers/ProjectIdentity.cs` (canonical `{tenant}:projects:{projectId}` → `ActorId`/`StateStoreKey`/`PubSubTopic`/`ProjectionKey(name)`/`SignalRGroup`/`LogScope`, wrapping EventStore `AggregateIdentity`); the **single shared vocabulary** under `Ui/` (`ProjectLifecycle{Active,Archived}`, `ReferenceState{Included..InvalidReference}`, `ResolutionResult`, `ProjectReasonCode`, all name-based JSON + `[ProjectionBadge]`); the **six rejection events** under `Events/` (`ProjectCreationRejected(TenantId, ReferenceState Reason, RejectedField?, CorrelationId?) : IRejectionEvent` is the one this story emits — **reuse it**); `Models/PayloadClassification.cs` (`SafeFields` allowlist + `ForbiddenContent` denylist + `IsSafe`/`IsForbidden` + `TaxonomyDocumentPath`) — the FS-2 harness asserts against `ForbiddenContent`. `Contracts` already references `Hexalith.EventStore.Contracts` + `Hexalith.FrontComposer.Contracts` as `ProjectReference`s.
- **Story 1.3 gives you:** the OpenAPI spine `Contracts/openapi/hexalith.projects.v1.yaml` already carries a **seed `CreateProject` POST** (`operationId: CreateProject`, `CreateProjectRequest` schema, `x-hexalith-idempotency-equivalence`) and a seed `GetProjectLifecycleStatus` GET — this story **grows them into the real contract** (the full envelope: AcceptedCommand/ProblemDetails/SafeAuthorizationDenial/IdempotencyConflict/Freshness is already authored). The NSwag generation (`nswag.json`, MSBuild `BeforeCompile` targets, `$(NSwagExe_Net100)`), the ported `HexalithIdempotencyHasher`, the `Generation/` spine→helpers generator, and `HexalithProjectsGeneratedArtifacts` provenance/fingerprint are all in place — **edit the spine + regenerate, never hand-edit `Generated/*.g.cs`**. `run-openapi-fingerprint-gate.ps1` is a **real gate now** (drift → fail); `run-frontcomposer-inspect-gate.ps1` is input-presence-gated (skip-clean unless you add `[Projection]`/`[Command]` annotations). `Directory.Packages.props` pins `Newtonsoft.Json 13.0.4`, `NSwag.MSBuild 14.7.1`, `YamlDotNet 17.1.0`, `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.3` + the xUnit v3/Shouldly/NSubstitute test stack.

### Copy the Folders vertical slice — do NOT reinvent (primary anti-pattern to prevent)
`Hexalith.Folders` is the canonical, fully-implemented aggregate→event→projection→server vertical slice. The architecture explicitly says **"Mirror, don't reinvent."** Read and mirror these sibling files; they are the exact pattern for each task:
- **`Hexalith.Folders/src/Hexalith.Folders/Aggregates/Folder/FolderAggregate.cs`** — the pure static `Handle(state, CreateFolder, occurredAt)`: validate → idempotency-replay/conflict check → existence guard → emit `[FolderCreated]` → `FolderResult.Accepted`; the `Handle(state, command)` → `MinValue` test overload. **This is the `ProjectAggregate.Handle(CreateProject)` template.**
- **`.../Aggregates/Folder/CreateFolder.cs`** — `sealed record … : IFolderCommand` with `CommandType => nameof(...)` + envelope fields (`ActorPrincipalId`/`CorrelationId`/`TaskId`/`IdempotencyKey`). Template for `CreateProject`.
- **`.../Aggregates/Folder/FolderCreated.cs`** — `sealed record … : IFolderEvent` metadata-only success event. Template for `ProjectCreated`.
- **`.../Aggregates/Folder/FolderState.cs` + `FolderStateApply.cs`** — `Empty`, `Apply(events, expectedStreamName)`, the `IdempotencyFingerprints` replay-dedup map, the **foreign-stream guard** (`TenantMismatch` on identity mismatch), **throw-on-unknown-event**. Templates for `ProjectState`/`ProjectStateApply`.
- **`.../Aggregates/Folder/FolderResult.cs`** — `Accepted(...)`/`Rejected(...)` factories + `SafePassthrough` (rejection never echoes unsafe input). Template for `ProjectResult` (reuse `ReferenceState` for reasons rather than minting a parallel code enum where possible).
- **`.../Aggregates/Folder/FolderCommandValidator.cs` + `FolderCommandValidationResult.cs`** — pure boundary validation + canonical idempotency fingerprint. Template for `ProjectCommandValidator` (add the secret/path/foreign-payload checks FR-19 requires, naming the field only).
- **`.../Projections/FolderList/FolderListProjection.cs` + `FolderListItem.cs` + `FolderProjectionEnvelope.cs`** — private-ctor-over-`FrozenDictionary`, `Empty`, deterministic-ordered `Apply`, **envelope/event tenant-agreement guard**, throw-on-unknown-event. Templates for `ProjectListProjection`/`ProjectDetailProjection`.
- **`Hexalith.Folders/src/Hexalith.Folders.Server/`** (domain-request-handler / `*DomainProcessor` / `/process` callback / endpoint mapping / ProblemDetails) — the Server command-async + `/process` aggregate-callback wiring template for Task 5. Mirror its 202-on-accept and safe-denial-404 mapping.
- **`Hexalith.Folders/tests/Hexalith.Folders.Tests/`** — Tier-1 aggregate/state/projection test patterns (Shouldly, EventStore/Testing fakes). **`Hexalith.Folders.IntegrationTests`/`Server.Tests`** for the Tier-2 endpoint pattern.

### Architecture compliance (guardrails — enforced fully here for the create slice)
- **EventStore is the sole write authority; persist-then-publish; pure `Handle`/`Apply`** [AR-3, architecture.md#Communication Patterns L412, #Enforcement L441]. `Handle` returns events (pure); `Apply` mutates in-memory state only; events persist before publish; the read model is never written as authority. Route the write **only** through the EventStore command pipeline (Server `/process`).
- **Domain rejections are events, not exceptions** [project-context.md, AR-3, FS-4]. Missing/unauthorized tenant → `ProjectCreationRejected` (an `IRejectionEvent`), never a thrown exception; infrastructure failures (not this slice's concern) are the exception/dead-letter path. One `DomainResult` never mixes success + rejection.
- **Canonical identity + tenant isolation at every layer** [AR-4, FS-3, FS-8, NFR-1, architecture.md#Process Patterns L423]. Derive stream/state/projection/topic identity **only** from `ProjectIdentity` (Story 1.2); tenant authority from authenticated claims/EventStore claim-transform **only** — never payload/header/query. Project data uses the **user-facing tenant as the EventStore envelope tenant** (vs Tenants' `system`). The foreign-tenant `Apply`-guard + the cross-tenant isolation negative test are the SM-3 acceptance proof.
- **Metadata-only everywhere; additive/tolerant schema** [NFR-2, NFR-6, FS-1/2/5, architecture.md#Anti-patterns L470]. `ProjectCreated`/`ProjectCreationRejected` carry no transcript/file/memory/prompt/secret/token/command-body/path. No `V2` events; the golden corpus enforces backward-compatible deserialization. Logging is structured metadata only (ids/reason codes/correlation/freshness) — never a setup body or path.
- **Command-async + safe-denial RFC 9457** [AR-16, architecture.md#Format Patterns L401]. Mutation → `202 AcceptedCommand`, no read-after-write; unauthorized vs nonexistent both → `404`; a generic 500 for a fail-closed denial is a named anti-pattern.
- **One vocabulary, no parallels** [AR-18, UX-DR5, architecture.md#Anti-patterns L469]. Reuse Story 1.2 `ProjectLifecycle`/`ReferenceState` — never mint a parallel `ProjectStatus`/`ErrorKind` enum or magic strings. Where a `ProjectResultCode` is genuinely needed for aggregate-internal results (replay/conflict/created), keep it minimal and aligned with the shared vocabulary's semantics; prefer `ReferenceState` for rejection reasons surfaced externally.
- **Boundary direction** [project-context.md#Code Quality, architecture.md#Architectural Boundaries]. `Contracts` low-dependency; domain core `Hexalith.Projects` **pure** (no Dapr/network/ACL/HTTP — do NOT call the Folders client from the aggregate, a named anti-pattern; auto-folder is deferred); `Server` owns Dapr/EventStore-runtime + endpoint + ProblemDetails; `Client`/`Cli`/`Mcp` never reference domain event types or Dapr; **edit the spine/generator, never `.g.cs`.**
- **No unbounded conversation storage** [Pattern A, architecture.md#Anti-patterns L466]. This story stores only the created project's own metadata — no `ConversationId[]`, no reference sets (those are Epic 2). Use partial classes (`ProjectAggregate.cs`) so Epic 2/4 add `.References.cs`/`.Resolution.cs` without churn.

### Library / framework requirements
- **.NET 10** / `netstandard2.0`-safe Contracts; nullable + implicit usings + warnings-as-errors; `LangVersion latest`; prefer `sealed` records. **Central Package Management only — no inline `Version=`**; reuse sibling-pinned versions verbatim. Likely new package needs (verify exact pins in `Hexalith.Folders/Directory.Packages.props` + `Hexalith.EventStore/Directory.Packages.props` and add to root `Directory.Packages.props`): the EventStore aggregate/command/projection/server packages the Server needs for the command pipeline + `/process` callback. **Do not** add Dapr/EventStore-runtime deps to `Contracts` or domain core — only to `Server`.
- **`System.Text.Json`** only (no Newtonsoft in domain/contracts; `Newtonsoft.Json` is the NSwag-generated-client serializer only). Events/commands use the EventStore/Folders `System.Text.Json` convention; `ProjectId` already has its converter; vocabulary enums are name-based.
- **xUnit v3 + Shouldly + NSubstitute** (match EventStore/Tenants/Folders; pins already in `Directory.Packages.props`). Tier-1 pure; Tier-2 Server tests may use Dapr-slim / EventStore in-memory test infra. **Do not** use xUnit v2 (Parties pattern). Use EventStore/Testing + Tenants/Testing fakes/builders before inventing doubles.
- **Idempotency**: reuse the Story 1.3 `HexalithIdempotencyHasher` canonical fingerprint contract for the `x-hexalith-idempotency-equivalence` field list; same-key/same-payload = replay, same-key/different-payload = conflict (mirror `FolderAggregate`). `Idempotency-Key` required on the mutation, rejected on the query.
- **Do not** add/upgrade Fluent UI, Dapr, Aspire, Roslyn (`Microsoft.CodeAnalysis.CSharp` pinned `4.12.0`), Fluxor, or the SDK. Reference only what the slice needs to compile + the already-pinned stack.
- **Identifier rule (R2-A7):** never `Guid.TryParse` a `ProjectId`/aggregate id — ULID-shaped non-whitespace strings.

### File / structure requirements (target — fills the Story 1.1 skeleton)
```text
src/Hexalith.Projects.Contracts/
├── Commands/CreateProject.cs                      # NEW — imperative command (no Command suffix), IProjectCommand
├── Events/ProjectCreated.cs                       # NEW — past-tense success event, metadata-only
├── Events/ProjectCreationRejected.cs              # EXISTS (Story 1.2) — reuse for rejection path
├── Identifiers/ProjectId.cs, ProjectIdentity.cs   # EXIST — reuse
├── Ui/ (ProjectLifecycle, ReferenceState, ...)    # EXIST — reuse
├── Models/PayloadClassification.cs                # EXISTS — FS-2 harness asserts against ForbiddenContent
├── Queries/GetProject*.cs                          # NEW (if a query DTO is needed for the minimal read)
└── openapi/hexalith.projects.v1.yaml              # MODIFY — grow seed CreateProject/GetProject → real contract
src/Hexalith.Projects/                              # domain core — PURE
├── Aggregates/Project/
│   ├── ProjectAggregate.cs                        # NEW — pure static Handle(CreateProject)
│   ├── ProjectState.cs  ProjectStateApply.cs      # NEW — Empty/Apply, identity guard, replay dedup, throw-on-unknown
│   ├── ProjectResult.cs  ProjectResultCode.cs     # NEW — Accepted/Rejected + SafePassthrough
│   ├── ProjectCommandValidator.cs (+Result)       # NEW — pure FR-19 validation, field-name-only rejections
│   └── IProjectCommand.cs  IProjectEvent.cs        # NEW — markers (mirror IFolderCommand/IFolderEvent)
└── Projections/
    ├── ProjectList/ (Projection, Item, Envelope)  # NEW — tenant-scoped, deterministic, tenant-guarded
    └── ProjectDetail/ (Projection, Item)          # NEW — minimal GetProject read model
src/Hexalith.Projects.Client/Generated/*.g.cs      # REGENERATED (never hand-edited) — spine grew
src/Hexalith.Projects.Server/                       # host wiring — Dapr/EventStore runtime + endpoint + ProblemDetails
├── Program.cs                                     # MODIFY — wire command pipeline + endpoints (extend skeleton)
├── (DomainRequestHandler / DomainProcessor / /process)  # NEW — mirror Folders.Server
docs/
├── event-catalog.md                               # NEW (or append) — ProjectCreated/ProjectCreationRejected entries (AR-6)
tests/Hexalith.Projects.Tests/                      # Tier-1: aggregate/state/projection/validator + cross-tenant + leakage + schema-evolution
tests/Hexalith.Projects.Server.Tests/               # Tier-2: 202 endpoint, safe-denial 404, GetProject read
tests/Hexalith.Projects.Contracts.Tests/OpenApi/    # extend: real CreateProject/GetProject spine shape
```
- File-scoped namespaces under `Hexalith.Projects.*` matching folder path. **CRLF** on hand-written `.cs`, UTF-8, final newline (Stories 1.1–1.3 reviews fixed LF violations — write CRLF from the start). **Generated `.g.cs` stay LF** (NSwag convention; Client tests assert LF). Private fields `_camelCase`; interfaces `I`-prefixed; `Async` suffix on async methods; prefer `sealed`. Command imperative no `Command` suffix; event past-tense no `Event` suffix.

### Testing requirements
- **Tier-1 pure** (`Hexalith.Projects.Tests`, `Contracts.Tests`): aggregate `Handle`/`Apply`, projections, validator — no Dapr/Aspire/network/containers/browser (project-context.md#Testing Rules). **Tier-2** (`Server.Tests`): the endpoint + pipeline may use Dapr-slim / EventStore in-memory test infra (a real boundary). Keep the two tiers separate; the filtered fast lane is Tier-1/Contracts/Client + the pure Server unit tests.
- **Mandatory negative-path coverage** (architecture.md#Pattern Enforcement L450, project-context.md): **cross-tenant isolation** (FS-8/SM-3 — A never leaks into B), **`NoPayloadLeakage`** (FS-2 — no forbidden field in event/log/DTO), **fail-closed** (missing tenant → rejection event + 404, not 500/200), **idempotency-by-construction** (same-key replay no second event; same-key/different-payload conflict), **schema-evolution** (FS-5 golden round-trip), **safe-denial** (unauthorized ≡ nonexistent at the HTTP surface). These are ACs, not optional.
- **Reusable harness, not one-offs:** author `NoPayloadLeakage` as a reusable Tier-1 guard (the FS-2 harness later epics extend) and the schema-evolution corpus as a directory of frozen golden files round-tripped in CI (FS-5).
- **Determinism:** no wall-clock/random in Tier-1; use the `Handle(state, command)` → `MinValue` overload (Folders pattern) and inject `OccurredAt`; normalize line endings before golden-file comparison.
- **The projection-rebuild determinism + duplicate-command/duplicate-projection-delivery idempotency proof suite is Story 1.5** — do not build it here; just keep `Apply` deterministic and idempotent-tolerant so 1.5 can prove it cheaply on this event set.

### Project Structure Notes
- **Alignment:** Targets match architecture.md#Complete Project Directory Structure exactly (`Aggregates/Project/` = `ProjectAggregate, ProjectState, StateApply, …`; `Projections/` = list/detail handlers; `Server/` = API, `/process`, projection dispatch, auth, ProblemDetails). No structural deviation expected.
- **Decisions the dev must make + document in the Dev Agent Record:**
  1. **`IProjectCommand`/`IProjectEvent` marker location** — Contracts (so Server/Client see the command shape) vs domain-core (so events stay domain-internal). Folders keeps `IFolderCommand`/`IFolderEvent` in domain-core `Aggregates/Folder/`; commands the API binds may need to be in Contracts. Decide and note; keep events metadata-only regardless.
  2. **`ProjectResultCode` vs reuse of `ReferenceState`** — prefer `ReferenceState` for externally-surfaced rejection reasons (no parallel enum); a small aggregate-internal result code (Created/IdempotentReplay/IdempotencyConflict/DuplicateProject/ValidationFailed) is acceptable for `Handle`'s control flow if it maps cleanly to the shared vocabulary at the boundary. Document the mapping.
  3. **The exact EventStore command-pipeline/`/process` packages + Server wiring** — verify the precise EventStore aggregate/command/projection/server package names + the `*DomainProcessor`/`/process` callback shape against `Hexalith.Folders.Server` before authoring; mirror it.
- **Variances to flag:** (1) Server moves from the 1.1 `/health`-only skeleton to a real command-async host — note the new EventStore-runtime `ProjectReference`s + any new central package pins (reused from EventStore/Folders). (2) If a new event-catalog/`docs/event-catalog.md` file is created, note it. (3) Generated `.g.cs` on LF vs repo CRLF (documented exception, matches Folders + nswag config).
- **Submodule discipline:** root-level submodules only; never `--recursive`; never modify sibling submodule pointers. All new files live under umbrella-root Projects-module paths (`src/`, `tests/`, `docs/`).

### References
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4: Create Project (end-to-end tracer bullet)] — the four BDD acceptance criteria (happy-path create→`ProjectCreated`→projections + 202 + no payload dup; fail-closed→`ProjectCreationRejected`; FR-19 setup validation + no-folder-OK; FS-2 leakage + FS-5 schema corpus).
- [Source: _bmad-output/planning-artifacts/epics.md#Functional Requirements (FR-1, FR-19)] — Create Project (tenant + name only required, optional description/setup/refs, lifecycle Active, fail-closed on missing/unauthorized tenant, no payload duplication, auto-folder optional); Validate Project Setup (reject secrets/paths/foreign payloads/unsupported refs; structured field-named errors without echoing values).
- [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements] — AR-3 (EventStore sole write authority, one `ProjectAggregate`, pure Handle/Apply, persist-then-publish, rejection events, no mixed DomainResult), AR-4 (canonical `{tenant}:projects:{projectId}`, user-facing tenant as envelope tenant), AR-5/AR-6 (`CreateProject`/`ProjectCreated`/`ProjectCreationRejected` + event-catalog entry rule), AR-8 (list/detail projections, tenant-scoped/rebuildable/freshness-bearing), AR-15/AR-16 (OpenAPI spine→client, 202 AcceptedCommand, RFC 9457 + safe-denial 404), AR-20 (Dapr-only infra).
- [Source: _bmad-output/planning-artifacts/epics.md#Cross-Cutting Foundational Slices (Epic 1)] — FS-2 (`NoPayloadLeakage` CI harness, built here, extended every epic), FS-3 (canonical identity-derivation, reuse Story 1.2 helper), FS-5 (schema-evolution regression corpus — frozen golden files round-tripped in CI), FS-8 (cross-tenant isolation conformance / SM-3 acceptance test, established here).
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1 decomposition (Step 3 guidance)] — 1.4 = the tracer-bullet vertical (scaffold + spine + enums + `Handle(CreateProject)`/`Apply(ProjectCreated)` + one rejection + list/detail projection + CI green, ships `CreateProject` end-to-end); partial-classes-per-concern so Epics 2/4 don't churn `ProjectAggregate`.
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication/Format/Process/Enforcement Patterns (L401-472)] — persist-then-publish; pure Handle/Apply; rejection events; 202 AcceptedCommand + no read-after-write; RFC 9457 + safe-denial 404; one shared vocabulary; derive all keys from canonical identity; route writes only through the command pipeline; never call sibling clients from aggregate/projection; never hand-edit `.g.cs`; mandatory negative-path tests (cross-tenant, NoPayloadLeakage, idempotency, rebuild); event-catalog entry per new event.
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure (L497-560)] — `Aggregates/Project/` (`ProjectAggregate, ProjectState, StateApply`), `Projections/`, `Server/` (`ProjectsDomainServiceRequestHandler.cs`, `ProjectsDomainProcessor.cs`, `/process`, ProblemDetails), domain core purity.
- [Source: _bmad-output/implementation-artifacts/1-1-module-scaffold-build-ci-wiring.md] — scaffold reality (empty Aggregates/Projections folders, Server `/health` skeleton, `$(Hexalith*Root)` root-detection, CRLF requirement, central package management, gate scripts).
- [Source: _bmad-output/implementation-artifacts/1-2-shared-vocabulary-identifiers-payload-taxonomy.md] — `ProjectId`/`ProjectIdentity`, shared `Ui/` vocabulary, `ProjectCreationRejected` (reuse), `PayloadClassification.ForbiddenContent` (FS-2 source of truth), no-`Guid.TryParse` rule.
- [Source: _bmad-output/implementation-artifacts/1-3-openapi-contract-spine-generated-typed-client.md] — the spine (seed `CreateProject`/`GetProjectLifecycleStatus` to grow), NSwag generation + `$(NSwagExe_Net100)`, `HexalithIdempotencyHasher`, `HexalithProjectsGeneratedArtifacts` provenance/fingerprint gate (real now), `Newtonsoft.Json`/`NSwag.MSBuild`/`YamlDotNet` pins, LF-generated/CRLF-handwritten convention, never-hand-edit-`.g.cs`.
- [Source: Hexalith.Folders/src/Hexalith.Folders/Aggregates/Folder/FolderAggregate.cs · CreateFolder.cs · FolderCreated.cs · FolderState.cs · FolderStateApply.cs · FolderResult.cs · FolderCommandValidator.cs] — the canonical aggregate/command/event/state/result/validator vertical-slice templates to mirror (pure Handle, persist-then-publish, idempotency replay/conflict, foreign-stream guard, throw-on-unknown, SafePassthrough, field-name-only rejection).
- [Source: Hexalith.Folders/src/Hexalith.Folders/Projections/FolderList/FolderListProjection.cs · FolderListItem.cs] — the tenant-scoped, deterministic, tenant-guarded, throw-on-unknown read-model projection template for `ProjectListProjection`/`ProjectDetailProjection`.
- [Source: Hexalith.Folders/src/Hexalith.Folders.Server/FoldersDomainServiceEndpoints.cs · FoldersDomainServiceRequestHandler.cs · FolderDomainProcessor.cs · FolderCommandRejected.cs] — the exact Server command-async + `/process` aggregate-callback wiring + endpoint mapping + 202-on-accept + safe-denial-404 / rejection-to-ProblemDetails mapping template for Task 5 (mirror these file shapes for `Projects`).
- [Source: _bmad-output/project-context.md#Framework-Specific / Testing / Code Quality / Critical Don't-Miss Rules] — Dapr-only infra; EventStore identity model + envelope ownership; persist-then-publish; pure Handle/Apply; rejections are events not exceptions; tenant isolation at every layer; metadata-only logging; additive/no-`V2`; never hand-edit `.g.cs`; central package management; xUnit v3 + Shouldly; reuse EventStore/Tenants Testing fakes; boundary direction (Contracts low-dependency, domain core pure, Server owns runtime, Client/Cli/Mcp no domain-events/Dapr).

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context) — `claude-opus-4-7[1m]`

### Debug Log References

- `dotnet build Hexalith.Projects.slnx` → **0 Warning(s), 0 Error(s)**.
- `tests/tools/run-filtered-tests.ps1` → exit 0. Contracts.Tests 111, Client.Tests 16, Tests (Tier-1) 50, Server.Tests (Tier-2) 11 — **188/188 pass**.
- `tests/tools/run-openapi-fingerprint-gate.ps1` → **PASSED** (regenerated client + provenance match the grown spine).
- `tests/tools/run-frontcomposer-inspect-gate.ps1` → **SKIPPED (clean)** — no `[Projection]`/`[Command]` attribute annotations added (the `IProjectCommand`/`IProjectEvent` markers are interfaces, not FrontComposer attributes), so input-presence skip is correct for this story.

### Completion Notes List

- **Tracer bullet complete end-to-end:** `CreateProject` command → pure `ProjectAggregate.Handle` → `ProjectCreated` (metadata-only success event) → persist-then-publish via the Server `/process` `ProjectsDomainProcessor` → `ProjectState`/`ProjectStateApply` (identity-guarded, replay-deduped, throw-on-unknown) → `ProjectListProjection` + `ProjectDetailProjection` (tenant-guarded, deterministic) → `POST /api/v1/projects` returns `202 AcceptedCommand`, confirmable via the minimal `GET /api/v1/projects/{projectId}` read with `X-Hexalith-Freshness`.
- **Fail-closed + safe-denial proven:** missing/unauthorized tenant → `ProjectCreationRejected` (`IRejectionEvent`, shared `ReferenceState` reason, never an exception, result never mixes success+rejection — enforced by EventStore `DomainResult`); the HTTP surface maps every denial (unauthenticated, reserved-tenant, gateway 401/403/404, cross-tenant read) to **safe-denial 404**, never 500/200, never echoing existence.
- **FR-19 setup validation:** `ProjectCommandValidator` rejects blank name, secrets/tokens, unrestricted/local paths, foreign-context payloads — returning the **field NAME only** (never the value); cross-checked against `PayloadClassification.ForbiddenContent`. No-folder create succeeds (auto-folder deferred; aggregate never calls a sibling client).
- **FS-2 `NoPayloadLeakage` harness** authored as a reusable Tier-1 guard in `Hexalith.Projects.Testing` (extended by later epics, not a one-off) and applied to `ProjectCreated` + `ProjectCreationRejected` + a log-scope rendering.
- **FS-5 schema-evolution golden corpus:** frozen `ProjectCreated.v1.json` + `ProjectCreationRejected.v1.json` deserialize via the production `System.Text.Json` converters and round-trip; an additive-unknown-field tolerance test proves no `V2` is needed.
- **FS-8/SM-3 cross-tenant isolation** asserted at Tier-1 (projections) and Tier-2 (GetProject endpoint): a project in tenant A never appears in a tenant-B query.

**Decisions documented (Project Structure Notes):**
1. **Marker location:** `IProjectCommand` lives in `Contracts/Commands/` and `IProjectEvent` in `Contracts/Events/` (not domain-core). Rationale: the Server endpoint and the API bind the command shape, and `ProjectCreated` is already a Contracts event implementing EventStore `IEventPayload`; keeping both markers in Contracts avoids a domain-core ProjectReference from the Server for the command shape while events stay metadata-only.
2. **`ProjectResultCode` vs `ReferenceState`:** a minimal aggregate-internal `ProjectResultCode` (Created/IdempotentReplay/IdempotencyConflict/DuplicateProject/ValidationFailed/Unauthorized/TenantMismatch/StateTransitionInvalid) drives `Handle` control flow and maps to the shared `ReferenceState` at the boundary via `ProjectResult.ToRejectionReason()` (Unauthorized→Unauthorized, TenantMismatch→TenantMismatch, Duplicate/Conflict→Conflict, Validation/StateTransition→InvalidReference). No parallel externally-surfaced error enum.
3. **EventStore command-pipeline/`/process` wiring:** mirrors `Hexalith.Folders.Server` (`*DomainProcessor : IDomainProcessor` + gateway `SubmitCommandAsync` + RFC 9457 safe mapping) but stripped of the layered-authz/ACL/tenant-gate chain (deferred to Story 1.6). The endpoint logic is decoupled from Dapr via `IProjectCommandSubmitter` (production = `EventStoreProjectCommandSubmitter` over `IEventStoreGatewayClient`; Tier-2 = in-memory fake) and `IProjectDetailReadModel` (production-shaped `InMemoryProjectDetailReadModel` over the rebuildable projection).

**Variances flagged:**
- **Server moved from the 1.1 `/health`-only skeleton to a real command-async host.** New `ProjectReference`s added to `Hexalith.Projects.Server.csproj`: `Hexalith.EventStore.Client` + `Hexalith.EventStore.Contracts` (via `$(HexalithEventStoreRoot)` root-detection). No new central package pins were needed (the EventStore refs are ProjectReferences; no inline `Version=`). Dapr remains the only infra abstraction (gateway client only; no direct Redis/Postgres/Cosmos/broker).
- **New `docs/event-catalog.md`** created (AR-6) with `ProjectCreated` + `ProjectCreationRejected` entries.
- **Spine grown:** seed `CreateProject` POST kept its `CreateProjectRequest` schema + `x-hexalith-idempotency-equivalence` list **unchanged** (so the generated client + idempotency helper stay byte-stable); the seed `GetProjectLifecycleStatus` GET was grown into the real minimal `GetProject` query at `/api/v1/projects/{projectId}` returning a new closed `Project` detail schema with freshness. Client `.g.cs` + provenance were **regenerated** (never hand-edited); fingerprint gate passes.
- **`ProjectCreationRejected` extended additively** with an optional `ProjectId? ProjectId` (no `V2`) for create-path correlation; the Story 1.2 taxonomy tests still pass (the 4-positional-arg constructor call remains valid).
- **Generated `.g.cs` stay LF** (NSwag convention, Client tests assert LF); all hand-written `.cs` are CRLF (UTF-8, final newline).
- **Server.Tests added to the filtered fast lane** (`run-filtered-tests.ps1`) — its Tier-2 endpoint/processor tests use in-memory fakes (no real Dapr/infra), so they belong alongside the Tier-1 lane.

### File List

**New — Contracts (`src/Hexalith.Projects.Contracts/`):**
- `Commands/IProjectCommand.cs`
- `Commands/CreateProject.cs`
- `Events/IProjectEvent.cs`
- `Events/ProjectCreated.cs`

**Modified — Contracts:**
- `Events/ProjectCreationRejected.cs` (additive optional `ProjectId`)
- `openapi/hexalith.projects.v1.yaml` (grown CreateProject + GetProject)

**New — domain core (`src/Hexalith.Projects/`):**
- `Aggregates/Project/ProjectAggregate.cs`
- `Aggregates/Project/ProjectState.cs`
- `Aggregates/Project/ProjectStateApply.cs`
- `Aggregates/Project/ProjectResult.cs`
- `Aggregates/Project/ProjectResultCode.cs`
- `Aggregates/Project/ProjectCommandValidator.cs`
- `Aggregates/Project/ProjectCommandValidationResult.cs`
- `Projections/ProjectList/ProjectProjectionEnvelope.cs`
- `Projections/ProjectList/ProjectListItem.cs`
- `Projections/ProjectList/ProjectListProjection.cs`
- `Projections/ProjectDetail/ProjectDetailItem.cs`
- `Projections/ProjectDetail/ProjectDetailProjection.cs`

**Regenerated (never hand-edited) — Client (`src/Hexalith.Projects.Client/Generated/`):**
- `HexalithProjectsClient.g.cs`
- `HexalithProjectsIdempotencyHelpers.g.cs`

**New — Server (`src/Hexalith.Projects.Server/`):**
- `IProjectTenantContextAccessor.cs`
- `HttpContextProjectTenantContextAccessor.cs`
- `IProjectCommandSubmitter.cs`
- `EventStoreProjectCommandSubmitter.cs`
- `IProjectDetailReadModel.cs`
- `InMemoryProjectDetailReadModel.cs`
- `ProjectsDomainProcessor.cs`
- `ProjectsDomainServiceEndpoints.cs`
- `ProjectsServerServiceCollectionExtensions.cs`

**Modified — Server:**
- `Hexalith.Projects.Server.csproj` (EventStore Client/Contracts ProjectReferences)
- `ProjectsServerModule.cs` (domain/command-type/route/reserved-tenant constants)
- `Program.cs` (wire AddProjectsServer + MapProjectsServerEndpoints)

**New — Testing (`src/Hexalith.Projects.Testing/`):**
- `Leakage/NoPayloadLeakageAssertions.cs`

**New — tests (`tests/`):**
- `Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateHandleTests.cs`
- `Hexalith.Projects.Tests/Aggregates/Project/ProjectStateApplyTests.cs`
- `Hexalith.Projects.Tests/Aggregates/Project/ProjectCommandValidatorTests.cs`
- `Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs`
- `Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`
- `Hexalith.Projects.Tests/SchemaEvolution/SchemaEvolutionGoldenCorpusTests.cs`
- `Hexalith.Projects.Tests/SchemaEvolution/Golden/ProjectCreated.v1.json`
- `Hexalith.Projects.Tests/SchemaEvolution/Golden/ProjectCreationRejected.v1.json`
- `Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`
- `Hexalith.Projects.Server.Tests/ProjectsDomainProcessorTests.cs`

**Modified — tests:**
- `Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj` (golden corpus content)
- `Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj` (unchanged refs; tests added)
- `Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` (real GetProject/CreateProject shape assertions)
- `tests/tools/run-filtered-tests.ps1` (added Server.Tests to the fast lane)

**New — docs:**
- `docs/event-catalog.md`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — story-automator autonomous review on 2026-05-25
**Outcome:** Approved → Status `done`. Issues: 0 CRITICAL / 0 HIGH / 0 MEDIUM / 1 LOW (tracked). Auto-fixed: 0.

**Gates re-verified green:** `dotnet build Hexalith.Projects.slnx` → 0W/0E; `run-filtered-tests.ps1` → 188/188 (Contracts 111, Client 16, Tier-1 50, Server Tier-2 11); `run-openapi-fingerprint-gate.ps1` → PASSED; `run-frontcomposer-inspect-gate.ps1` → SKIPPED (clean).

**Acceptance-criteria validation (all 7 IMPLEMENTED + genuinely TESTED):**
- AC-1 — `ProjectAggregate.Handle` is pure (no state mutation / I/O / Dapr; returns events only); `ProjectStateApply` mutates only in-memory state; both projections reflect the create; `POST /api/v1/projects` → 202 AcceptedCommand; `GET /api/v1/projects/{id}` confirms with `X-Hexalith-Freshness`. `HandleIsPure_DoesNotMutateInputState` asserts purity.
- AC-2 — Rejections are `ProjectCreationRejected` (`IRejectionEvent`), never exceptions; `ProjectResult` is either Accepted-with-event or Rejected-with-reason (never both); the HTTP surface maps missing-tenant, gateway 401/403/404, and cross-tenant read to safe-denial 404 (never 500/200). Tested at Tier-1 (`ProjectAggregateHandleTests`) and Tier-2 (`CreateProjectEndpointTests`, `ProjectsDomainProcessorTests`).
- AC-3 — `ProjectCommandValidator` rejects secrets/tokens/local-paths/URLs/foreign payloads, returning the field NAME only (no value echo, verified by `ShouldNotContain(setup)`); no-folder create succeeds; the aggregate never calls a sibling client.
- AC-4 — `NoPayloadLeakageAssertions` is a reusable Tier-1 guard in `Hexalith.Projects.Testing` (asserts against `PayloadClassification.ForbiddenContent` + host-path/JWT/PEM shapes); golden corpus round-trips frozen `ProjectCreated`/`ProjectCreationRejected` bytes via production STJ converters and tolerates an additive unknown field (no V2).
- AC-5 — Spine grew the seed CreateProject/GetProject into the real contract (202; 400 ValidationFailure; 401/403/404 SafeAuthorizationDenial; 409 IdempotencyConflict; Idempotency-Key required-on-mutation/rejected-on-query; freshness on GET; camelCase; ISO-8601; payload-free examples; no client-controlled tenant field). `Client/Generated/*.g.cs` regenerated (LF, reproducible, NUL-free); fingerprint gate PASSED.
- AC-6 — All keys derived from `ProjectIdentity` (`{tenant}:projects:{projectId}`); `ProjectStateApply` + both projections enforce the foreign-tenant guard; FS-8/SM-3 cross-tenant isolation negative test genuinely asserts "A never appears in a B query" at Tier-1 and Tier-2.
- AC-7 — `net10.0` (Contracts netstandard2.0-safe), `Nullable`/`ImplicitUsings`/`TreatWarningsAsErrors` ON with no `NoWarn`/`#pragma`/`SuppressMessage` to force green; domain core pure (references only Contracts + DI.Abstractions); Contracts low-dependency; Server owns the only EventStore-runtime/Dapr deps; Central Package Management only; no sibling submodule pointer changes.

**LOW (tracked, not fixed):** `CreateProject.CommandType => nameof(CreateProject)` (`"CreateProject"`) diverges from the wire discriminator `ProjectsServerModule.CreateProjectCommandType` (`"Hexalith.Projects.Commands.CreateProject"`). The record property is unused on the command-async path (submitter and processor both use the FQN constant consistently), so this is a non-functional, cosmetic inconsistency. Left as-is to preserve the byte-stable generated client / fingerprint and the green build; mirrors the Folders `IFolderCommand` property convention. A future story may either consume or remove the vestigial property.
