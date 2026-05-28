// <copyright file="ProjectFileReferenceDirectoryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

using FoldersGeneratedClient = Hexalith.Folders.Client.Generated.Client;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;

using Shouldly;

using Xunit;

/// <summary>Tier-2 tests for the Projects-to-Folders metadata-only file-reference ACL adapter (Story 2.5).</summary>
public sealed class ProjectFileReferenceDirectoryTests
{
    private const string FolderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private const string WorkspaceId = "workspace_01HZ9K8YQ3W6V2N4R7T5P0X1AD";
    private const string FilePath = "docs/synthetic-note.md";

    [Fact]
    public async Task ValidateLink_AuthorizedNotRedactedFile_Accepts()
    {
        RecordingHandler handler = new(JsonResponse(HttpStatusCode.OK, MetadataJson("file", "not_redacted", stale: false)));
        FoldersProjectFileReferenceDirectory directory = Directory(handler);

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Accepted);

        // Only the metadata-only route is ever called; the content-bearing range-read route is never used.
        handler.RequestPaths.ShouldContain(path => path.Contains("/context/metadata", System.StringComparison.Ordinal));
        handler.RequestPaths.ShouldNotContain(path => path.Contains("/context/range-read", System.StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateLink_RedactedFile_FailsClosed()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, MetadataJson("file", "redacted", stale: false))));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Redacted);
    }

    [Fact]
    public async Task ValidateLink_ExcludedFile_FailsClosed()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, MetadataJson("file", "excluded", stale: false))));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Redacted);
    }

    [Fact]
    public async Task ValidateLink_DirectoryKind_FailsClosedAsDenied()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, MetadataJson("directory", "not_redacted", stale: false))));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Denied);
    }

    [Fact]
    public async Task ValidateLink_StaleEvidence_FailsClosedAsStale()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, MetadataJson("file", "not_redacted", stale: true))));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Stale);
    }

    [Fact]
    public async Task ValidateLink_MissingFile_FailsClosedAsDenied()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, EmptyMetadataJson())));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Denied);
    }

    [Fact]
    public async Task ValidateLink_FoldersSafeDenial_FailsClosedAsDenied()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.NotFound, ProblemJson())));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Denied);
    }

    [Fact]
    public async Task ValidateLink_FoldersArchivedConflict_FailsClosedAsArchived()
    {
        // A Folders 409 conflict means the file or its folder is archived/inactive; the ACL must
        // fail closed as Archived (which the endpoint collapses to a safe denial), never accept.
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.Conflict, ProblemJson())));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Archived);
    }

    [Fact]
    public async Task ValidateLink_FoldersInputLimit_FailsClosedAsValidationFailed()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse((HttpStatusCode)422, ProblemJson())));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.ValidationFailed);
    }

    [Fact]
    public async Task ValidateLink_FoldersServerError_FailsClosedAsUnavailable()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.InternalServerError, ProblemJson())));

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Unavailable);
    }

    [Fact]
    public async Task ValidateLink_TransportFailure_FailsClosedAsUnavailable()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(new ThrowingHandler());

        ProjectFileReferenceValidationResult result = await ValidateAsync(directory).ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Unavailable);
    }

    [Fact]
    public async Task ValidateLink_BlankAddressingFields_FailsClosedAsValidationFailed()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(
            new RecordingHandler(JsonResponse(HttpStatusCode.OK, MetadataJson("file", "not_redacted", stale: false))));

        ProjectFileReferenceValidationResult result = await directory
            .ValidateLinkFileReferenceAsync(ProjectId(), FolderId, " ", FilePath, "corr-a", "task-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.ValidationFailed);
    }

    [Fact]
    public async Task ValidateLink_Cancellation_Propagates()
    {
        FoldersProjectFileReferenceDirectory directory = Directory(new ThrowingHandler());
        using CancellationTokenSource cts = new();
        await cts.CancelAsync().ConfigureAwait(true);

        await Should.ThrowAsync<System.OperationCanceledException>(async () => await directory
            .ValidateLinkFileReferenceAsync(ProjectId(), FolderId, WorkspaceId, FilePath, "corr-a", "task-a", cts.Token)
            .ConfigureAwait(true)).ConfigureAwait(true);
    }

    private static Task<ProjectFileReferenceValidationResult> ValidateAsync(FoldersProjectFileReferenceDirectory directory)
        => directory.ValidateLinkFileReferenceAsync(ProjectId(), FolderId, WorkspaceId, FilePath, "corr-a", "task-a", TestContext.Current.CancellationToken);

    private static FoldersProjectFileReferenceDirectory Directory(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("http://folders.test/"),
        };
        return new FoldersProjectFileReferenceDirectory(new FoldersGeneratedClient(httpClient));
    }

    private static ProjectId ProjectId() => new("01HZ9K8YQ3W6V2N4R7T5P0X1AB");

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static string MetadataJson(string kind, string redaction, bool stale)
        => $$"""
        {
          "items": [
            {
              "path": {
                "normalizedPath": "{{FilePath}}",
                "displayName": "synthetic-note.md",
                "pathPolicyClass": "tenant_sensitive_document",
                "unicodeNormalization": "NFC"
              },
              "kind": "{{kind}}",
              "byteLength": 42,
              "sensitivity": "tenant_sensitive",
              "redaction": "{{redaction}}"
            }
          ],
          "freshness": {
            "readConsistency": "eventually_consistent",
            "observedAt": "2026-05-12T12:34:56Z",
            "projectionWatermark": "watermark_00000001",
            "stale": {{stale.ToString().ToLowerInvariant()}}
          }
        }
        """;

    private static string EmptyMetadataJson()
        => """
        {
          "items": [],
          "freshness": {
            "readConsistency": "eventually_consistent",
            "observedAt": "2026-05-12T12:34:56Z",
            "projectionWatermark": "watermark_00000001",
            "stale": false
          }
        }
        """;

    private static string ProblemJson()
        => """
        {
          "type": "about:blank",
          "title": "Access unavailable",
          "status": 404,
          "category": "tenant_access_denied",
          "code": "resource_unavailable",
          "message": "The requested resource is unavailable.",
          "correlationId": "corr-a",
          "retryable": false,
          "clientAction": "no_action",
          "details": { "visibility": "redacted" }
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
