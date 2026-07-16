---
baseline_commit: 08aa616cf49de1d483b35fe2ea95fcb3a9a45882
---

# Story 4.3: Resolve Project From Attachments

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to ask Projects to resolve candidate Projects from an attached Project Folder or file references**,
so that **a user who starts with files before choosing a project is matched to the right workspace** _(FR-13; realizes UJ-2)_.

## Acceptance Criteria

AC1 and AC2 are the authoritative epic ACs (BDD, verbatim — epics.md:843-851). ACs 3–8 are the engineering acceptance criteria derived from the architecture, the Story 4.1 engine contract, and the shipped Story 4.2 conversation-resolution endpoint; they make "done" checkable and prevent false completion. 4.3 is the **attachment analog of Story 4.2** — same pipeline, different evidence source and reason codes.

**AC1 — Resolution outcome from attachments (FR-13).**
**Given** attached Project Folder / File references
**When** `ResolveProjectFromAttachments` runs the engine (4.1) over existing folder/file references
**Then** matching considers existing Project Folder and File References and tags `ProjectFolderMatched` / `FileReferenceMatched` reason codes where applicable, returns `NoMatch` / `SingleCandidate` / `MultipleCandidates`, and excludes archived Projects unless explicitly requested.

**AC2 — Fail-closed authorization & raw-content boundary (NFR-2, NFR-3).**
**Given** missing or stale Folder/File authorization
**When** matching runs
**Then** it fails closed (degraded reference state never produces a positive match — it surfaces as a `ResolutionExclusion`), and raw file contents are **never** treated as Project-owned data (no file bytes, byte ranges, workspace ids, or paths are read, matched, returned, or logged).

**AC3 — Synchronous read query, persists nothing (AR-10).**
**Given** the compute-on-demand rule
**When** `ResolveProjectFromAttachments` executes
**Then** it is a **synchronous REST GET query** that writes **no** event, projection, state, or resolution trace (only the later `ConfirmProjectResolution` command in Story 4.4 persists), and it returns the existing `ProjectResolution` wire model unchanged (no new model, no new vocabulary member).

**AC4 — Reuse the pure engine, do not duplicate scoring.**
**Given** the Story 4.1 engine is the single source of truth for scoring/ranking/outcome
**When** candidates are evaluated
**Then** the handler enumerates candidate evidence (which projects reference the attached folder/file ids, with their cached `ReferenceState`), maps it to `ProjectResolutionCandidateEvidence` + `ProjectResolutionMatchSignal` via a **pure Tier-1 mapper**, and delegates to `ProjectResolutionEngine.Resolve(context, candidates)` — it does **not** re-implement the qualifying/scoring/ranking/threshold heuristic, and attachment matches use **only** the `ProjectFolderMatched` and `FileReferenceMatched` reason codes.

**AC5 — Query contract conventions.**
**Given** the query is a read endpoint
**When** the request is processed
**Then** an `Idempotency-Key` header is **rejected** (`400 validation_error`, field `idempotency_key`), `X-Correlation-Id` / `X-Hexalith-Task-Id` are threaded, the requested freshness (if present) must be `eventually_consistent` (else `400`), and the response sets `X-Hexalith-Freshness: eventually_consistent`.

**AC6 — Safe-denial & RFC 9457.**
**Given** an unauthorized caller, unverifiable tenant evidence, or a missing/malformed attachment identifier
**When** resolution runs
**Then** the boundary returns an RFC 9457 ProblemDetails `404` safe-denial (unauthorized and nonexistent indistinguishable) — never a `500`, never an empty leak — using the existing `SafeDenial` / `ValidationProblem` / `ReadModelUnavailable` helpers; read-model unavailability returns `503 read_model_unavailable retryable:true`.

**AC7 — No payload leakage (NFR-2).**
**Given** the metadata-only boundary
**When** the response and any logs are produced
**Then** no file contents, byte ranges, workspace ids, raw/normalized paths, transcript text, prompt fragments, memory bodies, secrets, raw tokens, command bodies, or filesystem paths appear, the serialized `ProjectResolution` carries **no `tenantId`**, and this is proven by `NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(...)` plus a cross-tenant isolation negative test.

**AC8 — Contract spine + generated client stay in lockstep.**
**Given** the OpenAPI 3.1 Contract Spine is the single source of truth
**When** the `ResolveProjectFromAttachments` operation is added to `hexalith.projects.v1.yaml`
**Then** the typed client is **regenerated** (never hand-edited) and the artifacts-fingerprint verification test passes (spine SHA-256 == generated client), and `dotnet build … -warnaserror` and the full `dotnet test` lane stay green. The `ProjectResolution` / `ResolutionCandidate` / `ResolutionExclusion` schemas already exist in the spine (added by 4.2) — **reuse them; do not redefine.**

## Tasks / Subtasks

- [x] **Task 1 — Candidate enumeration: which projects reference the attached folder/file ids (AC1, AC4).** This is the **central design task** and the inverse of Story 4.2. 4.2 read one conversation, then enumerated all tenant projects. 4.3 receives one-or-more attachment ids and must find the projects that already reference them. (AC: 1, 4)
  - [x] **Decide and document the enumeration source in the Dev Agent Record** (mirroring how 4.2 documented its enumeration decision). The epic scopes FR-13 to resolution "via reference index + ACLs" (epics.md:211) and NFR-5 favors "precomputed tenant-scoped projections over request-time fan-out", so the **recommended** path is a tenant-scoped **reverse lookup over `ProjectReferenceIndexProjection`** (attached `folderId` / `fileId` → the project rows that reference them). Today the index exposes **only** the forward `List(tenantId, projectId)` (`ProjectReferenceIndexProjection.cs:153`) and has **no Server read-model interface at all** (`IProjectProjectionStore` exposes only `ListAsync` / `GetDetailAsync` / `GetReadinessAsync`). Closing that gap is in-scope: add a tenant-scoped `IProjectReferenceIndexReadModel` (+ Dapr + InMemory impls, mirroring the `DaprProjectListReadModel` / `InMemoryProjectListReadModel` pair) exposing a reverse-by-reference query, **or** a reverse secondary index on the projection.
  - [x] **Documented fallback (only if the reverse read surface proves infeasible this sprint):** enumerate authorized tenant projects via `IProjectListReadModel.ListAsync` (as 4.2 did), then read each project's `ProjectDetailItem.ProjectFolder` / `FileReferences` via `IProjectDetailReadModel.GetAsync` and match the attachment ids. Note this is an **O(projects) N+1 detail fan-out** and a documented NFR-5 latency watch-item — bound it (paging / cap) and justify in the record. `ProjectListItem` carries **no** folder/file references, so the list read model alone is insufficient for matching.
  - [x] Apply `ProjectQueryTenantFilter.FilterList` (defense-in-depth Ordinal tenant re-filter) to any enumerated rows before building candidates — never delegate tenant scoping to the engine alone.
