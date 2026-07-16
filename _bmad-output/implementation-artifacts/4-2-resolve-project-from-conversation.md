---
baseline_commit: 517cc2b51558fc380b6c33073602d1b17c6ba4c3
---

# Story 4.2: Resolve Project From Conversation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to ask Projects to resolve candidate Projects for a conversation that has no explicit Project**,
so that **I can infer or request the correct project context from conversation metadata** _(FR-12; realizes UJ-3)_.

## Acceptance Criteria

These two are the authoritative epic ACs (BDD, verbatim). ACs 3–8 are the engineering acceptance criteria derived from the architecture and the Story 4.1 contract; they make "done" checkable and prevent false completion.

**AC1 — Resolution outcome (FR-12).**
**Given** a conversation with no explicit Project
**When** `ResolveProjectFromConversation` runs the engine (4.1) over conversation metadata
**Then** it returns `NoMatch` / `SingleCandidate` / `MultipleCandidates` with reason code(s), and excludes archived Projects unless explicitly requested.

**AC2 — Authorization scope & fail-closed (NFR-2, NFR-9).**
**Given** authorization scope
**When** resolution runs
**Then** it does not access unauthorized conversations, folders, files, memories, or projects, and fails closed on unverifiable tenant evidence.

**AC3 — Synchronous read query, persists nothing (AR-10).**
**Given** the compute-on-demand rule
**When** `ResolveProjectFromConversation` executes
**Then** it is a **synchronous REST GET query** that writes **no** event, projection, state, or resolution trace (only the later `ConfirmProjectResolution` command in Story 4.4 persists), and it returns the existing `ProjectResolution` wire model unchanged (no new model, no new vocabulary member).

**AC4 — Reuse the pure engine, do not duplicate scoring.**
**Given** the Story 4.1 engine is the single source of truth for scoring/ranking/outcome
**When** candidates are evaluated
**Then** the handler pre-fetches and ACL-checks candidate evidence, maps it to `ProjectResolutionCandidateEvidence` + `ProjectResolutionMatchSignal`, and delegates to `ProjectResolutionEngine.Resolve(context, candidates)` — it does **not** re-implement the qualifying/scoring/ranking/threshold heuristic, and conversation matches use only the `ConversationLinked` and `MetadataMatched` reason codes.

**AC5 — Query contract conventions.**
**Given** the query is a read endpoint
**When** the request is processed
**Then** an `Idempotency-Key` header is **rejected** (`400 validation_error`, field `idempotency_key`), `X-Correlation-Id` / `X-Hexalith-Task-Id` are threaded, the requested freshness (if present) must be `eventually_consistent` (else `400`), and the response sets `X-Hexalith-Freshness: eventually_consistent`.

**AC6 — Safe-denial & RFC 9457.**
**Given** an unauthorized caller, a cross-tenant/unknown conversation, or unverifiable tenant evidence
**When** resolution runs
**Then** the boundary returns an RFC 9457 ProblemDetails `404` safe-denial (unauthorized and nonexistent indistinguishable) — never a `500`, never an empty leak — using the existing `SafeDenial` / `ValidationProblem` / `ReadModelUnavailable` helpers; read-model unavailability returns `503 read_model_unavailable retryable:true`.

**AC7 — No payload leakage (NFR-2).**
**Given** the metadata-only boundary
**When** the response and any logs are produced
**Then** no transcript text, prompt fragments, file contents, memory bodies, secrets, raw tokens, command bodies, or filesystem paths appear, the serialized `ProjectResolution` carries **no `tenantId`**, and this is proven by `NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(...)` plus a cross-tenant isolation negative test.

**AC8 — Contract spine + generated client stay in lockstep.**
**Given** the OpenAPI 3.1 Contract Spine is the single source of truth
**When** the `ResolveProjectFromConversation` operation is added to `hexalith.projects.v1.yaml`
**Then** the typed client is **regenerated** (never hand-edited) and the artifacts-fingerprint verification test passes (spine SHA-256 == generated client), and `dotnet build … -warnaserror` and the full `dotnet test` lane stay green.

## Tasks / Subtasks

- [x] **Task 1 — Candidate enumeration for a project-less conversation (AC1, AC4).** Close the reverse-lookup gap that Story 4.1 explicitly handed forward. `ProjectReferenceIndexProjection` exposes only the forward `List(tenantId, projectId)` lookup (`ProjectReferenceIndexProjection.cs:153`) — there is no conversation→projects path. (AC: 1, 4)
  - [x] Decide and document the enumeration source. **Recommended (matches the Story 3.2 split and the perf guidance):** enumerate the tenant's authorized projects via `IProjectListReadModel.ListAsync`, then derive per-project conversation-match signals; treat a reverse index entry on `ProjectReferenceIndexProjection` as an optional precompute optimization if profiling threatens the p95 budget. Do **not** scan with request-time multi-ACL fan-out beyond what the budget allows (NFR-3 watch-item).
  - [x] For each candidate project, build `ProjectResolutionMatchSignal`s: `ConversationLinked` (weight 50) when the Projects reference index records this conversation against the project; `MetadataMatched` (weight 20) for a metadata-derived heuristic match. Set each signal's `ReferenceState` from the ACL/freshness/authorization recheck — only `ReferenceState.Included` contributes to score; everything else surfaces as a `ResolutionExclusion`.
  - [x] Read conversation metadata **only** through a Projects-owned Pattern-A ACL (see Task 2). Never import `Hexalith.Conversations.*` into resolution/projection/aggregate code.
