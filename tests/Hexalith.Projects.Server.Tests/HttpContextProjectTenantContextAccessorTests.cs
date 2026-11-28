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
            new Claim("eventstore:tenant", "current"),
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

    [Theory]
    [InlineData("eventstore:current-tenant")]
    [InlineData("eventstore:tenant")]
    [InlineData("tenantId")]
    [InlineData("tenant_id")]
    [InlineData("tid")]
    [InlineData("tenants")]
    public void SingleDistinctTenantBindsAcrossSupportedClaimTypes(string claimType)
    {
        HttpContextProjectTenantContextAccessor accessor = Accessor(new ClaimsIdentity(
        [
            new Claim("sub", "actor"),
            new Claim(claimType, "tenant-a"),
            new Claim(claimType, "tenant-a"),
        ], "test"));

        accessor.AuthoritativeTenantId.ShouldBe("tenant-a");
        EventStoreClaimTransformEvidence evidence = accessor.GetClaimTransformEvidence("projects:read");
        evidence.Malformed.ShouldBeFalse();
        evidence.TenantId.ShouldBe("tenant-a");
    }

    [Theory]
    [InlineData("eventstore:tenant")]
    [InlineData("tenants")]
    public async Task ExplicitCurrentTenantBindsTheMatchingMember(string membershipClaimType)
    {
        ClaimsIdentity identity = new(
        [
            new Claim("sub", "actor"),
            new Claim(membershipClaimType, "tenant-a"),
            new Claim(membershipClaimType, "tenant-b"),
            new Claim("eventstore:current-tenant", "tenant-b"),
        ], "test");
        HttpContextProjectTenantContextAccessor accessor = Accessor(identity);
        accessor.AuthoritativeTenantId.ShouldBe("tenant-b");

        _ = await new ProjectsClaimsTransformation().TransformAsync(new ClaimsPrincipal(identity));

        accessor.AuthoritativeTenantId.ShouldBe("tenant-b");
        accessor.GetClaimTransformEvidence("projects:read").TenantId.ShouldBe("tenant-b");
    }

    [Theory]
    [InlineData("eventstore:tenant", "tenant-a", "eventstore:tenant", "tenant-b")]
    [InlineData("tenants", "tenant-a", "tenants", "tenant-b")]
    [InlineData("tenantId", "tenant-a", "tenant_id", "tenant-b")]
    [InlineData("eventstore:tenant", "tenant-a", "tenants", "tenant-b")]
    [InlineData("eventstore:current-tenant", "tenant-b", "eventstore:tenant", "tenant-a")]
    [InlineData("eventstore:current-tenant", "tenant-b", "eventstore:tenant", "*")]
    [InlineData("eventstore:current-tenant", "tenant-a", "eventstore:current-tenant", "tenant-b")]
    [InlineData("eventstore:current-tenant", " ", "eventstore:tenant", "tenant-a")]
    [InlineData("eventstore:current-tenant", "tenant-b", "tid", "tenant-a")]
    [InlineData("eventstore:tenant", " ", "tenantId", "tenant-a")]
    [InlineData("tenantId", "tenant-a", "tenants", "[\"tenant-a\",null]")]
    [InlineData("eventstore:tenant", "*", "tenantId", "*")]
    public async Task AmbiguousOrMalformedTenantEvidenceFailsClosedBeforeAndAfterTransformation(
        string firstType,
        string firstValue,
        string secondType,
        string secondValue)
    {
        ClaimsIdentity identity = new(
        [
            new Claim("sub", "actor"),
            new Claim(firstType, firstValue),
            new Claim(secondType, secondValue),
        ], "test");
        HttpContextProjectTenantContextAccessor accessor = Accessor(identity);
        accessor.AuthoritativeTenantId.ShouldBeNull();
        accessor.GetClaimTransformEvidence("projects:read").Malformed.ShouldBeTrue();

        _ = await new ProjectsClaimsTransformation().TransformAsync(new ClaimsPrincipal(identity));

        accessor.AuthoritativeTenantId.ShouldBeNull();
        accessor.GetClaimTransformEvidence("projects:read").Malformed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("tenants", "tenant-a")]
    [InlineData("tid", "tenant-a")]
    public async Task PromotionCannotMakeConflictingRawEvidenceAuthorizeAnExplicitSelection(string claimType, string value)
    {
        ClaimsIdentity identity = new(
        [
            new Claim("sub", "actor"),
            new Claim("eventstore:current-tenant", "tenant-b"),
            new Claim("tenant_id", "tenant-b"),
            new Claim(claimType, value),
        ], "test");
        HttpContextProjectTenantContextAccessor accessor = Accessor(identity);
        accessor.AuthoritativeTenantId.ShouldBeNull();

        _ = await new ProjectsClaimsTransformation().TransformAsync(new ClaimsPrincipal(identity));
        identity.FindAll("eventstore:tenant").Select(static claim => claim.Value).ShouldContain("tenant-b");

        accessor.AuthoritativeTenantId.ShouldBeNull();
        accessor.GetClaimTransformEvidence("projects:read").Malformed.ShouldBeTrue();
    }

    private static HttpContextProjectTenantContextAccessor Accessor(params ClaimsIdentity[] identities)
        => new(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identities) },
        });
}
