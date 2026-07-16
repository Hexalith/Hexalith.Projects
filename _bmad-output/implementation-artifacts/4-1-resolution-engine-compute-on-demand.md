---
baseline_commit: 9727968a754a2682e40261a45a4544cde44f60a7
---

# Story 4.1: Resolution engine (compute-on-demand)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **Projects platform engineer**,
I want **a pure, compute-on-demand resolution engine that scores candidate Projects from pre-fetched evidence and returns a typed `ResolutionResult` with per-candidate reason codes, persisting nothing**,
So that **resolution is deterministic, testable, fail-closed, and never stores sensitive inference data** _(AR-10; NFR-9; NFR-4)_.

**Epic 4 context:** This is the **first** Epic 4 story and the foundation the rest of the epic consumes. It lands the **engine only** — the pure scoring/ranking/outcome core. The HTTP surfaces that fetch evidence and call the engine come next: Story 4.2 (Resolve From Conversation), Story 4.3 (Resolve From Attachments); Story 4.4 (Confirm Ambiguous Project) is the only mutating story (`ConfirmProjectResolution` → `ProjectResolutionConfirmed`); Story 4.5 (Propose New Project). **Story 4.1 does no I/O and emits no events.**

## Acceptance Criteria

> Derived from epics.md#Story-4.1 (lines 801–819), expanded into numbered, individually testable criteria. The three epic BDD blocks map to AC2–AC3 (result + reason codes), AC5–AC7 (compute-on-demand / persist-nothing / archived-exclusion / documented heuristic), and AC10 (Tier-1 purity coverage).

1. **Engine exists as a pure domain-core type.** A new sealed `ProjectResolutionEngine` class lives under `src/Hexalith.Projects/Resolution/` (the `Hexalith.Projects` domain-core assembly, namespace `Hexalith.Projects.Resolution`), with an optional `ILogger<ProjectResolutionEngine>` constructor parameter defaulting to `NullLogger`. It exposes a single pure compute method (recommended name `Resolve(...)`) that takes **pre-fetched, Projects-shaped evidence** and returns a typed result record. The engine performs **no** I/O.

2. **Typed outcome.** Given inputs (conversation metadata and/or attached folder/file/memory references) and the pre-assembled candidate evidence (derived by the caller from the reference index + ACL-fetched metadata), when the engine computes, then it returns a top-level `ResolutionResult` of `NoMatch`, `SingleCandidate`, or `MultipleCandidates` (the existing enum at `src/Hexalith.Projects.Contracts/Ui/ResolutionResult.cs` — do **not** add members).

