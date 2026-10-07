// <copyright file="MemoriesFixtureEndpoints.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>
/// Metadata-only Memories role. The route and <c>Case</c> wire shape follow the published
/// <c>Hexalith.Memories.Client.Rest</c> 2.27.1 contract; a case never carries memory-unit content.
/// </summary>
internal static class MemoriesFixtureEndpoints
{
    /// <summary>Gets the published Memories case route.</summary>
    public const string CaseRoute = "/api/v1/tenants/{tenantId}/cases/{caseId}";

    /// <summary>Maps the Memories route consumed by the Projects memory adapter.</summary>
    /// <param name="app">The route builder.</param>
    /// <param name="state">The role's graph registry.</param>
    public static void Map(IEndpointRouteBuilder app, LiveFixtureState state)
        => _ = app.MapGet(CaseRoute, (string tenantId, string caseId) =>
        {
            LiveFixtureGraph? graph = state.Find(item =>
                string.Equals(item.TenantId, tenantId, StringComparison.Ordinal)
                && string.Equals(item.MemoryReferenceId, caseId, StringComparison.Ordinal));
            if (graph is null)
            {
                return Results.NotFound();
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            return Results.Ok(new
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
