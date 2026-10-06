import type { FullConfig, FullResult, Reporter, Suite, TestCase, TestResult } from '@playwright/test/reporter';

/**
 * Fails the explicit live lane when collection is empty or any collected case resolves as skipped.
 * The summary is metadata-only: counts, never titles, URLs, identifiers, or error text.
 */
export default class ZeroLiveSkipReporter implements Reporter {
  private collected = 0;
  private readonly skipped = new Set<string>();

  onBegin(_config: FullConfig, suite: Suite): void {
    this.collected = suite.allTests().length;
  }

  onTestEnd(test: TestCase, result: TestResult): void {
    if (result.status === 'skipped') this.skipped.add(test.id);
  }

  async onEnd(result: FullResult): Promise<{ status?: FullResult['status'] } | undefined> {
    if (process.env.E2E_LIVE_APPHOST !== '1') return undefined;

    console.log(`[zero-live-skip] collected=${this.collected} skipped=${this.skipped.size} status=${result.status}`);
    if (this.collected === 0 || this.skipped.size > 0) {
      console.error('[zero-live-skip] the live lane must collect cases and must not skip any of them.');
      return { status: 'failed' };
    }
    return undefined;
  }
}
