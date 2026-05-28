// <copyright file="GetProjectContextExplanationEndpoint.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Threading;
using System.Threading.Tasks;

using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server.Conversations;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Story 3.3 partial — adds the read-only <c>GET /api/v1/projects/{projectId}/context/explain</c>
/// endpoint that wraps the Story 3.2 assembly output (<see cref="ProjectContextInclusionPolicy"/>
/// from Story 3.1) into a <see cref="ProjectContextExplanation"/> surfacing both the assembled
/// <see cref="ProjectContext"/> and the per-candidate evaluation trace
/// (<see cref="ProjectContextAssemblyResult.Evaluations"/>) so operators can diagnose why each
/// candidate was included or excluded — realizes FR-17 / UJ-4 / AR-9.
/// </summary>
/// <remarks>
/// <para>
/// The handler is a line-for-line port of Story 3.2's <c>GetProjectContextAsync</c> with two changes:
/// (i) the assembly context's <c>OperationKind</c> is <see cref="ProjectContextOperationKind.Explain"/>
/// (was <see cref="ProjectContextOperationKind.Get"/>); (ii) the wire body wraps both
/// <c>assembled.Context</c> and <c>assembled.Evaluations</c> into a
/// <see cref="ProjectContextExplanation"/> (was the bare <c>assembled.Context</c>). No policy
/// include/exclude / fail-closed collapse / freshness mapping decision is duplicated in this
/// handler. The conversation page cap, the safe-denial 404 contract, the
/// <c>Idempotency-Key</c> rejection on the query, and the <c>X-Hexalith-Freshness</c> strict
/// validation are identical to Story 3.2.
/// </para>
/// <para>
/// Outer collapses (<c>Unauthorized</c> / <c>ProjectUnavailable</c>) surface as safe-denial 404
/// Problem Details (no wrapper body). The empty-<see cref="ProjectContextExplanation.Evaluations"/>
/// contract on outer collapse is observable via the Tier-1 trace tests, not the HTTP boundary.
/// </para>
/// </remarks>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> GetProjectContextExplanationAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationDirectory conversationDirectory,
        ProjectContextInclusionPolicy contextPolicy,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;
        string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");
        taskId = IsCanonicalIdentifier(taskId) ? taskId : null;

        if (string.IsNullOrWhiteSpace(projectId) || !IsCanonicalIdentifier(projectId))
        {
            // Malformed and missing project ids are indistinguishable at the safe-denial edge.
            return SafeDenial(correlationId, null);
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeReadAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
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

        string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
        if (requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, null, "freshness");
        }

        if (authorization.TenantAccessResult is not { } tenantAccessResult)
        {
            // Defensive: AuthorizeReadAsync should populate the result on every Allowed path. A
            // missing result is an upstream regression — collapse to safe-denial.
            return SafeDenial(correlationId, null);
        }

        ProjectDetailItem detail = authorization.ProjectDetail;
        DateTimeOffset now = timeProvider.GetUtcNow();

        ProjectConversationsPage conversations = await conversationDirectory
            .ListForProjectAsync(
                new ProjectId(projectId),
                new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
                new CallerPrincipalId(tenantContext.PrincipalId!),
                new PageRequest(ProjectContextConversationsPageSize, ContinuationCursor: null),
                cancellationToken)
            .ConfigureAwait(false);

        System.Collections.Generic.IReadOnlyList<ProjectContextConversationEvidence> conversationEvidence =
            ProjectContextConversationEvidenceMapper.Map(conversations, now);

        ProjectContextAssemblyResult assembled = contextPolicy.Assemble(
            new ProjectContextAssemblyContext(
                AuthoritativeTenantId: tenantContext.AuthoritativeTenantId,
                RequestedTenantId: tenantContext.AuthoritativeTenantId,
                ProjectId: projectId,
                OperationKind: ProjectContextOperationKind.Explain,
                CorrelationId: correlationId,
                TaskId: taskId,
                Now: now),
            new ProjectContextProjectEvidence(detail),
            new ProjectContextTenantAccess(tenantAccessResult),
            new ProjectContextReferenceEvidence(
                ProjectFolder: detail.ProjectFolder,
                FileReferences: detail.FileReferences,
                MemoryReferences: detail.MemoryReferences,
                Conversations: conversationEvidence));

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(
            new ProjectContextExplanation(assembled.Context, assembled.Evaluations),
            ResponseJsonOptions);
    }
}
