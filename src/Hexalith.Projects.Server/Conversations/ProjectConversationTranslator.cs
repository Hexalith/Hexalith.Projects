// <copyright file="ProjectConversationTranslator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;

/// <summary>
/// Translates Conversations read contracts into Projects-owned conversation reference DTOs.
/// </summary>
internal static class ProjectConversationTranslator
{
    public static ProjectConversationsPage ToPage(
        ProjectId requestedProjectId,
        ConversationTenantId requestedTenantId,
        ConversationListResult result)
    {
        ArgumentNullException.ThrowIfNull(requestedProjectId);
        ArgumentNullException.ThrowIfNull(requestedTenantId);
        ArgumentNullException.ThrowIfNull(result);

        ProjectConversationTrustSignal resultSignal = ToTrustSignal(result.FreshnessState, result.ReasonCode);
        if (result.Conversations.Count == 0
            && resultSignal is ProjectConversationTrustSignal.Forbidden or ProjectConversationTrustSignal.Unavailable)
        {
            return ProjectConversationsPage.Empty(requestedProjectId, resultSignal);
        }

        List<ProjectConversationItem> items = [];
        foreach (ConversationSummaryV1 summary in result.Conversations)
        {
            if (!MatchesRequestedScope(summary, requestedTenantId, requestedProjectId))
            {
                return ProjectConversationsPage.Empty(
                    requestedProjectId,
                    Worst(resultSignal, ProjectConversationTrustSignal.Unavailable));
            }

            items.Add(ToItem(requestedProjectId, summary));
        }

        ProjectConversationTrustSignal aggregateSignal = resultSignal;
        foreach (ProjectConversationItem item in items)
        {
            aggregateSignal = Worst(aggregateSignal, item.TrustSignal);
        }

        return new ProjectConversationsPage(
            requestedProjectId,
            items,
            new ProjectConversationPageMetadata(items.Count, result.Page.ContinuationCursor),
            aggregateSignal);
    }

    private static ProjectConversationItem ToItem(ProjectId requestedProjectId, ConversationSummaryV1 summary)
    {
        ProjectConversationTrustSignal freshnessSignal = ToTrustSignal(summary.Freshness.FreshnessState, summary.Freshness.ReasonCode);
        ProjectConversationTrustSignal hydrationSignal = HydrationSignal(requestedProjectId, summary.ProjectHydration);
        bool hydrationMatches = MatchesHydrationProject(summary.ProjectHydration, requestedProjectId);

        return new ProjectConversationItem(
            requestedProjectId,
            summary.ConversationId,
            summary.LifecycleState,
            summary.Label,
            Worst(freshnessSignal, hydrationSignal),
            hydrationMatches ? summary.ProjectHydration?.SafeLabel : null,
            hydrationMatches ? summary.ProjectHydration?.SafeStatus : null);
    }

    private static bool MatchesRequestedScope(
        ConversationSummaryV1 summary,
        ConversationTenantId requestedTenantId,
        ProjectId requestedProjectId)
        => summary.TenantId == requestedTenantId
            && summary.ProjectId is not null
            && string.Equals(summary.ProjectId.Value, requestedProjectId.Value, StringComparison.Ordinal);

    private static ProjectConversationTrustSignal HydrationSignal(
        ProjectId requestedProjectId,
        ProjectReferenceHydrationV1? hydration)
    {
        if (hydration is null)
        {
            return ProjectConversationTrustSignal.Current;
        }

        return MatchesHydrationProject(hydration, requestedProjectId)
            ? ToTrustSignal(hydration.HydrationState)
            : ProjectConversationTrustSignal.Unavailable;
    }

    private static bool MatchesHydrationProject(ProjectReferenceHydrationV1? hydration, ProjectId requestedProjectId)
        => hydration is not null
            && string.Equals(hydration.ProjectId.Value, requestedProjectId.Value, StringComparison.Ordinal);

    private static ProjectConversationTrustSignal ToTrustSignal(
        ProjectionTrustState state,
        ProjectionFreshnessReasonCode? reasonCode = null)
    {
        if (reasonCode == ProjectionFreshnessReasonCode.MixedGeneration)
        {
            return ProjectConversationTrustSignal.MixedGeneration;
        }

        if (state == ProjectionTrustState.Current)
        {
            return ProjectConversationTrustSignal.Current;
        }

        if (state == ProjectionTrustState.Stale)
        {
            return ProjectConversationTrustSignal.Stale;
        }

        if (state == ProjectionTrustState.Rebuilding)
        {
            return ProjectConversationTrustSignal.Rebuilding;
        }

        if (state == ProjectionTrustState.Unavailable)
        {
            return ProjectConversationTrustSignal.Unavailable;
        }

        if (state == ProjectionTrustState.Forbidden)
        {
            return ProjectConversationTrustSignal.Forbidden;
        }

        if (state == ProjectionTrustState.Redacted)
        {
            return ProjectConversationTrustSignal.Redacted;
        }

        return ProjectConversationTrustSignal.Unavailable;
    }

    private static ProjectConversationTrustSignal Worst(
        ProjectConversationTrustSignal first,
        ProjectConversationTrustSignal second)
        => Priority(second) > Priority(first) ? second : first;

    private static int Priority(ProjectConversationTrustSignal signal)
        => signal switch
        {
            ProjectConversationTrustSignal.Forbidden => 7,
            ProjectConversationTrustSignal.Unavailable => 6,
            ProjectConversationTrustSignal.MixedGeneration => 5,
            ProjectConversationTrustSignal.Rebuilding => 4,
            ProjectConversationTrustSignal.Stale => 3,
            ProjectConversationTrustSignal.Redacted => 2,
            _ => 0,
        };
}
