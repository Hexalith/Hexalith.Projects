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
