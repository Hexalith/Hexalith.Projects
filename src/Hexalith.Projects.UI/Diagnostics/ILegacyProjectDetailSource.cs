namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>Retained REST comparison contract.</summary>
public interface ILegacyProjectDetailSource
{
    /// <summary>Loads the retained legacy observation.</summary>
    Task<ProjectDetailLoadResult> GetProjectDetailAsync(string projectId, CancellationToken cancellationToken);
}
