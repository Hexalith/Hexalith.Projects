// <copyright file="ProjectConversationTranslatorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Conversations;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationProjectId = Hexalith.Conversations.Contracts.Identifiers.ProjectId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using ProjectsProjectId = Hexalith.Projects.Contracts.Identifiers.ProjectId;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Conversations;

using Shouldly;

using Xunit;

/// <summary>Pure translator tests for the Projects-owned conversation reference ACL surface.</summary>
public sealed class ProjectConversationTranslatorTests
{
    private static readonly ConversationTenantId Tenant = new("tenant-a");
    private static readonly ProjectsProjectId Project = new("project-001");
    private static readonly DateTimeOffset Now = new(2026, 5, 26, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Current", "current", false, ProjectConversationTrustSignal.Current)]
    [InlineData("Stale", "stale_threshold_exceeded", true, ProjectConversationTrustSignal.Stale)]
    [InlineData("Rebuilding", "rebuilding", false, ProjectConversationTrustSignal.Rebuilding)]
    [InlineData("Unavailable", "unavailable", false, ProjectConversationTrustSignal.Unavailable)]
    [InlineData("Forbidden", "forbidden", false, ProjectConversationTrustSignal.Forbidden)]
    [InlineData("Redacted", "redacted", false, ProjectConversationTrustSignal.Redacted)]
    public void ToPageShouldMapEveryTrustStateAndEchoProjectAndConversationIds(
        string stateValue,
        string reasonValue,
        bool isStale,
        ProjectConversationTrustSignal expected)
    {
        ProjectionTrustState state = ProjectionTrustState.Parse(stateValue);
        ProjectionFreshnessReasonCode reason = ProjectionFreshnessReasonCode.Parse(reasonValue);
        ConversationListResult result = Result(
            state,
            reason,
            [
                Summary("tenant-a", "project-001", "conversation-001", state, reason, isStale),
            ]);

        ProjectConversationsPage page = ProjectConversationTranslator.ToPage(Project, Tenant, result);

        ProjectConversationItem item = page.Items.Single();
        item.ProjectId.ShouldBe(Project);
        item.ConversationId.ShouldBe(new ConversationId("conversation-001"));
        item.TrustSignal.ShouldBe(expected);
        item.ProjectSafeLabel.ShouldBe("Project project-001");
        item.ProjectSafeStatus.ShouldBe("resolved");
        page.TrustSignal.ShouldBe(expected);
    }

    [Fact]
    public void ToPageShouldPreserveEmptyResultAndPagingContinuation()
    {
        ConversationListResult result = new(
            SchemaVersion.Current,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [],
            new ConversationPageMetadata(0, "cursor-next"),
            "No accessible matches.");

        ProjectConversationsPage page = ProjectConversationTranslator.ToPage(Project, Tenant, result);

        page.Items.ShouldBeEmpty();
        page.Page.ReturnedCount.ShouldBe(0);
        page.Page.ContinuationCursor.ShouldBe("cursor-next");
        page.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Current);
    }

    [Fact]
    public void ToPageShouldMapMixedGenerationReasonToExplicitProjectsSignal()
    {
        ConversationListResult result = new(
            SchemaVersion.Current,
            ProjectionTrustState.Rebuilding,
            ProjectionFreshnessReasonCode.MixedGeneration,
            [],
            new ConversationPageMetadata(0),
            "Retry after the read model finishes rebuilding.");

        ProjectConversationsPage page = ProjectConversationTranslator.ToPage(Project, Tenant, result);

        page.TrustSignal.ShouldBe(ProjectConversationTrustSignal.MixedGeneration);
    }

    [Fact]
    public void ToPageShouldSuppressHydrationDisplayWhenHydrationProjectDoesNotMatch()
    {
        ConversationListResult result = Result(
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [
                Summary(
                    "tenant-a",
                    "project-001",
                    "conversation-001",
                    ProjectionTrustState.Current,
                    ProjectionFreshnessReasonCode.Current,
                    hydrationProjectId: "project-other"),
            ]);

        ProjectConversationsPage page = ProjectConversationTranslator.ToPage(Project, Tenant, result);

        ProjectConversationItem item = page.Items.Single();
        item.ProjectSafeLabel.ShouldBeNull();
        item.ProjectSafeStatus.ShouldBeNull();
        item.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Unavailable);
        page.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Unavailable);
    }

    private static ConversationListResult Result(
        ProjectionTrustState state,
        ProjectionFreshnessReasonCode reason,
        IReadOnlyList<ConversationSummaryV1> summaries)
        => new(
            SchemaVersion.Current,
            state,
            reason,
            summaries,
            new ConversationPageMetadata(summaries.Count),
            "Accessible results are complete for the supplied filters.");

    internal static ConversationSummaryV1 Summary(
        string tenantId,
        string projectId,
        string conversationId,
        ProjectionTrustState state,
        ProjectionFreshnessReasonCode reason,
        bool isStale = false,
        ProjectionTrustState? hydrationState = null,
        string? hydrationProjectId = null)
    {
        ConversationProjectId conversationProjectId = new(projectId);
        ConversationProjectId hydrationConversationProjectId = new(hydrationProjectId ?? projectId);
        return new ConversationSummaryV1(
            SchemaVersion.Current,
            new ConversationTenantId(tenantId),
            new ConversationId(conversationId),
            Freshness(state, reason, isStale),
            "Open",
            Label: "Conversation " + conversationId,
            ProjectId: conversationProjectId,
            ProjectHydration: new ProjectReferenceHydrationV1(
                hydrationConversationProjectId,
                hydrationState ?? state,
                Resolved: true,
                SafeLabel: "Project " + projectId,
                SafeToken: "safe",
                SafeStatus: "resolved"));
    }

    private static ProjectionFreshnessV1 Freshness(
        ProjectionTrustState state,
        ProjectionFreshnessReasonCode reason,
        bool isStale)
        => new(
            SchemaVersion.Current,
            "pos:0000000001",
            1,
            Now,
            Now.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            isStale,
            state,
            reason);
}