- [x] **Task 2 — Authorized, tenant-scoped conversation metadata read (AC2, AC6, AC7).** (AC: 2, 6, 7)
  - [x] Resolve the single conversation's safe metadata through the conversation ACL. `IProjectConversationDirectory.ListForProjectAsync` is project-keyed (forward); for a project-less conversation you need a single-conversation metadata read. **A fail-closed single-conversation read already exists — reuse it:** `ConversationsProjectConversationAssignmentDirectory.ReadCurrentProjectAsync` (`ConversationsProjectConversationAssignmentDirectory.cs:217-264`) builds a `GetConversationQuery`, calls `IConversationClient.GetConversationAsync`, does the per-row scope re-check (`TenantId`/`ConversationId` Ordinal equality, line 249), and is fully fail-closed (`MapFailure`; exception → `Unavailable`). Extract/reuse this pattern (promote it to a read-directory method if needed) rather than inventing a new wrapper, keeping the sibling-client reference inside the ACL only.
  - [x] Pass tenant + `CallerPrincipalId` through to the verified server query path — a JWT claim alone is not the authority. Map sibling trust signals to the shared `ReferenceState` via the existing translator mappings.
- [x] **Task 3 — Tenant authority + fail-closed wiring (AC2, AC6).** (AC: 2, 6)
  - [x] Derive `AuthoritativeTenantId` and principal **only** from authenticated claims via `IProjectTenantContextAccessor` — never from the conversation id, request body, header, or query parameter.
  - [x] Authorize before doing any work via `ProjectAuthorizationGate` (use the read/list authorization path; tenant-level for a non-project-scoped resolve). On denial: `authorization.Retryable && Reason == Unavailable ? ReadModelUnavailable(...) : SafeDenial(...)`.
  - [x] Build `ProjectResolutionContext(AuthoritativeTenantId, RequestedTenantId, IncludeArchived, Now, CorrelationId, TaskId)`. Pass `Now = timeProvider.GetUtcNow()` (the engine reads no clock). The engine re-asserts tenant authority and emits all candidates as `TenantMismatch` exclusions → `NoMatch` when unverifiable; preserve that contract end-to-end.
- [x] **Task 4 — Resolve query endpoint (AC1, AC3, AC4, AC5, AC6).** (AC: 1, 3, 4, 5, 6)
  - [x] Create `src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs` as `public static partial class ProjectsDomainServiceEndpoints` with `private static async Task<IResult> ResolveProjectFromConversationAsync(...)`. Use `RefreshProjectContextEndpoint.cs` as the structural template (authorize → fetch ACL evidence → run pure policy → `Results.Json`).
  - [x] Register the route in `ProjectsDomainServiceEndpoints.MapProjectsDomainServiceEndpoints` mirroring the `/context` GET. Recommended shape: `GET /api/v1/projects/resolution/from-conversation?conversationId={id}&includeArchived={bool}` with `.WithName("ResolveProjectFromConversation")`; finalize the exact path in the spine. Validate `conversationId` with `IsCanonicalIdentifier` (malformed → `SafeDenial`).
  - [x] Read + canonicalize `X-Correlation-Id` / `X-Hexalith-Task-Id`; reject `Idempotency-Key` (`ValidationProblem(..., "idempotency_key")`); validate freshness (`eventually_consistent` only); set `X-Hexalith-Freshness` on the response; return `Results.Json(resolution, ResponseJsonOptions)`.
  - [x] Resolve `ProjectResolutionEngine` from DI (already registered: `TryAddTransient<ProjectResolutionEngine>()`), call `engine.Resolve(context, candidates)`, return the `ProjectResolution`.
- [x] **Task 5 — OpenAPI spine + client regeneration (AC8).** (AC: 8)
  - [x] Add the `ResolveProjectFromConversation` path + `operationId` to `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`, mirroring the `GetProjectContext` block (read-consistency `eventually_consistent`, correlation, authorization, canonical-error-categories, `X-Correlation-Id` + `X-Hexalith-Freshness` response headers, 400/401/403/404/503 `$ref` responses). Add a `ProjectResolution` component schema (with `ResolutionCandidate` / `ResolutionExclusion`) — **no `tenantId` property**.
  - [x] Regenerate the client (`dotnet msbuild /t:GenerateHexalithProjectsClient`); never hand-edit `Client/Generated/*.g.cs`. A GET produces a typed client method but **no** idempotency helper.
  - [x] Re-run the artifacts-fingerprint verification test (`HexalithProjectsGeneratedArtifacts.VerifyCurrent`) — it fails if the spine changed without regeneration.
- [x] **Task 6 — Tests (AC1–AC8).** (AC: 1, 2, 3, 4, 5, 6, 7, 8)
  - [x] Tier-1: resolution-mapping tests for the conversation adapter using `ProjectResolutionEvidenceBuilder` — `NoMatch` (no qualifying signal), `SingleCandidate` (one `ConversationLinked`), `MultipleCandidates` (≥2 qualifying), archived-excluded-by-default vs `includeArchived=true`, metadata-only (`MetadataMatched`) match, and unauthorized/stale/unavailable signal → exclusion (not a match).
  - [x] Tier-1: tenant fail-closed — missing/blank `AuthoritativeTenantId` and `RequestedTenantId != AuthoritativeTenantId` → every candidate `TenantMismatch` → `NoMatch`; assert the structured warning via `RecordingLogger`.
  - [x] Tier-1: leakage — `NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(result)`; serialized output `ShouldNotContain("tenantId")` and the tenant value.
  - [x] Tier-2 (Server.Tests): endpoint tests with in-memory directory/read-model fakes and the `CapturingConversationClient` pattern — idempotency-key rejected, freshness validation, safe-denial 404 for unauthorized/cross-tenant/unknown conversation, 503 on read-model unavailable, `X-Hexalith-Freshness` set, correlation threading.
  - [x] Cross-tenant isolation negative test via `ProjectTenantIsolationConformance` (conversation in tenant B never yields a tenant-A candidate).
  - [x] Build with the pinned SDK and verify the fingerprint gate: `/home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx -warnaserror` then `dotnet test Hexalith.Projects.slnx --no-build`.

