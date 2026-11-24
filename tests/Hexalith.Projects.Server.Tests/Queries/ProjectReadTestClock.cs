namespace Hexalith.Projects.Server.Tests.Queries;

internal sealed class ProjectReadTestClock(DateTimeOffset now) : TimeProvider, Hexalith.Projects.Projections.TenantAccess.IUtcClock
{
    public DateTimeOffset Now { get; set; } = now;
    public bool PauseTimers { get; set; }
    public override DateTimeOffset GetUtcNow() => Now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        => base.CreateTimer(callback, state, PauseTimers ? Timeout.InfiniteTimeSpan : dueTime, period);
    public DateTimeOffset UtcNow => Now;
}
