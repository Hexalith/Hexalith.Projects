// <copyright file="ProjectFolderValidationOutcomeMapperTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Folders;

using System;
using System.Linq;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Folders;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 purity tests for <see cref="ProjectFolderValidationOutcomeMapper"/>. Pure: no infrastructure,
/// no wall-clock — <c>now</c> is passed in. Tests live in Server.Tests because the mapper lives in the
/// Server assembly; the test BODIES use no infrastructure (Tier-1 equivalent).
/// </summary>
public sealed class ProjectFolderValidationOutcomeMapperTests
{
    private static readonly DateTimeOffset ProjectionObservedAt = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Now = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ProjectFolderValidationOutcome.Accepted, ReferenceState.Included)]
    [InlineData(ProjectFolderValidationOutcome.Archived, ReferenceState.Archived)]
    [InlineData(ProjectFolderValidationOutcome.Stale, ReferenceState.Stale)]
    [InlineData(ProjectFolderValidationOutcome.Denied, ReferenceState.Unauthorized)]
    [InlineData(ProjectFolderValidationOutcome.Unavailable, ReferenceState.Unavailable)]
    [InlineData(ProjectFolderValidationOutcome.ValidationFailed, ReferenceState.InvalidReference)]
    public void Map_OutcomesProduceExpectedReferenceState(ProjectFolderValidationOutcome outcome, ReferenceState expected)
    {
        ProjectFolderReference projection = new(
            FolderId: "folder-1",
            DisplayName: "Folder One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, _) = ProjectFolderValidationOutcomeMapper.Map(outcome, projection, Now);

        state.ShouldBe(expected);
    }

    [Fact]
    public void Map_AcceptedOutcome_OverridesProjectionStoredStale_To_Included()
    {
        ProjectFolderReference projection = new(
            FolderId: "folder-1",
            DisplayName: "Folder One",
            ReferenceState: ReferenceState.Stale,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectFolderValidationOutcomeMapper.Map(
            ProjectFolderValidationOutcome.Accepted,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Included);
        observedAt.ShouldBe(Now); // state changed Stale -> Included; observedAt becomes now
    }

    [Fact]
    public void Map_UnavailableOutcome_WithProjectionStoredPending_PreservesPending()
    {
        // AC 3 Folder mapping rule: Unavailable + projection-stored Pending preserves Pending so the
        // policy continues to emit the projectFolderPending diagnostic rather than referenceUnavailable.
        ProjectFolderReference projection = new(
            FolderId: null,
            DisplayName: "Pending Folder",
            ReferenceState: ReferenceState.Pending,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectFolderValidationOutcomeMapper.Map(
            ProjectFolderValidationOutcome.Unavailable,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Pending);
        observedAt.ShouldBe(ProjectionObservedAt); // state matched Pending; observedAt preserved
    }

    [Fact]
    public void Map_PreservesObservedAt_WhenStateUnchanged()
    {
        ProjectFolderReference projection = new(
            FolderId: "folder-1",
            DisplayName: "Folder One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectFolderValidationOutcomeMapper.Map(
            ProjectFolderValidationOutcome.Accepted,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Included);
        observedAt.ShouldBe(ProjectionObservedAt);
    }

    [Fact]
    public void Map_ReplacesObservedAt_WhenStateChanges()
    {
        ProjectFolderReference projection = new(
            FolderId: "folder-1",
            DisplayName: "Folder One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectFolderValidationOutcomeMapper.Map(
            ProjectFolderValidationOutcome.Archived,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Archived);
        observedAt.ShouldBe(Now);
    }

    [Fact]
    public void Map_AllOutcomes_CoveredByTheory()
    {
        ProjectFolderReference projection = new(
            FolderId: "folder-1",
            DisplayName: "Folder One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        foreach (ProjectFolderValidationOutcome outcome in Enum.GetValues<ProjectFolderValidationOutcome>())
        {
            (ReferenceState state, _) = ProjectFolderValidationOutcomeMapper.Map(outcome, projection, Now);
            // Every enum member must map to a defined ReferenceState (not the default fallback unless
            // outcome itself is unmapped, which would surface here as a future regression).
            state.ShouldNotBe((ReferenceState)int.MinValue);
        }

        // Sanity: the enum has the expected six members; adding a new one without updating the mapper
        // would trip a future test by missing a switch arm.
        Enum.GetValues<ProjectFolderValidationOutcome>().Length.ShouldBe(6);
    }

    [Fact]
    public void Map_NullProjection_Throws()
        => Should.Throw<ArgumentNullException>(
            () => ProjectFolderValidationOutcomeMapper.Map(ProjectFolderValidationOutcome.Accepted, null!, Now));
}
