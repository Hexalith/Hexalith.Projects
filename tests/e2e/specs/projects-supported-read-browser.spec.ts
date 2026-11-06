import { test, expect, request, type CDPSession, type Page } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import { expectNoA11yViolations } from '../support/helpers/a11y.js';
import { observeBrowserTraffic, inspectBrowserSession, assertServerOnlySession } from '../support/auth/browser-session.js';

// Registered only by the managed private actual-owner/OIDC launcher. Other live suites collect no
// skipped placeholder here. Native setup and all writes finish before this serial browser phase.
if (process.env.E2E_SUPPORTED_READ_APPHOST === '1') {
  test('supported OIDC SDK journeys preserve persisted domain state and separate inspection audits', async ({ page, context, browser }, testInfo) => {
    test.setTimeout(300_000);
    const retainJsonReceipt = async (name: string, value: unknown): Promise<void> => {
      const path = testInfo.outputPath(name);
      await writeFile(path, JSON.stringify(value, null, 2), { mode: 0o600 });
      await testInfo.attach(name, { path, contentType: 'application/json' });
    };
    // Accessibility receipts read rendered text, so each is refused if any protected fixture value appears.
    const protectedText = ['Synthetic project project-a', 'Synthetic descriptive metadata', 'Restricted legacy description'];
    const retainSafeReceipt = async (name: string, value: unknown): Promise<void> => {
      const body = JSON.stringify(value);
      for (const text of protectedText) expect(body.includes(text), `${name} retained protected text`).toBe(false);
      await retainJsonReceipt(name, value);
    };
    const interactiveRoles = new Set(['button', 'link', 'checkbox', 'textbox', 'searchbox', 'combobox', 'radio', 'switch', 'tab', 'menuitem', 'option', 'spinbutton', 'slider']);
    const accessibleText = (value: unknown): string => String(value ?? '').replace(/\s+/g, ' ').trim().slice(0, 160);
    // Chromium's computed accessibility tree, in depth-first order, is what a screen reader traverses.
    const readingOrder = async (cdp: CDPSession) => {
      const { nodes } = await cdp.send('Accessibility.getFullAXTree');
      const byId = new Map(nodes.map(node => [node.nodeId, node]));
      const lines: Array<{ depth: number; role: string; name: string; level?: number }> = [];
      const structural = new Set(['generic', 'none', 'InlineTextBox', 'LineBreak', 'RootWebArea']);
      const visit = (node: (typeof nodes)[number], depth: number, ancestorName: string): void => {
        const role = String(node.role?.value ?? '');
        const name = accessibleText(node.name?.value);
        const kept = !node.ignored && !structural.has(role) && !(role === 'StaticText' && (name === '' || ancestorName.includes(name)));
        if (kept) {
          const level = node.properties?.find(property => property.name === 'level')?.value.value;
          lines.push({ depth, role, name, ...(typeof level === 'number' ? { level } : {}) });
        }
        for (const childId of node.childIds ?? []) {
          const child = byId.get(childId);
          if (child) visit(child, kept ? depth + 1 : depth, kept && name ? name : ancestorName);
        }
      };
      const root = nodes.find(node => node.parentId === undefined);
      if (root) visit(root, 0, '');
      return lines;
    };
    const focusedAccessibleNode = async (cdp: CDPSession) => {
      const { nodes } = await cdp.send('Accessibility.getFullAXTree');
      const node = nodes.find(candidate => candidate.properties?.some(property => property.name === 'focused' && property.value.value === true));
      return { role: String(node?.role?.value ?? ''), name: accessibleText(node?.name?.value) };
    };
    // Full forward Tab traversal from the document start; stops when focus leaves the page or repeats.
    const keyboardOrder = async (target: Page, cdp: CDPSession, surface: string) => {
      await target.evaluate(() => {
        window.scrollTo(0, 0);
        // A removed focus probe outside the Blazor-rendered body resets the sequential navigation start.
        const probe = document.createElement('span');
        probe.tabIndex = -1;
        document.documentElement.insertBefore(probe, document.documentElement.firstChild);
        probe.focus();
        probe.remove();
        (window as unknown as { __focusProbe: WeakSet<Element> }).__focusProbe = new WeakSet<Element>();
      });
      const stops = [];
      for (let index = 0; index < 150; index++) {
        await target.keyboard.press('Tab');
        const stop = await target.evaluate(() => {
          let active: Element | null = document.activeElement;
          while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
          if (!active || active === document.body || active === document.documentElement) return null;
          const seen = (window as unknown as { __focusProbe: WeakSet<Element> }).__focusProbe;
          if (seen.has(active)) return null;
          seen.add(active);
          const root = active.getRootNode();
          const host = root instanceof ShadowRoot ? root.host : active;
          const rect = active.getBoundingClientRect();
          return {
            tag: active.tagName.toLowerCase(),
            hostTag: host.tagName.toLowerCase(),
            testId: host.closest('[data-testid]')?.getAttribute('data-testid') ?? null,
            focusVisible: active.matches(':focus-visible') || host.matches(':focus-visible'),
            withinViewport: rect.width > 0 && rect.height > 0 && rect.bottom > 0 && rect.right > 0 && rect.top < window.innerHeight && rect.left < window.innerWidth,
          };
        });
        if (stop === null) break;
        stops.push({ index, ...stop, ...await focusedAccessibleNode(cdp) });
      }
      expect.soft(stops.length, `${surface}: keyboard traversal reached no control`).toBeGreaterThan(0);
      for (const stop of stops) {
        expect.soft(stop.withinViewport, `${surface}: focus stop ${stop.index} (${stop.role}) is outside the viewport`).toBe(true);
        expect.soft(stop.focusVisible, `${surface}: focus stop ${stop.index} (${stop.role}) has no visible focus state`).toBe(true);
        if (interactiveRoles.has(stop.role)) expect.soft(stop.name, `${surface}: focus stop ${stop.index} (${stop.role}) has no accessible name`).not.toBe('');
      }
      return stops;
    };
    const noHorizontalScroll = (target: Page) => target.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1);
    // Every surface opens fresh in Safe Metadata mode, independent of earlier journey state.
    const openInventorySurface = async (target: Page): Promise<void> => {
      await target.goto('/inspect/projects');
      await expect(target.getByTestId('project-inventory-row').filter({ hasText: 'project-a' })).toBeVisible();
      await expect(target.getByTestId('project-inventory-page-snapshot')).toContainText('State:');
      await expect(target.getByTestId('project-inventory-inspection')).not.toBeChecked();
    };
    const openDetailSurface = async (target: Page): Promise<void> => {
      await target.goto('/inspect/projects/project-a');
      await expect(target.getByTestId('project-open-snapshot')).toContainText('Project version:');
      const inspection = target.getByRole('checkbox', { name: /Descriptive inspection/ });
      if (await inspection.isChecked()) await inspection.click();
      await expect(inspection).not.toBeChecked();
      await target.getByRole('button', { name: 'Context observation', exact: true }).click();
      await expect(target.getByTestId('project-context-snapshot')).toContainText('State:');
      await target.getByRole('button', { name: 'Conversation-start observation', exact: true }).click();
      await expect(target.getByTestId('project-start-snapshot')).toContainText('State:');
      await expect(target.getByTestId('project-detail-inspector')).not.toContainText('Synthetic project project-a');
    };
    const openSuggestionsSurface = async (target: Page): Promise<void> => {
      await openDetailSurface(target);
      await target.getByRole('button', { name: 'Project suggestions', exact: true }).click();
      const attachments = target.getByRole('checkbox', { name: 'Use attachment inputs' });
      await attachments.click();
      await expect(attachments).toBeChecked();
      await target.getByRole('textbox', { name: /File IDs/ }).fill('unsupported-file');
      await target.getByTestId('project-resolution-trace-run').click();
      await expect(target.getByTestId('project-resolution-trace-outcome')).toContainText('coverage incomplete');
      await target.getByRole('button', { name: 'Evaluation and coverage', exact: true }).click();
      await expect(target.getByTestId('project-resolution-trace-coverage')).toContainText('file');
    };
    const surfaces: Array<[string, (target: Page) => Promise<void>]> = [
      ['inventory', openInventorySurface],
      ['detail', openDetailSurface],
      ['suggestions', openSuggestionsSurface],
    ];
    const fixture = await request.newContext({ baseURL: process.env.FIXTURE_API_URL });
    type Observation = { domain: Record<string, string>; audit: Record<string, string> };
    const observe = async (suffix = ''): Promise<Observation> => {
      const response = await fixture.get(`/api/v1/live-fixtures/graphs/${process.env.E2E_RUN_ID}${suffix}/persisted-observation`);
      expect(response.status()).toBe(200);
      const value = await response.json() as Observation;
      expect(Object.keys(value.domain).length).toBeGreaterThan(0);
      return value;
    };
    const expectControlFocus = async (testId: string): Promise<void> => {
      await expect.poll(() => page.getByTestId(testId).evaluate(element => {
        let active: Element | null = document.activeElement;
        while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
        return active === element || !!active && (element.contains(active) || !!element.shadowRoot?.contains(active));
      })).toBe(true);
    };
    const traffic = observeBrowserTraffic(page);
    const telemetry: string[] = [];
    page.on('console', message => telemetry.push(message.text()));
    try {
      const before = await observe();
      const foreignBefore = await observe('-foreign');
      await page.goto('/inspect/projects');
      const row = page.getByTestId('project-inventory-row').filter({ hasText: 'project-a' });
      await expect(row).toBeVisible();
      await expect(row).not.toContainText('Synthetic project project-a');
      await expect(row.getByTestId('project-inventory-row-snapshot')).toContainText('Project version:');
      await expect(page.getByTestId('project-inventory-page-snapshot')).toContainText('State: Complete');
      await page.getByTestId('project-inventory-filter-archived').click();
      await expect(page.getByTestId('project-inventory-empty')).toBeVisible();
      await expect(page.getByTestId('project-inventory-page-snapshot')).toContainText('State: Complete');
      await page.getByTestId('project-inventory-filter-active').click();
      await page.getByRole('checkbox', { name: /^Quarantine inventory/ }).click();
      await expect(page.getByRole('checkbox', { name: /^Quarantine inventory/ })).toBeChecked();
      await expect(page.getByTestId('project-inventory-inspection')).toBeChecked();
      await expect(page.getByTestId('project-inventory-inspection')).toBeDisabled();
      const degraded = page.getByTestId('project-inventory-row').filter({ hasText: 'legacy-a' });
      await expect(degraded.getByTestId('project-inventory-row-snapshot')).toContainText('State: Unavailable');
      await expect(degraded).toContainText('Legacy project legacy-a');
      const quarantined = await observe();
      expect(quarantined.domain).toEqual(before.domain);
      expect(Object.keys(quarantined.audit).length).toBeGreaterThan(Object.keys(before.audit).length);
      await expect(page.getByTestId('project-inventory-page-snapshot')).toContainText('State: Complete');
      await page.getByRole('checkbox', { name: /^Quarantine inventory/ }).click();
      await expect(page.getByRole('checkbox', { name: /^Quarantine inventory/ })).not.toBeChecked();
      await expect(page.getByTestId('project-inventory-inspection')).not.toBeChecked();
      await expect(page.getByTestId('project-inventory-inspection')).toBeEnabled();
      await row.getByTestId('project-inventory-row-link').click();
      await expect(page.getByTestId('project-open-snapshot')).toContainText('State: Complete');
      const primaryProjectVersion = /Project version:\s*(\d+)/.exec(await page.getByTestId('project-open-snapshot').innerText())?.[1];
      expect(primaryProjectVersion).toMatch(/^[1-9]\d*$/);
      const observeConcurrentPrimary = async (phase: string) => {
        const snapshot = page.getByTestId('project-open-snapshot');
        await expect(page.getByRole('heading', { name: 'Project project-a', exact: true })).toBeVisible();
        await expect(page.getByTestId('project-detail-inspector')).toContainText(`Tenant: ${process.env.TEST_TENANT_ID}`);
        await expect(snapshot).toContainText(`Project version: ${primaryProjectVersion}`);
        await expect(snapshot).toContainText('Project — Included: True; Freshness: Current; Reason: Current');
        await expect(snapshot).toContainText('Authorization — Included: True; Freshness: Current; Reason: Current');
        const text = await snapshot.innerText();
        const state = /State:\s*(Complete|Partial|Unavailable|Denied)/.exec(text)?.[1];
        expect(['Complete', 'Unavailable']).toContain(state);
        const folderFreshness = /Folder — Included: (?:True|False); Freshness: (Current|Stale|Unavailable)/.exec(text)?.[1];
        if (state === 'Complete') {
          expect(text).toContain('Folder — Included: True; Freshness: Current; Reason: Current');
        } else {
          expect(text).toMatch(/Folder — Included: False; Freshness: (Stale|Unavailable); Reason: FolderUnavailable/);
          expect(text).toContain('Recovery guidance: Retry');
        }
        return { phase, responseState: state, projectVersion: primaryProjectVersion, folderFreshness, currentProject: true, currentAuthorization: true };
      };
      await page.getByRole('button', { name: 'Context observation', exact: true }).click();
      await expect(page.getByTestId('project-context-snapshot')).toContainText('State:');
      await page.getByRole('button', { name: 'Conversation-start observation', exact: true }).click();
      await expect(page.getByTestId('project-start-snapshot')).toContainText('State:');
      await expect(page.getByTestId('project-detail-inspector')).not.toContainText('Synthetic project project-a');
      const beforeKeyboardRead = /As of:\s*([0-9TZ:+.\-]+)/.exec(await page.getByTestId('project-open-snapshot').innerText())![1];
      await page.getByTestId('project-detail-reread').focus();
      await page.getByTestId('project-detail-reread').press('Enter');
      await expect(page.getByTestId('project-open-snapshot')).not.toContainText(beforeKeyboardRead);
      await expect(page.getByTestId('project-open-snapshot')).toContainText('Project version:');
      await expectControlFocus('project-detail-reread');
      const peer = await browser.newContext({ baseURL: process.env.BASE_URL, ignoreHTTPSErrors: true, storageState: { cookies: [], origins: [] } });
      try {
        const peerPage = await peer.newPage();
        await peerPage.goto('/inspect/projects');
        await peerPage.locator('#username').fill(process.env.TEST_USER_USERNAME!);
        await peerPage.locator('#password').fill(process.env.TEST_USER_PASSWORD!);
        await peerPage.locator('#kc-login').click();
        await peerPage.waitForURL(url => url.origin === new URL(process.env.BASE_URL!).origin);
        await expect(peerPage.getByTestId('project-inventory-row').filter({ hasText: 'project-a' })).toBeVisible();
        const sessions = await Promise.all([context.cookies(process.env.BASE_URL!), peer.cookies(process.env.BASE_URL!)]);
        const ticket = (cookies: typeof sessions[number]) => JSON.stringify(cookies.filter(cookie => /FrontComposer/i.test(cookie.name)).map(cookie => [cookie.name, cookie.value]).sort());
        expect(ticket(sessions[0]) !== ticket(sessions[1])).toBe(true);
        await Promise.all([page.goto('/inspect/projects'), peerPage.goto('/inspect/projects')]);
        await expect(page.getByTestId('project-inventory-row').filter({ hasText: 'project-a' })).toBeVisible();
        await expect(peerPage.getByTestId('project-inventory-row').filter({ hasText: 'project-a' })).toBeVisible();
      } finally { await peer.close(); }
      await page.goto('/inspect/projects/project-a');
      await expect(page.getByTestId('project-open-snapshot')).toContainText('Project version:');
      const after = await observe();
      expect(after.domain).toEqual(before.domain);
      await page.getByRole('checkbox', { name: /Descriptive inspection/ }).focus();
      await page.getByRole('checkbox', { name: /Descriptive inspection/ }).press('Space');
      await expect(page.getByRole('checkbox', { name: /Descriptive inspection/ })).toBeChecked();
      await expect(page.getByTestId('project-detail-inspector')).toContainText('Synthetic project project-a');
      await expectControlFocus('project-detail-inspection');
      const inspected = await observe();
      expect(inspected.domain).toEqual(before.domain);
      expect(Object.keys(inspected.audit).length).toBeGreaterThan(Object.keys(after.audit).length);
      const concurrentObservations = [];
      const foreignContext = await browser.newContext({ baseURL: process.env.BASE_URL, ignoreHTTPSErrors: true, storageState: { cookies: [], origins: [] } });
      try {
        const foreignPage = await foreignContext.newPage();
        await foreignPage.goto('/inspect/projects/project-a');
        await foreignPage.locator('#username').fill(process.env.E2E_FOREIGN_USERNAME!);
        await foreignPage.locator('#password').fill(process.env.E2E_FOREIGN_PASSWORD!);
        await foreignPage.locator('#kc-login').click();
        await foreignPage.waitForURL(url => url.origin === new URL(process.env.BASE_URL!).origin);
        await expect(foreignPage.getByTestId('project-feedback-fail-closed')).toContainText('safe_denial');
        await expect(foreignPage.getByTestId('project-navigation-project').filter({ hasText: 'foreign-project-a' })).toBeVisible();
        await Promise.all([page.goto('/inspect/projects/project-a'), foreignPage.goto('/inspect/projects/project-a')]);
        concurrentObservations.push(await observeConcurrentPrimary('concurrent-foreign-navigation'));
        const denied = foreignPage.getByTestId('project-feedback-fail-closed');
        await expect(denied).toContainText('safe_denial');
        const foreignAbsence = await denied.innerText();
        const beforeConcurrentRead = /As of:\s*([0-9TZ:+.\-]+)/.exec(await page.getByTestId('project-open-snapshot').innerText())![1];
        await Promise.all([page.getByTestId('project-detail-reread').click(), foreignPage.goto('/inspect/projects/missing-browser-target')]);
        await expect(page.getByTestId('project-open-snapshot')).not.toContainText(beforeConcurrentRead);
        await expect(foreignPage.getByTestId('project-feedback-fail-closed')).toHaveText(foreignAbsence);
        await expect(foreignPage.locator('body')).not.toContainText('Synthetic project project-a');
        concurrentObservations.push(await observeConcurrentPrimary('concurrent-foreign-absence'));
        expect((await observe('-foreign')).domain).toEqual(foreignBefore.domain);
        expect((await observe()).domain).toEqual(before.domain);
      } finally {
        await foreignContext.close();
      }
      const beforeClosedRead = /As of:\s*([0-9TZ:+.\-]+)/.exec(await page.getByTestId('project-open-snapshot').innerText())![1];
      await page.getByTestId('project-detail-reread').click();
      await expect(page.getByTestId('project-open-snapshot')).not.toContainText(beforeClosedRead);
      concurrentObservations.push(await observeConcurrentPrimary('after-foreign-close-supported-reread'));
      await retainJsonReceipt('supported-read-concurrent-observations.json', concurrentObservations);
      await page.getByRole('button', { name: 'Project suggestions', exact: true }).click();
      await page.getByRole('checkbox', { name: 'Use attachment inputs' }).click();
      await expect(page.getByRole('checkbox', { name: 'Use attachment inputs' })).toBeChecked();
      await page.getByRole('textbox', { name: /File IDs/ }).fill('unsupported-file');
      await page.getByTestId('project-resolution-trace-run').click();
      await expect(page.getByTestId('project-resolution-trace-outcome')).toContainText('coverage incomplete');
      await page.getByRole('button', { name: 'Evaluation and coverage', exact: true }).click();
      await expect(page.getByTestId('project-resolution-trace-coverage')).toContainText('file');
      await page.setViewportSize({ width: 320, height: 900 });
      await expectNoA11yViolations(page, testInfo, { include: '[role="main"]', receiptName: 'supported-read-320px.json', forbiddenText: protectedText });
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBe(true);
      await page.setViewportSize({ width: 1280, height: 900 });
      await page.evaluate(() => { document.documentElement.style.zoom = '2'; });
      await page.keyboard.press('Tab');
      expect(await page.evaluate(() => document.activeElement !== document.body)).toBe(true);
      await expectNoA11yViolations(page, testInfo, { include: '[role="main"]', receiptName: 'supported-read-css-zoom-200.json', forbiddenText: protectedText });
      // Per-surface journeys at desktop width and 320px reflow, with screen-reader order and keyboard order.
      const pageAccessibility = await context.newCDPSession(page);
      await pageAccessibility.send('Accessibility.enable');
      for (const [surface, open] of surfaces) {
        await page.setViewportSize({ width: 1280, height: 900 });
        await open(page);
        await expectNoA11yViolations(page, testInfo, { include: '[role="main"]', receiptName: `supported-read-a11y-${surface}-1280.json`, forbiddenText: protectedText, soft: true });
        await retainSafeReceipt(`supported-read-reading-order-${surface}-1280.json`, await readingOrder(pageAccessibility));
        await retainSafeReceipt(`supported-read-keyboard-${surface}-1280.json`, await keyboardOrder(page, pageAccessibility, `${surface} 1280`));
        await page.setViewportSize({ width: 320, height: 900 });
        await expectNoA11yViolations(page, testInfo, { include: '[role="main"]', receiptName: `supported-read-a11y-${surface}-320px.json`, forbiddenText: protectedText, soft: true });
        expect.soft(await noHorizontalScroll(page), `${surface}: 320px reflow scrolls horizontally`).toBe(true);
      }
      await page.setViewportSize({ width: 1280, height: 900 });
      // Chromium implements browser zoom as a device scale factor: a 1280x900 window at 200% lays out
      // 640x450 CSS pixels at devicePixelRatio 2, unlike CSS zoom, which axe cannot measure reliably.
      const zoomContext = await browser.newContext({ baseURL: process.env.BASE_URL, ignoreHTTPSErrors: true, storageState: await context.storageState(),
        viewport: { width: 640, height: 450 }, deviceScaleFactor: 2 });
      try {
        const zoomPage = await zoomContext.newPage();
        zoomPage.on('console', message => telemetry.push(message.text()));
        const zoomAccessibility = await zoomContext.newCDPSession(zoomPage);
        await zoomAccessibility.send('Accessibility.enable');
        for (const [surface, open] of surfaces) {
          await open(zoomPage);
          expect(await zoomPage.evaluate(() => ({ width: window.innerWidth, ratio: window.devicePixelRatio }))).toEqual({ width: 640, ratio: 2 });
          await expectNoA11yViolations(zoomPage, testInfo, { include: '[role="main"]', receiptName: `supported-read-a11y-${surface}-zoom-200.json`, forbiddenText: protectedText, soft: true });
          expect.soft(await noHorizontalScroll(zoomPage), `${surface}: 200% zoom scrolls horizontally`).toBe(true);
          await retainSafeReceipt(`supported-read-reading-order-${surface}-zoom-200.json`, await readingOrder(zoomAccessibility));
          await retainSafeReceipt(`supported-read-keyboard-${surface}-zoom-200.json`, await keyboardOrder(zoomPage, zoomAccessibility, `${surface} 200% zoom`));
        }
      } finally {
        await zoomContext.close();
      }
      expect((await observe()).domain).toEqual(before.domain);
      const browserTraffic = await traffic.settle();
      expect(browserTraffic.authorizationHeaders).toBe(0);
      expect(browserTraffic.tokenBearingUrls).toBe(0);
      assertServerOnlySession(await inspectBrowserSession(page, context, process.env.BASE_URL!), 'supported-sdk-browser');
      expect(telemetry.join('\n')).not.toContain('Synthetic project project-a');
      await retainJsonReceipt('supported-read-persisted-boundary.json', { before, after, quarantined, inspected, foreignBefore, foreignAfter: await observe('-foreign') });
    } finally { await fixture.dispose(); }
  });
}
