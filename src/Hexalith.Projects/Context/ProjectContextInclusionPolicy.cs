// <copyright file="ProjectContextInclusionPolicy.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;

using Microsoft.Extensions.Logging;

/// <summary>
/// The pure, allowlist-based <c>ProjectContext</c> assembly policy that includes a reference only
/// after tenant, project, lifecycle, authorization, and freshness checks all pass (AR-9, Story 3.1).
/// This is the single source of truth Stories 3.2 (Get), 3.3 (Explain), 3.4 (Refresh), and 3.5
/// (Conversation Start Setup) all consume — duplicating its decision logic in those stories'
/// endpoints/handlers is a forbidden anti-pattern.
/// </summary>
/// <remarks>
/// <para>
/// Purity guardrails (AC 11): the policy never references the sibling-conversations,
/// sibling-folders, sibling-memories namespaces, no infrastructure clients, no AspNetCore types,
/// no networking clients; never reads any wall-clock or stopwatch source; never sleeps;
/// never fetches anything. The only side effect is an
/// <see cref="ILogger{TCategoryName}"/> warning when a candidate reference uses a
/// non-allowlisted kind (AC 5).
/// </para>
/// <para>
/// All checks in <see cref="ProjectContextInclusionOrder.Sequence"/> are evaluated in order;
/// outer checks (TenantAuthority, ProjectVisibility) short-circuit the assembly, inner checks
/// emit per-candidate exclusion rows but still let the rest of the context assemble.
/// </para>
/// </remarks>
public sealed class ProjectContextInclusionPolicy
{
    private readonly ILogger<ProjectContextInclusionPolicy> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProjectContextInclusionPolicy"/> class.</summary>
    /// <param name="logger">Optional logger used to record warnings for non-allowlisted reference kinds.</param>
    public ProjectContextInclusionPolicy(ILogger<ProjectContextInclusionPolicy>? logger = null)
        => _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectContextInclusionPolicy>.Instance;

    /// <summary>
    /// Assembles the metadata-only <see cref="ProjectContext"/> for the supplied evidence inputs,
    /// applying the AR-9 inclusion order and emitting per-candidate evaluation rows for Story 3.3.
    /// </summary>
    /// <param name="context">The request-level inputs (tenant identifiers, operation, correlation, now).</param>
    /// <param name="project">The project projection evidence.</param>
    /// <param name="tenantAccess">The Story 1.6 tenant-access authorization result.</param>
    /// <param name="references">The per-kind candidate-reference evidence.</param>
    /// <returns>The assembled result containing the assembled context and the per-candidate evaluation trace.</returns>
    public ProjectContextAssemblyResult Assemble(
        ProjectContextAssemblyContext context,
        ProjectContextProjectEvidence project,
        ProjectContextTenantAccess tenantAccess,
        ProjectContextReferenceEvidence references)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(tenantAccess);
        ArgumentNullException.ThrowIfNull(references);

        ProjectContextFreshness freshness = MapFreshness(tenantAccess.Result.FreshnessStatus);

        // 1. TenantAuthority — collapse to Unauthorized on every non-Allowed outcome that is not
        //    bounded-stale-for-reads.
        TenantAuthorityVerdict authority = EvaluateTenantAuthority(context, tenantAccess, freshness);
        if (authority.CollapseToUnauthorized)
        {
            return new ProjectContextAssemblyResult(
                ProjectContext.Unauthorized(
                    context.RequestedTenantId ?? context.AuthoritativeTenantId ?? string.Empty,
                    context.ProjectId ?? string.Empty,
                    context.Now,
                    authority.Freshness),
                Array.Empty<ProjectContextEvaluation>());
        }

