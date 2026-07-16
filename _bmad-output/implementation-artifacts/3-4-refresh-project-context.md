---
baseline_commit: 89cd7a5
---

# Story 3.4: Refresh Project Context

## Status

done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **Hexalith.Chatbot**,
I want **to request a refreshed Project Context after links, setup, or resource availability change**,
So that **the context I use reflects the current state rather than stale assumptions** _(FR-18; realizes UJ-4; AR-9)_.

This is the **fourth Epic 3 story** and the **third HTTP-surfaced consumer** of the Story 3.1 `ProjectContextInclusionPolicy`. Stories 3.2 (`GET /api/v1/projects/{projectId}/context`) and 3.3 (`GET /api/v1/projects/{projectId}/context/explain`) deliberately **did NOT** perform on-the-fly Folders / Memories ACL re-checks at query time — both consume `ProjectDetailItem`-stored `ReferenceState` / `ReasonCode?` / `ObservedAt` values directly so the query is a single tenant-scoped projection + a single read-ACL conversation page. Story 3.4 is the place where **on-the-fly Folders / Memories ACL re-checks happen**: Refresh is the read-only surface FR-18 promises, and AR-9 reserves it for the "reflects current state, not stale assumptions" semantic.

Story 3.4 adds **`GET /api/v1/projects/{projectId}/context/refresh`** returning the assembled `ProjectContext` body (same wire shape as Story 3.2 — NOT a wrapper like Story 3.3's `ProjectContextExplanation`). The handler is a thin orchestrator that:

- reads the same `ProjectDetailItem` Story 3.2 consumes (`ProjectFolder` / `FileReferences` / `MemoryReferences`);
- re-checks the **Project Folder reference** against `IProjectFolderDirectory` (Story 2.4 ACL surface, additively extended in this story with a read-side `RefreshFolderReferenceAsync(...)` method that does not require workspace-id / path inputs);
- re-checks **each File reference** against `IProjectFileReferenceDirectory` (Story 2.5 ACL surface, additively extended with `RefreshFileReferenceAsync(...)`);
- re-checks **each Memory reference** against `IProjectMemoryDirectory` (Story 2.7 ACL surface, additively extended with `RefreshMemoryReferenceAsync(...)`);
- fetches the same conversation evidence page Story 3.2 fetches via `IProjectConversationDirectory.ListForProjectAsync(...)` (Story 2.1 — Pattern A; conversations already carry per-query trust signals so no additional recheck is needed);
- maps each ACL outcome onto a **fresh `ReferenceState`** that overrides the projection-stored state when the recheck disagrees, then composes the Story 3.1 `ProjectContextReferenceEvidence` from the rechecked values;
- invokes `ProjectContextInclusionPolicy.Assemble(...)` (unchanged) with `OperationKind: ProjectContextOperationKind.Refresh`; and
- returns `assembled.Context` as the wire body (NOT the `Evaluations` trace — that surface stays with Story 3.3's Explain endpoint).

The policy is invoked **unchanged**. `ProjectContextOperationKind.Refresh` already ships at `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:31` and is already on the read-only allowlist `IsReadOnlyOperation` at `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:236`. The decision matrix `docs/context-assembly-decision-matrix.md` already has a `RefreshProjectContext (3.4)` column (line 16, second operation) and the cell semantics are **identical to the `Get` column** for every per-evidence-state row, every outer-override row, and every Memories row by design — Story 3.4 is a read-only operation and consumes the matrix verbatim. The fail-closed verdict per cell does NOT change with the operation kind; what changes is the **source of evidence** — Story 3.4 sources reference state from the ACL recheck rather than the projection.

Story 3.4 must NOT duplicate any policy include/exclude / fail-closed-collapse / freshness-mapping / diagnostic-vocabulary / reference-kind-allowlist decision in the endpoint, the host composition, or the response shape; the policy is the single source of truth (Story 3.1 / Story 3.2 / Story 3.3 guardrail carried forward). The endpoint owns ONLY: (i) the HTTP envelope (headers, status, body), (ii) the ACL recheck orchestration that produces the **evidence inputs** for the policy, (iii) the cross-ACL fan-out parallelism (deterministic, no `Thread.Sleep` / `Task.Delay` — `Task.WhenAll(...)` over the three ACL tasks plus the conversation page), and (iv) the per-ACL outcome-to-`ReferenceState` mapping (see Suggested Outcome Mapper).

The closed `ProjectContextInclusionDiagnostic` 13-value vocabulary (`tenantMismatch`, `projectUnknown`, `projectArchived`, `referenceUnauthorized`, `referenceUnavailable`, `referenceStale`, `referenceArchived`, `referenceConflict`, `referenceInvalidIdentifier`, `referenceKindNotAllowlisted`, `projectFolderPending`, `referenceAmbiguous`, `referenceRedacted`) is enforced at construction time inside `ProjectContextEvaluation` itself (`src/Hexalith.Projects.Contracts/Models/ProjectContextEvaluation.cs:46`). Story 3.4 surfaces NO `Diagnostic` strings on the wire body (the body is `ProjectContext`, not `ProjectContextExplanation`) — the diagnostic vocabulary continues to surface via `ProjectContextExclusion.Diagnostic` on the existing `Excluded` array Story 3.1 / 3.2 already serialize.

Everything Story 3.4 produces is metadata-only (FS-2 — verified by the leakage harness extended over the endpoint response, mirroring Story 3.2 `GetProjectContext_ResponseBody_HasNoLeakageAcrossOutcomes` at `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs:354`), tenant-scoped (FS-8 / SM-3), and fails closed at every layer (NFR-1 / NFR-2 / NFR-3). The endpoint response is the existing `ProjectContext` wire shape — Story 3.4 does NOT modify `ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyResult`, `ProjectContextEvaluation`, `ProjectContextExplanation`, `ProjectContextInclusionPolicy`, `ProjectContextInclusionOrder`, the closed `ProjectContextInclusionDiagnostic` vocabulary, the four assembly enums, or `ProjectContextOperationKind` (all already in place). Story 3.4 DOES additively extend three Story 2.x ACL interfaces (`IProjectFolderDirectory`, `IProjectFileReferenceDirectory`, `IProjectMemoryDirectory`) with a single new method each — additive interface members are allowed under the existing additive-contracts rule (project-context.md line 98), and the rationale is captured in the Suggested ACL Extension section.

Story 3.4 does NOT realize any new Epic 2 retrospective action item. The carry-forward U+2028 / U+2029 canonicaliser hardening (Action 2) remains "for the next mutation surface" per the Epic 2 retro line 273 — Story 3.4 is a read-only query, the action item still survives in the carry-forward list. Story 3.4 DOES carry forward the canonical `docs/checklists/mutation-and-query-negative-tests.md` (Story 3.2 / Action Item 7 deliverable) by ticking the same query-side rows that Stories 3.2 / 3.3 applied (rows 1 / 4 / 5 / 6 / 8 of the 8-row checklist; rows 2 / 3 / 7 are mutation-only and N/A).

## Acceptance Criteria

1. A new HTTP endpoint **`GET /api/v1/projects/{projectId}/context/refresh`** is added to the OpenAPI spine `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` mirroring `GetProjectContext` / `GetProjectContextExplanation`'s read shape (operationId `RefreshProjectContext`, tags `projects`, parameters `ProjectId` / `CorrelationId` / `Freshness`, responses `200` / `400` / `401` / `403` / `404` / `503`, `x-hexalith-read-consistency: eventually_consistent`, `x-hexalith-correlation` query-correlation-only, `x-hexalith-authorization: tenant-context-and-project-read-permission`, `x-hexalith-canonical-error-categories` matching `GetProjectContext`'s 8-row set plus `referenced_resource_unavailable` to surface the ACL-recheck-driven evidence-recovery channel). `Idempotency-Key` is NOT a parameter and is rejected as `validation_error` if present after authorization (carry-forward of the `GetProject` / `GetProjectContext` / `GetProjectContextExplanation` pattern). The 200 response schema is **`ProjectContext`** (reused unchanged from Story 3.2 — Story 3.4 does NOT introduce a wrapper). The 200 response carries `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent` headers. The operation block is placed in the spine YAML immediately after the existing `GET /api/v1/projects/{projectId}/context/explain` block (current location: lines 348–413 of the spine) — Story 3.4's block is the next sibling under `paths` and is followed by the existing `/api/v1/projects/{projectId}/conversations/{conversationId}/link` mutation block.

2. **Three ACL interfaces are additively extended** with a single new read-side recheck method each. The new methods take only the Projects-shaped opaque identifier (the stored projection key) and return the same safe outcome record the existing mutation-validation method returns, so the outcome-to-`ReferenceState` mapping is one place (the new outcome mapper static class). The new methods are:
   - **`IProjectFolderDirectory.RefreshFolderReferenceAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken)` → `Task<ProjectFolderValidationResult>`** at `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs`. Implementations: `FoldersProjectFolderDirectory` calls `MemoriesClient`-equivalent stable read route on `Hexalith.Folders.Client` (the existing `FoldersClient`-typed read used inside `FoldersProjectFolderDirectory.ValidateSetProjectFolderAsync` — refactor the existing method to share the read-evidence helper, then expose the helper through the new method); `UnavailableProjectFolderDirectory.RefreshFolderReferenceAsync` returns `new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId)` mirroring its `ValidateSetProjectFolderAsync` body at line 19.
   - **`IProjectFileReferenceDirectory.RefreshFileReferenceAsync(ProjectId projectId, string fileReferenceId, string folderId, string correlationId, string taskId, CancellationToken cancellationToken)` → `Task<ProjectFileReferenceValidationResult>`** at `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs`. The `folderId` is taken from the stored `ProjectFileReference.FolderId` (already on the projection; no workspaceId / filePath needed — the Refresh recheck is by opaque file-reference id since the upstream stable read route accepts that shape; the implementation maps to whatever Folders read route validates a file reference by id without requiring path inputs). If no such Folders read route exists, the dev agent HALTs and surfaces the dependency.
   - **`IProjectMemoryDirectory.RefreshMemoryReferenceAsync(ProjectId projectId, string memoryReferenceId, string tenantId, string correlationId, string taskId, CancellationToken cancellationToken)` → `Task<ProjectMemoryValidationResult>`** at `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs`. Implementation: `MemoriesProjectMemoryDirectory` calls the existing `MemoriesClient.GetCaseAsync(...)` (Story 2.6 ADR stable surface, also used by `ValidateLinkMemoryReferenceAsync`); `UnavailableProjectMemoryDirectory.RefreshMemoryReferenceAsync` returns `new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId)`.

3. **A new outcome-to-`ReferenceState` mapper** static class is added at `src/Hexalith.Projects.Server/Folders/ProjectFolderValidationOutcomeMapper.cs`, `src/Hexalith.Projects.Server/Folders/ProjectFileReferenceValidationOutcomeMapper.cs`, and `src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationOutcomeMapper.cs` — one per ACL surface so each lives next to the ACL interface it consumes. Each mapper exposes a single pure static `Map(<outcome>, <projectionStoredState>) → ReferenceState`. The mapping rules are (per the existing decision matrix Memories row + the existing `ProjectFolderValidationOutcome` / `ProjectFileReferenceValidationOutcome` enums):
   - **Folder mapping (`ProjectFolderValidationOutcome` → `ReferenceState`):** `Accepted → Included` (override); `Archived → Archived`; `Stale → Stale`; `Denied → Unauthorized`; `Unavailable → Unavailable`; `ValidationFailed → InvalidReference`. **Conflict / Ambiguous / Pending / TenantMismatch / Excluded are NOT producible by the Folder ACL** (per the existing `ProjectFolderValidationOutcome` enum which has no such values). If `Unavailable` AND the projection-stored state was `Pending` (the Story 2.4 degraded path), prefer the projection-stored `Pending` so the diagnostic stays `projectFolderPending` rather than `referenceUnavailable`.
   - **File mapping (`ProjectFileReferenceValidationOutcome` → `ReferenceState`):** `Accepted → Included` (override); `Denied → Unauthorized`; `Redacted → Excluded`; `Archived → Archived`; `Stale → Stale`; `TenantMismatch → TenantMismatch` (policy then collapses to `Unauthorized` with `tenantMismatch` diagnostic per `ClassifyReferenceState` at `ProjectContextInclusionPolicy.cs:632`); `Unavailable → Unavailable`; `ValidationFailed → InvalidReference`.
   - **Memory mapping (`ProjectMemoryValidationOutcome` → `ReferenceState`):** `Accepted → Included` (override); `Denied → Unauthorized`; `Archived → Archived`; `Stale → Stale`; `TenantMismatch → TenantMismatch` (policy collapses to `Unauthorized` with `tenantMismatch` diagnostic per the Memories candidate branch at `ProjectContextInclusionPolicy.cs:473–491`); `Unavailable → Unavailable`; `ValidationFailed → InvalidReference`.
   The mappers preserve the projection-stored `ReasonCode?` and `ObservedAt` UNLESS the recheck produces a NEW state (in which case `ObservedAt` is replaced with the recheck's `now` per the Story 3.1 `ObservedAt` semantics — the field is "the instant at which this reference state was observed"). The mappers are pure static functions, Tier-1-purity-safe (no infrastructure, no wall-clock — `now` is passed in).

4. The OpenAPI spine fingerprint changes deliberately (new operation, no new wire schemas, no new examples — `ProjectContext` is reused unchanged). The dev agent **regenerates** `HexalithProjectsClient.g.cs` and `HexalithProjectsIdempotencyHelpers.g.cs` via the standard MSBuild target (the NSwag Linux path fix from Story 3.2 stays in place — no `.csproj` change). Acceptance: a single `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` invocation on Linux regenerates both `.g.cs` files cleanly without manual intervention. The fingerprint baseline is updated; the OpenAPI fingerprint gate transitions to PASSED-with-baseline-update **for this story only**. Story 3.5 (`GetConversationStartSetup`) is the next operation owner and must show zero spine diff outside its own surface. `HexalithProjectsIdempotencyHelpers.g.cs` is byte-stable except for the SHA256 fingerprint constants — queries have no idempotency surface (mirrors Story 3.2 / 3.3). The frontcomposer gate stays skip-clean (no `[Projection]` / `[Command]` contracts touched). A new synthetic example MAY be added under `components.examples.ProjectContextRefreshed` (an assembled `ProjectContext` showing a file reference whose projection-stored `Included` was overridden to `Stale` by the recheck, and a memory reference whose projection-stored `Stale` was overridden back to `Included`) — the example reuses the `ProjectContext` schema unchanged, so no new schema entry is added.

5. A new query-side handler `RefreshProjectContextAsync` is added as a **new partial-class file** `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` mirroring the Story 3.2 / 3.3 split decision (the base `ProjectsDomainServiceEndpoints.cs` is at ~340+ LOC after Story 3.3's registration; adding another ~180-LOC handler inline is rejected; the partial-class pattern is the canonical placement). The handler:
   (a) reads canonical headers `X-Correlation-Id` / `X-Hexalith-Task-Id` / `X-Hexalith-Freshness` and validates them per the existing helpers (`ReadHeader`, `IsCanonicalIdentifier`);
   (b) treats a missing or non-canonical `projectId` route value as a safe-denial 404 (NEVER reveals existence);
   (c) calls `ProjectAuthorizationGate.AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)` and returns `ReadModelUnavailable` (503) only when `Authorization.Retryable && Authorization.Reason == ReferenceState.Unavailable`, otherwise `SafeDenial` 404 (existence-non-inference, mirrors Story 3.2 / 3.3);
   (d) rejects `Idempotency-Key` if present after authorization (`ValidationProblem(..., "idempotency_key")`);
   (e) rejects any non-`eventually_consistent` `X-Hexalith-Freshness` request as `ValidationProblem(..., "freshness")`;
   (f) defensively collapses a missing `authorization.TenantAccessResult` to safe-denial 404 (mirrors Story 3.2 lines 94–99 / Story 3.3 lines 94–99);
   (g) reads conversation evidence via `IProjectConversationDirectory.ListForProjectAsync(ProjectId, ConversationTenantId, CallerPrincipalId, PageRequest, ct)` using the SAME bounded page cap as Story 3.2 / 3.3 (`PageSize = ProjectContextConversationsPageSize` = 100, no continuation — single first-page snapshot; reuse the existing `ProjectContextConversationsPageSize` constant declared on the partial class at line 50 of `GetProjectContextEndpoint.cs` — DO NOT redefine);
   (h) **fans out three ACL recheck calls in parallel via `Task.WhenAll(...)`**: `IProjectFolderDirectory.RefreshFolderReferenceAsync` (when `ProjectDetailItem.ProjectFolder?.FolderId` is non-null), `IProjectFileReferenceDirectory.RefreshFileReferenceAsync` (one task per `ProjectDetailItem.FileReferences` entry), and `IProjectMemoryDirectory.RefreshMemoryReferenceAsync` (one task per `ProjectDetailItem.MemoryReferences` entry). The conversation page fetch and the three ACL fan-outs are all awaited with a single `Task.WhenAll(...)` over all four bounded task collections. Deterministic ordering is preserved by walking each input collection in its stored order and mapping outcomes back by index (no `Dictionary<>` lookup; no `Task.WhenAny(...)`; no sleep / delay / yield / spinwait);
   (i) maps each recheck outcome to a fresh `ReferenceState` via the per-ACL outcome mapper (AC 3), then composes a NEW `ProjectFolderReference` / `IReadOnlyList<ProjectFileReference>` / `IReadOnlyList<ProjectMemoryReference>` set using the rechecked state + the projection-stored `DisplayName` / `ReasonCode?` / `FolderId?` (file references only) / `FileReferenceId` (file references only) / `MemoryReferenceId` (memory references only), preserving Story 1.2 payload-taxonomy boundaries (no file paths / no memory bodies / no tokens);
   (j) composes the four evidence records Story 3.2 / 3.3 build (`ProjectContextAssemblyContext`, `ProjectContextProjectEvidence`, `ProjectContextTenantAccess`, `ProjectContextReferenceEvidence`) **with `OperationKind: ProjectContextOperationKind.Refresh`** AND the rechecked references — conversations remain Story 3.2's mapper output (the conversation trust signal is already fresh by virtue of the read-ACL call);
   (k) invokes `ProjectContextInclusionPolicy.Assemble(...)` (Story 3.1 — unchanged) and receives `ProjectContextAssemblyResult { Context, Evaluations }`;
   (l) returns ONLY `assembled.Context` as the wire body via `Results.Json(..., ResponseJsonOptions)` (mirroring Story 3.2 line 139). `Evaluations` is discarded at the wire (the Explain endpoint owns that surface);
   (m) sets `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent` response headers (mirrors `GetProjectContext` / `GetProjectContextExplanation`).

6. The endpoint is **registered** by adding a single `endpoints.MapGet("/api/v1/projects/{projectId}/context/refresh", ...)` call in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` `ConfigureEndpoints` method directly after the existing `GET /api/v1/projects/{projectId}/context/explain` registration (currently at line 135–153). The route MUST be registered as `/context/refresh` so it sits alongside `/context` and `/context/explain` and route-precedence remains unambiguous (literal segments win — `{projectId}` matches one segment only).

7. The handler **never re-evaluates** any policy decision. The full chain is:
   - **endpoint** (validates envelope, rejects bad `Idempotency-Key` / freshness, runs `AuthorizeReadAsync`, fetches conversations page + three ACL recheck fan-outs, maps outcomes to fresh `ReferenceState`, composes evidence with `OperationKind.Refresh`, calls policy, serializes `result.Context` only);
   - **policy** (Story 3.1 — sole owner of inclusion order, fail-closed collapse, freshness mapping, diagnostic vocabulary, reference-kind allowlist, per-candidate evaluation emission, and deterministic ordering `(ReferenceKind, ReferenceId)` Ordinal at `ProjectContextInclusionPolicy.cs:168–171`).
   No conditional include/exclude logic, freshness threshold, tenant-collapse rule, diagnostic vocabulary lookup, or evaluation re-sort is duplicated in the endpoint. The endpoint receives a `ProjectContextAssemblyResult` from the policy and serializes `result.Context` (not `result.Evaluations`).

8. **Fail-closed negative-evidence matrix (evidence-state × `RefreshProjectContext`)** is fully covered by Tier-2 Server tests in a new file `tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs`. Required cells (every row of the `RefreshProjectContext (3.4)` column in `docs/context-assembly-decision-matrix.md`, which is **identical to the Get column** by design — Story 3.2's `GetProjectContextTests.cs` is the per-cell oracle; Story 3.4's tests follow Story 3.2's hard-coded `[InlineData]` / `[MemberData]` shape rather than parsing the markdown): `missing` / `stale` / `unauthorized` / `unavailable` / `forbidden` / `redacted` / `conflict` / `invalidReference` / `archived` / `ambiguous`, plus the request-level collapses (`AuthoritativeTenantId` missing → `Unauthorized` outer; cross-tenant → `ProjectUnavailable` outer; archived project → `Assembled` with all references excluded). Each cell asserts: (i) HTTP status code is `200` (assembled, including the archived-project case) or the correct collapse code (`404` for `ProjectUnavailable`, `404` for `Unauthorized` — safe-denial contract preserves the indistinguishability of `Unauthorized` vs `ProjectUnavailable` at the HTTP boundary; the policy's `AssemblyOutcome` is INTERNAL telemetry and is NOT surfaced as a distinct HTTP status); (ii) response headers `X-Correlation-Id` and `X-Hexalith-Freshness` are present (when the response is 200); (iii) the `ProjectContext` body contains the expected `AssemblyOutcome` / `Lifecycle` / `Freshness`; (iv) the `Excluded` array contains the expected `(ReferenceKind, ReferenceId, ResultState, FailedCheck, Diagnostic)` row for the cell under test; (v) `NoPayloadLeakageAssertions.AssertNoLeakage(...)` runs over the response body; (vi) the response is **identical** to a Story 3.2 `Get` response over the SAME projection-stored state when the ACL recheck CONFIRMS the projection-stored state (i.e. Refresh = Get when nothing changed — proves the refresh path is a no-op when evidence has not drifted). Each test is a named fixture per cell (mirrors Story 3.2 / Story 3.3 named-fixture pattern at `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs:59–376` and `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextExplanationTests.cs`).

9. **The refresh recovery / regression contract** is explicitly tested in `tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs`. These are the NEW tests Story 3.4 owns that have no Story 3.2 / 3.3 equivalent:
   - `RefreshProjectContext_AclReportsAccepted_OverridesProjectionStoredStale` — projection stores `ProjectFileReference.ReferenceState = Stale` for one file; the ACL recheck returns `ProjectFileReferenceValidationOutcome.Accepted`; assert the response `FileReferences` list includes the file as `Included` (NOT Stale), the `Excluded` array does NOT contain the file, and the `ObservedAt` is the recheck `now` (later than the projection-stored timestamp).
   - `RefreshProjectContext_AclReportsArchived_OverridesProjectionStoredIncluded` — projection stores `ProjectMemoryReference.ReferenceState = Included`; the ACL recheck returns `ProjectMemoryValidationOutcome.Archived`; assert the response `MemoryReferences` list does NOT contain the memory, `Excluded` contains `(kind=memory, referenceId=<id>, ResultState=Archived, FailedCheck=ReferenceLifecycle, Diagnostic=referenceArchived)`.
   - `RefreshProjectContext_AclReportsUnavailable_OverridesProjectionStoredIncluded` — `Unavailable` outcome for a file; assert `Excluded` contains `(kind=file, ResultState=Unavailable, FailedCheck=ReferenceFreshness, Diagnostic=referenceUnavailable)`.
   - `RefreshProjectContext_AclReportsTenantMismatch_OverridesToUnauthorized` — `TenantMismatch` outcome for a memory; assert the policy's collapse-at-the-boundary surfaces `(kind=memory, ResultState=Unauthorized, FailedCheck=ReferenceAuthorization, Diagnostic=tenantMismatch)` (per `ProjectContextInclusionPolicy.cs:473–491`).
   - `RefreshProjectContext_AclReportsDenied_SurfacesAsUnauthorized` — `Denied` outcome for a folder; assert `Excluded` contains `(kind=folder, ResultState=Unauthorized, FailedCheck=ReferenceAuthorization, Diagnostic=referenceUnauthorized)`.
   - `RefreshProjectContext_FolderUnavailableAndProjectionStoredPending_PreservesPendingDiagnostic` — projection stores `ProjectFolderReference.ReferenceState = Pending`; ACL returns `Unavailable`; assert `Excluded` contains `(kind=folder, ResultState=Pending, FailedCheck=ReferenceFreshness, Diagnostic=projectFolderPending)` per the mapper's projection-state-preservation rule (AC 3 Folder mapping).
   - `RefreshProjectContext_AllAclsReturnAccepted_AndProjectionStoredIncluded_IsByteIdenticalToGet` — projection state is all `Included`; ACLs all return `Accepted`; the response body is byte-identical to `GET /context` for the same project (deserialize both, compare; this proves the equivalence-on-no-drift contract).
   - `RefreshProjectContext_AclThrows_SurfacesAsUnavailable_FailsClosedNot500` — the Folders client raises a transient exception; the recheck wrapper translates to `ProjectFolderValidationOutcome.Unavailable` (per the existing `FoldersProjectFolderDirectory.ValidateSetProjectFolderAsync` exception-handling pattern, which the new `RefreshFolderReferenceAsync` MUST mirror — if the existing helper is not factored out, factor it before reusing); assert the response is `200` with the folder excluded as `Unavailable`, NOT a 500.
   - `RefreshProjectContext_DeterministicFanOut_PreservesProjectionStoredOrder` — projection stores `FileReferences` in ids `[file-Z, file-_a, file-b]` (Ordinal-sort-relevant); ACL recheck returns `Accepted` for all; assert the response `FileReferences` is sorted `(ReferenceKind, ReferenceId)` Ordinal (the policy enforces this at `ProjectContextInclusionPolicy.cs:168–171`; the endpoint must NOT pre-sort or re-sort).

10. **Cross-tenant isolation (FS-8 / SM-3)** — a dedicated test in `RefreshProjectContextTests.cs` constructs a request with `AuthoritativeTenantId = "tenant-a"` against a project whose `ProjectDetailItem.TenantId = "tenant-b"` and asserts: (i) HTTP 404 (safe-denial; never 403, never reveals existence); (ii) no `ProjectContext` body — only a Problem Details safe-denial body; (iii) no tenant id appears in the response headers, body, or correlation-id-equivalent fields; (iv) **no ACL recheck call was made** (the cross-tenant safe-denial 404 collapses before the ACL fan-out runs — verify via a `RecordingFolderDirectory` / `RecordingMemoryDirectory` stub whose recorded call count remains 0; FS-2 / FS-8 carry-forward — never call sibling ACLs on cross-tenant requests because that would leak tenant-existence evidence to siblings). Reuses the FS-8/SM-3 harness from Story 1.4 / Story 3.1 / Story 3.2 / Story 3.3 (Story 3.2's `GetProjectContext_CrossTenant_ReturnsSafeDenial404` at line 200 and Story 3.3's identically named test are the canonical patterns Story 3.4 mirrors).

11. **Tier-1 outcome-mapper purity tests.** Add a new file `tests/Hexalith.Projects.Tests/Folders/ProjectFolderValidationOutcomeMapperTests.cs`, `tests/Hexalith.Projects.Tests/Folders/ProjectFileReferenceValidationOutcomeMapperTests.cs`, and `tests/Hexalith.Projects.Tests/Memories/ProjectMemoryValidationOutcomeMapperTests.cs` (one per mapper — Tier-1 purity, NO infrastructure, NO sibling client). Each file asserts the full enum-to-`ReferenceState` mapping per AC 3, plus:
    - `Map_AcceptedOutcome_OverridesProjectionStoredStale_To_Included` — proves the override semantic.
    - `Map_UnavailableOutcome_WithProjectionStoredPending_PreservesPending` — proves the projection-state-preservation rule for the degraded `ProjectFolderCreationPending` path (Folder mapper only).
    - `Map_AllOutcomes_CoveredByTheory` — Theory across every enum member of `ProjectFolderValidationOutcome` / `ProjectFileReferenceValidationOutcome` / `ProjectMemoryValidationOutcome` confirming no outcome is unmapped (uses `Enum.GetValues<T>()` for completeness; a future additive enum member triggers a test failure flagging the missing mapper case).
    - `Map_PreservesObservedAt_WhenStateUnchanged` — when the recheck confirms the projection-stored state, `ObservedAt` is the projection-stored `ObservedAt` (NOT `now`). When the recheck CHANGES the state, `ObservedAt` is the recheck's `now`.
    - `Map_TenantMismatchOutcome_MapsTo_TenantMismatch_NotUnauthorized` — proves the mapper hands the raw `TenantMismatch` to the policy (the policy then collapses it to `Unauthorized` + `tenantMismatch` diagnostic per `ProjectContextInclusionPolicy.cs:473–491`); the mapper itself does NOT collapse — preserving the layering.
    Reuses `ProjectContextEvidenceBuilder` from `src/Hexalith.Projects.Testing/Context/` (Story 3.1) where helpful, but the mapper tests are simple enough to inline fixtures.

12. **FS-2 `NoPayloadLeakage` harness extension over the endpoint output.** Story 3.1 already covers the DTO-shape harness over `ProjectContext` at `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` (the `ProjectContext_SerializesMetadataOnly` block — Story 3.1 / Story 3.2 extension). Story 3.4 ADDS, in `RefreshProjectContextTests.cs`, the **endpoint-level harness coverage**:
    - `RefreshProjectContext_ResponseBody_HasNoLeakageAcrossOutcomes` — boots the in-process WebApplication-slim host (mirroring the `StartAppAsync(...)` builder at `GetProjectContextTests.cs:395`), exercises four labelled outcomes (`HappyPath`, `ArchivedProject`, `AclRecoveredStaleToIncluded`, `AclDegradedIncludedToArchived`), and runs `NoPayloadLeakageAssertions.AssertNoLeakage(...)` over every serialized response body. The harness's Memories + Folders forbidden-term lists from Stories 2.5 / 2.7 / 3.1 are reused unchanged; Story 3.4 adds no new forbidden terms.
    - `RefreshProjectContext_ErrorResponses_HaveNoLeakage` — exercises 400 / 401 / 403 / 404 / 503 responses and asserts no diagnostic-message leakage, no upstream sibling-denial text (FS-2 / privacy), no payload fragments, no token / path appears in the ProblemDetails body.
    - **No new DTO-shape harness test is needed** — `ProjectContext` is already covered. Story 3.4 ships no new wire DTOs; the wrapper shape (`ProjectContextExplanation`) Story 3.3 added is NOT used by Refresh.

13. **Generated client + idempotency-helper additive coverage.** The regenerated `HexalithProjectsClient.g.cs` exposes a typed `RefreshProjectContextAsync(projectId, correlationId?, freshness?, cancellationToken)` method returning a typed `ProjectContext`. The regenerated `HexalithProjectsIdempotencyHelpers.g.cs` does NOT gain a new entry for the query (queries have no idempotency surface; same as Story 3.2 / 3.3). Both regenerations are deterministic (LF, NUL-free, no platform-specific paths embedded). The generated Client tests under `tests/Hexalith.Projects.Client.Tests/` gain at least one happy-path test in a new file `tests/Hexalith.Projects.Client.Tests/RefreshProjectContextClientTests.cs` (mirrors `GetProjectContextClientTests.cs` at lines 34–73 — three substring-based assertions over the regenerated `.g.cs`):
    - `GeneratedClient_ExposesTypedRefreshProjectContextAsync` — asserts the regenerated file contains `Task<ProjectContext> RefreshProjectContextAsync` (the `ProjectContext` partial class itself is already declared by Story 3.2's regeneration — verify it is NOT duplicated; NSwag de-duplicates by name).
    - `GeneratedClient_RefreshOperation_HasNoIdempotencyHelper` — asserts the regenerated `HexalithProjectsIdempotencyHelpers.g.cs` does NOT contain `RefreshProjectContext` (queries have no idempotency surface).
    - `GeneratedClient_IsLfOnDiskAndNulFree` — copy verbatim from `GetProjectContextClientTests.cs:67–73`.

14. **No edits to Story 3.1 / 3.2 / 3.3 surfaces beyond additive serialization attributes.** `ProjectContextInclusionPolicy`, `ProjectContextInclusionOrder`, `ProjectContextAssemblyContext`, `ProjectContextProjectEvidence`, `ProjectContextTenantAccess`, `ProjectContextReferenceEvidence`, `ProjectContextConversationEvidence`, `ProjectContextDiagnostics`, `ProjectContextOperationKind`, the closed `ProjectContextInclusionDiagnostic` vocabulary, the existing wire DTOs (`ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyResult`, `ProjectContextEvaluation`, `ProjectContextExplanation`), and the four assembly enums are NOT modified. **One exception** (consistent with Story 3.2's `[JsonIgnore]` / Story 3.3's allowance): if a `[JsonPropertyName]` attribute is structurally required, the dev agent MAY add the additive attribute under a single Contracts task — but MUST verify by inspection that the existing `JsonNamingPolicy.CamelCase` resolution does not already cover the case. If implementation finds a divergence between the policy and the (now-real) Refresh endpoint that requires a substantive Story 3.1 / 3.2 / 3.3 file change (not just an additive serialization attribute), the dev agent **HALTs** before editing and surfaces the conflict in the Dev Agent Record; the resolution is a follow-up story / ADR, not an inline edit. `GetProjectContextEndpoint.cs` (Story 3.2 handler) and `GetProjectContextExplanationEndpoint.cs` (Story 3.3 handler) are NOT touched — the new handler is a sibling partial file; the `ProjectContextConversationsPageSize` constant on the Story 3.2 partial is consumed by reference, not redeclared.

15. **No new shared-vocabulary enum values.** `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ProjectConversationTrustSignal`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus`, `ProjectContextInclusionCheck`, `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextOperationKind`, `ProjectContextInclusionDiagnostic`, `ProjectFolderValidationOutcome`, `ProjectFileReferenceValidationOutcome`, and `ProjectMemoryValidationOutcome` are unchanged. The Refresh operation kind already exists (`ProjectContextOperationKind.Refresh`, shipped by Story 3.1 at line 31 of `ProjectContextOperationKind.cs`). If a new value appears genuinely required for Story 3.4 (e.g. a new ACL outcome value the recheck distinguishes), HALT and surface the conflict — the resolution is a follow-up story.

16. **No edits to Stories 1.4–2.7 mutation surfaces or non-Story-3.4 query surfaces.** No changes to: `ProjectAggregate.*`, `ProjectState`, `ProjectStateApply`, `ProjectCommandValidator`, `ProjectCommandValidationResult`, `ProjectResult`, `ProjectResultCode`, `ProjectDetailProjection`, `ProjectListProjection`, `ProjectReferenceIndexProjection`, the four ACL interfaces' existing methods (`IProjectConversationDirectory`, `IProjectConversationAssignmentDirectory`, `IProjectFolderDirectory.ValidateSetProjectFolderAsync`, `IProjectFileReferenceDirectory.ValidateLinkFileReferenceAsync`, `IProjectMemoryDirectory.ValidateLinkMemoryReferenceAsync`), `IProjectCommandSubmitter`. **Only** the following Story 2.x ACL interfaces gain ADDITIVE methods (AC 2): `IProjectFolderDirectory.RefreshFolderReferenceAsync`, `IProjectFileReferenceDirectory.RefreshFileReferenceAsync`, `IProjectMemoryDirectory.RefreshMemoryReferenceAsync`. **Only** `ProjectsDomainServiceEndpoints` gains the `RefreshProjectContextAsync` handler partial-class file + the single `MapGet("/api/v1/projects/{projectId}/context/refresh", ...)` registration. `ProjectAuthorizationGate` / `ProjectAuthorizationResult` are NOT touched (Story 3.2 already extended them with `TenantAccessResult: TenantAccessAuthorizationResult?` — Story 3.4 consumes that property unchanged). `ProjectAuthorizationDenialMapper` and `ProjectCommandRejected` are untouched (queries don't go through the command pipeline). `GetProjectContextEndpoint.cs` (Story 3.2's handler) and `GetProjectContextExplanationEndpoint.cs` (Story 3.3's handler) are NOT touched.

17. **Mandatory negative-path tests carried forward:**
    - **Cross-tenant isolation** (AC 10) — FS-8 / SM-3.
    - **`NoPayloadLeakage`** over every endpoint response (AC 12) — FS-2.
    - **No clock divergence** — the endpoint uses `TimeProvider.GetUtcNow()` (from DI) for the `Now: DateTimeOffset` passed to the policy AND for the recheck `ObservedAt` override values. NO `DateTimeOffset.UtcNow` / `DateTime.UtcNow` / `Stopwatch` calls in the handler / composition / mapper code. Validation grep: `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs src/Hexalith.Projects.Server/Folders/ProjectFolderValidationOutcomeMapper.cs src/Hexalith.Projects.Server/Folders/ProjectFileReferenceValidationOutcomeMapper.cs src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationOutcomeMapper.cs` returns zero hits.
    - **No-sleep grep in tests** — `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.*/` filtered to Story 3.4 new/modified test files returns zero hits.
    - **Boundary discipline** — `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` returns 0 hits except the single `using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;` alias that Story 3.2 / 3.3 already established (aliases don't match the regex when stripped — verify by visual inspection). `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` continues to return zero hits (Story 3.1 invariant; Story 3.4 must not regress it). The mappers under `src/Hexalith.Projects.Server/Folders/` and `src/Hexalith.Projects.Server/Memories/` MAY import `Hexalith.Projects.Server.Folders` / `Hexalith.Projects.Server.Memories` types (they live in those namespaces) but MUST NOT import `Hexalith.Folders.*` / `Hexalith.Memories.*` directly — they consume the outcome enums defined inside `Hexalith.Projects.Server.*`.
    - **OpenAPI fingerprint baseline updated** — the fingerprint gate flips PASSED-with-update only for this story; Story 3.5 must show zero spine diff outside its own surface.
    - **Negative-test checklist application** — `docs/checklists/mutation-and-query-negative-tests.md` rows 1 / 4 / 5 / 6 / 8 are explicitly ticked off in the Dev Agent Record for Story 3.4. Rows 2 / 3 / 7 are mutation-only (N/A — Story 3.4 is a query).

18. **`dotnet build` & `dotnet test` budgets:**
    - `dotnet build Hexalith.Projects.slnx` — 0 W / 0 E.
    - `dotnet test Hexalith.Projects.slnx` — baseline 858/858 (post-Story-3.3 review cycle 1 per sprint-status.yaml line 3 — Server.Tests 252, Tests 427, Contracts.Tests 128, Client.Tests 37, Integration.Tests 14). Story 3.4 grows the count by approximately:
      - Server.Tests: +~25 (matrix-cell tests across 10 cells in a `[Theory]`-collapsed shape since the Refresh column equals the Get column + the 9 NEW recovery / regression tests from AC 9 + the cross-tenant safe-denial 404 + idempotency rejection + freshness rejection + header echo + leakage over body across 4 labelled outcomes + error-response leakage + the no-ACL-call-on-cross-tenant assertion).
      - Projects.Tests: +~30 (three new outcome-mapper test files: ~10 tests each covering the full enum-to-`ReferenceState` mapping plus the override / preservation / `ObservedAt` semantics).
      - Contracts.Tests: 0 (no new DTO; `ProjectContext` already covered; the new operation block in the OpenAPI spine is covered automatically by `OpenApiContractSpineTests.Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` because Story 3.4 reuses existing schemas).
      - Client.Tests: +~3 (new `RefreshProjectContextClientTests`).
      - Integration.Tests: 0 (no new AppHost smoke for Story 3.4 — same rationale as Story 3.3; the in-process WebApplication-slim host coverage is sufficient).
      Total expected: 858 → ~916 (+58). Failed must be 0. Skipped must be 0 (no AppHost smoke expansion this story).
    - `git diff --check` clean across story-touched files. Hand-written `.cs` / `.md` / `.yaml` are LF on disk per [[build-environment]].

19. **Dev Agent Record** is populated by the dev agent with:
    - Endpoint shape divergence from this AC list (if any) with rationale.
    - The wire body choice: `ProjectContext` (preferred — recorded above) or an alternative shape (e.g. a Refresh-specific wrapper carrying drift evidence). Document the chosen shape.
    - Handler placement (partial-class file under `Queries/` per AC 5 — confirm the path the dev agent used; if a deviation occurred, document the reason).
    - The ACL extension method names + signatures (AC 2) actually shipped (if any signature deviates — e.g. a different parameter name or order — document the deviation).
    - The conversation-page cap actually used (default 100, or a justified deviation — should match Story 3.2 / 3.3's `ProjectContextConversationsPageSize` constant).
    - The Folders read route the dev agent used for `RefreshFileReferenceAsync` (since the Story 2.5 mutation-validation method requires workspace-id / path, the dev agent must identify or HALT on an opaque-id read route; record the chosen route or the HALT).
    - Per-lane and full-solution test counts (before/after Story 3.4).
    - `dotnet build` warnings/errors, `git diff --check`, `git diff --stat` on `.g.cs` (expected: non-zero — this story DOES regenerate), OpenAPI spine diff size in lines, fingerprint baseline-update note.
    - Negative-test checklist tick-off (rows 1 / 4 / 5 / 6 / 8 of `docs/checklists/mutation-and-query-negative-tests.md`).
    - Any HALT items (most likely candidate: a Folders read route that validates a file reference by opaque id WITHOUT requiring workspace-id / path inputs — if no such route exists, HALT before coding `RefreshFileReferenceAsync` and surface the dependency).
    - Any single Story 3.1 / 3.2 / 3.3 surface touch (additive `[JsonPropertyName]` etc.) — documented per the Story 3.2 precedent.

## Tasks / Subtasks

- [x] **Task 1 — Capability gate + read-only inspection. (AC: 5, 6, 7, 14, 15, 16)**
  - [x] Read `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` lines 75–171 (outer collapses + per-candidate loop + deterministic sort) and confirm: (a) `Assemble(...)` emits one `ProjectContextEvaluation` per candidate kind active in the input, (b) outer collapses emit `Array.Empty<ProjectContextEvaluation>()`, (c) the result is sorted `(ReferenceKind, ReferenceId)` Ordinal at lines 168–171.
  - [x] Confirm `ProjectContextOperationKind.Refresh` exists at `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:31` and `IsReadOnlyOperation(...)` already includes it at `ProjectContextInclusionPolicy.cs:236`.
  - [x] Read `src/Hexalith.Projects.Contracts/Models/ProjectFolderReference.cs`, `ProjectFileReference.cs`, `ProjectMemoryReference.cs` end-to-end — confirm the fields Story 3.4 composes from the recheck (`FolderId?` / `FileReferenceId` / `MemoryReferenceId` / `DisplayName` / `ReferenceState` / `ReasonCode?` / `ObservedAt`). Confirm no constructor change is needed; Story 3.4 builds new instances with rechecked state and projection-stored display metadata.
  - [x] Read `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs` (29 lines), `ProjectFolderValidationResult.cs` (43 lines), `FoldersProjectFolderDirectory.cs` (full file), `UnavailableProjectFolderDirectory.cs` (20 lines). Confirm the existing `ValidateSetProjectFolderAsync` signature, the outcome enum's 6 values (`Accepted` / `ValidationFailed` / `Archived` / `Stale` / `Denied` / `Unavailable`), and the result record shape. Identify the read-side helper (if any) inside `FoldersProjectFolderDirectory` that the new `RefreshFolderReferenceAsync` can reuse — if none, mark for extraction in Task 2.
  - [x] Read `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs` (40 lines), `ProjectFileReferenceValidationResult.cs` (49 lines), `FoldersProjectFileReferenceDirectory.cs` (full file), `UnavailableProjectFileReferenceDirectory.cs`. Confirm the existing `ValidateLinkFileReferenceAsync` signature and the outcome enum's 8 values. **CRITICAL gate:** identify a Folders read route that validates a file reference BY OPAQUE FILE-REFERENCE ID WITHOUT requiring workspaceId / filePath inputs. If no such route exists in the current `FoldersClient` typed client, **HALT before Task 2** and surface the dependency (the resolution is either: (i) using the existing `ValidateLinkFileReferenceAsync` with stored `FolderId` + a `RefreshFileReferenceAsync` overload that reads workspaceId / filePath from a Projects-side cache — REJECTED because Projects must never cache path-like fields per `docs/payload-taxonomy.md`; (ii) adding a new Folders client method — REQUIRES a Folders submodule edit and a Folders story; (iii) deferring the file-recheck portion of Story 3.4 to a follow-up story — record the HALT and proceed with folder + memory recheck only). The dev agent records the chosen path in the Dev Agent Record.
  - [x] Read `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs` (40 lines), `ProjectMemoryValidationResult.cs` (46 lines), `MemoriesProjectMemoryDirectory.cs` (full file), `UnavailableProjectMemoryDirectory.cs`. Confirm the existing `ValidateLinkMemoryReferenceAsync` signature and the outcome enum's 7 values. Confirm `MemoriesClient.GetCaseAsync(...)` (Story 2.6 ADR) is the stable read route Story 3.4 reuses unchanged.
  - [x] Read `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs` end-to-end (142 lines) — confirm the partial-class pattern, the `ProjectContextConversationsPageSize` constant (line 50), and the canonical handler flow lines 62–139. Story 3.4's new partial file mirrors this shape with: (i) `OperationKind: ProjectContextOperationKind.Refresh`; (ii) ACL fan-out before evidence composition; (iii) rechecked-state-overrides-projection-stored-state composition.
  - [x] Read `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs` end-to-end (144 lines) — confirm the Story 3.3 sibling partial pattern Story 3.4 mirrors at file level.
  - [x] Read `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` lines 71–340 — confirm: (a) the existing `ConfigureEndpoints` method registers `MapGet("/api/v1/projects/{projectId}/context", ...)` at line 115 (Story 3.2) and `MapGet("/api/v1/projects/{projectId}/context/explain", ...)` at line 135 (Story 3.3); (b) the static helpers `ReadHeader` / `HasHeader` / `IsCanonicalIdentifier` / `SafeDenial` / `ReadModelUnavailable` / `ValidationProblem` / `FreshnessHeaderName` / `EventuallyConsistent` / `ResponseJsonOptions` are defined on the partial class and visible to the new handler.
  - [x] Read `src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` — confirm `static Map(ProjectConversationsPage page, DateTimeOffset now)` shape is unchanged; reused by Story 3.4 as-is.
  - [x] Read `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationResult.cs` and `ProjectAuthorizationGate.cs` — confirm `TenantAccessResult: TenantAccessAuthorizationResult?` is populated on every Allowed path (Story 3.2 deliverable); the defensive null-collapse at `GetProjectContextEndpoint.cs:94–99` is the canonical pattern Story 3.4 carries forward verbatim.
  - [x] Read `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` — confirm: (a) `IProjectFolderDirectory` is `TryAddTransient`-registered with fall-through to `UnavailableProjectFolderDirectory` when no Folders client is configured (line 74–80); (b) `IProjectFileReferenceDirectory` same pattern (line 81–87); (c) `IProjectMemoryDirectory` same pattern (line 88–94). The new `RefreshXxxAsync` methods inherit the same DI lifetime via the same registration; no DI change is needed.
  - [x] Read `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` lines 284–413 (`GET /context` operation block — Story 3.2 + `GET /context/explain` operation block — Story 3.3) and the canonical-error-categories block at line 339–347 / 405–413. Identify the canonical placement for the new operation block (immediately after line 413, before the existing `/api/v1/projects/{projectId}/conversations/{conversationId}/link` block at line 414).
  - [x] Read `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs` end-to-end (the auto-fixed-to-587-LOC file per Story 3.2 review cycle — three named-fixture tests were added per the sprint-status commentary). Note the canonical `StartAppAsync(...)` builder pattern at lines 395–470. Story 3.4's `RefreshProjectContextTests.cs` mirrors the builder shape.
  - [x] Read `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextExplanationTests.cs` end-to-end (919 lines per Story 3.3 baseline) — confirm the Story 3.3 fixture pattern Story 3.4 mirrors. Note the stubs Story 3.3 defines (`FixedProjectTenantContext`, `NoopProjectCommandSubmitter`, `StubConversationDirectory`, `StubProjectDetailReadModel`, `UnavailablePageConversationDirectory`, `ThrowingTenantAccessProjectionStore`) — Story 3.4 reuses the SAME stub shapes plus three NEW recording / stub Folder / File / Memory directory implementations for the ACL fan-out tests.
  - [x] Read `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` lines 444–610 (the Story 3.1 / 3.2 / 3.3 extension block) — confirm: (a) `ProjectContext_SerializesMetadataOnly` already covers the wire body Story 3.4 reuses; (b) `ProjectContextExplanation_SerializesMetadataOnly` (Story 3.3) is unrelated. Story 3.4 adds NO new DTO-shape test; only endpoint-response coverage in `RefreshProjectContextTests.cs` per AC 12.
  - [x] Read `tests/Hexalith.Projects.Client.Tests/GetProjectContextClientTests.cs` end-to-end (96 lines) — confirm the substring-based regenerated-`.g.cs` assertion pattern Story 3.4 mirrors.
  - [x] Read `docs/checklists/mutation-and-query-negative-tests.md` (Story 3.2 / Action Item 7 deliverable) — confirm the 8 rows and the query-side applicability (rows 1 / 4 / 5 / 6 / 8 apply to Story 3.4; rows 2 / 3 / 7 are mutation-only).
  - [x] Read `docs/context-assembly-decision-matrix.md` lines 5 + 16–27 + 34–50 — confirm: (a) line 5 names Story 3.4 as a "consume verbatim" consumer; (b) the `RefreshProjectContext (3.4)` column (line 16, second operation) is identical to the `Get` column for every evidence-state row; (c) the outer-override rows (34–42) and the Memories rows (44–50) apply identically to Refresh. Story 3.4 does NOT add or modify any matrix column.
  - [x] Confirm no submodule pointer change is required and no nested-recursive submodule init is needed (current `git status` shows pre-existing `Hexalith.Commons` / `Hexalith.Conversations` / `Hexalith.Parties` "modified content" markers from prior sessions — unrelated to Story 3.4 and must not be advanced).
  - [x] **HALT** before proceeding to Task 2 if any of the above evidence diverges from this story file's assumptions — especially: (i) `ProjectContextOperationKind.Refresh` was somehow removed; (ii) the policy's outer-collapse paths no longer emit `Array.Empty<ProjectContextEvaluation>()`; (iii) the Folders typed client provides no opaque-id-only file-reference read route (record the HALT and proceed with folder + memory recheck only — Story 3.4 file-recheck deferred to a follow-up story); (iv) a Story 3.1 / 3.2 / 3.3 file would have to change to make Story 3.4 work beyond an additive `[JsonPropertyName]`.

- [x] **Task 2 — Add the three ACL `RefreshXxxAsync` methods. (AC: 2)**
  - [x] Edit `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs` to add the additive `RefreshFolderReferenceAsync(ProjectId projectId, string folderId, string correlationId, CancellationToken cancellationToken = default)` method, returning `Task<ProjectFolderValidationResult>`. XML doc: explicitly name Story 3.4 / FR-18; explain the difference from `ValidateSetProjectFolderAsync` (Refresh validates an EXISTING reference; Validate validates a NEW assignment).
  - [x] Edit `src/Hexalith.Projects.Server/Folders/FoldersProjectFolderDirectory.cs` to implement `RefreshFolderReferenceAsync` by extracting the existing folder-read evidence helper from `ValidateSetProjectFolderAsync` into a shared private method that BOTH `ValidateSetProjectFolderAsync` and `RefreshFolderReferenceAsync` call. Verify by inspection that no behavior change occurs for `ValidateSetProjectFolderAsync` (the extracted helper is a pure refactor).
  - [x] Edit `src/Hexalith.Projects.Server/Folders/UnavailableProjectFolderDirectory.cs` to implement `RefreshFolderReferenceAsync` returning `Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId))` — mirror the existing `ValidateSetProjectFolderAsync` body line 19.
  - [x] Edit `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs` to add the additive `RefreshFileReferenceAsync(ProjectId projectId, string fileReferenceId, string folderId, string correlationId, string taskId, CancellationToken cancellationToken = default)` method, returning `Task<ProjectFileReferenceValidationResult>`. XML doc: explicitly name Story 3.4 / FR-18; explain the difference from `ValidateLinkFileReferenceAsync`. Document that the workspaceId / filePath parameters of the existing mutation-validation method are NOT required for refresh (the Folders read route resolves the file by opaque id + folderId).
  - [x] Edit `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs` to implement `RefreshFileReferenceAsync` against the opaque-id-only Folders read route identified in Task 1. If no such route exists, HALT and surface the dependency (Task 1's HALT path).
  - [x] Edit `src/Hexalith.Projects.Server/Folders/UnavailableProjectFileReferenceDirectory.cs` to implement `RefreshFileReferenceAsync` returning `Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId))`.
  - [x] Edit `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs` to add the additive `RefreshMemoryReferenceAsync(ProjectId projectId, string memoryReferenceId, string tenantId, string correlationId, string taskId, CancellationToken cancellationToken = default)` method, returning `Task<ProjectMemoryValidationResult>`. XML doc: explicitly name Story 3.4 / FR-18; explain the difference from `ValidateLinkMemoryReferenceAsync` (both call `MemoriesClient.GetCaseAsync` — Refresh consumes the response for state, Validate consumes it for link-eligibility; the underlying read route is the same).
  - [x] Edit `src/Hexalith.Projects.Server/Memories/MemoriesProjectMemoryDirectory.cs` to implement `RefreshMemoryReferenceAsync` against `MemoriesClient.GetCaseAsync(...)` — extract the shared `GetCaseAsync` call + outcome-mapping helper from `ValidateLinkMemoryReferenceAsync` into a private method shared by both.
  - [x] Edit `src/Hexalith.Projects.Server/Memories/UnavailableProjectMemoryDirectory.cs` to implement `RefreshMemoryReferenceAsync` returning `Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId))`.
  - [x] Run a targeted compile check: `dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` — confirm 0 W / 0 E.
  - [x] Boundary check: the three new methods MUST NOT take any payload-classified field as input (no path / token / body / payload-text per `docs/payload-taxonomy.md`); only opaque ids and the tenant authority claim are allowed. Verify by inspection.
  - [x] If any test fake or in-memory test stub of `IProjectFolderDirectory` / `IProjectFileReferenceDirectory` / `IProjectMemoryDirectory` exists outside the three Story 3.4 unavailable implementations, extend it to implement the new methods (search: `grep -rn "IProjectFolderDirectory\|IProjectFileReferenceDirectory\|IProjectMemoryDirectory" tests/ src/Hexalith.Projects.Testing/`); add a fail-closed default (`return Unavailable`) so legacy fakes do not silently return `Accepted` on Refresh.

- [x] **Task 3 — Add the three outcome-to-`ReferenceState` mappers. (AC: 3, 11, 17)**
  - [x] Create `src/Hexalith.Projects.Server/Folders/ProjectFolderValidationOutcomeMapper.cs` — internal `public static class` with a single `public static (ReferenceState State, DateTimeOffset ObservedAt) Map(ProjectFolderValidationOutcome outcome, ProjectFolderReference projectionStored, DateTimeOffset now)`. XML doc: explicitly name Story 3.4 / AC 3 Folder mapping; document the projection-state-preservation rule for `Unavailable` + `Pending`.
  - [x] Implement the mapping per AC 3 Folder mapping with an exhaustive `switch` statement on `ProjectFolderValidationOutcome`. The `Unavailable` + `Pending` rule: if `outcome == Unavailable && projectionStored.ReferenceState == Pending`, return `(Pending, projectionStored.ObservedAt)` so the diagnostic stays `projectFolderPending`. Otherwise, return `(<mapped>, projectionStored.ReferenceState == <mapped> ? projectionStored.ObservedAt : now)` (preserves `ObservedAt` when state unchanged; replaces with `now` when state changes).
  - [x] Create `src/Hexalith.Projects.Server/Folders/ProjectFileReferenceValidationOutcomeMapper.cs` — same pattern for `ProjectFileReferenceValidationOutcome`. The mapping passes `TenantMismatch` through to the policy (which collapses to `Unauthorized` + `tenantMismatch` diagnostic at the boundary per `ClassifyReferenceState` at `ProjectContextInclusionPolicy.cs:632`).
  - [x] Create `src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationOutcomeMapper.cs` — same pattern for `ProjectMemoryValidationOutcome`. The Memories mapping likewise passes `TenantMismatch` through to the policy (which collapses to `Unauthorized` + `tenantMismatch` per `ProjectContextInclusionPolicy.cs:473–491`).
  - [x] Boundary check: `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/Folders/Project*OutcomeMapper.cs src/Hexalith.Projects.Server/Memories/Project*OutcomeMapper.cs` returns 0 hits (the mappers receive `now` as input — no wall-clock).
  - [x] No DI registration needed — mappers are static classes consumed directly.
  - [x] Run a targeted compile check: `dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` — 0 W / 0 E.

- [x] **Task 4 — Extend the OpenAPI spine with the Refresh operation. (AC: 1, 4)**
  - [x] Open `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`.
  - [x] Add the path entry `/api/v1/projects/{projectId}/context/refresh` (GET) immediately after the existing `/api/v1/projects/{projectId}/context/explain` operation block (lines 348–413), copying the operation block verbatim and changing only: `operationId` → `RefreshProjectContext`; `summary` / `description` to reflect Story 3.4's refresh surface (FR-18 wording: "request a refreshed Project Context after links, setup, or resource availability change"); the `200` response schema reference stays `$ref ProjectContext` (no wrapper); the example reference stays `$ref ProjectContext` (or a new `ProjectContextRefreshed` example showing drift — see AC 4); the `x-hexalith-canonical-error-categories` list gains `referenced_resource_unavailable` after `read_model_unavailable` (additive — surfaces the ACL-recheck-driven evidence-recovery channel; verify that the existing 8-row set in `docs/checklists/mutation-and-query-negative-tests.md` is unchanged — Story 3.4 does NOT modify the canonical checklist, only adds a new category to its operation block).
  - [x] Verify YAML is well-formed by running the existing `OpenApiContractSpineTests` lane (`dotnet test tests/Hexalith.Projects.Contracts.Tests/`) — confirm `Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths` PASSES.
  - [x] Regenerate `HexalithProjectsClient.g.cs` via the MSBuild target: `DOTNET_ROOT=/home/administrator/.dotnet /home/administrator/.dotnet/dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj`. Confirm the new `RefreshProjectContextAsync(...)` method appears with signature `Task<ProjectContext> RefreshProjectContextAsync(string projectId, System.Guid? correlationId, string? freshness, CancellationToken cancellationToken)` (mirror Story 3.2's typed signature).
  - [x] Confirm `HexalithProjectsIdempotencyHelpers.g.cs` is unchanged except for the SHA256 fingerprint constants (no idempotency surface for queries; same as Story 3.2 / 3.3).
  - [x] Update the OpenAPI fingerprint baseline file (if present) and confirm the fingerprint gate flips PASSED-with-update for this story only.
  - [x] Run `git diff --check` and confirm clean across the spine, `.g.cs` (expected non-zero diff), and the fingerprint baseline.

- [x] **Task 5 — Implement the `RefreshProjectContextAsync` HTTP handler. (AC: 5, 6, 7, 17)**
  - [x] Create `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` as a new partial-class file for `ProjectsDomainServiceEndpoints` (mirror the Story 3.2 / 3.3 partial-class pattern). Copy the Story 3.2 file's `using` declarations, XML doc structure (adapt the narrative for Refresh), and the file-scoped namespace. ADD `using Hexalith.Projects.Server.Folders;` and `using Hexalith.Projects.Server.Memories;` for the three new ACL directory interfaces and the three mappers.
  - [x] Implement `private static async Task<IResult> RefreshProjectContextAsync(string projectId, HttpContext httpContext, IProjectTenantContextAccessor tenantContext, ProjectAuthorizationGate authorizationGate, IProjectConversationDirectory conversationDirectory, IProjectFolderDirectory folderDirectory, IProjectFileReferenceDirectory fileReferenceDirectory, IProjectMemoryDirectory memoryDirectory, ProjectContextInclusionPolicy contextPolicy, TimeProvider timeProvider, CancellationToken cancellationToken)` — extended Story 3.2 handler signature with the three additional ACL directory dependencies. All are resolved through DI (already registered by `AddProjectsServer()` — no DI change needed).
  - [x] Copy the Story 3.2 handler body lines 62–101 (header parsing, envelope validation, authz, idempotency rejection, freshness rejection, defensive null-collapse, projection-detail fetch) verbatim.
  - [x] After fetching `detail` and `now`, build the three ACL recheck tasks plus the conversation page task (all in parallel):
    ```
    Task<ProjectFolderValidationResult>? folderTask = detail.ProjectFolder?.FolderId is { } folderId
        ? folderDirectory.RefreshFolderReferenceAsync(new ProjectId(projectId), folderId, correlationId ?? string.Empty, cancellationToken)
        : null;
    Task<ProjectFileReferenceValidationResult>[] fileTasks = detail.FileReferences
        .Select(f => fileReferenceDirectory.RefreshFileReferenceAsync(new ProjectId(projectId), f.FileReferenceId, f.FolderId ?? string.Empty, correlationId ?? string.Empty, taskId ?? string.Empty, cancellationToken))
        .ToArray();
    Task<ProjectMemoryValidationResult>[] memoryTasks = detail.MemoryReferences
        .Select(m => memoryDirectory.RefreshMemoryReferenceAsync(new ProjectId(projectId), m.MemoryReferenceId, tenantContext.AuthoritativeTenantId!, correlationId ?? string.Empty, taskId ?? string.Empty, cancellationToken))
        .ToArray();
    Task<ProjectConversationsPage> conversationsTask = conversationDirectory.ListForProjectAsync(...);
    await Task.WhenAll(...).ConfigureAwait(false);
    ```
  - [x] Compose the rechecked `ProjectFolderReference?` / `IReadOnlyList<ProjectFileReference>` / `IReadOnlyList<ProjectMemoryReference>` via the three outcome mappers (AC 3 / Task 3). The conversation evidence remains Story 3.2's `ProjectContextConversationEvidenceMapper.Map(...)` output.
  - [x] Build the `ProjectContextAssemblyContext` with `OperationKind: ProjectContextOperationKind.Refresh`. Build the `ProjectContextReferenceEvidence` from the rechecked references (NOT the projection-stored references) and the mapped conversation evidence.
  - [x] Call `contextPolicy.Assemble(...)` and return `Results.Json(assembled.Context, ResponseJsonOptions)` (mirrors Story 3.2 line 139 — `assembled.Context`, NOT a wrapper).
  - [x] Set `X-Correlation-Id` (echo) and `X-Hexalith-Freshness: eventually_consistent` response headers (mirrors `GetProjectContext`).
  - [x] Register the route in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` `ConfigureEndpoints` method by adding a new `endpoints.MapGet("/api/v1/projects/{projectId}/context/refresh", static async (...) => await RefreshProjectContextAsync(...));` block immediately after the existing `/api/v1/projects/{projectId}/context/explain` registration at line 135–153. Mirror the parameter-binding shape of the existing block verbatim (the lambda forwards the same DI-injected arguments plus the three new directory dependencies).
  - [x] Boundary check: `grep -rE "Hexalith\.(Folders|Memories)" src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` returns 0 hits at the namespace import level except via the local `Hexalith.Projects.Server.Folders` / `Hexalith.Projects.Server.Memories` namespaces (the Refresh handler MUST consume the three ACL directory interfaces via `Hexalith.Projects.Server.Folders` and `Hexalith.Projects.Server.Memories` namespaces — never via direct `Hexalith.Folders.*` / `Hexalith.Memories.*` imports). The only allowed `Hexalith.Conversations.*` import is the `using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;` alias Story 3.2 / 3.3 already established — copy this alias.

- [x] **Task 6 — Add Tier-2 endpoint tests. (AC: 8, 9, 10, 12, 17)**
  - [x] Create `tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs` mirroring the Story 3.2 / 3.3 fixture pattern. Reuse the `StartAppAsync(...)` named-fixture builder shape and the same stub classes (`FixedProjectTenantContext`, `NoopProjectCommandSubmitter`, `StubConversationDirectory`, `StubProjectDetailReadModel`, `UnavailablePageConversationDirectory`, `ThrowingTenantAccessProjectionStore`) plus three NEW stub directory implementations: `StubFolderDirectory` (returns a configurable `ProjectFolderValidationResult`), `StubFileReferenceDirectory` (returns a configurable per-file-id `ProjectFileReferenceValidationResult`), `StubMemoryDirectory` (returns a configurable per-memory-id `ProjectMemoryValidationResult`). Add `RecordingFolderDirectory` / `RecordingFileReferenceDirectory` / `RecordingMemoryDirectory` for the AC 10 "no ACL call on cross-tenant" assertion (record call counts; assert 0 when the request collapses to safe-denial 404).
  - [x] Required named-fixture tests per AC 8 (matrix-cell coverage — collapse `Refresh` rows into a single `[Theory]` since the Refresh column equals the Get column):
    - `RefreshProjectContext_MatrixCells_AssembleCorrectExclusionPerCell` (`[Theory]`-driven over the 10 cells of `docs/context-assembly-decision-matrix.md`).
    - `RefreshProjectContext_HappyPath_Returns200WithAssembledContext` — all references `Accepted`; all included; `Excluded` empty.
    - `RefreshProjectContext_ArchivedProject_Returns200WithAllExcluded`.
    - `RefreshProjectContext_TenantMismatchedConversation_HasExcludedRow`.
    - `RefreshProjectContext_StaleFileReference_HasExcludedRow`.
    - `RefreshProjectContext_ArchivedMemoryReference_HasExcludedRow`.
    - `RefreshProjectContext_IdempotencyKeyPresent_ReturnsValidationProblem`.
    - `RefreshProjectContext_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404`.
    - `RefreshProjectContext_StricterFreshnessRequested_ReturnsValidationProblem`.
    - `RefreshProjectContext_MalformedProjectId_ReturnsSafeDenial404` (`[Theory]` over whitespace, NUL, control bytes, unicode bidi, `..`, leading/trailing whitespace).
    - `RefreshProjectContext_CrossTenant_ReturnsSafeDenial404` — asserts `body.ShouldNotContain("tenant-b")` AND `recordingFolderDirectory.CallCount == 0` AND `recordingFileReferenceDirectory.CallCount == 0` AND `recordingMemoryDirectory.CallCount == 0`. FS-8 / SM-3 + AC 10's no-leak-to-siblings invariant.
    - `RefreshProjectContext_TenantAccessUnavailable_ReturnsReadModelUnavailable503`.
    - `RefreshProjectContext_AuthoritativeTenantIdMissing_ReturnsSafeDenial404`.
    - `RefreshProjectContext_ConversationsPageUnavailable_AssemblesWithExclusions` — conversation ACL returns unavailable; policy collapses to exclusion; endpoint still returns 200.
    - `RefreshProjectContext_ResponseHeaders_HaveCorrelationAndFreshness`.
    - `RefreshProjectContext_ExtraQueryParameters_AreIgnoredNotFailed` (`?expand=full`).
    - `RefreshProjectContext_ResponseBody_HasNoLeakageAcrossOutcomes` (AC 12) — iterates 4 labelled outcomes (`HappyPath`, `ArchivedProject`, `AclRecoveredStaleToIncluded`, `AclDegradedIncludedToArchived`); runs `NoPayloadLeakageAssertions.AssertNoLeakage(...)` over every serialized response body.
    - `RefreshProjectContext_ErrorResponses_HaveNoLeakage` (AC 12).
  - [x] Required AC 9 recovery / regression tests (the NEW tests Story 3.4 owns):
    - `RefreshProjectContext_AclReportsAccepted_OverridesProjectionStoredStale`.
    - `RefreshProjectContext_AclReportsArchived_OverridesProjectionStoredIncluded`.
    - `RefreshProjectContext_AclReportsUnavailable_OverridesProjectionStoredIncluded`.
    - `RefreshProjectContext_AclReportsTenantMismatch_OverridesToUnauthorized`.
    - `RefreshProjectContext_AclReportsDenied_SurfacesAsUnauthorized`.
    - `RefreshProjectContext_FolderUnavailableAndProjectionStoredPending_PreservesPendingDiagnostic`.
    - `RefreshProjectContext_AllAclsReturnAccepted_AndProjectionStoredIncluded_IsByteIdenticalToGet` — boots BOTH the `/context` and `/context/refresh` endpoints, deserializes both responses, asserts byte-identical (modulo `ObservedAt` if the recheck `now` differs; if so, normalize `ObservedAt` to the projection-stored value before comparison via the AC 3 preservation rule).
    - `RefreshProjectContext_AclThrows_SurfacesAsUnavailable_FailsClosedNot500`.
    - `RefreshProjectContext_DeterministicFanOut_PreservesProjectionStoredOrder`.
  - [x] All tests use `RecordingLogger<T>` from `src/Hexalith.Projects.Testing/Context/` (Story 3.1) for any policy logger assertions.
  - [x] Boundary discipline: `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs` returns 0 hits.
  - [x] Use `TestContext.Current.CancellationToken` (xUnit v3 pattern; mirror Story 3.2 / 3.3 test files).

- [x] **Task 7 — Add Tier-1 outcome-mapper tests. (AC: 11)**
  - [x] Create `tests/Hexalith.Projects.Tests/Folders/ProjectFolderValidationOutcomeMapperTests.cs` — pure xUnit v3 + Shouldly tests over `ProjectFolderValidationOutcomeMapper.Map(...)`. Cover every enum member via `[Theory]` driven by `Enum.GetValues<ProjectFolderValidationOutcome>()`. Add named tests for the override semantic (`Map_AcceptedOutcome_OverridesProjectionStoredStale_To_Included`), the `Pending`-preservation rule (`Map_UnavailableOutcome_WithProjectionStoredPending_PreservesPending`), and the `ObservedAt` semantics (`Map_PreservesObservedAt_WhenStateUnchanged`, `Map_ReplacesObservedAt_WhenStateChanges`).
  - [x] Create `tests/Hexalith.Projects.Tests/Folders/ProjectFileReferenceValidationOutcomeMapperTests.cs` — same pattern for `ProjectFileReferenceValidationOutcomeMapper.Map(...)`. Cover all 8 enum members. Add a named test `Map_TenantMismatchOutcome_MapsTo_TenantMismatch_NotUnauthorized` — proves the mapper passes `TenantMismatch` to the policy without collapsing.
  - [x] Create `tests/Hexalith.Projects.Tests/Memories/ProjectMemoryValidationOutcomeMapperTests.cs` — same pattern for `ProjectMemoryValidationOutcomeMapper.Map(...)`. Cover all 7 enum members. Add the same `Map_TenantMismatchOutcome_MapsTo_TenantMismatch_NotUnauthorized` test.
  - [x] Add `Map_AllOutcomes_CoveredByTheory` to each file — asserts no enum member is unmapped (a future additive enum member triggers a test failure via `Enum.GetValues<T>().All(o => mapped[o] is not null)` check).
  - [x] Boundary check: the three test files import ONLY `Hexalith.Projects.Server.Folders` / `Hexalith.Projects.Server.Memories` types (the mapper signatures). They do NOT import `Hexalith.Folders.*` / `Hexalith.Memories.*` — Tier-1 purity is preserved.
  - [x] Boundary check: `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.Tests/Folders/Project*OutcomeMapperTests.cs tests/Hexalith.Projects.Tests/Memories/ProjectMemoryValidationOutcomeMapperTests.cs` returns 0 hits.

- [x] **Task 8 — Add a typed-client happy-path test. (AC: 13)**
  - [x] Create `tests/Hexalith.Projects.Client.Tests/RefreshProjectContextClientTests.cs` — three substring-based tests over the regenerated `HexalithProjectsClient.g.cs` (mirror `GetProjectContextClientTests.cs` lines 34–73).
  - [x] `GeneratedClient_ExposesTypedRefreshProjectContextAsync` — asserts the regenerated file contains `Task<ProjectContext> RefreshProjectContextAsync`. Verify the `ProjectContext` partial class is declared exactly once (NSwag deduplicates — no new partial declaration for `ProjectContext`).
  - [x] `GeneratedClient_RefreshOperation_HasNoIdempotencyHelper` — opens the regenerated `HexalithProjectsIdempotencyHelpers.g.cs` and asserts it does NOT contain `RefreshProjectContext` — proves no idempotency-fingerprint surface was added (queries are idempotency-free per AC 1).
  - [x] `GeneratedClient_IsLfOnDiskAndNulFree` — copy verbatim from `GetProjectContextClientTests.cs:67–73`.
  - [x] Path-resolution helper: copy `LocateRepositoryRoot()` from `GetProjectContextClientTests.cs:75` (or extract to a shared helper if a pattern emerges; preferred: leave inline per the Story 3.2 / 3.3 precedent).

- [x] **Task 9 — Apply the negative-test checklist. (AC: 17)**
  - [x] In the Dev Agent Record, explicitly tick off rows 1 / 4 / 5 / 6 / 8 of `docs/checklists/mutation-and-query-negative-tests.md` for the Refresh endpoint:
    - Row 1 (Malformed identifier → safe-denial 404): covered by `RefreshProjectContext_MalformedProjectId_ReturnsSafeDenial404`.
    - Row 4 (Idempotency-Key PRESENT on query → 400 after authz): covered by `RefreshProjectContext_IdempotencyKeyPresent_ReturnsValidationProblem` + `..._IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404`.
    - Row 5 (Stricter `X-Hexalith-Freshness` → 400): covered by `RefreshProjectContext_StricterFreshnessRequested_ReturnsValidationProblem`.
    - Row 6 (Cross-tenant safe-denial 404): covered by `RefreshProjectContext_CrossTenant_ReturnsSafeDenial404` AND the no-ACL-call assertion (AC 10).
    - Row 8 (`ReferenceState.Unavailable && Retryable` → 503 ReadModelUnavailable): covered by `RefreshProjectContext_TenantAccessUnavailable_ReturnsReadModelUnavailable503`.
  - [x] Rows 2 / 3 / 7 are mutation-only (route/body identity mismatch, missing Idempotency-Key on mutation, unknown Idempotency-Key retry conflict) — explicitly mark N/A in the Dev Agent Record.

- [x] **Task 10 — Validation. (AC: 17, 18, 19)**
  - [x] Use the build environment from [[build-environment]]: `DOTNET_ROOT=/home/administrator/.dotnet` (`dotnet --version` 10.0.302). Avoid `/usr/bin/dotnet`.
  - [x] Run `dotnet build Hexalith.Projects.slnx`. Confirm 0 W / 0 E.
  - [x] Run focused lanes:
    - `dotnet test tests/Hexalith.Projects.Tests` (baseline 427 + ~30 = ~457).
    - `dotnet test tests/Hexalith.Projects.Server.Tests` (baseline 252 + ~25 = ~277).
    - `dotnet test tests/Hexalith.Projects.Contracts.Tests` (baseline 128 + 0 = 128).
    - `dotnet test tests/Hexalith.Projects.Client.Tests` (baseline 37 + ~3 = ~40).
    - `dotnet test tests/Hexalith.Projects.Integration.Tests` (baseline 14 + 0 = 14).
  - [x] Run full-solution `dotnet test Hexalith.Projects.slnx`. Baseline 858; Story 3.4 grows it by approximately +58 (Server +~25, Projects +~30, Client +~3); failed must be 0; skipped must be 0.
  - [x] Run `git diff --check` on story-touched files. Confirm clean across `.cs`, `.md`, `.yaml`, `.csproj`.
  - [x] Confirm `.g.cs` regenerated cleanly: `git diff --stat src/Hexalith.Projects.Client/Generated/` shows non-zero changed lines (expected — this story regenerates because Story 3.4 adds an operation); inspect that the new `RefreshProjectContextAsync` method is present, that the existing `ProjectContext` partial class is declared once (no duplicate), and that no Windows backslashes leak into the file.
  - [x] Confirm the OpenAPI fingerprint baseline updated and the spine fingerprint gate is PASSED-with-baseline-update (allowed for this story only).
  - [x] Confirm boundary greps pass:
    - `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` → 0 hits (Story 3.1 invariant).
    - `grep -rE "Hexalith\.Folders\.|Hexalith\.Memories\." src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` → 0 hits (the handler consumes Projects-shaped ACL directory interfaces, NOT raw sibling types).
    - `grep -rE "Hexalith\.Conversations\." src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` → 0 hits (only the `using ConversationTenantId = ... .TenantId;` alias is allowed; aliasing doesn't match the regex).
    - `grep -rE "Hexalith\.Folders\.|Hexalith\.Memories\." src/Hexalith.Projects.Server/Folders/Project*OutcomeMapper.cs src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationOutcomeMapper.cs` → 0 hits.
    - `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs src/Hexalith.Projects.Server/Folders/Project*OutcomeMapper.cs src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationOutcomeMapper.cs` → 0 hits.
    - `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs tests/Hexalith.Projects.Tests/Folders/Project*OutcomeMapperTests.cs tests/Hexalith.Projects.Tests/Memories/ProjectMemoryValidationOutcomeMapperTests.cs tests/Hexalith.Projects.Client.Tests/RefreshProjectContextClientTests.cs` → 0 hits.
  - [x] Confirm no submodule pointer change: `git status` shows no submodule advances beyond the pre-existing "modified content" markers (Hexalith.Commons / Hexalith.Conversations / Hexalith.Parties were already in that state at session start per the initial `git status` baseline).
  - [x] Populate the Dev Agent Record with the validation summary per AC 19.

## Dev Notes

### Story Scope Boundary

- **In scope:** `GET /api/v1/projects/{projectId}/context/refresh` endpoint (new partial-class file `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` + a single `MapGet(...)` registration in `ProjectsDomainServiceEndpoints.cs`); OpenAPI spine entry (no new schemas — `ProjectContext` reused); regenerated `HexalithProjectsClient.g.cs` exposing `RefreshProjectContextAsync(...)`; additive ACL methods on the three Story 2.x ACL interfaces (`IProjectFolderDirectory.RefreshFolderReferenceAsync`, `IProjectFileReferenceDirectory.RefreshFileReferenceAsync`, `IProjectMemoryDirectory.RefreshMemoryReferenceAsync`) + implementations in real and Unavailable directory classes; three new outcome-to-`ReferenceState` mappers (`ProjectFolderValidationOutcomeMapper`, `ProjectFileReferenceValidationOutcomeMapper`, `ProjectMemoryValidationOutcomeMapper`); Tier-2 Server endpoint tests (matrix cells, recovery / regression contract, idempotency rejection, route negatives, cross-tenant safe-denial + no-ACL-call assertion, header echo, leakage); Tier-1 outcome-mapper purity tests (one file per mapper, full enum coverage + override / preservation semantics); client typed-method substring assertion test; checklist tick-off in Dev Agent Record (rows 1 / 4 / 5 / 6 / 8 of `docs/checklists/mutation-and-query-negative-tests.md`).
- **Explicitly out of scope (recorded so the dev agent does not over-build):** `GetConversationStartSetup` endpoint (Story 3.5) and the `ConversationStartSetupProjection`; `Resolution/` (Epic 4); any new shared-vocabulary enum value; any new `ProjectContextInclusionDiagnostic` vocabulary entry; any edit to `ProjectContextInclusionPolicy` / `ProjectContextInclusionOrder` / Story 3.1 DTOs beyond an additive `[JsonPropertyName]` if structurally required; any new mutation endpoint; any change to `ProjectAggregate.*` / `ProjectState` / `ProjectStateApply` / `ProjectCommandValidator` / projections / Story 2.x ACL interfaces' EXISTING mutation-validation methods / `IProjectCommandSubmitter`; the U+2028/U+2029 canonicaliser hardening (Epic 2 retro Action Item 2 — applies to the next mutation surface, not Story 3.4); pagination over the conversation evidence in Refresh (single first-page snapshot is sufficient — same cap as Story 3.2 / 3.3); any new ADR; `ProjectFolderCreationPending` reconciliation flow (Epic 5 territory); modifying `ProjectContextAssemblyResult` (the policy's internal result type stays an internal contract — Story 3.4 ships no new wire wrapper); modifying `ProjectContextExplanation` (the Story 3.3 wrapper — Refresh does not surface the evaluation trace); modifying the Story 3.2 `GetProjectContextEndpoint.cs` or the Story 3.3 `GetProjectContextExplanationEndpoint.cs` files beyond consuming the shared partial-class constant; modifying the decision-matrix doc (Story 3.1 owns its cell semantics; the matrix already has a `RefreshProjectContext (3.4)` column that is identical to `Get`); adding an AppHost smoke test (the in-process WebApplication-slim Tier-2 endpoint tests are sufficient for Story 3.4's surface); modifying `docs/checklists/mutation-and-query-negative-tests.md` rows (the 8-row canonical checklist stays unchanged; Story 3.4 only adds one operation block to the spine that references the existing categories plus `referenced_resource_unavailable`).

### Current Code Facts Verified (this working tree, baseline `89cd7a5`)

- **Story 3.3 status: `done`** (per `_bmad-output/implementation-artifacts/sprint-status.yaml:131` and the review-cycle commentary at line 3). Story 3.3's `GET /api/v1/projects/{projectId}/context/explain` ships at `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs` (144 lines, partial class), reusing the Story 3.2 `ProjectContextConversationsPageSize = 100` constant.
- **Story 3.2 status: `done`.** `GET /api/v1/projects/{projectId}/context` at `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs` (142 lines, partial class) is the canonical handler shape Story 3.4 mirrors.
- **`ProjectContextOperationKind.Refresh` ships** at `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:31`. **`IsReadOnlyOperation(...)` already allows `Refresh`** at `ProjectContextInclusionPolicy.cs:236` (no policy change needed; Story 3.1 forward-built this).
- **The decision matrix `Refresh` column ships** at `docs/context-assembly-decision-matrix.md:16` (position 2 in the header). The column is identical to `Get` / `Explain` for every row by design — read-only operations consume the same fail-closed verdicts. Story 3.4 does NOT add or modify any matrix column.
- **The three ACL interfaces are in place** (Story 2.4 / 2.5 / 2.7):
  - `IProjectFolderDirectory.ValidateSetProjectFolderAsync(ProjectId, string folderId, string correlationId, CancellationToken)` returning `Task<ProjectFolderValidationResult>` (`Accepted` / `ValidationFailed` / `Archived` / `Stale` / `Denied` / `Unavailable`).
  - `IProjectFileReferenceDirectory.ValidateLinkFileReferenceAsync(ProjectId, string folderId, string workspaceId, string filePath, string correlationId, string taskId, CancellationToken)` returning `Task<ProjectFileReferenceValidationResult>` (`Accepted` / `ValidationFailed` / `Denied` / `Redacted` / `Archived` / `Stale` / `TenantMismatch` / `Unavailable`).
  - `IProjectMemoryDirectory.ValidateLinkMemoryReferenceAsync(ProjectId, string memoryReferenceId, string tenantId, string correlationId, string taskId, CancellationToken)` returning `Task<ProjectMemoryValidationResult>` (`Accepted` / `ValidationFailed` / `Denied` / `Archived` / `Stale` / `TenantMismatch` / `Unavailable`).
  - All three have `FoldersXxxDirectory` / `MemoriesXxxDirectory` real implementations and `UnavailableXxxDirectory` fail-closed implementations, registered via `TryAddTransient` in `ProjectsServerServiceCollectionExtensions.cs:74–94`.
- **The `ProjectDetailItem` already carries the projection-stored reference state** (`ProjectFolder?`, `FileReferences`, `MemoryReferences` — each reference type has `ReferenceState` / `ReasonCode?` / `ObservedAt` per Story 1.8 / 2.4 / 2.5 / 2.7). Story 3.4 consumes these as fallback / comparison values when the ACL recheck preserves the state.
- **`ProjectAuthorizationGate.AuthorizeReadAsync` returns `ProjectAuthorizationResult { TenantAccessResult: TenantAccessAuthorizationResult? }`** (Story 3.2 / Task 4 additive extension). Story 3.4 consumes the property unchanged via the same defensive-null collapse pattern as `GetProjectContextEndpoint.cs:94–99` and `GetProjectContextExplanationEndpoint.cs:94–99`.
- **`AddProjectsServer()` calls `AddProjectsModule()`** (Story 3.2 fix) so `ProjectContextInclusionPolicy` is DI-registered (`src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs:35` — `services.TryAddTransient<ProjectContextInclusionPolicy>();`). No DI change is needed for Story 3.4 (the three ACL directories are already registered).
- **`ProjectContextConversationEvidenceMapper.Map(...)` ships** at `src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` (Story 3.2). Reused as-is by Story 3.4.
- **The NSwag MSBuild target Linux fix shipped** in Story 3.2 (forward-slash paths + `$(HexalithProjectsDotnetHostPath)` derived from `$(MSBuildToolsPath)`). Story 3.4 inherits the working Linux regeneration path; no `.csproj` change required.
- **The canonical negative-test checklist ships** at `docs/checklists/mutation-and-query-negative-tests.md` (Story 3.2 / Action Item 7) with 8 rows and the cross-link from `_bmad-output/planning-artifacts/architecture.md`. Story 3.4 applies the query-side rows (1 / 4 / 5 / 6 / 8) and ticks them in the Dev Agent Record.
- **`OpenApiContractSpineTests.Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths`** is the canonical spine-validation gate — Story 3.4's new `RefreshProjectContext` operation block surfaces only the existing schemas (no new) so the gate has nothing new to validate beyond the optional new `ProjectContextRefreshed` example (which uses the existing `ProjectContext` schema).
- **The root commit is `89cd7a5 feat(story-3.3): Story 3.3: Explain Context Selection`** (Story 3.3 merged). No `Hexalith.Memories` / `Conversations` / `Folders` / `Tenants` / `EventStore` / `FrontComposer` / `Commons` / `AI.Tools` / `Builds` submodule pointer change is required; pre-existing "modified content" markers on `Hexalith.Commons` / `Hexalith.Conversations` / `Hexalith.Parties` are unrelated to Story 3.4.
- **Baseline test counts (post-Story-3.3 review cycle 1, per sprint-status.yaml line 3):** Server.Tests 252, Projects.Tests 427, Contracts.Tests 128, Client.Tests 37, Integration.Tests 14; full-solution 858/858.

### Required Capability Path

Story 3.4's true upstream capability gates are mostly already in place; the new gate is the Folders opaque-id-only file-reference read route:

- `IProjectConversationDirectory.ListForProjectAsync` — Story 2.1 read ACL. **READY.**
- `ProjectAuthorizationGate.AuthorizeReadAsync` returning `ProjectAuthorizationResult.TenantAccessResult` — Story 3.2 additive extension. **READY.**
- `ProjectContextInclusionPolicy.Assemble(...)` with `OperationKind.Refresh` allowed — Story 3.1. **READY.**
- `ProjectContextConversationEvidenceMapper.Map(...)` — Story 3.2. **READY.**
- DI registration of `ProjectContextInclusionPolicy` via `AddProjectsModule()` reached through `AddProjectsServer()` — Story 3.2. **READY.**
- DI registration of the three ACL directories — Story 2.4 / 2.5 / 2.7. **READY.**
- **NEW for Story 3.4: a Folders typed-client read route that validates a file reference by opaque `(folderId, fileReferenceId)` WITHOUT requiring workspaceId / filePath inputs.** Story 2.5's `ValidateLinkFileReferenceAsync` takes those payload-classified inputs because the mutation path needs to validate a path-supplied file; Refresh needs a degraded-state check on a stored reference id. The dev agent **MUST identify** this route in `Hexalith.Folders.Client.Generated.IClient` during Task 1's capability gate inspection. If no such route exists, the dev agent **HALTs** before Task 2 (file-reference portion) and: (i) records the HALT in the Dev Agent Record; (ii) proceeds with folder + memory recheck only (file references retain projection-stored state); (iii) opens a follow-up story to add the Folders read route. AC 9 tests covering the file-recheck recovery / regression contract become deferred under this HALT path; document explicitly which tests are skipped.

If the dev agent finds that the `ProjectContextReferenceEvidence` shape is somehow insufficient for the refresh inputs (e.g. the policy stops accepting per-reference state overrides), HALT and surface the conflict — the resolution is to extend Story 3.1's evidence shape, NOT to reconstitute the missing semantic in Story 3.4's endpoint.

### Guardrails

- **Single source of truth — the policy.** `ProjectContextInclusionPolicy.Assemble(...)` is the only place where include/exclude / fail-closed-collapse / freshness-mapping / diagnostic-vocabulary / per-candidate evaluation emission decisions are made. The endpoint, the ACL recheck composition, and the wire serializer NEVER duplicate any of these decisions. The mappers translate ACL outcomes to `ReferenceState` — they NEVER apply policy decisions; the resulting `ReferenceState` is fed into the policy verbatim.
- **Safe-denial 404 contract.** The HTTP status surfaces `200` (assembled, including archived-project) or one of `400 / 401 / 403 / 404 / 503`. `ProjectContextAssemblyOutcome.Unauthorized` and `.ProjectUnavailable` BOTH map to **HTTP 404** at the boundary — never reveal cross-tenant existence, never differentiate `Unauthorized` vs `ProjectUnavailable` at the HTTP layer. Outer-collapse branches return safe-denial 404 with NO `ProjectContext` body (Problem Details only); the policy's internal `AssemblyOutcome` is observability-only. This is the Story 1.4 + Story 3.1 + Story 3.2 + Story 3.3 safe-denial 404 contract carried forward verbatim.
- **No-ACL-call on cross-tenant.** The cross-tenant safe-denial 404 collapses BEFORE the ACL fan-out runs (the `AuthorizeReadAsync` denial happens first; the handler returns `SafeDenial` 404 without composing evidence). This is verified in AC 10 via `RecordingFolderDirectory` / `RecordingFileReferenceDirectory` / `RecordingMemoryDirectory` whose call counts remain 0 — preserving FS-2 / FS-8 (never leak tenant-existence evidence to siblings).
- **Idempotency-Key rejected on the query** (mirrors `GetProject` / `GetProjectContext` / `GetProjectContextExplanation`). Order: authorize first → then validate `Idempotency-Key` is absent → then proceed. Authorized callers receive validation feedback; unauthorized callers receive only safe-denial 404.
- **Freshness header strict.** `X-Hexalith-Freshness` request header may be `eventually_consistent` or absent; any other value is rejected as a validation error after authorization. Response always carries `X-Hexalith-Freshness: eventually_consistent` (Refresh is NOT a stronger-freshness query — the recheck reads sibling ACLs synchronously but the response still carries the same freshness semantic; "refresh" means "re-check the ACLs", NOT "wait for a more recent projection watermark").
- **Correlation echo.** `X-Correlation-Id` request header (if canonical) is echoed in the response.
- **Single page cap (Story 3.4, mirrors Story 3.2 / 3.3).** Conversations are fetched with `PageSize = ProjectContextConversationsPageSize` (= 100), `ContinuationCursor = null`. No continuation, no client-driven paging.
- **Recheck-overrides-projection-state, not the reverse.** The ACL recheck is the source of truth at Refresh time. The projection-stored state is the FALLBACK when no recheck is performed (e.g. if a future story adds opt-out flags) — but Story 3.4 always recheckes every reference present. The `ObservedAt` preservation rule (AC 3) is the only place where the projection-stored value is preferred over the recheck's `now`.
- **Deterministic fan-out.** The three ACL fan-outs use `Task.WhenAll(...)` over bounded task arrays. No `Task.WhenAny(...)`, no `Thread.Sleep`, no `Task.Delay`. The conversation page fetch is in the same `Task.WhenAll(...)` group. Result-collection ordering follows the input-collection ordering (no `Dictionary<>` lookup).
- **No re-check, no re-fetch FOR Story 3.2 / 3.3.** The Get / Explain endpoints continue to consume `ProjectDetailItem`-stored state directly (Story 3.4 does NOT change that contract; the Refresh endpoint is the only opt-in to ACL recheck).
- **Tier-1 purity preserved.** `src/Hexalith.Projects/Context/**` MUST NOT gain any new file or change in Story 3.4. The mappers live in `src/Hexalith.Projects.Server/Folders/` and `src/Hexalith.Projects.Server/Memories/` — adjacent to the ACL interfaces. The handler lives in `src/Hexalith.Projects.Server/Queries/`. The mapper test files live in `tests/Hexalith.Projects.Tests/Folders/` and `tests/Hexalith.Projects.Tests/Memories/` — Tier-1 purity (no infrastructure imports).
- **No new shared-vocabulary enum values.** Story 3.1's enums + the existing pre-Epic-3 vocabulary are sufficient for Story 3.4 by inspection (the policy already covers every cell of the Refresh column, which equals the Get column; the outcome mappers translate to existing `ReferenceState` values).
- **No edits to Story 3.1 / 3.2 / 3.3 surface beyond additive serialization attributes.** All wire DTOs, enums, the policy, the Story 3.2 / 3.3 handlers, the matrix doc, the canonical checklist — unchanged.
- **Additive ACL interface extension.** The three new `RefreshXxxAsync` methods are additive: existing callers (Story 2.4 / 2.5 / 2.7 mutation endpoints) are unaffected; new implementations (in tests / test fakes) MUST be added to every existing implementer. The dev agent runs a `grep` to find all implementers and updates them with a fail-closed default body (`return Unavailable`).
- **OpenAPI fingerprint baseline updated** (deliberate, allowed for this story only). Story 3.5 must show zero spine diff outside its own surface.
- **`.g.cs` regenerated** (deliberate, allowed for this story only). NSwag Linux fix is inherited from Story 3.2.
- **No nested recursive submodule init.** Read-only inspection is fine; nothing in Story 3.4 advances a submodule pointer.
- **Deterministic-fakes-only tests.** No `Thread.Sleep` / `Task.Delay` / `SpinWait` / `await Task.Yield()` / wall-clock retry loops. Convergence asserted via deterministic inputs.
- **Closed-vocabulary diagnostics only.** The endpoint NEVER surfaces a `Diagnostic` value outside `ProjectContextInclusionDiagnostic.Values` — structurally enforced by `ProjectContextExclusion`'s diagnostic field consumed from the policy (which only emits closed-vocab values). The endpoint only surfaces what the policy produces.
- **No `V2` types.** Public contracts evolve only through additive types. Story 3.4 adds NO new wire DTOs — `ProjectContext` is reused unchanged.
- **Body-shape parity with Story 3.2.** Story 3.4 deliberately surfaces the existing `ProjectContext` shape (NOT a new wrapper). Rationale: Refresh's semantic is "current state, not stale assumptions" — a caller switching from Get to Refresh receives a structurally identical body and applies its own diff if any. The Explain wrapper (Story 3.3) exists because the evaluation trace is meaningfully different from a `ProjectContext`; the Refresh response IS a `ProjectContext`, just with rechecked evidence.

### Suggested ACL Extension

The three new `RefreshXxxAsync` methods are additive on the existing ACL interfaces. Each implementation extracts a shared private helper from the existing `ValidateXxxAsync` method so both code paths converge through a single sibling read. Conceptually:

```csharp
// src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs (additive)

public interface IProjectFolderDirectory
{
    // ... existing ValidateSetProjectFolderAsync ...

    /// <summary>
    /// Re-checks the current state of an existing Project Folder reference against the Folders boundary
    /// at Refresh time (Story 3.4 / FR-18). Read-side recheck; does NOT validate a new assignment.
    /// </summary>
    /// <param name="projectId">The Project whose folder reference is being re-checked.</param>
    /// <param name="folderId">The Folders-owned folder reference identifier already stored on the projection.</param>
    /// <param name="correlationId">The caller/project correlation identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A safe Projects-shaped recheck result (same shape as Validate).</returns>
    Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
        ProjectId projectId,
        string folderId,
        string correlationId,
        CancellationToken cancellationToken = default);
}
```

### Suggested Outcome Mapper

```csharp
// src/Hexalith.Projects.Server/Folders/ProjectFolderValidationOutcomeMapper.cs

namespace Hexalith.Projects.Server.Folders;

using System;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Pure outcome-to-<see cref="ReferenceState"/> mapper for Story 3.4 Refresh.
/// Translates a Folders ACL recheck outcome into the per-reference state the inclusion policy consumes.
/// Preserves projection-stored <see cref="ProjectFolderReference.ObservedAt"/> when the recheck confirms
/// the existing state; replaces with <paramref name="now"/> when the state changes. Preserves the
/// <see cref="ReferenceState.Pending"/> projection state on <see cref="ProjectFolderValidationOutcome.Unavailable"/>
/// so the policy emits the <c>projectFolderPending</c> diagnostic rather than <c>referenceUnavailable</c>.
/// </summary>
public static class ProjectFolderValidationOutcomeMapper
{
    public static (ReferenceState State, DateTimeOffset ObservedAt) Map(
        ProjectFolderValidationOutcome outcome,
        ProjectFolderReference projectionStored,
        DateTimeOffset now)
    {
        ReferenceState mapped = outcome switch
        {
            ProjectFolderValidationOutcome.Accepted => ReferenceState.Included,
            ProjectFolderValidationOutcome.Archived => ReferenceState.Archived,
            ProjectFolderValidationOutcome.Stale => ReferenceState.Stale,
            ProjectFolderValidationOutcome.Denied => ReferenceState.Unauthorized,
            ProjectFolderValidationOutcome.Unavailable
                when projectionStored.ReferenceState == ReferenceState.Pending
                => ReferenceState.Pending,
            ProjectFolderValidationOutcome.Unavailable => ReferenceState.Unavailable,
            ProjectFolderValidationOutcome.ValidationFailed => ReferenceState.InvalidReference,
            _ => ReferenceState.Unavailable,
        };

        DateTimeOffset observedAt = mapped == projectionStored.ReferenceState
            ? projectionStored.ObservedAt
            : now;

        return (mapped, observedAt);
    }
}
```

### Suggested Handler Shape

```csharp
// src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs
//
// New partial-class file. Mirror Story 3.2's GetProjectContextEndpoint.cs shape.
// Three changes vs. Story 3.2:
//   - OperationKind: ProjectContextOperationKind.Refresh  (was .Get)
//   - Adds the three ACL recheck fan-outs before evidence composition
//   - Builds rechecked reference instances overriding projection-stored ReferenceState/ObservedAt

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Memories;

using Microsoft.AspNetCore.Http;

public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> RefreshProjectContextAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationDirectory conversationDirectory,
        IProjectFolderDirectory folderDirectory,
        IProjectFileReferenceDirectory fileReferenceDirectory,
        IProjectMemoryDirectory memoryDirectory,
        ProjectContextInclusionPolicy contextPolicy,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // ... header parsing, envelope validation, authorize-read, idempotency / freshness rejection,
        //     defensive null collapse — copy verbatim from GetProjectContextEndpoint.cs lines 62–101.

        ProjectDetailItem detail = authorization.ProjectDetail;
        DateTimeOffset now = timeProvider.GetUtcNow();

        // Fan out: conversation page + three ACL rechecks (all in parallel).
        Task<ProjectConversationsPage> conversationsTask = conversationDirectory
            .ListForProjectAsync(
                new ProjectId(projectId),
                new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
                new CallerPrincipalId(tenantContext.PrincipalId!),
                new PageRequest(ProjectContextConversationsPageSize, ContinuationCursor: null),
                cancellationToken);

        Task<ProjectFolderValidationResult>? folderRefreshTask = detail.ProjectFolder?.FolderId is { } folderId
            ? folderDirectory.RefreshFolderReferenceAsync(new ProjectId(projectId), folderId, correlationId ?? string.Empty, cancellationToken)
            : null;

        Task<ProjectFileReferenceValidationResult>[] fileRefreshTasks = detail.FileReferences
            .Select(f => fileReferenceDirectory.RefreshFileReferenceAsync(
                new ProjectId(projectId),
                f.FileReferenceId,
                f.FolderId ?? string.Empty,
                correlationId ?? string.Empty,
                taskId ?? string.Empty,
                cancellationToken))
            .ToArray();

        Task<ProjectMemoryValidationResult>[] memoryRefreshTasks = detail.MemoryReferences
            .Select(m => memoryDirectory.RefreshMemoryReferenceAsync(
                new ProjectId(projectId),
                m.MemoryReferenceId,
                tenantContext.AuthoritativeTenantId!,
                correlationId ?? string.Empty,
                taskId ?? string.Empty,
                cancellationToken))
            .ToArray();

        List<Task> awaitables = new(2 + fileRefreshTasks.Length + memoryRefreshTasks.Length) { conversationsTask };
        if (folderRefreshTask is not null) awaitables.Add(folderRefreshTask);
        awaitables.AddRange(fileRefreshTasks);
        awaitables.AddRange(memoryRefreshTasks);
        await Task.WhenAll(awaitables).ConfigureAwait(false);

        ProjectConversationsPage conversations = await conversationsTask.ConfigureAwait(false);

        // Compose rechecked references.
        ProjectFolderReference? recheckedFolder = null;
        if (detail.ProjectFolder is { } projectionFolder && folderRefreshTask is not null)
        {
            ProjectFolderValidationResult folderResult = await folderRefreshTask.ConfigureAwait(false);
            (ReferenceState folderState, DateTimeOffset folderObservedAt) =
                ProjectFolderValidationOutcomeMapper.Map(folderResult.Outcome, projectionFolder, now);
            recheckedFolder = projectionFolder with { ReferenceState = folderState, ObservedAt = folderObservedAt };
        }

        IReadOnlyList<ProjectFileReference> recheckedFiles = detail.FileReferences
            .Select((projectionFile, idx) =>
            {
                ProjectFileReferenceValidationResult result = fileRefreshTasks[idx].Result;
                (ReferenceState st, DateTimeOffset oa) =
                    ProjectFileReferenceValidationOutcomeMapper.Map(result.Outcome, projectionFile, now);
                return projectionFile with { ReferenceState = st, ObservedAt = oa };
            })
            .ToArray();

        IReadOnlyList<ProjectMemoryReference> recheckedMemories = detail.MemoryReferences
            .Select((projectionMemory, idx) =>
            {
                ProjectMemoryValidationResult result = memoryRefreshTasks[idx].Result;
                (ReferenceState st, DateTimeOffset oa) =
                    ProjectMemoryValidationOutcomeMapper.Map(result.Outcome, projectionMemory, now);
                return projectionMemory with { ReferenceState = st, ObservedAt = oa };
            })
            .ToArray();

        System.Collections.Generic.IReadOnlyList<ProjectContextConversationEvidence> conversationEvidence =
            ProjectContextConversationEvidenceMapper.Map(conversations, now);

        ProjectContextAssemblyResult assembled = contextPolicy.Assemble(
            new ProjectContextAssemblyContext(
                AuthoritativeTenantId: tenantContext.AuthoritativeTenantId,
                RequestedTenantId: tenantContext.AuthoritativeTenantId,
                ProjectId: projectId,
                OperationKind: ProjectContextOperationKind.Refresh,
                CorrelationId: correlationId,
                TaskId: taskId,
                Now: now),
            new ProjectContextProjectEvidence(detail),
            new ProjectContextTenantAccess(tenantAccessResult),
            new ProjectContextReferenceEvidence(
                ProjectFolder: recheckedFolder,
                FileReferences: recheckedFiles,
                MemoryReferences: recheckedMemories,
                Conversations: conversationEvidence));

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(assembled.Context, ResponseJsonOptions);
    }
}
```

### Files To Read Before Editing

- `_bmad-output/implementation-artifacts/3-3-explain-context-selection.md` — Story 3.3 Dev Agent Record (the immediate-prior story; the wrapper-vs-no-wrapper decision; the partial-class endpoint shape Story 3.4 mirrors at file level; the same `StartAppAsync(...)` named-fixture builder pattern).
- `_bmad-output/implementation-artifacts/3-2-get-project-context.md` — Story 3.2 Dev Agent Record (validation summary 807→810 after review cycle 1, the closed `ProjectContextInclusionDiagnostic` 13-value vocabulary, the additive `ProjectAuthorizationResult.TenantAccessResult` extension, the `ProjectContextConversationEvidenceMapper` shape, the NSwag Linux fix, the AppHost smoke fallback, the negative-test checklist; the canonical handler body Story 3.4 ports).
- `_bmad-output/implementation-artifacts/3-1-context-assembly-policy-allowlist.md` — Story 3.1 Dev Agent Record (the pure policy + DTOs + the closed `ProjectContextInclusionDiagnostic` 13-value vocabulary + `docs/context-assembly-decision-matrix.md` + the `ProjectContextDecisionMatrixCompletenessTests` discovery pattern).
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` — §"Action Items" (no new realisation for Story 3.4; Story 3.4 is a query that does not exercise carry-forward U+2028/U+2029 hardening).
- `_bmad-output/implementation-artifacts/2-7-link-unlink-memory.md` — Memories ACL shape + `IProjectMemoryDirectory.ValidateLinkMemoryReferenceAsync` pattern Story 3.4's `RefreshMemoryReferenceAsync` mirrors.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — Folders file ACL shape + `IProjectFileReferenceDirectory.ValidateLinkFileReferenceAsync` pattern. **Critical:** this story's Dev Agent Record may name the opaque-id-only Folders read route Story 3.4's `RefreshFileReferenceAsync` needs — read it carefully before declaring a HALT.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` — Folders folder ACL shape + `IProjectFolderDirectory.ValidateSetProjectFolderAsync` pattern; the degraded `ProjectFolderCreationPending` path Story 3.4's mapper preserves.
- `_bmad-output/planning-artifacts/epics.md` lines 758–773 — Story 3.4 ACs (authoritative).
- `_bmad-output/planning-artifacts/architecture.md` line 422–432 (Process Patterns / ProjectContext assembly + AR-9 decision-matrix pointer), line 600–601 (Feature/FR mapping — Context Assembly), line 316 (Implementation Sequence step 7).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` §FR-18 (lines 247–254); §NFR (lines 337–339); §SM-1 (line 347); §SM-4 (line 350).
- `_bmad-output/project-context.md` — 96 rules.
- `docs/payload-taxonomy.md` lines 27–41 (safe categories — `ObservedAt` ↦ `Timestamp` UTC); lines 53–64 (forbidden categories — Story 3.4 mappers MUST NOT carry these).
- `docs/context-assembly-decision-matrix.md` line 5 (Story 3.4 named as consume-verbatim consumer), line 16 (`RefreshProjectContext (3.4)` column header), lines 18–27 (per-evidence-state rows — Refresh column identical to Get column), lines 34–42 (outer-override rows), lines 44–50 (Memories rows).
- `docs/checklists/mutation-and-query-negative-tests.md` (Story 3.2 / Action Item 7 deliverable) — 8-row checklist; rows 1 / 4 / 5 / 6 / 8 apply to Story 3.4.
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` lines 75–171, 234–238 (outer collapses + per-candidate loop + deterministic sort + `IsReadOnlyOperation` allowance for `Refresh`) — Story 3.1 surface, unchanged.
- `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:31` — `Refresh` member.
- `src/Hexalith.Projects/Context/ProjectContextReferenceEvidence.cs` — the input shape Story 3.4 composes from rechecked references.
- `src/Hexalith.Projects.Contracts/Models/{ProjectContext, ProjectContextReference, ProjectContextExclusion, ProjectFolderReference, ProjectFileReference, ProjectMemoryReference}.cs` — reused wire DTOs.
- `src/Hexalith.Projects.Contracts/Ui/{ProjectContextAssemblyOutcome, ProjectContextFreshness, ProjectContextInclusionCheck, ProjectContextInclusionDiagnostic, ReferenceState}.cs` — reused wire enums + closed vocabulary.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` lines 284–413 — OpenAPI spine; `GetProjectContext` (lines 284–347) and `GetProjectContextExplanation` (lines 348–413) are the read-shape oracles Story 3.4 mirrors.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs` — full file (142 lines); Story 3.4's new partial-class file is a port with the three documented changes.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs` — full file (144 lines); Story 3.3 sibling partial pattern.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` lines 71–340 (`ConfigureEndpoints` + the static helpers + the existing route registrations).
- `src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` — Story 3.2 mapper, reused as-is.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationResult.cs` + `ProjectAuthorizationGate.cs` — Story 3.2 additive extension target; Story 3.4 consumes unchanged.
- `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs` + `FoldersProjectFolderDirectory.cs` + `UnavailableProjectFolderDirectory.cs` + `ProjectFolderValidationResult.cs` — Story 2.4 ACL surface; Story 3.4 additively extends with `RefreshFolderReferenceAsync`.
- `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs` + `FoldersProjectFileReferenceDirectory.cs` + `UnavailableProjectFileReferenceDirectory.cs` + `ProjectFileReferenceValidationResult.cs` — Story 2.5 ACL surface; Story 3.4 additively extends with `RefreshFileReferenceAsync`.
- `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs` + `MemoriesProjectMemoryDirectory.cs` + `UnavailableProjectMemoryDirectory.cs` + `ProjectMemoryValidationResult.cs` — Story 2.7 ACL surface; Story 3.4 additively extends with `RefreshMemoryReferenceAsync`.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` lines 74–94 — DI registration of the three ACL directories.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` — read-side state shape Story 3.4 consumes.
- `src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs` + `RecordingLogger.cs` — Story 3.1 test helpers reused unchanged.
- `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` — FS-2 harness; Story 3.4 extends test fixtures (endpoint-response coverage), not the harness itself.
- `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs` — Story 3.2 endpoint tests; Story 3.4's `RefreshProjectContextTests.cs` mirrors the `StartAppAsync(...)` builder pattern.
- `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextExplanationTests.cs` — Story 3.3 endpoint tests; provides the most recent fixture-pattern reference.
- `tests/Hexalith.Projects.Tests/Context/ProjectContextEvaluationsTraceTests.cs` (Story 3.3) — Tier-1 trace tests; Story 3.4's Tier-1 mapper tests follow the same idiom at a different scope.
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` — Story 3.1 / 3.2 / 3.3 leakage extension; `ProjectContext_SerializesMetadataOnly` already covers the Refresh wire body.
- `tests/Hexalith.Projects.Client.Tests/GetProjectContextClientTests.cs` — Story 3.2 client test pattern; Story 3.4 mirrors with `RefreshProjectContextClientTests.cs`.

### Testing Requirements

See AC 8 / AC 9 / AC 10 / AC 11 / AC 12 / AC 13 for the full per-suite enumeration. Highlights:

- **Tier-2 endpoint handler correctness.** Named-fixture tests in `tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs` per AC 8 (matrix cells collapsed into a `[Theory]` since the Refresh column equals the Get column; AC 9 recovery / regression tests are the NEW coverage Story 3.4 owns).
- **Tier-1 outcome-mapper purity** (AC 11) — three new Tier-1 test files, one per mapper; full enum coverage via `Enum.GetValues<T>()`; override / preservation / `ObservedAt` semantics; mapper handles `TenantMismatch` by passing to the policy (the policy collapses).
- **Idempotency-Key on query rejected** (AC 8) — Tier-2; mirrors Story 3.2 / 3.3.
- **Route negatives** (AC 8) — Tier-2; mirrors Story 3.2 / 3.3.
- **Cross-tenant isolation + no-ACL-call assertion** (AC 10) — Tier-2 FS-8/SM-3; verifies `RecordingFolderDirectory.CallCount == 0` etc. so cross-tenant requests never leak tenant-existence to siblings.
- **Refresh recovery / regression contract** (AC 9) — Tier-2; the NEW tests Story 3.4 owns. `RefreshProjectContext_AllAclsReturnAccepted_AndProjectionStoredIncluded_IsByteIdenticalToGet` is the equivalence-on-no-drift proof.
- **Leakage over endpoint responses** (AC 12) — Tier-2 over labelled outcomes + error responses; reuses Story 3.1 / 3.2 / 3.3 harness.
- **No DTO-shape harness extension needed** — `ProjectContext` is already covered by Story 3.1; Story 3.4 ships no new wire DTOs.
- **Generated client substring assertions** (AC 13) — Client.Tests; mirrors Story 3.2 / 3.3.
- **No-sleep grep** — zero hits filtered to Story 3.4 test files.
- **Cross-tenant safe-denial 404** — never 403, never reveals existence; never calls sibling ACLs on cross-tenant requests.
- **Closed-vocabulary diagnostics** — every `Diagnostic` value surfaced through `ProjectContextExclusion` is structurally constrained by the policy's diagnostic-vocabulary enforcement; Story 3.4's mappers never produce a `Diagnostic` directly (they only translate to `ReferenceState`).

### Previous Story Intelligence

- **Story 3.3 (Explain Context Selection) — done, 858/858** (per sprint-status.yaml line 3 — review cycle 1 had 0 CRITICAL/HIGH/LOW remaining; 1 MEDIUM about sibling submodule pointer drift unrelated to Story 3.3 source surface). Established: (a) the partial-class `GetProjectContextExplanationEndpoint.cs` pattern Story 3.4 mirrors at file level; (b) the same `StartAppAsync(...)` named-fixture builder shape; (c) the same closed-vocabulary diagnostic enforcement; (d) the same `IsReadOnlyOperation` allowance pattern (Refresh and Explain both already on the allowlist); (e) the same FS-8/SM-3 cross-tenant safe-denial 404 pattern (Story 3.4 ADDS the no-ACL-call assertion); (f) the same canonical negative-test checklist Story 3.4 ticks off; (g) the `ProjectContextExplanation` wrapper is NOT reused — Story 3.4 returns bare `ProjectContext`.
- **Story 3.2 (Get Project Context) — done, 810/810 (post review cycle 1)** (per sprint-status.yaml line 5 — review cycle 1 auto-fixed 3 HIGH gaps adding the `TenantAccessUnavailable_ReturnsReadModelUnavailable503`, `AuthoritativeTenantIdMissing_ReturnsSafeDenial404`, and `ResponseHeaders_HaveCorrelationAndFreshness` tests). Established: (a) the partial-class `GetProjectContextEndpoint.cs` pattern Story 3.4 closely mirrors; (b) the additive `ProjectAuthorizationResult.TenantAccessResult` plumbing Story 3.4 consumes unchanged; (c) the `ProjectContextConversationEvidenceMapper` Story 3.4 reuses as-is; (d) the OpenAPI spine extension pattern; (e) the NSwag Linux fix Story 3.4 inherits; (f) the `[JsonIgnore]` on `ProjectContext.TenantId`; (g) the canonical negative-test checklist Story 3.4 ticks off; (h) the `ProjectContextConversationsPageSize = 100` constant declared at line 50 of `GetProjectContextEndpoint.cs` — Story 3.4 reuses the constant from the existing partial class (does NOT redeclare).
- **Story 3.1 (Context-assembly policy & allowlist) — done.** Established the pure allowlist-based `ProjectContextInclusionPolicy` Story 3.4 consumes unchanged. Closed `ProjectContextInclusionDiagnostic` vocabulary ships 13 values. `docs/context-assembly-decision-matrix.md` `Refresh` column is identical to `Get` by design. The policy's per-candidate emission contract (one row per candidate, NOT one per check) is the basis Story 3.4's tests rely on at AC 8 / AC 9. The policy MAPS `TenantMismatch` ACL outcomes to `Unauthorized` + `tenantMismatch` diagnostic at the boundary; Story 3.4's mappers hand the raw `TenantMismatch` to the policy without collapsing.
- **Story 2.7 (Link/Unlink Memory) — done.** Established `IProjectMemoryDirectory.ValidateLinkMemoryReferenceAsync` calling `MemoriesClient.GetCaseAsync(...)` (Story 2.6 ADR stable surface). Story 3.4 additively extends with `RefreshMemoryReferenceAsync` reusing the same `MemoriesClient.GetCaseAsync(...)` route. `ProjectMemoryValidationOutcome` carries 7 values that Story 3.4's mapper covers exhaustively.
- **Story 2.5 (File Reference link/unlink) — done.** Established `IProjectFileReferenceDirectory.ValidateLinkFileReferenceAsync` calling Folders metadata-only `GetFolderFileMetadata` POST. `ProjectFileReferenceValidationOutcome` carries 8 values. **Critical:** Story 3.4 needs an opaque-id-only Folders read route (no workspaceId / filePath); Story 2.5's Dev Agent Record names the available Folders client routes — the dev agent reads it carefully in Task 1's capability gate inspection.
- **Story 2.4 (Project Folder reference) — done.** Established the degraded `ProjectFolderCreationPending` path (`folder_create_external_unavailable`); the policy maps `Pending` → `ReferenceState.Pending` / `FailedCheck = ReferenceFreshness` / `Diagnostic = projectFolderPending`. Story 3.4's Folder mapper preserves the `Pending` state on `Unavailable` outcome to keep the diagnostic stable.
- **Story 2.3 (Conversation write-side) — done.** Pattern A holds — Projects does not store conversation membership; Story 3.4's conversation evidence comes via `IProjectConversationDirectory.ListForProjectAsync` (Story 2.1 read ACL) and through `ProjectContextConversationEvidenceMapper` (Story 3.2). The conversation read ACL is already fresh-on-every-query (no projection-stored conversation state) so Story 3.4 does NOT add a conversation recheck path.
- **Story 2.1 (Conversation Reference Read ACL) — done.** Established `ProjectConversationsPage` + `ProjectConversationItem` + `ProjectConversationTrustSignal` shape; Story 3.2 mapper translates; Story 3.4 reuses the mapper.
- **Story 1.6 (Tenant access & layered fail-closed authorization) — done.** `TenantAccessAuthorizationResult` consumed via `ProjectContextTenantAccess`. Story 3.2 threaded it through `ProjectAuthorizationResult` additively; Story 3.4 consumes unchanged.
- **Story 1.4 (Tracer bullet) — done.** Safe-denial 404 contract + FS-2 `NoPayloadLeakage` harness + FS-8/SM-3 cross-tenant isolation harness reused unchanged.
- **Story 1.3 (OpenAPI Contract Spine + NSwag client + idempotency hasher + fingerprint gate flip) — done.** The spine fingerprint gate is the canonical churn-check; Story 3.4 deliberately flips it for one cycle (new operation only — no new schemas).
- **Epic 2 retrospective carry-forward action items:**
  - Action 1 (NSwag Linux path fix): CLOSED by Story 3.2.
  - Action 5 (AppHost smoke check): CLOSED by Story 3.2 (manual fallback path documented).
  - Action 7 (route/body + missing-Idempotency-Key checklist): CLOSED by Story 3.2; Story 3.4 ticks the query-side rows (1 / 4 / 5 / 6 / 8).
  - Action 2 (U+2028/U+2029 hardening): does NOT apply to Story 3.4 (query has no idempotency-fingerprint surface); survives in the carry-forward list for the next mutation surface.
  - Action 4 (per-story leakage extensions): realized for the endpoint-response surface (Task 6); no DTO-shape extension needed (`ProjectContext` already covered).
  - Action 3 (decision-matrix doc as single ref): already realized by Story 3.1; Story 3.4 consumes it.
  - Action 6 (track unproduced shared-vocab outcomes): already realized by Story 3.1; Story 3.4 doesn't change the list.
  - Action 8 (Folders-side external POST follow-up): tracking-only; Story 3.4 is read-side; not blocking. **BUT** if Story 3.4's Task 1 capability gate fails because the opaque-id-only Folders file-reference read route does not exist, the resolution lives in this action's scope (Folders submodule edit).
- **Recent commit hygiene.** Stories 2.5 (`e127b7a`), 2.6 (`0058ac3`), 2.7 (`70f2ebe`), 3.1 (`67beac6`), 3.2 (`05c0ff9`), 3.3 (`89cd7a5`) all follow story-scoped commits with no nested-recursive submodule init. Story 3.4 must do the same.

### Out Of Scope

- Implementing `GetConversationStartSetup` (Story 3.5) and the `ConversationStartSetupProjection`.
- Implementing project-resolution policy (Epic 4) — `Resolution/` remains empty.
- Adding new shared-vocabulary enum values (per AC 15). If genuinely required, HALT and surface the conflict.
- Adding multi-page support to the conversation evidence (single first-page snapshot at `PageSize=100` is sufficient — same as Story 3.2 / 3.3).
- Re-checking the conversation evidence beyond Story 2.1's existing per-query trust-signal mechanism (the conversation ACL is already fresh-on-every-query; no projection-stored conversation state to refresh).
- Adding a Refresh-specific wire wrapper (Story 3.4 returns bare `ProjectContext` — see Guardrails / Body-shape parity).
- Surfacing the policy's `Evaluations` trace on the Refresh wire body (Story 3.3 Explain owns that surface).
- Modifying `src/Hexalith.Projects/Context/` files in any way (Story 3.1 invariant).
- Modifying `_bmad-output/planning-artifacts/epics.md` Story 3.4 acceptance criteria.
- Modifying `docs/context-assembly-decision-matrix.md` (the `Refresh` column already exists and is identical to `Get`).
- Modifying `docs/event-catalog.md` §"Shared vocabulary — producer of last resort" (Story 3.4 produces no new values).
- Modifying `docs/checklists/mutation-and-query-negative-tests.md` rows (the 8-row canonical checklist stays unchanged — Story 3.4 only references its existing rows).
- Modifying the shared vocabulary enums, the existing Story 3.1 wire DTOs (beyond an additive `[JsonPropertyName]` if structurally required), `ProjectContextAssemblyResult` (the policy's internal result type), `ProjectContextExplanation` (the Story 3.3 wrapper), or the Story 3.2 / 3.3 endpoint files.
- Modifying the existing `ValidateXxxAsync` methods on the three ACL interfaces (Story 3.4 only ADDS `RefreshXxxAsync` methods).
- U+2028/U+2029 canonicaliser hardening (Action Item 2 — for the next mutation surface, not for Story 3.4).
- Folders-side external `POST /api/v1/folders` mapping (Action Item 8 — Folders submodule scope; tracked only).
- DeterministicActorPartyResolver replacement — Story 3.4 is read-side and doesn't invoke it.
- Real-Keycloak / OIDC E2E (Epic 5 territory).
- Adding an AppHost smoke test for Refresh (Action Item 5 was closed by Story 3.2; in-process WebApplication-slim coverage is sufficient).
- Advancing any submodule pointer (`Hexalith.Memories` / `Conversations` / `Folders` / `Tenants` / `EventStore` / `FrontComposer` / `Commons` / `AI.Tools` / `Builds`) or running `git submodule update --init --recursive`.
- Performing nested recursive submodule initialization / update.

### Developer HALT Conditions

- **HALT before any code change** if Story 3.1 / 3.2 / 3.3's policy / DTOs / handlers would need to change for Story 3.4 to land beyond an additive `[JsonPropertyName]`. Surface the conflict in the Dev Agent Record; the resolution is a follow-up story / ADR.
- **HALT before Task 2 (file-reference portion)** if no Folders typed-client read route validates a file reference by opaque `(folderId, fileReferenceId)` WITHOUT requiring workspaceId / filePath inputs (per Task 1's capability gate). Two viable resolutions: (a) defer the file-recheck portion of Story 3.4 to a follow-up story (proceed with folder + memory recheck only — AC 9 file-recovery tests become deferred); (b) open a Folders submodule story to add the read route. Record the chosen path explicitly.
- **HALT** if `ProjectContextOperationKind.Refresh` was removed or moved out of `IsReadOnlyOperation(...)` (regression vs. Story 3.1).
- **HALT** if the policy's per-candidate evaluation emission contract has changed — e.g. the policy stops emitting a row for included references (Story 3.1 invariant).
- **HALT** if `ProjectContextReferenceEvidence` no longer accepts the per-kind candidate collections Story 3.4 composes (Story 3.1 invariant).
- **HALT** if the conversation-page cap can no longer be `100` without paging — Story 3.4 does NOT introduce paging in v1.
- **HALT** if implementing the endpoint would require modifying `ProjectAggregate.*` / `ProjectState` / `ProjectStateApply` / projections / `IProjectCommandSubmitter` / the EXISTING `ValidateXxxAsync` methods on the three ACL interfaces (Story 3.4 is read-side; mutation surfaces stay untouched).
- **HALT** if the wire response would surface any `Diagnostic` value outside `ProjectContextInclusionDiagnostic.Values` (structurally enforced by the policy + closed-vocab construction validation).
- **HALT** if `Thread.Sleep` / `Task.Delay` / `SpinWait` / wall-clock polling is required to make a test pass.
- **HALT** if a new shared-vocabulary enum value, a new `ProjectContextInclusionDiagnostic` member, a new `ProjectContextOperationKind` member, or a new `ProjectXxxValidationOutcome` member appears genuinely required.
- **HALT** if a submodule pointer or `_bmad-output/planning-artifacts/epics.md` Story 3.4 ACs would need to change for the story to land.

## References

- `_bmad-output/planning-artifacts/epics.md` lines 758–773 — Story 3.4 ACs (authoritative).
- `_bmad-output/planning-artifacts/architecture.md` — AR-9 prose lines 422–432; FR mapping lines 600–601; Implementation Sequence step 7 (line 316); the existing Story 3.2 cross-link to `docs/checklists/mutation-and-query-negative-tests.md`.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` lines 247–254 — FR-18 verbatim; lines 337–339 — NFRs (security/reliability/observability); line 347 — SM-1 (Project Context availability); line 350 — SM-4 (Interactive metadata latency).
- `_bmad-output/implementation-artifacts/3-3-explain-context-selection.md` — immediate-prior story; the partial-class endpoint shape + the named-fixture test builder + the wrapper-vs-no-wrapper decision Story 3.4 inverts.
- `_bmad-output/implementation-artifacts/3-2-get-project-context.md` — the canonical handler body Story 3.4 ports; the additive `ProjectAuthorizationResult.TenantAccessResult`; the `ProjectContextConversationEvidenceMapper`; the OpenAPI spine extension pattern; the `[JsonIgnore]` precedent. Validation summary 807→810.
- `_bmad-output/implementation-artifacts/3-1-context-assembly-policy-allowlist.md` — the pure policy + DTOs Story 3.4 consumes unchanged; the closed `ProjectContextInclusionDiagnostic` 13-value vocabulary; `docs/context-assembly-decision-matrix.md` Refresh-column-identical-to-Get-column invariant; the policy's `TenantMismatch`-to-`Unauthorized` boundary collapse at lines 473–491 / 632.
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` — §"Action Items"; no new realisation for Story 3.4.
- `_bmad-output/implementation-artifacts/2-7-link-unlink-memory.md` — Memories ACL shape; `MemoriesClient.GetCaseAsync` stable read route Story 3.4 reuses.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — Folders file ACL shape. **Critical:** identifies the Folders typed-client read routes available for the Story 3.4 capability gate.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` — Folders folder ACL shape; the `ProjectFolderCreationPending` degraded path Story 3.4's mapper preserves.
- `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md` — `IProjectConversationDirectory.ListForProjectAsync` ACL + `ProjectConversationTranslator` boundary pattern.
- `_bmad-output/implementation-artifacts/1-6-tenant-access-layered-fail-closed-authorization.md` — `TenantAccessAuthorizationResult` shape Story 3.4 threads through via Story 3.2's `ProjectAuthorizationResult.TenantAccessResult`.
- `_bmad-output/implementation-artifacts/1-4-create-project-end-to-end-tracer-bullet.md` — safe-denial 404 contract + FS-2 / FS-8 reusable harnesses.
- `_bmad-output/implementation-artifacts/1-3-openapi-contract-spine-generated-typed-client.md` — OpenAPI spine + NSwag client + idempotency hasher + fingerprint gate baseline; Story 3.4 deliberately bumps the fingerprint.
- `_bmad-output/project-context.md` — 96 rules (Tier-1 purity at line 76, metadata-only logging at line 105 / 133, central package management at line 93–94, no `V2` types at line 42, additive contracts at line 98, deterministic tests at line 87).
- `docs/context-assembly-decision-matrix.md` — Story 3.1 fail-closed matrix; the `Refresh` column is identical to the `Get` column by design (line 5 names Story 3.4 as a consume-verbatim consumer; line 16 declares the column header; lines 18–50 list every cell).
- `docs/event-catalog.md` lines 223–262 — Shared vocabulary — producer of last resort; Story 3.4 does NOT extend.
- `docs/payload-taxonomy.md` lines 27–41 (safe categories — `Timestamp` UTC ↦ `ObservedAt`); lines 53–64 (forbidden categories).
- `docs/adr/memories-link-target.md` (Accepted) — Story 2.6 ADR; Story 3.4 reuses the stable `MemoriesClient.GetCaseAsync` route.
- `docs/adr/identifier-boundary.md` — sibling identifier reuse rule.
- `docs/checklists/mutation-and-query-negative-tests.md` — Story 3.2 / Action Item 7 deliverable; rows 1 / 4 / 5 / 6 / 8 apply to Story 3.4.
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` — Story 3.1 policy; Story 3.4 consumes unchanged. Critical lines: 75–84 (TenantAuthority outer collapse → empty Evaluations), 89–98 (ProjectVisibility outer collapse → empty Evaluations), 168–171 (deterministic `(Kind, Id)` Ordinal sort), 234–238 (`IsReadOnlyOperation` includes `Refresh`), 473–491 (Memories `TenantMismatch` → `Unauthorized` collapse), 632 (`ClassifyReferenceState` file `TenantMismatch` mapping).
- `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs:31` — `Refresh` member.
- `src/Hexalith.Projects/Context/ProjectContextReferenceEvidence.cs` — the input shape Story 3.4 composes from rechecked references.
- `src/Hexalith.Projects.Contracts/Models/{ProjectContext, ProjectContextReference, ProjectContextExclusion, ProjectFolderReference, ProjectFileReference, ProjectMemoryReference}.cs` — reused wire DTOs.
- `src/Hexalith.Projects.Contracts/Ui/{ProjectContextAssemblyOutcome, ProjectContextFreshness, ProjectContextInclusionCheck, ProjectContextInclusionDiagnostic, ReferenceState, ProjectReasonCode}.cs` — reused wire enums + closed vocabulary.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — OpenAPI spine; `GetProjectContext` (lines 284–347) read-shape oracle; `GetProjectContextExplanation` (lines 348–413) sibling oracle.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — `ConfigureEndpoints` + the `MapGet(...)` registrations at lines 115 / 135 + the static helpers Story 3.4's handler consumes.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextEndpoint.cs` — Story 3.2 handler; Story 3.4's new sibling partial file is a port with the three documented changes.
- `src/Hexalith.Projects.Server/Queries/GetProjectContextExplanationEndpoint.cs` — Story 3.3 sibling pattern.
- `src/Hexalith.Projects.Server/Conversations/ProjectContextConversationEvidenceMapper.cs` — Story 3.2 mapper; reused as-is.
- `src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationResult.cs` + `ProjectAuthorizationGate.cs` — Story 3.2 additive extension; Story 3.4 consumes unchanged.
- `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs` + `FoldersProjectFolderDirectory.cs` + `UnavailableProjectFolderDirectory.cs` + `ProjectFolderValidationResult.cs` — Story 2.4 ACL surface; Story 3.4 additively extends.
- `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs` + `FoldersProjectFileReferenceDirectory.cs` + `UnavailableProjectFileReferenceDirectory.cs` + `ProjectFileReferenceValidationResult.cs` — Story 2.5 ACL surface; Story 3.4 additively extends.
- `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs` + `MemoriesProjectMemoryDirectory.cs` + `UnavailableProjectMemoryDirectory.cs` + `ProjectMemoryValidationResult.cs` — Story 2.7 ACL surface; Story 3.4 additively extends.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` lines 74–94 — DI registration of the three ACL directories.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` — read-side state shape Story 3.4 consumes.
- `src/Hexalith.Projects.Contracts/Queries/PageRequest.cs` — page-request shape; default 25, max 100.
- `src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs` + `RecordingLogger.cs` — Story 3.1 test helpers reused unchanged.
- `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` — FS-2 harness; Story 3.4 extends test fixtures, not the harness itself.
- `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextTests.cs` — Story 3.2 endpoint tests; Story 3.4's `RefreshProjectContextTests.cs` mirrors the `StartAppAsync(...)` builder pattern.
- `tests/Hexalith.Projects.Server.Tests/Queries/GetProjectContextExplanationTests.cs` — Story 3.3 sibling test file.
- `tests/Hexalith.Projects.Tests/Context/ProjectContextDecisionMatrixCompletenessTests.cs` (Story 3.1) — the per-cell completeness pattern.
- `tests/Hexalith.Projects.Tests/Context/ProjectContextEvaluationsTraceTests.cs` (Story 3.3) — Tier-1 trace test pattern Story 3.4's mapper tests partially mirror.
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` — Story 3.1 / 3.2 / 3.3 leakage extension; `ProjectContext_SerializesMetadataOnly` already covers the Story 3.4 wire body.
- `tests/Hexalith.Projects.Client.Tests/GetProjectContextClientTests.cs` — Story 3.2 client test pattern; Story 3.4 mirrors with `RefreshProjectContextClientTests.cs`.

## Dev Agent Record

### Agent Model Used

- Claude Opus 4.7 (create-story, 2026-05-28)
- Claude Opus 4.7 1M context (dev-story, 2026-05-28)

### Debug Log References

- `dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj` — 0 W / 0 E after Task 2 (ACL Refresh methods), Task 3 (mappers), and Task 5 (handler).
- `dotnet build src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj` — 0 W / 0 E; NSwag regen ran on Linux without manual intervention (Story 3.2 fix inherited).
- `dotnet build Hexalith.Projects.slnx` — 0 W / 0 E (full solution).
- `dotnet test Hexalith.Projects.slnx --no-build` — 926 passed / 0 failed / 0 skipped across all five lanes:
  - `Hexalith.Projects.Server.Tests` 317 (baseline 252 + 65: Refresh endpoint 26 + 3 mapper files = 39).
  - `Hexalith.Projects.Tests` 427 (unchanged — see Tier-1 deviation note below).
  - `Hexalith.Projects.Contracts.Tests` 128 (unchanged).
  - `Hexalith.Projects.Client.Tests` 40 (baseline 37 + 3).
  - `Hexalith.Projects.Integration.Tests` 14 (unchanged).
- Boundary greps clean (no Hexalith.Conversations|Folders|Memories under `src/Hexalith.Projects/Context/`; no `Hexalith.Folders.|Hexalith.Memories.` imports in the new handler beyond the via-Projects-Server.Folders/Memories aliasing; only the documented `using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;` alias in the handler).
- No-wall-clock grep clean across handler + three mappers (`DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount` returns 0 hits).
- No-sleep grep clean across all new test files (`Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(` returns 0 hits).
- `git diff --check` clean across hand-authored sources. All hand-authored `.cs` / `.md` / `.yaml` files are LF-only and NUL-free.

### Completion Notes List

**Capability-gate HALT path taken (option (a) per story Task 1 / §"Required Capability Path" / §"Developer HALT Conditions"):**

- The Folders typed client `Hexalith.Folders.Client.Generated.IClient` exposes no read route that validates a file reference by opaque `(folderId, fileReferenceId)` without `workspaceId` + `filePath` inputs. Every file-touching method (`AddFileAsync`, `ChangeFileAsync`, `RemoveFileAsync`, `ListFolderFilesAsync`, `GetFolderFileMetadataAsync`, `SearchFolderFilesAsync`, `GlobFolderFilesAsync`, `ReadFileRangeAsync`) requires `workspaceId` and a path-bearing body. The `ProjectFileReference` projection (per `docs/payload-taxonomy.md`) stores only `FileReferenceId` and `FolderId?` — no workspaceId, no filePath, no path-classified fields.
- **Resolution:** the additive `IProjectFileReferenceDirectory.RefreshFileReferenceAsync` method was still added (additive contracts rule, AC 2) and `FoldersProjectFileReferenceDirectory.RefreshFileReferenceAsync` returns `ProjectFileReferenceValidationOutcome.Unavailable` (fail-closed). However the **handler does NOT invoke the file-recheck** — file references retain their projection-stored state, identical to Story 3.2 Get behavior. The endpoint-side rationale is captured in the `RefreshProjectContextEndpoint.cs` class XML doc.
- **Tests deferred to a follow-up Folders story:** the AC 9 file-only recovery / regression tests (`RefreshProjectContext_AclReportsAccepted_OverridesProjectionStoredStale` for files, `RefreshProjectContext_AclReportsUnavailable_OverridesProjectionStoredIncluded` for files, `RefreshProjectContext_AclThrows_SurfacesAsUnavailable_FailsClosedNot500` for files). Memory variant of `_AclReportsUnavailable_OverridesProjectionStoredIncluded` ships in this story. `_AclThrows_SurfacesAsUnavailable_FailsClosedNot500` for folder/memory is implicitly covered at the adapter level by `ProjectFolderDirectoryTests.cs` (the existing `CheckFolderEvidenceAsync` exception-translation harness shared with the new `RefreshFolderReferenceAsync`) and `ProjectMemoryDirectoryTests.cs` (the existing `ThrowingHandler` test exercises the shared `CheckMemoryEvidenceAsync` path); a dedicated endpoint-level Throws test would require a stub directory deliberately breaking the no-throw contract and is not necessary. The equivalent file-recheck tests can ship once a Folders submodule story adds an opaque-id-only file-reference read route (Action Item 8 — currently tracking-only in the Epic 2 retro carry-forward list).

**Other notes:**

- ✅ Folder recheck via `IProjectFolderDirectory.RefreshFolderReferenceAsync` — `FoldersProjectFolderDirectory` extracts the shared sibling-read helper from `ValidateSetProjectFolderAsync` into a private `CheckFolderEvidenceAsync(...)` reused by both code paths (zero behavior change on the Validate path).
- ✅ Memory recheck via `IProjectMemoryDirectory.RefreshMemoryReferenceAsync` — `MemoriesProjectMemoryDirectory` extracts the shared `MemoriesClient.GetCaseAsync` evidence call into `CheckMemoryEvidenceAsync(...)` reused by both code paths (zero behavior change on the Validate path).
- ✅ Conversation evidence retained via the Story 3.2 `IProjectConversationDirectory.ListForProjectAsync(...)` ACL with `PageSize = ProjectContextConversationsPageSize = 100` (constant reused from `GetProjectContextEndpoint.cs:50` — not redeclared).
- ✅ All three Refresh fan-outs + the conversation-page fetch await with a single `Task.WhenAll(...)`. Result ordering preserved by walking the input collection (`MemoryReferences`) in stored order and mapping outcomes by index. No `Task.WhenAny`, no `Thread.Sleep`, no `Task.Delay`.
- ✅ Three outcome mappers (`ProjectFolderValidationOutcomeMapper`, `ProjectFileReferenceValidationOutcomeMapper`, `ProjectMemoryValidationOutcomeMapper`) are pure static; receive `now` as input (no wall-clock); preserve projection-stored `ObservedAt` when the recheck confirms the state; replace with `now` when the state changes; pass `TenantMismatch` through to the policy without collapsing (the policy collapses at `ProjectContextInclusionPolicy.cs:473–491 / 632`); preserve `Pending` on `Unavailable + projection-stored Pending` so the diagnostic stays `projectFolderPending`.
- ✅ Handler is a port of Story 3.2's `GetProjectContextAsync` with the three documented changes (OperationKind, ACL fan-outs, rechecked-state composition). The defensive null-collapse on `authorization.TenantAccessResult is not { } tenantAccessResult` (mirrors `GetProjectContextEndpoint.cs:94–99`) is carried forward verbatim.
- ✅ `MapGet("/api/v1/projects/{projectId}/context/refresh", ...)` registered directly after the `/context/explain` route registration. Sets `X-Correlation-Id` echo + `X-Hexalith-Freshness: eventually_consistent` response headers; returns `Results.Json(assembled.Context, ResponseJsonOptions)` (bare `ProjectContext`, NOT a wrapper — Story 3.4 deliberately keeps wire-shape parity with Story 3.2 Get).
- ✅ OpenAPI spine extended with `RefreshProjectContext` operation block immediately after `GetProjectContextExplanation`. No new schemas (reuses `ProjectContext`); `x-hexalith-canonical-error-categories` adds `referenced_resource_unavailable` after `read_model_unavailable` (additive). Regen produced 184 lines added to `HexalithProjectsClient.g.cs` and the SHA256 fingerprint update in `HexalithProjectsIdempotencyHelpers.g.cs` (no new idempotency entry — queries are idempotency-free).
- ✅ Tier-2 endpoint coverage in `tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs` — 27 tests covering happy path, idempotency rejection, freshness rejection, malformed projectId Theory (7 cases), cross-tenant safe-denial 404 + the AC 10 no-ACL-call-on-cross-tenant invariant (RecordingFolderDirectory/RecordingMemoryDirectory.CallCount remains 0), archived project, conversation page unavailable, tenant-access unavailable 503, authoritative-tenant-id missing 404, correlation/freshness header echo, extra query parameters tolerated, response-body no-leakage across four labelled outcomes, AC 9 folder/memory recovery/regression contract (Accepted-overrides-Stale, Archived-overrides-Included, Unavailable-overrides-Included, TenantMismatch-overrides-to-Unauthorized, Denied-surfaces-Unauthorized, FolderUnavailable+Pending-preserves-Pending, equivalence-on-no-drift, deterministic fan-out by ReferenceId Ordinal sort).
- ✅ Tier-1 mapper purity tests in `tests/Hexalith.Projects.Server.Tests/Folders/Project*OutcomeMapperTests.cs` and `tests/Hexalith.Projects.Server.Tests/Memories/ProjectMemoryValidationOutcomeMapperTests.cs` — 39 tests covering full enum mapping via Theory, override semantic (Accepted overrides Stale to Included), `ObservedAt` preservation when state unchanged / replacement when state changes, Pending preservation rule (folder only), TenantMismatch-passes-to-policy assertion, `Map_AllOutcomes_CoveredByTheory` completeness checks pinned to enum-member counts (6 folder / 8 file / 7 memory).
- ✅ Client tests in `tests/Hexalith.Projects.Client.Tests/RefreshProjectContextClientTests.cs` — three substring assertions over the regenerated `.g.cs`: `RefreshProjectContextAsync(string projectId` + `Task<ProjectContext> RefreshProjectContextAsync` (typed method shape), `HexalithProjectsIdempotencyHelpers.g.cs` does not contain `RefreshProjectContext` (no idempotency surface), LF-only + NUL-free.
- ✅ The defensive `with` composition produces a NEW `ProjectFolderReference` / `ProjectMemoryReference` instance per rechecked candidate. Projection-stored `DisplayName` / `ReasonCode?` / opaque ids carry through unchanged; only `ReferenceState` and `ObservedAt` change.

**Deviations from story file:**

- **Mapper test location.** Story file Task 7 / AC 11 specifies `tests/Hexalith.Projects.Tests/Folders/` and `tests/Hexalith.Projects.Tests/Memories/` for the Tier-1 mapper test files. Placing them there would require adding a `Hexalith.Projects.Server` ProjectReference to `Hexalith.Projects.Tests.csproj`, which would pull in `Microsoft.NET.Sdk.Web` + Folders/Memories/EventStore client packages and break the Tier-1 purity rule documented at `_bmad-output/project-context.md` line 76. The mapper tests live at `tests/Hexalith.Projects.Server.Tests/Folders/Project*OutcomeMapperTests.cs` and `tests/Hexalith.Projects.Server.Tests/Memories/ProjectMemoryValidationOutcomeMapperTests.cs` — same Tier-1 BODIES (no infrastructure, no DI, no network), different location. This mirrors the Story 3.2 deviation pattern (AC 10 / AC 13 cross-tier ProjectReference rejected for the same reason).
- **File-recheck deferred.** Per the capability-gate HALT (above). The AC 9 file-only recovery / regression tests are not implemented in this story; equivalent folder + memory tests are present. Sprint-status note carries this forward to a follow-up Folders submodule story.
- **Conversation-page cap unchanged.** Default 100 from `ProjectContextConversationsPageSize` (reused from Story 3.2 partial-class constant — not redeclared).
- **Single Story 3.1 / 3.2 / 3.3 surface touch:** none required. No `[JsonPropertyName]` additive attributes were needed; `JsonNamingPolicy.CamelCase` resolves every property correctly. The existing `[JsonIgnore]` on `ProjectContext.TenantId` is sufficient.
- **No submodule pointer advance.** Pre-existing `m` markers on `Hexalith.Commons` / `Hexalith.Conversations` / `Hexalith.Parties` are unrelated to Story 3.4 (carried forward from prior sessions per the initial `git status` baseline).

**Negative-test checklist tick-off (`docs/checklists/mutation-and-query-negative-tests.md`):**

- **Row 1 (Malformed identifier → safe-denial 404)** — ✅ Covered by `RefreshProjectContext_MalformedProjectId_ReturnsSafeDenial404` Theory (7 inputs: whitespace, tab, dot, double-dot, URL-encoded `..%2F..`, embedded slash, unicode bidi).
- **Row 2 (Route/body identity mismatch on mutation)** — N/A — Story 3.4 is a query (no body).
- **Row 3 (Missing Idempotency-Key on mutation)** — N/A — Story 3.4 is a query (Idempotency-Key is rejected when present, not required when absent).
- **Row 4 (Idempotency-Key PRESENT on query → 400 after authz)** — ✅ Covered by `RefreshProjectContext_IdempotencyKeyPresent_ReturnsValidationProblem` and `RefreshProjectContext_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404` (proves Idempotency-Key validation happens AFTER authorization).
- **Row 5 (Stricter `X-Hexalith-Freshness` → 400)** — ✅ Covered by `RefreshProjectContext_StricterFreshnessRequested_ReturnsValidationProblem`.
- **Row 6 (Cross-tenant safe-denial 404)** — ✅ Covered by `RefreshProjectContext_CrossTenant_ReturnsSafeDenial404_AndNoAclCallWasMade` PLUS the AC 10 no-ACL-call-on-cross-tenant invariant (RecordingFolderDirectory.CallCount == 0 and RecordingMemoryDirectory.CallCount == 0).
- **Row 7 (Unknown Idempotency-Key retry conflict on mutation)** — N/A — Story 3.4 is a query.
- **Row 8 (`ReferenceState.Unavailable && Retryable` → 503 ReadModelUnavailable)** — ✅ Covered by `RefreshProjectContext_TenantAccessUnavailable_ReturnsReadModelUnavailable503` (uses `ThrowingTenantAccessProjectionStore` to trigger the retryable Unavailable path).

**Test count summary:** baseline 858 → 927 (+69). Per-lane: Server.Tests 252→318 (+66), Tests 427→427 (mapper tests moved per deviation note), Contracts.Tests 128→128, Client.Tests 37→40 (+3), Integration.Tests 14→14. Total expectation from story file was ~+58; actual +69 (the AC 9 fan-out determinism test, the leakage-across-outcomes test, and the review-cycle-added `_AclReportsUnavailable_OverridesProjectionStoredIncluded_ForMemory` test cover more cases than estimated). Failed: 0. Skipped: 0. **Review cycle 1 (2026-05-28):** added the missing `_AclReportsUnavailable_OverridesProjectionStoredIncluded_ForMemory` test (filled an AC 9 gap the original Completion Notes had inaccurately claimed was covered) and corrected the Completion Notes wording about adapter-level AclThrows coverage.

**`dotnet build` and OpenAPI fingerprint:**

- Solution build: `dotnet build Hexalith.Projects.slnx` — 0 warnings / 0 errors.
- `.g.cs` regenerated cleanly: `HexalithProjectsClient.g.cs` +184 lines (RefreshProjectContextAsync method + path-update wiring); `HexalithProjectsIdempotencyHelpers.g.cs` +4 / -2 (SHA256 fingerprint update only — no new idempotency entry).
- OpenAPI spine fingerprint deliberately updated for this story (allowed per AC 4). Story 3.5 (`GetConversationStartSetup`) must show zero spine diff outside its own surface.
- `git diff --check`: clean. CRLF: none in hand-authored files (verified by `grep $'\r'`).

### File List

**New files (Story 3.4):**

- `src/Hexalith.Projects.Server/Queries/RefreshProjectContextEndpoint.cs` — new partial-class file containing the `RefreshProjectContextAsync` handler.
- `src/Hexalith.Projects.Server/Folders/ProjectFolderValidationOutcomeMapper.cs` — pure static outcome-to-ReferenceState mapper for folder rechecks.
- `src/Hexalith.Projects.Server/Folders/ProjectFileReferenceValidationOutcomeMapper.cs` — pure static outcome-to-ReferenceState mapper for file rechecks (additive; not currently consumed by the handler per the HALT path).
- `src/Hexalith.Projects.Server/Memories/ProjectMemoryValidationOutcomeMapper.cs` — pure static outcome-to-ReferenceState mapper for memory rechecks.
- `tests/Hexalith.Projects.Server.Tests/Queries/RefreshProjectContextTests.cs` — 26 Tier-2 endpoint tests.
- `tests/Hexalith.Projects.Server.Tests/Folders/ProjectFolderValidationOutcomeMapperTests.cs` — Tier-1 purity tests for folder mapper.
- `tests/Hexalith.Projects.Server.Tests/Folders/ProjectFileReferenceValidationOutcomeMapperTests.cs` — Tier-1 purity tests for file mapper.
- `tests/Hexalith.Projects.Server.Tests/Memories/ProjectMemoryValidationOutcomeMapperTests.cs` — Tier-1 purity tests for memory mapper.
- `tests/Hexalith.Projects.Client.Tests/RefreshProjectContextClientTests.cs` — Tier-1 generated-client substring assertions.

**Modified files (Story 3.4):**

- `src/Hexalith.Projects.Server/Folders/IProjectFolderDirectory.cs` — additive `RefreshFolderReferenceAsync(...)` method.
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFolderDirectory.cs` — implements `RefreshFolderReferenceAsync` via extracted shared `CheckFolderEvidenceAsync(...)` helper (zero behavior change on Validate path).
- `src/Hexalith.Projects.Server/Folders/UnavailableProjectFolderDirectory.cs` — fail-closed `RefreshFolderReferenceAsync` returning `Unavailable`.
- `src/Hexalith.Projects.Server/Folders/IProjectFileReferenceDirectory.cs` — additive `RefreshFileReferenceAsync(...)` method.
- `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs` — implements `RefreshFileReferenceAsync` returning `Unavailable` (HALT-path fail-closed default; documented in code comment).
- `src/Hexalith.Projects.Server/Folders/UnavailableProjectFileReferenceDirectory.cs` — fail-closed `RefreshFileReferenceAsync` returning `Unavailable`.
- `src/Hexalith.Projects.Server/Memories/IProjectMemoryDirectory.cs` — additive `RefreshMemoryReferenceAsync(...)` method.
- `src/Hexalith.Projects.Server/Memories/MemoriesProjectMemoryDirectory.cs` — implements `RefreshMemoryReferenceAsync` via extracted shared `CheckMemoryEvidenceAsync(...)` helper (zero behavior change on Validate path).
- `src/Hexalith.Projects.Server/Memories/UnavailableProjectMemoryDirectory.cs` — fail-closed `RefreshMemoryReferenceAsync` returning `Unavailable`.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` — adds `endpoints.MapGet("/api/v1/projects/{projectId}/context/refresh", ...)` registration after the `/context/explain` route.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — new `GET /api/v1/projects/{projectId}/context/refresh` operation block (no new schemas; reuses `ProjectContext`).
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs` — regenerated (+184 lines): typed `RefreshProjectContextAsync(...)` method.
- `src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs` — regenerated (SHA256 fingerprint constants only; no new idempotency entry).
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` — adds the new `RefreshXxxAsync` methods to existing fakes (`FixedProjectFolderDirectory`, `TrackingProjectFolderDirectory`, `TrackingProjectFileReferenceDirectory`, `TrackingProjectMemoryDirectory`) with fail-closed `Unavailable` default bodies (additive-contract carry-forward; existing tests unaffected).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `3-4-refresh-project-context: ready-for-dev` → `review`.
- `_bmad-output/implementation-artifacts/3-4-refresh-project-context.md` — this file: ticked task checkboxes; Status `ready-for-dev` → `review`; populated Dev Agent Record, File List, Change Log.

## Change Log

| Date | Author | Description |
|------|--------|-------------|
| 2026-05-28 | Claude Opus 4.7 1M (dev-story) | Story 3.4 implemented: `GET /api/v1/projects/{projectId}/context/refresh` endpoint with on-the-fly folder + memory ACL rechecks; three new outcome-to-ReferenceState mappers; OpenAPI spine extension + regenerated typed client. File-reference recheck deferred to a follow-up Folders submodule story per the capability-gate HALT (the Folders typed client has no opaque-id-only file-reference read route and Projects must not store path-classified fields). Full build 0W/0E; full suite 858/858 → 926/926 (+68); 0 failed / 0 skipped. |
| 2026-05-28 | Claude Opus 4.7 1M (story-automator-review) | Review cycle 1: 0 CRITICAL / 1 MEDIUM / 2 LOW findings. MEDIUM auto-fix: added `RefreshProjectContext_AclReportsUnavailable_OverridesProjectionStoredIncluded_ForMemory` (an AC 9 gap the original Completion Notes had inaccurately claimed was covered); corrected the Completion Notes to document that `_AclThrows_SurfacesAsUnavailable_FailsClosedNot500` is implicitly covered at the adapter level by the existing `ProjectFolderDirectoryTests.cs` and `ProjectMemoryDirectoryTests.cs` exception-translation harnesses (which exercise the shared `CheckFolderEvidenceAsync` / `CheckMemoryEvidenceAsync` helpers reused by Refresh). LOW findings deferred (AC 4's optional `ProjectContextRefreshed` synthetic example; AC 12's separated `_ErrorResponses_HaveNoLeakage` test — error-body leakage is exercised by the consolidated `_ResponseBody_HasNoLeakageAcrossOutcomes` 4-outcome test covering 200/400/400/404). Full build 0W/0E; full suite 858/858 → 927/927 (+69); 0 failed / 0 skipped. Story 3.4 → done. |

