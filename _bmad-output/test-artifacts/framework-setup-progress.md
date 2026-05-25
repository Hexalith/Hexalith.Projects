---
stepsCompleted: ['step-01-preflight', 'step-02-select-framework', 'step-03-scaffold-framework', 'step-04-docs-and-scripts', 'step-05-validate-and-summary']
lastStep: 'step-05-validate-and-summary'
status: 'complete'
lastSaved: '2026-05-25'
mode: 'create (re-scaffold / overwrite — user-authorized)'
targetScope: 'umbrella-root-cross-module'
detectedStack: 'fullstack'
selectedFramework: 'playwright'
backendFramework: 'xunit-v3 (owned by module scaffold, not this workspace)'
workspaceRoot: 'tests/e2e'
executionMode: 'subagent-capable; executed as a single coherent pass for cross-file consistency'
verification: 'typecheck PASS · smoke 4/4 PASS · full suite collects (63 tests / 6 files)'
---

# Test Framework Setup — Progress (Re-scaffold run, 2026-05-25)

> This run **re-scaffolded** the workspace from scratch at the user's explicit request
> ("Re-scaffold (overwrite)"), overriding the preflight HALT that fires because an E2E
> framework already existed. The prior (same-day) source tree was backed up to
> `_bmad-output/test-artifacts/tests-e2e-backup-2026-05-25/` (source only, no node_modules)
> before the wipe. The regenerated workspace reproduces the proven architecture AND adds
> the verification + dependency fixes the prior run left pending.

## Step 1: Preflight Checks

### Target scope (user-confirmed, carried from prior run)

**Umbrella root (cross-module)** — an E2E workspace at the repo root for cross-module /
platform integration scenarios, driven by the **Hexalith.Projects Aspire AppHost**. This is
the home for the greenfield `Hexalith.Projects` platform E2E suite. FrontComposer's existing
`Hexalith.FrontComposer/tests/e2e` workspace is **out of scope** (separate, complete).

### Stack detection

- `test_stack_type: auto` → detected **`fullstack`**.
- Backend: .NET 10 across all 10 submodules (`*.slnx`/`*.sln`, `*.csproj`); Aspire AppHosts.
- Frontend/Node: Blazor + Node tooling (semantic-release/commitlint/Husky/Playwright).

### Prerequisite check

- Create-mode prerequisite "no existing E2E framework" **FAILED** (a complete `tests/e2e/`
  workspace existed from the same-day run). Reported as a HALT; user chose **Re-scaffold
  (overwrite)**. Proceeded after backing up the existing source tree.
- Runtime: **Node v26.1.0** (≥24 ✓), npm 11; `@playwright/test` + `@seontechnologies/playwright-utils`
  already installed.

## Step 2: Framework Selection — **Playwright** (browser/E2E)

`fullstack` + `test_framework: auto` → Playwright (large multi-module umbrella, multi-browser,
heavy API+UI, CI parallelism). Pre-committed by `tea_use_playwright_utils: true`, AR-23
(Playwright Node ≥24 + axe-core), the system test design (real Keycloak/OIDC, `data-testid`,
network-first + readiness probes), and the FrontComposer house convention. Backend xUnit v3
tiers remain owned by the module scaffold — out of scope for this workspace.

## Step 3: Scaffold Framework

Execution mode: `tea_execution_mode: auto` + subagents available → `subagent`; executed as a
**single coherent pass** because the work units are tightly coupled (helpers/fixtures/specs
reference each other) and cross-file consistency matters more than parallelism.

### Files regenerated (24 source files under `tests/e2e/`)

**Config / env:** `package.json` (Node ≥24; @playwright/test, @axe-core/playwright, @faker-js/faker,
@seontechnologies/playwright-utils, **dotenv** — see fix below), `playwright.config.ts`
(15/30/60 timeouts, BASE_URL env fallback, trace/screenshot/video on failure, HTML+JUnit+list,
CI parallelism, chromium/firefox/webkit, `testIdAttribute: data-testid`, `reducedMotion: reduce`,
`ignoreHTTPSErrors`, greenfield-guarded webServer, globalSetup), `tsconfig.json` (strict, NodeNext
ESM, path aliases), `.env.example`, `.nvmrc` (24), `.gitignore`, `global-setup.ts`.

**Fixtures & factories:** `support/merged-fixtures.ts` (`mergeTests` of api-request/auth-session/
recurse/log/intercept-network-call/network-error-monitor + custom `tenantContext`/`seededProject`),
`support/fixtures/projects-fixtures.ts`, `support/auth/keycloak-auth-provider.ts` (real OIDC
password grant, realm `hexalith`, multi-user, disk-persisted, 30s early renew), `support/factories/
{tenant,project}-factory.ts` (faker, override-driven, metadata-only; distinct-tenant pair + FR-19
forbidden-setup negative).

**Helpers & specs:** `support/helpers/{correlation,projects-api-client,readiness,a11y}.ts`,
`support/page-objects/project-detail.page.ts`, `specs/framework-smoke.spec.ts` (runnable today),
`specs/projects-{lifecycle,resolution,maintenance,audit,accessibility}.spec.ts` (F5/F6 journeys,
pattern-complete `test.fixme` until the Hexalith.Projects app exists).