## Dev Notes

### The one thing to get right

Story 4.1 shipped the resolution engine as a **pure, synchronous, side-effect-free function** that "never reads from sibling bounded contexts, never calls infrastructure, never writes events/projections/stores, never reads wall-clock time, and never persists a trace" (`ProjectResolutionEngine.cs:18-23`). **Story 4.2 is the impure host adapter around it.** The split is identical to Story 3.2 (`GetProjectContext`), where the endpoint fetches evidence and hands it to a pure policy. Concretely, 4.2 must:

1. Authorize at the API edge (claims-only tenant, fail-closed).
2. **Enumerate candidate projects for a conversation that has no explicit project** (the reverse direction the current projection does not support).
3. Read conversation metadata through the Pattern-A ACL only.
4. Map evidence → `ProjectResolutionCandidateEvidence` / `ProjectResolutionMatchSignal`.
5. Build `ProjectResolutionContext` and call `engine.Resolve(...)`.
6. Return `ProjectResolution` (no `tenantId`), persisting nothing.

Do **not** re-implement scoring/ranking/threshold logic — the engine and `docs/resolution-scoring-heuristic.md` are the single source of truth, and that doc names 4.2 explicitly: "Stories 4.2 (`Resolve From Conversation`) and 4.3 (`Resolve From Attachments`) pre-fetch evidence and call this engine. They do not duplicate these scoring, ranking, or outcome rules."

### `ResolveProjectFromConversation` is a QUERY, not a command

The architecture puts `ResolveProject` under `Queries/` and defines a firm two-lane split: reads are synchronous REST query endpoints; only mutations are command-async (202). Resolution writes nothing; the **only** resolution event, `ProjectResolutionConfirmed`, is emitted later by the separate `ConfirmProjectResolution` command (Story 4.4). Therefore 4.2:
- is a **GET** query endpoint (compute-on-demand), **not** an `IProjectCommand` + `IProjectCommandSubmitter` flow;
- **rejects** `Idempotency-Key` (idempotency-key is required on mutations, rejected on queries);
- carries freshness/trust state on the response;
- returns no `202`, writes no event/projection/state/trace.

Reference (do **not** use for 4.2; this is the 4.4 path): `IProjectCommand` / `IProjectCommandSubmitter` / `EventStoreProjectCommandSubmitter` are the mutation pipeline.

### Engine contract 4.2 must satisfy (from Story 4.1 code — exact)

- **Entry point (sync):** `ProjectResolutionEngine.Resolve(ProjectResolutionContext context, IReadOnlyList<ProjectResolutionCandidateEvidence> candidates) → ProjectResolution`. Both args null-guarded. DI: `services.TryAddTransient<ProjectResolutionEngine>()` already registered (`ProjectsServiceCollectionExtensions.cs:37`).
- **Context inputs (4.2 populates):** `ProjectResolutionContext(string? AuthoritativeTenantId, string? RequestedTenantId, bool IncludeArchived, DateTimeOffset Now, string? CorrelationId = null, string? TaskId = null, IReadOnlyList<string>? PresentedInputIds = null)`. `Now` is the engine's only clock source (echoed to `ProjectResolution.ObservedAt`). `CorrelationId`/`TaskId`/`PresentedInputIds` are logging/metadata-only and **never** emitted to the wire.
- **Candidate evidence (4.2 builds):** `ProjectResolutionCandidateEvidence(string ProjectId, string? DisplayName, ProjectLifecycle Lifecycle, IReadOnlyList<ProjectResolutionMatchSignal> Signals)`; `ProjectResolutionMatchSignal(string ReferenceKind, string ReferenceId, ProjectReasonCode ReasonCode, ReferenceState ReferenceState, DateTimeOffset ObservedAt)`. `ReferenceKind` is informational ("conversation"/"metadata"); only `ReferenceState.Included` signals contribute to score, every other state becomes a `ResolutionExclusion`.
- **Output (4.2 returns unchanged):** `ProjectResolution(ResolutionResult Result, IReadOnlyList<ResolutionCandidate> Candidates, IReadOnlyList<ResolutionExclusion> Excluded, DateTimeOffset ObservedAt)` with factories `NoMatch/SingleCandidate/MultipleCandidates`. **Deliberately no `TenantId` field.**
- **Scoring (engine-owned, do not duplicate):** per-reason-code weights `ConversationLinked=50, ProjectFolderMatched=45, FileReferenceMatched=35, MemoryMatched=30, MetadataMatched=20`; each reason code counts at most once; `MinimumQualifyingScore = 20`; outcome decided solely by qualifying-candidate count (0→NoMatch, 1→SingleCandidate, 2+→MultipleCandidates); deterministic order = score desc, then `ProjectId` Ordinal asc. **For conversation resolution only `ConversationLinked` and `MetadataMatched` are in play** — folder/file/memory reason codes belong to Story 4.3.
- **Tenant fail-closed (engine-enforced):** `AuthoritativeTenantId` blank, or `RequestedTenantId` present and `!= AuthoritativeTenantId` ⇒ every candidate excluded `TenantMismatch` / `tenantMismatch` ⇒ `NoMatch`, plus one structured warning. 4.2 must feed a server-derived `AuthoritativeTenantId` so this gate behaves correctly.
- **Archived exclusion:** `Lifecycle == ProjectLifecycle.Archived && !IncludeArchived` ⇒ excluded (`Archived` / `projectArchived`). "Explicitly requested" = `IncludeArchived: true` (drive it from the `includeArchived` query param, default `false`).

