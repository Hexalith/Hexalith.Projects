# Test Automation Summary — Story 3.1 Context-Assembly Policy & Allowlist

Workflow: `bmad-qa-generate-e2e-tests`. Date: 2026-05-28.

Story 3.1 ships the pure `ProjectContextInclusionPolicy` and its assembled-result DTOs. By explicit story
scope (AC 15, "No OpenAPI spine change"; Dev Notes "Explicitly out of scope") the policy has **no HTTP
endpoint**, **no Dapr binding**, and **no UI surface** — those land in Story 3.2 (`GetProjectContext`),
Story 3.3 (`ExplainContextSelection`), Story 3.4 (`RefreshProjectContext`), and Story 3.5
(`GetConversationStartSetup`). Consequently there is no API or browser E2E surface to drive in this story;
QA coverage is **Tier-1 contract/policy tests only**.

This QA run audited the existing 13 test files under `tests/Hexalith.Projects.Tests/Context/` and the FS-2
leakage-harness extension at `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs`, then
auto-applied the discovered gaps. **No production code was changed** — gaps were closed with tests only.

## Generated Tests

### Contract / Policy Tests
- [x] `tests/Hexalith.Projects.Tests/Context/ProjectContextContractValidationTests.cs` (new) — **57 tests**
  filling specific, AC-traceable gaps in the previously-shipped suite:
  - **`ProjectContextReference` eager validation** (AC 9 / Task 2):
    `NullOrWhitespaceReferenceKind_Throws` (×3), `NullOrWhitespaceReferenceId_Throws` (×3),
    `WhitespaceDisplayName_NormalizedToNull` (×4), `NonWhitespaceDisplayName_Preserved`.
  - **`ProjectContextExclusion` eager validation** (AC 9 / Task 2):
    `NullOrWhitespaceReferenceKind_Throws` (×3), `NullOrWhitespaceReferenceId_Throws` (×3),
    `EveryClosedVocabularyValue_IsAccepted`, `NullDiagnostic_IsAccepted`.
  - **`ProjectContextEvaluation` eager validation** (AC 9 / Task 2):
    `NullOrWhitespaceReferenceKind_Throws` (×3), `NullOrWhitespaceReferenceId_Throws` (×3),
    `OutOfVocabularyDiagnostic_Throws` (×4 — incl. wrong-case `"TENANTMISMATCH"` + path/JWT-shaped
    fixtures).
  - **`ProjectContextInclusionDiagnostic.IsKnown(...)` closed-vocabulary boundary**
    (AC 9, AC 17): `IsKnown_Null_ReturnsTrue`, `IsKnown_EveryShippedValue_ReturnsTrue`,
    `IsKnown_UnknownOrWrongCase_ReturnsFalse` (×7 — empty/whitespace/case variants/known-token
    free-text), `Values_AreImmutableAndOrdinal`.
  - **`ProjectContextInclusionOrder.IsAllowlisted(...)` Ordinal/case-sensitive contract**
    (AC 5): `WrongCaseOrSurroundingWhitespace_ReturnsFalse` (×8 — `"FILE"`, `"File"`, `"Folder"`,
    `"MEMORY"`, `"Conversation"`, leading/trailing whitespace, tab), `ExactAllowlistedValues_AllReturnTrue`.
  - **`ProjectContext` factory shapes** (Task 2):
    `Unauthorized_Factory_ProducesEmptyContextWithUnauthorizedOutcome`,
    `ProjectUnavailable_Factory_ProducesEmptyContextWithSafeDenialOutcome` (also asserts the safe-denial
    rule "never `Unauthorized` at the boundary").
  - **`ProjectContextInclusionPolicy.Assemble(...)` null-argument guards** (AC 1):
    `NullContext_ThrowsArgumentNullException`, `NullProjectEvidence_…`, `NullTenantAccess_…`,
    `NullReferenceEvidence_…`.
  - **Correlation-metadata non-leakage** (AC 10, AC 17):
    `CorrelationAndTaskIds_NeverLeakIntoAssembledContext` — injects a unique sentinel
    `CorrelationId`/`TaskId` into `ProjectContextAssemblyContext`, serializes the assembled result, and
    asserts neither sentinel appears anywhere in the JSON output.
  - **`ProjectContextReferenceEvidence` defaults** (Task 3):
    `NullCollections_DefaultToEmpty`, `Empty_IsTrulyEmpty`.

### API Tests
- **N/A by design.** Story 3.1 ships no HTTP endpoint; AC 15 forbids OpenAPI spine churn in this story.
  The `GetProjectContext` route lands in Story 3.2.

