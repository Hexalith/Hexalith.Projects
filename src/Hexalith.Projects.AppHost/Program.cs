using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dotnet;

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.EventStore.Aspire;
using Hexalith.Projects.AppHost;
using Hexalith.Projects.Aspire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
IsolatedLiveE2EFixtureProfile.Configure(builder);
SupportedProjectReadFixtureProfile.Configure(builder);
const string projectsUiOidcClientId = "hexalith-projects-ui";
const string projectsUiOidcClientSecret = "projects-ui-local-e2e";

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
(string? daprPlacementHostAddress, string? daprSchedulerHostAddress) = AspireDaprLocalServiceEndpoints.Resolve(
    builder.Configuration[AspireDaprLocalServiceEndpoints.PlacementHostAddressKey],
    builder.Configuration[AspireDaprLocalServiceEndpoints.SchedulerHostAddressKey]);
const string qualification = "Projects:DevelopmentRouteCatalog:Qualification:";
const string owner = "projects-disposable-owner";
string? qualificationDirectory = builder.Configuration[qualification + "Directory"];
string? qualificationPassword = null;
string daprNamespace = "default";
HexalithEventStoreSecurityOptions? securityOptions = null;
if (qualificationDirectory is not null)
{
    string selectedTenant = builder.Configuration[qualification + "Tenant"] ?? string.Empty;
    qualificationPassword = builder.Configuration[qualification + "BootstrapPassword"];
    daprNamespace = builder.Configuration[qualification + "Namespace"] ?? string.Empty;
    if (!OperatingSystem.IsLinux() || !builder.Environment.IsDevelopment() || !builder.ExecutionContext.IsRunMode
        || builder.Configuration.GetValue<bool>("KeycloakPersistent") || !builder.Configuration.GetValue<bool>("EnableKeycloak")
        || IsolatedLiveE2EFixtureProfile.IsEnabled(builder.Configuration) || SupportedProjectReadFixtureProfile.IsEnabled(builder.Configuration)
        || ProjectsLiveE2EFixtureProfile.IsEnabled(builder.Configuration)
        || !Path.IsPathFullyQualified(qualificationDirectory) || new DirectoryInfo(qualificationDirectory).LinkTarget is not null
        || (File.GetUnixFileMode(qualificationDirectory) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0
        || string.IsNullOrWhiteSpace(selectedTenant) || string.IsNullOrWhiteSpace(daprNamespace)
        || qualificationPassword is not { Length: >= 32 })
    {
        throw new InvalidOperationException("Ordinary qualification requires private Development custody, local security, and the selected tenant.");
    }
    if (!MatchesQualificationIdentity(builder.Configuration, qualificationDirectory, daprNamespace, selectedTenant, redisHost))
    {
        throw new InvalidOperationException("Ordinary qualification requires its protected identity and exact disposable Redis and catalog custody.");
    }

    string realmDirectory = Path.Combine(qualificationDirectory, "realm");
    Directory.CreateDirectory(realmDirectory);
    string realmFile = "hexalith-realm.json";
    var realm = JsonNode.Parse(File.ReadAllBytes(Path.Combine(builder.AppHostDirectory, "KeycloakRealms", realmFile)))!;
    JsonNode user = realm["users"]!.AsArray().Single(user => user!["id"]!.GetValue<string>() == owner)!;
    user["enabled"] = true;
    user["attributes"]!["tenants"] = new JsonArray(JsonValue.Create(selectedTenant));
    user["attributes"]!["current_tenant"] = new JsonArray(JsonValue.Create(selectedTenant));
    File.WriteAllBytes(Path.Combine(realmDirectory, realmFile), JsonSerializer.SerializeToUtf8Bytes(realm));
    securityOptions = new() { RealmImportPath = realmDirectory };

    accessControlConfigPath = Path.Combine(qualificationDirectory, "discovery.json");
    builder.Configuration["Dapr:Mtls:CertificateDirectory"] = Path.Combine(qualificationDirectory, "mtls");
}
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity(securityOptions);

IResourceBuilder<DotnetProjectResource> eventStore = builder.AddDotnetProject(ProjectsAspireModule.EventStoreAppId, new Projects.Hexalith_EventStore().ProjectPath);
ConfigureProjectsEventStoreDomainRegistrations(eventStore);
if (ProjectsLiveE2EFixtureProfile.IsEnabled(builder.Configuration))
{
    // A parallel browser lane must not serialize every accepted command behind EventStore's
    // tenant-wide projection ETag actor. Use the platform-supported polling mode only for the
    // explicit fixture profile; /project is still exercised asynchronously at this short cadence.
    _ = eventStore.WithEnvironment(
        "EventStore__Projections__Domains__projects__RefreshIntervalMs",
        "250");
}

IResourceBuilder<DotnetProjectResource> tenants = builder.AddDotnetProject(ProjectsAspireModule.TenantsAppId, new Projects.Hexalith_Tenants().ProjectPath);
_ = eventStore
    .WithReference(tenants)
    .WaitFor(tenants);
// Local-dev: widen the tenant-access projection freshness budgets. They are measured from the last
// tenant lifecycle/membership event; with the defaults (mutation 5 min, diagnostic 30 min) the
// projection goes "stale" once a few minutes pass without new tenant events, so project create/list
// start failing (safe-denial / read-model-unavailable) even though access is valid. Production keeps
// the tight defaults; here we set a generous budget so a seeded tenant stays usable.
IResourceBuilder<DotnetProjectResource> projects = builder.AddDotnetProject(ProjectsAspireModule.ProjectsAppId, new Projects.Hexalith_Projects_Server().ProjectPath)
    .WithEnvironment("Projects__TenantAccess__MutationFreshnessBudget", "7.00:00:00")
    .WithEnvironment("Projects__TenantAccess__DiagnosticStalenessBudget", "7.00:00:00");
IResourceBuilder<DotnetProjectResource> projectsUi = builder.AddDotnetProject(ProjectsAspireModule.ProjectsUiAppId, new Projects.Hexalith_Projects_UI().ProjectPath);
// The workers host is a Dapr pub/sub subscriber (tenant + project event projections). It must expose
// an HTTP endpoint so Aspire assigns it a managed port and wires the Dapr sidecar's --app-port; without
// a declared endpoint the sidecar runs with app-port 0, never calls /dapr/subscribe, and no events are
// delivered (the tenant-access + projection-journal read models stay empty). The explicit endpoint also
// avoids the framework default :5000 (which collided with other locally-running apps).
IResourceBuilder<DotnetProjectResource> projectsWorkers = builder.AddDotnetProject(ProjectsAspireModule.ProjectsWorkersAppId, new Projects.Hexalith_Projects_Workers().ProjectPath)
    .WithHttpEndpoint();

_ = ProjectsLiveE2EFixtureProfile.AddResources(builder, projects);

DevelopmentRouteCatalogComposition? supportedReadCatalog = null;
string? daprRunDirectory = null;
string? daprCertificateDirectory = null;
if (IsolatedLiveE2EFixtureProfile.IsEnabled(builder.Configuration))
{
    supportedReadCatalog = IsolatedLiveE2EFixtureProfile.AddResources(builder, eventStore, tenants, projects, projectsWorkers, projectsUi, daprComponentsPath);
}
else if (SupportedProjectReadFixtureProfile.IsEnabled(builder.Configuration))
{
    if (!SupportedProjectBrowserFixtureProfile.IsEnabled(builder.Configuration))
        _ = projectsUi.WithExplicitStart();
    SupportedProjectReadFixtureProfile.AddResources(builder, eventStore, tenants, projects, projectsWorkers, daprComponentsPath);
    supportedReadCatalog = SupportedProjectReadDevelopmentInfrastructure.Add(builder, eventStore, tenants, projects);
}
else
{
    _ = builder.AddHexalithProjects(
        eventStore,
        tenants,
        projects,
        projectsWorkers,
        redisHost,
        accessControlConfigPath,
        daprComponentsPath,
        daprPlacementHostAddress,
        daprSchedulerHostAddress);

    if (builder.ExecutionContext.IsRunMode)
    {
        (supportedReadCatalog, daprRunDirectory, daprCertificateDirectory) = ProjectsLocalDevelopmentCatalog.Add(
            builder, eventStore, tenants, projects, redisHost, daprComponentsPath, daprNamespace, qualificationDirectory);
        if (qualificationDirectory is not null)
        {
            builder.AddEventStoreDevelopmentSqliteStartupOrdering(tenants, eventStore, projects, projectsWorkers);
            // Retain only locators in the private runner workspace. The SDK owns the commit receipts.
            File.WriteAllBytes(Path.Combine(qualificationDirectory, "catalog-plane.json"), JsonSerializer.SerializeToUtf8Bytes(new
            {
                isolationId = daprNamespace,
                directory = supportedReadCatalog.Directory,
                rootDigest = supportedReadCatalog.RootDigest,
                custodyDirectory = builder.Configuration["Projects:DevelopmentRouteCatalog:CustodyDirectory"],
            }));
            File.WriteAllBytes(Path.Combine(qualificationDirectory, "mtls-plane.json"), JsonSerializer.SerializeToUtf8Bytes(new
            {
                isolationId = daprNamespace,
                runDirectory = daprRunDirectory,
                certificateDirectory = daprCertificateDirectory,
            }));
            foreach (ContainerResource resource in builder.Resources.OfType<ContainerResource>()
                .Where(resource => resource.Name is "dapr-sentry" or "dapr-placement-mtls" or "dapr-scheduler-mtls"))
            {
                LabelQualificationContainer(builder.CreateResourceBuilder(resource), daprNamespace);
            }
        }
    }
}

SupportedProjectBrowserFixtureProfile.Add(builder, projectsUi);

if (security is not null)
{
    // Match the local realm's workload credential to the gateway's outbound assertion issuer.
    bool persistWorkloadSecret = bool.TryParse(
        builder.Configuration[HexalithEventStoreSecurityOptions.DefaultPersistentConfigurationKey]?.Trim(),
        out bool keycloakPersistent) && keycloakPersistent;
    IResourceBuilder<ParameterResource> workloadClientSecret = builder.AddParameter(
        "eventstore-workload-client-secret",
        new GenerateParameterDefault
        {
            MinLength = 32,
            Lower = true,
            MinLower = 1,
            Numeric = true,
            MinNumeric = 1,
            Upper = true,
            MinUpper = 1,
            Special = false,
        },
        secret: true,
        persist: persistWorkloadSecret);
    _ = security.Keycloak.WithEnvironment("HEXALITH_EVENTSTORE_WORKLOAD_CLIENT_SECRET", workloadClientSecret);
    if (qualificationPassword is not null)
    {
        LabelQualificationContainer(security.Keycloak, daprNamespace);
        string password = qualificationPassword;
        var bootstrapPassword = builder.AddParameter("projects-catalog-bootstrap-password", () => password, secret: true);
        _ = security.Keycloak.WithEnvironment("HEXALITH_PROJECTS_QUALIFICATION_PASSWORD", bootstrapPassword);
        _ = tenants.WithEnvironment("Tenants__BootstrapGlobalAdminEnabled", "true")
            .WithEnvironment("Tenants__BootstrapGlobalAdminUserId", owner)
            .WithEnvironment("Tenants__BootstrapBoundWorkloadEnabled", "true")
            .WithEnvironment("Tenants__BootstrapBoundWorkloadReceiptPath", Path.Combine(supportedReadCatalog!.Directory, "tenants.bootstrap.json"))
            .WithEnvironment("EventStore__Authentication__Authority", security.RealmUrl)
            .WithEnvironment("EventStore__Authentication__TokenEndpoint", ReferenceExpression.Create($"{security.RealmUrl}/protocol/openid-connect/token"))
            .WithEnvironment("EventStore__Authentication__ClientId", HexalithEventStoreSecurityOptions.DefaultEventStoreClientId)
            .WithEnvironment("EventStore__Authentication__Username", owner)
            .WithEnvironment("EventStore__Authentication__Password", bootstrapPassword);
        _ = eventStore.WithEnvironment("Authentication__BoundCommand__AllowedDomains__tenants", "global-administrators")
            .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__AppId", "tenants")
            .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__Domain", "global-administrators");
    }
    if (daprRunDirectory is null)
    {
        _ = eventStore.WithEventStoreWorkloadClientCredentials("eventstore", workloadClientSecret);
    }

    _ = eventStore.WithJwtBearerSecurity(security);
    _ = tenants.WithJwtBearerSecurity(security);
    _ = projects
        .WithJwtBearerSecurity(security)
        // The project's default launch profile must never override AppHost-provided OIDC.
        .WithEnvironment("Authentication__JwtBearer__AllowAnonymousDevelopment", "false");
    _ = projectsWorkers.WithSecurityDependency(security);
    _ = projectsUi.WithOpenIdConnectSecurity(
        security,
        projectsUiOidcClientId,
        projectsUiOidcClientSecret);
}

_ = projectsUi
    .WithReference(eventStore)
    .WaitFor(eventStore)
    .WithEnvironment("EventStore__GatewayAddress", ReferenceExpression.Create($"{eventStore.GetEndpoint("http")}"))
    .WithReference(projects)
    .WaitFor(projects)
    .WithEnvironment("Projects__BaseAddress", ReferenceExpression.Create($"{projects.GetEndpoint("http")}"));

try
{
    DistributedApplication application = builder.Build();
    await using (application.ConfigureAwait(false))
    {
        await application.RunAsync().ConfigureAwait(false);
    }
}
finally
{
    try
    {
        if (supportedReadCatalog is not null) await supportedReadCatalog.DisposeAsync().ConfigureAwait(false);
    }
    finally
    {
        try
        {
            if (daprCertificateDirectory is not null && Directory.Exists(daprCertificateDirectory))
                Directory.Delete(daprCertificateDirectory, recursive: true);
        }
        finally
        {
            if (daprRunDirectory is not null && Directory.Exists(daprRunDirectory))
                Directory.Delete(daprRunDirectory, recursive: true);
        }
    }
}

static bool MatchesQualificationIdentity(IConfiguration configuration, string directory, string isolation, string tenant, string redisHost)
{
    const string loopback = "127.0.0.1:";
    string marker = Path.Combine(directory, "identity.json");
    string custody = Path.Combine(directory, "catalog-custody");
    if (!OperatingSystem.IsLinux() || Path.GetFullPath(directory) != directory
        || configuration["Projects:SupportedReads:ConfigurationFile"] != marker
        || configuration["Projects:DevelopmentRouteCatalog:CustodyDirectory"] != custody
        || Directory.Exists(custody) || File.Exists(custody) || new DirectoryInfo(custody).LinkTarget is not null
        || !redisHost.StartsWith(loopback, StringComparison.Ordinal)
        || !ushort.TryParse(redisHost.AsSpan(loopback.Length), System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out ushort port) || port is 0 or 6379)
    {
        return false;
    }

    try
    {
        var file = new FileInfo(marker);
        if (!file.Exists || file.LinkTarget is not null || file.Length is 0 or > 4096
            || (File.GetUnixFileMode(marker) & ~(UnixFileMode.UserRead | UnixFileMode.UserWrite)) != 0)
        {
            return false;
        }

        var identity = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(marker));
        return identity is { Count: 4 } && identity.GetValueOrDefault("isolationId") == isolation
            && identity.GetValueOrDefault("tenant") == tenant && identity.GetValueOrDefault("redisHost") == redisHost
            && identity.GetValueOrDefault("custodyDirectory") == custody;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
    {
        return false;
    }
}

static void LabelQualificationContainer<T>(IResourceBuilder<T> resource, string isolation)
    where T : ContainerResource
    => resource.WithContainerRuntimeArgs("--label", "hexalith.supported-reads=" + isolation,
        "--label", "hexalith.projects.ordinary=" + isolation);

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
