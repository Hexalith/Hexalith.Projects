// <copyright file="ProjectContextInclusionPolicyFileReferenceCandidateTests.cs" company="Hexalith">
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
/// Tier-1 tests for the file-reference candidate branch of
/// <see cref="ProjectContextInclusionPolicy"/> (Story 3.1 AC 6, AC 12). Covers every
/// <see cref="ReferenceState"/> the Story 2.5 Folders file-metadata ACL produces, plus the
/// Story 2.4 <see cref="ReferenceState.Pending"/> degraded folder path.
/// </summary>
public sealed class ProjectContextInclusionPolicyFileReferenceCandidateTests
{
    [Fact]
    public void File_Included_IsAccepted_WithFileReferenceMatchedReasonCode()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFile(ReferenceState.Included));

        result.Context.FileReferences.Count.ShouldBe(1);
        result.Context.FileReferences[0].ReasonCode.ShouldBe(ProjectReasonCode.FileReferenceMatched);
    }

    [Theory]
    [InlineData(ReferenceState.Archived, ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextInclusionDiagnostic.ReferenceArchived)]
    [InlineData(ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ReferenceStale)]
    [InlineData(ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ReferenceUnavailable)]
    [InlineData(ReferenceState.Unauthorized, ProjectContextInclusionCheck.ReferenceAuthorization, ProjectContextInclusionDiagnostic.ReferenceUnauthorized)]
    [InlineData(ReferenceState.InvalidReference, ProjectContextInclusionCheck.ReferenceKindAllowlist, ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier)]
    [InlineData(ReferenceState.Pending, ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ProjectFolderPending)]
    public void File_NonIncludedState_IsExcluded_WithExpectedDiagnostic(
        ReferenceState state,
        ProjectContextInclusionCheck expectedCheck,
        string expectedDiagnostic)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithFile(state));

        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.Excluded.Count.ShouldBe(1);
        ProjectContextExclusion row = result.Context.Excluded[0];
        row.FailedCheck.ShouldBe(expectedCheck);
        row.Diagnostic.ShouldBe(expectedDiagnostic);
    }
}
