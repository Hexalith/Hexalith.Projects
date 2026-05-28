import { test, expect } from '../support/merged-fixtures.js';
import { queryHeaders, mutationHeaders } from '../support/helpers/correlation.js';

/**
 * F5 critical journey — resolution → confirm (FR-12/13/14; E1 / R10).
 *
 * `test.fixme` until the resolution API + CL-2 scoring heuristics are defined (the test
 * design blocks PRECISE candidate assertions on CL-2). What IS assertable today and is
 * scaffolded here: the binary outcomes and the never-silently-attach guarantee (NFR-9).
 *
 * Endpoint paths are placeholders pending the OpenAPI spine (Story 1.3 / Epic 4).
 */
test.describe('Projects resolution', () => {
  test.fixme('ambiguous resolution returns MultipleCandidates and never silently attaches (E1 / R10)', async ({ apiRequest, authToken, tenantContext }) => {
    const { status, body } = await apiRequest<{ outcome: string; candidates: unknown[]; attached: boolean }>({
      method: 'POST',
      path: '/api/v1/resolution/from-conversation',
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: { conversationId: 'conv-ambiguous' },
    });
    expect(status).toBe(200);
    expect(body.outcome).toBe('MultipleCandidates');
    // NFR-9: ambiguity asks for confirmation; nothing is attached automatically.
    expect(body.attached).toBe(false);
    expect(body.candidates.length).toBeGreaterThan(1);
  });

  test.fixme('confirming a candidate persists only the confirmed choice (FR-14)', async ({ apiRequest, authToken, recurse, tenantContext, seededProject }) => {
    const { status } = await apiRequest({
      method: 'POST',
      path: '/api/v1/resolution/confirm',
      headers: { ...mutationHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: { conversationId: 'conv-ambiguous', projectId: seededProject.projectId },
    });
    expect(status).toBe(202);

    // Converge on the persisted confirmation (no sleeps).
    await recurse(
      () =>
        apiRequest<{ confirmedProjectId?: string }>({
          method: 'GET',
          path: '/api/v1/resolution/state',
          params: { conversationId: 'conv-ambiguous' },
          headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
        }),
      (res) => res.body.confirmedProjectId === seededProject.projectId,
      { timeout: 30_000, interval: 1_000, log: 'Waiting for confirmed resolution to persist' },
    );
  });

  test.fixme('archived projects are excluded from resolution unless explicitly requested (E1)', async ({ apiRequest, authToken, tenantContext }) => {
    const { body } = await apiRequest<{ candidates: Array<{ lifecycle: string }> }>({
      method: 'POST',
      path: '/api/v1/resolution/from-conversation',
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: { conversationId: 'conv-1', includeArchived: false },
    });
    expect(body.candidates.every((c) => c.lifecycle !== 'archived')).toBe(true);
  });
});
