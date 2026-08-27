import type { FullResult, Reporter, Suite, TestCase, TestResult } from '@playwright/test/reporter';

/** Fails the explicit live lane if collection is empty or any case resolves as skipped. */
export default class ZeroLiveSkipReporter implements Reporter {
  private collected = 0;
  private skipped = 0;

  onBegin(_config: unknown, suite: Suite): void {
    this.collected = suite.allTests().length;
  }

  onTestEnd(_test: TestCase, result: TestResult): void {
    if (result.status === 'skipped') this.skipped += 1;
  }

  onEnd(result: FullResult): { status: FullResult['status'] } | undefined {
    if (process.env.E2E_LIVE_APPHOST === '1' && (this.collected === 0 || this.skipped > 0)) {
      return { status: 'failed' };
    }
    return result.status === 'passed' ? undefined : { status: result.status };
  }
}
