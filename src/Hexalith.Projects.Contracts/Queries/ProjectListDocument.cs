namespace Hexalith.Projects.Contracts.Queries;

/// <summary>The authorized Project rows and observation returned by an envelope-paged list.</summary>
/// <param name="Items">Rows in ordinal Project identifier order.</param>
/// <param name="Snapshot">The list observation, including honest unavailable enumeration.</param>
public sealed record ProjectListDocument(IReadOnlyList<ProjectListRow> Items, AdmissionSnapshot Snapshot);
