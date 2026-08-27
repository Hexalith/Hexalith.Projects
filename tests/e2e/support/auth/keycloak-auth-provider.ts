import type { APIRequestContext } from '@playwright/test';

interface TokenResponse {
  access_token: string;
  expires_in: number;
}

interface UserCredentials {
  username: string;
  password: string;
}

/** Acquires a real API-only access token through the realm's dedicated password-grant client. */
export async function requestKeycloakAccessToken(
  request: APIRequestContext,
  userIdentifier = 'default',
): Promise<string> {
  const { username, password } = resolveCredentials(userIdentifier);
  const form: Record<string, string> = {
    grant_type: 'password',
    client_id: requireEnv('KEYCLOAK_CLIENT_ID'),
    username,
    password,
    scope: 'openid',
  };
  const secret = process.env.KEYCLOAK_CLIENT_SECRET?.trim();
  if (secret) form.client_secret = secret;

  const response = await request.post(
    `${requireEnv('KEYCLOAK_URL')}/realms/${process.env.KEYCLOAK_REALM ?? 'hexalith'}/protocol/openid-connect/token`,
    { form, headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
  );
  if (!response.ok()) {
    throw new Error(`[keycloak-auth-provider] token request failed (${response.status()}) for "${userIdentifier}".`);
  }

  const token = (await response.json()) as TokenResponse;
  if (!token.access_token || !Number.isFinite(token.expires_in) || token.expires_in <= 0) {
    throw new Error('[keycloak-auth-provider] token response was incomplete.');
  }
  return token.access_token;
}

/** Gets the browser-login credentials without exposing their values in diagnostics. */
export function browserLoginCredentials(): UserCredentials {
  return resolveCredentials('default');
}

function resolveCredentials(userIdentifier: string): UserCredentials {
  if (userIdentifier === 'default') {
    return {
      username: requireOneOf('TEST_USER_USERNAME', 'TEST_USER_EMAIL'),
      password: requireEnv('TEST_USER_PASSWORD'),
    };
  }
  const key = userIdentifier.toUpperCase().replace(/[^A-Z0-9]+/g, '_');
  return {
    username: requireOneOf(`E2E_USER_${key}_USERNAME`, `E2E_USER_${key}_EMAIL`),
    password: requireEnv(`E2E_USER_${key}_PASSWORD`),
  };
}

function requireOneOf(primaryName: string, legacyName: string): string {
  const value = process.env[primaryName]?.trim() || process.env[legacyName]?.trim();
  if (!value) {
    throw new Error(`[keycloak-auth-provider] ${primaryName} is required (${legacyName} is also accepted).`);
  }
  return value;
}

function requireEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`[keycloak-auth-provider] ${name} is required.`);
  return value;
}
