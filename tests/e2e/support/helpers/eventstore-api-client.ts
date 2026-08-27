import type { APIRequestContext } from '@playwright/test';

export type TenantCommandType = 'CreateTenant' | 'UpdateTenant' | 'AddUserToTenant';

export interface TenantCommandSubmission {
  messageId: string;
  tenantId: string;
  commandType: TenantCommandType;
  payload: Record<string, unknown>;
  correlationId: string;
}

export interface EventStoreCommandStatus {
  messageId?: string;
  correlationId?: string;
  status?: string;
  statusCode?: number;
  failureReason?: string;
  rejectionEventType?: string;
}

export interface TerminalCommandResult {
  submissionStatus: number;
  status: EventStoreCommandStatus;
}

const TERMINAL_STATUSES = new Set(['Completed', 'Rejected', 'PublishFailed', 'TimedOut']);
const TRANSIENT_SUBMISSION_STATUSES = new Set([502, 503, 504]);

/** Submits through EventStore's supported authenticated command API and waits for a terminal outcome. */
export async function submitAndWaitForTenantCommand(
  eventStore: APIRequestContext,
  authToken: string,
  command: TenantCommandSubmission,
  timeoutMs = 30_000,
): Promise<TerminalCommandResult> {
  const submissionDeadline = Date.now() + timeoutMs;
  let submissionStatus = 0;
  while (Date.now() < submissionDeadline) {
    try {
      const response = await eventStore.post('/api/v1/commands', {
        headers: {
          Authorization: `Bearer ${authToken}`,
          'X-Correlation-Id': command.correlationId,
        },
        data: {
          messageId: command.messageId,
          tenant: 'system',
          domain: 'tenants',
          aggregateId: command.tenantId,
          commandType: command.commandType,
          payload: command.payload,
          correlationId: command.correlationId,
        },
        timeout: 5_000,
      });
      submissionStatus = response.status();
    } catch {
      // A timed-out POST may still have been accepted. Retry the same deterministic message id
      // and suppress transport details because request diagnostics can contain credentials.
      submissionStatus = 504;
    }
    if (submissionStatus === 202) break;
    if (submissionStatus === 409) {
      return {
        submissionStatus,
        status: { status: 'Conflict', statusCode: submissionStatus },
      };
    }
    if (!TRANSIENT_SUBMISSION_STATUSES.has(submissionStatus)) {
      throw new Error(`[tenant-readiness] ${command.commandType} submission failed (${submissionStatus}).`);
    }
    await pollDelay(500);
  }
  if (submissionStatus !== 202) {
    throw new Error(
      `[tenant-readiness] ${command.commandType} submission stayed unavailable (${submissionStatus}) for ${timeoutMs}ms.`,
    );
  }

  const deadline = Date.now() + timeoutMs;
  let last: EventStoreCommandStatus = {};
  while (Date.now() < deadline) {
    try {
      const statusResponse = await eventStore.get(`/api/v1/commands/status/${encodeURIComponent(command.messageId)}`, {
        headers: { Authorization: `Bearer ${authToken}`, 'X-Correlation-Id': command.correlationId },
        timeout: 5_000,
      });
      if (statusResponse.status() === 200) {
        last = (await statusResponse.json()) as EventStoreCommandStatus;
        if (last.status && TERMINAL_STATUSES.has(last.status)) {
          return { submissionStatus, status: last };
        }
      } else {
        last = { statusCode: statusResponse.status() };
      }
    } catch {
      last = { statusCode: 504 };
    }
    await pollDelay(250);
  }

  throw new Error(
    `[tenant-readiness] ${command.commandType} did not reach a terminal state within ${timeoutMs}ms; last=${JSON.stringify(last)}`,
  );
}

function pollDelay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}
