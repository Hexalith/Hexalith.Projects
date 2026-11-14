import type { APIRequestContext } from '@playwright/test';

export interface TenantCommandSubmission {
  messageId: string;
  tenantId: string;
  commandType: 'RefreshTenantAccess';
  payload: { TenantId: string };
  correlationId: string;
}

export interface EventStoreCommandStatus {
  status?: string;
  statusCode?: number;
  rejectionEventType?: string;
}

export interface TerminalCommandResult {
  submissionStatus: number;
  status: EventStoreCommandStatus;
}

const TERMINAL_STATUSES = new Set(['Completed', 'Rejected', 'PublishFailed', 'TimedOut']);
const TRANSIENT_SUBMISSION_STATUSES = new Set([502, 503, 504]);

/**
 * Recovers source authority through the compiled Tenants command. One deadline covers submission
 * retries and terminal polling. Conflict and synchronous rejection require observing the original
 * operation's status; neither establishes source publication by itself.
 */
export async function submitAndWaitForTenantCommand(
  eventStore: APIRequestContext,
  authToken: string,
  command: TenantCommandSubmission,
  timeoutMs = 30_000,
  maxSubmissionAttempts = 4,
): Promise<TerminalCommandResult> {
  if (!Number.isFinite(timeoutMs) || timeoutMs <= 0 || !Number.isSafeInteger(maxSubmissionAttempts) || maxSubmissionAttempts < 1) {
    throw new Error('[tenant-readiness] command deadline and attempt bound must be positive.');
  }
  if (command.payload.TenantId !== command.tenantId) {
    throw new Error('[tenant-readiness] recovery payload must match the authenticated tenant target.');
  }
  const deadline = Date.now() + timeoutMs;
  const headers = {
    Authorization: `Bearer ${authToken}`,
    'X-Hexalith-Scope': 'platform:tenants',
    'X-Correlation-Id': command.correlationId,
  };
  let submissionStatus = 0;
  let last: EventStoreCommandStatus = {};

  // A lost response may have committed: observe it before retrying the same operation ID.
  for (let attempt = 0; attempt < maxSubmissionAttempts && Date.now() < deadline; attempt++) {
    try {
      const response = await eventStore.post('/api/v1/commands', {
        headers,
        data: {
          messageId: command.messageId,
          tenant: 'platform:tenants',
          domain: 'tenants',
          aggregateId: command.tenantId,
          commandType: command.commandType,
          payload: command.payload,
          correlationId: command.correlationId,
        },
        failOnStatusCode: false,
        timeout: Math.max(1, Math.min(30_000, deadline - Date.now())),
      });
      submissionStatus = response.status();
    } catch {
      // Transport error text may contain credentials; retain only a safe status category.
      submissionStatus = 504;
    }
    if ([202, 409, 422].includes(submissionStatus)) break;
    if (!TRANSIENT_SUBMISSION_STATUSES.has(submissionStatus)) {
      throw new Error(`[tenant-readiness] RefreshTenantAccess submission failed (${submissionStatus}).`);
    }
    last = await observeStatus();
    if (last.status && TERMINAL_STATUSES.has(last.status)) return { submissionStatus, status: last };
    await boundedDelay();
  }

  // Exhausted ambiguous submissions can still be observed under the original deadline.
  while (Date.now() < deadline) {
    last = await observeStatus();
    if (last.status && TERMINAL_STATUSES.has(last.status)) return { submissionStatus, status: last };
    await boundedDelay();
  }
  throw new Error(
    `[tenant-readiness] RefreshTenantAccess did not reach a terminal state within ${timeoutMs}ms; ` +
      `submissionStatus=${submissionStatus}; last=${JSON.stringify(last)}`,
  );

  async function observeStatus(): Promise<EventStoreCommandStatus> {
    if (Date.now() >= deadline) return last;
    try {
      const response = await eventStore.get(`/api/v1/commands/status/${encodeURIComponent(command.messageId)}`, {
        headers,
        failOnStatusCode: false,
        timeout: Math.max(1, Math.min(5_000, deadline - Date.now())),
      });
      if (response.status() !== 200) return { statusCode: response.status() };
      const body = await response.json() as EventStoreCommandStatus;
      return {
        status: typeof body.status === 'string' && TERMINAL_STATUSES.has(body.status) ? body.status : undefined,
        statusCode: 200,
        rejectionEventType: body.rejectionEventType === 'InsufficientPermissionsRejection' ? body.rejectionEventType : undefined,
      };
    } catch {
      return { statusCode: 504 };
    }
  }

  async function boundedDelay(): Promise<void> {
    const remaining = deadline - Date.now();
    if (remaining > 0) await new Promise((resolve) => setTimeout(resolve, Math.min(250, remaining)));
  }
}
