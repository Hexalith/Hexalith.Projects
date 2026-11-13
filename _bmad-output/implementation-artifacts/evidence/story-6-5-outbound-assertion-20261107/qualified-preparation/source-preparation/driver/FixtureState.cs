namespace Story65.OutboundAssertionProof;

using System.Collections.Concurrent;
using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class FixtureState(DriverCase test, bool canary, CancellationTokenSource caller)
{
    internal const string FreshAssertion = "synthetic-fresh-assertion";
    internal readonly DriverCase Test = test;
    internal readonly bool Canary = canary;
    internal readonly CancellationTokenSource Caller = caller;
    internal readonly ConcurrentQueue<Observation> Trace = new();
    internal readonly Dictionary<string, int> Faults = new(StringComparer.Ordinal);
    internal readonly List<AssertionValueSource> Sources = [];
    internal readonly List<string> Logs = [];
    internal readonly TaskCompletionSource IssuerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseIssuer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource BaseEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseBase = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal HttpRequestMessage Request = null!;
    internal HttpResponseMessage Response = null!;
    internal IReadOnlyDictionary<string, string>? ExpectedBindings;
    internal WorkloadAssertionRequest? IssuedRequest;
    internal int IssuerResolutions, LoggerResolutions, Issues, CanBindReads, BaseCalls, RequestDisposed, ResponseDisposed, Buffers;
    internal bool OriginalRequestAtBase, OriginalResponseAtBoundary, ReturnedBeforeBuffer, ResponseCreatedByFixture;
    internal string Audience = "sample", Operation = "", HeadersAtBase = "";
    internal Exception? LastInjectedError;
    private long _sequence;
    internal void Observe(string operation, string argument = "", CancellationToken token = default)
        => Trace.Enqueue(new(Interlocked.Increment(ref _sequence), operation, argument, token, token.CanBeCanceled, token.IsCancellationRequested));
    internal void Mark(string site)
    { Faults[site] = Faults.GetValueOrDefault(site) + 1; Observe("fault." + site); }
    internal void ThrowIf(string site)
    { if (Test.Fault == site) throw Inject(site, new FixtureFailure()); }
    internal T Inject<T>(string site, T exception) where T : Exception
    { Mark(site); LastInjectedError = exception; return exception; }
    internal async Task ProbeAsync()
    { if (Canary) await FlowCanaryState.ProbeAsync().ConfigureAwait(false); else await Task.Yield(); }
}
