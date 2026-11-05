namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadTests
{
    /// <summary>Accepted Folder replacement degrades optional references while enumeration and rebuild stay available.</summary>
    [Fact]
    public async Task ConfirmedFolderReplacementKeepsEnumerationCompleteAndReadsPartial()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a");
        ProjectIdentity identity = new("tenant-a", new ProjectId("project-a"));
        ProjectState state = ProjectState.Empty.Apply(request.Events.Select(item =>
            (IProjectEvent)JsonSerializer.Deserialize(item.Payload, item.SequenceNumber == 1 ? typeof(ProjectCreated) : typeof(ProjectFolderSet), SupportedProjectReadFixture.Json)!), identity);
        ProjectResult link = ProjectAggregate.Handle(state, new LinkFileReference("tenant-a", identity.ProjectId,
            "file-a", "folder-project-a", new("File"), "actor-1", "link", "task", "link"), SupportedProjectReadFixture.EventTime);
        link.IsAccepted.ShouldBeTrue();
        state = state.Apply(link.Events, identity);
        ProjectResult replacement = ProjectAggregate.Handle(state, new SetProjectFolder("tenant-a", identity.ProjectId,
            "replacement-folder", new("Replacement"), true, "actor-1", "replace", "task", "replace"), SupportedProjectReadFixture.EventTime);
        replacement.IsAccepted.ShouldBeTrue();
        request = request with { Events = [.. request.Events, SupportedProjectReadFixture.Event(link.Events.Single(), 3, 102)] };
        (await fixture.Projection.ProjectAsync(request, "linked-prefix", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!.Sequence.ShouldBe(3);
        ProjectionEventDto replacementEvent = SupportedProjectReadFixture.Event(replacement.Events.Single(), 4, 103);
        (await fixture.Projection.ProjectAsync(request with { Events = [replacementEvent] }, "replacement",
            TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        request = request with { Events = [.. request.Events, replacementEvent] };
        byte[] before = fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key)!;
        string? etag = (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken)).ETag;
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        page.Items.Single().ProjectId.ShouldBe("project-a");
        page.Items.Single().Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        GetProjectResponse opened = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(
            fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken));
        opened.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        opened.Snapshot.Components.ShouldContain(component => component.Name == "References" && !component.Included);
        ProjectReadReference folder = opened.References.ShouldHaveSingleItem();
        folder.ReferenceKind.ShouldBe("ProjectFolder");
        folder.FolderId.ShouldBe("replacement-folder");
        folder.ReferenceState.ShouldBe(ReferenceState.Included);
        folder.FileReferenceId.ShouldBeNull();
        fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key).ShouldBe(before);
        (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken)).ETag.ShouldBe(etag);
        fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!.UnavailableProjectIds.ShouldBeEmpty();
        (await fixture.Projection.PrepareRebuildAsync(request, "replacement-replay", TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    /// <summary>Clock-skewed history degrades only its own row at read time instead of blacking out the Tenant list.</summary>
    [Fact]
    public async Task SkewedEventTimestampDegradesOnlyItsRowAtReadTime()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        ProjectSetupUpdated skewed = new("tenant-a", "project-a", ProjectSetup.Empty,
            "actor-1", "setup", "task", "setup", "setup", SupportedProjectReadFixture.EventTime.AddSeconds(-5));
        (await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "project-a", [SupportedProjectReadFixture.Event(skewed, 3, 102)]),
            "skewed", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ProjectReadModel persisted = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        persisted.Sequence.ShouldBe(3);
        persisted.Freshness.ShouldBe(EvidenceFreshnessState.Current);
        fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!.UnavailableProjectIds.ShouldBeEmpty();
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        page.Items.Single().ProjectId.ShouldBe("project-a");
        page.Items.Single().Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
    }

    /// <summary>Administrator inventory selection cannot disclose an ordinarily bound Project.</summary>
    [Fact]
    public async Task AdministratorQuarantineOpenOfBoundProjectFailsSafely()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true, administrator: true);
        await fixture.SeedAsync("project-a");
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(InspectQuarantined: true), "project-a"),
            TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
        ProjectReadAuditRecord audit = fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, fixture.AuditStore.Keys.Single())!;
        audit.Outcome.ShouldBe("SafeDenial");
        audit.ProjectCount.ShouldBe(0);
        audit.FieldClass.ShouldBe("None");
        fixture.Folders.Reads.ShouldBe(0);
    }

    /// <summary>Archived folderless history remains ordinary inspection metadata rather than Active quarantine inventory.</summary>
    [Fact]
    public async Task ArchivedFolderlessProjectRemainsOrdinaryAndOutsideQuarantineInventory()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true, administrator: true);
        await fixture.SeedAsync("project-a", folder: false);
        ProjectArchived archived = new("tenant-a", "project-a", ProjectLifecycle.Archived,
            "actor-1", "archive", "task", "archive", "archive", SupportedProjectReadFixture.EventTime);
        (await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "project-a", [SupportedProjectReadFixture.Event(archived, 2, 101)]),
            "archive", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ProjectListPage ordinary = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        ordinary.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        ordinary.Items.Single().LifecycleState.ShouldBe(ProjectLifecycle.Archived);
        GetProjectResponse opened = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(
            fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken));
        opened.LifecycleState.ShouldBe(ProjectLifecycle.Archived);
        opened.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        ProjectListPage inventory = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(IncludeQuarantined: true)), TestContext.Current.CancellationToken));
        inventory.Items.ShouldBeEmpty();
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(InspectQuarantined: true), "project-a"),
            TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
    }
}
