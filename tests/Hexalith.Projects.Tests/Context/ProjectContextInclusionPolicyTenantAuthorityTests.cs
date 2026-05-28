// <copyright file="ProjectContextInclusionPolicyTenantAuthorityTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 tests for the <see cref="ProjectContextInclusionCheck.TenantAuthority"/> branch of
/// <see cref="ProjectContextInclusionPolicy"/> (Story 3.1 AC 6, AC 12, AC 17). Asserts the
/// existence-safe collapse to <see cref="ProjectContextAssemblyOutcome.Unauthorized"/> on every
/// non-Allowed Story 1.6 outcome and the bounded-stale read-only allowance.
/// </summary>
public sealed class ProjectContextInclusionPolicyTenantAuthorityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Assemble_MissingAuthoritativeTenant_CollapsesToUnauthorized(string? authoritativeTenantId)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(authoritativeTenantId: authoritativeTenantId),
            Project(),
            TenantAccess(),
            NoReferences());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Unauthorized);
        result.Context.Conversations.ShouldBeEmpty();
        result.Context.FileReferences.ShouldBeEmpty();
        result.Context.MemoryReferences.ShouldBeEmpty();
        result.Context.Excluded.ShouldBeEmpty();
        result.Evaluations.ShouldBeEmpty();
    }

    [Fact]
    public void Assemble_RequestedTenantDiffersFromAuthoritative_CollapsesToUnauthorized()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(authoritativeTenantId: "tenant-a", requestedTenantId: "tenant-b"),
            Project(tenantId: "tenant-a"),
            TenantAccess(tenantId: "tenant-a"),
            NoReferences());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Unauthorized);
    }

    [Theory]
    [InlineData(TenantAccessOutcome.Denied)]
    [InlineData(TenantAccessOutcome.UnknownTenant)]
    [InlineData(TenantAccessOutcome.DisabledTenant)]
    [InlineData(TenantAccessOutcome.MalformedEvidence)]
    [InlineData(TenantAccessOutcome.TenantMismatch)]
    [InlineData(TenantAccessOutcome.MissingAuthoritativeTenant)]
    [InlineData(TenantAccessOutcome.ReplayConflict)]
    [InlineData(TenantAccessOutcome.UnavailableProjection)]
    public void Assemble_TenantAccessNonAllowed_CollapsesToUnauthorized(TenantAccessOutcome outcome)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(outcome: outcome),
            NoReferences());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Unauthorized);
    }

    [Theory]
    [InlineData(ProjectContextOperationKind.Get, ProjectContextAssemblyOutcome.Assembled, ProjectContextFreshness.Stale)]
    [InlineData(ProjectContextOperationKind.Refresh, ProjectContextAssemblyOutcome.Assembled, ProjectContextFreshness.Stale)]
    [InlineData(ProjectContextOperationKind.Explain, ProjectContextAssemblyOutcome.Assembled, ProjectContextFreshness.Stale)]
    [InlineData(ProjectContextOperationKind.GetConversationStartSetup, ProjectContextAssemblyOutcome.Assembled, ProjectContextFreshness.Stale)]
    public void Assemble_StaleProjection_AllowedOnReadOnlyOperations_DowngradesFreshness(
        ProjectContextOperationKind operationKind,
        ProjectContextAssemblyOutcome expectedOutcome,
        ProjectContextFreshness expectedFreshness)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(operationKind: operationKind),
            Project(),
            TenantAccess(outcome: TenantAccessOutcome.StaleProjection, freshness: TenantProjectionFreshnessStatus.Stale),
            NoReferences());

        result.Context.AssemblyOutcome.ShouldBe(expectedOutcome);
        result.Context.Freshness.ShouldBe(expectedFreshness);
    }

    [Theory]
    [InlineData(TenantProjectionFreshnessStatus.Fresh, ProjectContextFreshness.Fresh)]
    [InlineData(TenantProjectionFreshnessStatus.Stale, ProjectContextFreshness.Stale)]
    [InlineData(TenantProjectionFreshnessStatus.Unavailable, ProjectContextFreshness.Unavailable)]
    [InlineData(TenantProjectionFreshnessStatus.Future, ProjectContextFreshness.Unknown)]
    [InlineData(TenantProjectionFreshnessStatus.Unknown, ProjectContextFreshness.Unknown)]
    public void Assemble_FreshnessStatusMappedToProjectContextFreshness(
        TenantProjectionFreshnessStatus inputStatus,
        ProjectContextFreshness expected)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(freshness: inputStatus),
            NoReferences());

        result.Context.Freshness.ShouldBe(expected);
    }
}
