namespace Hexalith.Projects.AppHost;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.EventStore.Aspire;
using Hexalith.Projects.Aspire;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>Composes the explicitly isolated, synthetic supported-read qualification topology.</summary>
public static class SupportedProjectReadFixtureProfile
{
    /// <summary>The explicit opt-in configuration key.</summary>
    public const string EnabledConfigurationKey = "Projects:SupportedReads:Enabled";
    /// <summary>The private, launcher-owned configuration file.</summary>
    public const string ConfigurationFileKey = "Projects:SupportedReads:ConfigurationFile";
    private const string Prefix = "Projects:SupportedReads:";

    /// <summary>Returns whether the profile was explicitly selected.</summary>
    public static bool IsEnabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return string.Equals(configuration[EnabledConfigurationKey], "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration[EnabledConfigurationKey], "1", StringComparison.Ordinal);
    }

    /// <summary>Loads and validates launcher-owned inputs before normal resources are composed.</summary>
    public static void Configure(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (!IsEnabled(builder.Configuration)) return;
#if !DEBUG || !SUPPORTED_PROJECT_READ_SOURCE
        if (IsEnabled(builder.Configuration)) throw new InvalidOperationException("Supported-read fixtures require local Debug source references.");
#endif
        if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Supported-read fixtures require Development.");
        if (ProjectsLiveE2EFixtureProfile.IsEnabled(builder.Configuration)
            || string.Equals(builder.Configuration["EnableKeycloak"], "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(builder.Configuration["KeycloakPersistent"], "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Conflicting fixture or persistent security configuration cannot be overridden by fixture inputs.");
        string path = Path.GetFullPath(Required(builder.Configuration, ConfigurationFileKey));
        if (!File.Exists(path)) throw new InvalidOperationException("The private fixture configuration is missing.");
        if (!OperatingSystem.IsWindows()
            && (File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite)) != 0)
            throw new InvalidOperationException("Fixture configuration must be private to its owner.");
        builder.Configuration.AddJsonFile(path, optional: false, reloadOnChange: false);
        Validate(builder.Configuration);
    }

    /// <summary>Rejects ambiguous profiles, shared dependencies and incomplete synthetic trust.</summary>
    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (ProjectsLiveE2EFixtureProfile.IsEnabled(configuration)
            || !string.Equals(configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration["KeycloakPersistent"], "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Supported-read fixtures cannot share the browser or persistent security profile.");
        string isolation = Required(configuration, Prefix + "IsolationId");
        if (!Regex.IsMatch(isolation, "^reads-[a-z0-9]{12,40}$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException("A unique supported-read namespace is required.");
        foreach ((string key, int reserved) in new[] { ("RedisHost", 6379), ("PlacementHostAddress", 50005), ("SchedulerHostAddress", 50006) })
        {
            string endpoint = Required(configuration, "Dapr:" + key);
            if (!IPEndPoint.TryParse(endpoint, out IPEndPoint? parsed) || !IPAddress.IsLoopback(parsed.Address) || parsed.Port == reserved || parsed.Port == 0)
                throw new InvalidOperationException("Supported-read dependencies must use owned loopback endpoints and dedicated ports.");
        }
        if (!Uri.TryCreate(Required(configuration, Prefix + "UserIssuer"), UriKind.Absolute, out Uri? user)
            || user.Scheme != "https" || !user.IsLoopback)
            throw new InvalidOperationException("The synthetic public issuer must use trusted loopback HTTPS.");
        foreach (string key in new[] { "CertificatePath", "CertificatePassword", "TrustBundle", "EventStoreSecret", "ProjectsSecret", "OwnerPrivateKey", "OwnerPublicKey", "ReceiptKey", "ChannelToken", "Tenant", "ForeignTenant", "DiscoveryConfiguration" })
            _ = Required(configuration, Prefix + key);
        foreach (string key in new[] { "CertificatePath", "TrustBundle" })
            if (!File.Exists(configuration[Prefix + key])) throw new InvalidOperationException("Fixture trust material is missing.");
        if (configuration[Prefix + "Tenant"] == configuration[Prefix + "ForeignTenant"])
            throw new InvalidOperationException("The foreign tenant must be distinct and populated.");
        string discoveryPath = Path.GetFullPath(Required(configuration, Prefix + "DiscoveryConfiguration"));
        string privateDirectory = Path.GetDirectoryName(discoveryPath)!;
        if (!File.Exists(discoveryPath) || privateDirectory != Path.GetDirectoryName(Path.GetFullPath(Required(configuration, Prefix + "CertificatePath"))))
            throw new InvalidOperationException("Discovery configuration must belong to the private fixture directory.");
        using JsonDocument discovery = JsonDocument.Parse(File.ReadAllBytes(discoveryPath));
        JsonElement resolver = discovery.RootElement.GetProperty("spec").GetProperty("nameResolution");
        string registry = resolver.GetProperty("configuration").GetProperty("connectionString").GetString()!;
        if (resolver.GetProperty("component").GetString() != "sqlite" || resolver.GetProperty("version").GetString() != "v1"
            || !Path.IsPathFullyQualified(registry) || Path.GetDirectoryName(registry) != privateDirectory || File.Exists(registry))
            throw new InvalidOperationException("Discovery requires a fresh fixture-private SQLite registry; mDNS is not isolated by NAMESPACE.");
    }

    /// <summary>Adds real Folder hosts, Dapr discovery, platform broker grants and distinct receiver trust.</summary>
    public static void AddResources(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> eventStore,
        IResourceBuilder<ProjectResource> tenants,
        IResourceBuilder<ProjectResource> projects,
        IResourceBuilder<ProjectResource> projectsWorkers,
        string resourcesPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(tenants);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(projectsWorkers);
        Validate(builder.Configuration);
        IConfiguration configuration = builder.Configuration;
        string isolation = Required(configuration, Prefix + "IsolationId");
        string ownerResources = AddNestedOwnerDeadline(configuration);
        string root = ResolveProjectRoot(configuration["HexalithFoldersRoot"] ?? builder.AppHostDirectory);
#if SUPPORTED_PROJECT_READ_SOURCE
        IResourceBuilder<ProjectResource> folders = builder.AddProject<global::Projects.Hexalith_Folders_Server>("folders",
            options => options.ExcludeLaunchProfile = true).WithHttpEndpoint().WithHttpHealthCheck("/health/live");
        IResourceBuilder<ProjectResource> folderWorkers = builder.AddProject<global::Projects.Hexalith_Folders_Workers>("folders-workers",
            options => options.ExcludeLaunchProfile = true).WithHttpEndpoint().WithHttpHealthCheck("/health/live");
#else
        string foldersRoot = configuration["HexalithFoldersRoot"] ?? Path.Combine(root, "references", "Hexalith.Folders");
        IResourceBuilder<ProjectResource> folders = builder.AddProject("folders", Path.Combine(foldersRoot, "src", "Hexalith.Folders.Server", "Hexalith.Folders.Server.csproj")).WithHttpEndpoint();
        IResourceBuilder<ProjectResource> folderWorkers = builder.AddProject("folders-workers", Path.Combine(foldersRoot, "src", "Hexalith.Folders.Workers", "Hexalith.Folders.Workers.csproj")).WithHttpEndpoint();
#endif
        var (state, pubsub) = builder.AddProjectsSharedDaprComponents(Required(configuration, "Dapr:RedisHost"));
        IResourceBuilder<ParameterResource> channel = Secret("channel", "ChannelToken");
        IResourceBuilder<ParameterResource> receipt = Secret("receipt", "ReceiptKey");
        IResourceBuilder<ParameterResource> certificatePassword = Secret("certificate-password", "CertificatePassword");
        IResourceBuilder<ParameterResource> eventStoreSecret = Secret("gateway-secret", "EventStoreSecret");
        IResourceBuilder<ParameterResource> projectsSecret = Secret("projects-secret", "ProjectsSecret");
        IResourceBuilder<ParameterResource> ownerKey = Secret("owner-key", "OwnerPrivateKey");
#if SUPPORTED_PROJECT_READ_SOURCE
        IResourceBuilder<ProjectResource> authority = builder.AddProject<global::Projects.Hexalith_Projects_ReadAuthority>("supported-read-authority")
#else
        IResourceBuilder<ProjectResource> authority = builder.AddProject("supported-read-authority", Path.Combine(root, "tests", "Hexalith.Projects.ReadAuthority", "Hexalith.Projects.ReadAuthority.csproj"))
#endif
            .WithHttpsEndpoint(name: "https")
            .WithHttpEndpoint(name: "http")
            .WithHttpHealthCheck("/alive")
            .WithEnvironment("Kestrel__Certificates__Default__Path", Required(configuration, Prefix + "CertificatePath"))
            .WithEnvironment("Kestrel__Certificates__Default__Password", certificatePassword);
        ReferenceExpression issuer = ReferenceExpression.Create($"{authority.GetEndpoint("https")}/workload-authority");
        foreach (IResourceBuilder<ProjectResource> resource in new[] { eventStore, tenants, projects, projectsWorkers, folders, folderWorkers })
        {
            string name = resource.Resource.Name;
            _ = resource.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                .WithEnvironment("EventStore__DomainService__AppId", name)
                .WithEnvironment("Authentication__Workload__Audience", name)
                .WithEnvironment("Authentication__Workload__AllowedCallers__0", "eventstore")
                .WithEnvironment("XDG_DATA_HOME", Path.Combine(Path.GetDirectoryName(Required(configuration, Prefix + "CertificatePath"))!, "data-" + name))
                .WithEnvironment("SSL_CERT_FILE", Required(configuration, Prefix + "TrustBundle"))
                .WithEnvironment("NAMESPACE", isolation)
                .WithEnvironment("Authentication__JwtBearer__Authority", Required(configuration, Prefix + "UserIssuer"))
                .WithEnvironment("Authentication__JwtBearer__Issuer", Required(configuration, Prefix + "UserIssuer"))
                .WithEnvironment("Authentication__JwtBearer__Audience", "supported-read-users")
                .WithEnvironment("Authentication__JwtBearer__SigningKey", string.Empty)
                .WithEnvironment("Authentication__JwtBearer__AllowedAlgorithms__0", "RS256")
                .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "true")
                .WithEnvironment("Authentication__JwtBearer__AllowAnonymousDevelopment", "false")
                .WithEnvironment("Authentication__WorkloadJwtBearer__Authority", issuer)
                .WithEnvironment("Authentication__WorkloadJwtBearer__Issuer", issuer)
                .WithEnvironment("Authentication__WorkloadJwtBearer__Audience", name)
                .WithEnvironment("Authentication__WorkloadJwtBearer__AllowedAlgorithms__0", "RS256")
                .WithEnvironment("Authentication__WorkloadJwtBearer__RequireHttpsMetadata", "true")
                .WithEnvironment("Authentication__HumanAuthorityReceipt__SigningKey", receipt);
            _ = resource.WithDaprSidecar(sidecar =>
            {
                _ = sidecar.WithOptions(new DaprSidecarOptions
                {
                    AppId = name, Config = Required(configuration, Prefix + "DiscoveryConfiguration"), ResourcesPaths = [resourcesPath, ownerResources],
                    PlacementHostAddress = Required(configuration, "Dapr:PlacementHostAddress"),
                    SchedulerHostAddress = Required(configuration, "Dapr:SchedulerHostAddress"),
                    EnableAppHealthCheck = true, AppHealthCheckPath = name is "folders" or "folders-workers" ? "/health/live" : "/alive",
                }).WithReference(state).WithReference(pubsub);
                sidecar.Resource.Annotations.Add(new EnvironmentCallbackAnnotation(context => context.EnvironmentVariables["NAMESPACE"] = isolation));
            }).WithEventStoreAppChannelToken(channel);
        }
        _ = authority.WithEnvironment("Authentication__WorkloadAuthority__Enabled", "true")
            .WithEnvironment("Authentication__WorkloadAuthority__Issuer", issuer)
            .WithEnvironment("Authentication__WorkloadAuthority__KeyId", isolation)
            .WithEnvironment("Authentication__WorkloadAuthority__SigningCertificatePath", Required(configuration, Prefix + "CertificatePath"))
            .WithEnvironment("Authentication__WorkloadAuthority__SigningCertificatePassword", certificatePassword);
        _ = eventStore
            .WithEnvironment("Authentication__BoundStreamRead__PublicKeys__projects", Required(configuration, Prefix + "OwnerPublicKey"))
            .WithEnvironment("Authentication__BoundOwnerQuery__AllowedQueries__projects", "Hexalith.Folders.Queries.AuthorizeFolderRead.v1")
            .WithEnvironment("Authentication__QueryAuthority__RequiredQueries__projects__0", "Hexalith.Projects.Queries.ListProjects.v1")
            .WithEnvironment("Authentication__QueryAuthority__RequiredQueries__projects__1", "Hexalith.Projects.Queries.GetProject.v1")
            .WithEnvironment("Authentication__DaprInternal__AllowedCallers__0", "projects")
            .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__AppId", "tenants")
            .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__Domain", "global-administrators")
            .WithEnvironment("EventStore__DomainServices__Registrations__*|folders|v1__AppId", "folders")
            .WithEnvironment("EventStore__DomainServices__Registrations__*|folders|v1__Domain", "folders")
            .WithEnvironment("EventStore__Publisher__TopicOverrides__folders", "folders.events");
        _ = folders.WithEnvironment("Folders__Authentication__Authority", Required(configuration, Prefix + "UserIssuer"))
            .WithEnvironment("Folders__Authentication__ValidIssuer", Required(configuration, Prefix + "UserIssuer"))
            .WithEnvironment("Folders__Authentication__Audience", "supported-read-users")
            .WithEnvironment("Folders__Authentication__RequireHttpsMetadata", "true");
        _ = eventStore.WithEnvironment("Authentication__HumanAuthorityReceipt__SigningKey", receipt);
        _ = projects.WithEnvironment("Authentication__BoundStreamRead__PrivateKey", ownerKey)
            .WithEnvironment("EventStore__OwnerQueries__OperationTimeout", "00:00:03");
        ConfigureClient(eventStore, "eventstore", eventStoreSecret);
        ConfigureClient(projects, "projects", projectsSecret);
        int ordinal = 0;
        foreach (string domain in new[] { "projects", "folders", "tenants" })
            AddGrant(domain, "eventstore", "EventStoreSecret", domain,
                ["domain-service:query", "domain-service:process",
                    "domain-service:project", "domain-service:metadata",
                    "domain-service:replay-state"], domain != "tenants");
        AddGrant("owner-gateway", "projects", "ProjectsSecret", "eventstore", ["eventstore:owner-authority-query"], false);
        _ = eventStore.WaitFor(authority);
        _ = tenants.WaitFor(authority);
        _ = projects.WithReference(eventStore).WithReference(tenants).WithReference(folders).WaitFor(eventStore).WaitFor(tenants).WaitFor(authority);
        _ = folders.WithReference(eventStore).WithReference(tenants).WaitFor(tenants);
        _ = projectsWorkers.WithReference(eventStore).WithReference(tenants);
        _ = folderWorkers.WithReference(eventStore).WithReference(tenants);

        IResourceBuilder<ParameterResource> Secret(string name, string key)
            => builder.AddParameter("supported-reads-" + name, () => Required(configuration, Prefix + key), secret: true);
        void ConfigureClient(IResourceBuilder<ProjectResource> resource, string workload, IResourceBuilder<ParameterResource> secret)
        {
            _ = resource.WithEnvironment("Authentication__WorkloadIssuer__Workload", workload)
                .WithEnvironment("Authentication__WorkloadIssuer__ClientId", workload + "-fixture")
                .WithEnvironment("Authentication__WorkloadIssuer__ClientSecret", secret)
                .WithEnvironment("Authentication__WorkloadIssuer__EnableResourceBindingRequests", "true")
                .WithEnvironment("Authentication__WorkloadIssuer__TokenEndpoint", ReferenceExpression.Create($"{issuer}/token"));
        }
        void AddGrant(string id, string workload, string secretKey, string audience, string[] operations, bool queryBindings)
        {
            string prefix = $"Authentication__WorkloadAuthority__Grants__{ordinal++}__";
            _ = authority.WithEnvironment(prefix + "GrantId", isolation + "-" + id)
                .WithEnvironment(prefix + "Revision", "1").WithEnvironment(prefix + "ClientId", workload + "-fixture")
                .WithEnvironment(prefix + "Workload", workload).WithEnvironment(prefix + "Audience", audience)
                .WithEnvironment(prefix + "ClientSecretSha256", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Required(configuration, Prefix + secretKey)))))
                .WithEnvironment(prefix + "AllowUnbound", "true")
                .WithEnvironment(prefix + "AllowQueryAuthorityDigest", queryBindings ? "true" : "false")
                .WithEnvironment(prefix + "NotBefore", DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"))
                .WithEnvironment(prefix + "ExpiresAt", DateTimeOffset.UtcNow.AddHours(2).ToString("O"));
            for (int i = 0; i < operations.Length; i++) _ = authority.WithEnvironment(prefix + $"Operations__{i}", operations[i]);
            if (queryBindings)
            {
                _ = authority.WithEnvironment(prefix + "Bindings__eventstore:bound-tenant__0", Required(configuration, Prefix + "Tenant"))
                    .WithEnvironment(prefix + "Bindings__eventstore:bound-tenant__1", Required(configuration, Prefix + "ForeignTenant"))
                    .WithEnvironment(prefix + "Bindings__eventstore:bound-domain__0", audience);
            }
        }
    }

