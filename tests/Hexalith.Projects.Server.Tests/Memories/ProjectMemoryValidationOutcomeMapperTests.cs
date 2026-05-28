// <copyright file="ProjectMemoryValidationOutcomeMapperTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Memories;

using System;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Memories;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 purity tests for <see cref="ProjectMemoryValidationOutcomeMapper"/>.
/// </summary>
public sealed class ProjectMemoryValidationOutcomeMapperTests
{
    private static readonly DateTimeOffset ProjectionObservedAt = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Now = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ProjectMemoryValidationOutcome.Accepted, ReferenceState.Included)]
    [InlineData(ProjectMemoryValidationOutcome.Denied, ReferenceState.Unauthorized)]
    [InlineData(ProjectMemoryValidationOutcome.Archived, ReferenceState.Archived)]
    [InlineData(ProjectMemoryValidationOutcome.Stale, ReferenceState.Stale)]
    [InlineData(ProjectMemoryValidationOutcome.TenantMismatch, ReferenceState.TenantMismatch)]
    [InlineData(ProjectMemoryValidationOutcome.Unavailable, ReferenceState.Unavailable)]
    [InlineData(ProjectMemoryValidationOutcome.ValidationFailed, ReferenceState.InvalidReference)]
    public void Map_OutcomesProduceExpectedReferenceState(ProjectMemoryValidationOutcome outcome, ReferenceState expected)
    {
        ProjectMemoryReference projection = new(
            MemoryReferenceId: "memory-1",
            DisplayName: "Memory One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, _) = ProjectMemoryValidationOutcomeMapper.Map(outcome, projection, Now);

        state.ShouldBe(expected);
    }

    [Fact]
    public void Map_AcceptedOutcome_OverridesProjectionStoredStale_To_Included()
    {
        ProjectMemoryReference projection = new(
            MemoryReferenceId: "memory-1",
            DisplayName: "Memory One",
            ReferenceState: ReferenceState.Stale,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectMemoryValidationOutcomeMapper.Map(
            ProjectMemoryValidationOutcome.Accepted,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Included);
        observedAt.ShouldBe(Now);
    }

    [Fact]
    public void Map_TenantMismatchOutcome_MapsTo_TenantMismatch_NotUnauthorized()
    {
        ProjectMemoryReference projection = new(
            MemoryReferenceId: "memory-1",
            DisplayName: "Memory One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, _) = ProjectMemoryValidationOutcomeMapper.Map(
            ProjectMemoryValidationOutcome.TenantMismatch,
            projection,
            Now);

        state.ShouldBe(ReferenceState.TenantMismatch);
        state.ShouldNotBe(ReferenceState.Unauthorized);
    }

    [Fact]
    public void Map_PreservesObservedAt_WhenStateUnchanged()
    {
        ProjectMemoryReference projection = new(
            MemoryReferenceId: "memory-1",
            DisplayName: "Memory One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectMemoryValidationOutcomeMapper.Map(
            ProjectMemoryValidationOutcome.Accepted,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Included);
        observedAt.ShouldBe(ProjectionObservedAt);
    }

    [Fact]
    public void Map_ReplacesObservedAt_WhenStateChanges()
    {
        ProjectMemoryReference projection = new(
            MemoryReferenceId: "memory-1",
            DisplayName: "Memory One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        (ReferenceState state, DateTimeOffset observedAt) = ProjectMemoryValidationOutcomeMapper.Map(
            ProjectMemoryValidationOutcome.Archived,
            projection,
            Now);

        state.ShouldBe(ReferenceState.Archived);
        observedAt.ShouldBe(Now);
    }

    [Fact]
    public void Map_AllOutcomes_CoveredByTheory()
    {
        ProjectMemoryReference projection = new(
            MemoryReferenceId: "memory-1",
            DisplayName: "Memory One",
            ReferenceState: ReferenceState.Included,
            ReasonCode: null,
            ObservedAt: ProjectionObservedAt);

        foreach (ProjectMemoryValidationOutcome outcome in Enum.GetValues<ProjectMemoryValidationOutcome>())
        {
            (ReferenceState state, _) = ProjectMemoryValidationOutcomeMapper.Map(outcome, projection, Now);
            state.ShouldNotBe((ReferenceState)int.MinValue);
        }

        Enum.GetValues<ProjectMemoryValidationOutcome>().Length.ShouldBe(7);
    }

    [Fact]
    public void Map_NullProjection_Throws()
        => Should.Throw<ArgumentNullException>(
            () => ProjectMemoryValidationOutcomeMapper.Map(ProjectMemoryValidationOutcome.Accepted, null!, Now));
}
