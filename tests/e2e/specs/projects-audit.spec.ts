import { test, expect } from '../support/merged-fixtures.js';
import { queryHeaders } from '../support/helpers/correlation.js';

/**
 * F5 critical journey — audit timeline (FR-21 / F1) + NoPayloadLeakage (NFR-2 / R2).
 *
 * `test.fixme` until AppHost/browser provisioning is available in CI. Asserts the timeline/export
 * contract is metadata-only over the Story 5.2 operator diagnostic endpoint rather than a duplicate
 * `/audit` route.
 */
const FORBIDDEN_PAYLOAD_MARKERS = [
  'BEGIN PRIVATE KEY',
  'AKIA', // AWS access key id prefix
  'password=',
  'Authorization: Bearer',
];

test.describe('Projects audit timeline', () => {
  test.fixme('operator diagnostics audit timeline is metadata-only with no leaked payloads (FR-21 / NFR-2)', async ({ apiRequest, authToken, tenantContext, seededProject }) => {
    const { status, body } = await apiRequest<{ auditTimeline: Array<Record<string, unknown>> }>({
      method: 'GET',
      path: `/api/v1/projects/${seededProject.projectId}/operator-diagnostics?auditLimit=25`,
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    expect(status).toBe(200);
    expect(body.auditTimeline.length).toBeGreaterThan(0);

    const firstEntry = body.auditTimeline[0];
    // Required safe metadata fields present (ProjectOperatorAuditTimelineItem wire names).
    expect(firstEntry).toHaveProperty('operationType');
    expect(firstEntry).toHaveProperty('correlationId');
    expect(firstEntry).toHaveProperty('auditEventId');

    // No forbidden payload anywhere in the serialized timeline (defense-in-depth E2E re-run of FS-2).
    const serialized = JSON.stringify(body);
    for (const marker of FORBIDDEN_PAYLOAD_MARKERS) {
      expect(serialized).not.toContain(marker);
    }
  });

  test.fixme('audit timeline renders as a screen-reader-readable list and safe export (UX-DR16/18)', async ({ page, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);
    await page.getByTestId('project-detail-tab-audit').click();
    await expect(page.getByTestId('audit-timeline')).toBeVisible();
    // Entries are individually addressable for assistive tech.
    await expect(page.getByTestId('audit-timeline-entry').first()).toBeVisible();
    await expect(page.getByTestId('safe-diagnostic-export-preview')).toBeVisible();
    await expect(page.getByTestId('safe-diagnostic-export-download')).toBeVisible();
  });
});