    private static string AddNestedOwnerDeadline(IConfiguration configuration)
    {
        string directory = Path.Combine(Path.GetDirectoryName(Required(configuration, Prefix + "CertificatePath"))!, "components");
        _ = Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "owner-resiliency.yaml");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            apiVersion = "dapr.io/v1alpha1", kind = "Resiliency",
            metadata = new { name = "supported-reads-owner-deadline" }, scopes = new[] { "eventstore" },
            spec = new
            {
                policies = new
                {
                    timeouts = new { supportedReadsOwnerTimeout = "2s" },
                    retries = new { supportedReadsOwnerNoRetry = new { policy = "constant", duration = "1ms", maxRetries = 0 } },
                },
                targets = new { apps = new { folders = new { timeout = "supportedReadsOwnerTimeout", retry = "supportedReadsOwnerNoRetry" } } },
            },
        }));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return directory;
    }

    private static string Required(IConfiguration configuration, string key)
        => !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]! : throw new InvalidOperationException("Missing supported-read fixture configuration: " + key);

    private static string ResolveProjectRoot(string directory)
    {
        foreach (string start in new[] { directory, Directory.GetCurrentDirectory() })
            for (DirectoryInfo? candidate = new(start); candidate is not null; candidate = candidate.Parent)
                if (File.Exists(Path.Combine(candidate.FullName, "Hexalith.Projects.slnx"))) return candidate.FullName;
        throw new DirectoryNotFoundException("The supported-read profile requires the owning Projects source checkout.");
    }
}
