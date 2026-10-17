namespace Hexalith.Projects.Server.Tests.Queries;

internal sealed class ProjectReadTestClock(DateTimeOffset now) : TimeProvider, Hexalith.Projects.Projections.TenantAccess.IUtcClock
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
    public DateTimeOffset UtcNow => Now;
}
