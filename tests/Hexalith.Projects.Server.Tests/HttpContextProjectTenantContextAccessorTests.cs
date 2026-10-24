// <copyright file="HttpContextProjectTenantContextAccessorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Security.Claims;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Server;

using Microsoft.AspNetCore.Http;

using Shouldly;

using Xunit;

/// <summary>Authority is sourced from the same primary identity normalized by the claims transformer.</summary>
public sealed class HttpContextProjectTenantContextAccessorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("supplementary")]
    public void SupplementaryIdentityCannotSupplyAuthority(string? authenticationType)
    {
        ClaimsIdentity primary = new([new Claim("sub", "actor")], "test");
        ClaimsIdentity extra = new(
        [
            new Claim("eventstore:current-tenant", "foreign"),
            new Claim("tenantId", "foreign"),
            new Claim(ClaimTypes.NameIdentifier, "foreign-actor"),
            new Claim("eventstore:permission", "projects:read"),
        ], authenticationType);
        HttpContextProjectTenantContextAccessor accessor = Accessor(primary, extra);

        accessor.PrincipalId.ShouldBe("actor");
        accessor.AuthoritativeTenantId.ShouldBeNull();
        accessor.GetClaimTransformEvidence("projects:read").Malformed.ShouldBeTrue();

        primary.AddClaim(new Claim("tenantId", "tenant"));
        EventStoreClaimTransformEvidence evidence = accessor.GetClaimTransformEvidence("projects:read");
        accessor.AuthoritativeTenantId.ShouldBe("tenant");
        evidence.PrincipalId.ShouldBe("actor");
        evidence.TenantId.ShouldBe("tenant");
        evidence.HasPermissionFor("projects:read").ShouldBeFalse();

        primary.AddClaim(new Claim("eventstore:permission", "projects:read"));
        accessor.GetClaimTransformEvidence("projects:read").HasPermissionFor("projects:read").ShouldBeTrue();
    }

    [Fact]
    public void SupplementarySubjectCannotCompleteMissingPrimarySubject()
    {
        HttpContextProjectTenantContextAccessor accessor = Accessor(
            new ClaimsIdentity([new Claim("tenantId", "tenant")], "test"),
            new ClaimsIdentity([new Claim("sub", "injected")], "extra"));

        accessor.PrincipalId.ShouldBeNull();
        accessor.GetClaimTransformEvidence("projects:read").Malformed.ShouldBeTrue();
    }

    [Fact]
    public void UnauthenticatedPrimaryCannotBorrowSupplementaryAuthentication()
    {
        HttpContextProjectTenantContextAccessor accessor = Accessor(
            new ClaimsIdentity(),
            new ClaimsIdentity([new Claim("tenantId", "tenant"), new Claim("sub", "actor")], "test"));

        accessor.AuthoritativeTenantId.ShouldBeNull();
        accessor.PrincipalId.ShouldBeNull();
        accessor.GetClaimTransformEvidence("projects:read").IsPresent.ShouldBeFalse();
    }

    [Fact]
    public void PrimaryIdentityRetainsCanonicalClaimPrecedence()
    {
        HttpContextProjectTenantContextAccessor accessor = Accessor(new ClaimsIdentity(
        [
            new Claim("eventstore:current-tenant", "current"),
            new Claim("eventstore:tenant", "canonical"),
            new Claim("tenantId", "raw"),
            new Claim(ClaimTypes.NameIdentifier, "canonical-actor"),
            new Claim("sub", "raw-actor"),
            new Claim("eventstore:permission", "projects:read"),
        ], "test"));

        accessor.AuthoritativeTenantId.ShouldBe("current");
        accessor.PrincipalId.ShouldBe("canonical-actor");
        EventStoreClaimTransformEvidence evidence = accessor.GetClaimTransformEvidence("projects:read");
        evidence.TenantId.ShouldBe("current");
        evidence.PrincipalId.ShouldBe("canonical-actor");
        evidence.PermissionTokens.ShouldBe(["projects:read"]);
    }

    private static HttpContextProjectTenantContextAccessor Accessor(params ClaimsIdentity[] identities)
        => new(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identities) },
        });
}
