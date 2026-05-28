// <copyright file="ProjectContextInclusionPolicyCrossTenantTests.cs" company="Hexalith">
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
/// Reuses the FS-8 / SM-3 cross-tenant isolation pattern from Story 1.4: an authoritative tenant
/// looking up a project owned by another tenant gets a safe-denial
/// <see cref="ProjectContextAssemblyOutcome.ProjectUnavailable"/> verdict — never
/// <see cref="ReferenceState.TenantMismatch"/> at the assembly boundary.
/// </summary>
public sealed class ProjectContextInclusionPolicyCrossTenantTests
{
    [Fact]
    public void Assemble_CrossTenantProject_ReturnsProjectUnavailable_NeverTenantMismatchAtBoundary()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(authoritativeTenantId: "tenant-a", requestedTenantId: "tenant-a"),
            Project(tenantId: "tenant-b"),
            TenantAccess(tenantId: "tenant-a"),
            WithAllKinds());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
        result.Context.AssemblyOutcome.ShouldNotBe(ProjectContextAssemblyOutcome.Unauthorized);
        result.Context.ProjectFolder.ShouldBeNull();
        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Conversations.ShouldBeEmpty();
        result.Context.Excluded.ShouldBeEmpty();
    }
}
