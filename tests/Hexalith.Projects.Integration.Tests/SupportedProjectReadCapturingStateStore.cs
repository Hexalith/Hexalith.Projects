#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;
using Hexalith.Projects.Infrastructure;

/// <summary>Captures the actual projection store document using the real Dapr client serialization options.</summary>
/// <param name="json">The actual Dapr client's serialization options.</param>
internal sealed class SupportedProjectReadCapturingStateStore(JsonSerializerOptions json) : IProjectsStateStore
{
    private long _version;

    /// <summary>Gets the last actual store document.</summary>
    internal JsonElement? Document { get; private set; }

    /// <inheritdoc/>
    public Task<ProjectsStateEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ProjectsStateEntry<T>(Document is { } value ? value.Deserialize<T>(json) : default,
            _version == 0 ? null : _version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string? eTag, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (eTag != (_version == 0 ? null : _version.ToString(System.Globalization.CultureInfo.InvariantCulture))) return Task.FromResult(false);
        Document = JsonSerializer.SerializeToElement(value, json);
        _version++;
        return Task.FromResult(true);
    }
}
#endif
