namespace Story65.OutboundAssertionProof;

using System.Text.Json;
using Microsoft.Extensions.Logging;

internal sealed class ProbeLogger<T> : ILogger<T>
{
    private readonly FixtureState state;
    public ProbeLogger(FixtureState state) => this.state = state;
    public IDisposable? BeginScope<TState>(TState scope) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) { state.Observe("logger.enabled", level.ToString()); return true; }
    public void Log<TState>(LogLevel level, EventId eventId, TState value, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(value, exception);
        Check.Require(level == LogLevel.Warning && eventId.Id == 5541 && exception is null && message.Length < 384
            && !message.Any(char.IsControl) && !message.Contains("synthetic-forwarded", StringComparison.Ordinal)
            && !message.Contains("synthetic private", StringComparison.Ordinal) && !message.Contains(FixtureState.FreshAssertion, StringComparison.Ordinal),
            "Original unavailable diagnostic bounds/effects changed.");
        if (state.Test.Fault == "issuer-null") Check.Require(message.Contains("Audience=<invalid-audience>", StringComparison.Ordinal), "Oversized audience was not bounded.");
        state.Logs.Add(JsonSerializer.Serialize(new { Level = level, Event = eventId.Id, Message = message })); state.Observe("logger.log", state.Logs[^1]);
        state.ThrowIf("logger");
    }
}
