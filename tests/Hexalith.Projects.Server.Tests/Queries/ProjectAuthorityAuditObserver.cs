#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using Hexalith.EventStore.Client.Projections;

/// <summary>Observes successful durable audit writes while preserving the platform persistence implementation.</summary>
internal sealed class ProjectAuthorityAuditObserver(IReadModelStore inner) : IReadModelStore
{
    /// <summary>Gets keys whose platform audit writes succeeded.</summary>
    public ConcurrentDictionary<string, byte> Keys { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets or sets a synthetic audit-store outage.</summary>
    public bool Unavailable { get; set; }
    /// <inheritdoc />
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
        => inner.GetAsync<T>(storeName, key, cancellationToken);
    /// <inheritdoc />
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
        => inner.SaveAsync(storeName, key, value, cancellationToken);
    /// <inheritdoc />
    public async Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        if (Unavailable) throw new IOException("Synthetic audit persistence outage.");
        bool saved = await inner.TrySaveAsync(storeName, key, value, etag, cancellationToken).ConfigureAwait(false);
        if (saved) Keys.TryAdd(key, 0);
        return saved;
    }
}
#endif
