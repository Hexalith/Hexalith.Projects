// <copyright file="AspireTopologyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Projects.AppHost;
using Hexalith.Projects.Aspire;

using Shouldly;

using Xunit;

using Microsoft.Extensions.Configuration;

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
        ProjectsAspireModule.LocalDaprRedisHost.ShouldBe("localhost:6379");
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

    /// <summary>Verifies the AppHost defaults to the Dapr-initialized Redis endpoint instead of creating a required Redis resource.</summary>
    [Fact]
    public void AppHostShouldUseConfiguredDaprRedisBackingWithoutRequiredRedisResource()
    {
        string appHost = File.ReadAllText(Path.Combine(ProjectRoot(), "src", "Hexalith.Projects.AppHost", "Program.cs"));

        appHost.ShouldContain("builder.Configuration[\"Dapr:RedisHost\"]");
        appHost.ShouldContain("ProjectsAspireModule.LocalDaprRedisHost");
        appHost.ShouldNotContain("builder.AddRedis(");
        appHost.ShouldNotContain("redis.GetEndpoint(\"tcp\")");
    }

    /// <summary>Verifies the AppHost resolves and forwards the platform-supported local Dapr service ports.</summary>
    [Fact]
    public void AppHostShouldResolveLocalDaprPlacementAndSchedulerEndpoints()
    {
        string root = ProjectRoot();
        string appHost = File.ReadAllText(Path.Combine(root, "src", "Hexalith.Projects.AppHost", "Program.cs"));
        string aspireModule = File.ReadAllText(Path.Combine(root, "src", "Hexalith.Projects.Aspire", "ProjectsAspireModule.cs"));

        appHost.ShouldContain("AspireDaprLocalServiceEndpoints.Resolve(");
        appHost.ShouldContain("daprPlacementHostAddress");
        appHost.ShouldContain("daprSchedulerHostAddress");
        aspireModule.ShouldContain("PlacementHostAddress = daprPlacementHostAddress");
        aspireModule.ShouldContain("SchedulerHostAddress = daprSchedulerHostAddress");
    }

    /// <summary>Verifies the AppHost uses the shared EventStore security helper instead of hand-rolled Keycloak wiring.</summary>
    [Fact]
    public void AppHostShouldUseSharedEventStoreSecurityResource()
    {
        string root = ProjectRoot();
        string appHost = File.ReadAllText(Path.Combine(root, "src", "Hexalith.Projects.AppHost", "Program.cs"));
        string appHostProject = File.ReadAllText(Path.Combine(root, "src", "Hexalith.Projects.AppHost", "Hexalith.Projects.AppHost.csproj"));

        appHost.ShouldContain("AddHexalithEventStoreSecurity(");
        appHost.ShouldContain("eventStore.WithJwtBearerSecurity(security)");
        appHost.ShouldContain("tenants.WithJwtBearerSecurity(security)");
        appHost.ShouldContain("_ = projects\n        .WithJwtBearerSecurity(security)");
        appHost.ShouldContain("projectsWorkers.WithSecurityDependency(security)");
        appHost.ShouldContain("projectsUi.WithOpenIdConnectSecurity(");
        appHost.ShouldNotContain("AddKeycloak(\"keycloak\"");
        appHost.ShouldNotContain("ConfigureJwt(");

        appHostProject.ShouldContain("Hexalith.EventStore.Aspire.csproj");
        appHostProject.ShouldNotContain("Aspire.Hosting.Keycloak");
    }

    /// <summary>Verifies the live fixture profile is fail-closed unless explicitly enabled.</summary>
    [Fact]
    public void LiveFixtureProfileShouldAddNoIngressWhenDisabled()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        string fixtureProject = FixtureProjectPath();
        IResourceBuilder<ProjectResource> projects = builder.AddProject("projects", fixtureProject);

        IResourceBuilder<ProjectResource>? control = ProjectsLiveE2EFixtureProfile.AddResources(
            builder,
            projects,
            fixtureProject);

        control.ShouldBeNull();
        builder.Resources.ShouldNotContain(resource =>
            string.Equals(resource.Name, ProjectsLiveE2EFixtureProfile.ControlResourceName, StringComparison.Ordinal)
            || string.Equals(resource.Name, "conversations", StringComparison.Ordinal)
            || string.Equals(resource.Name, "folders", StringComparison.Ordinal)
            || string.Equals(resource.Name, "memories", StringComparison.Ordinal));
    }

    /// <summary>Verifies the explicit live profile adds all role hosts and one external control ingress.</summary>
    [Fact]
    public void LiveFixtureProfileShouldAddRoleHostsAndControlWhenEnabled()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ProjectsLiveE2EFixtureProfile.EnabledConfigurationKey] = "true",
        });
        string fixtureProject = FixtureProjectPath();
        IResourceBuilder<ProjectResource> projects = builder.AddProject("projects", fixtureProject);

        IResourceBuilder<ProjectResource>? control = ProjectsLiveE2EFixtureProfile.AddResources(
            builder,
            projects,
            fixtureProject);

        control.ShouldNotBeNull();
        control.Resource.Name.ShouldBe(ProjectsLiveE2EFixtureProfile.ControlResourceName);
        string[] resourceNames = [.. builder.Resources.Select(static resource => resource.Name)];
        resourceNames.ShouldContain("conversations");
        resourceNames.ShouldContain("folders");
        resourceNames.ShouldContain("memories");
        resourceNames.ShouldContain(ProjectsLiveE2EFixtureProfile.ControlResourceName);
    }

    /// <summary>Verifies AppHost-provided OIDC always disables the diagnostic bypass.</summary>
    [Fact]
    public void AppHostShouldDisableAnonymousBypassWhenOidcIsWired()
    {
        string appHost = File.ReadAllText(Path.Combine(ProjectRoot(), "src", "Hexalith.Projects.AppHost", "Program.cs"));

        appHost.ShouldContain("Authentication__JwtBearer__AllowAnonymousDevelopment");
        appHost.ShouldContain("\"false\"");
        appHost.ShouldNotContain("Authentication__JwtBearer__AllowAnonymousDevelopment\", \"true");
    }

    /// <summary>Verifies live principals can exercise every supported Projects mutation without bypassing authorization.</summary>
    [Fact]
    public void KeycloakLivePrincipalsShouldCarryProjectsMutationPermissions()
    {
        string realmPath = Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Projects.AppHost",
            "KeycloakRealms",
            "hexalith-realm.json");
        using JsonDocument realm = JsonDocument.Parse(File.ReadAllText(realmPath));
        string[] requiredPermissions =
        [
            "projects:link_conversation",
            "projects:move_conversation",
            "projects:unlink_conversation",
            "projects:confirm_resolution",
            "projects:set_folder",
            "projects:link_file_reference",
            "projects:unlink_file_reference",
            "projects:link_memory",
            "projects:unlink_memory",
        ];

        foreach (string username in new[] { "admin-user", "tenant-a-user" })
        {
            JsonElement user = realm.RootElement
                .GetProperty("users")
                .EnumerateArray()
                .Single(candidate => string.Equals(
                    candidate.GetProperty("username").GetString(),
                    username,
                    StringComparison.Ordinal));
            string[] permissions = user
                .GetProperty("attributes")
                .GetProperty("permissions")
                .EnumerateArray()
                .Select(static permission => permission.GetString()!)
                .ToArray();

            foreach (string permission in requiredPermissions)
            {
                permissions.ShouldContain(permission);
            }
        }
    }

    /// <summary>Verifies only the explicit live profile moves Project projection delivery off the command tail.</summary>
    [Fact]
    public void LiveFixtureProfileShouldUseSupportedEventStoreProjectionPolling()
    {
        string appHost = File.ReadAllText(Path.Combine(ProjectRoot(), "src", "Hexalith.Projects.AppHost", "Program.cs"));

        appHost.ShouldContain("ProjectsLiveE2EFixtureProfile.IsEnabled(builder.Configuration)");
        appHost.ShouldContain("EventStore__Projections__Domains__projects__RefreshIntervalMs");
        appHost.ShouldContain("\"250\"");
    }

    /// <summary>Verifies only the explicitly named diagnostic profile enables anonymous startup.</summary>
    [Fact]
    public void ServerLaunchProfilesShouldKeepDefaultOidcCapableAndNameTheAnonymousDiagnosticProfile()
    {
        string launchSettingsPath = Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Projects.Server",
            "Properties",
            "launchSettings.json");
        using JsonDocument launchSettings = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        JsonElement profiles = launchSettings.RootElement.GetProperty("profiles");

        profiles
            .GetProperty("http")
            .GetProperty("environmentVariables")
            .TryGetProperty("Authentication__JwtBearer__AllowAnonymousDevelopment", out _)
            .ShouldBeFalse();
        profiles
            .GetProperty("anonymous-diagnostics")
            .GetProperty("environmentVariables")
            .GetProperty("Authentication__JwtBearer__AllowAnonymousDevelopment")
            .GetString()
            .ShouldBe("true");
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

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string FixtureProjectPath()
        => Path.Combine(
            ProjectRoot(),
            "tests",
            "Hexalith.Projects.E2E.Fixtures",
            "Hexalith.Projects.E2E.Fixtures.csproj");
}
