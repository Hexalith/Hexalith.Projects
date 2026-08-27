import { request } from '@playwright/test';

import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { createLiveFixtureIdentities } from '../support/factories/live-fixture-identities.js';
import {
  createLiveFixtureGraph,
  deleteLiveFixtureGraph,
  type LiveFixtureGraph,
} from '../support/helpers/live-fixtures-api-client.js';

test.describe('live fixture lifecycle', () => {
  test.describe.configure({ mode: 'parallel' });

  liveAppHostTest('exposes the exact run-scoped graph seeded for this worker', async ({ liveFixtureGraph }) => {
    const control = await request.newContext({
      baseURL: requireLiveEnv('FIXTURE_API_URL'),
      ignoreHTTPSErrors: true,
    });
    try {
      const response = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(liveFixtureGraph.graphId)}`);
      expect(response.status()).toBe(200);
      expect((await response.json()) as LiveFixtureGraph).toMatchObject({
        graphId: liveFixtureGraph.graphId,
        runId: liveFixtureGraph.runId,
        tenantId: liveFixtureGraph.tenantId,
        projectId: liveFixtureGraph.projectId,
      });
    } finally {
      await control.dispose();
    }
  });

  liveAppHostTest('manual graph cleanup is observable and isolated', async ({ liveFixtureGraph }, testInfo) => {
    const identities = createLiveFixtureIdentities({
      runId: liveFixtureGraph.runId,
      workerIndex: testInfo.workerIndex,
      retry: testInfo.retry,
      repeatEachIndex: testInfo.repeatEachIndex,
      scenario: `${testInfo.file}:${testInfo.title}:manual-cleanup`,
    });
    const graph: LiveFixtureGraph = {
      ...identities,
      runId: identities.runId.slice(0, 128),
      scenario: identities.scenario.slice(0, 128),
      tenantId: liveFixtureGraph.tenantId,
      principalId: liveFixtureGraph.principalId,
      filePath: liveFixtureGraph.filePath,
    };
    const control = await request.newContext({
      baseURL: requireLiveEnv('FIXTURE_API_URL'),
      ignoreHTTPSErrors: true,
    });
    let created = false;
    let primaryFailure: unknown;
    try {
      await createLiveFixtureGraph(control, graph);
      created = true;
      const cleanup = await deleteLiveFixtureGraph(control, graph.graphId);
      expect(cleanup.succeeded).toBe(true);
      expect(cleanup.attempts.map((attempt) => attempt.role)).toEqual(['memories', 'folders', 'conversations']);
      created = false;
      const after = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(graph.graphId)}`);
      expect(after.status()).toBe(404);
    } catch (error) {
      primaryFailure = error;
      throw error;
    } finally {
      if (created) {
        try {
          await deleteLiveFixtureGraph(control, graph.graphId);
        } catch (cleanupError) {
          await testInfo.attach('manual-live-fixture-cleanup-error', {
            body: JSON.stringify({ cleanup: 'unavailable' }),
            contentType: 'application/json',
          });
          if (primaryFailure === undefined) throw cleanupError;
        }
      }
      await control.dispose();
    }
  });
});

function requireLiveEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[live-fixtures-lifecycle] ${name} is required.`);
  return value;
}
