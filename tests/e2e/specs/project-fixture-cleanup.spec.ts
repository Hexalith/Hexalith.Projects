import { test, expect } from '@playwright/test';

import { createTenantContext } from '../support/factories/tenant-factory.js';
import { projectArchiveStep, type SeedProjectDeps } from '../support/fixtures/projects-fixtures.js';
import type { ApiRequest } from '../support/helpers/projects-api-client.js';
import type { Recurse } from '../support/helpers/readiness.js';

test.describe('Project archive cleanup diagnostics contract', () => {
  for (const status of [503, 504, null]) {
    test(`records the actual final observation ${status ?? 'transport failure'} without fabricating a 404`, async () => {
      const apiRequest = (async () => {
        if (status === null) throw new Error('transport failure with private endpoint');
        return { status, body: {} };
      }) as unknown as ApiRequest;
      const recurse: Recurse = async (command, predicate) => {
        const result = await command();
        if (!predicate(result)) throw new Error('convergence deadline');
        return result;
      };
      const deps: SeedProjectDeps = {
        apiRequest,
        recurse,
        authToken: 'offline-token',
        tenantContext: createTenantContext({ tenantId: 'cleanup-contract' }),
        graph: { graphId: 'cleanup-contract-graph' },
      };
      const result = await projectArchiveStep(deps, 'cleanup-contract-project', 'projects:primary').run();
      expect(result).toEqual([{ role: 'projects:primary', statusCode: status, succeeded: false }]);
      expect(JSON.stringify(result)).not.toContain('private endpoint');
    });
  }
});
