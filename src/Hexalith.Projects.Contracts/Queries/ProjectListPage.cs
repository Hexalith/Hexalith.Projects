namespace Hexalith.Projects.Contracts.Queries;

/// <summary>A bounded page of authorized Project metadata.</summary>
/// <param name="Items">Rows in ordinal Project identifier order.</param>
/// <param name="Cursor">The protected next-page cursor, or null.</param>
/// <param name="HasMore">Whether another authorized row exists.</param>
/// <param name="Snapshot">The list observation, including honest unavailable enumeration.</param>
public sealed record ProjectListPage(
    IReadOnlyList<ProjectListRow> Items,
    string? Cursor,
    bool HasMore,
    AdmissionSnapshot Snapshot);
