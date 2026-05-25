// <copyright file="ProjectTenantIsolationConformanceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.TenantIsolation;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Testing.TenantIsolation;

using Shouldly;

using Xunit;

/// <summary>Applies the reusable tenant-isolation conformance harness to pure projection surfaces.</summary>
public sealed class ProjectTenantIsolationConformanceTests
{
    [Fact]
    public async Task TenantIsolationConformance_CoversProjectionMembershipAndAuthorization()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        DateTimeOffset now = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);
        await store.SaveAsync(Projection("tenant-a", "principal-a", now), TestContext.Current.CancellationToken).ConfigureAwait(true);
        TenantAccessAuthorizer authorizer = new(store, new FixedUtcClock(now.AddMinutes(1)), new TenantAccessOptions());

        await ProjectTenantIsolationConformance.AssertNoLeakageAsync(
            [
                new ProjectTenantIsolationSurface(
                    "projection-store",
                    async cancellationToken =>
                    {
                        ProjectTenantAccessProjection? projection = await store.GetAsync("tenant-b", cancellationToken).ConfigureAwait(true);
                        return projection is null
                            ? ProjectTenantIsolationResult.NoLeak("IProjectTenantAccessProjectionStore.GetAsync")
                            : ProjectTenantIsolationResult.Leak("IProjectTenantAccessProjectionStore.GetAsync", projection.TenantId, null);
                    }),
                new ProjectTenantIsolationSurface(
                    "tenant-authorizer",
                    async cancellationToken =>
                    {
                        TenantAccessAuthorizationResult result = await authorizer.AuthorizeMutationAsync(
                            new TenantAccessAuthorizationContext("tenant-b", "principal-a", "tenant-b"),
                            cancellationToken).ConfigureAwait(true);

                        return !result.IsAllowed
                            ? ProjectTenantIsolationResult.NoLeak("TenantAccessAuthorizer.AuthorizeMutationAsync")
                            : ProjectTenantIsolationResult.Leak("TenantAccessAuthorizer.AuthorizeMutationAsync", result.TenantId, null);
                    }),
            ],
            TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    [Fact]
    public async Task TenantIsolationConformance_FailsWhenSurfaceLeaks()
    {
        await Should.ThrowAsync<ProjectTenantIsolationConformanceException>(
            async () => await ProjectTenantIsolationConformance.AssertNoLeakageAsync(
                [
                    new ProjectTenantIsolationSurface(
                        "leaky-surface",
                        _ => Task.FromResult(ProjectTenantIsolationResult.Leak("test", "tenant-a", "project-a"))),
                ],
                TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    private static ProjectTenantAccessProjection Projection(string tenantId, string principalId, DateTimeOffset timestamp)
    {
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = tenantId,
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = $"{tenantId}:1",
            LastEventTimestamp = timestamp,
        };
        projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");
        return projection;
    }
}
