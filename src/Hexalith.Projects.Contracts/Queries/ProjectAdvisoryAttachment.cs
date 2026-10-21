namespace Hexalith.Projects.Contracts.Queries;

/// <summary>An opaque attachment identity, never a file path.</summary>
/// <param name="ReferenceKind">The owner kind: folder or file.</param>
/// <param name="ReferenceId">The exact opaque owner identity.</param>
public sealed record ProjectAdvisoryAttachment(string ReferenceKind, string ReferenceId);
