// <copyright file="ProjectFolderDirectoryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Net;
using System.Net.Http;
using System.Text;

using FoldersGeneratedClient = Hexalith.Folders.Client.Generated.Client;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;

using Shouldly;

using Xunit;

/// <summary>Tier-2 tests for the Projects-to-Folders ACL adapter.</summary>
public sealed class ProjectFolderDirectoryTests
{
    private const string FolderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";

    [Fact]
    public async Task ValidateSetProjectFolder_ActiveFolderAndReadPermission_Accepts()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: false)),
            JsonResponse(HttpStatusCode.OK, PermissionsJson("allowed", "read", stale: false)));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Accepted);
    }

    /// <summary>Checks the v2 lifecycle and permissions routes and the required task identity.</summary>
    [Fact]
    public async Task ValidateSetProjectFolder_EffectivePermissions_MatchesClientTaskIdContract()
    {
        ProjectFolderDirectoryResponseHandler handler = new(
        [
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: false)),
            JsonResponse(HttpStatusCode.OK, PermissionsJson("allowed", "read", stale: false)),
        ]);
        FoldersProjectFolderDirectory directory = Directory(handler);

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Accepted);
        handler.RequestPaths.ShouldBe([
            $"/api/v2/folders/{FolderId}/lifecycle-status",
            $"/api/v2/folders/{FolderId}/effective-permissions",
        ]);
        handler.TaskIds.ShouldBe([null, "corr-a"]);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_StaleLifecycleEvidence_FailsClosed()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: true)));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Stale);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_ArchivedLifecycleEvidence_FailsClosed()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: true, stale: false)));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Archived);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_MismatchedLifecycleEvidence_FailsClosedAsUnavailable()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(
                HttpStatusCode.OK,
                LifecycleJson(archived: false, stale: false, folderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1ZZ")));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Unavailable);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_DeniedPermissionEvidence_FailsClosed()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: false)),
            JsonResponse(HttpStatusCode.OK, PermissionsJson("denied_safe", "read", stale: false)));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Denied);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_StalePermissionEvidence_FailsClosed()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: false)),
            JsonResponse(HttpStatusCode.OK, PermissionsJson("allowed", "read", stale: true)));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Stale);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_NoUsablePermission_FailsClosed()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: false)),
            JsonResponse(HttpStatusCode.OK, PermissionsJsonWithoutPermissions("allowed", stale: false)));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Denied);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_FoldersSafeDenial_FailsClosedAsDenied()
    {
        FoldersProjectFolderDirectory directory = Directory(JsonResponse(HttpStatusCode.NotFound, ProblemJson()));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Denied);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ProjectFolderValidationOutcome.Denied)]
    [InlineData(HttpStatusCode.NotFound, ProjectFolderValidationOutcome.Denied)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ProjectFolderValidationOutcome.Unavailable)]
    public async Task ValidateSetProjectFolder_CanonicalStatus_MapsTypedOutcome(
        HttpStatusCode statusCode,
        ProjectFolderValidationOutcome expectedOutcome)
    {
        FoldersProjectFolderDirectory directory = Directory(JsonResponse(statusCode, ProblemJson(statusCode)));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(expectedOutcome);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ProjectFolderValidationOutcome.Denied)]
    [InlineData(HttpStatusCode.NotFound, ProjectFolderValidationOutcome.Denied)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ProjectFolderValidationOutcome.Unavailable)]
    public async Task RefreshFolderReference_PermissionsCanonicalStatus_MapsTypedOutcome(
        HttpStatusCode statusCode,
        ProjectFolderValidationOutcome expectedOutcome)
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: false)),
            JsonResponse(statusCode, ProblemJson(statusCode)));

        ProjectFolderValidationResult result = await directory
            .RefreshFolderReferenceAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(expectedOutcome);
    }

    [Fact]
    public async Task RefreshFolderReference_MalformedPermissions_IsUnavailable()
    {
        FoldersProjectFolderDirectory directory = Directory(
            JsonResponse(HttpStatusCode.OK, LifecycleJson(archived: false, stale: false)),
            JsonResponse(HttpStatusCode.OK, "{}"));

        ProjectFolderValidationResult result = await directory
            .RefreshFolderReferenceAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Unavailable);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_MalformedSuccess_IsUnavailable()
    {
        FoldersProjectFolderDirectory directory = Directory(JsonResponse(HttpStatusCode.OK, "{}"));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Unavailable);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_FoldersServerError_FailsClosedAsUnavailable()
    {
        FoldersProjectFolderDirectory directory = Directory(JsonResponse(HttpStatusCode.InternalServerError, ProblemJson()));

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Unavailable);
    }

    [Fact]
    public async Task ValidateSetProjectFolder_TransportFailure_FailsClosedAsUnavailable()
    {
        FoldersProjectFolderDirectory directory = Directory();

        ProjectFolderValidationResult result = await directory
            .ValidateSetProjectFolderAsync(ProjectId(), FolderId, "corr-a", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectFolderValidationOutcome.Unavailable);
    }

    private static FoldersProjectFolderDirectory Directory(params HttpResponseMessage[] responses)
        => Directory(new ProjectFolderDirectoryResponseHandler(responses));

    private static FoldersProjectFolderDirectory Directory(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("http://folders.test/"),
        };
        return new FoldersProjectFolderDirectory(new FoldersGeneratedClient(httpClient));
    }

    private static ProjectId ProjectId() => new("01HZ9K8YQ3W6V2N4R7T5P0X1AB");

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static string LifecycleJson(bool archived, bool stale, string? folderId = null)
        => $$"""
        {
          "folderId": "{{folderId ?? FolderId}}",
          "lifecycleState": "ready",
          "archived": {{archived.ToString().ToLowerInvariant()}},
          "repositoryBindingId": "repo_01HZ9K8YQ3W6V2N4R7T5P0X1AD",
          "providerBindingRef": "provider_01HZ9K8YQ3W6V2N4R7T5P0X1AE",
          "freshness": {
            "readConsistency": "eventually_consistent",
            "observedAt": "2026-05-12T12:34:56Z",
            "projectionWatermark": "watermark_00000001",
            "stale": {{stale.ToString().ToLowerInvariant()}}
          }
        }
        """;

    private static string PermissionsJson(string outcome, string permission, bool stale, string? folderId = null)
        => $$"""
        {
          "folderId": "{{folderId ?? FolderId}}",
          "permissions": ["{{permission}}"],
          "authorizationOutcome": "{{outcome}}",
          "freshness": {
            "readConsistency": "eventually_consistent",
            "observedAt": "2026-05-12T12:34:56Z",
            "projectionWatermark": "watermark_00000001",
            "stale": {{stale.ToString().ToLowerInvariant()}}
          }
        }
        """;

    private static string PermissionsJsonWithoutPermissions(string outcome, bool stale, string? folderId = null)
        => $$"""
        {
          "folderId": "{{folderId ?? FolderId}}",
          "permissions": [],
          "authorizationOutcome": "{{outcome}}",
          "freshness": {
            "readConsistency": "eventually_consistent",
            "observedAt": "2026-05-12T12:34:56Z",
            "projectionWatermark": "watermark_00000001",
            "stale": {{stale.ToString().ToLowerInvariant()}}
          }
        }
        """;

    private static string ProblemJson(HttpStatusCode statusCode = HttpStatusCode.NotFound)
    {
        (string category, string code, string action, bool retryable, string title, string message) = statusCode switch
        {
            HttpStatusCode.Unauthorized => ("authentication_failure", "authentication_required", "check_credentials", false, "Authentication required", "Authentication is required."),
            HttpStatusCode.ServiceUnavailable => ("read_model_unavailable", "projection_unavailable", "retry", true, "Authorization evidence unavailable", "Authorization evidence is temporarily unavailable."),
            _ => ("tenant_access_denied", "resource_unavailable", "no_action", false, "Resource not available", "The requested resource is unavailable."),
        };
        return $$"""
        {
          "type": "about:blank",
          "title": "{{title}}",
          "status": {{(int)statusCode}},
          "category": "{{category}}",
          "code": "{{code}}",
          "message": "{{message}}",
          "correlationId": "corr-a",
          "retryable": {{retryable.ToString().ToLowerInvariant()}},
          "clientAction": "{{action}}",
          "details": { "visibility": "redacted" }
        }
        """;
    }

}
