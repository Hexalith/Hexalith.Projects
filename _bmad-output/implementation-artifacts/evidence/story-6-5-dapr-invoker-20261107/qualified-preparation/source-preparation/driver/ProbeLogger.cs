namespace Story65.DaprInvokerProof;

using System.Text.Json;
using Microsoft.Extensions.Logging;

internal sealed class ProbeLogger<T> : ILogger<T>
{
    private readonly FixtureState state;
    public ProbeLogger(FixtureState state) => this.state = state;
    public IDisposable? BeginScope<TState>(TState scope) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId eventId, TState value, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var fields = value is IEnumerable<KeyValuePair<string, object?>> pairs ? pairs.ToArray() : [];
        string observation = JsonSerializer.Serialize(new { Level = level, Event = eventId.Id, Fields = fields, Exception = exception?.GetType().FullName });
        state.Logs.Add(observation); state.Observe("logger", observation);
        state.ThrowIf("logger");
    }
}