- [x] **Task 2 — Match keys & per-reference trust state (AC1, AC2).** Build, for each matched project, the candidate evidence with the correct reason code and the **cached `ReferenceState`** from the reference index row. (AC: 1, 2)
  - [x] **Project Folder match → `ProjectFolderMatched`:** attached `folderId` Ordinal-equals a project's `ProjectFolderReference.FolderId` (both are Folders-owned ULID strings — see ADR `docs/adr/identifier-boundary.md`). `ReferenceKind = "folder"`, `ReferenceId = folderId`.
  - [x] **File Reference match → `FileReferenceMatched`:** attached `fileId` Ordinal-equals a project's `ProjectFileReference.FileReferenceId`. `ReferenceKind = "file"`, `ReferenceId = fileReferenceId`. **Verify the file-identity contract** (see Dev Notes "Attachment input contract & match keys" and the open question): the ADR holds sibling references as the owning context's ULID string, but `ProjectFileReference`'s XML doc currently calls `FileReferenceId` "Projects-owned opaque" — reconcile what opaque id the Chatbot presents for an attached file with what the reference index stores before finalizing the match key. Use **`StringComparison.Ordinal`** for all id equality (consistent with the engine's Ordinal ordering and the ACL's Ordinal scope echoes) — **not** `OrdinalIgnoreCase`.
  - [x] Carry the index row's `ReferenceState` onto **every** emitted signal (do **not** hardcode `Included`). Only `ReferenceState.Included` contributes to score; `Stale` / `Unauthorized` / `Unavailable` / `Excluded` / `Archived` / `Pending` / `TenantMismatch` / … pass through so the engine surfaces them as `ResolutionExclusion` rows (this is how AC2 fail-closed is satisfied).
  - [x] Skip the **pending Project Folder** edge case: the index can hold a folder row with `ReferenceState.Pending` and a `null`/placeholder reference id (key `_pending_project_folder`, from `ProjectFolderCreationPending`). A pending/null folder can never be a positive match — emit it as a non-`Included` signal (engine maps `Pending` → diagnostic `ProjectFolderPending`) or omit it; never crash the mapper on a null id.
- [x] **Task 3 — New pure Tier-1 evidence mapper + input records (AC2, AC4).** Mirror the shipped conversation trio exactly. (AC: 2, 4)
  - [x] Create `src/Hexalith.Projects/Resolution/AttachmentResolutionEvidenceMapper.cs` (static, pure, no infrastructure, no wall-clock — `now` passed in) cloning `ConversationResolutionEvidenceMapper.cs`: per candidate build a `List<ProjectResolutionMatchSignal>`, stamp each with its `ReferenceState` and `now`, **skip candidates with zero signals**, and **never** score/rank/decide outcome.
  - [x] Create the metadata-only input record(s) — `AttachmentResolutionMetadata` (the presented/attached refs + their derived `ReferenceState`, with a `FailClosed(referenceId, ReferenceState = Unavailable)` factory) and an attachment candidate record carrying `ProjectId` + `DisplayName` + `Lifecycle` + the project's matched folder/file ids and their states — cloning `ConversationResolutionMetadata.cs` / `ConversationResolutionProjectCandidate.cs`. These carry **only** opaque ids + safe display name + `ReferenceState` — never paths, workspace ids, or content.
  - [x] Emit **only** `ProjectReasonCode.ProjectFolderMatched` (kind `"folder"`) and `ProjectReasonCode.FileReferenceMatched` (kind `"file"`). Do **not** emit `MetadataMatched` (reserved for the conversation path per the scoring heuristic) unless explicitly added as a deliberate, documented heuristic.
