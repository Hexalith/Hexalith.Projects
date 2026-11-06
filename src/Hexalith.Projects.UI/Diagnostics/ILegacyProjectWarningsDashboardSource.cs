namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>Retained REST comparison contract.</summary>
public interface ILegacyProjectWarningsDashboardSource
{
    /// <summary>Loads the retained legacy observation.</summary>
    Task<ProjectWarningsDashboardLoadResult> LoadAsync(Hexalith.Projects.Contracts.Ui.ProjectLifecycle? lifecycle, CancellationToken cancellationToken);
}
