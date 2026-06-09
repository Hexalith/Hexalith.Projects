using Aspire.Hosting.ApplicationModel;

using Hexalith.Projects.AppHost;
using Hexalith.Projects.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

string daprComponentsPath = ProjectsAppHost.ResolveDaprComponentsPath(
    builder.AppHostDirectory,
    Directory.GetCurrentDirectory());
string accessControlConfigPath = ProjectsAppHost.ResolveDaprConfigPath(
    builder.AppHostDirectory,
    Directory.GetCurrentDirectory(),
    "accesscontrol.yaml");
_ = ProjectsAppHost.ResolveDaprConfigPath(
    builder.AppHostDirectory,
    Directory.GetCurrentDirectory(),
    "resiliency.yaml");
string redisHost = builder.Configuration["Dapr:RedisHost"] ?? ProjectsAspireModule.LocalDaprRedisHost;

IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase))
{
    keycloak = builder.AddKeycloak("keycloak", 8180);
    if (Directory.Exists(Path.Combine(builder.AppHostDirectory, "KeycloakRealms")))
    {
        _ = keycloak.WithRealmImport("./KeycloakRealms");
    }

    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}

IResourceBuilder<ProjectResource> eventStore = builder.AddProject<Projects.Hexalith_EventStore>(ProjectsAspireModule.EventStoreAppId);
ConfigureProjectsEventStoreDomainRegistrations(eventStore);
IResourceBuilder<ProjectResource> tenants = builder.AddProject<Projects.Hexalith_Tenants>(ProjectsAspireModule.TenantsAppId);
_ = eventStore
    .WithReference(tenants)
    .WaitFor(tenants);
// Local-dev: widen the tenant-access projection freshness budgets. They are measured from the last
// tenant lifecycle/membership event; with the defaults (mutation 5 min, diagnostic 30 min) the
// projection goes "stale" once a few minutes pass without new tenant events, so project create/list
// start failing (safe-denial / read-model-unavailable) even though access is valid. Production keeps
// the tight defaults; here we set a generous budget so a seeded tenant stays usable.
IResourceBuilder<ProjectResource> projects = builder.AddProject<Projects.Hexalith_Projects_Server>(ProjectsAspireModule.ProjectsAppId)
    .WithEnvironment("Projects__TenantAccess__MutationFreshnessBudget", "7.00:00:00")
    .WithEnvironment("Projects__TenantAccess__DiagnosticStalenessBudget", "7.00:00:00");
IResourceBuilder<ProjectResource> projectsUi = builder.AddProject<Projects.Hexalith_Projects_UI>(ProjectsAspireModule.ProjectsUiAppId);
// The workers host is a Dapr pub/sub subscriber (tenant + project event projections). It must expose
// an HTTP endpoint so Aspire assigns it a managed port and wires the Dapr sidecar's --app-port; without
// a declared endpoint the sidecar runs with app-port 0, never calls /dapr/subscribe, and no events are
// delivered (the tenant-access + projection-journal read models stay empty). The explicit endpoint also
// avoids the framework default :5000 (which collided with other locally-running apps).
IResourceBuilder<ProjectResource> projectsWorkers = builder.AddProject<Projects.Hexalith_Projects_Workers>(ProjectsAspireModule.ProjectsWorkersAppId)
    .WithHttpEndpoint();

_ = builder.AddHexalithProjects(
    eventStore,
    tenants,
    projects,
    projectsWorkers,
    redisHost,
    accessControlConfigPath,
    daprComponentsPath);

if (keycloak is not null && realmUrl is not null)
{
    ConfigureJwt(eventStore, keycloak, realmUrl);
    ConfigureJwt(tenants, keycloak, realmUrl);
    ConfigureJwt(projects, keycloak, realmUrl);
    ConfigureJwt(projectsUi, keycloak, realmUrl);
    ConfigureJwt(projectsWorkers, keycloak, realmUrl);
}

_ = projectsUi
    .WithReference(projects)
    .WaitFor(projects)
    .WithEnvironment("Projects__BaseAddress", ReferenceExpression.Create($"{projects.GetEndpoint("http")}"));

builder.Build().Run();

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

static void ConfigureJwt(
    IResourceBuilder<ProjectResource> resource,
    IResourceBuilder<KeycloakResource> keycloak,
    ReferenceExpression realmUrl)
{
    _ = resource
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", string.Empty);
}
