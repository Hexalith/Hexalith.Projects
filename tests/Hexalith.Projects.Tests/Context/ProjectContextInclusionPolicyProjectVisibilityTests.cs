// <copyright file="ProjectContextInclusionPolicyProjectVisibilityTests.cs" company="Hexalith">
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
/// Tier-1 tests for the <see cref="ProjectContextInclusionCheck.ProjectVisibility"/> branch of
/// <see cref="ProjectContextInclusionPolicy"/> (Story 3.1 AC 6, AC 12, AC 17). Asserts the
/// safe-denial 404 contract: cross-tenant or null projection collapses to
/// <see cref="ProjectContextAssemblyOutcome.ProjectUnavailable"/>, never
/// <see cref="ProjectContextAssemblyOutcome.Unauthorized"/>.
/// </summary>
public sealed class ProjectContextInclusionPolicyProjectVisibilityTests
{
    [Fact]
    public void Assemble_NullDetail_ReturnsProjectUnavailable()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            ProjectMissing(),
            TenantAccess(),
            NoReferences());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
        result.Evaluations.ShouldBeEmpty();
    }

    [Fact]
    public void Assemble_CrossTenantProjectDetail_ReturnsProjectUnavailable_SafeDenial404()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(authoritativeTenantId: "tenant-a", requestedTenantId: "tenant-a"),
            Project(tenantId: "tenant-b"),
            TenantAccess(tenantId: "tenant-a"),
            WithFolder());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
        result.Context.AssemblyOutcome.ShouldNotBe(ProjectContextAssemblyOutcome.Unauthorized);
        result.Context.ProjectFolder.ShouldBeNull();
        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Conversations.ShouldBeEmpty();
    }

    [Fact]
    public void Assemble_ProjectVisibility_OutcomeStrictlyDistinguishedFromUnauthorized()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult notVisible = policy.Assemble(Context(), ProjectMissing(), TenantAccess(), NoReferences());
        ProjectContextAssemblyResult tenantMissing = policy.Assemble(Context(authoritativeTenantId: null), Project(), TenantAccess(), NoReferences());

        notVisible.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
        tenantMissing.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Unauthorized);
    }
}
