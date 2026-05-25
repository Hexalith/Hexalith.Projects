import { test, expect } from '../support/merged-fixtures.js';
import { queryHeaders } from '../support/helpers/correlation.js';

/**
 * F5 critical journey — audit timeline (FR-21 / F1) + NoPayloadLeakage (NFR-2 / R2).
 *
 * `test.fixme` until the audit projection + console exist. Asserts the timeline is
 * metadata-only: it carries tenant, project id, operation, state transitions, affected
 * reference IDs, correlation id, and audit event id — and NEVER transcripts, file
 * contents, memory bodies, prompts, secrets, or raw tokens.
 */
const FORBIDDEN_PAYLOAD_MARKERS = [
  'BEGIN PRIVATE KEY',
  'AKIA', // AWS access key id prefix
  'password=',
  'Authorization: Bearer',
];

test.describe('Projects audit timeline', () => {
  test.fixme('audit timeline is metadata-only with no leaked payloads (FR-21 / NFR-2)', async ({ apiRequest, authToken, tenantContext, seededProject }) => {
    const { status, body } = await apiRequest<{ entries: Array<Record<string, unknown>> }>({
      method: 'GET',
      path: `/api/v1/projects/${seededProject.projectId}/audit`,
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    expect(status).toBe(200);
    expect(body.entries.length).toBeGreaterThan(0);

    const firstEntry = body.entries[0];
    // Required safe metadata fields present.
    expect(firstEntry).toHaveProperty('operation');
    expect(firstEntry).toHaveProperty('correlationId');
    expect(firstEntry).toHaveProperty('auditEventId');

    // No forbidden payload anywhere in the serialized timeline (defense-in-depth E2E re-run of FS-2).
    const serialized = JSON.stringify(body);
    for (const marker of FORBIDDEN_PAYLOAD_MARKERS) {
      expect(serialized).not.toContain(marker);
    }
  });

  test.fixme('audit timeline renders as a screen-reader-readable list (UX-DR16)', async ({ page, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}/audit`);
    await expect(page.getByTestId('audit-timeline')).toBeVisible();
    // Entries are individually addressable for assistive tech.
    await expect(page.getByTestId('audit-timeline-entry').first()).toBeVisible();
  });
});
