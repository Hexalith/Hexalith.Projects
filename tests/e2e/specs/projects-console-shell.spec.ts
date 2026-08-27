import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
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

const SERVER_DERIVED_TENANT_LABEL = 'server-derived tenant';

/**
 * Story 5.3 shell + shared empty-state/feedback selector contract.
 *
 * The Story 5.4 inventory and read-only detail journeys live in their dedicated spec
 * (projects-inventory-detail.spec.ts); this file stays scoped to the shared shell, empty-state,
 * and feedback selectors so the two specs do not maintain divergent copies of the same assertions.
 *
 * These run only in the explicit live AppHost lane with authenticated operator context.
 * Later stories own the reference matrix, trace, audit export,
 * warning dashboard, mutation, and MCP/CLI journeys.
 */
test.describe('Projects console shell shared rendering', () => {
  liveAppHostTest('renders the Project Diagnostic Header with lifecycle badge, copyable ids, and shell navigation selectors', async ({
    page,
    tenantContext,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);

    await expect(detail.diagnosticHeader).toBeVisible();
    await expect(detail.diagnosticHeader).toContainText(SERVER_DERIVED_TENANT_LABEL);
    await expect(detail.diagnosticHeader).not.toContainText(tenantContext.tenantId);
    await expect(detail.diagnosticHeader).toContainText(seededProject.projectId);
    await expect(detail.tenantCopy).toHaveAttribute('data-copy-value', SERVER_DERIVED_TENANT_LABEL);
    await expect(detail.projectIdCopy).toHaveAttribute('data-copy-value', seededProject.projectId);
    await expect(detail.lifecycleBadge).toBeVisible();
    await expect(detail.lifecycleBadge).toContainText(/Active|Archived/);
    await expect(page.getByRole('navigation', { name: 'Projects' })).toBeVisible();
  });

  liveAppHostTest('renders real fixture references and safe-denial states without blank tables', async ({
    page,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);

    await page.getByTestId('project-detail-tab-references').click();
    await expect(detail.referenceHealthMatrix).toBeVisible();
    await expect(detail.referenceKindCells.filter({ hasText: 'conversation' }).first()).toBeVisible();

    await page.goto('/projects/not-a-canonical-project-id');
    await expect(page.getByTestId('project-feedback-fail-closed')).toBeVisible();
  });

  liveAppHostTest('renders a successful detail without echoing protected payload markers', async ({ page, seededProject }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);

    await expect(detail.diagnosticHeader).toBeVisible();
    await expect(detail.feedbackRegion).toHaveCount(0);

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_SHELL_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }
  });
});