- [x] **Task 4 — Tenant authority + fail-closed wiring (AC2, AC6).** (AC: 2, 6)
  - [x] Derive `AuthoritativeTenantId` and principal **only** from authenticated claims via `IProjectTenantContextAccessor` — never from an attachment id, request body, header, or query parameter.
  - [x] Authorize **before** any work via `ProjectAuthorizationGate.AuthorizeListAsync` (tenant-level read; resolution is **not** project-scoped). On denial: `authorization.Retryable && Reason == Unavailable ? ReadModelUnavailable(...) : SafeDenial(...)`.
  - [x] Build `ProjectResolutionContext(AuthoritativeTenantId, RequestedTenantId, IncludeArchived, Now, CorrelationId, TaskId, PresentedInputIds)` with `AuthoritativeTenantId == RequestedTenantId`, `Now = timeProvider.GetUtcNow()` (the engine's only clock), and `PresentedInputIds` = the attached folder/file ids (metadata-only, **never** emitted to the wire). The engine re-asserts tenant authority and emits all candidates as `TenantMismatch` exclusions → `NoMatch` when unverifiable; preserve that contract end-to-end.
- [x] **Task 5 — Resolve query endpoint (AC1, AC3, AC4, AC5, AC6).** (AC: 1, 3, 4, 5, 6)
  - [x] Create `src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs` as a `public static partial class ProjectsDomainServiceEndpoints` with `private static async Task<IResult> ResolveProjectFromAttachmentsAsync(...)`. Use `ResolveProjectFromConversationEndpoint.cs` as the **exact** structural template (validate headers → validate attachment query → authorize → enumerate candidate evidence → run pure mapper → build context → `engine.Resolve` → `Results.Json`).
  - [x] Register the route in `ProjectsDomainServiceEndpoints.MapProjectsDomainServiceEndpoints` next to the `from-conversation` GET (`ProjectsDomainServiceEndpoints.cs:197`). Recommended shape: `GET /api/v1/projects/resolution/from-attachments?folderId={id}&fileId={id}&fileId={id}&includeArchived={bool}` with `.WithName("ResolveProjectFromAttachments")`; finalize the exact path/param names in the spine. Validate every id with `IsCanonicalIdentifier` (malformed → `SafeDenial`). Require **at least one** attachment id; define max-count / dedup / deterministic ordering of inputs.
  - [x] Read + canonicalize `X-Correlation-Id` / `X-Hexalith-Task-Id`; reject `Idempotency-Key` (`ValidationProblem(..., "idempotency_key")`); validate freshness (`eventually_consistent` only); parse `includeArchived` via `TryReadIncludeArchived`; set `X-Hexalith-Freshness` on the response; return `Results.Json(resolution, ResponseJsonOptions)`.
  - [x] Resolve `ProjectResolutionEngine` from DI (already registered: `TryAddTransient<ProjectResolutionEngine>()`), call `engine.Resolve(context, candidates)`, return the `ProjectResolution`. Persist nothing; return no `202`.
  - [x] Register any new read model / ACL in `ProjectsServerServiceCollectionExtensions` following the existing `TryAddTransient` + `null → Unavailable*` fallback pattern (`ProjectsServerServiceCollectionExtensions.cs:74-94`). The new pure mapper is a static class (no DI).
- [x] **Task 6 — OpenAPI spine + client regeneration (AC8).** (AC: 8)
  - [x] Add the `ResolveProjectFromAttachments` path + `operationId` to `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`, mirroring the `ResolveProjectFromConversation` block (read-consistency `eventually_consistent`, correlation, authorization, canonical-error-categories, `X-Correlation-Id` + `X-Hexalith-Freshness` response headers, 400/401/403/404/503 `$ref` responses). **Reference the existing `ProjectResolution` schema — do not add a new one** (4.2 already defined it).
  - [x] Regenerate the client (`dotnet msbuild /t:GenerateHexalithProjectsClient`); never hand-edit `Client/Generated/*.g.cs`. A GET produces a typed client method but **no** idempotency helper.
  - [x] Re-run the artifacts-fingerprint verification test (`HexalithProjectsGeneratedArtifacts.VerifyCurrent`) — it fails if the spine changed without regeneration.
- [x] **Task 7 — Tests (AC1–AC8).** (AC: 1, 2, 3, 4, 5, 6, 7, 8)
  - [x] Tier-1: mapper tests for the attachment adapter using `ProjectResolutionEvidenceBuilder` — `NoMatch` (no qualifying signal / attachment referenced by no project), `SingleCandidate` (one `ProjectFolderMatched` or one `FileReferenceMatched` — each qualifies alone at weight 45/35 ≥ 20), `MultipleCandidates` (≥2 qualifying), one project matched by **both** an attached folder and file (accumulates both reason codes, distinct weights summed once each), archived-excluded-by-default vs `includeArchived=true`, and degraded states (`Stale` / `Unauthorized` / `Unavailable` / `Pending`) → exclusion (not a match).
  - [x] Tier-1: tenant fail-closed — missing/blank `AuthoritativeTenantId` and `RequestedTenantId != AuthoritativeTenantId` → every candidate `TenantMismatch` → `NoMatch`; assert the structured warning via `RecordingLogger`.
  - [x] Tier-1: leakage — `NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(result)`; serialized output `ShouldNotContain("tenantId")` and the tenant value; assert no path / workspace-id / content tokens.
  - [x] Tier-2 (Server.Tests): endpoint tests cloning `ResolveProjectFromConversationTests.cs` with in-memory read-model fakes — happy single/multiple/no-match, `Idempotency-Key` rejected, freshness + `includeArchived` validation, **missing/malformed attachment id → safe-denial 404**, 503 on read-model unavailable, `X-Hexalith-Freshness` set, correlation/task threading. Exercise the canonical query negative-test rows from `docs/checklists/mutation-and-query-negative-tests.md`.
  - [x] Cross-tenant isolation negative test via `ProjectTenantIsolationConformance` (an attachment referenced only by a tenant-B project never yields a tenant-A candidate; and "authorized attachment referenced by no project" → `NoMatch` with no existence leak).
  - [x] Build with the pinned SDK and verify the fingerprint gate: `/home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx -warnaserror` then `dotnet test Hexalith.Projects.slnx --no-build`.

## Dev Notes

### The one thing to get right

Story 4.3 is the **attachment analog of the shipped Story 4.2** (`Resolve From Conversation`). The pure machinery is **done and frozen**: `ProjectResolutionEngine.Resolve(...)` owns ALL scoring/ranking/threshold/outcome; the wire model `ProjectResolution` (no `tenantId`), the evidence shapes, the scoring rules (`ProjectFolderMatched = 45`, `FileReferenceMatched = 35`, `MinimumQualifyingScore = 20`), and the reason codes already exist. **4.3 adds only the impure host adapter, a new pure evidence mapper, and the reverse candidate-enumeration.** Concretely, 4.3 must:

1. Authorize at the API edge (claims-only tenant, fail-closed, tenant-level read).
2. **Find which projects reference the attached folder/file ids** (the inverse direction of 4.2 — see "Candidate enumeration").
3. Read each matched reference's cached `ReferenceState` from the Projects-owned reference index (the Folders ACL authorization already ran at link/refresh time and its result is recorded there).
4. Map evidence → `ProjectResolutionCandidateEvidence` / `ProjectResolutionMatchSignal` (`ProjectFolderMatched` / `FileReferenceMatched` only) via a **pure** mapper.
5. Build `ProjectResolutionContext` and call `engine.Resolve(...)`.
6. Return `ProjectResolution` (no `tenantId`), persisting nothing.

Do **not** re-implement scoring/ranking/threshold logic — the engine and `docs/resolution-scoring-heuristic.md` are the single source of truth, and that doc names 4.3 explicitly: "Stories 4.2 (`Resolve From Conversation`) and 4.3 (`Resolve From Attachments`) pre-fetch evidence and call this engine. They do not duplicate these scoring, ranking, or outcome rules" (`docs/resolution-scoring-heuristic.md:5`).

### `ResolveProjectFromAttachments` is a QUERY, not a command

Reads are synchronous REST query endpoints; only mutations are command-async (202). Resolution writes nothing; the **only** resolution event, `ProjectResolutionConfirmed`, is emitted later by the separate `ConfirmProjectResolution` command (Story 4.4). Therefore 4.3:
- is a **GET** query endpoint (compute-on-demand), **not** an `IProjectCommand` + `IProjectCommandSubmitter` flow;
- **rejects** `Idempotency-Key` (`400 validation_error`, field `idempotency_key`) — idempotency-key is required on mutations, rejected on queries;
- carries freshness/trust state on the response (`X-Hexalith-Freshness: eventually_consistent`);
- returns no `202`, writes no event/projection/state/trace.

### Engine contract 4.3 must satisfy (from Story 4.1 code — exact, unchanged)

- **Entry point (sync):** `ProjectResolutionEngine.Resolve(ProjectResolutionContext context, IReadOnlyList<ProjectResolutionCandidateEvidence> candidates) → ProjectResolution` (`ProjectResolutionEngine.cs:40`). Both args null-guarded. DI already registered: `services.TryAddTransient<ProjectResolutionEngine>()`.
- **Context inputs (4.3 populates):** `ProjectResolutionContext(string? AuthoritativeTenantId, string? RequestedTenantId, bool IncludeArchived, DateTimeOffset Now, string? CorrelationId = null, string? TaskId = null, IReadOnlyList<string>? PresentedInputIds = null)`. `Now` is the engine's only clock source (echoed to `ProjectResolution.ObservedAt`). `CorrelationId` / `TaskId` / `PresentedInputIds` are logging/metadata-only and **never** emitted to the wire.
- **Candidate evidence (4.3 builds):** `ProjectResolutionCandidateEvidence(string ProjectId, string? DisplayName, ProjectLifecycle Lifecycle, IReadOnlyList<ProjectResolutionMatchSignal> Signals)`; `ProjectResolutionMatchSignal(string ReferenceKind, string ReferenceId, ProjectReasonCode ReasonCode, ReferenceState ReferenceState, DateTimeOffset ObservedAt)`. For 4.3: `ReferenceKind ∈ {"folder","file"}`, `ReferenceId` = the opaque folder/file id, `ReasonCode ∈ {ProjectFolderMatched, FileReferenceMatched}`. Only `ReferenceState.Included` contributes to score; every other state becomes a `ResolutionExclusion`.
- **Output (4.3 returns unchanged):** `ProjectResolution(ResolutionResult Result, IReadOnlyList<ResolutionCandidate> Candidates, IReadOnlyList<ResolutionExclusion> Excluded, DateTimeOffset ObservedAt)` with factories `NoMatch` / `SingleCandidate` / `MultipleCandidates`. **Deliberately no `TenantId` field** (`ProjectResolution.cs:16`).
- **Scoring (engine-owned, do not duplicate):** per-reason-code weights `ConversationLinked=50, ProjectFolderMatched=45, FileReferenceMatched=35, MemoryMatched=30, MetadataMatched=20`; each reason code counts **at most once** per candidate (a project matched by three files still scores `+35` once); `MinimumQualifyingScore = 20` (so a single folder **or** file match alone qualifies); outcome decided solely by qualifying-candidate count (0→NoMatch, 1→SingleCandidate, 2+→MultipleCandidates); deterministic order = score desc, then `ProjectId` Ordinal asc, `Rank = index + 1`. **For attachment resolution only `ProjectFolderMatched` and `FileReferenceMatched` are in play.**
- **Tenant fail-closed (engine-enforced):** `AuthoritativeTenantId` blank, or `RequestedTenantId` present and `!= AuthoritativeTenantId` ⇒ every candidate excluded `TenantMismatch` / `tenantMismatch` ⇒ `NoMatch`, plus one structured warning. 4.3 must feed a server-derived `AuthoritativeTenantId == RequestedTenantId` so this gate behaves correctly — and still tenant-scope candidate enumeration at the read-model boundary (`ProjectQueryTenantFilter.FilterList`); never rely on the engine alone for tenant scoping.
- **Archived exclusion:** `Lifecycle == ProjectLifecycle.Archived && !IncludeArchived` ⇒ excluded (`Archived` / `projectArchived`). "Explicitly requested" = `IncludeArchived: true` (drive it from the `includeArchived` query param, default `false`).

### Candidate enumeration — the central design task (inverse of 4.2)

4.2 read one conversation, then enumerated all tenant projects via `IProjectListReadModel.ListAsync` and derived per-project signals. **4.3 must invert this:** the input is one-or-more attachment ids, and the host must find the projects that already reference them.

- There is **no reverse `(folderId|fileId) → projectIds` lookup** anywhere today. `ProjectReferenceIndexProjection` exposes only forward `List(tenantId, projectId)` (`ProjectReferenceIndexProjection.cs:153`), and the projection is **not read by any runtime read model** — `IProjectProjectionStore` exposes only `ListAsync` / `GetDetailAsync` / `GetReadinessAsync`. `ProjectListItem` carries no references; only `ProjectDetailItem.ProjectFolder` / `FileReferences` link a project to its folder/file ids (`ProjectDetailItem.cs:32`).
- **Recommended approach (NFR-5-aligned, matches FR-13 coverage "via reference index + ACLs" — epics.md:211):** add a tenant-scoped reverse read surface over the reference index — `IProjectReferenceIndexReadModel` with a reverse-by-reference query, plus Dapr + InMemory impls mirroring the `DaprProjectListReadModel` / `InMemoryProjectListReadModel` pair (and a reverse secondary index on the projection if the Dapr key shape `…:references:{kind}:{referenceId}` can't be queried by reference id alone). The index row already carries everything the candidate needs: `ProjectId`, `ReferenceKind`, `ReferenceId`, `ReferenceState`, `DisplayName`, `ReasonCode`, `UpdatedAt`.
- **Documented fallback (only if the reverse surface is infeasible this sprint):** enumerate tenant projects via `IProjectListReadModel.ListAsync`, then `IProjectDetailReadModel.GetAsync` per project to read its references and match. This reuses fully-wired reads but is an **O(projects) N+1 detail fan-out** — a documented NFR-5 latency watch-item; bound it and justify in the Dev Agent Record.
- **Whichever you choose, record it in the Dev Agent Record** (Story 4.2 set this precedent — it documented its enumeration decision as the central design choice). Do **not** silently scan unbounded.

### Trust source — cached `ReferenceState`, because a live file re-check is infeasible

AC2 requires fail-closed on "missing or stale Folder/File authorization." The cleanest v1 trust source is the **`ReferenceState` already cached on the reference index row** — it reflects the Folders ACL authorization captured at link / `RefreshProjectContext` time. Only `Included` rows produce positive signals; `Stale` / `Unauthorized` / `Archived` / `Pending` / `Unavailable` rows pass through as exclusions. This satisfies "via reference index + ACLs" because the ACLs already ran and their verdict is recorded.

A request-time re-verify is **asymmetric** and mostly blocked:
- **Folder:** `IProjectFolderDirectory.RefreshFolderReferenceAsync(projectId, folderId, correlationId)` can re-check a folder by id (lifecycle + effective-permissions). This is an **optional bounded escalation**, not v1 scope — it adds per-attachment Folders fan-out (NFR-5 watch-item).
- **File:** `FoldersProjectFileReferenceDirectory.RefreshFileReferenceAsync` **hard-fails closed to `Unavailable`** (`FoldersProjectFileReferenceDirectory.cs:147-172`) — there is no upstream opaque-id file read route, and Projects must never store `workspaceId`/`filePath`. So a live file re-check would turn **every** file match into an exclusion. **v1 file matching must rely on the cached `ReferenceState`.** State this explicitly.

If you do add a bounded folder re-check, reuse the existing pure outcome→state mappers (`ProjectFolderValidationOutcomeMapper.Map`, `ProjectFileReferenceValidationOutcomeMapper.Map`) rather than writing new mapping logic — but note their `(projectionStored, now)` `ObservedAt`-preservation tuple is Story-3.4-Refresh-specific and likely irrelevant to a stateless resolution read; reuse only the outcome→`ReferenceState` switch.

### Attachment input contract & match keys

- **Match by opaque ULID string, Ordinal equality** (per ADR `docs/adr/identifier-boundary.md`: all sibling references are held as the owning context's plain ULID string; Projects mints only `ProjectId`).
  - **Folder:** attached `folderId` `StringComparison.Ordinal`-equals `ProjectFolderReference.FolderId` (both Folders-owned) → `ProjectFolderMatched`.
  - **File:** attached `fileId` Ordinal-equals `ProjectFileReference.FileReferenceId` → `FileReferenceMatched`.
- **⚠ Verify the file-identity contract before finalizing (open question — see end):** `ProjectFileReference`'s XML doc calls `FileReferenceId` "Projects-owned opaque", which would mean the Chatbot can only match files it previously linked (it would not know a Projects-minted id for a brand-new attachment). The ADR says file references reuse the **Folders** id representation. Reconcile these: confirm what opaque id the Chatbot presents for an attached file and that it equals what the reference index stores in the file-kind `ReferenceId`. If they diverge, the match key (or the file-identity stored at `LinkFileReference` time, Story 2.5) must be adjusted. The **folder** path is unambiguous and is the primary, always-correct match.
- **Endpoint input shape** is not pinned by the PRD/epics — define it. Recommended: repeated query params `folderId` (the attached Project Folder) and `fileId` (zero-or-more file references), e.g. `?folderId={id}&fileId={id}&fileId={id}&includeArchived={bool}`. Require ≥1 id; validate each with `IsCanonicalIdentifier` (malformed/missing → `SafeDenial` 404, mirroring 4.2's single `conversationId`); decide max-count, dedup, and deterministic input ordering. Avoid GET-with-body.
- **Folder-vs-file accumulation:** a project matched by both an attached folder **and** an attached file accumulates both reason codes (engine dedupes per reason code, sums distinct weights = 45 + 35 = 80 → `strong` band). One attachment matching two projects → `MultipleCandidates`. Multiple attachments matching one project → one candidate with multiple reason codes.

### The raw-file-content boundary (AC2, NFR-2) — non-negotiable

Raw file contents are **never** Project-owned data and must never cross the Folders ACL, appear on the wire, or hit logs. The existing file ACL deliberately uses the **metadata-only** `GetFolderFileMetadata` route and never the content-bearing `ReadFileRange` route; preserve that. 4.3 reads only safe **reference metadata** — opaque ids, safe display names, `ReferenceState`, reason codes. `ProjectFileReference` itself "never stores file contents, byte ranges, raw or workspace paths, diffs" (`ProjectFileReference.cs:19-22`). The Folders ACL is the **only** code permitted to reference `Hexalith.Folders.*` types (today: `FoldersProjectFolderDirectory.cs`, `FoldersProjectFileReferenceDirectory.cs`, and the DI alias). Resolution/mapper/projection code must never import Folders types. Note the Folders contract is **ahead of its wired external REST** — any ACL call goes through the typed client / in-topology Dapr, never an external REST endpoint, and fails closed if the method is unavailable.

### Tenant isolation & fail-closed (AC2, AC6)

- Tenant authority comes from authenticated claims + EventStore claim-transform **only** — never payload/header/query/attachment-id. `IProjectTenantContextAccessor` reads tenant from `eventstore:tenant` ?? `tenantId` and principal from `NameIdentifier` ?? `sub`.
- `ProjectAuthorizationGate` is the layered chain (JwtValidation → EventStoreClaimTransform rejecting client-controlled tenant overrides → TenantAccessFreshness via `TenantAccessAuthorizer` → ProjectAcl → validator/Dapr deny-by-default). Use `AuthorizeListAsync` (tenant-level read; `ReadProjectAction = "projects:read"`) — resolution is **not** project-scoped.
- Fail-closed: missing/unknown/disabled/stale/rebuilding/unavailable/forbidden/redacted evidence denies inclusion. Unauthorized vs nonexistent are indistinguishable at the boundary (**404**) — never a generic **500**. An authorized attachment referenced by **no** project is a legitimate **`200 NoMatch`** (it feeds Story 4.5 Propose New Project), not a 404 — and the same response a not-yet-referenced attachment would give, so no cross-tenant existence oracle is created. `ProjectQueryTenantFilter.Filter/FilterList` drops everything when the authoritative tenant is blank; apply Ordinal tenant equality before building any response.
- A reference-index / read-model outage (or Folders ACL outage, if a bounded re-check is used) is a fail-closed condition → `503 read_model_unavailable retryable:true`, never a leaked upstream error or a 500.

### Payload safety (NFR-2) & no persisted inference (NFR-9)

- Metadata-only everywhere: response, logs, audit, diagnostics. Log only ids / reason codes / correlation / freshness / status — never file contents, byte ranges, workspace ids, paths, transcripts, prompts, secrets, tokens, command bodies.
- The wire `ProjectResolution` has no `tenantId`; `PresentedInputIds`, `CorrelationId`, `TaskId` are never emitted. Prove with `NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(...)` (forbidden-content + path + JWT + PEM scans, plus a `tenantId`-property walk). Denylist source of truth: `PayloadClassification` (`docs/payload-taxonomy.md`).
- Compute-on-demand and discard: candidate scores and inferred matches are computed and thrown away. **No resolution trace is persisted** (persisted-trace history is an explicit Deferred Decision). Troubleshooting relies on structured metadata only.

### Performance (NFR-3) — multi-attachment fan-out is heavier than 4.2

p95 < 500 ms internal target for list/open/**resolve**/context. 4.3 can fan out across **both** folder and file references and across multiple attachments, so it is a heavier fan-out than the single-ACL conversation path — NFR-5 explicitly warns to favor "precomputed tenant-scoped projections over request-time fan-out." Prefer the cached reference-index `ReferenceState` (the recommended reverse-index path) over per-attachment live Folders ACL calls. Multi-ACL fan-out during resolution is a documented latency watch-item — mitigate with paging / short-TTL caching; a local Pattern-B projection is the documented escalation (deferred). If you take the enumerate-then-detail fallback, the N+1 detail fan-out is the watch-item to bound.

### Frozen vocabulary — consume, never extend

Do **not** add members to `ResolutionResult`, `ProjectReasonCode`, `ProjectLifecycle`, `ReferenceState`, or the `ProjectContextInclusionDiagnostic` string-const vocabulary — every member is pinned by the total-coverage tests in `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs` (`DescriptorLookupCoversEveryReasonCodeMember` etc. assert `lookup.Count == Enum.GetValues(...).Length`), and `ProjectVocabularyDescriptors.BuildDescriptors` throws at static init for any undescribed member. `ProjectFolderMatched` and `FileReferenceMatched` already exist (`ProjectReasonCode.cs:31,35`) with weights already declared — **no vocabulary change is needed for 4.3.** Confidence is the numeric `Score`/`Rank`, not a wire enum.

### Project Structure Notes

- New code lands in existing projects/namespaces — no new project, no boundary change:
  - Endpoint: `src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs` (partial of `ProjectsDomainServiceEndpoints`), route registered in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`.
  - Pure evidence mapper + input records: `src/Hexalith.Projects/Resolution/` (`AttachmentResolutionEvidenceMapper.cs`, `AttachmentResolutionMetadata.cs`, attachment candidate record) — Tier-1 pure, keep out of the engine file.
  - Reverse reference-index read model (recommended): `IProjectReferenceIndexReadModel` in `src/Hexalith.Projects.Server/` + Dapr impl in `src/Hexalith.Projects.Server/` (mirror `DaprProjectListReadModel.cs`) + in-memory impl (mirror `InMemoryProjectListReadModel.cs`); add a reverse query to `ProjectReferenceIndexProjection` / `IProjectProjectionStore` if needed.
  - Contract change: `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (new `ResolveProjectFromAttachments` operation only — `ProjectResolution` schema already present).
  - Regenerated client: `src/Hexalith.Projects.Client/Generated/*.g.cs` (build target output — do not hand-edit).
- **Detected variances / decision points (document the chosen approach in the Dev Agent Record):**
  1. **Reverse candidate enumeration** — no `(folderId|fileId) → projectIds` lookup and no Server read-model over `ProjectReferenceIndexProjection` exist today (the central design task; see "Candidate enumeration").
  2. **Architecture-vs-precedent structure** — `architecture.md` shows an aspirational `Contracts/Queries/` DTO + `Hexalith.Projects/Queries/` handler split, but the shipped Story 4.2 did **not** follow it: it put the endpoint in `Server/Queries/` (partial of `ProjectsDomainServiceEndpoints`), the pure mapper in `Hexalith.Projects/Resolution/`, and reused the `Contracts/Models/ProjectResolution` wire model. **Follow the shipped 4.2 precedent, not the aspirational doc**, for consistency.
  3. **File-identity match key** — reconcile `ProjectFileReference.FileReferenceId` ("Projects-owned opaque" in XML doc) with the ADR's owning-context-ULID rule and what the Chatbot actually presents (see open question).
- **MCP / CLI:** both are stubs and additive. They are **not** required by 4.3's resolution surface — add a read-only tool/verb (calling the regenerated typed client over HTTP, never Dapr/EventStore) only if you choose to expose an agent/operator entry point; otherwise defer to Epic 5.
- **Conventions:** xUnit + Shouldly with **deterministic builders + in-memory fakes** (`ProjectResolutionEvidenceBuilder`, `RecordingLogger`); the Projects test projects reference no NSubstitute and Stories 4.1/4.2 deliberately avoided it — do not add a mocking package. File-scoped namespaces; copyright header on every `.cs`; `I`-prefixed interfaces; `Async` suffix; `_camelCase` fields; sealed records/classes; route `.WithName` == spine `operationId` (PascalCase); name-based JSON enums; CRLF, UTF-8, final newline, no BOM; `git diff --check` clean. Build with `/home/administrator/.dotnet/dotnet` (10.0.300), not `/usr/bin/dotnet`. Do not weaken nullable/implicit-usings/warnings-as-errors. Root-level submodules only — no recursive submodule init.

### Previous Story Intelligence (Story 4.2 — `done`; Story 4.1 — `done`)

- **4.2 is the direct template** — clone its endpoint (`ResolveProjectFromConversationEndpoint.cs`), pure mapper (`ConversationResolutionEvidenceMapper.cs` + `ConversationResolutionMetadata.cs` + `ConversationResolutionProjectCandidate.cs`), and Tier-2 tests (`ResolveProjectFromConversationTests.cs`). 4.2's completion notes confirm the established pattern: pure host adapter around the 4.1 engine, candidate enumeration as host composition, single-resource ACL read, query-not-command discipline, fingerprint lockstep.
- **4.2's enumeration decision is the precedent for Task 1:** 4.2 enumerated candidates from `IProjectListReadModel.ListAsync` + `ProjectQueryTenantFilter.FilterList` and deliberately did **not** add a reverse conversation index. 4.3's matching is by-attachment-id, so it **does** need a reverse path over the folder/file reference index — document the chosen design exactly as 4.2 documented its choice.
- **4.2 review findings to pre-empt:** (a) the new ACL adapter shipped untested and review added 21 Tier-2 fail-closed cases — **author ACL/read-model tests up front** if you add a reverse read model; (b) keep endpoint XML doc + OpenAPI `description` **accurate** about the actual fail-closed behavior (a cross-tenant/unknown/degraded input that yields `200 NoMatch` is *not* a 404 — reserve 404 for unauthorized caller / unverifiable tenant / malformed-or-missing id); (c) `Idempotency-Key` rejection is `400 validation_error` (field `idempotency_key`), not `idempotency_error`.
- **4.1 delivered** the pure engine + scoring rules + `docs/resolution-scoring-heuristic.md` (which already lists folder=45 / file=35 and names 4.3) and registered the engine in DI; it made no OpenAPI/`.g.cs` change. 4.2 added the HTTP surface + `ProjectResolution` spine schema + client regeneration. **4.3 reuses that schema and adds only a new operation.** 4.1 noted the candidate set is host composition, never engine work — holds for 4.3.
- **4.1/4.2 validation blockers were code errors** (e.g. a `CS1739` named-argument mismatch), not environment issues — build with the pinned SDK; author CRLF/UTF-8/final-newline/no-BOM.

### Git Intelligence

- `08aa616 feat(story-4.2): Resolve project from conversation` (**baseline**) — the GET resolve-from-conversation endpoint, the pure conversation mapper trio, the single-conversation resolution ACL, the `ProjectResolution` spine operation + schema, and client regeneration. This is the exact pattern 4.3 mirrors; its 19-file diff is the blueprint for 4.3's file set.
- `517cc2b feat(story-4.1): Resolution engine (compute-on-demand)` / `723352b feat(tests): comprehensive tests for Story 4.1` — engine, scoring rules, contracts model, testing builder/leakage helper, determinism/leakage/scoring-matrix test patterns to mirror.
- Recent work confirms the established read-endpoint pattern (`GetProjectContext`, `RefreshProjectContext`, `ResolveProjectFromConversation`) and the conventional-commit + spine-fingerprint discipline 4.3 must follow.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3: Resolve Project From Attachments] (lines 837-851) — authoritative ACs (AC1 folder/file matching + reason codes; AC2 fail-closed on stale/missing auth + raw-content boundary).
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Project Resolution] (line 797) — "never silently attaching, never creating from inference without confirmation, archived excluded unless explicitly requested."
- [Source: _bmad-output/planning-artifacts/epics.md#FR Coverage Map] (line 211) — "FR-13 Resolve From Attachments: Epic 4 — resolution from Folder/File references via reference index + ACLs."
- [Source: _bmad-output/planning-artifacts/epics.md#Requirements Inventory] (lines 55, 80-87, 124) — FR-13; NFR-2/3/5/9; AR-18 single shared vocabulary.
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md] (FR-13 §4.3, UJ-2 file-first journey, NFR p95<500ms) — FR-13 testable consequences; "starts with files before choosing a project."
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns] — AR-10 compute-on-demand; persist only `ProjectResolutionConfirmed`; never silently attach; scoring heuristic owns ranking; archived excluded; metadata-only logging; claims-only tenant authority; fail-closed evidence rules.
- [Source: _bmad-output/planning-artifacts/architecture.md#Format & API Patterns] — reads = synchronous REST query carrying freshness; idempotency rejected on queries; correlation threading; RFC 9457 safe-denial 404; 503 retryable.
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication Patterns / Data boundaries] — ACL = Adapter + Translator + fail-closed Facade; only the ACL touches `Hexalith.Folders.*`; sibling file contents never cross into Projects.
- [Source: _bmad-output/planning-artifacts/architecture.md#NFR Performance] — p95<500ms; "resolution compute touches multiple ACLs" latency watch-item; mitigate paging/short-TTL caching; Pattern B escalation.
- [Source: docs/resolution-scoring-heuristic.md:5,22-32,48-56] — single source of truth; names Story 4.3; weights (folder 45, file 35), `MinimumQualifyingScore = 20`, once-per-reason-code, outcome thresholds, deterministic ordering.
- [Source: docs/payload-taxonomy.md] — payload-classification allowlist / denylist.
- [Source: docs/adr/identifier-boundary.md] — sibling references (folder/file) held as the owning context's ULID **string**; Projects mints only `ProjectId`; Ordinal/ULID id rules.
- [Source: docs/checklists/mutation-and-query-negative-tests.md] — canonical query negative-test rows every query endpoint must exercise.
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs:40-113] — `Resolve(...)` signature, tenant gate, archived gate, Included-only scoring, deterministic ordering, fail-closed warning.
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionContext.cs:22-49] — context inputs and `Empty(now)` factory.
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionCandidateEvidence.cs:21-73] — `ProjectResolutionCandidateEvidence` + `ProjectResolutionMatchSignal` (ReferenceKind doc: conversation/folder/file/memory/metadata).
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionScoringRules.cs:19-29] — `Weights` (ProjectFolderMatched=45, FileReferenceMatched=35), `MinimumQualifyingScore=20`.
- [Source: src/Hexalith.Projects/Resolution/ConversationResolutionEvidenceMapper.cs:40-113] — pure mapper template to clone (signal building, ReferenceState passthrough, skip zero-signal, never scores); docstring states folder/file codes "belong to Story 4.3".
- [Source: src/Hexalith.Projects/Resolution/ConversationResolutionMetadata.cs:33-69] + [ConversationResolutionProjectCandidate.cs:23-42] — metadata input record + `FailClosed` factory + candidate-row patterns to clone.
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs:16,22-165] — wire model (`ResolutionCandidate`, `ResolutionExclusion`); **no `TenantId`**; factories.
- [Source: src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs:31,35] — `ProjectFolderMatched` / `FileReferenceMatched` already defined.
- [Source: src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs] + [ProjectContextInclusionDiagnostic.cs] — only `Included` is a positive match; closed diagnostic vocabulary for exclusions (`ProjectFolderPending`, `ReferenceStale`, `ReferenceUnauthorized`, …).
- [Source: src/Hexalith.Projects.Contracts/Ui/ProjectVocabularyDescriptors.cs] + [tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs] — frozen-vocabulary total-coverage enforcement.
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectFolderReference.cs:20-25] — `ProjectFolderReference(FolderId?, DisplayName?, ReferenceState, ReasonCode?, ObservedAt)`; `FolderId` Folders-owned, null while pending.
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectFileReference.cs:19-37] — `ProjectFileReference(FileReferenceId, FolderId?, DisplayName?, ReferenceState, ReasonCode?, ObservedAt)`; "never stores file contents, byte ranges, raw or workspace paths, diffs."
- [Source: src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs:30-167] — reference-kind consts `folder`/`file`/`memory`, pending-folder key `_pending_project_folder`; forward-only `List(tenantId, projectId)` at line 153; no reverse lookup.
- [Source: src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexItem.cs:22-31] — row shape `(TenantId, ProjectId, ReferenceKind, ReferenceId?, ReferenceState, DisplayName?, ReasonCode?, UpdatedAt, Sequence)`.
- [Source: src/Hexalith.Projects.Infrastructure/IProjectProjectionStore.cs:16] — exposes only `ListAsync`/`GetDetailAsync`/`GetReadinessAsync` (reference index not read at runtime — the gap to close).
- [Source: src/Hexalith.Projects.Server/IProjectListReadModel.cs:21] + [IProjectDetailReadModel.cs:20] — `ListAsync(tenant, lifecycleFilter, ct)`; `GetAsync(tenant, projectId, ct)` (fallback enumeration source).
- [Source: src/Hexalith.Projects.Server/DaprProjectListReadModel.cs] + [InMemoryProjectListReadModel.cs] — Dapr/InMemory read-model pair to mirror for a new reverse reference-index read model.
- [Source: src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:58-168] — the host-adapter template 4.3 mirrors (authorize → enumerate → ACL → pure mapper → engine → JSON; persists nothing).
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:197-215] — route map + shared helpers (`SafeDenial`/`ValidationProblem`/`ReadModelUnavailable`/`ReadHeader`/`ReadQuery`/`IsCanonicalIdentifier`/`TryReadIncludeArchived`/`FreshnessHeaderName`/`EventuallyConsistent`/`ResponseJsonOptions`).
- [Source: src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:74-94] — DI `TryAddTransient` + `null → Unavailable*` fallback pattern for resolution/folder/file directories.
- [Source: src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs:23-46] + [FoldersProjectFolderDirectory.cs:36-151] — folder ACL (`RefreshFolderReferenceAsync` re-check by folderId; lifecycle + effective-permissions; stale/denied/archived/unavailable fail-closed mapping).
- [Source: src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs:17-64] + [FoldersProjectFileReferenceDirectory.cs:147-172] — file ACL; `RefreshFileReferenceAsync` **hard fail-closed to `Unavailable`** (no opaque-id file read route; never store workspaceId/filePath).
- [Source: src/Hexalith.Projects.Server/Folders/ProjectFolderValidationOutcomeMapper.cs:40-66] + [ProjectFileReferenceValidationOutcomeMapper.cs:40-65] — pure outcome→`ReferenceState` mappers to reuse only if a bounded re-check is added.
- [Source: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs] + [src/Hexalith.Projects.Server/IProjectTenantContextAccessor.cs] + [src/Hexalith.Projects.Server/TenantAccess/ProjectQueryTenantFilter.cs] — claims-only tenant authority; layered fail-closed gate; per-row tenant filter.
- [Source: src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs] + [src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs] — leakage denylist + `AssertProjectResolutionNoLeakage`.
- [Source: src/Hexalith.Projects.Testing/Resolution/ProjectResolutionEvidenceBuilder.cs:15-70] — deterministic Tier-1 builders (`Context`/`Candidate`/`Signal(reasonCode, state, referenceId, kind)`; `DefaultTenant = "acme"`, `DefaultNow = 2026-05-28T12:00:00Z`).
- [Source: src/Hexalith.Projects.Testing/TenantIsolation/ProjectTenantIsolationConformance.cs] + [src/Hexalith.Projects.Testing/Context/RecordingLogger.cs] — cross-tenant isolation + structured-log assertions.
- [Source: tests/Hexalith.Projects.Server.Tests/Queries/ResolveProjectFromConversationTests.cs:47] + [tests/Hexalith.Projects.Tests/Resolution/ConversationResolutionEvidenceMapperTests.cs] — Tier-2 endpoint + Tier-1 mapper test templates to clone.
- [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml] — spine (add `ResolveProjectFromAttachments`; the `ResolveProjectFromConversation` block + existing `ProjectResolution` schema are the template/reuse).
- [Source: src/Hexalith.Projects.Client/Generation/Program.cs] — client generator + `HexalithProjectsGeneratedArtifacts.VerifyCurrent` fingerprint gate.
- [Source: _bmad-output/project-context.md] — workspace-wide rules (Dapr-only infra, persist-then-publish, tenant isolation, frozen vocabulary, central package management, do-not-hand-edit `.g.cs`).

