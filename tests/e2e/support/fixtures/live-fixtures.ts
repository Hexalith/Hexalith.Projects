import { request, type TestInfo } from '@playwright/test';

import {
  createLiveFixtureIdentities,
  type LiveFixtureIdentities,
} from '../factories/live-fixture-identities.js';
import {
  createLiveFixtureGraph,
  deleteLiveFixtureGraph,
  type LiveFixtureGraph,
} from '../helpers/live-fixtures-api-client.js';

export interface LiveFixtureFixtures {
  liveFixtureIdentities: LiveFixtureIdentities;
  liveFixtureGraph: LiveFixtureGraph;
}

/** Creates all deterministic IDs for one test attempt from Playwright's isolation dimensions. */
export function identitiesForTest(testInfo: TestInfo): LiveFixtureIdentities {
  return createLiveFixtureIdentities({
    runId: requireLiveEnv('E2E_RUN_ID'),
    workerIndex: testInfo.workerIndex,
    retry: testInfo.retry,
    repeatEachIndex: testInfo.repeatEachIndex,
    scenario: `${testInfo.file}:${testInfo.title}`,
  });
}

/** Seeds the sibling compatibility host using metadata only. */
export async function provisionLiveFixtureGraph(
  identities: LiveFixtureIdentities,
): Promise<{ graph: LiveFixtureGraph; cleanup: () => Promise<void> }> {
  const fixtureRequest = await request.newContext({
    baseURL: requireLiveEnv('FIXTURE_API_URL'),
    ignoreHTTPSErrors: true,
  });
  const graph: LiveFixtureGraph = {
    ...identities,
    runId: identities.runId.slice(0, 128),
    scenario: identities.scenario.slice(0, 128),
    tenantId: requireLiveEnv('TEST_TENANT_ID'),
    principalId: requireLiveEnv('TEST_PRINCIPAL_ID'),
    filePath: 'docs/contract.pdf',
  };

  try {
    const provisioned = await createLiveFixtureGraph(fixtureRequest, graph);
    return {
      graph: provisioned,
      cleanup: async () => {
        try {
          await deleteLiveFixtureGraph(fixtureRequest, provisioned.graphId);
        } finally {
          await fixtureRequest.dispose();
        }
      },
    };
  } catch (error) {
    await fixtureRequest.dispose();
    throw error;
  }
}

function requireLiveEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[live-fixtures] ${name} is required for the live AppHost lane.`);
  return value;
}
