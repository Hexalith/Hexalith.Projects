// <copyright file="ProjectContextInclusionPolicyMemoriesCandidateTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 tests for the memory-candidate branch of
/// <see cref="ProjectContextInclusionPolicy"/> (Story 3.1 AC 6, AC 10, AC 12). Asserts the Memories
/// ACL outcomes Story 2.6 ADR defines, including the existence-safe collapse of
/// <see cref="ReferenceState.TenantMismatch"/> → boundary
/// <see cref="ReferenceState.Unauthorized"/> with closed-vocabulary
/// <see cref="ProjectContextInclusionDiagnostic.TenantMismatch"/> diagnostic.
/// </summary>
public sealed class ProjectContextInclusionPolicyMemoriesCandidateTests
{
    [Fact]
    public void Memory_Included_IsAccepted_WithMemoryMatchedReasonCode()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithMemory(ReferenceState.Included));

        result.Context.MemoryReferences.Count.ShouldBe(1);
        result.Context.MemoryReferences[0].ReasonCode.ShouldBe(ProjectReasonCode.MemoryMatched);
    }

    [Theory]
    [InlineData(ReferenceState.Archived, ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextInclusionDiagnostic.ReferenceArchived)]
    [InlineData(ReferenceState.Unauthorized, ProjectContextInclusionCheck.ReferenceAuthorization, ProjectContextInclusionDiagnostic.ReferenceUnauthorized)]
    [InlineData(ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ReferenceUnavailable)]
    [InlineData(ReferenceState.InvalidReference, ProjectContextInclusionCheck.ReferenceKindAllowlist, ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier)]
    public void Memory_NonIncludedState_IsExcluded_WithExpectedDiagnostic(
        ReferenceState state,
        ProjectContextInclusionCheck expectedCheck,
        string expectedDiagnostic)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithMemory(state));

        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Excluded.Count.ShouldBe(1);
        ProjectContextExclusion row = result.Context.Excluded[0];
        row.FailedCheck.ShouldBe(expectedCheck);
        row.Diagnostic.ShouldBe(expectedDiagnostic);
    }

    [Fact]
    public void Memory_TenantMismatch_CollapsesToUnauthorized_WithTenantMismatchDiagnostic()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithMemory(ReferenceState.TenantMismatch));

        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Excluded.Count.ShouldBe(1);
        ProjectContextExclusion row = result.Context.Excluded[0];
        row.ReferenceState.ShouldBe(ReferenceState.Unauthorized);
        row.FailedCheck.ShouldBe(ProjectContextInclusionCheck.ReferenceAuthorization);
        row.Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.TenantMismatch);
    }

    [Fact]
    public void Memory_AssembledOutput_NeverLeaksMemoriesContent()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithMemory(ReferenceState.Included));

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result.Context));
    }
}
