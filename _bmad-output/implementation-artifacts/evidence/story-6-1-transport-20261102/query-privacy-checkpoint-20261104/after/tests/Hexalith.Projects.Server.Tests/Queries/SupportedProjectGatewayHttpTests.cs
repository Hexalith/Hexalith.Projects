#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapr.Client;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Microsoft.AspNetCore.Http;
using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ErrorHandling;
using Hexalith.EventStore.Queries;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Folders;
using Hexalith.Folders.Aggregates.Folder;
using Hexalith.Folders.Authorization;
using Hexalith.Folders.Contracts;
using Hexalith.Folders.Projections.TenantAccess;
using Hexalith.Folders.Server;
using Hexalith.Folders.Server.Authentication;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

/// <summary>Local bound-signing source lane: real gateway/SDK/owner HTTP and Dapr persistence; loopback replaces service discovery only.</summary>
/// <param name="output">The native per-test output for closed transport observations.</param>
[Collection("ProjectReadTelemetry")]
public sealed partial class SupportedProjectGatewayHttpTests(ITestOutputHelper output)
{
    private const string Issuer = "story61-bound-signing";
    private const string Audience = "configured-read-audience";
    private const string Channel = "story61-owned-local-channel";
    private static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>Uses the SDK's actual named query client while preserving fixture discovery and workload signing.</summary>
    [Fact]
    public async Task NamedQueryClientUsesFixtureReceiverAndRealWorkloadAssertion()
    {
        int directDaprCalls = 0, ownerCalls = 0;
        var daprBuilder = WebApplication.CreateBuilder();
        daprBuilder.Configuration["urls"] = "http://127.0.0.1:0";
        await using WebApplication daprTrap = daprBuilder.Build();
        daprTrap.MapFallback(() => { Interlocked.Increment(ref directDaprCalls); return Results.StatusCode(500); });
        await daprTrap.StartAsync(TestContext.Current.CancellationToken);
        var receiverBuilder = WebApplication.CreateBuilder();
        receiverBuilder.Configuration["urls"] = "http://127.0.0.1:0";
        await using WebApplication receiver = receiverBuilder.Build();
        receiver.MapPost("/query", (HttpContext context) =>
        {
            Interlocked.Increment(ref ownerCalls);
            context.Request.Headers.Authorization.ShouldBeEmpty();
            context.Request.Headers[DaprAppChannelToken.HeaderName].ToString().ShouldBe(Channel);
            string assertion = context.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName].Single()!;
            ClaimsPrincipal principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(assertion, new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = Issuer, ValidateAudience = true, ValidAudience = "projects",
                ValidateLifetime = true, ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            }, out _);
            principal.FindFirst(EventStoreWorkloadAuthenticationDefaults.CallerClaimType)!.Value.ShouldBe("eventstore");
            principal.FindFirst(EventStoreWorkloadAuthenticationDefaults.OperationClaimType)!.Value.ShouldBe(EventStoreWorkloadOperations.DomainServiceQuery);
            return Results.Ok(QueryResult.FromPayload(JsonSerializer.SerializeToElement(new { observed = true })));
        });
        await receiver.StartAsync(TestContext.Current.CancellationToken);
        var routes = new GatewayFixtureLocalRoutes();
        routes.Add("projects", receiver);
        using RSA ownerKey = RSA.Create(2048);
        WebApplication gateway = await StartGatewayAsync(daprTrap.Urls.Single(), "http://127.0.0.1:1", routes, ownerKey, "tenant-a");
        await using var lifetime = gateway.ConfigureAwait(true);
        QueryResult result = await gateway.Services.GetRequiredService<IDomainQueryInvoker>().InvokeAsync(
            new QueryEnvelope("tenant-a", "projects", "projects", ListProjectsQuery.QueryType, "{}"u8.ToArray(), "transport-control", "actor-1"),
            TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        result.GetPayload().GetProperty("observed").GetBoolean().ShouldBeTrue();
        ownerCalls.ShouldBe(1);
        directDaprCalls.ShouldBe(0);
    }

    /// <summary>Exercises the real Folder receiver guard before any owner read or persistence call.</summary>
    [Fact]
    public async Task FolderFixtureRequiresCommittedGenerationBeforeOwnerDispatch()
    {
        const string tenant = "catalog-receiver-control";
        var routes = new GatewayFixtureLocalRoutes();
        WebApplication folders = await StartFoldersAsync("http://127.0.0.1:1", "http://127.0.0.1:1", routes,
            new GatewayFixtureOwnerHeads(), DateTimeOffset.UtcNow, tenant);
        await using var foldersLifetime = folders.ConfigureAwait(true);
        using RSA ownerKey = RSA.Create(2048);
        WebApplication gateway = await StartGatewayAsync("http://127.0.0.1:1", "http://127.0.0.1:1", routes, ownerKey, tenant);
        await using var gatewayLifetime = gateway.ConfigureAwait(true);
        string assertion = (await gateway.Services.GetRequiredService<IWorkloadAssertionIssuer>().IssueAsync(
            new("folders", EventStoreWorkloadOperations.DomainServiceQuery), TestContext.Current.CancellationToken))!;
        ClaimsPrincipal service = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(assertion, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = Issuer, ValidateAudience = true, ValidAudience = "folders",
            ValidateLifetime = true, ValidateIssuerSigningKey = true, RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        }, out _);
        service.FindAll(RouteCatalogSurfacePolicy.ClaimType).Select(claim => claim.Value).ShouldBe(["service"]);
        var committed = await folders.Services.GetRequiredService<IRouteCatalogAuthority>().ReadbackAsync(TestContext.Current.CancellationToken);
        string differentRoot = (committed.RootDigest[0] == 'f' ? "e" : "f") + committed.RootDigest[1..];
        long differentEpoch = checked(committed.Epoch + 1);
        differentRoot.ShouldNotBe(committed.RootDigest);
        differentEpoch.ShouldNotBe(committed.Epoch);
        using HttpClient client = new() { BaseAddress = new(folders.Urls.Single()) };
        foreach (bool differentGeneration in new[] { false, true })
        {
            using HttpRequestMessage request = new(HttpMethod.Post, "/query")
            {
                Content = JsonContent.Create(new QueryEnvelope(tenant, "folders", "folder-a", FolderOwnerReadQuery.QueryType,
                    "{}"u8.ToArray(), "receiver-control", "actor-1")),
            };
            request.Headers.Add(DaprAppChannelToken.HeaderName, Channel);
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
            if (differentGeneration)
            {
                request.Headers.Add(RouteCatalogInvocationHandler.RootHeader, differentRoot);
                request.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, differentEpoch.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }
    }

    [Fact]
    public async Task GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures()
    {
        string? http = Environment.GetEnvironmentVariable("STORY61_DAPR_HTTP_ENDPOINT"), grpc = Environment.GetEnvironmentVariable("STORY61_DAPR_GRPC_ENDPOINT");
        Assert.SkipUnless(http is not null && grpc is not null, "Requires the isolated Story 6.1 Dapr source lane.");
        string tenant = "gw-" + Guid.NewGuid().ToString("N");
        string foreignTenant = tenant + "-foreign";
        const string foreignOnlyProject = "foreign-only-project";
        using RSA ownerKey = RSA.Create(2048);
        var transport = new GatewayFixtureLocalRoutes();
        var heads = new GatewayFixtureOwnerHeads();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using ProjectReadDenialObserver denialObserver = new();
        WebApplication folders = await StartFoldersAsync(http!, grpc!, transport, heads, now, tenant, denialObserver).ConfigureAwait(true);
        await using var foldersLifetime = folders.ConfigureAwait(true);
        transport.Add("folders", folders);
        WebApplication conversations = await StartConversationsAsync(http!, grpc!, transport, denialObserver).ConfigureAwait(true);
        await using var conversationsLifetime = conversations.ConfigureAwait(true);
        transport.Add("conversations", conversations);
        WebApplication projects = await StartProjectsAsync(http!, grpc!, transport, ownerKey, now, denialObserver).ConfigureAwait(true);
        await using var projectsLifetime = projects.ConfigureAwait(true);
        transport.Add("projects", projects);
        WebApplication gateway = await StartGatewayAsync(http!, grpc!, transport, ownerKey, tenant, denialObserver).ConfigureAwait(true);
        await using var gatewayLifetime = gateway.ConfigureAwait(true);
        transport.Add("eventstore", gateway);
        var tenantProjection = new ProjectTenantAccessProjection { TenantId = tenant, Enabled = true, Watermark = 1,
            ProjectionWatermark = tenant + ":1", LastEventTimestamp = now };
        tenantProjection.Principals["actor-1"] = new("actor-1", "TenantOwner");
        await projects.Services.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(tenantProjection, TestContext.Current.CancellationToken);
        var folderTenant = new FolderTenantAccessProjection { TenantId = tenant, Enabled = true, Watermark = 1,
            ProjectionWatermark = tenant + ":1", LastEventTimestamp = now };
        folderTenant.Principals["actor-1"] = new("actor-1", "TenantOwner");
        await folders.Services.GetRequiredService<IFolderTenantAccessProjectionStore>().SaveAsync(folderTenant, TestContext.Current.CancellationToken);
        IWorkloadAssertionIssuer producer = gateway.Services.GetRequiredService<IWorkloadAssertionIssuer>();
        using HttpClient platform = gateway.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
        using DaprClient dapr = new DaprClientBuilder().UseHttpEndpoint(http!).UseGrpcEndpoint(grpc!).Build();
        string[] folderQueries = folders.Services.GetServices<IDomainQueryHandler>().Where(handler => handler.Domain == "folders")
            .Select(handler => handler.QueryType).Distinct(StringComparer.Ordinal).ToArray();
        folderQueries.ShouldContain(FolderOwnerReadQuery.QueryType);
        await dapr.SaveStateAsync("statestore", "admin:query-types:folders", folderQueries, cancellationToken: TestContext.Current.CancellationToken);
        foreach (string domain in new[] { "projects" })
        {
            using HttpRequestMessage metadataRequest = dapr.CreateInvokeMethodRequest(domain, "admin/operational-index-metadata",
                new AdminOperationalIndexMetadata.Request([domain]) { AppId = domain, ServiceVersion = "v1" });
            using HttpResponseMessage metadataResponse = await platform.SendAsync(metadataRequest, TestContext.Current.CancellationToken);
            metadataResponse.StatusCode.ShouldBe(HttpStatusCode.OK, domain + ": " + await metadataResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            AdminOperationalIndexMetadata.Response metadata = (await metadataResponse.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken))!;
            var declared = metadata.Domains.Single(item => item.Domain == domain);
            await dapr.SaveStateAsync("statestore", "admin:query-types:" + domain, declared.QueryTypes, cancellationToken: TestContext.Current.CancellationToken);
            if (domain == "folders") declared.QueryTypes.ShouldContain(FolderOwnerReadQuery.QueryType);
            if (domain == "projects")
            {
                foreach (var (targetTenant, id) in new[] { (tenant, "project-a"), (tenant, "project-b"), (foreignTenant, foreignOnlyProject) })
                {
                    ProjectionRequest seed = SupportedProjectReadFixture.Request(id);
                    seed = seed with { TenantId = targetTenant, Events = seed.Events.Select(item => item with
                        { Payload = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(item.Payload).Replace("tenant-a", targetTenant, StringComparison.Ordinal)) }).ToArray() };
                    using HttpRequestMessage dispatch = dapr.CreateInvokeMethodRequest("projects", "project/v2", new ProjectionDispatchRequest(seed,
                        ["project-reads"], "gateway-seed-" + id, metadata.CatalogFingerprint!));
                    using HttpResponseMessage projected = await platform.SendAsync(dispatch, TestContext.Current.CancellationToken);
                    projected.EnsureSuccessStatusCode();
                    (await projected.Content.ReadFromJsonAsync<ProjectionDispatchResponse>(TestContext.Current.CancellationToken))!.Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
                }
            }
        }
        EventStoreFolderEffectivePermissionsReadModel owner = folders.Services.GetRequiredService<EventStoreFolderEffectivePermissionsReadModel>();
        foreach (string id in new[] { "folder-project-a", "folder-project-b" })
        {
            await owner.ApplyAsync(tenant, id, "created", 1, "created", new FolderCreated(tenant, tenant, id, "Owner label", null, null, [],
                FolderLifecycleState.Active, FolderRepositoryBindingState.Unbound, "actor-1", "corr", "task", "create", "digest", now), TestContext.Current.CancellationToken);
            await owner.ApplyAsync(tenant, id, "granted", 2, "granted", new FolderAccessGranted(tenant, tenant, id, FolderAccessPrincipalKind.User,
                "actor-1", "read_metadata", "actor-1", "corr", "task", "grant", "digest", 2, now), TestContext.Current.CancellationToken);
            heads.Set(tenant, id, 2);
        }
        using HttpClient client = new() { BaseAddress = new Uri(gateway.Urls.Single()) };
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
        SubmitQueryRequest Open(object payload, string id = "project-a") => new(tenant, "projects", id, GetProjectQuery.QueryType, Payload: JsonSerializer.SerializeToElement(payload, JsonSerializerOptions.Web));
        SubmitQueryRequest List(ListProjectsQuery payload) => new(tenant, "projects", "projects", ListProjectsQuery.QueryType, Payload: JsonSerializer.SerializeToElement(payload, JsonSerializerOptions.Web));
        // The advisory lane exercises the real owner handler, ordinary owner evaluator and SDK/Dapr store.
        var conversationTenants = conversations.Services.GetRequiredService<ITenantProjectionStore>();
        var conversationTenant = new TenantLocalState { TenantId = tenant, Status = TenantStatus.Active };
        conversationTenant.Members["actor-1"] = TenantRole.TenantReader;
        await conversationTenants.SaveAsync(conversationTenant, TestContext.Current.CancellationToken);
        // Verify the production access facade carries the configured signal into its permission decision.
        var conversationPermission = await conversations.Services.GetRequiredService<IConversationTenantAccessService>().CheckAccessAsync(
            ConversationTenantAccessRequirement.Read, new(tenant), "actor-1", cancellationToken: TestContext.Current.CancellationToken);
        conversationPermission.IsAllowed.ShouldBeTrue(); conversationPermission.ProjectionVersion.ShouldBe(1);
        conversationPermission.ProjectionWatermark.ShouldBe(tenant + ":1");
        await dapr.SaveStateAsync("statestore", "admin:query-types:conversations", new[] { GetConversationAdvisoryMetadataQuery.QueryType }, cancellationToken: TestContext.Current.CancellationToken);
        string metadataKey = "projection:conversations-resolution:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tenant)))
            + ":" + Convert.ToHexStringLower(SHA256.HashData("conversation-a"u8));
        ConversationResolutionMetadataReadModel advisoryMetadata = new(new(tenant), new("conversation-a"),
            new("conversation-a", "project-a", "Project project-b", "Open"),
            new(Hexalith.Conversations.Contracts.Versioning.SchemaVersion.Current, "observed", 1, now.AddMinutes(-5), now.AddMinutes(-4),
                TimeSpan.FromMinutes(1), true, Hexalith.Conversations.Contracts.TrustStates.ProjectionTrustState.Stale,
                Hexalith.Conversations.Contracts.Projections.ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            "committed-advisory-fixture", new string('a', 64), true, ConversationResolutionScoringLabel.OwnerClassification)
            { LabelPurpose = ConversationResolutionScoringLabel.SupportedPurpose };
        await ReadModelWritePolicy.UpdateAsync<ConversationResolutionMetadataReadModel>(conversations.Services.GetRequiredService<IReadModelStore>(),
            "statestore", metadataKey, _ => advisoryMetadata, cancellationToken: TestContext.Current.CancellationToken);
        string[] observedKeys = [metadataKey, GatewayFixtureConversationTenants.Key(tenant), ProjectReadModelKeys.Index(tenant),
            ProjectReadModelKeys.Detail(tenant, "project-a"), ProjectReadModelKeys.Detail(tenant, "project-b")];
        using HttpClient stateClient = new() { BaseAddress = new Uri(http!) };
        async Task<byte[][]> StoreBytesAsync() => await Task.WhenAll(observedKeys.Select(key =>
            stateClient.GetByteArrayAsync("/v1.0/state/statestore/" + Uri.EscapeDataString(key), TestContext.Current.CancellationToken))).ConfigureAwait(false);
        byte[][] beforeAdvisory = await StoreBytesAsync();
        beforeAdvisory.ShouldAllBe(bytes => bytes.Length > 0);
        SubmitQueryRequest SuggestConversation(string id) => new(tenant, "projects", "projects", ResolveProjectFromConversationQuery.QueryType,
            Payload: JsonSerializer.SerializeToElement(new ResolveProjectFromConversationQuery(id), JsonSerializerOptions.Web));
        SubmitQueryRequest SuggestAttachments(params ProjectAdvisoryAttachment[] inputs) => new(tenant, "projects", "projects", ResolveProjectFromAttachmentsQuery.QueryType,
            Payload: JsonSerializer.SerializeToElement(new ResolveProjectFromAttachmentsQuery(inputs), JsonSerializerOptions.Web));
        var conversationSuggestions = await ReadAsync<ProjectAdvisoryResolutionResponse>(client, SuggestConversation("conversation-a"));
        output.WriteLine("Actual symmetric fixture transport responses=" + string.Join(',', transport.TransportResponses));
        conversationSuggestions.Candidates.Select(item => item.ProjectId).ShouldBe(["project-a", "project-b"]);
        conversationSuggestions.Candidates.Select(item => item.Score).ShouldBe([50, 20]); conversationSuggestions.Incomplete.ShouldBeFalse();
        var folderSuggestions = await ReadAsync<ProjectAdvisoryResolutionResponse>(client, SuggestAttachments(new ProjectAdvisoryAttachment("folder", "folder-project-b")));
        folderSuggestions.Candidates.Single().ProjectId.ShouldBe("project-b"); folderSuggestions.Candidates.Single().Score.ShouldBe(45);
        var files = await ReadAsync<ProjectAdvisoryResolutionResponse>(client, SuggestAttachments(new ProjectAdvisoryAttachment("file", "opaque-file")));
        files.Incomplete.ShouldBeTrue(); files.Result.ShouldBeNull(); files.UnsupportedInputKinds.ShouldBe(["file"]);
        byte[][] afterAdvisory = await StoreBytesAsync();
        for (int i = 0; i < observedKeys.Length; i++) afterAdvisory[i].ShouldBe(beforeAdvisory[i]);
        string absentConversation = await StatusAsync(client, SuggestConversation("missing"), HttpStatusCode.NotFound);
        conversationTenant.Members.Clear(); await conversationTenants.SaveAsync(conversationTenant, TestContext.Current.CancellationToken);
        string deniedConversation = await StatusAsync(client, SuggestConversation("conversation-a"), HttpStatusCode.NotFound);
        JsonDocument.Parse(absentConversation).RootElement.GetProperty("detail").GetString().ShouldBe(
            JsonDocument.Parse(deniedConversation).RootElement.GetProperty("detail").GetString());
        GetProjectResponse opened = await ReadAsync<GetProjectResponse>(client, Open(new GetProjectQuery()));
        opened.Name.ShouldBeNull();
        ProjectListPage first = await ReadAsync<ProjectListPage>(client, List(new(PageSize: 1)));
        first.Items.Single().ProjectId.ShouldBe("project-a"); first.Cursor.ShouldNotBeNull();
        (await ReadAsync<ProjectListPage>(client, List(new(Cursor: first.Cursor, PageSize: 1)))).Items.Single().ProjectId.ShouldBe("project-b");
        await StatusAsync(client, List(new(Cursor: "invalid")), HttpStatusCode.BadRequest);
        await StatusAsync(client, Open(new[] { 1 }), HttpStatusCode.BadRequest);
        await StatusAsync(client, Open(new GetProjectQuery(true)), HttpStatusCode.NotFound);
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: true));
        (await ReadAsync<GetProjectResponse>(client, Open(new GetProjectQuery(true)))).Name.ShouldBe("Project project-a");
        (await ReadAsync<ProjectListPage>(client, List(new(InspectDescriptiveMetadata: true)))).Items.ShouldAllBe(item => item.Name != null);
        // Real owner ACL revocation, then current stream-head lag/outage: neither can be hidden by inspection.
        await owner.ApplyAsync(tenant, "folder-project-a", "revoked", 3, "revoked", new FolderAccessRevoked(tenant, tenant, "folder-project-a",
            FolderAccessPrincipalKind.User, "actor-1", "read_metadata", "actor-1", "corr", "task", "revoke", "digest", 3, now), TestContext.Current.CancellationToken);
        heads.Set(tenant, "folder-project-a", 3);
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false, projectUser: true));
        IReadModelStore projectsStore = projects.Services.GetRequiredService<IReadModelStore>();
        (await projectsStore.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Detail(tenant, foreignOnlyProject), TestContext.Current.CancellationToken)).Value.ShouldBeNull();
        ProjectReadModel foreignModel = (await projectsStore.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Detail(foreignTenant, foreignOnlyProject), TestContext.Current.CancellationToken)).Value!;
        foreignModel.ShouldNotBeNull(); foreignModel.TenantId.ShouldBe(foreignTenant); foreignModel.ProjectId.ShouldBe(foreignOnlyProject);
        ProjectReadIndex foreignIndex = (await projectsStore.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Index(foreignTenant), TestContext.Current.CancellationToken)).Value!;
        foreignIndex.ShouldNotBeNull(); foreignIndex.TenantId.ShouldBe(foreignTenant); foreignIndex.ProjectIds.ShouldBe([foreignOnlyProject]);
        string[] foreignKeys = [ProjectReadModelKeys.Detail(foreignTenant, foreignOnlyProject), ProjectReadModelKeys.Index(foreignTenant)];
        async Task<(byte[] Bytes, string ETag)[]> ForeignStateAsync()
        {
            List<(byte[], string)> values = [];
            foreach (string key in foreignKeys)
            {
                using HttpResponseMessage state = await stateClient.GetAsync("/v1.0/state/statestore/" + Uri.EscapeDataString(key), TestContext.Current.CancellationToken).ConfigureAwait(false);
                state.EnsureSuccessStatusCode();
                byte[] bytes = await state.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                bytes.ShouldNotBeEmpty(); state.Headers.TryGetValues("ETag", out var versions).ShouldBeTrue();
                string version = versions!.Single(); version.ShouldNotBeNullOrWhiteSpace();
                values.Add((bytes, version));
            }
            return values.ToArray();
        }
        var foreignBefore = await ForeignStateAsync();
        var denied = await ObserveDenialAsync(client, Open(new GetProjectQuery()), denialObserver);
        var absent = await ObserveDenialAsync(client, Open(new GetProjectQuery(), "absent"), denialObserver);
        var foreignOnly = await ObserveDenialAsync(client, Open(new GetProjectQuery(), foreignOnlyProject), denialObserver);
        AssertSameDenialObservations(denied, absent);
        AssertSameDenialObservations(absent, foreignOnly);
        List<object> timingSamples = [];
        for (int round = 0; round < 4; round++)
        {
            for (int offset = 0; offset < 3; offset++)
            {
                int selected = (round + offset) % 3;
                string kind = selected == 0 ? "Denied" : selected == 1 ? "Absent" : "ForeignOnly";
                string project = selected == 0 ? "project-a" : selected == 1 ? "absent" : foreignOnlyProject;
                var observed = await ObserveDenialAsync(client, Open(new GetProjectQuery(), project), denialObserver);
                AssertSameDenialObservations(absent, observed);
                timingSamples.Add(new { Round = round, Kind = kind, observed.Duration, Trace = observed.Trace.ToString() });
            }
        }
        var foreignAfter = await ForeignStateAsync();
        foreignAfter.Length.ShouldBe(foreignBefore.Length);
        for (int i = 0; i < foreignBefore.Length; i++)
        {
            foreignAfter[i].Bytes.ShouldBe(foreignBefore[i].Bytes);
            foreignAfter[i].ETag.ShouldBe(foreignBefore[i].ETag);
        }
        output.WriteLine("Actual real-owner denial observations: " + JsonSerializer.Serialize(new
        {
            DeniedTrace = denied.Trace.ToString(), AbsentTrace = absent.Trace.ToString(), ForeignOnlyTrace = foreignOnly.Trace.ToString(),
            absent.Headers, absent.Logs, MetricCategory = absent.Metric, ServerActivity = absent.Activity,
            ForeignStateByteAndETagComparisons = foreignBefore.Length,
            BoundedInterleavedTimingSamples = timingSamples,
            TimingScope = "Existing real-owner component fixture; no distributed, load or constant-time acceptance.",
            TimingToleranceAccepted = false,
        }));
        (await ReadAsync<ProjectListPage>(client, List(new()))).Items.Single().ProjectId.ShouldBe("project-b");
        heads.Outage = true;
        ProjectListPage outage = await ReadAsync<ProjectListPage>(client, List(new()));
        outage.Items.ShouldBeEmpty(); outage.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        heads.Outage = false;
        foreach (string malformed in new[] { "{}", "null", "[]", "{\"outcome\":null}", "{\"outcome\":999}", "{\"outcome\":0,\"Outcome\":0}", "{\"outcome\":\"Allowed\"}" })
        {
            transport.OwnerResponseOverride = malformed;
            ProjectListPage unverifiable = await ReadAsync<ProjectListPage>(client, List(new()));
            unverifiable.Items.ShouldBeEmpty(); unverifiable.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
            await StatusAsync(client, Open(new GetProjectQuery(), "project-b"), HttpStatusCode.NotFound);
        }
        transport.OwnerResponseOverride = null;
        foreach (HttpStatusCode status in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound })
        {
            transport.OwnerStatusOverride = status;
            ProjectListPage ownerFailure = await ReadAsync<ProjectListPage>(client, List(new()));
            ownerFailure.Items.ShouldBeEmpty();
            ownerFailure.Snapshot.ResponseState.ShouldBe(status == HttpStatusCode.NotFound ? AdmissionResponseState.Complete : AdmissionResponseState.Unavailable);
        }
        transport.OwnerStatusOverride = null;
        IConfiguration signing = projects.Services.GetRequiredService<IConfiguration>();
        string? originalKey = signing["Authentication:BoundStreamRead:PrivateKey"];
        signing["Authentication:BoundStreamRead:PrivateKey"] = null;
        (await ReadAsync<ProjectListPage>(client, List(new()))).Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        signing["Authentication:BoundStreamRead:PrivateKey"] = originalKey;
        transport.TamperNextProjectQuery = true;
        await StatusAsync(client, Open(new GetProjectQuery(), "project-b"), HttpStatusCode.NotFound);
        transport.HumanBearerForwarded.ShouldBeFalse();
        transport.OwnerQueries.ShouldBeGreaterThan(0);
        gateway.Services.GetRequiredService<GatewayFixtureRouteCatalog>().AcquisitionCount.ShouldBeGreaterThan(0);
        byte[][] beforeMissingSurface = await StoreBytesAsync();
        int ownerQueriesBeforeMissingSurface = transport.OwnerQueries;
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false, includeSurface: false));
        await StatusAsync(client, SuggestAttachments(new ProjectAdvisoryAttachment("file", "unsupported")), HttpStatusCode.Forbidden);
        byte[][] afterMissingSurface = await StoreBytesAsync();
        for (int i = 0; i < observedKeys.Length; i++) afterMissingSurface[i].ShouldBe(beforeMissingSurface[i]);
        transport.OwnerQueries.ShouldBe(ownerQueriesBeforeMissingSurface);
        await conversations.StopAsync(TestContext.Current.CancellationToken); await gateway.StopAsync(TestContext.Current.CancellationToken); await projects.StopAsync(TestContext.Current.CancellationToken); await folders.StopAsync(TestContext.Current.CancellationToken);
    }

    private static WebApplicationBuilder Builder(string workload, string http, string grpc, GatewayFixtureLocalRoutes routes,
        ProjectReadDenialObserver? observer = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["APP_API_TOKEN"] = Channel, ["Authentication:JwtBearer:Issuer"] = Issuer,
            ["Authentication:JwtBearer:Audience"] = Audience, ["Authentication:JwtBearer:SigningKey"] = SigningKey,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "HS256", ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            ["Authentication:WorkloadIssuer:Workload"] = workload,
        });
        builder.Logging.ClearProviders().AddConsole().SetMinimumLevel(LogLevel.Error);
        builder.ConfigureOpenTelemetry();
        if (observer is not null)
        {
            builder.Logging.AddProvider(new ProjectReadDenialLogProvider(observer));
            builder.Logging.AddFilter<ProjectReadDenialLogProvider>(static (_, _) => true);
        }
        builder.Services.AddSingleton(_ => new DaprClientBuilder().UseHttpEndpoint(http).UseGrpcEndpoint(grpc).Build());
        builder.Services.AddHttpContextAccessor(); builder.Services.AddAuthorization(); builder.Services.AddEventStoreWorkloadAssertionIssuer();
        builder.Services.AddHttpClient(Options.DefaultName).ConfigurePrimaryHttpMessageHandler(() => new GatewayFixtureLoopbackHandler(routes));
        return builder;
    }

    private static async Task<WebApplication> StartProjectsAsync(string http, string grpc, GatewayFixtureLocalRoutes routes, RSA key, DateTimeOffset now,
        ProjectReadDenialObserver? observer = null)
    {
        var builder = Builder("projects", http, grpc, routes, observer);
        builder.Configuration["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(key.ExportPkcs8PrivateKey());
        builder.Services.AddProjectsServer(); builder.Services.AddProjectsServerRuntimeInfrastructure();
        builder.Services.AddSingleton<Hexalith.Projects.Projections.TenantAccess.IUtcClock>(new ProjectReadTestClock(now.AddSeconds(1)));
        builder.Services.AddSingleton<TimeProvider>(new ProjectReadTestClock(now.AddSeconds(1)));
        WebApplication app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); return app;
    }

    private static async Task<WebApplication> StartFoldersAsync(string http, string grpc, GatewayFixtureLocalRoutes routes, GatewayFixtureOwnerHeads heads, DateTimeOffset now, string tenant,
        ProjectReadDenialObserver? observer = null)
    {
        var builder = Builder("folders", http, grpc, routes, observer);
        builder.Services.AddFoldersProductionAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddFoldersServer(); builder.Services.AddEventStoreReadModelStore();
        GatewayFixtureCatalogComposition.AddFolderReceiver(builder, tenant, includeAdvisoryQueries: true);
        // Unused command services require a repository to compose; owner reads below use only persisted ACL projections.
        builder.Services.AddSingleton<IFolderRepository, InMemoryFolderRepository>();
        builder.Services.AddSingleton<Hexalith.Folders.Projections.TenantAccess.IUtcClock>(new Hexalith.Folders.Projections.TenantAccess.FixedUtcClock(now.AddSeconds(1)));
        builder.Services.AddSingleton<IOrganizationStreamSequenceReader>(heads);
        builder.Services.AddSingleton<EventStoreOrganizationEffectivePermissionsReadModel>();
        builder.Services.AddSingleton<EventStoreFolderEffectivePermissionsReadModel>();
        builder.Services.AddSingleton<IEffectivePermissionsReadModel>(sp => sp.GetRequiredService<EventStoreFolderEffectivePermissionsReadModel>());
        builder.Services.AddSingleton<IFolderTenantAccessProjectionStore, EventStoreFolderTenantAccessProjectionStore>();
        builder.Services.AddSingleton<IEventStoreAuthorizationValidator, VerifiedReceiptEventStoreAuthorizationValidator>();
        builder.Services.AddAuthentication().AddEventStoreWorkloadScheme(EventStoreWorkloadAuthenticationDefaults.WorkloadScheme, "Authentication:Workload",
            (options, _) => { options.Audience = "folders"; options.AllowedCallers.Add("eventstore"); });
        builder.Services.AddEventStoreWorkloadPolicies(EventStoreWorkloadAuthenticationDefaults.WorkloadScheme,
            [EventStoreWorkloadOperations.DomainServiceQuery, EventStoreWorkloadOperations.DomainServiceMetadata]);
        WebApplication app = builder.Build(); app.UseAuthentication(); app.UseEventStoreHttpHostSecurity(); app.UseAuthorization(); app.MapFoldersDomainServiceEndpoints();
        app.Services.GetRequiredService<EventStoreDomainServiceEndpointSource>().Capture(app);
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); return app;
    }

    private static async Task<WebApplication> StartConversationsAsync(string http, string grpc, GatewayFixtureLocalRoutes routes,
        ProjectReadDenialObserver? observer = null)
    {
        var builder = Builder("conversations", http, grpc, routes, observer);
        builder.Services.AddEventStoreReadModelStore();
        builder.Services.AddSingleton<GatewayFixtureConversationTenants>();
        builder.Services.AddSingleton<ITenantProjectionStore>(sp => sp.GetRequiredService<GatewayFixtureConversationTenants>());
        builder.Services.AddSingleton<IConversationTenantProjectionSignal>(sp => sp.GetRequiredService<GatewayFixtureConversationTenants>());
        builder.Services.AddSingleton<IConversationTenantAccessService, ConversationTenantAccessService>();
        builder.Services.AddSingleton<IDomainQueryHandler, GetConversationAdvisoryMetadataDomainQueryHandler>();
        builder.Services.AddAuthentication().AddEventStoreWorkloadScheme(EventStoreWorkloadAuthenticationDefaults.WorkloadScheme, "Authentication:Workload",
            (options, _) => { options.Audience = "conversations"; options.AllowedCallers.Add("eventstore"); });
        builder.Services.AddEventStoreWorkloadPolicies(EventStoreWorkloadAuthenticationDefaults.WorkloadScheme,
            [EventStoreWorkloadOperations.DomainServiceQuery]);
        WebApplication app = builder.Build(); app.UseAuthentication(); app.UseAuthorization();
        app.MapPost("/query", async (QueryEnvelope query, IServiceProvider services, CancellationToken token) =>
        {
            QueryResult result = await DomainQueryDispatcher.ExecuteAsync(services, query, token).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.NotFound(result);
        }).RequireAuthorization(EventStoreDomainServicePolicies.Query);
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); return app;
    }

    private static async Task<WebApplication> StartGatewayAsync(string http, string grpc, GatewayFixtureLocalRoutes routes, RSA key, string tenant,
        ProjectReadDenialObserver? denialObserver = null)
    {
        var builder = Builder("eventstore", http, grpc, routes, denialObserver);
        if (denialObserver is not null)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource("*")
                .AddProcessor(new SimpleActivityExportProcessor(denialObserver.TraceExporter)));
        }
        builder.Configuration["Authentication:BoundStreamRead:PublicKeys:projects"] = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
         builder.Configuration["Authentication:BoundOwnerQuery:AllowedQueries:projects"] = FolderOwnerReadQuery.QueryType + "," + GetConversationAdvisoryMetadataQuery.QueryType;
        builder.Configuration["Authentication:QueryAuthority:RequiredQueries:projects:0"] = ListProjectsQuery.QueryType;
        builder.Configuration["Authentication:QueryAuthority:RequiredQueries:projects:1"] = GetProjectQuery.QueryType;
        builder.Configuration["Authentication:QueryAuthority:RequiredQueries:projects:2"] = ResolveProjectFromConversationQuery.QueryType;
        builder.Configuration["Authentication:QueryAuthority:RequiredQueries:projects:3"] = ResolveProjectFromAttachmentsQuery.QueryType;
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = Issuer, ValidateAudience = true, ValidAudience = Audience,
                ValidateLifetime = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)) };
        }).AddEventStoreWorkloadScheme(DaprInternalAuthenticationOptions.SchemeName, "Authentication:DaprInternal",
            (options, _) => { options.Audience = "eventstore"; options.AllowedCallers.Add("projects"); });
        builder.Services.AddEventStoreWorkloadPolicies(DaprInternalAuthenticationOptions.SchemeName, [EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery]);
        builder.Services.Configure<Microsoft.AspNetCore.Authorization.AuthorizationOptions>(options =>
            options.DefaultPolicy = Hexalith.EventStore.Extensions.EventStoreServiceCollectionExtensions.CreateHumanDefaultPolicy());
        builder.Services.AddAuthorizationBuilder().AddPolicy(
            EventStoreHttpHostSecurityExtensions.HumanPolicy, EventStoreHttpHostSecurityExtensions.CreateHumanPolicy());
        builder.Services.AddControllers().AddApplicationPart(typeof(QueriesController).Assembly);
        builder.Services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(QueryEnvelope).Assembly));
        builder.Services.AddTransient<MediatR.IRequestHandler<Hexalith.EventStore.Server.Pipeline.Queries.SubmitQuery, Hexalith.EventStore.Server.Pipeline.Queries.SubmitQueryResult>, SubmitQueryHandler>();
        builder.Services.AddSingleton<ITenantValidator, ClaimsTenantValidator>(); builder.Services.AddSingleton<IRbacValidator, ClaimsRbacValidator>();
        builder.Services.AddSingleton<IETagService, GatewayFixtureNoProjectionETag>();
        builder.Services.AddSingleton<IDomainServiceResolver, GatewayFixtureRoutesResolver>();
        builder.Services.AddSingleton<IDomainQueryHandlerRegistry, DaprDomainQueryHandlerRegistry>();
        builder.Services.AddSingleton<IDomainQueryInvoker, DaprDomainQueryInvoker>();
        builder.Services.AddSingleton(new GatewayFixtureRouteCatalog(tenant, includeAdvisoryQueries: true));
        builder.Services.AddSingleton<IRouteCatalogAuthority>(sp => sp.GetRequiredService<GatewayFixtureRouteCatalog>());
        builder.Services.AddSingleton<IQueryRouter>(sp => new HandlerAwareQueryRouter(sp.GetRequiredService<IRouteCatalogAuthority>(), new GatewayFixtureUnsupportedActorRouter(), sp.GetRequiredService<IDomainQueryHandlerRegistry>(),
            sp.GetRequiredService<IDomainQueryInvoker>(), sp.GetRequiredService<ILogger<HandlerAwareQueryRouter>>()));
        builder.Services.AddTransient<DomainServiceWorkloadAssertionHandler>();
        builder.Services.AddHttpClient(Options.DefaultName).AddHttpMessageHandler<DomainServiceWorkloadAssertionHandler>();
        builder.Services.AddHttpClient(DaprDomainQueryInvoker.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(services => GatewayFixtureCatalogComposition.QueryTransport(services, () => new GatewayFixtureLoopbackHandler(routes)))
            .AddHttpMessageHandler<DomainServiceWorkloadAssertionHandler>();
        builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<QueryNotFoundExceptionHandler>(); builder.Services.AddExceptionHandler<QueryExecutionFailedExceptionHandler>();
        WebApplication app = builder.Build();
        if (denialObserver is not null)
        {
            app.Use(async (context, next) => { denialObserver.BindGatewayRequest(context); await next(context).ConfigureAwait(false); });
            app.UseMiddleware<Hexalith.EventStore.Middleware.CorrelationIdMiddleware>();
        }
        app.UseExceptionHandler(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); return app;
    }

    private static string HumanToken(string tenant, bool inspection, bool projectUser = false, bool includeSurface = true)
    {
        List<Claim> claims = [new("sub", "actor-1"), new("tenant_id", tenant), new("eventstore:tenant", tenant), new("azp", projectUser ? "chatbot" : "operator-web"),
            new("client_id", "issuing-client"), new("act", "{\"sub\":\"delegation-1\"}"), new("scope", "projects.read projects.list"),
            new("projects:role", projectUser ? "ProjectUser" : "TenantOperator"), new("eventstore:permission", "query:read"),
            new("eventstore:permission", "read_metadata"), new("eventstore:permission", Hexalith.Projects.Server.ProjectAuthorizationGate.ReadProjectAction),
            new("eventstore:permission", Hexalith.Projects.Server.ProjectAuthorizationGate.ListProjectsAction)];
        if (includeSurface) claims.Add(new(RouteCatalogSurfacePolicy.ClaimType, projectUser ? "agent" : "ui"));
        if (inspection) claims.Add(new("eventstore:permission", ProjectReadQueryExecutor.InspectionPermission));
        var handler = new JwtSecurityTokenHandler(); handler.OutboundClaimTypeMap.Clear();
        return handler.CreateEncodedJwt(new SecurityTokenDescriptor { Issuer = Issuer, Audience = Audience,
            NotBefore = DateTime.UtcNow.AddSeconds(-1), Expires = DateTime.UtcNow.AddMinutes(5), Subject = new ClaimsIdentity(claims),
            SigningCredentials = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256) });
    }

    private static async Task<T> ReadAsync<T>(HttpClient client, SubmitQueryRequest query)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/queries", query, TestContext.Current.CancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<SubmitQueryResponse>(body, JsonSerializerOptions.Web)!.Payload.Deserialize<T>(JsonSerializerOptions.Web)!;
    }
    private static async Task<string> StatusAsync(HttpClient client, SubmitQueryRequest query, HttpStatusCode expected)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/queries", query, TestContext.Current.CancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); response.StatusCode.ShouldBe(expected, body); return body;
    }
}
#endif
