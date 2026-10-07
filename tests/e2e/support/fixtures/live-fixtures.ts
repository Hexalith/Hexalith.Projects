import { createHash } from 'node:crypto';
import { basename, relative } from 'node:path';

import { request, type TestInfo } from '@playwright/test';

import {
  createLiveFixtureIdentities,
  createLiveRequestIdentity,
  type LiveFixtureIdentities,
  type LiveRequestIdentity,
} from '../factories/live-fixture-identities.js';
import {
  createLiveFixtureGraph,
  deleteLiveFixtureGraph,
  toLiveFixtureGraph,
  type FixtureCleanupResult,
  type LiveFixtureGraph,
} from '../helpers/live-fixtures-api-client.js';

export interface LiveFixtureFixtures {
  liveFixtureIdentities: LiveFixtureIdentities;
  liveFixtureGraph: LiveFixtureGraph;
  /** Operation-scoped request identities derived from this attempt's isolation dimensions. */
  requestIdentity: (operation: string) => LiveRequestIdentity;
  /** Registers Projects created directly by a test for reverse archive-to-convergence cleanup. */
  liveCleanup: LiveCleanup;
}

export interface LiveCleanup {
  /** Archives `projectId` to convergence on teardown; `label` becomes the metadata-only role. */
  trackProject(projectId: string, label: string): void;
}

/** Creates all deterministic IDs for one test attempt from Playwright's isolation dimensions. */
export function identitiesForTest(testInfo: TestInfo): LiveFixtureIdentities {
  return createLiveFixtureIdentities({
    runId: requireLiveEnv('E2E_RUN_ID'),
    workerIndex: testInfo.workerIndex,
    retry: testInfo.retry,
    repeatEachIndex: testInfo.repeatEachIndex,
    scenario: scenarioForTest(testInfo),
  });
}

/**
 * Builds the metadata-only scenario label for one test. The label becomes a fixture conversation label
 * and a Project name, so it is a slug of the spec name plus a digest of the relative spec path, full
 * title path (describe blocks included, so equal titles in different groups never collide), and
 * optional suffix: it never carries a local path, path separators, or free-text title words that
 * Project metadata validation rejects, and it stays unique per test.
 */
export function scenarioForTest(testInfo: TestInfo, suffix?: string): string {
  const spec = relative(testInfo.project.testDir, testInfo.file).replaceAll('\\', '/');
  const digest = createHash('sha256')
    .update(JSON.stringify([spec, testInfo.titlePath, suffix ?? '']), 'utf8')
    .digest('hex')
    .slice(0, 16);
  const slug = (value: string) => value.replace(/[^A-Za-z0-9-]+/g, '-').replace(/^-+|-+$/g, '');
  return [slug(basename(spec).replace(/\.spec\.ts$/, '')), digest, suffix ? slug(suffix) : undefined]
    .filter(Boolean)
    .join('-');
}

/** Binds the request-identity factory to one attempt's graph identity. */
export function requestIdentityFactory(identities: LiveFixtureIdentities): (operation: string) => LiveRequestIdentity {
  return (operation) => createLiveRequestIdentity(identities, operation);
}

/** Seeds the sibling compatibility host using metadata only. */
export async function provisionLiveFixtureGraph(
  identities: LiveFixtureIdentities,
): Promise<{ graph: LiveFixtureGraph; cleanup: () => Promise<FixtureCleanupResult> }> {
  const fixtureRequest = await request.newContext({
    baseURL: requireLiveEnv('FIXTURE_API_URL'),
    ignoreHTTPSErrors: true,
  });
  const graph = toLiveFixtureGraph(identities, {
    tenantId: requireLiveEnv('TEST_TENANT_ID'),
    principalId: requireLiveEnv('TEST_PRINCIPAL_ID'),
  });

  try {
    const provisioned = await createLiveFixtureGraph(fixtureRequest, graph);
    return {
      graph: provisioned,
      cleanup: async () => {
        try {
          return await deleteLiveFixtureGraph(fixtureRequest, provisioned.graphId);
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
