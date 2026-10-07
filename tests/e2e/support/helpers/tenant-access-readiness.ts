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
 * Serial readiness gate used once by Playwright global setup. Tenant command rejections (for example
 * an already-existing tenant or membership) are tolerated only when the outer Projects authorization
 * response then converges: the list read passes authorization (HTTP 200, or `read_model_unavailable`
 * for a tenant with no projected Project yet) and an intentionally invalid create returns HTTP 400
 * from body validation, which the API evaluates only after authorization. Nothing is written to a
 * projection directly.
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
  if (isProjectsAccessReady(initial)) return;

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
      'UpdateTenant',
      {
        TenantId: options.authority.tenantId,
        Name: `Projects E2E ${options.authority.tenantId}`.slice(0, 128),
        Description: 'Tenant refreshed through the supported live E2E readiness gate.',
      },
    ],
    [
      'AddUserToTenant',
      { TenantId: options.authority.tenantId, UserId: options.authority.principalId, Role: 'TenantOwner' },
    ],
  ] as const) {
    const stem = stableRequestId(options.runId, commandType, options.authority.tenantId, options.authority.principalId);
    try {
      results.push(
        await submitAndWaitForTenantCommand(options.eventStore, options.authToken, {
          messageId: `message-${stem}`,
          tenantId: options.authority.tenantId,
          commandType,
          payload,
          correlationId: `correlation-${stem}`,
        }, timeoutMs),
      );
    } catch (error) {
      // Keep the diagnostic metadata-only: the command error already carries statuses only.
      throw new Error(
        `${error instanceof Error ? error.message : '[tenant-readiness] tenant command failed.'} ` +
          `initialProjects=${JSON.stringify(initial)}; ` +
          `completed=${JSON.stringify(results.map((item) => ({ status: item.status.status, statusCode: item.status.statusCode })))}`,
      );
    }
  }

  const deadline = Date.now() + timeoutMs;
  let last = initial;
  while (Date.now() < deadline) {
    last = await projectsAccessProbe(options.projects, options.authToken, options.authority.tenantId, options.runId);
    if (isProjectsAccessReady(last)) return;
    await pollDelay(500);
  }

  throw new Error(
    `[tenant-readiness] Projects tenant access did not converge within ${timeoutMs}ms; ` +
      `commands=${JSON.stringify(results.map((item) => ({ status: item.status.status, statusCode: item.status.statusCode })))}; ` +
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
  for (const name of ['eventstore:current-tenant', 'tenantId', 'tenant_id', 'tenant', 'eventstore:tenant', 'tenants']) {
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

interface ProjectsAccessProbe {
  /** List read status; anything but 200 or an empty-journal 503 means read access is not ready. */
  listStatus: number;
  /** Problem category of a 503 list read; `null` for other statuses and transport failures. */
  listCategory: string | null;
  /** Invalid-create status; 400 proves mutation authorization passed before body validation. */
  createStatus: number;
}

function isProjectsAccessReady(probe: ProjectsAccessProbe): boolean {
  // Unauthorized list reads are a safe-denial 404. A tenant with no projected Project has no list
  // journal yet, and the API reports that as `read_model_unavailable` only after authorization
  // passed; the first seeded Project creates the journal.
  const listAuthorized = probe.listStatus === 200
    || (probe.listStatus === 503 && probe.listCategory === 'read_model_unavailable');
  return listAuthorized && probe.createStatus === 400;
}

async function projectsAccessProbe(
  projects: APIRequestContext,
  authToken: string,
  tenantId: string,
  runId: string,
): Promise<ProjectsAccessProbe> {
  const headers = {
    Authorization: `Bearer ${authToken}`,
    'X-Hexalith-Tenant-Id': tenantId,
    'X-Correlation-Id': `tenant-readiness-${stableRequestId(runId, tenantId)}`,
  };
  let listCategory: string | null = null;
  const listStatus = await safeStatus(async () => {
    const response = await projects.get('/api/v1/projects', {
      headers,
      failOnStatusCode: false,
      timeout: 5_000,
    });
    if (response.status() === 503) listCategory = await problemCategory(response);
    return response;
  });
  const createStatus = await safeStatus(() => projects.post('/api/v1/projects', {
    headers: { ...headers, 'Idempotency-Key': stableRequestId(runId, tenantId, 'authorization-probe') },
    data: {},
    failOnStatusCode: false,
    timeout: 5_000,
  }));
  return { listStatus, listCategory, createStatus };
}

/** Reads only the problem category; the rest of the body is never retained. */
async function problemCategory(response: { json(): Promise<unknown> }): Promise<string | null> {
  try {
    const category = ((await response.json()) as { category?: unknown }).category;
    return typeof category === 'string' ? category : null;
  } catch {
    return null;
  }
}

async function safeStatus(send: () => Promise<{ status(): number }>): Promise<number> {
  try {
    return (await send()).status();
  } catch {
    // A cold authorization/projection dependency may not answer before the per-attempt bound.
    // Collapse transport details (which can contain credentials) into a retryable safe status.
    return 503;
  }
}

function stableRequestId(...parts: string[]): string {
  return createHash('sha256').update(parts.join('|'), 'utf8').digest('hex').slice(0, 32);
}

function pollDelay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}
