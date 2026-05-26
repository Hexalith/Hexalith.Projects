// <copyright file="ProjectConversationsContractTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Queries;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;

using Shouldly;

using Xunit;

/// <summary>Tier-1 contract tests for the Projects-shaped conversation reference page.</summary>
public sealed class ProjectConversationsContractTests
{
    [Fact]
    public void ProjectConversationItemShouldCarryTypedConversationReferenceWithoutUpstreamProjection()
    {
        ProjectId projectId = new("project-001");
        ConversationId conversationId = new("conversation-001");

        ProjectConversationItem item = new(
            projectId,
            conversationId,
            "Open",
            "Design thread",
            ProjectConversationTrustSignal.Current,
            "Safe project label",
            "resolved");

        item.ProjectId.ShouldBe(projectId);
        item.ConversationId.ShouldBe(conversationId);
        item.LifecycleStatus.ShouldBe("Open");
        item.DisplayLabel.ShouldBe("Design thread");
        item.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Current);
        item.ProjectSafeLabel.ShouldBe("Safe project label");
        item.ProjectSafeStatus.ShouldBe("resolved");
    }

    [Fact]
    public void PageRequestShouldRejectOutOfRangePageSize()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new PageRequest(0));
        Should.Throw<ArgumentOutOfRangeException>(() => new PageRequest(101));
        new PageRequest(100, "cursor-001").ContinuationCursor.ShouldBe("cursor-001");
    }
}
