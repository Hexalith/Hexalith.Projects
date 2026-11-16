using Aspire.Hosting.ApplicationModel;
using Hexalith.EventStore.Aspire;
using Hexalith.Projects.PackageFixtures.AppHost;
using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Package fixture AppHost is test-only and requires Development.");
PackageCompatibilityComposition.Configure(builder);
if (!PackageCompatibilityComposition.IsEnabled(builder.Configuration))
    throw new InvalidOperationException("Private package fixture inputs are required.");
string root = builder.Configuration["Projects:PackageFixtures:Root"]!;
string resources = Path.Combine(root, "src", "Hexalith.Projects.AppHost", "DaprComponents");
IResourceBuilder<ProjectResource> Add<T>(string name, string health = "/alive") where T : IProjectMetadata, new()
    => builder.AddProject<PrebuiltMetadata<T>>(name, options => options.ExcludeLaunchProfile = true)
        .WithHttpEndpoint().WithHttpHealthCheck(health).WithEnvironment("DOTNET_WATCH", "false");
var gateway = Add<Projects.Hexalith_EventStore>("eventstore");
var tenants = Add<Projects.Hexalith_Tenants>("tenants");
var projects = Add<Projects.Hexalith_Projects_Server>("projects");
var workers = Add<Projects.Hexalith_Projects_PackageFixtures>("projects-workers")
    .WithArgs("--projects-worker").WithEnvironment("Projects__PackageFixtures__Enabled", "true");
var ui = Add<Projects.Hexalith_Projects_UI>("projects-ui", "/_framework/blazor.web.js");
ConfigureProjectsEventStoreDomainRegistrations(gateway);
bool actualOwners = builder.Configuration["Projects:PackageFixtures:Mode"] == "actual-owner";
if (!actualOwners)
{
    var roles = new Dictionary<string, IResourceBuilder<ProjectResource>>();
    foreach (string role in new[] { "conversations", "folders", "memories", "live-fixtures" })
    {
        var fixture = builder.AddProject<PrebuiltMetadata<Projects.Hexalith_Projects_E2E_Fixtures>>(role,
                options => options.ExcludeLaunchProfile = true)
            .WithEnvironment("FixtureRole", role == "live-fixtures" ? "control" : role)
            .WithEnvironment("DOTNET_WATCH", "false").WithHttpEndpoint().WithHttpHealthCheck("/health");
        roles.Add(role, fixture);
    }
    foreach (string role in new[] { "conversations", "folders", "memories" })
    {
        _ = projects.WithReference(roles[role]).WaitFor(roles[role]);
        _ = roles["live-fixtures"].WithEnvironment("FixtureEndpoints__" + char.ToUpperInvariant(role[0]) + role[1..], roles[role].GetEndpoint("http"))
            .WithReference(roles[role]).WaitFor(roles[role]);
    }
}
else
{
    PackageActualOwnerComposition.AddResources(builder, gateway, tenants, projects, workers, resources);
    var secret = builder.AddParameter("package-ui-secret", () => builder.Configuration["Projects:PackageFixtures:Browser:ClientSecret"]!, secret: true);
    _ = ui.WithEnvironment("Authentication__OpenIdConnect__Authority", builder.Configuration["Projects:PackageFixtures:UserIssuer"]!)
        .WithEnvironment("Authentication__OpenIdConnect__ClientId", "hexalith-projects-ui")
        .WithEnvironment("Authentication__OpenIdConnect__ClientSecret", secret)
        .WithEnvironment("SSL_CERT_FILE", builder.Configuration["Projects:PackageFixtures:TrustBundle"]!);
    var observer = builder.AddProject<PrebuiltMetadata<Projects.Hexalith_Projects_E2E_Fixtures>>("browser-read-observer",
        options => options.ExcludeLaunchProfile = true).WithEnvironment("FixtureRole", "control")
        .WithEnvironment("FixtureStateStore__Enabled", "true").WithHttpEndpoint().WithHttpHealthCheck("/health");
    foreach (string key in new[] { "IsolationId", "Tenant", "ForeignTenant" })
        _ = observer.WithEnvironment("FixtureStateStore__" + key, builder.Configuration["Projects:PackageFixtures:" + key]!);
    foreach (string key in new[] { "PrimaryActor", "ForeignActor" })
        _ = observer.WithEnvironment("FixtureStateStore__" + key, builder.Configuration["Projects:PackageFixtures:Browser:" + key]!);
    _ = observer.WithEnvironment("FixtureStateStore__RedisHost", builder.Configuration["Dapr:RedisHost"]!);
}
_ = gateway.WithReference(tenants).WaitFor(tenants);
_ = ui.WithReference(gateway).WaitFor(gateway).WithReference(projects).WaitFor(projects)
    .WithEnvironment("EventStore__GatewayAddress", gateway.GetEndpoint("http"))
    .WithEnvironment("Projects__BaseAddress", projects.GetEndpoint("http"));