### Authorized conversation read — Pattern A ACL only

- Conversation membership is **derived, not stored** (Pattern A): query Conversations via the Projects ACL, no local conversation storage. The ACL is the **only** code allowed to reference `Hexalith.Conversations.*` types (Adapter + Translator + fail-closed Facade).
- Read surface today: `IProjectConversationDirectory.ListForProjectAsync(ProjectId, ConversationTenantId, CallerPrincipalId, PageRequest, ct) → ProjectConversationsPage` (`ConversationsProjectConversationDirectory` wraps `IConversationClient.ListConversationsAsync`). It passes tenant + caller principal to the verified server query path; failures fail closed to `ProjectConversationsPage.Empty(projectId, signal)` (`Unauthorized|Forbidden|NotFound → Forbidden`; else `Unavailable`; exception → `Unavailable`). The translator re-checks per-row scope (`TenantId` + `ProjectId` Ordinal) and collapses the whole page to `Empty` if any row escapes scope.
- This is **project-keyed (forward)**. A project-less conversation needs a **single-conversation** metadata read, which the codebase already provides: `ConversationsProjectConversationAssignmentDirectory.ReadCurrentProjectAsync` (`ConversationsProjectConversationAssignmentDirectory.cs:217-264`) wraps `IConversationClient.GetConversationAsync` with the per-row scope re-check and fail-closed mapping. Reuse/extract that pattern; never call the sibling client from resolution code.
- Safe metadata available (reference-only): opaque ids, `LifecycleStatus`, safe display label, trust signal, hydration `SafeLabel`/`SafeStatus`. **Forbidden** (never crosses the ACL or appears on the wire/logs): transcript/message text, prompt fragments, conversation state objects.
- DI fail-closed default: with no `IConversationClient` registered, `IProjectConversationDirectory` resolves to `UnavailableProjectConversationDirectory` (always `Unavailable`). Tests should exercise this.

### Tenant isolation & fail-closed (AC2)

- Tenant authority comes from authenticated claims + EventStore claim-transform **only** — never payload/header/query. `IProjectTenantContextAccessor` reads tenant from `eventstore:tenant` ?? `tenantId` and principal from `NameIdentifier` ?? `sub`.
- `ProjectAuthorizationGate` is the layered chain (JwtValidation → EventStoreClaimTransform rejecting client-controlled tenant overrides → TenantAccessFreshness via `TenantAccessAuthorizer` → ProjectAcl via `IProjectDetailReadModel` + `ProjectQueryTenantFilter` → validator/Dapr deny-by-default). `ReadProjectAction = "projects:read"`.
- Fail-closed means: missing/unknown/disabled/stale/rebuilding/unavailable/forbidden/redacted evidence denies the operation and inclusion. Unauthorized vs nonexistent are indistinguishable at the boundary (**404**). A fail-closed denial is a safe reason code / 404 — **never a generic 500** (explicit anti-pattern).
- `ProjectQueryTenantFilter.Filter/FilterList` drops everything when the authoritative tenant is blank; apply Ordinal tenant equality before building any response.

### Payload safety (NFR-2) & no persisted inference (NFR-9)

- Metadata-only everywhere: events, logs, audit, diagnostics, all surfaces. Log only ids/reason codes/correlation/freshness/status — never transcripts, file contents, memory bodies, prompts, secrets, tokens, command bodies, or unrestricted paths.
- The wire `ProjectResolution` has no `tenantId`; `PresentedInputIds`, `CorrelationId`, `TaskId` are never emitted. Prove with `NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(...)` (forbidden-content + path + JWT + PEM scans, plus a tenantId-property walk). Denylist source of truth: `PayloadClassification` (`docs/payload-taxonomy.md`).
- Compute-on-demand and discard: candidate scores and inferred matches are computed and thrown away. **No resolution trace is persisted** (persisted-trace history is an explicit Deferred Decision). Troubleshooting relies on structured metadata only.

### Performance (NFR-3)

- p95 < 500 ms internal target for list/open/**resolve**/context. Favor reading the precomputed tenant-scoped projection over request-time fan-out. Multi-ACL fan-out during resolution is a documented latency watch-item — mitigate with paging / short-TTL caching; a local Pattern-B projection is the documented escalation (deferred, not for 4.2). Keep candidate enumeration bounded.

### Frozen vocabulary — consume, never extend

Do **not** add members to `ResolutionResult`, `ProjectReasonCode`, `ProjectLifecycle`, or `ReferenceState` — every member is pinned in the `ProjectVocabularyDescriptors.cs` table and a new member breaks the total-coverage test `tests/Hexalith.Projects.Contracts.Tests/Ui/ProjectVocabularyTests.cs` (`AssertTotalDescriptorCoverage` over `Enum.GetValues`). Confidence is the numeric `Score`/`Rank`, not a wire enum. Exclusion `Diagnostic` values must be members of the closed `ProjectContextInclusionDiagnostic` vocabulary.

