// <copyright file="ControlFixtureEndpoints.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>Runner-facing control API: seeds and removes one metadata-only graph across every sibling role.</summary>
internal static class ControlFixtureEndpoints
{
    /// <summary>Maps the control routes.</summary>
    /// <param name="app">The route builder.</param>
    /// <param name="state">The control's graph registry.</param>
    public static void Map(IEndpointRouteBuilder app, LiveFixtureState state)
    {
        _ = app.MapPost("/api/v1/live-fixtures/graphs", async (
            LiveFixtureGraph graph,
            FixtureProxy proxy,
            CancellationToken cancellationToken) =>
        {
            if (!graph.IsValid())
            {
                return Results.BadRequest();
            }

            LiveFixtureGraph? existing = state.Find(item => string.Equals(item.GraphId, graph.GraphId, StringComparison.Ordinal));
            if (existing is not null && existing != graph)
            {
                return Results.Conflict();
            }

            FixtureSeedFailure? failure = await proxy.SeedAsync(graph, cancellationToken).ConfigureAwait(false);
            if (failure is not null)
            {
                return Results.Json(failure, statusCode: StatusCodes.Status502BadGateway);
            }

            return state.TryAdd(graph)
                ? Results.Created($"/api/v1/live-fixtures/graphs/{Uri.EscapeDataString(graph.GraphId)}", graph)
                : Results.Conflict();
        });
        _ = app.MapGet("/api/v1/live-fixtures/graphs/{graphId}", (string graphId) =>
        {
            LiveFixtureGraph? graph = state.Find(item => string.Equals(item.GraphId, graphId, StringComparison.Ordinal));
            return graph is null ? Results.NotFound() : Results.Ok(graph);
        });
        _ = app.MapDelete("/api/v1/live-fixtures/graphs/{graphId}", async (
            string graphId,
            FixtureProxy proxy,
            CancellationToken cancellationToken) =>
        {
            FixtureCleanupResult cleanup = await proxy.RemoveAsync(graphId, cancellationToken).ConfigureAwait(false);
            _ = state.Remove(graphId);
            return Results.Json(cleanup, statusCode: cleanup.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status502BadGateway);
        });
    }
}
