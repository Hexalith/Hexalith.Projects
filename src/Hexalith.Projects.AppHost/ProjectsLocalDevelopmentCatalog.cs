namespace Hexalith.Projects.AppHost;

using System.Reflection;
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
        string resourcesPath)
    {
#if SUPPORTED_PROJECT_READ_SOURCE
        var custody = builder.Configuration.GetSection("Projects:DevelopmentRouteCatalog")
            .Get<DevelopmentRouteCatalogCompositionOptions>()
            ?? throw new InvalidOperationException("Projects:DevelopmentRouteCatalog requires owner custody for the selected Redis backend.");
        Assembly projectOwner = Load(projects, "Hexalith.Projects.Server.dll");
        Assembly tenantOwner = Load(tenants, "Hexalith.Tenants.Contracts.dll");
        Assembly tenantAdapters = Load(tenants, "Hexalith.Tenants.Server.dll");

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
        DevelopmentRouteCatalogReceiver<DotnetProjectResource>[] receivers =
        [
            new(gateway,
            [
                new(projects, "api/v1/commands", ["POST"]),
                new(projects, "api/v1/queries/owner-authority/workload", ["POST"]),
                new(projects, "api/v1/streams/read/workload", ["POST"]),
            ]) { TechnicalHttp = true },
            new(tenants, Calls("process", "replay-state", "query", "project", "admin/operational-index-metadata")),
            new(projects, Calls("process", "replay-state", "query", "project", "project/v2", "project/rebuild/v1", "admin/operational-index-metadata")),
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
                [new(projects, projectOwner, projectOwner), new(tenants, tenantOwner, tenantAdapters)],
                receivers, componentPath, custody);
            var infrastructure = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder,
                Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../..")), Path.Combine(resourcesPath, "sentry.yaml"));
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
        throw new InvalidOperationException("The local catalog requires the Debug source build of its executable owners.");
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
        string clientId = GetRequired("GatewayClientId");
        string clientSecret = GetRequired("GatewayClientSecret");
        foreach (var resource in new[] { gateway, projects, tenants })
        {
            _ = resource.WithEnvironment("Authentication__WorkloadJwtBearer__Authority", authority)
                .WithEnvironment("Authentication__WorkloadJwtBearer__Issuer", issuer)
                .WithEnvironment("Authentication__WorkloadJwtBearer__Audience", resource.Resource.Name)
                .WithEnvironment("Authentication__WorkloadJwtBearer__AllowedAlgorithms__0", "RS256")
                .WithEnvironment("Authentication__WorkloadJwtBearer__RequireHttpsMetadata", "true");
        }
        _ = gateway.WithEnvironment("Authentication__WorkloadIssuer__Workload", "eventstore")
            .WithEnvironment("Authentication__WorkloadIssuer__ClientId", clientId)
            .WithEnvironment("Authentication__WorkloadIssuer__ClientSecret",
                builder.AddParameter("projects-catalog-gateway-client-secret", () => clientSecret, secret: true))
            .WithEnvironment("Authentication__WorkloadIssuer__TokenEndpoint", tokenEndpoint)
            .WithEnvironment("Authentication__WorkloadIssuer__EnableResourceBindingRequests", "true");
    }

    private static Assembly Load(IResourceBuilder<DotnetProjectResource> resource, string artifact)
    {
        string projectPath = resource.Resource.GetProjectMetadata().ProjectPath;
        var appHostOutput = new DirectoryInfo(AppContext.BaseDirectory);
        string path = appHostOutput.Parent is { Name: "Hexalith.Projects.AppHost", Parent: { } artifactsBin }
            ? Path.Combine(artifactsBin.FullName, Path.GetFileNameWithoutExtension(projectPath), appHostOutput.Name, artifact)
            : Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", "Debug", "net11.0", artifact);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("Build the selected Debug source owner before composing its catalog: " + path);
        }

        return Assembly.LoadFrom(path);
    }
#endif
}
