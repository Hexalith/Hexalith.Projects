// <copyright file="AspireTopologyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Projects.AppHost;
using Hexalith.Projects.Aspire;

using Shouldly;

using Xunit;

/// <summary>
/// Structural tests for the Projects Aspire topology surface.
/// </summary>
public sealed class AspireTopologyTests
{
    /// <summary>Verifies stable app IDs and component names used by Dapr and runbooks.</summary>
    [Fact]
    public void ProjectsAspireModuleShouldExposeStableDaprAppIdsAndComponentNames()
    {
        ProjectsAspireModule.EventStoreAppId.ShouldBe("eventstore");
        ProjectsAspireModule.TenantsAppId.ShouldBe("tenants");
        ProjectsAspireModule.ProjectsAppId.ShouldBe("projects");
        ProjectsAspireModule.ProjectsWorkersAppId.ShouldBe("projects-workers");
        ProjectsAspireModule.ProjectsUiAppId.ShouldBe("projects-ui");
        ProjectsAspireModule.RedisResourceName.ShouldBe("redis");
        ProjectsAspireModule.StateStoreComponentName.ShouldBe("statestore");
        ProjectsAspireModule.PubSubComponentName.ShouldBe("pubsub");
    }

    /// <summary>Verifies shared Dapr components are modeled as Redis-backed resources.</summary>
    [Fact]
    public void AddProjectsSharedDaprComponentsShouldRegisterRedisBackedStateStoreAndPubSub()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();

        (IResourceBuilder<IDaprComponentResource> stateStore, IResourceBuilder<IDaprComponentResource> pubSub) =
            builder.AddProjectsSharedDaprComponents();

        stateStore.Resource.Name.ShouldBe(ProjectsAspireModule.StateStoreComponentName);
        stateStore.Resource.Type.ShouldBe("state.redis");
        pubSub.Resource.Name.ShouldBe(ProjectsAspireModule.PubSubComponentName);
        pubSub.Resource.Type.ShouldBe("pubsub.redis");

        IResource[] resources = [.. builder.Resources];
        resources.ShouldContain(r => string.Equals(r.Name, ProjectsAspireModule.StateStoreComponentName, StringComparison.Ordinal));
        resources.ShouldContain(r => string.Equals(r.Name, ProjectsAspireModule.PubSubComponentName, StringComparison.Ordinal));
    }

    /// <summary>Verifies the resource record remains a complete topology contract.</summary>
    [Fact]
    public void HexalithProjectsResourcesShouldExposeRequiredProjectAndComponentBuilders()
    {
        string[] names = [.. typeof(HexalithProjectsResources).GetProperties().Select(static p => p.Name)];

        names.ShouldContain("StateStore");
        names.ShouldContain("PubSub");
        names.ShouldContain("EventStore");
        names.ShouldContain("Tenants");
        names.ShouldContain("Projects");
        names.ShouldContain("ProjectsWorkers");
    }

    /// <summary>Verifies missing Dapr configuration fails fast with a clear exception.</summary>
    [Fact]
    public void ResolveDaprConfigPathShouldThrowWhenRequiredConfigIsMissing()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            FileNotFoundException exception = Should.Throw<FileNotFoundException>(
                () => ProjectsAppHost.ResolveDaprConfigPath(tempDirectory, tempDirectory, "accesscontrol.yaml"));
            exception.Message.ShouldContain("Dapr configuration file 'accesscontrol.yaml' was not found");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
