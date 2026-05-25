// <copyright file="ProjectsServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects;

using Microsoft.Extensions.DependencyInjection;

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

        // Intentionally empty: domain services are wired by later Epic-1 stories.
        return services;
    }
}
