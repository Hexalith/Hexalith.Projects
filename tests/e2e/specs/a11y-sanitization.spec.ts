import { test, expect, type TestInfo } from '@playwright/test';
import { expectNoA11yViolations, sanitizeA11yMetadata } from '../support/helpers/a11y.js';

test.use({ screenshot: 'off', video: 'off', trace: 'off' });

test('axe failure attachments retain selectors and rule metadata without node HTML or protected values', async ({ page }) => {
  const marker = 'private-fixture-description';
  await page.setContent('<main><h1>Fixture</h1><img src="data:," data-private="' + marker + '"></main>');
  const attachments: Array<{ name: string; body: string }> = [];
  const info = { attach: async (name: string, options: { body: string }) => { attachments.push({ name, body: options.body }); } } as unknown as TestInfo;
  await expect(expectNoA11yViolations(page, info, { forbiddenText: [marker] })).rejects.toThrow();
  const receipt = attachments.find(value => value.name === 'axe-violations.json')!;
  expect(receipt).toBeDefined();
  expect(receipt.body).not.toContain(marker);
  expect(receipt.body).not.toContain('<img');
  for (const rule of JSON.parse(receipt.body) as Array<{ nodes: Array<Record<string, unknown>> }>) {
    for (const node of rule.nodes) expect(node).not.toHaveProperty('html');
  }
  expect(JSON.parse(receipt.body).some((rule: { id: string }) => rule.id === 'image-alt')).toBe(true);
});

test('protected values and arbitrary credentials are removed from nested axe selectors', () => {
  const value = sanitizeA11yMetadata({ target: [['[data-label="private-name"]', 'span']], token: 'eyJabcdefgh.abcdefgh.abcdefgh' }, ['private-name']);
  expect(JSON.stringify(value)).not.toContain('private-name');
  expect(JSON.stringify(value)).not.toContain('eyJabcdefgh');
});
