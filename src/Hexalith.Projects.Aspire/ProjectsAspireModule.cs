// <copyright file="ProjectsAspireModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aspire;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

/// <summary>
/// Aspire topology helpers and stable Dapr resource names for Hexalith.Projects.
/// </summary>
public static class ProjectsAspireModule
{
    /// <summary>The Dapr app id for EventStore.</summary>
    public const string EventStoreAppId = "eventstore";

    /// <summary>The Dapr app id for Tenants.</summary>
    public const string TenantsAppId = "tenants";

    /// <summary>The Dapr app id for the Projects API host.</summary>
    public const string ProjectsAppId = "projects";

    /// <summary>The Dapr app id for the Projects workers host.</summary>
    public const string ProjectsWorkersAppId = "projects-workers";

    /// <summary>The optional app id reserved for a future Projects UI host.</summary>
    public const string ProjectsUiAppId = "projects-ui";

    /// <summary>The Aspire Redis resource name backing local Dapr components.</summary>
    public const string RedisResourceName = "redis";

    /// <summary>The Dapr state store component name.</summary>
    public const string StateStoreComponentName = "statestore";

    /// <summary>The Dapr pub/sub component name.</summary>
    public const string PubSubComponentName = "pubsub";

    /// <summary>Registers Redis-backed Dapr components with a stable local fallback endpoint.</summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The state-store and pub/sub component builders.</returns>
    public static (IResourceBuilder<IDaprComponentResource> StateStore, IResourceBuilder<IDaprComponentResource> PubSub)
        AddProjectsSharedDaprComponents(this IDistributedApplicationBuilder builder)
        => AddProjectsSharedDaprComponents(builder, "localhost:6379");

    /// <summary>Registers Redis-backed Dapr components using the supplied Redis endpoint reference.</summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="redisEndpoint">The Redis endpoint reference.</param>
    /// <returns>The state-store and pub/sub component builders.</returns>
    public static (IResourceBuilder<IDaprComponentResource> StateStore, IResourceBuilder<IDaprComponentResource> PubSub)
        AddProjectsSharedDaprComponents(this IDistributedApplicationBuilder builder, EndpointReference redisEndpoint)
    {
        ArgumentNullException.ThrowIfNull(redisEndpoint);
        return AddProjectsSharedDaprComponentsCore(builder, component => component.WithMetadata("redisHost", redisEndpoint));
    }

    /// <summary>Registers the Projects topology resources and attaches Dapr sidecars.</summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="eventStore">The EventStore project resource.</param>
    /// <param name="tenants">The Tenants project resource.</param>
    /// <param name="projects">The Projects API project resource.</param>
    /// <param name="projectsWorkers">The Projects Workers project resource.</param>
    /// <param name="redisEndpoint">The Redis endpoint reference used by Dapr components.</param>
    /// <param name="daprConfigPath">The Dapr access-control configuration file path.</param>
    /// <param name="daprResourcesPath">The Dapr resources directory containing resiliency.yaml.</param>
    /// <returns>The resource record used by structural tests and future topology extensions.</returns>
    public static HexalithProjectsResources AddHexalithProjects(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> eventStore,
        IResourceBuilder<ProjectResource> tenants,
        IResourceBuilder<ProjectResource> projects,
        IResourceBuilder<ProjectResource> projectsWorkers,
        EndpointReference redisEndpoint,
        string daprConfigPath,
        string daprResourcesPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(tenants);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(projectsWorkers);
        ArgumentNullException.ThrowIfNull(redisEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(daprConfigPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(daprResourcesPath);

        (IResourceBuilder<IDaprComponentResource> stateStore, IResourceBuilder<IDaprComponentResource> pubSub) =
            builder.AddProjectsSharedDaprComponents(redisEndpoint);

        AttachDaprSidecar(eventStore, EventStoreAppId, daprConfigPath, daprResourcesPath, stateStore, pubSub);
        AttachDaprSidecar(tenants, TenantsAppId, daprConfigPath, daprResourcesPath, stateStore, pubSub);

        _ = projects
            .WithReference(eventStore)
            .WithReference(tenants)
            .WaitFor(eventStore)
            .WaitFor(tenants);
        AttachDaprSidecar(projects, ProjectsAppId, daprConfigPath, daprResourcesPath, stateStore, pubSub);

        _ = projectsWorkers
            .WithReference(eventStore)
            .WithReference(tenants)
            .WaitFor(eventStore)
            .WaitFor(tenants);
        AttachDaprSidecar(projectsWorkers, ProjectsWorkersAppId, daprConfigPath, daprResourcesPath, stateStore, pubSub);

        return new HexalithProjectsResources(stateStore, pubSub, eventStore, tenants, projects, projectsWorkers);
    }

    private static (IResourceBuilder<IDaprComponentResource> StateStore, IResourceBuilder<IDaprComponentResource> PubSub)
        AddProjectsSharedDaprComponents(this IDistributedApplicationBuilder builder, string redisHost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisHost);
        return AddProjectsSharedDaprComponentsCore(builder, component => component.WithMetadata("redisHost", redisHost));
    }

    private static (IResourceBuilder<IDaprComponentResource> StateStore, IResourceBuilder<IDaprComponentResource> PubSub)
        AddProjectsSharedDaprComponentsCore(
            IDistributedApplicationBuilder builder,
            Func<IResourceBuilder<IDaprComponentResource>, IResourceBuilder<IDaprComponentResource>> addRedisMetadata)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(addRedisMetadata);

        IResourceBuilder<IDaprComponentResource> stateStore = addRedisMetadata(builder
            .AddDaprComponent(StateStoreComponentName, "state.redis")
            .WithMetadata("actorStateStore", "true")
            .WithMetadata("keyPrefix", "none"));
        IResourceBuilder<IDaprComponentResource> pubSub = addRedisMetadata(builder
            .AddDaprComponent(PubSubComponentName, "pubsub.redis"));

        return (stateStore, pubSub);
    }

    private static void AttachDaprSidecar(
        IResourceBuilder<ProjectResource> project,
        string appId,
        string daprConfigPath,
        string daprResourcesPath,
        IResourceBuilder<IDaprComponentResource> stateStore,
        IResourceBuilder<IDaprComponentResource> pubSub)
    {
        _ = project.WithDaprSidecar(sidecar => sidecar
            .WithOptions(new DaprSidecarOptions
            {
                AppId = appId,
                Config = daprConfigPath,
                ResourcesPaths = [daprResourcesPath],
                AppHealthCheckPath = "/alive",
                EnableAppHealthCheck = true,
            })
            .WithReference(stateStore)
            .WithReference(pubSub));
    }
}
