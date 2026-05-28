import type { APIRequestContext } from '@playwright/test';
import type { AuthProvider, PlaywrightStorageState, AuthOptions } from '@seontechnologies/playwright-utils/auth-session';

/**
 * Real Keycloak / OIDC auth provider for the Hexalith.Projects E2E lane.
 *
 * Per AR-19 + the test design, E2E proves runtime security with REAL Keycloak tokens
 * (realm `hexalith`) — synthetic JWT generators are for unit/integration tiers only.
 * Uses the OAuth2 resource-owner password grant against the realm token endpoint.
 *
 * Tokens are persisted to disk by auth-session (storage-state shape) and reused across
 * runs until expiry; multiple `userIdentifier`s map to distinct credential sets so
 * cross-tenant isolation negatives can authenticate as different tenants/users.
 */

interface TokenResponse {
  access_token: string;
  expires_in: number;
  refresh_token?: string;
}

interface UserCredentials {
  username: string;
  password: string;
}

const keycloakUrl = () => requireEnv('KEYCLOAK_URL');
const realm = () => process.env.KEYCLOAK_REALM ?? 'hexalith';
const clientId = () => requireEnv('KEYCLOAK_CLIENT_ID');
const clientSecret = () => process.env.KEYCLOAK_CLIENT_SECRET;

function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`[keycloak-auth-provider] ${name} must be set for E2E auth. See .env.example.`);
  }
  return value;
}

/**
 * Resolve credentials for a user identifier. The default user comes from
 * TEST_USER_EMAIL / TEST_USER_PASSWORD; additional users use
 * E2E_USER_<IDENTIFIER>_EMAIL / E2E_USER_<IDENTIFIER>_PASSWORD (uppercased).
 */
function resolveCredentials(userIdentifier: string): UserCredentials {
  if (userIdentifier === 'default') {
    return { username: requireEnv('TEST_USER_EMAIL'), password: requireEnv('TEST_USER_PASSWORD') };
  }
  const key = userIdentifier.toUpperCase().replace(/[^A-Z0-9]+/g, '_');
  return {
    username: requireEnv(`E2E_USER_${key}_EMAIL`),
    password: requireEnv(`E2E_USER_${key}_PASSWORD`),
  };
}

const tokenEndpoint = () => `${keycloakUrl()}/realms/${realm()}/protocol/openid-connect/token`;

/** Decode the `exp` (seconds since epoch) from a JWT access token, or null if undecodable. */
function decodeJwtExpSeconds(rawToken: string): number | null {
  const parts = rawToken.split('.');
  if (parts.length < 2) return null;
  try {
    const payload = JSON.parse(Buffer.from(parts[1], 'base64url').toString('utf8')) as { exp?: number };
    return typeof payload.exp === 'number' ? payload.exp : null;
  } catch {
    return null;
  }
}

export const keycloakAuthProvider: AuthProvider = {
  getEnvironment: (options?: Partial<AuthOptions>) => options?.environment ?? process.env.TEST_ENV ?? 'local',

  getUserIdentifier: (options?: Partial<AuthOptions>) => options?.userIdentifier ?? 'default',

  extractToken: (tokenData) => {
    const state = tokenData as Partial<PlaywrightStorageState>;
    return state.origins?.[0]?.localStorage?.find((item) => item.name === 'access_token')?.value ?? null;
  },

  // We persist the token in localStorage origins (API-first); no cookies to apply.
  extractCookies: () => [],

  isTokenExpired: (rawToken: string) => {
    const expSeconds = decodeJwtExpSeconds(rawToken);
    if (expSeconds === null) return true;
    // Renew 30s early to avoid mid-test expiry.
    return Date.now() > expSeconds * 1000 - 30_000;
  },

  manageAuthToken: async (request: APIRequestContext, options?: Partial<AuthOptions>): Promise<Record<string, unknown>> => {
    const userIdentifier = options?.userIdentifier ?? 'default';
    const { username, password } = resolveCredentials(userIdentifier);

    const form: Record<string, string> = {
      grant_type: 'password',
      client_id: clientId(),
      username,
      password,
      scope: 'openid',
    };
    const secret = clientSecret();
    if (secret) form.client_secret = secret;

    const response = await request.post(tokenEndpoint(), {
      form,
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    });

    if (!response.ok()) {
      const detail = await response.text();
      throw new Error(`[keycloak-auth-provider] token request failed (${response.status()}) for "${userIdentifier}": ${detail}`);
    }

    const { access_token } = (await response.json()) as TokenResponse;

    const storageState: PlaywrightStorageState = {
      cookies: [],
      origins: [
        {
          origin: process.env.API_URL ?? process.env.BASE_URL ?? keycloakUrl(),
          localStorage: [{ name: 'access_token', value: access_token }],
        },
      ],
    };
    return storageState as unknown as Record<string, unknown>;
  },

  // The auth-session library owns on-disk token-file removal; nothing extra to clear here.
  clearToken: () => {
    /* no-op */
  },
};

export default keycloakAuthProvider;