        // 2. ProjectVisibility — null detail OR (Detail.TenantId != context.AuthoritativeTenantId) →
        //    ProjectUnavailable (safe-denial; never reveals cross-tenant existence).
        ProjectDetailItem? detail = project.Detail;
        if (detail is null || !string.Equals(detail.TenantId, context.AuthoritativeTenantId, StringComparison.Ordinal))
        {
            return new ProjectContextAssemblyResult(
                ProjectContext.ProjectUnavailable(
                    context.RequestedTenantId ?? context.AuthoritativeTenantId ?? string.Empty,
                    context.ProjectId ?? string.Empty,
                    context.Now,
                    authority.Freshness),
                Array.Empty<ProjectContextEvaluation>());
        }

        // 3..7. Per-candidate evaluation. Build deterministic lists for the assembled result.
        List<ProjectContextReference> includedFolder = [];
        List<ProjectContextReference> includedFiles = [];
        List<ProjectContextReference> includedMemories = [];
        List<ProjectContextReference> includedConversations = [];
        List<ProjectContextExclusion> excluded = [];
        List<ProjectContextEvaluation> evaluations = [];

        bool projectIsArchived = detail.Lifecycle == ProjectLifecycle.Archived;

        // 3a. Project Folder.
        if (references.ProjectFolder is { } folder)
        {
            EvaluateFolderCandidate(
                folder,
                projectIsArchived,
                context.Now,
                evaluations,
                excluded,
                includedFolder);
        }

        // 3b. File references.
        foreach (ProjectFileReference file in references.FileReferences.OrderBy(static f => f.FileReferenceId, StringComparer.Ordinal))
        {
            EvaluateFileCandidate(
                file,
                projectIsArchived,
                context.Now,
                evaluations,
                excluded,
                includedFiles);
        }

        // 3c. Memory references.
        foreach (ProjectMemoryReference memory in references.MemoryReferences.OrderBy(static m => m.MemoryReferenceId, StringComparer.Ordinal))
        {
            EvaluateMemoryCandidate(
                memory,
                projectIsArchived,
                context.Now,
                evaluations,
                excluded,
                includedMemories);
        }

        // 3d. Conversations.
        foreach (ProjectContextConversationEvidence conversation in references.Conversations.OrderBy(static c => c.ConversationId, StringComparer.Ordinal))
        {
            EvaluateConversationCandidate(
                conversation,
                projectIsArchived,
                context.Now,
                evaluations,
                excluded,
                includedConversations,
                context);
        }

