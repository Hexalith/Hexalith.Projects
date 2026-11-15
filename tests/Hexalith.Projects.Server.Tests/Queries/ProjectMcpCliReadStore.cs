#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using Hexalith.EventStore.Client.Projections;

/// <summary>Observes persisted SDK writes and can fail only audit persistence in the real Gateway fixture.</summary>
internal sealed class ProjectMcpCliReadStore(IReadModelStore inner) : IReadModelStore
{
    public ConcurrentDictionary<string, byte> Writes { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<(string Store, string Key), byte> Reads { get; } = new();
    public bool FailAuditWrites { get; set; }
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
    {
        Reads.TryAdd((storeName, key), 0);
        return inner.GetAsync<T>(storeName, key, cancellationToken);
    }
    public Task<ReadModelEntry<T>> GetCompactedAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
    {
        Reads.TryAdd((storeName, key), 0);
        return inner.GetCompactedAsync<T>(storeName, key, cancellationToken);
    }
    public async Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        RefuseAuditFailure(key);
        await inner.SaveAsync(storeName, key, value, cancellationToken).ConfigureAwait(false);
        Writes.TryAdd(key, 0);
    }
    public async Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        RefuseAuditFailure(key);
        bool saved = await inner.TrySaveAsync(storeName, key, value, etag, cancellationToken).ConfigureAwait(false);
        if (saved) Writes.TryAdd(key, 0);
        return saved;
    }
    private void RefuseAuditFailure(string key)
    {
        if (FailAuditWrites && key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal))
        {
            throw new IOException("Inspection audit persistence unavailable");
        }
    }
}
#endif
