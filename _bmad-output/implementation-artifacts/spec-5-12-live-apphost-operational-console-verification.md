---
title: 'Story 5.12: Live AppHost Operational Console Verification'
type: 'chore'
created: '2026-07-14'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'f03a8d6'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-5-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-live-apphost.md'
  - '{project-root}/docs/runbooks/projects-topology.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 5's live operational-console coverage cannot run: the AppHost never assigns a `projects-ui` route, route documentation guesses a port, and 57 actual product tests are permanently `test.fixme`.

**Approach:** Align the AppHost SDK with the shared Aspire 13.4.6 hosting stack, discover UI/API/security endpoints through Aspire, replace permanent fixme registration with an explicit live gate, and run the focused then full live suites with auditable results.

## Boundaries & Constraints

**Always:** Preserve existing dirty-worktree changes; use root-declared submodules only; start/wait/describe/stop through non-interactive Aspire commands; require distinct discovered UI and API URLs in live mode; skip disabled live tests before auth/seeding fixtures resolve; keep evidence metadata-only; record every pass, failure, and retained skip accurately.

**Ask First:** Any sibling-submodule content or pointer edit; any package change beyond aligning the AppHost SDK to the already-shared 13.4.6 version; destructive deletion of Aspire caches or installation changes beyond supported update/reinstall commands; product changes needed solely to make a failing test pass.

**Never:** Guess ports; use `dotnet run` for the AppHost; initialize nested submodules; hand-edit generated files or Aspire/DCP caches; use synthetic JWTs for the live lane; expose tokens, passwords, payloads, or private paths in evidence; claim skipped or failed cases passed.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| No-AppHost lane | `E2E_LIVE_APPHOST` absent | Fixture-contract tests run; live tests register skipped before auth/seeding | No endpoint or credential lookup |
| Invalid live config | Live flag set; UI or API URL missing/invalid | Playwright configuration fails fast | Name the missing variable without values |
| Live topology | Ready `projects-ui`, `projects`, and `security` resources | Browser uses `BASE_URL`; API fixture uses `API_URL`; real Keycloak auth runs | Fail tests with normal Playwright artifacts |
| Startup blocker | Build/resource readiness fails | No route is claimed; exact versioned blocker is recorded | Stop Aspire and preserve logs metadata-only |
| Unsupported test prerequisite | Route exists but deterministic fixture/product route does not | Retain an explicit conditional skip with a concrete reason | Include it in final counts |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` -- AppHost SDK/orchestration version boundary.
- `tests/e2e/playwright.config.ts` -- live-mode validation and browser base URL.
- `tests/e2e/support/merged-fixtures.ts` -- pre-fixture live-test registration and API base URL routing.
- `tests/e2e/specs/projects-*.spec.ts` -- 57 live test declarations across 13 specifications.
- `tests/e2e/.env.example` and `tests/e2e/README.md` -- discovered route/auth contract and commands.
- `_bmad-output/planning-artifacts/architecture.md` and `docs/runbooks/projects-topology.md` -- supported Aspire lifecycle/runbook.
- `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, and this spec -- Story 5.12 ownership/status.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` and `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-26.md` -- final execution evidence.

## Tasks & Acceptance

**Execution:**
- [x] `epics.md`, `sprint-status.yaml`, and this spec -- add Story 5.12 without reopening Story 5.11 or overwriting existing release-handoff edits.
- [x] AppHost `.csproj` -- align `Aspire.AppHost.Sdk` with shared Aspire 13.4.6 and prove the resolved orchestration package is consistent.
- [x] E2E config/fixtures -- load local environment, validate live UI/API URLs, route API calls to `API_URL`, and export a definition-time `liveAppHostTest` gate.
- [x] `tests/e2e/specs/projects-*.spec.ts` -- replace all permanent live fixme declarations; retain only reasoned prerequisite skips.
- [x] Architecture, topology runbook, E2E README, and env template -- document start/wait/describe/endpoint export/stop with no fixed ports.
- [x] Run focused and full live Chromium lanes; update Story 5.12, test summary, retrospective action, and proposal with actual route/count/blocker evidence.

**Acceptance Criteria:**
- Given the aligned build and root Commons override, when Aspire starts, then `projects-ui`, `projects`, and `security` endpoints are discovered without guessed ports or nested submodules.
- Given live mode is disabled, when the focused lane runs, then no-AppHost tests pass and live cases skip before network fixtures.
- Given valid discovered endpoints and local real-Keycloak credentials, when live mode runs, then the 13 focused live cases and every applicable remaining live case execute with recorded results.
- Given a route, fixture, or product prerequisite remains unavailable, when verification closes, then its exact blocker is recorded and no unexplained permanent fixme remains.

## Spec Change Log

- 2026-07-14: Approved and implemented. Aligned Aspire `13.4.6`, provisioned/discovered the live
  topology, replaced permanent live fixmes, ran focused and full Chromium lanes, and recorded the
  tenant-access/UI prerequisite failures without claiming them passed.
- 2026-07-14: Completed the three-layer review. Hardened live configuration, token handling,
  fixture cleanup, and shared-state isolation; recorded recurring CI, browser OIDC, and deterministic
  sibling-fixture work in the deferred-work ledger; reran all verification.

## Design Notes

The UI and API are separate Aspire resources. `BASE_URL` must remain browser-only; the inherited `apiRequest` fixture is wrapped so requests default to `API_URL`. The live-test callable selects normal `test` or `test.skip` at definition time, preventing disabled cases from resolving authentication and seeded-project fixtures.

## Verification

**Commands:** Run route discovery and non-URL credential exports exactly as documented in
`tests/e2e/README.md`; no discovered port or credential is persisted in this evidence.

```bash
HexalithCommonsRoot="$PWD/references/Hexalith.Commons" \
  dotnet build src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --no-restore --warnaserror
