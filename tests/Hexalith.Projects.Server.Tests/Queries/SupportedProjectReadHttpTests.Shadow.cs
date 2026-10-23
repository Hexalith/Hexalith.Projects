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
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadHttpTests
{
    /// <summary>Compares actual retained HTTP reads with authorized supported metadata on identical Project events.</summary>
    [Fact]
    public async Task LegacyHttpListAndOpenMatchAuthorizedSupportedMetadataAcrossPages()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story61-shadow-http" };
        WebApplication app = await StartAsync(null, null, key, conversations: new AdvisoryTestConversationDirectory());
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            InMemoryProjectListReadModel legacyList = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            InMemoryProjectDetailReadModel legacyDetail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            InMemoryReadModelStore store = app.Services.GetRequiredService<InMemoryReadModelStore>();
            using HttpResponseMessage metadata = await client.PostAsJsonAsync("/admin/operational-index-metadata",
                new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" }, TestContext.Current.CancellationToken);
            metadata.EnsureSuccessStatusCode();
            string fingerprint = (await metadata.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken))!.CatalogFingerprint!;
            string[] projectIds = ["project-a", "project-b"];
            foreach (string id in projectIds)
            {
                ProjectionRequest request = SupportedProjectReadFixture.Request(id, id == "project-a" ? 100 : 200);
                ProjectCreated created = JsonSerializer.Deserialize<ProjectCreated>(request.Events[0].Payload, SupportedProjectReadFixture.Json)!;
                List<IProjectEvent> events =
                [
                    created with { SetupMetadata = id == "project-a" ? "Setup metadata marker" : null },
                    JsonSerializer.Deserialize<ProjectFolderSet>(request.Events[1].Payload, SupportedProjectReadFixture.Json)!,
                ];
                if (id == "project-a")
                {
                    events.Add(new ProjectSetupUpdated("tenant-a", id,
                        new ProjectSetup(["Plan the work"], ["Summarize progress"], [ProjectContextSourceKind.Conversation], [], null),
                        "actor-1", "setup", "task", "setup", "setup", SupportedProjectReadFixture.EventTime.AddSeconds(2)));
                }
                else
                {
                    events.Add(new ProjectArchived("tenant-a", id, ProjectLifecycle.Archived, "actor-1", "archive", "task",
                        "archive", "archive", SupportedProjectReadFixture.EventTime.AddSeconds(3)));
                }
                request = request with { Events = events.Select((value, index) => SupportedProjectReadFixture.Event(value, index + 1, request.Events[0].GlobalPosition + index)
                    with { Timestamp = value.OccurredAt }).ToArray() };
                foreach (IProjectEvent projectEvent in events)
                {
                    legacyList.Project(request.TenantId, projectEvent);
                    legacyDetail.Project(request.TenantId, projectEvent);
                }
                (await DispatchAsync(app, new(request, ["project-reads"], "http-shadow-" + id, fingerprint)))
                    .Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
            }

            byte[] indexBefore = store.SnapshotRaw(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!;
            Dictionary<string, byte[]> detailsBefore = projectIds.ToDictionary(id => id,
                id => store.SnapshotRaw(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", id))!);
            string legacyListBefore = JsonSerializer.Serialize(await legacyList.ListAsync("tenant-a", null, TestContext.Current.CancellationToken));
            Dictionary<string, string> legacyDetailsBefore = [];
            foreach (string id in projectIds)
                legacyDetailsBefore[id] = JsonSerializer.Serialize(await legacyDetail.GetAsync("tenant-a", id, TestContext.Current.CancellationToken));
            int storedKeysBefore = store.Count;

            using HttpResponseMessage legacyListed = await client.GetAsync("/api/v1/projects", TestContext.Current.CancellationToken);
            legacyListed.EnsureSuccessStatusCode();
            JsonElement legacyPage = await legacyListed.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            QueryEnvelope list = new("tenant-a", "projects", "projects", ListProjectsQuery.QueryType,
                JsonSerializer.SerializeToUtf8Bytes(new ListProjectsQuery(PageSize: 1, InspectDescriptiveMetadata: true), SupportedProjectReadFixture.Json),
                "http-shadow-list-first", "actor-1")
            {
                OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true,
                DelegationId = "delegation-1", Scopes = ["projects.read", "projects.list"], Audience = [Audience],
            };
            ProjectListPage first = await ReadShadowQueryAsync<ProjectListPage>(client, list);
            first.HasMore.ShouldBeTrue(); first.Cursor.ShouldNotBeNullOrWhiteSpace();
            ProjectListPage second = await ReadShadowQueryAsync<ProjectListPage>(client, list with
            {
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1, InspectDescriptiveMetadata: true), SupportedProjectReadFixture.Json),
                CorrelationId = "http-shadow-list-next",
            });
            second.HasMore.ShouldBeFalse(); second.Cursor.ShouldBeNull();
            ProjectListRow[] supportedRows = [.. first.Items, .. second.Items];
            supportedRows.Select(row => row.ProjectId).ShouldBe(projectIds);
            JsonElement[] legacyRows = legacyPage.GetProperty("items").EnumerateArray().ToArray();
            legacyRows.Select(row => row.GetProperty("projectId").GetString()).ShouldBe(projectIds);
            for (int index = 0; index < projectIds.Length; index++)
            {
                JsonElement legacyRow = legacyRows[index];
                ProjectListRow supportedRow = supportedRows[index];
                supportedRow.Name.ShouldBe(legacyRow.GetProperty("name").GetString());
                supportedRow.LifecycleState.ToString().ToLowerInvariant().ShouldBe(legacyRow.GetProperty("lifecycleState").GetString());
                supportedRow.ProjectFolderAvailability.ShouldBe(ReferenceState.Included);
                supportedRow.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);

                using HttpResponseMessage legacyOpened = await client.GetAsync("/api/v1/projects/" + projectIds[index], TestContext.Current.CancellationToken);
                legacyOpened.EnsureSuccessStatusCode();
                JsonElement legacyOpen = await legacyOpened.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
                GetProjectResponse supportedOpen = await ReadShadowQueryAsync<GetProjectResponse>(client, list with
                {
                    AggregateId = projectIds[index], QueryType = GetProjectQuery.QueryType,
                    Payload = JsonSerializer.SerializeToUtf8Bytes(new GetProjectQuery(true), SupportedProjectReadFixture.Json),
                    CorrelationId = "http-shadow-open-" + projectIds[index],
                });
                supportedOpen.ProjectId.ShouldBe(legacyOpen.GetProperty("projectId").GetString());
                supportedOpen.Name.ShouldBe(legacyOpen.GetProperty("name").GetString());
                supportedOpen.Description.ShouldBe(legacyOpen.GetProperty("description").GetString());
                supportedOpen.LifecycleState.ToString().ToLowerInvariant().ShouldBe(legacyOpen.GetProperty("lifecycleState").GetString());
                supportedOpen.CreatedAt.ShouldBe(legacyOpen.GetProperty("createdAt").GetDateTimeOffset());
                supportedOpen.UpdatedAt.ShouldBe(legacyOpen.GetProperty("updatedAt").GetDateTimeOffset());
                supportedOpen.SetupMetadata.ShouldBe(legacyOpen.GetProperty("setupMetadata").GetString());
                // The supported contract represents absent legacy setup as an empty bounded value.
                ProjectSetup expectedSetup = legacyOpen.GetProperty("projectSetup").ValueKind == JsonValueKind.Null
                    ? ProjectSetup.Empty : legacyOpen.GetProperty("projectSetup").Deserialize<ProjectSetup>(SupportedProjectReadFixture.Json)!;
                JsonSerializer.Serialize(supportedOpen.Setup).ShouldBe(JsonSerializer.Serialize(expectedSetup));
                JsonElement legacyFolder = legacyOpen.GetProperty("references").EnumerateArray().Single();
                ProjectReadReference supportedFolder = supportedOpen.References.Single();
                legacyFolder.GetProperty("referenceKind").GetString().ShouldBe("folder");
                supportedFolder.ReferenceKind.ShouldBe("ProjectFolder");
                supportedFolder.FolderId.ShouldBe(legacyFolder.GetProperty("referenceId").GetString());
                supportedFolder.DisplayName.ShouldBe(legacyFolder.GetProperty("displayName").GetString());
                supportedFolder.ReferenceState.ToString().ToLowerInvariant().ShouldBe(legacyFolder.GetProperty("referenceState").GetString());
                supportedOpen.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
                supportedOpen.Snapshot.ProjectVersion.ShouldBe(3);
                supportedOpen.UpdatedAt.ShouldBeGreaterThan(supportedOpen.CreatedAt);
            }

            foreach (ProjectLifecycle lifecycle in new[] { ProjectLifecycle.Active, ProjectLifecycle.Archived })
            {
                using HttpResponseMessage legacyFiltered = await client.GetAsync("/api/v1/projects?lifecycle=" + lifecycle.ToString().ToLowerInvariant(), TestContext.Current.CancellationToken);
                legacyFiltered.EnsureSuccessStatusCode();
                JsonElement legacyFilteredPage = await legacyFiltered.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
                ProjectListPage filtered = await ReadShadowQueryAsync<ProjectListPage>(client, list with
                {
                    Payload = JsonSerializer.SerializeToUtf8Bytes(new ListProjectsQuery(lifecycle.ToString(), InspectDescriptiveMetadata: true), SupportedProjectReadFixture.Json),
                    CorrelationId = "http-shadow-filter-" + lifecycle,
                });
                filtered.Items.Select(row => row.ProjectId).ShouldBe(legacyFilteredPage.GetProperty("items").EnumerateArray()
                    .Select(row => row.GetProperty("projectId").GetString()));
                filtered.Items.Single().LifecycleState.ShouldBe(lifecycle);
            }

            // Two pages, two opens and two filtered lists add only inspection audit records.
            store.Count.ShouldBe(storedKeysBefore + 6);
            store.SnapshotRaw(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a")).ShouldBe(indexBefore);
            foreach (string id in projectIds)
            {
                store.SnapshotRaw(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", id)).ShouldBe(detailsBefore[id]);
                JsonSerializer.Serialize(await legacyDetail.GetAsync("tenant-a", id, TestContext.Current.CancellationToken)).ShouldBe(legacyDetailsBefore[id]);
            }
            JsonSerializer.Serialize(await legacyList.ListAsync("tenant-a", null, TestContext.Current.CancellationToken)).ShouldBe(legacyListBefore);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task<T> ReadShadowQueryAsync<T>(HttpClient client, QueryEnvelope query)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        QueryResult result = (await response.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken).ConfigureAwait(false))!;
        result.Success.ShouldBeTrue(result.ErrorMessage);
        T payload = SupportedProjectReadFixture.Payload<T>(result);
        JsonElement.DeepEquals(result.GetPayload(), JsonSerializer.SerializeToElement(payload, SupportedProjectReadFixture.Json))
            .ShouldBeTrue("Raw supported HTTP payload must contain only fields represented by its closed contract, including nested objects.");
        return payload;
    }
}
