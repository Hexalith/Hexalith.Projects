namespace Hexalith.Projects.Server.Tests.Queries;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Dapr.Client;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Authentication;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Always-run authenticated routing plus opt-in isolated Dapr persistence; not production acceptance.</summary>
public sealed class SupportedProjectReadHttpTests
{
    private const string Issuer = "https://story61.example/identity";
    private const string Audience = "configured-read-audience";

    [Fact]
    public async Task AuthenticatedRegisteredReadsAlwaysExerciseHttpStatusAndSafeDenialContracts()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story61-always-http" };
        WebApplication app = await StartAsync(null, null, key, conversations: new AdvisoryTestConversationDirectory());
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            QueryEnvelope open = new("tenant-a", "projects", "project-a", GetProjectQuery.QueryType, "{}"u8.ToArray(), "ordinary-http", "actor-1")
            { OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true, DelegationId = "delegation-1", Scopes = ["projects.read", "projects.list"], Audience = [Audience] };
            using HttpResponseMessage anonymous = await client.PostAsJsonAsync("/query", open, TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            using HttpResponseMessage metadata = await client.PostAsJsonAsync("/admin/operational-index-metadata", new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" }, TestContext.Current.CancellationToken);
            metadata.EnsureSuccessStatusCode();
            string fingerprint = (await metadata.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken))!.CatalogFingerprint!;
            foreach (string route in new[] { "/project/v2", "/project/rebuild/v1" })
            {
                using HttpResponseMessage mutation = await client.PostAsJsonAsync(route, new ProjectionDispatchRequest(SupportedProjectReadFixture.Request("project-a"), ["project-reads"], "ordinary-projection", fingerprint), TestContext.Current.CancellationToken);
                mutation.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            }
            (await DispatchAsync(app, new(SupportedProjectReadFixture.Request("project-a"), ["project-reads"], "ordinary-projection", fingerprint))).Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
            QueryEnvelope list = open with { AggregateId = "projects", QueryType = ListProjectsQuery.QueryType };
            using HttpResponseMessage opened = await client.PostAsJsonAsync("/query", open, TestContext.Current.CancellationToken);
            using HttpResponseMessage listed = await client.PostAsJsonAsync("/query", list, TestContext.Current.CancellationToken);
            opened.StatusCode.ShouldBe(HttpStatusCode.OK); listed.StatusCode.ShouldBe(HttpStatusCode.OK);
            SupportedProjectReadFixture.Payload<GetProjectResponse>((await opened.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!).ProjectId.ShouldBe("project-a");
            SupportedProjectReadFixture.Payload<ProjectListPage>((await listed.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!).Items.Single().ProjectId.ShouldBe("project-a");
            foreach (QueryEnvelope query in new[] { open, list })
            {
                using HttpResponseMessage malformed = await client.PostAsJsonAsync("/query", query with { Payload = "[]"u8.ToArray() }, TestContext.Current.CancellationToken);
                malformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
            using HttpResponseMessage absent = await client.PostAsJsonAsync("/query", open with { AggregateId = "absent" }, TestContext.Current.CancellationToken);
            using HttpResponseMessage denied = await client.PostAsJsonAsync("/query", open with { OriginalActorId = "other" }, TestContext.Current.CancellationToken);
            absent.StatusCode.ShouldBe(HttpStatusCode.NotFound); denied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await absent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe(await denied.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            QueryEnvelope advisory = list with { QueryType = ResolveProjectFromAttachmentsQuery.QueryType,
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ResolveProjectFromAttachmentsQuery([new("file", "file-id")]), JsonSerializerOptions.Web) };
            using HttpResponseMessage suggestions = await client.PostAsJsonAsync("/query", advisory, TestContext.Current.CancellationToken);
            suggestions.StatusCode.ShouldBe(HttpStatusCode.OK);
            var advisoryResult = SupportedProjectReadFixture.Payload<ProjectAdvisoryResolutionResponse>((await suggestions.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!);
            advisoryResult.Incomplete.ShouldBeTrue(); advisoryResult.Result.ShouldBeNull(); advisoryResult.UnsupportedInputKinds.ShouldBe(["file"]);
            using HttpResponseMessage oversize = await client.PostAsJsonAsync("/query", advisory with
            { Payload = JsonSerializer.SerializeToUtf8Bytes(new ResolveProjectFromAttachmentsQuery(Enumerable.Repeat(new ProjectAdvisoryAttachment("folder", "same"), 33).ToArray()), JsonSerializerOptions.Web) }, TestContext.Current.CancellationToken);
            oversize.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using HttpResponseMessage advisoryDenied = await client.PostAsJsonAsync("/query", advisory with { OriginalActorId = "other" }, TestContext.Current.CancellationToken);
            advisoryDenied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            QueryEnvelope conversation = advisory with { QueryType = ResolveProjectFromConversationQuery.QueryType,
                Payload = "{\"conversationId\":\"conversation\"}"u8.ToArray() };
            using HttpResponseMessage conversationAllowed = await client.PostAsJsonAsync("/query", conversation, TestContext.Current.CancellationToken);
            conversationAllowed.StatusCode.ShouldBe(HttpStatusCode.OK);
            var conversationResult = SupportedProjectReadFixture.Payload<ProjectAdvisoryResolutionResponse>(
                (await conversationAllowed.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!);
            conversationResult.Incomplete.ShouldBeFalse(); conversationResult.Candidates.Single().ProjectId.ShouldBe("project-a");
            conversationResult.Candidates.Single().Score.ShouldBe(50);
            conversationResult.Candidates.Single().ReasonCodes.ShouldBe([Hexalith.Projects.Contracts.Ui.ProjectReasonCode.ConversationLinked]);
            using HttpResponseMessage conversationMalformed = await client.PostAsJsonAsync("/query", conversation with { Payload = "{}"u8.ToArray() }, TestContext.Current.CancellationToken);
            conversationMalformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using HttpResponseMessage conversationDenied = await client.PostAsJsonAsync("/query", advisory with
            { QueryType = ResolveProjectFromConversationQuery.QueryType, OriginalActorId = "other", Payload = "{\"conversationId\":\"unknown\"}"u8.ToArray() }, TestContext.Current.CancellationToken);
            conversationDenied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart()
    {
        string? httpEndpoint = Environment.GetEnvironmentVariable("STORY61_DAPR_HTTP_ENDPOINT");
        string? grpcEndpoint = Environment.GetEnvironmentVariable("STORY61_DAPR_GRPC_ENDPOINT");
        Assert.SkipUnless(httpEndpoint is not null && grpcEndpoint is not null, "Requires the isolated Story 6.1 Dapr persistence lane.");
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story61-http-fixture" };
        string project = "http-" + Guid.NewGuid().ToString("N");
        QueryEnvelope query = new("tenant-a", "projects", project, GetProjectQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new GetProjectQuery(true), SupportedProjectReadFixture.Json), "http-inspection-" + project, "actor-1")
        {
            OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true, DelegationId = "delegation-1",
            Scopes = ["projects.read", "projects.list"], Audience = [Audience],
        };
        ProjectionRequest projection = SupportedProjectReadFixture.Request(project, 20000);
        string? fingerprint;
        string? before;
        WebApplication app = await StartAsync(httpEndpoint!, grpcEndpoint!, key);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            using HttpResponseMessage anonymous = await client.PostAsJsonAsync("/project/v2", new ProjectionDispatchRequest(projection, ["project-reads"], "dispatch-" + project, "invalid"), TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            using HttpResponseMessage anonymousRead = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            anonymousRead.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            using HttpResponseMessage metadata = await client.PostAsJsonAsync("/admin/operational-index-metadata", new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" }, TestContext.Current.CancellationToken);
            metadata.EnsureSuccessStatusCode();
            var catalog = await metadata.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken);
            fingerprint = catalog!.CatalogFingerprint;
            fingerprint.ShouldNotBeNullOrWhiteSpace();
            var outcome = await DispatchAsync(app, new(projection, ["project-reads"], "dispatch-" + project, fingerprint!));
            outcome!.Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed, JsonSerializer.Serialize(outcome));
            IReadModelStore store = app.Services.GetRequiredService<IReadModelStore>();
            before = JsonSerializer.Serialize((await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", project), TestContext.Current.CancellationToken)).Value);
            using HttpResponseMessage opened = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            opened.EnsureSuccessStatusCode();
            QueryResult result = (await opened.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!;
            SupportedProjectReadFixture.Payload<GetProjectResponse>(result).Name.ShouldBe("Project " + project);
            QueryEnvelope list = query with { AggregateId = "projects", QueryType = ListProjectsQuery.QueryType,
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ListProjectsQuery(PageSize: 200), SupportedProjectReadFixture.Json) };
            using HttpResponseMessage listed = await client.PostAsJsonAsync("/query", list, TestContext.Current.CancellationToken);
            listed.EnsureSuccessStatusCode();
            ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>((await listed.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!);
            page.Items.ShouldContain(item => item.ProjectId == project && item.Name == null);
            using HttpResponseMessage malformed = await client.PostAsJsonAsync("/query", query with { Payload = "{\"pageSize\":-1}"u8.ToArray() }, TestContext.Current.CancellationToken);
            malformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using HttpResponseMessage denied = await client.PostAsJsonAsync("/query", query with { OriginalActorId = "other" }, TestContext.Current.CancellationToken);
            using HttpResponseMessage absent = await client.PostAsJsonAsync("/query", query with { AggregateId = "absent" }, TestContext.Current.CancellationToken);
            denied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            absent.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await denied.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe(await absent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            JsonSerializer.Serialize((await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", project), TestContext.Current.CancellationToken)).Value).ShouldBe(before);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        WebApplication restarted = await StartAsync(httpEndpoint!, grpcEndpoint!, key);
        await using (restarted.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(restarted.Urls.Single()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            using HttpResponseMessage opened = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            opened.EnsureSuccessStatusCode();
            SupportedProjectReadFixture.Payload<GetProjectResponse>((await opened.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!).Name.ShouldBe("Project " + project);
            (await DispatchAsync(restarted, new(projection, ["project-reads"], "dispatch-" + project, fingerprint!))).Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
            (await DispatchAsync(restarted, new(projection, ["project-reads"], "rebuild-" + project, fingerprint!), rebuild: true)).Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
            IReadModelStore store = restarted.Services.GetRequiredService<IReadModelStore>();
            JsonSerializer.Serialize((await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", project), TestContext.Current.CancellationToken)).Value).ShouldBe(before);
            await restarted.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ActualWorkloadPolicyAdmitsSdkProjectionButFailsClosedWithoutActorAuthority()
    {

        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story61-workload-fixture" };
        WebApplication app = await StartAsync(null, null, key, workload: true);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            IReadModelStore store = app.Services.GetRequiredService<IReadModelStore>();
            foreach (string route in new[] { "/project/v2", "/project/rebuild/v1" })
            {
                var mutation = new ProjectionDispatchRequest(SupportedProjectReadFixture.Request("blocked-project"), ["project-reads"], "blocked", "invalid");
                using HttpResponseMessage anonymous = await client.PostAsJsonAsync(route, mutation, TestContext.Current.CancellationToken);
                anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
                using HttpResponseMessage bearer = await client.PostAsJsonAsync(route, mutation, TestContext.Current.CancellationToken);
                bearer.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                client.DefaultRequestHeaders.Authorization = null;
                (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "blocked-project"), TestContext.Current.CancellationToken)).Value.ShouldBeNull();
                (await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"), TestContext.Current.CancellationToken)).Value.ShouldBeNull();
            }
            client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, "story61-isolated-channel-fixture");
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceMetadata));
            using HttpResponseMessage metadata = await client.PostAsJsonAsync("/admin/operational-index-metadata", new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" }, TestContext.Current.CancellationToken);
            metadata.EnsureSuccessStatusCode();
            string fingerprint = (await metadata.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken))!.CatalogFingerprint!;
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceProject));
            string project = "workload-" + Guid.NewGuid().ToString("N");
            using HttpResponseMessage dispatch = await client.PostAsJsonAsync("/project/v2", new ProjectionDispatchRequest(SupportedProjectReadFixture.Request(project, 30000), ["project-reads"], "dispatch-" + project, fingerprint), TestContext.Current.CancellationToken);
            dispatch.EnsureSuccessStatusCode();
            (await dispatch.Content.ReadFromJsonAsync<ProjectionDispatchResponse>(TestContext.Current.CancellationToken))!.Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceQuery));
            QueryEnvelope query = new("tenant-a", "projects", project, GetProjectQuery.QueryType, "{}"u8.ToArray(), "runtime-read", "actor-1")
            { OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true, DelegationId = "delegation-1", Scopes = ["projects.read", "projects.list"], Audience = [Audience] };
            using HttpResponseMessage read = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await read.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain(project);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            using HttpResponseMessage ambiguous = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            ambiguous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static Task<ProjectionDispatchResponse> DispatchAsync(WebApplication app, ProjectionDispatchRequest request, bool rebuild = false)
        => rebuild ? DomainProjectionDispatcher.RebuildAsync(app.Services, request,
            app.Services.GetRequiredService<IOptions<ProjectionDispatchOptions>>().Value,
            app.Services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value, TestContext.Current.CancellationToken)
        : DomainProjectionDispatcher.DispatchAsync(app.Services, request,
            app.Services.GetRequiredService<IOptions<ProjectionDispatchOptions>>().Value,
            app.Services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value, TestContext.Current.CancellationToken);

    private static string WorkloadToken(SecurityKey key, string operation)
    {
        JwtSecurityTokenHandler handler = new(); handler.OutboundClaimTypeMap.Clear();
        return handler.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = "projects", IssuedAt = DateTime.UtcNow, NotBefore = DateTime.UtcNow.AddSeconds(-1), Expires = DateTime.UtcNow.AddSeconds(120),
            SigningCredentials = new(key, SecurityAlgorithms.RsaSha256),
            Subject = new ClaimsIdentity([new("azp", "eventstore"), new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, operation)]),
        });
    }

    private static async Task<WebApplication> StartAsync(string? httpEndpoint, string? grpcEndpoint, SecurityKey key, bool workload = false, IProjectAdvisoryConversationDirectory? conversations = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Authority"] = Issuer, ["Authentication:JwtBearer:Issuer"] = Issuer,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "RS256",
            ["APP_API_TOKEN"] = "story61-isolated-channel-fixture",
            ["Authentication:JwtBearer:Audience"] = Audience, ["Authentication:JwtBearer:RequireHttpsMetadata"] = "true",
        });
        builder.Services.AddProjectsServer();
        if (conversations is not null)
        {
            builder.Services.RemoveAll<IProjectAdvisoryConversationDirectory>();
            builder.Services.AddSingleton(conversations);
        }
        if (workload) { builder.Services.AddProjectsServerRuntimeInfrastructure(); }
        builder.Services.RemoveAll<IProjectTenantAccessProjectionStore>();
        builder.Services.AddSingleton<IProjectTenantAccessProjectionStore, InMemoryProjectTenantAccessProjectionStore>();
        builder.Services.RemoveAll<IReadModelStore>();
        if (httpEndpoint is null || grpcEndpoint is null)
        {
            builder.Services.AddSingleton<InMemoryReadModelStore>();
            builder.Services.AddSingleton<IReadModelStore>(provider => provider.GetRequiredService<InMemoryReadModelStore>());
            builder.Services.AddSingleton<IReadModelBatchStore>(provider => provider.GetRequiredService<InMemoryReadModelStore>());
        }
        else
        {
            builder.Services.AddSingleton(_ => new DaprClientBuilder().UseHttpEndpoint(httpEndpoint).UseGrpcEndpoint(grpcEndpoint).Build());
            builder.Services.AddEventStoreReadModelStore();
        }
        if (!workload) { builder.Services.AddSingleton<IAsyncDomainProjectionHandler, ProjectReadProjectionHandler>(); }
        builder.Services.RemoveAll<IProjectFolderDirectory>();
        builder.Services.AddSingleton<IProjectFolderDirectory, ProjectReadTestFolders>();
        builder.Services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        builder.Services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        builder.Services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        builder.Services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        ProjectReadTestClock clock = new(SupportedProjectReadFixture.EventTime.AddSeconds(5));
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddSingleton<IUtcClock>(clock);
        builder.Services.AddProjectsAuthentication(builder.Configuration, builder.Environment);
        builder.Services.PostConfigureAll<JwtBearerOptions>(options =>
        {
            OpenIdConnectConfiguration oidc = new() { Issuer = Issuer }; oidc.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(oidc);
        });
        WebApplication app = builder.Build();
        ProjectTenantAccessProjection tenant = new() { TenantId = "tenant-a", Enabled = true, Watermark = 1, ProjectionWatermark = "tenant-a:1", LastEventTimestamp = clock.Now };
        tenant.Principals["actor-1"] = new("actor-1", "TenantOwner");
        await app.Services.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
        app.UseAuthentication(); app.UseAuthorization(); app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return app;
    }

    private static string Token(SecurityKey key)
    {
        JwtSecurityTokenHandler handler = new(); handler.OutboundClaimTypeMap.Clear();
        return handler.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = Audience, NotBefore = DateTime.UtcNow.AddMinutes(-1), Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new(key, SecurityAlgorithms.RsaSha256),
            Subject = new ClaimsIdentity([
                new("sub", "actor-1"), new("tenant_id", "tenant-a"), new("azp", "operator-web"), new("client_id", "issuing-client"),
                new("act", "{\"sub\":\"delegation-1\"}"), new("scope", "projects.read projects.list"), new("projects:role", "TenantOperator"),
                new("eventstore:permission", ProjectAuthorizationGate.ReadProjectAction), new("eventstore:permission", ProjectAuthorizationGate.ListProjectsAction),
                new("eventstore:permission", ProjectReadQueryExecutor.InspectionPermission),
            ]),
        });
    }
}
