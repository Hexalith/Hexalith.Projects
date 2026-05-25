import { test, expect } from '../support/merged-fixtures.js';

/**
 * F5 critical journey — audit-first maintenance with dry-run (UX-DR17 / UX-DR21 / UX-DR25).
 *
 * `test.fixme` until the Epic 5 console + maintenance commands exist. Demonstrates the
 * five-state command lifecycle (Idle → Submitting → Acknowledged(202) → Syncing → Confirmed)
 * and the dry-run-before-execute confirmation contract, driven through the web console with
 * network-first interception and deterministic readiness (no sleeps).
 */
test.describe('Projects maintenance (archive / dry-run)', () => {
  test.fixme('dry-run an archive shows the expected audit event before execution (UX-DR17/25)', async ({ page, interceptNetworkCall, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    const dryRunCall = interceptNetworkCall({ url: `**/api/v1/projects/${seededProject.projectId}/archive:dry-run` });
    await page.getByTestId('action-archive').click();
    await page.getByTestId('action-archive-dry-run').click();

    const { responseJson } = await dryRunCall;
    const dryRun = responseJson as { proposedState: string; expectedAuditEvent: unknown };
    // Confirmation surfaces current→proposed state + expected audit event, no execution yet.
    expect(dryRun.proposedState).toBe('archived');
    expect(dryRun.expectedAuditEvent).toBeTruthy();
    await expect(page.getByTestId('action-archive-confirm')).toBeEnabled();
  });

  test.fixme('confirmed archive flows through the five-state lifecycle to Confirmed (UX-DR21)', async ({ page, interceptNetworkCall, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    const archiveCall = interceptNetworkCall({ url: `**/api/v1/projects/${seededProject.projectId}/archive` });
    await page.getByTestId('action-archive').click();
    await page.getByTestId('action-archive-confirm').click();

    const { status } = await archiveCall;
    expect(status).toBe(202); // Acknowledged

    // Syncing → Confirmed: the UI re-queries after the nudge; assert the terminal state
    // via the visible badge rather than a fixed wait.
    await expect(page.getByTestId('project-lifecycle-badge')).toHaveText(/archived/i);
  });
});
