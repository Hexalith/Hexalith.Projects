import { test as base, mergeTests, expect } from '@playwright/test';
import { test as apiRequestFixture } from '@seontechnologies/playwright-utils/api-request/fixtures';
import { test as recurseFixture } from '@seontechnologies/playwright-utils/recurse/fixtures';
import { test as logFixture } from '@seontechnologies/playwright-utils/log/fixtures';
import { test as interceptFixture } from '@seontechnologies/playwright-utils/intercept-network-call/fixtures';
import { test as networkErrorMonitorFixture } from '@seontechnologies/playwright-utils/network-error-monitor/fixtures';
import { createAuthFixtures, setAuthProvider } from '@seontechnologies/playwright-utils/auth-session';

import keycloakAuthProvider from './auth/keycloak-auth-provider.js';
import { createTenantContext } from './factories/tenant-factory.js';
import { type ProjectFixtures, seedActiveProject } from './fixtures/projects-fixtures.js';

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
 *  - tenantContext       fresh isolated tenant (custom)
 *  - seededProject       active project converged in the read model (custom)
 */

// Register the auth provider at module load (before any fixture resolves a token).
setAuthProvider(keycloakAuthProvider);

// auth-session ships fixture factories (not a ready test object) — build one from base.
const authFixture = base.extend(createAuthFixtures());

const utilsTest = mergeTests(
  apiRequestFixture,
  authFixture,
  recurseFixture,
  logFixture,
  interceptFixture,
  networkErrorMonitorFixture,
);

export const test = utilsTest.extend<ProjectFixtures>({
  tenantContext: async ({}, use) => {
    await use(createTenantContext());
  },

  seededProject: async ({ apiRequest, authToken, recurse, tenantContext }, use) => {
    const { project, cleanup } = await seedActiveProject({ apiRequest, authToken, recurse, tenantContext });
    await use(project);
    await cleanup();
  },
});

export { expect };
