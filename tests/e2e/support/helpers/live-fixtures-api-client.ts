import type { APIRequestContext } from '@playwright/test';

import type { LiveFixtureIdentities } from '../factories/live-fixture-identities.js';

export interface LiveFixtureGraph extends LiveFixtureIdentities {
  tenantId: string;
  principalId: string;
  filePath: string;
}

/** Provisions a metadata-only sibling graph through the profile-scoped control resource. */
export async function createLiveFixtureGraph(
  request: APIRequestContext,
  graph: LiveFixtureGraph,
): Promise<LiveFixtureGraph> {
  const response = await request.post('/api/v1/live-fixtures/graphs', { data: graph });
  if (response.status() !== 201) {
    throw new Error(`[live-fixtures] graph ${graph.graphId} seed failed (${response.status()}): ${await safeBody(response)}`);
  }
  return (await response.json()) as LiveFixtureGraph;
}

/** Deletes one run-scoped graph; the control host performs role cleanup in reverse order. */
export async function deleteLiveFixtureGraph(request: APIRequestContext, graphId: string): Promise<void> {
  const response = await request.delete(`/api/v1/live-fixtures/graphs/${encodeURIComponent(graphId)}`);
  if (response.status() !== 204 && response.status() !== 404) {
    throw new Error(`[live-fixtures] graph ${graphId} cleanup failed (${response.status()}): ${await safeBody(response)}`);
  }
}

async function safeBody(response: { text(): Promise<string> }): Promise<string> {
  return (await response.text()).replace(/\s+/g, ' ').slice(0, 800);
}
