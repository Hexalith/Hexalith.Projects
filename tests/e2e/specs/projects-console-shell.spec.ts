import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { createTrackedProject } from '../support/fixtures/projects-fixtures.js';
import { openProjectDetailSection, ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

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
 * These run only in the explicit live AppHost lane with authenticated operator context. Every
 * empty, feedback, and degraded state is driven by attempt-scoped fixture outputs: a Project with no
 * linked references, the graph's degraded Project whose sibling conversation read is unavailable and slow,
 * UI validation of an empty trace input, a clipboard permission decision, and a safe denial.
 *
 * Not producible live, by construction of the UI: the `project-empty-denied` and
 * `project-empty-unavailable` components have no call site (denial renders `project-feedback-fail-closed`,
 * unavailable sibling evidence renders an Unavailable reference row), `project-empty-filtered`
 * depends on every other Project in the shared tenant, so no attempt-scoped filter can make it empty,
 * and `project-empty-none` cannot render live either: the references tab always carries the required
 * Project Folder lane (Pending until a folder is set), and the inventory and warnings lists are shared.
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

    await openProjectDetailSection(page, 'references');
    await expect(detail.referenceHealthMatrix).toBeVisible();
    await expect(detail.referenceKindCells.filter({ hasText: 'conversation' }).first()).toBeVisible();

    await page.goto('/projects/not-a-canonical-project-id');
    await expect(page.getByTestId('project-feedback-fail-closed')).toBeVisible();
  });

  liveAppHostTest('renders an unlinked Project, unavailable evidence, and denied access as explicit states without blank tables', async ({
    page,
    apiRequest,
    authToken,
    recurse,
    tenantContext,
    liveCleanup,
    liveFixtureGraph,
  }) => {
    const deps = { apiRequest, authToken, recurse, tenantContext, graph: liveFixtureGraph };
    const empty = await createTrackedProject(deps, liveCleanup, liveFixtureGraph.secondaryProjectId, 'console-empty');
    const degraded = await createTrackedProject(deps, liveCleanup, liveFixtureGraph.degradedProjectId, 'console-degraded');
    const detail = new ProjectDetailPage(page);

    // Nothing linked: the fixture graph links nothing to this Project, and the required Project Folder lane
    // still renders as an explicit Pending row instead of a blank table.
    await detail.goto(empty.projectId);
    await openProjectDetailSection(page, 'references');
    await expect(detail.referenceHealthMatrix).toBeVisible();
    const folderLane = detail.referenceHealthRows.filter({ has: detail.referenceKindCells.filter({ hasText: 'folder' }) });
    await expect(folderLane.first().getByTestId('project-reference-state')).toContainText('Pending');
    await expect(detail.referenceHealthRows.filter({ has: detail.referenceKindCells.filter({ hasText: /^(file|memory|conversation)$/ }) })).toHaveCount(0);

    // Unavailable: the degraded Project's unavailable sibling conversation renders an explicit row, not a blank table.
    await detail.goto(degraded.projectId);
    await openProjectDetailSection(page, 'references');
    // The conversation renders from both the Conversations-backed list and context evaluation; each row says so.
    const unavailableRows = detail.referenceHealthRows.filter({ hasText: liveFixtureGraph.unavailableConversationId });
    await expect(unavailableRows.first()).toBeVisible();
    for (let index = 0; index < (await unavailableRows.count()); index++) {
      await expect(unavailableRows.nth(index).getByTestId('project-reference-state')).toContainText('Unavailable');
    }

    // Denied: a canonical Project identity the caller cannot see collapses to the safe fail-closed feedback.
    await page.goto(`/projects/${liveFixtureGraph.proposalRetryProjectId}`);
    await expect(page.getByTestId('project-feedback-fail-closed')).toBeVisible();
  });

  liveAppHostTest('renders loading, error, success, warning, and fail-closed feedback from live outcomes', async ({
    page,
    browser,
    apiRequest,
    authToken,
    recurse,
    tenantContext,
    liveCleanup,
    liveFixtureGraph,
    seededProject,
  }) => {
    const deps = { apiRequest, authToken, recurse, tenantContext, graph: liveFixtureGraph };
    const degraded = await createTrackedProject(deps, liveCleanup, liveFixtureGraph.degradedProjectId, 'console-feedback');
    const detail = new ProjectDetailPage(page);

    // Loading: the fixture's unavailable conversation read is deliberately slow, so the trace stays in flight.
    await detail.goto(degraded.projectId);
    await openProjectDetailSection(page, 'resolution');
    await detail.resolutionTraceConversationId.fill(liveFixtureGraph.unavailableConversationId);
    await detail.resolutionTraceRun.click();
    await expect(page.getByTestId('project-feedback-loading')).toBeVisible();
    await expect(detail.resolutionTraceOutcome).toBeVisible();

    // Error: an empty conversation trace input is rejected by UI validation before any request.
    await detail.resolutionTraceConversationId.fill('');
    await detail.resolutionTraceRun.click();
    await expect(page.getByTestId('project-feedback-error')).toBeVisible();

    // Success: with clipboard permission the audit copy action completes.
    await page.context().grantPermissions(['clipboard-read', 'clipboard-write']);
    await detail.goto(seededProject.projectId);
    await openProjectDetailSection(page, 'audit');
    await detail.auditTimelineCopy.first().click();
    await expect(detail.safeDiagnosticExportFeedback.getByTestId('project-feedback-success')).toBeVisible();

    // Warning: a browser context without clipboard permission reports the copy as unavailable.
    const denied = await browser.newContext({
      baseURL: process.env.BASE_URL,
      ignoreHTTPSErrors: true,
      storageState: await page.context().storageState(),
    });
    try {
      const deniedPage = await denied.newPage();
      const deniedDetail = new ProjectDetailPage(deniedPage);
      await deniedDetail.goto(seededProject.projectId);
      await openProjectDetailSection(deniedPage, 'audit');
      await deniedDetail.auditTimelineCopy.first().click();
      await expect(deniedDetail.safeDiagnosticExportFeedback.getByTestId('project-feedback-warning')).toBeVisible();
    } finally {
      await denied.close();
    }

    // Fail-closed: a non-canonical Project identity is safely denied.
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
