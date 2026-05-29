# Test Automation Summary — Story 4.1 (Resolution engine, compute-on-demand)

> Workflow: `bmad-qa-generate-e2e-tests` · Date: 2026-05-29 · Engineer: QA automation (for Jerome)
> Story file: `_bmad-output/implementation-artifacts/4-1-resolution-engine-compute-on-demand.md`
> Test framework detected: **xUnit v3 + Shouldly** (.NET 10, `Hexalith.Projects.Tests`)

## Scope note — why no Playwright/HTTP "E2E"

Story 4.1 is, by design, a **pure compute-on-demand engine** — it has **no HTTP endpoint, no UI, no command/event/projection, and no I/O** (Dev Notes: "IS NOT any HTTP endpoint… ACL call, read-model query, or persisted trace"). So the workflow's *API tests (if applicable)* and *E2E tests (if UI exists)* steps have no web surface to target. The engine's public method `ProjectResolutionEngine.Resolve(context, candidates)` **is** the end-to-end behavioral boundary; "automated tests" here means Tier-1 behavioral coverage driven through that boundary with realistic, fully-composed evidence — the same boundary the Epic 3 `ProjectContextInclusionPolicy` draws. HTTP/Playwright coverage belongs to Stories 4.2/4.3 (which fetch evidence and call this engine).

## Generated tests

All additions are pure Tier-1, deterministic (fixed `Now`, zero `Thread.Sleep`/`Task.Delay`/`SpinWait`/`Task.Yield`), reuse the existing `ProjectResolutionEvidenceBuilder` + `RecordingLogger`, and introduce no new shared-vocabulary values or magic strings.

### Gaps discovered and auto-applied (9)

| # | Gap (untested branch / AC) | Test added | File |
|---|---|---|---|
| A | Tenant **mismatch** — both tenant ids set but unequal (`IsTenantAuthorityVerified` false-equality branch). AC4 / doc row / security negative path. Previously only the `null` authority case was pinned. | `Resolve_RequestedTenantMismatch_FailsClosedForEveryCandidate` | `ProjectResolutionEngineTests.cs` |
| B | `RequestedTenantId == null` + verified authority → candidate **qualifies** (the safe fail-open branch). | `Resolve_NullRequestedTenant_WithVerifiedAuthority_Qualifies` | `ProjectResolutionEngineTests.cs` |
| C | Candidate enumerated with **zero signals** → `NoMatch`, no exclusion row (silent-contribution edge). | `Resolve_CandidateWithNoSignals_NeitherQualifiesNorSurfacesExclusion` | `ProjectResolutionEngineTests.cs` |
| J | Archived **opt-in** candidate carrying a non-`Included` signal still **qualifies and surfaces the exclusion** (doc row line 20, AC5). | `Resolve_ArchivedOptInWithNonIncludedSignal_QualifiesAndStillSurfacesExclusion` | `ProjectResolutionEngineTests.cs` |
| D | Score **dominates** the `ProjectId` Ordinal tiebreak — higher score on a lexically-later id ranks first (AC6/AC9 ranking; prior tiebreak test used equal scores only). | `Resolve_ScoreDominatesProjectIdOrdering` | `ProjectResolutionEngineDeterminismTests.cs` |
| E | **AC11 trace reconstruction** — all five Resolution Trace states (`Resolved`/`NoMatch`/`MultipleCandidates`/`Excluded`/`FailedClosed`) reconstruct from engine evidence with no persisted trace; plus doc-table completeness. *No dedicated test existed.* | `ProjectResolutionTraceMappingTests` (4 cases) | `ProjectResolutionTraceMappingTests.cs` *(new)* |
| F | `ProjectFolderMatched` (weight 45) confidence-band cell was missing from the band theory; `MinimumQualifyingScore` had no doc↔code assertion (AC6 completeness). | `ConfidenceBandCells_QualifyWhenSoleCandidate` (+1 case) and `MinimumQualifyingScoreCell_CodeAgreesWithDocument` | `ProjectResolutionScoringMatrixTests.cs` |
| H | Validation branches with no coverage: negative `Score`, whitespace `ReferenceKind`, and `ResolutionCandidate` reason-code de-duplication. | `ResolutionCandidate_NegativeScore_Throws`, `MatchSignal_NullOrWhitespaceReferenceKind_Throws` (×3), `ResolutionCandidate_DuplicateReasonCodes_AreDeduplicated` | `ProjectResolutionContractValidationTests.cs` |
| AC9 | Domain-core **assembly** references no sibling-context / web assemblies (`Hexalith.Conversations/Folders/Memories`, `Dapr`, `Microsoft.AspNetCore`) — complements the existing ctor/field reflection proof with an assembly-level purity guarantee. | `ProjectResolutionEngine_Assembly_ReferencesNoSiblingContextOrWebAssemblies` | `ProjectResolutionPersistsNothingTests.cs` |