HexalithCommonsRoot="$PWD/references/Hexalith.Commons" \
  dotnet list src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj package --include-transitive

cd tests/e2e
npm run typecheck
env -u E2E_LIVE_APPHOST -u BASE_URL -u API_URL -u KEYCLOAK_URL \
  -u KEYCLOAK_CLIENT_ID -u TEST_USER_USERNAME -u TEST_USER_EMAIL \
  -u TEST_USER_PASSWORD -u TEST_TENANT_ID \
  npx playwright test --project=chromium specs/framework-smoke.spec.ts \
  specs/projects-accessibility.spec.ts specs/projects-resolution-trace.spec.ts \
  specs/projects-warnings-dashboard.spec.ts

# After the README's targeted aspire start/wait/describe and environment exports:
E2E_LIVE_APPHOST=1 PLAYWRIGHT_DISABLE_VIDEO=1 \
  npx playwright test --project=chromium specs/projects-lifecycle.spec.ts \
  --grep 'safe-denial: unauthorized'
E2E_LIVE_APPHOST=1 PLAYWRIGHT_DISABLE_VIDEO=1 \
  npx playwright test --project=chromium specs/framework-smoke.spec.ts \
  specs/projects-accessibility.spec.ts specs/projects-resolution-trace.spec.ts \
  specs/projects-warnings-dashboard.spec.ts
E2E_LIVE_APPHOST=1 PLAYWRIGHT_DISABLE_VIDEO=1 \
  npx playwright test --project=chromium specs/projects-*.spec.ts

