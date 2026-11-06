namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>Retained REST comparison contract.</summary>
public interface ILegacyProjectInventorySource
{
    /// <summary>Loads the retained legacy observation.</summary>
    Task<ProjectInventoryLoadResult> ListProjectsAsync(Hexalith.Projects.Contracts.Ui.ProjectLifecycle? lifecycle, CancellationToken cancellationToken);
}