**New test cases added: 17** (4 + 1 + 2 + 5 + 1 + 4), raising the Resolution suite from 54 → **71**.

### Files
- [M] `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionEngineTests.cs`
- [M] `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionEngineDeterminismTests.cs`
- [M] `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionScoringMatrixTests.cs`
- [M] `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionContractValidationTests.cs`
- [M] `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionPersistsNothingTests.cs`
- [A] `tests/Hexalith.Projects.Tests/Resolution/ProjectResolutionTraceMappingTests.cs`

## Coverage

| Acceptance criterion | Status after this run |
|---|---|
| AC1 pure engine type / optional `ILogger` | covered (pre-existing) |
| AC2 typed `ResolutionResult` outcome | covered |
| AC3 per-candidate reason codes (shared vocab) | covered |
| AC4 fail-closed qualification (incl. tenant **mismatch**) | **gap closed (A, B)** |
| AC5 archived exclusion + opt-in (incl. non-included signal) | **gap closed (J)** |
| AC6 documented scoring/threshold cells | **completeness closed (F)** |
| AC7 persist-nothing positive proof | covered |
| AC8 metadata-only, no `TenantId` on the wire | covered |
| AC9 purity & determinism (incl. **assembly** references + score-dominance ranking) | **gap closed (D, AC9)** |
| AC10 Tier-1 five epic-named cases + guards | covered |
| AC11 trace-ready output (5 states reconstructable) | **gap closed (E)** |

- **API endpoints:** N/A — Story 4.1 exposes none (deferred to Stories 4.2/4.3).
- **UI features:** N/A — Story 4.1 has none (Resolution Trace view is Story 5.6).
- **Engine behavioral surface (`Resolve`):** all 11 ACs now have at least one pinning Tier-1 test.

## Verification

```
dotnet build tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj -warnaserror
  → 0 Warning(s) / 0 Error(s)

dotnet test … --filter FullyQualifiedName~Hexalith.Projects.Tests.Resolution --no-build
  → Passed: 71, Failed: 0, Skipped: 0   (was 54; +17 exactly)

dotnet test tests/Hexalith.Projects.Tests/…csproj --no-build   (full Tier-1 lane)
  → Passed: 513, Failed: 0, Skipped: 0   (was 496; +17 exactly)
```

Pinned SDK `/home/administrator/.dotnet` 10.0.300. `git diff --check` clean; no `.g.cs`, OpenAPI spine, `src/`, or submodule-pointer changes — only the six Resolution test files above.

## Gaps intentionally NOT filled

- **`HttpClient`/Dapr in the assembly-reference test.** The new assembly test forbids sibling-context + `Microsoft.AspNetCore` + `Dapr` references; `HttpClient` is screened at the type level by the existing `…HaveNoPersistenceOrNetworkDependencies` reflection proof rather than by assembly name (`System.Net.Http` is a shared framework assembly), to avoid a false positive. The two tests are complementary.
- **HTTP / Aspire / browser surface.** Out of scope for this pure engine — Stories 4.2/4.3 (resolve endpoints) and 5.6 (Resolution Trace UI) are the right place for API + E2E coverage.

## Validation against `checklist.md`

- [x] API tests generated (if applicable) — N/A by design (no HTTP surface); documented.
- [x] E2E tests generated (if UI exists) — N/A by design (no UI); engine `Resolve(...)` boundary covered behaviorally.
- [x] Tests use standard test framework APIs (xUnit v3 + Shouldly).
- [x] Tests cover happy path.
- [x] Tests cover critical error cases (tenant mismatch/missing, unauthorized, archived, null/validation guards).
- [x] All generated tests run successfully (71/71 Resolution, 513/513 full Tier-1).
- [x] Proper locators — N/A for non-UI; typed builders + shared-vocabulary enums (no magic strings).
- [x] Tests have clear, intention-revealing descriptions.
- [x] No hardcoded waits/sleeps (deterministic, fixed `Now`).
- [x] Tests are independent (no shared mutable state / order dependency).
- [x] Test summary created.
- [x] Tests saved to appropriate directory (`tests/Hexalith.Projects.Tests/Resolution/`).
- [x] Summary includes coverage metrics.

## Next steps
- Run on the standard CI Tier-1 lane (no infra dependencies).
- When Stories 4.2/4.3 land the HTTP resolve surfaces, add API/integration tests there (Testcontainers/Dapr-slim where a real boundary is needed) — the pure-engine cases above remain the contract those endpoints must honor.
