// <copyright file="ProjectCreationProposalBuilderTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

/// <summary>Tier-1 proposal derivation tests for Story 4.5.</summary>
public sealed class ProjectCreationProposalBuilderTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryBuild_UsesCallerSuggestionBeforeFallbacks()
    {
        var proposal = ProjectCreationProposalBuilder.TryBuild(
            ResolutionResult.NoMatch,
            "conversation-001",
            " Synthetic Project ",
            "Conversation Label",
            "Attachment Label",
            " synthetic description ",
            " setup reference ",
            "folder-001",
            ["file-002", "file-001"],
            ObservedAt,
            "eventually_consistent");

        proposal.ShouldNotBeNull();
        proposal.SuggestedName.ShouldBe("Synthetic Project");
        proposal.Description.ShouldBe("synthetic description");
        proposal.SetupMetadata.ShouldBe("setup reference");
        proposal.FileReferenceIds.ShouldBe(["file-001", "file-002"]);
        NoPayloadLeakageAssertions.AssertNoLeakageInText(System.Text.Json.JsonSerializer.Serialize(proposal));
    }

    [Fact]
    public void TryBuild_FallsBackThroughConversationAttachmentThenDeterministicName()
    {
        ProjectCreationProposalBuilder.TryBuild(
            ResolutionResult.NoMatch,
            "conversation-001",
            callerSuggestedName: null,
            conversationLabel: "Conversation Label",
            attachmentLabel: "Attachment Label",
            description: null,
            setupMetadata: null,
            folderId: null,
            fileReferenceIds: [],
            ObservedAt,
            "eventually_consistent")!.SuggestedName.ShouldBe("Conversation Label");

        ProjectCreationProposalBuilder.TryBuild(
            ResolutionResult.NoMatch,
            "conversation-001",
            callerSuggestedName: null,
            conversationLabel: "raw transcript payload",
            attachmentLabel: "Attachment Label",
            description: null,
            setupMetadata: null,
            folderId: null,
            fileReferenceIds: [],
            ObservedAt,
            "eventually_consistent")!.SuggestedName.ShouldBe("Attachment Label");

        ProjectCreationProposalBuilder.TryBuild(
            ResolutionResult.NoMatch,
            "conversation-001",
            callerSuggestedName: null,
            conversationLabel: "raw transcript payload",
            attachmentLabel: "secret token payload",
            description: null,
            setupMetadata: null,
            folderId: null,
            fileReferenceIds: [],
            ObservedAt,
            "eventually_consistent")!.SuggestedName.ShouldBe("New project");
    }

    [Theory]
    [InlineData(ResolutionResult.SingleCandidate)]
    [InlineData(ResolutionResult.MultipleCandidates)]
    public void TryBuild_ReturnsNullUnlessResolutionIsNoMatch(ResolutionResult result)
    {
        ProjectCreationProposalBuilder.TryBuild(
            result,
            "conversation-001",
            "Synthetic Project",
            null,
            null,
            null,
            null,
            null,
            [],
            ObservedAt,
            "eventually_consistent").ShouldBeNull();
    }

    [Fact]
    public void IsSafeCreateMetadata_RejectsPayloadMarkers()
    {
        ProjectCreationProposalBuilder.IsSafeCreateMetadata("Synthetic Project", "transcript body", null).ShouldBeFalse();
        ProjectCreationProposalBuilder.IsSafeCreateMetadata("Synthetic Project", null, "secret token").ShouldBeFalse();
        ProjectCreationProposalBuilder.IsSafeCreateMetadata("docs/project", null, null).ShouldBeFalse();
    }
}
