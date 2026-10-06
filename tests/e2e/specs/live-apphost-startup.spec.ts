import { expect, liveAppHostTest, test } from '../support/merged-fixtures.js';

/**
 * Startup smoke for the managed lane. Every URL comes from the runner's single `aspire describe`
 * capture; nothing here assumes a port. The fixture control resource exists only because the runner
 * enabled the explicit live-fixture profile.
 */
test.describe('Live AppHost startup', () => {
  liveAppHostTest('serves the protected UI, Projects API, and profile-scoped fixture control resource', async ({ page, request }) => {
    const navigation = await page.goto('/');
    expect(navigation?.status()).toBe(200);
    expect(new URL(page.url()).origin).toBe(new URL(requireEnv('BASE_URL')).origin);

    const projectsAlive = await request.get(`${requireEnv('API_URL')}/alive`, { failOnStatusCode: false });
    expect(projectsAlive.status()).toBe(200);
    const fixtures = await request.get(`${requireEnv('FIXTURE_API_URL')}/health`);
    expect(fixtures.status()).toBe(200);
    expect(await fixtures.json()).toEqual({ role: 'control', status: 'ready' });
  });

  liveAppHostTest('resolves every live endpoint to a distinct dynamically assigned origin', async () => {
    const origins = ['BASE_URL', 'API_URL', 'EVENTSTORE_API_URL', 'KEYCLOAK_URL', 'FIXTURE_API_URL'].map(
      (name) => new URL(requireEnv(name)).origin,
    );
    expect(new Set(origins).size).toBe(origins.length);
  });

  liveAppHostTest('rejects anonymous browser and fixture-graph access before any session exists', async ({ browser, request }) => {
    const anonymous = await browser.newContext({ ignoreHTTPSErrors: true, storageState: { cookies: [], origins: [] } });
    try {
      const page = await anonymous.newPage();
      await page.goto(requireEnv('BASE_URL'), { waitUntil: 'domcontentloaded' });
      expect(new URL(page.url()).origin).toBe(new URL(requireEnv('KEYCLOAK_URL')).origin);
    } finally {
      await anonymous.close();
    }

    // The control resource exposes graph metadata only by exact graph identity; listing is not routed.
    const listing = await request.get(`${requireEnv('FIXTURE_API_URL')}/api/v1/live-fixtures/graphs`, { failOnStatusCode: false });
    expect([404, 405]).toContain(listing.status());
  });
});

function requireEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[live-apphost-startup] ${name} is required.`);
  return value;
}
