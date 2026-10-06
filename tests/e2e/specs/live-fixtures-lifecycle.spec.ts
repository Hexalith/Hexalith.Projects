import { request, type TestInfo } from '@playwright/test';

import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { createLiveFixtureIdentities } from '../support/factories/live-fixture-identities.js';
import { CleanupLedger, reportCleanup, setupWithReverseCleanup } from '../support/fixtures/cleanup-evidence.js';
import {
  createLiveFixtureGraph,
  deleteLiveFixtureGraph,
  LIVE_FIXTURE_GRAPH_FIELDS,
  toLiveFixtureGraph,
  type LiveFixtureGraph,
} from '../support/helpers/live-fixtures-api-client.js';

test.describe('live fixture lifecycle', () => {
  test.describe.configure({ mode: 'parallel' });

  liveAppHostTest('exposes the exact run-scoped graph seeded for this worker', async ({ liveFixtureGraph }, testInfo) => {
    const control = await fixtureControl();
    try {
      const response = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(liveFixtureGraph.graphId)}`);
      expect(response.status()).toBe(200);
      const observed = (await response.json()) as LiveFixtureGraph;
      // The DTO is symmetric: the response carries exactly the members the runner posted.
      expect(Object.keys(observed).sort()).toEqual([...LIVE_FIXTURE_GRAPH_FIELDS].sort());
      expect(observed).toEqual(liveFixtureGraph);
      expect(observed).toMatchObject({
        workerIndex: testInfo.workerIndex,
        retry: testInfo.retry,
        repeatEachIndex: testInfo.repeatEachIndex,
        tenantId: requireLiveEnv('TEST_TENANT_ID'),
        principalId: requireLiveEnv('TEST_PRINCIPAL_ID'),
      });
    } finally {
      await control.dispose();
    }
  });

  liveAppHostTest('manual graph cleanup is reverse-ordered, typed, and isolated from the worker graph', async ({ liveFixtureGraph }, testInfo) => {
    const graph = toLiveFixtureGraph(
      createLiveFixtureIdentities({
        runId: liveFixtureGraph.runId,
        workerIndex: testInfo.workerIndex,
        retry: testInfo.retry,
        repeatEachIndex: testInfo.repeatEachIndex,
        scenario: `${testInfo.file}:${testInfo.title}:manual-cleanup`,
      }),
      liveFixtureGraph,
    );
    expect(graph.graphId).not.toBe(liveFixtureGraph.graphId);
    expect(graph.projectId).not.toBe(liveFixtureGraph.projectId);

    const control = await fixtureControl();
    const ledger = new CleanupLedger();
    try {
      await setupWithReverseCleanup(testInfo, 'manual-live-fixture-cleanup', ledger, async () => {
        await createLiveFixtureGraph(control, graph);
        ledger.register({
          role: 'live-fixtures:manual',
          run: async () => (await deleteLiveFixtureGraph(control, graph.graphId)).attempts,
        });
      });

      const cleanup = await ledger.runReverse();
      expect(cleanup.succeeded).toBe(true);
      expect(cleanup.attempts.map((attempt) => attempt.role)).toEqual(['memories', 'folders', 'conversations']);
      expect(cleanup.attempts.every((attempt) => attempt.statusCode === 204)).toBe(true);
      expect(Object.keys(cleanup.attempts[0]).sort()).toEqual(['role', 'statusCode', 'succeeded']);

      const removed = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(graph.graphId)}`);
      expect(removed.status()).toBe(404);
      const isolated = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(liveFixtureGraph.graphId)}`);
      expect(isolated.status()).toBe(200);

      // Removal is idempotent: a second delete reports every role as an already-clean 404.
      const repeated = await deleteLiveFixtureGraph(control, graph.graphId);
      expect(repeated.succeeded).toBe(true);
      expect(repeated.attempts.map((attempt) => attempt.statusCode)).toEqual([404, 404, 404]);
    } finally {
      await reportCleanup(testInfo, 'manual-live-fixture-cleanup', await ledger.runReverse());
      await control.dispose();
    }
  });

  liveAppHostTest('rejects unbounded or path-escaping graph metadata without echoing it', async ({ liveFixtureGraph }, testInfo) => {
    const identities = createLiveFixtureIdentities({
      runId: liveFixtureGraph.runId,
      workerIndex: testInfo.workerIndex,
      retry: testInfo.retry,
      repeatEachIndex: testInfo.repeatEachIndex,
      scenario: `${testInfo.file}:${testInfo.title}:invalid`,
    });
    const control = await fixtureControl();
    try {
      for (const graph of [
        toLiveFixtureGraph(identities, liveFixtureGraph, '../outside-workspace.md'),
        { ...toLiveFixtureGraph(identities, liveFixtureGraph), folderId: 'f'.repeat(129) },
        { ...toLiveFixtureGraph(identities, liveFixtureGraph), retry: -1 },
      ]) {
        const response = await control.post('/api/v1/live-fixtures/graphs', { data: graph, failOnStatusCode: false });
        expect(response.status()).toBe(400);
        expect(await response.text()).toBe('');
      }

      const absent = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(identities.graphId)}`);
      expect(absent.status()).toBe(404);
    } finally {
      await control.dispose();
    }
  });
});

