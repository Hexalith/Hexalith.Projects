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

using Hexalith.Projects.Server.Folders;

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
        IProjectFolderDirectory folderDirectory,
        ProjectResolutionEngine resolutionEngine,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string correlationId = BeginQueryRequest(httpContext, out string? taskId);
        CancellationToken callerToken = cancellationToken;
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));
        cancellationToken = deadline.Token;
        try
        {

            if (httpContext.Request.Query["folderId"].Count + httpContext.Request.Query["fileId"].Count > MaxAttachmentReferenceCount)
            {
                return ValidationProblem(correlationId, taskId, "attachments");
            }

            if (!TryReadAttachmentIds(httpContext, "folderId", out IReadOnlyList<string> folderIds)
                || !TryReadAttachmentIds(httpContext, "fileId", out IReadOnlyList<string> fileIds)
                || (folderIds.Count == 0 && fileIds.Count == 0))
            {
                return SafeDenial(correlationId, taskId);
            }

            ProjectAuthorizationResult authorization = await authorizationGate
                .AuthorizeListAsync(tenantContext, httpContext, correlationId, taskId, cancellationToken)
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            if (authorization.IsAllowed && !HasCurrentLegacyReadAuthority(authorization)) return ReadModelUnavailable(correlationId, taskId);
            if (!authorization.IsAllowed)
            {
                return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                    ? ReadModelUnavailable(correlationId, taskId)
                    : SafeDenial(correlationId, taskId, authorization);
            }

            if (ValidateQueryRequest(httpContext, correlationId, taskId) is { } validationProblem)
            {
                return validationProblem;
            }

            if (!TryReadIncludeArchived(httpContext, out bool includeArchived))
            {
                return ValidationProblem(correlationId, taskId, "includeArchived");
            }

            // The retained result cannot express incomplete coverage without changing its v1 result type.
            if (fileIds.Count > 0) return ReadModelUnavailable(correlationId, taskId);

            string authoritativeTenantId = tenantContext.AuthoritativeTenantId!;
            string[]? acceptedFolders = await AuthorizeLegacyResolutionFoldersAsync(folderIds, folderDirectory, correlationId ?? httpContext.TraceIdentifier, cancellationToken)
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            if (acceptedFolders is null) return ReadModelUnavailable(correlationId, taskId);
            IReadOnlyList<ProjectReferenceIndexCandidateRow> projectedRows;
            try
            {
                projectedRows = acceptedFolders.Length == 0 ? [] : await referenceIndexReadModel
                    .ListByReferenceAsync(authoritativeTenantId, acceptedFolders, [], cancellationToken)
                    .WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ReadModelUnavailable(correlationId, taskId);
            }

            var allowedProjects = await AuthorizeLegacyResolutionCandidatesAsync(projectedRows
                .Where(row => row.TenantId == authoritativeTenantId).Select(row => row.ProjectId), authorizationGate,
                tenantContext, httpContext, correlationId, taskId, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (allowedProjects is null) return ReadModelUnavailable(correlationId, taskId);
            acceptedFolders = await AuthorizeLegacyResolutionFoldersAsync(acceptedFolders, folderDirectory, correlationId ?? httpContext.TraceIdentifier, cancellationToken)
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            if (acceptedFolders is null) return ReadModelUnavailable(correlationId, taskId);
            allowedProjects = await AuthorizeLegacyResolutionCandidatesAsync(allowedProjects.Keys, authorizationGate,
                tenantContext, httpContext, correlationId, taskId, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (allowedProjects is null) return ReadModelUnavailable(correlationId, taskId);
            authorization = await authorizationGate.AuthorizeListAsync(tenantContext, httpContext, correlationId, taskId, cancellationToken)
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            callerToken.ThrowIfCancellationRequested();
            if (authorization.IsAllowed && !HasCurrentLegacyReadAuthority(authorization)) return ReadModelUnavailable(correlationId, taskId);
            if (!authorization.IsAllowed) return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId) : SafeDenial(correlationId, taskId, authorization);
            List<AttachmentResolutionProjectCandidate> candidateProjects = new(projectedRows.Count);
            foreach (ProjectReferenceIndexCandidateRow row in projectedRows)
            {
                if (!string.Equals(row.TenantId, authoritativeTenantId, StringComparison.Ordinal)
                    || !allowedProjects.TryGetValue(row.ProjectId, out var detail)
                    || !string.Equals(detail.TenantId, authoritativeTenantId, StringComparison.Ordinal)
                    || !string.Equals(detail.ProjectId, row.ProjectId, StringComparison.Ordinal)
                    || detail.ProjectFolder is not { ReferenceState: ReferenceState.Included, FolderId: { } currentFolderId }
                    || !acceptedFolders.Contains(currentFolderId, StringComparer.Ordinal))
                {
                    continue;
                }

                List<AttachmentResolutionReference> folders = [];
                foreach (ProjectReferenceIndexItem reference in row.MatchedReferences)
                {
                    if (string.Equals(reference.ReferenceKind, AttachmentResolutionEvidenceMapper.FolderReferenceKind, StringComparison.Ordinal)
                        && string.Equals(reference.ReferenceId, currentFolderId, StringComparison.Ordinal))
                    {
                        folders.Add(new AttachmentResolutionReference(
                            AttachmentResolutionEvidenceMapper.FolderReferenceKind,
                            currentFolderId,
                            reference.ReferenceState));
                    }
                }

                candidateProjects.Add(new AttachmentResolutionProjectCandidate(
                    row.ProjectId,
                    DisplayName: null,
                    detail.Lifecycle,
                    folders,
                    []));
            }

            DateTimeOffset now = timeProvider.GetUtcNow();
            AttachmentResolutionMetadata attachments = new(
                acceptedFolders.Select(static id => new AttachmentResolutionReference(
                    AttachmentResolutionEvidenceMapper.FolderReferenceKind,
                    id,
                    ReferenceState.Included)).ToArray(),
                []);

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

            httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
            return IsLegacyResolutionIncomplete(resolution)
                ? ReadModelUnavailable(correlationId, taskId)
                : Results.Json(SafeLegacyResolution(resolution), ResponseJsonOptions);
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested) { throw; }
        catch (Exception) { return ReadModelUnavailable(correlationId, taskId); }
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
