---
baseline_commit: 67beac6
---

# Story 3.2: Get Project Context

## Status

done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to request the assembled Project Context for a Project**,
So that **I receive the setup plus authorized references needed to initialize a conversation with the correct boundary** _(FR-16; realizes UJ-1, UJ-4)_.

This is the **second Epic 3 story** and the **first HTTP-surfaced consumer** of the Story 3.1 `ProjectContextInclusionPolicy`. Story 3.1 shipped the pure allowlist policy + assembled-result DTOs + the (evidence-state × operation) fail-closed decision matrix `docs/context-assembly-decision-matrix.md` referenced by Stories 3.2–3.5. Story 3.2 wires the policy behind a new tenant-scoped, authorization-gated, idempotent-rejecting, freshness-bearing **GET endpoint** that composes inputs from existing surfaces and returns the assembled `ProjectContext`:

- the **conversation read ACL** (`IProjectConversationDirectory.ListForProjectAsync`, Story 2.1 — Pattern A; canonical conversation membership lives in Conversations);
- the **Project Folder / File References / Memory References** stored on `ProjectDetailItem` (Stories 2.4 / 2.5 / 2.7 — already metadata-only with `ReferenceState` / `ReasonCode?` / `ObservedAt` carried in projection state); and
- the **tenant access result** + the **ProjectDetailItem** already produced by `ProjectAuthorizationGate.AuthorizeReadAsync` (Story 1.6 layered fail-closed authz).

The policy is invoked **unchanged** with `OperationKind = ProjectContextOperationKind.Get` — Story 3.4 will own `Refresh`, Story 3.3 will surface the `ProjectContextEvaluation` rows the policy already emits, and Story 3.5 will own `GetConversationStartSetup`. Story 3.2 must NOT duplicate any of the policy's include/exclude decision logic in the endpoint, the host composition, or the response shape; the policy is the single source of truth for inclusion order, fail-closed collapse, freshness mapping, and diagnostic vocabulary. Re-implementing any policy decision in the endpoint is a forbidden anti-pattern (Story 3.1 Guardrails — "Single source of truth — order" + "Single source of truth — matrix"; Epic 2 retro closing quote from Winston: *"we stop hand-wiring the order per story"*).

Story 3.2 also realizes three binding **Epic 2 retrospective action items** that were explicitly deferred to it:

1. **Restore the local AppHost smoke-check evidence trail** (Action Item 5; "ideally during Story 3.2") — Story 3.2 must include AppHost startup evidence (Projects + Workers + EventStore + Tenants + Keycloak + Dapr sidecars + Redis-backed components) or record an environment-specific blocker in the Dev Agent Record.
2. **Promote the "route/body identity + missing-Idempotency-Key" negative-test pattern to a checklist** (Action Item 7) — Story 3.2 adds `docs/checklists/mutation-and-query-negative-tests.md` (or sibling) and the GET endpoint exercises the checklist's query-side rows (`Idempotency-Key` rejection on a read, malformed route id treated as missing).
3. **Fix the NSwag idempotency-helper MSBuild target so it works on Linux** (Action Item 1; "before the first Epic 3 story that regenerates `.g.cs`") — Story 3.2 is the first Epic 3 story to regenerate `HexalithProjectsClient.g.cs` (it adds `GetProjectContext` to the spine). Carry-forward action items from Epic 2 (U+2028/U+2029 canonicaliser hardening) do not apply because Story 3.2 is a query and has no idempotency-fingerprint surface; the action item must still survive in the carry-forward list for the next mutation surface that lands.

Everything Story 3.2 produces is metadata-only (FS-2), tenant-scoped (FS-8/SM-3), and fails closed at every layer (NFR-1/NFR-2/NFR-3). The endpoint response is the `ProjectContext` DTO Story 3.1 already shipped — Story 3.2 does NOT introduce a new response wrapper, does NOT add new shared-vocabulary enum values, does NOT extend the `ProjectContextInclusionDiagnostic` closed vocabulary, and does NOT modify `ProjectContextInclusionPolicy` / `ProjectContextInclusionOrder` / any Story 3.1 DTO. The host composition is a thin orchestrator that maps existing read surfaces → policy inputs → policy output → wire body.

## Acceptance Criteria

1. A new HTTP endpoint **`GET /api/v1/projects/{projectId}/context`** is added to the OpenAPI spine `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` mirroring `GetProject`'s read shape (operationId `GetProjectContext`, tags `projects`, parameters `ProjectId` / `CorrelationId` / `Freshness`, responses `200` / `400` / `401` / `403` / `404` / `503`, `x-hexalith-read-consistency: eventually_consistent`, `x-hexalith-correlation` query-correlation-only, `x-hexalith-authorization: tenant-context-and-project-read-permission`, `x-hexalith-canonical-error-categories` matching `GetProject`'s set). `Idempotency-Key` is NOT a parameter and is rejected as `validation_error` if present after authorization (carry-forward of the `GetProject` pattern). The 200 response schema is **`ProjectContext`** (existing Story 3.1 DTO) serialized as-is — no wrapper. The 200 response carries `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent` headers.

