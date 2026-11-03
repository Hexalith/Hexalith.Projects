namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Shouldly;
using Xunit;

/// <summary>Verifies authorized enumeration independently of each Project's readiness.</summary>
public sealed partial class SupportedProjectReadTests
{
    /// <summary>Preserves every row's evidence and persisted state across complete pages.</summary>
    /// <param name="pageSize">The number of rows requested per scoped continuation.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task CompleteEnumerationPreservesMixedRowEvidenceAcrossContinuations(int pageSize)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        string[] ids = ["project-a", "project-b", "project-c", "project-d", "project-e", "project-f"];
        for (int index = 0; index < ids.Length; index++) { await fixture.SeedAsync(ids[index], (index + 1) * 100); }
        foreach (string id in new[] { "project-b", "project-e" })
        {
            string key = ProjectReadModelKeys.Detail("tenant-a", id);
            ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
            fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, key, model with
            {
                Detail = model.Detail! with
                {
                    MemoryReferences = [new ProjectMemoryReference("memory-1", null, ReferenceState.Unavailable, null, SupportedProjectReadFixture.EventTime)],
                },
            });
        }

        fixture.Folders.Outcomes["folder-project-c"] = ProjectFolderValidationOutcome.Stale;
        string rebuildingKey = ProjectReadModelKeys.Detail("tenant-a", "project-f");
        ProjectReadModel rebuilding = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, rebuildingKey)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, rebuildingKey, rebuilding with { Freshness = EvidenceFreshnessState.Rebuilding });

        string indexKey = ProjectReadModelKeys.Index("tenant-a");
        ReadModelEntry<ProjectReadIndex> indexBefore = await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, TestContext.Current.CancellationToken);
        ReadModelEntry<ProjectReadModel>[] before = await Task.WhenAll(ids.Select(id => fixture.Store.GetAsync<ProjectReadModel>(
            ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", id), TestContext.Current.CancellationToken)));
        int count = fixture.Store.Count;
        AdmissionResponseState[] states = [AdmissionResponseState.Complete, AdmissionResponseState.Partial, AdmissionResponseState.Unavailable,
            AdmissionResponseState.Complete, AdmissionResponseState.Partial, AdmissionResponseState.Unavailable];
        List<ProjectListRow> rows = [];
        string? cursor = null;
        do
        {
            ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
                fixture.Query(new ListProjectsQuery(Cursor: cursor, PageSize: pageSize), correlation: $"page-{rows.Count}"), TestContext.Current.CancellationToken));
            page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
            page.Snapshot.AsOf.ShouldBe(fixture.Clock.Now);
            page.Snapshot.ProjectVersion.ShouldBe(0);
            page.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.None]);
            page.Snapshot.Components.Single().ShouldBe(new AdmissionComponent("Enumeration", true, EvidenceFreshnessState.Current, "Current")
            {
                LastVerifiedAt = indexBefore.Value!.LastVerifiedAt,
            });
            page.Items.Count.ShouldBeGreaterThan(0);
            page.HasMore.ShouldBe(rows.Count + page.Items.Count < ids.Length);
            (page.Cursor is not null).ShouldBe(page.HasMore);
            rows.AddRange(page.Items);
            cursor = page.Cursor;
        }
        while (cursor is not null);

        rows.Select(row => row.ProjectId).ShouldBe(ids);
        rows.Select(row => row.Snapshot.ResponseState).ShouldBe(states);
        rows.ShouldAllBe(row => row.Name == null && row.Snapshot.ProjectVersion == 2 && row.Snapshot.AsOf == fixture.Clock.Now);
        for (int index = 0; index < ids.Length; index++)
        {
            GetProjectResponse opened = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(
                fixture.Query(new GetProjectQuery(), ids[index]), TestContext.Current.CancellationToken));
            JsonSerializer.Serialize(rows[index].Snapshot).ShouldBe(JsonSerializer.Serialize(opened.Snapshot));
            rows[index].Snapshot.RecoveryActions.ShouldBe(states[index] == AdmissionResponseState.Complete
                ? [AdmissionRecoveryAction.None] : [AdmissionRecoveryAction.Retry]);
            ReadModelEntry<ProjectReadModel> after = await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
                ProjectReadModelKeys.Detail("tenant-a", ids[index]), TestContext.Current.CancellationToken);
            JsonSerializer.Serialize(after.Value).ShouldBe(JsonSerializer.Serialize(before[index].Value));
            after.ETag.ShouldBe(before[index].ETag);
        }

        rows[1].Snapshot.Components.Single(component => component.Name == "References").Included.ShouldBeFalse();
        rows[2].Snapshot.Components.Single(component => component.Name == "Folder").Freshness.ShouldBe(EvidenceFreshnessState.Stale);
        rows[5].Snapshot.Components.Single(component => component.Name == "Project").Freshness.ShouldBe(EvidenceFreshnessState.Rebuilding);
        ReadModelEntry<ProjectReadIndex> indexAfter = await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, TestContext.Current.CancellationToken);
        JsonSerializer.Serialize(indexAfter.Value).ShouldBe(JsonSerializer.Serialize(indexBefore.Value));
        indexAfter.ETag.ShouldBe(indexBefore.ETag);
        fixture.Store.Count.ShouldBe(count);
        fixture.AuditStore.Keys.ShouldBeEmpty();
    }

    /// <summary>A current authorized view remains complete when no rows match.</summary>
    /// <param name="scenario">The reason the authorized view is empty.</param>
    [Theory]
    [InlineData("empty-index")]
    [InlineData("lifecycle-filter")]
    [InlineData("quarantined-only")]
    [InlineData("chatbot-filtered")]
    public async Task CurrentAuthorizedEmptyEnumerationIsComplete(string scenario)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: scenario == "chatbot-filtered");
        if (scenario == "empty-index")
        {
            fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"),
                new ProjectReadIndex("tenant-a", [], 100, SupportedProjectReadFixture.EventTime));
        }
        else
        {
            await fixture.SeedAsync("project-a", folder: scenario != "quarantined-only");
            if (scenario == "chatbot-filtered") { fixture.Folders.Deny("folder-project-a"); }
        }

        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(LifecycleState: scenario == "lifecycle-filter" ? "Archived" : null)), TestContext.Current.CancellationToken));
        page.Items.ShouldBeEmpty();
        page.Cursor.ShouldBeNull();
        page.HasMore.ShouldBeFalse();
        page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        page.Snapshot.AsOf.ShouldBe(fixture.Clock.Now);
        page.Snapshot.ProjectVersion.ShouldBe(0);
        page.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.None]);
        page.Snapshot.Components.Single().ShouldBe(new AdmissionComponent("Enumeration", true, EvidenceFreshnessState.Current, "Current")
        {
            LastVerifiedAt = SupportedProjectReadFixture.EventTime,
        });
    }
}
