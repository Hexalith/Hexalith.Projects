namespace Hexalith.Projects.Server.Projections.ProjectReads;

/// <summary>The tenant-scoped supported enumeration index, atomically maintained with detail.</summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="ProjectIds">The sorted opaque identities of created Projects.</param>
/// <param name="GlobalPosition">The highest exact global position applied to this index.</param>
/// <param name="LastVerifiedAt">The persisted observation associated with that position.</param>
public sealed record ProjectReadIndex(
    string TenantId,
    IReadOnlyList<string> ProjectIds,
    long GlobalPosition,
    DateTimeOffset LastVerifiedAt)
{
    /// <summary>Gets source aggregates whose latest delivery could not be interpreted completely.</summary>
    public IReadOnlyList<string> UnavailableProjectIds { get; init; } = [];
}
