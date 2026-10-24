// <copyright file="GetProjectContextEndpoint.cs" company="Hexalith">
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
/// Story 3.2 partial — adds the read-only <c>GET /api/v1/projects/{projectId}/context</c> endpoint
/// that composes the Story 3.1 inputs (<see cref="ProjectContextInclusionPolicy"/>) from the
/// projection-stored <see cref="ProjectDetailItem"/>, the layered tenant-access result threaded
/// through <see cref="ProjectAuthorizationResult.TenantAccessResult"/>, and the Story 2.1
/// conversation page, then returns the assembled metadata-only <see cref="ProjectContext"/> as-is.
/// </summary>
/// <remarks>
/// <para>
/// The handler is a thin orchestrator: it never duplicates any policy include/exclude / fail-closed
/// collapse / freshness mapping decision. Conversation evidence is fetched via the Story 2.1 read
/// ACL with <see cref="PageSize"/>=100 and no continuation — a single first-page snapshot is FR-16
/// v1's scope. Folder / file / memory references are taken AS-IS from the projection. No on-the-fly
/// Folders / Memories ACL recheck happens at Get time (Story 3.4 Refresh territory).
/// </para>
/// <para>
/// Carry-forward of the Story 1.4 safe-denial 404 contract is strict: Unauthorized and
/// ProjectUnavailable both collapse to HTTP 404 at the boundary (existence-non-inference). The
/// internal <c>AssemblyOutcome</c> is observability-only, surfaced inside the returned
/// <see cref="ProjectContext"/> body for assembled responses; non-assembled requests get a safe
/// Problem Details body with no leakage.
/// </para>
/// </remarks>
public static partial class ProjectsDomainServiceEndpoints
{
    /// <summary>The single first-page snapshot cap for conversation evidence (FR-16 v1).</summary>
    private const int ProjectContextConversationsPageSize = 100;

    private static async Task<IResult> GetProjectContextAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationDirectory conversationDirectory,
        ProjectContextInclusionPolicy contextPolicy,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string correlationId = BeginQueryRequest(httpContext, out string? taskId);

        var prepared = await AuthorizeContextQueryAsync(
            projectId, httpContext, tenantContext, authorizationGate, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (prepared.Rejection is { } rejection)
        {
            return rejection;
        }

        ProjectDetailItem detail = prepared.Detail!;
        var tenantAccessResult = prepared.TenantAccess!;
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
                OperationKind: ProjectContextOperationKind.Get,
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

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(assembled.Context, ResponseJsonOptions);
    }
}
