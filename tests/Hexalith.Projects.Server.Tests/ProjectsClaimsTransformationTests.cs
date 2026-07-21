// <copyright file="ProjectsClaimsTransformationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Security.Claims;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Server;

using Shouldly;

using Xunit;

/// <summary>Tests for the Projects-local EventStore claim normalization shim.</summary>
public sealed class ProjectsClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_ShouldPromoteEventStoreTenantPrincipalAndPermissions()
    {
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("sub", "principal-a"),
                    new Claim("permissions", "[\"projects:create\",\"projects:read\"]"),
                ],
                authenticationType: "test"));
        ProjectsClaimsTransformation transformation = new();

        ClaimsPrincipal transformed = await transformation.TransformAsync(principal).ConfigureAwait(true);

        transformed.FindFirstValue("eventstore:tenant").ShouldBe("tenant-a");
        transformed.FindFirstValue(ClaimTypes.NameIdentifier).ShouldBe("principal-a");
        transformed.FindAll("eventstore:permission").Select(static claim => claim.Value).ShouldBe(
        [
            ProjectAuthorizationGate.CreateProjectAction,
            ProjectAuthorizationGate.ReadProjectAction,
        ]);
    }

    [Fact]
    public async Task TransformAsync_ShouldNotDuplicateAlreadyNormalizedEvidence()
    {
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new Claim("eventstore:tenant", "tenant-a"),
                    new Claim(ClaimTypes.NameIdentifier, "principal-a"),
                    new Claim("eventstore:permission", ProjectAuthorizationGate.CreateProjectAction),
                    new Claim("permissions", "[\"projects:read\"]"),
                ],
                authenticationType: "test"));
        ProjectsClaimsTransformation transformation = new();

        ClaimsPrincipal transformed = await transformation.TransformAsync(principal).ConfigureAwait(true);

        transformed.FindAll("eventstore:tenant").Select(static claim => claim.Value).ShouldBe(["tenant-a"]);
        transformed.FindAll("eventstore:permission").Select(static claim => claim.Value).ShouldBe([ProjectAuthorizationGate.CreateProjectAction]);
    }

    [Fact]
    public async Task TransformAsync_ShouldPreserveP2IdentityClaimsWithoutSynthesizingOptionalEvidence()
    {
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new Claim("sub", "actor-a"),
                    new Claim("azp", "projects-gateway"),
                    new Claim("act", "{\"sub\":\"delegated-service\"}"),
                    new Claim("scope", "projects.read"),
                    new Claim("aud", "hexalith-projects"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("permissions", "[\"projects:read\"]"),
                ],
                authenticationType: "test"));

        ClaimsPrincipal transformed = await new ProjectsClaimsTransformation()
            .TransformAsync(principal)
            .ConfigureAwait(true);

        transformed.FindFirstValue("sub").ShouldBe("actor-a");
        transformed.FindFirstValue("azp").ShouldBe("projects-gateway");
        transformed.FindFirstValue("act").ShouldBe("{\"sub\":\"delegated-service\"}");
        transformed.FindFirstValue("scope").ShouldBe("projects.read");
        transformed.FindFirstValue("aud").ShouldBe("hexalith-projects");
    }

    [Fact]
    public async Task TransformAsync_ShouldNotPromoteClaimsFromUnauthenticatedIdentity()
    {
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("sub", "actor-a"),
                    new Claim("permissions", "[\"projects:read\"]"),
                ]));

        ClaimsPrincipal transformed = await new ProjectsClaimsTransformation()
            .TransformAsync(principal)
            .ConfigureAwait(true);

        transformed.FindFirst("eventstore:tenant").ShouldBeNull();
        transformed.FindFirst(ClaimTypes.NameIdentifier).ShouldBeNull();
        transformed.FindFirst("eventstore:permission").ShouldBeNull();
    }

    [Fact]
    public async Task TransformAsync_ShouldNotReadEvidenceFromAnotherIdentity()
    {
        ClaimsIdentity authenticatedIdentity = new(
            [
                new Claim("tenant_id", "tenant-a"),
                new Claim("sub", "actor-a"),
                new Claim("permissions", "[\"projects:read\"]"),
            ],
            authenticationType: "validated-jwt");
        ClaimsIdentity untrustedIdentity = new(
            [
                new Claim("eventstore:tenant", "tenant-b"),
                new Claim(ClaimTypes.NameIdentifier, "actor-b"),
                new Claim("eventstore:permission", "projects:create"),
            ]);
        ClaimsPrincipal principal = new([authenticatedIdentity, untrustedIdentity]);

        _ = await new ProjectsClaimsTransformation().TransformAsync(principal).ConfigureAwait(true);

        authenticatedIdentity.FindFirst("eventstore:tenant")?.Value.ShouldBe("tenant-a");
        authenticatedIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("actor-a");
        authenticatedIdentity.FindAll("eventstore:permission").Select(static claim => claim.Value).ShouldBe(["projects:read"]);
        authenticatedIdentity.HasClaim("eventstore:tenant", "tenant-b").ShouldBeFalse();
        authenticatedIdentity.HasClaim("eventstore:permission", "projects:create").ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransformAndP2Envelope_ShouldPreserveDualPrincipalEvidenceAtEventStoreBoundary(bool delegated)
    {
        List<Claim> claims =
        [
            new Claim("sub", "actor-a"),
            new Claim("azp", "projects-gateway"),
            new Claim("client_id", "projects-gateway"),
            new Claim("scope", "projects.read projects.list"),
            new Claim("aud", "hexalith-projects"),
            new Claim("aud", "hexalith-eventstore"),
            new Claim("tenant_id", "tenant-a"),
            new Claim("permissions", "[\"projects:read\"]"),
        ];
        if (delegated)
        {
            claims.Add(new Claim("act", "{\"sub\":\"delegation-a\"}"));
        }

        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, authenticationType: "validated-jwt"));
        ClaimsPrincipal transformed = await new ProjectsClaimsTransformation()
            .TransformAsync(principal)
            .ConfigureAwait(true);
        DualPrincipalIdentity dualPrincipal = DualPrincipalClaimsHelper.Extract(transformed, "actor-a");

        QueryEnvelope envelope = new(
            tenantId: "tenant-a",
            domain: "projects",
            aggregateId: "project-a",
            queryType: "GetProject",
            payload: [],
            correlationId: "correlation-a",
            userId: "actor-a",
            entityId: null,
            isGlobalAdmin: false,
            paging: null,
            originalActorId: dualPrincipal.OriginalActorId,
            authenticatedWorkloadId: dualPrincipal.AuthenticatedWorkloadId,
            isDelegated: dualPrincipal.IsDelegated,
            scopes: dualPrincipal.Scopes,
            audience: dualPrincipal.Audience,
            delegationId: dualPrincipal.DelegationId);

        envelope.OriginalActorId.ShouldBe("actor-a");
        envelope.AuthenticatedWorkloadId.ShouldBe("projects-gateway");
        envelope.IsDelegated.ShouldBe(delegated);
        envelope.DelegationId.ShouldBe(delegated ? "delegation-a" : null);
        envelope.Scopes.ShouldBe(["projects.read", "projects.list"]);
        envelope.Audience.ShouldBe(["hexalith-projects", "hexalith-eventstore"]);
    }

    [Fact]
    public async Task TransformAsync_ShouldNotSynthesizeMalformedDelegationEvidence()
    {
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new Claim("sub", "actor-a"),
                    new Claim("act", "not-json"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("permissions", "[\"projects:read\"]"),
                ],
                authenticationType: "test"));

        ClaimsPrincipal transformed = await new ProjectsClaimsTransformation()
            .TransformAsync(principal)
            .ConfigureAwait(true);

        transformed.FindFirst("eventstore:delegation").ShouldBeNull();
        transformed.FindFirst("delegationId").ShouldBeNull();
        DualPrincipalClaimsHelper.Extract(transformed, "actor-a").DelegationId.ShouldBeNull();
    }
}
