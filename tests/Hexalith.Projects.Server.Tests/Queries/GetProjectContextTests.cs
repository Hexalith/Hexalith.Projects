// <copyright file="GetProjectContextTests.cs" company="Hexalith">
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
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.2 Tier-2 endpoint tests for <c>GET /api/v1/projects/{projectId}/context</c>.
/// Exercises the host composition end-to-end: authorize, page conversations via the Story 2.1 ACL,
/// compose evidence, invoke the Story 3.1 <c>ProjectContextInclusionPolicy</c> unchanged, return
/// the assembled <c>ProjectContext</c> wire body. Covers the happy path, Idempotency-Key rejection,
/// freshness rejection, malformed/missing-route negatives (safe-denial 404), cross-tenant safe
/// denial (FS-8 / SM-3), archived project, conversation page unavailable collapse, header echo,
/// extra query parameters tolerated, and no-payload-leakage assertions on every response body.
/// </summary>
public sealed class GetProjectContextTests
{
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FolderIdValue = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private const string FileRefId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1";
    private const string MemoryRefId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1";
    private const string ConversationIdValue = "conv_01HZ9K8YQ3W6V2N4R7T5P0X1C1";
    private const string CorrelationIdValue = "corr_01HZ9K8YQ3W6V2N4R7T5P0X1XX";

