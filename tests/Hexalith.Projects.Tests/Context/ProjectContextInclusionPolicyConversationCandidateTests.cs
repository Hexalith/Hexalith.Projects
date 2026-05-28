// <copyright file="ProjectContextInclusionPolicyConversationCandidateTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 tests for the conversation-candidate branch of
/// <see cref="ProjectContextInclusionPolicy"/> (Story 3.1 AC 6, AC 12). Exhaustively covers the
/// Story 2.1 <see cref="ProjectConversationTrustSignal"/> matrix.
/// </summary>
public sealed class ProjectContextInclusionPolicyConversationCandidateTests
{
    [Fact]
    public void Conversation_Current_IsIncluded_WithConversationLinkedReasonCode()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithConversation(ProjectConversationTrustSignal.Current));

        result.Context.Conversations.Count.ShouldBe(1);
        result.Context.Conversations[0].ReferenceState.ShouldBe(ReferenceState.Included);
        result.Context.Conversations[0].ReasonCode.ShouldBe(ProjectReasonCode.ConversationLinked);
        result.Context.Excluded.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ProjectConversationTrustSignal.Stale, ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.MixedGeneration, ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.Rebuilding, ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.Unavailable, ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness)]
    [InlineData(ProjectConversationTrustSignal.Forbidden, ReferenceState.Unauthorized, ProjectContextInclusionCheck.ReferenceAuthorization)]
    [InlineData(ProjectConversationTrustSignal.Redacted, ReferenceState.Excluded, ProjectContextInclusionCheck.ReferenceFreshness)]
    public void Conversation_NonCurrentSignal_IsExcluded_WithExpectedCheckAndState(
        ProjectConversationTrustSignal signal,
        ReferenceState expectedState,
        ProjectContextInclusionCheck expectedCheck)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithConversation(signal));

        result.Context.Conversations.ShouldBeEmpty();
        result.Context.Excluded.Count.ShouldBe(1);
        ProjectContextExclusion row = result.Context.Excluded[0];
        row.ReferenceState.ShouldBe(expectedState);
        row.FailedCheck.ShouldBe(expectedCheck);
        row.Diagnostic.ShouldNotBeNull();
        ProjectContextInclusionDiagnostic.IsKnown(row.Diagnostic).ShouldBeTrue();
    }
}
