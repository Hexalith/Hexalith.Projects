// <copyright file="ProjectsServerModuleTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.Indexes;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Tests.Queries;
using Hexalith.Projects.Workers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

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

    /// <summary>Verifies the SDK host maps the EventStore aggregate callback at the canonical route.</summary>
    [Fact]
    public void ServerEndpointsMapCanonicalProcessCallback()
        => SdkRoute(ProjectsServerModule.ProcessRoute).Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldBe(["POST"]);

    /// <summary>Verifies the SDK host maps the EventStore full-replay projection callback at the canonical route.</summary>
    [Fact]
    public void ServerEndpointsMapCanonicalProjectCallback()
        => SdkRoute(ProjectsServerModule.ProjectRoute).Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldBe(["POST"]);

    /// <summary>Verifies the SDK metadata callback advertises exactly the Projects query handlers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task OperationalIndexMetadataAdvertisesTheBoundProjectQueryHandlers()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "projects-sdk-metadata" };
        WebApplication app = await SupportedProjectReadHttpTests.StartAsync(null, null, key, workload: true).ConfigureAwait(true);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = await CallbackClientAsync(app, key, EventStoreWorkloadOperations.DomainServiceMetadata).ConfigureAwait(true);
            using HttpResponseMessage response = await client.PostAsJsonAsync("/admin/operational-index-metadata",
                MetadataRequest(ProjectsServerModule.DomainName, ProjectsServerModule.DomainName, "v1"), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            AdminOperationalIndexMetadata.DomainMetadata domain = JsonSerializer
                .Deserialize<AdminOperationalIndexMetadata.Response>(body, JsonSerializerOptions.Web)!.Domains.ShouldHaveSingleItem();
            domain.Domain.ShouldBe(ProjectsServerModule.DomainName);
            domain.QueryTypes.Order(StringComparer.Ordinal).ShouldBe(ProjectQueryTypes(app));
            JsonSerializer.Deserialize<AdminOperationalIndexMetadataResponse>(body, JsonSerializerOptions.Web)!
                .Domains.ShouldHaveSingleItem().QueryTypes.ShouldNotBeNull()
                .ShouldContain(ResolveProjectChatBotAuthority.QueryType);
        }
    }

    /// <summary>Pins the SDK metadata status for foreign request identities and refuses foreign advertisement.</summary>
    /// <param name="domain">The single requested domain.</param>
    /// <param name="appId">The requested application identity.</param>
    /// <param name="version">The requested service version.</param>
    /// <param name="status">The pinned SDK status.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("other", "projects", "v1", HttpStatusCode.ServiceUnavailable)]
    [InlineData("projects", "other", "v1", HttpStatusCode.BadRequest)]
    [InlineData("projects", "projects", "v2", HttpStatusCode.BadRequest)]
    public async Task OperationalIndexMetadataRejectsForeignIdentity(string domain, string? appId, string version, HttpStatusCode status)
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "projects-sdk-metadata-identity" };
        WebApplication app = await SupportedProjectReadHttpTests.StartAsync(null, null, key, workload: true).ConfigureAwait(true);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = await CallbackClientAsync(app, key, EventStoreWorkloadOperations.DomainServiceMetadata).ConfigureAwait(true);
            using HttpResponseMessage response = await client.PostAsJsonAsync("/admin/operational-index-metadata",
                MetadataRequest(domain, appId, version), TestContext.Current.CancellationToken).ConfigureAwait(true);
            response.StatusCode.ShouldBe(status);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            if (domain != ProjectsServerModule.DomainName)
            {
                foreach (string queryType in ProjectQueryTypes(app)) body.ShouldNotContain(queryType);
                body.ShouldNotContain("\"" + ProjectsServerModule.DomainName + "\"");
            }
        }
    }

    /// <summary>Verifies a blank AppId receives the SDK's legacy Projects metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task OperationalIndexMetadataReturnsLegacyProjectsMetadataForBlankAppId()
    {
        // Only EventStore calls this endpoint, with its registered identity (review decision EC-11).
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "projects-sdk-metadata-blank-identity" };
        WebApplication app = await SupportedProjectReadHttpTests.StartAsync(null, null, key, workload: true).ConfigureAwait(true);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = await CallbackClientAsync(app, key, EventStoreWorkloadOperations.DomainServiceMetadata).ConfigureAwait(true);
            using HttpResponseMessage response = await client.PostAsJsonAsync("/admin/operational-index-metadata",
                MetadataRequest(ProjectsServerModule.DomainName, null, "v1"), TestContext.Current.CancellationToken).ConfigureAwait(true);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken).ConfigureAwait(true))!
                .Domains.ShouldHaveSingleItem().Domain.ShouldBe(ProjectsServerModule.DomainName);
        }
    }

    /// <summary>Verifies runtime metadata route requires the EventStore metadata operation policy.</summary>
    [Fact]
    public void OperationalIndexMetadataRouteRequiresEventStoreMetadataPolicy()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddProjectsDomainService();
        WebApplication app = builder.Build();

        app.UseEventStoreDomainService();

        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(item => string.Equals(item.RoutePattern.RawText,
                "/admin/operational-index-metadata", StringComparison.Ordinal));
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldContain("POST");
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .ShouldContain(policy => policy.Policy == EventStoreDomainServicePolicies.Metadata);
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

    /// <summary>Verifies the SDK projection callback answers a foreign domain with 404 instead of replaying it.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task HostedProjectCallbackReturnsNotFoundForAForeignDomain()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "projects-sdk-foreign-projection" };
        WebApplication app = await SupportedProjectReadHttpTests.StartAsync(null, null, key, workload: true).ConfigureAwait(true);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = await CallbackClientAsync(app, key, EventStoreWorkloadOperations.DomainServiceProject).ConfigureAwait(true);
            ProjectionRequest foreign = ProjectionRequestOf(ProjectionEvent(CreatedEvent(), 1)) with { Domain = "orders" };

            using HttpResponseMessage response = await client
                .PostAsJsonAsync(ProjectsServerModule.ProjectRoute, foreign, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldNotContain("Project A");
        }
    }

    /// <summary>Verifies the SDK query receiver maps an invalid supported-read cursor to 400 through the Projects policy.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SdkQueryReceiverMapsAnInvalidCursorToBadRequest()
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        ProjectAuthoritySnapshot[] before = await fixture.SnapshotAsync().ConfigureAwait(true);

        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/v1/queries",
            fixture.List(new(Cursor: "not-a-protected-cursor")), TestContext.Current.CancellationToken).ConfigureAwait(true);

        fixture.Routes.Hops.Last(hop => hop.Target == "projects").Status.ShouldBe(400);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await fixture.AssertUnchangedAsync(before).ConfigureAwait(true);
    }

    /// <summary>
    /// Verifies the workers module marker exposes its name.
    /// </summary>
    [Fact]
    public void WorkersModuleNameIsSet()
    {
        ProjectsWorkersModule.Name.ShouldBe("Hexalith.Projects.Workers");
    }

    private static RouteEndpoint SdkRoute(string route)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddProjectsDomainService();
        WebApplication app = builder.Build();
        app.UseEventStoreDomainService();
        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(item => string.Equals(item.RoutePattern.RawText, route, StringComparison.Ordinal));
    }

    private static async Task<HttpClient> CallbackClientAsync(WebApplication app, SecurityKey key, string operation)
    {
        HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
        client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, "story61-isolated-channel-fixture");
        client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, SupportedProjectReadHttpTests.WorkloadToken(key, operation));
        RouteCatalogGenerationReadback catalog = await app.Services.GetRequiredService<IRouteCatalogAuthority>()
            .ReadbackAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        client.DefaultRequestHeaders.Add(RouteCatalogInvocationHandler.RootHeader, catalog.RootDigest);
        client.DefaultRequestHeaders.Add(RouteCatalogInvocationHandler.EpochHeader, catalog.Epoch.ToString(CultureInfo.InvariantCulture));
        return client;
    }

    private static AdminOperationalIndexMetadata.Request MetadataRequest(string domain, string? appId, string version)
        => new([domain]) { AppId = appId, ServiceVersion = version };

    private static string[] ProjectQueryTypes(WebApplication app) => [.. app.Services.GetServices<IDomainQueryHandler>()
        .Where(static handler => handler.Domain == ProjectsServerModule.DomainName).Select(static handler => handler.QueryType)
        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

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
