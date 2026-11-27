// <copyright file="Story71SdkAggregateAndReconstructorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Story71;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.SdkWriter.Admission;
using Hexalith.Projects.SdkWriter.Aggregates;
using Hexalith.Projects.SdkWriter.Claims;
using Hexalith.Projects.SdkWriter.Commands;
using Hexalith.Projects.SdkWriter.Composition;
using Hexalith.Projects.SdkWriter.Reconstruction;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

using WireArchiveProject = Hexalith.Projects.SdkWriter.Commands.ArchiveProject;
using WireConfirmProjectResolution = Hexalith.Projects.SdkWriter.Commands.ConfirmProjectResolution;
using WireCreateProject = Hexalith.Projects.SdkWriter.Commands.CreateProject;
using WireLinkFileReference = Hexalith.Projects.SdkWriter.Commands.LinkFileReference;
using WireLinkMemory = Hexalith.Projects.SdkWriter.Commands.LinkMemory;
using WireRestoreProject = Hexalith.Projects.SdkWriter.Commands.RestoreProject;
using WireSetProjectFolder = Hexalith.Projects.SdkWriter.Commands.SetProjectFolder;
using WireUnlinkFileReference = Hexalith.Projects.SdkWriter.Commands.UnlinkFileReference;
using WireUnlinkMemory = Hexalith.Projects.SdkWriter.Commands.UnlinkMemory;
using WireUpdateProjectSetup = Hexalith.Projects.SdkWriter.Commands.UpdateProjectSetup;

/// <summary>
/// Unconditional (runs in BOTH Debug source mode and <c>CI=true</c> Release package mode) tests for:
/// <see cref="ProjectSdkAggregate"/> &amp; <see cref="ProjectFolderClaimAggregate"/> <c>IDomainProcessor.ProcessAsync</c>
/// dispatch across all 14 + 3 commands, AD-17 equivalence, <see cref="SdkWriterHostComposition"/> single-writer fencing,
/// <see cref="LegacyProjectFolderClaimReconstructor"/> edge cases, and <see cref="ProjectCreationAdmissionEvaluator"/> P1 rules.
/// </summary>
public sealed class Story71SdkAggregateAndReconstructorTests
{
    private const string TenantA = "tenant-a";
    private const string ProjectIdA = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FolderIdA = "fld-p-1234567890abcdef12345678";
    private static readonly DateTimeOffset BaseTime = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ProjectSdkAggregate_ProcessAsync_DispatchesAll12RetainedV1CommandsAnd2Story71Commands()
    {
        Story71ManualTimeProvider clock = new(BaseTime);
        bool ownerAllowed = false;
        ProjectSdkAggregate aggregate = new(clock, (_, _, _) => ownerAllowed);
        IDomainProcessor processor = aggregate;
        ProjectSdkState state = new();

        // 1. CreateProject (v1)
        DomainResult createRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.CreateProject", new WireCreateProject("Alpha", "Desc", "ref-1"), "m1"),
            state);
        createRes.IsSuccess.ShouldBeTrue();
        createRes.Events.Count.ShouldBe(2);
        ApplyAll(state, createRes.Events);
        state.DomainState.IsCreated.ShouldBeTrue();
        state.DomainState.ProjectFolder!.ReferenceState.ShouldBe(ReferenceState.Pending);