### Open questions (resolve during dev / confirm with caller contract)

1. **File-attachment match key (blocking the file path; folder path is unaffected).** `ProjectFileReference.FileReferenceId` is documented "Projects-owned opaque", but ADR `identifier-boundary.md` holds sibling references as the owning context's Folders ULID. Confirm what opaque id `Hexalith.Chatbot` presents for an attached file and that it equals the file-kind `ReferenceId` stored in the reference index (set at `LinkFileReference`, Story 2.5). If they diverge, adjust the match key or the stored file identity. The **folder** match (`folderId` → `ProjectFolderReference.FolderId`) is unambiguous and always correct.
2. **Endpoint input shape.** Confirm the attachment request contract (repeated `folderId` / `fileId` query params vs a single id list), max count, dedup, and whether a single call may carry both a folder and files. Recommendation: repeated query params, ≥1 required, canonical-id validated.
3. **Live folder re-check (optional).** Confirm whether AC2 "stale authorization" must be re-verified live for folders (bounded `RefreshFolderReferenceAsync` per attached folder) or whether the cached index `ReferenceState` is sufficient (recommended for NFR-5). File re-check is infeasible regardless (RefreshFileReferenceAsync HALT).

## Dev Agent Record

### Agent Model Used

GPT-5 Codex via `bmad-dev-story`.

