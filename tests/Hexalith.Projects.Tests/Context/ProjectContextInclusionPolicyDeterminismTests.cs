// <copyright file="ProjectContextInclusionPolicyDeterminismTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using System;
using System.Linq;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 tests for the determinism guarantee of <see cref="ProjectContextInclusionPolicy"/>
/// (Story 3.1 AC 12, AC 17). Two calls with identical inputs return equal outputs; output lists
/// are ordered deterministically by <c>(ReferenceKind, ReferenceId)</c> Ordinal; ordering is
/// stable under input-permutation.
/// </summary>
public sealed class ProjectContextInclusionPolicyDeterminismTests
{
    [Fact]
    public void Assemble_TwoCallsWithIdenticalInputs_AreEqual()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextAssemblyContext ctx = Context();
        ProjectContextProjectEvidence projectEvidence = Project();
        ProjectContextTenantAccess tenantAccess = TenantAccess();
        ProjectContextReferenceEvidence evidence = WithAllKinds();

        ProjectContextAssemblyResult first = policy.Assemble(ctx, projectEvidence, tenantAccess, evidence);
        ProjectContextAssemblyResult second = policy.Assemble(ctx, projectEvidence, tenantAccess, evidence);

        AssertContextEqual(first.Context, second.Context);
        first.Evaluations.ToArray().ShouldBe(second.Evaluations.ToArray());
    }

    [Fact]
    public void Assemble_OutputLists_OrderedByReferenceKindThenIdOrdinal()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence permuted = new(
            ProjectFolder: null,
            FileReferences:
            [
                new ProjectFileReference("file_z", "folder_a", null, ReferenceState.Included, null, DefaultNow),
                new ProjectFileReference("file_a", "folder_a", null, ReferenceState.Included, null, DefaultNow),
                new ProjectFileReference("file_m", "folder_a", null, ReferenceState.Included, null, DefaultNow),
            ],
            MemoryReferences: Array.Empty<ProjectMemoryReference>(),
            Conversations: Array.Empty<ProjectContextConversationEvidence>());

        ProjectContextAssemblyResult result = policy.Assemble(Context(), Project(), TenantAccess(), permuted);

        result.Context.FileReferences.Select(static f => f.ReferenceId).ShouldBe(["file_a", "file_m", "file_z"]);
    }

    [Fact]
    public void Assemble_InputPermutation_DoesNotChangeOutput()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectFileReference[] ordered =
        [
            new("file_a", "folder_a", null, ReferenceState.Included, null, DefaultNow),
            new("file_m", "folder_a", null, ReferenceState.Stale, null, DefaultNow),
            new("file_z", "folder_a", null, ReferenceState.Archived, null, DefaultNow),
        ];

        ProjectContextAssemblyResult resultOrdered = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            new ProjectContextReferenceEvidence(null, ordered, Array.Empty<ProjectMemoryReference>(), Array.Empty<ProjectContextConversationEvidence>()));
        ProjectContextAssemblyResult resultReversed = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            new ProjectContextReferenceEvidence(null, [.. ordered.Reverse()], Array.Empty<ProjectMemoryReference>(), Array.Empty<ProjectContextConversationEvidence>()));

        AssertContextEqual(resultOrdered.Context, resultReversed.Context);
    }

    /// <summary>
    /// Compare two <see cref="ProjectContext"/> values by their stable, ordered metadata. We do not
    /// rely on record default equality here because the inner <c>IReadOnlyList&lt;&gt;</c> collections
    /// use reference equality on the array instances, while the policy intentionally allocates fresh
    /// arrays on each call.
    /// </summary>
    private static void AssertContextEqual(ProjectContext expected, ProjectContext actual)
    {
        actual.TenantId.ShouldBe(expected.TenantId);
        actual.ProjectId.ShouldBe(expected.ProjectId);
        actual.Lifecycle.ShouldBe(expected.Lifecycle);
        actual.AssemblyOutcome.ShouldBe(expected.AssemblyOutcome);
        actual.ObservedAt.ShouldBe(expected.ObservedAt);
        actual.Freshness.ShouldBe(expected.Freshness);
        actual.ProjectFolder.ShouldBe(expected.ProjectFolder);
        actual.Conversations.ToArray().ShouldBe(expected.Conversations.ToArray());
        actual.FileReferences.ToArray().ShouldBe(expected.FileReferences.ToArray());
        actual.MemoryReferences.ToArray().ShouldBe(expected.MemoryReferences.ToArray());
        actual.Excluded.ToArray().ShouldBe(expected.Excluded.ToArray());
    }

    [Fact]
    public void Assemble_ExcludedRowsAreOrderedDeterministically()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence evidence = new(
            ProjectFolder: null,
            FileReferences:
            [
                new ProjectFileReference("file_z", "folder_a", null, ReferenceState.Archived, null, DefaultNow),
                new ProjectFileReference("file_a", "folder_a", null, ReferenceState.Stale, null, DefaultNow),
            ],
            MemoryReferences:
            [
                new ProjectMemoryReference("case_z", null, ReferenceState.Unauthorized, null, DefaultNow),
                new ProjectMemoryReference("case_a", null, ReferenceState.Archived, null, DefaultNow),
            ],
            Conversations: Array.Empty<ProjectContextConversationEvidence>());

        ProjectContextAssemblyResult result = policy.Assemble(Context(), Project(), TenantAccess(), evidence);

        result.Context.Excluded.Select(static r => (r.ReferenceKind, r.ReferenceId))
            .ShouldBe(
            [
                ("file", "file_a"),
                ("file", "file_z"),
                ("memory", "case_a"),
                ("memory", "case_z"),
            ]);
    }
}
