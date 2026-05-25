// <copyright file="ProjectsDomainServiceEndpoints.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Projections.ProjectDetail;

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

        if (body is null || string.IsNullOrWhiteSpace(body.Name))
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
            body.Name,
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
            return SafeDenial(correlationId, null, authorization);
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
                new FreshnessMetadataResponse(EventuallyConsistent, detail.UpdatedAt, null, false)),
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

    private static bool IsCanonicalIdentifier(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length <= ProjectsServerModule.MaxCanonicalIdentifierLength
            && CanonicalIdentifierRegex().IsMatch(value);

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

    private sealed record CreateProjectHttpRequest(
        string? ProjectId,
        string? Name,
        string? Description,
        string? SetupMetadata);

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
        FreshnessMetadataResponse Freshness);

    private sealed record FreshnessMetadataResponse(
        string ReadConsistency,
        DateTimeOffset ObservedAt,
        string? ProjectionWatermark,
        bool Stale);
}
