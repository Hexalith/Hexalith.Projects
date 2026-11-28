namespace Hexalith.Projects.Server.Tests;

using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Extensions;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.IdentityModel.Tokens;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Routing;
using Hexalith.Projects.EventStoreExtensions;
using Hexalith.Projects.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

using Xunit;

/// <summary>Checks the complete compiled owner against actual processor and SDK handler registrations.</summary>
public sealed class ProjectsRuntimeCatalogTests
{
    /// <summary>Every installed command, query and projection has one matching actual receiver and operation schema.</summary>
    [Fact]
    public void CompiledOwnerMatchesActualProcessorQueriesAndProjectionReceivers()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddProjectsDomainService();
        using WebApplication app = builder.Build();
        app.UseEventStoreDomainService();
        app.MapProjectsHumanEndpoints();

        var declarations = CompiledRouteCatalogDeclarations.Read([typeof(ProjectsRuntimeCatalogDeclarations).Assembly]);
        declarations.Count.ShouldBe(26);
        var processor = app.Services.GetRequiredKeyedService<IDomainProcessor>("projects")
            .ShouldBeAssignableTo<IDomainCommandPayloadInventory>();
        processor.CommandPayloadTypes.Select(type => type.FullName).Order(StringComparer.Ordinal)
            .ShouldBe(declarations.Where(route => route.Kind == "command").Select(route => route.Execution!.HandlerType).Order(StringComparer.Ordinal));
        app.Services.GetServices<IDomainQueryHandler>().Select(handler => handler.QueryType).Order(StringComparer.Ordinal)
            .ShouldBe(declarations.Where(route => route.Kind == "query").Select(route => route.MessageType!).Order(StringComparer.Ordinal));
        app.Services.GetServices<IAsyncDomainProjectionHandler>().Select(handler => handler.ProjectionType).Order(StringComparer.Ordinal)
            .ShouldBe(new[] { "conversation-start-setup", "project-reads" });
        var legacyHandler = app.Services.GetServices<IDomainProjectionHandler>().ShouldHaveSingleItem().ShouldBeOfType<ProjectProjectionHandler>();
        declarations.Single(route => route.Kind == "projection" && route.ProjectionType == ProjectsServerModule.ProjectionType)
            .Execution!.HandlerType.ShouldBe(legacyHandler.GetType().FullName);
        app.Services.GetKeyedServices<IAsyncAggregateReplay>("projects").ShouldHaveSingleItem().ShouldBeOfType<ProjectsDomainProcessor>();
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        DevelopmentDomainRouteCatalog.Targets(app.Services, routes, declarations).Count.ShouldBe(26);
        CompiledRouteCatalogDeclarations.ReadOperations([typeof(ProjectsRuntimeCatalogDeclarations).Assembly]).Count.ShouldBe(26);
        EventStoreDomainServiceEndpointInventory.Validate(routes, fallbackPolicy: null).ShouldBeEmpty();
        var canonicalReceivers = routes.OfType<RouteEndpoint>()
            .Where(endpoint => EventStoreDomainServiceRoutes.TryGet(endpoint.RoutePattern.RawText, out _)).ToArray();
        canonicalReceivers.Select(endpoint => endpoint.RoutePattern.RawText).Distinct(StringComparer.Ordinal).Count()
            .ShouldBe(canonicalReceivers.Length);
        canonicalReceivers.Select(endpoint => endpoint.RoutePattern.RawText).ShouldContain("/replay-state");
        foreach (RouteEndpoint endpoint in canonicalReceivers)
        {
            endpoint.Metadata.ShouldContain(metadata => metadata.GetType().Name == "DomainRouteCatalogGuardMetadata");
        }
    }

    /// <summary>Checks the AD-19 invalid-input response for every installed query.</summary>
    [Fact]
    public void OnlyEstablishedCompatibilityQueriesRetainTheInvalidInput404()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddProjectsDomainService();
        using WebApplication app = builder.Build();

        string[] compatibility =
        [
            ProjectsServerModule.GetProjectContextQueryType,
            ProjectsServerModule.GetConversationStartSetupQueryType,
            ProjectsServerModule.ExplainContextSelectionQueryType,
            ResolveProjectChatBotAuthority.QueryType,
        ];
        string[] registered = app.Services.GetServices<IDomainQueryHandler>().Select(handler => handler.QueryType).ToArray();
        registered.ShouldContain(ListProjectsV2Query.QueryType);
        compatibility.ShouldAllBe(queryType => registered.Contains(queryType));
        IDomainQueryResponsePolicy policy = app.Services.GetRequiredService<IDomainQueryResponsePolicy>();
        foreach (string queryType in registered)
        {
            QueryEnvelope query = new("tenant-a", "projects", "projects", queryType, [], "correlation", "actor");
            int expected = compatibility.Contains(queryType) ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
            foreach (string reason in new[] { QueryAdapterFailureReason.InvalidEnvelope, QueryAdapterFailureReason.InvalidCursor })
            {
                IResult response = policy.CreateResponse(query, QueryResult.Failure(reason));
                response.ShouldBeAssignableTo<IStatusCodeHttpResult>().StatusCode.ShouldBe(expected);
            }

            IResult denied = policy.CreateResponse(query, QueryResult.Failure("safe-denial"));
            denied.ShouldBeAssignableTo<IStatusCodeHttpResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        }
    }

    /// <summary>Registers all declared adapters once and includes the extension in gateway readiness.</summary>
    [Fact]
    public async Task GatewayLoadsProjectsExtensionAndReportsHealthyReadiness()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventStoreExtensions(ExtensionConfiguration("1"));
        using ServiceProvider provider = services.BuildServiceProvider();

        var routeIds = provider.GetServices<IIdempotencyIntentAdapter>()
            .Cast<IDeclaredIdempotencyIntentAdapter>()
            .Select(adapter => adapter.Declaration.Route.RouteId)
            .Order(StringComparer.Ordinal);
        routeIds.ShouldBe(ProjectsRuntimeCatalogDeclarations.Commands
            .Select(command => command.Route.RouteId)
            .Order(StringComparer.Ordinal));

        HealthReport report = await provider.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(check => check.Tags.Contains("ready"), TestContext.Current.CancellationToken);
        report.Entries[EventStoreExtensionServiceCollectionExtensions.HealthCheckName].Status.ShouldBe(HealthStatus.Healthy);
    }

    /// <summary>Refuses an unsupported API major before registering Projects or allowing startup.</summary>
    [Fact]
    public void GatewayRefusesProjectsExtensionWithUnsupportedMajor()
    {
        var services = new ServiceCollection();
        var refused = Should.Throw<EventStoreExtensionException>(() => services.AddEventStoreExtensions(ExtensionConfiguration("2")));
        refused.Condition.ShouldBe(EventStoreExtensionConditions.UnsupportedMajor);
        refused.Message.ShouldContain("Hexalith.Projects.EventStoreExtensions");
        services.ShouldNotContain(service => service.ServiceType == typeof(IIdempotencyIntentAdapter));
    }

    private static IConfiguration ExtensionConfiguration(string apiMajor)
    {
        const string prefix = EventStoreExtensionServiceCollectionExtensions.ConfigurationSection + ":0:";
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [prefix + "PackageId"] = "Hexalith.Projects.EventStoreExtensions",
            [prefix + "ExtensionId"] = "projects",
            [prefix + "ModuleId"] = "projects",
            [prefix + "ApiMajor"] = apiMajor,
        }).Build();
    }

    /// <summary>Checks the manifest POST grants against the actual restricted SDK receiver.</summary>
    [Fact]
    public async Task ManifestGrantsMatchTheRestrictedSdkReceiver()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "hexalith.module-manifest.v2.json")));
        var domain = manifest.RootElement.GetProperty("modules").EnumerateArray().Single()
            .GetProperty("surfaces").GetProperty("interfaces").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "projects-domain");
        RouteCatalogDeclaredInvocation[] grants = domain.GetProperty("invocationRules").EnumerateArray()
            .Select(rule => new RouteCatalogDeclaredInvocation(
                rule.GetProperty("caller").GetString()!,
                rule.GetProperty("path").GetString()!.TrimStart('/'),
                rule.GetProperty("verbs").EnumerateArray().Select(verb => verb.GetString()!).ToArray()))
            .ToArray();
        grants.ShouldAllBe(grant => grant.CallerAppId == "eventstore");
        grants.Select(grant => grant.Method).ShouldContain("project/v2/reconcile");
        grants.Select(grant => grant.Method).ShouldContain("project/rebuild/part/v1");

        DirectoryInfo workspace = Directory.CreateTempSubdirectory("projects-sdk-grants-");
        try
        {
            CancellationToken token = TestContext.Current.CancellationToken;
            string accessControl = Path.Combine(workspace.FullName, "access-control.json");
            await File.WriteAllTextAsync(accessControl, JsonSerializer.Serialize(new
            {
                spec = new
                {
                    accessControl = new
                    {
                        policies = new[]
                        {
                            new { operations = grants.Select(grant => new { name = grant.Method, httpVerb = grant.HttpMethods, action = "allow" }) },
                        },
                    },
                },
            }), token);
            const string section = "EventStore:RouteCatalog:DevelopmentBootstrap:";
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["urls"] = "http://127.0.0.1:0",
                ["Projects:DevelopmentRouteCatalog:Enabled"] = "true",
                ["Authentication:JwtBearer:Authority"] = "https://catalog-fixture.example.test",
                ["Authentication:JwtBearer:Issuer"] = "https://catalog-fixture.example.test",
                ["Authentication:JwtBearer:Audience"] = "projects-fixture",
                [DaprAppChannelToken.ConfigurationKey] = "isolated-catalog-channel",
                [section + "Directory"] = workspace.FullName,
                [section + "AccessControlPath"] = accessControl,
                [section + "HostId"] = "projects",
                [section + "CoordinatorHostId"] = "eventstore",
                [section + "AttemptId"] = "projects-manifest-grants",
                [section + "RequiredHostIds:0"] = "projects",
                [section + "RequiredHostIds:1"] = "eventstore",
                [section + "DeclaredInvocations"] = JsonSerializer.Serialize(grants, JsonSerializerOptions.Web),
            });
            builder.AddProjectsDomainService();
            // This lane observes the real receiver fence without activating a shared catalog.
            builder.Services.Remove(builder.Services.Last(service => service.ServiceType == typeof(IHostedService)));
            WebApplication app = builder.Build();
            await using (app.ConfigureAwait(true))
            {
                app.UseEventStoreDomainService();
                app.MapProjectsHumanEndpoints();
                await app.StartAsync(token);
                var restricted = app.Services.GetRequiredService<FiniteRouteCatalogReceiver>().Observe();
                restricted.Endpoints.Where(endpoint => endpoint.Admitted)
                    .SelectMany(endpoint => endpoint.Methods.Select(method => (endpoint.Pattern, method))).Order()
                    .ShouldBe(grants.SelectMany(grant => grant.HttpMethods.Select(method => ("/" + grant.Method, method))).Order());
            }
        }
        finally
        {
            workspace.Delete(recursive: true);
        }
    }

    /// <summary>The actual compiled owner produces a complete schema-three catalog accepted by the SDK, without activating any authority.</summary>
    [Fact]
    public void CompleteGeneratedCatalogPassesTheSharedValidator()
    {
        var declarations = CompiledRouteCatalogDeclarations.Read([typeof(ProjectsRuntimeCatalogDeclarations).Assembly]);
        var operations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(ProjectsRuntimeCatalogDeclarations).Assembly]);
        var bindings = declarations.Select(route => new RouteCompositionBinding(route.RouteId, "projects",
            ProjectsRuntimeCatalogDeclarations.ReceiverMethods[route.RouteId], route.ContractVersion, false, "*")).ToArray();
        var policies = ProjectsRuntimeCatalogDeclarations.Commands.Select(command => DevelopmentIdempotencyBindings.Bind(command, 1)).ToArray();
        var input = new RouteCatalogGenerationInput(1, "Development", declarations, operations, bindings, policies);
        RouteCatalogEnvelope catalog = VersionedRouteCatalogBuilder.Build(input);
        RouteCatalogValidator.Validate(catalog);
        RouteCatalogCodec.Decode(RouteCatalogCodec.Encode(catalog), RouteCatalogCodec.ComputeRootDigest(catalog)).Routes.Count.ShouldBe(26);
        var incorrectlyInternal = declarations.Select(route => route.MessageType == ListProjectsQuery.QueryType
            ? route with { Execution = route.Execution! with { RequiresDelegatedContext = true } } : route).ToArray();
        Should.Throw<RouteCatalogValidationException>(() => VersionedRouteCatalogBuilder.Build(input with { Declarations = incorrectlyInternal }));
    }

    /// <summary>Signed public surfaces admit real user queries while owner protocols and delegation remain distinct.</summary>
    /// <param name="surface">The signed gateway surface.</param>
    [Theory]
    [InlineData("agent")]
    [InlineData("ui")]
    [InlineData("service")]
    public void SignedSurfacesAdmitPublicQueriesAndKeepOwnerProtocolsInternal(string surface)
    {
        using RSA rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        string token = handler.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = "https://signed-surface.example.test", Audience = "gateway-test",
            Subject = new ClaimsIdentity([new Claim("sub", "actor-one"), new Claim(RouteCatalogSurfacePolicy.ClaimType, surface)]),
            NotBefore = DateTime.UtcNow.AddSeconds(-1), Expires = DateTime.UtcNow.AddMinutes(1),
            SigningCredentials = new(key, SecurityAlgorithms.RsaSha256),
        });
        ClaimsPrincipal principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = "https://signed-surface.example.test", ValidAudience = "gateway-test", IssuerSigningKey = key,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256], RequireSignedTokens = true, ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
        }, out _);
        RouteCatalogSurfacePolicy.Surface(principal).ShouldBe(surface);
        var operations = ProjectsRuntimeCatalogDeclarations.Operations.ToDictionary(operation => operation.RouteId);
        foreach (var query in ProjectsRuntimeCatalogDeclarations.Routes.Where(route => route.Kind == "query"))
        {
            bool ownerOnly = query.MessageType == ResolveProjectChatBotAuthority.QueryType;
            operations[query.RouteId].Ingress.ShouldBe(ownerOnly ? "internal" : "public");
            RouteCatalogSurfacePolicy.Allows(operations[query.RouteId], principal).ShouldBe(!ownerOnly);
            query.Execution!.RequiresDelegatedContext.ShouldBe(ownerOnly);
        }
        foreach (var projection in ProjectsRuntimeCatalogDeclarations.Routes.Where(route => route.Kind == "projection"))
        {
            operations[projection.RouteId].Ingress.ShouldBe("internal");
            RouteCatalogSurfacePolicy.Allows(operations[projection.RouteId], principal).ShouldBeFalse();
        }
        principal.Identities.Single().RemoveClaim(principal.FindFirst(RouteCatalogSurfacePolicy.ClaimType)!);
        foreach (var query in ProjectsRuntimeCatalogDeclarations.Routes.Where(route => route.Kind == "query"))
            RouteCatalogSurfacePolicy.Allows(operations[query.RouteId], principal).ShouldBeFalse();
    }

    /// <summary>The read proof cannot promote legacy writers into direct agent operations or replace their typed admission policies.</summary>
    [Fact]
    public void WriterEligibilityRemainsFiniteAndRejectsDirectAgentInvocation()
    {
        string[] confirmation = [ProjectsServerModule.ArchiveProjectCommandType, ProjectsServerModule.RestoreProjectCommandType,
            ProjectsServerModule.SetProjectFolderCommandType, ProjectsServerModule.UnlinkFileReferenceCommandType,
            ProjectsServerModule.UnlinkMemoryCommandType, ProjectsServerModule.ConfirmProjectResolutionCommandType,
            ProjectsServerModule.GrantProjectChatBotAuthorityCommandType, ProjectsServerModule.RevokeProjectChatBotAuthorityCommandType];
        string[] uiOnly = [ProjectsServerModule.CreateProjectCommandType, ProjectsServerModule.UpdateProjectSetupCommandType,
            ProjectsServerModule.LinkFileReferenceCommandType, ProjectsServerModule.LinkMemoryCommandType];
        var agent = new ClaimsPrincipal(new ClaimsIdentity([new Claim(RouteCatalogSurfacePolicy.ClaimType, "agent")], "authenticated-test"));
        var ui = new ClaimsPrincipal(new ClaimsIdentity([new Claim(RouteCatalogSurfacePolicy.ClaimType, "ui")], "authenticated-test"));
        var commands = ProjectsRuntimeCatalogDeclarations.Operations.Where(operation => operation.Kind == "command").ToArray();
        commands.Select(operation => operation.MessageType!).Order().ShouldBe(confirmation.Concat(uiOnly).Order());
        foreach (var command in commands)
        {
            command.Eligibility.ShouldBe(confirmation.Contains(command.MessageType!) ? "confirmation-required" : "UI-only");
            RouteCatalogSurfacePolicy.Allows(command, agent).ShouldBeFalse();
            RouteCatalogSurfacePolicy.Allows(command, agent, verifiedOriginatingProof: true).ShouldBeFalse();
            RouteCatalogSurfacePolicy.Allows(command, ui).ShouldBeTrue();
        }
    }

    /// <summary>Typed canonical intent ignores JSON formatting and property order while binding every payload field and actor.</summary>
    [Fact]
    public void StandardIntentAdapterBindsCompleteTypedPayloadAndAuthenticatedActor()
    {
        IDeclaredIdempotencyIntentAdapter adapter = ProjectsIntentAdapters.Create()
            .Single(item => item.CommandType == ProjectsServerModule.CreateProjectCommandType);
        IdempotencyIntentCommand command = Command("{\"name\":\"Project\",\"description\":null,\"setupMetadata\":\"opaque\"}");
        var first = adapter.CreateIntent(command);
        var reordered = adapter.CreateIntent(command with
        {
            Payload = Encoding.UTF8.GetBytes("{ \"setupMetadata\": \"opaque\", \"name\": \"Project\" }"),
        });
        first.SemanticPayload.ShouldBe(reordered.SemanticPayload);
        adapter.CreateIntent(command with { AuthenticatedSubject = "another-actor" }).SemanticPayload.ShouldNotBe(first.SemanticPayload);
        adapter.CreateIntent(Command("{\"name\":\"Project\",\"setupMetadata\":\"changed\"}")).SemanticPayload.ShouldNotBe(first.SemanticPayload);
        Should.Throw<JsonException>(() => adapter.CreateIntent(Command("{\"name\":\"Project\",\"unexpected\":true}")));
        Should.Throw<InvalidOperationException>(() => adapter.CreateIntent(command with { AuthenticatedSubject = null }));
        ProjectsIntentAdapters.Create().Select(item => item.Declaration.Route.RouteId).Order(StringComparer.Ordinal)
            .ShouldBe(ProjectsRuntimeCatalogDeclarations.Commands.Select(item => item.Route.RouteId).Order(StringComparer.Ordinal));
    }

    /// <summary>Private catalog bootstrap can never enable itself outside Development or without the explicit profile.</summary>
    [Theory]
    [InlineData("Production", "true")]
    [InlineData("Development", "false")]
    public void PrivateBootstrapRejectsAnUnselectedOrProductionHost(string environment, string selected)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = Path.GetTempPath(),
            ["Projects:SupportedReads:Enabled"] = selected,
        });
        Should.Throw<InvalidOperationException>(() => builder.AddProjectsDevelopmentCatalog());
    }

    /// <summary>The executable refuses the anonymous Development bypass.</summary>
    [Fact]
    public void DomainServiceRefusesAnonymousDevelopment()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration["Authentication:JwtBearer:AllowAnonymousDevelopment"] = "true";
        Should.Throw<InvalidOperationException>(() => builder.AddProjectsDomainService());
    }

    /// <summary>Ordinary production registration leaves the existing runtime composition selected.</summary>
    [Fact]
    public void NoBootstrapKeepsOrdinaryHostComposition()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.AddProjectsDevelopmentCatalog().ShouldBeFalse();
    }

    /// <summary>Development receiver enrollment requires exactly one selected private or ordinary profile.</summary>
    /// <param name="reads">Whether the real-owner read profile is selected.</param>
    /// <param name="fixtures">Whether the separate compatibility profile is selected.</param>
    /// <param name="ordinary">Whether the ordinary local topology profile is selected.</param>
    /// <param name="accepted">Whether enrollment must be accepted.</param>
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, true, false)]
    public void PrivateBootstrapRequiresExactlyOneProfile(bool reads, bool fixtures, bool ordinary, bool accepted)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = Path.GetTempPath(),
            ["EventStore:RouteCatalog:DevelopmentBootstrap:HostId"] = "projects",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:CoordinatorHostId"] = "eventstore",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:AttemptId"] = "profile-selection",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RequiredHostIds:0"] = "projects",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RequiredHostIds:1"] = "eventstore",
            ["Projects:SupportedReads:Enabled"] = reads.ToString(),
            ["Projects:IsolatedLiveFixtures:Enabled"] = fixtures.ToString(),
            ["Projects:DevelopmentRouteCatalog:Enabled"] = ordinary.ToString(),
        });
        if (accepted)
        {
            builder.AddProjectsDevelopmentCatalog().ShouldBeTrue();
        }
        else
        {
            Should.Throw<InvalidOperationException>(() => builder.AddProjectsDevelopmentCatalog());
        }
    }

    private static IdempotencyIntentCommand Command(string payload) => new(ProjectsServerModule.CreateProjectCommandType,
        "tenant-one", "projects", "project-one", Encoding.UTF8.GetBytes(payload), null) { AuthenticatedSubject = "actor-one" };
}
