namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadTests
{
    /// <summary>Tenant inspection can explain quarantined history without repairing or activating it.</summary>
    /// <param name="pendingReceipt">Whether historical Folder creation progress was recorded.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenantCanInspectFolderlessHistoryWithoutChangingProjectState(bool pendingReceipt)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await fixture.SeedAsync("legacy-project", folder: false);
        if (pendingReceipt)
        {
            ProjectFolderCreationPending pending = new("tenant-a", "legacy-project", "Historical folder intent",
                "folder_creation_pending", true, "actor-1", "historical", "task", "pending", "pending", SupportedProjectReadFixture.EventTime);
            await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "legacy-project",
                [SupportedProjectReadFixture.Event(pending, 2, 101)]), "historical-pending", TestContext.Current.CancellationToken);
        }

        string key = ProjectReadModelKeys.Detail("tenant-a", "legacy-project");
        ReadModelEntry<ProjectReadModel> before = await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken);
        byte[] beforeBytes = fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key)!;
        int count = fixture.Store.Count;
        GetProjectResponse safe = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(
            fixture.Query(new GetProjectQuery(), "legacy-project"), TestContext.Current.CancellationToken));
        safe.Name.ShouldBeNull();
        safe.Setup.ShouldBeNull();
        fixture.Store.Count.ShouldBe(count);

        GetProjectResponse inspected = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(
            fixture.Query(new GetProjectQuery(true), "legacy-project", "inspect-history"), TestContext.Current.CancellationToken));
        inspected.Name.ShouldBe("Project legacy-project");
        inspected.Description.ShouldBe("Safe description");
        inspected.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        inspected.Snapshot.Components.ShouldContain(component => component.Name == "Folder" && !component.Included);
        if (pendingReceipt)
        {
            inspected.References.Single().ReferenceState.ShouldBe(ReferenceState.Pending);
            inspected.References.Single().FolderId.ShouldBeNull();
            inspected.References.Single().DisplayName.ShouldBeNull();
            before.Value!.Detail!.ProjectFolder!.DisplayName.ShouldBe("Historical folder intent");
        }
        else
        {
            inspected.References.ShouldBeEmpty();
        }

        fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key).ShouldBe(beforeBytes);
        (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken)).ETag.ShouldBe(before.ETag);
        fixture.Store.Count.ShouldBe(count + 1);
        fixture.AuditStore.Keys.Count.ShouldBe(1);
        fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, fixture.AuditStore.Keys.Single())!.Outcome.ShouldBe("Inspection");
        fixture.Folders.Reads.ShouldBe(0);
    }

    /// <summary>Current owner denial and noncurrent evidence prevent Chatbot disclosure on list and open.</summary>
    /// <param name="outcome">The current owner-read result.</param>
    [Theory]
    [InlineData(ProjectFolderValidationOutcome.Denied)]
    [InlineData(ProjectFolderValidationOutcome.Archived)]
    [InlineData(ProjectFolderValidationOutcome.Stale)]
    [InlineData(ProjectFolderValidationOutcome.Unavailable)]
    public async Task ChatbotCannotOpenOrListAProjectWhoseFolderCannotCurrentlyBeRead(ProjectFolderValidationOutcome outcome)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        await fixture.SeedAsync("protected-project");
        fixture.Folders.Outcomes["folder-protected-project"] = outcome;
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Items.ShouldBeEmpty();
        page.Cursor.ShouldBeNull();
        page.HasMore.ShouldBeFalse();
        page.Snapshot.ResponseState.ShouldBe(outcome is ProjectFolderValidationOutcome.Stale or ProjectFolderValidationOutcome.Unavailable
            ? AdmissionResponseState.Unavailable : AdmissionResponseState.Complete);

        QueryResult denied = await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "protected-project"), TestContext.Current.CancellationToken);
        denied.Success.ShouldBeFalse();
        denied.ErrorMessage.ShouldBe("safe-denial");
        denied.PayloadBytes.ShouldBeNull();
        fixture.Folders.Reads.ShouldBeGreaterThanOrEqualTo(2);

        fixture.Folders.Outcomes["folder-protected-project"] = ProjectFolderValidationOutcome.Accepted;
        GetProjectResponse allowed = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(
            fixture.Query(new GetProjectQuery(), "protected-project", "owner-now-allows"), TestContext.Current.CancellationToken));
        allowed.Name.ShouldBe("Project protected-project");
        allowed.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(), correlation: "owner-now-allows-list"), TestContext.Current.CancellationToken))
            .Items.Single().ProjectId.ShouldBe("protected-project");
    }

    /// <summary>Unbound legacy and pending creation work never becomes visible to Chatbot.</summary>
    /// <param name="pendingReceipt">Whether the legacy stream contains a creation-progress event.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChatbotOmitsFolderlessAndPendingCreationHistory(bool pendingReceipt)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        await fixture.SeedAsync("hidden-project", folder: false);
        if (pendingReceipt)
        {
            ProjectFolderCreationPending pending = new("tenant-a", "hidden-project", "Hidden folder intent", "pending", true,
                "actor-1", "pending", "task", "pending", "pending", SupportedProjectReadFixture.EventTime);
            await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "hidden-project",
                [SupportedProjectReadFixture.Event(pending, 2, 101)]), "pending-history", TestContext.Current.CancellationToken);
        }
        await fixture.SeedAsync("visible-project", 200);
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(PageSize: 1)), TestContext.Current.CancellationToken));
        page.Items.Single().ProjectId.ShouldBe("visible-project");
        page.HasMore.ShouldBeFalse();
        page.Cursor.ShouldBeNull();
        QueryResult denied = await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "hidden-project"), TestContext.Current.CancellationToken);
        denied.Success.ShouldBeFalse();
        denied.ErrorMessage.ShouldBe("safe-denial");
        denied.PayloadBytes.ShouldBeNull();
        fixture.Folders.Arguments.ShouldAllBe(argument => argument.ProjectId == "visible-project");
    }

    /// <summary>Tenant list/open inspection preserves read-model bytes and changes only dedicated audit records.</summary>
    /// <param name="open">Whether to read a single Project instead of its list.</param>
    /// <param name="inspectionPermission">Whether descriptive inspection is independently authorized.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TenantNamesRequireInspectionAndPreservePersistedProjectState(bool open, bool inspectionPermission)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: inspectionPermission);
        await fixture.SeedAsync("project-a");
        string detailKey = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        string indexKey = ProjectReadModelKeys.Index("tenant-a");
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, detailKey, model with
        {
            Detail = model.Detail! with
            {
                Description = "Restricted description marker",
                SetupMetadata = "Restricted setup metadata marker",
                Setup = new ProjectSetup(["Restricted goal marker"], ["Restricted instruction marker"], [], [], null),
                ProjectFolder = model.Detail.ProjectFolder! with { DisplayName = "Restricted folder label marker" },
            },
        });
        string[] protectedKeys = [detailKey, indexKey];
        var before = new Dictionary<string, (byte[] Bytes, string? ETag)>(StringComparer.Ordinal);
        foreach (string key in protectedKeys)
        {
            ReadModelEntry<object> entry = await fixture.Store.GetAsync<object>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken);
            before[key] = (fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key)!, entry.ETag);
        }
        int count = fixture.Store.Count;
        QueryEnvelope query = fixture.Query(open ? new GetProjectQuery() : new ListProjectsQuery(), open ? "project-a" : "projects");
        QueryResult safe = open ? await fixture.Executor.GetAsync(query, TestContext.Current.CancellationToken)
            : await fixture.Executor.ListAsync(query, TestContext.Current.CancellationToken);
        safe.Success.ShouldBeTrue();
        Encoding.UTF8.GetString(safe.PayloadBytes!).ShouldNotContain("Project project-a");
        Encoding.UTF8.GetString(safe.PayloadBytes!).ShouldNotContain("Restricted");
        using (JsonDocument payload = JsonDocument.Parse(safe.PayloadBytes!))
        {
            JsonElement row = open ? payload.RootElement : payload.RootElement.GetProperty("items")[0];
            foreach (string field in new[] { "name", "description", "setup", "setupMetadata" })
                row.TryGetProperty(field, out _).ShouldBeFalse();
        }
        fixture.AuditStore.Keys.ShouldBeEmpty();
        fixture.Store.Count.ShouldBe(count);

        QueryEnvelope inspected = query with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes<object>(open ? new GetProjectQuery(true) : new ListProjectsQuery(InspectDescriptiveMetadata: true), SupportedProjectReadFixture.Json),
            CorrelationId = "inspect-name",
        };
        QueryResult result = open ? await fixture.Executor.GetAsync(inspected, TestContext.Current.CancellationToken)
            : await fixture.Executor.ListAsync(inspected, TestContext.Current.CancellationToken);
        result.Success.ShouldBe(inspectionPermission);
        if (inspectionPermission)
        {
            using JsonDocument payload = JsonDocument.Parse(result.PayloadBytes!);
            JsonElement row = open ? payload.RootElement : payload.RootElement.GetProperty("items")[0];
            row.GetProperty("name").GetString().ShouldBe("Project project-a");
            if (open)
            {
                row.GetProperty("description").GetString().ShouldBe("Restricted description marker");
                row.GetProperty("setupMetadata").GetString().ShouldBe("Restricted setup metadata marker");
                row.GetProperty("setup").GetProperty("goals")[0].GetString().ShouldBe("Restricted goal marker");
                row.GetProperty("setup").GetProperty("userInstructions")[0].GetString().ShouldBe("Restricted instruction marker");
            }
        }
        else
        {
            result.ErrorMessage.ShouldBe("safe-denial");
            result.PayloadBytes.ShouldBeNull();
        }
        foreach (string key in protectedKeys)
        {
            fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key).ShouldBe(before[key].Bytes);
            (await fixture.Store.GetAsync<object>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken)).ETag.ShouldBe(before[key].ETag);
        }
        fixture.AuditStore.Keys.Count.ShouldBe(1);
        fixture.Store.Count.ShouldBe(count + fixture.AuditStore.Keys.Count);
        string auditKey = fixture.AuditStore.Keys.Single();
        auditKey.ShouldStartWith("projects:read-audit:v1:");
        ProjectReadAuditRecord record = fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, auditKey)!;
        record.Outcome.ShouldBe(inspectionPermission ? "Inspection" : "SafeDenial");
        JsonSerializer.Serialize(record).ShouldNotContain("Project project-a");
        QueryResult retry = open ? await fixture.Executor.GetAsync(inspected, TestContext.Current.CancellationToken)
            : await fixture.Executor.ListAsync(inspected, TestContext.Current.CancellationToken);
        retry.Success.ShouldBe(inspectionPermission);
        fixture.Store.Count.ShouldBe(count + 1);
        foreach (string key in protectedKeys)
        {
            fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, key).ShouldBe(before[key].Bytes);
            (await fixture.Store.GetAsync<object>(ProjectReadModelKeys.StoreName, key, TestContext.Current.CancellationToken)).ETag.ShouldBe(before[key].ETag);
        }
    }
}
