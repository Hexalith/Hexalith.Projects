# Test Automation Summary - Story 2.5 Link/Unlink File Reference

Workflow: `bmad-qa-generate-e2e-tests` (automate). Date: 2026-05-28.

Story 2.5 arrived in `review` with extensive existing coverage (full solution 506/506). This QA run audited
the implemented File Reference slice against the story's Testing Requirements and AC matrix, then auto-applied
the discovered gaps. No production code was changed — gaps were closed with tests only.

## Generated Tests

### API / Endpoint Tests
- [x] `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` — added four endpoint cases:
  - `LinkFile_RedactedFoldersEvidence_ReturnsSafe404AndDoesNotSubmit` — Folders `Redacted`/excluded/sensitivity evidence collapses to an indistinguishable safe-denial **404**, asserts no payload leakage, and never submits (AC5; sensitivity non-disclosure). Previously untested at the endpoint.
  - `LinkFile_UnavailableFoldersEvidence_Returns503AndDoesNotSubmit` — Folders `Unavailable` (retryable) maps to **503** and never false-accepts (AC5). The story requires "Folders ... unavailable mapping"; only `Denied`(404) and `Stale`(503) were previously covered.
  - `LinkFile_UnknownBodyField_Returns400AndDoesNotSubmit` — closed request schema (`JsonUnmappedMemberHandling.Disallow`) rejects an unexpected `rawContent` field with **400** (AC1 "closed request schemas"; "unknown fields rejected by closed JSON binding").
  - `DeleteFile_UnknownBodyField_Returns400AndDoesNotSubmit` — same closed-binding guarantee on the unlink route (rejects a planted `deleteUnderlyingFile` field).

### Domain / Boundary (Folders ACL) Tests
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectFileReferenceDirectoryTests.cs` — added `ValidateLink_FoldersArchivedConflict_FailsClosedAsArchived` exercising the **409 → `Archived`** branch of `MapFoldersStatus` (the only mapped status code with no test; story task "Map Folders 409 to archived" + Testing Requirement "archived/inactive folder or file evidence").

### E2E Tests
- [x] `tests/e2e/specs/projects-file-reference.spec.ts` (new) — API-level Playwright spec mirroring the existing `test.fixme` scaffolds (lifecycle/resolution/maintenance). Five journeys: authorized link → 202 + read-model convergence as `referenceKind=file`; link never touches the single Project Folder lane (AC3); unlink removes only the association, never the Project Folder row (AC4); denied/redacted Folders evidence → safe-denial 404 with no path/content leakage (AC5); same-`Idempotency-Key` duplicate link replays to exactly one reference (AC8). Marked `test.fixme` because the running Aspire topology/file-reference routes are not yet wired into E2E (consistent with all other Projects specs); typechecks under the workspace `tsc --noEmit`.

## Coverage

- **Endpoint ACL-outcome → HTTP mapping** (`FileReferenceValidationProblem`): now exercised for `Accepted`(202), `Denied`(404), `Redacted`(404), `Stale`(503), `Unavailable`(503). `Archived`/`TenantMismatch` collapse to the same 404 safe-denial branch as `Denied`.
- **Closed request binding**: link + unlink now prove unknown fields are rejected (400) and not submitted.
- **Folders status mapping** (`MapFoldersStatus`): 401/403/404→Denied, 422→ValidationFailed, 409→Archived (new), 5xx/transport→Unavailable, all now covered.
- **Security/no-leakage**: redacted-file safe-denial asserts `NoPayloadLeakageAssertions`; E2E denial asserts the raw path and the word `redacted` never appear in the response.
- Not changed (already strong): aggregate link/unlink/idempotency (22), projection file add/remove vs folder lane, NoPayloadLeakage event scans, OpenAPI spine + client-helper hashes.
- **Gaps intentionally not "filled"**: `ProjectFileReferenceValidationOutcome.TenantMismatch` is defined but unreachable in the directory by design — Folders tenant denial returns 401/403/404 → `Denied` (safe-denial), so a tenant-mismatch *ACL* test would assert unreachable behavior. Tenant mismatch is covered at the aggregate level (`UnlinkFileReference_TenantMismatch_IsRejected`, `LinkFileReference_TenantMismatch_IsRejected`). Flagged for the author rather than invented.

## Verification

| Lane | Command | Result |
|------|---------|--------|
| New file-ref endpoint + ACL filter | `dotnet test tests/Hexalith.Projects.Server.Tests/...csproj --filter "FullyQualifiedName~LinkFile\|FullyQualifiedName~DeleteFile\|FullyQualifiedName~ProjectFileReferenceDirectoryTests"` | Passed: 27, Failed: 0, Skipped: 0 |
| Full Projects server project | `dotnet test tests/Hexalith.Projects.Server.Tests/...csproj --no-build` | Passed: 163, Failed: 0, Skipped: 0 (was 158; +5 new) |
| E2E typecheck | `npm run typecheck` (in `tests/e2e`) | Passed (tsc --noEmit, 0 errors) |

Environment: `DOTNET_ROOT=/home/administrator/.dotnet` (SDK 10.0.300 per `global.json`); `/usr/bin/dotnet` (10.0.108) fails `rollForward: latestPatch`.

---

# Test Automation Summary - Story 2.4 Set & Auto-Create Project Folder

Workflow: `bmad-qa-generate-e2e-tests` (automate). Date: 2026-05-26.

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` - Added Set Project Folder replacement confirmation coverage, safe 404 metadata-only Folders denial mapping, and degraded pending auto-folder read response coverage.

