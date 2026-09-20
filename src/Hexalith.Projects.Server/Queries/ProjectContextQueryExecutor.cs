// <copyright file="ProjectContextQueryExecutor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Queries;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;

/// <summary>
/// Shared zero-write Get/Explain assembly over persisted Project detail and the pure allowlist.
/// </summary>
public sealed class ProjectContextQueryExecutor(
    IReadModelStore readModelStore,
    ProjectAuthorizationGate authorizationGate,
    ProjectQueryEnvelopePrincipalBinding principalBinding,
    ProjectContextInclusionPolicy inclusionPolicy)
{
    private readonly IReadModelStore _readModelStore = readModelStore ?? throw new ArgumentNullException(nameof(readModelStore));
    private readonly ProjectAuthorizationGate _authorizationGate = authorizationGate ?? throw new ArgumentNullException(nameof(authorizationGate));
    private readonly ProjectQueryEnvelopePrincipalBinding _principalBinding = principalBinding ?? throw new ArgumentNullException(nameof(principalBinding));
    private readonly ProjectContextInclusionPolicy _inclusionPolicy = inclusionPolicy ?? throw new ArgumentNullException(nameof(inclusionPolicy));

    /// <summary>Assembles supported Project Context from envelope identity and persisted detail.</summary>
    /// <param name="query">The authenticated query envelope.</param>
    /// <param name="projectId">The canonically parsed query Project identity.</param>
    /// <param name="operationKind">The Get or Explain operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The AD-32 admission, including canonical safe denial.</returns>
    public async Task<ProjectContextAdmission> ExecuteAsync(
        QueryEnvelope query,
        string projectId,
        ProjectContextOperationKind operationKind,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query.TenantId)
            || string.IsNullOrWhiteSpace(query.UserId)
            || string.IsNullOrWhiteSpace(projectId)
            || !TargetsMatch(query, projectId)
            || !ProjectContextQueryAuthority.MatchesPresented(query.Scopes, ProjectContextQueryAuthority.ExpectedScopes)
            || !ProjectContextQueryAuthority.MatchesPresented(query.Audience, ProjectContextQueryAuthority.ExpectedAudience)
            || !_principalBinding.TryBind(query, out IProjectTenantContextAccessor tenantContext)
            || _principalBinding.HttpContext is not { } httpContext)
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        ProjectAuthorizationResult initialAuthorization = await _authorizationGate
            .AuthorizeSupportedReadAsync(
                projectId,
                tenantContext,
                httpContext,
                query.CorrelationId,
                taskId: null,
                LoadDetailAsync,
                cancellationToken)
            .ConfigureAwait(false);
        ProjectDetailItem? detail = initialAuthorization.ProjectDetail;
        TenantAccessAuthorizationResult? initialTenantAccess = initialAuthorization.TenantAccessResult;
        if (!initialAuthorization.IsAllowed
            || detail is null
            || initialTenantAccess is not { IsAllowed: true }
            || string.IsNullOrWhiteSpace(initialTenantAccess.ProjectionWatermark)
            || !string.Equals(initialTenantAccess.TenantId, query.TenantId, StringComparison.Ordinal)
            || detail.Lifecycle == ProjectLifecycle.Archived
            || !string.Equals(detail.TenantId, query.TenantId, StringComparison.Ordinal)
            || !string.Equals(detail.ProjectId, projectId, StringComparison.Ordinal))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        ProjectAuthorizationResult finalAuthorization = await _authorizationGate
            .AuthorizeSupportedReadAsync(
                projectId,
                tenantContext,
                httpContext,
                query.CorrelationId,
                taskId: null,
                LoadDetailAsync,
                cancellationToken)
            .ConfigureAwait(false);
        ProjectDetailItem? finalDetail = finalAuthorization.ProjectDetail;
        TenantAccessAuthorizationResult? finalTenantAccess = finalAuthorization.TenantAccessResult;
        if (!finalAuthorization.IsAllowed
            || finalDetail is null
            || finalTenantAccess is not { IsAllowed: true }
            || string.IsNullOrWhiteSpace(finalTenantAccess.ProjectionWatermark)
            || !string.Equals(finalTenantAccess.TenantId, query.TenantId, StringComparison.Ordinal)
            || !string.Equals(
                finalTenantAccess.ProjectionWatermark,
                initialTenantAccess.ProjectionWatermark,
                StringComparison.Ordinal)
            || !HasStableProjectAuthority(detail, finalDetail))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        detail = finalDetail;
        if (HasTooManyCandidates(detail))
        {
            return ProjectContextAdmissionAssembler.Unavailable(
                projectId,
                detail.Lifecycle,
                detail.UpdatedAt,
                detail.Sequence,
                projectCurrent: true,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: true,
                ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty);
        }

        if (!ProjectPersistedDetailValidator.IsValid(detail))
        {
            return ProjectContextAdmissionAssembler.Unavailable(
                projectId,
                Enum.IsDefined(detail.Lifecycle) ? detail.Lifecycle : ProjectLifecycle.Active,
                finalTenantAccess.LastEventTimestamp ?? initialTenantAccess.LastEventTimestamp ?? default,
                projectVersion: 0,
                projectCurrent: false,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: true,
                ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty);
        }

        DateTimeOffset asOf = detail.UpdatedAt;
        ProjectContextReferenceEvidence references = new(
            detail.ProjectFolder,
            detail.FileReferences,
            detail.MemoryReferences,
            Conversations: []);
        return _inclusionPolicy.AssembleAdmission(
            new ProjectContextAssemblyContext(
                query.TenantId,
                query.TenantId,
                projectId,
                operationKind,
                query.CorrelationId,
                TaskId: null,
                asOf),
            new ProjectContextProjectEvidence(detail),
            new ProjectContextTenantAccess(finalTenantAccess),
            references,
            detail.Sequence,
            asOf,
            ownerBackedTrustAvailable: false);
    }

    private async Task<ProjectDetailItem?> LoadDetailAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken)
        => (await _readModelStore
            .GetAsync<ProjectDetailItem>(
                ConversationStartSetupProjectionHandler.StoreName,
                ConversationStartSetupProjectionHandler.Key(tenantId, projectId),
                cancellationToken)
            .ConfigureAwait(false)).Value;

    private static bool TargetsMatch(QueryEnvelope query, string projectId)
    {
        if (!string.IsNullOrWhiteSpace(query.EntityId)
            && !string.Equals(query.EntityId, query.AggregateId, StringComparison.Ordinal))
        {
            return false;
        }

        string envelopeTarget = query.EntityId ?? query.AggregateId;
        return string.Equals(envelopeTarget, projectId, StringComparison.Ordinal);
    }

    private static bool HasStableProjectAuthority(ProjectDetailItem initial, ProjectDetailItem final)
        => string.Equals(initial.TenantId, final.TenantId, StringComparison.Ordinal)
            && string.Equals(initial.ProjectId, final.ProjectId, StringComparison.Ordinal)
            && initial.Lifecycle == final.Lifecycle
            && initial.Sequence == final.Sequence
            && initial.CreatedAt == final.CreatedAt
            && initial.UpdatedAt == final.UpdatedAt;

    private static bool HasTooManyCandidates(ProjectDetailItem detail)
        => (long)(detail.ProjectFolder is null ? 0 : 1)
            + (detail.FileReferences?.Count ?? 0)
            + (detail.MemoryReferences?.Count ?? 0)
            > ProjectContextReadLimits.MaxReferences;
}
