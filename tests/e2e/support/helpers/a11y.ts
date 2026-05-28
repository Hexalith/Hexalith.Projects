import { AxeBuilder } from '@axe-core/playwright';
import { expect, type Page, type TestInfo } from '@playwright/test';

/**
 * WCAG 2.2 AA accessibility scanning (UX-DR27 / F6).
 *
 * Wraps axe-core. The console (UX-DR1..28) must be keyboard-accessible, status must
 * never be color-only, tables/timelines must be screen-reader-readable, and contrast
 * must pass. Violations are attached to the Playwright report for triage.
 */

// WCAG 2.0/2.1/2.2 A + AA rule sets.
const WCAG_AA_TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] as const;

export interface A11yScanOptions {
  /** Restrict the scan to a region (CSS selector), e.g. the main content landmark. */
  include?: string;
  /** Exclude known third-party/uncontrolled regions (use sparingly, document why). */
  exclude?: string[];
  /** Disable specific rules (use sparingly; prefer fixing the violation). */
  disableRules?: string[];
}

/**
 * Run an axe WCAG 2.2 AA scan and assert zero violations. On failure, the full
 * violations report is attached to the test for debugging.
 */
export async function expectNoA11yViolations(page: Page, testInfo: TestInfo, options: A11yScanOptions = {}): Promise<void> {
  let builder = new AxeBuilder({ page }).withTags([...WCAG_AA_TAGS]);

  if (options.include) builder = builder.include(options.include);
  for (const selector of options.exclude ?? []) builder = builder.exclude(selector);
  if (options.disableRules?.length) builder = builder.disableRules(options.disableRules);

  const results = await builder.analyze();

  if (results.violations.length > 0) {
    await testInfo.attach('axe-violations.json', {
      body: JSON.stringify(results.violations, null, 2),
      contentType: 'application/json',
    });
  }

  expect(
    results.violations,
    `Expected no WCAG 2.2 AA violations, found ${results.violations.length}: ${results.violations.map((v) => v.id).join(', ')}`,
  ).toEqual([]);
}