        // Deterministic ordering across all output lists by (ReferenceKind, ReferenceId) Ordinal.
        ProjectContextReference? singleFolder = includedFolder.FirstOrDefault();
        IReadOnlyList<ProjectContextReference> conversationsOut = SortRefs(includedConversations);
        IReadOnlyList<ProjectContextReference> filesOut = SortRefs(includedFiles);
        IReadOnlyList<ProjectContextReference> memoriesOut = SortRefs(includedMemories);
        IReadOnlyList<ProjectContextExclusion> excludedOut = excluded
            .OrderBy(static e => e.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(static e => e.ReferenceId, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<ProjectContextEvaluation> evaluationsOut = evaluations
            .OrderBy(static e => e.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(static e => e.ReferenceId, StringComparer.Ordinal)
            .ToArray();

        ProjectContext assembled = new(
            detail.TenantId,
            detail.ProjectId,
            detail.Lifecycle,
            detail.Setup,
            singleFolder,
            conversationsOut,
            filesOut,
            memoriesOut,
            excludedOut,
            ProjectContextAssemblyOutcome.Assembled,
            context.Now,
            authority.Freshness);

        return new ProjectContextAssemblyResult(assembled, evaluationsOut);
    }

    private static IReadOnlyList<ProjectContextReference> SortRefs(List<ProjectContextReference> refs)
        => refs
            .OrderBy(static r => r.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(static r => r.ReferenceId, StringComparer.Ordinal)
            .ToArray();

    private static ProjectContextFreshness MapFreshness(TenantProjectionFreshnessStatus status)
        => status switch
        {
            TenantProjectionFreshnessStatus.Fresh => ProjectContextFreshness.Fresh,
            TenantProjectionFreshnessStatus.Stale => ProjectContextFreshness.Stale,
            TenantProjectionFreshnessStatus.Unavailable => ProjectContextFreshness.Unavailable,
            TenantProjectionFreshnessStatus.Future => ProjectContextFreshness.Unknown,
            _ => ProjectContextFreshness.Unknown,
        };

    private static TenantAuthorityVerdict EvaluateTenantAuthority(
        ProjectContextAssemblyContext context,
        ProjectContextTenantAccess tenantAccess,
        ProjectContextFreshness mappedFreshness)
    {
        if (string.IsNullOrWhiteSpace(context.AuthoritativeTenantId))
        {
            return new TenantAuthorityVerdict(true, ProjectContextFreshness.Unknown);
        }

        if (!string.IsNullOrWhiteSpace(context.RequestedTenantId)
            && !string.Equals(context.AuthoritativeTenantId, context.RequestedTenantId, StringComparison.Ordinal))
        {
            return new TenantAuthorityVerdict(true, mappedFreshness);
        }

        TenantAccessAuthorizationResult result = tenantAccess.Result;
        bool readOnlyOperation = IsReadOnlyOperation(context.OperationKind);

        return result.Outcome switch
        {
            TenantAccessOutcome.Allowed => new TenantAuthorityVerdict(false, mappedFreshness),
            TenantAccessOutcome.StaleProjection when readOnlyOperation
                => new TenantAuthorityVerdict(false, ProjectContextFreshness.Stale),
            _ => new TenantAuthorityVerdict(true, mappedFreshness),
        };
    }

    private static bool IsReadOnlyOperation(ProjectContextOperationKind operationKind)
        => operationKind is ProjectContextOperationKind.Get
            or ProjectContextOperationKind.Refresh
            or ProjectContextOperationKind.Explain
            or ProjectContextOperationKind.GetConversationStartSetup;

    private void EvaluateFolderCandidate(
        ProjectFolderReference candidate,
        bool projectIsArchived,
        DateTimeOffset now,
        List<ProjectContextEvaluation> evaluations,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextReference> includedFolder)
    {
        const string Kind = "folder";
        string referenceId = string.IsNullOrWhiteSpace(candidate.FolderId)
            ? "pending"
            : candidate.FolderId!;

        if (!ProjectContextInclusionOrder.IsAllowlisted(Kind))
        {
            RecordNonAllowlistedKind(Kind, referenceId, now, evaluations, excluded);
            return;
        }

        if (projectIsArchived)
        {
            ReferenceState archivedState = ReferenceState.Archived;
            string diagnostic = ProjectContextDiagnostics.For(ProjectContextInclusionCheck.ProjectLifecycle, archivedState);
            excluded.Add(new ProjectContextExclusion(
                Kind,
                referenceId,
                archivedState,
                ReasonCode: null,
                ProjectContextInclusionCheck.ProjectLifecycle,
                diagnostic));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                archivedState,
                ProjectContextInclusionCheck.ProjectLifecycle,
                ReasonCode: null,
                diagnostic,
                now));
            return;
        }

        ReferenceState state = candidate.ReferenceState;
        if (state == ReferenceState.Included)
        {
            ProjectContextReference reference = new(
                Kind,
                referenceId,
                candidate.DisplayName,
                state,
                ProjectReasonCode.ProjectFolderMatched,
                candidate.ObservedAt);
            includedFolder.Add(reference);
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                state,
                FailedCheck: null,
                ProjectReasonCode.ProjectFolderMatched,
                Diagnostic: null,
                candidate.ObservedAt));
            return;
        }

