namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server;

/// <summary>Reads real retained rows with an explicit controlled review fault.</summary>
/// <param name="read">The actual source read and optional fault behavior.</param>
internal sealed class CutoverReviewDetailReadModel(Func<string, string, CancellationToken, Task<ProjectDetailItem?>> read) : IProjectDetailReadModel
{
    /// <inheritdoc/>
    public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
        => read(authoritativeTenantId, projectId, cancellationToken);
}
