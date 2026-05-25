// <copyright file="DaprConfigurationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using Hexalith.Projects.Aspire;
using Hexalith.Projects.Workers;

using Shouldly;

using Xunit;

/// <summary>
/// File-level Dapr configuration checks for local topology safety.
/// </summary>
public sealed class DaprConfigurationTests
{
    private static readonly string DaprComponentsDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Hexalith.Projects.AppHost", "DaprComponents"));

    /// <summary>Verifies access control config exists and declares explicit local-only policies.</summary>
    [Fact]
    public void AccessControlConfigurationShouldDeclareExplicitProjectsPolicies()
    {
        string content = ReadRequired("accesscontrol.yaml");

        content.ShouldContain("Local development only");
        content.ShouldContain("defaultAction: deny");
        content.ShouldContain($"appId: {ProjectsAspireModule.EventStoreAppId}");
        content.ShouldContain($"appId: {ProjectsAspireModule.TenantsAppId}");
        content.ShouldContain($"appId: {ProjectsAspireModule.ProjectsAppId}");
        content.ShouldContain($"appId: {ProjectsAspireModule.ProjectsWorkersAppId}");
        content.ShouldContain(ProjectsWorkersModule.TenantEventsRoute);
        content.ShouldContain(ProjectsWorkersModule.ProjectEventsRoute);
    }

    /// <summary>Verifies Dapr resiliency config exists and targets app/component boundaries.</summary>
    [Fact]
    public void ResiliencyConfigurationShouldTargetAppsComponentsAndPubSubDeadLetters()
    {
        string content = ReadRequired("resiliency.yaml");

        content.ShouldContain("kind: Resiliency");
        content.ShouldContain("projectsServiceInvocationRetry");
        content.ShouldContain("projectsComponentRetry");
        content.ShouldContain($"  - {ProjectsAspireModule.EventStoreAppId}");
        content.ShouldContain($"  - {ProjectsAspireModule.TenantsAppId}");
        content.ShouldContain($"  - {ProjectsAspireModule.ProjectsAppId}");
        content.ShouldContain($"  - {ProjectsAspireModule.ProjectsWorkersAppId}");
        content.ShouldContain($"      {ProjectsAspireModule.StateStoreComponentName}:");
        content.ShouldContain($"      {ProjectsAspireModule.PubSubComponentName}:");
        content.ShouldContain(ProjectsWorkersModule.TenantEventsDeadLetterTopicName);
        content.ShouldContain(ProjectsWorkersModule.ProjectEventsDeadLetterTopicName);
    }

    private static string ReadRequired(string fileName)
    {
        string path = Path.Combine(DaprComponentsDirectory, fileName);
        File.Exists(path).ShouldBeTrue($"Expected Dapr config file at {path}.");
        return File.ReadAllText(path);
    }
}
