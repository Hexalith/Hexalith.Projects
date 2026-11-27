// <copyright file="Story71FolderFirstTwoReplicaTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if SUPPORTED_PROJECT_READ_SOURCE
namespace Hexalith.Projects.Integration.Tests.Story71;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReads;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.SdkWriter.Admission;
using Hexalith.Projects.SdkWriter.Aggregates;
using Hexalith.Projects.SdkWriter.Claims;
using Hexalith.Projects.SdkWriter.Composition;
using Hexalith.Projects.SdkWriter.Storage;
using Hexalith.Projects.SdkWriter.Workflow;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

using WireCreateProject = Hexalith.Projects.SdkWriter.Commands.CreateProject;
using WireArchiveProject = Hexalith.Projects.SdkWriter.Commands.ArchiveProject;
using WireRestoreProject = Hexalith.Projects.SdkWriter.Commands.RestoreProject;

/// <summary>
/// Story 7.1 persisted two-replica Docker Dapr <c>1.19.2</c> + Redis <c>7.4.12</c> integration tests:
/// synchronized AD-35 claim race + lost reply + cross-replica failover recovery, boundary pauses/restart
/// + projection lag + retained/audit verification, abandonment fence-before-release + Safe Metadata audit
/// + supported read invisibility on Redis, and AD-17 <c>ProcessAsync</c> equivalence in isolated test composition.
/// </summary>
public sealed class Story71FolderFirstTwoReplicaTests(DockerDaprRedisTwoReplicaFixture fixture)
    : IClassFixture<DockerDaprRedisTwoReplicaFixture>
{
    private static readonly DateTimeOffset BaseTime = new(2026, 11, 27, 15, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    [Trait("Story", "7.1")]
    public async Task TwoReplicaClaimRace_WithLostReplyAndFailover_ConvergesToSingleActiveHolderOnRedis()
    {
        fixture.ObservedRedisServerVersion.ShouldContain("Redis server v=7.4.");
        CancellationToken ct = TestContext.Current.CancellationToken;

        string tenant = $"tenant-race-{Guid.NewGuid():N}"[..24];
        string actor = "actor-race";
        string folderId = $"fld-race-{Guid.NewGuid():N}"[..24];

        SharedClock clock = new(BaseTime);
        MutableProjectCreationAuthorityChecker authority = new();

        FoldersAggregateProvisioningClient foldersA = new(fixture.ReplicaAStore, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        FoldersAggregateProvisioningClient foldersB = new(fixture.ReplicaBStore, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        await foldersA.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(tenant, folderId, "Shared Race Folder", actor), ct);

        // Synchronized race barrier: both replicas reach Step 2 claim append simultaneously!
        int arrived = 0;
        TaskCompletionSource barrierOpen = new(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task WaitBarrierAsync(CancellationToken token)
        {
            if (Interlocked.Increment(ref arrived) == 2)
            {
                barrierOpen.TrySetResult();
            }

            await barrierOpen.Task.WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
        }

        ProjectCreationFaultOptions faultsA = new() { BeforeClaimAppendBarrier = WaitBarrierAsync, DropNextClaimReplyOnce = true };
        ProjectCreationFaultOptions faultsB = new() { BeforeClaimAppendBarrier = WaitBarrierAsync, DropNextClaimReplyOnce = true };

        FolderFirstProjectCreationWorkflow replicaA = BuildWorkflow(fixture.ReplicaAStore, foldersA, authority, clock, faultsA);
        FolderFirstProjectCreationWorkflow replicaB = BuildWorkflow(fixture.ReplicaBStore, foldersB, authority, clock, faultsB);

        ProjectCreationSelectionEvidence evA = new("ev-race-a", tenant, actor, "ui", folderId, BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationSelectionEvidence evB = new("ev-race-b", tenant, actor, "ui", folderId, BaseTime, BaseTime.AddMinutes(5));

        ProjectCreationRequest reqA = new(tenant, actor, ProjectCreationCallerClass.ProjectUser, "ui", "idem-race-a", "Race Project A", SuppliedFolderId: folderId, SelectionEvidence: evA);
        ProjectCreationRequest reqB = new(tenant, actor, ProjectCreationCallerClass.ProjectUser, "ui", "idem-race-b", "Race Project B", SuppliedFolderId: folderId, SelectionEvidence: evB);

        Task<ProjectCreationWorkflowResult> taskA = Task.Run(() => replicaA.CreateProjectAsync(reqA, ct), ct);
        Task<ProjectCreationWorkflowResult> taskB = Task.Run(() => replicaB.CreateProjectAsync(reqB, ct), ct);
        ProjectCreationWorkflowResult[] outcomes = await Task.WhenAll(taskA, taskB);

        // Exactly ONE replica won the AD-35 claim and lost its reply (WaitingForDependency);
        // the competing replica got a nondisclosing refusal (Rejected)!
        ProjectCreationWorkflowResult winner = outcomes.Single(r => r.Status == ProjectCreationWorkflowStatus.WaitingForDependency);
        ProjectCreationWorkflowResult loser = outcomes.Single(r => r.Status == ProjectCreationWorkflowStatus.Rejected);
        winner.TaskId.ShouldNotBeNullOrWhiteSpace();
        loser.TaskId.ShouldNotBeNullOrWhiteSpace();

        // Failover: resume the winner on the OTHER replica after lease expiry!
        clock.Advance(TimeSpan.FromSeconds(35));
        faultsA.BeforeClaimAppendBarrier = null;
        faultsB.BeforeClaimAppendBarrier = null;
        bool winnerWasA = outcomes[0] == winner;
        FolderFirstProjectCreationWorkflow failoverReplica = winnerWasA ? replicaB : replicaA;
        ProjectCreationRequest winnerReq = winnerWasA ? reqA : reqB;

        ProjectCreationWorkflowResult recovered = await failoverReplica.ResumeTaskAsync(winner.TaskId!, winnerReq, ct);
        recovered.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        recovered.FolderId.ShouldBe(folderId);
        recovered.FolderClaimGeneration.ShouldBe(1);
        recovered.ProjectId.ShouldNotBeNullOrWhiteSpace();

        // Verify persisted Redis state directly!
        string redisKeys = await fixture.ExecRedisCliAsync($"KEYS \"*{folderId}*\"", ct);
        redisKeys.ShouldContain(folderId);

        (ProjectFolderClaimState finalClaim, StoredEventStream claimStream) = await failoverReplica.RehydrateClaimStateAsync(tenant, folderId, ct);
        finalClaim.IsActive.ShouldBeTrue();
        finalClaim.ActiveClaim!.ProjectId.ShouldBe(recovered.ProjectId);
        finalClaim.ActiveClaim.Generation.ShouldBe(1);
        claimStream.Events.Count.ShouldBe(1);

        // Supported read model in Redis contains strictly the winner with the supplied Folder's own display name!
        ReadModelEntry<ProjectReadIndex> indexEntry = await fixture.ReplicaAStore.GetAsync<ProjectReadIndex>(
            DockerDaprRedisTwoReplicaFixture.StoreName,
            ProjectReadModelKeys.Index(tenant),
            ct);
        indexEntry.Value.ShouldNotBeNull();
        indexEntry.Value!.ProjectIds.ShouldBe([recovered.ProjectId!]);

        ReadModelEntry<ProjectReadModel> detailEntry = await fixture.ReplicaBStore.GetAsync<ProjectReadModel>(
            DockerDaprRedisTwoReplicaFixture.StoreName,
            ProjectReadModelKeys.Detail(tenant, recovered.ProjectId!),
            ct);
        detailEntry.Value!.Detail!.ProjectFolder!.DisplayName.ShouldBe("Shared Race Folder");
        foldersA.DeleteAttemptCount.ShouldBe(0);
        foldersB.DeleteAttemptCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Story", "7.1")]
    public async Task RestartAndProjectionLag_AcrossTwoReplicas_ConfirmsSupportedRetainedAndSafeMetadataAuditOnRedis()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string tenant = $"tenant-lag-{Guid.NewGuid():N}"[..24];
        string actor = "actor-lag";

        SharedClock clock = new(BaseTime);
        MutableProjectCreationAuthorityChecker authority = new();
        FoldersAggregateProvisioningClient foldersA = new(fixture.ReplicaAStore, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        ProjectCreationFaultOptions faultsA = new() { PauseAfterFolderReceipt = true };
        FolderFirstProjectCreationWorkflow replicaA = BuildWorkflow(fixture.ReplicaAStore, foldersA, authority, clock, faultsA);

        ProjectCreationRequest req = new(tenant, actor, ProjectCreationCallerClass.ProjectUser, "ui", "idem-lag-1", "Lag Recovery Project", "Safe desc", "setup-ref-1", "v1", "tenant_sensitive");

        // 1. Replica A provisions Folder and pauses after Step 1 receipt.
        ProjectCreationWorkflowResult afterStep1 = await replicaA.CreateProjectAsync(req, ct);
        afterStep1.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        afterStep1.FolderId.ShouldNotBeNullOrWhiteSpace();
        afterStep1.FolderProvisioningReceiptId.ShouldNotBeNullOrWhiteSpace();

        // Invisible in supported reads while paused after Step 1.
        (await fixture.ReplicaBStore.GetAsync<ProjectReadIndex>(DockerDaprRedisTwoReplicaFixture.StoreName, ProjectReadModelKeys.Index(tenant), ct)).Value.ShouldBeNull();

        // 2. Replica B resumes and pauses after Step 3 commit BEFORE projection confirmation (projection lag!).
        clock.Advance(TimeSpan.FromSeconds(35));
        FoldersAggregateProvisioningClient foldersB = new(fixture.ReplicaBStore, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        ProjectCreationFaultOptions faultsB = new() { PauseBeforeProjection = true };
        FolderFirstProjectCreationWorkflow replicaB = BuildWorkflow(fixture.ReplicaBStore, foldersB, authority, clock, faultsB);

        ProjectCreationWorkflowResult afterStep3 = await replicaB.ResumeTaskAsync(afterStep1.TaskId!, req, ct);
        afterStep3.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        (await fixture.ReplicaAStore.GetAsync<ProjectReadIndex>(DockerDaprRedisTwoReplicaFixture.StoreName, ProjectReadModelKeys.Index(tenant), ct)).Value.ShouldBeNull();

        // 3. Simulated process restart on Replica A completes projection confirmation!
        clock.Advance(TimeSpan.FromSeconds(35));
        DaprReadModelStore restartedStoreA = fixture.CreateFreshReplicaAStore();
        FoldersAggregateProvisioningClient restartedFoldersA = new(restartedStoreA, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        FolderFirstProjectCreationWorkflow restartedReplicaA = BuildWorkflow(restartedStoreA, restartedFoldersA, authority, clock, new ProjectCreationFaultOptions());

        ProjectCreationWorkflowResult completed = await restartedReplicaA.ResumeTaskAsync(afterStep1.TaskId!, req, ct);
        completed.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        completed.ProjectId.ShouldNotBeNullOrWhiteSpace();
        completed.FolderId.ShouldBe(afterStep1.FolderId);

        // Verify supported read model on Redis.
        ReadModelEntry<ProjectReadModel> supported = await fixture.ReplicaBStore.GetAsync<ProjectReadModel>(
            DockerDaprRedisTwoReplicaFixture.StoreName,
            ProjectReadModelKeys.Detail(tenant, completed.ProjectId!),
            ct);
        supported.Value.ShouldNotBeNull();
        supported.Value!.Freshness.ShouldBe(EvidenceFreshnessState.Current);
        supported.Value.Detail!.ProjectFolder!.FolderId.ShouldBe(completed.FolderId);

        // Verify retained projections and Safe Metadata audit from the persisted Redis event stream.
        (_, StoredEventStream stream) = await restartedReplicaA.RehydrateProjectStateAsync(tenant, completed.ProjectId!, ct);
        ProjectProjectionEnvelope[] envelopes = stream.Events
            .Select(e => new ProjectProjectionEnvelope(tenant, e.SequenceNumber, (IProjectEvent)ReadModelBackedEventStore.DeserializeEvent(e)))
            .ToArray();
        ProjectDetailProjection.Empty.Apply(envelopes).Get(tenant, completed.ProjectId!)!.ProjectFolder!.FolderId.ShouldBe(completed.FolderId);
        ProjectListProjection.Empty.Apply(envelopes).Get(tenant, completed.ProjectId!)!.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        ProjectReferenceIndexProjection.Empty.Apply(envelopes).List(tenant, completed.ProjectId!).ShouldHaveSingleItem().ReferenceId.ShouldBe(completed.FolderId);

        ProjectAuditTimelineItem auditItem = ProjectAuditTimelineProjection.Empty.Apply(envelopes).List(tenant, completed.ProjectId!, null).ShouldHaveSingleItem();
        auditItem.OperationType.ShouldBe("project.created");
        auditItem.ReferenceKind.ShouldBe("folder");
        auditItem.ReferenceId.ShouldBe(completed.FolderId);
        ProjectAuditFact fact = ProjectAuditTimelineProjection.ToSupportedFact(auditItem).ShouldNotBeNull();
        fact.Operation.ShouldBe("project.created");
        fact.Outcome.ShouldBe("Active");
        foldersA.DeleteAttemptCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Story", "7.1")]
    public async Task AbandonmentOnRedis_CommitsFenceBeforeReleaseRecordsAuditStaysInvisibleAndAllowsNextHolder()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string tenant = $"tenant-abn-{Guid.NewGuid():N}"[..24];
        string actor = "actor-abn";
        string folderId = $"fld-abn-{Guid.NewGuid():N}"[..24];

        SharedClock clock = new(BaseTime);
        MutableProjectCreationAuthorityChecker authority = new();
        FoldersAggregateProvisioningClient foldersA = new(fixture.ReplicaAStore, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        await foldersA.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(tenant, folderId, "Reusable Folder", actor), ct);

        ProjectCreationFaultOptions faultsA = new() { PauseAfterClaim = true };
        FolderFirstProjectCreationWorkflow replicaA = BuildWorkflow(fixture.ReplicaAStore, foldersA, authority, clock, faultsA);

        ProjectCreationSelectionEvidence ev1 = new("ev-abn-1", tenant, actor, "ui", folderId, BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationWorkflowResult paused = await replicaA.CreateProjectAsync(new(
            tenant, actor, ProjectCreationCallerClass.ProjectUser, "ui", "idem-abn-1", "Abandoned Project", SuppliedFolderId: folderId, SelectionEvidence: ev1), ct);
        paused.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        paused.FolderClaimGeneration.ShouldBe(1);

        // Identify the reserved ProjectId from the active claim on Redis.
        (ProjectFolderClaimState claimBefore, _) = await replicaA.RehydrateClaimStateAsync(tenant, folderId, ct);
        string abandonedProjectId = claimBefore.ActiveClaim!.ProjectId;

        // Replica B abandons the reservation after lease expiry!
        clock.Advance(TimeSpan.FromSeconds(35));
        FoldersAggregateProvisioningClient foldersB = new(fixture.ReplicaBStore, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        FolderFirstProjectCreationWorkflow replicaB = BuildWorkflow(fixture.ReplicaBStore, foldersB, authority, clock, new ProjectCreationFaultOptions());

        ProjectCreationWorkflowResult abandoned = await replicaB.AbandonReservationAsync(paused.TaskId!, "caller_abandoned", ct);
        abandoned.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);

        // Verify fence event precedes release in shared global positions on Redis!
        (ProjectSdkState abandonedState, StoredEventStream abandonedStream) = await replicaB.RehydrateProjectStateAsync(tenant, abandonedProjectId, ct);
        abandonedState.DomainState.IsCreationAbandoned.ShouldBeTrue();
        (ProjectFolderClaimState claimAfter, StoredEventStream claimStream) = await replicaB.RehydrateClaimStateAsync(tenant, folderId, ct);
        claimAfter.IsActive.ShouldBeFalse();
        abandonedStream.Events[0].GlobalPosition.ShouldBeLessThan(claimStream.Events[^1].GlobalPosition);

        // Verify Safe Metadata project.creation_abandoned audit fact!
        ProjectProjectionEnvelope abandonEnv = new(tenant, 1, (IProjectEvent)ReadModelBackedEventStore.DeserializeEvent(abandonedStream.Events[0]));
        ProjectAuditFact auditFact = ProjectAuditTimelineProjection.ToSupportedFact(abandonEnv).ShouldNotBeNull();
        auditFact.Operation.ShouldBe("project.creation_abandoned");
        auditFact.Outcome.ShouldBe("Abandoned");

        // Verify supported read model on Redis records IsCreationAbandoned == true and excludes Project from ProjectReadIndex!
        ReadModelEntry<ProjectReadModel> abandonedRm = await fixture.ReplicaAStore.GetAsync<ProjectReadModel>(
            DockerDaprRedisTwoReplicaFixture.StoreName,
            ProjectReadModelKeys.Detail(tenant, abandonedProjectId),
            ct);
        abandonedRm.Value.ShouldNotBeNull();
        abandonedRm.Value!.IsCreationAbandoned.ShouldBeTrue();
        abandonedRm.Value.Detail.ShouldBeNull();

        ReadModelEntry<ProjectReadIndex> index = await fixture.ReplicaAStore.GetAsync<ProjectReadIndex>(
            DockerDaprRedisTwoReplicaFixture.StoreName,
            ProjectReadModelKeys.Index(tenant),
            ct);
        index.Value.ShouldNotBeNull();
        index.Value!.ProjectIds.ShouldNotContain(abandonedProjectId);

        // A second Project can now claim the released Folder at Generation == 3 and succeed!
        ProjectCreationSelectionEvidence ev2 = new("ev-abn-2", tenant, actor, "ui", folderId, clock.GetUtcNow(), clock.GetUtcNow().AddMinutes(5));
        ProjectCreationWorkflowResult second = await replicaB.CreateProjectAsync(new(
            tenant, actor, ProjectCreationCallerClass.ProjectUser, "ui", "idem-abn-2", "Second Holder Project", SuppliedFolderId: folderId, SelectionEvidence: ev2), ct);
        second.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        second.FolderClaimGeneration.ShouldBe(3);
    }

    [Fact]
    [Trait("Story", "7.1")]
    public async Task IsolatedHostTestComposition_EnforcesSingleWriterAndAd17ProcessAsyncEquivalence()
    {
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(new SharedClock(BaseTime));
        services.AddProjectsSdkWriterTestComposition((_, _, _) => true);
        using ServiceProvider provider = services.BuildServiceProvider();

        SdkWriterHostComposition.AssertSingleWriterPerDomain(provider);

        IDomainProcessor projectsProcessor = provider.GetRequiredKeyedService<IDomainProcessor>(ProjectSdkAggregate.DomainName);
        string tenant = "tenant-ad17";
        string projectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AD";
        ProjectSdkState state = new();

        // Dispatch v1 CreateProject -> ArchiveProject -> RestoreProject through IDomainProcessor.ProcessAsync
        // and verify historical ProjectFolderCreationPending remains replayable.
        CommandEnvelope createEnv = new("m1", tenant, "projects", projectId, "Hexalith.Projects.Commands.CreateProject",
            JsonSerializer.SerializeToUtf8Bytes(new WireCreateProject("AD17 Project"), JsonOptions), "c1", null, "actor-1",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["taskId"] = "t1" });
        DomainResult created = await projectsProcessor.ProcessAsync(createEnv, state);
        created.IsSuccess.ShouldBeTrue();
        created.Events.Count.ShouldBe(2);
        created.Events[0].ShouldBeOfType<ProjectCreated>();
        created.Events[1].ShouldBeOfType<ProjectFolderCreationPending>();
        state.Apply((ProjectCreated)created.Events[0]);
        state.Apply((ProjectFolderCreationPending)created.Events[1]);

        CommandEnvelope archiveEnv = new("m2", tenant, "projects", projectId, "Hexalith.Projects.Commands.ArchiveProject",
            JsonSerializer.SerializeToUtf8Bytes(new WireArchiveProject("archive", "v1"), JsonOptions), "c2", null, "actor-1",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["taskId"] = "t2" });
        DomainResult archived = await projectsProcessor.ProcessAsync(archiveEnv, state);
        archived.IsSuccess.ShouldBeTrue();
        state.Apply(archived.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectArchived>());
        state.DomainState.Lifecycle.ShouldBe(ProjectLifecycle.Archived);

        CommandEnvelope restoreEnv = new("m3", tenant, "projects", projectId, "Hexalith.Projects.Commands.RestoreProject",
            JsonSerializer.SerializeToUtf8Bytes(new WireRestoreProject("v1", "restore"), JsonOptions), "c3", null, "actor-1",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["taskId"] = "t3" });
        DomainResult restored = await projectsProcessor.ProcessAsync(restoreEnv, state);
        restored.IsSuccess.ShouldBeTrue();
        state.Apply(restored.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectRestored>());
        state.DomainState.Lifecycle.ShouldBe(ProjectLifecycle.Active);
    }

    private static FolderFirstProjectCreationWorkflow BuildWorkflow(
        DaprReadModelStore store,
        IProjectFolderProvisioningClient folders,
        IProjectCreationAuthorityChecker authority,
        TimeProvider clock,
        ProjectCreationFaultOptions faults)
    {
        ReadModelBackedEventStore eventStore = new(store, DockerDaprRedisTwoReplicaFixture.StoreName, clock);
        return new FolderFirstProjectCreationWorkflow(
            store,
            store,
            DockerDaprRedisTwoReplicaFixture.StoreName,
            eventStore,
            new ProjectSdkAggregate(clock),
            new ProjectFolderClaimAggregate(clock),
            folders,
            authority,
            clock,
            leaseDuration: TimeSpan.FromSeconds(30),
            dependencyFailureBudget: 3,
            faultOptions: faults);
    }

    private sealed class SharedClock(DateTimeOffset startUtc) : TimeProvider
    {
        private long _ticks = startUtc.ToUniversalTime().Ticks;

        public void Advance(TimeSpan delta) => Interlocked.Add(ref _ticks, delta.Ticks);

        public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);
    }
}
#endif
