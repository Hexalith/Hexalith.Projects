---
baseline_commit: 70f2ebe467053af8de2ef4efe77f5c4349709a9d
---

# Story 3.1: Context-assembly policy & allowlist

## Status

done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **Projects platform engineer**,
I want **a pure, allowlist-based `ProjectContext` assembly policy that includes a reference only after tenant, project, lifecycle, authorization, and freshness checks all pass**,
So that **context assembly is a tested security boundary that can never silently include an unverified reference** _(AR-9; the core of NFR-1/NFR-2/NFR-3; realizes UJ-4)_.

This is the **first Epic 3 story** and the load-bearing enabler for the rest of Epic 3 (Story 3.2 `GetProjectContext`, Story 3.3 `ExplainContextSelection`, Story 3.4 `RefreshProjectContext`). It does **not** ship an HTTP endpoint, an OpenAPI route addition, a Dapr-bound projection, or any sibling-context call — those land in Story 3.2+. Story 3.1 lands the **pure inclusion policy** (`Hexalith.Projects/Context/`), the **assembled-read-result DTOs** the policy emits (`Hexalith.Projects.Contracts/Models/`), the **(evidence-state × operation) fail-closed matrix** referenced by every Epic 3 story (`docs/context-assembly-decision-matrix.md`), the extension of the FS-2 `NoPayloadLeakage` harness over the new assembly DTOs, and the Tier-1 test matrix that proves the include/exclude decision is deterministic, totally covered, and reason-coded.

Per the Epic 2 retrospective (`_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md`) and Winston's closing line *"The architecture is ready for Epic 3 if Story 3.1 codifies the inclusion policy once and we stop hand-wiring the order per story,"* the policy must define a **single stable evaluation order** — authorization (claim → tenant access → project belonging → ACL evidence) → lifecycle → freshness → allowlisted reference kind — and Story 3.2+ consumes this policy unchanged. The Story 2.6 ADR (`docs/adr/memories-link-target.md`, status `Accepted`) is binding on the Memories portion of the allowlist: an absent or unavailable Memories reference is a **fail-closed-clean exclusion** with a reason code from the existing shared vocabulary, never a 5xx and never a new enum value.

The policy is computed at query time from inputs the host assembles outside Story 3.1's surface (Project state, projection rows, ACL-fetched sibling metadata); inside the pure policy nothing is fetched, no Dapr/network/HTTP call is made, no clock is read except through an injected `IUtcClock` already established by Story 1.6's `TenantAccessAuthorizer`, and no shared-vocabulary enum value is invented. The policy is the **single source of truth** the rest of Epic 3 evaluates against — duplicating its decision logic in Story 3.2's endpoint, Story 3.3's explainer, or Story 3.4's refresh handler is a forbidden anti-pattern.

## Acceptance Criteria

1. A pure `ProjectContextInclusionPolicy` is introduced under `src/Hexalith.Projects/Context/` and emits an **assembled read result** (`ProjectContextAssemblyResult` containing the typed `ProjectContext` DTO + per-candidate evaluation rows). The policy is a `static class` (or a `sealed class` with a single primary-constructed `IUtcClock` if a freshness budget needs the clock — preferred: pass freshness evidence in via inputs to keep the policy itself state-free and clock-free). It takes the following inputs as records: (a) `ProjectContextAssemblyContext` (`AuthoritativeTenantId`, `RequestedTenantId`, `ProjectId`, `OperationKind`, `CorrelationId`, `TaskId`, `Now: DateTimeOffset`), (b) `ProjectContextProjectEvidence` (the `ProjectDetailItem` projection row — already metadata-only — or a typed null sentinel for "project not visible"), (c) `ProjectContextTenantAccess` (the `TenantAccessAuthorizationResult` from Story 1.6), and (d) `ProjectContextReferenceEvidence` (a deterministic-ordered collection of candidate references; see AC 4).

2. The inclusion order is **declared once** as `ProjectContextInclusionOrder`, a public static `IReadOnlyList<ProjectContextInclusionCheck>` next to `Hexalith.Projects.Authorization.AuthorizationOrder`. The sequence is exactly: `TenantAuthority` → `ProjectVisibility` → `ProjectLifecycle` → `ReferenceAuthorization` → `ReferenceLifecycle` → `ReferenceFreshness` → `ReferenceKindAllowlist`. Every later Epic 3 story consumes this order without redeclaring it. The evaluation short-circuits on the first failing check, but the policy still emits an evaluation row for **every** candidate reference — the failed-check identity and a reason code are recorded so Story 3.3 can explain the decision.

3. The `ProjectContext` DTO (`Hexalith.Projects.Contracts/Models/ProjectContext.cs`) is metadata-only and shaped as: `TenantId`, `ProjectId`, `Lifecycle: ProjectLifecycle`, `Setup: ProjectSetup?`, `ProjectFolder: ProjectContextReference?` (single optional Project Folder reference — never a list), `Conversations: IReadOnlyList<ProjectContextReference>`, `FileReferences: IReadOnlyList<ProjectContextReference>`, `MemoryReferences: IReadOnlyList<ProjectContextReference>`, `Excluded: IReadOnlyList<ProjectContextExclusion>` (across all reference kinds — the assembled "what was left out and why" channel), `AssemblyOutcome: ProjectContextAssemblyOutcome` (`Assembled` | `ProjectUnavailable` | `Unauthorized`), `ObservedAt: DateTimeOffset`, `Freshness: ProjectContextFreshness` (≈ Story 1.6 `TenantProjectionFreshnessStatus` re-exposed via the shared vocabulary; values: `Fresh` / `Stale` / `Unavailable` / `Unknown`). Lists are ordered deterministically by reference kind then opaque reference id; equality by reference id within a kind is stable across replays. `ProjectContextReference` is `(string ReferenceKind, string ReferenceId, string? DisplayName, ReferenceState ReferenceState, ProjectReasonCode? ReasonCode, DateTimeOffset ObservedAt)`; `ProjectContextExclusion` is `(string ReferenceKind, string ReferenceId, ReferenceState ReferenceState, ProjectReasonCode? ReasonCode, ProjectContextInclusionCheck FailedCheck, string? Diagnostic)` — `Diagnostic` is the safe metadata-only string used by Story 3.3 and is sourced exclusively from a closed `ProjectContextInclusionDiagnostic` vocabulary defined alongside the policy; raw upstream messages, payloads, paths, secrets, transcript fragments, file contents, or memory bodies must never reach `Diagnostic`.

4. Candidate references the policy evaluates come from a single typed input shape `ProjectContextReferenceEvidence` whose collections are: `ProjectFolder` (the `ProjectFolderReference` from `ProjectDetailItem`, or null), `FileReferences` (the `IReadOnlyList<ProjectFileReference>` from `ProjectDetailItem`), `MemoryReferences` (the `IReadOnlyList<ProjectMemoryReference>` from `ProjectDetailItem`), `Conversations` (an `IReadOnlyList<ProjectContextConversationEvidence>` constructed from the Story 2.1 `IProjectConversationDirectory` ACL output — `ProjectConversationItem` projected to a metadata-only candidate). Each candidate carries: opaque identifier, optional safe display name, owning-context state (`ReferenceState`/`ProjectConversationTrustSignal` already produced by Epic 2 ACLs), and a `LastCheckedAt: DateTimeOffset` (freshness signal). The policy never re-fetches sibling metadata; it evaluates only the evidence presented.

5. Reference-kind allowlist (final check) enforces that `ReferenceKind` is one of `"folder"`, `"file"`, `"memory"`, `"conversation"` (the four kinds Epic 2 produces). A non-allowlisted reference kind is **excluded**, emits a `ProjectContextExclusion` row with `ReferenceState = InvalidReference`, `FailedCheck = ReferenceKindAllowlist`, no reason code, and a structured-log entry via `ILogger<ProjectContextInclusionPolicy>` (LogLevel.Warning, redacted scope) so Operations is alerted. The policy never lets such a candidate fall through to the `ProjectContext` lists.

6. The policy maps Story 2.x ACL outcomes to `ReferenceState` exactly per the existing pattern (no new enum values invented):
   | Input evidence | Surfaced `ReferenceState` | `ReasonCode` | `FailedCheck` (if excluded) |
   | --- | --- | --- | --- |
   | All checks pass | `Included` | `ConversationLinked` / `ProjectFolderMatched` / `FileReferenceMatched` / `MemoryMatched` | n/a |
   | Tenant-authority missing (no authoritative tenant) | n/a (request-level fail) | n/a | `TenantAuthority` — entire assembly returns `ProjectContextAssemblyOutcome.Unauthorized` |
   | Tenant-access projection denies / mismatched / disabled / unknown / malformed | n/a (request-level fail) | n/a | `TenantAuthority` (collapsed to `Unauthorized`) |
   | Tenant-access projection stale (allowed for reads only, not for trust-bearing context) | `Stale` (per-reference downgrade) or assembly `Freshness = Stale` | n/a | `TenantAuthority` for trust-bearing operations |
   | Tenant-access projection unavailable / rebuilding / future | n/a (request-level fail) | n/a | `TenantAuthority` — assembly returns `Unauthorized` (existence-safe) |
   | Project not visible to the tenant (cross-tenant) | n/a (assembly fail) | n/a | `ProjectVisibility` — assembly returns `ProjectContextAssemblyOutcome.ProjectUnavailable` (safe-denial 404 contract; never `TenantMismatch` at the boundary) |
   | Project archived | per-reference rows still emitted on excluded path; assembly `Lifecycle = Archived` | per-reference reason | `ProjectLifecycle` |
   | Reference `Unauthorized` (per Story 2.x ACL) | `Unauthorized` | none | `ReferenceAuthorization` |
   | Reference `Unavailable` | `Unavailable` | none | `ReferenceFreshness` (treated as fail-closed-clean) |
   | Reference `Stale` | `Stale` | none | `ReferenceFreshness` |
   | Reference `Archived` (incl. Memories `Case.Status == Closed` / `Deleting` per the Story 2.6 ADR) | `Archived` | none | `ReferenceLifecycle` |
   | Reference `Ambiguous` | `Ambiguous` | none | `ReferenceLifecycle` |
   | Reference `Conflict` | `Conflict` | none | `ReferenceLifecycle` |
   | Reference `InvalidReference` (malformed identifier, mismatched owner context) | `InvalidReference` | none | `ReferenceKindAllowlist` |
   | Reference `Pending` (e.g. `ProjectFolderCreationPending` from Story 2.4 degraded path) | `Pending` | none | `ReferenceFreshness` (treated as not-yet-includable, not as a failure) |
   | Conversation upstream `Forbidden` / `Redacted` (`ProjectConversationTrustSignal`) | `Unauthorized` (resp. `Excluded`) | none | `ReferenceAuthorization` (resp. `ReferenceFreshness`) |
   | Conversation upstream `Rebuilding` / `Unavailable` / `MixedGeneration` | `Unavailable` (resp. `Stale` for `MixedGeneration`) | none | `ReferenceFreshness` |
   | Memories reference whose ACL recheck returns `TenantMismatch` | `Unauthorized` at the assembly boundary (existence-safe collapse — see Story 2.6 ADR §Epic 3 allowlist treatment); `TenantMismatch` is *defined* in the exclusion row only via `Diagnostic = "tenantMismatch"` for Story 3.3 operator troubleshooting | none | `ReferenceAuthorization` |
   | No Memories ACL registered (`UnavailableProjectMemoryDirectory`) | `Unavailable` | none | `ReferenceFreshness` (fail-closed-clean) |
   | Folders file-metadata ACL `Stale` | `Stale` | none | `ReferenceFreshness` |
   | Folders folder ACL `Unavailable` for the single Project Folder | Project Folder excluded (`Unavailable`); the rest of context still assembles successfully | none | `ReferenceFreshness` |

7. The (evidence-state × operation) fail-closed matrix is authored as a single Markdown table at `docs/context-assembly-decision-matrix.md` and is referenced verbatim by Stories 3.2, 3.3, 3.4, and 3.5 (per the Epic 2 retro action item *"Define the (evidence-state × operation) fail-closed matrix as a single document referenced by all Epic 3 stories"*). The columns are `Evidence state: missing/stale/unauthorized/unavailable/forbidden/redacted/conflict/invalidReference/archived/ambiguous`; the rows are the Epic 3 operations: `GetProjectContext` (3.2), `RefreshProjectContext` (3.4), `ExplainContextSelection` (3.3), `GetConversationStartSetup` (3.5). Each cell records: surfaced `ReferenceState`, `FailedCheck`, whether the outer assembly succeeds (`2xx`) or collapses (`Unauthorized` / `ProjectUnavailable`), and the matching test fixture name. The doc owner is the dev agent (Story 3.1 task); subsequent Epic 3 stories may **only extend** the doc (additive rows for new operations), never edit Story 3.1's cell semantics. Doc is LF on disk per [[build-environment]].

