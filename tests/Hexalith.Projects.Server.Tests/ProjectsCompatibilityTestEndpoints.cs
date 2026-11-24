namespace Hexalith.Projects.Server.Tests;

using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Authentication;
using Hexalith.Projects.Server.Queries;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>Provides direct inputs for domain and compatibility tests without a workload gateway.</summary>
internal static class ProjectsCompatibilityTestEndpoints
{
    /// <summary>Maps the Projects Server endpoints (the command-async POST + minimal-read GET).</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapProjectsServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints.MapPost(ProjectsServerModule.ProcessRoute, async (
            DomainServiceRequest request,
            IDomainProcessor processor) =>
        {
            DomainResult result = await processor
                .ProcessAsync(request.Command, request.CurrentState)
                .ConfigureAwait(false);
            return Results.Ok(DomainServiceWireResult.FromDomainResult(result));
        });
        _ = endpoints.MapPost(ProjectsServerModule.ProjectRoute, static (ProjectionRequest request) =>
            string.Equals(request.Domain, ProjectsServerModule.DomainName, StringComparison.Ordinal)
                ? Results.Ok(ProjectProjectionHandler.Project(request))
                : Results.NotFound());
        RouteHandlerBuilder queryEndpoint = endpoints.MapPost(
            "/query",
            async (QueryEnvelope query, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
            {
                QueryResult result = await DomainQueryDispatcher.ExecuteAsync(serviceProvider, query, cancellationToken).ConfigureAwait(false);
                return new ProjectsQueryResponsePolicy().CreateResponse(query, result);
            });
        RouteHandlerBuilder metadataEndpoint = endpoints.MapPost(
            "/admin/operational-index-metadata",
            static (AdminOperationalIndexMetadata.Request request, IServiceProvider services) =>
                CreateOperationalIndexMetadata(request, services) is { } response
                    ? Results.Ok(response)
                    : Results.BadRequest());
        IConfiguration configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        IHostEnvironment environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!ProjectsAuthenticationServiceCollectionExtensions.IsAnonymousDevelopmentBypass(configuration, environment))
        {
            _ = queryEndpoint.RequireAuthorization();
            _ = metadataEndpoint.RequireAuthorization();
        }

        endpoints.MapProjectsDomainServiceEndpoints();
        return endpoints;
    }

    internal static AdminOperationalIndexMetadata.Response? CreateOperationalIndexMetadata(
        AdminOperationalIndexMetadata.Request request, IServiceProvider? services = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Domains is not { Count: 1 } ||
            !string.Equals(request.Domains[0], ProjectsServerModule.DomainName, StringComparison.Ordinal) ||
            !string.Equals(request.AppId, ProjectsServerModule.DomainName, StringComparison.Ordinal) ||
            !string.Equals(request.ServiceVersion, "v1", StringComparison.Ordinal))
        {
            return null;
        }

        if (services is not null && services.GetServices<IAsyncDomainProjectionHandler>().Any())
        {
            DomainProjectionIdentityOptions identity = services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value;
            if (request.AppId != identity.AppId || request.ServiceVersion != identity.ServiceVersion) { return null; }
            AdminOperationalIndexMetadata.Response response = AdminOperationalIndexMetadata.Create(services.GetService<DiscoveryResult>() ?? new DiscoveryResult([], []), request.Domains,
                services.GetServices<IDomainQueryHandler>(), services.GetServices<IAsyncDomainProjectionHandler>(),
                identity.AppId, identity.ServiceVersion, services.GetRequiredService<IOptions<ProjectionDispatchOptions>>().Value,
                [ProjectsServerModule.DomainName]);
            return LegacyProjectionCatalogRegistration.Bind(response, services.GetServices<LegacyProjectionCatalogRegistration>());
        }

        // Projects uses its own processor rather than an EventStoreAggregate<TState>. Advertise the
        // exact handler routes so EventStore dispatches these reads to /query instead of a projection actor.
        return new AdminOperationalIndexMetadata.Response(
        [
            new AdminOperationalIndexMetadata.DomainMetadata(
                ProjectsServerModule.DomainName,
                [], [], [], [], [],
                [
                    ProjectsServerModule.GetConversationStartSetupQueryType,
                    ProjectsServerModule.GetProjectContextQueryType,
                    ProjectsServerModule.ExplainContextSelectionQueryType,
                    ResolveProjectChatBotAuthority.QueryType,
                    ListProjectsQuery.QueryType,
                    GetProjectQuery.QueryType,
                    ProjectsServerModule.ResolveProjectFromConversationQueryType,
                    ProjectsServerModule.ResolveProjectFromAttachmentsQueryType,
                ]),
        ]);
    }
}
