import { browserLoginCredentials } from '../support/auth/keycloak-auth-provider.js';
import { expect, liveAppHostTest, test } from '../support/merged-fixtures.js';

test.describe('Projects browser authentication', () => {
  liveAppHostTest('persists an HttpOnly server session without browser-readable tokens', async ({ page, context }) => {
    await page.goto('/');
    await page.reload();
    expect(new URL(page.url()).origin).toBe(new URL(requireEnv('BASE_URL')).origin);

    const cookies = await context.cookies(requireEnv('BASE_URL'));
    expect(cookies.some((cookie) => cookie.httpOnly)).toBe(true);
    const browserStorage = await page.evaluate(() => ({
      local: Object.entries(localStorage),
      session: Object.entries(sessionStorage),
    }));
    expect(JSON.stringify(browserStorage)).not.toMatch(/access_token|refresh_token|id_token|eyJ[a-zA-Z0-9_-]+\./i);
  });

  liveAppHostTest('challenges a missing session through real Keycloak authorization code flow', async ({ browser }) => {
    const context = await browser.newContext({
      ignoreHTTPSErrors: true,
      storageState: { cookies: [], origins: [] },
    });
    try {
      const page = await context.newPage();
      let browserAuthorizationHeaderObserved = false;
      page.on('request', (request) => {
        browserAuthorizationHeaderObserved ||= Boolean(request.headers().authorization);
      });

      await page.goto(requireEnv('BASE_URL'), { waitUntil: 'domcontentloaded' });
      expect(new URL(page.url()).origin).toBe(new URL(requireEnv('KEYCLOAK_URL')).origin);
      const credentials = browserLoginCredentials();
      await page.locator('#username').fill(credentials.username);
      await page.locator('#password').fill(credentials.password);
      await page.locator('#kc-login').click();
      await page.waitForURL((url) => url.origin === new URL(requireEnv('BASE_URL')).origin);

      expect(browserAuthorizationHeaderObserved).toBe(false);
      expect((await context.cookies(requireEnv('BASE_URL'))).some((cookie) => cookie.httpOnly)).toBe(true);
    } finally {
      await context.close();
    }
  });
});

function requireEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[projects-authentication] ${name} is required.`);
  return value;
}
