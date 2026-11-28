namespace Hexalith.Projects.Integration.Tests;

using System.Security.Cryptography;
using System.Text.Json;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using global::Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

/// <summary>Model-tests the ordinary run-mode AppHost composition without starting any resource.</summary>
/// <remarks>
/// The ordinary run composes its route catalog from an owner profile. This test provisions a fresh private profile and
/// disables configuration defaults, so developer user secrets, retained custody and environment never reach the model.
/// </remarks>
public sealed class OrdinaryRunModeCompositionTests : IDisposable
{
    private const string Prefix = "Projects:DevelopmentRouteCatalog:";
    private readonly string _directory = Directory.CreateTempSubdirectory("projects-ordinary-run-").FullName;

#if SUPPORTED_PROJECT_READ_SOURCE
    /// <summary>Pins the receiver grants, workload binding, owner proof trust, credential skip and secret persistence.</summary>
    /// <param name="persistent">Whether the local Keycloak resource persists across sessions.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryRunComposesCatalogGrantsAndWorkloadBindingAsync(bool persistent)
    {
        Dictionary<string, string?> inputs = Inputs(persistent);
        IDistributedApplicationTestingBuilder builder = await CreateAsync(inputs).ConfigureAwait(true);
        await using var lifetime = builder.ConfigureAwait(true);
        IResource gateway = Resource(builder, "eventstore");
        IResource projects = Resource(builder, "projects");
        IResource tenants = Resource(builder, "tenants");
        var gatewayEnvironment = await EnvironmentAsync(gateway, builder.ExecutionContext).ConfigureAwait(true);
        var projectsEnvironment = await EnvironmentAsync(projects, builder.ExecutionContext).ConfigureAwait(true);
        var tenantsEnvironment = await EnvironmentAsync(tenants, builder.ExecutionContext).ConfigureAwait(true);

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(TestRepositoryRoot.Find(), "module", "hexalith.module-manifest.v2.json")));
        string[] manifestCallbacks = manifest.RootElement.GetProperty("modules").EnumerateArray().Single()
            .GetProperty("surfaces").GetProperty("interfaces").EnumerateArray().Single()
            .GetProperty("invocationRules").EnumerateArray()
            .Select(rule =>
            {
                rule.GetProperty("verbs").EnumerateArray().Select(verb => verb.GetString()).ShouldBe(["POST"]);
                return rule.GetProperty("caller").GetString() + " " + rule.GetProperty("path").GetString()!.TrimStart('/');
            })
            .Order(StringComparer.Ordinal).ToArray();
        Grants(projectsEnvironment).ShouldBe(manifestCallbacks);
        Grants(tenantsEnvironment).ShouldBe(new[] { "process", "replay-state", "query", "project", "admin/operational-index-metadata" }
            .Select(method => "eventstore " + method).Order(StringComparer.Ordinal));
        Grants(gatewayEnvironment).ShouldBe(
        [
            "projects api/v1/commands",
            "projects api/v1/queries/owner-authority/workload",
            "projects api/v1/streams/read/workload",
            "tenants api/v1/streams/read/workload",
        ]);
        foreach (var environment in new[] { gatewayEnvironment, projectsEnvironment, tenantsEnvironment })
        {
            Enumerable.Range(0, 3).Select(index => environment["EventStore__RouteCatalog__DevelopmentBootstrap__RequiredHostIds__" + index])
                .ShouldBe(["eventstore", "projects", "tenants"]);
        }

        // The gateway binds the authenticated originating caller for every compiled Projects query without delegated context.
        string[] publicQueries = Hexalith.EventStore.ServiceDefaults.Routing.CompiledRouteCatalogDeclarations
            .Read([typeof(Hexalith.Projects.Contracts.Routing.ProjectsRuntimeCatalogDeclarations).Assembly])
            .Where(static route => route.Kind == "query" && route.Execution?.RequiresDelegatedContext != true)
            .Select(static route => route.MessageType!).Order(StringComparer.Ordinal).ToArray();
        publicQueries.ShouldNotBeEmpty();
        gatewayEnvironment.Keys.Count(static key => key.StartsWith("Authentication__QueryAuthority__RequiredQueries__projects__", StringComparison.Ordinal))
            .ShouldBe(publicQueries.Length);
        Enumerable.Range(0, publicQueries.Length)
            .Select(index => gatewayEnvironment["Authentication__QueryAuthority__RequiredQueries__projects__" + index])
            .ShouldBe(publicQueries);

        var clientSecrets = new List<ParameterResource>();
        foreach ((IResource resource, IReadOnlyDictionary<string, object> environment, string clientId) in new[]
        {
            (gateway, gatewayEnvironment, "GatewayClientId"),
            (projects, projectsEnvironment, "ProjectsClientId"),
            (tenants, tenantsEnvironment, "TenantsClientId"),
        })
        {
            environment["Authentication__WorkloadJwtBearer__Authority"].ShouldBe(inputs[Prefix + "Workload:Authority"]);
            environment["Authentication__WorkloadJwtBearer__Issuer"].ShouldBe(inputs[Prefix + "Workload:Issuer"]);
            environment["Authentication__WorkloadJwtBearer__Audience"].ShouldBe(resource.Name);
            environment["Authentication__WorkloadJwtBearer__RequireHttpsMetadata"].ShouldBe("true");
            environment["Authentication__WorkloadIssuer__Workload"].ShouldBe(resource.Name);
            environment["Authentication__WorkloadIssuer__ClientId"].ShouldBe(inputs[Prefix + "Workload:" + clientId]);
            environment["Authentication__WorkloadIssuer__TokenEndpoint"].ShouldBe(inputs[Prefix + "Workload:TokenEndpoint"]);
            environment["Authentication__WorkloadIssuer__EnableResourceBindingRequests"].ShouldBe("true");
            ParameterResource secret = environment["Authentication__WorkloadIssuer__ClientSecret"].ShouldBeOfType<ParameterResource>();
            secret.Secret.ShouldBeTrue();
            clientSecrets.Add(secret);
        }

        clientSecrets.Distinct().Count().ShouldBe(3);

        // The bound workload issuer replaces the Keycloak workload client for the gateway; Keycloak keeps its own secret.
        ParameterResource keycloakSecret = builder.Resources.OfType<ParameterResource>()
            .Single(static resource => resource.Name == "eventstore-workload-client-secret");
        keycloakSecret.Secret.ShouldBeTrue();
        clientSecrets.ShouldNotContain(keycloakSecret);
        (await EnvironmentAsync(Resource(builder, "security"), builder.ExecutionContext).ConfigureAwait(true))
            ["HEXALITH_EVENTSTORE_WORKLOAD_CLIENT_SECRET"].ShouldBeSameAs(keycloakSecret);
        // Persistence wraps the generated default in run mode only; resolving it would write user secrets.
        (keycloakSecret.Default is GenerateParameterDefault).ShouldBe(!persistent);
        keycloakSecret.Default!.GetType().Name.Contains("UserSecrets", StringComparison.Ordinal).ShouldBe(persistent);

        gatewayEnvironment["Authentication__DaprInternal__AllowedCallers__0"].ShouldBe("projects");
        gatewayEnvironment["Authentication__DaprInternal__AllowedCallers__1"].ShouldBe("tenants");
        gatewayEnvironment.Keys.Count(static key => key.StartsWith("Authentication__DaprInternal__AllowedCallers__", StringComparison.Ordinal)).ShouldBe(2);
        var ownerKeys = new List<ParameterResource>();
        foreach ((string workload, IReadOnlyDictionary<string, object> environment, string domains) in new[]
        {
            ("projects", projectsEnvironment, "projects"),
            ("tenants", tenantsEnvironment, "tenants,global-administrators"),
        })
        {
            ParameterResource ownerKey = environment["Authentication__BoundStreamRead__PrivateKey"].ShouldBeOfType<ParameterResource>();
            ownerKey.Secret.ShouldBeTrue();
            ownerKeys.Add(ownerKey);
            using RSA key = RSA.Create();
            key.ImportPkcs8PrivateKey(Convert.FromBase64String((await ownerKey.GetValueAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))!), out _);
            key.KeySize.ShouldBeGreaterThanOrEqualTo(2048);
            gatewayEnvironment["Authentication__BoundStreamRead__PublicKeys__" + workload].ShouldBe(Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
            gatewayEnvironment["Authentication__BoundStreamRead__AllowedDomains__" + workload].ShouldBe(domains);
        }

        ownerKeys.Distinct().Count().ShouldBe(2);
        gatewayEnvironment.Keys.Where(static key => key.StartsWith("Authentication__BoundStreamRead__PublicKeys__", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ShouldBe(["Authentication__BoundStreamRead__PublicKeys__projects", "Authentication__BoundStreamRead__PublicKeys__tenants"]);
        ParameterResource receipt = gatewayEnvironment["Authentication__HumanAuthorityReceipt__SigningKey"].ShouldBeOfType<ParameterResource>();
        receipt.Secret.ShouldBeTrue();
        projectsEnvironment["Authentication__HumanAuthorityReceipt__SigningKey"].ShouldBeSameAs(receipt);
        projectsEnvironment["Projects__DevelopmentRouteCatalog__Enabled"].ShouldBe("true");
        IDaprSidecarResource sidecar = projects.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;
        sidecar.Annotations.OfType<DaprComponentReferenceAnnotation>()
            .ShouldContain(static reference => reference.Component.Name == "projects-conversation-start-setup");
    }

    /// <summary>Names the missing Redis catalog custody when ordinary AppHost composition refuses.</summary>
    [Fact]
    public async Task OrdinaryRunWithoutCustodyRefusesAsync()
    {
        Dictionary<string, string?> inputs = Inputs(persistent: false);
        foreach (string key in inputs.Keys.Where(static key => key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _ = inputs.Remove(key);
        }

        Exception failure = await Should.ThrowAsync<Exception>(() => CreateAsync(inputs)).ConfigureAwait(true);
        Messages(failure).ShouldContain("Projects:DevelopmentRouteCatalog requires owner custody for the selected Redis backend.");
    }

    /// <summary>Refuses an incomplete workload binding before the private catalog inputs are created.</summary>
    /// <param name="key">The omitted workload binding input.</param>
    [Theory]
    [InlineData("ProjectsClientId")]
    [InlineData("ProjectsClientSecret")]
    [InlineData("TenantsClientId")]
    [InlineData("TenantsClientSecret")]
    public async Task OrdinaryRunRefusesAnIncompleteWorkloadBindingAsync(string key)
    {
        Dictionary<string, string?> inputs = Inputs(persistent: false);
        _ = inputs.Remove(Prefix + "Workload:" + key);
        Exception failure = await Should.ThrowAsync<Exception>(() => CreateAsync(inputs)).ConfigureAwait(true);
        Messages(failure).ShouldContain(message => message.Contains(Prefix + "Workload:" + key, StringComparison.Ordinal));
    }

    /// <summary>Refuses an unrelated backend before writing the private bootstrap realm.</summary>
    [Theory]
    [InlineData("persistent")]
    [InlineData("redis")]
    [InlineData("default-redis")]
    [InlineData("custody")]
    [InlineData("identity")]
    public async Task OrdinaryQualificationRefusesMismatchedCustodyBeforeWritingRealmAsync(string mismatch)
    {
        if (!OperatingSystem.IsLinux()) return;

        const string isolation = "fixtures-model";
        const string tenant = "fixtures-model-ordinary";
        const string redis = "127.0.0.1:24680";
        string marker = Path.Combine(_directory, "identity.json");
        string custody = Path.Combine(_directory, "catalog-custody");
        var inputs = Inputs(persistent: mismatch == "persistent");
        inputs[Prefix + "Qualification:Directory"] = _directory;
        inputs[Prefix + "Qualification:Namespace"] = isolation;
        inputs[Prefix + "Qualification:Tenant"] = tenant;
        inputs[Prefix + "Qualification:BootstrapPassword"] = new string('x', 32);
        inputs["Projects:SupportedReads:ConfigurationFile"] = marker;
        inputs[Prefix + "CustodyDirectory"] = mismatch == "custody" ? Path.Combine(_directory, "existing-owner-custody") : custody;
        inputs["Dapr:RedisHost"] = mismatch switch
        {
            "redis" => "127.0.0.1:24681",
            "default-redis" => null,
            _ => redis,
        };
        File.WriteAllBytes(marker, JsonSerializer.SerializeToUtf8Bytes(new
        {
            isolationId = mismatch == "identity" ? "other-run" : isolation,
            tenant,
            redisHost = redis,
            custodyDirectory = custody,
        }));
        File.SetUnixFileMode(marker, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        Exception failure = await Should.ThrowAsync<Exception>(() => CreateAsync(inputs)).ConfigureAwait(true);
        string reason = mismatch == "persistent"
            ? "Ordinary qualification requires private Development custody, local security, and the selected tenant."
            : "Ordinary qualification requires its protected identity and exact disposable Redis and catalog custody.";
        Messages(failure).ShouldContain(reason);
        Directory.Exists(Path.Combine(_directory, "realm")).ShouldBeFalse();
        Directory.Exists(custody).ShouldBeFalse();
    }
#else
    /// <summary>Pins the recorded package-mode decision: the ordinary run refuses and names the source-build requirement.</summary>
    [Fact]
    public async Task PackageModeOrdinaryRunRefusesWithTheSourceBuildRequirementAsync()
    {
        Exception failure = await Should.ThrowAsync<Exception>(() => CreateAsync(Inputs(persistent: false))).ConfigureAwait(true);
        Messages(failure).ShouldContain(message => message.Contains("only from a local Debug source build", StringComparison.Ordinal));
    }
#endif

    /// <inheritdoc />
    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static Task<IDistributedApplicationTestingBuilder> CreateAsync(Dictionary<string, string?> inputs)
        => DistributedApplicationTestingBuilder.CreateAsync<global::Projects.Hexalith_Projects_AppHost>(
            [],
            (_, settings) =>
            {
                settings.EnvironmentName = "Development";
                settings.DisableDefaults = true;
                settings.Configuration ??= new ConfigurationManager();
                settings.Configuration.AddInMemoryCollection(inputs);
            },
            TestContext.Current.CancellationToken);

    private Dictionary<string, string?> Inputs(bool persistent) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["EnableKeycloak"] = "true",
        ["KeycloakPersistent"] = persistent ? "true" : "false",
        ["Projects:E2E:LiveFixtures"] = "false",
        [Prefix + "CustodyDirectory"] = Path.Combine(_directory, "custody"),
        [Prefix + "AuthorityComponentName"] = "ordinarycatalog",
        [Prefix + "AuthorityStateKey"] = "ordinary-model",
        [Prefix + "ActiveDigestKeyGeneration"] = "model-v1",
        [Prefix + "DigestKeys:model-v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        [Prefix + "SecretMapGeneration"] = "model-v1",
        [Prefix + "Workload:Authority"] = "https://127.0.0.1:12345/workload-authority",
        [Prefix + "Workload:Issuer"] = "https://127.0.0.1:12345/workload-authority",
        [Prefix + "Workload:TokenEndpoint"] = "https://127.0.0.1:12345/workload-authority/token",
        [Prefix + "Workload:GatewayClientId"] = "eventstore-model",
        [Prefix + "Workload:GatewayClientSecret"] = "gateway-model-secret",
        [Prefix + "Workload:ProjectsClientId"] = "projects-model",
        [Prefix + "Workload:ProjectsClientSecret"] = "projects-model-secret",
        [Prefix + "Workload:TenantsClientId"] = "tenants-model",
        [Prefix + "Workload:TenantsClientSecret"] = "tenants-model-secret",
        [Prefix + "Workload:HumanAuthorityReceiptSigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
    };

    private static IResource Resource(IDistributedApplicationTestingBuilder builder, string name)
        => builder.Resources.Single(resource => resource.Name == name);

    private static string[] Grants(IReadOnlyDictionary<string, object> environment)
    {
        using JsonDocument grants = JsonDocument.Parse(environment["EventStore__RouteCatalog__DevelopmentBootstrap__DeclaredInvocations"].ShouldBeOfType<string>());
        return grants.RootElement.EnumerateArray().Select(grant =>
        {
            grant.GetProperty("HttpMethods").EnumerateArray().Select(verb => verb.GetString()).ShouldBe(["POST"]);
            return grant.GetProperty("CallerAppId").GetString() + " " + grant.GetProperty("Method").GetString();
        }).Order(StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> Messages(Exception failure)
    {
        for (Exception? current = failure; current is not null; current = current.InnerException)
        {
            yield return current.Message;
            if (current is AggregateException aggregate)
            {
                foreach (string message in aggregate.InnerExceptions.SelectMany(Messages))
                {
                    yield return message;
                }
            }
        }
    }

    private static async Task<IReadOnlyDictionary<string, object>> EnvironmentAsync(IResource resource, DistributedApplicationExecutionContext execution)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var context = new EnvironmentCallbackContext(execution, resource, new Dictionary<string, object>(), timeout.Token);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context).WaitAsync(timeout.Token).ConfigureAwait(true);
        }

        return context.EnvironmentVariables;
    }
}