### E2E (browser / Aspire) Tests
- **N/A by design.** Per Dev Notes "AppHost smoke-check restoration is deferred to Story 3.2", Story 3.1
  is Tier-1 only — no Aspire topology to drive.

## Coverage

- **Existing Story 3.1 test inventory (pre-QA, all passing):**
  `ProjectContextInclusionPolicyTests` (13), `…NonAllowlistedKindTests` (8),
  `…TenantAuthorityTests` (16), `…ProjectVisibilityTests` (3),
  `…LifecycleTests` (2), `…ConversationCandidateTests` (7),
  `…MemoriesCandidateTests` (6), `…FileReferenceCandidateTests` (7),
  `…ProjectFolderCandidateTests` (4), `…DeterminismTests` (4), `…CrossTenantTests` (1),
  `…LeakageTests` (12), `ProjectContextDecisionMatrixCompletenessTests` (16), plus 7 leakage-harness
  extensions in `NoPayloadLeakageTests`.
- **QA-added (this run):** 57 new tests across `ProjectContextContractValidationTests.cs`. All
  AC-traceable; no behavior overlap with the existing matrix coverage.
- **Decision-matrix completeness (AC 7):** unchanged — every (`ReferenceState` × kind) and
  (`ProjectConversationTrustSignal` × outcome) cell continues to pass.
- **Tier-1 purity (AC 11):** no-sleep grep on the new file is clean (`Thread.Sleep` / `Task.Delay` /
  `SpinWait` / `await Task.Yield()` all return zero hits).
- **No production code changed** — only the new tests file under `tests/Hexalith.Projects.Tests/Context/`.

### Gaps intentionally not "filled"
- **`ProjectContextInclusionPolicy` non-allowlisted-kind logger warning end-to-end firing.** The
  `EvaluateFileCandidate`/`…Memory…`/`…Conversation…`/`…Folder…` paths hardcode their `Kind` constant,
  so the `RecordNonAllowlistedKind` warning is **defense-in-depth only and unreachable through the
  public `Assemble(...)` surface**. The existing
  `Logger_RecordsWarning_WhenInvalidIdentifierEncountered` asserts the recorder is wired up; coverage
  of the helper contract is provided directly via `ProjectContextInclusionOrder.IsAllowlisted(...)` tests
  (new + existing). Flagged for the author rather than invented as a synthetic injection point.
- **Non-read-only `ProjectContextOperationKind`.** All four shipped values are read-only per
  `IsReadOnlyOperation`, so the "trust-bearing operation → collapse to `Unauthorized` on stale tenant
  projection" branch named in AC 6 is not reachable from the public surface today. Story 3.4 (Refresh)
  is the right place to introduce a non-read-only kind and exercise that branch; no synthetic test is
  added here to avoid asserting unreachable code.
- **HTTP / Aspire / browser surface.** Out of scope per the story; will be tested in Stories 3.2–3.5.

## Verification

| Lane | Command | Result |
|---|---|---|
| New contract-validation tests | `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --filter "FullyQualifiedName~ProjectContextContractValidationTests" --no-build` | **57 passed / 0 failed / 0 skipped** |
| Full Projects.Tests lane (post-QA) | `dotnet test tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj --no-build` | **407 passed / 0 failed / 0 skipped** (was 350; +57 exactly) |
| Full-solution suite | `dotnet test Hexalith.Projects.slnx --no-build` | **776 passed / 0 failed / 0 skipped** across Projects.Tests 407 / Server.Tests 196 / Contracts.Tests 128 / Client.Tests 31 / Integration.Tests 14 (was 719; +57 exactly) |
| Build | `dotnet build tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj` | **0 warnings / 0 errors** |
| No-sleep grep | `grep -rE "Thread\.Sleep\\(|Task\\.Delay\\(|SpinWait\\.|await Task\\.Yield\\(" tests/Hexalith.Projects.Tests/Context/ProjectContextContractValidationTests.cs` | **zero hits (clean)** |

## Files Added / Modified

- **Added:** `tests/Hexalith.Projects.Tests/Context/ProjectContextContractValidationTests.cs` (1 file,
  57 tests).
- **Modified:** none.

## Next Steps

- Run the full solution in CI to confirm the green pre-Story-3.2 baseline (now 776/776).
- Story 3.2 will introduce the HTTP `GetProjectContext` endpoint; at that point the qa-generate-e2e-tests
  workflow can produce API + Aspire E2E coverage on top of the now-fully-tested pure policy.
