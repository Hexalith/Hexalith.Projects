import type { APIRequestContext } from '@playwright/test';

import type { LiveFixtureIdentities } from '../factories/live-fixture-identities.js';

/**
 * Metadata-only sibling graph. Mirrors `Hexalith.Projects.E2E.Fixtures.LiveFixtureGraph` member for
 * member, so the posted request and every fixture response share one symmetric wire shape.
 */
export interface LiveFixtureGraph extends LiveFixtureIdentities {
  tenantId: string;
  principalId: string;
  filePath: string;
}

/** Exact wire members of the graph DTO; the C# contract test pins the same list. */
export const LIVE_FIXTURE_GRAPH_FIELDS = [
  'graphId',
  'runId',
  'workerIndex',
  'retry',
  'repeatEachIndex',
  'scenario',
  'tenantId',
  'principalId',
  'projectId',
  'secondaryProjectId',
  'proposalProjectId',
  'proposalRetryProjectId',
  'degradedProjectId',
  'conversationId',
  'ambiguousConversationId',
  'existingConversationId',
  'staleConversationId',
  'forbiddenConversationId',
  'unavailableConversationId',
  'folderId',
  'secondaryFolderId',
  'proposalFolderId',
  'workspaceId',
  'fileReferenceId',
  'secondaryFileReferenceId',
  'proposalFileReferenceId',
  'deniedFileReferenceId',
  'filePath',
  'memoryReferenceId',
  'correlationId',
  'taskId',
  'idempotencyKey',
] as const satisfies readonly (keyof LiveFixtureGraph)[];

// Compile-time proof that the field list is exhaustive for the TypeScript graph shape.
const exhaustiveGraphFields: [Exclude<keyof LiveFixtureGraph, (typeof LIVE_FIXTURE_GRAPH_FIELDS)[number]>] extends [never]
  ? true
  : false = true;
void exhaustiveGraphFields;

/** One metadata-only cleanup attempt: the attempted role and the observed HTTP status, never a body. */
export interface FixtureCleanupAttempt {
  role: string;
  statusCode: number | null;
  succeeded: boolean;
}

/** Ordered cleanup attempts for one graph or one fixture teardown. */
export interface FixtureCleanupResult {
  attempts: FixtureCleanupAttempt[];
  succeeded: boolean;
}

/** Metadata-only seed failure returned by the control resource with its reverse-order compensation. */
export interface FixtureSeedFailure {
  failedRole: string;
  statusCode: number | null;
  compensation: FixtureCleanupResult;
}

/** Builds the exact symmetric wire graph from one attempt's identities and token authority. */
export function toLiveFixtureGraph(
  identities: LiveFixtureIdentities,
  authority: { tenantId: string; principalId: string },
  filePath = 'docs/contract.pdf',
): LiveFixtureGraph {
  const graph: LiveFixtureGraph = {
    ...identities,
    runId: identities.runId.slice(0, 128),
    scenario: identities.scenario.slice(0, 128),
    tenantId: authority.tenantId,
    principalId: authority.principalId,
    filePath,
  };
  return Object.fromEntries(LIVE_FIXTURE_GRAPH_FIELDS.map((field) => [field, graph[field]])) as unknown as LiveFixtureGraph;
}

/** Provisions a metadata-only sibling graph through the profile-scoped control resource. */
export async function createLiveFixtureGraph(
  request: APIRequestContext,
  graph: LiveFixtureGraph,
): Promise<LiveFixtureGraph> {
  const response = await request.post('/api/v1/live-fixtures/graphs', { data: graph, failOnStatusCode: false });
  if (response.status() === 201) {
    return (await response.json()) as LiveFixtureGraph;
  }

  let failure = '';
  if (response.status() === 502) {
    const body = (await response.json()) as FixtureSeedFailure;
    failure = `; failedRole=${body.failedRole}; roleStatus=${body.statusCode ?? 'none'}; compensation=${summarizeCleanup(body.compensation)}`;
  }
  throw new Error(`[live-fixtures] graph seed failed (${response.status()})${failure}.`);
}

/** Deletes one run-scoped graph; the control host performs role cleanup in reverse order. */
export async function deleteLiveFixtureGraph(
  request: APIRequestContext,
  graphId: string,
): Promise<FixtureCleanupResult> {
  let response;
  try {
    response = await request.delete(`/api/v1/live-fixtures/graphs/${encodeURIComponent(graphId)}`, {
      failOnStatusCode: false,
    });
  } catch {
    // Transport diagnostics can contain endpoints; report only the missing status.
    return unavailableCleanup('live-fixtures', null);
  }

  if (response.status() !== 200 && response.status() !== 502) {
    return unavailableCleanup('live-fixtures', response.status());
  }
  return (await response.json()) as FixtureCleanupResult;
}

/** Formats cleanup evidence as role/status pairs only. */
export function summarizeCleanup(result: FixtureCleanupResult): string {
  return result.attempts.map((attempt) => `${attempt.role}:${attempt.statusCode ?? 'none'}`).join(',') || 'none';
}

function unavailableCleanup(role: string, statusCode: number | null): FixtureCleanupResult {
  return { attempts: [{ role, statusCode, succeeded: false }], succeeded: false };
}
