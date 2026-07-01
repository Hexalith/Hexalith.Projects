import { existsSync } from 'node:fs';

import { defineConfig, devices } from '@playwright/test';

/**
 * Cross-module E2E config for the Hexalith.Projects platform.
 *
 * System under test (greenfield): the `Hexalith.Projects` Aspire AppHost topology
 * (AR-22) — eventstore + tenants + projects + workers + projects-ui + Keycloak +
 * Dapr/Redis. Until Story 1.1 scaffolds that AppHost, `BASE_URL` points at the
 * planned projects-ui port and the local web server is OFF by default (see below).
 *
 * Conventions inherited from `references/Hexalith.FrontComposer/tests/e2e`: multi-browser
 * projects, `data-testid` selectors (UX-DR28), JUnit + HTML reporters, axe-core a11y.
 */

// Planned projects-ui dev URL. Override via env once the AppHost assigns a real port.
const BASE_URL = process.env.BASE_URL ?? 'https://localhost:7280';
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

// Greenfield guard: the Hexalith.Projects AppHost does not exist yet, so the web
// server is launched only when explicitly opted in (E2E_WEBSERVER=1). When enabled,
// E2E_WEBSERVER_CMD must launch the AppHost (or `aspire run`) bound to BASE_URL.
const WEBSERVER_ENABLED = process.env.E2E_WEBSERVER === '1';
const WEBSERVER_CMD =
  process.env.E2E_WEBSERVER_CMD ??
  // Placeholder — wire to the real AppHost once Story 1.1 lands, e.g.:
  // 'dotnet run --project ../../Hexalith.Projects/src/Hexalith.Projects.AppHost --no-launch-profile'
  'echo "set E2E_WEBSERVER_CMD to the Hexalith.Projects AppHost launch command" && exit 1';

export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: IS_CI,
  retries: IS_CI ? 2 : 0,
  workers: IS_CI ? '50%' : undefined,
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
    // Keep artifacts for failures and retries (the skill's "retain-on-failure-and-retries"
    // intent expressed with valid Playwright trace modes).
    trace: 'retain-on-failure',
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
  // Playwright-managed browsers, such as unsupported preview Linux images, can
  // still run the greenfield fixture contracts through system Chrome.
  projects: BROWSER_PROJECTS,
  outputDir: 'test-results',
  // Auth-session storage + token pre-fetch live here (defensive: no-op when Keycloak
  // is unreachable so the framework smoke check still runs).
  globalSetup: './global-setup.ts',
  webServer: WEBSERVER_ENABLED
    ? {
        command: WEBSERVER_CMD,
        url: BASE_URL,
        reuseExistingServer: !IS_CI,
        timeout: 180_000,
        ignoreHTTPSErrors: true,
        env: {
          ASPNETCORE_ENVIRONMENT: 'Test',
        },
      }
    : undefined,
});
