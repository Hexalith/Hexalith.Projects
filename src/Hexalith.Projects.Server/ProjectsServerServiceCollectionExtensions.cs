// <copyright file="ProjectsServerServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Registration;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// DI registration for the Hexalith.Projects Server command-async + minimal-read slice (Story 1.4).
/// </summary>
public static class ProjectsServerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Projects domain processor, the in-memory detail read model, and a
    /// <see cref="TimeProvider"/> for the command-async + minimal-read endpoints. The production
    /// <see cref="IProjectCommandSubmitter"/> (EventStore gateway-backed) and the gateway client are
    /// registered by <see cref="AddProjectsServerRuntimeInfrastructure"/>; a caller may register a fake submitter
    /// for Tier-2 tests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjectsServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddProjectsTenantAccess();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<InMemoryProjectDetailReadModel>();
        services.TryAddSingleton<IProjectDetailReadModel>(sp => sp.GetRequiredService<InMemoryProjectDetailReadModel>());
        services.TryAddSingleton<InMemoryProjectListReadModel>();
        services.TryAddSingleton<IProjectListReadModel>(sp => sp.GetRequiredService<InMemoryProjectListReadModel>());
        services.TryAddSingleton<IProjectTenantContextAccessor, HttpContextProjectTenantContextAccessor>();
        services.TryAddSingleton<ProjectAuthorizationGate>();
        services.TryAddSingleton<IProjectEventStoreAuthorizationValidator, DenyAllProjectEventStoreAuthorizationValidator>();
        services.TryAddSingleton<IProjectDaprPolicyEvidenceProvider, DenyAllProjectDaprPolicyEvidenceProvider>();
        services.TryAddSingleton<IClaimsTransformation, ProjectsClaimsTransformation>();
        services.TryAddSingleton<IDomainProcessor, ProjectsDomainProcessor>();

        return services;
    }

    /// <summary>
    /// Replaces pre-runtime fakes with Dapr/EventStore-backed runtime infrastructure.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjectsServerRuntimeInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProjectsDaprInfrastructure();
        services.RemoveAll<IProjectTenantAccessProjectionStore>();
        services.AddSingleton<IProjectTenantAccessProjectionStore, DaprProjectTenantAccessProjectionStore>();
        services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        services.RemoveAll<IProjectListReadModel>();
        services.RemoveAll<InMemoryProjectListReadModel>();
        services.AddSingleton<IProjectListReadModel, DaprProjectListReadModel>();
        services.RemoveAll<IProjectDetailReadModel>();
        services.RemoveAll<InMemoryProjectDetailReadModel>();
        services.AddSingleton<IProjectDetailReadModel, DaprProjectDetailReadModel>();
        services.AddEventStoreGatewayClient(options => options.BaseAddress = new Uri("http://eventstore"));
        services.TryAddSingleton<IProjectCommandSubmitter, EventStoreProjectCommandSubmitter>();

        return services;
    }

    /// <summary>Maps the Projects Server endpoints (the command-async POST + minimal-read GET).</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapProjectsServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapProjectsDomainServiceEndpoints();
        return endpoints;
    }
}
