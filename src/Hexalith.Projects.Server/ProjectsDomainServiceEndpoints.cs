// <copyright file="ProjectsDomainServiceEndpoints.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Memories;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Maps the Story 1.4 Project command-async + minimal-read endpoints. <c>POST /api/v1/projects</c> maps
/// the request to a <see cref="CreateProject"/> command, submits it through the EventStore command
/// pipeline (via <see cref="IProjectCommandSubmitter"/>), and returns <c>202 AcceptedCommand</c> on
/// accept — mapping a fail-closed denial to a safe-denial <c>404</c> (unauthorized and nonexistent are
/// externally indistinguishable; never a generic 500, never echoing whether a tenant/project exists).
/// <c>GET /api/v1/projects/{projectId}</c> returns the minimal projected detail with freshness. Tenant
/// authority comes from authenticated claims via <see cref="IProjectTenantContextAccessor"/> only —
/// never from payload/header/query.
/// </summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private const string FreshnessHeaderName = "X-Hexalith-Freshness";
    private const string EventuallyConsistent = "eventually_consistent";

    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalIdentifierRegex();

    /// <summary>Maps the Projects command-async + minimal-read endpoints onto the route builder.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapProjectsDomainServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/projects", static async (
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectListReadModel listReadModel,
            CancellationToken cancellationToken)
            => await ListProjectsAsync(httpContext, tenantContext, authorizationGate, listReadModel, cancellationToken).ConfigureAwait(false))
            .WithName("ListProjects");

        endpoints.MapPost("/api/v1/projects", static async (
            HttpContext httpContext,
            IProjectCommandSubmitter submitter,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await CreateProjectAsync(httpContext, submitter, tenantContext, authorizationGate, timeProvider, cancellationToken).ConfigureAwait(false))
            .WithName("CreateProject");

        endpoints.MapGet("/api/v1/projects/{projectId}", static async (
            string projectId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            CancellationToken cancellationToken)
            => await GetProjectAsync(projectId, httpContext, tenantContext, authorizationGate, cancellationToken).ConfigureAwait(false))
            .WithName("GetProject");

        endpoints.MapGet("/api/v1/projects/{projectId}/conversations", static async (
            string projectId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectConversationDirectory conversationDirectory,
            CancellationToken cancellationToken)
            => await ListProjectConversationsAsync(
                projectId,
                httpContext,
                tenantContext,
                authorizationGate,
                conversationDirectory,
                cancellationToken).ConfigureAwait(false))
            .WithName("ListProjectConversations");

        endpoints.MapGet("/api/v1/projects/{projectId}/context", static async (
            string projectId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectConversationDirectory conversationDirectory,
            Hexalith.Projects.Context.ProjectContextInclusionPolicy contextPolicy,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await GetProjectContextAsync(
                projectId,
                httpContext,
                tenantContext,
                authorizationGate,
                conversationDirectory,
                contextPolicy,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("GetProjectContext");

        endpoints.MapGet("/api/v1/projects/{projectId}/context/explain", static async (
            string projectId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectConversationDirectory conversationDirectory,
            Hexalith.Projects.Context.ProjectContextInclusionPolicy contextPolicy,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await GetProjectContextExplanationAsync(
                projectId,
                httpContext,
                tenantContext,
                authorizationGate,
                conversationDirectory,
                contextPolicy,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("GetProjectContextExplanation");

        endpoints.MapGet("/api/v1/projects/{projectId}/context/refresh", static async (
            string projectId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectConversationDirectory conversationDirectory,
            IProjectFolderDirectory folderDirectory,
            IProjectMemoryDirectory memoryDirectory,
            Hexalith.Projects.Context.ProjectContextInclusionPolicy contextPolicy,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await RefreshProjectContextAsync(
                projectId,
                httpContext,
                tenantContext,
                authorizationGate,
                conversationDirectory,
                folderDirectory,
                memoryDirectory,
                contextPolicy,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("RefreshProjectContext");

        endpoints.MapGet("/api/v1/projects/{projectId}/setup/conversation-start", static async (
            string projectId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            Hexalith.Projects.Context.ProjectContextInclusionPolicy contextPolicy,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await GetConversationStartSetupAsync(
                projectId,
                httpContext,
                tenantContext,
                authorizationGate,
                contextPolicy,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("GetConversationStartSetup");

        endpoints.MapGet("/api/v1/projects/resolution/from-conversation", static async (
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            Hexalith.Projects.Server.Conversations.IProjectConversationResolutionDirectory conversationResolutionDirectory,
            IProjectListReadModel listReadModel,
            Hexalith.Projects.Resolution.ProjectResolutionEngine resolutionEngine,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await ResolveProjectFromConversationAsync(
                httpContext,
                tenantContext,
                authorizationGate,
                conversationResolutionDirectory,
                listReadModel,
                resolutionEngine,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("ResolveProjectFromConversation");

        endpoints.MapGet("/api/v1/projects/resolution/from-attachments", static async (
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectReferenceIndexReadModel referenceIndexReadModel,
            Hexalith.Projects.Resolution.ProjectResolutionEngine resolutionEngine,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await ResolveProjectFromAttachmentsAsync(
                httpContext,
                tenantContext,
                authorizationGate,
                referenceIndexReadModel,
                resolutionEngine,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("ResolveProjectFromAttachments");

        endpoints.MapPost("/api/v1/projects/{projectId}/conversations/{conversationId}/link", static async (
            string projectId,
            string conversationId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectConversationAssignmentDirectory assignmentDirectory,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await LinkProjectConversationAsync(
                projectId,
                conversationId,
                httpContext,
                tenantContext,
                authorizationGate,
                assignmentDirectory,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("LinkProjectConversation");

        endpoints.MapPost("/api/v1/projects/{projectId}/conversations/{conversationId}/move", static async (
            string projectId,
            string conversationId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectConversationAssignmentDirectory assignmentDirectory,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await MoveProjectConversationAsync(
                projectId,
                conversationId,
                httpContext,
                tenantContext,
                authorizationGate,
                assignmentDirectory,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("MoveProjectConversation");

        endpoints.MapDelete("/api/v1/projects/{projectId}/conversations/{conversationId}", static async (
            string projectId,
            string conversationId,
            HttpContext httpContext,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectConversationAssignmentDirectory assignmentDirectory,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await UnlinkProjectConversationAsync(
                projectId,
                conversationId,
                httpContext,
                tenantContext,
                authorizationGate,
                assignmentDirectory,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("UnlinkProjectConversation");

        endpoints.MapPut("/api/v1/projects/{projectId}/folder", static async (
            string projectId,
            HttpContext httpContext,
            IProjectCommandSubmitter submitter,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            IProjectFolderDirectory folderDirectory,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await SetProjectFolderAsync(
                projectId,
                httpContext,
                submitter,
                tenantContext,
                authorizationGate,
                folderDirectory,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("SetProjectFolder");

        endpoints.MapPost("/api/v1/projects/{projectId}/files/{fileReferenceId}/link", static async (
            string projectId,
            string fileReferenceId,
            HttpContext httpContext,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectCommandSubmitter submitter,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectTenantContextAccessor tenantContext,
            [Microsoft.AspNetCore.Mvc.FromServices] ProjectAuthorizationGate authorizationGate,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectFileReferenceDirectory fileReferenceDirectory,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await LinkFileReferenceAsync(
                projectId,
                fileReferenceId,
                httpContext,
                submitter,
                tenantContext,
                authorizationGate,
                fileReferenceDirectory,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("LinkFileReference");

        endpoints.MapDelete("/api/v1/projects/{projectId}/files/{fileReferenceId}", static async (
            string projectId,
            string fileReferenceId,
            HttpContext httpContext,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectCommandSubmitter submitter,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectTenantContextAccessor tenantContext,
            [Microsoft.AspNetCore.Mvc.FromServices] ProjectAuthorizationGate authorizationGate,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await UnlinkFileReferenceAsync(
                projectId,
                fileReferenceId,
                httpContext,
                submitter,
                tenantContext,
                authorizationGate,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("UnlinkFileReference");

        endpoints.MapPost("/api/v1/projects/{projectId}/memories/{memoryReferenceId}/link", static async (
            string projectId,
            string memoryReferenceId,
            HttpContext httpContext,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectCommandSubmitter submitter,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectTenantContextAccessor tenantContext,
            [Microsoft.AspNetCore.Mvc.FromServices] ProjectAuthorizationGate authorizationGate,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectMemoryDirectory memoryDirectory,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await LinkMemoryAsync(
                projectId,
                memoryReferenceId,
                httpContext,
                submitter,
                tenantContext,
                authorizationGate,
                memoryDirectory,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("LinkMemory");

        endpoints.MapDelete("/api/v1/projects/{projectId}/memories/{memoryReferenceId}", static async (
            string projectId,
            string memoryReferenceId,
            HttpContext httpContext,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectCommandSubmitter submitter,
            [Microsoft.AspNetCore.Mvc.FromServices] IProjectTenantContextAccessor tenantContext,
            [Microsoft.AspNetCore.Mvc.FromServices] ProjectAuthorizationGate authorizationGate,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await UnlinkMemoryAsync(
                projectId,
                memoryReferenceId,
                httpContext,
                submitter,
                tenantContext,
                authorizationGate,
                timeProvider,
                cancellationToken).ConfigureAwait(false))
            .WithName("UnlinkMemory");

        endpoints.MapPatch("/api/v1/projects/{projectId}/setup", static async (
            string projectId,
            HttpContext httpContext,
            IProjectCommandSubmitter submitter,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await UpdateProjectSetupAsync(projectId, httpContext, submitter, tenantContext, authorizationGate, timeProvider, cancellationToken).ConfigureAwait(false))
            .WithName("UpdateProjectSetup");

        endpoints.MapPost("/api/v1/projects/{projectId}/archive", static async (
            string projectId,
            HttpContext httpContext,
            IProjectCommandSubmitter submitter,
            IProjectTenantContextAccessor tenantContext,
            ProjectAuthorizationGate authorizationGate,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
            => await ArchiveProjectAsync(projectId, httpContext, submitter, tenantContext, authorizationGate, timeProvider, cancellationToken).ConfigureAwait(false))
            .WithName("ArchiveProject");

        return endpoints;
    }

    private static async Task<IResult> CreateProjectAsync(
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? idempotencyKey = ReadHeader(httpContext, "Idempotency-Key");
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");

        // Idempotency key is required on the mutation; correlation/task are caller-provided or
        // generated. Validate canonical shape before any downstream use.
        if (string.IsNullOrWhiteSpace(idempotencyKey) || !IsCanonicalIdentifier(idempotencyKey))
        {
            return ValidationProblem(correlationId, taskId, "idempotency_key");
        }

        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : idempotencyKey;
        taskId = IsCanonicalIdentifier(taskId) ? taskId : correlationId;

        // Authorize before parsing the body so unauthorized callers cannot probe parsing feedback.
        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeCreateAsync(tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return SafeDenial(correlationId, taskId, authorization);
        }

        CreateProjectHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<CreateProjectHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body.ProjectMetadata is not null
            && !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "requestSchemaVersion");
        }

        if (!string.IsNullOrWhiteSpace(body.RequestSchemaVersion)
            && !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "requestSchemaVersion");
        }

        string? projectMetadataName = body.ProjectMetadata?.DisplayName;
        if (!string.IsNullOrWhiteSpace(projectMetadataName)
            && !string.IsNullOrWhiteSpace(body.Name)
            && !string.Equals(projectMetadataName.Trim(), body.Name.Trim(), StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "name");
        }

        string? submittedName = string.IsNullOrWhiteSpace(projectMetadataName) ? body.Name : projectMetadataName;
        if (string.IsNullOrWhiteSpace(submittedName))
        {
            return ValidationProblem(correlationId, taskId, "name");
        }

        ProjectId projectId;
        try
        {
            projectId = string.IsNullOrWhiteSpace(body.ProjectId)
                ? new ProjectId(Guid.NewGuid().ToString("N"))
                : new ProjectId(body.ProjectId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return ValidationProblem(correlationId, taskId, "projectId");
        }

        string authoritativeTenantId = tenantContext.AuthoritativeTenantId!;
        string principalId = tenantContext.PrincipalId!;

        CreateProject command = new(
            authoritativeTenantId,
            projectId,
            submittedName,
            body.Description,
            body.SetupMetadata,
            principalId,
            correlationId!,
            taskId!,
            idempotencyKey);

        ProjectCommandSubmissionResult result = await submitter
            .SubmitCreateProjectAsync(command, cancellationToken)
            .ConfigureAwait(false);

        string acceptedCorrelationId = IsCanonicalIdentifier(result.CorrelationId) ? result.CorrelationId! : correlationId!;

        return result.Outcome switch
        {
            ProjectCommandSubmissionOutcome.Accepted or ProjectCommandSubmissionOutcome.IdempotentReplay =>
                Accepted(httpContext, timeProvider, acceptedCorrelationId, taskId!, result.Outcome == ProjectCommandSubmissionOutcome.IdempotentReplay),
            ProjectCommandSubmissionOutcome.ValidationFailed => ValidationProblem(acceptedCorrelationId, taskId, "command"),
            ProjectCommandSubmissionOutcome.IdempotencyConflict => IdempotencyConflict(acceptedCorrelationId, taskId),
            ProjectCommandSubmissionOutcome.Unavailable => ReadModelUnavailable(acceptedCorrelationId, taskId),
            // Denied (and any default) → safe-denial 404.
            _ => SafeDenial(acceptedCorrelationId, taskId),
        };
    }

    private static async Task<IResult> GetProjectAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        CancellationToken cancellationToken)
    {
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;

        if (string.IsNullOrWhiteSpace(projectId) || !IsCanonicalIdentifier(projectId))
        {
            // A malformed project id is indistinguishable from a missing one at the edge.
            return SafeDenial(correlationId, null);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, null, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || authorization.ProjectDetail is null)
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, null)
                : SafeDenial(correlationId, null, authorization);
        }

        if (HasHeader(httpContext, "Idempotency-Key"))
        {
            return ValidationProblem(correlationId, null, "idempotency_key");
        }

        // A stricter caller-requested freshness class is invalid for an eventually-consistent query,
        // but only an authorized caller receives validation feedback.
        string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
        if (requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, null, "freshness");
        }

        ProjectDetailItem detail = authorization.ProjectDetail;

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;

        return Results.Json(
            new ProjectResponse(
                detail.ProjectId,
                detail.Name,
                detail.Description,
                ToWireLifecycle(detail.Lifecycle),
                detail.CreatedAt,
                detail.UpdatedAt,
                detail.SetupMetadata,
                detail.Setup,
                new ContextActivationResponse(
                    detail.Lifecycle == ProjectLifecycle.Active,
                    detail.Lifecycle == ProjectLifecycle.Active ? null : "archived"),
                ToProjectReferenceSummaries(detail),
                ToFreshness(detail.UpdatedAt, detail.Sequence)),
            ResponseJsonOptions);
    }

    private static async Task<IResult> ListProjectConversationsAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationDirectory conversationDirectory,
        CancellationToken cancellationToken)
    {
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;

        if (string.IsNullOrWhiteSpace(projectId) || !IsCanonicalIdentifier(projectId))
        {
            return SafeDenial(correlationId, null);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, null, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || authorization.ProjectDetail is null)
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, null)
                : SafeDenial(correlationId, null, authorization);
        }

        if (HasHeader(httpContext, "Idempotency-Key"))
        {
            return ValidationProblem(correlationId, null, "idempotency_key");
        }

        if (!TryReadPageRequest(httpContext, out PageRequest? page))
        {
            return ValidationProblem(correlationId, null, "page");
        }

        ProjectConversationsPage conversations = await conversationDirectory
            .ListForProjectAsync(
                new ProjectId(projectId),
                new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
                new CallerPrincipalId(tenantContext.PrincipalId!),
                page!,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(conversations, ResponseJsonOptions);
    }

    private static async Task<IResult> LinkProjectConversationAsync(
        string projectId,
        string conversationId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationAssignmentDirectory assignmentDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(conversationId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeLinkConversationAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || !IsActive(authorization.ProjectDetail))
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        LinkConversationHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<LinkConversationHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "link", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.ConversationId, conversationId, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "identity");
        }

        ProjectId? expectedCurrentProjectId = null;
        if (!string.IsNullOrWhiteSpace(body.ExpectedCurrentProjectId))
        {
            if (!IsCanonicalIdentifier(body.ExpectedCurrentProjectId)
                || !string.Equals(body.ExpectedCurrentProjectId, projectId, StringComparison.Ordinal))
            {
                return ValidationProblem(correlationId, taskId, "expectedCurrentProjectId");
            }

            expectedCurrentProjectId = new ProjectId(body.ExpectedCurrentProjectId);
        }

        ProjectConversationAssignmentResult result = await assignmentDirectory
            .LinkAsync(
                new ProjectId(projectId),
                new ConversationId(conversationId),
                new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
                new CallerPrincipalId(tenantContext.PrincipalId!),
                new ProjectConversationCommandMetadata(correlationId!, taskId!, idempotencyKey!),
                expectedCurrentProjectId,
                cancellationToken)
            .ConfigureAwait(false);

        return AssignmentResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> MoveProjectConversationAsync(
        string projectId,
        string conversationId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationAssignmentDirectory assignmentDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(conversationId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult targetAuthorization = await authorizationGate
            .AuthorizeMoveConversationAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!targetAuthorization.IsAllowed || !IsActive(targetAuthorization.ProjectDetail))
        {
            return targetAuthorization.Retryable && targetAuthorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, targetAuthorization);
        }

        MoveConversationHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<MoveConversationHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "move", StringComparison.Ordinal)
            || body.Confirmed != true
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.ConversationId, conversationId, StringComparison.Ordinal)
            || !IsCanonicalIdentifier(body.SourceProjectId)
            || string.Equals(body.SourceProjectId, projectId, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "move");
        }

        ProjectAuthorizationResult sourceAuthorization = await authorizationGate
            .AuthorizeMoveConversationAsync(body.SourceProjectId!, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!sourceAuthorization.IsAllowed || !IsActive(sourceAuthorization.ProjectDetail))
        {
            return sourceAuthorization.Retryable && sourceAuthorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, sourceAuthorization);
        }

        ProjectConversationAssignmentResult result = await assignmentDirectory
            .MoveAsync(
                new ProjectId(projectId),
                new ConversationId(conversationId),
                new ProjectId(body.SourceProjectId!),
                new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
                new CallerPrincipalId(tenantContext.PrincipalId!),
                new ProjectConversationCommandMetadata(correlationId!, taskId!, idempotencyKey!),
                cancellationToken)
            .ConfigureAwait(false);

        return AssignmentResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> UnlinkProjectConversationAsync(
        string projectId,
        string conversationId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationAssignmentDirectory assignmentDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(conversationId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeUnlinkConversationAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || !IsActive(authorization.ProjectDetail))
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        UnlinkConversationHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<UnlinkConversationHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "unlink", StringComparison.Ordinal)
            || !string.Equals(body.UnlinkIntent, "clear", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.ConversationId, conversationId, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "identity");
        }

        ProjectConversationAssignmentResult result = await assignmentDirectory
            .UnlinkAsync(
                new ProjectId(projectId),
                new ConversationId(conversationId),
                new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
                new CallerPrincipalId(tenantContext.PrincipalId!),
                new ProjectConversationCommandMetadata(correlationId!, taskId!, idempotencyKey!),
                cancellationToken)
            .ConfigureAwait(false);

        return AssignmentResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> SetProjectFolderAsync(
        string projectId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectFolderDirectory folderDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (string.IsNullOrWhiteSpace(projectId) || !IsCanonicalIdentifier(projectId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeSetProjectFolderAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || !IsActive(authorization.ProjectDetail))
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        SetProjectFolderHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<SetProjectFolderHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "set", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !IsCanonicalIdentifier(body.FolderId)
            || body.FolderMetadata is null)
        {
            return ValidationProblem(correlationId, taskId, "identity");
        }

        ProjectFolderReference? existingFolder = authorization.ProjectDetail?.ProjectFolder;
        if (existingFolder?.FolderId is { Length: > 0 } currentFolderId
            && !string.Equals(currentFolderId, body.FolderId, StringComparison.Ordinal)
            && body.ReplacementConfirmed != true)
        {
            return ValidationProblem(correlationId, taskId, "replacementConfirmed");
        }

        SetProjectFolder command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            body.FolderId!,
            body.FolderMetadata,
            body.ReplacementConfirmed == true,
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey!);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, validation.RejectedField ?? "command");
        }

        ProjectFolderValidationResult folderValidation = await folderDirectory
            .ValidateSetProjectFolderAsync(new ProjectId(projectId), body.FolderId!, correlationId!, cancellationToken)
            .ConfigureAwait(false);

        IResult? folderProblem = FolderValidationProblem(folderValidation, correlationId, taskId);
        if (folderProblem is not null)
        {
            return folderProblem;
        }

        ProjectCommandSubmissionResult result = await submitter
            .SubmitSetProjectFolderAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> LinkFileReferenceAsync(
        string projectId,
        string fileReferenceId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectFileReferenceDirectory fileReferenceDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(fileReferenceId))
        {
            return SafeDenial(correlationId, taskId);
        }

        // Gate the Project mutation intent BEFORE any Folders ACL call — unauthorized, hidden, archived,
        // stale, or unavailable Project evidence must never touch Folders.
        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeLinkFileReferenceAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || !IsActive(authorization.ProjectDetail))
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        LinkFileReferenceHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<LinkFileReferenceHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "link", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.FileReferenceId, fileReferenceId, StringComparison.Ordinal)
            || !IsCanonicalIdentifier(body.FolderId)
            || !IsCanonicalIdentifier(body.WorkspaceId)
            || !IsWorkspaceRelativePath(body.FilePath)
            || body.FileMetadata is null)
        {
            return ValidationProblem(correlationId, taskId, "identity");
        }

        LinkFileReference command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            fileReferenceId,
            body.FolderId!,
            body.FileMetadata,
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey!);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, validation.RejectedField ?? "command");
        }

        // Folders-owned authorization/freshness/redaction is the authority for whether the file is
        // currently usable; the client-supplied metadata is comparison/display intent only.
        ProjectFileReferenceValidationResult fileValidation = await fileReferenceDirectory
            .ValidateLinkFileReferenceAsync(
                new ProjectId(projectId),
                body.FolderId!,
                body.WorkspaceId!,
                body.FilePath!,
                correlationId!,
                taskId!,
                cancellationToken)
            .ConfigureAwait(false);

        IResult? fileProblem = FileReferenceValidationProblem(fileValidation, correlationId, taskId);
        if (fileProblem is not null)
        {
            return fileProblem;
        }

        ProjectCommandSubmissionResult result = await submitter
            .SubmitLinkFileReferenceAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> UnlinkFileReferenceAsync(
        string projectId,
        string fileReferenceId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(fileReferenceId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeUnlinkFileReferenceAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || !IsActive(authorization.ProjectDetail))
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        UnlinkFileReferenceHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<UnlinkFileReferenceHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "unlink", StringComparison.Ordinal)
            || !string.Equals(body.UnlinkIntent, "removeReference", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.FileReferenceId, fileReferenceId, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "identity");
        }

        // Unlink removes only the Projects-to-file association. It deliberately makes NO Folders call:
        // the underlying file is never read, deleted, archived, or otherwise touched.
        UnlinkFileReference command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            fileReferenceId,
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey!);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, validation.RejectedField ?? "command");
        }

        ProjectCommandSubmissionResult result = await submitter
            .SubmitUnlinkFileReferenceAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> LinkMemoryAsync(
        string projectId,
        string memoryReferenceId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectMemoryDirectory memoryDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(memoryReferenceId))
        {
            return SafeDenial(correlationId, taskId);
        }

        // Gate the Project mutation intent BEFORE any Memories ACL call — unauthorized, hidden,
        // archived, stale, or unavailable Project evidence must never touch Memories.
        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeLinkMemoryAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || !IsActive(authorization.ProjectDetail))
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        LinkMemoryHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<LinkMemoryHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "link", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.MemoryReferenceId, memoryReferenceId, StringComparison.Ordinal)
            || body.MemoryMetadata is null)
        {
            return ValidationProblem(correlationId, taskId, "identity");
        }

        LinkMemory command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            memoryReferenceId,
            body.MemoryMetadata,
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey!);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, validation.RejectedField ?? "command");
        }

        // Memories-owned authorization/freshness is the authority for whether the case is currently
        // usable; the client-supplied display metadata is comparison/display intent only.
        ProjectMemoryValidationResult memoryValidation = await memoryDirectory
            .ValidateLinkMemoryReferenceAsync(
                new ProjectId(projectId),
                memoryReferenceId,
                tenantContext.AuthoritativeTenantId!,
                correlationId!,
                taskId!,
                cancellationToken)
            .ConfigureAwait(false);

        IResult? memoryProblem = MemoryReferenceValidationProblem(memoryValidation, correlationId, taskId);
        if (memoryProblem is not null)
        {
            return memoryProblem;
        }

        ProjectCommandSubmissionResult result = await submitter
            .SubmitLinkMemoryAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> UnlinkMemoryAsync(
        string projectId,
        string memoryReferenceId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(memoryReferenceId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeUnlinkMemoryAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || !IsActive(authorization.ProjectDetail))
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        UnlinkMemoryHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<UnlinkMemoryHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "unlink", StringComparison.Ordinal)
            || !string.Equals(body.UnlinkIntent, "removeReference", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.MemoryReferenceId, memoryReferenceId, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "identity");
        }

        // Unlink removes only the Projects-to-memory association. It deliberately makes NO Memories
        // call: the underlying Case and MemoryUnits are never read, deleted, archived, or otherwise
        // touched.
        UnlinkMemory command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            memoryReferenceId,
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey!);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, validation.RejectedField ?? "command");
        }

        ProjectCommandSubmissionResult result = await submitter
            .SubmitUnlinkMemoryAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> UpdateProjectSetupAsync(
        string projectId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? idempotencyKey = ReadHeader(httpContext, "Idempotency-Key");
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");

        if (string.IsNullOrWhiteSpace(idempotencyKey) || !IsCanonicalIdentifier(idempotencyKey))
        {
            return ValidationProblem(correlationId, taskId, "idempotency_key");
        }

        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : idempotencyKey;
        taskId = IsCanonicalIdentifier(taskId) ? taskId : correlationId;

        if (string.IsNullOrWhiteSpace(projectId) || !IsCanonicalIdentifier(projectId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeUpdateSetupAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        UpdateProjectSetupHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<UpdateProjectSetupHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal) || body.ProjectSetup is null)
        {
            return ValidationProblem(correlationId, taskId, body?.ProjectSetup is null ? "projectSetup" : "requestSchemaVersion");
        }

        UpdateProjectSetup command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            body.ProjectSetup,
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, validation.RejectedField ?? "command");
        }

        ProjectCommandSubmissionResult result = await submitter
            .SubmitUpdateProjectSetupAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> ArchiveProjectAsync(
        string projectId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? idempotencyKey = ReadHeader(httpContext, "Idempotency-Key");
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");

        if (string.IsNullOrWhiteSpace(idempotencyKey) || !IsCanonicalIdentifier(idempotencyKey))
        {
            return ValidationProblem(correlationId, taskId, "idempotency_key");
        }

        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : idempotencyKey;
        taskId = IsCanonicalIdentifier(taskId) ? taskId : correlationId;

        if (string.IsNullOrWhiteSpace(projectId) || !IsCanonicalIdentifier(projectId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeArchiveAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        ArchiveProjectHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<ArchiveProjectHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "requestSchemaVersion");
        }

        if (!string.Equals(body.ArchiveIntent, "archive", StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "archiveIntent");
        }

        ArchiveProject command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, validation.RejectedField ?? "command");
        }

        ProjectCommandSubmissionResult result = await submitter
            .SubmitArchiveProjectAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static async Task<IResult> ListProjectsAsync(
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectListReadModel listReadModel,
        CancellationToken cancellationToken)
    {
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeListAsync(tenantContext, httpContext, correlationId, null, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return SafeDenial(correlationId, null, authorization);
        }

        if (HasHeader(httpContext, "Idempotency-Key"))
        {
            return ValidationProblem(correlationId, null, "idempotency_key");
        }

        string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
        if (requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, null, "freshness");
        }

        if (!TryReadLifecycleFilter(httpContext, out ProjectLifecycle? lifecycleFilter))
        {
            return ValidationProblem(correlationId, null, "lifecycle");
        }

        string authoritativeTenantId = tenantContext.AuthoritativeTenantId!;
        IReadOnlyList<ProjectListItem> projectedRows;
        try
        {
            projectedRows = await listReadModel
                .ListAsync(authoritativeTenantId, lifecycleFilter, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReadModelUnavailable(correlationId, null);
        }
        IReadOnlyList<ProjectListItem> rows = ProjectQueryTenantFilter.FilterList(authoritativeTenantId, projectedRows);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;

        return Results.Json(
            new ProjectListResponse(
                rows.Select(row => new ProjectListItemResponse(
                    row.ProjectId,
                    row.Name,
                    ToWireLifecycle(row.Lifecycle),
                    row.CreatedAt,
                    row.UpdatedAt,
                    ToFreshness(row.UpdatedAt, row.Sequence))).ToArray(),
                ToListFreshness(rows)),
            ResponseJsonOptions);
    }

    private static IResult Accepted(HttpContext httpContext, TimeProvider timeProvider, string correlationId, string taskId, bool idempotentReplay)
    {
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        httpContext.Response.Headers["X-Hexalith-Task-Id"] = taskId;

        return Results.Json(
            new AcceptedCommandResponse(timeProvider.GetUtcNow(), correlationId, taskId, "accepted", idempotentReplay),
            ResponseJsonOptions,
            statusCode: StatusCodes.Status202Accepted);
    }

    private static IResult MutationResult(
        HttpContext httpContext,
        TimeProvider timeProvider,
        ProjectCommandSubmissionResult result,
        string correlationId,
        string taskId)
    {
        string acceptedCorrelationId = IsCanonicalIdentifier(result.CorrelationId) ? result.CorrelationId! : correlationId;
        return result.Outcome switch
        {
            ProjectCommandSubmissionOutcome.Accepted or ProjectCommandSubmissionOutcome.IdempotentReplay =>
                Accepted(httpContext, timeProvider, acceptedCorrelationId, taskId, result.Outcome == ProjectCommandSubmissionOutcome.IdempotentReplay),
            ProjectCommandSubmissionOutcome.ValidationFailed => ValidationProblem(acceptedCorrelationId, taskId, "command"),
            ProjectCommandSubmissionOutcome.IdempotencyConflict => IdempotencyConflict(acceptedCorrelationId, taskId),
            ProjectCommandSubmissionOutcome.Unavailable => ReadModelUnavailable(acceptedCorrelationId, taskId),
            _ => SafeDenial(acceptedCorrelationId, taskId),
        };
    }

    private static IResult AssignmentResult(
        HttpContext httpContext,
        TimeProvider timeProvider,
        ProjectConversationAssignmentResult result,
        string correlationId,
        string taskId)
    {
        string acceptedCorrelationId = IsCanonicalIdentifier(result.CorrelationId) ? result.CorrelationId! : correlationId;
        return result.Outcome switch
        {
            ProjectConversationAssignmentOutcome.Accepted =>
                Accepted(httpContext, timeProvider, acceptedCorrelationId, taskId, idempotentReplay: false),
            ProjectConversationAssignmentOutcome.ValidationFailed => ValidationProblem(acceptedCorrelationId, taskId, "command"),
            ProjectConversationAssignmentOutcome.Conflict => IdempotencyConflict(acceptedCorrelationId, taskId),
            ProjectConversationAssignmentOutcome.Unavailable => ReadModelUnavailable(acceptedCorrelationId, taskId),
            _ => SafeDenial(acceptedCorrelationId, taskId),
        };
    }

    private static IResult? FolderValidationProblem(ProjectFolderValidationResult result, string? correlationId, string? taskId)
        => result.Outcome switch
        {
            ProjectFolderValidationOutcome.Accepted => null,
            ProjectFolderValidationOutcome.ValidationFailed => ValidationProblem(correlationId, taskId, "folderId"),
            ProjectFolderValidationOutcome.Stale or ProjectFolderValidationOutcome.Unavailable => ReadModelUnavailable(correlationId, taskId),
            _ => SafeDenial(correlationId, taskId),
        };

    private static IResult? FileReferenceValidationProblem(ProjectFileReferenceValidationResult result, string? correlationId, string? taskId)
        => result.Outcome switch
        {
            ProjectFileReferenceValidationOutcome.Accepted => null,
            ProjectFileReferenceValidationOutcome.ValidationFailed => ValidationProblem(correlationId, taskId, "fileReference"),
            ProjectFileReferenceValidationOutcome.Stale or ProjectFileReferenceValidationOutcome.Unavailable => ReadModelUnavailable(correlationId, taskId),
            // Denied / Redacted / Archived / TenantMismatch all collapse to an externally-indistinguishable
            // safe denial so neither Folders existence nor sensitivity is disclosed through Projects.
            _ => SafeDenial(correlationId, taskId),
        };

    private static IResult? MemoryReferenceValidationProblem(ProjectMemoryValidationResult result, string? correlationId, string? taskId)
        => result.Outcome switch
        {
            ProjectMemoryValidationOutcome.Accepted => null,
            ProjectMemoryValidationOutcome.ValidationFailed => ValidationProblem(correlationId, taskId, "memoryReference"),
            ProjectMemoryValidationOutcome.Stale or ProjectMemoryValidationOutcome.Unavailable => ReadModelUnavailable(correlationId, taskId),
            // Denied / Archived / TenantMismatch all collapse to an externally-indistinguishable safe
            // denial so neither Memories existence nor case classification is disclosed through Projects.
            _ => SafeDenial(correlationId, taskId),
        };

    private static bool TryReadMutationEnvelope(
        HttpContext httpContext,
        out string? correlationId,
        out string? taskId,
        out string? idempotencyKey,
        out IResult? rejection)
    {
        idempotencyKey = ReadHeader(httpContext, "Idempotency-Key");
        correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");

        if (string.IsNullOrWhiteSpace(idempotencyKey) || !IsCanonicalIdentifier(idempotencyKey))
        {
            rejection = ValidationProblem(correlationId, taskId, "idempotency_key");
            return false;
        }

        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : idempotencyKey;
        taskId = IsCanonicalIdentifier(taskId) ? taskId : correlationId;
        rejection = null;
        return true;
    }

    private static bool IsActive(ProjectDetailItem? detail)
        => detail?.Lifecycle == ProjectLifecycle.Active;

    private static IResult SafeDenial(string? correlationId, string? taskId)
        => SafeProblem(
            StatusCodes.Status404NotFound,
            "tenant_access_denied",
            "resource_unavailable",
            retryable: false,
            correlationId,
            taskId,
            message: "The requested resource is unavailable.",
            clientAction: "no_action",
            detailsVisibility: "redacted");

    private static IResult SafeDenial(string? correlationId, string? taskId, ProjectAuthorizationResult authorization)
        => SafeDenial(correlationId, taskId);

    private static IResult ValidationProblem(string? correlationId, string? taskId, string field)
        => SafeProblem(
            StatusCodes.Status400BadRequest,
            "validation_error",
            "validation_error",
            retryable: false,
            correlationId,
            taskId,
            rejectedField: field,
            message: "The request failed validation.",
            clientAction: "revise_request",
            detailsVisibility: "metadata_only");

    private static IResult IdempotencyConflict(string? correlationId, string? taskId)
        => SafeProblem(
            StatusCodes.Status409Conflict,
            "idempotency_conflict",
            "idempotency_conflict",
            retryable: false,
            correlationId,
            taskId,
            message: "The idempotency key conflicts with a previous request.",
            clientAction: "revise_request",
            detailsVisibility: "metadata_only");

    private static IResult ReadModelUnavailable(string? correlationId, string? taskId)
        => SafeProblem(
            StatusCodes.Status503ServiceUnavailable,
            "read_model_unavailable",
            "read_model_unavailable",
            retryable: true,
            correlationId,
            taskId,
            message: "The read model is temporarily unavailable.",
            clientAction: "retry",
            detailsVisibility: "metadata_only");

    private static IResult SafeProblem(
        int statusCode,
        string category,
        string code,
        bool retryable,
        string? correlationId,
        string? taskId,
        string? rejectedField = null,
        string message = "The requested resource is unavailable.",
        string clientAction = "no_action",
        string detailsVisibility = "metadata_only")
    {
        Dictionary<string, object?> details = new()
        {
            ["visibility"] = detailsVisibility,
        };

        if (!string.IsNullOrWhiteSpace(rejectedField))
        {
            details["rejectedField"] = rejectedField;
        }

        Dictionary<string, object?> extensions = new()
        {
            ["category"] = category,
            ["code"] = code,
            ["message"] = message,
            ["correlationId"] = correlationId,
            ["retryable"] = retryable,
            ["clientAction"] = clientAction,
            ["details"] = details,
        };

        if (!string.IsNullOrWhiteSpace(taskId))
        {
            extensions["taskId"] = taskId;
        }

        return Results.Problem(
            type: $"https://hexalith.dev/errors/projects/{code}",
            title: statusCode switch
            {
                StatusCodes.Status400BadRequest => "Validation failed",
                StatusCodes.Status404NotFound => "Access unavailable",
                StatusCodes.Status409Conflict => "Idempotency conflict",
                StatusCodes.Status503ServiceUnavailable => "Read model unavailable",
                _ => "Access unavailable",
            },
            statusCode: statusCode,
            extensions: extensions);
    }

    private static string ToWireLifecycle(Contracts.Ui.ProjectLifecycle lifecycle)
        => lifecycle.ToString().ToLowerInvariant();

    private static IReadOnlyList<ProjectReferenceSummaryResponse> ToProjectReferenceSummaries(ProjectDetailItem detail)
    {
        List<ProjectReferenceSummaryResponse> summaries = [];

        if (detail.ProjectFolder is not null)
        {
            summaries.Add(new ProjectReferenceSummaryResponse(
                "folder",
                ToWireReferenceState(detail.ProjectFolder.ReferenceState),
                detail.ProjectFolder.FolderId,
                detail.ProjectFolder.DisplayName,
                detail.ProjectFolder.ReasonCode,
                ToFreshness(detail.ProjectFolder.ObservedAt, detail.Sequence)));
        }

        foreach (ProjectFileReference file in detail.FileReferences.OrderBy(reference => reference.FileReferenceId, StringComparer.Ordinal))
        {
            summaries.Add(new ProjectReferenceSummaryResponse(
                "file",
                ToWireReferenceState(file.ReferenceState),
                file.FileReferenceId,
                file.DisplayName,
                file.ReasonCode,
                ToFreshness(file.ObservedAt, detail.Sequence)));
        }

        foreach (ProjectMemoryReference memory in detail.MemoryReferences.OrderBy(reference => reference.MemoryReferenceId, StringComparer.Ordinal))
        {
            summaries.Add(new ProjectReferenceSummaryResponse(
                "memory",
                ToWireReferenceState(memory.ReferenceState),
                memory.MemoryReferenceId,
                memory.DisplayName,
                memory.ReasonCode,
                ToFreshness(memory.ObservedAt, detail.Sequence)));
        }

        return summaries;
    }

    private static string ToWireReferenceState(ReferenceState state)
        => state switch
        {
            ReferenceState.Pending => "pending",
            ReferenceState.Included => "included",
            ReferenceState.Excluded => "excluded",
            ReferenceState.Unauthorized => "unauthorized",
            ReferenceState.Unavailable => "unavailable",
            ReferenceState.Stale => "stale",
            ReferenceState.Archived => "archived",
            ReferenceState.Ambiguous => "ambiguous",
            ReferenceState.TenantMismatch => "tenantMismatch",
            ReferenceState.Conflict => "conflict",
            ReferenceState.InvalidReference => "invalidReference",
            _ => "unavailable",
        };

    private static FreshnessMetadataResponse ToFreshness(DateTimeOffset observedAt, long sequence)
        => new(
            EventuallyConsistent,
            observedAt,
            sequence > 0 ? $"watermark_{sequence:D8}" : null,
            false,
            "trusted");

    private static FreshnessMetadataResponse ToListFreshness(IReadOnlyList<ProjectListItem> rows)
    {
        if (rows.Count == 0)
        {
            return ToFreshness(DateTimeOffset.UnixEpoch, 0);
        }

        return ToFreshness(rows.Max(row => row.UpdatedAt), rows.Max(row => row.Sequence));
    }

    private static bool TryReadLifecycleFilter(HttpContext httpContext, out ProjectLifecycle? lifecycleFilter)
    {
        lifecycleFilter = null;
        if (!httpContext.Request.Query.TryGetValue("lifecycle", out StringValues values))
        {
            return true;
        }

        string[] observed = values
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(value => value.Length > 0)
            .ToArray();

        if (observed.Length == 0)
        {
            return true;
        }

        if (observed.Length > 1)
        {
            return false;
        }

        switch (observed[0])
        {
            case "all":
                lifecycleFilter = null;
                return true;
            case "active":
                lifecycleFilter = ProjectLifecycle.Active;
                return true;
            case "archived":
                lifecycleFilter = ProjectLifecycle.Archived;
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadPageRequest(HttpContext httpContext, out PageRequest? page)
    {
        page = null;
        int pageSize = 25;
        if (httpContext.Request.Query.TryGetValue("pageSize", out StringValues pageValues)
            && (!int.TryParse(pageValues.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pageSize)))
        {
            return false;
        }

        string? cursor = null;
        if (httpContext.Request.Query.TryGetValue("cursor", out StringValues cursorValues))
        {
            cursor = cursorValues.FirstOrDefault();
        }

        try
        {
            page = new PageRequest(pageSize, cursor);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool IsCanonicalIdentifier(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length <= ProjectsServerModule.MaxCanonicalIdentifierLength
            && CanonicalIdentifierRegex().IsMatch(value);

    // A bounded, workspace-root-relative path used solely to address Folders file metadata. It is NEVER
    // a local/absolute/unrestricted filesystem path and is never stored by Projects: it rejects control
    // characters, backslashes, a leading slash, empty/`..`/`//` segments, and over-long input. Folders
    // path policy remains the authority.
    private static bool IsWorkspaceRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512)
        {
            return false;
        }

        if (value[0] == '/' || value.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (c == '\r' || c == '\n' || char.IsControl(c))
            {
                return false;
            }
        }

        string[] segments = value.Split('/');
        foreach (string segment in segments)
        {
            if (segment.Length == 0 || string.Equals(segment, "..", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasHeader(HttpContext httpContext, string name)
        => httpContext.Request.Headers.ContainsKey(name);

    private static string? ReadHeader(HttpContext httpContext, string name)
    {
        if (!httpContext.Request.Headers.TryGetValue(name, out StringValues values))
        {
            return null;
        }

        foreach (string? raw in values)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            string trimmed = raw.Trim();
            if (trimmed.Length == 0 || ContainsControlChars(trimmed))
            {
                continue;
            }

            return trimmed;
        }

        return null;
    }

    private static bool ContainsControlChars(string value)
    {
        foreach (char c in value)
        {
            if (c == '\r' || c == '\n' || char.IsControl(c))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record ProjectMetadataHttpRequest(
        string? DisplayName,
        string? MetadataClass);

    private sealed record CreateProjectHttpRequest(
        string? RequestSchemaVersion,
        ProjectMetadataHttpRequest? ProjectMetadata,
        string? ProjectId,
        string? Name,
        string? Description,
        string? SetupMetadata);

    private sealed record UpdateProjectSetupHttpRequest(
        string? RequestSchemaVersion,
        ProjectSetup? ProjectSetup);

    private sealed record ArchiveProjectHttpRequest(
        string? ArchiveIntent,
        string? RequestSchemaVersion);

    private sealed record LinkConversationHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? ConversationId,
        string? ExpectedCurrentProjectId);

    private sealed record MoveConversationHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? ConversationId,
        string? SourceProjectId,
        bool? Confirmed);

    private sealed record UnlinkConversationHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? UnlinkIntent,
        string? ProjectId,
        string? ConversationId);

    private sealed record SetProjectFolderHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? FolderId,
        ProjectFolderMetadata? FolderMetadata,
        bool? ReplacementConfirmed);

    private sealed record LinkFileReferenceHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? FileReferenceId,
        string? FolderId,
        string? WorkspaceId,
        string? FilePath,
        ProjectFileReferenceMetadata? FileMetadata);

    private sealed record UnlinkFileReferenceHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? UnlinkIntent,
        string? ProjectId,
        string? FileReferenceId);

    private sealed record LinkMemoryHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? MemoryReferenceId,
        ProjectMemoryReferenceMetadata? MemoryMetadata);

    private sealed record UnlinkMemoryHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? UnlinkIntent,
        string? ProjectId,
        string? MemoryReferenceId);

    private sealed record AcceptedCommandResponse(
        DateTimeOffset AcceptedAt,
        string CorrelationId,
        string TaskId,
        string Status,
        bool IdempotentReplay);

    private sealed record ProjectResponse(
        string ProjectId,
        string Name,
        string? Description,
        string LifecycleState,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        string? SetupMetadata,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        ProjectSetup? ProjectSetup,
        ContextActivationResponse ContextActivation,
        IReadOnlyList<ProjectReferenceSummaryResponse> References,
        FreshnessMetadataResponse Freshness);

    private sealed record ProjectListResponse(
        IReadOnlyList<ProjectListItemResponse> Items,
        FreshnessMetadataResponse Freshness);

    private sealed record ProjectListItemResponse(
        string ProjectId,
        string Name,
        string LifecycleState,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        FreshnessMetadataResponse Freshness);

    private sealed record ContextActivationResponse(
        bool Enabled,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        string? BlockedReasonCode);

    private sealed record ProjectReferenceSummaryResponse(
        string ReferenceKind,
        string ReferenceState,
        string? ReferenceId,
        string? DisplayName,
        string? ReasonCode,
        FreshnessMetadataResponse Freshness);

    private sealed record FreshnessMetadataResponse(
        string ReadConsistency,
        DateTimeOffset ObservedAt,
        string? ProjectionWatermark,
        bool Stale,
        string TrustState);
}
