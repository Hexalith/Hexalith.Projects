// <copyright file="ProjectContextInclusionPolicyProjectFolderCandidateTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 tests for the Project Folder branch of
/// <see cref="ProjectContextInclusionPolicy"/> (Story 3.1 AC 6, AC 12). Asserts the Story 2.4
/// invariant: there is exactly one Project Folder reference — never a list — and the folder lane
/// is disjoint from file/memory lanes in the assembled output.
/// </summary>
public sealed class ProjectContextInclusionPolicyProjectFolderCandidateTests
{
    [Fact]
    public void ProjectFolder_Included_IsExactlyOneReferenceInAssembledResult()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFolder(ReferenceState.Included));

        result.Context.ProjectFolder.ShouldNotBeNull();
        result.Context.ProjectFolder!.ReferenceKind.ShouldBe("folder");
        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Conversations.ShouldBeEmpty();
    }

    [Fact]
    public void ProjectFolder_Pending_IsExcluded_WithPendingDiagnostic()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFolder(ReferenceState.Pending, folderId: null));

        result.Context.ProjectFolder.ShouldBeNull();
        result.Context.Excluded.Count.ShouldBe(1);
        ProjectContextExclusion row = result.Context.Excluded[0];
        row.ReferenceState.ShouldBe(ReferenceState.Pending);
        row.FailedCheck.ShouldBe(ProjectContextInclusionCheck.ReferenceFreshness);
        row.Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ProjectFolderPending);
    }

    [Fact]
    public void ProjectFolder_Archived_IsExcluded_WithLifecycleCheck()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFolder(ReferenceState.Archived));

        result.Context.ProjectFolder.ShouldBeNull();
        result.Context.Excluded.Count.ShouldBe(1);
        result.Context.Excluded[0].FailedCheck.ShouldBe(ProjectContextInclusionCheck.ReferenceLifecycle);
        result.Context.Excluded[0].Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ReferenceArchived);
    }

    [Fact]
    public void ProjectFolder_Unavailable_IsExcluded_RestOfContextStillAssembles()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextReferenceEvidence evidence = new(
            ProjectFolder: new ProjectFolderReference(
                FolderId: "folder_unavailable",
                DisplayName: "Unavailable folder",
                ReferenceState: ReferenceState.Unavailable,
                ReasonCode: null,
                ObservedAt: DefaultNow),
            FileReferences:
            [
                new ProjectFileReference("file_x", "folder_other", "x.txt", ReferenceState.Included, null, DefaultNow),
            ],
            MemoryReferences: System.Array.Empty<ProjectMemoryReference>(),
            Conversations: System.Array.Empty<ProjectContextConversationEvidence>());

        ProjectContextAssemblyResult result = policy.Assemble(Context(), Project(), TenantAccess(), evidence);

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.ProjectFolder.ShouldBeNull();
        result.Context.FileReferences.Count.ShouldBe(1);
        result.Context.Excluded.Count.ShouldBe(1);
        result.Context.Excluded[0].FailedCheck.ShouldBe(ProjectContextInclusionCheck.ReferenceFreshness);
    }
}