### Debug Log References

- 2026-05-30: Selected the recommended reverse reference-index enumeration path. Added `ProjectReferenceIndexProjection.ListByReference(...)`, `IProjectProjectionStore.ListReferencesByReferenceAsync(...)`, and `IProjectReferenceIndexReadModel` with Dapr/InMemory implementations.
- 2026-05-30: File attachment identity uses the same opaque id stored in `ProjectReferenceIndexItem.ReferenceId` for `ReferenceKind == "file"` (`ProjectFileReference.FileReferenceId` / Story 2.5 link identity). Equality is Ordinal and case-sensitive for folder and file ids.
- 2026-05-30: Endpoint input finalized as repeated query parameters: `folderId` and `fileId`, at least one required, max 32 canonicalized unique ids, deterministic Ordinal ordering. Missing/malformed ids return safe-denial 404; too many ids return `400 validation_error` on `attachments`.
- 2026-05-30: `dotnet msbuild /t:GenerateHexalithProjectsClient` was blocked because `$(NSwagExe_Net100)` expanded empty in this shell (`run: not found`). Regenerated with the same NSwag package directly via `/home/administrator/.dotnet/dotnet /home/administrator/.nuget/packages/nswag.msbuild/14.7.1/tools/Net100/dotnet-nswag.dll run nswag.json`, then ran the checked-in helper generator. No generated files were hand-edited.
- 2026-05-30: `dotnet test Hexalith.Projects.slnx --no-build` is blocked in this sandbox by VSTest socket creation (`System.Net.Sockets.SocketException (13): Permission denied`). Used the xUnit v3 in-process runner for executable test assemblies instead.

