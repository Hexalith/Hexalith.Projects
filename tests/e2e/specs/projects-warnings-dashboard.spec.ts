import { test as base } from '@playwright/test';
import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { expectNoA11yViolations } from '../support/helpers/a11y.js';
import { getProjectOperatorDiagnostics, listProjects } from '../support/helpers/projects-api-client.js';
import { ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

const FORBIDDEN_WARNING_MARKERS = [
  'transcript',
  'raw prompt',
  'command body',
  'proposal body',
  'idempotencyKey',
  'candidateScore',
  'candidateRank',
  'rejectedCandidateId',
  'BEGIN PRIVATE KEY',
  'Authorization: Bearer',
  'secret token',
];

/**
 * Story 5.8 critical journeys - warnings queue and operational dashboard.
 *
 * Live-gated until authenticated AppHost/browser provisioning is explicitly enabled. These
 * selectors are kept ready for the Story 5.11 responsive/a11y hardening lane.
 */
test.describe('Projects warnings queue and operational dashboard (Story 5.8)', () => {
  liveAppHostTest('loads warning dashboard metadata through bounded query enrichment only', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const list = await listProjects(
      apiRequest,
      tenantContext.tenantId,
      {
        authToken,
        correlationId: 'corr-story-5-8-warning-dashboard-list',
        freshness: 'eventually_consistent',
      },
      'active',
    );

    expect(list.status).toBe(200);
    const visibleProject = list.body.items.find((item) => item.projectId === seededProject.projectId);
    expect(visibleProject).toBeTruthy();
    expect(list.body.freshness.readConsistency).toBe('eventually_consistent');

    const diagnostics = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        auditLimit: 25,
        correlationId: 'corr-story-5-8-warning-dashboard-diagnostics',
        freshness: 'eventually_consistent',
      },
    );

    expect(diagnostics.status).toBe(200);
    expect(diagnostics.body.projectId).toBe(seededProject.projectId);
    expect(Array.isArray(diagnostics.body.references)).toBe(true);
    expect(Array.isArray(diagnostics.body.auditTimeline)).toBe(true);
    expect(diagnostics.body.freshness.readConsistency).toBe('eventually_consistent');

    const serialized = JSON.stringify({ list: list.body, diagnostics: diagnostics.body });
    expect(serialized).not.toContain(tenantContext.tenantId);
    for (const marker of FORBIDDEN_WARNING_MARKERS) {
      expect(serialized).not.toContain(marker);
    }
  });

  liveAppHostTest('rejects warning dashboard query misuse without echoing project metadata', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const listWithIdempotency = await listProjects(apiRequest, tenantContext.tenantId, {
      authToken,
      correlationId: 'corr-story-5-8-list-idempotency',
      extraHeaders: { 'Idempotency-Key': 'warning-dashboard-is-read-only' },
    });
    expect(listWithIdempotency.status).toBe(400);
    expect(JSON.stringify(listWithIdempotency.body)).not.toContain(seededProject.projectId);
    expect(JSON.stringify(listWithIdempotency.body)).not.toContain(seededProject.name);

    const diagnosticsWithIdempotency = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        auditLimit: 25,
        correlationId: 'corr-story-5-8-diagnostics-idempotency',
        extraHeaders: { 'Idempotency-Key': 'warning-dashboard-enrichment-is-read-only' },
      },
    );
    expect(diagnosticsWithIdempotency.status).toBe(400);
    expect(JSON.stringify(diagnosticsWithIdempotency.body)).not.toContain(seededProject.name);
  });

  liveAppHostTest('renders dashboard tiles, queue rows, filters, and safe read-only drill-ins', async ({ page }) => {
    const warnings = new ProjectDetailPage(page);
    await warnings.gotoWarnings();

    await expect(warnings.warningsDashboard).toBeVisible();
    await expect(warnings.dashboardTiles.first()).toBeVisible();
    await expect(warnings.warningLifecycleFilter).toBeVisible();
    await expect(warnings.warningStateFilter).toBeVisible();
    await expect(warnings.warningReasonFilter).toBeVisible();
    await expect(warnings.warningReferenceTypeFilter).toBeVisible();
    await expect(warnings.warningsQueue.or(warnings.warningEmpty)).toBeVisible();

    if (await warnings.warningRows.count() > 0) {
      await expect(warnings.warningState.first()).toBeVisible();
      await expect(warnings.warningReason.first()).toBeVisible();
      await expect(warnings.warningReference.first()).toBeVisible();
      await expect(warnings.warningFreshness.first()).toBeVisible();
      await expect(warnings.warningSafeAction.first()).toContainText('Story 5.9');
    }
  });

  liveAppHostTest('warnings dashboard markup stays metadata-only and avoids payload leakage', async ({ page }) => {
    const warnings = new ProjectDetailPage(page);
    await warnings.gotoWarnings();

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_WARNING_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }
  });

  liveAppHostTest('warnings dashboard passes WCAG 2.2 AA axe scan', async ({ page }, testInfo) => {
    const warnings = new ProjectDetailPage(page);
    await warnings.gotoWarnings();

    await expectNoA11yViolations(page, testInfo, { include: 'main' });
  });
});

