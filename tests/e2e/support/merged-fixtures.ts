import { mergeTests, expect } from '@playwright/test';
import { test as apiRequestFixture } from '@seontechnologies/playwright-utils/api-request/fixtures';
import { test as recurseFixture } from '@seontechnologies/playwright-utils/recurse/fixtures';
import { test as logFixture } from '@seontechnologies/playwright-utils/log/fixtures';
import { test as interceptFixture } from '@seontechnologies/playwright-utils/intercept-network-call/fixtures';
import { test as networkErrorMonitorFixture } from '@seontechnologies/playwright-utils/network-error-monitor/fixtures';

import { requestKeycloakAccessToken } from './auth/keycloak-auth-provider.js';
import { createTenantContext } from './factories/tenant-factory.js';
import { createProjectInput } from './factories/project-factory.js';
import {
  identitiesForTest,
  provisionLiveFixtureGraph,
  type LiveFixtureFixtures,
} from './fixtures/live-fixtures.js';
import {
  type ProjectFixtures,
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
 *  - tenantContext       configured projected tenant with per-test metadata (custom)
 *  - seededProject       active project converged in the read model (custom)
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

  liveFixtureGraph: async ({ liveFixtureIdentities }, use, testInfo) => {
    const { graph, cleanup } = await provisionLiveFixtureGraph(liveFixtureIdentities);
    await use(graph);
    const result = await cleanup();
    if (!result.succeeded) {
      await testInfo.attach('live-fixture-cleanup.json', {
        body: JSON.stringify(result),
        contentType: 'application/json',
      });
      if (testInfo.status === testInfo.expectedStatus) {
        throw new Error('[live-fixtures] one or more sibling cleanup roles did not succeed.');
      }
    }
  },

  seededProject: async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }, use) => {
    const requestIdentity = fixtureRequestIdentity(liveFixtureGraph);
    const { project, cleanup } = await seedActiveProject(
      { apiRequest, authToken, recurse, tenantContext, requestIdentity },
      createProjectInput({ projectId: liveFixtureGraph.projectId }),
    );
    await use(project);
    await cleanup();
  },

  referencedProject: async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }, use) => {
    const { project, cleanup } = await seedReferencedProject(
      {
        apiRequest,
        authToken,
        recurse,
        tenantContext,
        requestIdentity: fixtureRequestIdentity(liveFixtureGraph),
      },
      liveFixtureGraph,
    );
    await use(project);
    await cleanup();
  },

  resolutionProjects: async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }, use) => {
    const { projects, cleanup } = await seedResolutionProjects(
      {
        apiRequest,
        authToken,
        recurse,
        tenantContext,
        requestIdentity: fixtureRequestIdentity(liveFixtureGraph),
      },
      liveFixtureGraph,
    );
    await use(projects);
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

function fixtureRequestIdentity(graph: LiveFixtureFixtures['liveFixtureGraph']) {
  return {
    correlationId: graph.correlationId,
    taskId: graph.taskId,
    idempotencyKey: graph.idempotencyKey,
  };
}

export { expect };
