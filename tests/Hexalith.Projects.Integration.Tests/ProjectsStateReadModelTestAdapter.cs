namespace Hexalith.Projects.Integration.Tests;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Projects.Infrastructure;

/// <summary>Adapts existing serialized test backends to the platform read-model interface.</summary>
/// <param name="state">The existing test backend.</param>
internal sealed class ProjectsStateReadModelTestAdapter(IProjectsStateStore state) : IReadModelStore
{
    /// <inheritdoc/>
    public async Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
    {
        ProjectsStateEntry<T> entry = await state.GetAsync<T>(storeName, key, cancellationToken).ConfigureAwait(false);
        return new(entry.Value, entry.ETag);
    }

    /// <inheritdoc/>
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
        => throw new NotSupportedException("Tenant-access persistence must always use conditional writes.");

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
        => state.TrySaveAsync(storeName, key, value, string.IsNullOrEmpty(etag) ? null : etag, cancellationToken);
}