## Step 4: Documentation & Scripts

`tests/e2e/README.md` regenerated (setup, running, AppHost bring-up, real-Keycloak auth,
architecture, best practices, CI nightly lane, troubleshooting, KB refs). `package.json` carries
`test:e2e` + the full script set (test, per-browser, smoke, a11y, install:browsers, report,
typecheck). .NET xUnit tiers documented as module-owned, not duplicated here.

## Step 5: Validation & Summary

### Verification — **EXECUTED this run** (prior run left this PENDING)

- **`npm run typecheck` (tsc --noEmit): PASS.** Initially failed with TS2339/TS2345 on `authToken`:
  `playwright-utils` ships auth-session fixtures as a plain object not expressed in Playwright's
  `Fixtures<>` types, so strict tsc couldn't infer the added test args from
  `base.extend(createAuthFixtures())`. **Fix:** typed the extend explicitly via the library's own
  `AuthFixtures` type — `base.extend<AuthFixtures>(createAuthFixtures() as unknown as
  Parameters<typeof base.extend<AuthFixtures>>[0])`. Runtime object unchanged (matches the
  auth-session fragment); only the static types were pinned.
- **`npm run test:smoke` (chromium): 4/4 PASS** (~2.5s). Validates factories, `data-testid` config,
  and axe-core WCAG 2.2 AA wiring with no app.
- **Full suite collection (`--list`): 63 tests / 6 files**, all imports resolve (exercises
  `merged-fixtures.ts` and every support module). Domain specs correctly enumerate as `test.fixme`.

### Dependency fix applied

- **Added `dotenv ^16.4.5`** to devDependencies and installed it. `@seontechnologies/playwright-utils@3.14.0`
  `auth-session` does `require('dotenv')` but does **not declare** it (absent from its `dependencies`
  and `peerDependencies`) — an undeclared transitive dep the consuming workspace must provide.
  Without it, `global-setup.ts` (and thus every run, including smoke) failed with
  `Cannot find module 'dotenv'`.

### Browsers

- **Chromium + chrome-headless-shell installed** (v1223 / Chrome 148). Firefox/WebKit not yet
  downloaded — only Chromium is needed for the default smoke/a11y lane; CI installs all three via
  `npm run install:browsers`.

### Checklist (checklist.md) — outcome

**PASS:** preflight (stack `fullstack`, manifests read), framework selection (Playwright; user
notified; xUnit noted module-owned), `support/` pattern + fixtures/helpers/factories/page-objects,
config correctness (TS, 15/30/60, BASE_URL fallback, screenshot/video on failure, HTML+JUnit+list,
parallel, CI retries/workers, multi-browser, data-testid, reduced-motion, guarded webServer), env
(`.env.example`, `.nvmrc` 24), fixtures (single merged object + auto-cleanup), factories (faker,
override-driven, fixture-layer cleanup), sample tests (runnable smoke + F5/F6 journeys; data-testid;
network-first; no sleeps), helpers (api client, Keycloak auth, correlation, readiness, a11y), docs
(README full), scripts (`test:e2e` + full set), security (no real creds; `.env.example` placeholders;
tokens via env; `.auth/` + `auth-sessions/` gitignored), **config loads + sample test executes
(now verified)**, no leftover TODO/FIXME (the `test.fixme` markers are intentional greenfield gates).

**ADAPTED (deliberate, house convention):** workspace root `tests/e2e/` with `specs/` +
`support/factories/` (checklist allows test-org flexibility; `support/` honored); fixture index named
`merged-fixtures.ts` (playwright-utils canonical); `.env.example`/`.nvmrc` in the workspace root;
trace = valid `retain-on-failure`; playwright-utils pinned `^3.14.0` (4.x flagged as a deliberate
future bump per the repo's no-casual-upgrade rule).

**N/A:** Pact consumer CDC checklist (`tea_use_pactjs_utils: false`).

### Completion summary

- **Framework:** Playwright (TS, Node ≥24, multi-browser, axe-core) — cross-module umbrella-root E2E
  for the greenfield Hexalith.Projects platform. xUnit v3 tiers owned by the module scaffold.
- **Knowledge fragments applied:** overview, fixtures-composition, auth-session, api-request, recurse,
  intercept-network-call, data-factories, network-error-monitor, log; network-first + playwright-config
  guardrails.
- **Verified:** typecheck clean, smoke 4/4 green, 63-test suite collects. Two real defects from the
  prior (unverified) scaffold fixed: auth-fixture typing + undeclared `dotenv` dependency.
- **Next steps (user):**
  1. `cd tests/e2e && npm run install:browsers` (add Firefox/WebKit for the full multi-browser lane)
  2. `cp .env.example .env` and fill Keycloak/test-user values when wiring domain journeys
  3. Un-`fixme` domain specs as Epic 1/5 lands; confirm v1 OpenAPI paths + `data-testid`s
  4. Recommended downstream TEA workflows: `ci` → `atdd` (start with Story 1.4 P0 scenarios) →
     `automate` → `trace`.

### Decision

Workflow **complete**. Framework re-scaffolded, documented, and — unlike the prior run —
**verified** (typecheck + smoke + full collection), with two genuine defects fixed.
