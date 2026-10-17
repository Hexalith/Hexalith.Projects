namespace Hexalith.Projects.Server.Projections.ProjectReads;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.Server.Authorization;
using Microsoft.Extensions.Options;

/// <summary>Adds authenticated named projection adapters beside the preserved legacy callback.</summary>
internal static class ProjectReadPlatformEndpoints
{
    /// <summary>Maps the platform dispatch and coordinated rebuild seams.</summary>
    public static void MapProjectReadPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints.ServiceProvider.GetService<ProjectAuthorityRuntimeSecurity>() is null) { return; }
        RouteHandlerBuilder delivery = endpoints.MapPost("/project/v2", async (ProjectionDispatchRequest request,
            IServiceProvider services, IOptions<ProjectionDispatchOptions> options,
            IOptions<DomainProjectionIdentityOptions> identity, CancellationToken cancellationToken) =>
        {
            try
            {
                return (IResult)Results.Ok(await DomainProjectionDispatcher.DispatchAsync(services, request,
                    options.Value, identity.Value, cancellationToken).ConfigureAwait(false));
            }
            catch (ArgumentException) { return Results.BadRequest("invalid-projection"); }
            catch (Exception) when (!cancellationToken.IsCancellationRequested) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
        });
        RouteHandlerBuilder rebuild = endpoints.MapPost("/project/rebuild/v1", async (ProjectionDispatchRequest request,
            IServiceProvider services, IOptions<ProjectionDispatchOptions> options,
            IOptions<DomainProjectionIdentityOptions> identity, CancellationToken cancellationToken) =>
        {
            try
            {
                return (IResult)Results.Ok(await DomainProjectionDispatcher.RebuildAsync(services, request,
                    options.Value, identity.Value, cancellationToken).ConfigureAwait(false));
            }
            catch (ArgumentException) { return Results.BadRequest("invalid-projection"); }
            catch (Exception) when (!cancellationToken.IsCancellationRequested) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
        });
        delivery.RequireAuthorization(EventStoreDomainServicePolicies.Project);
        rebuild.RequireAuthorization(EventStoreDomainServicePolicies.Project);
    }
}