base.describe('Projects warnings dashboard selector contract (no app required)', () => {
  base('supports dashboard drill-in filters and read-only queue actions', async ({ page }) => {
    await page.setContent(warningsDashboardFixture());
    const warnings = new ProjectDetailPage(page);
    const visibleWarningRows = warnings.warningRows.filter({ visible: true });

    await expect(warnings.warningsDashboard).toBeVisible();
    await expect(warnings.dashboardTiles).toHaveCount(3);
    await expect(warnings.warningsQueue.getByRole('columnheader', { name: 'Project' })).toBeVisible();
    await expect(warnings.warningsQueue.getByRole('columnheader', { name: 'Safe action' })).toBeVisible();
    await expect(visibleWarningRows).toHaveCount(2);
    await expect(warnings.warningSafeAction.first()).toContainText('Handled by Story 5.9');
    await expect(warnings.warningSafeAction.first().getByRole('button', { name: 'Handled by Story 5.9' })).toHaveAttribute(
      'aria-disabled',
      'true',
    );

    await warnings.warningStateFilter.selectOption('Stale');
    await expect(visibleWarningRows).toHaveCount(1);
    await expect(visibleWarningRows.first()).toContainText('memory-001');

    await warnings.warningReferenceTypeFilter.selectOption('file');
    await expect(visibleWarningRows).toHaveCount(0);
    await expect(warnings.warningEmpty).toBeVisible();
    await expect(warnings.warningEmpty).toContainText('Filter returned no results');

    await warnings.warningStateFilter.selectOption('');
    await expect(visibleWarningRows).toHaveCount(1);
    await expect(visibleWarningRows.first()).toContainText('file-001');
    await warnings.warningReferenceTypeFilter.selectOption('');
    await expect(visibleWarningRows).toHaveCount(2);

    await page.getByRole('button', { name: 'Stale: 1' }).click();
    await expect(warnings.warningStateFilter).toHaveValue('Stale');
    await expect(visibleWarningRows).toHaveCount(1);
    await expect(visibleWarningRows.first()).toContainText('memory-001');
    await warnings.warningStateFilter.focus();
    await expect(warnings.warningStateFilter).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(warnings.warningReasonFilter).toBeFocused();
  });

  base('keeps warning dashboard markup metadata-only and accessible', async ({ page }, testInfo) => {
    await page.setContent(warningsDashboardFixture());
    const warnings = new ProjectDetailPage(page);

    await expect(warnings.warningsDashboard.getByRole('heading', { name: 'Operational dashboard' })).toBeVisible();
    await expect(warnings.warningsQueue.getByRole('table', { name: 'Project warnings queue' })).toBeVisible();
    await expect(warnings.warningState.first()).toContainText('Stale');
    await expect(warnings.warningReason.first()).toContainText('Memory matched');
    await expect(warnings.warningReference.first()).toContainText('Memories');
    await expect(warnings.warningFreshness.first().locator('time')).toHaveAttribute('datetime', '2026-05-30T06:00:00.000Z');
    await expect(warnings.warningSafeAction.first().getByRole('link', { name: 'Open project' })).toHaveAttribute(
      'href',
      '/projects/project-001',
    );

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_WARNING_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }

    await expectNoA11yViolations(page, testInfo, { include: 'main' });
  });
});

