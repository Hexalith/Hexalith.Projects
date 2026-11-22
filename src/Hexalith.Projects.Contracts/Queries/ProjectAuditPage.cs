namespace Hexalith.Projects.Contracts.Queries;

/// <summary>A bounded page of Safe Metadata, newest first.</summary>
/// <param name="State">Coverage of committed historical and security facts.</param>
/// <param name="Items">The currently authorized observations.</param>
/// <param name="Cursor">An opaque continuation, when another page is available.</param>
public sealed record ProjectAuditPage(string State, IReadOnlyList<ProjectAuditFact> Items, string? Cursor);
