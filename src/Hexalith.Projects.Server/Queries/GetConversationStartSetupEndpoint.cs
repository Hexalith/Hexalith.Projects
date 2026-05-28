// <copyright file="GetConversationStartSetupEndpoint.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ConversationStartSetup;
using Hexalith.Projects.Projections.ProjectDetail;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Story 3.5 partial — adds the read-only
/// <c>GET /api/v1/projects/{projectId}/setup/conversation-start</c> endpoint that returns the
/// bounded subset of Project Setup Hexalith.Chatbot retrieves to start or resume a conversation
/// without re-querying every bounded context first (FR-20). Realizes AR-8's named
/// <c>ConversationStartSetupProjection</c> as a pure projector over the policy-assembled
/// <see cref="ProjectContext"/> (Story 3.1).
/// </summary>
/// <remarks>
/// <para>
/// The handler is a port of Story 3.2's <c>GetProjectContextAsync</c> with these subtractions:
/// no <c>IProjectConversationDirectory</c> / <c>IProjectFolderDirectory</c> /
/// <c>IProjectFileReferenceDirectory</c> / <c>IProjectMemoryDirectory</c> dependency; no
/// conversation page fetch; no <c>ProjectContextConversationEvidenceMapper</c> invocation; no
/// <c>using ConversationTenantId = ...</c> alias. The single change: the policy is invoked with
/// <see cref="ProjectContextOperationKind.GetConversationStartSetup"/> and an EMPTY
/// <see cref="ProjectContextReferenceEvidence"/> so it exercises ONLY its outer collapses
/// (tenant-authority / project-visibility / project-lifecycle / freshness); the response body is
/// the bounded <see cref="ConversationStartSetup"/> projected from <c>assembled.Context</c> via
/// <see cref="ConversationStartSetupProjector.Project"/>.
/// </para>
/// <para>
/// FR-20 fast-path guarantees by construction:
/// (i) NO sibling ACL recheck on ANY request path — the handler signature has no sibling
/// directory dependency, so a future regression that adds one will be caught structurally;
/// (ii) NO conversation page fetch — the handler does not declare
/// <c>IProjectConversationDirectory</c>;
/// (iii) NO per-reference inventory on the response body — the projector reads only
/// <c>ProjectContext.Setup</c> / <c>Lifecycle</c> / <c>ObservedAt</c> / <c>Freshness</c>.
/// </para>
/// <para>
/// Carry-forward invariants from Story 3.2 / 3.3 / 3.4: safe-denial 404 collapse for
/// <see cref="ProjectContextAssemblyOutcome.Unauthorized"/> /
/// <see cref="ProjectContextAssemblyOutcome.ProjectUnavailable"/>; <c>Idempotency-Key</c> rejected
/// on queries after authorization; strict <c>X-Hexalith-Freshness</c> validation; defensive
/// null-collapse on missing <see cref="ProjectAuthorizationResult.TenantAccessResult"/>. The
/// handler never duplicates any policy include/exclude / fail-closed-collapse / freshness-mapping
/// decision.
/// </para>
/// </remarks>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> GetConversationStartSetupAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
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

        ProjectContextAssemblyResult assembled = contextPolicy.Assemble(
            new ProjectContextAssemblyContext(
                AuthoritativeTenantId: tenantContext.AuthoritativeTenantId,
                RequestedTenantId: tenantContext.AuthoritativeTenantId,
                ProjectId: projectId,
                OperationKind: ProjectContextOperationKind.GetConversationStartSetup,
                CorrelationId: correlationId,
                TaskId: taskId,
                Now: now),
            new ProjectContextProjectEvidence(detail),
            new ProjectContextTenantAccess(tenantAccessResult),
            new ProjectContextReferenceEvidence(
                ProjectFolder: null,
                FileReferences: Array.Empty<ProjectFileReference>(),
                MemoryReferences: Array.Empty<ProjectMemoryReference>(),
                Conversations: Array.Empty<ProjectContextConversationEvidence>()));

        if (assembled.Context.AssemblyOutcome is ProjectContextAssemblyOutcome.Unauthorized
            or ProjectContextAssemblyOutcome.ProjectUnavailable)
        {
            return SafeDenial(correlationId, null);
        }

        ConversationStartSetup body = ConversationStartSetupProjector.Project(assembled.Context);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(body, ResponseJsonOptions);
    }
}
