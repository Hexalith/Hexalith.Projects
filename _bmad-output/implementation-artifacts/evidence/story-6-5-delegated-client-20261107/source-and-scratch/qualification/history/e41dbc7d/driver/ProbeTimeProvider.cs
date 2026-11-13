namespace Story65.DelegatedClientProof;

internal sealed class ProbeTimeProvider(FixtureState state) : TimeProvider
{
    public override long TimestampFrequency => TimeProvider.System.TimestampFrequency;
    public override long GetTimestamp() { state.Observe("clock.timestamp"); return TimeProvider.System.GetTimestamp(); }
    public override DateTimeOffset GetUtcNow()
    { state.Observe("clock.utc"); state.Trigger("proof-clock-throw"); return TimeProvider.System.GetUtcNow(); }
    public override ITimer CreateTimer(TimerCallback callback, object? value, TimeSpan dueTime, TimeSpan period)
    { state.Observe("clock.timer", dueTime.Ticks + ":" + period.Ticks); return TimeProvider.System.CreateTimer(callback, value, dueTime, period); }
}
