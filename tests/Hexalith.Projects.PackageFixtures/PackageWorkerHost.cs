namespace Hexalith.Projects.PackageFixtures;

using System.Security.Cryptography;

using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Projects.ServiceDefaults;
using Hexalith.Projects.Workers;

/// <summary>Test-only package host for the unchanged production worker handlers and SDK channel boundary.</summary>
internal static class PackageWorkerHost
{
    internal static async Task RunAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args: ["--preventHostingStartup=true"]);
        if (!builder.Environment.IsDevelopment() || !builder.Configuration.GetValue<bool>("Projects:PackageFixtures:Enabled")
            || string.IsNullOrWhiteSpace(builder.Configuration["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"]))
            throw new InvalidOperationException("The package worker requires explicit private Development fixture inputs.");
        builder.AddServiceDefaults();
        builder.Services.AddProjectsTenantEventWorkers();
        builder.Services.AddEventStoreHttpHostSecurity();
        builder.Services.AddEventStoreDevelopmentRouteCatalogBootstrap(builder.Configuration, async (_, token) =>
        {
            var actual = await DevelopmentRouteCatalogInputs.CaptureAsync(builder.Configuration,
                ProjectsWorkersModule.AppId, [], [], [], [], [], token).ConfigureAwait(false);
            var executable = typeof(PackageWorkerHost).Assembly;
            return actual with
            {
                ExecutableOwners = new Dictionary<string, string>
                {
                    [executable.FullName!] = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(executable.Location))),
                },
            };
        });
        WebApplication app = builder.Build();
        app.UseCloudEvents();
        app.UseAuthentication();
        app.UseEventStoreHttpHostSecurity();
        app.UseAuthorization();
        var subscriptions = app.MapGroup(string.Empty).RequireAuthorization(EventStoreWorkloadAuthenticationDefaults.SidecarChannelPolicy);
        subscriptions.MapSubscribeHandler();
        subscriptions.MapProjectsTenantEventWorkerEndpoints();
        app.MapDefaultEndpoints();
        await app.RunAsync().ConfigureAwait(false);
    }
}
