// <copyright file="CreateProjectEndpointTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.Testing.TenantIsolation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-2 tests for the Story 1.4 Server slice (AC 1, 2, 5, 7): the <c>POST /api/v1/projects</c>
/// endpoint returns <c>202 AcceptedCommand</c> on a valid create, maps a fail-closed denial to
/// <c>404</c> (not 500, not 200), and the minimal <c>GetProject</c> read returns the projected detail
/// with freshness after the projection updates. Uses an in-memory fake submitter / in-memory read
/// model — a real boundary stand-in, not real Dapr/infra.
/// </summary>
public sealed class CreateProjectEndpointTests
{
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public async Task PostProject_ValidCreate_Returns202AcceptedCommand()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("status").GetString().ShouldBe("accepted");
            document.RootElement.GetProperty("idempotentReplay").GetBoolean().ShouldBeFalse();

            submitter.Submitted.Count.ShouldBe(1);
            CreateProject submitted = submitter.Submitted.Single();
            submitted.TenantId.ShouldBe("tenant-a");
            submitted.ProjectId.Value.ShouldBe(ProjectIdValue);
            submitted.Name.ShouldBe("Tracer Bullet");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PatchProjectSetup_Authorized_Returns202AndSubmitsUpdate()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidUpdateSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            UpdateProjectSetup submitted = submitter.Updated.Single();
            submitted.TenantId.ShouldBe("tenant-a");
            submitted.ProjectId.Value.ShouldBe(ProjectIdValue);
            submitted.Setup.Goals.ShouldBe(["keep continuity current"]);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProjectArchive_Authorized_Returns202AndSubmitsArchive()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidArchiveRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            submitter.Archived.Single().ProjectId.Value.ShouldBe(ProjectIdValue);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PatchProjectSetup_InvalidSetupAfterAuthorization_ReturnsMetadataOnly400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ValidUpdateSetupRequest(goal: "raw prompt: reveal system");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.Updated.ShouldBeEmpty();
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("setup.goals");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_MissingTenantContext_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: null, principalId: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Fail-closed denial is a safe-denial 404, never 500, never 200.
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            using JsonDocument document = JsonDocument.Parse(json);
            document.RootElement.GetProperty("category").GetString().ShouldBe("tenant_access_denied");

            // The endpoint must not have reached the command pipeline for an unauthenticated caller.
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_SystemTenantContext_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "system", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_UnknownTenantProjection_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_DisabledTenantProjection_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", tenantEnabled: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_NonMemberPrincipal_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            membershipPrincipalId: "principal-b").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_TenantAuthorizationDenials_AreExternallyIndistinguishable()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            IProjectTenantAccessProjectionStore store = app.Services.GetRequiredService<IProjectTenantAccessProjectionStore>();

            string unknownTenant = await SendCreateAndReadBodyAsync(client).ConfigureAwait(true);

