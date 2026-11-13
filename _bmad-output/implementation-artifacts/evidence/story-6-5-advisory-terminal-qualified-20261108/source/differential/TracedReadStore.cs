namespace Story65.AdvisoryWaveProof;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Projects.Server.Projections.ProjectReads;

internal sealed class TracedReadStore(IReadModelStore inner, TraceLog trace) : IReadModelStore
{
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
    {
        bool index = key.EndsWith(":index", StringComparison.Ordinal);
        string item = index ? "index" : key[(key.LastIndexOf(':') + 1)..];
        int wave = index ? 3 : trace.Next("detail:" + item) == 1 ? 4 : 6;
        return trace.CallAsync(wave, index ? "index" : "detail", item, cancellationToken,
            () => inner.GetAsync<T>(storeName, key, cancellationToken), storeName + "/" + key);
    }
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
        => throw new InvalidOperationException("Advisory dependency attempted a write.");
    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
        => throw new InvalidOperationException("Advisory dependency attempted a CAS.");
}
