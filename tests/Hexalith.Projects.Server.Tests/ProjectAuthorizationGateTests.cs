// <copyright file="ProjectAuthorizationGateTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;

using Microsoft.AspNetCore.Http;

using Shouldly;

using Xunit;

/// <summary>Tier-2 tests for the host-side layered Projects authorization gate.</summary>
public sealed class ProjectAuthorizationGateTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AuthorizeCreate_WhenAllowed_EvaluatesDeclaredLayerOrder()
    {
        IProjectTenantAccessProjectionStore store = await SeedStoreAsync("tenant-a", "principal-a").ConfigureAwait(true);
        ProjectAuthorizationGate gate = new(
            new TenantAccessAuthorizer(store, new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions()),
            new AllowingProjectEventStoreAuthorizationValidator(),
            new AllowingProjectDaprPolicyEvidenceProvider(),
            new EmptyReadModel());

        ProjectAuthorizationResult result = await gate.AuthorizeCreateAsync(
            new FixedProjectTenantContextAccessor(
                "tenant-a",
                "principal-a",
                [ProjectAuthorizationGate.CreateProjectAction]),
            new DefaultHttpContext(),
            "corr-a",
            "task-a",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsAllowed.ShouldBeTrue();
        result.EvaluatedLayers.ShouldBe(AuthorizationOrder.LayeredProjectAuthorization);
    }

    [Fact]
    public async Task AuthorizeCreate_WhenClaimTransformFails_ShortCircuitsBeforeProjection()
    {
        IProjectTenantAccessProjectionStore store = await SeedStoreAsync("tenant-a", "principal-a").ConfigureAwait(true);
        ProjectAuthorizationGate gate = new(
            new TenantAccessAuthorizer(store, new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions()),
            new AllowingProjectEventStoreAuthorizationValidator(),
            new AllowingProjectDaprPolicyEvidenceProvider(),
            new EmptyReadModel());

        ProjectAuthorizationResult result = await gate.AuthorizeCreateAsync(
            new FixedProjectTenantContextAccessor("tenant-a", "principal-a", []),
            new DefaultHttpContext(),
            "corr-a",
            "task-a",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsAllowed.ShouldBeFalse();
        result.TerminalLayer.ShouldBe(AuthorizationLayer.EventStoreClaimTransform);
        result.EvaluatedLayers.ShouldBe([AuthorizationLayer.JwtValidation, AuthorizationLayer.EventStoreClaimTransform]);
    }

    [Fact]
    public async Task AuthorizeCreate_WhenClientTenantDisagrees_ReturnsTenantMismatch()
    {
        IProjectTenantAccessProjectionStore store = await SeedStoreAsync("tenant-a", "principal-a").ConfigureAwait(true);
        ProjectAuthorizationGate gate = new(
            new TenantAccessAuthorizer(store, new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions()),
            new AllowingProjectEventStoreAuthorizationValidator(),
            new AllowingProjectDaprPolicyEvidenceProvider(),
            new EmptyReadModel());
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["X-Hexalith-Tenant-Id"] = "tenant-b";

        ProjectAuthorizationResult result = await gate.AuthorizeCreateAsync(
            new FixedProjectTenantContextAccessor(
                "tenant-a",
                "principal-a",
                [ProjectAuthorizationGate.CreateProjectAction]),
            httpContext,
            "corr-a",
            "task-a",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsAllowed.ShouldBeFalse();
        result.TerminalLayer.ShouldBe(AuthorizationLayer.EventStoreClaimTransform);
        result.Reason.ShouldBe(ReferenceState.TenantMismatch);
        result.Code.ShouldBe("tenant_mismatch");
        result.EvaluatedLayers.ShouldBe([AuthorizationLayer.JwtValidation, AuthorizationLayer.EventStoreClaimTransform]);
    }

    [Fact]
    public async Task AuthorizeCreate_WhenProjectionTenantDisagrees_FailsClosedAsMalformedEvidence()
    {
        ProjectAuthorizationGate gate = new(
            new TenantAccessAuthorizer(new MismatchedTenantProjectionStore(), new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions()),
            new AllowingProjectEventStoreAuthorizationValidator(),
            new AllowingProjectDaprPolicyEvidenceProvider(),
            new EmptyReadModel());

        ProjectAuthorizationResult result = await gate.AuthorizeCreateAsync(
            new FixedProjectTenantContextAccessor(
                "tenant-a",
                "principal-a",
                [ProjectAuthorizationGate.CreateProjectAction]),
            new DefaultHttpContext(),
            "corr-a",
            "task-a",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsAllowed.ShouldBeFalse();
        result.TerminalLayer.ShouldBe(AuthorizationLayer.TenantAccessFreshness);
        result.Reason.ShouldBe(ReferenceState.InvalidReference);
        result.Code.ShouldBe("authorization_evidence_malformed");
    }

    [Fact]
    public async Task AuthorizeCreate_WhenDaprPolicyDefaultDenies_FailsAtDaprLayer()
    {
        IProjectTenantAccessProjectionStore store = await SeedStoreAsync("tenant-a", "principal-a").ConfigureAwait(true);
        ProjectAuthorizationGate gate = new(
            new TenantAccessAuthorizer(store, new FixedUtcClock(Now.AddMinutes(1)), new TenantAccessOptions()),
            new AllowingProjectEventStoreAuthorizationValidator(),
            new DenyAllProjectDaprPolicyEvidenceProvider(),
            new EmptyReadModel());

        ProjectAuthorizationResult result = await gate.AuthorizeCreateAsync(
            new FixedProjectTenantContextAccessor(
                "tenant-a",
                "principal-a",
                [ProjectAuthorizationGate.CreateProjectAction]),
            new DefaultHttpContext(),
            "corr-a",
            "task-a",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsAllowed.ShouldBeFalse();
        result.TerminalLayer.ShouldBe(AuthorizationLayer.DaprDenyByDefaultPolicy);
        result.Code.ShouldBe("dapr_policy_denied");
    }

    private static async Task<IProjectTenantAccessProjectionStore> SeedStoreAsync(string tenantId, string principalId)
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = tenantId,
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = $"{tenantId}:1",
            LastEventTimestamp = Now,
        };
        projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");
        await store.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        return store;
    }

    private sealed class FixedProjectTenantContextAccessor(
        string tenantId,
        string principalId,
        IReadOnlyCollection<string> permissionTokens) : IProjectTenantContextAccessor
    {
        public string? AuthoritativeTenantId => tenantId;

        public string? PrincipalId => principalId;

        public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
            => EventStoreClaimTransformEvidence.Allowed(tenantId, principalId, permissionTokens);
    }

    private sealed class EmptyReadModel : IProjectDetailReadModel
    {
        public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
            => Task.FromResult<ProjectDetailItem?>(null);
    }

    private sealed class MismatchedTenantProjectionStore : IProjectTenantAccessProjectionStore
    {
        public Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            ProjectTenantAccessProjection projection = new()
            {
                TenantId = "tenant-b",
                Enabled = true,
                Watermark = 1,
                ProjectionWatermark = "tenant-b:1",
                LastEventTimestamp = Now,
            };
            projection.Principals["principal-a"] = new ProjectTenantPrincipalEvidence("principal-a", "TenantOwner");
            return Task.FromResult<ProjectTenantAccessProjection?>(projection);
        }

        public Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
