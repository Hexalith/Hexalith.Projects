import { createHash } from 'node:crypto';

import type { APIRequestContext } from '@playwright/test';

import { submitAndWaitForTenantCommand, type TerminalCommandResult } from './eventstore-api-client.js';

export interface LiveTokenAuthority {
  tenantId: string;
  principalId: string;
}

/** Derives the authoritative Projects tenant and caller principal from the real access token. */
export function authorityFromAccessToken(accessToken: string, configuredTenant?: string): LiveTokenAuthority {
  const claims = decodeJwtClaims(accessToken);
  const principalId = typeof claims.sub === 'string' ? claims.sub.trim() : '';
  const tenantClaims = collectTenantClaims(claims);
  const hint = configuredTenant?.trim();
  const tenantId = hint && tenantClaims.includes(hint)
    ? hint
    : tenantClaims.find((value) => value !== '*' && value !== 'system') ?? tenantClaims[0] ?? '';
  if (!principalId || !tenantId) {
    throw new Error('[tenant-readiness] the live token must contain sub and an authoritative tenant claim.');
  }
  if (hint && !tenantClaims.includes(hint)) {
    throw new Error(`[tenant-readiness] configured TEST_TENANT_ID '${hint}' is not present in the live token.`);
  }
  return { tenantId, principalId };
}

/**
 * Serial readiness gate used once by Playwright global setup. Rejections are tolerated only when
 * the outer Projects list authorization subsequently converges to HTTP 200.
 */
export async function ensureProjectsTenantAccess(options: {
  eventStore: APIRequestContext;
  projects: APIRequestContext;
  authToken: string;
  authority: LiveTokenAuthority;
  runId: string;
  timeoutMs?: number;
}): Promise<void> {
  const timeoutMs = options.timeoutMs ?? 45_000;
  const initial = await projectsAccessProbe(options.projects, options.authToken, options.authority.tenantId, options.runId);
  if (initial.status === 200) return;

  const results: TerminalCommandResult[] = [];
  for (const [commandType, payload] of [
    [
      'CreateTenant',
      {
        TenantId: options.authority.tenantId,
        Name: `Projects E2E ${options.authority.tenantId}`.slice(0, 128),
        Description: 'Tenant provisioned through the supported live E2E readiness gate.',
      },
    ],
    [
      'AddUserToTenant',
      { TenantId: options.authority.tenantId, UserId: options.authority.principalId, Role: 'TenantOwner' },
    ],
  ] as const) {
    const stem = stableRequestId(options.runId, commandType, options.authority.tenantId, options.authority.principalId);
    results.push(
      await submitAndWaitForTenantCommand(options.eventStore, options.authToken, {
        messageId: `message-${stem}`,
        tenantId: options.authority.tenantId,
        commandType,
        payload,
        correlationId: `correlation-${stem}`,
        idempotencyKey: `idempotency-${stem}`,
      }),
    );
  }

  const deadline = Date.now() + timeoutMs;
  let last = initial;
  while (Date.now() < deadline) {
    last = await projectsAccessProbe(options.projects, options.authToken, options.authority.tenantId, options.runId);
    if (last.status === 200) return;
    await pollDelay(500);
  }

  throw new Error(
    `[tenant-readiness] Projects tenant access did not converge within ${timeoutMs}ms; ` +
      `commands=${JSON.stringify(results.map((item) => item.status))}; ` +
      `lastProjects=${JSON.stringify(last)}`,
  );
}

function decodeJwtClaims(accessToken: string): Record<string, unknown> {
  const part = accessToken.split('.')[1];
  if (!part) throw new Error('[tenant-readiness] access token is not a JWT.');
  try {
    return JSON.parse(Buffer.from(part, 'base64url').toString('utf8')) as Record<string, unknown>;
  } catch {
    throw new Error('[tenant-readiness] access token claims could not be decoded.');
  }
}

function collectTenantClaims(claims: Record<string, unknown>): string[] {
  const values: string[] = [];
  for (const name of ['tenantId', 'tenant_id', 'tenant', 'eventstore:tenant', 'tenants']) {
    const claim = claims[name];
    if (typeof claim === 'string') values.push(claim);
    if (Array.isArray(claim)) {
      for (const item of claim) {
        if (typeof item === 'string') values.push(item);
        else if (item && typeof item === 'object') {
          const candidate = (item as Record<string, unknown>).tenantId ?? (item as Record<string, unknown>).id;
          if (typeof candidate === 'string') values.push(candidate);
        }
      }
    }
  }
  return [...new Set(values.map((value) => value.trim()).filter(Boolean))];
}

async function projectsAccessProbe(
  projects: APIRequestContext,
  authToken: string,
  tenantId: string,
  runId: string,
): Promise<{ status: number; body: string }> {
  const response = await projects.get('/api/v1/projects', {
    headers: {
      Authorization: `Bearer ${authToken}`,
      'X-Hexalith-Tenant-Id': tenantId,
      'X-Correlation-Id': `tenant-readiness-${stableRequestId(runId, tenantId)}`,
    },
    failOnStatusCode: false,
  });
  return { status: response.status(), body: (await response.text()).replace(/\s+/g, ' ').slice(0, 800) };
}

function stableRequestId(...parts: string[]): string {
  return createHash('sha256').update(parts.join('|'), 'utf8').digest('hex').slice(0, 32);
}

function pollDelay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}
