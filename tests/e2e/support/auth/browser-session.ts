import { resolve } from 'node:path';

import type { BrowserContext, Page, Request, Route } from '@playwright/test';

/**
 * Live Chromium storage state written by global setup: the UI-origin HttpOnly server-session cookies,
 * the Keycloak-origin login cookies (Secure, scoped to the realm path), and non-credential UI
 * preferences. It never holds an access, refresh, or identity token, and the managed runner deletes it
 * after each run.
 */
export const browserSessionStoragePath = resolve(process.env.E2E_BROWSER_SESSION_FILE ?? '.auth/projects-ui-browser-session.json');

const TOKEN_LIKE_VALUE = /access_token|refresh_token|id_token|eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/i;
const SESSION_LIKE_COOKIE = /cookie|session|auth|token|oidc|nonce|correlation/i;
// Credential-bearing storage keys; generic names such as UI design tokens are not credentials.
const TOKEN_LIKE_KEY = /(?:access|refresh|id)[_-]?token|bearer|oidc|jwt/i;

/** Metadata-only summary of the browser-visible session surface; never contains cookie values. */
export interface BrowserSessionExposure {
  httpOnlyCookieCount: number;
  scriptReadableSessionCookies: string[];
  tokenLikeCookieNames: string[];
  tokenLikeStorageKeys: string[];
}

/**
 * Inspects cookies plus local/session storage for the UI origin. The server session must be carried by
 * HttpOnly cookies only; no access, refresh, or identity token may be readable by page script.
 */
export async function inspectBrowserSession(page: Page, context: BrowserContext, baseUrl: string): Promise<BrowserSessionExposure> {
  const cookies = await context.cookies(baseUrl);
  const storage = await page.evaluate(() => ({
    entries: [
      ...Object.keys(localStorage).map((key) => [key, localStorage.getItem(key) ?? ''] as const),
      ...Object.keys(sessionStorage).map((key) => [key, sessionStorage.getItem(key) ?? ''] as const),
    ],
    documentCookie: document.cookie,
  }));
  const scriptReadableCookieNames = new Set(
    storage.documentCookie
      .split(';')
      .map((pair) => pair.split('=')[0]?.trim())
      .filter((name): name is string => Boolean(name)),
  );

  return {
    httpOnlyCookieCount: cookies.filter((cookie) => cookie.httpOnly).length,
    scriptReadableSessionCookies: cookies
      .filter((cookie) => (!cookie.httpOnly || scriptReadableCookieNames.has(cookie.name)) && SESSION_LIKE_COOKIE.test(cookie.name))
      .map((cookie) => cookie.name),
    tokenLikeCookieNames: cookies
      .filter((cookie) => TOKEN_LIKE_VALUE.test(cookie.value) && !cookie.httpOnly)
      .map((cookie) => cookie.name),
    tokenLikeStorageKeys: storage.entries
      .filter(([key, value]) => TOKEN_LIKE_VALUE.test(key) || TOKEN_LIKE_VALUE.test(value) || TOKEN_LIKE_KEY.test(key))
      .map(([key]) => key),
  };
}

/** Throws a metadata-only error when the browser session exposes a token or a script-readable session cookie. */
export function assertServerOnlySession(exposure: BrowserSessionExposure, source: string): void {
  if (exposure.httpOnlyCookieCount === 0) {
    throw new Error(`[${source}] the browser session has no HttpOnly server-session cookie.`);
  }
  const leaks = [
    ...exposure.scriptReadableSessionCookies.map((name) => `cookie:${name}`),
    ...exposure.tokenLikeCookieNames.map((name) => `cookie-value:${name}`),
    ...exposure.tokenLikeStorageKeys.map((key) => `storage:${key}`),
  ];
  if (leaks.length > 0) {
    throw new Error(`[${source}] browser-readable session material detected: ${leaks.join(', ')}.`);
  }
}

/** Observes complete request headers without retaining their values; incomplete observations fail closed. */
export async function observeBrowserTraffic(page: Page, apiUrl?: string, timeoutMs = 5_000): Promise<{
  settle: () => Promise<{ apiOriginRequests: number; authorizationHeaders: number; tokenBearingUrls: number }>;
}> {
  if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) throw new Error('[browser-traffic] observation deadline must be positive.');
  const apiOrigin = apiUrl ? new URL(apiUrl).origin : undefined;
  const counts = { apiOriginRequests: 0, authorizationHeaders: 0, tokenBearingUrls: 0 };
  const requests: Request[] = [];
  const continuations: Array<{ complete: boolean; failed: boolean; promise: Promise<void> }> = [];
  const continueUnchanged = (route: Route) => {
    const continuation = { complete: false, failed: false, promise: Promise.resolve() };
    continuation.promise = route.fallback().then(() => { continuation.complete = true; }, () => {
      continuation.complete = true; continuation.failed = true;
    });
    continuations.push(continuation);
    return continuation.promise;
  };
  // Chromium exposes complete raw headers at this supported request-paused seam even for
  // pending fetches whose response has not arrived. Fall through earlier handlers unchanged;
  // this observes security properties and never supplies fixture responses or state.
  await page.route('**/*', continueUnchanged);
  const observe = (request: Request) => {
    const url = new URL(request.url());
    if (url.origin === apiOrigin) counts.apiOriginRequests++;
    if (/(?:^|[?#&])(?:access_token|id_token|refresh_token)=/.test(`${url.search}${url.hash}`)) counts.tokenBearingUrls++;
    requests.push(request);
  };
  page.on('request', observe);
  return { settle: async () => {
    // Freeze the observed request set once the page is ready. A later SignalR request must not
    // keep extending observation, and a missing headers response can never stand in for zero.
    page.off('request', observe);
    const observedContinuations = [...continuations];
    await page.unroute('**/*', continueUnchanged);
    if (!requests.length) throw new Error('[browser-traffic] no request headers were observed.');
    // Read complete headers after navigation has settled, rather than starting protocol header
    // requests while the authorization-code redirect chain is still replacing documents.
    const pending = requests.map(request => {
      const observation = { complete: false, failed: false, promise: Promise.resolve() };
      observation.promise = request.allHeaders().then(headers => {
        if (Object.keys(headers).some(key => key.toLowerCase() === 'authorization')) counts.authorizationHeaders++;
        observation.complete = true;
      }, () => { observation.failed = true; observation.complete = true; });
      return observation;
    });
    let timer: ReturnType<typeof setTimeout> | undefined;
    try {
      await Promise.race([
        Promise.all([...pending, ...observedContinuations].map(item => item.promise)),
        new Promise<void>(resolve => { timer = setTimeout(resolve, timeoutMs); }),
      ]);
      const unresolved = pending.filter(item => !item.complete).length;
      const failed = pending.filter(item => item.failed).length;
      const continuationFailures = observedContinuations.filter(item => item.failed).length;
      const pendingContinuations = observedContinuations.filter(item => !item.complete).length;
      if (unresolved || failed || continuationFailures || pendingContinuations) {
        const types = requests.filter((_request, index) => !pending[index]?.complete).map(request => request.resourceType()).sort();
        throw new Error(`[browser-traffic] incomplete header observations: unresolved=${unresolved}, failed=${failed}; continuationFailures=${continuationFailures}, pendingContinuations=${pendingContinuations}; resourceTypes=${types.join(',')}.`);
      }
      return counts;
    } finally {
      if (timer) clearTimeout(timer);
    }
  } };
}
