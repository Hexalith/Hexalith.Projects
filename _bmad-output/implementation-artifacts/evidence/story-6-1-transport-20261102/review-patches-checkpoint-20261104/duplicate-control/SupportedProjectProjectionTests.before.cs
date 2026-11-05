namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Discovery;
using Microsoft.Extensions.DependencyInjection;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectProjectionTests
{
    [Fact]
    public async Task CompleteCatalogClassifiesHarmlessEventsAsDeterministicNoOps()
    {
        Type[] relevant = ProjectReadProjectionHandler.RelevantEventTypes.OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        Type[] noOps = ProjectReadProjectionHandler.NoOpEventTypes.OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        Type[] contracts = typeof(IProjectEvent).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Namespace == typeof(IProjectEvent).Namespace
                && (typeof(IProjectEvent).IsAssignableFrom(type) || type.Name.EndsWith("Rejected", StringComparison.Ordinal)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        relevant.Concat(noOps).OrderBy(type => type.FullName, StringComparer.Ordinal).ShouldBe(contracts);
        relevant.Intersect(noOps).ShouldBeEmpty();

        foreach (Type noOp in noOps)
        {
            InMemoryReadModelStore incremental = new();
            ProjectionEventDto created = SupportedProjectReadFixture.Request("project-a").Events[0];
            JsonObject content = JsonSerializer.SerializeToNode(ValidNoOp(noOp), noOp, JsonSerializerOptions.Web)!.AsObject();
            content["futureHarmlessMetadata"] = "compatible";
            ProjectionEventDto harmless = new(noOp.FullName!, JsonSerializer.SerializeToUtf8Bytes(content),
                "json", 2, SupportedProjectReadFixture.EventTime, "projection", "harmless", "actor-1", 101);
            ProjectReadProjectionHandler handler = new(incremental, incremental);
            (await handler.ProjectAsync(new("tenant-a", "projects", "project-a", [created]),
                "create", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
            (await handler.ProjectAsync(new("tenant-a", "projects", "project-a", [harmless]),
                "harmless", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
            ProjectReadModel after = incremental.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName,
                ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
            after.Detail!.Name.ShouldBe("Project project-a");
            after.Sequence.ShouldBe(2);
            after.GlobalPosition.ShouldBe(101);

            InMemoryReadModelStore replay = new();
            (await new ProjectReadProjectionHandler(replay, replay).ProjectAsync(
                new("tenant-a", "projects", "project-a", [created, harmless]),
                "replay", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
            JsonSerializer.Serialize(replay.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName,
                ProjectReadModelKeys.Detail("tenant-a", "project-a"))).ShouldBe(JsonSerializer.Serialize(after));
        }
    }

    [Fact]
    public async Task OlderValidDuplicatePreservesLaterFolderObservation()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a");
        ProjectFolderSet later = new("tenant-a", "project-a", "folder-a", new("Later folder"), "actor", "correlation", "task", "key", "fingerprint", SupportedProjectReadFixture.EventTime.AddSeconds(1));
        request = request with { Events = [request.Events[0], SupportedProjectReadFixture.Event(later, 2, 101)] };
        (await fixture.Projection.ProjectAsync(request, "later-folder", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        byte[] before = fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        (await fixture.Projection.ProjectAsync(request with { Events = [request.Events[0]] }, "older-duplicate", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
        fixture.Store.SnapshotRaw(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a")).ShouldBe(before);
    }

    [Fact]
    public async Task OlderCreatedJsonRetainsOptionalAndAdditiveCompatibility()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        ProjectionEventDto created = SupportedProjectReadFixture.Request("project-a").Events[0];
        JsonObject content = JsonNode.Parse(created.Payload)!.AsObject();
        foreach (string optional in new[] { "description", "setupMetadata", "actorPrincipalId", "correlationId", "taskId", "idempotencyKey", "idempotencyFingerprint" }) content.Remove(optional);
        content["futureHarmlessMetadata"] = "compatible";
        created = created with { Payload = JsonSerializer.SerializeToUtf8Bytes(content) };
        (await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "project-a", [created]), "historical-created", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "project-a", [created]), "historical-duplicate", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
    }

    [Theory]
    [InlineData(false, "name")]
    [InlineData(false, "lifecycle")]
    [InlineData(false, "occurredAt")]
    [InlineData(false, "empty-name")]
    [InlineData(false, "invalid-lifecycle")]
    [InlineData(false, "default-occurredAt")]
    [InlineData(false, "unsafe-name")]
    [InlineData(true, "principalId")]
    [InlineData(true, "operation")]
    [InlineData(true, "expiresAt")]
    [InlineData(true, "empty-principalId")]
    [InlineData(true, "default-expiresAt")]
    public async Task MalformedKnownDuplicateCannotPreserveCurrent(bool noOp, string field)
        => await RejectMalformedKnownAsync(noOp, field, duplicate: true);

    [Theory]
    [InlineData("principalId")]
    [InlineData("grantId")]
    [InlineData("expiresAt")]
    [InlineData("empty-principalId")]
    [InlineData("default-expiresAt")]
    public async Task MalformedKnownNoOpCannotAdvanceCurrent(string field)
        => await RejectMalformedKnownAsync(noOp: true, field, duplicate: false);

    private static async Task RejectMalformedKnownAsync(bool noOp, string field, bool duplicate)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync().ConfigureAwait(false);
        await fixture.SeedAsync("project-a").ConfigureAwait(false);
        ProjectionEventDto item = noOp
            ? SupportedProjectReadFixture.Event(ValidNoOp(typeof(ProjectChatBotAuthorityGranted)), duplicate ? 2 : 3, duplicate ? 101 : 102)
            : SupportedProjectReadFixture.Request("project-a").Events[0];
        JsonObject content = JsonNode.Parse(item.Payload)!.AsObject();
        switch (field)
        {
            case "empty-name": content["name"] = " "; break;
            case "invalid-lifecycle": content["lifecycle"] = 999; break;
            case "default-occurredAt": content["occurredAt"] = default(DateTimeOffset); break;
            case "unsafe-name": content["name"] = "/private/resource/path"; break;
            case "empty-principalId": content["principalId"] = " "; break;
            case "default-expiresAt": content["expiresAt"] = default(DateTimeOffset); break;
            default: content.Remove(field); break;
        }
        item = item with { Payload = JsonSerializer.SerializeToUtf8Bytes(content) };
        (await fixture.Projection.ProjectAsync(new("tenant-a", "projects", "project-a", [item]), "malformed-known", TestContext.Current.CancellationToken).ConfigureAwait(false)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        model.Sequence.ShouldBe(2);
        model.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
    }

    private static object ValidNoOp(Type type)
    {
        ProjectId project = new("project-a");
        DateTimeOffset at = SupportedProjectReadFixture.EventTime;
        return type.Name switch
        {
            nameof(ProjectChatBotAuthorityGranted) => new ProjectChatBotAuthorityGranted("tenant-a", "project-a", "principal", "RecordGovernedNote", "human", "Ui", "grant", at.AddMinutes(1), "actor", null!, null!, null!, null!, at),
            nameof(ProjectChatBotAuthorityRevoked) => new ProjectChatBotAuthorityRevoked("tenant-a", "project-a", "principal", "RecordGovernedNote", "human", "Ui", "grant", "actor", null!, null!, null!, null!, at),
            nameof(ProjectChatBotAuthorityRejected) => new ProjectChatBotAuthorityRejected("denied"),
            nameof(ProjectCreationRejected) => new ProjectCreationRejected("tenant-a", ReferenceState.Unauthorized),
            nameof(ProjectArchiveRejected) => new ProjectArchiveRejected(project, "tenant-a", ReferenceState.Unauthorized),
            nameof(ProjectRestoreRejected) => new ProjectRestoreRejected(project, "tenant-a", ReferenceState.Unauthorized),
            nameof(ProjectSetupUpdateRejected) => new ProjectSetupUpdateRejected(project, "tenant-a", ReferenceState.Unauthorized),
            nameof(ProjectReferenceLinkRejected) => new ProjectReferenceLinkRejected(project, "tenant-a", "folder", "folder-a", ReferenceState.Unauthorized),
            nameof(ProjectReferenceUnlinkRejected) => new ProjectReferenceUnlinkRejected(project, "tenant-a", "folder", "folder-a", ReferenceState.Unauthorized),
            nameof(ProjectResolutionConfirmationRejected) => new ProjectResolutionConfirmationRejected(project, "tenant-a", ReferenceState.Unauthorized),
            _ => throw new InvalidOperationException("Unclassified no-op test fixture."),
        };
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("corrupt")]
    [InlineData("undecodable")]
    public async Task InvalidDuplicateDeliveryFailsRatherThanBeingSkipped(string fault)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        ProjectionEventDto duplicate = SupportedProjectReadFixture.Request("project-a").Events[0];
        duplicate = fault switch
        {
            "unknown" => duplicate with { EventTypeName = "New.PotentiallyRelevantEvent" },
            "corrupt" => duplicate with { Payload = "broken"u8.ToArray() },
            _ => duplicate with { SerializationFormat = "unrecognized" },
        };
        ProjectionRequest request = new("tenant-a", "projects", "project-a", [duplicate]);
        (await fixture.Projection.ProjectAsync(request, "invalid-duplicate", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Failed);
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        model.Sequence.ShouldBe(2);
        model.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
        fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Index("tenant-a"))!.UnavailableProjectIds.ShouldContain("project-a");
    }

    [Fact]
    public async Task IncrementalReplayDuplicateAndRestartPreserveExactGlobalPositions()
    {
        InMemoryReadModelStore store = new();
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a", 901);
        ProjectReadProjectionHandler handler = new(store, store);
        (await handler.ProjectAsync(request with { Events = [request.Events[0]] }, "first", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        handler = new(store, store);
        (await handler.ProjectAsync(request with { Events = [request.Events[1]] }, "second", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        string before = JsonSerializer.Serialize(store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a")));
        (await handler.ProjectAsync(request, "replay", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
        JsonSerializer.Serialize(store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))).ShouldBe(before);
        ProjectReadIndex index = store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!;
        index.GlobalPosition.ShouldBe(902); index.ProjectIds.ShouldBe(["project-a"]);
        InMemoryReadModelStore replay = new();
        await new ProjectReadProjectionHandler(replay, replay).ProjectAsync(request, "full", TestContext.Current.CancellationToken);
        JsonSerializer.Serialize(replay.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))).ShouldBe(before);
    }

    [Fact]
    public async Task UnknownFirstEventDegradesExistingTenantEnumerationUntilReplayed()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        ProjectionRequest valid = SupportedProjectReadFixture.Request("project-b", 500);
        ProjectionRequest unknown = valid with { Events = [valid.Events[0] with { EventTypeName = "Unknown.RelevantEvent" }] };
        (await fixture.Projection.ProjectAsync(unknown, "unknown", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        ProjectReadIndex index = fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!;
        index.UnavailableProjectIds.ShouldBe(["project-b"]);
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery()), TestContext.Current.CancellationToken)).Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        (await fixture.Projection.ProjectAsync(valid, "repair", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!.UnavailableProjectIds.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("gap")]
    [InlineData("global-position")]
    [InlineData("corrupt")]
    [InlineData("unknown")]
    [InlineData("tenant")]
    public async Task InvalidDeliveryRetainsLastKnownTruthAndMarksUnavailable(string fault)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a", folder: false);
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a");
        ProjectionEventDto next = request.Events[1];
        next = fault switch
        {
            "gap" => next with { SequenceNumber = 3 },
            "global-position" => next with { GlobalPosition = 0 },
            "corrupt" => next with { Payload = "broken"u8.ToArray() },
            "unknown" => next with { EventTypeName = "New.Unknown" },
            "tenant" => SupportedProjectReadFixture.Event(new ProjectFolderSet("tenant-b", "project-a", "folder-a", new ProjectFolderMetadata("Folder"), "actor", "correlation", "task", "key", "fingerprint", SupportedProjectReadFixture.EventTime), 2, 101),
            _ => next,
        };
        await fixture.Projection.ProjectAsync(request with { Events = [next] }, "bad", TestContext.Current.CancellationToken);
        ProjectReadModel model = fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        model.Sequence.ShouldBe(1); model.GlobalPosition.ShouldBe(100); model.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
        model.Detail!.ProjectFolder.ShouldBeNull();
    }

    [Fact]
    public async Task CataloguedRejectionsAdvanceWatermarkWithoutCreatingProjects()
    {
        InMemoryReadModelStore store = new();
        ProjectionRequest request = new("tenant-a", "projects", "preactivation",
            [SupportedProjectReadFixture.Event(new ProjectChatBotAuthorityRejected("denied"), 1, 700)]);
        (await new ProjectReadProjectionHandler(store, store).ProjectAsync(request, "rejected", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ProjectReadIndex index = store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!;
        index.ProjectIds.ShouldBeEmpty(); index.GlobalPosition.ShouldBe(700);
    }

    [Fact]
    public async Task RebuildPlanHasNoWritesAndPlatformPromotionRepairsBothKeys()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a");
        int before = fixture.Store.Count;
        DomainProjectionRebuildPlan plan = await fixture.Projection.PrepareRebuildAsync(request, "rebuild", TestContext.Current.CancellationToken);
        fixture.Store.Count.ShouldBe(before);
        plan.Operations.Count.ShouldBe(2);
        ReadModelBatchResult result = await fixture.Store.ExecuteAsync(new(new(ProjectReadModelKeys.StoreName,
            "tenant-a", "projects", "project-a", "rebuild", "rebuild"), plan.Operations), TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
        fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))!.GlobalPosition.ShouldBe(101);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MissingEventTypeThroughSdkDispatchPersistsUnavailableEnumeration(string? eventType)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("healthy");
        ProjectionRequest request = SupportedProjectReadFixture.Request("new-project", 500);
        request = request with { Events = [request.Events[0] with { EventTypeName = eventType! }] };
        ServiceCollection services = new();
        services.AddSingleton<IAsyncDomainProjectionHandler>(fixture.Projection);
        using ServiceProvider provider = services.BuildServiceProvider();
        ProjectionDispatchOptions options = new();
        DomainProjectionIdentityOptions identity = new() { AppId = "projects", ServiceVersion = "v1" };
        var catalog = AdminOperationalIndexMetadata.Create(new DiscoveryResult([], []), ["projects"], [],
            provider.GetServices<IAsyncDomainProjectionHandler>(), "projects", "v1", options, ["projects"]);
        var result = await DomainProjectionDispatcher.DispatchAsync(provider, new(request, ["project-reads"], "missing-event-type", catalog.CatalogFingerprint!), options, identity, TestContext.Current.CancellationToken);
        result.Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Failed);
        fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "new-project"))!.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
        fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))!.UnavailableProjectIds.ShouldBe(["new-project"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task RebuildRejectsHeadMismatchWithoutRepairingUnavailableMarkers(long sourceHead)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        ProjectionRequest request = SupportedProjectReadFixture.Request("project-a");
        await fixture.Projection.ProjectAsync(request with { Events = [request.Events[0] with { EventTypeName = "unknown" }] }, "unknown", TestContext.Current.CancellationToken);
        string beforeDetail = JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a")));
        string beforeIndex = JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a")));
        await Should.ThrowAsync<DomainProjectionRebuildRejectedException>(() => fixture.Projection.PrepareRebuildAsync(request with { RebuildSourceHeadSequence = sourceHead }, "wrong-head", TestContext.Current.CancellationToken));
        JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))).ShouldBe(beforeDetail);
        JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))).ShouldBe(beforeIndex);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OptimisticBatchConflictsRetryAndNeverExposePartialDetail(bool persistent)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        string detailKey = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        string indexKey = ProjectReadModelKeys.Index("tenant-a");
        ProjectReadModel initial = (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, TestContext.Current.CancellationToken)).Value!;
        ProjectReadIndex initialIndex = (await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, TestContext.Current.CancellationToken)).Value!;
        int conflicts = 0;
        fixture.Store.BatchFaultHook = (phase, ordinal, _) =>
        {
            if (phase == ReadModelBatchPhase.BeforeInstallOperation && ordinal == 1 && (persistent || conflicts == 0))
            {
                conflicts++;
                fixture.Store.SeedRaw(ProjectReadModelKeys.StoreName, indexKey, initialIndex);
            }
            return Task.CompletedTask;
        };
        var result = await fixture.Projection.ProjectAsync(ArchiveRequest(fixture), "conflict", TestContext.Current.CancellationToken);
        ProjectReadModel after = (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, TestContext.Current.CancellationToken)).Value!;
        ProjectReadIndex afterIndex = (await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, TestContext.Current.CancellationToken)).Value!;
        if (persistent)
        {
            result.Status.ShouldBe(ProjectionDispatchStatus.Retryable); conflicts.ShouldBe(ReadModelWritePolicy.DefaultMaxAttempts);
            JsonSerializer.Serialize(after).ShouldBe(JsonSerializer.Serialize(initial));
            JsonSerializer.Serialize(afterIndex).ShouldBe(JsonSerializer.Serialize(initialIndex));
            fixture.Store.BatchFaultHook = null;
            (await fixture.Projection.ProjectAsync(ArchiveRequest(fixture), "conflict", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        }
        else
        {
            result.Status.ShouldBe(ProjectionDispatchStatus.Completed); conflicts.ShouldBe(1);
            after.Detail!.Lifecycle.ShouldBe(ProjectLifecycle.Archived); after.GlobalPosition.ShouldBe(150); afterIndex.GlobalPosition.ShouldBe(150);
        }
    }

    [Fact]
    public async Task InterruptedBatchRetainsBothPriorValuesUntilRetryCompletes()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        string detailKey = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        string indexKey = ProjectReadModelKeys.Index("tenant-a");
        string beforeDetail = JsonSerializer.Serialize((await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, TestContext.Current.CancellationToken)).Value);
        string beforeIndex = JsonSerializer.Serialize((await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, TestContext.Current.CancellationToken)).Value);
        fixture.Store.BatchFaultHook = (phase, ordinal, _) => phase == ReadModelBatchPhase.Reconcile || phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 0
            ? Task.FromException(new IOException("Unavailable during partial batch")) : Task.CompletedTask;
        (await fixture.Projection.ProjectAsync(ArchiveRequest(fixture), "retryable", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        JsonSerializer.Serialize((await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, TestContext.Current.CancellationToken)).Value).ShouldBe(beforeDetail);
        JsonSerializer.Serialize((await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, TestContext.Current.CancellationToken)).Value).ShouldBe(beforeIndex);
        fixture.Store.BatchFaultHook = null;
        (await fixture.Projection.ProjectAsync(ArchiveRequest(fixture), "retryable", TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.Store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, TestContext.Current.CancellationToken)).Value!.GlobalPosition.ShouldBe(150);
        (await fixture.Store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, TestContext.Current.CancellationToken)).Value!.GlobalPosition.ShouldBe(150);
    }

    [Fact]
    public async Task ArchiveRestoreFiltersOpenAndReplayUseThePersistedCatalog()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a");
        ProjectionRequest archived = ArchiveRequest(fixture);
        await fixture.Projection.ProjectAsync(archived, "archive", TestContext.Current.CancellationToken);
        SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken)).LifecycleState.ShouldBe(ProjectLifecycle.Archived);
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery("Active")), TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery("Archived")), TestContext.Current.CancellationToken)).Items.Single().ProjectId.ShouldBe("project-a");
        ProjectionEventDto restored = SupportedProjectReadFixture.Event(new ProjectRestored("tenant-a", "project-a", ProjectLifecycle.Active,
            "actor-1", "restore", "task", "restore", "restore", fixture.Clock.Now), 4, 200);
        await fixture.Projection.ProjectAsync(archived with { Events = [restored] }, "restore", TestContext.Current.CancellationToken);
        SupportedProjectReadFixture.Payload<GetProjectResponse>(await fixture.Executor.GetAsync(fixture.Query(new GetProjectQuery(), "project-a"), TestContext.Current.CancellationToken)).LifecycleState.ShouldBe(ProjectLifecycle.Active);
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery("Archived")), TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();
        SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery("Active")), TestContext.Current.CancellationToken)).Items.Single().ProjectId.ShouldBe("project-a");
        InMemoryReadModelStore replay = new();
        await new ProjectReadProjectionHandler(replay, replay).ProjectAsync(archived with { Events = [.. SupportedProjectReadFixture.Request("project-a").Events, .. archived.Events, restored] }, "replay", TestContext.Current.CancellationToken);
        JsonSerializer.Serialize(replay.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))).ShouldBe(JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "project-a"))));
        JsonSerializer.Serialize(replay.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))).ShouldBe(JsonSerializer.Serialize(fixture.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"))));
    }

    private static ProjectionRequest ArchiveRequest(SupportedProjectReadFixture fixture)
        => new("tenant-a", "projects", "project-a", [SupportedProjectReadFixture.Event(new ProjectArchived("tenant-a", "project-a", ProjectLifecycle.Archived,
            "actor-1", "archive", "task", "archive", "archive", fixture.Clock.Now), 3, 150)]);

}
