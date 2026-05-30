// <copyright file="ProposeNewProjectEndpointTests.cs" company="Hexalith">
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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Proposals;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

using Xunit;

/// <summary>Story 4.5 Tier-2 endpoint tests for proposal preview and composite confirm.</summary>
public sealed class ProposeNewProjectEndpointTests
{
    private const string TenantA = "tenant-a";
    private const string PrincipalA = "principal-a";
    private const string ConversationIdValue = "conversation-001";
    private const string ProjectIdValue = "project-001";
    private const string ExistingProjectId = "project-existing";
    private const string FolderIdValue = "folder-001";
    private const string FileIdValue = "file-001";
    private const string WorkspaceIdValue = "workspace-001";
    private const string CorrelationIdValue = "corr-001";
    private const string TaskIdValue = "task-001";
    private const string IdempotencyKeyValue = "proposal-001";

    [Fact]
    public async Task Preview_NoMatch_ReturnsMetadataOnlyProposal()
    {
        using ServiceProvider provider = await BuildProviderAsync().ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Header("X-Correlation-Id").ShouldBe(CorrelationIdValue);
        response.Header("X-Hexalith-Freshness").ShouldBe("eventually_consistent");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("resolutionResult").GetString().ShouldBe("NoMatch");
        document.RootElement.GetProperty("suggestedName").GetString().ShouldBe("Suggested Project");
        document.RootElement.GetProperty("conversationId").GetString().ShouldBe(ConversationIdValue);
        document.RootElement.GetProperty("fileReferenceIds")[0].GetString().ShouldBe(FileIdValue);
        document.RootElement.TryGetProperty("tenantId", out _).ShouldBeFalse();
        document.RootElement.TryGetProperty("filePath", out _).ShouldBeFalse();
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(response.Body));
    }

    [Fact]
    public async Task Preview_ExistingCandidate_ReturnsValidationProblemWithoutLeakingCandidate()
    {
        using ServiceProvider provider = await BuildProviderAsync(
            listRows: [ProjectRow(ExistingProjectId, "Existing Project")],
            conversation: new ConversationResolutionMetadata(ConversationIdValue, ExistingProjectId, "Existing Project", ReferenceState.Included)).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("resolutionResult");
        response.Body.ShouldNotContain(ExistingProjectId);
        response.Body.ShouldNotContain("Existing Project");
    }

    [Fact]
    public async Task Preview_MultipleCandidates_ReturnsValidationProblemWithoutLeakingCandidates()
    {
        using ServiceProvider provider = await BuildProviderAsync(
            referenceRows:
            [
                ReferenceRow("project-alpha", FolderIdValue, "folder"),
                ReferenceRow("project-beta", FileIdValue, "file"),
            ]).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("resolutionResult");
        response.Body.ShouldNotContain("project-alpha");
        response.Body.ShouldNotContain("project-beta");
    }

    [Fact]
    public async Task Preview_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        using ServiceProvider provider = await BuildProviderAsync().ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(
            provider,
            PreviewBody(),
            headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["Idempotency-Key"] = IdempotencyKeyValue }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
    }

    [Fact]
    public async Task Preview_UnauthorizedCaller_ReturnsSafeDenial()
    {
        using ServiceProvider provider = await BuildProviderAsync(tenantId: null, principalId: null).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Body.ShouldNotContain(ConversationIdValue);
    }

    [Theory]
    [InlineData(ReferenceState.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ReferenceState.Stale, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ReferenceState.Unauthorized, HttpStatusCode.NotFound)]
    public async Task Preview_ConversationPreflightFailure_FailsClosed(ReferenceState state, HttpStatusCode expectedStatus)
    {
        using ServiceProvider provider = await BuildProviderAsync(
            conversation: ConversationResolutionMetadata.FailClosed(ConversationIdValue, state)).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(expectedStatus);
        response.Body.ShouldNotContain("Conversation Project");
    }

    [Fact]
    public async Task Preview_ReferenceIndexUnavailable_Returns503()
    {
        using ServiceProvider provider = await BuildProviderAsync(referenceIndexThrows: true).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Confirm_FullFlow_SubmitsCreateAssignmentFolderAndFilesWithDerivedKeys()
    {
        CapturingProjectCommandSubmitter submitter = new();
        CapturingAssignmentDirectory assignment = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, assignmentDirectory: assignment).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitter.Created.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":create");
        assignment.Confirmed.Single().Metadata.IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":conversation");
        submitter.Folders.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":folder");
        submitter.Files.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":file:" + FileIdValue);
        submitter.Files.Single().FileMetadata.DisplayName.ShouldBe("Design brief");
    }

    [Fact]
    public async Task Confirm_CreateReplayStillResumesAssignmentFolderAndFiles()
    {
        CapturingProjectCommandSubmitter submitter = new();
        submitter.CreateResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitter.Created.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":create");
        submitter.Folders.Count.ShouldBe(1);
        submitter.Files.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Confirm_AssignmentConflict_Returns409AfterCreateForRetryRecovery()
    {
        CapturingProjectCommandSubmitter submitter = new();
        CapturingAssignmentDirectory assignment = new(new ProjectConversationAssignmentResult(ProjectConversationAssignmentOutcome.Conflict, "assignment-corr"));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, assignmentDirectory: assignment).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        submitter.Created.Count.ShouldBe(1);
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_FolderDenied_ReturnsSafeDenialBeforeCreate()
    {
        CapturingProjectCommandSubmitter submitter = new();
        RecordingFolderDirectory folderDirectory = new(ProjectFolderValidationOutcome.Denied);
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, folderDirectory: folderDirectory).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        folderDirectory.CallCount.ShouldBe(1);
        submitter.Created.ShouldBeEmpty();
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_FileDenied_ReturnsSafeDenialBeforeCreate()
    {
        CapturingProjectCommandSubmitter submitter = new();
        RecordingFileReferenceDirectory fileDirectory = new(ProjectFileReferenceValidationOutcome.Denied);
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, fileReferenceDirectory: fileDirectory).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        fileDirectory.CallCount.ShouldBe(1);
        submitter.Created.ShouldBeEmpty();
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_FileCommandUnavailableThenSameRootRetrySucceeds()
    {
        CapturingProjectCommandSubmitter submitter = new();
        submitter.FileResults.Enqueue(new ProjectCommandSubmissionResult(ProjectCommandSubmissionOutcome.Unavailable, "projects-corr"));
        submitter.CreateResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        submitter.FolderResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        submitter.FileResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse first = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);
        EndpointResponse second = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        second.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitter.Created.Count.ShouldBe(2);
        submitter.Files.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Confirm_SameRootKeyDifferentBody_Returns409BeforeDuplicateCreate()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse first = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);
        EndpointResponse second = await SendConfirmAsync(provider, ConfirmBody(displayName: "Different Project")).ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        submitter.Created.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("\"projectId\": \"bad/slash\"", "identity")]
    [InlineData("\"fileReferenceIds\": [\"file-999\"]", "fileReferenceIds")]
    public async Task Confirm_MalformedBody_ReturnsValidationProblemBeforeWrites(string replacement, string rejectedField)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(rejectedField);

        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);
        string body = replacement.StartsWith("\"projectId\"", StringComparison.Ordinal)
            ? ConfirmBody().Replace("\"projectId\": \"project-001\"", replacement, StringComparison.Ordinal)
            : ConfirmBody().Replace("\"fileReferenceIds\": [\"file-001\"]", replacement, StringComparison.Ordinal);

        EndpointResponse response = await SendConfirmAsync(provider, body).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain(rejectedField);
        submitter.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_MissingIdempotencyKey_ReturnsValidationProblemBeforeWrites()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody(), includeIdempotencyKey: false).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("idempotency_key");
        submitter.Created.ShouldBeEmpty();
    }

    private static async Task<ServiceProvider> BuildProviderAsync(
        IReadOnlyList<ProjectListItem>? listRows = null,
        IReadOnlyList<ProjectReferenceIndexCandidateRow>? referenceRows = null,
        bool referenceIndexThrows = false,
        ConversationResolutionMetadata? conversation = null,
        CapturingProjectCommandSubmitter? submitter = null,
        CapturingAssignmentDirectory? assignmentDirectory = null,
        RecordingFolderDirectory? folderDirectory = null,
        RecordingFileReferenceDirectory? fileReferenceDirectory = null,
        string? tenantId = TenantA,
        string? principalId = PrincipalA)
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddProjectsServer();
        services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        services.RemoveAll<IProjectTenantContextAccessor>();
        services.AddSingleton<IProjectTenantContextAccessor>(new FixedProjectTenantContext(tenantId, principalId));
        services.RemoveAll<IProjectConversationResolutionDirectory>();
        services.AddSingleton<IProjectConversationResolutionDirectory>(new StubConversationResolutionDirectory(
            conversation ?? new ConversationResolutionMetadata(ConversationIdValue, null, "Conversation Project", ReferenceState.Included)));
        services.RemoveAll<IProjectListReadModel>();
        services.AddSingleton<IProjectListReadModel>(new StubProjectListReadModel(listRows ?? []));
        services.RemoveAll<IProjectReferenceIndexReadModel>();
        services.AddSingleton<IProjectReferenceIndexReadModel>(new StubProjectReferenceIndexReadModel(referenceRows ?? [], referenceIndexThrows));
        services.RemoveAll<IProjectCommandSubmitter>();
        services.AddSingleton<IProjectCommandSubmitter>(submitter ?? new CapturingProjectCommandSubmitter());
        services.RemoveAll<IProjectConversationAssignmentDirectory>();
        services.AddSingleton<IProjectConversationAssignmentDirectory>(assignmentDirectory ?? new CapturingAssignmentDirectory(ProjectConversationAssignmentResult.Accepted("assignment-corr")));
        services.RemoveAll<IProjectFolderDirectory>();
        services.AddSingleton<IProjectFolderDirectory>(folderDirectory ?? new RecordingFolderDirectory(ProjectFolderValidationOutcome.Accepted));
        services.RemoveAll<IProjectFileReferenceDirectory>();
        services.AddSingleton<IProjectFileReferenceDirectory>(fileReferenceDirectory ?? new RecordingFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Accepted));

        ServiceProvider provider = services.BuildServiceProvider();
        if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(principalId))
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
            await provider.GetRequiredService<IProjectTenantAccessProjectionStore>()
                .SaveAsync(projection, TestContext.Current.CancellationToken)
                .ConfigureAwait(false);
        }

        return provider;
    }

    private static Task<EndpointResponse> SendPreviewAsync(
        ServiceProvider provider,
        string body,
        IReadOnlyDictionary<string, string>? headers = null)
        => InvokeAsync(
            provider,
            "ProposeNewProjectAsync",
            body,
            headers,
            args =>
            [
                args.HttpContext,
                provider.GetRequiredService<IProjectTenantContextAccessor>(),
                provider.GetRequiredService<ProjectAuthorizationGate>(),
                provider.GetRequiredService<IProjectConversationResolutionDirectory>(),
                provider.GetRequiredService<IProjectListReadModel>(),
                provider.GetRequiredService<IProjectReferenceIndexReadModel>(),
                provider.GetRequiredService<ProjectResolutionEngine>(),
                provider.GetRequiredService<TimeProvider>(),
                args.CancellationToken,
            ]);

    private static Task<EndpointResponse> SendConfirmAsync(
        ServiceProvider provider,
        string body,
        bool includeIdempotencyKey = true)
    {
        Dictionary<string, string> headers = new(StringComparer.Ordinal);
        if (includeIdempotencyKey)
        {
            headers["Idempotency-Key"] = IdempotencyKeyValue;
        }

        return InvokeAsync(
            provider,
            "ConfirmNewProjectProposalAsync",
            body,
            headers,
            args =>
            [
                args.HttpContext,
                provider.GetRequiredService<IProjectCommandSubmitter>(),
                provider.GetRequiredService<IProjectTenantContextAccessor>(),
                provider.GetRequiredService<ProjectAuthorizationGate>(),
                provider.GetRequiredService<IProjectConversationResolutionDirectory>(),
                provider.GetRequiredService<IProjectListReadModel>(),
                provider.GetRequiredService<IProjectReferenceIndexReadModel>(),
                provider.GetRequiredService<IProjectConversationAssignmentDirectory>(),
                provider.GetRequiredService<IProjectFolderDirectory>(),
                provider.GetRequiredService<IProjectFileReferenceDirectory>(),
                provider.GetRequiredService<ProjectResolutionEngine>(),
                provider.GetRequiredService<IProjectProposalConfirmationIdempotencyLedger>(),
                provider.GetRequiredService<TimeProvider>(),
                args.CancellationToken,
            ]);
    }

    private static async Task<EndpointResponse> InvokeAsync(
        ServiceProvider provider,
        string methodName,
        string body,
        IReadOnlyDictionary<string, string>? headers,
        Func<InvocationArgs, object?[]> buildArguments)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DefaultHttpContext httpContext = new()
        {
            RequestServices = provider,
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.Headers["X-Correlation-Id"] = CorrelationIdValue;
        httpContext.Request.Headers["X-Hexalith-Task-Id"] = TaskIdValue;
        if (headers is not null)
        {
            foreach ((string name, string value) in headers)
            {
                httpContext.Request.Headers[name] = value;
            }
        }

        MethodInfo method = typeof(ProjectsDomainServiceEndpoints)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            .ShouldNotBeNull();
        object? task = method.Invoke(null, buildArguments(new InvocationArgs(httpContext, cancellationToken)));
        IResult result = await ((Task<IResult>)task!).ConfigureAwait(false);
        await result.ExecuteAsync(httpContext).ConfigureAwait(false);

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body);
        return new EndpointResponse(
            (HttpStatusCode)httpContext.Response.StatusCode,
            await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false),
            httpContext.Response.Headers.ToDictionary(static header => header.Key, static header => header.Value.ToString(), StringComparer.OrdinalIgnoreCase));
    }

    private static string PreviewBody()
        => $$"""
            {
              "requestSchemaVersion": "v1",
              "conversationId": "{{ConversationIdValue}}",
              "folderId": "{{FolderIdValue}}",
              "fileReferenceIds": ["{{FileIdValue}}"],
              "suggestedName": "Suggested Project",
              "description": "Safe project description",
              "setupMetadata": "Safe setup note"
            }
            """;

    private static string ConfirmBody(string displayName = "Suggested Project")
        => $$"""
            {
              "requestSchemaVersion": "v1",
              "operation": "confirmNewProjectProposal",
              "resolutionResult": "NoMatch",
              "confirmed": true,
              "projectId": "{{ProjectIdValue}}",
              "conversationId": "{{ConversationIdValue}}",
              "projectMetadata": { "displayName": "{{displayName}}" },
              "description": "Safe project description",
              "setupMetadata": "Safe setup note",
              "folder": {
                "folderId": "{{FolderIdValue}}",
                "folderMetadata": { "displayName": "Workspace folder" }
              },
              "fileReferences": [
                {
                  "fileReferenceId": "{{FileIdValue}}",
                  "folderId": "{{FolderIdValue}}",
                  "workspaceId": "{{WorkspaceIdValue}}",
                  "filePath": "docs/readme.md",
                  "fileMetadata": { "displayName": "Design brief" }
                }
              ],
              "fileReferenceIds": ["{{FileIdValue}}"]
            }
            """;

    private static ProjectListItem ProjectRow(string projectId, string name)
        => new(TenantA, projectId, name, ProjectLifecycle.Active, 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

    private static ProjectReferenceIndexCandidateRow ReferenceRow(string projectId, string referenceId, string referenceKind)
        => new(
            TenantA,
            projectId,
            "Project " + projectId,
            ProjectLifecycle.Active,
            [
                new ProjectReferenceIndexItem(
                    TenantA,
                    projectId,
                    referenceKind,
                    referenceId,
                    ReferenceState.Included,
                    DisplayName: null,
                    ReasonCode: null,
                    UpdatedAt: DateTimeOffset.UnixEpoch,
                    Sequence: 1),
            ]);

    private sealed record InvocationArgs(HttpContext HttpContext, CancellationToken CancellationToken);

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
                        ProjectAuthorizationGate.CreateProjectAction,
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

    private sealed class StubProjectListReadModel(IReadOnlyList<ProjectListItem> rows) : IProjectListReadModel
    {
        public Task<IReadOnlyList<ProjectListItem>> ListAsync(
            string authoritativeTenantId,
            ProjectLifecycle? lifecycleFilter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(rows);
    }

    private sealed class StubProjectReferenceIndexReadModel(IReadOnlyList<ProjectReferenceIndexCandidateRow> rows, bool throws) : IProjectReferenceIndexReadModel
    {
        public Task<IReadOnlyList<ProjectReferenceIndexCandidateRow>> ListByReferenceAsync(
            string authoritativeTenantId,
            IReadOnlyCollection<string> folderIds,
            IReadOnlyCollection<string> fileReferenceIds,
            CancellationToken cancellationToken = default)
            => throws
                ? Task.FromException<IReadOnlyList<ProjectReferenceIndexCandidateRow>>(new InvalidOperationException("reference index unavailable"))
                : Task.FromResult(rows);
    }

    private sealed class CapturingAssignmentDirectory(ProjectConversationAssignmentResult result) : IProjectConversationAssignmentDirectory
    {
        public List<AssignmentCall> Confirmed { get; } = [];

        public Task<ProjectConversationAssignmentResult> LinkAsync(
            ProjectId projectId,
            ConversationId conversationId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            ProjectId? expectedCurrentProjectId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectConversationAssignmentResult> MoveAsync(
            ProjectId targetProjectId,
            ConversationId conversationId,
            ProjectId sourceProjectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectConversationAssignmentResult> UnlinkAsync(
            ProjectId projectId,
            ConversationId conversationId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectConversationAssignmentResult> ConfirmResolutionAssignmentAsync(
            ProjectId targetProjectId,
            ConversationId conversationId,
            ProjectId? expectedSourceProjectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            Confirmed.Add(new AssignmentCall(targetProjectId, conversationId, expectedSourceProjectId, metadata));
            return Task.FromResult(result);
        }
    }

    private sealed record AssignmentCall(
        ProjectId TargetProjectId,
        ConversationId ConversationId,
        ProjectId? ExpectedSourceProjectId,
        ProjectConversationCommandMetadata Metadata);

    private sealed class RecordingFolderDirectory(ProjectFolderValidationOutcome outcome) : IProjectFolderDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFolderValidationResult(outcome, correlationId));
        }

        public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));
    }

    private sealed class RecordingFileReferenceDirectory(ProjectFileReferenceValidationOutcome outcome) : IProjectFileReferenceDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFileReferenceValidationResult> ValidateLinkFileReferenceAsync(
            ProjectId projectId,
            string folderId,
            string workspaceId,
            string filePath,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFileReferenceValidationResult(outcome, correlationId));
        }

        public Task<ProjectFileReferenceValidationResult> RefreshFileReferenceAsync(
            ProjectId projectId,
            string fileReferenceId,
            string folderId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId));
    }

    private sealed class CapturingProjectCommandSubmitter : IProjectCommandSubmitter
    {
        private static readonly ProjectCommandSubmissionResult DefaultAccepted = ProjectCommandSubmissionResult.Accepted("projects-corr", false);

        public Queue<ProjectCommandSubmissionResult> CreateResults { get; } = [];

        public Queue<ProjectCommandSubmissionResult> FolderResults { get; } = [];

        public Queue<ProjectCommandSubmissionResult> FileResults { get; } = [];

        public List<CreateProject> Created { get; } = [];

        public List<SetProjectFolder> Folders { get; } = [];

        public List<LinkFileReference> Files { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
        {
            Created.Add(command);
            return Task.FromResult(CreateResults.TryDequeue(out ProjectCommandSubmissionResult? result) ? result : DefaultAccepted);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(SetProjectFolder command, CancellationToken cancellationToken = default)
        {
            Folders.Add(command);
            return Task.FromResult(FolderResults.TryDequeue(out ProjectCommandSubmissionResult? result) ? result : DefaultAccepted);
        }

        public Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(LinkFileReference command, CancellationToken cancellationToken = default)
        {
            Files.Add(command);
            return Task.FromResult(FileResults.TryDequeue(out ProjectCommandSubmissionResult? result) ? result : DefaultAccepted);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(UnlinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(LinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(UnlinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
            ConfirmProjectResolution command,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
    }
}
