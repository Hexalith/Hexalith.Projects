import { request } from '@playwright/test';
import {
  authStorageInit,
  clearAuthToken,
  configureAuthSession,
  getAuthToken,
  setAuthProvider,
} from '@seontechnologies/playwright-utils/auth-session';
import keycloakAuthProvider from './support/auth/keycloak-auth-provider.js';

/**
 * Global setup: configure auth-session storage and register the Keycloak provider.
 *
 * Live mode always pre-fetches a fresh token so browser storage uses this AppHost's dynamic
 * UI origin. Offline mode stays network-free unless E2E_AUTH_PREFETCH=1 is explicit.
 */
async function globalSetup(): Promise<void> {
  authStorageInit();

  // Storage dir defaults to `${cwd}/.auth` (gitignored). Only `debug` is tuned here.
  configureAuthSession({
    debug: process.env.E2E_AUTH_DEBUG === '1',
  });

  setAuthProvider(keycloakAuthProvider);

  if (process.env.E2E_LIVE_APPHOST === '1' || process.env.E2E_AUTH_PREFETCH === '1') {
    const authRequest = await request.newContext({
      baseURL: process.env.KEYCLOAK_URL ?? process.env.BASE_URL,
      // Aspire's local Keycloak endpoint uses the development certificate.
      ignoreHTTPSErrors: true,
    });
    try {
      clearAuthToken();
      await getAuthToken(authRequest);
    } finally {
      await authRequest.dispose();
    }
  }
}

export default globalSetup;
