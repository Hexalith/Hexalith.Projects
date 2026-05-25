// <copyright file="ProjectsClaimsTransformationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Security.Claims;

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
}
