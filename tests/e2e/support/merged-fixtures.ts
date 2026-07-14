import { test as base, mergeTests, expect } from '@playwright/test';
import { test as apiRequestFixture } from '@seontechnologies/playwright-utils/api-request/fixtures';
import { test as recurseFixture } from '@seontechnologies/playwright-utils/recurse/fixtures';
import { test as logFixture } from '@seontechnologies/playwright-utils/log/fixtures';
import { test as interceptFixture } from '@seontechnologies/playwright-utils/intercept-network-call/fixtures';
import { test as networkErrorMonitorFixture } from '@seontechnologies/playwright-utils/network-error-monitor/fixtures';
import { createAuthFixtures, setAuthProvider } from '@seontechnologies/playwright-utils/auth-session';
import type { AuthFixtures } from '@seontechnologies/playwright-utils/auth-session';

import keycloakAuthProvider from './auth/keycloak-auth-provider.js';
import { createTenantContext } from './factories/tenant-factory.js';
import { type ProjectFixtures, seedActiveProject } from './fixtures/projects-fixtures.js';
import type { ApiRequest } from './helpers/projects-api-client.js';

/**
 * Single project test object (the fragment "merged-fixtures" pattern).
 *
 * Composition order matters: we merge the playwright-utils fixtures first, THEN extend
 * with project-domain fixtures so they can depend on `apiRequest` / `authToken` / `recurse`.
 * Import `{ test, expect }` from THIS file in every spec.
 *
 * Available fixtures:
 *  - apiRequest          typed HTTP client (api-request)
 *  - authToken / authOptions   real Keycloak token (auth-session)
 *  - recurse             deterministic polling (recurse) — use for read-model convergence
 *  - log                 report-integrated step logging (log)
 *  - interceptNetworkCall  network-first spy/stub (intercept-network-call)
 *  - networkErrorMonitor   automatic 4xx/5xx detection (network-error-monitor)
 *  - tenantContext       configured projected tenant with per-test metadata (custom)
 *  - seededProject       active project converged in the read model (custom)
 */

// Register the auth provider at module load (before any fixture resolves a token).
setAuthProvider(keycloakAuthProvider);

// auth-session ships fixture factories (not a ready test object) — build one from base.
// playwright-utils returns these as a plain object that isn't expressed in Playwright's
// `Fixtures<>` types, so strict tsc can't infer the added test args. Pin them explicitly
// via the library's own `AuthFixtures` type so `authToken` flows through `mergeTests`;
// the runtime object is exactly what the auth-session fragment documents.
const authFixture = base.extend<AuthFixtures>(
  createAuthFixtures() as unknown as Parameters<typeof base.extend<AuthFixtures>>[0],
);

const utilsTest = mergeTests(
  apiRequestFixture,
  authFixture,
  recurseFixture,
  logFixture,
  interceptFixture,
  networkErrorMonitorFixture,
);

export const test = utilsTest.extend<ProjectFixtures>({
  apiRequest: async ({ apiRequest }, use) => {
    const projectsApiRequest = (<T = unknown>(params: Parameters<ApiRequest>[0]) =>
      apiRequest<T>({
        ...params,
        baseUrl: params.baseUrl?.trim() || requireProjectsApiUrl(),
      })) as ApiRequest;
    await use(projectsApiRequest as Parameters<typeof use>[0]);
  },

  tenantContext: async ({}, use) => {
    const tenantId = process.env.E2E_LIVE_APPHOST === '1' ? requireLiveFixtureEnv('TEST_TENANT_ID') : undefined;
    await use(createTenantContext(tenantId ? { tenantId } : undefined));
  },

  seededProject: async ({ apiRequest, authToken, recurse, tenantContext }, use) => {
    const { project, cleanup } = await seedActiveProject({ apiRequest, authToken, recurse, tenantContext });
    await use(project);
    await cleanup();
  },
});

/**
 * Registers AppHost-backed tests as normal tests only when the live lane is explicit.
 * Selecting `test.skip` at definition time prevents disabled cases from resolving
 * real-auth and seeded-project fixtures.
 */
export const liveAppHostTest = (
  process.env.E2E_LIVE_APPHOST === '1' ? test : test.skip
) as typeof test;

function requireProjectsApiUrl(): string {
  return requireLiveFixtureEnv('API_URL');
}

function requireLiveFixtureEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`[projects-fixtures] ${name} must be set for AppHost-backed tests.`);
  }
  return value;
}

export { expect };
