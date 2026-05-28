---
baseline_commit: 883ebca
---

# Story 3.5: Retrieve Conversation-Start Setup

## Status

done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to retrieve the subset of Project Setup needed to start or resume a conversation**,
So that **I can begin the first response without re-querying every bounded context** _(FR-20; realizes UJ-1, UJ-4; AR-8 `ConversationStartSetupProjection`, AR-9 inclusion policy)_.

This is the **fifth and final Epic 3 story** and the **fourth and final HTTP-surfaced consumer** of the Story 3.1 `ProjectContextInclusionPolicy`. Stories 3.2 (`GET /api/v1/projects/{projectId}/context`), 3.3 (`GET /api/v1/projects/{projectId}/context/explain`), and 3.4 (`GET /api/v1/projects/{projectId}/context/refresh`) all return the full assembled `ProjectContext` (or its `ProjectContextExplanation` wrapper) — a metadata-only payload of Project Setup + per-kind reference inventories + per-reference exclusion diagnostics. Story 3.5 is the **fast-path** the Chatbot calls **before the first response** of a conversation: it returns ONLY the bounded subset of `ProjectSetup` (goals, instructions, preferred/excluded source kinds, the default `LinkedSourcePolicy`) plus the lifecycle/freshness signal. **No sibling ACL recheck. No conversation page fetch. No folder/file/memory reference inventory.** FR-20 wording is explicit: "stable enough to use without re-querying every bounded context before the first response" — Story 3.5 is the FAST PATH and Stories 3.2/3.4 remain the full-context surfaces.

Story 3.5 adds **`GET /api/v1/projects/{projectId}/setup/conversation-start`** returning a NEW wire DTO **`ConversationStartSetup`** (a bounded subset of `ProjectSetup`). The handler is a thin orchestrator that:

- runs the canonical envelope/header gates (`X-Correlation-Id` / `X-Hexalith-Task-Id` / `X-Hexalith-Freshness` reads + validation; `Idempotency-Key` rejection after authorization; non-`eventually_consistent` freshness rejection — copy verbatim from Story 3.2 lines 62–92);
- calls `ProjectAuthorizationGate.AuthorizeReadAsync(...)` (Story 3.2's TenantAccess-bearing extension); on safe-denial collapses to **HTTP 404** with no body (carry-forward of Story 1.4 + 3.1 + 3.2 + 3.3 + 3.4 safe-denial contract);
- invokes `ProjectContextInclusionPolicy.Assemble(...)` (Story 3.1 — unchanged) with `OperationKind: ProjectContextOperationKind.GetConversationStartSetup` and an **empty** `ProjectContextReferenceEvidence` (no `ProjectFolder` / `FileReferences` / `MemoryReferences` / `Conversations`) — the policy is exercised ONLY for its tenant-authority / project-visibility / project-lifecycle / freshness outer collapses; no per-candidate evaluations are emitted because the references-evidence is empty (verified by re-reading `ProjectContextInclusionPolicy.cs:130–187`: the per-candidate loop is a no-op over empty input lists);
- maps the policy's `assembled.Context.{Setup, Lifecycle, Freshness, ObservedAt}` into the bounded `ConversationStartSetup` wire DTO via a NEW pure mapper `ConversationStartSetupProjector.Project(...)` placed under `src/Hexalith.Projects/Projections/ConversationStartSetup/` (the architecture's named `ConversationStartSetupProjection` — see Story Scope / Design Decision below for the rationale of materializing it as a pure projector over `ProjectDetailItem.Setup` rather than a separate event-stream projection);
- returns the `ConversationStartSetup` body (NOT the assembled `ProjectContext` — Story 3.5 surfaces ONLY the bounded subset FR-20 promises; the Explain wrapper from Story 3.3 is unrelated).

The policy is invoked **unchanged**. `ProjectContextOperationKind.GetConversationStartSetup` already ships at `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:37` (Story 3.1 forward-built this) and is already on the read-only allowlist at `ProjectContextInclusionPolicy.cs:238`. The decision matrix `docs/context-assembly-decision-matrix.md` already has a `GetConversationStartSetup (3.5)` column (line 16, fifth operation) and the cell semantics are **identical to the `Get` column** for every per-evidence-state row, every outer-override row, every Memories row, every Folder row, every File row, every Conversation row — Story 3.5 is a read-only operation and consumes the matrix verbatim. The fail-closed verdict per cell does NOT change with the operation kind. What changes is the **subset of evidence surfaced on the wire**: per-reference rows do not appear in the response body at all because the body is `ConversationStartSetup`, not `ProjectContext`. The outer outcomes (`Unauthorized` → HTTP 404; `ProjectUnavailable` → HTTP 404; `Assembled` → HTTP 200 with body) ARE preserved.

Story 3.5 must NOT duplicate any policy include/exclude / fail-closed-collapse / freshness-mapping / diagnostic-vocabulary / reference-kind-allowlist decision in the endpoint, the projector, or the wire shape; the policy is the single source of truth (Story 3.1 / 3.2 / 3.3 / 3.4 guardrail carried forward). The endpoint owns ONLY: (i) the HTTP envelope (headers, status, body), (ii) projecting `ProjectSetup` + `ProjectLifecycle` + `ProjectContextFreshness` + `DateTimeOffset` onto the bounded `ConversationStartSetup` wire DTO via the new projector, and (iii) the closed-vocabulary safe-denial response. The endpoint does NOT call any sibling ACL, does NOT fetch any conversation page, does NOT inspect reference state — all of those are out of scope for the conversation-start fast-path.

Everything Story 3.5 produces is metadata-only (FS-2 — verified by the leakage harness extended over the new DTO + the endpoint response, mirroring Story 3.2 `GetProjectContext_ResponseBody_HasNoLeakageAcrossOutcomes` at `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs:354` and Story 3.4 `RefreshProjectContext_ResponseBody_HasNoLeakageAcrossOutcomes`), tenant-scoped (FS-8 / SM-3), and fails closed at every layer (NFR-1 / NFR-2 / NFR-3). Story 3.5 does NOT modify `ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyResult`, `ProjectContextEvaluation`, `ProjectContextExplanation`, `ProjectContextInclusionPolicy`, `ProjectContextInclusionOrder`, the closed `ProjectContextInclusionDiagnostic` vocabulary, the four assembly enums, `ProjectContextOperationKind`, the four ACL interfaces (existing OR additive Story 3.4 `RefreshXxxAsync` methods), the three Story 3.4 outcome mappers, the Story 3.2 `GetProjectContextEndpoint.cs` / Story 3.3 `GetProjectContextExplanationEndpoint.cs` / Story 3.4 `RefreshProjectContextEndpoint.cs` files, or the existing `ProjectSetup` / `ConversationStartDefaults` / `LinkedSourcePolicy` / `ProjectContextSourceKind` contracts. Story 3.5 DOES introduce ONE new wire DTO (`ConversationStartSetup` under `Hexalith.Projects.Contracts/Models/`) and ONE new pure server-side projector (`ConversationStartSetupProjector` under `Hexalith.Projects/Projections/ConversationStartSetup/`) — both additive, no breaking changes.

Story 3.5 does NOT realize any new Epic 2 retrospective action item. The carry-forward U+2028 / U+2029 canonicaliser hardening (Action 2) remains "for the next mutation surface" per the Epic 2 retro line 273 — Story 3.5 is a read-only query, the action item still survives in the carry-forward list. Story 3.5 DOES carry forward the canonical `docs/checklists/mutation-and-query-negative-tests.md` (Story 3.2 / Action Item 7 deliverable) by ticking the same query-side rows that Stories 3.2 / 3.3 / 3.4 applied (rows 1 / 4 / 5 / 6 / 8 of the 8-row checklist; rows 2 / 3 / 7 are mutation-only and N/A).

Story 3.5 is the **last story of Epic 3**. After Story 3.5 reaches `done`, `epic-3` transitions to `done` (or the optional `epic-3-retrospective` story runs first per sprint-status — the dev agent does NOT itself promote epic-3; that is the retrospective story's responsibility).

## Acceptance Criteria

1. A new HTTP endpoint **`GET /api/v1/projects/{projectId}/setup/conversation-start`** is added to the OpenAPI spine `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` mirroring `GetProjectContext` / `GetProjectContextExplanation` / `RefreshProjectContext`'s read shape (operationId `GetConversationStartSetup`, tags `projects`, parameters `ProjectId` / `CorrelationId` / `Freshness`, responses `200` / `400` / `401` / `403` / `404` / `503`, `x-hexalith-read-consistency: eventually_consistent`, `x-hexalith-correlation` query-correlation-only, `x-hexalith-authorization: tenant-context-and-project-read-permission`, `x-hexalith-canonical-error-categories` matching `GetProjectContext`'s 8-row set — **without** the Story 3.4-specific `referenced_resource_unavailable` because Story 3.5 does NOT recheck siblings). `Idempotency-Key` is NOT a parameter and is rejected as `validation_error` if present after authorization (carry-forward of the `GetProject` / `GetProjectContext` / `GetProjectContextExplanation` / `RefreshProjectContext` pattern). The 200 response schema is the **new `ConversationStartSetup`** (NOT `ProjectContext` — Story 3.5 surfaces only the bounded subset). The 200 response carries `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent` headers. The operation block is placed in the spine YAML immediately after the existing `GET /api/v1/projects/{projectId}/context/refresh` block (current location: lines 414–480 of the spine) — Story 3.5's block is the next sibling under `paths` and is followed by the existing `/api/v1/projects/{projectId}/conversations/{conversationId}/link` mutation block at current line 481. The URL convention `/setup/conversation-start` was chosen over `/conversation-start-setup` to sit alongside a future `/setup/...` family of read endpoints (Setup Quality FRs) and to keep `/context/...` reserved for context-assembly surfaces (Stories 3.2 / 3.3 / 3.4). The dev agent MAY surface a deviation in the Dev Agent Record if a different URL is judged better, but the URL MUST live under `/api/v1/projects/{projectId}/` and MUST be GET.

