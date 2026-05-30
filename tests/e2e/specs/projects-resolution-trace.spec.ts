import { test, expect } from '../support/merged-fixtures.js';
import { expectNoA11yViolations } from '../support/helpers/a11y.js';
import { ProjectDetailPage } from '../support/page-objects/project-detail.page.js';
import { test as base } from '@playwright/test';

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

base.describe('Project resolution trace selector and accessibility contract (no app required)', () => {
  base('supports keyboard traversal and metadata-only trace rendering', async ({ page }) => {
    await page.setContent(resolutionTraceFixture());
    const detail = new ProjectDetailPage(page);

    await expect(detail.resolutionTraceWorkbench).toBeVisible();
    await expect(detail.resolutionTraceMode).toBeVisible();
    await expect(detail.resolutionTraceConversationId).toBeVisible();
    await expect(detail.resolutionTraceIncludeArchived).toBeVisible();
    await expect(detail.resolutionTraceRun).toBeVisible();

    await detail.resolutionTraceMode.focus();
    await expect(detail.resolutionTraceMode).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceConversationId).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceFolderId).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceFileId).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceIncludeArchived).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(detail.resolutionTraceRun).toBeFocused();

    await expect(detail.resolutionTraceOutcome).toContainText('Resolved');
    await expect(detail.resolutionTraceInputSummary).toContainText('conversation-001');
    await expect(detail.resolutionTraceCandidateComparison.getByRole('table', { name: 'Resolution candidates' })).toBeVisible();
    await expect(detail.resolutionTraceReasons.first()).toContainText('ConversationMatched');
    await expect(detail.resolutionTraceExclusions.first()).toContainText('Policy redaction');

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_TRACE_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }
  });

  base('passes axe scan after a fixture trace renders', async ({ page }, testInfo) => {
    await page.setContent(resolutionTraceFixture());
    await expectNoA11yViolations(page, testInfo, { include: '[data-testid="project-detail-section-resolution"]' });
  });
});

function resolutionTraceFixture(): string {
  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Resolution trace selector contract</title>
    <style>
      body { margin: 0; color: #111; background: #fff; font-family: Arial, sans-serif; }
      main { max-width: 72rem; margin: 0 auto; padding: 1rem; }
      code, td, th { overflow-wrap: anywhere; }
      :focus-visible { outline: 3px solid #005a9e; outline-offset: 2px; }
    </style>
  </head>
  <body>
    <main>
      <section data-testid="project-detail-section-resolution" aria-labelledby="resolution-heading">
        <h1 id="resolution-heading">Resolution trace</h1>
        <form data-testid="project-resolution-trace-workbench" aria-label="Resolution trace workbench">
          <label>Trace mode
            <select data-testid="project-resolution-trace-mode">
              <option value="conversation">Conversation</option>
              <option value="attachments">Attachments</option>
            </select>
          </label>
          <label>Conversation id
            <input data-testid="project-resolution-trace-conversation-id" value="conversation-001" />
          </label>
          <label>Folder id
            <input data-testid="project-resolution-trace-folder-id" value="folder-001" />
          </label>
          <label>File id
            <input data-testid="project-resolution-trace-file-id" value="file-001" />
          </label>
          <label>
            <input data-testid="project-resolution-trace-include-archived" type="checkbox" />
            Include archived projects
          </label>
          <button data-testid="project-resolution-trace-run" type="button">Run trace</button>
        </form>
        <section data-testid="project-resolution-trace-outcome" role="status" aria-live="polite">
          Resolved with server-derived tenant scope and no payload body.
        </section>
        <p data-testid="project-resolution-trace-input-summary">
          conversation <code>conversation-001</code>, folder <code>folder-001</code>, file <code>file-001</code>
        </p>
        <section data-testid="project-resolution-trace-candidate-comparison" aria-labelledby="candidate-heading">
          <h2 id="candidate-heading">Candidate comparison</h2>
          <table aria-label="Resolution candidates">
            <thead>
              <tr>
                <th scope="col">Project</th>
                <th scope="col">Result</th>
                <th scope="col">Reason</th>
                <th scope="col">Safe evidence</th>
              </tr>
            </thead>
            <tbody>
              <tr data-testid="project-resolution-trace-candidate">
                <th scope="row"><code>project-001</code></th>
                <td>Resolved</td>
                <td data-testid="project-resolution-trace-reason">ConversationMatched</td>
                <td>Metadata-only audit identifier <code>audit-001</code></td>
              </tr>
            </tbody>
          </table>
        </section>
        <ul aria-label="Resolution exclusions">
          <li data-testid="project-resolution-trace-exclusion">Policy redaction excluded from payload rendering.</li>
        </ul>
        <div data-testid="project-resolution-trace-feedback" role="status">Trace completed safely.</div>
      </section>
    </main>
  </body>
</html>`;
}
