import { mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';

import { chromium, request } from '@playwright/test';

import { browserSessionStoragePath } from './support/auth/browser-session.js';
import {
  browserLoginCredentials,
  requestKeycloakAccessToken,
} from './support/auth/keycloak-auth-provider.js';
import { authorityFromAccessToken, ensureProjectsTenantAccess } from './support/helpers/tenant-access-readiness.js';

/** Establishes API readiness and a real browser authorization-code session for the live lane. */
async function globalSetup(): Promise<void> {
  if (process.env.E2E_LIVE_APPHOST !== '1') return;

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

  await createBrowserSession();
}

async function createBrowserSession(): Promise<void> {
  const executablePath = process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH?.trim();
  const browser = await chromium.launch(executablePath ? { executablePath } : undefined);
  const context = await browser.newContext({ ignoreHTTPSErrors: true });
  try {
    const page = await context.newPage();
    const baseUrl = requireEnv('BASE_URL');
    const credentials = browserLoginCredentials();
    await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
    await page.locator('#username').fill(credentials.username);
    await page.locator('#password').fill(credentials.password);
    await page.locator('#kc-login').click();
    await page.waitForURL((url) => url.origin === new URL(baseUrl).origin, { timeout: 30_000 });

    const browserStorage = await page.evaluate(() => ({
      local: Object.keys(localStorage),
      session: Object.keys(sessionStorage),
    }));
    if ([...browserStorage.local, ...browserStorage.session].some((key) => /token/i.test(key))) {
      throw new Error('[global-setup] browser session exposed a token-bearing storage key.');
    }

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
