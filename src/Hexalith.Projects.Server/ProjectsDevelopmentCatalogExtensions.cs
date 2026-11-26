namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Routing;

using Hexalith.EventStore.DomainService;

/// <summary>Enrolls the Projects SDK receiver in an explicitly selected Development catalog.</summary>
public static class ProjectsDevelopmentCatalogExtensions
{
    /// <summary>Registers the SDK observer for the selected Development bootstrap.</summary>
    /// <param name="builder">The Projects host builder.</param>
    /// <returns>Whether private catalog enrollment was selected.</returns>
    public static bool AddProjectsDevelopmentCatalog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        bool bootstrap = !string.IsNullOrWhiteSpace(builder.Configuration["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"]);
        if (!bootstrap) return false;
        string[] profiles =
        [
            "Projects:SupportedReads:Enabled",
            "Projects:IsolatedLiveFixtures:Enabled",
            "Projects:DevelopmentRouteCatalog:Enabled",
        ];
        if (!builder.Environment.IsDevelopment() || profiles.Count(key => builder.Configuration.GetValue<bool>(key)) != 1)
        {
            throw new InvalidOperationException("Projects catalog bootstrap requires exactly one explicit Development profile.");
        }

        builder.Services.AddEventStoreDevelopmentDomainCatalog(builder.Configuration, restrictAuxiliaryToInvocations: true,
            typeof(ProjectsRuntimeCatalogDeclarations).Assembly);
        return true;
    }
}
