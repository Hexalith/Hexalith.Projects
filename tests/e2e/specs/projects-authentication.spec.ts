import type { Page } from '@playwright/test';

import { browserLoginCredentials } from '../support/auth/keycloak-auth-provider.js';
import { assertServerOnlySession, inspectBrowserSession } from '../support/auth/browser-session.js';
import { expect, liveAppHostTest, test } from '../support/merged-fixtures.js';

/**
 * Browser session proof for the Projects UI. The UI is a confidential Keycloak authorization-code
 * client whose session is an HttpOnly FrontComposer cookie; outbound Projects calls are made by the
 * server with the signed-in user's relayed token. The ROPC client remains an API-fixture seam only.
 */
test.describe('Projects browser authentication', () => {
  liveAppHostTest('persists an HttpOnly server session across reload without browser-readable tokens', async ({ page, context }) => {
    const baseUrl = requireEnv('BASE_URL');
    await page.goto('/');
    await page.reload();
    expect(new URL(page.url()).origin).toBe(new URL(baseUrl).origin);

    const exposure = await inspectBrowserSession(page, context, baseUrl);
    expect(exposure.httpOnlyCookieCount).toBeGreaterThan(0);
    expect(() => assertServerOnlySession(exposure, 'projects-authentication')).not.toThrow();
  });

  liveAppHostTest('challenges a missing session through the real Keycloak authorization-code flow', async ({ browser }) => {
    const context = await browser.newContext({
      ignoreHTTPSErrors: true,
      storageState: { cookies: [], origins: [] },
    });
    try {
      const page = await context.newPage();
      const browserTraffic = observeBrowserTraffic(page);

      await page.goto(requireEnv('BASE_URL'), { waitUntil: 'domcontentloaded' });
      const authorize = new URL(page.url());
      expect(authorize.origin).toBe(new URL(requireEnv('KEYCLOAK_URL')).origin);
      expect(authorize.pathname).toMatch(/\/protocol\/openid-connect\/auth$/);
      expect(authorize.searchParams.get('response_type')).toBe('code');
      expect(authorize.searchParams.get('client_id')).toBe('hexalith-projects-ui');
      expect(authorize.searchParams.get('code_challenge_method')).toBe('S256');

      const credentials = browserLoginCredentials();
      await page.locator('#username').fill(credentials.username);
      await page.locator('#password').fill(credentials.password);
      await page.locator('#kc-login').click();
      await page.waitForURL((url) => url.origin === new URL(requireEnv('BASE_URL')).origin);
      await page.reload({ waitUntil: 'domcontentloaded' });
      expect(new URL(page.url()).origin).toBe(new URL(requireEnv('BASE_URL')).origin);

      expect(browserTraffic.authorizationHeaders).toBe(0);
      expect(browserTraffic.tokenBearingUrls).toBe(0);
      assertServerOnlySession(await inspectBrowserSession(page, context, requireEnv('BASE_URL')), 'projects-authentication');
    } finally {
      await context.close();
    }
  });

  liveAppHostTest('renders protected Project data through server-side token relay only', async ({ page, context, seededProject }) => {
    const browserTraffic = observeBrowserTraffic(page);

    await page.goto(`/projects/${seededProject.projectId}`);
    // The Projects API rejects anonymous calls, so rendering the seeded name proves the server relayed
    // the signed-in user's token on the outbound Projects call.
    await expect(page.getByTestId('project-detail-name')).toHaveText(seededProject.name);
    await page.reload();
    await expect(page.getByTestId('project-detail-name')).toHaveText(seededProject.name);

    expect(browserTraffic.apiOriginRequests).toBe(0);
    expect(browserTraffic.authorizationHeaders).toBe(0);
    expect(browserTraffic.tokenBearingUrls).toBe(0);
    assertServerOnlySession(await inspectBrowserSession(page, context, requireEnv('BASE_URL')), 'projects-authentication');
  });

  liveAppHostTest('keeps the Projects API closed to anonymous callers', async ({ request }) => {
    const response = await request.get(`${requireEnv('API_URL')}/api/v1/projects`, {
      headers: { 'X-Hexalith-Tenant-Id': requireEnv('TEST_TENANT_ID') },
      failOnStatusCode: false,
    });
    expect([401, 403, 404]).toContain(response.status());
  });
});

/** Counts browser-originated traffic properties without retaining URLs, headers, or bodies. */
function observeBrowserTraffic(page: Page): { apiOriginRequests: number; authorizationHeaders: number; tokenBearingUrls: number } {
  const apiOrigin = new URL(requireEnv('API_URL')).origin;
  const traffic = { apiOriginRequests: 0, authorizationHeaders: 0, tokenBearingUrls: 0 };
  page.on('request', (request) => {
    const url = new URL(request.url());
    if (url.origin === apiOrigin) traffic.apiOriginRequests += 1;
    if (request.headers().authorization) traffic.authorizationHeaders += 1;
    if (/(?:^|[?#&])(?:access_token|id_token|refresh_token)=/.test(`${url.search}${url.hash}`)) traffic.tokenBearingUrls += 1;
  });
  return traffic;
}

function requireEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[projects-authentication] ${name} is required.`);
  return value;
}
