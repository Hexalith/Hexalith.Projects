// <copyright file="WorkersSubscriptionHostingTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Workers;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>
/// Hosts the real Workers subscription endpoints over a fake state store and delivers the payloads the
/// EventStore publisher and Dapr actually send, so the flat Project-event binding and the Dapr
/// acknowledgement contract are exercised end to end.
/// </summary>
public sealed class WorkersSubscriptionHostingTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 3, 26, 11, TimeSpan.Zero);

    /// <summary>Verifies a CloudEvent-wrapped flat publisher payload binds, persists, and acknowledges SUCCESS.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProjectEventsRouteShouldBindTheFlatPublisherPayloadAndAcknowledgeSuccess()
    {
        InMemoryProjectsStateStore stateStore = new();
        WebApplication app = await StartWorkersAsync(stateStore).ConfigureAwait(true);
        try
        {
            ProjectCreated created = new(
                "tenant-a",
                "project-a",
                "Project A",
                null,
                null,
                ProjectLifecycle.Active,
                "user-a",
                "correlation-a",
                "task-a",
                "idempotency-a",
                "fingerprint-a",
                Now);

            // The EventStore publisher sends one flat object; System.Text.Json encodes the byte[] payload as base64.
            string flatPayload = JsonSerializer.Serialize(new
            {
                messageId = "message-a",
                aggregateId = "project-a",
                aggregateType = "Project",
                tenantId = "tenant-a",
                domain = "projects",
                sequenceNumber = 1,
                globalPosition = 42,
                timestamp = Now,
                correlationId = "correlation-a",
                causationId = "idempotency-a",
                userId = "user-a",
                domainServiceVersion = "1.0.0",
                eventTypeName = typeof(ProjectCreated).FullName,
                metadataVersion = 1,
                serializationFormat = "json",
                payload = JsonSerializer.SerializeToUtf8Bytes(created, DaprProjectProjectionStore.JsonOptions),
                extensions = (object?)null,
            });

            using JsonDocument acknowledgement = await PostCloudEventAsync(app, ProjectsWorkersModule.ProjectEventsRoute, flatPayload).ConfigureAwait(true);

            acknowledgement.RootElement.GetProperty("status").GetString().ShouldBe("SUCCESS");
            acknowledgement.RootElement.GetProperty("processingResult").GetString().ShouldBe("Applied");
            acknowledgement.RootElement.GetProperty("tenantId").GetString().ShouldBe("tenant-a");
            acknowledgement.RootElement.GetProperty("messageId").GetString().ShouldBe("message-a");
            acknowledgement.RootElement.GetProperty("sequence").GetInt64().ShouldBe(42);
            stateStore.Keys.ShouldNotBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    /// <summary>Verifies a CloudEvent-wrapped Tenants domain-event envelope is processed and acknowledged SUCCESS.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TenantEventsRouteShouldProcessTheDomainEventEnvelopeAndAcknowledgeSuccess()
    {
        InMemoryProjectsStateStore stateStore = new();
        WebApplication app = await StartWorkersAsync(stateStore).ConfigureAwait(true);
        try
        {
            // The Tenants processor requires a ULID message identity, exactly as EventStore publishes it.
            string envelope = JsonSerializer.Serialize(new
            {
                messageId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
                aggregateId = "tenant-a",
                tenantId = "system",
                domain = "tenants",
                eventTypeName = typeof(TenantCreated).FullName,
                sequenceNumber = 1,
                globalPosition = 7,
                timestamp = Now,
                correlationId = "correlation-tenant-a",
                serializationFormat = "json",
                payload = JsonSerializer.SerializeToUtf8Bytes(
                    new TenantCreated("tenant-a", "Tenant A", null, Now),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            });

            using JsonDocument acknowledgement = await PostCloudEventAsync(app, ProjectsWorkersModule.TenantEventsRoute, envelope).ConfigureAwait(true);

            acknowledgement.RootElement.GetProperty("status").GetString().ShouldBe("SUCCESS");
            acknowledgement.RootElement.GetProperty("processingResult").GetString().ShouldBe("Processed");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    private static async Task<WebApplication> StartWorkersAsync(InMemoryProjectsStateStore stateStore)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration["urls"] = "http://127.0.0.1:0";

        // Registered first: the Dapr infrastructure registrations are TryAdd, so the fake replaces the sidecar.
        builder.Services.AddSingleton<IProjectsStateStore>(stateStore);
        builder.Services.AddProjectsTenantEventWorkers();

        WebApplication app = builder.Build();
        app.UseCloudEvents();
        app.MapSubscribeHandler();
        app.MapProjectsTenantEventWorkerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return app;
    }

    private static async Task<JsonDocument> PostCloudEventAsync(WebApplication app, string route, string data)
    {
        string cloudEvent = $$"""
            {"specversion":"1.0","id":"cloud-event-1","source":"eventstore","type":"com.dapr.event.sent","datacontenttype":"application/json","data":{{data}}}
            """;
        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
        using StringContent content = new(cloudEvent, Encoding.UTF8, "application/cloudevents+json");
        using HttpResponseMessage response = await client
            .PostAsync(new Uri(route, UriKind.Relative), content, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        return JsonDocument.Parse(body);
    }

    private static async Task StopAsync(WebApplication app)
    {
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true);
    }
}