### Completion Notes List

- Implemented `GET /api/v1/projects/resolution/from-attachments` as a synchronous read query that rejects `Idempotency-Key`, validates freshness/includeArchived, threads correlation/task metadata, persists nothing, and returns the existing `ProjectResolution` wire model.
- Added a pure attachment evidence mapper and metadata-only input records. The mapper emits only `ProjectFolderMatched` / `FileReferenceMatched`, carries cached `ReferenceState` onto every signal, skips zero-signal candidates, and leaves all scoring/ranking/outcome decisions to `ProjectResolutionEngine`.
- Added a reverse reference-index read surface and runtime/pre-runtime implementations. The chosen enumeration source is the NFR-5-aligned reference-index reverse lookup, not the O(projects) detail fan-out fallback.
- Added OpenAPI operation `ResolveProjectFromAttachments`, regenerated the typed client/helpers, and added client-generation coverage proving the query has no idempotency helper/parameter and the fingerprint gate remains current.
- Added Tier-1 mapper coverage, Tier-2 endpoint coverage via in-process endpoint invocation (no Kestrel port bind), cross-tenant isolation and no-payload-leakage assertions.
- Validation passed: full solution build with pinned SDK 10.0.300 (`/home/administrator/.dotnet/dotnet restore Hexalith.Projects.slnx /p:NuGetAudit=false /p:RestoreFallbackFolders= /m:1`, then `/home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx --no-restore -warnaserror /m:1 /p:UseSharedCompilation=false`); `git diff --check`; xUnit v3 in-process runner: Projects.Tests 539/539, Contracts.Tests 135/135, Client.Tests 47/47, Integration.Tests 14/14; targeted Server.Tests `ResolveProjectFromAttachmentsTests` 14/14 (11 `[Fact]` + 1 `[Theory]`×3).
- `dotnet test Hexalith.Projects.slnx --no-build` could not complete in the dev sandbox because VSTest cannot open local sockets; this is an environment restriction, not a test failure under the in-process runner.
- Review addendum (2026-05-30): the literal `dotnet test Hexalith.Projects.slnx --no-build` lane DID run in the review environment (no socket restriction) and is fully green — see the Senior Developer Review (AI) section for post-fix totals.

