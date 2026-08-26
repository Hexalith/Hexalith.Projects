// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json;

using Hexalith.Projects.E2E.Fixtures;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<LiveFixtureState>();
builder.Services.AddHttpClient<FixtureProxy>();

WebApplication app = builder.Build();
string role = app.Configuration["FixtureRole"]?.Trim().ToLowerInvariant()
    ?? throw new InvalidOperationException("FixtureRole is required.");
LiveFixtureState state = app.Services.GetRequiredService<LiveFixtureState>();

app.MapGet("/health", () => Results.Ok(new { role, status = "ready" }));
app.MapPost("/_fixtures/graphs", (LiveFixtureGraph graph) => Results.Ok(state.Add(graph)));
app.MapDelete("/_fixtures/graphs/{graphId}", (string graphId) =>
    state.Remove(graphId) ? Results.NoContent() : Results.NotFound());

if (role == "control")
{
    app.MapPost("/api/v1/live-fixtures/graphs", async (
        LiveFixtureGraph graph,
        FixtureProxy proxy,
        CancellationToken cancellationToken) =>
    {
        graph.Validate();
        await proxy.SeedAsync(graph, cancellationToken).ConfigureAwait(false);
        return Results.Created($"/api/v1/live-fixtures/graphs/{Uri.EscapeDataString(graph.GraphId)}", state.Add(graph));
    });
    app.MapGet("/api/v1/live-fixtures/graphs/{graphId}", (string graphId) =>
    {
        LiveFixtureGraph? graph = state.Find(item => string.Equals(item.GraphId, graphId, StringComparison.Ordinal));
        return graph is null ? Results.NotFound() : Results.Ok(graph);
    });
    app.MapDelete("/api/v1/live-fixtures/graphs/{graphId}", async (
        string graphId,
        FixtureProxy proxy,
        CancellationToken cancellationToken) =>
    {
        IReadOnlyList<string> failures = await proxy.RemoveAsync(graphId, cancellationToken).ConfigureAwait(false);
        _ = state.Remove(graphId);
        return failures.Count == 0
            ? Results.NoContent()
            : Results.Problem(statusCode: 502, title: "Fixture cleanup was incomplete.", extensions: new Dictionary<string, object?>
            {
                ["failures"] = failures,
            });
    });
}
else if (role == "conversations")
{
    app.MapGet("/api/v1/conversations", (HttpRequest request) =>
    {
        string? tenantId = request.Headers["X-Tenant-Id"].FirstOrDefault();
        string? projectId = request.Query["projectId"].FirstOrDefault();
        LiveFixtureGraph[] graphs = state.Graphs
            .Where(graph => string.Equals(graph.TenantId, tenantId, StringComparison.Ordinal)
                && string.Equals(graph.ProjectId, projectId, StringComparison.Ordinal))
            .ToArray();
        object[] conversations = graphs.Select(graph => ConversationSummary(graph, graph.ExistingConversationId, graph.ProjectId)).ToArray();
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
    app.MapGet("/api/v1/conversations/{conversationId}", (string conversationId, HttpRequest request) =>
    {
        string? tenantId = request.Headers["X-Tenant-Id"].FirstOrDefault();
        LiveFixtureGraph? graph = state.Find(item =>
            string.Equals(item.TenantId, tenantId, StringComparison.Ordinal)
            && (string.Equals(item.ConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.AmbiguousConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.ExistingConversationId, conversationId, StringComparison.Ordinal)));
        if (graph is null)
        {
            return Results.NotFound();
        }

        string? projectId = string.Equals(conversationId, graph.ExistingConversationId, StringComparison.Ordinal)
            ? graph.ProjectId
            : null;
        return Results.Ok(new
        {
            schemaVersion = 1,
            freshnessState = "Current",
            reasonCode = "current",
            details = ConversationSummary(graph, conversationId, projectId),
            safeNextAction = "No action required.",
        });
    });
    app.MapPost("/api/v1/conversations/{conversationId}/project", (string conversationId, JsonElement body, HttpRequest request) =>
    {
        string? tenantId = request.Headers["X-Tenant-Id"].FirstOrDefault();
        LiveFixtureGraph? graph = state.Find(item =>
            string.Equals(item.TenantId, tenantId, StringComparison.Ordinal)
            && (string.Equals(item.ConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.AmbiguousConversationId, conversationId, StringComparison.Ordinal)
                || string.Equals(item.ExistingConversationId, conversationId, StringComparison.Ordinal)));
        if (graph is null)
        {
            return Results.NotFound();
        }

        string correlationId = request.Headers["X-Correlation-Id"].FirstOrDefault() ?? graph.GraphId;
        string? idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault();
        _ = body;
        return Results.Accepted(value: new
        {
            schemaVersion = 1,
            tenantId = graph.TenantId,
            conversationId,
            commandType = "ReassignConversationProjectCommand",
            correlationId,
            idempotencyKey,
            visibility = new { state = "Current", guidance = "Projection convergence is observable through the query API." },
        });
    });
}
else if (role == "folders")
{
    app.MapGet("/api/v1/folders/{folderId}/lifecycle-status", (string folderId) =>
    {
        LiveFixtureGraph? graph = FindFolder(state, folderId);
        return graph is null ? Results.NotFound() : Results.Ok(new
        {
            folderId,
            lifecycleState = "ready",
            archived = false,
            repositoryBindingId = $"binding-{graph.GraphId}",
            providerBindingRef = $"provider-{graph.GraphId}",
            freshness = Freshness(graph),
        });
    });
    app.MapGet("/api/v1/folders/{folderId}/effective-permissions", (string folderId) =>
        FindFolder(state, folderId) is null
            ? Results.NotFound()
            : Results.Ok(new
            {
                folderId,
                permissions = new[] { "read", "write" },
                authorizationOutcome = "allowed",
                freshness = Freshness(FindFolder(state, folderId)!),
            }));
    app.MapPost("/api/v1/folders/{folderId}/workspaces/{workspaceId}/context/metadata", (
        string folderId,
        string workspaceId,
        JsonElement body) =>
    {
        LiveFixtureGraph? graph = state.Find(item =>
            (string.Equals(item.FolderId, folderId, StringComparison.Ordinal)
                || string.Equals(item.SecondaryFolderId, folderId, StringComparison.Ordinal)
                || string.Equals(item.ProposalFolderId, folderId, StringComparison.Ordinal))
            && string.Equals(item.WorkspaceId, workspaceId, StringComparison.Ordinal));
        if (graph is null)
        {
            return Results.NotFound();
        }

        string path = body.GetProperty("paths")[0].GetProperty("normalizedPath").GetString() ?? string.Empty;
        if (!string.Equals(path, graph.FilePath, StringComparison.Ordinal) || path.StartsWith("secret/", StringComparison.Ordinal))
        {
            return Results.NotFound();
        }

        string displayName = path[(path.LastIndexOf('/') + 1)..];
        return Results.Ok(new
        {
            items = new[]
            {
                new
                {
                    path = new
                    {
                        normalizedPath = path,
                        displayName,
                        pathPolicyClass = "tenant_sensitive_document",
                        unicodeNormalization = "NFC",
                    },
                    kind = "file",
                    byteLength = 128,
                    sensitivity = "tenant_sensitive",
                    redaction = "not_redacted",
                },
            },
            limits = new
            {
                queryFamily = "metadata",
                configuredLimit = 100,
                actualCount = 1,
                actualBytes = 128,
                elapsedMilliseconds = 1,
                isTruncated = false,
                truncatedReason = "not_truncated",
            },
            freshness = Freshness(graph),
        });
    });
}
else if (role == "memories")
{
    app.MapGet("/api/v1/tenants/{tenantId}/cases/{caseId}", (string tenantId, string caseId) =>
    {
        LiveFixtureGraph? graph = state.Find(item =>
            string.Equals(item.TenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(item.MemoryReferenceId, caseId, StringComparison.Ordinal));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return graph is null ? Results.NotFound() : Results.Ok(new
        {
            id = caseId,
            tenantId,
            name = $"Fixture memory {graph.Scenario}",
            description = "Metadata-only E2E fixture.",
            status = "active",
            createdAt = now.AddMinutes(-1),
            lastUpdated = now,
            memoryUnitCount = 0,
        });
    });
}
else
{
    throw new InvalidOperationException($"Unsupported FixtureRole '{role}'.");
}

app.Run();

static LiveFixtureGraph? FindFolder(LiveFixtureState state, string folderId)
    => state.Find(item => string.Equals(item.FolderId, folderId, StringComparison.Ordinal)
        || string.Equals(item.SecondaryFolderId, folderId, StringComparison.Ordinal)
        || string.Equals(item.ProposalFolderId, folderId, StringComparison.Ordinal));

static object Freshness(LiveFixtureGraph graph) => new
{
    readConsistency = "eventually_consistent",
    observedAt = DateTimeOffset.UtcNow,
    projectionWatermark = graph.GraphId,
    stale = false,
};

static object ConversationSummary(LiveFixtureGraph graph, string conversationId, string? projectId)
{
    DateTimeOffset now = DateTimeOffset.UtcNow;
    return new
    {
        schemaVersion = 1,
        tenantId = graph.TenantId,
        conversationId,
        freshness = new
        {
            projectionContractSchemaVersion = 1,
            projectionCursor = graph.GraphId,
            lastAppliedEventPosition = 1,
            lastAppliedEventTimestamp = now.AddMilliseconds(-1),
            projectionGeneratedAt = now,
            lagDuration = "00:00:00.0010000",
            isStale = false,
            freshnessState = "Current",
            reasonCode = "current",
        },
        lifecycleState = "Open",
        label = $"Fixture conversation {graph.Scenario}",
        projectId,
        folderId = graph.FolderId,
        participantPartyIds = Array.Empty<string>(),
        messageCount = 0,
        fileReferenceCount = 1,
        partyHydration = Array.Empty<object>(),
    };
}
