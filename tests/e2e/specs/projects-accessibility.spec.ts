import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { expectNoA11yViolations } from '../support/helpers/a11y.js';
import { test as base, type Page } from '@playwright/test';

/**
 * F6 — Operational console WCAG 2.2 AA hardening (UX-DR27).
 *
 * Live-gated until `E2E_LIVE_APPHOST=1`. Scans each generated view with
 * axe-core (WCAG 2.0/2.1/2.2 A + AA). Status must never be color-only, tables/timelines
 * must be screen-reader-readable, focus must be visible, and contrast must pass.
 */
interface ConsoleView {
  name: string;
  path: (id: string) => string;
  anchorTestId: string;
  prepare?: (page: Page) => Promise<void>;
}

const consoleViews: ConsoleView[] = [
  { name: 'project inventory', path: (_id: string) => '/projects', anchorTestId: 'project-inventory-grid' },
  { name: 'project detail', path: (id: string) => `/projects/${id}`, anchorTestId: 'project-diagnostic-header' },
  {
    name: 'reference health',
    path: (id: string) => `/projects/${id}`,
    anchorTestId: 'project-reference-health-matrix',
    prepare: async (page: Page) => {
      await page.getByTestId('project-detail-tab-references').click();
    },
  },
  {
    name: 'resolution trace',
    path: (id: string) => `/projects/${id}`,
    anchorTestId: 'project-resolution-trace-workbench',
    prepare: async (page: Page) => {
      await page.getByTestId('project-detail-tab-resolution').click();
    },
  },
  { name: 'audit timeline', path: (id: string) => `/projects/${id}/audit`, anchorTestId: 'audit-timeline' },
];

test.describe('Operational console — WCAG 2.2 AA', () => {
  for (const view of consoleViews) {
    liveAppHostTest(`${view.name} has no WCAG 2.2 AA violations`, async ({ page, seededProject }, testInfo) => {
      await page.goto(view.path(seededProject.projectId));
      await view.prepare?.(page);
      // Wait on a deterministic anchor, not a timeout.
      await expect(page.getByTestId(view.anchorTestId)).toBeVisible();
      await expectNoA11yViolations(page, testInfo, { include: 'main' });
    });
  }
});

const responsiveViewports = [
  { name: 'mobile', width: 360, height: 780 },
  { name: 'tablet', width: 900, height: 900 },
  { name: 'desktop', width: 1280, height: 900 },
  { name: 'wide', width: 1440, height: 900 },
];

base.describe('Operational console WCAG and responsive fixture contracts (no app required)', () => {
  for (const viewport of responsiveViewports) {
    base(`preserves accessible critical metadata at ${viewport.name}`, async ({ page }, testInfo) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.setContent(operationalConsoleFixture());

      await expect(page.getByRole('main')).toBeVisible();
      await expect(page.getByTestId('project-inventory-grid')).toBeVisible();
      await expect(page.getByTestId('project-diagnostic-header')).toBeVisible();
      await expect(page.getByTestId('project-reference-health-matrix')).toBeVisible();
      await expect(page.getByTestId('project-resolution-trace-workbench')).toBeVisible();
      await expect(page.getByTestId('audit-timeline')).toBeVisible();
      await expect(page.getByTestId('safe-diagnostic-export')).toBeVisible();
      await expect(page.getByTestId('maintenance-action-panel')).toBeVisible();
      await expect(page.getByTestId('project-copy-tenant-scope')).toHaveAttribute('aria-label', /server-derived tenant/i);
      await expect(page.getByTestId('project-copy-project-id')).toHaveAttribute('aria-label', /project-001/);
      await expect(page.getByTestId('maintenance-action-warning')).toContainText('Dry-run required');
      await expect(page.getByTestId('project-warning-state')).toContainText('Stale');
      await expect(page.getByTestId('project-reference-reason')).toContainText('MemoryMatched');

      const hasHorizontalOverflow = await page.evaluate(() => {
        const root = document.scrollingElement ?? document.documentElement;
        return root.scrollWidth > root.clientWidth + 1;
      });
      expect(hasHorizontalOverflow).toBe(false);

      await expectNoA11yViolations(page, testInfo, { include: 'main' });
    });
  }

  base('supports keyboard-only traversal through filters, tabs, audit, and maintenance controls', async ({ page }) => {
    await page.setContent(operationalConsoleFixture());

    await page.getByTestId('project-inventory-filter-lifecycle').focus();
    await expect(page.getByTestId('project-inventory-filter-lifecycle')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('project-warning-filter-state')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('project-copy-tenant-scope')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('project-copy-project-id')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('project-detail-tab-references')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('project-resolution-trace-mode')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('project-resolution-trace-conversation-id')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('project-resolution-trace-run')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('audit-timeline-copy')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('safe-diagnostic-export-copy')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('maintenance-action-select')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('maintenance-action-dry-run-run')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('maintenance-action-confirm')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('maintenance-action-submit')).toBeFocused();
  });
});

