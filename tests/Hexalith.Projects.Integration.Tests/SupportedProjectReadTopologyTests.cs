namespace Hexalith.Projects.Integration.Tests;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using global::Aspire.Hosting.Eventing;
using global::Aspire.Hosting.Lifecycle;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Hexalith.EventStore.Aspire;
using Hexalith.Projects.AppHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;
using Shouldly;
using YamlDotNet.Serialization;
using Xunit;

/// <summary>Checks isolation and authentication composition without starting external services.</summary>
public sealed class SupportedProjectReadTopologyTests : IDisposable
{
    private readonly List<string> _directories = [];
    /// <summary>Keeps the existing topology unchanged unless the new lane is explicitly selected.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("yes")]
    public void DisabledProfileDoesNotRequirePrivateConfiguration(string? enabled)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [SupportedProjectReadFixtureProfile.EnabledConfigurationKey] = enabled });
        SupportedProjectReadFixtureProfile.Configure(builder);
        SupportedProjectReadFixtureProfile.IsEnabled(builder.Configuration).ShouldBeFalse();
        builder.Resources.ShouldBeEmpty();
    }

    /// <summary>Rejects shared dependencies and conflicting browser/security profiles before resource mutation.</summary>
    [Theory]
    [InlineData("Dapr:RedisHost", "127.0.0.1:6379")]
    [InlineData("Dapr:PlacementHostAddress", "127.0.0.1:50005")]
    [InlineData("Dapr:SchedulerHostAddress", "127.0.0.1:50006")]
    [InlineData("Dapr:RedisHost", "192.0.2.1:6380")]
    [InlineData("Projects:E2E:LiveFixtures", "true")]
    [InlineData("EnableKeycloak", "true")]
    [InlineData("KeycloakPersistent", "true")]
    [InlineData("Projects:SupportedReads:IsolationId", "default")]
    [InlineData("Projects:SupportedReads:UserIssuer", "http://127.0.0.1:5000/users")]
    [InlineData("Projects:SupportedReads:CatalogDigestKey", "invalid-base64")]
    [InlineData("Projects:SupportedReads:CatalogDigestKey", "c2hvcnQ=")]
    [InlineData("Projects:SupportedReads:TenantsSecret", "")]
    [InlineData("Projects:SupportedReads:TenantOwnerPrivateKey", "")]
    [InlineData("Projects:SupportedReads:TenantOwnerPublicKey", "")]
    [InlineData("Projects:SupportedReads:BootstrapPassword", "")]
    [InlineData("Projects:SupportedReads:InvocationDirectory", "relative/catalog-invocations")]
    public void InvalidConfigurationFailsBeforeComposition(string key, string value)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(ValidInputs()).Build();
        configuration[key] = value;
        Should.Throw<InvalidOperationException>(() => SupportedProjectReadFixtureProfile.Validate(configuration));
    }

    /// <summary>Rejects mDNS and reused discovery files before any sidecar can resolve a foreign host.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DiscoveryMustUseFreshPrivateSqlite(bool reused)
    {
        Dictionary<string, string?> inputs = ValidInputs();
        string path = inputs["Projects:SupportedReads:DiscoveryConfiguration"]!;
        if (reused)
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "registry.sqlite"), "already owned");
        else
            File.WriteAllText(path, File.ReadAllText(path).Replace("sqlite", "mdns", StringComparison.Ordinal));
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(inputs).Build();
        Should.Throw<InvalidOperationException>(() => SupportedProjectReadFixtureProfile.Validate(configuration));
    }

    /// <summary>Private testing transport never accepts a sibling directory, a link or shared IPC permissions.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    [InlineData("symlink")]
    [InlineData("shared")]
    public void InvocationDirectoryMustBeExactAndPrivate(string fault)
    {
        Dictionary<string, string?> inputs = ValidInputs();
        string path = inputs["Projects:SupportedReads:InvocationDirectory"]!;
        if (fault is "missing" or "symlink") Directory.Delete(path);
        if (fault == "foreign") inputs["Projects:SupportedReads:InvocationDirectory"] = Path.GetDirectoryName(path);
        if (fault == "symlink") Directory.CreateSymbolicLink(path, Path.GetDirectoryName(path)!);
        if (fault == "shared" && !OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead);
        if (fault == "shared" && OperatingSystem.IsWindows()) return;
        Should.Throw<InvalidOperationException>(() => SupportedProjectReadFixtureProfile.Validate(new ConfigurationBuilder().AddInMemoryCollection(inputs).Build()));
    }

    /// <summary>Bootstrap source proof has separate exact private owner custody.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    [InlineData("symlink")]
    [InlineData("shared")]
    public void OwnerSourceReadDirectoryMustBeExactAndPrivate(string fault)
    {
        Dictionary<string, string?> inputs = ValidInputs();
        string path = inputs["Projects:SupportedReads:OwnerSourceReadDirectory"]!;
        if (fault is "missing" or "symlink") Directory.Delete(path);
        if (fault == "foreign") inputs["Projects:SupportedReads:OwnerSourceReadDirectory"] = inputs["Projects:SupportedReads:InvocationDirectory"];
        if (fault == "symlink") Directory.CreateSymbolicLink(path, Path.GetDirectoryName(path)!);
        if (fault == "shared" && !OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead);
        if (fault == "shared" && OperatingSystem.IsWindows()) return;
        Should.Throw<InvalidOperationException>(() => SupportedProjectReadFixtureProfile.Validate(new ConfigurationBuilder().AddInMemoryCollection(inputs).Build()));
    }

    /// <summary>Uses real Folder projects and preserves bound trust, command/project grants and secret parameters.</summary>
    [Fact]
    public async Task EnabledProfileComposesRealOwnersAndDistinctTrustAsync()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        Dictionary<string, string?> inputs = ValidInputs();
        inputs["HexalithFoldersRoot"] = Path.Combine(TestRepositoryRoot.Find(), "references", "Hexalith.Folders");
        builder.Configuration.AddInMemoryCollection(inputs);
        string fixtureProject = Path.Combine(TestRepositoryRoot.Find(), "tests", "Hexalith.Projects.E2E.Fixtures", "Hexalith.Projects.E2E.Fixtures.csproj");
        var gateway = builder.AddProject("eventstore", fixtureProject);
        var tenants = builder.AddProject("tenants", fixtureProject);
        var projects = builder.AddProject("projects", fixtureProject);
        var workers = builder.AddProject("projects-workers", fixtureProject);
        string components = Path.Combine(TestRepositoryRoot.Find(), "src", "Hexalith.Projects.AppHost", "DaprComponents");
        SupportedProjectReadFixtureProfile.AddResources(builder, gateway, tenants, projects, workers, components);
        ProjectResource folders = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "folders");
        IDaprComponentResource conversationStore = builder.Resources.OfType<IDaprComponentResource>()
            .Single(resource => resource.Name == Hexalith.Projects.Server.Projections.ConversationStartSetup.ConversationStartSetupProjectionHandler.StoreName);
        conversationStore.Type.ShouldBe("state.redis");
        folders.GetProjectMetadata().ProjectPath.ShouldEndWith("Hexalith.Folders.Server.csproj");
        builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "folders-workers")
            .GetProjectMetadata().ProjectPath.ShouldEndWith("Hexalith.Folders.Workers.csproj");
        builder.Resources.OfType<ParameterResource>().ShouldAllBe(parameter => parameter.Secret);
        IReadOnlyDictionary<string, object> environment = await EnvironmentAsync(gateway.Resource, builder.ExecutionContext);
        environment["EventStore__Testing__NativeSourceReadDiagnostics__Enabled"].ShouldBe("true");
        environment["EventStore__Testing__AtomicNativeMetadataInspection__Enabled"].ShouldBe("true");
        environment["Authentication__JwtBearer__Issuer"].ShouldBe("https://127.0.0.1:54321/users");
        environment["Authentication__WorkloadJwtBearer__Issuer"].ShouldBeOfType<ReferenceExpression>();
        environment["Authentication__WorkloadIssuer__ClientSecret"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        ProjectResource authority = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "supported-read-authority");
        environment = await EnvironmentAsync(authority, builder.ExecutionContext);
        environment["Authentication__WorkloadAuthority__SigningCertificatePassword"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        environment["Authentication__WorkloadAuthority__Grants__0__AllowQueryAuthorityDigest"].ShouldBe("true");
        environment["Authentication__WorkloadAuthority__Grants__0__Operations__1"].ShouldBe("domain-service:process");
        environment["Authentication__WorkloadAuthority__Grants__0__Operations__2"].ShouldBe("domain-service:project");
        environment["Authentication__WorkloadAuthority__Grants__0__Bindings__eventstore:bound-domain__0"].ShouldBe("projects");
        environment["Authentication__WorkloadAuthority__Grants__3__Operations__0"].ShouldBe("eventstore:owner-authority-query");
        environment["Authentication__WorkloadAuthority__Grants__4__Operations__0"].ShouldBe("eventstore:command-submit");
        environment["Authentication__WorkloadAuthority__Grants__4__ClientSecretSha256"].ShouldBe(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(inputs["Projects:SupportedReads:TenantsSecret"]!))));
        environment["Authentication__WorkloadAuthority__Grants__6__Operations__0"].ShouldBe("eventstore:stream-read");
        environment["Authentication__WorkloadAuthority__Grants__6__AllowUnbound"].ShouldBe("false");
        environment["Authentication__WorkloadAuthority__Grants__6__Surface"].ShouldBe("service");
        environment["Authentication__WorkloadAuthority__Grants__6__Bindings__eventstore:bound-tenant__0"].ShouldBe("fixture-tenant");
        environment["Authentication__WorkloadAuthority__Grants__6__Bindings__eventstore:bound-tenant__1"].ShouldBe("fixture-foreign");
        environment["Authentication__WorkloadAuthority__Grants__6__Bindings__eventstore:bound-domain__0"].ShouldBe("folders");
        environment.Keys.Count(key => key.StartsWith("Authentication__WorkloadAuthority__Grants__6__Operations__", StringComparison.Ordinal)).ShouldBe(1);
        environment["Authentication__WorkloadAuthority__Grants__5__Operations__0"].ShouldBe("eventstore:stream-read");
        environment["Authentication__WorkloadAuthority__Grants__5__AllowUnbound"].ShouldBe("false");
        environment["Authentication__WorkloadAuthority__Grants__5__Workload"].ShouldBe("tenants");
        environment["Authentication__WorkloadAuthority__Grants__5__Bindings__eventstore:bound-tenant__0"].ShouldBe("platform:global-administrators");
        environment["Authentication__WorkloadAuthority__Grants__5__Bindings__eventstore:bound-domain__0"].ShouldBe("global-administrators");
        environment.Keys.Count(key => key.StartsWith("Authentication__WorkloadAuthority__Grants__5__Operations__", StringComparison.Ordinal)).ShouldBe(1);
        environment.Keys.Count(key => key.StartsWith("Authentication__WorkloadAuthority__Grants__5__Bindings__", StringComparison.Ordinal)).ShouldBe(2);
        var folderEnvironment = await EnvironmentAsync(folders, builder.ExecutionContext);
        folderEnvironment["Folders__EventStore__OwnerMetadataReads__Enabled"].ShouldBe("true");
        folderEnvironment["Folders__EventStore__OwnerMetadataReads__DiagnosticsEnabled"].ShouldBe("true");
        folderEnvironment["Authentication__BoundStreamRead__PrivateKey"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        IReadOnlyDictionary<string, object> tenantEnvironment = await EnvironmentAsync(tenants.Resource, builder.ExecutionContext);
        tenantEnvironment["EventStore__Authentication__TokenEndpoint"].ShouldBe("https://127.0.0.1:54321/users/token");
        tenantEnvironment["EventStore__Authentication__ClientId"].ShouldBe("supported-read-bootstrap");
        tenantEnvironment["EventStore__Authentication__Username"].ShouldBe("actor-1");
        tenantEnvironment["EventStore__Authentication__Password"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        tenantEnvironment["Authentication__WorkloadIssuer__ClientSecret"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        tenantEnvironment["Authentication__BoundStreamRead__PrivateKey"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        tenantEnvironment["Tenants__BootstrapBoundWorkloadEnabled"].ShouldBe("true");
        tenantEnvironment["Tenants__BootstrapGlobalAdminEnabled"].ShouldBe("true");
        tenantEnvironment["Tenants__BootstrapGlobalAdminUserId"].ShouldBe("actor-1");
        IReadOnlyDictionary<string, object> gatewayEnvironment = await EnvironmentAsync(gateway.Resource, builder.ExecutionContext);
        gatewayEnvironment["Authentication__BoundCommand__AllowedDomains__tenants"].ShouldBe("global-administrators");
        // Replay the removed, later Infrastructure assignments through Aspire's actual callbacks.
        // Equal final dictionaries prove that they rewrote only identical existing values.
        _ = tenants.WithEnvironment("Tenants__BootstrapBoundWorkloadEnabled", "true")
            .WithEnvironment("Tenants__BootstrapGlobalAdminEnabled", "true")
            .WithEnvironment("Tenants__BootstrapGlobalAdminUserId", "actor-1");
        _ = gateway.WithEnvironment("Authentication__BoundCommand__AllowedDomains__tenants", "global-administrators");
        IReadOnlyDictionary<string, object> previousTenantEnvironment = await EnvironmentAsync(tenants.Resource, builder.ExecutionContext);
        IReadOnlyDictionary<string, object> previousGatewayEnvironment = await EnvironmentAsync(gateway.Resource, builder.ExecutionContext);
        previousTenantEnvironment.Count.ShouldBe(tenantEnvironment.Count);
        previousGatewayEnvironment.Count.ShouldBe(gatewayEnvironment.Count);
        foreach ((string key, object value) in tenantEnvironment) { previousTenantEnvironment[key].ShouldBe(value); }
        foreach ((string key, object value) in gatewayEnvironment) { previousGatewayEnvironment[key].ShouldBe(value); }
        TestContext.Current.TestOutputHelper!.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            DuplicateBootstrapAssignmentsEquivalent = true,
            TenantEnvironmentEntries = tenantEnvironment.Count,
            GatewayEnvironmentEntries = gatewayEnvironment.Count,
        }));
        foreach (ProjectResource resource in builder.Resources.OfType<ProjectResource>().Where(resource => resource.Name != "supported-read-authority"))
        {
            (await EnvironmentAsync(resource, builder.ExecutionContext))["EventStore__RouteCatalog__ScopedDecodeReuse"].ShouldBe("true");
            (await EnvironmentAsync(resource, builder.ExecutionContext))["EventStore__RouteCatalog__SerializeLocalLeaseRelease"].ShouldBe("false");
            (await EnvironmentAsync(resource, builder.ExecutionContext))["EventStore__RouteCatalog__RetainValidatedCatalogBytes"].ShouldBe("true");
            (await EnvironmentAsync(resource, builder.ExecutionContext))["EventStore__RouteCatalog__ReuseHeldMetadataDispatch"]
                .ShouldBe(resource.Name == "folders" ? "true" : "false");
            DaprSidecarAnnotation annotation = resource.Annotations.OfType<DaprSidecarAnnotation>().Single();
            annotation.Sidecar.Annotations.OfType<DaprComponentReferenceAnnotation>()
                .Any(reference => ReferenceEquals(reference.Component, conversationStore)).ShouldBe(resource.Name == "projects");
            DaprSidecarOptions options = annotation.Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single().Options;
            string selectedDirectory = resource.Name switch
            {
                "eventstore" => Path.Combine(Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!, "components"),
                "projects" => Path.Combine(Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!, "components-projects"),
                _ => components,
            };
            options.ResourcesPaths.ShouldBe([selectedDirectory]);
            var sidecar = await EnvironmentAsync(annotation.Sidecar, builder.ExecutionContext);
            sidecar["NAMESPACE"].ShouldBe(inputs["Projects:SupportedReads:IsolationId"]);
            sidecar["APP_API_TOKEN"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        }
        using System.Text.Json.JsonDocument policy = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!, "components", "owner-resiliency.yaml")));
        policy.RootElement.GetProperty("scopes").EnumerateArray().Select(value => value.GetString()).ShouldBe(["eventstore"]);
        var specification = policy.RootElement.GetProperty("spec");
        var targets = specification.GetProperty("targets").GetProperty("apps");
        targets.EnumerateObject().Select(target => target.Name).Order(StringComparer.Ordinal)
            .ShouldBe(["eventstore", "folders", "projects", "projects-workers", "tenants"]);
        var target = targets.GetProperty("folders");
        string timeout = target.GetProperty("timeout").GetString()!;
        var deadlines = specification.GetProperty("policies").GetProperty("timeouts");
        deadlines.EnumerateObject().Count().ShouldBe(4);
        double ownerSeconds = double.Parse(deadlines.GetProperty(timeout).GetString()!.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture);
        string retry = target.GetProperty("retry").GetString()!;
        specification.GetProperty("policies").GetProperty("retries").GetProperty(retry).GetProperty("maxRetries").GetInt32().ShouldBe(0);
        foreach (var app in targets.EnumerateObject().Where(app => app.Name is "projects" or "folders"))
        {
            app.Value.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldBe(["retry", "timeout"]);
            app.Value.GetProperty("retry").GetString().ShouldBe(retry);
        }
        IReadOnlyDictionary<string, object> projectEnvironment = await EnvironmentAsync(projects.Resource, builder.ExecutionContext);
        double wholeOwnerSeconds = TimeSpan.Parse(projectEnvironment["EventStore__OwnerQueries__OperationTimeout"].ToString()!, System.Globalization.CultureInfo.InvariantCulture).TotalSeconds;
        string defaults = File.ReadAllText(Path.Combine(components, "resiliency.yaml"));
        object sourcePolicy = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build().Deserialize<object>(defaults);
        JsonNode original = JsonNode.Parse(new SerializerBuilder().JsonCompatible().Build().Serialize(sourcePolicy))!;
        JsonNode selected = JsonNode.Parse(policy.RootElement.GetRawText())!;
        foreach (string unchanged in new[] { "eventstore", "tenants", "projects-workers" })
            JsonNode.DeepEquals(original["spec"]!["targets"]!["apps"]![unchanged], selected["spec"]!["targets"]!["apps"]![unchanged]).ShouldBeTrue();
        JsonNode projectPolicy = JsonNode.Parse(File.ReadAllText(Path.Combine(
            Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!, "components-projects", "projects-resiliency.yaml")))!;
        projectPolicy["scopes"]!.AsArray().Select(value => value!.GetValue<string>()).ShouldBe(["projects"]);
        JsonNode.DeepEquals(original["spec"]!["targets"]!["apps"], projectPolicy["spec"]!["targets"]!["apps"]).ShouldBeTrue();
        foreach (JsonNode document in new[] { selected, projectPolicy })
        {
            var originalComponents = original["spec"]!["targets"]!["components"]!.AsObject();
            var selectedComponents = document["spec"]!["targets"]!["components"]!.AsObject();
            selectedComponents.Select(pair => pair.Key).Order(StringComparer.Ordinal).ShouldBe(originalComponents.Select(pair => pair.Key).Order(StringComparer.Ordinal));
            JsonNode.DeepEquals(originalComponents["pubsub"], selectedComponents["pubsub"]).ShouldBeTrue();
            JsonNode.DeepEquals(originalComponents["statestore"]!["inbound"], selectedComponents["statestore"]!["inbound"]).ShouldBeTrue();
            var stateTarget = selectedComponents["statestore"]!["outbound"]!.AsObject();
            stateTarget.Select(pair => pair.Key).Order(StringComparer.Ordinal).ShouldBe(["retry", "timeout"]);
            stateTarget["timeout"]!.GetValue<string>().ShouldBe(originalComponents["statestore"]!["outbound"]!["timeout"]!.GetValue<string>());
            document["spec"]!["policies"]!["timeouts"]![stateTarget["timeout"]!.GetValue<string>()]!.GetValue<string>().ShouldBe("15s");
            document["spec"]!["policies"]!["retries"]![stateTarget["retry"]!.GetValue<string>()]!["maxRetries"]!.GetValue<int>().ShouldBe(0);
            foreach (var section in original["spec"]!["policies"]!.AsObject())
                foreach (var entry in section.Value!.AsObject())
                    JsonNode.DeepEquals(entry.Value, document["spec"]!["policies"]![section.Key]![entry.Key]).ShouldBeTrue();
        }
        System.Text.RegularExpressions.Regex.IsMatch(defaults,
            @"(?m)^      projects:\r?\n(?:        [^\n]*\r?\n)*?        timeout: projectsServiceInvocationTimeout").ShouldBeTrue();
        string enclosing = System.Text.RegularExpressions.Regex.Match(defaults, @"projectsServiceInvocationTimeout:\s*(\d+)s").Groups[1].Value;
        deadlines.GetProperty(targets.GetProperty("projects").GetProperty("timeout").GetString()!).GetString().ShouldBe(enclosing + "s");
        ownerSeconds.ShouldBeGreaterThan(0);
        ownerSeconds.ShouldBeLessThan(wholeOwnerSeconds);
        (wholeOwnerSeconds * 2).ShouldBeLessThan(double.Parse(enclosing, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>The canonical helper replaces initial control-plane syntax for every real owner and worker.</summary>
    [Fact]
    public async Task CanonicalMtlsReplacesAllSixSidecarControlPlaneBindingsAsync()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        Dictionary<string, string?> inputs = ValidInputs();
        string root = TestRepositoryRoot.Find();
        string privateDirectory = Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!;
        inputs["Dapr:Mtls:CertificateDirectory"] = Path.Combine(privateDirectory, "mtls");
        builder.Configuration.AddInMemoryCollection(inputs);
        string fixture = Path.Combine(root, "tests", "Hexalith.Projects.E2E.Fixtures", "Hexalith.Projects.E2E.Fixtures.csproj");
        SupportedProjectReadFixtureProfile.AddResources(builder, builder.AddProject("eventstore", fixture), builder.AddProject("tenants", fixture),
            builder.AddProject("projects", fixture), builder.AddProject("projects-workers", fixture), Path.Combine(root, "src", "Hexalith.Projects.AppHost", "DaprComponents"));
        var owned = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder, root,
            Path.Combine(root, "src", "Hexalith.Projects.AppHost", "DaprComponents", "sentry.yaml"), inputs["Projects:SupportedReads:IsolationId"]!);
        _directories.Add(owned.RunDirectory);
        foreach (string file in new[] { "ca.crt", "issuer.crt", "issuer.key" }) File.WriteAllText(Path.Combine(owned.CertificateDirectory, file), "structural-" + file);
        builder.Resources.OfType<ContainerResource>().Select(resource => resource.Name).Order().ShouldBe(["dapr-placement-mtls", "dapr-scheduler-mtls", "dapr-sentry"]);
        ProjectResource[] applications = builder.Resources.OfType<ProjectResource>().Where(resource => resource.Annotations.OfType<DaprSidecarAnnotation>().Any()).ToArray();
        applications.Length.ShouldBe(6);
        foreach (ProjectResource application in applications)
        {
            var sidecar = application.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;
            DaprSidecarOptions options = sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single().Options;
            options.PlacementHostAddress.ShouldNotBe(inputs["Dapr:PlacementHostAddress"]);
            options.SchedulerHostAddress.ShouldNotBe(inputs["Dapr:SchedulerHostAddress"]);
            options.RuntimePath.ShouldNotBeNullOrWhiteSpace();
            var environment = await EnvironmentAsync(sidecar, builder.ExecutionContext);
            environment["NAMESPACE"].ShouldBe(inputs["Projects:SupportedReads:IsolationId"]);
            environment["DAPR_CONTROLPLANE_TRUST_DOMAIN"].ShouldBe("localhost");
            environment["DAPR_CONTROLPLANE_NAMESPACE"].ShouldBe("default");
            environment["DAPR_CERT_KEY"].ShouldBe("structural-issuer.key");
        }
    }

    /// <summary>Checks actual toolkit, canonical Platform native rewrite and startup subscriber ordering without starting processes.</summary>
    [Fact(Timeout = 30000)]
    public async Task SqliteOrderingObservesCanonicalNativeCommandRewriteAsync()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true, AllowUnsecuredTransport = true });
        builder.Environment.EnvironmentName = "Development";
        Dictionary<string, string?> inputs = ValidInputs();
        string root = TestRepositoryRoot.Find();
        string directory = Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!;
        inputs["Dapr:Mtls:CertificateDirectory"] = Path.Combine(directory, "mtls");
        builder.Configuration.AddInMemoryCollection(inputs);
        builder.Configuration["DcpPublisher:CliPath"] = Path.Combine(directory, "model-only-dcp-not-started");
        builder.Configuration["DcpPublisher:DashboardPath"] = Path.Combine(directory, "model-only-dashboard-not-started");
        string modeledCli = Path.Combine(directory, "dapr");
        builder.AddDapr(options => options.DaprPath = modeledCli);
        string fixture = Path.Combine(root, "tests", "Hexalith.Projects.E2E.Fixtures", "Hexalith.Projects.E2E.Fixtures.csproj");
        SupportedProjectReadFixtureProfile.AddResources(builder, builder.AddProject("eventstore", fixture), builder.AddProject("tenants", fixture),
            builder.AddProject("projects", fixture), builder.AddProject("projects-workers", fixture), Path.Combine(root, "src", "Hexalith.Projects.AppHost", "DaprComponents"));
        string runtimeHome = Path.Combine(directory, "structural-runtime-not-executed");
        string nativePath = Path.Combine(runtimeHome, ".dapr", "bin", OperatingSystem.IsWindows() ? "daprd.exe" : "daprd");
        Directory.CreateDirectory(Path.GetDirectoryName(nativePath)!);
        await File.WriteAllTextAsync(nativePath, "structural placeholder; never executed", TestContext.Current.CancellationToken);
        var peers = new[] { "tenants", "eventstore", "projects", "projects-workers", "folders", "folders-workers" }.Select(name =>
            builder.CreateResourceBuilder(builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == name))).ToArray();
        foreach (var peer in peers)
        {
            var sidecar = peer.Resource.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;
            var annotation = sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single();
            sidecar.Annotations[sidecar.Annotations.IndexOf(annotation)] = new DaprSidecarOptionsAnnotation(annotation.Options with { RuntimePath = runtimeHome });
            foreach (var wait in peer.Resource.Annotations.OfType<WaitAnnotation>().Where(wait => wait.WaitType == WaitType.WaitUntilHealthy
                && peers.Any(selected => ReferenceEquals(selected.Resource, wait.Resource))).ToArray())
            { peer.Resource.Annotations.Remove(wait); peer.WaitForStart(builder.CreateResourceBuilder(wait.Resource)); }
        }
        var owned = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder, root,
            Path.Combine(root, "src", "Hexalith.Projects.AppHost", "DaprComponents", "sentry.yaml"), inputs["Projects:SupportedReads:IsolationId"]!);
        _directories.Add(owned.RunDirectory);
        builder.AddEventStoreDevelopmentSqliteStartupOrdering(peers);
        await using var application = builder.Build();
        var eventing = application.Services.GetRequiredService<IDistributedApplicationEventing>();
        List<string> order = [];
        foreach (var subscriber in application.Services.GetServices<IDistributedApplicationEventingSubscriber>().Where(subscriber => subscriber.GetType().Name is
            "DaprDistributedApplicationLifecycleHook" or "DaprSelfHostedMtlsSubscriber" or "DevelopmentSqliteDiscoveryStartupSubscriber"))
        {
            string name = subscriber.GetType().Name;
            order.Add(name);
            if (name is "DaprSelfHostedMtlsSubscriber" or "DevelopmentSqliteDiscoveryStartupSubscriber")
                eventing.Subscribe<BeforeStartEvent>((startup, _) =>
                {
                    foreach (var peer in peers)
                    {
                        var actual = startup.Model.Resources.OfType<ExecutableResource>().Single(resource => resource.Name == peer.Resource.Name + "-dapr-cli");
                        string expected = name == "DaprSelfHostedMtlsSubscriber" ? modeledCli : nativePath;
                        actual.Annotations.OfType<ExecutableAnnotation>().Last().Command.ShouldBe(expected);
                        actual.Command.ShouldBe(expected);
                    }
                    return Task.CompletedTask;
                });
            await subscriber.SubscribeAsync(eventing, builder.ExecutionContext, TestContext.Current.CancellationToken);
        }
        order.ShouldBe(["DaprDistributedApplicationLifecycleHook", "DaprSelfHostedMtlsSubscriber", "DevelopmentSqliteDiscoveryStartupSubscriber"]);
        await eventing.PublishAsync(new BeforeStartEvent(application.Services, application.Services.GetRequiredService<DistributedApplicationModel>()), TestContext.Current.CancellationToken);
        TestContext.Current.TestOutputHelper!.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { SubscriberOrder = order, NativeRewrites = peers.Length }));
    }

    private Dictionary<string, string?> ValidInputs()
    {
        string directory = Directory.CreateTempSubdirectory("supported-read-topology-").FullName;
        _directories.Add(directory);
        string existing = Path.Combine(directory, "public-placeholder");
        File.WriteAllText(existing, "structural placeholder");
        string discovery = Path.Combine(directory, "discovery.json");
        string invocations = Directory.CreateDirectory(Path.Combine(directory, "catalog-invocations")).FullName;
        string ownerSource = Directory.CreateDirectory(Path.Combine(directory, "owner-source-reads")).FullName;
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(ownerSource, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(invocations, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.WriteAllText(discovery, System.Text.Json.JsonSerializer.Serialize(new
        {
            spec = new { nameResolution = new { component = "sqlite", version = "v1", configuration = new { connectionString = Path.Combine(directory, "registry.sqlite") } } },
        }));
        Dictionary<string, string?> inputs = new()
        {
            ["EnableKeycloak"] = "false", ["KeycloakPersistent"] = "false", ["Projects:E2E:LiveFixtures"] = "false",
            ["Dapr:RedisHost"] = "127.0.0.1:56379", ["Dapr:PlacementHostAddress"] = "127.0.0.1:55005", ["Dapr:SchedulerHostAddress"] = "127.0.0.1:55006",
            ["Projects:SupportedReads:IsolationId"] = "reads-1234567890abcdef", ["Projects:SupportedReads:UserIssuer"] = "https://127.0.0.1:54321/users",
            ["Projects:SupportedReads:CertificatePath"] = existing, ["Projects:SupportedReads:TrustBundle"] = existing,
            ["Projects:SupportedReads:DiscoveryConfiguration"] = discovery,
            ["Projects:SupportedReads:InvocationDirectory"] = invocations,
            ["Projects:SupportedReads:OwnerSourceReadDirectory"] = ownerSource,
            ["Projects:SupportedReads:CatalogDigestKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Projects:SupportedReads:Tenant"] = "fixture-tenant", ["Projects:SupportedReads:ForeignTenant"] = "fixture-foreign",
        };
        foreach (string key in new[] { "CertificatePassword", "EventStoreSecret", "ProjectsSecret", "OwnerPrivateKey", "OwnerPublicKey", "ReceiptKey", "ChannelToken", "TenantsSecret", "TenantOwnerPrivateKey", "TenantOwnerPublicKey", "BootstrapPassword", "FoldersSecret", "FolderOwnerPrivateKey", "FolderOwnerPublicKey" })
            inputs["Projects:SupportedReads:" + key] = "synthetic-structural-placeholder";
        return inputs;
    }

    /// <summary>Removes each test-owned structural configuration directory.</summary>
    public void Dispose()
    {
        foreach (string directory in _directories) Directory.Delete(directory, recursive: true);
    }

    private static async Task<IReadOnlyDictionary<string, object>> EnvironmentAsync(IResource resource, DistributedApplicationExecutionContext execution)
    {
        var context = new EnvironmentCallbackContext(execution, resource, new Dictionary<string, object>(), TestContext.Current.CancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context).ConfigureAwait(true);
        return context.EnvironmentVariables;
    }
}
