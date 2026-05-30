// <copyright file="OperatorReadAccessTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

/// <summary>Story 5.2 operator read-access tests for the metadata-only project diagnostic query.</summary>
public sealed class OperatorReadAccessTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string PrincipalA = "principal-a";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string CorrelationIdValue = "corr_01HZ9K8YQ3W6V2N4R7T5P0X1XX";
    private static readonly DateTimeOffset ObservedAt = new(2026, 5, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetOperatorDiagnostics_HappyPath_ReturnsMetadataOnlyProjectAndAuditRows()
    {
        EndpointResult result = await InvokeAsync(
            auditRows:
            [
                AuditRow(TenantA, ProjectIdValue, "audit-002", "project.folder_set", "folder", "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC", 2),
                AuditRow(TenantA, ProjectIdValue, "audit-001", "project.created", null, null, 1),
            ]).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        result.Headers["X-Correlation-Id"].ToString().ShouldBe(CorrelationIdValue);
        result.Headers["X-Hexalith-Freshness"].ToString().ShouldBe("eventually_consistent");

        using JsonDocument document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
        document.RootElement.GetProperty("lifecycleState").GetString().ShouldBe("active");
        document.RootElement.GetProperty("references").GetArrayLength().ShouldBe(1);
        JsonElement audit = document.RootElement.GetProperty("auditTimeline");
        audit.GetArrayLength().ShouldBe(2);
        audit[0].GetProperty("auditEventId").GetString().ShouldBe("audit-002");
        audit[0].GetProperty("operationType").GetString().ShouldBe("project.folder_set");
        audit[0].TryGetProperty("idempotencyKey", out _).ShouldBeFalse();
        document.RootElement.TryGetProperty("tenantId", out _).ShouldBeFalse("tenant authority is server-derived and not returned on the wire.");
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(result.Body));
    }

    [Fact]
    public async Task GetOperatorDiagnostics_AuditLimitBoundsTimelineAfterAuthorization()
    {
        EndpointResult result = await InvokeAsync(
            queryString: "?auditLimit=2",
            auditRows:
            [
                AuditRow(TenantA, ProjectIdValue, "audit-003", "project.memory_linked", "memory", "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1", 3),
                AuditRow(TenantA, ProjectIdValue, "audit-002", "project.folder_set", "folder", "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC", 2),
                AuditRow(TenantA, ProjectIdValue, "audit-001", "project.created", null, null, 1),
            ]).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("auditTimeline").GetArrayLength().ShouldBe(2);
    }

    [Theory]
    [InlineData("0")]    // below minimum (parsed < 1)
    [InlineData("-5")]   // negative
    [InlineData("101")]  // above MaxOperatorAuditLimit (100)
    [InlineData("abc")]  // non-numeric (int.TryParse fails)
    public async Task GetOperatorDiagnostics_InvalidAuditLimitAfterAuthorization_ReturnsValidationProblem(string auditLimit)
    {
        EndpointResult result = await InvokeAsync(queryString: $"?auditLimit={auditLimit}").ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("auditLimit");
        result.Body.ShouldNotContain(ProjectIdValue);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(result.Body));
    }

    [Theory]
    [InlineData("100")] // at MaxOperatorAuditLimit boundary
    [InlineData("1")]   // at minimum boundary
    public async Task GetOperatorDiagnostics_BoundaryAuditLimit_IsAccepted(string auditLimit)
    {
        EndpointResult result = await InvokeAsync(
            queryString: $"?auditLimit={auditLimit}",
            auditRows: [AuditRow(TenantA, ProjectIdValue, "audit-001", "project.created", null, null, 1)]).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperatorDiagnostics_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404()
    {
        EndpointResult result = await InvokeAsync(seedTenantAccess: false, headers: [new("Idempotency-Key", "idem-probe")]).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
        result.Body.ShouldNotContain(ProjectIdValue);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(result.Body));
    }

    [Fact]
    public async Task GetOperatorDiagnostics_IdempotencyKeyAfterAuthorization_ReturnsValidationProblem()
    {
        // An authorized caller that sends Idempotency-Key on a query must receive validation feedback
        // (queries are not commands), and only AFTER authorization succeeds.
        EndpointResult result = await InvokeAsync(headers: [new("Idempotency-Key", "operator-query-is-not-a-command")]).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("..")]
    [InlineData("..%2F..")]
    [InlineData("path/with/slash")]
    [InlineData("' OR 1=1")]
    public async Task GetOperatorDiagnostics_MalformedProjectId_ReturnsSafeDenial404(string malformedProjectId)
    {
        EndpointResult result = await InvokeAsync(projectId: malformedProjectId).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(result.Body));
    }

    [Fact]
    public async Task GetOperatorDiagnostics_MissingAuthoritativeTenant_ReturnsSafeDenial404()
    {
        EndpointResult result = await InvokeAsync(seedTenantAccess: false, tenantId: null, principalId: null).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
        result.Body.ShouldNotContain(ProjectIdValue);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(result.Body));
    }

    [Fact]
    public async Task GetOperatorDiagnostics_CrossTenantProject_ReturnsSafeDenial404()
    {
        // The caller is authorized in tenant-a, but the project belongs to tenant-b: the detail read
        // model returns null for the authoritative tenant, so the boundary must be a safe-denial 404
        // with no cross-tenant existence disclosure.
        EndpointResult result = await InvokeAsync(tenantId: TenantA, principalId: PrincipalA, projectDetailTenantId: TenantB).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
        result.Body.ShouldNotContain(TenantB);
        result.Body.ShouldNotContain(ProjectIdValue);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(result.Body));
    }

    [Fact]
    public async Task GetOperatorDiagnostics_FreshnessProbeAfterAuthorization_ReturnsValidationProblem()
    {
        EndpointResult result = await InvokeAsync(headers: [new("X-Hexalith-Freshness", "strong")]).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("freshness");
    }

    [Fact]
    public async Task GetOperatorDiagnostics_DropsCrossTenantAuditRowsReturnedByReadModel()
    {
        EndpointResult result = await InvokeAsync(
            auditRows:
            [
                AuditRow(TenantA, ProjectIdValue, "audit-001", "project.created", null, null, 1),
                AuditRow(TenantB, ProjectIdValue, "audit-tenant-b", "project.created", null, null, 2),
            ]).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        result.Body.ShouldNotContain("audit-tenant-b");
        result.Body.ShouldNotContain(TenantB);
    }

    [Fact]
    public async Task GetOperatorDiagnostics_AuditProjectionUnavailable_Returns503()
    {
        EndpointResult result = await InvokeAsync(auditReadModelOverride: new ThrowingAuditTimelineReadModel()).ConfigureAwait(true);

        result.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
        result.Body.ShouldNotContain(ProjectIdValue);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(result.Body));
    }

    private static async Task<EndpointResult> InvokeAsync(
        bool seedTenantAccess = true,
        string? tenantId = TenantA,
        string? principalId = PrincipalA,
        string projectId = ProjectIdValue,
        string projectDetailTenantId = TenantA,
        string? queryString = null,
        IReadOnlyList<KeyValuePair<string, string>>? headers = null,
        IReadOnlyList<ProjectAuditTimelineItem>? auditRows = null,
        IProjectAuditTimelineReadModel? auditReadModelOverride = null)
    {
        IProjectTenantAccessProjectionStore store = new InMemoryProjectTenantAccessProjectionStore();
        if (seedTenantAccess && !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(principalId))
        {
            ProjectTenantAccessProjection projection = new()
            {
                TenantId = tenantId,
                Enabled = true,
                Watermark = 1,
                ProjectionWatermark = $"{tenantId}:1",
                LastEventTimestamp = ObservedAt,
            };
            projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");
            await store.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        DefaultHttpContext httpContext = new();
        httpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .ConfigureHttpJsonOptions(static _ => { })
            .BuildServiceProvider();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.QueryString = new QueryString(queryString ?? string.Empty);
        httpContext.Request.Headers["X-Correlation-Id"] = CorrelationIdValue;
        foreach (KeyValuePair<string, string> header in headers ?? [])
        {
            httpContext.Request.Headers[header.Key] = header.Value;
        }

        ProjectAuthorizationGate gate = new(
            new TenantAccessAuthorizer(store, new FixedUtcClock(ObservedAt.AddMinutes(5)), new TenantAccessOptions()),
            new AllowingProjectEventStoreAuthorizationValidator(),
            new AllowingProjectDaprPolicyEvidenceProvider(),
            new StubProjectDetailReadModel(ProjectDetail(projectDetailTenantId)));

        IResult endpointResult = await ProjectsDomainServiceEndpoints.GetProjectOperatorDiagnosticsAsync(
            projectId,
            httpContext,
            new FixedProjectTenantContext(tenantId, principalId),
            gate,
            auditReadModelOverride ?? new StubAuditTimelineReadModel(auditRows ?? []),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await endpointResult.ExecuteAsync(httpContext).ConfigureAwait(true);

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body, Encoding.UTF8);
        return new EndpointResult(httpContext.Response.StatusCode, httpContext.Response.Headers, await reader.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    private static ProjectDetailItem ProjectDetail(string tenantId)
        => new(
            TenantId: tenantId,
            ProjectId: ProjectIdValue,
            Name: "Operator Project",
            Description: "safe metadata",
            SetupMetadata: "setup-metadata",
            Setup: new ProjectSetup(
                ["troubleshoot safely"],
                ["never expose payloads"],
                [ProjectContextSourceKind.Conversation],
                [ProjectContextSourceKind.FileReference],
                new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences)),
            ProjectFolder: new ProjectFolderReference(
                FolderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                DisplayName: "Operator Folder",
                ReferenceState: ReferenceState.Included,
                ReasonCode: null,
                ObservedAt: ObservedAt),
            FileReferences: [],
            MemoryReferences: [],
            Lifecycle: ProjectLifecycle.Active,
            CreatedAt: ObservedAt,
            UpdatedAt: ObservedAt.AddMinutes(1),
            Sequence: 7);

    private static ProjectAuditTimelineItem AuditRow(
        string tenantId,
        string projectId,
        string auditEventId,
        string operationType,
        string? referenceKind,
        string? referenceId,
        long sequence)
        => new(
            tenantId,
            projectId,
            auditEventId,
            operationType,
            ObservedAt.AddMinutes(sequence),
            PrincipalA,
            "corr-" + sequence,
            "task-" + sequence,
            "idem-" + sequence,
            referenceKind,
            referenceId,
            null,
            "included",
            "confirmed",
            referenceKind == "conversation" ? referenceId : null,
            null,
            sequence);

    private sealed record EndpointResult(int StatusCode, IHeaderDictionary Headers, string Body);

    private sealed class FixedProjectTenantContext(string? tenantId, string? principalId) : IProjectTenantContextAccessor
    {
        public string? AuthoritativeTenantId { get; } = tenantId;

        public string? PrincipalId { get; } = principalId;

        public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
            => string.IsNullOrWhiteSpace(AuthoritativeTenantId) || string.IsNullOrWhiteSpace(PrincipalId)
                ? EventStoreClaimTransformEvidence.Missing()
                : EventStoreClaimTransformEvidence.Allowed(AuthoritativeTenantId, PrincipalId, [ProjectAuthorizationGate.ReadProjectAction]);
    }

    private sealed class StubProjectDetailReadModel(ProjectDetailItem detail) : IProjectDetailReadModel
    {
        public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
            => Task.FromResult<ProjectDetailItem?>(
                string.Equals(detail.TenantId, authoritativeTenantId, StringComparison.Ordinal)
                    && string.Equals(detail.ProjectId, projectId, StringComparison.Ordinal)
                    ? detail
                    : null);
    }

    private sealed class StubAuditTimelineReadModel(IReadOnlyList<ProjectAuditTimelineItem> rows) : IProjectAuditTimelineReadModel
    {
        public Task<IReadOnlyList<ProjectAuditTimelineItem>> ListAsync(
            string authoritativeTenantId,
            string? projectId,
            int? limit,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProjectAuditTimelineItem>>(rows.Take(limit ?? rows.Count).ToArray());
    }

    private sealed class ThrowingAuditTimelineReadModel : IProjectAuditTimelineReadModel
    {
        public Task<IReadOnlyList<ProjectAuditTimelineItem>> ListAsync(
            string authoritativeTenantId,
            string? projectId,
            int? limit,
            CancellationToken cancellationToken = default)
            => Task.FromException<IReadOnlyList<ProjectAuditTimelineItem>>(new InvalidOperationException("audit projection unavailable"));
    }
}
