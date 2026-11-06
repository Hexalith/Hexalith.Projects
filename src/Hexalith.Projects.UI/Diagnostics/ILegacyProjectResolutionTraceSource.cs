namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>Retained REST advisory comparison contract.</summary>
public interface ILegacyProjectResolutionTraceSource
{
    /// <summary>Loads the retained legacy trace.</summary>
    Task<ProjectResolutionTraceLoadResult> LoadTraceAsync(ProjectResolutionTraceRequest request, CancellationToken cancellationToken);
}
