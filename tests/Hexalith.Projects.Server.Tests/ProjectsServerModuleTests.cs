// <copyright file="ProjectsServerModuleTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server;
using Hexalith.Projects.Workers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using Shouldly;

using Xunit;

/// <summary>
/// Trivial green Tier-2 tests proving the server and workers skeletons load.
/// </summary>
public sealed class ProjectsServerModuleTests
{
    /// <summary>
    /// Verifies the server module marker exposes its name.
    /// </summary>
    [Fact]
    public void ServerModuleNameIsSet()
    {
        ProjectsServerModule.Name.ShouldBe("Hexalith.Projects.Server");
    }

    /// <summary>Verifies the EventStore aggregate callback is reachable at the registered route.</summary>
    [Fact]
    public void ServerEndpointsMapCanonicalProcessCallback()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddProjectsServer();
        WebApplication app = builder.Build();

        app.MapProjectsServerEndpoints();

        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(item => string.Equals(item.RoutePattern.RawText, ProjectsServerModule.ProcessRoute, StringComparison.Ordinal));
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldContain("POST");
    }

    /// <summary>Verifies the EventStore full-replay projection callback is reachable at the canonical route.</summary>
    [Fact]
    public void ServerEndpointsMapCanonicalProjectCallback()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddProjectsServer();
        WebApplication app = builder.Build();

        app.MapProjectsServerEndpoints();

        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(item => string.Equals(item.RoutePattern.RawText, ProjectsServerModule.ProjectRoute, StringComparison.Ordinal));
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldContain("POST");
    }

    /// <summary>Verifies the projection callback rebuilds meaningful aggregate state from EventStore history.</summary>
    [Fact]
    public void ProjectCallbackRebuildsCanonicalAggregateState()
    {
        var created = new ProjectCreated(
            "tenant-a",
            "project-a",
            "Project A",
            "Description",
            null,
            ProjectLifecycle.Active,
            "actor-a",
            "correlation-a",
            "task-a",
            "idempotency-a",
            "fingerprint-a",
            new DateTimeOffset(2026, 8, 27, 1, 0, 0, TimeSpan.Zero));
        var request = new ProjectionRequest(
            "tenant-a",
            ProjectsServerModule.DomainName,
            "project-a",
            [new ProjectionEventDto(
                typeof(ProjectCreated).FullName!,
                JsonSerializer.SerializeToUtf8Bytes(created, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                "json",
                1,
                created.OccurredAt,
                created.CorrelationId)]);

        ProjectionResponse response = ProjectProjectionHandler.Project(request);
        ProjectState state = response.State.Deserialize<ProjectState>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        response.ProjectionType.ShouldBe(ProjectsServerModule.ProjectionType);
        state.IsCreated.ShouldBeTrue();
        state.TenantId.ShouldBe("tenant-a");
        state.ProjectId.ShouldBe("project-a");
        state.Name.ShouldBe("Project A");
        state.Lifecycle.ShouldBe(ProjectLifecycle.Active);
    }

    /// <summary>
    /// Verifies the workers module marker exposes its name.
    /// </summary>
    [Fact]
    public void WorkersModuleNameIsSet()
    {
        ProjectsWorkersModule.Name.ShouldBe("Hexalith.Projects.Workers");
    }
}
