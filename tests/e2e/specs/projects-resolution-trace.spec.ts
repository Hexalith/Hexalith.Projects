import { test, expect } from '../support/merged-fixtures.js';
import { expectNoA11yViolations } from '../support/helpers/a11y.js';
import { ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

const FORBIDDEN_TRACE_MARKERS = [
  'tenantId',
  'workspace',
  'transcript',
  'raw prompt',
  'file content',
  'memory payload',
  'proposal body',
  'command body',
  'ProblemDetails',
  'BEGIN PRIVATE KEY',
  'Authorization: Bearer',
  'secret token',
];

/**
 * Story 5.6 critical journeys - Resolution Trace Workbench.
 *
 * These remain `test.fixme` until AppHost fixtures expose deterministic trace data for
 * conversation and attachment resolution. The assertions bind the Web journey contract:
 * explicit compute-on-demand inputs, stable selectors, keyboard access, non-color-only
 * result/reason/exclusion states, and metadata-only rendering.
 */
test.describe('Project resolution trace workbench (Story 5.6)', () => {
  test.fixme('renders stable selectors and supports the keyboard path', async ({ page, seededProject }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);
    await page.getByTestId('project-detail-tab-resolution').click();

    await expect(detail.resolutionTraceWorkbench).toBeVisible();
    await expect(detail.resolutionTraceMode).toBeVisible();
    await expect(detail.resolutionTraceConversationId).toBeVisible();
    await expect(detail.resolutionTraceIncludeArchived).toBeVisible();
    await expect(detail.resolutionTraceRun).toBeVisible();

    await detail.resolutionTraceMode.focus();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceConversationId).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceIncludeArchived).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceRun).toBeFocused();
  });

  test.fixme('runs conversation and attachment traces without leaking payload data', async ({ page, seededProject }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);
    await page.getByTestId('project-detail-tab-resolution').click();

    await detail.resolutionTraceConversationId.fill('conversation-001');
    await detail.resolutionTraceRun.click();
    await expect(detail.resolutionTraceOutcome).toBeVisible();
    await expect(detail.resolutionTraceInputSummary).toContainText('conversation');
    await expect(detail.resolutionTraceCandidates.first()).toBeVisible();
    await expect(detail.resolutionTraceReasons.first()).toBeVisible();

    await detail.resolutionTraceMode.selectOption('attachments');
    await detail.resolutionTraceFolderId.fill('folder-001');
    await detail.resolutionTraceFileId.fill('file-001');
    await detail.resolutionTraceRun.click();
    await expect(detail.resolutionTraceCandidateComparison).toBeVisible();
    await expect(detail.resolutionTraceOutcome).toContainText(/Resolved|NoMatch|MultipleCandidates|Excluded|FailedClosed/);

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_TRACE_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }
  });

  test.fixme('passes axe accessibility scan after a trace renders', async ({ page, seededProject }, testInfo) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);
    await page.getByTestId('project-detail-tab-resolution').click();
    await detail.resolutionTraceConversationId.fill('conversation-001');
    await detail.resolutionTraceRun.click();

    await expect(detail.resolutionTraceOutcome).toBeVisible();
    await expectNoA11yViolations(page, testInfo, { include: '[data-testid="project-detail-section-resolution"]' });
  });
});
