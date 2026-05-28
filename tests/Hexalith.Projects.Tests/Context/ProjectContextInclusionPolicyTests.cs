// <copyright file="ProjectContextInclusionPolicyTests.cs" company="Hexalith">
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
/// Tier-1 happy-path and total-order tests for <see cref="ProjectContextInclusionPolicy"/> (Story 3.1
/// AC 1, AC 2, AC 6, AC 12). Deterministic-fakes-only — no Dapr, Aspire, network, browser,
/// containers, or wall-clock polling.
/// </summary>
public sealed class ProjectContextInclusionPolicyTests
{
    [Fact]
    public void InclusionOrder_Sequence_IsDeclaredOnceAndExact()
        => ProjectContextInclusionOrder.Sequence.ShouldBe(
        [
            ProjectContextInclusionCheck.TenantAuthority,
            ProjectContextInclusionCheck.ProjectVisibility,
            ProjectContextInclusionCheck.ProjectLifecycle,
            ProjectContextInclusionCheck.ReferenceAuthorization,
            ProjectContextInclusionCheck.ReferenceLifecycle,
            ProjectContextInclusionCheck.ReferenceFreshness,
            ProjectContextInclusionCheck.ReferenceKindAllowlist,
        ]);

    [Fact]
    public void Assemble_ProjectFolderIncluded_HappyPath()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFolder(ReferenceState.Included));

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.ProjectFolder.ShouldNotBeNull();
        result.Context.ProjectFolder!.ReferenceState.ShouldBe(ReferenceState.Included);
        result.Context.ProjectFolder.ReasonCode.ShouldBe(ProjectReasonCode.ProjectFolderMatched);
        result.Context.Excluded.ShouldBeEmpty();
        result.Evaluations.Count.ShouldBe(1);
    }

    [Fact]
    public void Assemble_FileReferenceIncluded_HappyPath()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFile(ReferenceState.Included));

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.FileReferences.Count.ShouldBe(1);
        result.Context.FileReferences[0].ReasonCode.ShouldBe(ProjectReasonCode.FileReferenceMatched);
        result.Context.Excluded.ShouldBeEmpty();
    }

    [Fact]
    public void Assemble_MemoryReferenceIncluded_HappyPath()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithMemory(ReferenceState.Included));

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.MemoryReferences.Count.ShouldBe(1);
        result.Context.MemoryReferences[0].ReasonCode.ShouldBe(ProjectReasonCode.MemoryMatched);
        result.Context.Excluded.ShouldBeEmpty();
    }

    [Fact]
    public void Assemble_ConversationIncluded_HappyPath()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithConversation());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.Conversations.Count.ShouldBe(1);
        result.Context.Conversations[0].ReasonCode.ShouldBe(ProjectReasonCode.ConversationLinked);
        result.Context.Excluded.ShouldBeEmpty();
    }

    [Fact]
    public void Assemble_AllKindsIncluded_AllListsPopulated()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithAllKinds());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.ProjectFolder.ShouldNotBeNull();
        result.Context.FileReferences.Count.ShouldBe(1);
        result.Context.MemoryReferences.Count.ShouldBe(1);
        result.Context.Conversations.Count.ShouldBe(1);
        result.Context.Excluded.ShouldBeEmpty();
        result.Evaluations.Count.ShouldBe(4);
    }

    [Fact]
    public void Assemble_NoReferences_ReturnsEmptyAssembledContext()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            NoReferences());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.ProjectFolder.ShouldBeNull();
        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Conversations.ShouldBeEmpty();
        result.Context.Excluded.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ReferenceState.Conflict, ProjectContextInclusionCheck.ReferenceLifecycle)]
    [InlineData(ReferenceState.Ambiguous, ProjectContextInclusionCheck.ReferenceLifecycle)]
    [InlineData(ReferenceState.Archived, ProjectContextInclusionCheck.ReferenceLifecycle)]
    [InlineData(ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ReferenceState.Pending, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ReferenceState.Unauthorized, ProjectContextInclusionCheck.ReferenceAuthorization)]
    [InlineData(ReferenceState.InvalidReference, ProjectContextInclusionCheck.ReferenceKindAllowlist)]
    public void Assemble_FileReference_PerStateExclusion_MapsToExpectedFailedCheck(
        ReferenceState state,
        ProjectContextInclusionCheck expected)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFile(state));

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.Excluded.Count.ShouldBe(1);
        result.Context.Excluded[0].FailedCheck.ShouldBe(expected);
    }

    [Fact]
    public void Assemble_ObservedAtMatchesContextNow()
    {
        ProjectContextInclusionPolicy policy = new();
        DateTimeOffset now = new(2026, 6, 1, 9, 30, 0, TimeSpan.Zero);

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(now: now),
            Project(),
            TenantAccess(),
            NoReferences());

        result.Context.ObservedAt.ShouldBe(now);
    }

    [Fact]
    public void Assemble_FreshnessMappedFromTenantProjectionStatus()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult fresh = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(freshness: Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Fresh),
            NoReferences());
        ProjectContextAssemblyResult stale = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(outcome: Hexalith.Projects.Authorization.TenantAccessOutcome.StaleProjection, freshness: Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Stale),
            NoReferences());

        fresh.Context.Freshness.ShouldBe(ProjectContextFreshness.Fresh);
        stale.Context.Freshness.ShouldBe(ProjectContextFreshness.Stale);
    }

    [Fact]
    public void Assemble_DoesNotMutate_InputCollections()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence evidence = WithAllKinds();
        int originalFileCount = evidence.FileReferences.Count;
        int originalMemoryCount = evidence.MemoryReferences.Count;
        int originalConversationCount = evidence.Conversations.Count;

        _ = policy.Assemble(Context(), Project(), TenantAccess(), evidence);

        evidence.FileReferences.Count.ShouldBe(originalFileCount);
        evidence.MemoryReferences.Count.ShouldBe(originalMemoryCount);
        evidence.Conversations.Count.ShouldBe(originalConversationCount);
    }

    [Fact]
    public void Assemble_EmitsEvaluationRow_PerEvaluatedCandidate()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            new ProjectContextReferenceEvidence(
                ProjectFolder: null,
                FileReferences:
                [
                    new ProjectFileReference("file_a", "folder_a", "a.txt", ReferenceState.Included, null, DefaultNow),
                    new ProjectFileReference("file_b", "folder_a", "b.txt", ReferenceState.Stale, null, DefaultNow),
                ],
                MemoryReferences: Array.Empty<ProjectMemoryReference>(),
                Conversations: Array.Empty<ProjectContextConversationEvidence>()));

        result.Evaluations.Count.ShouldBe(2);
        result.Evaluations.Select(e => e.ReferenceId).ShouldBe(["file_a", "file_b"]);
    }
}