### Domain / Boundary Tests
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectFolderDirectoryTests.cs` - Added Folders ACL fail-closed mapping for archived lifecycle evidence, mismatched evidence, denied/stale/missing permissions, upstream 5xx, and transport failure.
- [x] `tests/Hexalith.Projects.Tests/Replay/CommandDeliveryIdempotencyTests.cs` - Added SetProjectFolder replay/conflict/no-op idempotency proofs.
- [x] `tests/Hexalith.Projects.Tests/Leakage/NoPayloadLeakageTests.cs` - Added metadata-only coverage for `ProjectFolderCreationPending`, `ProjectFolderSet`, and folder link rejection events.

### E2E Tests
- [x] Not applicable for story 2.4. The implemented surface is a Projects API/domain/projection/ACL/client contract story with no browser UI route; Tier-1/Tier-2 API and boundary tests are the intended automation lane.

## Coverage

- Degraded auto-create pending behavior: covered by aggregate/domain tests, project read projection tests, reference-index tests, and endpoint pending reference response coverage.
- Set Project Folder replacement semantics: covered for missing confirmation rejection before Folders ACL and confirmed replacement dispatch.
- Folders ACL fail-closed mapping: covered for lifecycle, permissions, safe denial, 5xx, and transport failure.
- ProjectReferenceIndex projection: existing focused tests verified pending and set replacement behavior.
- Idempotency: covered for create pending idempotency and SetProjectFolder same-key replay/conflict plus same-folder no-op replay.
- Payload leakage: covered for new folder events, safe problem responses, and pending read response.
- Browser E2E: 0/0 applicable.

## Verification

| Lane | Command | Result |
|------|---------|--------|
| Projects domain/projection/leakage filter | `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~CommandDeliveryIdempotencyTests|FullyQualifiedName~NoPayloadLeakageTests|FullyQualifiedName~ProjectReferenceIndexProjectionTests|FullyQualifiedName~ProjectProjectionTests|FullyQualifiedName~ProjectAggregateHandleTests|FullyQualifiedName~ProjectCommandValidatorTests"` | Passed: 88, Failed: 0, Skipped: 0 |
| Projects server endpoint/ACL filter | `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~CreateProjectEndpointTests|FullyQualifiedName~ProjectFolderDirectoryTests|FullyQualifiedName~ProjectAuthorizationGateTests|FullyQualifiedName~ProjectsDomainProcessorTests"` | Passed: 68, Failed: 0, Skipped: 0 |
| OpenAPI contract spine filter | `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApiContractSpineTests"` | Passed: 18, Failed: 0, Skipped: 0 |
| Client generation filter | `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ClientGenerationTests"` | Passed: 26, Failed: 0, Skipped: 0 |
| Diff hygiene | `git diff --check` | Passed; Git reported existing LF-to-CRLF normalization warnings only |
| Senior review Projects filter | `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~NoPayloadLeakageTests|FullyQualifiedName~ProjectAggregateHandleTests|FullyQualifiedName~ProjectCommandValidatorTests|FullyQualifiedName~CommandDeliveryIdempotencyTests|FullyQualifiedName~ProjectReferenceIndexProjectionTests"` | Passed: 76, Failed: 0, Skipped: 0 |
| Senior review Server filter | `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~CreateProjectEndpointTests|FullyQualifiedName~ProjectFolderDirectoryTests|FullyQualifiedName~ProjectsDomainProcessorTests|FullyQualifiedName~ProjectAuthorizationGateTests"` | Passed: 69, Failed: 0, Skipped: 0 |
| Senior review full Projects solution | `dotnet test Hexalith.Projects.slnx --no-restore` | Passed: 445, Failed: 0, Skipped: 0 |

