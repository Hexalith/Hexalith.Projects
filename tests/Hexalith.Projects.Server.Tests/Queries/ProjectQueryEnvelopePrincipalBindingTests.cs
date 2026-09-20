// <copyright file="ProjectQueryEnvelopePrincipalBindingTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System.Security.Claims;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Authentication;
using Hexalith.Projects.Server.Queries;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>Tests immutable query-envelope binding to the authenticated callback principal.</summary>
public sealed class ProjectQueryEnvelopePrincipalBindingTests
{
    private const string ActorId = "actor-1";
    private const string TenantId = "tenant-a";

    [Fact]
    public void TryBind_ProductionPrincipal_RequiresEveryCanonicalIdentityField()
    {
        ProjectQueryEnvelopePrincipalBinding binding = CreateProductionBinding(Principal());
        QueryEnvelope query = Query();

        binding.TryBind(query, out _).ShouldBeTrue();
        binding.TryBind(query with { OriginalActorId = "other-actor" }, out _).ShouldBeFalse();
        binding.TryBind(query with { UserId = "other-actor" }, out _).ShouldBeFalse();
        binding.TryBind(query with { TenantId = "other-tenant" }, out _).ShouldBeFalse();
        binding.TryBind(query with { AuthenticatedWorkloadId = "other-workload" }, out _).ShouldBeFalse();
        binding.TryBind(query with { IsDelegated = false }, out _).ShouldBeFalse();
        binding.TryBind(query with { DelegationId = "other-delegation" }, out _).ShouldBeFalse();
        binding.TryBind(query with { Scopes = ["projects.read"] }, out _).ShouldBeFalse();
        binding.TryBind(query with { Audience = ["hexalith-projects"] }, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryBind_DuplicateSubjectClaims_FailsClosed()
    {
        ClaimsPrincipal principal = Principal();
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("sub", ActorId));
        ProjectQueryEnvelopePrincipalBinding binding = CreateProductionBinding(principal);

        binding.TryBind(Query(), out _).ShouldBeFalse();
    }

    [Fact]
    public void TryBind_ProducerBoundedScopeAndAudienceNormalization_MatchesEnvelope()
    {
        List<Claim> claims =
        [
            new("sub", ActorId),
            new("tenantId", TenantId),
            new("azp", "workload-1"),
            new(
                "scope",
                string.Join(' ', Enumerable.Range(0, 70).Select(index =>
                    $"scope-{index:D2}-" + new string('s', 600)))),
        ];
        for (int index = 0; index < 70; index++)
        {
            claims.Add(new Claim("aud", $"audience-{index:D2}-" + new string('a', 600)));
        }

        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "test"));
        DualPrincipalIdentity normalized = DualPrincipalClaimsHelper.Extract(principal, ActorId);
        QueryEnvelope query = Query() with
        {
            AuthenticatedWorkloadId = normalized.AuthenticatedWorkloadId,
            IsDelegated = normalized.IsDelegated,
            DelegationId = normalized.DelegationId,
            Scopes = normalized.Scopes,
            Audience = normalized.Audience,
        };

        CreateProductionBinding(principal).TryBind(query, out _).ShouldBeTrue();
        normalized.Scopes!.Count.ShouldBe(64);
        normalized.Audience!.Count.ShouldBe(64);
        normalized.Scopes.ShouldAllBe(value => value.Length <= 512);
        normalized.Audience.ShouldAllBe(value => value.Length <= 512);
    }

    [Fact]
    public void TryBind_ExplicitDevelopmentBypass_AllowsLegacyEnvelopeWithoutPrincipal()
    {
        var contextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        IConfiguration configuration = Configuration(allowAnonymousDevelopment: true);
        IHostEnvironment environment = Environment(Environments.Development);
        ProjectQueryEnvelopePrincipalBinding binding = new(
            contextAccessor,
            new HttpContextProjectTenantContextAccessor(contextAccessor),
            configuration,
            environment);
        QueryEnvelope legacy = Query() with
        {
            OriginalActorId = null,
            AuthenticatedWorkloadId = null,
            IsDelegated = false,
            Scopes = null,
            Audience = null,
            DelegationId = null,
        };

        binding.TryBind(legacy, out IProjectTenantContextAccessor tenantContext).ShouldBeTrue();
        tenantContext.AuthoritativeTenantId.ShouldBe(TenantId);
        tenantContext.PrincipalId.ShouldBe(ActorId);
    }

    private static ProjectQueryEnvelopePrincipalBinding CreateProductionBinding(ClaimsPrincipal principal)
    {
        var context = new DefaultHttpContext { User = principal };
        var contextAccessor = new HttpContextAccessor { HttpContext = context };
        return new ProjectQueryEnvelopePrincipalBinding(
            contextAccessor,
            new HttpContextProjectTenantContextAccessor(contextAccessor),
            Configuration(allowAnonymousDevelopment: false),
            Environment(Environments.Production));
    }

    private static IConfiguration Configuration(bool allowAnonymousDevelopment)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ProjectsAuthenticationOptions.SectionName}:AllowAnonymousDevelopment"] = allowAnonymousDevelopment.ToString(),
            })
            .Build();

    private static IHostEnvironment Environment(string environmentName)
        => WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = environmentName }).Environment;

    private static ClaimsPrincipal Principal()
        => new(new ClaimsIdentity(
        [
            new Claim("sub", ActorId),
            new Claim("tenantId", TenantId),
            new Claim("azp", "workload-1"),
            new Claim("client_id", "different-client"),
            new Claim("scope", "projects.read projects.list"),
            new Claim("aud", "hexalith-projects"),
            new Claim("aud", "hexalith-eventstore"),
            new Claim("act", "{\"sub\":\"delegate-service\"}"),
        ],
        "test"));

    private static QueryEnvelope Query()
        => new(
            TenantId,
            ProjectsServerModule.DomainName,
            "project-1",
            ProjectsServerModule.GetProjectContextQueryType,
            [],
            "correlation-1",
            ActorId,
            entityId: "project-1",
            isGlobalAdmin: false,
            paging: null,
            originalActorId: ActorId,
            authenticatedWorkloadId: "workload-1",
            isDelegated: true,
            scopes: ["projects.read", "projects.list"],
            audience: ["hexalith-projects", "hexalith-eventstore"],
            delegationId: "delegate-service");
}
