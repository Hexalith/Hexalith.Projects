namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Loads bounded inventory through the supported SDK query.</summary>
public sealed class ProjectInventorySource(ProjectReadQueryClient queries) : IProjectInventorySource
{
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
        string view = $"{queries.CurrentContext?.TenantId}|{query.LifecycleState}|{query.PageSize}|{query.InspectDescriptiveMetadata}|{query.IncludeQuarantined}";
        // Continuations are opaque, are never reconstructed and can only continue this exact view.
        if (_view != view || query.Cursor != _issuedCursor)
            query = query with
            {
                Cursor = null
            };
        _view = view;
        int generation = ++_loadGeneration;
        ProjectReadResult<ProjectListPage> result = await queries.QueryAsync<ProjectListPage>("projects", ListProjectsQuery.QueryType, query, cancellationToken).ConfigureAwait(false);
        if (generation == _loadGeneration)
            _issuedCursor = result.Value?.Cursor;
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
