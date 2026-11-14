import { randomBytes } from 'node:crypto';

import type { APIRequestContext } from '@playwright/test';

import { submitAndWaitForTenantCommand, type TerminalCommandResult } from './eventstore-api-client.js';

export interface LiveTokenAuthority {
  tenantId: string;
  principalId: string;
}

/** Requires actual public enrollment of the compiled Tenants access-recovery contract. */
export async function assertTenantRecoveryEnrollment(eventStore: APIRequestContext, authToken: string): Promise<void> {
  let enrolled = false;
  let status = 504;
  try {
    const response = await eventStore.get('/api/v1/routing/catalog', {
      headers: { Authorization: `Bearer ${authToken}`, 'X-Hexalith-Scope': 'platform:tenants' },
      failOnStatusCode: false,
      timeout: 5_000,
    });
    status = response.status();
    if (status === 200) {
      const body = await response.json() as { schemaVersion?: unknown; operations?: Array<Record<string, unknown>> };
      enrolled = body.schemaVersion === 3 && Array.isArray(body.operations) && body.operations.some((operation) =>
        operation.kind === 'command' && operation.domain === 'tenants' && operation.messageType === 'refresh-tenant-access');
    }
  } catch { /* No response payload, endpoint or token is retained. */ }
  if (!enrolled) {
    throw new Error(`[tenant-readiness] compiled tenants:refresh-access catalog enrollment is required (catalog HTTP ${status}); healthy legacy resources or an older package cohort do not establish recovery support.`);
  }
}

/** Derives a single concrete tenant and principal from the live token, rejecting ambiguous authority. */
export function authorityFromAccessToken(accessToken: string, configuredTenant?: string): LiveTokenAuthority {
  let claims: Record<string, unknown>;
  try {
    const part = accessToken.split('.')[1];
    if (!part) throw new Error();
    claims = JSON.parse(Buffer.from(part, 'base64url').toString('utf8')) as Record<string, unknown>;
    if (!claims || typeof claims !== 'object' || Array.isArray(claims)) throw new Error();
  } catch {
    throw new Error('[tenant-readiness] access token claims could not be decoded.');
  }
  const principalId = typeof claims.sub === 'string' ? claims.sub.trim() : '';
  const current = collectClaims(claims, ['eventstore:current-tenant', 'current_tenant', 'tenantId', 'tenant_id', 'tenant']);
  const memberships = collectClaims(claims, ['eventstore:tenant', 'tenants']);
  const hint = configuredTenant?.trim();
  const candidates = current.length ? current : memberships;
  if (!principalId || candidates.length !== 1 || !isConcreteTenant(candidates[0])) {
    throw new Error('[tenant-readiness] the live token must contain sub and one concrete authoritative tenant.');
  }
  const tenantId = candidates[0];
  if (hint && hint !== tenantId) {
    throw new Error('[tenant-readiness] configured TEST_TENANT_ID does not match the live token current tenant.');
  }
  if (memberships.length && !memberships.includes(tenantId) && !memberships.includes('*')) {
    throw new Error('[tenant-readiness] current tenant is absent from the live token membership claims.');
  }
  return { tenantId, principalId };
}

/**
 * Serial global-setup gate, before workers start. Republishes existing authority without changing
 * lifecycle, membership, roles or metadata. Only Projects list HTTP 200 with the unchanged token
 * establishes readiness. Missing Project journals remain blockers.
 */
