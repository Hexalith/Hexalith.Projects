namespace Hexalith.Projects.AppHost;

using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using global::Aspire.Hosting.Dotnet;

using Hexalith.EventStore.Aspire;

/// <summary>Enrolls the disposable read lane through canonical SDK catalogs and Platform peer identity.</summary>
internal static class SupportedProjectReadDevelopmentInfrastructure
{
    /// <summary>Composes real owners after the six application sidecars have been declared.</summary>
    internal static DevelopmentRouteCatalogComposition Add(IDistributedApplicationBuilder builder,
        IResourceBuilder<DotnetProjectResource> gateway, IResourceBuilder<DotnetProjectResource> tenants,
        IResourceBuilder<DotnetProjectResource> projects)
    {
#if SUPPORTED_PROJECT_READ_SOURCE
        foreach (string key in new[] { "TenantsSecret", "TenantOwnerPrivateKey", "TenantOwnerPublicKey", "BootstrapPassword", "FoldersSecret", "FolderOwnerPrivateKey", "FolderOwnerPublicKey" })
            if (string.IsNullOrWhiteSpace(builder.Configuration["Projects:SupportedReads:" + key]))
                throw new InvalidOperationException("The actual bound Tenants bootstrap requires private input: " + key);
        string isolation = builder.Configuration["Projects:SupportedReads:IsolationId"]
            ?? throw new InvalidOperationException("Missing private read-lane ownership.");
        string privateDirectory = Path.GetDirectoryName(Path.GetFullPath(builder.Configuration["Projects:SupportedReads:CertificatePath"]!))!;
        string root = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../.."));
        string sentryConfiguration = Path.Combine(root, "src", "Hexalith.Projects.AppHost", "DaprComponents", "sentry.yaml");
        builder.Configuration["Dapr:Mtls:CertificateDirectory"] = Path.Combine(privateDirectory, "mtls");
        var owned = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder, root, sentryConfiguration, isolation);
        WriteReceipt(privateDirectory, "mtls-plane.json", new
        {
            isolationId = isolation, runDirectory = owned.RunDirectory, certificateDirectory = owned.CertificateDirectory,
        });
        foreach (ContainerResource resource in builder.Resources.OfType<ContainerResource>()
            .Where(resource => resource.Name is "dapr-sentry" or "dapr-placement-mtls" or "dapr-scheduler-mtls"))
        {
            _ = builder.CreateResourceBuilder(resource).WithContainerRuntimeArgs(
                "--label", "hexalith.supported-reads=" + isolation);
        }

