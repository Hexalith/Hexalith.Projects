// <copyright file="RefreshProjectContextEndpoint.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Generic;
using System.Linq;
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
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Memories;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Story 3.4 partial — adds the read-only <c>GET /api/v1/projects/{projectId}/context/refresh</c>
/// endpoint that returns the assembled <see cref="ProjectContext"/> after an on-the-fly recheck of
/// every linked Folders / Memories reference against the sibling ACLs at Refresh time, so the
/// returned context reflects the current state rather than stale projection assumptions
/// (FR-18 / UJ-4 / AR-9).
/// </summary>
/// <remarks>
/// <para>
/// The handler is a port of Story 3.2's <c>GetProjectContextAsync</c> with three changes:
/// (i) the assembly context's <c>OperationKind</c> is <see cref="ProjectContextOperationKind.Refresh"/>;
/// (ii) folder and memory references are rechecked via
/// <see cref="IProjectFolderDirectory.RefreshFolderReferenceAsync"/> and
/// <see cref="IProjectMemoryDirectory.RefreshMemoryReferenceAsync"/>, with the safe outcomes mapped to
/// fresh <see cref="ReferenceState"/> values that override the projection-stored state when the
/// recheck disagrees; (iii) the conversation page fetch and the ACL recheck fan-outs are awaited in
/// parallel via a single <see cref="Task.WhenAll(IEnumerable{Task})"/>.
/// </para>
/// <para>
/// File-reference recheck is deferred to a follow-up story per the Story 3.4 capability-gate HALT
/// (option (a)): the Folders typed client has no stable read route that validates a file reference
/// by opaque <c>(folderId, fileReferenceId)</c> without <c>workspaceId</c> / <c>filePath</c> inputs,
/// and Projects MUST NOT store path-classified fields per <c>docs/payload-taxonomy.md</c>. Until a
/// Folders submodule story adds the opaque-id read route, file references retain their
/// projection-stored state (same behavior as Story 3.2 Get). The <c>RefreshFileReferenceAsync</c>
/// method is still added to the ACL interface (additive contracts rule).
/// </para>
/// <para>
/// Carry-forward invariants from Story 3.2 / 3.3: safe-denial 404 collapse for
/// <c>Unauthorized</c> / <c>ProjectUnavailable</c>; <c>Idempotency-Key</c> rejected on queries;
/// strict <c>X-Hexalith-Freshness</c> validation; defensive null-collapse on missing
/// <c>TenantAccessResult</c>; deterministic <c>(ReferenceKind, ReferenceId)</c> Ordinal sort owned by
/// the policy. The handler never duplicates a policy decision.
/// </para>
/// </remarks>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> RefreshProjectContextAsync(
        string projectId,
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationDirectory conversationDirectory,
        IProjectFolderDirectory folderDirectory,
        IProjectMemoryDirectory memoryDirectory,
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
        string safeCorrelationId = correlationId ?? string.Empty;
        string safeTaskId = taskId ?? string.Empty;

        // Fan out: conversation page + folder recheck + per-memory rechecks (file recheck deferred —
        // see class XML doc). All awaited with a single Task.WhenAll(...) over bounded task collections;
        // ordering is preserved by walking inputs in stored order and mapping by index. No
        // Task.WhenAny / no Thread.Sleep / no Task.Delay.
        Task<ProjectConversationsPage> conversationsTask = conversationDirectory.ListForProjectAsync(
            new ProjectId(projectId),
            new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
            new CallerPrincipalId(tenantContext.PrincipalId!),
            new PageRequest(ProjectContextConversationsPageSize, ContinuationCursor: null),
            cancellationToken);

        Task<ProjectFolderValidationResult>? folderRefreshTask = detail.ProjectFolder?.FolderId is { Length: > 0 } folderId
            ? folderDirectory.RefreshFolderReferenceAsync(new ProjectId(projectId), folderId, safeCorrelationId, cancellationToken)
            : null;

        Task<ProjectMemoryValidationResult>[] memoryRefreshTasks = detail.MemoryReferences
            .Select(memory => memoryDirectory.RefreshMemoryReferenceAsync(
                new ProjectId(projectId),
                memory.MemoryReferenceId,
                tenantContext.AuthoritativeTenantId!,
                safeCorrelationId,
                safeTaskId,
                cancellationToken))
            .ToArray();

        List<Task> awaitables = new(2 + memoryRefreshTasks.Length) { conversationsTask };
        if (folderRefreshTask is not null)
        {
            awaitables.Add(folderRefreshTask);
        }

        awaitables.AddRange(memoryRefreshTasks);
        await Task.WhenAll(awaitables).ConfigureAwait(false);

        ProjectConversationsPage conversations = await conversationsTask.ConfigureAwait(false);

        ProjectFolderReference? recheckedFolder = detail.ProjectFolder;
        if (detail.ProjectFolder is { } projectionFolder && folderRefreshTask is not null)
        {
            ProjectFolderValidationResult folderResult = await folderRefreshTask.ConfigureAwait(false);
            (ReferenceState folderState, DateTimeOffset folderObservedAt) = ProjectFolderValidationOutcomeMapper.Map(
                folderResult.Outcome,
                projectionFolder,
                now);
            recheckedFolder = projectionFolder with
            {
                ReferenceState = folderState,
                ObservedAt = folderObservedAt,
            };
        }

        IReadOnlyList<ProjectMemoryReference> recheckedMemories;
        if (memoryRefreshTasks.Length == 0)
        {
            recheckedMemories = detail.MemoryReferences;
        }
        else
        {
            List<ProjectMemoryReference> memoryList = new(memoryRefreshTasks.Length);
            for (int i = 0; i < memoryRefreshTasks.Length; i++)
            {
                ProjectMemoryReference projectionMemory = detail.MemoryReferences[i];
                ProjectMemoryValidationResult result = await memoryRefreshTasks[i].ConfigureAwait(false);
                (ReferenceState state, DateTimeOffset observedAt) = ProjectMemoryValidationOutcomeMapper.Map(
                    result.Outcome,
                    projectionMemory,
                    now);
                memoryList.Add(projectionMemory with
                {
                    ReferenceState = state,
                    ObservedAt = observedAt,
                });
            }

            recheckedMemories = memoryList;
        }

        IReadOnlyList<ProjectContextConversationEvidence> conversationEvidence =
            ProjectContextConversationEvidenceMapper.Map(conversations, now);

        ProjectContextAssemblyResult assembled = contextPolicy.Assemble(
            new ProjectContextAssemblyContext(
                AuthoritativeTenantId: tenantContext.AuthoritativeTenantId,
                RequestedTenantId: tenantContext.AuthoritativeTenantId,
                ProjectId: projectId,
                OperationKind: ProjectContextOperationKind.Refresh,
                CorrelationId: correlationId,
                TaskId: taskId,
                Now: now),
            new ProjectContextProjectEvidence(detail),
            new ProjectContextTenantAccess(tenantAccessResult),
            new ProjectContextReferenceEvidence(
                ProjectFolder: recheckedFolder,
                FileReferences: detail.FileReferences,
                MemoryReferences: recheckedMemories,
                Conversations: conversationEvidence));

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(assembled.Context, ResponseJsonOptions);
    }
}
