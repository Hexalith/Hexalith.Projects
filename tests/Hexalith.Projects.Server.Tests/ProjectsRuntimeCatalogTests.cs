namespace Hexalith.Projects.Server.Tests;

using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.IdentityModel.Tokens;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        builder.Services.AddProjectsServer();
        builder.Services.AddSingleton<ProjectsDevelopmentCatalogProfile>();
        builder.Services.AddEventStoreDomainServiceSecurity(builder.Configuration);
        builder.Services.AddProjectsServerRuntimeInfrastructure();
        builder.Services.AddSingleton(new DiscoveryResult(
            [new(typeof(ProjectsDomainProcessor), "projects", typeof(ProjectState), DomainKind.Aggregate)], []));
        builder.Services.AddSingleton(new LegacyProjectionCatalogRegistration(ProjectsRuntimeCatalogDeclarations.Routes
            .Where(route => route.Kind == "projection" && route.ProjectionType == "project").ToArray()));
        using WebApplication app = builder.Build();
        app.MapProjectsServerEndpoints();

        var declarations = CompiledRouteCatalogDeclarations.Read([typeof(ProjectsRuntimeCatalogDeclarations).Assembly]);
        declarations.Count.ShouldBe(23);
        var processor = app.Services.GetRequiredKeyedService<IDomainProcessor>("projects")
            .ShouldBeAssignableTo<IDomainCommandPayloadInventory>();
        processor.CommandPayloadTypes.Select(type => type.FullName).Order(StringComparer.Ordinal)
            .ShouldBe(declarations.Where(route => route.Kind == "command").Select(route => route.Execution!.HandlerType).Order(StringComparer.Ordinal));
        app.Services.GetServices<IDomainQueryHandler>().Select(handler => handler.QueryType).Order(StringComparer.Ordinal)
            .ShouldBe(declarations.Where(route => route.Kind == "query").Select(route => route.MessageType!).Order(StringComparer.Ordinal));
        app.Services.GetServices<IAsyncDomainProjectionHandler>().Select(handler => handler.ProjectionType).Order(StringComparer.Ordinal)
            .ShouldBe(new[] { "conversation-start-setup", "project-reads" });
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        DevelopmentDomainRouteCatalog.Targets(app.Services, routes, declarations).Count.ShouldBe(23);
        CompiledRouteCatalogDeclarations.ReadOperations([typeof(ProjectsRuntimeCatalogDeclarations).Assembly]).Count.ShouldBe(23);
        EventStoreDomainServiceEndpointInventory.Validate(routes, fallbackPolicy: null).ShouldBeEmpty();
        foreach (RouteEndpoint endpoint in routes.OfType<RouteEndpoint>().Where(endpoint =>
            EventStoreDomainServiceRoutes.TryGet(endpoint.RoutePattern.RawText, out _)))
        {
            endpoint.Metadata.ShouldContain(metadata => metadata.GetType().Name == "DomainRouteCatalogGuardMetadata");
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
        RouteCatalogCodec.Decode(RouteCatalogCodec.Encode(catalog), RouteCatalogCodec.ComputeRootDigest(catalog)).Routes.Count.ShouldBe(23);
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

    /// <summary>Ordinary production registration leaves the existing runtime composition selected.</summary>
    [Fact]
    public void NoBootstrapKeepsOrdinaryHostComposition()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.AddProjectsDevelopmentCatalog().ShouldBeFalse();
        builder.Services.ShouldNotContain(service => service.ServiceType == typeof(ProjectsDevelopmentCatalogProfile));
    }

    private static IdempotencyIntentCommand Command(string payload) => new(ProjectsServerModule.CreateProjectCommandType,
        "tenant-one", "projects", "project-one", Encoding.UTF8.GetBytes(payload), null) { AuthenticatedSubject = "actor-one" };
}
