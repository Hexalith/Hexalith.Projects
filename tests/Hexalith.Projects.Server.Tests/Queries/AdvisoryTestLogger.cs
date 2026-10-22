namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Server.Queries;
using Microsoft.Extensions.Logging;

internal sealed class AdvisoryTestLogger : ILogger<ProjectAdvisoryQueryExecutor>
{
    public List<(LogLevel Level, string Message, Exception? Exception, string[] Keys)> Entries { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception), exception,
            ((IEnumerable<KeyValuePair<string, object?>>)state!).Select(pair => pair.Key).ToArray()));
}
