// <copyright file="ProjectContextInclusionPolicyLifecycleTests.cs" company="Hexalith">
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
/// Tier-1 tests for the <see cref="ProjectContextInclusionCheck.ProjectLifecycle"/> branch
/// (Story 3.1 AC 6, AC 12). When the owning Project is <see cref="ProjectLifecycle.Archived"/>
/// the assembly still succeeds — but every candidate reference is excluded with
/// <see cref="ProjectContextInclusionCheck.ProjectLifecycle"/> /
/// <see cref="ReferenceState.Archived"/>; the assembled result is retained for audit/explain but
/// never feeds Chatbot context.
/// </summary>
public sealed class ProjectContextInclusionPolicyLifecycleTests
{
    [Fact]
    public void Assemble_ActiveProject_IncludesAllowedReferences()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(lifecycle: ProjectLifecycle.Active),
            TenantAccess(),
            WithAllKinds());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        result.Context.ProjectFolder.ShouldNotBeNull();
        result.Context.FileReferences.Count.ShouldBe(1);
        result.Context.MemoryReferences.Count.ShouldBe(1);
        result.Context.Conversations.Count.ShouldBe(1);
        result.Context.Excluded.ShouldBeEmpty();
    }

    [Fact]
    public void Assemble_ArchivedProject_AllReferencesExcluded_WithLifecycleFailedCheck()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(lifecycle: ProjectLifecycle.Archived),
            TenantAccess(),
            WithAllKinds());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Assembled);
        result.Context.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
        result.Context.ProjectFolder.ShouldBeNull();
        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Conversations.ShouldBeEmpty();
        result.Context.Excluded.Count.ShouldBe(4);
        foreach (ProjectContextExclusion row in result.Context.Excluded)
        {
            row.FailedCheck.ShouldBe(ProjectContextInclusionCheck.ProjectLifecycle);
            row.ReferenceState.ShouldBe(ReferenceState.Archived);
            row.Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ProjectArchived);
        }
    }
}
