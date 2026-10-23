namespace Hexalith.Projects.Server.Tests.Queries;

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadTests
{
    [Fact]
    public async Task ListDefaultsToFiftyAndPreservesConversationPolicy()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        for (int index = 0; index < 52; index++) { await fixture.SeedAsync($"project-{index:D3}", index * 10 + 100); }
        int writes = fixture.Store.Count;
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Items.Count.ShouldBe(50);
        page.HasMore.ShouldBeTrue();
        page.Cursor.ShouldNotBeNull();
        page.Items.ShouldAllBe(row => row.Name == null && row.Snapshot.ProjectVersion == 2);
        new PageRequest().PageSize.ShouldBe(25);
        fixture.Store.Count.ShouldBe(writes);
        page.Snapshot.AsOf.ShouldBe(fixture.Clock.Now);
    }

    [Fact]
    public async Task ChatbotFiltersBeforePagingAndHasNoGaps()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        for (int index = 0; index < 6; index++) { await fixture.SeedAsync($"project-{index}", index * 10 + 100); }
        fixture.Folders.Deny("folder-project-0"); fixture.Folders.Deny("folder-project-2");
        ProjectListPage first = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(PageSize: 2)), TestContext.Current.CancellationToken));
        first.Items.Select(row => row.ProjectId).ShouldBe(["project-1", "project-3"]);
        first.Items.ShouldAllBe(row => row.Name != null);
        ProjectListPage second = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(
            fixture.Query(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 2), correlation: "page-two"), TestContext.Current.CancellationToken));
        second.Items.Select(row => row.ProjectId).ShouldBe(["project-4", "project-5"]);
        second.HasMore.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    [InlineData(-1)]
    public async Task AuthorizedInvalidPagingIsSafe400Category(int size)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(PageSize: size)), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
    }

    [Theory]
    [InlineData("{\"pageSize\":2,\"pageSize\":3}")]
    [InlineData("{\"pageSize\":2,\"PageSize\":3}")]
    [InlineData("{\"tenantId\":\"tenant-b\"}")]
    [InlineData("{\"lifecycleState\":\"pending\"}")]
    [InlineData("{\"pageSize\":null}")]
    [InlineData("[]")]
    public async Task AuthorizedMalformedPayloadIsRejected(string json)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        QueryEnvelope query = fixture.Query(new ListProjectsQuery()) with { Payload = Encoding.UTF8.GetBytes(json) };
        (await fixture.Executor.ListAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
    }

    [Fact]
    public async Task CursorRejectsTamperFilterIdentitySizeAndExpiredReplay()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a"); await fixture.SeedAsync("project-b", 200);
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(PageSize: 1)), TestContext.Current.CancellationToken));
        foreach (ListProjectsQuery input in new[]
        {
            new ListProjectsQuery(Cursor: page.Cursor + "tamper", PageSize: 1),
            new ListProjectsQuery("Archived", page.Cursor, 1),
            new ListProjectsQuery(Cursor: page.Cursor, PageSize: 2),
        })
        {
            (await fixture.Executor.ListAsync(fixture.Query(input), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
        }

        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(16);
        // Refresh only the independent Tenant authority, keeping the cursor's authority version stable.
        var tenant = (await fixture.Tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken))!;
        tenant.LastEventTimestamp = fixture.Clock.Now;
        await fixture.Tenants.SaveAsync(tenant, TestContext.Current.CancellationToken);
        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(Cursor: page.Cursor, PageSize: 1)), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
    }

    [Fact]
    public async Task SafeMetadataAndInspectedMetadataHaveDurableAuditOnlyEffects()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await fixture.SeedAsync("project-a");
        ProjectReadModel before = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        int count = fixture.Store.Count;
        QueryResult safe = await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken);
        safe.Success.ShouldBeTrue();
        using JsonDocument safePayload = JsonDocument.Parse(safe.PayloadBytes!);
        safePayload.RootElement.TryGetProperty("name", out _).ShouldBeFalse();
        Encoding.UTF8.GetString(safe.PayloadBytes!).ShouldNotContain("Project project-a");
        fixture.Store.Count.ShouldBe(count);
        QueryEnvelope inspected = fixture.Query(new GetProjectQuery(true), "project-a");
        GetProjectResponse detail = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(inspected, TestContext.Current.CancellationToken));
        detail.Name.ShouldBe("Project project-a");
        detail.Setup.ShouldNotBeNull();
        fixture.Store.Count.ShouldBe(count + 1);
        ProjectReadAuditRecord record = fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName,
            fixture.AuditStore.Keys.Single())!;
        record.ProjectCount.ShouldBe(1); record.FieldClass.ShouldBe("ProjectNameDescriptionSetupReferenceLabels");
        record.Outcome.ShouldBe("Inspection"); record.RecordedAt.ShouldBe(fixture.Clock.Now);
        string auditJson = JsonSerializer.Serialize(record);
        auditJson.ShouldNotContain("project-a"); auditJson.ShouldNotContain("actor-1"); auditJson.ShouldNotContain("Safe description");
        (await fixture.Executor.GetAsync(inspected, TestContext.Current.CancellationToken)).Success.ShouldBeTrue();
        fixture.Store.Count.ShouldBe(count + 1);
        JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))).ShouldBe(JsonSerializer.Serialize(before));
    }

    [Fact]
    public async Task AuditFailurePreventsDescriptiveDisclosure()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await fixture.SeedAsync("project-a");
        fixture.Store.ConcurrentWriteBeforeTrySave = () => throw new IOException("Audit store unavailable");
        QueryResult result = await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(true), "project-a"), TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse(); result.ErrorMessage.ShouldBe("safe-denial"); result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task InspectionWithoutSeparatePermissionDeniesBeforeDisclosure()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        QueryResult result = await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(true), "project-a"), TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe("safe-denial"); result.PayloadBytes.ShouldBeNull();
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("tenant")]
    [InlineData("workload")]
    [InlineData("delegation")]
    [InlineData("audience")]
    [InlineData("scope")]
    [InlineData("header")]
    public async Task AuthorityMismatchWinsOverInvalidPayload(string mismatch)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        QueryEnvelope query = fixture.Query(new ListProjectsQuery()) with { Payload = "invalid"u8.ToArray() };
        query = mismatch switch
        {
            "actor" => query with { OriginalActorId = "other" },
            "tenant" => query with { TenantId = "tenant-b" },
            "workload" => query with { AuthenticatedWorkloadId = "other" },
            "delegation" => query with { DelegationId = null },
            "audience" => query with { Audience = ["other"] },
            "scope" => query with { Scopes = [] },
            _ => query,
        };
        if (mismatch == "header") { fixture.Http.Request.Headers["X-Hexalith-Tenant-Id"] = "tenant-b"; }
        QueryResult result = await fixture.Executor.ListAsync(query, TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe("safe-denial"); result.PayloadBytes.ShouldBeNull(); fixture.Folders.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task ChatbotCannotFallBackToTenantOperator()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        fixture.Identity.RemoveClaim(fixture.Identity.FindFirst("projects:role")!);
        fixture.Identity.AddClaim(new Claim("projects:role", "TenantOperator"));
        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
    }

    [Theory]
    [InlineData(EvidenceFreshnessState.Stale)]
    [InlineData(EvidenceFreshnessState.Rebuilding)]
    [InlineData(EvidenceFreshnessState.Unavailable)]
    public async Task NoncurrentPersistedProjectIsUnavailable(EvidenceFreshnessState freshness)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, key, model with { Freshness = freshness });
        GetProjectResponse response = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken));
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        response.Snapshot.Components.Single(component => component.Name == "Project").Freshness.ShouldBe(freshness);
    }

    [Fact]
    public async Task InspectionNeverDisclosesSiblingLabelsWithoutCurrentOwnerRead()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await fixture.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, key, model with
        {
            Detail = model.Detail! with
            {
                MemoryReferences = [new ProjectMemoryReference("memory-1", "private-memory-label", ReferenceState.Included, null, SupportedProjectReadFixture.EventTime)],
                FileReferences = [new ProjectFileReference("file-1", "folder-other", "private-file-label", ReferenceState.Included, null, SupportedProjectReadFixture.EventTime)],
            },
        });
        QueryResult result = await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(true), "project-a"), TestContext.Current.CancellationToken);
        GetProjectResponse response = SupportedProjectReadFixture.Payload<GetProjectResponse>(result);
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        Encoding.UTF8.GetString(result.PayloadBytes!).ShouldNotContain("private-");
        response.References.Where(item => item.ReferenceKind != "ProjectFolder").All(item => item.DisplayName is null && item.ReferenceState == ReferenceState.Unavailable).ShouldBeTrue();
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial); page.Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.Retry);
        page.Snapshot.Components.ShouldContain(component => component.Name == "References" && !component.Included);
    }

    [Fact]
    public async Task FolderlessHistoryIsInspectableButUnavailableAndOptionalOmissionsArePartial()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a", folder: false);
        GetProjectResponse response = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken));
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        await fixture.SeedAsync("project-b", 200);
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-b");
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, key, model with
        {
            Detail = model.Detail! with { MemoryReferences = [new ProjectMemoryReference("memory-1", null, ReferenceState.Unavailable, null, SupportedProjectReadFixture.EventTime)] },
        });
        response = SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-b"), TestContext.Current.CancellationToken));
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
    }
    [Theory]
    [InlineData("project.with.dots")]
    [InlineData("long-129")]
    [InlineData("long-256")]
    public async Task CanonicalPlatformProjectIdentifiersWorkForListOpenAndContinuation(string variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        string id = variant.StartsWith("long-", StringComparison.Ordinal) ? new string('a', int.Parse(variant[5..], System.Globalization.CultureInfo.InvariantCulture)) : variant;
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync(id); await fixture.SeedAsync("z-last", 500);
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(PageSize: 1)), TestContext.Current.CancellationToken));
        page.Items.Single().ProjectId.ShouldBe(id); page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), id), TestContext.Current.CancellationToken)).Success.ShouldBeTrue();
        ProjectListPage next = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(Cursor: page.Cursor, PageSize: 1)), TestContext.Current.CancellationToken));
        next.Items.Single().ProjectId.ShouldBe("z-last");
    }

    [Theory]
    [InlineData(".leading")]
    [InlineData("trailing-")]
    [InlineData("embedded/slash")]
    [InlineData("colon:id")]
    [InlineData("contains space")]
    [InlineData("too-long")]
    public async Task NoncanonicalIdentifiersAreRejected(string id)
    {
        if (id == "too-long") { id = new string('a', 257); }
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), id), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        await fixture.SeedAsync("project-a");
        string indexKey = ProjectReadModelKeys.Index("tenant-a");
        ProjectReadIndex index = fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, indexKey, index with { ProjectIds = [id] });
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken)).Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
    }

    [Theory]
    [InlineData("delayed-projection")]
    [InlineData("new-projection")]
    [InlineData("tenant-authority")]
    [InlineData("eventstore-authority")]
    [InlineData("folder-grant-before-boundary")]
    public async Task CursorInvalidatesOnAnyObservedProjectionOrAuthorityChange(string change)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        await fixture.SeedAsync("project-a", 100); await fixture.SeedAsync("project-b", 1000); await fixture.SeedAsync("project-c", 2000);
        if (change == "folder-grant-before-boundary") { fixture.Folders.Deny("folder-project-a"); }
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(PageSize: 1)), TestContext.Current.CancellationToken));
        page.Cursor.ShouldNotBeNull();
        switch (change)
        {
            case "delayed-projection":
                await fixture.Projection.ProjectAsync(new ProjectionRequest("tenant-a", "projects", "project-a",
                    [SupportedProjectReadFixture.Event(new ProjectArchived("tenant-a", "project-a", ProjectLifecycle.Archived, "actor-1", "archive", "task", "archive", "archive", fixture.Clock.Now), 3, 150)]), "delayed", TestContext.Current.CancellationToken);
                fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!.GlobalPosition.ShouldBe(2001);
                break;
            case "new-projection": await fixture.SeedAsync("project-d", 3000); break;
            case "tenant-authority":
                var tenant = (await fixture.Tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken))!;
                tenant.ProjectionWatermark = "tenant-a:2";
                await fixture.Tenants.SaveAsync(tenant, TestContext.Current.CancellationToken); break;
            case "eventstore-authority": fixture.Authorization.Watermark = "new-eventstore-authority"; break;
            default: fixture.Folders.Allow("folder-project-a"); break;
        }
        (await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(Cursor: page.Cursor, PageSize: 1), correlation: "next-page"), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
    }

    [Fact]
    public async Task DelayedProjectionDuringEnumerationReturnsUnavailableInsteadOfMixedRows()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a", 100); await fixture.SeedAsync("project-b", 1000);
        fixture.Folders.OnRead = async _ =>
        {
            fixture.Folders.OnRead = null;
            await fixture.Projection.ProjectAsync(new ProjectionRequest("tenant-a", "projects", "project-a",
                [SupportedProjectReadFixture.Event(new ProjectArchived("tenant-a", "project-a", ProjectLifecycle.Archived, "actor-1", "archive", "task", "archive", "archive", fixture.Clock.Now), 3, 150)]), "delayed-race", TestContext.Current.CancellationToken).ConfigureAwait(true);
        };
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Items.ShouldBeEmpty(); page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
    }

    [Theory]
    [InlineData(ProjectFolderValidationOutcome.Stale, ReferenceState.Stale)]
    [InlineData(ProjectFolderValidationOutcome.Unavailable, ReferenceState.Unavailable)]
    [InlineData(ProjectFolderValidationOutcome.Denied, ReferenceState.Unauthorized)]
    public async Task CurrentFolderEvidenceControlsEnumerationAndOpen(ProjectFolderValidationOutcome outcome, ReferenceState expected)
    {
        SupportedProjectReadFixture chatbot = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        await chatbot.SeedAsync("project-a"); chatbot.Folders.Outcomes["folder-project-a"] = outcome;
        ProjectListPage filtered = SupportedProjectReadFixture.Payload<ProjectListPage>(await chatbot.Executor.ListAsync(chatbot.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        filtered.Items.ShouldBeEmpty();
        filtered.Snapshot.ResponseState.ShouldBe(outcome == ProjectFolderValidationOutcome.Denied ? AdmissionResponseState.Complete : AdmissionResponseState.Unavailable);
        if (outcome != ProjectFolderValidationOutcome.Denied) { filtered.Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.Retry); }
        SupportedProjectReadFixture tenant = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await tenant.SeedAsync("project-a"); tenant.Folders.Outcomes["folder-project-a"] = outcome;
        GetProjectResponse opened = SupportedProjectReadFixture.Payload<GetProjectResponse>(await tenant.Executor.GetAsync(tenant.Query(new GetProjectQuery(true), "project-a"), TestContext.Current.CancellationToken));
        opened.References.Single().ReferenceState.ShouldBe(expected);
        opened.References.Single().ReasonCode.ShouldBe("ReferenceUnavailable"); opened.References.Single().DisplayName.ShouldBeNull();
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await tenant.Executor.ListAsync(tenant.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable); page.Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.Retry);
        page.Snapshot.Components.ShouldContain(component => component.Name == "Folder" && !component.Included);
    }

    [Fact]
    public async Task CorruptLifecycleNeverBecomesAnUnknownWireValueOrInventedActive()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, key)!;
        fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, key, model with { Detail = model.Detail! with { Lifecycle = (ProjectLifecycle)42 } });
        ProjectListPage list = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        list.Items.ShouldBeEmpty(); list.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        (await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InspectedAndChatbotListsAuditReturnedNamesOnceAndFailClosed(bool chatbot)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: chatbot, inspection: !chatbot);
        await fixture.SeedAsync("project-a"); await fixture.SeedAsync("project-b", 200); await fixture.SeedAsync("project-c", 300);
        fixture.Folders.Deny("folder-project-a");
        QueryEnvelope query = fixture.Query(new ListProjectsQuery(PageSize: 2, InspectDescriptiveMetadata: !chatbot));
        int count = fixture.Store.Count;
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(query, TestContext.Current.CancellationToken));
        page.Items.Count.ShouldBe(2); page.Items.ShouldAllBe(row => row.Name != null);
        ProjectReadAuditRecord record = fixture.Store.Snapshot<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, fixture.AuditStore.Keys.Single())!;
        record.ProjectCount.ShouldBe(2); record.FieldClass.ShouldBe("ProjectName"); record.Outcome.ShouldBe("Inspection");
        fixture.Store.Count.ShouldBe(count + 1);
        (await fixture.Executor.ListAsync(query, TestContext.Current.CancellationToken)).Success.ShouldBeTrue(); fixture.Store.Count.ShouldBe(count + 1);
        fixture.Store.ConcurrentWriteBeforeTrySave = () => throw new IOException("Audit unavailable");
        QueryResult denied = await fixture.Executor.ListAsync(query with { CorrelationId = "failed-audit" }, TestContext.Current.CancellationToken);
        denied.Success.ShouldBeFalse(); denied.ErrorMessage.ShouldBe("safe-denial"); denied.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task ProjectSpecificAuthorityOutageDoesNotBecomeAnEmptyCompleteList()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a"); fixture.Authorization.UnavailableProject = "project-a";
        ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken));
        page.Items.ShouldBeEmpty(); page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        page.Snapshot.Components.Single().Reason.ShouldBe("AuthorizationUnavailable");
        page.Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.Retry);
    }

}
