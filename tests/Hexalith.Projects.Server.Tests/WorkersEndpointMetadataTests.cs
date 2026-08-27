// <copyright file="WorkersEndpointMetadataTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Text;

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

    /// <summary>Verifies the flat EventStore publisher payload retains all projection metadata.</summary>
    [Fact]
    public void ProjectEventWireEnvelopeShouldConvertToContractsEnvelope()
    {
        DateTimeOffset timestamp = DateTimeOffset.Parse("2026-08-27T03:26:11Z", System.Globalization.CultureInfo.InvariantCulture);
        byte[] payload = Encoding.UTF8.GetBytes("{\"projectId\":\"project-1\"}");
        var wireEnvelope = new ProjectEventWireEnvelope(
            "message-1",
            "project-1",
            "Project",
            "tenant-1",
            "projects",
            3,
            42,
            timestamp,
            "correlation-1",
            "causation-1",
            "user-1",
            "1.0.0",
            "Hexalith.Projects.Contracts.Events.ProjectCreated",
            1,
            "json",
            payload,
            new Dictionary<string, string> { ["trace"] = "value" });

        Hexalith.EventStore.Contracts.Events.EventEnvelope envelope = wireEnvelope.ToEventEnvelope();

        envelope.Metadata.MessageId.ShouldBe("message-1");
        envelope.Metadata.AggregateId.ShouldBe("project-1");
        envelope.Metadata.AggregateType.ShouldBe("Project");
        envelope.Metadata.TenantId.ShouldBe("tenant-1");
        envelope.Metadata.Domain.ShouldBe("projects");
        envelope.Metadata.SequenceNumber.ShouldBe(3);
        envelope.Metadata.GlobalPosition.ShouldBe(42);
        envelope.Metadata.Timestamp.ShouldBe(timestamp);
        envelope.Metadata.CorrelationId.ShouldBe("correlation-1");
        envelope.Metadata.CausationId.ShouldBe("causation-1");
        envelope.Metadata.UserId.ShouldBe("user-1");
        envelope.Metadata.DomainServiceVersion.ShouldBe("1.0.0");
        envelope.Metadata.EventTypeName.ShouldBe("Hexalith.Projects.Contracts.Events.ProjectCreated");
        envelope.Metadata.MetadataVersion.ShouldBe(1);
        envelope.Metadata.SerializationFormat.ShouldBe("json");
        envelope.Payload.ShouldBe(payload);
        envelope.Extensions.ShouldContainKeyAndValue("trace", "value");
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
