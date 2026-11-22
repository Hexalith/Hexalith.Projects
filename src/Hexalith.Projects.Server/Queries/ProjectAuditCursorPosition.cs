namespace Hexalith.Projects.Server.Queries;

/// <summary>The protected boundary within a single snapshot of one authorized Project.</summary>
internal sealed record ProjectAuditCursorPosition(DateTimeOffset SnapshotAt, DateTimeOffset OccurredAt, string AuditEventId, string Revision, DateTimeOffset ExpiresAt);
