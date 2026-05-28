// <copyright file="ProjectsServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Projections.TenantAccess;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Service-registration entry point for the Hexalith.Projects domain core.
/// Wires the pure layered-fail-closed tenant-access services (Story 1.6) and the pure
/// <see cref="ProjectContextInclusionPolicy"/> AR-9 inclusion policy (Story 3.1). Later stories
/// add aggregate handlers, projections, and resolution behind this same call.
/// </summary>
public static class ProjectsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Hexalith.Projects domain core services to the service collection — tenant-access
    /// authorization (Story 1.6) and the pure context-assembly inclusion policy (Story 3.1).
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    public static IServiceCollection AddProjectsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProjectsTenantAccess();
        services.TryAddTransient<ProjectContextInclusionPolicy>();
        return services;
    }

    /// <summary>
    /// Adds the local tenant-access projection and pure fail-closed authorization services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    public static IServiceCollection AddProjectsTenantAccess(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TenantAccessOptions>().BindConfiguration(TenantAccessOptions.SectionName);
        services.AddOptions<ProjectTenantEventOptions>().BindConfiguration(ProjectTenantEventOptions.SectionName).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ProjectTenantEventOptions>, ProjectTenantEventOptionsValidator>());
        services.TryAddSingleton<IUtcClock, SystemUtcClock>();
        services.TryAddSingleton<IProjectTenantAccessProjectionStore, InMemoryProjectTenantAccessProjectionStore>();
        services.TryAddSingleton(static sp => sp.GetRequiredService<IOptions<TenantAccessOptions>>().Value);
        services.TryAddSingleton(static sp => sp.GetRequiredService<IOptions<ProjectTenantEventOptions>>().Value);
        services.TryAddSingleton<TenantAccessAuthorizer>();
        services.TryAddSingleton<ProjectTenantAccessHandler>();
        services.TryAddSingleton<ProjectsTenantAccessEventMapper>();

        return services;
    }
}
