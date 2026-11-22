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
using Microsoft.Extensions.DependencyInjection;

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

        if (httpContext.Request.Query.ContainsKey("cursor")
            && !httpContext.Request.Path.Value!.TrimEnd('/').EndsWith("/conversations", StringComparison.OrdinalIgnoreCase))
            return ValidationProblem(correlationId, taskId, "cursor");

        string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
        return requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal)
            ? ValidationProblem(correlationId, taskId, "freshness")
            : null;
    }

    private static async Task<(ProjectDetailItem? Detail, TenantAccessAuthorizationResult? TenantAccess, IResult? Rejection, LegacyContextAuthority? Authority)> AuthorizeContextQueryAsync(
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
            return (null, null, SafeDenial(correlationId, null), null);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed || authorization.ProjectDetail is null)
        {
            IResult rejection = authorization.Retryable
                ? ReadModelUnavailable(correlationId, null)
                : SafeDenial(correlationId, null, authorization);
            return (null, null, rejection, null);
        }

        if (authorization.TenantAccessResult is null)
        {
            return (null, null, SafeDenial(correlationId, null), null);
        }

        if (!HasCurrentLegacyReadAuthority(authorization))
        {
            return (null, null, ReadModelUnavailable(correlationId, null), null);
        }

        if (ValidateQueryRequest(httpContext, correlationId, null) is { } validationProblem)
        {
            return (null, null, validationProblem, null);
        }

        if (authorization.ProjectDetail.Lifecycle == Hexalith.Projects.Contracts.Ui.ProjectLifecycle.Active
            && (authorization.ProjectDetail.ProjectFolder is null
                || authorization.ProjectDetail.ProjectFolder.ReferenceState != Hexalith.Projects.Contracts.Ui.ReferenceState.Included))
            return (null, null, ReadModelUnavailable(correlationId, null), null);

        if (!await HasCurrentLegacyMembershipAsync(projectId, httpContext, tenantContext,
            correlationId, cancellationToken).ConfigureAwait(false))
            return (null, null, ReadModelUnavailable(correlationId, null), null);

        // An allowed result without its tenant evidence is an upstream regression. Retain the
        // existing safe-denial collapse instead of assembling a response from incomplete authority.
        return authorization.TenantAccessResult is { } tenantAccess
            ? (authorization.ProjectDetail, tenantAccess, null, new LegacyContextAuthority(authorization,
                System.Text.Json.JsonSerializer.Serialize(authorization.ProjectDetail, ResponseJsonOptions),
                tenantContext.AuthoritativeTenantId!, tenantContext.PrincipalId!))
            : (null, null, SafeDenial(correlationId, null), null);
    }

    // Retained context reads disclose only after re-reading Project, Tenant and EventStore authority
    // once their last owner await has completed, mirroring the supported executor and legacy open.
    private static async Task<IResult?> ConfirmLegacyContextDisclosureAsync(
        string projectId,
        LegacyContextAuthority authority,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        string correlationId,
        string? taskId,
        CancellationToken cancellationToken)
    {
        ProjectAuthorizationResult disclosure = await authorizationGate
            .AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (disclosure.Retryable || disclosure.IsAllowed && !HasCurrentLegacyReadAuthority(disclosure))
            return ReadModelUnavailable(correlationId, null);
        return HasStableLegacyReadEvidence(authority.Initial, disclosure, authority.ObservedDetail,
            authority.TenantId, authority.ActorId, tenantContext) ? null : SafeDenial(correlationId, null);
    }

    private sealed record LegacyContextAuthority(ProjectAuthorizationResult Initial, string ObservedDetail, string TenantId, string ActorId);

    private static async Task<bool> HasCurrentLegacyMembershipAsync(string projectId, HttpContext context,
        IProjectTenantContextAccessor tenant, string correlationId, CancellationToken cancellationToken)
    {
        string? conversationId = context.Request.Query["conversationId"].Count == 1 ? context.Request.Query["conversationId"][0] : null;
        var membership = context.RequestServices.GetService<Hexalith.Projects.Server.Conversations.IProjectConversationMembershipDirectory>();
        return !string.IsNullOrWhiteSpace(conversationId) && membership is not null
            && await membership.IsCurrentMemberAsync(tenant.AuthoritativeTenantId!, projectId, conversationId,
                tenant.PrincipalId!, correlationId, cancellationToken).ConfigureAwait(false);
    }
}
