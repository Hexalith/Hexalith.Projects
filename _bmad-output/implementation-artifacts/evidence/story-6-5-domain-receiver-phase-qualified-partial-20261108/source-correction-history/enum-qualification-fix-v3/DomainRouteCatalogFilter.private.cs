using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.PrivateCaptureCounter;

using Microsoft.AspNetCore.Http;

namespace Hexalith.EventStore.DomainService;

/// <summary>Runs endpoint work only after the registered platform receiver guard admits it.</summary>
internal sealed class DomainRouteCatalogFilter(IDomainRouteCatalogGuard guard, Microsoft.Extensions.Logging.ILogger<DomainRouteCatalogFilter>? logger = null) : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var counters = ReceiverPhaseBootstrap.Begin();
        bool captureTransferred = false;
        try
        {
        using var filterBody = counters?.Stage(CaptureCounters.Stage.ResultTail, CaptureCounters.AllocationMode.None);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        DomainRouteCatalogDecision decision;
        try
        {
            using var guardDecision = counters?.Stage(CaptureCounters.Stage.ComponentFile, CaptureCounters.AllocationMode.None);
            decision = guard is DomainRouteCatalogGuard receiver
                ? await receiver.AcquireDecisionWithTimingAsync(context, counters).ConfigureAwait(false)
                : new(await guard.AcquireAsync(context).ConfigureAwait(false));
            guardDecision?.Dispose();
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (DomainRouteCatalogBadAuxiliaryRequestException) { return Results.BadRequest(); }
        catch (DomainRouteCatalogForbiddenAuxiliaryRequestException) { return Results.StatusCode(StatusCodes.Status403Forbidden); }
        catch (DomainRouteCatalogMissingLegacyReceiverException) { return Results.NotFound(); }
        catch (Exception exception) { RouteCatalogRefusalDiagnostics.Report(logger, "receiver", exception); return Results.StatusCode(503); }
        RouteCatalogDispatchLease? lease = decision.Lease;
        try
        {
            using var scope = lease is null ? null : guard is DomainRouteCatalogGuard
                ? RouteCatalogRemoteCall.EnterReceiver(lease, context.HttpContext.RequestAborted)
                : RouteCatalogRemoteCall.Enter(lease);
            using var filterNext = counters?.Stage(CaptureCounters.Stage.SettingsEnumerationSortDictionary, CaptureCounters.AllocationMode.None);
            object? result = decision.TerminalRefusal ?? await next(context).ConfigureAwait(false);
            filterNext?.Dispose();
            // ASP.NET executes IResult after endpoint filters return, including streamed/custom results.
            if (result is IResult response && lease is not null)
            {
                var ownedResult = new DomainRouteCatalogResult(response, lease, guard is DomainRouteCatalogGuard, counters);
                captureTransferred = true;
                return ownedResult;
            }
            if (lease is not null)
            {
                using var leaseRelease = counters?.Stage(CaptureCounters.Stage.AuthenticationSerialization, CaptureCounters.AllocationMode.None);
                await lease.DisposeAsync().ConfigureAwait(false);
                leaseRelease?.Dispose();
            }
            return result;
        }
        catch
        {
            captureTransferred = false;
            if (lease is not null)
            {
                using var leaseRelease = counters?.Stage(CaptureCounters.Stage.AuthenticationSerialization, CaptureCounters.AllocationMode.None);
                await lease.DisposeAsync().ConfigureAwait(false);
                leaseRelease?.Dispose();
            }
            throw;
        }
        }
        finally
        {
            if (!captureTransferred) counters?.Dispose();
        }
    }
}
