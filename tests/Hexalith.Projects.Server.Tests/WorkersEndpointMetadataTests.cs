// <copyright file="WorkersEndpointMetadataTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using Hexalith.Projects.Workers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

/// <summary>
/// Tests Workers endpoint mapping and Dapr subscription metadata.
/// </summary>
public sealed class WorkersEndpointMetadataTests
{
    /// <summary>Verifies worker constants include routes, topics, and dead-letter topics.</summary>
    [Fact]
    public void WorkersModuleShouldExposeStableSubscriptionAndDeadLetterMetadata()
    {
        ProjectsWorkersModule.TenantEventsRoute.ShouldBe("/tenants/events");
        ProjectsWorkersModule.TenantEventsTopicName.ShouldBe("system.tenants.events");
        ProjectsWorkersModule.TenantEventsDeadLetterTopicName.ShouldBe("deadletter.system.tenants.events");
        ProjectsWorkersModule.ProjectEventsRoute.ShouldBe("/projects/events");
        ProjectsWorkersModule.ProjectEventsTopicName.ShouldBe("projects.events");
        ProjectsWorkersModule.ProjectEventsDeadLetterTopicName.ShouldBe("deadletter.projects.events");
    }

    /// <summary>Verifies mapped Dapr topic metadata carries dead-letter topics.</summary>
    [Fact]
    public void MapProjectsTenantEventWorkerEndpointsShouldMapDaprTopicsWithDeadLetters()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        builder.Services.AddProjectsTenantEventWorkers();

        WebApplication app = builder.Build();
        app.UseCloudEvents();
        app.MapSubscribeHandler();
        app.MapProjectsTenantEventWorkerEndpoints();

        RouteEndpoint tenantEndpoint = FindEndpoint(app, ProjectsWorkersModule.TenantEventsRoute);
        RouteEndpoint projectEndpoint = FindEndpoint(app, ProjectsWorkersModule.ProjectEventsRoute);

        GetTopicMetadataProperty(tenantEndpoint, "PubsubName").ShouldBe(ProjectsWorkersModule.TenantEventsPubSubName);
        GetTopicMetadataProperty(tenantEndpoint, "Name").ShouldBe(ProjectsWorkersModule.TenantEventsTopicName);
        GetTopicMetadataProperty(tenantEndpoint, "DeadLetterTopic").ShouldBe(ProjectsWorkersModule.TenantEventsDeadLetterTopicName);
        GetTopicMetadataProperty(projectEndpoint, "PubsubName").ShouldBe(ProjectsWorkersModule.ProjectEventsPubSubName);
        GetTopicMetadataProperty(projectEndpoint, "Name").ShouldBe(ProjectsWorkersModule.ProjectEventsTopicName);
        GetTopicMetadataProperty(projectEndpoint, "DeadLetterTopic").ShouldBe(ProjectsWorkersModule.ProjectEventsDeadLetterTopicName);
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string route)
        => ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(static dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => string.Equals(endpoint.RoutePattern.RawText, route, StringComparison.Ordinal));

    private static string? GetTopicMetadataProperty(RouteEndpoint endpoint, string propertyName)
    {
        object topicMetadata = endpoint.Metadata.Single(
            metadata => metadata.GetType().GetProperty("DeadLetterTopic") is not null);
        return topicMetadata.GetType().GetProperty(propertyName)?.GetValue(topicMetadata)?.ToString();
    }
}