aspire stop --apphost "$APPHOST" --non-interactive
aspire ps --format Json --non-interactive
git diff --check
```

Configuration collection was also invoked with `npx playwright test --list --project=chromium
specs/framework-smoke.spec.ts` for six invalid live environments: missing `BASE_URL`, missing
`API_URL`, missing `KEYCLOAK_URL`, identical UI/API origins, credentials embedded in `BASE_URL`, and
missing `KEYCLOAK_CLIENT_ID`. Every invocation exited `1` with the intended metadata-only error.

## Implementation Evidence

- AppHost build: passed, 0 warnings / 0 errors. Package inspection resolved
  `Aspire.Hosting.AppHost`, `Aspire.Hosting.Orchestration.linux-x64`, and `Aspire.Hosting` to `13.4.6`.
- Aspire lifecycle: targeted `start`, `wait projects-ui`, and `describe` reported healthy distinct UI,
  API, and security routes; targeted `stop` succeeded and no Projects topology remained.
- No-AppHost focus: 13 passed / 13 live skipped before network fixture resolution.
- Configuration fail-fast: missing `BASE_URL`, `API_URL`, and `KEYCLOAK_URL` each failed collection
  immediately and named only the missing variable; same-origin, URL-credential, and missing-client
  guards also failed as intended.
- Keycloak/TLS: automatic live real-token prefetch and one authenticated safe-denial case passed.
- Focused live: 26 total, 13 passed / 13 failed. Ten failures report project seed safe-denial `404`
  because `tenant-a` access projection is not provisioned; three report missing warnings-console
  selectors/static asset.
- Full live: 75 total, 19 passed / 56 failed, with every live declaration executing and no silent
  live skip. Local auth, JUnit, HTML, screenshot, and error-context artifacts were generated,
  summarized below, and removed; live traces are disabled because requests carry real bearer tokens.

| Product specification | Total | Passed | Failed |
| --- | ---: | ---: | ---: |
| accessibility | 10 | 5 | 5 |
| audit | 2 | 0 | 2 |
| console shell | 3 | 0 | 3 |
| file reference | 5 | 0 | 5 |
| inventory detail | 5 | 0 | 5 |
| lifecycle | 5 | 2 | 3 |
| maintenance | 10 | 5 | 5 |
| operator read access | 4 | 1 | 3 |
| proposal | 6 | 1 | 5 |
| reference health | 4 | 0 | 4 |
| resolution trace | 5 | 2 | 3 |
| resolution | 9 | 1 | 8 |
| warnings dashboard | 7 | 2 | 5 |
| **Total** | **75** | **19** | **56** |

## Suggested Review Order

**Live topology and configuration boundary**

- Live configuration normalizes endpoints, separates origins, validates prerequisites, and serializes shared fixtures.
  [`playwright.config.ts:19`](../../tests/e2e/playwright.config.ts#L19)

- AppHost SDK alignment removes the orchestration/DCP version mismatch at its source.
  [`Hexalith.Projects.AppHost.csproj:1`](../../src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj#L1)

- The operator runbook owns targeted lifecycle and dynamic endpoint discovery.
  [`projects-topology.md:19`](../../docs/runbooks/projects-topology.md#L19)

- The E2E guide turns discovered resources into executable browser, API, and security inputs.
  [`README.md:49`](../../tests/e2e/README.md#L49)

- Version posture records the supported Aspire stack consistently.
  [`architecture.md:123`](../planning-artifacts/architecture.md#L123)

**Authentication and fixture safety**

- Live setup clears cached auth and prefetches through development TLS without leaking responses.
  [`global-setup.ts:28`](../../tests/e2e/global-setup.ts#L28)

- Real Keycloak credentials produce browser-origin storage while error output remains metadata-only.
  [`keycloak-auth-provider.ts:104`](../../tests/e2e/support/auth/keycloak-auth-provider.ts#L104)

- Wrapped fixtures route API calls separately and skip offline cases before network resolution.
  [`merged-fixtures.ts:54`](../../tests/e2e/support/merged-fixtures.ts#L54)

- Project seeding validates accepted IDs and performs explicit, best-effort cleanup.
  [`projects-fixtures.ts:31`](../../tests/e2e/support/fixtures/projects-fixtures.ts#L31)

- Product specs use the shared live gate; lifecycle contains the authenticated safe-denial proof.
  [`projects-lifecycle.spec.ts:42`](../../tests/e2e/specs/projects-lifecycle.spec.ts#L42)

- The environment template exposes only dynamic route and credential contracts.
  [`.env.example:7`](../../tests/e2e/.env.example#L7)

**Evidence and sprint governance**

- The approved change proposal closes with exact observed outcomes.
  [`sprint-change-proposal-2026-07-14-live-apphost.md:275`](../planning-artifacts/sprint-change-proposal-2026-07-14-live-apphost.md#L275)

- Epic planning introduces Story 5.12 without reopening completed Story 5.11.
  [`epics.md:1110`](../planning-artifacts/epics.md#L1110)

- The test summary carries per-spec counts and blocker classification.
  [`test-summary.md:3`](tests/test-summary.md#L3)

- Sprint tracking remains at review while the retrospective action is marked executed.
  [`sprint-status.yaml:204`](sprint-status.yaml#L204)

- The retrospective distinguishes executed failures from deferred route work.
  [`epic-5-retro-2026-06-26.md:235`](epic-5-retro-2026-06-26.md#L235)

- Larger CI, browser OIDC, and deterministic fixture work remains explicitly deferred.
  [`deferred-work.md:43`](deferred-work.md#L43)
