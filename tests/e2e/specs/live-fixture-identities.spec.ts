import { test, expect } from '@playwright/test';

import {
  assertDisjointLiveFixtureDimensions,
  createLiveFixtureIdentities,
  createLiveRequestIdentity,
  type LiveFixtureDimensions,
} from '../support/factories/live-fixture-identities.js';
import { LIVE_FIXTURE_GRAPH_FIELDS, toLiveFixtureGraph } from '../support/helpers/live-fixtures-api-client.js';

/** Mirrors the Projects API canonical identifier rule for correlation/task/idempotency headers. */
const CANONICAL_IDENTIFIER = /^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$/;

const base: LiveFixtureDimensions = {
  runId: 'run-contract',
  workerIndex: 0,
  retry: 0,
  repeatEachIndex: 0,
  scenario: 'identity-contract',
};

test.describe('live fixture identity factory', () => {
  test('is deterministic, bounded, and URL-safe', () => {
    const first = createLiveFixtureIdentities(base);
    const second = createLiveFixtureIdentities(base);

    expect(second).toEqual(first);
    for (const [name, value] of Object.entries(first)) {
      if (typeof value !== 'string' || name === 'runId' || name === 'scenario') continue;
      expect(value.length, name).toBeLessThanOrEqual(96);
      expect(value, name).toMatch(/^[a-z0-9]+(?:-[a-z0-9]+)*$/);
    }
  });

  test('is disjoint across run, worker, retry, repeat, and scenario dimensions', () => {
    const variants: LiveFixtureDimensions[] = [
      base,
      { ...base, runId: 'run-contract-full' },
      { ...base, workerIndex: 1 },
      { ...base, retry: 1 },
      { ...base, repeatEachIndex: 1 },
      { ...base, scenario: 'another-scenario' },
    ];
    // Every entity and request identity of every variant is globally unique, not only the ProjectId.
    const ids = variants.flatMap((dimensions) => {
      const { runId: _runId, workerIndex: _worker, retry: _retry, repeatEachIndex: _repeat, scenario: _scenario, ...identities } =
        createLiveFixtureIdentities(dimensions);
      return Object.values(identities);
    });

    expect(new Set(ids).size).toBe(ids.length);
    expect(() => assertDisjointLiveFixtureDimensions(variants)).not.toThrow();
  });

  test('derives canonical operation-scoped request identities that never repeat', () => {
    const attempts = [base, { ...base, retry: 1 }, { ...base, workerIndex: 1 }].map((dimensions) =>
      createLiveFixtureIdentities(dimensions),
    );
    const identities = attempts.flatMap((attempt) =>
      ['create', 'archive', 'confirm'].map((operation) => createLiveRequestIdentity(attempt, operation)),
    );
    const values = identities.flatMap((identity) => [identity.correlationId, identity.taskId, identity.idempotencyKey]);

    expect(new Set(values).size).toBe(values.length);
    for (const value of values) expect(value).toMatch(CANONICAL_IDENTIFIER);
    expect(createLiveRequestIdentity(attempts[0], 'create')).toEqual(createLiveRequestIdentity(attempts[0], 'create'));
    expect(() => createLiveRequestIdentity(attempts[0], ' ')).toThrow(/operation are required/);
  });

  test('builds the symmetric wire graph with exactly the fixture DTO members', () => {
    const graph = toLiveFixtureGraph(
      createLiveFixtureIdentities({ ...base, scenario: 's'.repeat(200) }),
      { tenantId: 'tenant-contract', principalId: 'principal-contract' },
    );

    expect(Object.keys(graph)).toEqual([...LIVE_FIXTURE_GRAPH_FIELDS]);
    expect(graph.scenario).toHaveLength(128);
    expect(graph).toMatchObject({ tenantId: 'tenant-contract', principalId: 'principal-contract', filePath: 'docs/contract.pdf' });
  });

  test('rejects duplicate attempt dimensions', () => {
    expect(() => assertDisjointLiveFixtureDimensions([base, { ...base }])).toThrow(/duplicate fixture dimensions/);
  });
});
