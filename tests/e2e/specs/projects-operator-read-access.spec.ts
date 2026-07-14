import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { getProjectOperatorDiagnostics } from '../support/helpers/projects-api-client.js';
import { queryHeaders } from '../support/helpers/correlation.js';

const FORBIDDEN_OPERATOR_MARKERS = [
  'tenantId',
  'idempotencyKey',
  'candidate',
  'score',
  'rank',
  'transcript',
  'prompt',
  'content',
  'token',
  'secret',
  'body',
  'BEGIN PRIVATE KEY',
  'Authorization: Bearer',
];

/**
 * Story 5.2 critical journey - operator read access.
 *
 * This suite runs only in the explicit live lane with stable real tenant/operator fixtures.
 * These tests bind the Story 5.2 public API
 * contract: metadata-only project diagnostics, bounded audit rows, query validation
 * after authorization, safe denial, and no payload leakage.
 */
test.describe('Projects operator read access', () => {
  liveAppHostTest('returns metadata-only project diagnostics with bounded audit evidence (Story 5.2 AC1,3,5)', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const { status, body } = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      { authToken, correlationId: 'corr-operator-read-happy', auditLimit: 25 },
    );

    expect(status).toBe(200);
    expect(body.projectId).toBe(seededProject.projectId);
    expect(body.lifecycleState).toBe('active');
    expect(body.contextActivation.enabled).toBe(true);
    expect(Array.isArray(body.references)).toBe(true);
    expect(body.auditTimeline.length).toBeLessThanOrEqual(25);
    expect(body.freshness.readConsistency).toBe('eventually_consistent');
    expect(body.freshness.trustState).toBe('trusted');

    for (const row of body.auditTimeline) {
      expect(row.auditEventId).toBeTruthy();
      expect(row.operationType).toMatch(/^[a-z][a-z0-9_.-]*$/);
      expect(row.projectionSequence).toBeGreaterThanOrEqual(0);
    }

    const serialized = JSON.stringify(body);
    for (const marker of FORBIDDEN_OPERATOR_MARKERS) {
      expect(serialized).not.toContain(marker);
    }
  });

  liveAppHostTest('rejects query idempotency and non-eventual freshness after authorization (Story 5.2 AC2,7)', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const idempotencyRejected = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-operator-idempotency',
        extraHeaders: { 'Idempotency-Key': 'operator-query-is-not-a-command' },
      },
    );
    expect(idempotencyRejected.status).toBe(400);
    expect(JSON.stringify(idempotencyRejected.body)).not.toContain(seededProject.name);

    const freshnessRejected = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-operator-freshness',
        freshness: 'strong',
      },
    );
    expect(freshnessRejected.status).toBe(400);
    expect(JSON.stringify(freshnessRejected.body)).not.toContain(seededProject.name);
  });

  liveAppHostTest('collapses malformed or unauthorized project reads to safe denial (Story 5.2 AC2)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const { status, body } = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      'not/a/canonical/project-id',
      { authToken, correlationId: 'corr-operator-malformed' },
    );

    expect(status).toBe(404);
    expect(JSON.stringify(body)).not.toContain('not/a/canonical/project-id');
  });

  liveAppHostTest('does not disclose existence to unauthenticated operator probes (Story 5.2 AC1,2)', async ({
    apiRequest,
    tenantContext,
    seededProject,
  }) => {
    const { status, body } = await apiRequest({
      method: 'GET',
      path: `/api/v1/projects/${seededProject.projectId}/operator-diagnostics`,
      headers: {
        ...queryHeaders({ authToken: '', correlationId: 'corr-operator-no-auth' }),
        Authorization: '',
        'X-Hexalith-Tenant-Id': tenantContext.tenantId,
      },
      retryConfig: { maxRetries: 0 },
    });

    expect([401, 403, 404]).toContain(status);
    expect(JSON.stringify(body)).not.toContain(seededProject.projectId);
    expect(JSON.stringify(body)).not.toContain(seededProject.name);
  });
});
