import { test, expect } from '@playwright/test';

import {
  assertDisjointLiveFixtureDimensions,
  createLiveFixtureIdentities,
  type LiveFixtureDimensions,
} from '../support/factories/live-fixture-identities.js';

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

  test('is disjoint across worker, retry, repeat, and scenario dimensions', () => {
    const variants: LiveFixtureDimensions[] = [
      base,
      { ...base, workerIndex: 1 },
      { ...base, retry: 1 },
      { ...base, repeatEachIndex: 1 },
      { ...base, scenario: 'another-scenario' },
    ];
    const projectIds = variants.map((dimensions) => createLiveFixtureIdentities(dimensions).projectId);

    expect(new Set(projectIds).size).toBe(variants.length);
    expect(() => assertDisjointLiveFixtureDimensions(variants)).not.toThrow();
  });

  test('rejects duplicate attempt dimensions', () => {
    expect(() => assertDisjointLiveFixtureDimensions([base, { ...base }])).toThrow(/duplicate fixture dimensions/);
  });
});
