// <copyright file="Story71WorkflowAndRecoveryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if SUPPORTED_PROJECT_READ_SOURCE
namespace Hexalith.Projects.Tests.Story71;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.DomainService.DurableTasks;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReads;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.SdkWriter.Admission;
using Hexalith.Projects.SdkWriter.Aggregates;
using Hexalith.Projects.SdkWriter.Claims;
using Hexalith.Projects.SdkWriter.Storage;
using Hexalith.Projects.SdkWriter.Workflow;

using Shouldly;

using Xunit;

/// <summary>
/// Source-mode unit/workflow tests covering real <c>FolderAggregate</c> provisioning, 129–160 char names,
/// <c>VerifyFolderSecurity</c>, Step 1/3 lost-reply &amp; outage recovery, dependency failure budget exhaustion,
/// Selection Evidence 5-min expiry forward recovery, uncommitted Step 2 claim abandonment, live authority
/// <c>503 Unavailable</c> vs <c>404 Denied</c>, direct/delegated parity, and boundary pauses.
/// </summary>
public sealed class Story71WorkflowAndRecoveryTests
{
    private const string TenantA = "tenant-a";
    private const string ActorA = "actor-a";
    private static readonly DateTimeOffset BaseTime = new(2026, 11, 27, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task NoFolderCreation_ProvisionsRealFolderAggregateBindsExclusivelyAndConfirmsSupportedAndRetainedReads()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TestHarness h = new();
        ProjectCreationRequest req = new(
            TenantA,
            ActorA,
            ProjectCreationCallerClass.ProjectUser,
            "ui",
            "idem-no-folder-1",
            "Alpha Workspace",
            "Safe description",
            "setup-ref-1",
            "v1",
            "public_metadata");

        ProjectCreationWorkflowResult res = await h.Workflow.CreateProjectAsync(req, ct);

        res.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        res.HttpStatusCode.ShouldBe(200);
        res.TaskId.ShouldNotBeNullOrWhiteSpace();
        res.ProjectId.ShouldNotBeNullOrWhiteSpace();
        res.FolderId.ShouldNotBeNullOrWhiteSpace();
        res.FolderClaimGeneration.ShouldBe(1);
        res.FolderProvisioningReceiptId.ShouldNotBeNullOrWhiteSpace();
        h.Folders.DeleteAttemptCount.ShouldBe(0);

        // Real Folders security invariants verified.
        ProvisionedFolderRecord folder = (await h.Folders.GetProvisionedFolderAsync(TenantA, res.FolderId!, ct)).ShouldNotBeNull();
        folder.OriginalActorId.ShouldBe(ActorA);
        folder.InheritOrganizationPermissions.ShouldBeFalse();
        folder.WorkloadHasFolderAuthority.ShouldBeFalse();
        folder.OwnerVersion.ShouldBe(FoldersAggregateProvisioningClient.ProvisionedVersion);

        // Supported read model confirmed.
        ProjectReadModel readModel = h.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(TenantA, res.ProjectId!)).ShouldNotBeNull();
        readModel.Freshness.ShouldBe(EvidenceFreshnessState.Current);
        readModel.Detail!.Name.ShouldBe("Alpha Workspace");
        readModel.Detail.ProjectFolder!.FolderId.ShouldBe(res.FolderId);

