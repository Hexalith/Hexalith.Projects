import { AxeBuilder } from '@axe-core/playwright';
import { writeFile } from 'node:fs/promises';
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
  /** Optional metadata-only receipt, excluding DOM content and protected values. */
  receiptName?: string;
  /** Restrict the scan to a region (CSS selector), e.g. the main content landmark. */
  include?: string;
  /** Exclude known third-party/uncontrolled regions (use sparingly, document why). */
  exclude?: string[];
  /** Disable specific rules (use sparingly; prefer fixing the violation). */
  disableRules?: string[];
  /** Text that must never appear in the retained receipt (protected fixture values). */
  forbiddenText?: string[];
}

// Only numeric/closed-vocabulary check data is retained; axe messages and HTML snippets can carry page text.
const RETAINED_CHECK_DATA = ['fgColor', 'bgColor', 'contrastRatio', 'expectedContrastRatio', 'fontSize', 'fontWeight', 'messageKey'] as const;

type AxeNode = { target: unknown; any: AxeCheck[]; all: AxeCheck[]; none: AxeCheck[] };
type AxeCheck = { id: string; data?: unknown };

function retainedNode(node: AxeNode): { target: unknown; checks: Array<{ id: string; data: Record<string, unknown> }> } {
  const checks = [...node.any, ...node.all, ...node.none]
    .filter((check) => check.data !== null && typeof check.data === 'object')
    .map((check) => ({
      id: check.id,
      data: Object.fromEntries(RETAINED_CHECK_DATA
        .filter((key) => key in (check.data as Record<string, unknown>))
        .map((key) => [key, (check.data as Record<string, unknown>)[key]])),
    }))
    .filter((check) => Object.keys(check.data).length > 0);
  return { target: node.target, checks };
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
  if (options.receiptName) {
    const receipt = testInfo.outputPath(options.receiptName);
    const body = JSON.stringify({
        testEngine: results.testEngine,
        timestamp: results.timestamp,
        tags: WCAG_AA_TAGS,
        passedRuleCount: results.passes.length,
        incompleteRuleIds: results.incomplete.map(result => result.id),
        // Undetermined nodes keep selectors and numeric contrast data only, so each can be evaluated.
        incomplete: results.incomplete.map(result => ({ id: result.id, impact: result.impact, nodes: result.nodes.map(retainedNode) })),
        // Decided contrast targets let an undetermined node be matched to a definite verdict elsewhere.
        colorContrastPassTargets: results.passes.filter(result => result.id === 'color-contrast').flatMap(result => result.nodes.map(node => node.target)),
        violations: results.violations.map(result => ({ id: result.id, impact: result.impact })),
      }, null, 2);
    for (const text of options.forbiddenText ?? []) {
      expect(body.includes(text), `Accessibility receipt ${options.receiptName} retained protected text`).toBe(false);
    }
    await writeFile(receipt, body, { mode: 0o600 });
    await testInfo.attach(options.receiptName, { path: receipt, contentType: 'application/json' });
  }

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
