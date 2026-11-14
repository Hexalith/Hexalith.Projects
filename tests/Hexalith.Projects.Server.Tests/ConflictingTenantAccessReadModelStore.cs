namespace Hexalith.Projects.Server.Tests;

using Hexalith.EventStore.Client.Projections;

/// <summary>Refuses conditional writes to exercise the platform's exhausted retry path.</summary>
/// <param name="inner">The persisted backend observed after refusal.</param>
internal sealed class ConflictingTenantAccessReadModelStore(IReadModelStore inner) : IReadModelStore
{
    /// <summary>Gets the number of conditional attempts.</summary>
    internal int Attempts { get; private set; }

    /// <inheritdoc/>
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
        => inner.GetAsync<T>(storeName, key, cancellationToken);

    /// <inheritdoc/>
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
        => throw new NotSupportedException("Unconditional authorization writes are forbidden.");

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        Attempts++;
        return Task.FromResult(false);
    }
}
