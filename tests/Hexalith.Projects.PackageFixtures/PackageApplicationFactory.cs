namespace Hexalith.Projects.PackageFixtures;

using System.Collections.Concurrent;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Testing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>Captures the application through the published protected callback for health waits and teardown.</summary>
internal sealed class PackageApplicationFactory(Type entryPoint, string[] arguments, string privateDirectory)
    : DistributedApplicationFactory(entryPoint, arguments)
{
    private readonly string _browserToken = FixtureInputs.Secret();
    private readonly string _resourceKey = FixtureInputs.Secret();
    private readonly string _otlpKey = FixtureInputs.Secret();
    private readonly string _dashboardKey = FixtureInputs.Secret();
    private readonly ConcurrentDictionary<string, byte> _privateProfiles = new(StringComparer.Ordinal);

    internal DistributedApplication? Application { get; private set; }

    // Mirror Aspire 13.6.5's authenticated EnableDashboard testing defaults. Blank
    // UI/OTLP URLs request dynamic loopback ports; no machine-wide collector is used.
    private Dictionary<string, string?> TelemetrySettings => new()
    {
        ["ASPNETCORE_URLS"] = string.Empty,
        ["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = string.Empty,
        ["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = string.Empty,
        ["ASPIRE_DASHBOARD_DATA_DIRECTORY"] = Path.Combine(privateDirectory, "dashboard"),
        ["ASPIRE_DASHBOARD_PERSISTENCE_MODE"] = "Run",
        ["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"] = "http://127.0.0.1:0",
        ["DcpPublisher:RandomizePorts"] = "true",
        ["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true",
        ["ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS"] = "false",
        ["ASPIRE_INTERACTIVITY_ENABLED"] = "false",
        ["AppHost:SuppressDashboardLoginUrlInStartupSummary"] = "true",
        ["ASPIRE_DASHBOARD_SUPPRESS_BROWSER_TOKEN_IN_OUTPUT"] = "true",
        ["AppHost:BrowserToken"] = _browserToken,
        ["AppHost:ResourceService:AuthMode"] = "ApiKey",
        ["AppHost:ResourceService:ApiKey"] = _resourceKey,
        ["AppHost:DashboardApiKey"] = _dashboardKey,
        ["AppHost:OtlpApiKey"] = _otlpKey,
    };

    /// <inheritdoc/>
    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        base.OnBuilderCreating(applicationOptions, hostOptions);
        applicationOptions.DisableDashboard = false;
        string[] arguments = ReferenceEquals(hostOptions.Args, applicationOptions.Args)
            ? [.. hostOptions.Args ?? []] : [.. hostOptions.Args ?? [], .. applicationOptions.Args ?? []];
        hostOptions.Args = [.. arguments,
            "ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=false",
            "ASPIRE_DASHBOARD_FRONTEND_BROWSERTOKEN=" + _browserToken,
            "ASPIRE_DASHBOARD_RESOURCESERVICE_APIKEY=" + _resourceKey];
        applicationOptions.Args = hostOptions.Args;
        hostOptions.Configuration ??= new();
        hostOptions.Configuration.AddInMemoryCollection(TelemetrySettings);
    }

    /// <inheritdoc/>
    protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
        base.OnBuilding(applicationBuilder);
        applicationBuilder.Configuration.AddInMemoryCollection(TelemetrySettings);
        applicationBuilder.Services.Configure<ResourceNotificationServiceOptions>(
            options => options.DefaultWaitBehavior = WaitBehavior.StopOnResourceUnavailable);
        applicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>((resourceEvent, _) =>
        {
            if (resourceEvent.Resource is IResourceWithEnvironment resource
                && (resource is ProjectResource || resource.Name == "aspire-dashboard")
                && _privateProfiles.TryAdd(resource.Name, 0))
                resource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                    context.EnvironmentVariables["LOCALAPPDATA"] = Path.Combine(privateDirectory, "profiles", resource.Name)));
            return Task.CompletedTask;
        });
    }

    /// <inheritdoc/>
    protected override void OnBuilt(DistributedApplication application)
    {
        base.OnBuilt(application);
        Application = application;
    }
}