### Project Structure Notes

- New code lands in existing projects/namespaces — no new project, no boundary change:
  - Endpoint: `src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs` (partial of `ProjectsDomainServiceEndpoints`), route registered in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`.
  - Candidate-enumeration / evidence-mapping helper: `src/Hexalith.Projects/Resolution/` (`Hexalith.Projects.Resolution`) or `src/Hexalith.Projects.Server` host composition — keep it out of the pure engine file.
  - Optional ACL addition: `src/Hexalith.Projects.Server/Conversations/` (single-conversation metadata read), following `ConversationsProjectConversationDirectory` + `ProjectConversationTranslator`.
  - Contract change: `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (+ a `ResolveProjectFromConversation` request DTO if a body is used; the existing endpoints use private nested `…HttpRequest` records with `RequestSchemaVersion` "v1").
  - Regenerated client: `src/Hexalith.Projects.Client/Generated/*.g.cs` (build target output — do not hand-edit).
- **Detected variance / decision point:** the reference index is forward-only (`ProjectReferenceIndexProjection.List(tenantId, projectId)`), so conversation→projects candidate enumeration must be added (recommended: enumerate authorized tenant projects via `IProjectListReadModel.ListAsync` and derive signals; optional reverse-index precompute if the p95 budget is threatened). This was explicitly handed forward by Story 4.1 and is the central design task — document the chosen approach in the Dev Agent Record.
- **MCP / CLI:** both are stubs and additive. They are **not** required by 4.2's resolution surface — add a read-only tool/verb (calling the regenerated typed client over HTTP, never Dapr/EventStore) only if you choose to expose an agent/operator entry point; otherwise defer to Epic 5.
- **Conventions:** xUnit v3 + Shouldly with **deterministic builders + in-memory fakes** (`ProjectResolutionEvidenceBuilder`, `RecordingLogger`, the `CapturingConversationClient` pattern) — the Projects test projects reference no NSubstitute and Story 4.1 deliberately avoided it, so do not add a mocking package; file-scoped namespaces; copyright header on every `.cs`; `I`-prefixed interfaces; `Async` suffix; `_camelCase` fields; sealed records/classes; route `.WithName` == spine `operationId` (PascalCase); name-based JSON enums; CRLF, UTF-8, final newline, no BOM; `git diff --check` clean. Build with `/home/administrator/.dotnet/dotnet` (10.0.302), not `/usr/bin/dotnet`. Do not weaken nullable/implicit-usings/warnings-as-errors. Root-level submodules only — no recursive submodule init.

### Previous Story Intelligence (Story 4.1 — `done`)

- 4.1 delivered the pure engine + inputs/outputs + scoring rules + the `docs/resolution-scoring-heuristic.md` single source of truth, and registered the engine in DI. It made **no** OpenAPI/`.g.cs` change (no HTTP surface). **4.2 is the inverse: it adds the HTTP query surface and therefore must change the spine and regenerate the client.**
- Explicit caller guidance from 4.1 Dev Notes: "'the reference index + ACL-fetched metadata' describes where the **caller** gets candidate evidence — it is **input** to the engine, not work the engine does." Building the candidate set is host composition owned by 4.2/4.3.
- 4.1's real validation blocker was a `CS1739` named-argument mismatch (`ReasonCode:` vs `reasonCode:`) — a code error, not an environment issue. Build with the pinned SDK; author CRLF/UTF-8/final-newline/no-BOM.
- Carry-forwards flagged for **4.4** (not 4.2): U+2028/U+2029 canonicaliser hardening and an `IsReadOnlyOperation` discriminator — those attach to the mutating `ConfirmProjectResolution` story.
- Final 4.1 test counts: Resolution 71/71, Tier-1 `Hexalith.Projects.Tests` 513/513, full solution 1049/1049 — 4.2 must keep the lane green and add coverage, not replace it.

### Git Intelligence

