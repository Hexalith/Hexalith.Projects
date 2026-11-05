#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Microsoft.Extensions.Logging;

/// <summary>Forwards actual SDK log state to the request-scoped denial observer.</summary>
/// <param name="category">The framework-provided logger category.</param>
/// <param name="observer">The test's observation sink.</param>
/// <param name="scopes">This host's actual external logging scopes.</param>
internal sealed class ProjectReadDenialLogger(string category, ProjectReadDenialObserver observer,
    Func<IExternalScopeProvider> scopes) : ILogger
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopes().Push(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message;
        try { message = formatter(state, exception); }
        catch (Exception error) { message = "[formatter-failed:" + error.GetType().FullName + "]"; }
        observer.RecordLog(category, logLevel, eventId, state, exception, scopes(), message);
    }
}
#endif