export async function ensureProjectsTenantAccess(options: {
  eventStore: APIRequestContext;
  projects: APIRequestContext;
  authToken: string;
  authority: LiveTokenAuthority;
  runId: string;
  timeoutMs?: number;
}): Promise<void> {
  const authority = authorityFromAccessToken(options.authToken, options.authority.tenantId);
  if (authority.principalId !== options.authority.principalId) {
    throw new Error('[tenant-readiness] recovery principal does not match the live token.');
  }
  const timeoutMs = options.timeoutMs ?? 45_000;
  if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) throw new Error('[tenant-readiness] readiness deadline must be positive.');
  const deadline = Date.now() + timeoutMs;
  const correlationId = freshUlid();
  let last = await probe();
  if (last.listStatus === 200) return;
  if (Date.now() >= deadline) {
    throw new Error(`[tenant-readiness] readiness deadline expired before recovery; lastProjects=${JSON.stringify(last)}`);
  }

  // Fresh for each later recovery; POST retries retain this exact operation ID.
  let result: TerminalCommandResult;
  try {
    result = await submitAndWaitForTenantCommand(options.eventStore, options.authToken, {
      messageId: freshUlid(),
      tenantId: authority.tenantId,
      commandType: 'RefreshTenantAccess',
      payload: { TenantId: authority.tenantId },
      correlationId,
    }, Math.max(1, deadline - Date.now()));
  } catch (error) {
    throw new Error(`${error instanceof Error ? error.message : '[tenant-readiness] recovery failed.'} lastProjects=${JSON.stringify(last)}`);
  }
  if (result.status.status !== 'Completed') {
    throw new Error(`[tenant-readiness] recovery did not complete successfully; command=${JSON.stringify(result)}; lastProjects=${JSON.stringify(last)}`);
  }

  while (Date.now() < deadline) {
    last = await probe();
    if (last.listStatus === 200) return;
    const remaining = deadline - Date.now();
    if (remaining > 0) await new Promise((resolve) => setTimeout(resolve, Math.min(250, remaining)));
  }
  throw new Error(
    `[tenant-readiness] Projects list did not reach HTTP 200 within ${timeoutMs}ms; ` +
      `command=${JSON.stringify(result)}; lastProjects=${JSON.stringify(last)}`,
  );

  async function probe(): Promise<{ listStatus: number; listCategory: string | null }> {
    if (Date.now() >= deadline) return { listStatus: 504, listCategory: null };
    try {
      const response = await options.projects.get('/api/v1/projects', {
        headers: {
          Authorization: `Bearer ${options.authToken}`,
          'X-Hexalith-Tenant-Id': authority.tenantId,
          'X-Correlation-Id': correlationId,
        },
        failOnStatusCode: false,
        timeout: Math.max(1, Math.min(5_000, deadline - Date.now())),
      });
      let listCategory: string | null = null;
      if (response.status() === 503) {
        try {
          const body = await response.json() as { category?: unknown };
          if (body.category === 'read_model_unavailable') listCategory = body.category;
        } catch { /* A malformed problem body is still unavailable. */ }
      }
      return { listStatus: response.status(), listCategory };
    } catch {
      return { listStatus: 504, listCategory: null };
    }
  }
}

function collectClaims(claims: Record<string, unknown>, names: string[]): string[] {
  const values: string[] = [];
  for (const name of names) {
    const claim = claims[name];
    for (const item of Array.isArray(claim) ? claim : [claim]) {
      if (typeof item === 'string') values.push(item.trim());
      else if (item && typeof item === 'object') {
        const candidate = (item as Record<string, unknown>).tenantId ?? (item as Record<string, unknown>).id;
        if (typeof candidate === 'string') values.push(candidate.trim());
      }
    }
  }
  return [...new Set(values.filter(Boolean))];
}

function isConcreteTenant(value: string): boolean {
  return value !== '*' && value !== 'system' && !value.startsWith('platform:');
}

/** Canonical ULID: 48-bit timestamp and fresh 80-bit randomness, without changing dependency pins. */
function freshUlid(): string {
  let value = (BigInt(Date.now()) << 80n) | BigInt(`0x${randomBytes(10).toString('hex')}`);
  const alphabet = '0123456789ABCDEFGHJKMNPQRSTVWXYZ';
  let encoded = '';
  for (let index = 0; index < 26; index++) {
    encoded = alphabet[Number(value & 31n)] + encoded;
    value >>= 5n;
  }
  return encoded;
}
