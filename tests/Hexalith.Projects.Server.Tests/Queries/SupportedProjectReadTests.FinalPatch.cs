namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OpenFailsClosedWhenModelOrTenantAuthorityChangesDuringOwnerRead(bool revoke)
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await fixture.SeedAsync("project-a");
        fixture.Folders.OnRead = async _ =>
        {
            if (revoke)
            {
                var tenant = (await fixture.Tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken).ConfigureAwait(false))!;
                tenant.Principals.Remove("actor-1"); tenant.ProjectionWatermark = "tenant-a:2"; tenant.Watermark++;
                await fixture.Tenants.SaveAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
                var model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
                await fixture.Store.SaveAsync(ProjectReadModelKeys.StoreName, key, model with { Sequence = 3 }, TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
        };
        QueryResult result = await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(true), "project-a"), TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe("safe-denial"); result.PayloadBytes.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidModelObservationIsUnavailableInListAndOpen(bool undefined)
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(); await fixture.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        await fixture.Store.SaveAsync(ProjectReadModelKeys.StoreName, key, undefined ? model with { Freshness = (EvidenceFreshnessState)999 } : model with { LastVerifiedAt = default }, TestContext.Current.CancellationToken);
        var list = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        list.Items.ShouldBeEmpty(); list.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        var open = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken));
        open.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        open.Snapshot.Components.ShouldAllBe(component => Enum.IsDefined(component.Freshness));
        open.Snapshot.Components.Single(component => component.Name == "Project").LastVerifiedAt.ShouldBeNull();
    }

    [Fact]
    public async Task IndexWithoutObservationCannotReportCompleteEnumeration()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(); await fixture.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Index("tenant-a");
        var index = fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, key)!;
        await fixture.Store.SaveAsync(ProjectReadModelKeys.StoreName, key, index with { LastVerifiedAt = default }, TestContext.Current.CancellationToken);
        var page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Items.ShouldBeEmpty(); page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        page.Snapshot.Components.Single().LastVerifiedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InspectionRetryIdentityIncludesObservedRevisionForListAndOpen(bool open)
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true); await fixture.SeedAsync("project-a");
        QueryEnvelope query = open ? fixture.Query(new GetProjectQuery(true), "project-a") : fixture.Query(new ListProjectsQuery(InspectDescriptiveMetadata: true));
        Task<QueryResult> ReadAsync() => open ? fixture.Executor.GetAsync(query, TestContext.Current.CancellationToken) : fixture.Executor.ListAsync(query, TestContext.Current.CancellationToken);
        (await ReadAsync()).Success.ShouldBeTrue(); (await ReadAsync()).Success.ShouldBeTrue();
        fixture.AuditStore.Keys.Count.ShouldBe(1);
        // Same immutable request and count, changed inspected contents in a new projection observation.
        ProjectionRequest replay = SupportedProjectReadFixture.Request("project-a", 300);
        var created = JsonSerializer.Deserialize<ProjectCreated>(replay.Events[0].Payload, SupportedProjectReadFixture.Json)!;
        replay.Events[0] = SupportedProjectReadFixture.Event(created with { Name = "Changed inspected name" }, 1, 300);
        DomainProjectionRebuildPlan plan = await fixture.Projection.PrepareRebuildAsync(replay, "new-observation", TestContext.Current.CancellationToken);
        (await fixture.Store.ExecuteAsync(new(new(ProjectReadModelKeys.StoreName, "tenant-a", "projects", "project-a", "rebuild", "new-observation"), plan.Operations), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        QueryResult changed = await ReadAsync(); changed.Success.ShouldBeTrue();
        System.Text.Encoding.UTF8.GetString(changed.PayloadBytes!).ShouldContain("Changed inspected name");
        fixture.AuditStore.Keys.Count.ShouldBe(2);
        (await ReadAsync()).Success.ShouldBeTrue(); fixture.AuditStore.Keys.Count.ShouldBe(2);
        if (!open)
        {
            await fixture.SeedAsync("project-b", 400);
            var page = SupportedProjectReadFixture.Payload<ProjectListPage>(await ReadAsync());
            page.Items.Count.ShouldBe(2); fixture.AuditStore.Keys.Count.ShouldBe(3);
            (await ReadAsync()).Success.ShouldBeTrue(); fixture.AuditStore.Keys.Count.ShouldBe(3);
        }
        var records = fixture.AuditStore.Keys.Select(key => fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, key)!).ToArray();
        records.ShouldAllBe(record => record.Outcome == "Inspection");
        records.Count(record => record.ProjectCount == 1).ShouldBe(2);
        if (!open) records.Count(record => record.ProjectCount == 2).ShouldBe(1);
        JsonSerializer.Serialize(records).ShouldNotContain("Changed inspected name");
    }
}
