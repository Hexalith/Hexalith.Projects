import { getProject, type ApiRequest, type ProjectDetail, type ProjectLifecycle } from './projects-api-client.js';
import type { AuthHeaderOptions } from './correlation.js';

/**
 * Deterministic projection-readiness probes (TC-3 / TC-10).
 *
 * The Projects pipeline is command-async with NO read-after-write: a mutation returns
 * `202 AcceptedCommand`, then projections converge asynchronously (Dapr pub/sub +
 * SignalR nudge → re-query). NEVER `waitForTimeout`/sleep — poll the real read model
 * until it converges, using playwright-utils `recurse` for categorized timeout errors.
 */

/** The playwright-utils `recurse` *fixture* signature. */
export type Recurse = <T>(command: () => Promise<T>, predicate: (value: T) => boolean, options?: Record<string, unknown>) => Promise<T>;

const DEFAULT_TIMEOUT = 30_000;
const DEFAULT_INTERVAL = 1_000;

/** Wait until GetProject returns 200 and (optionally) the expected lifecycle. */
export async function waitForProject(
  recurse: Recurse,
  apiRequest: ApiRequest,
  tenantId: string,
  projectId: string,
  headerOptions: AuthHeaderOptions,
  expected?: { lifecycle?: ProjectLifecycle },
): Promise<ProjectDetail> {
  const result = await recurse(
    () => getProject(apiRequest, tenantId, projectId, headerOptions),
    (response) => {
      // Assertions inside the predicate: recurse stops when they pass (no `return true` needed).
      if (response.status !== 200) return false;
      if (expected?.lifecycle && response.body.lifecycleState !== expected.lifecycle) return false;
      return true;
    },
    {
      timeout: DEFAULT_TIMEOUT,
      interval: DEFAULT_INTERVAL,
      log: `Waiting for project ${projectId} to converge in the read model`,
      error: `Project ${projectId} did not appear in the read model within ${DEFAULT_TIMEOUT}ms — check Workers projection processing.`,
    },
  );
  return result.body;
}

/**
 * Wait until a predicate over the project detail holds (e.g. a referenced resource's
 * health flips to `stale`/`unavailable`, or freshness reaches a watermark).
 */
export async function waitForProjectState(
  recurse: Recurse,
  apiRequest: ApiRequest,
  tenantId: string,
  projectId: string,
  headerOptions: AuthHeaderOptions,
  predicate: (detail: ProjectDetail) => boolean,
  options?: { timeout?: number; interval?: number; description?: string },
): Promise<ProjectDetail> {
  const result = await recurse(
    () => getProject(apiRequest, tenantId, projectId, headerOptions),
    (response) => response.status === 200 && predicate(response.body),
    {
      timeout: options?.timeout ?? DEFAULT_TIMEOUT,
      interval: options?.interval ?? DEFAULT_INTERVAL,
      log: options?.description ?? `Waiting for project ${projectId} to reach expected state`,
    },
  );
  return result.body;
}
