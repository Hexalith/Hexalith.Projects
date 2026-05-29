// <copyright file="ResolveProjectFromConversationEndpoint.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Conversations;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Story 4.2 partial — adds the read-only <c>GET /api/v1/projects/resolution/from-conversation</c>
/// query endpoint (FR-12 / UJ-3). It is the impure host adapter around the Story 4.1 pure
/// <see cref="ProjectResolutionEngine"/>: authorize at the API edge (claims-only tenant, fail-closed),
/// read the single conversation's safe metadata through the Pattern-A ACL, enumerate the tenant's
/// authorized Projects, map the evidence to the engine's input shape, and return the existing
/// <see cref="ProjectResolution"/> wire model unchanged.
/// </summary>
/// <remarks>
/// <para>
/// This is a <b>synchronous read query</b> (AR-10 compute-on-demand): it writes no event, projection,
/// state, or resolution trace, and returns no <c>202</c>. <c>Idempotency-Key</c> is rejected (queries
/// never accept it); the requested freshness, if present, must be <c>eventually_consistent</c>; and the
/// response always sets <c>X-Hexalith-Freshness: eventually_consistent</c>. Scoring, ranking, the
/// qualifying threshold, and the outcome are decided solely by the engine — this handler never
/// re-implements that heuristic.
/// </para>
/// <para>
/// Fail-closed contract at the <i>caller</i> boundary (mirrors the Story 3.2 read endpoints): a
/// missing/malformed conversation id, an unauthorized caller, or unverifiable tenant authority all
/// collapse to an RFC 9457 safe-denial <c>404</c> (unauthorized and nonexistent are externally
/// indistinguishable); read-model unavailability surfaces as a retryable <c>503</c>. A well-formed
/// conversation that the ACL cannot read in scope (cross-tenant, unknown, or degraded trust) is
/// deliberately <b>not</b> turned into a <c>404</c> here: it fails closed to degraded reference
/// evidence that the engine surfaces as an exclusion, yielding <c>NoMatch</c> (HTTP 200). Returning
/// the same <c>NoMatch</c> for an unreadable conversation and for a legitimately project-less one
/// leaks no cross-tenant existence (NFR-2) and avoids the in-tenant-vs-cross-tenant oracle a
/// conversation-scoped 404 would create.
/// </para>
/// </remarks>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> ResolveProjectFromConversationAsync(
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationResolutionDirectory conversationResolutionDirectory,
        IProjectListReadModel listReadModel,
        ProjectResolutionEngine resolutionEngine,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;
        string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");
        taskId = IsCanonicalIdentifier(taskId) ? taskId : null;

        string? conversationId = ReadQuery(httpContext, "conversationId");
        if (string.IsNullOrWhiteSpace(conversationId) || !IsCanonicalIdentifier(conversationId))
        {
            // A missing or malformed conversation id is indistinguishable from an unauthorized one.
            return SafeDenial(correlationId, taskId);
        }

        // Tenant-level read authorization (resolution is not project-scoped). Claims-only tenant
        // authority; fail closed before any sibling ACL or read-model access.
        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeListAsync(tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        if (HasHeader(httpContext, "Idempotency-Key"))
        {
            return ValidationProblem(correlationId, taskId, "idempotency_key");
        }

        string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
        if (requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "freshness");
        }

        if (!TryReadIncludeArchived(httpContext, out bool includeArchived))
        {
            return ValidationProblem(correlationId, taskId, "includeArchived");
        }

        string authoritativeTenantId = tenantContext.AuthoritativeTenantId!;
        string principalId = tenantContext.PrincipalId!;
        string safeCorrelationId = correlationId ?? string.Empty;

        // Read the single conversation's safe metadata through the Pattern-A ACL (fail-closed).
        ConversationResolutionMetadata conversationMetadata = await conversationResolutionDirectory
            .ReadConversationMetadataAsync(
                new ConversationId(conversationId),
                new ConversationTenantId(authoritativeTenantId),
                new CallerPrincipalId(principalId),
                safeCorrelationId,
                cancellationToken)
            .ConfigureAwait(false);

        // Enumerate the tenant's authorized Projects (all lifecycles; the engine applies the
        // archived-exclusion rule). A single read-model query — no request-time multi-ACL fan-out.
        IReadOnlyList<ProjectListItem> projectedRows;
        try
        {
            projectedRows = await listReadModel
                .ListAsync(authoritativeTenantId, lifecycleFilter: null, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReadModelUnavailable(correlationId, taskId);
        }

        IReadOnlyList<ProjectListItem> rows = ProjectQueryTenantFilter.FilterList(authoritativeTenantId, projectedRows);
        List<ConversationResolutionProjectCandidate> candidateProjects = new(rows.Count);
        foreach (ProjectListItem row in rows)
        {
            candidateProjects.Add(new ConversationResolutionProjectCandidate(row.ProjectId, row.Name, row.Lifecycle));
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        IReadOnlyList<ProjectResolutionCandidateEvidence> candidates = ConversationResolutionEvidenceMapper.Map(
            conversationMetadata,
            candidateProjects,
            now);

        ProjectResolutionContext context = new(
            AuthoritativeTenantId: authoritativeTenantId,
            RequestedTenantId: authoritativeTenantId,
            IncludeArchived: includeArchived,
            Now: now,
            CorrelationId: correlationId,
            TaskId: taskId,
            PresentedInputIds: [conversationId]);

        ProjectResolution resolution = resolutionEngine.Resolve(context, candidates);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(resolution, ResponseJsonOptions);
    }

    private static bool TryReadIncludeArchived(HttpContext httpContext, out bool includeArchived)
    {
        includeArchived = false;
        if (!httpContext.Request.Query.TryGetValue("includeArchived", out StringValues values))
        {
            return true;
        }

        string? raw = values.Count == 0 ? null : values[values.Count - 1];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!bool.TryParse(raw.Trim(), out bool parsed))
        {
            return false;
        }

        includeArchived = parsed;
        return true;
    }

    private static string? ReadQuery(HttpContext httpContext, string name)
    {
        if (!httpContext.Request.Query.TryGetValue(name, out StringValues values) || values.Count == 0)
        {
            return null;
        }

        string? raw = values[values.Count - 1];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string trimmed = raw.Trim();
        return ContainsControlChars(trimmed) ? null : trimmed;
    }
}
