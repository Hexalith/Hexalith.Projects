// <copyright file="ProjectContextAdmissionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using System.Linq;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>AD-32 usability tests over the shared inclusion allowlist.</summary>
public sealed class ProjectContextAdmissionTests
{
    [Fact]
    public void AssembleAdmission_CurrentRequiredEvidenceWithoutOptionalCandidates_IsComplete()
    {
        ProjectContextAdmission admission = Admit(WithFolder());

        admission.IsSafeDenial.ShouldBeFalse();
        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        admission.Setup.ShouldBe(ProjectSetup.Empty);
        admission.ProjectFolder.ShouldNotBeNull();
        admission.FileReferences.ShouldBeEmpty();
        admission.Excluded.ShouldBeEmpty();
        admission.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.None]);
        admission.Snapshot.Components.ShouldContain(component => component.Name == "Setup" && component.Included);
    }

    [Fact]
    public void AssembleAdmission_UnauthorizedFile_IsPartialWithExplicitOmission()
    {
        ProjectContextReferenceEvidence references = new(
            WithFolder().ProjectFolder,
            WithFile(ReferenceState.Unauthorized).FileReferences,
            MemoryReferences: [],
            Conversations: []);

        ProjectContextAdmission admission = Admit(references);

        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        admission.FileReferences.ShouldBeEmpty();
        admission.Excluded.ShouldContain(item =>
            item.ReferenceId == "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1"
            && item.ReferenceState == ReferenceState.Unauthorized);
        admission.Excluded.ShouldNotBeEmpty();
        admission.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.ContactAdministrator]);
    }

    [Fact]
    public void AssembleAdmission_UnauthorizedAndStaleFiles_ReturnsBothApplicableRecoveries()
    {
        ProjectContextReferenceEvidence references = new(
            WithFolder().ProjectFolder,
            [
                new ProjectFileReference("file-unauth", "folder", "a", ReferenceState.Unauthorized, null, DefaultNow),
                new ProjectFileReference("file-stale", "folder", "b", ReferenceState.Stale, null, DefaultNow),
            ],
            MemoryReferences: [],
            Conversations: []);

        ProjectContextAdmission admission = Admit(references);

        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        admission.Snapshot.RecoveryActions.ShouldBe(
            [AdmissionRecoveryAction.RefreshContext, AdmissionRecoveryAction.ContactAdministrator]);
    }

    [Fact]
    public void AssembleAdmission_MissingFolder_IsUnavailable()
    {
        ProjectContextAdmission admission = Admit(NoReferences());

        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        admission.Setup.ShouldBeNull();
        admission.ProjectFolder.ShouldBeNull();
        admission.Snapshot.Components.ShouldContain(component =>
            component.Name == "Setup" && !component.Included && component.Freshness != EvidenceFreshnessState.Current);
        admission.Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.RefreshContext);
    }

    [Fact]
    public void AssembleAdmission_StaleTenant_DoesNotOverrideRequiredUnavailable()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAdmission admission = policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(TenantAccessOutcome.StaleProjection, TenantProjectionFreshnessStatus.Stale),
            WithFolder(),
            projectVersion: 1,
            asOf: DefaultNow);

        admission.IsSafeDenial.ShouldBeFalse();
        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
    }

    [Fact]
    public void AssembleAdmission_ExcludedSourceKind_IsPartial()
    {
        ProjectContextInclusionPolicy policy = new();
        Hexalith.Projects.Projections.ProjectDetail.ProjectDetailItem detail = Project().Detail! with
        {
            Setup = new ProjectSetup([], [], [], [ProjectContextSourceKind.FileReference], null),
        };

        ProjectContextAdmission admission = policy.AssembleAdmission(
            Context(),
            new ProjectContextProjectEvidence(detail),
            TenantAccess(),
            new ProjectContextReferenceEvidence(
                WithFolder().ProjectFolder,
                WithFile().FileReferences,
                MemoryReferences: [],
                Conversations: []),
            projectVersion: 1,
            asOf: DefaultNow);

        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        admission.FileReferences.ShouldBeEmpty();
        admission.Excluded.ShouldContain(item => item.ReferenceKind == "file");
    }

    [Fact]
    public void AssembleAdmission_ConversationWithoutOwnerTrust_IsPartialOmission()
    {
        ProjectContextAdmission admission = Admit(
            new ProjectContextReferenceEvidence(
                WithFolder().ProjectFolder,
                FileReferences: [],
                MemoryReferences: [],
                Conversations: WithConversation().Conversations),
            ownerBackedTrustAvailable: false);

        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        admission.Conversations.ShouldBeEmpty();
        admission.Excluded.ShouldContain(item =>
            item.ReferenceKind == "conversation"
            && item.ReferenceState == ReferenceState.Unavailable);
        admission.Evaluations.ShouldContain(item => item.ReferenceKind == "conversation" && item.FailedCheck == ProjectContextInclusionCheck.ReferenceFreshness);
    }

    [Fact]
    public void AssembleAdmission_Overflow_IsUnavailableWithoutTruncation()
    {
        ProjectFileReference[] files = Enumerable.Range(0, ProjectContextReadLimits.MaxReferences + 1)
            .Select(index => new ProjectFileReference(
                $"file_{index:D4}",
                "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                "name",
                ReferenceState.Included,
                null,
                DefaultNow))
            .ToArray();

        ProjectContextAdmission admission = Admit(new ProjectContextReferenceEvidence(
            WithFolder().ProjectFolder,
            files,
            MemoryReferences: [],
            Conversations: []));

        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        admission.FileReferences.ShouldBeEmpty();
        admission.Excluded.ShouldBeEmpty();
        admission.Snapshot.RecoveryActions.ShouldContain(AdmissionRecoveryAction.Retry);
    }

    [Fact]
    public void AssembleAdmission_DuplicateFileIdentity_IsUnavailable()
    {
        ProjectContextReferenceEvidence references = new(
            WithFolder().ProjectFolder,
            [
                new ProjectFileReference("file-dup", "folder", "a", ReferenceState.Included, null, DefaultNow),
                new ProjectFileReference("file-dup", "folder", "b", ReferenceState.Included, null, DefaultNow),
            ],
            MemoryReferences: [],
            Conversations: []);

        ProjectContextAdmission admission = Admit(references);

        admission.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        admission.FileReferences.ShouldBeEmpty();
    }

    [Fact]
    public void AssembleAdmission_UnauthorizedTenant_IsSafeDenial()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAdmission admission = policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(TenantAccessOutcome.Denied),
            WithFolder(),
            projectVersion: 1,
            asOf: DefaultNow);

        admission.IsSafeDenial.ShouldBeTrue();
    }

    private static ProjectContextAdmission Admit(
        ProjectContextReferenceEvidence references,
        bool ownerBackedTrustAvailable = false)
    {
        ProjectContextInclusionPolicy policy = new();
        return policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(),
            references,
            projectVersion: 7,
            asOf: DefaultNow,
            ownerBackedTrustAvailable);
    }
}
