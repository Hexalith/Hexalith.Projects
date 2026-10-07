import type { Page } from '@playwright/test';

import { browserLoginCredentials } from '../support/auth/keycloak-auth-provider.js';
import {
  assertServerOnlySession,
  browserSessionStoragePath,
  inspectBrowserSession,
} from '../support/auth/browser-session.js';
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
      // Keycloak may redirect the authorize request to its login-actions form, so the code-flow
      // parameters are asserted on the authorize request in the redirect chain, not the landed page.
      const authorizeRequest = page.waitForRequest((request) =>
        new URL(request.url()).pathname.endsWith('/protocol/openid-connect/auth'));

      await page.goto(requireEnv('BASE_URL'), { waitUntil: 'domcontentloaded' });
      const authorize = new URL((await authorizeRequest).url());
      expect(authorize.origin).toBe(new URL(requireEnv('KEYCLOAK_URL')).origin);
      expect(new URL(page.url()).origin).toBe(new URL(requireEnv('KEYCLOAK_URL')).origin);
      expect(authorize.searchParams.get('client_id')).toBe('hexalith-projects-ui');
      if (authorize.searchParams.has('request_uri')) {
        // Pushed authorization request (RFC 9126): the server sends response_type, PKCE, and redirect
        // parameters over the back channel, so the browser carries only an opaque request URI.
        expect(authorize.searchParams.get('request_uri')).toMatch(/^urn:ietf:params:oauth:request_uri:/);
        expect(authorize.searchParams.has('code_challenge')).toBe(false);
      } else {
        expect(authorize.searchParams.get('response_type')).toBe('code');
        expect(authorize.searchParams.get('code_challenge_method')).toBe('S256');
      }

      const credentials = browserLoginCredentials();
      await page.locator('#username').fill(credentials.username);
      await page.locator('#password').fill(credentials.password);
      await page.locator('#kc-login').click();
      await page.waitForURL((url) => url.origin === new URL(requireEnv('BASE_URL')).origin);
      await page.reload({ waitUntil: 'domcontentloaded' });
      expect(new URL(page.url()).origin).toBe(new URL(requireEnv('BASE_URL')).origin);

      const traffic = await browserTraffic.settle();
      expect(traffic.authorizationHeaders).toBe(0);
      expect(traffic.tokenBearingUrls).toBe(0);
      assertServerOnlySession(await inspectBrowserSession(page, context, requireEnv('BASE_URL')), 'projects-authentication');
    } finally {
      await context.close();
    }
  });

  liveAppHostTest('challenges again through Keycloak when the server-session cookie is invalidated', async ({ browser }) => {
    const baseUrl = requireEnv('BASE_URL');
    const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: browserSessionStoragePath });
    try {
      // Replace every FrontComposer session cookie (including chunks) with a value the server cannot unprotect.
      const sessionCookies = (await context.cookies(baseUrl)).filter((cookie) => /FrontComposer/i.test(cookie.name));
      expect(sessionCookies.length).toBeGreaterThan(0);
      await context.addCookies(sessionCookies.map((cookie) => ({ ...cookie, value: 'invalidated-session' })));

      const page = await context.newPage();
      const browserTraffic = observeBrowserTraffic(page);
      const authorizeRequest = page.waitForRequest((request) =>
        new URL(request.url()).pathname.endsWith('/protocol/openid-connect/auth'));

      await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
      const authorize = new URL((await authorizeRequest).url());
      expect(authorize.origin).toBe(new URL(requireEnv('KEYCLOAK_URL')).origin);
      expect(authorize.searchParams.get('client_id')).toBe('hexalith-projects-ui');

      // The authorize request proves the invalidated cookie was not accepted as a UI session. Keycloak may
      // complete the challenge from its own SSO session; when it does, the re-issued session must hold.
      // Wait for a settled destination: the UI past its OIDC callback, or the Keycloak login form.
      const uiOrigin = new URL(baseUrl).origin;
      const settled = await Promise.race([
        page
          .waitForURL((url) => url.origin === uiOrigin && !/^\/(signin-|authentication\/)/.test(url.pathname), {
            waitUntil: 'load',
            timeout: 30_000,
          })
          .then(() => 'ui' as const, () => 'timeout' as const),
        page
          .locator('#kc-login')
          .waitFor({ state: 'visible', timeout: 30_000 })
          .then(() => 'login' as const, () => 'timeout' as const),
      ]);
      expect(settled).not.toBe('timeout');
      if (settled === 'ui') {
        await page.reload({ waitUntil: 'load' });
        expect(new URL(page.url()).origin).toBe(uiOrigin);
      }
      const traffic = await browserTraffic.settle();
      expect(traffic.authorizationHeaders).toBe(0);
      expect(traffic.tokenBearingUrls).toBe(0);
      const exposure = await inspectBrowserSession(page, context, baseUrl);
      expect(exposure.tokenLikeStorageKeys).toEqual([]);
      expect(exposure.tokenLikeCookieNames).toEqual([]);
      expect(exposure.scriptReadableSessionCookies).toEqual([]);
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

    const traffic = await browserTraffic.settle();
    expect(traffic.apiOriginRequests).toBe(0);
    expect(traffic.authorizationHeaders).toBe(0);
    expect(traffic.tokenBearingUrls).toBe(0);
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

interface BrowserTraffic {
  apiOriginRequests: number;
  authorizationHeaders: number;
  tokenBearingUrls: number;
}

/**
 * Counts browser-originated traffic properties without retaining URLs, headers, or bodies. Uses
 * `allHeaders()` because `headers()` omits security-related headers such as Authorization, which
 * would make the no-Authorization assertion pass vacuously.
 */
function observeBrowserTraffic(page: Page): { settle: () => Promise<BrowserTraffic> } {
  const apiOrigin = new URL(requireEnv('API_URL')).origin;
  const traffic: BrowserTraffic = { apiOriginRequests: 0, authorizationHeaders: 0, tokenBearingUrls: 0 };
  const pending: Promise<void>[] = [];
  page.on('request', (request) => {
    const url = new URL(request.url());
    if (url.origin === apiOrigin) traffic.apiOriginRequests += 1;
    if (/(?:^|[?#&])(?:access_token|id_token|refresh_token)=/.test(`${url.search}${url.hash}`)) traffic.tokenBearingUrls += 1;
    pending.push(request.allHeaders().then(
      (headers) => {
        if (headers.authorization) traffic.authorizationHeaders += 1;
      },
      () => undefined,
    ));
  });
  return {
    settle: async () => {
      await Promise.all(pending);
      return traffic;
    },
  };
}

function requireEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[projects-authentication] ${name} is required.`);
  return value;
}
