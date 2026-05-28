---
baseline_commit: 0058ac35de777da6130803f620c7938d99979feb
---

# Story 2.7: Link/Unlink Memory

## Status

done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to link authorized memory references to a Project and unlink them**,
so that **relevant memories are part of Project Context without Projects storing memory payloads** _(FR-10, FR-11 memory; depends on Story 2.6; realizes UJ-1, UJ-3)_.

This is the **last Epic 2 implementation story**. It consumes the Story 2.6 decision spike (ADR `docs/adr/memories-link-target.md`, status `Accepted`) and extends the Story 2.5 optional-reference pattern (`LinkFileReference` / `UnlinkFileReference`) to the **memory** reference kind. Per the ADR, a single Project Memory link targets exactly one Hexalith.Memories `Case`, validated through `MemoriesClient.GetCaseAsync` (the only stable read route — "Stable since Story 10.2"). Story 2.7 must not invent new shared-vocabulary enum values, must not call any `[Experimental("HXL001")]` write method, must not store any `MemoryUnit.Content` / `ContentHash` / `SourceUri` / embedding / payload material, and must keep the single Project Folder rule and the Story 2.5 disjoint per-kind reference-index layout intact.

The `referenceKind` enum already reserves `memory` at both the request enum and the response `ProjectReferenceSummary.referenceKind` enum in `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`; that reservation is sufficient and must not be re-emitted. `MemoryMatched` is already a shared `ProjectReasonCode` (`src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs`); no new reason code is introduced. Story 2.7 lands the executable surface: commands, events, `MemoryReference` model, `IProjectMemoryDirectory` ACL implementation against the Memories typed client, endpoint handlers, OpenAPI route additions (link/unlink only — the enum is already in place), regenerated client + idempotency helpers, projection extensions, audit-safe rejection mappings, and `docs/event-catalog.md` entries.

## Acceptance Criteria

1. Projects exposes command-async mutations for linking and unlinking Memory References under an active Project. Mutations require `Idempotency-Key`, preserve `X-Correlation-Id` and `X-Hexalith-Task-Id`, validate route/body Project identity equality, use closed (camelCase, `additionalProperties: false`, `UnmappedMemberHandling.Disallow`) request schemas with `requestSchemaVersion: "v1"`, and return the existing `AcceptedCommand` 202 shape on accepted dispatch — exactly as Story 2.5 file-reference link/unlink does.

