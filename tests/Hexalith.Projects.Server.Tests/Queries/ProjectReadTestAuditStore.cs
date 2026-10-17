namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.Client.Projections;

internal sealed class ProjectReadTestAuditStore(IReadModelStore inner) : IReadModelStore
{
    public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
        => inner.GetAsync<T>(storeName, key, cancellationToken);
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
        => inner.SaveAsync(storeName, key, value, cancellationToken);
    public async Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        bool saved = await inner.TrySaveAsync(storeName, key, value, etag, cancellationToken).ConfigureAwait(false);
        if (saved) Keys.Add(key);
        return saved;
    }
}
