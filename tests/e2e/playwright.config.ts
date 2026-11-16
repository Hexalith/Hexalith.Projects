import 'dotenv/config';

import { existsSync } from 'node:fs';

import { chromium, defineConfig, devices } from '@playwright/test';

import { browserSessionStoragePath } from './support/auth/browser-session.js';
import { createBrowserProjects } from './support/helpers/browser-configuration.js';

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
  process.env.KEYCLOAK_CLIENT_ID = requireLiveText('KEYCLOAK_CLIENT_ID');
  process.env.EVENTSTORE_API_URL = requireLiveText('EVENTSTORE_API_URL');
  process.env.FIXTURE_API_URL = requireLiveText('FIXTURE_API_URL');
  process.env.E2E_RUN_ID = requireLiveText('E2E_RUN_ID');
  process.env.TEST_USER_USERNAME = requireOneOf('TEST_USER_USERNAME', 'TEST_USER_EMAIL');
  process.env.TEST_USER_PASSWORD = requireLiveText('TEST_USER_PASSWORD');
}
const IS_CI = !!process.env.CI;
const PACKAGE_FIXTURES = process.env.E2E_PACKAGE_FIXTURES === '1';
// Live requests and fixture data stay in private driver scratch.
const VIDEO_MODE = LIVE_APPHOST_ENABLED || process.env.PLAYWRIGHT_DISABLE_VIDEO === '1' ? 'off' : 'retain-on-failure';
const SCREENSHOT_MODE = LIVE_APPHOST_ENABLED ? 'off' : 'only-on-failure';
const BROWSER_PROJECTS = createBrowserProjects({
  env: process.env, args: process.argv, exists: existsSync,
  managedExecutablePath: () => chromium.executablePath(), devices,
});

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
  return value;
}

function requireOneOf(primaryName: string, legacyName: string): string {
  const value = process.env[primaryName]?.trim() || process.env[legacyName]?.trim();
  if (!value) {
    throw new Error(
      `[playwright-config] ${primaryName} must be set when E2E_LIVE_APPHOST=1 (${legacyName} is also accepted).`,
    );
  }
  return value;
}

export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: IS_CI,
  retries: IS_CI ? 2 : 0,
  workers: LIVE_APPHOST_ENABLED ? 2 : IS_CI ? '50%' : undefined,
  timeout: 60_000,
  expect: {
    timeout: 10_000,
  },
  reporter: [
    ['list'],
    ...(!PACKAGE_FIXTURES ? [
      ['html', { outputFolder: 'playwright-report', open: 'never' }],
      ['junit', { outputFile: 'test-results/junit.xml' }],
    ] as const : []),
    ...(LIVE_APPHOST_ENABLED ? [['./reporters/zero-live-skip-reporter.ts']] as const : []),
  ],
  use: {
    baseURL: BASE_URL,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    // Live requests carry real bearer tokens. Do not persist them in trace archives.
    trace: LIVE_APPHOST_ENABLED ? 'off' : 'retain-on-failure',
    screenshot: SCREENSHOT_MODE,
    video: VIDEO_MODE,
    storageState: LIVE_APPHOST_ENABLED ? browserSessionStoragePath : undefined,
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
  outputDir: PACKAGE_FIXTURES ? requireLiveText('E2E_PACKAGE_OUTPUT') : 'test-results',
  // Live global setup establishes supported tenant access and a real OIDC cookie session. Live mode
  // always runs two isolated workers and the zero-live-skip reporter fails empty or skipped runs.
  globalSetup: './global-setup.ts',
});