        IResourceBuilder<DotnetProjectResource> folders = builder.CreateResourceBuilder(builder.Resources.OfType<DotnetProjectResource>()
            .Single(resource => resource.Name == "folders"));
        IResourceBuilder<DotnetProjectResource> projectsWorkers = builder.CreateResourceBuilder(builder.Resources.OfType<DotnetProjectResource>()
            .Single(resource => resource.Name == "projects-workers"));
        IResourceBuilder<DotnetProjectResource> foldersWorkers = builder.CreateResourceBuilder(builder.Resources.OfType<DotnetProjectResource>()
            .Single(resource => resource.Name == "folders-workers"));
        DevelopmentRouteCatalogOwner<DotnetProjectResource>[] catalogOwners = LoadCatalogOwners(projects, tenants, folders);
        string digestKey = builder.Configuration["Projects:SupportedReads:CatalogDigestKey"]
            ?? throw new InvalidOperationException("The fresh disposable backend requires its private catalog digest key.");
        var options = new DevelopmentRouteCatalogCompositionOptions(Path.Combine(privateDirectory, "catalog-custody"),
            "readscatalog", isolation, "fixture-v1", new Dictionary<string, string> { ["fixture-v1"] = digestKey }, "fixture-v1");
        // This component is the exact literal backend already selected for this newly allocated fixture.
        string statePath = Path.Combine(privateDirectory, "catalog-statestore.json");
        WriteReceipt(privateDirectory, Path.GetFileName(statePath), new
        {
            apiVersion = "dapr.io/v1alpha1", kind = "Component", metadata = new { name = "statestore" },
            spec = new { type = "state.redis", version = "v1", metadata = new[]
            {
                new { name = "redisHost", value = builder.Configuration["Dapr:RedisHost"]! },
                new { name = "actorStateStore", value = "true" }, new { name = "keyPrefix", value = "none" },
            } },
        });
        DevelopmentRouteCatalogInvocation<DotnetProjectResource>[] DomainCalls(params string[] methods) => methods.Select(method =>
            new DevelopmentRouteCatalogInvocation<DotnetProjectResource>(gateway, method, ["POST"])).ToArray();
        DevelopmentRouteCatalogReceiver<DotnetProjectResource>[] receivers =
        [
            new(gateway,
            [
                new(tenants, "api/v1/commands/bound/workload", ["POST"]),
                new(tenants, "api/v1/streams/read/workload", ["POST"]),
                new(projects, "api/v1/queries/owner-authority/workload", ["POST"]),
                new(projects, "api/v1/streams/read/workload", ["POST"]),
                new(projects, "api/v1/commands", ["POST"]),
                new(folders, "api/v1/commands", ["POST"]),
                new(folders, "api/v1/streams/read/workload", ["POST"]),
                new(folders, "api/v1/streams/scan/workload", ["POST"]),
                new(foldersWorkers, "api/v1/streams/read/workload", ["POST"]),
                new(foldersWorkers, "api/v1/streams/scan/workload", ["POST"]),
            ]) { TechnicalHttp = true },
            new(tenants, DomainCalls("process", "replay-state", "query", "project", "admin/operational-index-metadata")),
            new(projects, DomainCalls("process", "replay-state", "query", "project", "project/v2", "project/v2/reconcile", "project/rebuild/v1", "project/rebuild/part/v1", "admin/operational-index-metadata")),
            new(folders, DomainCalls("process", "query", "project", "project/v2", "admin/operational-index-metadata")),
            new(projectsWorkers, []), new(foldersWorkers, []),
        ];
        if (builder.Configuration["Projects:SupportedReads:InvocationDirectory"] is { Length: > 0 } invocationDirectory)
        {
            string expected = Path.Combine(privateDirectory, "catalog-invocations");
            if (!Path.IsPathFullyQualified(invocationDirectory) || Path.GetFullPath(invocationDirectory) != expected
                || !Directory.Exists(expected) || new DirectoryInfo(expected).LinkTarget is not null)
                throw new InvalidOperationException("Catalog fixture invocations require the exact launcher-owned private directory.");
            _ = gateway.WithEnvironment("EventStore__Testing__CatalogInvocation__Enabled", "true")
                .WithEnvironment("EventStore__Testing__CatalogInvocation__Directory", expected)
                .WithEnvironment("EventStore__Testing__CatalogInvocation__MetadataAppIds__0", "projects")
                .WithEnvironment("EventStore__Testing__CatalogInvocation__MetadataAppIds__1", "folders")
                .WithEnvironment("EventStore__Testing__CatalogInvocation__ProjectionAppId", "projects");
        }
        _ = projects.WithEnvironment("Projects__SupportedReads__Enabled", "true");
        _ = projectsWorkers.WithEnvironment("Projects__SupportedReads__Enabled", "true");
        DevelopmentRouteCatalogComposition catalog = builder.AddEventStoreDevelopmentRouteCatalog(gateway,
            catalogOwners,
            receivers, statePath, options, isolation);
        builder.AddEventStoreDevelopmentSqliteStartupOrdering(tenants, gateway, projects, projectsWorkers, folders, foldersWorkers);
        _ = tenants.WithEnvironment("Tenants__BootstrapBoundWorkloadReceiptPath", Path.Combine(catalog.Directory, "tenants.bootstrap.json"));
        _ = tenants.WithEnvironment("EventStore__Testing__OwnerSourceRead__Enabled", "true")
            .WithEnvironment("EventStore__Testing__OwnerSourceRead__Directory", builder.Configuration["Projects:SupportedReads:OwnerSourceReadDirectory"]!)
            .WithEnvironment("EventStore__Testing__OwnerSourceRead__Mode", "administrator-bootstrap")
            .WithEnvironment("EventStore__Testing__OwnerSourceRead__GatewayAppId", gateway.Resource.Name);
        WriteReceipt(privateDirectory, "catalog-plane.json", new
        {
            isolationId = isolation, directory = catalog.Directory, rootDigest = catalog.RootDigest,
            custodyDirectory = options.CustodyDirectory,
        });
        return catalog;
#else
        throw new InvalidOperationException("The private read lane requires local source-built SDK owners.");
#endif
    }