### File List

- _bmad-output/implementation-artifacts/4-3-resolve-project-from-attachments.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.Projects/Projections/ProjectList/ProjectListProjection.cs (review fix — tolerate File/Memory reference events so ListAsync rebuild no longer throws)
- src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs
- src/Hexalith.Projects/Resolution/AttachmentResolutionEvidenceMapper.cs
- src/Hexalith.Projects/Resolution/AttachmentResolutionMetadata.cs
- src/Hexalith.Projects/Resolution/AttachmentResolutionProjectCandidate.cs
- src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs
- src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs
- src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml
- src/Hexalith.Projects.Infrastructure/DaprProjectProjectionStore.cs
- src/Hexalith.Projects.Infrastructure/IProjectProjectionStore.cs
- src/Hexalith.Projects.Server/DaprProjectReferenceIndexReadModel.cs
- src/Hexalith.Projects.Server/IProjectReferenceIndexReadModel.cs
- src/Hexalith.Projects.Server/InMemoryProjectReferenceIndexReadModel.cs
- src/Hexalith.Projects.Server/ProjectReferenceIndexCandidateRow.cs
- src/Hexalith.Projects.Server/ProjectReferenceIndexReadModelMapper.cs
- src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs
- src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs
- src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs
- tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs
- tests/Hexalith.Projects.Tests/Projections/ProjectReferenceIndexProjectionTests.cs
- tests/Hexalith.Projects.Tests/Projections/ProjectProjectionTests.cs (review fix — list-projection regression: file/memory events no longer throw)
- tests/Hexalith.Projects.Server.Tests/Queries/ResolveProjectFromAttachmentsTests.cs
- tests/Hexalith.Projects.Server.Tests/InMemoryProjectReferenceIndexReadModelTests.cs (review fix — real reverse read-model/mapper/projection coverage)
- tests/Hexalith.Projects.Tests/Resolution/AttachmentResolutionEvidenceMapperTests.cs
- tests/e2e/specs/projects-resolution.spec.ts (spine-backed attachment-resolution e2e scaffolds, `test.fixme` pending AppHost fixtures)
- tests/e2e/support/helpers/projects-api-client.ts (e2e `resolveProjectFromAttachments` helper)

