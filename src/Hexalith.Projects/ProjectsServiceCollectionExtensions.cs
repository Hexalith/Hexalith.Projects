// <copyright file="ProjectsServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Projections.TenantAccess;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Placeholder service registration entry point for the Hexalith.Projects domain core.
/// Later stories register aggregate handlers, projections, resolution and authorization
/// services here. For the scaffold it only establishes the registration surface.
/// </summary>
public static class ProjectsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Hexalith.Projects domain core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    public static IServiceCollection AddProjectsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProjectsTenantAccess();
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