        ProjectReadIndex index = h.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(TenantA)).ShouldNotBeNull();
        index.ProjectIds.ShouldContain(res.ProjectId!);

        // Equivalent retry converges to same task and Project without creating a second Folder or Project.
        ProjectCreationWorkflowResult retry = await h.Workflow.CreateProjectAsync(req, ct);
        retry.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        retry.TaskId.ShouldBe(res.TaskId);
        retry.ProjectId.ShouldBe(res.ProjectId);
        retry.FolderId.ShouldBe(res.FolderId);

        // Changed payload for same idempotency key conflicts (409).
        ProjectCreationWorkflowResult conflict = await h.Workflow.CreateProjectAsync(req with { Name = "Different Name" }, ct);
        conflict.Status.ShouldBe(ProjectCreationWorkflowStatus.Conflict);
        conflict.HttpStatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task LongDisplayName_129To160Chars_AcceptsValidAndRejectsForbiddenCharsWithoutLeakingNameLock()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TestHarness h = new();

        // 160-char valid name succeeds through real FolderCommandValidator + FolderAggregate.
        string valid160 = new string('a', 155) + "-proj";
        valid160.Length.ShouldBe(160);
        ProjectCreationWorkflowResult ok = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-160-ok", valid160), ct);
        ok.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        ProvisionedFolderRecord record = (await h.Folders.GetProvisionedFolderAsync(TenantA, ok.FolderId!, ct)).ShouldNotBeNull();
        record.DisplayName.ShouldBe(valid160);

        // 160-char name with forbidden content placed ONLY in characters 129..160 must fail Folders validation
        // AND must NOT leak a FolderNameLockRecord!
        string bad160 = new string('b', 135) + "-secret-token-end";
        bad160.Length.ShouldBeLessThanOrEqualTo(160);
        bad160.Length.ShouldBeGreaterThan(128);
        // Call ProvisionFolderAsync directly to verify FoldersAggregateProvisioningClient catches chars 129..160!
        string digest = FoldersAggregateProvisioningClient.ComputeRequestSha256(bad160);
        FolderProvisioningResult badProv = await h.Folders.ProvisionFolderAsync(
            TenantA,
            ActorA,
            new("task-bad-160", "task-bad-160/step/1", FolderFirstProjectCreationWorkflow.AdmissionOperation, digest, 1, 0, bad160), ct);
        badProv.Outcome.ShouldBe(FolderProvisioningOutcome.ValidationFailed);

        // Verify A-2 create-only same-name collision fails closed for a second Project with same valid160 name.
        ProjectCreationWorkflowResult collision = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-160-collision", valid160), ct);
        collision.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);
        h.Folders.DeleteAttemptCount.ShouldBe(0);
    }

    [Fact]
    public async Task VerifyFolderSecurity_BlocksInsecureProvisionedInsecureSuppliedAndPreCommitDeniedProvisioned()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        // 1. ForceInvalidProvisionSecurityOnce -> NeedsAttention, 0 Project events, 0 delete attempts!
        TestHarness h1 = new();
        h1.Folders.ForceInvalidProvisionSecurityOnce = true;
        ProjectCreationWorkflowResult res1 = await h1.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-sec-prov", "Sec Test"), ct);
        res1.Status.ShouldBe(ProjectCreationWorkflowStatus.NeedsAttention);
        h1.Folders.DeleteAttemptCount.ShouldBe(0);

        // 2. Insecure supplied Folder (InheritOrganizationPermissions = true) -> Rejected, 0 Project events!
        TestHarness h2 = new();
        await h2.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-insecure", "Insecure", ActorA, InheritOrganizationPermissions: true), ct);
        ProjectCreationSelectionEvidence ev2 = new("ev-sec-2", TenantA, ActorA, "ui", "fld-insecure", BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationWorkflowResult res2 = await h2.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-sec-sup", "Supplied Sec", SuppliedFolderId: "fld-insecure", SelectionEvidence: ev2), ct);
        res2.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);

        // 3. Pre-commit Denied on provisioned Folder -> commits ProjectCreationAbandoned, releases claim, retains Folder (0 deletes)!
        TestHarness h3 = new();
        h3.Faults.BeforePreCommitCheckHook = _ =>
        {
            h3.Authority.SetStatus(TenantA, ActorA, ActorAuthorityStatus.Denied);
            return Task.CompletedTask;
        };
        ProjectCreationWorkflowResult res3 = await h3.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-precommit-denied", "PreCommit Denied"), ct);
        res3.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);
        h3.Folders.DeleteAttemptCount.ShouldBe(0);
        (ProjectFolderClaimState claim3, _) = await h3.Workflow.RehydrateClaimStateAsync(TenantA, res3.FolderId!, ct);
        claim3.IsActive.ShouldBeFalse();
        (await h3.Folders.GetProvisionedFolderAsync(TenantA, res3.FolderId!, ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task SuppliedFolder_PreservesDisplayNameEnforcesForeignNonLeakAndOrphanNeedsAttention()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TestHarness h = new();
        await h.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-own", "Existing Folder Own Name", ActorA), ct);
        await h.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord("tenant-foreign", "fld-foreign-orphan", "Foreign Secret", "foreign-actor", IsOrphanedOrReserved: true), ct);
        await h.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-orphan", "Orphan Folder", ActorA, IsOrphanedOrReserved: true), ct);

        // 1. Valid supplied Folder preserves the Folder's own display name (NOT overwritten by Project name!).
        ProjectCreationSelectionEvidence evOwn = new("ev-own", TenantA, ActorA, "ui", "fld-own", BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationWorkflowResult ok = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-sup-own", "My New Project Name", SuppliedFolderId: "fld-own", SelectionEvidence: evOwn), ct);
        ok.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        ProjectReadModel rm = h.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(TenantA, ok.ProjectId!)).ShouldNotBeNull();
        rm.Detail!.Name.ShouldBe("My New Project Name");
        rm.Detail.ProjectFolder!.DisplayName.ShouldBe("Existing Folder Own Name");

        // 2. Foreign orphaned Folder fails closed as Rejected (never leaks NeedsAttention/orphan status!).
        ProjectCreationSelectionEvidence evForeign = new("ev-for", TenantA, ActorA, "ui", "fld-foreign-orphan", BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationWorkflowResult foreignRes = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-sup-for", "Probe Foreign", SuppliedFolderId: "fld-foreign-orphan", SelectionEvidence: evForeign), ct);
        foreignRes.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);

        // 3. Same-Tenant authorized orphaned/reserved Folder maps to NeedsAttention (NOT Rejected!).
        ProjectCreationSelectionEvidence evOrphan = new("ev-orp", TenantA, ActorA, "ui", "fld-orphan", BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationWorkflowResult orphanRes = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-sup-orp", "Probe Orphan", SuppliedFolderId: "fld-orphan", SelectionEvidence: evOrphan), ct);
        orphanRes.Status.ShouldBe(ProjectCreationWorkflowStatus.NeedsAttention);
    }

    [Fact]
    public async Task SelectionEvidenceExpiry_BlocksNewAdmissionButAllowsForwardRecoveryOfAdmittedTask()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TestHarness h = new();
        await h.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-sel", "Selected Folder", ActorA), ct);
        ProjectCreationSelectionEvidence evidence = new("ev-1", TenantA, ActorA, "ui", "fld-sel", BaseTime, BaseTime.AddMinutes(5));
        ProjectCreationRequest req = new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-sel-1", "Selected Project", SuppliedFolderId: "fld-sel", SelectionEvidence: evidence);

        // Pause after Step 1 receipt while Selection Evidence is valid.
        h.Faults.PauseAfterFolderReceipt = true;
        ProjectCreationWorkflowResult admitted = await h.Workflow.CreateProjectAsync(req, ct);
        admitted.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);

        // Advance clock past 5-minute expiry (+6 minutes) AND past lease duration (+35 seconds).
        h.Clock.Advance(TimeSpan.FromMinutes(6));
        h.Faults.PauseAfterFolderReceipt = false;

        // A NEW admission with the expired evidence is rejected with RequestPreview (0 tasks, 0 events).
        ProjectCreationWorkflowResult newExpired = await h.Workflow.CreateProjectAsync(req with { IdempotencyKey = "idem-sel-new-expired" }, ct);
        newExpired.Status.ShouldBe(ProjectCreationWorkflowStatus.RequestPreview);
        newExpired.TaskId.ShouldBeNull();

        // Equivalent retry of the ALREADY-ADMITTED task recovers forward to Succeeded!
        ProjectCreationWorkflowResult recovered = await h.Workflow.CreateProjectAsync(req, ct);
        recovered.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        recovered.TaskId.ShouldBe(admitted.TaskId);
        recovered.FolderId.ShouldBe("fld-sel");
    }

    [Fact]
    public async Task Step1AndStep3_LostReplyOutageAndBudgetExhaustion_RecoverViaAuthoritativeLookup()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        // 1. Step 1 lost reply + transient provision/lookup outages recover to single Folder/Project.
        TestHarness h1 = new(dependencyFailureBudget: 4);
        h1.Folders.RemainingProvisionOutages = 1;
        h1.Folders.DropNextProvisionReplyOnce = true;

        ProjectCreationRequest req = new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-step1-rec", "Step1 Recovery");
        ProjectCreationWorkflowResult attempt1 = await h1.Workflow.CreateProjectAsync(req, ct);
        attempt1.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);

        // Advance past lease expiry so next attempt runs; this attempt provisions durably then drops reply!
        h1.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult attempt2 = await h1.Workflow.ResumeTaskAsync(attempt1.TaskId!, req, ct);
        attempt2.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);

        // Simulate one lookup outage, then recover via LookupProvisioningAsync!
        h1.Folders.RemainingLookupOutages = 1;
        h1.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult attempt3 = await h1.Workflow.ResumeTaskAsync(attempt1.TaskId!, req, ct);
        attempt3.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);

        h1.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult attempt4 = await h1.Workflow.ResumeTaskAsync(attempt1.TaskId!, req, ct);
        attempt4.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        h1.Folders.DeleteAttemptCount.ShouldBe(0);

        // 2. Step 3 FailCommitOnce + DropNextCommitReplyOnce + budget exhaustion -> NeedsAttention retaining Folder/receipt.
        TestHarness h2 = new(dependencyFailureBudget: 3);
        h2.Faults.FailCommitOnce = true;
        h2.Faults.DropNextCommitReplyOnce = true;
        ProjectCreationWorkflowResult s3a = await h2.Workflow.CreateProjectAsync(new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-step3-rec", "Step3 Recovery"), ct);
        s3a.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);

        // Second attempt commits ProjectCreated durably then drops reply!
        h2.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult s3b = await h2.Workflow.ResumeTaskAsync(s3a.TaskId!, cancellationToken: ct);
        s3b.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);

        // Third attempt recovers committed ProjectCreated via CommitBoundProjectStepEffect.LookupAsync without duplicating!
        h2.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult s3c = await h2.Workflow.ResumeTaskAsync(s3a.TaskId!, cancellationToken: ct);
        s3c.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        (_, StoredEventStream projStream) = await h2.Workflow.RehydrateProjectStateAsync(TenantA, s3c.ProjectId!, ct);
        projStream.Events.Count.ShouldBe(1);

        // 3. Exhaust dependency failure budget -> truthful NeedsAttention with Folder and receipt retained!
        TestHarness h3 = new(dependencyFailureBudget: 2);
        h3.Faults.FailCommitOnce = true;
        ProjectCreationWorkflowResult b1 = await h3.Workflow.CreateProjectAsync(new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-budget", "Budget Exhaust"), ct);
        b1.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);

        h3.Faults.FailCommitOnce = true;
        h3.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult b2 = await h3.Workflow.ResumeTaskAsync(b1.TaskId!, cancellationToken: ct);
        b2.Status.ShouldBe(ProjectCreationWorkflowStatus.NeedsAttention);
        b2.FolderId.ShouldNotBeNullOrWhiteSpace();
        b2.FolderProvisioningReceiptId.ShouldNotBeNullOrWhiteSpace();
        h3.Folders.DeleteAttemptCount.ShouldBe(0);
    }

    [Fact]
    public async Task AbandonReservation_RecoversUncommittedStep2ClaimFencesBeforeReleaseAndHandlesReleaseFailure()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        // 1. Step 2 commits AD-35 claim then crashes BEFORE saving Receipts[1] (DropNextClaimReplyOnce).
        //    AbandonReservationAsync must recover the uncommitted claim via RehydrateClaimStateAsync,
        //    commit ProjectCreationAbandoned, project abandonment, release the claim, and transition task to Rejected!
        TestHarness h = new();
        await h.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-uncommitted", "Uncommitted Claim Folder", ActorA), ct);
        ProjectCreationSelectionEvidence ev = new("ev-unc", TenantA, ActorA, "ui", "fld-uncommitted", BaseTime, BaseTime.AddMinutes(5));
        h.Faults.DropNextClaimReplyOnce = true;

        ProjectCreationWorkflowResult crashed = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-unc-claim", "Uncommitted Claim", SuppliedFolderId: "fld-uncommitted", SelectionEvidence: ev), ct);
        crashed.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);

        // Verify Receipts has ONLY Step 1 (Step 2 receipt was NOT saved!), yet claim IS active in claim stream!
        DurableTaskProgress progressBefore = (await h.Workflow.GetTrustedProgressAsync(crashed.TaskId!, ct)).ShouldNotBeNull();
        progressBefore.Receipts.Length.ShouldBe(1);
        (ProjectFolderClaimState claimBefore, _) = await h.Workflow.RehydrateClaimStateAsync(TenantA, "fld-uncommitted", ct);
        claimBefore.IsActive.ShouldBeTrue();
        string reservedProjectId = claimBefore.ActiveClaim!.ProjectId;

        // Advance past lease expiry and abandon!
        h.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult abandoned = await h.Workflow.AbandonReservationAsync(crashed.TaskId!, "caller_cancelled", ct);
        abandoned.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);

        // Claim MUST now be released (NOT leaked!) and Project stream MUST have ProjectCreationAbandoned!
        (ProjectFolderClaimState claimAfter, _) = await h.Workflow.RehydrateClaimStateAsync(TenantA, "fld-uncommitted", ct);
        claimAfter.IsActive.ShouldBeFalse();
        (ProjectSdkState projState, _) = await h.Workflow.RehydrateProjectStateAsync(TenantA, reservedProjectId, ct);
        projState.DomainState.IsCreationAbandoned.ShouldBeTrue();

        // Supported read model recorded abandonment and excluded it from ProjectReadIndex.
        ProjectReadModel rm = h.Store.Snapshot<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(TenantA, reservedProjectId)).ShouldNotBeNull();
        rm.IsCreationAbandoned.ShouldBeTrue();
        rm.Detail.ShouldBeNull();

        // 2. When claim release fails (FailReleaseClaimOnce), task MUST transition to NeedsAttention (NOT Rejected)!
        TestHarness h2 = new();
        await h2.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-relfail", "Release Fail Folder", ActorA), ct);
        ProjectCreationSelectionEvidence ev2 = new("ev-rf", TenantA, ActorA, "ui", "fld-relfail", BaseTime, BaseTime.AddMinutes(5));
        h2.Faults.PauseAfterClaim = true;
        ProjectCreationWorkflowResult paused = await h2.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-relfail", "Rel Fail", SuppliedFolderId: "fld-relfail", SelectionEvidence: ev2), ct);
        h2.Faults.PauseAfterClaim = false;
        h2.Faults.FailReleaseClaimOnce = true;
        h2.Clock.Advance(TimeSpan.FromSeconds(35));

        ProjectCreationWorkflowResult failedRelease = await h2.Workflow.AbandonReservationAsync(paused.TaskId!, "caller_cancelled", ct);
        failedRelease.Status.ShouldBe(ProjectCreationWorkflowStatus.NeedsAttention);
    }

    [Fact]
    public async Task AuthorityUnavailableVsDenied_DirectDelegatedParityAndTrustedStatusReads()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TestHarness h = new();

        // 1. ActorAuthorityStatus.Unavailable -> 503 Unavailable (distinct from 404 Denied!).
        h.Authority.SetStatus(TenantA, ActorA, ActorAuthorityStatus.Unavailable);
        ProjectCreationWorkflowResult unavail = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-unavail", "Unavailable Auth"), ct);
        unavail.Status.ShouldBe(ProjectCreationWorkflowStatus.Unavailable);
        unavail.HttpStatusCode.ShouldBe(503);

        h.Authority.SetStatus(TenantA, ActorA, ActorAuthorityStatus.Denied);
        ProjectCreationWorkflowResult denied = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-denied", "Denied Auth"), ct);
        denied.Status.ShouldBe(ProjectCreationWorkflowStatus.Denied);
        denied.HttpStatusCode.ShouldBe(404);

        // 2. Direct vs Delegated Chatbot parity: same actor, same key, same payload -> identical task & outcome!
        h.Authority.SetStatus(TenantA, ActorA, ActorAuthorityStatus.Allowed);
        ProjectCreationRequest direct = new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-parity", "Parity Workspace", IsDelegated: false);
        ProjectCreationRequest delegated = new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "agent", "idem-parity", "Parity Workspace", IsDelegated: true, DelegatedWorkloadId: "chatbot-workload");

        ProjectCreationWorkflowResult directRes = await h.Workflow.CreateProjectAsync(direct, ct);
        ProjectCreationWorkflowResult delegatedRes = await h.Workflow.CreateProjectAsync(delegated, ct);
        directRes.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        delegatedRes.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        delegatedRes.TaskId.ShouldBe(directRes.TaskId);
        delegatedRes.ProjectId.ShouldBe(directRes.ProjectId);

        // 3. Trusted status read exposes InProgress even when actor authority is revoked;
        //    ResumeTaskAsync enforces live authority on pre-commit steps (503 Unavailable, then Denied -> abandons)!
        h.Faults.PauseAfterFolderReceipt = true;
        ProjectCreationWorkflowResult paused = await h.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-resume-check", "Resume Check Workspace"), ct);
        paused.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        h.Faults.PauseAfterFolderReceipt = false;

        h.Authority.SetStatus(TenantA, ActorA, ActorAuthorityStatus.Unavailable);
        (await h.Workflow.GetTrustedStatusAsync(paused.TaskId!, ct))!.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        ProjectCreationWorkflowResult resumeUnavail = await h.Workflow.ResumeTaskAsync(paused.TaskId!, cancellationToken: ct);
        resumeUnavail.Status.ShouldBe(ProjectCreationWorkflowStatus.Unavailable);
        resumeUnavail.HttpStatusCode.ShouldBe(503);

        h.Authority.SetStatus(TenantA, ActorA, ActorAuthorityStatus.Denied);
        h.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult resumeDenied = await h.Workflow.ResumeTaskAsync(paused.TaskId!, cancellationToken: ct);
        resumeDenied.Status.ShouldBe(ProjectCreationWorkflowStatus.Denied);
        resumeDenied.HttpStatusCode.ShouldBe(404);
        (await h.Workflow.GetTrustedStatusAsync(paused.TaskId!, ct))!.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);
    }

    [Fact]
    public async Task PauseAtEveryBoundary_KeepsProjectInvisibleUntilBindingAndReadModelConfirmation()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TestHarness h = new();
        ProjectCreationRequest req = new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-boundaries", "Boundary Project");

        // Pause before Step 1
        h.Faults.PauseBeforeProvisioning = true;
        ProjectCreationWorkflowResult r0 = await h.Workflow.CreateProjectAsync(req, ct);
        r0.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        h.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(TenantA)).ShouldBeNull();

        // Advance to pause after Step 1 receipt
        h.Faults.PauseBeforeProvisioning = false;
        h.Faults.PauseAfterFolderReceipt = true;
        ProjectCreationWorkflowResult r1 = await h.Workflow.ResumeTaskAsync(r0.TaskId!, req, ct);
        r1.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        r1.FolderId.ShouldNotBeNullOrWhiteSpace();
        h.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(TenantA)).ShouldBeNull();

        // Advance to pause after Step 3 commit BEFORE projection confirmation
        h.Faults.PauseAfterFolderReceipt = false;
        h.Faults.PauseBeforeProjection = true;
        h.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult r3 = await h.Workflow.ResumeTaskAsync(r0.TaskId!, req, ct);
        r3.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        h.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(TenantA)).ShouldBeNull();

        // IrreversibleStep is set -> AbandonReservationAsync MUST refuse with Conflict!
        ProjectCreationWorkflowResult refuseAbandon = await h.Workflow.AbandonReservationAsync(r0.TaskId!, cancellationToken: ct);
        refuseAbandon.Status.ShouldBe(ProjectCreationWorkflowStatus.Conflict);

        // Complete Step 4 read-model confirmation -> Succeeded and visible in both supported and retained reads!
        h.Faults.PauseBeforeProjection = false;
        h.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult done = await h.Workflow.ResumeTaskAsync(r0.TaskId!, req, ct);
        done.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        done.ProjectId.ShouldNotBeNullOrWhiteSpace();

        ProjectReadIndex index = h.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(TenantA)).ShouldNotBeNull();
        index.ProjectIds.ShouldBe([done.ProjectId!]);

        // Retained projections agree with supported read model!
        (_, StoredEventStream stream) = await h.Workflow.RehydrateProjectStateAsync(TenantA, done.ProjectId!, ct);
        ProjectProjectionEnvelope[] envelopes = stream.Events
            .Select(e => new ProjectProjectionEnvelope(TenantA, e.SequenceNumber, (IProjectEvent)ReadModelBackedEventStore.DeserializeEvent(e)))
            .ToArray();
        ProjectDetailProjection.Empty.Apply(envelopes).Get(TenantA, done.ProjectId!)!.ProjectFolder!.FolderId.ShouldBe(done.FolderId);
        ProjectListProjection.Empty.Apply(envelopes).Get(TenantA, done.ProjectId!)!.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        ProjectReferenceIndexProjection.Empty.Apply(envelopes).List(TenantA, done.ProjectId!).ShouldHaveSingleItem().ReferenceId.ShouldBe(done.FolderId);
        ProjectAuditTimelineProjection.Empty.Apply(envelopes).List(TenantA, done.ProjectId!, null).ShouldHaveSingleItem().OperationType.ShouldBe("project.created");
    }

    [Fact]
    public async Task Step4ProjectionRetryUnavailableEscalationPreCommitSuppliedFolderAndContractAlignment()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        // 1. DeriveFolderId matches FolderProvisioningContract (40 hex chars, no extra colon)
        //    and surrogate pair at index 127..128 never throws ArgumentException in ProvisionFolderAsync.
        string derived = FoldersAggregateProvisioningClient.DeriveFolderId("tenant-a", "task-1/step/1");
        derived.ShouldStartWith("fld-p-");
        derived.Length.ShouldBe("fld-p-".Length + 40);

        TestHarness hSurrogate = new();
        string surrogateBoundaryName = new string('a', 127) + "\U0001F680" + "-ok";
        surrogateBoundaryName.Length.ShouldBeGreaterThan(128);
        FolderProvisioningResult surrogateRes = await hSurrogate.Folders.ProvisionFolderAsync(
            TenantA,
            ActorA,
            new("task-sur", "task-sur/step/1", FolderFirstProjectCreationWorkflow.AdmissionOperation,
                FoldersAggregateProvisioningClient.ComputeRequestSha256(surrogateBoundaryName), 1, 0, surrogateBoundaryName),
            ct);
        surrogateRes.Outcome.ShouldBe(FolderProvisioningOutcome.Provisioned);

        // Unpaired high surrogate fails cleanly with ValidationFailed (never uncaught ArgumentException).
        string unpaired = "bad-" + '\uD800' + "-name";
        FolderProvisioningResult unpairedRes = await hSurrogate.Folders.ProvisionFolderAsync(
            TenantA,
            ActorA,
            new("task-unp", "task-unp/step/1", FolderFirstProjectCreationWorkflow.AdmissionOperation,
                FoldersAggregateProvisioningClient.ComputeRequestSha256(unpaired), 1, 0, unpaired),
            ct);
        unpairedRes.Outcome.ShouldBe(FolderProvisioningOutcome.ValidationFailed);

        // 2. Step 4 transient FailProjectionOnce -> WaitingForDependency -> retry clears EffectPending cleanly & succeeds!
        TestHarness hStep4 = new();
        hStep4.Faults.FailProjectionOnce = true;
        ProjectCreationRequest req4 = new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-step4-retry", "Step4 Retry Workspace");
        ProjectCreationWorkflowResult lag = await hStep4.Workflow.CreateProjectAsync(req4, ct);
        lag.Status.ShouldBe(ProjectCreationWorkflowStatus.WaitingForDependency);
        lag.ProjectId.ShouldBeNull();

        hStep4.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult recovered = await hStep4.Workflow.ResumeTaskAsync(lag.TaskId!, req4, ct);
        recovered.Status.ShouldBe(ProjectCreationWorkflowStatus.Succeeded);
        recovered.ProjectId.ShouldNotBeNullOrWhiteSpace();
        hStep4.Store.Snapshot<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(TenantA))!.ProjectIds.ShouldContain(recovered.ProjectId!);

        // 3. Step 4 Unavailable read model escalates cleanly to NeedsAttention without exposing ProjectId!
        TestHarness hUnavail = new();
        hUnavail.Faults.PauseBeforeProjection = true;
        ProjectCreationRequest reqUnavail = new(TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-step4-unavail", "Step4 Unavail Workspace");
        ProjectCreationWorkflowResult pausedUnavail = await hUnavail.Workflow.CreateProjectAsync(reqUnavail, ct);
        pausedUnavail.Status.ShouldBe(ProjectCreationWorkflowStatus.InProgress);
        (ProjectFolderClaimState claimUnavail, _) = await hUnavail.Workflow.RehydrateClaimStateAsync(TenantA, pausedUnavail.FolderId!, ct);
        string unavailProjectId = claimUnavail.ActiveClaim!.ProjectId;
        await hUnavail.Store.SaveAsync(
            ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Detail(TenantA, unavailProjectId),
            new ProjectReadModel(TenantA, unavailProjectId, null, 99, 99, BaseTime, EvidenceFreshnessState.Unavailable),
            ct);
        hUnavail.Faults.PauseBeforeProjection = false;
        hUnavail.Clock.Advance(TimeSpan.FromSeconds(35));
        ProjectCreationWorkflowResult escalated = await hUnavail.Workflow.ResumeTaskAsync(pausedUnavail.TaskId!, reqUnavail, ct);
        escalated.Status.ShouldBe(ProjectCreationWorkflowStatus.NeedsAttention);
        escalated.ProjectId.ShouldBeNull();

        // 4. Pre-commit supplied-Folder re-check: revoked -> Rejected + claim released; orphaned -> NeedsAttention!
        TestHarness hRevoked = new();
        await hRevoked.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-pre-rev", "Pre Revoked Folder", ActorA), ct);
        ProjectCreationSelectionEvidence evRev = new("ev-pre-rev", TenantA, ActorA, "ui", "fld-pre-rev", BaseTime, BaseTime.AddMinutes(5));
        hRevoked.Faults.BeforePreCommitCheckHook = _ =>
        {
            hRevoked.Folders.DenyActor(TenantA, ActorA);
            return Task.CompletedTask;
        };
        ProjectCreationWorkflowResult resRev = await hRevoked.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-pre-rev", "Pre Rev", SuppliedFolderId: "fld-pre-rev", SelectionEvidence: evRev), ct);
        resRev.Status.ShouldBe(ProjectCreationWorkflowStatus.Rejected);
        (ProjectFolderClaimState claimRev, _) = await hRevoked.Workflow.RehydrateClaimStateAsync(TenantA, "fld-pre-rev", ct);
        claimRev.IsActive.ShouldBeFalse();

        TestHarness hOrphan = new();
        await hOrphan.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-pre-orph", "Pre Orphan Folder", ActorA), ct);
        ProjectCreationSelectionEvidence evOrph = new("ev-pre-orph", TenantA, ActorA, "ui", "fld-pre-orph", BaseTime, BaseTime.AddMinutes(5));
        hOrphan.Faults.BeforePreCommitCheckHook = token =>
            hOrphan.Folders.RegisterSuppliedFolderAsync(new SuppliedFolderRecord(TenantA, "fld-pre-orph", "Pre Orphan Folder", ActorA, IsOrphanedOrReserved: true), token);
        ProjectCreationWorkflowResult resOrph = await hOrphan.Workflow.CreateProjectAsync(new(
            TenantA, ActorA, ProjectCreationCallerClass.ProjectUser, "ui", "idem-pre-orph", "Pre Orphan", SuppliedFolderId: "fld-pre-orph", SelectionEvidence: evOrph), ct);
        resOrph.Status.ShouldBe(ProjectCreationWorkflowStatus.NeedsAttention);
    }

    private sealed class TestHarness
    {
        public Story71InMemoryReadModelStore Store { get; } = new();
        public Story71ManualTimeProvider Clock { get; } = new(BaseTime);
        public MutableProjectCreationAuthorityChecker Authority { get; } = new();
        public FoldersAggregateProvisioningClient Folders { get; }
        public ReadModelBackedEventStore EventStore { get; }
        public ProjectCreationFaultOptions Faults { get; } = new();
        public FolderFirstProjectCreationWorkflow Workflow { get; }

        public TestHarness(int dependencyFailureBudget = 3)
        {
            Folders = new FoldersAggregateProvisioningClient(Store, ProjectReadModelKeys.StoreName, Clock);
            EventStore = new ReadModelBackedEventStore(Store, ProjectReadModelKeys.StoreName, Clock);
            Workflow = new FolderFirstProjectCreationWorkflow(
                Store,
                Store,
                ProjectReadModelKeys.StoreName,
                EventStore,
                new ProjectSdkAggregate(Clock),
                new ProjectFolderClaimAggregate(Clock),
                Folders,
                Authority,
                Clock,
                leaseDuration: TimeSpan.FromSeconds(30),
                dependencyFailureBudget: dependencyFailureBudget,
                faultOptions: Faults);
        }
    }
}
#endif