function warningsDashboardFixture(): string {
  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Warnings dashboard selector contract</title>
    <style>
      body { color: #111; background: #fff; font-family: Arial, sans-serif; }
      label { display: inline-grid; gap: 4px; margin: 4px; }
      button, select, a {
        align-items: center;
        display: inline-flex;
        min-height: 24px;
        margin: 4px;
        padding: 4px 8px;
      }
      :focus-visible { outline: 3px solid #005a9e; outline-offset: 2px; }
    </style>
    <script>
      function applyFilters() {
        const state = document.querySelector('[data-testid="project-warning-filter-state"]').value;
        const reference = document.querySelector('[data-testid="project-warning-filter-reference-type"]').value;
        const rows = Array.from(document.querySelectorAll('[data-testid="project-warning-row"]'));
        let visible = 0;
        for (const row of rows) {
          const matchState = !state || row.dataset.state === state;
          const matchReference = !reference || row.dataset.reference === reference;
          const show = matchState && matchReference;
          row.hidden = !show;
          if (show) visible += 1;
        }
        document.querySelector('[data-testid="project-warning-empty"]').hidden = visible > 0;
      }
      function applyDashboardState(state) {
        document.querySelector('[data-testid="project-warning-filter-state"]').value = state;
        applyFilters();
      }
    </script>
  </head>
  <body>
    <main>
      <section data-testid="project-warnings-dashboard" aria-labelledby="project-warnings-dashboard-heading">
        <h1 id="project-warnings-dashboard-heading">Operational dashboard</h1>
        <button type="button" data-testid="project-dashboard-tile" aria-label="Warning projects: 2">2 Warning projects</button>
        <button type="button" data-testid="project-dashboard-tile" aria-label="Stale: 1" onclick="applyDashboardState('Stale')">1 Stale</button>
        <button type="button" data-testid="project-dashboard-tile" aria-label="Conflicts: 1" onclick="applyDashboardState('Conflict')">1 Conflicts</button>
      </section>
      <form aria-label="Project warning filters">
        <label data-testid="project-warning-filter-lifecycle">Lifecycle
          <select><option>All</option><option>Active</option><option>Archived</option></select>
        </label>
        <label>Warnings
          <select data-testid="project-warning-filter-state" onchange="applyFilters()">
            <option value="">All warning states</option>
            <option value="Stale">Stale</option>
            <option value="Conflict">Conflict</option>
          </select>
        </label>
        <label>Reason
          <select data-testid="project-warning-filter-reason">
            <option value="">All reasons</option>
            <option value="MemoryMatched">Memory matched</option>
            <option value="FileReferenceMatched">File reference matched</option>
          </select>
        </label>
        <label>Reference
          <select data-testid="project-warning-filter-reference-type" onchange="applyFilters()">
            <option value="">All references</option>
            <option value="memory">memory</option>
            <option value="file">file</option>
          </select>
        </label>
      </form>
      <section data-testid="project-warnings-queue" aria-labelledby="project-warnings-queue-heading">
        <h2 id="project-warnings-queue-heading">Warnings queue</h2>
        <table aria-label="Project warnings queue">
          <thead>
            <tr>
              <th scope="col">Project</th>
              <th scope="col">Lifecycle</th>
              <th scope="col">State</th>
              <th scope="col">Reason</th>
              <th scope="col">Reference</th>
              <th scope="col">Freshness</th>
              <th scope="col">Safe action</th>
            </tr>
          </thead>
          <tbody>
            <tr data-testid="project-warning-row" data-state="Stale" data-reference="memory" tabindex="0">
              <th scope="row"><a href="/projects/project-001">Inventory Project <code>project-001</code></a></th>
              <td>Active</td>
              <td data-testid="project-warning-state">Stale warning state</td>
              <td data-testid="project-warning-reason">Memory matched (MemoryMatched)</td>
              <td data-testid="project-warning-reference">memory <code>memory-001</code> <small>Memories</small></td>
              <td data-testid="project-warning-freshness"><time datetime="2026-05-30T06:00:00.000Z">2026-05-30 06:00:00Z</time> trusted</td>
              <td data-testid="project-warning-safe-action">
                <a href="/projects/project-001">Open project</a>
                <a href="/projects/project-001">References</a>
                <a href="/projects/project-001">Audit</a>
                <button type="button" aria-disabled="true">Handled by Story 5.9</button>
              </td>
            </tr>
            <tr data-testid="project-warning-row" data-state="Conflict" data-reference="file" tabindex="0">
              <th scope="row"><a href="/projects/project-002">Archived Project <code>project-002</code></a></th>
              <td>Archived</td>
              <td data-testid="project-warning-state">Conflict warning state</td>
              <td data-testid="project-warning-reason">File reference matched (FileReferenceMatched)</td>
              <td data-testid="project-warning-reference">file <code>file-001</code> <small>Projects</small></td>
              <td data-testid="project-warning-freshness"><time datetime="2026-05-30T06:30:00.000Z">2026-05-30 06:30:00Z</time> trusted</td>
              <td data-testid="project-warning-safe-action">
                <a href="/projects/project-002">Open project</a>
                <a href="/projects/project-002">References</a>
                <a href="/projects/project-002">Audit</a>
                <button type="button" aria-disabled="true">Handled by Story 5.9</button>
              </td>
            </tr>
          </tbody>
        </table>
      </section>
      <div data-testid="project-warning-empty" role="status" hidden>Filter returned no results</div>
    </main>
  </body>
</html>`;
}
