// <copyright file="ProjectContextDecisionMatrixCompletenessTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Decision-matrix completeness assertion (Story 3.1 AC 7, AC 12). The hard-coded table below
/// mirrors <c>docs/context-assembly-decision-matrix.md</c>: for every (evidence-state) cell, the
/// policy must produce the surfaced state, failed check, and outer outcome the doc declares. Any
/// divergence between the doc and the policy fails this test, guaranteeing that Stories 3.2–3.5
/// can rely on the matrix as the single source of truth.
/// </summary>
public sealed class ProjectContextDecisionMatrixCompletenessTests
{
    public static IEnumerable<object?[]> ReferenceCells()
    {
        // (state on file/memory ACL, expected surfaced state, expected failed check)
        yield return new object?[] { ReferenceState.Included, ReferenceState.Included, (ProjectContextInclusionCheck?)null, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.Archived, ReferenceState.Archived, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.Unauthorized, ReferenceState.Unauthorized, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceAuthorization, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.Unavailable, ReferenceState.Unavailable, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.Stale, ReferenceState.Stale, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.Ambiguous, ReferenceState.Ambiguous, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.Conflict, ReferenceState.Conflict, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.InvalidReference, ReferenceState.InvalidReference, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceKindAllowlist, ProjectContextAssemblyOutcome.Assembled };
        yield return new object?[] { ReferenceState.Pending, ReferenceState.Pending, (ProjectContextInclusionCheck?)ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextAssemblyOutcome.Assembled };
    }

    [Theory]
    [MemberData(nameof(ReferenceCells))]
    public void File_DecisionMatrixCell_PolicyAgrees(
        ReferenceState input,
        ReferenceState expectedState,
        ProjectContextInclusionCheck? expectedFailedCheck,
        ProjectContextAssemblyOutcome expectedOutcome)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFile(input));

        result.Context.AssemblyOutcome.ShouldBe(expectedOutcome);
        if (expectedFailedCheck is null)
        {
            result.Context.FileReferences.Count.ShouldBe(1);
            result.Context.FileReferences[0].ReferenceState.ShouldBe(expectedState);
            result.Context.Excluded.ShouldBeEmpty();
        }
        else
        {
            result.Context.FileReferences.ShouldBeEmpty();
            result.Context.Excluded.Count.ShouldBe(1);
            result.Context.Excluded[0].ReferenceState.ShouldBe(expectedState);
            result.Context.Excluded[0].FailedCheck.ShouldBe(expectedFailedCheck.Value);
        }
    }

    [Theory]
    [InlineData(ProjectConversationTrustSignal.Current, ReferenceState.Included, null)]
    [InlineData(ProjectConversationTrustSignal.Stale, ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.MixedGeneration, ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.Rebuilding, ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.Unavailable, ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.Forbidden, ReferenceState.Unauthorized, ProjectContextInclusionCheck.ReferenceAuthorization)]
    [InlineData(ProjectConversationTrustSignal.Redacted, ReferenceState.Excluded, ProjectContextInclusionCheck.ReferenceFreshness)]
    public void Conversation_DecisionMatrixCell_PolicyAgrees(
        ProjectConversationTrustSignal signal,
        ReferenceState expectedState,
        ProjectContextInclusionCheck? expectedFailedCheck)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithConversation(signal));

        if (expectedFailedCheck is null)
        {
            result.Context.Conversations.Count.ShouldBe(1);
            result.Context.Conversations[0].ReferenceState.ShouldBe(expectedState);
        }
        else
        {
            result.Context.Conversations.ShouldBeEmpty();
            result.Context.Excluded.Count.ShouldBe(1);
            result.Context.Excluded[0].FailedCheck.ShouldBe(expectedFailedCheck.Value);
            result.Context.Excluded[0].ReferenceState.ShouldBe(expectedState);
        }
    }

    [Fact]
    public void Memory_TenantMismatchRow_CollapsesToUnauthorized_AtBoundary()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithMemory(ReferenceState.TenantMismatch));

        result.Context.Excluded.Count.ShouldBe(1);
        result.Context.Excluded[0].ReferenceState.ShouldBe(ReferenceState.Unauthorized);
        result.Context.Excluded[0].Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.TenantMismatch);
    }

    [Fact]
    public void OuterCells_TenantMissingAndCrossTenant_AreSafeDenials()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult tenantMissing = policy.Assemble(
            Context(authoritativeTenantId: null), Project(), TenantAccess(), NoReferences());
        ProjectContextAssemblyResult crossTenant = policy.Assemble(
            Context(authoritativeTenantId: "tenant-a", requestedTenantId: "tenant-a"), Project(tenantId: "tenant-b"), TenantAccess(tenantId: "tenant-a"), NoReferences());
        ProjectContextAssemblyResult notVisible = policy.Assemble(
            Context(), ProjectMissing(), TenantAccess(), NoReferences());

        tenantMissing.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Unauthorized);
        crossTenant.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
        notVisible.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
    }

    [Fact]
    public void ArchivedProject_AllReferencesExcluded_WithProjectLifecycleFailedCheck()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(lifecycle: ProjectLifecycle.Archived),
            TenantAccess(),
            WithAllKinds());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.Excluded.Count.ShouldBe(4);
        result.Context.Excluded
            .Select(static r => r.FailedCheck)
            .Distinct()
            .ShouldBe([ProjectContextInclusionCheck.ProjectLifecycle]);
    }
}
