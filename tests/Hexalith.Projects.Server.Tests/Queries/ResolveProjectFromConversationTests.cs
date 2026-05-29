// <copyright file="ResolveProjectFromConversationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.Testing.TenantIsolation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>
/// Story 4.2 Tier-2 endpoint tests for <c>GET /api/v1/projects/resolution/from-conversation</c>.
/// Exercises the host composition end-to-end: tenant-level authorize, single-conversation Pattern-A
/// ACL read, tenant-scoped project enumeration, pure evidence mapping, and the Story 4.1 engine call.
/// Covers happy paths, Idempotency-Key rejection, freshness/includeArchived validation, malformed and
/// missing conversation id safe-denial 404, read-model-unavailable 503, header echo, cross-tenant
/// isolation, and no-payload-leakage across outcomes.
/// </summary>
public sealed class ResolveProjectFromConversationTests
{
    private const string TenantA = "tenant-a";
    private const string PrincipalA = "principal-a";
    private const string ProjectAId = "project-a";
    private const string ProjectBId = "project-b";
    private const string ConversationIdValue = "conversation-001";
    private const string CorrelationIdValue = "corr-001";

    [Fact]
    public async Task Resolve_HappyPath_SingleCandidate_Returns200()
    {
        WebApplication app = await StartAppAsync(
            metadata: Linked(ProjectAId),
            rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");

            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("result").GetString().ShouldBe("SingleCandidate");
            JsonElement candidates = document.RootElement.GetProperty("candidates");
            candidates.GetArrayLength().ShouldBe(1);
            candidates[0].GetProperty("projectId").GetString().ShouldBe(ProjectAId);
            candidates[0].GetProperty("reasonCodes")[0].GetString().ShouldBe("ConversationLinked");
            document.RootElement.TryGetProperty("tenantId", out _).ShouldBeFalse("FS-8/SM-3: tenant authority must never appear on the wire.");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_NoQualifyingEvidence_Returns200NoMatch()
    {
        WebApplication app = await StartAppAsync(
            metadata: new ConversationResolutionMetadata(ConversationIdValue, LinkedProjectId: null, SafeLabel: "Unrelated", ReferenceState.Included),
            rows: [Row(ProjectAId, "Project Alpha"), Row(ProjectBId, "Project Beta")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("result").GetString().ShouldBe("NoMatch");
            document.RootElement.GetProperty("candidates").GetArrayLength().ShouldBe(0);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_TwoQualifyingProjects_Returns200MultipleCandidates()
    {
        WebApplication app = await StartAppAsync(
            metadata: new ConversationResolutionMetadata(ConversationIdValue, LinkedProjectId: ProjectAId, SafeLabel: "Project Beta", ReferenceState.Included),
            rows: [Row(ProjectAId, "Project Alpha"), Row(ProjectBId, "Project Beta")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("result").GetString().ShouldBe("MultipleCandidates");
            document.RootElement.GetProperty("candidates").GetArrayLength().ShouldBe(2);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = Request();
            request.Headers.Add("Idempotency-Key", "idem-should-be-rejected");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404()
    {
        // Authorization runs before Idempotency-Key validation: an unauthorized caller must never
        // receive validation feedback that probes the rejection path.
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")], seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = Request();
            request.Headers.Add("Idempotency-Key", "idem-leaked-through-rejection?");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_StricterFreshnessRequested_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = Request();
            request.Headers.Add("X-Hexalith-Freshness", "strong");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("freshness");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_InvalidIncludeArchived_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                Request($"/api/v1/projects/resolution/from-conversation?conversationId={ConversationIdValue}&includeArchived=maybe"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("includeArchived");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_MissingConversationId_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                Request("/api/v1/projects/resolution/from-conversation"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("..")]
    [InlineData("bad/slash")]
    [InlineData("bad char")]
    public async Task Resolve_MalformedConversationId_ReturnsSafeDenial404(string malformed)
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                Request($"/api/v1/projects/resolution/from-conversation?conversationId={Uri.EscapeDataString(malformed)}"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_ListReadModelUnavailable_Returns503()
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [], listThrows: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("category").GetString().ShouldBe("read_model_unavailable");
            document.RootElement.GetProperty("retryable").GetBoolean().ShouldBeTrue();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_AuthoritativeTenantIdMissing_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")], tenantId: null, principalId: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain(TenantA);
            body.ShouldNotContain(PrincipalA);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_IncludeArchivedTrue_IncludesArchivedCandidate()
    {
        WebApplication app = await StartAppAsync(
            metadata: Linked(ProjectAId),
            rows: [Row(ProjectAId, "Project Alpha", ProjectLifecycle.Archived)]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage excludedByDefault = await client.SendAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            using (JsonDocument document = JsonDocument.Parse(await excludedByDefault.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)))
            {
                document.RootElement.GetProperty("result").GetString().ShouldBe("NoMatch");
            }

            HttpResponseMessage includedWhenRequested = await client.SendAsync(
                Request($"/api/v1/projects/resolution/from-conversation?conversationId={ConversationIdValue}&includeArchived=true"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            using (JsonDocument document = JsonDocument.Parse(await includedWhenRequested.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)))
            {
                document.RootElement.GetProperty("result").GetString().ShouldBe("SingleCandidate");
                document.RootElement.GetProperty("candidates")[0].GetProperty("projectId").GetString().ShouldBe(ProjectAId);
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_ResponseHeaders_HaveCorrelationAndFreshness()
    {
        WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Resolve_CrossTenantConversation_YieldsNoCandidate()
    {
        // A conversation in another tenant is refused in-scope by the ACL (fail-closed Unavailable),
        // so it can never yield a candidate from the authoritative tenant's projects.
        WebApplication app = await StartAppAsync(
            metadata: ConversationResolutionMetadata.FailClosed(ConversationIdValue, ReferenceState.Unavailable),
            rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            await ProjectTenantIsolationConformance.AssertNoLeakageAsync(
                [
                    new ProjectTenantIsolationSurface(
                        "resolve-from-cross-tenant-conversation",
                        async ct =>
                        {
                            HttpResponseMessage response = await client.SendAsync(Request(), ct).ConfigureAwait(false);
                            string body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                            using JsonDocument document = JsonDocument.Parse(body);
                            bool leaked = document.RootElement.GetProperty("candidates").GetArrayLength() > 0
                                || body.Contains(ProjectAId, StringComparison.Ordinal);
                            return leaked
                                ? ProjectTenantIsolationResult.Leak("resolve-endpoint", null, ProjectAId)
                                : ProjectTenantIsolationResult.NoLeak("resolve-endpoint");
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
    public async Task Resolve_ResponseBody_HasNoLeakageAcrossOutcomes()
    {
        foreach ((string label, Func<HttpClient, Task<HttpResponseMessage>> send) in new (string, Func<HttpClient, Task<HttpResponseMessage>>)[]
            {
                ("happy", c => c.SendAsync(Request(), TestContext.Current.CancellationToken)),
                ("idempotencyRejected", c => c.SendAsync(WithHeader(Request(), "Idempotency-Key", "idem-x"), TestContext.Current.CancellationToken)),
                ("freshnessRejected", c => c.SendAsync(WithHeader(Request(), "X-Hexalith-Freshness", "strong"), TestContext.Current.CancellationToken)),
                ("missingConversation", c => c.SendAsync(Request("/api/v1/projects/resolution/from-conversation"), TestContext.Current.CancellationToken)),
            })
        {
            WebApplication app = await StartAppAsync(metadata: Linked(ProjectAId), rows: [Row(ProjectAId, "Project Alpha")]).ConfigureAwait(true);
            try
            {
                using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                HttpResponseMessage response = await send(client).ConfigureAwait(true);
                string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
                Should.NotThrow(
                    () => NoPayloadLeakageAssertions.AssertNoLeakageInText(body),
                    $"label={label} status={response.StatusCode}");
            }
            finally
            {
                await StopAsync(app).ConfigureAwait(true);
            }
        }
    }

    private static ConversationResolutionMetadata Linked(string projectId)
        => new(ConversationIdValue, projectId, SafeLabel: null, ReferenceState.Included);

    private static ProjectListItem Row(string projectId, string name, ProjectLifecycle lifecycle = ProjectLifecycle.Active)
        => new(TenantA, projectId, name, lifecycle, Sequence: 1, CreatedAt: DateTimeOffset.UnixEpoch, UpdatedAt: DateTimeOffset.UnixEpoch);

    private static HttpRequestMessage Request(string? url = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url ?? $"/api/v1/projects/resolution/from-conversation?conversationId={ConversationIdValue}");
        request.Headers.Add("X-Correlation-Id", CorrelationIdValue);
        return request;
    }

    private static HttpRequestMessage WithHeader(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Add(name, value);
        return request;
    }

    private static async Task<WebApplication> StartAppAsync(
        ConversationResolutionMetadata metadata,
        IReadOnlyList<ProjectListItem> rows,
        string? tenantId = TenantA,
        string? principalId = PrincipalA,
        bool seedTenantAccess = true,
        bool listThrows = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddProjectsServer();
        builder.Services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        builder.Services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        builder.Services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        builder.Services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        builder.Services.RemoveAll<IProjectTenantContextAccessor>();
        builder.Services.AddSingleton<IProjectTenantContextAccessor>(new FixedProjectTenantContext(tenantId, principalId));
        builder.Services.RemoveAll<IProjectConversationResolutionDirectory>();
        builder.Services.AddSingleton<IProjectConversationResolutionDirectory>(new StubConversationResolutionDirectory(metadata));
        builder.Services.RemoveAll<IProjectListReadModel>();
        builder.Services.AddSingleton<IProjectListReadModel>(new StubProjectListReadModel(rows, listThrows));

        WebApplication app = builder.Build();
        if (seedTenantAccess && !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(principalId))
        {
            ProjectTenantAccessProjection projection = new()
            {
                TenantId = tenantId,
                Enabled = true,
                Watermark = 1,
                ProjectionWatermark = $"{tenantId}:1",
                LastEventTimestamp = DateTimeOffset.UtcNow,
            };
            projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");
            await app.Services.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
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

    private sealed class FixedProjectTenantContext(string? tenantId, string? principalId) : IProjectTenantContextAccessor
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
                        ProjectAuthorizationGate.ListProjectsAction,
                        ProjectAuthorizationGate.ReadProjectAction,
                    ]);
    }

    private sealed class StubConversationResolutionDirectory(ConversationResolutionMetadata metadata) : IProjectConversationResolutionDirectory
    {
        public Task<ConversationResolutionMetadata> ReadConversationMetadataAsync(
            ConversationId conversationId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(metadata);
    }

    private sealed class StubProjectListReadModel(IReadOnlyList<ProjectListItem> rows, bool throws) : IProjectListReadModel
    {
        public Task<IReadOnlyList<ProjectListItem>> ListAsync(
            string authoritativeTenantId,
            ProjectLifecycle? lifecycleFilter,
            CancellationToken cancellationToken = default)
            => throws
                ? Task.FromException<IReadOnlyList<ProjectListItem>>(new InvalidOperationException("list read model unavailable"))
                : Task.FromResult(rows);
    }
}