DevelopmentRouteCatalogComposition catalog = actualOwners
    ? PackageActualOwnerInfrastructure.Add(builder, gateway, tenants, projects)
    : PackageCompatibilityComposition.AddResources(builder, gateway, tenants, projects, workers, ui, resources);
await using var catalogCleanup = catalog.ConfigureAwait(false);
DistributedApplication application = builder.Build();
await using var applicationCleanup = application.ConfigureAwait(false);
await application.RunAsync().ConfigureAwait(false);

static void ConfigureProjectsEventStoreDomainRegistrations(IResourceBuilder<ProjectResource> eventStore)
{
    string[] sampleRegistrationKeys =
    [
        "tenant-a|orders|v1",
        "tenant-b|inventory|v1",
        "*|counter|v1",
        "*|greeting|v1"
    ];

    foreach (string key in sampleRegistrationKeys)
    {
        SuppressEventStoreDomainServiceRegistration(eventStore, key);
    }

    // Register the projects domain so EventStore dispatches CreateProject (and the other project
    // commands) to the projects app's /process callback. The wildcard tenant ('*') covers every managed
    // tenant; AppId/Domain 'projects' match the Dapr sidecar app-id and ProjectsServerModule.DomainName.
    _ = eventStore
        .WithEnvironment("EventStore__DomainServices__Registrations__*|projects|v1__AppId", "projects")
        .WithEnvironment("EventStore__DomainServices__Registrations__*|projects|v1__Domain", "projects");

    // Project events are tenant-scoped by convention ('{tenant}.projects.events'), but the
    // projects-workers projection subscriber listens on the static 'projects.events' topic. Override so
    // persisted project events land where the worker subscribes (mirrors the Tenants
    // global-administrators topic-override precedent), otherwise the list projection journal never fills.
    _ = eventStore.WithEnvironment("EventStore__Publisher__TopicOverrides__projects", "projects.events");

    // Tenant aggregates live in the 'system' tenant, so by convention their events publish to
    // 'tenants.events'. The projects-workers tenant-access subscriber listens on 'system.tenants.events'
    // (ProjectsTenantEventSubscription.TopicName). Override so tenant lifecycle/membership events reach
    // the worker and populate the tenant-access projection the authorization gate reads.
    _ = eventStore.WithEnvironment("EventStore__Publisher__TopicOverrides__tenants", "system.tenants.events");

    SuppressEventStoreOperationalIndexMetadataRegistration(eventStore, "system|global-administrators|v1");
}

static void SuppressEventStoreDomainServiceRegistration(IResourceBuilder<ProjectResource> eventStore, string registrationKey)
{
    _ = eventStore
        .WithEnvironment($"EventStore__DomainServices__Registrations__{registrationKey}__AppId", string.Empty)
        .WithEnvironment($"EventStore__DomainServices__Registrations__{registrationKey}__Domain", string.Empty);
}

static void SuppressEventStoreOperationalIndexMetadataRegistration(
    IResourceBuilder<ProjectResource> eventStore,
    string registrationKey)
{
    _ = eventStore.WithEnvironment(
        $"EventStore__DomainServices__Registrations__{registrationKey}__Domain",
        string.Empty);
}