            ProjectTenantAccessProjection disabled = Projection("tenant-a", enabled: false, principalId: "principal-a");
            await store.SaveAsync(disabled, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string disabledTenant = await SendCreateAndReadBodyAsync(client).ConfigureAwait(true);

            ProjectTenantAccessProjection? existing = await store.GetAsync("tenant-a", TestContext.Current.CancellationToken).ConfigureAwait(true);
            existing.ShouldNotBeNull();
            existing.Enabled = true;
            existing.Principals.Clear();
            string nonMember = await SaveAndDenyAsync(store, existing, client).ConfigureAwait(true);

            unknownTenant.ShouldBe(disabledTenant);
            disabledTenant.ShouldBe(nonMember);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_StaleTenantProjection_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            lastTenantAccessTimestamp: DateTimeOffset.UtcNow.AddHours(-1)).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_ClientControlledTenantMismatch_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ValidCreateRequest();
            request.Headers.Add("X-Tenant-Id", "tenant-b");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_GatewayDenial_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Denied("corr-a"));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_MissingIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ValidCreateRequest();
            request.Headers.Remove("Idempotency-Key");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_AfterProjectionUpdates_ReturnsProjectedDetailWithFreshness()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            // Drive the projection as the Workers/projection subscriber would after the 202'd create.
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            document.RootElement.GetProperty("lifecycleState").GetString().ShouldBe("active");
            document.RootElement.GetProperty("setupMetadata").GetString().ShouldBe("setup-reference");
            document.RootElement.GetProperty("contextActivation").GetProperty("enabled").GetBoolean().ShouldBeTrue();
            document.RootElement.GetProperty("contextActivation").TryGetProperty("blockedReasonCode", out JsonElement blockedReason).ShouldBeTrue();
            blockedReason.ValueKind.ShouldBe(JsonValueKind.Null);
            document.RootElement.GetProperty("references").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("freshness").GetProperty("readConsistency").GetString().ShouldBe("eventually_consistent");
            document.RootElement.GetProperty("freshness").GetProperty("trustState").GetString().ShouldBe("trusted");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenant_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            // Project created in tenant A; the authenticated caller is tenant B → safe-denial 404.
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_ArchivedProject_ReturnsMetadataWithContextActivationBlocked()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Archived));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("lifecycleState").GetString().ShouldBe("archived");
            document.RootElement.GetProperty("contextActivation").GetProperty("enabled").GetBoolean().ShouldBeFalse();
            document.RootElement.GetProperty("contextActivation").GetProperty("blockedReasonCode").GetString().ShouldBe("archived");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetAndList_AfterSetupUpdateAndArchive_ReflectProjectionEvents()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            InMemoryProjectListReadModel list = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            detail.Project("tenant-a", CreatedEvent("tenant-a"));
            list.Project("tenant-a", CreatedEvent("tenant-a"));
            detail.Project("tenant-a", SetupUpdatedEvent("tenant-a"));
            list.Project("tenant-a", SetupUpdatedEvent("tenant-a"));
            detail.Project("tenant-a", ArchivedEvent("tenant-a"));
            list.Project("tenant-a", ArchivedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage open = await client.GetAsync($"/api/v1/projects/{ProjectIdValue}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage active = await client.GetAsync("/api/v1/projects?lifecycle=active", TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage archived = await client.GetAsync("/api/v1/projects?lifecycle=archived", TestContext.Current.CancellationToken).ConfigureAwait(true);

            open.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(await open.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("projectSetup").GetProperty("goals")[0].GetString().ShouldBe("keep continuity current");
            document.RootElement.GetProperty("contextActivation").GetProperty("enabled").GetBoolean().ShouldBeFalse();
            active.StatusCode.ShouldBe(HttpStatusCode.OK);
            archived.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonDocument.Parse(await active.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                .RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
            JsonDocument.Parse(await archived.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                .RootElement.GetProperty("items").GetArrayLength().ShouldBe(1);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_Authorized_ReturnsOnlyTenantScopedFilteredRows()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectListReadModel readModel = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Active, ProjectIdValue));
            readModel.Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Archived, "01HZ9K8YQ3W6V2N4R7T5P0X1AC"));
            readModel.Project("tenant-b", CreatedEvent("tenant-b", ProjectLifecycle.Active, "01HZ9K8YQ3W6V2N4R7T5P0X1AD"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage activeResponse = await client
                .GetAsync("/api/v1/projects?lifecycle=active", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            HttpResponseMessage allResponse = await client
                .GetAsync("/api/v1/projects", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            activeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            activeResponse.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");
            using JsonDocument activeDocument = JsonDocument.Parse(
                await activeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            JsonElement activeItems = activeDocument.RootElement.GetProperty("items");
            activeItems.GetArrayLength().ShouldBe(1);
            activeItems[0].GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            activeItems[0].TryGetProperty("tenantId", out _).ShouldBeFalse();

            allResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument allDocument = JsonDocument.Parse(
                await allResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            allDocument.RootElement.GetProperty("items").GetArrayLength().ShouldBe(2);
            allDocument.RootElement.GetProperty("freshness").GetProperty("trustState").GetString().ShouldBe("trusted");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_AuthorizedQueryWithIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_AuthorizedQueryWithIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/projects");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenantIdempotencyKey_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_MissingTenantIdempotencyKey_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: null, principalId: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/projects");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_AuthorizedInvalidLifecycle_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client
                .GetAsync("/api/v1/projects?lifecycle=deleted", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenantInvalidFreshness_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("X-Hexalith-Freshness", "strict");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_AuthorizedInvalidFreshness_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("X-Hexalith-Freshness", "strict");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenantExistingProjectAndMissingProject_AreIndistinguishable()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-b", CreatedEvent("tenant-b"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage foreignProject = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            using HttpRequestMessage missingProject = new(HttpMethod.Get, "/api/v1/projects/01HZ9K8YQ3W6V2N4R7T5P0X1AC");
            foreignProject.Headers.Add("X-Correlation-Id", "corr-same");
            missingProject.Headers.Add("X-Correlation-Id", "corr-same");

            HttpResponseMessage foreignResponse = await client.SendAsync(foreignProject, TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage missingResponse = await client.SendAsync(missingProject, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string foreignBody = await foreignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            string missingBody = await missingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            foreignResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            missingResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            foreignBody.ShouldBe(missingBody);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task TenantIsolationConformance_CoversEndpointReadAndQueryFilter()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));
            InMemoryProjectListReadModel listReadModel = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            listReadModel.Project("tenant-a", CreatedEvent("tenant-a"));

            await ProjectTenantIsolationConformance.AssertNoLeakageAsync(
                [
                    new ProjectTenantIsolationSurface(
                        "query-filter",
                        _ =>
                        {
                            ProjectDetailItem? leaked = ProjectQueryTenantFilter.Filter(
                                "tenant-b",
                                new ProjectDetailItem(
                                    "tenant-a",
                                    ProjectIdValue,
                                    "Foreign",
                                    null,
                                    null,
                                    null,
                                    ProjectLifecycle.Active,
                                    DateTimeOffset.UnixEpoch,
                                    DateTimeOffset.UnixEpoch,
                                    1));

                            return Task.FromResult(leaked is null
                                ? ProjectTenantIsolationResult.NoLeak("ProjectQueryTenantFilter")
                                : ProjectTenantIsolationResult.Leak("ProjectQueryTenantFilter", leaked.TenantId, leaked.ProjectId));
                        }),
                    new ProjectTenantIsolationSurface(
                        "read-endpoint",
                        async cancellationToken =>
                        {
                            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
                            HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(true);
                            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
                            bool leaked = response.StatusCode != HttpStatusCode.NotFound
                                || body.Contains(ProjectIdValue, StringComparison.Ordinal)
                                || body.Contains("tenant-a", StringComparison.Ordinal);

                            return leaked
                                ? ProjectTenantIsolationResult.Leak("GET /api/v1/projects/{projectId}", "tenant-a", ProjectIdValue)
                                : ProjectTenantIsolationResult.NoLeak("GET /api/v1/projects/{projectId}");
                        }),
                    new ProjectTenantIsolationSurface(
                        "list-query-filter",
                        _ =>
                        {
                            IReadOnlyList<ProjectListItem> leaked = ProjectQueryTenantFilter.FilterList(
                                "tenant-b",
                                [
                                    new ProjectListItem(
                                        "tenant-a",
                                        ProjectIdValue,
                                        "Foreign",
                                        ProjectLifecycle.Active,
                                        1,
                                        DateTimeOffset.UnixEpoch,
                                        DateTimeOffset.UnixEpoch),
                                ]);

                            return Task.FromResult(leaked.Count == 0
                                ? ProjectTenantIsolationResult.NoLeak("ProjectQueryTenantFilter.FilterList")
                                : ProjectTenantIsolationResult.Leak("ProjectQueryTenantFilter.FilterList", leaked[0].TenantId, leaked[0].ProjectId));
                        }),
                    new ProjectTenantIsolationSurface(
                        "list-endpoint",
                        async cancellationToken =>
                        {
                            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                            HttpResponseMessage response = await client.GetAsync("/api/v1/projects", cancellationToken).ConfigureAwait(true);
                            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
                            bool leaked = response.StatusCode != HttpStatusCode.OK
                                || body.Contains(ProjectIdValue, StringComparison.Ordinal)
                                || body.Contains("tenant-a", StringComparison.Ordinal);

                            return leaked
                                ? ProjectTenantIsolationResult.Leak("GET /api/v1/projects", "tenant-a", ProjectIdValue)
                                : ProjectTenantIsolationResult.NoLeak("GET /api/v1/projects");
                        }),
                ],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task SafeDenialProblemDetails_IsMetadataOnly()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task OpenAndListProjectResponses_AreMetadataOnly()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));
            app.Services.GetRequiredService<InMemoryProjectListReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage open = await client.GetAsync($"/api/v1/projects/{ProjectIdValue}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage list = await client.GetAsync("/api/v1/projects", TestContext.Current.CancellationToken).ConfigureAwait(true);

            open.StatusCode.ShouldBe(HttpStatusCode.OK);
            list.StatusCode.ShouldBe(HttpStatusCode.OK);
            string openBody = await open.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            string listBody = await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(openBody));
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(listBody));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_ReadModelUnavailable_ReturnsMetadataOnly503()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            detailReadUnavailable: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client
                .GetAsync($"/api/v1/projects/{ProjectIdValue}", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_ReadModelUnavailable_ReturnsMetadataOnly503()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            listReadUnavailable: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client
                .GetAsync("/api/v1/projects", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    private static ProjectCreated CreatedEvent(
        string tenant,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        string projectId = ProjectIdValue) => new(
        tenant,
        projectId,
        "Tracer Bullet",
        "A safe description",
        "setup-reference",
        lifecycle,
        "principal-a",
        "corr-a",
        "task-a",
        "idem-key-a",
        "sha256:deadbeef",
        DateTimeOffset.UnixEpoch);

    private static ProjectSetupUpdated SetupUpdatedEvent(string tenant) => new(
        tenant,
        ProjectIdValue,
        Setup(),
        "principal-a",
        "corr-setup",
        "task-setup",
        "idem-key-setup",
        "sha256:setup",
        DateTimeOffset.UnixEpoch.AddMinutes(1));

    private static ProjectArchived ArchivedEvent(string tenant) => new(
        tenant,
        ProjectIdValue,
        ProjectLifecycle.Archived,
        "principal-a",
        "corr-archive",
        "task-archive",
        "idem-key-archive",
        "sha256:archive",
        DateTimeOffset.UnixEpoch.AddMinutes(2));

    private static HttpRequestMessage ValidCreateRequest()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new
            {
                projectId = ProjectIdValue,
                name = "Tracer Bullet",
                description = "A safe description",
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-a");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidUpdateSetupRequest(string goal = "keep continuity current")
    {
        HttpRequestMessage request = new(HttpMethod.Patch, $"/api/v1/projects/{ProjectIdValue}/setup")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                projectSetup = new
                {
                    goals = new[] { goal },
                    userInstructions = new[] { "use safe metadata" },
                    preferredSourceKinds = new[] { "conversation" },
                    excludedSourceKinds = new[] { "fileReference" },
                    conversationStartDefaults = new
                    {
                        linkedSourcePolicy = "authorizedReferences",
                    },
                },
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-update");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidArchiveRequest()
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{ProjectIdValue}/archive")
        {
            Content = JsonContent.Create(new
            {
                archiveIntent = "archive",
                requestSchemaVersion = "v1",
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-archive");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static ProjectSetup Setup() => new(
        ["keep continuity current"],
        ["use safe metadata"],
        [ProjectContextSourceKind.Conversation],
        [ProjectContextSourceKind.FileReference],
        new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));

    private static async Task<string> SendCreateAndReadBodyAsync(HttpClient client)
    {
        using HttpRequestMessage request = ValidCreateRequest();
        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("tenant_access_denied");
        document.RootElement.GetProperty("code").GetString().ShouldBe("resource_unavailable");
        document.RootElement.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("redacted");
        return body;
    }

    private static async Task<string> SaveAndDenyAsync(
        IProjectTenantAccessProjectionStore store,
        ProjectTenantAccessProjection projection,
        HttpClient client)
    {
        await store.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        return await SendCreateAndReadBodyAsync(client).ConfigureAwait(true);
    }

    private static async Task<WebApplication> StartAppAsync(
        FakeProjectCommandSubmitter submitter,
        string? tenantId,
        string? principalId,
        bool seedTenantAccess = true,
        bool tenantEnabled = true,
        string? membershipPrincipalId = null,
        DateTimeOffset? lastTenantAccessTimestamp = null,
        bool detailReadUnavailable = false,
        bool listReadUnavailable = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddProjectsServer();
        builder.Services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        builder.Services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        builder.Services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        builder.Services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        builder.Services.RemoveAll<IProjectTenantContextAccessor>();
        builder.Services.AddSingleton<IProjectTenantContextAccessor>(new FixedProjectTenantContextAccessor(tenantId, principalId));
        builder.Services.AddSingleton<IProjectCommandSubmitter>(submitter);
        if (detailReadUnavailable)
        {
            builder.Services.RemoveAll<IProjectDetailReadModel>();
            builder.Services.AddSingleton<IProjectDetailReadModel, ThrowingProjectDetailReadModel>();
        }

        if (listReadUnavailable)
        {
            builder.Services.RemoveAll<IProjectListReadModel>();
            builder.Services.AddSingleton<IProjectListReadModel, ThrowingProjectListReadModel>();
        }

        WebApplication app = builder.Build();
        if (seedTenantAccess && !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(principalId))
        {
            await SeedTenantAccessAsync(
                app.Services,
                tenantId,
                membershipPrincipalId ?? principalId,
                tenantEnabled,
                lastTenantAccessTimestamp ?? DateTimeOffset.UtcNow).ConfigureAwait(true);
        }

        app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return app;
    }

    private static async Task SeedTenantAccessAsync(
        IServiceProvider services,
        string tenantId,
        string principalId,
        bool enabled,
        DateTimeOffset lastEventTimestamp)
    {
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = tenantId,
            Enabled = enabled,
            Watermark = 1,
            ProjectionWatermark = $"{tenantId}:1",
            LastEventTimestamp = lastEventTimestamp,
        };
        projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");

        await services
            .GetRequiredService<IProjectTenantAccessProjectionStore>()
            .SaveAsync(projection, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    private static ProjectTenantAccessProjection Projection(string tenantId, bool enabled, string principalId)
    {
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = tenantId,
            Enabled = enabled,
            Watermark = 1,
            ProjectionWatermark = $"{tenantId}:1",
            LastEventTimestamp = DateTimeOffset.UtcNow,
        };
        projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");
        return projection;
    }

    private static async Task StopAsync(WebApplication app)
    {
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true);
    }

    private sealed class FakeProjectCommandSubmitter(ProjectCommandSubmissionResult result) : IProjectCommandSubmitter
    {
        public List<CreateProject> Submitted { get; } = [];

        public List<UpdateProjectSetup> Updated { get; } = [];

        public List<ArchiveProject> Archived { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
        {
            Submitted.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
        {
            Updated.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
        {
            Archived.Add(command);
            return Task.FromResult(result);
        }
    }

    private sealed class FixedProjectTenantContextAccessor(string? tenantId, string? principalId) : IProjectTenantContextAccessor
    {
        public string? AuthoritativeTenantId { get; } = tenantId;

        public string? PrincipalId { get; } = principalId;

        public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
            => string.IsNullOrWhiteSpace(AuthoritativeTenantId) || string.IsNullOrWhiteSpace(PrincipalId)
                ? EventStoreClaimTransformEvidence.Missing()
                : EventStoreClaimTransformEvidence.Allowed(
                    AuthoritativeTenantId,
                    PrincipalId,
                    [
                        ProjectAuthorizationGate.CreateProjectAction,
                        ProjectAuthorizationGate.ReadProjectAction,
                        ProjectAuthorizationGate.ListProjectsAction,
                        ProjectAuthorizationGate.UpdateProjectSetupAction,
                        ProjectAuthorizationGate.ArchiveProjectAction,
                    ]);
    }

    private sealed class ThrowingProjectDetailReadModel : IProjectDetailReadModel
    {
        public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Synthetic projection unavailable.");
    }

    private sealed class ThrowingProjectListReadModel : IProjectListReadModel
    {
        public Task<IReadOnlyList<ProjectListItem>> ListAsync(
            string authoritativeTenantId,
            ProjectLifecycle? lifecycleFilter,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Synthetic projection unavailable.");
    }
}
