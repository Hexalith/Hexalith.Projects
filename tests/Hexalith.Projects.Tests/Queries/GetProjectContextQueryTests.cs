// <copyright file="GetProjectContextQueryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Queries;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.Testing.Reads;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>E6.3-U01/U03 query-shaped admission tests for supported Get and Explain.</summary>
public sealed class GetProjectContextQueryTests
{
    [Fact]
    public void GetAdmission_UnconfirmedConversation_IsMinimalUnavailableWithoutIdentity()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextAdmission admission = policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(),
            WithAllKinds(),
            projectVersion: 3,
            asOf: DefaultNow,
            ownerBackedTrustAvailable: false);

        ProjectContextReadResponse response = admission.ToReadResponse();
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        response.Setup.ShouldBeNull();
        response.ProjectFolder.ShouldBeNull();
        response.Conversations.ShouldBeEmpty();
        response.FileReferences.ShouldBeEmpty();
        response.MemoryReferences.ShouldBeEmpty();
        response.Excluded.ShouldBeEmpty();
        response.Snapshot.RecoveryActions.ShouldBe([AdmissionRecoveryAction.ContactAdministrator]);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(response));
    }

    [Fact]
    public void ExplainAdmission_HasDeterministicEvaluationsAndNoPersistedTraceIdentity()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextAdmission admission = policy.AssembleAdmission(
            Context(operationKind: ProjectContextOperationKind.Explain),
            Project(),
            TenantAccess(),
            WithFolder(),
            projectVersion: 3,
            asOf: DefaultNow);

        ExplainContextSelectionResponse explanation = admission.ToExplanation();
        explanation.Evaluations.ShouldContain(item => item.ReferenceKind == "folder" && item.FailedCheck == null);
        explanation.Context.Snapshot.AsOf.ShouldBe(DefaultNow);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(explanation));
    }

    [Fact]
    public void ShadowCompare_FrozenRepresentableCorpus_MatchesAfterAd32Normalization()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence references = WithAllKinds();
        ProjectContextAssemblyResult legacy = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            references);
        ProjectContextAdmission supported = policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(),
            references,
            projectVersion: 1,
            asOf: DefaultNow,
            ownerBackedTrustAvailable: true);

        ProjectContextShadowComparator.CompareGet(legacy.Context, supported.ToReadResponse())
            .Equivalent.ShouldBeTrue();
        ProjectContextShadowComparator.CompareExplain(
                new ProjectContextExplanation(legacy.Context, legacy.Evaluations),
                supported.ToExplanation())
            .Equivalent.ShouldBeTrue();
    }

    [Fact]
    public void ShadowCompare_StaleTenant_IsKnownLegacyDeficit()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence references = WithFolder();
        ProjectContextAssemblyResult legacy = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(Hexalith.Projects.Authorization.TenantAccessOutcome.StaleProjection, Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Stale),
            references);
        ProjectContextAdmission supported = policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(Hexalith.Projects.Authorization.TenantAccessOutcome.StaleProjection, Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Stale),
            references,
            projectVersion: 1,
            asOf: DefaultNow);

        legacy.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        supported.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        ProjectContextShadowComparator.CompareGet(legacy.Context, supported.ToReadResponse())
            .Equivalent.ShouldBeFalse();
    }

    [Fact]
    public void ShadowCompare_DifferentSetup_Diverges()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence references = WithFolder();
        ProjectContextAssemblyResult legacy = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            references);
        ProjectContextAdmission supported = policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(),
            references,
            projectVersion: 1,
            asOf: DefaultNow);
        ProjectContextReadResponse mutated = supported.ToReadResponse() with
        {
            Setup = new ProjectSetup(["goal"], [], [], [], null),
        };

        ProjectContextShadowComparator.CompareGet(legacy.Context, mutated)
            .Equivalent.ShouldBeFalse();
    }

    [Fact]
    public void ShadowCompare_DifferentReferenceMetadataOrCutoff_Diverges()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence references = WithAllKinds();
        ProjectContextAssemblyResult legacy = policy.Assemble(Context(), Project(), TenantAccess(), references);
        ProjectContextReadResponse supported = policy.AssembleAdmission(
                Context(),
                Project(),
                TenantAccess(),
                references,
                projectVersion: 1,
                asOf: DefaultNow,
                ownerBackedTrustAvailable: true)
            .ToReadResponse();
        ProjectContextReference original = supported.FileReferences.Single();

        ProjectContextReadResponse differentLabel = supported with
        {
            FileReferences =
            [
                new ProjectContextReference(
                    original.ReferenceKind,
                    original.ReferenceId,
                    "different label",
                    original.ReferenceState,
                    original.ReasonCode,
                    original.ObservedAt),
            ],
        };
        ProjectContextReadResponse differentReason = supported with
        {
            FileReferences =
            [
                new ProjectContextReference(
                    original.ReferenceKind,
                    original.ReferenceId,
                    original.DisplayName,
                    original.ReferenceState,
                    ProjectReasonCode.MetadataMatched,
                    original.ObservedAt),
            ],
        };
        ProjectContextReadResponse differentObservation = supported with
        {
            FileReferences =
            [
                new ProjectContextReference(
                    original.ReferenceKind,
                    original.ReferenceId,
                    original.DisplayName,
                    original.ReferenceState,
                    original.ReasonCode,
                    original.ObservedAt.AddTicks(1)),
            ],
        };
        ProjectContextReadResponse differentCutoff = supported with
        {
            Snapshot = supported.Snapshot with { AsOf = supported.Snapshot.AsOf.AddTicks(1) },
        };

        ProjectContextShadowComparator.CompareGet(legacy.Context, differentLabel).Equivalent.ShouldBeFalse();
        ProjectContextShadowComparator.CompareGet(legacy.Context, differentReason).Equivalent.ShouldBeFalse();
        ProjectContextShadowComparator.CompareGet(legacy.Context, differentObservation).Equivalent.ShouldBeFalse();
        ProjectContextShadowComparator.CompareGet(legacy.Context, differentCutoff).Equivalent.ShouldBeFalse();
    }

    [Fact]
    public void ShadowCompare_DifferentExclusionOrEvaluationMetadata_Diverges()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence references = new(
            WithFolder().ProjectFolder,
            FileReferences: [],
            MemoryReferences: WithMemory(ReferenceState.Stale).MemoryReferences,
            Conversations: []);
        ProjectContextAssemblyResult legacy = policy.Assemble(Context(), Project(), TenantAccess(), references);
        ProjectContextAdmission admission = policy.AssembleAdmission(
            Context(),
            Project(),
            TenantAccess(),
            references,
            projectVersion: 1,
            asOf: DefaultNow);
        ProjectContextReadResponse supported = admission.ToReadResponse();
        ExplainContextSelectionResponse explanation = admission.ToExplanation();
        ProjectContextExclusion exclusion = supported.Excluded.Single();
        ProjectContextEvaluation evaluation = explanation.Evaluations.Single(item => item.ReferenceKind == "memory");

        ProjectContextReadResponse differentExclusion = supported with
        {
            Excluded =
            [
                new ProjectContextExclusion(
                    exclusion.ReferenceKind,
                    exclusion.ReferenceId,
                    exclusion.ReferenceState,
                    ProjectReasonCode.MetadataMatched,
                    exclusion.FailedCheck,
                    ProjectContextInclusionDiagnostic.ReferenceUnavailable),
            ],
        };
        ExplainContextSelectionResponse differentEvaluation = explanation with
        {
            Evaluations = explanation.Evaluations
                .Select(item => item.ReferenceKind == "memory"
                    ? new ProjectContextEvaluation(
                        evaluation.ReferenceKind,
                        evaluation.ReferenceId,
                        evaluation.ResultState,
                        evaluation.FailedCheck,
                        ProjectReasonCode.MetadataMatched,
                        ProjectContextInclusionDiagnostic.ReferenceUnavailable,
                        evaluation.ObservedAt.AddTicks(1))
                    : item)
                .ToArray(),
        };

        ProjectContextShadowComparator.CompareGet(legacy.Context, differentExclusion).Equivalent.ShouldBeFalse();
        ProjectContextShadowComparator.CompareExplain(
                new ProjectContextExplanation(legacy.Context, legacy.Evaluations),
                differentEvaluation)
            .Equivalent.ShouldBeFalse();
    }
}
