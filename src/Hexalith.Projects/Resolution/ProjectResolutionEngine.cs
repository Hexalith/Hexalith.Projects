// <copyright file="ProjectResolutionEngine.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Pure, compute-on-demand Project resolution engine (AR-10, Story 4.1). The engine consumes only
/// pre-fetched Projects-shaped evidence and returns metadata-only resolution results. It never reads
/// from sibling bounded contexts, never calls infrastructure, never writes events/projections/stores,
/// never reads wall-clock time, and never persists a trace.
/// </summary>
public sealed class ProjectResolutionEngine
{
    private readonly ILogger<ProjectResolutionEngine> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProjectResolutionEngine"/> class.</summary>
    /// <param name="logger">Optional logger used only for structured warnings on fail-closed edge cases.</param>
    public ProjectResolutionEngine(ILogger<ProjectResolutionEngine>? logger = null)
        => _logger = logger ?? NullLogger<ProjectResolutionEngine>.Instance;

    /// <summary>
    /// Resolves candidate Projects from pre-fetched evidence, applying the documented fail-closed
    /// qualification and deterministic scoring heuristic.
    /// </summary>
    /// <param name="context">Request-level resolution context.</param>
    /// <param name="candidates">Pre-fetched candidate Project evidence.</param>
    /// <returns>The metadata-only resolution result.</returns>
    public ProjectResolution Resolve(
        ProjectResolutionContext context,
        IReadOnlyList<ProjectResolutionCandidateEvidence> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        List<ScoredCandidate> qualifying = [];
        List<ResolutionExclusion> excluded = [];

        bool tenantAuthorityVerified = IsTenantAuthorityVerified(context);
        if (!tenantAuthorityVerified && candidates.Count > 0)
        {
            _logger.LogWarning(
                "Project resolution failed closed because tenant authority is unverifiable (candidateCount={CandidateCount}, correlationId={CorrelationId}, taskId={TaskId}).",
                candidates.Count,
                context.CorrelationId,
                context.TaskId);
        }

        foreach (ProjectResolutionCandidateEvidence candidate in candidates.OrderBy(static c => c.ProjectId, StringComparer.Ordinal))
        {
            if (!tenantAuthorityVerified)
            {
                excluded.Add(CreateExclusion(
                    candidate,
                    ReferenceState.TenantMismatch,
                    reasonCode: null,
                    ProjectContextInclusionDiagnostic.TenantMismatch));
                continue;
            }

            if (candidate.Lifecycle == ProjectLifecycle.Archived && !context.IncludeArchived)
            {
                excluded.Add(CreateExclusion(
                    candidate,
                    ReferenceState.Archived,
                    reasonCode: null,
                    ProjectContextInclusionDiagnostic.ProjectArchived));
                continue;
            }

            CandidateEvaluation evaluation = EvaluateCandidate(candidate, excluded);
            if (evaluation.Score >= ProjectResolutionScoringRules.MinimumQualifyingScore && evaluation.ReasonCodes.Count > 0)
            {
                qualifying.Add(new ScoredCandidate(candidate, evaluation.ReasonCodes, evaluation.Score));
            }
        }

        ResolutionCandidate[] ranked = qualifying
            .OrderByDescending(static c => c.Score)
            .ThenBy(static c => c.Candidate.ProjectId, StringComparer.Ordinal)
            .Select(static (c, index) => new ResolutionCandidate(
                c.Candidate.ProjectId,
                c.Candidate.DisplayName,
                c.ReasonCodes,
                Rank: index + 1,
                c.Score))
            .ToArray();

        ResolutionExclusion[] exclusions = excluded
            .OrderBy(static e => e.ProjectId, StringComparer.Ordinal)
            .ThenBy(static e => e.ReferenceState)
            .ThenBy(static e => e.ReasonCode)
            .ThenBy(static e => e.Diagnostic, StringComparer.Ordinal)
            .ToArray();

        return ranked.Length switch
        {
            0 => ProjectResolution.NoMatch(exclusions, context.Now),
            1 => ProjectResolution.SingleCandidate(ranked[0], exclusions, context.Now),
            _ => ProjectResolution.MultipleCandidates(ranked, exclusions, context.Now),
        };
    }

    private static bool IsTenantAuthorityVerified(ProjectResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.AuthoritativeTenantId))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(context.RequestedTenantId)
            || string.Equals(context.AuthoritativeTenantId, context.RequestedTenantId, StringComparison.Ordinal);
    }

    private static CandidateEvaluation EvaluateCandidate(
        ProjectResolutionCandidateEvidence candidate,
        List<ResolutionExclusion> excluded)
    {
        List<ProjectReasonCode> reasonCodes = [];

        foreach (ProjectResolutionMatchSignal signal in candidate.Signals.OrderBy(static s => s.ReferenceId, StringComparer.Ordinal))
        {
            if (signal.ReferenceState != ReferenceState.Included)
            {
                excluded.Add(CreateExclusion(
                    candidate,
                    signal.ReferenceState,
                    signal.ReasonCode,
                    DiagnosticFor(signal.ReferenceState)));
                continue;
            }

            if (!reasonCodes.Contains(signal.ReasonCode))
            {
                reasonCodes.Add(signal.ReasonCode);
            }
        }

        ProjectReasonCode[] orderedReasonCodes = ProjectResolutionScoringRules.Weights
            .Select(static w => w.ReasonCode)
            .Where(reasonCodes.Contains)
            .ToArray();

        int score = orderedReasonCodes.Sum(ProjectResolutionScoringRules.WeightFor);
        return new CandidateEvaluation(orderedReasonCodes, score);
    }

    private static ResolutionExclusion CreateExclusion(
        ProjectResolutionCandidateEvidence candidate,
        ReferenceState state,
        ProjectReasonCode? reasonCode,
        string? diagnostic)
        => new(candidate.ProjectId, candidate.DisplayName, state, reasonCode, diagnostic);

    private static string DiagnosticFor(ReferenceState state)
        => state switch
        {
            ReferenceState.Unauthorized => ProjectContextInclusionDiagnostic.ReferenceUnauthorized,
            ReferenceState.Unavailable => ProjectContextInclusionDiagnostic.ReferenceUnavailable,
            ReferenceState.Stale => ProjectContextInclusionDiagnostic.ReferenceStale,
            ReferenceState.Archived => ProjectContextInclusionDiagnostic.ReferenceArchived,
            ReferenceState.TenantMismatch => ProjectContextInclusionDiagnostic.TenantMismatch,
            ReferenceState.Conflict => ProjectContextInclusionDiagnostic.ReferenceConflict,
            ReferenceState.InvalidReference => ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier,
            ReferenceState.Pending => ProjectContextInclusionDiagnostic.ProjectFolderPending,
            ReferenceState.Excluded => ProjectContextInclusionDiagnostic.ReferenceRedacted,
            ReferenceState.Ambiguous => ProjectContextInclusionDiagnostic.ReferenceAmbiguous,
            _ => ProjectContextInclusionDiagnostic.ReferenceUnavailable,
        };

    private sealed record ScoredCandidate(
        ProjectResolutionCandidateEvidence Candidate,
        IReadOnlyList<ProjectReasonCode> ReasonCodes,
        int Score);

    private sealed record CandidateEvaluation(IReadOnlyList<ProjectReasonCode> ReasonCodes, int Score);
}