        (ProjectContextInclusionCheck failedCheck, string diagnosticKey) = ClassifyReferenceState(state);
        excluded.Add(new ProjectContextExclusion(
            Kind,
            referenceId,
            state,
            ReasonCode: null,
            failedCheck,
            diagnosticKey));
        evaluations.Add(new ProjectContextEvaluation(
            Kind,
            referenceId,
            state,
            failedCheck,
            ReasonCode: null,
            diagnosticKey,
            candidate.ObservedAt));
    }

    private void EvaluateFileCandidate(
        ProjectFileReference candidate,
        bool projectIsArchived,
        DateTimeOffset now,
        List<ProjectContextEvaluation> evaluations,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextReference> includedFiles)
    {
        const string Kind = "file";
        string referenceId = candidate.FileReferenceId;

        if (string.IsNullOrWhiteSpace(referenceId))
        {
            RecordInvalidIdentifier(Kind, "unknown", now, evaluations, excluded);
            return;
        }

        if (!ProjectContextInclusionOrder.IsAllowlisted(Kind))
        {
            RecordNonAllowlistedKind(Kind, referenceId, now, evaluations, excluded);
            return;
        }

        if (projectIsArchived)
        {
            ReferenceState archivedState = ReferenceState.Archived;
            string diagnostic = ProjectContextDiagnostics.For(ProjectContextInclusionCheck.ProjectLifecycle, archivedState);
            excluded.Add(new ProjectContextExclusion(
                Kind,
                referenceId,
                archivedState,
                ReasonCode: null,
                ProjectContextInclusionCheck.ProjectLifecycle,
                diagnostic));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                archivedState,
                ProjectContextInclusionCheck.ProjectLifecycle,
                ReasonCode: null,
                diagnostic,
                now));
            return;
        }

        ReferenceState state = candidate.ReferenceState;
        if (state == ReferenceState.Included)
        {
            includedFiles.Add(new ProjectContextReference(
                Kind,
                referenceId,
                candidate.DisplayName,
                state,
                ProjectReasonCode.FileReferenceMatched,
                candidate.ObservedAt));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                state,
                FailedCheck: null,
                ProjectReasonCode.FileReferenceMatched,
                Diagnostic: null,
                candidate.ObservedAt));
            return;
        }

        (ProjectContextInclusionCheck failedCheck, string diagnosticKey) = ClassifyReferenceState(state);
        excluded.Add(new ProjectContextExclusion(
            Kind,
            referenceId,
            state,
            ReasonCode: null,
            failedCheck,
            diagnosticKey));
        evaluations.Add(new ProjectContextEvaluation(
            Kind,
            referenceId,
            state,
            failedCheck,
            ReasonCode: null,
            diagnosticKey,
            candidate.ObservedAt));
    }

    private void EvaluateMemoryCandidate(
        ProjectMemoryReference candidate,
        bool projectIsArchived,
        DateTimeOffset now,
        List<ProjectContextEvaluation> evaluations,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextReference> includedMemories)
    {
        const string Kind = "memory";
        string referenceId = candidate.MemoryReferenceId;

        if (string.IsNullOrWhiteSpace(referenceId))
        {
            RecordInvalidIdentifier(Kind, "unknown", now, evaluations, excluded);
            return;
        }

        if (!ProjectContextInclusionOrder.IsAllowlisted(Kind))
        {
            RecordNonAllowlistedKind(Kind, referenceId, now, evaluations, excluded);
            return;
        }

        if (projectIsArchived)
        {
            ReferenceState archivedState = ReferenceState.Archived;
            string diagnostic = ProjectContextDiagnostics.For(ProjectContextInclusionCheck.ProjectLifecycle, archivedState);
            excluded.Add(new ProjectContextExclusion(
                Kind,
                referenceId,
                archivedState,
                ReasonCode: null,
                ProjectContextInclusionCheck.ProjectLifecycle,
                diagnostic));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                archivedState,
                ProjectContextInclusionCheck.ProjectLifecycle,
                ReasonCode: null,
                diagnostic,
                now));
            return;
        }

        ReferenceState state = candidate.ReferenceState;
        if (state == ReferenceState.Included)
        {
            includedMemories.Add(new ProjectContextReference(
                Kind,
                referenceId,
                candidate.DisplayName,
                state,
                ProjectReasonCode.MemoryMatched,
                candidate.ObservedAt));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                state,
                FailedCheck: null,
                ProjectReasonCode.MemoryMatched,
                Diagnostic: null,
                candidate.ObservedAt));
            return;
        }

        // Memories ACL recheck may produce TenantMismatch — collapse to Unauthorized at the boundary
        // with the closed-vocabulary "tenantMismatch" diagnostic (Story 2.6 ADR).
        if (state == ReferenceState.TenantMismatch)
        {
            excluded.Add(new ProjectContextExclusion(
                Kind,
                referenceId,
                ReferenceState.Unauthorized,
                ReasonCode: null,
                ProjectContextInclusionCheck.ReferenceAuthorization,
                ProjectContextInclusionDiagnostic.TenantMismatch));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                ReferenceState.Unauthorized,
                ProjectContextInclusionCheck.ReferenceAuthorization,
                ReasonCode: null,
                ProjectContextInclusionDiagnostic.TenantMismatch,
                candidate.ObservedAt));
            return;
        }

        (ProjectContextInclusionCheck failedCheck, string diagnosticKey) = ClassifyReferenceState(state);
        excluded.Add(new ProjectContextExclusion(
            Kind,
            referenceId,
            state,
            ReasonCode: null,
            failedCheck,
            diagnosticKey));
        evaluations.Add(new ProjectContextEvaluation(
            Kind,
            referenceId,
            state,
            failedCheck,
            ReasonCode: null,
            diagnosticKey,
            candidate.ObservedAt));
    }

    private void EvaluateConversationCandidate(
        ProjectContextConversationEvidence candidate,
        bool projectIsArchived,
        DateTimeOffset now,
        List<ProjectContextEvaluation> evaluations,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextReference> includedConversations,
        ProjectContextAssemblyContext context)
    {
        const string Kind = "conversation";
        string referenceId = candidate.ConversationId;

        if (!ProjectContextInclusionOrder.IsAllowlisted(Kind))
        {
            RecordNonAllowlistedKind(Kind, referenceId, now, evaluations, excluded);
            return;
        }

        if (projectIsArchived)
        {
            ReferenceState archivedState = ReferenceState.Archived;
            string diagnostic = ProjectContextDiagnostics.For(ProjectContextInclusionCheck.ProjectLifecycle, archivedState);
            excluded.Add(new ProjectContextExclusion(
                Kind,
                referenceId,
                archivedState,
                ReasonCode: null,
                ProjectContextInclusionCheck.ProjectLifecycle,
                diagnostic));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                archivedState,
                ProjectContextInclusionCheck.ProjectLifecycle,
                ReasonCode: null,
                diagnostic,
                now));
            return;
        }

        (ReferenceState resultState, ProjectContextInclusionCheck? failedCheck) = MapConversationTrustSignal(candidate.TrustSignal);
        if (failedCheck is null)
        {
            includedConversations.Add(new ProjectContextReference(
                Kind,
                referenceId,
                candidate.DisplayLabel,
                resultState,
                ProjectReasonCode.ConversationLinked,
                candidate.LastCheckedAt));
            evaluations.Add(new ProjectContextEvaluation(
                Kind,
                referenceId,
                resultState,
                FailedCheck: null,
                ProjectReasonCode.ConversationLinked,
                Diagnostic: null,
                candidate.LastCheckedAt));
            return;
        }

        string diagnosticKey = ProjectContextDiagnostics.For(failedCheck.Value, resultState);
        excluded.Add(new ProjectContextExclusion(
            Kind,
            referenceId,
            resultState,
            ReasonCode: null,
            failedCheck.Value,
            diagnosticKey));
        evaluations.Add(new ProjectContextEvaluation(
            Kind,
            referenceId,
            resultState,
            failedCheck.Value,
            ReasonCode: null,
            diagnosticKey,
            candidate.LastCheckedAt));
    }

    private static (ReferenceState ResultState, ProjectContextInclusionCheck? FailedCheck) MapConversationTrustSignal(
        Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal signal)
        => signal switch
        {
            Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal.Current
                => (ReferenceState.Included, null),
            Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal.Stale
                => (ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness),
            Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal.MixedGeneration
                => (ReferenceState.Stale, ProjectContextInclusionCheck.ReferenceFreshness),
            Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal.Rebuilding
                => (ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness),
            Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal.Unavailable
                => (ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness),
            Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal.Forbidden
                => (ReferenceState.Unauthorized, ProjectContextInclusionCheck.ReferenceAuthorization),
            Hexalith.Projects.Contracts.Queries.ProjectConversationTrustSignal.Redacted
                => (ReferenceState.Excluded, ProjectContextInclusionCheck.ReferenceFreshness),
            _ => (ReferenceState.Unavailable, ProjectContextInclusionCheck.ReferenceFreshness),
        };

    private static (ProjectContextInclusionCheck FailedCheck, string Diagnostic) ClassifyReferenceState(ReferenceState state)
        => state switch
        {
            ReferenceState.Unauthorized
                => (ProjectContextInclusionCheck.ReferenceAuthorization, ProjectContextInclusionDiagnostic.ReferenceUnauthorized),
            ReferenceState.Unavailable
                => (ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ReferenceUnavailable),
            ReferenceState.Stale
                => (ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ReferenceStale),
            ReferenceState.Pending
                => (ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ProjectFolderPending),
            ReferenceState.Archived
                => (ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextInclusionDiagnostic.ReferenceArchived),
            ReferenceState.Ambiguous
                => (ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextInclusionDiagnostic.ReferenceAmbiguous),
            ReferenceState.Conflict
                => (ProjectContextInclusionCheck.ReferenceLifecycle, ProjectContextInclusionDiagnostic.ReferenceConflict),
            ReferenceState.InvalidReference
                => (ProjectContextInclusionCheck.ReferenceKindAllowlist, ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier),
            ReferenceState.Excluded
                => (ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ReferenceRedacted),
            ReferenceState.TenantMismatch
                => (ProjectContextInclusionCheck.ReferenceAuthorization, ProjectContextInclusionDiagnostic.TenantMismatch),
            _ => (ProjectContextInclusionCheck.ReferenceFreshness, ProjectContextInclusionDiagnostic.ReferenceUnavailable),
        };

    private void RecordNonAllowlistedKind(
        string referenceKind,
        string referenceId,
        DateTimeOffset now,
        List<ProjectContextEvaluation> evaluations,
        List<ProjectContextExclusion> excluded)
    {
        ReferenceState state = ReferenceState.InvalidReference;
        string diagnostic = ProjectContextInclusionDiagnostic.ReferenceKindNotAllowlisted;
        excluded.Add(new ProjectContextExclusion(
            referenceKind,
            referenceId,
            state,
            ReasonCode: null,
            ProjectContextInclusionCheck.ReferenceKindAllowlist,
            diagnostic));
        evaluations.Add(new ProjectContextEvaluation(
            referenceKind,
            referenceId,
            state,
            ProjectContextInclusionCheck.ReferenceKindAllowlist,
            ReasonCode: null,
            diagnostic,
            now));

        _logger.LogWarning(
            "ProjectContext inclusion policy rejected non-allowlisted reference kind '{ReferenceKind}' for reference '{ReferenceId}' (diagnostic={Diagnostic}).",
            referenceKind,
            referenceId,
            diagnostic);
    }

    private static void RecordInvalidIdentifier(
        string referenceKind,
        string fallbackReferenceId,
        DateTimeOffset now,
        List<ProjectContextEvaluation> evaluations,
        List<ProjectContextExclusion> excluded)
    {
        ReferenceState state = ReferenceState.InvalidReference;
        string diagnostic = ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier;
        excluded.Add(new ProjectContextExclusion(
            referenceKind,
            fallbackReferenceId,
            state,
            ReasonCode: null,
            ProjectContextInclusionCheck.ReferenceKindAllowlist,
            diagnostic));
        evaluations.Add(new ProjectContextEvaluation(
            referenceKind,
            fallbackReferenceId,
            state,
            ProjectContextInclusionCheck.ReferenceKindAllowlist,
            ReasonCode: null,
            diagnostic,
            now));
    }

    private readonly record struct TenantAuthorityVerdict(bool CollapseToUnauthorized, ProjectContextFreshness Freshness);
}
