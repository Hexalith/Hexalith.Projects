// <copyright file="ProjectResolutionTraceMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Ui;

using GeneratedCandidate = Hexalith.Projects.Client.Generated.ResolutionCandidate;
using GeneratedExclusion = Hexalith.Projects.Client.Generated.ResolutionExclusion;
using GeneratedResolution = Hexalith.Projects.Client.Generated.ProjectResolution;
using GeneratedResolutionExclusionReasonCode = Hexalith.Projects.Client.Generated.ResolutionExclusionReasonCode;
using GeneratedResolutionExclusionReferenceState = Hexalith.Projects.Client.Generated.ResolutionExclusionReferenceState;
using GeneratedResolutionResult = Hexalith.Projects.Client.Generated.ResolutionResult;

/// <summary>
/// Maps generated resolution query DTOs into transient UI descriptor rows.
/// </summary>
public static class ProjectResolutionTraceMapper
{
    public static ProjectResolutionTraceLoadResult ToLoadResult(
        ProjectResolutionTraceRequest request,
        IReadOnlyList<string> folderIds,
        IReadOnlyList<string> fileIds,
        GeneratedResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(resolution);

        ProjectResolutionTraceCandidateProjection[] candidates = resolution.Candidates
            .Select(MapCandidate)
            .ToArray();
        ProjectResolutionTraceExclusionProjection[] exclusions = resolution.Excluded
            .Select(MapExclusion)
            .ToArray();

        var trace = new ProjectResolutionTraceProjection
        {
            InputMode = request.Mode,
            PresentedConversationId = request.Mode == ProjectResolutionTraceRequest.ConversationMode
                ? Normalize(request.ConversationId)
                : null,
            PresentedFolderIds = string.Join(", ", folderIds),
            PresentedFileIds = string.Join(", ", fileIds),
            IncludeArchived = request.IncludeArchived,
            ObservedAt = resolution.ObservedAt,
            Result = ToContractResolutionResult(resolution.Result),
            CandidateCount = candidates.Length,
            ExclusionCount = exclusions.Length,
        };

        return ProjectResolutionTraceLoadResult.FromTrace(trace, candidates, exclusions);
    }

    public static string DeriveOutcomeLabel(
        ProjectResolutionTraceProjection trace,
        IReadOnlyList<ProjectResolutionTraceExclusionProjection> exclusions)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentNullException.ThrowIfNull(exclusions);

        return trace.Result switch
        {
            ResolutionResult.SingleCandidate => "Resolved",
            ResolutionResult.MultipleCandidates => "MultipleCandidates",
            _ when exclusions.Any(IsFailedClosed) => "FailedClosed",
            _ when exclusions.Count > 0 => "Excluded",
            _ => "NoMatch",
        };
    }

    private static ProjectResolutionTraceCandidateProjection MapCandidate(GeneratedCandidate candidate)
        => new()
        {
            Id = $"candidate:{candidate.ProjectId}",
            ProjectId = candidate.ProjectId ?? string.Empty,
            DisplayName = Normalize(candidate.DisplayName),
            Rank = candidate.Rank,
            Score = candidate.Score,
            ReasonCodes = string.Join(", ", candidate.ReasonCodes.Select(static reason => reason.ToString()).Distinct(StringComparer.Ordinal)),
        };

    private static ProjectResolutionTraceExclusionProjection MapExclusion(GeneratedExclusion exclusion)
        => new()
        {
            Id = $"exclusion:{exclusion.ProjectId}:{exclusion.ReferenceState}:{exclusion.ReasonCode?.ToString() ?? "none"}",
            ProjectId = exclusion.ProjectId ?? string.Empty,
            DisplayName = Normalize(exclusion.DisplayName),
            ReferenceState = ToContractReferenceState(exclusion.ReferenceState),
            ReasonCode = exclusion.ReasonCode is null ? null : ToContractReasonCode(exclusion.ReasonCode.Value),
            Diagnostic = Normalize(exclusion.Diagnostic),
        };

    private static bool IsFailedClosed(ProjectResolutionTraceExclusionProjection exclusion)
        => exclusion.ReferenceState is ReferenceState.Pending
            or ReferenceState.Unauthorized
            or ReferenceState.Unavailable
            or ReferenceState.Stale
            or ReferenceState.Ambiguous
            or ReferenceState.TenantMismatch
            or ReferenceState.Conflict
            or ReferenceState.InvalidReference
            // Unverifiable evidence maps to FailedClosed (docs/resolution-scoring-heuristic.md#Trace-Mapping).
            // Policy/archived exclusions (ProjectArchived, ReferenceArchived, ReferenceRedacted) are NOT
            // unverifiable and must map to Excluded, so they are intentionally absent here.
            || exclusion.Diagnostic is ProjectContextInclusionDiagnostic.TenantMismatch
                or ProjectContextInclusionDiagnostic.ProjectUnknown
                or ProjectContextInclusionDiagnostic.ReferenceUnauthorized
                or ProjectContextInclusionDiagnostic.ReferenceUnavailable
                or ProjectContextInclusionDiagnostic.ReferenceStale
                or ProjectContextInclusionDiagnostic.ReferenceConflict
                or ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier
                or ProjectContextInclusionDiagnostic.ProjectFolderPending
                or ProjectContextInclusionDiagnostic.ReferenceAmbiguous;

    private static ResolutionResult ToContractResolutionResult(GeneratedResolutionResult value)
        => Enum.TryParse(value.ToString(), ignoreCase: false, out ResolutionResult result)
            ? result
            : ResolutionResult.NoMatch;

    private static ReferenceState ToContractReferenceState(GeneratedResolutionExclusionReferenceState value)
        => Enum.TryParse(value.ToString(), ignoreCase: false, out ReferenceState result)
            ? result
            : ReferenceState.Unavailable;

    private static ProjectReasonCode ToContractReasonCode(GeneratedResolutionExclusionReasonCode value)
        => Enum.TryParse(value.ToString(), ignoreCase: false, out ProjectReasonCode result)
            ? result
            : ProjectReasonCode.MetadataMatched;

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
