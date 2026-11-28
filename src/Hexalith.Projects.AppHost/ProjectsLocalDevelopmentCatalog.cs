namespace Hexalith.Projects.AppHost;

using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using global::Aspire.Hosting.Dotnet;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Hexalith.EventStore.Aspire;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.Extensions.Configuration;

/// <summary>Enrolls the ordinary local topology using the owner's retained catalog custody.</summary>
internal static class ProjectsLocalDevelopmentCatalog
{
    internal static (DevelopmentRouteCatalogComposition Catalog, string RunDirectory, string CertificateDirectory) Add(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<DotnetProjectResource> gateway,
        IResourceBuilder<DotnetProjectResource> tenants,
        IResourceBuilder<DotnetProjectResource> projects,
        string redisHost,
        string resourcesPath,
        string daprNamespace = "default",
        string? qualificationDirectory = null)
    {
#if SUPPORTED_PROJECT_READ_SOURCE
        var custody = builder.Configuration.GetSection("Projects:DevelopmentRouteCatalog")
            .Get<DevelopmentRouteCatalogCompositionOptions>()
            ?? throw new InvalidOperationException("Projects:DevelopmentRouteCatalog requires owner custody for the selected Redis backend.");
        string projectOutput = OwnerOutput(projects);
        Assembly projectOwner = Load(Path.Combine(projectOutput, "Hexalith.Projects.Contracts.dll"));
        Assembly projectAdapters = Load(Path.Combine(projectOutput, "Hexalith.Projects.EventStoreExtensions.dll"));
        string tenantOutput = OwnerOutput(tenants);
        Assembly tenantOwner = Load(Path.Combine(tenantOutput, "Hexalith.Tenants.Contracts.dll"));
        Assembly tenantAdapters = Load(Path.Combine(tenantOutput, "Hexalith.Tenants.Server.dll"));

        _ = projects.WithEnvironment("Projects__DevelopmentRouteCatalog__Enabled", "true");
        var setupStore = builder.AddDaprComponent("projects-conversation-start-setup", "state.redis")
            .WithMetadata("redisHost", redisHost)
            .WithMetadata("keyPrefix", "name");
        var projectsSidecar = projects.Resource.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;
        _ = builder.CreateResourceBuilder(projectsSidecar).WithReference(setupStore);
        ConfigureWorkloadBinding(builder, gateway, projects, tenants);
        string[] publicQueries = CompiledRouteCatalogDeclarations.Read([projectOwner])
            .Where(route => route.Kind == "query" && route.Execution?.RequiresDelegatedContext != true)
            .Select(route => route.MessageType!).Order(StringComparer.Ordinal).ToArray();
        for (int index = 0; index < publicQueries.Length; index++)
        {
            _ = gateway.WithEnvironment($"Authentication__QueryAuthority__RequiredQueries__projects__{index}", publicQueries[index]);
        }
        DevelopmentRouteCatalogInvocation<DotnetProjectResource>[] Calls(params string[] methods)
            => methods.Select(method => new DevelopmentRouteCatalogInvocation<DotnetProjectResource>(gateway, method, ["POST"])).ToArray();
        List<DevelopmentRouteCatalogInvocation<DotnetProjectResource>> gatewayCalls =
        [
            new(projects, "api/v1/commands", ["POST"]),
            new(projects, "api/v1/queries/owner-authority/workload", ["POST"]),
            new(projects, "api/v1/streams/read/workload", ["POST"]),
            new(tenants, "api/v1/streams/read/workload", ["POST"]),
        ];
        if (qualificationDirectory is not null)
        {
            gatewayCalls.Add(new(tenants, "api/v1/commands/bound/workload", ["POST"]));
        }
        DevelopmentRouteCatalogReceiver<DotnetProjectResource>[] receivers =
        [
            new(gateway, gatewayCalls) { TechnicalHttp = true },
            new(tenants, Calls("process", "replay-state", "query", "project", "admin/operational-index-metadata")),
            new(projects, Calls("process", "replay-state", "query", "project", "project/v2", "project/v2/reconcile", "project/rebuild/v1", "project/rebuild/part/v1", "admin/operational-index-metadata")),
        ];

        DirectoryInfo componentDirectory = Directory.CreateTempSubdirectory("projects-catalog-component-");
        DevelopmentRouteCatalogComposition? catalog = null;
        try
        {
            string componentPath = Path.Combine(componentDirectory.FullName, "statestore.json");
            File.WriteAllBytes(componentPath, JsonSerializer.SerializeToUtf8Bytes(new
            {
                kind = "Component",
                spec = new
                {
                    type = "state.redis",
                    metadata = new[] { new { name = "redisHost", value = redisHost } },
                },
            }));
            catalog = builder.AddEventStoreDevelopmentRouteCatalog(gateway,
                [new(projects, projectOwner, projectAdapters), new(tenants, tenantOwner, tenantAdapters)],
                receivers, componentPath, custody, daprNamespace);
            var infrastructure = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder,
                Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../..")), Path.Combine(resourcesPath, "sentry.yaml"), daprNamespace);
            return (catalog, infrastructure.RunDirectory, infrastructure.CertificateDirectory);
        }
        catch
        {
            if (catalog is not null) catalog.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }
        finally
        {
            componentDirectory.Delete(recursive: true);
        }
#else
        throw new InvalidOperationException("The ordinary local topology composes its route catalog only from a local Debug source build "
            + "(UseHexalithProjectReferences=true, without CI=true), so package mode cannot start it.");
#endif
    }

#if SUPPORTED_PROJECT_READ_SOURCE
    private static void ConfigureWorkloadBinding(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<DotnetProjectResource> gateway,
        IResourceBuilder<DotnetProjectResource> projects,
        IResourceBuilder<DotnetProjectResource> tenants)
    {
        const string prefix = "Projects:DevelopmentRouteCatalog:Workload:";
        string GetRequired(string key) => builder.Configuration[prefix + key] is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Missing {prefix}{key} for the local workload binding authority.");
        string authority = GetRequired("Authority");
        string issuer = GetRequired("Issuer");
        string tokenEndpoint = GetRequired("TokenEndpoint");
        (string Workload, IResourceBuilder<DotnetProjectResource> Resource, string ClientId, string ClientSecret)[] clients =
        [
            ("eventstore", gateway, GetRequired("GatewayClientId"), GetRequired("GatewayClientSecret")),
            ("projects", projects, GetRequired("ProjectsClientId"), GetRequired("ProjectsClientSecret")),
            ("tenants", tenants, GetRequired("TenantsClientId"), GetRequired("TenantsClientSecret")),
        ];
        string receiptKey = GetRequired("HumanAuthorityReceiptSigningKey");
        var receiptSigningKey = builder.AddParameter("projects-catalog-human-authority-receipt-key", () => receiptKey, secret: true);
        _ = gateway.WithEnvironment("Authentication__HumanAuthorityReceipt__SigningKey", receiptSigningKey);
        _ = projects.WithEnvironment("Authentication__HumanAuthorityReceipt__SigningKey", receiptSigningKey);
        foreach (var resource in new[] { gateway, projects, tenants })
        {
            _ = resource.WithEnvironment("Authentication__WorkloadJwtBearer__Authority", authority)
                .WithEnvironment("Authentication__WorkloadJwtBearer__Issuer", issuer)
                .WithEnvironment("Authentication__WorkloadJwtBearer__Audience", resource.Resource.Name)
                .WithEnvironment("Authentication__WorkloadJwtBearer__AllowedAlgorithms__0", "RS256")
                .WithEnvironment("Authentication__WorkloadJwtBearer__RequireHttpsMetadata", "true");
        }
        foreach (var client in clients)
        {
            string secret = client.ClientSecret;
            string parameter = client.Workload == "eventstore" ? "projects-catalog-gateway-client-secret" : $"projects-catalog-{client.Workload}-client-secret";
            _ = client.Resource.WithEnvironment("Authentication__WorkloadIssuer__Workload", client.Workload)
                .WithEnvironment("Authentication__WorkloadIssuer__ClientId", client.ClientId)
                .WithEnvironment("Authentication__WorkloadIssuer__ClientSecret", builder.AddParameter(parameter, () => secret, secret: true))
                .WithEnvironment("Authentication__WorkloadIssuer__TokenEndpoint", tokenEndpoint)
                .WithEnvironment("Authentication__WorkloadIssuer__EnableResourceBindingRequests", "true");
        }

        // Owner source and owner-authority proofs use run-scoped keys. EventStore trusts only their public halves,
        // for the same exact callers and owner domains as the isolated compatibility profile.
        _ = gateway.WithEnvironment("Authentication__DaprInternal__AllowedCallers__0", "projects")
            .WithEnvironment("Authentication__DaprInternal__AllowedCallers__1", "tenants");
        foreach ((string workload, IResourceBuilder<DotnetProjectResource> owner, string domains) in new[]
        {
            ("projects", projects, "projects"),
            ("tenants", tenants, "tenants,global-administrators"),
        })
        {
            using RSA key = RSA.Create(2048);
            string privateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey());
            _ = owner.WithEnvironment("Authentication__BoundStreamRead__PrivateKey",
                builder.AddParameter($"projects-catalog-{workload}-owner-key", () => privateKey, secret: true));
            _ = gateway.WithEnvironment("Authentication__BoundStreamRead__PublicKeys__" + workload, Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()))
                .WithEnvironment("Authentication__BoundStreamRead__AllowedDomains__" + workload, domains);
        }
    }

    private static string OwnerOutput(IResourceBuilder<DotnetProjectResource> resource)
    {
        string projectPath = resource.Resource.GetProjectMetadata().ProjectPath;
        string projectName = Path.GetFileNameWithoutExtension(projectPath);
        string executableName = projectName + ".dll";
        var appHostOutput = new DirectoryInfo(AppContext.BaseDirectory);
        // An artifacts layout (<artifacts>/bin/<project>/<config>) selects the owner from the same tree, including
        // when a test assembly composes this model; the conventional Debug output remains the fallback.
        DirectoryInfo? artifactsBin = appHostOutput.Parent?.Parent;
        string? artifactsPath = artifactsBin is null ? null
            : Path.Combine(artifactsBin.FullName, projectName, appHostOutput.Name, executableName);
        string path = artifactsPath is not null
            && (appHostOutput.Parent!.Name == "Hexalith.Projects.AppHost" || (artifactsBin!.Name == "bin" && File.Exists(artifactsPath)))
            ? artifactsPath
            : Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", "Debug", "net11.0", executableName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("Build the selected Debug source owner before composing its catalog: " + path);
        }

        return Path.GetDirectoryName(path)!;
    }

    private static Assembly Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("Build the selected Debug source owner before composing its catalog: " + path);
        }

        Assembly loaded = Assembly.LoadFrom(path);
        if (!System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(loaded.Location)).AsSpan()
            .SequenceEqual(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))))
        {
            throw new InvalidOperationException("The loaded owner differs from the selected executable artifact.");
        }

        return loaded;
    }
#endif
}