test.describe('live cleanup evidence contract (no app required)', () => {
  test('runs steps newest first and records thrown steps as role-only failures', async () => {
    const ledger = new CleanupLedger();
    const order: string[] = [];
    ledger.register({ role: 'live-fixtures', run: async () => (order.push('graph'), [{ role: 'memories', statusCode: 204, succeeded: true }]) });
    ledger.register({ role: 'projects:primary', run: async () => (order.push('primary'), [{ role: 'projects:primary', statusCode: 202, succeeded: true }]) });
    ledger.register({
      role: 'projects:secondary',
      run: async () => {
        order.push('secondary');
        throw new Error('https://projects.invalid/api/v1/projects/secret-payload');
      },
    });

    const result = await ledger.runReverse();

    expect(order).toEqual(['secondary', 'primary', 'graph']);
    expect(result.succeeded).toBe(false);
    expect(result.attempts).toEqual([
      { role: 'projects:secondary', statusCode: null, succeeded: false },
      { role: 'projects:primary', statusCode: 202, succeeded: true },
      { role: 'memories', statusCode: 204, succeeded: true },
    ]);
    expect(JSON.stringify(result)).not.toContain('secret-payload');
    expect((await ledger.runReverse()).attempts).toEqual([]);
  });

  test('preserves the primary failure and fails a passing test on incomplete cleanup', async () => {
    const failed = { role: 'projects:seeded', statusCode: 409, succeeded: false };
    const failingTest = fakeTestInfo('failed');
    await expect(reportCleanup(failingTest.info, 'seeded-project-cleanup', { attempts: [failed], succeeded: false })).resolves.toBeUndefined();
    expect(failingTest.attachments).toEqual([
      { name: 'seeded-project-cleanup.json', body: JSON.stringify({ attempts: [failed], succeeded: false }, null, 2) },
    ]);

    const passingTest = fakeTestInfo('passed');
    await expect(reportCleanup(passingTest.info, 'seeded-project-cleanup', { attempts: [failed], succeeded: false }))
      .rejects.toThrow('[live-cleanup] seeded-project-cleanup did not complete: projects:seeded:409.');

    const cleanTest = fakeTestInfo('passed');
    await reportCleanup(cleanTest.info, 'seeded-project-cleanup', { attempts: [{ ...failed, succeeded: true }], succeeded: true });
    expect(cleanTest.attachments).toEqual([]);
  });

  test('rethrows the original setup failure after reverse compensation', async () => {
    const ledger = new CleanupLedger();
    const setup = fakeTestInfo('passed');
    const primary = new Error('primary setup failure');
    ledger.register({ role: 'projects:primary', run: async () => [{ role: 'projects:primary', statusCode: null, succeeded: false }] });

    await expect(setupWithReverseCleanup(setup.info, 'resolution-projects-cleanup', ledger, async () => {
      throw primary;
    })).rejects.toBe(primary);
    expect(setup.attachments.map((attachment) => attachment.name)).toEqual(['resolution-projects-cleanup-setup.json']);
  });
});

function fakeTestInfo(status: TestInfo['status']): { info: TestInfo; attachments: Array<{ name: string; body: string }> } {
  const attachments: Array<{ name: string; body: string }> = [];
  const info = {
    status,
    expectedStatus: 'passed',
    attach: async (name: string, options: { body: string }) => {
      attachments.push({ name, body: options.body });
    },
  } as unknown as TestInfo;
  return { info, attachments };
}

async function fixtureControl() {
  return request.newContext({ baseURL: requireLiveEnv('FIXTURE_API_URL'), ignoreHTTPSErrors: true });
}

function requireLiveEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[live-fixtures-lifecycle] ${name} is required.`);
  return value;
}
