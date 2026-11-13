namespace Story65.GatewayControllerProof;

using System.Collections.Concurrent;

internal sealed class FixtureState(DriverCase test, bool canary)
{
    internal readonly DriverCase Test = test;
    internal readonly bool Canary = canary;
    internal readonly ConcurrentQueue<Observation> Trace = new();
    internal readonly TaskCompletionSource AcquireEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource AcquireContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int Acquisitions, Mediations, Releases, Faults;
    internal bool AdmissionVerified, MarkerRestoredAtRelease, RemoteBoundaryRestoredAtRelease;
    private long _sequence;
    internal void Observe(string operation, string argument = "", CancellationToken token = default)
        => Trace.Enqueue(new(Interlocked.Increment(ref _sequence), operation, argument, token, token.CanBeCanceled, token.IsCancellationRequested));
    internal async Task ProbeAsync()
    { if (Canary) await FlowCanaryState.ProbeAsync().ConfigureAwait(false); else await Task.Yield(); }
    internal void Throw(string fault)
    { if (Test.Fault == fault) { Interlocked.Increment(ref Faults); throw new FixtureFailure(); } }
}
