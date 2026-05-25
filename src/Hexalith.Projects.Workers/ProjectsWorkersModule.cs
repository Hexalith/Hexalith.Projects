// <copyright file="ProjectsWorkersModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Workers;

using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Workers.Tenants.TenantEventHandlers;
using Hexalith.Tenants.Client.Configuration;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Anchor and registration helpers for the Hexalith.Projects background workers.
/// </summary>
public static class ProjectsWorkersModule
{
    /// <summary>The Dapr app id for the Projects workers host.</summary>
    public const string AppId = ProjectsTenantEventSubscription.AppId;

    /// <summary>The internal Tenants event route.</summary>
    public const string TenantEventsRoute = ProjectsTenantEventSubscription.Route;

    /// <summary>The Dapr pub/sub component name.</summary>
    public const string TenantEventsPubSubName = ProjectsTenantEventSubscription.PubSubName;

    /// <summary>The Tenants event topic name.</summary>
    public const string TenantEventsTopicName = ProjectsTenantEventSubscription.TopicName;

    /// <summary>
    /// Gets the module name used in worker diagnostics and registration.
    /// </summary>
    public static string Name => "Hexalith.Projects.Workers";

    /// <summary>Gets the worker description exposed at the health/smoke route.</summary>
    public static string Description => $"{Name} tenant-event worker";

    /// <summary>Adds the Tenants event worker subscriptions and local projection writer.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjectsTenantEventWorkers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDaprClient();
        services.AddProjectsTenantAccess();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<HexalithTenantsOptions>, ProjectsTenantEventSubscriptionOptionsValidator>());
        services.AddHexalithTenants(options =>
        {
            options.PubSubName = TenantEventsPubSubName;
            options.TopicName = TenantEventsTopicName;
        });
        services.AddOptions<HexalithTenantsOptions>().ValidateOnStart();
        services.AddProjectsTenantEventProjection();

        return services;
    }

    /// <summary>Maps the tenant-event worker endpoints.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapProjectsTenantEventWorkerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/", () => Description);
        endpoints.MapTenantEventSubscription();

        return endpoints;
    }

    private static IServiceCollection AddProjectsTenantEventProjection(this IServiceCollection services)
    {
        services.TryAddSingleton<ProjectsTenantEventHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<TenantCreated>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<TenantUpdated>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<TenantDisabled>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<TenantEnabled>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<UserAddedToTenant>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<UserRemovedFromTenant>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<UserRoleChanged>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<TenantConfigurationSet>, ProjectsTenantEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantEventHandler<TenantConfigurationRemoved>, ProjectsTenantEventHandler>());
        return services;
    }

    private sealed class ProjectsTenantEventSubscriptionOptionsValidator : IValidateOptions<HexalithTenantsOptions>
    {
        public ValidateOptionsResult Validate(string? name, HexalithTenantsOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (!string.Equals(options.PubSubName, TenantEventsPubSubName, StringComparison.Ordinal))
            {
                return ValidateOptionsResult.Fail($"Tenants PubSubName must be '{TenantEventsPubSubName}' for {Name}.");
            }

            if (!string.Equals(options.TopicName, TenantEventsTopicName, StringComparison.Ordinal))
            {
                return ValidateOptionsResult.Fail($"Tenants TopicName must be '{TenantEventsTopicName}' for {Name}.");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
