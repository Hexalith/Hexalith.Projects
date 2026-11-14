namespace Hexalith.Projects.Server;

using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Projects.Aggregates.Project;

/// <summary>Enrolls the existing bespoke host through supported SDK catalog and security boundaries.</summary>
public static class ProjectsDevelopmentCatalogExtensions
{
    /// <summary>Adds the SDK receiver only for the explicitly selected private Development bootstrap.</summary>
    /// <param name="builder">The Projects host builder.</param>
    /// <returns>Whether private catalog enrollment was selected.</returns>
    public static bool AddProjectsDevelopmentCatalog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        bool bootstrap = !string.IsNullOrWhiteSpace(builder.Configuration["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"]);
        if (!bootstrap) return false;
        bool supportedReads = builder.Configuration.GetValue<bool>("Projects:SupportedReads:Enabled");
        bool isolatedFixtures = builder.Configuration.GetValue<bool>("Projects:IsolatedLiveFixtures:Enabled");
        if (!builder.Environment.IsDevelopment() || supportedReads == isolatedFixtures)
            throw new InvalidOperationException("Projects catalog bootstrap requires exactly one explicit private Development profile.");

        builder.Services.AddSingleton<ProjectsDevelopmentCatalogProfile>();
        builder.Services.AddEventStoreDomainServiceSecurity(builder.Configuration);
        builder.Services.AddSingleton(new DiscoveryResult(
            [new(typeof(ProjectsDomainProcessor), ProjectsServerModule.DomainName, typeof(ProjectState), DomainKind.Aggregate)], []));
        builder.Services.AddSingleton(new LegacyProjectionCatalogRegistration(ProjectsRuntimeCatalogDeclarations.Routes
            .Where(route => route.Kind == "projection" && route.ProjectionType == ProjectsServerModule.ProjectionType).ToArray()));
        builder.Services.AddEventStoreDevelopmentDomainCatalog(builder.Configuration, typeof(ProjectsRuntimeCatalogDeclarations).Assembly);
        return true;
    }

    /// <summary>Maps SDK control endpoints and captures the complete actual receiver inventory.</summary>
    /// <param name="app">The mapped Projects host.</param>
    public static void MapProjectsDevelopmentCatalog(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.Services.GetService<ProjectsDevelopmentCatalogProfile>() is null) return;
        app.Services.GetRequiredService<EventStoreDomainServiceEndpointSource>().Capture(app);
        app.MapEventStoreRouteCatalog();
        app.UseEventStoreDevelopmentDomainCatalog();
    }
}
