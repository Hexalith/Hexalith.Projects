namespace Hexalith.Projects.Integration.Tests;

using System.Net;
using System.Security.Cryptography;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Shouldly;
using Xunit;
using global::Aspire.Hosting;
using Hexalith.Projects.PackageFixtures;

/// <summary>Qualifies the actual isolated gateway/domain/owner chain without discovery or head substitutions.</summary>
internal static class PackageReadScenarios
{
    public static async Task InitializeAsync(object inputContext, object scratchContext, object factoryContext,
        DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        using PackageReadFixture fixture = new((FixtureInputs)inputContext, (FixtureScratch)scratchContext,
            (PackageApplicationFactory)factoryContext, application, scenario, cancellationToken);
        await fixture.InitializeWriterProtocolAsync().ConfigureAwait(false);
    }

    public static async Task SeedAsync(object inputContext, object scratchContext, object factoryContext,
        DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        using PackageReadFixture fixture = new((FixtureInputs)inputContext, (FixtureScratch)scratchContext,
            (PackageApplicationFactory)factoryContext, application, scenario, cancellationToken);
        await fixture.SeedAsync().ConfigureAwait(false);
    }

    public static async Task RunAsync(object inputContext, object scratchContext, object factoryContext,
        DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        FixtureInputs inputs = (FixtureInputs)inputContext;
        FixtureScratch scratch = (FixtureScratch)scratchContext;
        PackageApplicationFactory factory = (PackageApplicationFactory)factoryContext;
        switch (scenario)
        {
            case "main": await MainReadsUseActualPersistedOwnersAsync(inputs, scratch, factory, application, scenario, cancellationToken).ConfigureAwait(false); break;
            case "duplicate": await DuplicateDeliveryUsesActualPersistedOwnersAsync(inputs, scratch, factory, application, scenario, cancellationToken).ConfigureAwait(false); break;
            case "full-replay": await FullReplayUsesActualPersistedOwnersAsync(inputs, scratch, factory, application, scenario, cancellationToken).ConfigureAwait(false); break;
            case "tenant-access-recovery": await TenantAccessRecoveryUsesActualPersistedOwnersAsync(inputs, scratch, factory, application, scenario, cancellationToken).ConfigureAwait(false); break;
            default: throw new InvalidOperationException("Unknown required package native scenario.");
        }
        Console.WriteLine("[package-native] completed scenario=" + scenario);
    }

    /// <summary>Proves authenticated recovery and its persisted end-state using the same real OIDC token.</summary>
    private static async Task TenantAccessRecoveryUsesActualPersistedOwnersAsync(FixtureInputs inputs, FixtureScratch scratch, PackageApplicationFactory factory, DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        using PackageReadFixture fixture = new(inputs, scratch, factory, application, scenario, cancellationToken);
        await fixture.SeedAsync().ConfigureAwait(false);
        await fixture.VerifyTenantAccessRecoveryAsync().ConfigureAwait(false);
        Console.WriteLine("[package-native] case=tenant-access-recovery passed=1 failed=0");
    }

