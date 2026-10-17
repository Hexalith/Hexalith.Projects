namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;
using Microsoft.AspNetCore.DataProtection;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Testing.Reads;
using Shouldly;
using Xunit;

public sealed class SupportedProjectShadowTests
{
    [Fact]
    public async Task LocalLegacyListAndOpenMatchPersistedReadsAndDecodedContinuation()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        ProjectionRequest[] requests = [SupportedProjectReadFixture.Request("project-a", 504), SupportedProjectReadFixture.Request("project-b", 700), SupportedProjectReadFixture.Request("project-c", 900)];
        ProjectProjectionEnvelope[] source = requests.SelectMany(request => request.Events.Select(item => new ProjectProjectionEnvelope(request.TenantId, item.SequenceNumber,
            item.SequenceNumber == 1 ? JsonSerializer.Deserialize<ProjectCreated>(item.Payload, SupportedProjectReadFixture.Json)!
                : JsonSerializer.Deserialize<ProjectFolderSet>(item.Payload, SupportedProjectReadFixture.Json)!))).ToArray();
        ProjectListProjection legacyList = ProjectListProjection.Rebuild(source);
        ProjectDetailProjection legacyDetails = ProjectDetailProjection.Rebuild(source);
        foreach (ProjectionRequest request in requests) { await fixture.Projection.ProjectAsync(request, "shadow-" + request.AggregateId, TestContext.Current.CancellationToken); }
        fixture.Folders.Denied.Add("folder-project-a");
        ProjectReadTestFolders legacyOwner = new(); legacyOwner.Denied.Add("folder-project-a");
        ProjectReadTestAuthorization legacyAuthority = new();
        QueryCursorScope legacyVisibility = QueryCursorScope.Create();
        List<ProjectListItem> legacyVisible = [];
        foreach (ProjectListItem row in legacyList.List("tenant-a", lifecycleFilter: null))
        {
            ProjectDetailItem detail = legacyDetails.Get("tenant-a", row.ProjectId)!;
            var ownerRead = await legacyOwner.RefreshFolderReferenceAsync(new ProjectId(row.ProjectId), detail.ProjectFolder!.FolderId!, "legacy-shadow", TestContext.Current.CancellationToken);
            bool included = ownerRead.Outcome == ProjectFolderValidationOutcome.Accepted;
            legacyVisibility.Add("project", row.ProjectId).Add("allowed", bool.TrueString).Add("authorization", legacyAuthority.Watermark)
                .Add("folder", included ? ReferenceState.Included.ToString() : ReferenceState.Unauthorized.ToString());
            if (included) { legacyVisible.Add(row); }
        }
        QueryEnvelope listQuery = fixture.Query(new ListProjectsQuery(PageSize: 1));
        ReadModelEntry<ProjectReadIndex> observedIndex = await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"), TestContext.Current.CancellationToken);
        long sourceHead = requests.SelectMany(request => request.Events).Max(item => item.GlobalPosition);
        var tenantAuthority = (await fixture.Tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken))!;
        string expectedScope = QueryCursorScope.Create().Add("tenant", listQuery.TenantId).Add("actor", listQuery.OriginalActorId)
            .Add("workload", listQuery.AuthenticatedWorkloadId).Add("delegation", listQuery.DelegationId)
            .Add("delegated", listQuery.IsDelegated.ToString()).Add("scopes", JsonSerializer.Serialize(listQuery.Scopes))
            .Add("audience", JsonSerializer.Serialize(listQuery.Audience)).Add("action", ProjectAuthorizationGate.ListProjectsAction)
            .Add("lifecycle", (string?)null).Add("pageSize", "1").Add("inspect", bool.FalseString).Add("policy", "project-list-v1-50-200-15m")
            .Add("authority", tenantAuthority.ProjectionWatermark).Add("eventStoreAuthority", legacyAuthority.Watermark).Add("eventStoreFreshness", "fresh")
            .Add("indexETag", observedIndex.ETag).Add("visibility", Digest(legacyVisibility.Build())).AddProjectionWatermark(sourceHead).Build();
        expectedScope = Digest(expectedScope);
        ProjectListPage first = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(listQuery, TestContext.Current.CancellationToken));
        first.Cursor.ShouldNotBeNull();
        using JsonDocument decoded = JsonDocument.Parse(fixture.DataProtection.CreateProtector("story61-tests").Unprotect(first.Cursor!));
        string actualScope = decoded.RootElement.GetProperty("scope").GetString()!;
        string actualPosition = decoded.RootElement.GetProperty("position").GetString()!;
        new QueryCursorCodec(fixture.DataProtection, "story61-tests").TryDecode(first.Cursor, ListProjectsQuery.QueryType, actualScope, out string? verifiedPosition, out _).ShouldBeTrue();
        verifiedPosition.ShouldBe(actualPosition);
        using JsonDocument logicalCursor = JsonDocument.Parse(actualPosition);
        string actualBoundary = logicalCursor.RootElement.GetProperty("projectId").GetString()!;
        ProjectListPage second = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1), correlation: "shadow-next"), TestContext.Current.CancellationToken));
        second.HasMore.ShouldBeFalse();
        ProjectDetailItem legacyOpen = legacyDetails.Get("tenant-a", legacyVisible[0].ProjectId)!;
        GetProjectResponse supportedOpen = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), legacyVisible[0].ProjectId, "shadow-open"), TestContext.Current.CancellationToken));
        object expectedPayload = new
        {
            Items = legacyVisible.Select(row => new { row.ProjectId, row.Name, LifecycleState = row.Lifecycle }).ToArray(),
            Open = new { legacyOpen.ProjectId, legacyOpen.Name, legacyOpen.Description, LifecycleState = legacyOpen.Lifecycle,
                legacyOpen.CreatedAt, legacyOpen.UpdatedAt, Setup = legacyOpen.Setup ?? Hexalith.Projects.Contracts.Models.ProjectSetup.Empty,
                legacyOpen.SetupMetadata, FolderId = legacyOpen.ProjectFolder!.FolderId, DisplayName = legacyOpen.ProjectFolder.DisplayName, legacyOpen.Sequence },
        };
        object actualPayload = new
        {
            Items = first.Items.Concat(second.Items).Select(row => new { row.ProjectId, row.Name, row.LifecycleState }).ToArray(),
            Open = new { supportedOpen.ProjectId, supportedOpen.Name, supportedOpen.Description, supportedOpen.LifecycleState,
                supportedOpen.CreatedAt, supportedOpen.UpdatedAt, supportedOpen.Setup, supportedOpen.SetupMetadata,
                FolderId = supportedOpen.References.Single().FolderId, DisplayName = supportedOpen.References.Single().DisplayName, Sequence = supportedOpen.Snapshot.ProjectVersion },
        };
        string[] legacyKeys = legacyDetails.Projects.Keys.Order(StringComparer.Ordinal).ToArray();
        string[] physicalKeys = observedIndex.Value!.ProjectIds.Select(id => ProjectReadModelKeys.Detail(observedIndex.Value.TenantId, id)).ToArray();
        List<ProjectReadModel> persisted = [];
        foreach (string key in physicalKeys)
        {
            ReadModelEntry<ProjectReadModel> entry = await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken);
            entry.ETag.ShouldNotBeNullOrWhiteSpace(); persisted.Add(entry.Value!);
        }
        string[] supportedKeys = persisted.Select(model => new AggregateIdentity(model.TenantId, "projects", model.ProjectId).ActorId).ToArray();
        long[] sourcePositions = requests.OrderBy(request => request.AggregateId, StringComparer.Ordinal).Select(request => request.Events.Max(item => item.GlobalPosition)).ToArray();
        ProjectReadShadowObservation legacy = new(JsonSerializer.SerializeToElement(expectedPayload), legacyKeys, sourcePositions, legacyVisible[0].ProjectId, expectedScope);
        ProjectReadShadowObservation supported = new(JsonSerializer.SerializeToElement(actualPayload), supportedKeys, persisted.Select(model => model.GlobalPosition).ToArray(), actualBoundary, actualScope);
        ProjectReadShadowComparison comparison = ProjectReadShadowComparator.Compare(legacy, supported);
        comparison.Equivalent.ShouldBeTrue(string.Join(",", comparison.Divergences));
        supportedOpen.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete); supportedOpen.Snapshot.AsOf.ShouldBe(fixture.Clock.Now);
        first.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        string? reportPath = Environment.GetEnvironmentVariable("STORY61_SHADOW_REPORT");
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { Fixture = "local-legacy-and-supported-list-open", AcceptedGatewayOwnerFixture = false,
                comparison, LegacyCanonicalKeys = legacyKeys, SupportedPhysicalKeys = physicalKeys, Legacy = legacy, Supported = supported,
                IndexGlobalPosition = observedIndex.Value.GlobalPosition, IndexETag = observedIndex.ETag }, new JsonSerializerOptions { WriteIndented = true }), TestContext.Current.CancellationToken);
        }
    }

    private static string Digest(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    [Theory]
    [InlineData("metadata")]
    [InlineData("order")]
    [InlineData("key")]
    [InlineData("watermark")]
    [InlineData("cursor")]
    [InlineData("authority")]
    [InlineData("extra-field")]
    public void ComparatorDetectsEveryUnnormalizedDifference(string fault)
    {
        var original = new ProjectReadShadowObservation(JsonSerializer.SerializeToElement(new { name = "Protected", ids = new[] { "a", "b" } }), ["tenant:key"], [801], "a", "authority-a");
        var different = fault switch
        {
            "metadata" => original with { Payload = JsonSerializer.SerializeToElement(new { name = "Changed", ids = new[] { "a", "b" } }) },
            "order" => original with { Payload = JsonSerializer.SerializeToElement(new { name = "Protected", ids = new[] { "b", "a" } }) },
            "key" => original with { ReadModelKeys = ["other:key"] },
            "watermark" => original with { GlobalPositions = [802] },
            "cursor" => original with { CursorBoundary = "b" },
            "authority" => original with { ScopeHash = "authority-b" },
            _ => original with { Payload = JsonSerializer.SerializeToElement(new { name = "Protected", ids = new[] { "a", "b" }, extra = true }) },
        };
        ProjectReadShadowComparison result = ProjectReadShadowComparator.Compare(original, different);
        result.Equivalent.ShouldBeFalse(); result.Divergences.ShouldNotBeEmpty();
        JsonSerializer.Serialize(result).ShouldNotContain("Protected");
    }
}
