#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using Hexalith.EventStore.Client.Projections;

/// <summary>Observes the real SDK's physical read keys without reproducing its key algorithms.</summary>
/// <param name="inner">The actual read-model store used by the SDK coordinator.</param>
internal sealed class SupportedProjectReadKeyRecordingStore(IReadModelStore inner) : IReadModelStore
{
    /// <summary>Gets the exact keys selected by real SDK read operations.</summary>
    internal List<string> ReadKeys { get; } = [];

    /// <summary>Gets the number of attempted writes.</summary>
    internal int WriteCount { get; private set; }

    /// <inheritdoc/>
    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadKeys.Add(key);
        return inner.GetAsync<TValue>(storeName, key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        where TValue : class
    {
        WriteCount++;
        return inner.SaveAsync(storeName, key, value, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string expectedETag, CancellationToken cancellationToken = default)
        where TValue : class
    {
        WriteCount++;
        return inner.TrySaveAsync(storeName, key, value, expectedETag, cancellationToken);
    }
}
#endif