    /// <summary>Proves duplicate delivery against one freshly seeded owned runtime.</summary>
    private static async Task DuplicateDeliveryUsesActualPersistedOwnersAsync(FixtureInputs inputs, FixtureScratch scratch, PackageApplicationFactory factory, DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        using PackageReadFixture fixture = new(inputs, scratch, factory, application, scenario, cancellationToken);
        fixture.RequireScenario("duplicate");
        await fixture.SeedAsync().ConfigureAwait(false);
        await fixture.VerifyDuplicateDeliveryAsync().ConfigureAwait(false);
        string[] completed = ["duplicate-project-delivery-retains-supported-reads-and-state"];
        await File.AppendAllTextAsync(Path.Combine(fixture.Evidence, "read-progress.log"), completed[0] + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        await WriteScenarioResultAsync(fixture, "duplicate", completed, cancellationToken).ConfigureAwait(false);
        Console.WriteLine("[package-native] case=" + completed[0] + " passed=1 failed=0");
    }

    /// <summary>Proves full replay against its own fresh source and persisted baseline.</summary>
    private static async Task FullReplayUsesActualPersistedOwnersAsync(FixtureInputs inputs, FixtureScratch scratch, PackageApplicationFactory factory, DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        using PackageReadFixture fixture = new(inputs, scratch, factory, application, scenario, cancellationToken);
        fixture.RequireScenario("full-replay");
        await fixture.SeedAsync().ConfigureAwait(false);
        await fixture.VerifyFullReplayAsync().ConfigureAwait(false);
        string[] completed = ["full-project-replay-retains-truth-and-invalidates-old-continuation"];
        await File.AppendAllTextAsync(Path.Combine(fixture.Evidence, "read-progress.log"), completed[0] + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        await WriteScenarioResultAsync(fixture, "full-replay", completed, cancellationToken).ConfigureAwait(false);
        Console.WriteLine("[package-native] case=" + completed[0] + " passed=1 failed=0");
    }

    /// <summary>Keeps all nine read, restart, audit, revocation and outage rows in one owned runtime.</summary>
    private static async Task MainReadsUseActualPersistedOwnersAsync(FixtureInputs inputs, FixtureScratch scratch, PackageApplicationFactory factory, DistributedApplication application, string scenario, CancellationToken cancellationToken)
    {
        using PackageReadFixture fixture = new(inputs, scratch, factory, application, scenario, cancellationToken);
        fixture.RequireScenario("main");
        await fixture.SeedAsync().ConfigureAwait(false);
        List<string> completed = [];
        async Task CompleteAsync(string row)
        {
            completed.Add(row);
            await File.AppendAllTextAsync(Path.Combine(fixture.Evidence, "read-progress.log"), row + Environment.NewLine, cancellationToken).ConfigureAwait(false);
            Console.WriteLine("[package-native] case=" + row + " passed=1 failed=0");
        }
        var before = await fixture.SnapshotAsync().ConfigureAwait(false);
        DateTimeOffset cursorRequestedAt = DateTimeOffset.UtcNow;
        ProjectListPage first = await fixture.ReadAsync<ProjectListPage>(list: new(PageSize: 1)).ConfigureAwait(false);
        Assert.True(first.Items.Select(item => item.ProjectId).SequenceEqual(["project-a"]), "Authorized first page differs from its owned fixture; count=" + first.Items.Count + ", state=" + first.Snapshot.ResponseState);
        first.Cursor.ShouldNotBeNullOrWhiteSpace();
        ProjectListPage continuation = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: first.Cursor, PageSize: 1)).ConfigureAwait(false);
        Assert.True(continuation.Items.Select(item => item.ProjectId).SequenceEqual(["project-b"]), "Authorized continuation differs from its owned fixture.");
        GetProjectResponse opened = await fixture.ReadAsync<GetProjectResponse>(open: true).ConfigureAwait(false);
        Assert.True(opened.Name is not null, "Authorized Chatbot metadata was not disclosed by the real owner chain.");
        opened.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(false);
        await CompleteAsync("authorized-list-continuation-open").ConfigureAwait(false);
        await fixture.AssertUserTrustCannotAuthenticateWorkloadAsync().ConfigureAwait(false);
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(false);
        await CompleteAsync("public-user-and-workload-trust-remain-separate").ConfigureAwait(false);

        string absent = await fixture.SafeDenialAsync(project: "absent").ConfigureAwait(false);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant).ConfigureAwait(false)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false).ConfigureAwait(false)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false, list: new(Cursor: first.Cursor, PageSize: 1)).ConfigureAwait(false)).ShouldBe(absent);
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(false);
        await CompleteAsync("populated-foreign-tenant-and-absent-safe-denial").ConfigureAwait(false);

        await fixture.RestartProjectsAsync().ConfigureAwait(false);
        ProjectListPage resumed = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: first.Cursor, PageSize: 1)).ConfigureAwait(false);
        Assert.True(resumed.Items.Select(item => item.ProjectId).SequenceEqual(["project-b"]) && !resumed.HasMore && resumed.Cursor is null,
            "The pre-restart cursor did not resume at the next expected row.");
        Assert.True(PackageReadFixture.SameStableReadMetadata(continuation, resumed), "Pre-restart continuation metadata changed after restart.");
        ProjectListPage freshFirst = await fixture.ReadAsync<ProjectListPage>(list: new(PageSize: 1)).ConfigureAwait(false);
        Assert.True(PackageReadFixture.SameStableReadMetadata(first, freshFirst), "First-page metadata changed after restart.");
        freshFirst.Cursor.ShouldNotBeNullOrWhiteSpace();
        ProjectListPage freshNext = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: freshFirst.Cursor, PageSize: 1)).ConfigureAwait(false);
        Assert.True(PackageReadFixture.SameStableReadMetadata(resumed, freshNext), "Fresh paging differs from the retained continuation.");
        Assert.True(freshFirst.Items.Concat(freshNext.Items).Select(item => item.ProjectId).SequenceEqual(["project-a", "project-b"]),
            "Recovered paging duplicated or omitted a fixture row.");
        GetProjectResponse reopened = await fixture.ReadAsync<GetProjectResponse>(open: true).ConfigureAwait(false);
        Assert.True(PackageReadFixture.SameStableReadMetadata(opened, reopened), "Open metadata, version or freshness changed after restart.");
        (await fixture.SafeDenialAsync(project: "absent").ConfigureAwait(false)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant).ConfigureAwait(false)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false).ConfigureAwait(false)).ShouldBe(absent);
        (await fixture.SafeDenialAsync(tenant: fixture.ForeignTenant, open: false, list: new(Cursor: first.Cursor, PageSize: 1)).ConfigureAwait(false)).ShouldBe(absent);
        await fixture.VerifyRestartStateAsync(before).ConfigureAwait(false);
        await CompleteAsync("owned-projects-process-restart-retains-reads-continuation-and-state").ConfigureAwait(false);

        GetProjectResponse safeOperator = await fixture.ReadAsync<GetProjectResponse>(open: true, chatbot: false).ConfigureAwait(false);
        Assert.True(safeOperator.Name is null && safeOperator.Description is null, "Uninspected operator metadata was disclosed.");
        await fixture.SafeDenialAsync(chatbot: false, inspection: true, inspectionPermission: false).ConfigureAwait(false);
        await fixture.SafeDenialAsync(open: false, chatbot: false, inspection: true, inspectionPermission: false).ConfigureAwait(false);
        string[] auditBeforeOpen = await fixture.AuditKeysAsync().ConfigureAwait(false);
        GetProjectResponse inspected = await fixture.ReadAsync<GetProjectResponse>(open: true, chatbot: false, inspection: true).ConfigureAwait(false);
        Assert.True(inspected.Name is not null, "Authorized inspection control failed.");
        await fixture.VerifyDurableAuditDeltaAsync(auditBeforeOpen, open: true, count: 1).ConfigureAwait(false);
        string[] auditBeforeList = await fixture.AuditKeysAsync().ConfigureAwait(false);
        ProjectListPage inspectedList = await fixture.ReadAsync<ProjectListPage>(chatbot: false, inspection: true).ConfigureAwait(false);
        inspectedList.Items.Count.ShouldBe(2);
        await fixture.VerifyDurableAuditDeltaAsync(auditBeforeList, open: false, count: 2).ConfigureAwait(false);
        ProjectListPage ordinaryOperator = await fixture.ReadAsync<ProjectListPage>(chatbot: false).ConfigureAwait(false);
        ordinaryOperator.Items.Select(item => item.ProjectId).ShouldBe(["project-a", "project-b"]);
        foreach (string legacy in new[] { "legacy-a", "legacy-b" })
        {
            (await fixture.SafeDenialAsync(project: legacy, chatbot: false).ConfigureAwait(false)).ShouldBe(absent);
            (await fixture.SafeDenialAsync(project: legacy).ConfigureAwait(false)).ShouldBe(absent);
            await fixture.SafeDenialAsync(project: legacy, chatbot: false, inspection: true, quarantine: true).ConfigureAwait(false);
            await fixture.SafeDenialAsync(project: legacy, chatbot: false, quarantine: true, projectAdministrator: true, inspectionPermission: false).ConfigureAwait(false);
        }
        await fixture.SafeDenialAsync(open: false, chatbot: false, inspection: true, quarantine: true).ConfigureAwait(false);
        await fixture.SafeDenialAsync(open: false, chatbot: false, quarantine: true, projectAdministrator: true, inspectionPermission: false).ConfigureAwait(false);
        string[] auditBeforeInventory = await fixture.AuditKeysAsync().ConfigureAwait(false);
        ProjectListPage inventory = await fixture.ReadAsync<ProjectListPage>(chatbot: false, projectAdministrator: true, quarantine: true).ConfigureAwait(false);
        inventory.Items.Select(item => item.ProjectId).ShouldBe(["legacy-a", "legacy-b"]);
        inventory.Items.ShouldAllBe(item => item.Name != null && item.ProjectFolderAvailability == ReferenceState.Pending
            && item.Snapshot.ResponseState == AdmissionResponseState.Unavailable);
        inventory.HasMore.ShouldBeFalse(); inventory.Cursor.ShouldBeNull();
        await fixture.VerifyDurableAuditDeltaAsync(auditBeforeInventory, open: false, count: 2, quarantine: true).ConfigureAwait(false);
        foreach (string legacy in new[] { "legacy-a", "legacy-b" })
        {
            string[] auditBeforeQuarantineOpen = await fixture.AuditKeysAsync().ConfigureAwait(false);
            GetProjectResponse quarantine = await fixture.ReadAsync<GetProjectResponse>(open: true, project: legacy, chatbot: false, projectAdministrator: true, quarantine: true).ConfigureAwait(false);
            quarantine.Name.ShouldBe("Legacy project " + legacy);
            quarantine.Description.ShouldBeNull(); quarantine.Setup.ShouldBeNull(); quarantine.SetupMetadata.ShouldBeNull();
            ProjectReadReference receipt = quarantine.References.Single();
            receipt.ReferenceKind.ShouldBe("ProjectFolder"); receipt.ReferenceState.ShouldBe(ReferenceState.Pending);
            receipt.FolderId.ShouldBeNull(); receipt.FileReferenceId.ShouldBeNull(); receipt.MemoryReferenceId.ShouldBeNull();
            receipt.DisplayName.ShouldBeNull(); receipt.ReasonCode.ShouldBe("ReferenceUnavailable");
            quarantine.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
            quarantine.Snapshot.Components.ShouldContain(component => component.Name == "Folder" && !component.Included);
            string serialized = System.Text.Json.JsonSerializer.Serialize(quarantine);
            Assert.True(!serialized.Contains("Restricted legacy", StringComparison.Ordinal), "Quarantine open disclosed legacy descriptive metadata.");
            await fixture.VerifyDurableAuditDeltaAsync(auditBeforeQuarantineOpen, open: true, count: 1, quarantine: true).ConfigureAwait(false);
        }
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(false);
        await CompleteAsync("inspection-permission-and-durable-metadata-only-audit").ConfigureAwait(false);

        await fixture.DenyNewAuditKeysAsync().ConfigureAwait(false);
        try
        {
            await fixture.VerifyAuditWriteFaultAsync().ConfigureAwait(false);
            await fixture.VerifyInspectedAuditRefusalAsync(open: true).ConfigureAwait(false);
            await fixture.VerifyInspectedAuditRefusalAsync(open: false).ConfigureAwait(false);
            await fixture.SafeDenialAsync(open: false, chatbot: false, projectAdministrator: true, quarantine: true).ConfigureAwait(false);
            foreach (string legacy in new[] { "legacy-a", "legacy-b" })
                await fixture.SafeDenialAsync(project: legacy, chatbot: false, projectAdministrator: true, quarantine: true).ConfigureAwait(false);
        }
        finally { await fixture.RestoreAuditStorageAsync().ConfigureAwait(false); }
        await fixture.WaitForAuthorizedRecoveryAsync("audit-storage").ConfigureAwait(false);
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(false);
        await CompleteAsync("audit-storage-unavailable-blocks-disclosure").ConfigureAwait(false);

        long priorHead = await fixture.HeadAsync(fixture.Tenant, "folder-project-a").ConfigureAwait(false);
        ProjectListPage immediatelyBeforeRevocation = await fixture.ReadAsync<ProjectListPage>(list: new(Cursor: first.Cursor, PageSize: 1)).ConfigureAwait(false);
        Assert.True(PackageReadFixture.SameStableReadMetadata(resumed, immediatelyBeforeRevocation),
            "The retained pre-restart cursor was not valid immediately before revocation.");
        Assert.True(PackageReadFixture.CursorIsWithinObservedLifetime(cursorRequestedAt, DateTimeOffset.UtcNow),
            "The retained cursor has reached its lifetime bound before revocation.");
        await fixture.ChangeFolderAccessAsync(fixture.Tenant, "folder-project-a", revoke: true).ConfigureAwait(false);
        (await fixture.HeadAsync(fixture.Tenant, "folder-project-a").ConfigureAwait(false)).ShouldBeGreaterThan(priorHead);
        before = await fixture.SnapshotAsync().ConfigureAwait(false);
        (await fixture.SafeDenialAsync().ConfigureAwait(false)).ShouldBe(absent);
        ProjectListPage revoked = await fixture.ReadAsync<ProjectListPage>().ConfigureAwait(false);
        Assert.True(revoked.Items.Select(item => item.ProjectId).SequenceEqual(["project-b"]), "Revoked Folder authority retained visibility.");
        using (var oldCursor = await fixture.QueryAsync(list: new(Cursor: first.Cursor, PageSize: 1)).ConfigureAwait(false))
        {
            Assert.True(PackageReadFixture.CursorIsWithinObservedLifetime(cursorRequestedAt, DateTimeOffset.UtcNow),
                "Cursor expiry cannot qualify as authority invalidation.");
            oldCursor.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(false);
        await CompleteAsync("revocation-observes-real-head-and-invalidates-old-continuation").ConfigureAwait(false);

        await fixture.ChangeFolderAccessAsync(fixture.Tenant, "folder-project-a", revoke: false).ConfigureAwait(false);
        await fixture.ReadAsync<GetProjectResponse>(open: true).ConfigureAwait(false);
        before = await fixture.SnapshotAsync().ConfigureAwait(false);
        await fixture.ResourceAsync("stop").ConfigureAwait(false);
        try
        {
            ProjectListPage unavailable = await fixture.ReadAsync<ProjectListPage>().ConfigureAwait(false);
            unavailable.Items.ShouldBeEmpty();
            unavailable.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
            await fixture.SafeDenialAsync().ConfigureAwait(false);
        }
        finally { await fixture.ResourceAsync("start").ConfigureAwait(false); }
        await fixture.WaitForAuthorizedRecoveryAsync("folder-service").ConfigureAwait(false);
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(false);
        await fixture.ReadAsync<GetProjectResponse>(open: true).ConfigureAwait(false);
        await CompleteAsync("owned-folder-outage-honest-unavailability-and-recovery").ConfigureAwait(false);
        await CompleteAsync("authenticated-command-and-projection-compatibility").ConfigureAwait(false);
        await WriteScenarioResultAsync(fixture, "main", completed, cancellationToken).ConfigureAwait(false);
    }

    private static Task WriteScenarioResultAsync(PackageReadFixture fixture, string scenario, IReadOnlyList<string> completed, CancellationToken cancellationToken)
        => PackageReadScenarioEvidence.WriteAfterProviderVerificationAsync(Path.Combine(fixture.Evidence, "matrix.json"), scenario, completed,
            fixture.Isolation, fixture.VerifyRawObserverBatchAsync, cancellationToken);
}