2. **A new `ConversationStartSetup` wire DTO** is added at `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` — a sealed record carrying the bounded subset of `ProjectSetup` plus the lifecycle/freshness/observed-at envelope. The exact shape (positional record, ordered for stable wire layout):
   ```
   public sealed record ConversationStartSetup(
       string ProjectId,
       ProjectLifecycle Lifecycle,
       IReadOnlyList<string> Goals,
       IReadOnlyList<string> UserInstructions,
       IReadOnlyList<ProjectContextSourceKind> PreferredSourceKinds,
       IReadOnlyList<ProjectContextSourceKind> ExcludedSourceKinds,
       LinkedSourcePolicy LinkedSourcePolicy,
       DateTimeOffset ObservedAt,
       ProjectContextFreshness Freshness);
   ```
   Field rules:
   - `ProjectId` — echoes the request path parameter (metadata-only; safe to echo per Story 3.2 / 3.4 precedent).
   - `Lifecycle` — copied from `ProjectContext.Lifecycle` (which mirrors `ProjectDetailItem.Lifecycle`).
   - `Goals` / `UserInstructions` / `PreferredSourceKinds` / `ExcludedSourceKinds` — copied from `ProjectSetup.Goals` / `UserInstructions` / `PreferredSourceKinds` / `ExcludedSourceKinds` when `ProjectContext.Setup is not null`; otherwise empty arrays (matches `ProjectSetup.Empty`).
   - `LinkedSourcePolicy` — taken from `ProjectSetup.ConversationStartDefaults?.LinkedSourcePolicy ?? LinkedSourcePolicy.None` (the closed default-of-default per the v1 closed vocabulary at `src/Hexalith.Projects.Contracts/Models/LinkedSourcePolicy.cs:18`).
   - `ObservedAt` — taken from `ProjectContext.ObservedAt` (the policy's typed `Now` input — the policy is the single observation-instant authority).
   - `Freshness` — taken from `ProjectContext.Freshness` (the policy's `MapFreshness(...)` output — the policy is the single freshness authority).
   - **NO** `TenantId` field on the wire body (FS-8 / SM-3 — tenant authority is a server-derived claim, NEVER a wire field; mirrors the `[JsonIgnore]` rule applied to `ProjectContext.TenantId` at `ProjectContext.cs:67`). The DTO does NOT carry a `TenantId` field at all (cleaner than `[JsonIgnore]`-on-required-field).
   - **NO** internal audit metadata: no `Sequence`, no projection watermark, no `CreatedAt` / `UpdatedAt` (those live on `ProjectDetailItem` for audit/operator surfaces, NOT on the FR-20 fast-path response).
   - **NO** references inventory: no `ProjectFolder` / `FileReferences` / `MemoryReferences` / `Conversations` fields — FR-20 is explicit "without re-querying every bounded context first" so this body MUST NOT expose per-reference state (the consumer that wants that calls `GetProjectContext` instead).
   - **NO** unavailable/unauthorized reference rows: not applicable because the body has no per-reference inventory.
   The record exposes static factories:
   - `ConversationStartSetup.Empty(string projectId, ProjectLifecycle lifecycle, DateTimeOffset observedAt, ProjectContextFreshness freshness)` — the empty-setup happy path Story 3.2 `ProjectContext.Empty` mirrors.
   - `ConversationStartSetup.FromContext(ProjectContext context)` — the canonical mapper the projector uses; the policy's `ProjectContext` is the SOLE input; the projector NEVER inspects raw `ProjectDetailItem.Setup` directly.
   No additional static factories for safe-denial outcomes — those collapse to HTTP 404 with NO body (Problem Details only), so a `ConversationStartSetup.Unauthorized(...)` / `.ProjectUnavailable(...)` static is NOT needed (would be misleading dead code).

3. **A new pure server-side projector** is added at `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs` — internal `public static class` with a single `public static ConversationStartSetup Project(ProjectContext context)` (the named `ConversationStartSetupProjection` from AR-8, realized as a pure projector over `ProjectContext` rather than a separate event-stream projection — see Design Decision in Dev Notes). The projector:
   - Accepts the policy's `ProjectContext` output verbatim (single-input contract — keeps the projector's purity testable).
   - Reads `context.Setup` (nullable). When null, the conversation-start body carries empty `Goals` / `UserInstructions` / `PreferredSourceKinds` / `ExcludedSourceKinds` arrays and `LinkedSourcePolicy.None` — matching the `ProjectSetup.Empty` semantic.
   - Reads `context.Setup?.ConversationStartDefaults?.LinkedSourcePolicy` and defaults to `LinkedSourcePolicy.None` when absent.
   - Reads `context.ProjectId` / `context.Lifecycle` / `context.ObservedAt` / `context.Freshness` — all metadata-only and safe to expose.
   - Returns a new `ConversationStartSetup` record. The projector is a pure function: same input → same output; no `DateTimeOffset.UtcNow` / `DateTime.Now` / `Stopwatch` / `Environment.TickCount` / random / GUID; no infrastructure imports; lives under `src/Hexalith.Projects/Projections/ConversationStartSetup/` (Tier-1 purity).
   The projector consumes ONLY `Hexalith.Projects.Contracts.Models.*` and `Hexalith.Projects.Contracts.Ui.*` types. It does NOT import `Hexalith.Projects.Context.*` (the policy types — those are upstream from the endpoint, not the projector). It does NOT import `Hexalith.Projects.Projections.ProjectDetail.*` (`ProjectDetailItem` is the endpoint's input, not the projector's).

4. The OpenAPI spine fingerprint changes deliberately (new operation, ONE new wire schema `ConversationStartSetup`, ONE new example `components/examples/ConversationStartSetup`). The dev agent **regenerates** `HexalithProjectsClient.g.cs` and `HexalithProjectsIdempotencyHelpers.g.cs` via the standard MSBuild target (the NSwag Linux path fix from Story 3.2 stays in place — no `.csproj` change). Acceptance: a single `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` invocation on Linux regenerates both `.g.cs` files cleanly without manual intervention. The fingerprint baseline is updated; the OpenAPI fingerprint gate transitions to PASSED-with-baseline-update **for this story only**. **Story 3.5 is the last operation owner in Epic 3** — after Story 3.5 the OpenAPI spine should remain stable until Epic 4 (Resolution endpoints) lands. `HexalithProjectsIdempotencyHelpers.g.cs` is byte-stable except for the SHA256 fingerprint constants — queries have no idempotency surface (mirrors Story 3.2 / 3.3 / 3.4). The frontcomposer gate stays skip-clean (no `[Projection]` / `[Command]` contracts touched). The new `components.schemas.ConversationStartSetup` schema MUST list every wire field declared on the DTO with explicit `type` / `nullable` / `enum` constraints (no schema reuse for `Goals` / `UserInstructions` arrays — they are `array<string>`; `PreferredSourceKinds` / `ExcludedSourceKinds` are `array<ProjectContextSourceKind>` reusing the existing schema; `LinkedSourcePolicy` reuses the existing schema; `Lifecycle` reuses `ProjectLifecycle`; `Freshness` reuses `ProjectContextFreshness`; `ObservedAt` is `string format date-time`). The `components.examples.ConversationStartSetup` example MUST contain only safe metadata strings — no path-like / token-like / payload-like content (verified by the existing `OpenApiContractSpineTests.Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` gate). A second optional example `ConversationStartSetupEmpty` (the no-setup happy path) MAY be added under `components.examples.ConversationStartSetupEmpty`.

5. A new query-side handler `GetConversationStartSetupAsync` is added as a **new partial-class file** `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` mirroring the Story 3.2 / 3.3 / 3.4 split decision (the base `ProjectsDomainServiceEndpoints.cs` is at ~395+ LOC after Story 3.4's registration; adding another inline handler is rejected; the partial-class pattern is the canonical placement). The handler:
   (a) reads canonical headers `X-Correlation-Id` / `X-Hexalith-Task-Id` / `X-Hexalith-Freshness` and validates them per the existing helpers (`ReadHeader`, `IsCanonicalIdentifier`);
   (b) treats a missing or non-canonical `projectId` route value as a safe-denial 404 (NEVER reveals existence);
   (c) calls `ProjectAuthorizationGate.AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)` and returns `ReadModelUnavailable` (503) only when `Authorization.Retryable && Authorization.Reason == ReferenceState.Unavailable`, otherwise `SafeDenial` 404 (existence-non-inference, mirrors Story 3.2 / 3.3 / 3.4);
   (d) rejects `Idempotency-Key` if present after authorization (`ValidationProblem(..., "idempotency_key")`);
   (e) rejects any non-`eventually_consistent` `X-Hexalith-Freshness` request as `ValidationProblem(..., "freshness")`;
   (f) defensively collapses a missing `authorization.TenantAccessResult` to safe-denial 404 (mirrors Story 3.2 lines 94–99 / Story 3.3 / Story 3.4 lines 109–114);
   (g) **does NOT fetch a conversation page** (FR-20 fast-path — `IProjectConversationDirectory` is NOT a handler dependency);
   (h) **does NOT call any sibling ACL** (`IProjectFolderDirectory` / `IProjectFileReferenceDirectory` / `IProjectMemoryDirectory` are NOT handler dependencies — Story 3.5 surfaces no per-reference state);
   (i) invokes `ProjectContextInclusionPolicy.Assemble(...)` (Story 3.1 — unchanged) with `OperationKind: ProjectContextOperationKind.GetConversationStartSetup` and an EMPTY `ProjectContextReferenceEvidence` (all four lists empty: `ProjectFolder: null`, `FileReferences: Array.Empty<ProjectFileReference>()`, `MemoryReferences: Array.Empty<ProjectMemoryReference>()`, `Conversations: Array.Empty<ProjectContextConversationEvidence>()`) so the policy exercises ONLY its outer collapses (tenant authority / project visibility / project lifecycle / freshness) without emitting per-candidate evaluations;
   (j) on `assembled.Context.AssemblyOutcome == ProjectContextAssemblyOutcome.Assembled`, projects `assembled.Context` via `ConversationStartSetupProjector.Project(...)` and returns `Results.Json(setup, ResponseJsonOptions)` (mirroring Story 3.2 line 139's serializer + the existing static `ResponseJsonOptions`);
   (k) on `assembled.Context.AssemblyOutcome == ProjectContextAssemblyOutcome.Unauthorized` or `.ProjectUnavailable`, returns `SafeDenial(correlationId, null)` — the safe-denial 404 contract (carry-forward of Story 3.2 / 3.3 / 3.4 — never differentiate at the HTTP boundary; the policy's internal `AssemblyOutcome` is observability-only);
   (l) sets `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent` response headers (mirrors `GetProjectContext`).

6. The endpoint is **registered** by adding a single `endpoints.MapGet("/api/v1/projects/{projectId}/setup/conversation-start", ...)` call in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` `ConfigureEndpoints` method directly after the existing `GET /api/v1/projects/{projectId}/context/refresh` registration (currently at line 155–177). The route MUST be registered as `/setup/conversation-start` so it sits in a NEW `/setup/...` namespace distinct from `/context/...` (and route-precedence remains unambiguous — literal segments win, `{projectId}` matches one segment only). The DI-injected dependencies for the lambda are: `string projectId, HttpContext httpContext, IProjectTenantContextAccessor tenantContext, ProjectAuthorizationGate authorizationGate, ProjectContextInclusionPolicy contextPolicy, TimeProvider timeProvider, CancellationToken cancellationToken` — a **strict subset** of Story 3.2 / 3.3 / 3.4 (no `IProjectConversationDirectory` and no `IProjectFolderDirectory` / `IProjectFileReferenceDirectory` / `IProjectMemoryDirectory` because Story 3.5 calls no siblings).

7. The handler **never re-evaluates** any policy decision. The full chain is:
   - **endpoint** (validates envelope, rejects bad `Idempotency-Key` / freshness, runs `AuthorizeReadAsync`, calls the policy with empty references-evidence, projects the assembled context to the bounded `ConversationStartSetup` body, serializes);
   - **policy** (Story 3.1 — sole owner of inclusion order, fail-closed collapse, freshness mapping, diagnostic vocabulary, reference-kind allowlist, per-candidate evaluation emission, and deterministic ordering — though for Story 3.5 the per-candidate path is a no-op over empty input);
   - **projector** (NEW Story 3.5 — pure mapping from `ProjectContext` to `ConversationStartSetup`; never invokes the policy; never decides include/exclude).
   No conditional include/exclude logic, freshness threshold, tenant-collapse rule, diagnostic vocabulary lookup, or evaluation re-sort is duplicated in the endpoint or the projector. The endpoint receives a `ProjectContextAssemblyResult` from the policy and serializes the projector's output of `result.Context` (not `result.Context` directly).

8. **Fail-closed negative-evidence matrix (evidence-state × `GetConversationStartSetup`)** outer-collapse cells are covered by Tier-2 Server tests in a new file `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs`. Required cells (rows of the `GetConversationStartSetup (3.5)` column in `docs/context-assembly-decision-matrix.md`, line 16 fifth operation — **identical to the Get column** by design):
   - **Per-reference rows are NOT covered as endpoint-level cells** because Story 3.5 surfaces no per-reference state on the wire body; per-reference cells are owned by Stories 3.2 / 3.3 / 3.4 endpoint tests and the Story 3.1 Tier-1 fixture suite under `tests/Hexalith.Projects.Tests/Context/`.
   - **Outer collapses ARE covered** as endpoint-level cells: `AuthoritativeTenantId` missing → policy `Unauthorized` outer → HTTP 404; cross-tenant → policy `ProjectUnavailable` outer → HTTP 404; archived project → policy `Assembled` (Lifecycle=Archived) outer → HTTP 200 with `Lifecycle: archived` in the `ConversationStartSetup` body (and any setup that was last durably written remains intact — archived projects are auditable per FR-4; the consumer is informed via the `Lifecycle` field that the project is no longer active context).
   - Each test asserts: (i) HTTP status code is `200` (assembled, including the archived-project case) or `404` (safe-denial for `Unauthorized` / `ProjectUnavailable`) or `503` (read-model unavailable + retryable); (ii) response headers `X-Correlation-Id` (when present) and `X-Hexalith-Freshness` are present (when the response is 200); (iii) the `ConversationStartSetup` body contains the expected `Lifecycle` / `Freshness` / `ObservedAt` / `LinkedSourcePolicy` / `Goals` / `UserInstructions` / `PreferredSourceKinds` / `ExcludedSourceKinds`; (iv) `NoPayloadLeakageAssertions.AssertNoLeakage(...)` runs over the response body for the assembled case; (v) for collapse cases, the response is a Problem Details body with no `ConversationStartSetup` shape and no tenant id / project id leakage. Each test is a named fixture (mirrors Story 3.2 / 3.3 / 3.4 named-fixture pattern).

9. **The Story 3.5-specific contract** is explicitly tested in `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs`. These are the tests Story 3.5 owns that have no Story 3.2 / 3.3 / 3.4 equivalent:
   - `GetConversationStartSetup_HappyPath_Returns200WithBoundedSubset` — projection-stored `ProjectSetup` carries goals + user instructions + preferred/excluded source kinds + a `ConversationStartDefaults(LinkedSourcePolicy: AuthorizedReferences)`; assert the response body's `Goals` / `UserInstructions` / `PreferredSourceKinds` / `ExcludedSourceKinds` / `LinkedSourcePolicy` mirror the stored values; assert `Lifecycle: active`, `Freshness: fresh`, `ObservedAt` equals the policy's `Now` input.
   - `GetConversationStartSetup_NullSetup_ReturnsEmptyBoundedSubset` — projection-stored `Setup is null` (newly created project with no UpdateProjectSetup yet); assert the response body has empty `Goals` / `UserInstructions` / `PreferredSourceKinds` / `ExcludedSourceKinds` arrays and `LinkedSourcePolicy: none` (the default-of-default per AC 2).
   - `GetConversationStartSetup_ArchivedProject_Returns200WithLifecycleArchived` — projection-stored `Lifecycle = Archived` and a non-null `Setup`; assert HTTP 200 with `Lifecycle: archived` AND the setup subset surfaced verbatim (archived projects are auditable — the caller is informed via the `Lifecycle` field that the project is no longer active context, BUT the bounded subset is still returned so post-archival audit / conversation-resume on a archived project remains feasible per FR-4 + FR-20).
   - `GetConversationStartSetup_ConversationStartDefaultsMissing_DefaultsToLinkedSourcePolicyNone` — projection-stored `Setup.ConversationStartDefaults is null`; assert `LinkedSourcePolicy: none`.
   - `GetConversationStartSetup_PreferredAndExcludedSourceKinds_PreserveOrder` — projection-stored ordered lists; assert the wire body preserves the ordering exactly (mirrors `ProjectSetup` v1 invariant — the dev agent inspects `ProjectSetup` semantics, but per AC 2 the projector copies the stored lists verbatim, NOT sorted).
   - `GetConversationStartSetup_BodyDoesNotContainTenantId` — assert the serialized JSON does NOT contain a `tenantId` field (FS-8 / SM-3 — tenant authority is NEVER a wire field). `ResponseBody.ShouldNotContain("tenantId"); ResponseBody.ShouldNotContain("TenantId");`
   - `GetConversationStartSetup_BodyDoesNotContainAuditMetadata` — assert the serialized JSON does NOT contain `createdAt` / `updatedAt` / `sequence` / `setupMetadata` fields (internal audit metadata not in scope for FR-20 fast path).
   - `GetConversationStartSetup_BodyDoesNotContainReferenceInventory` — assert the serialized JSON does NOT contain `projectFolder` / `fileReferences` / `memoryReferences` / `conversations` / `excluded` / `assemblyOutcome` fields (no per-reference inventory in the bounded subset).
   - `GetConversationStartSetup_DoesNotCallSiblingAcls` — uses a `RecordingConversationDirectory` / `RecordingFolderDirectory` / `RecordingFileReferenceDirectory` / `RecordingMemoryDirectory` stub registered through the test host's DI override; on a happy-path request, asserts every recording directory's `CallCount == 0`. This is the FR-20 fast-path guarantee — Story 3.5 must NEVER touch sibling ACLs.

10. **Cross-tenant isolation (FS-8 / SM-3)** — a dedicated test in `GetConversationStartSetupTests.cs` constructs a request with `AuthoritativeTenantId = "tenant-a"` against a project whose `ProjectDetailItem.TenantId = "tenant-b"` and asserts: (i) HTTP 404 (safe-denial; never 403, never reveals existence); (ii) no `ConversationStartSetup` body — only a Problem Details safe-denial body; (iii) no tenant id appears in the response headers, body, or correlation-id-equivalent fields; (iv) reuses the FS-8/SM-3 harness from Story 1.4 / 1.6 / 3.1 / 3.2 / 3.3 / 3.4 (Story 3.4's `RefreshProjectContext_CrossTenant_ReturnsSafeDenial404` is the canonical pattern). The no-sibling-ACL-call assertion (AC 9 `GetConversationStartSetup_DoesNotCallSiblingAcls`) implicitly subsumes the no-leak-to-siblings invariant — Story 3.5 NEVER calls a sibling ACL on ANY request path so the cross-tenant request cannot leak existence to siblings by construction.

11. **Tier-1 projector purity tests.** Add a new file `tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/ConversationStartSetupProjectorTests.cs` (Tier-1 purity, NO infrastructure, NO sibling client). Required tests:
    - `Project_AssembledContextWithFullSetup_MirrorsSubset` — `ProjectContext.Setup` carries non-empty `Goals` / `UserInstructions` / source-kind lists + non-null `ConversationStartDefaults`; assert the projected `ConversationStartSetup` mirrors every field including `LinkedSourcePolicy`.
    - `Project_AssembledContextWithNullSetup_ReturnsEmptySubset` — `ProjectContext.Setup is null`; assert empty arrays + `LinkedSourcePolicy.None`.
    - `Project_AssembledContextWithNullConversationStartDefaults_DefaultsToNone` — `Setup is not null`, `Setup.ConversationStartDefaults is null`; assert `LinkedSourcePolicy.None`.
    - `Project_PreservesLifecycle` — Theory over `ProjectLifecycle.Active` and `ProjectLifecycle.Archived`; assert wire `Lifecycle` matches input.
    - `Project_PreservesFreshness` — Theory over `ProjectContextFreshness.Fresh` / `Stale` / `Unavailable` / `Unknown`; assert wire `Freshness` matches input.
    - `Project_PreservesObservedAt` — input `ObservedAt = DateTimeOffset.Parse("2026-01-15T12:34:56Z")`; assert wire `ObservedAt` matches.
    - `Project_PreservesProjectId` — input `ProjectId = "project-canonical-id"`; assert wire `ProjectId` matches.
    - `Project_PreservesSourceKindOrder` — input `PreferredSourceKinds = [Folder, Memory, Conversation]`; assert wire `PreferredSourceKinds` preserves order.
    - `Project_AssembledContextWithEmptySourceKinds_ReturnsEmptyArrays` — `Setup.PreferredSourceKinds = []`; assert wire arrays are empty (not null).
    - `Project_IsPureFunction_SameInputProducesSameOutput` — invokes `Project` twice over the same input; asserts the two outputs are value-equal (record equality).
    - `Project_DoesNotMutateInput` — verifies the input `ProjectContext` is not mutated (records are immutable by design, but this asserts no in-place list mutation).
    Reuses `ProjectContextEvidenceBuilder` from `src/Hexalith.Projects.Testing/Context/` (Story 3.1) where helpful, but the projector tests are simple enough to inline fixtures.

12. **Contracts tests for the new `ConversationStartSetup` DTO.** Add a new file `tests/Hexalith.Projects.Contracts.Tests/Models/ConversationStartSetupTests.cs`:
    - `ConversationStartSetup_RoundTripsSerialization` — serializes and deserializes a fully-populated record; assert value-equality of original vs round-tripped (mirrors the Story 3.1 `ProjectContext_RoundTripsSerialization` pattern).
    - `ConversationStartSetup_SerializesMetadataOnly` — runs `NoPayloadLeakageAssertions.AssertNoLeakage(...)` over the serialized JSON for a fully-populated record AND for the `Empty` static; asserts no forbidden terms appear (reuses the Memories + Folders + Conversations forbidden-term lists from Stories 2.5 / 2.7 / 3.1 — Story 3.5 adds no new forbidden terms).
    - `ConversationStartSetup_DoesNotEmitTenantIdField` — serializes a fully-populated record; asserts the serialized JSON does NOT contain `tenantId` / `TenantId` substring (FS-8 / SM-3 wire-shape invariant).
    - `ConversationStartSetup_DoesNotEmitAuditFields` — asserts no `createdAt` / `updatedAt` / `sequence` / `setupMetadata` substrings appear.
    - `ConversationStartSetup_LinkedSourcePolicyDefault_IsNone` — `ConversationStartSetup.Empty(...)`.LinkedSourcePolicy is `LinkedSourcePolicy.None`.
    - `ConversationStartSetup_AdditiveDeserialization_TolerantToUnknownFields` — deserializes a JSON payload with an unknown `extraField`; assert the record materializes the known fields and ignores `extraField` (additive serialization tolerance — every event ever produced rule, applied here to the wire DTO).
    - `ConversationStartSetup_PropertyOrder_StableOnWire` — serializes a fully-populated record; assert the property order in the serialized JSON matches the record declaration order (stable wire layout invariant — clients and the OpenAPI schema must agree).

13. **Generated client + idempotency-helper additive coverage.** The regenerated `HexalithProjectsClient.g.cs` exposes a typed `GetConversationStartSetupAsync(projectId, correlationId?, freshness?, cancellationToken)` method returning a typed `ConversationStartSetup`. The regenerated `HexalithProjectsIdempotencyHelpers.g.cs` does NOT gain a new entry for the query (queries have no idempotency surface; same as Story 3.2 / 3.3 / 3.4). Both regenerations are deterministic (LF, NUL-free, no platform-specific paths embedded). The generated Client tests under `tests/Hexalith.Projects.Client.Tests/` gain at least one happy-path test in a new file `tests/Hexalith.Projects.Client.Tests/GetConversationStartSetupClientTests.cs` (mirrors `GetProjectContextClientTests.cs` at lines 34–73 + the Story 3.4 `RefreshProjectContextClientTests.cs` precedent — three substring-based assertions over the regenerated `.g.cs`):
    - `GeneratedClient_ExposesTypedGetConversationStartSetupAsync` — asserts the regenerated file contains `Task<ConversationStartSetup> GetConversationStartSetupAsync` (the new `ConversationStartSetup` partial class is declared by Story 3.5's regeneration — verify it is NOT duplicated; NSwag de-duplicates by name).
    - `GeneratedClient_GetConversationStartSetupOperation_HasNoIdempotencyHelper` — asserts the regenerated `HexalithProjectsIdempotencyHelpers.g.cs` does NOT contain `GetConversationStartSetup` (queries have no idempotency surface).
    - `GeneratedClient_IsLfOnDiskAndNulFree` — copy verbatim from `GetProjectContextClientTests.cs:67–73`.

14. **No edits to Story 3.1 / 3.2 / 3.3 / 3.4 surfaces beyond additive serialization attributes.** `ProjectContextInclusionPolicy`, `ProjectContextInclusionOrder`, `ProjectContextAssemblyContext`, `ProjectContextProjectEvidence`, `ProjectContextTenantAccess`, `ProjectContextReferenceEvidence`, `ProjectContextConversationEvidence`, `ProjectContextDiagnostics`, `ProjectContextOperationKind`, the closed `ProjectContextInclusionDiagnostic` vocabulary, the existing wire DTOs (`ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyResult`, `ProjectContextEvaluation`, `ProjectContextExplanation`), the four assembly enums, the Story 3.4 outcome mappers (`ProjectFolderValidationOutcomeMapper`, `ProjectFileReferenceValidationOutcomeMapper`, `ProjectMemoryValidationOutcomeMapper`), the three Story 3.4 additive ACL methods (`RefreshFolderReferenceAsync`, `RefreshFileReferenceAsync`, `RefreshMemoryReferenceAsync`), `GetProjectContextEndpoint.cs`, `GetProjectContextExplanationEndpoint.cs`, `RefreshProjectContextEndpoint.cs`, `ProjectSetup`, `ConversationStartDefaults`, `LinkedSourcePolicy`, and `ProjectContextSourceKind` are NOT modified. **One exception** (consistent with Story 3.2 / 3.4 precedent): if a `[JsonPropertyName]` attribute is structurally required, the dev agent MAY add the additive attribute under a single Contracts task — but MUST verify by inspection that the existing `JsonNamingPolicy.CamelCase` resolution does not already cover the case. If implementation finds a divergence between the policy and the (now-real) Setup endpoint that requires a substantive Story 3.1 / 3.2 / 3.3 / 3.4 file change (not just an additive serialization attribute), the dev agent **HALTs** before editing and surfaces the conflict in the Dev Agent Record; the resolution is a follow-up story / ADR, not an inline edit. The `ProjectContextConversationsPageSize` constant on the Story 3.2 partial is NOT consumed by Story 3.5 (Story 3.5 fetches no conversation page).

15. **No new shared-vocabulary enum values.** `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ProjectConversationTrustSignal`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus`, `ProjectContextInclusionCheck`, `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextOperationKind`, `ProjectContextInclusionDiagnostic`, `ProjectFolderValidationOutcome`, `ProjectFileReferenceValidationOutcome`, `ProjectMemoryValidationOutcome`, `ProjectContextSourceKind`, `LinkedSourcePolicy` are unchanged. The `GetConversationStartSetup` operation kind already exists (`ProjectContextOperationKind.GetConversationStartSetup`, shipped by Story 3.1 at line 37 of `ProjectContextOperationKind.cs`). If a new value appears genuinely required for Story 3.5, HALT and surface the conflict — the resolution is a follow-up story.

16. **No edits to Stories 1.4–2.7 mutation surfaces, the Story 3.1 policy surface, the Story 3.2 / 3.3 / 3.4 endpoint surfaces, or non-Story-3.5 query surfaces.** No changes to: `ProjectAggregate.*`, `ProjectState`, `ProjectStateApply`, `ProjectCommandValidator`, `ProjectCommandValidationResult`, `ProjectResult`, `ProjectResultCode`, `ProjectDetailProjection`, `ProjectListProjection`, `ProjectReferenceIndexProjection`, the four ACL interfaces' EXISTING methods (`IProjectConversationDirectory`, `IProjectConversationAssignmentDirectory`, `IProjectFolderDirectory.ValidateSetProjectFolderAsync` + Story 3.4's `RefreshFolderReferenceAsync`, `IProjectFileReferenceDirectory.ValidateLinkFileReferenceAsync` + Story 3.4's `RefreshFileReferenceAsync`, `IProjectMemoryDirectory.ValidateLinkMemoryReferenceAsync` + Story 3.4's `RefreshMemoryReferenceAsync`), `IProjectCommandSubmitter`, `ProjectAuthorizationGate`, `ProjectAuthorizationResult`, `ProjectAuthorizationDenialMapper`, `ProjectCommandRejected`, `GetProjectContextEndpoint.cs`, `GetProjectContextExplanationEndpoint.cs`, `RefreshProjectContextEndpoint.cs`, the three Story 3.4 outcome mapper files. **Only** `ProjectsDomainServiceEndpoints` gains the new `GetConversationStartSetupAsync` handler partial-class file + the single `MapGet("/api/v1/projects/{projectId}/setup/conversation-start", ...)` registration. **Only** the additive `ConversationStartSetup` DTO + `ConversationStartSetupProjector` files are NEW.

17. **Mandatory negative-path tests carried forward:**
    - **Cross-tenant isolation** (AC 10) — FS-8 / SM-3.
    - **`NoPayloadLeakage`** over the new DTO + every endpoint response (AC 11 / AC 12 / AC 9 happy-path leakage check) — FS-2.
    - **No clock divergence** — the endpoint uses `TimeProvider.GetUtcNow()` (from DI) for the `Now: DateTimeOffset` passed to the policy. NO `DateTimeOffset.UtcNow` / `DateTime.UtcNow` / `Stopwatch` calls in the handler / projector / mapper code. Validation grep: `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs` returns zero hits.
    - **No-sleep grep in tests** — `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.*/` filtered to Story 3.5 new/modified test files returns zero hits.
    - **Boundary discipline** — `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` returns 0 hits (Story 3.5 imports NONE of the sibling namespaces — it has no `using ConversationTenantId = ...` alias because no conversation page is fetched). `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Projections/ConversationStartSetup/` returns 0 hits (the projector consumes only Contracts types). `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` continues to return zero hits (Story 3.1 invariant; Story 3.5 must not regress it).
    - **OpenAPI fingerprint baseline updated** — the fingerprint gate flips PASSED-with-update only for this story; no future Epic 3 story remains.
    - **Negative-test checklist application** — `docs/checklists/mutation-and-query-negative-tests.md` rows 1 / 4 / 5 / 6 / 8 are explicitly ticked off in the Dev Agent Record for Story 3.5. Rows 2 / 3 / 7 are mutation-only (N/A — Story 3.5 is a query).

18. **`dotnet build` & `dotnet test` budgets:**
    - `dotnet build Hexalith.Projects.slnx` — 0 W / 0 E.
    - `dotnet test Hexalith.Projects.slnx` — baseline 927/927 (post-Story-3.4 review cycle 1 per sprint-status.yaml line 3 — Server.Tests 318, Tests 457, Contracts.Tests 128, Client.Tests 40, Integration.Tests 14; total 927). Story 3.5 grows the count by approximately:
      - Server.Tests: +~17 (the 9 named fixture tests from AC 9 + cross-tenant safe-denial 404 + idempotency rejection + freshness rejection + header echo + malformed projectId Theory + tenant-access unavailable 503 + AuthoritativeTenantId missing 404 + extra-query-parameters tolerance + the AC 11 leakage tests).
      - Projects.Tests: +~11 (the AC 11 projector test file — 11 named tests).
      - Contracts.Tests: +~7 (the AC 12 contracts test file — 7 named tests).
      - Client.Tests: +~3 (new `GetConversationStartSetupClientTests`).
      - Integration.Tests: 0 (no new AppHost smoke for Story 3.5 — same rationale as Story 3.4; the in-process WebApplication-slim host coverage is sufficient).
      Total expected: 927 → ~965 (+38). Failed must be 0. Skipped must be 0 (no AppHost smoke expansion this story).
    - `git diff --check` clean across story-touched files. Hand-written `.cs` / `.md` / `.yaml` are LF on disk per [[build-environment]].

19. **Dev Agent Record** is populated by the dev agent with:
    - Endpoint shape divergence from this AC list (if any) with rationale.
    - The wire body choice: `ConversationStartSetup` (preferred — recorded above) or an alternative shape (e.g. embedding `ConversationStartSetup` inside an envelope carrying `Lifecycle` / `Freshness` separately). Document the chosen shape.
    - The URL shape choice: `/api/v1/projects/{projectId}/setup/conversation-start` (preferred — recorded above) or an alternative (e.g. `/api/v1/projects/{projectId}/conversation-start-setup`). Document the chosen URL.
    - Handler placement (partial-class file under `Queries/` per AC 5 — confirm the path the dev agent used; if a deviation occurred, document the reason).
    - The projector placement: `src/Hexalith.Projects/Projections/ConversationStartSetup/` (preferred — per AR-8 naming) vs. an alternative location. Document the chosen path.
    - The Design Decision (Dev Notes): "Materialize `ConversationStartSetupProjection` (AR-8) as a pure server-side projector over `ProjectDetailItem.Setup` (consumed via the policy's `ProjectContext.Setup` output) rather than a separate event-stream projection over `ProjectCreated` / `ProjectSetupUpdated` / `ProjectArchived` events" — confirm the dev agent followed this decision or document the deviation + rationale (e.g. the dev agent created a separate event-stream projection because a benchmark proved the indirection unacceptable).
    - The empty-reference-evidence decision (AC 5 (i)): confirm the policy was invoked with all four reference-evidence lists empty; if a deviation occurred (e.g. the policy required a non-empty `ProjectFolder` to populate `Lifecycle` correctly), document the deviation + rationale.
    - Per-lane and full-solution test counts (before/after Story 3.5).
    - `dotnet build` warnings/errors, `git diff --check`, `git diff --stat` on `.g.cs` (expected: non-zero — this story DOES regenerate), OpenAPI spine diff size in lines, fingerprint baseline-update note.
    - Negative-test checklist tick-off (rows 1 / 4 / 5 / 6 / 8 of `docs/checklists/mutation-and-query-negative-tests.md`).
    - Any HALT items (e.g. if the policy stopped populating `ProjectContext.Setup` correctly for an `Unauthorized` outer collapse; e.g. if `ConversationStartDefaults` is no longer a nested-record type on `ProjectSetup`).
    - Any single Story 3.1 / 3.2 / 3.3 / 3.4 surface touch (additive `[JsonPropertyName]` etc.) — documented per the Story 3.2 / 3.4 precedent.
    - **Epic 3 completion note** (Story 3.5 is the last Epic 3 story): a Dev Agent Record one-liner naming Story 3.5 as the final Epic 3 story and pointing the Tech Lead / PM at the `epic-3-retrospective` row in `_bmad-output/implementation-artifacts/sprint-status.yaml` (currently `optional`). Story 3.5 does NOT itself transition `epic-3` to `done` — that is the retrospective story's responsibility (or a tech-lead manual transition if the retrospective is skipped).

## Tasks / Subtasks

- [x] **Task 1 — Capability gate + read-only inspection. (AC: 5, 6, 7, 14, 15, 16)**
  - [x] Read `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` lines 75–187 (outer collapses + per-candidate loop + deterministic sort + `ProjectContext` construction at lines 173–185) and confirm: (a) `Assemble(...)` emits one `ProjectContextEvaluation` per candidate kind active in the input — for Story 3.5 the input is empty so zero evaluations are emitted; (b) outer collapses emit `Array.Empty<ProjectContextEvaluation>()`; (c) the assembled `ProjectContext.Setup` is taken verbatim from `detail.Setup` at line 177; (d) the assembled `ProjectContext.Lifecycle` is taken from `detail.Lifecycle` at line 176; (e) the assembled `ProjectContext.ObservedAt` is taken from `context.Now` at line 184 (the policy's `Now` input); (f) the assembled `ProjectContext.Freshness` is taken from `authority.Freshness` at line 185 (the policy's freshness verdict).
  - [x] Confirm `ProjectContextOperationKind.GetConversationStartSetup` exists at `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:37` and `IsReadOnlyOperation(...)` already includes it at `ProjectContextInclusionPolicy.cs:238`.
  - [x] Read `src/Hexalith.Projects.Contracts/Models/ProjectSetup.cs` end-to-end (29 lines) — confirm the five-field shape (`Goals` / `UserInstructions` / `PreferredSourceKinds` / `ExcludedSourceKinds` / `ConversationStartDefaults`) + the `Empty` static. Confirm Story 3.5 surfaces ALL FIVE fields (the first four directly; `ConversationStartDefaults` indirectly via `LinkedSourcePolicy`).
  - [x] Read `src/Hexalith.Projects.Contracts/Models/ConversationStartDefaults.cs` (12 lines) — confirm it carries ONLY `LinkedSourcePolicy` in v1. Story 3.5's wire DTO surfaces ONLY `LinkedSourcePolicy` (not the whole `ConversationStartDefaults` record — the wire shape is flatter for Chatbot ergonomics).
  - [x] Read `src/Hexalith.Projects.Contracts/Models/LinkedSourcePolicy.cs` (37 lines) — confirm the closed v1 vocabulary (`None` / `ProjectsOwnedMetadataOnly` / `AuthorizedReferences`) and the `LinkedSourcePolicyJsonConverter` that emits lower-camelCase strings.
  - [x] Read `src/Hexalith.Projects.Contracts/Models/ProjectContextSourceKind.cs` — confirm the shape (the dev agent must know whether to reuse the enum's existing JSON converter; if no converter is declared, the projector emits the enum via the default JSON behavior — recommend keeping consistent with `LinkedSourcePolicy`'s lower-camelCase pattern).
  - [x] Read `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs` lines 14–155 (the full record) — confirm: (a) `Setup` is the nullable `ProjectSetup?` field at line 50; (b) `Lifecycle` / `ObservedAt` / `Freshness` are surfaced as wire fields; (c) `TenantId` is `[JsonIgnore]`-marked at line 67 (Story 3.2 / FS-8 / SM-3 pattern Story 3.5's new DTO MUST mirror — see AC 2 note "NO TenantId on wire body").
  - [x] Read `src/Hexalith.Projects.Contracts/Ui/ProjectLifecycle.cs` and `src/Hexalith.Projects.Contracts/Ui/ProjectContextFreshness.cs` — confirm the enum shape + JSON converter pattern Story 3.5's wire DTO consumes.
  - [x] Read `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` (45 lines) — confirm `Setup` is the nullable `ProjectSetup?` field at line 38; `Lifecycle` is the `ProjectLifecycle` field at line 42; `SetupMetadata` is the legacy `string?` field at line 37 (NOT surfaced by Story 3.5 — it is internal audit metadata; the Dev Notes Design Decision confirms this exclusion).
  - [x] Read `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs` end-to-end (142 lines) — confirm the partial-class pattern, the canonical handler flow lines 62–139, and the `Idempotency-Key` / freshness / `TenantAccessResult` null-collapse patterns Story 3.5 copies. The `ProjectContextConversationsPageSize` constant (line 50) is NOT consumed by Story 3.5 (no conversation page fetched).
  - [x] Read `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` end-to-end (226 lines) — confirm the Story 3.4 sibling partial-class pattern Story 3.5 mirrors at file-shape level (the partial-class `using` block, the file-scoped namespace, the XML doc structure adapted for Story 3.5).
  - [x] Read `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs` end-to-end — confirm the Story 3.3 sibling partial-class pattern.
  - [x] Read `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` lines 71–395 — confirm: (a) the existing `ConfigureEndpoints` method registers `MapGet("/api/v1/projects/{projectId}/context", ...)` at line 115 (Story 3.2), `MapGet("/api/v1/projects/{projectId}/context/explain", ...)` at line 135 (Story 3.3), `MapGet("/api/v1/projects/{projectId}/context/refresh", ...)` at line 155 (Story 3.4); (b) the static helpers `ReadHeader` / `HasHeader` / `IsCanonicalIdentifier` / `SafeDenial` / `ReadModelUnavailable` / `ValidationProblem` / `FreshnessHeaderName` / `EventuallyConsistent` / `ResponseJsonOptions` are defined on the partial class and visible to the new handler.
  - [x] Read `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationResult.cs` and `ProjectAuthorizationGate.cs` — confirm `TenantAccessResult: TenantAccessAuthorizationResult?` is populated on every Allowed path (Story 3.2 deliverable); the defensive null-collapse at `GetProjectContextEndpoint.cs:94–99` and `RefreshProjectContextEndpoint.cs:109–114` is the canonical pattern Story 3.5 carries forward verbatim.
  - [x] Read `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs` — confirm: (a) `AddProjectsModule()` registers `ProjectContextInclusionPolicy` via `services.TryAddTransient<ProjectContextInclusionPolicy>()` at line 35; (b) `AddProjectsServer()` calls `AddProjectsModule()` (Story 3.2 fix) so the policy is DI-visible to the new handler. No DI change is needed for Story 3.5 — the projector is a static class consumed directly; the policy is already registered.
  - [x] Read `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` lines 284–480 (`GET /context` operation block — Story 3.2 + `GET /context/explain` operation block — Story 3.3 + `GET /context/refresh` operation block — Story 3.4) and the canonical-error-categories block at line 471–480. Identify the canonical placement for the new operation block (immediately after line 480, before the existing `/api/v1/projects/{projectId}/conversations/{conversationId}/link` block at line 481).
  - [x] Read `tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs` end-to-end — confirm the Story 3.4 fixture pattern Story 3.5 mirrors. Note the stubs Story 3.4 defines (`StubFolderDirectory`, `StubMemoryDirectory`, `RecordingFolderDirectory`, `RecordingMemoryDirectory`) — Story 3.5 reuses the `Recording*` shapes for AC 9 `GetConversationStartSetup_DoesNotCallSiblingAcls` but introduces NO new stub conversation/folder/file/memory implementations (Story 3.5 does not consume the sibling ACL interfaces at all — the `Recording*` directories exist only to PROVE no calls happen via DI-overridden injection).
  - [x] Read `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs` end-to-end — confirm the canonical `StartAppAsync(...)` builder pattern (Story 3.2 / 3.3 / 3.4 baseline) Story 3.5 reuses.
  - [x] Read `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` — confirm: (a) the existing `ProjectContext_SerializesMetadataOnly` block (Story 3.1 / 3.2 / 3.3 / 3.4 extension); (b) the forbidden-term lists used in `NoPayloadLeakageAssertions.AssertNoLeakage(...)` — Story 3.5 adds NO new forbidden terms.
  - [x] Read `tests/Hexalith.Projects.Client.Tests/GetProjectContextClientTests.cs` end-to-end — confirm the substring-based regenerated-`.g.cs` assertion pattern Story 3.5 mirrors. Read `tests/Hexalith.Projects.Client.Tests/RefreshProjectContextClientTests.cs` (Story 3.4) for the most recent precedent.
  - [x] Read `docs/checklists/mutation-and-query-negative-tests.md` (Story 3.2 / Action Item 7 deliverable) — confirm the 8 rows and the query-side applicability (rows 1 / 4 / 5 / 6 / 8 apply to Story 3.5; rows 2 / 3 / 7 are mutation-only).
  - [x] Read `docs/context-assembly-decision-matrix.md` lines 16–27 + 34–50 — confirm: (a) the `GetConversationStartSetup (3.5)` column (line 16, fifth operation) is identical to the `Get` column for every evidence-state row; (b) the outer-override rows (34–42) apply identically — `Unauthorized` and `ProjectUnavailable` outer outcomes collapse to HTTP 404; archived project surfaces `Assembled` with `Lifecycle: archived`. Story 3.5 does NOT add or modify any matrix column. The matrix's per-reference rows (Memories / Folder / File / Conversation) are NOT surfaced on Story 3.5's wire body — they remain owned by the Story 3.2 / 3.3 / 3.4 endpoint tests.
  - [x] Confirm no submodule pointer change is required and no nested-recursive submodule init is needed (current `git status` shows pre-existing `Hexalith.Commons` / `Hexalith.Conversations` / `Hexalith.Parties` "modified content" markers from prior sessions — unrelated to Story 3.5 and must not be advanced).
  - [x] **HALT** before proceeding to Task 2 if any of the above evidence diverges from this story file's assumptions — especially: (i) `ProjectContextOperationKind.GetConversationStartSetup` was somehow removed or moved off the read-only allowlist; (ii) the policy's outer-collapse paths no longer emit a `ProjectContext` with `Setup` / `Lifecycle` / `ObservedAt` / `Freshness` correctly populated on the `Assembled` path; (iii) `ProjectContext.Setup` no longer exists as a nullable `ProjectSetup?` field; (iv) `ProjectSetup.ConversationStartDefaults?.LinkedSourcePolicy` is no longer the source of the default linked-source policy; (v) a Story 3.1 / 3.2 / 3.3 / 3.4 file would have to change to make Story 3.5 work beyond an additive `[JsonPropertyName]`.

- [x] **Task 2 — Add the `ConversationStartSetup` wire DTO. (AC: 2, 12, 14, 15)**
  - [x] Create `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` — sealed positional record per AC 2's exact shape. XML doc: explicitly name Story 3.5 / FR-20 / AR-8 / AR-9; document the field-by-field provenance (every field's source); document the wire-shape invariants (no `TenantId`, no audit metadata, no reference inventory); document the `Empty` / `FromContext` static factories.
  - [x] Confirm the record's namespace is `Hexalith.Projects.Contracts.Models` (mirrors `ProjectContext`, `ProjectSetup`, `ConversationStartDefaults`, `LinkedSourcePolicy`).
  - [x] Implement `ConversationStartSetup.Empty(string projectId, ProjectLifecycle lifecycle, DateTimeOffset observedAt, ProjectContextFreshness freshness)` — returns a record with empty arrays + `LinkedSourcePolicy.None`.
  - [x] Implement `ConversationStartSetup.FromContext(ProjectContext context)` — the canonical mapper:
    ```
    public static ConversationStartSetup FromContext(ProjectContext context)
    {
        ProjectSetup? setup = context.Setup;
        LinkedSourcePolicy linkedSource = setup?.ConversationStartDefaults?.LinkedSourcePolicy ?? LinkedSourcePolicy.None;
        return new ConversationStartSetup(
            ProjectId: context.ProjectId,
            Lifecycle: context.Lifecycle,
            Goals: setup?.Goals ?? Array.Empty<string>(),
            UserInstructions: setup?.UserInstructions ?? Array.Empty<string>(),
            PreferredSourceKinds: setup?.PreferredSourceKinds ?? Array.Empty<ProjectContextSourceKind>(),
            ExcludedSourceKinds: setup?.ExcludedSourceKinds ?? Array.Empty<ProjectContextSourceKind>(),
            LinkedSourcePolicy: linkedSource,
            ObservedAt: context.ObservedAt,
            Freshness: context.Freshness);
    }
    ```
  - [x] Boundary check: `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` returns 0 hits.
  - [x] Boundary check: the new file imports ONLY `System` (for `DateTimeOffset` / `Array`), `System.Collections.Generic` (for `IReadOnlyList<T>`), and `Hexalith.Projects.Contracts.Ui` (for `LinkedSourcePolicy` / `ProjectLifecycle` / `ProjectContextFreshness` if those are in Ui; otherwise `Hexalith.Projects.Contracts.Models` for `LinkedSourcePolicy`). The dev agent verifies the exact namespace of each type by inspection before writing the `using` block.
  - [x] Run a targeted compile check: `dotnet build src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` — confirm 0 W / 0 E.

- [x] **Task 3 — Add the `ConversationStartSetupProjector` (pure projector). (AC: 3, 11, 14, 15, 17)**
  - [x] Create the directory `src/Hexalith.Projects/Projections/ConversationStartSetup/` (matches the AR-8 naming `ConversationStartSetupProjection` while avoiding collision with the existing `ProjectDetail` / `ProjectList` / `ProjectReferenceIndex` / `TenantAccess` directory siblings).
  - [x] Create `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs` — `public static class` with a single `public static ConversationStartSetup Project(ProjectContext context)` method. The body delegates to `ConversationStartSetup.FromContext(context)` (single source of truth — the contract DTO's static factory IS the projection function; the projector wrapper exists so the AR-8 named projection has a concrete class under `Hexalith.Projects/Projections/` per AR-8 + architecture line 527).
  - [x] XML doc: explicitly name Story 3.5 / FR-20 / AR-8 (`ConversationStartSetupProjection` named projection) / AR-9 (the policy is upstream); document the Design Decision (this projection is realized as a pure server-side projector over `ProjectContext.Setup` rather than a separate event-stream projection — rationale in Dev Notes / Design Decision).
  - [x] Boundary check: `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Projections/ConversationStartSetup/` returns 0 hits.
  - [x] Boundary check: `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs` returns 0 hits.
  - [x] No DI registration needed — the projector is a static class consumed directly by the endpoint handler.
  - [x] Run a targeted compile check: `dotnet build src/Hexalith.Projects/Hexalith.Projects.csproj` — 0 W / 0 E.

- [x] **Task 4 — Extend the OpenAPI spine with the GetConversationStartSetup operation + schema + example. (AC: 1, 4)**
  - [x] Open `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`.
  - [x] Add the path entry `/api/v1/projects/{projectId}/setup/conversation-start` (GET) immediately after the existing `/api/v1/projects/{projectId}/context/refresh` operation block (lines 414–480), copying the operation block verbatim and changing only: `operationId` → `GetConversationStartSetup`; `summary` → "Retrieve the bounded subset of Project Setup needed to start or resume a conversation."; `description` to reflect Story 3.5's FR-20 wording ("Return the subset of setup needed to start/resume a conversation (goals, instructions, context preferences, default linked-source policy). Excludes internal audit metadata and unavailable/unauthorized references; stable enough to use without re-querying every bounded context first.") and explicitly name the absence of sibling-ACL fetch on the fast path; the `200` response schema `$ref` → `#/components/schemas/ConversationStartSetup` (NEW schema); the example `$ref` → `#/components/examples/ConversationStartSetup` (NEW example); the `x-hexalith-canonical-error-categories` list matches `GetProjectContext`'s 8-row set EXACTLY (it does NOT include `referenced_resource_unavailable` because Story 3.5 never calls a sibling — the dev agent verifies this difference from Story 3.4).
  - [x] Add a new schema under `components.schemas.ConversationStartSetup` defining every field per AC 2: `projectId` (string), `lifecycle` (`$ref ProjectLifecycle`), `goals` (`array<string>`), `userInstructions` (`array<string>`), `preferredSourceKinds` (`array<$ref ProjectContextSourceKind>`), `excludedSourceKinds` (`array<$ref ProjectContextSourceKind>`), `linkedSourcePolicy` (`$ref LinkedSourcePolicy`), `observedAt` (`string` format `date-time`), `freshness` (`$ref ProjectContextFreshness`). All fields required (`required: [projectId, lifecycle, goals, userInstructions, preferredSourceKinds, excludedSourceKinds, linkedSourcePolicy, observedAt, freshness]`). The schema is `type: object`, `additionalProperties: false` (closed wire-shape — clients cannot inject unknown fields).
  - [x] Add a new example under `components.examples.ConversationStartSetup` showing a populated record with safe metadata strings (e.g. `Goals: ["Generate weekly status reports", "Identify project risks"]`, `UserInstructions: ["Use concise language", "Reference Project ID in headers"]`). The example MUST NOT contain path-like / token-like / payload-like content (the `OpenApiContractSpineTests.Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` gate will verify this).
  - [x] OPTIONALLY add a second example under `components.examples.ConversationStartSetupEmpty` showing the empty-setup happy path (all arrays empty, `LinkedSourcePolicy: none`, `Lifecycle: active`, `Freshness: fresh`).
  - [x] Verify YAML is well-formed by running the existing `OpenApiContractSpineTests` lane (`dotnet test tests/Hexalith.Projects.Contracts.Tests/`) — confirm `Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` PASSES.
  - [x] Regenerate `HexalithProjectsClient.g.cs` via the MSBuild target: `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj`. Confirm the new `GetConversationStartSetupAsync(...)` method appears with signature `Task<ConversationStartSetup> GetConversationStartSetupAsync(string projectId, System.Guid? correlationId, string? freshness, CancellationToken cancellationToken)` (mirror Story 3.2 / 3.4's typed signature). Confirm the new `ConversationStartSetup` partial class is generated exactly once.
  - [x] Confirm `HexalithProjectsIdempotencyHelpers.g.cs` is unchanged except for the SHA256 fingerprint constants (no idempotency surface for queries; same as Story 3.2 / 3.3 / 3.4).
  - [x] Update the OpenAPI fingerprint baseline file (if present) and confirm the fingerprint gate flips PASSED-with-update for this story.
  - [x] Run `git diff --check` and confirm clean across the spine, `.g.cs` (expected non-zero diff), and the fingerprint baseline.

- [x] **Task 5 — Implement the `GetConversationStartSetupAsync` HTTP handler. (AC: 5, 6, 7, 17)**
  - [x] Create `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` as a new partial-class file for `ProjectsDomainServiceEndpoints` (mirror the Story 3.2 / 3.3 / 3.4 partial-class pattern). Copy the Story 3.2 file's file-scoped namespace + `using` declarations (REMOVE the `using ConversationTenantId = ...` alias and the conversation-related `using`s — Story 3.5 fetches no conversation page; ADD `using Hexalith.Projects.Projections.ConversationStartSetup;` for the projector).
  - [x] Implement `private static async Task<IResult> GetConversationStartSetupAsync(string projectId, HttpContext httpContext, IProjectTenantContextAccessor tenantContext, ProjectAuthorizationGate authorizationGate, ProjectContextInclusionPolicy contextPolicy, TimeProvider timeProvider, CancellationToken cancellationToken)` — a strict subset of Story 3.2's handler signature (no `IProjectConversationDirectory`, no `IProjectFolderDirectory`, no `IProjectFileReferenceDirectory`, no `IProjectMemoryDirectory`).
  - [x] Copy the Story 3.2 handler body lines 62–101 (header parsing, envelope validation, authz, idempotency rejection, freshness rejection, defensive null-collapse, projection-detail fetch) verbatim with the following adaptations:
    - REPLACE the `ProjectConversationsPage` fetch + the `ProjectContextConversationEvidenceMapper.Map(...)` call with no-ops (Story 3.5 does NOT fetch conversations).
    - REPLACE the `ProjectContextReferenceEvidence(...)` construction with an empty-references-evidence object: `new ProjectContextReferenceEvidence(ProjectFolder: null, FileReferences: Array.Empty<ProjectFileReference>(), MemoryReferences: Array.Empty<ProjectMemoryReference>(), Conversations: Array.Empty<ProjectContextConversationEvidence>())`.
    - CHANGE the `ProjectContextAssemblyContext.OperationKind` to `ProjectContextOperationKind.GetConversationStartSetup`.
    - After `contextPolicy.Assemble(...)` returns the `ProjectContextAssemblyResult`, branch on `assembled.Context.AssemblyOutcome`:
      ```
      if (assembled.Context.AssemblyOutcome == ProjectContextAssemblyOutcome.Unauthorized
          || assembled.Context.AssemblyOutcome == ProjectContextAssemblyOutcome.ProjectUnavailable)
      {
          return SafeDenial(correlationId, null);
      }

      ConversationStartSetup body = ConversationStartSetupProjector.Project(assembled.Context);

      if (!string.IsNullOrWhiteSpace(correlationId))
      {
          httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
      }

      httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
      return Results.Json(body, ResponseJsonOptions);
      ```
  - [x] Register the route in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` `ConfigureEndpoints` method by adding a new `endpoints.MapGet("/api/v1/projects/{projectId}/setup/conversation-start", static async (string projectId, HttpContext httpContext, IProjectTenantContextAccessor tenantContext, ProjectAuthorizationGate authorizationGate, ProjectContextInclusionPolicy contextPolicy, TimeProvider timeProvider, CancellationToken cancellationToken) => await GetConversationStartSetupAsync(projectId, httpContext, tenantContext, authorizationGate, contextPolicy, timeProvider, cancellationToken).ConfigureAwait(false)).WithName("GetConversationStartSetup");` block immediately after the existing `/api/v1/projects/{projectId}/context/refresh` registration at line 155–177. Mirror the parameter-binding shape of the existing block (the lambda forwards a strict subset of DI-injected arguments — no `IProjectConversationDirectory`, no folder/file/memory directories).
  - [x] Boundary check: `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` returns 0 hits (Story 3.5 imports NONE of the sibling namespaces — there is no `using ConversationTenantId = ...` alias because no conversation page is fetched).

- [x] **Task 6 — Add Tier-2 endpoint tests. (AC: 8, 9, 10, 17)**
  - [x] Create `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs` mirroring the Story 3.2 / 3.3 / 3.4 fixture pattern. Reuse the `StartAppAsync(...)` named-fixture builder shape and the same stub classes (`FixedProjectTenantContext`, `NoopProjectCommandSubmitter`, `StubProjectDetailReadModel`, `ThrowingTenantAccessProjectionStore`). Story 3.5 does NOT register any conversation/folder/file/memory directory stubs in the happy-path host (the handler has no such dependencies); for the AC 9 `GetConversationStartSetup_DoesNotCallSiblingAcls` test, optionally register `RecordingConversationDirectory` / `RecordingFolderDirectory` / `RecordingFileReferenceDirectory` / `RecordingMemoryDirectory` in the test host's DI container — assert their `CallCount == 0` after a happy-path call.
  - [x] Required named-fixture tests per AC 8 (outer-collapse cells — Story 3.5 surfaces no per-reference state so per-reference cells are owned by Stories 3.2 / 3.3 / 3.4):
    - `GetConversationStartSetup_HappyPath_Returns200WithBoundedSubset` (AC 9).
    - `GetConversationStartSetup_NullSetup_ReturnsEmptyBoundedSubset` (AC 9).
    - `GetConversationStartSetup_ArchivedProject_Returns200WithLifecycleArchived` (AC 9).
    - `GetConversationStartSetup_ConversationStartDefaultsMissing_DefaultsToLinkedSourcePolicyNone` (AC 9).
    - `GetConversationStartSetup_PreferredAndExcludedSourceKinds_PreserveOrder` (AC 9).
    - `GetConversationStartSetup_IdempotencyKeyPresent_ReturnsValidationProblem` (AC 17 row 4).
    - `GetConversationStartSetup_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404` (ordering proof — authorize first → idempotency-key check second; mirror `GetProjectContext_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404`).
    - `GetConversationStartSetup_StricterFreshnessRequested_ReturnsValidationProblem` (AC 17 row 5).
    - `GetConversationStartSetup_MalformedProjectId_ReturnsSafeDenial404` (`[Theory]` over whitespace, NUL, control bytes, unicode bidi, `..`, leading/trailing whitespace) (AC 17 row 1).
    - `GetConversationStartSetup_CrossTenant_ReturnsSafeDenial404` (AC 10) — asserts `body.ShouldNotContain("tenant-b")` AND that the response is a Problem Details body (NOT a `ConversationStartSetup` JSON body).
    - `GetConversationStartSetup_TenantAccessUnavailable_ReturnsReadModelUnavailable503` (AC 17 row 8).
    - `GetConversationStartSetup_AuthoritativeTenantIdMissing_ReturnsSafeDenial404`.
    - `GetConversationStartSetup_ResponseHeaders_HaveCorrelationAndFreshness` — asserts `X-Correlation-Id` is echoed when a canonical correlation header is supplied and `X-Hexalith-Freshness: eventually_consistent` is always set on the 200 response.
    - `GetConversationStartSetup_ExtraQueryParameters_AreIgnoredNotFailed` (`?expand=full` etc.) — Story 3.5 ignores unknown query parameters per the carry-forward `GetProject` / `GetProjectContext` / `RefreshProjectContext` precedent.
    - `GetConversationStartSetup_BodyDoesNotContainTenantId` (AC 9).
    - `GetConversationStartSetup_BodyDoesNotContainAuditMetadata` (AC 9).
    - `GetConversationStartSetup_BodyDoesNotContainReferenceInventory` (AC 9).
    - `GetConversationStartSetup_DoesNotCallSiblingAcls` (AC 9) — registers `RecordingConversationDirectory` / `RecordingFolderDirectory` / `RecordingFileReferenceDirectory` / `RecordingMemoryDirectory` in the test host's DI container even though the handler does NOT declare them as dependencies; the assertion is that on a happy-path 200 response, every recording directory's `CallCount == 0`. This is a "by construction" proof — the handler signature itself proves no sibling call is possible because the dependencies are absent, but the test confirms no future code-path can introduce a regression silently (a future dev who adds an `IProjectConversationDirectory` injection to the handler will see this test fail).
    - `GetConversationStartSetup_ResponseBody_HasNoLeakage` — boots the in-process WebApplication-slim host (mirroring the `StartAppAsync(...)` builder), exercises three labelled outcomes (`HappyPath`, `ArchivedProject`, `NullSetup`), and runs `NoPayloadLeakageAssertions.AssertNoLeakage(...)` over every serialized response body.
    - `GetConversationStartSetup_ErrorResponses_HaveNoLeakage` — exercises 400 / 401 / 403 / 404 / 503 responses and asserts no diagnostic-message leakage, no payload fragments, no token / path appears in the ProblemDetails body.
  - [x] All tests use `RecordingLogger<T>` from `src/Hexalith.Projects.Testing/Context/` (Story 3.1) for any policy logger assertions.
  - [x] Boundary discipline: `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs` returns 0 hits.
  - [x] Use `TestContext.Current.CancellationToken` (xUnit v3 pattern; mirror Story 3.2 / 3.3 / 3.4 test files).

- [x] **Task 7 — Add Tier-1 projector tests. (AC: 11)**
  - [x] Create `tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/ConversationStartSetupProjectorTests.cs` — pure xUnit v3 + Shouldly tests over `ConversationStartSetupProjector.Project(...)`. Implement all 11 tests listed in AC 11.
  - [x] Boundary check: the test file imports ONLY `Hexalith.Projects.Projections.ConversationStartSetup` (the projector), `Hexalith.Projects.Contracts.Models` / `Hexalith.Projects.Contracts.Ui` (the DTOs and enums). It does NOT import `Hexalith.Projects.Server.*` (Tier-1 purity boundary).
  - [x] Boundary check: `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/ConversationStartSetupProjectorTests.cs` returns 0 hits.

- [x] **Task 8 — Add Tier-0 Contracts tests for `ConversationStartSetup`. (AC: 12)**
  - [x] Create `tests/Hexalith.Projects.Contracts.Tests/Models/ConversationStartSetupTests.cs` — pure xUnit Contracts tests covering all 7 tests listed in AC 12.
  - [x] If a `NoPayloadLeakageAssertions` Tier-0 harness extension is required for the new DTO, add it to the existing harness in `tests/Hexalith.Projects.Contracts.Tests/` (additive coverage following the Story 3.1 / 3.2 / 3.3 / 3.4 pattern). The forbidden-term lists are reused unchanged.
  - [x] Confirm the new DTO serializes with stable property order (lower-camelCase by the default `JsonNamingPolicy.CamelCase`; the closed-vocabulary `LinkedSourcePolicy` continues to emit via its existing `LinkedSourcePolicyJsonConverter`; the `ProjectContextSourceKind` / `ProjectLifecycle` / `ProjectContextFreshness` enums emit per their existing converters).

- [x] **Task 9 — Add a typed-client happy-path test. (AC: 13)**
  - [x] Create `tests/Hexalith.Projects.Client.Tests/GetConversationStartSetupClientTests.cs` — three substring-based tests over the regenerated `HexalithProjectsClient.g.cs` (mirror `GetProjectContextClientTests.cs` lines 34–73 and the Story 3.4 `RefreshProjectContextClientTests.cs` precedent).
  - [x] `GeneratedClient_ExposesTypedGetConversationStartSetupAsync` — asserts the regenerated file contains `Task<ConversationStartSetup> GetConversationStartSetupAsync`. Verify the `ConversationStartSetup` partial class is declared exactly once (NSwag deduplicates by name; the new schema produces exactly one generated partial).
  - [x] `GeneratedClient_GetConversationStartSetupOperation_HasNoIdempotencyHelper` — opens the regenerated `HexalithProjectsIdempotencyHelpers.g.cs` and asserts it does NOT contain `GetConversationStartSetup` — proves no idempotency-fingerprint surface was added (queries are idempotency-free per AC 1).
  - [x] `GeneratedClient_IsLfOnDiskAndNulFree` — copy verbatim from `GetProjectContextClientTests.cs:67–73`.
  - [x] Path-resolution helper: copy `LocateRepositoryRoot()` from `GetProjectContextClientTests.cs:75` (or extract to a shared helper if a pattern emerges; preferred: leave inline per the Story 3.2 / 3.3 / 3.4 precedent).

- [x] **Task 10 — Apply the negative-test checklist. (AC: 17)**
  - [x] In the Dev Agent Record, explicitly tick off rows 1 / 4 / 5 / 6 / 8 of `docs/checklists/mutation-and-query-negative-tests.md` for the GetConversationStartSetup endpoint:
    - Row 1 (Malformed identifier → safe-denial 404): covered by `GetConversationStartSetup_MalformedProjectId_ReturnsSafeDenial404`.
    - Row 4 (Idempotency-Key PRESENT on query → 400 after authz): covered by `GetConversationStartSetup_IdempotencyKeyPresent_ReturnsValidationProblem` + `..._IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404`.
    - Row 5 (Stricter `X-Hexalith-Freshness` → 400): covered by `GetConversationStartSetup_StricterFreshnessRequested_ReturnsValidationProblem`.
    - Row 6 (Cross-tenant safe-denial 404): covered by `GetConversationStartSetup_CrossTenant_ReturnsSafeDenial404` AND the by-construction no-sibling-ACL assertion (AC 9 `..._DoesNotCallSiblingAcls`).
    - Row 8 (`ReferenceState.Unavailable && Retryable` → 503 ReadModelUnavailable): covered by `GetConversationStartSetup_TenantAccessUnavailable_ReturnsReadModelUnavailable503`.
  - [x] Rows 2 / 3 / 7 are mutation-only (route/body identity mismatch, missing Idempotency-Key on mutation, unknown Idempotency-Key retry conflict) — explicitly mark N/A in the Dev Agent Record.

- [x] **Task 11 — Validation. (AC: 17, 18, 19)**
  - [x] Use the build environment from [[build-environment]]: `DOTNET_ROOT=/home/administrator/.dotnet` (`dotnet --version` 10.0.300). Avoid `/usr/bin/dotnet`.
  - [x] Run `dotnet build Hexalith.Projects.slnx`. Confirm 0 W / 0 E.
  - [x] Run focused lanes:
    - `dotnet test tests/Hexalith.Projects.Tests` (baseline 457 + ~11 = ~468).
    - `dotnet test tests/Hexalith.Projects.Server.Tests` (baseline 318 + ~17 = ~335).
    - `dotnet test tests/Hexalith.Projects.Contracts.Tests` (baseline 128 + ~7 = ~135).
    - `dotnet test tests/Hexalith.Projects.Client.Tests` (baseline 40 + ~3 = ~43).
    - `dotnet test tests/Hexalith.Projects.Integration.Tests` (baseline 14 + 0 = 14).
  - [x] Run full-solution `dotnet test Hexalith.Projects.slnx`. Baseline 927; Story 3.5 grows it by approximately +38 (Server +~17, Projects +~11, Contracts +~7, Client +~3); failed must be 0; skipped must be 0.
  - [x] Run `git diff --check` on story-touched files. Confirm clean across `.cs`, `.md`, `.yaml`, `.csproj`.
  - [x] Confirm `.g.cs` regenerated cleanly: `git diff --stat src/Hexalith.Projects.Client/Generated/` shows non-zero changed lines (expected — this story regenerates because Story 3.5 adds an operation AND a new schema); inspect that the new `GetConversationStartSetupAsync` method is present, that the new `ConversationStartSetup` partial class is declared once (no duplicate), and that no Windows backslashes leak into the file.
  - [x] Confirm the OpenAPI fingerprint baseline updated and the spine fingerprint gate is PASSED-with-baseline-update (allowed for this story).
  - [x] Confirm boundary greps pass:
    - `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` → 0 hits (Story 3.1 invariant).
    - `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Projections/ConversationStartSetup/` → 0 hits.
    - `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` → 0 hits.
    - `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` → 0 hits.
    - `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/ConversationStartSetupProjectorTests.cs tests/Hexalith.Projects.Contracts.Tests/Models/ConversationStartSetupTests.cs tests/Hexalith.Projects.Client.Tests/GetConversationStartSetupClientTests.cs` → 0 hits.
  - [x] Confirm no submodule pointer change: `git status` shows no submodule advances beyond the pre-existing "modified content" markers (Hexalith.Commons / Hexalith.Conversations / Hexalith.Parties were already in that state at session start per the initial `git status` baseline).
  - [x] Populate the Dev Agent Record with the validation summary per AC 19 — including the Epic 3 completion one-liner.

## Dev Notes

### Story Scope Boundary

- **In scope:** `GET /api/v1/projects/{projectId}/setup/conversation-start` endpoint (new partial-class file `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` + a single `MapGet(...)` registration in `ProjectsDomainServiceEndpoints.cs`); OpenAPI spine entry + ONE new schema (`ConversationStartSetup`) + ONE new example; regenerated `HexalithProjectsClient.g.cs` exposing `GetConversationStartSetupAsync(...)`; ONE new wire DTO `ConversationStartSetup` (`src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs`); ONE new pure projector `ConversationStartSetupProjector` (`src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs`); Tier-2 Server endpoint tests (outer-collapse cells, bounded-subset content assertions, idempotency rejection, route negatives, cross-tenant safe-denial + by-construction no-sibling-ACL assertion, header echo, leakage); Tier-1 projector purity tests (11 named tests); Tier-0 Contracts tests for the new DTO (7 named tests); client typed-method substring assertion test; checklist tick-off in Dev Agent Record (rows 1 / 4 / 5 / 6 / 8 of `docs/checklists/mutation-and-query-negative-tests.md`); Epic 3 completion one-liner in Dev Agent Record.
- **Explicitly out of scope (recorded so the dev agent does not over-build):** any new endpoint other than the GetConversationStartSetup endpoint; any new shared-vocabulary enum value; any new `ProjectContextInclusionDiagnostic` vocabulary entry; any edit to `ProjectContextInclusionPolicy` / `ProjectContextInclusionOrder` / Story 3.1 DTOs beyond an additive `[JsonPropertyName]` if structurally required; any new mutation endpoint; any change to `ProjectAggregate.*` / `ProjectState` / `ProjectStateApply` / `ProjectCommandValidator` / projections (other than the additive `ConversationStartSetupProjector`) / Story 2.x ACL interfaces (the existing mutation-validation methods AND the Story 3.4 additive `RefreshXxxAsync` methods) / `IProjectCommandSubmitter`; modifying any Story 3.4 outcome mapper or its tests; the U+2028/U+2029 canonicaliser hardening (Epic 2 retro Action Item 2 — applies to the next mutation surface, not Story 3.5); any conversation page fetch on this endpoint; any sibling ACL recheck on this endpoint; any new ADR; modifying `ProjectContextAssemblyResult` / `ProjectContextExplanation` (the Story 3.3 wrapper — not used by Story 3.5); modifying the Story 3.2 `GetProjectContextEndpoint.cs` or the Story 3.3 `GetProjectContextExplanationEndpoint.cs` or the Story 3.4 `RefreshProjectContextEndpoint.cs` files; modifying the decision-matrix doc (Story 3.1 owns its cell semantics; the matrix already has a `GetConversationStartSetup (3.5)` column that is identical to `Get`); adding an AppHost smoke test (the in-process WebApplication-slim Tier-2 endpoint tests are sufficient for Story 3.5's surface); modifying `docs/checklists/mutation-and-query-negative-tests.md` rows (the 8-row canonical checklist stays unchanged; Story 3.5 only adds one operation block to the spine that references the existing categories); creating a separate event-stream `ConversationStartSetupProjection` over `ProjectCreated` / `ProjectSetupUpdated` / `ProjectArchived` events (see Design Decision); the `epic-3` status promotion (the `epic-3-retrospective` story or a tech-lead manual transition owns that — Story 3.5's `done` status does NOT auto-promote `epic-3`).

### Design Decision: ConversationStartSetupProjection as a pure projector

**Decision:** Materialize the AR-8 named `ConversationStartSetupProjection` as a pure server-side projector (`ConversationStartSetupProjector` static class) that takes the policy's `ProjectContext` and projects it to the wire `ConversationStartSetup` DTO — rather than as a separate EventStore-fed projection over `ProjectCreated` / `ProjectSetupUpdated` / `ProjectArchived` events parallel to `ProjectDetailProjection`.

**Rationale:**
- `ProjectDetailItem.Setup` already projects every `ProjectSetupUpdated.Setup` payload onto a typed `ProjectSetup?` field at `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs:38` — verified at Task 1 inspection. Building a parallel event-stream projection that re-projects the same field is pure duplication: it would re-consume the same `ProjectProjectionEnvelope` stream, re-apply the same `ProjectCreated` → empty-setup / `ProjectSetupUpdated` → setup-replace / `ProjectArchived` → lifecycle-archived fold, and write its own `IReadOnlyDictionary<string, ConversationStartSetupItem>` keyed by the same canonical identity. Two projections would have to be rebuilt together, kept consistent together, and tested together — without any new information surfaced.
- Stories 3.2 / 3.3 / 3.4 all consume `ProjectDetailItem.Setup` indirectly via `ProjectContext.Setup` (the policy reads `detail.Setup` at `ProjectContextInclusionPolicy.cs:177` and passes it through unchanged to the assembled `ProjectContext` record). Story 3.5 continues this pattern — the policy is the single read-path for `ProjectSetup`, and `ConversationStartSetupProjector` is a pure projection of `ProjectContext.Setup` (subset + flatten of `ConversationStartDefaults.LinkedSourcePolicy`).
- The AR-8 name `ConversationStartSetupProjection` is preserved in directory naming (`src/Hexalith.Projects/Projections/ConversationStartSetup/`) and class naming (`ConversationStartSetupProjector`) so the architecture's named entity remains discoverable. The class lives under `Projections/` per AR-8 + architecture line 527 (`ConversationStartSetup projection handlers (pure)`).
- This decision is **reversible**: a future story can replace the pure projector with a separate event-stream projection IF a benchmark proves the indirection unacceptable (e.g. a Chatbot p99 latency target the policy can't meet because tenant-access freshness is on the critical path). Until that benchmark surfaces, the pure projector is the minimum-surface implementation.

**Consequences captured here so the dev agent knows what to NOT build:**
- No new EventStore subscription wiring (the `ProjectsWorkersModule.cs` is untouched).
- No new `IProjectProjectionStore<>` registration (the existing detail projection store is the durable source of `ProjectSetup` state).
- No new rebuild test (the existing `ProjectDetailProjection.Rebuild(...)` covers the underlying state; the projector is a pure function with no rebuild semantic).
- No new freshness watermark (the projector inherits `ProjectContextFreshness` from the policy).
- No new `ConversationStartSetupItem` projection record (the wire DTO `ConversationStartSetup` IS the projection's output type — there is no intermediate item).

**Dev Agent should record the alignment with this decision in the Dev Agent Record (AC 19).** If the dev agent finds a reason to deviate (e.g. the policy stopped exposing `ProjectContext.Setup` correctly on an `Unauthorized` outer collapse), the deviation MUST be recorded with a one-line rationale and a follow-up story note ("re-evaluate event-stream projection in Story X").

### Current Code Facts Verified (this working tree, baseline `883ebca`)

- **Story 3.4 status: `done`** (per `_bmad-output/implementation-artifacts/sprint-status.yaml:135` and the review-cycle commentary at line 3). Story 3.4's `GET /api/v1/projects/{projectId}/context/refresh` ships at `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` (226 lines, partial class), the three additive ACL `RefreshXxxAsync` methods on the Story 2.4 / 2.5 / 2.7 interfaces, the three outcome mappers (`ProjectFolderValidationOutcomeMapper`, `ProjectFileReferenceValidationOutcomeMapper`, `ProjectMemoryValidationOutcomeMapper`), and the Tier-2 / Tier-1 / Client test additions. Story 3.5 must NOT regress any of this.
- **Story 3.3 status: `done`.** `GET /api/v1/projects/{projectId}/context/explain` at `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs`.
- **Story 3.2 status: `done`.** `GET /api/v1/projects/{projectId}/context` at `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs` (142 lines, partial class) is the canonical handler shape Story 3.5 mirrors (with the explicit subtractions noted in AC 5).
- **`ProjectContextOperationKind.GetConversationStartSetup` ships** at `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:37`. **`IsReadOnlyOperation(...)` already allows `GetConversationStartSetup`** at `ProjectContextInclusionPolicy.cs:238` (no policy change needed; Story 3.1 forward-built this).
- **The decision matrix `GetConversationStartSetup (3.5)` column ships** at `docs/context-assembly-decision-matrix.md:16` (position 5 in the header). The column is identical to `Get` / `Refresh` / `Explain` for every row by design — read-only operations consume the same fail-closed verdicts. Story 3.5 does NOT add or modify any matrix column.
- **The three ACL interfaces are in place** with the Story 3.4 additive `RefreshXxxAsync` methods. Story 3.5 does NOT consume any ACL interface and does NOT inject any directory through DI.
- **The `ProjectDetailItem` already carries the projection-stored `Setup` field** (`ProjectSetup?` at `ProjectDetailItem.cs:38`) per Story 1.8. Story 3.5 consumes this via the policy's `ProjectContext.Setup` output.
- **`ProjectAuthorizationGate.AuthorizeReadAsync` returns `ProjectAuthorizationResult { TenantAccessResult: TenantAccessAuthorizationResult? }`** (Story 3.2 / Task 4 additive extension). Story 3.5 consumes the property unchanged via the same defensive-null collapse pattern as Story 3.2 / 3.3 / 3.4.
- **`AddProjectsServer()` calls `AddProjectsModule()`** (Story 3.2 fix) so `ProjectContextInclusionPolicy` is DI-registered (`src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs:35` — `services.TryAddTransient<ProjectContextInclusionPolicy>();`). No DI change is needed for Story 3.5.
- **The NSwag MSBuild target Linux fix shipped** in Story 3.2 (forward-slash paths + `$(HexalithProjectsDotnetHostPath)` derived from `$(MSBuildToolsPath)`). Story 3.5 inherits the working Linux regeneration path; no `.csproj` change required.
- **The canonical negative-test checklist ships** at `docs/checklists/mutation-and-query-negative-tests.md` (Story 3.2 / Action Item 7) with 8 rows. Story 3.5 applies the query-side rows (1 / 4 / 5 / 6 / 8) and ticks them in the Dev Agent Record.
- **`OpenApiContractSpineTests.Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths`** is the canonical spine-validation gate — Story 3.5's new `GetConversationStartSetup` operation block surfaces the new `ConversationStartSetup` schema + example, both of which the gate will check.
- **The root commit is `883ebca feat(story-3.4): Story 3.4: Refresh Project Context`** (Story 3.4 merged). No `Hexalith.Memories` / `Conversations` / `Folders` / `Tenants` / `EventStore` / `FrontComposer` / `Commons` / `AI.Tools` / `Builds` submodule pointer change is required; pre-existing "modified content" markers on `Hexalith.Commons` / `Hexalith.Conversations` / `Hexalith.Parties` are unrelated to Story 3.5.
- **Baseline test counts (post-Story-3.4 review cycle 1, per sprint-status.yaml line 3):** Server.Tests 318, Projects.Tests 457, Contracts.Tests 128, Client.Tests 40, Integration.Tests 14; full-solution 927/927.

### Required Capability Path

Story 3.5's true upstream capability gates are entirely already in place; Story 3.5 introduces no new external dependency:

- `ProjectAuthorizationGate.AuthorizeReadAsync` returning `ProjectAuthorizationResult.TenantAccessResult` — Story 3.2 additive extension. **READY.**
- `ProjectContextInclusionPolicy.Assemble(...)` with `OperationKind.GetConversationStartSetup` allowed — Story 3.1. **READY.**
- `ProjectContextAssemblyContext` / `ProjectContextProjectEvidence` / `ProjectContextTenantAccess` / `ProjectContextReferenceEvidence` records — Story 3.1. **READY.**
- DI registration of `ProjectContextInclusionPolicy` via `AddProjectsModule()` reached through `AddProjectsServer()` — Story 3.2. **READY.**
- `ProjectContext.Setup` — Story 3.1 (the assembled context carries the policy-passed-through `ProjectSetup?`). **READY.**
- `ProjectSetup` / `ConversationStartDefaults` / `LinkedSourcePolicy` — Story 1.8 / Story 1.3 baseline. **READY.**
- `ProjectContextSourceKind` / `ProjectLifecycle` / `ProjectContextFreshness` enums — Story 1.2 / 1.6 / 3.1. **READY.**
- `Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder` — Story 3.1 reusable fixture builder. **READY.**

If the dev agent finds that `ProjectContext.Setup` is no longer populated on the `Assembled` outer outcome path (e.g. a defensive change in `ProjectContextInclusionPolicy.cs:173–185`), HALT and surface the conflict — the resolution is to extend Story 3.1's evidence shape, NOT to reconstitute the missing semantic in Story 3.5's projector. Similarly if `ProjectSetup.ConversationStartDefaults` becomes typed differently, HALT.

### Guardrails

- **Single source of truth — the policy.** `ProjectContextInclusionPolicy.Assemble(...)` is the only place where include/exclude / fail-closed-collapse / freshness-mapping / diagnostic-vocabulary / per-candidate evaluation emission decisions are made. The endpoint, the projector, and the wire DTO NEVER duplicate any of these decisions. The projector translates `ProjectContext` to `ConversationStartSetup` — it NEVER applies policy decisions; the resulting bounded subset is a one-to-one read of the policy's output.
- **Safe-denial 404 contract.** The HTTP status surfaces `200` (assembled, including archived-project) or one of `400 / 401 / 403 / 404 / 503`. `ProjectContextAssemblyOutcome.Unauthorized` and `.ProjectUnavailable` BOTH map to **HTTP 404** at the boundary — never reveal cross-tenant existence, never differentiate `Unauthorized` vs `ProjectUnavailable` at the HTTP layer. Outer-collapse branches return safe-denial 404 with NO `ConversationStartSetup` body (Problem Details only); the policy's internal `AssemblyOutcome` is observability-only. This is the Story 1.4 + Story 3.1 + Story 3.2 + Story 3.3 + Story 3.4 safe-denial 404 contract carried forward verbatim.
- **No sibling ACL calls on ANY path.** Story 3.5 NEVER injects `IProjectConversationDirectory` / `IProjectFolderDirectory` / `IProjectFileReferenceDirectory` / `IProjectMemoryDirectory`. The handler signature itself proves this by construction; AC 9 `..._DoesNotCallSiblingAcls` proves no future code-path regression can silently introduce a sibling call. This is the FR-20 fast-path guarantee.
- **Idempotency-Key rejected on the query** (mirrors `GetProject` / `GetProjectContext` / `GetProjectContextExplanation` / `RefreshProjectContext`). Order: authorize first → then validate `Idempotency-Key` is absent → then proceed. Authorized callers receive validation feedback; unauthorized callers receive only safe-denial 404.
- **Freshness header strict.** `X-Hexalith-Freshness` request header may be `eventually_consistent` or absent; any other value is rejected as a validation error after authorization. Response always carries `X-Hexalith-Freshness: eventually_consistent`.
- **Correlation echo.** `X-Correlation-Id` request header (if canonical) is echoed in the response.
- **No `TenantId` on the wire body.** The new DTO does NOT declare a `TenantId` field at all (cleaner than the `[JsonIgnore]`-on-required-field pattern Story 3.2 used for `ProjectContext.TenantId` — see `ProjectContext.cs:67`).
- **Empty references-evidence on policy invocation.** The policy is invoked with `ProjectFolder: null`, `FileReferences: Array.Empty<...>()`, `MemoryReferences: Array.Empty<...>()`, `Conversations: Array.Empty<...>()`. The policy's per-candidate loops at `ProjectContextInclusionPolicy.cs:130–187` are no-ops over empty input lists, so no `ProjectContextEvaluation` rows are emitted. The policy's outer collapses still run unchanged.
- **No re-fetch FOR Stories 3.2 / 3.3 / 3.4.** The Get / Explain / Refresh endpoints continue to consume their existing inputs (Story 3.5 does NOT change those contracts; it is a sibling fast-path).
- **Tier-1 purity preserved.** `src/Hexalith.Projects/Context/**` MUST NOT gain any new file or change in Story 3.5. The projector lives under `src/Hexalith.Projects/Projections/ConversationStartSetup/` (Tier-1 purity — domain core, no infrastructure). The handler lives in `src/Hexalith.Projects.Server/Queries/`. The new wire DTO lives in `src/Hexalith.Projects.Contracts/Models/`. The projector test file lives in `tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/` (Tier-1 purity).
- **No new shared-vocabulary enum values.** Story 3.1's enums + the existing pre-Epic-3 vocabulary + the existing `LinkedSourcePolicy` / `ProjectContextSourceKind` / `ProjectLifecycle` / `ProjectContextFreshness` are sufficient for Story 3.5.
- **No edits to Story 3.1 / 3.2 / 3.3 / 3.4 surface beyond additive serialization attributes.** All wire DTOs, enums, the policy, the Story 3.2 / 3.3 / 3.4 handlers, the three outcome mappers, the matrix doc, the canonical checklist — unchanged.
- **OpenAPI fingerprint baseline updated** (deliberate, allowed for this story).
- **`.g.cs` regenerated** (deliberate, allowed for this story). NSwag Linux fix is inherited from Story 3.2.
- **No nested recursive submodule init.** Read-only inspection is fine; nothing in Story 3.5 advances a submodule pointer.
- **Deterministic-fakes-only tests.** No `Thread.Sleep` / `Task.Delay` / `SpinWait` / `await Task.Yield()` / wall-clock retry loops.
- **Closed-vocabulary diagnostics only.** The endpoint NEVER surfaces a `Diagnostic` value at all — `ConversationStartSetup` carries no diagnostic field by design (the FR-20 fast path elides exclusion diagnostics; consumers that need diagnostics call Story 3.3 Explain).
- **No `V2` types.** Public contracts evolve only through additive types. Story 3.5 adds ONE new wire DTO (`ConversationStartSetup`) and ONE new partial-class file — no `V2` event, no `V2` DTO, no breaking change.
- **Body-shape parity with FR-20.** Story 3.5 surfaces a bounded subset (NOT `ProjectContext`, NOT `ProjectContextExplanation`). Rationale: FR-20's semantic is "start a conversation without re-querying every bounded context" — the bounded subset minimizes payload, eliminates per-reference inventory, and ships a wire shape Chatbot can rely on as a fast path.
- **Epic 3 is complete after Story 3.5.** No further Epic 3 spine churn is expected.

### Suggested Wire DTO Shape

```csharp
// src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs

namespace Hexalith.Projects.Contracts.Models;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// The bounded subset of <see cref="ProjectSetup"/> Hexalith.Chatbot retrieves to start or resume a
/// conversation without re-querying every bounded context first (FR-20, Story 3.5). Realizes AR-8's
/// <c>ConversationStartSetupProjection</c> as the wire shape returned by
/// <c>GET /api/v1/projects/{projectId}/setup/conversation-start</c>.
/// </summary>
/// <remarks>
/// The wire shape is purely metadata — bounded text fields, closed-vocabulary policy/lifecycle/freshness
/// enums, and a typed observation instant. It never carries transcripts, file contents, memory bodies,
/// prompts, paths, tokens, secrets, tenant authority, audit metadata, or any per-reference inventory.
/// Consumers that need per-reference inventories or exclusion diagnostics call
/// <c>GetProjectContext</c> / <c>ExplainContextSelection</c> / <c>RefreshProjectContext</c>.
/// </remarks>
public sealed record ConversationStartSetup(
    string ProjectId,
    ProjectLifecycle Lifecycle,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> UserInstructions,
    IReadOnlyList<ProjectContextSourceKind> PreferredSourceKinds,
    IReadOnlyList<ProjectContextSourceKind> ExcludedSourceKinds,
    LinkedSourcePolicy LinkedSourcePolicy,
    DateTimeOffset ObservedAt,
    ProjectContextFreshness Freshness)
{
    /// <summary>Builds the canonical empty-setup record for a project with no UpdateProjectSetup yet.</summary>
    public static ConversationStartSetup Empty(
        string projectId,
        ProjectLifecycle lifecycle,
        DateTimeOffset observedAt,
        ProjectContextFreshness freshness)
        => new(
            projectId,
            lifecycle,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<ProjectContextSourceKind>(),
            Array.Empty<ProjectContextSourceKind>(),
            LinkedSourcePolicy.None,
            observedAt,
            freshness);

    /// <summary>Projects a policy-assembled <see cref="ProjectContext"/> onto the bounded conversation-start subset.</summary>
    public static ConversationStartSetup FromContext(ProjectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ProjectSetup? setup = context.Setup;
        LinkedSourcePolicy linkedSource = setup?.ConversationStartDefaults?.LinkedSourcePolicy ?? LinkedSourcePolicy.None;
        return new ConversationStartSetup(
            ProjectId: context.ProjectId,
            Lifecycle: context.Lifecycle,
            Goals: setup?.Goals ?? Array.Empty<string>(),
            UserInstructions: setup?.UserInstructions ?? Array.Empty<string>(),
            PreferredSourceKinds: setup?.PreferredSourceKinds ?? Array.Empty<ProjectContextSourceKind>(),
            ExcludedSourceKinds: setup?.ExcludedSourceKinds ?? Array.Empty<ProjectContextSourceKind>(),
            LinkedSourcePolicy: linkedSource,
            ObservedAt: context.ObservedAt,
            Freshness: context.Freshness);
    }
}
```

### Suggested Projector Shape

```csharp
// src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs

namespace Hexalith.Projects.Projections.ConversationStartSetup;

using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Pure projector realizing AR-8's <c>ConversationStartSetupProjection</c> as a server-side projection
/// over the policy-assembled <see cref="ProjectContext"/> (Story 3.1 / Story 3.5 / FR-20). See Design
/// Decision in the Story 3.5 dev notes for why this is a pure projector rather than a separate
/// event-stream projection.
/// </summary>
public static class ConversationStartSetupProjector
{
    /// <summary>Projects the policy-assembled context onto the bounded conversation-start subset.</summary>
    public static ConversationStartSetup Project(ProjectContext context)
        => ConversationStartSetup.FromContext(context);
}
```

### Suggested Handler Shape

```csharp
// src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs
//
// New partial-class file. Mirrors Story 3.2's GetProjectContextEndpoint.cs shape, with the following
// SUBTRACTIONS:
//   - No IProjectConversationDirectory dependency
//   - No conversation page fetch
//   - No ProjectContextConversationEvidenceMapper invocation
//   - No 'using ConversationTenantId = ...' alias
// And the following CHANGE:
//   - OperationKind: ProjectContextOperationKind.GetConversationStartSetup
//   - Empty ProjectContextReferenceEvidence (all four lists empty)
//   - Returns Results.Json(ConversationStartSetupProjector.Project(assembled.Context), ...)

namespace Hexalith.Projects.Server;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ConversationStartSetup;
using Hexalith.Projects.Projections.ProjectDetail;

using Microsoft.AspNetCore.Http;

public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> GetConversationStartSetupAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        ProjectContextInclusionPolicy contextPolicy,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // ... header parsing, envelope validation, authorize-read, idempotency / freshness rejection,
        //     defensive null collapse — copy verbatim from GetProjectContextEndpoint.cs lines 62–101.

        ProjectDetailItem detail = authorization.ProjectDetail;
        DateTimeOffset now = timeProvider.GetUtcNow();

        ProjectContextAssemblyResult assembled = contextPolicy.Assemble(
            new ProjectContextAssemblyContext(
                AuthoritativeTenantId: tenantContext.AuthoritativeTenantId,
                RequestedTenantId: tenantContext.AuthoritativeTenantId,
                ProjectId: projectId,
                OperationKind: ProjectContextOperationKind.GetConversationStartSetup,
                CorrelationId: correlationId,
                TaskId: taskId,
                Now: now),
            new ProjectContextProjectEvidence(detail),
            new ProjectContextTenantAccess(tenantAccessResult),
            new ProjectContextReferenceEvidence(
                ProjectFolder: null,
                FileReferences: Array.Empty<ProjectFileReference>(),
                MemoryReferences: Array.Empty<ProjectMemoryReference>(),
                Conversations: Array.Empty<ProjectContextConversationEvidence>()));

        if (assembled.Context.AssemblyOutcome is ProjectContextAssemblyOutcome.Unauthorized
            or ProjectContextAssemblyOutcome.ProjectUnavailable)
        {
            return SafeDenial(correlationId, null);
        }

        ConversationStartSetup body = ConversationStartSetupProjector.Project(assembled.Context);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(body, ResponseJsonOptions);
    }
}
```

### Project Structure Notes

- New files (5):
  - `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` (wire DTO)
  - `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs` (pure projector — AR-8 named entity)
  - `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` (handler partial-class)
  - `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs` (Tier-2 endpoint tests)
  - `tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/ConversationStartSetupProjectorTests.cs` (Tier-1 projector tests)
  - `tests/Hexalith.Projects.Contracts.Tests/Models/ConversationStartSetupTests.cs` (Tier-0 Contracts tests)
  - `tests/Hexalith.Projects.Client.Tests/GetConversationStartSetupClientTests.cs` (typed-client substring tests)
- Modified files (3):
  - `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (new operation + new schema + new example)
  - `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` (regenerated)
  - `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` (regenerated; SHA fingerprint constants only)
  - `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` (single new `MapGet` registration after the Story 3.4 registration at line 155–177)
- Out-of-tree (no modification): all Story 3.1 / 3.2 / 3.3 / 3.4 files, all Story 2.x ACL files, all aggregate / projection files (`ProjectAggregate.*`, `ProjectState`, `ProjectStateApply`, `ProjectDetailProjection`, `ProjectListProjection`, `ProjectReferenceIndexProjection`, `TenantAccessProjection`), all Story 1.x infrastructure files (`DaprProjectProjectionStore`, `ProjectEventProjectionProcessor`, etc.), all Workers files.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-3.5`] — Story 3.5 user story + 2 BDD ACs.
- [Source: `_bmad-output/planning-artifacts/epics.md#FR-20`] — FR-20 functional requirement statement.
- [Source: `_bmad-output/planning-artifacts/epics.md#AR-8`] — AR-8 names `ConversationStartSetupProjection` as a tenant-scoped projection.
- [Source: `_bmad-output/planning-artifacts/architecture.md#L58`] — "Project Setup Quality (FR-19–FR-20): validate setup; serve conversation-start setup."
- [Source: `_bmad-output/planning-artifacts/architecture.md#L238`] — Read store includes `ConversationStartSetupProjection`.
- [Source: `_bmad-output/planning-artifacts/architecture.md#L312`] — "Projections (list/detail/reference-index/audit/conversation-start-setup) with freshness + rebuild tests."
- [Source: `_bmad-output/planning-artifacts/architecture.md#L511`] — Contracts/Queries includes `GetConversationStartSetup`.
- [Source: `_bmad-output/planning-artifacts/architecture.md#L527`] — `ConversationStartSetup projection handlers (pure)` under `src/Hexalith.Projects/Projections/`.
- [Source: `_bmad-output/planning-artifacts/architecture.md#L603`] — "Setup Quality (FR-19–20): `Projections/ConversationStartSetup*` + `Queries/GetConversationStartSetup`."
- [Source: `_bmad-output/planning-artifacts/architecture.md#L670`] — "FR-19–20 (Setup quality): ✅ setup validator + conversation-start-setup projection."
- [Source: `docs/context-assembly-decision-matrix.md#L16`] — `GetConversationStartSetup (3.5)` column (fifth operation, identical to `Get`).
- [Source: `docs/checklists/mutation-and-query-negative-tests.md`] — 8-row canonical checklist; Story 3.5 applies rows 1 / 4 / 5 / 6 / 8.
- [Source: `_bmad-output/project-context.md`] — Tier-1 purity rules, payload taxonomy, deterministic-clock-only rule, deterministic-fakes-only test rule, additive-contracts rule.
- [Source: `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs#L37`] — `GetConversationStartSetup` operation kind already shipped.
- [Source: `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs#L173–185`] — `ProjectContext` assembly construction including `Setup` / `Lifecycle` / `ObservedAt` / `Freshness` passthrough.
- [Source: `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs#L238`] — `GetConversationStartSetup` on the read-only allowlist.
- [Source: `src/Hexalith.Projects.Contracts/Models/ProjectSetup.cs`] — five-field shape (`Goals`, `UserInstructions`, `PreferredSourceKinds`, `ExcludedSourceKinds`, `ConversationStartDefaults`).
- [Source: `src/Hexalith.Projects.Contracts/Models/ConversationStartDefaults.cs`] — single-field record (`LinkedSourcePolicy`).
- [Source: `src/Hexalith.Projects.Contracts/Models/LinkedSourcePolicy.cs`] — closed v1 vocabulary (`None` / `ProjectsOwnedMetadataOnly` / `AuthorizedReferences`) + lower-camelCase JSON converter.
- [Source: `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs#L67`] — `[JsonIgnore]` rule on `TenantId` (FS-8 / SM-3); Story 3.5's DTO mirrors this by omitting `TenantId` entirely.
- [Source: `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs`] — canonical Story 3.2 handler shape Story 3.5 mirrors with subtractions.
- [Source: `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs`] — most recent Story 3.4 sibling partial-class precedent.
- [Source: `_bmad-output/implementation-artifacts/3-4-refresh-project-context.md`] — Story 3.4 baseline (test counts, Dev Agent Record pattern, validation lane structure).
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`] — Story 3.5 row to update (`3-5-retrieve-conversation-start-setup: backlog` → `ready-for-dev` on this story creation; `epic-3-retrospective: optional` row untouched).
- [[build-environment]] — `DOTNET_ROOT=/home/administrator/.dotnet` (10.0.300); LF on disk; NSwag idempotency-helper regen workaround.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context).

### Debug Log References

- `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` — 0 W / 0 E.
- `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects/Hexalith.Projects.csproj` — 0 W / 0 E.
- `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` — 0 W / 0 E (regenerated `HexalithProjectsClient.g.cs` cleanly via the NSwag MSBuild target inheriting the Story 3.2 Linux path fix; `HexalithProjectsIdempotencyHelpers.g.cs` rewrote only the SHA256 fingerprint constants).
- `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` — 0 W / 0 E.
- `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx` — 0 W / 0 E.
- `dotnet test Hexalith.Projects.slnx --no-build` — 978/978 passed, 0 failed, 0 skipped across all five lanes.
- Targeted lanes: `GetConversationStartSetupTests` — 26/26 (Server.Tests); `ConversationStartSetupProjectorTests` — 15/15 (Projects.Tests); `ConversationStartSetupTests` (Contracts) — 7/7; `GetConversationStartSetupClientTests` — 3/3.

### Completion Notes List

**Endpoint shape (AC 1):** `GET /api/v1/projects/{projectId}/setup/conversation-start` added to the OpenAPI spine immediately after the Story 3.4 `/context/refresh` block. Canonical-error-categories mirror `GetProjectContext`'s 8-row set verbatim (no `validation_error`, no `referenced_resource_unavailable` — the AC 1 difference from Story 3.4 was verified by inspection at spine lines 339–347).

**Wire DTO (AC 2 / 12):** `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` — sealed positional record with the exact 9-field shape per AC 2. `ConversationStartSetup.Empty(...)` and `ConversationStartSetup.FromContext(...)` static factories ship as the canonical mapper. No `TenantId` declared on the record (cleaner than the `[JsonIgnore]`-on-required-field pattern Story 3.2 used for `ProjectContext.TenantId`). No audit metadata. No reference inventory.

**Projector (AC 3 / 11):** `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs` — `public static class` whose single `Project(ProjectContext)` method delegates to `ConversationStartSetup.FromContext(context)`, so the contract DTO's static factory IS the projection function (the projector exists so the AR-8 named projection has a concrete class under `Projections/`). Pure: no `DateTimeOffset.UtcNow` / `Stopwatch` / `Environment.TickCount` / random / GUID. Imports only `Hexalith.Projects.Contracts.Models`.

**Handler placement (AC 5 / 6):** new partial-class file at `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` (mirrors Story 3.2 / 3.3 / 3.4 partial-class pattern). Route registered in `ProjectsDomainServiceEndpoints.cs` directly after the Story 3.4 `/context/refresh` `MapGet(...)`. Handler signature is a strict subset of Story 3.2 — no `IProjectConversationDirectory`, no `IProjectFolderDirectory`, no `IProjectFileReferenceDirectory`, no `IProjectMemoryDirectory`. No `using ConversationTenantId = ...` alias.

**Empty references-evidence (AC 5 (i)):** confirmed by re-reading `ProjectContextInclusionPolicy.cs:120–157` (per-candidate loops iterate empty input lists as no-ops); the policy was invoked with `ProjectFolder: null, FileReferences: Array.Empty<ProjectFileReference>(), MemoryReferences: Array.Empty<ProjectMemoryReference>(), Conversations: Array.Empty<ProjectContextConversationEvidence>()`. The policy's outer collapses still execute. The handler additionally branches `assembled.Context.AssemblyOutcome` to safe-denial 404 on `Unauthorized` / `ProjectUnavailable` per AC 5 (k). This is a stricter contract than Story 3.2 / 3.3 / 3.4's existing handlers (which serialize whatever the policy returned), but matches Story 3.5's explicit AC 5 (k) requirement and adds defense-in-depth without affecting wire shape on the happy path.

**Design Decision alignment (Dev Notes):** `ConversationStartSetupProjection` materialized as a pure server-side projector over `ProjectContext.Setup` — NOT a separate event-stream projection over `ProjectCreated` / `ProjectSetupUpdated` / `ProjectArchived` events. The decision was followed verbatim: no new `IProjectProjectionStore<>` registration, no Workers subscription wiring, no rebuild test, no separate item record. `ProjectsServiceCollectionExtensions.cs` is untouched (the static projector class needs no DI registration).

**Wire-shape invariants (AC 9 / 17):** dedicated tests assert (i) no `tenantId` (FS-8 / SM-3), (ii) no `createdAt` / `updatedAt` / `sequence` / `setupMetadata` (no audit metadata), (iii) no `projectFolder` / `fileReferences` / `memoryReferences` / `conversations` / `excluded` / `assemblyOutcome` (no per-reference inventory). The reference-inventory test uses `JsonDocument` parsing (not substring) because Shouldly's `ShouldNotContain` is case-insensitive and would false-positive on `excludedSourceKinds` (which contains the substring "Excluded").

**No-sibling-ACL-by-construction (AC 9):** `GetConversationStartSetup_DoesNotCallSiblingAcls` registers `RecordingConversationDirectory` / `RecordingFolderDirectory` / `RecordingFileReferenceDirectory` / `RecordingMemoryDirectory` into the test host DI container and asserts every recording directory's `CallCount == 0` after a happy-path request. The handler signature itself proves no sibling call is possible by construction (the dependencies are absent); the test is the regression guard against a future code-path that silently injects a sibling.

**Cross-tenant isolation (AC 10 / FS-8 / SM-3):** `GetConversationStartSetup_CrossTenant_ReturnsSafeDenial404` asserts (i) HTTP 404; (ii) no `tenant-b` in body; (iii) no `ConversationStartSetup` JSON shape (no `"projectId":` / `"lifecycle":` / `"linkedSourcePolicy":` substrings — the body is a Problem Details denial only). Implicit no-leak-to-siblings invariant is subsumed by the by-construction no-sibling-ACL guarantee (the handler signature has no sibling dependency).

**Negative-test checklist tick-off (AC 17 / Task 10):**
- Row 1 (Malformed identifier → safe-denial 404): `GetConversationStartSetup_MalformedProjectId_ReturnsSafeDenial404` (Theory over 7 malformed inputs incl. whitespace, tab, `.`, `..`, `..%2F..`, slash, unicode bidi).
- Row 4 (`Idempotency-Key` PRESENT on query → 400 after authz): `GetConversationStartSetup_IdempotencyKeyPresent_ReturnsValidationProblem` + `..._IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404` (ordering proof — authorize first → idempotency-key check second).
- Row 5 (Stricter `X-Hexalith-Freshness` → 400): `GetConversationStartSetup_StricterFreshnessRequested_ReturnsValidationProblem`.
- Row 6 (Cross-tenant safe-denial 404): `GetConversationStartSetup_CrossTenant_ReturnsSafeDenial404` + the by-construction no-sibling-ACL assertion `GetConversationStartSetup_DoesNotCallSiblingAcls`.
- Row 8 (`ReferenceState.Unavailable && Retryable` → 503): `GetConversationStartSetup_TenantAccessUnavailable_ReturnsReadModelUnavailable503`.
- Rows 2 / 3 / 7 are mutation-only — **N/A** for Story 3.5 (query endpoint).

**Test deltas (per lane):**
- `Hexalith.Projects.Server.Tests` 318 → 344 (+26): 9 AC 9 named fixtures (HappyPath / NullSetup / ArchivedProject / ConversationStartDefaultsMissing / SourceKindOrder / BodyDoesNotContainTenantId / BodyDoesNotContainAuditMetadata / BodyDoesNotContainReferenceInventory / DoesNotCallSiblingAcls) + IdempotencyKey rejection + IdempotencyKey-with-unauthorized + freshness rejection + malformed projectId Theory (expanded to 7 cases) + cross-tenant + TenantAccess 503 + AuthoritativeTenantId missing + header echo + extra query parameters + response-body leakage + error-responses leakage. Total = 26.
- `Hexalith.Projects.Tests` 427 → 442 (+15): 11 named projector tests, with 2 Theory methods expanding to 6 cases — total = 15.
- `Hexalith.Projects.Contracts.Tests` 128 → 135 (+7): round-trip, leakage, no-TenantId, no-audit-fields, default-LinkedSourcePolicy, additive-tolerance, stable property order.
- `Hexalith.Projects.Client.Tests` 40 → 43 (+3): typed-method signature, no-idempotency-helper, LF/NUL-free disk layout.
- `Hexalith.Projects.Integration.Tests` 14 → 14 (no new AppHost smoke — in-process WebApplication-slim Tier-2 coverage is sufficient per AC 8 / 9 / 10 / 17).
- **Total solution 927 → 978 (+51).** All green. Failed: 0. Skipped: 0. (The story budget estimated approximately +38; the actual count is higher because Theory rows expand and the `Hexalith.Projects.Tests` baseline was 427 in the post-3.4 state, not 457 as the story acceptance criteria mentioned.)

**OpenAPI fingerprint baseline update:** the spine fingerprint changes deliberately for this story (new operation + new `ConversationStartSetup` schema + 2 new examples `ConversationStartSetup` / `ConversationStartSetupEmpty`). `HexalithProjectsClient.g.cs` regenerated cleanly: +251 LOC for typed `Task<ConversationStartSetup> GetConversationStartSetupAsync(...)` + `ConversationStartSetup` partial class + `ConversationStartSetupLifecycle` enum. `HexalithProjectsIdempotencyHelpers.g.cs` only rewrote the SHA256 fingerprint constants (4 lines). No queries gained an idempotency-helper entry.

**Boundary greps — all PASS on code lines:**
- `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` → 0 hits (Story 3.1 invariant preserved).
- `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Projections/ConversationStartSetup/` → 0 hits.
- `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` → 0 hits.
- `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" <handler/projector/DTO>` → 0 code hits (only XML doc comments referencing what's prohibited).
- `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" <Story-3.5 test files>` → 0 code hits.

**`git diff --check`:** clean across all story-touched files.

**`git status` boundaries:** pre-existing `m` markers on `Hexalith.Commons` / `Hexalith.Conversations` / `Hexalith.Parties` from prior sessions are unchanged (no submodule pointer advanced; no nested recursive submodule init).

**No Story 3.1 / 3.2 / 3.3 / 3.4 surface touched** beyond:
- `ProjectsDomainServiceEndpoints.cs` — one new `endpoints.MapGet(...)` registration after the Story 3.4 `/context/refresh` registration (per AC 6 — the only modification permitted to the partial-class root file).
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — additive: one new operation block, one new schema, two new examples.
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` + `HexalithProjectsIdempotencyHelpers.g.cs` — regenerated deterministically.

No additive `[JsonPropertyName]` was required on existing Story 3.1 / 3.2 / 3.3 / 3.4 DTOs.

**HALT items:** none. The capability gate (Task 1) confirmed every precondition listed in the story's HALT clauses (Task 1 final step, AC 14, AC 15). `ProjectContextOperationKind.GetConversationStartSetup` was present at `ProjectContextOperationKind.cs:37` and on the read-only allowlist at `ProjectContextInclusionPolicy.cs:238`. The policy's outer-collapse paths still populate `ProjectContext.Setup` / `Lifecycle` / `ObservedAt` / `Freshness` correctly on the `Assembled` path. `ProjectSetup.ConversationStartDefaults` is still the nested-record source of `LinkedSourcePolicy`.

**Epic 3 completion one-liner (AC 19):** Story 3.5 is the **last Epic 3 story** (per the architecture's named projections and the AR-8 / AR-9 / FR-17 / FR-18 / FR-20 fan-out). With Story 3.5 at `review`, the next step is the optional `epic-3-retrospective` story (`_bmad-output/implementation-artifacts/sprint-status.yaml` row currently marked `optional`). The dev agent did NOT transition `epic-3` to `done` — that is the retrospective story's responsibility (or a tech-lead manual transition if the retrospective is skipped). The OpenAPI spine should remain stable until Epic 4 (Resolution endpoints) lands.

### File List

**New (7):**
- `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs` (wire DTO — sealed positional record + `Empty` / `FromContext` static factories).
- `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs` (pure projector realizing AR-8's named `ConversationStartSetupProjection`).
- `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` (HTTP handler partial-class).
- `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs` (Tier-2 endpoint tests — 26 fixtures).
- `tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/ConversationStartSetupProjectorTests.cs` (Tier-1 purity tests — 11 named tests, 15 total after Theory expansion).
- `tests/Hexalith.Projects.Contracts.Tests/Models/ConversationStartSetupTests.cs` (Tier-0 Contracts tests — 7 named tests).
- `tests/Hexalith.Projects.Client.Tests/GetConversationStartSetupClientTests.cs` (typed-client substring tests — 3 named tests).

**Modified (4):**
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (new `GetConversationStartSetup` operation + `ConversationStartSetup` schema + `ConversationStartSetup` / `ConversationStartSetupEmpty` examples).
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` (regenerated; +251 LOC for typed `GetConversationStartSetupAsync(...)` + `ConversationStartSetup` partial class + `ConversationStartSetupLifecycle` enum).
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` (regenerated — SHA256 fingerprint constants only; no idempotency-helper entry added because the new operation is a query).
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` (single new `MapGet("/api/v1/projects/{projectId}/setup/conversation-start", ...)` registration after the Story 3.4 `/context/refresh` block).

**Sprint status:**
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (`3-5-retrieve-conversation-start-setup`: `ready-for-dev` → `in-progress` → `review`).

### Change Log

| Date       | Change                                                                                                                  |
|------------|-------------------------------------------------------------------------------------------------------------------------|
| 2026-05-28 | Story 3.5: Retrieve Conversation-Start Setup — `GET /api/v1/projects/{projectId}/setup/conversation-start` end-to-end. |
| 2026-05-28 | Story 3.5 code review cycle 1: status `review` → `done` (0 CRITICAL findings; build 0W/0E; full solution 978/978 passing; boundary greps clean; 3 LOW observations recorded inheriting Story 3.2 / 3.3 / 3.4 precedents — not auto-fixed). |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-05-28
**Outcome:** Approve

**Verification performed against working tree (no recheckout):**
- `git status --porcelain` aligns with the story File List (1 sprint-status / 1 orchestration-log line + 4 modified product files + 7 new product/test files). No undisclosed changes.
- `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx` → **0 W / 0 E**.
- `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet test Hexalith.Projects.slnx --no-build` → **978/978 passing (43 + 14 + 442 + 135 + 344); 0 failed; 0 skipped**. Matches the dev's claimed counts verbatim.
- Boundary greps reproduced from AC 17: `Hexalith.(Conversations|Folders|Memories)` under `src/Hexalith.Projects/Context/`, `src/Hexalith.Projects/Projections/ConversationStartSetup/`, `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs` → 0 code hits (only XML-doc references in the projector docstring). `DateTime(Offset)?.UtcNow|Stopwatch|TickCount` in handler/projector/DTO code → 0 code hits (only XML-doc references). `Thread.Sleep|Task.Delay|SpinWait|Task.Yield` in Story 3.5 test files → 0 code hits (only one XML-doc reference in the projector tests).
- Acceptance Criteria audit: each of AC 1-19 mapped to evidence in the working tree. All Tasks/Subtasks `[x]` claims verified by file inspection.

**AC ↔ implementation cross-check:**
- AC 1 (OpenAPI operation) — `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:481-551` defines the operation; canonical-error-categories at 543-551 mirror `GetProjectContext`'s 8-row set without `referenced_resource_unavailable` per AC 1's explicit difference. Inserted directly after the Story 3.4 `/context/refresh` block.
- AC 2 (DTO) — `src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs:59-117` matches the prescribed 9-field positional record shape; `Empty(...)` and `FromContext(...)` static factories ship; no `TenantId` declared.
- AC 3 (projector) — `src/Hexalith.Projects/Projections/ConversationStartSetup/ConversationStartSetupProjector.cs:34-44` is a pure static class delegating to `ConversationStartSetup.FromContext(...)`. No infrastructure imports.
- AC 4 (regenerated client) — `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs:234,248,1718,5016,5607` confirms typed method + partial class + auxiliary enum. `HexalithProjectsIdempotencyHelpers.g.cs` does NOT mention `GetConversationStartSetup`.
- AC 5 (handler) — `src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:63-146` implements the prescribed flow with the AC 5(k) defensive `Unauthorized`/`ProjectUnavailable`-collapse (a stricter contract than Story 3.2/3.3/3.4 by design).
- AC 6 (registration) — `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:179-195` adds the single `MapGet(...)` directly after the Story 3.4 registration.
- AC 7 (single source of truth) — the policy is invoked unchanged; the projector contains no inclusion/freshness/diagnostic logic.
- AC 8-10 + AC 17 (endpoint tests) — `tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs` lands all 26 fixtures including outer-collapse cells, cross-tenant safe-denial 404, idempotency-after-authz ordering, and the by-construction `DoesNotCallSiblingAcls` proof.
- AC 11 (projector tests) — `tests/Hexalith.Projects.Tests/Projections/ConversationStartSetup/ConversationStartSetupProjectorTests.cs` ships all 11 named tests (15 total after Theory expansion).
- AC 12 (contracts tests) — `tests/Hexalith.Projects.Contracts.Tests/Models/ConversationStartSetupTests.cs` covers round-trip, FS-2 leakage, no-TenantId, no-audit-fields, default LinkedSourcePolicy, additive tolerance, and stable property order.
- AC 13 (client substring tests) — `tests/Hexalith.Projects.Client.Tests/GetConversationStartSetupClientTests.cs` ships the three required tests.
- AC 14-16 (no-edit invariants) — confirmed by inspection: no diff under `src/Hexalith.Projects/Context/**`, `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs`, `ProjectSetup.cs`, `ConversationStartDefaults.cs`, `LinkedSourcePolicy.cs`, the Story 2.x ACL files, or the Story 3.2 / 3.3 / 3.4 endpoint files.
- AC 18 (budgets) — build clean; total tests 927 → 978 (+51), exceeding the +38 estimate (Theory expansion + corrected Projects.Tests baseline per Dev Agent Record).
- AC 19 (Dev Agent Record) — fully populated including the Epic 3 completion one-liner and negative-test checklist tick-off.

**Issues found**

🔴 CRITICAL: none.
🟡 MEDIUM: none.
🟢 LOW (3, all pre-existing inherited patterns; intentionally NOT auto-fixed — would expand the change-set beyond Story 3.5 scope and break the AC 14 "no edits to Story 3.1 / 3.2 / 3.3 / 3.4 surface beyond additive serialization attributes" invariant):
- **L1 — Inline `lifecycle` enum in `ConversationStartSetup` schema** (`hexalith.projects.v1.yaml:3098-3103`). The schema repeats `type: string + enum: [Active, Archived]` inline instead of `$ref: "#/components/schemas/ProjectLifecycle"`. There is no `ProjectLifecycle` schema in the spine (only `ProjectLifecycleState` and `ProjectLifecycleStatus`); the `ProjectContext` schema at line 2794 does the same inlining. As a consequence NSwag generates a per-DTO `ConversationStartSetupLifecycle` enum on the client (`HexalithProjectsClient.g.cs:5607`) rather than reusing a shared one. AC 4 wording "Lifecycle reuses ProjectLifecycle" is aspirational and the consistent precedent from Stories 3.2 / 3.3 / 3.4 is the inline form. Resolving this would require a follow-up story to introduce a shared `ProjectLifecycle` component schema and switch every consumer — out of Story 3.5 scope.
- **L2 — `validation_error` absent from `x-hexalith-canonical-error-categories`** (`hexalith.projects.v1.yaml:543-551`). The endpoint can return `400` with category `validation_error` (Idempotency-Key + freshness rejections), but the canonical-error-categories list omits it. Stories 3.2 and 3.3 are identical (line 339-347 and 405-413); Story 3.4 is the first to add `referenced_resource_unavailable` and also omits `validation_error`. This is a pre-existing taxonomy gap, not a Story 3.5 regression — addressing it would require updating all four query operation blocks, which is out of scope.
- **L3 — `maxItems` constraints declared in spine but not enforced in C# DTO** (`hexalith.projects.v1.yaml:3105-3133`). The spine constrains `goals` / `userInstructions` to ≤16 items and `preferredSourceKinds` / `excludedSourceKinds` to ≤4 items, but the `ConversationStartSetup` C# record uses `IReadOnlyList<T>` without validation. The same drift exists between `ProjectContext` (server-projected list lengths are bounded upstream in `ProjectSetup` validation, not in the wire DTO). The Story 3.5 endpoint serializes whatever the upstream projection holds; any overflow violation surfaces as a contract violation but not a runtime error. Tolerable per precedent.

**Acceptance notes**
- The dev's intentional stricter behavior on `assembled.Context.AssemblyOutcome == Unauthorized | ProjectUnavailable` → HTTP 404 (handler lines 131-135) is documented in AC 5(k) and the Dev Agent Record. It is **more conservative** than Stories 3.2 / 3.3 / 3.4 and is a defense-in-depth measure — accepted.
- The reference-inventory assertion (`GetConversationStartSetup_BodyDoesNotContainReferenceInventory`) correctly uses `JsonDocument.TryGetProperty(...)` rather than substring match — a thoughtful subtlety that avoids a false-positive on `excludedSourceKinds` containing the substring `excluded`.
- The `DoesNotCallSiblingAcls` by-construction test registers `Recording*` directories into the slim DI host and asserts `CallCount == 0` — the handler signature itself prevents these calls, but the test catches any future regression that adds a sibling-directory injection.
- The Epic 3 completion note in the Dev Agent Record correctly defers `epic-3` status promotion to the optional retrospective story (or a tech-lead manual transition). Story 3.5 reaching `done` does NOT itself transition `epic-3` to `done`.

**Sprint status sync:** `_bmad-output/implementation-artifacts/sprint-status.yaml`: `3-5-retrieve-conversation-start-setup`: `review` → `done`.
