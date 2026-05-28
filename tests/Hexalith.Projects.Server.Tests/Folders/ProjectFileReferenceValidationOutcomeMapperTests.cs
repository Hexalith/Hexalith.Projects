// <copyright file="ProjectFileReferenceValidationOutcomeMapperTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Folders;

using System;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Folders;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 purity tests for <see cref="ProjectFileReferenceValidationOutcomeMapper"/>.
/// </summary>
public sealed class ProjectFileReferenceValidationOutcomeMapperTests
{
    private static readonly DateTimeOffset ProjectionObservedAt = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Now = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ProjectFileReferenceValidationOutcome.Accepted, ReferenceState.Included)]
    [InlineData(ProjectFileReferenceValidationOutcome.Denied, ReferenceState.Unauthorized)]
    [InlineData(ProjectFileReferenceValidationOutcome.Redacted, ReferenceState.Excluded)]
    [InlineData(ProjectFileReferenceValidationOutcome.Archived, ReferenceState.Archived)]
    [InlineData(ProjectFileReferenceValidationOutcome.Stale, ReferenceState.Stale)]
    [InlineData(ProjectFileReferenceValidationOutcome.TenantMismatch, ReferenceState.TenantMismatch)]
    [InlineData(ProjectFileReferenceValidationOutcome.Unavailable, ReferenceState.Unavailable)]
    [InlineData(ProjectFileReferenceValidationOutcome.ValidationFailed, ReferenceState.InvalidReference)]
    public void Map_OutcomesProduceExpectedReferenceState(ProjectFileReferenceValidationOutcome outcome, ReferenceState expected)
    {
        ProjectFileReference projection = new(
            FileReferenceId: "file-1",
            FolderId: "folder-1",
            DisplayName: "file.txt",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, _) = ProjectFileReferenceValidationOutcomeMapper.Map(outcome, projection, Now);

        state.ShouldBe(expected);
    }

    [Fact]
    public void Map_AcceptedOutcome_OverridesProjectionStoredStale_To_Included()
    {
        ProjectFileReference projection = new(
            FileReferenceId: "file-1",
            FolderId: "folder-1",
            DisplayName: "file.txt",
            ReferenceState: ReferenceState.Stale,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectFileReferenceValidationOutcomeMapper.Map(
            ProjectFileReferenceValidationOutcome.Accepted,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Included);
        observedAt.ShouldBe(Now);
    }

    [Fact]
    public void Map_TenantMismatchOutcome_MapsTo_TenantMismatch_NotUnauthorized()
    {
        // The mapper passes the raw TenantMismatch state to the policy. The policy itself collapses
        // TenantMismatch to Unauthorized + tenantMismatch diagnostic at the boundary; the mapper does
        // not collapse — preserving the layering (mapper translates, policy decides).
        ProjectFileReference projection = new(
            FileReferenceId: "file-1",
            FolderId: "folder-1",
            DisplayName: "file.txt",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, _) = ProjectFileReferenceValidationOutcomeMapper.Map(
            ProjectFileReferenceValidationOutcome.TenantMismatch,
            projection,
            Now);

        state.ShouldBe(ReferenceState.TenantMismatch);
        state.ShouldNotBe(ReferenceState.Unauthorized);
    }

    [Fact]
    public void Map_PreservesObservedAt_WhenStateUnchanged()
    {
        ProjectFileReference projection = new(
            FileReferenceId: "file-1",
            FolderId: "folder-1",
            DisplayName: "file.txt",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectFileReferenceValidationOutcomeMapper.Map(
            ProjectFileReferenceValidationOutcome.Accepted,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Included);
        observedAt.ShouldBe(ProjectionObservedAt);
    }

    [Fact]
    public void Map_ReplacesObservedAt_WhenStateChanges()
    {
        ProjectFileReference projection = new(
            FileReferenceId: "file-1",
            FolderId: "folder-1",
            DisplayName: "file.txt",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectFileReferenceValidationOutcomeMapper.Map(
            ProjectFileReferenceValidationOutcome.Archived,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Archived);
        observedAt.ShouldBe(Now);
    }

    [Fact]
    public void Map_AllOutcomes_CoveredByTheory()
    {
        ProjectFileReference projection = new(
            FileReferenceId: "file-1",
            FolderId: "folder-1",
            DisplayName: "file.txt",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        foreach (ProjectFileReferenceValidationOutcome outcome in Enum.GetValues<ProjectFileReferenceValidationOutcome>())
        {
            (ReferenceState state, _) = ProjectFileReferenceValidationOutcomeMapper.Map(outcome, projection, Now);
            state.ShouldNotBe((ReferenceState)int.MinValue);
        }

        Enum.GetValues<ProjectFileReferenceValidationOutcome>().Length.ShouldBe(8);
    }

    [Fact]
    public void Map_NullProjection_Throws()
        => Should.Throw<ArgumentNullException>(
            () => ProjectFileReferenceValidationOutcomeMapper.Map(ProjectFileReferenceValidationOutcome.Accepted, null!, Now));
}
