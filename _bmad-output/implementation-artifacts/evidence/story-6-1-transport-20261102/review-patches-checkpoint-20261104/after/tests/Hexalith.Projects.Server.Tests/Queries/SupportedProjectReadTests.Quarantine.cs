namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadTests
{
    [Fact]
    public async Task OrdinaryTenantReadsExcludeQuarantinedActiveProjects()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await fixture.SeedAsync("legacy-a", folder: false);
        await fixture.SeedAsync("visible-b", 200);

        ProjectListPage ordinary = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(InspectDescriptiveMetadata: true)), TestContext.Current.CancellationToken));
        ordinary.Items.Select(row => row.ProjectId).ShouldBe(["visible-b"]);
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(true), "legacy-a"),
            TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(IncludeQuarantined: true)),
            TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task AdministratorInventoryAuditsEveryReturnedProjectAndBindsCursorToInventory()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true, administrator: true);
        await fixture.SeedAsync("legacy-a", folder: false);
        await fixture.SeedAsync("legacy-b", 200, folder: false);
        await fixture.SeedAsync("visible-c", 300);
        int before = fixture.Store.Count;

        ProjectListPage first = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(PageSize: 1, IncludeQuarantined: true)), TestContext.Current.CancellationToken));
        first.Items.Single().ProjectId.ShouldBe("legacy-a");
        first.Items.Single().Name.ShouldBe("Project legacy-a");
        first.Items.Single().ProjectFolderAvailability.ShouldBe(ReferenceState.Pending);
        first.HasMore.ShouldBeTrue();
        first.Cursor.ShouldNotBeNull();
        first.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        first.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.None]);
        first.Snapshot.Components.Single().Name.ShouldBe("Enumeration");
        first.Items.Single().Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        first.Items.Single().Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.Retry);
        first.Items.Single().Snapshot.Components.ShouldContain(component => component.Name == "Folder" && !component.Included);
        fixture.AuditStore.Keys.Count.ShouldBe(1);

        ProjectListPage second = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1, IncludeQuarantined: true), correlation: "inventory-page-2"),
            TestContext.Current.CancellationToken));
        second.Items.Single().ProjectId.ShouldBe("legacy-b");
        second.HasMore.ShouldBeFalse();
        second.Cursor.ShouldBeNull();
        second.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        second.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.None]);
        second.Snapshot.Components.Single().Name.ShouldBe("Enumeration");
        second.Items.Single().Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        second.Items.Single().Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.Retry);
        fixture.AuditStore.Keys.Count.ShouldBe(2);
        fixture.Store.Count.ShouldBe(before + 2);
        fixture.AuditStore.Keys.Select(key => fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, key)!)
            .ShouldAllBe(record => record.Outcome == "Inspection" && record.ProjectCount == 1 && record.FieldClass == "ProjectName");
        JsonSerializer.Serialize(fixture.AuditStore.Keys.Select(key => fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, key)!))
            .ShouldNotContain("legacy-a");

        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1)),
            TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        fixture.Store.Count.ShouldBe(before + 2);
        string[] priorAudits = fixture.AuditStore.Keys.ToArray();
        ProjectListPage multi = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(PageSize: 2, IncludeQuarantined: true), correlation: "inventory-multirow"),
            TestContext.Current.CancellationToken));
        multi.Items.Select(row => row.ProjectId).ShouldBe(["legacy-a", "legacy-b"]);
        string[] added = fixture.AuditStore.Keys.Except(priorAudits, StringComparer.Ordinal).ToArray();
        added.Length.ShouldBe(2);
        added.Distinct(StringComparer.Ordinal).Count().ShouldBe(2);
        added.Select(key => fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, key)!)
            .ShouldAllBe(record => record.Outcome == "Inspection" && record.ProjectCount == 1 && record.FieldClass == "ProjectName");
        fixture.Store.Count.ShouldBe(before + 4);
    }

    [Fact]
    public async Task PendingHistoricalFolderIdIsNotAnAcceptedBindingForOrdinaryReads()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true, administrator: true);
        await fixture.SeedAsync("legacy-pending", folder: false);
        string key = ProjectReadModelKeys.Detail("tenant-a", "legacy-pending");
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, key, model with
        {
            Detail = model.Detail! with
            {
                SetupMetadata = "Restricted historical setup",
                ProjectFolder = new ProjectFolderReference("historical-folder", "Historical label", ReferenceState.Pending, "pending", SupportedProjectReadFixture.EventTime),
            },
        });
        byte[] before = fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key)!;
        string? etag = (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken)).ETag;
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()),
            TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "legacy-pending"), TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        GetProjectResponse inspected = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(
            fixture.Query(new GetProjectQuery(InspectQuarantined: true), "legacy-pending", "pending-inspect"), TestContext.Current.CancellationToken));
        inspected.Name.ShouldBe("Project legacy-pending");
        inspected.Description.ShouldBeNull(); inspected.Setup.ShouldBeNull(); inspected.SetupMetadata.ShouldBeNull();
        inspected.References.Single().FolderId.ShouldBe("historical-folder");
        inspected.References.Single().ReferenceState.ShouldBe(ReferenceState.Pending);
        inspected.References.Single().DisplayName.ShouldBeNull();
        JsonSerializer.Serialize(inspected).ShouldNotContain("Restricted historical setup");
        fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key).ShouldBe(before);
        (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken)).ETag.ShouldBe(etag);
        fixture.Folders.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task QuarantineDisclosureRequiresAdministratorPermissionAndDurableAudit()
    {
        SupportedProjectReadFixture withoutPermission = await SupportedProjectReadFixture.CreateAsync(administrator: true);
        await withoutPermission.SeedAsync("legacy-a", folder: false);
        (await withoutPermission.Executor.GetAsync(withoutPermission.Query(new GetProjectQuery(InspectQuarantined: true), "legacy-a"),
            TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");

        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true, administrator: true);
        await fixture.SeedAsync("legacy-a", folder: false);
        fixture.AuditStore.FailWrites = true;
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(InspectQuarantined: true), "legacy-a"),
            TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(IncludeQuarantined: true)),
            TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
        fixture.AuditStore.Keys.ShouldBeEmpty();
    }
}
