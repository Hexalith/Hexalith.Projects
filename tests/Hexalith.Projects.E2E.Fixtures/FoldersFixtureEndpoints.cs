// <copyright file="FoldersFixtureEndpoints.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

using System.Text.Json;

/// <summary>
/// Metadata-only Folders role. Routes and wire shapes follow the published <c>Hexalith.Folders.Client</c>
/// 1.0.0 contract (<c>api/v2/folders</c>): folder lifecycle status, effective permissions, and file
/// context metadata. Unknown folders, workspaces, or paths collapse to the contract's safe 404 denial.
/// </summary>
internal static class FoldersFixtureEndpoints
{
    /// <summary>Gets the published Folders client route prefix.</summary>
    public const string RoutePrefix = "/api/v2/folders/{folderId}";

    /// <summary>Maps the Folders routes consumed by the Projects folder and file-reference adapters.</summary>
    /// <param name="app">The route builder.</param>
    /// <param name="state">The role's graph registry.</param>
    public static void Map(IEndpointRouteBuilder app, LiveFixtureState state)
    {
        RouteGroupBuilder folders = app.MapGroup(RoutePrefix);
        _ = folders.MapGet("/lifecycle-status", (string folderId, HttpRequest request) =>
        {
            LiveFixtureGraph? graph = FindFolder(state, folderId);
            return graph is null
                ? SafeDenial(request)
                : Results.Ok(new
                {
                    folderId,
                    lifecycleState = "ready",
                    archived = false,
                    repositoryBindingId = $"binding-{graph.GraphId}",
                    providerBindingRef = $"provider-{graph.GraphId}",
                    freshness = Freshness(graph),
                });
        });
        _ = folders.MapGet("/effective-permissions", (string folderId, HttpRequest request) =>
        {
            LiveFixtureGraph? graph = FindFolder(state, folderId);
            return graph is null
                ? SafeDenial(request)
                : Results.Ok(new
                {
                    folderId,
                    permissions = new[] { "read", "write" },
                    authorizationOutcome = "allowed",
                    freshness = Freshness(graph),
                });
        });
        _ = folders.MapPost("/workspaces/{workspaceId}/context/metadata", (
            string folderId,
            string workspaceId,
            JsonElement body,
            HttpRequest request) =>
        {
            LiveFixtureGraph? graph = FindFolder(state, folderId);
            if (graph is null || !string.Equals(graph.WorkspaceId, workspaceId, StringComparison.Ordinal))
            {
                return SafeDenial(request);
            }

            if (!TryReadRequestedPath(body, out string normalizedPath, out string? displayName))
            {
                return Results.BadRequest();
            }

            // Only the graph's single metadata-only file is visible; every other path is a safe denial
            // that never reveals whether the path exists.
            if (!string.Equals(normalizedPath, graph.FilePath, StringComparison.Ordinal))
            {
                return SafeDenial(request);
            }

            return Results.Ok(new
            {
                items = new[]
                {
                    new
                    {
                        path = new
                        {
                            normalizedPath,
                            displayName = displayName ?? normalizedPath[(normalizedPath.LastIndexOf('/') + 1)..],
                            pathPolicyClass = "metadata_only",
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

    private static LiveFixtureGraph? FindFolder(LiveFixtureState state, string folderId)
        => state.Find(item => string.Equals(item.FolderId, folderId, StringComparison.Ordinal)
            || string.Equals(item.SecondaryFolderId, folderId, StringComparison.Ordinal)
            || string.Equals(item.ProposalFolderId, folderId, StringComparison.Ordinal));

    private static bool TryReadRequestedPath(JsonElement body, out string normalizedPath, out string? displayName)
    {
        normalizedPath = string.Empty;
        displayName = null;
        if (body.ValueKind != JsonValueKind.Object
            || !body.TryGetProperty("paths", out JsonElement paths)
            || paths.ValueKind != JsonValueKind.Array
            || paths.GetArrayLength() != 1)
        {
            return false;
        }

        JsonElement path = paths[0];
        if (path.ValueKind != JsonValueKind.Object
            || !path.TryGetProperty("normalizedPath", out JsonElement normalized)
            || normalized.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(normalized.GetString()))
        {
            return false;
        }

        normalizedPath = normalized.GetString()!;
        if (path.TryGetProperty("displayName", out JsonElement name) && name.ValueKind == JsonValueKind.String)
        {
            displayName = name.GetString();
        }

        return true;
    }

    private static object Freshness(LiveFixtureGraph graph) => new
    {
        readConsistency = "eventually_consistent",
        observedAt = DateTimeOffset.UtcNow,
        projectionWatermark = $"watermark-{graph.GraphId}",
        stale = false,
    };

    private static IResult SafeDenial(HttpRequest request)
        => Results.Json(
            new
            {
                type = "https://hexalith.dev/errors/folders/resource_unavailable",
                title = "Access unavailable",
                status = StatusCodes.Status404NotFound,
                category = "tenant_access_denied",
                code = "resource_unavailable",
                message = "The requested resource is unavailable.",
                correlationId = request.Headers["X-Correlation-Id"].FirstOrDefault() ?? "unavailable",
                retryable = false,
                clientAction = "no_action",
            },
            statusCode: StatusCodes.Status404NotFound,
            contentType: "application/problem+json");
}
