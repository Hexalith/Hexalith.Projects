import type { APIRequestContext } from '@playwright/test';

export type TenantCommandType = 'CreateTenant' | 'AddUserToTenant';

export interface TenantCommandSubmission {
  messageId: string;
  tenantId: string;
  commandType: TenantCommandType;
  payload: Record<string, unknown>;
  correlationId: string;
  idempotencyKey: string;
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

/** Submits through EventStore's supported authenticated command API and waits for a terminal outcome. */
export async function submitAndWaitForTenantCommand(
  eventStore: APIRequestContext,
  authToken: string,
  command: TenantCommandSubmission,
  timeoutMs = 30_000,
): Promise<TerminalCommandResult> {
  const response = await eventStore.post('/api/v1/commands', {
    headers: {
      Authorization: `Bearer ${authToken}`,
      'Idempotency-Key': command.idempotencyKey,
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
      idempotencyKey: command.idempotencyKey,
    },
  });
  if (response.status() !== 202) {
    throw new Error(
      `[tenant-readiness] ${command.commandType} submission failed (${response.status()}): ${await safeResponseText(response)}`,
    );
  }

  const deadline = Date.now() + timeoutMs;
  let last: EventStoreCommandStatus = {};
  while (Date.now() < deadline) {
    const statusResponse = await eventStore.get(`/api/v1/commands/status/${encodeURIComponent(command.messageId)}`, {
      headers: { Authorization: `Bearer ${authToken}`, 'X-Correlation-Id': command.correlationId },
    });
    if (statusResponse.status() === 200) {
      last = (await statusResponse.json()) as EventStoreCommandStatus;
      if (last.status && TERMINAL_STATUSES.has(last.status)) {
        return { submissionStatus: response.status(), status: last };
      }
    } else {
      last = { statusCode: statusResponse.status(), failureReason: await safeResponseText(statusResponse) };
    }
    await pollDelay(250);
  }

  throw new Error(
    `[tenant-readiness] ${command.commandType} did not reach a terminal state within ${timeoutMs}ms; last=${JSON.stringify(last)}`,
  );
}

async function safeResponseText(response: { text(): Promise<string> }): Promise<string> {
  const text = await response.text();
  return text.replace(/\s+/g, ' ').slice(0, 800);
}

function pollDelay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}
