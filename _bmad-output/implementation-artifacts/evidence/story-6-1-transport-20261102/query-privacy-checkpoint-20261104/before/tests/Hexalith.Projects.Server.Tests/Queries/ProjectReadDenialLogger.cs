#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Microsoft.Extensions.Logging;

/// <summary>Forwards actual SDK log state to the request-scoped denial observer.</summary>
/// <param name="category">The framework-provided logger category.</param>
/// <param name="observer">The test's observation sink.</param>
internal sealed class ProjectReadDenialLogger(string category, ProjectReadDenialObserver observer) : ILogger
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => observer.RecordLog(category, logLevel, eventId, state, exception);
}
#endif
