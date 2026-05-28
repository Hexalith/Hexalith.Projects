import { randomUUID } from 'node:crypto';

/**
 * Hexalith request-header helpers (AR-15 / AR-16).
 *
 * - `Idempotency-Key` is REQUIRED on mutations and REJECTED on queries.
 * - `X-Correlation-Id` / `X-Hexalith-Task-Id` thread tracing through the command-async pipeline.
 * - `X-Hexalith-Freshness` lets a reader express the freshness/trust it requires.
 */

export interface AuthHeaderOptions {
  authToken: string;
  correlationId?: string;
  taskId?: string;
}

export interface MutationHeaderOptions extends AuthHeaderOptions {
  /** Stable across logical retries of the SAME attempt; new value = new attempt. */
  idempotencyKey?: string;
}

const bearer = (token: string) => ({ Authorization: `Bearer ${token}` });

/** Headers for a query/read (no Idempotency-Key — the API rejects it on queries). */
export function queryHeaders(options: AuthHeaderOptions): Record<string, string> {
  return {
    ...bearer(options.authToken),
    'X-Correlation-Id': options.correlationId ?? randomUUID(),
    ...(options.taskId ? { 'X-Hexalith-Task-Id': options.taskId } : {}),
  };
}

/** Headers for a mutation/command (Idempotency-Key required → 202 AcceptedCommand). */
export function mutationHeaders(options: MutationHeaderOptions): Record<string, string> {
  return {
    ...bearer(options.authToken),
    'Idempotency-Key': options.idempotencyKey ?? randomUUID(),
    'X-Correlation-Id': options.correlationId ?? randomUUID(),
    ...(options.taskId ? { 'X-Hexalith-Task-Id': options.taskId } : {}),
  };
}

/** Header requesting a minimum projection freshness on a read (AR-16). */
export function withFreshness(headers: Record<string, string>, freshness: string): Record<string, string> {
  return { ...headers, 'X-Hexalith-Freshness': freshness };
}
