// <copyright file="ProjectsServerModuleTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server;
using Hexalith.Projects.Workers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

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

    /// <summary>Verifies persisted rejection events are skipped while the sequence still advances.</summary>
    [Fact]
    public void ProjectCallbackSkipsPersistedRejectionEventsAndKeepsTheSequence()
    {
        ProjectionRequest request = ProjectionRequestOf(
            ProjectionEvent(CreatedEvent(), 1),
            ProjectionEvent(new ProjectRestoreRejected(new ProjectId("project-a"), "tenant-a", ReferenceState.Conflict), 2),
            ProjectionEvent(ArchivedEvent(), 3));

        ProjectionResponse response = ProjectProjectionHandler.Project(request);
        ProjectState state = response.State.Deserialize<ProjectState>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        state.IsCreated.ShouldBeTrue();
        state.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
    }

    /// <summary>Verifies an incomplete history, a non-JSON event, or an unknown event type fails the replay.</summary>
    /// <param name="defect">The history defect to inject.</param>
    [Theory]
    [InlineData("sequence-gap")]
    [InlineData("non-json-format")]
    [InlineData("unknown-event-type")]
    public void ProjectCallbackRejectsDefectiveHistory(string defect)
    {
        ProjectionEventDto archived = ProjectionEvent(ArchivedEvent(), 2);
        ProjectionEventDto second = defect switch
        {
            "sequence-gap" => archived with { SequenceNumber = 3 },
            "non-json-format" => archived with { SerializationFormat = "protobuf" },
            "unknown-event-type" => archived with { EventTypeName = "Hexalith.Projects.Contracts.Events.ProjectTeleported" },
            _ => throw new ArgumentOutOfRangeException(nameof(defect)),
        };

        _ = Should.Throw<InvalidOperationException>(
            () => ProjectProjectionHandler.Project(ProjectionRequestOf(ProjectionEvent(CreatedEvent(), 1), second)));
    }

    /// <summary>Verifies the hosted projection callback answers a foreign domain with 404 instead of replaying it.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task HostedProjectCallbackReturnsNotFoundForAForeignDomain()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddProjectsServer();
        WebApplication app = builder.Build();
        app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            ProjectionRequest foreign = ProjectionRequestOf(ProjectionEvent(CreatedEvent(), 1)) with { Domain = "orders" };

            using HttpResponseMessage response = await client
                .PostAsJsonAsync(ProjectsServerModule.ProjectRoute, foreign, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Verifies the workers module marker exposes its name.
    /// </summary>
    [Fact]
    public void WorkersModuleNameIsSet()
    {
        ProjectsWorkersModule.Name.ShouldBe("Hexalith.Projects.Workers");
    }

    private static ProjectionRequest ProjectionRequestOf(params ProjectionEventDto[] events)
        => new("tenant-a", ProjectsServerModule.DomainName, "project-a", events);

    private static ProjectionEventDto ProjectionEvent(object payload, long sequence)
        => new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "json",
            sequence,
            new DateTimeOffset(2026, 8, 27, 1, 0, 0, TimeSpan.Zero),
            $"correlation-{sequence}");

    private static ProjectCreated CreatedEvent() => new(
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

    private static ProjectArchived ArchivedEvent() => new(
        "tenant-a",
        "project-a",
        ProjectLifecycle.Archived,
        "actor-a",
        "correlation-archive",
        "task-archive",
        "idempotency-archive",
        "fingerprint-archive",
        new DateTimeOffset(2026, 8, 27, 2, 0, 0, TimeSpan.Zero));
}
