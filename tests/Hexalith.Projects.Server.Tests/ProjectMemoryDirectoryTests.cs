// <copyright file="ProjectMemoryDirectoryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

using Hexalith.Memories.Client.Rest;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Memories;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-2 tests for the Projects-to-Memories metadata-only memory-reference ACL adapter (Story 2.7).
/// Convergence is asserted via deterministic fakes (stubbed <see cref="HttpMessageHandler"/> behind a
/// real <see cref="MemoriesClient"/>); no <c>Thread.Sleep</c> / <c>Task.Delay</c> / <c>SpinWait</c> /
/// wall-clock polling. Asserts the ACL request lane contains only the stable <c>GetCaseAsync</c> route.
/// </summary>
public sealed class ProjectMemoryDirectoryTests
{
    private const string TenantId = "tenant-a";
    private const string MemoryReferenceId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1";

    [Fact]
    public async Task ValidateLink_ActiveCaseSameTenant_Accepts()
    {
        RecordingHandler handler = new(JsonResponse(HttpStatusCode.OK, CaseJson("Active", TenantId)));
        MemoriesProjectMemoryDirectory directory = Directory(handler);

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Accepted);

        // Only the stable GetCaseAsync route is ever called; never any content-bearing or experimental
        // surface (no /memory-units, /search, /traverse, /export, /handlers, /ingest, /telemetry).
        handler.RequestPaths.ShouldAllBe(path => path.Contains($"/api/tenants/{TenantId}/cases/", StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/memory-units", StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/search", StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/traverse", StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/export", StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/handlers", StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/ingest", StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/telemetry", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateLink_ClosedCase_FailsClosedAsArchived()
    {
        MemoriesProjectMemoryDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, CaseJson("Closed", TenantId))));

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Archived);
    }

    [Fact]
    public async Task ValidateLink_DeletingCase_FailsClosedAsArchived()
    {
        MemoriesProjectMemoryDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, CaseJson("Deleting", TenantId))));

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Archived);
    }

    [Fact]
    public async Task ValidateLink_CrossTenantCase_FailsClosedAsTenantMismatch()
    {
        MemoriesProjectMemoryDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, CaseJson("Active", "tenant-other"))));

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.TenantMismatch);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ValidateLink_MemoriesAuthorizationDenied_FailsClosedAsDenied(HttpStatusCode statusCode)
    {
        MemoriesProjectMemoryDirectory directory = Directory(
            new RecordingHandler(JsonResponse(statusCode, ErrorJson("CASE_NOT_FOUND"))));

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Denied);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task ValidateLink_MemoriesUnavailable_FailsClosedAsUnavailable(HttpStatusCode statusCode)
    {
        MemoriesProjectMemoryDirectory directory = Directory(
            new RecordingHandler(JsonResponse(statusCode, ErrorJson("UPSTREAM_UNAVAILABLE"))));

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Unavailable);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task ValidateLink_MemoriesValidationFailure_FailsClosedAsValidationFailed(HttpStatusCode statusCode)
    {
        MemoriesProjectMemoryDirectory directory = Directory(
            new RecordingHandler(JsonResponse(statusCode, ErrorJson("INVALID_RESPONSE"))));

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.ValidationFailed);
    }

    [Fact]
    public async Task ValidateLink_TransportFailure_FailsClosedAsUnavailable()
    {
        MemoriesProjectMemoryDirectory directory = Directory(new ThrowingHandler());

        ProjectMemoryValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Unavailable);
    }

    [Fact]
    public async Task ValidateLink_BlankInputs_FailsClosedAsValidationFailed()
    {
        MemoriesProjectMemoryDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, CaseJson("Active", TenantId))));

        ProjectMemoryValidationResult result = await directory
            .ValidateLinkMemoryReferenceAsync(ProjectId(), MemoryReferenceId, " ", "corr-a", "task-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.ValidationFailed);
    }

    [Fact]
    public async Task ValidateLink_Cancellation_Propagates()
    {
        MemoriesProjectMemoryDirectory directory = Directory(new ThrowingHandler());
        using CancellationTokenSource cts = new();
        await cts.CancelAsync().ConfigureAwait(true);

        await Should.ThrowAsync<OperationCanceledException>(async () => await directory
            .ValidateLinkMemoryReferenceAsync(ProjectId(), MemoryReferenceId, TenantId, "corr-a", "task-a", cts.Token)
            .ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public async Task UnavailableDirectory_AlwaysReturnsUnavailable()
    {
        UnavailableProjectMemoryDirectory directory = new();

        ProjectMemoryValidationResult result = await directory
            .ValidateLinkMemoryReferenceAsync(ProjectId(), MemoryReferenceId, TenantId, "corr-a", "task-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Unavailable);
    }

    private static Task<ProjectMemoryValidationResult> ValidateAsync(MemoriesProjectMemoryDirectory directory)
        => directory.ValidateLinkMemoryReferenceAsync(ProjectId(), MemoryReferenceId, TenantId, "corr-a", "task-a", TestContext.Current.CancellationToken);

    private static MemoriesProjectMemoryDirectory Directory(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("http://memories.test/"),
        };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions());
        MemoriesClient client = new(httpClient, options, NullLogger<MemoriesClient>.Instance);
        return new MemoriesProjectMemoryDirectory(client);
    }

    private static ProjectId ProjectId() => new("01HZ9K8YQ3W6V2N4R7T5P0X1AB");

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static string CaseJson(string status, string tenantId)
        => $$"""
        {
          "id": "{{MemoryReferenceId}}",
          "tenantId": "{{tenantId}}",
          "name": "Synthetic Case",
          "status": "{{status.ToLowerInvariant()}}",
          "createdAt": "2026-05-12T12:34:56Z",
          "lastUpdated": "2026-05-12T12:34:56Z",
          "memoryUnitCount": 3
        }
        """;

    private static string ErrorJson(string code)
        => $$"""
        {
          "code": "{{code}}",
          "message": "synthetic",
          "suggestion": "synthetic"
        }
        """;

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public List<string> RequestPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new HttpRequestException("Synthetic transport failure.");
        }
    }
}
