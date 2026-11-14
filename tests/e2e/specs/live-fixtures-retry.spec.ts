import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

import { request } from '@playwright/test';

import { expect, liveAppHostTest } from '../support/merged-fixtures.js';
import { getProject } from '../support/helpers/projects-api-client.js';
import type { LiveFixtureGraph } from '../support/helpers/live-fixtures-api-client.js';

// The managed retry phase deliberately fails each first attempt after successful setup. Its real
// Playwright retry must observe compensation and new worker/retry identities. Other phases collect
// their ordinary matrix without a synthetic failure or a skipped retry probe.
if (process.env.E2E_FIXTURE_RETRY_PROBE === '1') {
  liveAppHostTest('archives the previous attempt and isolates the actual retry', async ({
    liveFixtureGraph, seededProject, apiRequest, authToken, tenantContext, requestIdentity,
  }, testInfo) => {
    const directory = process.env.E2E_FIXTURE_RETRY_DIRECTORY;
    if (!directory) throw new Error('[live-retry] the private evidence directory is required.');
    await mkdir(directory, { recursive: true, mode: 0o700 });
    const key = createHash('sha256').update(JSON.stringify([
      liveFixtureGraph.runId, testInfo.titlePath, testInfo.repeatEachIndex,
    ])).digest('hex');
    const receipt = join(directory, `${key}.json`);
    if (testInfo.retry === 0) {
      await writeFile(receipt, JSON.stringify(liveFixtureGraph), { mode: 0o600, flag: 'wx' });
      throw new Error('[live-retry] deliberate first-attempt failure exercises real compensation and Playwright retry.');
    }
    expect(testInfo.retry).toBe(1);
    const previous = JSON.parse(await readFile(receipt, 'utf8')) as LiveFixtureGraph;
    expect(previous.runId).toBe(liveFixtureGraph.runId);
    expect(previous.repeatEachIndex).toBe(testInfo.repeatEachIndex);
    expect(previous.workerIndex).not.toBe(testInfo.workerIndex);
    expect(previous.retry).toBe(0);
    const identities = Object.entries(liveFixtureGraph).filter(([name]) => name.endsWith('Id') || name === 'idempotencyKey');
    for (const [name, value] of identities) {
      if (name === 'runId' || name === 'tenantId' || name === 'principalId') continue;
      expect(value).not.toBe(previous[name as keyof LiveFixtureGraph]);
    }
    const archived = await getProject(apiRequest, tenantContext.tenantId, previous.projectId, {
      authToken, ...requestIdentity('retry:previous-project'),
    });
    expect(archived.status).toBe(200);
    expect(archived.body.lifecycleState).toBe('archived');
    expect(seededProject.projectId).toBe(liveFixtureGraph.projectId);
    const control = await request.newContext({ baseURL: process.env.FIXTURE_API_URL });
    try {
      const absent = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(previous.graphId)}`);
      expect(absent.status()).toBe(404);
      const current = await control.get(`/api/v1/live-fixtures/graphs/${encodeURIComponent(liveFixtureGraph.graphId)}`);
      expect(current.status()).toBe(200);
    } finally {
      await control.dispose();
    }
    await writeFile(join(directory, `${key}.verified.json`), JSON.stringify({
      retry: testInfo.retry, repeatEachIndex: testInfo.repeatEachIndex, workerChanged: true,
      identifiersDisjoint: true, previousProjectArchived: true, previousSiblingGraphRemoved: true,
    }), { mode: 0o600, flag: 'wx' });
  });
}
