import { expect, liveAppHostTest, test } from '../support/merged-fixtures.js';

test.describe('Live AppHost startup', () => {
  liveAppHostTest('serves the protected UI, Projects API, and profile-scoped fixture control resource', async ({ page, request }) => {
    const navigation = await page.goto('/');
    expect(navigation?.status()).toBe(200);
    expect(new URL(page.url()).origin).toBe(new URL(requireEnv('BASE_URL')).origin);

    const projects = await request.get(`${requireEnv('API_URL')}/health`, { failOnStatusCode: false });
    expect(projects.status()).toBeLessThan(500);
    const fixtures = await request.get(`${requireEnv('FIXTURE_API_URL')}/health`);
    expect(fixtures.status()).toBe(200);
  });
});

function requireEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[live-apphost-startup] ${name} is required.`);
  return value;
}
