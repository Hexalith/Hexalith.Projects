namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Disclosure checks after actual blocked dependencies, through authenticated HTTP.</summary>
public sealed partial class SupportedProjectReadHttpTests
{
    [Theory]
    [InlineData("audit", "tenant-revoked")]
    [InlineData("audit", "tenant-stale")]
    [InlineData("audit", "tenant-generation")]
    [InlineData("audit", "eventstore-revoked")]
    [InlineData("audit", "eventstore-generation")]
    [InlineData("audit", "detail")]
    [InlineData("folder", "tenant-revoked")]
    [InlineData("folder", "tenant-stale")]
    [InlineData("folder", "eventstore-revoked")]
    [InlineData("folder", "eventstore-generation")]
    [InlineData("folder", "detail")]
    public async Task Review2LegacyOpenRechecksAuthorityAndDetailAfterAwait(string blocked, string change)
    {
        ArgumentNullException.ThrowIfNull(change);
        using var rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var folders = new ProjectReadTestFolders();
        async Task Block() { entered.TrySetResult(); await release.Task.ConfigureAwait(false); }
        if (blocked == "folder") folders.OnRead = _ => Block();
        var validator = new ProjectReadTestAuthorization(); bool changed = false;
        validator.Decision = _ => changed && change == "eventstore-revoked" ? EventStoreAuthorizationValidationResult.Denied()
            : EventStoreAuthorizationValidationResult.Allowed(changed && change == "eventstore-generation" ? "new-authority" : "initial-authority");
        Hexalith.Projects.Infrastructure.DaprProjectProjectionStore? journal = null;
        ProjectDetailItem? changedDetail = null;
        var read = new CutoverReviewDetailReadModel((tenant, id, ct) => changedDetail is not null && id == "project-a"
            ? Task.FromResult<ProjectDetailItem?>(changedDetail) : journal!.GetDetailAsync(tenant, id, ct));
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        {
            services.RemoveAll<IProjectFolderDirectory>(); services.AddSingleton<IProjectFolderDirectory>(folders);
            services.RemoveAll<IProjectDetailReadModel>(); services.AddSingleton<IProjectDetailReadModel>(read);
            services.RemoveAll<IProjectEventStoreAuthorizationValidator>(); services.AddSingleton<IProjectEventStoreAuthorizationValidator>(validator);
            if (blocked == "audit")
            {
                services.RemoveAll<ProjectReadAudit>();
                services.AddSingleton(sp => new ProjectReadAudit(new ProjectReadTestAuditStore(sp.GetRequiredService<InMemoryReadModelStore>())
                    { BeforeWrite = Block }, sp.GetRequiredService<TimeProvider>()));
            }
        });
        journal = fixture.Journal;
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using var client = CutoverClient(fixture.App, key);
        string before = await JournalBytesAsync(fixture.State);
        Task<HttpResponseMessage> pending = client.GetAsync("/api/v1/projects/project-a?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (change.StartsWith("tenant-", StringComparison.Ordinal)) await ChangeTenantAsync(fixture.App.Services, change);
            if (change == "detail") changedDetail = (await journal.GetDetailAsync("tenant-a", "project-a", TestContext.Current.CancellationToken))! with { Name = "changed-protected-name", Sequence = 99 };
            changed = true; release.TrySetResult();
            using var response = await pending.ConfigureAwait(true);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            raw.ShouldNotContain("Project project-"); raw.ShouldNotContain("changed-protected-name"); raw.ShouldNotContain("setup");
            (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        }
        finally { release.TrySetResult(); await fixture.App.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Theory]
    [InlineData("context", "tenant-revoked", true)]
    [InlineData("context", "tenant-stale", true)]
    [InlineData("context", "tenant-generation", true)]
    [InlineData("context", "eventstore-revoked", true)]
    [InlineData("context", "eventstore-generation", true)]
    [InlineData("context", "detail", true)]
    [InlineData("explain", "tenant-revoked", true)]
    [InlineData("explain", "detail", true)]
    [InlineData("conversation-start", "tenant-revoked", true)]
    [InlineData("conversation-start", "detail", true)]
    [InlineData("context", "none", false)]
    [InlineData("explain", "eventstore-revoked", false)]
    [InlineData("conversation-start", "tenant-revoked", false)]
    public async Task Review2ContextRechecksAfterBlockedNativeMembership(string operation, string change, bool membershipAllowed)
    {
        ArgumentNullException.ThrowIfNull(change);
        using var rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var membership = new CurrentProjectConversationMembership(asyncDecision: async () =>
        { entered.TrySetResult(); await release.Task.ConfigureAwait(false); return membershipAllowed; });
        bool changed = false; var validator = new ProjectReadTestAuthorization();
        validator.Decision = _ => changed && change == "eventstore-revoked" ? EventStoreAuthorizationValidationResult.Denied()
            : EventStoreAuthorizationValidationResult.Allowed(changed && change == "eventstore-generation" ? "changed-authority" : "initial-authority");
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        {
            services.RemoveAll<IProjectConversationMembershipDirectory>(); services.AddSingleton<IProjectConversationMembershipDirectory>(membership);
            services.RemoveAll<IProjectEventStoreAuthorizationValidator>(); services.AddSingleton<IProjectEventStoreAuthorizationValidator>(validator);
        });
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using var client = CutoverClient(fixture.App, key);
        object payload = operation == "context" ? new GetProjectContextQuery("project-a") { ConversationId = "bound-conversation" }
            : operation == "explain" ? new ExplainContextSelectionQuery("project-a") { ConversationId = "bound-conversation" }
            : new GetConversationStartSetupQuery("project-a") { ConversationId = "bound-conversation" };
        string type = operation == "context" ? ProjectsServerModule.GetProjectContextQueryType : operation == "explain"
            ? ProjectsServerModule.ExplainContextSelectionQueryType : ProjectsServerModule.GetConversationStartSetupQueryType;
        var store = fixture.App.Services.GetRequiredService<InMemoryReadModelStore>();
        Task<HttpResponseMessage> pending = client.PostAsJsonAsync("/query", CutoverQuery("project-a", type, payload), TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (change.StartsWith("tenant-", StringComparison.Ordinal)) await ChangeTenantAsync(fixture.App.Services, change);
            if (change == "detail")
            {
                string storageKey = ConversationStartSetupProjectionHandler.Key("tenant-a", "project-a");
                var detail = (await store.GetAsync<ProjectDetailItem>(ConversationStartSetupProjectionHandler.StoreName, storageKey, TestContext.Current.CancellationToken)).Value!;
                await store.SaveAsync(ConversationStartSetupProjectionHandler.StoreName, storageKey, detail with { Name = "changed-protected-name", Sequence = 99 }, TestContext.Current.CancellationToken);
            }
            var before = Snapshot(store); changed = true; release.TrySetResult();
            using var response = await pending.ConfigureAwait(true);
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            if (change == "tenant-stale" || change == "none")
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                var result = await response.Content.ReadFromJsonAsync<Hexalith.EventStore.Contracts.Queries.QueryResult>(TestContext.Current.CancellationToken);
                using var body = JsonDocument.Parse(result!.PayloadBytes!);
                JsonElement context = operation == "explain" ? body.RootElement.GetProperty("context") : body.RootElement;
                context.GetProperty("setup").ValueKind.ShouldBe(JsonValueKind.Null);
                context.GetProperty("snapshot").GetProperty("responseState").GetString().ShouldBe("Unavailable");
            }
            else response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            raw.ShouldNotContain("changed-protected-name");
            Snapshot(store).ShouldBe(before);
        }
        finally { release.TrySetResult(); await fixture.App.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Theory]
    [InlineData(ProjectFolderValidationOutcome.Denied, true)]
    [InlineData(ProjectFolderValidationOutcome.Stale, true)]
    [InlineData(ProjectFolderValidationOutcome.Unavailable, true)]
    [InlineData(ProjectFolderValidationOutcome.Denied, false)]
    [InlineData(ProjectFolderValidationOutcome.Stale, false)]
    [InlineData(ProjectFolderValidationOutcome.Unavailable, false)]
    public async Task Review2AuthenticatedChatbotAndTenantRolesShareTheCurrentFolderBoundary(ProjectFolderValidationOutcome outcome, bool chatbot)
    {
        using var rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var folders = new ProjectReadTestFolders();
        foreach (string id in new[] { "project-a", "project-b", "project-c" }) folders.Outcomes["folder-" + id] = outcome;
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        { services.RemoveAll<IProjectFolderDirectory>(); services.AddSingleton<IProjectFolderDirectory>(folders); });
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using var client = CutoverClient(fixture.App, key);
        string workload = chatbot ? "chatbot" : "operator-web";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key, role: chatbot ? "ProjectUser" : "TenantOperator", workload: workload));
        string before = await JournalBytesAsync(fixture.State);
        using var retained = await client.GetAsync("/api/v1/projects?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        using var currentList = await client.PostAsJsonAsync("/query", CutoverQuery("projects", ListProjectsQuery.QueryType,
            new ListProjectsQuery(InspectDescriptiveMetadata: true)) with { AuthenticatedWorkloadId = workload }, TestContext.Current.CancellationToken);
        currentList.StatusCode.ShouldBe(HttpStatusCode.OK);
        string supportedRaw = await currentList.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var listResult = JsonSerializer.Deserialize<Hexalith.EventStore.Contracts.Queries.QueryResult>(supportedRaw, JsonSerializerOptions.Web)!;
        var supported = JsonSerializer.Deserialize<ProjectListPage>(listResult.PayloadBytes!, JsonSerializerOptions.Web)!;
        string retainedRaw = await retained.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (chatbot)
        {
            supported.Items.ShouldBeEmpty(); retainedRaw.ShouldNotContain("Project project-");
            retained.StatusCode.ShouldBe(outcome == ProjectFolderValidationOutcome.Denied ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
            supported.Snapshot.ResponseState.ShouldBe(outcome == ProjectFolderValidationOutcome.Denied ? AdmissionResponseState.Complete : AdmissionResponseState.Unavailable);
        }
        else { retained.StatusCode.ShouldBe(HttpStatusCode.OK); supported.Items.Count.ShouldBe(3); retainedRaw.ShouldContain("Project project-a"); }
        using var oldOpen = await client.GetAsync("/api/v1/projects/project-a?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        using var newOpen = await client.PostAsJsonAsync("/query", CutoverQuery("project-a", GetProjectQuery.QueryType, new GetProjectQuery(true)) with
            { AuthenticatedWorkloadId = workload }, TestContext.Current.CancellationToken);
        oldOpen.StatusCode.ShouldBe(chatbot ? HttpStatusCode.NotFound : HttpStatusCode.OK);
        newOpen.StatusCode.ShouldBe(oldOpen.StatusCode);
        string oldOpenRaw = await oldOpen.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        string newOpenRaw = await newOpen.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (chatbot) { oldOpenRaw.ShouldNotContain("Project project-"); newOpenRaw.ShouldNotContain("Project project-"); }
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        string? observation = Environment.GetEnvironmentVariable("STORY67_AUTHENTICATED_OBSERVATIONS");
        if (!string.IsNullOrWhiteSpace(observation))
        {
            string path = Path.ChangeExtension(observation, outcome + "-" + (chatbot ? "chatbot" : "tenant") + ".json");
            File.Exists(path).ShouldBeFalse();
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { outcome, chatbot, executed = true,
                passed = true, noDomainWrites = true, acceptedNormalizationPair = false,
                qualification = "Actual caller-specific safe outcomes; no D2 normalization or gate scenario added.",
                legacyListStatus = (int)retained.StatusCode, legacyListRaw = retainedRaw,
                supportedListStatus = (int)currentList.StatusCode, supportedListRaw = supportedRaw,
                legacyOpenStatus = (int)oldOpen.StatusCode, legacyOpenRaw = oldOpenRaw,
                supportedOpenStatus = (int)newOpen.StatusCode, supportedOpenRaw = newOpenRaw },
                new JsonSerializerOptions(JsonSerializerOptions.Web) { WriteIndented = true }), TestContext.Current.CancellationToken);
        }
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }

    private static async Task ChangeTenantAsync(IServiceProvider services, string change)
    {
        if (change == "tenant-stale") { ((ProjectReadTestClock)services.GetRequiredService<TimeProvider>()).Now += TimeSpan.FromMinutes(6); return; }
        var tenants = services.GetRequiredService<IProjectTenantAccessProjectionStore>();
        var value = (await tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken).ConfigureAwait(false))!;
        if (change == "tenant-revoked") value.Principals.Remove("actor-1");
        else { value.Watermark++; value.ProjectionWatermark = "changed-tenant-generation"; }
        await tenants.SaveAsync(value, TestContext.Current.CancellationToken).ConfigureAwait(false);
    }
}
