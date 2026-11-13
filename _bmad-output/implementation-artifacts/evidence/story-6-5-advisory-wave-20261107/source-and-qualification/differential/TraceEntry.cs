namespace Story65.AdvisoryWaveProof;

// Synthetic fixture values remain in memory and never enter CaptureCounters or the receipt.
internal sealed class TraceEntry(int wave, string operation, string item, CancellationToken token, long started) : IDisposable
{
    internal int Wave { get; } = wave;
    internal string Operation { get; } = operation;
    internal string Item { get; } = item;
    internal string Argument = "";
    internal CancellationToken Token { get; } = token;
    internal bool CanBeCanceled { get; } = token.CanBeCanceled;
    internal bool CanceledOnArrival { get; } = token.IsCancellationRequested;
    internal long Started { get; } = started;
    internal long Ended;
    internal string Error = "none";
    internal int CancellationObserved;
    // Registration is established by TraceLog after construction to avoid capturing an unconstructed instance.
    internal CancellationTokenRegistration Registration;
    public void Dispose() => Registration.Dispose();
}