## Sprint Status

- `2-4-set-auto-create-project-folder` is `done` after senior review auto-fixes.

---

# Test Automation Summary - Story 2.3 Link & Move Conversation Write-Side

Workflow: `bmad-qa-generate-e2e-tests` (automate). Date: 2026-05-26.

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Projects.Server.Tests/ProjectConversationAssignmentEndpointTests.cs` - Extended endpoint coverage for explicit move confirmation rejection and authorized unlink dispatch, alongside existing link acceptance, request-supplied `actorPartyId` rejection, source-project fail-closed authorization, move dispatch, route/body mismatch, and safe write-ACL outcome mapping.

### Domain / Boundary Tests
- [x] `tests/Hexalith.Projects.Server.Tests/Conversations/ProjectConversationAssignmentDirectoryTests.cs` - Extended write-ACL coverage for expected-current conflict mapping, upstream reassignment failure status mapping, thrown upstream reassignment failures, and actor-party resolver fail-closed behavior without dispatch.

### E2E Tests
- [x] Not applicable for story 2.3. The implemented surface is a Projects API/server ACL/client contract story with no browser UI route; Tier-1/Tier-2 API and boundary tests are the intended automation lane.

## Coverage

- Projects link/move/unlink endpoint flows: covered.
- Explicit move confirmation and source/expected-current guard behavior: covered.
- Request body authority rejection for `actorPartyId`: covered at endpoint and OpenAPI closed-schema levels.
- Server-derived actor Party mapping and fail-closed resolver behavior: covered.
- Conversations reassignment failure mapping: covered for validation, hidden denial, conflict, unavailable, and thrown upstream failure.
- Contract spine/client/idempotency helper/leakage regressions: verified through focused existing test lanes.
- Browser E2E: 0/0 applicable.

## Verification

| Lane | Command | Result |
|------|---------|--------|
| Story 2.3 server filter | `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectConversationAssignment"` | Passed: 24, Failed: 0, Skipped: 0 |
| Projects server | `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore` | Passed: 111, Failed: 0, Skipped: 0 |
| OpenAPI contract spine filter | `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApiContractSpineTests"` | Passed: 16, Failed: 0, Skipped: 0 |
| Client generation filter | `dotnet test tests\Hexalith.Projects.Client.Tests\Hexalith.Projects.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ClientGenerationTests"` | Passed: 24, Failed: 0, Skipped: 0 |
| Payload leakage filter | `dotnet test tests\Hexalith.Projects.Tests\Hexalith.Projects.Tests.csproj --no-restore --filter "FullyQualifiedName~NoPayloadLeakageTests"` | Passed: 14, Failed: 0, Skipped: 0 |
| Diff hygiene | `git diff --check` | Passed; Git reported existing LF-to-CRLF normalization warnings only |

## Sprint Status

- `2-3-link-move-conversation-write-side` remains `review`.

---

# Test Automation Summary - Story 2.2 Conversation Project Reassignment

Workflow: `bmad-qa-generate-e2e-tests` (automate). Date: 2026-05-26.

## Generated Tests

### API Tests
- [x] `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs` - Added project reassignment route happy-path coverage proving trusted tenant binding, typed `202 Accepted` result mapping, command type mapping, and metadata-only response safety.

