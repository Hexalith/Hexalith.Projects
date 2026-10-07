// <copyright file="ConversationsFixtureEndpoints.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

using System.Text.Json;

/// <summary>
/// Metadata-only Conversations role. Routes, headers, and wire shapes follow the published
/// <c>Hexalith.Conversations.Client</c> 1.0.0 contract (<c>api/v1/conversations</c>, tenant in
/// <c>X-Tenant-Id</c>, prefixed identifiers). Conversations never carry message or transcript content.
/// The graph's degraded Project lists three conversations whose trust is deterministically Stale,
/// Forbidden, and Unavailable; reading the Unavailable one is also slow, so loading states are observable.
/// </summary>
internal static class ConversationsFixtureEndpoints
{
    /// <summary>Gets the published Conversations client route prefix.</summary>
    public const string RoutePrefix = "/api/v1/conversations";

    private const string TenantHeader = "X-Tenant-Id";

    /// <summary>Gets the bounded latency of the unavailable conversation read.</summary>
    public static readonly TimeSpan UnavailableReadDelay = TimeSpan.FromSeconds(2);

    private static readonly ConversationFixtureTrust Current = new("Current", "current", "No action required.");
    private static readonly ConversationFixtureTrust Stale = new("Stale", "stale_threshold_exceeded", "Retry after the read model catches up.");
    private static readonly ConversationFixtureTrust Forbidden = new("Forbidden", "forbidden", "The requested conversation is not available.");
    private static readonly ConversationFixtureTrust Unavailable = new("Unavailable", "unavailable", "Retry after the read model is available.");

    /// <summary>Maps the Conversations routes consumed by the Projects conversation adapters.</summary>
    /// <param name="app">The route builder.</param>
    /// <param name="state">The role's graph registry.</param>
    public static void Map(IEndpointRouteBuilder app, LiveFixtureState state)
    {
        _ = app.MapGet(RoutePrefix, (HttpRequest request) =>
        {
            string? tenantId = request.Headers[TenantHeader].FirstOrDefault();
            string? projectId = request.Query["projectId"].FirstOrDefault();
            object[] conversations = [.. state.Graphs
                .Where(graph => string.Equals(graph.TenantId, tenantId, StringComparison.Ordinal))
                .SelectMany(graph => ListedConversations(graph, projectId))];
            return Results.Ok(new
            {
                schemaVersion = 1,
                freshnessState = "Current",
                reasonCode = "current",
                conversations,
                page = new { returnedCount = conversations.Length, continuationCursor = (string?)null },
                safeNextAction = "No action required.",
            });
        });
        _ = app.MapGet(RoutePrefix + "/{conversationId}", async (string conversationId, HttpRequest request, CancellationToken cancellationToken) =>
        {
            LiveFixtureGraph? graph = FindConversation(state, request, conversationId);
            if (graph is null)
            {
                return Results.NotFound();
            }

            if (string.Equals(conversationId, graph.UnavailableConversationId, StringComparison.Ordinal))
            {
                await Task.Delay(UnavailableReadDelay, cancellationToken).ConfigureAwait(false);
                return Results.Ok(HiddenDetail(Unavailable));
            }

            if (string.Equals(conversationId, graph.ForbiddenConversationId, StringComparison.Ordinal))
            {
                return Results.Ok(HiddenDetail(Forbidden));
            }

            ConversationFixtureTrust trust = string.Equals(conversationId, graph.StaleConversationId, StringComparison.Ordinal) ? Stale : Current;
            return Results.Ok(new
            {
                schemaVersion = 1,
                freshnessState = trust.State,
                reasonCode = trust.ReasonCode,
                details = Conversation(graph, conversationId, LinkedProject(graph, conversationId), trust),
                safeNextAction = trust.SafeNextAction,
            });
        });
        _ = app.MapPost(RoutePrefix + "/{conversationId}/project", (string conversationId, JsonElement body, HttpRequest request) =>
        {
            LiveFixtureGraph? graph = FindConversation(state, request, conversationId);
            if (graph is null)
            {
                return Results.NotFound();
            }

            if (body.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest();
            }

            return Results.Accepted(value: new
            {
                schemaVersion = 1,
                tenantId = $"tenant:{graph.TenantId}",
                conversationId = $"conv:{conversationId}",
                commandType = "ReassignConversationProjectCommand",
                correlationId = request.Headers["X-Correlation-Id"].FirstOrDefault() ?? graph.CorrelationId,
                idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault(),
                visibility = new { state = "Current", guidance = "Projection convergence is observable through the query API." },
            });
        });
    }

    private static LiveFixtureGraph? FindConversation(LiveFixtureState state, HttpRequest request, string conversationId)
    {
        string? tenantId = request.Headers[TenantHeader].FirstOrDefault();
        return state.Find(item =>
            string.Equals(item.TenantId, tenantId, StringComparison.Ordinal)
            && (string.Equals(item.ConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.AmbiguousConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.ExistingConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.StaleConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.ForbiddenConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.UnavailableConversationId, conversationId, StringComparison.Ordinal)));
    }

    private static IEnumerable<object> ListedConversations(LiveFixtureGraph graph, string? projectId)
    {
        if (string.Equals(graph.ProjectId, projectId, StringComparison.Ordinal))
        {
            yield return Conversation(graph, graph.ExistingConversationId, graph.ProjectId, Current);
        }
        else if (string.Equals(graph.DegradedProjectId, projectId, StringComparison.Ordinal))
        {
            yield return Conversation(graph, graph.StaleConversationId, graph.DegradedProjectId, Stale);
            yield return Conversation(graph, graph.ForbiddenConversationId, graph.DegradedProjectId, Forbidden);
            yield return Conversation(graph, graph.UnavailableConversationId, graph.DegradedProjectId, Unavailable);
        }
    }

    private static object HiddenDetail(ConversationFixtureTrust trust) => new
    {
        schemaVersion = 1,
        freshnessState = trust.State,
        reasonCode = trust.ReasonCode,
        details = (object?)null,
        safeNextAction = trust.SafeNextAction,
    };

    // Only the existing conversation is assigned; the unlinked and ambiguous conversations carry folder
    // and label metadata only, so Projects resolution must infer (or ask) instead of reading a link.
    private static string? LinkedProject(LiveFixtureGraph graph, string conversationId)
        => string.Equals(conversationId, graph.ExistingConversationId, StringComparison.Ordinal)
            ? graph.ProjectId
            : string.Equals(conversationId, graph.StaleConversationId, StringComparison.Ordinal) ? graph.DegradedProjectId : null;

    private static object Conversation(LiveFixtureGraph graph, string conversationId, string? projectId, ConversationFixtureTrust trust)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new
        {
            schemaVersion = 1,
            tenantId = $"tenant:{graph.TenantId}",
            conversationId = $"conv:{conversationId}",
            freshness = new
            {
                projectionContractSchemaVersion = 1,
                projectionCursor = graph.GraphId,
                lastAppliedEventPosition = 1,
                lastAppliedEventTimestamp = now.AddMilliseconds(-1),
                projectionGeneratedAt = now,
                lagDuration = "00:00:00.0010000",
                isStale = trust != Current,
                freshnessState = trust.State,
                reasonCode = trust.ReasonCode,
            },
            lifecycleState = "Open",
            label = $"Fixture conversation {graph.Scenario}",
            projectId = projectId is null ? null : $"project:{projectId}",
            folderId = $"folder:{graph.FolderId}",
            participantPartyIds = Array.Empty<string>(),
            messageCount = 0,
            fileReferenceCount = 1,
            partyHydration = Array.Empty<object>(),
        };
    }
}