#if SUPPORTED_PROJECT_READ_SOURCE
    /// <summary>Loads the compiled route-catalog owner and adapter assemblies for the distributed fixture owners.</summary>
    /// <param name="projects">The Projects server resource.</param>
    /// <param name="tenants">The Tenants server resource.</param>
    /// <param name="folders">The Folders server resource.</param>
    /// <returns>The enrolled route-catalog owner descriptors for Projects, Tenants, and Folders.</returns>
    internal static DevelopmentRouteCatalogOwner<DotnetProjectResource>[] LoadCatalogOwners(
        IResourceBuilder<DotnetProjectResource> projects,
        IResourceBuilder<DotnetProjectResource> tenants,
        IResourceBuilder<DotnetProjectResource> folders)
    {
        Assembly projectOwner = Load(projects.Resource, "Hexalith.Projects.Contracts.dll");
        Assembly projectAdapters = Load(projects.Resource, "Hexalith.Projects.EventStoreExtensions.dll");
        Assembly folderOwner = Load(folders.Resource, "Hexalith.Folders.Contracts.dll");
        Assembly folderAdapters = typeof(Hexalith.Folders.EventStore.FoldersCatalogAdapterSource).Assembly;
        Assembly tenantOwner = Load(tenants.Resource, "Hexalith.Tenants.Contracts.dll");
        Assembly tenantAdapters = Load(tenants.Resource, "Hexalith.Tenants.Server.dll");
        return
        [
            new(projects, projectOwner, projectAdapters),
            new(tenants, tenantOwner, tenantAdapters),
            new(folders, folderOwner, folderAdapters),
        ];
    }

    private static Assembly Load(DotnetProjectResource project, string artifact)
    {
        string projectPath = project.GetProjectMetadata().ProjectPath;
        string directory = Path.GetDirectoryName(projectPath)!;
        var currentOutput = new DirectoryInfo(AppContext.BaseDirectory);
        DirectoryInfo? artifactsBin = currentOutput.Parent?.Parent;
        string? artifactsPath = artifactsBin?.Name == "bin"
            ? Path.Combine(artifactsBin.FullName, Path.GetFileNameWithoutExtension(projectPath), currentOutput.Name, artifact)
            : null;
        string path = artifactsPath is not null && File.Exists(artifactsPath)
            ? artifactsPath
            : Path.Combine(directory, "bin", "Debug", "net11.0", artifact);

        if (!File.Exists(path)) throw new InvalidOperationException("Build the selected Debug/source owner before composing its catalog: " + path);
        Assembly loaded = Assembly.LoadFrom(path);
        if (!SHA256.HashData(File.ReadAllBytes(loaded.Location)).AsSpan().SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))))
            throw new InvalidOperationException("The loaded owner differs from the selected executable artifact.");
        return loaded;
    }

    private static void WriteReceipt(string directory, string file, object value)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using var output = new FileStream(Path.Combine(directory, file), options);
        JsonSerializer.Serialize(output, value, JsonSerializerOptions.Web);
    }
#endif
}