3. **Per-candidate reason codes from the shared vocabulary.** Each returned candidate carries one or more `ProjectReasonCode` values drawn only from the existing shared vocabulary: `ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, `MetadataMatched` (`src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs`). **No new shared-vocabulary enums, magic strings, or free-text reasons** are introduced (consistent with every Epic 1–3 story).

4. **Fail-closed match qualification.** Only references whose evidence `ReferenceState` is a positive, authorized state (`Included`) contribute a positive match signal. References in any non-included state (`Unauthorized`, `Unavailable`, `Stale`, `Archived`, `TenantMismatch`, `Conflict`, `InvalidReference`, `Pending`, `Excluded`, `Ambiguous`) do **not** count toward a match and are surfaced as exclusion evidence — never silently dropped. Unverifiable tenant/authorization evidence fails closed (the candidate does not match).

5. **Archived-project exclusion (opt-in override).** Archived candidate Projects (`ProjectLifecycle.Archived`) are excluded from matching unless the request explicitly opts in (an `IncludeArchived` flag, default `false`). Excluded-archived candidates are surfaced as exclusion evidence with a lifecycle reason, never silently omitted.

6. **Documented scoring/confidence-band heuristic.** A documented heuristic determines candidate ranking and the single-vs-multiple threshold, authored at `docs/resolution-scoring-heuristic.md` (mirroring the `docs/context-assembly-decision-matrix.md` convention). The heuristic is **deterministic** (stable tiebreak by `ProjectId` Ordinal), and per NFR-9 **biases toward `MultipleCandidates` when genuinely ambiguous** — it never collapses ambiguity into a silent single attach. A Tier-1 completeness test pins doc↔code agreement (every documented band/threshold cell is asserted against engine behavior).

7. **Persist nothing — with positive proof.** The engine writes no events, commands, projections, or stores; nothing is persisted by resolution (only a later `ProjectResolutionConfirmed` in Story 4.4 persists state). This is proven positively (Epic 3 retro carry-forward action item): a Tier-1 test asserts the `ProjectResolutionEngine` type takes no EventStore / Dapr / command-submitter / projection-store / `HttpClient` dependency (constructor + reflected fields), and that the compute method's only observable side effect is an optional `ILogger` warning.

8. **Metadata-only output, no TenantId on the wire.** The result DTOs are metadata-only (no transcripts, file contents, memory bodies, prompts, paths, tokens, secrets, raw upstream messages). The wire result DTO carries **no `TenantId` field** (FS-8 / SM-3 — Epic 3 retro design rule; prefer Story 3.5's "do not declare the field" form over `[JsonIgnore]`). The `NoPayloadLeakage` harness is extended over the new result DTOs.

9. **Purity & determinism.** The engine and its inputs reference no `Hexalith.Conversations` / `Hexalith.Folders` / `Hexalith.Memories` / `Dapr` / `Microsoft.AspNetCore` / `HttpClient` types; it reads no wall-clock or stopwatch (the evaluation instant `Now` is an input); it never sleeps or fetches. Output ordering is deterministic. Tier-1 tests are deterministic-fakes-only (zero `Thread.Sleep` / `Task.Delay` / `SpinWait` / `Task.Yield`).

10. **Tier-1 test coverage.** A Tier-1 suite under `tests/Hexalith.Projects.Tests/Resolution/` (xUnit v3 + Shouldly) covers — at minimum — the five epic-named cases: **no-match**, **single-candidate**, **multiple-candidates**, **archived-exclusion**, and **unauthorized-resource-exclusion**, plus: reason-code tagging, deterministic ranking/tiebreak, the scoring/threshold cells (AC6), persist-nothing proof (AC7), no-TenantId-on-wire + leakage (AC8), and null-argument guards. No Dapr/network/containers/browser.

11. **Trace-ready output (no persisted trace).** The result structure carries enough per-candidate evidence (reason codes + per-candidate exclusion reasons) for the future Resolution Trace view (UX-DR9/UX-DR15, Story 5.6) to render `Resolved` / `NoMatch` / `MultipleCandidates` / `Excluded` / `FailedClosed` **without** any persisted trace. Mapping: `Resolved` ≙ `SingleCandidate`; `Excluded` / `FailedClosed` are per-candidate / per-input evidence rows, **not** new top-level `ResolutionResult` members.

## Tasks / Subtasks

- [x] **Task 1 — Author the scoring/confidence-band heuristic doc** (AC: #6, #11)
  - [x] Create `docs/resolution-scoring-heuristic.md` mirroring `docs/context-assembly-decision-matrix.md` structure: input → qualifying-match rule → per-reason-code weight → candidate score → confidence band → single-vs-multiple threshold, with a tabulated cell matrix.
  - [x] Document the fail-closed qualification (AC4): only `ReferenceState.Included` references score; everything else is exclusion evidence.
  - [x] Document the NFR-9 ambiguity bias: ties / near-ties / multiple qualifying candidates → `MultipleCandidates`; `SingleCandidate` requires exactly one qualifying candidate (or one candidate strictly dominating per the documented margin — keep conservative).
  - [x] Cross-link from `_bmad-output/planning-artifacts/architecture.md` (AR-10) the same way the decision-matrix doc is cross-linked.

- [x] **Task 2 — Define the engine input evidence records (domain core)** (AC: #1, #4, #5, #9)
  - [x] `src/Hexalith.Projects/Resolution/ProjectResolutionContext.cs` — sealed record: `AuthoritativeTenantId` (string?, server-derived), `RequestedTenantId` (string?), `IncludeArchived` (bool, default false), `Now` (DateTimeOffset — the only clock source), `CorrelationId`/`TaskId` (string?, logged, never output). Optional: a metadata-only summary of presented inputs (e.g. presented conversation/attachment ids) for trace.
  - [x] `src/Hexalith.Projects/Resolution/ProjectResolutionCandidateEvidence.cs` — sealed record per candidate: `ProjectId` (string, eager-validated non-empty), `DisplayName` (string?, normalized to null if whitespace), `Lifecycle` (`ProjectLifecycle`), and the candidate's match signals (a list of per-reference signals carrying `ProjectReasonCode`, the matched reference's `ReferenceState`, opaque reference id, `ObservedAt`). Reuse `ProjectFolderReference` / `ProjectFileReference` / `ProjectMemoryReference` shapes where they fit; do not invent parallel reference models.
  - [x] Normalize null collections to empty (mirror `ProjectContextReferenceEvidence.Empty`); add an `Empty`/canonical-empty where useful.
  - [x] Eager-validate identifiers at construction (mirror `ProjectContextConversationEvidence`).

- [x] **Task 3 — Define the output result DTOs (Contracts/Models)** (AC: #2, #3, #8, #11)
  - [x] `src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs` — sealed record: `ResolutionResult Result`, `IReadOnlyList<ResolutionCandidate> Candidates` (ranked, qualifying), `IReadOnlyList<ResolutionExclusion> Excluded` (non-qualifying candidates with reason), `DateTimeOffset ObservedAt`. **No `TenantId` field on the wire** (FS-8/SM-3). Provide composition factories (`NoMatch(...)`, etc.) mirroring `ProjectContext` factory style.
  - [x] `ResolutionCandidate` — sealed record: `ProjectId`, `DisplayName?`, `IReadOnlyList<ProjectReasonCode> ReasonCodes` (≥1, deterministic order), `Rank` (int), and a metadata-only relative `Score` (int) — confidence *band* stays documented in the heuristic doc, **not** a new wire enum (see Dev Notes "Design decision: confidence band").
  - [x] `ResolutionExclusion` — sealed record: `ProjectId`, the surfaced `ReferenceState`/lifecycle reason, and an optional safe diagnostic from a closed vocabulary (reuse `ProjectContextInclusionDiagnostic` if the values fit, or document why a small resolution-specific closed set is needed — no free text).
  - [x] Apply name-based JSON + eager validation consistent with existing Contracts/Models records.

- [x] **Task 4 — Implement `ProjectResolutionEngine`** (AC: #1, #2, #3, #4, #5, #6, #7, #9)
  - [x] `src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs` — sealed class, optional `ILogger`, single pure `Resolve(ProjectResolutionContext context, IReadOnlyList<ProjectResolutionCandidateEvidence> candidates)` method. `ArgumentNullException.ThrowIfNull` all inputs.
  - [x] For each candidate: drop non-`Included` match signals to exclusion evidence (AC4, fail-closed); if `Lifecycle == Archived` and not `IncludeArchived`, classify as excluded (AC5); compute score from surviving reason codes per the documented weights (AC6).
  - [x] Rank qualifying candidates deterministically (score desc, then `ProjectId` Ordinal asc). Decide outcome per the documented threshold: 0 qualifying → `NoMatch`; exactly 1 → `SingleCandidate`; ≥2 (or ambiguous) → `MultipleCandidates` (NFR-9 bias).
  - [x] Single side effect allowed: `_logger.LogWarning(...)` (structured metadata only — never reason payloads/ids beyond opaque safe values) for diagnostic edge conditions. No other side effects.
  - [x] Optional: a single-source-of-truth ordering/weights declaration (mirror `ProjectContextInclusionOrder`) so the heuristic isn't duplicated in branches.

- [x] **Task 5 — DI registration** (AC: #1)
  - [x] Register `ProjectResolutionEngine` via `TryAddTransient<ProjectResolutionEngine>()` in `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs` (`AddProjectsModule`), exactly as `ProjectContextInclusionPolicy` is registered.

- [x] **Task 6 — Extend the leakage harness** (AC: #8)
  - [x] Add `ProjectResolution`-shaped assertions to `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` (or the per-story extension pattern used since Story 3.1), covering the new DTOs; confirm no Conversations/Folders/Memories forbidden terms and no `TenantId` field on the wire.

- [x] **Task 7 — Tier-1 test suite** (AC: #4, #5, #6, #7, #8, #9, #10, #11)
  - [x] `tests/Hexalith.Projects.Tests/Resolution/` new folder. Add deterministic builders under `src/Hexalith.Projects.Testing/Resolution/` (mirror `ProjectContextEvidenceBuilder` + reuse `RecordingLogger`).
  - [x] `ProjectResolutionEngineTests.cs` — the five epic-named cases (no-match / single / multiple / archived-exclusion / unauthorized-resource-exclusion) + reason-code tagging + happy path.
  - [x] `ProjectResolutionScoringMatrixTests.cs` — doc↔code completeness over `docs/resolution-scoring-heuristic.md` cells (Theory-driven, hard-coded cells mirror the doc; AC6).
  - [x] `ProjectResolutionEngineDeterminismTests.cs` — stable ranking/tiebreak; input reordering yields identical output; no wall-clock (fixed `Now`).
  - [x] `ProjectResolutionPersistsNothingTests.cs` — positive proof (AC7): reflect over `ProjectResolutionEngine` constructor + fields asserting no EventStore/Dapr/command/projection/`HttpClient` dependency; assert `Resolve` is side-effect-free beyond optional logger via `RecordingLogger`.
  - [x] `ProjectResolutionLeakageTests.cs` — `NoPayloadLeakage` over result DTOs + no-`TenantId` assertion (AC8).
  - [x] `ProjectResolutionContractValidationTests.cs` — eager-validation + null-argument guards + name-based JSON round-trip + additive-deserialization tolerance.

- [x] **Task 8 — Build & verify (no contract-spine / .g.cs churn)** (AC: #1–#11)
  - [x] `dotnet build Hexalith.Projects.slnx` → 0 W / 0 E (use `/home/administrator/.dotnet` 10.0.300, not `/usr/bin/dotnet` — see Dev Notes).
  - [x] `dotnet test Hexalith.Projects.slnx --no-build` → all green; report per-lane counts.
  - [x] Confirm **no** OpenAPI spine change and **no** `.g.cs` regeneration (Story 4.1 adds no HTTP surface — the fingerprint gate must stay PASSED). Confirm no submodule pointer change and no nested recursive submodule init.

## Dev Notes

### What this story is — and is NOT

- **IS:** a pure, Tier-1, compute-on-demand resolution **engine** plus its input-evidence records, output result DTOs, the scoring-heuristic doc, DI registration, and a Tier-1 test suite.
- **IS NOT:** any HTTP endpoint, command, event, projection, OpenAPI/`.g.cs` change, ACL call, read-model query, or persisted trace. Those belong to later stories. The engine is a **pure function over pre-fetched evidence** — exactly the boundary `ProjectContextInclusionPolicy.Assemble(...)` draws in Epic 3.

### The engine pattern to mirror (Epic 3 — `ProjectContextInclusionPolicy`)

`ProjectContextInclusionPolicy` is your structural template. Replicate its shape, not its checks:

- **Class:** `public sealed class ProjectContextInclusionPolicy` with `ILogger<...>? logger = null` ctor defaulting to `NullLogger` [Source: src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:41-48].
- **Method:** one public pure method taking pre-fetched evidence records and returning a result record [Source: src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:59-68].
- **Purity guardrails (verbatim model):** "never references the sibling-conversations, sibling-folders, sibling-memories namespaces, no infrastructure clients, no AspNetCore types, no networking clients; never reads any wall-clock or stopwatch source; never sleeps; never fetches anything. The only side effect is an `ILogger` warning" [Source: src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:26-34].
- **Single-source-of-truth ordering:** `ProjectContextInclusionOrder.Sequence` declares the ordered checks once; duplicating it elsewhere is a forbidden anti-pattern [Source: src/Hexalith.Projects/Context/ProjectContextInclusionOrder.cs]. Mirror this for resolution weights/threshold so the heuristic lives in one place.
- **Output container:** `ProjectContextAssemblyResult(Context, IReadOnlyList<ProjectContextEvaluation> Evaluations)` — assembled value + per-candidate trace [Source: src/Hexalith.Projects.Contracts/Models/ProjectContextAssemblyResult.cs]. Your `ProjectResolution` plays the analogous role; the per-candidate reason-codes + per-candidate exclusions ARE the trace (computed, not stored — AC11).
- **DI:** `services.TryAddTransient<ProjectContextInclusionPolicy>();` inside `AddProjectsModule` [Source: src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs:35]. Add the resolution engine line right beside it.
- **Test builders:** `ProjectContextEvidenceBuilder` (static, deterministic, `DefaultTenant`/`DefaultProjectId`/`DefaultNow` constants, fluent `With*` factories) and `RecordingLogger<T>` are the reusable doubles — mirror/reuse them; do **not** invent new logging providers [Source: src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs, src/Hexalith.Projects.Testing/Context/RecordingLogger.cs].

### Shared vocabulary is already forward-built — consume, never add

The exact enums the AC names already exist (Story 1.2 forward-built them); the resolution engine consumes them:

- `ResolutionResult { NoMatch, SingleCandidate, MultipleCandidates }` [Source: src/Hexalith.Projects.Contracts/Ui/ResolutionResult.cs:22-35].
- `ProjectReasonCode { ConversationLinked, ProjectFolderMatched, FileReferenceMatched, MemoryMatched, MetadataMatched }` [Source: src/Hexalith.Projects.Contracts/Ui/ProjectReasonCode.cs:23-44].
- `ProjectLifecycle { Active, Archived }` [Source: src/Hexalith.Projects.Contracts/Ui/ProjectLifecycle.cs:23-32].
- `ReferenceState { Pending, Included, Excluded, Unauthorized, Unavailable, Stale, Archived, Ambiguous, TenantMismatch, Conflict, InvalidReference }` [Source: src/Hexalith.Projects.Contracts/Ui/ReferenceState.cs].
- All four are already wired into `ProjectVocabularyDescriptors` (display label, accessible name, severity) — every new member would break the Tier-1 total-coverage test there, which is exactly why **you must not add members** [Source: src/Hexalith.Projects.Contracts/Ui/ProjectVocabularyDescriptors.cs:61-99].

### Design decision: candidate enumeration is NOT this story's job

The AC phrase "the reference index + ACL-fetched metadata" describes where the **caller** gets candidate evidence — it is **input** to the engine, not work the engine does. The `ProjectReferenceIndexProjection` today exposes only a forward `List(tenantId, projectId)` lookup and has **no reverse (reference → projects) query** [Source: src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs:149-167]. Building the candidate set (reverse lookup / `IProjectListReadModel.ListAsync` / ACL rechecks via `IProjectFolderDirectory` etc.) is the **host composition** owned by Stories 4.2/4.3 — the same split as Story 3.2, where the endpoint fetches `ProjectDetailItem` + conversations + tenant-access and hands them to the pure policy. **Do not add a reverse-index query, read-model call, or ACL call to the Story 4.1 engine** — it would break Tier-1 purity (AC7/AC9). Story 4.1's deliverable is the engine + the **input contract** (`ProjectResolutionCandidateEvidence`) that 4.2/4.3 will populate.

Candidate / reference shapes the host will project into evidence (for reference only):
- `ProjectDetailItem` carries `ProjectFolder` (single), `FileReferences`, `MemoryReferences`, `Lifecycle`, `Name` [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs:32-45].
- `ProjectListItem` carries `Lifecycle` (so the host can exclude archived) [Source: src/Hexalith.Projects/Projections/ProjectList/ProjectListItem.cs:23-30].
- Reference records all carry `ReferenceState` + opaque id + safe `DisplayName` + `ObservedAt` [Source: src/Hexalith.Projects.Contracts/Models/ProjectFolderReference.cs:20-25, ProjectFileReference.cs:31-37, ProjectMemoryReference.cs:30-35].

### Design decision: confidence band ≙ documented threshold, not a new wire enum

AC6 requires a documented **confidence-band heuristic**. To honor the "zero new shared-vocabulary enum values" discipline applied across Epic 1–3, express confidence as a **documented numeric score + band thresholds in `docs/resolution-scoring-heuristic.md`**, surfacing a metadata-only integer `Score`/`Rank` on `ResolutionCandidate`. Introduce a `ResolutionConfidence` wire enum **only if** a later trace-view story genuinely requires it — and if so it must follow the full shared-vocabulary pattern (`[ProjectionBadge]` + `ProjectVocabularyDescriptors` entry + Tier-1 completeness test) and be recorded as a decision. **Default: no new enum.** (HALT and record if the dev believes a wire enum is unavoidable.)

### Persist-nothing — positive proof (Epic 3 retro carry-forward)

The Epic 3 retrospective explicitly carries forward: "track Story 4.1 'persist nothing' rule with positive proof" [Source: _bmad-output/implementation-artifacts/sprint-status.yaml:73]. Satisfy it with `ProjectResolutionPersistsNothingTests.cs` (AC7): the engine takes no EventStore/Dapr/command-submitter/projection-store/`HttpClient` dependency (assert by reflection over ctor params + private fields) and the compute method is side-effect-free beyond the optional logger. This is a true Tier-1 property — there is nothing to mock because there is nothing to persist.

### UX trace mapping (forward-compat for Story 5.6, no work here)

The Resolution Trace view renders outcomes `Resolved / NoMatch / MultipleCandidates / Excluded / FailedClosed` [Source: _bmad-output/planning-artifacts/epics.md:161 (UX-DR9), ux-design-specification.md (Resolution Trace component)]. The code enum has only `NoMatch / SingleCandidate / MultipleCandidates`. Resolve the tension at the **evidence** level, not the enum level: `Resolved` ≙ `SingleCandidate`; `Excluded` (archived/policy/authorization) and `FailedClosed` (unverifiable tenant/auth) are **per-candidate `ResolutionExclusion` rows**. Make sure `ProjectResolution` carries enough per-candidate evidence to reconstruct all five trace outcomes; do **not** add enum members (AC11).

### Carry-forward items that do NOT apply to Story 4.1 (record as N/A)

- **U+2028/U+2029 canonicaliser hardening** — "needed before Story 4.4 mutation surface" [Source: sprint-status.yaml:73]. Story 4.1 regenerates no `.g.cs` and has no idempotency-fingerprint surface → N/A; defer to 4.4.
- **Wire `IsReadOnlyOperation` discriminator when Epic 4 introduces a trust-bearing operation kind** [Source: sprint-status.yaml:73]. The pure engine is compute-only and introduces no `ProjectContextOperationKind`-style operation; the trust-bearing/mutating operation is `ConfirmProjectResolution` (Story 4.4) → N/A here, flag for 4.4.
- **AppHost smoke / negative-test checklist** — those are HTTP-surface concerns (Stories 4.2+), not this pure engine.

### Project-wide invariants (always-on — from project-context.md)

- .NET 10 (`net10.0`), nullable + implicit usings + warnings-as-errors; do **not** weaken compiler settings to go green.
- File-scoped namespaces; 4-space indent, CRLF, UTF-8, final newline; private fields `_camelCase`; interfaces `I`-prefixed; async methods `Async`-suffixed; prefer `sealed`.
- Central package management — versions live in `Directory.Packages.props`, never inline.
- Domain core stays pure: no Dapr/Redis/Cosmos/broker/`HttpClient` in `src/Hexalith.Projects/`. Contracts stay low-dependency.
- Public contract changes are additive and serialization-tolerant; name-based JSON enums only (never ordinals).
- Logging is structured metadata only — never log payloads, ids beyond safe opaque values, secrets, tokens, or full bodies.
- Tenant isolation everywhere; fail closed when authorization/lifecycle/availability is unverifiable (NFR-2/NFR-3).

### Testing standards

- **Tier-1, pure, fast.** xUnit **v3** + **Shouldly** (matches `Hexalith.Projects.Tests` — `tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj` references `xunit.v3` + `Shouldly`). NSubstitute only if a seam genuinely needs it (prefer the deterministic builders/`RecordingLogger`).
- New tests under `tests/Hexalith.Projects.Tests/Resolution/`; new builders under `src/Hexalith.Projects.Testing/Resolution/`. Keep the Tier-1 vs Tier-2 boundary clean (Epic 3 retro action item) — everything in this story is Tier-1.
- Deterministic-fakes-only: zero `Thread.Sleep`/`Task.Delay`/`SpinWait`/`Task.Yield`; fixed `DateTimeOffset` `Now`; no wall-clock.
- Security/negative paths need explicit coverage, not just happy path: unauthorized-resource-exclusion and archived-exclusion are first-class ACs (AC4/AC5/AC10).

### Build / environment note

Use the SDK at `/home/administrator/.dotnet` (10.0.300), **not** `/usr/bin/dotnet`. Author hand-written `.cs` with CRLF, UTF-8, final newline, no BOM, zero NUL bytes; run `git diff --check` clean before declaring done. No submodule pointer changes; never run recursive submodule init.

### Hard HALT conditions (stop and record before coding)

- A new `ResolutionResult`, `ProjectReasonCode`, `ProjectLifecycle`, or `ReferenceState` member would be genuinely required.
- The engine cannot compute without calling a read model, the reference index, an ACL directory, Dapr, the network, or the wall-clock (i.e. purity cannot hold) — this signals scope is leaking from 4.2/4.3 into 4.1.
- A confidence **wire enum** is judged unavoidable (see "confidence band" decision) — record the rationale.
- Producing a result would require persisting anything, or emitting any event/command — contradicts AR-10 / AC7.
- An OpenAPI spine change or `.g.cs` regeneration appears necessary — Story 4.1 has no HTTP surface; this is a scope error.
- A submodule pointer or the epics.md Story 4.1 ACs would need to change.

### Project Structure Notes

- New domain-core code: `src/Hexalith.Projects/Resolution/` (currently only `.gitkeep`) — `ProjectResolutionEngine.cs`, `ProjectResolutionContext.cs`, `ProjectResolutionCandidateEvidence.cs`, and (optional) a single-source-of-truth weights/threshold declaration. Namespace `Hexalith.Projects.Resolution`.
- New contracts: `src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs` (+ `ResolutionCandidate`, `ResolutionExclusion` — colocated or separate files per existing Models convention). Namespace `Hexalith.Projects.Contracts.Models`. Consumes `Hexalith.Projects.Contracts.Ui` enums.
- New doc: `docs/resolution-scoring-heuristic.md` (mirrors `docs/context-assembly-decision-matrix.md`).
- New tests: `tests/Hexalith.Projects.Tests/Resolution/`; new builders: `src/Hexalith.Projects.Testing/Resolution/`.
- DI touch: `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs` (one additive `TryAddTransient` line).
- Leakage harness touch: `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs` (additive).
- **No** changes to: `ProjectAggregate.*`, `ProjectState*`, projections, endpoints, OpenAPI spine, `.g.cs`, existing Contracts/Ui enums, `ProjectContextInclusionPolicy`, or any submodule.
- **Variance to note:** epics.md Epic 1 decomposition mentions a `ProjectAggregate.Resolution.cs` partial [Source: epics.md:284]. That is the home for the **confirmation** write-side (`ConfirmProjectResolution` → `ProjectResolutionConfirmed`, Story 4.4), **not** the pure engine. The compute-on-demand engine is intentionally placed in `Resolution/` as a standalone pure policy (like `Context/`), not on the aggregate — because it persists nothing and runs off pre-fetched read-side evidence. Documented here so the dev does not misplace the engine on the aggregate.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-4.1 (lines 801–819)] — story statement + acceptance criteria.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-4 (lines 795–799, 253–260)] — epic framing; "resolution traces are computed, not stored; define scoring/confidence-band heuristics in the resolution stories."
- [Source: _bmad-output/planning-artifacts/architecture.md] — AR-10 (compute-on-demand; persist only `ProjectResolutionConfirmed`), AR-9 (reference index), AR-5/AR-6 (`ConfirmProjectResolution`/`ProjectResolutionConfirmed`/`ProjectResolutionConfirmationRejected`), NFR-9 (correctness over automation, never silently attach; SM-C1/SM-C2), NFR-2/NFR-3 (tenant isolation, fail-closed), NFR-4 (structured reason-code observability, no payloads), shared state & reason-code vocabulary.
- [Source: _bmad-output/planning-artifacts/epics.md:161,170 (UX-DR9/UX-DR15)] and ux-design-specification.md (Resolution Trace) — trace outcome set `Resolved/NoMatch/MultipleCandidates/Excluded/FailedClosed`; engine output must support these without persisted trace.
- [Source: src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs:26-68] — the pure-engine template (class shape, purity guardrails, pure method).
- [Source: src/Hexalith.Projects/Context/ProjectContextInclusionOrder.cs] — single-source-of-truth ordering pattern to mirror for weights/threshold.
- [Source: src/Hexalith.Projects.Contracts/Models/ProjectContextAssemblyResult.cs, ProjectContextEvaluation.cs] — result + per-candidate trace shape to analogize.
- [Source: src/Hexalith.Projects.Contracts/Ui/ResolutionResult.cs:22-35, ProjectReasonCode.cs:23-44, ProjectLifecycle.cs:23-32, ReferenceState.cs] — already-built shared vocabulary the engine consumes (no additions).
- [Source: src/Hexalith.Projects.Contracts/Ui/ProjectVocabularyDescriptors.cs:61-99] — total-coverage test that forbids new enum members.
- [Source: src/Hexalith.Projects/Projections/ProjectReferenceIndex/ProjectReferenceIndexProjection.cs:149-167] — forward-only `List(...)`; no reverse query today (candidate enumeration is a 4.2/4.3 host concern).
- [Source: src/Hexalith.Projects/Projections/ProjectDetail/ProjectDetailItem.cs:32-45, ProjectList/ProjectListItem.cs:23-30] — reference collections + lifecycle the host projects into evidence.
- [Source: src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs:35] — DI registration site/pattern.
- [Source: src/Hexalith.Projects.Testing/Context/ProjectContextEvidenceBuilder.cs, RecordingLogger.cs; src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs] — reusable test doubles + leakage harness.
- [Source: docs/context-assembly-decision-matrix.md] — convention to mirror for `docs/resolution-scoring-heuristic.md`.
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml:73] — Epic 3 retro carry-forward items (persist-nothing positive proof; FS-8/SM-3 no-TenantId wire rule; Tier-1/Tier-2 boundary; IsReadOnlyOperation/U+2028 deferred to 4.4).
- [Source: _bmad-output/project-context.md] — project-wide invariants (Dapr-only infra, pure domain core, additive contracts, name-based enums, structured logging, tenant isolation).

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (initial implementation 2026-05-28); Claude Opus 4.8 (validation + completion 2026-05-29)

### Debug Log References

- 2026-05-28: Implemented Story 4.1 code/docs/tests, then hit a validation blocker before completion: `/home/administrator/.dotnet/dotnet build Hexalith.Projects.slnx` and targeted Projects builds fail in this Linux workspace before compile/test execution because existing generated NuGet assets/project-reference evaluation contain Windows restore paths / multi-target sibling project evaluation failures. `Hexalith.EventStore.Contracts`, `Hexalith.Conversations.Contracts`, and `Hexalith.FrontComposer.Contracts` individual target builds can be made to pass, but the Projects solution/project build still fails with `Build FAILED. 0 Warning(s) 0 Error(s)` during referenced-project `GetTargetFrameworks` evaluation. Tasks remain unchecked and Status remains `in-progress` until the required build/test gate is green.
- 2026-05-29: Re-ran the build with the pinned SDK at `/home/administrator/.dotnet` (10.0.300). The build did **not** fail on project-reference evaluation — it surfaced two real compile errors (`CS1739`) in `ProjectResolutionEngine.cs`: the two tenant/archived `CreateExclusion(...)` call sites used a PascalCase named argument `ReasonCode:` that did not match the method parameter `reasonCode`. Fixed both to `reasonCode:`. After the fix: `dotnet build Hexalith.Projects.slnx -warnaserror` → **0 Warning(s) / 0 Error(s)**. The earlier "Windows restore path / GetTargetFrameworks" diagnosis was a misread of the same `Build FAILED` line; the actual blocker was the named-argument mismatch.

### Completion Notes List

- ✅ Root cause of the prior validation blocker was a 2-site `CS1739` named-argument mismatch (`ReasonCode:` → `reasonCode:`) in `ProjectResolutionEngine.cs`, not an environment/restore issue. Fixed; solution now builds clean (0W/0E) with the pinned 10.0.300 SDK.
- Implemented the compute-on-demand resolution design: deterministic scoring rules with distinct reason-code weights (single source of truth in `ProjectResolutionScoringRules`), fail-closed exclusion evidence (only `ReferenceState.Included` scores; everything else surfaces as a `ResolutionExclusion`), archived opt-in handling, missing/mismatched tenant authority fails closed, no `TenantId` on the wire, and no persistence/I/O/wall-clock dependency in the engine.
- NFR-9 ambiguity bias holds: 0 qualifying → `NoMatch`, exactly 1 → `SingleCandidate`, ≥2 → `MultipleCandidates` (never collapses 2+ qualifying candidates into a silent single attach).
- Tier-1 suite (xUnit v3 + Shouldly) covers the five epic-named cases (no-match / single / multiple / archived-exclusion / unauthorized-resource-exclusion) plus reason-code tagging, deterministic ranking/tiebreak, the scoring/threshold cells (doc↔code completeness), persist-nothing reflection proof, leakage + no-`TenantId`, and null/eager-validation guards.
- Test results (pinned SDK, `--no-build`): Resolution-only 54/54; full Tier-1 `Hexalith.Projects.Tests` 496/496; Contracts.Tests 135/135; Client.Tests 43/43; Server.Tests 344/344; Integration.Tests 14/14. **Total 1032 passed, 0 failed, 0 skipped.** _(Superseded by the 2026-05-29 Senior Developer Review re-run — see that section; counts grew to Resolution 71/71, Tier-1 513/513, total 1049/1049 after additional working-tree tests.)_
- Verified no OpenAPI spine change, no `.g.cs` regeneration, no submodule pointer change; `git diff --check` clean. _Correction (2026-05-29 review): the working tree contains more than the one-line `reasonCode:` fix — additional Tier-1 test cases were added across `ProjectResolutionContractValidationTests.cs`, `ProjectResolutionEngineDeterminismTests.cs`, `ProjectResolutionEngineTests.cs`, `ProjectResolutionPersistsNothingTests.cs`, `ProjectResolutionScoringMatrixTests.cs`, plus a new `ProjectResolutionTraceMappingTests.cs`. All are uncommitted and now reflected in the File List._

### File List

- `_bmad-output/implementation-artifacts/4-1-resolution-engine-compute-on-demand.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/resolution-scoring-heuristic.md`
- `src/Hexalith.Projects/ProjectsServiceCollectionExtensions.cs`
- `src/Hexalith.Projects/Resolution/ProjectResolutionCandidateEvidence.cs`
- `src/Hexalith.Projects/Resolution/ProjectResolutionContext.cs`
- `src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs`
- `src/Hexalith.Projects/Resolution/ProjectResolutionScoringRules.cs`
- `src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs`
- `src/Hexalith.Projects.Testing/Leakage/NoPayloadLeakageAssertions.cs`
- `src/Hexalith.Projects.Testing/Resolution/ProjectResolutionEvidenceBuilder.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionContractValidationTests.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionEngineDeterminismTests.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionEngineTests.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionLeakageTests.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionPersistsNothingTests.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionScoringMatrixTests.cs`
- `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionTraceMappingTests.cs`

### Change Log

- 2026-05-28: Added initial Story 4.1 resolution engine implementation, docs, DI registration, leakage harness extension, builders, and Tier-1 tests. Validation blocked before task completion by workspace build/project-reference evaluation issue.
- 2026-05-29: Fixed the two `CS1739` named-argument errors (`ReasonCode:` → `reasonCode:`) in `ProjectResolutionEngine.cs` that were the real validation blocker. Built `Hexalith.Projects.slnx` clean (0W/0E) and ran the full solution test suite green (1032 passed / 0 failed / 0 skipped across all 5 lanes). Marked all tasks/subtasks complete and set Status to `review`.
- 2026-05-29: story-automator-review cycle 1 (auto-fix). Re-verified build 0W/0E and full solution **1049/1049** (Resolution 71/71, Tier-1 `Hexalith.Projects.Tests` 513/513, Server 344/344, Contracts 135/135, Client 43/43, Integration 14/14; 0 failed / 0 skipped). Auto-fixed: added `ProjectResolutionTraceMappingTests.cs` to the File List (was present in the working tree but undocumented); corrected the stale "only working-tree change" Completion Note and test counts; clarified the Confidence Bands wording in `docs/resolution-scoring-heuristic.md` (band is informational; qualification depends only on score ≥ minimum). 0 CRITICAL / 0 HIGH findings; Status set to `done`.

## Senior Developer Review (AI)

**Reviewer:** jpiquot · **Date:** 2026-05-29 · **Outcome:** Approve (auto-fix applied)

Adversarial review of Story 4.1 against the working tree, a clean build, and the full test suite. Every claim was cross-checked against `git` reality.

**Verified correct (no action needed):**
- All 11 Acceptance Criteria implemented and covered by Tier-1 tests (xUnit v3 + Shouldly). The five epic-named cases, reason-code tagging, deterministic ranking/tiebreak, doc↔code scoring completeness, persist-nothing reflection proof (ctor params + private fields + assembly references), no-`TenantId`-on-wire + leakage, null-argument guards, and AC11 trace-state reconstruction are all present and green.
- Build `Hexalith.Projects.slnx -warnaserror` → **0W/0E** (pinned SDK 10.0.300).
- Full solution **1049 passed / 0 failed / 0 skipped**.
- No OpenAPI spine change, no `.g.cs` regeneration, no submodule pointer change; `git diff --check` clean. Engine takes no EventStore/Dapr/command/projection/`HttpClient` dependency and reads no wall-clock — purity holds (AC7/AC9).
- Every file claimed in the File List exists on disk; no false "files changed" claims.

**Findings (all auto-fixed — no CRITICAL/HIGH):**
- **MEDIUM** — File List omitted `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionTraceMappingTests.cs` (present, untracked, supplies AC11 coverage). → Added to File List.
- **MEDIUM** — Completion Note "the only working-tree source change is the one-line `reasonCode:` fix" was stale/false: the tree also holds added test cases across 5 fixtures plus the new trace-mapping fixture. → Corrected with a dated note.
- **MEDIUM** — Reported test counts (Resolution 54/54, total 1032) were stale after the added tests. → Updated to the measured 71/71 and 1049/1049.
- **LOW** — `docs/resolution-scoring-heuristic.md` Confidence Bands table conflated *qualifying* (score ≥ minimum) with producing `SingleCandidate`. → Reworded; the asserted `| 0 | none |` / `| 20-34 | low |` cells preserved (doc-reading fixtures re-run green, 27/27).

**Note on uncommitted work:** the Story 4.1 additions remain uncommitted in the working tree; committing is left to the operator/automator (this review does not commit).
