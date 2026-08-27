# Hexalith.Projects — Cross-Module E2E (Playwright)

Cross-module, platform-level end-to-end tests for the **Hexalith.Projects** workspace module,
driven against its **Aspire AppHost** topology (AR-22): `eventstore + tenants + projects + workers
+ projects-ui + Keycloak + Dapr/Redis`. This is the umbrella-root E2E suite — it is **separate from**
`references/Hexalith.FrontComposer/tests/e2e` (which is FrontComposer's own complete workspace).

The default lane stays offline and runs selector/factory contracts only. AppHost-backed journeys are
registered as normal tests only when `E2E_LIVE_APPHOST=1`; that lane requires routes discovered from
the running Aspire resource graph plus a real local Keycloak user and projected tenant access.

## Prerequisites

- **Node.js ≥ 24** (`.nvmrc` → `nvm use`). The Playwright/utils stack requires it.
- Aspire CLI `13.4.6`, .NET SDK `10.0.302`, `jq`, Dapr, and a Docker-compatible runtime for the live lane.

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
npm run test                 # default/offline lane; live journeys skip before fixture resolution
npm run test:headed          # headed mode
npm run test:ui              # Playwright UI mode
npm run test:debug           # step debugger
npm run test:chromium        # single browser project
npm run test:a11y            # offline contracts, plus live WCAG checks when E2E_LIVE_APPHOST=1
npm run report               # open the last HTML report
npm run typecheck            # tsc --noEmit
```

On local hosts where Playwright cannot install managed browser binaries, the config falls back to
system Chrome when available. CI always keeps the managed browser matrix; locally, set
`PLAYWRIGHT_INCLUDE_MANAGED_BROWSERS=1` to force Firefox/WebKit projects after installing browsers.

### Live AppHost route

The managed runner owns the complete lifecycle: it enables the explicit fixture profile, starts the
Projects AppHost, waits for every required resource, describes the graph exactly once, derives all
dynamic URLs, runs the startup/auth smoke and full Chromium suite with two workers, and always stops
that exact AppHost. Configure the non-URL values from `.env.example` or the shell:

```bash
cd tests/e2e
export KEYCLOAK_REALM=hexalith
export KEYCLOAK_CLIENT_ID=hexalith-eventstore
export TEST_USER_USERNAME=<local-realm-username>
export TEST_USER_PASSWORD=<local-realm-password>
npm run test:live:managed
```

If you copied `.env.example`, load the completed file into the shell before invoking the runner:

```bash
set -a
. ./.env
set +a
npm run test:live:managed
```

Do not preconfigure ports: `BASE_URL`, `API_URL`, `EVENTSTORE_API_URL`, `KEYCLOAK_URL`, and
`FIXTURE_API_URL` are assigned by Aspire and exported from the single captured graph. The runner
derives tenant and principal identities from the signed token, then provisions tenant access through
supported EventStore and Projects APIs. A unique `E2E_RUN_ID` is generated unless explicitly supplied.

## Authentication (real Keycloak / OIDC)

Per **AR-19** and the test design, E2E proves runtime security with **real Keycloak tokens**
(realm `hexalith`) — synthetic JWTs are unit/integration only. Configure in `.env`:

```
KEYCLOAK_REALM=hexalith
KEYCLOAK_CLIENT_ID=hexalith-eventstore
TEST_USER_USERNAME=...        TEST_USER_PASSWORD=...
```

API fixtures use an OAuth2 resource-owner password grant only for supported setup calls. Browser
proof performs a real Keycloak authorization-code login and persists only the server session cookie
in `.auth/` (gitignored). Access, refresh, and identity tokens must never be readable from browser
local or session storage.

## Architecture

```
tests/e2e/
├── playwright.config.ts        # explicit live-route validation, browsers, data-testid, reduced-motion
├── global-setup.ts             # tenant readiness + real browser OIDC session
├── specs/
│   ├── framework-smoke.spec.ts        # runnable self-check (factories + axe)
│   └── projects-*.spec.ts             # offline contracts + explicitly gated live F5/F6 journeys
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
- **Isolation:** live tests derive their tenant from the token and provision disjoint IDs from run,
  worker, retry, repeat, and scenario dimensions. The managed lane runs with two workers.
- **Cleanup:** seeded Projects are archived to convergence and sibling fixture roles are removed in
  reverse order. Cleanup artifacts contain attempted role and status only.
- **Determinism:** `reducedMotion: 'reduce'`; deterministic anchors before assertions; flaky
  T3/E2E goes to a **quarantine lane**, never silenced (R8).
- **Security/privacy:** real Keycloak tokens; assert safe-denial (404 for unauthorized == nonexistent)
  and NoPayloadLeakage (no transcripts/secrets/tokens in any output) on every relevant journey.

## CI integration

- **Lane:** the scheduled job runs `test:live:managed` with a GitHub-secret test password. The runner
  owns dynamic endpoint discovery, startup/auth smoke, full Chromium execution, and exact teardown.
- **Reporters:** JUnit (`test-results/junit.xml`) for CI aggregation + HTML (`playwright-report/`).
- **Retries:** `2` on CI, `0` locally. Live traces are disabled because requests carry real bearer
  tokens; screenshots/video remain failure diagnostics and must stay metadata-only.
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
- **Live journeys show as skipped** — use `npm run test:live:managed`; its live-only reporter fails
  the run if no tests are collected or any live case is skipped.
- **Live config fails before collection** — intentional when `BASE_URL`, `API_URL`, or `KEYCLOAK_URL`
  is missing/invalid; rerun `aspire describe` for this AppHost and export each resource URL.
- **Keycloak/user config errors** — fill every credential variable from `.env.example`. Tenant and
  principal values come from the signed token and are made ready through supported APIs.
- **Hangs waiting on a network call** — you intercepted *after* navigating. Set up
  `interceptNetworkCall(...)` **before** `page.goto(...)`.
- **`waitForProject` times out** — the read model never converged: check the Workers projection host
  and Dapr pub/sub; never paper over it with a sleep.
- **Seed returns safe-denial 404** — confirm the signed token contains a single current-tenant claim;
  global setup provisions and verifies access through supported APIs before fixtures run.
- **Self-signed cert errors** — local browser/API contexts and live token prefetch trust Aspire's
  development certificate through Playwright's `ignoreHTTPSErrors` boundary.

## Knowledge base references (TEA fragments applied)

`overview`, `fixtures-composition`, `auth-session`, `api-request`, `recurse`,
`intercept-network-call`, `data-factories`, `network-error-monitor`, `log` — from
`@seontechnologies/playwright-utils`. See also the system test design at
`_bmad-output/test-artifacts/test-design-*.md` (risks R1–R13, scenarios F5/F6, ASRs) and the
architecture/epics under `_bmad-output/planning-artifacts/`.
