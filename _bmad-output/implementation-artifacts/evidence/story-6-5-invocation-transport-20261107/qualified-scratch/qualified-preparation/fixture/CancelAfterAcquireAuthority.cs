namespace Story65.InvocationFixture;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal sealed class CancelAfterAcquireAuthority(CaseState state, RouteCatalogAuthority inner) : IRouteCatalogAuthority
{
    public Task PrepareAsync(RouteCatalogActivation candidate, CancellationToken token = default) => inner.PrepareAsync(candidate, token);
    public Task<RouteCatalogHostAttestation> ReadyAsync(string attempt, CancellationToken token = default) => inner.ReadyAsync(attempt, token);
    public Task CommitAsync(string attempt, CancellationToken token = default) => inner.CommitAsync(attempt, token);
    public Task RollbackAsync(RouteCatalogRollbackRequest request, CancellationToken token = default) => inner.RollbackAsync(request, token);
    public Task ReconcileAsync(RouteCatalogQuiescenceRequest request, CancellationToken token = default) => inner.ReconcileAsync(request, token);
    public Task<RouteCatalogGenerationReadback> ReadbackAsync(CancellationToken token = default) => inner.ReadbackAsync(token);
    public async Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken token = default)
    {
        state.Acquisitions++;
        var lease = await inner.AcquireAsync(token).ConfigureAwait(false);
        state.Caller.Cancel(); state.Observe("cancel.after-acquire"); return lease;
    }
}
