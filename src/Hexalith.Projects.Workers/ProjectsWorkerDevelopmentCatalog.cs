namespace Hexalith.Projects.Workers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

#if SUPPORTED_PROJECT_READ_SOURCE
using System.Security.Cryptography;

using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
#endif

/// <summary>Enrolls the actual callback worker as a receiver-free peer in the private Development lane.</summary>
internal static class ProjectsWorkerDevelopmentCatalog
{
    /// <summary>Observes this worker's actual executable and shared isolated SDK authority.</summary>
    internal static bool AddProjectsDevelopmentWorkerCatalog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrWhiteSpace(builder.Configuration["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"])) return false;
#if SUPPORTED_PROJECT_READ_SOURCE
        if (!builder.Environment.IsDevelopment()
            || !bool.TryParse(builder.Configuration["Projects:SupportedReads:Enabled"], out bool selected) || !selected)
            throw new InvalidOperationException("Worker catalog bootstrap requires the explicit private Development supported-read profile.");
        builder.Services.AddEventStoreHttpHostSecurity();
        builder.Services.AddEventStoreDevelopmentRouteCatalogBootstrap(builder.Configuration, async (_, token) =>
        {
            var actual = await DevelopmentRouteCatalogInputs.CaptureAsync(builder.Configuration,
                ProjectsWorkersModule.AppId, [], [], [], [], [], token).ConfigureAwait(false);
            var executable = typeof(ProjectsWorkerDevelopmentCatalog).Assembly;
            return actual with
            {
                ExecutableOwners = new Dictionary<string, string>
                {
                    [executable.FullName!] = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(executable.Location))),
                },
            };
        });
        return true;
#else
        throw new InvalidOperationException("Private worker catalog enrollment requires local source references.");
#endif
    }

    /// <summary>Keeps only the actual Dapr channel eligible for subscription discovery and event callbacks.</summary>
    internal static IEndpointRouteBuilder UseProjectsDevelopmentWorkerSecurity(this WebApplication app, bool selected)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!selected) return app;
#if SUPPORTED_PROJECT_READ_SOURCE
        app.UseAuthentication();
        app.UseEventStoreHttpHostSecurity();
        app.UseAuthorization();
        return app.MapGroup(string.Empty).RequireAuthorization(EventStoreWorkloadAuthenticationDefaults.SidecarChannelPolicy);
#else
        throw new InvalidOperationException("Private worker catalog enrollment requires local source references.");
#endif
    }
}
