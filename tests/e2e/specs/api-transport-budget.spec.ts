import { createServer } from 'node:http';
import type { AddressInfo } from 'node:net';

import { expect, test } from '../support/merged-fixtures.js';

test.describe('API transport budget contracts (no app required)', () => {
  // Model a short UI action budget without making a transport test wait 15 seconds.
  test.use({ actionTimeout: 5 });

  test('uses the API transport default and preserves an explicit shorter operation deadline', async ({ apiRequest, request }) => {
    const timers = new Set<ReturnType<typeof setTimeout>>();
    const server = createServer((incoming, response) => {
      const timer = setTimeout(() => {
        timers.delete(timer);
        response.writeHead(200, { 'content-type': 'application/json' }).end('{"ready":true}');
      }, 50);
      timers.add(timer);
      incoming.once('aborted', () => { clearTimeout(timer); timers.delete(timer); });
    });
    await new Promise<void>(resolve => server.listen(0, '127.0.0.1', resolve));
    const baseUrl = `http://127.0.0.1:${(server.address() as AddressInfo).port}`;
    try {
      // Prove the underlying context actually inherited the short UI action limit.
      await expect(request.get(baseUrl)).rejects.toThrow(/Timeout 5ms/);
      const completed = await apiRequest<{ ready: boolean }>({
        method: 'GET', baseUrl, path: '/', retryConfig: { maxRetries: 0 },
      });
      expect(completed.status).toBe(200);
      expect(completed.body).toEqual({ ready: true });
      // The same delayed endpoint must fail when its caller supplies a tighter deadline.
      await expect(apiRequest({
        method: 'GET', baseUrl, path: '/', timeout: 5, retryConfig: { maxRetries: 0 },
      })).rejects.toThrow(/Timeout 5ms/);
    } finally {
      for (const timer of timers) clearTimeout(timer);
      server.closeAllConnections();
      await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
    }
  });
});
