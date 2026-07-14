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
- Aspire CLI `13.4.6`, .NET SDK `10.0.300`, `jq`, Dapr, and a Docker-compatible runtime for the live lane.

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

Run lifecycle commands from the repository root. The `HexalithCommonsRoot` override makes the
root-level Commons checkout authoritative for nested sibling project references. Always pass the
exact AppHost path to `wait`, `describe`, and `stop`; this is required when another AppHost is running.

```bash
APPHOST="$PWD/src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj"
export HexalithCommonsRoot="$PWD/references/Hexalith.Commons"

aspire start --apphost "$APPHOST" --format Json --non-interactive
cleanup_apphost() { aspire stop --apphost "$APPHOST" --non-interactive >/dev/null 2>&1 || true; }
trap cleanup_apphost EXIT
aspire wait projects-ui --apphost "$APPHOST" --timeout 180 --non-interactive

export BASE_URL="$(aspire describe --apphost "$APPHOST" --format Json --non-interactive \
  | jq -r '.resources[] | select(.displayName == "projects-ui") | .urls[] | select(.name == "http") | .url')"
export API_URL="$(aspire describe --apphost "$APPHOST" --format Json --non-interactive \
  | jq -r '.resources[] | select(.displayName == "projects") | .urls[] | select(.name == "http") | .url')"
export KEYCLOAK_URL="$(aspire describe --apphost "$APPHOST" --format Json --non-interactive \
  | jq -r '.resources[] | select(.displayName == "security") | .urls[] | select(.name == "http") | .url')"

test -n "$BASE_URL" && test -n "$API_URL" && test -n "$KEYCLOAK_URL"
```

Configure the non-URL values from `.env.example` or the shell, then run from `tests/e2e`:

```bash
cd tests/e2e
export E2E_LIVE_APPHOST=1
export KEYCLOAK_REALM=hexalith
export KEYCLOAK_CLIENT_ID=hexalith-eventstore
export TEST_USER_USERNAME=<local-realm-username>
export TEST_USER_PASSWORD=<local-realm-password>
export TEST_TENANT_ID=<projected-tenant-id>

npm run typecheck
npm run test:chromium
```

The UI (`BASE_URL`) and API (`API_URL`) are intentionally different Aspire resources. Stop only this
AppHost after the run (the absolute `APPHOST` value remains valid after changing directories):

```bash
aspire stop --apphost "$APPHOST" --non-interactive
trap - EXIT
```

## Authentication (real Keycloak / OIDC)

Per **AR-19** and the test design, E2E proves runtime security with **real Keycloak tokens**
(realm `hexalith`) — synthetic JWTs are unit/integration only. Configure in `.env`:

```
KEYCLOAK_URL=<discovered-security-url>
KEYCLOAK_REALM=hexalith
KEYCLOAK_CLIENT_ID=hexalith-eventstore
TEST_USER_USERNAME=...        TEST_USER_PASSWORD=...        TEST_TENANT_ID=...
```

The `keycloakAuthProvider` does an OAuth2 resource-owner password grant, persists the token to disk
(`.auth/`, gitignored), and renews 30 s before expiry. Additional users for cross-tenant negatives
use `E2E_USER_<NAME>_USERNAME` / `E2E_USER_<NAME>_PASSWORD` and
`authOptions.userIdentifier`; legacy `*_EMAIL` names remain accepted.

## Architecture

```
tests/e2e/
├── playwright.config.ts        # explicit live-route validation, browsers, data-testid, reduced-motion
├── global-setup.ts             # auth-session storage + Keycloak provider (live prefetch automatic)
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
- **Isolation:** live tests use the configured projected tenant, generate unique project metadata,
  and run with one worker because several cross-module fixture IDs are shared. Offline contracts stay parallel.
- **Cleanup:** seeded data is archived on teardown; tokens persist to disk but never to git.
- **Determinism:** `reducedMotion: 'reduce'`; deterministic anchors before assertions; flaky
  T3/E2E goes to a **quarantine lane**, never silenced (R8).
- **Security/privacy:** real Keycloak tokens; assert safe-denial (404 for unauthorized == nonexistent)
  and NoPayloadLeakage (no transcripts/secrets/tokens in any output) on every relevant journey.

## CI integration

- **Lane:** the current scheduled job runs the offline contract lane. A recurring live AppHost lane
  still requires managed credentials, deterministic sibling fixtures, and lifecycle ownership; that
  adoption is tracked as deferred work rather than silently implied here.
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
- **Live journeys show as skipped** — set `E2E_LIVE_APPHOST=1` and all three discovered URLs. The
  definition-time gate prevents disabled tests from resolving Keycloak or seeded-project fixtures.
- **Live config fails before collection** — intentional when `BASE_URL`, `API_URL`, or `KEYCLOAK_URL`
  is missing/invalid; rerun `aspire describe` for this AppHost and export each resource URL.
- **Keycloak/user/tenant config errors** — fill every live variable from `.env.example`. Live mode
  validates them before collection and always prefetches a fresh token for the discovered UI origin.
- **Hangs waiting on a network call** — you intercepted *after* navigating. Set up
  `interceptNetworkCall(...)` **before** `page.goto(...)`.
- **`waitForProject` times out** — the read model never converged: check the Workers projection host
  and Dapr pub/sub; never paper over it with a sleep.
- **Seed returns safe-denial 404** — the Keycloak user exists but `TEST_TENANT_ID` is not present in
  the Projects tenant-access projection. Provision/replay that tenant before expecting seeded journeys
  to pass; do not substitute a random tenant ID.
- **Self-signed cert errors** — local browser/API contexts and live token prefetch trust Aspire's
  development certificate through Playwright's `ignoreHTTPSErrors` boundary.

## Knowledge base references (TEA fragments applied)

`overview`, `fixtures-composition`, `auth-session`, `api-request`, `recurse`,
`intercept-network-call`, `data-factories`, `network-error-monitor`, `log` — from
`@seontechnologies/playwright-utils`. See also the system test design at
`_bmad-output/test-artifacts/test-design-*.md` (risks R1–R13, scenarios F5/F6, ASRs) and the
architecture/epics under `_bmad-output/planning-artifacts/`.
