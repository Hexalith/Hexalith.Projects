# Hexalith.Projects — Cross-Module E2E (Playwright)

Cross-module, platform-level end-to-end tests for the **Hexalith.Projects** workspace module,
driven against its **Aspire AppHost** topology (AR-22): `eventstore + tenants + projects + workers
+ projects-ui + Keycloak + Dapr/Redis`. This is the umbrella-root E2E suite — it is **separate from**
`references/Hexalith.FrontComposer/tests/e2e` (which is FrontComposer's own complete workspace).

> **Greenfield status.** The `Hexalith.Projects` API / web UI / AppHost do not exist yet (they land
> in Epic 1 / Story 1.x). The framework is fully wired; today only `specs/framework-smoke.spec.ts`
> runs green. The domain journeys under `specs/projects-*.spec.ts` are written as pattern-complete
> `test.fixme` and are **un-skipped** as the app, the v1 OpenAPI paths, and the `data-testid`s land.

## Prerequisites

- **Node.js ≥ 24** (`.nvmrc` → `nvm use`). The Playwright/utils stack requires it.
- A POSIX-ish shell or PowerShell. On Windows, the `dotnet` CLI for launching the AppHost (later).

## Setup

```bash
cd tests/e2e
nvm use                          # Node 24
CI=1 npm ci --ignore-scripts     # exact lockfile; dependency lifecycle scripts cannot run
npm run install:browsers         # explicit Playwright browser/dependency installation
cp .env.example .env         # then fill in Keycloak + test-user values (see below)
```

Do not use `npm install` for this workspace setup. A dependency has previously attempted to run a
recursive repository postinstall. The locked `npm ci --ignore-scripts` command is the supported
local and CI path; only the reviewed `install:browsers` script is invoked explicitly afterward.

## Running tests

```bash
npm run test:smoke           # RUNNABLE NOW — framework self-check, no app required
npm run test                 # or: npm run test:e2e — full suite (domain specs are fixme until the app exists)
npm run test:headed          # headed mode
npm run test:ui              # Playwright UI mode
npm run test:debug           # step debugger
npm run test:chromium        # single browser project
npm run test:a11y            # WCAG 2.2 AA scan (fixme until the console exists)
npm run report               # open the last HTML report
npm run typecheck            # tsc --noEmit
```

On local hosts where Playwright cannot install managed browser binaries, the config falls back to
system Chrome when available. CI always keeps the managed browser matrix; locally, set
`PLAYWRIGHT_INCLUDE_MANAGED_BROWSERS=1` to force Firefox/WebKit projects after installing browsers.

### Bringing up the system under test (once it exists)

The local web server is **off by default** (greenfield guard). Enable it once the AppHost lands:

```bash
export BASE_URL=https://localhost:7280
export E2E_WEBSERVER=1
export E2E_WEBSERVER_CMD="dotnet run --project ../../Hexalith.Projects/src/Hexalith.Projects.AppHost --no-launch-profile"
npm run test
```

Alternatively run the AppHost yourself (or `aspire run`) and leave `E2E_WEBSERVER=0` with
`reuseExistingServer`.

## Authentication (real Keycloak / OIDC)

Per **AR-19** and the test design, E2E proves runtime security with **real Keycloak tokens**
(realm `hexalith`) — synthetic JWTs are unit/integration only. Configure in `.env`:

```
KEYCLOAK_URL=https://localhost:8443
KEYCLOAK_REALM=hexalith
KEYCLOAK_CLIENT_ID=hexalith-projects-e2e
TEST_USER_EMAIL=...        TEST_USER_PASSWORD=...        TEST_TENANT_ID=...
```

The `keycloakAuthProvider` does an OAuth2 resource-owner password grant, persists the token to disk
(`auth-sessions/`, gitignored), and renews 30 s before expiry. Additional users for cross-tenant
negatives use `E2E_USER_<NAME>_EMAIL` / `E2E_USER_<NAME>_PASSWORD` and `authOptions.userIdentifier`.

## Architecture

```
tests/e2e/
├── playwright.config.ts        # timeouts, multi-browser, data-testid, reduced-motion, guarded webServer
├── global-setup.ts             # auth-session storage + Keycloak provider (token prefetch opt-in)
├── specs/
│   ├── framework-smoke.spec.ts        # runnable self-check (factories + axe)
│   └── projects-*.spec.ts             # F5/F6 journeys (fixme until the app exists)
└── support/
    ├── merged-fixtures.ts      # ⭐ mergeTests(playwright-utils) + project fixtures — import { test, expect } from here
    ├── fixtures/               # project-domain fixtures (tenantContext, seededProject + cleanup)
    ├── auth/                   # Keycloak OIDC auth provider
    ├── factories/              # faker-based, override-driven, metadata-only data factories
    ├── helpers/                # api client, correlation headers, readiness probes, axe a11y
    └── page-objects/           # optional POM examples
```

- **Fixtures** (`merged-fixtures.ts`): one project `test` object. Built on
  `@seontechnologies/playwright-utils` (`apiRequest`, `authToken`, `recurse`, `log`,
  `interceptNetworkCall`, `networkErrorMonitor`) plus custom `tenantContext` / `seededProject`.
  `seededProject` creates a project via API, waits for read-model convergence, and **archives it on
  teardown** (auto-cleanup — Projects has no hard delete).
- **Factories** (`support/factories`): `createProjectInput` / `createTenantContext` with `Partial`
  overrides and `faker` for parallel-safe, schema-tolerant data. Metadata only — never sibling payloads.
- **Helpers** (`support/helpers`): `projects-api-client` (typed v1 calls), `correlation`
  (Idempotency-Key / X-Correlation-Id / Freshness — AR-15/16), `readiness` (`recurse`-based
  convergence — **no sleeps**), `a11y` (axe WCAG 2.2 AA).

## Best practices (enforced by this scaffold)

- **Selectors:** `data-testid` only (`testIdAttribute` is configured) — role/label-based, survives
  FrontComposer regeneration (UX-DR28). Never CSS/text-coupled selectors.
- **No sleeps / network-first:** intercept **before** navigate; converge via `recurse`/`expect.poll`,
  never `waitForTimeout`. Command-async means no read-after-write (TC-3, TC-10).
- **Isolation:** a fresh `tenantContext` per test; factories generate unique ids → parallel-safe.
- **Cleanup:** seeded data is archived on teardown; tokens persist to disk but never to git.
- **Determinism:** `reducedMotion: 'reduce'`; deterministic anchors before assertions; flaky
  T3/E2E goes to a **quarantine lane**, never silenced (R8).
- **Security/privacy:** real Keycloak tokens; assert safe-denial (404 for unauthorized == nonexistent)
  and NoPayloadLeakage (no transcripts/secrets/tokens in any output) on every relevant journey.

## CI integration

- **Lane:** per the test design, this E2E suite runs in the **Nightly** lane (not the <15 min PR lane),
  alongside Pact CDC and after the PR-lane T1/CT/T2/CMP gates are green.
- **Reporters:** JUnit (`test-results/junit.xml`) for CI aggregation + HTML (`playwright-report/`).
- **Retries:** `2` on CI, `0` locally; traces/screenshots/video retained on failure.
- **Browsers:** `npm run install:browsers` in the CI job before `npm run test`.
- **.NET tiers are not here:** Tier-1/2/3 xUnit v3 tests live inside the `Hexalith.Projects` module
  (`tests/`), run via `dotnet test <Module>.slnx` (and `--collect:"XPlat Code Coverage"`), and are
  owned by the module scaffold — not this workspace.
- **Pact/CDC** is intentionally not scaffolded here (`tea_use_pactjs_utils: false`); ACL↔sibling
  contract testing (R6) is an Epic 2 concern.

## Troubleshooting

- **`Cannot find module '@seontechnologies/playwright-utils/...'`** — run `CI=1 npm ci --ignore-scripts`.
  The pin is
  `^3.14.0` (the documented TEA API). The package is now on 4.x; if you intentionally upgrade, re-verify
  the fixture subpath imports and `auth-session` function names in `support/`.
- **Config/TypeScript errors** — ensure `@playwright/test` types are installed; run `npm run typecheck`.
- **Smoke test can't launch a browser** — run `npm run install:browsers` (`playwright install --with-deps`).
- **Domain specs all show as skipped** — expected: they are `test.fixme` until the Hexalith.Projects
  app exists. Remove `.fixme` per spec as the API/UI/`data-testid`s land.
- **`KEYCLOAK_URL must be set` / auth errors** — fill `.env` from `.env.example`; auth only runs for
  the domain specs (and only pre-fetches when `E2E_AUTH_PREFETCH=1`).
- **Hangs waiting on a network call** — you intercepted *after* navigating. Set up
  `interceptNetworkCall(...)` **before** `page.goto(...)`.
- **`waitForProject` times out** — the read model never converged: check the Workers projection host
  and Dapr pub/sub; never paper over it with a sleep.
- **Self-signed cert errors** — Keycloak/AppHost dev TLS; `ignoreHTTPSErrors` is already on.

## Knowledge base references (TEA fragments applied)

`overview`, `fixtures-composition`, `auth-session`, `api-request`, `recurse`,
`intercept-network-call`, `data-factories`, `network-error-monitor`, `log` — from
`@seontechnologies/playwright-utils`. See also the system test design at
`_bmad-output/test-artifacts/test-design-*.md` (risks R1–R13, scenarios F5/F6, ASRs) and the
architecture/epics under `_bmad-output/planning-artifacts/`.
