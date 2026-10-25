namespace Hexalith.Projects.Integration.Tests;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Hexalith.Projects.AppHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
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
        folders.GetProjectMetadata().ProjectPath.ShouldEndWith("Hexalith.Folders.Server.csproj");
        builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == "folders-workers")
            .GetProjectMetadata().ProjectPath.ShouldEndWith("Hexalith.Folders.Workers.csproj");
        builder.Resources.OfType<ParameterResource>().ShouldAllBe(parameter => parameter.Secret);
        IReadOnlyDictionary<string, object> environment = await EnvironmentAsync(gateway.Resource, builder.ExecutionContext);
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
        foreach (ProjectResource resource in builder.Resources.OfType<ProjectResource>().Where(resource => resource.Name != "supported-read-authority"))
        {
            DaprSidecarAnnotation annotation = resource.Annotations.OfType<DaprSidecarAnnotation>().Single();
            DaprSidecarOptions options = annotation.Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single().Options;
            options.ResourcesPaths.ShouldContain(Path.Combine(Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!, "components"));
            var sidecar = await EnvironmentAsync(annotation.Sidecar, builder.ExecutionContext);
            sidecar["NAMESPACE"].ShouldBe(inputs["Projects:SupportedReads:IsolationId"]);
            sidecar["APP_API_TOKEN"].ShouldBeOfType<ParameterResource>().Secret.ShouldBeTrue();
        }
        using System.Text.Json.JsonDocument policy = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(Path.GetDirectoryName(inputs["Projects:SupportedReads:CertificatePath"])!, "components", "owner-resiliency.yaml")));
        policy.RootElement.GetProperty("scopes").EnumerateArray().Select(value => value.GetString()).ShouldBe(["eventstore"]);
        var specification = policy.RootElement.GetProperty("spec");
        var target = specification.GetProperty("targets").GetProperty("apps").EnumerateObject().Single();
        target.Name.ShouldBe("folders");
        string timeout = target.Value.GetProperty("timeout").GetString()!;
        var deadlines = specification.GetProperty("policies").GetProperty("timeouts");
        deadlines.EnumerateObject().Count().ShouldBe(1);
        double ownerSeconds = double.Parse(deadlines.GetProperty(timeout).GetString()!.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture);
        string retry = target.Value.GetProperty("retry").GetString()!;
        specification.GetProperty("policies").GetProperty("retries").GetProperty(retry).GetProperty("maxRetries").GetInt32().ShouldBe(0);
        IReadOnlyDictionary<string, object> projectEnvironment = await EnvironmentAsync(projects.Resource, builder.ExecutionContext);
        double wholeOwnerSeconds = TimeSpan.Parse(projectEnvironment["EventStore__OwnerQueries__OperationTimeout"].ToString()!, System.Globalization.CultureInfo.InvariantCulture).TotalSeconds;
        string defaults = File.ReadAllText(Path.Combine(components, "resiliency.yaml"));
        System.Text.RegularExpressions.Regex.IsMatch(defaults,
            @"(?m)^      projects:\r?\n(?:        [^\n]*\r?\n)*?        timeout: projectsServiceInvocationTimeout").ShouldBeTrue();
        string enclosing = System.Text.RegularExpressions.Regex.Match(defaults, @"projectsServiceInvocationTimeout:\s*(\d+)s").Groups[1].Value;
        ownerSeconds.ShouldBeGreaterThan(0);
        ownerSeconds.ShouldBeLessThan(wholeOwnerSeconds);
        (wholeOwnerSeconds * 2).ShouldBeLessThan(double.Parse(enclosing, System.Globalization.CultureInfo.InvariantCulture));
    }

    private Dictionary<string, string?> ValidInputs()
    {
        string directory = Directory.CreateTempSubdirectory("supported-read-topology-").FullName;
        _directories.Add(directory);
        string existing = Path.Combine(directory, "public-placeholder");
        File.WriteAllText(existing, "structural placeholder");
        string discovery = Path.Combine(directory, "discovery.json");
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
            ["Projects:SupportedReads:Tenant"] = "fixture-tenant", ["Projects:SupportedReads:ForeignTenant"] = "fixture-foreign",
        };
        foreach (string key in new[] { "CertificatePassword", "EventStoreSecret", "ProjectsSecret", "OwnerPrivateKey", "OwnerPublicKey", "ReceiptKey", "ChannelToken" })
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
