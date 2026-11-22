namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Projections.ProjectReads;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadHttpTests
{
    [Fact]
    public async Task SevenAuthenticatedReadsRetainIdenticalPersistedHistoryAndExposeUnresolvedComparisonCells()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story67-authenticated" };
        WebApplication app = await StartAsync(null, null, key, conversations: new AdvisoryTestConversationDirectory());
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            ProjectionRequest history = SupportedProjectReadFixture.Request("project-a", 100);
            InMemoryReadModelStore store = app.Services.GetRequiredService<InMemoryReadModelStore>();
            InMemoryProjectListReadModel legacyList = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            InMemoryProjectDetailReadModel legacyDetail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            foreach (ProjectionEventDto item in history.Events)
            {
                IProjectEvent value = item.SequenceNumber == 1
                    ? JsonSerializer.Deserialize<ProjectCreated>(item.Payload, SupportedProjectReadFixture.Json)!
                    : JsonSerializer.Deserialize<ProjectFolderSet>(item.Payload, SupportedProjectReadFixture.Json)!;
                legacyList.Project(history.TenantId, value);
                legacyDetail.Project(history.TenantId, value);
            }
            await new ProjectReadProjectionHandler(store, store).ProjectAsync(history, "cutover-project-reads", TestContext.Current.CancellationToken);
            await new ConversationStartSetupProjectionHandler(store).ProjectAsync(history, "cutover-setup", TestContext.Current.CancellationToken);
            Dictionary<string, string> before = Snapshot(store);
            string legacyBefore = JsonSerializer.Serialize(await legacyDetail.GetAsync("tenant-a", "project-a", TestContext.Current.CancellationToken));
            var paths = new (string Operation, string Target, string Query, object Payload, string Legacy)[]
            {
                ("list", "projects", ListProjectsQuery.QueryType, new ListProjectsQuery(), "/api/v1/projects"),
                ("open", "project-a", GetProjectQuery.QueryType, new GetProjectQuery(), "/api/v1/projects/project-a"),
                ("conversation-start", "project-a", ProjectsServerModule.GetConversationStartSetupQueryType, new GetConversationStartSetupQuery("project-a"), "/api/v1/projects/project-a/setup/conversation-start"),
                ("context", "project-a", ProjectsServerModule.GetProjectContextQueryType, new GetProjectContextQuery("project-a"), "/api/v1/projects/project-a/context"),
                ("explain", "project-a", ProjectsServerModule.ExplainContextSelectionQueryType, new ExplainContextSelectionQuery("project-a"), "/api/v1/projects/project-a/context/explain"),
                ("resolve-conversation", "projects", ResolveProjectFromConversationQuery.QueryType, new ResolveProjectFromConversationQuery("conversation"), "/api/v1/projects/resolution/from-conversation?conversationId=conversation"),
                ("resolve-attachments", "projects", ResolveProjectFromAttachmentsQuery.QueryType, new ResolveProjectFromAttachmentsQuery([new("file", "opaque-file")]), "/api/v1/projects/resolution/from-attachments?fileId=opaque-file"),
            };
            List<object> observations = [];
            foreach (var path in paths)
            {
                QueryEnvelope query = new("tenant-a", "projects", path.Target, path.Query,
                    JsonSerializer.SerializeToUtf8Bytes(path.Payload, path.Payload.GetType(), JsonSerializerOptions.Web), "cutover-" + path.Operation, "actor-1")
                {
                    OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true,
                    DelegationId = "delegation-1", Scopes = ["projects.read", "projects.list"], Audience = [Audience],
                };
                using HttpResponseMessage supported = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
                supported.EnsureSuccessStatusCode();
                QueryResult result = (await supported.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!;
                result.Success.ShouldBeTrue(result.ErrorMessage);
                using HttpResponseMessage legacy = await client.GetAsync(path.Legacy, TestContext.Current.CancellationToken);
                // A safe refusal remains a comparison observation, never an equivalence success.
                string legacyBytes = await legacy.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                observations.Add(new
                {
                    path.Operation, SupportedStatus = (int)supported.StatusCode, LegacyStatus = (int)legacy.StatusCode,
                    SupportedPayloadHash = Convert.ToHexString(SHA256.HashData(result.PayloadBytes!)),
                    LegacyPayloadHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(legacyBytes))),
                    ComparisonPassed = false, Reason = "full-approved-normalization-and-security-matrix-incomplete",
                });
                Snapshot(store).ShouldBe(before);
            }
            JsonSerializer.Serialize(await legacyDetail.GetAsync("tenant-a", "project-a", TestContext.Current.CancellationToken)).ShouldBe(legacyBefore);
            string? output = Environment.GetEnvironmentVariable("STORY67_AUTHENTICATED_OBSERVATIONS");
            if (!string.IsNullOrWhiteSpace(output))
                await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { Fixture = "JWT-authenticated-host-with-controlled-authorities", AcceptedGatewayOwnerEquivalence = false, observations, PersistedByteHashes = before }, new JsonSerializerOptions { WriteIndented = true }), TestContext.Current.CancellationToken);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static Dictionary<string, string> Snapshot(InMemoryReadModelStore store)
        => new[] { ProjectReadModelKeys.StoreName, ConversationStartSetupProjectionHandler.StoreName }
            .SelectMany(name => store.SnapshotRawEntries(name).Where(entry => !ProjectAuditStorageKeys.IsAuditKey(entry.Key)).Select(entry => new KeyValuePair<string, string>(name + "/" + entry.Key, Convert.ToHexString(SHA256.HashData(entry.Value)))))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
}
