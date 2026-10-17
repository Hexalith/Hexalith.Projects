namespace Hexalith.Projects.Server.Queries;

/// <summary>Logical continuation protected exclusively by the platform cursor codec.</summary>
/// <param name="ProjectId">Last emitted Project identity.</param>
/// <param name="ExpiresAt">The domain list policy expiration instant.</param>
internal sealed record ProjectReadCursorPosition(string ProjectId, DateTimeOffset ExpiresAt);
