import { resolve } from 'node:path';

import type { BrowserContext, Page } from '@playwright/test';

/**
 * Live Chromium storage state written by global setup: the UI-origin HttpOnly server-session cookies,
 * the Keycloak-origin login cookies (Secure, scoped to the realm path), and non-credential UI
 * preferences. It never holds an access, refresh, or identity token, and the managed runner deletes it
 * after each run.
 */
export const browserSessionStoragePath = resolve('.auth', 'projects-ui-browser-session.json');

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
