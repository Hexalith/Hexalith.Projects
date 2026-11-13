namespace Story65.AdvisoryWaveProof;

// Gate uses the ordinary fixture clock; only the executor's original evaluation-clock
// call receives this fixed RAM-only failure, outside its existing dependency catches.
internal sealed class ThrowingTestClock(Exception failure) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => throw failure;
}
