// <copyright file="ProjectContextQueryTestFactory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

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
        TenantAccessAuthorizer tenantAccessAuthorizer)
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "actor-1"),
                new Claim("tenantId", "tenant-a"),
                new Claim("eventstore:permission", ProjectAuthorizationGate.ReadProjectAction),
                new Claim("azp", "projects-callback"),
                new Claim("client_id", "eventstore-gateway"),
                new Claim("scope", "projects.read projects.list"),
                new Claim("aud", "hexalith-projects"),
                new Claim("aud", "hexalith-eventstore"),
                new Claim("act", "{\"sub\":\"delegation-1\"}"),
            ],
            "test")),
        };
        FixedProjectQueryHttpContextAccessor httpContextAccessor = new(httpContext);
        IConfiguration configuration = new ConfigurationBuilder().Build();
        IHostEnvironment environment = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production }).Environment;
        HttpContextProjectTenantContextAccessor tenantContextAccessor = new(httpContextAccessor);
        ProjectAuthorizationGate authorizationGate = new(
            tenantAccessAuthorizer,
            new AllowingProjectEventStoreAuthorizationValidator(),
            new AllowingProjectDaprPolicyEvidenceProvider(),
            new InMemoryProjectDetailReadModel());
        ProjectQueryEnvelopePrincipalBinding principalBinding = new(
            httpContextAccessor,
            tenantContextAccessor,
            configuration,
            environment);

        return new ProjectContextQueryExecutor(
            readModelStore,
            authorizationGate,
            principalBinding,
            new ProjectContextInclusionPolicy());
    }
}
