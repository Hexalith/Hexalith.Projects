import type { APIRequestContext } from '@playwright/test';

import type { LiveFixtureIdentities } from '../factories/live-fixture-identities.js';

export interface LiveFixtureGraph extends LiveFixtureIdentities {
  tenantId: string;
  principalId: string;
  filePath: string;
}

export interface FixtureCleanupAttempt {
  role: string;
  statusCode: number | null;
  succeeded: boolean;
}

export interface FixtureCleanupResult {
  attempts: FixtureCleanupAttempt[];
  succeeded: boolean;
}

/** Provisions a metadata-only sibling graph through the profile-scoped control resource. */
export async function createLiveFixtureGraph(
  request: APIRequestContext,
  graph: LiveFixtureGraph,
): Promise<LiveFixtureGraph> {
  const response = await request.post('/api/v1/live-fixtures/graphs', { data: graph });
  if (response.status() !== 201) {
    throw new Error(`[live-fixtures] graph seed failed (${response.status()}).`);
  }
  return (await response.json()) as LiveFixtureGraph;
}

/** Deletes one run-scoped graph; the control host performs role cleanup in reverse order. */
export async function deleteLiveFixtureGraph(
  request: APIRequestContext,
  graphId: string,
): Promise<FixtureCleanupResult> {
  const response = await request.delete(`/api/v1/live-fixtures/graphs/${encodeURIComponent(graphId)}`);
  if (response.status() !== 200 && response.status() !== 502) {
    throw new Error(`[live-fixtures] graph cleanup failed (${response.status()}).`);
  }
  return (await response.json()) as FixtureCleanupResult;
}
