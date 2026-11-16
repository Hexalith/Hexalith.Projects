namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Testing.Reads;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadHttpTests
{
    [Fact]
    public async Task AuthenticatedRuntimeHistoryRetainsIndependentKeysExactPositionsAndScopedCursorBehavior()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "cutover-runtime-history" };
        var fixture = await CutoverRuntimeAsync(key);
        WebApplication app = fixture.App;
        await using var appLifetime = app.ConfigureAwait(true);
        using HttpClient client = CutoverClient(app, key);
        InMemoryReadModelStore store = app.Services.GetRequiredService<InMemoryReadModelStore>();
        var before = Snapshot(store);
        string journalBefore = await JournalBytesAsync(fixture.State);
        QueryEnvelope list = CutoverQuery("projects", ListProjectsQuery.QueryType, new ListProjectsQuery(PageSize: 1));
        ProjectListPage first = await ReadShadowQueryAsync<ProjectListPage>(client, list);
        first.Items.Single().ProjectId.ShouldBe("project-a");
        string decoded = app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Hexalith.Projects.ReadCursor.v1").Unprotect(first.Cursor!);
        using JsonDocument cursor = JsonDocument.Parse(decoded);
        using JsonDocument position = JsonDocument.Parse(cursor.RootElement.GetProperty("position").GetString()!);
        position.RootElement.GetProperty("projectId").GetString().ShouldBe("project-a");
        var clock = (ProjectReadTestClock)app.Services.GetRequiredService<TimeProvider>();
        position.RootElement.GetProperty("expiresAt").GetDateTimeOffset().ShouldBe(clock.Now.AddMinutes(15));
        cursor.RootElement.GetProperty("queryType").GetString().ShouldBe(ListProjectsQuery.QueryType);
        string expectedScope = await ExpectedCutoverScopeAsync(app, list);
        cursor.RootElement.GetProperty("scope").GetString().ShouldBe(expectedScope);
        foreach (ProjectionRequest history in fixture.History)
        {
            var legacy = (await fixture.Journal.GetDetailAsync("tenant-a", history.AggregateId, TestContext.Current.CancellationToken))!;
            var supported = (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
                ProjectReadModelKeys.Detail("tenant-a", history.AggregateId), TestContext.Current.CancellationToken)).Value!;
            string legacyIdentity = new AggregateIdentity(legacy.TenantId, "projects", legacy.ProjectId).ActorId;
            string supportedIdentity = new AggregateIdentity(supported.TenantId, "projects", supported.ProjectId).ActorId;
            legacyIdentity.ShouldBe(supportedIdentity);
            supported.GlobalPosition.ShouldBe(history.Events.Max(item => item.GlobalPosition));
            legacy.Sequence.ShouldBe(history.Events.Max(item => item.GlobalPosition));
            // Runtime legacy Sequence is a global position; it cannot be renamed to source aggregate version.
            supported.Sequence.ShouldBe(history.Events.Max(item => item.SequenceNumber));
            legacy.Sequence.ShouldNotBe(supported.Sequence);
            GetProjectResponse opened = await ReadShadowQueryAsync<GetProjectResponse>(client,
                CutoverQuery(history.AggregateId, GetProjectQuery.QueryType, new GetProjectQuery()));
            opened.Snapshot.ProjectVersion.ShouldBe(supported.Sequence);
            opened.Name.ShouldBeNull();
            using HttpResponseMessage legacyOpen = await client.GetAsync("/api/v1/projects/" + history.AggregateId + "?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
            JsonElement legacyBody = await legacyOpen.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            legacyBody.GetProperty("name").GetString().ShouldNotBeNullOrWhiteSpace();
        }
        ProjectListPage next = await ReadShadowQueryAsync<ProjectListPage>(client, list with
        { Payload = JsonSerializer.SerializeToUtf8Bytes(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1), JsonSerializerOptions.Web) });
        next.Items.Single().ProjectId.ShouldBe("project-b");
        using JsonDocument nextCursor = JsonDocument.Parse(app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Hexalith.Projects.ReadCursor.v1").Unprotect(next.Cursor!));
        using JsonDocument nextPosition = JsonDocument.Parse(nextCursor.RootElement.GetProperty("position").GetString()!);
        nextPosition.RootElement.GetProperty("expiresAt").GetDateTimeOffset().ShouldBe(position.RootElement.GetProperty("expiresAt").GetDateTimeOffset());
        foreach (ListProjectsQuery altered in new[] { new ListProjectsQuery(Cursor: first.Cursor, PageSize: 2), new ListProjectsQuery("Archived", first.Cursor, 1), new ListProjectsQuery(Cursor: first.Cursor + "tampered", PageSize: 1) })
        {
            using HttpResponseMessage rejected = await client.PostAsJsonAsync("/query", list with { Payload = JsonSerializer.SerializeToUtf8Bytes(altered, JsonSerializerOptions.Web) }, TestContext.Current.CancellationToken);
            rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        clock.Now = clock.Now.AddMinutes(16);
        IProjectTenantAccessProjectionStore tenants = app.Services.GetRequiredService<IProjectTenantAccessProjectionStore>();
        var authority = (await tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken))!;
        authority.LastEventTimestamp = clock.Now;
        await tenants.SaveAsync(authority, TestContext.Current.CancellationToken);
        using HttpResponseMessage expired = await client.PostAsJsonAsync("/query", list with { Payload = JsonSerializer.SerializeToUtf8Bytes(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1), JsonSerializerOptions.Web) }, TestContext.Current.CancellationToken);
        expired.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        Snapshot(store).ShouldBe(before);
        (await JournalBytesAsync(fixture.State)).ShouldBe(journalBefore);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AuthenticatedAdvisoryComparisonsPreserveSafeMetadataAndHonestUnavailableCoverage()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "cutover-advisory" };
        var fixture = await CutoverRuntimeAsync(key);
        WebApplication app = fixture.App;
        await using var appLifetime = app.ConfigureAwait(true);
        using HttpClient client = CutoverClient(app, key);
        string before = await JournalBytesAsync(fixture.State);
        ProjectAdvisoryResolutionResponse supported = await ReadShadowQueryAsync<ProjectAdvisoryResolutionResponse>(client,
            CutoverQuery("projects", ResolveProjectFromConversationQuery.QueryType, new ResolveProjectFromConversationQuery("conversation")));
        using HttpResponseMessage legacy = await client.GetAsync("/api/v1/projects/resolution/from-conversation?conversationId=conversation", TestContext.Current.CancellationToken);
        JsonElement legacyBody = await legacy.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        supported.Candidates.ShouldNotBeEmpty();
        legacyBody.GetProperty("candidates").EnumerateArray().All(candidate => !candidate.TryGetProperty("displayName", out JsonElement name) || name.ValueKind == JsonValueKind.Null).ShouldBeTrue();
        legacyBody.GetProperty("excluded").GetArrayLength().ShouldBe(0);
        JsonSerializer.SerializeToElement(supported, JsonSerializerOptions.Web).GetProperty("candidates").EnumerateArray()
            .All(candidate => !candidate.TryGetProperty("displayName", out _) && !candidate.TryGetProperty("name", out _)).ShouldBeTrue();
        foreach (string kind in new[] { "folder", "file" })
        {
            string id = kind == "folder" ? "folder-project-a" : "opaque-file";
            ProjectAdvisoryResolutionResponse attachment = await ReadShadowQueryAsync<ProjectAdvisoryResolutionResponse>(client,
                CutoverQuery("projects", ResolveProjectFromAttachmentsQuery.QueryType, new ResolveProjectFromAttachmentsQuery([new(kind, id)])));
            using HttpResponseMessage retained = await client.GetAsync("/api/v1/projects/resolution/from-attachments?" + kind + "Id=" + id, TestContext.Current.CancellationToken);
            JsonElement retainedBody = await retained.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            if (kind == "file")
            {
                attachment.Incomplete.ShouldBeTrue(); attachment.Result.ShouldBeNull();
                attachment.UnsupportedInputKinds.ShouldBe(["file"]);
                retained.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
                retainedBody.TryGetProperty("result", out _).ShouldBeFalse();
            }
            else
            {
                attachment.Candidates.ShouldNotBeEmpty();
                retainedBody.GetProperty("candidates").EnumerateArray().All(candidate => !candidate.TryGetProperty("displayName", out JsonElement name) || name.ValueKind == JsonValueKind.Null).ShouldBeTrue();
            }
        }
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("open")]
    [InlineData("conversation-start")]
    [InlineData("context")]
    [InlineData("explain")]
    [InlineData("resolve-conversation")]
    [InlineData("resolve-attachments")]
    public async Task AuthenticatedForeignTenantAndStaleAuthorityNeverPassCutoverComparison(string operation)
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "cutover-" + operation };
        var fixture = await CutoverRuntimeAsync(key);
        WebApplication app = fixture.App;
        await using var appLifetime = app.ConfigureAwait(true);
        using HttpClient client = CutoverClient(app, key);
        (string Target, string Query, object Payload, string Legacy) path = operation switch
        {
            "list" => ("projects", ListProjectsQuery.QueryType, new ListProjectsQuery(), "/api/v1/projects"),
            "open" => ("project-a", GetProjectQuery.QueryType, new GetProjectQuery(), "/api/v1/projects/project-a"),
            "conversation-start" => ("project-a", ProjectsServerModule.GetConversationStartSetupQueryType, new GetConversationStartSetupQuery("project-a"), "/api/v1/projects/project-a/setup/conversation-start"),
            "context" => ("project-a", ProjectsServerModule.GetProjectContextQueryType, new GetProjectContextQuery("project-a"), "/api/v1/projects/project-a/context"),
            "explain" => ("project-a", ProjectsServerModule.ExplainContextSelectionQueryType, new ExplainContextSelectionQuery("project-a"), "/api/v1/projects/project-a/context/explain"),
            "resolve-conversation" => ("projects", ResolveProjectFromConversationQuery.QueryType, new ResolveProjectFromConversationQuery("conversation"), "/api/v1/projects/resolution/from-conversation?conversationId=conversation"),
            _ => ("projects", ResolveProjectFromAttachmentsQuery.QueryType, new ResolveProjectFromAttachmentsQuery([new("file", "opaque-file")]), "/api/v1/projects/resolution/from-attachments?fileId=opaque-file"),
        };
        QueryEnvelope query = CutoverQuery(path.Target, path.Query, path.Payload);
        string before = await JournalBytesAsync(fixture.State);
        using HttpResponseMessage foreign = await client.PostAsJsonAsync("/query", query with { TenantId = "foreign" }, TestContext.Current.CancellationToken);
        using HttpResponseMessage denied = await client.PostAsJsonAsync("/query", query with { OriginalActorId = "denied" }, TestContext.Current.CancellationToken);
        foreign.StatusCode.ShouldBe(HttpStatusCode.NotFound); denied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await foreign.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe(await denied.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var clock = (ProjectReadTestClock)app.Services.GetRequiredService<TimeProvider>();
        clock.Now = clock.Now.AddMinutes(10);
        using HttpResponseMessage stale = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
        string staleBody = await stale.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (operation is "context" or "explain" or "conversation-start")
        {
            stale.StatusCode.ShouldBe(HttpStatusCode.OK);
            QueryResult result = (JsonSerializer.Deserialize<QueryResult>(staleBody, JsonSerializerOptions.Web))!;
            JsonElement payload = result.GetPayload();
            JsonElement response = operation == "explain" ? payload.GetProperty("context") : payload;
            response.GetProperty("snapshot").GetProperty("responseState").GetString().ShouldBe("Unavailable");
            response.GetProperty("setup").ValueKind.ShouldBe(JsonValueKind.Null);
        }
        else if (operation.StartsWith("resolve-", StringComparison.Ordinal))
        {
            stale.StatusCode.ShouldBe(HttpStatusCode.OK);
            var result = JsonSerializer.Deserialize<QueryResult>(staleBody, JsonSerializerOptions.Web)!;
            ProjectAdvisoryResolutionResponse advisory = SupportedProjectReadFixture.Payload<ProjectAdvisoryResolutionResponse>(result);
            advisory.Incomplete.ShouldBeTrue(); advisory.Candidates.ShouldBeEmpty(); advisory.Result.ShouldBeNull();
        }
        else stale.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        using HttpResponseMessage retained = await client.GetAsync(path.Legacy, TestContext.Current.CancellationToken);
        retained.StatusCode.ShouldBe(operation is "list" or "open" ? HttpStatusCode.NotFound : HttpStatusCode.ServiceUnavailable);
        JsonElement retainedBody = await retained.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        retainedBody.TryGetProperty("snapshot", out _).ShouldBeFalse();
        // Retained reads refuse stale authority; the supported result envelope remains a separate comparison obligation.
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AuthenticatedHistoricalPendingRecordsStayQuarantinedWithoutHistoryRepair()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "cutover-pending" };
        var fixture = await CutoverRuntimeAsync(key, pending: true);
        WebApplication app = fixture.App;
        await using var appLifetime = app.ConfigureAwait(true);
        using HttpClient client = CutoverClient(app, key);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key, role: "TenantProjectAdministrator"));
        InMemoryReadModelStore store = app.Services.GetRequiredService<InMemoryReadModelStore>();
        var before = Snapshot(store);
        string journalBefore = await JournalBytesAsync(fixture.State);
        ProjectListPage listed = await ReadShadowQueryAsync<ProjectListPage>(client,
            CutoverQuery("projects", ListProjectsQuery.QueryType, new ListProjectsQuery()));
        listed.Items.ShouldNotContain(row => row.ProjectId == "project-c");
        using HttpResponseMessage legacy = await client.GetAsync("/api/v1/projects/project-c?inspectDescriptiveMetadata=true&inspectQuarantined=true", TestContext.Current.CancellationToken);
        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProjectContextReadResponse context = await ReadShadowQueryAsync<ProjectContextReadResponse>(client,
            CutoverQuery("project-c", ProjectsServerModule.GetProjectContextQueryType, new GetProjectContextQuery("project-c") { ConversationId = "bound-conversation" }));
        context.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        context.Setup.ShouldBeNull(); context.ProjectFolder.ShouldBeNull();
        ProjectReadModel persisted = (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Detail("tenant-a", "project-c"), TestContext.Current.CancellationToken)).Value!;
        persisted.Detail!.ProjectFolder!.ReferenceState.ShouldBe(ReferenceState.Pending);
        persisted.Detail!.ProjectFolder!.FolderId.ShouldBeNull();
        Snapshot(store).ShouldBe(before);
        (await JournalBytesAsync(fixture.State)).ShouldBe(journalBefore);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<(WebApplication App, InMemoryProjectsStateStore State, DaprProjectProjectionStore Journal, ProjectionRequest[] History)> CutoverRuntimeAsync(SecurityKey key, bool pending = false, Action<IServiceCollection>? configure = null)
    {
        InMemoryProjectsStateStore state = new();
        DaprProjectProjectionStore journal = new(state);
        WebApplication app = await StartAsync(null, null, key, conversations: new AdvisoryTestConversationDirectory(), configure: services =>
        {
            services.RemoveAll<IProjectDetailReadModel>(); services.AddSingleton<IProjectDetailReadModel>(new DaprProjectDetailReadModel(journal));
            services.RemoveAll<IProjectListReadModel>(); services.AddSingleton<IProjectListReadModel>(new DaprProjectListReadModel(journal));
            services.RemoveAll<IProjectReferenceIndexReadModel>(); services.AddSingleton<IProjectReferenceIndexReadModel>(new DaprProjectReferenceIndexReadModel(journal));
            services.RemoveAll<IProjectConversationResolutionDirectory>(); services.AddSingleton<IProjectConversationResolutionDirectory, CutoverConversationResolutionDirectory>();
            configure?.Invoke(services);
        }).ConfigureAwait(false);
        InMemoryReadModelStore store = app.Services.GetRequiredService<InMemoryReadModelStore>();
        ProjectionRequest[] history = [SupportedProjectReadFixture.Request("project-a", 100), SupportedProjectReadFixture.Request("project-b", 200), SupportedProjectReadFixture.Request("project-c", 300)];
        if (pending)
        {
            ProjectFolderCreationPending historical = new("tenant-a", "project-c", "Historical", "pending", true,
                "actor-1", "historical-correlation", "historical-task", "pending", "pending", SupportedProjectReadFixture.EventTime);
            history[2] = history[2] with { Events = [history[2].Events[0], SupportedProjectReadFixture.Event(historical, 2, 301)] };
        }
        foreach (ProjectionRequest request in history)
        {
            foreach (ProjectionEventDto item in request.Events)
            {
                var metadata = new EventMetadata(request.AggregateId + "-" + item.SequenceNumber, request.AggregateId, "projects", request.TenantId,
                    "projects", item.SequenceNumber, item.GlobalPosition, item.Timestamp, "source-correlation", "source-command", "actor-1", "v1", item.EventTypeName, 1, "json");
                (await journal.AppendAsync(new EventEnvelope(metadata, item.Payload, null), TestContext.Current.CancellationToken).ConfigureAwait(false)).Status.ShouldBe(ProjectProjectionAppendStatus.Applied);
            }
            await new ProjectReadProjectionHandler(store, store).ProjectAsync(request, "cutover-" + request.AggregateId, TestContext.Current.CancellationToken).ConfigureAwait(false);
            await new ConversationStartSetupProjectionHandler(store).ProjectAsync(request, "cutover-setup-" + request.AggregateId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        return (app, state, journal, history);
    }

    private static HttpClient CutoverClient(WebApplication app, SecurityKey key)
    {
        HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
        return client;
    }

    private static QueryEnvelope CutoverQuery(string target, string type, object payload) => new("tenant-a", "projects", target, type,
        JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), JsonSerializerOptions.Web), "cutover-compare", "actor-1")
    { OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true, DelegationId = "delegation-1", Scopes = ["projects.read", "projects.list"], Audience = [Audience] };

    private static async Task<string> JournalBytesAsync(InMemoryProjectsStateStore store)
        => JsonSerializer.Serialize(await store.GetAsync<JsonElement>("statestore", "projects:projection-journal:tenant-a", TestContext.Current.CancellationToken).ConfigureAwait(false));

    private static async Task<string> ExpectedCutoverScopeAsync(WebApplication app, QueryEnvelope query)
    {
        InMemoryReadModelStore store = app.Services.GetRequiredService<InMemoryReadModelStore>();
        var index = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"), TestContext.Current.CancellationToken).ConfigureAwait(false);
        QueryCursorScope visibility = QueryCursorScope.Create();
        foreach (string id in new[] { "project-a", "project-b", "project-c" }) visibility.Add("project", id).Add("allowed", "True")
            .Add("authorization", "eventstore_validator_test_allow").Add("folder", "Included").Add("quarantined", "False");
        string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        string scope = QueryCursorScope.Create().Add("tenant", "tenant-a").Add("actor", "actor-1").Add("workload", "operator-web")
            .Add("delegation", "delegation-1").Add("delegated", "True").Add("scopes", JsonSerializer.Serialize(query.Scopes)).Add("audience", JsonSerializer.Serialize(query.Audience))
            .Add("action", ProjectAuthorizationGate.ListProjectsAction).Add("lifecycle", (string?)null).Add("pageSize", "1").Add("inspect", "False").Add("quarantine", "False")
            .Add("policy", "project-list-v1-50-200-15m").Add("authority", "tenant-a:1").Add("eventStoreAuthority", "eventstore_validator_test_allow")
            .Add("eventStoreFreshness", "fresh").Add("indexETag", index.ETag).Add("visibility", Hash(visibility.Build())).AddProjectionWatermark(301).Build();
        return Hash(scope);
    }
}
