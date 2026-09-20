// <copyright file="ProjectContextQueryExecutor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Queries;

using System;
using System.Collections.Generic;
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
        if (finalTenantAccess is not { IsAllowed: true }
            || string.IsNullOrWhiteSpace(finalTenantAccess.ProjectionWatermark)
            || !string.Equals(finalTenantAccess.TenantId, query.TenantId, StringComparison.Ordinal)
            || !HasStableTenantAuthorizationEvidence(initialTenantAccess, finalTenantAccess))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        if (IsSupportedDetailStoreFault(finalAuthorization))
        {
            return ProjectContextAdmissionAssembler.Unavailable(
                projectId,
                ProjectLifecycle.Active,
                finalTenantAccess.LastEventTimestamp ?? initialTenantAccess.LastEventTimestamp ?? default,
                projectVersion: 0,
                projectCurrent: false,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: false,
                ProjectContextUnavailableCause.StoreFault);
        }

        if (!finalAuthorization.IsAllowed
            || finalDetail is null
            || !HasStableEventStoreAuthorizationEvidence(
                initialAuthorization.EventStoreValidationResult,
                finalAuthorization.EventStoreValidationResult))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        if (!ProjectPersistedDetailValidator.IsHeaderValid(detail)
            || !ProjectPersistedDetailValidator.IsHeaderValid(finalDetail))
        {
            return ProjectContextAdmissionAssembler.Unavailable(
                projectId,
                ProjectLifecycle.Active,
                finalTenantAccess.LastEventTimestamp ?? initialTenantAccess.LastEventTimestamp ?? default,
                projectVersion: 0,
                projectCurrent: false,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: true,
                ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty);
        }

        if (!HasStableProjectAuthorityMetadata(detail, finalDetail))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        if (HasTooManyCandidates(detail) || HasTooManyCandidates(finalDetail))
        {
            return ProjectContextAdmissionAssembler.Unavailable(
                projectId,
                detail.Lifecycle,
                detail.UpdatedAt,
                projectVersion: 0,
                projectCurrent: true,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: true,
                ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty);
        }

        if (!HasStableProjectAuthorityReferences(detail, finalDetail))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        detail = finalDetail;
        if (!ProjectPersistedDetailValidator.IsValid(detail))
        {
            return ProjectContextAdmissionAssembler.Unavailable(
                projectId,
                detail.Lifecycle,
                detail.UpdatedAt,
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

    private static bool HasStableProjectAuthorityMetadata(ProjectDetailItem initial, ProjectDetailItem final)
        => string.Equals(initial.TenantId, final.TenantId, StringComparison.Ordinal)
            && string.Equals(initial.ProjectId, final.ProjectId, StringComparison.Ordinal)
            && string.Equals(initial.Name, final.Name, StringComparison.Ordinal)
            && string.Equals(initial.Description, final.Description, StringComparison.Ordinal)
            && string.Equals(initial.SetupMetadata, final.SetupMetadata, StringComparison.Ordinal)
            && SetupsEqual(initial.Setup, final.Setup)
            && EqualityComparer<ProjectFolderReference?>.Default.Equals(initial.ProjectFolder, final.ProjectFolder)
            && initial.Lifecycle == final.Lifecycle
            && initial.Sequence == final.Sequence
            && initial.CreatedAt == final.CreatedAt
            && initial.UpdatedAt == final.UpdatedAt;

    private static bool HasStableTenantAuthorizationEvidence(
        TenantAccessAuthorizationResult initial,
        TenantAccessAuthorizationResult final)
        => initial.Outcome == final.Outcome
            && string.Equals(initial.Code, final.Code, StringComparison.Ordinal)
            && string.Equals(initial.TenantId, final.TenantId, StringComparison.Ordinal)
            && string.Equals(initial.ProjectionWatermark, final.ProjectionWatermark, StringComparison.Ordinal)
            && initial.LastEventTimestamp == final.LastEventTimestamp
            && HasNonRegressingProjectionAge(initial.ProjectionAge, final.ProjectionAge)
            && initial.FreshnessStatus == final.FreshnessStatus
            && string.Equals(initial.Source, final.Source, StringComparison.Ordinal);

    private static bool HasNonRegressingProjectionAge(TimeSpan? initial, TimeSpan? final)
        => (initial, final) switch
        {
            (null, null) => true,
            ({ } initialAge, { } finalAge) => finalAge >= initialAge,
            _ => false,
        };

    private static bool HasStableEventStoreAuthorizationEvidence(
        EventStoreAuthorizationValidationResult? initial,
        EventStoreAuthorizationValidationResult? final)
        => initial is { Status: EventStoreAuthorizationValidationStatus.Allowed }
            && final is { Status: EventStoreAuthorizationValidationStatus.Allowed }
            && string.Equals(initial.FreshnessWatermark, final.FreshnessWatermark, StringComparison.Ordinal)
            && string.Equals(initial.FreshnessClass, final.FreshnessClass, StringComparison.Ordinal);

    private static bool HasStableProjectAuthorityReferences(ProjectDetailItem initial, ProjectDetailItem final)
        => SequencesEqual(initial.FileReferences, final.FileReferences)
            && SequencesEqual(initial.MemoryReferences, final.MemoryReferences);

    private static bool IsSupportedDetailStoreFault(ProjectAuthorizationResult authorization)
        => !authorization.IsAllowed
            && authorization.TerminalLayer == AuthorizationLayer.ProjectAcl
            && authorization.Reason == ReferenceState.Unavailable
            && authorization.Retryable;

    private static bool SetupsEqual(ProjectSetup? initial, ProjectSetup? final)
    {
        if (ReferenceEquals(initial, final))
        {
            return true;
        }

        return initial is not null
            && final is not null
            && SequencesEqual(initial.Goals, final.Goals)
            && SequencesEqual(initial.UserInstructions, final.UserInstructions)
            && SequencesEqual(initial.PreferredSourceKinds, final.PreferredSourceKinds)
            && SequencesEqual(initial.ExcludedSourceKinds, final.ExcludedSourceKinds)
            && EqualityComparer<ConversationStartDefaults?>.Default.Equals(
                initial.ConversationStartDefaults,
                final.ConversationStartDefaults);
    }

    private static bool SequencesEqual<T>(IReadOnlyList<T>? initial, IReadOnlyList<T>? final)
    {
        if (ReferenceEquals(initial, final))
        {
            return true;
        }

        if (initial is null || final is null || initial.Count != final.Count)
        {
            return false;
        }

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int index = 0; index < initial.Count; index++)
        {
            if (!comparer.Equals(initial[index], final[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasTooManyCandidates(ProjectDetailItem detail)
        => (long)(detail.ProjectFolder is null ? 0 : 1)
            + (detail.FileReferences?.Count ?? 0)
            + (detail.MemoryReferences?.Count ?? 0)
            > ProjectContextReadLimits.MaxReferences;
}