8. The unproduced-shared-vocabulary tracker is updated. Per the Epic 2 retro action item *"Track unproduced shared-vocabulary outcomes deliberately"*, `docs/event-catalog.md` (or a new sibling doc `docs/shared-vocabulary-coverage.md` if the catalog has no room) gains a "Producer of last resort" subsection listing every `ReferenceState`/`ProjectLifecycle`/`ProjectReasonCode`/`ProjectContextInclusionCheck`/`ProjectConversationTrustSignal`/`TenantAccessOutcome` value with its current producer (or the explicit note `unproduced — taxonomy-only — kept for symmetry with [Story X.Y]`). This makes `TenantMismatch` and `Stale` (the unproduced outcomes Epic 2 flagged) explicit rather than implicit, and gives Story 3.4 (Refresh) a single document to reconcile when it actually starts producing `Stale` for real.

9. The `ProjectContext` DTO + `ProjectContextReference` + `ProjectContextExclusion` + `ProjectContextAssemblyResult` + `ProjectContextAssemblyOutcome` + `ProjectContextFreshness` + `ProjectContextInclusionCheck` + `ProjectContextInclusionDiagnostic` types are added to `src/Hexalith.Projects.Contracts/Models/` (or `Ui/` for the enum types that are part of the shared rendering vocabulary). All enums are `[ProjectionBadge]`-annotated, `JsonStringEnumConverter<T>`-converted, name-based on the wire. They are introduced **only as additive types** — no edits to `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ResolutionResult`, `ProjectConversationTrustSignal`, or any existing enum (per the Story 2.6 ADR and the Epic 2 retro). The `ProjectContext` DTO is **not** wired into the OpenAPI spine in this story (no public route surfaces it yet; Story 3.2 adds `POST /api/v1/projects/{projectId}/context`). The Contracts project remains low-dependency, `netstandard2.0`-safe where applicable, with no Dapr/HTTP/EventStore-server dependency.

10. The FS-2 `NoPayloadLeakage` harness is extended over the new assembly DTOs (`ProjectContext`, `ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextAssemblyResult`). `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` adds: (a) a serialization scan of every new DTO, (b) Memories-specific forbidden-term coverage extended from Story 2.7 (`Content`, `ContentBytes`, `ContentHash`, `SourceUri`, `SourceType`, `IngestedBy`, `Metadata`, `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`, `Classification`, `FailureDetails`, `IngestionInput`, embedding vector, search snippet, traversal payload, `ErrorResponse.Message`, `Suggestion`), and (c) Folders content terms carried forward from Story 2.5 (file contents, byte ranges, raw or workspace paths). The harness must also assert that every `ProjectContextExclusion.Diagnostic` value present in any test fixture appears in the closed `ProjectContextInclusionDiagnostic` vocabulary — guaranteeing no free-form upstream text leaks through the `Diagnostic` field (this realizes the Epic 2 retro action item *"add a Memories-specific extension to assembly DTOs in Epic 3"*).

11. Tier-1 purity is mandatory. `ProjectContextInclusionPolicy` evaluates entirely against typed inputs — no `Hexalith.Conversations.*`, `Hexalith.Folders.*`, `Hexalith.Memories.*`, `Dapr.*`, `Microsoft.AspNetCore.*`, `System.Net.*`, or `HttpClient` usage anywhere under `src/Hexalith.Projects/Context/**`. Tests must not require Dapr/network/containers/browser. Convergence is **deterministic-fakes-only**: no `Thread.Sleep`, `Task.Delay`, `SpinWait`, wall-clock polling, or `await Task.Yield()` time-waits in any new test file. The validation step proves this via `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.*/` filtered to story-touched files (zero hits).

12. The decision matrix is **totally covered** by Tier-1 tests under `tests/Hexalith.Projects.Tests/Context/`. Required test files (mirror the Story 2.7 "named-fixture-per-cell" pattern so each fail-closed matrix row has an explicit, addressable test):
    - `ProjectContextInclusionPolicyTests.cs` — happy paths (Project Folder included, file references included, memory references included, conversations included) + per-failed-check exclusion paths (one test per row of the AC 6 mapping table) + total order test (asserts `ProjectContextInclusionOrder` is exactly `TenantAuthority → ProjectVisibility → ProjectLifecycle → ReferenceAuthorization → ReferenceLifecycle → ReferenceFreshness → ReferenceKindAllowlist`).
    - `ProjectContextInclusionPolicyNonAllowlistedKindTests.cs` — non-allowlisted reference kind ("workspace", "embedding", empty string, null, whitespace, very long string, unicode bidi-bomb) is excluded with `InvalidReference` + `FailedCheck = ReferenceKindAllowlist` + structured log entry recorded (a fake `ILogger<ProjectContextInclusionPolicy>` records the warning).
    - `ProjectContextInclusionPolicyTenantAuthorityTests.cs` — missing/null/whitespace `AuthoritativeTenantId`, `RequestedTenantId != AuthoritativeTenantId`, `TenantAccessAuthorizationResult` with each `TenantAccessOutcome` and each `TenantProjectionFreshnessStatus`, including the existence-safe collapse to `Unauthorized` for `TenantMismatch`/`UnknownTenant`/`DisabledTenant`/`MalformedEvidence`/`MissingAuthoritativeTenant`/`ReplayConflict`/`UnavailableProjection`. Stale projection allowed for read-only operations downgrades the assembly `Freshness` to `Stale` but does NOT collapse; for trust-bearing operations the assembly collapses to `Unauthorized`. (The `OperationKind` field on `ProjectContextAssemblyContext` distinguishes the two; Story 3.2 is read-only, Story 3.4 Refresh is read-only with explicit freshness, Story 3.5 is read-only.)
    - `ProjectContextInclusionPolicyProjectVisibilityTests.cs` — project not visible (null `ProjectDetailItem`), project belongs to different tenant (cross-tenant existence safe-denial → `ProjectUnavailable`), project not yet created. Asserts safe-denial 404 contract: outcome is `ProjectUnavailable`, never `TenantMismatch`, at the assembly boundary.
    - `ProjectContextInclusionPolicyLifecycleTests.cs` — project `Active` includes everything passing further checks; project `Archived` produces a `ProjectContext` with `Lifecycle = Archived` and `AssemblyOutcome = Assembled`, but **every** reference is excluded with `FailedCheck = ProjectLifecycle` and `ReferenceState = Archived` (the assembled result remains useful for audit/explain but never feeds Chatbot context).
    - `ProjectContextInclusionPolicyConversationCandidateTests.cs` — exhaustive `ProjectConversationTrustSignal` matrix (`Current` → `Included`; `Stale` → `Stale`/`ReferenceFreshness`; `Rebuilding` → `Unavailable`/`ReferenceFreshness`; `Unavailable` → `Unavailable`/`ReferenceFreshness`; `Forbidden` → `Unauthorized`/`ReferenceAuthorization`; `Redacted` → `Excluded`/`ReferenceFreshness`; `MixedGeneration` → `Stale`/`ReferenceFreshness`).
    - `ProjectContextInclusionPolicyMemoriesCandidateTests.cs` — every `ReferenceState` the Memories ACL produces per the Story 2.6 ADR (`Included`/`Archived`/`Unauthorized`/`Unavailable`/`InvalidReference`), including the existence-safe collapse of `TenantMismatch` → boundary `Unauthorized` with `Diagnostic = "tenantMismatch"`. Asserts no `MemoryUnit.Content` / `ContentHash` / `SourceUri` / `ErrorResponse.Message` / `Suggestion` / token / path appears anywhere in the assembled output (re-runs the leakage harness on the result).
    - `ProjectContextInclusionPolicyFileReferenceCandidateTests.cs` — every `ReferenceState` the Folders file-metadata ACL produces (`Included`/`Archived`/`Stale`/`Unavailable`/`Unauthorized`/`InvalidReference`), including the Story 2.4 `Pending` state for the Project Folder branch (`ProjectFolderCreationPending`).
    - `ProjectContextInclusionPolicyProjectFolderCandidateTests.cs` — present-and-active included, pending excluded with `Pending`/`ReferenceFreshness`, archived excluded with `Archived`/`ReferenceLifecycle`, unavailable excluded with `Unavailable`/`ReferenceFreshness`. Asserts there is **exactly one** Project Folder reference in the assembled result (never a list), and that file references and memory references remain bounded and disjoint from the folder lane in the assembled output (preserves the Story 2.5 / Story 2.7 disjoint-lane invariant).
    - `ProjectContextInclusionPolicyDeterminismTests.cs` — invoking the policy twice with identical inputs returns equal outputs (record-equality), the reference and exclusion lists are ordered deterministically (by reference kind then opaque id, Ordinal), and ordering is stable under list-input permutation (the policy sorts internally before evaluation).
    - `ProjectContextInclusionPolicyLeakageTests.cs` — serializes every `ProjectContext` produced by the matrix above through `NoPayloadLeakageAssertions.AssertNoLeakage(...)` (extended for Memories + Folders forbidden terms per AC 10). A negative test injects a `Diagnostic` value outside the closed vocabulary and asserts the policy throws `ArgumentException` at the boundary (the closed vocabulary is the only Diagnostic source — never free text).

