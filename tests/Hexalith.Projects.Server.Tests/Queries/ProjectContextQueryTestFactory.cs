// <copyright file="ProjectContextQueryTestFactory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Generic;
using System.Security.Claims;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Server.Queries;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>Builds the supported Project Context query executor with a production-shaped callback principal.</summary>
internal static class ProjectContextQueryTestFactory
{
    /// <summary>Creates an executor whose complete authorization chain allows valid test evidence.</summary>
    public static ProjectContextQueryExecutor Create(
        IReadModelStore readModelStore,
        TenantAccessAuthorizer tenantAccessAuthorizer,
        IReadOnlyList<string>? callbackAudience = null,
        IProjectEventStoreAuthorizationValidator? eventStoreAuthorizationValidator = null)
    {
        ProjectQueryEnvelopePrincipalBinding principalBinding = CreatePrincipalBinding(callbackAudience);
        ProjectAuthorizationGate authorizationGate = new(
            tenantAccessAuthorizer,
            eventStoreAuthorizationValidator ?? new AllowingProjectEventStoreAuthorizationValidator(),
            new AllowingProjectDaprPolicyEvidenceProvider(),
            new InMemoryProjectDetailReadModel());

        return new ProjectContextQueryExecutor(
            readModelStore,
            authorizationGate,
            principalBinding,
            new ProjectContextInclusionPolicy());
    }

    /// <summary>Creates a production-shaped callback principal binding for query-handler tests.</summary>
    public static ProjectQueryEnvelopePrincipalBinding CreatePrincipalBinding(
        IReadOnlyList<string>? callbackAudience = null)
    {
        callbackAudience ??= ["hexalith-projects", "hexalith-eventstore"];
        List<Claim> claims =
        [
            new Claim("sub", "actor-1"),
            new Claim("tenantId", "tenant-a"),
            new Claim("eventstore:permission", ProjectAuthorizationGate.ReadProjectAction),
            new Claim("azp", "projects-callback"),
            new Claim("client_id", "eventstore-gateway"),
            new Claim("scope", "projects.read projects.list"),
            new Claim("act", "{\"sub\":\"delegation-1\"}"),
        ];
        claims.AddRange(callbackAudience.Select(static audience => new Claim("aud", audience)));

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
        };
        FixedProjectQueryHttpContextAccessor httpContextAccessor = new(httpContext);
        IConfiguration configuration = new ConfigurationBuilder().Build();
        IHostEnvironment environment = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production }).Environment;
        HttpContextProjectTenantContextAccessor tenantContextAccessor = new(httpContextAccessor);
        return new ProjectQueryEnvelopePrincipalBinding(
            httpContextAccessor,
            tenantContextAccessor,
            configuration,
            environment);
    }
}
