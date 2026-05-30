import { test, expect } from '../support/merged-fixtures.js';
import { expectNoA11yViolations } from '../support/helpers/a11y.js';
import type { Page } from '@playwright/test';

/**
 * F6 — Operational console WCAG 2.2 AA hardening (UX-DR27).
 *
 * `test.fixme` until the Epic 5 console views exist. Scans each generated view with
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
  { name: 'resolution trace', path: (id: string) => `/projects/${id}/resolution`, anchorTestId: 'resolution-trace' },
  { name: 'audit timeline', path: (id: string) => `/projects/${id}/audit`, anchorTestId: 'audit-timeline' },
];

test.describe('Operational console — WCAG 2.2 AA', () => {
  for (const view of consoleViews) {
    test.fixme(`${view.name} has no WCAG 2.2 AA violations`, async ({ page, seededProject }, testInfo) => {
      await page.goto(view.path(seededProject.projectId));
      await view.prepare?.(page);
      // Wait on a deterministic anchor, not a timeout.
      await expect(page.getByTestId(view.anchorTestId)).toBeVisible();
      await expectNoA11yViolations(page, testInfo, { include: 'main' });
    });
  }
});
