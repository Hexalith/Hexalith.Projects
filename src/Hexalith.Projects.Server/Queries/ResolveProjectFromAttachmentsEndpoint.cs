// <copyright file="ResolveProjectFromAttachmentsEndpoint.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.Resolution;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Story 4.3 partial — adds the read-only <c>GET /api/v1/projects/resolution/from-attachments</c>
/// query endpoint (FR-13 / UJ-2). It is the impure host adapter around the Story 4.1 pure
/// <see cref="ProjectResolutionEngine"/> for metadata-only Project Folder and File Reference
/// attachment evidence.
/// </summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private const int MaxAttachmentReferenceCount = 32;

    private static async Task<IResult> ResolveProjectFromAttachmentsAsync(
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectReferenceIndexReadModel referenceIndexReadModel,
        ProjectResolutionEngine resolutionEngine,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;
        string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");
        taskId = IsCanonicalIdentifier(taskId) ? taskId : null;

        if (!TryReadAttachmentIds(httpContext, "folderId", out IReadOnlyList<string> folderIds)
            || !TryReadAttachmentIds(httpContext, "fileId", out IReadOnlyList<string> fileIds)
            || (folderIds.Count == 0 && fileIds.Count == 0))
        {
            return SafeDenial(correlationId, taskId);
        }

        if (folderIds.Count + fileIds.Count > MaxAttachmentReferenceCount)
        {
            return ValidationProblem(correlationId, taskId, "attachments");
        }

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
        IReadOnlyList<ProjectReferenceIndexCandidateRow> projectedRows;
        try
        {
            projectedRows = await referenceIndexReadModel
                .ListByReferenceAsync(authoritativeTenantId, folderIds, fileIds, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReadModelUnavailable(correlationId, taskId);
        }

        List<AttachmentResolutionProjectCandidate> candidateProjects = new(projectedRows.Count);
        foreach (ProjectReferenceIndexCandidateRow row in projectedRows)
        {
            if (!string.Equals(row.TenantId, authoritativeTenantId, StringComparison.Ordinal))
            {
                continue;
            }

            List<AttachmentResolutionReference> folders = [];
            List<AttachmentResolutionReference> files = [];
            foreach (ProjectReferenceIndexItem reference in row.MatchedReferences)
            {
                if (string.IsNullOrWhiteSpace(reference.ReferenceId))
                {
                    continue;
                }

                if (string.Equals(reference.ReferenceKind, AttachmentResolutionEvidenceMapper.FolderReferenceKind, StringComparison.Ordinal))
                {
                    folders.Add(new AttachmentResolutionReference(
                        AttachmentResolutionEvidenceMapper.FolderReferenceKind,
                        reference.ReferenceId,
                        reference.ReferenceState));
                }
                else if (string.Equals(reference.ReferenceKind, AttachmentResolutionEvidenceMapper.FileReferenceKind, StringComparison.Ordinal))
                {
                    files.Add(new AttachmentResolutionReference(
                        AttachmentResolutionEvidenceMapper.FileReferenceKind,
                        reference.ReferenceId,
                        reference.ReferenceState));
                }
            }

            candidateProjects.Add(new AttachmentResolutionProjectCandidate(
                row.ProjectId,
                row.DisplayName,
                row.Lifecycle,
                folders,
                files));
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        AttachmentResolutionMetadata attachments = new(
            folderIds.Select(static id => new AttachmentResolutionReference(
                AttachmentResolutionEvidenceMapper.FolderReferenceKind,
                id,
                ReferenceState.Included)).ToArray(),
            fileIds.Select(static id => new AttachmentResolutionReference(
                AttachmentResolutionEvidenceMapper.FileReferenceKind,
                id,
                ReferenceState.Included)).ToArray());

        IReadOnlyList<ProjectResolutionCandidateEvidence> candidates = AttachmentResolutionEvidenceMapper.Map(
            attachments,
            candidateProjects,
            now);

        ProjectResolutionContext context = new(
            AuthoritativeTenantId: authoritativeTenantId,
            RequestedTenantId: authoritativeTenantId,
            IncludeArchived: includeArchived,
            Now: now,
            CorrelationId: correlationId,
            TaskId: taskId,
            PresentedInputIds: [.. folderIds, .. fileIds]);

        ProjectResolution resolution = resolutionEngine.Resolve(context, candidates);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(resolution, ResponseJsonOptions);
    }

    private static bool TryReadAttachmentIds(
        HttpContext httpContext,
        string name,
        out IReadOnlyList<string> ids)
    {
        ids = [];
        if (!httpContext.Request.Query.TryGetValue(name, out StringValues values) || values.Count == 0)
        {
            return true;
        }

        SortedSet<string> unique = new(StringComparer.Ordinal);
        foreach (string? raw in values)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string trimmed = raw.Trim();
            if (ContainsControlChars(trimmed) || !IsCanonicalIdentifier(trimmed))
            {
                return false;
            }

            unique.Add(trimmed);
        }

        ids = unique.ToArray();
        return true;
    }
}