function operationalConsoleFixture(): string {
  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Projects operational console accessibility contract</title>
    <style>
      * { box-sizing: border-box; }
      body { margin: 0; color: #111; background: #fff; font-family: Arial, sans-serif; line-height: 1.45; }
      main { width: min(100%, 72rem); margin: 0 auto; padding: 1rem; }
      section { margin-block: 1rem; }
      table { width: 100%; border-collapse: collapse; table-layout: fixed; }
      th, td { border: 1px solid #777; padding: .5rem; vertical-align: top; overflow-wrap: anywhere; }
      code { overflow-wrap: anywhere; }
      form, .actions { display: grid; gap: .75rem; }
      label { display: grid; gap: .25rem; }
      button, select, input { min-height: 2.5rem; }
      :focus-visible { outline: 3px solid #005a9e; outline-offset: 2px; }
      @media (max-width: 767px) {
        main { padding: .75rem; }
        table, thead, tbody, tr, th, td { display: block; width: 100%; }
      }
    </style>
  </head>
  <body>
    <main>
      <h1>Projects operations</h1>
      <form aria-label="Inventory filters">
        <label>Lifecycle
          <select data-testid="project-inventory-filter-lifecycle">
            <option>All</option>
            <option>Active</option>
          </select>
        </label>
        <label>Warning state
          <select data-testid="project-warning-filter-state">
            <option>All</option>
            <option>Stale</option>
          </select>
        </label>
      </form>
      <section data-testid="project-inventory-grid" aria-labelledby="inventory-heading">
        <h2 id="inventory-heading">Inventory</h2>
        <table aria-label="Project inventory">
          <thead><tr><th scope="col">Project</th><th scope="col">Lifecycle</th><th scope="col">Warning</th></tr></thead>
          <tbody>
            <tr data-testid="project-inventory-row">
              <th scope="row"><code>project-001-with-a-long-identifier-that-wraps</code></th>
              <td><span data-testid="project-lifecycle-badge" aria-label="Lifecycle Active">Active</span></td>
              <td data-testid="project-warning-state">Stale warning state</td>
            </tr>
          </tbody>
        </table>
      </section>
      <header data-testid="project-diagnostic-header" aria-labelledby="detail-heading">
        <h2 id="detail-heading">Inventory Project</h2>
        <button data-testid="project-copy-tenant-scope" type="button" aria-label="Copy server-derived tenant scope">server-derived tenant</button>
        <button data-testid="project-copy-project-id" type="button" aria-label="Copy project id project-001-with-a-long-identifier-that-wraps">
          <code>project-001-with-a-long-identifier-that-wraps</code>
        </button>
      </header>
      <button data-testid="project-detail-tab-references" type="button" aria-controls="references">References</button>
      <section id="references" data-testid="project-reference-health-matrix" aria-labelledby="references-heading">
        <h2 id="references-heading">Reference health</h2>
        <table aria-label="Project reference health">
          <thead><tr><th scope="col">Reference</th><th scope="col">State</th><th scope="col">Reason</th><th scope="col">Safe action</th></tr></thead>
          <tbody>
            <tr data-testid="project-reference-health-row">
              <th data-testid="project-reference-kind" scope="row">memory <code>memory-001</code></th>
              <td data-testid="project-reference-state">Stale</td>
              <td data-testid="project-reference-reason">MemoryMatched</td>
              <td data-testid="project-reference-safe-actions">Run dry-run before maintenance.</td>
            </tr>
          </tbody>
        </table>
      </section>
      <section data-testid="project-detail-section-resolution" aria-labelledby="resolution-heading">
        <h2 id="resolution-heading">Resolution trace</h2>
        <form data-testid="project-resolution-trace-workbench" aria-label="Resolution trace workbench">
          <label>Mode
            <select data-testid="project-resolution-trace-mode"><option>Conversation</option></select>
          </label>
          <label>Conversation id
            <input data-testid="project-resolution-trace-conversation-id" value="conversation-001" />
          </label>
          <button data-testid="project-resolution-trace-run" type="button">Run trace</button>
        </form>
      </section>
      <section data-testid="audit-timeline" aria-labelledby="audit-heading">
        <h2 id="audit-heading">Audit timeline</h2>
        <ol>
          <li data-testid="audit-timeline-entry">
            <span data-testid="audit-timeline-operation">archive-preview</span>
            <code data-testid="audit-timeline-correlation-id">corr-001-long-value-that-wraps</code>
          </li>
        </ol>
        <button data-testid="audit-timeline-copy" type="button" aria-label="Copy audit identifier audit-001">Copy audit id</button>
      </section>
      <section data-testid="safe-diagnostic-export" aria-labelledby="export-heading">
        <h2 id="export-heading">Safe diagnostic export</h2>
        <p data-testid="safe-diagnostic-export-guarantee">Payloads, raw problem details, command bodies, and sibling denial details are excluded.</p>
        <button data-testid="safe-diagnostic-export-copy" type="button">Copy safe export</button>
      </section>
      <section data-testid="maintenance-action-panel" aria-labelledby="maintenance-heading">
        <h2 id="maintenance-heading">Maintenance action</h2>
        <form class="actions" aria-label="Maintenance action form">
          <label>Action
            <select data-testid="maintenance-action-select"><option>Archive</option></select>
          </label>
          <p data-testid="maintenance-action-warning">Dry-run required before submit. Current state Active, proposed state Archived.</p>
          <button data-testid="maintenance-action-dry-run-run" type="button">Run dry-run</button>
          <label>
            <input data-testid="maintenance-action-confirm" type="checkbox" />
            I understand the audit consequence.
          </label>
          <button data-testid="maintenance-action-submit" type="button">Submit maintenance action</button>
        </form>
      </section>
    </main>
  </body>
</html>`;
}
