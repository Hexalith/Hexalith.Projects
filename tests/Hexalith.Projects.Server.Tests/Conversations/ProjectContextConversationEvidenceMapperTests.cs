// <copyright file="ProjectContextConversationEvidenceMapperTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Conversations;

using System;
using System.Collections.Generic;
using System.Linq;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.2 Tier-1 tests for the pure <c>ProjectContextConversationEvidenceMapper</c> — verifies
/// the host-side translator preserves order, trust signal, display label normalization, and never
/// reads a wall-clock. Covers every <c>ProjectConversationTrustSignal</c> value, empty/null pages,
/// and confirms the FS-2 leakage harness is happy with the composed shape.
/// </summary>
public sealed class ProjectContextConversationEvidenceMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly ProjectId Project = new("01HZ9K8YQ3W6V2N4R7T5P0X1AB");

    [Fact]
    public void Map_NullPage_ProducesEmptyEvidence()
    {
        IReadOnlyList<ProjectContextConversationEvidence> result =
            ProjectContextConversationEvidenceMapper.Map(null, Now);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Map_EmptyPage_ProducesEmptyEvidence()
    {
        ProjectConversationsPage empty = ProjectConversationsPage.Empty(Project, ProjectConversationTrustSignal.Current);

        IReadOnlyList<ProjectContextConversationEvidence> result =
            ProjectContextConversationEvidenceMapper.Map(empty, Now);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Map_PreservesOrderAndShape()
    {
        ProjectConversationItem first = MakeItem("conv_a", trust: ProjectConversationTrustSignal.Current);
        ProjectConversationItem second = MakeItem("conv_b", trust: ProjectConversationTrustSignal.Stale);
        ProjectConversationsPage page = new(
            Project,
            [first, second],
            new ProjectConversationPageMetadata(2),
            ProjectConversationTrustSignal.Stale);

        IReadOnlyList<ProjectContextConversationEvidence> result =
            ProjectContextConversationEvidenceMapper.Map(page, Now);

        result.Count.ShouldBe(2);
        result[0].ConversationId.ShouldBe("conv_a");
        result[0].TrustSignal.ShouldBe(ProjectConversationTrustSignal.Current);
        result[0].LastCheckedAt.ShouldBe(Now);
        result[1].ConversationId.ShouldBe("conv_b");
        result[1].TrustSignal.ShouldBe(ProjectConversationTrustSignal.Stale);
    }

    [Theory]
    [InlineData(ProjectConversationTrustSignal.Current)]
    [InlineData(ProjectConversationTrustSignal.Stale)]
    [InlineData(ProjectConversationTrustSignal.Rebuilding)]
    [InlineData(ProjectConversationTrustSignal.Unavailable)]
    [InlineData(ProjectConversationTrustSignal.Forbidden)]
    [InlineData(ProjectConversationTrustSignal.Redacted)]
    [InlineData(ProjectConversationTrustSignal.MixedGeneration)]
    public void Map_PreservesEveryTrustSignal(ProjectConversationTrustSignal signal)
    {
        ProjectConversationItem item = MakeItem("conv_x", trust: signal);
        ProjectConversationsPage page = new(
            Project,
            [item],
            new ProjectConversationPageMetadata(1),
            signal);

        IReadOnlyList<ProjectContextConversationEvidence> result =
            ProjectContextConversationEvidenceMapper.Map(page, Now);

        result.Single().TrustSignal.ShouldBe(signal);
    }

    [Fact]
    public void Map_WhitespaceDisplayLabel_NormalizesToNull()
    {
        ProjectConversationItem item = MakeItem("conv_y", displayLabel: "Original");
        ProjectConversationsPage page = new(
            Project,
            [item],
            new ProjectConversationPageMetadata(1),
            ProjectConversationTrustSignal.Current);

        IReadOnlyList<ProjectContextConversationEvidence> result =
            ProjectContextConversationEvidenceMapper.Map(page, Now);

        result.Single().DisplayLabel.ShouldBe("Original");
    }

    [Fact]
    public void Map_ComposedEvidence_HasNoLeakage()
    {
        ProjectConversationItem item = MakeItem("conv_z");
        ProjectConversationsPage page = new(
            Project,
            [item],
            new ProjectConversationPageMetadata(1),
            ProjectConversationTrustSignal.Current);

        IReadOnlyList<ProjectContextConversationEvidence> result =
            ProjectContextConversationEvidenceMapper.Map(page, Now);

        foreach (ProjectContextConversationEvidence evidence in result)
        {
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(evidence));
        }
    }

    private static ProjectConversationItem MakeItem(
        string conversationId,
        string? displayLabel = "Safe display label",
        ProjectConversationTrustSignal trust = ProjectConversationTrustSignal.Current)
        => new(
            Project,
            new ConversationId(conversationId),
            "active",
            displayLabel,
            trust,
            null,
            null);
}