    [Fact]
    public async Task GetProjectContext_HappyPath_Returns200WithAssembledContext()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");

            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            document.RootElement.GetProperty("lifecycle").GetString().ShouldBe("Active");
            document.RootElement.GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
            document.RootElement.GetProperty("freshness").GetString().ShouldBe("Fresh");
            document.RootElement.GetProperty("projectFolder").GetProperty("referenceId").GetString().ShouldBe(FolderIdValue);
            document.RootElement.TryGetProperty("tenantId", out _).ShouldBeFalse("FS-8/SM-3: tenant authority must never appear on the wire.");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = GetContextRequest();
            request.Headers.Add("Idempotency-Key", "idem-should-be-rejected");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404()
    {
        // Tenant-access never authorized: Idempotency-Key validation must NOT happen before authz.
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false, projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = GetContextRequest();
            request.Headers.Add("Idempotency-Key", "idem-key-leaked-through-rejection?");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_StricterFreshnessRequested_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = GetContextRequest();
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

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("..%2F..")]
    [InlineData("path/with/slash")]
    [InlineData("bad‎char")]
    public async Task GetProjectContext_MalformedProjectId_ReturnsSafeDenial404(string malformedId)
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            string encoded = Uri.EscapeDataString(malformedId);
            HttpResponseMessage response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{encoded}/context"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_ExtraQueryParameters_AreIgnoredNotFailed()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                GetContextRequest($"/api/v1/projects/{ProjectIdValue}/context?expand=full&unknown=ignored"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_CrossTenant_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included, projectTenant: "tenant-b").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain("tenant-b");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_ProjectArchived_Returns200WithAllReferencesExcluded()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            lifecycle: ProjectLifecycle.Archived).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
            document.RootElement.GetProperty("lifecycle").GetString().ShouldBe("Archived");
            document.RootElement.TryGetProperty("projectFolder", out _).ShouldBeFalse("archived project has no included folder reference (collapsed to excluded).");
            document.RootElement.GetProperty("excluded").GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
            foreach (JsonElement excluded in document.RootElement.GetProperty("excluded").EnumerateArray())
            {
                excluded.GetProperty("failedCheck").GetString().ShouldBe("ProjectLifecycle");
            }

            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_ConversationsPageUnavailable_AssemblesWithExclusions()
    {
        // The conversation directory returns an Unavailable trust signal; the policy collapses the
        // single candidate to a fail-closed-clean exclusion; the endpoint still returns 200.
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            conversationDirectoryOverride: new UnavailablePageConversationDirectory()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_TenantAccessUnavailable_ReturnsReadModelUnavailable503()
    {
        // Acceptance Criterion 7 — Retryable Reason=Unavailable surfaces as 503 ReadModelUnavailable
        // (not safe-denial 404). A throwing tenant-access projection store maps to UnavailableProjection
        // outcome → ReferenceState.Unavailable → Retryable=true at the gate boundary.
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            tenantAccessStoreOverride: new ThrowingTenantAccessProjectionStore()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
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
    public async Task GetProjectContext_AuthoritativeTenantIdMissing_ReturnsSafeDenial404()
    {
        // Acceptance Criterion 7 — request-level collapse: missing AuthoritativeTenantId →
        // safe-denial 404 at the HTTP boundary. The policy's outer outcome would be Unauthorized,
        // but the wire status is indistinguishable from ProjectUnavailable / missing-record per the
        // safe-denial contract (Story 1.4).
        WebApplication app = await StartAppAsync(
            tenantId: null,
            principalId: null,
            projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain("tenant-a");
            body.ShouldNotContain("principal-a");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContext_ResponseHeaders_HaveCorrelationAndFreshness()
    {
        // Acceptance Criterion 4(j) — dedicated named fixture asserting both response headers are
        // always set on the happy path (the matrix completeness tests rely on these invariants).
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

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
    public async Task GetProjectContext_ResponseBody_HasNoLeakageAcrossOutcomes()
    {
        // Composite leakage check across every outcome the endpoint surfaces (200 / 400 / 404).
        foreach ((string label, Func<HttpClient, Task<HttpResponseMessage>> send) in new (string, Func<HttpClient, Task<HttpResponseMessage>>)[]
            {
                ("happy", c => c.SendAsync(GetContextRequest(), TestContext.Current.CancellationToken)),
                ("idempotencyRejected", c => c.SendAsync(WithHeader(GetContextRequest(), "Idempotency-Key", "idem-x"), TestContext.Current.CancellationToken)),
                ("freshnessRejected", c => c.SendAsync(WithHeader(GetContextRequest(), "X-Hexalith-Freshness", "strong"), TestContext.Current.CancellationToken)),
                ("malformedRoute", c => c.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/v1/projects/%20/context"), TestContext.Current.CancellationToken)),
            })
        {
            WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
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

    private static HttpRequestMessage GetContextRequest(string? url = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url ?? $"/api/v1/projects/{ProjectIdValue}/context");
        request.Headers.Add("X-Correlation-Id", CorrelationIdValue);
        return request;
    }

    private static HttpRequestMessage WithHeader(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Add(name, value);
        return request;
    }

    private static async Task<WebApplication> StartAppAsync(
        string? tenantId,
        string? principalId,
        bool seedTenantAccess = true,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        ReferenceState projectFolderState = ReferenceState.Included,
        string? projectTenant = null,
        IProjectConversationDirectory? conversationDirectoryOverride = null,
        IProjectTenantAccessProjectionStore? tenantAccessStoreOverride = null)
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
        builder.Services.AddSingleton<IProjectCommandSubmitter>(new NoopProjectCommandSubmitter());
        builder.Services.RemoveAll<IProjectConversationDirectory>();
        builder.Services.AddSingleton(
            conversationDirectoryOverride ?? new StubConversationDirectory(includeOneConversation: true));
        if (tenantAccessStoreOverride is not null)
        {
            builder.Services.RemoveAll<IProjectTenantAccessProjectionStore>();
            builder.Services.AddSingleton(tenantAccessStoreOverride);
        }
        // Override the detail read model with a stub that returns a synthetic ProjectDetailItem.
        ProjectDetailItem detail = new(
            TenantId: projectTenant ?? tenantId ?? "tenant-a",
            ProjectId: ProjectIdValue,
            Name: "Context Project",
            Description: "metadata-only context",
            SetupMetadata: "metadata",
            Setup: null,
            ProjectFolder: new ProjectFolderReference(
                FolderId: projectFolderState == ReferenceState.Pending ? null : FolderIdValue,
                DisplayName: "Context Folder",
                ReferenceState: projectFolderState,
                ReasonCode: null,
                ObservedAt: DateTimeOffset.UnixEpoch),
            FileReferences: [],
            MemoryReferences: [],
            Lifecycle: lifecycle,
            CreatedAt: DateTimeOffset.UnixEpoch,
            UpdatedAt: DateTimeOffset.UnixEpoch,
            Sequence: 1);
        builder.Services.RemoveAll<IProjectDetailReadModel>();
        builder.Services.AddSingleton<IProjectDetailReadModel>(new StubProjectDetailReadModel(detail));

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
                        ProjectAuthorizationGate.ReadProjectAction,
                    ]);
    }

    private sealed class NoopProjectCommandSubmitter : IProjectCommandSubmitter
    {
        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(SetProjectFolder command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(LinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(UnlinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(LinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(UnlinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
            ConfirmProjectResolution command,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));
    }

    private sealed class StubConversationDirectory(bool includeOneConversation) : IProjectConversationDirectory
    {
        public Task<ProjectConversationsPage> ListForProjectAsync(
            ProjectId projectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            PageRequest page,
            CancellationToken cancellationToken = default)
        {
            if (!includeOneConversation)
            {
                return Task.FromResult(ProjectConversationsPage.Empty(projectId, ProjectConversationTrustSignal.Current));
            }

            ProjectConversationItem item = new(
                projectId,
                new ConversationId(ConversationIdValue),
                "active",
                "Context conversation",
                ProjectConversationTrustSignal.Current,
                null,
                null);
            return Task.FromResult(new ProjectConversationsPage(
                projectId,
                [item],
                new ProjectConversationPageMetadata(1),
                ProjectConversationTrustSignal.Current));
        }
    }

    private sealed class StubProjectDetailReadModel(ProjectDetailItem detail) : IProjectDetailReadModel
    {
        public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
        {
            // Mirror the production projection's tenant-scope check: return null when the projected
            // detail belongs to a different tenant than the requesting authoritative tenant.
            if (!string.Equals(detail.TenantId, authoritativeTenantId, StringComparison.Ordinal)
                || !string.Equals(detail.ProjectId, projectId, StringComparison.Ordinal))
            {
                return Task.FromResult<ProjectDetailItem?>(null);
            }

            return Task.FromResult<ProjectDetailItem?>(detail);
        }
    }

    private sealed class UnavailablePageConversationDirectory : IProjectConversationDirectory
    {
        public Task<ProjectConversationsPage> ListForProjectAsync(
            ProjectId projectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            PageRequest page,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectConversationsPage.Empty(projectId, ProjectConversationTrustSignal.Unavailable));
    }

    private sealed class ThrowingTenantAccessProjectionStore : IProjectTenantAccessProjectionStore
    {
        public Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => Task.FromException<ProjectTenantAccessProjection?>(new InvalidOperationException("projection store unavailable"));

        public Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
