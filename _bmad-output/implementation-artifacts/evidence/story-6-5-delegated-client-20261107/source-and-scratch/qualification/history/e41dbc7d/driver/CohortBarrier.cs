namespace Story65.DelegatedClientProof;

internal sealed class CohortBarrier(int count)
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrived;
    internal async Task ArriveAsync(CancellationToken token)
    {
        int arrived = Interlocked.Increment(ref _arrived);
        Check.Require(count is 2 or 4 && arrived <= count, "Bounded cohort size changed.");
        if (arrived == count) _release.TrySetResult();
        await _release.Task.WaitAsync(token).ConfigureAwait(false);
    }
}
