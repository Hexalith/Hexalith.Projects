import { test, expect, type APIRequestContext } from '@playwright/test';

import { submitAndWaitForTenantCommand, type TenantCommandSubmission } from '../support/helpers/eventstore-api-client.js';
import { assertTenantRecoveryEnrollment, authorityFromAccessToken, ensureProjectsTenantAccess } from '../support/helpers/tenant-access-readiness.js';

const authority = { tenantId: 'source-tenant', principalId: 'source-principal' };
const token = jwt({ sub: authority.principalId, 'eventstore:current-tenant': authority.tenantId, 'eventstore:tenant': [authority.tenantId] });
const command: TenantCommandSubmission = {
  tenantId: authority.tenantId,
  messageId: '01ARZ3NDEKTSV4RRFFQ69G5FAV',
  correlationId: '01ARZ3NDEKTSV4RRFFQ69G5FAW',
  commandType: 'RefreshTenantAccess',
  payload: { TenantId: authority.tenantId },
};

test.describe('tenant access recovery helper contracts', () => {
  test('requires compiled public catalog enrollment instead of healthy legacy resources', async () => {
    await expect(assertTenantRecoveryEnrollment(context([], () => response(400), () => response(200, {
      schemaVersion: 3, operations: [{ kind: 'command', domain: 'tenants', messageType: 'refresh-tenant-access' }],
    })), token)).resolves.toBeUndefined();
    for (const [status, body] of [
      [503, {}], [200, { schemaVersion: 2, operations: [] }], [200, { schemaVersion: 3, operations: [] }],
    ] as const) {
      await expect(assertTenantRecoveryEnrollment(context([], () => response(400), () => response(status, body)), token))
        .rejects.toThrow('catalog enrollment is required');
    }
  });
  test('derives current authority without selecting an arbitrary membership', () => {
    expect(authorityFromAccessToken(token)).toEqual(authority);
    expect(authorityFromAccessToken(jwt({ sub: 'actor', 'eventstore:current-tenant': 'b', 'eventstore:tenant': ['a', 'b'] })))
      .toEqual({ tenantId: 'b', principalId: 'actor' });
    for (const claims of [
      { sub: 'actor', tenants: ['a', 'b'] },
      { sub: 'actor', tenant: 'system' },
      { sub: 'actor', tenant: '*' },
      { sub: 'actor', tenant: 'platform:tenants' },
      { sub: 'actor', tenant: 'a', 'eventstore:current-tenant': 'b' },
      { sub: 'actor', 'eventstore:current-tenant': 'b', tenants: ['a'] },
      { tenant: 'a' },
    ]) expect(() => authorityFromAccessToken(jwt(claims))).toThrow('[tenant-readiness]');
    expect(() => authorityFromAccessToken(token, 'foreign-tenant')).toThrow('does not match');
    expect(() => authorityFromAccessToken('malformed')).toThrow('could not be decoded');
  });

  test('recovery uses platform scope on submission and status, and a conflict is polled', async () => {
    const calls: Call[] = [];
    const eventStore = context(calls, () => response(409), () => response(200, { status: 'Completed', failureReason: 'private payload' }));
    const result = await submitAndWaitForTenantCommand(eventStore, token, command);
    expect(result).toEqual({ submissionStatus: 409, status: { status: 'Completed', statusCode: 200, rejectionEventType: undefined } });
    expect(calls.map((call) => call.path)).toEqual(['/api/v1/commands', `/api/v1/commands/status/${command.messageId}`]);
    for (const call of calls) {
      expect(call.options?.headers).toMatchObject({ Authorization: `Bearer ${token}`, 'X-Hexalith-Scope': 'platform:tenants' });
    }
    expect(calls[0].options?.data).toMatchObject({ tenant: 'platform:tenants', aggregateId: authority.tenantId, commandType: 'RefreshTenantAccess' });
    expect(JSON.stringify(result)).not.toContain('private payload');
  });

  test('ambiguous POST retries reuse one operation ID and remain within one deadline', async () => {
    const calls: Call[] = [];
    let attempts = 0;
    const eventStore = context(calls, () => response(++attempts === 1 ? 503 : 202), () =>
      attempts === 1 ? response(404) : response(200, { status: 'Completed' }));
    await submitAndWaitForTenantCommand(eventStore, token, command, 1_000);
    const posts = calls.filter((call) => call.method === 'POST');
    expect(posts).toHaveLength(2);
    expect(posts[0].options?.data).toEqual(posts[1].options?.data);
    expect(posts[1].options?.timeout).toBeLessThan(posts[0].options?.timeout ?? 0);
  });

  test('lost POST response with a completed status does not submit a second command', async () => {
    const calls: Call[] = [];
    const eventStore = context(calls, () => { throw new Error('credential-bearing transport error'); }, () => response(200, { status: 'Completed' }));
    expect((await submitAndWaitForTenantCommand(eventStore, token, command)).status.status).toBe('Completed');
    expect(calls.filter((call) => call.method === 'POST')).toHaveLength(1);
  });

  test('synchronous permission denial is resolved to terminal Rejected, never Completed', async () => {
    const calls: Call[] = [];
    const eventStore = context(calls, () => response(422), () => response(200, { status: 'Rejected', rejectionEventType: 'InsufficientPermissionsRejection' }));
    expect((await submitAndWaitForTenantCommand(eventStore, token, command)).status.status).toBe('Rejected');
    expect(calls).toHaveLength(2);
  });

  test('anonymous admission fails immediately and does not poll a fabricated success', async () => {
    const calls: Call[] = [];
    const eventStore = context(calls, () => response(401), () => response(200, { status: 'Completed' }));
    await expect(submitAndWaitForTenantCommand(eventStore, token, command)).rejects.toThrow('submission failed (401)');
    expect(calls).toHaveLength(1);
  });

  test('terminal polling and submission attempts share a finite overall budget', async () => {
    const calls: Call[] = [];
    const eventStore = context(calls, () => response(503), () => response(404));
    const started = Date.now();
    await expect(submitAndWaitForTenantCommand(eventStore, token, command, 30, 1)).rejects.toThrow('did not reach a terminal state');
    expect(Date.now() - started).toBeLessThan(1_000);
    expect(calls.filter((call) => call.method === 'POST')).toHaveLength(1);
    expect(calls.every((call) => (call.options?.timeout ?? 0) <= 30)).toBe(true);
  });

  test('ready list HTTP 200 avoids source commands and uses the unchanged token', async () => {
    const calls: Call[] = [];
    await ensureProjectsTenantAccess({
      eventStore: context([], () => { throw new Error('unexpected recovery'); }),
      projects: context(calls, () => { throw new Error('unexpected write'); }, () => response(200)),
      authToken: token, authority, runId: 'run-contract',
    });
    expect(calls).toHaveLength(1);
    expect(calls[0].options?.headers).toMatchObject({ Authorization: `Bearer ${token}`, 'X-Hexalith-Tenant-Id': authority.tenantId });
  });

  test('refreshes only existing authority and gives a later recovery a fresh ULID', async () => {
    const calls: Call[] = [];
    const eventStore = context(calls, () => response(202), () => response(200, { status: 'Completed' }));
    for (let recovery = 0; recovery < 2; recovery++) {
      let probes = 0;
      await ensureProjectsTenantAccess({
        eventStore,
        projects: context([], () => { throw new Error('unexpected write'); }, () => response(++probes === 1 ? 404 : 200)),
        authToken: token, authority, runId: 'same-run',
      });
    }
    const posts = calls.filter((call) => call.method === 'POST');
    const bodies = posts.map((call) => call.options?.data as Record<string, unknown>);
    expect(bodies.map((body) => body.commandType)).toEqual(['RefreshTenantAccess', 'RefreshTenantAccess']);
    for (const body of bodies) {
      expect(body.messageId).toMatch(/^[0-7][0-9A-HJKMNP-TV-Z]{25}$/);
      expect(body.payload).toEqual({ TenantId: authority.tenantId });
    }
    expect(bodies[0].messageId).not.toBe(bodies[1].messageId);
  });

  for (const status of ['Rejected', 'PublishFailed', 'TimedOut']) {
    test(`terminal ${status} cannot be masked by a later list HTTP 200`, async () => {
      let probes = 0;
      await expect(ensureProjectsTenantAccess({
        eventStore: context([], () => response(status === 'Rejected' ? 422 : 202), () => response(200, { status })),
        projects: context([], () => response(400), () => response(++probes === 1 ? 404 : 200)),
        authToken: token, authority, runId: 'terminal-failure',
      })).rejects.toThrow('recovery did not complete successfully');
      expect(probes).toBe(1);
    });
  }

  for (const listStatus of [400, 404, 503]) {
    test(`Completed recovery with Projects list ${listStatus} remains a blocker`, async () => {
      const projectsCalls: Call[] = [];
      await expect(ensureProjectsTenantAccess({
        eventStore: context([], () => response(202), () => response(200, { status: 'Completed' })),
        projects: context(projectsCalls, () => { throw new Error('unexpected mutation'); }, () => response(listStatus, { category: 'read_model_unavailable' })),
        authToken: token, authority, runId: 'blocked', timeoutMs: 30,
      })).rejects.toThrow('Projects list did not reach HTTP 200');
      expect(projectsCalls.every((call) => call.method === 'GET')).toBe(true);
    });
  }

  test('initial readiness probe cannot start a recovery after consuming the deadline', async () => {
    const calls: Call[] = [];
    const started = Date.now();
    await expect(ensureProjectsTenantAccess({
      eventStore: context(calls, () => response(202)),
      projects: context([], () => response(400), () => {
        // Synchronous clock advancement models a probe returning at its request deadline.
        while (Date.now() - started < 5) { /* Consume the entire configured budget. */ }
        return response(503);
      }),
      authToken: token, authority, runId: 'expired', timeoutMs: 1,
    })).rejects.toThrow('deadline expired before recovery');
    expect(calls).toEqual([]);
  });
});

type RequestOptions = NonNullable<Parameters<APIRequestContext['post']>[1]>;
interface Call { method: string; path: string; options?: RequestOptions }
function context(calls: Call[], post: () => ReturnType<typeof response>, get = () => response(200)): APIRequestContext {
  return {
    post: async (path: string, options?: RequestOptions) => { calls.push({ method: 'POST', path, options }); return post(); },
    get: async (path: string, options?: RequestOptions) => { calls.push({ method: 'GET', path, options }); return get(); },
  } as unknown as APIRequestContext;
}
function response(status: number, body: unknown = {}) {
  return { status: () => status, json: async () => body };
}
function jwt(claims: Record<string, unknown>): string {
  return `header.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.signature`;
}