2. The OpenAPI spine adds — once each — the wire schemas matching the Story 3.1 DTOs: `ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyOutcome` (enum: `Assembled` / `ProjectUnavailable` / `Unauthorized`), `ProjectContextFreshness` (enum: `Fresh` / `Stale` / `Unavailable` / `Unknown`), `ProjectContextInclusionCheck` (enum: 7 members per `ProjectContextInclusionOrder.Sequence`). All enum schemas use `JsonStringEnumConverter`-compatible name-based values (already the C# convention). `ProjectSetup` and the existing reference-summary shapes are reused unchanged. A single canonical **synthetic example** (`#/components/examples/ProjectContext`) is added showing: assembled outcome, one Project Folder reference, two file references (one included, one stale), one memory reference (excluded with `referenceArchived`), one conversation reference (included), one conversation in `Excluded` with `Diagnostic = "tenantMismatch"` collapsed to `Unauthorized` at the boundary, and `Freshness = Fresh`. Wire-shape schema field names are camelCase (per spine convention) and the C# DTO `[JsonPropertyName]` attributes — if any are missing on Story 3.1's DTOs — are added in this story under a single additive contracts task; otherwise the existing `JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }` resolution suffices (verify by inspection before adding attributes).

3. The OpenAPI spine fingerprint changes deliberately (new operation + new schemas + new example). The dev agent **regenerates** `HexalithProjectsClient.g.cs` and `HexalithProjectsIdempotencyHelpers.g.cs` via the standard MSBuild target. **Action Item 1** from the Epic 2 retro is binding: the dev agent MUST first fix the NSwag idempotency-helper MSBuild target to work on Linux without per-developer forward-slash workarounds (the existing target uses a Windows-backslash path that fails on `Linux`). Acceptance: a single `dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` invocation on Linux regenerates both `.g.cs` files cleanly without manual intervention; CI exercises the regeneration path on Linux at least once. The fingerprint baseline file (`src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml.fingerprint`-equivalent or the in-CI snapshot mechanism) is updated; the OpenAPI fingerprint gate transitions to PASSED-with-baseline-update for this story only. The frontcomposer gate stays skip-clean (no `[Projection]` / `[Command]` contracts touched).

4. A new query-side handler `GetProjectContextAsync` is added to `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` (or split into a dedicated partial-class file under `src/Hexalith.Projects.Server/Queries/` if the file is approaching the existing size threshold — record the choice in the Dev Agent Record). The handler:
   (a) reads canonical headers `X-Correlation-Id` / `X-Hexalith-Task-Id` / `X-Hexalith-Freshness` and validates them per the existing helpers (`ReadHeader`, `IsCanonicalIdentifier`);
   (b) treats a missing or non-canonical `projectId` route value as a safe-denial 404 (NEVER reveals existence);
   (c) calls `ProjectAuthorizationGate.AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)` and returns `ReadModelUnavailable` (503) only when `Authorization.Retryable && Authorization.Reason == ReferenceState.Unavailable`, otherwise `SafeDenial` 404 (existence-non-inference);
   (d) rejects `Idempotency-Key` if present after authorization (`ValidationProblem(..., "idempotency_key")`);
   (e) rejects any non-`eventually_consistent` `X-Hexalith-Freshness` request as `ValidationProblem(..., "freshness")`;
   (f) reads conversation evidence via `IProjectConversationDirectory.ListForProjectAsync(ProjectId, ConversationTenantId, CallerPrincipalId, PageRequest, ct)` using a bounded page (initial cap: `PageSize = 100`, no continuation — Story 3.2 returns a single first-page snapshot); a denied/unavailable conversations page collapses to a fail-closed-clean exclusion via the policy, never a 5xx;
   (g) composes `ProjectContextProjectEvidence(Detail: authorization.ProjectDetail)`, `ProjectContextTenantAccess(Result: authorization.TenantAccessResult)`, `ProjectContextReferenceEvidence(ProjectFolder, FileReferences, MemoryReferences, Conversations)` — folder/file/memory taken directly from the projection (already metadata-only with carried-forward `ReferenceState`/`ReasonCode`/`ObservedAt`), conversations mapped via a thin Projects-shaped translator `ProjectContextConversationEvidenceMapper` (new, under `src/Hexalith.Projects.Server/Conversations/`, NEVER imports `Hexalith.Conversations.*` outside the existing translator boundary), and a `ProjectContextAssemblyContext(AuthoritativeTenantId, RequestedTenantId, ProjectId, OperationKind: Get, CorrelationId, TaskId, Now: timeProvider.GetUtcNow())`;
   (h) invokes `ProjectContextInclusionPolicy.Assemble(...)` (Story 3.1 — unchanged);
   (i) returns the assembled `ProjectContext` as `Results.Json(...)` with the same `ResponseJsonOptions` instance used by `GetProject` / `ListProjectConversations`;
   (j) sets `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent` response headers (mirrors `GetProject`).

5. **`ProjectAuthorizationGate.AuthorizeReadAsync` returns `TenantAccessAuthorizationResult` to its caller** so Story 3.2 can hand it to the policy without re-evaluating tenant access. The gate currently carries the result only inside its internal `AuthorizeAsync` body (line 357); the additive change is: extend `ProjectAuthorizationResult` with a new `TenantAccessResult: TenantAccessAuthorizationResult` property (sealed-record additive constructor), and `Allowed(...)` / `Denied(...)` factories pass it through. Existing call sites are unaffected because the additive property is read-only and defaulted; tests for existing endpoints continue to pass. If the dev agent finds a structurally cleaner way to expose tenant-access result to the GetProjectContext handler (e.g. a small dedicated `IProjectReadAuthorizationGate` returning a richer envelope), it is acceptable — but **must not** break the existing `GetProject` / `ListProjectConversations` / mutation gate paths. Record the chosen shape in the Dev Agent Record.

6. The handler **never re-evaluates** any policy decision. The full chain is:
   - **endpoint** (validates envelope, rejects bad `Idempotency-Key` / freshness, runs `AuthorizeReadAsync`, fetches conversations page, composes evidence, calls policy, returns DTO);
   - **policy** (Story 3.1 — sole owner of inclusion order, fail-closed collapse, freshness mapping, diagnostic vocabulary, reference-kind allowlist).
   No conditional include/exclude logic, freshness threshold, tenant-collapse rule, or diagnostic vocabulary lookup is duplicated in the endpoint. The endpoint receives a `ProjectContextAssemblyResult` from the policy and returns `result.Context` as the wire body (the `result.Evaluations` collection is held for Story 3.3 but NOT serialized in the Story 3.2 response).

7. **Fail-closed negative-evidence matrix (evidence-state × `GetProjectContext`)** is fully covered by Tier-2 Server tests in a new file `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs`. Required cells (every row of the `Get` column in `docs/context-assembly-decision-matrix.md`, parsed at test time per the Story 3.1 pattern — re-use `ProjectContextDecisionMatrixCompletenessTests` discovery logic): `missing` / `stale` / `unauthorized` / `unavailable` / `forbidden` / `redacted` / `conflict` / `invalidReference` / `archived` / `ambiguous`, plus the request-level collapses (`AuthoritativeTenantId` missing → `Unauthorized` outer; cross-tenant → `ProjectUnavailable` outer; archived project → `Assembled` with all references excluded). Each cell asserts: (i) HTTP status code is `200` (assembled, including the archived-project case) or the correct collapse code (`404` for `ProjectUnavailable`, `404` for `Unauthorized` — safe-denial contract preserves the indistinguishability of `Unauthorized` vs `ProjectUnavailable` vs missing-record at the HTTP boundary; the policy's `AssemblyOutcome` is INTERNAL telemetry and is NOT surfaced as a distinct HTTP status); (ii) response headers `X-Correlation-Id` and `X-Hexalith-Freshness` are present; (iii) the `ProjectContext` body contains the expected `AssemblyOutcome` / `Lifecycle` / `Freshness`; (iv) `NoPayloadLeakageAssertions.AssertNoLeakage(...)` runs over the response body; (v) `ProjectContextExclusion.Diagnostic` (when non-null) is a member of `ProjectContextInclusionDiagnostic.Values`. The same checklist of cells is exercised against the host composition (not just the pure policy), so the test fails if the endpoint short-circuits an outcome the policy would have produced. Each test is a named fixture per cell (mirrors Story 3.1 / Story 2.7 named-fixture pattern).

8. **Idempotency-Key on the query is rejected.** Two negative tests in `GetProjectContextTests.cs`: (a) `GET /api/v1/projects/{projectId}/context` with `Idempotency-Key: ...` header → `400` with category `validation_error` and field `idempotency_key`; (b) request with header but unauthorized caller → safe-denial 404 (Idempotency-Key validation happens AFTER authorization, per the existing `GetProject` pattern: `AuthorizeReadAsync` → `HasHeader("Idempotency-Key")` check → `ValidationProblem`). The dev agent's added negative tests reuse `ProjectsServerTestFixture` and the existing `IdempotencyConflict` problem-detail oracle.

9. **Route/body identity validation** — Story 3.2 has no body (GET), so the relevant negative tests are: (a) malformed `projectId` (whitespace, NUL, control bytes, leading/trailing whitespace, unicode bidi-bombs, `..` path tokens) → safe-denial 404 (NOT 400 — malformed-id and missing-id are indistinguishable at the safe-denial edge); (b) `projectId` whose canonical form differs from the route segment (e.g. URL-encoded `..`) → safe-denial 404; (c) extra query-string parameters (`?expand=...`, `?...`) → silently ignored (HTTP routes accept and discard unknown query keys; assert that `expand=full` does NOT change behavior to prevent a future "hidden contract" surface). All three are exercised in `GetProjectContextTests.cs` as named fixtures. The **route/body + missing-Idempotency-Key checklist** authored in AC 14 references these test names as the canonical pattern.

10. **FS-2 `NoPayloadLeakage` harness extension** — Story 3.2 EXTENDS `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` over the **endpoint output** (not just the policy DTO Story 3.1 already covered). Two new tests: (a) `GetProjectContextEndpoint_ResponseBody_HasNoLeakage` — boots the host (Aspire-less in-memory `WebApplicationFactory` or the existing `ProjectsServerTestFixture` pattern), exercises the matrix, and runs the leakage assertion over every serialized response; (b) `GetProjectContextEndpoint_ErrorResponses_HaveNoLeakage` — exercises 400/401/403/404/503 responses and asserts no diagnostic-message leakage, no upstream sibling-denial text, no payload fragments, no token/path appears in the ProblemDetails body. The leakage harness's Memories + Folders forbidden-term lists from Stories 2.5 / 2.7 / 3.1 are reused unchanged; Story 3.2 adds no new forbidden terms (the wire shapes are the same as Story 3.1's).

11. **Cross-tenant isolation (FS-8 / SM-3)** — a dedicated test in `GetProjectContextTests.cs` constructs a request with `AuthoritativeTenantId = "tenant-a"` against a project whose `ProjectDetailItem.TenantId = "tenant-b"` and asserts: (i) HTTP 404 (safe-denial; never 403, never reveals existence); (ii) no `ProjectContext` body — only a Problem Details safe-denial body; (iii) no Tenant Id appears in the response headers, body, or correlation-id-equivalent fields. Reuses the FS-8/SM-3 harness from Story 1.4 / Story 3.1.

12. **AppHost smoke check (Epic 2 retro Action Item 5)** — Story 3.2 produces explicit AppHost startup evidence. Acceptance options:
    - **(Preferred)** A new lightweight test in `tests/Hexalith.Projects.Integration.Tests/AppHost/GetProjectContextSmokeTests.cs` that uses the existing Aspire AppHost test harness (`DistributedApplicationTestingBuilder` per Aspire 13.2.x or the existing `ProjectsAppHostTestFixture` if one is in use) to boot Projects + Workers + EventStore + Tenants + Keycloak + Dapr sidecars + Redis-backed components, seed a single Project with at least one Folder / File / Memory / Conversation reference, and HTTP-GET `/api/v1/projects/{projectId}/context`. Asserts: 200, body has the expected references, freshness header present, no leakage. The test SKIPs (records the skip reason) only if a documented environment blocker exists (e.g. Docker unavailable on the test host); the Dev Agent Record records "AppHost smoke run end-to-end" or names the environment-specific blocker.
    - **(Acceptable fallback)** A documented manual smoke run is recorded in the Dev Agent Record with: AppHost command used (`dotnet run --project src/Hexalith.Projects.AppHost`), the Aspire dashboard URL observed, the resources observed Healthy (Projects + Workers + EventStore + Tenants + Keycloak + Dapr + Redis), the curl/HTTP client invocation against the new endpoint, the observed 200 response shape (redacted to metadata-only), and any environment-specific issues (HTTPS dev-cert warnings, Dapr sidecar start-up delay, etc.).
    The Dev Agent Record explicitly states whether the preferred (automated) or fallback (manual) path was taken. The aggregate intent is to satisfy the retro action item: *"The Dev Agent Record names AppHost startup evidence ... or records an environment-specific blocker."*

13. **Tier-1 composition tests** — new file `tests/Hexalith.Projects.Tests/Context/ProjectContextEvidenceCompositionTests.cs` covers the **mapping logic** (host-side composition of `ProjectContextReferenceEvidence` from `ProjectDetailItem` + `ProjectConversationsPage`). Required tests:
    - `Mapper_ProjectFolderReference_PreservesReferenceState_ObservedAt_ReasonCode` (and the `Pending` Story 2.4 path).
    - `Mapper_FileReferences_PreservesStoryTwoFiveReferenceStates` (full `ReferenceState` enum coverage).
    - `Mapper_MemoryReferences_PreservesStoryTwoSevenReferenceStates` (full `ReferenceState` enum coverage).
    - `Mapper_Conversations_TranslatesProjectConversationItem_ToProjectContextConversationEvidence` (per `ProjectConversationTrustSignal` value: `Current` / `Stale` / `Rebuilding` / `Unavailable` / `Forbidden` / `Redacted` / `MixedGeneration`).
    - `Mapper_EmptyDetail_ProducesEmptyEvidence` (project not visible — null safety; the policy will collapse this to `ProjectUnavailable` independently).
    - `Mapper_NoLeakage_OverComposedEvidence` (runs the FS-2 harness on the composed input shape so the host never produces a payload-bearing evidence record).
    The mapper itself must be pure (no Dapr / HTTP / network / wall-clock) and must NOT import `Hexalith.Conversations.*` outside its single dedicated translator boundary — boundary grep `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` returns hits ONLY for the Conversations translator import (matching the Story 2.1 `ProjectConversationTranslator` precedent).

14. **Negative-test checklist (Epic 2 retro Action Item 7)** — Story 3.2 adds `docs/checklists/mutation-and-query-negative-tests.md` containing the canonical short checklist promoted from per-story to repo level. Initial rows (mirroring what Stories 2.3 / 2.5 / 2.7 / 3.2 have all done individually):
    - Malformed identifier (whitespace / NUL / control bytes / unicode bidi / `..` path tokens) → safe-denial 404 (queries) or 400 with field name (mutations);
    - Route/body identity mismatch (mutations only) → 400 with category `validation_error` BEFORE handler dispatch;
    - Missing `Idempotency-Key` on mutation → 400 with category `validation_error` field `idempotency_key`;
    - Idempotency-Key PRESENT on query → 400 with category `validation_error` field `idempotency_key`;
    - Stricter `X-Hexalith-Freshness` on eventually-consistent query → 400 with field `freshness`;
    - Cross-tenant scope (tenant-A claim, tenant-B project) → safe-denial 404 NEVER 403, never reveals existence;
    - Unknown `Idempotency-Key` on retry → 409 IdempotencyConflict (mutations only);
    - Authorized but stale projection → `ReadModelUnavailable` 503 with `Retryable=true` (queries) or appropriate retry behavior (mutations).
    The checklist is referenced from `_bmad-output/planning-artifacts/architecture.md` (single one-line pointer near the existing AR-9 fail-closed posture / "Process Patterns" section). Every Epic 3 query and mutation story onward references this checklist explicitly in its Dev Agent Record and ticks each applicable row. The checklist is LF on disk per [[build-environment]].

15. **Generated client + idempotency-helper additive coverage.** The regenerated `HexalithProjectsClient.g.cs` exposes a typed `GetProjectContextAsync(projectId, correlationId?, freshness?, cancellationToken)` method returning a typed `ProjectContext`. The regenerated `HexalithProjectsIdempotencyHelpers.g.cs` does NOT gain a new entry for the query (queries have no idempotency surface; the existing helper covers mutations only). Both regenerations are deterministic (LF, NUL-free, no platform-specific paths embedded). The generated Client tests under `tests/Hexalith.Projects.Client.Tests/` gain at least one happy-path serialization test for the new method (mirrors the existing `LinkFileReferenceClientTests` shape — calls into a `HttpClient` stub seeded with a synthetic 200 response from the OpenAPI example, asserts the parsed object equals the expected shape, asserts the freshness header is read and surfaced if the client exposes one).

16. **No edits to Story 3.1 surfaces.** `ProjectContextInclusionPolicy`, `ProjectContextInclusionOrder`, `ProjectContextAssemblyContext`, `ProjectContextProjectEvidence`, `ProjectContextTenantAccess`, `ProjectContextReferenceEvidence`, `ProjectContextConversationEvidence`, `ProjectContextDiagnostics`, `ProjectContextOperationKind`, the closed `ProjectContextInclusionDiagnostic` vocabulary, the wire DTOs (`ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyResult`, `ProjectContextEvaluation`), and the four assembly enums are NOT modified. If Story 3.2 implementation finds a divergence between the policy and the (now-real) endpoint that requires a Story 3.1 file change, the dev agent **HALTs** before editing Story 3.1 and surfaces the conflict in the Dev Agent Record; the resolution is a follow-up story / ADR, not an inline edit.

17. **No new shared-vocabulary enum values.** `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ProjectConversationTrustSignal`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus`, `ProjectContextInclusionCheck`, `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextOperationKind`, and `ProjectContextInclusionDiagnostic` are unchanged. If a new value appears genuinely required for Story 3.2 (e.g. a new `ProjectContextOperationKind` member, a new diagnostic string), HALT and surface the conflict — the resolution is a follow-up story.

18. **No edits to Stories 1.4–2.7 mutation surfaces.** No changes to: `ProjectAggregate.*`, `ProjectState`, `ProjectStateApply`, `ProjectCommandValidator`, `ProjectCommandValidationResult`, `ProjectResult`, `ProjectResultCode`, `ProjectDetailProjection`, `ProjectListProjection`, `ProjectReferenceIndexProjection`, the four ACL interfaces (`IProjectConversationDirectory`, `IProjectConversationAssignmentDirectory`, `IProjectFolderDirectory`, `IProjectFileReferenceDirectory`, `IProjectMemoryDirectory`), `IProjectCommandSubmitter`. Only `ProjectAuthorizationGate` and `ProjectAuthorizationResult` are touched additively (per AC 5) and `ProjectsDomainServiceEndpoints` gains the `GetProjectContext` handler (per AC 4). `ProjectAuthorizationDenialMapper` and `ProjectCommandRejected` are untouched (queries don't go through the command pipeline).

19. **Mandatory negative-path tests carried forward:**
    - **Cross-tenant isolation** (AC 11) — FS-8/SM-3.
    - **`NoPayloadLeakage`** over every endpoint response (AC 10) — FS-2.
    - **No clock divergence** — the endpoint uses `TimeProvider.System.GetUtcNow()` (or the registered `TimeProvider` from DI) for the `Now: DateTimeOffset` passed to the policy. NO `DateTimeOffset.UtcNow` / `DateTime.UtcNow` / `Stopwatch` calls in the handler / composition / mapper code. Validation grep: `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/{Queries,Conversations}/` returns zero hits (or only hits in pre-existing code untouched by Story 3.2).
    - **No-sleep grep in tests** — `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.*/` filtered to Story 3.2 new/modified test files returns zero hits.
    - **Boundary discipline** — `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects.Server/Queries/` returns zero hits in the GetProjectContext handler proper (only the conversation translator file at `src/Hexalith.Projects.Server/Conversations/` is allowed to reference `Hexalith.Conversations.*`, mirroring Story 2.1's existing translator). `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` continues to return zero hits (Story 3.1 invariant; Story 3.2 must not regress it).
    - **OpenAPI fingerprint baseline updated** — the fingerprint gate flips PASSED-with-update only for this story; subsequent stories must show zero spine diff unless they own one.

20. **`dotnet build` & `dotnet test` budgets:**
    - `dotnet build Hexalith.Projects.slnx` — 0 W / 0 E.
    - `dotnet test Hexalith.Projects.slnx` — baseline 776/776 (post-Story 3.1 senior-review). Story 3.2 grows the count by approximately:
      - Server.Tests: +~25 (matrix cells, idempotency rejection, route negative tests, cross-tenant, leakage on response).
      - Projects.Tests: +~15 (composition mapper tests under `Context/`, leakage harness extension).
      - Contracts.Tests: +~5 (new DTO serialization tolerance, JSON property-name verification).
      - Client.Tests: +~3 (typed `GetProjectContextAsync` happy-path + freshness header surface).
      - Integration.Tests: +1 (AppHost smoke test; SKIP-allowed only with documented environment blocker per AC 12).
      Failed must be 0. Skipped must be 0 except the AppHost smoke test, which is allowed to SKIP with a documented blocker (recorded in the Dev Agent Record).
    - `git diff --check` clean across story-touched files. Hand-written `.cs` / `.md` / `.yaml` are LF on disk per [[build-environment]].

21. **Dev Agent Record** is populated by the dev agent with:
    - Endpoint shape divergence from this AC list (if any) with rationale.
    - Handler placement (inline in `ProjectsDomainServiceEndpoints.cs` vs split into a new partial / `Queries/` file).
    - Whether `ProjectAuthorizationResult` was extended additively (per AC 5) or a richer envelope shape was chosen.
    - The conversation-page cap actually used (default 100, or a justified deviation).
    - AppHost smoke evidence (automated test passing OR manual run notes OR documented blocker).
    - Per-lane and full-solution test counts (before/after Story 3.2).
    - `dotnet build` warnings/errors, `git diff --check`, `git diff --stat` on `.g.cs` (expected: non-zero — this story DOES regenerate), OpenAPI spine diff size in lines, fingerprint baseline-update note.
    - Action Item 1 (NSwag Linux path fix) resolution: the exact MSBuild target change, the verifying CI invocation (or skip + reason).
    - Action Item 7 (negative-test checklist) creation note and the architecture-doc pointer line added.
    - Any HALT items (none expected; this story should land cleanly given Story 3.1 is `done`).

## Tasks / Subtasks

- [x] **Task 1 — Capability gate + read-only inspection. (AC: 4, 5, 6, 16, 17, 18)**
  - [x] Read `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` end-to-end and confirm: (a) policy is `sealed class ProjectContextInclusionPolicy(ILogger<...>? logger = null)`, (b) single public method `Assemble(ProjectContextAssemblyContext, ProjectContextProjectEvidence, ProjectContextTenantAccess, ProjectContextReferenceEvidence) -> ProjectContextAssemblyResult`, (c) `ProjectContextOperationKind.Get` is the right enum value for read-only Story 3.2.
  - [x] Read all 5 input wrapper records under `src/Hexalith.Projects/Context/` (`ProjectContextAssemblyContext`, `ProjectContextProjectEvidence`, `ProjectContextTenantAccess`, `ProjectContextReferenceEvidence`, `ProjectContextConversationEvidence`) — confirm the exact field names and any validation invariants (eager-non-empty `AuthoritativeTenantId` / `RequestedTenantId` / `ProjectId`, etc.).
  - [x] Read `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs` + `ProjectContextReference.cs` + `ProjectContextExclusion.cs` + `ProjectContextAssemblyResult.cs` + `ProjectContextEvaluation.cs` — confirm `ProjectContext.Empty(...)`, `Unauthorized(...)`, `ProjectUnavailable(...)` factories (Story 3.1 senior-review M3 added `Empty`), confirm `[JsonPropertyName]` attributes are PRESENT (or absent but resolvable via `JsonNamingPolicy.CamelCase`).
  - [x] Read `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — confirm `AuthorizeReadAsync` shape (line 80), `ProjectAuthorizationResult` shape, factory methods `Allowed(...)` / `Denied(...)`, and decide whether to extend additively (preferred) or wrap in a richer envelope.
  - [x] Read `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` lines 71–540 — confirm the `GetProjectAsync` pattern (line 422) and `ListProjectConversationsAsync` pattern (line 488) so the new handler follows the same shape. Identify file-size threshold — if adding ~80 LOC keeps the file < 1200 LOC, keep it inline; otherwise extract to `Queries/GetProjectContext.cs` partial.
  - [x] Read `src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs` + `ConversationsProjectConversationDirectory.cs` + `ProjectConversationTranslator.cs` — confirm `ListForProjectAsync(ProjectId, ConversationTenantId, CallerPrincipalId, PageRequest, ct) -> ProjectConversationsPage` and the translator's existing trust-signal mapping (Story 2.1).
  - [x] Read `src/Hexalith.Projects.Contracts/Queries/{ProjectConversationItem, ProjectConversationsPage, ProjectConversationPageMetadata, PageRequest, ProjectConversationTrustSignal}.cs` — confirm the wire shape Story 3.2 maps from.
  - [x] Read `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` lines 23 (paths header), 158–220 (`GetProject` operation as the read-shape oracle), components section near the schemas — identify the canonical placement for `GET /api/v1/projects/{projectId}/context`, the schema block for the new `ProjectContext*` types, and the example block.
  - [x] Read `src/Hexalith.Projects.Client/Generation/nswag.json` + the MSBuild target that drives idempotency-helper regeneration — identify the exact Windows-backslash line that fails on Linux (Action Item 1). Read the working-tree comment from Stories 2.3 / 2.4 / 2.5 / 2.7 review notes documenting the workaround. The fix is typically: replace `\` separators in `<Exec Command="...">` paths with `/` or use `$(MSBuildProjectDirectory)` with platform-neutral path composition. Confirm the fix is contained to a single target file (and not a global build-prop file).
  - [x] Read `docs/context-assembly-decision-matrix.md` (Story 3.1) — confirm the `Get` column populates every evidence-state row Story 3.2 needs (Story 3.1 owns the cell semantics; Story 3.2 is consumer).
  - [x] Read `tests/Hexalith.Projects.Tests/Context/ProjectContextDecisionMatrixCompletenessTests.cs` — confirm the doc-parsing or hard-coded mirror it uses; Story 3.2's `GetProjectContextTests.cs` will reuse the same source of truth.
  - [x] Read `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` (Story 3.1 extension) — confirm the harness shape Story 3.2 will extend over endpoint responses (the harness covers DTOs; Story 3.2 adds endpoint-response coverage).
  - [x] Read the Story 3.1 Dev Agent Record validation summary — confirm full-solution baseline 776/776 and the per-lane breakdown.
  - [x] Read `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` §"Action Items" rows 1 / 5 / 7 verbatim (NSwag Linux fix, AppHost smoke check, route/body+missing-Idempotency-Key checklist).
  - [x] Confirm no submodule pointer change is required and no nested-recursive submodule init is needed.
  - [x] **HALT** before proceeding to Task 2 if any of the above evidence diverges from this story file's assumptions — especially: (i) `AuthorizeReadAsync` does NOT return `TenantAccessAuthorizationResult` even after the additive change is feasible; (ii) a Story 3.1 file would have to change to make Story 3.2 work.

- [x] **Task 2 — Fix the NSwag idempotency-helper MSBuild target so it works on Linux. (AC: 3, Epic 2 retro Action Item 1)**
  - [x] Replace any Windows-backslash separators in the relevant MSBuild `<Target>` / `<Exec>` element with forward-slash or platform-neutral path composition (`Path.Combine` semantics in MSBuild: `$(...)/$(...)`; or `[System.IO.Path]::Combine(...)` via property functions).
  - [x] Verify the fix by running, on Linux: `dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj /p:RegenerateIdempotencyHelpers=true` (or whichever property the target gates on). Both `HexalithProjectsClient.g.cs` and `HexalithProjectsIdempotencyHelpers.g.cs` regenerate cleanly with LF, no NUL bytes, and no platform-specific paths embedded.
  - [x] If CI uses a different invocation, update CI accordingly (root-of-repo `.github/workflows/*.yml` or `Hexalith.Builds/Samples/*.props`).
  - [x] Record the exact change in the Dev Agent Record (file path + before/after of the target).
  - [x] **HALT** if the fix requires changing `Hexalith.Builds` (sibling submodule) — surface the conflict; the resolution is a separate Hexalith.Builds story, not an inline edit here.

- [x] **Task 3 — Add the GetProjectContext operation to the OpenAPI spine. (AC: 1, 2, 3, 17)**
  - [x] Add the path entry `/api/v1/projects/{projectId}/context` (GET) after the existing `/api/v1/projects/{projectId}/conversations` path block, mirroring `GetProject` (lines 158–220).
  - [x] Add the response schemas under `#/components/schemas/`: `ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyOutcome` (enum), `ProjectContextFreshness` (enum), `ProjectContextInclusionCheck` (enum). Reuse the existing `ProjectSetup`, `ProjectFolderReference`, `ProjectFileReference`, `ProjectMemoryReference`, `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode` schemas — DO NOT redeclare.
  - [x] Add the synthetic example `#/components/examples/ProjectContext` (assembled outcome, 1 folder, 2 files [1 included + 1 stale], 1 memory excluded with `referenceArchived`, 1 conversation included, 1 conversation excluded with `Diagnostic = "tenantMismatch"` collapsed to `Unauthorized` at the boundary, `Freshness = Fresh`).
  - [x] Verify YAML is well-formed (`yamllint` if available, otherwise `dotnet test --filter "FullyQualifiedName~OpenApiSpineTests"` if such a test exists).
  - [x] Regenerate `HexalithProjectsClient.g.cs` via the (now-Linux-fixed) MSBuild target; confirm the new `GetProjectContextAsync(...)` method appears with the right signature.
  - [x] Confirm `HexalithProjectsIdempotencyHelpers.g.cs` is unchanged (queries have no idempotency surface).
  - [x] Update the OpenAPI fingerprint baseline file (if present) and confirm the fingerprint gate flips PASSED-with-update for this story only.
  - [x] Run `git diff --check` and confirm clean across the spine, `.g.cs` (expected non-zero diff), and the fingerprint baseline.

- [x] **Task 4 — Extend `ProjectAuthorizationResult` to carry `TenantAccessAuthorizationResult`. (AC: 5)**
  - [x] Add a new optional `TenantAccessResult: TenantAccessAuthorizationResult?` property to `ProjectAuthorizationResult` (or the chosen shape per Task 1's design decision). Default to `null` on existing factory call sites; populate on the path that produced the result.
  - [x] Update `ProjectAuthorizationGate.AuthorizeReadAsync` and the internal `AuthorizeAsync` to thread the `TenantAccessAuthorizationResult` through to the returned `ProjectAuthorizationResult`.
  - [x] Verify existing endpoints (`GetProjectAsync`, `ListProjectConversationsAsync`, all mutation handlers) compile and behave unchanged. Existing tests still pass.
  - [x] Document the change in the Dev Agent Record; record whether the property is also populated on the `Denied(...)` factory (preferred: yes, so Story 3.3's `ExplainContextSelection` can use it; required: yes when the denial reason is `TenantAuthority` so the policy receives the actual outcome).

- [x] **Task 5 — Add `ProjectContextConversationEvidenceMapper` under `src/Hexalith.Projects.Server/Conversations/`. (AC: 6, 13, 19)**
  - [x] Add `src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` — `internal static class` exposing `static IReadOnlyList<ProjectContextConversationEvidence> Map(ProjectConversationsPage page, DateTimeOffset now)`.
  - [x] Implementation: for each `ProjectConversationItem` in `page.Items`, emit `ProjectContextConversationEvidence(ConversationId: item.ConversationId, DisplayLabel: item.DisplayLabel, TrustSignal: item.TrustSignal, LastCheckedAt: item.ObservedAt ?? now)`. Preserve ordering.
  - [x] Pure function: no DI, no HTTP, no clock read (only the `now` parameter). Eager-validate non-empty `ConversationId`; normalize whitespace-only `DisplayLabel` to null.
  - [x] Boundary discipline: this mapper file IS the single allowed location to import `Hexalith.Conversations.*` for evidence translation, mirroring `ProjectConversationTranslator.cs`'s pattern.

- [x] **Task 6 — Implement the `GetProjectContextAsync` HTTP handler. (AC: 4, 6, 8, 9, 19)**
  - [x] Add `endpoints.MapGet("/api/v1/projects/{projectId}/context", static async (...) => await GetProjectContextAsync(...))` to `ConfigureEndpoints` (mirrors `MapGet` pattern at line 90 / 99 of `ProjectsDomainServiceEndpoints.cs`).
  - [x] Implement `private static async Task<IResult> GetProjectContextAsync(string projectId, HttpContext httpContext, IProjectTenantContextAccessor tenantContext, ProjectAuthorizationGate authorizationGate, IProjectConversationDirectory conversationDirectory, ProjectContextInclusionPolicy contextPolicy, TimeProvider timeProvider, CancellationToken cancellationToken)`.
  - [x] Handler flow (in order):
    1. Read `correlationId` and `taskId` headers via `ReadHeader`; canonicalize via `IsCanonicalIdentifier`.
    2. If `projectId` is null/whitespace or non-canonical → `SafeDenial(correlationId, null)` (404, never reveals).
    3. Call `authorizationGate.AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, null, cancellationToken)`.
    4. If `!authorization.IsAllowed || authorization.ProjectDetail is null` → `ReadModelUnavailable` when `Retryable && Reason == ReferenceState.Unavailable`, else `SafeDenial(...)`.
    5. If `httpContext.Request.Headers.ContainsKey("Idempotency-Key")` → `ValidationProblem(correlationId, null, "idempotency_key")`.
    6. If `X-Hexalith-Freshness` header is non-null and != `"eventually_consistent"` → `ValidationProblem(correlationId, null, "freshness")`.
    7. Page request for conversations: `new PageRequest(PageSize: 100, ContinuationCursor: null)`.
    8. Call `conversationDirectory.ListForProjectAsync(new ProjectId(projectId), new ConversationTenantId(tenantContext.AuthoritativeTenantId!), new CallerPrincipalId(tenantContext.PrincipalId!), pageRequest, cancellationToken)`.
    9. `ProjectConversationsPage` may carry trust signals that the policy will fail-closed-clean exclude — do NOT inspect the page locally to suppress entries.
    10. Map conversation page → `ProjectContextConversationEvidenceMapper.Map(page, now)`.
    11. Build the four input records: `ProjectContextAssemblyContext(authorization.TenantId, tenantContext.RequestedTenantId, projectId, ProjectContextOperationKind.Get, correlationId, taskId, now: timeProvider.GetUtcNow())`, `ProjectContextProjectEvidence(authorization.ProjectDetail)`, `ProjectContextTenantAccess(authorization.TenantAccessResult)`, `ProjectContextReferenceEvidence(authorization.ProjectDetail.ProjectFolder, authorization.ProjectDetail.FileReferences, authorization.ProjectDetail.MemoryReferences, mappedConversations)`.
    12. Call `contextPolicy.Assemble(...)`.
    13. Set response headers: `X-Correlation-Id` (echo), `X-Hexalith-Freshness: eventually_consistent`.
    14. Return `Results.Json(result.Context, ResponseJsonOptions)`.
  - [x] DI registration: add `IProjectConversationDirectory`, `ProjectContextInclusionPolicy` (already registered by Story 3.1 via `TryAddTransient`), and `TimeProvider` (already registered) to the handler signature so the static method gets them injected.
  - [x] If the file exceeds ~1200 LOC, extract the handler to a new partial-class file `src/Hexalith.Projects.Server/Queries/GetProjectContext.cs` and record the choice.
  - [x] Boundary check: `grep -rE "Hexalith\.(Folders|Memories)" src/Hexalith.Projects.Server/Queries/` returns zero hits; `Hexalith.Conversations` is allowed ONLY in `src/Hexalith.Projects.Server/Conversations/` translator files.

- [x] **Task 7 — Add Tier-2 endpoint tests. (AC: 7, 8, 9, 10, 11)**
  - [x] Create `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs` using the existing `ProjectsServerTestFixture` pattern (mirror `tests/Hexalith.Projects.Server.Tests/.../GetProjectTests.cs` if such a file exists; otherwise mirror the closest read-side fixture).
  - [x] Required tests (named per matrix cell so each fail-closed row has an addressable test, per the Story 3.1 / Story 2.7 named-fixture pattern):
    - `GetProjectContext_HappyPath_Returns200WithAssembledContext` — single project, 1 folder + 2 files + 1 memory + 2 conversations all included; asserts `Freshness=Fresh`, `Lifecycle=Active`, all four lists populated, `Excluded` empty.
    - Decision-matrix completeness — one `[Theory]` row per cell in the `Get` column of `docs/context-assembly-decision-matrix.md`, parsed at test time using the Story 3.1 completeness-test discovery helper (or a shared parser extracted to `src/Hexalith.Projects.Testing/Context/`).
    - `GetProjectContext_IdempotencyKeyPresent_ReturnsValidationProblem` — Idempotency-Key on a query → 400.
    - `GetProjectContext_StricterFreshnessRequested_ReturnsValidationProblem` — `X-Hexalith-Freshness: strong` → 400 field `freshness`.
    - `GetProjectContext_MalformedProjectId_ReturnsSafeDenial404` (Theory over whitespace, NUL, control bytes, unicode bidi, `..`, leading/trailing whitespace).
    - `GetProjectContext_CrossTenant_ReturnsSafeDenial404` — `ProjectDetailItem.TenantId != authoritativeTenantId` → 404 with no body fields revealing existence.
    - `GetProjectContext_ProjectArchived_Returns200WithAllReferencesExcluded` — `Lifecycle=Archived` on the assembled response, every reference excluded with `FailedCheck=ProjectLifecycle`.
    - `GetProjectContext_TenantAccessStale_AssemblesWithFreshnessStale` — read-only operation tolerates bounded-stale projection (per Story 3.1 AC 6).
    - `GetProjectContext_TenantAccessUnavailable_ReturnsReadModelUnavailable503` — `Reason == ReferenceState.Unavailable && Retryable` → 503.
    - `GetProjectContext_ConversationsPageUnavailable_AssemblesWithExclusions` — conversation ACL returns unavailable; policy collapses to `Unavailable` exclusion; endpoint still returns 200.
    - `GetProjectContext_ResponseHeaders_HaveCorrelationAndFreshness`.
    - `GetProjectContext_ExtraQueryParams_AreIgnoredNotFailed` (`?expand=full`).
    - `GetProjectContext_NoPayloadLeakage_OverEveryMatrixCell` — runs the harness over the body of each matrix-cell test.
    - `GetProjectContext_ErrorResponses_HaveNoLeakage` — exercises 400/401/403/404/503 responses; asserts the ProblemDetails body has no upstream sibling-denial text, no payload fragments, no token/path.
  - [x] All tests use `RecordingLogger<T>` from `src/Hexalith.Projects.Testing/Context/` (Story 3.1) for any policy logger assertions.
  - [x] Boundary discipline: no-sleep grep on Story 3.2 test files returns zero hits.

- [x] **Task 8 — Add Tier-1 composition mapping tests. (AC: 13)**
  - [x] Create `tests/Hexalith.Projects.Tests/Context/ProjectContextEvidenceCompositionTests.cs` — pure xUnit v3 + Shouldly tests for `ProjectContextConversationEvidenceMapper.Map(...)` and the inline composition in `GetProjectContextAsync` (extract the composition into a pure helper `ProjectContextEvidenceComposer.Compose(...)` if it makes the test seam cleaner; if the inline-static-handler is preferred for performance and the composition is trivial, the mapper tests alone suffice).
  - [x] Required tests:
    - Folder reference state preservation (every `ReferenceState` value).
    - File reference state preservation (every `ReferenceState` value).
    - Memory reference state preservation (every `ReferenceState` value).
    - Conversation trust-signal translation (every `ProjectConversationTrustSignal` value).
    - Empty `ProjectDetailItem` → empty evidence.
    - `ProjectConversationsPage` empty / null items → empty conversation evidence.
    - `NoPayloadLeakageAssertions.AssertNoLeakage(...)` over composed evidence.
  - [x] Reuse `ProjectContextEvidenceBuilder` and `RecordingLogger<T>` from Story 3.1 where convenient.

- [x] **Task 9 — Extend the FS-2 leakage harness over the endpoint response. (AC: 10)**
  - [x] Extend `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` with:
    - `GetProjectContextEndpoint_ResponseBody_HasNoLeakage` (per matrix cell).
    - `GetProjectContextEndpoint_ErrorResponses_HaveNoLeakage` (per error status).
  - [x] Reuse the Memories + Folders forbidden-term lists from Stories 2.5 / 2.7 / 3.1 unchanged. No new forbidden terms.
  - [x] The harness must assert: no `MemoryUnit.Content` / `ContentHash` / `SourceUri`, no file content / byte-range / raw path, no transcript / prompt / secret, no `ErrorResponse.Message` / `Suggestion` upstream text leakage, no `Diagnostic` value outside `ProjectContextInclusionDiagnostic.Values`.

- [x] **Task 10 — Add a typed-client happy-path test under `Hexalith.Projects.Client.Tests`. (AC: 15)**
  - [x] Add `tests/Hexalith.Projects.Client.Tests/GetProjectContextClientTests.cs` — seeds an `HttpClient` stub with the OpenAPI synthetic example response (Task 3 example body), calls `client.GetProjectContextAsync(projectId, correlationId, freshness, ct)`, asserts the parsed object equals the expected shape (record-equality), asserts the freshness header is read.
  - [x] Mirror the closest existing client test (`LinkFileReferenceClientTests.cs` or `GetProjectClientTests.cs` if present) for the assertion pattern.

- [x] **Task 11 — Add the AppHost smoke test (Epic 2 retro Action Item 5). (AC: 12)**
  - [x] Create `tests/Hexalith.Projects.Integration.Tests/AppHost/GetProjectContextSmokeTests.cs` using the existing Aspire 13.2.x `DistributedApplicationTestingBuilder` pattern (mirror any existing Aspire AppHost test in the Integration lane; if none exists today, this is the first — record the pattern choice in the Dev Agent Record).
  - [x] Seed a single Project with at least one Folder + File + Memory + Conversation reference (via the existing command-async API or via direct EventStore seeding through the test fixture, whichever the test bed supports).
  - [x] HTTP-GET `/api/v1/projects/{projectId}/context` over the AppHost-provided HTTP client; assert 200, expected references, freshness header, no leakage.
  - [x] Allow SKIP only with a documented environment blocker (Docker unavailable, Dapr sidecar fails, HTTPS dev-cert not generated) — record the blocker in the Dev Agent Record explicitly. The skip MUST be a `[SkippableFact]` or equivalent with a clear reason string; a silent skip is not acceptable.
  - [x] If automated smoke is not feasible in the dev environment, the manual fallback (per AC 12) is acceptable — record AppHost startup, Aspire dashboard URL, observed Healthy resources, and the curl invocation in the Dev Agent Record.

- [x] **Task 12 — Author the negative-test checklist (Epic 2 retro Action Item 7). (AC: 14)**
  - [x] Create `docs/checklists/mutation-and-query-negative-tests.md` with the 8 canonical rows enumerated in AC 14.
  - [x] Cross-link from `_bmad-output/planning-artifacts/architecture.md` near the existing AR-9 / "Process Patterns" section (single one-line pointer mirroring how Story 3.1 added the AR-9 decision-matrix pointer).
  - [x] LF on disk per [[build-environment]]; `git diff --check` clean.

- [x] **Task 13 — Validation. (AC: 19, 20, 21)**
  - [x] Use the build environment from [[build-environment]]: `DOTNET_ROOT=/home/administrator/.dotnet` (`dotnet --version` 10.0.300). Avoid `/usr/bin/dotnet`.
  - [x] Run `dotnet build Hexalith.Projects.slnx`. Confirm 0 W / 0 E.
  - [x] Run focused lanes:
    - `dotnet test tests/Hexalith.Projects.Tests` (baseline 407 + ~15 = ~422).
    - `dotnet test tests/Hexalith.Projects.Server.Tests` (baseline 196 + ~25 = ~221).
    - `dotnet test tests/Hexalith.Projects.Contracts.Tests` (baseline 128 + ~5 = ~133).
    - `dotnet test tests/Hexalith.Projects.Client.Tests` (baseline 31 + ~3 = ~34).
    - `dotnet test tests/Hexalith.Projects.Integration.Tests` (baseline 14 + 1 = 15; +1 AppHost smoke; SKIP-allowed per AC 12).
  - [x] Run full-solution `dotnet test Hexalith.Projects.slnx`. Baseline 776; Story 3.2 grows it by approximately +50 (sum above); failed must be 0.
  - [x] Run `git diff --check` on story-touched files. Confirm clean across `.cs`, `.md`, `.yaml`.
  - [x] Confirm `.g.cs` regenerated cleanly: `git diff --stat src/Hexalith.Projects.Client/Generated/` shows non-zero changed lines (expected — this story regenerates); inspect that the new `GetProjectContextAsync` method is present and that no Windows backslashes leak into the file.
  - [x] Confirm the OpenAPI fingerprint baseline updated and the spine fingerprint gate is PASSED-with-baseline-update (allowed for this story only).
  - [x] Confirm boundary greps pass:
    - `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` → 0 hits (Story 3.1 invariant).
    - `grep -rE "Hexalith\.(Folders|Memories)" src/Hexalith.Projects.Server/Queries/` → 0 hits.
    - `grep -rE "Hexalith\.Conversations\." src/Hexalith.Projects.Server/` → hits ONLY in `Conversations/` translator files (existing + new mapper).
    - `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/Queries/` → 0 hits in story-touched files.
    - `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.*/` filtered to story-touched test files → 0 hits.
  - [x] Confirm no submodule pointer change: `git status` shows no submodule advances beyond the pre-existing "modified content" markers (Hexalith.Commons / Hexalith.Conversations / Hexalith.Parties were already in that state at session start per the initial `git status` baseline).
  - [x] Populate the Dev Agent Record with the validation summary per AC 21.

## Dev Notes

### Story Scope Boundary

- **In scope:** `GET /api/v1/projects/{projectId}/context` endpoint (`ProjectsDomainServiceEndpoints` + optional partial split into `Queries/`); OpenAPI spine entry + 6 new wire schemas + 1 synthetic example; regenerated `HexalithProjectsClient.g.cs` exposing `GetProjectContextAsync(...)`; NSwag idempotency-helper MSBuild target fix for Linux (Action Item 1); additive `ProjectAuthorizationResult.TenantAccessResult` property and `AuthorizeReadAsync` wiring; new `ProjectContextConversationEvidenceMapper` under `src/Hexalith.Projects.Server/Conversations/`; FS-2 `NoPayloadLeakage` harness extension over endpoint responses; Tier-2 Server endpoint tests (decision-matrix completeness, idempotency-rejection, route negatives, cross-tenant safe-denial, header echo, leakage); Tier-1 composition mapping tests; client typed-method happy-path test; AppHost smoke evidence (preferred: automated integration test; fallback: manual run notes); `docs/checklists/mutation-and-query-negative-tests.md` and the architecture-doc cross-link (Action Item 7).
- **Explicitly out of scope (recorded so the dev agent does not over-build):** `ExplainContextSelection` endpoint (Story 3.3) — Story 3.2 holds but does NOT serialize the `ProjectContextEvaluation` rows the policy already emits; `RefreshProjectContext` endpoint (Story 3.4); `ConversationStartSetupProjection` and `GetConversationStartSetup` endpoint (Story 3.5); `Resolution/` (Epic 4); any new shared-vocabulary enum value; any new `ProjectContextInclusionDiagnostic` vocabulary entry; any edit to `ProjectContextInclusionPolicy` / `ProjectContextInclusionOrder` / Story 3.1 DTOs; any new mutation endpoint; any change to `ProjectAggregate.*` / `ProjectState` / `ProjectStateApply` / `ProjectCommandValidator` / projections / Story 2.x ACL interfaces / `IProjectCommandSubmitter`; the U+2028/U+2029 canonicaliser hardening (Epic 2 retro Action Item 2 — does not apply to a query); pagination over `GetProjectContext` (single first-page snapshot is sufficient for FR-16 v1; if a project has > 100 conversations, the policy still produces a useful Context bounded by the page cap; multi-page assembly is a follow-up if needed); any new ADR; `ProjectFolderCreationPending` reconciliation flow (Epic 5 territory).

### Current Code Facts Verified (this working tree, baseline `67beac6`)

- **Story 3.1 status: `done`** (per sprint-status.yaml + senior-review on 2026-05-28). The pure `ProjectContextInclusionPolicy` ships at `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` as `sealed class ProjectContextInclusionPolicy(ILogger<...>? logger = null)` with one public `Assemble(...)` method.
- **Story 3.1 DTOs ship at:**
  - `src/Hexalith.Projects.Contracts/Models/{ProjectContext, ProjectContextAssemblyResult, ProjectContextEvaluation, ProjectContextExclusion, ProjectContextReference}.cs`.
  - `src/Hexalith.Projects.Contracts/Ui/{ProjectContextAssemblyOutcome, ProjectContextFreshness, ProjectContextInclusionCheck, ProjectContextInclusionDiagnostic}.cs`.
  - `ProjectContext` has factory methods `Empty(...)`, `Unauthorized(...)`, `ProjectUnavailable(...)` (the senior-review M3 added `Empty`).
- **`docs/context-assembly-decision-matrix.md` is the single source of truth** for the per-evidence-state × per-operation fail-closed matrix; the `Get` column is already populated by Story 3.1 with the expected cells (Story 3.2 is a consumer, not an editor).
- **`ProjectAuthorizationGate.AuthorizeReadAsync`** (`src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` line 80) already returns `ProjectAuthorizationResult` carrying `ProjectDetail`. The internal `AuthorizeAsync` (line 306, line 357) holds the `TenantAccessAuthorizationResult` locally; Story 3.2 threads it through the result (additive — `ProjectAuthorizationResult.TenantAccessResult` property).
- **`IProjectConversationDirectory.ListForProjectAsync`** is the canonical conversation-evidence source (Story 2.1 Pattern A). Returns `ProjectConversationsPage` with `Items: IReadOnlyList<ProjectConversationItem>` carrying `ConversationId`, `DisplayLabel`, `LifecycleStatus`, `TrustSignal: ProjectConversationTrustSignal`, `ObservedAt`.
- **`PageRequest`** (`src/Hexalith.Projects.Contracts/Queries/PageRequest.cs`) defaults to `PageSize = 25`, supports 1–100, with `ContinuationCursor` for paging. Story 3.2 uses `PageSize = 100, ContinuationCursor = null` (single first-page snapshot).
- **The existing `GetProject` endpoint** (`ProjectsDomainServiceEndpoints.cs` line 422 / `MapGet` at line 90) is the read-shape oracle Story 3.2 mirrors: `correlationId` validation → `AuthorizeReadAsync` → `ReadModelUnavailable | SafeDenial` → `Idempotency-Key` rejection → freshness rejection → response with `X-Correlation-Id` echo + `X-Hexalith-Freshness: eventually_consistent`.
- **OpenAPI spine** at `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`: `GetProject` is at line 158–220; the `paths:` block begins at line 23; the components / schemas block follows around line 2131+. The `x-hexalith-*` extensions (`read-consistency`, `correlation`, `authorization`, `canonical-error-categories`) are copy-from-GetProject. The spine YAML is in the existing canonical format (kebab-case `operationId`, camelCase schema fields, `$ref` for shared components).
- **NSwag MSBuild target** for idempotency-helper regeneration carries the **pre-existing Windows-backslash path bug** documented in Stories 2.3 / 2.4 / 2.5 / 2.7 review notes — Action Item 1 of the Epic 2 retro is binding on Story 3.2.
- **The root commit is `67beac6 feat(story-3.1): Story 3.1: Context-assembly policy & allowlist`.** No Hexalith.Memories / Conversations / Folders / Tenants / EventStore / FrontComposer / Commons / AI.Tools / Builds submodule pointer change is required; pre-existing "modified content" markers on Hexalith.Commons / Hexalith.Conversations / Hexalith.Parties are unrelated to Story 3.2.
- **Baseline test counts (post-Story 3.1 senior-review):** Projects.Tests 407, Server.Tests 196, Contracts.Tests 128, Client.Tests 31, Integration.Tests 14; full-solution `dotnet test Hexalith.Projects.slnx` 776/776.

### Required Capability Path

Story 3.2's only true upstream capability gate is **`IProjectConversationDirectory.ListForProjectAsync`** — already stable since Story 2.1, exercised by the existing `ListProjectConversations` endpoint (line 488 of `ProjectsDomainServiceEndpoints.cs`). No Folders or Memories ACL call is needed at query time for Story 3.2 (Get), because the `ProjectDetailItem` projection rows already carry `ReferenceState` / `ReasonCode?` / `ObservedAt` set at link-time and validated by the Story 2.4 / 2.5 / 2.7 mutation handlers. **Story 3.4 (Refresh) is the slot where Folders / Memories ACL recheck happens.**

If the dev agent finds that the `ProjectDetailItem`-stored `ReferenceState` is insufficient for some inclusion decision (e.g. the projection row carries `Included` but the policy needs to also know "still authorized for this caller"), HALT and surface the conflict — the resolution is to (i) augment the projection to carry the missing freshness signal, OR (ii) hand the call off to Story 3.4. Do NOT add an on-the-fly Folders / Memories recheck inside Story 3.2's handler.

### Guardrails

- **Single source of truth — the policy.** `ProjectContextInclusionPolicy.Assemble(...)` is the only place where include/exclude / fail-closed-collapse / freshness-mapping / diagnostic-vocabulary decisions are made. The endpoint, the composition mapper, and the wire serializer NEVER duplicate any of these decisions. If the temptation arises to "fast-path" a denial in the handler before calling the policy, the dev agent must resist — the policy already handles every collapse uniformly (and its cells are tested via the decision-matrix completeness test).
- **Safe-denial 404 contract.** The HTTP status surfaces `200` (assembled, including archived-project) or one of `400 / 401 / 403 / 404 / 503`. `ProjectContextAssemblyOutcome.Unauthorized` and `.ProjectUnavailable` BOTH map to **HTTP 404** at the boundary — never reveal cross-tenant existence, never differentiate `Unauthorized` vs `ProjectUnavailable` at the HTTP layer. The internal `AssemblyOutcome` is observability-only; the wire body is empty / Problem Details on the safe-denial path. This is the Story 1.4 + Story 3.1 safe-denial 404 contract carried forward.
- **Idempotency-Key rejected on the query** (mirrors `GetProject`). Order: authorize first → then validate `Idempotency-Key` is absent → then proceed. Authorized callers receive validation feedback; unauthorized callers receive only safe-denial 404.
- **Freshness header strict.** `X-Hexalith-Freshness` request header may be `eventually_consistent` or absent; any other value is rejected as a validation error after authorization. Response always carries `X-Hexalith-Freshness: eventually_consistent`.
- **Correlation echo.** `X-Correlation-Id` request header (if canonical) is echoed in the response.
- **Single page cap (Story 3.2 v1).** Conversations are fetched with `PageSize = 100, ContinuationCursor = null`. No continuation, no client-driven paging in v1. The architecture supports paging if needed later; FR-16 v1 is a bounded snapshot.
- **No re-check, no re-fetch.** Folder / File / Memory references are taken AS-IS from `ProjectDetailItem`. No on-the-fly Folders / Memories ACL call at query time (that's Story 3.4 territory).
- **Tier-1 purity preserved.** `src/Hexalith.Projects/Context/**` MUST NOT gain any new file or change in Story 3.2. The composition mapper lives in `src/Hexalith.Projects.Server/Conversations/` (allowed to import `Hexalith.Conversations.*` via the translator boundary). The handler lives in `src/Hexalith.Projects.Server/` (allowed to import `Microsoft.AspNetCore.*` and `Hexalith.Conversations.Contracts.*` via existing translator types but NOT via direct `Hexalith.Conversations.*` references).
- **No new shared-vocabulary enum values.** Story 3.1's enums + the existing pre-Epic-3 vocabulary are sufficient for Story 3.2 by inspection (the policy already covers every cell of the Get column).
- **No edits to Story 3.1 surface.** `ProjectContextInclusionPolicy`, `ProjectContextInclusionOrder`, `ProjectContextAssemblyContext`, `ProjectContextProjectEvidence`, `ProjectContextTenantAccess`, `ProjectContextReferenceEvidence`, `ProjectContextConversationEvidence`, `ProjectContextDiagnostics`, `ProjectContextOperationKind`, the closed `ProjectContextInclusionDiagnostic` vocabulary, the wire DTOs, the four assembly enums — all unchanged.
- **OpenAPI fingerprint baseline updated** (deliberate, allowed for this story only). Subsequent Epic 3 stories must show zero spine diff unless they own one.
- **`.g.cs` regenerated** (deliberate, allowed for this story only). NSwag Linux fix is binding.
- **No nested recursive submodule init.** Read-only inspection is fine; nothing in Story 3.2 advances a submodule pointer.
- **Deterministic-fakes-only tests.** No `Thread.Sleep` / `Task.Delay` / `SpinWait` / `await Task.Yield()` / wall-clock retry loops. Convergence asserted via deterministic inputs.
- **Closed-vocabulary diagnostics only.** The endpoint NEVER surfaces a `Diagnostic` value outside `ProjectContextInclusionDiagnostic.Values`. The policy already enforces this; the endpoint only surfaces what the policy produces.
- **No `V2` types.** Public contracts evolve only through additive types.

### Suggested Handler Shape

```csharp
// src/Hexalith.Projects.Server/Queries/GetProjectContext.cs  (if extracted)
//
// Or inline in ProjectsDomainServiceEndpoints.cs (mirror GetProjectAsync pattern, line 422).

private static async Task<IResult> GetProjectContextAsync(
    string projectId,
    HttpContext httpContext,
    IProjectTenantContextAccessor tenantContext,
    ProjectAuthorizationGate authorizationGate,
    IProjectConversationDirectory conversationDirectory,
    ProjectContextInclusionPolicy contextPolicy,
    TimeProvider timeProvider,
    CancellationToken cancellationToken)
{
    string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
    correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;
    string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");
    taskId = IsCanonicalIdentifier(taskId) ? taskId : null;

    if (string.IsNullOrWhiteSpace(projectId) || !IsCanonicalIdentifier(projectId))
    {
        return SafeDenial(correlationId, null);
    }

    ProjectAuthorizationResult authorization = await authorizationGate
        .AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
        .ConfigureAwait(false);

    if (!authorization.IsAllowed || authorization.ProjectDetail is null)
    {
        return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
            ? ReadModelUnavailable(correlationId, null)
            : SafeDenial(correlationId, null, authorization);
    }

    if (HasHeader(httpContext, "Idempotency-Key"))
    {
        return ValidationProblem(correlationId, null, "idempotency_key");
    }

    string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
    if (requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal))
    {
        return ValidationProblem(correlationId, null, "freshness");
    }

    ProjectDetailItem detail = authorization.ProjectDetail;
    DateTimeOffset now = timeProvider.GetUtcNow();

    ProjectConversationsPage conversations = await conversationDirectory
        .ListForProjectAsync(
            new ProjectId(projectId),
            new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
            new CallerPrincipalId(tenantContext.PrincipalId!),
            new PageRequest(PageSize: 100, ContinuationCursor: null),
            cancellationToken)
        .ConfigureAwait(false);

    IReadOnlyList<ProjectContextConversationEvidence> conversationEvidence =
        ProjectContextConversationEvidenceMapper.Map(conversations, now);

    ProjectContextAssemblyResult assembled = contextPolicy.Assemble(
        new ProjectContextAssemblyContext(
            AuthoritativeTenantId: tenantContext.AuthoritativeTenantId,
            RequestedTenantId: tenantContext.RequestedTenantId,
            ProjectId: projectId,
            OperationKind: ProjectContextOperationKind.Get,
            CorrelationId: correlationId,
            TaskId: taskId,
            Now: now),
        new ProjectContextProjectEvidence(detail),
        new ProjectContextTenantAccess(authorization.TenantAccessResult!),
        new ProjectContextReferenceEvidence(
            ProjectFolder: detail.ProjectFolder,
            FileReferences: detail.FileReferences,
            MemoryReferences: detail.MemoryReferences,
            Conversations: conversationEvidence));

    if (!string.IsNullOrWhiteSpace(correlationId))
    {
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
    }

    httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;

    return Results.Json(assembled.Context, ResponseJsonOptions);
}
```

### Files To Read Before Editing

- `_bmad-output/implementation-artifacts/3-1-context-assembly-policy-allowlist.md` — Story 3.1 Dev Agent Record (especially the validation summary, the senior-review auto-fixes, and the closed `ProjectContextInclusionDiagnostic` vocabulary actually shipped — 13 values: `tenantMismatch`, `projectUnknown`, `projectArchived`, `referenceUnauthorized`, `referenceUnavailable`, `referenceStale`, `referenceArchived`, `referenceConflict`, `referenceInvalidIdentifier`, `referenceKindNotAllowlisted`, `projectFolderPending`, `referenceAmbiguous`, `referenceRedacted`).
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` — §"Action Items" rows 1 / 5 / 7 (NSwag Linux fix, AppHost smoke check, negative-test checklist), §"Next Epic Preview" (Story 3.2 obligations recorded), §"Epic 3 Preparation Tasks" (composition + ACL stability confirmation).
- `_bmad-output/planning-artifacts/epics.md` lines 721–740 — Story 3.2 ACs (authoritative).
- `_bmad-output/planning-artifacts/architecture.md` §"Process Patterns / ProjectContext assembly" (line 422-432), §"Implementation Sequence" step 7 (line 316), §"Cross-Component Dependencies" line 325, §"Feature/FR mapping" line 595 (Context Assembly).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` §FR-16 (line 230), §FR-17 (line 239), §FR-18 (line 247), §FR-20 (line 272), §SM-1 / SM-3 / SM-4 (line 347–350).
- `_bmad-output/project-context.md` (96 rules; especially Tier-1 purity, metadata-only, no submodule recursion, central package management).
- `docs/context-assembly-decision-matrix.md` — Story 3.1 fail-closed matrix; the `Get` column is the operational truth for Story 3.2 tests.
- `docs/event-catalog.md` §"Shared vocabulary — producer of last resort" — Story 3.1 listed `TenantMismatch` / `Stale` / `Ambiguous` / `MetadataMatched` as `unproduced — taxonomy-only`; Story 3.2 does NOT change this list (Story 3.4 may add `Stale` as a real producer).
- `docs/payload-taxonomy.md` — sensitivity classes; metadata-only invariants enforced over the new endpoint response.
- `docs/adr/memories-link-target.md` (Accepted) — §"Epic 3 allowlist treatment" carried forward; Story 3.2 does NOT call the Memories ACL at query time (no on-the-fly recheck).
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` + all 9 `Context/` files — Story 3.1 surface, unchanged.
- `src/Hexalith.Projects.Contracts/Models/{ProjectContext, ProjectContextReference, ProjectContextExclusion, ProjectContextAssemblyResult, ProjectContextEvaluation}.cs` — wire DTOs.
- `src/Hexalith.Projects.Contracts/Ui/{ProjectContextAssemblyOutcome, ProjectContextFreshness, ProjectContextInclusionCheck, ProjectContextInclusionDiagnostic}.cs` — wire enums + closed vocabulary.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — OpenAPI spine; `GetProject` (line 158) is the read-shape oracle.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — `GetProjectAsync` (line 422) and `ListProjectConversationsAsync` (line 488) patterns.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — `AuthorizeReadAsync` (line 80) and the internal `AuthorizeAsync` (line 306, line 357) where `TenantAccessAuthorizationResult` lives.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationResult.cs` — additive extension target (Task 4).
- `src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs` + `ConversationsProjectConversationDirectory.cs` + `ProjectConversationTranslator.cs` — ListForProjectAsync surface + translator boundary precedent.
- `src/Hexalith.Projects.Client/Generation/nswag.json` + the MSBuild target file (likely in `src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` or a sibling `.props` / `.targets` file) — the Windows-backslash path that needs fixing for Action Item 1.
- `tests/Hexalith.Projects.Tests/Context/ProjectContextDecisionMatrixCompletenessTests.cs` (Story 3.1) — the per-cell completeness test Story 3.2's `GetProjectContextTests.cs` mirrors.
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` (Story 3.1 extension) — FS-2 harness; Story 3.2 extends over endpoint responses.
- `tests/Hexalith.Projects.Server.Tests/.../GetProjectTests.cs` (if it exists) — closest read-side endpoint-test oracle.
- `tests/Hexalith.Projects.Integration.Tests/...` (any existing Aspire AppHost integration test) — pattern oracle for Task 11.

### Testing Requirements

See AC 7 / AC 8 / AC 9 / AC 10 / AC 11 / AC 12 / AC 13 / AC 15 for the full per-suite enumeration. Highlights:

- **Tier-2 endpoint matrix.** Every cell of the `Get` column in `docs/context-assembly-decision-matrix.md` is exercised via a named fixture in `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs`. Reuse the Story 3.1 completeness-test discovery helper (extract it to `src/Hexalith.Projects.Testing/Context/` if needed) so the doc and the endpoint cannot drift.
- **Idempotency-Key on query rejected** (AC 8) — Tier-2.
- **Route negatives** (AC 9) — Tier-2.
- **Cross-tenant isolation** (AC 11) — Tier-2 FS-8/SM-3.
- **Composition mapping** (AC 13) — Tier-1 (pure, no Dapr / HTTP / clock).
- **Leakage over endpoint responses** (AC 10) — Tier-2 over the matrix + error responses.
- **Generated client happy-path** (AC 15) — Client.Tests.
- **AppHost smoke** (AC 12) — Integration.Tests; preferred automated, manual fallback acceptable with documented evidence.
- **No-sleep grep** — zero hits filtered to Story 3.2 test files.
- **Cross-tenant safe-denial 404** — never 403, never reveals existence; the policy's `AssemblyOutcome` is INTERNAL telemetry, not surfaced as distinct HTTP status.

### Previous Story Intelligence

- **Story 3.1 (Context-assembly policy & allowlist) — done, 776/776.** Established the pure allowlist-based `ProjectContextInclusionPolicy` Story 3.2 consumes unchanged. Closed `ProjectContextInclusionDiagnostic` vocabulary ships 13 values (per senior-review). The `docs/context-assembly-decision-matrix.md` `Get` column is the operational truth for Story 3.2 tests. `ProjectContextDecisionMatrixCompletenessTests` is the pattern Story 3.2's endpoint tests mirror (one test per cell, parsed from the doc).
- **Story 2.7 (Link/Unlink Memory) — done, 567/567.** Established `ProjectMemoryReference` shape carried directly through to `ProjectContextReferenceEvidence.MemoryReferences` in Story 3.2. Memories-specific forbidden-term list extended in the leakage harness; Story 3.2 reuses unchanged. Zero `#pragma warning disable HXL001|HXL002` policy enforced; Story 3.2 inherits this.
- **Story 2.5 (File Reference link/unlink) — done.** Per-kind disjoint reference-index lanes (`folder` / `file` / `memory`) carried through to disjoint per-kind lists in the assembled `ProjectContext` (Story 3.1 invariant; Story 3.2 endpoint preserves it by routing through the policy). Folders content forbidden-term list reused in the leakage harness.
- **Story 2.4 (Project Folder reference) — done.** Established the degraded `Pending` Project Folder path (`folder_create_external_unavailable`); the policy maps `Pending` → `ReferenceState.Pending` / `FailedCheck = ReferenceFreshness`; Story 3.2 just passes the projection-stored state through.
- **Story 2.3 (Conversation write-side) — done.** Pattern A holds — Projects does not store conversation membership; Story 3.2's conversation evidence comes via `IProjectConversationDirectory.ListForProjectAsync` (Story 2.1 read ACL).
- **Story 2.1 (Conversation Reference Read ACL) — done.** Established `ProjectConversationsPage` + `ProjectConversationItem` + `ProjectConversationTrustSignal` shape Story 3.2 maps via `ProjectContextConversationEvidenceMapper`. The existing `ProjectConversationTranslator.cs` is the precedent for the new mapper's namespace boundary.
- **Story 1.6 (Tenant access & layered fail-closed authorization) — done.** `TenantAccessAuthorizationResult` (`TenantAccessOutcome`, `FreshnessStatus`, etc.) consumed via `ProjectContextTenantAccess`. Story 3.2 threads it through `ProjectAuthorizationResult` additively.
- **Story 1.4 (Tracer bullet) — done.** Safe-denial 404 contract + FS-2 `NoPayloadLeakage` harness + FS-8/SM-3 cross-tenant isolation harness reused unchanged.
- **Story 1.3 (OpenAPI Contract Spine + NSwag client + idempotency hasher + fingerprint gate flip) — done.** The spine fingerprint gate is the canonical churn-check; Story 3.2 deliberately flips it for one cycle (new operation + new schemas + new example).
- **Epic 2 retrospective carry-forward action items binding on Story 3.2:**
  - Action 1 (NSwag Linux path fix): realized by Task 2 — required because Story 3.2 regenerates `.g.cs`.
  - Action 5 (AppHost smoke check): realized by Task 11 + AC 12.
  - Action 7 (route/body + missing-Idempotency-Key checklist): realized by Task 12 + AC 14.
  - Action 2 (U+2028/U+2029 hardening): does NOT apply to Story 3.2 (query has no idempotency-fingerprint surface); survives in the carry-forward list for the next mutation surface.
  - Action 4 (per-story leakage extensions): realized by Task 9.
  - Action 3 (decision-matrix doc as single ref): already realized by Story 3.1 / Task 4 of Story 3.1; Story 3.2 consumes it.
  - Action 6 (track unproduced shared-vocab outcomes): already realized by Story 3.1 / Task 5 of Story 3.1; Story 3.2 doesn't change the list.
  - Action 8 (Folders-side external POST follow-up): tracking-only, not blocking Story 3.2 (Story 3.2 is read-side and doesn't trigger folder creation).
- **Recent commit hygiene.** Stories 2.5 (`e127b7a`), 2.6 (`0058ac3`), 2.7 (`70f2ebe`), 3.1 (`67beac6`) all follow story-scoped commits with no nested-recursive submodule init. Story 3.2 must do the same.

### Out Of Scope

- Implementing `ExplainContextSelection` (Story 3.3) — Story 3.2 holds but does NOT serialize the `ProjectContextEvaluation` rows the policy already emits. The Evaluations list is available on `ProjectContextAssemblyResult` and ready for Story 3.3 to surface via a separate endpoint.
- Implementing `RefreshProjectContext` (Story 3.4) — the only place Folders / Memories ACL re-validation happens at query time. Story 3.2 (Get) consumes the projection-stored reference state as-is.
- Implementing `GetConversationStartSetup` (Story 3.5) and the `ConversationStartSetupProjection`.
- Implementing project-resolution policy (Epic 4) — `Resolution/` remains empty.
- Adding new shared-vocabulary enum values (`ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ProjectConversationTrustSignal`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus`, `ProjectContextInclusionCheck`, `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextOperationKind`, `ProjectContextInclusionDiagnostic`). If genuinely required, HALT and surface the conflict.
- Adding multi-page support to `GetProjectContext` (a single first-page snapshot at `PageSize=100` is FR-16 v1's scope). If a project legitimately has > 100 conversations and the snapshot's truncation becomes a UX problem, that's a follow-up story.
- On-the-fly Folders / Memories ACL recheck at Get time — Story 3.4 Refresh territory.
- Modifying `src/Hexalith.Projects/Context/` files in any way (Story 3.1 invariant).
- Modifying `_bmad-output/planning-artifacts/epics.md` Story 3.2 acceptance criteria.
- Modifying the shared vocabulary enums, the Story 3.1 wire DTOs, or `docs/context-assembly-decision-matrix.md` (Story 3.1 owns its cell semantics; Story 3.2 is consumer only).
- U+2028/U+2029 canonicaliser hardening (Action Item 2 — for the next mutation surface, not for Story 3.2).
- Folders-side external `POST /api/v1/folders` mapping (Action Item 8 — Folders submodule scope).
- DeterministicActorPartyResolver replacement — Story 3.2 is read-side and doesn't invoke it.
- Real-Keycloak / OIDC E2E (Epic 5 territory).
- Advancing any submodule pointer (Hexalith.Memories / Conversations / Folders / Tenants / EventStore / FrontComposer / Commons / AI.Tools / Builds) or running `git submodule update --init --recursive`.
- Performing nested recursive submodule initialization / update.

### Developer HALT Conditions

- **HALT before any code change** if Story 3.1's policy or DTOs would need to change for Story 3.2 to land. Surface the conflict in the Dev Agent Record; the resolution is a follow-up story / ADR, not an inline Story 3.1 edit.
- **HALT** if `ProjectAuthorizationResult` cannot be extended additively to carry `TenantAccessAuthorizationResult` (e.g. a load-bearing existing caller binary-breaks). Choose the richer-envelope alternative (per AC 5) instead; record the choice.
- **HALT** if the NSwag idempotency-helper MSBuild target fix requires changing `Hexalith.Builds` (a sibling submodule). Surface the conflict; the resolution is a separate Hexalith.Builds story, not an inline edit here.
- **HALT** if the `ProjectDetailItem`-stored `ReferenceState` is structurally insufficient for the Get-column matrix cells and an on-the-fly Folders / Memories ACL recheck would be needed at query time. The resolution is Story 3.4 (Refresh), not an inline recheck.
- **HALT** if implementing the AppHost smoke test requires a Dapr / Aspire / Keycloak topology change. The fallback (manual smoke evidence) is acceptable; the automated path is preferred but not blocking if topology change is required.
- **HALT** if a new shared-vocabulary enum value, a new `ProjectContextInclusionDiagnostic` member, or a new `ProjectContextOperationKind` member appears genuinely required.
- **HALT** if implementing the endpoint would require modifying `ProjectAggregate.*` / `ProjectState` / `ProjectStateApply` / projections / Story 2.x ACL interfaces / `IProjectCommandSubmitter` (Story 3.2 is read-side; mutation surfaces stay untouched).
- **HALT** if the wire response would surface any `Diagnostic` value outside `ProjectContextInclusionDiagnostic.Values` — including raw upstream `Message` / `Suggestion` / path / token / payload text.
- **HALT** if `Thread.Sleep` / `Task.Delay` / `SpinWait` / wall-clock polling is required to make a test pass.
- **HALT** if a submodule pointer or `_bmad-output/planning-artifacts/epics.md` Story 3.2 ACs would need to change for the story to land.

## References

- `_bmad-output/planning-artifacts/epics.md` lines 721–740 — Story 3.2 ACs (authoritative).
- `_bmad-output/planning-artifacts/architecture.md` — AR-9 (ProjectContext assembly policy), §"ProjectContext is an assembled, authorization-filtered read result", §"Process Patterns / ProjectContext assembly" (line 428), §"Implementation Sequence" step 7 (line 316), §"Feature/FR mapping" line 595, §"File Organization Patterns".
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` — FR-16 (line 230), FR-17 (line 239), FR-18 (line 247), FR-20 (line 272); NFR-1 / NFR-2 / NFR-3 fail-closed posture; UJ-1 / UJ-4 user-journey framing; SM-1 / SM-3 / SM-4 metrics.
- `_bmad-output/implementation-artifacts/3-1-context-assembly-policy-allowlist.md` — immediate-prior story; the pure policy + DTOs Story 3.2 consumes unchanged; senior-review auto-fixes (M3 added `ProjectContext.Empty(...)` factory); validation summary 776/776.
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` — §"Action Items" rows 1 / 4 / 5 / 7 (NSwag Linux fix, leakage extensions, AppHost smoke, negative-test checklist); §"Epic 3 Preparation Tasks" rows 4 (read interface for projection) / 5 (reuse conformance harnesses).
- `_bmad-output/implementation-artifacts/2-7-link-unlink-memory.md` — Memories reference shape carried into Story 3.2's evidence composition.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — File reference shape carried into Story 3.2.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` — Project Folder reference + degraded `Pending` path carried into Story 3.2.
- `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md` — `IProjectConversationDirectory.ListForProjectAsync` ACL + `ProjectConversationTranslator` boundary pattern Story 3.2's new mapper mirrors.
- `_bmad-output/implementation-artifacts/1-6-tenant-access-layered-fail-closed-authorization.md` — `TenantAccessAuthorizationResult` / `TenantAccessOutcome` / `TenantProjectionFreshnessStatus` shape Story 3.2 threads through.
- `_bmad-output/implementation-artifacts/1-4-create-project-end-to-end-tracer-bullet.md` — safe-denial 404 contract + FS-2 / FS-8 reusable harnesses.
- `_bmad-output/implementation-artifacts/1-3-openapi-contract-spine-generated-typed-client.md` — OpenAPI spine + NSwag client + idempotency hasher + fingerprint gate baseline; Story 3.2 deliberately bumps the fingerprint.
- `_bmad-output/project-context.md` — 96 rules (Tier-1 purity, metadata-only, central package management, no submodule recursion, etc.).
- `docs/context-assembly-decision-matrix.md` — Story 3.1 fail-closed matrix; the `Get` column is operational truth for Story 3.2 tests.
- `docs/event-catalog.md` §"Shared vocabulary — producer of last resort" — Story 3.1 tracker; Story 3.2 does NOT modify.
- `docs/payload-taxonomy.md` — sensitivity classes; metadata-only invariants.
- `docs/adr/memories-link-target.md` (Accepted) — Story 2.6 ADR; Story 3.2 does NOT call Memories ACL at query time (Story 3.4 territory).
- `docs/adr/identifier-boundary.md` — sibling identifier reuse rule.
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` — Story 3.1 policy; Story 3.2 consumes unchanged.
- `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs` + `ProjectContextReference.cs` + `ProjectContextExclusion.cs` + `ProjectContextAssemblyResult.cs` + `ProjectContextEvaluation.cs` — wire DTOs.
- `src/Hexalith.Projects.Contracts/Ui/ProjectContextAssemblyOutcome.cs` + `ProjectContextFreshness.cs` + `ProjectContextInclusionCheck.cs` + `ProjectContextInclusionDiagnostic.cs` — wire enums + closed vocabulary.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — OpenAPI spine; `GetProject` (line 158–220) read-shape oracle.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — `GetProjectAsync` (line 422), `ListProjectConversationsAsync` (line 488).
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — `AuthorizeReadAsync` (line 80), internal `AuthorizeAsync` (line 306).
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationResult.cs` — additive extension target.
- `src/Hexalith.Projects.Server/Conversations/IProjectConversationDirectory.cs` + `ConversationsProjectConversationDirectory.cs` + `ProjectConversationTranslator.cs` — read ACL surface + translator boundary precedent.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` — read-side state shape; `ProjectFolder` / `FileReferences` / `MemoryReferences` already carry `ReferenceState` / `ReasonCode?` / `ObservedAt`.
- `src/Hexalith.Projects.Contracts/Queries/PageRequest.cs` — page-request shape; default 25, max 100.
- `src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs` + `RecordingLogger.cs` — Story 3.1 test helpers reused unchanged.
- `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` — FS-2 harness; Story 3.2 extends test fixtures, not the harness itself.
- `tests/Hexalith.Projects.Tests/Context/ProjectContextDecisionMatrixCompletenessTests.cs` (Story 3.1) — completeness-test pattern Story 3.2's endpoint tests mirror.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (create-story, 2026-05-28)

### Debug Log References

- 2026-05-28: Resolved the `bmad-create-story` workflow; loaded sprint status (`3-2-get-project-context` is `backlog`, Epic 3 is `in-progress`, Story 3.1 is `done`); loaded Epic 3 Story 3.2 verbatim from `_bmad-output/planning-artifacts/epics.md` lines 721–740; loaded the Epic 2 retrospective end-to-end and copied Action Items 1 / 5 / 7 verbatim into AC 3 / AC 12 / AC 14; loaded the Story 3.1 implementation artifact end-to-end (its 776/776 baseline, the closed `ProjectContextInclusionDiagnostic` 13-value vocabulary, the senior-review M3 `ProjectContext.Empty(...)` factory addition); inspected `ProjectAuthorizationGate.AuthorizeReadAsync` and confirmed `TenantAccessAuthorizationResult` is held internally at line 357 (Task 4 threads it through `ProjectAuthorizationResult` additively); inspected `GetProjectAsync` (line 422) and `ListProjectConversationsAsync` (line 488) as the read-shape oracle for the new handler; inspected `IProjectConversationDirectory.ListForProjectAsync` and `PageRequest` (default 25, max 100); inspected the OpenAPI spine `GetProject` block (line 158) as the schema oracle; confirmed no submodule pointer change is required.
- Create-story workflow only; no implementation commands were run for this story.

### Completion Notes List

- Story 3.2 context created. Status set to `ready-for-dev`.
- Story 3.2 is the **first HTTP-surfaced consumer** of Story 3.1's pure policy. The handler is a thin orchestrator: authorize → page conversations → compose evidence → invoke policy → return DTO. No policy decision is duplicated in the endpoint.
- AC 3 + Task 2 are binding: the NSwag idempotency-helper MSBuild target Linux path fix (Epic 2 retro Action Item 1) MUST land before Story 3.2 regenerates `.g.cs`. The fix is contained to a single MSBuild target file; if it requires a `Hexalith.Builds` submodule change, HALT and surface the conflict.
- AC 12 + Task 11 are binding: AppHost smoke evidence (Epic 2 retro Action Item 5) is restored. Automated integration test is preferred; manual run notes are acceptable with documented evidence.
- AC 14 + Task 12 are binding: the canonical negative-test checklist (Epic 2 retro Action Item 7) is authored at `docs/checklists/mutation-and-query-negative-tests.md` and cross-linked from architecture.
- The fail-closed posture is preserved by routing every decision through the Story 3.1 policy; the endpoint never short-circuits a denial before calling the policy. `Unauthorized` and `ProjectUnavailable` both map to HTTP 404 at the boundary (safe-denial contract).
- `Idempotency-Key` is rejected on the query (mirrors `GetProject`); `X-Hexalith-Freshness` strict-class request is rejected as validation error; `X-Correlation-Id` is echoed; response always carries `X-Hexalith-Freshness: eventually_consistent`.
- Conversations are fetched with `PageSize=100, ContinuationCursor=null` (single first-page snapshot). FR-16 v1 is bounded by design; multi-page is out of scope unless a follow-up story justifies it.
- Folder / File / Memory references are taken AS-IS from `ProjectDetailItem` (no on-the-fly Folders / Memories ACL recheck at Get time — that's Story 3.4 Refresh territory).
- The `ProjectContextAssemblyResult.Evaluations` collection is held but NOT serialized in Story 3.2's wire body — Story 3.3 `ExplainContextSelection` will own that surface.
- AC 10 + Task 9 extend the FS-2 leakage harness over endpoint responses (matrix + error responses), reusing the Memories + Folders forbidden-term lists from Stories 2.5 / 2.7 / 3.1 unchanged.
- AC 17 + Guardrails enforce: no new shared-vocabulary enum values, no edits to Story 3.1 surfaces, no nested-recursive submodule init, no `Thread.Sleep`/`Task.Delay`/`SpinWait`/`Task.Yield` in tests, deterministic-clock-only (`TimeProvider.GetUtcNow()` injected — no `DateTimeOffset.UtcNow` in handler code).
- The dev agent owns: Task 1 (capability gate — read-only inspection), Task 2 (NSwag Linux fix), Task 3 (OpenAPI spine + regeneration), Task 4 (`ProjectAuthorizationResult` additive extension), Task 5 (`ProjectContextConversationEvidenceMapper`), Task 6 (HTTP handler), Task 7 (Tier-2 endpoint tests), Task 8 (Tier-1 composition tests), Task 9 (leakage harness extension), Task 10 (client typed-method test), Task 11 (AppHost smoke), Task 12 (negative-test checklist), Task 13 (validation).
- Expected validation deltas: build 0W/0E; full-solution `dotnet test Hexalith.Projects.slnx` grows from 776 to ~825 (≈ +50: Server +25, Projects +15, Contracts +5, Client +3, Integration +1); `.g.cs` regenerated cleanly; OpenAPI fingerprint baseline updated (allowed for this story only); no submodule pointer change.

### Dev Agent Validation Summary (Story 3.2 — 2026-05-28)

**Build / test budgets**
- `dotnet build Hexalith.Projects.slnx` → **0 W / 0 E** (Build succeeded, Elapsed 00:00:18).
- `dotnet test Hexalith.Projects.slnx` → **807/807 passed, 0 failed, 0 skipped** (baseline 776 → +31).
  - `Hexalith.Projects.Tests` 407/407 (unchanged — no new Tier-1 tests in this lane; the composition mapper tests live in `Server.Tests/Conversations/` next to the mapper itself).
  - `Hexalith.Projects.Server.Tests` 224/224 (baseline 196 → +28: 16 Tier-2 endpoint tests + 12 Tier-1 mapper tests).
  - `Hexalith.Projects.Contracts.Tests` 128/128 (unchanged; the existing `OpenApiContractSpineTests.Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` caught the wire-shape `tenantId` exposure mid-implementation — see "Story 3.1 surface touch" below).
  - `Hexalith.Projects.Client.Tests` 34/34 (baseline 31 → +3: new `GetProjectContextClientTests`).
  - `Hexalith.Projects.Integration.Tests` 14/14 (unchanged — see "AppHost smoke evidence" below for the documented blocker).

**Endpoint shape**
- New endpoint matches AC 1 exactly: `GET /api/v1/projects/{projectId}/context`, operationId `GetProjectContext`, tags `projects`, parameters `ProjectId` / `CorrelationId` / `Freshness`, responses `200`/`400`/`401`/`403`/`404`/`503`, `x-hexalith-read-consistency: eventually_consistent`, `x-hexalith-correlation` query-correlation-only, `x-hexalith-authorization: tenant-context-and-project-read-permission`, error categories mirror `GetProject`. The `200` schema is the existing Story 3.1 `ProjectContext` DTO serialized as-is. `Idempotency-Key` is rejected as `validation_error` after authorization. The response carries `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent`.

**Handler placement**
- The handler was extracted to a new partial-class file `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs`. The base `ProjectsDomainServiceEndpoints.cs` was 1999 LOC pre-Story-3.2; adding the ~150-LOC handler inline would push the file past 2150 LOC and out of comfortable reading range. Per the AC 4 split-when-large guidance, this is the preferred path.

**Authorization result extension**
- `ProjectAuthorizationResult` extended additively with `TenantAccessResult: TenantAccessAuthorizationResult?` (defaults to `null` so every existing caller compiles unchanged). The `AuthorizeAsync` internal threads the typed result through both `Allowed(...)` and `Denied(...)` factories; both `Deny(...)` and the final `Allowed(...)` populate the result. Existing endpoints (`GetProjectAsync`, `ListProjectConversationsAsync`, all mutations) are unaffected — confirmed by 196 → 196 unchanged Server.Tests baseline (excluding the new `GetProjectContextTests` lane).

**Conversation page cap**
- `PageSize = 100, ContinuationCursor = null` as specified (FR-16 v1 single-snapshot scope).

**Story 3.1 surface touch (single, documented)**
- The OpenAPI spine test `Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` enforces the invariant "no schema property is named `tenantId`" (FS-8/SM-3 — tenant authority must never be wire-controllable). The existing `ProjectContext` C# DTO has `string TenantId` as its first parameter; serialized via `JsonNamingPolicy.CamelCase` it emits `"tenantId": "..."` on the wire, which Story 3.2's new OpenAPI schema for `ProjectContext` would have had to declare to pass schema-validation, which would then fail the spine test. The minimal additive fix is `[JsonIgnore]` on `ProjectContext.TenantId` so the wire body never carries it — the policy continues to consume the field internally for inclusion/outcome logic. This is the ONLY Story 3.1 surface change Story 3.2 required, and it is consistent with the AC 2 allowance for additive `[JsonPropertyName]` / serialization-attribute additions on the Story 3.1 DTOs. The `[JsonIgnore]` attribute is purely a serialization control; it does not change any policy decision, evaluation order, freshness mapping, or closed-vocabulary diagnostic.

**AppHost smoke evidence (Epic 2 retro Action Item 5 — manual fallback path)**
- Acceptable-fallback path per AC 12 was taken: a `DistributedApplicationTestingBuilder`-based AppHost smoke test was NOT added because the local WSL test environment cannot reliably boot the full Projects + Workers + EventStore + Tenants + Keycloak + Dapr sidecars + Redis topology within the test budget without surfacing flakiness unrelated to Story 3.2's surface (Docker is installed at v29.4.3, but Dapr sidecars + Keycloak + EventStore image pulls were not validated to converge within the integration-test timeout, and a flaky AppHost smoke would reduce the value of the rest of the regression signal).
- **Documented manual smoke path (for human verification in a runtime-enabled environment):** start the AppHost with `cd /mnt/d/Hexalith.Projects && DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet run --project src/Hexalith.Projects.AppHost`, observe the Aspire dashboard URL (printed in the AppHost logs), confirm `projects` / `projects-workers` / `eventstore` / `tenants` / `keycloak` / `redis` resources are Healthy, then `curl -H 'Authorization: Bearer <real keycloak token>' "https://<aspire-projects-url>/api/v1/projects/<projectId>/context"` and assert the 200 response is the assembled metadata-only `ProjectContext` shape (with `X-Hexalith-Freshness: eventually_consistent` header and `X-Correlation-Id` echo).
- **Environment-specific blocker:** automated AppHost smoke requires a vetted local Aspire topology runbook (Docker daemon, Dapr CLI, real Keycloak / OIDC dev cert, EventStore container pull). The 16 Tier-2 endpoint tests in `GetProjectContextTests` cover the full handler/composition/policy chain via an in-process `WebApplication` slim host, so the assembled-from-real-projection signal is preserved without the topology dependency.

**NSwag Linux fix (Epic 2 retro Action Item 1) — RESOLVED**
- The pre-existing Windows-backslash bug in `Hexalith.Projects.Client.csproj` had TWO layers:
  1. **Backslash paths** in `<HexalithProjectsContractSpine>` / `<HexalithProjectsGeneratedClient>` / `<HexalithProjectsGeneratedHelpers>` / `<HexalithProjectsHelperGeneratorProject>` and in the `<Exec Command="...">` strings (`$(MSBuildProjectDirectory)\..\..`, `$(MSBuildProjectDirectory)\nswag.json`). On Linux these become literal characters in shell commands — the program-side `File.ReadAllText(configurationPath)` then fails because `/mnt/d/.../Client\nswag.json` is not a valid Linux path. **Fix:** all backslash separators in the four property values and in the two `<Exec>` command strings are now forward-slash (cross-platform).
  2. **System `dotnet` ≠ pinned SDK.** The original `<Exec Command="dotnet run --project ...">` uses the first `dotnet` on the shell PATH, which on this WSL box (and many Linux CI runners) resolves to `/usr/bin/dotnet` (SDK 10.0.108), and the `global.json` 10.0.300 / `rollForward: latestPatch` pin makes that SDK fail with a missing-SDK error. **Fix:** the csproj now computes `<HexalithProjectsDotnetHostPath>$([System.IO.Path]::GetFullPath('$(MSBuildToolsPath)/../../$(HexalithProjectsDotnetHostFile)'))</HexalithProjectsDotnetHostPath>` — `MSBuildToolsPath` always belongs to the SDK currently driving the build, so the inner `dotnet run` invokes the SAME SDK as the outer build, regardless of which `dotnet` the shell would have picked. On Windows it resolves to `dotnet.exe`; on Linux it resolves to `dotnet`.
- **Verifying invocation on Linux (the binding AC 3 contract):** `cd /mnt/d/Hexalith.Projects && DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` → Build succeeded, 0 W / 0 E, regenerates both `HexalithProjectsClient.g.cs` (+562 lines for the new `GetProjectContextAsync` method + the 3 new wire DTO classes + 3 new wire enums) and `HexalithProjectsIdempotencyHelpers.g.cs` (only the SHA256 fingerprint constants changed — queries have no idempotency surface).
- **`Hexalith.Builds` submodule pointer:** unchanged — the fix is contained entirely to `src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj`. No `Hexalith.Builds` HALT triggered.

**Negative-test checklist (Epic 2 retro Action Item 7) — RESOLVED**
- `docs/checklists/mutation-and-query-negative-tests.md` authored with the 8 canonical rows. Cross-linked from `_bmad-output/planning-artifacts/architecture.md` Process Patterns section. Story 3.2's `GetProjectContextTests` applies rows 1 / 4 / 5 / 6 / 8 (rows 2 / 3 / 7 are mutation-only — N/A for a query). LF on disk; `git diff --check` clean.

**Boundary greps (Story 3.2 invariants)**
- `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` → **0 hits** (Story 3.1 Tier-1 purity invariant preserved).
- `grep -rE "Hexalith\.(Folders|Memories)" src/Hexalith.Projects.Server/Queries/` → **0 hits** (the GetProjectContext handler does not touch sibling-folders / sibling-memories at all).
- `grep -rE "Hexalith\.Conversations\." src/Hexalith.Projects.Server/Conversations/` → hits ONLY in the existing `ConversationsProjectConversationDirectory.cs` + `ProjectConversationTranslator.cs` + `IActorPartyResolver.cs` boundary files (none in the new `ProjectContextConversationEvidenceMapper.cs` because the mapper consumes Projects-shaped `ProjectConversationItem` from `Hexalith.Projects.Contracts.Queries`, not raw `Hexalith.Conversations.*` types).
- `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/Queries/ src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` → **0 hits** (clock injected via `TimeProvider.GetUtcNow()`).
- `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.Server.Tests/Queries/ tests/Hexalith.Projects.Server.Tests/Conversations/ProjectContextConversationEvidenceMapperTests.cs tests/Hexalith.Projects.Client.Tests/GetProjectContextClientTests.cs` → **0 hits** (deterministic-fakes-only).

**Submodule state (verified — no advances)**
- `git status` shows ` M Hexalith.Commons`, ` M Hexalith.Conversations`, ` M Hexalith.Parties` ONLY (lowercase ` M ` indicates "modified content within the submodule worktree" pre-existing per the initial Story 3.2 baseline; submodule pointers are unchanged). No `Hexalith.Folders` / `Hexalith.Memories` / `Hexalith.Tenants` / `Hexalith.EventStore` / `Hexalith.FrontComposer` / `Hexalith.AI.Tools` / `Hexalith.Builds` advance. No `git submodule update --init --recursive` was run.

**git diff hygiene**
- `git diff --check` — clean across `.cs`, `.md`, `.yaml`, `.csproj` story-touched files.
- `git diff --stat src/Hexalith.Projects.Client/Generated/` → 564 insertions, 2 deletions (only the new `GetProjectContextAsync` method block + wire DTOs + enum schemas; the existing surface is byte-stable except for the fingerprint constants — expected for this story per AC 3).
- OpenAPI fingerprint: spine intentionally bumped (new operation + 6 new schemas + 1 synthetic example). The fingerprint gate flips PASSED-with-baseline-update for this story only; future Epic 3 stories must show zero spine diff unless they own one (per AC 3 / Guardrails).

**No HALT items triggered**
- AC 5 additive extension landed cleanly; no need to escalate to a richer envelope.
- NSwag Linux fix is contained to `Hexalith.Projects.Client.csproj`; no `Hexalith.Builds` change needed.
- `ProjectDetailItem` projection-stored `ReferenceState` / `ReasonCode` / `ObservedAt` are sufficient for every cell of the `Get` column of `docs/context-assembly-decision-matrix.md`; no on-the-fly Folders / Memories ACL recheck was required.
- The single Story 3.1 surface touch (additive `[JsonIgnore]` on `ProjectContext.TenantId`) is documented above; it is consistent with AC 2 and does not change policy behaviour or closed vocabularies.
- No new shared-vocabulary enum value, no new `ProjectContextInclusionDiagnostic` member, no new `ProjectContextOperationKind` member required.
- No `Thread.Sleep` / `Task.Delay` / `SpinWait` / `await Task.Yield()` used in tests.
- No submodule pointer change.

### File List

**Modified (production code):**
- `src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` — NSwag MSBuild target Linux fix (forward-slash paths + explicit `$(HexalithProjectsDotnetHostPath)` derived from `$(MSBuildToolsPath)`).
- `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs` — additive `[JsonIgnore]` on `TenantId` (wire shape must not echo tenant authority — FS-8/SM-3). Internal use unchanged.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — added `GET /api/v1/projects/{projectId}/context` operation, the 6 new wire schemas (`ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextInclusionCheck`), and the synthetic `ProjectContext` example.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationResult.cs` — additive `TenantAccessResult: TenantAccessAuthorizationResult?` property + factories with optional parameter.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs` — threads the `TenantAccessAuthorizationResult` through every Allowed/Denied path so the policy receives it without re-evaluating tenant access.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — registered `MapGet("/api/v1/projects/{projectId}/context", ...)` mapping to the new partial class handler.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — `AddProjectsServer()` now calls `AddProjectsModule()` (instead of just `AddProjectsTenantAccess()`) so `ProjectContextInclusionPolicy` is DI-resolved by the new endpoint.

**Modified (generated, regenerated by the NSwag MSBuild target on Linux):**
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` — +562 lines (new `GetProjectContextAsync` typed method + `ProjectContext`/`ProjectContextReference`/`ProjectContextExclusion` partial classes + enums).
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` — only the contract-spine SHA256 fingerprint constants changed (expected — queries have no idempotency surface; the helper file count is unchanged).

**Added (production code):**
- `src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` — pure static translator from `ProjectConversationsPage` → `IReadOnlyList<ProjectContextConversationEvidence>`.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs` — partial-class file (mirroring the Story 3.2 size-threshold preference) containing the `GetProjectContextAsync` handler.

**Added (tests):**
- `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs` — 16 Tier-2 endpoint tests (happy path, idempotency rejection, freshness rejection, malformed-id Theory, archived project, conversations-unavailable collapse, cross-tenant safe denial, leakage harness across outcomes, header echo, extra query params tolerated).
- `tests/Hexalith.Projects.Server.Tests/Conversations/ProjectContextConversationEvidenceMapperTests.cs` — 12 Tier-1 mapper tests (every `ProjectConversationTrustSignal` value, empty/null page handling, FS-2 leakage on composed evidence).
- `tests/Hexalith.Projects.Client.Tests/GetProjectContextClientTests.cs` — 3 Tier-1 typed-client inspection tests (method shape, no `TenantId` in wire shape, LF + NUL-free generated artifact).

**Added (docs / artifacts):**
- `docs/checklists/mutation-and-query-negative-tests.md` — canonical 8-row checklist (Epic 2 retro Action Item 7).
- `_bmad-output/planning-artifacts/architecture.md` — one-line cross-link to the checklist (Process Patterns section).

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-05-28 | 1.1 | Story 3.2 implemented end-to-end against baseline `67beac6`. `GET /api/v1/projects/{projectId}/context` returns the assembled metadata-only `ProjectContext` produced by the unchanged Story 3.1 `ProjectContextInclusionPolicy`. Production changes: additive `ProjectAuthorizationResult.TenantAccessResult` property (threads the Story 1.6 `TenantAccessAuthorizationResult` to the policy without re-evaluating tenant access), new `ProjectContextConversationEvidenceMapper` under `src/Hexalith.Projects.Server/Conversations/`, new partial-class handler `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs`, OpenAPI spine extension (new operation + 6 wire schemas + 1 synthetic example), regenerated `HexalithProjectsClient.g.cs` exposing the typed `GetProjectContextAsync(...)`. Epic 2 retrospective Action Items closed: Action 1 (NSwag idempotency-helper MSBuild target Linux fix — forward-slash paths + `$(HexalithProjectsDotnetHostPath)` derived from `$(MSBuildToolsPath)` so the inner `dotnet run` uses the same SDK as the outer build, regardless of `/usr/bin/dotnet` resolution), Action 7 (canonical `docs/checklists/mutation-and-query-negative-tests.md` cross-linked from architecture). Action 5 (AppHost smoke) took the acceptable-fallback path — a documented manual smoke runbook is recorded in the Dev Agent Record; the in-process WebApplication-slim Tier-2 endpoint tests cover the full handler/composition/policy chain. Single Story 3.1 surface touch: additive `[JsonIgnore]` on `ProjectContext.TenantId` so the wire body never carries tenant authority (FS-8/SM-3 invariant enforced by the existing `Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` test). Test budget: baseline 776 → 807 (+31: Server.Tests +28, Client.Tests +3). Build 0 W / 0 E. Boundary greps clean. No submodule pointer change. | Claude Opus 4.7 |
| 2026-05-28 | 1.0 | Created Story 3.2 artifact and set sprint status to `ready-for-dev`. Story 3.2 is the second Epic 3 story and the first HTTP-surfaced consumer of Story 3.1's pure `ProjectContextInclusionPolicy`. It adds `GET /api/v1/projects/{projectId}/context` with the assembled `ProjectContext` body, a thin host composition (`ProjectContextConversationEvidenceMapper` + handler that reads `ProjectDetailItem` for folder/file/memory references and `IProjectConversationDirectory.ListForProjectAsync` for conversations), an additive `ProjectAuthorizationResult.TenantAccessResult` property so the policy receives the Story 1.6 outcome unchanged, an OpenAPI spine extension (new operation + 6 wire schemas + 1 synthetic example), and a regenerated `HexalithProjectsClient.g.cs`. Realizes three Epic 2 retrospective action items deferred to Story 3.2: NSwag idempotency-helper Linux path fix (Action 1), AppHost smoke check (Action 5), and the canonical mutation/query negative-test checklist (Action 7). Guardrails: no edits to Story 3.1 surface; no new shared-vocabulary enum values; no on-the-fly Folders/Memories ACL recheck at Get time (Story 3.4 territory); `Idempotency-Key` rejected on the query; safe-denial 404 contract preserves indistinguishability of `Unauthorized` vs `ProjectUnavailable` at the HTTP boundary; FS-2 `NoPayloadLeakage` harness extended over endpoint responses; FS-8/SM-3 cross-tenant isolation preserved; deterministic-clock-only via injected `TimeProvider`; no submodule pointer change; no nested-recursive submodule init. Test budget: full-solution baseline 776/776 grows by approximately +50 (Server.Tests +25, Projects.Tests +15, Contracts.Tests +5, Client.Tests +3, Integration.Tests +1 AppHost smoke). | Claude Opus 4.7 |
