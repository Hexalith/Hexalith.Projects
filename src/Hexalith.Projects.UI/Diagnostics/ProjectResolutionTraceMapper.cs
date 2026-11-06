namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Preserves the delivered advisory result and coverage without claiming admission.</summary>
public static class ProjectResolutionTraceMapper
{
    /// <summary>Creates transient descriptors from the delivered observation.</summary>
    public static ProjectResolutionTraceLoadResult ToLoadResult(ProjectResolutionTraceRequest request,
        IReadOnlyList<string> folders, IReadOnlyList<string> files, ProjectAdvisoryResolutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);
        ProjectResolutionTraceProjection trace = new()
        {
            InputMode = request.Mode,
            PresentedConversationId = request.ConversationId,
            PresentedFolderIds = string.Join(", ", folders),
            PresentedFileIds = string.Join(", ", files),
            IncludeArchived = request.IncludeArchived,
            Result = response.Result,
            EvaluatedAt = response.EvaluatedAt,
            Incomplete = response.Incomplete,
            UnsupportedInputKinds = string.Join(", ", response.UnsupportedInputKinds),
            CandidateCount = response.Candidates.Count,
        };
        return ProjectResolutionTraceLoadResult.FromTrace(trace, response.Candidates.Select(candidate => new ProjectResolutionTraceCandidateProjection
        {
            Id = candidate.ProjectId,
            ProjectId = candidate.ProjectId,
            Rank = candidate.Rank,
            Score = candidate.Score,
            ReasonCodes = string.Join(", ", candidate.ReasonCodes),
        }).ToArray(), []);
    }

    /// <summary>Labels bounded observations without inferring an exclusive selection.</summary>
    public static string DeriveOutcomeLabel(ProjectResolutionTraceProjection trace, IReadOnlyList<ProjectResolutionTraceExclusionProjection> exclusions)
    {
        ArgumentNullException.ThrowIfNull(trace);
        return trace.Result switch
        {
            ResolutionResult.SingleCandidate => "One unselected suggestion",
            ResolutionResult.MultipleCandidates => "Multiple unselected suggestions",
            _ when trace.Incomplete => "No suggestion observed; coverage incomplete",
            _ => "No match in evaluated observations",
        };
    }
}
