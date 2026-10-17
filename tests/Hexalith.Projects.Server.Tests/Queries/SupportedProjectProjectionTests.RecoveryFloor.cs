namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectProjectionTests
{
    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(false, 4)]
    [InlineData(true, 4)]
    public async Task FailedObservedSequenceCannotBeClearedByOlderPrefix(bool rebuild, int floor)
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync();
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a");
        request = request with { Events = [request.Events[0], .. Enumerable.Range(2, floor - 1).Select(sequence => request.Events[1] with { SequenceNumber = sequence, GlobalPosition = 99 + sequence })] };
        await fixture.Projection.ProjectAsync(request with { Events = [request.Events[0]] }, "first", TestContext.Current.CancellationToken);
        await fixture.Projection.ProjectAsync(request with { Events = [request.Events[^1] with { EventTypeName = "unknown" }] }, "failed", TestContext.Current.CancellationToken);
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        ProjectReadModel degraded = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        degraded.Sequence.ShouldBe(1); degraded.RecoveryFloor.ShouldBe(floor);
        if (rebuild)
        {
            await Should.ThrowAsync<DomainProjectionRebuildRejectedException>(() => fixture.Projection.PrepareRebuildAsync(request with
                { Events = request.Events[..^1], RebuildSourceHeadSequence = floor - 1 }, "prefix", TestContext.Current.CancellationToken));
        }
        else (await fixture.Projection.ProjectAsync(request with { Events = request.Events[..^1] }, "prefix", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
        fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!.UnavailableProjectIds.ShouldContain("project-a");
        if (rebuild)
        {
            DomainProjectionRebuildPlan plan = await fixture.Projection.PrepareRebuildAsync(request with { RebuildSourceHeadSequence = floor }, "complete", TestContext.Current.CancellationToken);
            (await fixture.Store.ExecuteAsync(new(new(ProjectReadModelKeys.StoreName, "tenant-a", "projects", "project-a", "rebuild", "complete"), plan.Operations), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        }
        else (await fixture.Projection.ProjectAsync(request with { Events = request.Events[1..] }, "redelivery", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!.Freshness.ShouldBe(EvidenceFreshnessState.Current);
        fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!.UnavailableProjectIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task LegacyDegradedModelWithoutFloorRequiresAtLeastNextSequence()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(); await fixture.SeedAsync("project-a", folder: false);
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var previous = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, key, previous with { Freshness = EvidenceFreshnessState.Unavailable, RecoveryFloor = null });
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a");
        (await fixture.Projection.ProjectAsync(request with { Events = [request.Events[0]] }, "old", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        (await fixture.Projection.ProjectAsync(request, "complete", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
    }

    [Fact]
    public async Task OversizedTenantIndexBatchFailsBeforeChangingDetailOrIndex()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(); await fixture.SeedAsync("project-a");
        string detailKey = ProjectReadModelKeys.Detail("tenant-a", "project-a"), indexKey = ProjectReadModelKeys.Index("tenant-a");
        var index = fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, indexKey, index with { ProjectIds = Enumerable.Range(0, 6000).Select(n => n.ToString("D6") + new string('x', 250)).Append("project-a").Order(StringComparer.Ordinal).ToArray() });
        string beforeDetail = JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey));
        string beforeIndex = JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey));
        int count = fixture.Store.Count;
        await Should.ThrowAsync<ArgumentException>(() => fixture.Projection.ProjectAsync(ArchiveRequest(fixture), "oversized", TestContext.Current.CancellationToken));
        fixture.Store.Count.ShouldBe(count);
        JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey)).ShouldBe(beforeDetail);
        JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey)).ShouldBe(beforeIndex);
    }
}
