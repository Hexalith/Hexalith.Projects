import { test, expect } from '@playwright/test';
import {
  createProjectInput,
  createMinimalProjectInput,
  createForbiddenSetupInput,
} from '../support/factories/project-factory.js';
import { createDistinctTenantPair, createTenantContext } from '../support/factories/tenant-factory.js';
import { expectNoA11yViolations } from '../support/helpers/a11y.js';

/**
 * Framework self-check — RUNNABLE today, no Hexalith.Projects app required.
 *
 * Validates the harness wiring (factories, data-testid config, axe-core / WCAG 2.2 AA)
 * so the scaffold is verifiably green before the system under test exists. Uses the base
 * `@playwright/test` object (not merged-fixtures) so it has zero auth/app dependencies.
 */
test.describe('framework self-check (no app required)', () => {
  test('project factory yields a metadata-only create input with a required name', () => {
    const input = createProjectInput();
    expect(input.name).toBeTruthy();
    // FR-1: name is the only required field.
    expect(createMinimalProjectInput().name).toBeTruthy();
    expect(createMinimalProjectInput().description).toBeUndefined();
  });

  test('tenant factory yields distinct tenants for cross-tenant isolation tests', () => {
    const [a, b] = createDistinctTenantPair();
    expect(a.tenantId).not.toBe(b.tenantId);
    // Overrides work for deterministic scenarios.
    expect(createTenantContext({ tenantId: 'tenant-fixed' }).tenantId).toBe('tenant-fixed');
  });

  test('forbidden-setup factory carries content that must be rejected (FR-19 negatives)', () => {
    const forbidden = createForbiddenSetupInput();
    // Used ONLY to assert rejection — the aggregate must never persist/echo this.
    expect(forbidden.setup?.instructions ?? '').toContain('SECRET');
  });

  test('axe a11y helper passes on an accessible document (validates WCAG 2.2 AA wiring)', async ({ page }, testInfo) => {
    await page.setContent(`
      <!doctype html>
      <html lang="en">
        <head><meta charset="utf-8" /><title>Framework a11y self-check</title></head>
        <body>
          <main>
            <h1>Hexalith.Projects E2E framework</h1>
            <p data-testid="smoke-status">Accessibility tooling is wired correctly.</p>
          </main>
        </body>
      </html>
    `);
    // data-testid selector strategy (UX-DR28) — exercised here to prove config.
    await expect(page.getByTestId('smoke-status')).toBeVisible();
    await expectNoA11yViolations(page, testInfo);
  });
});