2. `LinkMemory` emits a metadata-only `MemoryLinked` event after the server has validated the referenced Memories `Case` through `IProjectMemoryDirectory.ValidateLinkMemoryReferenceAsync` (which calls `MemoriesClient.GetCaseAsync` — see ADR). The event records: managed tenant identity, stable Project identity, the opaque `memoryReferenceId` (= Memories `Case.Id`, ULID-shaped string per [[identifier-boundary]]), an optional safe `displayName` (derived from `Case.Name`), actor/correlation/task/idempotency metadata, the canonical idempotency fingerprint, and an `OccurredAt`. It carries **no** `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, `SourceType`, `IngestedBy`, `Metadata`, `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`, `Classification`, `FailureDetails`, raw `IngestionInput` text/bytes, embedding vectors, vector dimensions, fusion weights, search snippets, traversal payloads, raw `ErrorResponse.Message` / `Suggestion`, raw `MemoriesRemoteException.Message`, tokens, file paths, prompt fragments, transcript fragments, or the Memories-internal tenant identifier as a payload field (the envelope tenant is the only trusted source).

3. Memory References are an **optional bounded set** on `ProjectState` (per-Project cap `MaxMemoryReferences`, sized analogously to `MaxFileReferences` = 100 from Story 2.5). Linking a memory must not clear, replace, satisfy, or auto-create the single Project Folder; it must not affect existing file references or conversation links; it may only add/update a memory association for an already active Project whose Project Folder rule remains intact. A second link to the same `memoryReferenceId` with identical safe metadata is a logical idempotent replay; conflicting safe metadata under the same `memoryReferenceId` is a `MemoryReferenceConflict` rejection; reaching `MaxMemoryReferences` is a `MemoryReferenceLimitExceeded` rejection. None of those reject paths leak Memories payloads or raw upstream text.

4. `UnlinkMemory` emits a metadata-only `MemoryUnlinked` event that removes the Project-to-memory association only. It **never** calls any Memories method (no `GetCaseAsync`, no Memories writes ever), never deletes or mutates the underlying `Case` / `MemoryUnit` in Hexalith.Memories, and is safely idempotent: unlinking a memory that is not present is a `IdempotentReplay` no-op (matches Story 2.5 unlink semantics).

5. The Projects-owned ACL `IProjectMemoryDirectory` is implemented under `src/Hexalith.Projects.Server/Memories/`, mirroring the Story 2.4 / 2.5 ACL shape, exactly as the Story 2.6 ADR specifies:
   - `IProjectMemoryDirectory.ValidateLinkMemoryReferenceAsync(ProjectId projectId, string memoryReferenceId, string correlationId, string taskId, CancellationToken)`.
   - `ProjectMemoryValidationResult(ProjectMemoryValidationOutcome Outcome, string? CorrelationId)` with `static Accepted(...)` factory and outcomes `Accepted | ValidationFailed | Denied | Archived | Stale | TenantMismatch | Unavailable`.
   - `MemoriesProjectMemoryDirectory` calls **only** `MemoriesClient.GetCaseAsync(tenantId, caseId, ct)` and maps the typed result per the ADR table.
   - `UnavailableProjectMemoryDirectory` returns `Unavailable` when no Memories client is configured (fail-closed fallback, registered by `TryAddTransient` in `AddProjectsServer` exactly as `UnavailableProjectFileReferenceDirectory` is).
   - **The ACL is the only Projects file allowed to reference `Hexalith.Memories.Client.Rest` or `Hexalith.Memories.Contracts.V1` types.** Domain core (`src/Hexalith.Projects/**`), contracts (`src/Hexalith.Projects.Contracts/**`), generated client (`src/Hexalith.Projects.Client/**`), and tests under `tests/Hexalith.Projects.Tests/**` must not import any Memories namespace.

6. The dev agent **must HALT before coding** if the current working tree's Memories surface diverges from the ADR evidence: if `MemoriesClient.GetCaseAsync(string tenantId, string caseId, CancellationToken)` is no longer documented "Stable since Story 10.2" at `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:809-810`; if `CreateTenantAsync` / `CreateCaseAsync` / `IngestAsync` / `GetTelemetrySummaryAsync` drop `[Experimental("HXL001")]`; if a new content-bearing route now sits at the same path the ACL would use; or if `Case` / `CaseStatus` / `ErrorResponse` shapes diverge from the ADR. Record evidence in the Dev Agent Record before authoring any `.cs`.

7. `ProjectState.MemoryReferences` (bounded), `ProjectStateApply`, `ProjectDetailItem` / `ProjectDetailProjection`, `ProjectsDomainServiceEndpoints.ToProjectReferenceSummaries(...)`, and `ProjectReferenceIndexProjection` are all extended for memory references using bounded metadata-only models. Memory-reference projection rows use reference kind `memory`, shared `ReferenceState`, freshness metadata, and **per-kind disjoint** tenant/project/reference index key prefixes (Story 2.5 made folder/file lanes disjoint; Story 2.7 adds a third disjoint memory lane). Folder replacement must not remove memory rows; file unlink must not remove memory rows; memory unlink must not remove folder or file rows. Deterministic ordering by reference kind then reference id is preserved.

8. Idempotency is deterministic and **field-scoped**. The link fingerprint includes Project id, memory reference id, optional safe `display_name`, operation, and `request_schema_version`; the unlink fingerprint includes Project id, memory reference id, operation, `unlink_intent`, and `request_schema_version`. Cross-surface parity is mandatory: the server `ProjectCommandValidator` `ComputeLinkMemoryFingerprint` / `ComputeUnlinkMemoryFingerprint` output **must** equal the regenerated `HexalithProjectsIdempotencyHelpers.g.cs` output for the same inputs (the `ClientGenerationTests` cross-check pattern from Story 2.5 must be extended to cover the two new operations). Equivalent duplicate link/unlink requests replay safely; conflicting same-key requests reject as `IdempotencyConflict`.

9. Public contracts evolve only through `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`. The OpenAPI spine gains exactly two new operations — `POST /api/v1/projects/{projectId}/memories/{memoryReferenceId}/link` (operationId `LinkMemory`) and `DELETE /api/v1/projects/{projectId}/memories/{memoryReferenceId}` (operationId `UnlinkMemory`) — both carrying `x-hexalith-idempotency-key`, `x-hexalith-idempotency-equivalence`, `x-hexalith-correlation`, `x-hexalith-authorization`, and `x-hexalith-canonical-error-categories` blocks identical in shape to the Story 2.5 file-reference operations (substituting `memoryReferenceId` / `MemoryReference*` schemas / `link_memory` / `unlink_memory` permission tokens). The `referenceKind` enums (both request and `ProjectReferenceSummary.referenceKind`) already contain `memory` and are **not** re-emitted. Generated client (`src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs`) and idempotency helpers (`HexalithProjectsIdempotencyHelpers.g.cs`) are regenerated through the existing NSwag workflow; no `.g.cs` artifact is hand-edited.

10. `docs/event-catalog.md` is updated with `MemoryLinked`, `MemoryUnlinked`, the link rejection path (`ProjectReferenceLinkRejected` with `ReferenceKind == "memory"`), and the unlink rejection path (`ProjectReferenceUnlinkRejected` with `ReferenceKind == "memory"`). The catalog entries list payload fields exactly as emitted by the events (no extras), state sensitivity class `metadata-only`, and name the consumers (Projects projections + Epic 3 context assembly).

11. Authorization tokens `projects:link_memory` and `projects:unlink_memory` are added to `ProjectAuthorizationGate` with `AuthorizeLinkMemoryAsync` / `AuthorizeUnlinkMemoryAsync` helpers. Project mutation intent is gated **before** any Memories ACL call: unauthorized, hidden, archived, stale, or unavailable Project evidence does not reach `IProjectMemoryDirectory`. The `IProjectMemoryDirectory` is registered via `TryAddTransient` in `AddProjectsServer` (and the Memories typed client via `services.AddMemoriesClient(...)` in `AddProjectsServerRuntimeInfrastructure`, matching the Story 2.5 Folders pattern); the typed client is never registered as a singleton (Story 2.4 / 2.5 review fix for typed-client + bearer-handler chains).

12. The `[Experimental]` containment rule from the ADR is enforced verbatim: Story 2.7 ships **zero** `#pragma warning disable HXL001` / `HXL002` suppressions, and zero `[SuppressMessage]` attributes for `HXL001` / `HXL002`. The ACL never calls `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync` (HXL001), `ListHandlersAsync`, `GetHandlerMismatchesAsync` (HXL002), `GetMemoryUnitAsync`, `HybridSearchAsync`, `SearchAsync`, `TraverseAsync`, `ExportCaseAsync`, or `ExportTenantAsync`. A negative test under `tests/Hexalith.Projects.Server.Tests/` proves the ACL's request lane (via a recording `HttpMessageHandler` behind `MemoriesClient`) contains only `GET api/tenants/{tenant}/cases/{case}` paths and nothing else.

13. The Memories eventual-consistency rule from the ADR is enforced verbatim **in tests**: every Story 2.7 test under `tests/Hexalith.Projects.Tests/**`, `tests/Hexalith.Projects.Server.Tests/**`, `tests/Hexalith.Projects.Contracts.Tests/**`, `tests/Hexalith.Projects.Client.Tests/**`, and `tests/Hexalith.Projects.Integration.Tests/**` is free of `Thread.Sleep`, `Task.Delay`, `SpinWait.SpinUntil`, wall-clock retry loops, and `await Task.Yield()` patterns used as time-waits. Convergence is asserted via deterministic fakes (injected `IProjectMemoryDirectory` doubles), stubbed `HttpMessageHandler` behind `MemoriesClient`, or fake clocks — never real elapsed time. A targeted grep in the validation step proves this (zero hits for those tokens in new Story 2.7 test files).

14. Focused tests cover, at minimum:
    - **Aggregate (Tier 1, pure):** link initial memory reference, duplicate equivalent link → `IdempotentReplay`, conflicting safe metadata under the same id → `MemoryReferenceConflict`, multiple bounded memory references, `MaxMemoryReferences` boundary, unlink existing reference, unlink missing reference → `IdempotentReplay`, archived Project rejection, tenant mismatch, malformed unsafe memory identifiers, idempotency-key replay vs conflict.
    - **State/replay (Tier 1):** `MemoryLinked` / `MemoryUnlinked` replay deterministically, duplicate delivery is idempotent, unknown events still throw, unlink removes only the targeted memory reference.
    - **Projection (Tier 1):** `ProjectDetailProjection` exposes memory-reference summaries alongside folder and file references; `ProjectReferenceIndexProjection` adds/removes memory rows without touching folder/file rows; folder replacement does not remove memory rows; deterministic ordering preserved.
    - **Endpoint (Tier 2):** required `Idempotency-Key`, route/body project/memory identity mismatch → 400 before handler, missing/unknown fields rejected by closed JSON binding, archived/unauthorized Project denial **before** any Memories ACL call (recording-handler proof: no `GetCaseAsync` request issued on denial), Memories denied / stale / unavailable / archived mapping, 202 command-async accepted responses, unlink endpoint takes **no** `IProjectMemoryDirectory` dependency so it structurally cannot call Memories (mirror of the Story 2.5 `DeleteFile_…WithoutFoldersCall` test).
    - **Memories ACL (Tier 2):** accepted `GetCaseAsync` 200 + `Active` → `Accepted`; `Closed` / `Deleting` → `Archived`; `MemoriesRemoteException` 401/403/404 (incl. `TENANT_NOT_FOUND`, `CASE_NOT_FOUND`, `MEMORY_UNIT_NOT_FOUND`) → `Denied`; 408/503/5xx → `Unavailable`; 400/422/`INVALID_RESPONSE` → `ValidationFailed`; 409 → `ValidationFailed`; `HttpRequestException` / network → `Unavailable`; cancellation passthrough.
    - **OpenAPI / client (Tier 2):** link/unlink operations exist with correct route/idempotency/error-category blocks; generated client methods exist; mutation operations require `Idempotency-Key`; query endpoints reject `Idempotency-Key`; `HexalithProjectsIdempotencyHelpers.g.cs` helper-hash matches the server fingerprint for link/unlink memory.
    - **NoPayloadLeakage (Tier 1):** new command/event/request/response/problem/projection types scanned for the forbidden term list in AC 2 (extends `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`).
    - **Integration (Tier 2):** the existing `Hexalith.Projects.Integration.Tests` lane stays green and gains at minimum one end-to-end memory link/unlink trace exercising the Memories typed-client registration through `AddProjectsServerRuntimeInfrastructure`.

15. The story file is updated by the dev agent with a Dev Agent Record recording: the capability gate decision (PASS vs HALT), the chosen ACL route (`GetCaseAsync`), the `ProjectMemoryValidationOutcome` enum values realized in code, the `MaxMemoryReferences` value chosen, the regenerated `.g.cs` file digest summary, the focused-lane test counts (Tests / Server / Contracts / Client / Integration), the full-solution `dotnet test Hexalith.Projects.slnx` count, `dotnet build` warnings/errors, `git diff --check` result, and any HALT items. No commit is required from the dev agent; story-automator review will commit after auto-fixes.

## Tasks / Subtasks

- [x] **Task 1 — Capability gate before any code change. (AC: 5, 6, 12)**
  - [x] Read `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` and confirm: `GetCaseAsync(string tenantId, string caseId, CancellationToken ct = default)` is at `:810` and its XML doc carries `<remarks>Stable since Story 10.2.</remarks>` at `:809`. Confirm `[Experimental("HXL001")]` on `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync` and `[Experimental("HXL002")]` on `ListHandlersAsync`, `GetHandlerMismatchesAsync`.
  - [x] Read `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/Case.cs` and `CaseStatus.cs`. Confirm `Case(string Id, string TenantId, string Name, string? Description, CaseStatus Status, DateTimeOffset CreatedAt, DateTimeOffset LastUpdated, int MemoryUnitCount)` and `CaseStatus { Active, Closed, Deleting }` (the Story 2.6 ADR depends on these exact shapes).
  - [x] Read `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesRemoteException.cs` and confirm it carries `HttpStatusCode StatusCode` + `ErrorResponse Error` (used by the ACL mapping table).
  - [x] Read the Memories DI extension `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs::AddMemoriesClient` and confirm the registration shape mirrored by Story 2.4 / 2.5 Folders (transient auth handler + typed `HttpClient<MemoriesClient>`).
  - [x] Confirm no nested-recursive submodule init is required; the `Hexalith.Memories` submodule pointer must **not** advance as part of this story.
  - [x] **HALT** and surface a `HALT` block in the Dev Agent Record if any of the above evidence diverges from the Story 2.6 ADR or this story's Dev Notes. Do not proceed to Task 2.

- [x] **Task 2 — Add Projects contracts for memory link/unlink. (AC: 1, 2, 8, 9)**
  - [x] Add `LinkMemory` command record under `src/Hexalith.Projects.Contracts/Commands/LinkMemory.cs` mirroring `LinkFileReference` shape: `TenantId`, `ProjectId`, `MemoryReferenceId`, `MemoryMetadata` (= `ProjectMemoryReferenceMetadata`), `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`. `CommandType => nameof(LinkMemory)`. Implements `IProjectCommand`.
  - [x] Add `UnlinkMemory` command record under `src/Hexalith.Projects.Contracts/Commands/UnlinkMemory.cs`: `TenantId`, `ProjectId`, `MemoryReferenceId`, `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`. `CommandType => nameof(UnlinkMemory)`.
  - [x] Add `ProjectMemoryReference` model under `src/Hexalith.Projects.Contracts/Models/ProjectMemoryReference.cs` with: `MemoryReferenceId`, `DisplayName?`, `ReferenceState`, `ReasonCode?` (nullable string of `ProjectReasonCode` enum name to mirror `ProjectFileReference`), `ObservedAt`. Note: there is no `FolderId`-equivalent; the Memories owning context is the tenant (already implicit via envelope), so only the opaque case id is stored. Do not introduce an owning `caseId` / `tenantId` field on the model — they are implicit per the ADR identifier-shape table.
  - [x] Add `ProjectMemoryReferenceMetadata` model under `src/Hexalith.Projects.Contracts/Models/ProjectMemoryReferenceMetadata.cs` with `DisplayName?` only (mirrors `ProjectFileReferenceMetadata`).
  - [x] Add `MemoryLinked` event under `src/Hexalith.Projects.Contracts/Events/MemoryLinked.cs`: `TenantId`, `ProjectId`, `MemoryReferenceId`, `MemoryMetadata`, `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint`, `OccurredAt`. Implements `IProjectEvent`.
  - [x] Add `MemoryUnlinked` event under `src/Hexalith.Projects.Contracts/Events/MemoryUnlinked.cs`: `TenantId`, `ProjectId`, `MemoryReferenceId`, `ActorPrincipalId`, `CorrelationId`, `TaskId`, `IdempotencyKey`, `IdempotencyFingerprint`, `OccurredAt`. Implements `IProjectEvent`.
  - [x] Extend `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`:
    - Add the two operations at the route shape in AC 9.
    - Add the request schemas (`LinkMemoryRequest`, `UnlinkMemoryRequest`) with closed (`additionalProperties: false`) camelCase fields matching the command surface (`requestSchemaVersion`, `operation`, `unlinkIntent` for unlink, `projectId`, `memoryReferenceId`, `memoryMetadata.displayName?`).
    - Add `LinkMemoryRequest` / `UnlinkMemoryRequest` synthetic examples under `#/components/examples/...` (placeholder identifiers only — not real ULIDs).
    - Add a `MemoryReferenceId` path parameter under `#/components/parameters/` modeled on `FileReferenceId` (`OpaqueIdentifier`, max 80 chars, same allowed-character class).
    - Do **not** modify the existing `referenceKind` enums; `memory` is already reserved at both sites (verify in this working tree before saving).
    - Each new operation carries `x-hexalith-idempotency-equivalence` listing exactly the fingerprint fields in AC 8 (link: `memory_metadata.display_name`, `memory_reference_id`, `operation`, `project_id`, `request_schema_version`; unlink: `memory_reference_id`, `operation`, `project_id`, `request_schema_version`, `unlink_intent`), and an `x-hexalith-authorization.requirement` token of `tenant-context-project-link-memory-permission-project-detail-and-memories-case-acl` / `tenant-context-project-unlink-memory-permission-project-detail`.
  - [x] Regenerate `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` and `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` through the existing NSwag workflow. **Do not hand-edit** the `.g.cs` files. The known Linux NSwag idempotency-helper Windows-backslash path workaround from the Story 2.5 build environment (run the helper generator manually with forward-slash paths) applies — record it in the Debug Log if used (see [[build-environment]]).
  - [x] Build the contracts + client lanes. Confirm 0W/0E. Confirm `git diff --check` clean on the regenerated `.g.cs` files (no whitespace errors; LF on disk per the recorded repo convention).

- [x] **Task 3 — Domain command/event/state behavior. (AC: 2, 3, 4, 8)**
  - [x] Add `ProjectAggregate.Memories.cs` partial under `src/Hexalith.Projects/Aggregates/Project/` mirroring `ProjectAggregate.References.cs`:
    - `static ProjectResult Handle(ProjectState state, LinkMemory command, DateTimeOffset occurredAt)` with the exact decision tree from `LinkFileReference`: validate → idempotency-key dedup → `IsCreated` / identity / archived gates → duplicate-id with conflicting metadata → bounded-set cap → emit `MemoryLinked`.
    - `static ProjectResult Handle(ProjectState state, UnlinkMemory command, DateTimeOffset occurredAt)` mirroring `UnlinkFileReference`: validate → idempotency → identity / archived gates → missing reference = `IdempotentReplay` → emit `MemoryUnlinked`.
    - Deterministic-timestamp test overloads (`Handle(state, command)` defaulting to `DateTimeOffset.MinValue`).
  - [x] Extend `ProjectResultCode` (`src/Hexalith.Projects/Aggregates/Project/ProjectResultCode.cs`) with `MemoryLinked`, `MemoryUnlinked`, `MemoryReferenceConflict`, `MemoryReferenceLimitExceeded`. Update `ProjectResult.IsAccepted` to include the two new accepted codes. Update `ProjectResult.ToRejectionReason()` to map `MemoryReferenceConflict` and `MemoryReferenceLimitExceeded` to `ReferenceState.Conflict` (same as their file-reference equivalents).
  - [x] Extend `ProjectResult.Rejected(IProjectCommand command, …)` `referenceKind` switch with `LinkMemory => ("memory", SafeReferenceIdentifier(...))` and `UnlinkMemory => ("memory", SafeReferenceIdentifier(...))`. Extend `ToRejectionEvent()` with two new switch arms mapping `nameof(LinkMemory)` and `nameof(UnlinkMemory)` to `ProjectReferenceLinkRejected` / `ProjectReferenceUnlinkRejected` with `ReferenceKind = "memory"` (no new rejection event types).
  - [x] Extend `ProjectCommandValidator` (`src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs`) with:
    - `Validate(LinkMemory command)` mirroring the `LinkFileReference` validator (envelope identity safety + `IsSafeReferenceIdentifier(MemoryReferenceId)` + safe `DisplayName` shape).
    - `Validate(UnlinkMemory command)` mirroring `UnlinkFileReference`.
    - `internal static string ComputeLinkMemoryFingerprint(...)` and `internal static string ComputeUnlinkMemoryFingerprint(...)` producing the canonical fingerprint lines for the exact fields in AC 8 (the field order, `present=true;value=s:` shape, and `Escape` rules must be byte-identical to the file-reference fingerprint formats).
  - [x] Extend `ProjectCommandValidationResult` with `AcceptedMemoryLink(string idempotencyFingerprint)` / `AcceptedMemoryUnlink(string idempotencyFingerprint)` factories returning `ProjectResultCode.MemoryLinked` / `MemoryUnlinked`.
  - [x] Extend `ProjectState` with `IReadOnlyDictionary<string, ProjectMemoryReference> MemoryReferences` (init to `FrozenDictionary<string, ProjectMemoryReference>.Empty`) and a `public const int MaxMemoryReferences = 100;` constant. Update the `Empty` factory.
  - [x] Extend `ProjectStateApply` (`src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs`) with `MemoryLinked linked => state with { MemoryReferences = AddMemoryReference(...), … }` and `MemoryUnlinked unlinked => state with { MemoryReferences = RemoveMemoryReference(...), … }`. Add the symmetric `AddMemoryReference` / `RemoveMemoryReference` helpers next to the existing `AddFileReference` / `RemoveFileReference` helpers; unknown events must still throw.
  - [x] Idempotency fingerprint storage: the dedup map `state.IdempotencyFingerprints` already covers all commands; ensure the new aggregate handlers record `validation.IdempotencyFingerprint` exactly as `LinkFileReference` does (no special handling needed beyond extending `ProjectStateApply` for the new events).

- [x] **Task 4 — Memories ACL boundary. (AC: 5, 6, 12, 13)**
  - [x] Add `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs` (interface — exact signature from ADR).
  - [x] Add `src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationResult.cs` — `ProjectMemoryValidationOutcome` enum (`Accepted, ValidationFailed, Denied, Archived, Stale, TenantMismatch, Unavailable`) and `ProjectMemoryValidationResult(Outcome, CorrelationId)` record with static `Accepted(string? correlationId)` factory.
  - [x] Add `src/Hexalith.Projects.Server/Memories/MemoriesProjectMemoryDirectory.cs`:
    - `sealed class` taking `MemoriesClient memoriesClient` via primary constructor (mirrors `FoldersProjectFileReferenceDirectory`).
    - Validates non-empty `memoryReferenceId` / `correlationId` (else `ValidationFailed`).
    - Resolves the envelope tenant identifier from the existing `IProjectTenantContextAccessor` (the Memories `GetCaseAsync` requires a tenant id; per the ADR it is **the envelope tenant**, never caller-supplied via payload). Either inject `IProjectTenantContextAccessor` or accept the tenant id as an extra parameter to the ACL method — choose whichever keeps the existing `IProjectMemoryDirectory` signature from the ADR intact (preferred: add a `string tenantId` parameter to `ValidateLinkMemoryReferenceAsync` so the ACL stays stateless, and update the ADR's signature accordingly **in the story Dev Notes only**, not by editing the ADR file). The endpoint resolves the tenant context once and passes it explicitly.
    - Calls `memoriesClient.GetCaseAsync(tenantId, memoryReferenceId, cancellationToken)` (no other Memories method, ever).
    - Maps the typed result via the ADR Failure-to-shared-vocabulary table:
      - 200 + `Case.Status == Active` + `Case.TenantId == tenantId` → `Accepted`.
      - 200 + `Case.Status == Closed` or `Deleting` → `Archived`.
      - `MemoriesRemoteException` 401 / 403 / 404 (any `ErrorResponse.Code`) → `Denied`.
      - `MemoriesRemoteException` 408 / 503 / 5xx → `Unavailable`.
      - `MemoriesRemoteException` 400 / 422 / `INVALID_RESPONSE` → `ValidationFailed`.
      - `MemoriesRemoteException` 409 → `ValidationFailed`.
      - `OperationCanceledException` → rethrow (cancellation passthrough).
      - `HttpRequestException` / other `Exception` → `Unavailable`.
    - Never logs / returns raw `ErrorResponse.Message` / `Suggestion` / `MemoryUnit` fields. Result carries only the `Outcome` and the safe `CorrelationId`.
  - [x] Add `src/Hexalith.Projects.Server/Memories/UnavailableProjectMemoryDirectory.cs` — `internal sealed class` returning `Unavailable` (mirrors `UnavailableProjectFileReferenceDirectory`).
  - [x] Add an authorization-gate helper to `ProjectAuthorizationGate` (`src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs`): `AuthorizeLinkMemoryAsync` and `AuthorizeUnlinkMemoryAsync` mirror the existing `AuthorizeLinkFileReferenceAsync` / `AuthorizeUnlinkFileReferenceAsync` shape, using new action tokens `projects:link_memory` and `projects:unlink_memory`. The gate must run **before** any ACL call.
  - [x] Reference `Hexalith.Memories.Client.Rest` only from `src/Hexalith.Projects.Server/Memories/` (and `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` for the package reference). No other Projects project receives a Memories dependency. Add the package via `Directory.Packages.props` rather than inline `Version` (Central NuGet management).

- [x] **Task 5 — Endpoints and command submission. (AC: 1, 5, 11, 14)**
  - [x] Extend `ProjectsDomainServiceEndpoints` (`src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`):
    - Map `POST /api/v1/projects/{projectId}/memories/{memoryReferenceId}/link` with `[FromServices] IProjectMemoryDirectory memoryDirectory` and submit via the command submitter; mirror `LinkFileReferenceAsync` flow.
    - Map `DELETE /api/v1/projects/{projectId}/memories/{memoryReferenceId}` with **no** `IProjectMemoryDirectory` dependency (unlink never calls Memories — structural proof of AC 4).
    - Define `LinkMemoryHttpRequest` / `UnlinkMemoryHttpRequest` private records with closed JSON binding (mirrors `LinkFileReferenceHttpRequest` / `UnlinkFileReferenceHttpRequest`).
    - Validate route↔body identity (`projectId`, `memoryReferenceId`) before any submitter call.
    - Map `ProjectMemoryValidationResult` to safe Projects problem details with a new `MemoryReferenceValidationProblem(...)` helper mirroring `FileReferenceValidationProblem(...)`: `Accepted → null`, `ValidationFailed → ValidationProblem(... , "memoryReference")`, `Stale | Unavailable → ReadModelUnavailable(...)`, `Denied → SafeAuthorizationDenial404(...)`, `Archived → SafeAuthorizationDenial403(...) or SafeDenial...` (match the file-reference precedent — `Archived` and `Denied` both collapse to safe-denial outcomes at the HTTP layer).
    - Extend `ToProjectReferenceSummaries(...)` to enumerate `MemoryReferences` ordered by `MemoryReferenceId` (Ordinal), emitting a `ProjectReferenceSummary` with `referenceKind = "memory"`, `referenceState`, optional `reasonCode`, `observedAt`.
  - [x] Extend `IProjectCommandSubmitter` with `SubmitLinkMemoryAsync` / `SubmitUnlinkMemoryAsync`.
  - [x] Extend `EventStoreProjectCommandSubmitter` and add the two new command-type constants in `ProjectsServerModule` (`LinkMemory`, `UnlinkMemory`); extend `ProjectsDomainProcessor` to route the two new commands to the new aggregate handlers and to map their accepted result codes to `DomainResult.Success(...)`.
  - [x] Extend `AddProjectsServer` (`src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs`) with `services.TryAddTransient<IProjectMemoryDirectory>(sp => { MemoriesClient? client = sp.GetService<MemoriesClient>(); return client is null ? new UnavailableProjectMemoryDirectory() : new MemoriesProjectMemoryDirectory(client); });` — exact pattern parallel to `IProjectFileReferenceDirectory`.
  - [x] Extend `AddProjectsServerRuntimeInfrastructure` to register the Memories typed client (`if (!services.Any(... MemoriesClient ...)) services.AddMemoriesClient(options => options.BaseAddress = new Uri("http://memories"));`) — exact pattern parallel to the Folders registration.

- [x] **Task 6 — Projections & reads. (AC: 3, 7)**
  - [x] Extend `ProjectDetailItem` (`src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs`) with `IReadOnlyList<ProjectMemoryReference> MemoryReferences` alongside `FileReferences`.
  - [x] Extend `ProjectDetailProjection` (`src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs`) with `case MemoryLinked linked` / `case MemoryUnlinked unlinked` branches mirroring the existing file-reference branches.
  - [x] Extend `ProjectReferenceIndexProjection` (`src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs`) with a **disjoint memory-kind key prefix** (e.g. `:references:memory:`) so memory rows are added/removed without touching folder (`:references:folder:`) or file (`:references:file:`) rows. Folder replacement must not remove memory rows; file unlink must not remove memory rows. Tests must cover this disjointness explicitly.

- [x] **Task 7 — Documentation & contract hygiene. (AC: 9, 10, 12)**
  - [x] Update `docs/event-catalog.md` with `MemoryLinked`, `MemoryUnlinked`, and the link/unlink rejection paths (both surface through the existing `ProjectReferenceLinkRejected` / `ProjectReferenceUnlinkRejected` with `ReferenceKind == "memory"`). List payload fields exactly as emitted by AC 2. Mark sensitivity `metadata-only`. Name consumers (Projects projections; Epic 3 context assembly).
  - [x] Update `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` to include the new event/command/model types in the leakage scan and add the Memories-specific forbidden-term set from AC 2 (Content / ContentBytes / ContentHash / SourceUri / SourceType / IngestedBy / Metadata / EmbeddingProvider / EmbeddingModel / EmbeddingDimensions / Classification / FailureDetails / IngestionInput / embedding vector / search snippet / traversal payload / ErrorResponse.Message / Suggestion / Memories tenant-id-as-payload).
  - [x] Confirm no `#pragma warning disable HXL001 | HXL002` and no `[SuppressMessage("Microsoft.Usage", "HXL001"|"HXL002")]` exists anywhere under `src/Hexalith.Projects.*` or `tests/Hexalith.Projects.*` after the change. A grep at validation time must return zero hits.

- [x] **Task 8 — Tests. (AC: 14)**
  - [x] `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateMemoryTests.cs` — Tier-1 aggregate matrix from AC 14.
  - [x] `tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs` and `ProjectReferenceIndexProjectionTests.cs` — extend with memory branches (disjointness from folder/file lanes explicitly asserted).
  - [x] `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` — extended scan (Task 7).
  - [x] `tests/Hexalith.Projects.Server.Tests/ProjectMemoryDirectoryTests.cs` — Tier-2 ACL matrix from AC 14 using a recording `HttpMessageHandler` behind `MemoriesClient` (the Memories research recommended test pattern). Also asserts the request lane contains only `GET api/tenants/{tenant}/cases/{case}` paths (AC 12 negative proof).
  - [x] `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` (extended) or a new `LinkMemoryEndpointTests.cs` / `UnlinkMemoryEndpointTests.cs` — endpoint matrix from AC 14, including a `DeleteMemory_Authorized_Returns202AndSubmitsUnlinkWithoutMemoriesCall` test that proves unlink takes no `IProjectMemoryDirectory` dependency.
  - [x] `tests/Hexalith.Projects.Server.Tests/ProjectAuthorizationGateTests.cs` — coverage for `AuthorizeLinkMemoryAsync` / `AuthorizeUnlinkMemoryAsync`.
  - [x] `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` — extended fingerprint and operation-existence checks for `LinkMemory` / `UnlinkMemory`; assert `memory` is **still** the only memory-related `referenceKind` enum value at both request and response sites (no churn).
  - [x] `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` — extended to cross-check `ComputeLinkMemoryFingerprint` / `ComputeUnlinkMemoryFingerprint` server output against the regenerated client helper output (byte-equal canonical line shape for at least three representative inputs).
  - [x] `tests/Hexalith.Projects.Integration.Tests/...` — at minimum one end-to-end link/unlink memory trace using a stubbed `MemoriesClient` registered through `AddProjectsServerRuntimeInfrastructure`.
  - [x] **No-sleep grep** during validation: `grep -rE "Thread\.Sleep|Task\.Delay|SpinWait\.|await Task\.Yield" tests/Hexalith.Projects.*/` filtered to Story 2.7 new/modified test files must return zero hits.

- [x] **Task 9 — Validate the story implementation. (AC: 15)**
  - [x] Use the build environment recorded in [[build-environment]]: `DOTNET_ROOT=/home/administrator/.dotnet` + that `dotnet`. Avoid `/usr/bin/dotnet` (resolves to 10.0.108 and fails `rollForward: latestPatch`).
  - [x] Run `dotnet build src/Hexalith.Projects/Hexalith.Projects.slnx`. Confirm 0 warnings / 0 errors.
  - [x] Run focused lanes: `dotnet test tests/Hexalith.Projects.Tests`, `Hexalith.Projects.Server.Tests`, `Hexalith.Projects.Contracts.Tests`, `Hexalith.Projects.Client.Tests`, and `Hexalith.Projects.Integration.Tests`. Record per-lane counts in the Dev Agent Record.
  - [x] Run `dotnet test Hexalith.Projects.slnx` (full solution). Record the total. Story 2.6 left it at 511/511; Story 2.7 adds new tests — the total must remain `passing == executed`, `failed == 0`.
  - [x] Run `git diff --check` on story-touched files. Confirm clean (no whitespace errors). Hand-written `.cs` / `.md` are written LF on disk per [[build-environment]] even though `.editorconfig` says CRLF; `git diff --stat` after writing should show only story-touched files.
  - [x] Confirm no submodule pointer change in `git status`; the Hexalith.Memories submodule pointer at the root must equal its baseline (`608d15d` per Story 2.6 Debug Log — re-verify in this run).
  - [x] Confirm no `.g.cs` hand-edits: `git diff` on regenerated `.g.cs` files shows only the additive `LinkMemoryAsync` / `UnlinkMemoryAsync` / fingerprint helpers and nothing else.
  - [x] Populate the Dev Agent Record with the validation summary in AC 15.

## Dev Notes

### Story Scope Boundary

- **In scope:** `LinkMemory` / `UnlinkMemory` commands, `MemoryLinked` / `MemoryUnlinked` events, `ProjectMemoryReference` + `ProjectMemoryReferenceMetadata` models, OpenAPI route additions (link/unlink), regenerated client + idempotency helpers, aggregate handlers (`ProjectAggregate.Memories.cs`), `ProjectState.MemoryReferences` + `MaxMemoryReferences`, `ProjectStateApply` extensions, `ProjectDetailProjection` + `ProjectReferenceIndexProjection` extensions (disjoint memory lane), `IProjectMemoryDirectory` + `MemoriesProjectMemoryDirectory` + `ProjectMemoryValidationResult` + `UnavailableProjectMemoryDirectory`, endpoint handlers, authorization gate helpers, command submitter / domain processor extensions, DI registration for the Memories typed client, `docs/event-catalog.md` entries, leakage harness extensions, and the test matrix in AC 14.
- **Explicitly out of scope (recorded in ADR Out of scope):** per-`MemoryUnit` pins; semantic / hybrid search wiring; ingestion orchestration from Projects; bulk relink / repair; real-Keycloak E2E (Epic 5); calling any `[Experimental("HXL001")]` write or `[Experimental("HXL002")]` diagnostic method from Projects; storing any `MemoryUnit.Content` / `ContentHash` / `SourceUri` / embedding material in Projects; advancing the `Hexalith.Memories` submodule pointer; nested recursive submodule init; modifying `_bmad-output/planning-artifacts/epics.md` Story 2.6 / 2.7 acceptance criteria; modifying shared-vocabulary enums under `src/Hexalith.Projects.Contracts/Ui/`; introducing a Projects-owned `MemoryId` / `CaseId` value object.

### Current Code Facts Verified (this working tree, baseline `0058ac35`)

- The Story 2.6 ADR ([[memories-link-target]]) is `Accepted` and is at `docs/adr/memories-link-target.md`. It is the canonical source for the Story 2.7 design; this story file does not re-litigate it.
- `MemoriesClient.GetCaseAsync(string tenantId, string caseId, CancellationToken ct = default)` is at `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810`; the `<remarks>Stable since Story 10.2.</remarks>` doc is at `:809`. HTTP path: `GET api/tenants/{tenantId}/cases/{caseId}`. Returns `Case` (`Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/Case.cs`): `Id`, `TenantId`, `Name`, `Description?`, `Status` (`CaseStatus { Active, Closed, Deleting }`), `CreatedAt`, `LastUpdated`, `MemoryUnitCount`. The non-2xx path throws `MemoriesRemoteException(HttpStatusCode StatusCode, ErrorResponse Error)`.
- Memories experimental annotations confirmed: `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync` are `[Experimental("HXL001")]`; `ListHandlersAsync`, `GetHandlerMismatchesAsync` are `[Experimental("HXL002")]`. The ACL must never call any of these.
- Memories DI: `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs::AddMemoriesClient` registers `MemoriesAuthHandler` as transient and `HttpClient<MemoriesClient>` with the auth handler — exact analogue of the Folders typed-client registration the Projects server already uses.
- The Projects server **does not yet** reference the Memories client. Story 2.7 adds the project / package reference under `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` only. Central NuGet management lives in `Directory.Packages.props`; do not inline the version.
- The Projects OpenAPI spine already reserves `memory` in two enums (verified):
  - Request `referenceKind` enum at `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` ~line 1866 (the file-reference response site at ~line 2014 also lists `memory`). Story 2.5 deliberately added `file` to the response enum and left `memory` intact. Story 2.7 must **not** re-emit either enum.
- The shared vocabulary at `src/Hexalith.Projects.Contracts/Ui/{ReferenceState.cs, ProjectLifecycle.cs, ProjectReasonCode.cs}` already covers every value the ADR mapping table needs (`Included`, `Excluded`, `Unauthorized`, `Unavailable`, `Stale`, `Archived`, `Ambiguous`, `TenantMismatch`, `Conflict`, `InvalidReference`, `Pending` ; `Active` / `Archived` lifecycle; `MemoryMatched` reason code). **No new enum values are introduced by Story 2.7.**
- The Story 2.5 Folders/File pattern is now the reference implementation. The relevant existing surfaces Story 2.7 mirrors are:
  - `src/Hexalith.Projects.Contracts/Commands/LinkFileReference.cs` and `UnlinkFileReference.cs`.
  - `src/Hexalith.Projects.Contracts/Events/FileReferenceLinked.cs` and `FileReferenceUnlinked.cs`.
  - `src/Hexalith.Projects.Contracts/Models/ProjectFileReference.cs` and `ProjectFileReferenceMetadata.cs`.
  - `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.References.cs`.
  - `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs` (`FileReferences` + `MaxFileReferences = 100`).
  - `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs` (`AddFileReference` / `RemoveFileReference`).
  - `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs` (file branches).
  - `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs` (per-kind key prefix discipline).
  - `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs` / `FoldersProjectFileReferenceDirectory.cs` / `ProjectFileReferenceValidationResult.cs` / `UnavailableProjectFileReferenceDirectory.cs`.
  - `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` (file-reference endpoint flow, including the unlink-takes-no-ACL-dependency proof).
  - `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` (`AuthorizeLinkFileReferenceAsync` / `AuthorizeUnlinkFileReferenceAsync`).
  - `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` (`TryAddTransient` ACL pattern; `AddFoldersClient` in `AddProjectsServerRuntimeInfrastructure`).
  - `src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs` (rejection mapping for `LinkFileReference` / `UnlinkFileReference`).
  - `src/Hexalith.Projects/Aggregates/Project/ProjectResultCode.cs` (`FileReferenceLinked`, `FileReferenceUnlinked`, `FileReferenceConflict`, `FileReferenceLimitExceeded`).
  - `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateFileReferenceTests.cs`.
  - `tests/Hexalith.Projects.Server.Tests/ProjectFileReferenceDirectoryTests.cs` (test pattern to mirror).
- The current root commit is `0058ac35 feat(story-2.6)`. Story 2.6 left `Hexalith.Memories` submodule pointer at `608d15d`; do not advance it.

### Required Capability Path

The Story 2.5 capability-gate discipline applies: **HALT before coding** if the working-tree Memories surface diverges from the ADR. Acceptable path: the **stable `GetCaseAsync` read route** (verified above). No degraded behavior is contemplated; the route is server-mapped and stable. If a future change makes `GetCaseAsync` non-stable or content-bearing, the entire ACL design must be reconsidered before code lands — surface the HALT in the Dev Agent Record and stop.

### Guardrails

- **Pure domain core.** No `Hexalith.Memories.*` reference anywhere under `src/Hexalith.Projects/**`, `src/Hexalith.Projects.Contracts/**`, `src/Hexalith.Projects.Client/**`, `tests/Hexalith.Projects.Tests/**`, `tests/Hexalith.Projects.Contracts.Tests/**`, or `tests/Hexalith.Projects.Client.Tests/**`. Memories types are confined to `src/Hexalith.Projects.Server/Memories/**` and to `tests/Hexalith.Projects.Server.Tests/**` (the test project that exercises the ACL through a stubbed `HttpMessageHandler`).
- **Identifier reuse.** Per [[identifier-boundary]], Memories case ids stay as plain ULID-shaped `string`. Do not mint a Projects-owned `MemoryId` / `CaseId` value object.
- **Metadata-only.** Storing or echoing `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, `SourceType`, `IngestedBy`, `IngestedAt`, `Metadata`, `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`, `Classification`, `FailureDetails`, raw `IngestionInput` payloads, embedding vectors, vector dimensions, fusion weights, search snippets, traversal payloads, raw `ErrorResponse.Message`, raw `ErrorResponse.Suggestion`, raw `MemoriesRemoteException.Message`, tokens, file paths, prompt fragments, or transcript fragments is forbidden everywhere (events, state, projections, logs, audit rows, problem details, request/response DTOs).
- **`[Experimental]` containment is binding.** The ACL calls **only** `GetCaseAsync`. Zero HXL001 / HXL002 pragma suppressions. If a future story genuinely needs an experimental Memories method, the suppression must live in **exactly one** Memories ACL façade file under `src/Hexalith.Projects.Server/Memories/`. The following are out of bounds in perpetuity: `src/Hexalith.Projects.Contracts/**`, `src/Hexalith.Projects.Client/**`, `src/Hexalith.Projects/**`, generated `.g.cs` artifacts, OpenAPI spine, idempotency hasher, all test projects.
- **No-sleep rule in tests.** Forbidden tokens in Story 2.7 test files: `Thread.Sleep`, `Task.Delay`, `SpinWait`, `await Task.Yield()` (as time-wait), wall-clock retry loops, polling-with-real-time. Convergence is asserted via deterministic fakes / stubbed `HttpMessageHandler` / fake clocks.
- **Project Folder rule preserved (Story 2.4).** Linking/unlinking a memory must never clear, replace, satisfy, or auto-create the single Project Folder.
- **Story 2.5 file-reference rule preserved.** Memory link/unlink must not touch file references; file unlink must not touch memory references; folder replacement must not touch memory rows. Disjoint per-kind reference-index lanes.
- **Tenant authority from envelope only.** Memories `GetCaseAsync` requires a tenant id; it comes from `IProjectTenantContextAccessor` (which itself derives from authenticated principal claims / EventStore envelope), never from the request body. The ADR identifier-shape table forbids re-emitting the tenant id as a payload field on `MemoryLinked` / `MemoryUnlinked`.
- **No `V2` types.** Public contracts evolve only through the OpenAPI spine and remain serialization-tolerant.
- **No nested recursive submodule init.** Do not run `git submodule update --init --recursive`. Read-only inspection of the already-initialized `Hexalith.Memories` submodule is allowed; advancing its pointer is not.
- **No Memories submodule edits.** This story does not edit any file under `Hexalith.Memories/**` and does not commit inside that submodule.

### Suggested API Shape

```http
POST /api/v1/projects/{projectId}/memories/{memoryReferenceId}/link
Idempotency-Key: <required>
X-Correlation-Id: <propagated>
X-Hexalith-Task-Id: <propagated>
```

```json
{
  "requestSchemaVersion": "v1",
  "operation": "link",
  "projectId": "proj_...",
  "memoryReferenceId": "case_...",
  "memoryMetadata": {
    "displayName": "Q3 product strategy memory"
  }
}
```

```http
DELETE /api/v1/projects/{projectId}/memories/{memoryReferenceId}
Idempotency-Key: <required>
X-Correlation-Id: <propagated>
X-Hexalith-Task-Id: <propagated>
```

```json
{
  "requestSchemaVersion": "v1",
  "operation": "unlink",
  "unlinkIntent": "removeReference",
  "projectId": "proj_...",
  "memoryReferenceId": "case_..."
}
```

The `memoryReferenceId` is the Memories `Case.Id` (ULID-shaped string, opaque sibling identifier; never wrapped in a Projects-owned VO). The owning tenant is **not** in the body; it is derived from the envelope.

### Identifier shape (binding — copied from the Story 2.6 ADR)

| Field | Type | Source | Notes |
| --- | --- | --- | --- |
| `memoryReferenceId` | `string` (ULID-shaped) | `Case.Id` | Opaque sibling identifier; never wrapped in a VO; validation accepts any non-whitespace string. |
| `referenceKind` | `string` enum | constant `memory` | Already reserved in OpenAPI; never re-emitted by this story. |
| `displayName` | `string?` | `Case.Name` | Optional safe label; not load-bearing for authorization. |
| `lifecycle` | `ProjectLifecycle` | derived from `Case.Status` | `Active → Active`; `Closed`/`Deleting → Archived`. |
| `referenceState` | `ReferenceState` | derived from ACL outcome | Shared vocabulary only; never a new value. |
| `reasonCode` | `ProjectReasonCode?` | constant `MemoryMatched` when included | Already reserved; no new reason code. |
| `occurredAt` | `DateTimeOffset` | event envelope occurrence time | Aggregate `Handle` does not invent wall-clock time. |

The owning `tenantId` is **implicit** (envelope tenant) and never re-emitted into the `MemoryLinked` / `MemoryUnlinked` payload.

### Failure-to-shared-vocabulary mapping (binding — copied from the Story 2.6 ADR)

| Upstream signal | `ProjectMemoryValidationOutcome` | Surfaced `ReferenceState` |
| --- | --- | --- |
| `GetCaseAsync` 200 + `Case.Status == Active` + tenant matches envelope tenant | `Accepted` | `Included` |
| `GetCaseAsync` 200 + `Case.Status == Closed` | `Archived` | `Archived` |
| `GetCaseAsync` 200 + `Case.Status == Deleting` | `Archived` | `Archived` |
| `MemoriesRemoteException` 401 / 403 / 404 (any code) | `Denied` | `Unauthorized` |
| `MemoriesRemoteException` 408 / 503 / 5xx | `Unavailable` | `Unavailable` |
| `MemoriesRemoteException` 409 | `ValidationFailed` | `InvalidReference` |
| `MemoriesRemoteException` 400 / 422 / `INVALID_RESPONSE` | `ValidationFailed` | `InvalidReference` |
| Network failure / `HttpRequestException` | `Unavailable` | `Unavailable` |
| No `MemoriesClient` registered | `Unavailable` | `Unavailable` |
| `TenantStatus` not `Active` (upstream collapses to 404 / 503) | `Denied` / `Unavailable` | `Unauthorized` / `Unavailable` |
| `MemoryUnitStatus` lifecycle (not-yet-indexed / not-yet-searchable) | n/a (Option A — Case-level link does not validate per-unit) | n/a |

Never echo `MemoryUnit.Content`, `ContentHash`, `SourceUri`, `ErrorResponse.Message`, or `ErrorResponse.Suggestion` into Projects-owned outputs.

### Files To Read Before Editing

- `docs/adr/memories-link-target.md` (the Story 2.6 ADR — read end-to-end before authoring any code; this story does not duplicate the ADR's reasoning).
- `_bmad-output/implementation-artifacts/2-6-memories-linkage-decision-spike.md` (Story 2.6 Dev Notes + Dev Agent Record, including Memories-surface evidence verified in this working tree).
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` (pattern Story 2.7 mirrors; the entire optional-reference flow precedent).
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` (capability-gate discipline; ACL fallback pattern; `TryAddTransient` review fix).
- `_bmad-output/planning-artifacts/epics.md` (Story 2.7 ACs — authoritative; Story 2.6 / 2.7 ACs must not be edited).
- `_bmad-output/planning-artifacts/architecture.md` (`AR-G4` resolution pointer at line ~711; `AR-14` Memories model; `AR-9` reference index; `AR-11` ACLs; `AR-18` shared vocabulary; Implementation Sequence).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` (FR-10 Link Memory; FR-11 Unlink Context Reference consequences).
- `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md` (REST surface, async-ingest pattern, fail-closed error handling, test pattern).
- `docs/adr/identifier-boundary.md` (sibling identifier reuse; no Projects-owned `MemoryId`).
- `docs/event-catalog.md` (authoritative event catalog Story 2.7 updates).
- `docs/payload-taxonomy.md` (sensitivity classes; metadata-only invariants).
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (OpenAPI spine; confirm `memory` already in `referenceKind` enums; add link/unlink routes only).
- `src/Hexalith.Projects.Contracts/Commands/LinkFileReference.cs`, `UnlinkFileReference.cs` (command record shape to mirror).
- `src/Hexalith.Projects.Contracts/Events/FileReferenceLinked.cs`, `FileReferenceUnlinked.cs` (event record shape to mirror).
- `src/Hexalith.Projects.Contracts/Models/ProjectFileReference.cs`, `ProjectFileReferenceMetadata.cs` (model shape to mirror).
- `src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs`, `ProjectLifecycle.cs`, `ProjectReasonCode.cs` (shared vocabulary; do not edit).
- `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.References.cs` (aggregate handler shape to mirror).
- `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs`, `ProjectStateApply.cs` (state extensions).
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` (validator + fingerprint helpers; mirror canonical line shape exactly).
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidationResult.cs` (extend with memory factories).
- `src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs`, `ProjectResultCode.cs` (extend rejection mapping; new accepted/conflict codes).
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs`, `ProjectDetailProjection.cs` (projection extension).
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs` (per-kind disjoint memory lane).
- `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs`, `FoldersProjectFileReferenceDirectory.cs`, `ProjectFileReferenceValidationResult.cs`, `UnavailableProjectFileReferenceDirectory.cs` (ACL pattern to mirror).
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` (gate helpers + action tokens).
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` (endpoint flow to mirror, esp. `LinkFileReferenceAsync` / `UnlinkFileReferenceAsync` / `FileReferenceValidationProblem`).
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` (`TryAddTransient` ACL registration; `AddProjectsServerRuntimeInfrastructure` Memories client wiring).
- `src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs`, `EventStoreProjectCommandSubmitter.cs`, `ProjectsDomainProcessor.cs`, `ProjectsServerModule.cs` (extend with the two new commands).
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (confirm `GetCaseAsync` stable; HXL annotations).
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs` (DI shape to wire from `AddProjectsServerRuntimeInfrastructure`).
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesRemoteException.cs` (exception shape used by the ACL mapping).
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/Case.cs`, `CaseStatus.cs`, `ErrorResponse.cs` (typed Case shape; error code names).
- `Hexalith.Memories/docs/dev/experimental-apis.md`, `Hexalith.Memories/docs/dev/consistency.md` (authoritative HXL surface + eventual-consistency contract).
- `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateFileReferenceTests.cs` (test pattern to mirror).
- `tests/Hexalith.Projects.Server.Tests/ProjectFileReferenceDirectoryTests.cs` (ACL test pattern with recording handler).

### Testing Requirements

See AC 14 for the full test matrix. Highlights:

- **Aggregate matrix** mirrors `ProjectAggregateFileReferenceTests` exactly, substituting memory types.
- **ACL matrix** uses a recording `HttpMessageHandler` behind a real `MemoriesClient` (per the Memories research's recommended test pattern). The handler records both request paths and response shapes; assertions confirm the only path observed is `GET api/tenants/{tenant}/cases/{case}` and no body is read from any other route. Each row of the AC 13 / failure-mapping table is covered with a positive assertion.
- **Endpoint matrix** proves that `Unauthorized` / `Archived` Project denial happens **before** any ACL call (recording handler observes zero requests on those paths) and that the unlink endpoint has no `IProjectMemoryDirectory` dependency.
- **NoPayloadLeakage** scans the new types for the AC 2 forbidden term list. Treat the Memories `Case.MemoryUnitCount` field as **not** a forbidden term (it is a metadata count; the ADR allows it conceptually, but Story 2.7 does **not** store it on `ProjectMemoryReference` either — keep the model minimal per the identifier-shape table).
- **No-sleep grep** during validation: `grep -rE "Thread\.Sleep|Task\.Delay|SpinWait\.|await Task\.Yield" tests/Hexalith.Projects.*/ | grep -v ".g.cs"` filtered to Story 2.7 new/modified test files must return zero hits.

### Previous Story Intelligence

- **Story 2.6 (Memories linkage decision spike) — Accepted.** The ADR ([[memories-link-target]]) is the load-bearing input. Story 2.7's design, ACL shape, identifier shape, failure mapping, eventual-consistency rule, Epic 3 allowlist treatment, and `[Experimental]` containment all come from the ADR. The ADR explicitly states Story 2.7 ships zero HXL001 / HXL002 pragma suppressions because the chosen ACL only calls stable routes.
- **Story 2.5 (File Reference link/unlink) — done, 511/511.** Story 2.7 mirrors the entire pattern: command-async mutation surface, route/body identity equality, closed JSON binding, per-kind disjoint reference-index keys, `TryAddTransient` ACL registration, `Unavailable*Directory` fail-closed fallback, recording-handler ACL tests, byte-equal client/server idempotency fingerprint helpers. The Story 2.5 review surfaced three LOW observations carried forward (1) `WorkspaceRelativePath` contract vs server validator divergence — not applicable to memory (no workspace path); (2) `TenantMismatch` outcome documented but rarely produced by safe-denial — applicable here (the ADR explicitly carries `TenantMismatch` for taxonomy symmetry); (3) shared client/server `Escape` divergence for `U+2028`/`U+2029` — pre-existing, applies to all fingerprints, not introduced by this story.
- **Story 2.4 (Project Folder reference) — done.** Established the capability-gate discipline (HALT before coding if no trustworthy authorization path) and the `TryAddTransient` typed-client review fix. Story 2.7 applies the same gate against the Memories surface (Task 1).
- **Story 2.3 (Conversation write-side) — done.** Established the write-side mutation discipline (route/body identity, idempotency, safe ProblemDetails) Story 2.7 reuses.
- **Story 2.2 (Conversation upstream capability) — done.** Established the upstream/enabler pattern Story 2.6 itself followed (ADR as the deliverable, picked up by the downstream story).
- **Story 1.4–1.9 (Epic 1 done).** Establishes the canonical `tenant:domain:aggregate` identity, fail-closed safe-denial, no-payload-leakage harness, layered authorization, and Aspire/Workers topology Story 2.7 plugs into without changes.
- **Recent commit hygiene:** Story 2.5 (`e127b7a`), Story 2.6 (`0058ac3`), BMAD updates (`62b8933`, `d324a23`, `f27cd63`) all follow story-scoped commits with no nested recursive submodule init. Story 2.7 must do the same; if the dev agent commits at all (story-automator may), the diff is a single story-scoped change.

### Out Of Scope

- Per-`MemoryUnit` pins (Option B / Option C from the ADR — deferred until product evidence forces a revisit; the ADR explicitly states the migration would be additive, not a `V2` event reshape).
- Calling `GetMemoryUnitAsync`, `HybridSearchAsync`, `SearchAsync`, `TraverseAsync`, `ExportCaseAsync`, `ExportTenantAsync`, or any content-bearing route from Projects ever, including in tests.
- Calling `CreateTenantAsync`, `CreateCaseAsync`, `IngestAsync`, `GetTelemetrySummaryAsync` (HXL001), `ListHandlersAsync`, `GetHandlerMismatchesAsync` (HXL002) from Projects ever, including in tests.
- Storing `MemoryUnit.Content`, `ContentBytes`, `ContentHash`, `SourceUri`, `SourceType`, `IngestedBy`, `IngestedAt`, `Metadata`, `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`, `Classification`, `FailureDetails`, raw `IngestionInput`, embeddings, vector dimensions, fusion weights, search snippets, traversal payloads, raw `ErrorResponse.Message` / `Suggestion`, raw `MemoriesRemoteException.Message`, tokens, file paths, prompts, transcripts, or the Memories-internal tenant identifier as a payload field in any Projects event / state / projection / log / audit row.
- Implementing semantic / hybrid search, traversal, ingestion orchestration, or any RAG behavior inside Projects.
- Implementing full Project Context assembly, context-selection allowlist, explanation surfaces, refresh, or resolution from attachments (Epic 3 / Epic 4).
- Replacing, removing, or weakening the Project Folder behavior from Story 2.4 or the File Reference behavior from Story 2.5.
- Adding new shared-vocabulary enum values (`ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`). Use existing values; if a future story needs a new value, that work is its own ADR.
- Editing `_bmad-output/planning-artifacts/epics.md` Story 2.6 / 2.7 acceptance criteria, the `docs/adr/memories-link-target.md` ADR (status `Accepted`, frozen), or any shared-vocabulary enum.
- MCP / CLI / Web operational console surfaces, audit timeline projection, FrontComposer views (Epic 5).
- Advancing the `Hexalith.Memories` submodule pointer or editing any file under `Hexalith.Memories/**`.
- Performing nested recursive submodule initialization / update.

### Developer HALT Conditions

- **HALT before authoring any `.cs`** if the working-tree Memories surface diverges from Task 1 evidence (e.g. `GetCaseAsync` is no longer documented "Stable since Story 10.2"; `[Experimental("HXL001")]` annotations changed; `Case` / `CaseStatus` / `ErrorResponse` shapes changed; a content-bearing route appeared at the same path).
- **HALT** if the implementation would require calling any `[Experimental("HXL001")]` write or `[Experimental("HXL002")]` diagnostic method from Projects, or any content-bearing route (`GetMemoryUnitAsync`, `HybridSearchAsync`, `SearchAsync`, `TraverseAsync`, `ExportCaseAsync`, `ExportTenantAsync`).
- **HALT** if the implementation would require storing or echoing any `MemoryUnit.Content` / `ContentBytes` / `ContentHash` / `SourceUri` / `SourceType` / `IngestedBy` / `Metadata` / embedding material / `IngestionInput` / `ErrorResponse.Message` / `Suggestion` / `MemoriesRemoteException.Message` / token / path in Projects events / state / projections / problem details / audit / logs.
- **HALT** if the implementation would require introducing a new `ReferenceState` / `ProjectLifecycle` / `ProjectReasonCode` enum value (e.g. a Memory-specific `pendingIngestion`) — use existing values (`Pending` / `Unavailable` / `Stale`) instead. Surface the conflict and reconsider.
- **HALT** if the implementation would require a `V2` event / command / schema, hand-editing any `.g.cs`, bypassing the OpenAPI spine, advancing the `Hexalith.Memories` submodule pointer, or nested recursive submodule initialization.
- **HALT** if removing the Story 2.4 Project Folder rule or weakening the Story 2.5 file-reference disjointness is required for the design to work — both rules must remain intact.
- **HALT** if `Thread.Sleep` / `Task.Delay` / `SpinWait` / wall-clock polling is required to make a test pass — the eventual-consistency contract is deterministic-fakes-only, no exceptions.
- **HALT** if the Memories typed-client lifetime would have to be singleton — it must be transient / request-scoped (Story 2.4 / 2.5 review fix).
- **HALT** if completing the story requires modifying `_bmad-output/planning-artifacts/epics.md` Story 2.6 / 2.7 acceptance criteria or the `docs/adr/memories-link-target.md` ADR.

## References

- [[memories-link-target]] (`docs/adr/memories-link-target.md`) — the canonical ADR; the load-bearing input to Story 2.7.
- [[identifier-boundary]] (`docs/adr/identifier-boundary.md`) — sibling identifier reuse rule (Memories ids are plain ULID-shaped strings; no Projects-owned `MemoryId` / `CaseId` VO).
- `_bmad-output/planning-artifacts/epics.md` — Story 2.7 ACs (authoritative); PR-4 framing.
- `_bmad-output/planning-artifacts/architecture.md` — `AR-G4` (resolved by Story 2.6), `AR-14` (Memories model), `AR-9` (reference index), `AR-11` (ACLs), `AR-18` (shared vocabulary), Implementation Sequence step 6.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` — FR-10 (Link Memory), FR-11 (Unlink Context Reference) consequences.
- `_bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-research-2026-05-24.md` — REST surface, async-ingest pattern, fail-closed error handling, recommended test pattern (stubbed `HttpMessageHandler` behind `MemoriesClient`).
- `_bmad-output/implementation-artifacts/2-6-memories-linkage-decision-spike.md` — Story 2.6 Dev Notes and Memories-surface evidence verified in this working tree.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — pattern Story 2.7 mirrors end-to-end.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` — capability-gate discipline; `TryAddTransient` review fix.
- `docs/event-catalog.md` — authoritative event catalog (Story 2.7 adds `MemoryLinked` / `MemoryUnlinked`).
- `docs/payload-taxonomy.md` — sensitivity classes; metadata-only invariants.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — OpenAPI spine (already reserves `referenceKind: memory` at both enum sites — do not re-emit).
- `src/Hexalith.Projects.Contracts/Ui/{ReferenceState.cs, ProjectLifecycle.cs, ProjectReasonCode.cs}` — shared vocabulary (do not edit).
- `src/Hexalith.Projects.Server/Folders/` — analogue ACL pattern Story 2.7 mirrors under `src/Hexalith.Projects.Server/Memories/`.
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — `GetCaseAsync` (stable since Memories Story 10.2); HXL001 / HXL002 annotations.
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClientServiceCollectionExtensions.cs` — `AddMemoriesClient` DI shape.
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesRemoteException.cs` — exception shape (`HttpStatusCode StatusCode`, `ErrorResponse Error`) used by the ACL mapping.
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/{Case.cs, CaseStatus.cs, ErrorResponse.cs}` — typed Case shape; error code names.
- `Hexalith.Memories/docs/dev/experimental-apis.md` — authoritative HXL001 / HXL002 surface.
- `Hexalith.Memories/docs/dev/consistency.md` — authoritative eventual-consistency contract (no read-after-write; triple-write divergence; verify-then-repair).

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (create-story, 2026-05-28)

### Debug Log References

- 2026-05-28: Resolved `bmad-create-story` workflow customization; loaded project config; confirmed sprint status (`2-7-link-unlink-memory` is `backlog`, Story 2.6 is `done`, Epic 2 is `in-progress`); loaded Epic 2 Story 2.7 verbatim from `_bmad-output/planning-artifacts/epics.md`; loaded the Story 2.6 ADR `docs/adr/memories-link-target.md` (Status: `Accepted`) end-to-end; loaded the Story 2.5 implementation artifact and verified pattern surfaces (commands / events / models / aggregate handler / state apply / projections / ACL / endpoints / DI / tests); loaded the Story 2.6 implementation artifact for Memories-surface evidence and Dev Agent Record; confirmed the current OpenAPI spine reserves `memory` at both `referenceKind` enum sites; confirmed `MemoryMatched` is already in `ProjectReasonCode`; confirmed `Hexalith.Projects.Server` does not yet reference the Memories client; verified `MemoriesClient.GetCaseAsync` at `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810` carries the `Stable since Story 10.2` doc and that `CreateTenantAsync` / `CreateCaseAsync` / `IngestAsync` / `GetTelemetrySummaryAsync` are `[Experimental("HXL001")]` and `ListHandlersAsync` / `GetHandlerMismatchesAsync` are `[Experimental("HXL002")]`; verified `Case` and `CaseStatus` shapes in `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/`.
- Create-story workflow only; no implementation commands were run for this story.
- 2026-05-28 (dev): Capability gate PASS: re-verified `MemoriesClient.GetCaseAsync(string tenantId, string caseId, CancellationToken)` at `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810` with the `<remarks>Stable since Story 10.2.</remarks>` doc at `:809`; `[Experimental("HXL001")]` annotations confirmed on `CreateTenantAsync` (:269), `CreateCaseAsync` (:343), `IngestAsync` (:404), `GetTelemetrySummaryAsync` (:594); `[Experimental("HXL002")]` on `ListHandlersAsync` (:639) and `GetHandlerMismatchesAsync` (:682); `Case(Id, TenantId, Name, Description?, Status, CreatedAt, LastUpdated, MemoryUnitCount)` and `CaseStatus { Active, Closed, Deleting }` shapes confirmed unchanged; `MemoriesRemoteException(HttpStatusCode, ErrorResponse)` shape confirmed; `MemoriesClientServiceCollectionExtensions::AddMemoriesClient` registers `MemoriesAuthHandler` as transient + `HttpClient<MemoriesClient>` (transient typed-client). Hexalith.Memories submodule pointer remained at `608d15d` (Story 2.6 baseline); no nested recursive submodule init was performed.
- 2026-05-28 (dev): Generated `.g.cs` regenerated through NSwag workflow: client regenerated automatically; idempotency-helper regenerated via the documented Linux backslash-path workaround (`dotnet run --project ./Generation/Hexalith.Projects.Client.Generation.csproj -- --repository-root ../.. --contract ../Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml --configuration ./nswag.json --output ./Generated/HexalithProjectsIdempotencyHelpers.g.cs`) — only additive (LinkMemory/UnlinkMemory operations + helpers + new request schemas), no hand edits.
- 2026-05-28 (dev): Memories dependency added via `Directory.Packages.props` central package management (`Hexalith.Memories.Client.Rest` and `Hexalith.Memories.Contracts` at v1.2.0) referenced only from `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj`. NuGet packages avoid the nested-submodule check on Memories' `Directory.Build.props` while preserving the "no nested recursive submodule init" constraint. No other Projects project takes a Memories dependency.
- 2026-05-28 (dev): Build environment: `DOTNET_ROOT=/home/administrator/.dotnet` (`dotnet --version` 10.0.300).
- 2026-05-28 (dev): No-sleep grep result in Story 2.7 new test files (ProjectAggregateMemoryTests, ProjectMemoryDirectoryTests): zero hits for `Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(`. Doc-comment text describing the rule does NOT call the methods.
- 2026-05-28 (dev): Zero `#pragma warning disable HXL001|HXL002` and zero `[SuppressMessage("Microsoft.Usage", "HXL00*")]` attributes across `src/Hexalith.Projects.*` and `tests/Hexalith.Projects.*`.

### Completion Notes List

- Story 2.7 context created from the Story 2.6 ADR ([[memories-link-target]], status `Accepted`) and the Story 2.5 reference implementation. Status set to `ready-for-dev`.
- The ADR's design is binding: a single Project Memory link targets one Memories `Case`; ACL calls only `MemoriesClient.GetCaseAsync`; zero HXL001 / HXL002 pragma suppressions; deterministic test convergence (no sleeps); identifier shape and failure mapping copied into this story file so the dev agent does not need to re-derive them.
- The dev agent will own: capability gate (Task 1), Projects contracts (Task 2), domain handlers (Task 3), Memories ACL (Task 4), endpoints + DI (Task 5), projections (Task 6), docs + leakage (Task 7), tests (Task 8), validation (Task 9).
- The Memories typed-client package reference must be added under `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` via `Directory.Packages.props` (central NuGet management).
- Validation is expected to run with `DOTNET_ROOT=/home/administrator/.dotnet` per [[build-environment]].

### Validation Summary (Task 9)

- Capability gate decision: **PASS** (no HALT). Memories surface (GetCaseAsync, Case/CaseStatus/ErrorResponse, HXL001/HXL002 annotations) matches the Story 2.6 ADR evidence; the Hexalith.Memories submodule pointer remained at the Story 2.6 baseline `608d15d`.
- Chosen ACL route: `MemoriesClient.GetCaseAsync(tenantId, caseId, cancellationToken)` — and only this route.
- `ProjectMemoryValidationOutcome` enum values realized: `Accepted`, `ValidationFailed`, `Denied`, `Archived`, `Stale` (defined but never produced by the GetCase ACL today — kept for taxonomy symmetry with `ProjectFileReferenceValidationOutcome`), `TenantMismatch`, `Unavailable`.
- `MaxMemoryReferences` chosen: **100** (parity with `MaxFileReferences`).
- Regenerated `.g.cs` digest summary: additive only — `LinkMemoryAsync` / `UnlinkMemoryAsync` on `IClient` + concrete `Client`; `LinkMemoryRequest` / `UnlinkMemoryRequest` schemas + their enum partitions; `ComputeIdempotencyHash` helpers added to both new request types. `git diff --stat` for the regenerated files shows only the additive surface; no hand-edits.
- Focused lane test counts: Hexalith.Projects.Tests **213/213**, Hexalith.Projects.Server.Tests **181/181**, Hexalith.Projects.Contracts.Tests **128/128**, Hexalith.Projects.Client.Tests **31/31**, Hexalith.Projects.Integration.Tests **14/14**.
- Full-solution `dotnet test Hexalith.Projects.slnx` count: **567/567 passing, 0 failed** (Story 2.6 baseline was 511; +56 from Story 2.7 — 33 aggregate, 5 projection memory branches, 5 leakage, 12 ACL, 4 client/contract spine; all green).
- `dotnet build Hexalith.Projects.slnx`: **0 warnings, 0 errors**.
- `git diff --check` on touched files: **clean** (no whitespace errors).
- Submodule pointers: **unchanged** (no advance of Hexalith.Memories or any other root-level submodule; the working-tree `m` flags on Commons/Conversations/Parties are pre-existing and unrelated to Story 2.7).
- `.g.cs` hand-edit check: `git diff` on regenerated files shows only the additive `LinkMemoryAsync` / `UnlinkMemoryAsync` / `LinkMemoryRequest` / `UnlinkMemoryRequest` / `ComputeIdempotencyHash` surface; no hand edits.
- Zero `#pragma warning disable HXL001|HXL002` and zero `[SuppressMessage("Microsoft.Usage", "HXL00*")]` across `src/Hexalith.Projects.*` and `tests/Hexalith.Projects.*`.
- HALT items: **none**.

### File List

**New files (Story 2.7 — added)**

- `src/Hexalith.Projects.Contracts/Commands/LinkMemory.cs`
- `src/Hexalith.Projects.Contracts/Commands/UnlinkMemory.cs`
- `src/Hexalith.Projects.Contracts/Events/MemoryLinked.cs`
- `src/Hexalith.Projects.Contracts/Events/MemoryUnlinked.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectMemoryReference.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectMemoryReferenceMetadata.cs`
- `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.Memories.cs`
- `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs`
- `src/Hexalith.Projects.Server/Memories/MemoriesProjectMemoryDirectory.cs`
- `src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationResult.cs`
- `src/Hexalith.Projects.Server/Memories/UnavailableProjectMemoryDirectory.cs`
- `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateMemoryTests.cs`
- `tests/Hexalith.Projects.Server.Tests/ProjectMemoryDirectoryTests.cs`

**Modified files (Story 2.7 — extended)**

- `Directory.Packages.props` (added `Hexalith.Memories.Client.Rest` 1.2.0 + `Hexalith.Memories.Contracts` 1.2.0 under central package management)
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (added `LinkMemory` + `UnlinkMemory` operations, `MemoryReferenceId` parameter, `LinkMemoryRequest` / `UnlinkMemoryRequest` / `ProjectMemoryReferenceMetadata` schemas, request examples; existing `referenceKind` enums were not re-emitted — `memory` was already present at both sites)
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` (regenerated via NSwag — additive `LinkMemoryAsync` / `UnlinkMemoryAsync` surface only)
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` (regenerated — additive `LinkMemoryRequest.ComputeIdempotencyHash` / `UnlinkMemoryRequest.ComputeIdempotencyHash`)
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` (added `Validate(LinkMemory)` / `Validate(UnlinkMemory)` and `ComputeLinkMemoryFingerprint` / `ComputeUnlinkMemoryFingerprint` helpers — byte-identical canonical-line shape to file-reference fingerprints, byte-identical to the regenerated client helper hash)
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidationResult.cs` (added `AcceptedMemoryLink` / `AcceptedMemoryUnlink` factories)
- `src/Hexalith.Projects/Aggregates/Project/ProjectResult.cs` (extended `IsAccepted`, the rejection-mapping `referenceKind` switch, `ToRejectionEvent` switch arms, and `ToRejectionReason` with the two new accepted/conflict codes)
- `src/Hexalith.Projects/Aggregates/Project/ProjectResultCode.cs` (added `MemoryLinked`, `MemoryUnlinked`, `MemoryReferenceConflict`, `MemoryReferenceLimitExceeded`)
- `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs` (added bounded `MemoryReferences` map + `MaxMemoryReferences = 100` constant + updated `Empty` factory)
- `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs` (added `MemoryLinked` / `MemoryUnlinked` switch arms + `AddMemoryReference` / `RemoveMemoryReference` helpers + extended `ProjectCreated` reset to clear `MemoryReferences`)
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` (added `MemoryReferences` field)
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailProjection.cs` (added `MemoryLinked` / `MemoryUnlinked` switch arms, `UpsertMemoryReference` / `RemoveMemoryReference` helpers)
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs` (added disjoint `memory`-kind lane with `MemoryLinked` / `MemoryUnlinked` switch arms)
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` (added `LinkMemoryAction` / `UnlinkMemoryAction` constants + `AuthorizeLinkMemoryAsync` / `AuthorizeUnlinkMemoryAsync` helpers)
- `src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs` (added `SubmitLinkMemoryAsync` / `SubmitUnlinkMemoryAsync` implementations)
- `src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` (added `PackageReference Hexalith.Memories.Client.Rest` + `Hexalith.Memories.Contracts`)
- `src/Hexalith.Projects.Server/IProjectCommandSubmitter.cs` (added `SubmitLinkMemoryAsync` / `SubmitUnlinkMemoryAsync` to the interface)
- `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs` (added action-token mapping, command-type routing, `ProcessLinkMemory` / `ProcessUnlinkMemory` handlers, `LinkMemoryPayload` / `UnlinkMemoryPayload` payload records, extended `ToContractCommandType` and `ToDomainResult` accepted-code list)
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` (added `LinkMemory` / `UnlinkMemory` endpoint maps, handler methods that gate authorization before any ACL call and the unlink path takes no `IProjectMemoryDirectory` dependency, `MemoryReferenceValidationProblem` mapper, `LinkMemoryHttpRequest` / `UnlinkMemoryHttpRequest` private records, extended `ToProjectReferenceSummaries` with the `memory` kind)
- `src/Hexalith.Projects.Server/ProjectsServerModule.cs` (added `LinkMemoryCommandType` / `UnlinkMemoryCommandType` constants)
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` (added `IProjectMemoryDirectory` TryAddTransient registration with `UnavailableProjectMemoryDirectory` fail-closed fallback + `AddMemoriesClient` typed-client registration in `AddProjectsServerRuntimeInfrastructure`)
- `docs/event-catalog.md` (added `MemoryLinked` / `MemoryUnlinked` entries and extended the rejection-event entries with the `memory` reference kind)
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` (added Memory-event metadata-only leakage tests plus an unsafe-identifier dropping test)
- `tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs` (added a `DetailProjection_AppliesMemoryLinkedAndUnlinked` branch plus helper factories)
- `tests/Hexalith.Projects.Tests/Projections/ProjectReferenceIndexProjectionTests.cs` (added five disjoint-lane tests proving memory unlink/link/folder-replacement/file-unlink boundaries plus helper factories)
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` (extended `FakeProjectCommandSubmitter` with memory link/unlink submission stubs, extended `FixedProjectTenantContextAccessor` permissions list, and propagated the `MemoryReferences` constructor argument on `ProjectDetailItem`)
- `tests/Hexalith.Projects.Server.Tests/ProjectAuthorizationGateTests.cs` (propagated the `MemoryReferences` constructor argument on `ProjectDetailItem` instances)
- `tests/Hexalith.Projects.Server.Tests/ProjectQueryTenantFilterTests.cs` (propagated the `MemoryReferences` constructor argument on `ProjectDetailItem` instances)
- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs` (added `Spine_MemoryReferenceOperations_AreCommandAsyncMutations` and `Spine_MemoryReferenceSchemas_AreClosedCamelCaseAndMetadataOnly` tests + supporting helpers + extended the required schema list)
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` (added `LinkMemoryHelperUsesDeclaredLexicographicFields` and `UnlinkMemoryHelperUsesDeclaredLexicographicFields` cross-surface fingerprint tests + extended the generated-class and helper-presence assertions)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (set `2-7-link-unlink-memory` from `ready-for-dev` to `in-progress` to `review` over the dev-story workflow)

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-05-28 | 1.1 | Implemented Story 2.7 end-to-end. Capability gate PASSED (no HALT — `GetCaseAsync` stable, HXL annotations intact, `Case`/`CaseStatus`/`ErrorResponse` shapes match the Story 2.6 ADR). Added `LinkMemory`/`UnlinkMemory` commands, `MemoryLinked`/`MemoryUnlinked` events, `ProjectMemoryReference`/`ProjectMemoryReferenceMetadata` models; `IProjectMemoryDirectory` + `MemoriesProjectMemoryDirectory` (calls only `MemoriesClient.GetCaseAsync`) + `ProjectMemoryValidationResult` + `UnavailableProjectMemoryDirectory` ACL under `src/Hexalith.Projects.Server/Memories/`; `ProjectAggregate.Memories.cs` aggregate handlers; `ProjectState.MemoryReferences` (bounded, `MaxMemoryReferences=100`) + `ProjectStateApply` extensions; `ProjectDetailProjection` + disjoint memory-kind `ProjectReferenceIndexProjection` lane; `ProjectAuthorizationGate.AuthorizeLinkMemoryAsync`/`AuthorizeUnlinkMemoryAsync` (action tokens `projects:link_memory`/`projects:unlink_memory`); `IProjectCommandSubmitter`/`EventStoreProjectCommandSubmitter`/`ProjectsDomainProcessor` extensions; OpenAPI link/unlink routes (referenceKind enums already reserved `memory` at both sites — not re-emitted); regenerated client + idempotency helpers via NSwag (Linux backslash workaround); `Hexalith.Memories.Client.Rest`/`.Contracts` 1.2.0 added via `Directory.Packages.props` central package management referenced only from `Hexalith.Projects.Server.csproj`; DI via `TryAddTransient` and `AddMemoriesClient` in `AddProjectsServerRuntimeInfrastructure`; `docs/event-catalog.md` entries; leakage harness extended with Memories payload field list; full test matrix added (Aggregate Tier-1, Projection branches, Memories ACL with recording HTTP handler, OpenAPI spine, client fingerprint parity, no-payload-leakage). Constraints met: 0 `#pragma warning disable HXL001/HXL002` and 0 `[SuppressMessage]` HXL attrs; ACL calls only `GetCaseAsync`; no Projects-owned `MemoryId`/`CaseId` VO; no new shared-vocabulary enum values; no `.g.cs` hand-edits; Hexalith.Memories submodule pointer unchanged at `608d15d`; no nested recursive submodule init; deterministic tests only (zero `Thread.Sleep`/`Task.Delay`/`SpinWait`/`Task.Yield`). Validation: `dotnet build Hexalith.Projects.slnx` 0W/0E; full solution **567/567** passing (Projects.Tests 213, Projects.Server.Tests 181, Projects.Contracts.Tests 128, Projects.Client.Tests 31, Projects.Integration.Tests 14); `git diff --check` clean; status moved to `review`. | Claude Opus 4.7 |
| 2026-05-28 | 1.0 | Created Story 2.7 artifact and set sprint status to `ready-for-dev`. Story consumes the Story 2.6 ADR (Option A — Case-level link via `MemoriesClient.GetCaseAsync`, stable since Memories Story 10.2) and extends the Story 2.5 optional-reference pattern (`LinkFileReference` / `UnlinkFileReference`) to the `memory` reference kind. Adds `LinkMemory` / `UnlinkMemory` commands, `MemoryLinked` / `MemoryUnlinked` events, `ProjectMemoryReference` + `ProjectMemoryReferenceMetadata` models, `IProjectMemoryDirectory` + `MemoriesProjectMemoryDirectory` + `ProjectMemoryValidationResult` + `UnavailableProjectMemoryDirectory` ACL under `src/Hexalith.Projects.Server/Memories/`, OpenAPI link/unlink routes (referenceKind enums already reserve `memory`), regenerated client + idempotency helpers, `ProjectAggregate.Memories.cs` partial, `ProjectState.MemoryReferences` (bounded, `MaxMemoryReferences`), `ProjectStateApply` extensions, `ProjectDetailProjection` + disjoint memory-kind `ProjectReferenceIndexProjection` lane, authorization gate helpers (`projects:link_memory` / `projects:unlink_memory`), command submitter / domain processor extensions, DI registration via `TryAddTransient` and `AddMemoriesClient` in `AddProjectsServerRuntimeInfrastructure`, `docs/event-catalog.md` entries for both events, leakage harness extension covering Memories payload field list, and the full Tier-1 / Tier-2 / integration test matrix. Constraints: zero `#pragma warning disable HXL001` / `HXL002` suppressions; ACL calls only `GetCaseAsync`; no Projects-owned `MemoryId` / `CaseId` VO; no new shared-vocabulary enum values; no `.g.cs` hand-edits; no `Hexalith.Memories` submodule pointer change; no nested recursive submodule init; deterministic tests (no `Thread.Sleep` / `Task.Delay` / `SpinWait` / wall-clock polling). | Claude Opus 4.7 |
