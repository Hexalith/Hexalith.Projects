import { test, expect } from '../support/merged-fixtures.js';
import { ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

const FORBIDDEN_SHELL_MARKERS = [
  'transcript',
  'raw prompt',
  'BEGIN PRIVATE KEY',
  'Authorization: Bearer',
  'secret token',
  'proposal body',
  'command body',
];

/**
 * Story 5.3 shell-level selector contract.
 *
 * These remain fixme until the stable AppHost/UI fixture can launch the FrontComposer console
 * with authenticated operator context. Later stories own the concrete inventory/detail, reference
 * matrix, trace, audit, warning, mutation, and MCP/CLI journeys.
 */
test.describe('Projects console shell shared rendering', () => {
  test.fixme('renders the Project Diagnostic Header with lifecycle badge, copyable ids, and shell navigation selectors', async ({
    page,
    tenantContext,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);

    await expect(detail.diagnosticHeader).toBeVisible();
    await expect(detail.diagnosticHeader).toContainText(tenantContext.tenantId);
    await expect(detail.diagnosticHeader).toContainText(seededProject.projectId);
    await expect(detail.tenantCopy).toHaveAttribute('data-copy-value', tenantContext.tenantId);
    await expect(detail.projectIdCopy).toHaveAttribute('data-copy-value', seededProject.projectId);
    await expect(detail.lifecycleBadge).toBeVisible();
    await expect(detail.lifecycleBadge).toContainText(/Active|Archived/);
    await expect(page.getByTestId(/^fc-nav-/)).toBeVisible();
  });

  test.fixme('distinguishes no-data, denied, unavailable, and filtered empty states without blank tables', async ({
    page,
  }) => {
    await page.goto('/projects/project-with-filtered-empty-results');

    await expect(page.getByTestId('project-empty-filtered')).toBeVisible();
    await expect(page.getByTestId('project-empty-denied').or(page.getByTestId('project-feedback-fail-closed'))).toBeVisible();
    await expect(page.getByTestId('project-empty-unavailable').or(page.getByTestId('project-feedback-warning'))).toBeVisible();
    await expect(page.getByTestId('project-empty-none')).toBeVisible();
  });

  test.fixme('renders safe feedback categories without echoing protected payload markers', async ({ page }) => {
    await page.goto('/projects/project-with-feedback-examples');

    await expect(page.getByTestId('project-feedback-success')).toBeVisible();
    await expect(page.getByTestId('project-feedback-warning')).toBeVisible();
    await expect(page.getByTestId('project-feedback-error')).toBeVisible();
    await expect(page.getByTestId('project-feedback-fail-closed')).toBeVisible();
    await expect(page.getByTestId('project-feedback-loading')).toBeVisible();

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_SHELL_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }
  });
});
