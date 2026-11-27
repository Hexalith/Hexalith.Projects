#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapr.Client;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Folders.Authorization;
using Hexalith.Folders.Contracts;
using Hexalith.Folders.Projections.TenantAccess;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Projections.ProjectReads;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectGatewayHttpTests
{
    private static void AddCutoverHumanAuthentication(WebApplicationBuilder builder)
        => builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = Issuer, ValidateAudience = true,
                ValidAudience = Audience, ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)) };
        });

    [Fact]
    public async Task NativeConversationMembershipUsesTheRealGatewayOwnerAndPersistedNativeSource()
    {
        string http = Environment.GetEnvironmentVariable("STORY61_DAPR_HTTP_ENDPOINT")!;
        string grpc = Environment.GetEnvironmentVariable("STORY61_DAPR_GRPC_ENDPOINT")!;
        Assert.SkipUnless(http is not null && grpc is not null, "Requires owned isolated Dapr.");
        string tenant = "cutover-native-" + Guid.NewGuid().ToString("N");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using RSA key = RSA.Create(2048);
        using ProjectReadDenialObserver denialObserver = new();
        var routes = new GatewayFixtureLocalRoutes(); var heads = new GatewayFixtureOwnerHeads();
        WebApplication folders = await StartFoldersAsync(http!, grpc!, routes, heads, now, "*", denialObserver);
        await using var foldersLifetime = folders.ConfigureAwait(true); routes.Add("folders", folders);
        WebApplication conversations = await StartConversationsAsync(http!, grpc!, routes, denialObserver);
        await using var conversationsLifetime = conversations.ConfigureAwait(true); routes.Add("conversations", conversations);
        WebApplication projects = await StartProjectsAsync(http!, grpc!, routes, key, now, "*", denialObserver);
        await using var projectsLifetime = projects.ConfigureAwait(true); routes.Add("projects", projects);
        Exception? gatewayFailure = null;
        WebApplication gateway = await StartGatewayAsync(http!, grpc!, routes, key, "*", denialObserver, exception => gatewayFailure = exception);
        await using var gatewayLifetime = gateway.ConfigureAwait(true); routes.Add("eventstore", gateway);
        var projectTenant = new ProjectTenantAccessProjection { TenantId = tenant, Enabled = true, Watermark = 1,
            ProjectionWatermark = tenant + ":1", LastEventTimestamp = now };
        projectTenant.Principals["actor-1"] = new("actor-1", "TenantOwner");
        await projects.Services.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(projectTenant, TestContext.Current.CancellationToken);
        var nativeTenant = new TenantLocalState { TenantId = tenant, Status = TenantStatus.Active };
        nativeTenant.Members["actor-1"] = TenantRole.TenantReader;
        await conversations.Services.GetRequiredService<ITenantProjectionStore>().SaveAsync(nativeTenant, TestContext.Current.CancellationToken);
        await SeedCutoverConversationAsync(conversations, tenant, "bound-conversation", "project-a", now);
        var store = projects.Services.GetRequiredService<IReadModelStore>();
        ProjectionRequest history = SupportedProjectReadFixture.Request("project-a", 100);
        history = history with { TenantId = tenant, Events = history.Events.Select(item => item with
            { Payload = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(item.Payload).Replace("tenant-a", tenant, StringComparison.Ordinal)) }).ToArray() };
        await new ConversationStartSetupProjectionHandler(store).ProjectAsync(history, "native-setup", TestContext.Current.CancellationToken);
        await SeedCutoverFoldersAsync(folders, heads, tenant, now, ["project-a"]);
        using var client = new HttpClient { BaseAddress = new Uri(gateway.Urls.Single()) };
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
        foreach (var operation in new[] { (ProjectsServerModule.GetProjectContextQueryType, "context"),
            (ProjectsServerModule.GetConversationStartSetupQueryType, "start"), (ProjectsServerModule.ExplainContextSelectionQueryType, "explain") })
        {
            var request = new SubmitQueryRequest(tenant, "projects", "project-a", operation.Item1,
                Payload: JsonSerializer.SerializeToElement(new { projectId = "project-a", conversationId = "bound-conversation" }));
            using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/queries", request, TestContext.Current.CancellationToken);
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, raw + "\n" + gatewayFailure?.GetType().FullName
                + "\n" + gatewayFailure?.StackTrace + "\n" + gatewayFailure?.InnerException?.GetType().FullName
                + "\n" + string.Join(";", routes.TransportResponses));
            var result = JsonSerializer.Deserialize<SubmitQueryResponse>(raw, JsonSerializerOptions.Web)!;
            JsonElement context = operation.Item2 == "explain" ? result.Payload.GetProperty("context") : result.Payload;
            context.GetProperty("snapshot").GetProperty("responseState").GetString().ShouldBe("Complete");
            raw.ShouldNotContain("private-native-");
        }
        SubmitQueryRequest Setup(string projectId) => new(tenant, "projects", projectId, ProjectsServerModule.GetConversationStartSetupQueryType,
            Payload: JsonSerializer.SerializeToElement(new GetConversationStartSetupQuery(projectId) { ConversationId = "bound-conversation" }, JsonSerializerOptions.Web));
        int dispatchesBeforeDenials = routes.ProjectQueries.Count;
        for (int round = 0; round < 3; round++)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
            var absent = await ObserveDenialAsync(client, Setup("missing-project"), denialObserver).ConfigureAwait(true);
            client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false, actorId: "denied-actor"));
            var denied = await ObserveDenialAsync(client, Setup("project-a"), denialObserver).ConfigureAwait(true);
            AssertSameDenialObservations(absent, denied);
        }

        routes.ProjectQueries.Skip(dispatchesBeforeDenials)
            .Where(query => query.QueryType == ProjectsServerModule.GetConversationStartSetupQueryType)
            .Select(query => (query.AggregateId, query.OriginalActorId ?? string.Empty))
            .ShouldBe(Enumerable.Repeat(new[] { ("missing-project", "actor-1"), ("project-a", "denied-actor") }, 3).SelectMany(pair => pair));

        routes.OwnerQueries.ShouldBeGreaterThanOrEqualTo(3);
        routes.HumanBearerForwarded.ShouldBeFalse();
        await ObserveCutoverMatrixAsync(gateway, projects, folders, conversations, routes, heads, tenant, now, history, http!);
        await gateway.StopAsync(TestContext.Current.CancellationToken); await projects.StopAsync(TestContext.Current.CancellationToken);
        await conversations.StopAsync(TestContext.Current.CancellationToken); await folders.StopAsync(TestContext.Current.CancellationToken);
    }
    private static async Task SeedCutoverConversationAsync(WebApplication conversations, string tenant, string conversationId, string projectId, DateTimeOffset now)
    {
        var fresh = new ProjectionFreshnessV1(SchemaVersion.Current, "native-generation", 17, now, now, TimeSpan.Zero,
            false, ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current);
        var native = new ConversationProjectedReadModels(
            new(SchemaVersion.Current, new(tenant), new(conversationId), fresh, "Open", "private-native-label", ProjectId: new(projectId)),
            new(SchemaVersion.Current, new(tenant), new(conversationId), fresh, "Open", "private-native-label", ProjectId: new(projectId),
                Attributes: new Dictionary<string, string> { ["private"] = "private-native-attributes" })) { DispatchId = "native-original-17" };

        ConversationResolutionMetadataReadModel advisoryMetadata = new(new(tenant), new(conversationId),
            new(conversationId, native.Detail.ProjectId!.Value, native.Detail.Label, native.Detail.LifecycleState), fresh,
            "native-original-17", new string('a', 64), true, ConversationResolutionScoringLabel.OwnerClassification)
            { LabelPurpose = ConversationResolutionScoringLabel.SupportedPurpose };
        Type persistenceType = typeof(ConversationProjectionReadModelWriter).Assembly.GetType(
            "Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataPersistence")!;
        object persistence = Activator.CreateInstance(persistenceType, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, [conversations.Services.GetRequiredService<DaprReadModelStore>()], null)!;
        await ((Task)persistenceType.GetMethod("MarkPendingAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(persistence, [advisoryMetadata, TestContext.Current.CancellationToken])!).ConfigureAwait(false);
        await ((Task)typeof(ConversationProjectionReadModelWriter).GetMethod("PersistGenerationAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(conversations.Services.GetRequiredService<ConversationProjectionReadModelWriter>(), [native, TestContext.Current.CancellationToken])!).ConfigureAwait(false);
        foreach (string method in new[] { "WriteMetadataAsync", "CompleteAsync" })
            await ((Task)persistenceType.GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(persistence, [advisoryMetadata, TestContext.Current.CancellationToken])!).ConfigureAwait(false);
    }
}
#endif
