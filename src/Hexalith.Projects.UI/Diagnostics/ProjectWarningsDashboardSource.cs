namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Exposes only warning evidence delivered on the current inventory page; no diagnostic fan-out.</summary>
public sealed class ProjectWarningsDashboardSource(IProjectInventorySource inventory) : IProjectWarningsDashboardSource
{
    /// <inheritdoc/>
    public Task<ProjectWarningsDashboardLoadResult> LoadAsync(ProjectLifecycle? lifecycle, CancellationToken cancellationToken)
        => LoadPageAsync(new ListProjectsQuery(lifecycle?.ToString()), cancellationToken);

    /// <inheritdoc/>
    public async Task<ProjectWarningsDashboardLoadResult> LoadPageAsync(ListProjectsQuery query, CancellationToken cancellationToken)
    {
        ProjectInventoryLoadResult page = await inventory.LoadPageAsync(query, cancellationToken).ConfigureAwait(false);
        if (page.Feedback is not null)
            return ProjectWarningsDashboardLoadResult.FromFeedback(page.Feedback);
        return ProjectWarningsDashboardLoadResult.FromRows(page.Rows, [], new ProjectOperationalDashboardProjection
        {
            TotalVisibleProjects = page.Rows.Count,
            ActiveProjects = page.Rows.Count(row => row.Lifecycle == ProjectLifecycle.Active),
            ArchivedProjects = page.Rows.Count(row => row.Lifecycle == ProjectLifecycle.Archived),
        }, page.TenantScope) with
        {
            Inventory = page
        };
    }
}
