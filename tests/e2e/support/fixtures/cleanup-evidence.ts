import type { TestInfo } from '@playwright/test';

import {
  summarizeCleanup,
  type FixtureCleanupAttempt,
  type FixtureCleanupResult,
} from '../helpers/live-fixtures-api-client.js';

/** One cleanup step, registered only after the resource it removes was requested. */
export interface CleanupStep {
  /** Metadata-only role label, for example `projects:primary`; never an identifier or payload. */
  role: string;
  /** Returns typed role/status attempts; a thrown error is recorded as a failed attempt for `role`. */
  run: () => Promise<FixtureCleanupAttempt[]>;
}

/**
 * Per-test cleanup ledger. Steps run once, newest first, so dependent resources are removed before
 * the resources they depend on. Evidence carries attempted role and HTTP status only.
 */
export class CleanupLedger {
  private readonly steps: CleanupStep[] = [];

  /** Registers a step; later registrations run earlier. */
  register(step: CleanupStep): void {
    this.steps.push(step);
  }

  /** Runs every pending step in reverse registration order and returns typed evidence. */
  async runReverse(): Promise<FixtureCleanupResult> {
    const attempts: FixtureCleanupAttempt[] = [];
    for (let step = this.steps.pop(); step; step = this.steps.pop()) {
      try {
        attempts.push(...(await step.run()));
      } catch {
        // Error text can contain URLs or response details; the role/status contract drops it.
        attempts.push({ role: step.role, statusCode: null, succeeded: false });
      }
    }
    return { attempts, succeeded: attempts.every((attempt) => attempt.succeeded) };
  }
}

/**
 * Reports teardown evidence. Failed cleanup is attached as metadata-only JSON and fails the test only
 * when the test itself passed, so cleanup never replaces the primary failure.
 */
export async function reportCleanup(testInfo: TestInfo, name: string, result: FixtureCleanupResult): Promise<void> {
  if (result.succeeded) return;
  await attachCleanup(testInfo, name, result);
  if (testInfo.status === testInfo.expectedStatus) {
    throw new Error(`[live-cleanup] ${name} did not complete: ${summarizeCleanup(result)}.`);
  }
}

/**
 * Runs a fixture setup. When setup fails, already-registered steps run in reverse, failed cleanup is
 * attached as diagnostics, and the original setup failure is rethrown unchanged.
 */
export async function setupWithReverseCleanup<T>(
  testInfo: TestInfo,
  name: string,
  ledger: CleanupLedger,
  setup: () => Promise<T>,
): Promise<T> {
  try {
    return await setup();
  } catch (primary) {
    const result = await ledger.runReverse();
    if (!result.succeeded) await attachCleanup(testInfo, `${name}-setup`, result);
    throw primary;
  }
}

async function attachCleanup(testInfo: TestInfo, name: string, result: FixtureCleanupResult): Promise<void> {
  await testInfo.attach(`${name}.json`, {
    body: JSON.stringify(
      {
        attempts: result.attempts.map(({ role, statusCode, succeeded }) => ({ role, statusCode, succeeded })),
        succeeded: result.succeeded,
      },
      null,
      2,
    ),
    contentType: 'application/json',
  });
}