        // 2. UpdateProjectSetup
        DomainResult setupRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.UpdateProjectSetup", new WireUpdateProjectSetup("v1", new ProjectSetup(["Ship 7.1"], [], [ProjectContextSourceKind.Conversation], [], null)), "m2"),
            state);
        setupRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, setupRes.Events);

        // 3. SetProjectFolder
        DomainResult folderRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.SetProjectFolder", new WireSetProjectFolder("v1", "set", ProjectIdA, FolderIdA, new("Alpha Folder"), false), "m3"),
            state);
        folderRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, folderRes.Events);

        // 4. LinkFileReference & 5. UnlinkFileReference
        DomainResult linkFileRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.LinkFileReference", new WireLinkFileReference("v1", "link", ProjectIdA, "file-1", FolderIdA, new("Spec.md")), "m4"),
            state);
        linkFileRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, linkFileRes.Events);

        DomainResult unlinkFileRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.UnlinkFileReference", new WireUnlinkFileReference("v1", "unlink", "removeReference", ProjectIdA, "file-1"), "m5"),
            state);
        unlinkFileRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, unlinkFileRes.Events);

        // 6. LinkMemory & 7. UnlinkMemory
        DomainResult linkMemRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.LinkMemory", new WireLinkMemory("v1", "link", ProjectIdA, "mem-1", new("Notes")), "m6"),
            state);
        linkMemRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, linkMemRes.Events);

        DomainResult unlinkMemRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.UnlinkMemory", new WireUnlinkMemory("v1", "unlink", "removeReference", ProjectIdA, "mem-1"), "m7"),
            state);
        unlinkMemRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, unlinkMemRes.Events);

        // 8. ConfirmProjectResolution
        DomainResult confirmRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.ConfirmProjectResolution", new WireConfirmProjectResolution("v1", "confirm", ProjectIdA, "conv-1", null, "MultipleCandidates"), "m8"),
            state);
        confirmRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, confirmRes.Events);

        // 9. GrantProjectChatBotAuthority — fails closed when owner delegate returns false (or null)!
        GrantProjectChatBotAuthority grantCmd = new("bot-1", "RecordNote", "ai", "Ai", "grant-1", BaseTime.AddHours(1));
        DomainResult deniedGrant = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Contracts.Commands.GrantProjectChatBotAuthority", grantCmd, "m9a"),
            state);
        deniedGrant.IsRejection.ShouldBeTrue();
        deniedGrant.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectChatBotAuthorityRejected>().ReasonCode.ShouldBe("owner_unavailable");

        // Null owner verifier delegate also fails closed.
        ProjectSdkAggregate noOwnerAgg = new(clock, isCurrentOwner: null);
        DomainResult nullOwnerGrant = await ((IDomainProcessor)noOwnerAgg).ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Contracts.Commands.GrantProjectChatBotAuthority", grantCmd, "m9b"),
            state);
        nullOwnerGrant.IsRejection.ShouldBeTrue();
        nullOwnerGrant.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectChatBotAuthorityRejected>().ReasonCode.ShouldBe("owner_unavailable");

        // Succeeds when owner delegate returns true.
        ownerAllowed = true;
        DomainResult okGrant = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Contracts.Commands.GrantProjectChatBotAuthority", grantCmd, "m9c"),
            state);
        okGrant.IsSuccess.ShouldBeTrue();
        ApplyAll(state, okGrant.Events);

        // 10. RevokeProjectChatBotAuthority
        RevokeProjectChatBotAuthority revokeCmd = new("bot-1", "RecordNote", "ai", "Ai", "grant-1");
        DomainResult okRevoke = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Contracts.Commands.RevokeProjectChatBotAuthority", revokeCmd, "m10"),
            state);
        okRevoke.IsSuccess.ShouldBeTrue();
        ApplyAll(state, okRevoke.Events);

        // 11. ArchiveProject & 12. RestoreProject
        DomainResult archiveRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.ArchiveProject", new WireArchiveProject("archive", "v1"), "m11"),
            state);
        archiveRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, archiveRes.Events);

        DomainResult restoreRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, "Hexalith.Projects.Commands.RestoreProject", new WireRestoreProject("v1", "restore"), "m12"),
            state);
        restoreRes.IsSuccess.ShouldBeTrue();
        ApplyAll(state, restoreRes.Events);
        state.DomainState.Lifecycle.ShouldBe(ProjectLifecycle.Active);

        // 13. CreateBoundProject trims FolderDisplayName & hashes Description/SetupMetadata.
        ProjectSdkState boundState = new();
        CreateBoundProject boundCmd = new("  Bound Name  ", "Safe desc", "setup-ref-1", FolderIdA, "  Trimmed Folder  ", "prov-1", 1, "task-1/step/3");
        DomainResult boundRes = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, nameof(CreateBoundProject), boundCmd, "m13"),
            boundState);
        boundRes.IsSuccess.ShouldBeTrue();
        ProjectCreated boundCreated = boundRes.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectCreated>();
        boundCreated.Name.ShouldBe("Bound Name");
        boundCreated.FolderDisplayName.ShouldBe("Trimmed Folder");
        ApplyAll(boundState, boundRes.Events);

        // ComputeBoundCreationFingerprint must change when Description or SetupMetadata changes.
        string fp1 = ProjectSdkAggregate.ComputeBoundCreationFingerprint("N", "DescA", "SetupA", FolderIdA, "F", "p1", 1, "t/step/3");
        string fp2 = ProjectSdkAggregate.ComputeBoundCreationFingerprint("N", "DescB", "SetupA", FolderIdA, "F", "p1", 1, "t/step/3");
        string fp3 = ProjectSdkAggregate.ComputeBoundCreationFingerprint("N", "DescA", "SetupB", FolderIdA, "F", "p1", 1, "t/step/3");
        fp1.ShouldNotBe(fp2);
        fp1.ShouldNotBe(fp3);

        // 14. AbandonProjectCreation on already-created state rejects; on fresh state succeeds.
        DomainResult abandonCreated = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, nameof(AbandonProjectCreation), new AbandonProjectCreation(FolderIdA, 1, "late", "t/abandon"), "m14a"),
            boundState);
        abandonCreated.IsRejection.ShouldBeTrue();

        ProjectSdkState freshState = new();
        DomainResult abandonFresh = await processor.ProcessAsync(
            Envelope("projects", ProjectIdA, nameof(AbandonProjectCreation), new AbandonProjectCreation(FolderIdA, 1, "cancelled", "t/abandon"), "m14b"),
            freshState);
        abandonFresh.IsSuccess.ShouldBeTrue();
        abandonFresh.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectCreationAbandoned>();

        // Wrong envelope.Domain fails closed!
        DomainResult wrongDomain = await processor.ProcessAsync(
            Envelope("wrong-domain", ProjectIdA, nameof(CreateBoundProject), boundCmd, "m15"),
            new ProjectSdkState());
        wrongDomain.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public void ProjectSdkState_RehydratesAll8RejectionTypesAndPreservesTenantMismatchGuard()
    {
        ProjectSdkState state = new();
        ProjectId pid = new(ProjectIdA);

        // Apply all 8 persisted rejection event types — Version increments every time.
        state.Apply(new ProjectCreationRejected(TenantA, ReferenceState.InvalidReference));
        state.Apply(new ProjectArchiveRejected(pid, TenantA, ReferenceState.Conflict));
        state.Apply(new ProjectRestoreRejected(pid, TenantA, ReferenceState.Conflict));
        state.Apply(new ProjectSetupUpdateRejected(pid, TenantA, ReferenceState.InvalidReference));
        state.Apply(new ProjectReferenceLinkRejected(pid, TenantA, "folder", FolderIdA, ReferenceState.Conflict));
        state.Apply(new ProjectReferenceUnlinkRejected(pid, TenantA, "file", "f1", ReferenceState.Conflict));
        state.Apply(new ProjectResolutionConfirmationRejected(pid, TenantA, ReferenceState.Conflict));
        state.Apply(new ProjectChatBotAuthorityRejected("owner_unavailable"));
        state.Version.ShouldBe(8);

        // Create under tenant-a, then attempt to apply a tenant-b event -> must throw TenantMismatch!
        state.Apply(new ProjectCreated(TenantA, ProjectIdA, "Alpha", null, null, ProjectLifecycle.Active, "a1", "c1", "t1", "k1", "f1", BaseTime));
        state.Version.ShouldBe(9);
        Should.Throw<InvalidOperationException>(() => state.Apply(
            new ProjectArchived("tenant-b", ProjectIdA, ProjectLifecycle.Archived, "a1", "c2", "t2", "k2", "f2", BaseTime.AddSeconds(1))));
    }

    [Fact]
    public async Task ProjectFolderClaimAggregate_ProcessAsync_EnforcesDomainGenerationRetryAndStaleReleaseFencing()
    {
        Story71ManualTimeProvider clock = new(BaseTime);
        ProjectFolderClaimAggregate agg = new(clock);
        IDomainProcessor processor = agg;
        ProjectFolderClaimState state = new();

        // Wrong domain fails closed.
        DomainResult wrongDomain = await processor.ProcessAsync(
            Envelope("projects", FolderIdA, nameof(AcquireProjectFolderClaim), new AcquireProjectFolderClaim(ProjectIdA, "t1", "t1/step/2"), "c0"),
            state);
        wrongDomain.IsRejection.ShouldBeTrue();

        // Acquire generation 1.
        DomainResult acq1 = await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(AcquireProjectFolderClaim), new AcquireProjectFolderClaim(ProjectIdA, "t1", "t1/step/2"), "c1"),
            state);
        acq1.IsSuccess.ShouldBeTrue();
        ProjectFolderClaimAcquired ev1 = acq1.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectFolderClaimAcquired>();
        ev1.Generation.ShouldBe(1);
        state.Apply(ev1);

        // Active identical retry returns NoOp.
        (await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(AcquireProjectFolderClaim), new AcquireProjectFolderClaim(ProjectIdA, "t1", "t1/step/2"), "c1"),
            state)).IsNoOp.ShouldBeTrue();

        // Release generation 1.
        DomainResult rel1 = await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(ReleaseProjectFolderClaim), new ReleaseProjectFolderClaim(ProjectIdA, 1, "t1", "t1/release", "abandoned"), "c2"),
            state);
        rel1.IsSuccess.ShouldBeTrue();
        state.Apply(rel1.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectFolderClaimReleased>());

        // Identical release retry -> NoOp; conflicting (ProjectId, ExpectedGeneration) on same step -> Rejection!
        (await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(ReleaseProjectFolderClaim), new ReleaseProjectFolderClaim(ProjectIdA, 1, "t1", "t1/release", "abandoned"), "c2"),
            state)).IsNoOp.ShouldBeTrue();
        (await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(ReleaseProjectFolderClaim), new ReleaseProjectFolderClaim(ProjectIdA, 99, "t1", "t1/release", "abandoned"), "c2b"),
            state)).IsRejection.ShouldBeTrue();

        // Retry of previously released Acquire MUST reject (never return NoOp!).
        (await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(AcquireProjectFolderClaim), new AcquireProjectFolderClaim(ProjectIdA, "t1", "t1/step/2"), "c1-late"),
            state)).IsRejection.ShouldBeTrue();

        // Second Project acquires generation 3.
        string projectIdB = "01HZ9K8YQ3W6V2N4R7T5P0X1AC";
        DomainResult acq2 = await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(AcquireProjectFolderClaim), new AcquireProjectFolderClaim(projectIdB, "t2", "t2/step/2"), "c3"),
            state);
        acq2.IsSuccess.ShouldBeTrue();
        ProjectFolderClaimAcquired ev2 = acq2.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectFolderClaimAcquired>();
        ev2.Generation.ShouldBe(3);
        state.Apply(ev2);

        // Stale release for generation 1 leaves later holder (ProjectB, gen 3) intact!
        DomainResult staleRel = await processor.ProcessAsync(
            Envelope("project-folder-claims", FolderIdA, nameof(ReleaseProjectFolderClaim), new ReleaseProjectFolderClaim(ProjectIdA, 1, "t1-stale", "t1-stale/release", "stale"), "c4"),
            state);
        staleRel.IsRejection.ShouldBeTrue();
        state.IsActive.ShouldBeTrue();
        state.ActiveClaim!.ProjectId.ShouldBe(projectIdB);
        state.ActiveClaim.Generation.ShouldBe(3);
    }

    [Fact]
    public void SdkWriterHostComposition_ReplacesPreExistingKeyedWritersAndAssertsSingleWriterPerDomain()
    {
        ServiceCollection services = new();
        // Register duplicate pre-existing keyed writers.
        services.AddKeyedSingleton<IDomainProcessor>("projects", new ProjectSdkAggregate());
        services.AddKeyedSingleton<IAsyncAggregateReplay>("projects", new ProjectSdkAggregate());
        services.AddKeyedSingleton<IDomainProcessor>("project-folder-claims", new ProjectFolderClaimAggregate());

        services.AddProjectsSdkWriterTestComposition();
        using ServiceProvider provider = services.BuildServiceProvider();
        Should.NotThrow(() => SdkWriterHostComposition.AssertSingleWriterPerDomain(provider));

        // Adding a second writer after composition must trigger AssertSingleWriterPerDomain failure.
        services.AddKeyedSingleton<IDomainProcessor>("projects", new ProjectSdkAggregate());
        using ServiceProvider duplicateProvider = services.BuildServiceProvider();
        Should.Throw<InvalidOperationException>(() => SdkWriterHostComposition.AssertSingleWriterPerDomain(duplicateProvider));
    }

    [Fact]
    public void LegacyProjectFolderClaimReconstructor_HandlesIncumbentsConflictsIncompleteCorruptStaleAndColonTenants()
    {
        string tenant = "tenant-legacy";
        string pIncumbent = "01HZ9K8YQ3W6V2N4R7T5P0X1A1";
        string pConflict1 = "01HZ9K8YQ3W6V2N4R7T5P0X1A2";
        string pConflict2 = "01HZ9K8YQ3W6V2N4R7T5P0X1A3";
        string pIncomplete = "01HZ9K8YQ3W6V2N4R7T5P0X1A4";
        string pArchived = "01HZ9K8YQ3W6V2N4R7T5P0X1A5";
        string pPending = "01HZ9K8YQ3W6V2N4R7T5P0X1A6";
        string pCorrupt = "01HZ9K8YQ3W6V2N4R7T5P0X1A7";
        string pAbsent = "01HZ9K8YQ3W6V2N4R7T5P0X1A9";

        string fIncumbent = "fld-incumbent";
        string fConflict = "fld-conflict";
        string fIncomplete = "fld-incomplete";
        string fCorrupt = "fld-corrupt";
        string fStaleArchived = "fld-stale-archived";
        string fStaleAbsent = "fld-stale-absent";
        string fPreClosed = "fld-pre-closed";
        string fFree = "fld-free";

        // Build pre-existing claim states.
        ProjectFolderClaimState staleArchivedClaim = new();
        staleArchivedClaim.Apply(new ProjectFolderClaimAcquired(tenant, fStaleArchived, pArchived, 1, "t1", "t1/step/2", "a", "c", "r1", BaseTime));

        ProjectFolderClaimState staleAbsentClaim = new();
        staleAbsentClaim.Apply(new ProjectFolderClaimAcquired(tenant, fStaleAbsent, pAbsent, 1, "t2", "t2/step/2", "a", "c", "r2", BaseTime));

        ProjectFolderClaimState preClosedClaim = new();
        preClosedClaim.Apply(new ProjectFolderClaimClosed(tenant, fPreClosed, "prior_quarantine", null, null, "a", "c", BaseTime));

        LegacyProjectHistorySnapshot incumbentSnap = new(tenant, pIncumbent, [BoundCreated(tenant, pIncumbent, fIncumbent)]);
        LegacyProjectHistorySnapshot[] snapshots =
        [
            incumbentSnap,
            incumbentSnap, // Duplicate identical snapshot must NOT be treated as conflict!
            new(tenant, pConflict1, [BoundCreated(tenant, pConflict1, fConflict)]),
            new(tenant, pConflict2, [BoundCreated(tenant, pConflict2, fConflict)]),
            new(tenant, pIncomplete, [new ProjectFolderSet(tenant, pIncomplete, fIncomplete, new("F"), "a", "c", "t", "k", "f", BaseTime)], IsIncompleteHistory: true),
            new(tenant, pCorrupt, [
                BoundCreated(tenant, pCorrupt, fCorrupt),
                BoundCreated(tenant, pCorrupt, fCorrupt) with { IdempotencyKey = "k-dup-2" },
            ]), // Corrupt duplicate ProjectCreated -> quarantined + Folder closed
            new(tenant, pArchived, [BoundCreated(tenant, pArchived, fStaleArchived), new ProjectArchived(tenant, pArchived, ProjectLifecycle.Archived, "a", "c", "t", "ka", "fa", BaseTime.AddSeconds(1))]),
            new(tenant, pPending, [
                new ProjectCreated(tenant, pPending, "Legacy Pending", null, null, ProjectLifecycle.Active, "a", "c", "t", "kp1", "fp1", BaseTime),
                new ProjectFolderCreationPending(tenant, pPending, "Legacy Pending", "folder_create_external_unavailable", true, "a", "c", "t", "kp2", "fp2", BaseTime),
            ]),
        ];

        Dictionary<string, ProjectFolderClaimState> existingClaims = new(StringComparer.Ordinal)
        {
            [LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fStaleArchived)] = staleArchivedClaim,
            [LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fStaleAbsent)] = staleAbsentClaim,
            [LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fPreClosed)] = preClosedClaim,
        };

        LegacyProjectFolderClaimPlan plan = LegacyProjectFolderClaimReconstructor.Reconstruct(
            snapshots,
            [
                (tenant, fIncumbent),
                (tenant, fConflict),
                (tenant, fIncomplete),
                (tenant, fCorrupt),
                (tenant, fStaleArchived),
                (tenant, fStaleAbsent),
                (tenant, fPreClosed),
                (tenant, fFree),
            ],
            existingClaims);

        // Length-prefixed keys prevent colon ambiguity.
        LegacyProjectFolderClaimReconstructor.FolderKey("a:b", "c").ShouldNotBe(LegacyProjectFolderClaimReconstructor.FolderKey("a", "b:c"));
        LegacyProjectFolderClaimReconstructor.ProjectKey("a:b", "c").ShouldNotBe(LegacyProjectFolderClaimReconstructor.ProjectKey("a", "b:c"));

        plan.IncumbentClaims[LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fIncumbent)].ShouldBe(pIncumbent);
        plan.IncumbentClaims.ShouldNotContainKey(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fStaleArchived));
        plan.IncumbentClaims.ShouldNotContainKey(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fStaleAbsent));

        plan.ClosedFolderKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fConflict));
        plan.ClosedFolderKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fIncomplete));
        plan.ClosedFolderKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fCorrupt));
        plan.ClosedFolderKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fStaleArchived));
        plan.ClosedFolderKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fStaleAbsent));
        plan.ClosedFolderKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fPreClosed));

        plan.QuarantinedProjectKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.ProjectKey(tenant, pConflict1));
        plan.QuarantinedProjectKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.ProjectKey(tenant, pConflict2));
        plan.QuarantinedProjectKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.ProjectKey(tenant, pIncomplete));
        plan.QuarantinedProjectKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.ProjectKey(tenant, pCorrupt));
        plan.QuarantinedProjectKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.ProjectKey(tenant, pArchived));

        plan.FreeFolderKeys.ShouldBe([LegacyProjectFolderClaimReconstructor.FolderKey(tenant, fFree)]);
        plan.LegacyPendingFolderProjectKeys.ShouldContain(LegacyProjectFolderClaimReconstructor.ProjectKey(tenant, pPending));
    }

    [Fact]
    public void ProjectCreationAdmissionEvaluator_EnforcesAllowlistP1SchemaRecursiveDuplicateKeysAndSelectionEvidence()
    {
        // 1. Allowlist personas (unknown enum value, TenantOperator, TenantProjectAdministrator, WorkloadIdentity -> Denied)
        ProjectCreationAdmissionEvaluator.Evaluate(Req(callerClass: (ProjectCreationCallerClass)99), BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Denied);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(callerClass: ProjectCreationCallerClass.TenantOperator), BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Denied);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(callerClass: ProjectCreationCallerClass.TenantProjectAdministrator), BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Denied);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(callerClass: ProjectCreationCallerClass.WorkloadIdentity), BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Denied);

        // 2. Oversized TenantId/ActorPrincipalId (>256) -> Denied; blank Surface -> 400 ValidationFailure; invalid IdempotencyKey -> 400
        ProjectCreationAdmissionEvaluator.Evaluate(Req(tenantId: new string('a', 257)), BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Denied);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(actorId: new string('a', 257)), BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Denied);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(surface: "  "), BaseTime).RejectedField.ShouldBe("surface");
        ProjectCreationAdmissionEvaluator.Evaluate(Req(idempotencyKey: "bad\nkey"), BaseTime).RejectedField.ShouldBe("idempotency_key");

        // 3. P1 schema rules: v1 name-only accepted; null schema + MetadataClass rejected; v1 without projectMetadata rejected; displayName mismatch rejected
        ProjectCreationAdmissionEvaluator.Evaluate(Req(name: "Legacy NameOnly"), BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Admitted);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(requestSchemaVersion: null, metadataClass: "public_metadata"), BaseTime).RejectedField.ShouldBe("requestSchemaVersion");
        ProjectCreationAdmissionEvaluator.Evaluate(Req(rawBodyJson: """{"requestSchemaVersion":"v1","name":"Missing PM"}"""), BaseTime).RejectedField.ShouldBe("projectMetadata");
        ProjectCreationAdmissionEvaluator.Evaluate(Req(rawBodyJson: """{"requestSchemaVersion":"v1","name":"Top","projectMetadata":{"displayName":"Different","metadataClass":"public_metadata"}}"""), BaseTime).RejectedField.ShouldBe("name");

        // 4. Recursive duplicate JSON keys in nested objects and arrays of objects -> 400 ValidationFailure
        ProjectCreationAdmissionEvaluator.Evaluate(
            Req(rawBodyJson: """{"name":"Alpha","extra":{"nested":{"dup":1,"dup":2}}}"""),
            BaseTime).RejectedField.ShouldBe("body");
        ProjectCreationAdmissionEvaluator.Evaluate(
            Req(rawBodyJson: """{"name":"Alpha","items":[{"a":1,"a":2}]}"""),
            BaseTime).RejectedField.ShouldBe("body");

        // 5. Forbidden metadata does NOT echo secret value
        ProjectCreationAdmissionDecision secretFail = ProjectCreationAdmissionEvaluator.Evaluate(Req(name: "my-secret-value-xyz"), BaseTime);
        secretFail.Outcome.ShouldBe(ProjectCreationAdmissionOutcome.ValidationFailure);
        secretFail.HttpStatusCode.ShouldBe(400);
        secretFail.RejectedField.ShouldBe("name");
        (secretFail.ReasonCode ?? string.Empty).ShouldNotContain("my-secret-value-xyz");

        // 6. Service/Workflow Caller supplied Folder -> Denied; ProjectUser without/expired Selection Evidence -> RequestPreview; valid -> Admitted
        ProjectCreationAdmissionEvaluator.Evaluate(
            Req(callerClass: ProjectCreationCallerClass.ServiceWorkflowCaller, suppliedFolderId: FolderIdA),
            BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Denied);

        ProjectCreationAdmissionEvaluator.Evaluate(
            Req(suppliedFolderId: FolderIdA, selectionEvidence: null),
            BaseTime).Outcome.ShouldBe(ProjectCreationAdmissionOutcome.RequestPreview);

        ProjectCreationSelectionEvidence validEvidence = new("ev-1", TenantA, "actor-1", "ui", FolderIdA, BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationAdmissionEvaluator.Evaluate(Req(suppliedFolderId: FolderIdA, selectionEvidence: validEvidence), BaseTime.AddMinutes(2))
            .Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Admitted);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(suppliedFolderId: FolderIdA, selectionEvidence: validEvidence), BaseTime.AddMinutes(6))
            .Outcome.ShouldBe(ProjectCreationAdmissionOutcome.RequestPreview);
        ProjectCreationAdmissionEvaluator.Evaluate(Req(suppliedFolderId: FolderIdA, selectionEvidence: validEvidence), BaseTime.AddMinutes(6), skipSelectionEvidenceExpiryCheck: true)
            .Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Admitted);

        // 5. Additional hardening checks: MetadataClass in RequestSha256, whitespace SuppliedFolderId,
        //    empty requestSchemaVersion in RawBodyJson, unknown JSON properties, and omitted-field reset.
        ProjectCreationAdmissionDecision tierLow = ProjectCreationAdmissionEvaluator.Evaluate(
            Req(requestSchemaVersion: "v1", metadataClass: "public_metadata"), BaseTime);
        ProjectCreationAdmissionDecision tierHigh = ProjectCreationAdmissionEvaluator.Evaluate(
            Req(requestSchemaVersion: "v1", metadataClass: "tenant_sensitive"), BaseTime);
        tierLow.Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Admitted);
        tierHigh.Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Admitted);
        tierLow.RequestSha256.ShouldNotBe(tierHigh.RequestSha256);

        ProjectCreationAdmissionEvaluator.Evaluate(Req(suppliedFolderId: "   "), BaseTime)
            .ShouldSatisfyAllConditions(
                d => d.Outcome.ShouldBe(ProjectCreationAdmissionOutcome.ValidationFailure),
                d => d.RejectedField.ShouldBe("suppliedFolderId"));

        ProjectCreationAdmissionEvaluator.Evaluate(
            Req(rawBodyJson: """{"requestSchemaVersion":"","name":"Alpha"}"""), BaseTime)
            .ShouldSatisfyAllConditions(
                d => d.Outcome.ShouldBe(ProjectCreationAdmissionOutcome.ValidationFailure),
                d => d.RejectedField.ShouldBe("requestSchemaVersion"));

        ProjectCreationAdmissionEvaluator.Evaluate(
            Req(rawBodyJson: """{"name":"Alpha","unknownProp":1}"""), BaseTime)
            .Outcome.ShouldBe(ProjectCreationAdmissionOutcome.ValidationFailure);

        ProjectCreationAdmissionEvaluator.Evaluate(
            Req(rawBodyJson: """{"requestSchemaVersion":"v1","projectMetadata":{"metadataClass":"tenant_sensitive","displayName":"Alpha","extra":true}}"""), BaseTime)
            .Outcome.ShouldBe(ProjectCreationAdmissionOutcome.ValidationFailure);

        ProjectCreationAdmissionDecision rawReset = ProjectCreationAdmissionEvaluator.Evaluate(
            new ProjectCreationRequest(
                TenantA, "actor-1", ProjectCreationCallerClass.ProjectUser, "ui", "idem-reset",
                "Typed Name", Description: "Leaked Desc", SetupMetadata: "Leaked Setup", SuppliedFolderId: "fld-leaked",
                RawBodyJson: """{"name":"Raw Only"}"""),
            BaseTime);
        rawReset.Outcome.ShouldBe(ProjectCreationAdmissionOutcome.Admitted);
        rawReset.CanonicalName.ShouldBe("Raw Only");
        rawReset.CanonicalDescription.ShouldBeNull();
        rawReset.CanonicalSetupMetadata.ShouldBeNull();
        rawReset.SuppliedFolderId.ShouldBeNull();
    }

    [Fact]
    public async Task ProjectFolderClaimAggregate_CloseAndGuards_AndSdkAggregateIdentityGuards()
    {
        const string ProjectIdB = "01HZ9K8YQ3W6V2N4R7T5P0X1CD";
        IDomainProcessor claimProcessor = new ProjectFolderClaimAggregate(new Story71ManualTimeProvider(BaseTime));
        ProjectFolderClaimState claimState = new();

        // Acquire -> Close via ProcessAsync -> post-close Acquire and Release MUST reject; repeat Close is NoOp.
        DomainResult acq = await claimProcessor.ProcessAsync(
            Envelope(ProjectFolderClaimAggregate.DomainName, FolderIdA, nameof(AcquireProjectFolderClaim),
                new AcquireProjectFolderClaim(ProjectIdA, "t1", "t1/step/2"), "m-acq"),
            claimState);
        acq.IsSuccess.ShouldBeTrue();
        claimState.Apply(acq.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectFolderClaimAcquired>());

        // Foreign tenant/folder command against populated claimState must reject.
        CommandEnvelope foreignEnv = Envelope(ProjectFolderClaimAggregate.DomainName, FolderIdA, nameof(AcquireProjectFolderClaim),
            new AcquireProjectFolderClaim(ProjectIdB, "t-for", "t-for/step/2"), "m-for") with { TenantId = "tenant-foreign" };
        (await claimProcessor.ProcessAsync(foreignEnv, claimState)).IsRejection.ShouldBeTrue();

        // Direct Apply invariant guard rejects double-acquire or wrong generation.
        Should.Throw<InvalidOperationException>(() => claimState.Apply(
            new ProjectFolderClaimAcquired(TenantA, FolderIdA, ProjectIdB, 2, "t2", "t2/step/2", "a", "c", "r", BaseTime)));

        DomainResult closed = await claimProcessor.ProcessAsync(
            Envelope(ProjectFolderClaimAggregate.DomainName, FolderIdA, nameof(CloseProjectFolderClaim),
                new CloseProjectFolderClaim("reconstructor_quarantine", "t-close", "t-close/close"), "m-close"),
            claimState);
        closed.IsSuccess.ShouldBeTrue();
        claimState.Apply(closed.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectFolderClaimClosed>());
        claimState.IsClosed.ShouldBeTrue();
        claimState.IsActive.ShouldBeFalse();

        // Idempotent close -> NoOp; post-close Acquire and Release -> Rejection.
        (await claimProcessor.ProcessAsync(
            Envelope(ProjectFolderClaimAggregate.DomainName, FolderIdA, nameof(CloseProjectFolderClaim),
                new CloseProjectFolderClaim("reconstructor_quarantine", "t-close", "t-close/close"), "m-close-2"),
            claimState)).IsNoOp.ShouldBeTrue();

        (await claimProcessor.ProcessAsync(
            Envelope(ProjectFolderClaimAggregate.DomainName, FolderIdA, nameof(AcquireProjectFolderClaim),
                new AcquireProjectFolderClaim(ProjectIdB, "t3", "t3/step/2"), "m-acq-after-close"),
            claimState)).IsRejection.ShouldBeTrue();

        (await claimProcessor.ProcessAsync(
            Envelope(ProjectFolderClaimAggregate.DomainName, FolderIdA, nameof(ReleaseProjectFolderClaim),
                new ReleaseProjectFolderClaim(ProjectIdA, 1, "t1", "t1/release", "caller_cancelled"), "m-rel-after-close"),
            claimState)).IsRejection.ShouldBeTrue();

        // ProjectSdkAggregate: TenantMismatch before idempotency, distinct second AbandonProjectCreation rejects,
        // and ProjectSdkState.Apply throws on foreign rejection events once DomainState is initialized.
        IDomainProcessor projProcessor = new ProjectSdkAggregate(new Story71ManualTimeProvider(BaseTime));
        ProjectSdkState projState = new();
        DomainResult abn1 = await projProcessor.ProcessAsync(
            Envelope("projects", ProjectIdA, nameof(AbandonProjectCreation),
                new AbandonProjectCreation(FolderIdA, 1, "reason_one", "t1/abandon"), "m-abn-1"),
            projState);
        abn1.IsSuccess.ShouldBeTrue();
        projState.Apply(abn1.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectCreationAbandoned>());

        // Distinct second AbandonProjectCreation rejects with CreationAbandoned (not NoOp!).
        DomainResult abn2 = await projProcessor.ProcessAsync(
            Envelope("projects", ProjectIdA, nameof(AbandonProjectCreation),
                new AbandonProjectCreation(FolderIdA, 1, "reason_two", "t2/abandon"), "m-abn-2"),
            projState);
        abn2.IsRejection.ShouldBeTrue();
        abn2.Events.ShouldHaveSingleItem().ShouldBeOfType<ProjectCreationRejected>().Reason.ShouldBe(ReferenceState.Conflict);

        // Foreign tenant reusing same MessageId must hit TenantMismatch (NOT NoOp!).
        CommandEnvelope foreignReplay = Envelope("projects", ProjectIdA, nameof(AbandonProjectCreation),
            new AbandonProjectCreation(FolderIdA, 1, "reason_one", "t1/abandon"), "m-abn-1") with { TenantId = "tenant-foreign" };
        (await projProcessor.ProcessAsync(foreignReplay, projState)).IsRejection.ShouldBeTrue();

        // Foreign rejection event applied to initialized ProjectSdkState must throw.
        Should.Throw<InvalidOperationException>(() => projState.Apply(
            new ProjectCreationRejected("tenant-foreign", ReferenceState.Conflict, null, "c", new ProjectId(ProjectIdA))));
    }

    private static ProjectCreationRequest Req(
        string tenantId = TenantA,
        string actorId = "actor-1",
        ProjectCreationCallerClass callerClass = ProjectCreationCallerClass.ProjectUser,
        string surface = "ui",
        string idempotencyKey = "idem-1",
        string? name = "Alpha Project",
        string? requestSchemaVersion = null,
        string? metadataClass = null,
        string? suppliedFolderId = null,
        ProjectCreationSelectionEvidence? selectionEvidence = null,
        string? rawBodyJson = null)
        => new(tenantId, actorId, callerClass, surface, idempotencyKey, name, null, null, requestSchemaVersion, metadataClass, suppliedFolderId, selectionEvidence, rawBodyJson);

    private static ProjectCreated BoundCreated(string tenantId, string projectId, string folderId)
        => new(tenantId, projectId, "Proj", null, null, ProjectLifecycle.Active, "actor", "corr", "task", "idem", "fp", BaseTime)
        {
            FolderId = folderId,
            FolderDisplayName = "Folder",
            FolderClaimGeneration = 1,
        };

    private static CommandEnvelope Envelope<T>(string domain, string aggregateId, string commandType, T payload, string messageId)
        => new(
            MessageId: messageId,
            TenantId: TenantA,
            Domain: domain,
            AggregateId: aggregateId,
            CommandType: commandType,
            Payload: JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions),
            CorrelationId: "corr-1",
            CausationId: null,
            UserId: "actor-1",
            Extensions: new Dictionary<string, string>(StringComparer.Ordinal) { ["taskId"] = "task-1" });

    private static void ApplyAll(ProjectSdkState state, IReadOnlyList<IEventPayload> events)
    {
        foreach (IEventPayload ev in events)
        {
            switch (ev)
            {
                case ProjectCreated e: state.Apply(e); break;
                case ProjectCreationAbandoned e: state.Apply(e); break;
                case ProjectSetupUpdated e: state.Apply(e); break;
                case ProjectFolderSet e: state.Apply(e); break;
                case ProjectFolderCreationPending e: state.Apply(e); break;
                case FileReferenceLinked e: state.Apply(e); break;
                case FileReferenceUnlinked e: state.Apply(e); break;
                case MemoryLinked e: state.Apply(e); break;
                case MemoryUnlinked e: state.Apply(e); break;
                case ProjectArchived e: state.Apply(e); break;
                case ProjectRestored e: state.Apply(e); break;
                case ProjectResolutionConfirmed e: state.Apply(e); break;
                case ProjectChatBotAuthorityGranted e: state.Apply(e); break;
                case ProjectChatBotAuthorityRevoked e: state.Apply(e); break;
            }
        }
    }
}
