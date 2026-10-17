namespace Hexalith.Projects.Server.Queries;

/// <summary>Durable FR-21 metadata; contains no actor, Project identity, or descriptive content.</summary>
/// <param name="RequestSurrogate">A digest binding the immutable request and authenticated identity.</param>
/// <param name="Outcome">Inspection or SafeDenial.</param>
/// <param name="ProjectCount">The number of Projects whose descriptive fields were returned.</param>
/// <param name="FieldClass">The closed inspected field class.</param>
/// <param name="RecordedAt">The first durable request observation.</param>
public sealed record ProjectReadAuditRecord(string RequestSurrogate, string Outcome, int ProjectCount, string FieldClass, DateTimeOffset RecordedAt);
