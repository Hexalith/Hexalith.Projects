// <copyright file="GetProjectContextQueryCompositionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>E6.3-A01/A02/A03 composition tests for supported Get and Explain through <c>/query</c>.</summary>
public sealed class GetProjectContextQueryCompositionTests
{
    private const string TenantId = "tenant-a";
    private const string ProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private static readonly DateTimeOffset ObservedAt = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Query_GetProjectContext_ReturnsCompleteThroughSupportedComposition()
    {
        WebApplication app = await StartAppAsync(seedDetail: true, seedTenantAccess: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.PostAsJsonAsync("/query", GetEnvelope(), JsonOptions, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("success").GetBoolean().ShouldBeTrue();
            ProjectContextReadResponse payload = JsonSerializer.Deserialize<ProjectContextReadResponse>(
                document.RootElement.GetProperty("payloadBytes").GetBytesFromBase64(),
                JsonOptions)!;
            payload.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Query_ExplainContextSelection_ReturnsEvaluationsThroughSupportedComposition()
    {
        WebApplication app = await StartAppAsync(seedDetail: true, seedTenantAccess: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            QueryEnvelope envelope = new(
                TenantId,
                ProjectsServerModule.DomainName,
                ProjectId,
                ProjectsServerModule.ExplainContextSelectionQueryType,
                JsonSerializer.SerializeToUtf8Bytes(new ExplainContextSelectionQuery(ProjectId), JsonOptions),
                "corr-1",
                "actor-1");
            HttpResponseMessage response = await client.PostAsJsonAsync("/query", envelope, JsonOptions, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            QueryResult result = (await response.Content.ReadFromJsonAsync<QueryResult>(JsonOptions, TestContext.Current.CancellationToken).ConfigureAwait(true))!;
            ExplainContextSelectionResponse payload = JsonSerializer.Deserialize<ExplainContextSelectionResponse>(result.PayloadBytes!, JsonOptions)!;
            payload.Evaluations.ShouldNotBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Query_DeniedOrMissing_IsIndistinguishableSafe404()
    {
        WebApplication missing = await StartAppAsync(seedDetail: false, seedTenantAccess: true).ConfigureAwait(true);
        WebApplication denied = await StartAppAsync(seedDetail: true, seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient missingClient = new() { BaseAddress = new Uri(missing.Urls.First()) };
            using HttpClient deniedClient = new() { BaseAddress = new Uri(denied.Urls.First()) };
            HttpResponseMessage missingResponse = await missingClient.PostAsJsonAsync("/query", GetEnvelope(), JsonOptions, TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage deniedResponse = await deniedClient.PostAsJsonAsync("/query", GetEnvelope(), JsonOptions, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string missingBody = await missingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            string deniedBody = await deniedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            missingResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            deniedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            using JsonDocument missingDocument = JsonDocument.Parse(missingBody);
            using JsonDocument deniedDocument = JsonDocument.Parse(deniedBody);
            missingDocument.RootElement.GetProperty("errorMessage").GetString().ShouldBe("safe-denial");
            deniedDocument.RootElement.GetProperty("errorMessage").GetString().ShouldBe("safe-denial");
            missingDocument.RootElement.TryGetProperty("payloadBytes", out JsonElement missingPayload).ShouldBeTrue();
            missingPayload.ValueKind.ShouldBeOneOf(JsonValueKind.Null, JsonValueKind.Undefined);
            deniedDocument.RootElement.TryGetProperty("payloadBytes", out JsonElement deniedPayload).ShouldBeTrue();
            deniedPayload.ValueKind.ShouldBeOneOf(JsonValueKind.Null, JsonValueKind.Undefined);
        }
        finally
        {
            await StopAsync(missing).ConfigureAwait(true);
            await StopAsync(denied).ConfigureAwait(true);
        }
    }

    private static QueryEnvelope GetEnvelope()
        => new(
            TenantId,
            ProjectsServerModule.DomainName,
            ProjectId,
            ProjectsServerModule.GetProjectContextQueryType,
            JsonSerializer.SerializeToUtf8Bytes(new GetProjectContextQuery(ProjectId), JsonOptions),
            "corr-1",
            "actor-1");

    private static async Task<WebApplication> StartAppAsync(bool seedDetail, bool seedTenantAccess)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddProjectsServer();
        WebApplication app = builder.Build();
        if (seedTenantAccess)
        {
            ProjectTenantAccessProjection projection = new()
            {
                TenantId = TenantId,
                Enabled = true,
                Watermark = 1,
                ProjectionWatermark = $"{TenantId}:1",
                LastEventTimestamp = DateTimeOffset.UtcNow,
            };
            projection.Principals["actor-1"] = new ProjectTenantPrincipalEvidence("actor-1", "TenantOwner");
            await app.Services.GetRequiredService<Hexalith.Projects.Projections.TenantAccess.IProjectTenantAccessProjectionStore>()
                .SaveAsync(projection, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        if (seedDetail)
        {
            ProjectDetailItem detail = new(
                TenantId,
                ProjectId,
                "Project",
                null,
                null,
                Setup: null,
                new ProjectFolderReference("folder-1", "Folder", ReferenceState.Included, null, ObservedAt),
                [],
                [],
                ProjectLifecycle.Active,
                ObservedAt,
                ObservedAt,
                4);
            await app.Services.GetRequiredService<IReadModelStore>()
                .SaveAsync(
                    ConversationStartSetupProjectionHandler.StoreName,
                    ConversationStartSetupProjectionHandler.Key(TenantId, ProjectId),
                    detail,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return app;
    }

    private static async Task StopAsync(WebApplication app)
    {
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true);
    }
}
