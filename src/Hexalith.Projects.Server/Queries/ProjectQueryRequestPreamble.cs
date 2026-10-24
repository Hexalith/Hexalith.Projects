// <copyright file="ProjectQueryRequestPreamble.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using ByteAether.Ulid;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;

using Microsoft.AspNetCore.Http;

/// <summary>Shared request handling for the retained legacy read operations.</summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private static string BeginQueryRequest(HttpContext httpContext, out string? taskId)
    {
        string? presentedCorrelationId = ReadHeader(httpContext, "X-Correlation-Id");
        string correlationId = IsCanonicalIdentifier(presentedCorrelationId)
            ? presentedCorrelationId!
            : Ulid.New().ToString();
        taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");
        taskId = IsCanonicalIdentifier(taskId) ? taskId : null;

        // Set these before route validation and authorization so every handler response carries
        // the same safe tracing and cache policy, including validation, denial and outage paths.
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        httpContext.Response.Headers.CacheControl = "no-store";
        string[] vary = httpContext.Response.Headers.GetCommaSeparatedValues("Vary");
        if (!vary.Contains("*", StringComparer.Ordinal))
        {
            string[] missing = new[] { "Authorization", "Cookie" }
                .Where(header => !vary.Contains(header, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (missing.Length > 0)
            {
                httpContext.Response.Headers.AppendCommaSeparatedValues("Vary", missing);
            }
        }
        return correlationId;
    }

    private static IResult? ValidateQueryRequest(HttpContext httpContext, string correlationId, string? taskId)
    {
        // Call only after authorization: validation details must not distinguish denied callers.
        if (HasHeader(httpContext, "Idempotency-Key") || httpContext.Request.Query.ContainsKey("Idempotency-Key"))
        {
            return ValidationProblem(correlationId, taskId, "idempotency_key");
        }

        string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
        return requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal)
            ? ValidationProblem(correlationId, taskId, "freshness")
            : null;
    }

    private static async Task<(ProjectDetailItem? Detail, TenantAccessAuthorizationResult? TenantAccess, IResult? Rejection)> AuthorizeContextQueryAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        string correlationId,
        string? taskId,
        CancellationToken cancellationToken)
    {
        if (!IsCanonicalIdentifier(projectId))
        {
            return (null, null, SafeDenial(correlationId, null));
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || authorization.ProjectDetail is null)
        {
            IResult rejection = authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, null)
                : SafeDenial(correlationId, null, authorization);
            return (null, null, rejection);
        }

        if (ValidateQueryRequest(httpContext, correlationId, null) is { } validationProblem)
        {
            return (null, null, validationProblem);
        }

        // An allowed result without its tenant evidence is an upstream regression. Retain the
        // existing safe-denial collapse instead of assembling a response from incomplete authority.
        return authorization.TenantAccessResult is { } tenantAccess
            ? (authorization.ProjectDetail, tenantAccess, null)
            : (null, null, SafeDenial(correlationId, null));
    }
}
