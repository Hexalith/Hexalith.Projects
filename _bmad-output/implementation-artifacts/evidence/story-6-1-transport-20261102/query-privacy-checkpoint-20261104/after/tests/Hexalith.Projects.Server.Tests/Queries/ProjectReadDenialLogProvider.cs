#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Microsoft.Extensions.Logging;

/// <summary>Owns one host's real logging scopes while forwarding every emitted record.</summary>
/// <param name="observer">The complete observation sink.</param>
internal sealed class ProjectReadDenialLogProvider(ProjectReadDenialObserver observer) : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new ProjectReadDenialLogger(categoryName, observer, () => _scopes);

    /// <inheritdoc/>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    /// <inheritdoc/>
    public void Dispose() { }
}
#endif