- `517cc2b feat(story-4.1): Resolution engine (compute-on-demand)` — engine, contracts model, scoring rules, testing builder/leakage helper, 7 Tier-1 resolution test files, scoring-heuristic doc.
- `723352b feat(tests): comprehensive tests for Story 4.1` — determinism, leakage, scoring-matrix patterns to mirror.
- `9727968` / `43e1100` — Epic 4 orchestration/complexity scaffolding.
- Recent work confirms the established read-endpoint pattern (`GetProjectContext`, `RefreshProjectContext`) and the conventional-commit + spine-fingerprint discipline 4.2 must follow.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Project Resolution] — Epic objective, FR-12–15, "never silently attaches; inference never creates a project without confirmation; archived excluded by default."
- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2: Resolve Project From Conversation] — authoritative ACs (AC1, AC2).
- [Source: _bmad-output/planning-artifacts/architecture.md#Decision Priority Analysis] — AR-10 compute-on-demand; persist only `ProjectResolutionConfirmed`; Pattern A; deferred resolution-trace history.
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns] — reads = synchronous REST query; writes = 202 command-async; idempotency rejected on queries; correlation threading; RFC 9457.
- [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns] — safe-denial 404; queries carry freshness/trust; idempotency rejected on queries.
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns] — AR-9 allowlist inclusion; AR-10 resolution rules; tenant authority source; fail-closed definition; metadata-only logging.
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication Patterns] — ACL = Adapter + Translator + fail-closed Facade; only ACL touches sibling types.
- [Source: _bmad-output/planning-artifacts/architecture.md#The Shared State & Reason-Code Vocabulary (single source of truth)] — `NoMatch/SingleCandidate/MultipleCandidates`, reason codes, inclusion states; do not invent parallel enums.
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — `ResolveProject` under `Queries/`; `Resolution/` pure compute-on-demand; `ConfirmProjectResolution` under `Commands/`.
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping] — Resolution (FR-12–15) reads reference index + ACLs; persists only confirmation.
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements Coverage Validation] — p95 < 500 ms resolve budget; multi-ACL fan-out latency watch-item.
- [Source: docs/resolution-scoring-heuristic.md] — single source of truth for scoring/ranking/threshold; explicitly states 4.2 pre-fetches evidence and calls the engine without duplicating the rules; weights and `MinimumQualifyingScore = 20`.
- [Source: docs/payload-taxonomy.md] — payload-classification allowlist / denylist.
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs] — `Resolve(...)` signature, tenant gate, archived gate, scoring, deterministic ordering, fail-closed warning.
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionContext.cs] — context inputs and `Empty(now)` factory.
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionCandidateEvidence.cs] — `ProjectResolutionCandidateEvidence` + `ProjectResolutionMatchSignal`.
- [Source: src/Hexalith.Projects/Resolution/ProjectResolutionScoringRules.cs] — `Weights`, `MinimumQualifyingScore`.
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs] — wire model (`ResolutionCandidate`, `ResolutionExclusion`); no `TenantId`.
- [Source: src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs:37] — engine DI registration.
- [Source: src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs] — route map + `SafeDenial`/`ValidationProblem`/`ReadModelUnavailable`/`SafeProblem`/`ReadHeader`/`IsCanonicalIdentifier`/`ResponseJsonOptions`/header constants.
- [Source: src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs] — closest read-only fetch-then-pure-policy analog (safe-denial 404 collapse).
- [Source: src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs] — multi-source ACL fan-out → pure policy (best evidence-assembly template).
- [Source: src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs] + [ConversationsProjectConversationDirectory.cs] + [ProjectConversationTranslator.cs] — Pattern-A conversation read ACL, fail-closed mapping, per-row scope re-check.
- [Source: src/Hexalith.Projects/Authorization/TenantAccessAuthorizer.cs] + [src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs] + [src/Hexalith.Projects.Server/TenantAccess/ProjectQueryTenantFilter.cs] + [src/Hexalith.Projects.Server/IProjectTenantContextAccessor.cs] — tenant fail-closed chain; claims-only tenant authority.
- [Source: src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs] + [src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs] — leakage denylist + `AssertProjectResolutionNoLeakage`.
- [Source: src/Hexalith.Projects.Testing/Resolution/ProjectResolutionEvidenceBuilder.cs] — deterministic Tier-1 evidence builder (`DefaultTenant = "acme"`, `DefaultNow = 2026-05-28T12:00:00Z`).
- [Source: src/Hexalith.Projects.Testing/TenantIsolation/ProjectTenantIsolationConformance.cs] + [src/Hexalith.Projects.Testing/Context/RecordingLogger.cs] — cross-tenant isolation + structured-log assertions.
- [Source: tests/Hexalith.Projects.Server.Tests/Conversations/ConversationsProjectConversationDirectoryTests.cs] — `CapturingConversationClient` fake pattern for endpoint tests.
- [Source: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml] — spine (18 operations today; add `ResolveProjectFromConversation` as the next; `GetProjectContext` GET block is the template).
- [Source: src/Hexalith.Projects.Client/Generation/Program.cs] — client generator + `HexalithProjectsGeneratedArtifacts.VerifyCurrent` fingerprint gate.
- [Source: src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs:153] — forward-only `List(tenantId, projectId)`; no reverse lookup (the gap Task 1 closes).
- [Source: _bmad-output/project-context.md] — workspace-wide rules (Dapr-only infra, persist-then-publish, tenant isolation, frozen vocabulary, central package management, xUnit-version-per-module, do-not-hand-edit `.g.cs`).

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8, 1M context) via Claude Code `bmad-dev-story` workflow.

### Debug Log References

- `dotnet build Hexalith.Projects.slnx -warnaserror` → 0 Warning(s) / 0 Error(s) (pinned SDK 10.0.302).
- `dotnet test Hexalith.Projects.slnx --no-build` → 1082 passed / 0 failed / 0 skipped — Tier-1 `Hexalith.Projects.Tests` 525, `Hexalith.Projects.Server.Tests` 362, `Hexalith.Projects.Contracts.Tests` 135, `Hexalith.Projects.Client.Tests` 46, `Hexalith.Projects.Integration.Tests` 14 (baseline 1049 → +33).
- Client regenerated via `dotnet msbuild /t:GenerateHexalithProjectsClient` + build (NSwag 14.7.1 + idempotency-helper generator). Spine fingerprint advanced (`ContractSpineSha256` `6c537fb0…` → `a2b60c2e…`); `HexalithProjectsGeneratedArtifacts.VerifyCurrentDetailed` passes; generated `.g.cs` LF-only, no machine path leakage.
- `git diff --check` clean; no submodule pointer changes.

### Completion Notes List

