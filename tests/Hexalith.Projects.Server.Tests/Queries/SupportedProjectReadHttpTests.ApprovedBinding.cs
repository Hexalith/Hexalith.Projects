namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.ProjectReads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadHttpTests
{
    [Theory]
    [InlineData("/context")]
    [InlineData("/context/explain")]
    [InlineData("/context/refresh")]
    public async Task LegacyContextRechecksNativeMembershipAfterItsLastOwnerAwait(string path)
    {
        using RSA rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var changing = new ChangingProjectConversationMembership();
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        {
            services.RemoveAll<Hexalith.Projects.Server.Conversations.IProjectConversationMembershipDirectory>();
            services.AddSingleton<Hexalith.Projects.Server.Conversations.IProjectConversationMembershipDirectory>(changing);
        });
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using HttpClient client = CutoverClient(fixture.App, key);
        string before = await JournalBytesAsync(fixture.State);
        using var response = await client.GetAsync("/api/v1/projects/project-a" + path + "?conversationId=bound-conversation", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain("projectSetup"); body.ShouldNotContain("Project project-a");
        changing.Checks.ShouldBe(2);
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("explain")]
    [InlineData("conversation-start")]
    public async Task ContextRequiresAnExplicitNativeConversationAndNeverFallsBack(string operation)
    {
        using RSA rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var fixture = await CutoverRuntimeAsync(key);
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using HttpClient client = CutoverClient(fixture.App, key);
        var path = operation switch
        {
            "context" => (ProjectsServerModule.GetProjectContextQueryType, "/context"),
            "explain" => (ProjectsServerModule.ExplainContextSelectionQueryType, "/context/explain"),
            _ => (ProjectsServerModule.GetConversationStartSetupQueryType, "/setup/conversation-start"),
        };
        string journal = await JournalBytesAsync(fixture.State);
        foreach (string? conversation in new[] { null, "unbound-conversation", "bound-conversation" })
        {
            var query = CutoverQuery("project-a", path.Item1, new { projectId = "project-a", conversationId = conversation });
            using HttpResponseMessage supported = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            supported.StatusCode.ShouldBe(HttpStatusCode.OK);
            var result = await supported.Content.ReadFromJsonAsync<Hexalith.EventStore.Contracts.Queries.QueryResult>(TestContext.Current.CancellationToken);
            JsonElement payload = result!.GetPayload();
            JsonElement response = operation == "explain" ? payload.GetProperty("context") : payload;
            string state = response.GetProperty("snapshot").GetProperty("responseState").GetString()!;
            state.ShouldBe(conversation == "bound-conversation" ? "Complete" : "Unavailable");
            if (conversation != "bound-conversation") response.GetProperty("setup").ValueKind.ShouldBe(JsonValueKind.Null);
            string legacyUrl = "/api/v1/projects/project-a" + path.Item2 + (conversation is null ? string.Empty : "?conversationId=" + conversation);
            using HttpResponseMessage legacy = await client.GetAsync(legacyUrl, TestContext.Current.CancellationToken);
            legacy.StatusCode.ShouldBe(conversation == "bound-conversation" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
            if (conversation != "bound-conversation")
            {
                string body = await legacy.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                body.ShouldNotContain("Project project-a"); body.ShouldNotContain("projectSetup");
            }
        }
        (await JournalBytesAsync(fixture.State)).ShouldBe(journal);
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("/api/v1/projects")]
    [InlineData("/api/v1/projects/project-a")]
    public async Task LegacyNamesRequireExplicitInspectionAndSuccessfulDurableAudit(string route)
    {
        using RSA rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var fixture = await CutoverRuntimeAsync(key);
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using HttpClient client = CutoverClient(fixture.App, key);
        var store = fixture.App.Services.GetRequiredService<Hexalith.EventStore.Testing.Fakes.InMemoryReadModelStore>();
        string journal = await JournalBytesAsync(fixture.State);
        using HttpResponseMessage ordinary = await client.GetAsync(route, TestContext.Current.CancellationToken);
        ordinary.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ordinary.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain("Project project-a");
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(key, inspection: false));
        using HttpResponseMessage unauthorizedInspection = await client.GetAsync(route + "?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        unauthorizedInspection.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await unauthorizedInspection.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain("Project project-a");
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(key));
        using HttpResponseMessage inspection = await client.GetAsync(route + "?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        inspection.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await inspection.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("Project project-a");
        var audit = store.SnapshotRawEntries(ProjectReadModelKeys.StoreName).Where(entry => entry.Key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal)).ToArray();
        audit.ShouldNotBeEmpty();
        foreach (var entry in audit)
        {
            string persisted = System.Text.Encoding.UTF8.GetString(entry.Value);
            persisted.ShouldContain("Inspection"); persisted.ShouldNotContain("Project project-a");
        }
        store.ConcurrentWriteBeforeTrySave = () => throw new IOException("controlled audit outage");
        using HttpResponseMessage failed = await client.GetAsync(route + "?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        failed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await failed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain("Project project-a");
        (await JournalBytesAsync(fixture.State)).ShouldBe(journal);
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }
}
