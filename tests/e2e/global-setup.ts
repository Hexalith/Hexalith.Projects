import { mkdir, rm } from 'node:fs/promises';
import { dirname } from 'node:path';

import { chromium, request, type FullConfig } from '@playwright/test';

import {
  assertServerOnlySession,
  browserSessionStoragePath,
  inspectBrowserSession,
} from './support/auth/browser-session.js';
import {
  browserLoginCredentials,
  requestKeycloakAccessToken,
} from './support/auth/keycloak-auth-provider.js';
import { authorityFromAccessToken, ensureProjectsTenantAccess } from './support/helpers/tenant-access-readiness.js';

/**
 * Establishes token-derived tenant readiness once per Playwright invocation, then a real browser
 * authorization-code session saved as storage state: the UI-origin HttpOnly server-session cookies
 * plus the Keycloak-origin login cookies, never a token. The managed runner deletes it after each run.
 */
async function globalSetup(config: FullConfig): Promise<void> {
  if (process.env.E2E_LIVE_APPHOST !== '1') return;

  // Never reuse a previous run's session; a stale cookie must not mask a broken login flow.
  await rm(browserSessionStoragePath, { force: true });

  const apiContext = await request.newContext({ ignoreHTTPSErrors: true });
  try {
    const authToken = await requestKeycloakAccessToken(apiContext);
    const authority = authorityFromAccessToken(authToken, process.env.TEST_TENANT_ID);
    process.env.TEST_TENANT_ID = authority.tenantId;
    process.env.TEST_PRINCIPAL_ID = authority.principalId;

    const eventStore = await request.newContext({ baseURL: requireEnv('EVENTSTORE_API_URL'), ignoreHTTPSErrors: true });
    const projects = await request.newContext({ baseURL: requireEnv('API_URL'), ignoreHTTPSErrors: true });
    try {
      await ensureProjectsTenantAccess({
        eventStore,
        projects,
        authToken,
        authority,
        runId: requireEnv('E2E_RUN_ID'),
      });
    } finally {
      await eventStore.dispose();
      await projects.dispose();
    }
  } finally {
    await apiContext.dispose();
  }

  await createBrowserSession(config);
}

async function createBrowserSession(config: FullConfig): Promise<void> {
  const chromiumProject = config.projects.find((project) => project.name === 'chromium');
  const browser = await chromium.launch(chromiumProject?.use.launchOptions);
  const context = await browser.newContext({ ignoreHTTPSErrors: true });
  try {
    const page = await context.newPage();
    const baseUrl = requireEnv('BASE_URL');
    const uiOrigin = new URL(baseUrl).origin;
    const keycloakOrigin = new URL(requireEnv('KEYCLOAK_URL')).origin;
    const credentials = browserLoginCredentials();

    // An anonymous protected route must be challenged through the real Keycloak code flow.
    await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
    if (new URL(page.url()).origin !== keycloakOrigin) {
      throw new Error('[global-setup] the protected Projects UI did not challenge through Keycloak.');
    }
    await page.locator('#username').fill(credentials.username);
    await page.locator('#password').fill(credentials.password);
    await page.locator('#kc-login').click();
    await page.waitForURL((url) => url.origin === uiOrigin, { timeout: 30_000 });

    // The session must survive a reload without another challenge.
    await page.reload({ waitUntil: 'domcontentloaded' });
    if (new URL(page.url()).origin !== uiOrigin) {
      throw new Error('[global-setup] the Projects UI session did not persist across reload.');
    }
    assertServerOnlySession(await inspectBrowserSession(page, context, baseUrl), 'global-setup');

    await mkdir(dirname(browserSessionStoragePath), { recursive: true });
    await context.storageState({ path: browserSessionStoragePath });
  } finally {
    await context.close();
    await browser.close();
  }
}

function requireEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[global-setup] ${name} is required for the live AppHost lane.`);
  return value;
}

export default globalSetup;