- **Central design decision (Task 1 — candidate enumeration source).** Per the Story 4.1 hand-forward and the NFR-3 guidance, candidates are enumerated from the tenant-scoped `IProjectListReadModel.ListAsync` (a single read-model query, then `ProjectQueryTenantFilter.FilterList`) — **not** request-time multi-ACL fan-out. The reverse conversation→projects index was deliberately **not** added: `ProjectReferenceIndexProjection` indexes folder/file/memory kinds only (no conversation lane), and the precomputed-reverse-index option is an explicit optional optimization deferred unless profiling threatens the p95 budget.
- **Pure host adapter around the 4.1 engine.** A new pure mapper `ConversationResolutionEvidenceMapper` (in domain-core `src/Hexalith.Projects/Resolution/`, Tier-1 testable) translates the single conversation's safe metadata + candidate projects into `ProjectResolutionCandidateEvidence` and delegates **all** scoring/ranking/threshold/outcome to `ProjectResolutionEngine.Resolve(...)`. Conversation matches emit only `ConversationLinked` (explicit/hinted project) and `MetadataMatched` (deterministic trim + ordinal-ignore-case label equality) reason codes (AC4). Degraded conversation trust maps to a non-`Included` `ReferenceState` → engine exclusion, never a positive match.
- **Single-conversation Pattern-A ACL.** Added `IProjectConversationResolutionDirectory` + `ConversationsProjectConversationResolutionDirectory` (real, fail-closed over `IConversationClient.GetConversationAsync`, per-row tenant/conversation scope re-check mirroring `ReadCurrentProjectAsync`) + `UnavailableProjectConversationResolutionDirectory` (fail-closed default). A **new** interface was chosen over extending `IProjectConversationDirectory` to avoid breaking existing directory stubs; the sibling `Hexalith.Conversations.*` reference stays confined to the ACL.
- **Query, not command (AC3).** `GET /api/v1/projects/resolution/from-conversation?conversationId=&includeArchived=` writes no event/projection/state/trace, returns the existing `ProjectResolution` wire model unchanged (no new model/vocabulary member), rejects `Idempotency-Key` (`400 validation_error` field `idempotency_key`), validates freshness (`eventually_consistent` only), threads `X-Correlation-Id`/`X-Hexalith-Task-Id`, and sets `X-Hexalith-Freshness: eventually_consistent`.
- **Fail-closed & tenant authority (AC2, AC6, AC7).** Tenant/principal come only from claims via `IProjectTenantContextAccessor`; authorization (`AuthorizeListAsync`, tenant-level read) runs before any ACL/read-model access; an unauthorized caller, unverifiable tenant authority, or a missing/malformed `conversationId` collapse to safe-denial `404`. A well-formed conversation the ACL cannot read in scope (cross-tenant / unknown / degraded trust) is **not** a `404` here: it fails closed to degraded reference evidence → engine exclusion → `200 NoMatch` — the same response a legitimately project-less conversation returns, so no cross-tenant existence leaks and no in-tenant-vs-cross-tenant oracle is created. Read-model unavailability → `503 read_model_unavailable retryable:true`, never a `500`. The engine receives a server-derived `AuthoritativeTenantId` (`RequestedTenantId == AuthoritativeTenantId`) so its tenant gate behaves. `ProjectResolution` carries no `tenantId`; proven by `NoPayloadLeakageAssertions` + cross-tenant isolation test (`ProjectTenantIsolationConformance`).
- **Contract spine + client lockstep (AC8).** Added the `ResolveProjectFromConversation` operation and `ProjectResolution`/`ResolutionCandidate`/`ResolutionExclusion` schemas (no `tenantId`) to `hexalith.projects.v1.yaml`; regenerated the typed client (GET → typed method, no idempotency helper); the artifacts-fingerprint gate passes.
- **Frozen vocabulary respected.** No new members added to `ResolutionResult`/`ProjectReasonCode`/`ProjectLifecycle`/`ReferenceState`/`ProjectContextInclusionDiagnostic`.

### File List

New:
- `src/Hexalith.Projects/Resolution/ConversationResolutionMetadata.cs`
- `src/Hexalith.Projects/Resolution/ConversationResolutionProjectCandidate.cs`
- `src/Hexalith.Projects/Resolution/ConversationResolutionEvidenceMapper.cs`
- `src/Hexalith.Projects.Server/Conversations/IProjectConversationResolutionDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/ConversationsProjectConversationResolutionDirectory.cs`
- `src/Hexalith.Projects.Server/Conversations/UnavailableProjectConversationResolutionDirectory.cs`
- `src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ConversationResolutionEvidenceMapperTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Queries/ResolveProjectFromConversationTests.cs`
- `tests/Hexalith.Projects.Server.Tests/Conversations/ConversationsProjectConversationResolutionDirectoryTests.cs` _(added in review — Tier-2 fail-closed coverage for the single-conversation ACL)_
- `tests/Hexalith.Projects.Client.Tests/ResolveProjectFromConversationClientTests.cs`

Modified:
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` (route registration for `ResolveProjectFromConversation`)
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` (DI registration for `IProjectConversationResolutionDirectory`)
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (new operation + `ProjectResolution`/`ResolutionCandidate`/`ResolutionExclusion` schemas)
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` (regenerated — do not hand-edit)
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` (regenerated — fingerprint constants)

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-05-29 · **Outcome:** Approve (auto-fix cycle 1) · **Mode:** adversarial, auto-fix without prompting.

**Scope verified:** every file in the File List read against the requirements; build + full test lane re-run with the pinned SDK; File List cross-checked against `git status` (exact match, no undocumented or phantom files); fingerprint gate re-verified after spine edit.

