namespace Story65.DaprInvokerProof;

using System.Collections.Concurrent;

internal sealed class FixtureState(DriverCase test, bool canary, CancellationTokenSource caller)
{
    internal readonly DriverCase Test = test;
    internal readonly bool Canary = canary;
    internal readonly CancellationTokenSource Caller = caller;
    internal readonly ConcurrentQueue<Observation> Trace = new();
    internal readonly Dictionary<string, int> Faults = new(StringComparer.Ordinal);
    internal readonly List<string> Logs = [];
    internal readonly TaskCompletionSource AcquireEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource AcquireContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int Acquisitions, Resolutions, Releases, Requests, RequestDisposed, Responses, ResponseDisposed;
    internal bool RemoteBoundaryRestoredAtRelease, RequestUsesOriginalQuery, BindingsVerified;
    internal byte[]? RequestBytes;
    internal string? BindingsJson, RequestArgumentsJson;
    internal string Projection = "";
    private long _sequence;
    internal void Observe(string operation, string argument = "", CancellationToken token = default)
        => Trace.Enqueue(new(Interlocked.Increment(ref _sequence), operation, argument, token, token.CanBeCanceled, token.IsCancellationRequested));
    internal void MarkFault(string name)
    {
        Faults[name] = Faults.GetValueOrDefault(name) + 1;
        Observe("fault." + name);
    }
    internal void ThrowIf(string name)
    {
        if (Test.Fault == name) { MarkFault(name); throw new FixtureFailure(); }
    }
    internal async Task ProbeAsync()
    { if (Canary) await FlowCanaryState.ProbeAsync().ConfigureAwait(false); else await Task.Yield(); }
}