### Domain / Boundary Tests
- [x] `Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/ReassignConversationProjectCommandHandlerTest.cs` - Added duplicate idempotency replay coverage proving equivalent project reassignment requests replay the stored logical outcome without aggregate load or mutation.
- [x] `Hexalith.Conversations/tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateProjectAssignmentTest.cs` - Added explicit missing-target rejection coverage proving an omitted assignment target is not treated as clear/unassignment.

### E2E Tests
- [x] Not applicable for story 2.2. The implemented surface is a Conversations contract/domain/server API/client upstream capability with no browser UI route.

## Coverage

- Contract serialization and vocabulary: covered by existing story 2.2 contract tests.
- Aggregate assign/reassign/clear/no-op/rejection/replay behavior: covered, with missing-target rejection added by this QA step.
- Tenant-first server boundary and idempotency conflict/duplicate behavior: covered, with project-specific duplicate replay added by this QA step.
- API/client mapping: covered, with project route happy path added by this QA step.
- Projection materialization/list filtering after reassignment/clear: covered by existing story 2.2 projection/query tests.
- UI E2E: 0/0 applicable.

## Verification

| Lane | Command | Result |
|------|---------|--------|
| Conversations contracts | `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` | Passed: 587, Failed: 0, Skipped: 0 |
| Conversations domain | `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-restore` | Passed: 173, Failed: 0, Skipped: 0 |
| Conversations server/API | `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-restore` | Passed: 513, Failed: 0, Skipped: 0 |
| Conversations client | `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj --no-restore` | Passed: 25, Failed: 0, Skipped: 0 |
| Diff hygiene | `git diff --check` in `Hexalith.Conversations` | Passed; Git reported existing LF-to-CRLF normalization warnings only |

---

# Test Automation Summary

## Story 2.1 - Conversation Reference Read ACL

Workflow: `bmad-qa-generate-e2e-tests` (automate). Date: 2026-05-26.

## Generated Tests

### API / ACL Tests
- [x] `tests/Hexalith.Projects.Server.Tests/Conversations/ConversationsProjectConversationDirectoryTests.cs` - Added fail-closed coverage for upstream `401`, `403`, `404`, `500`, and thrown upstream failures.

### Pure Translator Tests
- [x] `tests/Hexalith.Projects.Server.Tests/Conversations/ProjectConversationTranslatorTests.cs` - Added safe hydration display assertions and mismatch suppression coverage.

### E2E Tests
- [x] Not applicable for story 2.1. The story requires Tier 1 pure translator/access-decision tests only, with no Dapr, network, containers, or browser.

## Coverage

- ACL query construction and paging: covered.
- Tenant/project mismatch fail-closed filtering: covered.
- Forbidden/unauthorized/unavailable upstream read handling: covered.
- Projection trust-state mapping, `MixedGeneration`, and safe hydration signals: covered.
- Browser E2E: 0/0 applicable for this story.

## Verification

| Lane | Command | Result |
|------|---------|--------|
| Projects contracts | `dotnet test tests\Hexalith.Projects.Contracts.Tests\Hexalith.Projects.Contracts.Tests.csproj --no-restore` | Passed: 117, Failed: 0, Skipped: 0 |
| Projects server | `dotnet test tests\Hexalith.Projects.Server.Tests\Hexalith.Projects.Server.Tests.csproj --no-restore` | Passed: 87, Failed: 0, Skipped: 0 |
| Projects solution | `dotnet test Hexalith.Projects.slnx --no-restore` | Passed: 366, Failed: 0, Skipped: 0 |
| Conversations client | `dotnet test Hexalith.Conversations\tests\Hexalith.Conversations.Client.Tests\Hexalith.Conversations.Client.Tests.csproj --no-restore` | Passed: 24, Failed: 0, Skipped: 0 |
| Conversations contracts | `dotnet test Hexalith.Conversations\tests\Hexalith.Conversations.Contracts.Tests\Hexalith.Conversations.Contracts.Tests.csproj --no-restore` | Passed: 580, Failed: 0, Skipped: 0 |

---

# Test Automation Summary - Story 1.1 (Module scaffold & build/CI wiring)

Workflow: `bmad-qa-generate-e2e-tests` (automate). Date: 2026-05-25.