**Evidence:**
- Build: `/home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx -warnaserror` → **0 Warning(s) / 0 Error(s)**.
- Tests: `dotnet test Hexalith.Projects.slnx --no-build` → **1103 passed / 0 failed / 0 skipped** (Tier-1 `Hexalith.Projects.Tests` 525, `Hexalith.Projects.Server.Tests` 383 (+21 review-added), `Hexalith.Projects.Contracts.Tests` 135, `Hexalith.Projects.Client.Tests` 46, `Hexalith.Projects.Integration.Tests` 14). Baseline 1082 → 1103.
- Spine fingerprint advanced after the description correction (`a2b60c2e…` → `daaae187…`); `HexalithProjectsGeneratedArtifacts.VerifyCurrent` green; `git diff --check` clean; no submodule pointer change.

**AC validation:** AC1–AC8 all **IMPLEMENTED** and verified against code (not just task ticks). The engine is genuinely reused — no scoring/ranking/threshold logic re-implemented in the adapter; conversation matches use only `ConversationLinked` / `MetadataMatched`; no `ResolutionResult`/`ProjectReasonCode`/`ProjectLifecycle`/`ReferenceState`/`ProjectContextInclusionDiagnostic` members added; `ProjectResolution` carries no `tenantId` (proven by leakage + client reflection tests); `Idempotency-Key` rejected (`400 validation_error`) after authorization; freshness validated; archived-exclusion delegated to the engine; tenant authority claims-only and fail-closed.

**Task audit:** all `[x]` tasks confirmed done by file:line evidence. No false completion. No CRITICAL findings.

**Findings auto-fixed this cycle:**
- **[MEDIUM] Inaccurate / self-contradictory safe-denial documentation.** The endpoint XML doc and the OpenAPI `description` claimed a *cross-tenant/unknown conversation* "collapses to safe-denial 404." Actual (and correct) behavior: a well-formed conversationId the ACL cannot read in scope fails closed to degraded evidence → engine exclusion → **200 NoMatch**, identical to a legitimately project-less conversation. This is *more* privacy-preserving than a 404 (a conversation-scoped 404 would create an in-tenant-vs-cross-tenant membership oracle) and satisfies the architecture's existence-non-inference intent. The 404 path correctly covers an unauthorized caller, unverifiable tenant authority, and a missing/malformed `conversationId`. **Fix:** corrected `ResolveProjectFromConversationEndpoint.cs` XML doc and the spine operation `description` to describe the actual behavior; regenerated the client (fingerprint re-verified). Code behavior left unchanged (it was correct).
- **[MEDIUM] New conversation ACL adapter had no tests.** `ConversationsProjectConversationResolutionDirectory` (the only code touching `Hexalith.Conversations.*` on this path — trust-state mapping, HTTP-status mapping, per-row tenant/conversation scope re-check, explicit-project-before-hydration precedence) and the `UnavailableProjectConversationResolutionDirectory` fail-closed default were exercised only through endpoint stubs. **Fix:** added `ConversationsProjectConversationResolutionDirectoryTests.cs` (21 Tier-2 cases using a `CapturingConversationClient`-style stub) covering `ToReferenceState` (Current→Included, Stale→Stale, Forbidden→Unauthorized, Redacted→Excluded, Rebuilding/Unavailable→Unavailable), `MapFailure` (401/403/404→Unauthorized, 5xx→Unavailable), tenant/conversation scope escape → Unavailable with no leak, hidden-body → fail-closed, exception → Unavailable, cancellation propagation, and the unavailable default.
- **[LOW] Stale completion note.** Note claimed Idempotency-Key rejection emits `400 idempotency_error`; the code and AC5 use `400 validation_error`. **Fix:** corrected the note.

## Change Log

- 2026-05-29 — Story 4.2 Resolve Project From Conversation implemented end-to-end against baseline `517cc2b`. Added the GET resolve-from-conversation query endpoint as the impure host adapter around the pure Story 4.1 resolution engine: conversation→projects candidate enumeration via the tenant-scoped list read model, a single-conversation Pattern-A metadata ACL, a pure evidence mapper (ConversationLinked / MetadataMatched only), OpenAPI spine operation + `ProjectResolution` schema, and client regeneration. All 8 ACs satisfied; full solution 1082/1082 green; build `-warnaserror` 0W/0E; fingerprint gate passes; no submodule pointer changes; `git diff --check` clean.
- 2026-05-29 — Senior Developer Review (AI), auto-fix cycle 1: 0 CRITICAL. Auto-fixed 2 MEDIUM + 1 LOW findings — (M1) corrected self-contradictory safe-denial documentation in the endpoint XML doc and the OpenAPI operation description (a cross-tenant/unknown/degraded conversation is `200 NoMatch` via engine exclusion, not a `404`; 404 is reserved for unauthorized caller / unverifiable tenant / malformed-or-missing `conversationId`), client regenerated (fingerprint `a2b60c2e…` → `daaae187…`); (M2) added `ConversationsProjectConversationResolutionDirectoryTests.cs` (21 Tier-2 fail-closed cases) to cover the previously untested single-conversation ACL adapter + unavailable default; (L1) corrected stale completion note (`idempotency_error` → `validation_error`). No code behavior changed beyond documentation; build `-warnaserror` 0W/0E; full solution **1103/1103** green; fingerprint gate passes; `git diff --check` clean; no submodule pointer change. Status → done.
