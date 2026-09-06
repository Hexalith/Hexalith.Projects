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
    TenantAccessAuthorizer tenantAccessAuthorizer,
    ProjectContextInclusionPolicy inclusionPolicy)
{
    private readonly IReadModelStore _readModelStore = readModelStore ?? throw new ArgumentNullException(nameof(readModelStore));
    private readonly TenantAccessAuthorizer _tenantAccessAuthorizer = tenantAccessAuthorizer ?? throw new ArgumentNullException(nameof(tenantAccessAuthorizer));
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
            || !ProjectContextQueryAuthority.MatchesPresented(query.Audience, ProjectContextQueryAuthority.ExpectedAudience))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        string actorId = query.OriginalActorId ?? query.UserId;
        TenantAccessAuthorizationContext authorizationContext = new(query.TenantId, actorId, query.TenantId);
        TenantAccessAuthorizationResult tenantAccess = await _tenantAccessAuthorizer
            .AuthorizeDiagnosticReadAsync(authorizationContext, cancellationToken)
            .ConfigureAwait(false);
        if (!tenantAccess.IsAllowed)
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        ProjectDetailItem? detail;
        try
        {
            ReadModelEntry<ProjectDetailItem> entry = await _readModelStore
                .GetAsync<ProjectDetailItem>(
                    ConversationStartSetupProjectionHandler.StoreName,
                    ConversationStartSetupProjectionHandler.Key(query.TenantId, projectId),
                    cancellationToken)
                .ConfigureAwait(false);
            detail = entry.Value;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TenantAccessAuthorizationResult faultReauthorization = await ReauthorizeAsync(
                    authorizationContext,
                    tenantAccess,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!faultReauthorization.IsAllowed)
            {
                return ProjectContextAdmission.SafeDenial(projectId);
            }

            return ProjectContextAdmissionAssembler.Unavailable(
                projectId,
                ProjectLifecycle.Active,
                faultReauthorization.LastEventTimestamp ?? default,
                0,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: true,
                overflow: false);
        }

        TenantAccessAuthorizationResult reauthorized = await ReauthorizeAsync(
                authorizationContext,
                tenantAccess,
                cancellationToken)
            .ConfigureAwait(false);
        if (!reauthorized.IsAllowed)
        {
            return ProjectContextAdmission.SafeDenial(projectId);
        }

        if (detail is null
            || detail.Lifecycle != ProjectLifecycle.Active
            || !string.Equals(detail.TenantId, query.TenantId, StringComparison.Ordinal)
            || !string.Equals(detail.ProjectId, projectId, StringComparison.Ordinal))
        {
            return ProjectContextAdmission.SafeDenial(projectId);
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
            new ProjectContextTenantAccess(reauthorized),
            references,
            detail.Sequence,
            asOf,
            ownerBackedTrustAvailable: false);
    }

    private async Task<TenantAccessAuthorizationResult> ReauthorizeAsync(
        TenantAccessAuthorizationContext authorizationContext,
        TenantAccessAuthorizationResult prior,
        CancellationToken cancellationToken)
    {
        TenantAccessAuthorizationResult current = await _tenantAccessAuthorizer
            .AuthorizeDiagnosticReadAsync(authorizationContext, cancellationToken)
            .ConfigureAwait(false);
        if (!current.IsAllowed)
        {
            return current;
        }

        if (!string.Equals(current.ProjectionWatermark, prior.ProjectionWatermark, StringComparison.Ordinal))
        {
            return new TenantAccessAuthorizationResult(
                TenantAccessOutcome.Denied,
                "authority-version-mismatch",
                current.TenantId,
                current.ProjectionWatermark,
                current.LastEventTimestamp,
                current.ProjectionAge,
                current.FreshnessStatus,
                current.Source);
        }

        return current;
    }

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
}
