using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.PrivateCaptureCounter;

using Microsoft.AspNetCore.Http;

namespace Hexalith.EventStore.DomainService;

/// <summary>Holds receiver authority through the actual asynchronous response execution.</summary>
internal sealed class DomainRouteCatalogResult(IResult result, RouteCatalogDispatchLease lease, bool trustedReceiver) : IResult
{
    private readonly CaptureCounters? _counters;

    /// <summary>Transfers only the private diagnostic capture after wrapper construction.</summary>
    internal DomainRouteCatalogResult(IResult result, RouteCatalogDispatchLease lease, bool trustedReceiver, CaptureCounters? counters)
        : this(result, lease, trustedReceiver) => _counters = counters;

    /// <summary>Preserves ordinary extension-guard response execution without conferring a trusted receiver marker.</summary>
    internal DomainRouteCatalogResult(IResult result, RouteCatalogDispatchLease lease) : this(result, lease, false) { }

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        CaptureCounters.Interval? releaseInterval = null;
        try
        {
        ArgumentNullException.ThrowIfNull(httpContext);
        await using var release = lease.ConfigureAwait(false);
        try
        {
        using var scope = trustedReceiver ? RouteCatalogRemoteCall.EnterReceiver(lease, httpContext.RequestAborted)
            : RouteCatalogRemoteCall.Enter(lease);
        using var resultExecution = _counters?.Stage(CaptureCounters.Stage.ActorBoundary, CaptureCounters.AllocationMode.None);
        await result.ExecuteAsync(httpContext).ConfigureAwait(false);
        resultExecution?.Dispose();
        }
        finally
        {
            // Original remote scope has unwound; original await-using lease disposal follows.
            releaseInterval = _counters?.Stage(CaptureCounters.Stage.AuthenticationSerialization, CaptureCounters.AllocationMode.None);
        }
        }
        finally
        {
            releaseInterval?.Dispose();
            _counters?.Dispose();
        }
    }
}
