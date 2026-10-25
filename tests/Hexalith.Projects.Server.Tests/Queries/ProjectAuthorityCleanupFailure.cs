#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Microsoft.Extensions.Hosting;

/// <summary>Causes actual owned-host stop and service-container disposal failures for teardown coverage.</summary>
internal sealed class ProjectAuthorityCleanupFailure : IHostedService, IDisposable
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.FromException(new IOException("Synthetic owned-host stop failure."));
    /// <inheritdoc />
    public void Dispose() => throw new IOException("Synthetic owned-host disposal failure.");
}
#endif
