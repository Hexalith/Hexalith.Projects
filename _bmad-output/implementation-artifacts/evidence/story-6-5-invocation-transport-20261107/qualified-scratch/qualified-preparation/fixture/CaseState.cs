namespace Story65.InvocationFixture;

using System.Collections.Concurrent;
using Hexalith.EventStore.Contracts.Routing;

internal sealed class CaseState(string name, Action<bool>? firstProbe, Func<Task>? fakeAwait)
{
    internal readonly string Name = name;
    internal readonly Action<bool>? FirstProbe = firstProbe;
    internal readonly Func<Task>? FakeAwait = fakeAwait;
    internal readonly ConcurrentQueue<(string Operation, string Argument, CancellationToken Token, bool Cancellable, bool Canceled)> Trace = new();
    internal readonly Dictionary<string, int> Faults = new(StringComparer.Ordinal);
    internal readonly TaskCompletionSource TransportEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource TransportContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource ReleaseContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly CancellationTokenSource Caller = new();
    internal Exception? Injected;
    internal IRouteCatalogRemoteBoundary? ExpectedReleaseBoundary;
    internal int Reads, Writes, Acquisitions, ObserverCalls, TrustCalls, Releases, Transports, Ordinary, BodyReads, RequestDisposals, ResponseDisposals, Connections;
    internal int NewEntries = 1;
    internal bool DenyObserver, HeldWait, HandlerReturned, OriginalResponse, ScopePathVerified;
    internal HttpResponseMessage Response = null!;
    internal void Observe(string operation, string argument = "", CancellationToken token = default)
        => Trace.Enqueue((operation, argument, token, token.CanBeCanceled, token.IsCancellationRequested));
    internal T Fault<T>(string site, T exception) where T : Exception
    {
        Faults[site] = Faults.GetValueOrDefault(site) + 1;
        Injected = exception; Observe("fault." + site); return exception;
    }
    internal async Task YieldAsync()
    { if (FakeAwait is not null) await FakeAwait().ConfigureAwait(false); else await Task.Yield(); }
    internal object[][] NormalizedTrace()
    {
        List<CancellationToken> tokens = [];
        return Trace.Select(item =>
        {
            int index = tokens.FindIndex(token => token == item.Token);
            if (index < 0) { index = tokens.Count; tokens.Add(item.Token); }
            return new object[] { item.Operation, item.Argument, index, item.Cancellable, item.Canceled };
        }).ToArray();
    }
}