### Change Log

- 2026-05-30: Implemented Story 4.3 resolve-from-attachments query, reverse reference-index enumeration, pure mapper/input records, OpenAPI/client regeneration, and tests.
- 2026-05-30: Senior Developer Review (AI) — auto-fix pass. Fixed a CRITICAL latent defect (`ProjectListProjection` threw on `FileReferenceLinked`/`FileReferenceUnlinked`/`MemoryLinked`/`MemoryUnlinked`, which would 503 the attachment-resolution reverse read model — and the existing list endpoint — for any tenant that had linked a file or memory). Added real reverse-read-model coverage, eight Tier-2 fail-closed/degraded/task-threading/combined-cap endpoint tests, made the cross-tenant test exercise the endpoint's own re-filter, documented the mapper's defense-in-depth re-check, and completed the File List. Full `dotnet test Hexalith.Projects.slnx` lane verified green.

## Senior Developer Review (AI)

**Reviewer:** Jerome (adversarial AI review, auto-fix mode) · **Date:** 2026-05-30 · **Outcome:** Approved — all blocking issues fixed in place.

### Method

Adversarial multi-agent review across 7 dimensions (AC1/AC4 enumeration+mapper, AC2/AC6/AC7 security/tenant/leakage, AC3/AC5 query discipline, AC8 spine/client lockstep, task audit, test quality, read-model code quality) with each finding independently verified against the real code (23 raw → 17 confirmed, 6 rejected). Additionally validated by **running the code**: the full `dotnet build … -warnaserror` + `dotnet test Hexalith.Projects.slnx --no-build` lane (the lane the dev sandbox could not run) and a targeted empirical probe of the reverse-read-model rebuild path.

### Findings & resolution

- **🔴 CRITICAL (caught by probe, missed by static review) — `ProjectListProjection` desynced from the aggregate.** The list projection threw on `FileReferenceLinked`/`FileReferenceUnlinked`/`MemoryLinked`/`MemoryUnlinked` (it only knew Created/SetupUpdated/Archived/Folder*). Because `DaprProjectReferenceIndexReadModel.ListByReferenceAsync` calls `IProjectProjectionStore.ListAsync` → `ProjectListProjection.Rebuild(…)` over the **full per-tenant journal**, any tenant that had ever linked a file or memory would make 4.3's attachment-resolution endpoint (the story's core scenario) — and the existing `GET /api/v1/projects` list endpoint — throw → **503**. The aggregate (`ProjectStateApply`) and `ProjectDetailProjection` already handle these events; the list projection now does too (sequence/UpdatedAt touch). **Fixed** in `ProjectListProjection.cs` + regression test `ProjectProjectionTests.ListProjection_ToleratesFileAndMemoryReferenceEvents`.
- **🟠 HIGH — new reverse read-model stack shipped with no direct tests** (the exact Story-4.2 lesson the story told dev to pre-empt). The endpoint tests drove a hand-written stub, so the real `InMemoryProjectReferenceIndexReadModel` → `ProjectReferenceIndexReadModelMapper` (incl. `ProjectQueryTenantFilter.FilterList`) → projection chain was never exercised end-to-end. **Fixed** — new `InMemoryProjectReferenceIndexReadModelTests` (6 tests): folder/file match, archived-lifecycle passthrough, cross-tenant drop, orphan-reference (ContainsKey join) drop, deterministic ordering.
- **🟠 HIGH / 🟡 MEDIUM — missing Tier-2 fail-closed endpoint coverage** (AC6/AC2). Added: unauthorized-caller→404, missing-`AuthoritativeTenantId`→404 (+no leak), `Idempotency-Key`-while-unauthorized→404 (authorize-before-idempotency ordering), invalid `includeArchived`→400, strengthened freshness rejection to assert field, degraded `ReferenceState` (Stale/Unauthorized/Unavailable)→`200 NoMatch` with exclusion on the wire, `X-Hexalith-Task-Id` threaded into the ProblemDetails body + malformed-task-id dropped, and the combined 32-id cap boundary.
- **🟡 MEDIUM — cross-tenant test validated the stub, not production.** The endpoint stub no longer filters by tenant, so `Resolve_CrossTenantReference_YieldsNoCandidate` now genuinely exercises the endpoint's own Ordinal re-filter; the real projection/mapper tenant filter is proven by the new read-model tests.
- **🟢 LOW — documentation.** File List completed (the two `tests/e2e/*` files, the new read-model test, the modified list-projection + projection-test files); corrected Server.Tests count `13/13`→`14/14`; recorded the now-green full test lane; added a defense-in-depth comment on the mapper's presented-id re-check.

### Rejected / not actioned (with reason)

- **Validation-before-authorization (too-many-ids 400 / malformed-id 404 emitted before `AuthorizeListAsync`)** — not a real oracle: the count/shape checks reveal only the caller's own request shape (not tenant data or resource existence), and the story’s own Task 5 (“validate query → authorize”) + Debug Log sanction it. Two independent verifiers rejected it; left as-is.
- **`DaprProjectReferenceIndexReadModel` two-read TOCTOU / unbounded reverse scan** — the two reads are one tenant-scoped journal rebuilt twice (consistent), and the in-memory filter mirrors the existing list/detail reads' NFR profile; bounded by the 32-attachment cap. Documented NFR-5 watch-item, not a defect.
- **`git diff --check` on the two e2e `.ts` files** — flagged CR-at-EOL only; the files are CRLF **consistent with their e2e siblings** (`correlation.ts`), there is no repo `.gitattributes`/`core.whitespace=cr-at-eol`, so this is a repo-wide artifact for CRLF `.ts` files, not a 4.3 defect. The substantive C# implementation is `git diff --check` clean. (The dev's "git diff --check clean" claim was imprecise for the e2e files.)
- **Reused `ResolutionCandidate.reasonCodes` schema description mentioning conversation codes** — pre-existing shared-schema description from 4.2; no spine change made here, so no client regeneration / fingerprint churn was introduced.

### Validation evidence (post-fix)

- Build: `dotnet build Hexalith.Projects.slnx -warnaserror` → **0 warnings, 0 errors** (pinned SDK 10.0.300).
- Full lane: `dotnet test Hexalith.Projects.slnx --no-build` → **all green** — Projects.Tests 540, Server.Tests 413, Contracts.Tests 135, Client.Tests 47, Integration.Tests 14 (**1149 total, 0 failed**). The spine artifacts-fingerprint gate passes (no spine/client change in the review).
- `git diff --check` clean for all `*.cs`. No `.g.cs` hand-edits.
