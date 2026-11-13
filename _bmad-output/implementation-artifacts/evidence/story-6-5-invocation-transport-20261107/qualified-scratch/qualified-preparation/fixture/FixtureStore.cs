namespace Story65.InvocationFixture;

using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.Contracts.Routing;

internal sealed class FixtureStore(CaseState state, RouteCatalogActivationState initial) : IRouteCatalogStateStore
{
    private readonly object gate = new();
    private RouteCatalogActivationState current = initial;
    private int version = 1;
    public RouteCatalogStateCoordinates Coordinates { get; } = new("synthetic-state", "synthetic-cell", "http://fixture/");
    internal RouteCatalogActivationState Current { get { lock (gate) return current; } }
    public async Task<(RouteCatalogActivationState State, string ETag)> ReadAsync(CancellationToken token)
    {
        if (!token.CanBeCanceled && state.Writes > 0)
            Verify.That(ReferenceEquals(RouteCatalogRemoteExecution.Current, state.ExpectedReleaseBoundary), "Release did not restore its original enclosing boundary.");
        state.Reads++; state.Observe("store.read", token: token);
        state.FirstProbe?.Invoke(ExecutionContext.IsFlowSuppressed());
        await state.YieldAsync().ConfigureAwait(false); token.ThrowIfCancellationRequested();
        lock (gate) return (current, version.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    public async Task<bool> CompareExchangeAsync(RouteCatalogActivationState next, string etag, CancellationToken token)
    {
        bool release;
        lock (gate) release = next.Leases.Count < current.Leases.Count;
        state.Writes++; state.Observe(release ? "store.release" : "store.admit", next.Leases.Count.ToString(), token);
        if (release)
        {
            Verify.That(ReferenceEquals(RouteCatalogRemoteExecution.Current, state.ExpectedReleaseBoundary), "Release CAS changed the enclosing boundary.");
            state.Releases++; state.ReleaseEntered.TrySetResult();
            if (state.Name == "held-release") await state.ReleaseContinue.Task.ConfigureAwait(false);
            if (state.Name is "release-fault-overrides-response" or "release-fault-overrides-validation")
                throw state.Fault("release", new IOException("synthetic-release-fault"));
        }
        await state.YieldAsync().ConfigureAwait(false); token.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (etag != version.ToString(System.Globalization.CultureInfo.InvariantCulture)) return false;
            current = next; version++; return true;
        }
    }
}
