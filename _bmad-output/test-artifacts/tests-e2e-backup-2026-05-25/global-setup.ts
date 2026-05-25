import { authStorageInit, configureAuthSession, setAuthProvider, authGlobalInit } from '@seontechnologies/playwright-utils/auth-session';
import keycloakAuthProvider from './support/auth/keycloak-auth-provider.js';

/**
 * Global setup: configure auth-session storage and register the Keycloak provider.
 *
 * Token pre-fetch (`authGlobalInit`) only runs when E2E_AUTH_PREFETCH=1, so the framework
 * smoke check stays runnable without a live Keycloak. When pre-fetch IS requested, a
 * failure throws loudly — a requested-but-unavailable IdP is a real error, not silent.
 */
async function globalSetup(): Promise<void> {
  authStorageInit();

  // Storage dir defaults to `${cwd}/.auth` (gitignored). Only `debug` is tuned here.
  configureAuthSession({
    debug: process.env.E2E_AUTH_DEBUG === '1',
  });

  setAuthProvider(keycloakAuthProvider);

  if (process.env.E2E_AUTH_PREFETCH === '1') {
    await authGlobalInit();
  }
}

export default globalSetup;
