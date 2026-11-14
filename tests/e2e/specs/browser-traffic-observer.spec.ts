import type { Page, Request, Route } from '@playwright/test';
import { expect, test } from '@playwright/test';

import { observeBrowserTraffic } from '../support/auth/browser-session.js';

test.describe('browser traffic header observer contracts (no app required)', () => {
  test('counts actual complete Authorization headers and detaches its request listener', async () => {
    const page = fakePage();
    const traffic = await observeBrowserTraffic(page.value, 'https://api.invalid', 10);
    page.emit(Promise.resolve({ Authorization: 'private-value' }));
    expect(await traffic.settle()).toEqual({ apiOriginRequests: 0, authorizationHeaders: 1, tokenBearingUrls: 0 });
    expect(page.detached()).toBe(true);
    expect(page.continued()).toEqual([[]]);
  });

  test('fails closed when a complete headers observation rejects without exposing its error', async () => {
    const page = fakePage();
    const traffic = await observeBrowserTraffic(page.value, undefined, 10);
    page.emit(Promise.reject(new Error('private-header-details')));
    await expect(traffic.settle()).rejects.toThrow('unresolved=0, failed=1; continuationFailures=0, pendingContinuations=0');
    expect(page.detached()).toBe(true);
    expect(page.continued()).toEqual([[]]);
  });

  test('bounds unresolved headers and never substitutes zero Authorization', async () => {
    const page = fakePage();
    const traffic = await observeBrowserTraffic(page.value, undefined, 5);
    page.emit(new Promise(() => {}));
    await expect(traffic.settle()).rejects.toThrow('unresolved=1, failed=0; continuationFailures=0, pendingContinuations=0; resourceTypes=document.');
  });

  test('fails closed when unchanged request continuation fails', async () => {
    const page = fakePage();
    const traffic = await observeBrowserTraffic(page.value, undefined, 10);
    page.emit(Promise.resolve({}), Promise.reject(new Error('private-network-error')));
    await expect(traffic.settle()).rejects.toThrow('continuationFailures=1, pendingContinuations=0');
    expect(page.continued()).toEqual([[]]);
  });

  test('bounds pending continuation without accepting complete headers alone', async () => {
    const page = fakePage();
    const traffic = await observeBrowserTraffic(page.value, undefined, 5);
    page.emit(Promise.resolve({}), new Promise(() => {}));
    await expect(traffic.settle()).rejects.toThrow('continuationFailures=0, pendingContinuations=1');
  });

  test('preserves earlier route handlers and still observes complete raw headers', async ({ page }) => {
    let handled = 0;
    await page.route('https://observer.invalid/chain', async route => {
      handled++;
      await route.fulfill({ status: 200, contentType: 'text/html', body: '<title>observer contract</title>' });
    });
    const traffic = await observeBrowserTraffic(page);
    await page.goto('https://observer.invalid/chain');
    expect(handled).toBe(1);
    expect(await traffic.settle()).toEqual({ apiOriginRequests: 0, authorizationHeaders: 0, tokenBearingUrls: 0 });
    // Removing the observer must also leave the earlier handler installed.
    await page.reload();
    expect(handled).toBe(2);
  });

  test('rejects an empty observation and removes only its observer', async () => {
    const page = fakePage();
    const traffic = await observeBrowserTraffic(page.value, undefined, 5);
    await expect(traffic.settle()).rejects.toThrow('no request headers were observed');
    expect(page.detached()).toBe(true);
  });
});

function fakePage(): {
  value: Page;
  emit(headers: Promise<Record<string, string>>, continuation?: Promise<void>): void;
  detached(): boolean;
  continued(): unknown[][];
} {
  let listener: ((request: Request) => void) | undefined;
  let routeListener: ((route: Route) => Promise<void>) | undefined;
  const continued: unknown[][] = [];
  return {
    value: {
      on: (_event: string, callback: (request: Request) => void) => { listener = callback; },
      off: () => { listener = undefined; },
      route: async (_pattern: string, callback: (route: Route) => Promise<void>) => { routeListener = callback; },
      unroute: async () => { routeListener = undefined; },
    } as unknown as Page,
    emit: (headers, continuation = Promise.resolve()) => {
      void routeListener?.({ fallback: (...args: unknown[]) => { continued.push(args); return continuation; } } as unknown as Route);
      listener?.({ url: () => 'https://ui.invalid/', resourceType: () => 'document', allHeaders: () => headers } as unknown as Request);
    },
    detached: () => listener === undefined && routeListener === undefined,
    continued: () => continued,
  };
}
