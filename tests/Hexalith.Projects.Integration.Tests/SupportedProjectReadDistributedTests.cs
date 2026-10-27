#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Net;
using System.Text.Json;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Shouldly;
using Xunit;

/// <summary>Qualifies the actual isolated gateway/domain/owner chain without discovery or head substitutions.</summary>
public sealed class SupportedProjectReadDistributedTests
{
    /// <summary>Executes every approved matrix row and independently checks persisted domain and audit state.</summary>
    [Fact]
    public async Task SupportedReadsTraverseCurrentDistributedOwnersWithoutDomainWritesAsync()
    {
        using SupportedProjectReadDistributedFixture fixture = new();
        await fixture.SeedAsync();
        List<string> completed = [];
        async Task CompleteAsync(string row)
        {
            completed.Add(row);
            await File.AppendAllTextAsync(Path.Combine(fixture.Evidence, "read-progress.log"), row + Environment.NewLine, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        var before = await fixture.SnapshotAsync();
        DateTimeOffset cursorRequestedAt = DateTimeOffset.UtcNow;
        ProjectListPage first = await fixture.ReadAsync<ProjectListPage>(list: new(PageSize: 1));
        Assert.True(first.Items.Select(item => item.ProjectId).SequenceEqual(["project-a"]), "Authorized first page differs from its owned fixture; count=" + first.Items.Count + ", state=" + first.Snapshot.ResponseState);
        first.Cursor.ShouldNotBeNullOrWhiteSpace();
        ProjectListPage continuation = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: first.Cursor, PageSize: 1));
        Assert.True(continuation.Items.Select(item => item.ProjectId).SequenceEqual(["project-b"]), "Authorized continuation differs from its owned fixture.");
        GetProjectResponse opened = await fixture.ReadAsync<GetProjectResponse>(open: true);
        Assert.True(opened.Name is not null, "Authorized Chatbot metadata was not disclosed by the real owner chain.");
        opened.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        await fixture.AssertUnchangedAsync(before);
        await CompleteAsync("authorized-list-continuation-open");
        await fixture.AssertUserTrustCannotAuthenticateWorkloadAsync();
        await fixture.AssertUnchangedAsync(before);
        await CompleteAsync("public-user-and-workload-trust-remain-separate");

        string absent = await fixture.SafeDenialAsync(project: "absent");
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false, list: new(Cursor: first.Cursor, PageSize: 1))).ShouldBe(absent);
        await fixture.AssertUnchangedAsync(before);
        await CompleteAsync("populated-foreign-tenant-and-absent-safe-denial");

        await fixture.RestartProjectsAsync();
        ProjectListPage resumed = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: first.Cursor, PageSize: 1));
        Assert.True(resumed.Items.Select(item => item.ProjectId).SequenceEqual(["project-b"]) && !resumed.HasMore && resumed.Cursor is null,
            "The pre-restart cursor did not resume at the next expected row.");
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(continuation, resumed), "Pre-restart continuation metadata changed after restart.");
        ProjectListPage freshFirst = await fixture.ReadAsync<ProjectListPage>(list: new(PageSize: 1));
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(first, freshFirst), "First-page metadata changed after restart.");
        freshFirst.Cursor.ShouldNotBeNullOrWhiteSpace();
        ProjectListPage freshNext = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: freshFirst.Cursor, PageSize: 1));
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(resumed, freshNext), "Fresh paging differs from the retained continuation.");
        Assert.True(freshFirst.Items.Concat(freshNext.Items).Select(item => item.ProjectId).SequenceEqual(["project-a", "project-b"]),
            "Recovered paging duplicated or omitted a fixture row.");
        GetProjectResponse reopened = await fixture.ReadAsync<GetProjectResponse>(open: true);
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(opened, reopened), "Open metadata, version or freshness changed after restart.");
        (await fixture.SafeDenialAsync(project: "absent")).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false, list: new(Cursor: first.Cursor, PageSize: 1))).ShouldBe(absent);
        await fixture.VerifyRestartStateAsync(before);
        await CompleteAsync("owned-projects-process-restart-retains-reads-continuation-and-state");

        GetProjectResponse safeOperator = await fixture.ReadAsync<GetProjectResponse>(open: true, chatbot: false);
        Assert.True(safeOperator.Name is null && safeOperator.Description is null, "Uninspected operator metadata was disclosed.");
        await fixture.SafeDenialAsync(chatbot: false, inspection: true, inspectionPermission: false);
        await fixture.SafeDenialAsync(open: false, chatbot: false, inspection: true, inspectionPermission: false);
        string[] auditBeforeOpen = await fixture.AuditKeysAsync();
        GetProjectResponse inspected = await fixture.ReadAsync<GetProjectResponse>(open: true, chatbot: false, inspection: true);
        Assert.True(inspected.Name is not null, "Authorized inspection control failed.");
        await fixture.VerifyDurableAuditDeltaAsync(auditBeforeOpen, open: true, count: 1);
        string[] auditBeforeList = await fixture.AuditKeysAsync();
        ProjectListPage inspectedList = await fixture.ReadAsync<ProjectListPage>(chatbot: false, inspection: true);
        inspectedList.Items.Count.ShouldBe(2);
        await fixture.VerifyDurableAuditDeltaAsync(auditBeforeList, open: false, count: 2);
        await fixture.AssertUnchangedAsync(before);
        await CompleteAsync("inspection-permission-and-durable-metadata-only-audit");

        await fixture.DenyNewAuditKeysAsync();
        try
        {
            await fixture.VerifyAuditWriteFaultAsync();
            await fixture.VerifyInspectedAuditRefusalAsync(open: true);
            await fixture.VerifyInspectedAuditRefusalAsync(open: false);
        }
        finally { await fixture.RestoreAuditStorageAsync(); }
        await fixture.WaitForAuthorizedRecoveryAsync("audit-storage");
        await fixture.AssertUnchangedAsync(before);
        await CompleteAsync("audit-storage-unavailable-blocks-disclosure");

        long priorHead = await fixture.HeadAsync(fixture.Tenant, "folder-project-a");
        ProjectListPage immediatelyBeforeRevocation = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: first.Cursor, PageSize: 1));
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(resumed, immediatelyBeforeRevocation),
            "The retained pre-restart cursor was not valid immediately before revocation.");
        Assert.True(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(cursorRequestedAt, DateTimeOffset.UtcNow),
            "The retained cursor has reached its lifetime bound before revocation.");
        await fixture.ChangeFolderAccessAsync(fixture.Tenant, "folder-project-a", revoke: true);
        (await fixture.HeadAsync(fixture.Tenant, "folder-project-a")).ShouldBeGreaterThan(priorHead);
        before = await fixture.SnapshotAsync();
        (await fixture.SafeDenialAsync()).ShouldBe(absent);
        ProjectListPage revoked = await fixture.ReadAsync<ProjectListPage>();
        Assert.True(revoked.Items.Select(item => item.ProjectId).SequenceEqual(["project-b"]), "Revoked Folder authority retained visibility.");
        using (var oldCursor = await fixture.QueryAsync(list: new(Cursor: first.Cursor, PageSize: 1)))
        {
            Assert.True(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(cursorRequestedAt, DateTimeOffset.UtcNow),
                "Cursor expiry cannot qualify as authority invalidation.");
            oldCursor.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        await fixture.AssertUnchangedAsync(before);
        await CompleteAsync("revocation-observes-real-head-and-invalidates-old-continuation");

        await fixture.ChangeFolderAccessAsync(fixture.Tenant, "folder-project-a", revoke: false);
        await fixture.ReadAsync<GetProjectResponse>(open: true);
        before = await fixture.SnapshotAsync();
        await fixture.ResourceAsync("stop");
        try
        {
            ProjectListPage unavailable = await fixture.ReadAsync<ProjectListPage>();
            unavailable.Items.ShouldBeEmpty();
            unavailable.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
            await fixture.SafeDenialAsync();
        }
        finally { await fixture.ResourceAsync("start"); }
        await fixture.WaitForAuthorizedRecoveryAsync("folder-service");
        await fixture.AssertUnchangedAsync(before);
        await fixture.ReadAsync<GetProjectResponse>(open: true);
        await CompleteAsync("owned-folder-outage-honest-unavailability-and-recovery");
        await CompleteAsync("authenticated-command-and-projection-compatibility");
        await fixture.VerifyDuplicateDeliveryAsync();
        await CompleteAsync("duplicate-project-delivery-retains-supported-reads-and-state");
        await fixture.VerifyFullReplayAsync();
        await CompleteAsync("full-project-replay-retains-truth-and-invalidates-old-continuation");
        await File.WriteAllTextAsync(Path.Combine(fixture.Evidence, "matrix.json"), JsonSerializer.Serialize(new
        {
            allCasesExecuted = completed.Count == 11, cases = completed, readOnlyDomainBytesAndEtagsUnchanged = true,
            projectsProcessRestartVerified = true, duplicateDeliveryVerified = true, fullReplayVerified = true, preRestartContinuationAcceptedAfterRestart = true, freshPagingAgrees = true,
            retainedCursorAcceptedImmediatelyBeforeRevocation = true, cursorRejectionWithinLifetime = true,
            postRestartForeignAndAbsentDenialsVerified = true, bothTenantDomainWatermarksUnchanged = true,
            actualDaprDiscovery = true, actualOwnerActorHeads = true, acceptedEquivalence = false, productionReadiness = false,
        }, new JsonSerializerOptions { WriteIndented = true }), TestContext.Current.CancellationToken);
    }
}
#endif