## Applicability finding: NO new E2E/automated tests applicable at this stage

Story 1.1 scaffolds **structure + build + CI wiring only** — no Contracts, identifiers,
OpenAPI spine, aggregate, projection, API endpoint, or UI surface is implemented (those
land in Stories 1.2 / 1.3 / 1.4+ / 5.x per the epics and the system test design). The QA
automate skill generates tests for **implemented feature behavior**; there is no end-user
feature behavior to drive end-to-end at the scaffold stage.

The system test design (`_bmad-output/test-artifacts/test-design-qa.md`) confirms this: every
mapped P0/P1 scenario is assigned to a **content/feature story** ("Epic 1 — first content
story" = 1.2+), not to the scaffold slice. Story 1.1's only test obligation (AC-4) is that the
build + a real, non-empty filtered test lane pass green — already satisfied.

Inventing new tests here would be one of: gold-plating an already-complete designed harness;
testing the throwaway `/health` compile-skeleton endpoint (no designed scenario, likely replaced
in 1.3/1.9); or un-`fixme`-ing domain journeys that cannot pass without the app (would break the
green gate). All three are rejected per the skill's "Keep It Simple / avoid over-engineering"
guidance and the non-blocking, keep-the-build-green constraint.

## Existing test coverage (authored by dev-story + test-design preflight; verified green, untouched)

### .NET tiers (xUnit v3 + Shouldly, central package management)
- `tests/Hexalith.Projects.Contracts.Tests` — 1 test (contract marker / domain name).
- `tests/Hexalith.Projects.Tests` (Tier-1) — 2 tests (module marker + registration extension callable).
- `tests/Hexalith.Projects.Server.Tests` (Tier-2) — 2 tests (server + workers markers).
- `tests/Hexalith.Projects.Integration.Tests` (Tier-3) — 1 test (server + testing references resolve).

These are intentionally trivial "the lane is real and green" tests — they prove markers resolve
and the registration surface is callable, which is the entirety of the testable scaffold surface.

### E2E (Playwright, Node >=24) — `tests/e2e/`
- `specs/framework-smoke.spec.ts` — RUNNABLE NOW, no app required (factories, `data-testid`
  config, axe-core WCAG 2.2 AA wiring). 4/4 pass on chromium.
- `specs/projects-*.spec.ts` (lifecycle, resolution, maintenance, audit, accessibility) — F5/F6
  domain journeys authored as pattern-complete `test.fixme`; they auto-activate as the API/UI/
  `data-testid`s land in later stories. Correctly NOT runnable today (greenfield guard).

## Verification (no source changes were made by this QA step — confirmation only)

| Lane | Command | Result |
|------|---------|--------|
| Build | `dotnet build Hexalith.Projects.slnx -c Debug` | Build succeeded — **0 Warning(s), 0 Error(s)** |
| .NET tests | `dotnet test Hexalith.Projects.slnx --no-build -c Debug` | **6 passed, 0 failed, 0 skipped** (1 + 2 + 2 + 1) |
| E2E typecheck | `npm run typecheck` (in `tests/e2e`) | PASS (tsc --noEmit, clean) |
| E2E smoke | `npx playwright test specs/framework-smoke.spec.ts --project=chromium` | **4 passed** (~1.6s, incl. axe a11y self-check) |

## Coverage

- API endpoints implemented & in test design: 0 (none exist yet; `/health` is a non-feature compile skeleton) — 0/0 applicable.
- UI features implemented: 0/0 applicable (no UI surface yet).
- Scaffold test lanes proven real & green: 4/4 .NET test projects + e2e framework smoke.

## Next steps (deferred to the stories that implement the behavior)

- **Story 1.3** flips the two CI no-op-clean gates (FrontComposer inspect, OpenAPI fingerprint)
  into real gates once `Contracts/openapi/*.yaml` + `[Command]`/`[Projection]` contracts land.
- **Stories 1.2–1.4+** add Contracts/identity + create-project; un-`fixme` the matching
  `projects-*.spec.ts` journeys and add the FS-1/FS-2/FS-3 harness tests per the test design.
- Re-run this `qa-generate-e2e-tests` workflow against the first real feature surface (1.4+).