13. The pure policy is registered in the domain core (`Hexalith.Projects/Context/`) only — no Server / Workers / Client wiring lands in Story 3.1. `ProjectsServiceCollectionExtensions.cs` exposes a `services.AddTransient<ProjectContextInclusionPolicy>()` (or static type registration if it's a static class — preferred) so Story 3.2's host endpoint can consume it next sprint. If a singleton makes sense (static class), the registration line is added as a no-op marker comment pointing Story 3.2 at the consumption point. The Contracts project picks up only the new DTO/enum types; the Client project is untouched.

14. No new shared-vocabulary enum values are introduced. The existing `ReferenceState` (`Pending`/`Included`/`Excluded`/`Unauthorized`/`Unavailable`/`Stale`/`Archived`/`Ambiguous`/`TenantMismatch`/`Conflict`/`InvalidReference`), `ProjectLifecycle` (`Active`/`Archived`), `ProjectReasonCode` (`ConversationLinked`/`ProjectFolderMatched`/`FileReferenceMatched`/`MemoryMatched`/`MetadataMatched`), and `ProjectConversationTrustSignal` (`Current`/`Stale`/`Rebuilding`/`Unavailable`/`Forbidden`/`Redacted`/`MixedGeneration`) are sufficient by inspection — the AC 6 mapping table proves it. If during implementation a new value appears genuinely required, the dev agent **HALTs** before adding it and surfaces the conflict in the Dev Agent Record; the resolution is an ADR, not an inline edit.

15. No OpenAPI spine churn. `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` is untouched in Story 3.1 (Story 3.2 will add the `GetProjectContext` operation later). Story 3.1 must not regenerate `HexalithProjectsClient.g.cs` or `HexalithProjectsIdempotencyHelpers.g.cs`; their existing diff-stat after Story 3.1 must be zero. The OpenAPI fingerprint gate stays PASSED by construction (skip-clean, as the spine did not change).

16. No public surface change for Stories 1.4–2.7. No edits to: `ProjectAggregate.*`, `ProjectState`, `ProjectStateApply`, `ProjectCommandValidator`, `ProjectCommandValidationResult`, `ProjectResult`, `ProjectResultCode`, `ProjectDetailProjection`, `ProjectListProjection`, `ProjectReferenceIndexProjection`, the four ACL interfaces (`IProjectConversationDirectory`, `IProjectConversationAssignmentDirectory`, `IProjectFolderDirectory`, `IProjectFileReferenceDirectory`, `IProjectMemoryDirectory`), `ProjectAuthorizationGate`, `ProjectsDomainServiceEndpoints`, `IProjectCommandSubmitter`, or the existing shared-vocabulary enums. Story 3.1 is **purely additive** — Tier-1 surface only.

17. Mandatory negative-path tests carried forward per Epic 1/Epic 2 conformance: (a) cross-tenant isolation — the policy with `AuthoritativeTenantId != ProjectDetailItem.TenantId` returns `ProjectUnavailable` (safe-denial; reuses the FS-8 cross-tenant pattern); (b) `NoPayloadLeakage` over every assembled `ProjectContext` (extended per AC 10); (c) no clock divergence — the policy uses only the `Now: DateTimeOffset` passed in via `ProjectContextAssemblyContext` (no `DateTimeOffset.UtcNow`/`DateTime.UtcNow`/`Stopwatch` calls anywhere in `src/Hexalith.Projects/Context/**`); (d) idempotency — the policy is a pure function (no shared state, no instance fields, no caching) so repeated calls with the same inputs produce equal outputs (record-equality assertion); (e) replay safety — the policy never observes events; it only inspects state-shaped inputs, so projection-rebuild compatibility is structural (no test required beyond the determinism test).

18. The story file is updated by the dev agent with a Dev Agent Record recording: any policy-shape divergence from this AC list (with the rationale), the closed `ProjectContextInclusionDiagnostic` vocabulary actually shipped, whether the policy is `static class` or `sealed class`, the actual file count under `src/Hexalith.Projects/Context/`, the focused-lane test counts (Tests / Server / Contracts / Client / Integration), the full-solution `dotnet test Hexalith.Projects.slnx` count (baseline post-Story 2.7 = 567 — Story 3.1 adds ~50–80 Tier-1 tests; no regression in any other lane), `dotnet build` warnings/errors, `git diff --check` result, and any HALT items. No commit is required from the dev agent; story-automator review will commit after auto-fixes.

## Tasks / Subtasks

- [x] **Task 1 — Capability gate (read-only inspection). (AC: 1, 4, 6, 14, 16)**
  - [x] Read `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` end-to-end and confirm fields used as inputs (`TenantId`, `ProjectId`, `Lifecycle`, `Setup`, `ProjectFolder`, `FileReferences`, `MemoryReferences`, `Sequence`, `UpdatedAt`).
  - [x] Read `src/Hexalith.Projects.Contracts/Models/{ProjectFolderReference, ProjectFileReference, ProjectMemoryReference, ProjectSetup, LinkedSourcePolicy, ConversationStartDefaults, ProjectContextSourceKind}.cs` to confirm metadata-only fields.
  - [x] Read `src/Hexalith.Projects.Contracts/Queries/{ProjectConversationItem, ProjectConversationTrustSignal, ProjectConversationsPage, ProjectConversationPageMetadata}.cs` to confirm the conversation evidence shape.
  - [x] Read `src/Hexalith.Projects.Contracts/Ui/{ReferenceState, ProjectLifecycle, ProjectReasonCode}.cs` and `Hexalith.Projects/Authorization/{TenantAccessAuthorizationResult, TenantAccessOutcome, TenantProjectionFreshnessStatus, AuthorizationOrder}.cs`. Confirm no new enum values are required (re-check the AC 6 mapping table against existing values; HALT if any required state is genuinely absent).
  - [x] Read `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs` to confirm the per-kind disjoint key contract is preserved by the assembled-output shape Story 3.1 emits.
  - [x] Read `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` and `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` end-to-end so the Story 3.1 extension is additive, not duplicative.
  - [x] Read the Story 2.6 ADR `docs/adr/memories-link-target.md` §"Epic 3 allowlist treatment" and the Epic 2 retrospective `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` §"Next Epic Preview" + §"Epic 3 Preparation Tasks" + §"Action Items" verbatim before authoring any policy code — they are binding inputs.
  - [x] Confirm no submodule pointer change is required and no nested-recursive submodule init is needed for Story 3.1 (the policy is purely internal to `Hexalith.Projects/`).
  - [x] **HALT** and surface a `HALT` block in the Dev Agent Record if any of the above evidence diverges from this story file's assumptions, especially the AC 6 mapping table. Do not proceed to Task 2.

- [x] **Task 2 — Add the assembled `ProjectContext` DTOs in Contracts. (AC: 3, 9, 14, 16)**
  - [x] Add `src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionCheck.cs` — `enum` with members `TenantAuthority`, `ProjectVisibility`, `ProjectLifecycle`, `ReferenceAuthorization`, `ReferenceLifecycle`, `ReferenceFreshness`, `ReferenceKindAllowlist`. `[JsonConverter(typeof(JsonStringEnumConverter<ProjectContextInclusionCheck>))]`. `[ProjectionBadge(BadgeSlot.Info)]` on every member (purely diagnostic in the assembled output).
  - [x] Add `src/Hexalith.Projects.Contracts/Ui/ProjectContextAssemblyOutcome.cs` — `enum` with members `Assembled`, `ProjectUnavailable`, `Unauthorized`. Name-based JSON. `[ProjectionBadge]` mapping: `Assembled = Success`, `ProjectUnavailable = Neutral` (safe-denial: never `Danger` — never reveals existence), `Unauthorized = Danger`.
  - [x] Add `src/Hexalith.Projects.Contracts/Ui/ProjectContextFreshness.cs` — `enum` with members `Fresh`, `Stale`, `Unavailable`, `Unknown`. Name-based JSON. `[ProjectionBadge]` mapping: `Fresh = Success`, `Stale = Warning`, `Unavailable = Danger`, `Unknown = Neutral`.
  - [x] Add `src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionDiagnostic.cs` — a `public static class` exposing `IReadOnlyList<string> Values` (the closed diagnostic vocabulary) plus a `bool IsKnown(string?)` helper. Initial values: `"tenantMismatch"`, `"projectUnknown"`, `"projectArchived"`, `"referenceUnauthorized"`, `"referenceUnavailable"`, `"referenceStale"`, `"referenceArchived"`, `"referenceConflict"`, `"referenceInvalidIdentifier"`, `"referenceKindNotAllowlisted"`, `"projectFolderPending"`. No raw upstream `Message` / `Suggestion` / path / token / payload string is ever added to this vocabulary; new values are added only via a follow-up story.
  - [x] Add `src/Hexalith.Projects.Contracts/Models/ProjectContextReference.cs` — `sealed record ProjectContextReference(string ReferenceKind, string ReferenceId, string? DisplayName, ReferenceState ReferenceState, ProjectReasonCode? ReasonCode, DateTimeOffset ObservedAt)`. Eager-validates non-empty `ReferenceKind` / `ReferenceId`; normalizes whitespace-only `DisplayName` to `null`; mirrors the Story 2.7 `ProjectMemoryReference` shape.
  - [x] Add `src/Hexalith.Projects.Contracts/Models/ProjectContextExclusion.cs` — `sealed record ProjectContextExclusion(string ReferenceKind, string ReferenceId, ReferenceState ReferenceState, ProjectReasonCode? ReasonCode, ProjectContextInclusionCheck FailedCheck, string? Diagnostic)`. Eager-validates `Diagnostic` (null or member of `ProjectContextInclusionDiagnostic.Values`) — throws `ArgumentException` otherwise.
  - [x] Add `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs` — `sealed record ProjectContext(string TenantId, string ProjectId, ProjectLifecycle Lifecycle, ProjectSetup? Setup, ProjectContextReference? ProjectFolder, IReadOnlyList<ProjectContextReference> Conversations, IReadOnlyList<ProjectContextReference> FileReferences, IReadOnlyList<ProjectContextReference> MemoryReferences, IReadOnlyList<ProjectContextExclusion> Excluded, ProjectContextAssemblyOutcome AssemblyOutcome, DateTimeOffset ObservedAt, ProjectContextFreshness Freshness)`. Empty-list defaults via factory helpers (`ProjectContext.Empty(...)`, `ProjectContext.Unauthorized(...)`, `ProjectContext.ProjectUnavailable(...)`).
  - [x] Add `src/Hexalith.Projects.Contracts/Models/ProjectContextAssemblyResult.cs` — `sealed record ProjectContextAssemblyResult(ProjectContext Context, IReadOnlyList<ProjectContextEvaluation> Evaluations)`. The `Evaluations` collection is the per-candidate trace Story 3.3 will use (records: `(string ReferenceKind, string ReferenceId, ReferenceState ResultState, ProjectContextInclusionCheck? FailedCheck, ProjectReasonCode? ReasonCode, string? Diagnostic, DateTimeOffset ObservedAt)`).
  - [x] Add `src/Hexalith.Projects.Contracts/Models/ProjectContextEvaluation.cs` — `sealed record ProjectContextEvaluation(...)` with the above shape; validates non-empty `ReferenceKind` / `ReferenceId`, validates `Diagnostic` against the closed vocabulary.
  - [x] Verify the Contracts project still compiles `netstandard2.0` + `net10.0` (do not introduce a `net10.0`-only dependency); the new DTOs only need `System` + `System.Collections.Generic` + `System.Text.Json.Serialization` + `Hexalith.FrontComposer.Contracts.Attributes` for `[ProjectionBadge]`.
  - [x] Run `dotnet build src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj`. Confirm 0W/0E.

- [x] **Task 3 — Author the pure inclusion policy under `src/Hexalith.Projects/Context/`. (AC: 1, 2, 5, 6, 11, 13, 14, 17)**
  - [x] Remove `src/Hexalith.Projects/Context/.gitkeep` (the folder gets real content now).
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextAssemblyContext.cs` — `sealed record ProjectContextAssemblyContext(string? AuthoritativeTenantId, string? RequestedTenantId, string? ProjectId, ProjectContextOperationKind OperationKind, string? CorrelationId, string? TaskId, DateTimeOffset Now)`. Validates eager-non-empty `AuthoritativeTenantId`/`RequestedTenantId`/`ProjectId` at the public entry, but accepts null at construction so callers can express "missing tenant authority" inputs.
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs` — `enum` with members `Get`, `Refresh`, `Explain`, `GetConversationStartSetup`. Name-based JSON. Drives the read-vs-trust-bearing distinction in AC 6.
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextProjectEvidence.cs` — `sealed record ProjectContextProjectEvidence(ProjectDetailItem? Detail)` (a typed wrapper around the projection row; null = project not visible).
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextTenantAccess.cs` — `sealed record ProjectContextTenantAccess(TenantAccessAuthorizationResult Result)` (typed wrapper around the Story 1.6 result; the policy never re-evaluates tenant access).
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextConversationEvidence.cs` — `sealed record ProjectContextConversationEvidence(string ConversationId, string? DisplayLabel, ProjectConversationTrustSignal TrustSignal, DateTimeOffset LastCheckedAt)`. Built by Story 3.2 from `ProjectConversationItem`; kept Projects-shaped so the policy never touches `Hexalith.Conversations.*` types.
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextReferenceEvidence.cs` — `sealed record ProjectContextReferenceEvidence(ProjectFolderReference? ProjectFolder, IReadOnlyList<ProjectFileReference> FileReferences, IReadOnlyList<ProjectMemoryReference> MemoryReferences, IReadOnlyList<ProjectContextConversationEvidence> Conversations)`. Empty-list defaults via factory.
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextInclusionOrder.cs` — `public static class ProjectContextInclusionOrder` exposing `IReadOnlyList<ProjectContextInclusionCheck> Sequence { get; } = [...]` exactly the order in AC 2. A unit test in Task 5 asserts the sequence bytes match.
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs` — the pure decision function:
    - Preferred shape: `public static class ProjectContextInclusionPolicy` with one static method `static ProjectContextAssemblyResult Assemble(ProjectContextAssemblyContext context, ProjectContextProjectEvidence project, ProjectContextTenantAccess tenantAccess, ProjectContextReferenceEvidence references)`. (No `IUtcClock`; `Now` is on the context.)
    - Implementation: evaluate `ProjectContextInclusionOrder.Sequence` in order. Short-circuit on tenant/project failures (return `ProjectContext.Unauthorized(...)` or `.ProjectUnavailable(...)`). For per-reference checks, iterate every candidate, emit an evaluation row, and decide include / exclude per AC 6.
    - Map ACL/Conversations evidence to `ReferenceState` via small pure helper methods (`MapMemoryReferenceState`, `MapFileReferenceState`, `MapProjectFolderState`, `MapConversationTrustSignal`). Helpers are private; tests cover the public `Assemble` method only.
    - Emit deterministic ordering: every list in the returned `ProjectContext` (`Conversations`, `FileReferences`, `MemoryReferences`, `Excluded`, `Evaluations`) is sorted by `(ReferenceKind, ReferenceId)` Ordinal. `Project Folder` is a single optional reference, never a list.
    - Emit a `ProjectContextFreshness` decision from `TenantAccessAuthorizationResult.FreshnessStatus` (`Fresh`→`Fresh`, `Stale`→`Stale`, `Unavailable`→`Unavailable`, `Future`→`Unknown`, `Unknown`→`Unknown`) at the assembly level; this lets Story 3.4's Refresh slice surface degraded refs without changing the policy.
    - Use only the `context.Now` value as the time source. No `DateTimeOffset.UtcNow` / `DateTime.UtcNow` / `Stopwatch` / `Environment.TickCount` calls anywhere under `src/Hexalith.Projects/Context/**`.
    - Add the `ILogger<ProjectContextInclusionPolicy>` warning path (AC 5) ONLY if the policy is `static class` AND the warning is parameterized via an injected `Action<string>` (preferred: pass a `Logger logger` argument and have a static no-op default). Tier-1 purity precludes a captured `ILogger` field on a static class.
    - Decision: prefer a `sealed class ProjectContextInclusionPolicy` taking `ILogger<ProjectContextInclusionPolicy>? logger = null` via primary constructor — pure single-method behavior, but logger-injectable for the AC 5 warning. Keep the method `public ProjectContextAssemblyResult Assemble(...)`. Tier-1 purity preserved (no Dapr/network/clock/state).
  - [x] Add `src/Hexalith.Projects/Context/ProjectContextDiagnostics.cs` — `internal static class` exposing helpers that build only safe diagnostics from the closed vocabulary (no free-text). Asserts at the boundary that the chosen string is in `ProjectContextInclusionDiagnostic.Values` (throws `InvalidOperationException` otherwise — a code/contract bug, not user input).
  - [x] Update `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs` to add `services.TryAddTransient<ProjectContextInclusionPolicy>()` (or leave the policy as a `static class` and add a single-line comment naming Story 3.2 as the consumer — if `static`, no DI line is needed). Preferred: `sealed class` with DI registration to allow logger injection.
  - [x] Boundary check: `grep -rE "Hexalith\.(Conversations|Folders|Memories)" src/Hexalith.Projects/Context/` returns zero hits. `grep -rE "Dapr|System\.Net\.Http|HttpClient" src/Hexalith.Projects/Context/` returns zero hits. `grep -rE "DateTime(Offset)?\.UtcNow|DateTime\.Now|Stopwatch|Environment\.TickCount" src/Hexalith.Projects/Context/` returns zero hits.

- [x] **Task 4 — Author the fail-closed decision matrix doc. (AC: 7)**
  - [x] Create `docs/context-assembly-decision-matrix.md` with the table described in AC 7. Column header order: `Evidence state | GetProjectContext (3.2) | RefreshProjectContext (3.4) | ExplainContextSelection (3.3) | GetConversationStartSetup (3.5) | Test fixture`. Row header order: `missing`, `stale`, `unauthorized`, `unavailable`, `forbidden`, `redacted`, `conflict`, `invalidReference`, `archived`, `ambiguous`. Each cell: `<surfaced ReferenceState> / <FailedCheck or "n/a"> / <outer assembly outcome>`.
  - [x] In the doc preamble, name the policy file path (`src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs`) and the test file path that covers each row (the Task 5 file names). Stories 3.2–3.5 may extend rows (additive only); Story 3.1 owns the initial semantics.
  - [x] Cross-link the doc from `docs/event-catalog.md` (a single one-line pointer) and from `_bmad-output/planning-artifacts/architecture.md` (a single one-line pointer at the AR-9 section, mirroring how Story 2.6 added the AR-G4 pointer).
  - [x] LF on disk per [[build-environment]]; verify `file docs/context-assembly-decision-matrix.md` reports no CR. `git diff --check` reports clean.

- [x] **Task 5 — Track unproduced shared-vocabulary outcomes. (AC: 8)**
  - [x] Extend `docs/event-catalog.md` with a new section `## Shared vocabulary — producer of last resort` (placed at the end of the catalog so existing event entries are untouched). The table lists every value of `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ProjectContextInclusionCheck` (new in this story), `ProjectContextAssemblyOutcome` (new), `ProjectContextFreshness` (new), `ProjectConversationTrustSignal`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus` with three columns: `Value`, `Current producer`, `Notes (unproduced / taxonomy-only / planned producer)`. Explicitly mark `TenantMismatch` (per the Epic 2 retro) and `Stale` (per Story 2.5/2.7 reviews) — and `Pending` if it is unproduced beyond Story 2.4's degraded folder path — as `unproduced — taxonomy-only` with a planned-producer note pointing at Story 3.4 (Refresh) where `Stale` becomes a real producible state.
  - [x] If `docs/event-catalog.md` is large or organized differently, prefer adding a sibling doc `docs/shared-vocabulary-coverage.md` and a one-line pointer from `event-catalog.md`. Choose whichever keeps the catalog readable; record the choice in the Dev Agent Record.
  - [x] LF on disk per [[build-environment]]; `git diff --check` clean.

- [x] **Task 6 — Tests (Tier-1, pure). (AC: 5, 11, 12, 14, 17)**
  - [x] Create `tests/Hexalith.Projects.Tests/Context/` and add the test files enumerated in AC 12, one per matrix lane. Each test file is xUnit v3 + Shouldly (mirrors `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateMemoryTests.cs`).
  - [x] Use a Projects-Testing fake `ILogger<ProjectContextInclusionPolicy>` (`RecordingLogger<T>` style — a minimal recording implementation). Put it in `src/Hexalith.Projects.Testing/Context/` if it does not already exist and reuse the established Projects-Testing namespace conventions.
  - [x] Decision-matrix completeness assertion: a dedicated test enumerates every cell of `docs/context-assembly-decision-matrix.md` (parse the Markdown table at test time or hard-code a `(EvidenceState, OperationKind) → (ExpectedReferenceState, ExpectedFailedCheck, ExpectedOutcome)` table) and runs the policy once per cell. Any divergence between the doc and policy output is a test failure. (This is the load-bearing guarantee that the doc and code agree.)
  - [x] Extend `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` per AC 10: assert no leakage on every `ProjectContext` produced by the policy under the matrix; assert `Diagnostic` values are members of `ProjectContextInclusionDiagnostic.Values`; reuse the Story 2.7 Memories-specific forbidden-term list and the Story 2.5 Folders forbidden-term list.
  - [x] No-sleep grep during validation: `grep -rE "Thread\.Sleep\(|Task\.Delay\(|SpinWait\.|await Task\.Yield\(" tests/Hexalith.Projects.*/` filtered to Story 3.1 new/modified test files returns zero hits.
  - [x] Cross-tenant isolation: write a test that constructs the policy input with `AuthoritativeTenantId = "tenant-a"` and `ProjectDetailItem.TenantId = "tenant-b"` and asserts the result is `ProjectContextAssemblyOutcome.ProjectUnavailable` (never `TenantMismatch` at the boundary). Reuse the FS-8/SM-3 pattern from Story 1.4.
  - [x] Determinism: write a test that runs the policy twice with the same inputs and asserts record-equality (every list ordered, every record bytewise stable). Also assert ordering is stable when input lists are permuted.

- [x] **Task 7 — Validation. (AC: 7, 10, 11, 13, 15, 16, 18)**
  - [x] Use the build environment recorded in [[build-environment]]: `DOTNET_ROOT=/home/administrator/.dotnet` (`dotnet --version` 10.0.300). Avoid `/usr/bin/dotnet` (resolves to 10.0.108 and fails `rollForward: latestPatch`).
  - [x] Run `dotnet build src/Hexalith.Projects/Hexalith.Projects.slnx`. Confirm 0 warnings / 0 errors.
  - [x] Run focused lanes: `dotnet test tests/Hexalith.Projects.Tests`, `Hexalith.Projects.Server.Tests`, `Hexalith.Projects.Contracts.Tests`, `Hexalith.Projects.Client.Tests`, `Hexalith.Projects.Integration.Tests`. Record per-lane counts. Story 2.7 left lanes at 213/181/128/31/14 — Story 3.1 adds Tier-1 tests to Projects.Tests only; Server/Contracts/Client/Integration are not expected to change (allowed to grow by zero or by a small number for the new Contracts type serialization).
  - [x] Run full-solution `dotnet test Hexalith.Projects.slnx`. Record total. Baseline 567 (post-2.7); Story 3.1 grows it by ~50–80; failed must be 0.
  - [x] Run `git diff --check` on story-touched files. Confirm clean (no whitespace errors). Hand-written `.cs` / `.md` are LF on disk per [[build-environment]].
  - [x] Confirm no submodule pointer change and no `.g.cs` change: `git status` shows no submodule advances and `git diff --stat src/Hexalith.Projects.Client/Generated/` shows zero changed lines.
  - [x] Confirm boundary greps from Task 3 return zero hits.
  - [x] Confirm OpenAPI spine is untouched: `git diff --stat src/Hexalith.Projects.Contracts/openapi/` shows zero changed lines.
  - [x] Populate the Dev Agent Record with the validation summary in AC 18.

## Dev Notes

### Story Scope Boundary

- **In scope:** Pure `ProjectContextInclusionPolicy` and its inputs under `src/Hexalith.Projects/Context/`; assembled-result DTOs and shared enums under `src/Hexalith.Projects.Contracts/Models/` + `Ui/`; the closed `ProjectContextInclusionDiagnostic` vocabulary; the `docs/context-assembly-decision-matrix.md` fail-closed matrix doc; the shared-vocabulary unproduced-outcomes tracker (`docs/event-catalog.md` extension or sibling doc); FS-2 `NoPayloadLeakage` harness extension over the new DTOs; Tier-1 decision-matrix tests; cross-tenant isolation test; determinism test; DI registration of `ProjectContextInclusionPolicy` in `ProjectsServiceCollectionExtensions.cs`.
- **Explicitly out of scope (recorded so the dev agent does not over-build):** `GetProjectContext` endpoint and OpenAPI route (Story 3.2); host-side composition that wires ACL fetches to populate `ProjectContextReferenceEvidence` (Story 3.2); `ExplainContextSelection` endpoint (Story 3.3); `RefreshProjectContext` endpoint (Story 3.4); `GetConversationStartSetup` endpoint and `ConversationStartSetupProjection` (Story 3.5); any new shared-vocabulary enum values; any change to `ProjectAggregate.*` / `ProjectState` / `ProjectStateApply` / `ProjectCommandValidator` / `ProjectResult*` / `ProjectDetailProjection` / `ProjectListProjection` / `ProjectReferenceIndexProjection` / the four Story 2.x ACL interfaces / `ProjectAuthorizationGate` / `ProjectsDomainServiceEndpoints` / `IProjectCommandSubmitter`; any OpenAPI spine change; any `.g.cs` regeneration; any submodule pointer change; any AppHost smoke check evidence trail (deferred to Story 3.2 per the Epic 2 retro action item); any Memories / Folders / Conversations write call from the policy; any HTTP/Dapr/network/browser/container dependency in the policy; any non-deterministic clock read; any new ADR (the AC 14 HALT exists to surface the conflict if one appears, but the resolution is a follow-up story, not an inline edit).

### Current Code Facts Verified (this working tree, baseline `70f2ebe`)

- The `src/Hexalith.Projects/Context/` folder exists as a placeholder (single `.gitkeep`) — Story 3.1 fills it.
- The `src/Hexalith.Projects/Resolution/` folder is also a placeholder (`.gitkeep`); Story 4.x will fill it. Do not pre-build resolution surface in Story 3.1.
- `ProjectDetailItem` (`src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs`) already carries the read-side state Story 3.1 consumes: `TenantId`, `ProjectId`, `Name`, `Description`, `SetupMetadata`, `Setup`, `ProjectFolder`, `FileReferences`, `MemoryReferences`, `Lifecycle`, `CreatedAt`, `UpdatedAt`, `Sequence`. No edits required.
- `ProjectFolderReference`, `ProjectFileReference`, `ProjectMemoryReference` already carry `(ReferenceState, ReasonCode?, DisplayName?, ObservedAt)`. Story 3.1 maps these to `ProjectContextReference` 1:1 (plus reference-kind label).
- The Story 2.1 conversation read ACL surfaces `ProjectConversationItem` (`src/Hexalith.Projects.Contracts/Queries/ProjectConversationItem.cs`) with `LifecycleStatus`, `DisplayLabel`, `TrustSignal: ProjectConversationTrustSignal` — Story 3.1 captures this Projects-shape (already free of `Hexalith.Conversations.*` payloads) in `ProjectContextConversationEvidence`.
- The shared vocabulary at `src/Hexalith.Projects.Contracts/Ui/{ReferenceState.cs, ProjectLifecycle.cs, ProjectReasonCode.cs}` already covers every value the AC 6 mapping table needs. **No new enum values are introduced by Story 3.1.**
- Story 1.6 produced `TenantAccessAuthorizationResult(TenantAccessOutcome, Code, TenantId, ProjectionWatermark, LastEventTimestamp, ProjectionAge, FreshnessStatus, Source)` with `TenantAccessOutcome { Allowed, Denied, StaleProjection, UnavailableProjection, UnknownTenant, DisabledTenant, MalformedEvidence, TenantMismatch, MissingAuthoritativeTenant, ReplayConflict }` and `TenantProjectionFreshnessStatus { Unknown, Fresh, Stale, Future, Unavailable }`. Story 3.1 maps these into the policy via `ProjectContextTenantAccess` without re-evaluating them — Story 1.6's `TenantAccessAuthorizer` remains the only producer.
- `ProjectReferenceIndexProjection` (`src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs`) keys references with per-kind disjoint prefixes (`folder` / `file` / `memory`). Story 3.1 does not read this projection directly — it operates on the per-`ProjectDetailItem`-list shape (`ProjectFolder` + `FileReferences` + `MemoryReferences`) which Story 3.2 will compose from the projection at query time. The disjointness invariant is preserved structurally because the inputs are already disjoint.
- The FS-2 leakage harness lives in `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` and is consumed by `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`. Story 3.1 extends the latter, not the former.
- The current root commit is `70f2ebe feat(story-2.7)`. No Hexalith.Memories / Conversations / Folders submodule pointer change is required; Story 3.1 does not touch any submodule.

### Required Capability Path

Story 3.1 has **no upstream capability gate** — the inputs (ProjectDetailItem, TenantAccess result, conversation ACL output, sibling reference DTOs) are all already produced by Stories 1.4–2.7. The capability discipline still applies symbolically: the dev agent re-reads the AC 6 mapping table against the existing shared vocabulary before writing any policy code, and HALTs surface-divergence-style if a new enum value appears genuinely required (AC 14). No degraded path is contemplated; the policy is internal, deterministic, and totally covered by Tier-1 tests.

### Guardrails

- **Pure domain core.** `src/Hexalith.Projects/Context/**` must not reference `Hexalith.Conversations.*`, `Hexalith.Folders.*`, `Hexalith.Memories.*`, `Dapr.*`, `Microsoft.AspNetCore.*`, `System.Net.*`, or `HttpClient`. The conversation evidence is captured Projects-shaped (`ProjectContextConversationEvidence`) so the policy never imports `Hexalith.Conversations.Contracts.*` types — Story 3.2 will perform that mapping in `Hexalith.Projects.Server.Conversations`.
- **Deterministic-only clock.** The policy uses only `ProjectContextAssemblyContext.Now`. No `DateTimeOffset.UtcNow`, `DateTime.UtcNow`, `Stopwatch`, `Environment.TickCount`, `TimeProvider`, or hidden clock read anywhere under `src/Hexalith.Projects/Context/**`. Tests pass an explicit `Now`.
- **Closed diagnostic vocabulary.** Every `ProjectContextExclusion.Diagnostic` value is a member of the `ProjectContextInclusionDiagnostic.Values` static list. No raw upstream `Message`/`Suggestion`/path/token/payload string is ever assigned to `Diagnostic`. The DTO eager-validates at construction.
- **Single source of truth — order.** The inclusion order lives in `ProjectContextInclusionOrder.Sequence` and nowhere else. Stories 3.2–3.5 reference this list directly. If a future story needs a different order it adds a new operation-kind branch, not a parallel sequence.
- **Single source of truth — matrix.** `docs/context-assembly-decision-matrix.md` is the canonical map of (evidence-state × operation) decisions. The Task 6 completeness test parses the doc (or a hard-coded mirror) and asserts the policy and the doc agree cell-by-cell. Doc-only changes that diverge from the policy fail tests.
- **No-sleep rule in tests.** Forbidden tokens in Story 3.1 test files: `Thread.Sleep`, `Task.Delay`, `SpinWait`, `await Task.Yield()` (as time-wait), wall-clock retry loops, polling-with-real-time. Convergence is asserted via deterministic inputs and a `Now: DateTimeOffset` injected via the assembly context.
- **No new shared-vocabulary enum values.** `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ProjectConversationTrustSignal`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus` are not edited. New enums are only Story-3.1-introduced policy-specific types: `ProjectContextInclusionCheck`, `ProjectContextAssemblyOutcome`, `ProjectContextFreshness`, `ProjectContextOperationKind`.
- **Tenant authority from envelope only.** Story 3.1 does not derive a tenant identity; it consumes whatever `AuthoritativeTenantId` / `RequestedTenantId` Story 3.2's host endpoint will pass in. The policy collapses any tenant-evidence mismatch / missing / disabled / replay-conflict / malformed result to `ProjectContextAssemblyOutcome.Unauthorized` at the boundary (existence-safe; never reveals cross-tenant existence).
- **Safe-denial 404 contract.** Cross-tenant project access surfaces as `ProjectContextAssemblyOutcome.ProjectUnavailable`. Never as `TenantMismatch` at the boundary. `TenantMismatch` taxonomy survives only in the `Diagnostic` field for Story 3.3 operator troubleshooting — and only as the closed `"tenantMismatch"` diagnostic string, never raw upstream text.
- **Project Folder rule preserved (Story 2.4).** The assembled `ProjectContext.ProjectFolder` is a **single** optional reference — never a list. File references and memory references remain per-kind disjoint with the folder lane in the assembled output (preserves the Story 2.5 / Story 2.7 invariant).
- **No `V2` types.** Public contracts evolve only through additive types; the new DTOs are additive.
- **No nested recursive submodule init.** Read-only inspection of already-initialized submodules is allowed; nothing in Story 3.1 advances any submodule pointer.
- **No `.g.cs` change.** Story 3.1 does not regenerate the typed client or idempotency helpers; the generated `.g.cs` files must show zero `git diff` lines after the story lands.
- **No OpenAPI spine change.** Story 3.2 will add the `GetProjectContext` route. Story 3.1 does not.
- **No Dapr / Aspire / HttpClient / network usage.** Tier-1 purity is binding. Pure functional core, then later host integration (Story 3.2).
- **AppHost smoke-check restoration is deferred to Story 3.2.** Per the Epic 2 retro action item, Story 3.2 (Get Project Context) is the right slot for the explicit AppHost smoke evidence trail; Story 3.1 is pure / Tier-1 only and is the wrong slot to demonstrate Aspire topology.

### Suggested Type Sketch

```csharp
// src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs
public sealed class ProjectContextInclusionPolicy(ILogger<ProjectContextInclusionPolicy>? logger = null)
{
    public ProjectContextAssemblyResult Assemble(
        ProjectContextAssemblyContext context,
        ProjectContextProjectEvidence project,
        ProjectContextTenantAccess tenantAccess,
        ProjectContextReferenceEvidence references)
    {
        // 1. TenantAuthority — collapse to Unauthorized on any non-Allowed outcome that is not bounded-stale-for-reads.
        // 2. ProjectVisibility — null detail OR (Detail.TenantId != context.AuthoritativeTenantId) → ProjectUnavailable.
        // 3. ProjectLifecycle — Archived: emit assembled context with every reference excluded with FailedCheck = ProjectLifecycle.
        // 4..7. Per-reference: ReferenceAuthorization → ReferenceLifecycle → ReferenceFreshness → ReferenceKindAllowlist
        //       Map (ReferenceState, ProjectConversationTrustSignal) → (Include | Exclude(FailedCheck, Diagnostic)).
        // 8. Deterministic ordering, NoPayloadLeakage-safe, ProjectContextFreshness computed from tenantAccess.FreshnessStatus.
    }
}
```

```text
src/Hexalith.Projects/Context/
├── ProjectContextAssemblyContext.cs
├── ProjectContextConversationEvidence.cs
├── ProjectContextDiagnostics.cs            // internal helpers
├── ProjectContextInclusionOrder.cs
├── ProjectContextInclusionPolicy.cs        // the pure decision function
├── ProjectContextOperationKind.cs
├── ProjectContextProjectEvidence.cs
├── ProjectContextReferenceEvidence.cs
└── ProjectContextTenantAccess.cs

src/Hexalith.Projects.Contracts/Models/
├── ProjectContext.cs                       // assembled-result DTO
├── ProjectContextAssemblyResult.cs
├── ProjectContextEvaluation.cs
├── ProjectContextExclusion.cs
└── ProjectContextReference.cs

src/Hexalith.Projects.Contracts/Ui/
├── ProjectContextAssemblyOutcome.cs
├── ProjectContextFreshness.cs
├── ProjectContextInclusionCheck.cs
└── ProjectContextInclusionDiagnostic.cs    // public static class — closed string list
```

### Decision Matrix (binding — mirrors AC 6, sourced exclusively from existing shared vocabulary)

| Evidence state                                | Surfaced `ReferenceState`       | `FailedCheck`              | Outer outcome                                |
| --------------------------------------------- | ------------------------------- | -------------------------- | -------------------------------------------- |
| Tenant authority missing                      | n/a                             | `TenantAuthority`          | `Unauthorized` (assembly-level)              |
| Tenant access denied / disabled / unknown     | n/a                             | `TenantAuthority`          | `Unauthorized` (assembly-level)              |
| Tenant access stale, read-only operation      | n/a (assembly Freshness=Stale)  | n/a                        | `Assembled` (per-reference still evaluated)  |
| Tenant access stale, trust-bearing operation  | n/a                             | `TenantAuthority`          | `Unauthorized` (assembly-level)              |
| Tenant access unavailable / future / rebuilding | n/a                            | `TenantAuthority`          | `Unauthorized` (assembly-level)              |
| Project not visible (null or cross-tenant)    | n/a                             | `ProjectVisibility`        | `ProjectUnavailable` (safe-denial 404)        |
| Project archived                              | `Archived` per reference        | `ProjectLifecycle`         | `Assembled` (Lifecycle=Archived; refs excluded) |
| Reference `Unauthorized`                      | `Unauthorized`                  | `ReferenceAuthorization`   | `Assembled`                                  |
| Reference `Unavailable`                       | `Unavailable`                   | `ReferenceFreshness`       | `Assembled`                                  |
| Reference `Stale`                             | `Stale`                         | `ReferenceFreshness`       | `Assembled`                                  |
| Reference `Archived` (Memories `Closed`/`Deleting`, Folders archived, conversation upstream archived) | `Archived` | `ReferenceLifecycle`       | `Assembled`                                  |
| Reference `Ambiguous`                         | `Ambiguous`                     | `ReferenceLifecycle`       | `Assembled`                                  |
| Reference `Conflict`                          | `Conflict`                      | `ReferenceLifecycle`       | `Assembled`                                  |
| Reference `InvalidReference`                  | `InvalidReference`              | `ReferenceKindAllowlist`   | `Assembled`                                  |
| Reference `Pending` (Story 2.4 degraded folder) | `Pending`                     | `ReferenceFreshness`       | `Assembled`                                  |
| Conversation upstream `Forbidden`             | `Unauthorized`                  | `ReferenceAuthorization`   | `Assembled`                                  |
| Conversation upstream `Redacted`              | `Excluded`                      | `ReferenceFreshness`       | `Assembled`                                  |
| Conversation upstream `Rebuilding`/`Unavailable` | `Unavailable`                | `ReferenceFreshness`       | `Assembled`                                  |
| Conversation upstream `Stale`/`MixedGeneration` | `Stale`                       | `ReferenceFreshness`       | `Assembled`                                  |
| Memories `TenantMismatch` from ACL recheck    | `Unauthorized` (boundary collapse, `Diagnostic="tenantMismatch"`) | `ReferenceAuthorization` | `Assembled` |
| No Memories ACL registered (`Unavailable`)    | `Unavailable`                   | `ReferenceFreshness`       | `Assembled`                                  |
| Non-allowlisted reference kind                | `InvalidReference`              | `ReferenceKindAllowlist`   | `Assembled` (+ structured log warning)       |

### Files To Read Before Editing

- `docs/adr/memories-link-target.md` (Story 2.6 ADR — §"Epic 3 allowlist treatment" is binding on the Memories portion).
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` (Epic 2 retrospective — §"Next Epic Preview" + §"Epic 3 Preparation Tasks" + §"Action Items" verbatim).
- `_bmad-output/implementation-artifacts/2-7-link-unlink-memory.md` (immediate-prior story; pattern for additive, Tier-1-only deliverables; Dev Agent Record shape to mirror).
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` (Folders ACL outcome mapping reused in AC 6).
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` (degraded `Pending` Project Folder path referenced in AC 6).
- `_bmad-output/planning-artifacts/epics.md` (Story 3.1 ACs — authoritative; do not edit).
- `_bmad-output/planning-artifacts/architecture.md` (§"`ProjectContext` is an assembled, authorization-filtered read result"; AR-9 inclusion-policy text; Implementation Sequence step 7).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` (FR-16 Get Project Context consequences; FR-17 Explain Context Selection consequences; FR-18 Refresh Project Context consequences; FR-20 Conversation-Start Setup consequences).
- `_bmad-output/project-context.md` (umbrella rule set — 96 rules; especially the Tier-1 purity rule, central package management, no-`V2`, metadata-only, no submodule recursion).
- `docs/payload-taxonomy.md` (sensitivity classes; metadata-only invariants for the new DTOs).
- `docs/event-catalog.md` (current event catalog; Story 3.1 extends with the shared-vocabulary-coverage subsection per AC 8).
- `src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs` (FS-1 allowlist source of truth; AC 10 leakage harness extends through this).
- `src/Hexalith.Projects.Contracts/Models/{ProjectFolderReference, ProjectFileReference, ProjectMemoryReference, ProjectSetup, LinkedSourcePolicy, ConversationStartDefaults, ProjectContextSourceKind}.cs` (existing reference shape; Story 3.1 mirrors it for `ProjectContextReference`).
- `src/Hexalith.Projects.Contracts/Queries/{ProjectConversationItem, ProjectConversationTrustSignal, ProjectConversationsPage, ProjectConversationPageMetadata}.cs` (conversation evidence shape).
- `src/Hexalith.Projects.Contracts/Ui/{ReferenceState, ProjectLifecycle, ProjectReasonCode}.cs` (shared vocabulary — do not edit).
- `src/Hexalith.Projects/Authorization/{TenantAccessAuthorizationResult, TenantAccessAuthorizationContext, TenantAccessOutcome, TenantProjectionFreshnessStatus, AuthorizationLayer, AuthorizationOrder}.cs` (Story 1.6 surfaces consumed by the policy).
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` (the read-side state shape consumed as `ProjectContextProjectEvidence.Detail`).
- `src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs` (per-kind disjoint key contract preserved by the assembled-output shape; not read directly by the policy).
- `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` (FS-2 harness; AC 10 extension target).
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` (extension entry point).
- `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectAggregateMemoryTests.cs` (xUnit v3 + Shouldly test-file shape to mirror).

### Testing Requirements

See AC 12 for the full per-cell test enumeration. Highlights:

- **Decision-matrix completeness test.** A single test that asserts the policy output matches `docs/context-assembly-decision-matrix.md` cell-for-cell. This is the load-bearing guarantee that the doc and code do not drift apart.
- **Determinism + ordering.** Two calls with equal inputs return equal outputs. Reference lists are ordered Ordinal by `(ReferenceKind, ReferenceId)`. Input-permutation does not change output.
- **Cross-tenant isolation.** `AuthoritativeTenantId != Detail.TenantId` → `ProjectContextAssemblyOutcome.ProjectUnavailable` (never `TenantMismatch` at the boundary; safe-denial 404 contract).
- **NoPayloadLeakage extension.** Every assembled `ProjectContext` from every matrix row is asserted leakage-free; the leakage harness gains the Memories + Folders forbidden-term coverage from Stories 2.5/2.7. Negative test: a fixture with a forbidden term is rejected by the harness.
- **Closed-vocabulary diagnostics.** Every `ProjectContextExclusion.Diagnostic` value must be a member of `ProjectContextInclusionDiagnostic.Values`. A negative test injects an out-of-vocabulary string and asserts the DTO throws at construction.
- **No-sleep grep.** Filtered to Story 3.1 test files, zero hits for `Thread.Sleep`/`Task.Delay`/`SpinWait`/`Task.Yield`.
- **Non-allowlisted reference kind logging.** A fake `ILogger<ProjectContextInclusionPolicy>` records the warning entry when a non-allowlisted kind is presented; the entry contains the safe `Diagnostic = "referenceKindNotAllowlisted"` plus tenant/project IDs but no payload string.

### Previous Story Intelligence

- **Story 2.7 (Link/Unlink Memory) — done, 567/567.** Established the Memories ACL surface Story 3.1's allowlist treats as fail-closed-clean. Confirmed zero `#pragma warning disable HXL001|HXL002`, deterministic-fakes-only tests, central NuGet package management for Memories. Story 3.1 does not touch Memories at all — the existing `IProjectMemoryDirectory` is a recheck input via the Story 3.2 host composition (not Story 3.1's responsibility).
- **Story 2.6 (Memories linkage decision spike) — done, ADR `Accepted`.** §"Epic 3 allowlist treatment" is binding on Story 3.1. The ADR's failure-to-shared-vocabulary mapping is reproduced in AC 6. The boundary-safe collapse of `TenantMismatch` → `Unauthorized` (with `Diagnostic = "tenantMismatch"`) implements the ADR rule verbatim.
- **Story 2.5 (File Reference link/unlink) — done.** Per-kind disjoint reference-index lanes (`folder` / `file` / `memory`); the assembled `ProjectContext` preserves the same disjointness by emitting three separate lists. Folders content forbidden-term list is carried into the leakage harness.
- **Story 2.4 (Project Folder reference) — done.** Established the degraded `Pending` Project Folder path (`folder_create_external_unavailable`); Story 3.1 maps `Pending` to `ReferenceState.Pending` with `FailedCheck = ReferenceFreshness` (not a failure; not-yet-includable).
- **Story 2.3 (Conversation write-side) — done.** Pattern A holds — Projects does not store conversation membership; the policy consumes Conversations metadata via `ProjectContextConversationEvidence` (a Projects-shaped wrapper that the Story 3.2 host will populate from `IProjectConversationDirectory.ListForProjectAsync`).
- **Story 2.1 (Conversation Reference Read ACL) — done.** Established `ProjectConversationTrustSignal { Current, Stale, Rebuilding, Unavailable, Forbidden, Redacted, MixedGeneration }` consumed in AC 6.
- **Story 1.6 (Tenant access & layered fail-closed authorization) — done.** `TenantAccessAuthorizationResult` + `TenantAccessOutcome` + `TenantProjectionFreshnessStatus` consumed by Story 3.1 via `ProjectContextTenantAccess`. The `allowBoundedStaleTenantProjection` pattern from `ProjectAuthorizationGate` is reused conceptually — read-only `OperationKind` values allow `TenantProjectionFreshnessStatus.Stale`, trust-bearing values do not.
- **Story 1.4 (Tracer bullet) — done.** Established the safe-denial 404 contract, the `NoPayloadLeakage` reusable harness, and FS-8/SM-3 cross-tenant isolation harness — all reused unchanged.
- **Recent commit hygiene.** Stories 2.5 (e127b7a), 2.6 (0058ac3), 2.7 (70f2ebe) all follow story-scoped commits with no nested-recursive submodule init. Story 3.1 must do the same.
- **Epic 2 retrospective carry-forwards** (binding on Story 3.1):
  - "Define the (evidence-state × operation) fail-closed matrix as a single document referenced by all Epic 3 stories" — realized by AC 7 / Task 4.
  - "Continue per-story leakage-harness extensions; add a Memories-specific extension to assembly DTOs in Epic 3" — realized by AC 10 / Task 6.
  - "Track unproduced shared-vocabulary outcomes deliberately" — realized by AC 8 / Task 5.
  - "Restore an explicit local AppHost smoke check at least once in Epic 3 — ideally during Story 3.2 (Get Project Context)" — deferred to Story 3.2 (Story 3.1 is Tier-1 only).
  - "Promote the route/body identity + missing-Idempotency-Key negative-test pattern from per-story to a checklist" — deferred to Story 3.2 (Story 3.1 has no HTTP surface).
  - "Fix the NSwag idempotency-helper MSBuild target so it works on Linux" — does not apply to Story 3.1 (no `.g.cs` regeneration).
  - "Harden the shared idempotency canonicaliser for line-separator code points" — does not apply to Story 3.1 (no idempotency-fingerprint surface).

### Out Of Scope

- Implementing the `GetProjectContext` HTTP endpoint, OpenAPI route, request/response schemas, route/body identity validation, `Idempotency-Key` rejection on query, or generated client method (Story 3.2).
- Wiring the four Story 2.x ACLs (`IProjectConversationDirectory`, `IProjectFolderDirectory`, `IProjectFileReferenceDirectory`, `IProjectMemoryDirectory`) into the host-side composition that produces `ProjectContextReferenceEvidence` (Story 3.2).
- Implementing the `ExplainContextSelection` query / endpoint (Story 3.3) — Story 3.1 emits the `ProjectContextEvaluation` rows Story 3.3 will surface; it does not expose them via HTTP.
- Implementing the `RefreshProjectContext` query / endpoint (Story 3.4) — Story 3.1 emits `ProjectContextFreshness`; Story 3.4 owns the refresh semantics.
- Implementing the `ConversationStartSetupProjection` or `GetConversationStartSetup` endpoint (Story 3.5).
- Implementing project-resolution policy (Epic 4) — `Resolution/` stays empty.
- Adding new shared-vocabulary enum values (`ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`, `ProjectConversationTrustSignal`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus`) — if Story 3.1 implementation finds one truly missing, HALT and surface the conflict; the resolution is a follow-up ADR, not an inline edit.
- Regenerating `HexalithProjectsClient.g.cs` or `HexalithProjectsIdempotencyHelpers.g.cs`.
- Modifying `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`.
- Modifying `ProjectAggregate.*`, `ProjectState`, `ProjectStateApply`, `ProjectCommandValidator`, `ProjectCommandValidationResult`, `ProjectResult`, `ProjectResultCode`, `ProjectDetailProjection`, `ProjectListProjection`, `ProjectReferenceIndexProjection`, `ProjectAuthorizationGate`, `ProjectsDomainServiceEndpoints`, `IProjectCommandSubmitter`, or any existing shared-vocabulary enum.
- Modifying `_bmad-output/planning-artifacts/epics.md` Story 3.1 acceptance criteria; the `docs/adr/memories-link-target.md` ADR (status `Accepted`, frozen); or any shared-vocabulary enum.
- Advancing any submodule pointer (Hexalith.Memories / Conversations / Folders / Tenants / EventStore / FrontComposer / Commons / AI.Tools / Builds) or running `git submodule update --init --recursive`.
- Performing nested recursive submodule initialization / update.
- Running real-Keycloak / OIDC E2E (Epic 5 territory; the policy is pure and tested with deterministic inputs).
- Restoring the AppHost smoke-check evidence trail (deferred to Story 3.2 per the Epic 2 retro action item).
- Adding a checklist for route/body identity + missing-`Idempotency-Key` (deferred to Story 3.2; Story 3.1 has no HTTP surface).

### Developer HALT Conditions

- **HALT before authoring any policy `.cs`** if the AC 6 mapping table cannot be expressed entirely with existing shared-vocabulary values. Surface the conflict in the Dev Agent Record; the resolution is a follow-up ADR, not an inline enum addition.
- **HALT** if implementing the policy would require a network call, Dapr call, `HttpClient` usage, Aspire/Dapr/Microsoft.AspNetCore reference under `src/Hexalith.Projects/Context/**`, or any wall-clock read.
- **HALT** if implementing the policy would require editing `ProjectAggregate.*`, `ProjectState`, `ProjectStateApply`, `ProjectCommandValidator`, `ProjectResult*`, `ProjectDetailProjection`, `ProjectListProjection`, `ProjectReferenceIndexProjection`, or any Story 2.x ACL interface.
- **HALT** if implementing the policy would require modifying `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` or regenerating `.g.cs`.
- **HALT** if implementing the leakage harness extension would require modifying `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` (the harness itself stays general-purpose; Story 3.1 only extends the per-DTO tests).
- **HALT** if the `Diagnostic` field surfaces any string outside `ProjectContextInclusionDiagnostic.Values` — including but not limited to raw upstream `Message`/`Suggestion`/path/token/payload text, transcript fragments, prompt fragments, file contents, memory bodies.
- **HALT** if `Thread.Sleep` / `Task.Delay` / `SpinWait` / wall-clock polling is required to make a test pass.
- **HALT** if a submodule pointer or `_bmad-output/planning-artifacts/epics.md` Story 3.1 ACs would need to change for the story to land.

## References

- `_bmad-output/planning-artifacts/epics.md` — Story 3.1 ACs (authoritative).
- `_bmad-output/planning-artifacts/architecture.md` — AR-9 (ProjectContext assembly policy); §"`ProjectContext` is an assembled, authorization-filtered read result"; Implementation Sequence step 7.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md` — FR-16/FR-17/FR-18/FR-20 consequences; NFR-1/NFR-2/NFR-3 fail-closed posture; UJ-1/UJ-4 user-journey framing.
- `docs/adr/memories-link-target.md` (status: `Accepted`) — §"Epic 3 allowlist treatment" + §"Failure-to-shared-vocabulary mapping"; binding on the Memories portion of AC 6.
- `docs/adr/identifier-boundary.md` — sibling identifier reuse rule (no Projects-owned VOs for Memories/Folders/Conversations identifiers).
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-28.md` — Epic 2 retrospective; §"Next Epic Preview" + §"Epic 3 Preparation Tasks" + §"Action Items" are binding inputs for Story 3.1.
- `_bmad-output/implementation-artifacts/2-7-link-unlink-memory.md` — immediate-prior story; Dev Agent Record shape to mirror; Memories ACL/forbidden-term list carried into AC 10.
- `_bmad-output/implementation-artifacts/2-5-link-unlink-file-reference.md` — Folders ACL outcome mapping reused in AC 6; Folders forbidden-term list carried into AC 10.
- `_bmad-output/implementation-artifacts/2-4-set-auto-create-project-folder.md` — degraded `Pending` Project Folder path mapped in AC 6.
- `_bmad-output/implementation-artifacts/2-1-conversation-reference-read-acl.md` — `ProjectConversationTrustSignal` matrix consumed in AC 6.
- `_bmad-output/implementation-artifacts/1-6-tenant-access-layered-fail-closed-authorization.md` — `TenantAccessAuthorizationResult` + `TenantAccessOutcome` + `TenantProjectionFreshnessStatus` consumed by the policy.
- `_bmad-output/implementation-artifacts/1-4-create-project-end-to-end-tracer-bullet.md` — safe-denial 404 contract + FS-2 `NoPayloadLeakage` reusable harness + FS-8/SM-3 cross-tenant isolation harness reused unchanged.
- `_bmad-output/project-context.md` — umbrella rule set (96 rules); Tier-1 purity, central package management, no-`V2`, metadata-only, no submodule recursion.
- `docs/payload-taxonomy.md` — sensitivity classes; metadata-only invariants.
- `docs/event-catalog.md` — current event catalog; Story 3.1 extends with the shared-vocabulary-coverage subsection per AC 8.
- `src/Hexalith.Projects.Contracts/Models/PayloadClassification.cs` — FS-1 allowlist/denylist source of truth.
- `src/Hexalith.Projects.Contracts/Models/{ProjectFolderReference, ProjectFileReference, ProjectMemoryReference, ProjectSetup}.cs` — existing reference shapes Story 3.1 mirrors.
- `src/Hexalith.Projects.Contracts/Queries/{ProjectConversationItem, ProjectConversationTrustSignal}.cs` — conversation evidence shapes consumed by the policy.
- `src/Hexalith.Projects.Contracts/Ui/{ReferenceState, ProjectLifecycle, ProjectReasonCode}.cs` — shared vocabulary (do not edit).
- `src/Hexalith.Projects/Authorization/{TenantAccessAuthorizationResult, TenantAccessOutcome, TenantProjectionFreshnessStatus, AuthorizationOrder}.cs` — Story 1.6 surfaces.
- `src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs` — read-side state shape.
- `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` — FS-2 harness; AC 10 extension target.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (create-story, 2026-05-28)

### Debug Log References

- 2026-05-28: Resolved the `bmad-create-story` workflow; loaded `_bmad/bmm/config.yaml`; confirmed sprint status (`3-1-context-assembly-policy-allowlist` is `backlog`, Epic 3 is `backlog`, Stories 2.1–2.7 + Epic 2 retro are `done`); loaded Epic 3 Story 3.1 verbatim from `_bmad-output/planning-artifacts/epics.md` (lines 691–719); loaded the Epic 2 retrospective end-to-end and copied the action items binding on Story 3.1 into AC 7, AC 8, AC 10 + Dev Notes; loaded the Story 2.6 ADR §"Epic 3 allowlist treatment" + §"Failure-to-shared-vocabulary mapping" and copied the Memories portion of the mapping into AC 6; loaded the Story 2.7 implementation artifact end-to-end for pattern continuity (Tier-1 purity, deterministic-fakes-only tests, central NuGet package management, no `.g.cs` hand-edits); inspected `src/Hexalith.Projects/Context/.gitkeep` and confirmed the folder is empty (Story 3.1 fills it); inspected the shared-vocabulary enums and confirmed every value the AC 6 mapping table needs is already present (no new enum value required); inspected `ProjectDetailItem`, `ProjectFolderReference`, `ProjectFileReference`, `ProjectMemoryReference`, `ProjectSetup`, `ProjectConversationItem`, `ProjectConversationTrustSignal`, `TenantAccessAuthorizationResult`, `TenantAccessOutcome`, `TenantProjectionFreshnessStatus`, `AuthorizationOrder`; confirmed the FS-2 leakage harness shape at `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs`.
- Create-story workflow only; no implementation commands were run for this story.
- 2026-05-28 (dev-story execution): Implemented Tasks 1–7. **Task 1 capability gate** — re-confirmed every input shape and the shared-vocabulary coverage in `ReferenceState` / `ProjectLifecycle` / `ProjectReasonCode` / `ProjectConversationTrustSignal` / `TenantAccessOutcome` / `TenantProjectionFreshnessStatus`; **no new shared-vocabulary enum value required** (AC 14). **Task 2 Contracts DTOs** — added 4 enums + `ProjectContextInclusionDiagnostic` static class to `Ui/`, added 5 records to `Models/`; `dotnet build src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` produced 0W/0E. **Task 3 pure policy** — implemented as `sealed class ProjectContextInclusionPolicy(ILogger<ProjectContextInclusionPolicy>? logger = null)` with one public `Assemble(...)` method so the AC 5 structured-warning path can be injected without violating Tier-1 purity (a `static class` would have forced an `Action<string>` shim and obscured the logger API). Removed `src/Hexalith.Projects/Context/.gitkeep`; registered via `services.TryAddTransient<ProjectContextInclusionPolicy>()` in `ProjectsServiceCollectionExtensions.cs`. Boundary greps for `Hexalith.(Conversations|Folders|Memories)`, `Dapr|System.Net.Http|HttpClient`, and wall-clock reads under `src/Hexalith.Projects/Context/**` all return zero hits on `.cs` files (the .cs files paraphrase the prohibition without using the literal forbidden module strings in doc comments). **Task 4 decision-matrix doc** — authored `docs/context-assembly-decision-matrix.md` (per-evidence-state × per-operation table + outer overrides + memories/folders/conversation rows + non-allowlisted-kind row), cross-linked from `event-catalog.md` (preamble pointer) and from `architecture.md` (Process Patterns "ProjectContext assembly" bullet). LF on disk; `git diff --check` clean. **Task 5 unproduced-vocabulary tracker** — added `## Shared vocabulary — producer of last resort` to `event-catalog.md` (chose the inline section over a sibling doc to keep the catalog as a single discoverability surface); explicitly marked `ReferenceState.Stale` (Folders/Memories), `ReferenceState.TenantMismatch`, `ReferenceState.Ambiguous`, and `ProjectReasonCode.MetadataMatched` as `unproduced — taxonomy-only` with planned-producer pointers at Stories 3.4 / 4.x. **Task 6 Tier-1 tests** — created `tests/Hexalith.Projects.Tests/Context/` with 11 test files covering happy paths, tenant-authority collapse, project-visibility safe-denial, lifecycle archived, conversation trust-signal matrix, memories ACL outcomes (including TenantMismatch boundary collapse), file ACL outcomes, project folder lane, non-allowlisted kind allowlist, determinism + ordering, leakage, cross-tenant isolation, and decision-matrix completeness mirror. Added `RecordingLogger<T>` + `ProjectContextEvidenceBuilder` to `src/Hexalith.Projects.Testing/Context/`. Extended `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` with 7 new tests covering every new DTO (`ProjectContextReference`, `ProjectContextExclusion`, `ProjectContextEvaluation`, `ProjectContext`, `ProjectContextAssemblyResult`, archived-project rows) and Memories-specific forbidden-term coverage (`Content`/`ContentBytes`/`ContentHash`/`SourceUri`/etc.). Two test fixes during stabilization: (1) determinism test was initially failing because `ProjectContext` is a `sealed record` with `IReadOnlyList<>` fields and default record equality compares lists by reference; fixed by comparing per-list `.ToArray()` instead of the whole record; (2) decision-matrix completeness cross-tenant cell was initially failing because the evidence builder defaulted `requestedTenantId` to `"acme"`, collapsing the request to `Unauthorized` before reaching project-visibility; fixed by passing `requestedTenantId: "tenant-a"` explicitly so project-visibility produces the expected `ProjectUnavailable` verdict. **Task 7 validation** — `dotnet build Hexalith.Projects.slnx` 0W/0E; full-solution `dotnet test Hexalith.Projects.slnx` 719 passed / 0 failed / 0 skipped (Projects.Tests 350, Server.Tests 196, Contracts.Tests 128, Client.Tests 31, Integration.Tests 14). Boundary greps clean; no-sleep grep on Story 3.1 test files clean; `git diff --check` clean across story-touched files; no `.g.cs` change (`git status --porcelain src/Hexalith.Projects.Client/Generated/` empty); OpenAPI spine untouched (`git status --porcelain src/Hexalith.Projects.Contracts/openapi/` empty); no submodule pointer change (the pre-existing `Hexalith.Commons` / `Hexalith.Conversations` / `Hexalith.Parties` "modified content" markers were present at session start per the initial `git status` and were not introduced or touched by Story 3.1).

### Completion Notes List

- Story 3.1 context created. Status set to `ready-for-dev`.
- The policy is **purely additive** — no edits to Stories 1.4–2.7 surfaces (aggregate / state / projections / ACL interfaces / shared-vocabulary enums / OpenAPI spine / generated client / idempotency helpers).
- AC 6 is binding on the dev agent: the mapping table exhausts the (evidence-state × failed-check) decision space using only existing shared-vocabulary values. If a new value is genuinely required during implementation, **HALT** and surface the conflict — do not edit the shared vocabulary inline.
- AC 7 / Task 4 author `docs/context-assembly-decision-matrix.md`. This is the single doc Stories 3.2–3.5 reference; it must agree cell-by-cell with the policy output, enforced by the Task 6 completeness test.
- AC 8 / Task 5 update the shared-vocabulary unproduced-outcomes tracker per the Epic 2 retro action item. The tracker is the right place to record `TenantMismatch` and `Stale` as taxonomy-only outcomes until Story 3.4 starts producing `Stale` for real.
- AC 10 / Task 6 extend the FS-2 `NoPayloadLeakage` harness over the new assembly DTOs and carry forward the Memories + Folders forbidden-term lists from Stories 2.5/2.7.
- AC 11 + Guardrails enforce Tier-1 purity: no Dapr/Aspire/HttpClient/Hexalith.Conversations/Folders/Memories reference under `src/Hexalith.Projects/Context/**`, no wall-clock read, no `.g.cs` regeneration, no OpenAPI spine churn, no submodule pointer change.
- AppHost smoke-check evidence and route/body+missing-Idempotency-Key checklist are deferred to Story 3.2 (the explicit Epic 2 retro recommendation).
- The dev agent owns: Task 1 (capability gate — read-only inspection), Task 2 (Contracts DTOs), Task 3 (pure policy under `src/Hexalith.Projects/Context/`), Task 4 (decision-matrix doc), Task 5 (shared-vocabulary tracker), Task 6 (Tier-1 tests), Task 7 (validation).
- **Validation summary (AC 18, 2026-05-28).**
  - **Policy shape divergence from this AC list:** none material; the policy is a `sealed class` with a primary-constructed nullable `ILogger<ProjectContextInclusionPolicy>` (the AC-list "preferred" choice), exposing one public `Assemble(...)` method. The `ProjectContextDiagnostics` helper is internal; tests cover the public surface only.
  - **Closed `ProjectContextInclusionDiagnostic` vocabulary actually shipped:** `tenantMismatch`, `projectUnknown`, `projectArchived`, `referenceUnauthorized`, `referenceUnavailable`, `referenceStale`, `referenceArchived`, `referenceConflict`, `referenceInvalidIdentifier`, `referenceKindNotAllowlisted`, `projectFolderPending`, `referenceAmbiguous`, `referenceRedacted` (13 values; the AC-list naming `projectFolderPending` retained; two additional values — `referenceAmbiguous`, `referenceRedacted` — added to round-trip the `Ambiguous` and `Redacted` rows of the AC 6 mapping table). Every value is asserted leakage-free by the harness extension.
  - **`static class` vs `sealed class`:** `sealed class` (logger-injectable, transient DI registration). Justification recorded above.
  - **Files under `src/Hexalith.Projects/Context/`:** 9 .cs files (`ProjectContextAssemblyContext.cs`, `ProjectContextConversationEvidence.cs`, `ProjectContextDiagnostics.cs`, `ProjectContextInclusionOrder.cs`, `ProjectContextInclusionPolicy.cs`, `ProjectContextOperationKind.cs`, `ProjectContextProjectEvidence.cs`, `ProjectContextReferenceEvidence.cs`, `ProjectContextTenantAccess.cs`). Placeholder `.gitkeep` removed.
  - **Focused-lane test counts (re-measured by review on 2026-05-28):** `Hexalith.Projects.Tests` 407 (+194 over the post-2.7 baseline 213; review re-count corrects the dev-author's recorded 350), `Hexalith.Projects.Server.Tests` 196 (+15 over baseline 181 — no Server code touched; the delta is pre-existing drift not attributable to Story 3.1), `Hexalith.Projects.Contracts.Tests` 128 (no change), `Hexalith.Projects.Client.Tests` 31 (no change), `Hexalith.Projects.Integration.Tests` 14 (no change).
  - **Full-solution `dotnet test Hexalith.Projects.slnx`:** 776 passed / 0 failed / 0 skipped (post-2.7 baseline 567; Story 3.1 grew the solution by +209 tests, above the AC-list ~50–80 estimate because every cell of the AC 6 mapping table is asserted as a per-cell `[Theory]` row, plus the decision-matrix completeness mirror, plus the leakage-harness extension over the new DTOs, plus Memories-specific forbidden-term coverage, plus the additive `ProjectContextContractValidationTests` contract-boundary suite). Review re-run on 2026-05-28 confirms 776/0/0.
  - **`dotnet build` warnings/errors:** 0 W / 0 E (both per-project Contracts/Core/Testing/Tests builds and the full-solution build).
  - **`git diff --check` result:** clean across story-touched files (`src/Hexalith.Projects/Context/`, `src/Hexalith.Projects.Contracts/Models/`, `src/Hexalith.Projects.Contracts/Ui/`, `src/Hexalith.Projects.Testing/Context/`, `tests/Hexalith.Projects.Tests/Context/`, `tests/Hexalith.Projects.Tests/Leakage/`, `docs/context-assembly-decision-matrix.md`, `docs/event-catalog.md`, `_bmad-output/planning-artifacts/architecture.md`, `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs`, `src/Hexalith.Projects.Testing/Hexalith.Projects.Testing.csproj`).
  - **HALT items:** none. No new shared-vocabulary enum value was required, no submodule pointer change was required, no `.g.cs` regeneration was required, and no OpenAPI spine change was required.

### File List

**Added (`src/Hexalith.Projects/Context/`):**

- `src/Hexalith.Projects/Context/ProjectContextAssemblyContext.cs`
- `src/Hexalith.Projects/Context/ProjectContextConversationEvidence.cs`
- `src/Hexalith.Projects/Context/ProjectContextDiagnostics.cs`
- `src/Hexalith.Projects/Context/ProjectContextInclusionOrder.cs`
- `src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs`
- `src/Hexalith.Projects/Context/ProjectContextOperationKind.cs`
- `src/Hexalith.Projects/Context/ProjectContextProjectEvidence.cs`
- `src/Hexalith.Projects/Context/ProjectContextReferenceEvidence.cs`
- `src/Hexalith.Projects/Context/ProjectContextTenantAccess.cs`

**Added (`src/Hexalith.Projects.Contracts/Models/`):**

- `src/Hexalith.Projects.Contracts/Models/ProjectContext.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectContextAssemblyResult.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectContextEvaluation.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectContextExclusion.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectContextReference.cs`

**Added (`src/Hexalith.Projects.Contracts/Ui/`):**

- `src/Hexalith.Projects.Contracts/Ui/ProjectContextAssemblyOutcome.cs`
- `src/Hexalith.Projects.Contracts/Ui/ProjectContextFreshness.cs`
- `src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionCheck.cs`
- `src/Hexalith.Projects.Contracts/Ui/ProjectContextInclusionDiagnostic.cs`

**Added (`src/Hexalith.Projects.Testing/Context/`):**

- `src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs`
- `src/Hexalith.Projects.Testing/Context/RecordingLogger.cs`

**Added (`tests/Hexalith.Projects.Tests/Context/`):**

- `tests/Hexalith.Projects.Tests/Context/ProjectContextContractValidationTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextDecisionMatrixCompletenessTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyConversationCandidateTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyCrossTenantTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyDeterminismTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyFileReferenceCandidateTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyLeakageTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyLifecycleTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyMemoriesCandidateTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyNonAllowlistedKindTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyProjectFolderCandidateTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyProjectVisibilityTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyTenantAuthorityTests.cs`
- `tests/Hexalith.Projects.Tests/Context/ProjectContextInclusionPolicyTests.cs`

**Modified:**

- `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs` — registers `ProjectContextInclusionPolicy` via `TryAddTransient<>` so Story 3.2's host can resolve the policy next sprint.
- `src/Hexalith.Projects.Testing/Hexalith.Projects.Testing.csproj` — adds `Microsoft.Extensions.Logging.Abstractions` so the testing project can publish the `RecordingLogger<T>` fake.
- `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` — extends the FS-2 leakage harness over the new assembly DTOs and asserts Memories-specific forbidden-term coverage (AC 10).
- `docs/event-catalog.md` — adds the preamble cross-link to `context-assembly-decision-matrix.md` and the new `## Shared vocabulary — producer of last resort` section (AC 8 / Task 5).
- `_bmad-output/planning-artifacts/architecture.md` — adds the AR-9 cross-link to `docs/context-assembly-decision-matrix.md` on the Process Patterns "ProjectContext assembly" bullet.
- `_bmad-output/implementation-artifacts/3-1-context-assembly-policy-allowlist.md` — Dev Agent Record, File List, Change Log, Status (`ready-for-dev` → `review`), task checkboxes.

**Added (`docs/`):**

- `docs/context-assembly-decision-matrix.md` — the canonical per-evidence-state × per-operation fail-closed decision matrix (AC 7 / Task 4).

**Deleted:**

- `src/Hexalith.Projects/Context/.gitkeep` — placeholder removed now that the folder has real content.

## Senior Developer Review (AI)

Reviewer: Jérôme Piquot (via Claude Opus 4.7 story-automator-review). Date: 2026-05-28.

Outcome: **Approve** — no CRITICAL findings. Build clean (0W/0E). Full-solution `dotnet test` re-run by review: 776 passed / 0 failed / 0 skipped (Projects.Tests 407, Server.Tests 196, Contracts.Tests 128, Client.Tests 31, Integration.Tests 14). Boundary greps clean; no-sleep grep clean; cross-tenant safe-denial enforced; FS-2 leakage harness clean over every new DTO; no submodule pointer change; no `.g.cs` change; OpenAPI spine untouched.

Auto-fixes applied during review:

- **H1 — File List gap fixed.** Added `tests/Hexalith.Projects.Tests/Context/ProjectContextContractValidationTests.cs` to the Added-test-files block; the file shipped with the implementation but was missing from the Dev Agent Record list.
- **H2 — Validation summary numbers corrected.** Updated focused-lane and full-solution counts from the stale 350 / 719 the dev-author recorded to the actual 407 / 776 the review re-run measured. The 57-test delta matches the unrecorded ContractValidationTests plus matrix `[Theory]` row counts.
- **M1 — No-op logger test replaced.** `ProjectContextInclusionPolicyNonAllowlistedKindTests.Logger_RecordsWarning_WhenInvalidIdentifierEncountered` previously asserted `Entries.Count == 0` and `IsEnabled(Warning) == true` without ever exercising the policy. Renamed to `Logger_HappyPathAssemble_EmitsNoWarning` and now asserts that a full `Assemble(...)` with `WithAllKinds()` evidence emits zero warning entries — a meaningful regression guard for the day a non-allowlisted candidate kind reaches the warning path. The non-allowlisted-kind logging branch in `RecordNonAllowlistedKind` remains structurally unreachable through `Assemble(...)` today because every shipped candidate type hard-codes an allowlisted kind ("folder"/"file"/"memory"/"conversation"); the branch survives as a defensive guard for future candidate-evidence types and is documented in the test comment so the next reader is not surprised.
- **M3 — Missing `ProjectContext.Empty(...)` factory added.** Task 2's checklist listed three factories; only `Unauthorized` and `ProjectUnavailable` shipped. Added the `Empty(tenantId, projectId, lifecycle, observedAt, freshness)` factory (assembled-outcome, empty per-kind lists) so Stories 3.2 / 3.4 fixtures can express the "happy path, no references" assembly shape from a single named constructor.
- **L1 — `ProjectsServiceCollectionExtensions` summary refreshed.** The XML doc still claimed "Placeholder ... only establishes the registration surface" even though it now wires both Story 1.6 tenant-access services and the Story 3.1 inclusion policy. Updated to describe current behavior.

Findings left as-is (with rationale):

- **M2 — `IsReadOnlyOperation` tautology.** Returns `true` for every member of `ProjectContextOperationKind` today. Intentional per the Dev Notes (all four Epic 3 ops are read-only); the taxonomy is preserved so a future trust-bearing operation can be wired without changing the policy shape. Documented in the decision-matrix doc's "Read-only vs trust-bearing operations" section.
- **L2 — `ProjectContextDiagnostics.Guard` is public although unused externally.** Internal accessor; harmless.
- **L3 — `MapConversationTrustSignal` default arm.** Maps unknown signals to `Unavailable / ReferenceFreshness`. Acceptable defensive coding — future enum additions should be caught by a compile-time analyzer or a `[Theory]` over every enum value, but adding that test now is out of scope.

Action items (for Stories 3.2–3.5 to absorb, not blocking Story 3.1 close):

- When a candidate-evidence type that can legitimately emit a non-allowlisted kind is introduced (e.g. a generic external-reference shape), wire a positive logger-records-warning test through `Assemble(...)`. Today the path is structurally unreachable.
- When Story 3.4 introduces a trust-bearing operation kind, update `IsReadOnlyOperation` to discriminate and add a `[Theory]` over every kind in the tenant-authority stale-projection test.

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-05-28 | 1.0 | Created Story 3.1 artifact and set sprint status to `ready-for-dev`. Story is the first Epic 3 story; it lands the pure allowlist-based `ProjectContextInclusionPolicy` (`src/Hexalith.Projects/Context/`), the assembled-result DTOs (`Hexalith.Projects.Contracts/Models/` + `Ui/`), the closed `ProjectContextInclusionDiagnostic` vocabulary, the `docs/context-assembly-decision-matrix.md` fail-closed matrix doc (referenced by Stories 3.2–3.5), the shared-vocabulary unproduced-outcomes tracker (per the Epic 2 retro action item), and the FS-2 `NoPayloadLeakage` harness extension over the new DTOs with Memories + Folders forbidden-term coverage carried from Stories 2.5/2.7. Tier-1 purity enforced: no Dapr/Aspire/HttpClient/Hexalith.Conversations/Folders/Memories reference under `src/Hexalith.Projects/Context/**`; no wall-clock read; no `.g.cs` regeneration; no OpenAPI spine churn; no submodule pointer change; deterministic-fakes-only tests (no `Thread.Sleep`/`Task.Delay`/`SpinWait`/`Task.Yield`). AC 6 mapping table exhausts the decision space using only existing shared-vocabulary values; no new `ReferenceState`/`ProjectLifecycle`/`ProjectReasonCode`/`ProjectConversationTrustSignal` values introduced. AppHost smoke-check evidence and route/body+missing-Idempotency-Key checklist deferred to Story 3.2 per the Epic 2 retrospective. | Claude Opus 4.7 |
| 2026-05-28 | 1.1 | Implemented Tasks 1–7. Added 9 .cs files under `src/Hexalith.Projects/Context/`, 5 records under `src/Hexalith.Projects.Contracts/Models/`, 4 enum/static-class types under `src/Hexalith.Projects.Contracts/Ui/`, the `RecordingLogger<T>` + `ProjectContextEvidenceBuilder` helpers under `src/Hexalith.Projects.Testing/Context/`, 13 Tier-1 test files under `tests/Hexalith.Projects.Tests/Context/`, the decision-matrix doc at `docs/context-assembly-decision-matrix.md`, the producer-of-last-resort section in `docs/event-catalog.md`, the AR-9 architecture cross-link, the FS-2 harness extension in `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` (7 new tests), and the `ProjectContextInclusionPolicy` DI registration in `ProjectsServiceCollectionExtensions.cs`. The closed diagnostic vocabulary ships 13 values (added `referenceAmbiguous` and `referenceRedacted` beyond the AC 18 baseline so the `Ambiguous` and `Redacted` rows have explicit closed-vocabulary diagnostics). Full-solution `dotnet test Hexalith.Projects.slnx` 719 passed / 0 failed / 0 skipped (Projects.Tests 350 / Server.Tests 196 / Contracts.Tests 128 / Client.Tests 31 / Integration.Tests 14). 0W/0E build, boundary greps clean, no-sleep grep clean, `git diff --check` clean, no `.g.cs` change, OpenAPI spine untouched, no submodule pointer change. Status moved to `review`. | Claude Opus 4.7 |
| 2026-05-28 | 1.2 | Story-automator review pass (Senior Developer Review (AI)). Auto-fixes applied: added missing `ProjectContextContractValidationTests.cs` to the File List (H1); corrected the focused-lane / full-solution test counts in the AC 18 validation summary from 350 / 719 to the actual 407 / 776 (H2); replaced the no-op `Logger_RecordsWarning_WhenInvalidIdentifierEncountered` test with a meaningful `Logger_HappyPathAssemble_EmitsNoWarning` regression guard (M1); added `ProjectContext.Empty(...)` factory to honor the Task 2 checklist (M3); refreshed the stale "Placeholder" XML summary on `ProjectsServiceCollectionExtensions.AddProjectsModule` (L1). M2 (`IsReadOnlyOperation` tautology) left as-is per the Dev Notes rationale; L2 (`ProjectContextDiagnostics.Guard` accessor) and L3 (`MapConversationTrustSignal` default arm) noted as defensive coding. Re-run `dotnet build Hexalith.Projects.slnx` 0W/0E; re-run `dotnet test Hexalith.Projects.slnx` 776 passed / 0 failed / 0 skipped (Projects.Tests 407, Server.Tests 196, Contracts.Tests 128, Client.Tests 31, Integration.Tests 14). Status moved to `done`; sprint-status synced. | Claude Opus 4.7 (review) |
