namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.Projects.Contracts.Queries;
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
        first.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        fixture.AuditStore.Keys.Count.ShouldBe(1);

        ProjectListPage second = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1, IncludeQuarantined: true), correlation: "inventory-page-2"),
            TestContext.Current.CancellationToken));
        second.Items.Single().ProjectId.ShouldBe("legacy-b");
        second.HasMore.ShouldBeFalse();
        fixture.AuditStore.Keys.Count.ShouldBe(2);
        fixture.Store.Count.ShouldBe(before + 2);
        fixture.AuditStore.Keys.Select(key => fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, key)!)
            .ShouldAllBe(record => record.Outcome == "Inspection" && record.ProjectCount == 1 && record.FieldClass == "ProjectName");
        JsonSerializer.Serialize(fixture.AuditStore.Keys.Select(key => fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, key)!))
            .ShouldNotContain("legacy-a");

        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1)),
            TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        fixture.Store.Count.ShouldBe(before + 2);
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
