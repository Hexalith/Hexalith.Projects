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
