import { mergeTests, expect } from '@playwright/test';
import { test as apiRequestFixture } from '@seontechnologies/playwright-utils/api-request/fixtures';
import { test as recurseFixture } from '@seontechnologies/playwright-utils/recurse/fixtures';
import { test as logFixture } from '@seontechnologies/playwright-utils/log/fixtures';
import { test as interceptFixture } from '@seontechnologies/playwright-utils/intercept-network-call/fixtures';
import { test as networkErrorMonitorFixture } from '@seontechnologies/playwright-utils/network-error-monitor/fixtures';

import { requestKeycloakAccessToken } from './auth/keycloak-auth-provider.js';
import { createTenantContext } from './factories/tenant-factory.js';
import { createProjectInput } from './factories/project-factory.js';
import { CleanupLedger, reportCleanup, setupWithReverseCleanup } from './fixtures/cleanup-evidence.js';
import {
  identitiesForTest,
  provisionLiveFixtureGraph,
  requestIdentityFactory,
  type LiveFixtureFixtures,
} from './fixtures/live-fixtures.js';
import {
  type ProjectFixtures,
  projectArchiveStep,
  seedActiveProject,
  seedReferencedProject,
  seedResolutionProjects,
} from './fixtures/projects-fixtures.js';
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
 *  - tenantContext       token-derived projected tenant with per-test metadata (custom)
 *  - liveFixtureGraph    run/worker/retry/repeat/scenario-scoped sibling graph (custom)
 *  - requestIdentity     operation-scoped correlation/task/idempotency identities (custom)
 *  - liveCleanup         reverse archive-to-convergence cleanup for directly created Projects (custom)
 *  - seededProject       active project converged in the read model (custom)
 *
 * Cleanup runs in reverse dependency order, reports attempted role/status only, and fails a test
 * only when the test itself passed, so the primary failure is always preserved.
 */

const utilsTest = mergeTests(
  apiRequestFixture,
  recurseFixture,
  logFixture,
  interceptFixture,
  networkErrorMonitorFixture,
);

interface DirectAuthFixtures {
  authToken: string;
}

export const test = utilsTest.extend<ProjectFixtures & LiveFixtureFixtures & DirectAuthFixtures>({
  authToken: async ({ request }, use) => {
    await use(process.env.E2E_LIVE_APPHOST === '1' ? await requestKeycloakAccessToken(request) : 'offline-token');
  },

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

  liveFixtureIdentities: async ({}, use, testInfo) => {
    await use(identitiesForTest(testInfo));
  },

  requestIdentity: async ({ liveFixtureIdentities }, use) => {
    await use(requestIdentityFactory(liveFixtureIdentities));
  },

  liveFixtureGraph: async ({ liveFixtureIdentities }, use, testInfo) => {
    const { graph, cleanup } = await provisionLiveFixtureGraph(liveFixtureIdentities);
    await use(graph);
    // Sibling roles are removed after every dependent Project fixture has been archived.
    await reportCleanup(testInfo, 'live-fixture-cleanup', await cleanup());
  },

  liveCleanup: async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }, use, testInfo) => {
    const ledger = new CleanupLedger();
    const deps = { apiRequest, authToken, recurse, tenantContext, graph: liveFixtureGraph };
    await use({
      trackProject: (projectId, label) => ledger.register(projectArchiveStep(deps, projectId, `projects:${label}`)),
    });
    await reportCleanup(testInfo, 'live-project-cleanup', await ledger.runReverse());
  },

  seededProject: async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }, use, testInfo) => {
    const ledger = new CleanupLedger();
    const deps = { apiRequest, authToken, recurse, tenantContext, graph: liveFixtureGraph };
    const project = await setupWithReverseCleanup(testInfo, 'seeded-project-cleanup', ledger, () =>
      seedActiveProject(deps, ledger, 'projects:seeded', createProjectInput({ projectId: liveFixtureGraph.projectId })));
    await use(project);
    await reportCleanup(testInfo, 'seeded-project-cleanup', await ledger.runReverse());
  },

  referencedProject: async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }, use, testInfo) => {
    const ledger = new CleanupLedger();
    const deps = { apiRequest, authToken, recurse, tenantContext, graph: liveFixtureGraph };
    const project = await setupWithReverseCleanup(testInfo, 'referenced-project-cleanup', ledger, () =>
      seedReferencedProject(deps, ledger, liveFixtureGraph));
    await use(project);
    await reportCleanup(testInfo, 'referenced-project-cleanup', await ledger.runReverse());
  },

  resolutionProjects: async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }, use, testInfo) => {
    const ledger = new CleanupLedger();
    const deps = { apiRequest, authToken, recurse, tenantContext, graph: liveFixtureGraph };
    const projects = await setupWithReverseCleanup(testInfo, 'resolution-projects-cleanup', ledger, () =>
      seedResolutionProjects(deps, ledger, liveFixtureGraph));
    await use(projects);
    await reportCleanup(testInfo, 'resolution-projects-cleanup', await ledger.runReverse());
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
