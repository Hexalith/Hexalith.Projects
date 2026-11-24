namespace Hexalith.Projects.Server.Tests.Queries;

using System.Security.Cryptography;
using System.Text;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
#if HEXALITH_SOURCE_FIXTURE
using Hexalith.Folders.Server.Authentication;
using Hexalith.Folders.Server.Queries;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>Composes the actual SDK catalog boundaries around an isolated component fixture's synthetic query inventory.</summary>
internal static class GatewayFixtureCatalogComposition
{
#if HEXALITH_SOURCE_FIXTURE
    /// <summary>Installs the real receiver guard with the same immutable query declarations used by its gateway.</summary>
    internal static void AddFolderReceiver(WebApplicationBuilder builder, string tenant, bool includeAdvisoryQueries = false)
    {
        builder.Services.AddEventStoreDomainServiceSecurity(builder.Configuration);
        builder.Services.AddSingleton<HumanAuthorityReceiptVerifier>();
        builder.Services.AddSingleton<IRouteCatalogAuthority>(new GatewayFixtureRouteCatalog(tenant, includeAdvisoryQueries));
        builder.Services.Configure<RouteCatalogHostOptions>(options => options.Identity = new RouteCatalogHostIdentity(
            "isolated-component-folders", "folders",
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(FolderOwnerReadQueryHandler).Assembly.Location))),
            Digest("isolated-component-http-loopback"), Digest(builder.Configuration["Authentication:JwtBearer:Issuer"]!)));
    }

#endif
    /// <summary>Registers the shared fixture catalog against the SDK Projects receiver.</summary>
    internal static void AddProjectsReceiver(WebApplicationBuilder builder, string tenant, bool includeAdvisoryQueries = false,
        Func<HttpMessageHandler>? createLoopback = null)
    {
        builder.Services.AddSingleton<IRouteCatalogAuthority>(services =>
            new GatewayFixtureRouteCatalog(tenant, includeAdvisoryQueries, () => RouteCatalogDomainObservation.Capabilities(services)));
        builder.Services.Configure<RouteCatalogHostOptions>(options =>
        {
            options.Identity = new RouteCatalogHostIdentity("isolated-component-projects", "projects",
                Digest("isolated-component-projects"), Digest("isolated-component-http-loopback"),
                Digest(builder.Configuration["Authentication:JwtBearer:Issuer"]!));
            if (createLoopback is not null) options.InvocationTransportFactory = _ => createLoopback();
        });
    }

    /// <summary>Uses the SDK's real root/epoch sender with distinct owner-created loopback transports.</summary>
    internal static HttpMessageHandler QueryTransport(IServiceProvider services, Func<HttpMessageHandler> createLoopback)
        => new RouteCatalogInvocationHandler(services.GetRequiredService<IRouteCatalogAuthority>(), createLoopback(),
            Options.Create(new RouteCatalogHostOptions { InvocationTransportFactory = _ => createLoopback() }));

    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
