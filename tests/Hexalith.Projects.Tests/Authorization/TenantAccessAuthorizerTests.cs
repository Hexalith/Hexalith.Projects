// <copyright file="TenantAccessAuthorizerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Authorization;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.TenantAccess;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 fail-closed matrix for the Story 1.6 tenant-access authorizer.
/// </summary>
public sealed class TenantAccessAuthorizerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MutationShouldBeAllowedOnlyWithFreshEnabledMembershipEvidence()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await store.SaveAsync(Projection("tenant-a", Now.AddMinutes(-1), enabled: true, principals: ["user-a"]), cancellationToken);
        TenantAccessAuthorizer authorizer = CreateAuthorizer(store);

        TenantAccessAuthorizationResult result = await authorizer.AuthorizeMutationAsync(
            new TenantAccessAuthorizationContext("tenant-a", "user-a", "tenant-a"),
            cancellationToken);

        result.Outcome.ShouldBe(TenantAccessOutcome.Allowed);
        result.Code.ShouldBe("allowed");
        result.FreshnessStatus.ShouldBe(TenantProjectionFreshnessStatus.Fresh);
        result.ProjectionWatermark.ShouldBe("tenant-a:7");
    }

    [Theory]
    [InlineData(null, "user-a", "tenant-a", TenantAccessOutcome.MissingAuthoritativeTenant)]
    [InlineData("tenant-a", "user-a", "tenant-b", TenantAccessOutcome.TenantMismatch)]
    [InlineData("system", "user-a", "system", TenantAccessOutcome.Denied)]
    [InlineData("tenant-a", "user-b", "tenant-a", TenantAccessOutcome.Denied)]
    public async Task MutationShouldRejectInvalidAuthorityOrPrincipal(
        string? authoritativeTenantId,
        string principalId,
        string requestedTenantId,
        TenantAccessOutcome expected)
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await store.SaveAsync(Projection("tenant-a", Now.AddMinutes(-1), enabled: true, principals: ["user-a"]), cancellationToken);
        TenantAccessAuthorizer authorizer = CreateAuthorizer(store);

        TenantAccessAuthorizationResult result = await authorizer.AuthorizeMutationAsync(
            new TenantAccessAuthorizationContext(authoritativeTenantId, principalId, requestedTenantId),
            cancellationToken);

        result.Outcome.ShouldBe(expected);
    }

    [Fact]
    public async Task MutationShouldFailClosedForUnavailableUnknownDisabledMalformedReplayOrStaleProjection()
    {
        InMemoryProjectTenantAccessProjectionStore staleStore = new();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await staleStore.SaveAsync(Projection("tenant-a", Now.AddMinutes(-10), enabled: true, principals: ["user-a"]), cancellationToken);

        InMemoryProjectTenantAccessProjectionStore disabledStore = new();
        await disabledStore.SaveAsync(Projection("tenant-a", Now.AddMinutes(-1), enabled: false, principals: ["user-a"]), cancellationToken);

        InMemoryProjectTenantAccessProjectionStore conflictStore = new();
        await conflictStore.SaveAsync(Projection("tenant-a", Now.AddMinutes(-1), enabled: true, replayConflict: true, principals: ["user-a"]), cancellationToken);

        InMemoryProjectTenantAccessProjectionStore malformedStore = new();
        await malformedStore.SaveAsync(Projection("tenant-a", Now.AddMinutes(-1), enabled: true, malformed: true, principals: ["user-a"]), cancellationToken);

        (TenantAccessAuthorizer Authorizer, TenantAccessOutcome Expected)[] cases =
        [
            (CreateAuthorizer(staleStore), TenantAccessOutcome.StaleProjection),
            (CreateAuthorizer(disabledStore), TenantAccessOutcome.DisabledTenant),
            (CreateAuthorizer(conflictStore), TenantAccessOutcome.ReplayConflict),
            (CreateAuthorizer(malformedStore), TenantAccessOutcome.MalformedEvidence),
            (CreateAuthorizer(new InMemoryProjectTenantAccessProjectionStore()), TenantAccessOutcome.UnknownTenant),
            (CreateAuthorizer(new ThrowingProjectTenantAccessProjectionStore()), TenantAccessOutcome.UnavailableProjection),
        ];

        foreach ((TenantAccessAuthorizer authorizer, TenantAccessOutcome expected) in cases)
        {
            TenantAccessAuthorizationResult result = await authorizer.AuthorizeMutationAsync(
                new TenantAccessAuthorizationContext("tenant-a", "user-a", "tenant-a"),
                cancellationToken);

            result.Outcome.ShouldBe(expected);
        }
    }

    [Fact]
    public async Task DiagnosticReadShouldAllowBoundedStaleProjectionWithFreshnessMetadata()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await store.SaveAsync(Projection("tenant-a", Now.AddMinutes(-10), enabled: true, principals: ["user-a"]), cancellationToken);
        TenantAccessAuthorizer authorizer = CreateAuthorizer(store);

        TenantAccessAuthorizationResult result = await authorizer.AuthorizeDiagnosticReadAsync(
            new TenantAccessAuthorizationContext("tenant-a", "user-a", "tenant-a"),
            cancellationToken);

        result.Outcome.ShouldBe(TenantAccessOutcome.Allowed);
        result.FreshnessStatus.ShouldBe(TenantProjectionFreshnessStatus.Stale);
        result.ProjectionAge.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void AuthorizationOrderShouldDeclareTheLayeredChainOnce()
    {
        AuthorizationOrder.LayeredProjectAuthorization.ShouldBe(
        [
            AuthorizationLayer.JwtValidation,
            AuthorizationLayer.EventStoreClaimTransform,
            AuthorizationLayer.TenantAccessFreshness,
            AuthorizationLayer.ProjectAcl,
            AuthorizationLayer.EventStoreValidator,
            AuthorizationLayer.DaprDenyByDefaultPolicy,
        ]);

        AuthorizationOrder.EffectivePermissions.ShouldContain("tenant_access_projection");
        AuthorizationOrder.EffectivePermissions.ShouldContain("project_detail_projection");
    }

    [Theory]
    [InlineData(TenantAccessOutcome.Denied, ReferenceState.Unauthorized)]
    [InlineData(TenantAccessOutcome.MissingAuthoritativeTenant, ReferenceState.Unauthorized)]
    [InlineData(TenantAccessOutcome.UnknownTenant, ReferenceState.Unauthorized)]
    [InlineData(TenantAccessOutcome.TenantMismatch, ReferenceState.TenantMismatch)]
    [InlineData(TenantAccessOutcome.StaleProjection, ReferenceState.Stale)]
    [InlineData(TenantAccessOutcome.UnavailableProjection, ReferenceState.Unavailable)]
    [InlineData(TenantAccessOutcome.MalformedEvidence, ReferenceState.Unavailable)]
    [InlineData(TenantAccessOutcome.ReplayConflict, ReferenceState.Unavailable)]
    [InlineData(TenantAccessOutcome.DisabledTenant, ReferenceState.Unavailable)]
    public void TenantAccessOutcomeShouldMapToSharedReferenceStateVocabulary(
        TenantAccessOutcome outcome,
        ReferenceState expected)
    {
        TenantAccessOutcomeReferenceStateMapper.ToReferenceState(outcome).ShouldBe(expected);
    }

    private static TenantAccessAuthorizer CreateAuthorizer(IProjectTenantAccessProjectionStore store)
        => new(store, new FixedUtcClock(Now), new TenantAccessOptions());

    private static ProjectTenantAccessProjection Projection(
        string tenantId,
        DateTimeOffset lastEventTimestamp,
        bool enabled,
        bool replayConflict = false,
        bool malformed = false,
        params string[] principals)
    {
        Dictionary<string, ProjectTenantPrincipalEvidence> principalEvidence = new(StringComparer.Ordinal);
        foreach (string principal in principals)
        {
            principalEvidence[principal] = new ProjectTenantPrincipalEvidence(principal, "Member");
        }

        return new ProjectTenantAccessProjection
        {
            TenantId = tenantId,
            Enabled = enabled,
            Principals = principalEvidence,
            Watermark = 7,
            LastEventTimestamp = lastEventTimestamp,
            ProjectionWatermark = $"{tenantId}:7",
            ReplayConflict = replayConflict,
            MalformedEvidence = malformed,
        };
    }

    private sealed class ThrowingProjectTenantAccessProjectionStore : IProjectTenantAccessProjectionStore
    {
        public Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("store unavailable");

        public Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("store unavailable");
    }
}
