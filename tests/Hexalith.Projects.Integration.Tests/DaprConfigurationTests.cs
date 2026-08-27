// <copyright file="DaprConfigurationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;

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
        content.ShouldContain("Production must use deny-by-default Dapr access control");
        content.ShouldContain("defaultAction: allow");
        content.ShouldContain($"appId: {ProjectsAspireModule.EventStoreAppId}");
        content.ShouldContain($"appId: {ProjectsAspireModule.TenantsAppId}");
        content.ShouldContain($"appId: {ProjectsAspireModule.ProjectsAppId}");
        content.ShouldContain($"appId: {ProjectsAspireModule.ProjectsWorkersAppId}");
        content.ShouldContain(ProjectsWorkersModule.TenantEventsRoute);
        content.ShouldContain(ProjectsWorkersModule.ProjectEventsRoute);
    }

    /// <summary>Verifies the imported local identity realm cannot be mistaken for production configuration.</summary>
    [Fact]
    public void LocalIdentityFixtureShouldDeclareExternalProductionConfiguration()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Hexalith.Projects.AppHost",
            "KeycloakRealms",
            "hexalith-realm.json");
        string content = File.ReadAllText(Path.GetFullPath(path));

        content.ShouldContain("hexalith.identity.environment");
        content.ShouldContain("Development");
        content.ShouldContain("hexalith.identity.productionConfiguration");
        content.ShouldContain("external");
    }

    /// <summary>Verifies local identity keeps API password-grant and browser code-flow clients separate.</summary>
    [Fact]
    public void LocalIdentityFixtureShouldDeclareProjectsUiConfidentialCodeFlowClient()
    {
        using JsonDocument realm = JsonDocument.Parse(File.ReadAllText(RealmPath()));
        JsonElement[] clients = [.. realm.RootElement.GetProperty("clients").EnumerateArray()];
        JsonElement apiClient = clients.Single(client => client.GetProperty("clientId").GetString() == "hexalith-eventstore");
        JsonElement uiClient = clients.Single(client => client.GetProperty("clientId").GetString() == "hexalith-projects-ui");

        apiClient.GetProperty("directAccessGrantsEnabled").GetBoolean().ShouldBeTrue();
        apiClient.GetProperty("standardFlowEnabled").GetBoolean().ShouldBeFalse();
        uiClient.GetProperty("publicClient").GetBoolean().ShouldBeFalse();
        uiClient.GetProperty("directAccessGrantsEnabled").GetBoolean().ShouldBeFalse();
        uiClient.GetProperty("standardFlowEnabled").GetBoolean().ShouldBeTrue();
        uiClient.GetProperty("secret").GetString().ShouldNotBeNullOrWhiteSpace();
        uiClient.GetProperty("redirectUris").EnumerateArray().Select(static item => item.GetString()).ShouldBe(
        [
            "http://localhost:*",
            "https://localhost:*",
        ]);

        foreach (JsonElement client in new[] { apiClient, uiClient })
        {
            JsonElement mapper = client.GetProperty("protocolMappers").EnumerateArray().Single(item =>
                item.GetProperty("name").GetString() == "current-tenant-mapper");
            JsonElement config = mapper.GetProperty("config");
            config.GetProperty("claim.name").GetString().ShouldBe("eventstore:current-tenant");
            config.GetProperty("multivalued").GetString().ShouldBe("false");
        }
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

    private static string RealmPath()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Hexalith.Projects.AppHost",
            "KeycloakRealms",
            "hexalith-realm.json"));
}
