namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Projects.Server.Projections.ProjectReads;

internal sealed class AdvisoryGuardedReadStore(IReadModelStore inner) : IReadModelStore
{
    public ConcurrentBag<string> Reads { get; } = [];
    public bool EmptyETag { get; set; }
    public IReadOnlyList<string>? IndexIdentities { get; set; }
    public Func<string, CancellationToken, Task>? BeforeRead { get; set; }
    public async Task<ReadModelEntry<T>> GetAsync<T>(string store, string key, CancellationToken cancellationToken = default) where T : class
    {
        Reads.Add(key);
        if (BeforeRead is not null) await BeforeRead(key, cancellationToken).ConfigureAwait(false);
        var entry = await inner.GetAsync<T>(store, key, cancellationToken).ConfigureAwait(false);
        if (entry.Value is ProjectReadIndex index && IndexIdentities is not null)
            return new((T)(object)(index with { ProjectIds = IndexIdentities }), entry.ETag);
        return EmptyETag ? new(entry.Value, string.Empty) : entry;
    }
    public Task SaveAsync<T>(string store, string key, T value, CancellationToken cancellationToken = default) where T : class
        => throw new InvalidOperationException("Advisory queries cannot persist.");
    public Task<bool> TrySaveAsync<T>(string store, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
        => throw new InvalidOperationException("Advisory queries cannot persist.");
}
