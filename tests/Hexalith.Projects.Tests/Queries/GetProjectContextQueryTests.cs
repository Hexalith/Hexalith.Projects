// <copyright file="GetProjectContextQueryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Queries;

using Hexalith.Projects.Context;
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
    public void GetAdmission_MetadataOnly_ContainsAllowlistedReferencesOnly()
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
        response.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        response.Conversations.ShouldBeEmpty();
        response.FileReferences.ShouldContain(item => item.ReferenceKind == "file");
        response.MemoryReferences.ShouldContain(item => item.ReferenceKind == "memory");
        response.Excluded.ShouldContain(item => item.ReferenceKind == "conversation");
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
        explanation.Evaluations.ShouldContain(item => item.ReferenceKind == "folder" && item.FailedCheck is null);
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
                new Hexalith.Projects.Contracts.Models.ProjectContextExplanation(legacy.Context, legacy.Evaluations),
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
}
