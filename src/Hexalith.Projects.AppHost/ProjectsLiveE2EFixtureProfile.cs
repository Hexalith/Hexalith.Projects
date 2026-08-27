// <copyright file="ProjectsLiveE2EFixtureProfile.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.AppHost;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.Configuration;

/// <summary>Composes metadata-only sibling compatibility fixtures for an explicit live E2E run.</summary>
public static class ProjectsLiveE2EFixtureProfile
{
    /// <summary>Gets the configuration key that must be explicitly enabled for fixture composition.</summary>
    public const string EnabledConfigurationKey = "Projects:E2E:LiveFixtures";

    /// <summary>Determines whether the explicit live E2E fixture profile is enabled.</summary>
    /// <param name="configuration">The AppHost configuration.</param>
    /// <returns><see langword="true"/> only for the explicit values <c>true</c> or <c>1</c>.</returns>
    public static bool IsEnabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? configured = configuration[EnabledConfigurationKey]?.Trim();
        return string.Equals(configured, bool.TrueString, StringComparison.OrdinalIgnoreCase)
            || string.Equals(configured, "1", StringComparison.Ordinal);
    }

    /// <summary>Adds role-specific sibling fixtures and their runner control surface when enabled.</summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="projects">The Projects server resource that consumes sibling public contracts.</param>
    /// <param name="fixtureProjectPath">Optional explicit fixture project path for structural hosts and tests.</param>
    /// <returns>The runner control resource, or <see langword="null"/> when the profile is disabled.</returns>
    public static IResourceBuilder<ProjectResource>? AddResources(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> projects,
        string? fixtureProjectPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(projects);

        if (!IsEnabled(builder.Configuration))
        {
            return null;
        }

        string resolvedFixtureProjectPath = fixtureProjectPath is null
            ? ResolveFixtureProjectPath(builder.AppHostDirectory)
            : Path.GetFullPath(fixtureProjectPath);
        if (!File.Exists(resolvedFixtureProjectPath))
        {
            throw new FileNotFoundException("The live E2E fixture project was not found.", resolvedFixtureProjectPath);
        }

        IResourceBuilder<ProjectResource> conversations = AddRole(builder, resolvedFixtureProjectPath, "conversations");
        IResourceBuilder<ProjectResource> folders = AddRole(builder, resolvedFixtureProjectPath, "folders");
        IResourceBuilder<ProjectResource> memories = AddRole(builder, resolvedFixtureProjectPath, "memories");

        _ = projects
            .WithReference(conversations)
            .WithReference(folders)
            .WithReference(memories)
            .WaitFor(conversations)
            .WaitFor(folders)
            .WaitFor(memories);

        return builder
            .AddProject("live-fixtures", resolvedFixtureProjectPath)
            .WithEnvironment("FixtureRole", "control")
            .WithEnvironment("FixtureEndpoints__Conversations", conversations.GetEndpoint("http"))
            .WithEnvironment("FixtureEndpoints__Folders", folders.GetEndpoint("http"))
            .WithEnvironment("FixtureEndpoints__Memories", memories.GetEndpoint("http"))
            .WithHttpEndpoint()
            .WithHttpHealthCheck("/health")
            .WithExternalHttpEndpoints()
            .WithReference(conversations)
            .WithReference(folders)
            .WithReference(memories)
            .WaitFor(conversations)
            .WaitFor(folders)
            .WaitFor(memories);
    }

    private static IResourceBuilder<ProjectResource> AddRole(
        IDistributedApplicationBuilder builder,
        string fixtureProjectPath,
        string role)
        => builder
            .AddProject(role, fixtureProjectPath)
            .WithEnvironment("FixtureRole", role)
            .WithHttpEndpoint()
            .WithHttpHealthCheck("/health");

    private static string ResolveFixtureProjectPath(string appHostDirectory)
    {
        string appHostCandidate = Path.GetFullPath(Path.Combine(
            appHostDirectory,
            "..",
            "..",
            "tests",
            "Hexalith.Projects.E2E.Fixtures",
            "Hexalith.Projects.E2E.Fixtures.csproj"));
        return File.Exists(appHostCandidate)
            ? appHostCandidate
            : throw new FileNotFoundException("The live E2E fixture project was not found.", appHostCandidate);
    }
}
