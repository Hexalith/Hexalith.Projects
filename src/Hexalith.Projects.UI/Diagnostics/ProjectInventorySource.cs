namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Loads bounded inventory through the supported SDK query.</summary>
public sealed class ProjectInventorySource(ProjectReadQueryClient queries) : IProjectInventorySource
{
    private readonly Lock _loadState = new();
    private string? _view;
    private string? _issuedCursor;
    private int _loadGeneration;

    /// <inheritdoc/>
    public Task<ProjectInventoryLoadResult> ListProjectsAsync(ProjectLifecycle? lifecycle, CancellationToken cancellationToken)
        => LoadPageAsync(new ListProjectsQuery(lifecycle?.ToString()), cancellationToken);

    /// <inheritdoc/>
    public async Task<ProjectInventoryLoadResult> LoadPageAsync(ListProjectsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        int generation;
        lock (_loadState)
        {
            generation = ++_loadGeneration;
        }

        string? view = null;
        ProjectReadResult<ProjectListPage> result = await queries.QueryAsync<ProjectListPage>("projects", ListProjectsQuery.QueryType, tenant =>
        {
            view = $"{tenant}|{query.LifecycleState}|{query.PageSize}|{query.InspectDescriptiveMetadata}|{query.IncludeQuarantined}";
            // An opaque continuation belongs to the exact tenant and view that issued it.
            lock (_loadState)
            {
                return _view == view && query.Cursor == _issuedCursor ? query : query with { Cursor = null };
            }
        }, cancellationToken).ConfigureAwait(false);
        lock (_loadState)
        {
            if (generation == _loadGeneration)
            {
                _view = view;
                _issuedCursor = result.Value?.Cursor;
            }
        }
        if (result.Value is not { } page)
            return ProjectInventoryLoadResult.FromFeedback(result.Feedback!);
        return ProjectInventoryLoadResult.FromRows(page.Items.Select(item => new ProjectInventoryRowProjection
        {
            Id = item.ProjectId,
            ProjectId = item.ProjectId,
            Name = item.Name ?? string.Empty,
            Lifecycle = item.LifecycleState,
            ProjectFolderAvailability = item.ProjectFolderAvailability,
            TenantScope = result.TenantScope,
        }).ToArray(), result.TenantScope) with
        {
            Page = page,
            Snapshot = page.Snapshot,
            Cursor = page.Cursor,
            HasMore = page.HasMore
        };
    }
}
