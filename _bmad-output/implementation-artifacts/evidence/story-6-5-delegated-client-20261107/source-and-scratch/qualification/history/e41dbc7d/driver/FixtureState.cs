namespace Story65.DelegatedClientProof;

using System.Collections.Concurrent;

internal sealed class FixtureState(DriverCase test, bool canary = false, CohortBarrier? cohort = null)
{
    internal DriverCase Test { get; } = test;
    internal bool Canary { get; } = canary;
    internal CohortBarrier? Cohort { get; } = cohort;
    internal readonly ConcurrentQueue<Observation> Trace = new();
    internal readonly TaskCompletionSource IssuerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseIssuer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly List<AssertionValueSource> Sources = [];
    internal byte[]? RequestBytes;
    internal string? Assertion;
    internal bool ProofVerified;
    internal int Triggered, RequestDisposed, ResponseDisposed;
    private long _sequence;
    internal void Observe(string operation, string argument = "", CancellationToken token = default)
        => Trace.Enqueue(new(Interlocked.Increment(ref _sequence), operation, argument, token, token.CanBeCanceled, token.IsCancellationRequested));
    internal void Trigger(string fault)
    {
        if (Test.Fault == fault) { Interlocked.Increment(ref Triggered); throw new FixtureFailure(); }
    }
    internal async Task FakeAwaitAsync()
    { if (Canary) await FlowCanaryState.ProbeAsync().ConfigureAwait(false); else await Task.Yield(); }
}
