import 'dotenv/config';

import { existsSync } from 'node:fs';

import { defineConfig, devices } from '@playwright/test';

/**
 * Cross-module E2E config for the Hexalith.Projects platform.
 *
 * System under test: the `Hexalith.Projects` Aspire AppHost topology (AR-22) —
 * eventstore + tenants + projects + workers + projects-ui + Keycloak + Dapr/Redis.
 * Aspire assigns the live resource ports; callers discover them with `aspire describe`
 * and provide the UI and API endpoints explicitly.
 *
 * Conventions inherited from `references/Hexalith.FrontComposer/tests/e2e`: multi-browser
 * projects, `data-testid` selectors (UX-DR28), JUnit + HTML reporters, axe-core a11y.
 */

const LIVE_APPHOST_ENABLED = process.env.E2E_LIVE_APPHOST === '1';
const BASE_URL = resolveHttpUrl('BASE_URL', LIVE_APPHOST_ENABLED, 'http://projects-ui.invalid');
const API_URL = resolveHttpUrl('API_URL', LIVE_APPHOST_ENABLED, 'http://projects-api.invalid');
const KEYCLOAK_URL = resolveHttpUrl('KEYCLOAK_URL', LIVE_APPHOST_ENABLED, 'http://security.invalid');
if (LIVE_APPHOST_ENABLED) {
  assertDistinctOrigins(BASE_URL, API_URL);
  process.env.BASE_URL = BASE_URL;
  process.env.API_URL = API_URL;
  process.env.KEYCLOAK_URL = KEYCLOAK_URL;
  requireLiveText('KEYCLOAK_CLIENT_ID');
  requireOneOf('TEST_USER_USERNAME', 'TEST_USER_EMAIL');
  requireLiveText('TEST_USER_PASSWORD');
  requireLiveText('TEST_TENANT_ID');
}
const IS_CI = !!process.env.CI;
const CHROMIUM_EXECUTABLE_PATH = resolveExecutable([
  process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
  !IS_CI ? '/usr/bin/google-chrome' : undefined,
  !IS_CI ? '/usr/bin/google-chrome-stable' : undefined,
  !IS_CI ? '/usr/bin/chromium' : undefined,
  !IS_CI ? '/usr/bin/chromium-browser' : undefined,
]);
const VIDEO_MODE = process.env.PLAYWRIGHT_DISABLE_VIDEO === '1' ? 'off' : 'retain-on-failure';
const CHROMIUM_USE = CHROMIUM_EXECUTABLE_PATH
  ? { ...devices['Desktop Chrome'], launchOptions: { executablePath: CHROMIUM_EXECUTABLE_PATH } }
  : { ...devices['Desktop Chrome'] };
const MANAGED_BROWSER_PROJECT_REQUESTED = projectRequested(['firefox', 'webkit']);
const INCLUDE_MANAGED_BROWSER_PROJECTS =
  IS_CI ||
  MANAGED_BROWSER_PROJECT_REQUESTED ||
  process.env.PLAYWRIGHT_INCLUDE_MANAGED_BROWSERS === '1' ||
  !CHROMIUM_EXECUTABLE_PATH;
const BROWSER_PROJECTS = [
  { name: 'chromium', use: CHROMIUM_USE },
  ...(INCLUDE_MANAGED_BROWSER_PROJECTS
    ? [
        { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
        { name: 'webkit', use: { ...devices['Desktop Safari'] } },
      ]
    : []),
];

function resolveExecutable(candidates: Array<string | undefined>): string | undefined {
  return candidates.find((candidate) => candidate && existsSync(candidate));
}

function projectRequested(projectNames: string[]): boolean {
  return process.argv.some((argument, index, args) => {
    if (argument === '--project') {
      return projectNames.includes(args[index + 1]);
    }

    return projectNames.some((projectName) => argument === `--project=${projectName}`);
  });
}

function resolveHttpUrl(name: string, required: boolean, fallback: string): string {
  if (!required) {
    return fallback;
  }

  const configured = process.env[name]?.trim();
  if (!configured) {
    throw new Error(`[playwright-config] ${name} must be set when E2E_LIVE_APPHOST=1.`);
  }

  let parsed: URL;
  try {
    parsed = new URL(configured);
  } catch {
    throw new Error(`[playwright-config] ${name} must be a valid absolute URL.`);
  }

  if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
    throw new Error(`[playwright-config] ${name} must use http or https.`);
  }

  if (parsed.username || parsed.password) {
    throw new Error(`[playwright-config] ${name} must not contain credentials.`);
  }

  return parsed.toString().replace(/\/$/, '');
}

function assertDistinctOrigins(uiUrl: string, apiUrl: string): void {
  if (new URL(uiUrl).origin === new URL(apiUrl).origin) {
    throw new Error('[playwright-config] BASE_URL and API_URL must identify distinct Aspire resources.');
  }
}

function requireLiveText(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`[playwright-config] ${name} must be set when E2E_LIVE_APPHOST=1.`);
  }
  process.env[name] = value;
  return value;
}

function requireOneOf(primaryName: string, legacyName: string): string {
  const value = process.env[primaryName]?.trim() || process.env[legacyName]?.trim();
  if (!value) {
    throw new Error(
      `[playwright-config] ${primaryName} must be set when E2E_LIVE_APPHOST=1 (${legacyName} is also accepted).`,
    );
  }
  process.env[primaryName] = value;
  return value;
}

export default defineConfig({
  testDir: './specs',
  fullyParallel: !LIVE_APPHOST_ENABLED,
  forbidOnly: IS_CI,
  retries: IS_CI ? 2 : 0,
  workers: LIVE_APPHOST_ENABLED ? 1 : IS_CI ? '50%' : undefined,
  timeout: 60_000,
  expect: {
    timeout: 10_000,
  },
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['junit', { outputFile: 'test-results/junit.xml' }],
  ],
  use: {
    baseURL: BASE_URL,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    // Live requests carry real bearer tokens. Do not persist them in trace archives.
    trace: LIVE_APPHOST_ENABLED ? 'off' : 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: VIDEO_MODE,
    // Keycloak dev certs are self-signed; the AppHost serves https locally.
    ignoreHTTPSErrors: true,
    // UX-DR28: role/label-based selectors via data-testid.
    testIdAttribute: 'data-testid',
    // TC-10: Blazor Auto + SignalR is a known flake source — render deterministically.
    contextOptions: {
      reducedMotion: 'reduce',
    },
  },
  // CI keeps the managed multi-browser matrix. Local machines that cannot install
  // Playwright-managed browsers can still run the no-AppHost fixture contracts
  // through system Chrome.
  projects: BROWSER_PROJECTS,
  outputDir: 'test-results',
  // Auth-session storage + token pre-fetch live here. Live runs always prefetch;
  // no-AppHost contracts never require Keycloak unless explicitly requested.
  globalSetup: './global-setup.ts',
});
