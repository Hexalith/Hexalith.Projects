// <copyright file="ResolveProjectFromAttachmentsTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.Testing.TenantIsolation;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Shouldly;

using Xunit;

/// <summary>Story 4.3 Tier-2 endpoint tests for <c>GET /api/v1/projects/resolution/from-attachments</c>.</summary>
public sealed class ResolveProjectFromAttachmentsTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string PrincipalA = "principal-a";
    private const string ProjectAId = "project-a";
    private const string ProjectBId = "project-b";
    private const string FolderId = "folder-001";
    private const string FileId = "file-001";
    private const string CorrelationIdValue = "corr-001";
    private const string TaskIdValue = "task-001";

    [Fact]
    public async Task Resolve_FolderHappyPath_SingleCandidate_Returns200()
    {
        EndpointResponse response = await SendAsync(rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Header("X-Correlation-Id").ShouldBe(CorrelationIdValue);
        response.Header("X-Hexalith-Freshness").ShouldBe("eventually_consistent");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("result").GetString().ShouldBe("SingleCandidate");
        document.RootElement.GetProperty("candidates")[0].GetProperty("projectId").GetString().ShouldBe(ProjectAId);
        document.RootElement.GetProperty("candidates")[0].GetProperty("reasonCodes")[0].GetString().ShouldBe("ProjectFolderMatched");
        document.RootElement.TryGetProperty("tenantId", out _).ShouldBeFalse();
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(response.Body));
    }

    [Fact]
    public async Task Resolve_FileHappyPath_SingleCandidate_Returns200()
    {
        EndpointResponse response = await SendAsync(
            url: $"/api/v1/projects/resolution/from-attachments?fileId={FileId}",
            rows: [Row(ProjectAId, Reference(AttachmentKind.File, FileId))]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("result").GetString().ShouldBe("SingleCandidate");
        document.RootElement.GetProperty("candidates")[0].GetProperty("reasonCodes")[0].GetString().ShouldBe("FileReferenceMatched");
    }

    [Fact]
    public async Task Resolve_TwoQualifyingProjects_ReturnsMultipleCandidates()
    {
        EndpointResponse response = await SendAsync(
            url: $"/api/v1/projects/resolution/from-attachments?folderId={FolderId}&fileId={FileId}",
            rows:
            [
                Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId)),
                Row(ProjectBId, Reference(AttachmentKind.File, FileId, projectId: ProjectBId)),
            ]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("result").GetString().ShouldBe("MultipleCandidates");
        document.RootElement.GetProperty("candidates").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task Resolve_NoReferencedProject_ReturnsNoMatch()
    {
        EndpointResponse response = await SendAsync(rows: []).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("result").GetString().ShouldBe("NoMatch");
        document.RootElement.GetProperty("candidates").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Resolve_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
            headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["Idempotency-Key"] = "idem-should-be-rejected" }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
    }

    [Fact]
    public async Task Resolve_InvalidFreshness_ReturnsValidationProblem()
    {
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
            headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Hexalith-Freshness"] = "strong" }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("freshness");
    }

    [Fact]
    public async Task Resolve_TooManyAttachmentIds_ReturnsValidationProblem()
    {
        string query = string.Join(
            "&",
            Enumerable.Range(0, 33).Select(static index => "fileId=file-" + index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)));

        EndpointResponse response = await SendAsync(
            "/api/v1/projects/resolution/from-attachments?" + query,
            [Row(ProjectAId, Reference(AttachmentKind.File, FileId))]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("attachments");
    }

    [Theory]
    [InlineData("/api/v1/projects/resolution/from-attachments")]
    [InlineData("/api/v1/projects/resolution/from-attachments?folderId=bad/slash")]
    [InlineData("/api/v1/projects/resolution/from-attachments?fileId=bad%20space")]
    public async Task Resolve_MissingOrMalformedAttachmentId_ReturnsSafeDenial404(string url)
    {
        EndpointResponse response = await SendAsync(
            url,
            [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Resolve_ReadModelUnavailable_Returns503()
    {
        EndpointResponse response = await SendAsync(rows: [], referenceIndexThrows: true).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("read_model_unavailable");
        document.RootElement.GetProperty("retryable").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Resolve_IncludeArchivedTrue_IncludesArchivedCandidate()
    {
        IReadOnlyList<ProjectReferenceIndexCandidateRow> rows =
        [
            Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId), ProjectLifecycle.Archived),
        ];

        EndpointResponse excluded = await SendAsync(rows: rows).ConfigureAwait(true);
        using (JsonDocument document = JsonDocument.Parse(excluded.Body))
        {
            document.RootElement.GetProperty("result").GetString().ShouldBe("NoMatch");
        }

        EndpointResponse included = await SendAsync(
            url: $"/api/v1/projects/resolution/from-attachments?folderId={FolderId}&includeArchived=true",
            rows: rows).ConfigureAwait(true);
        using (JsonDocument document = JsonDocument.Parse(included.Body))
        {
            document.RootElement.GetProperty("result").GetString().ShouldBe("SingleCandidate");
        }
    }

    [Fact]
    public async Task Resolve_CrossTenantReference_YieldsNoCandidate()
    {
        // The stub returns the tenant-B row verbatim (no tenant filter of its own), so this asserts the
        // ENDPOINT's Ordinal tenant re-filter drops it — that defensive branch is not dead code.
        await ProjectTenantIsolationConformance.AssertNoLeakageAsync(
            [
                new ProjectTenantIsolationSurface(
                    "resolve-from-cross-tenant-attachment",
                    async ct =>
                    {
                        EndpointResponse response = await SendAsync(
                            rows: [Row(ProjectBId, Reference(AttachmentKind.Folder, FolderId, tenantId: TenantB, projectId: ProjectBId), tenantId: TenantB)]).ConfigureAwait(false);
                        using JsonDocument document = JsonDocument.Parse(response.Body);
                        bool leaked = document.RootElement.GetProperty("candidates").GetArrayLength() > 0
                            || response.Body.Contains(ProjectBId, StringComparison.Ordinal);
                        return leaked
                            ? ProjectTenantIsolationResult.Leak("resolve-attachments", null, ProjectBId)
                            : ProjectTenantIsolationResult.NoLeak("resolve-attachments");
                    }),
            ],
            TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    [Fact]
    public async Task Resolve_ResponseBody_HasNoLeakageAcrossOutcomes()
    {
        foreach ((string label, Func<Task<EndpointResponse>> send) in new (string, Func<Task<EndpointResponse>>)[]
            {
                ("happy", () => SendAsync(rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))])),
                ("idempotencyRejected", () => SendAsync(
                    rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
                    headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["Idempotency-Key"] = "idem-x" })),
                ("freshnessRejected", () => SendAsync(
                    rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
                    headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Hexalith-Freshness"] = "strong" })),
                ("missingAttachment", () => SendAsync("/api/v1/projects/resolution/from-attachments", [])),
            })
        {
            EndpointResponse response = await send().ConfigureAwait(true);
            Should.NotThrow(
                () => NoPayloadLeakageAssertions.AssertNoLeakageInText(response.Body),
                $"label={label} status={response.StatusCode}");
        }
    }

    [Fact]
    public async Task Resolve_UnauthorizedCaller_ReturnsSafeDenial404()
    {
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
            seedTenantAccess: false).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Body.ShouldNotContain(ProjectAId);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(response.Body));
    }

    [Fact]
    public async Task Resolve_AuthoritativeTenantIdMissing_ReturnsSafeDenial404()
    {
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
            tenantId: null,
            principalId: null,
            seedTenantAccess: false).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Body.ShouldNotContain(TenantA);
        response.Body.ShouldNotContain(PrincipalA);
    }

    [Fact]
    public async Task Resolve_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404()
    {
        // Authorization (endpoint line ~58) runs before the Idempotency-Key rejection (line ~68), so an
        // unauthorized caller receives the safe-denial 404 rather than a 400 that would leak request-shape
        // feedback to an unauthenticated principal.
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
            seedTenantAccess: false,
            headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["Idempotency-Key"] = "idem-should-not-leak" }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Resolve_InvalidIncludeArchived_ReturnsValidationProblem()
    {
        EndpointResponse response = await SendAsync(
            url: $"/api/v1/projects/resolution/from-attachments?folderId={FolderId}&includeArchived=maybe",
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("includeArchived");
    }

    [Fact]
    public async Task Resolve_CombinedAttachmentCapBoundary_RejectsThirtyThreeAcrossBothParams()
    {
        // The server caps the COMBINED unique id count at 32; 32 folderId + 1 fileId must be rejected even
        // though each array alone is within the spine's per-array maxItems: 32 (OpenAPI cannot express a
        // cross-parameter sum, so the server is the authoritative limit).
        string folders = string.Join(
            "&",
            Enumerable.Range(0, 32).Select(static index => "folderId=folder-" + index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)));

        EndpointResponse response = await SendAsync(
            "/api/v1/projects/resolution/from-attachments?" + folders + "&fileId=" + FileId,
            [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("attachments");
    }

    [Theory]
    [InlineData(ReferenceState.Stale)]
    [InlineData(ReferenceState.Unauthorized)]
    [InlineData(ReferenceState.Unavailable)]
    public async Task Resolve_DegradedReferenceState_ExcludesCandidateNotMatch(ReferenceState degraded)
    {
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId, state: degraded))]).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("result").GetString().ShouldBe("NoMatch");
        document.RootElement.GetProperty("candidates").GetArrayLength().ShouldBe(0);
        JsonElement excluded = document.RootElement.GetProperty("excluded");
        excluded.GetArrayLength().ShouldBeGreaterThan(0);
        excluded[0].GetProperty("projectId").GetString().ShouldBe(ProjectAId);
    }

    [Fact]
    public async Task Resolve_CanonicalTaskId_ThreadedIntoValidationProblemBody()
    {
        // X-Hexalith-Task-Id is metadata-only and never echoed on the 200 path; it surfaces on an error
        // outcome as the ProblemDetails taskId extension (proving the threading + canonicalization).
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
            headers: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Idempotency-Key"] = "idem-should-be-rejected",
                ["X-Hexalith-Task-Id"] = TaskIdValue,
            }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("taskId").GetString().ShouldBe(TaskIdValue);
    }

    [Fact]
    public async Task Resolve_MalformedTaskId_IsDroppedFromValidationProblemBody()
    {
        EndpointResponse response = await SendAsync(
            rows: [Row(ProjectAId, Reference(AttachmentKind.Folder, FolderId))],
            headers: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Idempotency-Key"] = "idem-should-be-rejected",
                ["X-Hexalith-Task-Id"] = "bad/slash",
            }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.TryGetProperty("taskId", out _).ShouldBeFalse();
    }

    private static async Task<EndpointResponse> SendAsync(
        string? url = null,
        IReadOnlyList<ProjectReferenceIndexCandidateRow>? rows = null,
        string? tenantId = TenantA,
        string? principalId = PrincipalA,
        bool seedTenantAccess = true,
        bool referenceIndexThrows = false,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FixedProjectTenantContext tenantContext = new(tenantId, principalId);
        StubProjectReferenceIndexReadModel readModel = new(rows ?? [], referenceIndexThrows);

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddProjectsServer();
        services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        services.RemoveAll<IProjectTenantContextAccessor>();
        services.AddSingleton<IProjectTenantContextAccessor>(tenantContext);
        services.RemoveAll<IProjectReferenceIndexReadModel>();
        services.AddSingleton<IProjectReferenceIndexReadModel>(readModel);

        using ServiceProvider provider = services.BuildServiceProvider();
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
            await provider.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(projection, cancellationToken).ConfigureAwait(false);
        }

        DefaultHttpContext httpContext = new()
        {
            RequestServices = provider,
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Method = HttpMethods.Get;
        string requestUrl = url ?? $"/api/v1/projects/resolution/from-attachments?folderId={FolderId}";
        int queryIndex = requestUrl.IndexOf('?', StringComparison.Ordinal);
        httpContext.Request.Path = queryIndex < 0 ? requestUrl : requestUrl[..queryIndex];
        httpContext.Request.QueryString = queryIndex < 0 ? QueryString.Empty : new QueryString(requestUrl[queryIndex..]);
        httpContext.Request.Headers["X-Correlation-Id"] = CorrelationIdValue;
        if (headers is not null)
        {
            foreach ((string name, string value) in headers)
            {
                httpContext.Request.Headers[name] = value;
            }
        }

        MethodInfo method = typeof(ProjectsDomainServiceEndpoints)
            .GetMethod("ResolveProjectFromAttachmentsAsync", BindingFlags.NonPublic | BindingFlags.Static)
            .ShouldNotBeNull();
        object? task = method.Invoke(
            null,
            [
                httpContext,
                tenantContext,
                provider.GetRequiredService<ProjectAuthorizationGate>(),
                readModel,
                provider.GetRequiredService<ProjectResolutionEngine>(),
                provider.GetRequiredService<TimeProvider>(),
                cancellationToken,
            ]);
        IResult result = await ((Task<IResult>)task!).ConfigureAwait(false);
        await result.ExecuteAsync(httpContext).ConfigureAwait(false);

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body);
        return new EndpointResponse(
            (HttpStatusCode)httpContext.Response.StatusCode,
            await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false),
            httpContext.Response.Headers.ToDictionary(static header => header.Key, static header => header.Value.ToString(), StringComparer.OrdinalIgnoreCase));
    }

    private static ProjectReferenceIndexCandidateRow Row(
        string projectId,
        ProjectReferenceIndexItem reference,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        string tenantId = TenantA)
        => new(tenantId, projectId, "Project " + projectId, lifecycle, [reference]);

    private static ProjectReferenceIndexItem Reference(
        AttachmentKind kind,
        string referenceId,
        ReferenceState state = ReferenceState.Included,
        string projectId = ProjectAId,
        string tenantId = TenantA)
        => new(
            tenantId,
            projectId,
            kind == AttachmentKind.Folder ? "folder" : "file",
            referenceId,
            state,
            DisplayName: null,
            ReasonCode: null,
            UpdatedAt: DateTimeOffset.UnixEpoch,
            Sequence: 1);

    private enum AttachmentKind
    {
        Folder,
        File,
    }

    private sealed record EndpointResponse(
        HttpStatusCode StatusCode,
        string Body,
        IReadOnlyDictionary<string, string> Headers)
    {
        public string? Header(string name)
            => Headers.TryGetValue(name, out string? value) ? value : null;
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

    private sealed class StubProjectReferenceIndexReadModel(
        IReadOnlyList<ProjectReferenceIndexCandidateRow> rows,
        bool throws) : IProjectReferenceIndexReadModel
    {
        public Task<IReadOnlyList<ProjectReferenceIndexCandidateRow>> ListByReferenceAsync(
            string authoritativeTenantId,
            IReadOnlyCollection<string> folderIds,
            IReadOnlyCollection<string> fileReferenceIds,
            CancellationToken cancellationToken = default)
            => throws
                ? Task.FromException<IReadOnlyList<ProjectReferenceIndexCandidateRow>>(new InvalidOperationException("reference index unavailable"))
                // The stub deliberately does NOT filter by tenant: it returns whatever rows it is given
                // (reference-id matched only) so the endpoint's own Ordinal tenant re-filter is the code
                // under test for cross-tenant isolation. The real read-model/projection/mapper tenant
                // filter is proven separately by InMemoryProjectReferenceIndexReadModelTests.
                : Task.FromResult<IReadOnlyList<ProjectReferenceIndexCandidateRow>>(
                    rows
                        .Select(row => row with
                        {
                            MatchedReferences = row.MatchedReferences
                                .Where(reference =>
                                    (string.Equals(reference.ReferenceKind, "folder", StringComparison.Ordinal)
                                        && reference.ReferenceId is not null
                                        && folderIds.Contains(reference.ReferenceId, StringComparer.Ordinal))
                                    || (string.Equals(reference.ReferenceKind, "file", StringComparison.Ordinal)
                                        && reference.ReferenceId is not null
                                        && fileReferenceIds.Contains(reference.ReferenceId, StringComparer.Ordinal)))
                                .ToArray(),
                        })
                        .Where(row => row.MatchedReferences.Count > 0)
                        .ToArray());
    }
}
