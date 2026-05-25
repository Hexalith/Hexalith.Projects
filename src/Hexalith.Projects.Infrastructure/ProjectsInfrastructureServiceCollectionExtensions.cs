// <copyright file="ProjectsInfrastructureServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

using Hexalith.Projects.Projections.TenantAccess;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

/// <summary>
/// Host-infrastructure registrations for Dapr-backed Projects runtime state.
/// </summary>
public static class ProjectsInfrastructureServiceCollectionExtensions
{
    /// <summary>Adds Dapr-backed projection and dedup stores for runtime hosts.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjectsDaprInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDaprClient();
        services.AddOptions<ProjectsStateStoreOptions>().BindConfiguration(ProjectsStateStoreOptions.SectionName);
        services.TryAddSingleton(static sp => sp.GetRequiredService<IOptions<ProjectsStateStoreOptions>>().Value);
        services.TryAddSingleton<IProjectsStateStore, DaprProjectsStateStore>();
        services.TryAddSingleton<IProjectTenantAccessProjectionStore, DaprProjectTenantAccessProjectionStore>();
        services.TryAddSingleton<IProjectProjectionStore, DaprProjectProjectionStore>();
        services.TryAddSingleton<ProjectEventProjectionProcessor>();

        _ = services.AddHealthChecks()
            .AddCheck("dapr-sidecar", () => HealthCheckResult.Healthy("Dapr sidecar configured by Aspire."), tags: ["ready", "dapr"])
            .AddCheck("dapr-state-store", () => HealthCheckResult.Healthy("Dapr state-store component configured."), tags: ["ready", "dapr", "state"])
            .AddCheck("dapr-pubsub", () => HealthCheckResult.Healthy("Dapr pub/sub component configured."), tags: ["ready", "dapr", "pubsub"])
            .AddCheck("eventstore-gateway", () => HealthCheckResult.Healthy("EventStore gateway dependency configured."), tags: ["ready", "eventstore"])
            .AddCheck("projects-projections", () => HealthCheckResult.Healthy("Project projection store configured."), tags: ["ready", "projection"]);

        return services;
    }
}
