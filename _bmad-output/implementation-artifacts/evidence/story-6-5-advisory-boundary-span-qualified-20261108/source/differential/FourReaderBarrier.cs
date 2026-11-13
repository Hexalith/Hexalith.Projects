namespace Story65.AdvisoryWaveProof;

internal sealed class FourReaderBarrier
{
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrived;
    internal async Task ArriveAsync(CancellationToken token)
    {
        int arrived = Interlocked.Increment(ref _arrived);
        if (arrived > 4) throw new InvalidOperationException("More than four fixture readers arrived.");
        if (arrived == 4) _released.TrySetResult();
        await _released.Task.WaitAsync(token).ConfigureAwait(false);
    }
}
