namespace Story65.InvocationFixture;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal sealed class FixtureTrust(CaseState state, DevelopmentRouteCatalogTrustValidator original) : IRouteCatalogTrustValidator
{
    public async Task ValidateAsync(RouteCatalogActivation activation, string binding, CancellationToken token)
    {
        state.TrustCalls++; state.Observe("trust", token: token);
        await original.ValidateAsync(activation, binding, token).ConfigureAwait(false);
    }
}
