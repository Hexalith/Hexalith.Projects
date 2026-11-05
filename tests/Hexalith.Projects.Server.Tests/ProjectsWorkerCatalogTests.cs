namespace Hexalith.Projects.Server.Tests;

using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.Workers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

/// <summary>Verifies the private peer enrollment keeps actual worker callbacks behind the Dapr channel boundary.</summary>
public sealed class ProjectsWorkerCatalogTests
{
#if HEXALITH_SOURCE_FIXTURE
    /// <summary>The selected private worker never exposes subscription discovery or event delivery anonymously.</summary>
    [Fact]
    public void PrivateWorkerCallbacksRequireTheAuthenticatedChannel()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddProjectsTenantEventWorkers();
        builder.Services.AddEventStoreHttpHostSecurity();
        using WebApplication app = builder.Build();
        IEndpointRouteBuilder subscriptions = app.UseProjectsDevelopmentWorkerSecurity(selected: true);
        subscriptions.MapSubscribeHandler();
        subscriptions.MapProjectsTenantEventWorkerEndpoints();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        EventStoreDomainServiceEndpointInventory.Validate(endpoints, fallbackPolicy: null).ShouldBeEmpty();
        foreach (RouteEndpoint endpoint in endpoints.OfType<RouteEndpoint>())
        {
            endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldBeNull();
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(policy => policy.Policy)
                .ShouldBe([EventStoreWorkloadAuthenticationDefaults.SidecarChannelPolicy]);
        }
    }
#endif

    /// <summary>A production worker cannot select the disposable Development authority.</summary>
    [Fact]
    public void WorkerBootstrapRejectsProduction()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = Path.GetTempPath(),
            ["Projects:SupportedReads:Enabled"] = "true",
        });
        Should.Throw<InvalidOperationException>(() => builder.AddProjectsDevelopmentWorkerCatalog());
    }
}
