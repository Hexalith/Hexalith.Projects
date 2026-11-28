using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dotnet;
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
IResourceBuilder<DotnetProjectResource> Add<T>(string name, string health = "/alive") where T : IProjectMetadata, new()
    => builder.AddPrebuiltProject(name, new T().ProjectPath)
        .WithHttpHealthCheck(health).WithEnvironment("DOTNET_WATCH", "false");
var gateway = Add<Projects.Hexalith_EventStore>("eventstore");
var tenants = Add<Projects.Hexalith_Tenants>("tenants");
// Keep the seeded authority current throughout a bounded package phase. The recovery
// scenario deliberately uses a short budget to prove denial and authoritative refresh.
string tenantAccessBudget = builder.Configuration["Projects:PackageFixtures:Scenario"] == "tenant-access-recovery"
    ? PackageActualOwnerComposition.TenantAccessRecoveryFreshnessBudget
    : PackageActualOwnerComposition.OrdinaryTenantAccessFreshnessBudget;
var projects = Add<Projects.Hexalith_Projects_Server>("projects")
    .WithEnvironment("Projects__TenantAccess__MutationFreshnessBudget", tenantAccessBudget)
    .WithEnvironment("Projects__TenantAccess__DiagnosticStalenessBudget", tenantAccessBudget);
var workers = Add<Projects.Hexalith_Projects_PackageFixtures>("projects-workers")
    .WithArgs("--projects-worker").WithEnvironment("Projects__PackageFixtures__Enabled", "true");
var ui = Add<Projects.Hexalith_Projects_UI>("projects-ui", "/_framework/blazor.web.js");
ConfigureProjectsEventStoreDomainRegistrations(gateway);
bool actualOwners = builder.Configuration["Projects:PackageFixtures:Mode"] == "actual-owner";
if (!actualOwners)
{
    var roles = new Dictionary<string, IResourceBuilder<DotnetProjectResource>>();
    foreach (string role in new[] { "conversations", "folders", "memories", "live-fixtures" })
    {
        var fixture = builder.AddPrebuiltProject(role, new Projects.Hexalith_Projects_E2E_Fixtures().ProjectPath)
            .WithEnvironment("FixtureRole", role == "live-fixtures" ? "control" : role)
            .WithEnvironment("DOTNET_WATCH", "false").WithHttpHealthCheck("/health");
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
    var observer = builder.AddPrebuiltProject("browser-read-observer", new Projects.Hexalith_Projects_E2E_Fixtures().ProjectPath).WithEnvironment("FixtureRole", "control")
        .WithEnvironment("FixtureStateStore__Enabled", "true").WithHttpHealthCheck("/health");
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
PackageHostingStartupComposition.Add(root, Path.GetDirectoryName(builder.Configuration["Projects:PackageFixtures:CertificatePath"]!)!, gateway, tenants, projects);
if (actualOwners)
    PackageHostingStartupComposition.Add(root, Path.GetDirectoryName(builder.Configuration["Projects:PackageFixtures:CertificatePath"]!)!,
        builder.CreateResourceBuilder(builder.Resources.OfType<DotnetProjectResource>().Single(resource => resource.Name == "folders")));
await using var catalogCleanup = catalog.ConfigureAwait(false);
DistributedApplication application = builder.Build();
await using var applicationCleanup = application.ConfigureAwait(false);
await application.RunAsync().ConfigureAwait(false);

static void ConfigureProjectsEventStoreDomainRegistrations(IResourceBuilder<DotnetProjectResource> eventStore)
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

static void SuppressEventStoreDomainServiceRegistration(IResourceBuilder<DotnetProjectResource> eventStore, string registrationKey)
{
    _ = eventStore
        .WithEnvironment($"EventStore__DomainServices__Registrations__{registrationKey}__AppId", string.Empty)
        .WithEnvironment($"EventStore__DomainServices__Registrations__{registrationKey}__Domain", string.Empty);
}

static void SuppressEventStoreOperationalIndexMetadataRegistration(
    IResourceBuilder<DotnetProjectResource> eventStore,
    string registrationKey)
{
    _ = eventStore.WithEnvironment(
        $"EventStore__DomainServices__Registrations__{registrationKey}__Domain",
        string.Empty);
}
