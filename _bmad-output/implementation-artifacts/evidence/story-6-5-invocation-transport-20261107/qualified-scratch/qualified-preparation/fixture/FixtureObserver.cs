namespace Story65.InvocationFixture;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal sealed class FixtureObserver(CaseState state, RouteCatalogHostIdentity identity) : IRouteCatalogHostObserver
{
    public async Task<RouteCatalogHostIdentity> ObserveAsync(RouteCatalogActivation activation, RouteCatalogStateCoordinates coordinates, CancellationToken token)
    {
        state.ObserverCalls++; state.Observe("observer", token: token);
        await state.YieldAsync().ConfigureAwait(false); token.ThrowIfCancellationRequested();
        if (state.DenyObserver) throw state.Fault("observer", new RouteCatalogUnavailableException("synthetic-observer-refusal"));
        return identity;
    }
}
